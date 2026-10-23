# Web parity decisions

Decisions made with the owner while reviewing [web-parity-spec.md](web-parity-spec.md). Nothing here is implemented yet.
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
| D16 | Provider live updates | **Undecided.** Polling the Requests tab (about every 30 s) vs manual refresh is to be decided later. The notification bell polls regardless (D2). | Part C |
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

## Still open (not yet asked)

- D21 follow-up: enforce `IsVerified` at web login when OTP is added.

- Q2: server-to-server credential vs user token if the API gets JWT auth.
- R2: when OTP is added to the web later, it must never render `data.otp` (backend should set `Otp:IncludeInResponse=false`).
- R11: the full customer Terms & Conditions text is truncated in the app. The owner will provide it; not needed yet.
- D16: provider Requests tab refresh (polling vs manual).
- R22: map `/app/...` deep-link paths to the web pages for logged-in users instead of the download page.
- Q11 / section 10: audit the existing web login, signup, request flow and My Requests against the app behaviours listed in the spec.
