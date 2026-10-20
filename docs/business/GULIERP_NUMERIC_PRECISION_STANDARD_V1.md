# GULIERP_NUMERIC_PRECISION_STANDARD_V1

| Field | Value |
|---|---|
| Goal | `GULIERP_NUMERIC_PRECISION_STANDARD_V1` |
| Nature | Contract freeze / governance standard |
| Repository | `D:\guli\projects\gulierp-next` |
| Scope | Quantity, price/cost, amount, tax rate, discount rate, exchange rate, rounding, display |
| Hard boundary | No production code change, no database change, no migration, no Vue change, no test change, no Inventory implementation, no commit, no push |
| Final gate | `GULIERP_NUMERIC_PRECISION_STANDARD_V1_VERIFIED` |

## 1. Purpose

Freeze the V1 numeric contract before Inventory Receiving, document lineage, posting, reversal, and future stock movement work.

This standard prevents silent drift between Sales, Purchase, MDM, Inventory, frontend display, database scale, and reference ERP behavior.

## 2. Scope

In scope:

- quantities and base quantities;
- unit prices, unit costs, base prices, tax-exclusive prices, tax-inclusive prices;
- commercial amounts including net, tax, gross, total, discount and posted document amounts;
- tax rates, discount rates and exchange rates;
- storage precision, calculation precision, display precision and rounding boundary;
- sign, zero and range rules.

Out of scope for V1:

- per-item arbitrary precision engines;
- per-currency configurable minor-unit tables;
- per-UOM precision configuration UI;
- invoice-country-specific rounding exceptions;
- finance posting or accounting voucher implementation;
- Inventory Receiving implementation.

## 3. Terminology

| Term | Meaning |
|---|---|
| Storage precision | Database column precision and scale. |
| Calculation precision | Internal arithmetic behavior before a business boundary is reached. |
| Display precision | UI/report/API presentation scale. |
| Rounding mode | The tie-breaking algorithm used when a value must be rounded. |
| Rounding boundary | The exact point where raw calculation becomes a persisted or externally visible business value. |
| Quantity | A business line quantity in an order, receipt, shipment or movement. |
| BaseQuantity | Quantity converted to the item's base UOM. |
| UnitPrice / UnitCost | A tax-exclusive price or cost per unit unless explicitly named otherwise. |
| Amount | Money value at a document or line boundary. |
| Rate | A ratio, not a percent integer. `13%` is stored and transmitted as `0.13`. |

## 4. Four Precision Layers

Every numeric business field must be understood through four layers:

| Layer | Rule |
|---|---|
| Storage | Uses PostgreSQL `numeric(p,s)` and EF `decimal` mapping. |
| Calculation | Uses C# `decimal`; never use `double` or `float` for business money, quantity or rate calculations. |
| Display | May show fewer decimals than storage, but must not imply a different stored value. |
| Rounding | Happens only at frozen business boundaries, not casually after every arithmetic operator. |

API JSON for V1 transmits numeric decimals as JSON numbers, not formatted strings. UI formatting such as currency symbols and percent signs is presentation only.

## 5. Quantity Standard

| Field family | V1 storage | Calculation | Default display | Rule |
|---|---:|---|---:|---|
| Quantity | `numeric(20,6)` | `decimal`, full intermediate precision | 4 decimals | Ordinary business document line quantity. |
| BaseQuantity | `numeric(20,6)` | `decimal`, full intermediate precision | 4 decimals | Quantity normalized to base UOM. |
| MovementQuantity | `numeric(20,6)` | `decimal`, full intermediate precision | 4 decimals | Inventory movement absolute quantity. |

Ordinary order, receipt, shipment and movement commands require quantity greater than zero. Sign is not expressed by negative quantity for normal flow; sign is expressed by movement direction or document type.

Current Sales/Purchase `numeric(18,4)` is valid current implementation evidence, but it is not the V1 target contract.

## 6. Price / Cost Standard

| Field family | V1 storage | Calculation | Default display | Rule |
|---|---:|---|---:|---|
| UnitPrice | `numeric(20,6)` | `decimal`, full intermediate precision | 4 decimals, allow up to 6 where needed | Tax-exclusive unit price unless explicitly named otherwise. |
| UnitCost | `numeric(20,6)` | `decimal`, full intermediate precision | 4 decimals, allow up to 6 where needed | Cost per unit. |
| BasePrice | `numeric(20,6)` | `decimal`, full intermediate precision | 4 decimals, allow up to 6 where needed | Company/base-currency price where introduced. |
| TaxInclusivePrice | `numeric(20,6)` | `decimal`, full intermediate precision | 4 decimals, allow up to 6 where needed | Must be explicitly named; never infer from `UnitPrice`. |

Ordinary price and cost values are greater than or equal to zero. Negative prices require a correction, return or finance-specific contract and are not part of ordinary V1 documents.

## 7. Amount Standard

| Field family | V1 storage | Calculation | Default display | Rule |
|---|---:|---|---:|---|
| NetAmount | `numeric(20,2)` | raw decimal until line amount boundary | 2 decimals | Tax-exclusive line/header amount. |
| TaxAmount | `numeric(20,2)` | raw decimal until line tax boundary | 2 decimals | Tax amount. |
| GrossAmount / TotalAmount | `numeric(20,2)` | sum of rounded net and tax amounts | 2 decimals | Tax-inclusive amount. |
| DiscountAmount | `numeric(20,2)` | raw decimal until discount amount boundary | 2 decimals | Explicit discount amount where introduced. |

Amount scale is frozen at 2 decimals for V1 commercial document fields. Higher internal precision may exist before the amount boundary, but persisted commercial amounts must use the V1 amount scale.

## 8. Tax Rate Standard

| Field family | V1 storage | Calculation | Default display | Rule |
|---|---:|---|---:|---|
| TaxRate | `numeric(12,8)` | ratio decimal | percent with 2 decimals | `0.13` means `13.00%`. |

Tax rates are ratios in the closed range `0 <= TaxRate <= 1` for ordinary V1 documents. UI may render `13.00%`, but APIs and persistence use `0.13`.

## 9. Discount Standard

| Field family | V1 storage | Calculation | Default display | Rule |
|---|---:|---|---:|---|
| DiscountRate | `numeric(12,8)` | ratio decimal | percent with 2 decimals | `0.20` means a 20% discount amount. |
| DiscountFactor | Not a synonym | ratio decimal | percent/factor only if explicitly named | `0.80` is a multiplier, not `DiscountRate`. |

`DiscountRate` means the discount percentage to subtract from gross amount. It does not mean the remaining payable factor.

## 10. Exchange Rate Standard

| Field family | V1 storage | Calculation | Default display | Rule |
|---|---:|---|---:|---|
| ExchangeRate | `numeric(20,10)` | ratio decimal | 6 decimals by default, allow up to 10 | Multiplier from document currency to company/base currency. |

When multi-currency fields are introduced, the default representation is:

`CompanyCurrencyAmount = DocumentCurrencyAmount * ExchangeRate`

Currency conversion rounding must occur at the amount boundary, not during every multiplication.

## 11. Rounding Mode

The V1 commercial rounding mode is:

`MidpointRounding.AwayFromZero`

This matches the current Sales/Purchase runtime calculation behavior. Future statutory, invoice, country, tax bureau, accounting or banker's-rounding exceptions require an explicit extension contract and must not silently override this V1 default.

## 12. Rounding Boundary

V1 rounding boundaries:

1. Calculate raw line gross amount: `Quantity * UnitPrice`.
2. Calculate raw discount effect: `gross * DiscountRate`.
3. Round line `NetAmount` to amount scale 2.
4. Calculate tax from rounded line `NetAmount`.
5. Round line `TaxAmount` to amount scale 2.
6. Line `TotalAmount = NetAmount + TaxAmount`.
7. Header totals are sums of final rounded line amounts.

Do not round after every multiplication. Do not recompute header totals from rounded display strings.

## 13. Tax Calculation Boundary

V1 backend tax calculation is tax-exclusive:

```text
gross = quantity * unitPrice
net = round(gross * (1 - discountRate), 2, AwayFromZero)
tax = round(net * taxRate, 2, AwayFromZero)
total = net + tax
```

Tax-inclusive price entry or conversion may be introduced later only with explicit field names and conversion rules.

## 14. Display Rules

| Semantic type | Default display |
|---|---:|
| Quantity | 4 decimals by default; trim trailing zeroes allowed in read-only views. |
| UnitPrice / UnitCost | 4 decimals by default; entry/detail surfaces should not block 6-decimal future values. |
| Amount | 2 decimals. |
| TaxRate / DiscountRate | percent with 2 decimals. |
| ExchangeRate | 6 decimals by default; allow up to 10. |

Display rules never change storage or calculation semantics.

## 15. UOM Precision Boundary

V1 uses the global quantity scale `numeric(20,6)`.

Per-UOM display precision and per-item quantity precision are reserved for P1/V1.5. They must not be invented inside Inventory Receiving V1 unless a separate contract freezes their ownership and validation model.

## 16. Currency Precision Boundary

V1 commercial amounts use 2 decimals by default for CNY-like commercial documents.

Per-currency minor units, cash rounding and settlement rounding are P1/V1.5 or finance-module extensions. They must be explicit and must not alter the V1 default amount contract.

## 17. Sign Rules

| Semantic type | V1 sign rule |
|---|---|
| Ordinary order/receipt/shipment quantity | Greater than zero. |
| Inventory movement quantity | Absolute quantity greater than zero; direction carries sign. |
| Amount | Zero allowed where price or tax is zero; negative ordinary-flow amounts are not allowed. |
| Returns/corrections/reversals | Use document type, movement direction and reversal relation, not ad hoc negative quantity. |

## 18. Zero Rules

Quantity zero is invalid for ordinary authored business lines.

Zero amount is allowed when business inputs legitimately produce it, for example free goods, zero price or zero tax rate.

Rates may be zero. Tax and discount rates for ordinary V1 documents must not be negative and must not exceed 1.

Use exact decimal comparison. Do not introduce floating-point epsilon rules.

## 19. Range Safety

| Storage | Integer digits | Maximum positive magnitude |
|---|---:|---:|
| `numeric(20,6)` | 14 | `99,999,999,999,999.999999` |
| `numeric(20,2)` | 18 | `99,999,999,999,999,999.99` |
| `numeric(12,8)` | 4 | `9,999.99999999` |
| `numeric(20,10)` | 10 | `9,999,999,999.9999999999` |

The range is sufficient for V1 ERP quantities, prices, commercial amounts, rates and exchange rates while leaving room to widen current implemented precision without changing semantic scale.

## 20. Canonical Standard Table

| Semantic type | CLR/API type | Storage | Calculation | Rounding boundary | Display | Current implementation status |
|---|---|---:|---|---|---:|---|
| Quantity | `decimal` / JSON number | `numeric(20,6)` | full decimal | not per multiplication | 4 | `IMPLEMENTATION_GAP`: current Sales/Purchase `18,4`. |
| BaseQuantity | `decimal` / JSON number | `numeric(20,6)` | full decimal | after UOM conversion boundary | 4 | `FUTURE_FIELD`. |
| MovementQuantity | `decimal` / JSON number | `numeric(20,6)` | full decimal | posting/movement boundary | 4 | `FUTURE_FIELD`: Inventory not implemented. |
| UnitPrice | `decimal` / JSON number | `numeric(20,6)` | full decimal | line amount boundary | 4, support 6 | `IMPLEMENTATION_GAP`: current Sales/Purchase `18,4`. |
| UnitCost | `decimal` / JSON number | `numeric(20,6)` | full decimal | movement/cost boundary | 4, support 6 | `FUTURE_FIELD`. |
| NetAmount | `decimal` / JSON number | `numeric(20,2)` | full decimal before boundary | line amount boundary | 2 | `SAFE_BUT_DIFFERENT`: current `18,2`. |
| TaxAmount | `decimal` / JSON number | `numeric(20,2)` | full decimal before boundary | line tax boundary | 2 | `SAFE_BUT_DIFFERENT`: current `18,2`. |
| TotalAmount | `decimal` / JSON number | `numeric(20,2)` | sum rounded lines | header sum boundary | 2 | `SAFE_BUT_DIFFERENT`: current `18,2`. |
| TaxRate | `decimal` / JSON number | `numeric(12,8)` | ratio decimal | tax boundary | percent 2 | `IMPLEMENTATION_GAP`: current `9,6`. |
| DiscountRate | `decimal` / JSON number | `numeric(12,8)` | ratio decimal | amount boundary | percent 2 | `IMPLEMENTATION_GAP`: current `9,6`. |
| ExchangeRate | `decimal` / JSON number | `numeric(20,10)` | ratio decimal | currency amount boundary | 6 to 10 | `FUTURE_FIELD` / UI mock only. |

## 21. Examples

### Example A: tax-exclusive line

```text
quantity = 3.333333
unitPrice = 12.345678
discountRate = 0.10
taxRate = 0.13

gross raw = 41.152259588774
net = round(37.0370336298966, 2, AwayFromZero) = 37.04
tax = round(37.04 * 0.13, 2, AwayFromZero) = 4.82
total = 41.86
```

### Example B: discount semantics

```text
DiscountRate = 0.20 means subtract 20%.
DiscountFactor = 0.80 means multiply by 80%.
They are not interchangeable field names.
```

### Example C: exchange rate

```text
DocumentCurrencyAmount = 100.00
ExchangeRate = 7.1800000000
CompanyCurrencyAmount raw = 718.000000000000
CompanyCurrencyAmount = round(718.000000000000, 2, AwayFromZero) = 718.00
```

## 22. Prohibited Patterns

The following are prohibited in V1 implementation work:

- using `double` or `float` for business quantities, prices, amounts or rates;
- storing percent rates as `13` when the contract expects `0.13`;
- treating `DiscountRate = 0.80` as "20% off";
- formatting money strings and then using them for business totals;
- rounding after every multiplication;
- letting frontend JavaScript preview calculations define backend persisted amounts;
- using negative quantity for ordinary inbound/outbound direction;
- introducing per-item precision or rounding engines without a separate contract;
- copying Onlyit/DEV physical legacy precision fields as-is;
- changing existing migrations or database schema inside this governance goal.

## 23. Compatibility / Future Extension

V1 intentionally chooses a unified modern baseline:

- it uses current GuliERP behavior where behavior is already stable, especially `AwayFromZero` and line-level amount rounding;
- it widens storage precision targets for quantity, price and rate to prevent Inventory/Receiving migrations from starting too narrow;
- it keeps amount scale at 2 for commercial document fields because current Sales/Purchase and reference evidence align there;
- it defers per-UOM, per-item, per-currency and statutory rounding exceptions.

Future extensions must be additive and explicit. They may add configuration, but must not reinterpret existing field names.

## 24. Frozen Contract

`GULIERP_NUMERIC_PRECISION_STANDARD_V1` freezes:

- `decimal` as the CLR business numeric type;
- JSON numeric decimals, not formatted strings, for API payloads;
- quantity, base quantity and movement quantity storage as `numeric(20,6)`;
- unit price, unit cost and base price storage as `numeric(20,6)`;
- commercial amount storage as `numeric(20,2)`;
- tax and discount rates as ratio decimals stored as `numeric(12,8)`;
- exchange rates as multiplier decimals stored as `numeric(20,10)`;
- commercial rounding as `MidpointRounding.AwayFromZero`;
- tax-exclusive V1 backend line calculation;
- header totals as sums of rounded line amounts;
- no Inventory Receiving implementation until this contract is accepted as the numeric baseline.

Final gate: `GULIERP_NUMERIC_PRECISION_STANDARD_V1_VERIFIED`
