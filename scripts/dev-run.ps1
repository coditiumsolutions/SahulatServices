# Local dev: ensure SQL tunnel, then run the web app.
# Usage: .\scripts\dev-run.ps1

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

& "$PSScriptRoot\ensure-sql-tunnel.ps1"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "Starting HomeServicesPortal..."
dotnet run --project "$root\HomeServicesPortal\HomeServicesPortal.csproj"
