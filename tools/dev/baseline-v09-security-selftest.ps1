# Offline regression checks. Loads only formatter AST nodes, never operator script bodies.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$cases = @(
    'Host=sentinel-host;Database=sentinel-db;Username=sentinel-user;Password=sentinel-secret;',
    'HOST = sentinel-host;Pwd = "sentinel-secret;tail";',
    "Password='sentinel-secret`nmultiline-tail';Host=sentinel-host;",
    'User ID=sentinel-user;Password="sentinel-secret',
    'PGPASSWORD=sentinel-secret'
)
$checks = 0
foreach ($relative in @(
    'tools/dev/assert-gulierp-db-target.ps1',
    'tools/dev/Mdm001Acceptance.Harness.ps1',
    'tools/dev/mdm-001-operator-evidence.ps1',
    'tools/dev/g2-004-operator-evidence.ps1',
    'tools/dev/g2-005-operator-evidence.ps1'
)) {
    $tokens = $null; $parseErrors = $null
    $ast = [System.Management.Automation.Language.Parser]::ParseFile((Join-Path $repo $relative), [ref]$tokens, [ref]$parseErrors)
    if ($parseErrors.Count) { throw "Parser errors: $relative" }
    $functions = $ast.FindAll({ param($n)
        $n -is [System.Management.Automation.Language.FunctionDefinitionAst] -and
        $n.Name -in @('Redact', 'Redact-ConnectionString', 'Redact-SecretText')
    }, $true)
    if (-not $functions.Count) { throw "No formatter found: $relative" }
    foreach ($function in $functions) {
        foreach ($sample in $cases) {
            $result = & {
                param($definition, $functionName, $inputValue)
                . ([scriptblock]::Create($definition))
                & $functionName $inputValue
            } $function.Extent.Text $function.Name $sample
            if ($result -match 'sentinel-|multiline-tail|;tail' -or $result -notlike '*Password=***;*') {
                throw "Redaction failed: $relative / $($function.Name) (values suppressed)"
            }
            $checks++
        }
    }
}
foreach ($relative in @(
    'tools/dev/g2-001-operator-evidence.ps1',
    'tools/dev/g2-003-operator-evidence.ps1',
    'tools/dev/g2-004-bootstrap-operator-user.ps1',
    'tools/dev/provision-web-preview-user.ps1',
    'tools/dev/g2-004-operator-evidence.ps1'
)) {
    $tokens = $null; $parseErrors = $null
    $ast = [System.Management.Automation.Language.Parser]::ParseFile((Join-Path $repo $relative), [ref]$tokens, [ref]$parseErrors)
    if ($parseErrors.Count) { throw "Parser errors: $relative" }
    $assignment = $ast.Find({ param($n)
        $n -is [System.Management.Automation.Language.AssignmentStatementAst] -and
        $n.Left.Extent.Text -eq '$displayConn'
    }, $true)
    if (-not $assignment) { throw "Missing connection summary: $relative" }
    $result = & { param($statement)
        $ConnectionString = 'Password=sentinel-secret;'; $conn = $ConnectionString
        . ([scriptblock]::Create($statement))
        $displayConn
    } $assignment.Extent.Text
    if ($result -ne 'Host=***;Database=***;Username=***;Password=***;') {
        throw "Unsafe connection summary: $relative"
    }
    $checks++
}
# Execute only the DB target guard: it parses strings and does not connect to a database.
$pwsh = (Get-Process -Id $PID).Path
foreach ($case in @(
    @{ Connection = 'Host=sentinel-host;Username=sentinel-user;Password=sentinel-secret;'; Exit = 1 },
    @{ Connection = 'Host=sentinel-host;Database=sentinel-db;Username=sentinel-user;Password=sentinel-secret;'; Exit = 0 },
    @{ Connection = 'Host=sentinel-host;Database=sentinel-wrong;Username=sentinel-user;Password=sentinel-secret;'; Exit = 1 }
)) {
    $captured = & $pwsh -NoProfile -File (Join-Path $PSScriptRoot 'assert-gulierp-db-target.ps1') -ConnectionString $case.Connection -ExpectedDatabase 'sentinel-db' 2>&1 | Out-String
    if ($LASTEXITCODE -ne $case.Exit -or $captured -match 'sentinel-') {
        throw 'DB guard status/redaction regression (values suppressed)'
    }
    $checks++
}
Write-Output "BASELINE_SECURITY_SELFTEST_PASS checks=$checks; no database connection performed"
