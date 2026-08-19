import { check, sleep } from 'k6';
import { login } from './lib/auth.js';
import { createConfirmCapture } from './lib/payment-flow.js';

/**
 * Load test for the create-intent -> confirm -> capture payment path.
 *
 * Target: ramp to 20 concurrent VUs over 1 minute, hold for 3 minutes, ramp down.
 * Each VU issues create + confirm + capture back-to-back, so sustained success at
 * this rate validates the throughput claim in docs/load-testing.md.
 *
 * Run:
 *   PAYMENT_BASE_URL=http://localhost:8080 \
 *   MERCHANT_BASE_URL=http://localhost:8080 \
 *   API_KEY=sk_test_... MERCHANT_EMAIL=... MERCHANT_PASSWORD=... \
 *   k6 run tests/load/create-intent-capture.js
 */

export const options = {
  scenarios: {
    create_confirm_capture: {
      executor: 'ramping-vus',
      startVUs: 1,
      stages: [
        { duration: '1m', target: 20 },
        { duration: '3m', target: 20 },
        { duration: '1m', target: 0 },
      ],
      gracefulRampDown: '30s',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<500', 'p(99)<1000'],
  },
};

export function setup() {
  return { token: login() };
}

export default function (data) {
  const r = createConfirmCapture(data.token);

  check(r, {
    'create intent returns 200': (x) => x.createStatus === 200,
    'confirm returns 200': (x) => x.confirmStatus === 200,
    'capture returns 200': (x) => x.captureStatus === 200,
  });

  sleep(0.5);
}
