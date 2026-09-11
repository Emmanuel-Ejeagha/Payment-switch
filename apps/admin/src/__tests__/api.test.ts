import { describe, expect, it } from "vitest"
import { BASE_PATH, apiUrl } from "@/lib/api"

// The admin app runs under basePath `/admin`, so every same-origin fetch
// (proxy routes, auth routes, middleware redirects) must carry the prefix or
// the dev server 404s. This locks that contract.
describe("apiUrl", () => {
  it("prefixes client paths with the admin basePath", () => {
    expect(BASE_PATH).toBe("/admin")
    expect(apiUrl("/api/auth/login")).toBe("/admin/api/auth/login")
    expect(apiUrl("/api/auth/token")).toBe("/admin/api/auth/token")
    expect(apiUrl("/api/auth/logout")).toBe("/admin/api/auth/logout")
    expect(apiUrl("/api/proxy/merchant/api/v1/merchants")).toBe(
      "/admin/api/proxy/merchant/api/v1/merchants"
    )
    expect(apiUrl("/login")).toBe("/admin/login")
    expect(apiUrl("/")).toBe("/admin/")
  })
})
