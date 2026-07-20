# Starts the SSH SQL tunnel in the background if localhost:11433 is not already listening.
# DISABLED: Hostinger VPS (93.127.199.220) is no longer used. Keep for reference only.
# Safe to run repeatedly (Visual Studio F5, dotnet run, dev-run.ps1).
# Usage: .\scripts\ensure-sql-tunnel.ps1 [-ForceRestart]

param(
    [int]$LocalPort = 11433,
    [int]$RemotePort = 1433,
    [string]$SshKey = "D:\.ssh\hostinger_vps",
    [string]$VpsHost = "root@93.127.199.220",
    [int]$WaitSeconds = 25,
    [switch]$ForceRestart
)

# Tunnel disabled — app connects directly to SQL Server.
Write-Host "SQL tunnel is disabled (Hostinger VPS no longer in use). Exiting."
exit 0

function Test-PortListening {
    param([int]$Port)

    $listener = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
    if ($listener) { return $true }

    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $client.Connect("127.0.0.1", $Port)
        return $true
    }
    catch {
        return $false
    }
    finally {
        $client.Dispose()
    }
}

function Stop-StaleSqlTunnel {
    param([int]$Port)

    Get-CimInstance Win32_Process -Filter "Name='ssh.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -match "${Port}:127\.0\.0\.1:${RemotePort}" -or $_.CommandLine -match "-L\s+${Port}:" } |
        ForEach-Object {
            Write-Host "Stopping stale SSH tunnel (pid $($_.ProcessId))..."
            Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
        }

    Start-Sleep -Seconds 1
}

if ($ForceRestart -and (Test-PortListening -Port $LocalPort)) {
    Stop-StaleSqlTunnel -Port $LocalPort
}
elseif (-not $ForceRestart -and (Test-PortListening -Port $LocalPort)) {
    Write-Host "SQL tunnel already listening on port $LocalPort."
    exit 0
}

if (-not (Test-Path -LiteralPath $SshKey)) {
    Write-Error "SSH key not found: $SshKey"
    exit 1
}

Write-Host "Starting SQL tunnel: localhost:${LocalPort} -> ${VpsHost}:${RemotePort}"

$sshArgs = @(
    "-i", $SshKey,
    "-o", "ExitOnForwardFailure=yes",
    "-o", "StrictHostKeyChecking=accept-new",
    "-o", "ServerAliveInterval=30",
    "-o", "ServerAliveCountMax=3",
    "-o", "TCPKeepAlive=yes",
    "-N",
    "-L", "${LocalPort}:127.0.0.1:${RemotePort}",
    $VpsHost
)

Start-Process -FilePath "ssh" -ArgumentList $sshArgs -WindowStyle Hidden

$deadline = (Get-Date).AddSeconds($WaitSeconds)
while ((Get-Date) -lt $deadline) {
    if (Test-PortListening -Port $LocalPort) {
        Write-Host "SQL tunnel ready on localhost:${LocalPort}."
        exit 0
    }
    Start-Sleep -Milliseconds 500
}

Write-Error "SQL tunnel failed to start within ${WaitSeconds}s. Check SSH key and VPS connectivity."
exit 1
