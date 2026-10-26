# Web parity decisions

Decisions made with the owner while reviewing [web-parity-spec.md](web-parity-spec.md). Implementation status is in the table at the end.
Question IDs (Q#) and risk IDs (R#) refer to the spec. Security and endpoint protection are out of scope here; a separate report covers them.

## Decided

| # | Topic | Decision | Spec ref |
|---|---|---|---|
| D1 | Provider job workflow | **Full parity.** Requests (accept/reject), bookings, start, cancel, complete with passcode and amounts, wallet, profile, documents, categories. | Part C, section 3.6 |
| D2 | Notifications | **Inbox + polled bell.** Navbar bell with unread count refreshed about every 60 s, inbox page (mark read, read all, tap routes to the target page). No web push. | Q1, section 3.10 |
| D3 | Delete a finished request | **No delete on web.** Completed and Cancelled requests stay in their tabs. | Q3, R5 |
| D4 | Document capture | **File picker + browser crop + server limits.** jpg/png, 5 MB, client-side downscale, crop ratios photo 1:1 and CNIC 85.6:54. No face or CNIC detection (admin review instead). | Q7, section 8 |
| D5 | Provider onboarding | **Skip the "Become a Provider" upgrade screen** (P1). Web signup already has a Provider account type. Flow: signup, login (no OTP, see D13), documents upload, Verification Pending, dashboard once verified. Clients cannot upgrade on the web. | R24, P1 |
| D6 | Dual-role accounts | **Add a role switcher** in the navbar between `/client/*` and `/provider/*`. Active role kept in the session; the inbox and unread count use the active role. | section 5 |
| D7 | Customer reviews on Home | **Remove it.** No fabricated reviews. | R1, Q5 |
| D8 | Provider availability | **Explicit Save / Cancel.** Online toggle opens a times form; Cancel changes nothing, Save sends the chosen times. No ambiguous Skip. | R10, Q8 |
| D9 | Rejected requests screen | **Skip.** Rejected offers just disappear, as the API hides them. | Q4, P12 |
| D10 | Verified documents | **Follow the app, plus a warning.** Lock verified documents; allow CNIC replacement behind a clear warning that it resets verification. | R19, Q10 |
| D11 | Provider edit extras | **Add Description and service zones** (max 2, from the zones API) to provider profile edit. | Q6, Q9 |
| D12 | Maps | **Embedded map plus an "Open in Google Maps" link** on booking detail and on address pin selection, with search and reverse geocoding through the existing API. | P15, C8, C9 |
| D13 | OTP | **Not on the website for now.** No OTP screens or calls in the client or provider pages (S5 OTP, S6 Forgot Password, S7 Reset Password, and the OTP step after login or signup). The OTP stays in the backend and the app, unchanged. Web signup keeps creating accounts as verified (`PublicAccountController` sets `IsVerified = true`, "Web signup has no OTP step"). It can be added later. | R2, S5-S7 |
| D14 | Route names | **Web routes may differ from the app's.** Keep the current web routes (e.g. `/signup`, `/client/dashboard`, `/provider/dashboard`); new pages follow the existing `/client/...` and `/provider/...` pattern. The spec's proposed routes are suggestions only. | section 1 |
| D15 | Session length | **Keep the 8-hour sliding cookie; "Remember me" extends it to about 30 days.** | R6, section 5 |
| D16 | Provider live updates | **Poll every 30 s.** The Requests list refreshes itself every 30 seconds while the page is open (paused when the tab is hidden or a dialog is open). The notification bell polls every 60 s (D2). | Part C |
| D17 | Contact Us | **No separate page.** The footer already has Contact Us. S8 is covered. | S8 |
| D18 | Delete account | **Add a "Delete account" option to each profile** that asks for the password, as in the app. The public `/delete-account` page stays as is. | S10, section 3.11 |
| D19 | Shared deep links | **Map `/app/requests/{id}`, `/app/bookings/{id}` and `/app/notifications` to the matching web pages for logged-in users**, at the end of the build. Everyone else still goes to the download page. | R22 |
| D20 | Session claims | **Carry both the client and provider ids in the cookie claims** (needed by the role switcher, D6). | section 5 |
| D21 | Unverified accounts at login | **Allow them for now.** The web has no way to verify (D13), so login does not check `IsVerified`. **Gap to rectify once OTP is in place:** block or redirect unverified accounts to OTP, as the app does. | L1 in the audit |

## Consequences for the screen inventory

- P1 (Provider Registration upgrade): **dropped**.
- P12 (Rejected Requests): **dropped**.
- P7 (live camera): stays SKIP; replaced by file picker and crop (D4).
- S5, S6, S7 (OTP, forgot password, reset password): **deferred**. Forgot/reset password need OTP, so web users can't reset a password until OTP is added.
- S9 notifications inbox: build for both roles, with polling (D2).
- New: role switcher in the navbar (D6).
- C1 Home: no reviews section (D7).

## Implementation status

| # | Status |
|---|---|
| D1 | Done: requests, bookings, start, cancel, passcode completion, wallet, profile, documents, categories. |
| D2 | Done: inbox and bell, polled every 60 s. No web push. |
| D3 | Done: no delete on web. |
| D4 | Done: file picker, browser crop (Cropper.js), downscale, server limits. |
| D5 | Done: signup then documents then Verification Pending then dashboard. |
| D6 | Done: switch button in the second navbar for accounts that have both profiles. |
| D7 | Done: reviews section not added. |
| D8 | Done: explicit Save/Cancel in the availability dialog. |
| D9 | Skipped, as decided. |
| D10 | Done: police certificate locks once verified; CNIC replacement needs confirmation and resets verification. |
| D11 | Done: description and zones (max 2) on provider profile edit. |
| D12 | Provider booking detail done (embedded OpenStreetMap plus Directions link). Client address pin done (Leaflet map with search, click/drag pin, "use my location" and reverse geocoding on the address form; not yet browser-tested). |
| D13 | Followed: no OTP screens or calls on the web. |
| D14 | Followed: web routes differ from the app's. |
| D15 | Done: 8 hours, or 30 days with Remember me. |
| D16 | Done: 30 s polling on Requests. |
| D17 | Followed: footer Contact link only. |
| D18 | Built but switched off: buttons hidden, endpoints return 404 (flag `DeleteAccountEnabled`). |
| D19 | Not started. |
| D20 | Done: `ClientId` and `ProviderId` claims, plus the active role. |
| D21 | Followed. Gap to fix once OTP exists. |

## Still open

- D21 follow-up: enforce `IsVerified` at web login when OTP is added.
- Q2: server-to-server credential vs user token if the API gets JWT auth.
- R2: when OTP is added to the web later, it must never render `data.otp` (backend should set `Otp:IncludeInResponse=false`).
- R11: the customer Terms & Conditions text in the app is still truncated; the website now has the owner's interim text (`/terms-and-conditions`), to be updated later.
- R22 / D19: map `/app/...` deep-link paths to the web pages for logged-in users instead of the download page.
- Guest browsing of services (audit D8).
- "Provider cancelled" notice (audit M6) is done: the last-seen stage per request is kept in a per-browser cookie (`sgt_req_seen_<clientId>`, `RequestBounceTracker`), so it does not follow the client to another browser. Unlike the app, the notice stays until the request is assigned again instead of showing once.
- Time format: the per-user switch is a browser cookie; move it to the account if it should follow the user across devices.
