# Security evaluation — Sahulat Ghar Tak backend

**Date:** 2026-11-03
**Scope:** `HomeServicesPortal` (ASP.NET Core 8) on default branch `main`, product site sahulatghartak.com
**Method:** Source review only. The live database and the server's `appsettings*.json` were not opened. No exploit traffic was sent to production.
**Code changes in this review:** the committed production JWT passphrase was replaced with a placeholder, and a mobile number plus password were removed from `scripts/test-apis.ps1`. Application behavior on the server is unchanged: deploy rsync excludes `appsettings.Production.json` and `appsettings.json` (`.github/workflows/deploy.yml`).

An older write-up, `docs/audits/auth-gap-report.md` (2026-08-27, re-checked in that file on 2026-09-29), already recorded plaintext passwords and anonymous APIs. Both are still true in code. This review did not re-count production password rows, so the older "57 accounts" figure is not repeated as a new measurement.

## Overall risk

**Critical.** Anyone on the internet can call the mobile API as any client, provider, or new staff admin. The API takes the user id from the request, not from a session. Passwords are stored as typed. One-time passwords are not sent by SMS; the only SMS implementation logs the code, and the committed production config also puts the code in the HTTP response. Identity-document images are written under `wwwroot` at predictable paths.

The admin and public websites are in better shape: cookie login, role attributes, antiforgery on the posts that were checked, and portal actions that take the user id from the signed-in cookie. Those controls do not cover `/api/*`.

## Prioritized fix order

1. Turn off anonymous staff registration and review the `Staff` table for accounts you did not create.
2. Require authentication on every API route that reads or changes a person, booking, payment, document, address, device token, or location. Take the caller id from the token, not the body.
3. Stop serving CNIC and police-verification images from `wwwroot`.
4. On the **server** copy of production config (not overwritten by deploy): set `Otp:IncludeInResponse` to false, stop logging OTP values, and send OTP through a real SMS gateway. Rate-limit login, OTP, reset, and delete.
5. Hash passwords with the BCrypt already referenced by `PasswordHasher.Verify`, and force a reset. Existing plaintext values cannot be hashed in place without the original password.
6. Rotate the JWT signing key that was committed (git history still has it; the server file may still have it). Do not switch the API to JWT until step 2 is done — a token with a caller-supplied id would not fix the missing ownership checks.
7. Stop giving every staff login the Admin, Dispatcher, and Customer Support roles at once.
8. Force the auth cookie `Secure` flag and honor `X-Forwarded-Proto` behind nginx.
9. Publish Swagger only in Development.

---

## Critical

### C1. Anyone can register a staff admin

**What.** `POST /api/auth/register-staff` is anonymous and stores `IsAdmin` from the request body. Staff accounts are created already verified and active.

**Where.** `HomeServicesPortal/Controllers/AuthController.cs` (class is `[AllowAnonymous]`; `RegisterStaff`). `HomeServicesPortal/Services/AuthService.cs` `RegisterStaffAsync` sets `UserType = Staff`, `IsVerified = true`, `IsActive = true`, and `IsAdmin = request.IsAdmin`. `HomeServicesPortal/DTOs/RegisterStaffRequest.cs` exposes `IsAdmin`. `HomeServicesPortal/Controllers/AccountController.cs` maps `IsAdmin` to the role `Super Admin`, otherwise `Admin`, and that login is what opens `/Admin`.

**Why it matters.** A new staff row is an admin portal account. `IsAdmin: true` becomes Super Admin, which is the role on APK upload (`ApkManagementController`).

**Remediation.** Remove this action from the public API, or require an existing Super Admin. Audit `UsersLogin` / `Staff` for rows you did not create and disable them. Keep staff creation on `AdministrationController`, which is already role-limited.

### C2. Mobile API authorizes by caller-supplied ids

**What.** Almost every controller under `Controllers/Api/` is `[AllowAnonymous]`. There is no fallback authorization policy in `Program.cs`. `POST /api/auth/login` checks the password and returns `UserId`, `ProfileId`, `UserType`, name, and mobile. It never calls `JwtTokenService.CreateToken` (the only caller of `CreateToken` is the method itself). Later calls send `clientUid`, `providerUid`, or `userId` in the query or body, and the server trusts them.

**Where.**

- Bookings, including accept/reject, start, delete, and completion: `Controllers/Api/ServiceBookingsApiController.cs`. Completion goes to `BookingService.VerifyCompletionPasscodeAsync`, which checks the booking's provider id and passcode, then sets `CustomerPaid`, `FinalAmount`, commission, and provider earning from the request. The passcode is not a secret: `CustomerServiceRequestService` copies `ServiceBookings.Passcode` into `CustomerServiceRequestApiDto.Passcode`, and `GET /api/customer-service-requests` is anonymous.
- Client and provider profiles, including CNIC and mobile: `ClientsDetailApiController.cs`, `ProvidersDetailApiController.cs`, DTOs `ClientDetailApiDto` and `ProviderDetailApiDto`.
- Home addresses with coordinates: `ClientAddressesApiController.cs`, `ClientAddressApiDto`.
- Wallet balance and ledger: `ProviderWalletApiController.cs`.
- Inbox and FCM device tokens: `NotificationsApiController.cs` (the file's own comment says a person who knows a user id can register a device and read that inbox).
- Provider online/offline: `PAvailableStatusApiController.cs` (`api/provider-avability-status`).
- Category and service-title assignment: `ProviderCategoriesApiController.cs`, `ProviderServiceTitlesApiController.cs`.
- Booking list fields also include provider CNIC, both mobile numbers, and the client's full address and pin: `ServiceBookingApiDto`.

**Why it matters.** This is the customer and provider data, the job workflow, and the cash figures recorded at completion. The public website does not have this hole: `PublicDashboardController` and `ProviderBookingsController` / `ProviderRequestsController` resolve the id from the cookie and check ownership.

**Remediation.** Add a fallback policy so `/api` requires a bearer token except a short public list (service catalog, cities, app version config). Issue the token from login. On each action, compare the route or body id with the token's user, and ignore a mismatched id. Keep the passcode off anonymous responses. Keep completion amounts inside server-side rules rather than accepting an arbitrary collected amount from an unauthenticated caller.

### C3. Identity documents are unauthenticated, and the files are public

**What.** Provider document upload, download metadata, delete, and verification are anonymous. Verification sets `Providers.IsVerified` from the body and records `VerifiedBy` from the body. Files are stored under `wwwroot` as `/{providerUid}/cnic_front.jpg`, `cnic_back.jpg`, `police_verification.jpg`, and `profile.jpg`. `UseStaticFiles` serves `wwwroot` with no auth check.

**Where.** `Controllers/Api/ProviderDocumentsApiController.cs`. `Services/ProviderDocumentsApiService.cs` `VerifyDocumentsAsync` and `SetProviderIsVerifiedAsync`. `Services/FileStorageService.cs` (`FixedFileNames`, `wwwroot/uploads/providers`). `appsettings.Production.json` `FileStorage:ProviderDocuments`. `Program.cs` `UseStaticFiles`. `RequireVerifiedProviderFilter` treats `Providers.IsVerified` as the gate for `/provider/*`.

**Why it matters.** CNIC images and police checks are identity documents. Sequential provider ids make the file URLs predictable. Anonymous verify/unverify flips the provider portal gate and the nearby-provider filter.

**Remediation.** Authorize upload and read to the owning provider, and verify/delete to staff. Store files outside `wwwroot` and stream them through an authorized action. The image pipeline itself is sound: `ImageService` decodes JPEG/PNG with Skia, rejects other formats, and caps pixel count; `FileStorageService` keeps a path prefix check and a fixed file name. Keep that, and do not trust the client `Content-Type` alone (it already re-encodes).

### C4. OTP is not delivered by SMS, and production config returns it

**What.** `ISmsService` is always `DummySmsService`, which writes the mobile number and the code to the log and to stdout. `OtpService.BuildSendResponse` sets `OTP` when `Otp:IncludeInResponse` is true. That flag is true in `appsettings.Production.json`, `appsettings.example.json`, and `appsettings.Development.example.json`. Nothing in `OtpService` checks the hosting environment. `AuthController`'s comment says production never returns the code; the code does not do that. `ResetPasswordAsync` accepts any verified account, including staff, and sets the password after the OTP matches. `send-otp` replaces a pending code and sets `AttemptCount` back to 0 with no send cap. Only `resend-otp` enforces `MaxResendCount` (5). Verify allows 3 tries per code.

**Where.** `Program.cs` (`AddScoped<ISmsService, DummySmsService>`). `Services/DummySmsService.cs`. `Services/OtpService.cs` (`BuildSendResponse`, `SaveOtpAsync`). `Options/OtpOptions.cs`. `appsettings.Production.json` `Otp:IncludeInResponse`. `AuthService.ResetPasswordAsync`.

**Why it matters.** Password reset, and therefore portal login, depends on this code. If the server file still has `IncludeInResponse: true` (deploy never overwrites it), the code comes back to the caller. If that flag is already false on the server, the code still lands in logs because no real SMS sender exists. Confirm the mobile app is not reading `data.otp` before turning the flag off, and ship SMS in the same change.

**Remediation.** Register a real SMS sender in non-Development. Set `IncludeInResponse` false on the server file. Log the mobile and type only, never the code. Rate-limit send, resend, verify, and reset by mobile and by IP. Apply the resend cap to `send-otp` as well, and do not reset a lockout by calling send again. Keep password-reset OTPs from applying to `UserType = Staff`, or require a staff-only reset path.

### C5. Passwords are stored in plaintext

**What.** `PasswordHasher.Hash` returns the password unchanged. `Verify` uses BCrypt only when the stored value starts with `$2`; otherwise it is an ordinal string compare. Registration, reset, staff create, web signup, and admin user create all call `Hash`.

**Where.** `HomeServicesPortal/Helpers/PasswordHasher.cs`. Call sites include `AuthService`, `PublicAccountController` (signup), `CustomerService`, `ServiceProviderService`, and `UserService`. BCrypt.Net-Next 4.0.3 is already in the project file.

**Why it matters.** A database backup, a copied `UsersLogin` export, or any future query bug discloses the password the person typed. The same value is the admin portal password.

**Remediation.** Implement `Hash` with BCrypt. On successful `Verify` of a non-`$2` value, replace the row with a hash (or force reset). Do this together with C4 so a reset cannot be taken over while you migrate. Minimum length is 6 (`RegisterClientRequest`, `RegisterStaffRequest`, `PublicSignupViewModel`); raise it when you touch hashing.

---

## High

### H1. A production JWT signing key was committed

**What.** `appsettings.Production.json` contained a short human-readable `Jwt:Key` whose text told operators to change it in production. Issuer and audience are the fixed strings `SahulatGharTak` and `SahulatGharTakClients`. Lifetime defaults to 480 minutes. This change replaced the committed value with `REPLACE_WITH_A_SERVER_ONLY_SECRET_AT_LEAST_32_CHARACTERS`. Git history still has the old value. The running server keeps its own file.

**Where.** `HomeServicesPortal/appsettings.Production.json`. `Program.cs` reads `Jwt:Key` into `SymmetricSecurityKey` for the JWT bearer handler. `JwtTokenService` would sign with the same key. `CreateToken` is unused, and the default authenticate scheme is the cookie (`Program.cs`), so a forged bearer token is not what `[Authorize]` checks today.

**Why it matters.** The key is a shared HMAC secret. The moment any route authenticates with the JWT scheme, anyone who saw the old key can mint `UserId`, `UserType`, and `Role` claims. Rotating only the repo file does not rotate the server.

**Remediation.** Generate a random 32-byte key, put it only on the server (or a secret store), and restart the service. Treat the old key as public. Keep JWT as an explicit scheme for `/api` and `/hubs/location` after C2. The `access_token` query reader in `Program.cs` is limited to `/hubs/location`; prefer a header or a short-lived SignalR negotiation token so the JWT is not written into access logs.

### H2. Web signup marks the phone verified with no OTP

**What.** `POST /signup` creates the user with `IsVerified = true` and stores the password through `PasswordHasher.Hash`. The comment says web signup has no OTP step. Login then allows that account.

**Where.** `PublicAccountController.Signup`, around the `IsVerified = true` assignment.

**Why it matters.** The first person to submit a mobile number owns that login. The real subscriber cannot register it afterwards (`Mobile number already registered`).

**Remediation.** Create web accounts unverified and run them through the same OTP path as the API, after C4 is fixed. Until then, treat web-created accounts as unproven phone ownership.

### H3. Staff role checks do not separate duties

**What.** Every successful staff login gets either `Super Admin` or `Admin`, and also `Dispatcher` and `Customer Support`. `PortalRoleConstants` has a distinct `Staff` role, but `AccountController` never issues it. Non-admin staff still receive the role `Admin`.

**Where.** `AccountController.Login` claim list. Compare with `[Authorize(Roles = "Super Admin,Admin")]` on `AdministrationController` and `PushBroadcastController`, and `"Super Admin,Admin,Dispatcher,Customer Support"` on payments, customers, bookings, and reports. Only `ApkManagementController` is Super Admin alone.

**Why it matters.** A staff account that was meant to be support can manage users, passwords, payments, and push broadcasts.

**Remediation.** Issue one role from `Staff` (or from a real role column). Stop appending Dispatcher and Customer Support to every login. Re-check each `[Authorize(Roles = ...)]` against that single role.

### H4. Provider live location is exposed

**What.** `GET /api/provider-locations/nearby` is anonymous and returns latitude and longitude. Radius is allowed up to 500 km and `OnlyVerified` / `OnlyAvailable` can be set false by the caller (`NearbyProvidersRequestDto`). `LocationTrackingHub.JoinBookingGroup` adds the connection to `booking-{id}` with no check that the caller is the client or provider on that booking. The hub class is `[Authorize]`, so this is any signed-in cookie (client, provider, or staff), not the anonymous internet. `PushLocation` does check that the caller owns the booking.

**Where.** `Controllers/Api/ProviderLocationsApiController.cs`. `Hubs/LocationTrackingHub.cs` `JoinBookingGroup`. `Services/ProviderLocationQueryService.cs`.

**Why it matters.** Nearby is a live map of workers. The hub group receives position updates for a job, which also implies the client's job location.

**Remediation.** Return distance and a coarse area on the public nearby call, or require a client token and a tight radius. In `JoinBookingGroup`, allow only the booking's client, its provider, or staff.

### H5. Auth cookies are not forced onto HTTPS

**What.** Cookie authentication is added in `Program.cs` with an 8-hour sliding expiration and no `Cookie.SecurePolicy`, `HttpOnly`, or `SameSite` override. Framework defaults are HttpOnly and SameSite=Lax, and Secure only when the request ASP.NET sees is HTTPS (`CookieSecurePolicy.SameAsRequest`). `UseForwardedHeaders` is not registered. `docs/seo/SEO-Report.md` records that canonical URLs rendered as `http://` behind nginx, which means the app has been seeing HTTP from the proxy. `UseHttpsRedirection` and `UseHsts` (non-Development) are present.

**Where.** `Program.cs` cookie options and pipeline. Public "remember me" sets `ExpiresUtc` to 30 days (`PublicAccountController.SignInPublicUserAsync`). Admin login sets 8 hours (`AccountController`). A separate 90-day cookie in `PublicDashboardController.MarkBouncedBack` sets `Secure = Request.IsHttps`, so it has the same proxy problem.

**Why it matters.** A session cookie without `Secure` is sent on any later HTTP request to the host. Admin and customer sessions share this cookie scheme.

**Remediation.** `Cookie.SecurePolicy = CookieSecurePolicy.Always` in production. Add forwarded-headers support limited to the nginx hop so `Request.IsHttps` is true. Shorten the 30-day public remember-me ticket.

---

## Medium

### M1. Login and OTP have no meaningful throttle

**What.** The only rate limiter is the fixed window `delete-account` (5 per minute), and it is applied only to the MVC `POST /delete-account` (`HomeController`). `POST /api/auth/delete-account`, login, staff login, send-otp, and reset are not limited. Staff and public login have no failure counter.

**Where.** `Program.cs` `AddRateLimiter`. `HomeController.DeleteAccount`. `AuthController` has no `EnableRateLimiting`.

**Why it matters.** With plaintext passwords and OTP in the response this is secondary. After those are fixed, online guessing and OTP spam are the next path. Delete-account on the API still only needs mobile and password (`AuthService.DeleteAccountAsync`).

**Remediation.** Limit login, OTP, reset, and both delete-account routes by IP and by mobile. Lock or delay after repeated failures. Do not reveal whether the mobile exists once you can do so without breaking the app's current error text (see M3).

### M2. Swagger is on in every environment

**What.** `UseSwagger` and `UseSwaggerUI` run outside the `IsDevelopment` branch. The document includes the anonymous write APIs.

**Where.** `Program.cs` around the Swagger registration and middleware.

**Remediation.** Map Swagger only when `IsDevelopment()`, or protect it with the staff policy.

### M3. Auth errors distinguish accounts

**What.** Login returns different messages for a bad password, an inactive account, and an unverified mobile (`AuthService.LoginAsync`). Password reset send returns "Account not found" when the mobile is not a verified user (`OtpService.SendOtpAsync`). Registration returns "Mobile number already registered."

**Remediation.** Use one message for unknown user and bad password. Keep detailed reasons in server logs.

### M4. Some exception text is returned to API clients

**What.** `ExceptionHandlingMiddleware` returns `ArgumentException.Message` and `InvalidOperationException.Message` as the API error body. Other exceptions become "An unexpected error occurred." Non-development also has `UseExceptionHandler`, which returns a generic API message. The middleware runs for `/api` and can still surface those two exception types.

**Where.** `Middleware/ExceptionHandlingMiddleware.cs`.

**Remediation.** Log the exception, return a fixed message, and keep a correlation id.

### M5. OTP values sit in the database in the clear

**What.** `UserOTP.OTPCode` is the six-digit code. Expiry is 5 minutes (`OtpService.OtpExpiryMinutes`). Generation uses `RandomNumberGenerator.GetInt32`, which is appropriate.

**Where.** `Entities/UserOTP.cs`. `OtpService.GenerateSixDigitOtp`.

**Remediation.** Store a hash of the code. Compare with a fixed-time check. This is minor next to C4.

### M6. A test script contained a mobile number and a password

**What.** `scripts/test-apis.ps1` fell back to a specific mobile number and sent a short password to `POST /api/auth/login` when it could not read a client from the local database. `api.txt` used that same mobile number, those passwords, and `staff123` as sample bodies across register, login, and response examples. The script now runs the login call only when `SAHULAT_TEST_PASSWORD` is set (mobile from the DB, or `SAHULAT_TEST_MOBILE`). The samples in `api.txt` were replaced with obvious placeholders (`03001234567`, `ExamplePass1`, `ExamplePass12`, `ExampleStaff1`).

**Why it matters.** If those passwords were real accounts, they remain in git history. `api.txt` is the mobile contract, so the values were easy to reuse.

**Remediation.** If either password was ever stored on a real row, reset that account. Keep samples obviously fake. The Flutter copy of `api.txt` (see `.cursor/rules/external-api-docs.mdc`) was not in this repo and still needs the same placeholder swap.

---

## Low

### L1. Admin maps test writes geocoder text into HTML

**What.** `Views/MapsTest/_MapsTestScripts.cshtml` assigns `displayName` from the geocoder into `innerHTML`. The page is staff-only (`MapsTestController`). Nominatim display names are not treated as HTML.

**Remediation.** Use `textContent` or encode the string. The admin notification drawer does encode (`_AdminLayout.cshtml` `escapeHtml`) before `innerHTML`. Provider request refresh assigns same-origin Razor HTML to `innerHTML` (`ProviderRequests.cshtml`); that is safe as long as the partial stays encoded by Razor.

### L2. Example SQL settings disable encryption

**What.** `appsettings.example.json` and `appsettings.Development.example.json` use `Encrypt=False` and `TrustServerCertificate=True` with placeholder credentials. Tracked `appsettings.json` / `appsettings.Development.json` are gitignored. No live connection string was found in tracked files.

**Remediation.** Examples should show `Encrypt=True` and a trusted certificate. Confirm the server connection string is not still `Encrypt=False`.

### L3. Old SQL host is written in a comment

**What.** `Program.cs` comments mention a former SSH tunnel target `root@93.127.199.220:1433`. `DevSqlTunnelBootstrap` is not registered (the `Add` call is commented out). It starts a fixed PowerShell script, not a request-controlled command.

**Remediation.** Drop the address from the comment if that host is still yours.

### L4. Packages

**What.** `HomeServicesPortal.csproj` pins ASP.NET/EF packages in the 8.0.26–8.0.28 range, `System.IdentityModel.Tokens.Jwt` 7.1.2, and `Microsoft.IdentityModel.Tokens` 7.1.2. 7.1.2 is the patched release for CVE-2024-21319 (JWE decompression). The package is marked deprecated upstream; current maintained releases are 8.x. Vendored jQuery is 3.6.0 (`wwwroot/lib/jquery`). Published core CVEs for jQuery XSS stop before 3.5.0; this review did not find a core CVE that applies to 3.6.0. `dotnet list package --vulnerable` could not be run here (no .NET SDK in the review environment).

**Remediation.** When JWT auth is turned on, move IdentityModel to a current maintained major. Bump jQuery on the next front-end pass. Re-run `dotnet list package --vulnerable` on a machine with the SDK.

---

## Informational

- **IndexNow key** in `IndexNowService.IndexNowDefaults` is public on purpose (served as `/{key}.txt`). Not a credential.
- **Firebase** is a file path (`Firebase:ServiceAccountPath`), including `/etc/sahulatghartak/firebase-service-account.json` in the production file. The JSON key is gitignored (`*firebase-adminsdk*.json`, `HomeServicesPortal/secrets/`). Missing credentials disable push rather than crashing startup.
- **No raw SQL** (`FromSqlRaw`, `ExecuteSql`, string-built queries) was found. Queries go through EF Core LINQ.
- **No unsafe deserialization** (`BinaryFormatter`, `TypeNameHandling`, Newtonsoft settings) was found.
- **CORS** is not configured, so browsers on other origins cannot call the API. Mobile apps and any non-browser client are not limited by that.
- **Open redirects** on admin and public login use `Url.IsLocalUrl`. Public login also refuses return URLs under `/Admin`, `/adminportal`, and `/Account`.
- **Role switch** (`POST /switch-role`) loads the client or provider row for the signed-in user id. It does not accept an id from the form.
- **APK files:** `ApkManagementService` uses `Path.GetFileName` and a root prefix check. Upload and delete are Super Admin only. Public download of the published APK from `wwwroot/downloads` is expected for the store page.
- **Antiforgery** is present on the MVC posts reviewed (admin CRUD, public login/signup, provider and client actions, delete-account page). API controllers do not use cookies for authorization today, so cookie CSRF is not the API's problem; C2 is.
- **Privacy policy** (`docs/legal/PRIVACY_POLICY.md`) says passwords are not stored in plain text. That does not match `PasswordHasher`.

## What was not in the repo

- No card-processor SDK or payment-gateway secret. "Payments" are an internal ledger (`PaymentService`, admin `PaymentsController`) plus the booking completion amounts in C2.
- No production connection string, Firebase private key, or `.env` in git. `.env` is gitignored.
- This review did not log into sahulatghartak.com or the admin portal, and did not confirm the server's live `Otp:IncludeInResponse` or `Jwt:Key`. Those two values on the server are the ones that decide whether C4 and H1 are already true in production or only true of the committed template.
