#requires -Version 5.1
<#
.SYNOPSIS
    G2-004V1 — Secure bootstrap of the operator-evidence test user.

.DESCRIPTION
    The IdentitySeed.SeedAsync is dev-only and is NEVER called in
    Production. The operator-evidence script (g2-004-operator-evidence.ps1)
    needs a real, password-hashed, Tenant-membership-having User
    on the real Production-shape test database. This script is
    the secure bootstrap path.

    The script:
      1. Reads the Npgsql connection string (env var OR prompt).
         The password is NEVER echoed; it is masked in any log.
      2. Reads the operator username (must carry the test_operator_
         marker prefix; the tool refuses anything else).
      3. Reads the password via Read-Host -AsSecureString. The
         SecureString is converted to a plain string only at the
         point of piping to the .NET tool's STDIN, then wiped
         from memory as soon as the .NET tool exits.
      4. Invokes the .NET bootstrap tool (tools/GuliERP.Identity.Bootstrap)
         which:
            - uses ASP.NET Core Identity UserManager.CreateAsync
              (PBKDF2 password hashing; no custom hash)
            - ensures Tenant + Company + UserCompanyMembership
            - is idempotent (re-running resets the password for
              an existing marker-prefixed user)
            - refuses to touch any user / tenant / company
              WITHOUT the marker prefix (defense)
      5. Returns a JSON object on stdout. The password is NEVER
         echoed; the hash lives only in the database.

    The script is intended to be run ONCE per database (or after
    the operator's password rotation policy).

.PARAMETER ConnectionString
    Optional. If not provided, the script reads
    $env:ConnectionStrings__GuliERP or
    $env:GULIERP_FOUNDATION_CONNECTION.

.PARAMETER UserName
    Optional. Defaults to 'test_operator_g2_004'. MUST start with
    'test_operator_' (defense — see brief).

.PARAMETER TenantCode
    Optional. Defaults to 'test_operator_g2_004_t'. MUST start with
    'test_operator_'.

.PARAMETER CompanyCode
    Optional. Defaults to 'test_operator_g2_004_c'. MUST start with
    'test_operator_'.

.EXAMPLE
    PS> .\g2-004-bootstrap-operator-user.ps1
    (interactive: paste connection string + password)

.EXAMPLE
    PS> $env:ConnectionStrings__GuliERP = "Host=192.168.2.228;...;Password=***"
    PS> .\g2-004-bootstrap-operator-user.ps1 -SkipPrompt
    (reads password via Read-Host -AsSecureString)
#>
[CmdletBinding()]
param(
    [string]$ConnectionString,
    [string]$UserName = 'test_operator_g2_004',
    [string]$TenantCode = 'test_operator_g2_004_t',
    [string]$CompanyCode = 'test_operator_g2_004_c',
    [switch]$SkipPrompt
)

$ErrorActionPreference = 'Stop'
Set-Location -Path (Join-Path $PSScriptRoot '..\..')

$DOTNET = 'D:\guli\gulierp\.dotnet\dotnet.exe'
if (-not (Test-Path $DOTNET)) {
    throw "G2-004V1 requires D:\guli\gulierp\.dotnet\dotnet.exe. Not found."
}

$BOOTSTRAP_PROJECT = Join-Path $PSScriptRoot '..\GuliERP.Identity.Bootstrap\GuliERP.Identity.Bootstrap.csproj'

# --- 0. Resolve connection string ----------------------------------------
if (-not $ConnectionString) {
    $ConnectionString = $env:ConnectionStrings__GuliERP
}
if (-not $ConnectionString) {
    $ConnectionString = $env:GULIERP_FOUNDATION_CONNECTION
}
if (-not $ConnectionString) {
    if ($SkipPrompt) {
        throw 'No connection string. Set $env:ConnectionStrings__GuliERP or pass -ConnectionString.'
    }
    $ConnectionString = Read-Host -Prompt 'Npgsql connection string (Host=...;Port=...;Database=...;Username=...;Password=...)'
}

$displayConn = ($ConnectionString -replace 'Password=[^;]+', 'Password=***')
Write-Host "[G2-004V1] Using connection: $displayConn" -ForegroundColor Cyan

# --- 1. Read username / tenant / company (with marker guards) ------------
# (The .NET tool also enforces the marker; the PS guard is for
# fast-feedback before the build.)
foreach ($pair in @(@('UserName', $UserName), @('TenantCode', $TenantCode), @('CompanyCode', $CompanyCode))) {
    $name = $pair[0]; $val = $pair[1]
    if (-not $val.StartsWith('test_operator_', [System.StringComparison]::Ordinal)) {
        throw "SAFETY: $name='$val' must start with 'test_operator_'."
    }
}

# --- 2. Read password (SecureString) -------------------------------------
$securePwd = $null
try {
    if ($SkipPrompt) {
        $securePwd = Read-Host -Prompt 'Operator test user password' -AsSecureString
    } else {
        $securePwd = Read-Host -Prompt 'Operator test user password' -AsSecureString
    }
}
catch {
    throw "Failed to read password: $($_.Exception.Message)"
}
if ($null -eq $securePwd -or $securePwd.Length -lt 1) {
    throw "Empty password. Aborting."
}

# Convert SecureString to plain string ONLY at the point of
# piping to the .NET tool's STDIN. The plain string lives in
# a local variable; we wipe it as soon as the .NET tool exits.
$plainPwd = $null
try {
    $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePwd)
    try {
        $plainPwd = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
    }
    finally {
        [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($BSTR)
    }

    # Build the .NET tool args.
    $args = @(
        'run', '--project', $BOOTSTRAP_PROJECT,
        '-c', 'Release', '--no-restore',
        '--', $ConnectionString, $UserName, $TenantCode, $CompanyCode
    )

    # Invoke the .NET tool with the password on STDIN.
    # ProcessStartInfo.RedirectStandardInput = true. The .NET
    # tool reads password from Console.In.
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $DOTNET
    foreach ($a in $args) { $psi.ArgumentList.Add($a) }
    $psi.RedirectStandardInput = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true

    $proc = New-Object System.Diagnostics.Process
    $proc.StartInfo = $psi
    $null = $proc.Start()

    # Write the password + newline to STDIN, then close.
    $proc.StandardInput.WriteLine($plainPwd)
    $proc.StandardInput.Close()

    # Read stdout / stderr (do not echo).
    $stdout = $proc.StandardOutput.ReadToEnd()
    $stderr = $proc.StandardError.ReadToEnd()
    $proc.WaitForExit()

    if ($proc.ExitCode -ne 0) {
        Write-Host '[G2-004V1] Bootstrap FAILED.' -ForegroundColor Red
        if ($stderr) { Write-Host $stderr -ForegroundColor Red }
        exit $proc.ExitCode
    }

    # Parse the JSON and emit a friendly summary (no password).
    $parsed = $stdout | ConvertFrom-Json
    Write-Host '[G2-004V1] Bootstrap OK.' -ForegroundColor Green
    Write-Host "  userName       = $($parsed.userName)"
    Write-Host "  userId         = $($parsed.userId)"
    Write-Host "  tenantCode     = $($parsed.tenantCode)"
    Write-Host "  tenantId       = $($parsed.tenantId)"
    Write-Host "  companyCode    = $($parsed.companyCode)"
    Write-Host "  companyId      = $($parsed.companyId)"
    Write-Host "  markerPrefix   = $($parsed.markerPrefix)"
    Write-Host "  password       = (hashed by Identity PBKDF2; not echoed)"
    Write-Host ''
    Write-Host 'Next: run the operator evidence pack:'
    Write-Host '  PS> .\tools\dev\g2-004-operator-evidence.ps1 -SkipPrompt'

    # Emit the JSON for downstream scripting (e.g.
    # g2-004-operator-evidence.ps1 can read this).
    Write-Output $stdout
    exit 0
}
finally {
    # Wipe the plain password as soon as the tool exits.
    if ($plainPwd) {
        $plainPwd = $null
    }
    if ($securePwd) {
        $securePwd.Dispose()
    }
}
