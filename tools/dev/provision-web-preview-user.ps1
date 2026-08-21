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

.PARAMETER Reset
    If set, performs a CONTROLLED HARD RESET of only the
    web_preview_admin fixture before re-provisioning:
      - Deletes the AspNet Identity record for
        web_preview_admin via UserManager.DeleteAsync
        (never raw SQL).
      - Before that, deletes the custom FK rows only for
        that user: UserRoleAssignment,
        UserOrganizationMembership, UserCompanyMembership
        (all by explicit UserId equality, so no other user
        rows are ever touched).
      - Transactionally commits the delete, then runs the
        standard idempotent provision on top:
          * Reuses existing web_preview_t / web_preview_c
            (NEVER deletes a tenant or company).
          * Recreates user with a FRESH HILO Id (not
            MAX(Id)+1, not hardcoded) via
            UserManager.CreateAsync.
          * Binds default company membership.
          * Grants the 4 system roles via
            gulierp_user_role_assignment.
          * Password is read via Read-Host -AsSecureString
            and hashed by Identity PasswordHasher.
    Without -Reset, the script is purely idempotent:
      - If user exists, only rotates its password and
        ensures missing role assignments / memberships /
        status fields (non-destructive).
      - If user is missing, creates it.

    Safety guard: user, tenant, company must all carry the
    "web_preview_" marker prefix. The bootstrap tool
    refuses to touch anything else.

.EXAMPLE
    PS> .\provision-web-preview-user.ps1
    (interactive: paste connection string + password)

.EXAMPLE
    PS> .\provision-web-preview-user.ps1 -Reset
    (read-only pre-diagnose, then hard-reset the preview
    fixture, then re-provision. Password is prompted anew.)

.EXAMPLE
    PS> $env:ConnectionStrings__GuliERP = "Host=192.168.2.228;...;Password=***"
    PS> .\provision-web-preview-user.ps1 -SkipPrompt
    (reads password via Read-Host -AsSecureString)

.EXAMPLE
    PS> .\provision-web-preview-user.ps1 -GrantMdmOperator
    Adds the 12 MDM permission claims (6 read + 6 manage) to the
    ERP_MDM_OPERATOR role in the user's Tenant and grants that
    role to the user. Idempotent. Use this AFTER the user can log
    in but gets 403 on the 6 master-data SPA pages. No password
    is needed (the operation is non-destructive to the password).
#>
[CmdletBinding()]
param(
    [switch]$SkipPrompt,
    [switch]$Reset,
    [switch]$GrantMdmOperator
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

# --- 2b. (Reset mode only) Pre-reset READ-ONLY diagnose -------------------
# Captures the 5+1 evidence BEFORE the delete phase so the
# operator has an audit trail of the pre-reset state. No
# password is read or echoed.
if ($Reset) {
    Write-Host '[WEB-PREVIEW-IDENTITY-HARD-RESET] Step 2b: Pre-reset READ-ONLY diagnose' -ForegroundColor Magenta
    try {
        $preDiagArgs = @(
            'run', '--project', $BOOTSTRAP_PROJECT,
            '-c', 'Release', '--no-restore',
            '--',
            '--diagnose', $ConnectionString, $USERNAME
        )
        $pdiagPsi = New-Object System.Diagnostics.ProcessStartInfo
        $pdiagPsi.FileName = $DOTNET
        foreach ($a in $preDiagArgs) { $pdiagPsi.ArgumentList.Add($a) }
        $pdiagPsi.RedirectStandardOutput = $true
        $pdiagPsi.RedirectStandardError  = $true
        $pdiagPsi.UseShellExecute         = $false
        $pdiagPsi.CreateNoWindow          = $true

        $pdiagProc = New-Object System.Diagnostics.Process
        $pdiagProc.StartInfo = $pdiagPsi
        $pstarted = $pdiagProc.Start()
        if (-not $pstarted) { throw 'Failed to start pre-diagnose process.' }
        $pstdoutTask = $pdiagProc.StandardOutput.ReadToEndAsync()
        $pstderrTask = $pdiagProc.StandardError.ReadToEndAsync()
        $pExited = $pdiagProc.WaitForExit(60000)
        if (-not $pExited) {
            try { Stop-Process -Id $pdiagProc.Id -Force -ErrorAction SilentlyContinue } catch {}
            throw 'Pre-diagnose timeout.'
        }
        $pstderr = $pstderrTask.GetAwaiter().GetResult()
        if ($pstderr) { Write-Host $pstderr -ForegroundColor DarkGray }
        if ($pdiagProc.ExitCode -ne 0) {
            Write-Warning "Pre-diagnose returned exit code $($pdiagProc.ExitCode). Continuing reset (user may not exist yet)."
        } else {
            $pstdout = $pstdoutTask.GetAwaiter().GetResult()
            try {
                $diag = $pstdout | ConvertFrom-Json -ErrorAction Stop
                Write-Host "PRE_RESET_DIAGNOSE:`n$($diag | ConvertTo-Json -Depth 5)" -ForegroundColor Yellow
            } catch {
                Write-Host "PRE_RESET_DIAGNOSE(raw): $pstdout" -ForegroundColor Yellow
            }
        }
    } catch {
        Write-Warning "Pre-reset diagnose skipped due to error: $_"
    }
}

# --- 2c. (-GrantMdmOperator mode only) Grant the 12 MDM permissions -------
# This is a NON-DESTRUCTIVE operation: it does not touch the
# password, does not delete any data, and is idempotent. It is
# meant to be run AFTER the user can log in but receives 403 on
# the 6 master-data SPA pages. The user can be either
# `web_preview_admin` (default) or `test_operator_g2_004` (G2-004
# operator) — the marker guard inside the .NET tool accepts both.
if ($GrantMdmOperator) {
    Write-Host '[WEB-PREVIEW-002] Step 2c: Grant MDM Operator permissions' -ForegroundColor Cyan
    $grantArgs = @(
        'run', '--project', $BOOTSTRAP_PROJECT,
        '-c', 'Release', '--no-restore',
        '--', '--grant-mdm-operator', $ConnectionString, $USERNAME
    )
    $gPsi = New-Object System.Diagnostics.ProcessStartInfo
    $gPsi.FileName = $DOTNET
    foreach ($a in $grantArgs) { $gPsi.ArgumentList.Add($a) }
    $gPsi.RedirectStandardOutput = $true
    $gPsi.RedirectStandardError  = $true
    $gPsi.UseShellExecute         = $false
    $gPsi.CreateNoWindow          = $true

    $gProc = New-Object System.Diagnostics.Process
    $gProc.StartInfo = $gPsi
    $gStarted = $gProc.Start()
    if (-not $gStarted) { throw 'Failed to start grant-mdm-operator process.' }
    $gStdoutTask = $gProc.StandardOutput.ReadToEndAsync()
    $gStderrTask = $gProc.StandardError.ReadToEndAsync()
    $gExited = $gProc.WaitForExit(60000)
    if (-not $gExited) {
        try { Stop-Process -Id $gProc.Id -Force -ErrorAction SilentlyContinue } catch {}
        throw "GRANT_MDM_PROCESS_TIMEOUT (60 s). Killed PID $($gProc.Id)."
    }
    $gStderr = $gStderrTask.GetAwaiter().GetResult()
    if ($gStderr) { Write-Host $gStderr -ForegroundColor DarkGray }
    if ($gProc.ExitCode -ne 0) {
        Write-Host "[WEB-PREVIEW-002] Grant FAILED with exit code $($gProc.ExitCode)." -ForegroundColor Red
        if ($gStderr) { Write-Host $gStderr -ForegroundColor Red }
        exit $gProc.ExitCode
    }
    $gStdout = $gStdoutTask.GetAwaiter().GetResult()
    $gParsed = $gStdout | ConvertFrom-Json -ErrorAction SilentlyContinue
    if ($null -eq $gParsed -or $gParsed.ok -ne $true) {
        throw "Grant OK exit but stdout is not parseable as JSON. Output: $gStdout"
    }
    Write-Host '[WEB-PREVIEW-002] Grant OK.' -ForegroundColor Green
    Write-Host "  userName       = $($gParsed.userName)"
    Write-Host "  userId         = $($gParsed.userId)"
    Write-Host "  tenantCode     = $($gParsed.tenantCode)"
    Write-Host "  tenantId       = $($gParsed.tenantId)"
    Write-Host "  roleCode       = $($gParsed.roleCode)"
    Write-Host "  roleId         = $($gParsed.roleId)"
    Write-Host "  totalClaims    = $($gParsed.totalClaims)"
    Write-Host "  grantedClaims  = $(@($gParsed.grantedClaims) -join ',')"
    Write-Host ''
    Write-Host 'Next: log out, log back in (Cookie / Claims / permissions refresh), then open the 6 master-data pages.'
    Write-Host 'No password was read or echoed.'
    $grantFinalJson = $gParsed | ConvertTo-Json -Compress
    Write-Output $grantFinalJson
    exit 0
}

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

    # Build the bootstrap tool's argument list.
    #   - Without -Reset: the idempotent 6-arg form:
    #       <conn> <user> <tenant> <company> <marker> <roles>
    #   - With    -Reset: first sub-command is --reset-fixture,
    #       followed by the same 6 positional args (user etc.).
    #       --reset-fixture also re-asserts marker guards inside
    #       the .NET tool (defense in depth).
    $positional = @(
        $ConnectionString, $USERNAME, $TENANT_CODE, $COMPANY_CODE,
        $MARKER_PREFIX, $SYSTEM_ROLES_CSV
    )
    $subCmd = if ($Reset) { @('--reset-fixture') } else { @() }
    $dotnetArgs = @(
        'run', '--project', $BOOTSTRAP_PROJECT,
        '-c', 'Release', '--no-restore',
        '--'
    ) + $subCmd + $positional

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
