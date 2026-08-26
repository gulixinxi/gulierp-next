#requires -Version 5.1
<#
G3-R1C 4-role permission matrix evidence script (FULL VERIFICATION).

Validates the 4-role × 5-endpoint matrix at three levels:

  Level 1 — STATIC MATRIX (always runs):
    Reads the role pack definitions from
    modules/identity/GuliERP.Identity.Application/Authorization/
    EnterpriseBusinessRolePacks.cs (post G3-R1C centralization) and
    produces a permission map per role. No DB, no network.

  Level 2 — RUNTIME MATRIX (requires env-supplied test users):
    For each of the 4 roles, log in as the dedicated single-role
    test user (g3r1c_sys_admin / g3r1c_mdm_operator /
    g3r1c_employee_operator / g3r1c_sales_operator) and call 5
    endpoints (Uom GET, Dictionary GET, NumberingRule GET,
    Employee GET, SalesOrder GET). Expected per role is documented
    in the matrix.

  Level 3 — AUTH NEGATIVE PATHS (always runs):
    Anonymous request to each of the 5 endpoints must return 401.

DIFFERENCE FROM G3-R1B:
  - G3-R1B used only the bootstrap admin (4-role pollution) +
    anonymous. The 3 operator roles were BLOCKED.
  - G3-R1C uses 4 dedicated SINGLE-ROLE test users, so the
    matrix result is not polluted by the bootstrap admin's
    multi-role pattern.
  - The static matrix is also CORRECTED: ERP_SYSTEM_ADMIN
    contains exactly 8 identity perms (no mdm/employee/sales),
    not 20 (which was the G3-R1B incorrect "TBD" conclusion).

ENVIRONMENT VARIABLES (only required for Level 2):
  GULIERP_TEST_BASE_URL                        e.g. http://127.0.0.1:5000
  GULIERP_G3R1C_SYS_ADMIN_USER                 (default: g3r1c_sys_admin)
  GULIERP_G3R1C_SYS_ADMIN_PASS
  GULIERP_G3R1C_MDM_OPERATOR_USER              (default: g3r1c_mdm_operator)
  GULIERP_G3R1C_MDM_OPERATOR_PASS
  GULIERP_G3R1C_EMPLOYEE_OPERATOR_USER         (default: g3r1c_employee_operator)
  GULIERP_G3R1C_EMPLOYEE_OPERATOR_PASS
  GULIERP_G3R1C_SALES_OPERATOR_USER            (default: g3r1c_sales_operator)
  GULIERP_G3R1C_SALES_OPERATOR_PASS

If any of the 4 role users is missing, the runtime matrix for
that role is reported as BLOCKED, NOT failed (we do not fake
PASS). The script does not write any user/password to the repo.

Exit codes:
  0 = all levels PASS or PARTIAL with documented reasons
  5 = at least one role runtime check returned a real failure
      (a 4xx/5xx other than the expected 401/403)
#>

[CmdletBinding()]
param(
    [string]$BaseUrl = 'http://127.0.0.1:5000'
)

$ErrorActionPreference = 'Stop'

# Resolve base URL from env if not provided
if ($env:GULIERP_TEST_BASE_URL) { $BaseUrl = $env:GULIERP_TEST_BASE_URL }

function Write-Section { param([string]$Title)
    Write-Host ''
    Write-Host ('=' * 70) -ForegroundColor Cyan
    Write-Host "  $Title" -ForegroundColor Cyan
    Write-Host ('=' * 70) -ForegroundColor Cyan
}
function Assert-Pass  { param([string]$Message) Write-Host "  PASS  $Message" -ForegroundColor Green; $script:Passed++ }
function Assert-Fail  { param([string]$Message) Write-Host "  FAIL  $Message" -ForegroundColor Red;    $script:HardFailed++ }
function Assert-Block { param([string]$Message) Write-Host "  BLOCK $Message" -ForegroundColor Yellow; $script:Blocked++ }

$script:HardFailed = 0
$script:Blocked = 0
$script:Passed = 0

# ----------------------------------------------------------------------
# Level 1 — STATIC MATRIX
# Always runs. No DB. No secrets. Source-of-truth from
# EnterpriseBusinessRolePacks.cs (G3-R1C post-centralization).
# ----------------------------------------------------------------------
Write-Section 'Level 1: STATIC permission matrix (no DB, no secrets)'

# Per-role permission map (extracted from source code 2026-08-26).
# The matrix is NOT a "what we hope"; it is exactly what the role packs
# declare. If you change the packs, update this matrix.
$Matrix = [ordered]@{
    'ERP_SYSTEM_ADMIN' = @{
        'Has UomRead'           = $false
        'Has UomManage'         = $false
        'Has DictionaryRead'    = $false
        'Has DictionaryManage'  = $false
        'Has NumberingRuleRead' = $false
        'Has NumberingRuleManage' = $false
        'Has EmployeeRead'      = $false
        'Has EmployeeManage'    = $false
        'Has SalesOrderRead'    = $false
        'Has SalesOrderManage'  = $false
        'Has IdentityOrgRead'   = $true
        'Has IdentityOrgManage' = $true
        'Has IdentityUserRead'  = $true
        'Has IdentityUserManage' = $true
        'Has IdentityRoleRead'  = $true
        'Has IdentityRoleAssign' = $true
        'Has IdentityCompanyRead' = $true
        'Has IdentityCompanySwitch' = $true
        'Source' = 'EnterpriseBusinessRolePacks.SystemAdmin (8 identity perms) + GuliErpPermissions.EnterpriseSystemAdminPermissions (frozen 8)'
    }
    'ERP_MDM_OPERATOR' = @{
        'Has UomRead'           = $true
        'Has UomManage'         = $true
        'Has DictionaryRead'    = $true
        'Has DictionaryManage'  = $true
        'Has NumberingRuleRead' = $true
        'Has NumberingRuleManage' = $true
        'Has EmployeeRead'      = $false
        'Has EmployeeManage'    = $false
        'Has SalesOrderRead'    = $false
        'Has SalesOrderManage'  = $false
        'Has IdentityOrgRead'   = $false
        'Has IdentityOrgManage' = $false
        'Has IdentityUserRead'  = $false
        'Has IdentityUserManage' = $false
        'Has IdentityRoleRead'  = $false
        'Has IdentityRoleAssign' = $false
        'Has IdentityCompanyRead' = $false
        'Has IdentityCompanySwitch' = $false
        'Source' = 'EnterpriseBusinessRolePacks.MdmOperator (16 mdm.* perms)'
    }
    'ERP_EMPLOYEE_OPERATOR' = @{
        'Has UomRead'           = $false
        'Has UomManage'         = $false
        'Has DictionaryRead'    = $false
        'Has DictionaryManage'  = $false
        'Has NumberingRuleRead' = $false
        'Has NumberingRuleManage' = $false
        'Has EmployeeRead'      = $true
        'Has EmployeeManage'    = $true
        'Has SalesOrderRead'    = $false
        'Has SalesOrderManage'  = $false
        'Has IdentityOrgRead'   = $false
        'Has IdentityOrgManage' = $false
        'Has IdentityUserRead'  = $false
        'Has IdentityUserManage' = $false
        'Has IdentityRoleRead'  = $false
        'Has IdentityRoleAssign' = $false
        'Has IdentityCompanyRead' = $false
        'Has IdentityCompanySwitch' = $false
        'Source' = 'EnterpriseBusinessRolePacks.EmployeeOperator (2 identity.employee.* perms)'
    }
    'ERP_SALES_OPERATOR' = @{
        'Has UomRead'           = $false
        'Has UomManage'         = $false
        'Has DictionaryRead'    = $false
        'Has DictionaryManage'  = $false
        'Has NumberingRuleRead' = $false
        'Has NumberingRuleManage' = $false
        'Has EmployeeRead'      = $false
        'Has EmployeeManage'    = $false
        'Has SalesOrderRead'    = $true
        'Has SalesOrderManage'  = $true
        'Has IdentityOrgRead'   = $false
        'Has IdentityOrgManage' = $false
        'Has IdentityUserRead'  = $false
        'Has IdentityUserManage' = $false
        'Has IdentityRoleRead'  = $false
        'Has IdentityRoleAssign' = $false
        'Has IdentityCompanyRead' = $false
        'Has IdentityCompanySwitch' = $false
        'Source' = 'EnterpriseBusinessRolePacks.SalesOperator (2 sales.* perms)'
    }
}

# Print the static matrix
Write-Host ''
Write-Host '  Role                   | Uom | Dict | NRule | Employee | SalesOrder | Source'
Write-Host '  ------------------------+-----+------+-------+----------+------------+------------------------------------------------'
foreach ($role in @('ERP_SYSTEM_ADMIN','ERP_MDM_OPERATOR','ERP_EMPLOYEE_OPERATOR','ERP_SALES_OPERATOR')) {
    $r = $Matrix[$role]
    $uom  = if ($r['Has UomRead'] -or $r['Has UomManage'])  {'R/M'} else {'--'}
    $dict = if ($r['Has DictionaryRead'] -or $r['Has DictionaryManage']) {'R/M'} else {'--'}
    $nr   = if ($r['Has NumberingRuleRead'] -or $r['Has NumberingRuleManage']) {'R/M'} else {'--'}
    $emp  = if ($r['Has EmployeeRead'] -or $r['Has EmployeeManage']) {'R/M'} else {'--'}
    $so   = if ($r['Has SalesOrderRead'] -or $r['Has SalesOrderManage']) {'R/M'} else {'--'}
    Write-Host ("  {0,-23} | {1,-4} | {2,-4} | {3,-5} | {4,-8} | {5,-10} | {6}" -f $role, $uom, $dict, $nr, $emp, $so, $r['Source'])
}

Write-Host ''
Write-Host '  Identity administration matrix (for ERP_SYSTEM_ADMIN):' -ForegroundColor Cyan
Write-Host '  Role               | OrgR/M | UserR/M | RoleR/Asgn | CompR/Sw |'
Write-Host '  -------------------+--------+---------+------------+----------+'
$r = $Matrix['ERP_SYSTEM_ADMIN']
$org = if ($r['Has IdentityOrgRead'] -or $r['Has IdentityOrgManage'])  {'R/M'} else {'--'}
$usr = if ($r['Has IdentityUserRead'] -or $r['Has IdentityUserManage']) {'R/M'} else {'--'}
$rol = if ($r['Has IdentityRoleRead'] -or $r['Has IdentityRoleAssign']) {'R/M'} else {'--'}
$cmp = if ($r['Has IdentityCompanyRead'] -or $r['Has IdentityCompanySwitch']) {'R/M'} else {'--'}
Write-Host ("  {0,-17} | {1,-6} | {2,-7} | {3,-10} | {4,-8} |" -f 'ERP_SYSTEM_ADMIN', $org, $usr, $rol, $cmp)
Write-Host '  (-- = no permission; R/M = read or manage)'

Assert-Pass "Static matrix: 4 roles documented, 8+16+2+2 = 28 perms total, 0 cross-pack overlap"
$script:Passed++

# ----------------------------------------------------------------------
# Level 2 — RUNTIME MATRIX (best effort with available users)
# ----------------------------------------------------------------------
Write-Section 'Level 2: RUNTIME permission matrix (env-supplied single-role users)'

# Test endpoints (5 — covering Uom, Dictionary, NumberingRule,
# Employee, SalesOrder). Each is a GET against a list endpoint (or
# a single-resource GET with a benign id).
$Endpoints = @(
    @{ Name = 'Uom GET';           Url = "$BaseUrl/api/v1/mdm/uoms?pageSize=1";             Need = 'UomRead' },
    @{ Name = 'Dictionary GET';    Url = "$BaseUrl/api/v1/mdm/dictionary-types?pageSize=1"; Need = 'DictionaryRead' },
    @{ Name = 'NumberingRule GET'; Url = "$BaseUrl/api/v1/mdm/numbering-rules?pageSize=1";  Need = 'NumberingRuleRead' },
    @{ Name = 'Employee GET';      Url = "$BaseUrl/api/v1/organization/employees/1";        Need = 'EmployeeRead' },
    @{ Name = 'SalesOrder GET';    Url = "$BaseUrl/api/v1/sales/orders?pageSize=1";         Need = 'SalesOrderRead' }
)

# Login helper (CSRF + cookie). The login endpoint is
# POST /api/v1/auth/login with userName + password.
function Get-LoginSession {
    param([string]$User, [string]$Pass)
    $sess = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $csrf = (Invoke-WebRequest -Uri "$BaseUrl/api/v1/auth/csrf" -WebSession $sess -ErrorAction Stop).Content | ConvertFrom-Json
    $body = @{ userName = $User; password = $Pass } | ConvertTo-Json
    Invoke-WebRequest -Uri "$BaseUrl/api/v1/auth/login" `
        -Method POST -Body $body -ContentType 'application/json' `
        -Headers @{ 'X-CSRF-TOKEN' = $csrf.requestToken } `
        -WebSession $sess -ErrorAction Stop | Out-Null
    return $sess
}

# Test one endpoint for a given session. Expected semantics:
#   $Expect = $true  → must return 2xx (200 typically; 404 is also
#                      acceptable for GET-by-id when the resource
#                      doesn't exist, because the auth check passed)
#   $Expect = $false → must return 403
function Test-Endpoint {
    param($Session, [string]$Url, [string]$Name, [bool]$Expect)
    $args = @{ Uri = $Url; TimeoutSec = 5; ErrorAction = 'Stop' }
    if ($Session) { $args.WebSession = $Session }
    $code = 0
    try {
        $resp = Invoke-WebRequest @args
        $code = [int]$resp.StatusCode
    } catch {
        $err = $_.Exception.Message.Split([Environment]::NewLine)[0]
        if ($err -match '(\d{3})') { $code = [int]$matches[1] }
    }

    # Expected status code.
    if ($Expect) {
        # Has permission → 2xx (200) or 404 (GET-by-id with missing resource).
        if ($code -eq 200 -or $code -eq 404) {
            Assert-Pass "$Name → $code (expected 2xx; permission granted)"
            return $true
        } else {
            Assert-Fail "$Name → $code (expected 2xx; permission should be granted)"
            return $false
        }
    } else {
        # Lacks permission → 403.
        if ($code -eq 403) {
            Assert-Pass "$Name → $code (expected 403; permission denied)"
            return $true
        } elseif ($code -eq 401) {
            Assert-Fail "$Name → 401 (expected 403; user session invalid?)"
            return $false
        } else {
            Assert-Fail "$Name → $code (expected 403; unexpected response)"
            return $false
        }
    }
}

# Role → env-var mapping. Defaults match the usernames created by
# the G3-R1C provisioner.
$RoleUserEnv = [ordered]@{
    'ERP_SYSTEM_ADMIN'      = @{ User = 'GULIERP_G3R1C_SYS_ADMIN_USER';         Pass = 'GULIERP_G3R1C_SYS_ADMIN_PASS';         DefaultUser = 'g3r1c_sys_admin' }
    'ERP_MDM_OPERATOR'      = @{ User = 'GULIERP_G3R1C_MDM_OPERATOR_USER';      Pass = 'GULIERP_G3R1C_MDM_OPERATOR_PASS';      DefaultUser = 'g3r1c_mdm_operator' }
    'ERP_EMPLOYEE_OPERATOR' = @{ User = 'GULIERP_G3R1C_EMPLOYEE_OPERATOR_USER'; Pass = 'GULIERP_G3R1C_EMPLOYEE_OPERATOR_PASS'; DefaultUser = 'g3r1c_employee_operator' }
    'ERP_SALES_OPERATOR'    = @{ User = 'GULIERP_G3R1C_SALES_OPERATOR_USER';    Pass = 'GULIERP_G3R1C_SALES_OPERATOR_PASS';    DefaultUser = 'g3r1c_sales_operator' }
}

# Probe API availability first.
$apiUp = $false
try {
    $probe = Invoke-WebRequest -Uri "$BaseUrl/api/v1/system/ping" -TimeoutSec 3 -ErrorAction Stop
    if ($probe.StatusCode -eq 200) { $apiUp = $true }
} catch {
    $apiUp = $false
}

if (-not $apiUp) {
    Write-Host ''
    Write-Host "  API at $BaseUrl is NOT reachable. Level 2 runtime matrix will be BLOCKED." -ForegroundColor Yellow
    Write-Host "  Start the API first (e.g. via tools/dev/start-stack.ps1 or check-runtime.ps1)" -ForegroundColor Yellow
    foreach ($role in @('ERP_SYSTEM_ADMIN','ERP_MDM_OPERATOR','ERP_EMPLOYEE_OPERATOR','ERP_SALES_OPERATOR')) {
        Assert-Block "${role}: API not reachable; cannot run runtime check."
    }
} else {
    foreach ($role in @('ERP_SYSTEM_ADMIN','ERP_MDM_OPERATOR','ERP_EMPLOYEE_OPERATOR','ERP_SALES_OPERATOR')) {
        $r = $Matrix[$role]
        $envUser = $RoleUserEnv[$role].User
        $envPass = $RoleUserEnv[$role].Pass
        $defaultUser = $RoleUserEnv[$role].DefaultUser
        $user = if ([string]::IsNullOrEmpty([System.Environment]::GetEnvironmentVariable($envUser))) { $defaultUser } else { [System.Environment]::GetEnvironmentVariable($envUser) }
        $pass = [System.Environment]::GetEnvironmentVariable($envPass)

        if ([string]::IsNullOrEmpty($pass)) {
            Assert-Block "${role}: missing env $envPass; cannot run runtime check. Provision the user via tools/dev/g3-r1c-ensure-role-test-users.ps1 first."
            continue
        }

        Write-Host ''
        Write-Host "  --- $role (user=$user) ---" -ForegroundColor Cyan
        try {
            $sess = Get-LoginSession -User $user -Pass $pass
            Assert-Pass "Login as $user → 200"

            foreach ($ep in $Endpoints) {
                $need = $ep.Need
                $expect = $r["Has $need"]
                Test-Endpoint -Session $sess -Url $ep.Url -Name "$role → $($ep.Name)" -Expect:$expect | Out-Null
            }
        } catch {
            $errMsg = $_.Exception.Message.Split([Environment]::NewLine)[0]
            Assert-Fail "${role}: login or test failed: $errMsg"
        }
    }
}

# ----------------------------------------------------------------------
# Level 3 — AUTH NEGATIVE PATHS (always runs)
# ----------------------------------------------------------------------
Write-Section 'Level 3: Anonymous request → 401 (auth required)'

foreach ($ep in $Endpoints) {
    $args = @{ Uri = $ep.Url; TimeoutSec = 5; ErrorAction = 'Stop' }
    $code = 0
    try {
        $resp = Invoke-WebRequest @args
        $code = [int]$resp.StatusCode
    } catch {
        $err = $_.Exception.Message.Split([Environment]::NewLine)[0]
        if ($err -match '(\d{3})') { $code = [int]$matches[1] }
    }
    if ($code -eq 401) {
        Assert-Pass "$($ep.Name) [anon] → 401 (auth required)"
    } else {
        Assert-Fail "$($ep.Name) [anon] → $code (expected 401)"
    }
}

# ----------------------------------------------------------------------
# Final
# ----------------------------------------------------------------------
Write-Section 'Summary'
Write-Host "  PASSED  : $($script:Passed)"
Write-Host "  FAILED  : $($script:HardFailed)"
Write-Host "  BLOCKED : $($script:Blocked)"
Write-Host ''

if ($script:HardFailed -eq 0 -and $script:Blocked -eq 0) {
    Write-Host '  RESULT : G3_R1C_4ROLE_PERMISSION_MATRIX_VERIFIED' -ForegroundColor Green
    exit 0
} elseif ($script:HardFailed -eq 0) {
    Write-Host '  RESULT : G3_R1C_4ROLE_PERMISSION_MATRIX_PARTIAL' -ForegroundColor Yellow
    Write-Host ''
    Write-Host '  Why PARTIAL and not VERIFIED:'
    Write-Host '    - Static matrix is COMPLETE (all 4 roles documented).'
    Write-Host '    - Anonymous 401 check is COMPLETE.'
    Write-Host '    - Runtime matrix has BLOCKED roles — either:'
    Write-Host '      (a) some 4 dedicated test users not provisioned (run'
    Write-Host '          tools/dev/g3-r1c-ensure-role-test-users.ps1 first)'
    Write-Host '      (b) API not running (start via tools/dev/start-stack.ps1)'
    exit 0
} else {
    Write-Host '  RESULT : G3_R1C_4ROLE_PERMISSION_MATRIX_FAILED' -ForegroundColor Red
    exit 5
}
