# Flutter App Changes Tracker

Running checklist of backend changes that the Flutter app needs to adopt to complete a feature's integration, or that are being deliberately held back as breaking changes pending approval. Updated incrementally as each backend feature lands — **`api.txt` (repo root, currently v3.21) is the exact, authoritative request/response contract of every endpoint referenced below**; read the cited `api.txt` section before implementing, since this file only summarizes.

Sections are removed once the Flutter app has fully adopted them — this file tracks *pending/active* work, not a history of everything ever shipped. Completed feature history lives in git log and `api.txt`'s own version notes, not here.

**Standing constraint (2026-09-21):** the Flutter app is live on the Play Store and App Store, both of which have review/approval lag, while the backend/API can be updated instantly. Every backend change in this project is therefore built to be **additive and optional** — a currently-published app build must keep working completely unchanged against the updated backend, with zero risk of breakage while store approval for the new app version is pending. New fields on existing endpoints are nullable/optional with a legacy fallback; new endpoints are new routes an old app simply never calls. Nothing here is a hard cutover.

Legend:
- **Available now** — backend is live, app can adopt whenever convenient (non-breaking, optional).
- **Held for approval** — a genuinely breaking change to a live endpoint contract (not just optional-field additions) that hasn't been implemented at all yet; listed here so the scope is visible ahead of time. Per the constraint above, when these are eventually implemented they should also default to an optional/additive interim contract rather than a hard break, unless explicitly decided otherwise at that time.
- **TODO(remove after old app retired)** — inline code/doc comments marking legacy-fallback branches that exist ONLY to support currently-published app builds. Once the new app version is confirmed live on both stores (i.e. no meaningfully active install base still hits these code paths), these branches can be deleted — grep the codebase for this exact marker to find all of them. Do not remove any of these until that confirmation, even if it looks safe.

---

## Service Bookings: Scheduled Date/Time

**Status: backend ready and live (2026-09-24); Flutter provider app not started.**

`GET`/`POST`/`PUT /api/service-bookings` and `POST /api/service-bookings/{bookingUid}/respond`
now return `preferredServiceDate` (string, `"yyyy-MM-dd"`, nullable) and `preferredServiceTime`
(string, `"HH:mm"`-style free text, nullable) on every booking object — sourced server-side from
the linked `CustomerServiceRequests` row (via `RequestUid`), not stored on `ServiceBookings`
itself. See `api.txt`'s `SERVICE BOOKINGS APIs` section for the exact response shape. Purely
additive — no existing field renamed/removed, no input contract change; a currently-published app
build that doesn't read these two fields is unaffected.

Both fields can be `null` (the customer's original request may have left them blank). Neither is
a UTC instant — pass through/display as-is, no timezone conversion.

Build spec:
1. Add `preferredServiceDate` (`String?`) and `preferredServiceTime` (`String?`) to
   `lib/models/provider/service_booking.dart`'s `ServiceBooking` model (nullable, matching how
   the model already handles other optional fields).
2. Requests tab and Bookings tab cards: add a "Scheduled" row showing the date/time when present
   (e.g. `Scheduled: 22 Jul, 2:30 PM` — format `preferredServiceDate` for display, show
   `preferredServiceTime` as-is or format it as needed); omit the row entirely when both are null,
   so a booking with no preferred schedule doesn't show an empty/placeholder row.
3. `booking_detail_screen.dart`: same "Scheduled" row in the detail view, same null-handling.
4. No app-version gate needed — new optional response fields only, works against both old and new
   app builds with no coordination required.
