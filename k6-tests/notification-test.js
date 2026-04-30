/**
 * K6 Performance Test — Notification Service
 *
 * Endpoints tested:
 *   GET  /api/notification              — List all notifications (paginated)
 *   GET  /api/notification/{id}         — Get notification by ID
 *   GET  /api/notification/{id}/preview — Get rendered HTML email preview
 *   POST /api/notification/events/order-placed — Submit an OrderPlacedEvent
 *
 * Run:
 *   k6 run notification-test.js
 *   k6 run -e BASE_URL=http://localhost:5005 notification-test.js
 */
import http from 'k6/http';
import { check, group } from 'k6';
import { SharedArray } from 'k6/data';
import { NOTIFICATION_URL, DEFAULT_THRESHOLDS, DEFAULT_STAGES } from './config.js';
import {
  jsonHeaders,
  basicAuthHeaders,
  fetchBearerToken,
  checkGetResponse,
  checkPostResponse,
} from './utils.js';

// ---------------------------------------------------------------------------
// Data-driven testing — load notification event payloads via SharedArray
// ---------------------------------------------------------------------------
const notificationEvents = new SharedArray('notifications', function () {
  return JSON.parse(open('./data/notifications.json'));
});

// ---------------------------------------------------------------------------
// Options
// ---------------------------------------------------------------------------
export const options = {
  stages: DEFAULT_STAGES,
  thresholds: {
    ...DEFAULT_THRESHOLDS,
    'http_req_duration{group:::Notification - Get All}':           ['p(95)<500'],
    'http_req_duration{group:::Notification - Get By ID}':         ['p(95)<400'],
    'http_req_duration{group:::Notification - Preview}':           ['p(95)<600'],
    'http_req_duration{group:::Notification - Post Order Event}':  ['p(95)<1000'],
  },
  tags: { service: 'notification' },
};

// ---------------------------------------------------------------------------
// Setup — authenticate and seed a notification so GET-by-ID / preview work
// ---------------------------------------------------------------------------
export function setup() {
  const token = fetchBearerToken();
  const headers = token
    ? { 'Content-Type': 'application/json', Accept: 'application/json', Authorization: `Bearer ${token}` }
    : { 'Content-Type': 'application/json', Accept: 'application/json' };

  // Seed one notification via POST so we have a known ID for GET tests
  const seedPayload = JSON.stringify(notificationEvents[0]);
  const seedRes = http.post(
    `${NOTIFICATION_URL}/events/order-placed`,
    seedPayload,
    { headers, tags: { name: 'SETUP - seed notification' } },
  );

  let seededId = null;
  if (seedRes.status === 201) {
    try {
      const body = JSON.parse(seedRes.body);
      seededId = body.id || null;
    } catch (_) {
      // Seed failed — GET-by-ID tests will use fallback
    }
  }

  return { token, seededId };
}

// ---------------------------------------------------------------------------
// Default (main) function — executed by each VU on every iteration
// ---------------------------------------------------------------------------
export default function (data) {
  const headers = data.token
    ? jsonHeaders(data.token)
    : basicAuthHeaders();

  let createdId = null;

  // ---- Group 1: POST — Submit Order-Placed Event ----
  group('Notification - Post Order Event', function () {
    const event = notificationEvents[Math.floor(Math.random() * notificationEvents.length)];
    const payload = JSON.stringify({
      orderId:     event.orderId,
      customerId:  event.customerId,
      totalAmount: event.totalAmount,
      placedAt:    event.placedAt,
    });

    const res = http.post(`${NOTIFICATION_URL}/events/order-placed`, payload, {
      headers,
      tags: { name: 'POST /api/notification/events/order-placed' },
    });

    checkPostResponse(res, 'Post OrderPlacedEvent');

    check(res, {
      'response contains notification id': (r) => {
        try {
          const body = JSON.parse(r.body);
          return body.id !== undefined && body.id !== null;
        } catch (_) {
          return false;
        }
      },
      'response contains preview URL': (r) => {
        try {
          const body = JSON.parse(r.body);
          return body.previewUrl !== undefined;
        } catch (_) {
          return false;
        }
      },
    });

    // Capture the created ID for subsequent GET requests
    if (res.status === 201) {
      try {
        const body = JSON.parse(res.body);
        createdId = body.id;
      } catch (_) {
        // fall through
      }
    }
  });

  // ---- Group 2: GET — List All Notifications (paginated) ----
  group('Notification - Get All', function () {
    const page = Math.floor(Math.random() * 3) + 1;
    const res = http.get(`${NOTIFICATION_URL}?page=${page}&pageSize=20`, {
      headers,
      tags: { name: 'GET /api/notification' },
    });

    checkGetResponse(res, 'Get All Notifications');

    check(res, {
      'response is JSON array': (r) => {
        try {
          return Array.isArray(JSON.parse(r.body));
        } catch (_) {
          return false;
        }
      },
    });
  });

  // ---- Group 3: GET — Get Notification By ID ----
  const notificationId = createdId || data.seededId;
  if (notificationId) {
    group('Notification - Get By ID', function () {
      const res = http.get(`${NOTIFICATION_URL}/${notificationId}`, {
        headers,
        tags: { name: 'GET /api/notification/{id}' },
      });

      checkGetResponse(res, `Get Notification ${notificationId}`);

      check(res, {
        'response contains orderId': (r) => {
          try {
            const body = JSON.parse(r.body);
            return body.orderId !== undefined;
          } catch (_) {
            return false;
          }
        },
        'response contains status': (r) => {
          try {
            const body = JSON.parse(r.body);
            return body.status !== undefined;
          } catch (_) {
            return false;
          }
        },
      });
    });

    // ---- Group 4: GET — HTML Email Preview ----
    group('Notification - Preview', function () {
      const res = http.get(`${NOTIFICATION_URL}/${notificationId}/preview`, {
        headers: { ...headers, Accept: 'text/html' },
        tags: { name: 'GET /api/notification/{id}/preview' },
      });

      check(res, {
        'preview — status is 200': (r) => r.status === 200,
        'preview — response time < 600ms': (r) => r.timings.duration < 600,
        'preview — content-type is HTML': (r) =>
          r.headers['Content-Type'] && r.headers['Content-Type'].includes('text/html'),
        'preview — body contains HTML': (r) => r.body && r.body.includes('<'),
      });
    });
  }
}
