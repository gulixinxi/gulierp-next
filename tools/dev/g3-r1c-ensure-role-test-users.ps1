#requires -Version 5.1
<#
G3-R1C dev-only operator helper to ensure the 4 dedicated
single-role test users for the 4-role permission matrix
runtime verification.

The 4 users:
  - g3r1c_sys_admin          → ERP_SYSTEM_ADMIN    (8 identity perms)
  - g3r1c_mdm_operator       → ERP_MDM_OPERATOR    (16 mdm perms)
  - g3r1c_employee_operator  → ERP_EMPLOYEE_OPERATOR (2 identity.employee perms)
  - g3r1c_sales_operator     → ERP_SALES_OPERATOR  (2 sales perms)

ENVIRONMENT VARIABLES (required):
  ConnectionStrings__GuliERP                       PostgreSQL connection string
  GULIERP_G3R1C_SYS_ADMIN_PASS                     >= 12 chars, mixed case + digit + non-alnum
  GULIERP_G3R1C_MDM_OPERATOR_PASS
  GULIERP_G3R1C_EMPLOYEE_OPERATOR_PASS
  GULIERP_G3R1C_SALES_OPERATOR_PASS

ENVIRONMENT VARIABLES (optional, default shown):
  GULIERP_G3R1C_TENANT_CODE                        GULI
  GULIERP_G3R1C_COMPANY_CODE                       GULI001

OUTPUT:
  Console log + JSON summary between ---JSON-BEGIN--- / ---JSON-END---
  Final exit code:
    0 = success
    1 = missing env var (run the helper to see which one)
    3 = tenant/company not found
    4 = Identity operation failed
    5 = unexpected exception

SAFETY:
  - This script is DEV-ONLY. It does NOT touch production users.
  - All 4 created users have the `g3r1c_` prefix so a future
    cleanup script can target them without touching prod users.
  - Passwords are NEVER echoed. They are read from env vars only.

USAGE:
  # Set env vars (do not commit these values):
  $env:ConnectionStrings__GuliERP = "Host=...;Database=...;Username=...;Password=...;Include Error Detail=true"
  $env:GULIERP_G3R1C_SYS_ADMIN_PASS         = "SysAdminP@ssw0rd2026!"
  $env:GULIERP_G3R1C_MDM_OPERATOR_PASS      = "MdmOper@torP@ss2026!"
  $env:GULIERP_G3R1C_EMPLOYEE_OPERATOR_PASS = "Employee0pP@ss2026!"
  $env:GULIERP_G3R1C_SALES_OPERATOR_PASS    = "Sales0pP@ss2026!"

  # Run:
  pwsh tools/dev/g3-r1c-ensure-role-test-users.ps1

  # Or, if the env vars are set in the current shell, the
  # script will pick them up automatically.
#>

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

function Write-Section { param([string]$Title)
    Write-Host ''
    Write-Host ('=' * 70) -ForegroundColor Cyan
    Write-Host "  $Title" -ForegroundColor Cyan
    Write-Host ('=' * 70) -ForegroundColor Cyan
}
function Write-OK      { param([string]$Message) Write-Host "  PASS  $Message" -ForegroundColor Green }
function Write-Block   { param([string]$Message) Write-Host "  BLOCK $Message" -ForegroundColor Yellow }
function Write-Err     { param([string]$Message) Write-Host "  FAIL  $Message" -ForegroundColor Red; $script:HardFailed++ }

$script:HardFailed = 0

# ---- 1. Validate env vars (do NOT echo values) ----
Write-Section 'G3-R1C ensure role test users — preflight'

$requiredEnv = @(
    'ConnectionStrings__GuliERP',
    'GULIERP_G3R1C_SYS_ADMIN_PASS',
    'GULIERP_G3R1C_MDM_OPERATOR_PASS',
    'GULIERP_G3R1C_EMPLOYEE_OPERATOR_PASS',
    'GULIERP_G3R1C_SALES_OPERATOR_PASS'
)

$missing = @()
foreach ($name in $requiredEnv) {
    $v = [System.Environment]::GetEnvironmentVariable($name)
    if ([string]::IsNullOrEmpty($v)) {
        $missing += $name
    }
}
if ($missing.Count -gt 0) {
    Write-Host ''
    Write-Host 'ERROR: missing required environment variables:' -ForegroundColor Red
    foreach ($m in $missing) {
        Write-Host "  - $m" -ForegroundColor Red
    }
    Write-Host ''
    Write-Host 'Set them in the current shell before running this script.' -ForegroundColor Yellow
    Write-Host 'Example (do not commit these values):' -ForegroundColor Yellow
    Write-Host '  $env:ConnectionStrings__GuliERP = "Host=...;Database=...;Username=...;Password=...;Include Error Detail=true"' -ForegroundColor Yellow
    Write-Host '  $env:GULIERP_G3R1C_SYS_ADMIN_PASS         = "..."' -ForegroundColor Yellow
    Write-Host '  $env:GULIERP_G3R1C_MDM_OPERATOR_PASS      = "..."' -ForegroundColor Yellow
    Write-Host '  $env:GULIERP_G3R1C_EMPLOYEE_OPERATOR_PASS = "..."' -ForegroundColor Yellow
    Write-Host '  $env:GULIERP_G3R1C_SALES_OPERATOR_PASS    = "..."' -ForegroundColor Yellow
    Write-Host ''
    Write-Host 'Passwords must satisfy the Identity policy:' -ForegroundColor Yellow
    Write-Host '  - minimum 12 characters' -ForegroundColor Yellow
    Write-Host '  - at least 1 digit, 1 uppercase, 1 lowercase' -ForegroundColor Yellow
    Write-Host '  - at least 1 non-alphanumeric character' -ForegroundColor Yellow
    Write-Host '  - at least 4 unique characters' -ForegroundColor Yellow
    exit 1
}

# Sanity-check password length (12+ chars). Identity will give a
# more specific error if other rules fail.
foreach ($p in @('GULIERP_G3R1C_SYS_ADMIN_PASS',
                'GULIERP_G3R1C_MDM_OPERATOR_PASS',
                'GULIERP_G3R1C_EMPLOYEE_OPERATOR_PASS',
                'GULIERP_G3R1C_SALES_OPERATOR_PASS')) {
    $v = [System.Environment]::GetEnvironmentVariable($p)
    if ($v.Length -lt 12) {
        Write-Host "ERROR: $p is shorter than 12 characters (got $($v.Length))." -ForegroundColor Red
        exit 1
    }
}

$connStr = [System.Environment]::GetEnvironmentVariable('ConnectionStrings__GuliERP')
# Mask the password in the connection string for logging
$masked = ($connStr -replace '(?i)(password\s*=\s*)([^;"]+)', '$1<REDACTED>')
Write-OK "ConnectionStrings__GuliERP = $masked"
Write-OK "GULIERP_G3R1C_*_PASS        = (4 vars set, not echoed)"

$tenantCode = [System.Environment]::GetEnvironmentVariable('GULIERP_G3R1C_TENANT_CODE')
if ([string]::IsNullOrEmpty($tenantCode)) {
    $tenantCode = 'GULI'
    $env:GULIERP_G3R1C_TENANT_CODE = $tenantCode
}
$companyCode = [System.Environment]::GetEnvironmentVariable('GULIERP_G3R1C_COMPANY_CODE')
if ([string]::IsNullOrEmpty($companyCode)) {
    $companyCode = 'GULI001'
    $env:GULIERP_G3R1C_COMPANY_CODE = $companyCode
}
Write-OK "Tenant/Company: $tenantCode / $companyCode"

# ---- 2. Invoke the provisioner ----
Write-Section 'Running provisioner (tools/GuliERP.G3R1C.IdentityProvisioner)'

$repoRoot = (Resolve-Path "$PSScriptRoot/../..").Path
$proj = Join-Path $repoRoot 'tools/GuliERP.G3R1C.IdentityProvisioner/GuliERP.G3R1C.IdentityProvisioner.csproj'
if (-not (Test-Path $proj)) {
    Write-Host "ERROR: project file not found: $proj" -ForegroundColor Red
    exit 5
}

$stdoutFile = [System.IO.Path]::GetTempFileName()
$stderrFile = [System.IO.Path]::GetTempFileName()
try {
    # Note: we deliberately do NOT pass the connection string on
    # the command line (would be visible in `ps`). The provisioner
    # reads from env (ConnectionStrings__GuliERP).
    & dotnet run --project "$proj" --no-build --configuration Debug 2>&1 | Tee-Object -FilePath $stdoutFile | Out-Host
    $exitCode = $LASTEXITCODE
} catch {
    Write-Host "ERROR: dotnet run failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 5
}

# ---- 3. Parse JSON summary ----
Write-Section 'Provisioner summary'
$stdout = Get-Content $stdoutFile -Raw
$jsonStart = $stdout.IndexOf('---JSON-BEGIN---')
$jsonEnd   = $stdout.IndexOf('---JSON-END---')
if ($jsonStart -lt 0 -or $jsonEnd -lt 0) {
    Write-Host "ERROR: provisioner did not emit a JSON summary." -ForegroundColor Red
    Write-Host "  exit code = $exitCode" -ForegroundColor Red
    exit 5
}
$json = $stdout.Substring($jsonStart + '---JSON-BEGIN---'.Length, $jsonEnd - $jsonStart - '---JSON-BEGIN---'.Length).Trim()
try {
    $summary = $json | ConvertFrom-Json
} catch {
    Write-Host "ERROR: failed to parse provisioner JSON:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host '--- raw JSON ---' -ForegroundColor Yellow
    Write-Host $json -ForegroundColor Yellow
    exit 5
}

# Print the summary.
Write-Host ("  TenantId   = {0}" -f $summary.tenantId) -ForegroundColor Cyan
Write-Host ("  CompanyId  = {0}" -f $summary.companyId) -ForegroundColor Cyan
Write-Host ("  AllSucceeded = {0}" -f $summary.allSucceeded) -ForegroundColor $(if ($summary.allSucceeded) {'Green'} else {'Red'})
Write-Host ''
Write-Host '  Per-user:' -ForegroundColor Cyan
foreach ($u in $summary.users) {
    $status = if ($u.success) {'PASS'} else {'FAIL'}
    $color  = if ($u.success) {'Green'} else {'Red'}
    Write-Host ("    [{0}] {1,-22} role={2,-22} expectedPerms={3,2}" -f $status, $u.userName, $u.roleCode, $u.expectedPermissionCount) -ForegroundColor $color
    if ($u.userCreated)   { Write-Host "         - user created" -ForegroundColor DarkGray }
    if ($u.userExisted)   { Write-Host "         - user existed" -ForegroundColor DarkGray }
    if ($u.passwordReset) { Write-Host "         - password reset" -ForegroundColor DarkGray }
    if ($u.roleCreated)   { Write-Host "         - role created" -ForegroundColor DarkGray }
    if ($u.roleExisted)   { Write-Host "         - role existed" -ForegroundColor DarkGray }
    if ($u.roleClaimsAdded -gt 0) {
        Write-Host "         - role claims added: $($u.roleClaimsAdded)" -ForegroundColor DarkGray
    }
    if ($u.assignmentCreated)         { Write-Host "         - role assignment created" -ForegroundColor DarkGray }
    if ($u.assignmentAlreadyExisted)  { Write-Host "         - role assignment already existed" -ForegroundColor DarkGray }
    if ($u.extraRoleAssignmentsRemoved -and $u.extraRoleAssignmentsRemoved.Count -gt 0) {
        Write-Host "         - extra role assignments REVOKED (ids: $($u.extraRoleAssignmentsRemoved -join ','))" -ForegroundColor Yellow
    }
    if ($u.extraClaimsKept -and $u.extraClaimsKept.Count -gt 0) {
        Write-Host "         - extra claims KEPT (out-of-source): $($u.extraClaimsKept -join ',')" -ForegroundColor Yellow
    }
    if (-not $u.success -and $u.error) {
        Write-Host "         - ERROR: $($u.error)" -ForegroundColor Red
    }
}

# ---- 4. Final ----
Write-Section 'Final'
if ($summary.allSucceeded -and $exitCode -eq 0) {
    Write-OK '4 dedicated single-role test users are ready.'
    Write-Host ''
    Write-Host 'NEXT STEP: run the permission matrix evidence script:' -ForegroundColor Cyan
    Write-Host '  pwsh tools/dev/g3-r1c-permission-matrix-evidence.ps1' -ForegroundColor Cyan
    Write-Host ''
    Write-Host 'Set the SAME 4 passwords as env vars (plus GULIERP_TEST_BASE_URL if not default).' -ForegroundColor Cyan
    Write-Host 'The env var names the matrix script expects:' -ForegroundColor Cyan
    Write-Host '  GULIERP_G3R1C_SYS_ADMIN_USER         (= g3r1c_sys_admin)' -ForegroundColor Cyan
    Write-Host '  GULIERP_G3R1C_SYS_ADMIN_PASS' -ForegroundColor Cyan
    Write-Host '  GULIERP_G3R1C_MDM_OPERATOR_USER      (= g3r1c_mdm_operator)' -ForegroundColor Cyan
    Write-Host '  GULIERP_G3R1C_MDM_OPERATOR_PASS' -ForegroundColor Cyan
    Write-Host '  GULIERP_G3R1C_EMPLOYEE_OPERATOR_USER (= g3r1c_employee_operator)' -ForegroundColor Cyan
    Write-Host '  GULIERP_G3R1C_EMPLOYEE_OPERATOR_PASS' -ForegroundColor Cyan
    Write-Host '  GULIERP_G3R1C_SALES_OPERATOR_USER    (= g3r1c_sales_operator)' -ForegroundColor Cyan
    Write-Host '  GULIERP_G3R1C_SALES_OPERATOR_PASS' -ForegroundColor Cyan
    exit 0
} else {
    Write-Err "Provisioner reported failure (allSucceeded=$($summary.allSucceeded), exitCode=$exitCode)"
    exit 4
}
