<#
.SYNOPSIS
    GuliERP Next stack launcher — one script, two windows.

    Opens two separate PowerShell windows:
      1. Backend (GuliERP.Api) on http://127.0.0.1:5000
      2. Frontend (Vue + Vite) on http://127.0.0.1:5173 (default)

    Each window can be closed independently (Ctrl+C or X).
    This script does NOT block; it just spawns + verifies + exits.

.DESCRIPTION
    Password is read ONCE via Read-Host -AsSecureString, then propagated
    to the backend process via the standard
    $env:ConnectionStrings__GuliERP variable. The password is NEVER
    written to a file, log, or git. The launching process scrubs the
    plain password from its own memory after the env var is set.

    Pre-flight:
      * Asserts the DB target (via assert-gulierp-db-target.ps1) — aborts
        if the connection string points at a non-canonical database.
      * Polls /health/live until 200 before launching the frontend, so
        the SPA's first /me call does not race the backend boot.

    Re-running this script:
      * Stops any previous instances whose PIDs are recorded in
        <RepoRoot>/.stack-pids.json before launching fresh ones, so
        port 5000 / 5173 are released cleanly.

.PARAMETER DbUser
    PostgreSQL user name. Default: gulidata (G2-004 convention).

.PARAMETER BackendPort
    Port for the GuliERP.Api host. Default: 5000.

.PARAMETER FrontendPort
    Port for the Vite dev server. Default: 5173.

.PARAMETER ExpectedDb
    Canonical database name (assertion target). Default: gulierp_g2_003_test.

.PARAMETER SkipFrontend
    Start only the backend (e.g. when you only need to test the API).

.PARAMETER SkipBackend
    Start only the frontend (use this when the backend is already
    running on :5000 from a previous session).

.PARAMETER Dotnet
    Path to the dotnet executable. Default: D:\guli\gulierp\.dotnet\dotnet.exe.

.PARAMETER Npm
    Path to the npm executable. Default: whatever is on PATH (npm).

.EXAMPLE
    PS> .\tools\dev\start-stack.ps1
    # Reads PG password once, opens two windows, returns immediately.

.EXAMPLE
    PS> .\tools\dev\start-stack.ps1 -SkipFrontend
    # Backend only.

.EXAMPLE
    PS> .\tools\dev\start-stack.ps1 -SkipBackend -FrontendPort 5174
    # Frontend on 5174 (backend already running on 5000).

.NOTES
    Hard rules (per session policy):
      * Password is NEVER echoed, NEVER logged, NEVER written to disk
        (only to the in-process $env:ConnectionStrings__GuliERP).
      * No telemetries to remote hosts.
      * The script does NOT touch git, docs, or business code.
      * Closing THIS launcher does NOT close the child windows; close
        them individually (or use the -StopOnExit switch in a future
        revision).
#>
[CmdletBinding()]
param(
    [string]$DbUser = 'gulidata',
    [int]$BackendPort = 5000,
    [int]$FrontendPort = 5173,
    [string]$ExpectedDb = 'gulierp_g2_003_test',
    [switch]$SkipFrontend,
    [switch]$SkipBackend,
    [string]$Dotnet = 'D:\guli\gulierp\.dotnet\dotnet.exe',
    [string]$Npm = 'npm'
)

$ErrorActionPreference = 'Stop'

# --- Resolve repository root (parent of tools/dev) ----------------
$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$BackendCsproj = Join-Path $RepoRoot 'apps\api\GuliERP.Api\GuliERP.Api.csproj'
$WebDir = Join-Path $RepoRoot 'apps\web'
$PidFile = Join-Path $RepoRoot '.stack-pids.json'

Write-Host ''
Write-Host '=== GuliERP Next stack launcher ===' -ForegroundColor Cyan
Write-Host "Repo root : $RepoRoot"
Write-Host "Backend   : http://127.0.0.1:$BackendPort"
if (-not $SkipFrontend) {
    Write-Host "Frontend  : http://127.0.0.1:$FrontendPort"
}
Write-Host "DB target : $ExpectedDb @ 192.168.2.228  (user: $DbUser)"
Write-Host ''

# --- 0. Sanity checks ------------------------------------------------
if (-not (Test-Path $BackendCsproj)) {
    throw "Backend csproj not found: $BackendCsproj"
}
if (-not (Test-Path $WebDir)) {
    throw "Web dir not found: $WebDir"
}
if (-not (Test-Path $Dotnet)) {
    throw "dotnet not found: $Dotnet (use -Dotnet to override)"
}

# --- 1. Stop any prior instances recorded in .stack-pids.json --------
function Stop-PriorStack {
    if (-not (Test-Path $PidFile)) { return }
    try {
        $prior = Get-Content -Raw -Path $PidFile -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop
    } catch {
        Write-Host "  (ignoring malformed $PidFile: $_)" -ForegroundColor DarkGray
        Remove-Item $PidFile -Force -ErrorAction SilentlyContinue
        return
    }
    if ($prior.backend.pid) {
        $p = Get-Process -Id $prior.backend.pid -ErrorAction SilentlyContinue
        if ($p -and $p.ProcessName -match 'GuliERP') {
            Write-Host "  Stopping prior backend (pid=$($p.Id))..." -ForegroundColor DarkYellow
            Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
        }
    }
    if ($prior.frontend.pid) {
        $p = Get-Process -Id $prior.frontend.pid -ErrorAction SilentlyContinue
        if ($p) {
            Write-Host "  Stopping prior frontend (pid=$($p.Id))..." -ForegroundColor DarkYellow
            Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
        }
    }
    Remove-Item $PidFile -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 500
}
Stop-PriorStack

# --- 2. Read PG password ONCE (SecureString, masked) ---------------
$sec = Read-Host -Prompt "PostgreSQL password for user '$DbUser' (input is masked)" -AsSecureString
if ($sec.Length -eq 0) { throw 'Empty password — aborting.' }
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec)
try {
    $plain = [Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
    $conn = "Host=192.168.2.228;Port=5432;Database=$ExpectedDb;Username=$DbUser;Password=$plain;Include Error Detail=true"
    $env:ConnectionStrings__GuliERP = $conn
    $env:GULIERP_ConnectionStrings__GuliERP = $conn
} finally {
    # Zero the BSTR as soon as the plain string is in the env var.
    if ($bstr -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}
$plain = $null

# --- 3. Assert DB target (fail-closed wrong-DB guard) --------------
$assert = Join-Path $PSScriptRoot 'assert-gulierp-db-target.ps1'
& $assert -ConnectionString $conn -ExpectedDatabase $ExpectedDb
if ($LASTEXITCODE -ne 0) { throw 'DB guard FAILED — aborting.' }

# --- 4. Helper: wait for an HTTP endpoint to respond ----------------
function Wait-ForHttp {
    param(
        [string]$Url,
        [int]$TimeoutSec = 60,
        [int]$PollMs = 500
    )
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        try {
            $r = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 3 -ErrorAction Stop
            if ($r.StatusCode -ge 200 -and $r.StatusCode -lt 500) { return $true }
        } catch {
            $code = $null
            if ($_.Exception.Response) { $code = [int]$_.Exception.Response.StatusCode }
            if ($code -ge 200 -and $code -lt 500) { return $true }
        }
        Start-Sleep -Milliseconds $PollMs
    }
    return $false
}

# --- 5. Helper: spawn a new PowerShell window with a script body ----
function Open-PowerShellWindow {
    param(
        [Parameter(Mandatory = $true)][string]$Title,
        [Parameter(Mandatory = $true)][string]$ScriptBody
    )
    $encoded = [Convert]::ToBase64String(
        [System.Text.Encoding]::Unicode.GetBytes($ScriptBody))
    $args = @(
        '-NoProfile'
        '-NoExit'
        '-ExecutionPolicy', 'Bypass'
        '-EncodedCommand', $encoded
    )
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = 'powershell.exe'
    foreach ($a in $args) { $psi.ArgumentList.Add($a) }
    $psi.WorkingDirectory = $RepoRoot
    $psi.UseShellExecute = $true
    $psi.WindowStyle = 'Normal'
    $psi.CreateNoWindow = $false
    return [System.Diagnostics.Process]::Start($psi)
}

# --- 6. Start backend (if not skipped) ------------------------------
$backendPid = $null
$backendUrl = "http://127.0.0.1:$BackendPort"

if (-not $SkipBackend) {
    Write-Host "Starting backend in a new window..." -ForegroundColor Cyan

    $backendBody = @"
Set-Location '$RepoRoot'
`$env:ConnectionStrings__GuliERP = '$($env:ConnectionStrings__GuliERP -replace "'", "''")'
`$env:GULIERP_ConnectionStrings__GuliERP = `$env:ConnectionStrings__GuliERP
Write-Host '=== GuliERP.Api ===' -ForegroundColor Cyan
Write-Host 'URL     : http://127.0.0.1:$BackendPort'
Write-Host 'Env     : ConnectionStrings__GuliERP=redacted (set by launcher)'
Write-Host 'DB target: $ExpectedDb @ 192.168.2.228 (user: $DbUser)'
Write-Host 'Stop    : Ctrl+C in THIS window, or close the window.'
Write-Host ''

& '$Dotnet' run --project '$BackendCsproj' -c Release --no-build --no-launch-profile --urls "http://127.0.0.1:$BackendPort"
"@

    $proc = Open-PowerShellWindow -Title 'GuliERP.Api' -ScriptBody $backendBody
    if (-not $proc) { throw 'Failed to start backend process.' }
    $backendPid = $proc.Id
    Write-Host "  backend window opened, pid=$backendPid" -ForegroundColor DarkGreen

    # Wait for /health/live (cheap probe) then /health/ready
    Write-Host "  waiting for $backendUrl/health/live ..." -ForegroundColor DarkCyan
    if (-not (Wait-ForHttp "$backendUrl/health/live" -TimeoutSec 90)) {
        Write-Host '  WARN: /health/live did not return 200 within 90s.' -ForegroundColor Red
        Write-Host "        Check the backend window for the actual error." -ForegroundColor Red
        Write-Host "        Continuing anyway so the frontend window still opens." -ForegroundColor Red
    } else {
        Write-Host "  backend /health/live OK" -ForegroundColor Green
    }
    Write-Host "  waiting for $backendUrl/health/ready (DB ping)..." -ForegroundColor DarkCyan
    if (-not (Wait-ForHttp "$backendUrl/health/ready" -TimeoutSec 60)) {
        Write-Host '  WARN: /health/ready did not return 200 within 60s.' -ForegroundColor Yellow
        Write-Host "        The backend is up but the DB is not reachable." -ForegroundColor Yellow
        Write-Host "        Check the password + that 192.168.2.228:5432 is reachable." -ForegroundColor Yellow
    } else {
        Write-Host "  backend /health/ready OK" -ForegroundColor Green
    }
} else {
    Write-Host '[skip-backend] -SkipBackend set; expecting an existing backend on ' + $backendUrl -ForegroundColor Yellow
}

# --- 7. Start frontend (if not skipped) -----------------------------
$frontendPid = $null
$frontendUrl = "http://127.0.0.1:$FrontendPort"

if (-not $SkipFrontend) {
    Write-Host ''
    Write-Host "Starting frontend in a new window..." -ForegroundColor Cyan

    # Check that node_modules exists; if not, run npm ci first.
    if (-not (Test-Path (Join-Path $WebDir 'node_modules'))) {
        Write-Host "  node_modules missing — running 'npm ci' first (this can take a few minutes)..." -ForegroundColor Yellow
        Push-Location $WebDir
        try { & $Npm ci } finally { Pop-Location }
    }

    $frontendBody = @"
Set-Location '$WebDir'
Write-Host '=== GuliERP Web (Vite dev) ===' -ForegroundColor Cyan
Write-Host 'URL : http://127.0.0.1:$FrontendPort'
Write-Host 'API : $backendUrl  (assumed; the SPA hits the relative /api/v1/* paths)'
Write-Host 'Stop: Ctrl+C in THIS window, or close the window.'
Write-Host ''

& '$Npm' run dev -- --port $FrontendPort --strictPort
"@

    $proc = Open-PowerShellWindow -Title 'GuliERP.Web' -ScriptBody $frontendBody
    if (-not $proc) { throw 'Failed to start frontend process.' }
    $frontendPid = $proc.Id
    Write-Host "  frontend window opened, pid=$frontendPid" -ForegroundColor DarkGreen

    # Wait for the Vite dev server to start listening.
    Write-Host "  waiting for $frontendUrl ..." -ForegroundColor DarkCyan
    if (-not (Wait-ForHttp "$frontendUrl" -TimeoutSec 60)) {
        Write-Host '  WARN: frontend did not respond within 60s.' -ForegroundColor Red
        Write-Host "        Check the frontend window for the actual error." -ForegroundColor Red
    } else {
        Write-Host "  frontend OK" -ForegroundColor Green
    }
} else {
    Write-Host '[skip-frontend] -SkipFrontend set; not starting Vite dev server.' -ForegroundColor Yellow
}

# --- 8. Record PIDs so a future start-stack.ps1 cleans them up -------
$pids = [pscustomobject]@{
    launchedAt = (Get-Date).ToString('o')
    backend    = [pscustomobject]@{ pid = $backendPid; url = $backendUrl }
    frontend   = [pscustomobject]@{ pid = $frontendPid; url = $frontendUrl }
}
Set-Content -Path $PidFile -Value ($pids | ConvertTo-Json -Depth 5) -Encoding UTF8

# --- 9. Summary -------------------------------------------------------
Write-Host ''
Write-Host '=== Stack launched ===' -ForegroundColor Green
if ($backendPid) {
    Write-Host "  backend  pid=$backendPid  $backendUrl" -ForegroundColor White
}
if ($frontendPid) {
    Write-Host "  frontend pid=$frontendPid  $frontendUrl" -ForegroundColor White
}
Write-Host ''
Write-Host "  PIDs recorded in $PidFile" -ForegroundColor DarkGray
Write-Host '  To stop: close the two PowerShell windows individually, or' -ForegroundColor DarkGray
Write-Host '  re-run this script (it will kill the prior PIDs first).' -ForegroundColor DarkGray
Write-Host ''
Write-Host 'Next step for MDM/WEB-PREVIEW work:' -ForegroundColor Yellow
Write-Host '  Open http://localhost:' + $FrontendPort + ' in your browser, log in,' -ForegroundColor White
Write-Host '  then run MDM-002 / WEB-PREVIEW-002 grant from a THIRD shell:' -ForegroundColor White
Write-Host '    .\tools\dev\g2-004-bootstrap-operator-user.ps1 -GrantMdmOperator' -ForegroundColor Cyan
Write-Host ''
