# MDM-WEB-001 Real API Handoff Report

> Owner: TRAE (web SPA). Source baseline: 157efa4 → End commit c444663
> Goal Gate target: MDM_WEB_001_CODE_READY_RUNTIME_OPERATOR_PENDING
> Real backend live-capture evidence (UOM / ItemCategory / Item POST + GET against PostgreSQL)
> was NOT performed in this session. See §Runtime below.

## 1. Pages wired

| Page | Route (from router/mdm.ts) | Data source after patch |
|---|---|---|
| 计量单位 (UOM) | `/mdm/uom` | `src/api/mdm/uom.ts` → `GET/POST/PUT /api/v1/mdm/uoms[/{id}]` |
| 物料分类 (ItemCategory) | `/mdm/item-category` | `src/api/mdm/item-category.ts` → `GET/POST/PUT /api/v1/mdm/item-categories[/{id}]` |
| 物料 (Item) | `/mdm/item` | `src/api/mdm/item.ts` → `GET/POST/PUT /api/v1/mdm/items[/{id}]` |

All three pages NO LONGER `import ... from '../../mock/mdm'`.
Grep (§6) confirms 0 matches in `apps/web/src/views/mdm/`.

## 2. API layer (apps/web/src/api/mdm/)

Three thin clients, each reusing `src/api/http.ts` (cookie-include, CSRF retry, RFC7807 ApiError).
No second HTTP client, no direct `fetch()`, no Axios instance inside `api/mdm/`.

| File | List | Get | Create | Update | Concurrency |
|---|---|---|---|---|---|
| `uom.ts` | `listUoms(params)` / `listAllUomsActiveOnly()` | `getUom(id)` | `createUom(form)` | `updateUom(id, form, cv)` → `expectedConcurrencyVersion` in body |
| `item-category.ts` | `listItemCategories(params)` / `listAllCategories()` | `getItemCategory(id)` | `createItemCategory(form)` | `updateItemCategory(id, form, cv)` → `expectedConcurrencyVersion` in body |
| `item.ts` | `listItems(params)` + `joinItemReferences()` for display names | `getItem(id)` | `createItem(form)` | `updateItem(id, form, cv)` → `expectedConcurrencyVersion` in body |

Wire/UI conversion is handled inside each client:
- Backend integer enums (dimension/kind/status/itemNature) → UI string enums via `*IntToUi / *UiToInt`.
- `UomDto.modifiedAt` → `Uom.updatedAt` (name mismatch between handoff §4.1 and legacy UI).
- Category hierarchy (`level`, `fullPath`, `parentName`) computed client-side in `deriveCategoryHierarchy()` (§9 TRAE prototype reconciliation).

## 3. Page behaviour matrix

| Capability | UOM | ItemCategory | Item |
|---|---|---|---|
| Loading indicator on mount / filter change | ✅ (`v-loading` via `loading` ref not visually rendered but state exists; empty + loading → MdmEmptyState) | ✅ | ✅ |
| Real list (keyword / status / dimension…) | ✅ keyword + status + dimension → wire params | ✅ keyword + status | ✅ keyword + category + itemNature + status |
| Create (POST, CSRF) | ✅ + reload list | ✅ + reload list + selectors | ✅ + reload list |
| Edit — re-read GET for fresh concurrencyVersion | ✅ openEdit `getUom(row.id)` | ✅ openEdit `getItemCategory(row.id)` | ✅ openEdit `getItem(row.id)` |
| Update (PUT, expectedConcurrencyVersion, CSRF) | ✅ | ✅ | ✅ |
| Detail drawer (fresh GET) | ✅ + fallback to row snapshot | ✅ + full-path enrich via `listAllCategories` | ✅ + denormalize via `joinItemReferences` |
| Status toggle (Deactivate / Activate) | ✅ via `setUomStatus` (re-read → update status → PUT) | ✅ via `setItemCategoryStatus` | ✅ via `setItemStatus` |
| Cycle-error handling (mdm_item_category_cycle) | N/A | ✅ Chinese: 会造成循环引用，请重新选择上级分类 | N/A |
| 403 / 401 | ApiError code bubbles → global `onAuthEvent` for 401 | same | same |
| RFC7807 render | title + detail + requestId via ElMessage/error banner | same | same |
| Concurrency conflict warning | ✅ 并发冲突：数据已被其他用户修改，请刷新后重试 | ✅ | ✅ |
| Distinguish empty vs failure | ✅ error banner + "重新加载" button vs MdmEmptyState when no error | ✅ | ✅ |
| Reload after save | ✅ `await fetchUoms()` | ✅ `Promise.all([fetchCategories, fetchAllForSelectors])` | ✅ `await fetchItems()` |

## 4. Verify

| Step | Command / method | Result |
|---|---|---|
| Typecheck | `apps/web: npx vue-tsc --noEmit` | exit 0 |
| Production build | `apps/web: npm run build` (vue-tsc -b + vite build) | exit 0; dist produced (1678 modules; UomList / ItemCategoryList / ItemList chunks + `uom-*.js`, `item-category-*.js`) |
| Whitespace | `git diff --check apps/web` | exit 0 (only LF→CRLF warnings, no whitespace errors) |
| Mock import residues | `rg mock/mdm\|mockUoms\|mockItemCategories\|mockItems apps/web/src/views/mdm` | 0 matches |
| Second HTTP client | `rg "fetch\(\|axios\|createHttpClient" apps/web/src/api/mdm` | 0 matches (all calls go through `apiGet/apiPost/apiPut` from `../http`) |
| Write ops go through CSRF path | grep `apiPost\|apiPut` in api/mdm/ | All 6 mutations use apiPost/apiPut → http.ts `isUnsafe()` → `csrf.ensure()` → `headers.set(csrf.headerName, csrf.token)` |
| ConcurrencyVersion sent | grep `expectedConcurrencyVersion` in api/mdm/ | 6 hits (updateUom, updateItemCategory, updateItem each have param declaration + body assignment) |
| Errors not swallowed | Page try/catch | All surfaces `ElMessage.error(title + detail + requestId)`; only `authentication_required` is intentionally re-thrown for the global redirect guard (http.ts `emitAuth`) |

## 5. Mock retention policy

Per scope §IV, `src/mock/mdm.ts` is NOT deleted in this patch because:
1. SalesOrder prototype pages (`src/mock/sales-order.ts`) are in the same Mock bucket and are explicitly protected.
2. `concurrencyVersion` on UI types was made optional **only** to avoid breaking SalesOrder Mock's existing object literals; real API never omits it.
3. MDM pages no longer import it; grep in §4 confirms 0 references under `src/views/mdm/`.

If a future scrub confirms the Mock file is entirely orphaned, it may be removed under a separate cosmetic commit (out of scope here).

## 6. Runtime status: RUNTIME_OPERATOR_PENDING

The Operator-side live backend (`/api/v1/mdm/*` wired against real PostgreSQL `gulierp_g2_003_test`)
was NOT executed during this session. No mock fallback was introduced; if the API is unreachable the
pages surface the RFC7807 error banner and an explicit "重新加载" button.

Gate at the moment of this commit: **MDM_WEB_001_CODE_READY_RUNTIME_OPERATOR_PENDING**.

Operator next steps to upgrade gate to MDM_WEB_001_REAL_API_VERIFIED (out of scope for TRAE):
1. Ensure backend MiniMax binary is launched against the canonical DB target and is healthy at `/health/ready`.
2. Log in through the real Login page so GuliERP auth cookies are minted.
3. Visit `/mdm/uom`, `/mdm/item-category`, `/mdm/item`. Confirm:
   - Seeded DEV UOM rows (13 rows from `data/bootstrap/reference/system/uom.json`) are visible.
   - Create + Edit + Deactivate/Activate round-trips result in fresh list rows, no 4xx.
   - `mdm_item_category_cycle` produces the Chinese cycle error instead of saving.
   - A duplicate `code` → `mdm_duplicate_code` RFC7807 is rendered.
4. Do NOT ask TRAE to re-verify if backend configuration is the cause; use the Operator evidence scripts.

## 7. Blocking items

None on the code path. Items deferred / not blocking:
- `v-loading` visual directive on `el-table` is NOT rendered in this pass (error banner + empty-state already distinguish failure vs empty). If operator feedback asks for explicit skeleton it is a cosmetic follow-up.
- `exportData()` buttons remain "待后端支持" — no MDM export endpoint in Handoff V1.
- `MdmFormDrawer` emits a `Duplicate key "modelModifiers"` esbuild warning during vite build. Pre-existing; the component still works and is out of scope for MDM-WEB-001 (shared component, owned by a different goal).

## 8. Files changed (commit `c444663`)

```
apps/web/src/api/http.ts                  +45 -5   (RequestOptions.params; apiPut/apiDelete/apiPatch; CSRF retry finalUrl)
apps/web/src/api/mdm/uom.ts               +125 new (UOM list/detail/create/update/statusToggle converters)
apps/web/src/api/mdm/item-category.ts     +137 new (full-tree + paged; cycle-error detection marker at caller)
apps/web/src/api/mdm/item.ts              +154 new (listItems + joinItemReferences + wire filters)
apps/web/src/types/mdm.ts                 +214 -53 (PagedResult, *Int/*Dto types, converters, hierarchy helper; concurrencyVersion made optional for SalesOrder Mock)
apps/web/src/views/mdm/UomList.vue        +158 -42 (real fetchUoms + onMounted; error banner; concurrency-safe save)
apps/web/src/views/mdm/ItemCategoryList.vue +173 -36 (full-tree selectors + hierarchy derivation; cycle-error message)
apps/web/src/views/mdm/ItemList.vue       +194 -48 (category + UOM selectors; findUom powered by activeUoms)
```
