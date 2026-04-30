/**
 * K6 Performance Test — Customer Service
 *
 * Endpoints tested:
 *   GET  /api/customer       — List all customers
 *   GET  /api/customer/{id}  — Get customer by ID
 *
 * Run:
 *   k6 run customer-test.js
 *   k6 run -e BASE_URL=http://localhost:5002 customer-test.js
 */
import http from 'k6/http';
import { check, group } from 'k6';
import { SharedArray } from 'k6/data';
import { CUSTOMER_URL, DEFAULT_THRESHOLDS, DEFAULT_STAGES } from './config.js';
import { jsonHeaders, basicAuthHeaders, fetchBearerToken, checkGetResponse } from './utils.js';

// ---------------------------------------------------------------------------
// Data-driven testing — load customer test data via SharedArray
// ---------------------------------------------------------------------------
const customers = new SharedArray('customers', function () {
  return JSON.parse(open('./data/customers.json'));
});

// ---------------------------------------------------------------------------
// Options
// ---------------------------------------------------------------------------
export const options = {
  stages: DEFAULT_STAGES,
  thresholds: {
    ...DEFAULT_THRESHOLDS,
    'http_req_duration{group:::Customer - Get All}':    ['p(95)<400'],
    'http_req_duration{group:::Customer - Get By ID}':  ['p(95)<300'],
  },
  tags: { service: 'customer' },
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

  // ---- Group 1: Get All Customers ----
  group('Customer - Get All', function () {
    const res = http.get(CUSTOMER_URL, {
      headers,
      tags: { name: 'GET /api/customer' },
    });

    checkGetResponse(res, 'Get All Customers');

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

  // ---- Group 2: Get Customer By ID (data-driven) ----
  group('Customer - Get By ID', function () {
    const customer = customers[Math.floor(Math.random() * customers.length)];

    const res = http.get(`${CUSTOMER_URL}/${customer.id}`, {
      headers,
      tags: { name: 'GET /api/customer/{id}' },
    });

    checkGetResponse(res, `Get Customer ${customer.id}`);

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
