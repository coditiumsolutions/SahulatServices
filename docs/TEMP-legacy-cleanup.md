# TEMP: legacy endpoint cleanup candidates

Temporary notes, written 2026-10-01. Nothing here has been done. Delete this file once the items are resolved or moved
elsewhere. Each item must be shown to the owner (diff plus api.txt / db.txt edits, mirrored to the Flutter api.txt)
and approved before any change.

## Context

- GPS is now production-level. Store builds published 2026-09-30: Android `1.0.4+10`, iOS `1.0.6+10`.
- The app version gate (`GET /api/v1/app/config`) is NOT in those builds yet; the Flutter agent still has it on their
  list in `docs/flutter-changes.md`. Until a gate-aware build ships and is adopted, old builds cannot be forced to
  update, so compatibility branches used by old builds are unsafe to remove.
- `AppConfig` currently has one shared `MinimumRequiredVersion` (1.0.3). Android and iOS version numbers differ, so
  per-platform minimums are needed before raising them (Android 1.0.4, iOS 1.0.6).
- Unconfirmed: whether builds 1.0.4 / 1.0.6 send `labourAmount` and `categoryIds`.

## Candidates (safest first)

### 5. Admin controllers on dead legacy tables (low risk, no app impact)
- `Controllers/ProviderLocationsController.cs`, `Controllers/ProviderAvailabilityController.cs`,
  `Controllers/ReviewsController.cs` (each already shows "feature unavailable"), plus their views, nav entries, view
  models, and the old `SahulatAppDbContext` entities they use.
- Check first: nothing else references the entities; `Models/ViewModels/ProviderLocationViewModels.cs`,
  `Models/Entities/ProviderLocation.cs`.

### 2. `"Pending"` legacy request status (medium)
- `Helpers/RequestStatusConstants.cs` (`LegacyPending`), `Services/ServiceRequestService.cs` (allowed-status list,
  filter folding, count folding), `Services/DashboardService.cs:27`, api.txt ~L1955.
- First: count live `CustomerServiceRequests` rows with Status = 'Pending'; run a one-off UPDATE to 'Initiated' if
  any. Old builds may still send `Pending` when editing a request.

### 3. Deprecated `Providers.CategoryUid` scalar (medium)
- `Entities/Provider.cs:44`, `Services/ProviderDetailService.cs:103`, `Services/UserService.cs:347`,
  `Services/IProviderCategoryService.cs:15`, api.txt L1169 and L1208.
- First: confirm nothing reads the column (including the admin portal and the old context), then decide whether to
  drop the DB column too (needs a SQL script and a db.txt bump).

### 1. Optional `labourAmount` on job completion (HIGH, money)
- `Services/BookingService.cs:1258-1276` (`TODO(remove after old app retired)`),
  `Models/Api/VerifyCompletionPasscodeDto.cs:19-23`, api.txt ~L2690-2704.
- Result: `labourAmount` becomes required; commission always on labour. Old builds that omit it would fail to
  complete jobs. Wait for the version gate plus a raised minimum.

### 4. Single-category registration (HIGH)
- `Services/AuthService.cs:262`, api.txt L100-170 (`categoryId` / `categoryName` without `categoryIds`).
- Result: registration requires `categoryIds`. Breaks old builds. Same precondition as #1.

## Review of commits and api.txt up to 2026-09-30 (added 2026-10-01)

Result: no additional held-back breaking changes were found. The only deferred hard cutovers are the ones above, and
they are all already marked `TODO(remove after old app retired)` (the marker `docs/flutter-changes.md` says to grep for).
The "Held for approval" list in `docs/flutter-changes.md` is empty, and `docs/feature-plan-multi-provider-notifications-billing-categories.md`
shows the three held items were all implemented in an additive form instead:
- `register-provider` single category -> `categoryIds` (this file's #4)
- completion `labourAmount` / `materialItems` (#1)
- `Providers.CategoryUid` drop, described there as a "fast-follow cleanup PR" (#3)

Exact code locations for the TODO markers (grep `TODO(remove after old app retired)`):
- #1: `Services/BookingService.cs:1258`, `Models/Api/VerifyCompletionPasscodeDto.cs:18`
- #4: `Services/AuthService.cs:183`, `DTOs/RegisterProviderRequest.cs:45`

Checked and found nothing to remove:
- Category cap of 3 (commit bd24f2f) was already shipped as a deliberate breaking change.
- `progressStatus` (91b779d): additive; the backend still returns `status`, so there is no backend compat code. The
  client progress-bar rewire is Flutter-side only.
- `BookingTracking` removal (91b779d) is complete. Other new fields (`clientLatitude/Longitude`, scheduled date/time,
  `PoliceVerification`, forward geocoding, service titles) are purely additive.

Watch items (not version-gated cleanups, not recommended as part of this):
- Client `cnic` on `register-client` is optional since 2026-09-04. The Flutter placeholder-CNIC workaround
  (`_randomPlaceholderCnic`) is Flutter-side. The backend still accepts the field; it could stop reading it once old
  builds are gone, but nothing is gained by doing that.
- `Otp:IncludeInResponse` is `true` in every appsettings file (including production): the OTP code is returned in the
  API response. api.txt marks this TEMPORARY until an SMS gateway exists. Not about app versions, so it is not listed
  as a cleanup, but it is a security gap worth tracking next to `docs/auth-gap-report.md`.

Unchanged blocker: whether builds 1.0.4 (Android) / 1.0.6 (iOS) already send `labourAmount` and `categoryIds` is still
unconfirmed, and they have no version gate, so #1 and #4 stay on hold.

## Not a candidate
- `PUT /api/provider-locations/{providerUid}` (REST location push, "fallback" for clients without SignalR) is part of
  the live GPS design; only review it if the owner asks.

## Suggested order
1. #5 now, then #2 after the row-count check.
2. #3 after confirming nothing reads the column.
3. #1 and #4 only after a gate-aware build ships and the per-platform minimum version is raised.

## Decisions after the Flutter audit (2026-10-08)

Audited builds: Flutter commit 9cdaaf2, Android 1.0.5+11 / iOS 1.0.7+11. These are the first builds with the version
gate, so Android 1.0.4 and below and iOS 1.0.6 and below cannot be forced to update. The version gate in the app only
runs on cold launch (not on resume).

- #4 single-category registration: DONE 2026-10-08. `categoryIds` + `primaryCategoryId` are required; `categoryId` /
  `categoryName` removed from `RegisterProviderRequest` and `AuthService`. Response fields are unchanged. Low residual
  risk: older builds already sent `categoryIds` (Flutter a858135, 2026-09-22) but this could not be proven.
- #2 "Pending" status: PARTLY DONE 2026-10-08. The client PUT no longer accepts "Pending" (400 "Invalid status value.").
  Kept on purpose: `LegacyPending` in `IsUnassigned`, `Normalize`, the admin `ServiceRequestService` and
  `DashboardService`, so any existing Pending rows still behave as Initiated. Still to do: count live Pending rows, run
  a one-off UPDATE to Initiated, then these can go.
- #1 optional `labourAmount` / `materialItems`: KEPT by owner decision. The app omits them when the provider enters no
  labour/materials. The TODO markers were reworded to KEEP, so grep for the old marker no longer finds them.
- #3 `Providers.CategoryUid` scalar: NOT SAFE yet. The latest build still reads `categoryUid` from
  `GET /api/provider-profiles/{userId}` (hard non-null cast, used at login), `categoryId` from
  `GET /api/providers-detail/{uid}`, and echoes `categoryId` in `PUT /api/providers-detail/{uid}`. Wait for the next
  Flutter build (flutter-changes v1.13.0, "TODO for the next build") to be live and adopted.
- #5 dead admin controllers: still not done (no app impact).
