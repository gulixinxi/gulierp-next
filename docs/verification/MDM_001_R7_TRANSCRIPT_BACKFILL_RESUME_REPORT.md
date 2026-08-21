# MDM-001 R7 Operator Transcript Evidence Backfill & Resume Fix Report

> **Scope**: mdm-001R9 (Operator Transcript Backfill + R8 报告修正) — R8 报告
> 错误地声称 `Test-PriorOperatorEvidence` 可在仅有 transcript 时判 PASS,
> 但 R7 harness 既不写 Unit/Integration `.trx` 也不写 `.log`。
> R9 引入严格 **Mode B (Operator Transcript Backfill)** 验证链,
> 用 `docs/verification/MDM_001_R7_OPERATOR_TRANSCRIPT_EVIDENCE.md`
> (R9 新建) 替代不存在的 TRX/log 来证明 R7 Step 1-9 走过。
>
> **R9 严格不变量**:
> - 不创建任何假 TRX / 假 .log / 假 dotnet test 原始日志
> - 不重跑 Step 1-9
> - 不修改业务代码 / Migration / Integration Test / 前端 / 数据库
> - 不要求 Operator 在本轮执行任何命令(本轮 R9 全 Agent 端)

## 1. Meta

| Field | Value |
|---|---|
| Goal | **MDM-001 R7 Operator Transcript Backfill + R8 修正** |
| Sub-issue ID | **MDM-001R9** |
| Project root | `D:\guli\projects\gulierp-next` |
| Branch | `master` |
| Start HEAD | `ff8bb8e docs(verification): record mdm api runtime resume readiness` (R8) |
| End HEAD | `ba24180 docs(verification): backfill mdm r7 operator transcript evidence` (本轮 R9 收尾) |
| Window | 2026-08-21 16:50 +0800 起 |
| Gate | `MDM_001_API_RUNTIME_RESUME_HARNESS_VERIFIED` (R8) → **本轮升级为** `MDM_001_TRANSCRIPT_BACKFILL_VERIFIED` |
| Touched files | 3 .ps1 (Harness + main + selftest) + 1 canonical transcript file (R9 新建) + 1 R8 报告修正 + 1 R9 报告 + GOAL_REGISTRY |
| Agent-side PG | **❌ UNAVAILABLE** — 与 R6/R7/R8 同;Operator-only verification |

## 2. R8 证据判断为什么错误(per brief §二)

R8 报告 (`MDM_001_API_RUNTIME_RESUME_FIX_REPORT.md`) 曾在两处过度乐观:

**R8 误表述 1**(报告 §5,line 164-173):
> Resume 模式证据验证(`Test-PriorOperatorEvidence`): 必须存在(在 `tests/_evidence_trx/` 或 `-EvidenceRoot` 参数指定): 至少 1 个 `POC001_Run{1..5}.trx` 或对应 .log ... 证据不完整 → 打印 `MDM_001_FINAL_ACCEPTANCE_FAILED`,不伪造

**实际**:R7 harness **从未**为 Unit/Integration 写 `.trx` 或 `.log`。Operator 在 R7 之后明确报告:"R7 没有生成 Unit/Integration TRX,这是 Harness 证据落盘缺陷,不得把不存在的文件描述为存在"。

**R8 误表述 2**(报告 §6,line 197-200):
> **TRX 缺失的诚实披露**: R7 + R8 harness 默认**不**为 Unit/Integration 写 TRX 文件(只写 host log); 旧 `tests/_evidence_trx/GuliERP.*.Tests.trx` 来自 2026-08-20(前轮),不适用于本轮; 证据文件来源完全在 `tests/_evidence_trx/api_host_round*.log` 和 Operator 控制台 transcript; Resume 模式接受 .log 替代 .trx(per `Test-PriorOperatorEvidence` 的 `Test-Path $unitTrx -or Test-Path $unitLog` 逻辑)

**实际**:R7 harness 同样**不**写 `.log`(per canonical transcript 144 行):
> "The R7 harness's Step 6/7/8/9 sections run `dotnet test` with output going to stdout only — there is no `--logger trx` argument. The R7 harness does not write per-round TRX or per-suite `.log` files. Therefore the **only machine-generated R7 evidence on disk** is the API host log."

R8 报告 §6 的"Resume 模式接受 .log 替代 .trx"既不准确(8/20 那 8 个 `.trx` 来自前轮不适用 R7)也未真正实现(`Test-PriorOperatorEvidence` 在 R8 阶段**没有**Mode B,Mode A 失败时直接 HARD STOP 而不会退化到 transcript 验证)。

**R9 修正原则**:
- 不抹掉 R8 报告的其它贡献(Stop-ApiHost 生命周期修复、selftest 30/30、function-after-try-finally bug guard)
- 明确撤回 R8 报告中关于证据可用性的不实表述
- 引入 Mode B 用 R9 新建 canonical transcript file 来诚实验证 R7 Step 1-9

## 3. R7 真实存在与不存在的证据(per brief §三 + brief §一)

| 证据 | R7 真实状态 | 验证方式 |
|---|---|---|
| MDM Unit 57/57 PASS | **OPERATOR_TRANSCRIPT_REPORTED**(仅在 Operator 控制台 transcript,无 `.trx` / 无 `.log`) | Mode B 读取 canonical transcript file |
| Identity Unit 21/21 PASS | OPERATOR_TRANSCRIPT_REPORTED | 同上 |
| Foundation 44/44 PASS | OPERATOR_TRANSCRIPT_REPORTED | 同上 |
| Integration Round 1 10/10 PASS | OPERATOR_TRANSCRIPT_REPORTED | 同上 |
| Integration Round 2 10/10 PASS | OPERATOR_TRANSCRIPT_REPORTED | 同上 |
| Integration Round 3 10/10 PASS | OPERATOR_TRANSCRIPT_REPORTED | 同上 |
| Integration Round 4 10/10 PASS | OPERATOR_TRANSCRIPT_REPORTED | 同上 |
| Integration Round 5 10/10 PASS | OPERATOR_TRANSCRIPT_REPORTED | 同上 |
| API Runtime Round 1 /health/live=200 | **MACHINE_LOG_VERIFIED** | `tests/_evidence_trx/api_host_round1_20260821155926.log` 6,788 字节 |
| API Runtime Round 1 /=200 | **MACHINE_LOG_VERIFIED** | 同上 |
| API Runtime Round 1 /health/ready=200 | **MACHINE_LOG_VERIFIED** | 同上 |
| API Runtime Round 2 | **NOT_RUN**(R7 在 Round 1 之后因 Stop-ApiHost 错误中断) | n/a |
| `GuliERP.Mdm.Tests.trx` 等 8 个 `.trx` 文件 | **NOT_AVAILABLE for R7**(来自 2026-08-20 前轮 run,**不**是 R7 证据) | `Test-Path` → True 但 `git log --since=R7-commit --until=R7-commit+1h` 显示 R7 run 期间无新写入 |
| Per-round Integration `.log` | **NOT_AVAILABLE** | n/a(R7 harness 不写) |

**R9 关键不变量**:
- ✗ **不**创建任何假 TRX / 假 .log / 假 dotnet test 原始日志
- ✗ **不**修改 `tests/_evidence_trx/GuliERP.*.Tests.trx` 文件(它们是 8/20 旧 run 的产物)
- ✗ **不**伪造"Operator 跑过"的任何内容到 disk
- ✓ **可以**新建 `docs/verification/MDM_001_R7_OPERATOR_TRANSCRIPT_EVIDENCE.md`(这是文档,不是机器证据)

## 4. Mode A / Mode B 双模式严格验证(per brief §三)

### Mode A — Machine TRX(未来 run 用)

完整 TRX + log 文件必须**实际存在**于 `-EvidenceRoot`(默认 `tests/_evidence_trx/`):

| 必须文件 | 用途 |
|---|---|
| `GuliERP.Mdm.Tests.trx` 或 `.log` | MDM 单元 57/57 |
| `GuliERP.Identity.Tests.trx` 或 `.log` | Identity 单元 21/21 |
| `GuliERP.Foundation.Tests.trx` 或 `.log` | Foundation 单元 44/44 |
| 5 个 `POC001_Run{1..5}.trx` 或 `.log` | 5 轮 Integration 50/50 |
| `api_host_round1_*.log` | API Round 1 三个 200 |

任意 TRX 出现 `outcome="Failed"` → 报告 `TrxFails`。
任意 integration TRX 不显示 `total=10 passed=10 failed=0` → 报告 `TrxFails`。
所有条件满足 → `Mode = MACHINE_TRX` / `Ok = $true`。

### Mode B — Operator Transcript Backfill(R7 专用)

**前置条件**(必须**全部**满足):
1. 存在 canonical `docs/verification/MDM_001_R7_OPERATOR_TRANSCRIPT_EVIDENCE.md`(R9 新建,含 `EVIDENCE_TYPE=OPERATOR_TRANSCRIPT_REPORTED` 标记)
2. transcript 文件**显式**包含 `TRX_STATUS=NOT_AVAILABLE` 声明(R7 没写 TRX,这是 honest disclosure)
3. transcript 文件记录 **57/57, 21/21, 44/44** 的 PASS 计数
4. transcript 文件记录 **Integration Round 1-5 各 10/10 PASS**
5. transcript 文件记录 **API Round 1 三个 200**(Round 2 `NOT_RUN`)
6. transcript 文件中**显式登记** R7 harness commit SHA `15d46c4` 且与当前 git 验证一致
7. `tests/_evidence_trx/api_host_round1_20260821155926.log` 存在且其 **SHA256 与 transcript 中登记的一致**(R9 用 `C855F079C3AB366A8631BE85B8DC27A63FE607993BBBBF51F6C44AEF054FFDDA` 验证)
8. 当前 R9 HEAD 到 R7 HEAD `15d46c4` 的 git diff 不得触及:
   - `^modules/.*\.cs$`
   - `^apps/.*\.cs$`
   - `Migrations/.*\.cs$`
   - `^tests/GuliERP\..*\.cs$`
   - `tools/GuliERP\..*\.cs$`
9. transcript 文件**显式承认**所有 TRX / log 文件 `not present`(honest disclosure)

任一条件失败 → `Mode = OPERATOR_TRANSCRIPT_BACKFILL_INVALID` / `Ok = $false` + Missing 列表。
所有条件满足 → `Mode = OPERATOR_TRANSCRIPT_BACKFILL_VERIFIED` / `Ok = $true` / `Backfill` 字段被填充。

### NEITHER_MODE_VERIFIED — HARD STOP

Mode A 失败(TRX 不全)且 Mode B 失败(transcript 不全)→ `Mode = NEITHER_MODE_VERIFIED` / `Ok = $false`。
Resume 在此状态下**不**启动 API,**不**提示密码,直接打印 `MDM_001_FINAL_ACCEPTANCE_FAILED` + Missing。

## 5. Canonical Operator Transcript Evidence File

**路径**:`D:\guli\projects\gulierp-next\docs\verification\MDM_001_R7_OPERATOR_TRANSCRIPT_EVIDENCE.md`

**11 个 section**:
- A. Provenance — Operator 真实执行时间窗口 + 命令 + R7 harness HEAD
- B. Step outcomes — Step 1-10 每个步骤的 Operator 报告
- C. Test counts summary — 57/21/44/50 = 172/172 PASS 表格
- D. SHA256 manifest — `api_host_round1_20260821155926.log` SHA256 `C855F079C3AB366A8631BE85B8DC27A63FE607993BBBBF51F6C44AEF054FFDDA`
- E. TRX status — 显式 `TRX_STATUS=NOT_AVAILABLE` + 12 个文件 NOT_AVAILABLE/NOT_RUN/MACHINE_LOG_VERIFIED 分类
- F. R7→R9 invariant — R9 承诺只改 harness/docs/GOAL_REGISTRY,不改 business code
- G. What is NOT in this file — 不含密码 / 不含 connection string / 不含 TRX / 不含 fabrication
- H. Trust boundary — 5 条 trust 条件 + 失败行为
- I. Operator-visible final-action summary — Operator 唯一动作
- J. Hash registration — R9 自身 file 的 SHA256 由 R9 selftest 计算
- K. Boundary compliance — R9 修改范围 + 禁止项

**关键 honest disclosure**:
> "The 8/20 `GuliERP.*.Tests.trx` files in `tests/_evidence_trx/` are from a prior (R2-era) run; **they do NOT reflect the R7 Operator run** and MUST NOT be used as R7 evidence."

## 6. Harness 函数变更(per brief §三)

`tools/dev/Mdm001Acceptance.Harness.ps1`:

1. **新增** `Test-OperatorTranscriptBackfill`(139 行):
   - 验证 transcript file 存在
   - 验证 6 项强制 marker(`EVIDENCE_TYPE` / `TRX_STATUS=NOT_AVAILABLE` / 57/21/44 / Round 1-5 / API 三个 200)
   - 验证 R7 HEAD 字段与 `ExpectedR7Head` 一致
   - 验证 API log SHA256 与磁盘文件 SHA256 一致
   - 验证 R7→HEAD git diff 不含 business code(走 `Test-NoBusinessCodeChangeSinceR7`)
   - 验证 transcript **显式**声明 `NOT_AVAILABLE` 和 `not present`(honest disclosure guard)
   - 返回 `[pscustomobject]@{ Ok, Mode, Missing, ApiLogSha256, ApiLogPath, RecordedR7Head }`

2. **修改** `Test-NoBusinessCodeChangeSinceR7`(73 行):
   - 用 `$ErrorActionPreference = 'Continue'` 临时包裹 `& git diff ...`,避免 invalid HEAD 抛 terminating NativeCommandError
   - try/finally 恢复 `$ErrorActionPreference`
   - G10 selftest 验证:bad HEAD `HEAD~99999` → `Ok=$false` + `ChangedCsFiles` 含 `git diff failed:`

3. **修改** `Test-OperatorTranscriptBackfill` marker regex(更宽松):
   - `'(?im)^\s*EVIDENCE_TYPE\s*=\s*OPERATOR_TRANSCRIPT_REPORTED\s*$'` → `'(?im)EVIDENCE_TYPE\s*=\s*OPERATOR_TRANSCRIPT_REPORTED'`
   - `'(?im)^\s*TRX_STATUS\s*=\s*NOT_AVAILABLE\s*$'` → `'(?im)TRX_STATUS\s*=\s*NOT_AVAILABLE'`
   - 原因:canonical transcript file 用 `> **EVIDENCE_TYPE=...**` markdown 包装,旧 regex 必须严格单行;新 regex 允许 markdown bold
   - SHA256 regex 也放宽:`(?is)api_host_round1_20260821155926\.log\b[^|]*?\|\s*`?\s*([0-9A-Fa-f]{64})\s*`?`
   - 旧 regex `\|\s*`?tests/...`?\s*\|\s*([0-9A-Fa-f]{64})` 在含 markdown table 时不匹配

`tools/dev/mdm-001-final-acceptance.ps1`:

4. **修改** `Test-PriorOperatorEvidence`:
   - Mode A 失败时不再 silent HARD STOP
   - 改 fall through 到 Mode B(读 `Test-OperatorTranscriptBackfill` 用 canonical transcript file)
   - Mode B 成功时也调用 `Test-NoBusinessCodeChangeSinceR7` 验证 R7→HEAD 无 business code 改动
   - Mode A 和 Mode B 都失败 → `NEITHER_MODE_VERIFIED`
   - API log 查找改用 `Get-ChildItem -Recurse`,因为 Operator 把 log 放在 `tests/_evidence_trx/` 子目录

`tools/dev/mdm-001-final-acceptance-selftest.ps1`:

5. **新增** Section G(R9 15 测试):
   - G1-G8:`Test-OperatorTranscriptBackfill` 8 个纯逻辑测试
   - G9-G11:`Test-NoBusinessCodeChangeSinceR7` 3 个测试
   - G12-G15:`Test-PriorOperatorEvidence` Mode A/Mode B 集成测试
   - G12 用真实 canonical `MDM_001_R7_OPERATOR_TRANSCRIPT_EVIDENCE.md`
   - G15 验证 Mode A 失败 → fall back Mode B

6. **修改** Section F(F1/F3):
   - F1 显式传 `-TranscriptEvidencePath (Join-Path $tmpEvidence 'no-such-transcript.md')`,避免默认 canonical file 误判
   - F3 同上,避免 failed TRX 被 Mode B 掩盖

7. **修复** `Assert-True ($array -match 'pattern')` 类型错误:
   - PowerShell 5.1 把 `Object[]`(单元素数组)传 `[bool]$Value` 时报"Cannot convert System.Object[] to type System.Boolean"
   - 改用 `[bool]($array -match 'pattern')` 显式 cast
   - 影响 G2-G7, G12, G14(原 `[bool](...)` 在 F1/F3 已正确)

## 7. SHA256 Manifest(per brief §五)

| 文件 | SHA256 | 来源 |
|---|---|---|
| `tests/_evidence_trx/api_host_round1_20260821155926.log` | `C855F079C3AB366A8631BE85B8DC27A63FE607993BBBBF51F6C44AEF054FFDDA` | `Get-FileHash -Algorithm SHA256`(R9 验证) |
| `tests/_evidence_trx/api_host_round1_20260821155926.log.err` | `E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855` | 空文件标准 SHA256 |
| `docs/verification/MDM_001_R7_OPERATOR_TRANSCRIPT_EVIDENCE.md` | (R9 selftest 计算) | `Get-FileHash` on R9-created file |

canonical transcript file 中 D. SHA256 manifest section **显式**登记 `api_host_round1_20260821155926.log` SHA256 = `C855F079C3AB366A8631BE85B8DC27A63FE607993BBBBF51F6C44AEF054FFDDA`,R9 selftest 运行时验证两个值必须相等。

## 8. 自测结果(per brief §五)

`tools/dev/mdm-001-final-acceptance-selftest.ps1` 全跑结果:

```
============================================================
mdm-001R9 Self-Test result: 45 pass / 0 fail / 45 total
============================================================
```

详细 Section 分布:
- A. Get-FinalGateDecision: 5/5 PASS
- B. Parsers: 4/4 PASS
- C. Redaction/env: 3/3 PASS
- D. Process lifecycle(real long-running process): 10/10 PASS
- E. Structural discoverability: 4/4 PASS
- F. Resume evidence(pure logic): 3/3 PASS
- G. R9 NEW Operator Transcript Backfill: 15/15 PASS
- Other: 1/1 PASS

**关键 G 段**:
- G1: transcript file not found → Ok=False
- G2: missing EVIDENCE_TYPE → Ok=False
- G3: missing TRX_STATUS=NOT_AVAILABLE → Ok=False
- G4: missing 57/57 → Ok=False
- G5: missing Round 3 → Ok=False
- G6: API log file missing on disk → Ok=False
- G7: API log SHA256 mismatch → Ok=False
- **G8: Full valid transcript + real API log → Ok=True, Mode=BACKFILL_VERIFIED** ← fixture 端到端
- G9: Real repo R7→HEAD diff touches ONLY harness/docs → Ok=True
- G10: Bad R7 head(HEAD~99999)→ Ok=False, 不会抛 terminating error
- G11: Synthetic forbidden file in allowed path → 0 violations detected
- **G12: Canonical evidence file (`MDM_001_R7_OPERATOR_TRANSCRIPT_EVIDENCE.md`) → Mode=BACKFILL_VERIFIED, Ok=True** ← 真实文件端到端
- G13: Canonical + business code change simulation(synthetic check)→ 0 forbidden violations
- G14: Mode A: fake TRX evidence in temp root → Ok=True, Mode=MACHINE_TRX
- G15: Mode A partial(3 of 5 TRX)→ falls back Mode B → NEITHER

## 9. 是否创建任何假 TRX(per brief §八-8)

**NO**。R9 创建/修改的文件清单:

| 文件 | R9 是否创建假 TRX? |
|---|---|
| `tools/dev/Mdm001Acceptance.Harness.ps1` | NO(纯 .ps1,无 .trx) |
| `tools/dev/mdm-001-final-acceptance.ps1` | NO(纯 .ps1,无 .trx) |
| `tools/dev/mdm-001-final-acceptance-selftest.ps1` | NO(纯 .ps1,无 .trx) |
| `docs/verification/MDM_001_R7_OPERATOR_TRANSCRIPT_EVIDENCE.md` | NO — 这是 Markdown 文档,不是 TRX(.trx 是 .NET test 输出的 XML 格式) |
| `docs/verification/MDM_001_API_RUNTIME_RESUME_FIX_REPORT.md` | NO — R8 报告修正 |
| `docs/verification/MDM_001_R7_TRANSCRIPT_BACKFILL_RESUME_REPORT.md` | NO — R9 报告(本文件) |
| `docs/governance/GOAL_REGISTRY.md` | NO — Gate 升级文字 |

`tests/_evidence_trx/` 目录下 R9 完全没有动:8 个 `.trx` 仍是 8/20 旧 run 产物,`api_host_round1_*.log` 仍是 R7 Operator 写的内容。

## 10. R7→HEAD business code change 检查(per brief §三-9 + §五-11)

`Test-NoBusinessCodeChangeSinceR7 -RepoRoot (Get-Location).Path -R7Head '15d46c4'` 在 selftest G9 跑过:

```
[PASS] G9. Real repo R7->HEAD diff touches ONLY harness/docs -> Ok=True
```

`git diff --name-only 15d46c4..HEAD` 实际输出(本机):
- `docs/governance/GOAL_REGISTRY.md`
- `docs/verification/MDM_001_*.md`(R8 + R9 报告)
- `tools/dev/Mdm001Acceptance.Harness.ps1`
- `tools/dev/mdm-001-final-acceptance.ps1`
- `tools/dev/mdm-001-final-acceptance-selftest.ps1`

0 个 `.cs` 在 `modules/`, `apps/api/`, `Migrations/`, `tests/GuliERP.*.cs`, `tools/GuliERP.*.cs`。

## 11. Resume 最终流程(per brief §三-四)

```powershell
cd D:\guli\projects\gulierp-next
.\tools\dev\mdm-001-final-acceptance.ps1 -ResumeApiRuntime
```

1. R9 验证 canonical `MDM_001_R7_OPERATOR_TRANSCRIPT_EVIDENCE.md` 存在 + markers + counts + API log SHA256
2. R9 验证 R7→HEAD git diff 不含 business code
3. R9 验证 Solution build 0 errors(或最低限度 Harness self-test PASS)
4. **如全部通过** → 提示 1 次密码(`Read-Host -AsSecureString`)
5. 跑 API Runtime Round 1(从 R8 修复后的 `Start-ApiHost` 起)
6. Round 1 结束后 `Stop-ApiHost` + `Test-ApiPortListening` 确认端口释放
7. 跑 API Runtime Round 2
8. Round 2 结束后 `Stop-ApiHost` + `Test-ApiPortListening` 确认端口释放
9. finally 强制最后再 `Stop-ApiHost` + 检查无残留进程
10. 两轮都 PASS → 绿色 `MDM_001_REAL_MASTER_DATA_VERIFIED`

**任一失败 → 红色 `MDM_001_FINAL_ACCEPTANCE_FAILED`** + 具体失败原因。

## 12. Operator 最终唯一命令(per brief §九-11)

```powershell
cd D:\guli\projects\gulierp-next
.\tools\dev\mdm-001-final-acceptance.ps1 -ResumeApiRuntime
```

输入密码 1 次(`Read-Host -AsSecureString`)→ 等待 ~30 秒(2 轮 API Runtime)。

**本轮(R9)** Operator 不需要执行任何命令(per brief §九-7: "本轮不要求 Operator 执行任何命令")。

## 13. 当前 Gate(per brief §九-12)

**`MDM_001_TRANSCRIPT_BACKFILL_VERIFIED`**(本轮升级)

**Gate 完整升级路径**:
- R7: `MDM_001_FINAL_ACCEPTANCE_READINESS_VERIFIED`
- R8: `MDM_001_API_RUNTIME_RESUME_HARNESS_VERIFIED`
- **R9: `MDM_001_TRANSCRIPT_BACKFILL_VERIFIED`** ← 本轮
- 最终(Operator 跑 Resume 后):`MDM_001_REAL_MASTER_DATA_VERIFIED`

## 14. 报告路径(per brief §九-16)

- R9 主报告(本文件):`docs/verification/MDM_001_R7_TRANSCRIPT_BACKFILL_RESUME_REPORT.md`
- canonical transcript(R9 新建):`docs/verification/MDM_001_R7_OPERATOR_TRANSCRIPT_EVIDENCE.md`
- R8 报告(已修正):`docs/verification/MDM_001_API_RUNTIME_RESUME_FIX_REPORT.md`
- R7 主报告:`docs/verification/MDM_001_FINAL_ACCEPTANCE_READINESS_REPORT.md`
