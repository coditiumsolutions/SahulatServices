# API Audit Report — Redundant & Non-Functional Endpoints

<!-- Version: 1.1 | UpdatedAt: 08/07/2026 -->

**Remediation (08/07/2026):** High-priority REST cleanup completed — MVC left untouched for later admin refactor.

- Removed `GET /api/providers/{id}/service-requests` (`ProvidersApiController`) — queried removed `ProviderProfiles` / `ServiceRequests`.
- Removed `/api/provider-profiles` (`ProviderProfilesApiController`) — duplicate of `/api/providers-detail`.
- Deleted API-only DTOs/helpers: `ProviderProfileApiDto`, `ProviderServiceRequestApiDto`, `ProviderServiceRequestResponse`.

**Project:** SahulatGharTak / HomeServicesPortal  
**Database:** SahulatAppDB (live schema documented in `db.txt` v1.9)  
**Reference docs:** `api.txt` v1.5 (active mobile/client APIs)

---

## Executive summary

The codebase runs **two parallel data layers**:

| Context | Purpose | Live DB alignment |
|---------|---------|-------------------|
| `AppDbContext` | Auth, providers, client addresses, customer service requests | **Aligned** with live `SahulatAppDB` |
| `SahulatAppDbContext` | Original admin portal + legacy booking workflow | **Mostly broken** — maps to tables marked `[REMOVED]` in `db.txt` |

**REST APIs documented in `api.txt`:** 22 endpoints — all use `AppDbContext` or `ServiceCategories` (shared live table). These are the intended production APIs.

**Problems found:**

- **1 REST endpoint is non-functional** against the live database.
- **2 REST endpoints are redundant** with documented alternatives (overlap in purpose).
- **12+ admin MVC module areas** still registered but query removed tables (admin portal CRUD, not in `api.txt`).
- **Legacy services** (`ServiceProviderService`, `ProviderAvailabilityService`, etc.) mix both contexts — API paths work where migrated; admin paths often fail.

---

## 1. Active & functional REST APIs (`api.txt`)

These are documented, intended for Flutter/mobile use, and work against the live schema.

| Group | Route prefix | Methods | Data source | Status |
|-------|--------------|---------|-------------|--------|
| Auth | `/api/auth` | POST ×4 | `AppDbContext` | OK |
| Provider availability | `/api/provider-avability-status` | GET, POST, PUT | `AppDbContext.Providers` | OK |
| Provider detail | `/api/providers-detail` | GET, PUT | `AppDbContext.Providers` | OK |
| Client addresses | `/api/client-addresses` | GET, POST, PUT, DELETE | `AppDbContext.ClientAddresses` | OK |
| Customer service requests | `/api/customer-service-requests` | GET, POST, PUT, DELETE | `AppDbContext.CustomerServiceRequests` | OK |
| Service categories | `/api/service-categories` | GET ×2 | `SahulatAppDbContext.ServiceCategories`* | OK |

\* `ServiceCategories` exists in live DB. The API uses the legacy repository/`SahulatAppDbContext` entity, but the **table is valid**. Consider migrating reads to `AppDbContext` for consistency.

---

## 2. Non-functional REST APIs

Endpoints exposed in Swagger/code but **fail or return errors** against the live database.

### `GET /api/providers/{providerUid}/service-requests`

| Field | Detail |
|-------|--------|
| **Controller** | `ProvidersApiController` |
| **Service** | `ServiceProviderService.GetServiceRequestsForProviderAsync` |
| **Documented in `api.txt`** | No |
| **Failure mode** | `500` — *"An unexpected error occurred."* (or SQL invalid object name) |

**Root cause:** Still uses legacy `SahulatAppDbContext` repositories:

1. `ActiveProvidersQuery()` → `ProviderProfiles` table (removed; live data is in `Providers`).
2. `_requestRepo` → `ServiceRequests` table (removed; replaced by `CustomerServiceRequests`).

**Replacement:** Providers should read open jobs from `CustomerServiceRequests` filtered by `CategoryUID` (and optionally status), using `AppDbContext.Providers` for the provider lookup — same pattern as the new customer API.

---

## 3. Redundant / overlapping REST APIs

Endpoints that work (or partially work) but **duplicate** documented APIs or naming is misleading.

### `GET /api/provider-profiles` and `GET /api/provider-profiles/{userUid}`

| Field | Detail |
|-------|--------|
| **Controller** | `ProviderProfilesApiController` |
| **Documented in `api.txt`** | No |
| **Data source** | `AppDbContext.Providers` (fixed; no longer uses `ProviderProfiles` table) |
| **Status** | Functional |

**Overlap with:**

| Endpoint | Difference |
|----------|------------|
| `GET /api/providers-detail/{providerUid}` | Full profile: mobile, description, availability, category name, ratings, jobs |
| `GET /api/provider-profiles?categoryId=` | Lightweight list: uid, name, category, cnic, rating, verified |
| `GET /api/provider-profiles/{userUid}` | Lookup by **user** UID, not provider UID |

**Recommendation:** Keep **one** provider read API surface for mobile:

- **List/browse:** `GET /api/provider-profiles?categoryId=` *or* a new list on `providers-detail`.
- **Full profile / edit:** `GET|PUT /api/providers-detail/{providerUid}`.

Deprecate `provider-profiles` or document it in `api.txt` with a clear use case (e.g. category browse cards only).

---

### Provider “service requests” naming collision

| API | Table | Purpose |
|-----|-------|---------|
| `GET/POST/PUT/DELETE /api/customer-service-requests` | `CustomerServiceRequests` | **Current** — clients create service jobs |
| `GET /api/providers/{id}/service-requests` | `ServiceRequests` (legacy) | **Broken** — provider inbox by category |
| Admin `/ServiceRequests/*` MVC | `ServiceRequests` (legacy) | **Broken** — admin CRUD |

Only `customer-service-requests` is valid. The provider inbox endpoint should be reimplemented against `CustomerServiceRequests`, not removed legacy tables.

---

## 4. Undocumented but functional REST APIs

| Method | Route | Notes |
|--------|-------|-------|
| GET | `/api/provider-profiles` | Works; optional `?categoryId=` |
| GET | `/api/provider-profiles/{userUid}` | Works; parameter is **UsersLogin.UID**, not provider UID |

**Action:** Add to `api.txt` **or** mark deprecated and remove from Swagger after Flutter migration.

---

## 5. Legacy admin MVC routes (non-API, largely non-functional)

These are **not** REST JSON APIs and are **not** in `api.txt`. They power the browser admin UI (`/Bookings`, `/Customers`, etc.) and use `SahulatAppDbContext` + `IRepository<>` for tables **removed** from live `SahulatAppDB`.

| Admin route prefix | Legacy table(s) | Live DB status | Expected result |
|------------------|-----------------|----------------|-----------------|
| `/ServiceProviders` | `ProviderProfiles`, `Users` | Removed | Errors on list/create |
| `/ProviderAvailability` | `ProviderAvailability`, `ProviderProfiles` | Removed | Errors (API uses `Providers` instead) |
| `/ProviderLocations` | `ProviderLocations` | Removed | Errors |
| `/ProviderDocuments` | `ProviderDocuments` | Removed | Errors |
| `/Customers` | `Customers` | Removed | Errors |
| `/ServiceRequests` | `ServiceRequests` | Removed | Errors |
| `/ProviderQuotes` | `ProviderQuotes` | Removed | Errors |
| `/Bookings` | `Bookings` | Removed | Errors |
| `/BookingTracking` | `BookingTracking` | Removed | Errors |
| `/Payments` | `Payments` | Removed | Errors |
| `/Reviews` | `Reviews` | Removed | Errors |
| `/ServiceCategories` (MVC) | `ServiceCategories` | **Exists** | May still work |
| `/Administration/Users` | ASP.NET Identity | Separate | Identity-only admin users |
| `/Admin/Dashboard` | Mixed legacy repos | Partial | Dashboard counts likely wrong |

**Still functional admin areas:**

- `/adminportal`, `/Account/Login` — ASP.NET Identity login
- `/ServiceCategories` MVC — same live table as API (duplicate admin vs API)

---

## 6. Dual-context services (technical debt)

Several services inject **both** `AppDbContext` and legacy repositories. API methods may work while MVC admin methods on the same service fail.

| Service | API methods (OK) | Admin / legacy methods (broken) |
|---------|------------------|----------------------------------|
| `ProviderAvailabilityService` | `Get/SaveProviderAvailabilityStatusAsync` → `Providers` | `GetListAsync`, CRUD → `ProviderAvailability` |
| `ServiceProviderService` | `GetProviderProfilesForApiAsync` → `Providers` | `GetListAsync`, CRUD, `GetServiceRequestsForProviderAsync` → legacy tables |
| `ServiceCategoryService` | `GetActiveCategoriesForApiAsync` | MVC CRUD → same table (OK) |

---

## 7. Other notes

### Route typo (cosmetic)

- Live route: `/api/provider-avability-status` (missing **i** in “availability”).
- Documented intentionally in `api.txt`. Changing it would break existing clients.

### JWT middleware vs auth responses

- `Program.cs` registers JWT bearer authentication.
- Auth APIs return profile fields only (no token). JWT is **not** required for current mobile APIs (`[AllowAnonymous]` on API controllers).
- Not broken, but **unused** for current client flow.

### Removed controllers (historical)

These were deleted in recent work and should **not** be reintroduced without migration:

- `AuthApiController` → replaced by `AuthController` (`/api/auth`)
- `ServiceProvidersApiController` → replaced by `ProviderProfilesApiController` + `ProvidersDetailApiController`

---

## 8. Recommendations (priority order)

### High — fix or remove broken REST endpoints

1. **Fix or remove** `GET /api/providers/{providerUid}/service-requests`.
   - Implement provider inbox using `AppDbContext.CustomerServiceRequests` + `Providers.CategoryUID`.
   - Or remove controller until provider inbox is spec’d.

### Medium — reduce redundancy

2. **Consolidate provider read APIs** — document `provider-profiles` in `api.txt` or deprecate in favor of `providers-detail`.
3. **Migrate `ServiceCategoryService` API reads** to `AppDbContext` and drop dependency on `SahulatAppDbContext` for public APIs.

### Low — admin portal cleanup

4. **Disable or hide** admin MVC menus for modules backed by removed tables (Bookings, Customers, legacy ServiceRequests, etc.).
5. **Long term:** Retire `SahulatAppDbContext` legacy entities or remap admin modules to `AppDbContext` live tables.
6. **Rename** internal DTO `ProviderProfileApiDto` → `ProviderSummaryApiDto` to avoid confusion with removed `ProviderProfiles` table.

---

## 9. Quick reference — REST API status matrix

| Endpoint | In `api.txt` | Live DB | Verdict |
|----------|:------------:|:-------:|---------|
| POST `/api/auth/register-client` | Yes | OK | **Keep** |
| POST `/api/auth/register-provider` | Yes | OK | **Keep** |
| POST `/api/auth/register-staff` | Yes | OK | **Keep** |
| POST `/api/auth/login` | Yes | OK | **Keep** |
| GET/POST/PUT `/api/provider-avability-status` | Yes | OK | **Keep** |
| GET/PUT `/api/providers-detail/{id}` | Yes | OK | **Keep** |
| GET/POST/PUT/DELETE `/api/client-addresses` | Yes | OK | **Keep** |
| GET/POST/PUT/DELETE `/api/customer-service-requests` | Yes | OK | **Keep** |
| GET `/api/service-categories` | Yes | OK | **Keep** |
| GET `/api/provider-profiles` | No | OK | **Redundant** — document or merge |
| GET `/api/provider-profiles/{userUid}` | No | OK | **Redundant** — document or merge |
| GET `/api/providers/{id}/service-requests` | No | **Broken** | **Fix or remove** |

---

## 10. Live schema tables (for context)

**In use (core):** `UsersLogin`, `Clients`, `ServiceCategories`, `Providers`, `Staff`, `ClientAddresses`, `CustomerServiceRequests`

**Removed from live DB (legacy code still references):** `ServiceProviders`, `ProviderProfiles`, `Users`, `Customers`, `ServiceRequests`, `ProviderAvailability`, `ProviderLocations`, `ProviderDocuments`, `ProviderQuotes`, `Bookings`, `BookingTracking`, `Payments`, `Reviews`, `AspNet*` (identity tables may exist separately on server but not in app auth flow)

---

*Generated from static code and schema analysis against `db.txt` v1.9 and `api.txt` v1.5. Re-run this audit after major API or database changes.*
