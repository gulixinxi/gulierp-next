# Sales Order Requirement Discovery

Status: discovery only. No implementation approval.

## Known Business Facts

- DEV sales navigation includes sales order, sales outbound, sales invoice,
  sales receipt, customer maintenance, product maintenance, order ledger,
  outbound detail, invoice detail, receipt detail, sales report, pending shipment
  list, outbound posting and accounts receivable.
- DEV suggests a downstream chain: sales order -> sales outbound -> sales
  invoice -> receipt/write-off.
- Latest handoff proves that a visible approval flow was valuable as an
  experience reference, but it must not become the new implementation base.

## Items To Confirm

- Required order statuses and allowed transitions.
- Required header fields.
- Required line fields.
- Pricing, discount, tax and currency rules.
- Whether customer credit, approval limits or price policy are in V1.
- Whether order confirmation reserves inventory.
- Whether partial shipment is required.
- Whether sales order can generate production demand.
- Required list/detail/print/export UX.

## Hard Gate

Formal Sales Order UI or implementation is forbidden until:

`BUSINESS_SPEC_FROZEN -> UX_PROTOTYPE -> USER_UX_APPROVED -> API_CONTRACT_FROZEN -> IMPLEMENTATION`

