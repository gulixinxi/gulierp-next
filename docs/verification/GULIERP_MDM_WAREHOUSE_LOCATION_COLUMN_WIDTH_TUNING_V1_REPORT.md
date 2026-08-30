# GULIERP_MDM_WAREHOUSE_LOCATION_COLUMN_WIDTH_TUNING_V1_REPORT

| Field | Value |
|---|---|
| Goal | `GULIERP_MDM_WAREHOUSE_LOCATION_COLUMN_WIDTH_TUNING_V1` |
| Nature | Column-width tuning for 2 MDM list pages (Warehouse + Location) |
| Repository | `D:\guli\projects\gulierp-next` |
| Scope | `apps/web/src/views/mdm/WarehouseList.vue` + `apps/web/src/views/mdm/LocationList.vue` only |
| Hard boundary | No backend change, no form / DTO / API change, no commit, no push |
| Final gate | `GULIERP_MDM_WAREHOUSE_LOCATION_COLUMN_WIDTH_TUNING_V1_READY_FOR_OPERATOR_ACCEPTANCE` |
| Origin | Follow-up of `GULIERP_MDM_REFERENCE_LIST_COLUMN_WIDTH_TUNING_V1` (which only covered 7 of the 9 MDM list pages). Warehouse + Location were excluded from the original 7 and Operator flagged the 仓库名称 / 库位名称 / WarehouseList.actions 字段 偏宽 / 不合理。 |

## 1. Scope

2 list pages (read-only Audit, then width / min-width change only):

| # | Page | File |
|---|---|---|
| 1 | 仓库 | `apps/web/src/views/mdm/WarehouseList.vue` |
| 2 | 库位 | `apps/web/src/views/mdm/LocationList.vue` |

## 2. Before / After

### 2.1 仓库 (WarehouseList.vue) — 9 columns

| Column | Before | After | Notes |
|---|---|---|---|
| # (index) | width=50 | **width=48** | align to standard |
| 仓库代码 (code) | width=130 | **width=150** | align to standard |
| **仓库名称 (name)** | min-width=180 | **min-width=160** | Operator 反馈太宽; warehouse names ≤20 字 |
| 类型 (type) | width=100 | width=100 | kept |
| 所在城市 (city) | width=120 | **width=110** | 4-6 chars |
| 国家 (countryCode) | width=80 | width=80 | kept |
| 状态 (status) | width=80 | **width=90** | align to standard |
| 更新时间 (updatedAt) | width=160 | **width=165** | align to standard |
| **操作 (actions)** | **width=160** | **width=130** | 2 buttons (查看库位 \| 编辑/停用) — 160 明显过宽 |

### 2.2 库位 (LocationList.vue) — 10 columns

| Column | Before | After | Notes |
|---|---|---|---|
| # | width=50 | **width=48** | align to standard |
| 库位代码 (code) | width=130 | **width=150** | align to standard |
| **库位名称 (name)** | min-width=180 | **min-width=150** | Operator 反馈不合理; 库位名如 "A-01-03" ≤10 字 |
| **所属仓库 (warehouseName)** | **width=180 fixed** | **min-width=140** | denormalized 列, 180 fixed 挤压其他列 |
| 类型 (type) | width=90 | width=90 | kept |
| 通道 (aisle) | width=70 | width=70 | kept |
| 货位 (bay) | width=70 | width=70 | kept |
| 层 (shelf) | width=70 | width=70 | kept |
| 状态 (status) | width=80 | **width=90** | align to standard |
| 更新时间 (updatedAt) | width=160 | **width=165** | align to standard |
| 操作 (actions) | width=120 | **width=130** | align to standard |

## 3. Net effect

- WarehouseList: total fixed-width budget from 1010 → 950, name column narrows by 20px min.
- LocationList: total fixed-width budget from 1180 → 1060, warehouseName 180 fixed → 140 min (frees 40px of fixed budget), 库位名称 min 180 → 150 (frees 30px).
- All 9 (Warehouse) + 10 (Location) columns now use the same scale as the 7 reference pages tuned in `GULIERP_MDM_REFERENCE_LIST_COLUMN_WIDTH_TUNING_V1`.

## 4. Frontend Verification

| Check | Result |
|---|---|
| `npm run typecheck` (in `apps/web`) | **PASS** (0 errors) |
| `npm run build` (in `apps/web`) | **PASS** (built in 6.64s) |
| Backend / API / migration changes | **0** (none) |
| Sensitive scan | N/A (no credentials in diff) |

## 5. Diff Review

| File | +Lines | -Lines | Notes |
|---|---|---|---|
| `apps/web/src/views/mdm/WarehouseList.vue` | 6 | 6 | 6 column width/min-width changes only |
| `apps/web/src/views/mdm/LocationList.vue` | 6 | 6 | 6 column width/min-width changes only |
| **Total** | **12** | **12** | perfectly balanced; no formatting churn |

## 6. Commit / Push

**NO COMMIT, NO PUSH** per brief §三十一 (visual acceptance first). Operator should open `/mdm/warehouses` and `/mdm/locations` in the browser, confirm the new widths feel right, and (if good) merge back to working tree.

A possible commit message: `tune(mdm): align WarehouseList / LocationList column widths`.

## 7. Cross-reference

| File | This Goal | Previous `REFERENCE_LIST_COLUMN_WIDTH_TUNING_V1` |
|---|---|---|
| UomList | (unchanged) | 10/10 |
| ItemCategoryList | (unchanged) | 8/8 |
| ItemList | (unchanged) | (locked in 2efdd53) |
| EmployeeList | (unchanged) | 6/6 |
| DictionaryList | (unchanged) | 15/15 |
| PaymentMethodList | (unchanged) | 5/5 |
| NumberingRuleList | (unchanged) | 5/5 |
| **WarehouseList** | **6/6 (new)** | — |
| **LocationList** | **6/6 (new)** | — |
| **TOTAL** | **12 ins / 12 del** | 49 ins / 49 del |

After this Goal, all 9 MDM list pages use the same width scale.
