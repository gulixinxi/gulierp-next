# GULIERP_FOUNDATION_AND_BUSINESS_CONFIGURATION_MASTER_CHECKLIST

**Goal**: `BASE-000 — ERP Common Configuration Inventory & Gap Discovery`
**Extraction Time**: 2026-08-20T11:46:44+08:00
**Session**: mvs_11a243eed8e544d6b19087711a392283
**Policy**: READ-ONLY Discovery. No production code, no DDL/DML, no MDM-000D files modified.
**Status**: PARTIAL — 3 PG DBs blocked by PGPASSWORD (network OK, no auth)

---

## 1. Source Inventory Summary

| Source | Status | Method | Notes |
|---|---|---|---|
| DEV (旧 ERP) | `EXTRACTED_VIA_JSON_DUMP` | 22 reverse-engineering JSON + 79 JU_ samples + 12 biz samples | Primary business knowledge source |
| ONLYIT (独立) | `NOT_AVAILABLE_AS_INDEPENDENT_SOURCE` | N/A | DEV is itself Onlyit-derived; honest disclosure per `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md:6` |
| gulierp_adminnet_poc | `NOT_ACCESSED_PASSWORD_REQUIRED` | TCP connect test only | Network reachable (192.168.2.228:5432) but no PGPASSWORD in agent session |
| gulierp_g2_001 | `NOT_ACCESSED_PASSWORD_REQUIRED` | TCP connect test only | Same as above |
| gulierp_g2_003_test | `PARTIALLY_ACCESSED_VIA_MIGRATION_FILES` | Read `Migrations/*.cs` files | 14 identity tables extracted (no live data) |
| VOL.NET / VOL.PRO | `PATTERN_REFERENCE_ONLY` | `docs/research/vol-pro/**` | Dictionary/Lookup/DataSource pattern only; no DB |

**Read evidence**:
- `D:\guli\gulierp\docs\reverse-engineering\dev-meta\` — 22 JSON + samples
- `D:\guli\projects\gulierp-next\modules\identity\GuliERP.Identity.Infrastructure\Migrations\` — 14 identity tables
- `D:\guli\projects\gulierp-next\modules\foundation\GuliERP.Foundation\Migrations\` — `foundation` schema only, 0 tables

**Honest gaps** (not reachable in this session):
- Live data in 3 PostgreSQL DBs (need Operator-typed PGPASSWORD, per G2-001 design)
- ONLYIT independent source (would require VPN+creds to original Onlyit system)

---

## 2. Discovery Matrix Statistics

**Total findings**: 149 (machine-curated, see `FOUNDATION_AND_CONFIGURATION_DISCOVERY_MATRIX.json`)

### By category
| Category | Count |
|---|---|
| REFERENCE_DATA | 49 |
| FOUNDATION | 41 |
| SEMANTIC_DATA_TYPE | 16 |
| MASTER_DATA_CONVENTION | 11 |
| STATUS | 10 |
| PRODUCTIVITY_METADATA | 7 |
| NUMBERING | 6 |
| CODING | 4 |
| TAX | 4 |
| RULE | 1 |

### By decision
| Decision | Count | Meaning |
|---|---|---|
| NEW_BUILD | 91 | No evidence in any source — greenfield |
| REUSE_PATTERN | 24 | DEV/VOL pattern is good — adopt shape, modernize impl |
| DEFER | 16 | Out of V1 scope; document for V2+ |
| PROPOSED | 9 | Not yet verified — need external standard review |
| ADAPT | 5 | Mostly usable — change a few details |
| REUSE_DATA | 4 | DEV exact value is correct — copy directly (e.g. currency CNY) |

### By priority
| Priority | Count | Meaning |
|---|---|---|
| P0_NOW | 36 | Must be done before any business module starts (cross-cutting rules) |
| P1_BEFORE_MODULE | 71 | Must be done before the specific module that uses it |
| P2_WITH_MODULE | 34 | Build together with the consuming module |
| P3_DEFER | 8 | Out of V1; document for later |

---

## 3. P0 Foundation Candidates (≤15 — must do now)

> Cap: 15 items. Total cost target: not multi-week. See `P0_FOUNDATION_CANDIDATES.json` for full schema.

| # | Title | Cost | Why must come first | Rework if deferred |
|---|---|---|---|---|
| P0-01 | Semantic Data Type & Precision Convention (BASE-001) | M (3d) | All money/qty/price fields need this. Wrong precision → financial audit failure. Wrong rounding → 0.01 lost per sum. | Every SalesOrder/PurchaseOrder/Inventory table needs precision annotation. POC-003 already started. |
| P0-02 | Numbering Convention (BASE-002) | M (3-5d) | SO/PO/GR/GI/Transfer/MO/QC all need auto-numbering. DEV JU_AutoCode is system-global, not tenant-scoped. | NumberSequence per-tenant scope UNC-001. UNIQUE constraint at DB level UNC-004. Cross-tenant data corruption. |
| P0-03 | Parameter Scope Convention (BASE-003) | S (1-2d) | SYSTEM/TENANT/COMPANY/PLANT scopes must be explicit. DEV has no scope concept. | Every config field ambiguous: "default warehouse" — per-tenant? per-plant? per-company? |
| P0-04 | Foundation Cross-Cutting Convention (BASE-004) | L (1-2w) | IAuditWriter (POC-003 has), IInventoryService, ISoftDelete, IPlantScoped. Per POC-001/002/003 contracts. | Once modules start, refactoring cross-cutting interfaces breaks 10+ files per change. |
| P0-05 | UOM Convention with Dimension + Conversion | S (1-2d) | DEV has 13 UOM but no dimension metadata. | Cross-dimensional bugs (kg item assigned to m UOM) undetected. UNC-007. |
| P0-06 | Currency + ExchangeRate Convention | S (1-2d) | DEV has no exchange rate table; rates hardcoded per transaction. | Multi-currency tenant cannot consolidate; cross-period comparisons impossible. |
| P0-07 | Tax Code + Inclusive/Exclusive Convention | M (3-5d) | DEV no dedicated tax code table; tax-inclusive flag missing. | Tax-inclusive vs exclusive calculations diverge. 1.13 multiplications need scale >= 4. |
| P0-08 | InventoryStatus Convention | S (1-2d) | 5 separate decimal fields: OnHand/Reserved/Available/PendingInspection/Blocked. | Cannot block stock for QC; cannot show reserved vs available. UNC-008. |
| P0-09 | BP Code Format + Address/Contact Convention | S (1-2d) | DEV pure numeric (20001, 20002) — no type prefix. | Same number space for Customer/Supplier — type invisible in code. UNC-009. |
| P0-10 | Item Master Convention (Type/Category/BaseUom) | S (1-2d) | Item.Type enum (Stockable/Non-Stockable/Service/Phantom) + Category tree. | DEV has flat "商品属性" 4 items; no category tree. |
| P0-11 | Document Status State Machine Library | M (3-5d) | DEV: 3 separate status fields (ReportStatus + LockStatus + WorkflowStatus) — meaningless combinations. UNC-013. | Untestable state combinations. Reports show inconsistent data. |
| P0-12 | Field-Level Comment Obligation | XS (0.5d) | DEV: 0/151 JU_ tables have ms_description. Architecture rule, not just style. | Schema documentation rots. New dev onboards slower. UNC-003. |
| P0-13 | Concurrency Version Pattern Enforcement | XS (0.5d) | POC-003 uses `ConcurrencyVersion` (incremented on every state change). Per `src\GuliERP.Modules.Sales\SalesOrderService.cs`. | Without enforcement, optimistic-concurrency check fails. POC-003 already has it; lock in. |
| P0-14 | AggregateState Status (Replace 3-field status anti-pattern) | S (1-2d) | DEV: 3 status fields on one entity. POC-003 uses DocumentStatus enum. | Legacy 3-field pattern creeps into new modules. |
| P0-15 | IDiscoverableAndTruncatable Configuration (Preflight) | XS (0.5d) | Per G2-001/002-001 plan: every test DB has truncate-on-startup; production NEVER. | Test data bleeds into prod accidentally. |

**Total P0 cost**: M+M+S+L+S+S+M+S+S+S+M+XS+XS+S+XS = ~13-18 working days (with L-sized P0-04 the bottleneck; can be split into 1-2 sub-PRs).

---

## 4. User Not Yet Considered Findings (25 items)

> These are the "横切基础规则" the user may not have explicitly considered. See `USER_NOT_YET_CONSIDERED_FINDINGS.json` for full text.

### HIGH (act now) — 6 items
| ID | Category | Topic | Why it bites |
|---|---|---|---|
| UNC-001 | NUMBERING | NumberSequence per-tenant scope | Two tenants → SO/PO collision → production data corruption |
| UNC-002 | AUDIT | OperationLog vs DesignLog split | Lost traceability on dictionary/numbering/tax changes |
| UNC-003 | CONVENTION | pg_description on EVERY column | Schema docs rot; onboarding slower |
| UNC-004 | CONVENTION | UNIQUE constraint on number fields at DB level | Race condition: duplicate SO numbers possible |
| UNC-005 | ROUNDING | Rounding mode per semantic type | Currency cross-aggregation loses 0.01 per sum |
| UNC-006 | INVENTORY | InventoryPostingEngine is SINGLE entry point | Inventory balance ≠ sum of transactions |

### MEDIUM (before module) — 8 items
UNC-007 UomDimension required enum · UNC-008 InventoryStatus 5 separate fields · UNC-009 BP Code structured CUST-/SUPP-/ · UNC-010 Lot expiry + FEFO · UNC-011 Plant as cross-cutting scope · UNC-012 Field-level precision annotation attribute · UNC-013 Single AggregateState vs multi-status fields · UNC-014 Item code structured category-prefix-serial

### LOW (consider) — 11 items
UNC-015 Color/Material/Size as attribute not UOM · UNC-016 Design Audit vs Operation Audit · UNC-017 Photo/Attachment as entity not image column · UNC-018 Negative stock policy · UNC-019 Public holiday + make-up work day · UNC-019b i18n locale per tenant · UNC-020 Printed form number vs DB ID · UNC-021 SoftDelete entity with restore window · UNC-022 Code+Name+Description separation · UNC-023 Lot/Serial controlled flag per item · UNC-024 CostMethod per item (FIFO/MAC/Standard)

---

## 5. Implementation Queue (10 tasks, 28 days total)

> See `IMPLEMENTATION_QUEUE.json` for full schema. Do not execute these tasks in this session.

Sequence: **BASE-001 → 002 → 003 → 004** (foundation) → 005 (reference) → 006 (master) → 007 (inventory) → 008 (state) → 009 (tests) → 010 (process). Parallel where deps allow.

| ID | Name | Days | Why | Dependency | Gate |
|---|---|---|---|---|---|
| BASE-001 | Semantic Data Type & Precision Convention | 3 | All money/qty/price fields need this. Already seeded in MDM-000D R1. | G2-002 Foundation verified | `BASE_001_SEMANTIC_TYPE_ACCEPTED` |
| BASE-002 | Numbering Convention | 3-5 | SO/PO/GR/GI/Transfer/MO/QC all need auto-numbering with tenant scope + UNIQUE. | BASE-001 | `BASE_002_NUMBERING_VERIFIED` |
| BASE-003 | Parameter Scope Convention | 1-2 | SYSTEM/TENANT/COMPANY/PLANT scope must be explicit. | BASE-001 | `BASE_003_SCOPE_ACCEPTED` |
| BASE-004 | Foundation Cross-Cutting Convention | 5-10 | IAuditWriter (POC-003), IInventoryService, ISoftDelete, IPlantScoped. | BASE-001, BASE-002, BASE-003 | `BASE_004_CROSS_CUTTING_VERIFIED` |
| BASE-005 | UOM + Currency + Tax (Master Reference Data) | 3-5 | Per P0-05/06/07; UOM with dimension, currency with rate, tax with inclusive flag. | BASE-001, BASE-002 | `BASE_005_REFERENCE_VERIFIED` |
| BASE-006 | BusinessPartner + Item + Category (Master Data) | 3-5 | Per P0-09/10; BP code format, item type/category, base UOM. | BASE-005 | `BASE_006_MASTER_VERIFIED` |
| BASE-007 | Warehouse + Location + InventoryStatus | 2-3 | Per P0-08; 5 separate decimal fields, status enum. | BASE-005 | `BASE_007_INVENTORY_STATUS_VERIFIED` |
| BASE-008 | Document State Machine Library | 3-5 | Per P0-11/14; replace 3-field status anti-pattern. POC-003 already uses DocumentStatus. | BASE-001 | `BASE_008_STATE_MACHINE_VERIFIED` |
| BASE-009 | Field-Level Comment Obligation + Architecture Tests | 1-2 | Per P0-12/13; CI fails build if missing. | BASE-001 | `BASE_009_COMMENT_OBLIGATION_VERIFIED` |
| BASE-010 | Discovery Preflight Checklist (Process) | 1-2 | Per P0-15; every Discovery task reads BASE-000 outputs first. | All above | `BASE_010_PREFLIGHT_VERIFIED` |

**Total estimated**: 28 working days (sequential); ~16-18 with parallelization after BASE-004.

---

## 6. Top Cross-Cutting Rules (the "if not done now, will cause rework" list)

These are the must-execute-now items distilled from P0 + HIGH user-not-yet-considered. They are NOT a full backlog — they are the ones that, if shipped wrong, force >1 week of refactor per module.

1. **Precision & Rounding** (P0-01, UNC-005): every money/qty field. Foundation contract.
2. **Numbering with tenant scope + UNIQUE** (P0-02, UNC-001, UNC-004): cross-tenant data integrity.
3. **UOM with dimension** (P0-05, UNC-007): prevents kg/m cross-dim bugs.
4. **InventoryPostingEngine is SINGLE entry** (UNC-006): every other module calls it, never direct write.
5. **pg_description on every column** (P0-12, UNC-003): architecture rule, CI-enforced.
6. **Field-level precision attribute** (UNC-012): `[DecimalPrecision(P, S)]` on every decimal.
7. **AggregateState over multi-field status** (P0-11, P0-14, UNC-013): state machine testable.
8. **Plant as cross-cutting scope** (UNC-011): `IPlantScoped` marker + tenant/company/plant.

---

## 7. Foundation Trap Avoidance

> Per the goal: "扫描结果可能很多,禁止结论:这些全部要先做完"

The 36 P0_NOW findings in the matrix include items that should be done **before a specific module starts**, not before all development. The actual cross-module-blocking set is the 8 items in Section 6 above. Everything else is a "this module needs this decision made" item, not a "stop all development until this is done" item.

**MUST_COMPLETE_BEFORE_MDM**: P0-01 (semantic type — MDM-000 dictionary depends on it), P0-05/06/07 (UOM/Currency/Tax — MDM master data).
**MUST_COMPLETE_BEFORE_INVENTORY**: P0-02 (numbering), P0-08 (inventory status), BASE-004 (cross-cutting), UNC-006 (single entry point).
**MUST_COMPLETE_BEFORE_SALES_PURCHASE**: P0-09 (BP code), P0-10 (item master), P0-11 (status machine).
**MUST_COMPLETE_BEFORE_MANUFACTURING**: P0-15 (preflight), BASE-007 (warehouse/location), UNC-018 (negative stock policy).
**IMPLEMENT_WITH_MODULE**: P0-03 (parameter scope can be applied module-by-module), UNC-007/008/010/011/012.
**DEFER**: UNC-015/016/017/019/019b/020/021/022/023/024 — V2+.

---

## 8. Boundary Compliance

**Not modified (per Goal §12):**
- `data/bootstrap/reference/**` (MDM-000D R1 output)
- `tools/discovery/mdm-000d/**`
- `docs/architecture/MDM_000D_*.md`
- `gulierp_adminnet_poc`, `gulierp_g2_001`, `gulierp_g2_003_test` (READ-ONLY)
- `D:\guli\gulierp` (READ-ONLY)
- `G2-005` (not entered)
- `MDM-000` / `MDM-001` (not entered)
- `Inventory` / `Sales` / `Purchase` modules (not entered)

**Not created (per Goal §13):**
- No `ReferenceData` table, no `Uom` table, no `Currency` table, no `SemanticDataType` table
- No migration files
- No DB writes

**Output paths (read-only canonical):**
- `tools/discovery/base-000/_canonical/DATABASE_SOURCE_INVENTORY.json`
- `tools/discovery/base-000/_canonical/FOUNDATION_AND_CONFIGURATION_DISCOVERY_MATRIX.json`
- `tools/discovery/base-000/_canonical/USER_NOT_YET_CONSIDERED_FINDINGS.json`
- `tools/discovery/base-000/_canonical/P0_FOUNDATION_CANDIDATES.json`
- `tools/discovery/base-000/_canonical/IMPLEMENTATION_QUEUE.json`

---

## 9. Final Gate

`BASE_000_PARTIAL_CONFIGURATION_INVENTORY_COMPLETE`

**Reason for PARTIAL (not COMPLETE)**:
- 3 PostgreSQL DBs (gulierp_adminnet_poc, gulierp_g2_001, gulierp_g2_003_test) are network-reachable but PGPASSWORD is not available in agent session. Per G2-001 design, PGPASSWORD is Operator-typed via `Read-Host -AsSecureString`, not in any tracked file.
- ONLYIT independent source is not available; DEV is the onlyit-derived system (per `DEV_METADATA_REVERSE_ENGINEERING_REPORT.md:6`).
- VOL: no runtime DB; pattern reference only.

**To upgrade to `BASE_000_ERP_CONFIGURATION_INVENTORY_COMPLETE`**:
1. Operator runs `tests/GuliERP.Smoke.Postgres.Exploration/` (or similar) with PGPASSWORD set via `Read-Host -AsSecureString` in PowerShell.
2. Agent re-runs the scan with live data from 3 PG DBs.
3. P0 candidates are re-validated against live FK/CHECK/UNIQUE constraints.

---

## 10. STOP

This task ends here. **No implementation, no DB writes, no module entry.**
Next step (Operator decision) is to either (a) provide PGPASSWORD to upgrade Gate, or (b) accept PARTIAL and start BASE-001.
