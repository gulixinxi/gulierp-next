<#
.SYNOPSIS
  WEB-PREVIEW-001 backend launcher.
  Starts the GuliERP.Api against gulierp_g2_003_test with cookie auth + CSRF.
  Password is read via Read-Host -AsSecureString (NOT echoed / NOT stored).

.INSTRUCTIONS
  1. Open a NEW PowerShell window (your own shell, NOT the agent's).
  2. Run:
       powershell -ExecutionPolicy Bypass -File "d:\guli\projects\gulierp-next\tools\dev\run-web-preview-backend.ps1"
  3. When prompted, paste the PostgreSQL password (it will be masked).
  4. The script asserts DB=gulierp_g2_003_test, then starts dotnet on http://127.0.0.1:5000.
  5. Leave the window open. Tell the agent "backend up on :5000".

.NOTES
  - Password stays in process memory only; nothing is written to source/docs/logs.
  - To use a different DB user, pass -DbUser <name>. Default: gulidata (per G2-004).
  - To override the port, pass -Port <n>. Default: 5000.
#>
[CmdletBinding()]
param(
  [string]$DbUser = 'gulidata',
  [int]$Port = 5000,
  [string]$Dotnet = 'dotnet',
  [string]$Project,
  [string]$ExpectedDb = 'gulierp_g2_003_test'
)
$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $Project) {
  $Project = Join-Path $RepoRoot 'apps\api\GuliERP.Api\GuliERP.Api.csproj'
}
$ErrorActionPreference = 'Stop'

Write-Host '=== WEB-PREVIEW-001 backend launcher ===' -ForegroundColor Cyan
Write-Host "Target DB : $ExpectedDb @ 192.168.2.228"
Write-Host "DB user   : $DbUser  (override via -DbUser)"
Write-Host "Backend   : http://127.0.0.1:$Port"
Write-Host ''

# --- 1. Read password securely (masked, never echoed) ---
$sec = Read-Host -Prompt "PostgreSQL password for user '$DbUser'" -AsSecureString
if ($sec.Length -eq 0) { Write-Host 'Empty password — aborting.' -ForegroundColor Red; exit 2 }
$plain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
  [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec))

# --- 2. Build connection string in process memory only ---
$conn = "Host=192.168.2.228;Port=5432;Database=$ExpectedDb;Username=$DbUser;Password=$plain;Include Error Detail=true"
$env:ConnectionStrings__GuliERP = $conn
$env:GULIERP_ConnectionStrings__GuliERP = $conn
# Scrub the plaintext immediately; env var holds the only copy.
$plain = $null

# --- 3. Assert DB target (wrong-DB guard) ---
$assert = Join-Path $PSScriptRoot 'assert-gulierp-db-target.ps1'
& $assert -ConnectionString $conn -ExpectedDatabase $ExpectedDb
if ($LASTEXITCODE -ne 0) { Write-Host 'DB guard FAILED — aborting.' -ForegroundColor Red; exit 3 }

# --- 4. Start dotnet (foreground; Ctrl+C to stop) ---
Write-Host ''
Write-Host 'Starting backend...' -ForegroundColor Cyan
& $Dotnet run --project $Project --no-launch-profile --urls "http://127.0.0.1:$Port"

