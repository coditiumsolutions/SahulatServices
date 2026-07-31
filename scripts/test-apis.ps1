# Smoke-test documented REST APIs against a running HomeServicesPortal instance.
# Usage:
#   .\scripts\test-apis.ps1
#   .\scripts\test-apis.ps1 -BaseUrl "https://localhost:7265"
#   .\scripts\test-apis.ps1 -BaseUrl "http://127.0.0.1:5310"

param(
    [string]$BaseUrl = "https://localhost:7265"
)

$ErrorActionPreference = "Continue"
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

$script:Pass = 0
$script:Fail = 0
$script:Skip = 0
$results = New-Object System.Collections.Generic.List[object]

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
        $json = ($Body | ConvertTo-Json -Depth 8 -Compress)
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
            Ok          = $true
            StatusCode  = [int]$resp.StatusCode
            Content     = $resp.Content
            ExpectMatch = ($ExpectStatus -contains [int]$resp.StatusCode)
        }
    } catch {
        $status = 0
        $content = $_.Exception.Message
        if ($_.ErrorDetails -and $_.ErrorDetails.Message) {
            $content = $_.ErrorDetails.Message
            if ($content -match '"status"\s*:\s*(\d+)') { }
        }
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
        # PS7: status often only in Exception.Response or message like "Response status code does not indicate success: 404"
        if ($status -eq 0 -and $_.Exception.Message -match '(\d{3})\s*\(') {
            $status = [int]$Matches[1]
        }
        if ($status -eq 0 -and $_.Exception.Message -match 'status code does not indicate success:\s*(\d{3})') {
            $status = [int]$Matches[1]
        }
        return @{
            Ok          = $false
            StatusCode  = $status
            Content     = $content
            ExpectMatch = ($ExpectStatus -contains $status)
        }
    }
}

function Assert-Test {
    param(
        [string]$Name,
        [hashtable]$Result,
        [int[]]$ExpectStatus = @(200),
        [scriptblock]$ExtraCheck = $null
    )

    $matched = $ExpectStatus -contains $Result.StatusCode
    $extraOk = $true
    $extraMsg = ""
    if ($matched -and $ExtraCheck) {
        try {
            $parsed = $null
            if ($Result.Content) {
                $parsed = $Result.Content | ConvertFrom-Json -ErrorAction Stop
            }
            $extraOk = & $ExtraCheck $parsed $Result
        } catch {
            $extraOk = $false
            $extraMsg = $_.Exception.Message
        }
    }

    $ok = $matched -and $extraOk
    if ($ok) {
        $script:Pass++
        $status = "PASS"
    } else {
        $script:Fail++
        $status = "FAIL"
    }

    $msg = "HTTP $($Result.StatusCode); expected $($ExpectStatus -join ',')"
    if ($extraMsg) { $msg += "; $extraMsg" }
    if (-not $ok -and $Result.Content) {
        $snippet = $Result.Content
        if ($snippet.Length -gt 180) { $snippet = $snippet.Substring(0, 180) + "..." }
        $msg += " | $snippet"
    }

    $results.Add([pscustomobject]@{ Status = $status; Name = $Name; Detail = $msg }) | Out-Null
    Write-Host ("[{0}] {1} --- {2}" -f $status, $Name, $msg)
}

function Assert-Gone {
    param([string]$Name, [string]$Path)

    $r = Invoke-Api -Method GET -Path $Path -ExpectStatus @(404)
    # 404 from ASP.NET routing for missing controller, or 405, etc.
    $ok = $r.StatusCode -in 404, 405
    if ($ok) {
        $script:Pass++
        Write-Host "[PASS] $Name --- HTTP $($r.StatusCode) (endpoint removed)"
        $results.Add([pscustomobject]@{ Status = "PASS"; Name = $Name; Detail = "HTTP $($r.StatusCode)" }) | Out-Null
    } else {
        $script:Fail++
        Write-Host "[FAIL] $Name --- expected 404/405, got $($r.StatusCode)"
        $results.Add([pscustomobject]@{ Status = "FAIL"; Name = $Name; Detail = "HTTP $($r.StatusCode)" }) | Out-Null
    }
}

Write-Host ""
Write-Host "SahulatGharTak API smoke tests"
Write-Host "Base URL: $BaseUrl"
Write-Host ("=" * 60)

# --- Removed endpoints must not exist ---
Assert-Gone -Name "REMOVED provider-profiles" -Path "/api/provider-profiles"
Assert-Gone -Name "REMOVED providers/{id}/service-requests" -Path "/api/providers/4/service-requests"

# --- Service categories ---
$r = Invoke-Api -Method GET -Path "/api/service-categories"
Assert-Test -Name "GET service-categories" -Result $r -ExtraCheck {
    param($data, $raw)
    return ($null -ne $data)
}

$categoryId = $null
if ($r.StatusCode -eq 200 -and $r.Content) {
    try {
        $cats = $r.Content | ConvertFrom-Json
        if ($cats -is [System.Array] -and $cats.Count -gt 0) {
            $categoryId = [int]$cats[0].id
        } elseif ($cats.id) {
            $categoryId = [int]$cats.id
        }
    } catch { }
}

if ($categoryId) {
    $r = Invoke-Api -Method GET -Path "/api/service-categories/$categoryId"
    Assert-Test -Name "GET service-categories/{id}" -Result $r
} else {
    $script:Skip++
    Write-Host "[SKIP] GET service-categories/{id} --- no category seed data"
}

# --- Discover a provider for detail / availability tests ---
$providerUid = $null
$connJson = Get-Content (Join-Path $PSScriptRoot "..\HomeServicesPortal\appsettings.Development.json") -Raw | ConvertFrom-Json
$connStr = $connJson.ConnectionStrings.DefaultConnection
try {
    Add-Type -AssemblyName System.Data
    $conn = New-Object System.Data.SqlClient.SqlConnection $connStr
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT TOP 1 UID FROM Providers ORDER BY UID"
    $providerUid = $cmd.ExecuteScalar()
    $cmd.CommandText = "SELECT TOP 1 UID FROM Clients ORDER BY UID"
    $clientUid = $cmd.ExecuteScalar()
    $cmd.CommandText = "SELECT TOP 1 MobileNo FROM UsersLogin WHERE UserType='Client' AND IsActive=1 ORDER BY UID"
    $clientMobile = $cmd.ExecuteScalar()
    $conn.Close()
} catch {
    Write-Host "[WARN] Could not seed IDs from DB: $($_.Exception.Message)"
}

if (-not $providerUid) {
    $script:Skip++
    Write-Host "[SKIP] Provider-dependent tests --- no Providers row found"
} else {
    $providerUid = [int]$providerUid

    $r = Invoke-Api -Method GET -Path "/api/providers-detail/$providerUid"
    Assert-Test -Name "GET providers-detail/{id}" -Result $r -ExtraCheck {
        param($data, $raw)
        return $data.success -eq $true -and $data.data.uid -eq $providerUid
    }

    $r = Invoke-Api -Method GET -Path "/api/provider-avability-status/$providerUid"
    Assert-Test -Name "GET provider-avability-status/{id}" -Result $r -ExtraCheck {
        param($data, $raw)
        return $data.success -eq $true
    }

    $availBody = @{
        providerUid     = $providerUid
        isOnline        = $true
        availableFrom   = "09:00"
        availableTo     = "18:00"
    }
    $r = Invoke-Api -Method PUT -Path "/api/provider-avability-status/$providerUid" -Body $availBody
    Assert-Test -Name "PUT provider-avability-status/{id}" -Result $r -ExtraCheck {
        param($data, $raw)
        return $data.success -eq $true
    }
}

# --- Auth login (expects existing client; soft-fail if credentials unknown) ---
$loginMobile = if ($clientMobile) { [string]$clientMobile } else { "03335191392" }
$r = Invoke-Api -Method POST -Path "/api/auth/login" -Body @{ mobileNo = $loginMobile; password = "ghauri" } -ExpectStatus @(200, 401, 403)
Assert-Test -Name "POST auth/login (known or invalid password)" -Result $r -ExpectStatus @(200, 401, 403) -ExtraCheck {
    param($data, $raw)
    return $null -ne $data.success
}

# --- Client addresses + service requests (require a client) ---
if (-not $clientUid) {
    $script:Skip++
    Write-Host "[SKIP] Client address / service-request CRUD --- no Clients row found"
} else {
    $clientUid = [int]$clientUid
    if (-not $categoryId) {
        $script:Skip++
        Write-Host "[SKIP] Service-request create --- no category id"
    }

    $addrTitle = "API-Test-$(Get-Date -Format 'HHmmss')"
    $createAddr = @{
        clientUid    = $clientUid
        addressTitle = $addrTitle
        fullAddress  = "Test house, Street 1"
        area         = "Test Area"
        city         = "Karachi"
        latitude     = 24.8607
        longitude    = 67.0011
    }
    $r = Invoke-Api -Method POST -Path "/api/client-addresses" -Body $createAddr -ExpectStatus @(200, 201)
    Assert-Test -Name "POST client-addresses" -Result $r -ExpectStatus @(200, 201) -ExtraCheck {
        param($data, $raw)
        return $data.success -eq $true -and $data.data.uid -gt 0
    }

    $addressUid = $null
    if ($r.StatusCode -in 200, 201 -and $r.Content) {
        $addressUid = [int](($r.Content | ConvertFrom-Json).data.uid)
    }

    $r = Invoke-Api -Method GET -Path "/api/client-addresses?clientUid=$clientUid"
    Assert-Test -Name "GET client-addresses?clientUid=" -Result $r -ExtraCheck {
        param($data, $raw)
        return $data.success -eq $true
    }

    if ($addressUid) {
        $r = Invoke-Api -Method GET -Path "/api/client-addresses/$addressUid"
        Assert-Test -Name "GET client-addresses/{id}" -Result $r

        $updateAddr = @{
            addressUid   = $addressUid
            clientUid    = $clientUid
            addressTitle = "$addrTitle-upd"
            fullAddress  = "Updated address"
            area         = "Updated Area"
            city         = "Lahore"
            latitude     = 31.52
            longitude    = 74.35
        }
        $r = Invoke-Api -Method PUT -Path "/api/client-addresses/$addressUid" -Body $updateAddr
        Assert-Test -Name "PUT client-addresses/{id}" -Result $r

        $requestUid = $null
        if ($categoryId) {
            $createReq = @{
                clientUid            = $clientUid
                categoryUid          = $categoryId
                clientAddressUid     = $addressUid
                serviceTitle         = "API smoke test request"
                serviceDescription   = "Created by scripts/test-apis.ps1"
                preferredServiceDate = (Get-Date).AddDays(2).ToString("yyyy-MM-dd")
                preferredServiceTime = "10:00"
                isUrgent             = $false
                contactPerson        = "API Tester"
                contactNo            = "03001234567"
                estimatedBudget      = 1500
                remarks              = "auto-test"
            }
            $r = Invoke-Api -Method POST -Path "/api/customer-service-requests" -Body $createReq -ExpectStatus @(200, 201)
            Assert-Test -Name "POST customer-service-requests" -Result $r -ExpectStatus @(200, 201) -ExtraCheck {
                param($data, $raw)
                return $data.success -eq $true -and $data.data.uid -gt 0
            }
            if ($r.StatusCode -in 200, 201) {
                $requestUid = [int](($r.Content | ConvertFrom-Json).data.uid)
            }

            $r = Invoke-Api -Method GET -Path "/api/customer-service-requests?clientUid=$clientUid"
            Assert-Test -Name "GET customer-service-requests?clientUid=" -Result $r

            if ($requestUid) {
                $r = Invoke-Api -Method GET -Path "/api/customer-service-requests/$requestUid"
                Assert-Test -Name "GET customer-service-requests/{id}" -Result $r

                $updateReq = @{
                    requestUid           = $requestUid
                    categoryUid          = $categoryId
                    clientAddressUid     = $addressUid
                    serviceTitle         = "API smoke test request UPD"
                    serviceDescription   = "Updated by scripts/test-apis.ps1"
                    preferredServiceDate = (Get-Date).AddDays(3).ToString("yyyy-MM-dd")
                    preferredServiceTime = "14:00"
                    isUrgent             = $true
                    contactPerson        = "API Tester"
                    contactNo            = "03001234567"
                    estimatedBudget      = 2000
                    status               = "Pending"
                    remarks              = "updated"
                }
                $r = Invoke-Api -Method PUT -Path "/api/customer-service-requests/$requestUid" -Body $updateReq
                Assert-Test -Name "PUT customer-service-requests/{id}" -Result $r

                $r = Invoke-Api -Method DELETE -Path "/api/customer-service-requests/$requestUid"
                Assert-Test -Name "DELETE customer-service-requests/{id}" -Result $r
            }
        }

        $r = Invoke-Api -Method DELETE -Path "/api/client-addresses/$addressUid"
        Assert-Test -Name "DELETE client-addresses/{id}" -Result $r
    }
}

Write-Host ""
Write-Host ("=" * 60)
Write-Host ("RESULT: {0} passed, {1} failed, {2} skipped" -f $script:Pass, $script:Fail, $script:Skip)
Write-Host ""

if ($script:Fail -gt 0) {
    exit 1
}
exit 0

