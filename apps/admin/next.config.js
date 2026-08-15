/** @type {import('next').NextConfig} */
const nextConfig = {
  output: "standalone",
  // Served behind nginx at /admin (see infra/nginx/*/server.conf) and the
  // cluster Ingress. The standalone build honors basePath, so assets and app
  // routes are prefixed with /admin automatically.
  basePath: "/admin",
}

module.exports = nextConfig
