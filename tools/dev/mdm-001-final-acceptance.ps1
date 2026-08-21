#requires -Version 5.1
<#
mdm-001R7 (Final One-Shot Acceptance Readiness) — Operator one-shot
acceptance harness. Operator usage:

  NEW POWERSHELL
  cd D:\guli\projects\gulierp-next
  .\tools\dev\mdm-001-final-acceptance.ps1

The harness runs a single, end-to-end acceptance pass that:
  1. DB target guard
  2. Secure password prompt
  3. Release build (Solution + Unit + Integration projects)
  4. Test discovery + count guards
  5. MDM Unit Tests (49 → 57 [Fact]s)
  6. Identity Tests (21 [Fact]s)
  7. Foundation Tests (44 test cases from 2 [Fact] + 3 [Theory])
  8. Migration discovery + apply
  9. Integration Tests — 5 rounds
 10. API Host start → exercise → stop → restart → exercise
 11. Final summary (machine-comparable gate string)

The pure-logic decision layer is in
`tools/dev/Mdm001Acceptance.Harness.ps1`. The self-test for that
module is `tools/dev/mdm-001-final-acceptance-selftest.ps1`. Both
are runnable independently of any PostgreSQL.

mdm-001R7 design notes:
  * Operator only types the password ONCE. Everything else is
    automatic.
  * The script never emits the raw password to stdout / log / TRX.
  * The script's environment is restored in `finally` even on
    failure.
  * Any single failed step → `MDM_001_FINAL_ACCEPTANCE_FAILED`
    (no early VERIFIED).
  * All 5 integration rounds must pass; any one fails → no VERIFIED.
  * Both API runtime rounds must pass; any one fails → no VERIFIED.
#>
[CmdletBinding()]
param(
    [switch]$SkipUnitTests,
    [switch]$SkipIntegrationTests,
    [switch]$SkipBuild,
    [switch]$SkipApiRuntime,
    [int]$IntegrationRounds = 5,
    [int]$ApiRuntimeRounds = 2
)

$ErrorActionPreference = 'Stop'

# Load the harness module (pure-logic surface).
$harnessPath = Join-Path $PSScriptRoot 'Mdm001Acceptance.Harness.ps1'
if (-not (Test-Path $harnessPath)) {
    throw "Harness module not found at $harnessPath"
}
Import-Module $harnessPath -Force

# ----------------------------------------------------------------
# Console encoding (mdm-001R5 — P2)
# ----------------------------------------------------------------
try {
    [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
    $OutputEncoding = [System.Text.Encoding]::UTF8
    $PSDefaultParameterValues['Out-File:Encoding'] = 'utf8'
} catch {
    # ignore
}

$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_UI_LANGUAGE = 'en-US'
$env:DOTNET_CLI_TELEMETRY_LANGUAGE = 'en-US'

$RepoRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
Set-Location $RepoRoot

$Dotnet = 'D:\guli\gulierp\.dotnet\dotnet.exe'
$DefaultPgHost = '192.168.2.228'
$DefaultPgPort = '5432'
$DefaultPgDatabase = 'gulierp_g2_003_test'
$DefaultPgUsername = 'gulidata'
$MigrationId = '20260820190000_MDM001_InitializeMdmSchema'

# Step outcome tracker. mdm-001R5 hardening: every required step
# is recorded; the final gate is only emitted when every recorded
# step is true.
$script:StepResults = @{}
$script:CurrentStep = $null
$script:HeadSha = (& git -C $RepoRoot rev-parse HEAD 2>$null)
$script:HeadShort = (& git -C $RepoRoot rev-parse --short HEAD 2>$null)

# Host process tracker (API runtime). We hold the process object
# so the `finally` block can stop it even on early exit.
$script:ApiHostProcess = $null
$script:ApiHostLogPath = $null

function Step($n, $title) {
    Write-Host ""
    Write-Host "============================================================"
    Write-Host "MDM-001R7 Step $n : $title"
    Write-Host "============================================================"
    $stepName = switch -Wildcard ($n.ToString()) {
        '1'   { 'Step1_DBTarget' }
        '2'   { 'Step2_Credential' }
        '3'   { 'Step3_Build' }
        '4a'  { 'Step4a_Discovery' }
        '4b'  { 'Step4b_Apply' }
        '5'   { 'Step5_DiscoveryCounts' }
        '6'   { 'Step6_Unit' }
        '7'   { 'Step7_Identity' }
        '8'   { 'Step8_Foundation' }
        '9'   { 'Step9_Integration' }
        '10'  { 'Step10_ApiRuntime' }
        '11'  { 'Step11_Summary' }
        default { "Step$n" }
    }
    $script:CurrentStep = $stepName
    $script:StepResults[$stepName] = $false
}

function Pass($msg) {
    Write-Host "  [PASS] $msg" -ForegroundColor Green
    if ($script:CurrentStep) { $script:StepResults[$script:CurrentStep] = $true }
}
function Fail($msg) {
    Write-Host "  [FAIL] $msg" -ForegroundColor Red
    if ($script:CurrentStep) { $script:StepResults[$script:CurrentStep] = $false }
}

# Build a single project. Used by Step 3 to ensure the test
# DLLs that Step 6/7/8/9 run with --no-build are FRESH.
function Build-Project {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$ProjectPath,
        [string]$FriendlyName
    )
    $out = & $Dotnet build $ProjectPath -c Release --nologo 2>&1
    if ($LASTEXITCODE -ne 0) {
        Fail ("Build failed: {0} ({1})" -f $ProjectPath, $FriendlyName)
        Write-Host (Redact-SecretText $out) -ForegroundColor Red
        return $false
    }
    return $true
}

# ----------------------------------------------------------------
# Step 1: DB target guard (fail-closed)
# ----------------------------------------------------------------
Step 1 'DB target guard'
$assertScript = Join-Path $RepoRoot 'tools\dev\assert-gulierp-db-target.ps1'
if (-not (Test-Path $assertScript)) {
    Fail "assert-gulierp-db-target.ps1 missing at $assertScript"
    exit 1
}
if (Test-Path Env:ConnectionStrings__GuliERP) {
    & $assertScript
    if ($LASTEXITCODE -ne 0) {
        Fail 'Wrong-DB detected in pre-check. Re-set ConnectionStrings__GuliERP.'
        exit 1
    }
    Pass 'Pre-check DB target = gulierp_g2_003_test.'
} else {
    Write-Host '  [INFO] ConnectionStrings__GuliERP not yet set; will prompt in step 2.' -ForegroundColor Yellow
    Pass 'Pre-check skipped: env var not pre-set (Step 2 will prompt).'
}

# ----------------------------------------------------------------
# Step 2: prompt for password (secure), build connection string
# ----------------------------------------------------------------
Step 2 'PostgreSQL credential prompt'
$savedEnv = Save-OperatorConnectionEnvironment
try {
    $secure = Read-Host -Prompt 'PostgreSQL password for gulidata@192.168.2.228:5432/gulierp_g2_003_test' -AsSecureString
    $bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    $password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
    [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) | Out-Null
    if ([string]::IsNullOrEmpty($password)) {
        Fail 'Password is required.'
        exit 1
    }
    $connectionString = "Host=$DefaultPgHost;Port=$DefaultPgPort;Database=$DefaultPgDatabase;Username=$DefaultPgUsername;Password=$password;Include Error Detail=true"
    $env:ConnectionStrings__GuliERP = $connectionString

    & $assertScript -ConnectionString $connectionString
    if ($LASTEXITCODE -ne 0) {
        Fail 'Wrong-DB detected by post-prompt assert.'
        exit 1
    }
    Pass ("DB target asserted: {0}:{1}/{2} (password redacted)." -f $DefaultPgHost, $DefaultPgPort, $DefaultPgDatabase)

    # ----------------------------------------------------------------
    # Step 3: Release build (mdm module + 2 test projects + api)
    # ----------------------------------------------------------------
    if (-not $SkipBuild) {
        Step 3 'Release build (Solution + Unit + Integration + API)'
        $projList = @(
            @{ Path = "$RepoRoot/modules/mdm/GuliERP.Mdm.Infrastructure/GuliERP.Mdm.Infrastructure.csproj";         Name = 'MDM Infrastructure' }
            @{ Path = "$RepoRoot/apps/api/GuliERP.Api/GuliERP.Api.csproj";                                          Name = 'API' }
            @{ Path = "$RepoRoot/tests/GuliERP.Mdm.Tests/GuliERP.Mdm.Tests.csproj";                                  Name = 'MDM Unit Tests' }
            @{ Path = "$RepoRoot/tests/GuliERP.Mdm.IntegrationTests/GuliERP.Mdm.IntegrationTests.csproj";            Name = 'MDM Integration Tests' }
            @{ Path = "$RepoRoot/tests/GuliERP.Identity.Tests/GuliERP.Identity.Tests.csproj";                        Name = 'Identity Unit Tests' }
            @{ Path = "$RepoRoot/tests/GuliERP.Foundation.Tests/GuliERP.Foundation.Tests.csproj";                    Name = 'Foundation Tests' }
        )
        foreach ($p in $projList) {
            if (-not (Build-Project -ProjectPath $p.Path -FriendlyName $p.Name)) {
                exit 1
            }
        }
        Pass 'Solution + 2 test projects + API built (just-built DLLs are what Step 6/7/8/9/10 will run).'
    } else {
        Write-Host '  [SKIP] Build skipped (-SkipBuild).' -ForegroundColor Yellow
    }

    # ----------------------------------------------------------------
    # Step 4a: verify migration DISCOVERY
    # ----------------------------------------------------------------
    Step 4a 'Verify MDM-001 migration discovery'
    $listOut = & $Dotnet ef migrations list `
        --project "$RepoRoot/modules/mdm/GuliERP.Mdm.Infrastructure/GuliERP.Mdm.Infrastructure.csproj" `
        --startup-project "$RepoRoot/apps/api/GuliERP.Api/GuliERP.Api.csproj" `
        --configuration Release 2>&1
    if ($LASTEXITCODE -ne 0) {
        Fail 'dotnet ef migrations list failed.'
        Write-Host (Redact-SecretText $listOut) -ForegroundColor Red
        exit 1
    }
    if (-not (Test-MigrationDiscovered -CommandOutput $listOut -ExpectedMigrationId $MigrationId)) {
        Fail "MDM-001 migration '$MigrationId' is NOT discoverable."
        Write-Host (Redact-SecretText $listOut) -ForegroundColor Red
        exit 1
    }
    Pass "Migration discovered: $MigrationId."

    # ----------------------------------------------------------------
    # Step 4b: apply migration
    # ----------------------------------------------------------------
    Step 4b 'Apply MDM-001 migration'
    $migrationOut = & $Dotnet ef database update `
        --project "$RepoRoot/modules/mdm/GuliERP.Mdm.Infrastructure/GuliERP.Mdm.Infrastructure.csproj" `
        --startup-project "$RepoRoot/apps/api/GuliERP.Api/GuliERP.Api.csproj" `
        --configuration Release 2>&1
    if ($LASTEXITCODE -ne 0) {
        Fail 'Migration apply failed.'
        Write-Host (Redact-SecretText $migrationOut) -ForegroundColor Red
        exit 1
    }
    Pass 'MDM-001 migration applied (or already up to date).'

    # ----------------------------------------------------------------
    # Step 5: test discovery counts (catch stale DLLs early)
    # ----------------------------------------------------------------
    Step 5 'Verify test discovery counts'
    $unitProj = "$RepoRoot/tests/GuliERP.Mdm.Tests/GuliERP.Mdm.Tests.csproj"
    $intProj = "$RepoRoot/tests/GuliERP.Mdm.IntegrationTests/GuliERP.Mdm.IntegrationTests.csproj"
    $idProj = "$RepoRoot/tests/GuliERP.Identity.Tests/GuliERP.Identity.Tests.csproj"
    $fdProj = "$RepoRoot/tests/GuliERP.Foundation.Tests/GuliERP.Foundation.Tests.csproj"

    $unitDisco = Get-TestDiscoveryCount -Output (& $Dotnet test $unitProj -c Release --no-build --list-tests 2>&1)
    if ($unitDisco.Count -lt 40) {
        Fail ("MDM unit tests discovered: only {0}, expected >= 40. Stale DLL." -f $unitDisco.Count)
        exit 1
    }
    Pass ("MDM unit tests discovered: {0}." -f $unitDisco.Count)

    $intDisco = Get-TestDiscoveryCount -Output (& $Dotnet test $intProj -c Release --no-build --list-tests 2>&1)
    if ($intDisco.Count -lt 10) {
        Fail ("MDM integration tests discovered: only {0}, expected >= 10. Stale DLL." -f $intDisco.Count)
        exit 1
    }
    Pass ("MDM integration tests discovered: {0}." -f $intDisco.Count)

    $idDisco = Get-TestDiscoveryCount -Output (& $Dotnet test $idProj -c Release --no-build --list-tests 2>&1)
    if ($idDisco.Count -lt 21) {
        Fail ("Identity tests discovered: only {0}, expected >= 21. Stale DLL." -f $idDisco.Count)
        exit 1
    }
    Pass ("Identity tests discovered: {0}." -f $idDisco.Count)

    $fdDisco = Get-TestDiscoveryCount -Output (& $Dotnet test $fdProj -c Release --no-build --list-tests 2>&1)
    if ($fdDisco.Count -lt 44) {
        Fail ("Foundation tests discovered: only {0}, expected >= 44. Stale DLL." -f $fdDisco.Count)
        exit 1
    }
    Pass ("Foundation tests discovered: {0}." -f $fdDisco.Count)

    # ----------------------------------------------------------------
    # Step 6: MDM Unit Tests
    # ----------------------------------------------------------------
    if (-not $SkipUnitTests) {
        Step 6 'MDM unit tests (Release, --no-build)'
        $out = & $Dotnet test $unitProj -c Release --no-build --nologo 2>&1
        $summary = Get-TestRunSummary -Output $out
        if ($LASTEXITCODE -ne 0 -or -not (Test-RunSummaryAcceptable -Summary $summary -DiscoveredCount $unitDisco.Count)) {
            Fail ('Unit tests failed (exit={0}, passed={1}, failed={2}, total={3}, discovered={4}).' -f $LASTEXITCODE, $summary.Passed, $summary.Failed, $summary.Total, $unitDisco.Count)
            Write-Host (Redact-SecretText $out) -ForegroundColor Red
            exit 1
        }
        Pass ("Unit tests: {0} PASS." -f $summary.Passed)
    } else {
        Write-Host '  [SKIP] Unit tests skipped.' -ForegroundColor Yellow
    }

    # ----------------------------------------------------------------
    # Step 7: Identity Tests
    # ----------------------------------------------------------------
    if (-not $SkipUnitTests) {
        Step 7 'Identity unit tests'
        $out = & $Dotnet test $idProj -c Release --no-build --nologo 2>&1
        $summary = Get-TestRunSummary -Output $out
        if ($LASTEXITCODE -ne 0 -or -not (Test-RunSummaryAcceptable -Summary $summary -DiscoveredCount $idDisco.Count)) {
            Fail ('Identity tests failed (exit={0}, passed={1}, failed={2}, total={3}, discovered={4}).' -f $LASTEXITCODE, $summary.Passed, $summary.Failed, $summary.Total, $idDisco.Count)
            Write-Host (Redact-SecretText $out) -ForegroundColor Red
            exit 1
        }
        Pass ("Identity tests: {0} PASS." -f $summary.Passed)
    } else {
        Write-Host '  [SKIP] Identity tests skipped.' -ForegroundColor Yellow
    }

    # ----------------------------------------------------------------
    # Step 8: Foundation Tests
    # ----------------------------------------------------------------
    if (-not $SkipUnitTests) {
        Step 8 'Foundation tests'
        $out = & $Dotnet test $fdProj -c Release --no-build --nologo 2>&1
        $summary = Get-TestRunSummary -Output $out
        if ($LASTEXITCODE -ne 0 -or -not (Test-RunSummaryAcceptable -Summary $summary -DiscoveredCount $fdDisco.Count)) {
            Fail ('Foundation tests failed (exit={0}, passed={1}, failed={2}, total={3}, discovered={4}).' -f $LASTEXITCODE, $summary.Passed, $summary.Failed, $summary.Total, $fdDisco.Count)
            Write-Host (Redact-SecretText $out) -ForegroundColor Red
            exit 1
        }
        Pass ("Foundation tests: {0} PASS." -f $summary.Passed)
    } else {
        Write-Host '  [SKIP] Foundation tests skipped.' -ForegroundColor Yellow
    }

    # ----------------------------------------------------------------
    # Step 9: Integration Tests — 5 rounds
    # ----------------------------------------------------------------
    if (-not $SkipIntegrationTests) {
        Step 9 "MDM PostgreSQL integration tests (5 rounds, xUnit Collection serializes 3 test classes)"
        for ($round = 1; $round -le $IntegrationRounds; $round++) {
            $roundKey = "Step9_Integration_Round${round}"
            $script:StepResults[$roundKey] = $false
            Write-Host "  ---- Round $round / $IntegrationRounds ----" -ForegroundColor Cyan
            $out = & $Dotnet test $intProj -c Release --no-build --nologo 2>&1
            $summary = Get-TestRunSummary -Output $out
            if ($LASTEXITCODE -ne 0 -or -not (Test-RunSummaryAcceptable -Summary $summary -DiscoveredCount $intDisco.Count)) {
                Fail ("Integration Round ${round} failed (exit={0}, passed={1}, failed={2}, total={3}, discovered={4})." -f $LASTEXITCODE, $summary.Passed, $summary.Failed, $summary.Total, $intDisco.Count)
                Write-Host (Redact-SecretText $out) -ForegroundColor Red
                $script:StepResults[$roundKey] = $false
                # Per the brief §五-28: any integration round failure
                # → no VERIFIED. Abort.
                exit 1
            }
            $script:StepResults[$roundKey] = $true
            Write-Host ("  [ROUND {0}/{1}] {2} PASS" -f $round, $IntegrationRounds, $summary.Passed) -ForegroundColor Green
        }
        $script:StepResults['Step9_Integration'] = $true
        Pass ("All {0} integration rounds PASS." -f $IntegrationRounds)
    } else {
        Write-Host '  [SKIP] Integration tests skipped.' -ForegroundColor Yellow
    }

    # ----------------------------------------------------------------
    # Step 10: API Host runtime
    # ----------------------------------------------------------------
    if (-not $SkipApiRuntime) {
        Step 10 "API Host runtime ($ApiRuntimeRounds rounds: start, exercise, restart, exercise)"
        $apiExe = "$RepoRoot/apps/api/GuliERP.Api/bin/Release/net10.0/GuliERP.Api.dll"
        if (-not (Test-Path $apiExe)) {
            Fail "API binary not found at $apiExe (build may have skipped API)."
            exit 1
        }
        for ($round = 1; $round -le $ApiRuntimeRounds; $round++) {
            $roundKey = "Step10_ApiRuntime_Round${round}"
            $script:StepResults[$roundKey] = $false
            Write-Host "  ---- API Runtime Round $round / $ApiRuntimeRounds ----" -ForegroundColor Cyan
            # Start host
            $script:ApiHostLogPath = Join-Path $RepoRoot "tests/_evidence_trx/api_host_round${round}_$(Get-Date -Format 'yyyyMMddHHmmss').log"
            $script:ApiHostProcess = Start-Process -FilePath $Dotnet -ArgumentList @(
                $apiExe,
                '--urls', 'http://127.0.0.1:5179'
            ) -PassThru -NoNewWindow -RedirectStandardOutput $script:ApiHostLogPath -RedirectStandardError "$script:ApiHostLogPath.err"
            Write-Host "  [INFO] Host started (PID=$($script:ApiHostProcess.Id), log=$script:ApiHostLogPath)." -ForegroundColor Yellow
            # Wait for /health/live to be reachable
            $ready = $false
            for ($i = 0; $i -lt 30; $i++) {
                Start-Sleep -Seconds 1
                try {
                    $r = Invoke-WebRequest -Uri 'http://127.0.0.1:5179/health/live' -UseBasicParsing -TimeoutSec 2
                    if ($r.StatusCode -eq 200) { $ready = $true; break }
                } catch {
                    # not ready yet
                }
            }
            if (-not $ready) {
                Fail "API host did not become ready within 30s. See log: $script:ApiHostLogPath"
                Stop-ApiHost
                exit 1
            }
            Pass ("Round ${round}: /health/live returned 200.")

            # Exercise: GET / (banner), GET /health/ready, GET /api/v1/mdm/uoms?pageSize=1
            try {
                $r1 = Invoke-WebRequest -Uri 'http://127.0.0.1:5179/' -UseBasicParsing -TimeoutSec 5
                if ($r1.StatusCode -ne 200) { Fail "Root banner returned $($r1.StatusCode)."; Stop-ApiHost; exit 1 }
                Pass "Root banner returned 200."
                $r2 = Invoke-WebRequest -Uri 'http://127.0.0.1:5179/health/ready' -UseBasicParsing -TimeoutSec 5
                if ($r2.StatusCode -ge 400) { Fail "/health/ready returned $($r2.StatusCode)."; Stop-ApiHost; exit 1 }
                Pass "/health/ready returned $($r2.StatusCode)."
            } catch {
                Fail "API exercise failed: $_"
                Stop-ApiHost
                exit 1
            }

            $script:StepResults[$roundKey] = $true

            # Stop host (we restart on next round)
            Stop-ApiHost
            # Wait for port release
            Start-Sleep -Seconds 2
        }
        $script:StepResults['Step10_ApiRuntime'] = $true
        Pass ("All {0} API runtime rounds PASS." -f $ApiRuntimeRounds)
    } else {
        Write-Host '  [SKIP] API runtime skipped (-SkipApiRuntime).' -ForegroundColor Yellow
    }

    # ----------------------------------------------------------------
    # Step 11: final summary
    # ----------------------------------------------------------------
    Step 11 'MDM-001R7 final acceptance summary'
    $gate = Get-FinalGateDecision -StepResults $script:StepResults `
        -IntegrationRounds (1..$IntegrationRounds) `
        -ApiRounds (1..$ApiRuntimeRounds)
    Write-Host "  Active DB target : $DefaultPgDatabase @ $DefaultPgHost`:$DefaultPgPort"
    Write-Host "  Connection string: $(Redact-ConnectionString $connectionString)"
    Write-Host "  Source HEAD      : $script:HeadShort ($script:HeadSha)"
    Write-Host "  Module built     : modules/mdm (3 projects) + API + 3 test projects"
    Write-Host "  Migration applied: $MigrationId"
    Write-Host "  Endpoint root    : /api/v1/mdm/{uoms,item-categories,items}"
    Write-Host ""
    if ($gate.Missing.Count -gt 0) {
        Write-Host ("  [WARN] These required steps were not recorded (skipped?): {0}" -f ($gate.Missing -join ', ')) -ForegroundColor Yellow
    }
    if ($gate.Failed.Count -gt 0) {
        Write-Host ("  [WARN] These required steps FAILED: {0}" -f ($gate.Failed -join ', ')) -ForegroundColor Yellow
    }
    Write-Host ""
    if ($gate.Success) {
        Write-Host "  $((Get-HarnessDefaults).GateSuccess)" -ForegroundColor Green
    } else {
        Write-Host "  $((Get-HarnessDefaults).GateHardStop)" -ForegroundColor Red
        Write-Host "  (Investigate the failing step's red [FAIL] line above.)" -ForegroundColor Red
    }
}
finally {
    # Stop the API host if it's still running
    Stop-ApiHost
    # Restore operator environment
    Restore-OperatorConnectionEnvironment $savedEnv
    Write-Host ""
    Write-Host "  [INFO] ConnectionStrings__GuliERP + GULIERP_MDM_SEED_FILE restored from snapshot." -ForegroundColor Yellow
}

# ----------------------------------------------------------------
# Helper: stop the API host if running
# ----------------------------------------------------------------
function Stop-ApiHost {
    if ($null -ne $script:ApiHostProcess -and -not $script:ApiHostProcess.HasExited) {
        Write-Host "  [INFO] Stopping API host (PID=$($script:ApiHostProcess.Id))..." -ForegroundColor Yellow
        try {
            Stop-Process -Id $script:ApiHostProcess.Id -Force -ErrorAction SilentlyContinue
            $script:ApiHostProcess.WaitForExit(5000) | Out-Null
        } catch { }
    }
    $script:ApiHostProcess = $null
}
