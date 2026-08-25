# G2-MDM-DICT-001A Dictionary Model Audit and Plan

## 1. Repo / Branch / HEAD

- Repo: `D:\guli\projects\gulierp-next`
- Branch: `master`
- HEAD at audit time: `5c548ce feat(mdm): complete employee master mutations UI`
- Scope: audit and planning only.
- Commit policy: this report is not committed in G2-MDM-DICT-001A.
- Push policy: NO PUSH.

## 2. Dirty / Untracked Summary

`git status --short` was executed before this audit. The worktree contains 104 dirty or untracked entries.

Observed categories:

- Existing modified files under `.gitignore`, `apps/api`, `modules/foundation`, `modules/identity`, `modules/mdm`, `tests`, `tools/dev`, and existing verification docs.
- Existing untracked runtime/evidence folders such as `.runtime-browser-profile/`, `.stack-logs/`, `.stack-pids.json`, `artifacts/`, `data/`, `tests/**/TestResults/`, and `tests/_evidence_trx/`.
- Existing untracked planning/business/architecture/audit/review/verification documents.
- Existing untracked module and test files related to validation, employee, operator evidence, and discovery work.

No cleanup was performed. Unrelated WIP was preserved.

## 3. Existing Dictionary / Dict Asset Audit

### 3.1 Backend Source

Current source has Dictionary as a boundary concept only:

- `modules/foundation/GuliERP.Foundation/FoundationBoundary.cs`
  - `FoundationBoundary.PhaseOneEntities` includes `"Dictionary"`.
- `modules/foundation/GuliERP.Foundation/ModelBoundaries.cs`
  - `DictionaryBoundary` is declared as `"Reference data and controlled vocabulary boundary."`
  - Implementation status is `"BoundaryOnly"`.
- `docs/foundation/FOUNDATION_BOUNDARY.md`
  - Documents `Dictionary` as the controlled vocabulary/reference data boundary.

No complete runtime Dictionary capability was found.

### 3.2 Backend Entity / API / Migration / Tests Status

| Question | Current finding |
| --- | --- |
| Dictionary / Dict entity exists? | No runtime entity found. Only `DictionaryBoundary` boundary placeholder exists. |
| DictionaryType / DictionaryItem structure exists? | No. |
| Database table or EF mapping exists? | No. `FoundationDbContext` currently has no `DbSet` entries. `MdmDbContext` has `Uoms`, `ItemCategories`, `Items`, `BusinessPartners`, `Warehouses`, and `Locations` only. |
| Migration exists? | No dictionary migration/table content found in migration files. |
| Backend service exists? | No `Dictionary` admin/query service found. |
| API endpoint exists? | No `MapDictionary...`, `/api/v1/mdm/dictionary-types`, `/api/v1/mdm/dictionary-items`, or `/mdm/dictionaries` backend route found. |
| Tests exist? | No Dictionary CRUD/service/API tests found. `FoundationBoundaryTests` only verifies that the boundary name exists. |

Generic C# `Dictionary<TKey,TValue>` usages were found in error handling, endpoint problem details, identity seed code, request logging, and test harness output. These are not business dictionary assets.

### 3.3 Frontend Page / API Client / Mock Status

| Question | Current finding |
| --- | --- |
| Menu entry exists? | No active `基础字典` navigation entry in `apps/web/src/layout/navigation.ts`. |
| Route exists? | No `/mdm/dictionaries` route in `apps/web/src/router/mdm.ts`. |
| Page exists? | No `DictionaryList.vue` found. |
| API client exists? | No `apps/web/src/api/mdm/dictionary.ts` found. |
| Mock data exists? | No dictionary mock in `apps/web/src/mock/mdm.ts`; that file contains legacy UOM, ItemCategory, and Item mock data only. |
| Workbench card exists? | `MasterDataWorkbench.vue` lists `基础字典` only under deferred modules, with reason `未发现可直接接入的 CRUD API`. |
| Lookup helper relevance? | `apps/web/src/components/LookupDialog.vue` is a SalesOrder-oriented lookup helper, not Dictionary CRUD. |

### 3.4 Existing Documentation Definition

Relevant documents consistently describe Dictionary as a future or missing capability:

- `docs/planning/G2_MDM_UI_001_MASTER_DATA_WORKBENCH_PLAN.md`
  - Records Dictionary as `Boundary/design references exist; no CRUD Dictionary module found`.
  - Explicitly excludes Dictionary admin UI from the first MDM UI phase.
- `docs/planning/G2_MDM_UI_001E_UOM_REFERENCE_PATH_STANDARD.md`
  - States that generic dictionaries are not ready for page-first implementation.
  - Requires DTO/API/page design first and warns not to revive `mock/mdm.ts` as an acceptance substitute.
- `docs/product/CORE_MODULE_SCOPE_V1.md` and business specs mention Dictionary/reference data as a desired/core capability, but do not provide a frozen runtime model or API contract.
- Document Kernel reports and MDM UI planning keep numbering separate from dictionary administration.

## 4. Audit Conclusion

The current system does not have a complete basic-dictionary module.

The only implemented assets are:

- Foundation boundary naming.
- Product/planning references.
- Deferred workbench visibility.

The following are absent and must be implemented in a later goal before a real Dictionary page can be accepted:

- Runtime entities.
- EF configuration and migration.
- DTOs.
- Service contracts and implementation.
- API endpoints and authorization policies.
- Frontend API client.
- Frontend page and route/menu entry.
- Tests.

## 5. Minimum Model Freeze

First-phase Dictionary should solve only system-maintainable generic option sets. It must not become a complex configuration center.

### 5.1 DictionaryType

Recommended fields:

| Field | Purpose |
| --- | --- |
| `id` | Opaque technical identifier. |
| `code` | Stable type code using the UOM/master-data code rule. |
| `name` | Display name. |
| `description` | Optional description. |
| `status` | Active/inactive. |
| `sortOrder` | Display ordering. |
| `isSystem` | System-owned type marker. |
| `createdAt` | Audit timestamp. |
| `updatedAt` | Audit timestamp. |
| `concurrencyVersion` | Optimistic concurrency token. |

### 5.2 DictionaryItem

Recommended fields:

| Field | Purpose |
| --- | --- |
| `id` | Opaque technical identifier. |
| `dictionaryTypeId` | Parent dictionary type id. |
| `code` | Stable item code using the UOM/master-data code rule. |
| `name` | Display name. |
| `value` | Runtime value exposed to forms or business flows. |
| `description` | Optional description. |
| `status` | Active/inactive. |
| `sortOrder` | Display ordering inside a type. |
| `isDefault` | Default option marker within the type. |
| `isSystem` | System-owned item marker. |
| `createdAt` | Audit timestamp. |
| `updatedAt` | Audit timestamp. |
| `concurrencyVersion` | Optimistic concurrency token. |

### 5.3 First-Phase Constraints

- `code` follows the existing UOM/master-data code validation standard.
- Type code uniqueness should be scoped by the owning runtime boundary selected in 001B.
- Item code/value uniqueness should be scoped inside one dictionary type.
- `isSystem=true` records cannot be deleted; editing should be restricted to safe fields only.
- Do not add delete in first phase; use status enable/disable.
- Do not implement multilingual dictionaries in first phase.
- Do not implement hierarchy dictionaries in first phase.
- Do not implement dynamic form configuration in first phase.
- Do not add fine-grained permission expansion in this audit.
- Do not implement numbering rules here.
- Do not mix with `DocumentNumberService`.

## 6. API Design Recommendation

Use the UOM contract shape: list/detail if needed, create, update with optimistic concurrency, and status change.

Recommended API surface:

| Method | Path | Purpose |
| --- | --- | --- |
| `GET` | `/api/v1/mdm/dictionary-types` | List dictionary types with keyword/status paging. |
| `POST` | `/api/v1/mdm/dictionary-types` | Create dictionary type. |
| `PUT` | `/api/v1/mdm/dictionary-types/{id}` | Update dictionary type with `expectedConcurrencyVersion`. |
| `PATCH` | `/api/v1/mdm/dictionary-types/{id}/status` | Enable/disable dictionary type. |
| `GET` | `/api/v1/mdm/dictionary-types/{typeId}/items` | List items under one dictionary type. |
| `POST` | `/api/v1/mdm/dictionary-types/{typeId}/items` | Create item under one dictionary type. |
| `PUT` | `/api/v1/mdm/dictionary-items/{id}` | Update dictionary item with `expectedConcurrencyVersion`. |
| `PATCH` | `/api/v1/mdm/dictionary-items/{id}/status` | Enable/disable dictionary item. |

Recommended backend file pattern:

- Domain entities: `DictionaryType`, `DictionaryItem`.
- EF configurations for explicit table names, indexes, max lengths, status, sort order, and concurrency version.
- DTOs in the owning application module.
- Service interface and implementation using the same validation/concurrency approach as UOM.
- Endpoint group in `apps/api/GuliERP.Api/Mdm/MdmEndpoints.cs` only after the backend module contract is ready.
- Tests for entity rules, service validation, endpoint auth/CSRF behavior, pagination, uniqueness, system-record restrictions, and concurrency.

Open design point for 001B:

- Confirm the owning bounded context. Since this is Master Data UI and general reference data, MDM is the current recommended API surface. If Foundation must own the tables, 001B must explicitly define the dependency boundary before coding.

## 7. Frontend Page Design Recommendation

Recommended frontend files:

- API client: `apps/web/src/api/mdm/dictionary.ts`
- Page: `apps/web/src/views/mdm/DictionaryList.vue`
- Route: `/mdm/dictionaries`
- Menu label: `基础字典`
- Workbench card: `基础字典`

Recommended page shape:

- Left side or top section: dictionary type list.
- Right side or lower section: items for the selected dictionary type.
- Shared toolbar search for type keyword and item keyword.
- Status filter for active/inactive/all.
- Create/edit drawers for types and items using `MdmFormDrawer`.
- Status badge using `MdmStatusBadge`.
- Enable/disable using the UOM status-confirmation pattern.
- Loading, empty, and error states using existing MDM components and CSS.
- No mock fallback.
- No disconnected local-only CRUD.

## 8. UOM Standard Path Reuse

Dictionary should reuse the UOM canonical path defined in `docs/planning/G2_MDM_UI_001E_UOM_REFERENCE_PATH_STANDARD.md`:

- Menu entry in `navigation.ts`.
- Route in `router/mdm.ts`.
- Page component under `views/mdm`.
- API client under `api/mdm`.
- DTO-to-UI and UI-to-wire conversion inside the API client.
- Opaque string ids in frontend.
- `list/create/update/status` naming convention.
- Fresh detail/read before mutation when concurrency is required.
- `authentication_required` re-thrown to global auth handling.
- CSRF and cookies inherited from unified `api/http.ts`.
- `MdmFormDrawer`, `MdmPagination`, `MdmEmptyState`, `MdmStatusBadge`, and existing MDM page CSS.
- Empty successful lists must not be shown as API failures.

Do not mark a page failed only because direct unauthenticated API access returns `401 authentication_required`. That is expected without an ASP.NET Core Identity cookie. Runtime acceptance must distinguish:

- Unauthenticated direct API call: expected `401 authentication_required`.
- Browser with valid login cookie and CSRF token: expected normal list/create/update/status behavior, subject to permissions.

## 9. Boundary with Numbering / DocumentNumberService

Basic Dictionary and numbering rules are separate capabilities.

- Dictionary manages generic option sets and controlled vocabulary.
- `IDocumentNumberService` / `DocumentNumberService` generates document numbers for business documents.
- `DocumentNumberService` is not a numbering-rule management page or CRUD contract.
- Numbering rules must not be implemented as DictionaryType/DictionaryItem.
- Numbering rule management should be a separate goal with rule list, create/edit/enable-disable, preview next number, collision behavior, period behavior, audit, and runtime acceptance.

## 10. Follow-up Split Recommendation

### 10.1 G2-MDM-DICT-001B: Backend Minimum Model + API + Tests

- Scope: implement `DictionaryType` and `DictionaryItem` backend model, DTOs, service, endpoints, authorization policy alignment, validation, concurrency, and tests.
- Migration needed: Yes.
- Tests needed: Yes; unit/service/API/integration tests should cover validation, uniqueness, status, system record restrictions, concurrency, and auth/CSRF.
- Existing MDM impact: Additive if implemented under new tables/endpoints.
- Rollback: Revert code and migration; if migration has been applied, use the project-standard rollback path for the generated migration.
- Data initialization: Optional minimal seed for system dictionary types only after ownership and seed responsibility are explicit.

### 10.2 G2-MDM-DICT-001C: Frontend Page + API Client + Menu Integration

- Scope: add `dictionary.ts`, `DictionaryList.vue`, `/mdm/dictionaries` route, navigation entry, and workbench card.
- Migration needed: No.
- Tests needed: Typecheck/build, plus component or route smoke where available.
- Existing MDM impact: Additive UI entry and page; no change to UOM/ItemCategory/Warehouse/Location/Employee behavior.
- Rollback: Revert frontend files and route/menu/workbench entry.
- Data initialization: Depends on 001B; the page should handle empty lists without mock fallback.

### 10.3 G2-MDM-DICT-001D: Authenticated Runtime CRUD Acceptance

- Scope: verify true browser-session CRUD for dictionary types and items.
- Migration needed: No, unless 001B reveals a schema gap before acceptance.
- Tests needed: Runtime route/API evidence plus typecheck/build if frontend changed since 001C.
- Existing MDM impact: None expected; validates isolated dictionary routes.
- Rollback: No code rollback expected unless acceptance finds a defect.
- Data initialization: Requires either seeded/system test data or operator-created `G2_MDM_DICT_001D` prefixed test data. Do not fabricate authentication or bypass CSRF.

## 11. Risks

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Ownership ambiguity between Foundation and MDM | Wrong dependency direction or misplaced schema | Decide owner in 001B before coding. |
| Dictionary becomes a configuration platform | Scope creep and unstable contracts | Keep 001B to generic option sets only. |
| Numbering rules get mixed into Dictionary | Incorrect model and future migration pain | Keep numbering in a separate goal. |
| Mock fallback reappears | False acceptance without backend | Follow UOM standard: real API only. |
| System records are edited too freely | Seed/runtime integrity risk | Use `isSystem` restrictions and tests. |
| Unauthenticated 401 misread as page failure | False negative acceptance | Separate direct unauthenticated API checks from browser logged-in runtime checks. |
| Migration touches existing MDM tables | Wider rollback risk | 001B should be additive and isolated. |

## 12. Boundary Statements

- no Identity changes
- no Bootstrap changes
- no G2-005 changes
- no migration changes in this audit
- no database schema changes in this audit
- no backend code changes in this audit
- no frontend runtime code changes in this audit
- no Dictionary CRUD implementation in this audit
- no numbering-rule implementation
- no purchasing, sales, or inventory document work
- unrelated WIP preserved
- NO PUSH

## 13. Verification

Required command for this audit:

- `git diff --check -- docs/planning/G2_MDM_DICT_001A_DICTIONARY_MODEL_AUDIT_AND_PLAN.md`

Build commands were not required in this audit because only this planning document was added and no frontend/backend code was modified.
