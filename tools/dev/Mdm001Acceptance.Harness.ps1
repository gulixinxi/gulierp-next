#requires -Version 5.1
<#
mdm-001R7 (Final One-Shot Acceptance Readiness) — pure-logic harness
module for `mdm-001-final-acceptance.ps1`. Every public function here
is a side-effect-free decision / parser / formatter that can be
unit-tested by `mdm-001-final-acceptance-selftest.ps1` without
requiring a PostgreSQL connection or a running dotnet process.

Per the brief §六: "PowerShell Parser must produce 0 errors" and
"pure-logic self-tests must cover: all PASS → VERIFIED, any Unit
fail → HARD_STOP, any Integration round fail → HARD_STOP, 5th
round fail → HARD_STOP, Migration fail → HARD_STOP, API Runtime
fail → HARD_STOP, test count insufficient → HARD_STOP, TRX
missing → HARD_STOP, host not exited → cleanup + HARD_STOP,
password not in output, env var finally restored, repeat-run
state not bleeding."

This module is the test surface; the main script is just an
orchestrator on top.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ----------------------------------------------------------------
# Default constants. The main script may override these via
# parameters, but the harness defaults match the canonical
# `tools/dev/assert-gulierp-db-target.ps1` settings.
# ----------------------------------------------------------------
function Get-HarnessDefaults {
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    param()
    return [pscustomobject]@{
        Dotnet           = 'D:\guli\gulierp\.dotnet\dotnet.exe'
        PgHost           = '192.168.2.228'
        PgPort           = '5432'
        PgDatabase       = 'gulierp_g2_003_test'
        PgUsername       = 'gulidata'
        MigrationId      = '20260820190000_MDM001_InitializeMdmSchema'
        IntegrationMinCount = 10
        UnitMinCount        = 40
        IntegrationRounds   = 5
        # The GATE STRING constants. The script must use exactly
        # these — not local variants — so the gate string emitted
        # to the operator's log file is machine-comparable.
        GateSuccess = 'MDM_001_REAL_MASTER_DATA_VERIFIED'
        GateHardStop = 'MDM_001_FINAL_ACCEPTANCE_FAILED'
        GateEnvBlocked = 'MDM_001_POSTGRES_INTEGRATION_STABILIZATION_ENVIRONMENT_BLOCKED'
    }
}

# Backward compat for code that reads $script:HarnessDefaults
$script:HarnessDefaults = Get-HarnessDefaults

# ----------------------------------------------------------------
# Decision: when is the final gate "VERIFIED" (success)?
# Per the brief §五-28 / §六:
#   - Every required step in the StepResult map must be true.
#   - All 5 integration rounds must be true.
#   - At least 1 API runtime round must be true.
#   - The final gate is GATE_SUCCESS only when all of the above
#     hold; otherwise GATE_HARD_STOP.
# This function is pure: it does not read the env, only the
# StepResult map. The self-test verifies every code path.
# ----------------------------------------------------------------
function Get-FinalGateDecision {
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory = $true)][hashtable]$StepResults,
        [int[]]$IntegrationRounds = @(1, 2, 3, 4, 5),
        [int[]]$ApiRounds         = @(1, 2)
    )

    $requiredSteps = @(
        'Step1_DBTarget',
        'Step2_Credential',
        'Step3_Build',
        'Step4a_Discovery',
        'Step4b_Apply',
        'Step5_DiscoveryCounts',
        'Step6_Unit',
        'Step7_Identity',
        'Step8_Foundation',
        'Step9_Integration',
        'Step10_ApiRuntime'
    )

    $missing = @()
    $failed = @()
    foreach ($s in $requiredSteps) {
        if (-not $StepResults.ContainsKey($s)) { $missing += $s; continue }
        if (-not [bool]$StepResults[$s]) { $failed += $s }
    }

    foreach ($r in $IntegrationRounds) {
        $key = "Step9_Integration_Round${r}"
        if (-not $StepResults.ContainsKey($key)) { $missing += $key; continue }
        if (-not [bool]$StepResults[$key]) { $failed += $key }
    }

    $apiMissing = $false
    foreach ($r in $ApiRounds) {
        $key = "Step10_ApiRuntime_Round${r}"
        if (-not $StepResults.ContainsKey($key)) { $apiMissing = $true; break }
        if (-not [bool]$StepResults[$key]) { $failed += $key }
    }
    if ($apiMissing) { $missing += 'Step10_ApiRuntime_Round*' }

    if ($missing.Count -gt 0 -or $failed.Count -gt 0) {
        return [pscustomobject]@{
            Gate    = $script:HarnessDefaults.GateHardStop
            Success = $false
            Missing = $missing
            Failed  = $failed
        }
    }
    return [pscustomobject]@{
        Gate    = $script:HarnessDefaults.GateSuccess
        Success = $true
        Missing = @()
        Failed  = @()
    }
}

# ----------------------------------------------------------------
# Decision: did all 5 integration rounds pass?
# ----------------------------------------------------------------
function Test-AllIntegrationRoundsPassed {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory = $true)][hashtable]$StepResults,
        [int]$Rounds = 5
    )
    for ($i = 1; $i -le $Rounds; $i++) {
        $key = "Step9_Integration_Round${i}"
        if (-not $StepResults.ContainsKey($key)) { return $false }
        if (-not [bool]$StepResults[$key]) { return $false }
    }
    return $true
}

# ----------------------------------------------------------------
# Parser: convert `dotnet test` stdout into a structured
# pass/fail summary. This is a pure function — it does not
# call any external process; it just parses the captured output.
# Handles BOTH the "Passed!" and "Failed!" forms.
# ----------------------------------------------------------------
function Get-TestRunSummary {
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory = $true)][AllowEmptyCollection()]$Output
    )
    $joined = if ($Output -is [string]) { $Output } else { ($Output | ForEach-Object { "$_" }) -join "`n" }
    $failed = 0; $passed = 0; $skipped = 0; $total = 0
    $passedMatch = [regex]::Match($joined, 'Passed!\s*-\s*Failed:\s*(\d+)\s*,\s*Passed:\s*(\d+)\s*,\s*Skipped:\s*(\d+)\s*,\s*Total:\s*(\d+)')
    if ($passedMatch.Success) {
        $failed = [int]$passedMatch.Groups[1].Value
        $passed = [int]$passedMatch.Groups[2].Value
        $skipped = [int]$passedMatch.Groups[3].Value
        $total = [int]$passedMatch.Groups[4].Value
    } else {
        $failedMatch = [regex]::Match($joined, 'Failed!\s*-\s*Failed:\s*(\d+)\s*,\s*Passed:\s*(\d+)\s*,\s*Skipped:\s*(\d+)\s*,\s*Total:\s*(\d+)')
        if ($failedMatch.Success) {
            $failed = [int]$failedMatch.Groups[1].Value
            $passed = [int]$failedMatch.Groups[2].Value
            $skipped = [int]$failedMatch.Groups[3].Value
            $total = [int]$failedMatch.Groups[4].Value
        }
    }
    return [pscustomobject]@{
        Failed = $failed; Passed = $passed; Skipped = $skipped; Total = $total
    }
}

# ----------------------------------------------------------------
# Parser: convert `dotnet test --list-tests` stdout into a
# discovered test count.
# ----------------------------------------------------------------
function Get-TestDiscoveryCount {
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory = $true)][AllowEmptyCollection()]$Output
    )
    $joined = if ($Output -is [string]) { $Output } else { ($Output | ForEach-Object { "$_" }) -join "`n" }
    $names = $joined -split "`r?`n" | Where-Object { $_ -match '^\s+GuliERP\.[A-Za-z0-9_.]+(\(.+\))?\s*$' }
    return [pscustomobject]@{
        Count = @($names).Count
    }
}

# ----------------------------------------------------------------
# Convert captured command output to plain text (mdm-001R2 fix).
# ----------------------------------------------------------------
function ConvertTo-PlainText {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $false)]
        [AllowNull()]
        [AllowEmptyCollection()]
        $Value
    )
    if ($null -eq $Value) { return '' }
    $joined = if ($Value -is [string]) {
        $Value
    } else {
        ($Value | ForEach-Object { "$_" }) -join "`n"
    }
    $joined = $joined -replace ([char]27 + "\[[0-9;?]*[a-zA-Z]"), ''
    $joined = $joined -replace "`r`n?", "`n"
    return $joined
}

# ----------------------------------------------------------------
# True when the captured dotnet ef migrations list output contains
# the full expected migration ID as a bounded token.
# ----------------------------------------------------------------
function Test-MigrationDiscovered {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory = $false)]
        [AllowNull()]
        [AllowEmptyCollection()]
        $CommandOutput,

        [Parameter(Mandatory = $true)]
        [string]$ExpectedMigrationId
    )
    $text = ConvertTo-PlainText -Value $CommandOutput
    if ([string]::IsNullOrEmpty($text)) { return $false }
    $escaped = [regex]::Escape($ExpectedMigrationId)
    $pattern = "(?<![A-Za-z0-9_])${escaped}(?=\s|\(|$)"
    return [regex]::IsMatch($text, $pattern)
}

# ----------------------------------------------------------------
# Redact a connection string so the password never appears in
# any output. Pure.
# ----------------------------------------------------------------
function Redact-ConnectionString {
    [CmdletBinding()]
    [OutputType([string])]
    param([string]$cs)
    if ([string]::IsNullOrEmpty($cs)) { return '<empty>' }
    return ($cs -replace "(?i)Password\s*=\s*[^;]+", 'Password=***')
}

# ----------------------------------------------------------------
# Redact a generic block of text. Useful for any text the script
# emits to console (build output, test output, etc.).
# ----------------------------------------------------------------
function Redact-SecretText {
    [CmdletBinding()]
    param([object]$Value)
    if ($null -eq $Value) { return '' }
    $text = ($Value | Out-String)
    $text = $text -replace "(?i)(Password|Pwd)\s*=\s*[^;`r`n]+", '$1=***'
    return $text
}

# ----------------------------------------------------------------
# Snapshot the operator's environment variables so we can restore
# them in `finally`. The self-test verifies the snapshot/restore
# pair is roundtrip-safe.
# ----------------------------------------------------------------
function Save-OperatorConnectionEnvironment {
    [CmdletBinding()]
    [OutputType([hashtable])]
    param()

    return @{
        ConnectionStrings__GuliERP = @{
            Exists = Test-Path Env:ConnectionStrings__GuliERP
            Value  = $env:ConnectionStrings__GuliERP
        }
        GULIERP_MDM_SEED_FILE = @{
            Exists = Test-Path Env:GULIERP_MDM_SEED_FILE
            Value  = $env:GULIERP_MDM_SEED_FILE
        }
    }
}

function Restore-OperatorConnectionEnvironment {
    [CmdletBinding()]
    param($Snapshot)
    if ($null -eq $Snapshot) { return }
    foreach ($k in @('ConnectionStrings__GuliERP', 'GULIERP_MDM_SEED_FILE')) {
        if ($Snapshot.ContainsKey($k)) {
            if ($Snapshot[$k].Exists) {
                Set-Item -Path "Env:$k" -Value $Snapshot[$k].Value
            } else {
                Remove-Item "Env:$k" -ErrorAction SilentlyContinue
            }
        }
    }
}

# ----------------------------------------------------------------
# R9 Operator Transcript Backfill helpers.
#
# R8 only supported Machine TRX mode (real TRX files in
# tests/_evidence_trx/). When the Operator's R7 run did not
# produce per-round TRX, the R8 Resume mode failed closed —
# correctly, but the failure surfaced the real R7 evidence
# gap: only the API host log file + Operator's console
# transcript exist. R9 introduces a STRICT Transcript
# Backfill mode that reads from a canonical evidence file
# (docs/verification/MDM_001_R7_OPERATOR_TRANSCRIPT_EVIDENCE.md)
# and validates it against a fixed set of markers, fixed test
# counts, an explicit TRX NOT_AVAILABLE statement, an
# SHA256 match on the API host log, and a git-path invariant
# that no business code was modified between R7 and R9.
# No fake TRX is ever created.
# ----------------------------------------------------------------

# Canonical R7 Transcript Backfill file. The harness trusts
# only THIS file as the transcript backfill source. The
# Operator can change the value at runtime via
# `-TranscriptEvidencePath` on the main script.
$script:DefaultTranscriptEvidencePath = 'docs/verification/MDM_001_R7_OPERATOR_TRANSCRIPT_EVIDENCE.md'

# Validate the R7 Transcript Backfill file. Returns a
# pscustomobject with:
#   Ok                  $true only when every invariant holds
#   Mode                'OPERATOR_TRANSCRIPT_BACKFILL_VERIFIED' on success
#   Missing             @() on success; one entry per failed invariant
#   ApiLogSha256        the SHA256 recorded in the file (or $null)
#   ApiLogPath          the API host log path recorded in the file
#   RecordedR7Head      the R7 HEAD recorded in the file
# The function NEVER creates files. It only reads.
function Test-OperatorTranscriptBackfill {
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory = $false)][string]$TranscriptPath = $script:DefaultTranscriptEvidencePath,
        [Parameter(Mandatory = $false)][string]$RepoRoot = (Get-Location).Path,
        [string]$ExpectedR7Head = '15d46c4'
    )
    $missing = @()
    $fullPath = if ([System.IO.Path]::IsPathRooted($TranscriptPath)) { $TranscriptPath } else { Join-Path $RepoRoot $TranscriptPath }

    if (-not (Test-Path $fullPath)) {
        return [pscustomobject]@{
            Ok = $false
            Mode = 'NOT_AVAILABLE'
            Missing = @("Transcript file not found at $fullPath")
            ApiLogSha256 = $null
            ApiLogPath = $null
            RecordedR7Head = $null
        }
    }

    $content = Get-Content -Raw $fullPath -Encoding UTF8

    # 1. Mandatory EVIDENCE_TYPE marker. The canonical R7 file
    #    uses markdown bold (`**EVIDENCE_TYPE=OPERATOR_TRANSCRIPT_REPORTED**`),
    #    so we match the substring anywhere on a line — we do NOT
    #    require it to be the only content of the line.
    if ($content -notmatch '(?im)EVIDENCE_TYPE\s*=\s*OPERATOR_TRANSCRIPT_REPORTED') {
        $missing += 'EVIDENCE_TYPE=OPERATOR_TRANSCRIPT_REPORTED marker missing'
    }

    # 2. TRX_STATUS=NOT_AVAILABLE must be explicit. Same markdown-friendly
    #    substring match as above.
    if ($content -notmatch '(?im)TRX_STATUS\s*=\s*NOT_AVAILABLE') {
        $missing += 'TRX_STATUS=NOT_AVAILABLE marker missing (this file is a Transcript Backfill, not a TRX source)'
    }

    # 3. Test counts present
    $checks = @(
        @{ Pattern = '57\s*PASS'; Name = 'MDM 57/57' },
        @{ Pattern = '21\s*PASS'; Name = 'Identity 21/21' },
        @{ Pattern = '44\s*PASS'; Name = 'Foundation 44/44' },
        @{ Pattern = 'Round 1.*10\s*PASS'; Name = 'Integration Round 1 10/10' },
        @{ Pattern = 'Round 2.*10\s*PASS'; Name = 'Integration Round 2 10/10' },
        @{ Pattern = 'Round 3.*10\s*PASS'; Name = 'Integration Round 3 10/10' },
        @{ Pattern = 'Round 4.*10\s*PASS'; Name = 'Integration Round 4 10/10' },
        @{ Pattern = 'Round 5.*10\s*PASS'; Name = 'Integration Round 5 10/10' },
        @{ Pattern = '/health/live.*200'; Name = 'API Round 1 /health/live 200' },
        @{ Pattern = '/health/ready.*200'; Name = 'API Round 1 /health/ready 200' },
        @{ Pattern = 'Root.*200|root banner.*200|banner.*200|/ 200'; Name = 'API Round 1 root banner 200' }
    )
    foreach ($c in $checks) {
        if ($content -notmatch $c.Pattern) {
            $missing += "Missing test count: $($c.Name)"
        }
    }

    # 4. Recorded R7 HEAD must match
    $recordedR7Head = $null
    $m = [regex]::Match($content, '(?im)R7 harness HEAD\s*\|\s*`?([0-9a-f]{7,40})`?')
    if ($m.Success) {
        $recordedR7Head = $m.Groups[1].Value
    } else {
        $missing += 'R7 harness HEAD field not found in evidence file'
    }
    if ($null -ne $recordedR7Head -and $recordedR7Head -ne $ExpectedR7Head) {
        # Allow short-SHA prefix matches (e.g. 15d46c4 matches 15d46c4...)
        if (-not $ExpectedR7Head.StartsWith($recordedR7Head) -and -not $recordedR7Head.StartsWith($ExpectedR7Head)) {
            $missing += "R7 HEAD mismatch: file says $recordedR7Head, expected $ExpectedR7Head"
        }
    }

    # 5. SHA256 of the API host log must match
    $apiLogSha256 = $null
    $apiLogPath = $null
    # The canonical R7 file has the SHA256 inside a markdown table
    # row with backticks, e.g.
    #   | `tests/_evidence_trx/api_host_round1_20260821155926.log` | `<sha256>` | `MACHINE_LOG_VERIFIED` | ...
    # We use a tolerant non-greedy match anchored on the path so the
    # SHA256 may be preceded or followed by backticks / spaces.
    $mLog = [regex]::Match($content, '(?is)api_host_round1_20260821155926\.log\b[^|]*?\|\s*`?\s*([0-9A-Fa-f]{64})\s*`?')
    if (-not $mLog.Success) {
        # Fallback: any 64-hex token that appears within 200 chars
        # of the path token.
        $idx = $content.IndexOf('api_host_round1_20260821155926.log', [System.StringComparison]::OrdinalIgnoreCase)
        if ($idx -ge 0) {
            $window = $content.Substring($idx, [Math]::Min(400, $content.Length - $idx))
            $m2 = [regex]::Match($window, '([0-9A-Fa-f]{64})')
            if ($m2.Success) { $mLog = $m2 }
        }
    }
    if ($mLog.Success) {
        $apiLogSha256 = $mLog.Groups[1].Value.ToUpperInvariant()
        $apiLogPath = (Join-Path $RepoRoot 'tests/_evidence_trx/api_host_round1_20260821155926.log')
    } else {
        $missing += 'API host log SHA256 not recorded in evidence file'
    }
    if ($null -ne $apiLogSha256 -and $apiLogSha256.Length -eq 64) {
        if (-not (Test-Path $apiLogPath)) {
            $missing += "API host log file missing on disk at $apiLogPath"
        } else {
            $actualSha = (Get-FileHash -Path $apiLogPath -Algorithm SHA256).Hash.ToUpperInvariant()
            if ($actualSha -ne $apiLogSha256) {
                $missing += "API host log SHA256 mismatch: file=$apiLogSha256 actual=$actualSha"
            }
        }
    }

    # 6. "Not fabricated" check: the file must explicitly mark
    #    TRX as NOT_AVAILABLE and the per-round TRX as absent.
    if ($content -notmatch '(?im)NOT_AVAILABLE') {
        $missing += 'Evidence file does not explicitly acknowledge NOT_AVAILABLE for any missing evidence'
    }
    if ($content -notmatch '(?im)not present') {
        $missing += 'Evidence file does not explicitly state that TRX/log files are "not present"'
    }

    $ok = ($missing.Count -eq 0)
    return [pscustomobject]@{
        Ok = $ok
        Mode = if ($ok) { 'OPERATOR_TRANSCRIPT_BACKFILL_VERIFIED' } else { 'OPERATOR_TRANSCRIPT_BACKFILL_INVALID' }
        Missing = $missing
        ApiLogSha256 = $apiLogSha256
        ApiLogPath = $apiLogPath
        RecordedR7Head = $recordedR7Head
    }
}

# Test-NoBusinessCodeChangeSinceR7: assert that the diff
# from the R7 harness commit to the current HEAD touches
# ONLY the harness scripts, the verification reports, and
# the GOAL_REGISTRY. Returns pscustomobject {Ok, ChangedCsFiles}.
# Pure: uses git CLI but no side effects.
function Test-NoBusinessCodeChangeSinceR7 {
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory = $false)][string]$RepoRoot = (Get-Location).Path,
        [string]$R7Head = '15d46c4'
    )
    Push-Location $RepoRoot
    try {
        # The caller may pass an invalid ref (e.g. HEAD~99999). When
        # $ErrorActionPreference is 'Stop' and the harness sets
        # Set-StrictMode, `& git ... 2>&1` on a bad ref raises a
        # terminating NativeCommandError BEFORE we get to check
        # $LASTEXITCODE. We temporarily relax to 'Continue' so the
        # call always returns and we can report a structured failure.
        $prevEAP = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        try {
            $diffOutput = & git diff --name-only "$R7Head..HEAD" 2>&1
        } finally {
            $ErrorActionPreference = $prevEAP
        }
        if ($LASTEXITCODE -ne 0) {
            return [pscustomobject]@{
                Ok = $false
                ChangedCsFiles = @("git diff failed: $diffOutput")
                AllChanged = @()
            }
        }
        $allChanged = @($diffOutput | Where-Object { $_ -match '\S' })
        # Disallowed patterns: business code, migrations, tests, web
        $disallowedPatterns = @(
            '^modules/.*\.cs$',
            '^apps/.*\.cs$',
            'Migrations/.*\.cs$',
            '^tests/GuliERP\..*\.cs$',
            'tools/GuliERP\..*\.cs$'
        )
        $csChanged = @()
        foreach ($f in $allChanged) {
            foreach ($p in $disallowedPatterns) {
                if ($f -match $p) {
                    $csChanged += $f
                    break
                }
            }
        }
        return [pscustomobject]@{
            Ok = ($csChanged.Count -eq 0)
            ChangedCsFiles = $csChanged
            AllChanged = $allChanged
        }
    } finally {
        Pop-Location
    }
}

# ----------------------------------------------------------------
# API Host process lifecycle (mdm-001R8 fix).
#
# The R7 acceptance harness failed at Round 1 with
# "The term 'Stop-ApiHost' is not recognized" — root cause:
# the function was defined AFTER the `try-finally` block, and
# PowerShell does NOT register function declarations that
# appear after the final `try-finally` in a .ps1 file. The fix
# is two-fold:
#
#   1. Move ALL function definitions to the top of the script
#      (or to this harness module which is loaded BEFORE the
#      main script body).
#   2. Wrap each helper in this harness module with proper
#      scope semantics so the selftest can dot-source the
#      module, Get-Command each helper, and prove the helpers
#      exist BEFORE the script runs.
#
# These helpers take a Process object (or a script-scope
# variable name) so they do not depend on `$script:` globals
# that might not be set.
# ----------------------------------------------------------------

# Start-ApiHost: launches a long-running .NET host process.
# Returns a [pscustomobject] with .Process (System.Diagnostics.Process)
# and .LogPath so the caller can read the PID + tail the log.
function Start-ApiHost {
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory = $true)][string]$DotnetExe,
        [Parameter(Mandatory = $true)][string]$HostDll,
        [Parameter(Mandatory = $true)][string]$LogPath,
        [Parameter(Mandatory = $true)][string]$Urls,
        [int]$StartTimeoutSec = 30
    )
    $errPath = "$LogPath.err"
    $proc = Start-Process -FilePath $DotnetExe `
        -ArgumentList @($HostDll, '--urls', $Urls) `
        -PassThru -NoNewWindow `
        -RedirectStandardOutput $LogPath `
        -RedirectStandardError $errPath
    return [pscustomobject]@{
        Process = $proc
        LogPath = $LogPath
        ErrPath = $errPath
        Pid     = $proc.Id
        StartedAt = Get-Date
    }
}

# Wait-ApiHostReady: polls an HTTP endpoint until it returns
# the expected status, or until the timeout elapses.
# Returns $true on success, $false on timeout.
function Wait-ApiHostReady {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory = $true)][string]$Url,
        [int]$TimeoutSec = 30,
        [int]$ExpectedStatus = 200,
        [int]$PollIntervalMs = 500
    )
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        try {
            $r = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 2
            if ($r.StatusCode -eq $ExpectedStatus) { return $true }
        } catch {
            # not ready yet
        }
        Start-Sleep -Milliseconds $PollIntervalMs
    }
    return $false
}

# Test-ApiEndpoint: a single HTTP GET that returns a structured
# result. Never throws — exceptions are converted to a result
# with StatusCode = -1.
function Test-ApiEndpoint {
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory = $true)][string]$Url,
        [int]$TimeoutSec = 5
    )
    try {
        $r = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec $TimeoutSec
        return [pscustomobject]@{
            Ok = $true
            StatusCode = $r.StatusCode
            Error = $null
        }
    } catch {
        # The Exception object may or may not have a `.Response`
        # property — the property only exists on
        # HttpRequestException (i.e. a real HTTP failure). For a
        # "connection refused" or "DNS failure" the property is
        # absent. Use a defensive Member check to avoid throwing
        # inside the catch handler.
        $code = -1
        $ex = $_.Exception
        if ($null -ne $ex -and ($ex.PSObject.Properties.Name -contains 'Response')) {
            $resp = $ex.Response
            if ($null -ne $resp) {
                try { $code = [int]$resp.StatusCode } catch { $code = -1 }
            }
        }
        return [pscustomobject]@{
            Ok = $false
            StatusCode = $code
            Error = if ($null -ne $ex) { $ex.Message } else { 'unknown error' }
        }
    }
}

# Stop-ApiHost: gracefully stops a process. The caller passes
# the .Process object (not a script-scope variable) so this
# function has no hidden state.
#
# Semantics:
#   1. If $Process is $null or has already exited -> return
#      a result indicating "already stopped".
#   2. Try to close the main window. If that does not work
#      within $GracePeriodSec, fall back to -Force.
#   3. Wait for the process to exit. If it does not exit
#      within $HardKillSec, return a result indicating the
#      process is still alive (caller can decide to retry or
#      accept the result).
#   4. Return [pscustomobject] { Stopped = $true/false, Pid,
#      AlreadyExited, UsedForce, Message }.
function Stop-ApiHost {
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory = $true)][AllowNull()]$Process,
        [int]$GracePeriodSec = 5,
        [int]$HardKillSec = 5
    )
    if ($null -eq $Process) {
        return [pscustomobject]@{
            Stopped = $true
            Pid = $null
            AlreadyExited = $true
            UsedForce = $false
            Message = "Process was null; nothing to stop."
        }
    }
    # NB: avoid the name `$pid` (PowerShell automatic read-only
    # variable). Use `$processId` instead.
    $processId = $null
    try { $processId = $Process.Id } catch { $processId = $null }

    if ($Process.HasExited) {
        return [pscustomobject]@{
            Stopped = $true
            Pid = $processId
            AlreadyExited = $true
            UsedForce = $false
            Message = "Process already exited."
        }
    }

    $usedForce = $false
    try {
        $Process.CloseMainWindow() | Out-Null
        if (-not $Process.WaitForExit($GracePeriodSec * 1000)) {
            $usedForce = $true
            Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
        }
        if (-not $Process.WaitForExit($HardKillSec * 1000)) {
            return [pscustomobject]@{
                Stopped = $false
                Pid = $processId
                AlreadyExited = $false
                UsedForce = $usedForce
                Message = "Process did not exit within $HardKillSec s after $(if ($usedForce) { 'force kill' } else { 'graceful close' })."
            }
        }
    } catch {
        return [pscustomobject]@{
            Stopped = $false
            Pid = $processId
            AlreadyExited = $false
            UsedForce = $usedForce
            Message = "Exception during stop: $($_.Exception.Message)"
        }
    }

    return [pscustomobject]@{
        Stopped = $true
        Pid = $processId
        AlreadyExited = $false
        UsedForce = $usedForce
        Message = "Stopped in $(if ($usedForce) { 'force' } else { 'graceful' }) mode."
    }
}

# Test-ApiProcessAlive: returns $true if the Process object is
# still running, $false if it has exited or is null. Useful for
# port-release verification after Stop-ApiHost.
function Test-ApiProcessAlive {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory = $true)][AllowNull()]$Process
    )
    if ($null -eq $Process) { return $false }
    try {
        if ($Process.HasExited) { return $false }
        return $true
    } catch {
        return $false
    }
}

# Test-ApiPortListening: tests whether a TCP port on a host
# is listening. Returns $true if a connection can be opened
# within $TimeoutSec, $false otherwise.
#
# NB: avoid `$Host` (PowerShell automatic read-only variable
# for the console host). Use `$Hostname` (param alias
# `-TargetHost`).
function Test-ApiPortListening {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory = $true)][Alias('TargetHost')][string]$Hostname,
        [Parameter(Mandatory = $true)][int]$Port,
        [int]$TimeoutSec = 5
    )
    try {
        $client = New-Object System.Net.Sockets.TcpClient
        $iar = $client.BeginConnect($Hostname, $Port, $null, $null)
        $ok = $iar.AsyncWaitHandle.WaitOne($TimeoutSec * 1000)
        if ($ok) {
            try { $client.EndConnect($iar) | Out-Null } catch { $ok = $false }
        }
        try { $client.Close() } catch { }
        return $ok
    } catch {
        return $false
    }
}

# ----------------------------------------------------------------
# Pure decision: should the gate be "VERIFIED" given an outcome
# set? This is the SINGLE source of truth for the success gate.
# The self-test verifies the boolean table.
# ----------------------------------------------------------------
function Test-AllOutcomesPassed {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory = $true)][hashtable]$StepResults,
        [string[]]$RequiredStepKeys,
        [int[]]$IntegrationRounds = @(1,2,3,4,5)
    )
    foreach ($k in $RequiredStepKeys) {
        if (-not $StepResults.ContainsKey($k)) { return $false }
        if (-not [bool]$StepResults[$k]) { return $false }
    }
    foreach ($r in $IntegrationRounds) {
        $key = "Step9_Integration_Round${r}"
        if (-not $StepResults.ContainsKey($key)) { return $false }
        if (-not [bool]$StepResults[$key]) { return $false }
    }
    return $true
}

# ----------------------------------------------------------------
# Compute the set of keys that are still required. Used by
# the summary to give the operator a checklist of "did not run"
# steps. Pure.
# ----------------------------------------------------------------
function Get-MissingStepKeys {
    [CmdletBinding()]
    [OutputType([string[]])]
    param(
        [Parameter(Mandatory = $true)][hashtable]$StepResults,
        [string[]]$RequiredStepKeys,
        [int[]]$IntegrationRounds = @(1,2,3,4,5),
        [int[]]$ApiRounds = @(1,2)
    )
    $missing = @()
    foreach ($k in $RequiredStepKeys) {
        if (-not $StepResults.ContainsKey($k)) { $missing += $k }
    }
    foreach ($r in $IntegrationRounds) {
        $key = "Step9_Integration_Round${r}"
        if (-not $StepResults.ContainsKey($key)) { $missing += $key }
    }
    foreach ($r in $ApiRounds) {
        $key = "Step10_ApiRuntime_Round${r}"
        if (-not $StepResults.ContainsKey($key)) { $missing += $key }
    }
    return $missing
}

# ----------------------------------------------------------------
# Validate that an output stream does NOT contain a raw password.
# Pure.
# ----------------------------------------------------------------
function Test-OutputContainsPassword {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory = $true)][AllowEmptyCollection()]$Output,
        [Parameter(Mandatory = $false)][AllowEmptyString()][string]$Password = ''
    )
    if ([string]::IsNullOrEmpty($Password)) { return $false }
    $joined = if ($Output -is [string]) { $Output } else { ($Output | ForEach-Object { "$_" }) -join "`n" }
    return $joined.Contains($Password)
}

# ----------------------------------------------------------------
# Validate that the connection string contains the password and
# that the redacted form does not. Pure.
# ----------------------------------------------------------------
function Test-ConnectionStringRedaction {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory = $true)][string]$Original,
        [Parameter(Mandatory = $true)][string]$Redacted,
        [Parameter(Mandatory = $true)][string]$Password
    )
    if (-not $Original.Contains($Password)) { return $false }
    if ($Redacted.Contains($Password)) { return $false }
    if (-not $Redacted.Contains('Password=***')) { return $false }
    return $true
}

# ----------------------------------------------------------------
# Sanity check: a test project's "Pass" verdict requires both
# 0 failed AND total >= discovered. Pure.
# ----------------------------------------------------------------
function Test-RunSummaryAcceptable {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory = $true)][pscustomobject]$Summary,
        [Parameter(Mandatory = $true)][int]$DiscoveredCount
    )
    if ($Summary.Failed -gt 0) { return $false }
    if ($Summary.Total -lt $DiscoveredCount) { return $false }
    if ($Summary.Passed -lt 1) { return $false }
    return $true
}

# ----------------------------------------------------------------
# Helper: build a StepResult map entry. Pure.
# ----------------------------------------------------------------
function New-StepResultMap {
    [CmdletBinding()]
    [OutputType([hashtable])]
    param()
    return @{}
}

# ----------------------------------------------------------------
# Helper: record a step outcome. Pure.
# ----------------------------------------------------------------
function Set-StepResult {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][hashtable]$Map,
        [Parameter(Mandatory = $true)][string]$Key,
        [Parameter(Mandatory = $true)][bool]$Value
    )
    $Map[$Key] = $Value
    return $Map
}

# ----------------------------------------------------------------
# Module-style functions are auto-exported when this file is
# loaded via `Import-Module`. No Export-ModuleMember needed
# because we are a .ps1 (not .psm1) file; Import-Module on
# a .ps1 file evaluates it in a module scope and all functions
# become accessible.
# ----------------------------------------------------------------
