import { expect, test } from "@playwright/test"

const email = process.env.E2E_EMAIL
const password = process.env.E2E_PASSWORD

test("rejects invalid credentials", async ({ page }) => {
  await page.goto("/login")

  await page.getByLabel(/Email/).fill("nobody@example.com")
  await page.getByLabel(/Password/).fill("wrong-password")
  await page.getByRole("button", { name: /Sign in|Log in|Login/i }).click()

  await expect(page.getByText(/Invalid|Incorrect|failed|error/i).first()).toBeVisible({
    timeout: 10_000,
  })
})

// Requires a seed merchant owner (see docs/frontend-tests.md). Skipped unless
// E2E_EMAIL and E2E_PASSWORD are provided so the suite can run without secrets.
test.skip(!email || !password, "seeded owner credentials required (E2E_EMAIL/E2E_PASSWORD)")

test("logs in with valid credentials and lands on the dashboard", async ({ page }) => {
  await page.goto("/login")

  await page.getByLabel(/Email/).fill(email!)
  await page.getByLabel(/Password/).fill(password!)
  await page.getByRole("button", { name: /Sign in|Log in|Login/i }).click()

  await expect(page).toHaveURL(/\/dashboard/, { timeout: 15_000 })
  await expect(page.getByRole("main")).toBeVisible()
})