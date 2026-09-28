# Flutter App Changes Tracker

Running checklist of backend changes that the Flutter app needs to adopt to complete a feature's integration, or that are being deliberately held back as breaking changes pending approval. Updated incrementally as each backend feature lands — **`api.txt` (repo root, currently v3.22) is the exact, authoritative request/response contract of every endpoint referenced below**; read the cited `api.txt` section before implementing, since this file only summarizes.

Sections are removed once the Flutter app has fully adopted them — this file tracks *pending/active* work, not a history of everything ever shipped. Completed feature history lives in git log and `api.txt`'s own version notes, not here.

**Standing constraint (2026-09-21):** the Flutter app is live on the Play Store and App Store, both of which have review/approval lag, while the backend/API can be updated instantly. Every backend change in this project is therefore built to be **additive and optional** — a currently-published app build must keep working completely unchanged against the updated backend, with zero risk of breakage while store approval for the new app version is pending. New fields on existing endpoints are nullable/optional with a legacy fallback; new endpoints are new routes an old app simply never calls. Nothing here is a hard cutover.

Legend:
- **Available now** — backend is live, app can adopt whenever convenient (non-breaking, optional).
- **Held for approval** — a genuinely breaking change to a live endpoint contract (not just optional-field additions) that hasn't been implemented at all yet; listed here so the scope is visible ahead of time. Per the constraint above, when these are eventually implemented they should also default to an optional/additive interim contract rather than a hard break, unless explicitly decided otherwise at that time.
- **TODO(remove after old app retired)** — inline code/doc comments marking legacy-fallback branches that exist ONLY to support currently-published app builds. Once the new app version is confirmed live on both stores (i.e. no meaningfully active install base still hits these code paths), these branches can be deleted — grep the codebase for this exact marker to find all of them. Do not remove any of these until that confirmation, even if it looks safe.

---

## Available now

### Provider Service Titles (per-category)

**Status: backend ready and live (2026-09-28); purely additive, no existing endpoint's shape
changed. Nothing required from the app.**

Providers can already belong to multiple `ServiceCategories` (`GET`/`PUT
api/providers/{providerUid}/categories`, see `api.txt`). This adds one level of granularity on
top: a provider can now *optionally* declare which predefined `ServiceTitles` they offer, scoped
to categories they already have.

What's new:
- `GET api/providers/{providerUid}/service-titles` — list a provider's current service titles.
  Returns an empty array by default (every provider starts with zero titles — this is normal,
  not an incomplete profile).
- `PUT api/providers/{providerUid}/service-titles` — full-replace the set, body
  `{ "serviceTitleIds": [5, 9] }`. Same full-replace convention as the categories endpoint, with
  one difference: an **empty** `serviceTitleIds` list is valid here (it removes all titles) —
  categories require at least one, titles don't.
- Every id sent must belong (via `ServiceTitles.CategoryUid`) to a category the provider already
  has — otherwise a 400 naming the offending title. Add the category first via the existing
  `PUT .../categories` if needed.
- Full request/response shapes, examples, and error cases: see `api.txt`'s "GET/PUT Provider
  Service Titles" sections (immediately after "PUT Provider Categories (Full Replace)").

Nothing required: if the Flutter app never calls these two endpoints, provider registration and
every existing screen behave exactly as before — this is purely an *additional*, optional
profile refinement, same additive posture as the multi-category feature before it.

If/when a Flutter provider-profile screen wants to adopt it, the natural flow (no new concepts
beyond what the categories feature already introduced):
1. `GET api/providers/{providerUid}/categories` (existing) — the provider's current categories.
2. For each category, `GET api/service-titles?categoryUid={id}` (existing) — the pickable titles
   under it.
3. Let the provider check/uncheck titles per category, then `PUT
   api/providers/{providerUid}/service-titles` with the full chosen set (existing full-replace
   convention).

One behavior change, admin-portal-only, no API contract implication for this app: the admin
"Assign Provider" screen now silently narrows its eligible-provider list by title when a
customer's free-text service title happens to exactly match one of these predefined titles (and
at least one eligible provider has it) — falls back to the existing category-only behavior
otherwise. Listed here only for completeness; there is nothing for the Flutter app to change.

---
