# Canonical V0.9 offline backend regression. No operator, migration or seed execution.
[CmdletBinding()]
param([string]$ResultsDirectory = 'artifacts/baseline-offline-tests')
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$projects = @(
    'GuliERP.Foundation.Tests', 'GuliERP.Identity.Tests',
    'GuliERP.Identity.Bootstrap.Tests', 'GuliERP.Mdm.Tests',
    'GuliERP.Mdm.Bootstrap.Tests', 'GuliERP.DocumentKernel.Tests',
    'GuliERP.Sales.Tests', 'GuliERP.Purchase.Tests', 'GuliERP.Api.Tests'
)
# The excluded class is a real-DB seed loader, not an ordinary unit test.
$filter = 'FullyQualifiedName!~GuliERP.Mdm.Tests.ReferenceSeedIntegrationFacts'
$saved = @{}
$failed = @()
$summary = @()
try {
    foreach ($variable in @(Get-ChildItem Env: | Where-Object {
        $_.Name -match '(?i)ConnectionString|PGPASSWORD|GULIERP_FOUNDATION_CONNECTION'
    })) {
        $saved[$variable.Name] = $variable.Value
        Remove-Item -LiteralPath ('Env:' + $variable.Name)
    }
    $outputRoot = [IO.Path]::GetFullPath((Join-Path $root $ResultsDirectory))
    New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
    foreach ($project in $projects) {
        $projectPath = Join-Path $root "tests/$project/$project.csproj"
        Write-Host "Offline suite: $project (real-DB suites excluded)"
        & dotnet test $projectPath --no-restore --filter $filter --logger "trx;LogFileName=$project.trx" --results-directory $outputRoot -v quiet
        $exit = $LASTEXITCODE
        if ($exit -ne 0) { $failed += $project }
        $trxPath = Join-Path $outputRoot "$project.trx"
        if (-not (Test-Path -LiteralPath $trxPath)) { throw "Missing TRX: $project" }
        [xml]$trx = Get-Content -LiteralPath $trxPath -Raw
        $c = $trx.TestRun.ResultSummary.Counters
        if ([int]$c.total -eq 0) { throw "No tests executed: $project" }
        $summary += [pscustomobject]@{
            project = $project; total = [int]$c.total; passed = [int]$c.passed
            failed = [int]$c.failed; notExecuted = [int]$c.notExecuted; exitCode = $exit
        }
    }
    $summary | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputRoot 'summary.json') -Encoding utf8
    $summary | Format-Table
    if ($failed.Count) { throw "Offline regressions failed: $($failed -join ', ')" }
    Write-Output 'BASELINE_OFFLINE_REGRESSION_PASS'
}
finally {
    foreach ($name in $saved.Keys) { Set-Item -LiteralPath ('Env:' + $name) -Value $saved[$name] }
}
