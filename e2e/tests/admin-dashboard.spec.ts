import { expect, test } from "@playwright/test"

const email = process.env.E2E_ADMIN_EMAIL
const password = process.env.E2E_ADMIN_PASSWORD

// Requires the seeded admin (see docs/frontend-tests.md). Skipped unless
// E2E_ADMIN_EMAIL and E2E_ADMIN_PASSWORD are provided.
test.skip(!email || !password, "admin credentials required (E2E_ADMIN_EMAIL/E2E_ADMIN_PASSWORD)")

test("admin logs in and reaches the dashboard overview", async ({ page }) => {
  await page.goto("/login")

  await page.getByLabel(/Email/).fill(email!)
  await page.getByLabel(/Password/).fill(password!)
  await page.getByRole("button", { name: /Sign in|Log in|Login/i }).click()

  await expect(page).toHaveURL(/\/dashboard|\/$/, { timeout: 15_000 })
  await expect(page.getByText(/Merchants|Overview|Dashboard/i).first()).toBeVisible({
    timeout: 15_000,
  })
})