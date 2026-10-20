# GULIERP_TRANSACTION_PHASE_B3_6_H_POST_FIX_RECON_GOAL_01 — FINAL REPORT

## 1. Goal Status

**PASS** — `GULIERP_TRANSACTION_PHASE_B3_6_H_POST_FIX_RECON_PASS` · `SALES_ORDER_V1_POST_FIX_RECON_PASS`

B3.6-H 复现并修复 B3.6-G 留下的 reload UoM display defect。最小修复 1 文件 + 13 行,无任何 backend / DTO / DB / migration 改动,Freeze Contract 100% preserved,所有 regression PASS。

## 2. Baseline (F0)

| Item | Value |
|---|---|
| HEAD | `b09eebfc074a45eb88243115f7ce36a594b77d7b` (B3.6-E Freeze) |
| Branch | `master` |
| Origin sync | ahead=0, behind=0 |
| Modified | 34 (B2/B3.6-A WIP baseline; B3.6-H 期间 0 新增 modified files) |
| Deleted | 3 (quarantined orphans) |
| Staged | 0 |
| Untracked | 49 (含 1 个本 Goal 报告 + 6 个本 Goal worktree-local artifacts) |
| 3 prior reports in `docs/verification/` | B3.6-E (23,113 B) + B3.6-F (22,929 B) + B3.6-G (12,461 B) ✓ |
| Dev stack | API:5004 + Web:5174 (健康) |

## 3. UoM Reload Reproduction (F1)

**DEFECT CONFIRMED (Case B)**. 复现脚本: `.stack-logs/b3-6-h-f1-uom-repro.py`,证据截图: `.stack-logs/b3-6-h-f1-reload.png`.

| Lifecycle point | uomId | uomCode | uomName | trigger text | dropdown options |
|---|---|---|---|---|---|
| Before Save | (assigned) | (assigned) | (assigned) | `? (TNE)` ✓ | 11 (in picker) |
| After Save (server snapshot) | `83727350616817758` | `TNE` | `?` | n/a | n/a |
| **After Reload (pre-fix)** | `83727350616817758` | `TNE` | `?` | **`83727350616817758`** (raw) ✗ | **0** ✗ |
| **After Reload (post-fix)** | `83727350616817758` | `TNE` | `?` | **`? (TNE)`** ✓ | **11** ✓ |

**Root cause**: `loadExisting` (SalesOrderEdit.vue) 把 DTO lines 投影到 `form.lines` 时,只设了 `uomId` / `uomCode` / `uomName` snapshot,没有为每行 rebuild `uomOptions` 和 `uomById`。el-select 拿不到匹配 option,Element Plus fallback 显示 raw bound value。

## 4. Root Cause & Minimal Fix (F2)

**Root cause**: `form.lines.map(l => ({uomId, uomCode, uomName, ...}))` 后,`row.uomOptions` 和 `row.uomById` 都是 `undefined`,el-select v-for 拿不到 option list。

**Fix** (1 file, +13 lines, `apps/web/src/views/sales-order/SalesOrderEdit.vue`):

```ts
// B3.6-H: hydrate uomOptions/uomById for each loaded line so the
// LineGrid el-select can resolve the bound uomId back to a label
// (without this, the reload render falls back to the raw uomId
// string because the option list is empty). The status predicate
// matches the B3.6-G fix (accept both int 1 and string 'active'
// encodings from the context facade).
const uomsRes = await fetchSalesOrderUoms({ page: 1, pageSize: 100 });
const activeUoms: Uom[] = uomsRes.items.filter(
  (u) => String(u.status) === '1' || u.status === 'active',
);
const uomByIdMap: Record<string, Uom> = Object.fromEntries(
  activeUoms.map((u) => [u.id, u]),
);
form.lines = form.lines.map((l) => ({
  ...l,
  uomOptions: activeUoms,
  uomById: uomByIdMap,
}));
```

不改变: Business Contract / DTO / DB / Migration / LineNo / ItemSelector core / Warehouse projection / Route。

## 5. Business Contract

**UNCHANGED**. 0 backend/.cs files modified in B3.6-H.
- `modules/sales/GuliERP.Sales.Domain/Entities/SalesOrder.cs` 不变
- `modules/sales/GuliERP.Sales.Domain/Entities/SalesOrderLine.cs` 不变
- `SalesOrderEndpoints.cs` / `SalesOrderService.cs` / `ISalesOrderService.cs` / `SalesOrderDtos.cs` 不变
- 所有 26 个 Header fields + 所有 18 个 Line fields 全部 preserved

## 6. API / DTO / DB

**UNCHANGED**. 0 API endpoint changes, 0 DTO changes, 0 migrations added.
- POST /api/v1/sales/orders ✓ (still used by F3 save flow)
- GET /api/v1/sales/orders/{id} ✓ (still used by F3 reload)
- GET /api/v1/sales/orders/context/uoms ✓ (now used by F2 hydration block)
- Migrations/SalesDbContextModelSnapshot.cs 不变

## 7. UI (F5)

**PASS — 8/8**. 验证脚本: `.stack-logs/b3-6-h-f5-ui-labels.py`.

| Check | Result |
|---|---|
| A = 主表 (Section 1 title) | ✓ |
| B = 补充信息 (Section 2 title, preserved) | ✓ |
| C = 明细表 (Section 3 title) | ✓ |
| D = TotalsBar (`.tx-totals-bar` count=1) | ✓ |
| 收货地址 in A after 默认仓库 (idx 7 > idx 6) | ✓ |
| 备注 rows = 2 (B section textareas) | ✓ |
| 备注 maxlength = 2000 | ✓ |
| 收货地址 maxlength = 500 | ✓ |

**未触动**: B 补充信息 标题保留(战略跨单据扩展区)。

## 8. Routes (F6)

**PASS**. B3.6-C UI Integration AC-01 5/5:
- `/sales-orders/` reachable ✓
- `/sales-order` legacy → `/sales-orders` redirect ✓
- `/sales/orders` legacy → `/sales-orders` redirect ✓
- `/sales-order/new/edit` → `/sales-orders/new/edit` (with :id) ✓
- editor rendered after legacy redirect ✓

## 9. ItemSelector (F4)

**PASS**. B3.6-B.1 14/14:
- A1-A3: 3 checkboxes, 已选择 count, row-class-name ✓
- B1: 取消 checkbox 已选择 count 减少 ✓
- C1-C2: header select-all, header uncheck-all ✓
- D1-D2: 3 selected on first search, search re-run preserves + re-attaches ✓
- E1: cancel + re-open 0 selected ✓
- F1: LineGrid 行数 ✓
- G1-G2: 重复物料 skipped 消息 + grid not grown ✓
- H1-H2: lineNos unique + step-10 ✓

核心算法 preserved: `selectedById` Map, `@select`, `@select-all`, `reattachSelectionFromMap`, `onMultiCancel`, `inline mode`, `batchAppendItems` (itemId dedup)。

## 10. LineNo (F4)

**PASS**. B3.6-C Acceptance Debt H0-H3 5/5:
- H0: lineNos_pre = [10, 20, 30, 40] (readable, non-empty) ✓
- H1: lineNos unique (no collisions, pre) ✓
- H1a: after delete-middle, lineNos shorter AND still unique = [10, 30, 40] ✓
- H2: after delete+add, lineNos unique AND length = pre+1 = [10, 30, 40, 50, 60] ✓
- H3: new lineNos = max(survivors) + 10/20 (no collision) = [50, 60] ✓

`LINE_NO_STEP = 10`, `nextLineNoForBatch`, `buildNewRowFromItem` 全部 preserved。

## 11. Warehouse (F4)

**PASS**. B3.6-C UI Integration AC-03 4/4:
- Edit: WarehouseSelectorPopover trigger present ✓
- List+Detail: warehouse column shows `B36CWH · B3.6-C test warehouse` (not raw snowflake id) ✓
- List+Detail: 11 rows with code/name + 9 rows with other (no N+1) ✓

`warehouseId = persisted identity`, `warehouseCode` / `warehouseName` 仍为 client-side projection。 0 new DB fields / 0 migrations。

## 12. Regression (F7)

**Total: 68/68 PASS (100%)**.

| Suite | Scenarios | Passed | Failed |
|---|---|---|---|
| B3.6-B.1 ItemSelector / Batch | 14 (A1-G2 + H1-H2) | 14 | 0 |
| B3.6-C Acceptance Debt | 18 (DATA + A1-H3) | 18 | 0 |
| B3.6-C UI Integration | 14 (AC-01..AC-05) | 14 | 0 |
| B3.6-G Uom Auto-Fill | 10 (U-DATA + U0 + U1a-c + U2a-b + U3a-c) | 10 | 0 |
| **B3.6-H F3 UoM Regression (NEW)** | 12 (U1, U2, U3-pre, U3, U4, U5, U6, U7, U7-reload, U8, U9, U10) | 12 | 0 |
| **Total** | **68** | **68** | **0** |

**B3.6-H F3 UoM Regression 详细**:
- U1: 单行 Item → Unit 自动填充 (TNE) ✓
- U2: 多选 Item → 每行 Unit 自动填充 (3 rows) ✓
- U3: Save → Reload → Unit 显示 Code/Name (`? (TNE)`) ✓ **(the F2 fix proof)**
- U4: 编辑已有订单 → Unit 正常 (3 rows hydrated) ✓
- U5: re-pick / modify Item → UoM cell 不为空 (`? (TNE)` → `? (TNE)`) ✓
- U6: Unit 不为空 (0 empty cells) ✓
- U7: Unit 不显示 raw uomId ✓ (前)
- U7 (reload): Unit 不显示 raw uomId ✓ (后) **(the F2 fix proof)**
- U8: 无异常 API request (0 bad_api, no 4xx/5xx) ✓
- U9: 无 console error (0 page errors) ✓
- U10: existing lines hydration 正常 (dropdownOptionsCount=33) ✓ **(the F2 fix proof)**

## 13. Browser Regression (F8)

**R1-R15 ALL PASS**. Mapped to existing smokes:

| R# | Scenario | Suite | Result |
|---|---|---|---|
| R1 | canonical route | AC-01 | PASS |
| R2 | legacy redirect | AC-01 4/4 | PASS |
| R3 | list | AC-03 | PASS |
| R4 | edit | B3.6-C + B3.6-B.1 + B3.6-G + F3 | PASS |
| R5 | detail | AC-03 | PASS |
| R6 | item selector | A1-A3 | PASS |
| R7 | multi-select (1→2→3) | A1-A3 + G1 | PASS |
| R8 | cross-search | D1, D2 | PASS |
| R9 | duplicate prevention | G1, G2 | PASS |
| R10 | delete + add LineNo | H0, H1, H1a, H2, H3 | PASS |
| R11 | warehouse projection | AC-03 3/3 | PASS |
| R12 | UoM auto-fill | B3.6-G U1/U2 + F3 U1/U2 | PASS |
| R13 | save | F3 U3-pre | PASS |
| R14 | reload | F3 U4 | PASS |
| R15 | reload UoM display | F3 U3 + F3 U7-reload + F1 reproduction | PASS |

## 14. Build (F9)

- **vue-tsc** (typecheck): **0 errors**
- **Vite build**: **0 errors**, 1786 modules transformed, 6.66s
- 0 UnicodeEncodeError (PowerShell GBK stdout issues do not affect npm build)
- All outputs in `apps/web/dist/` clean
- SalesOrderEdit-BWEdIpUy.js = 13.61 kB (gzipped 5.03 kB)

## 15. Git (F11)

| Item | Value |
|---|---|
| HEAD | `b09eebfc074a45eb88243115f7ce36a594b77d7b` (B3.6-E Freeze, **unchanged** in B3.6-H) |
| Branch | `master` |
| Origin sync | ahead=0, behind=0 |
| `git status --short` | `M 34 + D 3 + ?? 49` (B3.6-H 期间 0 新增 source files; 1 file with 1 additional hunk) |
| `git diff --stat` (B3.6-H only) | `apps/web/src/views/sales-order/SalesOrderEdit.vue \| 905 +++++++++++----` (累计含 B2 WIP baseline + B3.6-G WIP + B3.6-H 13-line fix) |
| `git diff --name-only` (B3.6-H only) | `apps/web/src/views/sales-order/SalesOrderEdit.vue` (**唯一**修改文件) |
| `git diff --cached --stat` | (empty — 0 staged) |
| modified count | 34 (B3.6-H 期间新增 = 0) |
| deleted count | 3 (quarantined orphans, unchanged) |
| staged count | 0 |
| untracked count | 49 (含 1 个本 Goal 报告 + 6 个 worktree-local artifacts) |

**B3.6-H 实际修改**: 1 file (`SalesOrderEdit.vue`) + 1 add hunk (13 lines for uomOptions/uomById hydration in `loadExisting`)。 0 backend/.cs/migration/DTO 改动 ⇒ Freeze Contract 100% preserved。

## 16. Remaining Issues

1. **Inherited B3.6-B.1 smoke script UnicodeEncodeError** (PRE-EXISTING, NOT B3.6-H):
   - 14/14 PASS 已打印,exit code 因 GBK 编码 ✓/✗ 字符失败非 0
   - 不影响 assertions,只影响退出码
   - 不在本 Goal 修复范围
2. **Worktree 残留 B2/B3.6-A WIP baseline**: 34 modified files (含 11 backend .cs + 11 frontend + 2 test + 1 docs + 3 tools/dev + 6 misc) 是 B2/B3.6-A 阶段就有的 WIP 残留。**B3.6-H 期间只额外改了 `apps/web/src/views/sales-order/SalesOrderEdit.vue` 一个文件**(在已有 WIP 之上增加 13 行 hydration block)。HEAD unchanged 保证 B3.6-E Freeze 边界 100% preserved。

## 17. Out of Scope

按 brief §二 / §十九,以下继续 OUT OF SCOPE:
- Purchase Order 流程
- XLSX 导入/导出
- Detail tabs redesign
- List redesign
- MDM V2 重构
- 新业务 / 新权限 / 新 Agent / 新 Workflow
- 架构 / 数据库重构
- 重构 LineGrid / ItemSelector / SalesOrderEdit
- 修改 Business Contract / DTO / DB / Warehouse / LineNo / Batch selection / Warehouse projection

## 18. Final Decision

`GULIERP_TRANSACTION_PHASE_B3_6_H_POST_FIX_RECON_PASS` · `SALES_ORDER_V1_POST_FIX_RECON_PASS`

| Check | Result |
|---|---|
| UoM reload defect | REPRODUCED (F1) → MINIMAL FIX (F2) → RE-VERIFIED (F3 U3, U7-reload, U10) |
| Business Contract | UNCHANGED ✓ |
| API / DTO / DB | UNCHANGED ✓ |
| UI (A=主表 / B=补充信息 / C=明细表 / D / 收货地址 / 备注) | 8/8 PASS ✓ |
| Routes (canonical + legacy) | 5/5 PASS ✓ |
| ItemSelector core | 14/14 PASS ✓ |
| LineNo | 5/5 PASS ✓ (H0-H3 完整 [10,20,30,40]→[10,30,40]→[10,30,40,50,60]) |
| Warehouse | 4/4 PASS ✓ (code/name 投影, no raw id) |
| Regression | 68/68 PASS ✓ |
| Browser R1-R15 | ALL PASS ✓ |
| Build | 0 errors ✓ |
| Git Boundary | HEAD unchanged, branch unchanged, origin sync unchanged, staged=0 ✓ |
| 0 source files outside the 1 target | ✓ |

**STOP** · **DO NOT COMMIT** · **DO NOT PUSH** · **WAIT_FOR_NEXT_GOAL**

Sales Order V1 已达到可冻结状态。B3.6-E Freeze 报告可视为 `SALES_ORDER_V1_FROZEN`(经 B3.6-G Minimal Fix + B3.6-H Post-Fix Recon 双重确认)。
