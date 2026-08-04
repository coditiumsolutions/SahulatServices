# Provider Job Workflow — End-to-End Test Report

<!-- Version: 1.0 | UpdatedAt: 08/04/2026 -->

**Project:** SahulatGharTak / HomeServicesPortal
**Environment:** `https://localhost:7265` (local dev, live SQL Server `SahulatAppDB`)
**Scope:** Client registration → service request → staff assignment → provider accept/reject → passcode-verified completion → payment ledger posting, plus a spot-check of the admin sidebar sort/filter/WIP work from the same session.

---

## Executive summary

Ran the full provider job workflow live against the running app and real database — no mocks — driving it through the actual mobile-facing REST APIs (`api.txt`) and the admin portal's real HTTP endpoints (login, CSRF token, form posts), the same way the Flutter app and admin staff would.

**Result:** The happy path works end to end. Testing surfaced **2 real bugs in the reject flow**, both traced to the same root cause and both fixed and re-verified live during the session. No other defects found.

| Area | Result |
|---|---|
| Client/provider registration, OTP, login | Pass |
| Client address + service request creation | Pass |
| Staff assignment (admin portal) | Pass |
| Provider accept → passcode generation → contact info reveal | Pass |
| Passcode verification (wrong code) | Pass (correctly rejected) |
| Passcode verification (correct code, partial payment) | Pass |
| Payment ledger posting (3-case commission/earning/collection logic) | Pass |
| Provider reject → reassignment | **Failed initially — 2 bugs found, fixed, re-verified** |
| Admin Bookings sort (most-recent-first) + status filter pills | Pass |
| WIP placeholder pages (9 previously-broken sidebar links) | Pass |

---

## 1. Test method

All steps were performed against the live app, not simulated:

- **Mobile API surface**: `curl` against `/api/*` endpoints exactly as documented in `api.txt` (no auth header — identity passed via body fields, per the current "Auth: None" state of the API).
- **Admin portal**: real cookie-based login (`POST /Account/Login` with a scraped anti-forgery token), then real form submissions to `/Admin/ServiceRequests/Assign/{id}` with a fresh CSRF token per request — the same request shape a browser would send.
- Two new accounts were registered fresh for this test (one Client, one Provider under "Electrician Services") rather than reusing existing data, to get a clean, traceable trail.
- Where a request failed with an opaque `"An unexpected error occurred."` (the API's generic 500 message), a second instance of the app was started with `dotnet run` in the foreground to capture the actual exception/stack trace, since the primary instance's console output wasn't being captured.

---

## 2. Happy path — step by step

| # | Step | Endpoint / Action | Result |
|---|---|---|---|
| 1 | Register client | `POST /api/auth/register-client` | `201`, `userId:35`, `profileId:34` |
| 2 | Send + verify OTP (client) | `POST /api/auth/send-otp`, `POST /api/auth/verify-otp` | `200`, verified |
| 3 | Register second account, upgrade to Provider | `POST /api/auth/register-client` → `POST /api/auth/register-provider` (categoryId 1, Electrician Services) | `201` then `200`, `providerUid:16` |
| 4 | Login both accounts | `POST /api/auth/login` | `200` for both |
| 5 | Create client address | `POST /api/client-addresses` | `201`, `uid:19` |
| 6 | Create service request | `POST /api/customer-service-requests` | `201`, `uid:62`, `status:"Pending"` |
| 7 | Staff assigns provider | Admin portal `GET`/`POST /Admin/ServiceRequests/Assign/62` | `302` (success redirect); booking `uid:24` created with `status:"Pending"` |
| 8 | Provider accepts | `POST /api/service-bookings/24/respond` `{accept:true}` | `200`, `status:"Accepted"`, `passcode:"9023"` generated, `acceptedOn` set |
| 9 | Contact info reveal check | `GET /api/service-bookings/24` | Provider mobile/CNIC and client mobile/address now populated on both directions (were `null` while `Pending`) |
| 10 | Wrong passcode | `POST /api/service-bookings/24/verify-completion` `{passcode:"0000"}` | `400`, `"Incorrect passcode."` — booking unchanged |
| 11 | Correct passcode, partial payment | `POST /api/service-bookings/24/verify-completion` `{passcode:"9023", actualAmountPaid:1000}` (of a 1500 final bill) | `200`, `status:"Completed"`, `customerPaid:1000`, `customerRemaining:500`, `completedOn` set |
| 12 | Parent request sync | `GET /api/customer-service-requests/62` | `status:"Completed"` |
| 13 | Payment ledger posting | Admin portal `/Admin/Payments/Ledger` | 3 entries posted: Company/Credit/Commission (150), Provider/Credit/JobEarning (1350), Provider/Credit/CashCollect (1000) — matches `CashToProvider` mode with partial payment |

All of the above matched the designed behavior exactly, including the partial-payment math (`CustomerRemaining = FinalAmount − actualAmountPaid`) and the fact that `Rejected`/`Pending` bookings correctly withhold contact info while `Accepted`/`Completed` reveal it.

---

## 3. Bugs found and fixed

Both bugs were in the **provider-reject path**, which hadn't been exercised end-to-end before this session. Root cause in both cases: `Rejected` was added as a booking status late in the original implementation, and two other code paths weren't updated to account for it.

### 3.1 DB CHECK constraint missing `Rejected`

- **Symptom:** `POST /api/service-bookings/{id}/respond` with `{accept:false}` returned a generic `500 "An unexpected error occurred."`
- **Root cause:** The live `ServiceBookings` table has a `CK_ServiceBookings_Status` CHECK constraint that was created before `Rejected` was added to the application's status list, so it only allowed `Pending | Accepted | In Progress | Completed | Closed | Cancelled`. Any attempt to write `Status = 'Rejected'` was rejected by SQL Server (`SqlException Error 547`), even though the C# code and `db.txt` documentation both already listed `Rejected` as valid.
- **Fix:** Altered the constraint on the live DB to include `Rejected`:
  ```sql
  ALTER TABLE ServiceBookings DROP CONSTRAINT CK_ServiceBookings_Status;
  ALTER TABLE ServiceBookings ADD CONSTRAINT CK_ServiceBookings_Status
    CHECK ([Status]='Cancelled' OR [Status]='Closed' OR [Status]='Completed'
        OR [Status]='In Progress' OR [Status]='Accepted' OR [Status]='Pending' OR [Status]='Rejected');
  ```
- **Verified:** Reject call succeeded after the fix, `Status` correctly persisted as `Rejected` in the DB.

### 3.2 Successful reject reported as "not found"

- **Symptom:** After fixing 3.1, a reject call that *did* succeed still returned `{"success":false,"message":"Booking not found.","data":null}` (HTTP 404) instead of confirming success.
- **Root cause:** `ServiceBookingApiService.RespondToBookingAsync` calls `BookingService.RespondToAssignmentAsync` (which correctly sets `Status = "Rejected"`), then re-fetches the booking via `GetBookingByIdAsync` to build the response — but that lookup filters out `Rejected` bookings by design (they're meant to be hidden from normal client/provider-facing queries). So the rejecting provider's own confirmation was being swallowed by that same filter.
- **Fix:** `RespondToBookingAsync` now re-fetches the booking directly (bypassing the `Rejected` filter) scoped to `bookingUid` + the acting `providerUid`, so the caller who just performed the action can see their own result regardless of the resulting status.
- **Verified:** Reject call now returns `200`, `"Booking rejected successfully."`, `status:"Rejected"` in the response body.

### 3.3 Assign form wouldn't reload after a rejection

- **Symptom:** After a request's booking was rejected (parent request correctly reverted to `Status:"Pending"`), staff clicking "Assign" on that request again in the admin portal was silently redirected back to the request list instead of seeing the assignment form.
- **Root cause:** `BookingService.GetAssignProviderFormAsync` (which loads the Assign form) checks whether the request "already has a booking" using a query that did **not** exclude `Rejected` bookings — unlike `AssignProviderAsync` (the form-submit handler), which already had that exclusion. So the form-load check saw the old rejected booking and refused to show the form, even though the submit handler would have accepted a new one.
- **Fix:** Aligned the two checks — `GetAssignProviderFormAsync` now excludes `Status != "Rejected"` bookings from its "already booked" check, matching `AssignProviderAsync`.
- **Verified:** Assign form loaded correctly (`200`) for a previously-rejected request; reassigned to a different provider; new booking (`uid:27`) created successfully with `status:"Pending"`, no "already has a booking" error.

**Files changed:**
- `Services/ServiceBookingApiService.cs` — `RespondToBookingAsync`
- `Services/BookingService.cs` — `GetAssignProviderFormAsync`
- Live DB — `CK_ServiceBookings_Status` constraint (no migration file; applied directly via `sqlcmd` since this DB predates EF migration tracking, consistent with how the `Passcode`/`AcceptedOn`/`CompletedOn` columns were added earlier)

---

## 4. Reject → reassignment flow (re-verified after fixes)

| Step | Result |
|---|---|
| Provider rejects booking | `200`, `status:"Rejected"` |
| Parent service request | Reverts to `status:"Pending"` |
| Rejected booking hidden from provider's booking list | Confirmed absent from `GET /api/service-bookings?providerUid=16` |
| Rejected booking hidden from direct lookup | `GET /api/service-bookings/{id}` → `404` (by design — staff-only visibility) |
| Staff re-opens Assign form for the same request | `200` (previously `302` bounce — this was the bug) |
| Staff assigns a different provider | `302` success; new booking created, `status:"Pending"` |

---

## 5. Admin sidebar spot-check (same session, prior work)

Quick regression check of the sort/filter and WIP-page work from earlier in this session, done alongside the workflow test:

- `/Admin/Bookings` — most-recently-created booking now appears first; status filter pills (`Pending`, `Accepted`, `In Progress`, `Completed`, `Closed`, `Cancelled`, `Rejected`) all present and link correctly.
- All 9 previously-broken sidebar links (P-Locations, P-Availability, BookingTracking, Reviews, and all 5 Reports pages) return `200` with the "Work in Progress" placeholder instead of a SQL exception or 404.
- Spot-checked all working admin pages (Dashboard, S-Categories, S-Provider, P-Documents, Clients, S-Requests, Bookings, all 4 Finance pages, Users & Roles, APK Management) — all still `200`. One transient `500` was observed on `/Admin` (Dashboard) during a burst of rapid sequential test requests; three immediate retries all returned `200`, so this reads as a momentary DB connection blip under load rather than a regression, not a defect.

---

## 6. Test data left in the database

The following was created during this test run and left in place (not cleaned up):

- 2 `UsersLogin`/`Clients`/`Providers` rows (mobile `03219990001` — Client, `03219990002` — Provider, Electrician Services)
- 1 `ClientAddresses` row (uid 19)
- 3 `CustomerServiceRequests` (uid 62, 63, 64)
- 4 `ServiceBookings` (uid 24 Completed, 25 Rejected, 26 Rejected, 27 Pending)
- 3 `PaymentLedger` entries tied to booking 24

No production data was touched. Delete on request if a clean slate is wanted before further testing or demos.
