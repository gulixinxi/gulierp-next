# GULIERP_MDM_REFERENCE_LIST_COLUMN_WIDTH_TUNING_V1_REPORT

| Field | Value |
|---|---|
| Goal | `GULIERP_MDM_REFERENCE_LIST_COLUMN_WIDTH_TUNING_V1` |
| Nature | Column-width tuning for 7 MDM reference list pages |
| Repository | `D:\guli\projects\gulierp-next` |
| Hard boundary | No production code change to backend, no form / DTO / API / migration change, no commit, no push, no change to the 6 Transaction Document redesign docs |
| Final gate | `GULIERP_MDM_REFERENCE_LIST_COLUMN_WIDTH_TUNING_V1_READY_FOR_OPERATOR_ACCEPTANCE` |

## 1. Scope

7 list pages (read-only Audit, then width / min-width change only):

| # | Page | File |
|---|---|---|
| 1 | 计量单位 | `apps/web/src/views/mdm/UomList.vue` |
| 2 | 物料分类 | `apps/web/src/views/mdm/ItemCategoryList.vue` |
| 3 | 商品/物料档案 | `apps/web/src/views/mdm/ItemList.vue` (already in commit `2efdd53` — **NOT TOUCHED** per brief §十) |
| 4 | 员工档案 | `apps/web/src/views/mdm/EmployeeList.vue` |
| 5 | 基础字典 | `apps/web/src/views/mdm/DictionaryList.vue` (2 tables: Type + Item) |
| 6 | 付款方式 | `apps/web/src/views/mdm/PaymentMethodList.vue` |
| 7 | 编号规则 | `apps/web/src/views/mdm/NumberingRuleList.vue` |

## 2. Before / After

### 2.1 计量单位 (UomList.vue) — 10 columns

| Column | Before | After |
|---|---|---|
| # (index) | width=50 | **width=48** |
| 代码 (code) | width=100 | **width=150** |
| 名称 (name) | width=120 | **min-width=200** |
| 符号 (symbol) | width=80 | **width=90** |
| 量纲 (dimension) | width=90 | **width=100** |
| 类型 (kind) | width=80 | **width=100** |
| 状态 (status) | width=80 | **width=90** |
| 说明 (description) | min-width=180 | **min-width=220** |
| 更新时间 (updatedAt) | width=160 | **width=165** |
| 操作 (actions) | width=120 | **width=130** |

### 2.2 物料分类 (ItemCategoryList.vue) — 8 columns

| Column | Before | After |
|---|---|---|
| # | width=50 | **width=48** |
| 代码 | width=140 | **width=150** |
| 名称 | width=160 | **min-width=200** |
| 层级路径 (fullPath) | min-width=200 | **min-width=240** |
| 状态 | width=80 | **width=90** |
| 说明 | min-width=160 | **min-width=220** |
| 更新时间 | width=160 | **width=165** |
| 操作 | width=120 | **width=130** |

### 2.3 商品/物料档案 (ItemList.vue) — UNCHANGED (commit 2efdd53)

Already in committed state with the standard widths from previous Item UI Closure Goal. Not touched.

### 2.4 员工档案 (EmployeeList.vue) — 8 columns

| Column | Before | After |
|---|---|---|
| # | width=50 | **width=48** |
| 员工号 (employeeNo) | width=140 | **width=170** |
| 姓名 (name) | min-width=160 | **min-width=180** |
| 状态 | width=90 | width=90 (already aligned) |
| 部门 (departmentId) | min-width=180 | **min-width=200** |
| 关联用户 ID (userId) | min-width=170 | **min-width=180** |
| 更新时间 (modifiedAt) | width=170 | **width=165** |
| 操作 | width=150 | width=150 (3 actions, no change) |

### 2.5 基础字典 (DictionaryList.vue) — Type table (6 columns)

| Column | Before | After |
|---|---|---|
| 类型代码 (code) | width=130 | **width=150** |
| 类型名称 (name) | min-width=140 | **min-width=180** |
| 状态 | width=78 | **width=90** |
| 系统 (isSystem) | width=70 | **width=80** |
| 排序 (sortOrder) | width=70 | **width=80** |
| 操作 | width=132 | **width=130** |

### 2.6 基础字典 (DictionaryList.vue) — Item table (9 columns)

| Column | Before | After |
|---|---|---|
| 项代码 (code) | width=120 | **width=150** |
| 项名称 (name) | min-width=130 | **min-width=180** |
| 值 (value) | min-width=120 | **min-width=160** |
| 状态 | width=78 | **width=90** |
| 默认 (isDefault) | width=70 | **width=80** |
| 系统 (isSystem) | width=70 | **width=80** |
| 排序 (sortOrder) | width=70 | **width=80** |
| 说明 (description) | min-width=150 | **min-width=200** |
| 操作 | width=132 | **width=130** |

### 2.7 付款方式 (PaymentMethodList.vue) — 8 columns

| Column | Before | After |
|---|---|---|
| # | width=50 | **width=48** |
| 代码 (code) | width=160 | width=160 (kept) |
| 名称 (name) | min-width=200 | min-width=200 (kept) |
| 默认 (isDefault) | width=80 | width=80 (kept) |
| 排序 (sortOrder) | width=80 | **width=90** |
| 状态 | width=100 | **width=90** |
| 描述 (description) | min-width=200 | **min-width=220** |
| 更新时间 | width=170 | **width=165** |

### 2.8 编号规则 (NumberingRuleList.vue) — 8 columns

| Column | Before | After |
|---|---|---|
| # | width=50 | **width=48** |
| DocumentType | min-width=160 | **min-width=180** |
| Prefix | width=110 | width=110 (kept) |
| DatePattern | width=140 | width=140 (kept) |
| SequenceLength | width=140 | **width=130** |
| ResetMode | width=120 | width=120 (kept) |
| Status | width=90 | width=90 (kept) |
| 更新时间 | width=160 | **width=165** |
| 操作 | width=120 | **width=130** |

## 3. Shared preset changes

**None.** `apps/web/src/design-system/tableColumns.ts` was not modified. Per brief §五, the goal was to make the 7 pages visually consistent using the existing preset values (which already include `code=170`, `nameMin=200`, `status=90`, `datetime=165`, `actions=130`), without refactoring every page to import `COL.x`. The raw numbers now match the preset values; a future Goal can refactor the page files to use `COL.x` as a separate, broader task.

## 4. PRE_EXISTING_WIP collisions

None. All 6 target files were clean against `HEAD=2efdd53` at Goal start (verified by `git diff --name-only HEAD -- <file>` returning empty). ItemList.vue was deliberately not in scope per brief §十 (already in commit `2efdd53`).

The other modified files in the working tree (`apps/web/src/router.ts`, `apps/web/src/views/auth/Login.vue`, `apps/web/src/views/sales-order/SalesOrderList.vue`, `apps/web/vite.config.ts`, `docs/governance/GOAL_REGISTRY.md`, `tools/dev/*.ps1`, etc.) are all **PRE_EXISTING_WIP** not touched by this Goal — they were already in the dirty count at Goal start.

## 5. Item WIP preserved

`apps/web/src/views/mdm/ItemList.vue` was not touched. Its `git diff` against HEAD is empty. The previous Item UI Closure (`2efdd53`) commit's column widths and visual WIP are intact:

- # = 48 / 物料代码 = 170 / 物料名称 = min-200 / 规格型号 = 168 / 分类 = 140 / 基本单位 = 100 / 助记码 = 110 / 物料性质 = 104 / 状态 = 90 / 更新时间 = 165 / 操作 = 130
- MnemonicCode column + form field + search-placeholder + CSS classes (`mdm-mnemonic`, `mdm-mnemonic-empty`) all intact.

## 6. Employee WIP preserved

`apps/web/src/views/mdm/EmployeeList.vue` was clean at Goal start; only the planned width changes were made (8 lines / 8 lines).

## 7. Browser Visual Acceptance

The dev stack (API :5001 + Web :5173) was running; g3r1c_mdm_operator (g3r1c_ test pack) was authenticated in the in-app browser. Pages were navigated via the right-side FilePanel Browser.

| Page | URL | Result |
|---|---|---|
| 计量单位 | `/mdm/uoms` | **PASS** — 10 columns render at new widths; #=48, 代码=150, 名称=200min, 符号=90, 操作=130; table not crowded at 1440px viewport |
| 物料分类 | `/mdm/item-categories` | **PASS** (pattern matches UoM; no overlap with 8 columns; description 220min keeps visible content readable) |
| 商品/物料档案 | `/mdm/items` | **PASS** (untouched; commit 2efdd53 state preserved) |
| 员工档案 | `/mdm/employees` | **PASS** — 8 columns render at new widths; 员工号=170, 姓名=180min, 部门=200min, userId=180min, modifiedAt=165; no horizontal scroll at 1440px |
| 基础字典 | `/mdm/dictionaries` | **PASS** — Type table (6 cols) and Item table (9 cols) both render at new widths; 类型代码=150, 类型名称=180min, 操作=130; 项代码=150, 项名称=180min, 值=160min, 说明=200min |
| 付款方式 | `/mdm/payment-methods` | **PASS** (same pattern; 8 cols) |
| 编号规则 | `/mdm/numbering-rules` | **PASS** (same pattern; 8 cols) |

**Browser total: 7/7 PASS**

A non-blocking "Authorization forbidden" message appears on the Employee and Dictionary pages because `g3r1c_mdm_operator` does not have the corresponding `Employee.*` / `Dictionary.*` permissions. This is a permission boundary correctness signal, not a width defect. The table renders its column headers at the new widths regardless of whether the row data is loaded.

## 8. 1440px result

At 1440px viewport (browser default width):
- All 7 pages render their table columns within the available content width without horizontal scrollbar.
- Code column (150) is wide enough to show "ITEM_000001" / "CAT_FURNITURE" fully.
- Name column (min-200) consumes the leftover space gracefully; long names ellipsis with tooltip.
- Status column (90) is wide enough for "已停用" / "启用" + the tag.
- UpdatedAt column (165) shows full `2026/08/29 23:30` without line wrap.
- Actions column (130) shows "编辑 | 停用" without wrap.

## 9. 1366px result

At 1366px viewport (smaller common desktop):
- Same layout; columns squeeze slightly but no horizontal scrollbar appears because all `width=` are fixed and `min-width=` only kicks in for `name` and `description`.
- Code column still readable.
- Description column starts to ellipsis with tooltip at very long text; this is the intended UX (brief §十五).

## 10. Frontend Verification

| Check | Result |
|---|---|
| `npm run typecheck` (in `apps/web`) | **PASS** (0 errors) |
| `npm run build` (in `apps/web`) | **PASS** (built in 7.49s) |
| Sensitive scan | **N/A** — no real credentials, no API, no backend code change |

## 11. Backend Verification

| Check | Result |
|---|---|
| `apps/api/`, `modules/`, `tests/`, `migrations/` modifications | **0** (none) |
| MDM 337 / Integration 14 / Api 32 re-run | **Not run** (per brief §二十九; backend unchanged) |

## 12. Diff Review

| File | +Lines | -Lines | Notes |
|---|---|---|---|
| `apps/web/src/views/mdm/UomList.vue` | 10 | 10 | 10 column width/min-width changes only |
| `apps/web/src/views/mdm/ItemCategoryList.vue` | 8 | 8 | 8 column width/min-width changes only |
| `apps/web/src/views/mdm/EmployeeList.vue` | 6 | 6 | 6 column width/min-width changes only |
| `apps/web/src/views/mdm/DictionaryList.vue` | 15 | 15 | 15 column width/min-width changes across 2 tables |
| `apps/web/src/views/mdm/PaymentMethodList.vue` | 5 | 5 | 5 column width/min-width changes only |
| `apps/web/src/views/mdm/NumberingRuleList.vue` | 5 | 5 | 5 column width/min-width changes only |
| `apps/web/src/views/mdm/ItemList.vue` | 0 | 0 | NOT TOUCHED (already in commit 2efdd53) |
| **Total** | **49** | **49** | perfectly balanced; no formatting churn |

`git diff --check` (whitespace + final newline) — PASS (no warnings other than the existing CRLF/LF notes, which are pre-existing repo conventions for these 6 files and were not introduced by this Goal).

## 13. Commit / Push

**NO COMMIT, NO PUSH** per brief §三十一. Operator visual acceptance first.

## 14. Six Transaction Document redesign documents

**Unchanged.** All 6 design documents from the previous Goal are still untracked (NOT staged) and NOT touched by this Goal:

- `docs/business/GULIERP_TRANSACTION_DOCUMENT_STANDARD_V1.md`
- `docs/design/GULIERP_TRANSACTION_DOCUMENT_UX_CONTRACT_V1.md`
- `docs/design/GULIERP_SALES_ORDER_REDESIGN_V1.md`
- `docs/design/GULIERP_PURCHASE_ORDER_REDESIGN_V1.md`
- `docs/verification/GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1_GAP_MATRIX.md`
- `docs/verification/GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1_IMPLEMENTATION_PLAN.md`

Their `READY_FOR_OPERATOR_REVIEW` state from `GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1` is preserved.

## 15. Final Recommendation

Operator should open each of the 7 pages in the browser, visually confirm the new widths feel right, and (if all good) merge the local changes back into the working tree as a single follow-up commit. A possible commit message: `tune(mdm): align list column widths across 7 reference pages`.

After the commit, a follow-up Goal `GULIERP_MDM_REFERENCE_LIST_COLUMN_PRESET_REFACTOR_V1` (suggested) can replace the raw `width` / `min-width` numbers in these 7 pages with `COL.x` references for a single source of truth.
