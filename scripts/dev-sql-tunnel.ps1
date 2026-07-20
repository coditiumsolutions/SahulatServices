# Interactive foreground SQL tunnel (keeps this window open).
# DISABLED: Hostinger VPS (93.127.199.220) is no longer used. Keep for reference only.
# Usage: .\scripts\dev-sql-tunnel.ps1

Write-Host "SQL tunnel is disabled (Hostinger VPS no longer in use). Exiting."
exit 0

# $sshKey = "D:\.ssh\hostinger_vps"
# $vpsHost = "root@93.127.199.220"
# $localPort = 11433
# $remotePort = 1433
#
# Write-Host "Starting SQL tunnel: localhost:${localPort} -> ${vpsHost}:${remotePort}"
# Write-Host "Leave this running. In another terminal: dotnet run --project HomeServicesPortal"
# Write-Host ""
#
# ssh -i $sshKey -o ExitOnForwardFailure=yes -o ServerAliveInterval=30 -o ServerAliveCountMax=3 -o TCPKeepAlive=yes -N -L "${localPort}:127.0.0.1:${remotePort}" $vpsHost
