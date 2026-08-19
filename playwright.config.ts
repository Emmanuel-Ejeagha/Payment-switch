import { defineConfig, devices } from "@playwright/test"

export default defineConfig({
  testDir: "./e2e/tests",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  reporter: [["list"]],
  use: {
    trace: "on-first-retry",
  },
  projects: [
    {
      name: "merchant",
      use: {
        ...devices["Desktop Chrome"],
        baseURL: process.env.MERCHANT_URL ?? "http://localhost:3000",
      },
      testMatch: /merchant-.*\.spec\.ts|checkout\.spec\.ts/,
    },
    {
      name: "admin",
      use: {
        ...devices["Desktop Chrome"],
        baseURL: process.env.ADMIN_URL ?? "http://localhost:3001",
      },
      testMatch: /admin-.*\.spec\.ts/,
    },
  ],
})