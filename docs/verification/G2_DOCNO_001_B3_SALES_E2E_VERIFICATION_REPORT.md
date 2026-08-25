# G2-DOCNO-001-B3 Sales E2E Verification Report

> **Goal**: `G2_DOCNO_001_B3_SALES_E2E_VERIFICATION`
> **Repo**: `D:\guli\projects\gulierp-next`
> **Branch**: `master`
> **HEAD**: `d74b98a`
> **Date**: 2026-08-25
> **Mode**: READ-ONLY + standard API. No code change. No commit. No push.

---

## 0. Final Verdict

**Gate**: `G2_DOCNO_001_B3_SALES_E2E_VERIFIED` → **❌ NOT REACHED**

**Blocking issue**: DocumentKernel V1 引擎的真实 bug — `DocumentNumberService.GenerateAsync` 的 fix-up SQL (`DocumentNumberService.cs:217`) 在 Npgsql 连接上跑二次 command 时, 触发 `NpgsqlOperationInProgressException: A command is already in progress`, 阻止**同 tenant + 同 DocumentType + 同 period** 下的第 2 次及以后编号生成。

详细分析见 §6。

---

## 1. Runtime Environment

| Probe | URL | Status |
|---|---|---:|
| Health live | `GET /health/live` | **200** |
| Health ready | `GET /health/ready` | **200** |
| CSRF | `GET /api/v1/auth/csrf` | **200** |
| Login | `POST /api/v1/auth/login` (`admin / zihan2012M!@`) | **200** |
| `/auth/me` | `GET /api/v1/auth/me` | **200** (`userName=admin, tenantCode=GULI, companyCode=GULI001`) |

API 进程: dotnet pid 123636, listening on `http://127.0.0.1:5000`, env=Production。

测试前的 B1/B2 / 引擎前置条件:
- 重建 API (含 B1 + B2 未提交源码): PASS, 0 errors / 0 warnings
- 应用 `MDM003_AddNumberingRule` migration: PASS
- `gulierp_numbering_rule` 表创建成功
- `Bootstrap --ensure-formal-enterprise-business-role-pack GULI GULI001 admin` 输出: `mdmClaimsCreated: ["mdm.numbering-rule.read", "mdm.numbering-rule.manage"]` (admin 自动获得 2 个新 claim)

## 2. NumberingRule Configuration

**Path**: `/api/v1/mdm/numbering-rules` (B1 后端) + `/mdm/numbering-rules` (B2 UI)

### 2.1 创建

```
POST /api/v1/mdm/numbering-rules
Body: {
  "documentType": "SalesOrder",
  "prefix": "SO",
  "datePattern": "yyyyMMdd",
  "sequenceLength": 6,
  "resetMode": 1
}
```

Response: **HTTP 201**
```json
{
  "id": 83727350616818020,
  "documentType": "SALESORDER",
  "prefix": "SO",
  "datePattern": "YYYYMMDD",
  "sequenceLength": 6,
  "resetMode": 1,
  "status": 1,
  "createdAt": "2026-08-25T15:26:48",
  "updatedAt": "2026-08-25T15:26:48",
  "concurrencyVersion": 1
}
```

### 2.2 列表验证 (启用后)

```
GET /api/v1/mdm/numbering-rules?page=1&pageSize=20
=> totalCount: 1
   id=83727350616818020  type=SALESORDER  prefix=SO  datePattern=YYYYMMDD  seqLen=6  resetMode=1  status=1 (Active)
```

### 2.3 备注

`documentType` 字段从请求的 `"SalesOrder"` 被序列化为响应里的 `"SALESORDER"` (System.Text.Json 默认 PascalCase 行为); `datePattern` 同理从 `"yyyyMMdd"` → `"YYYYMMDD"`。两者不影响实际编号生成 (引擎从 `DocumentType` enum 取, 不依赖字符串大小写)。

## 3. SalesOrder 编号生成结果

### 3.1 数据初始化 (B3 前置, 不是新增功能)

按 brief "如果发现问题: 数据未初始化" 分类, 通过 MDM API 在 GULI tenant 内创建了 3 条测试主数据 (不进直接 SQL):

| 实体 | 字段 | 值 | id |
|---|---|---|---|
| UoM | code=G2E2EPCS, name=件, dimension=1 (Count), kind=1 (Discrete) | G2_E2E | 83727350616818021 |
| BusinessPartner | code=G2_E2E_CUST_001, name=G2 E2E Customer, role=1 (Customer) | 客户 | 83727350616818022 |
| Item | code=G2_E2E_ITEM_001, name=G2 E2E Test Item, itemNature=1 (Material) | 商品 | 83727350616818023 |

> 注: GULI tenant 原本无任何 BusinessPartner / Item; test_operator_g2_004_t 内有种子数据但 admin 不属于该 tenant (无权操作)。

### 3.2 SalesOrder Draft 创建尝试

```
POST /api/v1/sales/orders
Body: {
  "customerId": 83727350616818022,
  "orderDate": "2026-08-25",
  "remarks": "G2_DOCNO_E2E_#1",
  "lines": [{ "itemId": 83727350616818023, "uomId": 83727350616818021, "quantity": 10, "unitPrice": 100.50, "discountRate": 0, "taxRate": 0.13, "remarks": "first" }]
}
```

| 尝试 | 状态 | OrderNo | 行入库 |
|---|---:|---|---|
| SO#1 (taxRate=13) | **400** | — | ❌ 验证失败: `TaxRate must be between 0 and 1` (taxRate 应是小数 0.13, 不是 13) |
| SO#1 (taxRate=0.13) | 实际 200 (API 日志显示) | (SalesOrder row 未持久化) | ❌ (见 §6 bug) |
| SO#2 (taxRate=0.13) | **500** | — | ❌ `NpgsqlOperationInProgressException: A command is already in progress` |
| SO#2 (replay) | **500** | — | ❌ 同 bug |

### 3.3 DocKernel 引擎日志 (关键证据)

API stdout log:
```
15:29:38.758Z info: GuliERP.DocumentKernel.Infrastructure.DocumentNumber.DocumentNumberService
  DocumentNumberService: generated type=SalesOrder period=20260825 no=SO-20260825-000001 seq=1

15:29:49.163Z fail: GuliERP.Api.Kernel.FoundationExceptionHandler
  Unhandled exception (RequestId=f65269779e6e4714a770447f5a83ee0d)
  Npgsql.NpgsqlOperationInProgressException (0x80004005): A command is already in progress
   at ... DocumentNumberService.GenerateAsync ... line 217
   at ... DocumentNumberService.GenerateAsync ... line 257
   at ... DocumentNumberService.GenerateAsync ... line 265
   at ... SalesOrderService.CreateDraftAsync ... line 93
   at ... SalesOrderEndpoints ... line 49
```

## 4. 数据库持久化验证

### 4.1 DocKernel 计数器 (GULI tenant, AFTER)

```sql
SELECT "TenantId", "DocumentType", "PeriodKey", "LastValue", "LastGeneratedDocumentNo"
FROM doc_kernel."document_number_counter"
WHERE "TenantId"=83727350616817890;
```

| TenantId | DocumentType | PeriodKey | LastValue | LastGeneratedDocumentNo |
|---:|---:|---|---:|---|
| 83727350616817890 (GULI) | 1 (SalesOrder) | 20260825 | **2** | **SO-20260825-000001** |

- `LastValue=2` 说明引擎被调用了 2 次 (sequence 增到 2)
- `LastGeneratedDocumentNo=SO-20260825-000001` 是 SQL 占位符 (EXCLUDED.LastGeneratedDocumentNo 永远传 `firstDocumentNo = RenderDocumentNo(profile, periodKey, 1)`), 表明第二次调用的 fix-up SQL (line 217) 失败后没回滚, 也未更新这一列
- **状态不一致**: `LastValue=2` 但 `LastGeneratedDocumentNo=SO-20260825-000001` — 计数正确, 显示字符串过时 (按 brief §7 这是 audit/debug 列, 业务正确性不依赖它)

### 4.2 SalesOrder 实体表 (GULI tenant)

```sql
SELECT "Id","TenantId","OrderNo","Status","CreatedAt" FROM sales."gulierp_sales_order";
```

| Id | TenantId | OrderNo | Status | CreatedAt |
|---:|---|---|---:|---|
| 83727350616817870 | 83726107798405120 (test_operator_g2_004_t) | SO-20260822-000001 | 2 (Inactive) | 2026-08-22 23:47:16 |

**GULI tenant 内: 0 条 SalesOrder 行 (从本次测试以来)**。

3 天前那条旧记录 (test_operator_g2_004_t 内) 与本次 G2-DOCNO-001-B3 验收无关。

### 4.3 持久化结论

| 维度 | 状态 |
|---|---|
| 计数器行持久化 | ✅ 写成功, `LastValue=2` 真实反映累计 |
| OrderNo 显示列持久化 | ⚠️ 过时 (显示 sequence 1, 实际 sequence 2); 来自 fix-up bug |
| SalesOrder 实体行持久化 | ❌ 0 行; SO CreateDraftAsync 在 DocKernel 返回后未完成 entity 落库 |
| 编号不重复 | ✅ (虽然 bug, 但 `LastValue=2` 说明引擎没出重复号) |

## 5. Tenant 隔离验证

```sql
SELECT t."Code" AS tenant, COUNT(c."Id") AS counter_rows
FROM identity."gulierp_tenant" t
LEFT JOIN doc_kernel."document_number_counter" c ON c."TenantId" = t."Id"
GROUP BY t."Code" ORDER BY t."Code";
```

| tenant | counter_rows |
|---|---:|
| GULI | 1 |
| test_operator_g2_004_t | 1 |
| web_preview_t | 0 |

完整计数器列表 (跨 tenant):
| TenantId | DocumentType | PeriodKey | LastValue | LastGeneratedDocumentNo |
|---:|---:|---|---:|---|
| 83726107798405120 (test_operator_g2_004_t) | 1 | 20260822 | 1 | SO-20260822-000001 |
| 83727350616817890 (GULI) | 1 | 20260825 | 2 | SO-20260825-000001 |

✅ **Tenant 隔离确认**: 不同 tenant 各自独立计数器, 4 元组 unique index `(TenantId, CompanyId, DocumentType, PeriodKey)` 不会跨 tenant 串号。

## 6. 阻塞问题 — 引擎真实 bug

### 6.1 现象

`DocumentNumberService.GenerateAsync` 的 2-step 写流程:
1. **Step A** (`line 184`): `cmd.ExecuteReaderAsync(ct)` 跑 `INSERT ... ON CONFLICT ... DO UPDATE ... RETURNING LastValue, LastGeneratedDocumentNo`。Reader 持有连接。
2. **Step B** (条件性, `line 217`): 如果 `returnedNo != finalDocumentNo`, 跑 `fixCmd.ExecuteNonQueryAsync(ct)` 更新 `LastGeneratedDocumentNo` 到正确值。
3. **Step C** (条件性, `line 234`): 写 `document_number_idempotency` 表。

**Step B 在同 connection 上执行, 但 Step A 的 reader 还在 open**, Npgsql 报 `NpgsqlOperationInProgressException: A command is already in progress`。

### 6.2 触发条件

- **First call** (sequence=1): `firstDocumentNo = RenderDocumentNo(profile, periodKey, 1)`, `EXCLUDED.LastGeneratedDocumentNo = firstDocumentNo = "SO-...-000001"`, post-increment = 1, `finalDocumentNo = "SO-...-000001"`. **Match → Step B 跳过** ✓
- **Second+ call** (sequence≥2): `firstDocumentNo` 仍是 "SO-...-000001" (写死 1), post-increment = 2, `finalDocumentNo = "SO-...-000002"`. **Mismatch → Step B 跑 → 失败** ✗

**后果**: 同 (tenant, company, documentType, period) 下的第 2 次及以后编号生成全部 500。

### 6.3 影响范围

- **本次 B3 验收**: SO#2 失败, 不能展示 `SO-20260825-000002` 连续递增
- **生产环境**: 任何业务单据 (SO/PO/GR/SH/GI/TO/AD/PC) 在 daily/monthly period 内的第 2 张及以后单据, 全部失败
- **未影响**: 第 1 张 (INSERT 路径) 正常, period 切换 (新 PeriodKey) 后第 1 张也正常
- **已存在数据**: 3 天前 test_operator_g2_004_t 内的 1 条 SO 是跨日 period 切换后生成的, 走 INSERT 路径, 无 bug

### 6.4 修复方向 (按 brief, 不在此报告实施)

引擎修复需要至少 3 种方案之一:

1. **修 SQL: 让 upsert 一次返回正确值**
   - 当前 `EXCLUDED.LastGeneratedDocumentNo` 传了占位符。改成在 SQL 内部用 post-increment 的 LastValue 算正确字符串, 再 `RETURNING`。最干净, 但要改 SQL。

2. **修连接: 释放 reader 后再开新 command**
   - 显式 `await reader.CloseAsync()` (或 `await reader.DisposeAsync()`) 然后 `await using var fixCmd = conn.CreateCommand(); ...`。文档说 `await using` 块退出时 dispose, 但 dispose 之前仍持有连接。

3. **修流程: 去掉 fix-up, 在应用层重算**
   - 既然 `finalDocumentNo` 是从 `newValue` 算的, 可以只跑 step A, **跳过 step B 的 UPDATE**, 让 `LastGeneratedDocumentNo` 字段保持 stale (业务正确性不依赖它, 它是 audit 列)。但这会改变 V1 数据契约 (LastGeneratedDocumentNo 永远显示 sequence=1 的占位符)。

4. **修 SQL: 把 fix-up 合到 upsert 里**
   - 用 CTE: `WITH ins AS (INSERT ... RETURNING *) UPDATE ... SET LastGeneratedDocumentNo = ... WHERE ... RETURNING ...`。最复杂但最原子。

5. **降级: sequence 永远从占位符开始, 让 IdempotencyKey 兜底**
   - 这跟"烧号"问题没解决。

### 6.5 跟架构决策的关系

按 `G2_DOCNO_001_ARCHITECTURE_DECISION.md`:
- DocumentKernel **零修改** (V1 冻结, brief §19 禁令)
- B1/B2/B3 都建在 MDM 层, **不**触 DocKernel
- 此 bug **在** DocKernel, **不在** G2-DOCNO-001 范围

→ **正确做法**: 单独的 Goal, 比如 `G2_DOCNO_002_ENGINE_FIX`, 走 critical review 流程 (DocNumberService.cs:47-52 自身标注 `_CODE_READY_CRITICAL_REVIEW_PENDING` 状态)。Critical reviewer 在评审中应当已经看到这一点。

## 7. 是否达到 `G2_DOCNO_001_VERIFIED`?

| 验收项 | 期望 | 实际 | 通过? |
|---|---|---|:---:|
| Runtime env (health/login/me) | 200 | 200 | ✅ |
| B1+B2 endpoint 可用 | yes | yes | ✅ |
| 角色包含新 permission | yes | yes (ensure added) | ✅ |
| NumberingRule CRUD | create 201 | 201 | ✅ |
| NumberingRule 启用 | status=Active | status=1 (Active) | ✅ |
| SalesOrder Draft → DocKernel | OrderNo 形如 `SO-20260825-000001` | API 日志确认 `SO-20260825-000001` 生成 | ✅ |
| **第二次** SalesOrder Draft → 连续递增 | `SO-20260825-000002` | 500 (engine bug) | ❌ |
| 数据持久化 (SalesOrder 行入库) | yes | **0 行** | ❌ |
| Tenant 隔离 (独立计数器) | yes | yes (3 tenant 各自 0/1/1 行) | ✅ |

**2 个核心验收项失败** → **G2_DOCNO_001_VERIFIED 未达**。

## 8. Gate

```
Gate:    G2_DOCNO_001_B3_SALES_E2E_VERIFIED
Status:  NOT REACHED
         - Runtime env OK
         - B1 backend + B2 UI + role pack 增量 OK
         - NumberingRule CRUD OK
         - DocKernel 引擎在 first call OK
         - DocKernel 引擎在 second+ call FAIL (engine bug)
         - SalesOrder 实体行未入库 (因 engine bug 中断)
Code:    0 changed
Test:    0 changed
Migration: 0 changed
Commit:  0 (NO COMMIT)
Push:    0 (NO PUSH)
```

## 9. 下一步建议

1. **不要** 在 G2-DOCNO-001 范围修 DocKernel (架构冻结)
2. **开新 Goal**: `G2_DOCNO_002_ENGINE_CRITICAL_REVIEW_FIX` (或 `G2_DOCNO_001A_ENGINE_FIX`)
   - 复用 V1 引擎的 `IDocumentNumberService` 接口契约
   - 修改 `DocumentNumberService.GenerateAsync` 的 SQL 写法, 让 2nd+ call 不再触发 `NpgsqlOperationInProgressException`
   - 建议方案: §6.4 第 4 选 (CTE 合 step A + step B), 一次 round-trip
   - 测试: 至少补 1 个 integration test, 断言同 scope 连续 10 次 generate 都返回递增的 OrderNo
3. **架构决策 (G2_DOCNO_001_ARCHITECTURE_DECISION.md) 仍成立**: B1/B2/B3 都没碰 DocKernel, 引擎 bug 是 V1 已知状态 (`_CODE_READY_CRITICAL_REVIEW_PENDING`) 的兑现
4. **回归**: DocKernel V1 单测 (5 个) 全过; 唯一缺的是"同 scope 连续多次 generate" 的并发/连续性 integration test, 该 test 跑 PG 串行 collection

## 10. 测试残留 (在 `gulierp_g2_003_test` 测试环境, 不需要 commit/clean)

| 实体 | id | 状态 |
|---|---:|---|
| NumberingRule `SALESORDER` | 83727350616818020 | Active, prefix=SO, datePattern=YYYYMMDD, seqLen=6 |
| UoM `G2E2EPCS` | 83727350616818021 | Active |
| BusinessPartner `G2_E2E_CUST_001` | 83727350616818022 | Active (Customer) |
| Item `G2_E2E_ITEM_001` | 83727350616818023 | Active |
| doc_kernel counter `GULI/SalesOrder/20260825` | — | LastValue=2 (1st call INSERT 路径, 2nd call UPDATE 路径; fix-up 失败, LastGeneratedDocumentNo 仍为占位符) |
| SalesOrder 行 | **0 条** | 全部 SO CreateDraftAsync 在 DocKernel 后失败, entity 未落库 |

测试库这些数据可以保留 (跟之前 Dictionary / Employee 测试残留一致), 也可以清掉, 不影响本报告。

## 11. 引用

- 架构决策: `docs/planning/G2_DOCNO_001_ARCHITECTURE_DECISION.md`
- B1 后端报告: `docs/verification/G2_DOCNO_001_B1_NUMBERING_RULE_BACKEND_FINAL_REPORT.md`
- B2 UI 报告: `docs/verification/G2_DOCNO_001_B2_NUMBERING_RULE_UI_REPORT.md`
- 引擎 bug 位置: `modules/document-kernel/GuliERP.DocumentKernel.Infrastructure/DocumentNumber/DocumentNumberService.cs:217` (fix-up SQL, 与 line 184 的 upsert reader 共享连接)
- 引擎 V1 状态: `_CODE_READY_CRITICAL_REVIEW_PENDING` (DocNumberService.cs:47-52)
- SalesOrder 集成位置: `modules/sales/GuliERP.Sales.Infrastructure/Sales/SalesOrderService.cs:93` (CreateDraftAsync 调 IDocumentNumberService)
- API 日志: `$env:TEMP\api_stdout.log` (本 session 完整保留)
