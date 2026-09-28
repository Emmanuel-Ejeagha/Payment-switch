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
    ]
  },
}

module.exports = nextConfig
