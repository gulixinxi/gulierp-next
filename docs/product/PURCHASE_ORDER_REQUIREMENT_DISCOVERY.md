# Purchase Order Requirement Discovery

Status: discovery only. No implementation approval.

## Known Business Facts

- DEV purchase navigation includes purchase order, pending receiving list,
  purchase receiving, receiving posting, supplier maintenance, material
  maintenance, requisition, MRP summary, order ledger, receiving detail, invoice
  detail, payment detail, accounts payable, purchase payment, purchase invoice
  and purchase report.
- DEV suggests a downstream chain: purchase order -> purchase receiving ->
  purchase invoice -> payment/write-off.
- Purchase touches MDM, inventory, finance and approval boundaries.

## Items To Confirm

- Required order statuses and approval steps.
- Required header and line fields.
- Requisition to purchase order relationship.
- Supplier price, tax, currency and payment term rules.
- Partial receiving rules.
- Over-receiving tolerance.
- Whether receiving requires quality inspection.
- Whether purchase order confirmation affects MRP or inventory reservation.
- Required list/detail/print/export UX.

## Hard Gate

Formal Purchase Order UI or implementation is forbidden until:

`BUSINESS_SPEC_FROZEN -> UX_PROTOTYPE -> USER_UX_APPROVED -> API_CONTRACT_FROZEN -> IMPLEMENTATION`

