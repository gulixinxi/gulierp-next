# GULIERP Repository Authority — Canonical vs Legacy

> 生效日期: 2026-08-24
> 任务: `GULIERP_NEXT_CANONICAL_REPOSITORY_RECOVERY_001`
> 操作 Agent: Mavis
> 治理来源: `docs/governance/META_GULI_GOVERNANCE_V1.md` (HR-1..HR-10 自治程序规则)

---

## 1. Canonical vs Legacy

| 字段 | **Canonical Repository** | **Legacy Repository** |
|---|---|---|
| 路径 | `D:\guli\projects\gulierp-next` | `D:\guli\gulierp` |
| Solution | `GuliERP.slnx` (2.8 KB) | `GuliERP.sln` (9.5 KB) |
| 目录风格 | `apps/`, `modules/`, `building-blocks/`, `tests/`, `tools/`, `docs/` | `src/GuliERP.<Module>/<Layer>/`, `tests/`, `web/`, `docs/` |
| 提交数 | 172 commits | 82 commits |
| 当前 HEAD | `f376411` (GULIERP_SHELL_FINAL_POLISH_003) | `8a769f4` (P1-004C login 修复) |
| 分支 | `master` (唯一) | `main` (唯一) |
| Git remote | 无(本地仓库) | 无(本地仓库) |
| Foundation 实现 | `modules/foundation/.../Validation/MasterDataCodeValidator` + `ICodeValidator` | `src/GuliERP.Foundation/` (legacy pipeline) |
| MDM 实现 | `modules/mdm/.../Validation/CodeValidationContextExtensions` + `MdmService` (use ForMdm factory) | `src/GuliERP.MDM/` (legacy MdmService) |
| Identity 实现 | `modules/identity/.../Employee*` + `EnterpriseBootstrapService` | `src/GuliERP.IAM/` (空模块占位) |
| 活跃 Goal | API-CONTRACT-ID-001 (VERIFIED), GULIERP-ENTERPRISE-BOOTSTRAP-001 (CLOSED 2026-08-23), Employee Master V1 Domain Implementation (uncommitted) | P1-005 Inventory Architecture & Posting Engine (规划中,不开始) |
| 业务文档 | `docs/business/` (18 份设计:Code Pipeline / Master Data Model V1 / Employee Master V1 / Contact Profile 等) | `docs/goals/` + `docs/verification/` + `docs/reverse-engineering/` (无 employee master 设计) |

**唯一权威决定**:
- `D:\guli\projects\gulierp-next` = **CANONICAL_GULIERP_NEXT** (active development)
- `D:\guli\gulierp` = **LEGACY_GULIERP** (`LEGACY / FROZEN / READ-ONLY`)

---

## 2. 治理规则 (Mandatory)

### Rule 1 — 所有新 GuliERP 开发默认只在 canonical repo

- ✅ 在 `D:\guli\projects\gulierp-next\` 内进行 GuliERP 相关开发
- ❌ 禁止在 `D:\guli\gulierp\` 启动新 Goal、新 PoC、新业务模块
- ❌ 禁止把 `D:\guli\projects\gulierp-next\` 的代码未审计复制到 `D:\guli\gulierp\`(两套代码互不兼容)

### Rule 2 — Agent 开始任务前必须验证三件套

```
1. pwd             — 确认在 canonical 目录
2. git rev-parse HEAD  — 确认 HEAD 是 master 分支且是 canonical 仓库的 SHA
3. git status --short — 确认 working tree 状态,识别 dirty 来源
```

如果 pwd 在 `D:\guli\gulierp`,**STOP** — 不是 canonical 仓库。

### Rule 3 — 如果 pwd 指向 Legacy,必须 STOP

Legacy 仓库 `D:\guli\gulierp` 状态 = `LEGACY / FROZEN / READ-ONLY`。
Agent 不得:
- 启动新 Goal (P1-005 Inventory / P1-006+ / 任何后续)
- 修改 Legacy 仓库的 source / docs / config
- `git reset --hard` / `git clean` / `git checkout --` 覆盖
- 重新打开 P1-004C / P1-005 任何阶段

### Rule 4 — Legacy 只允许只读参考

Legacy 仓库可以作为**历史参考**(POC-001..POC-004 / ERP-VIS-001 / P1-001..P1-004 的过往决策),但:
- 只读 read-only 工具(`Get-Content`, `Select-String`, `git log`)
- **禁止** push / reset / rebase / amend / revert
- **禁止** 把 Legacy 仓库的代码复制到 Canonical(两套互不兼容)
- **禁止** commit 任何文件到 Legacy 仓库

### Rule 5 — 禁止跨两个仓库复制未审计代码

Legacy 仓库的 `src/GuliERP.<Module>/` 跟 Canonical 仓库的 `modules/<Module>/` 是**两套互不兼容的代码流水线**:
- 不同的 Foundation 实现(Legacy: `IExecutionContextAccessor` / Canonical: `RequestContextAccessor` + `ICodeValidator`)
- 不同的 ErrorCode 命名空间(Legacy: `Foundation_` 前缀 / Canonical: 模块前缀)
- 不同的 DI 注册模式(Legacy: `GuliERPFoundationServiceCollectionExtensions` / Canonical: `modules/*/DependencyInjection.cs`)
- 不同的测试基类(Legacy: `ArchitectureBoundaryTests` / Canonical: per-module Facts pattern)

Agent 不得:
- 把 Legacy 的 `src/GuliERP.MDM/` 复制到 Canonical 的 `modules/mdm/`(两者的 `MdmService` 实现完全不同)
- 把 Canonical 的 `modules/foundation/.../Validation/MasterDataCodeValidator` 复制到 Legacy(两者的 `CodeValidationContext` 命名空间不兼容)
- 把任何一个仓库的 dbcontext / migration / 实体复制到另一个

### Rule 6 — 聊天摘要不得覆盖实际 git / repository evidence

- 摘要描述的工作必须以**实际 git 状态 / 实际文件系统 / 实际 dotnet build 输出**为证据
- 如果摘要描述的文件、commit、测试在仓库中**不存在**,Agent 必须主动声明 **STATE_MISMATCH** 而非基于摘要继续
- 仓库证据规则:
  - 文件存在 = `Test-Path` + `Get-ChildItem` 真实枚举
  - 文件状态 = `git status --short` 输出
  - Build 状态 = `dotnet build GuliERP.slnx -c Release` 退出码
  - 测试状态 = `dotnet test --no-build` 的 `Passed` / `Failed` 数字
  - 绝对不能引用未运行命令验证的"摘要里说的数字"

---

## 3. Agent 跨仓库行为检查清单 (每个 GuliERP 任务开始前必做)

```powershell
# 步骤 1: 验证 pwd
$pwd = (Get-Location).Path
if ($pwd -like '*\gulierp-next*') {
    Write-Host "CANONICAL OK: $pwd" -ForegroundColor Green
} elseif ($pwd -like '*\gulierp*') {
    Write-Host "LEGACY DETECTED — STOP" -ForegroundColor Red
    Write-Host "切换到 D:\guli\projects\gulierp-next 后再继续" -ForegroundColor Yellow
    exit 1
} else {
    Write-Host "UNKNOWN REPO: $pwd" -ForegroundColor Red
    exit 1
}

# 步骤 2: 验证 HEAD 和 branch
git rev-parse HEAD
git branch --show-current
# 期望: master 分支 + f376411 或更新

# 步骤 3: 验证 working tree 状态
git status --short
# 期望: 没有新 dirty(uncommitted 但 accepted by user 可接受)

# 步骤 4: 验证 build
dotnet build GuliERP.slnx -c Release --nologo
# 期望: 0 errors, 0 warnings(或已知 inherited 警告,标记在 MEMORY.md)

# 步骤 5: 验证测试
dotnet test tests\GuliERP.Foundation.Tests\GuliERP.Foundation.Tests.csproj -c Release --no-build --nologo
# 期望: 68/68 PASS
```

如果任何步骤失败,**STOP** 并报告失败原因,不要继续。

---

## 4. Legacy 仓库最终治理(冻结声明)

**`D:\guli\gulierp` 自 2026-08-24 起进入 `LEGACY / FROZEN / READ-ONLY` 状态**:

- ❌ 禁止: 启动 P1-005 Inventory / 任何后续 P1-XXX
- ❌ 禁止: 修改 `src/GuliERP.*` 任何业务源码
- ❌ 禁止: 修改 `tests/GuliERP.*` 任何测试
- ❌ 禁止: 修改 `web/` 任何页面
- ❌ 禁止: 修改 `docs/goals/` / `docs/verification/` / `docs/reverse-engineering/` 任何文档
- ❌ 禁止: 重新打开 P1-004C 闭环工作
- ❌ 禁止: `git push` / `git reset --hard` / `git rebase` / `git commit --amend` / `git revert`
- ❌ 禁止: `git clean -fd` 删除 P1-004C 残留的 untracked 报告

- ✅ 允许: `git log` / `git show` / `git diff` 只读命令
- ✅ 允许: 任何 `Get-Content` / `Select-String` / `Get-ChildItem` 工具(只读)
- ✅ 允许: 在两套仓库对比报告中作为**历史参考**引用

**未提交 P1-004C 工作**(3 modified + 5 untracked,位于 `D:\guli\gulierp`):
- 这些属于 P1-004C 阶段产物,不是当前任务
- Legacy 仓库冻结后,P1-004C 闭环工作**不会被继续推进**
- 如果用户想恢复 P1-004C,必须先在治理层面解除 Legacy 冻结(本治理文档 Rule 3 失效),且必须显式记录在 `docs/governance/GOAL_REGISTRY.md` 内

---

## 5. 跨仓库引用协议 (Cross-Reference Only, No Code Copy)

如果 Legacy 仓库有某个决策 / 设计 / 测试模式需要被 Canonical 仓库借鉴,流程是:

1. **不要直接复制代码**
2. 在 Canonical 仓库的 `docs/governance/LEGACY_REFERENCES.md` 记录:
   - Legacy 仓库的 commit SHA
   - Legacy 仓库的文件路径
   - 借鉴的具体决策(一句话)
   - 借鉴日期 + 操作 Agent
3. Canonical 仓库在独立设计自己的实现(可能跟 Legacy 完全不同,这是允许的)
4. 借鉴完成后,Legacy 文件**不可触碰**

这确保两套仓库互不污染,又能保留决策传承。

---

## 6. 治理文档版本

| 版本 | 日期 | 变更 |
|---|---|---|
| 1.0 | 2026-08-24 | 初版,随 `GULIERP_NEXT_CANONICAL_REPOSITORY_RECOVERY_001` 任务发布 |

本治理文档本身位于 Canonical 仓库 `docs/governance/`,**不在** Legacy 仓库。

---

**最终决策**:
- Canonical 仓库 = `D:\guli\projects\gulierp-next`
- Legacy 仓库 = `D:\guli\gulierp` (FROZEN, READ-ONLY)
- 所有 GuliERP 开发必须在 Canonical 仓库进行
- 违反本治理规则的 Agent 操作 = `BLOCKED_GOVERNANCE_VIOLATION`
