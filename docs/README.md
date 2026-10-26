---
status: current
---

# docs/ index

Documents are grouped by purpose. Every file starts with a small YAML frontmatter block
(`status: current` / `active` / `planned` / `partially-stale` / `stale`, plus `type`, `updated` or `reason`
where useful) so its freshness is visible without reading the whole document. The two canonical,
always-current references for this project are **not** in this folder. They live at the repo root:
`api.txt` (mobile/client API contract) and `db.txt` (live DB schema).

| Folder | What lives there |
|---|---|
| [seo/](seo/) | SEO and GEO research, the Urdu plan, store listing copy |
| [mobile-app/](mobile-app/) | What the Flutter app needs to adopt, notification sounds |
| [design/](design/) | Design references and implementation plans |
| [audits/](audits/) | Security and API audits, cleanup notes |
| [testing/](testing/) | Test plans and manual test-session reports |
| [legal/](legal/) | The privacy policy and the terms and conditions |

## seo/

- **[SEO-Report.md](seo/SEO-Report.md)** (active) - how to get sahulatghartak.com into Google Search and its
  AI surfaces. Holds the implementation status, the Meta Pixel setup and event list (section 3.9), and the
  prioritised plan.
- **[GEO-Report.md](seo/GEO-Report.md)** (active) - how chatbots such as ChatGPT, Gemini and Perplexity pick
  sources, the AI-crawler policy, and IndexNow.
- **[Urdu-Variants-Plan.md](seo/Urdu-Variants-Plan.md)** (planned) - plan for Urdu versions of the public site.
  Nothing is built yet.
- **[storelisting.md](seo/storelisting.md)** (current) - copy-paste text for the Google Play and App Store
  listings.

## mobile-app/

- **[flutter-changes.md](mobile-app/flutter-changes.md)** (current) - running checklist of backend changes the
  Flutter app needs to adopt. Updated incrementally as features land.
- **[deep-linking.md](mobile-app/deep-linking.md)** (active) - Android App Links / iOS Universal Links: link map,
  the `.well-known` files served by this repo, the app-side setup, and the pending list.
- **[notification-sounds.md](mobile-app/notification-sounds.md)** (current) - custom push sounds and channels.
- **[web-parity-spec.md](mobile-app/web-parity-spec.md)** (current) - the Flutter app's screens and workflows, written
  as the spec for the website's client/provider portal.
- **[web-parity-decisions.md](mobile-app/web-parity-decisions.md)** (current) - decisions D1-D21 made while building
  the portal, with an implementation status table.
- **[web-parity-audit.md](mobile-app/web-parity-audit.md)** (current) - gap tables for the web pages against the app,
  and what has been built since.
- **[flutter-integration-plan.md](mobile-app/flutter-integration-plan.md)** (stale) - one-time integration task
  list from 2026-08-04; expected long since adopted.

## design/

- **[status-workflow.md](design/status-workflow.md)** (current) - design reference for
  `CustomerServiceRequests.Status` and `ServiceBookings.Status`, implemented and current.
- **[gps-maps-implementation.md](design/gps-maps-implementation.md)** (partially stale) - Phases 1 and 2 are
  accurate history; the Phase 3 "not started" framing may be outdated. Check `api.txt` first.
- **[feature-plan-multi-provider-notifications-billing-categories.md](design/feature-plan-multi-provider-notifications-billing-categories.md)**
  (stale) - implementation plan from 2026-09-22; all four described features have shipped.

## audits/

- **[auth-gap-report.md](audits/auth-gap-report.md)** (active) - security audit from 2026-08-27. Its critical
  findings (plaintext passwords, no JWT issuance) were still true when re-verified on 2026-09-29. An open
  issue, not a historical record.
- **[TEMP-legacy-cleanup.md](audits/TEMP-legacy-cleanup.md)** (active) - temporary list of legacy endpoint
  cleanup candidates from 2026-10-01. Nothing done; each item needs owner approval.
- **[api-audit-report.md](audits/api-audit-report.md)** (stale) - audit against `api.txt` v1.5 / `db.txt` v1.9,
  long superseded. Per standing project instruction, not updated for new endpoint work.

## testing/

- **[notification-testing.md](testing/notification-testing.md)** (current) - push notification test plan and
  results, including the Android emulator and iOS findings.
- **[provider-workflow-test-report.md](testing/provider-workflow-test-report.md)** (stale) - one-off manual test
  session, 2026-08-04.
- **[payment-module-test-report.md](testing/payment-module-test-report.md)** (stale) - one-off manual test
  session, 2026-08-04.

## legal/

- **[PRIVACY_POLICY.md](legal/PRIVACY_POLICY.md)** (current) - the privacy policy. The live copy is served from
  `Views/Home/Privacy.cshtml` at `/privacy-policy`; update both together.
- **[TERMS_&_CONDITONS.md](legal/TERMS_&_CONDITONS.md)** (interim) - customer and provider terms. The live copy is the
  two partials `Views/Shared/_TermsCustomer.cshtml` and `_TermsProvider.cshtml`, shown at `/terms-and-conditions` and
  in the signup dialog; update the markdown and both partials together. The owner will revise the text later.

## Maintaining this index

When adding a doc, put it in the folder that fits (or add a new folder and a row in the table above), give it
a `status` frontmatter field, and add one line here. When a doc's findings are resolved or its plan fully ships,
flip its `status` to `stale` (with a `reason` and `superseded_by`) and move its line to the end of its folder
section, rather than deleting it. These are historical records of what was investigated or built, and why.
If you move a file, search the repo for its old path (`grep -rn "docs/<name>.md"`), because `AGENTS.md`,
`api.txt`, `README.md` and code comments link to these paths.
