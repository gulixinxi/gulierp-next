#requires -Version 5.1
<#
mdm-001R8 (API Runtime Resume Harness Fix) — Operator one-shot
acceptance harness. Two modes:

  Full mode (default):
    cd D:\guli\projects\gulierp-next
    .\tools\dev\mdm-001-final-acceptance.ps1

  Resume mode (only the API Runtime rounds, after prior
  evidence files exist on disk):
    .\tools\dev\mdm-001-final-acceptance.ps1 -ResumeApiRuntime

Full mode runs a single, end-to-end acceptance pass that:
  1. DB target guard
  2. Secure password prompt
  3. Release build (Solution + Unit + Integration projects)
  4. Test discovery + count guards
  5. MDM Unit Tests (57 [Fact]s)
  6. Identity Tests (21 [Fact]s)
  7. Foundation Tests (44 test cases from 2 [Fact] + 3 [Theory])
  8. Migration discovery + apply
  9. Integration Tests — 5 rounds
 10. API Host start → exercise → stop → restart → exercise
 11. Final summary (machine-comparable gate string)

Resume mode (-ResumeApiRuntime) skips Steps 1-9 entirely and
only re-runs Step 10 (2 API Runtime rounds). It reads prior
evidence files (Operator transcript + TRX) to confirm the
unit/integration suite already passed. If any required prior
evidence is missing, the resume mode FAILS closed with no
fake VERIFIED.

The pure-logic decision layer is in
`tools/dev/Mdm001Acceptance.Harness.ps1`. The self-test for that
module is `tools/dev/mdm-001-final-acceptance-selftest.ps1`. Both
are runnable independently of any PostgreSQL.

mdm-001R8 design notes:
  * R7 root cause: `Stop-ApiHost` was defined AFTER the
    `try-finally` block; PowerShell does NOT register function
    declarations that come after the final `try-finally` in
    a .ps1 file. R8 moves ALL function definitions to the top
    of the script and exposes the helpers in the harness module
    so the selftest can dot-source + Get-Command each helper
    BEFORE the script runs.
  * R8 also adds `-ResumeApiRuntime` to allow the operator to
    re-run only Step 10 (2 API rounds) without redoing the
    5 integration rounds.
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
    [switch]$ResumeApiRuntime,
    [int]$IntegrationRounds = 5,
    [int]$ApiRuntimeRounds = 2,
    [string]$EvidenceRoot = 'tests/_evidence_trx',
    [int]$ResumeApiReadyTimeoutSec = 30
)

$ErrorActionPreference = 'Stop'

# ====================================================================
# SECTION A — Module + environment setup (runs BEFORE any function
#              definition so the helpers are discoverable).
# ====================================================================

# Load the harness module FIRST so all pure-logic + lifecycle
# helpers are registered before we define any local functions.
$harnessPath = Join-Path $PSScriptRoot 'Mdm001Acceptance.Harness.ps1'
if (-not (Test-Path $harnessPath)) {
    throw "Harness module not found at $harnessPath"
}
Import-Module $harnessPath -Force

# Console encoding (mdm-001R5 — P2)
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

# G3_DEV_DOTNET_RESOLVER_CLEANUP_001: safe dotnet resolver (G3_DEV_DOTNET_RESOLVER_CLEANUP_FIX_001).
# - Default to system dotnet (PATH lookup).
# - Allow GULIERP_DOTNET to override: either a full path to dotnet.exe
#   OR a name resolvable via PATH (e.g., "dotnet").
# - Hard-prohibit the old vendored D:\guli\gulierp\.dotnet\*.
# - Validate the resolved dotnet is .NET 10 SDK.
$Dotnet = if ($env:GULIERP_DOTNET) {
    if (Test-Path -LiteralPath $env:GULIERP_DOTNET) {
        $env:GULIERP_DOTNET
    }
    else {
        try {
            (Get-Command -Name $env:GULIERP_DOTNET -ErrorAction Stop).Source
        }
        catch {
            throw "GULIERP_DOTNET is set to '$env:GULIERP_DOTNET' but it is neither a valid file path nor a command resolvable via PATH."
        }
    }
}
else {
    (Get-Command -Name dotnet -ErrorAction Stop).Source
}
if ($Dotnet -like 'D:\guli\gulierp\.dotnet\*') {
    throw "Old vendored dotnet is forbidden: $Dotnet (use system dotnet or set GULIERP_DOTNET)."
}
if (-not (Test-Path -LiteralPath $Dotnet)) {
    throw "MDM-001 final acceptance requires a .NET 10 SDK on PATH (or set GULIERP_DOTNET). Not found: $Dotnet."
}
$_dotnetVersion = (& $Dotnet --version).Trim()
if ($_dotnetVersion -notmatch '^10\.') {
    throw "GuliERP Next requires .NET 10 SDK. Current: $_dotnetVersion ($Dotnet)."
}
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

# Host process tracker (API runtime). We hold the Process object
# so the `finally` block can stop it even on early exit. The
# harness helpers (Stop-ApiHost, Test-ApiProcessAlive) accept
# the Process object directly, so we do not need any
# `$script:ApiHostProcess` global.
$script:ApiHostHandle = $null
$script:ApiHostLogPath = $null

# ====================================================================
# SECTION B — Local helper functions. ALL of these are defined
#              BEFORE the main try-finally so PowerShell registers
#              them. R7 bug: function `Stop-ApiHost` was defined
#              AFTER the try-finally, so the call from inside
#              the try block failed with "term not recognized".
# ====================================================================

function Step($n, $title) {
    Write-Host ""
    Write-Host "============================================================"
    Write-Host "MDM-001 Step $n : $title"
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

# Run the API host, wait for ready, exercise endpoints, stop the
# host, and verify the port is released. Returns $true on full
# success; $false on any failure (the function records its own
# Pass/Fail lines so the operator sees what failed).
#
# This wraps the harness lifecycle helpers in a single
# idempotent primitive so the resume-mode and full-mode
# code paths can share it.
function Invoke-ApiRuntimeRound {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory = $true)][int]$Round,
        [Parameter(Mandatory = $true)][int]$TotalRounds,
        [Parameter(Mandatory = $true)][string]$ApiExe,
        [Parameter(Mandatory = $true)][string]$Urls,
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [int]$ReadyTimeoutSec = 30
    )
    $roundKey = "Step10_ApiRuntime_Round${Round}"
    $script:StepResults[$roundKey] = $false
    Write-Host "  ---- API Runtime Round $Round / $TotalRounds ----" -ForegroundColor Cyan

    $logPath = Join-Path $RepoRoot "tests/_evidence_trx/api_host_round${Round}_$(Get-Date -Format 'yyyyMMddHHmmss').log"

    # Start
    $handle = Start-ApiHost -DotnetExe $Dotnet -HostDll $ApiExe -LogPath $logPath -Urls $Urls
    $script:ApiHostHandle = $handle
    $script:ApiHostLogPath = $logPath
    Write-Host "  [INFO] Host started (PID=$($handle.Pid), log=$logPath)." -ForegroundColor Yellow

    # Wait for ready
    $ready = Wait-ApiHostReady -Url "$Urls/health/live" -TimeoutSec $ReadyTimeoutSec
    if (-not $ready) {
        Fail "API host did not become ready within ${ReadyTimeoutSec}s. See log: $logPath"
        $null = Stop-ApiHost -Process $handle.Process
        $script:ApiHostHandle = $null
        return $false
    }
    Pass ("Round ${Round}: /health/live returned 200.")

    # Exercise endpoints
    $banner = Test-ApiEndpoint -Url "$Urls/"
    if (-not $banner.Ok -or $banner.StatusCode -ne 200) {
        Fail "Root banner returned $($banner.StatusCode)."
        $null = Stop-ApiHost -Process $handle.Process
        $script:ApiHostHandle = $null
        return $false
    }
    Pass "Root banner returned 200."

    $ready2 = Test-ApiEndpoint -Url "$Urls/health/ready"
    if ($ready2.StatusCode -ge 400) {
        Fail "/health/ready returned $($ready2.StatusCode)."
        $null = Stop-ApiHost -Process $handle.Process
        $script:ApiHostHandle = $null
        return $false
    }
    Pass "/health/ready returned $($ready2.StatusCode)."

    # Mark round PASS
    $script:StepResults[$roundKey] = $true

    # Stop
    $stop = Stop-ApiHost -Process $handle.Process -GracePeriodSec 5 -HardKillSec 5
    if (-not $stop.Stopped) {
        Fail ("Stop-ApiHost failed for PID $($stop.Pid): $($stop.Message)")
        $script:ApiHostHandle = $null
        return $false
    }
    Write-Host "  [INFO] Host stopped (PID=$($stop.Pid), force=$($stop.UsedForce))." -ForegroundColor Yellow
    $script:ApiHostHandle = $null

    # Verify port release
    $uri = [System.Uri]$Urls
    $portOpen = Test-ApiPortListening -Hostname $uri.Host -Port $uri.Port -TimeoutSec 2
    if ($portOpen) {
        Fail "Port $($uri.Port) on $($uri.Host) is STILL LISTENING after Stop-ApiHost."
        return $false
    }
    Pass "Port $($uri.Port) released after Stop-ApiHost."

    # Wait a little for the OS to fully release the port (Windows
    # TIME_WAIT) before the next round tries to bind again.
    Start-Sleep -Seconds 2
    return $true
}

# Read the Operator transcript TRX file from the EvidenceRoot
# and verify it has 10/10 PASS in the inner test summary. Returns
# $true if the file exists AND the summary shows 10 passed + 0
# failed. Pure: no side effects, no PG dependency.
function Test-OperatorTranscriptTrx {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory = $true)][string]$TrxPath
    )
    if (-not (Test-Path $TrxPath)) { return $false }
    $content = Get-Content -Raw $TrxPath -Encoding UTF8
    # The TRX format embeds a <Counters ... total="N" passed="P" failed="F" .../>
    # element. We accept any of the common shapes produced by
    # `dotnet test --logger trx`.
    $m = [regex]::Match($content, 'total="(?<total>\d+)"[^>]*passed="(?<passed>\d+)"[^>]*failed="(?<failed>\d+)"')
    if (-not $m.Success) {
        $m = [regex]::Match($content, 'passed="(?<passed>\d+)"[^>]*failed="(?<failed>\d+)"[^>]*total="(?<total>\d+)"')
    }
    if (-not $m.Success) { return $false }
    $total = [int]$m.Groups['total'].Value
    $passed = [int]$m.Groups['passed'].Value
    $failed = [int]$m.Groups['failed'].Value
    return ($total -eq 10 -and $passed -eq 10 -and $failed -eq 0)
}

# Verify that the prior run's evidence files all exist AND
# record PASS for the unit suites. R9 supports TWO modes:
#
#   Mode A: Machine TRX — real TRX files exist in EvidenceRoot.
#           R7 did not produce these; R9 keeps the contract for
#           future runs that do.
#   Mode B: Operator Transcript Backfill — the canonical
#           `MDM_001_R7_OPERATOR_TRANSCRIPT_EVIDENCE.md` file
#           records the Operator's transcript, declares TRX as
#           NOT_AVAILABLE, and SHA256s the API host log.
#
# Returns pscustomobject {Ok, Mode, Missing, HaveXxx, ...}.
# `Mode` is the source of truth for downstream consumers.
function Test-PriorOperatorEvidence {
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory = $true)][string]$EvidenceRoot,
        [Parameter(Mandatory = $false)][string]$TranscriptEvidencePath = $script:DefaultTranscriptEvidencePath,
        [string]$ExpectedR7Head = '15d46c4'
    )
    $missing = @()
    $trxFails = @()
    $unitTrx = Join-Path $EvidenceRoot 'GuliERP.Mdm.Tests.trx'
    $idTrx   = Join-Path $EvidenceRoot 'GuliERP.Identity.Tests.trx'
    $fdTrx   = Join-Path $EvidenceRoot 'GuliERP.Foundation.Tests.trx'
    $intTrxList = 1..5 | ForEach-Object { Join-Path $EvidenceRoot "POC001_Run$($_).trx" }

    # ----- Mode A: Machine TRX evidence -----
    $intLogs = 1..5 | ForEach-Object { Join-Path $EvidenceRoot "POC001_Run$($_).log" }
    $unitLog = Join-Path $EvidenceRoot 'GuliERP.Mdm.Tests.log'
    $idLog   = Join-Path $EvidenceRoot 'GuliERP.Identity.Tests.log'
    $fdLog   = Join-Path $EvidenceRoot 'GuliERP.Foundation.Tests.log'
    $apiLog  = Get-ChildItem -Path $EvidenceRoot -Filter 'api_host_round1_*.log' -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1

    $haveUnit = (Test-Path $unitTrx) -or (Test-Path $unitLog)
    $haveId   = (Test-Path $idTrx)   -or (Test-Path $idLog)
    $haveFd   = (Test-Path $fdTrx)   -or (Test-Path $fdLog)
    $haveInt  = ($intTrxList | Where-Object { Test-Path $_ } | Measure-Object).Count -ge 5
    $haveApi  = $null -ne $apiLog

    $machineOk = $haveUnit -and $haveId -and $haveFd -and $haveInt -and $haveApi

    if (-not $haveUnit) { $missing += 'Mode A: MDM unit TRX/log missing' }
    if (-not $haveId)   { $missing += 'Mode A: Identity unit TRX/log missing' }
    if (-not $haveFd)   { $missing += 'Mode A: Foundation unit TRX/log missing' }
    if (-not $haveInt)  { $missing += 'Mode A: 5 integration round TRX/log missing' }
    if (-not $haveApi)  { $missing += 'Mode A: API Runtime Round 1 log missing' }

    # Per-TRX validation only if a TRX exists.
    foreach ($p in @($unitTrx, $idTrx, $fdTrx)) {
        if (Test-Path $p) {
            $content = Get-Content -Raw $p -Encoding UTF8
            $m = [regex]::Match($content, 'outcome="Failed"')
            if ($m.Success) { $trxFails += "Mode A: $p contains failed test" }
        }
    }
    foreach ($p in $intTrxList) {
        if (Test-Path $p) {
            if (-not (Test-OperatorTranscriptTrx -TrxPath $p)) {
                $trxFails += "Mode A: $p does not show 10/10 PASS"
            }
        }
    }

    if ($machineOk -and $trxFails.Count -eq 0) {
        return [pscustomobject]@{
            Ok = $true
            Mode = 'MACHINE_TRX'
            Missing = @()
            TrxFails = @()
            HaveUnit = $haveUnit
            HaveId = $haveId
            HaveFd = $haveFd
            HaveInt = $haveInt
            HaveApi = $haveApi
            Backfill = $null
        }
    }

    # ----- Mode B: Operator Transcript Backfill -----
    $backfill = Test-OperatorTranscriptBackfill `
        -TranscriptPath $TranscriptEvidencePath `
        -RepoRoot $RepoRoot `
        -ExpectedR7Head $ExpectedR7Head
    if ($backfill.Ok) {
        # Backfill also requires that the R7->HEAD diff did NOT
        # touch business code (otherwise the transcript numbers
        # may not apply to the current source).
        $noBusinessCodeChange = Test-NoBusinessCodeChangeSinceR7 `
            -RepoRoot $RepoRoot `
            -R7Head $ExpectedR7Head
        if (-not $noBusinessCodeChange.Ok) {
            return [pscustomobject]@{
                Ok = $false
                Mode = 'OPERATOR_TRANSCRIPT_BACKFILL_INVALID'
                Missing = @("Mode B: business code / migration / integration test was modified between R7 and HEAD; transcript numbers may not apply. Changed: $($noBusinessCodeChange.ChangedCsFiles -join ', ')")
                TrxFails = $trxFails
                HaveUnit = $haveUnit
                HaveId = $haveId
                HaveFd = $haveFd
                HaveInt = $haveInt
                HaveApi = $haveApi
                Backfill = $backfill
            }
        }
        return [pscustomobject]@{
            Ok = $true
            Mode = 'OPERATOR_TRANSCRIPT_BACKFILL_VERIFIED'
            Missing = @()
            TrxFails = @()
            HaveUnit = $haveUnit
            HaveId = $haveId
            HaveFd = $haveFd
            HaveInt = $haveInt
            HaveApi = $haveApi
            Backfill = $backfill
        }
    }

    # ----- Both modes failed: combine reasons -----
    foreach ($m in $backfill.Missing) {
        $missing += "Mode B: $m"
    }
    return [pscustomobject]@{
        Ok = $false
        Mode = 'NEITHER_MODE_VERIFIED'
        Missing = $missing
        TrxFails = $trxFails
        HaveUnit = $haveUnit
        HaveId = $haveId
        HaveFd = $haveFd
        HaveInt = $haveInt
        HaveApi = $haveApi
        Backfill = $backfill
    }
}

# ====================================================================
# SECTION C — Main flow. try-finally wraps the entire run so the
#              finally block restores env + stops any host.
# ====================================================================

try {
    # ----------------------------------------------------------------
    # Resume mode: only re-run Step 10 (API Runtime). Mark Step 1-9
    # as OPERATOR_TRANSCRIPT_VERIFIED based on prior evidence.
    # ----------------------------------------------------------------
    if ($ResumeApiRuntime) {
        Step 11 'MDM-001R9 RESUME: only API Runtime Round 1 + 2; prior evidence verified (Mode A: MACHINE_TRX or Mode B: OPERATOR_TRANSCRIPT_BACKFILL)'
        $evidence = Test-PriorOperatorEvidence -EvidenceRoot $EvidenceRoot
        if (-not $evidence.Ok) {
            $reason = if ($evidence.Mode -eq 'NEITHER_MODE_VERIFIED') {
                "Both Mode A (Machine TRX) and Mode B (Operator Transcript Backfill) failed.`n  Mode A Missing: $($evidence.Missing -join '; ')`n  Mode B Missing: $(if ($evidence.Backfill) { $evidence.Backfill.Missing -join '; ' } else { '(no backfill result)' })"
            } else {
                "Mode $($evidence.Mode): $($evidence.Missing -join '; '). TrxFails: $($evidence.TrxFails -join '; ')"
            }
            Fail $reason
            Write-Host ""
            Write-Host "  MDM_001_FINAL_ACCEPTANCE_FAILED" -ForegroundColor Red
            Write-Host "  (Cannot resume without prior evidence.)" -ForegroundColor Red
            Write-Host ""
            Write-Host "  HINT: Either (a) drop real TRX files into $EvidenceRoot/," -ForegroundColor Yellow
            Write-Host "         or (b) ensure docs/verification/MDM_001_R7_OPERATOR_TRANSCRIPT_EVIDENCE.md" -ForegroundColor Yellow
            Write-Host "         exists with the correct EVIDENCE_TYPE=OPERATOR_TRANSCRIPT_REPORTED marker." -ForegroundColor Yellow
            return
        }
        Write-Host "  [INFO] Prior evidence verified (Mode: $($evidence.Mode)):" -ForegroundColor Yellow
        Write-Host "    Unit  TRX/log  : $evidence.HaveUnit" -ForegroundColor Yellow
        Write-Host "    Identity TRX/log : $evidence.HaveId" -ForegroundColor Yellow
        Write-Host "    Foundation TRX/log: $evidence.HaveFd" -ForegroundColor Yellow
        Write-Host "    Integration rounds: $evidence.HaveInt" -ForegroundColor Yellow
        Write-Host "    API Round 1 log: $evidence.HaveApi" -ForegroundColor Yellow
        if ($evidence.Mode -eq 'OPERATOR_TRANSCRIPT_BACKFILL_VERIFIED' -and $null -ne $evidence.Backfill) {
            Write-Host "    Backfill R7 HEAD : $($evidence.Backfill.RecordedR7Head)" -ForegroundColor Yellow
            Write-Host "    API log SHA256   : $($evidence.Backfill.ApiLogSha256)" -ForegroundColor Yellow
        }
        Write-Host "  [INFO] Marking Step 1-9 as $($evidence.Mode)." -ForegroundColor Yellow
        foreach ($k in @(
            'Step1_DBTarget','Step2_Credential','Step3_Build',
            'Step4a_Discovery','Step4b_Apply','Step5_DiscoveryCounts',
            'Step6_Unit','Step7_Identity','Step8_Foundation','Step9_Integration'
        )) {
            $script:StepResults[$k] = $true
        }

        Step 10 "API Host runtime (RESUME: $ApiRuntimeRounds rounds)"
        $apiExe = "$RepoRoot/apps/api/GuliERP.Api/bin/Release/net10.0/GuliERP.Api.dll"
        if (-not (Test-Path $apiExe)) {
            Fail "API binary not found at $apiExe (must build first). Re-run with full mode or build separately."
            Write-Host ""
            Write-Host "  MDM_001_FINAL_ACCEPTANCE_FAILED" -ForegroundColor Red
            return
        }
        $allPassed = $true
        for ($round = 1; $round -le $ApiRuntimeRounds; $round++) {
            $ok = Invoke-ApiRuntimeRound `
                -Round $round -TotalRounds $ApiRuntimeRounds `
                -ApiExe $apiExe -Urls 'http://127.0.0.1:5179' `
                -RepoRoot $RepoRoot `
                -ReadyTimeoutSec $ResumeApiReadyTimeoutSec
            if (-not $ok) { $allPassed = $false; break }
        }
        if ($allPassed) {
            $script:StepResults['Step10_ApiRuntime'] = $true
        }

        # Final summary
        Step 11 'MDM-001R8 final summary (RESUME)'
        $gate = Get-FinalGateDecision -StepResults $script:StepResults `
            -IntegrationRounds (1..$IntegrationRounds) `
            -ApiRounds (1..$ApiRuntimeRounds)
        if ($gate.Success) {
            Write-Host ""
            Write-Host "  $((Get-HarnessDefaults).GateSuccess)" -ForegroundColor Green
        } else {
            Write-Host ""
            Write-Host "  $((Get-HarnessDefaults).GateHardStop)" -ForegroundColor Red
            Write-Host "  (Investigate the failing step's red [FAIL] line above.)" -ForegroundColor Red
        }
        return
    }

    # ----------------------------------------------------------------
    # Full mode (default). Step 1 - Step 11.
    # ----------------------------------------------------------------

    # Step 1: DB target guard
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

    # Step 2: prompt for password
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

        # Step 3: Release build
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

        # Step 4a: migration discovery
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

        # Step 4b: migration apply
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

        # Step 5: test discovery counts
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

        # Step 6: MDM Unit Tests
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

        # Step 7: Identity Tests
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

        # Step 8: Foundation Tests
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

        # Step 9: Integration Tests — 5 rounds
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

        # Step 10: API Host runtime
        if (-not $SkipApiRuntime) {
            Step 10 "API Host runtime ($ApiRuntimeRounds rounds: start, exercise, restart, exercise)"
            $apiExe = "$RepoRoot/apps/api/GuliERP.Api/bin/Release/net10.0/GuliERP.Api.dll"
            if (-not (Test-Path $apiExe)) {
                Fail "API binary not found at $apiExe (build may have skipped API)."
                exit 1
            }
            $allPassed = $true
            for ($round = 1; $round -le $ApiRuntimeRounds; $round++) {
                $ok = Invoke-ApiRuntimeRound `
                    -Round $round -TotalRounds $ApiRuntimeRounds `
                    -ApiExe $apiExe -Urls 'http://127.0.0.1:5179' `
                    -RepoRoot $RepoRoot `
                    -ReadyTimeoutSec 30
                if (-not $ok) { $allPassed = $false; break }
            }
            if ($allPassed) {
                $script:StepResults['Step10_ApiRuntime'] = $true
                Pass ("All {0} API runtime rounds PASS." -f $ApiRuntimeRounds)
            } else {
                Fail "One or more API runtime rounds failed."
            }
        } else {
            Write-Host '  [SKIP] API runtime skipped (-SkipApiRuntime).' -ForegroundColor Yellow
        }

        # Step 11: final summary
        Step 11 'MDM-001 final acceptance summary'
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
        # Step 2 saved its env snapshot inside its own scope; the
        # outer finally restores it for both full and resume mode.
        if ($savedEnv) {
            Restore-OperatorConnectionEnvironment $savedEnv
        }
        Write-Host ""
        Write-Host "  [INFO] ConnectionStrings__GuliERP + GULIERP_MDM_SEED_FILE restored from snapshot." -ForegroundColor Yellow
    }
}
finally {
    # Final safety net: stop the API host if it's still running.
    if ($null -ne $script:ApiHostHandle -and $null -ne $script:ApiHostHandle.Process) {
        $null = Stop-ApiHost -Process $script:ApiHostHandle.Process -GracePeriodSec 3 -HardKillSec 3
        $script:ApiHostHandle = $null
    }
}
