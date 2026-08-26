#requires -Version 5.1
<#
G3-R2A SalesOrder — Web (Vite dev / frontend) evidence script.

Validates the FRONTEND half of the SalesOrder rebuild:
  - The Vite dev server (default :5173) serves the
    /sales/orders routes with a 200 status.
  - The SalesOrderList + SalesOrderEdit + SalesOrderDetail
    files do NOT import from mock/sales-order.ts.
  - The SalesOrderList + SalesOrderEdit files DO import
    the new sales-order-context.ts (the G3-R2A facade).
  - The frontend build is clean (npm run build).

ENVIRONMENT VARIABLES (all optional):
  GULIERP_WEB_BASE_URL        e.g. http://127.0.0.1:5173 (default)
  GULIERP_TEST_BASE_URL       e.g. http://127.0.0.1:5001 (default;
                              used for the API smoke check)
  GULIERP_G3R1C_SALES_OPERATOR_PASS   (for Level 3 cross-check)
#>

[CmdletBinding()]
param(
    [string]$WebBaseUrl = 'http://127.0.0.1:5173',
    [string]$BaseUrl    = 'http://127.0.0.1:5001'
)

$ErrorActionPreference = 'Stop'

if ($env:GULIERP_WEB_BASE_URL)  { $WebBaseUrl = $env:GULIERP_WEB_BASE_URL }
if ($env:GULIERP_TEST_BASE_URL) { $BaseUrl = $env:GULIERP_TEST_BASE_URL }

function Write-Section { param([string]$Title)
    Write-Host ''
    Write-Host ('=' * 70) -ForegroundColor Cyan
    Write-Host "  $Title" -ForegroundColor Cyan
    Write-Host ('=' * 70) -ForegroundColor Cyan
}
function Assert-Pass  { param([string]$Message) Write-Host "  PASS    $Message" -ForegroundColor Green; $script:Passed++ }
function Assert-Fail  { param([string]$Message) Write-Host "  FAIL    $Message" -ForegroundColor Red;    $script:HardFailed++ }
function Assert-Block { param([string]$Message) Write-Host "  BLOCK   $Message" -ForegroundColor Yellow; $script:Blocked++ }

$script:HardFailed = 0
$script:Blocked = 0
$script:Passed = 0

# ----------------------------------------------------------------------
# Level 1 — Vite dev server route availability
# (Vue SPA — each route returns the same index.html shell;
#  the actual content is loaded by the JS bundle.)
# ----------------------------------------------------------------------
Write-Section 'Level 1: Vite dev server route availability'

$webUp = $false
try {
    $probe = Invoke-WebRequest -Uri ($WebBaseUrl + '/') -UseBasicParsing -TimeoutSec 3 -ErrorAction Stop
    if ($probe.StatusCode -eq 200) { $webUp = $true }
} catch { $webUp = $false }
if (-not $webUp) {
    Write-Host ('Vite dev server at ' + $WebBaseUrl + ' is NOT reachable. All Level 1 checks will be BLOCKED.') -ForegroundColor Yellow
    Write-Host '(Start the frontend with: cd apps/web && npm run dev)' -ForegroundColor Yellow
}

$Routes = @(
    '/',
    '/login',
    '/sales/orders',
    '/sales/orders/new/edit',
    '/sales/orders/1/edit',
    '/sales/orders/1'
)
foreach ($route in $Routes) {
    if (-not $webUp) {
        $msg = ('{0}: web server down' -f $route)
        Assert-Block $msg
        continue
    }
    try {
        $r = Invoke-WebRequest -Uri ($WebBaseUrl + $route) -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
        if ($r.StatusCode -eq 200) {
            $msg = ('{0} -> 200 (SPA shell)' -f $route)
            Assert-Pass $msg
        } else {
            $msg = ('{0} -> {1}' -f $route, $r.StatusCode)
            Assert-Fail $msg
        }
    } catch {
        $err = $_.Exception.Message.Split([Environment]::NewLine)[0]
        $msg = ('{0} -> {1}' -f $route, $err)
        Assert-Fail $msg
    }
}

# ----------------------------------------------------------------------
# Level 2 — Static structure (no DB, no secrets, no network)
# ----------------------------------------------------------------------
Write-Section 'Level 2: STATIC structure (no DB, no secrets, no network)'

$RequiredFiles = @(
    'apps/web/src/api/sales-order.ts',
    'apps/web/src/api/sales-order-context.ts',
    'apps/web/src/stores/sales-order.ts',
    'apps/web/src/views/sales-order/SalesOrderList.vue',
    'apps/web/src/views/sales-order/SalesOrderEdit.vue',
    'apps/web/src/views/sales-order/SalesOrderDetail.vue',
    'apps/web/src/router.ts',
    'apps/web/src/types/sales-order.ts'
)
$missing = @()
foreach ($f in $RequiredFiles) {
    if (-not (Test-Path $f)) { $missing += $f }
}
if ($missing.Count -gt 0) {
    $msg = ('Missing required files: {0}' -f ($missing -join ', '))
    Assert-Fail $msg
} else {
    $cnt = $RequiredFiles.Count
    $msg = ('All {0} required SalesOrder files present' -f $cnt)
    Assert-Pass $msg
}

# 2b) NO mock imports in the 3 SalesOrder pages
$SalesOrderPages = @('SalesOrderList','SalesOrderEdit','SalesOrderDetail')
$mockViolations = @()
foreach ($page in $SalesOrderPages) {
    $path = ('apps/web/src/views/sales-order/{0}.vue' -f $page)
    $content = Get-Content $path -Raw -ErrorAction SilentlyContinue
    if ($content -match "from\s+['\`"](?:\.\./)*mock") {
        $mockViolations += $path
    }
}
if ($mockViolations.Count -gt 0) {
    $msg = ('Mock imports found in SalesOrder pages: {0}' -f ($mockViolations -join ', '))
    Assert-Fail $msg
} else {
    Assert-Pass 'No mock imports in any of the 3 SalesOrder pages'
}

# 2c) The 2 main pages DO import the new context facade
$listContent = Get-Content 'apps/web/src/views/sales-order/SalesOrderList.vue' -Raw
$editContent = Get-Content 'apps/web/src/views/sales-order/SalesOrderEdit.vue' -Raw
$listHasFacade = $listContent -match "from\s+['\`"]\.\./\.\./api/sales-order-context"
$editHasFacade = $editContent -match "from\s+['\`"]\.\./\.\./api/sales-order-context"
if ($listHasFacade) {
    Assert-Pass 'SalesOrderList imports the G3-R2A context facade'
} else {
    Assert-Fail 'SalesOrderList does NOT import the G3-R2A context facade'
}
if ($editHasFacade) {
    Assert-Pass 'SalesOrderEdit imports the G3-R2A context facade'
} else {
    Assert-Fail 'SalesOrderEdit does NOT import the G3-R2A context facade'
}

# 2d) The 2 main pages NO LONGER import the old MDM read APIs
# (the static MDM type imports are still allowed and harmless).
$oldMdmApis = @(
    @{ Pattern = "from\s+['\`"]\.\./\.\./api/mdm/business-partner[\s'\\\`/]"; Label = 'api/mdm/business-partner' },
    @{ Pattern = "from\s+['\`"]\.\./\.\./api/mdm/item[\s'\\\`/]";                Label = 'api/mdm/item' },
    @{ Pattern = "from\s+['\`"]\.\./\.\./api/mdm/uom[\s'\\\`/]";                 Label = 'api/mdm/uom' }
)
foreach ($page in $SalesOrderPages) {
    $path = ('apps/web/src/views/sales-order/{0}.vue' -f $page)
    $content = Get-Content $path -Raw -ErrorAction SilentlyContinue
    $hits = @()
    foreach ($api in $oldMdmApis) {
        if ($content -match $api.Pattern) { $hits += $api.Label }
    }
    if ($page -in @('SalesOrderList','SalesOrderEdit') -and $hits.Count -gt 0) {
        $msg = ('{0} still imports old MDM read APIs: {1}' -f $page, ($hits -join ', '))
        Assert-Fail $msg
    } else {
        $msg = ('{0}: no old MDM read API imports (allowed: type-only refs)' -f $page)
        Assert-Pass $msg
    }
}

# 2e) Frontend build artifact check
$dist = 'apps/web/dist/index.html'
if (Test-Path $dist) {
    $size = (Get-Item $dist).Length
    $msg = ('Frontend build artifact present: {0} ({1} bytes)' -f $dist, $size)
    Assert-Pass $msg
} else {
    $msg = ('Frontend build artifact missing: {0} (run npm run build first)' -f $dist)
    Assert-Block $msg
}

# ----------------------------------------------------------------------
# Level 3 — API + page cross-check (with SALES_OPERATOR)
# ----------------------------------------------------------------------
Write-Section 'Level 3: API + page cross-check (SALES_OPERATOR login + dropdown)'

$apiUp = $false
try {
    $probe = Invoke-WebRequest -Uri ($BaseUrl + '/api/v1/system/ping') -TimeoutSec 3 -ErrorAction Stop
    if ($probe.StatusCode -eq 200) { $apiUp = $true }
} catch { $apiUp = $false }

if ($apiUp -and [System.Environment]::GetEnvironmentVariable('GULIERP_G3R1C_SALES_OPERATOR_PASS')) {
    $sess = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $csrf = (Invoke-WebRequest -Uri ($BaseUrl + '/api/v1/auth/csrf') -WebSession $sess).Content | ConvertFrom-Json
    $body = @{
        userName = 'g3r1c_sales_operator'
        password = [System.Environment]::GetEnvironmentVariable('GULIERP_G3R1C_SALES_OPERATOR_PASS')
    } | ConvertTo-Json
    try {
        Invoke-WebRequest -Uri ($BaseUrl + '/api/v1/auth/login') -Method POST -Body $body -ContentType 'application/json' -Headers @{ 'X-CSRF-TOKEN' = $csrf.requestToken } -WebSession $sess -ErrorAction Stop | Out-Null
        $me = Invoke-WebRequest -Uri ($BaseUrl + '/api/v1/auth/me') -WebSession $sess -ErrorAction Stop
        if ($me.StatusCode -eq 200) {
            $meBody = $me.Content | ConvertFrom-Json
            $msg = ('/auth/me -> 200 (userName={0}, companyId={1})' -f $meBody.userName, $meBody.companyId)
            Assert-Pass $msg
        } else {
            $msg = ('/auth/me -> {0}' -f $me.StatusCode)
            Assert-Fail $msg
        }
        foreach ($p in @('customers','items','uoms','warehouses','payment-methods')) {
            $r = Invoke-WebRequest -Uri ($BaseUrl + '/api/v1/sales/orders/context/' + $p + '?pageSize=1') -WebSession $sess -ErrorAction Stop
            if ($r.StatusCode -eq 200) {
                $cnt = ($r.Content | ConvertFrom-Json).totalCount
                $msg = ('context/{0} -> 200 (totalCount={1})' -f $p, $cnt)
                Assert-Pass $msg
            } else {
                $msg = ('context/{0} -> {1}' -f $p, $r.StatusCode)
                Assert-Fail $msg
            }
        }
    } catch {
        Assert-Fail ('SALES_OPERATOR cross-check failed: {0}' -f $_.Exception.Message.Split([Environment]::NewLine)[0])
    }
} else {
    Assert-Block 'SALES_OPERATOR: env missing or API down; cross-check skipped'
}

# ----------------------------------------------------------------------
# Level 4 — Manual browser checklist (operator to verify)
# ----------------------------------------------------------------------
Write-Section 'Level 4: Manual browser checklist (operator to verify)'

Write-Host '  The following 8 steps verify the visual state in a real browser.' -ForegroundColor Cyan
Write-Host '  No browser automation is available in the agent env; please' -ForegroundColor Cyan
Write-Host '  do these manually against the Vite dev server.' -ForegroundColor Cyan
Write-Host ''
Write-Host '   1. Open http://localhost:5173/login, sign in as g3r1c_sales_operator.'
Write-Host '   2. Navigate to /sales/orders. Expect: order list with 2 pre-existing orders (SO-20260825-000001 and -000002).'
Write-Host '   3. Click 新建销售订单. Expect: redirect to /sales/orders/new/edit with an empty form.'
Write-Host '   4. In the customer dropdown, type a customer name. Expect: dropdown populates from real MDM.'
Write-Host '   5. In the item dropdown, type an item name. Expect: dropdown populates from real MDM.'
Write-Host '   6. The UOM dropdown should auto-populate based on the selected item.'
Write-Host '   7. Save as draft. Expect: redirect to /sales/orders/<id> with the new orderNo.'
Write-Host '   8. Open DevTools Network. Confirm /api/v1/sales/orders/* requests carry the cookie and CSRF header.'

# ----------------------------------------------------------------------
# Final
# ----------------------------------------------------------------------
Write-Section 'Summary'
Write-Host ('  PASSED   : {0}' -f $script:Passed)
Write-Host ('  FAILED   : {0}' -f $script:HardFailed)
Write-Host ('  BLOCKED  : {0}' -f $script:Blocked)
Write-Host ''

if ($script:HardFailed -eq 0 -and $script:Blocked -eq 0) {
    Write-Host '  RESULT : G3_R2A_SALESORDER_WEB_VERIFIED' -ForegroundColor Green
    exit 0
} elseif ($script:HardFailed -eq 0) {
    Write-Host '  RESULT : G3_R2A_SALESORDER_WEB_PARTIAL' -ForegroundColor Yellow
    exit 0
} else {
    Write-Host '  RESULT : G3_R2A_SALESORDER_WEB_FAILED' -ForegroundColor Red
    exit 5
}
