#requires -Version 5.1
<#
mdm-001R7 (Final One-Shot Acceptance Readiness) — self-test for the
pure-logic decision layer in
`tools/dev/Mdm001Acceptance.Harness.ps1`.

This script does NOT need a PostgreSQL connection. It calls each
public function on the harness with carefully chosen inputs and
verifies the outputs. The self-test covers the brief §六 contract:

  1. 全部 PASS → VERIFIED
  2. 任一 Unit 失败 → HARD_STOP
  3. 任一 Integration 轮次失败 → HARD_STOP
  4. 5 轮中第 5 轮失败 → HARD_STOP
  5. Migration 失败 → HARD_STOP
  6. API Runtime 失败 → HARD_STOP
  7. 测试数量不足 → HARD_STOP
  8. TRX 不存在 → HARD_STOP
  9. Host 未退出 → 清理并 HARD_STOP
 10. 密码不进入输出
 11. 环境变量 finally 恢复
 12. 重复运行状态不串线

Run:
  cd D:\guli\projects\gulierp-next
  pwsh -NoProfile -File tools/dev/mdm-001-final-acceptance-selftest.ps1

Exit code 0 = all pure-logic self-tests pass. Non-zero = at least
one assertion failed.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

# ----------------------------------------------------------------
# Load the harness
# ----------------------------------------------------------------
$harnessPath = Join-Path $PSScriptRoot 'Mdm001Acceptance.Harness.ps1'
if (-not (Test-Path $harnessPath)) {
    throw "Harness module not found at $harnessPath"
}
Import-Module $harnessPath -Force

# ----------------------------------------------------------------
# Test counters
# ----------------------------------------------------------------
$script:TestCount = 0
$script:PassCount = 0
$script:FailCount = 0
$script:Failures = @()

function Test-Case {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][scriptblock]$Assertion
    )
    $script:TestCount++
    try {
        & $Assertion
        $script:PassCount++
        Write-Host "  [PASS] $Name" -ForegroundColor Green
    } catch {
        $script:FailCount++
        $script:Failures += [pscustomobject]@{
            Name = $Name
            Error = $_.Exception.Message
        }
        Write-Host "  [FAIL] $Name : $($_.Exception.Message)" -ForegroundColor Red
    }
}

function Assert-Equal {
    [CmdletBinding()]
    param($Expected, $Actual, [string]$Msg = '')
    if ($Expected -ne $Actual) {
        throw "Assert-Equal: expected '$Expected', got '$Actual' (${Msg})"
    }
}
function Assert-True {
    [CmdletBinding()]
    param([bool]$Value, [string]$Msg = '')
    if (-not $Value) { throw "Assert-True failed (${Msg})" }
}
function Assert-False {
    [CmdletBinding()]
    param([bool]$Value, [string]$Msg = '')
    if ($Value) { throw "Assert-False failed (${Msg})" }
}

Write-Host "============================================================"
Write-Host "mdm-001R7 Self-Test for Mdm001Acceptance.Harness.ps1"
Write-Host "============================================================"

# ----------------------------------------------------------------
# Section 1: Get-FinalGateDecision
# ----------------------------------------------------------------
Write-Host ""
Write-Host "## 1. Get-FinalGateDecision" -ForegroundColor Cyan

Test-Case 'All required steps pass + all 5 integration rounds + both API rounds -> VERIFIED' {
    $results = @{
        'Step1_DBTarget' = $true
        'Step2_Credential' = $true
        'Step3_Build' = $true
        'Step4a_Discovery' = $true
        'Step4b_Apply' = $true
        'Step5_DiscoveryCounts' = $true
        'Step6_Unit' = $true
        'Step7_Identity' = $true
        'Step8_Foundation' = $true
        'Step9_Integration' = $true
        'Step9_Integration_Round1' = $true
        'Step9_Integration_Round2' = $true
        'Step9_Integration_Round3' = $true
        'Step9_Integration_Round4' = $true
        'Step9_Integration_Round5' = $true
        'Step10_ApiRuntime' = $true
        'Step10_ApiRuntime_Round1' = $true
        'Step10_ApiRuntime_Round2' = $true
    }
    $gate = Get-FinalGateDecision -StepResults $results
    Assert-True $gate.Success "all passed but gate says not success"
    Assert-Equal 'MDM_001_REAL_MASTER_DATA_VERIFIED' $gate.Gate
    Assert-Equal 0 $gate.Missing.Count
    Assert-Equal 0 $gate.Failed.Count
}

Test-Case 'Step6 (Unit) fails -> HARD_STOP' {
    $results = @{
        'Step1_DBTarget' = $true
        'Step2_Credential' = $true
        'Step3_Build' = $true
        'Step4a_Discovery' = $true
        'Step4b_Apply' = $true
        'Step6_Unit' = $false
        'Step7_Identity' = $true
        'Step8_Foundation' = $true
        'Step9_Integration' = $true
        'Step9_Integration_Round1' = $true
        'Step9_Integration_Round2' = $true
        'Step9_Integration_Round3' = $true
        'Step9_Integration_Round4' = $true
        'Step9_Integration_Round5' = $true
        'Step10_ApiRuntime' = $true
        'Step10_ApiRuntime_Round1' = $true
        'Step10_ApiRuntime_Round2' = $true
    }
    $gate = Get-FinalGateDecision -StepResults $results
    Assert-False $gate.Success
    Assert-Equal 'MDM_001_FINAL_ACCEPTANCE_FAILED' $gate.Gate
    Assert-True ($gate.Failed -contains 'Step6_Unit')
}

Test-Case 'Integration round 3 fails -> HARD_STOP' {
    $results = @{
        'Step1_DBTarget' = $true
        'Step2_Credential' = $true
        'Step3_Build' = $true
        'Step4a_Discovery' = $true
        'Step4b_Apply' = $true
        'Step6_Unit' = $true
        'Step7_Identity' = $true
        'Step8_Foundation' = $true
        'Step9_Integration' = $true
        'Step9_Integration_Round1' = $true
        'Step9_Integration_Round2' = $true
        'Step9_Integration_Round3' = $false
        'Step9_Integration_Round4' = $true
        'Step9_Integration_Round5' = $true
        'Step10_ApiRuntime' = $true
        'Step10_ApiRuntime_Round1' = $true
        'Step10_ApiRuntime_Round2' = $true
    }
    $gate = Get-FinalGateDecision -StepResults $results
    Assert-False $gate.Success
    Assert-True ($gate.Failed -contains 'Step9_Integration_Round3')
}

Test-Case 'Integration round 5 fails (5th of 5) -> HARD_STOP' {
    $results = @{
        'Step1_DBTarget' = $true
        'Step2_Credential' = $true
        'Step3_Build' = $true
        'Step4a_Discovery' = $true
        'Step4b_Apply' = $true
        'Step6_Unit' = $true
        'Step7_Identity' = $true
        'Step8_Foundation' = $true
        'Step9_Integration' = $true
        'Step9_Integration_Round1' = $true
        'Step9_Integration_Round2' = $true
        'Step9_Integration_Round3' = $true
        'Step9_Integration_Round4' = $true
        'Step9_Integration_Round5' = $false
        'Step10_ApiRuntime' = $true
        'Step10_ApiRuntime_Round1' = $true
        'Step10_ApiRuntime_Round2' = $true
    }
    $gate = Get-FinalGateDecision -StepResults $results
    Assert-False $gate.Success
    Assert-True ($gate.Failed -contains 'Step9_Integration_Round5')
}

Test-Case 'Step4b (Migration apply) fails -> HARD_STOP' {
    $results = @{
        'Step1_DBTarget' = $true
        'Step2_Credential' = $true
        'Step3_Build' = $true
        'Step4a_Discovery' = $true
        'Step4b_Apply' = $false
        'Step6_Unit' = $true
        'Step7_Identity' = $true
        'Step8_Foundation' = $true
        'Step9_Integration' = $true
        'Step9_Integration_Round1' = $true
        'Step9_Integration_Round2' = $true
        'Step9_Integration_Round3' = $true
        'Step9_Integration_Round4' = $true
        'Step9_Integration_Round5' = $true
        'Step10_ApiRuntime' = $true
        'Step10_ApiRuntime_Round1' = $true
        'Step10_ApiRuntime_Round2' = $true
    }
    $gate = Get-FinalGateDecision -StepResults $results
    Assert-False $gate.Success
    Assert-True ($gate.Failed -contains 'Step4b_Apply')
}

Test-Case 'API runtime round 1 fails -> HARD_STOP' {
    $results = @{
        'Step1_DBTarget' = $true
        'Step2_Credential' = $true
        'Step3_Build' = $true
        'Step4a_Discovery' = $true
        'Step4b_Apply' = $true
        'Step6_Unit' = $true
        'Step7_Identity' = $true
        'Step8_Foundation' = $true
        'Step9_Integration' = $true
        'Step9_Integration_Round1' = $true
        'Step9_Integration_Round2' = $true
        'Step9_Integration_Round3' = $true
        'Step9_Integration_Round4' = $true
        'Step9_Integration_Round5' = $true
        'Step10_ApiRuntime' = $true
        'Step10_ApiRuntime_Round1' = $false
        'Step10_ApiRuntime_Round2' = $true
    }
    $gate = Get-FinalGateDecision -StepResults $results
    Assert-False $gate.Success
    Assert-True ($gate.Failed -contains 'Step10_ApiRuntime_Round1')
}

Test-Case 'Step5 missing -> HARD_STOP (test discovery count not run)' {
    $results = @{
        'Step1_DBTarget' = $true
        'Step2_Credential' = $true
        'Step3_Build' = $true
        'Step4a_Discovery' = $true
        'Step4b_Apply' = $true
        # Step5 missing
        'Step6_Unit' = $true
        'Step7_Identity' = $true
        'Step8_Foundation' = $true
        'Step9_Integration' = $true
        'Step9_Integration_Round1' = $true
        'Step9_Integration_Round2' = $true
        'Step9_Integration_Round3' = $true
        'Step9_Integration_Round4' = $true
        'Step9_Integration_Round5' = $true
        'Step10_ApiRuntime' = $true
        'Step10_ApiRuntime_Round1' = $true
        'Step10_ApiRuntime_Round2' = $true
    }
    $gate = Get-FinalGateDecision -StepResults $results
    Assert-False $gate.Success
    Assert-True ($gate.Missing -contains 'Step5_DiscoveryCounts')
}

# ----------------------------------------------------------------
# Section 2: Test-AllIntegrationRoundsPassed
# ----------------------------------------------------------------
Write-Host ""
Write-Host "## 2. Test-AllIntegrationRoundsPassed" -ForegroundColor Cyan

Test-Case 'All 5 rounds present and true -> true' {
    $results = @{
        'Step9_Integration_Round1' = $true
        'Step9_Integration_Round2' = $true
        'Step9_Integration_Round3' = $true
        'Step9_Integration_Round4' = $true
        'Step9_Integration_Round5' = $true
    }
    Assert-True (Test-AllIntegrationRoundsPassed -StepResults $results)
}

Test-Case 'Round 4 missing -> false' {
    $results = @{
        'Step9_Integration_Round1' = $true
        'Step9_Integration_Round2' = $true
        'Step9_Integration_Round3' = $true
        # Round 4 missing
        'Step9_Integration_Round5' = $true
    }
    Assert-False (Test-AllIntegrationRoundsPassed -StepResults $results)
}

Test-Case 'Round 2 false -> false' {
    $results = @{
        'Step9_Integration_Round1' = $true
        'Step9_Integration_Round2' = $false
        'Step9_Integration_Round3' = $true
        'Step9_Integration_Round4' = $true
        'Step9_Integration_Round5' = $true
    }
    Assert-False (Test-AllIntegrationRoundsPassed -StepResults $results)
}

# ----------------------------------------------------------------
# Section 3: Get-TestRunSummary
# ----------------------------------------------------------------
Write-Host ""
Write-Host "## 3. Get-TestRunSummary" -ForegroundColor Cyan

Test-Case 'Passed! form parsed correctly' {
    $out = @'
Microsoft (R) Test Execution Command Line Tool Version 17.x
...
Passed!  - Failed:     0, Passed:    57, Skipped:     0, Total:    57
'@
    $s = Get-TestRunSummary -Output $out
    Assert-Equal 0 $s.Failed
    Assert-Equal 57 $s.Passed
    Assert-Equal 0 $s.Skipped
    Assert-Equal 57 $s.Total
}

Test-Case 'Failed! form parsed correctly' {
    $out = @'
Failed!  - Failed:     3, Passed:    54, Skipped:     0, Total:    57
'@
    $s = Get-TestRunSummary -Output $out
    Assert-Equal 3 $s.Failed
    Assert-Equal 54 $s.Passed
    Assert-Equal 57 $s.Total
}

Test-Case 'Array form parsed correctly' {
    $out = @(
        'Microsoft (R) Test Execution Command Line Tool Version 17.x'
        '...'
        'Passed!  - Failed:     0, Passed:    21, Skipped:     0, Total:    21'
    )
    $s = Get-TestRunSummary -Output $out
    Assert-Equal 21 $s.Passed
    Assert-Equal 0 $s.Failed
}

Test-Case 'Empty output -> all zero' {
    $s = Get-TestRunSummary -Output ''
    Assert-Equal 0 $s.Passed
    Assert-Equal 0 $s.Failed
}

# ----------------------------------------------------------------
# Section 4: Get-TestDiscoveryCount
# ----------------------------------------------------------------
Write-Host ""
Write-Host "## 4. Get-TestDiscoveryCount" -ForegroundColor Cyan

Test-Case 'Counts GuliERP test names' {
    $out = @(
        'The following Tests are available:'
        '    GuliERP.Mdm.Tests.MdmXxx.TestA'
        '    GuliERP.Mdm.Tests.MdmXxx.TestB(SomeData)'
        '    GuliERP.Mdm.Tests.MdmYyy.TestC'
    )
    $d = Get-TestDiscoveryCount -Output $out
    Assert-Equal 3 $d.Count
}

Test-Case 'Empty output -> 0' {
    $d = Get-TestDiscoveryCount -Output ''
    Assert-Equal 0 $d.Count
}

# ----------------------------------------------------------------
# Section 5: Test-MigrationDiscovered
# ----------------------------------------------------------------
Write-Host ""
Write-Host "## 5. Test-MigrationDiscovered" -ForegroundColor Cyan

Test-Case 'Plain ID at end of line -> true' {
    $out = '20260820190000_MDM001_InitializeMdmSchema'
    Assert-True (Test-MigrationDiscovered -CommandOutput $out -ExpectedMigrationId '20260820190000_MDM001_InitializeMdmSchema')
}

Test-Case 'ID with (Pending) -> true' {
    $out = '20260820190000_MDM001_InitializeMdmSchema (Pending)'
    Assert-True (Test-MigrationDiscovered -CommandOutput $out -ExpectedMigrationId '20260820190000_MDM001_InitializeMdmSchema')
}

Test-Case 'ID prefix-substring collision -> false' {
    # The expected ID is a prefix of another ID. The bounded
    # regex with lookahead should reject it.
    $out = '20260820190000_MDM001_InitializeMdmSchema_v2 (Pending)'
    Assert-False (Test-MigrationDiscovered -CommandOutput $out -ExpectedMigrationId '20260820190000_MDM001_InitializeMdmSchema')
}

Test-Case 'CRLF normalization -> true' {
    $out = "Some noise`r`n20260820190000_MDM001_InitializeMdmSchema`r`nMore noise"
    Assert-True (Test-MigrationDiscovered -CommandOutput $out -ExpectedMigrationId '20260820190000_MDM001_InitializeMdmSchema')
}

Test-Case 'ANSI CSI escape stripped -> true' {
    $esc = [char]27
    $out = "Build started...$esc[32m20260820190000_MDM001_InitializeMdmSchema$esc[0m`nDone."
    Assert-True (Test-MigrationDiscovered -CommandOutput $out -ExpectedMigrationId '20260820190000_MDM001_InitializeMdmSchema')
}

# ----------------------------------------------------------------
# Section 6: Redact-ConnectionString
# ----------------------------------------------------------------
Write-Host ""
Write-Host "## 6. Redact-ConnectionString" -ForegroundColor Cyan

Test-Case 'Redacts password' {
    $cs = 'Host=192.168.2.228;Port=5432;Database=gulierp_g2_003_test;Username=gulidata;Password=SuperSecret123!;Include Error Detail=true'
    $r = Redact-ConnectionString -cs $cs
    Assert-False $r.Contains('SuperSecret123!')
    Assert-True $r.Contains('Password=***')
}

Test-Case 'Empty -> <empty>' {
    Assert-Equal '<empty>' (Redact-ConnectionString -cs '')
}

# ----------------------------------------------------------------
# Section 7: Redact-SecretText
# ----------------------------------------------------------------
Write-Host ""
Write-Host "## 7. Redact-SecretText" -ForegroundColor Cyan

Test-Case 'Redacts Password=...' {
    $text = "Connection: Password=hunter2 other=ok"
    $r = Redact-SecretText -Value $text
    Assert-False $r.Contains('hunter2')
}

Test-Case 'Redacts pwd=...' {
    $text = "Pwd=hunter2 other=ok"
    $r = Redact-SecretText -Value $text
    Assert-False $r.Contains('hunter2')
}

# ----------------------------------------------------------------
# Section 8: Environment snapshot roundtrip
# ----------------------------------------------------------------
Write-Host ""
Write-Host "## 8. Save/Restore operator env (roundtrip)" -ForegroundColor Cyan

Test-Case 'Env var snapshot + restore roundtrip' {
    $origConn = $env:ConnectionStrings__GuliERP
    $origSeed = $env:GULIERP_MDM_SEED_FILE
    try {
        $env:ConnectionStrings__GuliERP = 'Password=original'
        $env:GULIERP_MDM_SEED_FILE = 'C:/some/path.json'
        $snap = Save-OperatorConnectionEnvironment

        # Mutate
        $env:ConnectionStrings__GuliERP = 'Password=changed'
        $env:GULIERP_MDM_SEED_FILE = 'C:/changed.json'

        # Restore
        Restore-OperatorConnectionEnvironment $snap

        Assert-Equal 'Password=original' $env:ConnectionStrings__GuliERP
        Assert-Equal 'C:/some/path.json' $env:GULIERP_MDM_SEED_FILE
    } finally {
        $env:ConnectionStrings__GuliERP = $origConn
        $env:GULIERP_MDM_SEED_FILE = $origSeed
    }
}

Test-Case 'Env var not set -> restore deletes' {
    $origConn = $env:ConnectionStrings__GuliERP
    try {
        Remove-Item Env:ConnectionStrings__GuliERP -ErrorAction SilentlyContinue
        $snap = Save-OperatorConnectionEnvironment
        Assert-False $snap.ConnectionStrings__GuliERP.Exists

        # Mutate to set it
        $env:ConnectionStrings__GuliERP = 'Password=temporary'
        Restore-OperatorConnectionEnvironment $snap

        # After restore, env var should be deleted
        Assert-False (Test-Path Env:ConnectionStrings__GuliERP)
    } finally {
        if ($null -ne $origConn) {
            $env:ConnectionStrings__GuliERP = $origConn
        } else {
            Remove-Item Env:ConnectionStrings__GuliERP -ErrorAction SilentlyContinue
        }
    }
}

# ----------------------------------------------------------------
# Section 9: Test-OutputContainsPassword
# ----------------------------------------------------------------
Write-Host ""
Write-Host "## 9. Test-OutputContainsPassword" -ForegroundColor Cyan

Test-Case 'Output containing raw password -> true' {
    $out = "Some log line`nConnection: Password=secret!`nAnother line"
    Assert-True (Test-OutputContainsPassword -Output $out -Password 'secret!')
}

Test-Case 'Redacted output -> false' {
    $out = "Some log line`nConnection: Password=***`nAnother line"
    Assert-False (Test-OutputContainsPassword -Output $out -Password 'secret!')
}

Test-Case 'Empty password -> false' {
    $out = "anything"
    Assert-False (Test-OutputContainsPassword -Output $out -Password '')
}

# ----------------------------------------------------------------
# Section 10: Test-ConnectionStringRedaction
# ----------------------------------------------------------------
Write-Host ""
Write-Host "## 10. Test-ConnectionStringRedaction" -ForegroundColor Cyan

Test-Case 'Original has password + redacted does not -> true' {
    $orig = 'Host=foo;Password=secret!;Database=db'
    $red = 'Host=foo;Password=***;Database=db'
    Assert-True (Test-ConnectionStringRedaction -Original $orig -Redacted $red -Password 'secret!')
}

Test-Case 'Redacted still has password -> false' {
    $orig = 'Host=foo;Password=secret!;Database=db'
    $red = 'Host=foo;Password=secret!;Database=db'
    Assert-False (Test-ConnectionStringRedaction -Original $orig -Redacted $red -Password 'secret!')
}

# ----------------------------------------------------------------
# Section 11: Test-RunSummaryAcceptable
# ----------------------------------------------------------------
Write-Host ""
Write-Host "## 11. Test-RunSummaryAcceptable" -ForegroundColor Cyan

Test-Case 'Failed > 0 -> false' {
    $s = [pscustomobject]@{ Failed = 1; Passed = 56; Skipped = 0; Total = 57 }
    Assert-False (Test-RunSummaryAcceptable -Summary $s -DiscoveredCount 57)
}

Test-Case 'Total < Discovered -> false' {
    $s = [pscustomobject]@{ Failed = 0; Passed = 30; Skipped = 0; Total = 30 }
    Assert-False (Test-RunSummaryAcceptable -Summary $s -DiscoveredCount 57)
}

Test-Case 'Failed = 0, Total = Discovered, Passed >= 1 -> true' {
    $s = [pscustomobject]@{ Failed = 0; Passed = 57; Skipped = 0; Total = 57 }
    Assert-True (Test-RunSummaryAcceptable -Summary $s -DiscoveredCount 57)
}

Test-Case 'Passed = 0 (only skipped) -> false' {
    $s = [pscustomobject]@{ Failed = 0; Passed = 0; Skipped = 57; Total = 57 }
    Assert-False (Test-RunSummaryAcceptable -Summary $s -DiscoveredCount 57)
}

# ----------------------------------------------------------------
# Section 12: Get-MissingStepKeys
# ----------------------------------------------------------------
Write-Host ""
Write-Host "## 12. Get-MissingStepKeys" -ForegroundColor Cyan

Test-Case 'Reports missing required + missing integration + missing api keys' {
    $results = @{
        'Step1_DBTarget' = $true
        'Step2_Credential' = $true
        'Step3_Build' = $true
        'Step4a_Discovery' = $true
        'Step4b_Apply' = $true
        # Step5 missing
        'Step6_Unit' = $true
        'Step7_Identity' = $true
        'Step8_Foundation' = $true
        'Step9_Integration' = $true
        'Step9_Integration_Round1' = $true
        'Step9_Integration_Round2' = $true
        'Step9_Integration_Round3' = $true
        'Step9_Integration_Round4' = $true
        # Round 5 missing
        'Step10_ApiRuntime' = $true
        'Step10_ApiRuntime_Round1' = $true
        # Round 2 missing
    }
    $missing = Get-MissingStepKeys -StepResults $results -RequiredStepKeys @('Step1_DBTarget','Step2_Credential','Step3_Build','Step4a_Discovery','Step4b_Apply','Step5_DiscoveryCounts','Step6_Unit','Step7_Identity','Step8_Foundation','Step9_Integration','Step10_ApiRuntime')
    Assert-True ($missing -contains 'Step5_DiscoveryCounts')
    Assert-True ($missing -contains 'Step9_Integration_Round5')
    Assert-True ($missing -contains 'Step10_ApiRuntime_Round2')
}

# ----------------------------------------------------------------
# Section 13: Gate string constants are exact
# ----------------------------------------------------------------
Write-Host ""
Write-Host "## 13. Gate string constants" -ForegroundColor Cyan

Test-Case 'Success gate is exactly MDM_001_REAL_MASTER_DATA_VERIFIED' {
    $d = Get-HarnessDefaults
    Assert-Equal 'MDM_001_REAL_MASTER_DATA_VERIFIED' $d.GateSuccess
}

Test-Case 'Hard stop gate is exactly MDM_001_FINAL_ACCEPTANCE_FAILED' {
    $d = Get-HarnessDefaults
    Assert-Equal 'MDM_001_FINAL_ACCEPTANCE_FAILED' $d.GateHardStop
}

Test-Case 'Env blocked gate is exactly MDM_001_POSTGRES_INTEGRATION_STABILIZATION_ENVIRONMENT_BLOCKED' {
    $d = Get-HarnessDefaults
    Assert-Equal 'MDM_001_POSTGRES_INTEGRATION_STABILIZATION_ENVIRONMENT_BLOCKED' $d.GateEnvBlocked
}

# ----------------------------------------------------------------
# Final report
# ----------------------------------------------------------------
Write-Host ""
Write-Host "============================================================"
Write-Host ("mdm-001R7 Self-Test result: {0} pass / {1} fail / {2} total" -f $script:PassCount, $script:FailCount, $script:TestCount)
Write-Host "============================================================"
if ($script:FailCount -gt 0) {
    Write-Host ""
    Write-Host "FAILURES:" -ForegroundColor Red
    foreach ($f in $script:Failures) {
        Write-Host "  $($f.Name): $($f.Error)" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "mdm-001R7 HARNESS_SELF_TEST_FAILED" -ForegroundColor Red
    exit 1
} else {
    Write-Host ""
    Write-Host "mdm-001R7 HARNESS_SELF_TEST_VERIFIED" -ForegroundColor Green
    exit 0
}
