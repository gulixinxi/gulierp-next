#requires -Version 5.1
<#
.SYNOPSIS
    WEB-PREVIEW-001A + G2-004 / G2-005 — Read-only diagnostic
    for an operator / preview user.

.DESCRIPTION
    Answers the 5 non-secret diagnostic questions:

        EXISTS          YES / NO
        ACTIVE          YES / NO
        LOCKED          YES / NO
        TENANT_BINDING  VALID / INVALID
        COMPANY_BINDING  VALID / INVALID

    Optionally, when -VerifyPassword is passed, also runs:

        PASSWORD_VERIFICATION  MATCH / NO_MATCH

    The script NEVER prints:
      - PasswordHash
      - SecurityStamp
      - ConcurrencyToken
      - any other secret field

    The script is read-only: it does not insert / update /
    delete any row. It uses the same .NET bootstrap binary
    path as a host so it can resolve the User via
    UserManager.FindByNameAsync (which loads from the
    canonical Identity schema).

    The script refuses to query any user WITHOUT a marker
    prefix ("test_operator_" or "web_preview_"). This is a
    defense against accidental prod-user diagnosis.

.PARAMETER UserName
    The userName to diagnose. Default = test_operator_g2_004.
    MUST start with a marker prefix.

.PARAMETER VerifyPassword
    If set, prompts for a password via Read-Host -AsSecureString
    and runs CheckPasswordAsync. The result is MATCH / NO_MATCH
    only; the password is NEVER echoed.

.PARAMETER ConnectionString
    Optional. If not provided, the script reads
    $env:ConnectionStrings__GuliERP.

.EXAMPLE
    PS> .\diagnose-operator-user.ps1

.EXAMPLE
    PS> .\diagnose-operator-user.ps1 -UserName web_preview_admin

.EXAMPLE
    PS> .\diagnose-operator-user.ps1 -VerifyPassword
    (prompts for the candidate password; result: MATCH / NO_MATCH)
#>
[CmdletBinding()]
param(
    [string]$UserName = 'test_operator_g2_004',
    [switch]$VerifyPassword,
    [string]$ConnectionString
)

$ErrorActionPreference = 'Stop'
Set-Location -Path (Join-Path $PSScriptRoot '..\..')

# The script supports BOTH marker prefixes. We accept either
# the G2-004 default ("test_operator_") or the WEB-PREVIEW
# override ("web_preview_"). This lets the Operator diagnose
# either user without an explicit -MarkerPrefix parameter.
$ACCEPTED_MARKERS = @('test_operator_', 'web_preview_')
$MARKER_OK = $false
foreach ($m in $ACCEPTED_MARKERS) {
    if ($UserName.StartsWith($m, [System.StringComparison]::Ordinal)) {
        $MARKER_OK = $true
        break
    }
}
if (-not $MARKER_OK) {
    throw "SAFETY: userName must start with one of: $($ACCEPTED_MARKERS -join ', '). Got '$UserName'."
}

# G3_DEV_DOTNET_RESOLVER_CLEANUP_001: safe dotnet resolver (G3_DEV_DOTNET_RESOLVER_CLEANUP_FIX_001).
# - Default to system dotnet (PATH lookup).
# - Allow GULIERP_DOTNET to override: either a full path to dotnet.exe
#   OR a name resolvable via PATH (e.g., "dotnet").
# - Hard-prohibit the old vendored D:\guli\gulierp\.dotnet\*.
# - Validate the resolved dotnet is .NET 10 SDK.
$DOTNET = if ($env:GULIERP_DOTNET) {
    if (Test-Path -LiteralPath $env:GULIERP_DOTNET) {
        $env:GULIERP_DOTNET
    }
    else {
        try {
            (Get-Command -Name $env:GULIERP_DOTNET -ErrorAction Stop).Source
        }
        catch {
            throw "GULIERP_DOTNET is set to '$env:GULIERP_DOTNET' but it is neither a valid file path nor a command resolvable via PATH."
        }
    }
}
else {
    (Get-Command -Name dotnet -ErrorAction Stop).Source
}
if ($DOTNET -like 'D:\guli\gulierp\.dotnet\*') {
    throw "Old vendored dotnet is forbidden: $DOTNET (use system dotnet or set GULIERP_DOTNET)."
}
if (-not (Test-Path -LiteralPath $DOTNET)) {
    throw "diagnose-operator-user requires a .NET 10 SDK on PATH (or set GULIERP_DOTNET). Not found: $DOTNET."
}
$_dotnetVersion = (& $DOTNET --version).Trim()
if ($_dotnetVersion -notmatch '^10\.') {
    throw "GuliERP Next requires .NET 10 SDK. Current: $_dotnetVersion ($DOTNET)."
}

$BOOTSTRAP_PROJECT = Join-Path $PSScriptRoot '..\GuliERP.Identity.Bootstrap\GuliERP.Identity.Bootstrap.csproj'
$ASSERT_SCRIPT     = Join-Path $PSScriptRoot 'assert-gulierp-db-target.ps1'

# --- 1. DB target guard ---------------------------------------------------
if (Test-Path Env:ConnectionStrings__GuliERP) {
    & $ASSERT_SCRIPT
    if ($LASTEXITCODE -ne 0) {
        throw 'Wrong-DB detected in pre-check.'
    }
}

# --- 2. Resolve connection string ------------------------------------------
if (-not $ConnectionString) {
    $ConnectionString = $env:ConnectionStrings__GuliERP
}
if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    $ConnectionString = Read-Host -Prompt 'Npgsql connection string'
}
& $ASSERT_SCRIPT -ConnectionString $ConnectionString
if ($LASTEXITCODE -ne 0) {
    throw 'Wrong-DB detected by post-prompt assert.'
}

# --- 3. Read candidate password (optional) --------------------------------
$plainPwd = $null
if ($VerifyPassword) {
    $securePwd = Read-Host -Prompt "Candidate password for $UserName (not echoed)" -AsSecureString
    if ($null -eq $securePwd -or $securePwd.Length -lt 1) {
        throw 'Empty password. Aborting.'
    }
    $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePwd)
    try {
        $plainPwd = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
    }
    finally {
        [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($BSTR)
    }
    $securePwd.Dispose()
}

# --- 4. Invoke the .NET diagnose path --------------------------------------
# We REUSE the bootstrap tool binary as a host (it already has
# the Identity DI container wired). The tool now exposes a
# "--diagnose" mode that runs the 5+1 read-only checks and
# emits a JSON result. To avoid changing the bootstrap tool's
# exit contract for the production provisioning path, the
# diagnose mode is a separate flag handled before the
# provisioning logic runs.
$dotnetArgs = @(
    'run', '--project', $BOOTSTRAP_PROJECT,
    '-c', 'Release', '--no-restore',
    '--', '--diagnose', $ConnectionString, $UserName
)

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $DOTNET
foreach ($a in $dotnetArgs) { $psi.ArgumentList.Add($a) }
if ($VerifyPassword) {
    $psi.RedirectStandardInput = $true
}
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $true

$proc = New-Object System.Diagnostics.Process
$proc.StartInfo = $psi

$started = $proc.Start()
if (-not $started) {
    throw 'Failed to start diagnose process.'
}

$stdoutTask = $proc.StandardOutput.ReadToEndAsync()
$stderrTask = $proc.StandardError.ReadToEndAsync()

if ($VerifyPassword) {
    $proc.StandardInput.WriteLine($plainPwd)
    $proc.StandardInput.Flush()
    $proc.StandardInput.Close()
}

$timeoutMs = 30000
$exited = $proc.WaitForExit($timeoutMs)
if (-not $exited) {
    try { Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue } catch {}
    throw "DIAGNOSE_PROCESS_TIMEOUT ($timeoutMs ms). Killed PID $($proc.Id)."
}

$stdout = $stdoutTask.GetAwaiter().GetResult()
$stderr = $stderrTask.GetAwaiter().GetResult()

if ($proc.ExitCode -ne 0) {
    Write-Host '[DIAGNOSE] FAILED.' -ForegroundColor Red
    if ($stderr) { Write-Host $stderr -ForegroundColor Red }
    exit $proc.ExitCode
}

# Parse the JSON result.
$parsed = $null
try {
    $parsed = $stdout | ConvertFrom-Json -ErrorAction Stop
}
catch {
    $candidates = $stdout -split "(`r`n|`n|`r)"
    for ($i = $candidates.Count - 1; $i -ge 0; $i--) {
        $line = $candidates[$i].Trim()
        if (-not $line) { continue }
        if ($line[0] -ne '{') { continue }
        try {
            $cand = $line | ConvertFrom-Json -ErrorAction Stop
            if ($cand.diagnostic) {
                $parsed = $cand
                break
            }
        } catch {}
    }
}
if ($null -eq $parsed) {
    throw 'Diagnose OK exit but stdout is not parseable as JSON.'
}

# Print the human-readable summary (no secrets).
Write-Host '[DIAGNOSE] OK.' -ForegroundColor Green
Write-Host "  userName         = $($parsed.userName)"
Write-Host "  EXISTS           = $($parsed.exists)"
Write-Host "  ACTIVE           = $($parsed.active)"
Write-Host "  LOCKED           = $($parsed.locked)"
Write-Host "  LOCKOUT_END      = $($parsed.lockoutEnd)"
Write-Host "  TENANT_BINDING   = $($parsed.tenantBinding)"
Write-Host "  TENANT_CODE      = $($parsed.tenantCode)"
Write-Host "  COMPANY_BINDING  = $($parsed.companyBinding)"
Write-Host "  COMPANY_CODE     = $($parsed.companyCode)"
Write-Host "  PASSWORD_VERIFICATION = $($parsed.passwordVerification)"
Write-Host "  ACCESS_FAILED_COUNT  = $($parsed.accessFailedCount)"
Write-Host "  USER_ID          = $($parsed.userId)"
Write-Host "  passwordHash     = (not echoed)"
Write-Host "  securityStamp    = (not echoed)"
Write-Host ''

# Emit the canonical JSON for downstream scripting.
$finalJson = $parsed | ConvertTo-Json -Compress
Write-Output $finalJson

if ($plainPwd) { $plainPwd = $null }
exit 0
