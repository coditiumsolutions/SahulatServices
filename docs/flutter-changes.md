---
status: current
version: 1.13.0
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

## TODO for the next build

### Stop depending on `categoryId` for providers-detail
`categoryId` on providers-detail is backed by the deprecated `Providers.CategoryUid` scalar, which the backend plans to retire. `categoryIds` + `primaryCategoryId` (`GET`/`PUT /api/providers/{providerUid}/categories`) are the source of truth. Today the app still:
- reads `categoryId` / `categoryName` from `GET /api/providers-detail/{uid}` (`lib/models/provider/provider_detail.dart:59`) and shows it on the Profile tab (`lib/screens/provider/profile/profile_tab.dart:287`);
- echoes `categoryId` back in `PUT /api/providers-detail/{uid}` (`lib/services/provider_profile_api_service.dart:32`);
- reads `categoryUid` from `GET /api/provider-profiles/{userId}` with a hard `as int` cast (`lib/models/provider_profile_model.dart:27`), used by `AuthProvider.login` (`lib/providers/auth_provider.dart:63-66`). If the field disappears the cast throws, the catch swallows it and `providerUid` is not set either.

Change: show the primary category from the categories endpoint, drop `categoryId` from the providers-detail PUT body, and make the `categoryUid` parse nullable so `providerUid` is still set. Until the build with this ships and is adopted, the backend must keep returning and accepting these fields.
