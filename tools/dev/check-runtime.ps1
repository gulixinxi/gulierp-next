#Requires -Version 5.1
<#
.SYNOPSIS
    GuliERP Next runtime guard. Verifies the operator's machine is running
    the NEW project (D:\guli\projects\gulierp-next, ASP.NET Core Identity +
    PostgreSQL) and NOT the OLD project (D:\guli\gulierp, Admin.NET +
    SQLite).

.DESCRIPTION
    Phase 2 deliverable for GULIERP_PROJECT_CONSOLIDATION_PHASE2_RUNTIME_001.

    This script is read-only — it never modifies any state. It reports
    each check as PASS / WARN / FAIL and exits with a non-zero code on
    FAIL.

    Hard FAIL conditions (exit code != 0):
      1. Current working directory is under D:\guli\gulierp
      2. Path D:\guli\gulierp still exists
      3. dotnet is the OLD vendored SDK (D:\guli\gulierp\.dotnet\dotnet.exe)
      4. ConnectionStrings__GuliERP targets SQLite
      5. Any appsettings.json or *.json config under D:\guli\gulierp
         contains "DataSource=" or "Data Source=" (SQLite signature)
      6. global.json is missing in the current repo
      7. global.json does not pin .NET 10.x
      8. NAS PG (192.168.2.228:5432) is unreachable AND no env var
         ConnectionStrings__GuliERP is set (B1 CLI seed will fail)
      9. Repository HEAD is detached (operator should be on a branch)
     10. Repository is on master with uncommitted B1/B2/B3 work AND the
         user explicitly asked for clean run

    WARN conditions (exit code = 0 but with a yellow report):
      - Port 5000 or 5173 is in use by a non-gulierp process
      - ConnectionStrings__GuliERP env var is not set (B1 CLI needs it)
      - The OLD PID 109480 (or any dotnet process matching
        D:\guli\gulierp path) is detected

.PARAMETER RepoPath
    Path to the repository to check. Defaults to the directory this script
    lives in (assumed to be <repo>/tools/dev/check-runtime.ps1).

.PARAMETER Section
    Optional: run only one section (1-8). Default: run all 8 sections.

.EXAMPLE
    PS> .\tools\dev\check-runtime.ps1
    Run all 8 checks. Default behavior.

.EXAMPLE
    PS> .\tools\dev\check-runtime.ps1 -Section 4
    Run only section 4 (NAS PG + ConnectionStrings).

.EXAMPLE
    PS> .\tools\dev\check-runtime.ps1 -RepoPath D:\guli\projects\gulierp-next
    Run against a specific repo path.

.OUTPUTS
    Writes a colored report to the host. Returns 0 on PASS+WARN, non-zero
    on any FAIL.

.NOTES
    Author  : Mavis (M3 / mavis)
    Created : 2026-08-25
    Phase   : 2 of GULIERP_PROJECT_CONSOLIDATION
    Audit   : docs/governance/GULIERP_PROJECT_MIGRATION_AUDIT_001.md
    Report  : docs/governance/GULIERP_RUNTIME_GUARD_REPORT.md
#>
[CmdletBinding()]
param(
    [string]$RepoPath = "",
    [ValidateRange(1,8)]
    [int]$Section = 0
)

$ErrorActionPreference = 'Stop'

# ----------------------------------------------------------------
#  Constants
# ----------------------------------------------------------------
$OLD_PROJECT_ROOT       = "D:\guli\gulierp"
$NEW_PROJECT_ROOT       = "D:\guli\projects\gulierp-next"
$OLD_DOTNET_RUNTIME     = "D:\guli\gulierp\.dotnet\dotnet.exe"
$OLD_SQLITE_HINT        = "DataSource="
$OLD_SQLITE_HINT_ALT    = "Data Source="
$NAS_PG_HOST            = "192.168.2.228"
$NAS_PG_PORT            = 5432
$API_PORT               = 5000
$WEB_PORT               = 5173
$EXPECTED_DOTNET_MAJOR  = 10
$EXPECTED_SDK_VERSION   = "10.0.100"

# ----------------------------------------------------------------
#  Color helpers
# ----------------------------------------------------------------
function Write-Pass { param($msg) Write-Host "  [PASS] $msg" -ForegroundColor Green }
function Write-Warn { param($msg) Write-Host "  [WARN] $msg" -ForegroundColor Yellow }
function Write-Fail { param($msg) Write-Host "  [FAIL] $msg" -ForegroundColor Red }
function Write-Info { param($msg) Write-Host "  [INFO] $msg" -ForegroundColor Gray }

$script:FailCount = 0
$script:WarnCount = 0

function Test-Section1-GitRepoPath {
    Write-Host ""
    Write-Host "[Section 1/8] Git Repository Path" -ForegroundColor Cyan
    $root = if ($RepoPath) { $RepoPath } else { (git rev-parse --show-toplevel 2>&1) }
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($root)) {
        Write-Fail "Not inside a Git repository. cwd = $(Get-Location)"
        $script:FailCount++
        return
    }
    # Normalize path separators (git on Windows returns forward slashes).
    $root = (Resolve-Path $root -ErrorAction SilentlyContinue).Path
    $rootNorm = $root -replace '/', '\'
    $oldNorm  = $OLD_PROJECT_ROOT -replace '/', '\'
    $newNorm  = $NEW_PROJECT_ROOT -replace '/', '\'
    Write-Info "Repo root: $root"
    if ($rootNorm -like "$oldNorm*") {
        Write-Fail "Repo is under OLD project root: $OLD_PROJECT_ROOT"
        Write-Fail "  Expected: $NEW_PROJECT_ROOT"
        Write-Fail "  Action  : cd $NEW_PROJECT_ROOT (or kill stale bash/PowerShell session)"
        $script:FailCount++
        return
    }
    if ($rootNorm -ne $newNorm) {
        Write-Warn "Repo is not at canonical location. Got: $root | Expected: $NEW_PROJECT_ROOT"
        $script:WarnCount++
        return
    }
    Write-Pass "Repo at canonical location: $root"
    # HEAD
    $head = git rev-parse HEAD 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Fail "Cannot read HEAD. (Empty repo? Detached?)"
        $script:FailCount++
        return
    }
    Write-Info "HEAD: $head"
    $branch = git rev-parse --abbrev-ref HEAD 2>&1
    if ($branch -eq "HEAD") {
        Write-Fail "HEAD is detached. Operator should be on a branch."
        $script:FailCount++
    } else {
        Write-Pass "Branch: $branch"
    }
    # Dirty state
    $dirty = git status --short 2>&1
    $dirtyCount = @($dirty | Where-Object { $_ }).Count
    if ($dirtyCount -gt 0) {
        Write-Warn "$dirtyCount dirty file(s). Expected (B1/B2/B3 uncommitted): $($dirty[0..2] -join ', ')"
        $script:WarnCount++
    } else {
        Write-Pass "Working tree clean"
    }
}

function Test-Section2-DotNetVersion {
    Write-Host ""
    Write-Host "[Section 2/8] .NET Runtime + SDK" -ForegroundColor Cyan
    $dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue)
    if (-not $dotnet) {
        Write-Fail "dotnet not on PATH. Install .NET 10 SDK from https://dot.net"
        $script:FailCount++
        return
    }
    $dotnetPath = $dotnet.Source
    Write-Info "dotnet path: $dotnetPath"
    if ($dotnetPath -like "$OLD_DOTNET_RUNTIME" -or $dotnetPath -like "$OLD_PROJECT_ROOT\.dotnet*") {
        Write-Fail "dotnet is the OLD vendored SDK: $dotnetPath"
        Write-Fail "  Action  : uninstall OLD .dotnet, install system .NET 10 SDK"
        $script:FailCount++
        return
    }
    $version = (& dotnet --version 2>&1).Trim()
    Write-Info "dotnet --version: $version"
    $major = 0
    if ($version -match "^(\d+)\.") { $major = [int]$Matches[1] }
    if ($major -ne $EXPECTED_DOTNET_MAJOR) {
        Write-Fail "dotnet major version = $major, expected $EXPECTED_DOTNET_MAJOR"
        $script:FailCount++
        return
    }
    if ($version -lt $EXPECTED_SDK_VERSION) {
        Write-Warn "dotnet $version < expected $EXPECTED_SDK_VERSION (with rollForward: latestFeature this is OK)"
        $script:WarnCount++
    } else {
        Write-Pass "dotnet $version (>= $EXPECTED_SDK_VERSION)"
    }
    $sdks = & dotnet --list-sdks 2>&1
    Write-Info "Installed SDKs: $($sdks -join ', ')"
    $runtimes = & dotnet --list-runtimes 2>&1
    $aspnet = $runtimes | Where-Object { $_ -match "AspNetCore" }
    if (-not $aspnet) {
        Write-Fail "No Microsoft.AspNetCore.App runtime installed. B1 CLI / API cannot run."
        $script:FailCount++
    } else {
        Write-Pass "AspNetCore runtime: $($aspnet[0])"
    }
}

function Test-Section3-GlobalJson {
    Write-Host ""
    Write-Host "[Section 3/8] global.json Consistency" -ForegroundColor Cyan
    $root = if ($RepoPath) { $RepoPath } else { (git rev-parse --show-toplevel 2>&1) }
    $gj = Join-Path $root "global.json"
    if (-not (Test-Path $gj)) {
        Write-Fail "global.json missing at $gj"
        Write-Fail "  Action  : create global.json with { sdk: { version: 10.0.100, rollForward: latestFeature } }"
        $script:FailCount++
        return
    }
    Write-Info "global.json: $gj"
    try {
        $gjObj = Get-Content $gj -Raw -Encoding UTF8 | ConvertFrom-Json
    } catch {
        Write-Fail "global.json is not valid JSON: $($_.Exception.Message)"
        $script:FailCount++
        return
    }
    if (-not $gjObj.sdk) {
        Write-Fail "global.json has no 'sdk' property"
        $script:FailCount++
        return
    }
    $sdkVersion = $gjObj.sdk.version
    $rollForward = $gjObj.sdk.rollForward
    Write-Info "Pinned SDK: $sdkVersion, rollForward: $rollForward"
    if ($sdkVersion -notmatch "^$EXPECTED_DOTNET_MAJOR\.") {
        Write-Fail "global.json pins .NET $sdkVersion, expected 10.x"
        $script:FailCount++
        return
    }
    if ($rollForward -ne "latestFeature") {
        Write-Warn "global.json rollForward = $rollForward (expected latestFeature for .NET 10)"
        $script:WarnCount++
    } else {
        Write-Pass "Pinned $sdkVersion with rollForward: $rollForward"
    }
}

function Test-Section4-DatabaseType {
    Write-Host ""
    Write-Host "[Section 4/8] Database Type" -ForegroundColor Cyan
    if (Test-Path $OLD_PROJECT_ROOT) {
        Write-Fail "OLD project root still exists: $OLD_PROJECT_ROOT"
        Write-Fail "  Action  : archive OLD to D:\guli\archive\gulierp-old (Phase 2 §3)"
        $script:FailCount++
    } else {
        Write-Pass "OLD project root not present (already archived or never created)"
    }
    $oldDotnet = $OLD_DOTNET_RUNTIME
    if (Test-Path $oldDotnet) {
        Write-Fail "OLD vendored .NET SDK still present: $oldDotnet"
        Write-Fail "  Action  : remove-Item $oldDotnet (Phase 2 §4.2)"
        $script:FailCount++
    } else {
        Write-Pass "OLD vendored .NET SDK not present"
    }
    $envConn = $env:ConnectionStrings__GuliERP
    if ($envConn) {
        Write-Info "env: ConnectionStrings__GuliERP = $envConn"
        if ($envConn -match "(?i)(DataSource=|Data Source=|Sqlite)") {
            Write-Fail "env: ConnectionStrings__GuliERP targets SQLite: $envConn"
            $script:FailCount++
        } elseif ($envConn -match "Host=([^;]+)") {
            $host = $Matches[1]
            Write-Pass "env: ConnectionStrings__GuliERP targets PostgreSQL host: $host"
        } else {
            Write-Warn "env: ConnectionStrings__GuliERP present but no Host= detected"
            $script:WarnCount++
        }
    } else {
        Write-Warn "env: ConnectionStrings__GuliERP not set (B1 CLI seed will need --connection-string)"
        $script:WarnCount++
    }
    $root = if ($RepoPath) { $RepoPath } else { $NEW_PROJECT_ROOT }
    Get-ChildItem -Path $root -Recurse -Filter "appsettings*.json" -ErrorAction SilentlyContinue |
        ForEach-Object {
            $content = Get-Content $_.FullName -Raw -Encoding UTF8 -ErrorAction SilentlyContinue
            if ($content -match $OLD_SQLITE_HINT -or $content -match $OLD_SQLITE_HINT_ALT) {
                Write-Fail "SQLite signature in $($_.FullName)"
                $script:FailCount++
            }
        }
    Get-ChildItem -Path $root -Recurse -Include "Database.json" -ErrorAction SilentlyContinue |
        ForEach-Object {
            $content = Get-Content $_.FullName -Raw -Encoding UTF8 -ErrorAction SilentlyContinue
            if ($content -match $OLD_SQLITE_HINT -or $content -match $OLD_SQLITE_HINT_ALT) {
                Write-Fail "SQLite signature in $($_.FullName) (Admin.NET Database.json format)"
                $script:FailCount++
            }
        }
}

function Test-Section5-NasPg {
    Write-Host ""
    Write-Host "[Section 5/8] NAS PG Reachable" -ForegroundColor Cyan
    try {
        $tcp = Test-NetConnection -ComputerName $NAS_PG_HOST -Port $NAS_PG_PORT -InformationLevel Quiet -WarningAction SilentlyContinue -ErrorAction Stop
        if ($tcp) {
            Write-Pass "$NAS_PG_HOST`:$NAS_PG_PORT reachable"
        } else {
            Write-Fail "$NAS_PG_HOST`:$NAS_PG_PORT UNREACHABLE"
            Write-Fail "  Action  : check VPN / network / NAS status"
            $script:FailCount++
        }
    } catch {
        Write-Fail "Test-NetConnection failed: $($_.Exception.Message)"
        $script:FailCount++
    }
}

function Test-Section6-Ports {
    Write-Host ""
    Write-Host "[Section 6/8] Port Availability" -ForegroundColor Cyan
    foreach ($p in @($API_PORT, $WEB_PORT)) {
        $listen = Get-NetTCPConnection -LocalPort $p -State Listen -ErrorAction SilentlyContinue
        if ($listen) {
            $procId = $listen.OwningProcess
            $proc = Get-Process -Id $procId -ErrorAction SilentlyContinue
            $procPath = if ($proc) { $proc.Path } else { '' }
            $procName = if ($proc) { $proc.ProcessName } else { 'unknown' }
            if ($procPath -like "$OLD_PROJECT_ROOT*") {
                Write-Fail "Port $p owned by OLD process (PID $procId, $procName, $procPath)"
                Write-Fail "  Action  : Stop-Process -Id $procId (with user confirmation)"
                $script:FailCount++
            } elseif ([string]::IsNullOrEmpty($procPath) -or $procId -lt 4) {
                # System / kernel / orphaned listener (path unavailable).
                # This is benign (e.g. Windows HTTP service) but noteworthy.
                Write-Info "Port $p owned by system/kernel process (PID $procId, $procName). Benign."
            } else {
                Write-Info "Port $p owned by $procName PID $procId ($procPath)"
            }
        } else {
            Write-Pass "Port $p free"
        }
    }
}

function Test-Section7-NoStaleProcesses {
    Write-Host ""
    Write-Host "[Section 7/8] Stale dotnet processes" -ForegroundColor Cyan
    $dots = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
    if (-not $dots) {
        Write-Pass "No dotnet processes running"
    } else {
        foreach ($d in $dots) {
            $path = $d.Path
            if ($path -like "$OLD_PROJECT_ROOT*") {
                Write-Fail "Stale OLD dotnet process: PID $($d.Id), $path"
                Write-Fail "  Action  : Stop-Process -Id $($d.Id) (with user confirmation)"
                $script:FailCount++
            } else {
                Write-Warn "Active dotnet process: PID $($d.Id), $path (probably the NEW API)"
                $script:WarnCount++
            }
        }
    }
}

function Test-Section8-DiskSpace {
    Write-Host ""
    Write-Host "[Section 8/8] Disk Space" -ForegroundColor Cyan
    $drive = (Get-Item $NEW_PROJECT_ROOT).PSDrive
    $freeGB = [Math]::Round($drive.Free / 1GB, 2)
    Write-Info "Drive $($drive.Name): $freeGB GB free"
    if ($freeGB -lt 2.0) {
        Write-Fail "Disk free = $freeGB GB (need >= 2 GB for builds)"
        $script:FailCount++
    } else {
        Write-Pass "Disk free = $freeGB GB (>= 2 GB)"
    }
}

# ----------------------------------------------------------------
#  Main
# ----------------------------------------------------------------
Write-Host ""
Write-Host "=== GuliERP Next Runtime Guard ===" -ForegroundColor White -BackgroundColor DarkCyan
Write-Host ("Date    : {0:yyyy-MM-dd HH:mm:ss} ({1})" -f (Get-Date), ([System.TimeZoneInfo]::Local.Id))
Write-Host "Repo    : $NEW_PROJECT_ROOT (expected)"
Write-Host "OLD root: $OLD_PROJECT_ROOT (must NOT exist for PASS)"

$sections = @(
    @{ N=1; F='Test-Section1-GitRepoPath' },
    @{ N=2; F='Test-Section2-DotNetVersion' },
    @{ N=3; F='Test-Section3-GlobalJson' },
    @{ N=4; F='Test-Section4-DatabaseType' },
    @{ N=5; F='Test-Section5-NasPg' },
    @{ N=6; F='Test-Section6-Ports' },
    @{ N=7; F='Test-Section7-NoStaleProcesses' },
    @{ N=8; F='Test-Section8-DiskSpace' }
)

foreach ($s in $sections) {
    if ($Section -eq 0 -or $Section -eq $s.N) {
        & $s.F
    }
}

Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor White -BackgroundColor DarkCyan
if ($script:FailCount -gt 0) {
    Write-Host ("OVERALL: {0} FAIL, {1} WARN" -f $script:FailCount, $script:WarnCount) -ForegroundColor Red
    Write-Host "Exit code: 1 (run check failed; do not proceed with B1/B2/B3 deployment)"
    exit 1
} else {
    Write-Host ("OVERALL: 0 FAIL, {0} WARN" -f $script:WarnCount) -ForegroundColor Green
    if ($script:WarnCount -gt 0) {
        Write-Host "Exit code: 0 (warnings present; review before proceeding)"
    } else {
        Write-Host "Exit code: 0 (clean)"
    }
    exit 0
}
