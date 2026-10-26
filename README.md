<p align="center">
  <img src="HomeServicesPortal/wwwroot/img/logo-512.png" alt="Sahulat Ghar Tak logo" width="160" />
</p>

# Sahulat Ghar Tak — Web App & API

Backend, website and admin portal for **Sahulat Ghar Tak** ("Convenience to your doorstep"), a home-services marketplace that connects customers with vetted service providers. One ASP.NET Core application serves three audiences at once:

- **REST APIs** (`/api/*`) consumed by the Flutter mobile app, documented via Swagger.
- **Public website and client/provider portal** — server-rendered pages at `/` (marketing), `/login`, `/signup`, `/client/*` and `/provider/*`. Customers and providers can do on the web what they do in the app: request and track services, accept and complete jobs, upload documents, see the wallet and notifications.
- **Admin/staff portal** — server-rendered MVC views (login at `/adminportal`) for managing providers, requests, bookings, payments, categories, documents and configuration.

## Tech stack

- **.NET 8** (ASP.NET Core MVC + Web API), C# with nullable reference types and implicit usings
- **Entity Framework Core 8** targeting **SQL Server** (the SQLite provider is also referenced for local scenarios)
- **AutoMapper** for DTO ↔ entity mapping
- **JWT Bearer** auth for the APIs, **cookie auth** for the admin portal and the public client/provider portal
- **BCrypt.Net** for password hashing
- **Swashbuckle / Swagger** for API documentation
- **QuestPDF** and **ClosedXML** for PDF/Excel exports, **SkiaSharp** for image processing
- **Firebase Admin SDK** for FCM push notifications (optional; disabled when no service account is configured)
- Front end of the public site: Razor views, Bootstrap 5 and Font Awesome from CDNs, plus Cropper.js for the provider document upload

## Project layout

```
Web App/
├── HomeServicesPortal/        # The ASP.NET Core project
│   ├── Controllers/           # MVC controllers: admin portal and the public client/provider portal
│   │   └── Api/               # REST API controllers (mobile app)
│   ├── Services/              # Business logic (interface + implementation pairs)
│   ├── Repositories/          # EF Core repositories (generic + specialized)
│   ├── Entities/              # Domain entities (Client, Provider, ServiceBooking, ...)
│   ├── Data/                  # DbContexts: SahulatAppDbContext + AppDbContext
│   ├── DTOs/ · Models/        # Request/response contracts and view models
│   ├── Mappings/              # AutoMapper profiles
│   ├── Filters/               # Action filters (e.g. the verified-provider gate)
│   ├── Helpers/               # Shared helpers (time, display, constants, password hashing)
│   ├── Middleware/            # Exception handling, per-visitor time format
│   ├── Options/               # Strongly-typed config (Otp, FileStorage)
│   ├── Views/                 # Razor views: admin portal, public site, client and provider portal
│   ├── wwwroot/               # Static assets (css, js, img) + uploaded provider documents
│   └── Program.cs             # Composition root, DI, auth, pipeline
├── tests/                     # HomeServicesPortal.ReleaseTests (fast unit tests) and HomeServicesPortal.Tests
├── deploy/                    # Deployment scripts (gcp/, hostinger/)
├── scripts/                   # Dev helpers + SQL migration/seed scripts (see scripts/README.md)
├── docs/                      # Audits, design notes, legal text, mobile-app and SEO docs (see docs/README.md)
├── api.txt · db.txt           # Live API contract and DB schema references
├── AGENTS.md                  # Guidance for coding agents (CLAUDE.md points to it)
└── SahulatGharTak.sln
```

> **Note on the data layer:** the project currently runs two `DbContext`s — `SahulatAppDbContext` (the original context behind the admin portal) and `AppDbContext` (the live-schema-aligned one used by new work and the mobile APIs) — reflecting an in-progress consolidation. See `AGENTS.md` and `docs/audits/api-audit-report.md` before touching data access.

## Getting started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Access to a SQL Server instance (the app connects directly to the configured server)

> The database is shared with production, so be careful with anything that writes.

### Configuration

Configuration files with secrets are git-ignored. Copy the examples and fill in your values:

```bash
cd HomeServicesPortal
cp appsettings.example.json appsettings.json
cp appsettings.Development.example.json appsettings.Development.json
```

Then set, at minimum:

- `ConnectionStrings:DefaultConnection` — your SQL Server connection string
- `Jwt:Key` — a secure secret **at least 32 characters** long (also `Issuer`, `Audience`)
- `FileStorage:ProviderDocuments` — upload path for provider documents

Optional: `Firebase:ServiceAccountPath` (push notifications), plus the `Otp` and `Notifications` sections already listed in the example file.

### Run

The `https` launch profile (`HomeServicesPortal/Properties/launchSettings.json`) binds
`https://localhost:7265` (and `http://localhost:5212`) — the same local base URL documented in `api.txt` — so always pass
`--launch-profile https` (or the explicit `--urls` below) to keep the app and the API reference in sync.

Note that `dotnet run` cannot take `SahulatGharTak.sln`; point it at the single project the solution contains.

**Windows (PowerShell / Visual Studio terminal), from the repo root:**

```powershell
# from the repo root
.\scripts\dev-run.ps1

# or directly, on the port api.txt documents
dotnet run --project HomeServicesPortal\HomeServicesPortal.csproj --launch-profile https

# forcing the ports explicitly (ignores launch profiles)
dotnet run --project HomeServicesPortal\HomeServicesPortal.csproj --urls "https://localhost:7265;http://localhost:5212"
```

**WSL / Linux terminal:** WSL here only has the .NET 10 SDK, while the app targets `net8.0`, so shell out to
Windows PowerShell rather than running `dotnet` directly. Use `-LiteralPath` — the `[D]` in the repo path is
treated as a wildcard by PowerShell's path resolution and fails with a bare path argument.

On a Linux machine with the .NET 8 SDK actually installed, the plain `dotnet run` command works as-is.

Once running (at `https://localhost:7265`):

| What | Where |
|---|---|
| Public website | `/` |
| Login / sign up (clients and providers) | `/login`, `/signup` |
| Client portal | `/client/dashboard`, `/client/requests`, `/client/profile` |
| Provider portal | `/provider/dashboard`, `/provider/requests`, `/provider/bookings`, `/provider/wallet`, `/provider/profile` |
| Legal pages | `/privacy-policy`, `/terms-and-conditions`, `/delete-account` |
| Admin portal | `/adminportal` (login) |
| Swagger UI | `/swagger` |
| APIs | `/api/...` (e.g. `api/service-categories`, `api/service-bookings`, `api/customer-service-requests`, `api/providers-detail`) |

### Tests

```bash
dotnet test tests/HomeServicesPortal.ReleaseTests -c Release
```

`HomeServicesPortal.Tests` (status-workflow tests) uses a database fixture, so run it only against a database you are happy to write to.

## Authentication

- **APIs** use JWT Bearer tokens. Unauthenticated/forbidden `/api` requests return a JSON `ApiResponse` (`401`/`403`) rather than a redirect.
- **Admin portal** uses cookie authentication with an 8-hour sliding expiration; the login path is `/adminportal`. Staff accounts cannot sign in on the public login.
- **Public client/provider portal** uses a separate cookie sign-in at `/login` (mobile number and password): 8 hours, or about 30 days with "Remember me". The cookie carries the login id, the active role, and both the client and provider profile ids, so an account with both profiles can switch dashboards. Every `/provider/*` page except documents and the pending page requires a verified provider. OTP is not part of the web flow yet.

## Deployment

Deployment is automated via GitHub Actions (`.github/workflows/deploy.yml`): on push to `main`, the app is built with `dotnet publish -c Release` and rsync'd to the GCP VM, then the `sahulatghartak` systemd service is restarted. Production `appsettings*.json` files are preserved on the server (excluded from the sync). Pushes that only change `*.md`, `*.txt`, `docs/`, `scripts/` or `deploy/` do not trigger a deploy.

Manual/alternative deployment scripts live under `deploy/gcp/` and `deploy/hostinger/`.

## Reference docs

- `api.txt` — active mobile/client API reference (the source of truth for the app)
- `db.txt` — live database schema
- `AGENTS.md` — architecture notes and working rules for coding agents
- `docs/README.md` — index of everything under `docs/` with a freshness status for each file
- `docs/mobile-app/web-parity-spec.md`, `web-parity-decisions.md`, `web-parity-audit.md` — how the website mirrors the app, the decisions made, and what is built
- `docs/legal/` — privacy policy and terms and conditions
- `docs/audits/api-audit-report.md` — audit of redundant/non-functional endpoints and the two-data-layer situation (stale; see `docs/README.md`)

---

## Credits

**Developed By:**

- Rayder-23
- Coditium Solutions
