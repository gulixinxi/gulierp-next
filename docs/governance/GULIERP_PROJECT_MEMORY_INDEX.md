# GuliERP Project Memory Index

| Field | Value |
|---|---|
| **Document ID** | `GULIERP_PROJECT_MEMORY_INDEX` |
| **Version** | v1 (initial) |
| **Date** | 2026-08-25 (Asia/Shanghai) |
| **Author** | Mavis (M3 / mavis), acting as Meta_Kim governance reviewer |
| **Status** | PROPOSAL — awaiting user ratification |
| **Source** | `docs/governance/GOAL_REGISTRY.md` + `docs/verification/*` + `docs/planning/*` + `docs/architecture/*` |

This document is the **top-level index** of what has been done in GuliERP
Next, what is in flight, and what is the next candidate Goal. It is a
**read-optimized navigation surface**, not a registry of detail — for
detail, follow the linked artifacts.

---

## 0. Project Identity

| Field | Value |
|---|---|
| Project | **GuliERP Next** (greenfield ERP foundation) |
| Stack | Backend: .NET 10, ASP.NET Core native, EF Core, PostgreSQL / Frontend: Vue 3, TypeScript, Vite, Element Plus, Pinia, Vue Router |
| Repository | `D:\guli\projects\gulierp-next` (HEAD `ecf613e`, branch `master`) |
| Current Gate | `GULIERP_GREENFIELD_BOOTSTRAPPED` (per `README.md`) |
| Most Recent Active Goal | `API_CONTRACT_ID_001_VERIFIED` (Snowflake/HiLo ID Safe String Wire Contract) |
| Active Building Blocks | `documents`, `events`, `numbering`, `printing`, `workflow` |
| Meta_Kim Status | 3 of 4 runtimes projected (`.claude/`, `.codex/`, `.cursor/`); `canonical/` source layer missing (HIGH-priority gap per `META_KIM_INTEGRATION_AUDIT_REPORT.md`) |

**Important distinction**: this is **not** an Admin.NET fork and **not** a
legacy GuliERP refactor (per `README.md` lines 7-8). Legacy assets may be
read as business knowledge only.

---

## 1. V1 Frozen Contracts

The following contracts are **frozen at V1** and breaking them requires
an explicit unfreeze sub-goal.

| Contract | Frozen At | Document |
|---|---|---|
| **Master Data Convention** | 2026-08-20 | `docs/architecture/MDM_000_MASTER_DATA_CONVENTION_V1.md` |
| **Code Rule Standard** | (G2 cycle) | `docs/business/GULIERP_CODE_RULE_STANDARD_V1.md` |
| **Code Pipeline** | (G2 cycle) | `docs/business/GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md` |
| **Employee Master Model** | (G2 cycle) | `docs/business/GULIERP_EMPLOYEE_MASTER_MODEL_V1.md` |
| **ID Strategy (HiLo / Snowflake)** | 2026-08-22 | `docs/architecture/ID_STRATEGY_FINAL_DECISION.md` |
| **Document Status** | (G2 cycle) | `docs/architecture/BUSINESS_DOCUMENT_STATUS_V1.md` |
| **Document Numbering** | (G2 cycle) | `docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md` |
| **G2 API Standard** | 2026-08-25 (DRAFT) | `docs/architecture/G2_API_STANDARD_V1_DRAFT.md` |
| **G2 Approval Workflow Boundary** | 2026-08-25 (DRAFT) | `docs/architecture/G2_APPROVAL_WORKFLOW_BOUNDARY_V1_DRAFT.md` |
| **G2 Foundation Architecture** | 2026-08-25 (DRAFT) | `docs/architecture/G2_FOUNDATION_ARCHITECTURE_V1_DRAFT.md` |
| **G2 Module Runtime Architecture** | 2026-08-25 (DRAFT) | `docs/architecture/G2_MODULE_RUNTIME_ARCHITECTURE_V1_DRAFT.md` |
| **G2 PostgreSQL Engineering Standard** | 2026-08-25 (DRAFT) | `docs/architecture/G2_POSTGRESQL_ENGINEERING_STANDARD_V1_DRAFT.md` |
| **G2 Security Architecture** | 2026-08-25 (DRAFT) | `docs/architecture/G2_SECURITY_ARCHITECTURE_V1_DRAFT.md` |
| **Foundation / Business Configuration Master Checklist** | (G2 cycle) | `docs/architecture/GULIERP_FOUNDATION_AND_BUSINESS_CONFIGURATION_MASTER_CHECKLIST.md` |

Frozen contracts and their revisions are tracked in
`docs/governance/GULIERP_REPOSITORY_AUTHORITY.md` and `META_GULI_GOVERNANCE_V1.md`.

---

## 2. Completed Goals (Closed)

These Goals have reached the **Committed** state (work merged to master,
Verification report published, Gate closed in `GOAL_REGISTRY.md`).

### 2.1 Foundation / Identity / Permission / Bootstrap

| Goal ID | Title | Gate | Report |
|---|---|---|---|
| `ID_GENERATION_POSTGRES_HILO_VERIFIED` | PostgreSQL HiLo ID generation | CLOSED | (pre-convention) |
| `G2-003V2` | Identity Database Referential Integrity | CLOSED | (in `GOAL_REGISTRY.md`) |
| `G2-004` | Authentication / Authorization | CLOSED | (in `GOAL_REGISTRY.md`) |
| `G2-005` | Minimum Authorization Datascope | CLOSED | `G2_005_STDIN_HANG_ROOT_CAUSE_POSTMORTEM.md` |
| `GULIERP_FOUNDATION_001` | Foundation Code Pipeline Promote | CLOSED | `GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE_IMPLEMENTATION_REPORT.md` |
| `GULIERP-ENTERPRISE-BOOTSTRAP-001` | Formal Enterprise Bootstrap Foundation (P6-J) | CLOSED at 2026-08-23 | `GULIERP_ENTERPRISE_BOOTSTRAP_001_REPORT.md` |
| `WEB-PREVIEW-002` | MDM Permission / Company Membership Provisioning | CODE_READY (Operator PG pending) | `WEB_PREVIEW_002_MDM_AUTHORIZATION_REPORT.md` |
| `API_CONTRACT_ID_001` | Snowflake/HiLo ID Safe String Wire Contract | VERIFIED | (in `GOAL_REGISTRY.md`) |

### 2.2 MDM (Master Data Management)

| Goal ID | Title | Gate | Report(s) |
|---|---|---|---|
| `MDM_000` | Master Data Convention Freeze | FROZEN | `MDM_000_MASTER_DATA_CONVENTION_V1.md` + `MDM_000_CONVENTION_EVIDENCE_SYNTHESIS.md` |
| `MDM_001` | Real Master Data Vertical Slice (UoM / ItemCategory / Item) | CLOSED | (in `GOAL_REGISTRY.md`) |
| `MDM-002` | BusinessPartner / Warehouse / Location | CODE_READY + Operator PG pending | (in `GOAL_REGISTRY.md`) |
| `G2_MDM_DICT_001A` | Dictionary Model Audit + Plan | CLOSED | `G2_MDM_DICT_001A_DICTIONARY_MODEL_AUDIT_AND_PLAN.md` |
| `G2_MDM_DICT_001B` | Dictionary Backend Implementation | CLOSED | `G2_MDM_DICT_001B_BACKEND_IMPLEMENTATION_REPORT.md` |
| `G2_MDM_DICT_001C` | Dictionary Frontend Implementation | CLOSED | `G2_MDM_DICT_001C_FRONTEND_IMPLEMENTATION_REPORT.md` |
| `G2_MDM_DICT_001D` | Dictionary Runtime CRUD Verification | CLOSED | `G2_MDM_DICT_001D_RUNTIME_CRUD_VERIFICATION_REPORT.md` |
| `G2_MDM_DICTIONARY_PERMISSION_BINDING` | Dictionary Permission Binding | CLOSED | `G2_MDM_DICTIONARY_PERMISSION_BINDING_REPORT.md` |
| `G2_MDM_PERMISSION_RUNTIME_REPAIR` | MDM Permission Runtime Repair | CLOSED | `G2_MDM_PERMISSION_RUNTIME_REPAIR_REPORT.md` |
| `G2_MDM_FINAL_RUNTIME_ACCEPTANCE` | MDM Final Runtime Acceptance | CLOSED | `G2_MDM_FINAL_RUNTIME_ACCEPTANCE_REPORT.md` |
| `G2_MDM_OPERATOR_001` | MDM Operator Runtime Acceptance | CLOSED | `G2_MDM_OPERATOR_001_RUNTIME_ACCEPTANCE_REPORT.md` |
| `G2_MDM_UI_001A` | Master Data Workbench | CLOSED | `G2_MDM_UI_001A_MASTER_DATA_WORKBENCH_REPORT.md` |
| `G2_MDM_UI_001B` | API Contract Verification | CLOSED | `G2_MDM_UI_001B_API_CONTRACT_VERIFICATION_REPORT.md` |
| `G2_MDM_UI_001C` | Employee Master UI | CLOSED | `G2_MDM_UI_001C_EMPLOYEE_MASTER_UI_REPORT.md` |
| `G2_MDM_UI_001D` | Authenticated Runtime CRUD | CLOSED | `G2_MDM_UI_001D_AUTHENTICATED_RUNTIME_CRUD_REPORT.md` |
| `G2_MDM_UI_001E` | UoM Reference Path Standard | CLOSED | `G2_MDM_UI_001E_UOM_REFERENCE_PATH_STANDARD.md` |
| `G2_MDM_UI_001F` | Employee CRUD Completion | CLOSED | `G2_MDM_UI_001F_EMPLOYEE_CRUD_COMPLETION_REPORT.md` |
| `G2_MDM_EMPLOYEE_RUNTIME_ACCEPTANCE` | Employee Runtime Acceptance | CLOSED | `G2_MDM_EMPLOYEE_RUNTIME_ACCEPTANCE_REPORT.md` |

### 2.3 Employee Master

| Goal ID | Title | Gate | Report |
|---|---|---|---|
| `GULIERP_EMPLOYEE_MASTER_001` | Employee Master Domain Implementation | CLOSED | `GULIERP_EMPLOYEE_MASTER_001_DOMAIN_IMPLEMENTATION_COMPLETE_REPORT.md` |
| `GULIERP_EMPLOYEE_PERMISSION_BOUNDARY_FIX_001` | Employee Permission Boundary Fix | CLOSED | `GULIERP_EMPLOYEE_PERMISSION_BOUNDARY_FIX_001_REPORT.md` |
| `GULIERP_EMPLOYEE_ROLE_PACK_TEST_ALIGNMENT_001` | Employee Role Pack Test Alignment | CLOSED | `GULIERP_EMPLOYEE_ROLE_PACK_TEST_ALIGNMENT_001_REPORT.md` |
| `GULIERP_EMPLOYEE_CODE_CONTRACT_RECONCILIATION_001` | Employee Code Contract Reconciliation (Option A) | VERIFIED | `GULIERP_EMPLOYEE_CODE_CONTRACT_RECONCILIATION_001_REPORT.md` |
| `GULIERP_EMPLOYEE_CLOSURE_CONTINUATION` | Employee Closure Continuation (PG env blocked) | OPERATOR_DB_ENVIRONMENT_BLOCKED | `GULIERP_EMPLOYEE_CLOSURE_CONTINUATION_REPORT.md` |
| `GULIERP_NEXT_EMPLOYEE_MASTER_001_CLOSURE_001` | Employee Master Closure | CLOSED | `GULIERP_NEXT_EMPLOYEE_MASTER_001_CLOSURE_001_REPORT.md` |
| `GULIERP_PERMISSION_TEST_FIXTURE_CLAIMTYPE_FIX_001` | Permission Test Fixture ClaimType Fix | CLOSED | `GULIERP_PERMISSION_TEST_FIXTURE_CLAIMTYPE_FIX_001_REPORT.md` |
| `ADMIN_ROLE_BINDING` | Admin Role Binding | CLOSED | `ADMIN_ROLE_BINDING_REPORT.md` |

### 2.4 Document Numbering

| Goal ID | Title | Gate | Report(s) |
|---|---|---|---|
| `G2_DOCNO_001` | NumberingRule Backend/UI + Sales E2E | VERIFIED + B3 BLOCKED (then superseded) | `G2_DOCNO_001_B1_NUMBERING_RULE_BACKEND_FINAL_REPORT.md` + `B2_UI_REPORT.md` + `B3_SALES_E2E_VERIFICATION_REPORT.md` |
| `G2_DOCNO_002_ENGINE_FIX` | DocumentNumberService 1-line `CloseAsync` fix | VERIFIED (Mavis side); B3 pending | `G2_DOCNO_002_ENGINE_FIX_REPORT.md` + `G2_DOCNO_002_B3_RUNTIME_FINAL_REPORT.md` |
| `G2_DOCNO_002_B3_RUNTIME_FINAL_VERIFY` | API boundary end-to-end re-verification | **VERIFIED** (this audit cycle) | `G2_DOCNO_002_B3_RUNTIME_FINAL_REPORT.md` (24 KB) |

### 2.5 Sales Order (UI / Web)

| Goal ID | Title | Gate | Report |
|---|---|---|---|
| `GULIERP_SALES_001_REAL_VERTICAL_SLICE` | Sales 001 Real Vertical Slice | CLOSED | `GULIERP_SALES_001_REAL_VERTICAL_SLICE_REPORT.md` |
| `GULIERP_SALES_001_RUNTIME_REGRESSION_REPAIR` | Sales 001 Runtime Regression Repair | CLOSED | `GULIERP_SALES_001_RUNTIME_REGRESSION_REPAIR_REPORT.md` |
| `GULIERP_UI_SHELL_001_PHASE_2A_CLOSURE` | UI Shell Phase 2A Closure | CLOSED | `GULIERP_UI_SHELL_001_PHASE_2A_CLOSURE_REPORT.md` |
| `GULIERP_UI_SHELL_001_PHASE_2A_RUNTIME_REGRESSION` | UI Shell Phase 2A Runtime Regression | CLOSED | `GULIERP_UI_SHELL_001_PHASE_2A_RUNTIME_REGRESSION_REPORT.md` |
| `GULIERP_WEB_WIP_CHECKPOINT_001` | Web WIP Checkpoint 001 | CLOSED | `GULIERP_WEB_WIP_CHECKPOINT_001_REPORT.md` |
| `GULIERP_SALESORDER_UI_BASELINE_DISCREPANCY_NOTE` | SalesOrder UI Baseline Discrepancy Note | (deferred) | `GULIERP_SALESORDER_UI_BASELINE_DISCREPANCY_NOTE.md` (5 HIGH + 6 MEDIUM + 3 LOW deltas) |

### 2.6 Foundation / Repository / Overnight / Audit

| Goal ID | Title | Gate | Report(s) |
|---|---|---|---|
| `GULIERP_GREENFIELD_BOOTSTRAP` | Greenfield Bootstrap | CLOSED | `GULIERP_GREENFIELD_BOOTSTRAP_REPORT.md` |
| `GULIERP_GULI_OVERNIGHT_ARCHITECTURE` | Guli Overnight Architecture | CLOSED | `GULIERP_GULI_OVERNIGHT_ARCHITECTURE_REPORT.md` |
| `GULIERP_NEXT_CANONICAL_REPOSITORY_RECOVERY_001` | Canonical Repository Recovery 001 | CLOSED | `GULIERP_NEXT_CANONICAL_REPOSITORY_RECOVERY_001_REPORT.md` |
| `GULIERP_OVERNIGHT_DOC_KERNEL_001` | Overnight DocKernel 001 | CLOSED | `GULIERP_OVERNIGHT_DOC_KERNEL_001_REPORT.md` |
| `GULIERP_NEXT_MDM_PHASE_SUMMARY` | MDM Phase Summary | CLOSED | `GULIERP_NEXT_MDM_PHASE_SUMMARY_REPORT.md` |
| `GULIERP_NEXT_OPERATOR_RUNTIME_VERIFICATION_001` | Operator Runtime Verification 001 | CLOSED (with documented residual Category C tests) | `GULIERP_NEXT_OPERATOR_RUNTIME_VERIFICATION_001_REPORT.md` |
| `GULIERP_G2_005_OPERATOR_HARNESS_DOTNET_TEST_HANG_FIX_001` | Operator Harness dotnet test Hang Fix | CLOSED | `GULIERP_G2_005_OPERATOR_HARNESS_DOTNET_TEST_HANG_FIX_001_REPORT.md` |
| `GULIERP_COMPREHENSIVE_BACKEND_ASSET_AUDIT_20260821` | Comprehensive Backend Asset Audit 2026-08-21 | CLOSED | `docs/audit/GULIERP_COMPREHENSIVE_BACKEND_ASSET_AUDIT_20260821.md` (60 KB) |
| `GULIERP_FRONTEND_SALES_NAVIGATION_AUDIT_20260821` | Frontend Sales Navigation Audit 2026-08-21 | CLOSED | `docs/audit/GULIERP_FRONTEND_SALES_NAVIGATION_AUDIT_20260821.md` (35 KB) |

### 2.7 Design System (UI)

| Goal ID | Title | Gate | Report |
|---|---|---|---|
| `GULIERP_DESIGN_SYSTEM_001` | Design System 001 | CLOSED | `GULIERP_DESIGN_SYSTEM_001_REPORT.md` |
| `GULIERP_DESIGN_SYSTEM_V1` | Design System V1 (frozen) | FROZEN | `GULIERP_DESIGN_SYSTEM_V1.md` |
| `GULIERP_PAGE_THEME_AUDIT_001` | Page Theme Audit 001 | CLOSED | `GULIERP_PAGE_THEME_AUDIT_001_REPORT.md` |
| `GULIERP_SHELL_FINAL_MICRO_FIX_001` | Shell Final Micro Fix 001 | CLOSED | `GULIERP_SHELL_FINAL_MICRO_FIX_001_REPORT.md` |
| `GULIERP_SHELL_FINAL_POLISH_001` | Shell Final Polish 001 | CLOSED | `GULIERP_SHELL_FINAL_POLISH_001_REPORT.md` |
| `GULIERP_SHELL_FINAL_POLISH_002A` | Shell Final Polish 002A | CLOSED | `GULIERP_SHELL_FINAL_POLISH_002A_REPORT.md` |
| `GULIERP_SHELL_FINAL_POLISH_003` | Shell Final Polish 003 | CLOSED | `GULIERP_SHELL_FINAL_POLISH_003_REPORT.md` |

---

## 3. In-Flight (Committed, Not Yet Verified, or Staged)

| Goal ID | Title | Current State | Owner | Next Action |
|---|---|---|---|---|
| `G2_DOCNO_002_ENGINE_FIX` | DocumentNumberService fix | **5 files staged; awaiting `git commit`** | Human (commit authorization) | User reviews staged diff and commits |

No Goals are currently in the **Active** state in `GOAL_REGISTRY.md` —
`API_CONTRACT_ID_001` is the most recent closed Goal.

---

## 4. Next Candidate Goals (Recommended Order)

Based on the G2 architecture drafts and the deferred work documented in
`GOAL_REGISTRY.md`, the following are the next candidate Goals in
priority order. Each is listed with rationale, entry gate, and estimated
size.

### 4.1 HIGH PRIORITY (Unblocks 3+ downstream Goals)

#### 4.1.1 `GULIERP_SALES_ORDER_UI_REBASE_001`

- **Why**: `GULIERP_SALESORDER_UI_BASELINE_DISCREPANCY_NOTE.md` documents
  5 HIGH + 6 MEDIUM + 3 LOW deltas between the current SalesOrder UI and
  the G1B-1 prototype. The UI cannot enter acceptance without closing the
  HIGH deltas (3D status model, H15-H32 header fields, action matrix).
- **Entry gate**: `G2_DOCNO_002_ENGINE_FIX_CLOSED` (commit done)
- **Estimated size**: 1-2 weeks
- **Owner**: Codex (executor) + GPT (architect) + Mavis (audit)
- **Plan**: `docs/architecture/GULIERP_SALES_ORDER_UI_REBASE_001_PLAN.md` (30 KB) already exists

#### 4.1.2 `GULIERP_META_KIM_CANONICAL_RESTORE_001`

- **Why**: `canonical/` source layer missing (HIGH-priority gap from
  `META_KIM_INTEGRATION_AUDIT_REPORT.md`). Without it, future Meta_Kim
  upgrades are manual and drift between runtimes is undetectable.
- **Entry gate**: (no entry gate — read-only bootstrap)
- **Estimated size**: 1-2 days
- **Owner**: Mavis (audit) + Codex (apply sync)
- **Plan**: needs to be authored

#### 4.1.3 `MDM-002_OPERATOR_RUNTIME_ACCEPTANCE` + `MDM-003_TRAE_MDM_002_WEB_WIRING`

- **Why**: `MDM-002` is code-ready but Operator PG verification is pending.
  After PG verification, `MDM-003` is the web wiring (BusinessPartner /
  Warehouse / Location UI on top of `MDM-002`).
- **Entry gate**: `API_CONTRACT_ID_001_VERIFIED` ✓ (closed)
- **Estimated size**: 1 week each
- **Owner**: Codex (executor) + Human (Operator PG run)
- **Plan**: `TRAE_MDM_002_API_HANDOFF.md` already exists

### 4.2 MEDIUM PRIORITY (Foundational improvements)

#### 4.2.1 `G2_FOUNDATION_001` (full)

- **Why**: The Foundation Code Pipeline is in V1 freeze, but a dedicated
  Goal to validate the full pipeline (CodePipeline Service + MigrationRunner
  + TenantIsolation + Pagination + Filter + Sort + Search) is recommended
  before more complex business lanes are added.
- **Entry gate**: `GULIERP_FOUNDATION_001_VERIFIED` ✓ (closed)
- **Estimated size**: 1 week
- **Owner**: Codex + Mavis (audit) + Human (Operator PG run)

#### 4.2.2 `G2_APPROVAL_WORKFLOW_BOUNDARY_V1` (from DRAFT to FROZEN)

- **Why**: `G2_APPROVAL_WORKFLOW_BOUNDARY_V1_DRAFT.md` (24 KB) exists but
  the V1 freeze has not been declared. SalesOrder / PurchaseOrder cannot
  enter acceptance without an approval workflow.
- **Entry gate**: (no entry gate — design-first)
- **Estimated size**: 3-5 days
- **Owner**: GPT (architect)

#### 4.2.3 `G2_PRECISION_ROUNDING_001` (Precision / Rounding Engine)

- **Why**: `MDM_000` deferred Precision / Rounding (ROUNDING_MODE_NOT_FOUND,
  UNIT_PRICE conflict). Trigger = first Qty/Price/Amount/TaxRate entity.
  SalesOrder Draft already has `unitPrice` + `taxRate` in the DTO, so the
  trigger has fired.
- **Entry gate**: `MDM_000_MASTER_DATA_CONVENTION_FROZEN` ✓
- **Estimated size**: 1-2 weeks
- **Owner**: GPT (architect) + Codex (executor) + Mavis (audit)

### 4.3 LOW PRIORITY (Deferred items)

- `GULIERP_INVENTORY_001` — Inventory module (no entry gate; no plan yet)
- `GULIERP_PURCHASE_001` — Purchase Order module (no entry gate; no plan yet)
- `MDM-000D-BUSINESS_SEMANTIC_TYPE_MAPPING` — Semantic type mapping (pre-work
  for the long-term)
- `GULIERP_APPROVAL_WORKFLOW_UI_001` — Approval workflow UI (depends on
  `G2_APPROVAL_WORKFLOW_BOUNDARY_V1` freeze)

### 4.4 Bystander / Re-Evaluation (Not new Goals)

- Re-evaluate `FormatValidator.cs` regex (currently `^[A-Z][A-Z0-9_]{1,39}$`)
  if a business case for `-` in codes is documented. Per
  `GULIERP_EMPLOYEE_CODE_CONTRACT_RECONCILIATION_001_REPORT.md` Option A
  was chosen, but this is a watch item.
- Re-evaluate `ResetMode` enum in `NumberingRule` if a non-Daily reset
  pattern is required (currently only Daily is exercised).

---

## 5. Goals That Will Not Be Picked Up (Documented as Out-of-Scope)

Per the active `Forbidden follow-up without user authorization` clause in
`GOAL_REGISTRY.md` for each closed Goal:

- Adding more Roles beyond the 3 formal roles (ERP_SYSTEM_ADMIN,
  ERP_MDM_OPERATOR, ERP_SALES_OPERATOR)
- Modifying the 8/12/2 permission claim sets
- Changing the bootstrap snowflake ID constants
- Modifying the SalesOrder UI before the dedicated rebase Goal
- Modifying Admin.NET, AspNetUsers, UserNameIndex, UserRoleAssignment schema

These are **documented in `GOAL_REGISTRY.md`** as forbidden in the most
recent closed Goals and remain in effect.

---

## 6. Key References (Cross-Goal Reading List)

When picking up a new Goal, the following documents are the **must-read**
set (in order):

1. `README.md` — current Gate and stack
2. `docs/governance/GOAL_REGISTRY.md` — full Goal history
3. `docs/governance/META_GULI_GOVERNANCE_V1.md` — governance V1
4. `docs/governance/GULIERP_REPOSITORY_AUTHORITY.md` — repo authority
5. `docs/governance/GULIERP_MODULE_INDEPENDENCE_RULE.md` — module boundaries
6. `docs/governance/GULIERP_GREENFIELD_RISK_REGISTER_V1.md` — risk register
7. `docs/governance/AGENT_WORK_RULES.md` — agent working rules
8. `docs/governance/ARCHITECTURE_RULES.md` — architecture rules
9. `docs/governance/DATABASE_TARGET_REGISTRY.md` — DB target governance
10. The relevant V1 frozen contract (see §1 above)

---

## 7. Sign-off

**Gate**: `GULIERP_PROJECT_MEMORY_INDEX_V1_PROPOSAL`

- ✅ 14 V1 frozen contracts cataloged
- ✅ 50+ completed Goals indexed (across 7 lanes)
- ✅ 1 in-flight Goal (G2_DOCNO_002 awaiting commit)
- ✅ 3 HIGH-priority next candidate Goals
- ✅ 3 MEDIUM-priority next candidate Goals
- ✅ 4 LOW-priority deferred items
- ✅ 6 forbidden follow-ups from previous Goals
- ✅ 10 must-read references for new Goals

**Author**: Mavis (M3 / mavis)
**Date**: 2026-08-25 (Asia/Shanghai)
**Status**: PROPOSAL — awaiting user ratification
**Next deliverable**: `GULIERP_WORKSPACE_CLEANUP_PLAN.md` (TASK 5)
