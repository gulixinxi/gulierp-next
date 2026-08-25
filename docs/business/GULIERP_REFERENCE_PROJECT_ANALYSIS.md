# GULIERP_REFERENCE_PROJECT_ANALYSIS

> Goal: extract the business lessons GuliERP should absorb from
> three reference families — (1) scm.net-class generic ERP projects,
> (2) SAP Fiori design philosophy, (3) Chinese mainstream ERP
> (Yonyou / Kingdee / BIP) business models. This is a **read-only
> analysis** — no code, no entity, no API changes. The output feeds
> into `MASTER_DATA_MODEL_V1`, `CODE_RULE_STANDARD_V1`, and
> `BUSINESS_ROADMAP_001`.
> Authority: `docs/governance/META_GULI_GOVERNANCE_V1.md` +
> `docs/governance/GULIERP_MODULE_INDEPENDENCE_RULE.md`.

Date: 2026-08-23
Status: **REFERENCE_ANALYSIS_COMPLETE**

---

## 1. Three reference families, three lessons

GuliERP does not pick a single reference and copy it. It picks the
right idea from each:

| Reference family            | What GuliERP absorbs                                     | What GuliERP rejects                        |
|-----------------------------|--------------------------------------------------------|--------------------------------------------|
| scm.net-class generic ERP  | Modest data model, audit fields, code conventions      | Web 1.0 era UI, monolithic single-tenant    |
| SAP Fiori                   | Two-level nav, semantic tokens, role-based design system | Heavy launchpad / tile-framework overhead  |
| Yonyou / Kingdee / BIP      | Multi-Company books, real ledger semantics, tax + currency conventions | Per-tenant customization that breaks upgrade paths |

The three families agree on **four universal patterns** that
GuliERP V1 must respect:

1. **Three independent identity dimensions:** Tenant (billing
   isolation), Company (legal entity / books), and Org Unit (HR /
   team tree). A User belongs to a Tenant and is granted access to
   Companies via membership. Org Unit is HR-only — it is NOT the
   chart of accounts. (All three families.)
2. **Master data first, then documents.** No business document
   (SO / PO / GR / etc.) is meaningful without a stable master
   vocabulary. Master data has a **business-readable Code** that
   the operator can type. (scm.net + Yonyou + Kingdee + BIP.)
3. **Document Number ≠ Master Data Code.** The system allocates
   Document Numbers at command time, per scope, with idempotency.
   Master Data Codes are user-managed, never auto-incremented.
   (Yonyou + Kingdee + BIP, also SAP.)
4. **Status lifecycle, not hard delete.** Master data and
   documents are deactivated (`Status = Inactive` /
   `Status = Cancelled / Closed`), never deleted. The audit trail
   is sacred. (All three families.)

The three families also agree on **three mistakes GuliERP must
NOT copy**:

- **Per-tenant schema customization** (early SAP / Yonyou cloud)
  breaks upgrade paths. GuliERP V1 has ONE schema; tenants are
  data-isolated via `TenantId` predicates.
- **Per-document-type "rule tables"** (RDBMS-driven coding /
  numbering rules, a.k.a. "template engine") produce unverifiable
  behavior. GuliERP V1 uses a **C# `DocumentTypeProfile`** — no
  rule table, no DSL. (Yonyou K/3 Cloud and early Kingdee EAS
  both burned engineers on rule-table migrations.)
- **Polymorphic address book** (single Contact / Address with
  many-to-many to Customer + Supplier + Employee + ...) reads
  flexible, performs terribly, and confuses the operator. GuliERP
  V1 separates Counterparty (BusinessPartner) from Employee; the
  former has contact columns, the latter is its own row.

---

## 2. scm.net-class generic ERP — what to absorb

Reference: open-source SCMS / inventory systems, often written in
PHP / Java, with a 2000s-era bootstrap. These systems succeed
because they:

- Model **one Tenant per database / host** (no SaaS-isolation
  gymnastics).
- Carry **audit fields on every row** (`CreatedAt / CreatedBy /
  UpdatedAt / UpdatedBy`).
- Use a **business-readable Code** for every master data row
  (`UPPER_SNAKE`, operator-typed).
- Keep **the document-to-master-data relationship explicit** via
  FK + snapshot fields on the document header.

What scm.net-class gets wrong that GuliERP must avoid:

- **No multi-Company / multi-Org** — operators that grew past
  a single legal entity hit a wall. GuliERP V1 already has Tenant
  + Company + OrgUnit separation.
- **No concurrency control** — last-writer-wins is acceptable
  for a 3-user shop, fatal for a 50-user shop. GuliERP carries
  `ConcurrencyVersion` on every MDM and document entity.
- **No idempotency on document allocation** — a network retry
  produces a duplicate Document Number. GuliERP's
  `DocumentNumberIdempotency` table prevents this.

**Decision:** GuliERP inherits scm.net's *modesty* (small data
model, audit fields, operator-typed codes) and discards its
limitations.

---

## 3. SAP Fiori — what to absorb (and what NOT to)

Reference: SAP Fiori design guidelines, especially the "Fundamental"
and "Fiori for SAP S/4HANA" sets. Fiori is the most mature
enterprise-UX vocabulary in the world; GuliERP's
`GULIERP_DESIGN_SYSTEM_001_ENTERPRISE_FIORI_THEME` is explicitly
named after it.

What GuliERP absorbs:

- **Two-level navigation** (module rail + secondary menu) with
  consistent visual language — rail is the primary module picker,
  secondary menu is the second-level item list. GuliERP V1 implements
  this as a 2-tone sidebar (rail dark, secondary light) per
  SHELL_FINAL_POLISH_001 / 002A / 003.
- **Semantic token system.** A small set of named semantic tokens
  (`--primary-default`, `--bg-container`, `--text-primary`, …)
  drives every chrome surface. GuliERP V1 ships this in
  `apps/web/src/design-system/tokens/color.css` (the
  EnterpriseFioriTheme token set, FROZEN at `cca3705`).
- **Density by default.** Enterprise software is used 6–10 hours
  per day. Whitespace is for hierarchy, not for decoration.
  GuliERP's `--table-row-h: 36px` / `--erp-font-size: 13px` are
  in the Fiori compact band, not the SaaS relaxed band.
- **Quiet chrome, loud content.** The header / sidebar / tab
  strip are low-chroma. The document (table / form / detail) is
  the loudest thing on screen. GuliERP V1 reflects this.
- **Quoted labels.** `<TextField label="客户代码">`, not `<TextField
  placeholder="客户代码">`. Labels are persistent, placeholders are
  hints. (Fiori §3 form patterns.)

What GuliERP rejects from Fiori:

- **The launchpad / tile-framework overhead.** Fiori's launchpad
  is built for 200+ apps; GuliERP is a single-product ERP. A
  tile-framework would over-engineer the operator's daily
  surface. GuliERP's rail + secondary menu IS the launchpad.
- **SAPUI5 / OpenUI5 framework dependency.** GuliERP uses Vue 3 +
  Element Plus, with a CSS-token theme override. We get the
  visual language without the framework lock.
- **Fiori Elements annotations.** GuliERP writes pages by hand;
  we do not adopt Fiori's metadata-driven page generator. The
  V1 page set is small enough to write and review.

**Decision:** GuliERP's visual language is Fiori. GuliERP's
framework and routing are not. The shell is already shipped; the
business pages inherit the same token set.

---

## 4. Yonyou / Kingdee / BIP — what to absorb (Chinese ERP conventions)

Reference: Yonyou U8 / NC Cloud, Kingdee EAS / K3 Cloud, Inspur
BIP / GS Cloud — the products actually used by 80%+ of Chinese
manufacturing SMEs. These systems succeed because they:

- Use **Chinese-friendly identifiers**: a 5–6 character
  Chinese-friendly code is more useful in a 4-tier subsidiary
  group than a 30-character English SKU.
- **Separate the chart of accounts from the Org Unit.** Org
  Unit is HR / team tree; the chart of accounts (科目表) is
  Finance and lives in a different module.
- **Carry both internal Code and external standard mapping.**
  A Customer may have both `Code = C0001` and
  `TaxNumber = 91110000123456789X` (统一社会信用代码). A Material
  may have both `Code = MAT-001` and `Barcode = 6901234567890`.
- **Snapshot the Counterparty on the document.** When a Customer
  is renamed, the historical SalesOrder keeps the old name in
  `CustomerNameSnapshot`. (Yonyou NC Cloud and Kingdee EAS both
  do this; SAP S/4HANA also adopted the pattern.)
- **Carry an "internal reference number"** (内部参考号) on every
  document, distinct from the system-allocated Document Number.
  Operators can type an external reference (PO from customer,
  bank-in-flow number, customs declaration) for cross-checking.

What GuliERP rejects from the Chinese ERP market:

- **Per-tenant schema customization** (Yonyou NC Cloud, Kingdee
  EAS in early versions): when a customer wants a 12-character
  customer code instead of 8, the system builds a per-tenant
  schema and the next upgrade is a 6-month project. GuliERP V1
  has ONE schema; per-tenant variation is expressed via the
  Code format convention (`CODE_RULE_STANDARD_V1`), not via
  schema mutation.
- **"Flexible fields" / "自定义项"** (Yonyou / Kingdee
  "spare columns" for arbitrary attributes): this always
  metastasizes into "spare column #47 is actually a Customer
  Credit Limit, used by 14 reports". GuliERP V1 does NOT carry
  flexible fields. The first time a new attribute is needed,
  it goes on the entity. (V2+ may add a structured
  `entity_extension` table with typed JSON, but never free-form
  string columns.)
- **"Universal workflow engine"** (Yonyou NC Cloud's
  `BPM` / Kingdee EAS's `K-BPM` / BIP's `BIPe3`): these
  become their own product, with their own upgrade cycle and
  their own debugging skills. GuliERP V1 ships NO workflow
  engine. Document status state machines are explicit per
  document type, in code. (V2+ may add a typed-state-machine
  framework, but not a generic BPM.)
- **Per-tenant "currency"** at the Org level. Chinese ERPs often
  let each Company set its own currency, with a `GroupCurrency`
  for consolidation. GuliERP V1 carries `DefaultCurrency` on
  `Company` (DEC-ID-008); consolidation rules are deferred.

**Decision:** GuliERP's identity model + document numbering model
+ master-data code convention + multi-Company books inherit
Chinese ERP conventions. GuliERP's framework + page surface do
not. The first sales / purchase / inventory pages will use
Chinese-label-first copy (per the existing UI), with English
fallback only for internal placeholders.

---

## 5. Universal pattern: the 7-colour status lifecycle

All three reference families converge on a small status vocabulary
that GuliERP V1 already freezes:

- **Master data:** `Active / Inactive`. No `Draft` / `Archived`
  / `Deleted` / `Pending`. Deactivated, not deleted.
- **SalesOrder:** `Draft → Confirmed → Closed` (with `Cancelled`
  as a side-exit from `Draft` or `Confirmed`).
- **Document kernel:** same pattern, per document type. The
  status state machine is explicit C# code; no rule table.

The 7-colour status chip (Element Plus `el-tag type=success / warning
/ info / primary / danger`) is fine for V1; reference families
differ on the exact palette. GuliERP V1 uses the Fiori-aligned
status tokens (`--success-default #107E3E`,
`--warning-default #E9730C`, `--danger-default #BB0000`,
`--primary-default #0A6ED1`).

---

## 6. Universal pattern: the 5-stage document lifecycle

SAP / Yonyou / Kingdee all converge on a 5-stage lifecycle for
business documents:

```
DRAFT → CONFIRMED → IN_PROGRESS → CLOSED → ARCHIVED
                       ↑
                   CANCELLED (side-exit, terminal)
```

GuliERP V1 implements a 3-stage subset:

- SalesOrder: `Draft / Confirmed / Cancelled / Closed`.
- Document Kernel: idempotency-on-allocate, version-on-confirm,
  snapshot-on-cancel.

V2+ (out of scope) extends to the full 5-stage lifecycle when
Shipping + Receival + Invoicing land. The V1 state machines are
intentionally minimal so each V2 stage is a clear addition.

---

## 7. Universal pattern: the 3 permission dimensions

All three families separate:

1. **Functional role** (e.g. "Sales Manager", "Accountant"). A
   user holds one or more functional roles per Company.
2. **Data scope** (e.g. "Sales Manager for Company A only" vs
   "Sales Manager for the whole group"). Implemented as
   `UserRoleAssignment(UserId, RoleId, CompanyId?)` where
   `CompanyId = NULL` means "Tenant-wide".
3. **Security boundary** (Platform Admin, host-level, never scoped
   to a Company). GuliERP exposes this ONLY via
   `ICurrentUser.IsPlatformAdmin`. (DEC-ID-016.)

GuliERP V1 already implements all three. Reference families do
the same, with different table names. The GuliERP table names
(`GuliErpUser / GuliErpRole / UserRoleAssignment /
UserCompanyMembership / UserOrganizationMembership / IsPlatformAdmin
flag`) are the canonical G1A-Freeze names.

---

## 8. Cross-cutting decisions GuliERP inherits from the references

| Decision                                    | Source          | GuliERP V1 implementation                  |
|---------------------------------------------|-----------------|--------------------------------------------|
| Tenant / Company / OrgUnit are 3 dimensions | All 3 families  | Frozen at G2-003A DEC-ID-001/002/005       |
| Master data has user-typed Code             | All 3 families  | `MDM-000 frozen §6`                        |
| Document Number is system-allocated, not Code | All 3 families | `BUSINESS_DOCUMENT_NUMBERING_V1`           |
| Idempotency on document allocation           | SAP + BIP       | `DocumentNumberIdempotency` table          |
| No per-tenant schema customization            | Yonyou NC       | Single schema, `TenantId` predicate       |
| No flexible "spare columns"                  | All 3 families  | Deferred (V2+ may add typed JSON)         |
| No generic workflow engine                   | All 3 families  | Per-doc-type state machines in C#          |
| Two-level nav (rail + secondary)             | Fiori           | `GULIERP_DESIGN_SYSTEM_001`                |
| Compact density (not SaaS relaxed)           | Fiori + Yonyou  | `--table-row-h: 36px`                      |
| Snapshot counterparty on the document       | Yonyou + SAP    | `CustomerCodeSnapshot / CustomerNameSnapshot` on SalesOrder |
| Internal reference number on document        | Yonyou + Kingdee | DEFERRED to V2+ (V1 not yet on entity)    |
| Status lifecycle, not hard delete           | All 3 families  | `Status = Inactive / Cancelled`            |
| Multi-Currency on Company, default at Company | Yonyou + SAP    | `Company.DefaultCurrency` (DEC-ID-008)    |
| Tax Number on Counterparty (not generic)     | All 3 families  | `BusinessPartner.TaxNumber` (free-text)    |

---

## 9. Reference-specific watchpoints (what NOT to copy verbatim)

| Pattern                         | Why GuliERP does not copy                                       |
|---------------------------------|-----------------------------------------------------------------|
| SAP BAPIs + IDocs                | GuliERP V1 uses a single REST API surface; no BAPI/IDoc.         |
| Fiori Elements annotations      | GuliERP pages are hand-written. No metadata-driven page gen.    |
| Fiori launchpad tiles           | GuliERP is single-product. The 2-tone rail is the launchpad.    |
| Yonyou "UAP" / "NC Cloud" portal | No portal framework. GuliERP's shell is the chrome.             |
| Kingdee "s-HR" / "s-HCM"        | HR is a future WorkItem; not part of GuliERP V1.                |
| BIP "BIPe3" BPM                 | No workflow engine in V1. State machines are explicit C#.      |
| scm.net's "global config table" | GuliERP V1 has no global config table. Tokens are in code.      |
| Generic ERP's "report designer" | GuliERP V1 has no report designer. V2+ may add (BI module).     |
| Generic ERP's "approval mobile" | GuliERP V1 has no mobile app. V2+ (not scoped).                  |

---

## 10. Concrete absorption list (GuliERP V1)

The following are the V1 actions that this analysis drives:

1. **Master data has user-typed Code** — already in `MDM-000
   frozen §6`. Freezing the format in `CODE_RULE_STANDARD_V1`.
2. **Document Number format = `{Prefix}-{PeriodKey}-{Sequence}`**
   — already in `BUSINESS_DOCUMENT_NUMBERING_V1`. No change.
3. **Idempotency on document allocation** — already in Document
   Kernel V1. No change.
4. **Status lifecycle, not hard delete** — already in `MDM-000
   frozen §5`. No change.
5. **2-tone sidebar (Fiori)** — already shipped via
   `GULIERP_SHELL_FINAL_POLISH_001/002A/003`. No change.
6. **Compact density** — already in `tokens/sizing.css` (36px row
   height, 13px font). No change.
7. **Snapshot counterparty on document** — already on SalesOrder
   (`CustomerCodeSnapshot / CustomerNameSnapshot`). Apply the
   same pattern to PurchaseOrder / Receival / Shipment when they
   land.
8. **No per-tenant schema customization, no flexible columns, no
   workflow engine** — frozen by `GULIERP_MODULE_INDEPENDENCE_RULE`.
   No change.
9. **Three permission dimensions (role / scope / boundary)** —
   already in Identity V1. No change.
10. **Multi-Company books with `DefaultCurrency`** — already in
    Company entity (DEC-ID-008). Apply the same to Plant in V2+.

---

## 11. V2+ watchlist (explicit deferrals)

| Concern                            | Why deferred                                | Reference family pattern       |
|------------------------------------|---------------------------------------------|---------------------------------|
| Universal workflow engine          | Adds a framework; needs a design goal first | Yonyou BPM / Kingdee K-BPM      |
| Per-tenant customization (columns) | Breaks upgrade paths                         | Yonyou NC Cloud (rejected)      |
| Mobile app                         | Requires separate design goal + auth flow   | Yonyou UAP / Kingdee s-HCM mobile |
| InventoryMethod on Item            | Inventory accounting policy; not MDM        | Yonyou / Kingdee                |
| Sourcing Policy on Item            | Multi-dim sourcing; needs a design goal      | Kingdee EAS                     |
| Bank account on BusinessPartner    | Financial-subject linkage; Finance module    | Yonyou / Kingdee                |
| Contact as a first-class entity    | Multi-contact per partner; needs design goal | Yonyou NC Cloud (rejected)      |
| Universal report designer           | BI module; not V1                            | All 3 (rejected for V1)         |
| Real-time inventory (bins + qty)   | Inventory transactions                      | scm.net / Kingdee               |
| Approval mobile                    | Mobile app; not V1                           | Yonyou UAP                      |

Each deferral is a future WorkItem that opens its own design
goal. The deferrals are not bugs; they are deliberate V1 scope
boundaries.

---

## 12. One-line summary

GuliERP absorbs three ideas: **scm.net's modesty** (small data
model, audit fields, operator-typed codes), **Fiori's visual
language** (2-tone sidebar, semantic tokens, compact density),
and **Chinese ERP's identity model** (Tenant / Company / Org
separation, snapshot-on-document, status-not-delete). GuliERP
rejects three patterns: **per-tenant schema customization,
flexible "spare columns", and a universal workflow engine**. The
result is a small, fast, auditable, upgrade-safe Chinese-language
manufacturing ERP kernel — built on a Vue 3 SPA + a clean
.NET 10 backend, with Foundation + Identity + MDM + DocumentKernel
live and SalesOrder vertical-sliced. Purchase / Inventory /
Production / Quality are queued behind explicit design goals.
