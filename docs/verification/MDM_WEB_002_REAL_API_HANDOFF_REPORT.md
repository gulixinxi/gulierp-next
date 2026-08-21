# MDM-WEB-002 Real API Handoff Report

> Frontend wiring of BusinessPartner / Warehouse / Location to the frozen
> MDM-002 backend API (TRAE_MDM_002_API_HANDOFF.md). Frontend-only; no
> Operator DB verification this round.

## 1. Baseline

| Item | Value |
|---|---|
| Start HEAD | `829b36f` |
| Branch | master |
| MDM-001 | REAL_MASTER_DATA_VERIFIED |
| MDM-WEB-001 | three pages real-API code ready |
| MDM-002 backend | CODE_READY_RUNTIME_OPERATOR_PENDING (12 endpoints) |
| API Handoff | frozen |
| This round scope | frontend only; no Operator DB verify; no new harness |

## 2. API Contract source of truth

- `docs/architecture/TRAE_MDM_002_API_HANDOFF.md` (endpoints, DTOs, error map)
- `modules/mdm/GuliERP.Mdm.Application/MdmDtos.cs` (DTO shapes)
- `modules/mdm/GuliERP.Mdm.Domain/Enums/MdmEnums.cs` (enum int values)
- Base path: `/api/v1/mdm/{business-partners|warehouses|locations}`

## 3. API Clients (all under apps/web/src/api/mdm/)

| File | Endpoints | Notes |
|---|---|---|
| `business-partner.ts` | list / get / create / update / setStatus | role bit-flag filter (§5); code immutable on PUT; re-GET before status toggle for fresh concurrencyVersion |
| `warehouse.ts` | list / listAllActive / get / create / update / setStatus | plantId opaque long?; no Location Count fabricated (§13); company scope from cookie context |
| `location.ts` | list / get / create / update / setStatus + joinLocationWarehouse | warehouseId REQUIRED; cross-scope error `mdm_location_parent_warehouse_cross_scope` rendered as "所选仓库不属于当前公司或无权访问" |

All clients reuse `../http` (apiGet/apiPost/apiPut), cookie auth, CSRF on
unsafe methods, and `ApiError` (RFC7807). No second HTTP client. No mock
fallback.

## 4. Types (apps/web/src/types/mdm.ts)

Added wire DTOs + UI types + enum converters:
- `BusinessPartnerRoleInt` (0/1/2/3) ↔ `BusinessPartnerRole` (CUSTOMER/SUPPLIER/BOTH)
- `BusinessPartnerRoleFilter` (all/customer/supplier) → `roleFilterToInt` (omit|1|2)
- `WarehouseType` (PHYSICAL/VIRTUAL/RETURN) ↔ int 1/2/3
- `LocationType` (BIN/SHELF/ZONE/DOCK) ↔ int 1/2/3/4
- `BusinessPartner`/`Warehouse`/`Location` UI types with `concurrencyVersion`
- `BusinessPartnerForm`/`WarehouseForm`/`LocationForm`

## 5. Pages (apps/web/src/views/mdm/)

| Page | Reuses | Key behavior |
|---|---|---|
| `BusinessPartnerList.vue` | MdmListToolbar/FormDrawer/DetailDrawer/Pagination/EmptyState/StatusBadge/TableRowActions | route meta.defaultRole sets initial role filter; role filter 全部/客户/供应商; frozen fields Contact/Phone/Email/Address/TaxNumber; Customer/Supplier/Both tags |
| `WarehouseList.vue` | same component set + auth store | current-company context bar; type/status filters; plantId shown in detail only; toolbar "库位管理" + per-row "查看库位" → Location |
| `LocationList.vue` | same component set | Warehouse dropdown from real API; honors `?warehouseId=N`; on warehouse change refreshes options (§12); cross-scope error mapping |

All six interaction requirements met: loading / empty / API failure / reload /
save-then-reread / fresh-GET-before-edit / concurrency conflict zh / 401→login /
403 explicit / RFC7807 title+detail+requestId / no swallowed errors /
404→"数据不存在或无权访问".

## 6. Router & Menu

`apps/web/src/router/mdm.ts` — 5 new child routes under `/mdm`:
- `business-partners` (defaultRole all), `customers` (customer), `suppliers` (supplier)
  → all `import('../views/mdm/BusinessPartnerList.vue')` (ONE component, 3 routes)
- `warehouses` → `WarehouseList.vue`
- `locations` → `LocationList.vue`

`apps/web/src/layouts/ErpShell.vue` — minimal menu wiring in the 基础数据 module:
- 客户档案 → `/mdm/customers`; 供应商 → `/mdm/suppliers`; 仓库 → `/mdm/warehouses`; 库位 → `/mdm/locations`
- route watcher refined: MDM-001 paths → 主数据 module; MDM-002 paths → 基础数据 module (keeps clicked item visible + highlighted)
- No 404 dead-ends; no fake entries; no Shell refactor

## 7. Verification

| Check | Result |
|---|---|
| Typecheck (`vue-tsc -b`) | PASS (exit 0, clean) |
| Production Build (`vite build`) | PASS (exit 0; 3 new page chunks emitted) |
| `git diff --check -- apps/web` | PASS (no whitespace errors) |
| Mock import in 3 new pages | 0 (only a "No mock fallback" doc comment in BP) |
| Second HTTP client | 0 (all clients import `../http`) |
| Write ops via CSRF | PASS (apiPost/apiPut route through `request()` X-CSRF-TOKEN) |
| Updates carry ExpectedConcurrencyVersion | PASS (BP/WH/Loc update + setStatus) |
| 客户/供应商 reuse one component | PASS (3 routes → BusinessPartnerList.vue) |
| Warehouse→Location route valid | PASS (mdm-warehouses + mdm-locations; goLocations/goAllLocations) |
| SalesOrder files touched | 0 |
| Backend files touched | 0 |

## 8. Files Changed

Modified:
- `apps/web/src/types/mdm.ts`
- `apps/web/src/router/mdm.ts`
- `apps/web/src/layouts/ErpShell.vue`

New:
- `apps/web/src/api/mdm/business-partner.ts`
- `apps/web/src/api/mdm/warehouse.ts`
- `apps/web/src/api/mdm/location.ts`
- `apps/web/src/views/mdm/BusinessPartnerList.vue`
- `apps/web/src/views/mdm/WarehouseList.vue`
- `apps/web/src/views/mdm/LocationList.vue`

Excluded from commits: `apps/web/tsconfig.tsbuildinfo` (build artifact).

## 9. Runtime status

Backend runtime Operator DB verification NOT performed this round
(per scope). If backend is unavailable, pages show the failure banner with a
重新加载 button; no mock fallback is created. Gate remains
RUNTIME_OPERATOR_PENDING and does NOT block frontend code submission.

## 10. Gate

**MDM_WEB_002_CODE_READY_RUNTIME_OPERATOR_PENDING**

## 11. Next suggested task (not started)

Operator DB provisioning + runtime smoke of the 12 MDM-002 endpoints against
real data, then end-to-end click-through of the three new pages against a live
backend. Do NOT start without explicit instruction.
