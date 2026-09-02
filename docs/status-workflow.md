# Request & Booking Status Workflow

**Status: implemented.** Design reference for the `CustomerServiceRequests.Status` /
`ServiceBookings.Status` model, the client- and provider-facing progress indicators, and the
cleanup of the legacy `BookingTracking` feature. Originally written before implementation as
the spec both the backend and the Flutter app (client + provider) should be built against —
everything below has now been built to this spec (see "Implementation status" at the bottom
for what changed, file by file, and what's still open). Read alongside `api.txt` (mobile
contract, now updated to match) and `db.txt` (live schema).

## Background: two tables, two statuses

- `CustomerServiceRequests.Status` — the request as the **client** created it. Managed by
  staff at a coarse level (assign / cancel) and read by the client app.
- `ServiceBookings.Status` — the actual job once staff assigns a provider. Managed by the
  **provider** app (accept/reject, start, verify-completion) and by staff (manual overrides,
  final close-out). One request has at most one non-`Rejected` booking at a time.

These are deliberately **not** kept as a 1:1 mirror. The booking status is the real, granular
state machine; the request status stays coarse; the client app gets a third, purely
*computed* view (`progressStatus`, see below) that never touches either stored column.

## BookingTracking — removed

`db.txt` marks `BookingTracking` and its parent `Bookings` table `[REMOVED]` — this was a
legacy, staff-only tracking feature (`On The Way / Arrived / In Progress / Completed /
Cancelled`) built against `SahulatAppDbContext`/the old `Bookings` table, with no `/api`
route and no live schema backing it. Deleted entirely as part of this change:

- `Controllers/BookingTrackingController.cs`
- `Services/BookingTrackingService.cs`, `Services/IBookingTrackingService.cs`
- `Models/Entities/BookingTracking.cs`, `Models/Entities/Booking.cs`
- `Models/ViewModels/BookingTrackingViewModels.cs`
- `Views/BookingTracking/*`
- `SahulatAppDbContext` DbSet + nav menu entry (`_AdminLayout.cshtml`)

Nothing in the current system (mobile or admin) depended on it. Its old job — a granular,
provider-driven timeline — is superseded by the real `ServiceBookings.Status` machine plus
the new `/start` endpoint below.

## `ServiceBookings.Status` — provider/staff state machine

Canonical values (whitelisted, shared constant — see Implementation notes):

```
Pending -> Accepted -> In Progress -> Completed -> Closed
        -> Rejected
Accepted / In Progress -> Cancelled   (staff-only, post-acceptance cancel)
```

| Status | Set by | Trigger |
|---|---|---|
| `Pending` | staff | booking created when staff assigns a provider to a request |
| `Accepted` | provider | `POST /service-bookings/{id}/respond` with `accept:true` |
| `Rejected` | provider | `POST /service-bookings/{id}/respond` with `accept:false` (hidden from all client/provider GET responses; parent request reverts to `Pending` so staff can reassign) |
| `In Progress` | provider | **new:** `POST /service-bookings/{id}/start` (precondition: current status `Accepted`) |
| `Completed` | provider | `POST /service-bookings/{id}/verify-completion` (passcode match). Posts `PaymentLedger`/`ProviderPayout` entries automatically (`RecordBookingCompletionAsync`) and syncs the parent request to `Completed`. |
| `Closed` | staff | manual, via admin portal, after reviewing/reconciling the completed job. **No automatic ledger action is tied to this today** — ledger posting already happened at `Completed`. Confirmed by reading `PaymentService.cs`; if `Closed` should trigger something additional, that logic doesn't exist yet and needs to be specified separately. |
| `Cancelled` | staff | manual, admin-only, only reachable from `Accepted`/`In Progress` (requires a `CancelReason`) |

No orphan values remain in this table now that `/start` exists — every status has a real,
reachable code path.

**Fixed:** `ContactVisibleStatuses` (`ServiceBookingApiService.cs`) and
`ProviderVisibleStatuses` (`CustomerServiceRequestService.cs`) previously gated
mobile/CNIC/passcode/address visibility on `["Accepted", "In Progress", "Completed"]` —
`Closed` was missing, so contact/address details for an archived job would have incorrectly
nulled out the moment staff closed it. `Closed` has been added to both lists.

## `CustomerServiceRequests.Status` — coarse, staff-managed

Canonical values (whitelist now enforced on the mobile `PUT` — previously unvalidated free
text, see Implementation status):

```
Pending -> Assigned -> Completed
Pending -> Cancelled     (client-driven, pre-assignment)
Assigned -> Cancelled    (staff-driven, post-assignment — see note below)
```

- `Pending` — request created, no booking yet, or a provider just rejected (reverted here by
  the reject flow so staff can reassign).
- `Assigned` — staff created a booking for this request (booking `Pending`, `Accepted`, or
  `In Progress`/`Completed`/`Closed` — this column does not track the fine-grained booking
  states, see `progressStatus` below).
- `Completed` — synced automatically when the booking reaches `Completed` (see above).
- `Cancelled` — reachable two ways:
  - **Client-driven, pre-assignment:** `PUT /customer-service-requests/{id}` with
    `status: "Cancelled"` + required `cancelReason`.
  - **Staff-driven, post-assignment:** when staff cancels an `Accepted`/`In Progress` booking
    (admin portal), the *same* admin action also sets `request.Status = "Cancelled"` in the
    same save — mirroring the existing pattern in `AssignProviderAsync` (assign) and the
    reject branch of `RespondToAssignmentAsync` (both already write both tables atomically
    from one staff/provider action). This is a deliberate, single-transaction write driven by
    one explicit action, not an implicit background sync — see Flutter Q2 below for why this
    doesn't reintroduce the drift problem this doc otherwise avoids.

This column is **not** where `In Progress` lives — no automated flow ever writes that value
here; that distinction exists only in the computed client-facing field. Staff retain their
existing, out-of-scope-for-this-doc ability to force this column to any whitelisted value
manually via the admin portal (unchanged, pre-existing behavior) — see Flutter Q1.

## Client-facing progress indicator (`progressStatus`)

New, **computed, read-only** field added to the `GET /customer-service-requests` response
(list + by-id). Never persisted — derived at query time from the request + its joined
booking (if any). This is what the Flutter client app renders as the progress bar.

| `progressStatus` | Derived when |
|---|---|
| `Requested` | no non-`Rejected` booking exists yet; request `Status == "Pending"` |
| `Assigned` | booking exists with `Status` in (`Pending`, `Accepted`) |
| `In Progress` | booking `Status == "In Progress"`, **or** booking `Status == "Accepted"` and `now >= PreferredServiceDate`/`PreferredServiceTime` |
| `Completed` | booking `Status` is `Completed` **or** `Closed` — the client never needs to know a job was staff-reviewed, `Closed` still just reads as `Completed` to them |

Spelling note: `In Progress` (with the space) is used verbatim here — see Flutter Q3 below
for why this was changed from the original draft's `"InProgress"`.

**`Cancelled` is excluded from `progressStatus` entirely** (per this conversation's
correction — superseding the earlier draft of this doc, which had listed it as a stage).
Cancellation is instead surfaced as a separate flag/state outside the progress-bar enum:

- If request `Status == "Cancelled"`, or the booking's `Status == "Cancelled"`, the API
  should report this as `status: "Cancelled"` (or equivalent) **and omit/null `progressStatus`
  entirely** rather than emitting a `Cancelled` stage value.
- The Flutter app hides the progress bar whenever this cancelled state is present and
  instead renders a plain "Cancelled" label — this rendering behavior is the Flutter
  agent's responsibility, not something the backend needs to compute beyond exposing the
  cancelled flag/status.
- This applies whether the cancellation happened pre-assignment (client-cancelled,
  `Requested` stage) or post-assignment (staff-cancelled, was already `Assigned`/`InProgress`)
  — in both cases: no progress bar, just "Cancelled".

No new column, no migration, no background job (the schedule-time comparison is a pure
read-time computation) — no sync bug can be introduced because nothing is written.

## Provider-facing progress indicator

Provider app shows **no progress bar** for an incoming, not-yet-accepted assignment (the
"requests" list) — it appears only once accepted, on the "my bookings" page, driven directly
by the real `ServiceBookings.Status`:

```
Accepted -> In Progress -> Completed -> Closed
```

- `Accepted` / `In Progress` / `Completed` are provider-driven (`/respond`, `/start`,
  `/verify-completion`).
- `Closed` is staff-driven (admin portal review) — provider app only needs to *display* it
  (read-only), no mobile write path required.

## Implementation notes (original plan — all items below are done, see Implementation status)

1. Delete `BookingTracking` feature (files listed above).
2. Requests need **two** whitelists, not one shared constant — see Flutter Q1:
   - Admin/staff (`ServiceRequestService`, existing): keep as-is,
     `["Pending","Assigned","In Progress","Completed","Cancelled"]` — staff can force any of
     these manually via the portal, no change in scope here.
   - Client-facing (`CustomerServiceRequestService.UpdateRequestAsync`, currently
     **unvalidated** free text — the actual bug): new, narrower constant,
     `["Pending","Cancelled"]`. A client can edit request details while `Pending`, or cancel
     it; a client's own `PUT` should never be able to self-report `Assigned`/`In
     Progress`/`Completed` — those are staff/provider/system-driven only. Reject any other
     value with 400.
3. Add `Closed` to `ContactVisibleStatuses` and `ProviderVisibleStatuses`.
4. Add `POST /service-bookings/{id}/start` (provider-only, precondition `Accepted`) → sets
   `Status = "In Progress"`.
5. Add computed `progressStatus` (and a cancelled indicator that suppresses it) to the
   customer-service-requests DTO/mapping expression. Use `"In Progress"` (with the space) as
   the literal, matching the booking-side spelling (see Flutter Q3).
6. Staff-side booking cancel (admin portal, `Accepted`/`In Progress` -> `Cancelled`) must also
   write `request.Status = "Cancelled"` in the same save — see the updated
   `CustomerServiceRequests.Status` section above and Flutter Q2.
7. Update `api.txt` with the new endpoint and the new `progressStatus`/cancelled-flag fields
   once implemented, then hand the Flutter agent: the new enum values, the "no progress bar
   when cancelled" rule, and the new provider `/start` action.

## Flutter agent review — open questions for backend

Raised while reviewing this doc before implementation. Backend agent: please answer inline
below each item (or edit the sections above directly and note it here).

1. **`ValidStatuses` contradicts the `CustomerServiceRequests.Status` diagram.** Implementation
   note #2 lists the shared constant as
   `["Pending","Assigned","In Progress","Completed","Cancelled"]`, which includes
   `"In Progress"`. But the state diagram at line 74 (`Pending -> Assigned -> Completed`) and
   the explicit note at line 88 ("This column is **not** where `In Progress` lives") both say
   the request-status column never holds that value. Which is correct — should the whitelist
   drop `"In Progress"`, or does the request column need it after all?
   > _Backend answer:_ Both were half-right, so I split it into two whitelists (see
   > Implementation note #2, updated). The admin/staff whitelist keeps `"In Progress"` —
   > that's pre-existing manual-override capability for staff, unrelated to this refactor,
   > and out of scope to remove. The *client-facing* `PUT` (the one that was actually
   > unvalidated and the reason a whitelist was needed at all) gets a separate, narrower
   > list — `["Pending","Cancelled"]` — since a client should never be able to self-report
   > `Assigned`/`In Progress`/`Completed` on their own request via free text in the first
   > place. So: the request column *can* hold `"In Progress"`, but only via deliberate staff
   > action, never via the client API or any automated flow — the diagram/note about "no
   > automated flow writes this" still stands as originally written.

2. **Does `CustomerServiceRequests.Status` ever become `"Cancelled"` post-assignment?** The
   request-status section only documents `Pending -> Cancelled` as pre-assignment/client-driven.
   The `progressStatus` cancelled rule (line 108) checks *either* `request.Status == "Cancelled"`
   *or* `booking.Status == "Cancelled"` — implying a staff post-assignment cancel only ever
   writes `ServiceBookings.Status`, leaving the request row parked at `Assigned` permanently (by
   design, since the client-facing view is computed at read time). Please confirm this is
   intentional and that nothing should ever write `Cancelled` back onto an already-`Assigned`
   request row — this is exactly the kind of thing that gets "fixed" later by adding a sync
   write and reintroducing the two-column drift this doc is trying to eliminate.
   > _Backend answer:_ Good catch — parking it at `Assigned` forever was wrong, not just for
   > the client view but for the admin's own `ServiceRequests` list, which would keep showing
   > a dead request as `Assigned` indefinitely. Fixed: post-assignment cancel *does* write
   > `request.Status = "Cancelled"`, but as an explicit part of the *same staff action* that
   > cancels the booking (one admin click, one save, both rows updated together) — not an
   > automatic side-effect triggered off a provider action. That's the actual distinction that
   > avoids reintroducing drift: the failure mode we're avoiding is a write on table A silently
   > forgetting to touch table B in a *different, provider-driven* code path (that's exactly
   > how the original accept-sync gap happened). A single staff-initiated method writing both
   > rows in one `SaveChangesAsync` — same pattern already used by `AssignProviderAsync` and
   > the reject branch of `RespondToAssignmentAsync` — doesn't have that failure mode, because
   > there's only one call site and one save. See the updated `CustomerServiceRequests.Status`
   > section and Implementation note #6.

3. **Naming: collapse `"In Progress"` / `"InProgress"` to one spelling.** The booking-side
   literal is `"In Progress"` (with a space) and the computed `progressStatus` value is
   `"InProgress"` (no space, line 101) — two branches of the same word live in the same API
   response. Flutter would prefer backend standardize on a single spelling for both (pick
   whichever matches existing convention elsewhere in `api.txt`) rather than have the mobile
   app carry two string variants of "in progress" that are easy to typo or conflate in switch
   statements.
   > _Backend answer:_ Standardized on `"In Progress"` (with the space) for both — updated
   > the `progressStatus` table above. Went that direction rather than stripping the space
   > from the booking-side value because `"In Progress"` (with the space) is already the
   > persisted, whitelisted, badge-mapped value across `ServiceBookings.Status` in multiple
   > existing files (`BookingService.ValidStatuses`, the `Bookings/Details.cshtml` and
   > `Bookings/Index.cshtml` badge switches, and every existing row in the live DB). Changing
   > that would mean a data migration plus touching all of those existing files/rows for zero
   > benefit. `progressStatus` doesn't exist yet, so aligning it to the established spelling
   > was the free direction. One clarification since you're going to be switch-casing on two
   > different fields either way: `status` (raw passthrough of the booking/request column,
   > if you read it directly anywhere) and `progressStatus` (the new computed field) are two
   > separate fields with two separate value sets already (`Requested`/`Completed` only exist
   > in `progressStatus`, not as raw statuses) — the fix here just means that wherever the
   > word "in progress" appears in either field, it's spelled the same way, so there's no
   > typo/conflation risk even though the fields themselves remain distinct.

4. **Flagging, not a question: this is a breaking read-contract change for Flutter.** Once
   `progressStatus` ships, the mobile app's progress bar (currently driven directly off
   `request.status`) needs to switch to reading `progressStatus` plus the
   null/omitted-means-cancelled convention. Noting this here so it's tracked as real client-side
   work alongside the backend change, not just an api.txt enum update.
   > _Backend answer:_ Agreed, tracked. Will call this out explicitly in the `api.txt` diff
   > and the handoff message when this ships (Implementation note #7) — the client progress
   > bar rewire is real client-side work, not a drop-in field rename, since the
   > cancelled-suppression convention (null/omitted `progressStatus` means "hide the bar,
   > show plain Cancelled") is new behavior, not just a renamed value.

## Implementation status

All items in "Implementation notes" above are done. What changed, file by file:

**BookingTracking removed:**
- Deleted: `Controllers/BookingTrackingController.cs`, `Services/BookingTrackingService.cs`,
  `Services/IBookingTrackingService.cs`, `Models/Entities/BookingTracking.cs`,
  `Models/Entities/Booking.cs`, `Models/ViewModels/BookingTrackingViewModels.cs`,
  `Views/BookingTracking/*`.
- `Data/SahulatAppDbContext.cs`: removed the `Bookings`/`BookingTrackings` `DbSet`s and their
  `OnModelCreating` fluent-config blocks.
- `Program.cs`: removed the `IBookingTrackingService` DI registration.
- `Views/Shared/_AdminLayout.cshtml`: removed the "BookingTracking" nav link (the "Tracking"
  sidebar section stays — "Maps/GPS Test" is still there).
- **Unplanned but required, discovered by the compiler, not by review:** removing the
  `Booking` entity broke four other legacy files that referenced it as a type, all part of
  the same already-dead `SahulatAppDbContext`-side subsystem (their tables are also
  `[REMOVED]` in `db.txt`):
  - `Models/Entities/Payment.cs`, `Models/Entities/Review.cs` — dropped their `Booking`
    navigation property (the properties, not the classes — these entities remain, since
    other legacy code still references them).
  - `Models/Entities/ProviderProfile.cs`, `Models/Entities/ServiceRequest.cs` — dropped their
    `ICollection<Booking>` navigation property, same reason.
  - `Services/ReviewService.cs` + `Services/IReviewService.cs` — **deleted outright**, not
    patched. Confirmed via `grep` that no controller anywhere references `IReviewService` —
    `Controllers/ReviewsController.cs` exists but is a standalone stub that always returns a
    "Work In Progress" view (its own doc comment says so: *"Backed by the legacy Reviews,
    Booking, and Customer tables ... all [REMOVED] per db.txt. All routes show a WIP page
    until migrated to the live schema."*). The service was unreachable dead code before this
    change too; removing its `Booking` dependency rather than leaving it broken was the
    correct call, not scope creep. `Program.cs`'s matching DI registration was removed too.
    `Views/Reviews/*` and `Models/ViewModels/ReviewViewModels.cs` were left in place —
    harmless, referenced by nothing now, out of scope to chase further.

**Whitelist (Implementation note #2):**
- Added `Helpers/RequestStatusConstants.cs` (`ClientEditableStatuses = ["Pending",
  "Cancelled"]`).
- `Services/CustomerServiceRequestService.cs`, `UpdateRequestAsync`: now rejects any
  `request.Status` outside that list with `400` / `"Invalid status value."` before the
  cancel-reason check. `Services/ServiceRequestService.cs` (admin) was left untouched, as
  planned — its own, wider `ValidStatuses` list already existed and is unaffected.

**`Closed` visibility gate (note #3):**
- `Services/ServiceBookingApiService.cs`: `ContactVisibleStatuses` → added `"Closed"`.
- `Services/CustomerServiceRequestService.cs`: `ProviderVisibleStatuses` → added `"Closed"`.

**`/start` endpoint (note #4):**
- `Services/IBookingService.cs` + `Services/BookingService.cs`: new `StartJobAsync(bookingUid,
  providerUid, ...)` — validates the booking belongs to `providerUid` and is currently
  `Accepted`, then sets `Status = "In Progress"`.
- `Services/IServiceBookingApiService.cs` + `Services/ServiceBookingApiService.cs`: new
  `StartJobAsync` wrapper, same pattern as `RespondToBookingAsync`/`VerifyCompletionAsync`.
- `Models/Api/StartJobDto.cs`: new, just `{ ProviderUid }`.
- `Controllers/Api/ServiceBookingsApiController.cs`: new `POST
  /service-bookings/{bookingUid}/start`, same response/error shape as the neighboring
  `/respond` and `/verify-completion` actions.

**`progressStatus` (note #5):**
- `Models/Api/CustomerServiceRequestApiDto.cs`: new `string? ProgressStatus` property.
- `Services/CustomerServiceRequestService.cs`: the old single-stage `MapToDtoExpression()`
  was split into a two-stage read — `MapToProgressInputExpression()` now projects a
  `(Dto, BookingStatus)` pair per request (still one SQL round trip; `BookingStatus` is the
  linked non-`Rejected` booking's raw status, or `null` if none exists), and the new
  `ApplyProgressStatus(dto, bookingStatus)` computes `ProgressStatus` in memory afterward —
  including the `HasScheduleTimeArrived` check (`DateOnly` + free-text `PreferredServiceTime`
  combined into a `DateTime` and compared against `DateTime.Now`). Doing the schedule-time
  comparison in memory rather than inside the SQL expression was a deliberate choice: EF
  Core's SQL translator is not something to trust with ad-hoc string-to-time parsing, and
  correctness matters more here than shaving one extra tiny per-row computation.
  `GetRequestsAsync` and `GetRequestByIdAsync` both call this two-stage path now.

**Post-assignment cancel sync (note #6, Flutter Q2's fix):**
- `Services/BookingService.cs`, `UpdateAsync`: inside the existing `isPostAcceptanceCancel`
  branch, now also loads the linked `CustomerServiceRequests` row and sets its `Status` to
  `"Cancelled"` and `CancelReason` to the same reason, before the single shared
  `SaveChangesAsync` call — one save, one call site, matching the pattern already used by
  `AssignProviderAsync` and the reject branch of `RespondToAssignmentAsync`.

**`api.txt` (note #7):**
- `GET /customer-service-requests` (list + by-id): documented `progressStatus` (values, the
  null-means-cancelled rule, and that it's never derived from `status`); fixed the response
  examples, which had been showing the impossible/never-actually-set raw value
  `"status": "InProgress"` — replaced with real reachable values (`"Pending"`/`"Assigned"`)
  plus the new `progressStatus` field alongside them.
- `PUT /customer-service-requests/{id}`: documented the new whitelist and its `400` failure
  case; fixed the same `"InProgress"` inconsistency in its request/response examples.
- `PUT /service-bookings/{id}`: documented that a post-acceptance cancel now also cancels the
  parent request.
- New section: `POST /service-bookings/{id}/start`, matching the existing doc style for
  `/respond` and `/verify-completion`.
- Two provider/client-visibility notes (on `GET /customer-service-requests` and
  `GET /service-bookings`) updated to include `Closed` alongside `Accepted`/`In
  Progress`/`Completed`.

**Verification performed:**
- `dotnet build -c Release` succeeds with zero errors, zero new warnings, after every change
  in this doc (multiple full rebuilds across the session, each after all edits since the
  previous green build) — both via the WSL-side SDK and, later, the real Windows-side .NET 8
  SDK/runtime (`powershell.exe` → `dotnet build`), since this WSL sandbox only has the .NET 10
  runtime installed and cannot execute a net8.0 app itself.
- Careful manual trace of every touched code path (whitelist rejection, `/start`
  precondition, the two-stage `progressStatus` query, the cancel-sync write) against the
  design above — all match.
- **Automated integration test suite added: `tests/HomeServicesPortal.Tests/`** (xUnit,
  `DatabaseFixture.cs` + `StatusWorkflowTests.cs`). Runs against a real database — whatever
  `HomeServicesPortal/appsettings.json`'s `DefaultConnection` currently points at, same
  connection the running app uses, resolved by walking up from the test binary's output
  folder to find that file. A shared fixture inserts one throwaway `UsersLogin`/`Client`/
  `Provider`/`ClientAddress` set (reusing an existing active `ServiceCategory` rather than
  inserting a new one); each test builds its own `CustomerServiceRequest`/`ServiceBooking` rows
  on top of that and deletes them in a `finally` block regardless of pass/fail; the shared
  fixture rows are removed in `DisposeAsync` after the whole run. Calls the real service
  methods directly (`CustomerServiceRequestService`, `BookingService`) — not a mock, not
  in-memory EF — so this exercises the actual SQL translation of the two-stage
  `progressStatus` query, not just the C# logic around it.
- **Run via `dotnet test` through the real Windows .NET 8 runtime (`powershell.exe`,
  `Set-Location -LiteralPath` to handle the `[D]` in the repo path): 14/14 passed.** Covers the
  client whitelist (rejects a client self-reporting `Completed`, allows `Cancelled` +
  reason), all six `progressStatus` branches (`Requested`; `Assigned` from a `Pending`- or
  `Accepted`-but-not-yet-due booking; `In Progress` both via a real `In Progress` booking
  status and via an `Accepted` booking past its scheduled time; `Completed` from both
  `Completed` and `Closed`; `null` — not a `"Cancelled"` stage — when the booking is
  `Cancelled`), the `Closed` contact-visibility fix, `/start`'s three paths (success, wrong
  booking status, wrong provider), and the post-acceptance cancel sync writing `Cancelled` to
  both the booking and the linked request in one save.
- Post-run cleanup independently confirmed: queried `GET /customer-service-requests` on the
  live running instance immediately after the test run and found 0 rows matching
  `AUTOTEST*` among 157 total requests — the fixture's teardown left no residue.

To re-run: `dotnet test tests/HomeServicesPortal.Tests/HomeServicesPortal.Tests.csproj -c
Release` (from a shell with the .NET 8 runtime — this WSL sandbox can't run it, use
PowerShell/Windows or wherever the app itself runs). Safe to run repeatedly; every row it
touches is scoped to its own fixture and cleaned up per-test and at teardown.

**Still open / not part of this change (out of scope, noted for later):**
- The Flutter client and provider apps still need to be rewired to consume `progressStatus`
  and call the new `/start` endpoint — tracked in Flutter Q4 above, not started here.
- The pre-existing `[AllowAnonymous]`/no-ownership-check gap on `CustomerServiceRequestsApiController`
  and `ServiceBookingsApiController` (see `docs/auth-gap-report.md`) is unchanged — the new
  `/start` endpoint inherits the same lack of authentication as every other action on that
  controller. Not something this change was scoped to fix, but worth remembering it means
  `/start` is currently callable by anyone who knows a `bookingUid`, same as `/respond` and
  `/verify-completion` already are.
