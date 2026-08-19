import { expect, test } from "@playwright/test"

const checkoutUrl = process.env.E2E_CHECKOUT_URL

// Needs a live hosted-checkout page (a payment link the merchant created).
// Skipped unless E2E_CHECKOUT_URL is provided so the suite can run without a
// provisioned payment link (see docs/frontend-tests.md).
test.skip(!checkoutUrl, "E2E_CHECKOUT_URL required (a live payment-link checkout page)")

test("completes a card checkout with the test card", async ({ page }) => {
  await page.goto(checkoutUrl!)

  await page.getByLabel(/Card number|Card/i).fill("4242424242424242")
  await page.getByLabel(/Expiry|MM\/YY/i).fill("12/30")
  await page.getByLabel(/CVC|CVC\/CVV/i).fill("123")
  await page.getByRole("button", { name: /Pay/i }).click()

  await expect(page.getByText(/success|complete|thank you/i).first()).toBeVisible({
    timeout: 15_000,
  })
})