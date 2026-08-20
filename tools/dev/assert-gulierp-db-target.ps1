<#
.SYNOPSIS
    Assert-GulierpDbTarget — Wrong-DB prevention guard.

.DESCRIPTION
    Parses a Npgsql connection string (current env var or supplied argument)
    and verifies that the Database name matches the canonical GuliERP Next
    Operator target. Default canonical target: gulierp_g2_003_test.

    On mismatch: FAIL CLOSED. Prints EXPECTED vs ACTUAL Database (never the
    password). Exit code 1.

    This script is INTENTIONALLY TINY. It is not a framework, not a wrapper
    around Npgsql. It is a string-level guard that runs in milliseconds and
    costs zero round-trips to the database.

.PARAMETER ConnectionString
    Optional Npgsql connection string to validate. If not provided, the
    script reads $env:ConnectionStrings__GuliERP. If still empty, FAIL.

.PARAMETER ExpectedDatabase
    Override the canonical target. Default: gulierp_g2_003_test.
    Used by future database cutover (Pre-G2-006 and beyond).

.PARAMETER ShowPassword
    INTENTIONALLY NOT IMPLEMENTED. Password is never extracted, never
    printed, never stored.

.EXAMPLE
    PS> .\tools\dev\assert-gulierp-db-target.ps1
    Uses $env:ConnectionStrings__GuliERP. Default target = gulierp_g2_003_test.

.EXAMPLE
    PS> .\tools\dev\assert-gulierp-db-target.ps1 -ExpectedDatabase gulierp_next_dev
    Override canonical target (for future cutover).

.EXAMPLE
    PS> .\tools\dev\assert-gulierp-db-target.ps1 -ConnectionString "Host=...;Database=...;Username=...;Password=***"
    Validate an explicit connection string.

.NOTES
    Goal: DB-HYGIENE-001
    Date: 2026-08-20
    Session: mvs_11a243eed8e544d6b19087711a392283
    Policy: read-only; no DB connect; no password printing.
#>
[CmdletBinding()]
param(
    [string]$ConnectionString,
    [string]$ExpectedDatabase = 'gulierp_g2_003_test'
)

$ErrorActionPreference = 'Stop'

function Parse-NpgsqlKey([string]$s, [string]$key) {
    # Matches ";Key=Value" or "Key=Value" at start, case-insensitive, semi-colon
    # terminated. Npgsql keys are case-insensitive but written PascalCase.
    $pattern = '(?i)(?:^|;)\s*' + [regex]::Escape($key) + '\s*=\s*([^;]+)'
    $m = [regex]::Match($s, $pattern)
    if ($m.Success) { return $m.Groups[1].Value.Trim() }
    return $null
}

function Redact([string]$s) {
    if ([string]::IsNullOrEmpty($s)) { return '<empty>' }
    # Truncate long strings; replace control chars; mask to ASCII-printable summary
    if ($s.Length -gt 80) { $s = $s.Substring(0, 80) + '...' }
    return $s
}

# -------------------------------------------------------------------
# Step 1: acquire connection string
# -------------------------------------------------------------------
if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    $ConnectionString = $env:ConnectionStrings__GuliERP
}

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    Write-Host "[FAIL] No connection string provided." -ForegroundColor Red
    Write-Host "       Set `-ConnectionString` or `$env:ConnectionStrings__GuliERP`."
    Write-Host "       EXPECTED DATABASE: $ExpectedDatabase"
    Write-Host "       ACTUAL DATABASE  : <no connection string>"
    exit 1
}

# -------------------------------------------------------------------
# Step 2: extract Host / Database / Username (NOT Password)
# -------------------------------------------------------------------
$hostName  = Parse-NpgsqlKey -s $ConnectionString -key 'Host'
$database  = Parse-NpgsqlKey -s $ConnectionString -key 'Database'
$username  = Parse-NpgsqlKey -s $ConnectionString -key 'Username'
# Password is intentionally NOT extracted.

if ([string]::IsNullOrEmpty($database)) {
    Write-Host "[FAIL] Connection string is missing the Database= key." -ForegroundColor Red
    Write-Host "       Connection: $(Redact $ConnectionString)"
    Write-Host "       EXPECTED DATABASE: $ExpectedDatabase"
    exit 1
}

# -------------------------------------------------------------------
# Step 3: assert
# -------------------------------------------------------------------
if ($database -eq $ExpectedDatabase) {
    Write-Host "[PASS] Host=$hostName Database=$database Username=$username (matches canonical target)"
    exit 0
} else {
    Write-Host "[FAIL] Wrong-DB detected." -ForegroundColor Red
    Write-Host "       EXPECTED DATABASE: $ExpectedDatabase"
    Write-Host "       ACTUAL DATABASE  : $database"
    Write-Host "       Host             : $hostName"
    Write-Host "       Username         : $username"
    Write-Host "       (Password is never printed. Set `$env:ConnectionStrings__GuliERP if not already set.)"
    Write-Host ""
    Write-Host "       To override the canonical target (e.g. for a future cutover),"
    Write-Host "       pass -ExpectedDatabase <name>. To update the canonical target"
    Write-Host "       project-wide, update docs/governance/DATABASE_TARGET_REGISTRY.md"
    Write-Host "       and tools/dev/assert-gulierp-db-target.ps1 together."
    exit 1
}
