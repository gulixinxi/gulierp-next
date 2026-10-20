# GULIERP_NUMERIC_PRECISION_STANDARD_V1_REPORT

| Field | Value |
|---|---|
| Goal | `GULIERP_NUMERIC_PRECISION_STANDARD_V1` |
| Nature | Current implementation audit + governance verification |
| Repository | `D:\guli\projects\gulierp-next` |
| Branch | `master` |
| HEAD | `139fe1e940258d71b85d328d88b4e1358c0f7b1e` |
| Output standard | `docs/business/GULIERP_NUMERIC_PRECISION_STANDARD_V1.md` |
| Hard boundary | No production code change, no database change, no migration, no Vue change, no test change, no Inventory implementation, no commit, no push |
| Final gate | `GULIERP_NUMERIC_PRECISION_STANDARD_V1_VERIFIED` |

## 1. Repo

Verified repository root:

`D:/guli/projects/gulierp-next`

## 2. Branch

Verified current branch:

`master`

## 3. HEAD

Verified HEAD:

`139fe1e940258d71b85d328d88b4e1358c0f7b1e`

## 4. Dirty Worktree Classification

The worktree was dirty before this goal.

Pre-existing modified files included API, Vue, design-system, router, docs, tests and dev-tool scripts. Pre-existing untracked files included multiple architecture/audit documents and evidence artifacts.

This goal only adds:

- `docs/business/GULIERP_NUMERIC_PRECISION_STANDARD_V1.md`
- `docs/verification/GULIERP_NUMERIC_PRECISION_STANDARD_V1_REPORT.md`

No unrelated WIP is classified as produced by this goal.

## 5. Files Inspected

Current GuliERP implementation:

- `modules/sales/GuliERP.Sales.Infrastructure/Persistence/Configurations/SalesOrderLineConfiguration.cs`
- `modules/sales/GuliERP.Sales.Infrastructure/Persistence/Configurations/SalesOrderConfiguration.cs`
- `modules/sales/GuliERP.Sales.Infrastructure/Sales/SalesOrderService.cs`
- `modules/sales/GuliERP.Sales.Application/SalesOrderDtos.cs`
- `modules/sales/GuliERP.Sales.Domain/Entities/SalesOrderLine.cs`
- `modules/sales/GuliERP.Sales.Infrastructure/Migrations/SalesDbContextModelSnapshot.cs`
- `modules/purchase/GuliERP.Purchase.Infrastructure/Persistence/Configurations/PurchaseOrderLineConfiguration.cs`
- `modules/purchase/GuliERP.Purchase.Infrastructure/Persistence/Configurations/PurchaseOrderConfiguration.cs`
- `modules/purchase/GuliERP.Purchase.Infrastructure/Purchase/PurchaseOrderService.cs`
- `modules/purchase/GuliERP.Purchase.Application/PurchaseOrderDtos.cs`
- `modules/purchase/GuliERP.Purchase.Domain/Entities/PurchaseOrderLine.cs`
- `modules/purchase/GuliERP.Purchase.Infrastructure/Migrations/PurchaseDbContextModelSnapshot.cs`
- `modules/inventory/README.md`
- `modules/mdm/**`
- `apps/web/src/views/sales-order/SalesOrderEdit.vue`
- `apps/web/src/views/sales-order/SalesOrderDetail.vue`
- `apps/web/src/views/sales-order/SalesOrderList.vue`
- `apps/web/src/views/purchase/PurchaseOrderEdit.vue`
- `apps/web/src/views/purchase/PurchaseOrderDetail.vue`
- `apps/web/src/views/purchase/PurchaseOrderList.vue`
- `apps/web/src/components/LookupDialog.vue`
- `apps/web/src/mock/sales-order.ts`
- `apps/web/src/types/sales-order.ts`

Governance/reference inputs:

- `docs/business/GULIERP_PRE_INVENTORY_ARCHITECTURE_DECISION_001.md`
- `docs/business/GULIERP_DEV_TO_CURRENT_ARCHITECTURE_GAP_AUDIT_001.md`
- `docs/verification/ONLYIT_MATURE_ERP_REFERENCE_AUDIT_001.md`

## 6. Current Sales Precision

Current Sales EF precision:

| Field | Current precision |
|---|---:|
| `SalesOrderLine.Quantity` | `HasPrecision(18, 4)` |
| `SalesOrderLine.UnitPrice` | `HasPrecision(18, 4)` |
| `SalesOrderLine.DiscountRate` | `HasPrecision(9, 6)` |
| `SalesOrderLine.TaxRate` | `HasPrecision(9, 6)` |
| `SalesOrderLine.NetAmount` | `HasPrecision(18, 2)` |
| `SalesOrderLine.TaxAmount` | `HasPrecision(18, 2)` |
| `SalesOrderLine.TotalAmount` | `HasPrecision(18, 2)` |
| `SalesOrder.TotalNetAmount` | `HasPrecision(18, 2)` |
| `SalesOrder.TotalTaxAmount` | `HasPrecision(18, 2)` |
| `SalesOrder.TotalAmount` | `HasPrecision(18, 2)` |

Sales migrations/model snapshot confirm matching `numeric(18,4)`, `numeric(9,6)` and `numeric(18,2)` storage types.

## 7. Current Purchase Precision

Current Purchase EF precision:

| Field | Current precision |
|---|---:|
| `PurchaseOrderLine.Quantity` | `HasPrecision(18, 4)` |
| `PurchaseOrderLine.UnitPrice` | `HasPrecision(18, 4)` |
| `PurchaseOrderLine.DiscountRate` | `HasPrecision(9, 6)` |
| `PurchaseOrderLine.TaxRate` | `HasPrecision(9, 6)` |
| `PurchaseOrderLine.NetAmount` | `HasPrecision(18, 2)` |
| `PurchaseOrderLine.TaxAmount` | `HasPrecision(18, 2)` |
| `PurchaseOrderLine.TotalAmount` | `HasPrecision(18, 2)` |
| `PurchaseOrder.TotalNetAmount` | `HasPrecision(18, 2)` |
| `PurchaseOrder.TotalTaxAmount` | `HasPrecision(18, 2)` |
| `PurchaseOrder.TotalAmount` | `HasPrecision(18, 2)` |

Purchase migrations/model snapshot confirm matching `numeric(18,4)`, `numeric(9,6)` and `numeric(18,2)` storage types.

## 8. Current MDM Precision

No MDM numeric price/cost/quantity precision contract was found in the inspected `modules/mdm` scan.

MDM Item, Warehouse and Location are important owners for item/UOM/warehouse/location validation, but this audit did not find current MDM storage fields that freeze quantity, unit price, cost, amount, tax rate, discount rate or exchange rate precision.

## 9. Current Calculation Rules

Sales and Purchase current services use the same calculation pattern:

```text
gross = quantity * unitPrice
net = decimal.Round(gross * (1 - discountRate), 2, MidpointRounding.AwayFromZero)
tax = decimal.Round(net * taxRate, 2, MidpointRounding.AwayFromZero)
total = net + tax
```

Current validation:

- `Quantity > 0`
- `UnitPrice >= 0`
- `0 <= DiscountRate <= 1`
- `0 <= TaxRate <= 1`

Header totals are produced from summed line rounded amount fields.

## 10. Current UI Formatting Facts

Sales/Purchase edit pages:

- quantity input uses `:min="0.0001"` and `:precision="4"`;
- unit price input uses `:min="0"` and `:precision="4"`;
- discount and tax rate inputs use range `0..1` and `:precision="4"`;
- preview totals use JavaScript `number` arithmetic and `Math.round(... * 100) / 100`;
- default tax rate examples use ratio values such as `0.13`.

Sales/Purchase detail pages:

- quantity display uses 4 decimals;
- unit price display uses 4 decimals;
- net, tax and total amount display uses 2 decimals;
- rates display as percent with 2 decimals.

Sales/Purchase list pages:

- total amount display uses 2 decimals.

Lookup dialog:

- item default prices are formatted with `Number(v).toFixed(2)`, which is inconsistent with the 4-decimal edit/detail unit price surfaces and the V1 6-decimal storage target;
- tax rates display as percent with 2 decimals;
- exchange rate-like mock fields are not governed by a current backend contract.

These UI facts are current-state evidence only. They do not override the frozen backend numeric standard.

## 11. DEV Evidence Used

The DEV gap audit records:

- quantity convention about 3 decimals;
- unit price convention about 6 decimals;
- amount convention about 2 decimals;
- stable distinction between quantity, price and amount precision.

This is used as business-design evidence that different numeric families require different precision. DEV physical low-code implementation patterns are not copied.

## 12. Onlyit Evidence Used

The Onlyit mature ERP audit records:

- `res.price_dot_num` supports price decimal settings up to 6;
- `res.num_dot_num` supports quantity decimal settings up to 4;
- Onlyit has multiple rounding controls for inbound/outbound/resource behavior;
- mature ERP behavior separates storage, calculation, display and rounding decisions.

This is used as mature reference evidence. V1 does not copy Onlyit's per-resource arbitrary precision and multi-rounding engine.

## 13. Current Inconsistencies

| Area | Current fact | V1 decision | Classification |
|---|---|---|---|
| Sales quantity | `numeric(18,4)` | `numeric(20,6)` | `IMPLEMENTATION_GAP` |
| Purchase quantity | `numeric(18,4)` | `numeric(20,6)` | `IMPLEMENTATION_GAP` |
| Sales unit price | `numeric(18,4)` | `numeric(20,6)` | `IMPLEMENTATION_GAP` |
| Purchase unit price | `numeric(18,4)` | `numeric(20,6)` | `IMPLEMENTATION_GAP` |
| Sales/Purchase amount precision | `numeric(18,2)` | `numeric(20,2)` | `SAFE_BUT_DIFFERENT` precision widening only |
| Sales/Purchase rates | `numeric(9,6)` | `numeric(12,8)` | `IMPLEMENTATION_GAP` |
| UI price lookup display | 2 decimals | price display should not block 6-decimal storage target | `IMPLEMENTATION_GAP` |
| UI preview arithmetic | JavaScript `number` | backend `decimal` owns persisted calculation | `UI_RISK` |
| Inventory movement quantity | not implemented | `numeric(20,6)` | `FUTURE_FIELD` |
| Exchange rate | mock/UI only | `numeric(20,10)` | `FUTURE_FIELD` |

## 14. Final V1 Decisions

| Decision | Final V1 value |
|---|---|
| CLR business numeric type | C# `decimal` |
| API numeric representation | JSON numeric decimals, not formatted strings |
| Quantity storage | `numeric(20,6)` |
| Price/cost storage | `numeric(20,6)` |
| Commercial amount storage | `numeric(20,2)` |
| Tax/discount rate storage | `numeric(12,8)` |
| Exchange rate storage | `numeric(20,10)` |
| Tax rate representation | ratio, `0.13` means `13%` |
| Discount rate representation | ratio discount amount, `0.20` means 20% off |
| Rounding mode | `MidpointRounding.AwayFromZero` |
| Rounding boundary | line net, line tax, header sum of rounded lines |
| Tax base | tax-exclusive V1 backend calculation |
| Ordinary quantity sign | positive quantity; direction/document type carries sign |
| Zero rule | zero quantity invalid; zero amount/rate allowed when business-valid |

## 15. Gap Matrix

| Semantic type | Current implementation | V1 standard | Impact |
|---|---|---|---|
| Quantity | Sales/Purchase `18,4` | `20,6` | Future migration needed before or during adoption. |
| UnitPrice | Sales/Purchase `18,4` | `20,6` | Future migration + UI precision adjustment needed. |
| Amount | Sales/Purchase `18,2` | `20,2` | Precision widening only; scale aligned. |
| TaxRate | Sales/Purchase `9,6` | `12,8` | Future migration + UI precision review needed. |
| DiscountRate | Sales/Purchase `9,6` | `12,8` | Future migration + UI precision review needed. |
| ExchangeRate | no backend field | `20,10` | Future field contract. |
| Inventory quantity | Inventory not implemented | `20,6` | Must be used when Inventory entities are created. |
| Rounding mode | AwayFromZero | AwayFromZero | Current backend behavior aligned. |
| Header total method | sum rounded line amounts | sum rounded line amounts | Current backend behavior aligned. |

## 16. Migration Impact

This goal creates no migration.

Future implementation impact:

- Sales/Purchase quantity and unit price columns need widening from `18,4` to `20,6` if brought into full V1 standard alignment.
- Sales/Purchase tax and discount rate columns need widening from `9,6` to `12,8`.
- Sales/Purchase amount columns need precision widening from `18,2` to `20,2`; scale remains 2.
- New Inventory movement and receiving quantity fields must start at `20,6`, not current Sales/Purchase `18,4`.
- New exchange-rate fields must start at `20,10`.

## 17. Code Impact

This goal changes no production code.

Future implementation impact:

- EF configurations should use the frozen precision table.
- Application services should keep `decimal` arithmetic.
- Backend persisted amount calculation should remain authoritative.
- Frontend JavaScript previews must not be treated as persisted calculation proof.
- Inventory implementation must not invent a separate numeric standard.

## 18. UI Impact

This goal changes no Vue files.

Future UI impact:

- quantity edit/detail surfaces should support the V1 quantity storage boundary while choosing a practical display default;
- unit price and cost entry must not truncate valid 6-decimal values;
- amount display remains 2 decimals;
- tax and discount rates display as percent but store/transmit ratio decimals;
- lookup price formatting currently using 2 decimals should be reviewed before price-sensitive workflows.

## 19. Test Contract

This goal changes no tests.

Future tests should cover:

- quantity accepts 6-decimal storage values where the field is V1-aligned;
- price accepts 6-decimal storage values;
- amount rounds to 2 decimals using `AwayFromZero`;
- tax uses rounded line net as its taxable base;
- header totals equal sums of rounded line amounts;
- tax and discount rates are ratios, not percent integers;
- frontend preview differences cannot override backend persisted totals;
- Inventory movement quantity is positive absolute quantity with explicit direction.

## 20. P0 / P1 / Future Boundary

P0 before Inventory Receiving implementation:

- this numeric standard is frozen;
- new Inventory quantity fields use `numeric(20,6)`;
- new Inventory amount/rate fields use this standard;
- line amount rounding uses `AwayFromZero`;
- no direct CRUD receiving implementation starts from the old `18,4` assumption.

P1/V1.5:

- per-UOM display precision;
- per-currency minor unit table;
- tax-inclusive price conversion contract;
- configurable commercial rounding policies;
- UI-wide numeric formatter alignment.

Future:

- country/invoice/tax-bureau-specific rounding;
- finance settlement rounding;
- per-item/resource precision settings if business requirements justify the complexity.

## 21. Next Goal

After successful completion, the next goal is:

`GULIERP_DOCUMENT_LINEAGE_AND_EXECUTION_CONTRACT_V1`

Do not start Inventory Receiving from this goal.

## 22. Final Gate

Verification result:

- contract standard file created;
- verification report file created;
- no production code changed;
- no database changed;
- no migration created;
- no Vue changed;
- no test changed;
- no Inventory implementation started;
- no commit created;
- no push performed.

Final status:

`GULIERP_NUMERIC_PRECISION_STANDARD_V1_VERIFIED`
