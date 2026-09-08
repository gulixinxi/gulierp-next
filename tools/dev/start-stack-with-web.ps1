#requires -Version 5.1
<#
.SYNOPSIS
    One-command launcher for the full GuliERP dev stack: backend (5000) + web (5173).

.DESCRIPTION
    Thin wrapper around start-stack.ps1 that:
      - starts BOTH backend AND web (no -SkipFrontend) by default
      - prints a friendly banner with the URLs you'll need to open
      - runs a post-launch health check on both ports (cancellable via -NoHealthCheck)
      - forwards any extra params to start-stack.ps1 unchanged

    For the legacy single-port (backend-only) flow, use start-stack.ps1 directly.
    For stopping, use stop-stack.ps1 (handles both backend + frontend tree).

.EXAMPLE
    pwsh tools/dev/start-stack-with-web.ps1
    # Default: backend on 5000, web on 5173, both health-checked.

.EXAMPLE
    pwsh tools/dev/start-stack-with-web.ps1 -BackendPort 5001 -FrontendPort 5174
    # Custom ports (e.g. when 5000/5173 are occupied by another instance).

.EXAMPLE
    pwsh tools/dev/start-stack-with-web.ps1 -NoHealthCheck
    # Skip the post-launch verification (faster, useful in CI smoke tests).

.EXAMPLE
    pwsh tools/dev/stop-stack.ps1
    # Stops whatever was started (reads .stack-pids.json + scans default ports).
#>

[CmdletBinding()]
param(
    [int]$BackendPort  = 5000,
    [int]$FrontendPort = 5173,
    [string]$DbUser     = 'gulidata',
    [string]$ExpectedDb = 'gulierp_g2_003_test',
    [switch]$SkipBackend,
    [switch]$SkipFrontend,
    [switch]$NoHealthCheck
)

$ErrorActionPreference = 'Stop'

$ScriptDir  = Split-Path -Parent $PSCommandPath
$Launcher   = Join-Path $ScriptDir 'start-stack.ps1'

if (-not (Test-Path $Launcher)) {
    throw "Cannot find $Launcher. Run from the repo root or fix the script location."
}

# ---- Build args to forward to start-stack.ps1 ----
$forward = @()
if ($PSBoundParameters.ContainsKey('BackendPort'))  { $forward += '-BackendPort';  $forward += [string]$BackendPort }
if ($PSBoundParameters.ContainsKey('FrontendPort')) { $forward += '-FrontendPort'; $forward += [string]$FrontendPort }
if ($PSBoundParameters.ContainsKey('DbUser'))       { $forward += '-DbUser';       $forward += $DbUser }
if ($PSBoundParameters.ContainsKey('ExpectedDb'))   { $forward += '-ExpectedDb';   $forward += $ExpectedDb }
if ($SkipBackend)  { $forward += '-SkipBackend' }
if ($SkipFrontend) { $forward += '-SkipFrontend' }

# ---- Banner ----
Write-Host ''
Write-Host '===========================================' -ForegroundColor Cyan
Write-Host ' GuliERP stack (backend + web) launcher'  -ForegroundColor Cyan
Write-Host '===========================================' -ForegroundColor Cyan
Write-Host ("  Backend  : http://localhost:{0}  (GuliERP.Api, ASP.NET Core)" -f $BackendPort)
if (-not $SkipFrontend) {
    Write-Host ("  Frontend : http://localhost:{0}  (Vite dev server)"          -f $FrontendPort)
    Write-Host "            NOTE: must be 'localhost', not '127.0.0.1' (Vite binds to ::1 IPv6)"
}
Write-Host ("  DB User  : {0}" -f $DbUser)
Write-Host ("  DB Name  : {0}" -f $ExpectedDb)
Write-Host ''

# ---- Run the underlying launcher (async, so we don't block on its tail) ----
# start-stack.ps1 currently waits for the foreground frontend child before
# returning. To avoid hanging the wrapper, we run it via Start-Process so
# it stays out of our pipeline; we then poll the services ourselves.
Write-Host 'Launching start-stack.ps1 in the background...' -ForegroundColor DarkGray
$argLine = ($forward | ForEach-Object { if ($_ -match '\s') { '"' + $_ + '"' } else { $_ } }) -join ' '
$launcherPsi = New-Object System.Diagnostics.ProcessStartInfo
$launcherPsi.FileName = 'pwsh.exe'
$launcherPsi.Arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$Launcher`" $argLine"
$launcherPsi.WorkingDirectory = (Get-Location)
$launcherPsi.UseShellExecute = $false
$launcherPsi.CreateNoWindow = $true
$launcherPsi.RedirectStandardOutput = $true
$launcherPsi.RedirectStandardError = $true
$launcherProc = [System.Diagnostics.Process]::Start($launcherPsi)
Write-Host ("  launcher pwsh PID: {0}" -f $launcherProc.Id)

$sw = [System.Diagnostics.Stopwatch]::StartNew()

# ---- Post-launch health check (best-effort) ----
if ($NoHealthCheck) {
    Write-Host ''
    Write-Host 'Skipped post-launch health check (-NoHealthCheck).' -ForegroundColor DarkGray
    exit 0
}

Write-Host ''
Write-Host '=== Post-launch health check ===' -ForegroundColor Cyan
# Kestrel + first JIT compile can take 5-15s after the launcher says
# 'health: live', especially with WIP builds. Poll up to 30s per service.
Start-Sleep -Seconds 5

$checks = @()
if (-not $SkipBackend)  { $checks += [pscustomobject]@{ Name = 'Backend '; Url = "http://localhost:$BackendPort/health/live" } }
if (-not $SkipFrontend) { $checks += [pscustomobject]@{ Name = 'Frontend'; Url = "http://localhost:$FrontendPort/" } }

$allOk = $true
foreach ($c in $checks) {
    $ok = $false
    $lastErr = ''
    for ($attempt = 1; $attempt -le 6; $attempt++) {
        try {
            $r = Invoke-WebRequest -Uri $c.Url -UseBasicParsing -TimeoutSec 5
            if ($r.StatusCode -eq 200) {
                Write-Host ("  {0}  {1}  -> HTTP 200 OK  (attempt {2})" -f $c.Name, $c.Url, $attempt) -ForegroundColor Green
                $ok = $true
                break
            } else {
                $lastErr = "HTTP $($r.StatusCode)"
            }
        } catch {
            $lastErr = ($_.Exception.Message -split "`n")[0]
        }
        if ($attempt -lt 6) {
            Write-Host ("  {0}  ...waiting (attempt {1}/6, last: {2})" -f $c.Name, $attempt, $lastErr) -ForegroundColor DarkGray
            Start-Sleep -Seconds 5
        }
    }
    if (-not $ok) {
        Write-Host ("  {0}  {1}  -> FAIL after 6 attempts: {2}" -f $c.Name, $c.Url, $lastErr) -ForegroundColor Red
        $allOk = $false
    }
}

Write-Host ''
if ($allOk) {
    Write-Host ('All services ready in {0:mm\:ss}.' -f $sw.Elapsed) -ForegroundColor Green
    if (-not $SkipFrontend) {
        Write-Host ''
        Write-Host 'Browser login hints:' -ForegroundColor Cyan
        Write-Host '  URL   : http://localhost:' $FrontendPort '/'
        Write-Host '  User  : g3r1c_sales_operator    (or _mdm_operator / _employee_operator / _sys_admin)'
        Write-Host '  Pass  : G3r1c-{Sys|Mdm|Emp|Sal}-2026-Pass!'
    }
} else {
    Write-Host 'One or more services did not respond. Common causes:' -ForegroundColor Yellow
    Write-Host '  - .env.local missing or has placeholder password (start-stack prompts)'
    Write-Host '  - 5000 / 5173 already in use by a different process'
    Write-Host '  - DB unreachable from this host'
    Write-Host 'Check .stack-logs/backend-*.log and .stack-logs/frontend-*.log.'
}
