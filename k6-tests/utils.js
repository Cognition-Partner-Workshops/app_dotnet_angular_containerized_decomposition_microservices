/**
 * Shared utility helpers for K6 test scripts.
 */
import http from 'k6/http';
import { check } from 'k6';
import encoding from 'k6/encoding';
import {
  AUTH_USERNAME,
  AUTH_PASSWORD,
  AUTH_TOKEN_URL,
  AUTH_CLIENT_ID,
  AUTH_CLIENT_SECRET,
} from './config.js';

// ---------------------------------------------------------------------------
// Header builders
// ---------------------------------------------------------------------------

/**
 * Returns common JSON request headers with an optional Bearer token.
 */
export function jsonHeaders(token) {
  const headers = { 'Content-Type': 'application/json', Accept: 'application/json' };
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }
  return headers;
}

/**
 * Returns headers configured for Basic authentication.
 */
export function basicAuthHeaders() {
  const encoded = encoding.b64encode(`${AUTH_USERNAME}:${AUTH_PASSWORD}`);
  return {
    'Content-Type': 'application/json',
    Accept: 'application/json',
    Authorization: `Basic ${encoded}`,
  };
}

// ---------------------------------------------------------------------------
// Authentication helpers (used inside setup())
// ---------------------------------------------------------------------------

/**
 * Retrieve a Bearer token via client-credentials or password grant.
 * Returns the token string, or null when the token endpoint is unavailable.
 */
export function fetchBearerToken() {
  const payload = {
    grant_type: 'client_credentials',
    client_id: AUTH_CLIENT_ID,
    client_secret: AUTH_CLIENT_SECRET,
    username: AUTH_USERNAME,
    password: AUTH_PASSWORD,
  };

  const res = http.post(AUTH_TOKEN_URL, JSON.stringify(payload), {
    headers: { 'Content-Type': 'application/json' },
    tags: { name: 'auth_token' },
  });

  const ok = check(res, {
    'token request succeeded': (r) => r.status === 200,
  });

  if (ok) {
    try {
      const body = JSON.parse(res.body);
      return body.access_token || body.token || null;
    } catch (_) {
      return null;
    }
  }
  return null;
}

// ---------------------------------------------------------------------------
// Common response checks
// ---------------------------------------------------------------------------

/**
 * Standard checks for a successful GET response.
 */
export function checkGetResponse(res, label) {
  return check(res, {
    [`${label} — status is 200`]: (r) => r.status === 200,
    [`${label} — response time < 500ms`]: (r) => r.timings.duration < 500,
    [`${label} — body is not empty`]: (r) => r.body && r.body.length > 0,
  });
}

/**
 * Standard checks for a successful POST (201 Created) response.
 */
export function checkPostResponse(res, label) {
  return check(res, {
    [`${label} — status is 201`]: (r) => r.status === 201,
    [`${label} — response time < 1000ms`]: (r) => r.timings.duration < 1000,
    [`${label} — body is not empty`]: (r) => r.body && r.body.length > 0,
  });
}
