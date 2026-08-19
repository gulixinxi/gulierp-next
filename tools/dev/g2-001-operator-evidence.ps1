#requires -Version 5.1
<#
.SYNOPSIS
    G2-001R1 Operator Evidence Pack — verify the real-PostgreSQL round of G2-001.

.DESCRIPTION
    G2-001R1 fixed two operator-visible defects:
      1. /health/ready returned 503 with the operator's real connection
         string. Root cause: Program.cs was re-adding appsettings.json AFTER
         the default env-var provider, so appsettings.json (with
         Password=CHANGE_ME) overrode the env-var reading. Fix: removed
         the duplicate AddJsonFile calls.
      2. The 5 good-DB integration tests were statically [Fact(Skip=...)]
         which is xunit-discovery-time evaluated and never runs. Fix:
         plain [Fact] + loud "env var not set" exception; the operator
         evidence pack sets the env var so the tests execute.

    This script:
      1. Prompts for the connection string (interactive) OR honours the
         GULIERP_FOUNDATION_CONNECTION / ConnectionStrings__GuliERP env var.
      2. Stops any lingering GuliERP.Api process.
      3. Builds Release (mandatory; --no-build removed because stale
         binaries are a known G2-001R1 risk).
      4. Runs `dotnet ef database update` against the real PostgreSQL.
      5. Runs the integration tests against the real PostgreSQL.
      6. Starts the host (Round 1) in a separate process, hits
         /health/live and /health/ready, stops the host. The
         DiagnosticResponseWriter emits a JSON body with the actual
         readiness failure reason (so a future 503 surfaces the root
         cause in the response).
      7. Restarts the host (Round 2) and re-runs the readiness probes.
      8. Bad-DB negative round: starts the host with a hard-coded bad
         connection, asserts /health/live=200 + /health/ready=503.

    The script is intentionally PowerShell-only and G2-001R1-scoped. It
    does NOT auto-advance to G2-002. After it passes, the Operator must
    flip the G2_001_HOST_POSTGRESQL_VERIFIED gate in
    docs/governance/GOAL_REGISTRY.md.

.PARAMETER ConnectionString
    Optional. If not provided, the script reads
    $env:ConnectionStrings__GuliERP or
    $env:GULIERP_FOUNDATION_CONNECTION.

.PARAMETER SkipPrompt
    When set, do not interactively prompt for the password; assume the
    password is already baked into the connection string.

.PARAMETER SkipBadDb
    Skip the bad-DB negative round. Useful for re-runs when the good
    round has already been confirmed.

.EXAMPLE
    PS> .\g2-001-operator-evidence.ps1
    (interactive: paste the full Npgsql connection string including password)

.EXAMPLE
    PS> $env:ConnectionStrings__GuliERP = "Host=192.168.2.228;Port=5432;Database=gulierp_g2_001;Username=gulidata;Password=***"
    PS> .\g2-001-operator-evidence.ps1 -SkipPrompt
#>
[CmdletBinding()]
param(
    [string]$ConnectionString,
    [switch]$SkipPrompt,
    [switch]$SkipBadDb
)

$ErrorActionPreference = 'Stop'
Set-Location -Path (Join-Path $PSScriptRoot '..\..')

$DOTNET = 'D:\guli\gulierp\.dotnet\dotnet.exe'
if (-not (Test-Path $DOTNET)) {
    throw "G2-001R1 requires D:\guli\gulierp\.dotnet\dotnet.exe. Not found."
}

# --- 0. Resolve connection string ----------------------------------------
if (-not $ConnectionString) {
    $ConnectionString = $env:ConnectionStrings__GuliERP
}
if (-not $ConnectionString) {
    $ConnectionString = $env:GULIERP_FOUNDATION_CONNECTION
}
if (-not $ConnectionString) {
    if ($SkipPrompt) {
        throw 'No connection string provided. Set $env:ConnectionStrings__GuliERP or pass -ConnectionString.'
    }
    $ConnectionString = Read-Host -Prompt 'Npgsql connection string (Host=...;Port=...;Database=...;Username=...;Password=...)'
}

$displayConn = ($ConnectionString -replace 'Password=[^;]+', 'Password=***')
Write-Host "[G2-001R1] Using connection: $displayConn" -ForegroundColor Cyan

# Expose the env var for the rest of the run.
$env:ConnectionStrings__GuliERP = $ConnectionString
$env:GULIERP_FOUNDATION_CONNECTION = $ConnectionString

# --- 0a. Stop any lingering GuliERP.Api process from prior runs ---------
$lingering = Get-Process -Name 'GuliERP.Api' -ErrorAction SilentlyContinue
if ($lingering) {
    Write-Host "[G2-001R1] Stopping $($lingering.Count) lingering GuliERP.Api process(es) from prior runs" -ForegroundColor Yellow
    $lingering | ForEach-Object { Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue }
    Start-Sleep -Seconds 2
}

# --- Helpers -------------------------------------------------------------
$script:BadDbResult = $null
$script:Round1Result = $null
$script:Round2Result = $null
$script:MigrationResult = $null
$script:TestResult = $null

# Probe a URL, capture both the status code AND the response body. The
# default Invoke-WebRequest throws on non-2xx, which is wrong for
# expected-503 bad-DB probes; this helper never throws.
function Invoke-StatusProbe {
    param(
        [string]$Url,
        [int]$TimeoutSec = 10
    )
    $client = [System.Net.Http.HttpClient]::new()
    $client.Timeout = [TimeSpan]::FromSeconds($TimeoutSec)
    try {
        $resp = $client.GetAsync($Url).GetAwaiter().GetResult()
        $body = $resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        return [pscustomobject]@{
            Status = [int]$resp.StatusCode
            Body = $body
        }
    } catch {
        return [pscustomobject]@{
            Status = -1
            Body = "EXCEPTION: $($_.Exception.Message)"
        }
    } finally {
        $client.Dispose()
    }
}

function Start-GuliHost {
    param([int]$Port = 5099, [string]$Env = 'Production')
    $env:ASPNETCORE_URLS = "http://127.0.0.1:$Port"
    $env:ASPNETCORE_ENVIRONMENT = $Env
    $proc = Start-Process -FilePath $DOTNET `
        -ArgumentList 'run','--project','apps/api/GuliERP.Api/GuliERP.Api.csproj','--no-build','-c','Release' `
        -PassThru `
        -RedirectStandardOutput "$env:TEMP\g2-host-r1.log" `
        -RedirectStandardError  "$env:TEMP\g2-host-r1-err.log"
    return $proc
}

function Wait-ForHostReady {
    param([string]$Url, [int]$TimeoutSec = 30)
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        try {
            $client = [System.Net.Http.HttpClient]::new()
            $client.Timeout = [TimeSpan]::FromSeconds(2)
            $r = $client.GetAsync($Url).GetAwaiter().GetResult()
            $client.Dispose()
            if ($r.StatusCode -ge 200 -and $r.StatusCode -lt 500) { return $true }
        } catch { }
        Start-Sleep -Milliseconds 500
    }
    return $false
}

# --- 1. Build (Release) -------------------------------------------------
Write-Host "`n[G2-001R1] Step 1/8 — dotnet build -c Release (mandatory; --no-build removed)" -ForegroundColor Yellow
& $DOTNET build GuliERP.slnx -c Release 2>&1 | Out-String -Stream |
    Where-Object { $_ -match '成功生成|失败|Build succeeded|Build FAILED' } |
    ForEach-Object { Write-Host "  $_" }
if ($LASTEXITCODE -ne 0) { throw "Release build failed with exit $LASTEXITCODE" }

# --- 2. Apply migration --------------------------------------------------
Write-Host "`n[G2-001R1] Step 2/8 — dotnet ef database update" -ForegroundColor Yellow
try {
    & $DOTNET ef database update `
        --project 'modules\foundation\GuliERP.Foundation\GuliERP.Foundation.csproj' `
        2>&1 | Out-String -Stream |
        ForEach-Object { Write-Host "  $_" }
    if ($LASTEXITCODE -ne 0) { throw "dotnet ef database update failed with exit $LASTEXITCODE" }
    $script:MigrationResult = 'PASS'
} catch {
    $script:MigrationResult = "FAIL: $($_.Exception.Message)"
    Write-Host "  MIGRATION FAILED: $($_.Exception.Message)" -ForegroundColor Red
}

# --- 3. Integration tests ------------------------------------------------
Write-Host "`n[G2-001R1] Step 3/8 — integration tests (real DB)" -ForegroundColor Yellow
try {
    & $DOTNET test 'tests\GuliERP.Foundation.IntegrationTests\GuliERP.Foundation.IntegrationTests.csproj' `
        -c Release --no-build `
        2>&1 | Out-String -Stream |
        ForEach-Object { Write-Host "  $_" }
    if ($LASTEXITCODE -ne 0) { throw "Integration tests failed with exit $LASTEXITCODE" }
    $script:TestResult = 'PASS'
} catch {
    $script:TestResult = "FAIL: $($_.Exception.Message)"
    Write-Host "  INTEGRATION TESTS FAILED: $($_.Exception.Message)" -ForegroundColor Red
}

# --- 4. Runtime Round 1 (real DB) ---------------------------------------
Write-Host "`n[G2-001R1] Step 4/8 — host Round 1 (real DB)" -ForegroundColor Yellow
$proc1 = Start-GuliHost -Port 5099
try {
    if (-not (Wait-ForHostReady -Url 'http://127.0.0.1:5099/' -TimeoutSec 30)) {
        throw 'Host did not respond within 30s in Round 1'
    }

    $live = Invoke-StatusProbe -Url 'http://127.0.0.1:5099/health/live'
    Write-Host "  /health/live:  $($live.Status) — $($live.Body)"
    $ready = Invoke-StatusProbe -Url 'http://127.0.0.1:5099/health/ready'
    Write-Host "  /health/ready: $($ready.Status) — $($ready.Body)"

    $script:Round1Result = [pscustomobject]@{
        Live = $live
        Ready = $ready
    }
} finally {
    Stop-Process -Id $proc1.Id -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
}

# --- 5. Runtime Round 2 (real DB) ---------------------------------------
Write-Host "`n[G2-001R1] Step 5/8 — host Round 2 (real DB)" -ForegroundColor Yellow
$proc2 = Start-GuliHost -Port 5099
try {
    if (-not (Wait-ForHostReady -Url 'http://127.0.0.1:5099/' -TimeoutSec 30)) {
        throw 'Host did not respond within 30s in Round 2'
    }

    $live = Invoke-StatusProbe -Url 'http://127.0.0.1:5099/health/live'
    Write-Host "  /health/live:  $($live.Status) — $($live.Body)"
    $ready = Invoke-StatusProbe -Url 'http://127.0.0.1:5099/health/ready'
    Write-Host "  /health/ready: $($ready.Status) — $($ready.Body)"

    $script:Round2Result = [pscustomobject]@{
        Live = $live
        Ready = $ready
    }
} finally {
    Stop-Process -Id $proc2.Id -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
}

# --- 6. Bad-DB negative round --------------------------------------------
if (-not $SkipBadDb) {
    Write-Host "`n[G2-001R1] Step 6/8 — host bad-DB negative round" -ForegroundColor Yellow
    $env:ConnectionStrings__GuliERP = 'Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=2;Command Timeout=2'
    $proc3 = Start-GuliHost -Port 5099
    try {
        if (-not (Wait-ForHostReady -Url 'http://127.0.0.1:5099/' -TimeoutSec 30)) {
            throw 'Host did not respond within 30s in bad-DB round'
        }

        $live = Invoke-StatusProbe -Url 'http://127.0.0.1:5099/health/live'
        Write-Host "  /health/live:  $($live.Status) — $($live.Body)"
        $ready = Invoke-StatusProbe -Url 'http://127.0.0.1:5099/health/ready'
        Write-Host "  /health/ready: $($ready.Status) — $($ready.Body)"

        $script:BadDbResult = [pscustomobject]@{
            Live = $live
            Ready = $ready
        }
    } finally {
        Stop-Process -Id $proc3.Id -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
    }
    # Restore the real connection for the summary.
    $env:ConnectionStrings__GuliERP = $ConnectionString
}

# --- 7. Final summary ---------------------------------------------------
Write-Host "`n[G2-001R1] SUMMARY" -ForegroundColor Cyan

$summary = [pscustomobject]@{
    Migration     = $script:MigrationResult
    Integration    = $script:TestResult
    Round1         = $script:Round1Result
    Round2         = $script:Round2Result
    BadDbNegative  = $script:BadDbResult
}

Write-Host ($summary | ConvertTo-Json -Depth 6)

# --- 8. Hard-stop decision ---------------------------------------------
$hardFails = @()
if ($script:MigrationResult -ne 'PASS') { $hardFails += "Migration: $($script:MigrationResult)" }
if ($script:TestResult -ne 'PASS') { $hardFails += "Integration tests: $($script:TestResult)" }
if ($null -eq $script:Round1Result -or $script:Round1Result.Live.Status -ne 200 -or $script:Round1Result.Ready.Status -ne 200) {
    $hardFails += "Round 1: live=$($script:Round1Result.Live.Status) ready=$($script:Round1Result.Ready.Status)"
}
if ($null -eq $script:Round2Result -or $script:Round2Result.Live.Status -ne 200 -or $script:Round2Result.Ready.Status -ne 200) {
    $hardFails += "Round 2: live=$($script:Round2Result.Live.Status) ready=$($script:Round2Result.Ready.Status)"
}
if (-not $SkipBadDb) {
    if ($null -eq $script:BadDbResult -or $script:BadDbResult.Live.Status -ne 200 -or $script:BadDbResult.Ready.Status -ne 503) {
        $hardFails += "Bad-DB negative: live=$($script:BadDbResult.Live.Status) (expect 200) ready=$($script:BadDbResult.Ready.Status) (expect 503)"
    }
}

if ($hardFails.Count -eq 0) {
    Write-Host "`n[G2-001R1] ALL CHECKS PASS" -ForegroundColor Green
    Write-Host "  Next: open docs/verification/G2_001_HOST_POSTGRESQL_REPORT.md"
    Write-Host "  and update docs/governance/GOAL_REGISTRY.md to mark G2-001 as VERIFIED."
    exit 0
} else {
    Write-Host "`n[G2-001R1] HARD STOPS:" -ForegroundColor Red
    $hardFails | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}
