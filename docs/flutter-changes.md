---
status: current
---

# Flutter App Changes Tracker

Running checklist of backend changes that the Flutter app needs to adopt to complete a feature's integration, or that are being deliberately held back as breaking changes pending approval. Updated incrementally as each backend feature lands — **`api.txt` (repo root, currently v3.32) is the exact, authoritative request/response contract of every endpoint referenced below**; read the cited `api.txt` section before implementing, since this file only summarizes.

Sections are removed once the Flutter app has fully adopted them — this file tracks *pending/active* work, not a history of everything ever shipped. Completed feature history lives in git log and `api.txt`'s own version notes, not here.

**Standing constraint (2026-09-21):** the Flutter app is live on the Play Store and App Store, both of which have review/approval lag, while the backend/API can be updated instantly. Every backend change in this project is therefore built to be **additive and optional** — a currently-published app build must keep working completely unchanged against the updated backend, with zero risk of breakage while store approval for the new app version is pending. New fields on existing endpoints are nullable/optional with a legacy fallback; new endpoints are new routes an old app simply never calls. Nothing here is a hard cutover.

Legend:
- **Available now** — backend is live, app can adopt whenever convenient (non-breaking, optional).
- **Held for approval** — a genuinely breaking change to a live endpoint contract (not just optional-field additions) that hasn't been implemented at all yet; listed here so the scope is visible ahead of time. Per the constraint above, when these are eventually implemented they should also default to an optional/additive interim contract rather than a hard break, unless explicitly decided otherwise at that time.
- **TODO(remove after old app retired)** — inline code/doc comments marking legacy-fallback branches that exist ONLY to support currently-published app builds. Once the new app version is confirmed live on both stores (i.e. no meaningfully active install base still hits these code paths), these branches can be deleted — grep the codebase for this exact marker to find all of them. Do not remove any of these until that confirmation, even if it looks safe.

---

## Notification appearance, channels and sounds - remaining verification

Implemented in the app (2026-10-02): the `ic_notification` small icon and accent colour, the `job_requests` /
`booking_updates` / `announcements` channels (now the `_v2` ids with sounds, see below, next to `high_importance_channel`), foreground banners that use
`data['channel_id']`, show the title/body as received with `BigTextStyle`, and replace the earlier banner for the same
booking or request, event times taken from `sent_at` / the inbox `createdAt` in local time, and a live,
colour-coded inbox with clear unread styling. Still to confirm on a device:
- The white house glyph looks right in the status bar and banner (it is a simplified drawing of the logo; swap
  `ic_notification.png` in the five `drawable-*` folders if design wants a different mark).
- Two pushes for the same booking leave one banner in the shade (foreground), and a leading emoji renders in both the
  banner and the inbox.
- Foreground banner time reads the event time, not the receive time.
- Pushes land on the new `_v2` channels once `Notifications:AndroidChannelsEnabled` is on (use the Push Tester to force a
  channel for one send first).
- iOS: nothing to build (thread-id grouping and default sound are backend-side); confirm on a physical device.

### Sounds: bundled, needs device verification
Implemented (2026-10-02): the three `.ogg` files are in `android/app/src/main/res/raw/`, the three `.wav` files are in
`ios/Runner/` and registered in the Runner target (Copy Bundle Resources, edited in `project.pbxproj` by hand: open the
project in Xcode once and confirm they show under Build Phases > Copy Bundle Resources). The source copies stay in
`assets/notification-sounds/` (not a declared Flutter asset, so they are not bundled twice).

Channels now carry their sounds, under **new ids** because a channel's sound is fixed once it exists on a device:
`job_requests_v2` (`job_request`), `booking_updates_v2` (`booking_update`), `announcements_v2` (`announcement`).
`init()` deletes the earlier silent `job_requests` / `booking_updates` / `announcements`. The foreground banner accepts
`data['channel_id']` with or without the `_v2` suffix, falling back to `booking_updates_v2`.

- **Backend owner:** the `NotificationChannels` constants and the Push Tester must send the `_v2` ids (without it,
  background pushes naming the old ids fall onto a generic channel with no sound and no pop-up).
- iOS foreground banners need no app code: the app does not draw local notifications on iOS, so the system plays the
  sound named in the push itself.
- **Verify:** each channel plays its own sound in foreground, background and cold start, and the three sound different.
  Existing test devices keep the old silent channels until the new build runs once (they are deleted then).

### Rollout note (backend side, for the owner)
`Notifications:AndroidChannelsEnabled` is **off**. If it were on before this build is installed, an older app build would
receive pushes naming channels it does not have, and Android would drop them on a generic channel with no pop-up banner.
Switch it on only after this build is live, accepting that anyone still on an older build loses the pop-up until they
update (or after the version gate has forced the update). Until then pushes arrive on `high_importance_channel` exactly
as today.

---

## Testing findings

Issues found while testing the notification system end to end (2026-10-01 and 2026-10-02). Things the Flutter app must
fix or confirm.

### 1. Role switch re-registers the device token (RESOLVED - no Flutter change needed)
The first test appeared to show the token staying on the old role after "switch to customer". It was a backend bug,
not a Flutter gap: register-token rejected userType = "Client" for upgraded (dual-role) accounts with 400
"Unknown or inactive user". Fixed in the backend. Re-tested against the fixed backend: the app calls register-token on
the role switch and the token row moved from Provider to Client. Keep this behaviour (also re-register on login and on
onTokenRefresh, always with the role the app is currently in).

### 2. Notification tap routing (to confirm)
- Tapping a banner opens the app (backend fix: pushes no longer carry an Android click action, so no manifest intent
  filter is needed), including when the app was swiped away (cold start delivery works).
- Not yet verified: that the app routes using data['screen'], booking_id and request_id via onMessageOpenedApp (app
  in background) and getInitialMessage() (cold start).

### Verified working
Provider and client booking pushes arrive and tapping them opens the app. The banner time now reads "Now" (the backend
sets an explicit event time).
