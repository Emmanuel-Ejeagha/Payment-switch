# Roles & Authorization (Step 7.5)

Three roles exist: `Admin`, `Merchant`, `Support`. Role names are
**case-insensitive and canonicalized** — assignment, storage, issuance, and
claims checks all normalize (`"admin"`, `" Admin "` → `"Admin"`), so
near-duplicate casings can never split or smuggle privileges.

## Canonicalization rules

| Boundary | Rule |
|---|---|
| Assign (`POST /api/v1/admin/roles`, `AssignRoleCommandValidator`) | Accepts any casing/whitespace of a known role; rejects unknown roles with `Role must be one of: Admin, Merchant, Support.` |
| Store (`User.AddRole`) | Persists the canonical name; adding an existing role (any casing) is a no-op; unknown roles throw |
| Remove (`User.RemoveRole`) | Removes all casings of the role (cleans legacy dupes); unknown roles are a no-op |
| Issue (`TokenService`) | Emits canonical role claims (legacy rows normalized at issuance) |
| Enforce (claims code) | `RoleNames.IsInRole` (case-insensitive); never raw `IsInRole("…")` or ordinal `Contains` |
| Enforce (endpoints) | `[Authorize(Roles = RolePolicies.AdminOnly/ReadOnly)]` constants — never string literals |

Single source: `BuildingBlocks.Shared/Auth/RoleNames.cs` (`RoleNames`,
`RolePolicies`).

## Decision: Support = read-only operations

`Support` was assignable (validator + admin-portal dropdown) but enforced
nowhere — a dead role. Decision: **Support grants read-only operational
visibility, never state transitions, money movement, role grants, or secret
management.** Rationale: support staff need to see merchants, batches,
notifications, and reconciliation reports to help customers, but must not move
money or change trust state. Deleting the role instead would strand existing
assignments and the portal UI; leaving it grant-less would keep a trap.

## Access matrix

| Capability | Admin | Support | Merchant (owner) |
|---|---|---|---|
| Assign roles, suspend/unsuspend users | ✅ | ❌ | ❌ |
| Merchant approve/reject/activate/suspend/reactivate | ✅ | ❌ | ❌ |
| Settlement trigger, reconciliation run | ✅ | ❌ | ❌ |
| Hangfire dashboard, SignalR `admin` group | ✅ | ❌ | ❌ |
| Webhook replay/test, API-key generate/revoke, secret rotation | ✅ (or owner) | ❌ | ✅ (own) |
| List/get merchants, settlement batches, notifications, reconciliation reports | ✅ | ✅ | ❌ (own merchant reads stay owner-scoped) |
| Admin portal login (`apps/admin` requires `Admin` role claim) | ✅ | ❌ (API only for now) | ❌ |

`Merchant` is the default self-service role (every new user; owner-scoped
merchant operations). `Support` is API-level read access today — the admin
portal still gates login on `Admin`; extending portal RBAC to Support views is
future work.

## Granting access

`POST /api/v1/admin/roles` (Admin-only, `Strict` rate-limited) with
`{ "targetUserId": "…", "role": "Support" }` — casing does not matter.
Bootstrap admin comes from the `Seed:AdminEmail/Password` seeder.
