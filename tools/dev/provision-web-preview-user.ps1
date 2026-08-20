#requires -Version 5.1
<#
.SYNOPSIS
    WEB-PREVIEW-001A — Secure bootstrap of the dedicated web-preview user.

.DESCRIPTION
    Mirrors the G2-004 bootstrap pattern but for a different
    marker prefix. Creates / resets a single dedicated user
    bound to an existing development tenant + company and
    grants the user the 4 system roles (PLATFORM_ADMIN,
    TENANT_ADMIN, COMPANY_ADMIN, NORMAL_USER) so the
    browser preview can hit the G2-005-protected MDM endpoints
    + the G2-004R1 CSRF flow without manually editing
    UserRoleAssignment rows.

    The script:
      1. Asserts the DB target is gulierp_g2_003_test (fails
         closed if not).
      2. Resolves the Npgsql connection string (env var OR
         interactive prompt). Password is NEVER echoed.
      3. Invokes the GuliERP.Identity.Bootstrap tool (extended
         in MDM-001R1) with:
            - marker prefix = "web_preview_"
            - userName      = "web_preview_admin"
            - tenantCode    = "web_preview_t" (new marker-tagged
                              tenant, owned by the bootstrap
                              tool — NOT a fake default)
            - companyCode   = "web_preview_c"
            - 6th arg       = "PLATFORM_ADMIN,TENANT_ADMIN,
                              COMPANY_ADMIN,NORMAL_USER"
            - password      = read from STDIN via the .NET
                              tool's ReadToEndAsync (not echoed)
      4. Output: a JSON object on stdout with the resolved
         userId, tenantId, companyId, grantedRoles.
         The password is NEVER echoed.

    The script is intended to be run ONCE per database
    (or after the operator's password rotation policy).
    Idempotent: re-runs reset the password (via Identity's
    RemovePasswordAsync + AddPasswordAsync) and re-grant any
    missing system roles.

    SECURITY:
      - The bootstrap tool's marker guard refuses to touch
        any user / tenant / company WITHOUT the "web_preview_"
        prefix. The PowerShell script mirrors this guard.
      - The password is read via Read-Host -AsSecureString
        and converted to a plain string ONLY at the point of
        piping to the .NET tool's STDIN; it is wiped as soon
        as the .NET tool exits.
      - The script NEVER writes the password to logs, files,
        git, or environment variables.

.PARAMETER SkipPrompt
    If set, the connection string is read from
    $env:ConnectionStrings__GuliERP without interactive
    prompt. The password is still prompted via
    Read-Host -AsSecureString.

.EXAMPLE
    PS> .\provision-web-preview-user.ps1
    (interactive: paste connection string + password)

.EXAMPLE
    PS> $env:ConnectionStrings__GuliERP = "Host=192.168.2.228;...;Password=***"
    PS> .\provision-web-preview-user.ps1 -SkipPrompt
    (reads password via Read-Host -AsSecureString)
#>
[CmdletBinding()]
param(
    [switch]$SkipPrompt
)

$ErrorActionPreference = 'Stop'
Set-Location -Path (Join-Path $PSScriptRoot '..\..')

# --- 0. Marker + role contract (lock to the WEB-PREVIEW-001A spec) --------
$MARKER_PREFIX     = 'web_preview_'
$USERNAME          = 'web_preview_admin'
$TENANT_CODE       = 'web_preview_t'
$COMPANY_CODE      = 'web_preview_c'
# All 4 system roles so the preview user can hit every
# G2-005-protected endpoint. Order is not significant.
$SYSTEM_ROLES_CSV  = 'PLATFORM_ADMIN,TENANT_ADMIN,COMPANY_ADMIN,NORMAL_USER'

$DOTNET            = 'D:\guli\gulierp\.dotnet\dotnet.exe'
if (-not (Test-Path $DOTNET)) {
    throw "WEB-PREVIEW-001A requires D:\guli\gulierp\.dotnet\dotnet.exe. Not found."
}

$BOOTSTRAP_PROJECT = Join-Path $PSScriptRoot '..\GuliERP.Identity.Bootstrap\GuliERP.Identity.Bootstrap.csproj'

$ASSERT_SCRIPT     = Join-Path $PSScriptRoot 'assert-gulierp-db-target.ps1'
if (-not (Test-Path $ASSERT_SCRIPT)) {
    throw "assert-gulierp-db-target.ps1 missing at $ASSERT_SCRIPT"
}

# --- 1. DB target guard (fail-closed) -------------------------------------
Write-Host '[WEB-PREVIEW-001A] Step 1: DB target guard' -ForegroundColor Cyan
# Pre-check: if the env var is set, assert it now. The
# post-prompt step re-asserts with the resolved connection
# string.
if (Test-Path Env:ConnectionStrings__GuliERP) {
    & $ASSERT_SCRIPT
    if ($LASTEXITCODE -ne 0) {
        throw 'Wrong-DB detected in pre-check. Re-set $env:ConnectionStrings__GuliERP.'
    }
}

# --- 2. Resolve connection string ------------------------------------------
if (-not $SkipPrompt -and -not (Test-Path Env:ConnectionStrings__GuliERP)) {
    $ConnectionString = Read-Host -Prompt 'Npgsql connection string (Host=...;Port=...;Database=...;Username=...;Password=...)'
} else {
    $ConnectionString = $env:ConnectionStrings__GuliERP
}
if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    throw 'No connection string. Set $env:ConnectionStrings__GuliERP or pass it at the prompt.'
}

# Re-assert with the resolved connection string (defense in
# depth). The assert never prints the password.
& $ASSERT_SCRIPT -ConnectionString $ConnectionString
if ($LASTEXITCODE -ne 0) {
    throw 'Wrong-DB detected by post-prompt assert.'
}
$displayConn = ($ConnectionString -replace 'Password=[^;]+', 'Password=***')
Write-Host "[WEB-PREVIEW-001A] Using connection: $displayConn" -ForegroundColor Cyan

# --- 3. Read password (SecureString) ---------------------------------------
$securePwd = $null
try {
    $securePwd = Read-Host -Prompt "Password for $USERNAME (interactive, will be hashed by Identity PBKDF2)" -AsSecureString
}
catch {
    throw "Failed to read password: $($_.Exception.Message)"
}
if ($null -eq $securePwd -or $securePwd.Length -lt 1) {
    throw 'Empty password. Aborting.'
}

# --- 4. Invoke the .NET bootstrap tool with the marker override -----------
$plainPwd = $null
try {
    $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePwd)
    try {
        $plainPwd = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
    }
    finally {
        [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($BSTR)
    }

    # 6-arg CLI: <conn> <userName> <tenantCode> <companyCode>
    #            <markerPrefix> <systemRolesCsv>
    $dotnetArgs = @(
        'run', '--project', $BOOTSTRAP_PROJECT,
        '-c', 'Release', '--no-restore',
        '--',
        $ConnectionString, $USERNAME, $TENANT_CODE, $COMPANY_CODE,
        $MARKER_PREFIX, $SYSTEM_ROLES_CSV
    )

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $DOTNET
    foreach ($a in $dotnetArgs) { $psi.ArgumentList.Add($a) }
    $psi.RedirectStandardInput = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true

    $proc = New-Object System.Diagnostics.Process
    $proc.StartInfo = $psi

    # Drain BOTH pipes concurrently (process IO deadlock
    # prevention — same pattern as g2-004-bootstrap-operator-user.ps1).
    $started = $proc.Start()
    if (-not $started) {
        throw 'Failed to start bootstrap process.'
    }
    $stdoutTask = $proc.StandardOutput.ReadToEndAsync()
    $stderrTask = $proc.StandardError.ReadToEndAsync()

    $proc.StandardInput.WriteLine($plainPwd)
    $proc.StandardInput.Flush()
    $proc.StandardInput.Close()

    $bootstrapTimeoutMs = 60000
    $exited = $proc.WaitForExit($bootstrapTimeoutMs)
    if (-not $exited) {
        try { Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue } catch {}
        throw "BOOTSTRAP_PROCESS_TIMEOUT ($bootstrapTimeoutMs ms). Killed bootstrap PID $($proc.Id)."
    }

    $stdout = $stdoutTask.GetAwaiter().GetResult()
    $stderr = $stderrTask.GetAwaiter().GetResult()

    if ($proc.ExitCode -ne 0) {
        Write-Host '[WEB-PREVIEW-001A] Bootstrap FAILED.' -ForegroundColor Red
        if ($stderr) { Write-Host $stderr -ForegroundColor Red }
        exit $proc.ExitCode
    }

    # Parse the final JSON. Defensive fallback for any
    # mixed-content stdout.
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
                if ($cand.ok -eq $true -and $cand.userName -and $cand.userId) {
                    $parsed = $cand
                    break
                }
            } catch {}
        }
    }
    if ($null -eq $parsed) {
        throw 'Bootstrap OK exit but stdout is not parseable as JSON.'
    }
    if ($parsed.ok -ne $true) {
        throw "Bootstrap OK exit but JSON ok != true. Output: $($parsed | Out-String)"
    }

    Write-Host '[WEB-PREVIEW-001A] Bootstrap OK.' -ForegroundColor Green
    Write-Host "  userName       = $($parsed.userName)"
    Write-Host "  userId         = $($parsed.userId)"
    Write-Host "  tenantCode     = $($parsed.tenantCode)"
    Write-Host "  tenantId       = $($parsed.tenantId)"
    Write-Host "  companyCode    = $($parsed.companyCode)"
    Write-Host "  companyId      = $($parsed.companyId)"
    Write-Host "  markerPrefix   = $($parsed.markerPrefix)"
    Write-Host "  grantedRoles   = $($parsed.grantedRoles -join ',')"
    Write-Host "  password       = (hashed by Identity PBKDF2; not echoed)"
    Write-Host ''
    Write-Host 'Next: verify the user via the diagnostic script:'
    Write-Host '  PS> .\tools\dev\diagnose-operator-user.ps1 -UserName web_preview_admin'
    Write-Host 'Then log in via the browser preview:'
    Write-Host '  POST /api/v1/auth/login  { userName: "web_preview_admin", password: <REDACTED> }'

    # Emit the canonical final JSON line for downstream
    # scripting. The password is NOT in the JSON.
    $finalJson = $parsed | ConvertTo-Json -Compress
    Write-Output $finalJson
    exit 0
}
finally {
    # Wipe the plain password as soon as the tool exits.
    if ($plainPwd) { $plainPwd = $null }
    if ($securePwd) { $securePwd.Dispose() }
}
