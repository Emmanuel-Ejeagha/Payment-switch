import nextConfig from "eslint-config-next"

// `next lint` was removed in Next.js 16; ESLint is invoked directly against this
// flat config instead. See the `lint` script in package.json.
const config = [
  ...nextConfig,
  {
    ignores: [".next/**", "node_modules/**", "next-env.d.ts"],
  },
]

export default config
