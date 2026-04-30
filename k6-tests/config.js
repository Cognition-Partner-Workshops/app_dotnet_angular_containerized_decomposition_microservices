/**
 * Shared configuration for all K6 performance test scripts.
 *
 * Environment variables (pass via -e flag or __ENV):
 *   BASE_URL        – API Gateway root (default http://localhost:5000)
 *   AUTH_USERNAME    – Basic-auth username  (default "admin")
 *   AUTH_PASSWORD    – Basic-auth password  (default "password")
 *   AUTH_TOKEN_URL   – OAuth / token endpoint for Bearer auth
 *   AUTH_CLIENT_ID   – OAuth client id
 *   AUTH_CLIENT_SECRET – OAuth client secret
 *   VUS             – override default virtual users
 *   DURATION        – override default duration
 */

// ---------------------------------------------------------------------------
// Service base URLs – routed through the API Gateway (port 5000) by default.
// Override with direct service URLs when testing individual services.
// ---------------------------------------------------------------------------
export const BASE_URL           = __ENV.BASE_URL           || 'http://localhost:5000';
export const IDENTITY_URL       = __ENV.IDENTITY_URL       || `${BASE_URL}/api/identity`;
export const CUSTOMER_URL       = __ENV.CUSTOMER_URL       || `${BASE_URL}/api/customer`;
export const ORDER_URL          = __ENV.ORDER_URL          || `${BASE_URL}/api/order`;
export const PRODUCT_URL        = __ENV.PRODUCT_URL        || `${BASE_URL}/api/product`;
export const NOTIFICATION_URL   = __ENV.NOTIFICATION_URL   || `${BASE_URL}/api/notification`;

// ---------------------------------------------------------------------------
// Authentication
// ---------------------------------------------------------------------------
export const AUTH_USERNAME       = __ENV.AUTH_USERNAME       || 'admin';
export const AUTH_PASSWORD       = __ENV.AUTH_PASSWORD       || 'password';
export const AUTH_TOKEN_URL      = __ENV.AUTH_TOKEN_URL      || `${BASE_URL}/api/identity/token`;
export const AUTH_CLIENT_ID      = __ENV.AUTH_CLIENT_ID      || '';
export const AUTH_CLIENT_SECRET  = __ENV.AUTH_CLIENT_SECRET  || '';

// ---------------------------------------------------------------------------
// Default load-test options (can be imported and merged per-script)
// ---------------------------------------------------------------------------
export const DEFAULT_STAGES = [
  { duration: '30s', target: 10 },   // ramp-up
  { duration: '1m',  target: 10 },   // steady state
  { duration: '30s', target: 20 },   // spike
  { duration: '1m',  target: 20 },   // sustained spike
  { duration: '30s', target: 0 },    // ramp-down
];

export const DEFAULT_THRESHOLDS = {
  http_req_duration: ['p(95)<500', 'p(99)<1000'],   // 95th < 500 ms, 99th < 1 s
  http_req_failed:   ['rate<0.05'],                  // < 5 % error rate
  checks:            ['rate>0.95'],                  // > 95 % of checks pass
};

export const DEFAULT_OPTIONS = {
  stages:     DEFAULT_STAGES,
  thresholds: DEFAULT_THRESHOLDS,
  noConnectionReuse: false,
  userAgent: 'K6-PerformanceTest/1.0',
};
