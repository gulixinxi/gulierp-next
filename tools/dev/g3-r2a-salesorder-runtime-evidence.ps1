#requires -Version 5.1
<#
G3-R2A SalesOrder Runtime Acceptance — Evidence Script.

Validates the SalesOrder end-to-end runtime:
  - 6 main CRUD endpoints (List / Get / CreateDraft / UpdateDraft / Confirm / Cancel)
  - 5 context facade endpoints (customers / items / uoms / warehouses / payment-methods)
  - 4 role-perspective checks + 1 anonymous check
  - SALES_OPERATOR is the only role that gets 200 on SalesOrder endpoints
    (per the G3-R1C boundary contract)

The script uses the 4 dedicated single-role test users from G3-R1C
(g3r1c_sys_admin / g3r1c_mdm_operator / g3r1c_employee_operator /
g3r1c_sales_operator) and the V1 validation rule that codes/prefixes
must be PURE ASCII LETTERS (no digits, no underscores, no hyphens).

For each role, the script:
  1. Logs in.
  2. Tests all 6 main endpoints with role-appropriate expectations.
  3. Tests all 5 context facade endpoints (SALES_OPERATOR only;
     other roles are covered in negative-check below).
  4. Returns the verified state.

For SALES_OPERATOR, a full Draft -> Read -> Update -> Confirm
state machine cycle is exercised. The created test order is
cancelled at the end (V1 has no DELETE endpoint; cancel is
the cleanup path).

Test data is tagged with the \`GRTHREERUNTWO\` prefix (mapped
from the G3-R2A brief's \`g3r2a_\` marker through the V1
letter-only validation rule).

ENVIRONMENT VARIABLES (only required for Level 2+):
  GULIERP_TEST_BASE_URL                  e.g. http://127.0.0.1:5001 (default)
  GULIERP_G3R1C_SYS_ADMIN_USER / _PASS
  GULIERP_G3R1C_MDM_OPERATOR_USER / _PASS
  GULIERP_G3R1C_EMPLOYEE_OPERATOR_USER / _PASS
  GULIERP_G3R1C_SALES_OPERATOR_USER / _PASS
  ConnectionStrings__GuliERP            NOT NEEDED (the script is HTTP-only)
#>

[CmdletBinding()]
param(
    [string]$BaseUrl = 'http://127.0.0.1:5001'
)

$ErrorActionPreference = 'Stop'

if ($env:GULIERP_TEST_BASE_URL) { $BaseUrl = $env:GULIERP_TEST_BASE_URL }

function Write-Section { param([string]$Title)
    Write-Host ''
    Write-Host ('=' * 70) -ForegroundColor Cyan
    Write-Host "  $Title" -ForegroundColor Cyan
    Write-Host ('=' * 70) -ForegroundColor Cyan
}
function Assert-Pass    { param([string]$Message) Write-Host "  PASS    $Message" -ForegroundColor Green; $script:Passed++ }
function Assert-Fail    { param([string]$Message) Write-Host "  FAIL    $Message" -ForegroundColor Red;    $script:HardFailed++ }
function Assert-Block   { param([string]$Message) Write-Host "  BLOCK   $Message" -ForegroundColor Yellow; $script:Blocked++ }
function Assert-Created { param([string]$Message) Write-Host "  CREATED $Message" -ForegroundColor Green; $script:Passed++ }
function Assert-Updated { param([string]$Message) Write-Host "  UPDATED $Message" -ForegroundColor Green; $script:Passed++ }
function Assert-Cleaned { param([string]$Message) Write-Host "  CLEANED $Message" -ForegroundColor DarkGray; $script:Passed++ }

$script:HardFailed = 0
$script:Blocked = 0
$script:Passed = 0

# Digit-to-letter map (V1 codes must be pure ASCII letters).
$digitToLetter = @{ '0'='A';'1'='B';'2'='C';'3'='D';'4'='E';'5'='F';'6'='G';'7'='H';'8'='I';'9'='J' }
function New-TestTag {
    $ts = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
    $tag = ''
    foreach ($c in $ts.ToString().ToCharArray()) { $tag += $digitToLetter[$c.ToString()] }
    # GRTHREERUNTWO + 6 ts letters
    return "GRTHREERUNTWO$tag"
}

# Common login helper
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

function Get-StatusCode {
    param($Session, [string]$Url, [string]$Method = 'GET', $Body = $null)
    $headers = @{}
    if ($Session -and $Method -in @('POST','PUT','PATCH','DELETE')) {
        $csrf = (Invoke-WebRequest -Uri "$BaseUrl/api/v1/auth/csrf" -WebSession $Session -ErrorAction SilentlyContinue).Content
        if ($csrf) {
            try { $headers['X-CSRF-TOKEN'] = ($csrf | ConvertFrom-Json).requestToken } catch {}
        }
    }
    if ($headers.Count -gt 0) { $hCopy = $headers } else { $hCopy = @{} }
    $args = @{ Uri = $Url; Method = $Method; TimeoutSec = 8; ErrorAction = 'Stop' }
    if ($Session) { $args.WebSession = $Session }
    if ($hCopy.Count -gt 0) { $args.Headers = $hCopy }
    if ($null -ne $Body) { $args.Body = $Body; $args.ContentType = 'application/json' }
    $code = 0
    try {
        $resp = Invoke-WebRequest @args
        $code = [int]$resp.StatusCode
    } catch {
        $err = $_.Exception.Message.Split([Environment]::NewLine)[0]
        if ($err -match '(\d{3})') { $code = [int]$matches[1] }
    }
    return $code
}

# Probe API
$apiUp = $false
try {
    $probe = Invoke-WebRequest -Uri "$BaseUrl/api/v1/system/ping" -TimeoutSec 3 -ErrorAction Stop
    if ($probe.StatusCode -eq 200) { $apiUp = $true }
} catch { $apiUp = $false }
if (-not $apiUp) {
    Write-Host "API at $BaseUrl is NOT reachable. All Level 2+ checks will be BLOCKED." -ForegroundColor Yellow
}

# Role env-var mapping
$RoleEnv = [ordered]@{
    'ERP_SYSTEM_ADMIN'      = @{ User = 'GULIERP_G3R1C_SYS_ADMIN_USER';         Pass = 'GULIERP_G3R1C_SYS_ADMIN_PASS';         DefaultUser = 'g3r1c_sys_admin' }
    'ERP_MDM_OPERATOR'      = @{ User = 'GULIERP_G3R1C_MDM_OPERATOR_USER';      Pass = 'GULIERP_G3R1C_MDM_OPERATOR_PASS';      DefaultUser = 'g3r1c_mdm_operator' }
    'ERP_EMPLOYEE_OPERATOR' = @{ User = 'GULIERP_G3R1C_EMPLOYEE_OPERATOR_USER'; Pass = 'GULIERP_G3R1C_EMPLOYEE_OPERATOR_PASS'; DefaultUser = 'g3r1c_employee_operator' }
    'ERP_SALES_OPERATOR'    = @{ User = 'GULIERP_G3R1C_SALES_OPERATOR_USER';    Pass = 'GULIERP_G3R1C_SALES_OPERATOR_PASS';    DefaultUser = 'g3r1c_sales_operator' }
}

function Get-RoleSession {
    param([string]$Role)
    $envU = $RoleEnv[$Role].User
    $envP = $RoleEnv[$Role].Pass
    $defUser = $RoleEnv[$Role].DefaultUser
    $user = if ([string]::IsNullOrEmpty([System.Environment]::GetEnvironmentVariable($envU))) { $defUser } else { [System.Environment]::GetEnvironmentVariable($envU) }
    $pass = [System.Environment]::GetEnvironmentVariable($envP)
    if ([string]::IsNullOrEmpty($pass)) { return $null }
    return New-Object psobject -Property @{
        User = $user
        Pass = $pass
        Session = (Get-LoginSession -User $user -Pass $pass -Url $BaseUrl)
    }
}

# ----------------------------------------------------------------------
# Level 1 — Endpoint availability (anonymous 401, not 404)
# ----------------------------------------------------------------------
Write-Section 'Level 1: Endpoint availability (anonymous 401, not 404)'

$EndPoints = @(
    @{ Name = 'SalesOrder list';        Method = 'GET';    Url = "$BaseUrl/api/v1/sales/orders?pageSize=1" },
    @{ Name = 'SalesOrder by id';       Method = 'GET';    Url = "$BaseUrl/api/v1/sales/orders/1" },
    @{ Name = 'SalesOrder create';      Method = 'POST';   Url = "$BaseUrl/api/v1/sales/orders" },
    @{ Name = 'SalesOrder update';      Method = 'PUT';    Url = "$BaseUrl/api/v1/sales/orders/1" },
    @{ Name = 'SalesOrder confirm';     Method = 'POST';   Url = "$BaseUrl/api/v1/sales/orders/1/confirm" },
    @{ Name = 'SalesOrder cancel';      Method = 'POST';   Url = "$BaseUrl/api/v1/sales/orders/1/cancel" },
    @{ Name = 'Context customers';      Method = 'GET';    Url = "$BaseUrl/api/v1/sales/orders/context/customers?pageSize=1" },
    @{ Name = 'Context items';          Method = 'GET';    Url = "$BaseUrl/api/v1/sales/orders/context/items?pageSize=1" },
    @{ Name = 'Context uoms';           Method = 'GET';    Url = "$BaseUrl/api/v1/sales/orders/context/uoms?pageSize=1" },
    @{ Name = 'Context warehouses';     Method = 'GET';    Url = "$BaseUrl/api/v1/sales/orders/context/warehouses?pageSize=1" },
    @{ Name = 'Context payment-methods';Method = 'GET';    Url = "$BaseUrl/api/v1/sales/orders/context/payment-methods?pageSize=1" }
)
foreach ($ep in $EndPoints) {
    $code = Get-StatusCode -Session $null -Url $ep.Url -Method $ep.Method -Body '{}'
    if ($code -eq 401) {
        Assert-Pass "$($ep.Name) → 401 anonymous (endpoint exists)"
    } else {
        Assert-Fail "$($ep.Name) → $code (expected 401; endpoint missing?)"
    }
}

# ----------------------------------------------------------------------
# Level 2 — SALES_OPERATOR full CRUD cycle + 5 context facades
# ----------------------------------------------------------------------
Write-Section 'Level 2: SALES_OPERATOR full CRUD cycle + 5 context facades'

$salesOp = Get-RoleSession 'ERP_SALES_OPERATOR'
if ($null -eq $salesOp -or -not $apiUp) {
    Assert-Block "SALES_OPERATOR: env missing or API down; cannot run"
} else {
    $sess = $salesOp.Session

    # -------- 5 context facades first (read path, no side effects) --------
    Write-Host ''
    Write-Host "  --- 5 context facade endpoints (read path) ---" -ForegroundColor Cyan
    foreach ($path in @('customers','items','uoms','warehouses','payment-methods')) {
        $code = Get-StatusCode -Session $sess -Url "$BaseUrl/api/v1/sales/orders/context/$path`?pageSize=1"
        if ($code -eq 200) {
            Assert-Pass "context/$path → 200 (SALES_OPERATOR can read)"
        } else {
            Assert-Fail "context/$path → $code (expected 200)"
        }
    }

    # -------- Look up real MDM ids (customer / item / uom) --------
    Write-Host ''
    Write-Host "  --- resolve real MDM ids ---" -ForegroundColor Cyan
    $custs = Invoke-WebRequest -Uri "$BaseUrl/api/v1/sales/orders/context/customers?pageSize=1" -WebSession $sess
    $cust = ($custs.Content | ConvertFrom-Json).items[0]
    $customerId = $cust.id
    $customerName = $cust.name
    Assert-Pass "Resolved customer: $customerName (id=$customerId)"

    $itemsR = Invoke-WebRequest -Uri "$BaseUrl/api/v1/sales/orders/context/items?pageSize=1" -WebSession $sess
    $item = ($itemsR.Content | ConvertFrom-Json).items[0]
    $itemId = $item.id
    $itemName = $item.name
    Assert-Pass "Resolved item: $itemName (id=$itemId)"

    # V1 slice: a line's uomId MUST be the item's baseUomId (the
    # SalesOrder service returns sales.invalid_uom otherwise). We
    # therefore resolve the UOM by the item's baseUomId, not by
    # picking the first UOM in the system.
    $uomId = $item.baseUomId
    $uomsR = Invoke-WebRequest -Uri "$BaseUrl/api/v1/sales/orders/context/uoms?keyword=&pageSize=200" -WebSession $sess
    $uom = ($uomsR.Content | ConvertFrom-Json).items | Where-Object { $_.id -eq $uomId } | Select-Object -First 1
    if (-not $uom) {
        Assert-Fail "Could not resolve the item's baseUomId (id=$uomId) in the UOM list"
    } else {
        Assert-Pass "Resolved uom (item's baseUom): $($uom.name) (id=$uomId)"
    }

    # -------- SalesOrder List (the operator sees existing orders) --------
    Write-Host ''
    Write-Host "  --- SalesOrder main endpoints ---" -ForegroundColor Cyan
    $code = Get-StatusCode -Session $sess -Url "$BaseUrl/api/v1/sales/orders?pageSize=10"
    if ($code -eq 200) { Assert-Pass "list → 200 (SALES_OPERATOR)" }
    else { Assert-Fail "list → $code" }

    # -------- Create --------
    Write-Host ''
    Write-Host "  --- Create ---" -ForegroundColor Cyan
    $tag = New-TestTag
    $remarks = "G3R2A test order $tag"
    $orderDate = (Get-Date).ToString('yyyy-MM-dd')
    $createBody = @{
        customerId = [long]$customerId
        orderDate = $orderDate
        requestedDeliveryDate = $null
        remarks = $remarks
        lines = @(
            @{
                itemId = [long]$itemId
                uomId = [long]$uomId
                quantity = 2.0000
                unitPrice = 10.00
                discountRate = 0
                taxRate = 0.13
                remarks = $null
            }
        )
    } | ConvertTo-Json -Depth 8
    $createUrl = "$BaseUrl/api/v1/sales/orders"
    $createResp = $null
    $createCode = 0
    try {
        $csrf = (Invoke-WebRequest -Uri "$BaseUrl/api/v1/auth/csrf" -WebSession $sess -ErrorAction SilentlyContinue).Content | ConvertFrom-Json
        $headers = @{ 'X-CSRF-TOKEN' = $csrf.requestToken }
        $resp = Invoke-WebRequest -Uri $createUrl -Method POST -Body $createBody -ContentType 'application/json' -Headers $headers -WebSession $sess -ErrorAction Stop
        $createCode = [int]$resp.StatusCode
        $createResp = $resp.Content | ConvertFrom-Json
    } catch {
        $err = $_.Exception.Message.Split([Environment]::NewLine)[0]
        if ($err -match '(\d{3})') { $createCode = [int]$matches[1] }
    }
    if ($createCode -eq 201) {
        $orderId = $createResp.id
        $orderNo = $createResp.orderNo
        Assert-Created "SalesOrder $orderNo (id=$orderId, customer=$customerName) → 201"
    } else {
        Assert-Fail "create → $createCode (expected 201)"
        $orderId = $null
    }

    if ($orderId) {
        # -------- Read --------
        $code = Get-StatusCode -Session $sess -Url "$BaseUrl/api/v1/sales/orders/$orderId"
        if ($code -eq 200) { Assert-Pass "get $orderId → 200" } else { Assert-Fail "get $orderId → $code" }

        # -------- Update --------
        Write-Host ''
        Write-Host "  --- Update ---" -ForegroundColor Cyan
        $cv = $createResp.concurrencyVersion
        $updateBody = @{
            customerId = [long]$customerId
            orderDate = $orderDate
            requestedDeliveryDate = $null
            remarks = "$remarks (updated)"
            expectedConcurrencyVersion = $cv
            lines = @(
                @{
                    itemId = [long]$itemId
                    uomId = [long]$uomId
                    quantity = 3.0000
                    unitPrice = 11.00
                    discountRate = 0
                    taxRate = 0.13
                    remarks = "updated line"
                }
            )
        } | ConvertTo-Json -Depth 8
        $code = Get-StatusCode -Session $sess -Url "$BaseUrl/api/v1/sales/orders/$orderId" -Method 'PUT' -Body $updateBody
        if ($code -eq 200) { Assert-Updated "update $orderId → 200 (concurrencyVersion=$cv)" }
        else { Assert-Fail "update $orderId → $code" }

        # -------- Confirm --------
        Write-Host ''
        Write-Host "  --- Confirm ---" -ForegroundColor Cyan
        $code = Get-StatusCode -Session $sess -Url "$BaseUrl/api/v1/sales/orders/$orderId/confirm" -Method 'POST' -Body '{}'
        if ($code -eq 200) { Assert-Updated "confirm $orderId → 200" }
        else { Assert-Fail "confirm $orderId → $code" }

        # -------- Cancel (cleanup: V1 has no DELETE) --------
        Write-Host ''
        Write-Host "  --- Cancel (cleanup) ---" -ForegroundColor Cyan
        $code = Get-StatusCode -Session $sess -Url "$BaseUrl/api/v1/sales/orders/$orderId/cancel" -Method 'POST' -Body '{}'
        if ($code -eq 200) { Assert-Cleaned "cancel $orderId → 200 (V1 cleanup path)" }
        else { Assert-Fail "cancel $orderId → $code" }
    }
}

# ----------------------------------------------------------------------
# Level 3 — Non-SALES_OPERATOR write paths (must return 403)
# ----------------------------------------------------------------------
Write-Section 'Level 3: Non-SALES_OPERATOR write paths (must return 403)'

$NonSalesRoles = @('ERP_SYSTEM_ADMIN','ERP_MDM_OPERATOR','ERP_EMPLOYEE_OPERATOR')
$SampleEndpoints = @(
    @{ Name = 'list';         Url = "$BaseUrl/api/v1/sales/orders?pageSize=1" },
    @{ Name = 'get-by-id';    Url = "$BaseUrl/api/v1/sales/orders/1" },
    @{ Name = 'create';       Url = "$BaseUrl/api/v1/sales/orders" },
    @{ Name = 'update';       Url = "$BaseUrl/api/v1/sales/orders/1" },
    @{ Name = 'confirm';      Url = "$BaseUrl/api/v1/sales/orders/1/confirm" },
    @{ Name = 'cancel';       Url = "$BaseUrl/api/v1/sales/orders/1/cancel" },
    @{ Name = 'context/customers'; Url = "$BaseUrl/api/v1/sales/orders/context/customers?pageSize=1" },
    @{ Name = 'context/items';     Url = "$BaseUrl/api/v1/sales/orders/context/items?pageSize=1" }
)
foreach ($role in $NonSalesRoles) {
    Write-Host ''
    Write-Host "  --- $role (negative) ---" -ForegroundColor Cyan
    $ctx = Get-RoleSession $role
    if ($null -eq $ctx) {
        Assert-Block ("{0}: env missing; cannot run" -f $role)
        continue
    }
    $sess = $ctx.Session
    foreach ($ep in $SampleEndpoints) {
        $method = if ($ep.Name -in @('list','get-by-id','context/customers','context/items')) { 'GET' }
                   elseif ($ep.Name -eq 'create') { 'POST' }
                   elseif ($ep.Name -eq 'update') { 'PUT' }
                   else { 'POST' }
        $code = Get-StatusCode -Session $sess -Url $ep.Url -Method $method -Body '{}'
        if ($code -eq 403) {
            Assert-Pass ("{0,-28} {1} → 403 (denied)" -f $role, $ep.Name)
        } elseif ($code -eq 401) {
            Assert-Fail ("{0,-28} {1} → 401 (session invalid?)" -f $role, $ep.Name)
        } else {
            Assert-Fail ("{0,-28} {1} → {2} (expected 403)" -f $role, $ep.Name, $code)
        }
    }
}

# ----------------------------------------------------------------------
# Level 4 — Final verdict
# ----------------------------------------------------------------------
Write-Section 'Summary'
Write-Host "  PASSED   : $($script:Passed)"
Write-Host "  FAILED   : $($script:HardFailed)"
Write-Host "  BLOCKED  : $($script:Blocked)"
Write-Host ''

if ($script:HardFailed -eq 0 -and $script:Blocked -eq 0) {
    Write-Host '  RESULT : G3_R2A_SALESORDER_RUNTIME_REBUILD_VERIFIED' -ForegroundColor Green
    exit 0
} elseif ($script:HardFailed -eq 0) {
    Write-Host '  RESULT : G3_R2A_SALESORDER_RUNTIME_REBUILD_PARTIAL' -ForegroundColor Yellow
    Write-Host '  (No failures; some checks BLOCKED due to env or API down)' -ForegroundColor Yellow
    exit 0
} else {
    Write-Host '  RESULT : G3_R2A_SALESORDER_RUNTIME_REBUILD_FAILED' -ForegroundColor Red
    exit 5
}
