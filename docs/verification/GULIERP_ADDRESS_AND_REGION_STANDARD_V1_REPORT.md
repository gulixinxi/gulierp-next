# GULIERP_ADDRESS_AND_REGION_STANDARD_V1_REPORT

| Field | Value |
|---|---|
| Goal | `GULIERP_ADDRESS_AND_REGION_STANDARD_V1` |
| Nature | Standard / contract only |
| Repository | `D:\guli\projects\gulierp-next` |
| Branch | `master` |
| HEAD | `139fe1e940258d71b85d328d88b4e1358c0f7b1e` |
| Standard output | `docs/business/GULIERP_ADDRESS_AND_REGION_STANDARD_V1.md` |
| Verification output | `docs/verification/GULIERP_ADDRESS_AND_REGION_STANDARD_V1_REPORT.md` |
| Final gate | `GULIERP_ADDRESS_AND_REGION_STANDARD_V1_VERIFIED` |

## 1. Predecessor Gates

Required predecessor files were read and consumed:

| File | Gate / status consumed |
|---|---|
| `docs/business/GULIERP_MASTER_DATA_CODE_RULE_STANDARD_V1.md` | `GULIERP_MASTER_DATA_CODE_RULE_STANDARD_V1_FROZEN` |
| `docs/verification/GULIERP_MASTER_DATA_CODE_RULE_STANDARD_V1_REPORT.md` | `GULIERP_MASTER_DATA_CODE_RULE_STANDARD_V1_VERIFIED` |
| `docs/business/GULIERP_MASTER_DATA_COMMON_FIELD_CONTRACT_V1.md` | `GULIERP_MASTER_DATA_COMMON_FIELD_CONTRACT_V1_FROZEN` |
| `docs/verification/GULIERP_MASTER_DATA_COMMON_FIELD_CONTRACT_V1_REPORT.md` | `GULIERP_MASTER_DATA_COMMON_FIELD_CONTRACT_V1_VERIFIED` |
| `docs/verification/GULIERP_MDM_UI_AND_COMMON_FIELD_AUDIT_V1_REPORT.md` | `GULIERP_MDM_UI_AND_COMMON_FIELD_AUDIT_V1_VERIFIED` |

## 2. Branch And HEAD

| Check | Observed value |
|---|---|
| Branch | `master` |
| HEAD | `139fe1e940258d71b85d328d88b4e1358c0f7b1e` |

## 3. Initial Git Status

The worktree already contained existing WIP before this goal. This goal did not clean, revert, stage or modify it.

```text
 M apps/api/GuliERP.Api/Program.cs
 M apps/web/src/components/mdm/MdmFormDrawer.vue
 M apps/web/src/components/mdm/MdmListToolbar.vue
 M apps/web/src/design-system/components/document.css
 M apps/web/src/design-system/components/table.css
 M apps/web/src/design-system/tokens/sizing.css
 M apps/web/src/design-system/tokens/spacing.css
 M apps/web/src/router.ts
 M apps/web/src/views/auth/Login.vue
 M apps/web/src/views/mdm/BusinessPartnerList.vue
 M apps/web/src/views/mdm/ItemList.vue
 M apps/web/src/views/sales-order/SalesOrderList.vue
 M apps/web/vite.config.ts
 M docs/governance/GOAL_REGISTRY.md
 M docs/verification/GULIERP_EMPLOYEE_CODE_CONTRACT_RECONCILIATION_001_REPORT.md
 M tests/GuliERP.Api.Tests/SalesRuntimeRegressionSourceFacts.cs
 M tests/GuliERP.Identity.IntegrationTests/EnterpriseBootstrapAndOrganizationTreeFacts.cs
 M tests/GuliERP.Identity.IntegrationTests/EnterpriseRolePackCrossTenantFacts.cs
 M tools/dev/check-runtime.ps1
 M tools/dev/start-stack.ps1
 M tools/dev/stop-stack.ps1
?? .stack-pids.json
?? apps/web/src/assets/
?? apps/web/src/design-system/tableColumns.ts
?? docs/business/DEV_LOWCODE_ARCHITECTURE_ANALYSIS_INSTRUCTIONS.md
?? docs/business/DEV_LOWCODE_ARCHITECTURE_INVENTORY_001.sql
?? docs/business/GULIERP_ADMINNET_PLATFORM_FOUNDATION_REFERENCE_AUDIT_001.md
?? docs/business/GULIERP_DEV_TO_CURRENT_ARCHITECTURE_GAP_AUDIT_001.md
?? docs/business/GULIERP_MASTER_DATA_CODE_RULE_STANDARD_V1.md
?? docs/business/GULIERP_MASTER_DATA_COMMON_FIELD_CONTRACT_V1.md
?? docs/business/GULIERP_NUMERIC_PRECISION_STANDARD_V1.md
?? docs/business/GULIERP_PRE_INVENTORY_ARCHITECTURE_DECISION_001.md
?? docs/business/GULIERP_REFERENCE_EVIDENCE_BOUNDARY_AND_COLLECTION_PLAN_001.md
?? docs/design/GULIERP_UI_LAYOUT_STANDARD_V2.md
?? docs/design/GULIERP_UI_SYSTEM_CALIBRATION_001_REPORT.md
?? docs/verification/GULIERP_ADMINNET_PLATFORM_FOUNDATION_REFERENCE_AUDIT_001_REPORT.md
?? docs/verification/GULIERP_DEV_TO_CURRENT_ARCHITECTURE_GAP_AUDIT_001_REPORT.md
?? docs/verification/GULIERP_EMPLOYEE_MASTER_FINAL_GOVERNANCE_CLOSURE_001_REPORT.md
?? docs/verification/GULIERP_EMPLOYEE_MASTER_POSTGRES_RUNTIME_CLOSURE_001_REPORT.md
?? docs/verification/GULIERP_FORMAL_ADMIN_ROLEPACK_CONTRACT_ALIGNMENT_001_REPORT.md
?? docs/verification/GULIERP_MASTER_DATA_CODE_RULE_STANDARD_V1_REPORT.md
?? docs/verification/GULIERP_MASTER_DATA_COMMON_FIELD_CONTRACT_V1_REPORT.md
?? docs/verification/GULIERP_MDM_UI_AND_COMMON_FIELD_AUDIT_V1_REPORT.md
?? docs/verification/GULIERP_NEXT_EMPLOYEE_OPERATOR_RUNTIME_FINAL_001_REPORT.md
?? docs/verification/GULIERP_NUMERIC_PRECISION_STANDARD_V1_REPORT.md
?? docs/verification/GULIERP_PRE_INVENTORY_ARCHITECTURE_DECISION_001_REPORT.md
?? docs/verification/ONLYIT_MATURE_ERP_REFERENCE_AUDIT_001.md
?? docs/verification/ONLYIT_NUMERIC_PRECISION_MATRIX_001.csv
?? docs/verification/ONLYIT_PRODUCTION_HARDENING_MATRIX_001.md
?? docs/verification/ONLYIT_SCHEMA_INVENTORY_001.csv
?? tools/dev/.quarantine/
```

## 4. Files Reviewed

| Area | Files / evidence |
|---|---|
| User goal | pasted task instructions for `GULIERP_ADDRESS_AND_REGION_STANDARD_V1`. |
| Predecessor standards | Code Rule standard/report, Common Field contract/report, MDM UI/Common Field audit report. |
| BusinessPartner entity | `modules/mdm/GuliERP.Mdm.Domain/Entities/BusinessPartner.cs`. |
| BusinessPartner persistence | `modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/Configurations/BusinessPartnerConfiguration.cs`. |
| Warehouse entity | `modules/mdm/GuliERP.Mdm.Domain/Entities/Warehouse.cs`. |
| Warehouse persistence | `modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/Configurations/WarehouseConfiguration.cs`. |
| MDM DTOs | `modules/mdm/GuliERP.Mdm.Application/MdmDtos.cs`. |
| Location entity | `modules/mdm/GuliERP.Mdm.Domain/Entities/Location.cs`. |
| Company entity | `modules/identity/GuliERP.Identity.Domain/Entities/Company.cs`. |
| OrganizationUnit entity | `modules/identity/GuliERP.Identity.Domain/Entities/OrganizationUnit.cs`. |
| Employee entity | `modules/identity/GuliERP.Identity.Domain/Entities/Employee.cs`. |
| Plant entity | `modules/identity/GuliERP.Identity.Domain/Entities/Plant.cs`. |
| Identity persistence | `modules/identity/GuliERP.Identity.Infrastructure/Persistence/IdentityDbContext.cs`. |
| Country seed placeholder | `data/bootstrap/reference/system/country.json`. |
| City draft | `data/bootstrap/reference/mdm/masterdata-v15/city-v15-draft.json`. |
| Architecture references | MDM and Identity architecture docs found through address/region search. |

## 5. Address-Bearing Entities Found

Repo-wide audit found 3 current postal-address-bearing entities:

| Entity | Classification | Evidence |
|---|---|---|
| BusinessPartner | `HAS_ADDRESS`, `TEXT_ONLY` | Address lines, City, Region, PostalCode and CountryCode on entity/DTO/config. |
| Warehouse | `HAS_ADDRESS`, `TEXT_ONLY` | Same text address shape as BusinessPartner. |
| Plant | `HAS_ADDRESS`, `TEXT_ONLY` | Address lines, City, Region and required CountryCode in Identity. |

Entities checked but not postal-address-bearing:

| Entity | Classification | Evidence |
|---|---|---|
| Company | `NO_ADDRESS` | Has legal/tax/currency/timezone fields, no postal address fields. |
| OrganizationUnit | `NO_ADDRESS` | Organization tree only. |
| Employee | `NO_ADDRESS` | HR-lite profile only. |
| Location | `NO_ADDRESS` for postal address | Has WarehouseId and bin coordinates: Aisle, Bay, Shelf. |

## 6. Existing Address Fields

| Entity | Current fields | Notes |
|---|---|---|
| BusinessPartner | `AddressLine1`, `AddressLine2`, `City`, `Region`, `PostalCode`, `CountryCode` | all nullable text fields; `CountryCode` max length 2. |
| Warehouse | `AddressLine1`, `AddressLine2`, `City`, `Region`, `PostalCode`, `CountryCode` | all nullable text fields; `CountryCode` max length 2. |
| Plant | `AddressLine1`, `AddressLine2`, `City`, `Region`, `CountryCode` | CountryCode required max length 2 fixed length; no PostalCode. |

Current max-length evidence:

| Field family | Current limit |
|---|---|
| AddressLine1 / AddressLine2 | 200 |
| City | 100 |
| Region | 100 |
| PostalCode | 20 |
| CountryCode | 2 |

## 7. Existing Country / Region Data Evidence

`data/bootstrap/reference/system/country.json` evidence:

| Field | Value |
|---|---|
| `item_count` | 0 |
| `classification` | `NEEDS_EXTERNAL_STANDARD_UPDATE` |
| `seed_status_at_file_level` | `NEEDS_EXTERNAL_STANDARD_UPDATE` |
| `items` | empty |
| Notes | file is manifest/schema demonstration only and contains no effective data |

`data/bootstrap/reference/mdm/masterdata-v15/city-v15-draft.json` evidence:

| Field | Value |
|---|---|
| source | onlyit demo MDB extraction |
| item_count | 538 |
| status | draft requiring operator review |
| decision | not an authoritative Country/Region seed |

## 8. Standard Decisions

| Decision | Result |
|---|---|
| Country canonical code | ISO 3166-1 alpha-2. |
| Country model | dedicated reference data with Code, Name, EnglishName, optional native/integration fields and active lifecycle. |
| Country source governance | authoritative, versioned, permissioned source required; manifest records source/version/effective/import metadata. |
| China administrative region code | GB/T 2260 or official equivalent authoritative source. |
| AdministrativeRegion model | single hierarchical parent-child table/equivalent. |
| Region uniqueness | `UNIQUE(CountryCode, Code)`. |
| Region lifecycle | inactive instead of hard delete. |
| International compatibility | supported region data plus free-text fallback. |
| PostalAddress contract | CountryCode, optional RegionId, snapshots, fallback text, address lines and editable PostalCode. |
| BusinessPartner V1 | primary address only. |
| Warehouse V1 | single address. |
| Plant | same semantic contract when remediated. |
| Company | registered address deferred. |
| Common Address table | rejected for V1. |
| Persistence | shared semantic contract with entity-owned component/additive columns. |
| PostalCode | address property, editable, no global regex. |
| Transaction address | future transaction documents must store immutable snapshots. |

## 9. Matrices Completed

| Matrix | Status |
|---|---|
| Address Matrix | complete |
| Region Field Matrix | complete |
| UI Behavior Matrix | complete |
| Compatibility Matrix | complete |
| Migration Direction Matrix | complete |
| Implementation Work Package Matrix | complete |

## 10. Compatibility Analysis

Compatibility outcome:

```text
ADDITIVE
BACKWARD_COMPATIBLE
NO_DESTRUCTIVE_ADDRESS_BACKFILL
```

Existing BusinessPartner and Warehouse text fields are preserved. Future Region references and snapshots are nullable/additive. Legacy text that cannot be matched safely remains visible and unbound.

Plant requires special care because current `CountryCode` is required; future remediation should not weaken that behavior accidentally and should add region/fallback fields compatibly.

No current conclusion requires immediate API breaking change.

## 11. Database Portability

The standard avoids provider-specific contract dependencies:

- no PostgreSQL-only `ltree`;
- no `jsonb` path as semantic requirement;
- no `citext`;
- no PostGIS;
- no provider-specific recursive feature as the only way to use the tree.

The Region hierarchy contract is portable to SQL Server, openGauss, GaussDB, KingbaseES, DM and OceanBase.

## 12. No Production Code Change

No production code was intentionally modified by this goal.

Allowed outputs only:

```text
docs/business/GULIERP_ADDRESS_AND_REGION_STANDARD_V1.md
docs/verification/GULIERP_ADDRESS_AND_REGION_STANDARD_V1_REPORT.md
```

## 13. No DB Change

No database connection was opened, no SQL was executed and no data was imported.

## 14. No Migration

No migration was created, edited or executed.

## 15. No Seed Change

No existing seed or JSON data file was modified.

`country.json` and the onlyit city draft were read only.

## 16. Closing Verification Results

Closing checks were run after writing the two allowed files.

| Check | Result |
|---|---|
| Target-file status | Only the two address/region goal files are new target files for this goal. |
| Full git status | Existing WIP remains and is not owned by this goal. |
| Whitespace check | Passed for this goal's two new files. |
| Sensitive/local-secret scan | No match in this goal's two files. |
| Placeholder-marker scan | No unresolved placeholder marker in this goal's two files. |

## 17. Final Git Status

Final status contains the same pre-existing WIP plus these two new goal files:

```text
?? docs/business/GULIERP_ADDRESS_AND_REGION_STANDARD_V1.md
?? docs/verification/GULIERP_ADDRESS_AND_REGION_STANDARD_V1_REPORT.md
```

No staging, commit or push was performed.

## 18. Final Gate

Gate checklist:

| Requirement | Result |
|---|---|
| Predecessor Code Rule gate verified | pass |
| Predecessor Common Field gate verified | pass |
| Predecessor MDM audit gate verified | pass |
| Repo-wide address audit completed | pass |
| Address-bearing entities identified | pass |
| Country canonical standard frozen | pass |
| Country data governance frozen | pass |
| China region standard frozen | pass |
| Region hierarchy frozen | pass |
| Region uniqueness frozen | pass |
| International fallback frozen | pass |
| PostalAddress contract frozen | pass |
| PostalCode contract frozen | pass |
| BusinessPartner address V1 frozen | pass |
| Warehouse address V1 frozen | pass |
| Multiple address decision frozen | pass |
| RegionId/text compatibility frozen | pass |
| Existing text preservation frozen | pass |
| UI cascader contract frozen | pass |
| Legacy unmatched behavior frozen | pass |
| Transaction snapshot rule frozen | pass |
| Seed version governance frozen | pass |
| Database portability verified | pass |
| Address Matrix complete | pass |
| Region Matrix complete | pass |
| UI Behavior Matrix complete | pass |
| Compatibility Matrix complete | pass |
| Implementation WPs formed | pass |
| No production code modified | pass |
| No database change | pass |
| No migration | pass |
| No seed change | pass |
| No commit | pass |
| No push | pass |

Final status:

`GULIERP_ADDRESS_AND_REGION_STANDARD_V1_VERIFIED`
