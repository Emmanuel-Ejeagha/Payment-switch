/**
 * Admin API client helper.
 *
 * The admin app runs under basePath `/admin` (see next.config.js), so its
 * proxy route handlers are mounted at `/admin/api/proxy/...`. Browser fetches
 * must include that prefix or the dev server 404s. Keep BASE_PATH in sync with
 * basePath in next.config.js.
 */
export const BASE_PATH = "/admin"

export function apiUrl(path: string): string {
  return `${BASE_PATH}${path}`
}