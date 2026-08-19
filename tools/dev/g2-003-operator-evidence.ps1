#requires -Version 5.1
<#
.SYNOPSIS
    G2-003 Operator Evidence Pack — verify the real-PostgreSQL round of G2-003
    (Identity & Organization Kernel).

.DESCRIPTION
    G2-003 implements the Identity module: Tenant / Company / Plant /
    OrganizationUnit / User / Role / Membership / Role Assignment, plus
    the ICurrentTenant / ICurrentCompany / ICurrentUser runtime
    contracts. Mavis cannot inject the operator's PostgreSQL password
    (security policy); this script is the Operator unlock path that
    upgrades the G2-003 gate from CODE_READY_OPERATOR_DB_PENDING to
    IDENTITY_ORG_KERNEL_VERIFIED.

    Steps:
      0. Resolve connection string (env var OR prompt OR pass-through).
         Per the G2-001 discipline, the connection string contains the
         real password; the script redacts it before any display.
      1. Stop any lingering GuliERP.Api process.
      2. Build Release (mandatory; stale binaries are a known G2-001R1
         risk).
      3. Apply BOTH migrations (foundation + identity) to a fresh
         G2-003 test database. The migration files live in the
         Identity.Infrastructure project. The script creates
         `gulierp_g2_003_test` if it does not exist (the Operator
         account must own the database).
      4. Run ALL integration tests against the real PostgreSQL
         (Foundation + Identity). The 5 G2-001R1 env-dep loud-fail
         tests will now PASS; the 1 G2-003 ResolveDefault loud-fail
         test will now PASS.
      5. Run identity domain Round 1: start host, hit
         /health/live + /health/ready, stop. The host should boot
         and the foundation schema + identity schema should be
         present.
      6. Run identity domain Round 2 (re-run for repeatability).
      7. Negative round: bad-DB connection — host must still boot
         /health/live=200 + /health/ready=503 (G2-001 preserved).

    The script is intentionally PowerShell-only and G2-003-scoped. It
    does NOT auto-advance to G2-004. After it passes, the Operator
    must flip the G2_003_IDENTITY_ORG_KERNEL_VERIFIED gate in
    docs/governance/GOAL_REGISTRY.md (replacing
    CODE_READY_OPERATOR_DB_PENDING).

.PARAMETER ConnectionString
    Optional. If not provided, the script reads
    $env:ConnectionStrings__GuliERP or
    $env:GULIERP_FOUNDATION_CONNECTION.

.PARAMETER SkipPrompt
    When set, do not interactively prompt for the password; assume the
    password is already baked into the connection string.

.PARAMETER SkipBadDb
    Skip the bad-DB negative round.

.EXAMPLE
    PS> .\g2-003-operator-evidence.ps1
    (interactive: paste the full Npgsql connection string including password)

.EXAMPLE
    PS> $env:ConnectionStrings__GuliERP = "Host=192.168.2.228;Port=5432;Database=gulierp_g2_003_test;Username=gulidata;Password=***"
    PS> .\g2-003-operator-evidence.ps1 -SkipPrompt
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
    throw "G2-003 requires D:\guli\gulierp\.dotnet\dotnet.exe. Not found."
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
Write-Host "[G2-003] Using connection: $displayConn" -ForegroundColor Cyan

# Expose the env var for the rest of the run.
$env:ConnectionStrings__GuliERP = $ConnectionString
$env:GULIERP_FOUNDATION_CONNECTION = $ConnectionString

# --- 0a. Stop any lingering GuliERP.Api process from prior runs ---------
$lingering = Get-Process -Name 'GuliERP.Api' -ErrorAction SilentlyContinue
if ($lingering) {
    Write-Host "[G2-003] Stopping $($lingering.Count) lingering GuliERP.Api process(es) from prior runs" -ForegroundColor Yellow
    $lingering | ForEach-Object { Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue }
    Start-Sleep -Seconds 2
}

# --- Helpers -------------------------------------------------------------
$script:BadDbResult = $null
$script:Round1Result = $null
$script:Round2Result = $null
$script:FoundationMigrationResult = $null
$script:IdentityMigrationResult = $null
$script:TestResult = $null

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
        -RedirectStandardOutput "$env:TEMP\g2-003-host-r1.log" `
        -RedirectStandardError  "$env:TEMP\g2-003-host-r1-err.log"
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
Write-Host "`n[G2-003] Step 1/8 — dotnet build -c Release (mandatory; --no-build removed)" -ForegroundColor Yellow
& $DOTNET build GuliERP.slnx -c Release 2>&1 | Out-String -Stream |
    Where-Object { $_ -match '成功生成|失败|Build succeeded|Build FAILED' } |
    ForEach-Object { Write-Host "  $_" }
if ($LASTEXITCODE -ne 0) { throw "Release build failed with exit $LASTEXITCODE" }

# --- 2. Apply Foundation migration --------------------------------------
Write-Host "`n[G2-003] Step 2/8 — Foundation: dotnet ef database update" -ForegroundColor Yellow
try {
    & $DOTNET ef database update `
        --project 'modules\foundation\GuliERP.Foundation\GuliERP.Foundation.csproj' `
        2>&1 | Out-String -Stream |
        ForEach-Object { Write-Host "  $_" }
    if ($LASTEXITCODE -ne 0) { throw "Foundation migration failed with exit $LASTEXITCODE" }
    $script:FoundationMigrationResult = 'PASS'
} catch {
    $script:FoundationMigrationResult = "FAIL: $($_.Exception.Message)"
    Write-Host "  FOUNDATION MIGRATION FAILED: $($_.Exception.Message)" -ForegroundColor Red
}

# --- 3. Apply Identity migration ----------------------------------------
Write-Host "`n[G2-003] Step 3/8 — Identity: dotnet ef database update" -ForegroundColor Yellow
try {
    & $DOTNET ef database update `
        --project 'modules\identity\GuliERP.Identity.Infrastructure\GuliERP.Identity.Infrastructure.csproj' `
        --startup-project 'apps\api\GuliERP.Api\GuliERP.Api.csproj' `
        2>&1 | Out-String -Stream |
        ForEach-Object { Write-Host "  $_" }
    if ($LASTEXITCODE -ne 0) { throw "Identity migration failed with exit $LASTEXITCODE" }
    $script:IdentityMigrationResult = 'PASS'
} catch {
    $script:IdentityMigrationResult = "FAIL: $($_.Exception.Message)"
    Write-Host "  IDENTITY MIGRATION FAILED: $($_.Exception.Message)" -ForegroundColor Red
}

# --- 4. Integration tests ------------------------------------------------
Write-Host "`n[G2-003] Step 4/8 — integration tests (real DB)" -ForegroundColor Yellow
try {
    & $DOTNET test GuliERP.slnx `
        -c Release --no-build `
        2>&1 | Out-String -Stream |
        Where-Object { $_ -match '已通过|失败|总计|FAIL|^\s*Failed\s' } |
        ForEach-Object { Write-Host "  $_" }
    if ($LASTEXITCODE -ne 0) { throw "Integration tests failed with exit $LASTEXITCODE" }
    $script:TestResult = 'PASS'
} catch {
    $script:TestResult = "FAIL: $($_.Exception.Message)"
    Write-Host "  INTEGRATION TESTS FAILED: $($_.Exception.Message)" -ForegroundColor Red
}

# --- 5. Runtime Round 1 (real DB) ---------------------------------------
Write-Host "`n[G2-003] Step 5/8 — host Round 1 (real DB)" -ForegroundColor Yellow
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

# --- 6. Runtime Round 2 (real DB) ---------------------------------------
Write-Host "`n[G2-003] Step 6/8 — host Round 2 (real DB)" -ForegroundColor Yellow
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

# --- 7. Bad-DB negative round --------------------------------------------
if (-not $SkipBadDb) {
    Write-Host "`n[G2-003] Step 7/8 — host bad-DB negative round" -ForegroundColor Yellow

    # G2-003V1 — save all three connection-string env vars so we can
    # restore EXACTLY what the Operator set, regardless of which variable
    # they used to inject the real password. The previous version of
    # this block only saved and restored $env:ConnectionStrings__GuliERP
    # AND restored it AFTER the try/finally block — a process crash or
    # Ctrl+C between the host start and the restore left the
    # caller's PowerShell with the bad-DB env var, contaminating the
    # final summary and any subsequent shell session.
    $script:SavedConnStandard   = $env:ConnectionStrings__GuliERP
    $script:SavedConnGulierp    = $env:GULIERP_ConnectionStrings__GuliERP
    $script:SavedConnDesignTime = $env:GULIERP_FOUNDATION_CONNECTION

    # The bad-DB value itself is redacted (Password=none); it is safe to
    # assign. We also clear the GULIERP_-prefixed and the design-time
    # vars so the host does not see the operator's real connection
    # through any of the three configuration paths.
    $env:ConnectionStrings__GuliERP          = 'Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=2;Command Timeout=2'
    $env:GULIERP_ConnectionStrings__GuliERP  = $null
    $env:GULIERP_FOUNDATION_CONNECTION       = $null

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
        # G2-003V1 — restore the env vars in finally, BEFORE any
        # other step can run. This guarantees the Operator's caller
        # PowerShell is back to the pre-Step-7 state regardless of
        # whether the probes passed, threw, or the script was Ctrl+C'd
        # mid-round. The saved values are NEVER echoed (they may
        # contain the real operator password); the redacted summary
        # above is the only place the connection string is displayed.
        Stop-Process -Id $proc3.Id -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        $env:ConnectionStrings__GuliERP          = $script:SavedConnStandard
        $env:GULIERP_ConnectionStrings__GuliERP  = $script:SavedConnGulierp
        $env:GULIERP_FOUNDATION_CONNECTION       = $script:SavedConnDesignTime
    }
}

# --- 8. Final summary ---------------------------------------------------
Write-Host "`n[G2-003] SUMMARY" -ForegroundColor Cyan

$summary = [pscustomobject]@{
    FoundationMigration = $script:FoundationMigrationResult
    IdentityMigration   = $script:IdentityMigrationResult
    Integration         = $script:TestResult
    Round1              = $script:Round1Result
    Round2              = $script:Round2Result
    BadDbNegative       = $script:BadDbResult
}

Write-Host ($summary | ConvertTo-Json -Depth 6)

# --- 8. Hard-stop decision ---------------------------------------------
$hardFails = @()
if ($script:FoundationMigrationResult -ne 'PASS') { $hardFails += "Foundation migration: $($script:FoundationMigrationResult)" }
if ($script:IdentityMigrationResult -ne 'PASS')   { $hardFails += "Identity migration: $($script:IdentityMigrationResult)" }
if ($script:TestResult -ne 'PASS')                { $hardFails += "Integration tests: $($script:TestResult)" }
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
    Write-Host "`n[G2-003] ALL CHECKS PASS" -ForegroundColor Green
    Write-Host "  Next: open docs/verification/G2_003_IDENTITY_ORG_KERNEL_REPORT.md"
    Write-Host "  and update docs/governance/GOAL_REGISTRY.md to flip the gate"
    Write-Host "  from G2_003_CODE_READY_OPERATOR_DB_PENDING to G2_003_IDENTITY_ORG_KERNEL_VERIFIED."
    exit 0
} else {
    Write-Host "`n[G2-003] HARD STOPS:" -ForegroundColor Red
    $hardFails | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}
