import { check, sleep } from 'k6';
import { login } from './lib/auth.js';
import { createConfirmCapture } from './lib/payment-flow.js';

/**
 * Soak test: sustain a modest load (10 concurrent VUs) for 30 minutes to expose
 * slow leaks (connections, message accumulation, memory) that short load runs
 * miss. The assertion is stability: http_req_duration p(99) must stay under the
 * threshold for the whole run, and the error rate must stay ~0.
 *
 * Run:
 *   PAYMENT_BASE_URL=http://localhost:8080 \
 *   MERCHANT_BASE_URL=http://localhost:8080 \
 *   API_KEY=sk_test_... MERCHANT_EMAIL=... MERCHANT_PASSWORD=... \
 *   k6 run tests/load/soak.js
 */

export const options = {
  scenarios: {
    soak: {
      executor: 'constant-vus',
      vus: 10,
      duration: '30m',
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

  sleep(1);
}
