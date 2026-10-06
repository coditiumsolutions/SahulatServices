---
status: current
version: 1.10.0
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

## Admin release of update blocks (`app_unblock`) - app done, backend pending (api.txt v3.38)

A forced-update block lives on the device, so staff need a server-side way to lift a wrong or test one. The app already
handles a silent, data-only `app_unblock` push (`type`, `sent_at`): it deletes the stored block and any queued prompt,
shows nothing, and remembers `sent_at` as "cleared at" (secure storage `update_block_cleared_at`). An `app_update` whose
`sent_at` is not later than that is ignored, also in the background handler; one without `sent_at` is never ignored.
Code: `lib/utils/update_block.dart`, `lib/services/push_notification_service.dart`, tests in `test/update_block_test.dart`.

Backend built (2026-10-06, api.txt v3.39 "Admin release of update blocks"): the silent push, and its control on the admin
Push Broadcast page near the bottom (scope, mandatory reason, confirmation, history, audit). There is deliberately no
mobile endpoint. Verified end to end on the Android emulator (block, release, stale `app_update` ignored, fresh one blocks
again): `docs/notification-testing.md` section 12. Still open for the app side: case 23 (release while swiped away), case 27
(older build) and iOS on a physical device.

---

## Notification appearance, channels and sounds - remaining verification

Still open:
- **Sounds on iOS: statically checked, not yet heard.** The three `.wav` files in `ios/Runner/` are 16-bit PCM, 1 to 3
  seconds (iOS requires linear PCM / IMA4 / mu-law / a-law and under 30 s), they are in the Runner group and in the
  Resources build phase, and iOS has no resource shrinking that could strip them. Still to do on a physical iPhone:
  open the project in Xcode once to confirm Build Phases > Copy Bundle Resources lists them, then confirm each push type
  plays its own sound (the sound name comes from the push, e.g. `job_request.wav`; the app draws no local notifications
  on iOS).
- **Xcode:** open `ios/Runner.xcodeproj` once and confirm the three `.wav` files show under Build Phases > Copy Bundle
  Resources (added by hand in `project.pbxproj`).
- **iOS delivery** on a physical device (no app code needed: thread-id grouping and sounds are backend-side; the app
  draws no local notifications on iOS).
- Optional: design may want a different `ic_notification` mark (a simplified drawing of the logo today); swap the PNG in
  the five `drawable-*` folders.