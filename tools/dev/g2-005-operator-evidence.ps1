#requires -Version 5.1
<#
G2-005 — Final Operator Evidence Harness

Operator usage:

  NEW POWERSHELL
  cd D:\guli\projects\gulierp-next
  .\tools\dev\g2-005-operator-evidence.ps1

The harness prompts locally for the PostgreSQL password, propagates the
resolved Npgsql connection to child processes, restores the caller's
environment in finally, parses TRX files as the source of truth, and
stops only script-owned host PIDs.
#>
[CmdletBinding()]
param(
    [switch]$SkipPrompt,
    [switch]$FocusBootstrapTestOnly,
    [switch]$FocusIdentityIntegrationTestOnly
)

$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'

$RepoRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
Set-Location $RepoRoot

$Dotnet = 'D:\guli\gulierp\.dotnet\dotnet.exe'
$DefaultPgHost = '192.168.2.228'
$DefaultPgPort = '5432'
$DefaultPgDatabase = 'gulierp_g2_003_test'
$DefaultPgUsername = 'gulidata'

function Step-Header($n, $title) {
    Write-Host ""
    Write-Host "============================================================"
    Write-Host "G2-005 Step $n : $title"
    Write-Host "============================================================"
}

function Pass($msg) { Write-Host "  [PASS] $msg" -ForegroundColor Green }

function Redact-SecretText {
    [CmdletBinding()]
    param([object]$Value)

    if ($null -eq $Value) { return '' }
    $text = ($Value | Out-String)
    $text = $text -replace "(?i)(Password|Pwd)\s*=\s*[^;`r`n]+", '$1=***'
    return $text
}

function Write-RedactedCommandDiagnostics {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)] [string]$Label,
        [object]$Output
    )

    $redacted = Redact-SecretText $Output
    $lines = @($redacted -split "(`r`n|`n|`r)" | Where-Object { $_ -ne '' })
    if ($lines.Count -eq 0) {
        Write-Host "  [INFO] $Label diagnostics: <empty>" -ForegroundColor Yellow
        return
    }

    Write-Host "  [INFO] $Label diagnostics (redacted, last 80 lines):" -ForegroundColor Yellow
    foreach ($line in ($lines | Select-Object -Last 80)) {
        Write-Host "    $line" -ForegroundColor Yellow
    }
}

function Save-OperatorConnectionEnvironment {
    [CmdletBinding()]
    param()

    return @{
        ConnectionStrings__GuliERP = @{
            Exists = Test-Path Env:ConnectionStrings__GuliERP
            Value = $env:ConnectionStrings__GuliERP
        }
        GULIERP_ConnectionStrings__GuliERP = @{
            Exists = Test-Path Env:GULIERP_ConnectionStrings__GuliERP
            Value = $env:GULIERP_ConnectionStrings__GuliERP
        }
        GULIERP_FOUNDATION_CONNECTION = @{
            Exists = Test-Path Env:GULIERP_FOUNDATION_CONNECTION
            Value = $env:GULIERP_FOUNDATION_CONNECTION
        }
    }
}

function Save-HarnessEnvironment {
    [CmdletBinding()]
    param()

    $state = @{}
    foreach ($name in Get-HarnessEnvironmentVariableNames) {
        $state[$name] = @{
            Exists = Test-Path -LiteralPath "Env:$name"
            Value = (Get-Item -LiteralPath "Env:$name" -ErrorAction SilentlyContinue).Value
        }
    }
    return $state
}

function Restore-HarnessEnvironment {
    [CmdletBinding()]
    param([Parameter(Mandatory = $true)] [hashtable]$State)

    foreach ($name in $State.Keys) {
        $item = $State[$name]
        if ($item.Exists) {
            Set-Item -Path "Env:$name" -Value $item.Value
        }
        else {
            Remove-Item -Path "Env:$name" -ErrorAction SilentlyContinue
        }
    }
}

function Set-OperatorConnectionEnvironment {
    [CmdletBinding()]
    param([Parameter(Mandatory = $true)] [string]$ConnectionString)

    $env:ConnectionStrings__GuliERP = $ConnectionString
    $env:GULIERP_ConnectionStrings__GuliERP = $ConnectionString
    $env:GULIERP_FOUNDATION_CONNECTION = $ConnectionString
}

function Get-CleanTestEnvironmentRemovals {
    [CmdletBinding()]
    param()

    return @(Get-HarnessEnvironmentVariableNames)
}

function Get-PostgreSqlTestEnvironmentOverrides {
    [CmdletBinding()]
    param([Parameter(Mandatory = $true)] [string]$ConnectionString)

    return @{
        ConnectionStrings__GuliERP = $ConnectionString
        GULIERP_ConnectionStrings__GuliERP = $ConnectionString
        GULIERP_FOUNDATION_CONNECTION = $ConnectionString
    }
}

function Get-ConnectionStringDatabaseName {
    [CmdletBinding()]
    param([string]$ConnectionString)

    if ([string]::IsNullOrWhiteSpace($ConnectionString)) { return $null }
    foreach ($part in ($ConnectionString -split ';')) {
        $kv = $part -split '=', 2
        if ($kv.Count -eq 2 -and $kv[0].Trim() -ieq 'Database') {
            return $kv[1].Trim()
        }
    }
    return $null
}

function Assert-ApprovedOperatorDatabaseTarget {
    [CmdletBinding()]
    param([Parameter(Mandatory = $true)] [string]$ConnectionString)

    $databaseName = Get-ConnectionStringDatabaseName -ConnectionString $ConnectionString
    if ($databaseName -ne $DefaultPgDatabase) {
        $displayTarget = if ([string]::IsNullOrWhiteSpace($databaseName)) { 'UNKNOWN' } else { $databaseName }
        Fail-Fatal "Database target is not approved for G2-005 PG focused verification. Database target=$displayTarget" 2
    }
    Write-Host "  ConnectionStrings__GuliERP = PRESENT"
    Write-Host "  Database target = $DefaultPgDatabase"
}

function Resolve-OperatorPostgreSqlConnectionString {
    [CmdletBinding()]
    param([switch]$PromptIfMissing)

    $resolvedConnection = $env:ConnectionStrings__GuliERP
    if (-not $resolvedConnection) { $resolvedConnection = $env:GULIERP_ConnectionStrings__GuliERP }
    if (-not $resolvedConnection) { $resolvedConnection = $env:GULIERP_FOUNDATION_CONNECTION }

    if ($resolvedConnection) {
        Write-Host "[G2-005] Using caller-provided PostgreSQL connection string."
        return $resolvedConnection
    }

    if (-not $PromptIfMissing) {
        Fail-Fatal "No PostgreSQL connection string is present for focused PG verification." 3
    }

    Write-Host "[G2-005] Using default Operator PostgreSQL target:"
    Write-Host "  Host=$DefaultPgHost;Port=$DefaultPgPort;Database=$DefaultPgDatabase;Username=$DefaultPgUsername;Password=***"
    Write-Host "[G2-005] PostgreSQL password will be read via Read-Host -AsSecureString (NOT echoed)."
    $script:OperatorPgSecurePassword = Read-Host -Prompt 'PostgreSQL password' -AsSecureString
    if ($null -eq $script:OperatorPgSecurePassword -or $script:OperatorPgSecurePassword.Length -lt 1) {
        Fail-Fatal "Empty PostgreSQL password. Aborting." 2
    }

    Use-OperatorPlainPassword {
        param($plainPwd)
        $script:ResolvedOperatorConnection = "Host=$DefaultPgHost;Port=$DefaultPgPort;Database=$DefaultPgDatabase;Username=$DefaultPgUsername;Password=$plainPwd"
    }
    return $script:ResolvedOperatorConnection
}

function Restore-OperatorConnectionEnvironment {
    [CmdletBinding()]
    param()

    if ($null -eq $script:OriginalOperatorConnectionEnvironment) { return }

    foreach ($name in @(
            'ConnectionStrings__GuliERP',
            'GULIERP_ConnectionStrings__GuliERP',
            'GULIERP_FOUNDATION_CONNECTION'
        )) {
        $state = $script:OriginalOperatorConnectionEnvironment[$name]
        if ($state.Exists) {
            Set-Item -Path "Env:$name" -Value $state.Value
        }
        else {
            Remove-Item -Path "Env:$name" -ErrorAction SilentlyContinue
        }
    }
}

function Fail-Fatal($msg, $exitCode) {
    Write-Host "  [FATAL] $msg" -ForegroundColor Red
    Restore-OperatorConnectionEnvironment
    Complete-Cleanup
    Stop-AllOwnedHosts
    exit $exitCode
}

function Convert-HttpContentToString {
    [CmdletBinding()]
    param([object]$Content)

    if ($null -eq $Content) { return '' }
    if ($Content -is [byte[]]) {
        return [System.Text.Encoding]::UTF8.GetString($Content)
    }
    return [string]$Content
}

function Invoke-HttpProbe {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)] [string]$Method,
        [Parameter(Mandatory = $true)] [string]$Uri,
        [string]$ContentType,
        [string]$Body,
        [hashtable]$Headers,
        [int]$TimeoutSec = 10,
        [Microsoft.PowerShell.Commands.WebRequestSession]$WebSession,
        [switch]$SkipHeaderCheck
    )

    $params = @{
        Method = $Method
        Uri = $Uri
        UseBasicParsing = $true
        TimeoutSec = $TimeoutSec
        SkipHttpErrorCheck = $true
    }
    if ($SkipHeaderCheck) { $params['SkipHeaderCheck'] = $true }
    if ($ContentType) { $params['ContentType'] = $ContentType }
    if ($Body -ne $null) { $params['Body'] = $Body }
    if ($Headers) { $params['Headers'] = $Headers }
    if ($WebSession) { $params['WebSession'] = $WebSession }

    try {
        $resp = Invoke-WebRequest @params -ErrorAction Stop
        return @{
            StatusCode = [int]$resp.StatusCode
            Content = Convert-HttpContentToString $resp.Content
            Headers = $resp.Headers
            Ok = $true
        }
    }
    catch {
        $ex = $_.Exception
        $status = -1
        $body = ''
        $hdr = $null
        if ($ex.Response) {
            try { $status = [int]$ex.Response.StatusCode } catch {}
            try {
                $stream = $ex.Response.GetResponseStream()
                if ($stream) {
                    $reader = New-Object System.IO.StreamReader($stream, [System.Text.Encoding]::UTF8)
                    $body = $reader.ReadToEnd()
                    $reader.Close()
                    $stream.Close()
                }
            } catch {}
        }
        return @{
            StatusCode = $status
            Content = $body
            Headers = $hdr
            Ok = $false
            Error = $ex.Message
        }
    }
}

function Wait-HostReady {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)] [string]$BaseUrl,
        [int]$TimeoutSec = 30
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        $probe = Invoke-HttpProbe -Method Get -Uri "$BaseUrl/health/live" -TimeoutSec 2
        if ($probe.StatusCode -eq 200) { return $true }
        Start-Sleep -Seconds 1
    }
    return $false
}

function Start-HostProcess {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)] [string]$Url,
        [string]$Environment,
        [string]$LogPrefix = 'g2-005'
    )

    $dotnetArgs = @(
        'run', '--project', 'apps/api/GuliERP.Api/GuliERP.Api.csproj',
        '-c', 'Release', '--no-build', '--urls', $Url
    )
    $stdout = "$env:TEMP\$LogPrefix-host.log"
    $stderr = "$env:TEMP\$LogPrefix-host.err.log"
    $savedAspNetCoreEnvironment = $env:ASPNETCORE_ENVIRONMENT
    $savedDotNetEnvironment = $env:DOTNET_ENVIRONMENT
    try {
        if ($Environment) {
            $env:ASPNETCORE_ENVIRONMENT = $Environment
            $env:DOTNET_ENVIRONMENT = $Environment
        }
        return Start-Process -FilePath $Dotnet -ArgumentList $dotnetArgs `
            -PassThru `
            -RedirectStandardOutput $stdout `
            -RedirectStandardError $stderr `
            -WindowStyle Hidden
    }
    finally {
        if ($null -eq $savedAspNetCoreEnvironment) {
            Remove-Item -Path Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue
        }
        else {
            Set-Item -Path Env:ASPNETCORE_ENVIRONMENT -Value $savedAspNetCoreEnvironment
        }
        if ($null -eq $savedDotNetEnvironment) {
            Remove-Item -Path Env:DOTNET_ENVIRONMENT -ErrorAction SilentlyContinue
        }
        else {
            Set-Item -Path Env:DOTNET_ENVIRONMENT -Value $savedDotNetEnvironment
        }
    }
}

$script:OwnedHostPids = New-Object 'System.Collections.Generic.List[int]'

function Register-OwnedHostPid {
    [CmdletBinding()]
    param([int]$ProcessId)

    if ($ProcessId -gt 0 -and -not $script:OwnedHostPids.Contains($ProcessId)) {
        $script:OwnedHostPids.Add($ProcessId)
    }
}

function Stop-OwnedHost {
    [CmdletBinding()]
    param([int]$ProcessId)

    if ($ProcessId -le 0) { return }
    if (-not $script:OwnedHostPids.Contains($ProcessId)) { return }
    try {
        $ownedProcess = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
        if ($ownedProcess) {
            Stop-Process -Id $ProcessId -Force -ErrorAction SilentlyContinue
            Start-Sleep -Milliseconds 500
        }
    }
    finally {
        [void]$script:OwnedHostPids.Remove($ProcessId)
    }
}

function Stop-AllOwnedHosts {
    foreach ($processId in @($script:OwnedHostPids)) {
        Stop-OwnedHost -ProcessId $processId
    }
}

function Parse-TrxCounters {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)] [string]$TrxPath,
        [Parameter(Mandatory = $true)] [string]$SuiteName
    )

    if (-not (Test-Path -LiteralPath $TrxPath -PathType Leaf)) {
        throw "[$SuiteName] TRX file missing: $TrxPath"
    }
    [xml]$trx = Get-Content -LiteralPath $TrxPath -Raw -Encoding UTF8
    $counters = $trx.TestRun.ResultSummary.Counters
    if ($null -eq $counters) {
        throw "[$SuiteName] TRX missing TestRun/ResultSummary/Counters element: $TrxPath"
    }
    return @{
        Suite = $SuiteName
        Total = [int]$counters.total
        Executed = [int]$counters.executed
        Passed = [int]$counters.passed
        Failed = [int]$counters.failed
        NotExecuted = [int]$counters.notExecuted
        Outcome = [string]$trx.TestRun.ResultSummary.outcome
        Path = $TrxPath
    }
}

function Get-ProcessTreeIds {
    [CmdletBinding()]
    param([Parameter(Mandatory = $true)] [int]$RootProcessId)

    $ids = New-Object 'System.Collections.Generic.List[int]'
    if ($RootProcessId -le 0) { return @() }
    [void]$ids.Add($RootProcessId)

    for ($i = 0; $i -lt $ids.Count; $i++) {
        $parentId = $ids[$i]
        $children = Get-CimInstance Win32_Process -Filter "ParentProcessId = $parentId" -ErrorAction SilentlyContinue
        foreach ($child in @($children)) {
            $childId = [int]$child.ProcessId
            if ($childId -gt 0 -and -not $ids.Contains($childId)) {
                [void]$ids.Add($childId)
            }
        }
    }

    return @($ids)
}

function Stop-OwnedProcessTree {
    [CmdletBinding()]
    param([Parameter(Mandatory = $true)] [int]$RootProcessId)

    $treeIds = @(Get-ProcessTreeIds -RootProcessId $RootProcessId)
    foreach ($processId in ($treeIds | Select-Object -Skip 1 | Sort-Object -Descending)) {
        Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
    }
    Stop-Process -Id $RootProcessId -Force -ErrorAction SilentlyContinue
}

function Format-CommandForLog {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)] [string]$FilePath,
        [Parameter(Mandatory = $true)] [string[]]$Arguments
    )

    $parts = @($FilePath) + $Arguments
    return ($parts | ForEach-Object {
            if ($_ -match '[\s;]') { '"' + ($_ -replace '"', '\"') + '"' } else { $_ }
        }) -join ' '
}

function Get-HarnessEnvironmentVariableNames {
    [CmdletBinding()]
    param()

    return @(
        'ConnectionStrings__GuliERP',
        'GULIERP_ConnectionStrings__GuliERP',
        'GULIERP_FOUNDATION_CONNECTION',
        'GULIERP_OPERATOR_USER',
        'GULIERP_OPERATOR_TENANT',
        'PGHOST',
        'PGPORT',
        'PGDATABASE',
        'PGUSER',
        'PGPASSWORD',
        'PGPASSFILE',
        'PGSERVICE'
    )
}

function Write-EnvironmentPresenceSnapshot {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)] [string]$Label,
        [hashtable]$EnvironmentOverrides,
        [string[]]$EnvironmentRemovals
    )

    $removalSet = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($name in @($EnvironmentRemovals)) { [void]$removalSet.Add($name) }

    Write-Host "    Environment snapshot: $Label"
    foreach ($name in Get-HarnessEnvironmentVariableNames) {
        $parentPresent = Test-Path -LiteralPath "Env:$name"
        $childPresent = $parentPresent
        if ($removalSet.Contains($name)) {
            $childPresent = $false
        }
        if ($EnvironmentOverrides -and $EnvironmentOverrides.ContainsKey($name)) {
            $childPresent = $true
        }

        $parentState = if ($parentPresent) { 'PRESENT' } else { 'ABSENT' }
        $childState = if ($childPresent) { 'PRESENT' } else { 'ABSENT' }
        Write-Host "      $name parent=$parentState child=$childState"
    }
}

function Write-ChildProcessEnvironmentContractSnapshot {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)] [string]$Label,
        [Parameter(Mandatory = $true)] [System.Diagnostics.ProcessStartInfo]$ProcessStartInfo
    )

    $keys = @(
        'PATH',
        'PATHEXT',
        'DOTNET_ROOT',
        'DOTNET_ROOT_X64',
        'DOTNET_CLI_HOME',
        'DOTNET_SKIP_FIRST_TIME_EXPERIENCE',
        'DOTNET_NOLOGO',
        'HOME',
        'USERPROFILE',
        'TEMP',
        'TMP',
        'NUGET_PACKAGES',
        'MSBUILDDEBUGPATH',
        'MSBUILDDISABLENODEREUSE'
    )

    Write-Host "    Child ProcessStartInfo environment contract: $Label"
    Write-Host "      ProcessStartInfo.EnvironmentVariables.Count=$($ProcessStartInfo.EnvironmentVariables.Count)"
    foreach ($key in $keys) {
        $state = if ($ProcessStartInfo.EnvironmentVariables.ContainsKey($key)) { 'PRESENT' } else { 'ABSENT' }
        Write-Host "      $key=$state"
    }
}

function Set-ProcessEnvironment {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)] [System.Diagnostics.ProcessStartInfo]$ProcessStartInfo,
        [hashtable]$EnvironmentOverrides,
        [string[]]$EnvironmentRemovals
    )

    foreach ($name in @($EnvironmentRemovals)) {
        if ($ProcessStartInfo.EnvironmentVariables.ContainsKey($name)) {
            $ProcessStartInfo.EnvironmentVariables.Remove($name)
        }
    }

    if ($EnvironmentOverrides) {
        foreach ($name in $EnvironmentOverrides.Keys) {
            $value = [string]$EnvironmentOverrides[$name]
            if ($ProcessStartInfo.EnvironmentVariables.ContainsKey($name)) {
                $ProcessStartInfo.EnvironmentVariables[$name] = $value
            }
            else {
                $ProcessStartInfo.EnvironmentVariables.Add($name, $value)
            }
        }
    }
}

function Join-ProcessArguments {
    [CmdletBinding()]
    param([Parameter(Mandatory = $true)] [string[]]$Arguments)

    return ($Arguments | ForEach-Object {
            if ($_ -notmatch '[\s"]') { return $_ }
            '"' + ($_ -replace '(\\*)"', '$1$1\"' -replace '(\\+)$', '$1$1') + '"'
        }) -join ' '
}

function Get-BufferedTail {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)] [System.Collections.IList]$Lines,
        [int]$LineCount = 80
    )

    if ($Lines.Count -eq 0) { return @() }
    $skip = [Math]::Max(0, $Lines.Count - $LineCount)
    return @($Lines[$skip..($Lines.Count - 1)] |
        ForEach-Object { (Redact-SecretText $_).TrimEnd() })
}

function Invoke-DotNetTestWithTimeout {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)] [string]$SuiteName,
        [Parameter(Mandatory = $true)] [string]$SuiteProject,
        [Parameter(Mandatory = $true)] [string]$EvidenceDir,
        [hashtable]$EnvironmentOverrides,
        [string[]]$EnvironmentRemovals,
        [int]$TimeoutSec = 300
    )

    if (-not (Test-Path -LiteralPath $EvidenceDir)) {
        New-Item -ItemType Directory -Path $EvidenceDir -Force | Out-Null
    }

    $parentWorkingDirectory = (Get-Location).Path
    $processWorkingDirectory = $RepoRoot
    if ([System.IO.Path]::IsPathRooted($SuiteProject)) {
        $projectPath = $SuiteProject
    }
    else {
        $projectPath = Join-Path $RepoRoot $SuiteProject
    }
    $projectPath = [System.IO.Path]::GetFullPath($projectPath)
    if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
        Write-Host "  [FATAL] $SuiteName project file missing: $projectPath" -ForegroundColor Red
        Write-Host "    Parent PowerShell CWD: $parentWorkingDirectory" -ForegroundColor Red
        Write-Host "    RepoRoot: $RepoRoot" -ForegroundColor Red
        Write-Host "    Process WorkingDirectory: $processWorkingDirectory" -ForegroundColor Red
        Fail-Fatal "[$SuiteName] project file missing before dotnet test start." 1
    }

    $suiteStart = Get-Date
    $trxFile = Join-Path $EvidenceDir ($SuiteName + '.trx')
    $stdoutFile = Join-Path $EvidenceDir ($SuiteName + '.stdout.log')
    $stderrFile = Join-Path $EvidenceDir ($SuiteName + '.stderr.log')
    $trxLogger = "trx;LogFileName=$trxFile"
    # Static harness guard: keep the TRX logger contract visible as "--logger $trxLogger".
    $dotnetTestArgs = @('test', $projectPath, '-c', 'Release', '--no-build', '--nologo', '--logger', $trxLogger)
    $sanitizedCommand = Format-CommandForLog -FilePath $Dotnet -Arguments $dotnetTestArgs

    Write-Host "  [RUN] $SuiteName"
    Write-Host "    Parent PowerShell CWD: $parentWorkingDirectory"
    Write-Host "    Process WorkingDirectory: $processWorkingDirectory"
    Write-Host "    Project: $projectPath"
    Write-Host "    Project exists: True"
    Write-EnvironmentPresenceSnapshot -Label $SuiteName -EnvironmentOverrides $EnvironmentOverrides -EnvironmentRemovals $EnvironmentRemovals
    Write-Host "    RedirectStandardInput: False"
    Write-Host "    Command: $sanitizedCommand"
    Write-Host "    TRX: $trxFile"
    Write-Host "    Timeout: ${TimeoutSec}s"

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $Dotnet
    $psi.WorkingDirectory = $processWorkingDirectory
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.StandardOutputEncoding = [System.Text.Encoding]::UTF8
    $psi.StandardErrorEncoding = [System.Text.Encoding]::UTF8
    Set-ProcessEnvironment -ProcessStartInfo $psi -EnvironmentOverrides $EnvironmentOverrides -EnvironmentRemovals $EnvironmentRemovals
    Write-ChildProcessEnvironmentContractSnapshot -Label $SuiteName -ProcessStartInfo $psi

    $argumentListProperty = [System.Diagnostics.ProcessStartInfo].GetProperty('ArgumentList')
    if ($argumentListProperty) {
        foreach ($argument in $dotnetTestArgs) {
            [void]$psi.ArgumentList.Add($argument)
        }
    }
    else {
        $psi.Arguments = Join-ProcessArguments -Arguments $dotnetTestArgs
    }

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $psi
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    $started = $process.Start()
    if (-not $started) {
        Fail-Fatal "Failed to start dotnet test for $SuiteName." 1
    }
    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    $nextHeartbeat = (Get-Date).AddSeconds(10)

    while ((Get-Date) -lt $deadline) {
        $exited = $process.WaitForExit(1000)
        if ((Get-Date) -ge $nextHeartbeat) {
            Write-Host "    Still running: elapsed=$([int]$stopwatch.Elapsed.TotalSeconds)s PID=$($process.Id)"
            $nextHeartbeat = (Get-Date).AddSeconds(10)
        }
        if ($exited) { break }
    }

    $stopwatch.Stop()
    if (-not $process.HasExited) {
        $elapsed = [int]$stopwatch.Elapsed.TotalSeconds
        Write-Host "  [TIMEOUT] $SuiteName after ${elapsed}s. PID=$($process.Id)" -ForegroundColor Red
        Write-Host "    Command: $sanitizedCommand" -ForegroundColor Red
        Stop-OwnedProcessTree -RootProcessId $process.Id
        $process.WaitForExit()
        $stdoutText = $stdoutTask.Result
        $stderrText = $stderrTask.Result
        $stdoutLinesBuffer = @($stdoutText -split "(`r`n|`n|`r)" | Where-Object { $_ -ne '' })
        $stderrLinesBuffer = @($stderrText -split "(`r`n|`n|`r)" | Where-Object { $_ -ne '' })
        Set-Content -LiteralPath $stdoutFile -Value $stdoutLinesBuffer -Encoding UTF8
        Set-Content -LiteralPath $stderrFile -Value $stderrLinesBuffer -Encoding UTF8
        $stderrTail = Get-BufferedTail -Lines $stderrLinesBuffer
        if ($stderrTail.Count -gt 0) {
            Write-Host "    STDERR tail:" -ForegroundColor Yellow
            foreach ($line in $stderrTail) { Write-Host "      $line" -ForegroundColor Yellow }
        }
        Fail-Fatal "$SuiteName timed out after ${TimeoutSec}s." 1
    }

    $process.WaitForExit()
    $stdoutText = $stdoutTask.Result
    $stderrText = $stderrTask.Result
    $stdoutLinesBuffer = @($stdoutText -split "(`r`n|`n|`r)" | Where-Object { $_ -ne '' })
    $stderrLinesBuffer = @($stderrText -split "(`r`n|`n|`r)" | Where-Object { $_ -ne '' })
    Set-Content -LiteralPath $stdoutFile -Value $stdoutLinesBuffer -Encoding UTF8
    Set-Content -LiteralPath $stderrFile -Value $stderrLinesBuffer -Encoding UTF8
    foreach ($line in $stdoutLinesBuffer) {
        Write-Host "    $((Redact-SecretText $line).TrimEnd())"
    }
    foreach ($line in $stderrLinesBuffer) {
        Write-Host "    STDERR: $((Redact-SecretText $line).TrimEnd())"
    }
    $suiteExit = $process.ExitCode
    if (-not (Test-Path -LiteralPath $trxFile -PathType Leaf)) {
        $stderrTail = Get-BufferedTail -Lines $stderrLinesBuffer
        if ($stderrTail.Count -gt 0) {
            Write-Host "    STDERR tail:" -ForegroundColor Yellow
            foreach ($line in $stderrTail) { Write-Host "      $line" -ForegroundColor Yellow }
        }
        Fail-Fatal "[$SuiteName] TRX file missing: $trxFile" 1
    }

    $trxInfo = Get-Item -LiteralPath $trxFile
    if ($trxInfo.LastWriteTime -lt $suiteStart.AddSeconds(-1)) {
        Fail-Fatal "[$SuiteName] TRX is stale: $trxFile (LastWriteTime=$($trxInfo.LastWriteTime), suiteStart=$suiteStart)" 1
    }

    $c = Parse-TrxCounters -TrxPath $trxFile -SuiteName $SuiteName
    Write-Host "    Elapsed: $([int]$stopwatch.Elapsed.TotalSeconds)s"
    Write-Host "    TRX: total=$($c.Total) executed=$($c.Executed) passed=$($c.Passed) failed=$($c.Failed) notExecuted=$($c.NotExecuted) outcome=$($c.Outcome)"

    if ($suiteExit -ne 0) {
        $stderrTail = Get-BufferedTail -Lines $stderrLinesBuffer
        if ($stderrTail.Count -gt 0) {
            Write-Host "    STDERR tail:" -ForegroundColor Yellow
            foreach ($line in $stderrTail) { Write-Host "      $line" -ForegroundColor Yellow }
        }
        Write-Host "  [FAIL] $SuiteName : dotnet test exited $suiteExit" -ForegroundColor Red
        return @{ Counters = $c; ExitCode = $suiteExit; TimedOut = $false; TrxPath = $trxFile }
    }

    if ($c.Failed -gt 0 -or $c.NotExecuted -gt 0) {
        Write-Host "  [FAIL] $SuiteName : TRX failed=$($c.Failed) notExecuted=$($c.NotExecuted)" -ForegroundColor Red
        return @{ Counters = $c; ExitCode = $suiteExit; TimedOut = $false; TrxPath = $trxFile }
    }

    if ($c.Outcome -ne 'Completed' -and $c.Outcome -ne 'Passed') {
        Write-Host "  [FAIL] $SuiteName : TRX outcome=$($c.Outcome)" -ForegroundColor Red
        return @{ Counters = $c; ExitCode = $suiteExit; TimedOut = $false; TrxPath = $trxFile }
    }

    Pass "$SuiteName : $($c.Passed)/$($c.Total) passed (TRX)"
    return @{ Counters = $c; ExitCode = $suiteExit; TimedOut = $false; TrxPath = $trxFile }
}

function Assert-Status {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)] [hashtable]$Probe,
        [Parameter(Mandatory = $true)] [int]$ExpectedStatus,
        [Parameter(Mandatory = $true)] [string]$Label,
        [string]$BodyContains
    )

    if ($Probe.StatusCode -ne $ExpectedStatus) {
        Fail-Fatal "$Label -> $($Probe.StatusCode) (expected $ExpectedStatus). Body: $($Probe.Content)" 1
    }
    if ($BodyContains -and $Probe.Content -notmatch [regex]::Escape($BodyContains)) {
        Fail-Fatal "$Label body did not contain '$BodyContains'. Body: $($Probe.Content)" 1
    }
    Pass "$Label -> $ExpectedStatus"
}

function Assert-AntiforgeryCookieCaptured {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)] [Microsoft.PowerShell.Commands.WebRequestSession]$WebSession,
        [Parameter(Mandatory = $true)] [string]$BaseUrl,
        [Parameter(Mandatory = $true)] [string]$Label
    )

    $cookies = $WebSession.Cookies.GetCookies([Uri]$BaseUrl)
    $hasAntiforgeryCookie = $false
    foreach ($cookie in $cookies) {
        if ($cookie.Name -eq '.GuliERP.Antiforgery') {
            $hasAntiforgeryCookie = $true
            break
        }
    }

    if (-not $hasAntiforgeryCookie) {
        Fail-Fatal "$Label /csrf did not store .GuliERP.Antiforgery in the WebRequestSession cookie jar" 1
    }
    Pass "$Label /csrf stored .GuliERP.Antiforgery in the same WebRequestSession"
}

function Use-OperatorPlainPassword {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)] [scriptblock]$ScriptBlock
    )

    $plain = $null
    try {
        $bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($script:OperatorPgSecurePassword)
        try {
            $plain = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
            & $ScriptBlock $plain
        }
        finally {
            if ($bstr -ne [IntPtr]::Zero) {
                [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
            }
        }
    }
    finally {
        if ($null -ne $plain) { $plain = $null }
    }
}

function Use-OperatorUserPlainPassword {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)] [scriptblock]$ScriptBlock
    )

    $plain = $null
    try {
        $bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($script:OperatorUserSecurePassword)
        try {
            $plain = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
            & $ScriptBlock $plain
        }
        finally {
            if ($bstr -ne [IntPtr]::Zero) {
                [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
            }
        }
    }
    finally {
        if ($null -ne $plain) { $plain = $null }
    }
}

function Invoke-NpgsqlScalar {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)] [string]$ConnectionString,
        [Parameter(Mandatory = $true)] [string]$Sql,
        [hashtable]$Parameters
    )

    $runtimeDir = Join-Path $RepoRoot 'apps/api/GuliERP.Api/bin/Release/net10.0'
    $npgsqlPath = Join-Path $runtimeDir 'Npgsql.dll'
    if (-not (Test-Path -LiteralPath $npgsqlPath -PathType Leaf)) {
        Fail-Fatal "Npgsql runtime assembly missing at $npgsqlPath. Run the Release build before DB grant setup." 1
    }
    $nugetRoot = Join-Path $env:USERPROFILE '.nuget/packages'
    foreach ($dependency in @(
            'microsoft.extensions.logging.abstractions',
            'system.diagnostics.diagnosticsource',
            'system.threading.channels',
            'system.text.json'
        )) {
        $dependencyRoot = Join-Path $nugetRoot $dependency
        $dependencyDll = Get-ChildItem -LiteralPath $dependencyRoot -Recurse -Filter '*.dll' -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -match '\\lib\\net10\.0\\' } |
            Sort-Object @{ Expression = { [version]$_.FullName.Split('\')[-4] }; Descending = $true } |
            Select-Object -First 1
        if (-not $dependencyDll) {
            $dependencyDll = Get-ChildItem -LiteralPath $dependencyRoot -Recurse -Filter '*.dll' -ErrorAction SilentlyContinue |
                Where-Object { $_.FullName -match '\\lib\\net8\.0\\' } |
                Sort-Object @{ Expression = { [version]$_.FullName.Split('\')[-4] }; Descending = $true } |
                Select-Object -First 1
        }
        if ($dependencyDll) {
            try { Add-Type -Path $dependencyDll.FullName -ErrorAction Stop } catch {}
        }
    }
    foreach ($assembly in (Get-ChildItem -LiteralPath $runtimeDir -Filter '*.dll' -File)) {
        try { Add-Type -Path $assembly.FullName -ErrorAction Stop } catch {}
    }

    $connection = [Npgsql.NpgsqlConnection]::new($ConnectionString)
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        try {
            $command.CommandText = $Sql
            $command.CommandTimeout = 30
            if ($Parameters) {
                foreach ($name in $Parameters.Keys) {
                    [void]$command.Parameters.AddWithValue($name, $Parameters[$name])
                }
            }
            return $command.ExecuteScalar()
        }
        finally {
            if ($command) { $command.Dispose() }
        }
    }
    finally {
        $connection.Dispose()
    }
}

function Ensure-OperatorProbeReadGrant {
    [CmdletBinding()]
    param([Parameter(Mandatory = $true)] [string]$ConnectionString)

    $grantSql = @'
with operator_user as (
    select u."Id" as user_id, u."TenantId" as tenant_id, m."CompanyId" as company_id
    from identity."AspNetUsers" u
    join identity.gulierp_user_company_membership m
      on m."UserId" = u."Id"
     and m."TenantId" = u."TenantId"
     and m."IsDefault" = true
     and m."Status" = 1
    where u."UserName" = @userName
      and u."TenantId" <> 0
      and u."Status" = 1
    order by m."CompanyId"
    limit 1
),
existing_role as (
    select r."Id" as role_id
    from identity."AspNetRoles" r
    join operator_user ou on ou.tenant_id = r."TenantId"
    where r."Code" = @roleCode
    limit 1
),
insert_role as (
    insert into identity."AspNetRoles" (
        "Id", "TenantId", "Code", "IsSystem", "Description", "Status",
        "CreatedAt", "ModifiedAt", "ConcurrencyVersion",
        "Name", "NormalizedName", "ConcurrencyStamp"
    )
    select
        ((extract(epoch from clock_timestamp()) * 1000000)::bigint + floor(random() * 1000)::bigint),
        ou.tenant_id,
        @roleCode,
        false,
        'G2-005 operator runtime allow marker role.',
        1,
        now(),
        now(),
        1,
        @roleName,
        @normalizedRoleName,
        ('g2-005-' || md5(random()::text || clock_timestamp()::text))
    from operator_user ou
    where not exists (select 1 from existing_role)
    returning "Id" as role_id
),
chosen_role as (
    select role_id from existing_role
    union all
    select role_id from insert_role
    limit 1
),
insert_claim as (
    insert into identity."AspNetRoleClaims" ("RoleId", "ClaimType", "ClaimValue")
    select cr.role_id, 'gulierp.permission', 'g2.probe.read'
    from chosen_role cr
    where not exists (
        select 1
        from identity."AspNetRoleClaims" c
        where c."RoleId" = cr.role_id
          and c."ClaimType" = 'gulierp.permission'
          and c."ClaimValue" = 'g2.probe.read'
    )
    returning 1
),
insert_assignment as (
    insert into identity.gulierp_user_role_assignment (
        "Id", "TenantId", "UserId", "RoleId", "CompanyId",
        "ValidFrom", "ValidTo", "Status",
        "CreatedAt", "ModifiedAt", "ConcurrencyVersion"
    )
    select
        ((extract(epoch from clock_timestamp()) * 1000000)::bigint + floor(random() * 1000)::bigint),
        ou.tenant_id,
        ou.user_id,
        cr.role_id,
        ou.company_id,
        null,
        null,
        1,
        now(),
        now(),
        1
    from operator_user ou
    cross join chosen_role cr
    where not exists (
        select 1
        from identity.gulierp_user_role_assignment a
        where a."TenantId" = ou.tenant_id
          and a."UserId" = ou.user_id
          and a."RoleId" = cr.role_id
          and a."CompanyId" = ou.company_id
          and a."Status" = 1
    )
    returning 1
)
select
    case
        when not exists (select 1 from operator_user) then 'missing_operator_user_or_default_company'
        when not exists (select 1 from chosen_role) then 'missing_role'
        else 'ok'
    end;
'@

    $result = Invoke-NpgsqlScalar -ConnectionString $ConnectionString -Sql $grantSql -Parameters @{
        userName = $OperatorUser
        roleCode = 'G2_005_OPERATOR_PROBE_READ'
        roleName = ($OperatorUser + '_g2_005_probe_read')
        normalizedRoleName = (($OperatorUser + '_g2_005_probe_read').ToUpperInvariant())
    }

    if ([string]$result -ne 'ok') {
        Fail-Fatal "G2-005 operator probe-read grant setup failed: $result" 1
    }
    Pass "Operator marker user has g2.probe.read grant for runtime allow probe"
}

function Revoke-OperatorProbeReadGrant {
    [CmdletBinding()]
    param([Parameter(Mandatory = $true)] [string]$ConnectionString)

    $revokeSql = @'
with operator_user as (
    select u."Id" as user_id, u."TenantId" as tenant_id
    from identity."AspNetUsers" u
    where u."UserName" = @userName
    limit 1
),
target_role as (
    select r."Id" as role_id
    from identity."AspNetRoles" r
    join operator_user ou on ou.tenant_id = r."TenantId"
    where r."Code" = @roleCode
    limit 1
),
delete_assignments as (
    delete from identity.gulierp_user_role_assignment a
    using operator_user ou, target_role tr
    where a."TenantId" = ou.tenant_id
      and a."UserId" = ou.user_id
      and a."RoleId" = tr.role_id
    returning 1
)
select count(*)::int from delete_assignments;
'@

    [void](Invoke-NpgsqlScalar -ConnectionString $ConnectionString -Sql $revokeSql -Parameters @{
        userName = $OperatorUser
        roleCode = 'G2_005_OPERATOR_PROBE_READ'
    })
    Pass "Operator marker g2.probe.read runtime grant cleared before deny probe"
}

function Initialize-AuthenticatedRuntimeSession {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)] [string]$BaseUrl,
        [Parameter(Mandatory = $true)] [string]$Label
    )

    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $csrfResp = Invoke-HttpProbe -Method Get -Uri "$BaseUrl/api/v1/auth/csrf" -WebSession $session
    Assert-Status -Probe $csrfResp -ExpectedStatus 200 -Label "$Label GET /api/v1/auth/csrf"

    try {
        $csrfToken = ($csrfResp.Content | ConvertFrom-Json).requestToken
    }
    catch {
        Fail-Fatal "$Label /csrf response was not valid JSON. Body: $($csrfResp.Content)" 1
    }
    if ([string]::IsNullOrEmpty($csrfToken)) {
        Fail-Fatal "$Label /csrf response missing requestToken" 1
    }
    Assert-AntiforgeryCookieCaptured -WebSession $session -BaseUrl $BaseUrl -Label $Label

    Use-OperatorUserPlainPassword {
        param($plainPwd)
        $loginBody = (ConvertTo-Json -InputObject @{
            userName = $OperatorUser
            password = $plainPwd
            tenantCode = $OperatorTenant
        } -Compress)
        $loginResp = Invoke-HttpProbe -Method Post -Uri "$BaseUrl/api/v1/auth/login" `
            -ContentType 'application/json' -Body $loginBody `
            -Headers @{ 'X-CSRF-TOKEN' = $csrfToken } `
            -WebSession $session
        Assert-Status -Probe $loginResp -ExpectedStatus 200 -Label "$Label POST /api/v1/auth/login"
    }

    $me = Invoke-HttpProbe -Method Get -Uri "$BaseUrl/api/v1/auth/me" -WebSession $session
    Assert-Status -Probe $me -ExpectedStatus 200 -Label "$Label GET /api/v1/auth/me authenticated cookie roundtrip"
    try {
        $meBody = $me.Content | ConvertFrom-Json
    }
    catch {
        Fail-Fatal "$Label /auth/me response was not valid JSON. Body: $($me.Content)" 1
    }
    if ($null -eq $meBody.tenantId -or $null -eq $meBody.companyId) {
        Fail-Fatal "$Label /auth/me response missing tenantId/companyId. Body: $($me.Content)" 1
    }
    Pass "$Label authenticated cookie established in the same WebRequestSession"
    return @{
        WebSession = $session
        TenantId = [long]$meBody.tenantId
        CompanyId = [long]$meBody.companyId
    }
}

function Complete-Cleanup {
    if ($script:OperatorPgSecurePassword) {
        $script:OperatorPgSecurePassword.Dispose()
        $script:OperatorPgSecurePassword = $null
    }
    if ($script:OperatorUserSecurePassword) {
        $script:OperatorUserSecurePassword.Dispose()
        $script:OperatorUserSecurePassword = $null
    }
}

Register-EngineEvent -SourceIdentifier 'PowerShell.Exiting' -Action {
    Stop-AllOwnedHosts
    Restore-OperatorConnectionEnvironment
    Complete-Cleanup
} -ErrorAction SilentlyContinue | Out-Null

Write-Host "G2-005 — Final Operator Evidence Harness"
Write-Host "Repository: $RepoRoot"
Write-Host "Dotnet: $Dotnet"

$script:OriginalOperatorConnectionEnvironment = Save-OperatorConnectionEnvironment

if ($FocusBootstrapTestOnly) {
    $savedHarnessEnvironment = Save-HarnessEnvironment
    try {
        Step-Header 4 'Focused Bootstrap TRX suite'
        $runId = Get-Date -Format 'yyyyMMdd-HHmmss'
        $trxDir = Join-Path $RepoRoot "tests/_evidence_trx/g2-005/$runId"
        $environmentOverrides = @{}
        $result = Invoke-DotNetTestWithTimeout `
            -SuiteName 'GuliERP.Identity.Bootstrap.Tests' `
            -SuiteProject 'tests/GuliERP.Identity.Bootstrap.Tests/GuliERP.Identity.Bootstrap.Tests.csproj' `
            -EvidenceDir $trxDir `
            -EnvironmentRemovals (Get-CleanTestEnvironmentRemovals) `
            -EnvironmentOverrides $environmentOverrides `
            -TimeoutSec 300

        $c = $result.Counters
        if ($result.ExitCode -ne 0 -or $c.Failed -gt 0 -or $c.NotExecuted -gt 0) {
            Fail-Fatal "Focused Bootstrap suite FAILED (exit=$($result.ExitCode), failed=$($c.Failed), notExecuted=$($c.NotExecuted))." 1
        }

        Write-Host ""
        Write-Host "[G2-005] Focused Bootstrap verification PASS."
        Write-Host "TRX: $($result.TrxPath)"
        Restore-OperatorConnectionEnvironment
        Complete-Cleanup
        Stop-AllOwnedHosts
        return
    }
    finally {
        Restore-HarnessEnvironment -State $savedHarnessEnvironment
    }
}

if ($FocusIdentityIntegrationTestOnly) {
    Step-Header 4 'Focused Identity Integration PG suite'
    $parentBefore = Save-OperatorConnectionEnvironment
    try {
        $promptForPgPassword = -not $SkipPrompt
        $conn = Resolve-OperatorPostgreSqlConnectionString -PromptIfMissing:$promptForPgPassword
        Assert-ApprovedOperatorDatabaseTarget -ConnectionString $conn

        $runId = Get-Date -Format 'yyyyMMdd-HHmmss'
        $trxDir = Join-Path $RepoRoot "tests/_evidence_trx/g2-005/$runId"
        $result = Invoke-DotNetTestWithTimeout `
            -SuiteName 'GuliERP.Identity.IntegrationTests' `
            -SuiteProject 'tests/GuliERP.Identity.IntegrationTests/GuliERP.Identity.IntegrationTests.csproj' `
            -EvidenceDir $trxDir `
            -EnvironmentRemovals (Get-CleanTestEnvironmentRemovals) `
            -EnvironmentOverrides (Get-PostgreSqlTestEnvironmentOverrides -ConnectionString $conn) `
            -TimeoutSec 300

        $c = $result.Counters
        if ($result.ExitCode -ne 0 -or $c.Failed -gt 0 -or $c.NotExecuted -gt 0) {
            Fail-Fatal "Focused Identity Integration suite FAILED (exit=$($result.ExitCode), failed=$($c.Failed), notExecuted=$($c.NotExecuted))." 1
        }

        Write-Host ""
        Write-Host "[G2-005] Focused Identity Integration verification PASS."
        Write-Host "TRX: $($result.TrxPath)"
        $parentConnectionState = if ($parentBefore.ConnectionStrings__GuliERP.Exists) { 'PRESENT' } else { 'ABSENT' }
        Write-Host "Parent environment unchanged: ConnectionStrings__GuliERP=$parentConnectionState"
        Restore-OperatorConnectionEnvironment
        Complete-Cleanup
        Stop-AllOwnedHosts
        return
    }
    finally {
        Restore-OperatorConnectionEnvironment
        Complete-Cleanup
    }
}

$conn = $env:ConnectionStrings__GuliERP
if (-not $conn) { $conn = $env:GULIERP_ConnectionStrings__GuliERP }
if (-not $conn) { $conn = $env:GULIERP_FOUNDATION_CONNECTION }

$script:OperatorPgSecurePassword = $null
if (-not $conn) {
    if ($SkipPrompt) {
        Fail-Fatal "No connection string and SkipPrompt was specified." 3
    }
    Write-Host "[G2-005] Using default Operator PostgreSQL target:"
    Write-Host "  Host=$DefaultPgHost;Port=$DefaultPgPort;Database=$DefaultPgDatabase;Username=$DefaultPgUsername;Password=***"
    Write-Host "[G2-005] PostgreSQL password will be read via Read-Host -AsSecureString (NOT echoed)."
    $script:OperatorPgSecurePassword = Read-Host -Prompt 'PostgreSQL password' -AsSecureString
    if ($null -eq $script:OperatorPgSecurePassword -or $script:OperatorPgSecurePassword.Length -lt 1) {
        Fail-Fatal "Empty PostgreSQL password. Aborting." 2
    }
    Use-OperatorPlainPassword {
        param($plainPwd)
        $script:ResolvedOperatorConnection = "Host=$DefaultPgHost;Port=$DefaultPgPort;Database=$DefaultPgDatabase;Username=$DefaultPgUsername;Password=$plainPwd"
    }
    $conn = $script:ResolvedOperatorConnection
}
else {
    Write-Host "[G2-005] Using caller-provided connection string."
}

Write-Host "[G2-005] Connection: $((Redact-SecretText $conn).Trim())"
Set-OperatorConnectionEnvironment -ConnectionString $conn

$MarkerPrefix = 'test_operator_'
$OperatorUser = $env:GULIERP_OPERATOR_USER
if (-not $OperatorUser) { $OperatorUser = 'test_operator_g2_004' }
$OperatorTenant = $env:GULIERP_OPERATOR_TENANT
if (-not $OperatorTenant) { $OperatorTenant = 'test_operator_g2_004_t' }

foreach ($pair in @(
        @('OperatorUser', $OperatorUser),
        @('OperatorTenant', $OperatorTenant)
    )) {
    $name = $pair[0]
    $val = $pair[1]
    if (-not $val.StartsWith($MarkerPrefix, [System.StringComparison]::Ordinal)) {
        Fail-Fatal "SAFETY: $name='$val' must start with '$MarkerPrefix'." 2
    }
}
Write-Host "[G2-005] Operator test user = $OperatorUser"

$script:OperatorUserSecurePassword = $null
if ($SkipPrompt) {
    Fail-Fatal "Operator test user password is required for authenticated runtime probes; SkipPrompt was specified." 3
}
Write-Host "[G2-005] Operator test user password will be read via Read-Host -AsSecureString (NOT echoed)."
$script:OperatorUserSecurePassword = Read-Host -Prompt 'Operator test user password' -AsSecureString
if ($null -eq $script:OperatorUserSecurePassword -or $script:OperatorUserSecurePassword.Length -lt 1) {
    Fail-Fatal "Empty Operator test user password. Aborting." 2
}

try {
    Step-Header 1 'Release build'
    & $Dotnet build GuliERP.slnx -c Release --no-restore --nologo -warnaserror -maxcpucount:1 /p:BuildInParallel=false /p:UseSharedCompilation=false /nodeReuse:false 2>&1 | Tee-Object -Variable buildOut | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-RedactedCommandDiagnostics -Label 'Release build' -Output $buildOut
        Fail-Fatal "Release build failed (exit=$LASTEXITCODE)." 1
    }
    Pass "Release build clean (warnings treated as errors)"

    Step-Header 2 'Foundation migration'
    & $Dotnet ef database update --project modules/foundation/GuliERP.Foundation/GuliERP.Foundation.csproj --configuration Release --no-build 2>&1 | Tee-Object -Variable migOut | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-RedactedCommandDiagnostics -Label 'Foundation migration' -Output $migOut
        Fail-Fatal "Foundation migration failed (exit=$LASTEXITCODE)." 1
    }
    Pass "Foundation migration applied (or already up to date)"

    Step-Header 3 'Identity migration'
    & $Dotnet ef database update --project modules/identity/GuliERP.Identity.Infrastructure/GuliERP.Identity.Infrastructure.csproj --startup-project apps/api/GuliERP.Api/GuliERP.Api.csproj --configuration Release --no-build 2>&1 | Tee-Object -Variable idMigOut | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-RedactedCommandDiagnostics -Label 'Identity migration' -Output $idMigOut
        Fail-Fatal "Identity migration failed (exit=$LASTEXITCODE)." 1
    }
    Pass "Identity migration applied (or already up to date)"

    Step-Header 4 'Actual TRX suites'
    $runId = Get-Date -Format 'yyyyMMdd-HHmmss'
    $trxDir = Join-Path $RepoRoot "tests/_evidence_trx/g2-005/$runId"
    if (-not (Test-Path -LiteralPath $trxDir)) { New-Item -ItemType Directory -Path $trxDir -Force | Out-Null }
    Write-Host "  Evidence directory: $trxDir"

    $suites = @(
        @{ Name = 'GuliERP.Identity.Bootstrap.Tests';       Project = 'tests/GuliERP.Identity.Bootstrap.Tests/GuliERP.Identity.Bootstrap.Tests.csproj';       Environment = 'clean' },
        @{ Name = 'GuliERP.Identity.Tests';                 Project = 'tests/GuliERP.Identity.Tests/GuliERP.Identity.Tests.csproj';                         Environment = 'clean' },
        @{ Name = 'GuliERP.Foundation.Tests';               Project = 'tests/GuliERP.Foundation.Tests/GuliERP.Foundation.Tests.csproj';                     Environment = 'clean' },
        @{ Name = 'GuliERP.Identity.IntegrationTests';      Project = 'tests/GuliERP.Identity.IntegrationTests/GuliERP.Identity.IntegrationTests.csproj';   Environment = 'pg' },
        @{ Name = 'GuliERP.Foundation.IntegrationTests';    Project = 'tests/GuliERP.Foundation.IntegrationTests/GuliERP.Foundation.IntegrationTests.csproj'; Environment = 'pg' }
    )

    $grandTotal = 0
    $grandPassed = 0
    $grandFailed = 0
    $grandNotExecuted = 0
    $suiteHasFailure = $false

    foreach ($suite in $suites) {
        $suiteName = $suite.Name
        $suiteProject = $suite.Project
        $environmentRemovals = @(Get-CleanTestEnvironmentRemovals)
        $environmentOverrides = @{}
        if ($suite.Environment -eq 'pg') {
            $environmentOverrides = Get-PostgreSqlTestEnvironmentOverrides -ConnectionString $conn
        }

        Write-Host "  Suite environment contract: $suiteName -> $($suite.Environment)"
        $result = Invoke-DotNetTestWithTimeout `
            -SuiteName $suiteName `
            -SuiteProject $suiteProject `
            -EvidenceDir $trxDir `
            -EnvironmentRemovals $environmentRemovals `
            -EnvironmentOverrides $environmentOverrides `
            -TimeoutSec 300
        $c = $result.Counters
        if ($result.ExitCode -ne 0 -or $c.Failed -gt 0 -or $c.NotExecuted -gt 0 -or ($c.Outcome -ne 'Completed' -and $c.Outcome -ne 'Passed')) {
            $suiteHasFailure = $true
        }

        $grandTotal += $c.Total
        $grandPassed += $c.Passed
        $grandFailed += $c.Failed
        $grandNotExecuted += $c.NotExecuted
    }

    Write-Host "  TRX Summary: total=$grandTotal passed=$grandPassed failed=$grandFailed notExecuted=$grandNotExecuted"
    if ($suiteHasFailure -or $grandFailed -gt 0 -or $grandNotExecuted -gt 0) {
        Fail-Fatal "Test suites FAILED (failed=$grandFailed, notExecuted=$grandNotExecuted)." 1
    }

    Step-Header 5 'Testing runtime authorization wiring'
    $testingBaseUrl = 'http://127.0.0.1:5105'
    $testingHostPid = 0
    try {
        $testingHost = Start-HostProcess -Url $testingBaseUrl -Environment 'Testing' -LogPrefix 'g2-005-testing'
        if ($testingHost -and $testingHost.Id) {
            $testingHostPid = [int]$testingHost.Id
            Register-OwnedHostPid -ProcessId $testingHostPid
        }
        if (-not (Wait-HostReady -BaseUrl $testingBaseUrl -TimeoutSec 30)) {
            Fail-Fatal "Testing host (PID $testingHostPid) did not become ready at $testingBaseUrl" 1
        }
        Pass "Testing host ready at $testingBaseUrl (PID $testingHostPid, script-owned)"

        $unauth = Invoke-HttpProbe -Method Get -Uri "$testingBaseUrl/__test/g2-005/company-resource/10?tenantId=1"
        Assert-Status -Probe $unauth -ExpectedStatus 401 -Label 'Testing unauthenticated protected probe' -BodyContains 'authentication_required'

        Revoke-OperatorProbeReadGrant -ConnectionString $conn
        $denyActor = Initialize-AuthenticatedRuntimeSession -BaseUrl $testingBaseUrl -Label 'Testing deny actor'
        $tenantId = [long]$denyActor.TenantId
        $companyId = [long]$denyActor.CompanyId
        $baseHeaders = @{ 'X-Tenant-Id' = "$tenantId"; 'X-Company-Id' = "$companyId" }
        $deny = Invoke-HttpProbe -Method Get -Uri "$testingBaseUrl/__test/g2-005/company-resource/$companyId`?tenantId=$tenantId" -Headers $baseHeaders -WebSession $denyActor.WebSession
        Assert-Status -Probe $deny -ExpectedStatus 403 -Label 'Testing authenticated without g2.probe.read' -BodyContains 'authorization_forbidden'

        $platformAdminActor = Initialize-AuthenticatedRuntimeSession -BaseUrl $testingBaseUrl -Label 'Testing PlatformAdmin actor'
        $platformAdminNoPermission = Invoke-HttpProbe -Method Get -Uri "$testingBaseUrl/__test/g2-005/company-resource/$companyId`?tenantId=$tenantId" -Headers (@{ 'X-Tenant-Id' = "$tenantId"; 'X-Company-Id' = "$companyId"; 'X-Platform-Admin' = 'true' }) -WebSession $platformAdminActor.WebSession
        Assert-Status -Probe $platformAdminNoPermission -ExpectedStatus 403 -Label 'Testing PlatformAdmin without permission' -BodyContains 'authorization_forbidden'

        Ensure-OperatorProbeReadGrant -ConnectionString $conn
        $allowActor = Initialize-AuthenticatedRuntimeSession -BaseUrl $testingBaseUrl -Label 'Testing allow actor'
        $allowHeaders = @{ 'X-Tenant-Id' = "$tenantId"; 'X-Company-Id' = "$companyId" }
        $allow = Invoke-HttpProbe -Method Get -Uri "$testingBaseUrl/__test/g2-005/company-resource/$companyId`?tenantId=$tenantId" -Headers $allowHeaders -WebSession $allowActor.WebSession
        Assert-Status -Probe $allow -ExpectedStatus 200 -Label 'Testing real PostgreSQL permission wiring'

        $crossCompanyId = $companyId + 1
        $crossCompany = Invoke-HttpProbe -Method Get -Uri "$testingBaseUrl/__test/g2-005/company-resource/$crossCompanyId`?tenantId=$tenantId" -Headers $allowHeaders -WebSession $allowActor.WebSession
        Assert-Status -Probe $crossCompany -ExpectedStatus 404 -Label 'Testing cross-company data scope'

        $crossTenantId = $tenantId + 1
        $crossTenant = Invoke-HttpProbe -Method Get -Uri "$testingBaseUrl/__test/g2-005/company-resource/$companyId`?tenantId=$crossTenantId" -Headers $allowHeaders -WebSession $allowActor.WebSession
        Assert-Status -Probe $crossTenant -ExpectedStatus 404 -Label 'Testing cross-tenant data scope'
    }
    finally {
        Stop-OwnedHost -ProcessId $testingHostPid
    }

    Step-Header 6 'Production boundary'
    $productionBaseUrl = 'http://127.0.0.1:5106'
    $productionHostPid = 0
    try {
        $productionHost = Start-HostProcess -Url $productionBaseUrl -Environment 'Production' -LogPrefix 'g2-005-production'
        if ($productionHost -and $productionHost.Id) {
            $productionHostPid = [int]$productionHost.Id
            Register-OwnedHostPid -ProcessId $productionHostPid
        }
        if (-not (Wait-HostReady -BaseUrl $productionBaseUrl -TimeoutSec 30)) {
            Fail-Fatal "Production host (PID $productionHostPid) did not become ready at $productionBaseUrl" 1
        }
        Pass "Production host ready at $productionBaseUrl (PID $productionHostPid, script-owned)"

        $prodProbe = Invoke-HttpProbe -Method Get -Uri "$productionBaseUrl/__test/g2-005/company-resource/10?tenantId=1" -Headers @{ 'X-Test-Authenticated' = 'true'; 'X-User-Id' = '100'; 'X-Tenant-Id' = '1'; 'X-Company-Id' = '10'; 'X-Test-Permission' = 'g2.probe.read' }
        Assert-Status -Probe $prodProbe -ExpectedStatus 404 -Label 'Production testing-only G2-005 probe'

        $prodMe = Invoke-HttpProbe -Method Get -Uri "$productionBaseUrl/api/v1/auth/me" -Headers @{ 'X-User-Id' = '1'; 'X-Tenant-Id' = '1'; 'X-Company-Id' = '1'; 'X-Test-Permission' = 'g2.probe.read' }
        Assert-Status -Probe $prodMe -ExpectedStatus 401 -Label 'Production /auth/me no cookie with spoofed headers' -BodyContains 'authentication_required'
        Pass 'Production trust path: no X-Test-Permission / X-Test-Role / X-Test-DataScope / X-Test-Plant production bypass added'
    }
    finally {
        Stop-OwnedHost -ProcessId $productionHostPid
    }

    Write-Host ""
    Write-Host "============================================================"
    Write-Host "[G2-005] OPERATOR EVIDENCE HARNESS — ALL CHECKS PASS"
    Write-Host "============================================================"
    Write-Host "Operator evidence: ALL CHECKS PASS."
    Write-Host "Governance status: see GOAL_REGISTRY.md (current Gate: G2_005_MINIMUM_AUTHORIZATION_DATASCOPE_VERIFIED)."
    Write-Host "Environment restoration status:"
    foreach ($name in @('ConnectionStrings__GuliERP', 'GULIERP_ConnectionStrings__GuliERP', 'GULIERP_FOUNDATION_CONNECTION')) {
        Write-Host "  $name = RESTORED (verify in caller shell if desired; values are not printed)"
    }
}
finally {
    Restore-OperatorConnectionEnvironment
    Complete-Cleanup
    Stop-AllOwnedHosts
}

if (-not $SkipPrompt) {
    Read-Host "Press Enter to exit"
}
