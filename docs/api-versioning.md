# API Versioning Contract

This page is the authoritative versioning contract for all HTTP APIs in this
repository. It supersedes ad-hoc `[ApiVersion("1.0")]` usage and answers three
questions for every API surface: **how** versioning works, **when** a version is
bumped, and **how** a version is deprecated or removed.

The behavior described below is enforced by the shared versioning setup
(`src/BuildingBlocks/BuildingBlocks.Shared/Versioning/VersioningExtensions.cs`)
and verified by `VersioningBehaviorTests`
(`tests/Integration/Payment.API.IntegrationTests/VersioningBehaviorTests.cs`).

## 1. Versioning scheme

APIs are versioned with a **URL path segment** (`UrlSegmentApiVersionReader`):

- **Internal/back-office APIs** (Identity, Merchant, Payment, Ledger,
  Notification, Settlement) inherit the route template
  `api/v{version:apiVersion}/[controller]` from `BaseApiController`, e.g.
  `POST /api/v1/payments`.
- **Public merchant-facing APIs** hard-code the segment in the controller route,
  e.g. `v1/payments` (`PublicPaymentsController`).

A version segment is `v{major}` or `v{major}.{minor}` (`v1`, `v1.1`). Swagger
groups by `v{major}` (`GroupNameFormat = "'v'VVV"`).

**Current supported version: `1.0`.**

## 2. Version selection and enforcement

| Request | Behavior |
|---|---|
| `POST /api/v1/payments` | Route resolves; auth runs (401 without credentials) |
| `POST /api/payments` (no segment) | **404** — the version segment is mandatory |
| `POST /api/v2/payments` (unknown major) | **404** — no resource at that version |
| `POST /api/v1.1/payments` (unsupported minor) | **404** — no resource at that version |
| `POST /api/v1.2.3/payments` (malformed) | **404** — malformed segment matches no route |

- Successful (2xx) responses from a versioned action include the
  `api-supported-versions` header (e.g. `1.0`) because
  `ReportApiVersions = true`. Unauthorized/error responses short-circuit before
  the versioning response filter and do not include it.
- `AssumeDefaultVersionWhenUnspecified = true` is retained for future
  header/query readers; it does **not** relax the URL-segment requirement above.
- Clients must always send an explicit, currently-supported version segment.
  Treat any 404 at a version other than the current one as "that version does
  not exist" — not as a typo to be retried silently.

## 3. When to bump

- **Patch** (no segment change, same `v1`): bug fixes and non-breaking
  internal changes. No API version change.
- **Minor** (`v1` → `v1.1`): purely additive changes — new endpoints, new
  optional fields, new response members. Old requests continue to work
  unchanged; new clients can opt in via `v1.1`.
- **Major** (`v1` → `v2`): any breaking change — removing/renaming a field,
  changing a type or status-code contract, changing auth requirements, or
  changing pagination semantics. New behavior ships under `v2`; the old surface
  keeps serving `v1` until the deprecation window (below) closes.

## 4. Deprecation policy

1. Announce intent in the changelog and the API docs **at least one minor
   release before** deprecation takes effect.
2. Mark the version deprecated on the controller:
   `[ApiVersion("1.0", Deprecated = true)]`. Asp.Versioning then emits
   `api-deprecated-versions` alongside `api-supported-versions`.
3. Minimum support window once deprecated: **the current version plus the
   previous major's most recent minor** (N-1), or **12 months**, whichever is
   longer.
4. Deprecated versions return normal responses (they are not error-gated), but
   the `api-deprecated-versions` header must be visible so clients are
   informed on every call.

## 5. Removal policy

1. A version is removed only by shipping a **new major** that drops it — never
   mid-major.
2. Removal happens only **after** the deprecation window from §4 has elapsed.
3. The removal is announced at least **two releases in advance** (changelog +
   this document) so clients can migrate before the 404s start.
4. After removal the route returns **404** (consistent with §2) — no
   compatibility shim.

## 6. Adding a new version

For a new major `v2`:

1. Create new controllers with `[ApiVersion("2.0")]` (or annotate shared
   controllers with `[MapToApiVersion("2.0")]`).
2. Keep `v1` controllers in place until the §4/§5 windows close — never
   repurpose `v1` routes to `v2` behavior.
3. Update this document and `docs/ROADMAP.md` with the new current version and
   the deprecation/removal dates for the old one.

## 7. Non-compliance

Any controller that adds a route **without** `[ApiVersion(...)]` and outside a
`v{version}` segment is a defect: it would be unversioned and unmanaged.
Code review (see `docs/branch-protection.md`) should reject it.
