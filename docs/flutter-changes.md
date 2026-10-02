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

## Notification appearance, channels and sounds (Available now, channels gated)

Backend is done (api.txt v3.32, "Push payload contract"). The Flutter app needs the following. Items 1-3 are safe to
ship at any time; the backend will not name the new Android channels until `Notifications:AndroidChannelsEnabled` is
switched on (see the note at the end).

### 1. Small notification icon (the "app icon not showing" fix)
Android draws the small status-bar/banner icon as a flat one-colour silhouette. With no icon set it falls back to the
full-colour launcher icon, which turns into a blank or grey blob (seen on the emulator). Fix:
- Add `ic_notification` as a **white glyph on a transparent background** (the Sahulat mark, simplified, no colour, no
  gradient) to `android/app/src/main/res/drawable-mdpi` 24px, `-hdpi` 36px, `-xhdpi` 48px, `-xxhdpi` 72px,
  `-xxxhdpi` 96px.
- `android/app/src/main/res/values/colors.xml`: add `<color name="notification_accent">#003366</color>`.
- `AndroidManifest.xml`, inside `<application>`, next to the existing channel-id meta-data:
  `com.google.firebase.messaging.default_notification_icon` = `@drawable/ic_notification` and
  `com.google.firebase.messaging.default_notification_color` = `@color/notification_accent`.
- `push_notification_service.dart`: initialise `AndroidInitializationSettings('ic_notification')` instead of
  `'@mipmap/launcher_icon'`, and pass `color: const Color(0xFF003366)` in the foreground `AndroidNotificationDetails`.

### 2. Android channels (needed for sounds and for per-type importance)
Sound and importance are channel settings on Android 8+, so they cannot vary per push. Create these in `init()`, next to
the existing `high_importance_channel` (keep that one: it is the manifest default and the fallback):

| id | Name shown in system settings | Importance | Used for |
|---|---|---|---|
| `job_requests` | Job requests | `Importance.high` | new job for a provider |
| `booking_updates` | Booking updates | `Importance.high` | accepted, started, completed, cancelled, reassigning |
| `announcements` | Announcements | `Importance.defaultImportance` | app update broadcast |

Each push carries `data['channel_id']` with one of those ids (api.txt). Do not set a custom sound yet (see item 6).

### 3. Foreground notifications must match the background ones
In `_onForegroundMessage`, when showing the local notification:
- use the channel `data['channel_id']` (fall back to `booking_updates`);
- show `notification.title` / `notification.body` exactly as received (titles can start with one emoji, which is
  intended; do not strip it) with `BigTextStyle` so long bodies can expand;
- use a **stable notification id** derived from the stacking key: `booking-{booking_id}` if `booking_id` is not empty,
  else `request-{request_id}`, else a counter. A newer push for the same booking then replaces the earlier banner
  instead of stacking, which is what the system does for background pushes (the backend sends the same key as the
  Android tag and the iOS thread id);
- keep `payload: jsonEncode(data)` so taps still route.

### 4. Times shown inside the app
Use `data['sent_at']` (ISO 8601 UTC) for any notification time you render, and the inbox `createdAt` (now sent with a
`Z`, so it parses as UTC), converted to local time. Never use the time the device received the message.

### 5. iOS
Nothing visual to build: the backend sends a `thread-id` per booking (iOS groups the notifications) and the default
sound. Custom sounds come later (item 6).

### 6. Sounds: not yet, but prepared
No sounds are chosen yet, so do nothing for sounds now. When the files exist, follow `docs/notification-sounds.md`
(put the Android files in `res/raw/`, the iOS files in the Runner bundle, then give each channel a `sound`). Remember an
Android channel's sound is fixed once it exists on a device: if a sound ever changes, create a new channel id.

### Rollout note (backend side, for the owner)
`Notifications:AndroidChannelsEnabled` is **off**. If it were on before this build is installed, an older app build would
receive pushes naming channels it does not have, and Android would drop them on a generic channel with no pop-up banner.
Switch it on only after this build is live, accepting that anyone still on an older build loses the pop-up until they
update (or after the version gate has forced the update). Until then pushes arrive on `high_importance_channel` exactly
as today. The Push Tester page can force a channel for one test send to try the new build first.

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
