#requires -Version 5.1
<#
.SYNOPSIS
    G2-MDM Runtime Acceptance — Read-only diagnostic for admin role
    binding & duplicate Role analysis.

.DESCRIPTION
    Answers the 4 non-secret diagnostic questions for the runtime
    authorization failure on /api/v1/mdm/dictionary-types:

        ADMIN_BINDING                  <role codes bound to admin>
        ADMIN_HAS_MDM_OPERATOR         YES / NO
        ADMIN_HAS_DICTIONARY_PERMS     YES / NO  (mdm.dictionary.read + .manage)
        DUPLICATE_MDM_OPERATOR         count of ERP_MDM_OPERATOR rows
                                       (grouped by TenantId)

    The script is READ-ONLY. It never INSERTs, UPDATEs, or DELETEs.
    It runs SQL queries via psql (read-only session) and emits a
    structured report at docs/verification/ADMIN_ROLE_BINDING_REPORT.md.

    This script is a NEW diagnostic tool (not a modification to
    production code). It does not touch the production code path.

    Schema references:
      - identity.AspNetUsers (Id, UserName, NormalizedUserName,
        Email, ..., TenantId, IsPlatformAdmin, Status)
      - identity.AspNetRoles (Id, Name, NormalizedName, ...,
        TenantId, Code, IsSystem, Description, Status)
      - identity.AspNetRoleClaims (Id, RoleId, ClaimType, ClaimValue)
          where ClaimType = 'gulierp.permission'
      - identity.gulierp_user_company_membership (Id, TenantId,
        CompanyId, UserId, IsDefault, Status)
      - identity.gulierp_user_role_assignment (Id, TenantId, UserId,
        RoleId, CompanyId, Status, ...)  -- THE actual role
        assignment table used by GuliERP.
      - identity.AspNetUserRoles (UserId, RoleId) -- the default
        ASP.NET Core Identity join table; in GuliERP this is empty
        and the real bindings live in gulierp_user_role_assignment.

.PARAMETER UserName
    The userName to diagnose. Default = admin.
    Accepted: 'admin' / 'guli_admin'.

.PARAMETER ConnectionString
    Optional. If not provided, the script reads
    $env:ConnectionStrings__GuliERP.

.PARAMETER OutputDir
    Where to write the binding report.
    Default = docs/verification (relative to repo root).

.EXAMPLE
    PS> $env:ConnectionStrings__GuliERP = '<canonical gulierp_g2_003_test conn string>'
        .\diagnose-admin-role-binding.ps1
#>
[CmdletBinding()]
param(
    [ValidateSet('admin', 'guli_admin')]
    [string]$UserName = 'admin',
    [string]$ConnectionString,
    [string]$OutputDir
)

$ErrorActionPreference = 'Stop'
Set-Location -Path (Join-Path $PSScriptRoot '..\..')

$ACCEPTED_USERS = @('admin', 'guli_admin')
if ($ACCEPTED_USERS -notcontains $UserName) {
    throw "SAFETY: UserName must be one of: $($ACCEPTED_USERS -join ', '). Got '$UserName'."
}

$PSQL_CANDIDATES = @(
    # EDB installer default paths (Program Files)
    'C:\Program Files\PostgreSQL\17\bin\psql.exe',
    'C:\Program Files\PostgreSQL\16\bin\psql.exe',
    'C:\Program Files\PostgreSQL\15\bin\psql.exe',
    'C:\Program Files\PostgreSQL\14\bin\psql.exe',
    'C:\Program Files\PostgreSQL\13\bin\psql.exe',
    # Green/zip installs under common dev locations
    'D:\tools\pg17\pgsql\bin\psql.exe',
    'D:\tools\pg16\pgsql\bin\psql.exe',
    'D:\tools\pg15\pgsql\bin\psql.exe',
    'D:\tools\pg14\pgsql\bin\psql.exe',
    'D:\tools\pg13\pgsql\bin\psql.exe',
    'D:\apps\pg17\pgsql\bin\psql.exe',
    'D:\apps\pg16\pgsql\bin\psql.exe',
    'D:\postgres\pgsql\bin\psql.exe',
    'D:\pg17\pgsql\bin\psql.exe',
    'D:\pg16\pgsql\bin\psql.exe'
)
$PSQL = $null
foreach ($p in $PSQL_CANDIDATES) {
    if (Test-Path $p) { $PSQL = $p; break }
}
if (-not $PSQL) {
    $PSQL = (Get-Command psql.exe -ErrorAction SilentlyContinue)?.Source
}
if (-not $PSQL) {
    throw "psql.exe not found. Set PATH or install PostgreSQL client."
}

if (-not $ConnectionString) {
    $ConnectionString = $env:ConnectionStrings__GuliERP
}
if (-not $ConnectionString) {
    throw "ConnectionString is required (or set `$env:ConnectionStrings__GuliERP)."
}

$ASSERT_SCRIPT = Join-Path $PSScriptRoot 'assert-gulierp-db-target.ps1'
if (Test-Path $ASSERT_SCRIPT) {
    & $ASSERT_SCRIPT
    if ($LASTEXITCODE -ne 0) {
        throw 'Wrong-DB detected in pre-check. Aborting.'
    }
}

if (-not $OutputDir) {
    $OutputDir = Join-Path (Get-Location) 'docs\verification'
}
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

$redactedConn = ($ConnectionString -replace '(?i)(Password|Pwd)\s*=\s*[^;]+', '$1=<REDACTED>')

# --------------------------------------------------------------------------
# Translate the Npgsql-format connection string (PascalCase keys
# Host= / Database= / Username= / Password=) into the libpq env vars
# (PGHOST / PGPORT / PGDATABASE / PGUSER / PGPASSWORD) that the
# native psql client understands. After this, psql is invoked WITHOUT
# a positional connection string; it picks the values from env.
# This is the safest cross-platform way to feed the user's existing
# ConnectionStrings__GuliERP (which is Npgsql format) into psql.
# --------------------------------------------------------------------------
function Get-PsqlEnvFromNpgsqlConn {
    param([string]$s)
    # NOTE: avoid using $host / $UserName as local variable names —
    # they collide with PowerShell's read-only automatic variables.
    $hostName = Parse-NpgsqlKeyStandalone -s $s -key 'Host'
    $portNum  = Parse-NpgsqlKeyStandalone -s $s -key 'Port'
    $dbName   = Parse-NpgsqlKeyStandalone -s $s -key 'Database'
    $userName = Parse-NpgsqlKeyStandalone -s $s -key 'Username'
    $pwd      = Parse-NpgsqlKeyStandalone -s $s -key 'Password'
    # NOTE: PGPASSWORD is the canonical libpq var for non-interactive
    # password input. It is process-local; we wipe it after the run.
    if ($hostName) { $env:PGHOST     = $hostName }
    if ($portNum)  { $env:PGPORT     = $portNum } else { $env:PGPORT = '5432' }
    if ($dbName)   { $env:PGDATABASE = $dbName }
    if ($userName) { $env:PGUSER     = $userName }
    if ($pwd)      { $env:PGPASSWORD = $pwd }
}

# Standalone (non-nested) helper. PowerShell's nested-function
# scoping caused the helper to be invisible to the parent; pulling
# it out to script scope fixes that.
function Parse-NpgsqlKeyStandalone {
    param([string]$s, [string]$key)
    $m = [regex]::Match($s, '(?i)(?:^|;)\s*' + [regex]::Escape($key) + '\s*=\s*([^;]+)')
    if ($m.Success) { return $m.Groups[1].Value.Trim() } else { return $null }
}

Get-PsqlEnvFromNpgsqlConn -s $ConnectionString

function Invoke-PsqlReadOnly {
    param([string]$Sql, [string]$Tag)
    # Connection comes from PG* env vars; we pass NO positional conn
    # string to psql (libpq style).
    $result = & $PSQL -X -A -t -F '|' --pset pager=off --pset footer=off -c $Sql 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "psql query failed ($Tag): $result"
    }
    return $result
}

# --- SQL queries (read-only) ----------------------------------------------
$QUERIES = [ordered]@{
    'Q1_USER' = @"
SELECT u.`"Id`", u.`"UserName`", u.`"TenantId`", u.`"Status`", u.`"IsPlatformAdmin`"
FROM identity.`"AspNetUsers`" u
WHERE u.`"UserName`" = '$UserName';
"@
    'Q2_USER_COMPANY' = @"
SELECT m.`"Id`", m.`"TenantId`", m.`"CompanyId`", m.`"IsDefault`", m.`"Status`"
FROM identity.`"gulierp_user_company_membership`" m
JOIN identity.`"AspNetUsers`" u ON u.`"Id`" = m.`"UserId`"
WHERE u.`"UserName`" = '$UserName' AND m.`"Status`" = 1;
"@
    'Q3_MDM_OPERATOR_ROLES' = @"
SELECT r.`"Id`", r.`"TenantId`", r.`"Code`", r.`"Name`", r.`"NormalizedName`",
       r.`"IsSystem`", r.`"Status`", r.`"CreatedAt`"
FROM identity.`"AspNetRoles`" r
WHERE r.`"Code`" = 'ERP_MDM_OPERATOR'
ORDER BY r.`"TenantId`", r.`"Id`";
"@
    'Q4_DUPLICATE_CHECK' = @"
SELECT `"TenantId`", COUNT(*) AS dup_count
FROM identity.`"AspNetRoles`"
WHERE `"Code`" = 'ERP_MDM_OPERATOR'
GROUP BY `"TenantId`" ORDER BY `"TenantId`";
"@
    'Q5_ROLE_CLAIMS' = @"
SELECT rc.`"RoleId`", r.`"TenantId`", rc.`"ClaimType`", rc.`"ClaimValue`"
FROM identity.`"AspNetRoleClaims`" rc
JOIN identity.`"AspNetRoles`" r ON r.`"Id`" = rc.`"RoleId`"
WHERE r.`"Code`" = 'ERP_MDM_OPERATOR'
ORDER BY r.`"TenantId`", rc.`"RoleId`", rc.`"ClaimType`", rc.`"ClaimValue`";
"@
    'Q6_USER_ASSIGNMENTS' = @"
SELECT a.`"Id`", a.`"TenantId`", a.`"CompanyId`", a.`"UserId`", a.`"RoleId`",
       r.`"Code`" AS role_code, a.`"Status`"
FROM identity.`"gulierp_user_role_assignment`" a
JOIN identity.`"AspNetUsers`" u ON u.`"Id`" = a.`"UserId`"
JOIN identity.`"AspNetRoles`" r ON r.`"Id`" = a.`"RoleId`"
WHERE u.`"UserName`" = '$UserName'
ORDER BY a.`"Id`";
"@
    'Q7_LEGACY_USER_ROLES' = @"
SELECT ur.`"UserId`", ur.`"RoleId`", r.`"Code`" AS role_code
FROM identity.`"AspNetUserRoles`" ur
JOIN identity.`"AspNetUsers`" u ON u.`"Id`" = ur.`"UserId`"
JOIN identity.`"AspNetRoles`" r ON r.`"Id`" = ur.`"RoleId`"
WHERE u.`"UserName`" = '$UserName';
"@
    'Q8_DICTIONARY_CLAIMS_ON_ADMIN_ROLES' = @"
SELECT DISTINCT r.`"Id`" AS role_id, r.`"TenantId`", r.`"Code`", rc.`"ClaimValue`"
FROM identity.`"AspNetRoles`" r
JOIN identity.`"gulierp_user_role_assignment`" a ON a.`"RoleId`" = r.`"Id`"
JOIN identity.`"AspNetUsers`" u ON u.`"Id`" = a.`"UserId`"
LEFT JOIN identity.`"AspNetRoleClaims`" rc
       ON rc.`"RoleId`" = r.`"Id`" AND rc.`"ClaimType`" = 'gulierp.permission'
WHERE u.`"UserName`" = '$UserName'
  AND r.`"Code`" = 'ERP_MDM_OPERATOR'
ORDER BY r.`"Id`", rc.`"ClaimValue`";
"@
    'Q9_ALL_CLAIMS_ADMIN_ROLES' = @"
SELECT r.`"Id`" AS role_id, r.`"Code`", r.`"TenantId`",
       COALESCE(string_agg(rc.`"ClaimValue`", ';' ORDER BY rc.`"ClaimValue`"), '<none>') AS claims
FROM identity.`"AspNetRoles`" r
JOIN identity.`"gulierp_user_role_assignment`" a ON a.`"RoleId`" = r.`"Id`"
JOIN identity.`"AspNetUsers`" u ON u.`"Id`" = a.`"UserId`"
LEFT JOIN identity.`"AspNetRoleClaims`" rc
       ON rc.`"RoleId`" = r.`"Id`" AND rc.`"ClaimType`" = 'gulierp.permission'
WHERE u.`"UserName`" = '$UserName'
  AND r.`"Code`" = 'ERP_MDM_OPERATOR'
GROUP BY r.`"Id`", r.`"Code`", r.`"TenantId`";
"@
    'Q10_SYSTEM_ADMIN_CHECK' = @"
SELECT a.`"RoleId`", r.`"Code`"
FROM identity.`"gulierp_user_role_assignment`" a
JOIN identity.`"AspNetUsers`" u ON u.`"Id`" = a.`"UserId`"
JOIN identity.`"AspNetRoles`" r ON r.`"Id`" = a.`"RoleId`"
WHERE u.`"UserName`" = '$UserName' AND a.`"Status`" = 1
ORDER BY r.`"Code`";
"@
    'Q11_TENANT_LIST' = @"
SELECT `"Id`", `"Code`", `"Name`", `"Status`" FROM identity.`"gulierp_tenant`" ORDER BY `"Id`";
"@
}

# Run all queries, collect results.
# (PG* env vars already set by Get-PsqlEnvFromNpgsqlConn above.)
$results = [ordered]@{}
foreach ($key in $QUERIES.Keys) {
    $sql = $QUERIES[$key]
    Write-Host "[$key] running..."
    $out = Invoke-PsqlReadOnly -Sql $sql -Tag $key
    $results[$key] = $out
    Write-Host "  -> $($out.Count) row(s)"
}
# Wipe all PG* env vars we set.
foreach ($k in @('PGHOST', 'PGPORT', 'PGDATABASE', 'PGUSER', 'PGPASSWORD')) {
    Remove-Item "Env:$k" -ErrorAction SilentlyContinue
}

# Try to read the current branch / HEAD for the report header.
$branch = 'master'
$head = 'unknown'
try {
    $branch = (& git rev-parse --abbrev-ref HEAD 2>$null).Trim()
    $head   = (& git rev-parse --short HEAD 2>$null).Trim()
} catch {}

$ts = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss zzz')

# --- Render ADMIN_ROLE_BINDING_REPORT.md --------------------------------
$reportPath = Join-Path $OutputDir 'ADMIN_ROLE_BINDING_REPORT.md'
$report = @"
# ADMIN_ROLE_BINDING_REPORT

## 0. Run Metadata

- Repo:    D:\guli\projects\gulierp-next
- Branch:  $branch
- HEAD:    $head
- Date:    $ts
- User:    $UserName
- DB conn: $redactedConn
- Mode:    READ-ONLY (psql -X -A -t, no writes)
- Tool:    tools/dev/diagnose-admin-role-binding.ps1

## 1. Q1 — Admin User Identity

~~~
$($results['Q1_USER'] -join "`n")
~~~

## 2. Q2 — Active UserCompanyMembership

~~~
$($results['Q2_USER_COMPANY'] -join "`n")
~~~

## 3. Q3 — All ERP_MDM_OPERATOR Roles (across all tenants)

~~~
$($results['Q3_MDM_OPERATOR_ROLES'] -join "`n")
~~~

## 4. Q4 — Duplicate Check (count by TenantId)

~~~
$($results['Q4_DUPLICATE_CHECK'] -join "`n")
~~~

## 5. Q5 — RoleClaims on all ERP_MDM_OPERATOR Roles

~~~
$($results['Q5_ROLE_CLAIMS'] -join "`n")
~~~

## 6. Q6 — Admin's UserRoleAssignments (gulierp_user_role_assignment)

~~~
$($results['Q6_USER_ASSIGNMENTS'] -join "`n")
~~~

## 7. Q7 — Admin's legacy AspNetUserRoles (should be empty in GuliERP)

~~~
$($results['Q7_LEGACY_USER_ROLES'] -join "`n")
~~~

## 8. Q8 — Dictionary Claims on Admin's MDM Roles (focused)

~~~
$($results['Q8_DICTIONARY_CLAIMS_ON_ADMIN_ROLES'] -join "`n")
~~~

## 9. Q9 — All Claims on Admin's MDM Roles (consolidated)

~~~
$($results['Q9_ALL_CLAIMS_ADMIN_ROLES'] -join "`n")
~~~

## 10. Q10 — Admin's active role codes (full)

~~~
$($results['Q10_SYSTEM_ADMIN_CHECK'] -join "`n")
~~~

## 11. Q11 — All Tenants (for cross-tenant duplicate interpretation)

~~~
$($results['Q11_TENANT_LIST'] -join "`n")
~~~

## 12. Findings (TO BE FILLED after this run)

- Admin user exists? (YES / NO)
- Admin is bound to which role code(s)? (list, from Q10)
- Admin's role has `mdm.dictionary.read`? (YES / NO, from Q8 / Q9)
- Admin's role has `mdm.dictionary.manage`? (YES / NO, from Q8 / Q9)
- Duplicate `ERP_MDM_OPERATOR` in same TenantId? (count, from Q4)

## 13. Interpretation Notes (Reference)

- If Q4 shows count > 1 for the formal Tenant (id 83727350616817890,
  the value of `FormalTenantId` in Bootstrap/Program.cs:90), the
  existing `--ensure-formal-enterprise-business-role-pack` is
  BLOCKED. The Provisioner's `EnsureRolePackAsync` throws
  `InvalidOperationException("Duplicate role 'ERP_MDM_OPERATOR'
  exists in tenant {tenantId}.")`
  (see `modules/identity/.../EnterpriseBusinessRolePackProvisioner.cs:113-117`).
- The two role IDs reported by the operator
  (83727350616817910 / 83727350616817820) are 990 apart, suggesting
  they were created in the same Snowflake worker run; consistent with
  a previous bootstrap retry that left a residue role behind.
- Q8 and Q9 are the only queries that actually answer the
  authorization question; the rest are context.
- The legacy `AspNetUserRoles` table is expected to be empty in
  GuliERP because the canonical join is `gulierp_user_role_assignment`.
  If Q7 returns rows, that would be a separate anomaly.

## 14. Raw JSON Dump (machine-readable)

~~~json
$( ($results | ConvertTo-Json -Depth 5) )
~~~
"@

Set-Content -Path $reportPath -Value $report -Encoding UTF8
Write-Host `"
Write-Host "Wrote: $reportPath"
Write-Host `"

# Print a one-screen summary.
Write-Host "============================================"
Write-Host "  ADMIN_ROLE_BINDING — ON-SCREEN SUMMARY"
Write-Host "============================================"
foreach ($key in $QUERIES.Keys) {
    Write-Host "[$key]"
    foreach ($line in $results[$key]) {
        Write-Host "  $line"
    }
    Write-Host `"
}
