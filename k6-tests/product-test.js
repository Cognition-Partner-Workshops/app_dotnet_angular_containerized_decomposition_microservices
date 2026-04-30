/**
 * K6 Performance Test — Product Service
 *
 * Endpoints tested:
 *   GET  /api/product       — List all products
 *   GET  /api/product/{id}  — Get product by ID
 *
 * Run:
 *   k6 run product-test.js
 *   k6 run -e BASE_URL=http://localhost:5004 product-test.js
 */
import http from 'k6/http';
import { check, group } from 'k6';
import { SharedArray } from 'k6/data';
import { PRODUCT_URL, DEFAULT_THRESHOLDS, DEFAULT_STAGES } from './config.js';
import { jsonHeaders, basicAuthHeaders, fetchBearerToken, checkGetResponse } from './utils.js';

// ---------------------------------------------------------------------------
// Data-driven testing — load product test data via SharedArray
// ---------------------------------------------------------------------------
const products = new SharedArray('products', function () {
  return JSON.parse(open('./data/products.json'));
});

// ---------------------------------------------------------------------------
// Options
// ---------------------------------------------------------------------------
export const options = {
  stages: DEFAULT_STAGES,
  thresholds: {
    ...DEFAULT_THRESHOLDS,
    'http_req_duration{group:::Product - Get All}':    ['p(95)<400'],
    'http_req_duration{group:::Product - Get By ID}':  ['p(95)<300'],
  },
  tags: { service: 'product' },
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

  // ---- Group 1: Get All Products ----
  group('Product - Get All', function () {
    const res = http.get(PRODUCT_URL, {
      headers,
      tags: { name: 'GET /api/product' },
    });

    checkGetResponse(res, 'Get All Products');

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

  // ---- Group 2: Get Product By ID (data-driven) ----
  group('Product - Get By ID', function () {
    const product = products[Math.floor(Math.random() * products.length)];

    const res = http.get(`${PRODUCT_URL}/${product.id}`, {
      headers,
      tags: { name: 'GET /api/product/{id}' },
    });

    checkGetResponse(res, `Get Product ${product.id}`);

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
