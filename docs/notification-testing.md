---
status: current
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
  iOS cannot be tested on a simulator (needs a physical device and an APNs key).
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
5. `AdminNotifications` whose `RelatedEntityUID` is a test request or booking

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
is delivered and the banner shows, and works correctly. Not separately confirmed: that tapping it from this state
routes to the screen named in `screen` / `booking_id` (the Flutter agent's `getInitialMessage` handling).

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
   banner (iOS: thread id), and `channel_id` in the data payload. Android channels (`job_requests`, `booking_updates`,
   `announcements`) are only named in the push when `Notifications:AndroidChannelsEnabled` is on (default off), because
   an app build without those channels would drop the pop-up banner. Test with the Push Tester's "Send on the type's
   Android channel" box on a build that creates them. Sounds are prepared but not chosen: `docs/notification-sounds.md`.

## 7. Not covered

- Real delivery on iOS (needs a physical device, APNs key uploaded to Firebase).
- **The App Update notification was not tested.** This covers the `app_update` push (staff broadcast from
  `/Admin/PushBroadcast`, `screen = app_update`) and how the app reacts to it, plus the in-app version gate
  (`GET /api/v1/app/config`: update prompt, forced update, `minimum_required_version`). Only the booking-lifecycle
  pushes were exercised on the emulator.
- The admin "Push Broadcast" page (`/Admin/PushBroadcast`) sending to real devices.
- Cold-start tap **routing** (`getInitialMessage`): cold-start delivery works (section 5), but the tap landing on the
  right screen was not confirmed.
- Scheduled reminders and payout notifications are not built.
