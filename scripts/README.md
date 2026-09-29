# scripts/ index

This folder mixes reusable dev tooling with one-off SQL migration scripts that already ran
against the live database. Each script now carries a `STATUS:` line in its header comment;
this file groups them so freshness is visible without opening each one.

## Active — reusable, still used

- **[dev-run.ps1](dev-run.ps1)** — local dev entrypoint (`dotnet run`). Start here.
- **[test-apis.ps1](test-apis.ps1)** — smoke-tests documented REST APIs against a running
  instance.
- **[test-customer-service-requests-optional-preferred.ps1](test-customer-service-requests-optional-preferred.ps1)** —
  smoke-test script for the optional preferred-date/time fields (see AGENTS.md).
- **[seed-service-requests.sql](seed-service-requests.sql)** — reusable dev-data seeder (10
  `CustomerServiceRequests` per active category). Safe to re-run against a dev DB.

## Disabled — kept for reference, not currently used

- **[dev-sql-tunnel.ps1](dev-sql-tunnel.ps1)** / **[ensure-sql-tunnel.ps1](ensure-sql-tunnel.ps1)** —
  Hostinger SSH SQL tunnel scripts. The app now connects directly to SQL Server, so both are
  no-ops (self-declared `DISABLED` at the top of each file). Only relevant again if the
  Hostinger tunnel setup is revived — see `AGENTS.md`.

## Applied — one-off SQL migrations, already run against the live DB

These already executed against production and their target state matches `db.txt` today.
They're idempotent (safe to re-run, they no-op if the target already exists) but there's
nothing left for them to do — historical record of how the schema got here, not a pending
task list.

- **[create-provider-documents.sql](create-provider-documents.sql)** — created `ProviderDocuments`.
- **[alter-provider-documents-verification.sql](alter-provider-documents-verification.sql)** —
  added `VerifiedOn`/`VerifiedBy`/`VerificationRemarks` to `ProviderDocuments`.
- **[create-admin-notifications.sql](create-admin-notifications.sql)** — created `AdminNotifications`.
- **[rename-request-status-initiated.sql](rename-request-status-initiated.sql)** — renamed the
  `CustomerServiceRequests` pre-assignment status `Pending` → `Initiated` (2026-09-07).
- **[add-clients-alert-comments.sql](add-clients-alert-comments.sql)** — added `Clients.CustomerAlert`/`Comments`.
- **[add-clients-city-location.sql](add-clients-city-location.sql)** — added `Clients.City`/`Location`.
- **[add-providers-city.sql](add-providers-city.sql)** — added `Providers.City`.
- **[create-configurations.sql](create-configurations.sql)** — created and seeded `Configurations`.

## Stale — do not run against the current database

- **[migrate-service-providers-to-provider-profiles.sql](migrate-service-providers-to-provider-profiles.sql)** —
  targets `ServiceProviders`/`ProviderProfiles`, both marked `[REMOVED]` in `db.txt` today.
  Superseded by the live `Providers`/`Clients` schema. Kept for historical reference only.

## Maintaining this index

New one-off migration script → add a `STATUS: APPLIED` (or `STALE`, if superseded before ever
running) line to its header comment once you've confirmed it ran, and add one line here under
the matching section. Reusable tooling (smoke tests, seeders, dev entrypoints) stays under
Active as long as it's still used.
