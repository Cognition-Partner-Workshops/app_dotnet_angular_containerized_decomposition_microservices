/**
 * K6 Performance Test — Identity Service
 *
 * Endpoints tested:
 *   GET  /api/identity       — List all identities
 *   GET  /api/identity/{id}  — Get identity by ID
 *
 * Run:
 *   k6 run identity-test.js
 *   k6 run -e BASE_URL=http://localhost:5001 identity-test.js
 */
import http from 'k6/http';
import { check, group } from 'k6';
import { SharedArray } from 'k6/data';
import { IDENTITY_URL, DEFAULT_THRESHOLDS, DEFAULT_STAGES } from './config.js';
import { jsonHeaders, basicAuthHeaders, fetchBearerToken, checkGetResponse } from './utils.js';

// ---------------------------------------------------------------------------
// Data-driven testing — load identity test data via SharedArray
// ---------------------------------------------------------------------------
const identities = new SharedArray('identities', function () {
  return JSON.parse(open('./data/identities.json'));
});

// ---------------------------------------------------------------------------
// Options
// ---------------------------------------------------------------------------
export const options = {
  stages: DEFAULT_STAGES,
  thresholds: {
    ...DEFAULT_THRESHOLDS,
    'http_req_duration{group:::Identity - Get All}':    ['p(95)<400'],
    'http_req_duration{group:::Identity - Get By ID}':  ['p(95)<300'],
  },
  tags: { service: 'identity' },
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

  // ---- Group 1: Get All Identities ----
  group('Identity - Get All', function () {
    const res = http.get(IDENTITY_URL, {
      headers,
      tags: { name: 'GET /api/identity' },
    });

    checkGetResponse(res, 'Get All Identities');

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

  // ---- Group 2: Get Identity By ID (data-driven) ----
  group('Identity - Get By ID', function () {
    const identity = identities[Math.floor(Math.random() * identities.length)];

    const res = http.get(`${IDENTITY_URL}/${identity.id}`, {
      headers,
      tags: { name: 'GET /api/identity/{id}' },
    });

    checkGetResponse(res, `Get Identity ${identity.id}`);

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
