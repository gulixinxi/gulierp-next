[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][ValidateSet('backend', 'frontend')][string]$Role,
    [Parameter(Mandatory = $true)][string]$RepoRoot,
    [string]$Dotnet,
    [string]$BackendCsproj,
    [int]$BackendPort = 5000,
    [string]$Npm,
    [string]$WebDir,
    [int]$FrontendPort = 5173,
    [string]$BackendUrl = 'http://127.0.0.1:5000'
)

$ErrorActionPreference = 'Stop'

function Write-Log {
    param([string]$Message)
    $line = "[{0}] {1}" -f (Get-Date).ToString('o'), $Message
    if ($env:GULIERP_STACK_LOG) {
        Add-Content -LiteralPath $env:GULIERP_STACK_LOG -Value $line -Encoding UTF8
    }
    Write-Host $Message
}

function Invoke-AndLog {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [Parameter(Mandatory = $true)][string]$WorkingDirectory
    )
    Set-Location -LiteralPath $WorkingDirectory
    Write-Log ("Starting {0} {1}" -f $FilePath, ($Arguments -join ' '))
    & $FilePath @Arguments 2>&1 | ForEach-Object {
        $text = [string]$_
        if ($env:GULIERP_STACK_LOG) {
            Add-Content -LiteralPath $env:GULIERP_STACK_LOG -Value $text -Encoding UTF8
        }
        Write-Host $text
    }
    exit $LASTEXITCODE
}

if ($Role -eq 'backend') {
    Write-Log '=== GuliERP.Api ==='
    Write-Log ("Repo    : {0}" -f $RepoRoot)
    Write-Log ("URL     : http://127.0.0.1:{0}" -f $BackendPort)
    Write-Log 'Secret  : ConnectionStrings__GuliERP=redacted'
    Invoke-AndLog -FilePath $Dotnet -Arguments @(
        'run',
        '--project', $BackendCsproj,
        '-c', 'Release',
        '--no-build',
        '--no-launch-profile',
        '--urls', "http://127.0.0.1:$BackendPort"
    ) -WorkingDirectory $RepoRoot
}

if ($Role -eq 'frontend') {
    Write-Log '=== GuliERP Web (Vite) ==='
    Write-Log ("Repo : {0}" -f $RepoRoot)
    Write-Log ("URL  : http://127.0.0.1:{0}" -f $FrontendPort)
    Write-Log ("API  : {0}" -f $BackendUrl)
    Invoke-AndLog -FilePath $Npm -Arguments @(
        '--prefix', $WebDir,
        'run', 'dev',
        '--',
        '--host', '127.0.0.1',
        '--port', ([string]$FrontendPort),
        '--strictPort'
    ) -WorkingDirectory $WebDir
}
