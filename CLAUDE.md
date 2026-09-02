# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

Sahulat Ghar Tak ("Convenience to your doorstep") — ASP.NET Core 8 backend for a home-services marketplace. A single app serves two audiences at once:

- **REST APIs** (`/api/*`) consumed by mobile/web clients, documented via Swagger at `/swagger`.
- **Admin/staff portal** — server-rendered MVC views (login at `/adminportal`) for managing providers, bookings, payments, categories, documents.

## Commands

```bash
# Run the app (from repo root) — ensures SQL tunnel first if needed
./scripts/dev-run.ps1

# or directly
dotnet run --project HomeServicesPortal/HomeServicesPortal.csproj

# Build
dotnet build HomeServicesPortal/HomeServicesPortal.csproj

# EF Core migrations (target the relevant DbContext explicitly — see below)
dotnet ef migrations add <Name> --context AppDbContext --project HomeServicesPortal
dotnet ef database update --context AppDbContext --project HomeServicesPortal
```

There is no test project in this repo currently — `scripts/test-customer-service-requests-optional-preferred.ps1` is a manual smoke-test script (hits a running instance), not an automated test suite.

### Configuration setup (required before first run)

Config files with secrets are git-ignored; copy the examples and fill in values:

```bash
cd HomeServicesPortal
cp appsettings.example.json appsettings.json
cp appsettings.Development.example.json appsettings.Development.json
```

Minimum required: `ConnectionStrings:DefaultConnection` (SQL Server), `Jwt:Key` (32+ chars, plus `Issuer`/`Audience`), `FileStorage:ProviderDocuments`.

The app connects **directly** to the configured SQL Server — there is no local tunnel needed by default. `scripts/ensure-sql-tunnel.ps1` / `dev-sql-tunnel.ps1` are leftover from a prior Hostinger SSH-tunnel setup (`HomeServicesPortal/Infrastructure/DevSqlTunnelBootstrap.cs` is currently disabled/commented out in `Program.cs`); only touch these if reviving that setup.

## Architecture

### The two-DbContext situation (critical to understand before touching data access)

The app registers **two separate `DbContext`s side by side** in `Program.cs`, both pointed at the same SQL Server connection string:

- **`AppDbContext`** (`Data/AppDbContext.cs`) — the current, live-schema-aligned context. Auth, `Providers`, `ClientAddresses`, `CustomerServiceRequests`. All actively-documented mobile APIs (`api.txt`) use this.
- **`SahulatAppDbContext`** (`Data/SahulatAppDbContext.cs`, split with `SahulatAppDbContext.Auth.cs`) — the original/legacy context backing the admin MVC portal and an older booking workflow. Many of the tables it maps to are marked `[REMOVED]` in `db.txt` (the live schema doc) — querying through it can 500.

This is an **in-progress consolidation, not a stable pattern to imitate**. When adding new features:
- New REST API work → use `AppDbContext` and its entities (`Entities/*.cs` — `Client`, `Provider`, `ServiceBooking`, `CustomerServiceRequest`, etc.), not `Models/Entities/*.cs` (the older `SahulatAppDbContext`-side entities).
- Read `docs/api-audit-report.md` before modifying or extending any existing endpoint — it tracks which endpoints are functional, non-functional, or redundant against the live DB, and which tables are dead.
- `api.txt` (mobile/client API reference) and `db.txt` (live DB schema) are the source of truth for what's actually deployed/working — prefer them over assumptions from reading controller code alone, since some controllers query removed tables.

### Layering

`Controllers` (MVC, admin portal) / `Controllers/Api` (REST, mobile clients) → `Services` (interface + implementation pairs, business logic) → `Repositories` (`IRepository<T>` generic + specialized repos like `IUserRepository`, `IProviderDocumentRepository`) → `Data` (the two DbContexts).

Cross-cutting:
- `Mappings/AuthMappingProfile.cs` — AutoMapper profile (DTO ↔ entity).
- `Middleware/ExceptionHandlingMiddleware.cs` — wraps the pipeline; non-dev unhandled exceptions on `/api/*` return a JSON `ApiResponse` instead of an HTML error page/redirect.
- `Options/` — strongly-typed config (`OtpOptions`, `FileStorageOptions`), bound in `Program.cs`.
- `Helpers/` — `PasswordHasher` (BCrypt), `PortalRoleConstants`, `UserTypeConstants`, `OtpTypeConstants`, `MobileNumberHelper`.

### Auth model (two schemes, split by request path)

- **APIs** (`/api/*`): JWT Bearer. `Program.cs` wires custom `OnChallenge`/`OnForbidden` handlers so failures return JSON `ApiResponse` with proper status codes instead of redirects.
- **Admin portal** (everything else): cookie auth, 8-hour sliding expiration, login path `/adminportal`. Backed by `UsersLogin` + `Staff` — **no ASP.NET Identity tables/migrations** are used.
- Both schemes are registered together; the `OnRedirectToLogin`/`OnRedirectToAccessDenied` cookie events branch on whether the request path starts with `/api` to decide JSON-vs-redirect, so any new auth-adjacent middleware needs to preserve that branching.

### Response conventions

API controllers return `ApiResponse<T>` (`Models/Api/ApiResponse.cs`) wrapping success/failure + message, not bare data or ASP.NET's default `ValidationProblem`. Model-validation failures are intercepted in `Program.cs` (`ApiBehaviorOptions.InvalidModelStateResponseFactory`) and reshaped into the same `ApiResponse` envelope — follow this convention for any new API controller rather than returning raw `BadRequest(ModelState)`.

## Deployment

GitHub Actions (`.github/workflows/deploy.yml`): push to `main` → `dotnet publish -c Release` → rsync to a GCP VM → restart the `sahulatghartak` systemd service. Production `appsettings*.json` are excluded from the rsync (preserved on the server). Manual/alternative scripts live under `deploy/gcp/` and `deploy/hostinger/`.

## WSL environment notes

This repo lives on the Windows filesystem (`D:\Ry Work [D]\...`) and is normally edited/built from Windows tooling (Visual Studio, etc.) with `core.autocrlf=true`. WSL's own git previously didn't see that setting, so WSL-side `git diff`/`git status` used to show every tracked file as modified (a CRLF-vs-LF false alarm). **Fixed** by setting `core.autocrlf=true` globally in WSL's own git config too, so both sides agree on line-ending handling — plain `git status`/`git diff`/`git add`/`git commit` from WSL Bash are safe to use directly now.

WSL still can't run the app or tests directly (only .NET 10 is installed there vs the app's net8.0 target) — for builds, running, or testing the app, use `powershell.exe` from WSL Bash, which reaches the Windows .NET 8 runtime:

```bash
powershell.exe -NoProfile -Command "Set-Location -LiteralPath 'D:\Ry Work [D]\Bahria Town\SahulatGharTak App\Web App'; dotnet build HomeServicesPortal/HomeServicesPortal.csproj"
```

- **Always use `-LiteralPath`, never plain `cd`/`Set-Location <path>`** — the `[D]` in the folder name is treated as a wildcard glob by PowerShell's path resolution and fails to resolve with a bare path argument.
- For commit messages with a body (or any `$`/backtick-containing text) when running git through PowerShell, don't pass it inline via `-Command` from bash — bash's own `$()`/backtick expansion mangles it before PowerShell ever sees it. Write a `.ps1` file under `/mnt/c/Windows/Temp/` (using a single-quoted `@'...'@` here-string for the message) and run it with `-File`, e.g. `powershell.exe -NoProfile -File 'C:\Windows\Temp\commit.ps1'`.
- Windows PowerShell 5.1 is what's installed (no `pwsh.exe`/PS7) — e.g. `Invoke-WebRequest -SkipCertificateCheck` isn't available; use the classic `ICertificatePolicy` bypass for self-signed HTTPS instead.
- A detached long-running process (e.g. `dotnet run` in the background) must use `Start-Process -PassThru -WindowStyle Hidden`, not `Start-Job` — jobs die when the launching `powershell.exe -File` process exits after each Bash tool call.
