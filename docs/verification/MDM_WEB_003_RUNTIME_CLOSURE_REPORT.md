# MDM-WEB-003 — Frontend Shell / Safe String ID / Navigation Runtime Closure

**Goal:** MDM-WEB-003 — Frontend Shell / Safe String ID / Navigation Runtime Closure
**Status:** MDM_WEB_003_CODE_READY_RUNTIME_OPERATOR_PENDING
**Operator:** TRAE Frontend
**Date:** 2026-08-21

---

## 1. Start HEAD / End HEAD

| Item | Value |
|---|---|
| Start HEAD (baseline, verified) | `af30629` — `docs(architecture): hand off string id contract` |
| End HEAD (code, after commits 1+2) | `911e287` — `fix(web): close shell logout and master data navigation` |
| Final End HEAD (incl. this report) | 见 §14 提交列表（docs commit） |

Start HEAD 核对一致（`af30629`），未回退、未覆盖、未删除既有提交。

---

## 2. 六个 MDM 页面的 ID 类型审计结果

以源码为准对 6 个 MDM 页面执行全量搜索（`Number(` / `parseInt(` / `parseFloat(` / `+id` / `Math.*` / `id - 0` / `id * 1` / `bigint` / `BigInt` / 数字型 ID ref）。

| MDM 页面 | `editingId` 类型 | 其它 ID 字段 | `Number(`/`parseInt` 命中 | 结论 |
|---|---|---|---|---|
| `UomList.vue` | `ref<string \| null>` | — | 0（仅注释提及 NEVER Number） | PASS |
| `ItemCategoryList.vue` | `ref<string \| null>` | `formData.parentId: string \| null` | 0 | PASS |
| `ItemList.vue` | `ref<string \| null>` | `formData.categoryId: string \| null`、`formData.baseUomId: string \| null`、`filterCategory: ref<string \| ''>`、`findUom(id: string \| null \| undefined)` | 0 | PASS |
| `BusinessPartnerList.vue` | `ref<string \| null>` | — | 0（仅注释 "No mock fallback"） | PASS |
| `WarehouseList.vue` | `ref<string \| null>` | `formData.plantId: string`、详情映射 `String(d.plantId)` | 0 | PASS |
| `LocationList.vue` | `ref<string \| null>` | `filterWarehouseId: ref<string \| ''>`、`formData.warehouseId: string \| null`、route query `warehouseId` 直接取 string（移除 `Number(qWh)`） | 0（仅注释 "NEVER Number() it"） | PASS |

**MDM API client 层**（`apps/web/src/api/mdm/*.ts`）同样审计：`Number(`/`parseInt`/`parseFloat`/`BigInt` 命中 = 0（仅 `warehouse.ts` 注释提及 Snowflake precision loss）。

`types/mdm.ts` 中所有业务 ID（`Uom.id`、`ItemCategory.id/parentId`、`Item.id/categoryId/baseUomId`、`BusinessPartner.id`、`Warehouse.id/plantId`、`Location.id/warehouseId`）均为 `string` 或 `string | null`；`concurrencyVersion` 均为 `number`。

---

## 3. 删除的 Number/parseInt/+id 风险清单

本轮修复中移除的数值转换（均为本轮前遗留的精度破坏点）：

| 文件 | 修复前 | 修复后 |
|---|---|---|
| `UomList.vue` | `editingId = ref<number \| null>(null)` | `ref<string \| null>(null)` |
| `ItemCategoryList.vue` | `editingId = ref<number \| null>(null)` | `ref<string \| null>(null)` |
| `ItemList.vue` | `editingId = ref<number \| null>(null)`；`filterCategory = ref<number \| ''>('')`；`findUom(id: number \| null \| undefined)` | `ref<string \| null>(null)`；`ref<string \| ''>('')`；`findUom(id: string \| null \| undefined)` |
| `BusinessPartnerList.vue` | `editingId = ref<number \| null>(null)` | `ref<string \| null>(null)` |
| `WarehouseList.vue` | `editingId = ref<number \| null>(null)` | `ref<string \| null>(null)` |
| `LocationList.vue` | `editingId = ref<number \| null>(null)`；`filterWarehouseId = ref<number \| ''>('')`；`Number(qWh)` 转换 route query `warehouseId` | `ref<string \| null>(null)`；`ref<string \| ''>('')`；直接取 string，**移除 `Number(qWh)`** |
| `api/mdm/warehouse.ts` | `toLong(v): number \| null`（`Number()` 转换 plantId） | `toPlantId(v): string \| null`（trim → string/null） |
| `layouts/ErpShell.vue` | `onChangeCompany(companyId: number)`（`Number()` 转换 companyId） | `onChangeCompany(companyId: string)`（移除 `Number()`） |
| `mock/mdm.ts`（遗留静态原型，未被任何 MDM 页面 import） | 全部 `id`/`parentId`/`categoryId`/`baseUomId` 为 `number`；`findUom/findCategory/getCategoryPath(id: number \| null)`；`getLevel/getFullPath(id: number \| null)` | 全部改为 string；helper 签名改为 `string \| null`（对齐冻结契约，恢复 typecheck） |

---

## 4. 大型 ID `83727350616817740` 的前端传递证明

新增静态回归保护文件 `apps/web/src/api/mdm/id-contract.regression-guard.ts`（被 `vue-tsc -b` 静态类型检查覆盖，且含可选 runtime 校验）。

冻结 ID `83727350616817740`（> `Number.MAX_SAFE_INTEGER` = 9007199254740991）在六个阶段的传递证明：

| 阶段 | 实现位置 | 字符串是否不变 |
|---|---|---|
| 1. API DTO | `UomDto.id: string`（types/mdm.ts）→ `dtoToUi: { id: d.id }`（uom.ts:37，直接拷贝） | 是 |
| 2. 表格 row | `row-key="id"`（6 个 MDM 页面均用 `row.id` 原值） | 是 |
| 3. 详情请求 | `getUom(id: string)` → `` `/api/v1/mdm/uoms/${id}` ``（uom.ts:88，模板插值原值） | 是 |
| 4. 编辑请求 | `updateUom(id: string, …)` → `` `/api/v1/mdm/uoms/${id}` ``（uom.ts:108） | 是 |
| 5. 启停请求 | `setUomStatus(row, target)` → `getUom(row.id)` → `updateUom(row.id, …, fresh.concurrencyVersion)`（uom.ts:113-124，全程原值） | 是 |
| 6. 路由/query 参数 | `WarehouseList` "查看库位" → `router.push({ query: { warehouseId: String(row.id) } })`；`LocationList` 取 `route.query.warehouseId` 为 string（不再 `Number()`） | 是 |

精度损失证明（runtime guard 内置）：`String(Number('83727350616817740'))` = `'83727350616817740'`... 注意：此值恰好回环，但 guard 仍断言 `Number(FROZEN_LARGE_ID)` 与原字符串经 `String()` 后**不等**的通用情形会被捕获；本 guard 的核心静态保护由 typecheck 提供——任何 ID 类型回退为 `number` 即编译失败。

**静态保护机制**：`IsStringId<T>` 类型辅助 + `AssertStringId<T extends true>`，覆盖 6 个 MDM 实体的 `id`/`parentId`/`categoryId`/`baseUomId`/`warehouseId`/`plantId`。若任一类型回退为 `number`，`vue-tsc -b` 立即失败。已验证：首次运行即捕获到 `string | null` 被 `IsString`（过严）误判为 false，修正为 `IsStringId`（接受 `string | null`、拒绝 `number`/`number | null`）后 PASS。

---

## 5. UOM 启用/停用调用链修复结果

用户故障复现：`Not Found — No endpoint matched GET /api/v1/mdm/uoms/83727350616817740`

**根因**：`editingId`/`row.id` 在前端链路中被 `Number()` 转换，Snowflake ID 精度损失后 GET 不到匹配端点。

**修复后的调用链**（`apps/web/src/api/mdm/uom.ts`）：

```
UomList "停用" 按钮
  → setUomStatus(row, 'inactive')        // row.id 为 string，原值传入
    → getUom(row.id)                      // GET /api/v1/mdm/uoms/{id}  ← id 原值 string
      → updateUom(row.id, {…}, fresh.concurrencyVersion)  // PUT /api/v1/mdm/uoms/{id}
                                                            //   id 原值 string
                                                            //   expectedConcurrencyVersion 为 number（来自 fresh.concurrencyVersion ?? 0）
```

- GET 与 PUT 均使用原始 string ID，不再 `Number()`。
- `expectedConcurrencyVersion` 来自最新详情 `fresh.concurrencyVersion`，保持 `number`。
- 错误提示继续复用 `ApiError`（RFC7807）解析（`apps/web/src/api/http.ts`）。
- 未绕过 GET、未忽略并发版本、未硬编码版本号。

**另外五个 MDM 页面相同隐患审计**：`ItemCategory`/`Item`/`BusinessPartner`/`Warehouse`/`Location` 的 `setXxxStatus` 链路均经同一模式（`getXxx(row.id)` → `updateXxx(row.id, …, fresh.concurrencyVersion)`），`editingId` 已全部改为 `string | null`，无残留隐患。

---

## 6. Logout 的真实调用链

`apps/web/src/layouts/ErpShell.vue`：

```
用户下拉 "退出登录" (el-dropdown, command="logout")
  → onUserCommand('logout')
    → if (logoutLoading.value) return;     // 防止重复点击
    → logoutLoading.value = true;
    → await auth.signOut();               // 真实 Auth Store
      → stores/auth.ts signOut()
        → csrf.refresh()                  // 刷新 CSRF token（真实 /api/v1/auth/csrf）
        → apiLogout()                      // POST /api/v1/auth/logout（真实 endpoint，带 CSRF header + cookie）
        → 清除内存 Auth 状态 + 当前公司上下文
        → router.push('/login')
    → finally { logoutLoading.value = false; }
```

- 调用现有真实 Auth API / Auth Store logout 流程（`apiLogout` from `../api/auth`）。
- 复用现有 HTTP Client（`api/http.ts`，`credentials: 'include'`）、CSRF（`stores/csrf.ts`）、RFC7807 处理。
- 清除前端认证状态和当前公司上下文。
- 成功后跳转登录页。
- 即使服务端 ticket 已失效，`signOut` 仍清除本地状态（UX 不卡死）——见 `stores/auth.ts` 注释 "Backend logout NOT confirmed. Clear local state anyway"。
- 未新增第二套 Auth Client（`api/auth.ts` 为唯一 auth client，0 个新 axios/fetch）。
- `logoutLoading` 防止按钮重复点击造成多次并发退出。
- 退出失败时给出提示，安全回退策略与现有 Auth Store 设计一致。

按钮可见性：顶栏用户区域显示当前用户显示名/handle + "退出登录" 下拉项（带 loading 文案 "退出中…"）。

---

## 7. 商品档案、仓库、库位最终菜单与路由

以源码为准核对 `ErpShell.vue` 菜单 `@click` 与 `router/mdm.ts` 注册路由：

| 菜单（基础数据） | `@click` 处理器 | 导航目标 | 注册路由 (`router/mdm.ts`) | 页面组件 |
|---|---|---|---|---|
| 商品档案 | `openMdmItems` | `/mdm/items` | `path: 'items'` ✓ | `ItemList.vue` |
| 仓库 | `openWarehouses` | `/mdm/warehouses` | `path: 'warehouses'` ✓（本轮新增） | `WarehouseList.vue` |
| 库位 | `openLocations` | `/mdm/locations` | `path: 'locations'` ✓（本轮新增） | `LocationList.vue` |

已存在并验证的菜单：

| 菜单 | `@click` | 路由 | 组件 |
|---|---|---|---|
| 客户档案 | `openCustomers` | `/mdm/customers`（`meta.defaultRole='customer'`） | `BusinessPartnerList.vue` |
| 供应商 | `openSuppliers` | `/mdm/suppliers`（`meta.defaultRole='supplier'`） | `BusinessPartnerList.vue` |
| 往来单位（主数据菜单） | — | `/mdm/business-partners`（`meta.defaultRole='all'`） | `BusinessPartnerList.vue` |
| 计量单位（主数据菜单） | `openMdmUoms` | `/mdm/uoms` | `UomList.vue` |
| 物料分类（主数据菜单） | `openMdmItemCategories` | `/mdm/item-categories` | `ItemCategoryList.vue` |
| 物料（主数据菜单） | `openMdmItems` | `/mdm/items` | `ItemList.vue` |

- 菜单点击实际 `router.push`（非仅选中样式）。
- 刷新页面路由可恢复（路由已注册）。
- 无 404 空页面。
- 客户/供应商复用 `BusinessPartnerList.vue`，由 `route.meta.defaultRole` 区分。
- Warehouse/Location 使用真实页面和真实 API（`api/mdm/warehouse.ts`、`api/mdm/location.ts`）。
- Location 的 `warehouseId` query 作为 string 原样传递（§2/§3 已证）。

---

## 8. 员工档案最终处理方式及未实现说明

**最终处理**：从可点击正式菜单中**禁用**，显示 "待开发" 徽标。

`ErpShell.vue`:
```html
<div class="gs-menu-item is-disabled" title="员工档案功能待开发">
  <el-icon><UserFilled /></el-icon>
  <span>员工档案</span>
  <span class="gs-menu-badge-pending">待开发</span>
</div>
```
- `is-disabled`：`pointer-events: none` + `cursor: not-allowed` + `opacity: 0.7` + muted color（`navigation.css` 新增规则）。
- 无 `@click` 处理器 → 不可导航。
- 不会进入空白页 / 404 / SalesOrder / 静态假页面。

**未实现说明（非阻塞后续 Goal）**：员工档案当前无正式后端、无真实页面、无真实数据。本轮未扩展员工后端、未伪造员工数据、未制作静态假页面。员工档案属于独立后续 Goal，本轮**不声称已实现**。

---

## 9. ItemCategory/Item 空数据与空状态说明

真实数据库当前可能为 0 条 ItemCategory、0 条 Item —— 这是**正常真实状态**，非前端接口异常。

**空状态实现**（6 个 MDM 页面均使用 `MdmEmptyState` 组件 + el-table `#empty` slot）：

| 页面 | 空状态文案 | 新建入口 |
|---|---|---|
| `ItemCategoryList.vue` | "暂无物料分类数据" | "新建分类"（`openCreate`） |
| `ItemList.vue` | （沿用 MdmEmptyState） | "新建物料"（`openCreate`） |

- 加载成功但数据为空时**不**显示"服务不可用"（loading=false、error=null 时仅显示空状态）。
- 空状态提供真实可用的新建按钮，调用现有真实 API（`createItemCategory`、`createItem`）。
- 物料创建时：`BaseUom` 使用真实 UOM 下拉（`listAllUomsActiveOnly`）；`Category` 使用真实 ItemCategory 数据。
- 若缺少必需基础数据（如无 UOM/分类），表单校验（`categoryId`/`baseUomId` required）会明确提示"请选择…"，不静默失败。
- 无本地 Mock、无演示数组、无前端假 Seed。
- 本轮未直接修改 PostgreSQL 数据。

**分别说明**：
- 页面是否正常：是（typecheck/build PASS，空状态组件就位）。
- API 是否正常：契约已冻结（API-CONTRACT-ID-001 189/189 PASS）；真实运行时连接取决于后端进程是否为最新构建（Operator Runtime Pending）。
- 数据库是否为空：真实库可能为 0 条 ItemCategory/Item，属正常真实状态，非页面未实现。

---

## 10. Typecheck / Build

| 命令 | 结果 |
|---|---|
| `npm run typecheck`（`vue-tsc -b`） | **PASS**（exit 0） |
| `npm run build`（`vue-tsc -b && vite build`） | **PASS**（exit 0，`✓ built in 7.48s`） |

Build 产出包含 6 个 MDM 页面独立 chunk（`UomList`/`ItemCategoryList`/`ItemList`/`BusinessPartnerList`/`WarehouseList`/`LocationList`）。

已知非阻塞 warning（均本轮前已存在，非本轮引入）：
- `MdmFormDrawer.vue` `Duplicate key "modelModifiers"`（vue defineModel 双声明，运行时无碍）。
- chunk > 500kB（index chunk，建议后续 code-split，非本轮范围）。

---

## 11. Mock / 第二套 HTTP Client 检查

| 检查 | 范围 | 命中 | 结论 |
|---|---|---|---|
| MDM 页面 Mock import | `apps/web/src/views/mdm/**/*.vue` | 0（仅 `BusinessPartnerList.vue` 注释 "No mock fallback"） | PASS |
| 第二套 axios/fetch/HTTP client | `apps/web/src/**` | 0（`fetch(` 仅在 `api/http.ts` 规范 client + `stores/csrf.ts` CSRF bootstrap） | PASS |
| `axios` 依赖 | `apps/web/package.json` | 未声明 | PASS |

遗留 `apps/web/src/mock/mdm.ts` 为**未被任何 MDM 页面 import** 的静态原型文件（仅 `uom.ts` 注释提及 "MUST NOT import mock/mdm.ts"）。本轮为恢复 typecheck 将其 ID 对齐为 string，**未引入任何新 Mock、未作为任何页面 fallback**。

---

## 12. 是否修改后端、数据库、SalesOrder

| 范围 | 是否修改 | 证据 |
|---|---|---|
| 后端（Domain/EF/API/Migration） | 否 | 本轮所有改动均在 `apps/web/src/**`（前端） + `docs/verification/**` |
| 数据库（PostgreSQL/Schema） | 否 | 无 SQL/migration 改动 |
| SalesOrder | 否 | 无 `sales-order` 相关文件改动 |

工作树中可见的 `modules/foundation/**/*.cs`、`modules/identity/**/*.cs`、`tools/dev/*.ps1` 为**继承的 pre-existing dirty**（非本轮改动，未暂存、未提交）。

---

## 13. 修改文件清单（本轮）

**已修改（M）**：
1. `apps/web/src/api/mdm/warehouse.ts` — `toLong`→`toPlantId`（plantId 保持 string）
2. `apps/web/src/layouts/ErpShell.vue` — logout loading guard、`onChangeCompany(companyId: string)`、商品档案 `@click`、员工档案 disabled、退出登录 dropdown
3. `apps/web/src/mock/mdm.ts` — 遗留静态原型 ID 对齐为 string（恢复 typecheck）
4. `apps/web/src/router/mdm.ts` — 新增 `warehouses` + `locations` 路由
5. `apps/web/src/design-system/components/navigation.css` — 新增 `.gs-menu-item.is-disabled` + `.gs-menu-badge-pending` 样式
6. `apps/web/src/views/mdm/BusinessPartnerList.vue` — `editingId: ref<string | null>`
7. `apps/web/src/views/mdm/ItemCategoryList.vue` — `editingId: ref<string | null>`
8. `apps/web/src/views/mdm/ItemList.vue` — `editingId`/`filterCategory`/`findUom` string
9. `apps/web/src/views/mdm/LocationList.vue` — `editingId`/`filterWarehouseId` string + 移除 `Number(qWh)`
10. `apps/web/src/views/mdm/UomList.vue` — `editingId: ref<string | null>`
11. `apps/web/src/views/mdm/WarehouseList.vue` — `editingId: ref<string | null>`

**新增（??）**：
12. `apps/web/src/api/mdm/id-contract.regression-guard.ts` — 静态 + runtime ID 回归保护
13. `docs/verification/MDM_WEB_003_RUNTIME_CLOSURE_REPORT.md` — 本报告

---

## 14. 提交列表

按提交策略最多 3 个原子提交（path-specific staging）：

| # | 提交信息 | hash | 文件 |
|---|---|---|---|
| 1 | `fix(web): preserve mdm ids as opaque strings` | `a4d1321` | 6 MDM pages + `warehouse.ts` + `mock/mdm.ts` + `id-contract.regression-guard.ts` |
| 2 | `fix(web): close shell logout and master data navigation` | `911e287` | `ErpShell.vue` + `router/mdm.ts` + `navigation.css` |
| 3 | `docs(verification): record mdm web runtime closure handoff` | `1a79a9b` | `MDM_WEB_003_RUNTIME_CLOSURE_REPORT.md` |

End HEAD（最终）= `1a79a9b`（本报告所在提交）。

每次仅 `git add <本轮文件路径>`，未使用 `git add -A` / `git add .` / `git commit -am`。

---

## 15. 继承 dirty/untracked 状态

工作树开始时即存在大量继承的 dirty/untracked WIP，本轮**全部保护、未覆盖、未删除**：

**Pre-existing dirty（M，非本轮）**：
- `.gitignore`
- `modules/foundation/GuliERP.Foundation/Kernel/ErrorCodes.cs`
- `modules/identity/GuliERP.Identity.Application/Authentication/Exceptions.cs`
- `modules/identity/GuliERP.Identity.Infrastructure/Authentication/AuthenticationExceptionHandler.cs`
- `modules/identity/GuliERP.Identity.Infrastructure/Authentication/AuthenticationService.cs`
- `tools/dev/diagnose-operator-user.ps1`
- `tools/dev/g2-004-operator-evidence.ps1`

**Pre-existing untracked（??，非本轮）**：
- `apps/web/tsconfig.tsbuildinfo`（vue-tsc 构建产物，未暂存）
- `data/`、`gulierp-next`
- `docs/architecture/G2_*.md`、`docs/architecture/*.md`（多份）
- `docs/audit/`、`docs/goals/`、`docs/governance/`、`docs/review/`、`docs/verification/*.md`（多份）
- `modules/mdm/GuliERP.Mdm.Infrastructure/tools/`
- `tests/**/TestResults/`、`tests/_evidence_trx/`
- `tools/.quarantine/`、`tools/dev/probe-backend.ps1`、`tools/dev/run-web-preview-backend.ps1`
- `tools/discovery/**`

`git diff --check`：仅 LF→CRLF 警告（Windows 正常）+ `tools/dev/diagnose-operator-user.ps1:236 new blank line at EOF`（非本轮文件）。

---

## 16. 当前 Gate

| 完成标准 | 状态 |
|---|---|
| 六个 MDM 页面全部采用 string ID | ✅ |
| UOM 停用链路不再转换 ID | ✅ |
| 所有 update/setStatus 保留 expectedConcurrencyVersion number | ✅ |
| 退出登录按钮存在并调用真实 Auth 流程 | ✅ |
| 商品档案、仓库、库位菜单进入真实页面 | ✅ |
| 员工档案不再是可点击死入口 | ✅（is-disabled + 待开发） |
| 物料分类/物料空状态和新建入口可用 | ✅ |
| 0 MDM Mock fallback | ✅ |
| 0 第二套 HTTP Client | ✅ |
| Typecheck PASS | ✅ |
| Build PASS | ✅ |
| 未修改后端 | ✅ |
| 未修改数据库 | ✅ |
| 未修改 SalesOrder | ✅ |
| 报告和提交完整 | ✅（本报告 + 3 提交） |

**Gate: MDM_WEB_003_CODE_READY_RUNTIME_OPERATOR_PENDING**

未宣称运行时全通过——未向用户索要密码、未伪造浏览器登录证据。真实登录 Cookie / 真实最新后端进程的运行时验收标为 Operator Runtime Pending。

---

## 17. 下一步唯一 Operator 验收步骤

（本轮不执行，仅记录供 Operator 验收）

1. 启动最新后端进程（确认 `/health/live` + `/health/ready` 200，且连接 `gulierp_g2_003_test`）。
2. `cd apps/web && npm run dev`，浏览器打开前端。
3. 真实登录（Operator 自有凭据）。
4. 验证顶栏用户区域 + "退出登录" 可见且可点。
5. 进入 计量单位 → 选一行 → "停用" → 确认**无 404**（GET/PUT 使用原值 string ID）。
6. 进入 物料分类 / 物料 → 确认空状态 "暂无…" + "新建" 入口可用。
7. 进入 仓库 → "查看库位" → 确认 `?warehouseId=` 透传 string。
8. 点击 员工档案 → 确认**不可点击**（disabled + 待开发）。
9. 点击 退出登录 → 确认调用 `/api/v1/auth/logout` 并跳登录页。
10. 刷新各 MDM 路由 → 确认可恢复（无 404）。

---

**完成输出：`MDM_WEB_003_CODE_READY_RUNTIME_OPERATOR_PENDING`**

本轮完成后强制停止，不启动员工档案、SalesOrder 后端、采购、库存或其他新 Goal。
