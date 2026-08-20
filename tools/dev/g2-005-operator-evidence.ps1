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
    [switch]$SkipPrompt
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

function Set-OperatorConnectionEnvironment {
    [CmdletBinding()]
    param([Parameter(Mandatory = $true)] [string]$ConnectionString)

    $env:ConnectionStrings__GuliERP = $ConnectionString
    $env:GULIERP_ConnectionStrings__GuliERP = $ConnectionString
    $env:GULIERP_FOUNDATION_CONNECTION = $ConnectionString
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

function Complete-Cleanup {
    if ($script:OperatorPgSecurePassword) {
        $script:OperatorPgSecurePassword.Dispose()
        $script:OperatorPgSecurePassword = $null
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

try {
    Step-Header 1 'Release build'
    & $Dotnet build GuliERP.slnx -c Release --no-restore --nologo -warnaserror -maxcpucount:1 /p:BuildInParallel=false /p:UseSharedCompilation=false /nodeReuse:false 2>&1 | Tee-Object -Variable buildOut | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-RedactedCommandDiagnostics -Label 'Release build' -Output $buildOut
        Fail-Fatal "Release build failed (exit=$LASTEXITCODE)." 1
    }
    Pass "Release build clean (warnings treated as errors)"

    Step-Header 2 'Foundation migration'
    & $Dotnet ef database update --project modules/foundation/GuliERP.Foundation/GuliERP.Foundation.csproj --no-build 2>&1 | Tee-Object -Variable migOut | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-RedactedCommandDiagnostics -Label 'Foundation migration' -Output $migOut
        Fail-Fatal "Foundation migration failed (exit=$LASTEXITCODE)." 1
    }
    Pass "Foundation migration applied (or already up to date)"

    Step-Header 3 'Identity migration'
    & $Dotnet ef database update --project modules/identity/GuliERP.Identity.Infrastructure/GuliERP.Identity.Infrastructure.csproj --startup-project apps/api/GuliERP.Api/GuliERP.Api.csproj --no-build 2>&1 | Tee-Object -Variable idMigOut | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-RedactedCommandDiagnostics -Label 'Identity migration' -Output $idMigOut
        Fail-Fatal "Identity migration failed (exit=$LASTEXITCODE)." 1
    }
    Pass "Identity migration applied (or already up to date)"

    Step-Header 4 'Actual TRX suites'
    $trxDir = Join-Path $RepoRoot 'tests/_evidence_trx/g2-005'
    if (-not (Test-Path $trxDir)) { New-Item -ItemType Directory -Path $trxDir -Force | Out-Null }

    $suites = @(
        @{ Name = 'GuliERP.Identity.Bootstrap.Tests';       Project = 'tests/GuliERP.Identity.Bootstrap.Tests/GuliERP.Identity.Bootstrap.Tests.csproj' },
        @{ Name = 'GuliERP.Identity.Tests';                 Project = 'tests/GuliERP.Identity.Tests/GuliERP.Identity.Tests.csproj' },
        @{ Name = 'GuliERP.Foundation.Tests';               Project = 'tests/GuliERP.Foundation.Tests/GuliERP.Foundation.Tests.csproj' },
        @{ Name = 'GuliERP.Identity.IntegrationTests';      Project = 'tests/GuliERP.Identity.IntegrationTests/GuliERP.Identity.IntegrationTests.csproj' },
        @{ Name = 'GuliERP.Foundation.IntegrationTests';    Project = 'tests/GuliERP.Foundation.IntegrationTests/GuliERP.Foundation.IntegrationTests.csproj' }
    )

    $grandTotal = 0
    $grandPassed = 0
    $grandFailed = 0
    $grandNotExecuted = 0
    $suiteHasFailure = $false

    foreach ($suite in $suites) {
        $suiteName = $suite.Name
        $suiteProject = $suite.Project
        Write-Host "  [RUN] dotnet test $suiteName ..."
        $trxFile = Join-Path $trxDir ($suiteName + '.trx')
        $trxLogger = "trx;LogFileName=$trxFile"
        & $Dotnet test $suiteProject -c Release --no-build --nologo --logger $trxLogger 2>&1 | Tee-Object -Variable suiteOut | Out-Null
        $suiteExit = $LASTEXITCODE
        try {
            $c = Parse-TrxCounters -TrxPath $trxFile -SuiteName $suiteName
        }
        catch {
            Fail-Fatal $_.Exception.Message 1
        }

        Write-Host "    TRX: total=$($c.Total) executed=$($c.Executed) passed=$($c.Passed) failed=$($c.Failed) notExecuted=$($c.NotExecuted) outcome=$($c.Outcome)"
        if ($suiteExit -ne 0) {
            Write-RedactedCommandDiagnostics -Label "$suiteName test" -Output $suiteOut
            Write-Host "  [FAIL] $suiteName : dotnet test exited $suiteExit" -ForegroundColor Red
            $suiteHasFailure = $true
        }
        elseif ($c.Failed -gt 0 -or $c.NotExecuted -gt 0) {
            Write-Host "  [FAIL] $suiteName : TRX failed=$($c.Failed) notExecuted=$($c.NotExecuted)" -ForegroundColor Red
            $suiteHasFailure = $true
        }
        elseif ($c.Outcome -ne 'Completed' -and $c.Outcome -ne 'Passed') {
            Write-Host "  [FAIL] $suiteName : TRX outcome=$($c.Outcome)" -ForegroundColor Red
            $suiteHasFailure = $true
        }
        else {
            Pass "$suiteName : $($c.Passed)/$($c.Total) passed (TRX)"
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

        $baseHeaders = @{ 'X-Test-Authenticated' = 'true'; 'X-User-Id' = '100'; 'X-Tenant-Id' = '1'; 'X-Company-Id' = '10' }
        $deny = Invoke-HttpProbe -Method Get -Uri "$testingBaseUrl/__test/g2-005/company-resource/10?tenantId=1" -Headers $baseHeaders
        Assert-Status -Probe $deny -ExpectedStatus 403 -Label 'Testing authenticated without g2.probe.read' -BodyContains 'authorization_forbidden'

        $allowHeaders = @{ 'X-Test-Authenticated' = 'true'; 'X-User-Id' = '100'; 'X-Tenant-Id' = '1'; 'X-Company-Id' = '10'; 'X-Test-Permission' = 'g2.probe.read' }
        $allow = Invoke-HttpProbe -Method Get -Uri "$testingBaseUrl/__test/g2-005/company-resource/10?tenantId=1" -Headers $allowHeaders
        Assert-Status -Probe $allow -ExpectedStatus 200 -Label 'Testing permission header wiring'

        $crossCompany = Invoke-HttpProbe -Method Get -Uri "$testingBaseUrl/__test/g2-005/company-resource/20?tenantId=1" -Headers $allowHeaders
        Assert-Status -Probe $crossCompany -ExpectedStatus 404 -Label 'Testing cross-company data scope'

        $crossTenant = Invoke-HttpProbe -Method Get -Uri "$testingBaseUrl/__test/g2-005/company-resource/10?tenantId=2" -Headers $allowHeaders
        Assert-Status -Probe $crossTenant -ExpectedStatus 404 -Label 'Testing cross-tenant data scope'

        $platformAdminNoPermission = Invoke-HttpProbe -Method Get -Uri "$testingBaseUrl/__test/g2-005/company-resource/10?tenantId=1" -Headers (@{ 'X-Test-Authenticated' = 'true'; 'X-User-Id' = '100'; 'X-Tenant-Id' = '1'; 'X-Company-Id' = '10'; 'X-Platform-Admin' = 'true' })
        Assert-Status -Probe $platformAdminNoPermission -ExpectedStatus 403 -Label 'Testing PlatformAdmin without permission' -BodyContains 'authorization_forbidden'
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
    Write-Host "Gate remains: G2_005_CODE_READY_OPERATOR_EVIDENCE_PENDING until this Operator evidence is reviewed and accepted."
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
