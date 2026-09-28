# syntax=docker/dockerfile:1

# ---- Stage 1: install workspace dependencies ----
FROM node:22-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json* ./
COPY packages/shared/package.json packages/shared/package.json
COPY packages/ui/package.json packages/ui/package.json
COPY apps/merchant/package.json apps/merchant/package.json
RUN npm ci

# ---- Stage 2: build the merchant app (standalone) ----
FROM node:22-alpine AS builder
WORKDIR /app
ARG NEXT_PUBLIC_API_URL
ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL
# Root package.json declares the npm workspaces; Turbopack needs it to resolve
# the workspace root and hoisted node_modules.
COPY package.json package-lock.json* ./
COPY --from=deps /app/node_modules ./node_modules
COPY packages packages
COPY apps/merchant apps/merchant
RUN cd apps/merchant && npm run build
# Ensure a public dir exists even though these apps ship no static assets.
RUN mkdir -p apps/merchant/public

# ---- Stage 3: run standalone server ----
FROM node:22-alpine AS runner
WORKDIR /app
ENV NODE_ENV=production
ENV PORT=3000
ENV HOSTNAME=0.0.0.0
RUN addgroup --system --gid 1001 nodejs && adduser --system --uid 1001 nextjs
COPY --from=builder /app/apps/merchant/.next/standalone ./
COPY --from=builder /app/apps/merchant/.next/static ./apps/merchant/.next/static
COPY --from=builder /app/apps/merchant/public ./apps/merchant/public
USER nextjs
EXPOSE 3000
CMD ["node", "apps/merchant/server.js"]