# GULIERP_MASTER_DATA_CODE_RULE_STANDARD_V1_REPORT

| Field | Value |
|---|---|
| Goal | `GULIERP_MASTER_DATA_CODE_RULE_STANDARD_V1` |
| Nature | Standard / contract only |
| Repository | `D:\guli\projects\gulierp-next` |
| Branch | `master` |
| HEAD | `139fe1e940258d71b85d328d88b4e1358c0f7b1e` |
| Standard output | `docs/business/GULIERP_MASTER_DATA_CODE_RULE_STANDARD_V1.md` |
| Verification output | `docs/verification/GULIERP_MASTER_DATA_CODE_RULE_STANDARD_V1_REPORT.md` |
| Final gate | `GULIERP_MASTER_DATA_CODE_RULE_STANDARD_V1_VERIFIED` |

## 1. Predecessor Gate

Required predecessor files were read:

| File | Gate / status consumed |
|---|---|
| `docs/business/GULIERP_MASTER_DATA_COMMON_FIELD_CONTRACT_V1.md` | `GULIERP_MASTER_DATA_COMMON_FIELD_CONTRACT_V1_FROZEN` |
| `docs/verification/GULIERP_MASTER_DATA_COMMON_FIELD_CONTRACT_V1_REPORT.md` | `GULIERP_MASTER_DATA_COMMON_FIELD_CONTRACT_V1_VERIFIED` |
| `docs/verification/GULIERP_MDM_UI_AND_COMMON_FIELD_AUDIT_V1_REPORT.md` | `GULIERP_MDM_UI_AND_COMMON_FIELD_AUDIT_V1_VERIFIED` |

## 2. Branch And HEAD

| Check | Value |
|---|---|
| Branch | `master` |
| HEAD | `139fe1e940258d71b85d328d88b4e1358c0f7b1e` |

## 3. Initial Git Status

The worktree already contained existing WIP before this goal. This goal did not modify or clean it.

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
| Predecessor standards | Common field contract and predecessor MDM audit. |
| Existing code standard | `docs/business/GULIERP_CODE_RULE_STANDARD_V1.md`. |
| DocumentKernel API | `IDocumentNumberService.cs`, `DocumentNumberDtos.cs`, `DocumentTypeProfile.cs`. |
| DocumentKernel domain | `DocumentNumberCounter.cs`, `DocumentNumberIdempotency.cs`. |
| DocumentKernel persistence | counter and idempotency configurations. |
| DocumentKernel service | `DocumentNumberService.cs`. |
| Foundation validation | `MasterDataCodeValidator`, `FormatValidator`, `ICodeRuleProvider`, `ReservedNameValidator`, `DocumentNumberSimilarityValidator`. |
| MDM numbering config | `NumberingRule`, `NumberingRuleConfiguration`, `NumberingRuleService`. |
| MDM code consumers | MDM DTOs, services and entity configurations inspected in predecessor/common-field goal. |
| Identity code consumers | Company, OrganizationUnit, Employee and Plant evidence from predecessor/common-field goal. |

## 5. Existing Numbering Infrastructure Evidence

| Search term family | Result |
|---|---|
| `IDocumentNumberService` / `DocumentNumber` | Implemented for transaction documents. |
| `NumberingRule` / `Prefix` / `SequenceLength` / `ResetMode` | Implemented as MDM management surface for document-number rules. |
| `MasterDataCodeValidator` / `CodeRule` | Implemented for validation only. |
| `NextCode` / `PreviewCode` / `GenerateCode` for master data | Missing. |
| `MasterDataCodeRule` / master-data sequence state | Missing. |

Capability classification:

| Capability | Classification |
|---|---|
| Document number generation | `IMPLEMENTED` |
| Document number persistent counter | `IMPLEMENTED` |
| Document number tenant/company scope | `IMPLEMENTED` |
| Document number reset/prefix | `IMPLEMENTED` |
| Document number concurrency | `IMPLEMENTED` with PostgreSQL-specific implementation |
| Document number preview | `MISSING` |
| Master data manual validation | `IMPLEMENTED` |
| Master data auto code generation | `MISSING` |
| Master data sequence persistence | `MISSING` |
| Master data preview | `MISSING` |

## 6. Document Number Reuse Decision

Final decision:

`IDocumentNumberService` is not directly reusable for Master Data Code.

Reuse boundary:

| Part | Decision |
|---|---|
| Service API | not reusable |
| Document counter table | not reusable |
| Document idempotency table | not reusable |
| Static document profile catalog | not reusable |
| Atomic-counter semantics | reusable as design concept |
| Scope resolution discipline | reusable as design concept |
| Validation pipeline | reusable with adapter |
| Database-specific upsert code | not a standard-layer dependency |

Reason:

Document numbers are transaction-period numbers with DocumentType and BusinessDate. Master-data codes are long-lived business identifiers with entity-specific mutability, import override and no date reset.

## 7. Matrices Completed

| Matrix | Status |
|---|---|
| Entity Code Strategy Matrix | complete |
| Rule Scope Matrix | complete |
| Code Mutability Matrix | complete through predecessor + this standard |
| API Behavior Matrix | complete |
| Existing Data Compatibility Matrix | complete through standard sections 29 and 30 |

## 8. Core Decisions

| Decision | Result |
|---|---|
| Code modes | `MANUAL`, `AUTO`, `AUTO_EDITABLE`; no separate `AUTO_IF_EMPTY`. |
| BusinessPartner default | `AUTO_EDITABLE`, `BP_000001`. |
| BusinessPartner role sequence | Shared BP sequence, not customer/supplier split. |
| Item default | `AUTO_EDITABLE`, `ITEM_000001`; immutable after create. |
| Warehouse default | `AUTO_EDITABLE`, `WH_001`. |
| Location default | `AUTO_EDITABLE`, `LOC_0001`. |
| Location uniqueness | Warehouse-wide: `(TenantId, CompanyId, WarehouseId, Code)`. |
| Employee default | `AUTO_EDITABLE`, `EMP_000001`. |
| Reset policy | `NONE` only. |
| Gap policy | `GAP_ALLOWED`. |
| Preview semantics | Non-reserving prediction by default; allocate at save. |
| Atomic generation | Required. |
| Transaction failure consumption | If sequence advanced, do not reuse number. |
| Code normalization | Store canonical uppercase; case-insensitive semantics. |
| Existing data | Preserve all existing codes. |
| Import override | Allowed for admin import/migration/integration with permission. |
| Persistence recommendation | Two tables: rule config and sequence state. |
| Rule inactive behavior | AUTO/AUTO_EDITABLE fail, no silent manual fallback. |
| Controlled change | Dedicated command, permission, uniqueness, audit, no snapshot rewrite. |

## 9. Compatibility Analysis

| Area | Impact |
|---|---|
| Database | Requires new code-rule and sequence-state tables in a later implementation goal. |
| Domain | New rule/sequence entities or equivalents needed later. |
| Application | New MasterDataCodeService or equivalent needed later. |
| API | Create Code should become nullable where mode supports generation; one DTO shape preferred. |
| Frontend | Create forms need mode-aware Code display and no frontend sequence calculation. |
| Bootstrap | Default rules should be seeded for auto-capable entities. |
| Tests | Concurrency, import override, duplicate, rule inactive and gap behavior tests required. |
| Existing data | Existing codes preserved; no mass renumbering. |

Breaking-change assessment:

- No breaking change if implementation is additive, keeps existing Code values and rolls out nullable create Code semantics carefully.
- Location uniqueness migration may be breaking if duplicates or existing constraints are not handled deliberately.

## 10. Database Portability Analysis

The standard avoids binding semantics to PostgreSQL-only features. Provider-specific optimizations may be used in implementation, but the contract only requires atomic sequence semantics, unique normalized Code and bounded retry.

Portability checked against future possibilities: openGauss, GaussDB, KingbaseES, DM, OceanBase and SQL Server.

## 11. No Code Change Verification

No production code was intentionally modified. The only allowed outputs for this goal are:

```text
docs/business/GULIERP_MASTER_DATA_CODE_RULE_STANDARD_V1.md
docs/verification/GULIERP_MASTER_DATA_CODE_RULE_STANDARD_V1_REPORT.md
```

## 12. No DB Change Verification

No database connection was opened, no SQL was executed and no seed/reference data was changed.

## 13. No Migration Verification

No migration was created, edited or executed.

## 14. Closing Verification Results

Closing checks were run after both files were written.

| Check | Result |
|---|---|
| Target-file status | Only the standard and verification report for this goal are new target files. |
| Full git status | Existing WIP remains and is not owned by this goal. |
| Whitespace check | No whitespace errors; Git reported only existing LF/CRLF warnings in dirty files. |
| Sensitive/local-secret scan | No match in this goal's two files. |
| Placeholder-marker scan | No match in this goal's two files. |

## 15. Final Gate

| Requirement | Result |
|---|---|
| Predecessor Contract verified | pass |
| Existing numbering infrastructure verified | pass |
| Document Number and Master Data Code separated | pass |
| Reuse boundary frozen | pass |
| Code Modes frozen | pass |
| Entity Code Strategy frozen | pass |
| BusinessPartner shared-vs-role sequence decided | pass |
| Location uniqueness decided | pass |
| Manual Code Contract frozen | pass |
| Code Normalization frozen | pass |
| Prefix / Sequence frozen | pass |
| Reset Policy frozen | pass |
| Concurrency Contract frozen | pass |
| Transaction / Gap policy frozen | pass |
| Preview semantics frozen | pass |
| Controlled Change frozen | pass |
| Import behavior frozen | pass |
| Persistence recommendation selected | pass |
| Database portability checked | pass |
| Entity Strategy Matrix complete | pass |
| API Behavior Matrix complete | pass |
| Implementation Work Packages complete | pass |
| No production code change | pass |
| No database change | pass |
| No migration | pass |
| No commit | pass |
| No push | pass |

Final status:

`GULIERP_MASTER_DATA_CODE_RULE_STANDARD_V1_VERIFIED`
