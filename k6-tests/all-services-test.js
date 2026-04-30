/**
 * K6 Combined Performance Test — All 5 Microservices
 *
 * 10 Virtual Users, 2 per service, each service hit 25 times over 5 minutes.
 * Uses per-VU scenarios so each pair of VUs targets a specific service.
 * Different data records are selected per iteration to emulate production traffic patterns.
 *
 * Run:
 *   k6 run all-services-test.js
 *   k6 run -e BASE_URL=http://localhost:5000 all-services-test.js
 */
import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { SharedArray } from 'k6/data';
import { Counter, Trend } from 'k6/metrics';
import encoding from 'k6/encoding';

// ---------------------------------------------------------------------------
// Configuration
// ---------------------------------------------------------------------------
const BASE_URL         = __ENV.BASE_URL         || 'http://localhost:5000';
const IDENTITY_URL     = __ENV.IDENTITY_URL     || `${BASE_URL}/api/identity`;
const CUSTOMER_URL     = __ENV.CUSTOMER_URL     || `${BASE_URL}/api/customer`;
const ORDER_URL        = __ENV.ORDER_URL        || `${BASE_URL}/api/order`;
const PRODUCT_URL      = __ENV.PRODUCT_URL      || `${BASE_URL}/api/product`;
const NOTIFICATION_URL = __ENV.NOTIFICATION_URL  || `${BASE_URL}/api/notification`;

const AUTH_USERNAME    = __ENV.AUTH_USERNAME    || 'admin';
const AUTH_PASSWORD    = __ENV.AUTH_PASSWORD    || 'password';
const AUTH_TOKEN_URL   = __ENV.AUTH_TOKEN_URL   || `${BASE_URL}/api/identity/token`;

// ---------------------------------------------------------------------------
// Custom metrics per service
// ---------------------------------------------------------------------------
const identityRequests     = new Counter('identity_requests');
const customerRequests     = new Counter('customer_requests');
const orderRequests        = new Counter('order_requests');
const productRequests      = new Counter('product_requests');
const notificationRequests = new Counter('notification_requests');

const identityDuration     = new Trend('identity_duration', true);
const customerDuration     = new Trend('customer_duration', true);
const orderDuration        = new Trend('order_duration', true);
const productDuration      = new Trend('product_duration', true);
const notificationDuration = new Trend('notification_duration', true);

// ---------------------------------------------------------------------------
// Data-driven testing — 20 records each, cycled with different data per VU
// ---------------------------------------------------------------------------
const identities = new SharedArray('identities', function () {
  return JSON.parse(open('./data/identities.json'));
});

const customers = new SharedArray('customers', function () {
  return JSON.parse(open('./data/customers.json'));
});

const orders = new SharedArray('orders', function () {
  return JSON.parse(open('./data/orders.json'));
});

const products = new SharedArray('products', function () {
  return JSON.parse(open('./data/products.json'));
});

const notificationEvents = new SharedArray('notifications', function () {
  return JSON.parse(open('./data/notifications.json'));
});

// ---------------------------------------------------------------------------
// Scenarios: 2 VUs per service, 25 iterations each = 50 per service
// 5 min duration with pacing: sleep ~12s between iterations (300s / 25 = 12s)
// ---------------------------------------------------------------------------
export const options = {
  scenarios: {
    identity_load: {
      executor: 'per-vu-iterations',
      vus: 2,
      iterations: 25,
      maxDuration: '5m',
      exec: 'identityScenario',
      tags: { service: 'identity' },
    },
    customer_load: {
      executor: 'per-vu-iterations',
      vus: 2,
      iterations: 25,
      maxDuration: '5m',
      exec: 'customerScenario',
      tags: { service: 'customer' },
    },
    order_load: {
      executor: 'per-vu-iterations',
      vus: 2,
      iterations: 25,
      maxDuration: '5m',
      exec: 'orderScenario',
      tags: { service: 'order' },
    },
    product_load: {
      executor: 'per-vu-iterations',
      vus: 2,
      iterations: 25,
      maxDuration: '5m',
      exec: 'productScenario',
      tags: { service: 'product' },
    },
    notification_load: {
      executor: 'per-vu-iterations',
      vus: 2,
      iterations: 25,
      maxDuration: '5m',
      exec: 'notificationScenario',
      tags: { service: 'notification' },
    },
  },
  thresholds: {
    http_req_duration:         ['p(95)<500', 'p(99)<1000'],
    http_req_failed:           ['rate<0.05'],
    checks:                    ['rate>0.95'],
    identity_duration:         ['p(95)<400'],
    customer_duration:         ['p(95)<400'],
    order_duration:            ['p(95)<400'],
    product_duration:          ['p(95)<400'],
    notification_duration:     ['p(95)<800'],
  },
};

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------
function authHeaders(token) {
  const base = { 'Content-Type': 'application/json', Accept: 'application/json' };
  if (token) {
    base['Authorization'] = `Bearer ${token}`;
  } else {
    const encoded = encoding.b64encode(`${AUTH_USERNAME}:${AUTH_PASSWORD}`);
    base['Authorization'] = `Basic ${encoded}`;
  }
  return base;
}

function fetchToken() {
  const res = http.post(AUTH_TOKEN_URL, JSON.stringify({
    grant_type: 'client_credentials',
    username: AUTH_USERNAME,
    password: AUTH_PASSWORD,
  }), { headers: { 'Content-Type': 'application/json' }, tags: { name: 'auth_token' } });

  if (res.status === 200) {
    try {
      const body = JSON.parse(res.body);
      return body.access_token || body.token || null;
    } catch (_) { return null; }
  }
  return null;
}

function pick(arr, iteration) {
  return arr[iteration % arr.length];
}

// Pacing: spread 25 iterations evenly across ~5 min ≈ 12s between requests
// Add jitter ±3s to simulate realistic production variance
function paceRequest() {
  const base = 10;
  const jitter = Math.random() * 6 - 3;
  sleep(Math.max(1, base + jitter));
}

// ---------------------------------------------------------------------------
// Setup — fetch auth token once
// ---------------------------------------------------------------------------
export function setup() {
  const token = fetchToken();
  return { token };
}

// ---------------------------------------------------------------------------
// Scenario: Identity Service (VUs 1-2)
// ---------------------------------------------------------------------------
export function identityScenario(data) {
  const headers = authHeaders(data.token);
  const iter = __ITER;
  const identity = pick(identities, iter + __VU);

  group('Identity - Get All', function () {
    const res = http.get(IDENTITY_URL, {
      headers,
      tags: { name: 'GET /api/identity' },
    });
    identityRequests.add(1);
    identityDuration.add(res.timings.duration);

    check(res, {
      'Identity GetAll — status 200': (r) => r.status === 200,
      'Identity GetAll — response time < 500ms': (r) => r.timings.duration < 500,
      'Identity GetAll — body is not empty': (r) => r.body && r.body.length > 0,
    });
  });

  group('Identity - Get By ID', function () {
    const res = http.get(`${IDENTITY_URL}/${identity.id}`, {
      headers,
      tags: { name: 'GET /api/identity/{id}' },
    });
    identityRequests.add(1);
    identityDuration.add(res.timings.duration);

    check(res, {
      [`Identity Get #${identity.id} — status 200`]: (r) => r.status === 200,
      [`Identity Get #${identity.id} — has id field`]: (r) => {
        try { return JSON.parse(r.body).id !== undefined; } catch (_) { return false; }
      },
      [`Identity Get #${identity.id} — response < 300ms`]: (r) => r.timings.duration < 300,
    });
  });

  paceRequest();
}

// ---------------------------------------------------------------------------
// Scenario: Customer Service (VUs 3-4)
// ---------------------------------------------------------------------------
export function customerScenario(data) {
  const headers = authHeaders(data.token);
  const iter = __ITER;
  const customer = pick(customers, iter + __VU);

  group('Customer - Get All', function () {
    const res = http.get(CUSTOMER_URL, {
      headers,
      tags: { name: 'GET /api/customer' },
    });
    customerRequests.add(1);
    customerDuration.add(res.timings.duration);

    check(res, {
      'Customer GetAll — status 200': (r) => r.status === 200,
      'Customer GetAll — response time < 500ms': (r) => r.timings.duration < 500,
      'Customer GetAll — body is not empty': (r) => r.body && r.body.length > 0,
    });
  });

  group('Customer - Get By ID', function () {
    const res = http.get(`${CUSTOMER_URL}/${customer.id}`, {
      headers,
      tags: { name: 'GET /api/customer/{id}' },
    });
    customerRequests.add(1);
    customerDuration.add(res.timings.duration);

    check(res, {
      [`Customer Get #${customer.id} — status 200`]: (r) => r.status === 200,
      [`Customer Get #${customer.id} — has id field`]: (r) => {
        try { return JSON.parse(r.body).id !== undefined; } catch (_) { return false; }
      },
      [`Customer Get #${customer.id} — response < 300ms`]: (r) => r.timings.duration < 300,
    });
  });

  paceRequest();
}

// ---------------------------------------------------------------------------
// Scenario: Order Service (VUs 5-6)
// ---------------------------------------------------------------------------
export function orderScenario(data) {
  const headers = authHeaders(data.token);
  const iter = __ITER;
  const order = pick(orders, iter + __VU);

  group('Order - Get All', function () {
    const res = http.get(ORDER_URL, {
      headers,
      tags: { name: 'GET /api/order' },
    });
    orderRequests.add(1);
    orderDuration.add(res.timings.duration);

    check(res, {
      'Order GetAll — status 200': (r) => r.status === 200,
      'Order GetAll — response time < 500ms': (r) => r.timings.duration < 500,
      'Order GetAll — body is not empty': (r) => r.body && r.body.length > 0,
    });
  });

  group('Order - Get By ID', function () {
    const res = http.get(`${ORDER_URL}/${order.id}`, {
      headers,
      tags: { name: 'GET /api/order/{id}' },
    });
    orderRequests.add(1);
    orderDuration.add(res.timings.duration);

    check(res, {
      [`Order Get #${order.id} — status 200`]: (r) => r.status === 200,
      [`Order Get #${order.id} — has id field`]: (r) => {
        try { return JSON.parse(r.body).id !== undefined; } catch (_) { return false; }
      },
      [`Order Get #${order.id} — response < 300ms`]: (r) => r.timings.duration < 300,
    });
  });

  paceRequest();
}

// ---------------------------------------------------------------------------
// Scenario: Product Service (VUs 7-8)
// ---------------------------------------------------------------------------
export function productScenario(data) {
  const headers = authHeaders(data.token);
  const iter = __ITER;
  const product = pick(products, iter + __VU);

  group('Product - Get All', function () {
    const res = http.get(PRODUCT_URL, {
      headers,
      tags: { name: 'GET /api/product' },
    });
    productRequests.add(1);
    productDuration.add(res.timings.duration);

    check(res, {
      'Product GetAll — status 200': (r) => r.status === 200,
      'Product GetAll — response time < 500ms': (r) => r.timings.duration < 500,
      'Product GetAll — body is not empty': (r) => r.body && r.body.length > 0,
    });
  });

  group('Product - Get By ID', function () {
    const res = http.get(`${PRODUCT_URL}/${product.id}`, {
      headers,
      tags: { name: 'GET /api/product/{id}' },
    });
    productRequests.add(1);
    productDuration.add(res.timings.duration);

    check(res, {
      [`Product Get #${product.id} — status 200`]: (r) => r.status === 200,
      [`Product Get #${product.id} — has id field`]: (r) => {
        try { return JSON.parse(r.body).id !== undefined; } catch (_) { return false; }
      },
      [`Product Get #${product.id} — response < 300ms`]: (r) => r.timings.duration < 300,
    });
  });

  paceRequest();
}

// ---------------------------------------------------------------------------
// Scenario: Notification Service (VUs 9-10)
// Includes POST + GET + Preview — mimics order-driven notification flow
// ---------------------------------------------------------------------------
export function notificationScenario(data) {
  const headers = authHeaders(data.token);
  const iter = __ITER;
  const event = pick(notificationEvents, iter + __VU);

  let createdId = null;

  // POST: Submit a new order-placed event (creates a notification)
  group('Notification - Post Order Event', function () {
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
    notificationRequests.add(1);
    notificationDuration.add(res.timings.duration);

    check(res, {
      'Notification POST — status 201': (r) => r.status === 201,
      'Notification POST — response < 1000ms': (r) => r.timings.duration < 1000,
      'Notification POST — has id': (r) => {
        try { return JSON.parse(r.body).id !== undefined; } catch (_) { return false; }
      },
    });

    if (res.status === 201) {
      try { createdId = JSON.parse(res.body).id; } catch (_) { /* skip */ }
    }
  });

  // GET: List all notifications (paginated)
  group('Notification - Get All', function () {
    const page = (iter % 3) + 1;
    const res = http.get(`${NOTIFICATION_URL}?page=${page}&pageSize=10`, {
      headers,
      tags: { name: 'GET /api/notification' },
    });
    notificationRequests.add(1);
    notificationDuration.add(res.timings.duration);

    check(res, {
      'Notification GetAll — status 200': (r) => r.status === 200,
      'Notification GetAll — response < 500ms': (r) => r.timings.duration < 500,
      'Notification GetAll — is array': (r) => {
        try { return Array.isArray(JSON.parse(r.body)); } catch (_) { return false; }
      },
    });
  });

  // GET by ID + Preview (only if we successfully created one)
  if (createdId) {
    group('Notification - Get By ID', function () {
      const res = http.get(`${NOTIFICATION_URL}/${createdId}`, {
        headers,
        tags: { name: 'GET /api/notification/{id}' },
      });
      notificationRequests.add(1);
      notificationDuration.add(res.timings.duration);

      check(res, {
        'Notification GetByID — status 200': (r) => r.status === 200,
        'Notification GetByID — has orderId': (r) => {
          try { return JSON.parse(r.body).orderId !== undefined; } catch (_) { return false; }
        },
      });
    });

    group('Notification - Preview', function () {
      const res = http.get(`${NOTIFICATION_URL}/${createdId}/preview`, {
        headers: { ...headers, Accept: 'text/html' },
        tags: { name: 'GET /api/notification/{id}/preview' },
      });
      notificationRequests.add(1);
      notificationDuration.add(res.timings.duration);

      check(res, {
        'Notification Preview — status 200': (r) => r.status === 200,
        'Notification Preview — is HTML': (r) =>
          r.headers['Content-Type'] && r.headers['Content-Type'].includes('text/html'),
        'Notification Preview — body has HTML content': (r) => r.body && r.body.includes('<'),
      });
    });
  }

  paceRequest();
}
