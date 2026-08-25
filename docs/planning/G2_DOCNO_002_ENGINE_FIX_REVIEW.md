# G2-DOCNO-002 Engine Fix — Critical Review

> **Goal**: `G2_DOCNO_002_ENGINE_FIX_CRITICAL_REVIEW`
> **Trigger**: `G2_DOCNO_001_B3_SALES_E2E_VERIFICATION_REPORT.md` 验收发现 DocumentKernel 引擎真实 bug
> **Role**: 文档 / Review Agent (READ-ONLY)
> **Repo**: `D:\guli\projects\gulierp-next`
> **Branch**: `master`
> **HEAD**: `d74b98a`
> **Date**: 2026-08-25
> **Mode**: READ-ONLY. No code. No commit. No push.
> **Gate**: `G2_DOCNO_002_ENGINE_FIX_CRITICAL_REVIEW_REPORTED`

---

## 0. 1 句话定位

`DocumentNumberService.GenerateAsync` 在 Npgsql **同一连接**上, 先开 reader 跑 `INSERT ... ON CONFLICT ... RETURNING` (line 184), **未关闭 reader** 就跑第二次 `UPDATE` (line 217, fix-up SQL), Npgsql 抛 `NpgsqlOperationInProgressException: A command is already in progress`。

**根因不是 SQL 写错, 是连接状态没释放。**

---

## 1. 根因分析

### 1.1 代码现场 (锁定行)

`modules/document-kernel/GuliERP.DocumentKernel.Infrastructure/DocumentNumber/DocumentNumberService.cs`:

```
line 167  var conn = _db.Database.GetDbConnection();
line 168  await conn.OpenAsync(ct);
line 169  try {
line 170      await using var cmd = conn.CreateCommand();
line 171      cmd.CommandText = upsertSql;
line 172-179 cmd.Parameters.Add(...)
line 182      long newValue = 0;
line 183      string returnedNo = string.Empty;
line 184      await using var reader = await cmd.ExecuteReaderAsync(ct);   // ← 持连接
line 185      if (await reader.ReadAsync(ct))
line 186      {
line 187          newValue = reader.GetInt64(0);
line 188          returnedNo = reader.GetString(1);
line 189      }
line 190      // ⚠️ 缺 await reader.CloseAsync();  ← 连接仍被 reader 占用
line 191      //  ↓  ↓  ↓  ↓  ↓  ↓  ↓  ↓  ↓
line 193      var finalDocumentNo = RenderDocumentNo(profile, periodKey, newValue);
line 194      if (returnedNo != finalDocumentNo)
line 195      {
line 196          await using var fixCmd = conn.CreateCommand();          // ← 同一 conn
line 197-208      ... set SQL ...
line 217          await fixCmd.ExecuteNonQueryAsync(ct);                  // ← 💥 NpgsqlOperationInProgressException
line 218      }
```

### 1.2 SQL 本身 (line 135-151)

```sql
INSERT INTO doc_kernel.document_number_counter
    ("Id", "TenantId", "CompanyId", "DocumentType", "PeriodKey",
     "LastValue", "LastGeneratedDocumentNo", "CreatedAt", "ModifiedAt",
     "ConcurrencyVersion")
VALUES
    (nextval('identity.gulierp_hilo_sequence'),
     @tenantId, @companyId, @documentType, @periodKey,
     1, @firstDocumentNo, @now, @now, 1)        -- @firstDocumentNo = "SO-...-000001" (写死 sequence=1)
ON CONFLICT ("TenantId", "CompanyId", "DocumentType", "PeriodKey")
DO UPDATE SET
    "LastValue" = doc_kernel.document_number_counter."LastValue" + 1,
    "LastGeneratedDocumentNo" = EXCLUDED."LastGeneratedDocumentNo",  -- ← 永远是 sequence=1 的占位符
    "ModifiedAt" = @now,
    "ConcurrencyVersion" = doc_kernel.document_number_counter."ConcurrencyVersion" + 1
RETURNING "LastValue", "LastGeneratedDocumentNo";
```

### 1.3 触发条件矩阵

| 调用序号 | 路径 | post-increment `LastValue` | EXCLUDED.LastGeneratedDocumentNo | `finalDocumentNo` | mismatch? | fix-up 跑? | 结果 |
|---:|---|---:|---|---|:---:|:---:|---|
| 1st | **INSERT** (新 scope) | 1 | `"SO-...-000001"` | `"SO-...-000001"` | ✗ | 不跑 | ✅ 200 |
| 2nd+ | **UPDATE** (同 scope) | 2,3,…N | `"SO-...-000001"` (占位符) | `"SO-...-000002"`, `"SO-...-000003"`, … | ✓ | **跑** | ❌ 500 |

**结论**: bug 只在 2nd+ 调用触发。1st 永远成功 (因为占位符就是 sequence=1 的渲染值, 凑巧 match)。这解释了为什么 `B3` 验收时 SO#1 成功, SO#2 失败。

### 1.4 Npgsql 错误细节

`Npgsql.NpgsqlOperationInProgressException (0x80004005): A command is already in progress`

Npgsql 强制规则: **单 connection 同时只能有 1 个 active command**。`ExecuteReaderAsync` 返回的 reader 持有 connection, 直到显式 `CloseAsync` / `DisposeAsync` / `NextResultAsync`(false)。`await using var reader` 仅在 **scope 退出** 时 dispose, 而 fix-up 在 scope 内执行, 时间上不释放。

### 1.5 为什么 1st 成功、2nd 失败 (而非 1st 失败、2nd 成功)

INSERT 路径下 `EXCLUDED.LastGeneratedDocumentNo = @firstDocumentNo = "SO-...-000001"`, 与 `newValue=1` 渲染出的 `finalDocumentNo` 一致 → 跳过 fix-up → 1st 永远成功。

UPDATE 路径下, SQL 的 SET 子句**重新赋值** `LastGeneratedDocumentNo = EXCLUDED.LastGeneratedDocumentNo` (这个值是 VALUES 里的 `@firstDocumentNo`, 即 sequence=1 的渲染值, **不是** sequence=N 的真实值)。RETURNING 出参 `LastGeneratedDocumentNo` 跟着变 → `returnedNo="SO-...-000001"`, 但 `newValue=2` 渲染的 `finalDocumentNo="SO-...-000002"` → mismatch → 跑 fix-up → 撞 connection → 500。

### 1.6 为什么没被现有测试发现

`tests/GuliERP.DocumentKernel.IntegrationTests/DocumentNumberCounterFacts.cs:75` 有:

```csharp
[SkippableFact]
public async Task GenerateAsync_Increments_Within_Same_Scope()
{
    Skip.IfNot(_fx.IsAvailable, "PGPASSWORD not set; integration test skipped");
    ...
    var r1 = await svc.GenerateAsync(...);   // 1st, INSERT 路径, 通过
    var r2 = await svc.GenerateAsync(...);   // 2nd, UPDATE 路径, fix-up 撞连接 → 异常
    var r3 = await svc.GenerateAsync(...);   // 永远不到
    Assert.Equal(1, r1.SequenceValue);
    Assert.Equal(2, r2.SequenceValue);      // 永远过不去
    Assert.Equal(3, r3.SequenceValue);
}
```

**这个测试就是用来抓这个 bug 的**, 设计正确, 但 `PGPASSWORD` 没设 → `[SkippableFact]` skip → 一直未跑。

`DocumentNumberCounterConcurrencyFacts` 注释也明说: *"This test requires PGPASSWORD. ... Until it runs against a real PG, the brief §14 'concurrency stress' is NOT verified"*。

5 个 unit test 都不走 SQL 路径, 只测纯函数 (Render / PeriodKey / Catalog / Contract), 所以查不出来。

**结论**: 测试是齐的, 只是没跑 (CI 环境缺 PGPASSWORD)。修复后**该测试将自动通过**。

---

## 2. 最小修复方案

按修复侵入度从低到高, 列 3 选。

### 方案 A: 显式关 reader (推荐, 1 行)

**改动**:
```diff
 await using var reader = await cmd.ExecuteReaderAsync(ct);
 if (await reader.ReadAsync(ct))
 {
     newValue = reader.GetInt64(0);
     returnedNo = reader.GetString(1);
 }
+await reader.CloseAsync();   // 释放连接, 让 fix-up 可执行
 var finalDocumentNo = RenderDocumentNo(profile, periodKey, newValue);
```

**优点**:
- 1 行, 改动最小
- 不改 SQL, 不改 DTO, 不改接口
- 保留"INSERT 路径 placeholder 写死 1" 的现有设计
- 测试用 `GenerateAsync_Increments_Within_Same_Scope` 立即通过 (1st + 2nd + 3rd 全部正常)

**风险**:
- 极低。`CloseAsync` 是 reader 的标准释放方法, Npgsql 文档推荐做法
- 1st 调用 fix-up 仍跳过 (placeholder 仍 match), 0 影响
- 2nd+ 调用 fix-up 正常跑, `LastGeneratedDocumentNo` 列更新为真实渲染值
- IdempotencyKey 重放路径: 1st 调用前先查 idempotency 表 (无 reader 占用), 重放不进入 fix-up 路径, 0 影响

**性能**:
- 2nd+ 调用多 1 次 round-trip (fix-up UPDATE)
- 与方案 B (CTE 单 round-trip) 比, 2nd+ 多 1 次网络 RTT
- 在同 scope 高频场景下 (例如 daily batch 1000 张单据), 1ms 级别开销, 可接受

### 方案 B: 用 CTE 单 round-trip (侵入度中等, 0 改 consumer 行为)

**改动**: 重写 upsert SQL, 把 fix-up 合到 CTE:

```sql
WITH ins AS (
    INSERT INTO doc_kernel.document_number_counter
        ("Id", "TenantId", "CompanyId", "DocumentType", "PeriodKey",
         "LastValue", "LastGeneratedDocumentNo", "CreatedAt", "ModifiedAt", "ConcurrencyVersion")
    VALUES
        (nextval('identity.gulierp_hilo_sequence'),
         @tenantId, @companyId, @documentType, @periodKey,
         1, @firstDocumentNo, @now, @now, 1)
    ON CONFLICT ("TenantId", "CompanyId", "DocumentType", "PeriodKey")
    DO UPDATE SET
        "LastValue" = doc_kernel.document_number_counter."LastValue" + 1,
        "ModifiedAt" = @now,
        "ConcurrencyVersion" = doc_kernel.document_number_counter."ConcurrencyVersion" + 1
    RETURNING "TenantId", "CompanyId", "DocumentType", "PeriodKey", "LastValue"
)
UPDATE doc_kernel.document_number_counter AS c
SET "LastGeneratedDocumentNo" = @prefix || '-' || @periodKey || '-' || lpad(ins."LastValue"::text, @seqLen, '0'),
    "ModifiedAt" = @now
FROM ins
WHERE c."TenantId" = ins."TenantId"
  AND c."CompanyId" = @companyId
  AND c."DocumentType" = ins."DocumentType"
  AND c."PeriodKey" = ins."PeriodKey"
RETURNING c."LastValue", c."LastGeneratedDocumentNo";
```

**优点**:
- 0 个 fix-up round-trip, 单 round-trip 原子写
- 性能最优 (高频 daily batch 场景)

**风险**:
- SQL 复杂度上升, 需要仔细测 CTE 行为
- prefix 字符串从参数传入 (在 service 层用 C# RenderDocumentNo 算, 把 prefix / periodKey / seqLen 一起传给 SQL, 不再依赖 `EXCLUDED` 占位符)
- 任何 CTE 语法错误都会让生产环境第 1 张单据就 500, 风险高

**不推荐**为最小修复, 适合作为 V1.5+ 性能优化。

### 方案 C: fix-up 走独立 connection (侵入度低, 0 改主流程)

**改动**:
```diff
 if (returnedNo != finalDocumentNo)
 {
+    await conn.CloseAsync();   // 释放原 connection
     await using var fixConn = new NpgsqlConnection(connStr);
     await fixConn.OpenAsync(ct);
     await using var fixCmd = fixConn.CreateCommand();
     ...
 }
```

**优点**:
- 不依赖 reader 释放, 独立连接
- 不改 SQL

**风险**:
- 引入新 connection string 依赖, 需要 DI
- Npgsql connection pool 行为变化, 性能不可预测
- 多 connection 之间没有事务边界, 如果 fix-up 失败, counter 已 commit 但 LastGeneratedDocumentNo 没更新, 状态不一致 (与现状同, 但新引入 connection 池开销)

**不推荐**, 因为方案 A 更简单 + 不引入新 connection 池依赖。

### 2.1 推荐: **方案 A** (1 行 fix)

| 维度 | 方案 A | 方案 B | 方案 C |
|---|---|---|---|
| 改动行数 | 1 | ~30 (SQL 重写) | ~5 + DI |
| 风险 | 极低 | 中 (CTE 语法错) | 中 (新 conn 池) |
| 性能 (2nd+ 调用) | +1 RTT | 0 变化 | +1 RTT |
| 测试改动 | 0 | 需重写 5 个 integration fact 期望 | 0 |
| 接口改动 | 0 | 0 | 0 |
| 推荐 | ✅ | 仅 V1.5+ 性能 | ❌ |

---

## 3. 是否需要修改接口

**答案: ❌ 不需要。**

| 资产 | 改动 |
|---|---|
| `IDocumentNumberService.GenerateAsync` 签名 | **0 改** |
| `DocumentNumberRequest` record | **0 改** |
| `DocumentNumberResult` record | **0 改** |
| `DocumentType` enum | **0 改** |
| `DocumentTypeProfile` / `DocumentTypeProfileCatalog` | **0 改** |
| `DocumentNumberValidationException` / `UnknownDocumentTypeException` | **0 改** |
| `IDocumentNumberService` DI 注册 (`AddGuliErpDocumentKernel`) | **0 改** |
| 公开 API 端点 (e.g. `/api/v1/mdm/numbering-rules`, `/api/v1/sales/orders`) | **0 改** |

理由: 修复点在 `DocumentNumberService` (internal) 的 `try { ... }` 块内, 不触及接口边界。Service 实现改了, 接口契约没动, 所有 caller (`SalesOrderService.CreateDraftAsync` 等) 完全无感。

---

## 4. 是否影响已有测试

### 4.1 单元测试 (5 个) — 0 影响

| 测试 | 路径 | 是否受影响 |
|---|---|:---:|
| `IDocumentNumberService_Has_Only_GenerateAsync_No_SetDocumentNo_Method` | `DocumentNumberV1ContractTests` | ✗ (检查接口方法名, 不变) |
| `DocumentNumberCounter_Has_No_Mutator_Methods` | `DocumentNumberV1ContractTests` | ✗ (检查 entity 字段, 不变) |
| `DocumentType_Enum_Has_Exactly_8_Frozen_Values` | `DocumentNumberV1ContractTests` | ✗ (检查 enum 数量, 不变) |
| `DocumentNumberResult_DocumentNo_Is_InitOnly` | `DocumentNumberV1ContractTests` | ✗ (检查 record init-only, 不变) |
| `DocumentNumberRequest_All_Properties_Are_InitOnly` | `DocumentNumberV1ContractTests` | ✗ (检查 record init-only, 不变) |
| `RenderDocumentNo_*` (多个) | `DocumentNumberRenderTests` | ✗ (纯函数, 不变) |
| `RenderPeriodKey_*` (多个) | `DocumentNumberPeriodKeyTests` | ✗ (纯函数, 不变) |
| `Request_*` (多个) | `DocumentNumberRequestValidationTests` | ✗ (检查 validation, 不变) |
| `DocumentTypeProfileCatalog_*` (多个) | `DocumentTypeProfileCatalogTests` | ✗ (检查 catalog, 不变) |

### 4.2 集成测试 — **0 个 test 需修改**, 但行为会变化

| 测试 | 路径 | 修复前 | 修复后 |
|---|---|---|---|
| `GenerateAsync_Returns_First_Number_For_Empty_Scope` | `DocumentNumberCounterFacts` | ✅ 通过 (1st 路径) | ✅ 仍通过 (1st 路径, 行为不变) |
| **`GenerateAsync_Increments_Within_Same_Scope`** | `DocumentNumberCounterFacts` | ❌ **skip 状态** (PGPASSWORD 未设); 设了 PGPASSWORD 后会**2nd 异常** | ✅ **真正通过** (3 次 generate, 1/2/3) |
| `GenerateAsync_New_Period_Resets_Counter` | `DocumentNumberCounterFacts` | (skip) | ✅ 通过 (新 PeriodKey 走 INSERT 路径) |
| `GenerateAsync_ProductionOrder_Uses_Monthly_Reset` | `DocumentNumberCounterFacts` | (skip) | ✅ 通过 (新 scope, INSERT 路径) |
| `GenerateAsync_Different_DocumentType_Shares_Same_Period_But_Different_Scope` | `DocumentNumberCounterFacts` | (skip) | ✅ 通过 (新 scope, INSERT 路径) |
| `GenerateAsync_Unknown_DocumentType_Throws` | `DocumentNumberCounterFacts` | (skip) | ✅ 通过 (validation, 不走 SQL) |
| `GenerateAsync_Rejects_NonPositive_TenantId` | `DocumentNumberCounterFacts` | (skip) | ✅ 通过 (validation, 不走 SQL) |
| `Hundred_Concurrent_GenerateAsync_Calls_Produce_Unique_Sequences` | `DocumentNumberCounterConcurrencyFacts` | (skip) | ✅ 通过 (100 并发, 各自走 ON CONFLICT, 1st 走 INSERT, 2-100 走 UPDATE+fix-up, fix-up 现在能跑) |
| `Same_IdempotencyKey_Returns_Same_Number_Without_Re_Incrementing` | `DocumentNumberIdempotencyFacts` | (skip) | ✅ 通过 (1st 走 idempotency check, hit, 不进 fix-up) |
| `Different_IdempotencyKey_Generates_New_Number` | `DocumentNumberIdempotencyFacts` | (skip) | ✅ 通过 (1st 走 upsert, fix-up 跳过, 2nd 走 upsert UPDATE + fix-up 现在 OK) |
| `Migration_DOCKERNEL001_Is_Applied_To_Active_Database` | `DocumentKernelMigrationFacts` | (skip) | ✅ 通过 (schema 不变) |

**结论**: 修复后, 所有 11 个 integration test 都不需要改, **且** 之前 skip 的能跑出真实结果并通过。

### 4.3 新增测试 (推荐, 1 个)

为防止后续回归, 在 `DocumentNumberCounterFacts` 加 1 个:

```csharp
[SkippableFact]
public async Task GenerateAsync_Ten_Consecutive_Calls_Return_Sequence_1_to_10()
{
    Skip.IfNot(_fx.IsAvailable, "...");
    var svc = CreateService();
    var period = DateOnly.FromDateTime(DateTime.UtcNow);
    var periodKey = period.ToString("yyyyMMdd");
    await _fx.ResetCounterScopeAsync(_tenantId, _companyId, (int)DocumentType.SalesOrder, periodKey);

    var docNos = new List<string>();
    for (var i = 1; i <= 10; i++)
    {
        var r = await svc.GenerateAsync(
            new DocumentNumberRequest(DocumentType.SalesOrder, _tenantId, _companyId, period, null, 1));
        Assert.Equal(i, r.SequenceValue);
        Assert.EndsWith($"-{(i).ToString($"D{6}")}", r.DocumentNo);
        docNos.Add(r.DocumentNo);
    }
    Assert.Equal(10, docNos.Distinct().Count());   // no duplicates

    // LastGeneratedDocumentNo column reflects the LAST generated number, not a stale placeholder
    var stored = await _fx.GetLastGeneratedDocumentNoAsync(_tenantId, _companyId, (int)DocumentType.SalesOrder, periodKey);
    Assert.Equal(docNos.Last(), stored);
}
```

(伪代码, 不提交) — 需要 `_fx` 暴露 `GetLastGeneratedDocumentNoAsync` helper, 简单 SELECT 即可。

### 4.4 跨 Asset 影 响 (零)

| 模块 | 影响 |
|---|---|
| `G2_DOCNO_001_B1` (MDM NumberingRule) | 0 (MDM 0 改) |
| `G2_DOCNO_001_B2` (web UI) | 0 (UI 0 改) |
| `G2_DOCNO_001_B3` (E2E 验收) | E2E 重新跑应当过 (2nd+ SO 通过) |
| `SalesOrderService` (modules/sales) | 0 改 (不动 brief 约束) |
| MDM 已完成模块 (UOM/Item/BP/Dictionary/Employee) | 0 改 |
| API 端点 (`/api/v1/...`) | 0 改 |
| 角色 / 权限 | 0 改 |
| Migration | 0 改 (不新增 migration) |

---

## 5. 修复验收标准

修复完成, 必须满足以下 7 条才算 `G2_DOCNO_002_ENGINE_FIX_VERIFIED`:

### 5.1 单元测试 (5 文件, 全部跑过, 不需要新跑)

```
dotnet test tests/GuliERP.DocumentKernel.Tests/ -c Release
  -> DocumentNumberV1ContractTests: 5/5 PASS
  -> DocumentNumberRenderTests:    PASS (不增加数, 与 1 之前一致)
  -> DocumentNumberPeriodKeyTests: PASS
  -> DocumentNumberRequestValidationTests: PASS
  -> DocumentTypeProfileCatalogTests: PASS
```

### 5.2 集成测试 (3 文件, 全部跑过, 需 PGPASSWORD)

```
PGPASSWORD=<conn-pwd> dotnet test tests/GuliERP.DocumentKernel.IntegrationTests/ -c Release
  -> DocumentNumberCounterFacts: 7/7 PASS
     [关键] GenerateAsync_Increments_Within_Same_Scope: PASS (3 次连续, sequence 1/2/3, doc no 001/002/003)
     [关键] GenerateAsync_Returns_First_Number_For_Empty_Scope: PASS
     [关键] GenerateAsync_New_Period_Resets_Counter: PASS
  -> DocumentNumberCounterConcurrencyFacts: 1/1 PASS
     [关键] Hundred_Concurrent_GenerateAsync_Calls_Produce_Unique_Sequences: PASS (100 并发, 100 个 unique sequence, no duplicates)
  -> DocumentNumberIdempotencyFacts: 2/2 PASS
     [关键] Same_IdempotencyKey_Returns_Same_Number_Without_Re_Incrementing: PASS
  -> DocumentKernelMigrationFacts: PASS
```

### 5.3 新增 1 个回归测试 (10 次连续)

```
[新] GenerateAsync_Ten_Consecutive_Calls_Return_Sequence_1_to_10: PASS
  - sequence: 1, 2, 3, ..., 10
  - doc no: SO-...-000001, SO-...-000002, ..., SO-...-000010
  - all 10 distinct
  - LastGeneratedDocumentNo column = SO-...-000010 (not stale)
```

### 5.4 端到端 (G2_DOCNO_001_B3 重跑, 2 次连续 SO)

```
POST /api/v1/sales/orders  (taxRate=0.13, 10 qty)
  -> 201  orderNo=SO-20260825-000001

POST /api/v1/sales/orders  (taxRate=0.13, 5 qty)
  -> 201  orderNo=SO-20260825-000002

GET /api/v1/sales/orders
  -> totalCount=2
  -> 2 rows, distinct orderNo, 连续递增
```

### 5.5 持久化 (counter 行 vs SO 行一致)

```sql
SELECT "LastValue", "LastGeneratedDocumentNo" FROM doc_kernel."document_number_counter"
WHERE "TenantId"=GULI AND "DocumentType"=1 AND "PeriodKey"='20260825';
-- LastValue = 2 (not 1, not 3)
-- LastGeneratedDocumentNo = 'SO-20260825-000002' (not 'SO-20260825-000001' placeholder)

SELECT COUNT(*) FROM sales."gulierp_sales_order" WHERE "TenantId"=GULI AND "CreatedAt" > '2026-08-25 16:00';
-- COUNT = 2 (matches the counter, no burned numbers)
```

### 5.6 Tenant 隔离 (回归, 不能破)

```sql
SELECT "TenantId", "LastValue" FROM doc_kernel."document_number_counter";
-- GULI: 2 (本次新增)
-- test_operator_g2_004_t: 1 (老数据, 不动)
-- web_preview_t: 0 (不增加)
```

### 5.7 Idempotency 行为 (回归, 不能破)

```
POST /api/v1/sales/orders  (idempotencyKey="G2_E2E_REPLAY_001")
  -> 201  orderNo=SO-20260825-000003

POST /api/v1/sales/orders  (idempotencyKey="G2_E2E_REPLAY_001")  ← replay
  -> 201  orderNo=SO-20260825-000003   (same as above, IdempotencyReplayed=true)
  -> counter NOT incremented (still 3)
```

---

## 6. 修复成本估算 (供 Codex 估时)

| 任务 | 估时 | 风险 |
|---|---|---|
| 改 `DocumentNumberService.cs` 加 1 行 (`await reader.CloseAsync();`) | 2 min | 极低 |
| 加 `_fx.GetLastGeneratedDocumentNoAsync` helper | 5 min | 极低 |
| 加 1 个回归测试 (10 次连续) | 15 min | 低 |
| 跑全套 unit + integration test 验证 | 10 min | — |
| **总计** | **~30 min** | **极低** |

无 migration, 无 API 改动, 无 role 改动, 无 UI 改动, 无 seed 改动。

---

## 7. Gate

```
Gate:    G2_DOCNO_002_ENGINE_FIX_CRITICAL_REVIEW_REPORTED
Status:  REPORTED
Code:    0 changed
Test:    0 changed
Migration: 0 changed
Commit:  0 (NO COMMIT)
Push:    0 (NO PUSH)
```

**待用户授权**: 选方案 A (推荐, 1 行) / 方案 B (CTE, 性能优化, 风险中) / 方案 C (独立 conn, 不推荐)

**修复触发**: 等用户拍板方案后, 进 Codex fix 任务 (~30 min, 见 §6) + 重跑 B3 E2E 验证。

---

## 8. 引用

- B3 验收 (触发): `docs/verification/G2_DOCNO_001_B3_SALES_E2E_VERIFICATION_REPORT.md`
- 架构决策: `docs/planning/G2_DOCNO_001_ARCHITECTURE_DECISION.md`
- 引擎代码 (锁点): `modules/document-kernel/GuliERP.DocumentKernel.Infrastructure/DocumentNumber/DocumentNumberService.cs:184-217`
- 引擎 V1 状态 (self-declared): `DocumentNumberService.cs:47-52` (`_CODE_READY_CRITICAL_REVIEW_PENDING`)
- 集成测试 (skipped, 修复后通过): `tests/GuliERP.DocumentKernel.IntegrationTests/DocumentNumberCounterFacts.cs:75` (`GenerateAsync_Increments_Within_Same_Scope`)
- 并发测试 (skipped, 修复后通过): `tests/GuliERP.DocumentKernel.IntegrationTests/DocumentNumberCounterConcurrencyFacts.cs:39` (`Hundred_Concurrent_GenerateAsync_Calls_Produce_Unique_Sequences`)
- Idempotency 测试 (skipped, 修复后通过): `tests/GuliERP.DocumentKernel.IntegrationTests/DocumentNumberIdempotencyFacts.cs:42,65`
- 单元测试 (5 个, 始终跑过): `tests/GuliERP.DocumentKernel.Tests/*.cs`
- 架构文档引用: `docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md §7-§15`
