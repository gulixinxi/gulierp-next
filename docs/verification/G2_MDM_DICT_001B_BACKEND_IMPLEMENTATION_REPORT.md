# G2-MDM-DICT-001B Backend Implementation Report

## 1. Repo / Branch / HEAD

- Repo: `D:\guli\projects\gulierp-next`
- Branch: `master`
- HEAD: `edafc74 docs(mdm): plan dictionary model and implementation split`
- Scope: backend-only minimum dictionary implementation.
- NO PUSH.

## 2. Modified Files

Backend implementation:

- `apps/api/GuliERP.Api/Mdm/MdmEndpoints.cs`
- `modules/mdm/GuliERP.Mdm.Application/IMdmMasterData002Services.cs`
- `modules/mdm/GuliERP.Mdm.Application/MdmDtos.cs`
- `modules/mdm/GuliERP.Mdm.Application/MdmErrorCodes.cs`
- `modules/mdm/GuliERP.Mdm.Application/MdmPermissions.cs`
- `modules/mdm/GuliERP.Mdm.Application/MdmPolicies.cs`
- `modules/mdm/GuliERP.Mdm.Domain/Entities/DictionaryType.cs`
- `modules/mdm/GuliERP.Mdm.Domain/Entities/DictionaryItem.cs`
- `modules/mdm/GuliERP.Mdm.Infrastructure/DependencyInjection.cs`
- `modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmDictionaryService.cs`
- `modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/MdmDbContext.cs`
- `modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/Configurations/DictionaryTypeConfiguration.cs`
- `modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/Configurations/DictionaryItemConfiguration.cs`
- `modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/20260825014004_AddMdmDictionaryTypesAndItems.cs`
- `modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/20260825014004_AddMdmDictionaryTypesAndItems.Designer.cs`
- `modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/MdmDbContextModelSnapshot.cs`

Tests:

- `tests/GuliERP.Mdm.Tests/GuliERP.Mdm.Tests.csproj`
- `tests/GuliERP.Mdm.Tests/MdmDictionaryServiceFacts.cs`
- `tests/GuliERP.Mdm.Tests/MdmServiceBoundaryArchitectureTests.cs`

Report:

- `docs/verification/G2_MDM_DICT_001B_BACKEND_IMPLEMENTATION_REPORT.md`

## 3. New Entities

### DictionaryType

Fields:

- `Id`
- `TenantId`
- `Code`
- `Name`
- `Description`
- `Status`
- `SortOrder`
- `IsSystem`
- `CreatedAt`
- `UpdatedAt`
- `ConcurrencyVersion`
- `Items`

### DictionaryItem

Fields:

- `Id`
- `TenantId`
- `DictionaryTypeId`
- `Code`
- `Name`
- `Value`
- `Description`
- `Status`
- `SortOrder`
- `IsDefault`
- `IsSystem`
- `CreatedAt`
- `UpdatedAt`
- `ConcurrencyVersion`
- `DictionaryType`

Implementation notes:

- Tenant scoped through `TenantId` and `ICurrentTenant`.
- Status follows existing `MasterDataStatus`.
- Code canonicalization and validation reuse the existing MDM code validation path.
- Mutation concurrency uses `ExpectedConcurrencyVersion`.
- `IsSystem=true` records are protected from update and status changes in this phase.
- No multilingual, hierarchy, dynamic form, numbering-rule, or configuration-center behavior was added.

## 4. New Tables / Migration

Migration:

- `20260825014004_AddMdmDictionaryTypesAndItems`

Tables:

- `gulierp_dictionary_type`
- `gulierp_dictionary_item`

Indexes and constraints:

- `DictionaryType`: unique `(TenantId, Code)`.
- `DictionaryItem`: unique `(TenantId, DictionaryTypeId, Code)`.
- Indexes include tenant/status/sort-order oriented access paths and the dictionary type foreign key.

Migration safety:

- `Up` creates only the two new dictionary tables and indexes.
- `Down` drops only the two new dictionary tables.
- No existing table, existing migration, or existing schema object is dropped or rewritten.

## 5. API Endpoints

Dictionary type:

- `GET /api/v1/mdm/dictionary-types`
- `GET /api/v1/mdm/dictionary-types/{id}`
- `POST /api/v1/mdm/dictionary-types`
- `PUT /api/v1/mdm/dictionary-types/{id}`
- `PATCH /api/v1/mdm/dictionary-types/{id}/status`

Dictionary item:

- `GET /api/v1/mdm/dictionary-types/{typeId}/items`
- `GET /api/v1/mdm/dictionary-items/{id}`
- `POST /api/v1/mdm/dictionary-types/{typeId}/items`
- `PUT /api/v1/mdm/dictionary-items/{id}`
- `PATCH /api/v1/mdm/dictionary-items/{id}/status`

Authorization:

- `MdmPolicies.DictionaryRead`
- `MdmPolicies.DictionaryManage`
- No authentication bypass was added.
- No tenant/company security boundary was weakened.

## 6. DTO / Service

DTOs added:

- `DictionaryTypeDto`
- `CreateDictionaryTypeRequest`
- `UpdateDictionaryTypeRequest`
- `DictionaryItemDto`
- `CreateDictionaryItemRequest`
- `UpdateDictionaryItemRequest`
- `ChangeDictionaryStatusRequest`

Service contract:

- `IMdmDictionaryService`

Service implementation:

- `MdmDictionaryService`

Service behavior:

- list/get/create/update/status for dictionary types.
- list/get/create/update/status for dictionary items.
- duplicate code protection inside tenant/type boundaries.
- same item code is allowed under different dictionary types.
- tenant isolation is enforced in all reads and mutations.
- concurrency conflict raises MDM validation error.
- missing tenant/type raises MDM validation error.
- system dictionary type/item update and status mutation are blocked.

## 7. Test Coverage

New focused tests in `MdmDictionaryServiceFacts` cover:

- create `DictionaryType` succeeds.
- duplicate `DictionaryType.Code` is rejected.
- update `DictionaryType` succeeds.
- update concurrency conflict is rejected.
- status change for `DictionaryType` succeeds.
- create `DictionaryItem` succeeds.
- duplicate `DictionaryItem.Code` in the same type is rejected.
- same item code under a different type is allowed.
- update `DictionaryItem` succeeds.
- status change for `DictionaryItem` succeeds.
- tenant-isolated reads.
- `IsSystem` protection for type and item.

Existing architecture allowlist was updated for the new MDM dictionary service/configuration files.

## 8. Verification Commands And Results

PASS:

- `dotnet build apps/api/GuliERP.Api/GuliERP.Api.csproj --no-restore -m:1 -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false -v:minimal`
  - Result: PASS, 0 warnings, 0 errors.
- `dotnet test tests/GuliERP.Mdm.Tests/GuliERP.Mdm.Tests.csproj --no-restore --no-build --filter MdmDictionaryServiceFacts --logger "console;verbosity=normal"`
  - Result: PASS, 12 passed.
- `dotnet test tests/GuliERP.Api.Tests/GuliERP.Api.Tests.csproj --no-restore --no-build --logger "console;verbosity=minimal"`
  - Result: PASS, 32 passed.

Known non-blocking verification gap:

- `dotnet test tests/GuliERP.Mdm.Tests/GuliERP.Mdm.Tests.csproj --no-restore --no-build --logger "console;verbosity=minimal"`
  - Result: FAIL, 233 passed, 2 failed.
  - Failing tests:
    - `MdmCurrentTenantParallelTests.MdmSeed_ResolveSeedFilePath_Explicit_Path_Falls_Through_To_Walk_Up_When_Missing`
    - `MdmCurrentTenantParallelTests.MdmSeed_ResolveSeedFilePath_Walks_Up_From_CurrentDirectory`
  - Observed reason: expected temp path, actual resolved path under `D:\guli\projects\gulierp-next\data/bootstrap...`.
  - This appears tied to existing untracked `data/` WIP and seed path resolution, not to the new dictionary implementation.
  - No unrelated WIP was cleaned or modified to force this full-suite result green.

Command note:

- An earlier `dotnet build ... --no-restore` attempt returned a blank build failure with 0 warnings and 0 errors. The verified build command above disables parallel/shared node reuse and passed.
- An earlier `dotnet test ... --filter MdmDictionaryServiceFacts` attempt hung without output; the verified `--no-build` run above passed 12/12.

## 9. Frontend 001C Need

Frontend work is still required in `G2-MDM-DICT-001C`:

- `apps/web/src/api/mdm/dictionary.ts`
- `apps/web/src/views/mdm/DictionaryList.vue`
- `/mdm/dictionaries` route
- navigation and master data workbench entry
- UOM-style drawer, status, search, paging, loading, empty, and error handling

No frontend page was added in this stage.

## 10. UOM Standard Path Reuse

The backend follows the UOM canonical path from `G2_MDM_UI_001E`:

- tenant-scoped MDM entity.
- code/name/description/status/sort/concurrency DTO shape.
- service-centered application contract.
- minimal API endpoint group under `/api/v1/mdm`.
- create/update/status mutation separation.
- duplicate code validation.
- optimistic concurrency on mutations.
- ProblemDetails-compatible MDM validation exception flow through existing handlers.

The dictionary path is intentionally two-level because dictionary items belong to a dictionary type; otherwise it keeps the same CRUD shape as UOM.

## 11. DocumentNumberService / Numbering Rule Boundary

This implementation does not add numbering-rule management.

- `DocumentNumberService` remains a numbering generation capability.
- DictionaryType/DictionaryItem are generic option-set master data.
- No preview-number, rule-expression, document prefix, reset-cycle, or numbering policy endpoint was added.
- Numbering rules should remain a separate goal.

## 12. Explicit Scope Boundaries

- no Identity changes.
- no Bootstrap changes.
- no G2-005 changes.
- no frontend page changes.
- no purchase/sales/inventory changes.
- no numbering-rule changes.
- no unrelated WIP cleanup.
- NO PUSH.

## 13. Current Result

`G2-MDM-DICT-001B` backend minimum dictionary implementation is code-ready with focused dictionary tests and API tests passing. Full MDM test suite still has two environment/worktree-sensitive seed path failures unrelated to the dictionary implementation.

## 14. Pre-Commit Audit File List

`git status --short` was executed before this audit. The repository still has many unrelated modified and untracked files outside this stage. The 001B commit candidate should include only the files below.

### Entity / Domain

- NEW: `modules/mdm/GuliERP.Mdm.Domain/Entities/DictionaryType.cs`
- NEW: `modules/mdm/GuliERP.Mdm.Domain/Entities/DictionaryItem.cs`

### EF Mapping / DbContext

- NEW: `modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/Configurations/DictionaryTypeConfiguration.cs`
- NEW: `modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/Configurations/DictionaryItemConfiguration.cs`
- MODIFIED: `modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/MdmDbContext.cs`

### Migration

- NEW: `modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/20260825014004_AddMdmDictionaryTypesAndItems.cs`
- NEW: `modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/20260825014004_AddMdmDictionaryTypesAndItems.Designer.cs`
- MODIFIED: `modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/MdmDbContextModelSnapshot.cs`

### Service / Application

- MODIFIED: `modules/mdm/GuliERP.Mdm.Application/IMdmMasterData002Services.cs`
- NEW: `modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmDictionaryService.cs`
- MODIFIED: `modules/mdm/GuliERP.Mdm.Infrastructure/DependencyInjection.cs`

### API Endpoints

- MODIFIED: `apps/api/GuliERP.Api/Mdm/MdmEndpoints.cs`

### DTO / Contracts

- MODIFIED: `modules/mdm/GuliERP.Mdm.Application/MdmDtos.cs`

### Permissions / Policies / Error Codes

- MODIFIED: `modules/mdm/GuliERP.Mdm.Application/MdmPermissions.cs`
- MODIFIED: `modules/mdm/GuliERP.Mdm.Application/MdmPolicies.cs`
- MODIFIED: `modules/mdm/GuliERP.Mdm.Application/MdmErrorCodes.cs`

### Tests

- MODIFIED: `tests/GuliERP.Mdm.Tests/GuliERP.Mdm.Tests.csproj`
- NEW: `tests/GuliERP.Mdm.Tests/MdmDictionaryServiceFacts.cs`
- MODIFIED: `tests/GuliERP.Mdm.Tests/MdmServiceBoundaryArchitectureTests.cs`

### Verification Report

- NEW: `docs/verification/G2_MDM_DICT_001B_BACKEND_IMPLEMENTATION_REPORT.md`

### Unrelated WIP Check

No suspicious unrelated WIP is required for the 001B commit candidate above. The worktree contains unrelated files such as Identity/Foundation changes, `apps/api/GuliERP.Api/Organization/OrganizationEndpoints.cs`, sales/runtime reports, untracked `data/`, test result folders, and other planning documents. They should not be staged for 001B.

## 15. Migration Safety Audit

Migration audited:

- `20260825014004_AddMdmDictionaryTypesAndItems`

Findings:

- `Up` creates only `mdm.gulierp_dictionary_type` and `mdm.gulierp_dictionary_item`.
- `Up` creates only dictionary-related primary keys, foreign key, indexes, and unique constraints.
- `Down` drops only `mdm.gulierp_dictionary_item` and `mdm.gulierp_dictionary_type`.
- No existing table is dropped.
- No non-dictionary existing business table is altered.
- No data is deleted or updated.
- Table names follow the current MDM `mdm.gulierp_*` naming style.
- Index names follow the current `ix_gulierp_*` / `ux_gulierp_*` style.
- `DictionaryType` unique constraint exists: `(TenantId, Code)` via `ux_gulierp_dictionary_type_tenant_code`.
- `DictionaryItem` unique constraint exists: `(TenantId, DictionaryTypeId, Code)` via `ux_gulierp_dictionary_item_tenant_type_code`.
- `Status` is mapped as integer through EF configuration, consistent with MDM status style.
- `ConcurrencyVersion` is configured as a concurrency token.
- Audit-style fields are present in the migration: `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy`.

MIGRATION_SAFE_TO_COMMIT = YES

## 16. Endpoint Contract Audit

`apps/api/GuliERP.Api/Mdm/MdmEndpoints.cs` adds only the dictionary endpoint group under the existing `/api/v1/mdm` group.

Confirmed endpoints:

- `GET /api/v1/mdm/dictionary-types`
- `GET /api/v1/mdm/dictionary-types/{id}`
- `POST /api/v1/mdm/dictionary-types`
- `PUT /api/v1/mdm/dictionary-types/{id}`
- `PATCH /api/v1/mdm/dictionary-types/{id}/status`
- `GET /api/v1/mdm/dictionary-types/{typeId}/items`
- `GET /api/v1/mdm/dictionary-items/{id}`
- `POST /api/v1/mdm/dictionary-types/{typeId}/items`
- `PUT /api/v1/mdm/dictionary-items/{id}`
- `PATCH /api/v1/mdm/dictionary-items/{id}/status`

Negative checks:

- No purchase endpoint was added.
- No sales endpoint was added.
- No inventory endpoint was added.
- No numbering-rule endpoint was added.
- No Identity endpoint was added.
- No Bootstrap endpoint was added.
- No `AllowAnonymous` or authentication bypass was added to the dictionary endpoints.
- Read endpoints require `MdmPolicies.DictionaryRead`.
- Mutation endpoints require `MdmPolicies.DictionaryManage`.

Endpoint contract audit conclusion: PASS.

## 17. MDM Full Test Failure Attribution

Command:

- `dotnet test tests/GuliERP.Mdm.Tests/GuliERP.Mdm.Tests.csproj --no-restore --no-build --logger "console;verbosity=minimal"`

Result:

- FAIL, 233 passed, 2 failed, 0 skipped, 235 total.

Failures:

- `GuliERP.Mdm.Tests.MdmCurrentTenantParallelTests.MdmSeed_ResolveSeedFilePath_Explicit_Path_Falls_Through_To_Walk_Up_When_Missing`
  - Summary: expected a temp path under `C:\Users\Administrator\AppData\Local\Temp...`; actual path resolved under `D:\guli\projects\gulierp-next\data/bootstrap...`.
  - Attribution: KNOWN_UNRELATED_WIP_FAILURE.
- `GuliERP.Mdm.Tests.MdmCurrentTenantParallelTests.MdmSeed_ResolveSeedFilePath_Walks_Up_From_CurrentDirectory`
  - Summary: expected a temp path under `C:\Users\Administrator\AppData\Local\Temp...`; actual path resolved under `D:\guli\projects\gulierp-next\data/bootstrap...`.
  - Attribution: KNOWN_UNRELATED_WIP_FAILURE.

Dictionary relationship:

- These failures exercise seed file path resolution, not dictionary entity, EF mapping, service, endpoint, DTO, permission, or migration behavior.
- The focused dictionary test suite passed 12/12 in the same worktree.
- The untracked `data/` directory was not cleaned or modified.
- The failing tests were not edited to force a green full MDM result.
- Full MDM PASS is not claimed.

## 18. Repeated Verification For Pre-Commit Audit

PASS:

- `dotnet build apps/api/GuliERP.Api/GuliERP.Api.csproj --no-restore -m:1 -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false -v:minimal`
  - Result: PASS, 0 warnings, 0 errors.
- `dotnet test tests/GuliERP.Mdm.Tests/GuliERP.Mdm.Tests.csproj --no-restore --no-build --filter MdmDictionaryServiceFacts --logger "console;verbosity=normal"`
  - Result: PASS, 12 passed.
- `dotnet test tests/GuliERP.Api.Tests/GuliERP.Api.Tests.csproj --no-restore --no-build --logger "console;verbosity=minimal"`
  - Result: PASS, 32 passed.

Scoped diff check:

- `git diff --check -- docs/verification/G2_MDM_DICT_001B_BACKEND_IMPLEMENTATION_REPORT.md`
  - Result: PASS.
- `git diff --check -- <001B scoped file list>`
  - Result: PASS with LF/CRLF warnings only.

MDM full tests:

- `dotnet test tests/GuliERP.Mdm.Tests/GuliERP.Mdm.Tests.csproj --no-restore --no-build --logger "console;verbosity=minimal"`
  - Result: FAIL, 233 passed, 2 failed.
  - Failure attribution: KNOWN_UNRELATED_WIP_FAILURE.

## 19. Commit Grouping Recommendation

Recommended 001B commit message:

- `feat(mdm): add dictionary backend CRUD`

Recommended commit content:

- Include all files listed in section 14.
- Exclude unrelated modified/untracked WIP from `git status --short`.
- Before committing, run `git diff --cached --name-only` and confirm it contains only the section 14 file list.

Commit recommendation:

- Recommended to commit after the user explicitly asks for the 001B commit.
- Do not commit in this audit turn.
- Do not push.
