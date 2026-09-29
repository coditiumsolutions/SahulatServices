# deploy/ index

Two deploy targets exist side by side. Each script now carries a `STATUS:` line in its header
comment; this file groups them so it's clear at a glance which one is live.

## Primary — GitHub Actions CI/CD (not in this folder)

`.github/workflows/deploy.yml` is the actual production deploy path: push to `main` ->
`dotnet publish` -> rsync to the GCP VM -> restart the `sahulatghartak-api` systemd service.
This runs automatically; nothing under `deploy/` is required for a normal deploy.

## Active — GCP (current server, manual/alternative path)

- **[gcp/deploy.ps1](gcp/deploy.ps1)** — manual deploy script for the same GCP VM the CI
  pipeline targets (`api.sahulatghartak.com`). Use only when deploying outside CI.
- **[gcp/setup-server.sh](gcp/setup-server.sh)** — one-time VM provisioning (.NET runtime,
  nginx, systemd service). Already run against the current server; only needed again for a
  replacement/new VM.
- **[gcp/deploy.config.psd1](gcp/deploy.config.psd1)** — connection config for the two scripts
  above (host, SSH key, remote path, service name, port, domain).

## Legacy — Hostinger (superseded, kept for reference only)

Per `AGENTS.md`: the Hostinger VPS (93.127.199.220) is no longer used — the app connects
directly to SQL Server and deploys to GCP instead. Do not run these against production.

- **[hostinger/deploy.ps1](hostinger/deploy.ps1)**
- **[hostinger/setup-server.sh](hostinger/setup-server.sh)**
- **[hostinger/fix-404.sh](hostinger/fix-404.sh)** — one-off nginx 404 fix script for that VPS.
- **[hostinger/deploy.config.psd1](hostinger/deploy.config.psd1)** — connection config for the
  scripts above.

## Maintaining this index

If the deploy target ever changes again (new VM, new provider), add the new scripts under
Active, move GCP's scripts to a new Legacy entry with a `STATUS: LEGACY` line in each header
(mirroring how Hostinger is marked now), and update `AGENTS.md`'s Deployment section to match.
