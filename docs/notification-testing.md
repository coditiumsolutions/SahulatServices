---
status: current
version: 1.14.0
---

# Push notification test routine

How the booking push notifications were tested end to end (2026-10-01): local API against the shared dev database,
Android emulator running the Flutter app. Repeat this after changing `BookingPushNotifier`, `NotificationService`,
the notification templates, or any booking status transition in `BookingService`.

Contract reference: `api.txt` section "PUSH NOTIFICATIONS (FCM) & APP VERSION CHECK". Flutter-side to-do and test
findings: `docs/flutter-changes.md`.

## 1. Prerequisites

- Local API running (`https://localhost:7265`) with `Firebase:ServiceAccountPath` pointing at the service-account key
  (`HomeServicesPortal/secrets/`, git-ignored) and `Notifications:BookingPushEnabled = true`.
- Android emulator with a **Google Play** system image, app installed and logged in, `POST_NOTIFICATIONS` allowed.
  Test backgrounded banners with **Home** or screen lock. Swiping the app away from recents (cold start) still receives
  pushes; only a **Force stop** from Android settings blocks FCM until the app is opened again. Cold-start checks need
  the app launched outside `flutter run` (or a physical device), since killing the run session drops the debugger.
  The emulator plays no sound, so sounds need a physical device. iOS cannot be tested on a simulator (needs a physical
  device and an APNs key). Test **release** builds for anything involving sounds or resources: a debug build can hide
  release-only problems (section 10).
- A test account that is both a client and a provider (here user 76 = Client 74 + Provider 35), so one device can play
  both roles. A second provider (here 64) is only needed for the "taken by another provider" test.
- Admin portal login for the staff-only steps (assign, staff cancel). Credentials live in
  `HomeServicesPortal/secrets/admin-credentials.txt` (git-ignored).
- Use `curl.exe -k` for HTTP calls. Windows PowerShell 5.1 `Invoke-WebRequest` fails against the local dev certificate.

## 2. Device token and role

Pushes are filtered by role: a token registered as `Provider` only receives provider pushes, and vice versa.
`register-token` upserts by token, so the same device token moves to the new role.

```
GET the current row:  SELECT UserId, UserType, UpdatedAt FROM UserDeviceTokens
Flip it (if the app has not):  POST /api/notifications/register-token {userId, userType, deviceToken, platform}
```

The app re-registers on login and on a role switch ("switch to customer" in the app moved the row to Client).
Check the row's `UserType` before each group of tests; do not print the token itself.

### Push Tester page (on-demand sends)

`/Admin/PushTester` (Setup menu, Admin / Super Admin) fires any booking notification without driving the whole flow:
pick one user, one registered device, or every Client/Provider device, then a booking event (real template wording,
with editable service/provider/reason text) or a fully custom title, message, type, screen and extra `key=value`
data. "Deliver to any role" (on by default) ignores the token's role; untick it to test the real role filtering.
"Also save to inbox" writes the inbox row too. Use it for quick banner, tap and wording checks; the matrix in section 5
is still the way to test the real state transitions.

## 3. Baseline and cleanup

Everything runs on the shared live database, so record counts first and delete only what the tests created.

```sql
SELECT (SELECT COUNT(*) FROM CustomerServiceRequests) Req, (SELECT COUNT(*) FROM ServiceBookings) Bk,
       (SELECT COUNT(*) FROM PaymentLedger) Led, (SELECT COUNT(*) FROM ProviderPayouts) Pay,
       (SELECT COUNT(*) FROM UserNotifications) Inbox, (SELECT COUNT(*) FROM AdminNotifications) Adm,
       (SELECT MAX(UID) FROM CustomerServiceRequests) MaxReq, (SELECT MAX(UID) FROM ServiceBookings) MaxBk
```

Real users may create requests while you test (this happened: two real requests completed during the run), so do not
delete by "everything after the baseline". Delete by explicit ids, in one transaction, in this order:

1. `PaymentLedger` rows for the test booking(s) (completion writes 3: commission, job earning, cash collected; a
   cash-to-provider job creates no `ProviderPayouts` row)
2. `BookingMaterialItems` for the test bookings
3. `ServiceBookings`, then `CustomerServiceRequests` (FK order)
4. `UserNotifications` where `RequestUid` is a test request
5. `AdminNotifications` whose `RelatedEntityUID` is a test request or booking, **and** whose `Type` matches
   (`ServiceRequestCreated` for the request, `ProviderBookingCancelled` for the booking). `RelatedEntityUID` alone is
   ambiguous (request and booking ids share one number space), and an id-only delete removed one unrelated row on
   2026-10-02 (see section 9).

Leave the device token row alone.

## 4. Driving the flow

All mobile calls are anonymous for now (see `docs/auth-gap-report.md` finding 9). Name test requests `NOTIF TEST ...`.

| Step | How |
|---|---|
| Create request (as the test client) | `POST /api/customer-service-requests` `{clientUid, categoryUid, clientAddressUid, serviceTitle, serviceTitleUid, ...}` |
| Assign provider(s) | Admin portal only: `GET /Admin/ServiceRequests/Assign/{requestUid}` then `POST` the form (`ProviderUids`, amounts, `PaymentMode`, `CommissionType`, anti-forgery token) |
| Accept / reject | `POST /api/service-bookings/{bookingUid}/respond` `{providerUid, accept}` |
| Start job | `POST /api/service-bookings/{bookingUid}/start` `{providerUid}` |
| Complete | `POST /api/service-bookings/{bookingUid}/verify-completion` `{providerUid, passcode, actualAmountPaid, labourAmount}`; the passcode is in `ServiceBookings.Passcode` after accept |
| Provider cancel | `PUT /api/service-bookings/{bookingUid}` full body with `status: "Cancelled"` and a `cancelReason` |
| Staff cancel | Admin portal: `POST /Admin/Bookings/Edit/{bookingUid}` with the form's existing values and `Status=Cancelled`, `CancelReason` |

Logging in to the portal with curl: `GET /adminportal` (cookie jar, read the `__RequestVerificationToken`), then
`POST /Account/Login` with `Username`, `Password`, the token and `RememberMe=false`. Form POSTs need a fresh token from
the form's own GET page. Resubmit the form's current values unchanged except what you mean to change.

## 5. Test matrix and results

Run one event at a time and wait for the tester to confirm the banner and the tap before the next. A tap should open
the app (data `screen`/`booking_id` routing is the Flutter agent's job). Also check `UserNotifications` gets exactly one
row per recipient and that repeating an accept/start/complete adds none (idempotency).

**Provider role (token = Provider)**

| Event | Trigger | Banner | Result |
|---|---|---|---|
| New job | assign provider(s) in the portal | New job request | pass (tap pass after the click-action fix) |
| Taken by another provider | a sibling provider accepts | Job no longer available | pass |
| Cancelled | staff cancels an accepted booking | Booking cancelled | pass, tap pass |
| Completed | verify-completion | Job completed | pass |

**Client role (token = Client)**

| Event | Trigger | Banner | Result |
|---|---|---|---|
| Provider accepted | provider accepts | Provider accepted your request | pass, tap pass |
| Finding another provider | accepted provider cancels | Provider cancelled | pass |
| Booking cancelled | staff cancels an accepted booking | Booking cancelled | pass |
| Job started | provider starts | Job started | pass |
| Job completed | verify-completion | Job completed | pass |

Accept and start notify the client only, and "new job" notifies the provider only, so the same flow needs the token on
the matching role at each step. Steps that notify the other role still write their inbox row but show no banner.

**Cold start (app swiped away from recents, 2026-10-02, physical Android device, sent from the Push Tester):** the push
is delivered and the banner shows. Tap routing from this state (`getInitialMessage`) was confirmed on 2026-10-05
(section 10).

## 6. Findings from this run

1. **Tap did nothing (backend, fixed).** Android pushes carried `AndroidNotification.ClickAction =
   FLUTTER_NOTIFICATION_CLICK`; the app has no activity filtering for that action, so Android had nothing to launch.
   Removed the click action from the Android config (`NotificationService.BuildAndroid`). The `click_action` key stays
   in the data payload. Tapping now opens the app.
2. **`register-token` rejected the Client role for upgraded accounts (backend, fixed).** It compared against
   `UsersLogin.UserType`, which is "Provider" for an account upgraded from client. `UserExistsAsync` now checks that the
   role's own profile row exists. This made the first role-switch attempt look like a Flutter bug; the app was fine.
3. **Reassignment blocked after a provider cancel (existing bug, fixed).** A provider cancel resets the request to
   Initiated so staff can reassign, but the assign form and the assign/booking-create guards counted the Cancelled
   booking as active ("Request not found, not initiated, or already assigned"). The three guards in `BookingService`
   (`GetAssignProviderFormAsync`, `AssignProviderAsync`) and `ServiceBookingApiService` now ignore Cancelled bookings.
   `ServiceBookings` has no unique constraint on `RequestUID`, so a second booking row on the same request is allowed.
   Verified after the fix: a provider-cancelled request (booking Cancelled, request Initiated) opened the assign form and
   took a second booking alongside the cancelled one.
4. **Banner showed "2032y" (backend, fixed).** The Android payload had no event time, so the device-derived stamp was
   wrong (seen on a physical phone too). `BuildAndroid` now sets `EventTimestamp` to the server UTC time; the banner
   reads "Now". Every push also carries `sent_at` (ISO UTC) in its data for any in-app timestamp.
5. **Inbox `CreatedAt` had no `Z` (backend, fixed).** The value is UTC but was read back as Unspecified, so clients
   parsed it as local time. `UserNotificationApiDto.CreatedAt` is now marked UTC.

6. **Inbox limits added (backend).** Per user and role, rows older than `Inbox.RetentionDays` (90) are deleted and
   only the newest `Inbox.MaxPerRole` (200) are kept; both are editable under Admin > Configurations (validated 7-365
   and 20-1000). Pruning runs after each new inbox row is saved. To check it, lower the values in Configurations,
   send enough notifications to one user (Push Tester with "save to inbox" does not prune; real booking events do) and
   confirm `UserNotifications` shrinks for that user and role only.

7. **Appearance changes (backend, built, not yet tested on a device).** Titles/bodies reworded (one emoji only on new
   job, accepted, cancelled, completed), Android accent colour, a per-booking tag so a newer push replaces the earlier
   banner (iOS: thread id), and `channel_id` in the data payload. Android channels (now `job_requests_v2`,
   `booking_updates_v2`, `announcements_v2`, each with its own sound) are only named in the push when
   `Notifications:AndroidChannelsEnabled` is on (default off), because an app build without those channels would drop
   the pop-up banner. Test with the Push Tester's "Send on the type's Android channel" box on a build that creates
   them: **off** = `high_importance_channel` (device default sound, by design), **on** = the type's `_v2` channel
   (custom sound). Sounds: `docs/notification-sounds.md`. Appearance and sounds are tested on a device: section 10.

## 7. Not covered

- Real APNs delivery on iOS (needs a physical iPhone and the APNs key uploaded to Firebase). The iOS Simulator round in
  section 13 covers the app behaviour only.
- The `app_update` push and the update UI were exercised on the Android emulator (section 11) and the iOS Simulator
  (section 13). The in-app version gate (`GET /api/v1/app/config`: `minimum_required_version`) is still unchecked.
- The admin "Push Broadcast" page (`/Admin/PushBroadcast`) sending to real devices.
- **Silent `app_unblock` on iOS** failed on the Simulator (section 13) and needs a physical iPhone to settle.
- **Stacking** (one banner per booking) fails on a physical Android device: three banners for accept, start and complete
  (section 9). Accepted as a known issue and not pursued; see `docs/flutter-changes.md`.
- Scheduled reminders and payout notifications are not built.

## 8. Android build with icon, channels, live inbox and sounds (2026-10-02, emulator)

Device checklist run on the Android emulator against the new Flutter build (`ic_notification` icon, `_v2` channels,
foreground banners, live colour-coded inbox, bundled sounds). Pushes were sent from the Push Tester. Next round: the
same list on a physical device.

| # | Check | Result |
|---|---|---|
| A1 | System settings lists exactly the four channels (Important notifications, Job requests, Booking updates, Announcements), no old silent duplicates | confirmed |
| A2 | Small icon is the white glyph (not a blank blob), accent colour applied | confirmed, in both the pop-up and the status bar |
| B3-B5 | Job request / booking update / announcement each play their own sound, in foreground, background and swiped away | **not tested**: the emulator has no sound; tested on a physical device on 2026-10-05 (section 10) |
| C6 | Foreground push (app open on the client or provider home) | notification arrives and the unread badge increments. ("On the right channel" in the checklist only meant the banner is posted on the channel named by `channel_id`; it is visible in system settings, not in the UI.) |
| C7, C8 | Several pushes for the same booking leave one banner | **not conclusive**: all three stayed as separate notifications. The Push Tester sends no `booking_id` / `request_id`, so there is no stacking key. Re-tested with a real booking on a physical device: still fails (sections 9 and 10) |
| D9 | Banner time is the event time, not "now" | confirmed |
| D10 | Inbox time matches the local clock | confirmed |
| E11 | Leading emoji in a title renders in the inbox | confirmed |
| E12 | New push appears in an open inbox without a manual refresh, with unread styling | confirmed |
| E13 | Rows are colour-coded by type | confirmed |
| E14 | Client and provider inboxes are separate | confirmed |
| F16, F17 | Tap routing from foreground, background and cold start lands on the screen named by `screen` | confirmed |
| G18 | After logout, no pushes arrive | confirmed |
| G19 | After a role switch, pushes for the old role are not delivered | confirmed |

**Follow-up:** sounds were verified on a physical device and stacking was re-driven with a real booking; both are
recorded in section 10.

## 9. Stacking run with a real booking (2026-10-02, production)

Scripted in `scripts/test-push-stacking.sh` (credentials via `ADMIN_*` / `DB*` environment variables, nothing stored).
Run against `https://sahulatghartak.com` for the dual-role account (user 76 = Client 74 + Provider 35), token registered
as Client.

| Step | Action | Result |
|---|---|---|
| 1 | `POST /api/customer-service-requests` "NOTIF TEST stacking" | request UID 396 |
| 2 | admin portal assign to provider 35 | booking UID 228 (provider-role push, no banner on a Client token) |
| 3 | `respond` accept, then wait 10 s | success, client inbox row "Provider accepted" |
| 4 | `start`, then wait 10 s | success, client inbox row "Job started" |
| 5 | `verify-completion` (passcode read from the DB) | success, client inbox row "Job completed" |

Backend side: all three calls succeeded and the client got exactly three inbox rows in order (booking_accepted,
job_started, job_completed), so every push carried `booking_id` / `request_id` and so the `booking-228` tag.
**Expected on the device:** one banner for the booking, showing "Job completed".
**Device result (tester): FAIL.** Three separate banners, one per stage, did not merge. Not yet diagnosed. The backend
code sets `AndroidNotification.Tag = booking-{id}` whenever `booking_id` is in the data and `NotifyUserAsync` always sends
it, and the Flutter foreground path uses a stable id from the same key, so merging was expected on both paths. Open
questions, in order: (1) was the production build running the tag code (deploy of 208c42e) when the test ran;
(2) was the app foreground or background for each push (a system-drawn banner has the tag but id 0, a Flutter-drawn one
has an id but no tag, so a mix never merges); (3) does the device's launcher/OEM group or ignore tags. Next step: Push
Tester with the same `booking_id` filled in on two sends, once with the app in the background and once in the
foreground, which isolates the tag from the real flow.

Cleanup removed the test rows by explicit ids and counts returned to baseline for requests (15), bookings (7), ledger (7)
and inbox (38). **One side effect:** `AdminNotifications` went from 66 to 65 instead of staying level. The script's
cleanup matched `RelatedEntityUID IN (request, booking)` without the `Type`, and booking 228 collides with an older
request-228 bell row, which was deleted too (not recoverable). The script and section 3 now filter by `Type`.

## 10. Physical Android device round (2026-10-05, release APK)

| Check | Result |
|---|---|
| Install the new build **over** the published one (secure-storage upgrade) | pass: still logged in, no data loss seen |
| Cold-start tap routing (`getInitialMessage`) | pass: lands on the screen named by `screen` / `booking_id` |
| Custom sounds, channel toggle **off** (`high_importance_channel`) | device default sound, as designed |
| Custom sounds, channel toggle **on** (`_v2` channels), first release APK | **fail: silence**. Release resource shrinking had stripped `res/raw/*.ogg`; the channels pointed at missing files. `aapt2 dump resources` on the APK showed no `raw/` entries. |
| Same, after the fix (`android/app/src/main/res/raw/keep.xml`) | **pass: each channel plays its own sound** |
| Stacking with a real booking (accept, start, complete) | **fail**: three banners, again after the app-side tag change. Known issue, accepted, not pursued |

Notes for repeating this:
- **Test the release build.** The debug APK contained the sounds; only release stripped them (`docs/android-build-notes.md`
  section 4 has the cause, the fix and the `aapt2` check).
- **Uninstall the old app before re-testing sounds.** Android never changes a channel after it is created, so phones that
  ran the broken build keep `_v2` channels pointing at missing files until the app is uninstalled (or the channels are
  deleted in system settings).
- Stacking: the foreground path now posts keyed pushes with the same tag and id 0 the system uses for background pushes,
  but the three banners still did not merge. Untested leftovers if it is ever revisited: whether the production backend
  was running the tag code (commit 208c42e) during the test, and whether the device launcher ignores tags. Every stage
  still gets its own banner and inbox row, so nothing is lost.

### iOS test checklist (physical iPhone, not yet run; the Simulator round is in section 13)
Prerequisites (Mac): `flutter pub get`, `cd ios && pod install` (the secure-storage upgrade swapped
`flutter_secure_storage_macos` for `flutter_secure_storage_darwin`), open `ios/Runner.xcworkspace`, confirm
`job_request.wav`, `booking_update.wav`, `announcement.wav` are listed under Runner target > Build Phases > Copy Bundle
Resources (they were registered in `project.pbxproj` by hand), and confirm Push Notifications and Background Modes
(Remote notifications) under Signing & Capabilities. The APNs key must be uploaded in Firebase. `aps-environment` is
`production` for every build configuration, so a debug build from Xcode may not get a token: test via TestFlight / an
archive build, or set a development environment for Debug and Profile.

1. Log in, allow notifications, and confirm the device token row exists (`UserDeviceTokens`, platform `ios`).
2. Each push type plays its **own sound** (`job_request.wav`, `booking_update.wav`, `announcement.wav`; the sound name
   comes from the push) in the foreground, the background and the locked state.
3. Foreground push: the system banner shows (the app draws no local notifications on iOS) and the unread badge updates.
4. Tap routing from foreground, background and cold start.
5. Several pushes for one real booking group under one thread (`thread-id` = `booking-{id}`).
6. Logout stops pushes; a role switch moves them to the other role.
7. The `app_update` broadcast and the version gate (still untested on any platform, section 7).

## 11. app_update carries per-platform version info (added 2026-10-05; first device result 2026-10-06, see below)

An `app_update` push now carries `latest_version`, `store_url` and an informational `platform` in its data map, resolved
per platform (api.txt v3.34). The Push Broadcast form has one editable version field per platform, pre-filled from `AppConfig:Android` / `AppConfig:Ios` plus any saved version (what `GET /api/v1/app/config` returns); a typed version applies to that push only, and the store links come from `AppConfig`. Each field has a **Check store for update** button that reads the store listing and saves the version it finds (cases 9 to 12).
The app blocks itself when the installed version is older than `latest_version`. Other push types must not carry these keys.

Setup: type different versions per platform in the form (e.g. Android `1.0.5`, iOS `1.0.3`) and register at
least one Android and one iOS token (or use two Android devices and check the log). Use **Admin > Push Broadcast**. The Push Tester's `app_update` entry follows the same rule (per-token platform).
Check what arrived with a debug build / `adb logcat` / the FCM data shown in the app, not only the banner.

| # | Case | Expected |
|---|---|---|
| 1 | Android-only broadcast | Android tokens receive `latest_version` = Android value and the Play Store `store_url`; iOS tokens receive nothing |
| 2 | iOS-only broadcast | The reverse: iOS value and App Store URL; Android receives nothing |
| 3 | All platforms, different versions (Android 1.0.5, iOS 1.0.3) | Each platform gets its own version and URL. The server log shows two separate sends (the broadcast result adds both). |
| 4 | Targeted platform's version field is empty or malformed (`1.0`, `1.0.5+3`) | The form shows a clear error naming the platform and **nothing is sent**, also for "All" when only one platform is missing |
| 5 | `app_update` has no inbox row | `UserNotifications` unchanged (count before and after); `notification_id` is empty |
| 6 | Other push types (`booking_accepted`, `job_started`, ...) | No `latest_version`, `store_url` or `platform` key in the data map |
| 7 | Edited version differs from `AppConfig` | The push carries the typed value; `GET /api/v1/app/config` still returns the configured one |
| 8 | `store_url` blank in config | The key is sent as `""` and the app falls back to `GET /api/v1/app/config?platform=...` |
| 9 | Check store for update, App Store | Field fills with the listing's version cut to major.minor.patch (listing "1.0.6 - GPS" -> `1.0.6`); `GET /api/v1/app/config?platform=ios` returns it as `latest_version`; the `Configurations` row `AppConfig.Ios.LatestVersion` exists |
| 10 | Check store for update, Google Play | Same for Android (`AppConfig.Android.LatestVersion`). Best-effort: it reads the Play page, so a Google page change shows an error and saves nothing |
| 11 | Store unreachable / app not found / blank store URL | Red message under the field, field and saved value unchanged |
| 12 | Delete the `AppConfig.*.LatestVersion` rows in Admin > Configurations | The app config and the form fall back to the appsettings value |
| 13 | Installed version vs `latest_version` | Older: full-screen block, Update Now opens the store link, survives a restart. Same or newer: ignored. **Partly run:** older version shows the dialog / block (2026-10-06); the rest not yet checked. iOS Simulator 2026-10-07: block and dialog shown correctly (section 13) |

### force_update (added 2026-10-05; first device result 2026-10-06, see below)

`app_update` also carries `force_update`, exactly `"true"` or `"false"`, set per platform with the **Force update** checkbox next to each version field (default ticked). Unticked, the app shows a dismissable "Update available" dialog (the body is its message) instead of the block. Not saved; `GET /api/v1/app/config` is unchanged.

| # | Case | Expected |
|---|---|---|
| 14 | Broadcast with Force update ticked | the push data has `force_update` = `"true"`; the app blocks. **Run via the Push Tester 2026-10-06: pass** (full-screen block shown) |
| 15 | Broadcast with it unticked | `force_update` = `"false"`; the app shows the dismissable dialog with the body as its text, and "Maybe later" leaves the app usable. **Run via the Push Tester 2026-10-06: pass** for the dialog appearing; "Maybe later" not recorded |
| 16 | All platforms, Android ticked and iOS unticked | two separate sends: Android `"true"`, iOS `"false"` |
| 17 | Every `app_update` push (broadcast and Push Tester) | `force_update` present and exactly `"true"` or `"false"`, never missing or empty |
| 18 | Other types (`booking_accepted`, `job_started`, ...) | no `force_update` key |
| 19 | Push Tester `app_update` | its "force the update" checkbox (default ticked) sets the value the same way. **Pass 2026-10-06** (unticked gave the dialog, ticked gave the block) |
| 20 | `GET /api/v1/app/config?platform=...` | unchanged, still returns its own `force_update` |
| 21 | Installed version same or newer than `latest_version` | nothing shown, whatever `force_update` says |

### Device result: update dialog and force-update block (2026-10-06, Android emulator)

First on-device check of the app's update UI, sent to one device so no real users were involved.

- **Target:** user 76, Provider token (`UserDeviceTokens` id 20, android). The emulator app was build **1.0.4**.
- **Sender:** the admin Push Tester, `app_update`, device mode, run on a local copy of the API (same production database and
  Firebase). The Push Tester has no version field, so a POST-only `LatestVersionOverride` of **1.0.7** was added to it for
  this run; it applies to that one send and saves nothing, so the live `latest_version` stayed `1.0.4` (a saved 1.0.7
  would have prompted every real user). Save to inbox was off.
- **Send 1, `force_update` = false:** server log `recipients=1, sent=1, failed=0`. The emulator showed the dismissable
  "Update available" dialog.
- **Send 2, `force_update` = true:** server log `recipients=1, sent=1, failed=0`. The emulator showed the full-screen
  force-update block.
- **Result:** pass for both. Cases 14, 15 and 19 are confirmed; the push data carried the right `force_update` value for
  the app to choose the dialog or the block.
- **Not yet checked:** "Update Now" opening the Play Store link, "Maybe later" leaving the app usable, the block
  surviving a restart, an installed version equal to or newer than `latest_version` showing nothing (case 21), the
  all-platforms and iOS cases, and the store-check buttons (cases 9 to 12).
- **Follow-up:** the `LatestVersionOverride` change in `PushTesterController` / `PushTesterViewModels` is local and
  uncommitted. Decide whether to keep it as a documented test aid.

## 12. Getting out of a test block (device stuck on the update screen)

A forced `app_update` (`force_update` = `"true"`) is persisted on the device in secure storage
(`pending_update_version`, `pending_update_store_url`) and is cleared only when the installed version reaches the stored
one. A test push with a version above the installed build therefore locks the device until one of the fixes below. Real
users leave the block by updating, or staff release it with the silent `app_unblock` push (`api.txt` v3.38, "Admin release
of update blocks"; the app side is built, the backend and its control on the Push Broadcast page are not yet). Once the
backend ships, run these on a blocked device first, and fall back to the fixes below only if the push did not land:

| # | Case | Expected |
|---|---|---|
| 22 | Block a test device (Push Tester, `force_update` ticked, version above installed), then send "Release" to that device | the block screen disappears with no banner, sound or inbox row; relaunching the app stays unblocked |
| 23 | Release with the app swiped away (background handler) | the next launch is not blocked |
| 24 | Send an `app_update` with a `sent_at` earlier than the release (resend an old one) | ignored; a new `app_update` sent after the release blocks again |
| 25 | Release scopes: one device, one user, Android, iOS, everyone | only the chosen devices are released; history row written with admin, scope, reason and counts |
| 26 | Release with an empty reason / no matching devices | rejected with a message, nothing sent or written |
| 27 | A build without this change receives `app_unblock` | ignores it (stays blocked); fix with section 12 below |

Result so far: cases 22 to 27 were verified on the Android emulator (2026-10-06). On the iOS Simulator (2026-10-07) the
release did **not** clear the block (section 13).

- **Android emulator (done 2026-10-06):** clear the app data, then relaunch. Package id `com.coditiumsols.sahulatghartak`,
  `adb` lives at `D:\ryDevelop\Android\Sdk\platform-tools\adb.exe` on this machine:
  ```
  adb devices
  adb -s emulator-5554 shell pm clear com.coditiumsols.sahulatghartak
  adb -s emulator-5554 shell monkey -p com.coditiumsols.sahulatghartak -c android.intent.category.LAUNCHER 1
  ```
  This also logs the account out. If `flutter run` was attached, restart it.
- **Android physical device:** Settings > Apps > Sahulat Ghar Tak > Storage > **Clear data** (logs out), or the same
  `adb shell pm clear` command over USB debugging. Plain uninstall and reinstall also works.
- **iOS physical device:** deleting the app is **not reliable**, because the iOS Keychain can survive an uninstall, so
  the stored requirement may come back after a reinstall. Instead install a build whose `version:` in `pubspec.yaml` is at
  or above the pushed `latest_version` (Xcode run or TestFlight); the app clears the stored requirement on launch. The
  same trick works on Android and the emulator: `flutter run` a build with a high enough version.
- **Prevent it:** send test pushes only to a single device (Push Tester, device mode) with a `latest_version` you are
  willing to be stuck on, or use `force_update = false` (dialog only, nothing persisted). Never save a higher `AppConfig`
  `latest_version` for testing, since that blocks real users through the splash check as well.

## 13. iOS Simulator round (2026-10-07)

Run on the iOS Simulator with the iOS app (bundle id `com.coditiumsols.sahulatghartak.ios`, build `1.0.6+10`). Xcode
settings checked beforehand: the three `.wav` files and `GoogleService-Info.plist` in Copy Bundle Resources, signing team,
Push Notifications and Background Modes (Remote notifications); the APNs key is uploaded in Firebase. The Simulator cannot
receive a real APNs push, so this round proves the app's behaviour, not APNs delivery.

| Check | Result |
|---|---|
| Notifications show up, each type with its sound | pass |
| `app_update` with `force_update` = `"false"`: dismissable dialog | pass |
| `app_update` with `force_update` = `"true"`: full-screen block | pass |
| Silent `app_unblock` clears a forced block (cases 22, 25) | **fail**: several retries, the Simulator stayed blocked |
| Silent `app_unblock` sent from the production admin page (admin: "1 delivered to FCM, 0 failed") | **fail**: nothing in the console, block stayed |
| Local `xcrun simctl push` of a **visible** alert carrying `type: app_unblock` (payload via stdin) | **pass**: banner, console line `Update block: app_unblock received, releasing`, block cleared |
| Local `xcrun simctl push` of the **silent** form (`content-available: 1`, no alert) | **fail**: no log line, block stayed |

**What this means**
- The notification system is verified end to end on Android (emulator and physical device) and, for in-app behaviour,
  on the iOS Simulator. It is **not** yet confirmed on a physical iPhone, so real APNs delivery, sounds through APNs and
  background delivery are still open (checklist in section 10).
- `app_unblock` on iOS is the one failure. It is a background push (`apns-push-type: background`, `content-available: 1`,
  no alert). iOS may drop or throttle those, and the Simulator does not receive real pushes, so the failure is not yet
  attributed to the app or the backend. Next step: send it to a physical iPhone and read the device log; if it still
  fails there, check the payload flags against `api.txt` v3.39 and the app's background handler.
- **Update 2026-10-07:** the pushes were sent from the production API, and Xcode 14+ Simulators on Apple silicon / T2 Macs
  do receive real APNs sandbox pushes, so delivery of visible pushes is expected there. Silent background pushes are
  reported as unreliable on the Simulator. One app gap was found and fixed: a release handled by the background push
  handler (separate isolate) cleared storage but not the running app's in-memory block, so the block stayed until a cold
  start. The app now re-reads the stored block on resume (`UpdateBlock.syncFromStore`), and logs `app_unblock received`
  in the foreground and background paths. **Still to confirm:** run the Simulator with the app blocked in the foreground,
  send the release, and watch the console: the log line means delivery works, no line means the push never arrived
  (check the payload has `content-available: 1` and `apns-push-type: background`, then try a physical iPhone).
- **Conclusion 2026-10-07:** the app's release logic is correct on iOS (the visible-form test proves it). The silent form
  does not reach Dart on the Simulator, either from production or from local `simctl`. The Simulator also delivered real
  pushes late and in bursts (all arriving together, straight into the inbox with no banner), so Simulator delivery is not
  representative. Silent iOS pushes are best effort (the admin page says so), so a pull-based backstop is proposed: the
  app checks the latest release time on launch and resume. Silent delivery itself still needs a physical iPhone.
- Testing note: `xcrun simctl push booted <bundle-id> - <<EOF ... EOF` takes the payload on stdin; stale or unsaved
  `.apns` files caused several false results. The payload needs `gcm.message_id` or FlutterFire ignores it.
- The stuck Simulator was freed by building with a `pubspec.yaml` version at or above the pushed `latest_version`
  (section 12), then restoring the version.
