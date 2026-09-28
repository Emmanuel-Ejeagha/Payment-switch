import http from 'k6/http';

/**
 * Identity login used by the k6 setup() stage to obtain a merchant JWT. The same
 * token authorizes the merchant dashboard capture endpoint. The merchant must be
 * registered + email-verified + approved + activated before the load run (see
 * docs/load-testing.md "Provisioning").
 */

const MERCHANT_BASE_URL = __ENV.MERCHANT_BASE_URL || 'http://localhost:8080';

export function login() {
  const res = http.post(
    `${MERCHANT_BASE_URL}/api/v1/auth/login`,
    JSON.stringify({
      email: __ENV.MERCHANT_EMAIL || '',
      password: __ENV.MERCHANT_PASSWORD || '',
    }),
    { headers: { 'Content-Type': 'application/json' } },
  );

  if (res.status !== 200) {
    throw new Error(`login failed: HTTP ${res.status} ${res.body}`);
  }

  return res.json('accessToken');
}
