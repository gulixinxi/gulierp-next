$ErrorActionPreference = 'Continue'

$root = Split-Path -Parent $PSScriptRoot
$results = [ordered]@{}

function Invoke-Step {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][scriptblock]$Command
    )

    Write-Host "== $Name =="
    & $Command
    $exit = $LASTEXITCODE
    if ($null -eq $exit) { $exit = 0 }
    $results[$Name] = $exit
    Write-Host "$Name exit code: $exit"
}

Invoke-Step "backend build" {
    dotnet build "$root/apps/api/GuliERP.Api/GuliERP.Api.csproj"
}

Invoke-Step "backend tests" {
    dotnet test "$root/tests/GuliERP.Foundation.Tests/GuliERP.Foundation.Tests.csproj"
}

Invoke-Step "frontend install" {
    Push-Location "$root/apps/web"
    npm install
    Pop-Location
}

Invoke-Step "frontend build" {
    Push-Location "$root/apps/web"
    npm run build
    Pop-Location
}

Invoke-Step "git diff check" {
    Push-Location $root
    git diff --check
    Pop-Location
}

Write-Host ""
Write-Host "VERIFY SUMMARY"
foreach ($item in $results.GetEnumerator()) {
    $status = if ($item.Value -eq 0) { "PASS" } else { "FAIL" }
    Write-Host ("{0}: {1}" -f $item.Key, $status)
}

if ($results.Values | Where-Object { $_ -ne 0 }) {
    exit 1
}

exit 0

