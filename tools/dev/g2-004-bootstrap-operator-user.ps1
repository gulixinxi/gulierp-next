#requires -Version 5.1
<#
.SYNOPSIS
    G2-004 — Secure bootstrap of the operator-evidence test user.

.DESCRIPTION
    The IdentitySeed.SeedAsync is dev-only and is NEVER called in
    Production. The operator-evidence script (g2-004-operator-evidence.ps1)
    needs a real, password-hashed, Tenant-membership-having User
    on the real Production-shape test database. This script is
    the secure bootstrap path.

    The script:
      1. Reads the Npgsql connection string (env var OR prompt).
         The password is NEVER echoed; it is masked in any log.
      2. Reads the operator username (must carry the test_operator_
         marker prefix; the tool refuses anything else).
      3. Reads the password via Read-Host -AsSecureString. The
         SecureString is converted to a plain string only at the
         point of piping to the .NET tool's STDIN, then wiped
         from memory as soon as the .NET tool exits.
      4. Invokes the .NET bootstrap tool (tools/GuliERP.Identity.Bootstrap)
         which:
            - uses ASP.NET Core Identity UserManager.CreateAsync
              (PBKDF2 password hashing; no custom hash)
            - ensures Tenant + Company + UserCompanyMembership
            - is idempotent (re-running resets the password for
              an existing marker-prefixed user)
            - refuses to touch any user / tenant / company
              WITHOUT the marker prefix (defense)
      5. Returns a JSON object on stdout. The password is NEVER
         echoed; the hash lives only in the database.

    The script is intended to be run ONCE per database (or after
    the operator's password rotation policy).

.PARAMETER ConnectionString
    Optional. If not provided, the script reads
    $env:ConnectionStrings__GuliERP or
    $env:GULIERP_FOUNDATION_CONNECTION.

.PARAMETER UserName
    Optional. Defaults to 'test_operator_g2_004'. MUST start with
    'test_operator_' (defense — see brief).

.PARAMETER TenantCode
    Optional. Defaults to 'test_operator_g2_004_t'. MUST start with
    'test_operator_'.

.PARAMETER CompanyCode
    Optional. Defaults to 'test_operator_g2_004_c'. MUST start with
    'test_operator_'.

.EXAMPLE
    PS> .\g2-004-bootstrap-operator-user.ps1
    (interactive: paste connection string + password)

.EXAMPLE
    PS> $env:ConnectionStrings__GuliERP = "Host=192.168.2.228;...;Password=***"
    PS> .\g2-004-bootstrap-operator-user.ps1 -SkipPrompt
    (reads password via Read-Host -AsSecureString)
#>
[CmdletBinding()]
param(
    [string]$ConnectionString,
    [string]$UserName = 'test_operator_g2_004',
    [string]$TenantCode = 'test_operator_g2_004_t',
    [string]$CompanyCode = 'test_operator_g2_004_c',
    [switch]$SkipPrompt
)

$ErrorActionPreference = 'Stop'
Set-Location -Path (Join-Path $PSScriptRoot '..\..')

$DOTNET = 'D:\guli\gulierp\.dotnet\dotnet.exe'
if (-not (Test-Path $DOTNET)) {
    throw "G2-004 requires D:\guli\gulierp\.dotnet\dotnet.exe. Not found."
}

$BOOTSTRAP_PROJECT = Join-Path $PSScriptRoot '..\GuliERP.Identity.Bootstrap\GuliERP.Identity.Bootstrap.csproj'

# --- 0. Resolve connection string ----------------------------------------
if (-not $ConnectionString) {
    $ConnectionString = $env:ConnectionStrings__GuliERP
}
if (-not $ConnectionString) {
    $ConnectionString = $env:GULIERP_FOUNDATION_CONNECTION
}
if (-not $ConnectionString) {
    if ($SkipPrompt) {
        throw 'No connection string. Set $env:ConnectionStrings__GuliERP or pass -ConnectionString.'
    }
    $ConnectionString = Read-Host -Prompt 'Npgsql connection string (Host=...;Port=...;Database=...;Username=...;Password=...)'
}

$displayConn = ($ConnectionString -replace 'Password=[^;]+', 'Password=***')
Write-Host "[G2-004] Using connection: $displayConn" -ForegroundColor Cyan

# --- 1. Read username / tenant / company (with marker guards) ------------
# (The .NET tool also enforces the marker; the PS guard is for
# fast-feedback before the build.)
foreach ($pair in @(@('UserName', $UserName), @('TenantCode', $TenantCode), @('CompanyCode', $CompanyCode))) {
    $name = $pair[0]; $val = $pair[1]
    if (-not $val.StartsWith('test_operator_', [System.StringComparison]::Ordinal)) {
        throw "SAFETY: $name='$val' must start with 'test_operator_'."
    }
}

# --- 2. Read password (SecureString) -------------------------------------
$securePwd = $null
try {
    if ($SkipPrompt) {
        $securePwd = Read-Host -Prompt 'Operator test user password' -AsSecureString
    } else {
        $securePwd = Read-Host -Prompt 'Operator test user password' -AsSecureString
    }
}
catch {
    throw "Failed to read password: $($_.Exception.Message)"
}
if ($null -eq $securePwd -or $securePwd.Length -lt 1) {
    throw "Empty password. Aborting."
}

# Convert SecureString to plain string ONLY at the point of
# piping to the .NET tool's STDIN. The plain string lives in
# a local variable; we wipe it as soon as the .NET tool exits.
$plainPwd = $null
try {
    $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePwd)
    try {
        $plainPwd = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
    }
    finally {
        [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($BSTR)
    }

    # Build the .NET tool args.
    # G2-004V1R3 fix: do NOT use `$args` as a local variable; it
    # is a PowerShell automatic variable. Use `$dotnetArgs`.
    $dotnetArgs = @(
        'run', '--project', $BOOTSTRAP_PROJECT,
        '-c', 'Release', '--no-restore',
        '--', $ConnectionString, $UserName, $TenantCode, $CompanyCode
    )

    # Invoke the .NET tool with the password on STDIN.
    # ProcessStartInfo.RedirectStandardInput = true. The .NET
    # tool reads password from Console.In.
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $DOTNET
    foreach ($a in $dotnetArgs) { $psi.ArgumentList.Add($a) }
    $psi.RedirectStandardInput = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true

    $proc = New-Object System.Diagnostics.Process
    $proc.StartInfo = $psi

    # G2-004V1R4 fix: PROCESS IO DEADLOCK PREVENTION.
    # The .NET bootstrap tool emits a LOT of diagnostic logs
    # (now routed to stderr via StderrLoggerProvider — every
    # EF Core / Identity / Bootstrap info line). On Windows
    # the redirected stderr pipe buffer is ~4 KB. If the
    # parent does NOT drain stderr while the child runs, the
    # child blocks on its next stderr write, never reaches
    # the final JSON on stdout, and never exits. The parent
    # in turn is doing WaitForExit or a blocking ReadToEnd on
    # stdout, and the two deadlock. The classic pattern is:
    #   1. Start the process
    #   2. CONCURRENTLY kick off ReadToEndAsync on stdout
    #      AND stderr so the parent is draining both pipes
    #   3. Write the password to stdin, flush, close stdin
    #   4. WaitForExit (the read tasks are concurrently
    #      draining the pipes; WaitForExit is now safe)
    #   5. Await the read tasks to get the final strings
    # The read tasks are Task<string> from the async stream
    # readers; we use GetAwaiter().GetResult() to bridge
    # them into PowerShell's synchronous flow.
    $started = $proc.Start()
    if (-not $started) {
        Write-Host '[G2-004V1R4] Failed to start bootstrap process.' -ForegroundColor Red
        exit 7
    }

    # Concurrently drain BOTH pipes. The read tasks are
    # running on the .NET threadpool, so this does NOT
    # block the caller.
    $stdoutTask = $proc.StandardOutput.ReadToEndAsync()
    $stderrTask = $proc.StandardError.ReadToEndAsync()

    # Write the password + newline to STDIN, then flush
    # and close. The .NET bootstrap tool reads from
    # Console.In; flushing ensures the bytes are actually
    # in the pipe before the tool's ReadToEndAsync waits.
    $proc.StandardInput.WriteLine($plainPwd)
    $proc.StandardInput.Flush()
    $proc.StandardInput.Close()

    # Defensive timeout: 60 seconds. If the bootstrap tool
    # hangs (e.g. DB unreachable, EF migration loop), we
    # kill ONLY this script-owned bootstrap PID. We NEVER
    # touch other .NET dev processes on the workstation.
    $bootstrapTimeoutMs = 60000
    $exited = $proc.WaitForExit($bootstrapTimeoutMs)
    if (-not $exited) {
        try {
            Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
        } catch {}
        Write-Host "[G2-004V1R4] BOOTSTRAP_PROCESS_TIMEOUT ($bootstrapTimeoutMs ms). Killed bootstrap PID $($proc.Id). Inspect the .NET tool's stderr for the cause." -ForegroundColor Red
        # Try to collect whatever stderr was buffered before
        # the kill. (After Stop-Process, ReadToEndAsync may
        # complete quickly because the pipe is closed.)
        $stderr = ''
        try { $stderr = $stderrTask.GetAwaiter().GetResult() } catch {}
        if ($stderr) { Write-Host "  --- stderr (partial) ---" -ForegroundColor Red; Write-Host $stderr -ForegroundColor Red }
        exit 7
    }

    # Drain the read tasks. They are already complete
    # because the child has exited, but we await them
    # anyway to retrieve the strings.
    $stdout = $stdoutTask.GetAwaiter().GetResult()
    $stderr = $stderrTask.GetAwaiter().GetResult()

    if ($proc.ExitCode -ne 0) {
        Write-Host '[G2-004V1R4] Bootstrap FAILED.' -ForegroundColor Red
        if ($stderr) { Write-Host $stderr -ForegroundColor Red }
        exit $proc.ExitCode
    }

    # G2-004V1R3 fix: parse the final machine-readable JSON from
    # stdout. The .NET bootstrap tool routes ALL diagnostic
    # logging to stderr (LogToStandardErrorThreshold = Trace),
    # so stdout is reserved for the final JSON. Even so, this
    # wrapper is defensive: it tries the whole stdout first;
    # if that fails (mixed content, trailing whitespace,
    # accidental log line), it falls back to a line-by-line
    # search for the LAST line that is a valid JSON object
    # with the expected shape (ok=true, userName, userId,
    # markerPrefix). The fallback never silently swallows
    # errors; on failure it exits with a clear message.
    $parsed = $null
    $parseError = $null
    try {
        $parsed = $stdout | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        $parseError = $_.Exception.Message
        # Fallback: find the last line that ConvertFrom-Json
        # can parse AND has the expected shape.
        $candidates = $stdout -split "(`r`n|`n|`r)"
        for ($i = $candidates.Count - 1; $i -ge 0; $i--) {
            $line = $candidates[$i].Trim()
            if (-not $line) { continue }
            if ($line[0] -ne '{') { continue }
            try {
                $cand = $line | ConvertFrom-Json -ErrorAction Stop
                if ($cand.ok -eq $true -and $cand.userName -and $cand.userId -and $cand.markerPrefix) {
                    $parsed = $cand
                    break
                }
            } catch {}
        }
    }
    if ($null -eq $parsed) {
        Write-Host '[G2-004] Bootstrap OK exit but stdout is not parseable as JSON.' -ForegroundColor Red
        if ($parseError) { Write-Host "  First attempt: $parseError" -ForegroundColor Red }
        Write-Host '  --- stdout ---' -ForegroundColor Red
        Write-Host $stdout -ForegroundColor Red
        Write-Host '  --- stderr ---' -ForegroundColor Red
        Write-Host $stderr -ForegroundColor Red
        exit 7
    }
    if ($parsed.ok -ne $true) {
        Write-Host "[G2-004] Bootstrap OK exit but JSON ok != true. Output: $($parsed | Out-String)" -ForegroundColor Red
        exit 7
    }
    Write-Host '[G2-004] Bootstrap OK.' -ForegroundColor Green
    Write-Host "  userName       = $($parsed.userName)"
    Write-Host "  userId         = $($parsed.userId)"
    Write-Host "  tenantCode     = $($parsed.tenantCode)"
    Write-Host "  tenantId       = $($parsed.tenantId)"
    Write-Host "  companyCode    = $($parsed.companyCode)"
    Write-Host "  companyId      = $($parsed.companyId)"
    Write-Host "  markerPrefix   = $($parsed.markerPrefix)"
    Write-Host "  password       = (hashed by Identity PBKDF2; not echoed)"
    Write-Host ''
    Write-Host 'Next: run the operator evidence pack:'
    Write-Host '  PS> .\tools\dev\g2-004-operator-evidence.ps1 -SkipPrompt'

    # Emit the canonical final JSON line on stdout for
    # downstream scripting (e.g. g2-004-operator-evidence.ps1
    # can pipe it). We emit the re-serialized compact form so
    # downstream consumers always get a single line.
    $finalJson = $parsed | ConvertTo-Json -Compress
    Write-Output $finalJson
    exit 0
}
finally {
    # Wipe the plain password as soon as the tool exits.
    if ($plainPwd) {
        $plainPwd = $null
    }
    if ($securePwd) {
        $securePwd.Dispose()
    }
}
