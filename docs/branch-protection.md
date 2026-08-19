# Branch Protection & PR Gates

`main` is the only branch that deploys to production: the CI/CD pipeline
(`.github/workflows/ci-cd.yml`) runs the `deploy` job only on pushes to
`refs/heads/main`, and that job depends on the Docker jobs, which in turn depend
on the test jobs. **Direct pushes to `main` must therefore be impossible**; every
change to `main` must land via a reviewed pull request whose required status
checks are green. This document defines exactly what to enforce and how.

## Current status (in-repo)

- CODEOWNERS for PR review: `.github/CODEOWNERS` (owner: `@Emmanuel-Ejeagha`).
- Required status checks already exist as GitHub Actions jobs; these are the
  check contexts to require on `main`:

  | Check context (job `name`) | Runs on PRs to `main`? | What it gates |
  |---|---|---|
  | `Build & Test` | yes | `dotnet format`, Release build, all unit suites + coverage |
  | `Integration Tests` | yes | Testcontainers integration suites (Identity/Merchant/Payment/Ledger/Notification/Settlement) |
  | `Frontend Build & Lint` | yes | npm audit + typecheck + lint + build for `admin`/`merchant` |

- Optional-but-recommended (`.github/workflows/security.yml`): `CodeQL`,
  `Secret Scanning`. Requiring them slows PRs slightly but prevents a reviewed
  PR from merging with a known vuln.

## Rules to apply on GitHub for `main`

1. **Require a pull request before merging** — blocks direct pushes for
   non-admin users.
2. **Enforce for administrators** (`enforce_admins`) — same rules apply to
   owners/admins; without this a repo owner can bypass every gate.
3. **Required approving reviews: 1** and **require review from Code Owners**
   (uses `.github/CODEOWNERS`). **Dismiss stale reviews** on new commits.
4. **Required status checks** (strict: branch must be up to date): the three
   table rows above; add `CodeQL` and `Secret Scanning` if you accept the
   latency cost.
5. **Require linear history**, **no force pushes**, **no deletions**.
6. Consider the same ruleset on `develop` (with the same checks) so that the
   shared integration branch gets the same quality bar.

## Applying the rules

### Option A — GitHub UI

`Settings → Branches → Add branch protection rule` (classic) or
`Settings → Rules → New ruleset` (rulesets). The exact toggles for classic mode:

- **Protect matching branches**: `main`
- **Require a pull request before merging** ✓ (1 approving review)
- **Require approvals from the CODEOWNERS** ✓
- **Dismiss stale pull request approvals when new commits are pushed** ✓
- **Require status checks to pass before merging** ✓ — add `Build & Test`,
  `Integration Tests`, `Frontend Build & Lint` (all "required")
- **Require branches to be up to date before merging** ✓
- **Require linear history** ✓
- **Do not allow force pushes** ✓, **Do not allow deletions** ✓
- **Do not allow bypassing the above settings** (enforce admins) ✓

### Option B — GitHub CLI (classic protection API)

```bash
cat > /tmp/branch-protection.json <<'EOF'
{
  "required_status_checks": {
    "strict": true,
    "contexts": [
      "Build & Test",
      "Integration Tests",
      "Frontend Build & Lint"
    ]
  },
  "enforce_admins": true,
  "required_pull_request_reviews": {
    "required_approving_review_count": 1,
    "dismiss_stale_reviews": true,
    "require_code_owner_reviews": true
  },
  "restrictions": null,
  "allow_force_pushes": false,
  "allow_deletions": false,
  "required_linear_history": true
}
EOF

gh api -X PUT repos/:owner/:repo/branches/main/protection \
  --input /tmp/branch-protection.json
```

Verify afterwards:

```bash
gh api repos/:owner/:repo/branches/main/protection --jq \
  '{required_checks: [.required_status_checks.contexts[]], reviews: .required_pull_request_reviews, enforce_admins: .enforce_admins}'
```

## Enforcement behavior

- A PR whose required checks have not completed or failed **cannot be merged**
  (merge button is disabled / the merge is rejected).
- A force-push or direct push to `main` is rejected at the ref-update level,
  even by admins.
- The `deploy` job only fires on `refs/heads/main` pushes, so every deployment
  is now provably downstream of a reviewed, green PR.
