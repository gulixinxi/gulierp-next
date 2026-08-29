#requires -Version 5.1
<#
.SYNOPSIS
    Download the operator-side CN administrative-region snapshot for
    GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1.

.DESCRIPTION
    Fetches the official MCA National Geographical Names Database
    administrative-region JSON and writes it under
    artifacts/operator/mdm-foundation/. The JSON data itself remains
    operator evidence and is not intended to be committed.

    The script also writes a manifest containing source URL, retrieval
    timestamp, SHA-256 checksum, and node count. This gives the runtime
    acceptance harness a concrete, repeatable input without inventing
    or hand-writing region data.

    Licensing / redistribution note:
      This script records the official source and checksum. It does not
      assert redistribution rights for committing the dataset. Operator
      review is still required before promoting the downloaded snapshot
      into a committed seed package.
#>
[CmdletBinding()]
param(
    [string]$SourceUrl = 'http://dmfw.mca.gov.cn/9095/xzqh/getList?code=&maxLevel=3',
    [string]$OutputPath = 'artifacts\operator\mdm-foundation\mca-cn.json',
    [string]$ManifestPath = 'artifacts\operator\mdm-foundation\mca-cn.manifest.json'
)

$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

function Resolve-RepoPath {
    param([Parameter(Mandatory=$true)][string]$Path)
    if ([System.IO.Path]::IsPathRooted($Path)) { return $Path }
    return Join-Path $RepoRoot $Path
}

function Count-RegionNodes {
    param($Node)
    if ($null -eq $Node) { return 0 }
    $count = 0
    if ($Node.code -and $Node.code -ne '00' -and $Node.code -ne '资料暂缺') {
        $count++
    }
    if ($Node.children) {
        foreach ($child in $Node.children) {
            $count += Count-RegionNodes -Node $child
        }
    }
    return $count
}

$resolvedOutput = Resolve-RepoPath -Path $OutputPath
$resolvedManifest = Resolve-RepoPath -Path $ManifestPath
$outDir = Split-Path -Parent $resolvedOutput
if (-not (Test-Path -LiteralPath $outDir)) {
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
}

$headers = @{
    'Accept' = 'application/json, text/plain, */*'
    'Referer' = 'http://dmfw.mca.gov.cn/'
}
$response = Invoke-WebRequest `
    -Uri $SourceUrl `
    -Headers $headers `
    -UserAgent 'Mozilla/5.0 GuliERP-Operator-Evidence' `
    -UseBasicParsing `
    -TimeoutSec 60 `
    -ErrorAction Stop
if ($response.StatusCode -ne 200) {
    throw "MCA snapshot request returned HTTP $($response.StatusCode)."
}

$json = $response.Content | ConvertFrom-Json
if ($null -eq $json.data -or $json.data.code -ne '00') {
    throw 'MCA snapshot did not contain the expected data root with code=00.'
}

$nodeCount = Count-RegionNodes -Node $json.data
if ($nodeCount -le 0) {
    throw 'MCA snapshot did not contain any administrative-region nodes.'
}

Set-Content -LiteralPath $resolvedOutput -Value $response.Content -Encoding utf8
$hash = Get-FileHash -LiteralPath $resolvedOutput -Algorithm SHA256

$manifest = [ordered]@{
    dataset = 'mca-cn-administrative-region'
    source = 'Ministry of Civil Affairs National Geographical Names Database'
    sourceUrl = $SourceUrl
    standard = 'GB/T 2260-compatible administrative division code hierarchy'
    maxLevel = 3
    retrievedAt = (Get-Date).ToUniversalTime().ToString('O')
    recordCount = $nodeCount
    checksumAlgorithm = 'SHA-256'
    checksum = $hash.Hash
    outputFile = $resolvedOutput
    # GULIERP_MASTER_DATA_FOUNDATION_OPERATOR_REFERENCE_DATA_AND_RUNTIME_ACCEPTANCE_V1
    # (Wave 5.2 final): V1 policy is the OPERATOR_IMPORT_MODEL.
    # The raw dataset is consumed under artifacts/operator/... and
    # is NOT committed to the GuliERP repo. Redistributing the
    # full raw dataset to a third party is permanently
    # DO_NOT_REDISTRIBUTE_RAW_MCA_DATA and does not require
    # additional review to maintain.
    redistributionStatus = 'OPERATOR_IMPORT_MODEL'
    redistributionApproval = 'NOT_REQUIRED_BY_V1_POLICY'
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $resolvedManifest -Encoding utf8

Write-Host "MCA CN snapshot downloaded." -ForegroundColor Green
Write-Host "  Output: $resolvedOutput"
Write-Host "  Manifest: $resolvedManifest"
Write-Host "  Record count: $nodeCount"
Write-Host "  SHA-256: $($hash.Hash)"
