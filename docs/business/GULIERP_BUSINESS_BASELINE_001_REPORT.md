# GULIERP_BUSINESS_BASELINE_001 — REPORT

> Goal: pause code development and consolidate GuliERP's
> business baseline. Six documents ship: an audit of the
> existing assets, a reference-project analysis, the V1
> master-data model, the V1 code rule standard, the V1.5 →
> V3 development roadmap, and this report. No code, no entity,
> no migration, no API, no business Vue page changes.
> Authority: `docs/governance/META_GULI_GOVERNANCE_V1.md` +
> `docs/governance/GULIERP_MODULE_INDEPENDENCE_RULE.md` +
> `docs/governance/GOAL_REGISTRY.md`.

Date: 2026-08-23
Status: **BUSINESS_BASELINE_001_COMPLETE**

---

## 1. Documents shipped (6)

| # | File                                                                                  | Size (bytes) | Purpose                                                                  |
|---|---------------------------------------------------------------------------------------|--------------|--------------------------------------------------------------------------|
| 1 | `docs/business/GULIERP_EXISTING_BUSINESS_ASSET_AUDIT.md`                              | 22,903       | Single source of truth: what GuliERP already has on disk                |
| 2 | `docs/business/GULIERP_REFERENCE_PROJECT_ANALYSIS.md`                                 | 19,483       | What to absorb / what to reject from scm.net, Fiori, Chinese ERP        |
| 3 | `docs/business/GULIERP_MASTER_DATA_MODEL_V1.md`                                       | 22,172       | V1 frozen vocabulary: Org, Counterparty, Material, Storage, People       |
| 4 | `docs/business/GULIERP_CODE_RULE_STANDARD_V1.md`                                     | 18,331       | V1 frozen identifier rules: Technical ID, Master Data Code, Document No |
| 5 | `docs/business/GULIERP_BUSINESS_ROADMAP_001.md`                                       | 23,537       | V1.5 / V2.0 / V2.5 / V3.0 work-item breakdown                              |
| 6 | `docs/business/GULIERP_BUSINESS_BASELINE_001_REPORT.md` (this file)                    | (this)       | Closure report                                                           |

**Total**: 6 documents, ~107 KB of frozen business spec.

All 6 are written to `docs/business/` (a new directory) so they
live next to `docs/architecture/`, `docs/product/specs/`,
`docs/governance/`, and `docs/design/`.

---

## 2. Document 1 — Existing Business Asset Audit

Inventory of the live state at HEAD = `f376411`:

- **Module inventory** (Foundation, Identity, MDM, DocumentKernel,
  Sales, Purchase / Inventory / Production / Quality as skeleton).
- **Master data entities** (9 in MDM: Uom / ItemCategory / Item /
  BusinessPartner / Warehouse / Location; 10 in Identity:
  Tenant / Company / Plant / OrganizationUnit / GuliErpUser /
  GuliErpRole / UserRoleAssignment /
  UserOrganizationMembership / UserCompanyMembership / Employee).
- **Business enums** (every V1 enum with FROZEN values + DEFERRED
  values).
- **Common entity fields** (Id / Code / Name / Status / TenantId /
  CompanyId / Description / CreatedAt / CreatedBy / ModifiedAt /
  ModifiedBy / ConcurrencyVersion, per `MDM-000 frozen §3`).
- **ID strategy** (long / PostgreSQL bigint / HiLo, single shared
  sequence `gulierp_hilo_sequence`).
- **Permission / authorization** (3 dimensions: role, scope,
  boundary, per G2-005).
- **Document / numbering model** (`{PREFIX}-{YYYYMMDD}-{SEQ}`,
  atomic per scope, idempotency on retry).
- **Shell + design system** (FROZEN at `f376411`).
- **API surface** (7 endpoint files).
- **Test surface** (236/238 PASS at HEAD).
- **Existing spec documents** (the inputs to this baseline).
- **What is NOT yet built** (deferred list).

---

## 3. Document 2 — Reference Project Analysis

Three reference families, three lessons:

- **scm.net-class generic ERP** — modest data model, audit fields,
  operator-typed codes. GuliERP inherits the modesty, discards the
  no-concurrency / no-idempotency limitations.
- **SAP Fiori** — two-level nav, semantic tokens, compact density.
  GuliERP inherits the visual language; rejects the launchpad /
  Elements framework overhead.
- **Yonyou / Kingdee / BIP** — multi-Company books, snapshot
  counterparty, status-not-delete, internal reference number.
  GuliERP inherits the identity model; rejects per-tenant schema
  customization, flexible "spare columns", universal workflow
  engine.

**4 universal patterns** all 3 families agree on:

1. Three independent identity dimensions: Tenant / Company /
   OrgUnit.
2. Master data first, then documents.
3. Document Number ≠ Master Data Code.
4. Status lifecycle, not hard delete.

**3 mistakes** all 3 families made that GuliERP explicitly
rejects:

1. Per-tenant schema customization (breaks upgrade paths).
2. Flexible "spare columns" / 自定义项 (always metastasizes into
   undocumented business logic).
3. Universal workflow engine (becomes its own product).

The absorption list (§10 of the analysis) is the V1 checklist
GuliERP already meets: master data has user-typed Code, document
number format is frozen, idempotency is live, status-not-delete
is enforced, 2-tone sidebar is shipped, compact density is
shipped, snapshot counterparty is on SalesOrder, no per-tenant
customization, no flexible columns, no workflow engine, three
permission dimensions, multi-Company books with DefaultCurrency.

---

## 4. Document 3 — Master Data Model V1 (FROZEN)

The V1 master-data vocabulary:

### 4.1 Org tree (Identity)

- **Tenant** — host-level isolation, 1 per host in V1.
- **Company** — legal entity, 1 Tenant → N Companies, parent-child
  tree (cycles forbidden). Carries `DefaultCurrency` (ISO 4217)
  and `Timezone` (IANA).
- **Plant** — logistics / production site, 1 Company → N Plants
  (V1 carries the entity, Production-module semantics deferred to
  V2+).
- **OrganizationUnit** — HR / team tree, 4 types
  (Branch / Department / Team / Other).

### 4.2 Counterparty (MDM)

- **BusinessPartner** — unified counterparty with
  `BusinessPartnerRole` bit-flag (Customer | Supplier | Both).
  Single set of contact + address columns in V1
  (multi-contact / multi-address is V2+).
- **Customer / Supplier** — V1 views on BusinessPartner (not
  separate tables).
- **Contact** — V1 deferred (single triple on BusinessPartner).

### 4.3 Material (MDM)

- **Item** — material / semi-finished / finished good / service
  (`ItemNature` enum, 4 values FROZEN).
- **ItemCategory** — hierarchical, nullable `ParentId`,
  `Level` and `FullPath` are derived (not persisted).
- **Uom** — system master, no `TenantId`, 6 dimensions
  (Count / Mass / Length / Area / Volume / Time) × 2 kinds
  (Discrete / Si).

### 4.4 Storage (MDM)

- **Warehouse** — tenant + company-scoped, optional `PlantId`
  (V1 unused), `WarehouseType` enum (3 values FROZEN).
- **Location** — tenant + company-scoped, FK to Warehouse in
  same Tenant + Company, `LocationType` enum (4 values FROZEN).
  Cascade deactivation on parent Warehouse.

### 4.5 People (Identity)

- **Employee** — lightweight, Company-scoped, `EmployeeNo`
  unique within Company, optional `UserId` link.

### 4.6 Cross-cutting V1 contract

Every MDM + Identity entity carries:

- `Id: long` (HiLo)
- `Code: string` (UPPER_SNAKE, 2..40 chars, scoped uniqueness)
- `Name: string`
- `Status: Active | Inactive` (V1 only; no Draft / Archived /
  Deleted)
- `TenantId: long` (if tenant-owned)
- `CompanyId: long` (if company-scoped)
- `Description: string?`
- `CreatedAt / CreatedBy / ModifiedAt / ModifiedBy`
- `ConcurrencyVersion: int`
- Scope markers: `IMultiTenant` / `ICompanyScoped` (Application
  service applies the `Where(...)` predicates)

### 4.7 What is NOT in V1

- Contact as a separate entity (V2+)
- Bank account / credit limit / price list on BusinessPartner
  (V2+)
- InventoryMethod on Item (Inventory module)
- Sourcing Policy on Item (Production / Procurement)
- UoM conversion ratios (V2+)
- Multi-Address / Multi-Contact (V2+)
- Universal workflow / report designer / mobile app (V3+)

---

## 5. Document 4 — Code Rule Standard V1 (FROZEN)

Three identifier classes, never interchanged:

- **Technical ID** — `long`, HiLo, system-generated.
- **Master Data Code** — `string`, operator-typed, UPPER_SNAKE,
  2..40 chars, scoped uniqueness.
- **Document Number** — `string`, system-allocated,
  `{PREFIX}-{YYYYMMDD}-{SEQ}`.

### 5.1 Master Data Code format (V1)

```
MDR-CODE = UPPER_SNAKE
        = [A-Z][A-Z0-9_]*
        length ∈ [2, 40]
        no leading / trailing underscore
        no double underscore
        no whitespace
```

Validation pipeline (in the Application service):

1. Format check (regex)
2. Reserved-name check
3. Uniqueness check (scoped to Tenant or Company)
4. No-document-number-pattern check (no `YYYYMMDD`, no
   `SO/PO/GR/...` prefix)

### 5.2 Recommended (not enforced) prefixes

| Entity              | Prefix       | Example             |
|---------------------|--------------|---------------------|
| `ItemCategory`      | `CAT-`       | `CAT-RAW-MAT`       |
| `Item`              | `MAT-`       | `MAT-STEEL-A36`     |
| `Uom`               | (no prefix)  | `KGM`, `PCS`, `BEN` |
| `BusinessPartner`   | `C-` / `S-` / `B-` | `C-001`        |
| `Warehouse`         | `WH-`        | `WH-NORTH`          |
| `Location`          | `LOC-`       | `LOC-A-01-03`       |
| `Plant`             | `PLT-`       | `PLT-001`           |
| `OrganizationUnit`  | `OU-`        | `OU-FIN-001`        |
| `Employee`          | `EMP-`       | `EMP-001`           |
| `GuliErpRole`       | (system: `PLATFORM_ADMIN` …; custom: `ROLE_*`) | `ROLE_FINANCE_MANAGER` |

### 5.3 Document Number format (V1 FROZEN)

```
DOC-NO = {PREFIX}-{YYYYMMDD}-{SEQ}
PREFIX ∈ {SO, PO, GR, GI, TR, SI, PI, MO, QI}      (per DocumentType)
PERIOD = YYYYMMDD                                    (UTC, daily)
SEQ    = integer, padded (V1 default: 4 → "0001")
```

Counter scope = `(TenantId, CompanyId, DocumentType, PeriodKey)`.
Allocation is atomic via `DocumentNumberCounter` with
`IdempotencyKey` dedup (no `SELECT MAX(...) + 1`).

### 5.4 Reserved / forbidden

- `SYSTEM`, `SYS`, `RESERVED` — reserved.
- `EMP-SYSTEM`, `WH-DEFAULT`, `LOC-RECEIVING`, `LOC-SHIPPING` —
  reserved for bootstrap.
- `ROLE_PLATFORM_ADMIN`, `ROLE_TENANT_ADMIN`,
  `ROLE_COMPANY_ADMIN`, `ROLE_NORMAL_USER` — system roles,
  immutable.
- `*-YYYYMMDD-*` (any code) — looks like a document number, not
  allowed in code.
- `SO*`, `PO*`, `GR*`, `GI*`, `TR*` (code prefix) — document
  prefixes, not allowed in code.

---

## 6. Document 5 — Business Roadmap

Four horizons:

```
V1.5  (6–8 weeks)    PurchaseOrder + GoodsReceipt + SO↔PO links
V2.0  (8–16 weeks)   Inventory kernel + GoodsIssue + Transfer + QI + reports
V2.5  (16–24 weeks)  Production + Finance + Workflow + Print
V3.0  (24+ weeks)    SRM + PLM + BI + Mobile + Auto-coding + UoM conversions
```

### 6.1 V1.5 priority matrix

| WorkItem                                  | Effort   | Priority | Why                                  |
|-------------------------------------------|----------|----------|--------------------------------------|
| PurchaseOrder vertical slice              | 2 w     | **P0**   | operator demand; mirrors SO         |
| GoodsReceipt                               | 1–2 w   | **P0**   | closes the buy side                 |
| SO↔PO reciprocal links                    | 1 w     | P2       | follow-the-chain                    |
| SalesOrder completion                     | 1–2 w   | P2       | foundation for V2-W5 + V2.5-W4      |
| PAGE_THEME_AUDIT Phase 2 (SalesOrder)     | 1 w     | P2       | consistency with V1 design system    |

### 6.2 V2 priority matrix

| WorkItem                                  | Effort   | Priority | Why                                  |
|-------------------------------------------|----------|----------|--------------------------------------|
| Inventory kernel                          | 2–3 w   | **P0**   | on-hand is the heart of ERP         |
| GoodsIssue                                 | 1–2 w   | **P0**   | the sell side                       |
| StockTransfer                              | 1 w     | P1       | internal logistics                  |
| QualityInspection                          | 1–2 w   | P1       | regulatory / customer demand        |
| SalesOrder rich state                      | 1–2 w   | P1       | enables invoice + partial ship      |
| On-hand + transaction log reports         | 1–2 w   | P1       | the operator's daily tool           |

### 6.3 V2.5 priority matrix

| WorkItem                                  | Effort   | Priority | Why                                  |
|-------------------------------------------|----------|----------|--------------------------------------|
| Production kernel (BOM / Routing)         | 3–4 w   | P1       | makes "we make things" first-class  |
| ManufacturingOrder vertical slice         | 3 w     | P1       | closes the make side                |
| Finance kernel (COA / GL)                 | 3–4 w   | P1       | books of account                    |
| AR / AP                                   | 2–3 w   | P1       | collection + payment                |
| Typed-state-machine workflow               | 3 w     | P1       | approval without a BPM              |
| Print / form engine                        | 2–3 w   | P1       | paper-out is a daily need           |

### 6.4 V3 priority matrix

SRM, PLM, BI, auto-coding engine, multi-Address / multi-Contact,
UoM conversions, mobile, and (only if the typed-state-machine
proves insufficient) a universal workflow engine. Effort and
priority per WorkItem in §7.

### 6.5 Six roadmap principles

1. One capability per design goal.
2. Foundation + MDM first, business module second.
3. V1 freezes are sacred.
4. Manual coding in V1, auto-coding in V2.
5. Status state machines, not workflow engines.
6. Page-by-page audit, not big-bang UI migration.

### 6.6 Cross-cutting themes

- One design goal per WorkItem.
- One change per freeze.
- Page-theme audit on every new module.
- Source-grep regression test on every visual contract.
- No per-tenant customization.
- No flexible "spare columns".
- No generic BPM in V1 / V1.5 / V2.
- One audit + one freeze per WorkItem.

---

## 7. What this baseline gives you

A new agent (or a new operator, or a new operator-evaluator)
can read these 6 documents and answer:

- What does GuliERP already have? → Audit (§1).
- Why does it look the way it does? → Reference analysis (§2).
- What is the V1 vocabulary? → Master data model (§3).
- How do I name a customer / item / warehouse? → Code rule
  standard (§4).
- What comes next? → Roadmap (§5).
- Why these specific decisions and not others? → This report
  (§6).

The 6 documents are the **handoff pack** for the next design
goal (the next WorkItem on the roadmap). They are NOT a
re-implementation of the existing code; they are a
specification of the V1 state that the next WorkItem extends.

---

## 8. What this baseline does NOT do

- **No code, no entity, no migration, no API, no business
  page changes.** The brief is explicit. The 6 documents are
  text only.
- **No new test surface.** No `GULIERP_BUSINESS_BASELINE_001`
  test project. The existing test surface is unchanged.
- **No source-grep regression test changes.** The existing
  `Shell_User_Menu_Exposes_Logout_And_Uses_Auth_SignOut` is
  unchanged; it still PASSES (verified at HEAD `f376411`).
- **No re-implementation of V1 frozen contracts.** The
  documents describe the V1 frozen state; they do not propose
  changes to it. The roadmap is a forward plan, not a
  refactor.

---

## 9. Validation: nothing changes

This baseline is documentation-only. The validation is:

| Check                                            | Result                                       |
|--------------------------------------------------|----------------------------------------------|
| `git diff --stat` against `f376411`             | `docs/business/` is the only changed area    |
| `npm run typecheck` (vue-tsc)                    | N/A — no `.ts` / `.vue` changed             |
| `npm run build` (vite)                          | N/A — no front-end changed                  |
| `dotnet test`                                   | N/A — no C# changed                         |
| `GuliERP.Api.Tests`                             | N/A — unchanged at 32/32                    |
| `GuliERP.Sales.Tests`                           | N/A — unchanged at 9/9                      |

The HEAD is unchanged at `f376411`. The 6 documents are new
untracked files in `docs/business/`.

---

## 10. Operator / agent handoff

A new operator or a new agent can pick up at the next WorkItem:

1. Read this report (§1–§9 above).
2. Read the audit (`GULIERP_EXISTING_BUSINESS_ASSET_AUDIT.md`)
   to know what already exists.
3. Read the master data model
   (`GULIERP_MASTER_DATA_MODEL_V1.md`) to know the V1 vocabulary.
4. Read the code rule standard
   (`GULIERP_CODE_RULE_STANDARD_V1.md`) to know how to name
   things.
5. Pick a WorkItem from the roadmap
   (`GULIERP_BUSINESS_ROADMAP_001.md` §4–§7).
6. Open a new design goal in `docs/governance/GOAL_REGISTRY.md`.
7. Ship.

The handoff pack is complete. The next design goal is the
**Operator's** choice — the brief ends here.

---

## 11. One-line summary

Six documents, 107 KB of frozen business spec, zero code changes.
The V1 platform is shippable today; the V1.5 / V2 / V2.5 / V3
roadmap is explicit; the V1 master-data vocabulary and code
rule standard are frozen. The next WorkItem is the Operator's
choice.
