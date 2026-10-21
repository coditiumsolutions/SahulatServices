---
status: current
version: 1.17.0
---

# Flutter App Changes Tracker

Running checklist of backend changes that the Flutter app still needs to adopt, and of app work that is built but waiting on a release or on the backend. Updated incrementally — **`api.txt` (repo root, see its header for the current version) is the exact, authoritative request/response contract of every endpoint referenced below**; read the cited `api.txt` section before implementing, since this file only summarizes.

Sections are removed once the Flutter app has fully adopted them — this file tracks *pending/active* work, not a history of everything ever shipped. Completed feature history lives in git log and `api.txt`'s own version notes, not here.

**Standing constraint (2026-09-21):** the Flutter app is live on the Play Store and App Store, both of which have review/approval lag, while the backend/API can be updated instantly. Every backend change in this project is therefore built to be **additive and optional** — a currently-published app build must keep working completely unchanged against the updated backend, with zero risk of breakage while store approval for the new app version is pending. New fields on existing endpoints are nullable/optional with a legacy fallback; new endpoints are new routes an old app simply never calls. Nothing here is a hard cutover.

Legend:
- **Available now** — backend is live, app can adopt whenever convenient (non-breaking, optional).
- **Built, awaiting release** — implemented in the app but not yet in a store build, or needing backend/store-side setup before it does anything.
- **TODO(remove after old app retired)** — inline code/doc comments marking legacy-fallback branches that exist ONLY to support currently-published app builds. Once the new app version is confirmed live on both stores, these branches can be deleted — grep the codebase for this exact marker. Do not remove any of these until that confirmation, even if it looks safe.

---

## Built, awaiting release: deep links (Android App Links + iOS Universal Links)

Implemented in build Android `1.0.6+12` / iOS `1.0.8+12` (uncommitted at the time of writing). A `https://sahulatghartak.com/...` link tapped outside our own site (WhatsApp, Facebook, Google results, SMS, ads) opens the installed app; without the app it opens the website as before. Additive: old builds ignore all of it.

### What the app does
- `lib/utils/deep_link_router.dart` (`parseDeepLink`, `DeepLinkRouter`), started from `main()`, routed from `SplashScreen` right after `routeColdStartPush()`. Uses `app_links` (7.2.2). A link that arrives before the session is resolved is held and routed after the splash, the same pattern as `PushNotificationService.takeInitialEvent()`; later links route immediately.
- Only `https` links on `sahulatghartak.com` / `www.sahulatghartak.com` are accepted; anything else is ignored. A link never grants access, the API still authorises.
- Path mapping:

  | Link path | Opens |
  |---|---|
  | `/`, `/Home/DownloadApp`, `/home-maintenance`, `/specialized-services`, `/property-and-legal-services`, any unknown or malformed path | The account's root screen (customer shell, provider dashboard, or pending-verification page) |
  | `/app/requests/{requestId}` | `RequestDetailScreen` via `routeForPush` (client side active only; otherwise root) |
  | `/app/bookings/{bookingId}` | Provider job detail via `routeForPush` (provider accounts only; otherwise root) |
  | `/app/notifications` | The shared notifications inbox, for both roles |

- Ids are integers (as in `PushEvent`); a non-numeric id falls back to the root screen.
- Logged out: the link does nothing and the normal landing/login flow stays up. No protected screen opens from a link.
- Android (`AndroidManifest.xml`): verified `VIEW`/`BROWSABLE` filter on host `sahulatghartak.com` with `path` `/` and `pathPrefix` `/app`, `/home-maintenance`, `/specialized-services`, `/property-and-legal-services`, `/Home/DownloadApp`; `flutter_deeplinking_enabled=false`. Not claimed: `/privacy-policy`, `/delete-account`, `/about`, `/adminportal`, `/Admin`, `/api`, `/swagger`. `www` is not added because it is unknown whether the site answers there. Note `pathPrefix` is a case-sensitive string prefix (`/app` also matches `/apple…`).
- iOS: `Runner.entitlements` has `applinks:sahulatghartak.com`; `Info.plist` has `FlutterDeepLinkingEnabled=false`.
- Tests: `test/deep_link_router_test.dart` covers the parser.

### Still to do before it works
- **Apple**: enable the Associated Domains capability on the App ID `com.coditiumsols.sahulatghartak.ios` and regenerate the provisioning profile (entitlements are baked in at signing).
- **Verify on a real build** that `app_links` receives Universal Links through this app's `SceneDelegate` (not yet confirmed).
- Ship the store builds, then run the tests below.

### Backend/website side (not the Flutter agent)
- Built (`Helpers/AppLinkFiles.cs`, two `MapGet` routes in `Program.cs`, tests in `AppLinkFilesTests.cs`), live once main is deployed:
  - `https://sahulatghartak.com/.well-known/assetlinks.json` for `com.coditiumsols.sahulatghartak`, listing the Play App Signing key and the upload key. The debug key is deliberately not listed, so debug builds will not verify.
  - `https://sahulatghartak.com/.well-known/apple-app-site-association` for `J24H49NXWD.com.coditiumsols.sahulatghartak.ios`, same path list as the Android filter (iOS patterns `/app*`, `/home-maintenance*`, ...). Both are `application/json`, no redirect. They are served from code because the static file provider skips `.well-known` and the iOS file has no extension.
  - If the Android path list in the app changes, change `AppLinkFiles.PathPrefixes` to match.
- `/app/...` URLs redirect to `/Home/DownloadApp` on the website, so they are safe without the app. Still only put them in messages once a supporting build is adopted.
- Optional, no app change: iOS Smart App Banner meta tag (`apple-itunes-app`, app-id 6810184599) in the public layout.

### Testing
- Android (Play-signed internal-testing build; debug builds will not verify): `adb shell pm get-app-links com.coditiumsols.sahulatghartak` should show `verified`; open links with `adb shell am start -a android.intent.action.VIEW -c android.intent.category.BROWSABLE -d "https://sahulatghartak.com/home-maintenance" com.coditiumsols.sahulatghartak`.
- iOS (TestFlight or real device, not the Simulator): paste a link into Notes and long-press or tap it. iOS caches the association file, so reinstall after changes.
- Cover cold start, backgrounded and foreground; logged out; wrong role (booking link on a client, request link on a provider in provider mode); unknown path; and confirm `/privacy-policy` and `/delete-account` still open in the browser.

---

## Available now: backend built, app not adopted yet (api.txt v3.42)

### Provider zones (register, view, edit)
- `GET /api/zones` returns the configured zone names (`data`: array of strings). Use them for a multi-select picker.
- **Limit: a provider can pick at most 2 zones.** Stop the user at 2 in the picker; the backend returns 400 ("A provider can have at most 2 zones.") for more.
- Registration: `POST /api/auth/register-provider` accepts an optional `zones` array (names from `/api/zones`). Omit it or send `[]` for none. An unknown name is 400.
- Profile: `GET /api/providers/{providerUid}/zones` lists the provider's zones. `PUT /api/providers/{providerUid}/zones` with `{ "zones": [...] }` replaces the whole set (send `[]` to clear). Idempotent.
- Old builds are unaffected: `zones` is optional everywhere.

### Text estimate on service titles
- `GET /api/service-titles` and `/api/service-titles/{id}` now include `estimateText` (string or null), e.g. `"2000-3000"`. Show it as the estimated budget when present; fall back to `basePrice` when it is null. `basePrice` is unchanged (still numeric, still populates `estimatedBudget`).

---

## TODO for the next build

### Stop depending on `categoryId` for providers-detail
`categoryId` on providers-detail is backed by the deprecated `Providers.CategoryUid` scalar, which the backend plans to retire. `categoryIds` + `primaryCategoryId` (`GET`/`PUT /api/providers/{providerUid}/categories`) are the source of truth. Today the app still:
- reads `categoryId` / `categoryName` from `GET /api/providers-detail/{uid}` (`lib/models/provider/provider_detail.dart:59`) and shows it on the Profile tab (`lib/screens/provider/profile/profile_tab.dart:287`);
- echoes `categoryId` back in `PUT /api/providers-detail/{uid}` (`lib/services/provider_profile_api_service.dart:32`);
- reads `categoryUid` from `GET /api/provider-profiles/{userId}` with a hard `as int` cast (`lib/models/provider_profile_model.dart:27`), used by `AuthProvider.login` (`lib/providers/auth_provider.dart:63-66`). If the field disappears the cast throws, the catch swallows it and `providerUid` is not set either.

Change: show the primary category from the categories endpoint, drop `categoryId` from the providers-detail PUT body, and make the `categoryUid` parse nullable so `providerUid` is still set. Until the build with this ships and is adopted, the backend must keep returning and accepting these fields.
