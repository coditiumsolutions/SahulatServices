# Authentication Gap Report

**Scope:** HomeServicesPortal backend (ASP.NET Core 8) and mobile API, production database
**Accounts audited:** 57
**Date:** 2026-08-27

## Summary

The mobile API has no real authentication layer. Every write endpoint accepts anonymous requests and trusts whatever `clientUid` or `providerUid` the caller includes in the request body or query string. Passwords are stored in plaintext across all 57 production accounts. A JWT pipeline exists in the code but is never wired up — `/api/auth/login` validates credentials and returns a plain JSON profile, no token.

This isn't a single bug to patch — it's the absence of a session model. The recommendations below build one in the order requested: hashing and tokens first, secure client-side storage second, then OAuth once the foundation holds weight.

**Findings at a glance:** 2 critical, 2 high, 2 medium, 2 already solid.

---

## Findings

### 1. Passwords are stored in plaintext — Critical

`PasswordHasher.Hash()` is a no-op that returns the password unchanged. `Verify()` only calls BCrypt when the stored value happens to start with `$2` — otherwise it falls back to a raw string comparison. Checked all 57 rows in production `UsersLogin`: zero carry a bcrypt prefix. Every password is sitting in the database as typed.

`HomeServicesPortal/Helpers/PasswordHasher.cs`:
```csharp
public static string Hash(string password) => password;

public static bool Verify(string password, string passwordHash)
{
    if (passwordHash.StartsWith("$2", StringComparison.Ordinal))
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);

    return string.Equals(password, passwordHash, StringComparison.Ordinal);
}
```

**Impact:** Anyone with read access to the database — or a future SQL injection, backup leak, or misconfigured export — gets every user's real password, not a hash to crack. Because people reuse passwords, this is a credential-stuffing risk against other services too, not just this app.

### 2. No authentication on mobile write endpoints — Critical

Every controller under `Controllers/Api/` is marked `[AllowAnonymous]`, with one exception (`PUT /provider-locations/{id}`). `POST /api/customer-service-requests`, `/service-bookings`, `/respond`, and `/verify-completion` all accept a `clientUid` or `providerUid` straight from the request body with no check that the caller is authenticated as that identity.

Confirmed live — `03771062310` has never logged in, has 13 requests:
```
UID  MobileNo      UserType  IsVerified  LastLogin
18   03771062310   Client    0           NULL

-- yet CustomerServiceRequests.ClientUID = 17 has 13 rows,
-- several Completed with real ServiceBookings + PaymentLedger entries
```

**Impact:** This is the root of the earlier "wrong contact info" investigation — not a mis-join in the data layer, but the fact that requests/bookings can be created against any account, verified or not, logged in or not. It's also a straightforward data-integrity and fraud surface: any client can be impersonated by anyone who knows their numeric ID.

### 3. JWT infrastructure exists but is never issued — High

`JwtTokenService.CreateToken()` and the `AddJwtBearer` validation pipeline in `Program.cs` are fully configured — but `AuthController.Login()` never calls `CreateToken()`. `LoginAsync` returns a plain `LoginResponse` (userId, profileId, userType, name, mobile) with no token at all. The bearer scheme has nothing to validate because nothing ever hands out a bearer token.

Confirmed — grep across the codebase for `CreateToken()` invocations: zero.
```csharp
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();  // registered
// ...no controller or service ever calls _jwtTokenService.CreateToken(...)
```

**Impact:** This is good news operationally — the hard part (signing key config, validation parameters, claims shape) is already done correctly. Wiring `Login()` to actually issue and return this token is most of the fix for finding #2.

### 4. Admin portal cookie has no explicit security flags — High

`AddCookie(...)` in `Program.cs` sets `LoginPath`, `SlidingExpiration`, and the 8-hour expiry, but never sets `Cookie.HttpOnly`, `Cookie.SecurePolicy`, or `Cookie.SameSite` explicitly. ASP.NET Core's cookie auth defaults `HttpOnly` to true and `SameSite` to Lax already, but `SecurePolicy` defaults to `SameAsRequest` — meaning if the portal is ever reached over plain HTTP (a misconfigured proxy, a dev/staging box without HSTS), the session cookie will happily go out unencrypted.

**Impact:** Low likelihood given `UseHsts()` is already on in production, but this is exactly the kind of flag that should be asserted in code rather than inherited from a default that could silently change.

### 5. OTP verification is not enforced before write access — Medium

`Otp:IncludeInResponse` is `true` in the production `appsettings.json` — every `send-otp` call returns the actual OTP code in the JSON response, which defeats the purpose of SMS delivery as a separate verification channel. Separately, since write endpoints don't check `IsVerified` at all (see finding #2), OTP verification is currently cosmetic for anything except the login gate itself.

**Impact:** Once auth is real, this becomes low-severity — a config flag for a dev/staging environment. Today, on production, it means OTP provides no actual proof of phone ownership to anyone inspecting API traffic.

### 6. Account-upgrade path leaves orphaned profile rows — Medium

`RegisterProviderAsync` flips `UsersLogin.UserType` from Client to Provider in place but never removes or deactivates the old `Clients` row. Confirmed live: `UserUid 41` ("zayan") has both a `Clients` row and a `Providers` row under one account. Not itself an auth vulnerability, but it's exactly the kind of dangling identity state that becomes a real bug once sessions are real — a stale `clientUid` cached from before an upgrade would need to be handled explicitly.

### 7. Admin portal CSRF protection — Already solid

`[ValidateAntiForgeryToken]` is applied consistently across every mutating admin action (`AccountController`, `AdministrationController`, `BookingsController`, and others). No changes needed here.

### 8. API rate limiting on sensitive actions — Already solid

A fixed-window limiter (5 requests/minute) is already applied to `delete-account`. Worth extending the same pattern to `login`, `send-otp`, and `verify-otp` once those become real attack surfaces under a proper auth model — but the mechanism is already in place and working.

---

## Requirements — current state

| # | Requirement | State | Notes |
|---|---|---|---|
| 1 | JWT | Partially built | Pipeline configured and validated in `Program.cs`; never issued by `Login()`. Wiring this up is the single highest-leverage fix — it turns every `[AllowAnonymous]` mobile endpoint into something that *can* require `[Authorize]` and read the caller's own `clientUid`/`providerUid` from claims instead of trusting the request body. |
| 2 | Password hashing | Not implemented | BCrypt is already a dependency and `Verify()` already knows how to check a bcrypt hash — `Hash()` just never calls it. Needs a hash of every existing plaintext password on next login (or a forced reset) alongside the code fix, since the 57 existing rows are unusable as bcrypt hashes today. |
| 3 | Web: HttpOnly / SameSite / Secure cookies | Partially covered by defaults | Admin portal cookie relies on framework defaults for two of three flags. Needs `Cookie.HttpOnly = true`, `Cookie.SecurePolicy = CookieSecurePolicy.Always`, and `Cookie.SameSite = SameSiteMode.Strict` (or `Lax` if any cross-site POST flows are needed) set explicitly rather than inherited. |
| 4 | Android: EncryptedSharedPreferences / Keystore | Depends on #1 | Backend has nothing to do here directly, but it's the natural home for the JWT once issued — the Flutter app's `flutter_secure_storage` package (already in use per a prior Flutter audit) backs onto EncryptedSharedPreferences on Android and Keychain on iOS automatically, so this mostly falls out of finishing #1. |
| 5 | iOS: Keychain Services | Depends on #1 | Same note as Android — `flutter_secure_storage` already routes to Keychain on iOS. No separate backend work; verify the app stores the JWT (not just the profile fields it stores today) once #1 lands. |

---

## Recommended sequence

### Phase 1 — Close the gap

- **Fix password hashing.** Replace `PasswordHasher.Hash()` with an actual BCrypt call. Existing plaintext rows need a migration path — either force a reset on next login (simplest, safest) or hash them in place during a maintenance window.
- **Issue the JWT on login.** Call `_jwtTokenService.CreateToken(...)` from `AuthController.Login()` and return it in `LoginResponse`. This is largely plumbing — the token pipeline already exists.
- **Require the token on write endpoints.** Replace `[AllowAnonymous]` with `[Authorize]` on the mutating actions in `CustomerServiceRequestsApiController` and `ServiceBookingsApiController`, and derive `clientUid`/`providerUid` from the authenticated claims rather than trusting the request body — this is what actually closes the impersonation gap, not just adding a login screen.
- **Set cookie flags explicitly** on the admin portal's `AddCookie(...)` block.

### Phase 2 — Client-side storage

- **Web:** once a real BFF exists (Phase 4), the browser never sees the JWT directly — it gets an `HttpOnly` session cookie instead. Until then, if the web client talks to the API directly, at minimum avoid `localStorage` for the token.
- **Android / iOS:** confirm the Flutter app moves the JWT into `flutter_secure_storage` alongside (or instead of) the plain profile fields it currently persists — this was already checked as clean for `contactPerson`/`contactNo` in an earlier investigation, but the token itself doesn't exist yet to audit.

### Phase 3 — Once the foundation holds: OAuth 2.0 + PKCE

- Move mobile login to OAuth with PKCE via system browser (Custom Tabs / `ASWebAuthenticationSession`) rather than the current direct-credential POST — this is a bigger lift and depends on Phase 1 being solid first, since PKCE is protecting a token exchange that doesn't meaningfully exist yet.

### Phase 4 — Platform hardening

- **BFF pattern for web:** introduce a backend-for-frontend that owns the OAuth/JWT exchange and hands the browser only an `HttpOnly` session cookie — the browser never holds a bearer token directly.
- **Encrypted offline cache on mobile:** if the app grows an offline cache beyond simple profile fields, move it to SQLCipher rather than a raw SQLite file — not urgent today since no PII caching beyond secure storage was found in the Flutter audit, but worth setting as the default before it becomes necessary.

---

## Evidence reference

| Check | Method | Result |
|---|---|---|
| Password hash format across all accounts | SQL: LEN/prefix scan, `UsersLogin` | 0 / 57 bcrypt |
| JWT issuance on login | Code search: `CreateToken()` call sites | 0 call sites |
| `[AllowAnonymous]` on mobile API controllers | Code search: `Controllers/Api/*.cs` | 13 / 14 controllers |
| Unverified account with real request history | SQL: `UsersLogin` ⋈ `CustomerServiceRequests` | Confirmed, UID 18 |
| Cookie `SecurePolicy` / `SameSite` set explicitly | Code search: `Program.cs` `AddCookie` block | Not set |
| Anti-forgery tokens on admin mutations | Code search: `[ValidateAntiForgeryToken]` | Present, consistent |

---

**Note:** Scope is HomeServicesPortal backend only. Flutter client-side storage was previously audited separately (contact-field handling, session persistence) and found clean — this report does not re-cover that ground except where it depends on backend changes above.
