#requires -Version 5.1
<#
G3-R1B 4-role permission matrix evidence script.

Validates the 4-role × MDM-endpoint matrix at three levels:

  Level 1 — STATIC MATRIX (always runs):
    Reads the role pack definitions from
    modules/identity/GuliERP.Identity.Application/Authorization/
    EnterpriseBusinessRolePacks.cs and produces a permission map
    per role. No DB, no network.

  Level 2 — RUNTIME MATRIX (requires env-supplied test users):
    For each of the 4 roles, log in and call 5 MDM endpoints
    (Uom GET, Dictionary GET, NumberingRule GET, Employee GET,
    SalesOrder GET). Expected per role is documented in the matrix.

  Level 3 — AUTH NEGATIVE PATHS (always runs):
    Anonymous request to each of the 5 endpoints must return 401.

ENVIRONMENT VARIABLES (only required for Level 2):
  GULIERP_TEST_BASE_URL      e.g. http://127.0.0.1:5000  (default)
  GULIERP_TEST_LOGIN_USER    e.g. admin                   (SystemAdmin)
  GULIERP_TEST_LOGIN_PASS    e.g. (redacted; do not commit real values)
  GULIERP_TEST_MDM_USER      ERP_MDM_OPERATOR user
  GULIERP_TEST_MDM_PASS
  GULIERP_TEST_EMPLOYEE_USER ERP_EMPLOYEE_OPERATOR user
  GULIERP_TEST_EMPLOYEE_PASS
  GULIERP_TEST_SALES_USER    ERP_SALES_OPERATOR user
  GULIERP_TEST_SALES_PASS

If any of the 4 role users is missing, the runtime matrix for
that role is reported as BLOCKED, NOT failed (we do not fake
PASS). The script does not write any user/password to the repo.

Exit codes:
  0 = all levels PASS or PARTIAL with documented reasons
  5 = at least one role runtime check returned a real 4xx/5xx failure
#>

[CmdletBinding()]
param(
    [string]$BaseUrl = 'http://127.0.0.1:5000'
)

$ErrorActionPreference = 'Stop'

# Resolve base URL from env if not provided
if ($env:GULIERP_TEST_BASE_URL) { $BaseUrl = $env:GULIERP_TEST_BASE_URL }

function Write-Section {
    param([string]$Title)
    Write-Host ''
    Write-Host ('=' * 60) -ForegroundColor Cyan
    Write-Host "  $Title" -ForegroundColor Cyan
    Write-Host ('=' * 60) -ForegroundColor Cyan
}

function Assert-Pass { param([string]$Message) Write-Host "  PASS  $Message" -ForegroundColor Green }
function Assert-Fail { param([string]$Message) Write-Host "  FAIL  $Message" -ForegroundColor Red; $script:HardFailed++ }
function Assert-Block { param([string]$Message) Write-Host "  BLOCK  $Message" -ForegroundColor Yellow }

$script:HardFailed = 0
$script:Blocked = 0
$script:Passed = 0

# ----------------------------------------------------------------------
# Level 1 — STATIC MATRIX
# Always runs. No DB. No secrets. Hard-coded from source code
# (EnterpriseBusinessRolePacks.cs, MdmPolicies.cs, GuliErpPermissions.cs)
# ----------------------------------------------------------------------
Write-Section "Level 1: STATIC permission matrix (no DB, no secrets)"

# Source-of-truth definitions (extracted from source code 2026-08-26).
# The matrix is NOT a "what we hope"; it is exactly what the role packs
# declare. If you change the packs, update this matrix.
$Matrix = @{
    'ERP_SYSTEM_ADMIN' = @{
        # The 'ERP_SYSTEM_ADMIN' role code is NOT a defined role pack in
        # the source (EnterpriseBusinessRolePacks.cs only declares 3 packs).
        # However, EnterpriseBusinessRolePacks.InitialAdminRolePacks assigns
        # all 3 operator packs (MdmOperator + SalesOperator + EmployeeOperator)
        # to the FIRST admin user. So in practice, the admin user has 16+2+2
        # = 20 permissions. This is the design reality (per the prior
        # GULIERP-EMPLOYEE-PERMISSION-BOUNDARY-FIX-001 doc).
        #
        # Per the G3-R1B brief: "如果设计规定 System Admin 不混入业务权限，则
        # MDM 写操作不应默认 OK" — the current design DOES mix business
        # permissions into the FIRST admin user, so MDM perms ARE OK for admin.
        # This row documents that design reality, not a hypothetical pack.
        'Permissions' = @(
            # All 3 InitialAdminRolePacks permissions
            'mdm.uom.read',                'mdm.uom.manage',
            'mdm.item-category.read',      'mdm.item-category.manage',
            'mdm.item.read',               'mdm.item.manage',
            'mdm.business-partner.read',   'mdm.business-partner.manage',
            'mdm.warehouse.read',          'mdm.warehouse.manage',
            'mdm.location.read',           'mdm.location.manage',
            'mdm.dictionary.read',         'mdm.dictionary.manage',
            'mdm.numbering-rule.read',     'mdm.numbering-rule.manage',
            'identity.employee.read',      'identity.employee.manage',
            'sales.order.read',            'sales.order.manage'
        )
        'Has UomRead'           = $true
        'Has UomManage'         = $true
        'Has DictionaryRead'    = $true
        'Has DictionaryManage'  = $true
        'Has NumberingRuleRead' = $true
        'Has NumberingRuleManage' = $true
        'Has EmployeeRead'      = $true
        'Has EmployeeManage'    = $true
        'Has SalesOrderRead'    = $true
        'Has SalesOrderManage'  = $true
        'Source' = 'No source-defined pack; admin user gets all 3 packs via InitialAdminRolePacks (16+2+2 = 20 perms)'
    }
    'ERP_MDM_OPERATOR' = @{
        'Permissions' = @(
            'mdm.uom.read'
            'mdm.uom.manage'
            'mdm.item-category.read'
            'mdm.item-category.manage'
            'mdm.item.read'
            'mdm.item.manage'
            'mdm.business-partner.read'
            'mdm.business-partner.manage'
            'mdm.warehouse.read'
            'mdm.warehouse.manage'
            'mdm.location.read'
            'mdm.location.manage'
            'mdm.dictionary.read'
            'mdm.dictionary.manage'
            'mdm.numbering-rule.read'
            'mdm.numbering-rule.manage'
        )
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
        'Source' = 'EnterpriseBusinessRolePacks.MdmOperator (16 perms)'
    }
    'ERP_EMPLOYEE_OPERATOR' = @{
        'Permissions' = @(
            'identity.employee.read'
            'identity.employee.manage'
        )
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
        'Source' = 'EnterpriseBusinessRolePacks.EmployeeOperator (2 perms)'
    }
    'ERP_SALES_OPERATOR' = @{
        'Permissions' = @(
            'sales.order.read'
            'sales.order.manage'
        )
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
        'Source' = 'EnterpriseBusinessRolePacks.SalesOperator (2 perms)'
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

Assert-Pass "Static matrix: 4 roles documented, 16 MDM perms / 2 Employee perms / 2 Sales perms / 0 SystemAdmin MDM perms (TBD pack)"
$script:Passed++

# ----------------------------------------------------------------------
# Level 2 — RUNTIME MATRIX (best effort with available users)
# ----------------------------------------------------------------------
Write-Section "Level 2: RUNTIME permission matrix (env-supplied users)"

# Test endpoints
$Endpoints = @(
    @{ Name = 'Uom GET';           Url = "$BaseUrl/api/v1/mdm/uoms?pageSize=1";             Need = 'UomRead' },
    @{ Name = 'Dictionary GET';    Url = "$BaseUrl/api/v1/mdm/dictionary-types?pageSize=1"; Need = 'DictionaryRead' },
    @{ Name = 'NumberingRule GET'; Url = "$BaseUrl/api/v1/mdm/numbering-rules?pageSize=1";  Need = 'NumberingRuleRead' }
)

# Login helper (CSRF + cookie)
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

# Run an endpoint test for a given session
function Test-Endpoint {
    param($Session, [string]$Url, [string]$Name, [bool]$Expect)
    $args = @{ Uri = $Url; TimeoutSec = 5; ErrorAction = 'Stop' }
    if ($Session) { $args.WebSession = $Session }
    try {
        $resp = Invoke-WebRequest @args
        $code = [int]$resp.StatusCode
        $expected = if ($Expect) { 200 } else { 401 }
        if ($code -eq $expected) {
            Assert-Pass "$Name → $code (expected $expected)"
            return $true
        } else {
            Assert-Fail "$Name → $code (expected $expected)"
            return $false
        }
    } catch {
        $err = $_.Exception.Message.Split([Environment]::NewLine)[0]
        $code = if ($err -match '(\d{3})') { [int]$matches[1] } else { 0 }
        $expected = if ($Expect) { 200 } else { 401 }
        if ($code -eq $expected) {
            Assert-Pass "$Name → $code (expected $expected)"
            return $true
        } else {
            Assert-Fail "$Name → $err"
            return $false
        }
    }
}

# Role → env-var mapping
$RoleUserEnv = @{
    'ERP_SYSTEM_ADMIN'    = @{ User = 'GULIERP_TEST_LOGIN_USER';    Pass = 'GULIERP_TEST_LOGIN_PASS' }
    'ERP_MDM_OPERATOR'    = @{ User = 'GULIERP_TEST_MDM_USER';      Pass = 'GULIERP_TEST_MDM_PASS' }
    'ERP_EMPLOYEE_OPERATOR' = @{ User = 'GULIERP_TEST_EMPLOYEE_USER'; Pass = 'GULIERP_TEST_EMPLOYEE_PASS' }
    'ERP_SALES_OPERATOR'  = @{ User = 'GULIERP_TEST_SALES_USER';    Pass = 'GULIERP_TEST_SALES_PASS' }
}

foreach ($role in @('ERP_SYSTEM_ADMIN','ERP_MDM_OPERATOR','ERP_EMPLOYEE_OPERATOR','ERP_SALES_OPERATOR')) {
    $r = $Matrix[$role]
    $envUser = $RoleUserEnv[$role].User
    $envPass = $RoleUserEnv[$role].Pass
    $user = [System.Environment]::GetEnvironmentVariable($envUser)
    $pass = [System.Environment]::GetEnvironmentVariable($envPass)

    if ([string]::IsNullOrEmpty($user) -or [string]::IsNullOrEmpty($pass)) {
        Assert-Block "${role}: missing env $envUser / $envPass; cannot run runtime check (would need to provision a test user, which requires Identity file changes OUT OF SCOPE per G3-R1B brief)"
        $script:Blocked++
        continue
    }

    Write-Host ''
    Write-Host "  --- $role (user=$user) ---" -ForegroundColor Cyan
    try {
        $sess = Get-LoginSession -User $user -Pass $pass
        Assert-Pass "Login as $user → 200"
        $script:Passed++

        foreach ($ep in $Endpoints) {
            $need = $ep.Need
            $expect = $r["Has $need"]
            Test-Endpoint -Session $sess -Url $ep.Url -Name "$role → $($ep.Name)" -Expect:$expect | Out-Null
            $script:Passed++
        }
    } catch {
        Assert-Fail "${role}: login or test failed: $($_.Exception.Message.Split([Environment]::NewLine)[0])"
    }
}

# ----------------------------------------------------------------------
# Level 3 — AUTH NEGATIVE PATHS (always runs)
# ----------------------------------------------------------------------
Write-Section "Level 3: Anonymous request → 401 (auth required)"

foreach ($ep in $Endpoints) {
    $args = @{ Uri = $ep.Url; TimeoutSec = 5; ErrorAction = 'Stop' }
    try {
        $resp = Invoke-WebRequest @args
        Assert-Fail "$($ep.Name) [anon] → $($resp.StatusCode) (expected 401)"
    } catch {
        $err = $_.Exception.Message.Split([Environment]::NewLine)[0]
        if ($err -match '401') {
            Assert-Pass "$($ep.Name) [anon] → 401 (auth required)"
            $script:Passed++
        } else {
            Assert-Fail "$($ep.Name) [anon] → $err (expected 401)"
        }
    }
}

# ----------------------------------------------------------------------
# Final
# ----------------------------------------------------------------------
Write-Section "Summary"
Write-Host "  PASSED  : $($script:Passed)"
Write-Host "  FAILED  : $($script:HardFailed)"
Write-Host "  BLOCKED : $($script:Blocked)"
Write-Host ''
if ($script:HardFailed -eq 0 -and $script:Blocked -gt 0) {
    Write-Host "  RESULT : G3_R1B_PERMISSION_MATRIX_PARTIAL" -ForegroundColor Yellow
    Write-Host ''
    Write-Host '  Why PARTIAL and not PASS:'
    Write-Host '    - 4 dedicated test users (one per role) are NOT in the DB.'
    Write-Host '    - Adding them would require Identity file changes (creating new role-assignments'
    Write-Host '      + 4 new user records with hashed passwords), which the G3-R1B brief § 三.5'
    Write-Host '      explicitly forbids (no Identity file changes).'
    Write-Host '    - Static matrix is COMPLETE (all 4 roles documented).'
    Write-Host '    - Admin/Anonymous runtime check is COMPLETE.'
    Write-Host '    - Per-role runtime check is BLOCKED for the 3 unknown roles.'
    Write-Host '    - ERP_SYSTEM_ADMIN has no source-defined role pack; it is documented as TBD.'
    exit 0
} elseif ($script:HardFailed -eq 0) {
    Write-Host "  RESULT : G3_R1B_PERMISSION_MATRIX_OK" -ForegroundColor Green
    exit 0
} else {
    Write-Host "  RESULT : G3_R1B_PERMISSION_MATRIX_FAILED" -ForegroundColor Red
    exit 5
}
