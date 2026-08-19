import http from 'k6/http';

/**
 * Shared create-intent -> confirm -> capture scenario for the Payment API.
 *
 * - create + confirm use the public payments API (secret-key auth + required
 *   Idempotency-Key header), capture uses the merchant dashboard API (JWT auth).
 * - Card "4242" is the mock gateways' clean-authorize test card, so confirm
 *   always returns 200 and capture always succeeds. To simulate the 3DS
 *   RequiresAction branch, set CARD_LAST_FOUR=3001 (see RETENTION-independent
 *   mock gateway docs).
 */

const PAYMENT_BASE_URL = __ENV.PAYMENT_BASE_URL || 'http://localhost:8080';
const API_KEY = __ENV.API_KEY || '';
const AMOUNT = Number(__ENV.PAYMENT_AMOUNT || 10000);
const CURRENCY = __ENV.PAYMENT_CURRENCY || 'USD';
const CARD_LAST_FOUR = __ENV.CARD_LAST_FOUR || '4242';
const CARD_BRAND = __ENV.CARD_BRAND || 'Visa';

export function paymentBaseUrl() {
  return PAYMENT_BASE_URL;
}

/**
 * Runs one full payment: create -> confirm -> capture.
 * @param {string} merchantToken JWT (identity login) used to authorize capture.
 * @returns {{createStatus: number, confirmStatus: number, captureStatus: number}}
 */
export function createConfirmCapture(merchantToken) {
  const key = idempotencyKey();
  const publicHeaders = {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${API_KEY}`,
    'Idempotency-Key': key,
  };

  const create = http.post(
    `${PAYMENT_BASE_URL}/v1/payments/intents`,
    JSON.stringify({
      amount: AMOUNT,
      currency: CURRENCY,
      paymentMethod: 'Card',
      cardLastFour: CARD_LAST_FOUR,
      cardBrand: CARD_BRAND,
    }),
    { headers: publicHeaders, tags: { flow: 'create' } },
  );
  if (create.status !== 200) {
    return { createStatus: create.status };
  }

  const intentId = create.json('intentId');

  const confirm = http.post(
    `${PAYMENT_BASE_URL}/v1/payments/${intentId}/confirm`,
    '{}',
    { headers: publicHeaders, tags: { flow: 'confirm' } },
  );

  const capture = http.post(
    `${PAYMENT_BASE_URL}/api/v1/payments/${intentId}/capture`,
    '{}',
    {
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${merchantToken}` },
      tags: { flow: 'capture' },
    },
  );

  return {
    createStatus: create.status,
    confirmStatus: confirm.status,
    captureStatus: capture.status,
  };
}

function idempotencyKey() {
  return `k6-${__VU}-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
}
