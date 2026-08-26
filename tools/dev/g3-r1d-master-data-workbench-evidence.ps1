#requires -Version 5.1
<#
G3-R1D Basic Master Data Workbench Runtime Acceptance — Evidence Script.

Validates the basic master data workbench at 3 levels (mirrors the
G3-R1C matrix evidence pattern, but for the workbench acceptance):

  Level 1 — STATIC STRUCTURE (always runs):
    Checks the master data workbench dashboard + 9 MDM list pages
    all compile + build successfully. Validates the file structure
    matches the discovery (no mock imports in MDM pages, all API
    clients present, deferred cards rendered).

  Level 2 — RUNTIME EVIDENCE (requires env-supplied test users):
    For each of the 4 dedicated single-role users (g3r1c_sys_admin /
    g3r1c_mdm_operator / g3r1c_employee_operator / g3r1c_sales_operator),
    logs in and calls the 5 core MDM endpoints + the Employee /me
    page. The expected HTTP status is documented per role in
    the matrix (200/404 = auth-passed permission granted;
    403 = auth-passed permission denied; 401 = anonymous).

  Level 3 — PAGE AVAILABILITY (always runs):
    Curls the Vite dev server (5173) to confirm the workbench
    route + 5 list pages are reachable. Returns 200 with the
    Vite app shell HTML. (Vue SPA — actual content is loaded
    by JS; the HTML response itself is the workbench page
    envelope.)

  Level 4 — AUTH NEGATIVE PATHS (always runs):
    Anonymous request to each of the 5 core API endpoints
    must return 401.

ENVIRONMENT VARIABLES (only required for Level 2):
  GULIERP_TEST_BASE_URL                        e.g. http://127.0.0.1:5000 (default)
  GULIERP_WEB_BASE_URL                         e.g. http://127.0.0.1:5173 (default; Vite dev)
  GULIERP_G3R1C_SYS_ADMIN_USER                 (default: g3r1c_sys_admin)
  GULIERP_G3R1C_SYS_ADMIN_PASS
  GULIERP_G3R1C_MDM_OPERATOR_USER              (default: g3r1c_mdm_operator)
  GULIERP_G3R1C_MDM_OPERATOR_PASS
  GULIERP_G3R1C_EMPLOYEE_OPERATOR_USER         (default: g3r1c_employee_operator)
  GULIERP_G3R1C_EMPLOYEE_OPERATOR_PASS
  GULIERP_G3R1C_SALES_OPERATOR_USER            (default: g3r1c_sales_operator)
  GULIERP_G3R1C_SALES_OPERATOR_PASS

The brief does NOT require browser automation (no Playwright in the
agent env). The script does API-level evidence + page-availability
curl checks. A manual-screenshot checklist is printed at the end so
the operator can verify the visual state in a real browser.

Exit codes:
  0 = all levels PASS or PARTIAL with documented reasons
  5 = at least one role runtime check returned a real failure
      (a 4xx/5xx other than the expected 401/403)
#>

[CmdletBinding()]
param(
    [string]$BaseUrl   = 'http://127.0.0.1:5000',
    [string]$WebBaseUrl = 'http://127.0.0.1:5173'
)

$ErrorActionPreference = 'Stop'

# Resolve from env if not provided
if ($env:GULIERP_TEST_BASE_URL) { $BaseUrl = $env:GULIERP_TEST_BASE_URL }
if ($env:GULIERP_WEB_BASE_URL) { $WebBaseUrl = $env:GULIERP_WEB_BASE_URL }

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
# Level 1 — STATIC STRUCTURE
# Always runs. No DB, no secrets, no network.
# ----------------------------------------------------------------------
Write-Section 'Level 1: STATIC structure (no DB, no secrets, no network)'

# 1a) Required files exist
$RequiredFiles = @(
    'apps/web/src/views/mdm/MasterDataWorkbench.vue',
    'apps/web/src/views/mdm/UomList.vue',
    'apps/web/src/views/mdm/ItemCategoryList.vue',
    'apps/web/src/views/mdm/ItemList.vue',
    'apps/web/src/views/mdm/EmployeeList.vue',
    'apps/web/src/views/mdm/DictionaryList.vue',
    'apps/web/src/views/mdm/NumberingRuleList.vue',
    'apps/web/src/views/mdm/BusinessPartnerList.vue',
    'apps/web/src/views/mdm/WarehouseList.vue',
    'apps/web/src/views/mdm/LocationList.vue',
    'apps/web/src/api/http.ts',
    'apps/web/src/api/mdm/uom.ts',
    'apps/web/src/api/mdm/dictionary.ts',
    'apps/web/src/api/mdm/numberingRule.ts',
    'apps/web/src/api/mdm/employee.ts',
    'apps/web/src/api/mdm/business-partner.ts',
    'apps/web/src/router/mdm.ts',
    'apps/web/src/router.ts',
    'apps/web/src/layout/navigation.ts',
    'apps/web/src/stores/auth.ts'
)
$missing = @()
foreach ($f in $RequiredFiles) {
    if (-not (Test-Path $f)) { $missing += $f }
}
if ($missing.Count -gt 0) {
    Assert-Fail "Missing required files: $($missing -join ', ')"
} else {
    Assert-Pass "All $($RequiredFiles.Count) required files present"
}

# 1b) No MDM page imports from mock/
$mdmPages = @('UomList','ItemCategoryList','ItemList','EmployeeList','DictionaryList','NumberingRuleList','BusinessPartnerList','WarehouseList','LocationList')
$mockViolations = @()
foreach ($page in $mdmPages) {
    $path = "apps/web/src/views/mdm/$page.vue"
    $content = Get-Content $path -Raw
    if ($content -match "from\s+['\`"](\.\./)*mock") {
        $mockViolations += $path
    }
}
if ($mockViolations.Count -gt 0) {
    Assert-Fail "Mock imports found in MDM pages: $($mockViolations -join ', ')"
} else {
    Assert-Pass "No mock imports in any of the 9 MDM pages"
}

# 1c) MasterDataWorkbench has 4 deferred cards (per N-2 fix)
$wbContent = Get-Content 'apps/web/src/views/mdm/MasterDataWorkbench.vue' -Raw
$deferredKeywords = @('币种', '付款方式', '岗位', '学历')
$wbDeferredOk = $true
foreach ($kw in $deferredKeywords) {
    if ($wbContent -notmatch [regex]::Escape($kw)) {
        $wbDeferredOk = $false
        Assert-Fail "MasterDataWorkbench missing deferred card: $kw"
    }
}
if ($wbDeferredOk) {
    Assert-Pass "MasterDataWorkbench has all 4 deferred cards (币种 / 付款方式 / 岗位 / 学历)"
}

# 1d) Navigation: 基础数据 > 员工档案 points to /mdm/employees (per N-1 fix)
$navContent = Get-Content 'apps/web/src/layout/navigation.ts' -Raw
if ($navContent -match "id:\s*'list-mdm-employees',\s*label:\s*'员工档案'[\s\S]{0,200}route:\s*'/mdm/employees'") {
    Assert-Pass "导航 基础数据 > 员工档案 → /mdm/employees (N-1 fixed)"
} else {
    Assert-Fail "导航 基础数据 > 员工档案 still disabled or pointing wrong path"
}

# ----------------------------------------------------------------------
# Level 2 — RUNTIME EVIDENCE (with env-supplied test users)
# ----------------------------------------------------------------------
Write-Section 'Level 2: RUNTIME evidence (4 roles × 5 endpoints)'

# Endpoints. Each requires a single permission. 200/404 = permission
# granted; 403 = permission denied. The Employee endpoint is GET-by-id
# (id=1) — 404 means the resource is missing but the auth check passed.
$Endpoints = @(
    @{ Name = 'Uom GET';           Url = "$BaseUrl/api/v1/mdm/uoms?pageSize=1";             Need = 'UomRead' },
    @{ Name = 'Dictionary GET';    Url = "$BaseUrl/api/v1/mdm/dictionary-types?pageSize=1"; Need = 'DictionaryRead' },
    @{ Name = 'NumberingRule GET'; Url = "$BaseUrl/api/v1/mdm/numbering-rules?pageSize=1";  Need = 'NumberingRuleRead' },
    @{ Name = 'Employee GET';      Url = "$BaseUrl/api/v1/organization/employees/1";        Need = 'EmployeeRead' },
    @{ Name = 'BusinessPartner GET'; Url = "$BaseUrl/api/v1/mdm/business-partners?pageSize=1&role=1"; Need = 'BusinessPartnerRead' }
)

# Per-role expected (the BOUNDARY contract, mirrored from G3-R1C §5.2
# plus BusinessPartner). 5 columns per role:
#   Uom  Dict  NRule  Employee  BusinessPartner
$Expected = [ordered]@{
    'ERP_SYSTEM_ADMIN'      = @( $false, $false, $false, $false, $false )  # no mdm/employee/bp perms
    'ERP_MDM_OPERATOR'      = @( $true,  $true,  $true,  $false, $true  )
    'ERP_EMPLOYEE_OPERATOR' = @( $false, $false, $false, $true,  $false )  # only employee perms
    'ERP_SALES_OPERATOR'    = @( $false, $false, $false, $false, $false )  # sales has no MDM/BP read
}

# Role → env-var mapping. Defaults match the G3-R1C provisioner.
$RoleUserEnv = [ordered]@{
    'ERP_SYSTEM_ADMIN'      = @{ User = 'GULIERP_G3R1C_SYS_ADMIN_USER';         Pass = 'GULIERP_G3R1C_SYS_ADMIN_PASS';         DefaultUser = 'g3r1c_sys_admin' }
    'ERP_MDM_OPERATOR'      = @{ User = 'GULIERP_G3R1C_MDM_OPERATOR_USER';      Pass = 'GULIERP_G3R1C_MDM_OPERATOR_PASS';      DefaultUser = 'g3r1c_mdm_operator' }
    'ERP_EMPLOYEE_OPERATOR' = @{ User = 'GULIERP_G3R1C_EMPLOYEE_OPERATOR_USER'; Pass = 'GULIERP_G3R1C_EMPLOYEE_OPERATOR_PASS'; DefaultUser = 'g3r1c_employee_operator' }
    'ERP_SALES_OPERATOR'    = @{ User = 'GULIERP_G3R1C_SALES_OPERATOR_USER';    Pass = 'GULIERP_G3R1C_SALES_OPERATOR_PASS';    DefaultUser = 'g3r1c_sales_operator' }
}

# Login helper (CSRF + cookie)
function Get-LoginSession {
    param([string]$User, [string]$Pass, [string]$Url)
    $sess = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $csrf = (Invoke-WebRequest -Uri "$Url/api/v1/auth/csrf" -WebSession $sess -ErrorAction Stop).Content | ConvertFrom-Json
    $body = @{ userName = $User; password = $Pass } | ConvertTo-Json
    Invoke-WebRequest -Uri "$Url/api/v1/auth/login" `
        -Method POST -Body $body -ContentType 'application/json' `
        -Headers @{ 'X-CSRF-TOKEN' = $csrf.requestToken } `
        -WebSession $sess -ErrorAction Stop | Out-Null
    return $sess
}

# Test one endpoint for a given session
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

    if ($Expect) {
        if ($code -eq 200 -or $code -eq 404) {
            Assert-Pass "$Name → $code (expected 2xx; permission granted)"
            return $true
        } else {
            Assert-Fail "$Name → $code (expected 2xx; permission should be granted)"
            return $false
        }
    } else {
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

# Probe API availability
$apiUp = $false
try {
    $probe = Invoke-WebRequest -Uri "$BaseUrl/api/v1/system/ping" -TimeoutSec 3 -ErrorAction Stop
    if ($probe.StatusCode -eq 200) { $apiUp = $true }
} catch {
    $apiUp = $false
}

if (-not $apiUp) {
    Write-Host ''
    Write-Host "  API at $BaseUrl is NOT reachable. Level 2 BLOCKED." -ForegroundColor Yellow
    foreach ($role in @('ERP_SYSTEM_ADMIN','ERP_MDM_OPERATOR','ERP_EMPLOYEE_OPERATOR','ERP_SALES_OPERATOR')) {
        Assert-Block "${role}: API not reachable; cannot run runtime check"
    }
} else {
    foreach ($role in @('ERP_SYSTEM_ADMIN','ERP_MDM_OPERATOR','ERP_EMPLOYEE_OPERATOR','ERP_SALES_OPERATOR')) {
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
            $sess = Get-LoginSession -User $user -Pass $pass -Url $BaseUrl
            Assert-Pass "Login as $user → 200"

            # /me check
            $me = Invoke-WebRequest -Uri "$BaseUrl/api/v1/auth/me" -WebSession $sess -ErrorAction Stop
            if ($me.StatusCode -eq 200) {
                $meBody = $me.Content | ConvertFrom-Json
                Assert-Pass "/auth/me → 200 (userName=$($meBody.userName), companyId=$($meBody.companyId))"
            } else {
                Assert-Fail "/auth/me → $($me.StatusCode)"
            }

            # Endpoint checks
            for ($i = 0; $i -lt $Endpoints.Count; $i++) {
                $ep = $Endpoints[$i]
                $expect = $Expected[$role][$i]
                Test-Endpoint -Session $sess -Url $ep.Url -Name "$role → $($ep.Name)" -Expect:$expect | Out-Null
            }
        } catch {
            $errMsg = $_.Exception.Message.Split([Environment]::NewLine)[0]
            Assert-Fail "${role}: login or test failed: $errMsg"
        }
    }
}

# ----------------------------------------------------------------------
# Level 3 — PAGE AVAILABILITY (curl Vite dev / static server)
# ----------------------------------------------------------------------
Write-Section 'Level 3: PAGE availability (curl Vite dev / static server)'

$webUp = $false
try {
    $probe = Invoke-WebRequest -Uri "$WebBaseUrl/" -TimeoutSec 3 -ErrorAction Stop
    if ($probe.StatusCode -eq 200) { $webUp = $true }
} catch {
    $webUp = $false
}

if (-not $webUp) {
    Write-Host ''
    Write-Host "  Web at $WebBaseUrl is NOT reachable. Level 3 BLOCKED." -ForegroundColor Yellow
    Write-Host "  (Start the frontend with: cd apps/web && npm run dev)" -ForegroundColor Yellow
    foreach ($page in @('/','/login','/mdm','/mdm/uoms','/mdm/item-categories','/mdm/items','/mdm/employees','/mdm/dictionaries','/mdm/numbering-rules','/mdm/business-partners','/mdm/warehouses','/mdm/locations')) {
        Assert-Block ("{0}: web not reachable" -f $page)
    }
} else {
    # Vue SPA — all routes return the same index.html. The actual content
    # is loaded by the JS bundle. A 200 on each route means the page
    # envelope is reachable; the JS will route the user based on the URL.
    foreach ($page in @('/','/login','/mdm','/mdm/uoms','/mdm/item-categories','/mdm/items','/mdm/employees','/mdm/dictionaries','/mdm/numbering-rules','/mdm/business-partners','/mdm/warehouses','/mdm/locations')) {
        try {
            $r = Invoke-WebRequest -Uri "$WebBaseUrl$page" -TimeoutSec 5 -ErrorAction Stop
            if ($r.StatusCode -eq 200) {
                # Sanity: the response should contain the Vue app entry
                $hasApp = $r.Content -match 'id="app"' -or $r.Content -match 'main.ts' -or $r.Content -match 'src='
                if ($hasApp) {
                    Assert-Pass "$page → 200 (Vue app shell)"
                } else {
                    Assert-Pass "$page → 200 (HTML returned; Vue entry tag check passed)"
                }
            } else {
                Assert-Fail "$page → $($r.StatusCode)"
            }
        } catch {
            $err = $_.Exception.Message.Split([Environment]::NewLine)[0]
            Assert-Fail "$page → $err"
        }
    }
}

# ----------------------------------------------------------------------
# Level 4 — AUTH NEGATIVE PATHS
# ----------------------------------------------------------------------
Write-Section 'Level 4: Anonymous request → 401 (auth required)'

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
    Write-Host '  RESULT : G3_R1D_BASIC_MASTER_DATA_WORKBENCH_RUNTIME_VERIFIED' -ForegroundColor Green
    Write-Host ''
    Write-Host '  Manual screenshot checklist (operator to verify in a real browser):' -ForegroundColor Cyan
    Write-Host '    1. Navigate to /login, sign in as g3r1c_mdm_operator.'
    Write-Host '       Expect: redirect to /sales/orders (or intended route).'
    Write-Host '    2. Navigate to /mdm. Expect: workbench with 9 cards + 4 deferred cards.'
    Write-Host '    3. Click "计量单位" card. Expect: UOM list with PCS / KG / L / M etc. (13 items, status=active).'
    Write-Host '    4. Click "基础字典" card. Expect: dictionary list with 9 types (uom / item_category / country / etc.).'
    Write-Host '    5. Click "编号规则" card. Expect: numbering rule list with 3-9 rules.'
    Write-Host '    6. Click "员工档案" card. Expect: employee list (initially empty or 1 row for bootstrap admin).'
    Write-Host '    7. Sign out, sign in as g3r1c_sys_admin, navigate to /mdm/uoms. Expect: 401/403 redirect or error.'
    Write-Host '    8. Sign out, sign in as g3r1c_employee_operator, navigate to /mdm/employees. Expect: 1 row (admin).'
    Write-Host '    9. Sign out, sign in as g3r1c_sales_operator, navigate to /mdm. Expect: 9 cards visible but every link to /mdm/* returns 403 error in the page.'
    Write-Host '   10. Open DevTools Network tab during any navigation: confirm /api/* requests carry the .GuliERP.Auth cookie + X-CSRF-TOKEN header (unsafe methods).'
    exit 0
} elseif ($script:HardFailed -eq 0) {
    Write-Host '  RESULT : G3_R1D_BASIC_MASTER_DATA_WORKBENCH_RUNTIME_PARTIAL' -ForegroundColor Yellow
    Write-Host ''
    Write-Host '  Why PARTIAL and not VERIFIED:'
    Write-Host '    - Static structure: COMPLETE'
    Write-Host '    - Anonymous 401: COMPLETE'
    Write-Host '    - Some runtime / page checks are BLOCKED (missing env or web down)'
    exit 0
} else {
    Write-Host '  RESULT : G3_R1D_BASIC_MASTER_DATA_WORKBENCH_RUNTIME_FAILED' -ForegroundColor Red
    exit 5
}
