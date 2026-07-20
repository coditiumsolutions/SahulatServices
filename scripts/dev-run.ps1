# Local dev: run the web app.
# Usage: .\scripts\dev-run.ps1
#
# Hostinger SSH SQL tunnel is disabled — SQL is reached directly via ConnectionStrings.
# & "$PSScriptRoot\ensure-sql-tunnel.ps1"
# if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Write-Host ""
Write-Host "Starting HomeServicesPortal..."
dotnet run --project "$root\HomeServicesPortal\HomeServicesPortal.csproj"
