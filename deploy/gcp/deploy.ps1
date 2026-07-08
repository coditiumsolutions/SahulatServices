param(
    [string]$ConfigPath = "$PSScriptRoot\deploy.config.psd1",
    [switch]$SetupServer
)

$ErrorActionPreference = "Stop"
$config = Import-PowerShellDataFile $ConfigPath

$keyPath = if ([System.IO.Path]::IsPathRooted($config.SshKey)) {
    $config.SshKey
} else {
    Join-Path $env:USERPROFILE ".ssh\$($config.SshKey)"
}

if (-not (Test-Path $keyPath)) {
    throw "SSH key not found: $keyPath"
}

# OpenSSH on Windows requires restrictive ACL on private keys
try {
    icacls $keyPath /inheritance:r | Out-Null
    icacls $keyPath /grant:r "${env:USERNAME}:(R)" | Out-Null
} catch {
    Write-Warning "Could not adjust key ACL (may already be correct): $_"
}

$projectRoot = Resolve-Path "$PSScriptRoot\..\.."
$publishDir = Join-Path $projectRoot "publish-gcp"
$projectFile = Join-Path $projectRoot "HomeServicesPortal\HomeServicesPortal.csproj"
$prodSettings = Join-Path $PSScriptRoot "appsettings.Production.json"

$sshArgs = @(
    "-F", "$env:TEMP\empty_ssh_config",
    "-i", $keyPath,
    "-o", "BatchMode=yes",
    "-o", "StrictHostKeyChecking=accept-new"
)

if (-not (Test-Path "$env:TEMP\empty_ssh_config")) {
    New-Item -ItemType File -Path "$env:TEMP\empty_ssh_config" -Force | Out-Null
}

$remote = "$($config.SshUser)@$($config.SshHost)"
$remotePath = $config.RemotePath
$serviceName = $config.ServiceName
$appPort = $config.AppPort
$domain = $config.Domain

Write-Host "==> Testing SSH to $remote ..." -ForegroundColor Cyan
ssh @sshArgs $remote "echo SSH_OK && whoami"
if ($LASTEXITCODE -ne 0) {
    throw "SSH connection failed. Check key ($keyPath) and user ($($config.SshUser))."
}

if ($SetupServer) {
    Write-Host "==> Running one-time server setup..." -ForegroundColor Cyan
    scp @sshArgs "$PSScriptRoot\setup-server.sh" "${remote}:/tmp/setup-sahulat-api.sh"
    if ($LASTEXITCODE -ne 0) { throw "Failed to upload setup-server.sh" }
    ssh @sshArgs $remote "sed -i 's/\r$//' /tmp/setup-sahulat-api.sh && sudo bash /tmp/setup-sahulat-api.sh"
    if ($LASTEXITCODE -ne 0) { throw "Server setup failed" }
}

Write-Host "==> Publishing Release build..." -ForegroundColor Cyan
if (Test-Path $publishDir) {
    Remove-Item -Recurse -Force $publishDir
}
dotnet publish $projectFile -c Release -o $publishDir --self-contained false
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

Copy-Item $prodSettings (Join-Path $publishDir "appsettings.Production.json") -Force

Write-Host "==> Ensuring remote directory..." -ForegroundColor Cyan
ssh @sshArgs $remote "mkdir -p $remotePath/wwwroot/uploads/providers $remotePath/wwwroot/uploads/documents"

Write-Host "==> Uploading published files..." -ForegroundColor Cyan
scp @sshArgs -r "$publishDir\*" "${remote}:${remotePath}/"
if ($LASTEXITCODE -ne 0) { throw "scp upload failed" }

Write-Host "==> Restarting service and verifying..." -ForegroundColor Cyan
$restartCmd = "sudo systemctl daemon-reload && sudo systemctl restart $serviceName && sleep 3 && sudo systemctl is-active $serviceName && curl -s -o /dev/null -w 'local_http=%{http_code}' http://127.0.0.1:$appPort/swagger/index.html && echo && curl -s -o /dev/null -w 'public_http=%{http_code}' https://$domain/swagger/index.html && echo"
ssh @sshArgs $remote $restartCmd

Write-Host "==> Deployment complete: https://$domain/" -ForegroundColor Green
Write-Host "    Swagger: https://$domain/swagger" -ForegroundColor Green
Write-Host "    Auth API: https://$domain/api/auth/login" -ForegroundColor Green
