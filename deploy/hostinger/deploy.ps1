param(
    [string]$ConfigPath = "$PSScriptRoot\deploy.config.psd1"
)

$ErrorActionPreference = "Stop"
$config = Import-PowerShellDataFile $ConfigPath

$keyName = $config.SshKey
$config.SshKey = if ([System.IO.Path]::IsPathRooted($keyName)) {
    $keyName
} else {
    Join-Path $env:USERPROFILE ".ssh\$keyName"
}

# Also allow D:\.ssh fallback for Hostinger key name only
if (-not (Test-Path $config.SshKey) -and $keyName -eq 'hostinger_vps') {
    $config.SshKey = 'D:\.ssh\hostinger_vps'
}

$projectRoot = Resolve-Path "$PSScriptRoot\..\.."
$publishDir = Join-Path $projectRoot "publish"
$projectFile = Join-Path $projectRoot "HomeServicesPortal\HomeServicesPortal.csproj"

$sshArgs = @(
    "-F", "$env:TEMP\empty_ssh_config",
    "-i", $config.SshKey,
    "-o", "BatchMode=yes",
    "-o", "StrictHostKeyChecking=yes"
)

if (-not (Test-Path $config.SshKey)) {
    throw "SSH key not found: $($config.SshKey). Update deploy.config.psd1"
}

Write-Host "==> Publishing Release build (linux-x64)..." -ForegroundColor Cyan
dotnet publish $projectFile -c Release -o $publishDir --self-contained false -r linux-x64
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

if (-not (Test-Path "$env:TEMP\empty_ssh_config")) {
    New-Item -ItemType File -Path "$env:TEMP\empty_ssh_config" -Force | Out-Null
}

$remote = "$($config.SshUser)@$($config.SshHost)"
$remotePath = $config.RemotePath

Write-Host "==> Testing SSH connection to $remote ..." -ForegroundColor Cyan
ssh @sshArgs $remote "echo SSH_OK && whoami && uname -a"
if ($LASTEXITCODE -ne 0) {
    throw @"
SSH connection failed.

Fix checklist:
1. In Hostinger hPanel -> VPS -> SSH Access, copy your SSH username (often 'root' or u123456789).
2. Add your PUBLIC key (.pub) to the VPS authorized keys.
3. Update deploy\hostinger\deploy.config.psd1 with SshUser and SshKey path.
4. Re-run: .\deploy\hostinger\deploy.ps1
"@
}

Write-Host "==> Ensuring remote directory exists..." -ForegroundColor Cyan
ssh @sshArgs $remote "mkdir -p $remotePath/wwwroot/uploads/providers $remotePath/wwwroot/uploads/documents"

Write-Host "==> Uploading published files..." -ForegroundColor Cyan
scp @sshArgs -r "$publishDir\*" "${remote}:${remotePath}/"
if ($LASTEXITCODE -ne 0) { throw "scp upload failed" }

Write-Host "==> Setting permissions and restarting service..." -ForegroundColor Cyan
ssh @sshArgs $remote @"
chown -R www-data:www-data $remotePath
if systemctl list-unit-files | grep -q '$($config.ServiceName)'; then
  systemctl restart $($config.ServiceName)
  systemctl status $($config.ServiceName) --no-pager -l | head -20
else
  echo 'Service not installed yet. Run setup-server.sh on the server once.'
fi
"@

Write-Host "==> Deployment complete: https://sahulatghartak.com/" -ForegroundColor Green
