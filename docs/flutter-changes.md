# Flutter App Changes Tracker

Running checklist of backend changes that the Flutter app needs to adopt to complete a feature's integration, or that are being deliberately held back as breaking changes pending approval. Updated incrementally as each backend feature lands — **`api.txt` (repo root, currently v3.25) is the exact, authoritative request/response contract of every endpoint referenced below**; read the cited `api.txt` section before implementing, since this file only summarizes.

Sections are removed once the Flutter app has fully adopted them — this file tracks *pending/active* work, not a history of everything ever shipped. Completed feature history lives in git log and `api.txt`'s own version notes, not here.

**Standing constraint (2026-09-21):** the Flutter app is live on the Play Store and App Store, both of which have review/approval lag, while the backend/API can be updated instantly. Every backend change in this project is therefore built to be **additive and optional** — a currently-published app build must keep working completely unchanged against the updated backend, with zero risk of breakage while store approval for the new app version is pending. New fields on existing endpoints are nullable/optional with a legacy fallback; new endpoints are new routes an old app simply never calls. Nothing here is a hard cutover.

Legend:
- **Available now** — backend is live, app can adopt whenever convenient (non-breaking, optional).
- **Held for approval** — a genuinely breaking change to a live endpoint contract (not just optional-field additions) that hasn't been implemented at all yet; listed here so the scope is visible ahead of time. Per the constraint above, when these are eventually implemented they should also default to an optional/additive interim contract rather than a hard break, unless explicitly decided otherwise at that time.
- **TODO(remove after old app retired)** — inline code/doc comments marking legacy-fallback branches that exist ONLY to support currently-published app builds. Once the new app version is confirmed live on both stores (i.e. no meaningfully active install base still hits these code paths), these branches can be deleted — grep the codebase for this exact marker to find all of them. Do not remove any of these until that confirmation, even if it looks safe.

---

## Held for approval

### Provider category cap: max 3 categories per provider (2026-09-29)

**This is a breaking change to `PUT /api/providers/{providerUid}/categories`** — see `api.txt` v3.25, "PUT Provider Categories (Full Replace)" section.

- `categoryIds` in the request body is now capped at 3. A provider may have 1–3 categories, never more.
- A request sending more than 3 `categoryIds` now gets a clean `400`:
  ```json
  { "success": false, "message": "A provider can have at most 3 categories.", "data": null }
  ```
  previously this would have succeeded.
- If the Flutter app has (or plans) any UI letting a provider or admin pick more than 3 categories at once (e.g. during registration via `categoryIds`/`primaryCategoryId` on `POST /api/auth/register-provider`, or a multi-category picker on a provider-profile screen), it must enforce the same 1–3 cap client-side — ideally before hitting the API, so the user gets an inline validation message rather than a server error.
- Existing DB rows were migrated one-time (2026-09-29) — any provider that had more than 3 categories was trimmed down to 3 (kept their primary category + 2 others). No further action needed on existing data, this is purely a going-forward contract change.
- `GET /api/providers/{providerUid}/categories` response shape is unchanged — this only affects the `PUT` (write) side.
- Unaffected: `GET`/`PUT /api/providers/{providerUid}/service-titles` — Service Titles remain **unlimited** per provider (any number of predefined titles across the provider's up-to-3 categories).
