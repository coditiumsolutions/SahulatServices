---
status: active
type: implementation
version: 2.0
updated: 2026-10-21
related:
  - docs/mobile-app/flutter-changes.md
---

# Deep linking (Android App Links + iOS Universal Links)

Version 2. Version 1 was the plan in `flutter-changes.md`; most of it is now implemented, so this file records what
exists and what is still pending. The app-side release checklist stays in `flutter-changes.md` until the build ships.

## What it does

A `https://sahulatghartak.com/...` link tapped outside our own site (WhatsApp, Facebook, Google results, SMS, ads)
opens the installed app. Without the app it opens the website as before. The phone decides per link: the website never
redirects, detects the app or blocks anything. Additive: old app builds ignore all of it.

Identifiers: Android package `com.coditiumsols.sahulatghartak`; iOS bundle id `com.coditiumsols.sahulatghartak.ios`,
team `J24H49NXWD` (app id `J24H49NXWD.com.coditiumsols.sahulatghartak.ios`). Only the bare host `sahulatghartak.com`
is claimed; the site folds `www` into it.

## Link map

| Link path | Opens in the app |
|---|---|
| `/`, `/Home/DownloadApp`, `/home-maintenance`, `/specialized-services`, `/property-and-legal-services`, any unknown path | The account's root screen (customer shell, provider dashboard or pending-verification page) |
| `/app/requests/{id}` | Request detail, client side only |
| `/app/bookings/{id}` | Provider job detail, provider accounts only |
| `/app/notifications` | The shared notifications inbox, both roles |

Ids are integers, as in `PushEvent`; a non-numeric id falls back to the root screen. Logged out, a link does nothing
and the normal landing/login stays up. A link never grants access: the API still authorises.

Deliberately not claimed, so they keep opening in the browser: `/privacy-policy`, `/delete-account` (both are
submitted to the stores), `/about`, `/adminportal`, `/Admin`, `/api`, `/swagger`.

## Website side (this repo)

- `/app` and `/app/...` have no web pages: `Program.cs` redirects them (302) to `/Home/DownloadApp`, so a link opened
  without the app, or on desktop, lands on the download page instead of a 404.
- `Helpers/AppLinkFiles.cs` builds both verification files. `Program.cs` serves them with two `MapGet` routes, as
  `application/json`, no redirect:
  - `/.well-known/assetlinks.json`: package plus SHA-256 fingerprints of the Play App Signing key and the upload key.
    The debug key is not listed, so debug builds do not verify. Fingerprints are public, so they live in code.
  - `/.well-known/apple-app-site-association`: the iOS app id and the same path list (`/` exact, then `/app*`,
    `/home-maintenance*`, `/specialized-services*`, `/property-and-legal-services*`, `/Home/DownloadApp*`).
- Served from code because the static file provider skips dot-prefixed folders (`.well-known`) and the iOS file has no
  extension.
- `PathPrefixes` in `AppLinkFiles.cs` must match the app's Android intent-filter. Change both together.
- Tests: `tests/HomeServicesPortal.ReleaseTests/AppLinkFilesTests.cs` (package, fingerprint format, app id, and that
  privacy/delete-account/admin paths are absent).

## App side (Flutter repo, build Android `1.0.6+12` / iOS `1.0.8+12`, uncommitted when written)

- `lib/utils/deep_link_router.dart` (`parseDeepLink`, `DeepLinkRouter`) using `app_links` 7.2.2, started from `main()`
  and routed from `SplashScreen` right after `routeColdStartPush()`. A link arriving before the session resolves is
  held, then routed after the splash; later links route immediately. Role checks reuse `routeForPush`.
- Only `https` links on `sahulatghartak.com` / `www.sahulatghartak.com` are accepted.
- Android: verified `VIEW`/`BROWSABLE` filter on `sahulatghartak.com` (`path` `/`, `pathPrefix` for the five prefixes
  above), `flutter_deeplinking_enabled=false`. `pathPrefix` is a case-sensitive string prefix, so `/app` also matches
  `/apple...` and only the exact casing `/Home/DownloadApp` opens the app.
- iOS: `applinks:sahulatghartak.com` in `Runner.entitlements`, `FlutterDeepLinkingEnabled=false` in `Info.plist`.
- Tests: `test/deep_link_router_test.dart` (parser).

## Pending

Blocking, before any link opens the app:
- [ ] Deploy `main` so the two `.well-known` files are live; check both return 200, `application/json`, no redirect.
- [ ] Apple: enable the Associated Domains capability on the App ID `com.coditiumsols.sahulatghartak.ios` and
      regenerate the provisioning profile.
- [ ] Ship the new builds (Android `1.0.6+12`, iOS `1.0.8+12`) to the stores. Links only work on these builds.

Verification on real builds:
- [ ] Android, Play-signed internal-testing build: `adb shell pm get-app-links com.coditiumsols.sahulatghartak`
      shows `verified`; open `https://sahulatghartak.com/home-maintenance` with the `adb` VIEW intent.
- [ ] iOS, TestFlight or real device (not the Simulator): paste a link into Notes and tap it; reinstall after
      association-file changes because iOS caches it.
- [ ] Confirm `app_links` receives Universal Links through this app's `SceneDelegate` (not yet confirmed).
- [ ] Cover cold start, backgrounded and foreground; logged out; wrong role (booking link on a client, request link on
      a provider in provider mode); unknown path; and that `/privacy-policy` and `/delete-account` still open in the
      browser.

Website follow-ups:
- [ ] Only put `/app/...` links in messages once a supporting build is adopted (old builds just open the website).
- [ ] Optional: iOS Smart App Banner meta tag (`apple-itunes-app`, app-id 6810184599) in `_PublicLayout.cshtml`, no
      app change needed.
- [ ] Optional: add `www` to both files and the app if it turns out the site answers on `www` without redirecting.
- [ ] If the claimed paths change, update `PathPrefixes`, the Android filter and this file together.
- [ ] Optional: track first launch from a link, to measure usage.
