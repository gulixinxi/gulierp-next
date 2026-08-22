[CmdletBinding()]
param(
    [int]$BackendPort = 5000,
    [int]$FrontendPort = 5173
)

$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$PidFile = Join-Path $RepoRoot '.stack-pids.json'
$BackendCsproj = Join-Path $RepoRoot 'apps\api\GuliERP.Api\GuliERP.Api.csproj'
$WebDir = Join-Path $RepoRoot 'apps\web'

function Write-Warn {
    param([string]$Message)
    Write-Host $Message -ForegroundColor Yellow
}

function Get-ProcessCommandLineSafe {
    param([Parameter(Mandatory = $true)][int]$ProcessId)
    try {
        $proc = Get-CimInstance Win32_Process -Filter "ProcessId = $ProcessId" -ErrorAction Stop
        if ($proc) { return [string]$proc.CommandLine }
    } catch {
        return ''
    }
    return ''
}

function Test-ProjectProcess {
    param(
        [Parameter(Mandatory = $true)][int]$ProcessId,
        [Parameter(Mandatory = $true)][string]$Role
    )
    $process = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
    if (-not $process) { return $false }

    $commandLine = Get-ProcessCommandLineSafe -ProcessId $ProcessId
    $haystack = $commandLine.ToLowerInvariant()
    if ($Role -eq 'backend' -and $haystack.Contains($BackendCsproj.ToLowerInvariant())) { return $true }
    if ($Role -eq 'backend' -and $haystack.Contains($RepoRoot.ToLowerInvariant()) -and $haystack.Contains('gulierp.api')) { return $true }
    if ($Role -eq 'frontend' -and $haystack.Contains($WebDir.ToLowerInvariant())) { return $true }
    if ($haystack.Contains($RepoRoot.ToLowerInvariant()) -and
        $haystack.Contains('start-stack-child.ps1') -and
        $haystack.Contains(("-role {0}" -f $Role))) {
        return $true
    }
    return $false
}

function Get-PortOwner {
    param([Parameter(Mandatory = $true)][int]$Port)
    try {
        $conn = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction Stop | Select-Object -First 1
        if (-not $conn) { return $null }
        return [int]$conn.OwningProcess
    } catch {
        # Fall back for shells where Get-NetTCPConnection is present but denied.
    }
    try {
        $lines = & netstat -ano -p tcp 2>$null
        foreach ($line in $lines) {
            if ($line -notmatch '\bLISTENING\b') { continue }
            $parts = $line -split '\s+' | Where-Object { $_ }
            if ($parts.Count -lt 5) { continue }
            $localEndpoint = [string]$parts[1]
            if (-not $localEndpoint.EndsWith(":$Port", [StringComparison]::OrdinalIgnoreCase)) { continue }
            $ownerPid = 0
            if ([int]::TryParse([string]$parts[-1], [ref]$ownerPid) -and $ownerPid -gt 0) {
                return $ownerPid
            }
        }
    } catch {
        return $null
    }
    return $null
}

function Stop-VerifiedProcess {
    param(
        [Parameter(Mandatory = $true)][int]$ProcessId,
        [Parameter(Mandatory = $true)][string]$Role,
        [switch]$TrustPidFile
    )
    $process = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
    if (-not $process) {
        Write-Host ("  {0} pid={1} is already stopped" -f $Role, $ProcessId) -ForegroundColor DarkGray
        return
    }
    $trustedByPidFile = $false
    if ($TrustPidFile) {
        $trustedByPidFile = ($Role -eq 'backend' -and $process.ProcessName -in @('GuliERP.Api', 'dotnet', 'pwsh', 'powershell')) -or
                            ($Role -eq 'frontend' -and $process.ProcessName -in @('node', 'npm', 'cmd', 'pwsh', 'powershell'))
    }
    if (-not $trustedByPidFile -and -not (Test-ProjectProcess -ProcessId $ProcessId -Role $Role)) {
        Write-Warn ("  refusing to stop {0} pid={1}; process ownership could not be verified" -f $Role, $ProcessId)
        return
    }
    Write-Warn ("  stopping {0} pid={1}" -f $Role, $ProcessId)
    Stop-Process -Id $ProcessId -Force -ErrorAction SilentlyContinue
}

function Get-RecordPid {
    param(
        [object]$Record,
        [string]$Role
    )
    if (-not $Record) { return $null }
    $value = $null
    if ($Role -eq 'backend' -and $Record.backend) { $value = $Record.backend.pid }
    if ($Role -eq 'frontend' -and $Record.frontend) { $value = $Record.frontend.pid }
    if ($null -eq $value) { return $null }
    $text = ([string]$value).Trim()
    $parsed = 0
    if ([int]::TryParse($text, [ref]$parsed) -and $parsed -gt 0) { return $parsed }
    Write-Warn ("  ignoring malformed {0} pid in {1}: {2}" -f $Role, $PidFile, $text)
    return $null
}

$record = $null
if (Test-Path -LiteralPath $PidFile) {
    try {
        $raw = Get-Content -LiteralPath $PidFile -Raw -ErrorAction Stop
        if ([string]::IsNullOrWhiteSpace($raw)) { throw 'PID file is empty.' }
        $record = $raw | ConvertFrom-Json -ErrorAction Stop
    } catch {
        Write-Warn ("Ignoring malformed {0}: {1}" -f $PidFile, $($_.Exception.Message))
        Remove-Item -LiteralPath $PidFile -Force -ErrorAction SilentlyContinue
        $record = $null
    }
} else {
    Write-Host 'No GuliERP stack PID file found; checking default ports.' -ForegroundColor DarkGray
}

foreach ($role in @('frontend', 'backend')) {
    $recordPid = Get-RecordPid -Record $record -Role $role
    $trustPidFile = $record -and [string]$record.repoRoot -eq $RepoRoot
    if ($recordPid) { Stop-VerifiedProcess -ProcessId $recordPid -Role $role -TrustPidFile:$trustPidFile }
}

$frontendOwnerPid = Get-PortOwner -Port $FrontendPort
if ($frontendOwnerPid) { Stop-VerifiedProcess -ProcessId $frontendOwnerPid -Role 'frontend' }

$backendOwnerPid = Get-PortOwner -Port $BackendPort
if ($backendOwnerPid) { Stop-VerifiedProcess -ProcessId $backendOwnerPid -Role 'backend' }

Remove-Item -LiteralPath $PidFile -Force -ErrorAction SilentlyContinue
Write-Host 'GuliERP stack stop completed.' -ForegroundColor Green
exit 0
