# Web parity audit: existing pages vs the app

Compares the pages already on the website with [web-parity-spec.md](web-parity-spec.md), applying the choices in [web-parity-decisions.md](web-parity-decisions.md). Read-only review; nothing has been changed.
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

## Status after the client-side pass

Done: request form (R1-R6), My Requests (M1-M5, M7-M9, M11), request detail, signup (S1, S4-S6), client dashboard (D1, D3), profile (Edit Profile with gender, Delete Account), remember-me length (L4), login unverified rule recorded as D21 (L1).
Not done, on purpose:
- S3 terms checkbox: waiting for the owner's Terms text.
- M6 "provider cancelled" notice: needs a stored last-seen status per request.
- D4 notification bell: part of the notifications work (D2).
- D5 24-hour time switch: the time format is a single site-wide setting today, not per user.
- D6 address map pin, D8 guest browsing: not started.
- L2 provider pending gate, L3 both ids in claims: belong to the provider side.

## Provider pages

All provider pages other than the dashboard placeholder (`/provider/requests`, `/provider/bookings`, `/provider/wallet`, `/provider/profile`) are placeholders. The full set in Part C of the spec is still to be built.

## Suggested fix order for the existing pages

1. My Requests: status label from `progressStatus`, tabs, progress bar, request detail page, cancel with reason, provider contact and passcode (M1 to M8).
2. Request form: "Other" title, contact switch, date and time, remarks (R1 to R4, R6).
3. Login: provider gate and unverified-account rule (L1, L2); claims for both ids (L3, D20).
4. Signup: gender, terms (after owner text), provider fields (S1, S3, S5, S6).
5. Dashboard: replace the placeholder with the services grid, search and bell (D1, D3, D4).
6. Profile: Edit Profile, 24-hour time, Delete Account (D5).
