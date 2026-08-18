# Business Source of Truth

Gate: `GULIERP_GREENFIELD_BOOTSTRAPPED`

This document is the business knowledge intake for GuliERP Next. It does not
authorize implementation of Sales, Purchase, or Inventory. Implementation starts
only after the UI approval gate is passed.

## Source Inputs

| Source | Role | Usage |
|---|---|---|
| `D:\guli\gulierp\docs\reverse-engineering\DEV_METADATA_REVERSE_ENGINEERING_REPORT.md` | DEV metadata map | Business knowledge only |
| `DEV_BUSINESS_DOCUMENT_TEMPLATE_SPEC.md` | Document template concepts | Rework into strong document models |
| `DEV_SECURITY_MODEL_ANALYSIS.md` | Security risks and useful permission dimensions | Redesign security boundary |
| `DEV_FORMULA_AND_RULE_CATALOG.md` | Formula/rule catalog | Reject SQL rules; reimplement as typed services |
| `DEV_INVENTORY_SPEC.md` | Inventory knowledge | Redesign inventory model |
| `DEV_PRODUCTION_SPEC.md` | Production knowledge | Future spec reference |
| `DEV_QUALITY_SPEC.md` | Quality traces | Future spec reference |
| Latest handoff `ERP-VIS-001_*` | UX/runtime evidence experience | Reference only; failed/legacy POC is not the base |

## Asset Classification

### REUSE_KNOWLEDGE

Knowledge that may inform new GuliERP design, without copying implementation:

- Business navigation concepts: Sales, Purchase, Inventory, Production.
- Document header/line pattern as a concept.
- Business numbering concept.
- Upstream/downstream document relationship concept.
- Operation audit and design audit separation.
- Organization scope, role, action, row-level and field-level permission needs.
- Inventory domain needs: warehouse, receiving, issuing, transfer, safety stock,
  batch visibility, posting, stock query.
- Production domain needs: BOM, routing, work center, production order,
  subcontracting, reporting.
- Quality traces: inspection required flag, quality status, exception record,
  quality event.

### REDESIGN

Concepts that are useful but must be redesigned:

- DEV metadata templates become strong typed domain models.
- DEV template linker becomes application services and domain/integration events.
- DEV row-level filters become typed data scope policies.
- DEV permissions become backend-enforced policies, not UI-only hiding.
- DEV workflow status string becomes workflow instance and task state models.
- DEV inventory flow becomes explicit transactions, balances, reservations and
  posting boundaries.
- DEV production tables become versioned BOM, routing, work center and
  production order state machines.

### REIMPLEMENT

Capabilities that should be implemented from scratch in GuliERP style after
spec and UX approval:

- Foundation identity and organization boundary.
- Business number generator.
- Document abstractions.
- Audit writer and design audit writer.
- Dictionary/reference data.
- Permission and data scope enforcement.
- Inventory transaction and balance service.
- Workflow boundary and approval integration.

### REJECT

Patterns that must not be copied:

- Admin.NET runtime, Furion, SqlSugar, Admin.NET Auth/Token/Menu/Web Shell.
- Legacy GuliERP sync scripts.
- Direct copy of existing Sales/Purchase/Inventory implementations.
- Metadata SQL rules, manual SQL in lookup/select/control rules.
- DEV database shape with zero foreign keys and zero check constraints.
- Text-name relationships for customer, supplier, item, warehouse, unit or org.
- Default template password `123456`.
- Files stored in `image`, `text`, or `ntext` columns.
- Inventory balance calculated only by live aggregation.
- Inventory posting as a manual screen step.
- Field-level security implemented only as frontend hiding.

## Current Business Facts

- DEV contains four business navigation canvases: sales, purchase, inventory,
  production.
- Sales flow references order, shipment/outbound, invoice, receipt, ledgers and
  reports.
- Purchase flow references order, receiving/inbound, invoice, payment, ledgers,
  requisition and MRP summary.
- Inventory flow references receiving, issuing, transfer, safety stock warning,
  batch inventory, stock query and posting.
- Production flow references calendar, production order, plan, schedule,
  dispatch, receipt, assembly, reporting, subcontracting and operation transfer.
- Quality traces exist but do not prove a complete QMS was used.

## Open Confirmation Items

- Which Sales Order fields and states are required by the actual business?
- Which Purchase Order fields and receiving/invoice/payment links are required?
- Which Inventory posting rules are required for V1?
- Whether batch, lot, location and safety stock are required in the first
  deployable release.
- Whether approval limits, data scope and field policies are required in V1 or
  can remain reserved.

