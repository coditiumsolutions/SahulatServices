# Client Feature Breakdown: Multi-Provider Assign, Admin Notifications, Labour/Material Billing, Provider Multi-Category

## Context

The client submitted four vague feature requests for the Sahulat Ghar Tak backend. Each has been investigated against the live code/schema (`AppDbContext`, not the legacy `SahulatAppDbContext`) and broken into concrete DB/API/App/backend-only work. Key existing facts that shape every decision below:

- The booking-assignment fan-out (one job → many providers) **already exists** (`BookingService.AssignProviderAsync`); what's missing is safe, idempotent race resolution when the first provider accepts.
- The admin notification system (`AdminNotification` table + `AdminNotificationsHub` SignalR + polling) already exists for one notification type (`ServiceRequestCreated`); it needs to be extended, not built from scratch.
- There is **no labour/material split** anywhere today — commission is currently computed on the whole `FinalAmount`.
- Provider→Category is currently a strict 1:1 scalar FK, used as an exact-match filter in ~10 places.
- **No test suite exists.** No transactions/rowversion/concurrency tokens exist anywhere in the booking code today (team explicitly avoids `BeginTransactionAsync` due to `SqlServerRetryingExecutionStrategy` constraints). Every plan below must respect that constraint while still being race-safe.
- `db.txt` and `api.txt` (repo root) are the source of truth and must be updated for every DB/API change in this plan. Do **not** update `docs/api-audit-report.md` (stale, per standing instruction).
- Migrations target `AppDbContext` explicitly: `dotnet ef migrations add <Name> --context AppDbContext --project HomeServicesPortal`.

### Scope constraints for this implementation pass (per user direction)

- **"No frontend changes" means the Flutter mobile app specifically — the ASP.NET admin portal (server-rendered `.cshtml` views under `HomeServicesPortal/Views/`) is in scope and should be implemented fully**, including Feature 7's notification-icon UI.
- **Database changes must be additive/non-breaking wherever possible.** Anything that would break the live production app or an existing live API contract must be held for explicit approval before implementing — do not just implement and flag it after the fact.
- Any backend change whose corresponding Flutter app work is deferred (not implemented in this pass) must be recorded in a new tracking file: **`docs/flutter-changes.md`**. This file lists, per feature, exactly what changed on the backend and what the Flutter app needs to do to complete the integration (new/changed endpoints, request/response shape changes, new fields to display, etc.) — it is the single running checklist for the app team's follow-up work. Update it incrementally as each feature is implemented, don't write it all at once at the end.
- **Feature 9's breaking change is explicitly held for approval**: `ProviderCategories` junction table, backfill, sync helper, and all non-breaking additive endpoints/read-path fixes are implemented now. The breaking change to `POST /api/auth/register-provider`'s request contract (single `categoryId` → `categoryIds` array) is **not** implemented in this pass — it's documented in `docs/flutter-changes.md` as a pending breaking change awaiting go-ahead, along with a suggested non-breaking interim path (e.g. accept an optional `categoryIds` array alongside the existing singular field, falling back to singular if array is omitted — a backward-compatible way to ship this without waiting, worth proposing when that work is greenlit).

---

## Feature 2 — First-accept-wins multi-provider assignment

**Problem today:** `BookingService.RespondToAssignmentAsync` (`HomeServicesPortal/Services/BookingService.cs:722-775`) only loads the single booking row for the responding provider and checks `Status=="Pending"` in memory before saving — it never looks at sibling `Pending` bookings for the same `RequestUid`. Two providers can race and both "accept"; even without a race, an accepted job never disappears from other providers' pending list.

### 1. Database
- No schema/CHECK-constraint migration needed. Reuse existing `Status="Cancelled"` for superseded siblings, with `CancelReason = "Assigned to another provider"` (per user decision — avoids an ALTER CHECK CONSTRAINT migration and avoids touching every place that filters on `Status`).
- `db.txt`: add a note under `ServiceBookings.CancelReason`/`Status` documenting this specific reason string as the marker for "lost the race to another provider" (distinct from customer/provider-initiated cancellations, which use different reason text).

### 2. API changes
- Rewrite `RespondToAssignmentAsync` (`BookingService.cs:722-775`) accept path to use an atomic conditional update instead of check-then-act:
  - `await _db.ServiceBookings.Where(b => b.Uid == bookingUid && b.Status == "Pending").ExecuteUpdateAsync(s => s.SetProperty(b => b.Status, "Accepted").SetProperty(b => b.AcceptedOn, DateTime.UtcNow)...)` — check the returned row count.
    - Row count `1` → claim succeeded, proceed to generate passcode and cancel siblings.
    - Row count `0` → booking was already claimed/cancelled/rejected; return a well-defined `ApiResponse` failure (e.g. `409`-style body, not a 500) — message: "This job has already been assigned to another provider" (or "already responded to" if it was this same provider retrying).
  - `ExecuteUpdateAsync` runs as a single atomic statement against SQL Server — it does not require `BeginTransactionAsync`, so it's compatible with the team's existing avoidance of explicit transactions under `SqlServerRetryingExecutionStrategy`.
  - Immediately after a successful claim, bulk-supersede siblings in one more `ExecuteUpdateAsync`: `_db.ServiceBookings.Where(b => b.RequestUid == requestUid && b.Uid != bookingUid && b.Status == "Pending").ExecuteUpdateAsync(s => s.SetProperty(b => b.Status, "Cancelled").SetProperty(b => b.CancelReason, "Assigned to another provider"))`.
  - Update `CustomerServiceRequest.Status` from `"Assigned"` to `"Accepted"` on successful claim (existing status enum already supports this transition).
- **Idempotency:**
  - Same provider double-tapping accept: second call finds `Status != "Pending"` (already `"Accepted"`), row count 0 → return success-idempotent response (message: "Already accepted") rather than an error, since it's the same provider re-confirming their own prior success. Distinguish this case from "lost to someone else" by checking if the booking's current `ProviderUid` matches the caller before deciding the message (still no error/500 either way).
  - Provider accepting an already-superseded booking: row count 0, `ProviderUid` doesn't match an accepted state owned by them → return the "already assigned to another provider" failure response.
- Reject path: no change to core logic needed, but note it already resets the request to `"Initiated"` for reassignment — confirm this is still desired when other siblings are still `Pending` (if other providers are still pending on the same request, resetting to `"Initiated"` prematurely could conflict — recommend only resetting the request to `"Initiated"` if this was the *last* remaining `Pending` sibling; otherwise leave the request `"Assigned"` and let the others still respond). Implement this check alongside the reject branch.
- `GET /api/service-bookings?providerUid=` needs **no query change** — once a sibling's `Status` flips to `"Cancelled"`, it naturally drops out of any existing "pending jobs for this provider" filter, so removal from other providers' windows falls out for free.
- API authentication/authorization hardening (e.g. `[AllowAnonymous]` on `ServiceBookingsApiController`) is explicitly out of scope here — per direction, security work across the whole application will be addressed together once features are complete.
- `api.txt`: update the `/respond` endpoint doc with the new idempotent response shapes (already-accepted, lost-race, success).

### 3. Flutter app changes required (not implemented this pass — track in `docs/flutter-changes.md`)
- Provider app's "pending jobs" list should re-fetch after any accept/reject action so a job that another provider just won disappears from the list.
- App must handle the new "already assigned to another provider" idempotent-failure response gracefully (toast/message) instead of treating it as a generic error.
- No backend contract change is required for existing successful-accept responses — this is additive behavior (new failure-response case + siblings disappearing from list queries), so the app can adopt the improved handling whenever convenient; it won't break against the old app in the meantime since a losing provider simply gets an error response it may currently mishandle as generic.

### 4. Backend-only changes
- The `ExecuteUpdateAsync` conditional-claim logic itself, and the sibling-supersede bulk update, are pure backend changes.

---

## Feature 7 — Three admin-portal notification icons

**Reuse target:** `AdminNotification` entity/table, `AdminNotificationService`, `AdminNotificationsController`, `AdminNotificationsHub`, and the bell UI/JS in `_AdminLayout.cshtml` (lines 757-777 markup, 446-580 CSS, 936-1160 JS) — all confirmed generic enough to extend by `Type`.

### 1. Database
- No new table. Add two new `Type` string constants used in the existing `AdminNotifications` table: `CustomerRequestCancelled`, `ProviderBookingCancelled` (alongside existing `ServiceRequestCreated`).
- Decision on actor-tracking gap: rely on `Type` alone to distinguish "who cancelled" (cheaper, no migration) rather than adding `CancelledBy`/`CancelledAt` columns — the `Type` string already tells the admin portal which icon/category a cancellation belongs to, and the triggering call site inherently knows which actor caused it.
- `db.txt`: document the two new `Type` values under `AdminNotifications`.

### 2. API changes
- Extend `IAdminNotificationService`/`AdminNotificationService` (`HomeServicesPortal/Services/AdminNotificationService.cs`) with two new methods mirroring `NotifyServiceRequestCreatedAsync`'s exact pattern (insert row, best-effort SignalR push via `_hub.Clients.Group("admins").SendCoreAsync(...)` in try/catch):
  - `NotifyCustomerCancellationAsync(requestUid, ...)`
  - `NotifyProviderCancellationAsync(bookingUid, ...)`
- Make `GetRecentAsync`/`MarkAllReadAsync` type-scoped without breaking existing behavior: add an optional `IEnumerable<string> types = null` parameter — `null`/omitted means "all types" (preserves current single-bell behavior for any caller that doesn't pass it); when provided, filters `WHERE Type IN (@types)` for feed/unread-count, and scopes the read-marking to just those types.
- `AdminNotificationsController` (`HomeServicesPortal/Controllers/AdminNotificationsController.cs`): add a `type`/`types` query parameter to `GET feed`, `GET unread-count`, and `POST mark-read`, defaulting to the existing single-`ServiceRequestCreated` behavior when omitted so the current bell keeps working unchanged; the two new icons call the same endpoints with their own `type` value.
- Call sites (fire-and-forget, same `IServiceScopeFactory.CreateAsyncScope()` + try/catch pattern used today):
  - `CustomerServiceRequestService.UpdateRequestAsync` (`HomeServicesPortal/Services/CustomerServiceRequestService.cs:122-189`) — when the status transition target is `"Cancelled"`, call `NotifyCustomerCancellationAsync`.
  - `BookingService.UpdateAsync` (`HomeServicesPortal/Services/BookingService.cs:294-362`), inside the existing `isPostAcceptanceCancel` branch (lines 309-316) — call `NotifyProviderCancellationAsync`. Also consider hooking the reject-while-pending path in `RespondToAssignmentAsync`'s reject branch if the client wants "provider declined before accepting" to also raise this icon (reasonable given the client's stated reason: "if the provider cancels in any emergency we can shift the job" — a pre-accept reject is a milder version of the same need; recommend including it).
- `api.txt`: document the `type`/`types` query param on the three notification endpoints.

### 3. Flutter app changes required
- None — this is an admin-portal (server-rendered MVC) feature only; the Flutter app is unaffected. No entry needed in `docs/flutter-changes.md` for this feature.

### 4. Backend-only changes (portal UI is backend-rendered, still counts as "backend changes, app unaffected")
- `_AdminLayout.cshtml`: duplicate the `.hs-notify-wrap` bell block (lines 757-777) twice more — one for "Customer notification" (icon suggestion: `fa-user-slash` or a cancel-glyph variant), one for "Provider notification" (`fa-user-tie` + cancel glyph) — each with its own badge/panel IDs.
- Parameterize the inline JS IIFE (lines 936-1160) so it can be instantiated three times against three different `type` values and three sets of DOM element IDs, rather than copy-pasting the whole block verbatim (extract the IIFE body into a small reusable function taking `{ type, badgeId, panelId, listId }`).
- CSS (lines 446-580) reused as-is across all three icons.

---

## Feature 8 — Labour charge + itemized material breakdown, ledger stays labour-only, customer billed the full amount

**Decision (per user):** provider enters `LabourAmount` as a single figure, but materials are entered as an **itemized breakdown** (e.g. "light bulb — Rs 200", "pipe fitting — Rs 150") rather than one lump `MaterialAmount`. Admin can always enter/edit both via the existing booking-edit form (non-breaking — admin portal is in scope). The mobile passcode-verify completion endpoint *ideally* also captures these from the provider, but making `labourAmount`/`materialItems` part of that request is a **breaking change to a live endpoint the current Flutter app calls** — per the DB/API change-approval rule, this endpoint contract change is **held for approval** and tracked in `docs/flutter-changes.md` rather than implemented this pass. In the interim, the fields are nullable/optional on the backend (admin can fill them in after the fact via the booking-edit form/new material-items endpoints) so nothing breaks for the current app.

### 1. Database
- Migration `AddLabourAndMaterialItemsToServiceBookings`:
  - Add `LabourAmount decimal(10,2) NULL` to `ServiceBookings`.
  - New child table `BookingMaterialItems`:
    ```
    Uid          int          NOT NULL PK IDENTITY
    BookingUID   int          NOT NULL FK -> ServiceBookings.UID
    ItemName     nvarchar(200) NOT NULL
    Quantity     decimal(10,2) NOT NULL DEFAULT (1)
    UnitPrice    decimal(10,2) NOT NULL
    Amount       decimal(10,2) NOT NULL  -- Quantity * UnitPrice, stored (not computed column) for historical stability if UnitPrice reference data ever changes
    CreatedOn    datetime     NOT NULL DEFAULT (getdate())
    ```
  - `MaterialAmount` is **not** a stored column on `ServiceBookings` — it's computed as `SUM(BookingMaterialItems.Amount)` for that booking wherever needed (e.g. a `[NotMapped]` property on the entity, or a query-time projection), avoiding a denormalized total that can drift from its line items.
- `FinalAmount` formula becomes `LabourAmount + SUM(MaterialItems.Amount) + VisitCharges + AdditionalCharges − Deductions`. `EstimatedAmount` remains for the pre-completion estimate, unchanged.
- `db.txt`: document `LabourAmount`, the new `BookingMaterialItems` table, and the updated `FinalAmount` formula (noting material total is derived, not stored).

### 2. API changes
- `ResolveCommissionAmounts` (`HomeServicesPortal/Services/BookingService.cs:968+`) call sites change their base input from `booking.FinalAmount` to `booking.LabourAmount` only: `commissionAmount = LabourAmount * CommissionValue/100 (or fixed)`, `providerEarning = LabourAmount − commissionAmount`. `CustomerPaid`/`FinalAmount` continue to reflect the full labour+materials+visit+additional−deductions total — only the commission/ledger base narrows to labour. If `LabourAmount` is null (not yet entered), fall back to treating commission base as `0` (or as `FinalAmount` if that's less disruptive to existing reports) until an admin fills it in — needs a sensible default so nothing crashes on bookings created before this feature lands; recommend `0` with a portal-visible "Labour amount not set" flag rather than silently defaulting to the old whole-amount behavior, so it's obvious which historical/in-flight bookings need attention.
- `ApplyAssignTotals`/`ApplyBookingTotals` (`BookingService.cs:891-936`, admin assign/edit forms) — extended to accept `LabourAmount` and a material-items list, so admin can set/edit these via the portal today. This is additive (new optional fields), not breaking.
- **Held for approval, not implemented this pass:** changing `VerifyCompletionPasscodeAsync`'s (lines 850-857) request contract to require/accept `labourAmount`/`materialItems` from the mobile app at completion time — this is the endpoint the live Flutter app calls to complete a job, and changing its contract needs a coordinated app release. Tracked in `docs/flutter-changes.md`.
- New endpoints for managing material items (additive, new routes, non-breaking), usable by admin now and by the app later once wired up: `GET /api/service-bookings/{bookingUid}/material-items`, `PUT /api/service-bookings/{bookingUid}/material-items` (full-replace of the item list, recomputes `FinalAmount`/`CustomerRemaining`).
- `PaymentService.RecordBookingCompletionAsync` (`HomeServicesPortal/Services/PaymentService.cs:162-296`) needs **no structural change** — it already writes `PaymentLedger` rows from `booking.CommissionAmount`/`booking.ProviderEarning`/`booking.CustomerPaid`, which will now correctly reflect labour-only commission once the upstream calculation and stored `FinalAmount` are fixed.
- `api.txt`: document the new material-items endpoints and the admin assign/edit form's new optional fields now; add a clearly-marked "planned, not yet active" section for the eventual passcode-verify contract change.

### 3. Flutter app changes required (held for approval — track in `docs/flutter-changes.md`)
- Provider app's job-completion/passcode-verify screen will eventually need: a Labour Charges input, plus a repeatable "add material item" row (item name, quantity, unit price) — but this is NOT implemented this pass since it requires the held-back breaking contract change on `VerifyCompletionPasscodeAsync`.
- Suggested non-breaking interim path to propose when this is greenlit: keep `labourAmount`/`materialItems` optional on the existing endpoint (rather than required) so an old app version can keep completing jobs without them, while a new app version can start sending them — avoids a hard-forced simultaneous release.
- Customer-facing receipt/invoice view, if the app has one, should eventually show the itemized material breakdown plus labour charge and grand total — also deferred, tracked alongside the above.

### 4. Backend-only changes
- Admin booking-edit form (`HomeServicesPortal/Views/Bookings/_FormFields.cshtml:78-167`): add a `LabourAmount` input plus a dynamic add/remove material-item row UI (item name, quantity, unit price, computed line amount); update the client-side recompute script (lines 170-203) so `FinalAmount = Labour + Σ(material item amounts) + Visit + Additional − Deductions`, and `Commission = rate% (or fixed) of LabourAmount` (not `FinalAmount`).
- `ServiceBooking` entity: add `LabourAmount` property and `ICollection<BookingMaterialItem> MaterialItems` nav property; new `BookingMaterialItem` entity + `AppDbContext.BookingMaterialItems` DbSet.
- `ResolveCommissionAmounts` base-input change, the material-items CRUD logic, and the two new management endpoints are fully backend/admin-portal-only this pass — no Flutter app dependency yet.

---

## Feature 9 — Provider multi-category support (junction table as source of truth from day one)

**Decision (per user):** introduce `ProviderCategories` as the canonical source of truth immediately; `Providers.CategoryUid` becomes a synced, deprecated "primary category" convenience column, explicitly marked for removal in a fast-follow cleanup once call sites are migrated — not kept indefinitely as a permanent parallel field.

**Scope for this pass:** the junction table, backfill, sync helper, read-path matching fixes, and new additive category-management endpoints are all implemented now (non-breaking). The breaking change to `POST /api/auth/register-provider` (single `categoryId` → `categoryIds` array) is **held for approval** and tracked in `docs/flutter-changes.md` rather than implemented — registration keeps working exactly as today (single category, seeded as the primary + first junction row) until that change is greenlit.

### 1. Database
- Migration `AddProviderCategoriesJunctionTable`:
  ```
  ProviderCategories
    Uid          int      NOT NULL PK IDENTITY
    ProviderUID  int      NOT NULL FK -> Providers.UID
    CategoryUID  int      NOT NULL FK -> ServiceCategories.UID
    IsPrimary    bit      NOT NULL DEFAULT (0)
    CreatedOn    datetime NOT NULL DEFAULT (getdate())
    UNIQUE (ProviderUID, CategoryUID)
  ```
- In the same migration's `Up()`, backfill via raw SQL (`migrationBuilder.Sql(...)`): insert one row per existing `Providers.CategoryUid`, `IsPrimary=1` — so no provider is ever without a junction row.
- `Providers.CategoryUid` stays in the schema for now (still `NOT NULL`) but `db.txt` marks it explicitly: **"DEPRECATED — mirrors the `IsPrimary=1` row in `ProviderCategories`; do not add new dependencies on this column; scheduled for removal once all call sites are migrated (tracking: fast-follow cleanup PR)."**
- `db.txt`: add the new table's definition alongside this deprecation note.

### 2. API changes
- New entity `ProviderCategory` (`HomeServicesPortal/Entities/ProviderCategory.cs`) + `AppDbContext.ProviderCategories` DbSet + unique index config in `OnModelCreating`.
- `Provider.cs`: add `ICollection<ProviderCategory> ProviderCategories` nav property.
- New centralized sync helper — `ProviderCategoryService.SyncCategoriesAsync(providerUid, List<int> categoryUids, int primaryCategoryUid)` — writes the junction rows AND updates `Providers.CategoryUid` to match `primaryCategoryUid`, so both stay consistent from one place rather than duplicated logic at every call site. This is the only place that should ever write to `CategoryUid` going forward.
- Call sites migrated to read from the junction table (`Any()`/join) instead of the scalar, since these are the ones where "any of the provider's categories" must match — required for the feature to actually work:
  - `BookingService.cs:485` job-inbox matching: `p.CategoryUid == request.CategoryUid` → junction-table `Any()` check.
  - `ProviderLocationQueryService.cs:45` nearby-provider filtering → junction-table `Any()` check.
  - `CommissionRules` category-scope resolution → junction-table `Any()` check.
  - `ServiceCategoryService.cs:295` category-delete in-use check → also check `ProviderCategories.Any(pc => pc.CategoryUid == categoryUid)`, not just the scalar.
- Call sites that only need to write (create/edit), routed through the new sync helper instead of setting `CategoryUid` directly:
  - `ServiceProviderService.cs:312,348,382,408` (admin create/edit)
  - `ProviderDetailService.cs:97`, `UserService.cs:175-315`, `AuthService.cs:220` (mobile registration/profile-edit)
- New endpoints (additive, non-breaking):
  - New `GET /api/providers/{providerUid}/categories` — list of category UIDs/names for a provider, flags which is primary.
  - New `PUT /api/providers/{providerUid}/categories` — body `{ "categoryIds": [1,3,5], "primaryCategoryId": 1 }` — full-replace via the sync helper, for post-registration category management. This alone is enough for a provider (or admin, on their behalf) to become multi-category without touching the registration endpoint.
  - **Held for approval:** `POST /api/auth/register-provider` changing from single `categoryId`/`categoryName` to `categoryIds: [int]` + `primaryCategoryId: int`. Suggested non-breaking interim path to propose when greenlit: accept an optional `categoryIds` array alongside the existing singular field — if provided, use it (first/flagged entry as primary); if omitted, fall back to today's singular behavior — avoids forcing a simultaneous app release.
- Admin web form (`Views/ServiceProviders/_FormFields.cshtml:42-49`) — in scope, ASP.NET-only: replace single `<select>` with multi-select checkboxes plus a primary-category designation (radio alongside checkboxes, or "first checked = primary"); `ServiceProviderService` create/edit take `List<int> CategoryUids` + `int PrimaryCategoryUid`, routed through the sync helper.
- `api.txt`: document the two new category-management endpoints now; add a clearly-marked "planned, not yet active" section for the eventual `register-provider` contract change, and note `categoryId`/`CategoryUid` fields elsewhere in the doc now mean "primary category" specifically pending the fast-follow removal.

### 3. Flutter app changes required (track in `docs/flutter-changes.md`)
- Not required immediately: registration keeps working unchanged (single category) since that endpoint's contract isn't changing this pass.
- Available now, app team's choice when to adopt: a provider profile screen could call the new `GET`/`PUT /api/providers/{providerUid}/categories` endpoints to let a provider view/manage additional categories post-registration, without waiting on the registration-endpoint change.
- Held for later (once the registration contract change is greenlit): registration screen change to multi-select (checkboxes), submitting `categoryIds` array + primary designation.
- Any screen displaying "provider's category/expertise" as a single value could be updated to show the full list where relevant (e.g., provider cards/detail views) — optional, not blocking.

### 4. Backend-only changes
- The `ProviderCategory` entity, DbSet, migration, sync helper, and the read-path call-site migrations (job matching, location query, commission scope, category-delete check) are backend-only besides the endpoint/contract changes called out above.
- **Fast-follow (separate PR, out of scope for this change but noted for tracking):** once `CategoryUid` has zero remaining production reads outside the sync helper, drop the column and remove the sync helper's write to it, making `ProviderCategories` (with `IsPrimary`) the sole source of truth.

---

## Suggested implementation order

1. **Feature 2** (first-accept-wins) — self-contained, no schema change, lowest risk, addresses a real correctness/race bug today.
2. **Feature 7** (notification icons) — mostly additive, reuses an existing pattern, no risk to existing flows.
3. **Feature 9** (provider multi-category) — schema-heavy but the migration/backfill is safe (additive table, `CategoryUid` untouched during transition); do this before Feature 8 since it's more architecturally invasive and best landed while context is fresh.
4. **Feature 8** (labour/material split) — coordinate with the app team since it changes the mobile completion contract; do last since it's the most likely to need a synchronized app release.

## `docs/flutter-changes.md` — new tracking file

Created/updated incrementally as each feature is implemented (not written all at once at the end). Structure: one section per feature, listing (a) what changed on the backend, (b) what's already usable by the app with no backend change needed, (c) what's held for approval / not yet active, and (d) a suggested non-breaking interim contract when relevant. This is the single checklist the app team works off of.

## Verification

- No test project exists in this repo; verification is manual per feature:
  - **Feature 2**: use `scripts/dev-run.ps1` to run locally, hit `/api/service-bookings/{id}/respond` concurrently (e.g. two quick sequential Postman/curl calls with different provider tokens) against a request assigned to 2+ providers; confirm only one succeeds, the other gets the "already assigned" response, and `GET /api/service-bookings?providerUid=` for the loser no longer returns it.
  - **Feature 7**: log into `/adminportal`, trigger a customer cancellation and a provider post-acceptance cancellation via API calls, confirm all three bell icons behave correctly (new two update independently; existing Job Request bell's unread count is unaffected by the other two).
  - **Feature 8**: via the admin booking-edit form (or the new material-items endpoints), set a `LabourAmount` and 2+ material items on a booking, confirm `FinalAmount` reflects labour + summed material items, confirm `BookingMaterialItems` rows persisted match what was entered, and check the resulting `PaymentLedger` row's `CommissionAmount`/`ProviderEarning` are computed off `LabourAmount` only (not the full amount). Mobile passcode-verify completion flow is intentionally unchanged this pass — confirm it still works exactly as before for an app that never sends labour/material data.
  - **Feature 9**: use the new `PUT /api/providers/{providerUid}/categories` endpoint (or the updated admin form) to give an existing provider multiple categories, confirm `ProviderCategories` rows are created and `Providers.CategoryUid` is set to the chosen primary; confirm the provider appears in job-inbox matching for a request in a *secondary* (non-primary) category. Confirm `POST /api/auth/register-provider` still behaves exactly as before (single category) since its contract is untouched this pass.
- After each feature, update `db.txt`/`api.txt`/`docs/flutter-changes.md` and run `dotnet build HomeServicesPortal/HomeServicesPortal.csproj` (via `powershell.exe` from WSL if applicable) to confirm no compile errors before considering the feature done.
