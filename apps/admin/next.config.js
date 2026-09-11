/** @type {import('next').NextConfig} */
const nextConfig = {
  output: "standalone",
  // Served behind nginx at /admin (see infra/nginx/*/server.conf) and the
  // cluster Ingress. The standalone build honors basePath, so assets and app
  // routes are prefixed with /admin automatically.
  basePath: "/admin",
  async redirects() {
    return [
      // Visiting the bare dev/admin origin (http://localhost:3001/) lands on
      // /admin. basePath:false keeps the source matching the real root rather
      // than /admin/.
      { source: "/", destination: "/admin", basePath: false, permanent: false },
      // The bare /admin root never reaches middleware (it strips to an empty
      // remainder the matcher can't match), so gate it here: no session cookie
      // goes straight to login instead of flashing the dashboard shell.
      // Sessions with cookies fall through to middleware + client refresh.
      // NOTE: destination is basePath-relative here (basePath is enabled for
      // this rule), so "/login" becomes "/admin/login" — do not prefix it.
      {
        source: "/",
        destination: "/login",
        permanent: false,
        missing: [{ type: "cookie", key: "access_token" }],
      },
    ]
  },
}

module.exports = nextConfig
