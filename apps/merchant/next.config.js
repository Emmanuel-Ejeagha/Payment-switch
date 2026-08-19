/** @type {import('next').NextConfig} */
const nextConfig = {
  output: "standalone",
  images: {
    // Marketing and auth photography is served from the Unsplash CDN. The
    // <Image> usages pass `unoptimized`, so the browser fetches these directly
    // and the server never needs outbound egress at runtime — but the host
    // still has to be allow-listed for next/image to accept the URL.
    remotePatterns: [
      {
        protocol: "https",
        hostname: "images.unsplash.com",
        pathname: "/**",
      },
    ],
  },
}

module.exports = nextConfig
