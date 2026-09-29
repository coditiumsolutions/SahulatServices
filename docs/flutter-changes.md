---
status: current
---

# Flutter App Changes Tracker

Running checklist of backend changes that the Flutter app needs to adopt to complete a feature's integration, or that are being deliberately held back as breaking changes pending approval. Updated incrementally as each backend feature lands — **`api.txt` (repo root, currently v3.26) is the exact, authoritative request/response contract of every endpoint referenced below**; read the cited `api.txt` section before implementing, since this file only summarizes.

Sections are removed once the Flutter app has fully adopted them — this file tracks *pending/active* work, not a history of everything ever shipped. Completed feature history lives in git log and `api.txt`'s own version notes, not here.

**Standing constraint (2026-09-21):** the Flutter app is live on the Play Store and App Store, both of which have review/approval lag, while the backend/API can be updated instantly. Every backend change in this project is therefore built to be **additive and optional** — a currently-published app build must keep working completely unchanged against the updated backend, with zero risk of breakage while store approval for the new app version is pending. New fields on existing endpoints are nullable/optional with a legacy fallback; new endpoints are new routes an old app simply never calls. Nothing here is a hard cutover.

Legend:
- **Available now** — backend is live, app can adopt whenever convenient (non-breaking, optional).
- **Held for approval** — a genuinely breaking change to a live endpoint contract (not just optional-field additions) that hasn't been implemented at all yet; listed here so the scope is visible ahead of time. Per the constraint above, when these are eventually implemented they should also default to an optional/additive interim contract rather than a hard break, unless explicitly decided otherwise at that time.
- **TODO(remove after old app retired)** — inline code/doc comments marking legacy-fallback branches that exist ONLY to support currently-published app builds. Once the new app version is confirmed live on both stores (i.e. no meaningfully active install base still hits these code paths), these branches can be deleted — grep the codebase for this exact marker to find all of them. Do not remove any of these until that confirmation, even if it looks safe.

---

## Available now

### Account deletion now blocks on pending dues (2026-09-29)

**`POST /api/auth/delete-account`** — same endpoint, same request/response shape, but two behavior changes worth surfacing in the app's UI:

- **New 400 cases.** The app should show these two new error messages verbatim (via the existing `message` field) rather than a generic failure, since they're actionable — the user can resolve the condition and retry:
  ```json
  { "success": false, "message": "Cannot delete account: you have an active or unpaid booking. Please settle it before deleting your account.", "data": null }
  ```
  ```json
  { "success": false, "message": "Cannot delete account: you have an active booking or a pending payout. Please resolve it before deleting your account.", "data": null }
  ```
  A client is blocked if they owe money on a booking or have one still open (Pending/Accepted/In Progress). A provider is blocked if they have an open booking or an unpaid payout still pending. If the app already surfaces `message` generically on any 400 from this endpoint, no code change is needed — this is just so the UI copy doesn't get overridden with something more generic.
- **Name is now preserved, not scrubbed.** On successful deletion, the account's display name is no longer replaced with a placeholder — irrelevant to the app itself (the account becomes inaccessible either way, since login is disabled), but noted here for completeness. See `api.txt` v3.26, "POST Delete Account" section, for the full before/after.