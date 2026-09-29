---
status: current
---

# docs/ index

Every file here now starts with a small YAML frontmatter block (`status: current` /
`active` / `partially-stale` / `stale`) so its freshness is visible without reading the
whole document. This index groups them by that status. The two canonical, always-current
references for this project are **not** in this folder — they live at the repo root:
`api.txt` (mobile/client API contract) and `db.txt` (live DB schema).

## Current — living references, safe to trust as-is

- **[flutter-changes.md](flutter-changes.md)** — running checklist of backend changes the
  Flutter app needs to adopt. Updated incrementally as features land.
- **[status-workflow.md](status-workflow.md)** — design reference for
  `CustomerServiceRequests.Status` / `ServiceBookings.Status`, implemented and current.
- **[PRIVACY_POLICY.md](PRIVACY_POLICY.md)** — legal document (last updated 2026-09-08).

## Active — old but still accurate/unresolved

- **[auth-gap-report.md](auth-gap-report.md)** — security audit from 2026-08-27. Re-verified
  2026-09-29: its critical findings (plaintext passwords, no JWT issuance) are still true in
  the current code. Not stale — an open issue, not a historical record.

## Partially stale — verify before trusting

- **[gps-maps-implementation.md](gps-maps-implementation.md)** — Phase 1/2 (backend + admin
  web test harness) sections are accurate history; the Phase 3 "not started" framing may be
  outdated now that GPS fields ship on other endpoints. Check `api.txt` first.

## Stale — historical record only, do not use as current documentation

- **[api-audit-report.md](api-audit-report.md)** — audit against `api.txt` v1.5 / `db.txt`
  v1.9, long superseded. Per standing project instruction, this file is not updated for new
  endpoint work.
- **[provider-workflow-test-report.md](provider-workflow-test-report.md)** — one-off manual
  test session, 2026-08-04.
- **[payment-module-test-report.md](payment-module-test-report.md)** — one-off manual test
  session, 2026-08-04.
- **[flutter-integration-plan.md](flutter-integration-plan.md)** — one-time integration task
  list for a specific backend phase, 2026-08-04; expected long since adopted.
- **[feature-plan-multi-provider-notifications-billing-categories.md](feature-plan-multi-provider-notifications-billing-categories.md)** —
  implementation plan from 2026-09-22; all four described features have since shipped.

## Maintaining this index

When adding a new doc to this folder, give it a `status` frontmatter field and add one line
here. When a doc's findings get resolved or its plan fully ships, flip its `status` to `stale`
(with a `reason` and `superseded_by`) and move its line into the Stale section above, rather
than deleting it — these are historical records of what was investigated/built and why.
