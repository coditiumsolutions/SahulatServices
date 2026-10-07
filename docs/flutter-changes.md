---
status: current
version: 1.11.0
---

# Flutter App Changes Tracker

Running checklist of backend changes that the Flutter app needs to adopt to complete a feature's integration, or that are being deliberately held back as breaking changes pending approval. Updated incrementally as each backend feature lands — **`api.txt` (repo root, currently v3.33) is the exact, authoritative request/response contract of every endpoint referenced below**; read the cited `api.txt` section before implementing, since this file only summarizes.

Sections are removed once the Flutter app has fully adopted them — this file tracks *pending/active* work, not a history of everything ever shipped. Completed feature history lives in git log and `api.txt`'s own version notes, not here.

**Standing constraint (2026-09-21):** the Flutter app is live on the Play Store and App Store, both of which have review/approval lag, while the backend/API can be updated instantly. Every backend change in this project is therefore built to be **additive and optional** — a currently-published app build must keep working completely unchanged against the updated backend, with zero risk of breakage while store approval for the new app version is pending. New fields on existing endpoints are nullable/optional with a legacy fallback; new endpoints are new routes an old app simply never calls. Nothing here is a hard cutover.

Legend:
- **Available now** — backend is live, app can adopt whenever convenient (non-breaking, optional).
- **Held for approval** — a genuinely breaking change to a live endpoint contract (not just optional-field additions) that hasn't been implemented at all yet; listed here so the scope is visible ahead of time. Per the constraint above, when these are eventually implemented they should also default to an optional/additive interim contract rather than a hard break, unless explicitly decided otherwise at that time.
- **TODO(remove after old app retired)** — inline code/doc comments marking legacy-fallback branches that exist ONLY to support currently-published app builds. Once the new app version is confirmed live on both stores (i.e. no meaningfully active install base still hits these code paths), these branches can be deleted — grep the codebase for this exact marker to find all of them. Do not remove any of these until that confirmation, even if it looks safe.

---

**New, ready to build (backend built 2026-10-07, live once pushed to main, api.txt v3.40 "Release pull"):** silent pushes are unreliable on iOS, so
`GET /api/v1/app/config?platform=ios&device_token=<fcm token>` now also returns `last_unblock_at` (UTC ISO 8601 with "Z",
or `null`; always present). `device_token` is optional: without it (or with an unregistered one) you only get releases
sent to everyone or to your platform; with it you also get releases sent to your user or your device. Never an error, never
reveals whether a token exists. App steps:
- Store when a forced block was created (the push `sent_at`, else the receive time). Blocks stored by older builds have no
  time: do not clear them from the pull.
- On launch and on every resume, call app config (send `device_token` when you have one). If `last_unblock_at` is later than
  the block's created time, clear the block exactly like a pushed `app_unblock` and raise "cleared at" to `last_unblock_at`.
- Fail open: a failed or slow call changes nothing. Compare with "later than" only (the value can differ from the pushed
  `sent_at` by a few milliseconds). Treat a missing or unparseable value as null.
