# Inventory Requirement Discovery

Status: discovery only. No implementation approval.

## Known Business Facts

- DEV inventory navigation includes pending inbound, purchase inbound,
  production inbound, subcontract inbound, customer-supplied inbound, other
  inbound, stock query, in/out detail, stock report, sales outbound, pending
  shipment, production issue, other issue, safety stock warning, historical
  stock, workshop material issue, sales transfer, material transfer, system
  settings, batch inventory, scan issue and location binding.
- DEV has no independent inventory transaction fact table.
- DEV appears to compute balance through business document aggregation.
- DEV has warehouse but no confirmed first-class location model.
- DEV treats batch inventory as query/view behavior rather than a first-class
  lot entity.

## Items To Confirm

- Required warehouse and location granularity.
- Whether lot/batch is mandatory in V1.
- Whether safety stock is global per item or per item and warehouse.
- Required posting model: immediate, approval-based or event-driven.
- Whether inventory reservation is required for confirmed sales orders.
- Whether quality status blocks available stock.
- Transfer, adjustment and stocktake approval requirements.
- Required stock query and detail reporting UX.

## Hard Gate

Formal Inventory UI or implementation is forbidden until:

`BUSINESS_SPEC_FROZEN -> UX_PROTOTYPE -> USER_UX_APPROVED -> API_CONTRACT_FROZEN -> IMPLEMENTATION`

