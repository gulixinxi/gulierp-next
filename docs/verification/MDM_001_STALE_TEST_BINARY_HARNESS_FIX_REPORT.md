# MDM-001 Operator Evidence Stale Test Binary Harness Fix Report

> 严格限定范围:Operator 第 3 次实跑后,Step 4b 已 PASS(R3+R4 修复生效),但 Step 6 显示
> "19/19 PASS" / Step 7 10 个 Integration 全 PendingModelChangesWarning。根因:Harness
> **只 build MDM Infrastructure + API,不 build 测试项目**,Step 6/7 用 `--no-build`
> 跑出陈旧 DLL。本轮修复:Step 3 显式 build 2 个测试项目 + 动态 test count + stale binary
> 硬地板 + TRX summary parser + UTF-8 编码 + Step 8 条件化最终 gate。Gate 保持
> `MDM_001_OPERATOR_EVIDENCE_HARD_STOP`,真实 PG 验证仍待 Operator 端最终重跑。

## 1. Meta

| Field | Value |
|---|---|
| Goal | **MDM-001 Operator Evidence Stale Test Binary Harness Fix** |
| Sub-issue ID | **MDM-001R5** |
| Project root | `D:\guli\projects\gulierp-next` |
| Branch | `master` |
| Start HEAD | `61e183264f76111c2f7bfb43630c8184af64624a` |
| End HEAD | `61e183264f76111c2f7bfb43630c8184af64624a` (本轮 docs-only 提交后回填) |
| Window | 2026-08-21 14:45 +0800 (本轮会话) |
| Touched files | 1 modified (script) + 1 new (report) |
| New helpers in script | `Get-TestDiscoveryCount`, `Get-TestRunSummary`, `Build-TestProject` |
| Gate | `MDM_001_OPERATOR_EVIDENCE_HARD_STOP` (保持) |

## 2. Operator 第三次报告的现场

| Step | 结果 | 备注 |
|---|---|---|
| Step 1 | PASS | DB target guard |
| Step 2 | PASS | credential |
| Step 3 | PASS | Infrastructure + API build |
| Step 4a | PASS | Migration discovery |
| Step 4b | **PASS** | Migration apply(R3+R4 修复生效) |
| Step 4c | PASS | tables (by integration coverage) |
| Step 5 | PASS | seed (by integration coverage) |
| Step 6 | **显示 19/19 PASS** | **应该是 43/43** |
| Step 7 | **10 个全 FAIL** (PendingModelChangesWarning) | **应是 10 个全 PASS** |

## 3. 高可信根因(本轮验证后确认)

**Operator 第三轮的 19/19 + 10/10 PendingModelChanges 不是新生产模型缺陷,而是 Harness 跑了陈旧测试 DLL。**

### 3.1 验证证据(本轮)

| # | 证据 | 命令 / 文件 | 结果 |
|---|---|---|---|
| 1 | 脚本 Step 3 只 build MDM Infrastructure + API,不 build 测试项目 | `tools\dev\mdm-001-operator-evidence.ps1` line 351-365(R5 前) | **确认**:Step 3 body 只包含 `& $Dotnet build ... Mdm.Infrastructure.csproj` + `& $Dotnet build ... GuliERP.Api.csproj`,**无任何 `tests/*` project build** |
| 2 | 脚本 Step 6 / 7 使用 `--no-build` | line 316, 333(R5 前) | **确认**:`-c Release --no-build --nologo` |
| 3 | 当前源码 MDM Unit Tests = 43 | `dotnet test ... --list-tests` | **43 项**(MdmDtosTests 4 + MdmEntityContractTests 3 + MdmEnumContractTests 4 + MdmHiLoMetadataTests 5 + MdmIndexMetadataTests 7 + MdmRelationshipMetadataTests 12 + MdmValidationExceptionTests 8) |
| 4 | 当前源码 Integration Tests = 10 | `dotnet test ... --list-tests` | **10 项**(MdmItemCategoryAndItemFacts 5 + MdmMigrationFacts 1 + MdmUomFacts 4) |
| 5 | 当前 Release bin DLL 包含多少测试 | `dotnet test ... --no-build --list-tests` | MDM Tests DLL = **43** / Integration DLL = **10**(与源码对齐) |
| 6 | 脚本 Step 6 硬编码 "19 / 19 PASS" | line 322(R5 前) | **确认**:硬编码 string `'Unit tests: 19 / 19 PASS (...)'` |
| 7 | 脚本 Step 7 没有 TRX 解析 / 动态 count | line 339(R5 前) | **确认**:`Pass 'Integration tests PASS (migration, UOM seed, ...)'` — 无 count |
| 8 | 脚本 Step 8 写死 "MDM_001_REAL_MASTER_DATA_VERIFIED" | line 354(R5 前) | **确认**:无论前 7 步是否实际通过,Step 8 都会输出 success gate string |
| 9 | PowerShell 控制台编码未设 | (header) | **确认**:`[Console]::OutputEncoding` 未设 → 中文乱码(operator 看到 `鐨勬祴璇曡繍琛`) |

### 3.2 因果链(高可信)

```
R2 提交 harness 时,源码 MDM Tests = 19
R3 提交后,源码 MDM Tests = 26 (+7 R3)
R4 提交后,源码 MDM Tests = 43 (+12 R4 + 5 uncommitted 跨 R2 数字)
                          ↑
                          └── 实际值 = 24 baseline + 7 R3 + 12 R4 = 43

但 R3 / R4 都没改 harness。

Operator 跑 harness:
  Step 3 build MDM Infrastructure + API  (不 build test projects)
  Step 6 --no-build → 跑 bin/Release/GuliERP.Mdm.Tests.dll
                       ↑ 这个 DLL 是 R2 时的(只含 19 项)
                       → 输出 "19 / 19 PASS"(脚本硬编码 string)
  Step 7 --no-build → 跑 bin/Release/GuliERP.Mdm.IntegrationTests.dll
                       ↑ 这个 DLL 是 R3 修复前 build 的,Context 用 R3 前 Anonymous HasOne 关系
                       → 与 R4 修复后的 Snapshot Model 不一致
                       → 10 个测试全部触发 PendingModelChangesWarning
```

**双 DLL 都是陈旧**:Unit DLL = R2 era(19),Integration DLL = R3 era(pre-R4 anonymous nav)。

**R3 + R4 的代码侧修复在源码层完全正确**(43/43 PASS,Integration Context Model Differ 0,*Id1 全消),
但 Harness 永远跑不到新 DLL,因为 Harness 本身不 build test 项目。

## 4. 修复

### 4.1 Step 3 补 build 2 个测试项目

```powershell
# mdm-001R5: also build the 2 test projects. Without this,
# Step 6/7's --no-build runs the stale DLL.
$testProjectList = @(
    @{ Path = "$RepoRoot/tests/GuliERP.Mdm.Tests/GuliERP.Mdm.Tests.csproj";                 Name = 'MDM Unit Tests project' }
    @{ Path = "$RepoRoot/tests/GuliERP.Mdm.IntegrationTests/GuliERP.Mdm.IntegrationTests.csproj"; Name = 'MDM Integration Tests project' }
)
foreach ($tp in $testProjectList) {
    if (-not (Build-TestProject -ProjectPath $tp.Path -FriendlyName $tp.Name)) {
        exit 1
    }
}
Pass 'MDM.Tests + MDM.IntegrationTests built (just-built DLLs are what Step 6/7 will run).'
```

### 4.2 Step 6 改 dynamic count + stale guard

```powershell
# Pre-check discovered count
$unitDiscovery = Get-TestDiscoveryCount -ProjectPath $unitProj
if ($unitDiscovery.ExitCode -ne 0 -or $unitDiscovery.Count -lt 40) {
    Fail ("MDM unit tests discovery: only {0} tests, expected >= 40. " -f $unitDiscovery.Count) +
         "The test DLL is likely stale (pre-dates the R3 / R4 fixes). " +
         "Step 3 must build the test project. Aborting before `dotnet test` so the operator does not see a misleading PASS."
    exit 1
}
Pass ("MDM unit tests discovered: {0} (>= 40 expected; current source has 43 with R3 + R4 metadata tests)." -f $unitDiscovery.Count)

# Run with --no-build (Step 3 just built)
$unitOut = & $Dotnet test $unitProj -c Release --no-build --nologo 2>&1
$unitSummary = Get-TestRunSummary -Output $unitOut
if ($LASTEXITCODE -ne 0 -or $unitSummary.Failed -gt 0 -or $unitSummary.Total -lt $unitDiscovery.Count) {
    Fail (...)
    exit 1
}
Pass ("Unit tests: {0} / {0} PASS (R3 7 + R4 12 + 24 baseline = 43 in current source)." -f $unitSummary.Passed)
```

**Stale 硬地板**:MDM Unit Tests 必须 ≥ 40(当前 43);Integration Tests 必须 ≥ 10(当前 10)。
任何一项不达标,Step 6/7 在跑实际测试**之前**就 FAIL 并 abort,Operator 不会看到误导性的 PASS。

### 4.3 新增 helpers

```powershell
function Get-TestDiscoveryCount {
    [CmdletBinding()]
    param([Parameter(Mandatory=$true)][string]$ProjectPath, [int]$TimeoutSec=60)
    $listOut = & $Dotnet test $ProjectPath -c Release --no-build --list-tests 2>&1
    if ($LASTEXITCODE -ne 0) { return @{ Count=-1; Output=$listOut; ExitCode=$LASTEXITCODE } }
    $names = $listOut | Where-Object { $_ -match '^\s+GuliERP\.[A-Za-z0-9_.]+(\(.+\))?\s*$' }
    return @{ Count = @($names).Count; Output = $listOut; ExitCode = 0 }
}

function Get-TestRunSummary {
    [CmdletBinding()]
    param([Parameter(Mandatory=$true)][AllowEmptyCollection()]$Output)
    $joined = if ($Output -is [string]) { $Output } else { ($Output | ForEach-Object { "$_" }) -join "`n" }
    # Parses both "Passed!" and "Failed!" forms of the dotnet test summary line
    $m = [regex]::Match($joined, '(Passed|Failed)!\s*-\s*Failed:\s*(\d+)\s*,\s*Passed:\s*(\d+)\s*,\s*Skipped:\s*(\d+)\s*,\s*Total:\s*(\d+)')
    if ($m.Success) {
        return [pscustomobject]@{ Failed=[int]$m.Groups[2].Value; Passed=[int]$m.Groups[3].Value; Skipped=[int]$m.Groups[4].Value; Total=[int]$m.Groups[5].Value }
    }
    return [pscustomobject]@{ Failed=0; Passed=0; Skipped=0; Total=0 }
}

function Build-TestProject {
    [CmdletBinding()]
    param([Parameter(Mandatory=$true)][string]$ProjectPath, [string]$FriendlyName)
    $tOut = & $Dotnet build $ProjectPath -c Release --nologo 2>&1
    if ($LASTEXITCODE -ne 0) {
        Fail ("Test project build failed: {0} ({1})" -f $ProjectPath, $FriendlyName)
        Write-Host (Redact-SecretText $tOut) -ForegroundColor Red
        return $false
    }
    return $true
}
```

### 4.4 Step 8 条件化最终 gate

```powershell
$requiredSteps = @('Step1_DBTarget', 'Step2_Credential', 'Step3_Build',
                    'Step4a_Discovery', 'Step4b_Apply',
                    'Step6_Unit', 'Step7_Integration')
$allPassed = $true
$missed = @()
foreach ($s in $requiredSteps) {
    if (-not $script:StepResults.ContainsKey($s)) { $missed += $s; $allPassed = $false }
    elseif (-not $script:StepResults[$s]) { $allPassed = $false }
}
if ($missed.Count -gt 0) {
    Write-Host ("  [WARN] These required steps were not recorded (skipped?): {0}" -f ($missed -join ', ')) -ForegroundColor Yellow
}
if ($allPassed) {
    Write-Host "  MDM_001_REAL_MASTER_DATA_VERIFIED" -ForegroundColor Green
} else {
    Write-Host "  MDM_001_OPERATOR_EVIDENCE_FAILED" -ForegroundColor Red
}
```

`Step` 函数现在 set `$script:CurrentStep`,Pass/Fail 跟踪 step 结果到 `$script:StepResults`。

### 4.5 PowerShell UTF-8 编码(中文乱码 fix)

```powershell
try {
    [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
    $OutputEncoding = [System.Text.Encoding]::UTF8
    $PSDefaultParameterValues['Out-File:Encoding'] = 'utf8'
} catch { }
$env:DOTNET_CLI_UI_LANGUAGE = 'en-US'
$env:DOTNET_CLI_TELEMETRY_LANGUAGE = 'en-US'
```

显式 UTF-8 + dotnet CLI 强制 en-US(避免 locale 漂移影响 test count regex)。

## 5. 一次性审查 Step 7 之后所有脚本(本轮静态)

| # | 缺陷 | 本轮处理 |
|---|---|---|
| 1 | `--no-build` 旧 DLL 风险 | ✅ Step 3 build test projects |
| 2 | 硬编码 "19 / 19" | ✅ R5 改 dynamic count |
| 3 | 硬编码 "10 项" | ✅ R5 改 dynamic count |
| 4 | PowerShell array `-match/-notmatch` 语义 | ✅ R2 已修(`Test-MigrationDiscovered` 用 `ConvertTo-PlainText + regex`) |
| 5 | `$Host` 保留变量冲突 | ✅ 无使用(R2 round 已确认) |
| 6 | 端口占用误判 | N/A — 脚本不监听端口(API host 由其他 harness 启动) |
| 7 | 中文输出乱码 | ✅ R5 加 UTF-8 编码 |
| 8 | 进程未退出 | N/A — `dotnet test` exit clean;`dotnet ef` exit clean |
| 9 | 密码 / ConnectionString 泄漏 | ✅ `Redact-SecretText` + `Redact-ConnectionString` 已在 R2 加,R5 复用 |
| 10 | Migration 已应用时重跑 fail | ✅ Step 4b "or already up to date" 文案,EF `database update` 本身幂等 |
| 11 | Seed 幂等 / 重复运行 | ✅ Step 5 INFO 引用 `UomSeed_Loads_13Rows_And_IsIdempotent` integration case |
| 12 | Step 8 写死 success gate | ✅ R5 改条件化(每个 step 必须 Pass 才 emit `MDM_001_REAL_MASTER_DATA_VERIFIED`) |
| 13 | 没记录 step 结果 | ✅ R5 加 `$script:StepResults` tracking + `Step` 函数 set `$CurrentStep` |
| 14 | Integration ProjectFixture 是否改模型? | ✅ 本轮 R5 验证:用 Integration 真实 context 路径 build model,`MdmRelationshipMetadataTests` 12 个 test 涵盖(runtime + snapshot 一致性) |

## 6. 无密码全链预检(本轮已跑)

| # | 项 | 命令 | 结果 |
|---|---|---|---|
| 1 | Solution Release build | `dotnet build GuliERP.slnx -c Release` | **0 errors, 0 warnings**, 8.49s |
| 2 | MDM Tests 实际 run | `dotnet test tests/GuliERP.Mdm.Tests -c Release --no-build` | **43/43 PASS**, 616ms |
| 3 | Identity Tests 实际 run | (本轮 R4 round 已确认 21/21) | **21/21 PASS** |
| 4 | Foundation Tests 实际 run | (本轮 R4 round 已确认 44/44) | **44/44 PASS** |
| 5 | Integration Tests `--list-tests` | `dotnet test ... --list-tests` | **10 项 discovered** |
| 6 | Integration Context Model vs Snapshot | (R4 `MdmRelationshipMetadataTests` 12 个) | **0 pending model changes** |
| 7 | Integration Project 引用 MDM Infrastructure DLL | csproj ProjectReference | ✅ `..\..\modules\mdm\GuliERP.Mdm.Infrastructure\GuliERP.Mdm.Infrastructure.csproj` |
| 8 | Integration Runtime Model 0 个 `*Id1` | (R4 `Runtime_Model_Has_No_Shadow_FK_Columns`) | **0 个** |
| 9 | PowerShell 语法检查 | `[Parser]::ParseFile` | **0 errors** |
| 10 | `git diff --check` (限本轮 1 file) | EXIT 0 | ✅ |
| 11 | 旧 DLL stale guard 验证 | `Get-TestDiscoveryCount` on real project | **43/10 = matches** |
| 12 | Get-TestRunSummary 解析 | inline test (Passed!/Failed!/garbage) | **3/3 解析正确** |

## 7. 修改 / 新增文件清单

| 文件 | 类型 | 内容 |
|---|---|---|
| `tools/dev/mdm-001-operator-evidence.ps1` | M | +80 / -10 行(UTF-8 编码 + Step 3 test build + dynamic count + stale guard + TRX summary parser + Step 8 conditional gate + step tracking) |
| `docs/verification/MDM_001_STALE_TEST_BINARY_HARNESS_FIX_REPORT.md` | A | 本报告 |

**本轮 2 提交,2 文件**:
1. `fix(dev): prevent stale binaries in mdm operator evidence` — 1 file (script)
2. `docs(verification): record mdm evidence preflight hardening` — 1 file (report)

## 8. 边界遵守

| 禁止项 | 实际 |
|---|---|
| 修改 MDM 业务模型 | ❌ 0 改 |
| 修改 Migration / Designer / Snapshot | ❌ 0 改 |
| 抑制 PendingModelChangesWarning | ❌ 0 改(R5 用 stale guard 暴露问题,不是压制) |
| 修改数据库 | ❌ 0 改 |
| 修改前端 | ❌ 0 改 |
| 进入后续 Goal | ❌ 0 改 |
| 删除用户数据 | ❌ 0 改 |
| `git clean` / `git reset` / `git stash` / `git checkout` | ❌ 0 用 |
| `git add -A` / `git add .` | ❌ 0 用,显式 `git add <files>` |
| 提交 .quarantine / TestResults / 密码 / 继承 dirty | ❌ 0 commit |
| 提交 Identity / Foundation 业务代码 | ❌ 0 commit |
| 改 Domain / Application / API Contract | ❌ 0 commit |

## 9. 一次性静态回归(纯函数测试,本轮已跑)

| Helper | 测试输入 | 期望 | 实际 |
|---|---|---|---|
| `ConvertTo-PlainText` + `Test-MigrationDiscovered` | 5 case (ID alone / +Pending / similar wrong / empty / null) | 5/5 | **5/5 PASS** |
| `Get-TestDiscoveryCount` | MDM.Tests 项目 | Count = 43 | **43** ✅ |
| `Get-TestDiscoveryCount` | Integration 项目 | Count = 10 | **10** ✅ |
| `Get-TestRunSummary` | "Passed!  - Failed: 0, Passed: 43, Skipped: 0, Total: 43 - ..." | Passed=43 Failed=0 Total=43 | **PASS** ✅ |
| `Get-TestRunSummary` | "Failed!  - Failed: 5, Passed: 38, Skipped: 0, Total: 43" | Passed=38 Failed=5 Total=43 | **PASS** ✅ |
| `Get-TestRunSummary` | "garbage no summary" | all 0 | **PASS** ✅ |
| PowerShell 语法 | `[Parser]::ParseFile` | 0 errors | **0 errors** ✅ |
| Hardcoded "19" 在 runtime | grep `^\s*Pass '` lines | none | **0 个** ✅ |
| `[Console]::OutputEncoding` init | grep | present | **present** ✅ |
| `DOTNET_CLI_UI_LANGUAGE = en-US` | grep | present | **present** ✅ |
| Stale 硬地板(floor=100) | `Get-TestDiscoveryCount` | should fail | **WOULD FAIL**(count=43) ✅ |

## 10. 旧 DLL vs 新 DLL 时间戳证据

| 文件 | Operator 第 3 次跑时(假设) | 本轮 R5 修复后 build |
|---|---|---|
| `tests/GuliERP.Mdm.Tests/bin/Release/net10.0/GuliERP.Mdm.Tests.dll` | 14:18 (R2 era, 19 tests) | **14:49:43 (R5, 43 tests)** |
| `tests/GuliERP.Mdm.IntegrationTests/bin/Release/net10.0/GuliERP.Mdm.IntegrationTests.dll` | 14:18 (R2 era, anonymous nav) | **14:49:44 (R5, post-R4)** |

**Operator 重跑后这 2 个 DLL 会自动由 Step 3 重建** → 后续 Step 6/7 跑的是当前源码。

## 11. 修复前后对比

| 维度 | R4 后 / R5 前 | R5 后 |
|---|---|---|
| Step 3 build scope | MDM Infrastructure + API | **+ MDM.Tests + MDM.IntegrationTests** |
| Step 6 test count source | 硬编码 "19 / 19 PASS" | **dynamic from `--list-tests` + RunSummary** |
| Step 6 stale binary guard | none | **count < 40 → FAIL hard before `dotnet test`** |
| Step 7 test count source | 硬编码描述 | **dynamic from `--list-tests` + RunSummary** |
| Step 7 stale binary guard | none | **count < 10 → FAIL hard before `dotnet test`** |
| Step 8 final gate | `MDM_001_REAL_MASTER_DATA_VERIFIED` (硬) | **conditional on every required step PASS** |
| Console encoding | default (GBK on zh-CN) | **UTF-8 + dotnet CLI en-US** |
| Step 状态跟踪 | none | `$script:StepResults[stepname] = $true/$false` |

## 12. 是否已经达到 "Operator 只需最后再运行一次" 的条件

**是**(除真实 PG 验证外)。

修复后,Operator 再跑一次 harness 应当:
1. Step 1-3 全 PASS(无密码)
2. Step 4a-4c 全 PASS(无密码,设计时 placeholder DB)
3. Step 5 PASS(由 integration 覆盖)
4. Step 6 全 PASS(43 项,无密码)
5. Step 7 需要 Operator 输入真实 PGPASSWORD,跑出真实 10/10 PASS
6. Step 8 输出 `MDM_001_REAL_MASTER_DATA_VERIFIED`

**没有更多 step 会因为 Harness 缺陷 fail**(所有已知 step 7 之后缺陷已一次性修复)。

## 13. Operator 安全重跑步骤(本轮修后)

```powershell
cd D:\guli\projects\gulierp-next
$secure = Read-Host -Prompt 'PG password' -AsSecureString
$bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
$env:PGPASSWORD = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
[System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) | Out-Null

$env:DOTNET_ROOT = 'D:\guli\gulierp\.dotnet'
$env:Path = 'D:\guli\gulierp\.dotnet;' + $env:Path
$env:ConnectionStrings__GuliERP = "Host=192.168.2.228;Port=5432;Database=gulierp_g2_003_test;Username=gulidata;Password=$env:PGPASSWORD;Include Error Detail=true"

.\tools\dev\mdm-001-operator-evidence.ps1
```

**预期**:
- 控制台 UTF-8(无中文乱码)
- Step 3 build 4 个项目(MDM Infrastructure + API + MDM.Tests + MDM.IntegrationTests)
- Step 4a-4c 全 PASS
- Step 6 报告 43/43 PASS(动态)
- Step 7 报告 10/10 PASS(动态)
- Step 8 输出绿色 `MDM_001_REAL_MASTER_DATA_VERIFIED`

## 14. 当前 Gate

**`MDM_001_OPERATOR_EVIDENCE_HARD_STOP`**(保持)

理由:R5 修了 Harness 全部已知 stale-binary 缺陷;代码侧 43/43 PASS,Integration Model Differ 0;
**真实 PG 验证仍待 Operator 端在修后脚本上最终重跑**。重跑全 PASS → `MDM_001_REAL_MASTER_DATA_VERIFIED`。

## 15. 不进入其他 Goal / 不让 Operator 立即重跑

本轮强制 STOP。**不进入**:
- ❌ MDM-002 / DocumentKernel / G2-006 / SalesOrder
- ❌ 不让 Operator 立即重跑 — 必须先把本报告交给 ChatGPT 复核

## 16. 提交列表(本轮)

1. `fix(dev): prevent stale binaries in mdm operator evidence` — 1 file (script)
2. `docs(verification): record mdm evidence preflight hardening` — 1 file (report)

(End HEAD 在 commit 后回填)

## 17. 剩余 dirty / untracked

- 9 modified:继承 G2-005 + web WIP dirty(本轮**未触碰**)
- 165 untracked:156 继承 + 5 quarantined diagnostic(R4 round 留)+ 4 quarantine backups
- 1 modified(this round): `tools/dev/mdm-001-operator-evidence.ps1`
- 1 new(this round): `docs/verification/MDM_001_STALE_TEST_BINARY_HARNESS_FIX_REPORT.md`
