# G2-DOCNO-001 Architecture Decision

> **Goal**: `G2-DOCNO-001` — 实施边界架构裁决
> **Role**: Architecture Decider (Mavis, PM/Doc Agent)
> **Inputs**:
> - `docs/planning/G2_DOCNO_001_ASSET_AUDIT_REPORT.md` (资产清点)
> - `docs/planning/G2_DOCNO_001_CODE_ASSET_REVIEW.md` (代码面扫描)
> **Date**: 2026-08-25
> **Mode**: READ-ONLY. No code. No commit. No push.
> **Gate**: `G2_DOCNO_001_ARCHITECTURE_DECIDED`

---

## 0. 冲突识别

两份报告**结论冲突**, 必须先 reconcile 才能定边界:

| 报告 | 推荐方案 | 关键论据 |
|---|---|---|
| `ASSET_AUDIT_REPORT` (Mavis, 14:13) | **B. 新增 MDM NumberingRule 模块** | DocKernel V1 冻结 (brief §19 禁 runtime config), 5 个单测 contract 假设, critical review pending 状态; MDM 已有 master data 模式 |
| `CODE_ASSET_REVIEW` (14:15) | **A. 扩展已有 DocumentKernel** | 引擎已完整 (counter/idempotency/profile/migration/tests); `modules/sales/...SalesOrderService.cs` **已注入** `IDocumentNumberService` 并在 `CreateDraftAsync` 调用 |

**关键发现 (Code Review 补完 Audit 盲点)**: `modules/sales/GuliERP.Sales.Infrastructure/Sales/SalesOrderService.cs` 已经注入 `IDocumentNumberService` 并调用 — 这是 Audit 漏掉的文件 (Audit 只看了 `apps/api/GuliERP.Api/Sales/SalesOrderEndpoints.cs`, 没扫 `modules/sales/`)。Audit 的"B3_SALES_INTEGRATION"任务因此**不需要**, 集成已就位。

---

## 1. 架构决策 (One-liner)

**HYBRID: DocKernel 引擎冻结 (零修改) + MDM 新增 NumberingRule 管理模块 (Plan B 模块归属 + Plan A 复用) + SalesOrder 集成已就位 (Code Review 发现) + 后续 PO / Inventory 沿用 SalesOrder 模式**。

---

## 2. 5 个问题裁决

### Q1. NumberingRule 是否属于 MDM? → **是, MDM 管辖, 但仅 AUDIT/DISPLAY, 不做 OVERRIDE**

| 决策 | NumberingRule 实体放 `mdm` schema, 由 `MdmDbContext` 管理, 模块归属 GuliERP.Mdm |
|---|---|
| 理由 | (a) Dictionary / Employee 都在 MDM, NumberingRule 跟它们同属 master data <br> (b) DocKernel 引擎按 V1 brief §19 冻结, 不能扩运行时配置 <br> (c) NumberingRule 是**对引擎当前行为的查询/审计投影**, 不是 override 引擎的写入路径 |
| 限制 | NumberingRule 表是**只读视图**: 字段是 (TenantId, DocumentType, CustomPrefix?, CustomResetPeriod?, CustomSequenceLength?, IsActive) — 不被引擎消费, 只被 UI / API 暴露给运营方"看引擎现在按什么规则跑" |
| 写入路径 | NumberingRule 的写入只在运营方手工调整时发生; 引擎不读它; 引擎仍然读 V1 冻结的 `DocumentTypeProfileCatalog` |

### Q2. DocumentKernel 是否保持零修改? → **是, 零修改**

| 决策 | `modules/document-kernel/**` **零行** 代码修改 |
|---|---|
| 理由 | (a) V1 brief §19 明确禁止运行时配置 profile <br> (b) 5 个单测的 contract 假设 (`IDocumentNumberService_Has_Only_GenerateAsync_No_SetDocumentNo_Method`, `DocumentNumberCounter_Has_No_Mutator_Methods`, `DocumentType_Enum_Has_Exactly_8_Frozen_Values` 等) 不能破 <br> (c) 引擎标注 `BUSINESS_DOCUMENT_KERNEL_V1_CODE_READY_CRITICAL_REVIEW_PENDING` 状态保留, 等独立 critical review <br> (d) 4 元组 unique index + `INSERT ... ON CONFLICT ... RETURNING` 原子性保证, 不需要再扩 |
| 边界 | `IDocumentNumberService` 接口签名不变; `DocumentNumberRequest` / `DocumentNumberResult` 不变; `DocumentType` 枚举不变; `DocumentTypeProfileCatalog` 不变; 2 张表不变; 1 个 migration 不变 |
| 例外 | 仅当**未来** V1.5+ 决策正式允许运行时 override 时, 才考虑加 `profileOverride` 参数到 `GenerateAsync` — **不属本 Goal 范围** |

### Q3. SalesOrder 如何调用? → **已就位, 不需要新加调用点**

| 决策 | SalesOrder 调用 `IDocumentNumberService.GenerateAsync(...)` **已就位** |
|---|---|
| 现状 | `modules/sales/GuliERP.Sales.Infrastructure/Sales/SalesOrderService.cs` (Code Review 发现) <br> • 构造函数注入 `IDocumentNumberService numbers` <br> • `CreateDraftAsync` 调用: `DocumentType.SalesOrder` + 业务日期 + idempotencyKey + actorId <br> • 返回 `DocumentNo` 写入 `SalesOrder.OrderNo` 字段 <br> • `SalesOrderConfiguration` 有 `(TenantId, CompanyId, OrderNo)` 唯一索引 |
| 含义 | G2-DOCNO-001 不需要 B3_SALES_INTEGRATION 任务 (从我的 Audit 移除) |
| 验收 | 端到端: login → POST `/api/v1/sales-orders` (Create Draft) → 200 → `OrderNo` 形如 `SO-20260825-000001` |
| 后续 | **PurchaseOrder / Inventory** 在各自模块正式落地时, 沿用 SalesOrder 模式 — 不复制 counter / idempotency 逻辑, 只调 `IDocumentNumberService` |

### Q4. Migration 边界? → **mdm 加 1 张新表, doc_kernel 完全不动**

| Schema | 动作 | 表 |
|---|---|---|
| `doc_kernel` | **零变更** | `document_number_counter` / `document_number_idempotency` 维持现状 |
| `mdm` | **新增 1 张表 + 1 个 migration** | `gulierp_numbering_rule` <br> (字段: Id, TenantId, DocumentType, CustomPrefix?, CustomResetPeriod?, CustomSequenceLength?, IsActive, audit fields, ConcurrencyVersion) |
| `identity` | 零变更 | — |
| `foundation` | 零变更 | — |

| 决策 | 1 个新 migration: `MdmDbContext` 加 `AddMdmNumberingRules` <br> 唯一索引: `(TenantId, DocumentType)` (一个 tenant 对一个 DocumentType 至多 1 条 active rule) |
|---|---|
| 序列 | 复用 `identity.gulierp_hilo_sequence` (跟现有所有 MDM / DocKernel / Identity 一致) |
| 不动 | `doc_kernel.document_number_counter` / `document_number_idempotency` (V1 冻结); 现有 3 个 MDM migration (`InitializeMdmSchema` / `BusinessPartnerWarehouseLocation` / `AddMdmDictionaryTypesAndItems`) 零变更 |

### Q5. Permission 边界? → **MDM 新增 2 个 permission, 增量到 MdmOperator role pack**

| 新增 permission code | 用途 |
|---|---|
| `MdmPermissions.NumberingRuleRead = "mdm.numbering-rule.read"` | GET endpoints |
| `MdmPermissions.NumberingRuleManage = "mdm.numbering-rule.manage"` | POST/PUT/DELETE endpoints |

| Policy | 用途 |
|---|---|
| `MdmPolicies.NumberingRuleRead` = `"GuliERP.Permission:mdm.numbering-rule.read"` | 注册为 ASP.NET Core Authorization policy |
| `MdmPolicies.NumberingRuleManage` | 同上 |

| Role pack 增量 | `EnterpriseBusinessRolePacks.MdmOperator` 的 Permissions 数组追加 2 项 (mdm.numbering-rule.read / mdm.numbering-rule.manage) |
|---|---|
| 含义 | admin 通过 `ERP_MDM_OPERATOR` 自动获得; 已有 `gulierp-identity-bootstrap --ensure-formal-enterprise-business-role-pack GULI GULI001 admin` 流程会幂等加 claims |
| 不动 | `ERP_SYSTEM_ADMIN` 不接收 (业务 operator 角色 vs 系统管理角色 边界) |
| 不动 | `ERP_SALES_OPERATOR` / `ERP_EMPLOYEE_OPERATOR` 不接收 (跟 NumberingRule 业务无关) |

---

## 3. 实施路径 (3 个 Codex Task)

| # | Task | 范围 | 不动 |
|---|---|---|---|
| 1 | `G2_DOCNO_001_B1_NUMBERING_RULE_BACKEND` | MDM: 1 entity + 1 service + 1 endpoint + 1 migration + 2 个新 permission + role pack 增量 + 12 unit + 4 integration tests | DocKernel 0 行; `doc_kernel` 0 变更 |
| 2 | `G2_DOCNO_001_B2_NUMBERING_RULE_UI` | web: `NumberingRuleList.vue` + API client + 主数据中心卡片 + 菜单 (仿 Dictionary 模板) | DocKernel 0 行; backend service 0 改 |
| 3 | `G2_DOCNO_001_B3_SALES_E2E_VERIFICATION` | **不是新代码** — 是 runtime 验收, 跑端到端: login → POST Create Draft → 200 → `OrderNo = SO-20260825-000001` (复用 SalesOrder 既有集成) | 0 改 |

**Code Review 引入的修正**: 删除原 Audit 提的 `B3_SALES_INTEGRATION` (SalesOrder 已集成), 改为 `B3_SALES_E2E_VERIFICATION` (验收已有集成)。

---

## 4. 严格边界 (Scope Boundary)

### 4.1 DO (允许)

- ✅ 新建 1 个 `mdm` schema 表 (`gulierp_numbering_rule`) + 1 个 migration
- ✅ 新增 2 个 MDM permission code + 2 个 policy
- ✅ 增量 `EnterpriseBusinessRolePacks.MdmOperator` 加 2 个 permission
- ✅ 新建 MDM NumberingRule backend (entity / service / endpoint / tests)
- ✅ 新建 web UI (list page / API client / menu / workbench card)
- ✅ 跑端到端 runtime 验收 (login → SalesOrder Create Draft → OrderNo 格式)
- ✅ 跑 DocumentKernel 既有 integration tests (确保引擎没被破坏)

### 4.2 DO NOT (禁止)

- ❌ 改 `modules/document-kernel/**` 任何文件 (engine frozen)
- ❌ 改 `doc_kernel.*` 任何表 / 索引 / migration
- ❌ 改 `IDocumentNumberService` / `DocumentNumberRequest` / `DocumentNumberResult` / `DocumentType` 枚举
- ❌ 改 `DocumentTypeProfileCatalog` (V1 8 项 frozen)
- ❌ 改 Identity / Bootstrap / G2-005 / G2-MDM 已完成模块 (除了 role pack 增量 1 处)
- ❌ 改 SalesOrder / Purchase / Inventory 业务代码
- ❌ 新增运行时 override 路径 (V1 brief §19 禁令)
- ❌ commit / push

### 4.3 与已有约束的对齐

| 已有约束 | 本次决策如何对齐 |
|---|---|
| `G2-005` (Authorization) 不动 | 仅增量 MdmOperator 2 个 permission, 不改 policy / handler / Identity 架构 |
| `G2-MDM` 已完成 (UOM/Dictionary/Employee) 不动 | NumberingRule 走 MDM 标准模式 (跟 Dictionary 一样), 复用已有 role pack / workbench / API client pattern |
| DocKernel V1 冻结 | **零修改**, 引擎维持 `CODE_READY_CRITICAL_REVIEW_PENDING` 状态 |
| 跨 tenant 隔离 | NumberingRule `(TenantId, DocumentType)` 唯一, 用 `ICurrentTenant` 过滤查询 |
| 编号生成原子性 | 不动 (4 元组 unique index 仍在 DocKernel 引擎内) |

---

## 5. Gate

```
Gate:    G2_DOCNO_001_ARCHITECTURE_DECIDED
Status:  DECIDED (HYBRID: DocKernel 冻结 + MDM 新增 NumberingRule + SalesOrder 集成已就位)
Code:    0 changed
Test:    0 changed
Migration: 0 changed
Commit:  0 (NO COMMIT)
Push:    0 (NO PUSH)
```

待用户授权方案 → 进入 Codex B1 (NumberingRule Backend)。

---

## 6. 待用户确认

请确认 / 修改以下任一决策:

1. **Q1 答复 (NumberingRule 在 MDM, AUDIT/DISPLAY only)**: 接受 / 改 OVERRIDE 允许 / 改 DocKernel 管辖
2. **Q2 答复 (DocKernel 零修改)**: 接受 / 放开允许扩
3. **Q3 答复 (SalesOrder 已集成, 移除 B3_INTEGRATION, 改为 B3_E2E_VERIFICATION)**: 接受 / 重写集成
4. **Q4 答复 (mdm 加 1 张新表 + 1 migration, doc_kernel 不动)**: 接受 / 改 DocKernel 加 NumberingRule
5. **Q5 答复 (2 个 MDM permission, 增量 MdmOperator role pack)**: 接受 / 改独立 role pack / 改 ERP_SYSTEM_ADMIN

任一项 YES, 进入 Codex B1; 任一项 NO, 给修订指令。
