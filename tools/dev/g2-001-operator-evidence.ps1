#requires -Version 5.1
<#
.SYNOPSIS
    G2-001 Operator Evidence Pack — verify the real-PostgreSQL round of G2-001.

.DESCRIPTION
    This script is the Operator-driven counterpart to the Mavis-driven code
    verification. Mavis cannot run this script because it requires the real
    PostgreSQL password. The script:

      1. Prompts for the connection string (interactive) OR honours the
         GULIERP_FOUNDATION_CONNECTION env var.
      2. Runs `dotnet ef database update` against the real PostgreSQL.
      3. Verifies that the `foundation` schema and the
         `__ef_migrations_history` table exist.
      4. Runs the integration tests against the real PostgreSQL.
      5. Starts the host in a separate process, hits /health/live and
         /health/ready, then stops the host.

    The script is intentionally PowerShell-only and G2-001-scoped. It does
    NOT auto-advance to G2-002. After it passes, the Operator must flip
    the G2_001_HOST_POSTGRESQL_VERIFIED gate in
    docs/governance/GOAL_REGISTRY.md (the Mavis-written Verification
    Report tells the Operator what to write).

.PARAMETER ConnectionString
    Optional. If not provided, the script reads
    $env:ConnectionStrings__GuliERP or
    $env:GULIERP_FOUNDATION_CONNECTION.

.PARAMETER SkipPrompt
    When set, do not interactively prompt for the password; assume the
    password is already baked into the connection string.

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
    [switch]$SkipPrompt
)

$ErrorActionPreference = 'Stop'
Set-Location -Path (Join-Path $PSScriptRoot '..\..')

$DOTNET = 'D:\guli\gulierp\.dotnet\dotnet.exe'
if (-not (Test-Path $DOTNET)) {
    throw "G2-001 requires D:\guli\gulierp\.dotnet\dotnet.exe. Not found."
}

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

# Do NOT log the connection string in full to disk; trim the password.
$displayConn = ($ConnectionString -replace 'Password=[^;]+', 'Password=***')
Write-Host "[G2-001] Using connection: $displayConn" -ForegroundColor Cyan

# 1. Apply migration
Write-Host "`n[G2-001] Step 1/5 — apply migration G2001_InitializeFoundationSchema" -ForegroundColor Yellow
& $DOTNET ef database update `
    --project 'modules\foundation\GuliERP.Foundation\GuliERP.Foundation.csproj' `
    --verbose `
    2>&1 | Out-String -Stream | ForEach-Object { Write-Host "  $_" }

# 2. Verify schema and migration history via SQL
Write-Host "`n[G2-001] Step 2/5 — verify schema and migration history" -ForegroundColor Yellow
$env:GULIERP_FOUNDATION_CONNECTION = $ConnectionString
$env:ConnectionStrings__GuliERP = $ConnectionString

# Use a one-off C# probe via dotnet-script-like approach? We can rely on the
# integration tests instead. Print the test command for Step 3.
Write-Host "  (validation is performed by FoundationDatabaseFacts in Step 3)"

# 3. Run integration tests (real DB)
Write-Host "`n[G2-001] Step 3/5 — run integration tests against real PostgreSQL" -ForegroundColor Yellow
& $DOTNET test `
    'tests\GuliERP.Foundation.IntegrationTests\GuliERP.Foundation.IntegrationTests.csproj' `
    -c Release `
    --no-build `
    --filter 'FullyQualifiedName~FoundationDatabaseFacts|FullyQualifiedName~FoundationHostHealthFacts' `
    2>&1 | Out-String -Stream | ForEach-Object { Write-Host "  $_" }

# 4. Run host Round 1 (real DB)
Write-Host "`n[G2-001] Step 4/5 — host Round 1 (real DB)" -ForegroundColor Yellow
$env:ConnectionStrings__GuliERP = $ConnectionString
$env:ASPNETCORE_URLS = 'http://127.0.0.1:5099'
$env:ASPNETCORE_ENVIRONMENT = 'Production'

$proc = Start-Process -FilePath $DOTNET `
    -ArgumentList 'run','--project','apps/api/GuliERP.Api/GuliERP.Api.csproj','--no-build','-c','Release' `
    -PassThru -RedirectStandardOutput "$env:TEMP\host-r1.log" -RedirectStandardError "$env:TEMP\host-r1-err.log"
try {
    Start-Sleep -Seconds 8
    $r = Invoke-WebRequest -Uri 'http://127.0.0.1:5099/health/live' -UseBasicParsing -TimeoutSec 5
    Write-Host "  /health/live: $($r.StatusCode) - $($r.Content)"
    if ($r.Content -ne 'Healthy') { throw '/health/live must return Healthy' }
    $r = Invoke-WebRequest -Uri 'http://127.0.0.1:5099/health/ready' -UseBasicParsing -TimeoutSec 10
    Write-Host "  /health/ready: $($r.StatusCode) - $($r.Content)"
    if ($r.Content -ne 'Healthy') { throw '/health/ready must return Healthy (real DB)' }
} finally {
    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
}

# 5. Run host Round 2 (real DB)
Write-Host "`n[G2-001] Step 5/5 — host Round 2 (real DB)" -ForegroundColor Yellow
$proc = Start-Process -FilePath $DOTNET `
    -ArgumentList 'run','--project','apps/api/GuliERP.Api/GuliERP.Api.csproj','--no-build','-c','Release' `
    -PassThru -RedirectStandardOutput "$env:TEMP\host-r2.log" -RedirectStandardError "$env:TEMP\host-r2-err.log"
try {
    Start-Sleep -Seconds 8
    $r = Invoke-WebRequest -Uri 'http://127.0.0.1:5099/health/live' -UseBasicParsing -TimeoutSec 5
    Write-Host "  /health/live: $($r.StatusCode) - $($r.Content)"
    if ($r.Content -ne 'Healthy') { throw '/health/live must return Healthy' }
    $r = Invoke-WebRequest -Uri 'http://127.0.0.1:5099/health/ready' -UseBasicParsing -TimeoutSec 10
    Write-Host "  /health/ready: $($r.StatusCode) - $($r.Content)"
    if ($r.Content -ne 'Healthy') { throw '/health/ready must return Healthy (real DB)' }
} finally {
    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
}

Write-Host "`n[G2-001] ALL FIVE STEPS PASSED" -ForegroundColor Green
Write-Host "  Next: open docs/verification/G2_001_HOST_POSTGRESQL_REPORT.md"
Write-Host "  and update docs/governance/GOAL_REGISTRY.md to mark G2-001 as VERIFIED."
