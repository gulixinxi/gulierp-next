<#
.SYNOPSIS
    Starts the local GuliERP backend and frontend stack with health checks.
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

$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$BackendCsproj = Join-Path $RepoRoot 'apps\api\GuliERP.Api\GuliERP.Api.csproj'
$WebDir = Join-Path $RepoRoot 'apps\web'
$PidFile = Join-Path $RepoRoot '.stack-pids.json'
$LogDir = Join-Path $RepoRoot '.stack-logs'
$Runner = Join-Path $PSScriptRoot 'start-stack-child.ps1'
$BackendUrl = "http://127.0.0.1:$BackendPort"
$FrontendUrl = "http://127.0.0.1:$FrontendPort"
$ApiProxyUrl = "$FrontendUrl/api/v1/auth/csrf"
$OriginalConnectionString = $env:ConnectionStrings__GuliERP
$OriginalGuliErpConnectionString = $env:GULIERP_ConnectionStrings__GuliERP

function Write-Info {
    param([string]$Message)
    Write-Host $Message -ForegroundColor Cyan
}

function Write-Warn {
    param([string]$Message)
    Write-Host $Message -ForegroundColor Yellow
}

function Resolve-ToolPath {
    param([Parameter(Mandatory = $true)][string]$Name)
    if (Test-Path -LiteralPath $Name) {
        return (Resolve-Path -LiteralPath $Name).Path
    }
    if ([System.IO.Path]::GetExtension($Name) -eq '') {
        foreach ($suffix in @('.cmd', '.exe', '.ps1')) {
            $candidate = Get-Command ($Name + $suffix) -ErrorAction SilentlyContinue
            if ($candidate) { return $candidate.Source }
        }
    }
    $command = Get-Command $Name -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }
    throw "Required tool not found: $Name"
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

function Get-PortOwner {
    param([Parameter(Mandatory = $true)][int]$Port)
    try {
        $conn = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction Stop | Select-Object -First 1
        if (-not $conn) { return $null }
        $process = Get-Process -Id $conn.OwningProcess -ErrorAction SilentlyContinue
        return [pscustomobject]@{
            Port = $Port
            ProcessId = [int]$conn.OwningProcess
            ProcessName = if ($process) { $process.ProcessName } else { '<unknown>' }
            CommandLine = Get-ProcessCommandLineSafe -ProcessId ([int]$conn.OwningProcess)
        }
    } catch {
        return $null
    }
}

function Test-ProjectProcess {
    param(
        [Parameter(Mandatory = $true)][int]$ProcessId,
        [Parameter(Mandatory = $true)][string]$Role
    )
    $process = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
    if (-not $process) { return $false }

    $commandLine = Get-ProcessCommandLineSafe -ProcessId $ProcessId
    $needleRoot = $RepoRoot.ToLowerInvariant()
    $needleWeb = $WebDir.ToLowerInvariant()
    $needleBackend = $BackendCsproj.ToLowerInvariant()
    $haystack = $commandLine.ToLowerInvariant()

    if ($Role -eq 'backend' -and $haystack.Contains($needleBackend)) { return $true }
    if ($Role -eq 'backend' -and $haystack.Contains($needleRoot) -and $haystack.Contains('gulierp.api')) { return $true }
    if ($Role -eq 'frontend' -and $haystack.Contains($needleWeb)) { return $true }
    if ($haystack.Contains($needleRoot) -and $haystack.Contains('start-stack-child.ps1')) { return $true }
    return $false
}

function Stop-ProjectProcess {
    param(
        [Parameter(Mandatory = $true)][int]$ProcessId,
        [Parameter(Mandatory = $true)][string]$Role
    )
    $process = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
    if (-not $process) { return 'stale' }
    if (-not (Test-ProjectProcess -ProcessId $ProcessId -Role $Role)) {
        Write-Warn ("  refusing to stop {0} pid={1}; process ownership could not be verified" -f $Role, $ProcessId)
        return 'foreign'
    }
    Write-Warn ("  stopping prior {0} pid={1}" -f $Role, $ProcessId)
    Stop-Process -Id $ProcessId -Force -ErrorAction SilentlyContinue
    return 'stopped'
}

function Stop-LaunchedProcess {
    param(
        [object]$Process,
        [string]$Role
    )
    if ($Process -and -not $Process.HasExited) {
        Write-Warn ("  stopping launched {0} pid={1}" -f $Role, $Process.Id)
        Stop-Process -Id $Process.Id -Force -ErrorAction SilentlyContinue
    }
}

function Read-PidRecord {
    if (-not (Test-Path -LiteralPath $PidFile)) { return $null }
    try {
        $raw = Get-Content -LiteralPath $PidFile -Raw -ErrorAction Stop
        if ([string]::IsNullOrWhiteSpace($raw)) { throw 'PID file is empty.' }
        return ($raw | ConvertFrom-Json -ErrorAction Stop)
    } catch {
        Write-Warn ("  ignoring malformed {0}: {1}" -f $PidFile, $($_.Exception.Message))
        Remove-Item -LiteralPath $PidFile -Force -ErrorAction SilentlyContinue
        return $null
    }
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
    if ([int]::TryParse($text, [ref]$parsed) -and $parsed -gt 0) {
        return $parsed
    }
    Write-Warn ("  ignoring malformed {0} pid in {1}: {2}" -f $Role, $PidFile, $text)
    return $null
}

function Wait-ForHttpStatus {
    param(
        [Parameter(Mandatory = $true)][string]$Url,
        [int]$ExpectedStatus = 200,
        [int]$TimeoutSec = 60,
        [int]$PollMs = 750
    )
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
            if ([int]$response.StatusCode -eq $ExpectedStatus) { return $true }
        } catch {
            $response = $_.Exception.Response
            if ($response -and [int]$response.StatusCode -eq $ExpectedStatus) { return $true }
        }
        Start-Sleep -Milliseconds $PollMs
    }
    return $false
}

function Show-LogTail {
    param(
        [string]$Path,
        [int]$Lines = 60
    )
    if (Test-Path -LiteralPath $Path) {
        Write-Host ("--- log tail: {0} ---" -f $Path) -ForegroundColor DarkGray
        Get-Content -LiteralPath $Path -Tail $Lines -ErrorAction SilentlyContinue
    }
}

function Assert-PortAvailableOrReusable {
    param(
        [Parameter(Mandatory = $true)][int]$Port,
        [Parameter(Mandatory = $true)][string]$Role
    )
    $owner = Get-PortOwner -Port $Port
    if (-not $owner) { return }
    if (Test-ProjectProcess -ProcessId $owner.ProcessId -Role $Role) {
        Write-Warn ("  {0} port {1} is already owned by this project pid={2}; stopping for a clean restart" -f $Role, $Port, $owner.ProcessId)
        [void](Stop-ProjectProcess -ProcessId $owner.ProcessId -Role $Role)
        Start-Sleep -Milliseconds 700
        return
    }
    Write-Host ''
    Write-Host ("Port {0} is already in use by an unrelated process." -f $Port) -ForegroundColor Red
    Write-Host ("  PID     : {0}" -f $owner.ProcessId) -ForegroundColor Red
    Write-Host ("  Process : {0}" -f $owner.ProcessName) -ForegroundColor Red
    if ($owner.CommandLine) {
        Write-Host ("  Command : {0}" -f $owner.CommandLine) -ForegroundColor Red
    }
    Write-Host 'Close that process or choose another explicit port, then rerun the launcher.' -ForegroundColor Yellow
    exit 1
}

function Stop-PriorStack {
    $prior = Read-PidRecord
    if (-not $prior) { return }
    $backendRecordPid = Get-RecordPid -Record $prior -Role 'backend'
    $frontendRecordPid = Get-RecordPid -Record $prior -Role 'frontend'
    if ($backendRecordPid) { [void](Stop-ProjectProcess -ProcessId $backendRecordPid -Role 'backend') }
    if ($frontendRecordPid) { [void](Stop-ProjectProcess -ProcessId $frontendRecordPid -Role 'frontend') }
    Remove-Item -LiteralPath $PidFile -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 700
}

function Get-ConnectionString {
    if (-not [string]::IsNullOrWhiteSpace($env:ConnectionStrings__GuliERP)) {
        return $env:ConnectionStrings__GuliERP
    }
    if (-not [string]::IsNullOrWhiteSpace($env:GULIERP_ConnectionStrings__GuliERP)) {
        return $env:GULIERP_ConnectionStrings__GuliERP
    }
    $secure = Read-Host -Prompt "PostgreSQL password for user '$DbUser' (input is masked)" -AsSecureString
    if ($secure.Length -eq 0) { throw 'Empty password; aborting.' }
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try {
        $plain = [Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
        return "Host=192.168.2.228;Port=5432;Database=$ExpectedDb;Username=$DbUser;Password=$plain;Include Error Detail=true"
    } finally {
        if ($bstr -ne [IntPtr]::Zero) {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
        }
        $plain = $null
    }
}

function Start-StackProcess {
    param(
        [Parameter(Mandatory = $true)][string]$Role,
        [Parameter(Mandatory = $true)][string[]]$RunnerArguments,
        [Parameter(Mandatory = $true)][string]$LogPath,
        [hashtable]$Environment = @{}
    )
    $hostExe = (Get-Process -Id $PID).Path
    if (-not $hostExe) { $hostExe = Resolve-ToolPath -Name 'powershell.exe' }
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $hostExe
    $psi.WorkingDirectory = $RepoRoot
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $false
    $psi.RedirectStandardError = $false
    $psi.CreateNoWindow = $true
    $psi.Arguments = ('-NoProfile -ExecutionPolicy Bypass -File "{0}" {1}' -f $Runner, ($RunnerArguments -join ' '))
    foreach ($key in $Environment.Keys) {
        $psi.EnvironmentVariables[$key] = [string]$Environment[$key]
    }
    $psi.EnvironmentVariables['GULIERP_STACK_LOG'] = $LogPath
    $process = [System.Diagnostics.Process]::Start($psi)
    if (-not $process) { throw ("Failed to start {0} process." -f $Role) }
    return $process
}

function Invoke-Checked {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [Parameter(Mandatory = $true)][string]$WorkingDirectory
    )
    $argLine = $Arguments -join ' '
    Write-Host ("  {0} {1}" -f $FilePath, $argLine) -ForegroundColor DarkGray
    $process = Start-Process -FilePath $FilePath -ArgumentList $Arguments -WorkingDirectory $WorkingDirectory -NoNewWindow -Wait -PassThru
    if ($process.ExitCode -ne 0) {
        throw ("Command failed with exit code {0}: {1}" -f $process.ExitCode, $FilePath)
    }
}

Write-Host ''
Write-Host '=== GuliERP Next local stack ===' -ForegroundColor Cyan
Write-Host ("Repo root : {0}" -f $RepoRoot)
Write-Host ("Backend   : {0}" -f $BackendUrl)
if (-not $SkipFrontend) { Write-Host ("Frontend  : {0}" -f $FrontendUrl) }
Write-Host ("DB target : {0} @ 192.168.2.228 (user: {1})" -f $ExpectedDb, $DbUser)
Write-Host ''

if (-not (Test-Path -LiteralPath $BackendCsproj)) { throw "Backend csproj not found: $BackendCsproj" }
if (-not (Test-Path -LiteralPath $WebDir)) { throw "Web dir not found: $WebDir" }
if (-not (Test-Path -LiteralPath $Runner)) { throw "Runner not found: $Runner" }
if (-not (Test-Path -LiteralPath $LogDir)) { New-Item -ItemType Directory -Path $LogDir | Out-Null }

$Dotnet = Resolve-ToolPath -Name $Dotnet
$Npm = Resolve-ToolPath -Name $Npm

Stop-PriorStack
if (-not $SkipBackend) { Assert-PortAvailableOrReusable -Port $BackendPort -Role 'backend' }
if (-not $SkipFrontend) { Assert-PortAvailableOrReusable -Port $FrontendPort -Role 'frontend' }

$connection = $null
if (-not $SkipBackend) {
    $connection = Get-ConnectionString
    $env:ConnectionStrings__GuliERP = $connection
    $env:GULIERP_ConnectionStrings__GuliERP = $connection
    $assert = Join-Path $PSScriptRoot 'assert-gulierp-db-target.ps1'
    & $assert -ConnectionString $connection -ExpectedDatabase $ExpectedDb
    if ($LASTEXITCODE -ne 0) { throw 'DB target guard failed; aborting.' }
}

try {
    if (-not $SkipBackend) {
        Write-Info 'Building backend...'
        Invoke-Checked -FilePath $Dotnet -Arguments @('build', $BackendCsproj, '-c', 'Release') -WorkingDirectory $RepoRoot
    }

    if (-not $SkipFrontend -and -not (Test-Path -LiteralPath (Join-Path $WebDir 'node_modules'))) {
        Write-Warn "Frontend dependencies are missing; running npm ci in apps\web."
        Invoke-Checked -FilePath $Npm -Arguments @('ci') -WorkingDirectory $WebDir
    }

    $timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $backendLog = Join-Path $LogDir "backend-$timestamp.log"
    $frontendLog = Join-Path $LogDir "frontend-$timestamp.log"
    $backendProcess = $null
    $frontendProcess = $null
    $backendRecordedPid = $null
    $frontendRecordedPid = $null

    if (-not $SkipBackend) {
        Write-Info 'Starting backend...'
        $backendArgs = @(
            '-Role', 'backend',
            '-RepoRoot', ('"{0}"' -f $RepoRoot),
            '-Dotnet', ('"{0}"' -f $Dotnet),
            '-BackendCsproj', ('"{0}"' -f $BackendCsproj),
            '-BackendPort', $BackendPort
        )
        $backendProcess = Start-StackProcess -Role 'backend' -RunnerArguments $backendArgs -LogPath $backendLog -Environment @{
            'ConnectionStrings__GuliERP' = $connection
            'GULIERP_ConnectionStrings__GuliERP' = $connection
        }
        Write-Host ("  backend pid={0}" -f $backendProcess.Id) -ForegroundColor Green
        Write-Info ("Waiting for {0}/health/live ..." -f $BackendUrl)
        if (-not (Wait-ForHttpStatus -Url "$BackendUrl/health/live" -ExpectedStatus 200 -TimeoutSec 90)) {
            Show-LogTail -Path $backendLog
            Stop-LaunchedProcess -Process $backendProcess -Role 'backend'
            exit 1
        }
        $backendOwner = Get-PortOwner -Port $BackendPort
        if ($backendOwner -and (Test-ProjectProcess -ProcessId $backendOwner.ProcessId -Role 'backend')) {
            $backendRecordedPid = $backendOwner.ProcessId
        } else {
            $backendRecordedPid = $backendProcess.Id
        }
    } else {
        Write-Warn ("Skipping backend start; expecting an existing backend at {0}" -f $BackendUrl)
    }

    if (-not $SkipFrontend) {
        Write-Info 'Starting frontend...'
        $frontendArgs = @(
            '-Role', 'frontend',
            '-RepoRoot', ('"{0}"' -f $RepoRoot),
            '-Npm', ('"{0}"' -f $Npm),
            '-WebDir', ('"{0}"' -f $WebDir),
            '-FrontendPort', $FrontendPort,
            '-BackendUrl', $BackendUrl
        )
        $frontendProcess = Start-StackProcess -Role 'frontend' -RunnerArguments $frontendArgs -LogPath $frontendLog -Environment @{
            'VITE_API_TARGET' = $BackendUrl
        }
        Write-Host ("  frontend pid={0}" -f $frontendProcess.Id) -ForegroundColor Green
        Write-Info ("Waiting for {0} ..." -f $FrontendUrl)
        if (-not (Wait-ForHttpStatus -Url $FrontendUrl -ExpectedStatus 200 -TimeoutSec 70)) {
            Show-LogTail -Path $frontendLog
            Stop-LaunchedProcess -Process $frontendProcess -Role 'frontend'
            Stop-LaunchedProcess -Process $backendProcess -Role 'backend'
            exit 1
        }
        $frontendOwner = Get-PortOwner -Port $FrontendPort
        if ($frontendOwner -and (Test-ProjectProcess -ProcessId $frontendOwner.ProcessId -Role 'frontend')) {
            $frontendRecordedPid = $frontendOwner.ProcessId
        } else {
            $frontendRecordedPid = $frontendProcess.Id
        }

        Write-Info ("Checking frontend API proxy {0} ..." -f $ApiProxyUrl)
        if (-not (Wait-ForHttpStatus -Url $ApiProxyUrl -ExpectedStatus 200 -TimeoutSec 30)) {
            Show-LogTail -Path $frontendLog
            Show-LogTail -Path $backendLog
            Stop-LaunchedProcess -Process $frontendProcess -Role 'frontend'
            Stop-LaunchedProcess -Process $backendProcess -Role 'backend'
            exit 1
        }
    }

    $pids = [pscustomobject]@{
        launchedAt = (Get-Date).ToString('o')
        repoRoot = $RepoRoot
        backend = [pscustomobject]@{
            pid = $backendRecordedPid
            url = $BackendUrl
            log = if ($backendProcess) { $backendLog } else { $null }
        }
        frontend = [pscustomobject]@{
            pid = $frontendRecordedPid
            url = $FrontendUrl
            log = if ($frontendProcess) { $frontendLog } else { $null }
        }
    }
    Set-Content -LiteralPath $PidFile -Value ($pids | ConvertTo-Json -Depth 5) -Encoding UTF8

    Write-Host ''
    Write-Host '=== Stack ready ===' -ForegroundColor Green
    Write-Host ("Backend health : {0}/health/live" -f $BackendUrl)
    if (-not $SkipFrontend) {
        Write-Host ("Frontend       : {0}" -f $FrontendUrl) -ForegroundColor White
        Write-Host ("API proxy      : {0}" -f $ApiProxyUrl)
    }
    Write-Host ("PID file       : {0}" -f $PidFile) -ForegroundColor DarkGray
    Write-Host ("Logs           : {0}" -f $LogDir) -ForegroundColor DarkGray
    exit 0
} finally {
    $env:ConnectionStrings__GuliERP = $OriginalConnectionString
    $env:GULIERP_ConnectionStrings__GuliERP = $OriginalGuliErpConnectionString
    $connection = $null
}
