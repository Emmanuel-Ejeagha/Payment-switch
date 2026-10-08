# syntax=docker/dockerfile:1

# ---- Stage 1: install workspace dependencies ----
FROM node:22-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json* ./
COPY packages/shared/package.json packages/shared/package.json
COPY packages/ui/package.json packages/ui/package.json
COPY apps/admin/package.json apps/admin/package.json
RUN npm ci

# ---- Stage 2: build the admin app (standalone) ----
FROM node:22-alpine AS builder
WORKDIR /app
ARG NEXT_PUBLIC_API_URL
ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL
# Root package.json declares the npm workspaces; Turbopack needs it to resolve
# the workspace root and hoisted node_modules.
COPY package.json package-lock.json* ./
COPY --from=deps /app/node_modules ./node_modules
COPY packages packages
COPY apps/admin apps/admin
# Dev-only workspace deps (vitest tooling, etc.) nest under the workspace dir
# rather than hoisting; carry them over so `next build` type-checking resolves
# exactly like a local install. Runtime is unaffected (standalone output).
COPY --from=deps /app/apps/admin/node_modules ./apps/admin/node_modules
RUN cd apps/admin && npm run build
# Ensure a public dir exists even though these apps ship no static assets.
RUN mkdir -p apps/admin/public

# ---- Stage 3: run standalone server ----
FROM node:22-alpine AS runner
WORKDIR /app
ENV NODE_ENV=production
ENV PORT=3001
ENV HOSTNAME=0.0.0.0
RUN addgroup --system --gid 1001 nodejs && adduser --system --uid 1001 nextjs
# The node:22-alpine base ships a bundled npm whose transitive deps carry
# HIGH/CRITICAL advisories (picomatch, brace-expansion, pacote, ...). The
# runtime only ever runs `node server.js`, so drop npm entirely: smaller
# image and no dead vulnerable surface for Trivy to flag.
RUN rm -rf /usr/local/lib/node_modules/npm /usr/local/bin/npm /usr/local/bin/npx
# Pull latest patched Alpine packages at build time so image scans stay clean
# on fixed CVEs without waiting for a new node base tag.
RUN apk upgrade --no-cache
COPY --from=builder /app/apps/admin/.next/standalone ./
COPY --from=builder /app/apps/admin/.next/static ./apps/admin/.next/static
COPY --from=builder /app/apps/admin/public ./apps/admin/public
USER nextjs
EXPOSE 3001
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD wget -qO- http://localhost:3001/api/health || exit 1
CMD ["node", "apps/admin/server.js"]