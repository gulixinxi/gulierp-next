# MDM-001 Migration Discovery Harness Fix Report

> 严格限定范围的修复 Goal:只修 Step 4a 的 False Negative,不改任何业务代码、不进 MDM-002、
> DocumentKernel、G2-006、SalesOrder;Gate 保持 `MDM_001_OPERATOR_EVIDENCE_ENVIRONMENT_BLOCKED`。
> 真实 PG 验证留给 Operator 端复跑。

## 1. Meta

| Field | Value |
|---|---|
| Goal | **MDM-001 Operator Evidence Harness Migration Discovery False-Negative Fix** |
| Sub-issue ID | **mdm-001R2** |
| Project root | `D:\guli\projects\gulierp-next` |
| Branch | `master` |
| Start HEAD | `082d573eb9c5f0c8698d10395ec273825dce5dee` |
| End HEAD | `082d573eb9c5f0c8698d10395ec273825dce5dee` (本轮 docs-only 提交后回填) |
| Window | 2026-08-21 14:00 +0800 (本轮会话) |
| Touched file | `tools/dev/mdm-001-operator-evidence.ps1` (本文件) |
| New file | `docs/verification/MDM_001_MIGRATION_DISCOVERY_HARNESS_FIX_REPORT.md` (本文件) |
| Gate | `MDM_001_OPERATOR_EVIDENCE_ENVIRONMENT_BLOCKED` (保持不变) |

## 2. Operator 报告的现象

Operator 跑 `tools\dev\mdm-001-operator-evidence.ps1`,到达 Step 4a 时输出:

```
[FAIL] MDM-001 migration is NOT discoverable. Re-check the [DbContext] / [Migration] attributes on the Designer.cs file.
Build started...
Build succeeded.
20260820190000_MDM001_InitializeMdmSchema (Pending)
```

但 `[DbContext(typeof(MdmDbContext))]` 和 `[Migration("20260820190000_MDM001_InitializeMdmSchema")]`
两个属性在 `modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/20260820190000_MDM001_InitializeMdmSchema.Designer.cs`
中**确实存在**(本轮 source 复核确认),且 EF Core `dotnet ef migrations list` 命令 **EXIT 0** 且明确列出
该 migration ID,只标 `(Pending)`。

**因此,本结果是 Harness False Negative,不是 Code Defect。**

## 3. 根因诊断

### 3.1 Step 4a 原实现(已删除,存档)

```powershell
$listOut = & $Dotnet ef migrations list ... 2>&1
if ($LASTEXITCODE -ne 0) { Fail '...'; exit 1 }
if ($listOut -notmatch '20260820190000_MDM001_InitializeMdmSchema') {
    Fail 'MDM-001 migration is NOT discoverable. ...'
    Write-Host (Redact-SecretText $listOut) -ForegroundColor Red
    exit 1
}
```

### 3.2 实际 `dotnet ef migrations list` 输出(本轮实测复现,无密码)

```
Build started...
Build succeeded.
An error occurred using the connection to database 'gulierp_design_time_placeholder' on server 'tcp://localhost:5432'.
An error occurred while accessing the database. Continuing without the information provided by the database. Error: Failed to connect to 127.0.0.1:5432
20260820190000_MDM001_InitializeMdmSchema
Pending status not shown. Unable to determine which migrations have been applied. This can happen when your project uses a version of Entity Framework Core lower than 5.0.0 or when an error occurs while accessing the database.
```

共 6 行,1 行是 Migration ID,5 行是 build / connection noise。

### 3.3 根因 — PowerShell 数组 `-notmatch` 语义陷阱

`& $Dotnet ... 2>&1` 在 PowerShell 里把 stdout + stderr 合并成 **`System.Object[]`**(本轮实测类型为
`System.Object[]`,长度 6)。

PowerShell 操作符手册明确:
> `-notmatch` 当 LHS 是 **数组**时,返回**不匹配的元素子集**,而不是 boolean。
> 在 `if (...)` 上下文里,非空数组被视为 truthy(即使每个元素都是 false-y)。

具体到本场景:
- `$listOut -notmatch '20260820190000_MDM001_InitializeMdmSchema'`
- 返回 5 元素 `System.Object[]`(`Build started...` / `Build succeeded.` / 两条 connection warning / `Pending status not shown. ...`)
- 在 `if (...)` 里,5 元素数组 → truthy → 进入 fail 分支

也就是说,只要 `dotnet ef` 输出里**任何一行不含 migration ID**,本 check 就会 FAIL。这是一个**结构性**的 false negative
陷阱,与 `(Pending)` 字符串无关;`Migration ID` 单独一行 + 4 行 noise 也会 FAIL。

**本轮用同一份 dotnet ef 实测输出复现:**
- 旧代码 `if ($listOut -notmatch '20260820190000_MDM001_InitializeMdmSchema')` → True(FAIL)
- 新代码 `Test-MigrationDiscovered` → True(PASS)
- `[string]$listOut -notmatch '...'` → False(PASS) — 佐证 join-to-string 即可修

### 3.4 其他次要风险(本轮一并修)

| 风险 | 处理 |
|---|---|
| `(Pending)` 字符串本身不是问题,不需要专门排除 | 已确认:`-notmatch` 即使接受 `20260820190000_MDM001_InitializeMdmSchema` 子串也能匹配 `(Pending)` 行;新 regex 用 lookbehind / lookahead 仍然支持 |
| ANSI CSI 颜色序列可能干扰匹配 | 新 helper `ConvertTo-PlainText` 显式剥 ESC `[` 序列 |
| CRLF / CR 行尾在 Windows 上可能干扰 | 新 helper 显式 `CRLF/CR → LF` 归一化 |
| 其他迁移恰好带相同 prefix 可能误匹配 | regex 用 `(?<![A-Za-z0-9_])` lookbehind 锚定 ID 边界,防 prefix-collision |
| `Migration ID` 紧贴其他字符(罕见)可能误匹配 | regex 用 `(?=\s|\(|$)` lookahead 锚定后边界 |

## 4. 修复内容(最小化)

### 4.1 新增 helper:`ConvertTo-PlainText`

```powershell
function ConvertTo-PlainText {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $false)]
        [AllowNull()][AllowEmptyCollection()]
        $Value
    )
    if ($null -eq $Value) { return '' }
    $joined = if ($Value -is [string]) {
        $Value
    } else {
        ($Value | ForEach-Object { "$_" }) -join "`n"
    }
    # Strip ANSI CSI: ESC [ ... letter
    $joined = $joined -replace ([char]27 + "\[[0-9;?]*[a-zA-Z]"), ''
    # Defensive CRLF / CR -> LF
    $joined = $joined -replace "`r`n?", "`n"
    return $joined
}
```

### 4.2 新增 helper:`Test-MigrationDiscovered`

```powershell
function Test-MigrationDiscovered {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $false)]
        [AllowNull()][AllowEmptyCollection()]
        $CommandOutput,
        [Parameter(Mandatory = $true)]
        [string]$ExpectedMigrationId
    )
    $text = ConvertTo-PlainText -Value $CommandOutput
    if ([string]::IsNullOrEmpty($text)) { return $false }
    $escaped = [regex]::Escape($ExpectedMigrationId)
    $pattern = "(?<![A-Za-z0-9_])${escaped}(?=\s|\(|$)"
    return [regex]::IsMatch($text, $pattern)
}
```

### 4.3 Step 4a 主体改写

```diff
-if ($listOut -notmatch '20260820190000_MDM001_InitializeMdmSchema') {
-    Fail 'MDM-001 migration is NOT discoverable. Re-check the [DbContext] / [Migration] attributes on the Designer.cs file.'
+if (-not (Test-MigrationDiscovered -CommandOutput $listOut -ExpectedMigrationId $expectedMigrationId)) {
+    Fail "MDM-001 migration '$expectedMigrationId' is NOT discoverable. Re-check the [DbContext] / [Migration] attributes on the Designer.cs file."
     Write-Host (Redact-SecretText $listOut) -ForegroundColor Red
     exit 1
 }
-Pass 'MDM-001 migration discovered (20260820190000_MDM001_InitializeMdmSchema).'
+Pass "Migration discovered: $expectedMigrationId (pending/apply state checked later)."
```

并补充 `mdm-001R2` 根因注释到 Step 4a 头部(意图 + 旧坑 + 新约定)。

## 5. 六类(外加 4 类)解析测试 — 全部 PASS

| # | 用例 | 输入(摘) | 期望 | 实际 |
|---|---|---|---|---|
| 1 | Migration ID 单独一行(mixed stdout) | `Build started` / `Build succeeded` / `<id>` / `Pending status not shown` | PASS | **PASS** |
| 2 | Migration ID + `(Pending)` | `<id> (Pending)` | PASS | **PASS** |
| 3 | 前后空格/CRLF | `<id>   ` + ``<id>`r`n`` | PASS | **PASS** |
| 4 | ANSI 包裹 | `` `e[33m<id>`e[0m (Pending) `` | PASS | **PASS** |
| 5 | 相似但不同 Migration ID | `20260820190001_MDM001_InitializeMdmSchema (Pending)` | FAIL | **FAIL** |
| 6 | 输出不存在 Migration ID | `No migrations were found.` | FAIL | **FAIL** |
| 7 (defensive) | ID 作为更长 token 子串(`X<id>-extended (Pending)`) | 期望 FAIL(防止 prefix-collision) | FAIL | **FAIL** |
| 8 (defensive) | 空字符串 `''` | 期望 FAIL | FAIL | **FAIL** |
| 9 (defensive) | `$null` | 期望 FAIL(不抛) | FAIL | **FAIL** |
| 10 (defensive) | 混合行尾(CR/LF/CRLF) | `Build started`r`Build succeeded.`r`n`<id>`n | PASS | **PASS** |

**总计 10/10 PASS**。执行命令(内联 temp PowerShell,本仓库无脚本测试框架):

```powershell
$ast = [System.Management.Automation.Language.Parser]::ParseFile(
    'tools/dev/mdm-001-operator-evidence.ps1', [ref]$null, [ref]$null)
$helpers = $ast.FindAll({
    param($n) $n -is [System.Management.Automation.Language.FunctionDefinitionAst] -and
    $n.Name -in 'ConvertTo-PlainText', 'Test-MigrationDiscovered'
}, $true)
foreach ($h in $helpers) { Invoke-Expression $h.Extent.Text }
# ... 10 T('case', $output, $expected) calls
```

## 6. 端到端冒烟(实跑 `dotnet ef`,复现 Operator 真实输入)

```powershell
$realOut = & 'D:\guli\gulierp\.dotnet\dotnet.exe' ef migrations list `
    --project 'modules\mdm\GuliERP.Mdm.Infrastructure/GuliERP.Mdm.Infrastructure.csproj' `
    --startup-project 'apps\api\GuliERP.Api\GuliERP.Api.csproj' `
    --configuration Release 2>&1

Test-MigrationDiscovered -CommandOutput $realOut `
    -ExpectedMigrationId '20260820190000_MDM001_InitializeMdmSchema'
# => True  (旧代码此场景会 FAIL)
```

即:**Operator 看到的同一份 `dotnet ef` 输出,经过新 helper 判定为 PASS**。

## 7. 静态 / 语法 / 工作树验证

| # | 项 | 结果 |
|---|---|---|
| 1 | `[DbContext(typeof(MdmDbContext))]` 属性 | ✅ `modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/20260820190000_MDM001_InitializeMdmSchema.Designer.cs` |
| 2 | `[Migration("20260820190000_MDM001_InitializeMdmSchema")]` 属性 | ✅ 同上 |
| 3 | Migration ID 常量(脚本硬编码 vs 实际 .cs 文件) | ✅ 完全一致 |
| 4 | `MdmDbContext` 在 API Startup 中的 DI 注册 | ✅ `apps/api/GuliERP.Api/Mdm/*` 路径已 wire |
| 5 | PowerShell 语法(`Parser::ParseFile`) | ✅ 0 error / 1296 tokens |
| 6 | `git diff --check tools\dev\mdm-001-operator-evidence.ps1` | ✅ EXIT 0(仅 Windows CRLF 提示) |
| 7 | 工作树 modified(untracked 保持) | 10 modified(9 继承 + 1 我改的脚本)/ 156 untracked 全继承 |

## 8. 修改边界(本轮严格遵守)

| 项 | 本轮是否触碰 | 证据 |
|---|---|---|
| `tools/dev/mdm-001-operator-evidence.ps1` | ✅ 改 | +79 lines / -34 lines(本轮 `git diff --stat` 仅此文件) |
| 业务代码 / Domain / Application / API / Migration | ❌ 0 改 | git diff 不包含 |
| `apps/web/**` | ❌ 0 改 | git diff 不包含 |
| 9 个继承 modified 文件(`.gitignore` / Foundation / Identity / 3 个 tools/dev/*.ps1) | ❌ 0 改 | git diff 不包含 |
| 数据库 | ❌ 0 改 | 无 DDL / DML 执行 |
| Migration ID 常量 | ❌ 0 改 | 与 Designer.cs 仍然完全一致 |
| Migration Designer attributes | ❌ 0 改 | source 复核未变 |
| 密码 / 配置文件 | ❌ 0 写 | 全文无 password/secret 字面值 |
| TestResults / .NET SDK 缓存 | ❌ 0 提交 | 不在 commit candidate |
| `git add -A` / `git add .` / `git commit -am` | ❌ 0 用 | 显式 `git add <file1> <file2>` |
| 其他 Agent 继承 dirty | ❌ 0 触碰 | 9 modified / 156 untracked 全部保留 |

## 9. Operator 安全重跑说明

修复后,Operator 在原交互终端再次执行:

```powershell
cd D:\guli\projects\gulierp-next
$secure = Read-Host -Prompt 'PostgreSQL password for gulidata@192.168.2.228:5432/gulierp_g2_003_test' -AsSecureString
$bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
$env:PGPASSWORD = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
[System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) | Out-Null

$env:DOTNET_ROOT = 'D:\guli\gulierp\.dotnet'
$env:Path = 'D:\guli\gulierp\.dotnet;' + $env:Path
$env:ConnectionStrings__GuliERP = "Host=192.168.2.228;Port=5432;Database=gulierp_g2_003_test;Username=gulidata;Password=$env:PGPASSWORD;Include Error Detail=true"

.\tools\dev\mdm-001-operator-evidence.ps1
```

**预期**:Step 4a 输出从 `[FAIL] ... is NOT discoverable` 变成
`[PASS] Migration discovered: 20260820190000_MDM001_InitializeMdmSchema (pending/apply state checked later).`。
然后 Step 4b 继续跑 `dotnet ef database update`,被 host 端真实 PG 接受,后续 Step 5-7 进入测试套件。

## 10. 当前 Gate

**`MDM_001_OPERATOR_EVIDENCE_ENVIRONMENT_BLOCKED`**(保持不变)

本轮仅修了 Harness 的 Step 4a false negative,**未真实运行 PG 验证**;Gate 仍需 Operator 端在修后
脚本上重跑 Step 4b ~ Step 7 才升级到 `MDM_001_REAL_MASTER_DATA_VERIFIED`。**Agent session 不得代替 Operator
跑真实 PG 验证**(PGPASSWORD 仍不可注入)。

## 11. 提交纪律

| 项 | 本轮 |
|---|---|
| 暂存方式 | `git add <file1> <file2>` 显式 |
| 暂存文件 | `tools/dev/mdm-001-operator-evidence.ps1` + `docs/verification/MDM_001_MIGRATION_DISCOVERY_HARNESS_FIX_REPORT.md` |
| 提交信息 | `fix(dev): accept pending mdm migration as discoverable` |
| 文件数 | 2 |
| 提交 atomic | 1 commit |
| 未提交项 | 9 modified 继承 dirty + 156 untracked 继承 + TestResults / .NET SDK 缓存 |

## 12. 禁止 / 边界确认

本轮:
- ❌ 不进入 MDM-002 / DocumentKernel 修复 / G2-006 / SalesOrder
- ❌ 不修改业务代码 / Migration ID / Migration Designer / 数据库
- ❌ 不要求 Operator 重新提供密码(只在文档里给出**自己终端**的安全执行方式)
- ❌ 不使用 `git add -A` / `git add .` / `git commit -am`
- ❌ 不把任何 FAIL 改写为无条件 PASS

修复完成后停止,等待 Operator 端真实 PG 复跑。
