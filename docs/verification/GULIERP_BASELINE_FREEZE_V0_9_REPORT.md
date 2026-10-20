# GULIERP_BASELINE_FREEZE_V0_9_FINAL_REPORT

日期：2026-09-10。

**结论：GULIERP_BASELINE_FREEZE_V0_9 已完成本地冻结整理，当前 HEAD 可作为 V0.9 本地基线。** 本轮完成初始107项继承变更审计、变更分类、敏感输出遮罩、安全离线回归入口、文档对齐、分组本地提交、主工作树验证与 clean clone 复现验证。未执行 push，未启动 Inventory 开发。

## 1. 项目当前定位

GuliERP-Next 是开发中的 .NET 10 / Vue 3 ERP 基线项目。当前阶段目标不是扩展业务功能，而是把已有成果整理成稳定、可复现、可继续开发的 V0.9 基线。

当前真实能力：

- 已实现：Identity 基础能力、MDM、Sales Order、Purchase Order 基础能力。
- 未完成：Inventory、Manufacturing、Finance。
- 数据库实现：Npgsql / PostgreSQL。
- 认证方式：Cookie 会话、CSRF、Policy 授权。
- 本轮未新增库存代码、未新增采购/销售业务功能、未修改数据库业务模型、未修改 API 行为、未修改前端业务页面。

当前 Git 状态：

- Branch：`master`
- 已验证 HEAD：`cd1040b`
- Push 状态：未 push
- 主工作树状态：本报告写回前为 clean；本报告写回后需随最终报告提交形成最终 HEAD。

## 2. Phase 1：Worktree Baseline Audit

已生成：`docs/verification/GULIERP_WORKTREE_BASELINE_AUDIT_V1.md`。

执行内容：

- 采集当前 branch、HEAD commit、git status、modified/deleted/untracked 文件、最近20次 commit。
- 对初始工作树做逐文件数量统计。
- 记录基线冻结风险与后续处理建议。

初始审计结果：

- 初始待处理路径：107 项。
- Modified：34 项。
- Deleted：3 项。
- Untracked：70 项。
- 暂存区：无暂存。

风险说明：初始工作树包含大量继承 WIP，不能用一次性 `git add .` 或批量清理处理；必须逐项分类、逐项保留证据。

## 3. Phase 2：Change Classification

已生成：`docs/verification/GULIERP_CHANGE_CLASSIFICATION_V1.md`。

分类结果：

| 分类 | 数量 | 处理结论 |
|---|---:|---|
| A_KEEP | 11 | 必须保留 |
| B_COMMIT_READY | 0 | 初始审计时无直接提交项 |
| C_TEMPORARY | 17 | 临时/本地产物，保留但不提交 |
| D_REVIEW_REQUIRED | 79 | 需要审查后再分组处理 |

后续复核中，79 项 D 类被进一步处置为：

- 76 项转入可提交分组。
- 3 项作为本地 quarantine 保留并精确 ignore，不删除、不提交。

提交分组包括：

- baseline/dev-tools
- baseline/docs-and-verification
- baseline/reference-docs
- baseline/remove-orphan-mocks
- baseline/sales-v1-frontend-inherited
- baseline/sales-v1-inherited
- baseline/tests

## 4. Phase 3：Security Audit

审计范围：`tests/`、`modules/*Tests/`、`tools/`、`scripts/`。

重点关键词：`ConnectionString`、`Password`、`Username`、`Host=`、`Database=`、`PGPASSWORD`。

已修复问题：

- 测试输出中可能直接打印连接串的位置改为固定遮罩，例如 `Host=***;Database=***;Username=***;Password=***;`。
- Operator evidence 脚本对 stdout/stderr 和诊断块增加脱敏，避免连接信息写入证据文件。
- `mdm-001-final-acceptance-selftest.ps1` 的自测 fixture 改为 sentinel placeholder，避免真实形态连接串作为测试样本泄漏。
- 新增敏感扫描工具，扫描 tracked 与非 ignored 的 untracked 文本文件，只输出统计，不输出值。

新增安全/验证工具：

- `tools/dev/verify-baseline-offline.ps1`
- `tools/dev/baseline-v09-security-selftest.ps1`
- `tools/dev/baseline-sensitive-scan.py`

安全验证结果：

- `baseline-v09-security-selftest.ps1`：PASS，43 checks。
- `baseline-sensitive-scan.py`：PASS，`candidateCount=0`，`reviewedCount=319`。
- 扫描不打印敏感值；历史日志、ignored 文件、二进制产物和 Git 历史 blob 不在本次普通扫描范围。

## 5. Phase 4：Documentation Alignment

已对 README 与 docs 做对齐，修正明显过期描述：

- .NET 版本：对齐 .NET 10 / net10.0。
- 认证方式：Cookie 会话、CSRF、Policy 授权。
- 数据库：Npgsql / PostgreSQL。
- 已实现模块：Identity 基础能力、MDM、Sales Order、Purchase Order 基础能力。
- 未实现模块：Inventory、Manufacturing、Finance。

原则：只修正文档事实，不夸大完成度，不把本地测试通过写成生产验收通过。

## 6. Phase 5：Final Baseline Report

最终产物：本文件 `docs/verification/GULIERP_BASELINE_FREEZE_V0_9_REPORT.md`。

本轮形成的本地提交：

| Commit | Message | 内容 |
|---|---|---|
| `9a3ef3e` | `chore(baseline): add v0.9 verification safety tools` | 安全工具、脱敏修复、ignore 与 env 示例 |
| `2423bc5` | `chore(baseline): version inherited sales v1 backend` | 继承 Sales V1 后端/测试基线化 |
| `8153503` | `chore(baseline): version inherited sales v1 frontend` | 继承 Sales V1 前端基线化与旧 mock 删除记录 |
| `1c3b5ce` | `docs(baseline): document v0.9 closure evidence` | 文档、审计和验证证据 |
| `31f520b` | `test(baseline): preserve identity integration assertions` | Identity integration 测试断言保留 |
| `e1aac84` | `chore(baseline): include api tests in solution restore` | solution 复现性修复，纳入 Api.Tests 与工具项目 |
| `cd1040b` | `docs(baseline): finalize v0.9 closure report` | 冻结报告初版与提交台账 |

## 7. 已验证能力

主工作树与 clean clone 已验证：

- .NET restore：PASS。
- .NET build：PASS，0 warnings，0 errors。
- 离线基线回归：PASS，755/755。
- API source regression：PASS，Sales Order 当前编辑/详情结构断言已按真实继承 UI 更新。
- 前端 typecheck：PASS。
- 前端 build：PASS。
- 安全自测：PASS，43 checks。
- 敏感扫描：PASS，`candidateCount=0`。
- `git diff --check`：PASS。

离线基线回归项目：

| Project | Passed | Failed |
|---|---:|---:|
| GuliERP.Foundation.Tests | 68 | 0 |
| GuliERP.Identity.Tests | 103 | 0 |
| GuliERP.Identity.Bootstrap.Tests | 64 | 0 |
| GuliERP.Mdm.Tests | 336 | 0 |
| GuliERP.Mdm.Bootstrap.Tests | 53 | 0 |
| GuliERP.DocumentKernel.Tests | 44 | 0 |
| GuliERP.Sales.Tests | 37 | 0 |
| GuliERP.Purchase.Tests | 18 | 0 |
| GuliERP.Api.Tests | 32 | 0 |
| 合计 | 755 | 0 |

clean clone 复现证据：

- Clone HEAD：`cd1040b`
- Clone 初始 status：0 项
- clean clone .NET restore/build/offline/security/sensitive-scan/diff-check：PASS
- clean clone `apps/web` npm ci/typecheck/build：PASS
- Vite 构建中出现 `@vueuse/core` Rollup PURE annotation 警告；构建退出码为 0，属于已知上游依赖噪音。

## 8. 未完成能力与边界

未完成能力：

- Inventory
- Manufacturing
- Finance
- 采购收货 / 销售发货 / 库存履行闭环
- 正式实库 CRUD 验收
- 浏览器人工验收
- 生产部署、恢复和性能验证

已知边界：

- 本轮未运行实库迁移、seed、operator 脚本或浏览器验收。
- `ReferenceSeedIntegrationFacts` 仍属于需要真实 DB 环境的测试；离线基线命令按 FQN 排除它。
- 历史 `mdm-001-final-acceptance-selftest.ps1` 对旧 R7 到 HEAD 的历史差异有旧假设，整套历史自测仍不能作为当前 V0.9 全绿门禁；其中脱敏相关检查已通过。
- ignored 的本地临时文件与 quarantine 文件保留在本机，不纳入提交。

## 9. 风险列表

| 风险 | 状态 | 后续建议 |
|---|---|---|
| 实库验收未执行 | 已披露 | 下一轮由明确 DB 环境和账号执行 |
| 浏览器/操作员验收未执行 | 已披露 | 下一轮补充人工或 Playwright 验收 |
| 历史自测脚本含旧基线假设 | 已披露 | 单独重构为当前门禁，禁止用弱化断言制造 PASS |
| ignored 本地临时/quarantine 文件 | 已保留 | 人工确认后再决定归档或删除 |
| Inventory 未开始 | 符合冻结阶段原则 | 进入下一阶段后按新契约开发 |

## 10. 下一阶段

下一阶段名称：**GULIERP_INVENTORY_KERNEL_V1**。

当前仅登记下一阶段，不启动实施。进入下一阶段前，应先明确库存内核的边界、数据模型、API 契约、测试策略和迁移策略。

## 11. 冻结判定

判定：`GULIERP_BASELINE_FREEZE_V0_9_VERIFIED_LOCAL_BASELINE`。

含义：当前本地 HEAD 已完成文档、分类、安全和离线回归整理，并通过 clean clone 复现验证，可作为下一阶段开发的 V0.9 本地基线。该判定不等于生产发布、不等于实库验收完成、不等于 Inventory 已开始。
