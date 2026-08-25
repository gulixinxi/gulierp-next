# G2-DOCNO-001-B2 NumberingRule UI Report

> Date: 2026-08-25  
> Project: `D:\guli\projects\gulierp-next`  
> HEAD: `d74b98a` (`docs(mdm): summarize MDM phase progress`)  
> Goal: `G2_DOCNO_001_B2_NUMBERING_RULE_UI`

## 1. Scope

Implemented the operator-facing MDM NumberingRule management UI.

In scope:

- API client for real backend NumberingRule endpoints.
- `/mdm/numbering-rules` route.
- MDM navigation menu item: `主数据` -> `编号规则`.
- Master Data Workbench card: `编号规则`.
- List/search/pagination/create/edit/enable/disable UI using existing MDM component patterns.

Out of scope and not changed by this B2 task:

- `modules/document-kernel`
- `SalesOrderService`
- migrations
- Identity
- Bootstrap
- numbering expression designer / visual rule editor / custom scripts

## 2. Modified Files

Added:

- `apps/web/src/api/mdm/numberingRule.ts`
- `apps/web/src/views/mdm/NumberingRuleList.vue`
- `docs/verification/G2_DOCNO_001_B2_NUMBERING_RULE_UI_REPORT.md`

Modified:

- `apps/web/src/router/mdm.ts`
- `apps/web/src/layout/navigation.ts`
- `apps/web/src/views/mdm/MasterDataWorkbench.vue`

## 3. UI Capability

Page:

- `/mdm/numbering-rules`

List columns:

- `DocumentType`
- `Prefix`
- `DatePattern`
- `SequenceLength`
- `ResetMode`
- `Status`

Supported operations:

- Query by keyword.
- Filter by status.
- Pagination.
- Create NumberingRule.
- Edit NumberingRule.
- Enable / disable NumberingRule.

Reused MDM components:

- `MdmListToolbar`
- `MdmFormDrawer`
- `MdmStatusBadge`
- `MdmPagination`
- `MdmEmptyState`
- `MdmTableRowActions`

No mock data is imported by the NumberingRule page or API client.

## 4. API Contract

Frontend client:

- `GET /api/v1/mdm/numbering-rules`
- `GET /api/v1/mdm/numbering-rules/{id}`
- `POST /api/v1/mdm/numbering-rules`
- `PUT /api/v1/mdm/numbering-rules/{id}`
- `POST /api/v1/mdm/numbering-rules/{id}/status`

Status and reset mode mapping:

- `MasterDataStatus`: `1 -> active`, `2 -> inactive`
- `ResetMode`: `1 -> DAILY`, `2 -> MONTHLY`, `3 -> YEARLY`, `4 -> NEVER`

Concurrency:

- Edit and status changes pass `expectedConcurrencyVersion`.
- Status changes re-read detail first to use a fresh concurrency version.

## 5. Verification

### Typecheck

Command:

```powershell
npm run typecheck
```

Working directory:

```text
D:\guli\projects\gulierp-next\apps\web
```

Result:

- PASS

### Build

Command:

```powershell
npm run build
```

Working directory:

```text
D:\guli\projects\gulierp-next\apps\web
```

Result:

- PASS

Observed existing build warnings:

- Rollup removed non-actionable `/* #__PURE__ */` comments from `@vueuse/core`.
- Existing `MdmFormDrawer.vue` generated-code warning: duplicate `modelModifiers`.
- Vite chunk-size warning for the main bundle.

None of the warnings blocked the build.

### HTTP Shell Verification

Vite dev server:

- Requested port: `5173`
- Actual port: `5174` because `5173` was already in use.
- Vite proxy target from config: `http://127.0.0.1:5000`

Commands:

```powershell
curl.exe -s -o NUL -w "%{http_code} %{url_effective}\n" http://127.0.0.1:5174/mdm
curl.exe -s -o NUL -w "%{http_code} %{url_effective}\n" http://127.0.0.1:5174/mdm/numbering-rules
```

Results:

- `/mdm`: HTTP 200
- `/mdm/numbering-rules`: HTTP 200

## 6. Boundary Audit

This B2 task only added the UI layer and report.

No B2 changes were made to:

- DocumentKernel
- SalesOrder
- migrations
- Identity
- Bootstrap

Current worktree still contains unrelated/pre-existing WIP in backend, Identity, migration, and verification artifacts. Those files were not cleaned, reverted, committed, or pushed.

## 7. Gate

Status:

```text
G2_DOCNO_001_B2_UI_READY
```

Commit:

- Not committed.

Push:

- NO PUSH.
