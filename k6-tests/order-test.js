/**
 * K6 Performance Test — Order Service
 *
 * Endpoints tested:
 *   GET  /api/order       — List all orders
 *   GET  /api/order/{id}  — Get order by ID
 *
 * Run:
 *   k6 run order-test.js
 *   k6 run -e BASE_URL=http://localhost:5003 order-test.js
 */
import http from 'k6/http';
import { check, group } from 'k6';
import { SharedArray } from 'k6/data';
import { ORDER_URL, DEFAULT_THRESHOLDS, DEFAULT_STAGES } from './config.js';
import { jsonHeaders, basicAuthHeaders, fetchBearerToken, checkGetResponse } from './utils.js';

// ---------------------------------------------------------------------------
// Data-driven testing — load order test data via SharedArray
// ---------------------------------------------------------------------------
const orders = new SharedArray('orders', function () {
  return JSON.parse(open('./data/orders.json'));
});

// ---------------------------------------------------------------------------
// Options
// ---------------------------------------------------------------------------
export const options = {
  stages: DEFAULT_STAGES,
  thresholds: {
    ...DEFAULT_THRESHOLDS,
    'http_req_duration{group:::Order - Get All}':    ['p(95)<400'],
    'http_req_duration{group:::Order - Get By ID}':  ['p(95)<300'],
  },
  tags: { service: 'order' },
};

// ---------------------------------------------------------------------------
// Setup — authenticate and return a token for use in default function
// ---------------------------------------------------------------------------
export function setup() {
  const token = fetchBearerToken();
  return { token };
}

// ---------------------------------------------------------------------------
// Default (main) function — executed by each VU on every iteration
// ---------------------------------------------------------------------------
export default function (data) {
  const headers = data.token
    ? jsonHeaders(data.token)
    : basicAuthHeaders();

  // ---- Group 1: Get All Orders ----
  group('Order - Get All', function () {
    const res = http.get(ORDER_URL, {
      headers,
      tags: { name: 'GET /api/order' },
    });

    checkGetResponse(res, 'Get All Orders');

    check(res, {
      'response is JSON': (r) => {
        try {
          JSON.parse(r.body);
          return true;
        } catch (_) {
          return false;
        }
      },
    });
  });

  // ---- Group 2: Get Order By ID (data-driven) ----
  group('Order - Get By ID', function () {
    const order = orders[Math.floor(Math.random() * orders.length)];

    const res = http.get(`${ORDER_URL}/${order.id}`, {
      headers,
      tags: { name: 'GET /api/order/{id}' },
    });

    checkGetResponse(res, `Get Order ${order.id}`);

    check(res, {
      'response contains id field': (r) => {
        try {
          const body = JSON.parse(r.body);
          return body.id !== undefined;
        } catch (_) {
          return false;
        }
      },
    });
  });
}
