# Web parity audit: existing pages vs the app

Compares the pages already on the website with [web-parity-spec.md](web-parity-spec.md), applying the choices in [web-parity-decisions.md](web-parity-decisions.md). The gap tables below are the original review; the **Status** sections at the end say what has since been built.
Severity: **High** = wrong behaviour or data, **Med** = missing app behaviour, **Low** = copy or polish.

## Login (`/login`)

| # | Gap | App behaviour | Web today | Sev |
|---|---|---|---|---|
| L1 | Unverified accounts | App routes to OTP. | Web login has no `IsVerified` check, so an unverified app account can log in on the web. Per D13 (no OTP on web), decide whether to block, allow, or show a message. | High |
| L2 | Provider landing | Verified provider goes to the dashboard, unverified goes to Verification Pending. | Always the provider dashboard. | Med |
| L3 | Dual-role claims | Both ids kept in the session (D20). | Cookie holds a single `ProfileId` for the login role. | Med |
| L4 | Remember me | D15: 8 h, or about 30 days with Remember me. | 8 h, or 14 days with Remember me. Change 14 to 30 days. | Low |
| L5 | Error copy | `Invalid mobile number or password.`, `Account is inactive. Contact support.` | Same texts. Matches. | OK |
| L6 | Mobile mask and dash stripping | `03XX-XXXXXXX`, exactly 11 digits. | Matches. | OK |
| L7 | Forgot password link | Present in app. | Absent; deferred by D13. | Deferred |

## Signup (`/signup`)

| # | Gap | App behaviour | Web today | Sev |
|---|---|---|---|---|
| S1 | Gender | Male / Female / Other, required. | No gender field. The client profile has a `Gender` column that stays empty. | Med |
| S2 | Confirm password | Required, must match. | Present. Matches. | OK |
| S3 | Terms and Conditions | Checkbox gate and text (text pending from owner). | None. | Med |
| S4 | Provider CNIC mask | `XXXXX-XXXXXXX-X`, sent with dashes. | Regex only, no live mask. | Low |
| S5 | Provider categories | 1 to 3 categories with one primary, plus optional service titles. | One category. Per D5 the signup path stays, so decide whether it should offer more, or leave extra categories to the profile page. | Med |
| S6 | Provider extras | Experience (0 to 50), city, description at registration. | Not collected. | Med |
| S7 | Duplicate checks | Unique mobile. | Unique mobile and unique CNIC. Matches or better. | OK |
| S8 | Next step | OTP in app. | Redirect to login with a success message. Fine per D13. | OK |
| S9 | Mobile mask | Live mask. | Added this session. | OK |

## Request form (`/client/services/{id}/categories/{id}/request`)

| # | Gap | App behaviour | Web today | Sev |
|---|---|---|---|---|
| R1 | "Other (not listed)" title | Last dropdown option; reveals a required "Specify Service" field and makes the description required (`Please describe the job so staff can assign a provider`). `serviceTitleUid` is null. | Missing. Only listed titles. | High |
| R2 | On-site contact | "Same as my account" switch (default on), else contact person and number fields with the mobile mask. | Missing; the contact is always the account name and number. | High |
| R3 | Preferred date and time | Optional date (today to +365 days) and time. | Missing. | High |
| R4 | Remarks | Optional field. | Missing. | Med |
| R5 | Budget text | `estimateText`, else `basePrice`, else `No estimate set for this service yet`; before choosing a title, `Select a service title below to see the estimate`. | Right priority. Wording differs, and the currency is shown as `Rs. 1,500`; the app uses `Rs 1,500`. | Low |
| R6 | Description label | `Service Description (optional)`, required for Other. | Optional only. | Med |
| R7 | Address list text | `{title} — {fullAddress}, {area}, {city}`. | Matches. | OK |
| R8 | Posted ids | Server derives the client from the session. | Controller already ignores posted ids and resolves the client from the session. | OK |
| R9 | After submit | Goes to My Requests. | Same, with a success message. | OK |
| R10 | Failure copy | `Submission Failed` with the API message. | Shows the service message inline. Fine. | OK |

## My Requests (`/client/requests`)

| # | Gap | App behaviour | Web today | Sev |
|---|---|---|---|---|
| M1 | Tabs | Active / Completed / Cancelled with counts, split by `progressStatus` (null means Cancelled). | One flat list, no tabs. | High |
| M2 | Status label | Shows `progressStatus` (Requested, Assigned, In Progress, Completed) with colour, or `Cancelled`. | Shows raw `Status` as the headline (for example `Initiated`) and `progressStatus` as a small line. | High |
| M3 | Progress bar | 4-step bar, or red `Cancelled`. | None. | Med |
| M4 | Cancel | Only while status is Initiated or Pending; preset reasons plus Other; sends a reason. | No cancel action at all. | High |
| M5 | Provider contact and passcode | Passcode dialog, Call, WhatsApp once a provider is assigned. | Provider name only. | High |
| M6 | Bounced-back notice | Orange notice when the provider cancelled and the request was reopened. | None (needs a stored last-seen status). | Med |
| M7 | Request detail page | Full detail with cards for service, schedule, provider, contact, budget and info. | None; the list cards are not clickable. | High |
| M8 | Schedule and Urgent | Shows scheduled date and time, Urgent pill. | Urgent pill only; no schedule (the form has no date). | Med |
| M9 | Budget display | Only when greater than 0. | Shown whenever it has a value, so `Rs. 0` can appear. | Low |
| M10 | Delete | Hide on device. | Not offered, per D3. | OK |
| M11 | Active count text | Total used by mistake in the app (R9). | No banner. Do it correctly. | Low |
| M12 | Date format | `dd-MM-yy`, PKT. | PKT helper already used. | OK |

## Client dashboard, services browsing, profile

| # | Gap | App behaviour | Web today | Sev |
|---|---|---|---|---|
| D1 | Dashboard content | Services grid, featured categories, search with ranking, bell with unread count. | Welcome card with account info and three links; the page says "More tools are coming soon". | High |
| D2 | Service order and style | Sorted by `displayOrder`, with an icon and colour per service name. | Sorted by `DisplayOrder` and name, icon by keyword. Similar. | OK |
| D3 | Search | Live suggestions, ranked, top 6. | None. | Med |
| D4 | Notification bell | Unread badge. | None (D2 in the decisions file). | Med |
| D5 | Profile | Account info, Edit Profile (name, gender), addresses (add, edit, delete), 24-hour time, Delete Account, Logout, Contact, Privacy Policy. | Profile page with address create, edit and delete exists. No Edit Profile, no 24-hour preference, no Delete Account (D18), no gender. | Med |
| D6 | Address pin | Map with search, reverse geocoding, optional coordinates. | Not checked in detail; confirm whether the address form has a map (D12). | Med |
| D7 | Address delete rule | `Cannot delete address because it is linked to service requests.` | Not checked; confirm the message and behaviour. | Low |
| D8 | Footer and guest browsing | Guest can browse services and categories, with a login prompt at request time. | Services pages are under `/client/...` (login required). The public site has separate marketing pages. Decide whether guests should browse real categories before logging in. | Med |

## Status (updated 26 Oct 2026)

### Client side: done
Request form (R1-R6, R8), My Requests and request detail (M1-M5, M7-M9, M11), signup (S1, S3-S6; terms checkbox added with the owner's text), client dashboard (D1, D3), profile (Edit Profile with gender, D5), remember-me length (L4), notification bell and inbox (D4), role switcher, per-browser 24-hour time switch (D5).
Login with unverified accounts stays allowed (D21, L1) until OTP exists.

### Provider side: done
- Entry gate: unverified providers go to `/provider/pending` (L2); both profile ids and the active role are in the cookie (L3).
- `/provider/dashboard`: overview stats, online/offline with explicit Save/Cancel hours (D8), notifications card.
- `/provider/documents`: first upload and My Documents with browser crop; CNIC replacement asks for confirmation and sends the account back to review; police certificate locks once verified (D4, D10). New providers go straight to it after signup.
- `/provider/requests`: accept and reject with reason, refreshes every 30 seconds (D16).
- `/provider/bookings` and `/provider/bookings/{id}`: tabs, start, cancel with reason, passcode completion (amount, payment mode, labour, materials), embedded map and Directions link (D12).
- `/provider/wallet`: balance, pending payout, transaction history.
- `/provider/profile`, `/provider/profile/edit` (description and zones, D11) and `/provider/categories` (categories, primary, services).
- Notifications inbox and bell for both roles (D2); switch-dashboard button in the second navbar (D6).

### Not done, on purpose or still open
- M6 "provider cancelled" notice: needs a stored last-seen status per request.
- D6 address map pin and D8 guest browsing: not started.
- Delete Account (D18) is built but switched off for now: the buttons are hidden and both endpoints return 404 (flag `DeleteAccountEnabled` in `ClientProfileController` and `ProviderProfileController`).
- Rejected requests history: skipped (D9).
- OTP, forgot password and reset password: deferred (D13); L1 (block unverified accounts at login) must be fixed once OTP exists.
- Deep links `/app/requests/{id}`, `/app/bookings/{id}`, `/app/notifications` for logged-in users (D19): not mapped yet.
- Provider availability toggle appears on the dashboard and profile only, not in the navbar of every provider page.
- The 24-hour switch is stored in a browser cookie, not on the account.
- Terms text is an interim draft (see `docs/legal/TERMS_&_CONDITONS.md`).

### Not browser-tested
Server responses were checked end to end against the running site, but these need a look in a real browser: the crop dialog on the documents page, the 30-second Requests refresh, the 60-second bell badge, the booking-detail map, and the dialogs' button locking.
