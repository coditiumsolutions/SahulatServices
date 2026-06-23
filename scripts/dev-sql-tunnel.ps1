# Forwards local port 11433 -> VPS SQL Server (1433) so local dev can reach SahulatAppDB.
# Keep this window open while running the app (F5 / dotnet run).
# Usage: .\scripts\dev-sql-tunnel.ps1

$sshKey = "D:\.ssh\hostinger_vps"
$vpsHost = "root@93.127.199.220"
$localPort = 11433
$remotePort = 1433

Write-Host "Starting SQL tunnel: localhost:${localPort} -> ${vpsHost}:${remotePort}"
Write-Host "Leave this running. In another terminal: dotnet run --project HomeServicesPortal"
Write-Host ""

ssh -i $sshKey -o ExitOnForwardFailure=yes -N -L "${localPort}:127.0.0.1:${remotePort}" $vpsHost
