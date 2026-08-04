# Flutter Integration Plan — Booking Workflow & Payment/Wallet APIs

This document breaks the backend work completed on 2026-08-04 into 3 phases for
integrating into the Flutter provider app. Each phase includes a ready-to-paste
Claude Code prompt, meant to be run **inside the Flutter project repo** (Claude
Code needs to inspect the existing app structure — routing, state management,
API client, models — before writing code, so these prompts intentionally ask it
to explore first rather than assuming a structure).

Full endpoint specs (request/response shapes, error cases) are documented in
this backend repo's `api.txt` — the "SERVICE BOOKINGS APIs" and "PROVIDER
WALLET API" sections. Point Claude Code at that file (copy it into the Flutter
repo temporarily, or paste the relevant section) if it doesn't have access to
this repo.

## What's new on the backend

1. **Provider accept/reject workflow** — staff-assigned bookings now land as
   `Status="Pending"` instead of auto-`Accepted`. The provider must explicitly
   respond via `POST /api/service-bookings/{id}/respond`.
2. **4-digit completion passcode** — generated on accept, shown to the client,
   entered by the provider (from the client) to confirm job completion via
   `POST /api/service-bookings/{id}/verify-completion`. Supports partial
   payment (`actualAmountPaid` can be less than `finalAmount`).
3. **Contact/address reveal gated by status** — provider's mobile/photo/CNIC
   and client's mobile/address only appear in the booking payload once
   `Status` is `Accepted`, `In Progress`, or `Completed` (null while `Pending`).
4. **Provider wallet API** — `GET /api/providers-wallet/{providerUid}` returns
   running ledger balance + transaction history, so the app can show earnings
   without hitting the admin-only ledger.

---

## Phase 1 — Booking Accept/Reject + Passcode Completion

The core workflow change. Without this, providers can't act on new jobs at all
under the new backend contract (bookings arrive `Pending`, not `Accepted`).

**Scope:**
- Update the booking model/DTO to include `status` values `Pending`,
  `Rejected` (may appear historically, should never surface for the current
  provider — filter defensively), `passcode`, `acceptedOn`, `completedOn`.
- New provider-facing screen/action: incoming job request card with
  Accept/Reject buttons, calling `POST /service-bookings/{id}/respond`.
- On accept, surface the passcode prominently in the booking-detail screen
  (client sees it; provider does NOT need to see it here — provider re-enters
  a passcode given verbally/in-person by the client during completion).
- New completion flow: provider enters the passcode + actual amount collected
  (with an optional payment-mode override) to call
  `POST /service-bookings/{id}/verify-completion`. Handle the three error
  cases distinctly (`Incorrect passcode.`, `This booking is not awaiting
  completion.`, `Booking not found.`).
- Update booking-list/detail UI to branch correctly on the expanded status set.

### Claude Code prompt

```
I'm integrating a backend workflow change into this Flutter app. Before this
change, staff-assigned bookings were auto-accepted; now they arrive with
status "Pending" and the provider must explicitly accept or reject them, then
later confirm completion with a 4-digit passcode.

First, explore the existing codebase to understand:
- Where booking data is fetched/modeled (API client, DTOs/models, repository
  or service layer)
- How booking status is currently used to drive UI (list screens, detail
  screens, status badges/chips)
- The app's state management approach (Provider/Riverpod/Bloc/etc.) so new
  screens match existing patterns

Then implement:

1. Update the booking model to add: `passcode` (String?), `acceptedOn`
  (DateTime?), `completedOn` (DateTime?), and extend the status enum/string
  handling to include "Pending" (now the initial state for staff-assigned
  bookings) and "Rejected" (should never be shown for the current provider,
  but don't crash if it appears).

2. Add an API call for POST /api/service-bookings/{bookingUid}/respond
  Body: { "providerUid": <int>, "accept": <bool> }
  On accept: booking becomes Accepted, passcode is generated and returned.
  On reject: booking becomes Rejected, removed from the provider's active list.
  Errors to handle: 400 "This booking is no longer awaiting a response.",
  404 "Booking not found."

3. Add UI for a "Pending" booking (new job assignment) with Accept/Reject
  actions calling the endpoint above. Show a clear success/error state.

4. Add an API call for POST /api/service-bookings/{bookingUid}/verify-completion
  Body: { "providerUid": <int>, "passcode": <string>, "actualAmountPaid":
  <decimal>, "paymentMode": <string?> } — paymentMode is optional, only send
  it if the provider is overriding "CashToProvider"/"OnlineToCompany" from
  what staff set at assignment.
  On success: booking becomes Completed, customerPaid/customerRemaining
  update (partial payment is supported — customerRemaining can stay > 0 if
  actualAmountPaid < finalAmount).
  Errors to handle distinctly: 400 "Incorrect passcode." (let provider
  retry), 400 "This booking is not awaiting completion." (stale state, tell
  user to refresh), 404 "Booking not found."

5. Add a completion screen/dialog: passcode entry field, actual amount
  collected field (pre-filled with finalAmount but editable for partial
  payment), optional payment mode toggle. Submit calls verify-completion.

6. Update any booking-detail or list screen that switches on status to handle
  Pending/Accepted/In Progress/Completed correctly, and to conditionally
  render provider/client contact fields (providerMobileNo,
  providerProfilePhotoPath, providerCnic, clientMobileNo, clientAddressTitle,
  clientFullAddress, clientArea, clientCity) — these are null while status is
  Pending and populated once Accepted/In Progress/Completed, so guard for null
  rather than assuming they're always present.

Full request/response JSON shapes are in @api.txt 
"SERVICE BOOKINGS APIs" — sections "POST Respond To Booking" and "POST Verify
Completion Passcode".

Match existing code style, error handling conventions, and navigation
patterns already used elsewhere in the app. Don't introduce a new state
management approach if one is already in use.
```

---

## Phase 2 — Contact Reveal & Booking Detail Enrichment

Builds on Phase 1's model changes. Once a booking is accepted, both sides
should be able to see each other's contact info and (for the provider) the
client's address — this is data the backend now returns inline on the booking
object, no separate endpoint needed.

**Scope:**
- Surface `providerMobileNo`, `providerProfilePhotoPath`, `providerCnic` (if
  the client-facing app also lives in this codebase) and `clientMobileNo`,
  `clientAddressTitle`, `clientFullAddress`, `clientArea`, `clientCity` on the
  provider's booking-detail screen once available.
- "Call client" quick action using `clientMobileNo` once populated.
- Handle the gated/null case gracefully (booking still `Pending` → don't show
  a broken "Call" button or blank address).

### Claude Code prompt

```
Continuing the booking-workflow integration from Phase 1. The backend booking
API now inline-includes contact/address fields once a booking is past
"Pending" status:
- providerMobileNo, providerProfilePhotoPath, providerCnic
- clientMobileNo, clientAddressTitle, clientFullAddress, clientArea, clientCity

These are null when status is "Pending" and populated once status is
"Accepted", "In Progress", or "Completed" — no separate API call needed, it's
already on the booking object returned by GET /api/service-bookings/{id} and
GET /api/service-bookings (and included in the respond/verify-completion
responses too).

Update the booking-detail screen to:
1. Show the client's full address (clientAddressTitle, clientFullAddress,
   clientArea, clientCity) once available — this is the job site.
2. Show a "Call Client" button using clientMobileNo (use url_launcher or
   whatever calling mechanism is already used elsewhere in the app for
   contacting clients/providers — check existing code first).
3. When these fields are null (status still Pending), hide/disable this
   section entirely rather than showing empty text or a broken action —
   check how the app currently handles other conditionally-null API fields
   for the pattern to follow.

Keep this scoped to reading and displaying already-fetched fields — no new
endpoints are needed for this phase.
```

---

## Phase 3 — Provider Wallet Screen

Independent of Phases 1–2 (different endpoint, no shared state), but
logically follows since it's the payoff view for completed jobs: providers
can now see their running balance and transaction history from a job well.

**Scope:**
- New wallet screen: current balance, pending payout total, transaction list.
- `GET /api/providers-wallet/{providerUid}` — single call, no pagination in
  current backend implementation (returns full history).
- Balance sign convention: positive = company owes provider; negative =
  provider owes company (unremitted cash-job commission). Must be visually
  distinguished (not just a plain number) since negative balance is a
  legitimate, common state (any pending cash job creates one).

### Claude Code prompt

```
Add a Provider Wallet screen to this Flutter app.

First explore the existing codebase for:
- How the provider's own UID is currently accessed (auth/session state)
- Existing screens with similar "balance + transaction list" shape, if any,
  to match visual style
- The app's existing HTTP client wrapper/error-handling pattern

Then implement:

1. API call for GET /api/providers-wallet/{providerUid}
   No request body. Response shape:
   {
     "success": true,
     "message": "...",
     "data": {
       "providerUid": int,
       "providerName": string,
       "categoryName": string,
       "balance": decimal,
       "pendingPayoutTotal": decimal,
       "transactions": [
         { "uid": int, "createdOn": ISO datetime, "bookingUid": int?,
           "reason": string, "signedAmount": decimal, "runningBalance": decimal }
       ]
     }
   }
   404 response: { "success": false, "message": "Provider not found.", "data": null }

2. Wallet screen showing:
   - Current balance, prominently. IMPORTANT: balance sign has meaning —
     positive = company owes the provider money (earned but not yet paid
     out); negative = provider owes the company (commission collected in
     cash but not yet remitted). Use color/icon to distinguish (e.g. green
     for positive, amber/red for negative) rather than showing a bare
     number — a negative balance is normal, not necessarily an error state,
     so don't alarm the user unnecessarily but do make the direction clear.
   - pendingPayoutTotal as a secondary stat — this is money already approved
     for payout that the admin hasn't disbursed yet, separate from balance.
   - Transaction list, most-recent-first (already sorted by the API): show
     createdOn (formatted), reason (e.g. "JobEarning", "CashCollect", "Payout"
     — humanize these labels), signedAmount (with +/- and color), and
     optionally link bookingUid to the relevant booking detail screen if one
     exists.

3. Add navigation entry point to this screen (e.g. from a profile/earnings
   menu item) consistent with existing app navigation.

4. Handle loading, empty-transactions, and 404/error states following
   existing patterns in the app.

Full example response is in api.txt (backend repo) under "PROVIDER WALLET
API". Ask me to paste it if you don't have access to that file.
```

---

## Suggested order

Phase 1 is required first — it changes the fundamental booking lifecycle and
everything else assumes it's in place. Phases 2 and 3 can run in either order
or in parallel once Phase 1 lands, since they touch different screens and
don't share new state.
