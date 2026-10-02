---
status: current
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

## Notification appearance, channels and sounds - remaining verification

Implemented in the app (2026-10-02) and confirmed on the Android emulator (results in `docs/notification-testing.md`
section 8): the `ic_notification` small icon and accent colour (pop-up and status bar), the Android channels, foreground
banners, event times from `sent_at` / the inbox `createdAt` in local time, and a live, colour-coded inbox with clear
unread styling (emoji titles, live updates, separate client/provider inboxes, tap routing from foreground, background
and cold start, no pushes after logout or for the old role after a role switch).

Still open:
- **Sounds on a physical Android device.** Emulator has no sound. Each channel must play its own sound in foreground,
  background and cold start, and the three must sound different.
- **One banner per booking.** The Push Tester sends no `booking_id`/`request_id`, so the emulator test could not show
  stacking. Re-test with a real booking (accept, start, complete) and confirm one banner remains.
- **Channels live:** pushes land on the `_v2` channels once `Notifications:AndroidChannelsEnabled` is on (use the Push
  Tester's channel option to force one send first).
- **Repeat the Android list on a physical device**, including the locked-screen and swiped-away cases.
- **Xcode:** open `ios/Runner.xcodeproj` once and confirm the three `.wav` files show under Build Phases > Copy Bundle
  Resources (added by hand in `project.pbxproj`).
- **iOS delivery** on a physical device (no app code needed: thread-id grouping and sounds are backend-side; the app
  draws no local notifications on iOS).
- Optional: design may want a different `ic_notification` mark (a simplified drawing of the logo today); swap the PNG in
  the five `drawable-*` folders.

### Sounds and channel ids (implemented)
The three `.ogg` files are in `android/app/src/main/res/raw/` and the three `.wav` files in `ios/Runner/`; the source
copies stay in `assets/notification-sounds/` (not a declared Flutter asset, so not bundled twice). Channels carry their
sounds under **new ids**, because a channel's sound is fixed once it exists on a device: `job_requests_v2`
(`job_request`), `booking_updates_v2` (`booking_update`), `announcements_v2` (`announcement`), next to
`high_importance_channel`. `init()` deletes the earlier silent `job_requests` / `booking_updates` / `announcements`. The
foreground banner accepts `data['channel_id']` with or without `_v2`, falling back to `booking_updates_v2`. If a sound
ever changes, mint a new channel id again. Backend constants and the Push Tester use the `_v2` ids (api.txt v3.33).

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

### 2. Notification tap routing (RESOLVED)
Confirmed on the emulator (2026-10-02): tapping a banner routes by `screen`, `booking_id` and `request_id` from the
foreground, the background (`onMessageOpenedApp`) and a cold start (`getInitialMessage`). The backend no longer sends an
Android click action, so no manifest intent filter is needed.

### Verified working
Provider and client booking pushes arrive and route on tap. The banner time reads the event time (the backend sets an
explicit event time and `sent_at`), and the inbox shows local time.
