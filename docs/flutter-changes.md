---
status: current
---

# Flutter App Changes Tracker

Running checklist of backend changes that the Flutter app needs to adopt to complete a feature's integration, or that are being deliberately held back as breaking changes pending approval. Updated incrementally as each backend feature lands — **`api.txt` (repo root, currently v3.29) is the exact, authoritative request/response contract of every endpoint referenced below**; read the cited `api.txt` section before implementing, since this file only summarizes.

Sections are removed once the Flutter app has fully adopted them — this file tracks *pending/active* work, not a history of everything ever shipped. Completed feature history lives in git log and `api.txt`'s own version notes, not here.

**Standing constraint (2026-09-21):** the Flutter app is live on the Play Store and App Store, both of which have review/approval lag, while the backend/API can be updated instantly. Every backend change in this project is therefore built to be **additive and optional** — a currently-published app build must keep working completely unchanged against the updated backend, with zero risk of breakage while store approval for the new app version is pending. New fields on existing endpoints are nullable/optional with a legacy fallback; new endpoints are new routes an old app simply never calls. Nothing here is a hard cutover.

Legend:
- **Available now** — backend is live, app can adopt whenever convenient (non-breaking, optional).
- **Held for approval** — a genuinely breaking change to a live endpoint contract (not just optional-field additions) that hasn't been implemented at all yet; listed here so the scope is visible ahead of time. Per the constraint above, when these are eventually implemented they should also default to an optional/additive interim contract rather than a hard break, unless explicitly decided otherwise at that time.
- **TODO(remove after old app retired)** — inline code/doc comments marking legacy-fallback branches that exist ONLY to support currently-published app builds. Once the new app version is confirmed live on both stores (i.e. no meaningfully active install base still hits these code paths), these branches can be deleted — grep the codebase for this exact marker to find all of them. Do not remove any of these until that confirmation, even if it looks safe.

---

## Available now

### Push notifications, notification inbox and app version check

Backend status: **built and tested locally; deployed once the server release is pushed.** The exact contract is
`api.txt` v3.29, section "PUSH NOTIFICATIONS (FCM) & APP VERSION CHECK" (payload keys, event table, inbox
endpoints) - read it before implementing; this list is the to-do.

**1. Firebase setup (both apps / flavors)**
- Add `firebase_core` + `firebase_messaging`. Firebase project: `sahulatghartak-ef6ed`.
- Android package `com.coditiumsols.sahulatghartak`: add `google-services.json` (Firebase console -> Project settings).
  Android 13+ needs the runtime `POST_NOTIFICATIONS` permission (request it, e.g. after login).
- iOS bundle `com.coditiumsols.sahulatghartak.ios`: add `GoogleService-Info.plist`, enable Push Notifications +
  Background Modes (remote notifications) capabilities, and upload an APNs auth key in the Firebase console
  (needed before any iOS push can arrive). iOS is only testable on a physical device; test Android on an emulator
  with a Google Play system image.
- Do **not** add Firebase Analytics / Crashlytics / other Firebase SDKs without telling the backend owner: the
  privacy policy says the app uses FCM only (no analytics SDKs).

**2. Register / unregister the device token** (no auth header needed yet)
- After a successful login, and again on every `FirebaseMessaging.instance.onTokenRefresh`:
  `POST /api/notifications/register-token` `{ userId, userType, deviceToken, platform }` where `userId` is
  `LoginResponse.userId`, `userType` is `"Client"` or `"Provider"` (the role the app is currently in), `deviceToken`
  is `FirebaseMessaging.instance.getToken()`, `platform` is `"android"` or `"ios"`. Idempotent; safe to call every
  launch. If the user switches role in a dual-role account, register again for the new role.
- On logout: `POST /api/notifications/unregister-token` `{ deviceToken }` (before clearing the session) so the next
  person on the phone does not receive this user's pushes.

**3. Handle incoming pushes**
- Foreground: `FirebaseMessaging.onMessage` does not show a banner by itself - show an in-app banner / local
  notification, and refresh the inbox badge.
- Background / terminated: the OS shows the notification. Handle taps with `FirebaseMessaging.onMessageOpenedApp`
  and `FirebaseMessaging.instance.getInitialMessage()` (app launched from a notification).
- Routing: read `message.data['screen']` and ids `booking_id` / `request_id` (strings, may be empty). Please map these
  proposed values to the real routes (tell the backend owner if a name should change):
  | `screen` | Who gets it | Suggested destination |
  |---|---|---|
  | `request_details` | client | the request/booking details screen for `request_id` (show progress) |
  | `provider_job_requests` | provider | the incoming job requests list (highlight `booking_id`) |
  | `my_bookings` | provider | "my bookings" (open `booking_id`) |
  | `app_update` | everyone | the update prompt / open the store link from `/api/v1/app/config` |
- Notification `type` values: `job_assigned`, `booking_accepted`, `job_unavailable`, `provider_reassigning`,
  `booking_cancelled`, `job_started`, `job_completed`, `app_update`. A `job_unavailable` or `provider_reassigning`
  push means the lists should be refreshed (a request can go back to "Requested").
- Register a top-level `FirebaseMessaging.onBackgroundMessage` handler (data processing only).

**4. Notification inbox screen** (the backend keeps a copy of every booking push)
- `GET /api/notifications?userId=&userType=&page=&pageSize=` (newest first; pass the current role as `userType`).
- Badge: `GET /api/notifications/unread-count?userId=&userType=` on launch / resume / after each push.
- Tap an item: `POST /api/notifications/{id}/read` `{ userId }`, then route using the item's `screen` + `bookingUid` /
  `requestUid`. "Mark all read": `POST /api/notifications/read-all` `{ userId, userType }`.
- Timestamps are UTC with a `Z` suffix - convert to local time like the other endpoints.

**5. Version enforcement at app start**
- `GET /api/v1/app/config?platform=android|ios` (bare snake_case JSON, **not** the `ApiResponse` envelope; no auth).
  If installed version < `minimum_required_version` -> block the app with a non-dismissable update screen; else if
  `force_update` is true and installed < `latest_version` -> same; else if installed < `latest_version` -> dismissable
  prompt. Open `store_url` to update. Compare versions numerically per segment (1.0.10 > 1.0.9). Fail open (let the
  app run) if the call fails.

**Notes**
- The backend sends pushes only for: new job (provider), accepted (client), job taken by another provider (provider),
  finding another provider (client), cancelled (client/provider), job started (client), job completed (client +
  provider), and staff broadcasts. Scheduled reminders and payout notifications are not built yet.
- Endpoints are anonymous for now (identity in the body/query) like the rest of the API; they will move to Bearer JWT
  later - see `docs/auth-gap-report.md` finding 9. Keep the `userId` handling in one place so the header is easy to add.

---

## Testing findings

Issues found while testing the notification system end to end on the Android emulator (2026-10-01). Things the Flutter app must fix or confirm.

### 1. Role switch re-registers the device token (RESOLVED - no Flutter change needed)
The first test appeared to show the token staying on the old role after "switch to customer". It was a backend bug,
not a Flutter gap: egister-token rejected userType = "Client" for upgraded (dual-role) accounts with 400
"Unknown or inactive user". Fixed in the backend (needs the next deploy). Re-tested against the fixed backend: the app
calls egister-token on the role switch and the token row moved from Provider to Client. Keep this behaviour (also
re-register on login and on onTokenRefresh, always with the role the app is currently in).

### 2. Notification tap routing (to confirm)
- Tapping a banner now opens the app (backend fix: pushes no longer carry an Android click action, so no manifest
  intent filter is needed). Not yet verified: that the app routes using data['screen'], ooking_id and equest_id
  via onMessageOpenedApp (app in background) and getInitialMessage() (cold start). See step 3 above.

### Verified working (backend to emulator, Provider role)
New job, "job taken by another provider", booking cancelled and job completed banners all arrive, and tapping them
opens the app.
