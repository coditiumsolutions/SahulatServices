param(
    [string]$BaseUrl = "https://localhost:7265"
)

$ErrorActionPreference = "Continue"
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

function Invoke-Api {
    param(
        [string]$Method,
        [string]$Path,
        [object]$Body = $null,
        [int[]]$ExpectStatus = @(200)
    )

    $uri = "$BaseUrl$Path"
    $headers = @{ "Accept" = "application/json" }
    $json = $null

    if ($null -ne $Body) {
        $json = ($Body | ConvertTo-Json -Depth 10 -Compress)
        $headers["Content-Type"] = "application/json"
    }

    $params = @{
        Uri             = $uri
        Method          = $Method
        Headers         = $headers
        UseBasicParsing = $true
        TimeoutSec      = 60
    }
    if ($null -ne $json) { $params.Body = $json }

    # -SkipCertificateCheck is PowerShell 7+; fall back for Windows PowerShell 5.
    $supportsSkipCert = (Get-Command Invoke-WebRequest).Parameters.ContainsKey("SkipCertificateCheck")
    if ($supportsSkipCert) { $params.SkipCertificateCheck = $true }

    try {
        $resp = Invoke-WebRequest @params
        return @{
            Ok         = $true
            StatusCode = [int]$resp.StatusCode
            Content    = $resp.Content
        }
    } catch {
        $status = 0
        $content = $_.Exception.Message

        if ($_.Exception.Response) {
            try { $status = [int]$_.Exception.Response.StatusCode } catch { }
            try {
                $stream = $_.Exception.Response.GetResponseStream()
                if ($stream) {
                    $reader = New-Object System.IO.StreamReader($stream)
                    $tmp = $reader.ReadToEnd()
                    $reader.Close()
                    if ($tmp) { $content = $tmp }
                }
            } catch { }
        }

        return @{
            Ok         = $false
            StatusCode = $status
            Content    = $content
        }
    }
}

function Try-ParseJson {
    param([string]$s)
    if (-not $s) { return $null }
    try { return ($s | ConvertFrom-Json -ErrorAction Stop) } catch { return $null }
}

Write-Host ""
Write-Host "SahulatGharTak API test: optional service request fields"
Write-Host "Base URL: $BaseUrl"
Write-Host ("=" * 60)

$connJson = Get-Content (Join-Path $PSScriptRoot "..\HomeServicesPortal\appsettings.Development.json") -Raw | ConvertFrom-Json
$connStr = $connJson.ConnectionStrings.DefaultConnection

$clientUid = $null
$categoryId = $null

try {
    Add-Type -AssemblyName System.Data
    $conn = New-Object System.Data.SqlClient.SqlConnection $connStr
    $conn.Open()

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT TOP 1 UID FROM Clients ORDER BY UID"
    $clientUid = $cmd.ExecuteScalar()

    $cmd.CommandText = "SELECT TOP 1 UID FROM ServiceCategories WHERE IsActive = 1 ORDER BY UID"
    $categoryId = $cmd.ExecuteScalar()

    $conn.Close()
} catch {
    Write-Host "[WARN] Could not seed ClientUid from DB: $($_.Exception.Message)"
}

if (-not $clientUid) {
    Write-Host "[SKIP] No Clients row found; cannot run create/update tests."
    exit 0
}
$clientUid = [int]$clientUid

if (-not $categoryId) {
    Write-Host "[SKIP] No active ServiceCategories row found in DB."
    exit 0
}
$categoryId = [int]$categoryId

$addrTitle = "OptionalPreferred-API-Test-$(Get-Date -Format 'HHmmss')"

# 1) Create client address (required for service request)
$createAddrBody = @{
    clientUid     = $clientUid
    addressTitle  = $addrTitle
    fullAddress   = "Test house, Street 1"
    area          = "Test Area"
    city          = "Karachi"
    latitude      = 24.8607
    longitude     = 67.0011
}

$createAddr = Invoke-Api -Method POST -Path "/api/client-addresses" -Body $createAddrBody -ExpectStatus @(200,201)
$addrJson = Try-ParseJson $createAddr.Content
$addressUid = $null

if ($createAddr.StatusCode -in 200,201 -and $addrJson -and $addrJson.success -eq $true -and $addrJson.data -and $addrJson.data.uid) {
    $addressUid = [int]$addrJson.data.uid
}

if (-not $addressUid) {
    Write-Host "[FAIL] Could not create client address. HTTP=$($createAddr.StatusCode) Content=$($createAddr.Content)"
    exit 1
}

# 2) Create service request with EMPTY optional fields
$createReqBody = @{
    clientUid            = $clientUid
    categoryUid          = $categoryId
    clientAddressUid     = $addressUid
    serviceTitle         = "OptionalPreferred API smoke test"
    serviceDescription   = ""      # IMPORTANT: optional and should be stored as null
    preferredServiceDate = ""      # IMPORTANT: Flutter currently sends empty string when unset
    preferredServiceTime = ""      # IMPORTANT
    isUrgent             = $false
    contactPerson        = "API Tester"
    contactNo            = "03001234567"
    estimatedBudget      = 1500
    remarks              = "auto-test"
}

$createReq = Invoke-Api -Method POST -Path "/api/customer-service-requests" -Body $createReqBody -ExpectStatus @(200,201)
$createReqJson = Try-ParseJson $createReq.Content

if ($createReq.StatusCode -notin 200,201 -or -not $createReqJson -or $createReqJson.success -ne $true) {
    Write-Host "[FAIL] POST /api/customer-service-requests with empty preferred date/time failed."
    Write-Host "HTTP=$($createReq.StatusCode)"
    Write-Host "Content=$($createReq.Content)"
    exit 1
}

$requestUid = $null
if ($createReqJson.data -and $createReqJson.data.uid) { $requestUid = [int]$createReqJson.data.uid }
if (-not $requestUid) {
    Write-Host "[FAIL] Request created but requestUid missing in response. Content=$($createReq.Content)"
    exit 1
}

if ($createReqJson.data.PSObject.Properties.Name -contains 'serviceDescription') {
    if ($null -ne $createReqJson.data.serviceDescription -and $createReqJson.data.serviceDescription -ne '') {
        Write-Host "[FAIL] Expected serviceDescription to be null when empty string is sent."
        Write-Host "Response serviceDescription=$($createReqJson.data.serviceDescription)"
        exit 1
    }
}
if ($createReqJson.data.PSObject.Properties.Name -contains 'preferredServiceDate') {
    if ($null -ne $createReqJson.data.preferredServiceDate -and $createReqJson.data.preferredServiceDate -ne '') {
        Write-Host "[FAIL] Expected preferredServiceDate to be null when empty string is sent."
        Write-Host "Response preferredServiceDate=$($createReqJson.data.preferredServiceDate)"
        exit 1
    }
}
if ($createReqJson.data.PSObject.Properties.Name -contains 'preferredServiceTime') {
    if ($null -ne $createReqJson.data.preferredServiceTime -and $createReqJson.data.preferredServiceTime -ne '') {
        Write-Host "[FAIL] Expected preferredServiceTime to be null when empty string is sent."
        Write-Host "Response preferredServiceTime=$($createReqJson.data.preferredServiceTime)"
        exit 1
    }
}

Write-Host "[PASS] Created request $requestUid with empty serviceDescription/preferredServiceDate/preferredServiceTime (stored as null)."

# 3) Update same request with EMPTY optional fields
$updateReqBody = @{
    requestUid           = $requestUid
    categoryUid          = $categoryId
    clientAddressUid     = $addressUid
    serviceTitle         = "OptionalPreferred API smoke test (updated)"
    serviceDescription   = ""      # should be treated as null
    preferredServiceDate = ""      # should be treated as null
    preferredServiceTime = ""      # should be treated as null
    isUrgent             = $true
    contactPerson        = "API Tester"
    contactNo            = "03001234567"
    estimatedBudget      = 2000
    status               = "Pending"
    remarks              = "updated"
}

$updateReq = Invoke-Api -Method PUT -Path "/api/customer-service-requests/$requestUid" -Body $updateReqBody -ExpectStatus @(200)
$updateReqJson = Try-ParseJson $updateReq.Content

if ($updateReq.StatusCode -ne 200 -or -not $updateReqJson -or $updateReqJson.success -ne $true) {
    Write-Host "[FAIL] PUT /api/customer-service-requests/{id} with empty preferred date/time failed."
    Write-Host "HTTP=$($updateReq.StatusCode)"
    Write-Host "Content=$($updateReq.Content)"
    exit 1
}

if ($updateReqJson.data.PSObject.Properties.Name -contains 'serviceDescription') {
    if ($null -ne $updateReqJson.data.serviceDescription -and $updateReqJson.data.serviceDescription -ne '') {
        Write-Host "[FAIL] Expected serviceDescription to be null after update with empty string."
        Write-Host "Response serviceDescription=$($updateReqJson.data.serviceDescription)"
        exit 1
    }
}
if ($updateReqJson.data.PSObject.Properties.Name -contains 'preferredServiceDate') {
    if ($null -ne $updateReqJson.data.preferredServiceDate -and $updateReqJson.data.preferredServiceDate -ne '') {
        Write-Host "[FAIL] Expected preferredServiceDate to be null after update with empty string."
        Write-Host "Response preferredServiceDate=$($updateReqJson.data.preferredServiceDate)"
        exit 1
    }
}
if ($updateReqJson.data.PSObject.Properties.Name -contains 'preferredServiceTime') {
    if ($null -ne $updateReqJson.data.preferredServiceTime -and $updateReqJson.data.preferredServiceTime -ne '') {
        Write-Host "[FAIL] Expected preferredServiceTime to be null after update with empty string."
        Write-Host "Response preferredServiceTime=$($updateReqJson.data.preferredServiceTime)"
        exit 1
    }
}

Write-Host "[PASS] Updated request $requestUid with empty serviceDescription/preferredServiceDate/preferredServiceTime (stored as null)."

# Cleanup: delete request and address (best-effort)
$null = Invoke-Api -Method DELETE -Path "/api/customer-service-requests/$requestUid" -ExpectStatus @(200)
$null = Invoke-Api -Method DELETE -Path "/api/client-addresses/$addressUid" -ExpectStatus @(200)

Write-Host "[DONE] Optional preferred date/time tests completed."

