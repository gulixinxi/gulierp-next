# GULIERP_MDM_FOUNDATION_REUSE_WAVE_V1_SCHEMA_AND_OPERATOR_CLOSURE
# (2026-08-30) — Operator precheck for MDM007
# (LocationWarehouseScopedCodeUniqueness).
#
# PURPOSE
#   The Reuse Wave adds Foundation-driven per-Warehouse auto-code
#   for Location. The engine generates e.g. Warehouse A: LOC_000001
#   AND Warehouse B: LOC_000001 in parallel. The V1 schema uniqueness
#   is (TenantId, CompanyId, Code), which would REJECT the second
#   insert. The fix is MDM007: drop the old index, create the new
#   warehouse-scoped index.
#
#   This script does the data-collision precheck that MDM007
#   requires. It scans the production DB for any (TenantId,
#   CompanyId, WarehouseId, Code) row with count > 1, which would
#   be a true duplicate that the new constraint would block.
#
# USAGE
#   pwsh tools/dev/gulierp-mdm007-location-warehouse-precheck.ps1
#     -ConnectionString "<postgres-conn-string>"
#
# EXIT CODES
#   0 = PASS — no intra-Warehouse duplicate Locations
#   1 = BLOCK — at least one (TenantId, CompanyId, WarehouseId,
#       Code) appears more than once in mdm.gulierp_location
#   2 = ERROR — connection failed, query failed, etc.
#
# RATIONALE (per brief §七)
#   Cross-Warehouse same Code (Warehouse A: LOC_000001 +
#   Warehouse B: LOC_000001) is NOT a collision — the new index
#   permits it by design. Only INTRA-Warehouse duplicate Codes
#   block MDM007.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ConnectionString
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

# SQL: count rows sharing the (TenantId, CompanyId, WarehouseId, Code)
# scope. A count > 1 means two rows in the SAME Warehouse carry the
# SAME Code — a true duplicate the new unique index would reject.
$Query = @"
SELECT
    "TenantId",
    "CompanyId",
    "WarehouseId",
    "Code",
    COUNT(*) AS "DuplicateCount"
FROM "mdm"."gulierp_location"
GROUP BY "TenantId", "CompanyId", "WarehouseId", "Code"
HAVING COUNT(*) > 1
ORDER BY "DuplicateCount" DESC, "TenantId", "CompanyId", "WarehouseId", "Code";
"@

Write-Host "[MDM007 PRECHECK] Connecting to PostgreSQL..." -ForegroundColor Cyan
Write-Host "[MDM007 PRECHECK] Scanning mdm.gulierp_location for intra-Warehouse Code duplicates..." -ForegroundColor Cyan

try {
    # Prefer psql if available
    $psql = (Get-Command psql -ErrorAction SilentlyContinue)
    if ($psql) {
        $result = & psql $ConnectionString -tA -c $Query 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Host "[MDM007 PRECHECK] ERROR: psql exited with code $LASTEXITCODE" -ForegroundColor Red
            Write-Host $result
            exit 2
        }
    }
    else {
        # Fallback: use Npgsql via dotnet-script (if present) or
        # report. We intentionally do NOT bundle a heavy
        # connection client in this script — Operator should
        # have psql available.
        Write-Host "[MDM007 PRECHECK] ERROR: psql not found on PATH. Install PostgreSQL client tools." -ForegroundColor Red
        exit 2
    }
}
catch {
    Write-Host "[MDM007 PRECHECK] ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 2
}

if ([string]::IsNullOrWhiteSpace($result)) {
    Write-Host "[MDM007 PRECHECK] PASS — no intra-Warehouse duplicate Locations." -ForegroundColor Green
    Write-Host "[MDM007 PRECHECK] Safe to apply MDM007_LocationWarehouseScopedCodeUniqueness." -ForegroundColor Green
    exit 0
}

Write-Host "[MDM007 PRECHECK] BLOCK — found at least one (TenantId, CompanyId, WarehouseId, Code) duplicate:" -ForegroundColor Red
Write-Host ""
Write-Host "  TenantId | CompanyId | WarehouseId | Code         | Count"
Write-Host "  ---------+-----------+-------------+--------------+------"
foreach ($line in $result) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    $cols = $line -split '\|' | ForEach-Object { $_.Trim() }
    if ($cols.Count -lt 5) { continue }
    Write-Host ("  {0,-8} | {1,-9} | {2,-11} | {3,-12} | {4}" -f $cols[0], $cols[1], $cols[2], $cols[3], $cols[4])
}
Write-Host ""
Write-Host "[MDM007 PRECHECK] LOCATION_EXISTING_COLLISION_BLOCKER" -ForegroundColor Red
Write-Host "[MDM007 PRECHECK] Do NOT apply MDM007. Operator must:" -ForegroundColor Red
Write-Host "  1. Investigate the duplicates listed above." -ForegroundColor Red
Write-Host "  2. Either: de-duplicate the Locations by hand" -ForegroundColor Red
Write-Host "     (no automatic renumber per brief §三十三)" -ForegroundColor Red
Write-Host "  3. Re-run this precheck until it returns PASS." -ForegroundColor Red
Write-Host "  4. Only then apply MDM007." -ForegroundColor Red
exit 1
