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
