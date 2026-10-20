# GULIERP_MASTER_DATA_COMMON_FIELD_CONTRACT_V1_REPORT

| Field | Value |
|---|---|
| Goal | `GULIERP_MASTER_DATA_COMMON_FIELD_CONTRACT_V1` |
| Nature | Contract / standard only |
| Repository | `D:\guli\projects\gulierp-next` |
| Branch | `master` |
| HEAD | `139fe1e940258d71b85d328d88b4e1358c0f7b1e` |
| Contract output | `docs/business/GULIERP_MASTER_DATA_COMMON_FIELD_CONTRACT_V1.md` |
| Verification output | `docs/verification/GULIERP_MASTER_DATA_COMMON_FIELD_CONTRACT_V1_REPORT.md` |
| Final gate | `GULIERP_MASTER_DATA_COMMON_FIELD_CONTRACT_V1_VERIFIED` |

## 1. Predecessor Gate

Predecessor report reviewed:

`docs/verification/GULIERP_MDM_UI_AND_COMMON_FIELD_AUDIT_V1_REPORT.md`

Predecessor final gate confirmed:

`GULIERP_MDM_UI_AND_COMMON_FIELD_AUDIT_V1_VERIFIED`

Consumed predecessor facts:

| Fact | Value |
|---|---|
| Audited objects/surfaces | 14 |
| P0/P1/P2/P3 | 4 / 9 / 10 / 4 |
| BusinessPartner top issues | text-only address, no effective country data, manual Code, no Mnemonic/Alias/SearchKeywords, limited search |
| Other affected objects | Warehouse, Item, ItemCategory, UOM, Location, Employee |
| Code rule state | format validation exists; full auto-code rule/generator missing |
| Region data state | no effective country/region data; `country.json` item_count 0 |

## 2. Repository HEAD

| Command | Observed value |
|---|---|
| `git branch --show-current` | `master` |
| `git rev-parse HEAD` | `139fe1e940258d71b85d328d88b4e1358c0f7b1e` |

## 3. Initial Git Status

Initial worktree already contained WIP unrelated to this goal:

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

This goal did not clean, revert, stage or modify those files.

## 4. Documents Reviewed

| Document | Usage |
|---|---|
| `docs/verification/GULIERP_MDM_UI_AND_COMMON_FIELD_AUDIT_V1_REPORT.md` | Predecessor audit and object inventory. |
| `docs/business/GULIERP_CODE_RULE_STANDARD_V1.md` | Existing identifier classes, code format, old mutability baseline and Document Number separation. |
| `data/bootstrap/reference/system/country.json` | Confirmed no effective country data and external update classification. |

## 5. Code Evidence Reviewed

| Area | Evidence |
|---|---|
| MDM entities | `BusinessPartner`, `Item`, `ItemCategory`, `Uom`, `Warehouse`, `Location`, `DictionaryType`, `DictionaryItem`, `NumberingRule`. |
| MDM enums | `MasterDataStatus`, UOM, ItemNature, BusinessPartnerRole, WarehouseType, LocationType, NumberingRuleResetMode. |
| MDM configurations | Unique scopes and field lengths in each `*Configuration.cs`. |
| MDM DTOs | Create/update/read DTO shapes in `MdmDtos.cs`; Code create-only for current MDM rows. |
| MDM services | Keyword coverage, duplicate checks, concurrency checks and ModifiedAt/ConcurrencyVersion updates. |
| Identity entities | `Company`, `OrganizationUnit`, `Employee`, `Plant`. |
| Identity configuration | Unique code scopes and concurrency tokens in `IdentityDbContext.cs`. |
| Identity enums | Company, Plant, Organization and Employee lifecycle differences. |
| Sales usage | Active-only selector/reference behavior and historical snapshot relevance. |

## 6. Audit Findings Consumed

The contract consumed all required predecessor findings:

| Finding | Contract outcome |
|---|---|
| BusinessPartner address is text-only | BusinessPartner address marked `CURRENT_TEXT_MODEL_NOT_FINAL`; structured address required downstream. |
| Warehouse repeats text-only address | Warehouse receives same structured-address downstream requirement. |
| CountryCode lacks effective data | Country/region work deferred to address standard, not solved here. |
| Full Master Data Code Rule missing | This contract freezes Code semantics only and names the code-rule goal next. |
| MnemonicCode missing | Recommended for BusinessPartner, Item and Employee; optional elsewhere. |
| Alias/SearchKeywords missing | Not universal and not persisted in V1 common contract. |
| Search coverage narrow | Search Field Matrix freezes default fields for BusinessPartner and Item. |
| No MasterDataBaseEntity | Contract allows convention/interface/value object/explicit fields; no universal base class required. |

## 7. Contract Decisions

| Decision | Frozen result |
|---|---|
| Actual master data objects | 9 |
| Actual reference data objects | 4 |
| Configuration surfaces | 1 |
| Required common fields | Id, Code or EmployeeNo, Name, Status, Audit, Concurrency; TenantId by scope. |
| CompanyId | Conditional only. |
| OrganizationId | Conditional only. |
| Code format | Canonical uppercase, 40 max, no whitespace, no Chinese characters, normalized in Code itself. |
| Code mutability | Entity-specific: strict immutable for Item/UOM/Company/system dictionary type; controlled change for BP/Warehouse/Location/Employee/Plant/OU and tenant dictionary items. |
| ShortName | Required for BusinessPartner; optional for Item/Warehouse/Company/OU/Plant; not applicable for UOM/ItemCategory/Location/Employee. |
| MnemonicCode | Recommended for BusinessPartner, Item, Employee; optional for Warehouse/Location/Company/OU/Plant; not applicable for UOM/ItemCategory. |
| Alias | No universal persisted V1 field. |
| SearchKeywords | No universal persisted V1 field. |
| Status | Active/Inactive for MDM; preserve Identity-owned richer lifecycles. |
| Audit | `CreatedAt/CreatedBy/ModifiedAt/ModifiedBy`; server generated; read DTOs expose time fields. |
| Concurrency | `ConcurrencyVersion` required. |
| Delete | Deactivate/close/leave/decommission instead of common soft-delete columns. |

## 8. Entity Matrices Completed

The contract contains:

| Matrix | Status |
|---|---|
| Common Field Matrix | completed for all 14 objects/surfaces |
| Scope Matrix | completed for all 14 objects/surfaces |
| Search Field Matrix | completed for BusinessPartner, Item, Warehouse, Location, Employee, ItemCategory and UOM |
| Code Mutability Matrix | completed for all code-bearing master/reference/configuration rows |
| Delete policy matrix | completed for all 14 objects/surfaces |
| Compatibility impact matrix | completed for BusinessPartner, Item, ItemCategory, UOM, Warehouse, Location and Identity-adjacent objects |

## 9. Compatibility Analysis

| Entity | DB impact | Breaking-change assessment |
|---|---|---|
| BusinessPartner | Additive fields and later address/reference tables; search indexes likely | No breaking change if nullable/additive and current address fields stay during transition. |
| Item | Additive MnemonicCode and optional ShortName; search index likely | No breaking change if DTO compatibility is preserved. |
| ItemCategory | No common-field DB change required | No breaking change. |
| UOM | No ShortName/Mnemonic DB change; possible Symbol search index | No breaking change. |
| Warehouse | Additive address-related fields later; optional mnemonic/short name only if approved | No breaking change if current fields stay during transition. |
| Location | Future uniqueness index may change to warehouse-aware | Potentially breaking if duplicates exist; requires planned migration check. |
| Employee | Additive MnemonicCode in Identity-owned goal | No breaking change if nullable. |
| Plant | Address standard may require new reference fields later | No breaking change if additive. |

DB migrations are required for later implementation, but none were created by this goal.

## 10. No-Production-Code Verification

No production code files were modified by this goal.

Allowed new files only:

```text
docs/business/GULIERP_MASTER_DATA_COMMON_FIELD_CONTRACT_V1.md
docs/verification/GULIERP_MASTER_DATA_COMMON_FIELD_CONTRACT_V1_REPORT.md
```

## 11. No-Database-Change Verification

No database connection was opened, no SQL was executed, no seed data was changed and no external country/region data was imported.

## 12. No-Migration Verification

No migration file was added, edited or executed.

## 13. Implementation Wave

Recommended next implementation sequence:

| Wave | Purpose |
|---:|---|
| 1 | Common field and API contract preparation. |
| 2 | Master Data Code Rule standard. |
| 3 | Address and Region standard. |
| 4 | BusinessPartner remediation. |
| 5 | Item / ItemCategory / UOM remediation. |
| 6 | Warehouse / Location remediation. |
| 7 | Selectors / Search / UX. |
| 8 | Regression and runtime acceptance. |

The next goal is:

`GULIERP_MASTER_DATA_CODE_RULE_STANDARD_V1`

This report does not start that goal.

## 14. Closing Verification Results

Closing checks were run after both allowed files were written.

| Check | Result |
|---|---|
| Full `git status --short` | Existing WIP remains; the two files from this goal appear as new untracked files. |
| Target-file status | Only `docs/business/GULIERP_MASTER_DATA_COMMON_FIELD_CONTRACT_V1.md` and `docs/verification/GULIERP_MASTER_DATA_COMMON_FIELD_CONTRACT_V1_REPORT.md` are new for this goal. |
| `git diff --check` | No whitespace errors. Git reported only pre-existing LF/CRLF warnings for dirty files. |
| Sensitive/local-secret scan | No match after removing the literal scan expression from this report. |
| Placeholder-marker scan | No match after removing the literal scan expression from this report. |

No production code, database object, migration, frontend file or test file was modified by this goal.

## 15. Final Gate

Gate checklist:

| Requirement | Result |
|---|---|
| Predecessor audit verified | pass |
| 14 audited objects recovered and classified | pass |
| Master Data / Reference Data separated | pass |
| Common Field Matrix completed | pass |
| Scope Matrix completed | pass |
| Search Field Matrix completed | pass |
| Code Contract frozen | pass |
| Code Mutability Matrix completed | pass |
| ShortName applicability frozen | pass |
| MnemonicCode applicability frozen | pass |
| Alias / SearchKeywords V1 decision frozen | pass |
| Status Contract frozen | pass |
| Audit Contract frozen | pass |
| Delete Contract frozen | pass |
| BusinessPartner Contract frozen | pass |
| Item Contract frozen | pass |
| UOM Contract frozen | pass |
| Warehouse / Location Contract frozen | pass |
| DB impact classified | pass |
| Implementation Wave formed | pass |
| No production code modified | pass |
| No database change | pass |
| No migration | pass |
| No commit | pass |
| No push | pass |
| Core decisions are explicit | pass |

Final status:

`GULIERP_MASTER_DATA_COMMON_FIELD_CONTRACT_V1_VERIFIED`
