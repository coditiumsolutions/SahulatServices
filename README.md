# Sahulat Ghar Tak — Web App & API

Backend and admin portal for **Sahulat Ghar Tak** ("Convenience to your doorstep"), a home-services marketplace that connects customers with vetted service providers. This ASP.NET Core application powers two things at once:

- **REST APIs** consumed by the mobile and web clients (`/api/*`, documented via Swagger).
- **Admin/staff portal** — server-rendered MVC views for managing providers, bookings, payments, categories, and documents.

## Tech stack

- **.NET 8** (ASP.NET Core MVC + Web API), C# with nullable reference types and implicit usings
- **Entity Framework Core 8** targeting **SQL Server** (SQLite provider is also referenced for local scenarios)
- **AutoMapper** for DTO ↔ entity mapping
- **JWT Bearer** auth for the APIs and **cookie auth** for the admin portal
- **BCrypt.Net** for password hashing
- **Swashbuckle / Swagger** for API documentation
- **QuestPDF** and **ClosedXML** for PDF/Excel exports, **SixLabors.ImageSharp** for image processing

## Project layout

```
Web App/
├── HomeServicesPortal/        # The ASP.NET Core project
│   ├── Controllers/           # MVC controllers (admin portal)
│   │   └── Api/               # REST API controllers (mobile/web clients)
│   ├── Services/              # Business logic (interface + implementation pairs)
│   ├── Repositories/          # EF Core repositories (generic + specialized)
│   ├── Entities/              # Domain entities (Client, Provider, ServiceBooking, ...)
│   ├── Data/                  # DbContexts: SahulatAppDbContext + AppDbContext
│   ├── DTOs/ · Models/        # Request/response contracts
│   ├── Mappings/              # AutoMapper profiles
│   ├── Middleware/            # Exception handling, etc.
│   ├── Options/               # Strongly-typed config (Otp, FileStorage)
│   ├── Views/                 # Razor views for the admin portal
│   ├── wwwroot/               # Static assets + uploaded provider documents
│   └── Program.cs             # Composition root, DI, auth, pipeline
├── deploy/                    # Deployment scripts (gcp/, hostinger/)
├── scripts/                   # Dev helpers + SQL migration/seed scripts
├── docs/                      # api-audit-report.md
├── api.txt · db.txt          # Live API and DB schema references
└── SahulatGharTak.sln
```

> **Note on the data layer:** the project currently runs two `DbContext`s — `SahulatAppDbContext` (live schema) and `AppDbContext` — reflecting an in-progress consolidation. See `docs/api-audit-report.md` for details.

## Getting started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Access to a SQL Server instance (the app connects directly to the configured server)

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

### Run

The `https` launch profile (`HomeServicesPortal/Properties/launchSettings.json`) binds
`https://localhost:7265` — the same local base URL documented in `api.txt` — so always pass
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

```bash
powershell.exe -NoProfile -Command "Set-Location -LiteralPath 'D:\Ry Work [D]\Bahria Town\SahulatGharTak App\Web App'; dotnet run --project HomeServicesPortal\HomeServicesPortal.csproj --launch-profile https"
```

On a Linux box with the .NET 8 SDK actually installed, the plain command works as-is:

```bash
dotnet run --project HomeServicesPortal/HomeServicesPortal.csproj --launch-profile https
```

Once running (at `https://localhost:7265`):

- **Admin portal:** `/adminportal` (login)
- **Swagger UI:** `/swagger`
- **APIs:** `/api/...` (e.g. `api/service-categories`, `api/service-bookings`, `api/customer-service-requests`, `api/providers-detail`)

## Authentication

- **APIs** use JWT Bearer tokens. Unauthenticated/forbidden `/api` requests return a JSON `ApiResponse` (`401`/`403`) rather than a redirect.
- **Admin portal** uses cookie authentication with an 8-hour sliding expiration; the login path is `/adminportal`.

## Deployment

Deployment is automated via GitHub Actions (`.github/workflows/deploy.yml`): on push to `main`, the app is built with `dotnet publish -c Release` and rsync'd to the GCP VM, then the `sahulatghartak` systemd service is restarted. Production `appsettings*.json` files are preserved on the server (excluded from the sync).

Manual/alternative deployment scripts live under `deploy/gcp/` and `deploy/hostinger/`.

## Reference docs

- `api.txt` — active mobile/client API reference
- `db.txt` — live database schema
- `docs/api-audit-report.md` — audit of redundant/non-functional endpoints and the two-data-layer situation

---

## Credits

**Developed By:**

- Rayder-23
- Coditium Solutions
