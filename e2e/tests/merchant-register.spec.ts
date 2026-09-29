import { expect, test } from "@playwright/test"

test("registers a new merchant and is directed to email verification", async ({ page }) => {
  const email = `e2e-${Date.now()}@example.com`

  await page.goto("/register")

  await page.getByLabel(/Business name/i).fill("E2E Test Ltd.")
  await page.getByLabel(/Email/i).fill(email)
  await page.getByLabel(/Password/i).fill("E2e-password-123")
  await page.getByLabel(/Confirm password|Repeat password/i).fill("E2e-password-123")
  await page.getByRole("button", { name: /Create account|Sign up|Register/i }).click()

  await expect(page.getByText(/verify your email|check your inbox|verification email/i).first()).toBeVisible({
    timeout: 15_000,
  })
})