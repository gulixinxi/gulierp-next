# GULIERP_UI_SYSTEM_CALIBRATION_001_REPORT

Status: GULIERP_UI_SYSTEM_CALIBRATION_001_IMPLEMENTED_OPERATOR_VISUAL_ACCEPTANCE_PENDING

## 1. Repo / HEAD

- Repository: `D:/guli/projects/gulierp-next`
- Branch: `master`
- HEAD at start: `139fe1e940258d71b85d328d88b4e1358c0f7b1e`
- Latest commit: `docs(verification): add G3 R2B purchase order runtime report`
- Scope: UI system calibration only. No backend, DB schema, migration, Identity, permission, Sales/Purchase business logic, Inventory Receiving, or legacy repository changes.

## 2. Pre-existing WIP

The following files existed before this UI goal and are excluded from the UI commit candidate:

- `docs/governance/GOAL_REGISTRY.md`
- `docs/verification/GULIERP_EMPLOYEE_CODE_CONTRACT_RECONCILIATION_001_REPORT.md`
- `tests/GuliERP.Api.Tests/SalesRuntimeRegressionSourceFacts.cs`
- `tests/GuliERP.Identity.IntegrationTests/EnterpriseBootstrapAndOrganizationTreeFacts.cs`
- `tests/GuliERP.Identity.IntegrationTests/EnterpriseRolePackCrossTenantFacts.cs`
- `docs/verification/GULIERP_EMPLOYEE_MASTER_FINAL_GOVERNANCE_CLOSURE_001_REPORT.md`
- `docs/verification/GULIERP_EMPLOYEE_MASTER_POSTGRES_RUNTIME_CLOSURE_001_REPORT.md`
- `docs/verification/GULIERP_FORMAL_ADMIN_ROLEPACK_CONTRACT_ALIGNMENT_001_REPORT.md`
- `docs/verification/GULIERP_NEXT_EMPLOYEE_OPERATOR_RUNTIME_FINAL_001_REPORT.md`
- `tools/dev/.quarantine/`

## 3. Three Sample Pages

- `apps/web/src/views/mdm/BusinessPartnerList.vue`: complex master-data list with contact, email, tax number, type, status, and row actions.
- `apps/web/src/views/mdm/ItemList.vue`: high-frequency item master list with code, name, specification, category, UOM, nature, and status.
- `apps/web/src/views/sales-order/SalesOrderList.vue`: ERP document list with document number, customer, dates, status, currency, money columns, and document actions.

## 4. Before Issues

- List pages used local column numbers directly, making code/name/status/action widths drift by page.
- MDM pages duplicated table cell padding overrides even though global table density already exists.
- Search width differed by component and document list page (`260px` in MDM toolbar, `280px` in sales list).
- Standard MDM drawer width was hard-coded at `480px`, narrow for longer Chinese labels and validation messages.
- A few table/document visual states used hard-coded amber/red/green/blue hex values outside semantic tokens.

## 5. Layout Changes

- Added Layout V2 spacing tokens for page padding, section gap, toolbar gap, and form/filter gaps.
- Kept the existing shell proportions: 64px rail, 180px secondary nav, 52px topbar.
- Kept list pages full-width for ERP desktop work rather than adding a centered marketing-style max width.
- Tuned pagination vertical padding from the older larger rhythm to a denser V2 list rhythm.

## 6. Color Changes

- Preserved Enterprise Fiori-inspired Blue:
  - Primary `#0A6ED1`
  - Rail `#354A5F`
  - Success `#107E3E`
  - Warning `#E9730C`
  - Danger `#BB0000`
- Replaced sample/shared hard-coded warning, danger, success, and primary hover colors with semantic tokens.
- No green-theme, component-library, or brand-direction change was made.

## 7. Table Density

- Continued using compact ERP table density:
  - Header height `36px`
  - Row height `36px`
  - Small table row height `32px`
  - Base font `13px`
- Removed per-page Element Plus padding overrides from the three sample tables so global density is the single source.
- Preserved horizontal scrolling behavior for wide ERP document lists.

## 8. Column Preset Rules

Added `apps/web/src/design-system/tableColumns.ts` and matching CSS variables in `apps/web/src/design-system/tokens/sizing.css`.

Representative presets:

- Code: `132px`
- Document No: `160px`
- Date: `120px`
- DateTime: `168px`
- Status: `88px`
- Money: `136px`
- Name min-width: `200px`
- Business Partner Name: `240px`
- MDM Actions: `136px`
- Document Actions: `150px`

The sample pages now bind their table widths to the shared presets instead of repeating magic numbers.

## 9. Form Rules

- `MdmFormDrawer` now accepts `drawerSize` and `labelWidth`.
- Defaults are `--dialog-width-md` and `104px`.
- Existing callers keep working through defaults; no business form schema was changed.

## 10. Shared Components Modified

- `apps/web/src/components/mdm/MdmListToolbar.vue`
- `apps/web/src/components/mdm/MdmFormDrawer.vue`
- `apps/web/src/design-system/tokens/spacing.css`
- `apps/web/src/design-system/tokens/sizing.css`
- `apps/web/src/design-system/components/table.css`
- `apps/web/src/design-system/components/document.css`
- `apps/web/src/design-system/tableColumns.ts`

## 11. Page-specific Changes

- `BusinessPartnerList.vue`: code, partner name, type, person, phone, email, tax, status, datetime, and actions columns moved to presets.
- `ItemList.vue`: code, name, specification, category, UOM, item nature, status, datetime, and actions columns moved to presets.
- `SalesOrderList.vue`: document number, customer, date, status, currency, money, datetime, selection/index, and actions columns moved to presets.

## 12. Build / Test Results

- `npm run typecheck`: PASS.
- `npm run build`: PASS. Vite reported non-blocking warnings for third-party `#__PURE__` comments in `@vueuse/core` and large chunk size.
- `dotnet test tests/GuliERP.Api.Tests/GuliERP.Api.Tests.csproj -c Release`: command started but remained at restore/project resolution with no completion.
- `dotnet test tests/GuliERP.Sales.Tests/GuliERP.Sales.Tests.csproj -c Release`: command started but remained at restore/project resolution with no completion.
- `dotnet test tests/GuliERP.Purchase.Tests/GuliERP.Purchase.Tests.csproj -c Release`: command was started before the sample page was corrected from Purchase to Sales; it remained at restore/project resolution with no completion and is excluded from the final UI gate.
- Retried with `--no-restore` after `dotnet build-server shutdown`; test commands still hung with no output. `dotnet --info` and `dotnet --list-sdks` both resolved .NET 10 from `C:/Program Files/dotnet`.
- Backend tests are recorded as ENVIRONMENT_HUNG, not as UI regression failures. No backend code was changed.

## 13. Manual Acceptance URLs

The web dev server started on port 5174 because port 5173 was already in use. HTTP route checks returned 200 for all three sample pages:

- `http://127.0.0.1:5174/mdm/business-partners`
- `http://127.0.0.1:5174/mdm/items`
- `http://127.0.0.1:5174/sales/orders`

Vite proxy remains configured through `VITE_API_TARGET`, defaulting to `http://127.0.0.1:5000`.

Runtime service note:

- Frontend dev server: PASS on `http://127.0.0.1:5174`.
- API `/health/live`: PASS (`200`).
- API `/health/ready`: FAIL (`503 Service Unavailable`).
- Readiness detail: `foundation-db` is unhealthy because PostgreSQL returned `28P01: password authentication failed for user "gulidata"`. No password was printed or stored.
- Interpretation: the frontend route shell is reachable, but API-backed data calls may fail until the current backend runtime uses a valid PostgreSQL connection string.

## 14. Manual Acceptance Checklist

- 1366px 是否拥挤
- 1920px 是否过空
- 表格一屏记录数
- 编码列
- 名称列
- 金额列
- 状态列
- 操作列
- 搜索区
- 页面标题
- 左右留白
- 色彩层级
- hover / selected
- dialog / form width

## 15. Remaining UI Issues

- Remaining MDM list pages still need gradual preset rollout after this V2 candidate is visually accepted.
- Purchase order list still has its own list-column numbers and should be calibrated in a later rollout, after the purchase workflow is formally accepted by the operator.
- Operator visual review is still required before claiming `GULIERP_UI_LAYOUT_V2_VERIFIED`.

## 16. Candidate V2 Freeze

This implementation is a candidate V2 freeze only. Final freeze requires operator browser acceptance against the three URLs above.

## 17. Recommended Rollout Plan

1. Operator checks the three sample pages at 1366px, 1440px, and 1920px.
2. If accepted, freeze `GULIERP_UI_LAYOUT_STANDARD_V2.md`.
3. Roll out presets to remaining MDM pages.
4. Roll out presets to Sales and Purchase document lists.
5. Apply V2 to Inventory pages only when Inventory Receiving begins as its own business goal.

NO COMMIT. NO PUSH.
