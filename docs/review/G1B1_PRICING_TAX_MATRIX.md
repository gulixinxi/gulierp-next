# G1B-1 Pricing / Tax Verification Matrix

| Field | Value |
|---|---|
| Reviewer | Mavis (Independent Review Agent) |
| Goal | G1B-1R — SalesOrder UX Independent Review Preparation |
| Subject | 8+ numeric pricing/tax examples for prototype validation |
| Authority | `SALES_ORDER_BUSINESS_SPEC_V1.md` §3.4, §4 (FROZEN per DEC-SO-001 + DEC-SO-002) |
| Hard rule | Pricing/tax calculations are **server-side** in real impl. The prototype is a UX demo, but it must compute the SAME way to be a valid demo. |

> **How to use this matrix**: each test case is a closed numeric
> scenario. The Operator (or TRAE before the Operator) must:
>
> 1. Open the prototype, create or open the matching mock document.
> 2. Enter the L11 / L16 / L17 / L19 / L23 / L24 values as listed.
> 3. Read the displayed L20 / L21 / L22 / L25.
> 4. Compare to the **Expected** values.
>
> If any single number mismatches: **FAIL**. The UX demo is not faithful
> to the spec, and the operator should NOT grant
> `SALES_ORDER_UX_APPROVED`.

---

## 0. Master formulas (per spec §4)

These formulas are the **canonical** calculation. The prototype must
implement them, in JavaScript mock layer, exactly.

```
F1  NetAmountExclTax (L20) = Quantity × UnitPriceExclTax × (1 - DiscountRate) - DiscountAmount
F2  TaxAmount       (L21) = L20 × LineTaxRate
F3  GrossAmount     (L22) = L20 + L21
F4  UnitPriceInclTax (L17) = UnitPriceExclTax × (1 + LineTaxRate)  -- when L18=TaxExclusive
F5  UnitPriceExclTax (L16) = UnitPriceInclTax / (1 + LineTaxRate)  -- when L18=TaxInclusive
F6  HeaderTotalExclTax  = Σ L20
F7  HeaderTotalTax      = Σ L21
F8  HeaderTotalInclTax  = Σ L22
```

Precision:
- L20 / L25: HALF_EVEN, 4 decimal places (per POC-003 invariant)
- L21 / L22: HALF_EVEN, 2 decimal places (display)
- L11: decimal up to 4 decimal places (per spec §3.3 L11 type)

**Rounding policy** (POC-003):
- `HALF_EVEN` (banker's rounding): 0.5 → 0; 1.5 → 2; 2.5 → 2; 3.5 → 4.
- Applied to intermediate calculations, then re-rounded at display.

---

## 1. Test case matrix — 12 examples

Each row is a single test case. Operator opens the prototype, creates
or loads a mock document matching the inputs, and reads the displayed
amounts.

### TC-PT-01: Plain 未税 (TaxExclusive) — no discount

| Input | Value |
|---|---|
| LinePriceMode (L18) | `TaxExclusive` |
| Quantity (L11) | 100 |
| UnitPriceExclTax (L16) | 1.20 |
| LineTaxRate (L19) | 0.13 |
| DiscountRate (L23) | 0 |
| DiscountAmount (L24) | 0 |

**Expected**:
| Field | Calculation | Display |
|---|---|---|
| L17 (UnitPriceInclTax) | 1.20 × 1.13 = 1.356 → HALF_EVEN 4dp | 1.3560 |
| L20 (NetAmount) | 100 × 1.20 × 1.0 = 120.00 | 120.0000 |
| L21 (TaxAmount) | 120.00 × 0.13 = 15.60 | 15.60 |
| L22 (GrossAmount) | 120.00 + 15.60 = 135.60 | 135.60 |
| L25 (NetAmountExclTax) | same as L20 (no discount) | 120.0000 |

**PASS rule**: all 5 numbers match to 2/4dp.

---

### TC-PT-02: Plain 含税 (TaxInclusive) — no discount

| Input | Value |
|---|---|
| LinePriceMode (L18) | `TaxInclusive` |
| Quantity (L11) | 10 |
| UnitPriceInclTax (L17) | 961.00 (user enters this) |
| LineTaxRate (L19) | 0.13 |
| DiscountRate (L23) | 0 |
| DiscountAmount (L24) | 0 |

**Expected**:
| Field | Calculation | Display |
|---|---|---|
| L16 (UnitPriceExclTax) | 961.00 / 1.13 = 850.4424... → HALF_EVEN 4dp | 850.4424 |
| L20 (NetAmount) | 10 × 850.4424 = 8504.4240 | 8504.4240 |
| L21 (TaxAmount) | 8504.4240 × 0.13 = 1105.57512 → 2dp | 1105.58 |
| L22 (GrossAmount) | 9610.00 (= 10 × 961) | 9610.00 |
| L25 | 8504.4240 | 8504.4240 |

**PASS rule**: L16 derived to 850.4424 (or close, depending on
rounding order). L20 = 8504.42. L22 = 9610.00. L21 = 1105.58.
(Verify that L21 uses L20 as base, not L22 minus L20 — that would give
1105.58 too but a different derivation path.)

**Tolerance**: ±0.01 per line. ±0.05 per header total.

---

### TC-PT-03: 含税→未税 reverse check (verify both ways give same L20)

Compute **含税→未税** (TC-PT-02 path) and **未税→含税** (TC-PT-03 path).
Both should produce L20 ≈ 8504.42 (modulo rounding).

**TC-PT-03 path** (start from L16):

| Input | Value |
|---|---|
| L18 | `TaxExclusive` |
| L11 | 10 |
| L16 | 850.00 (user enters) |
| L19 | 0.13 |

| Field | Calculation | Display |
|---|---|---|
| L17 | 850 × 1.13 = 960.50 | 960.50 |
| L20 | 10 × 850.00 = 8500.00 | 8500.0000 |
| L21 | 8500 × 0.13 = 1105.00 | 1105.00 |
| L22 | 8500 + 1105 = 9605.00 | 9605.00 |

**Comparison**:
- TC-PT-02 gave L20 = 8504.4240, L22 = 9610.00.
- TC-PT-03 gives L20 = 8500.00, L22 = 9605.00.
- **Difference**: 4.42 in L20, 5.00 in L22.

**This is a known issue with two-way conversion at 4dp precision.**
The Operator should record which path is canonical:
- For prototype UX, **both paths are valid** (different user input
  styles). The prototype must allow both, and display the resulting
  numbers consistently.

**PASS rule**: both paths compute without error; L20 / L21 / L22
follow the formulas. The difference is the rounding artefact, not a
bug.

**Operator note**: if the prototype uses L17 / L16 = 850.00 / 960.50
exactly (truncating instead of HALF_EVEN), L20 in 含税 path would
become 850.00 × 10 = 8500.00, and L22 = 9605.00, which is consistent
with TC-PT-03. This is **also valid** if the prototype rounds L16 from
961.00/1.13 to 850.00 (or 850.4424 → 850 for display, but uses 850.4424
in calculation). The point is consistency: whichever rounding is used
must be applied consistently.

**Recommendation for the prototype**:
- Use **L16 for all calculations** (the user-entered or derived Net
  price). When user enters 含税, derive L16 = L17 / (1 + L19) and
  store it to 4dp.
- All downstream calcs (L20, L21, L22) use L16, not L17.
- Display both L16 and L17 to user; L17 may show "approximate" for
  inverse calculation.

---

### TC-PT-04: Discount rate 10% (DEC-SO-002)

| Input | Value |
|---|---|
| L18 | `TaxExclusive` |
| L11 | 4 |
| L16 | 2300.00 |
| L19 | 0.13 |
| L23 | 0.10 (10%) |
| L24 | 0 (derived) |

**Expected**:
| Field | Calculation | Display |
|---|---|---|
| L17 | 2300 × 1.13 = 2599.00 | 2599.00 |
| L20 | 4 × 2300 = 9200.00 | 9200.0000 |
| L24 (derived) | 9200 × 0.10 = 920.00 | 920.00 (read-only) |
| L25 (NetAmountExclTax) | 9200 × 0.9 = 8280.00 | 8280.0000 |
| L21 (TaxAmount) | 8280 × 0.13 = 1076.40 | 1076.40 |
| L22 (GrossAmount) | 8280 + 1076.40 = 9356.40 | 9356.40 |

**PASS rule**: L24 derived to 920.00. L25 = 8280.00. L21 = 1076.40
on L25 (not L20). L22 = 9356.40.

**Hard fail**: if L21 = 9200 × 0.13 = 1196.00 (using L20 instead of L25)
= FAIL.

---

### TC-PT-05: Discount amount 200 (DEC-SO-002)

| Input | Value |
|---|---|
| L18 | `TaxExclusive` |
| L11 | 1 |
| L16 | 1200.00 |
| L19 | 0.13 |
| L23 | 0 (derived) |
| L24 | 200.00 (user enters) |

**Expected**:
| Field | Calculation | Display |
|---|---|---|
| L17 | 1200 × 1.13 = 1356.00 | 1356.00 |
| L20 | 1 × 1200 = 1200.00 | 1200.0000 |
| L23 (derived) | 200 / 1200 = 0.16666... | 0.1667 (16.67%, 4dp) |
| L25 (NetAmountExclTax) | 1200 - 200 = 1000.00 | 1000.0000 |
| L21 (TaxAmount) | 1000 × 0.13 = 130.00 | 130.00 |
| L22 (GrossAmount) | 1000 + 130 = 1130.00 | 1130.00 |

**PASS rule**: L23 derived to 0.1667 (or 0.17 with 2dp display). L25 =
1000.00. L21 = 130.00 on L25.

---

### TC-PT-06: Mixed tax rates (DEC-SO-001 explicit)

3 lines on same SO with different tax rates.

| Line | L4 | L11 | L16 | L19 |
|---|---|---|---|---|
| L1 | ITEM-002 | 2 | 850.00 | 0.13 |
| L2 | ITEM-005 | 100 | 8.50 | 0.09 |
| L3 | ITEM-001 | 500 | 1.20 | 0.06 |

**Expected per line**:
| Line | L20 | L21 | L22 |
|---|---|---|---|
| L1 | 1700.00 | 221.00 | 1921.00 |
| L2 | 850.00 | 76.50 | 926.50 |
| L3 | 600.00 | 36.00 | 636.00 |

**Header totals**:
| Field | Sum |
|---|---|
| HeaderTotalExclTax | 3150.00 |
| HeaderTotalTax | 333.50 |
| HeaderTotalInclTax | 3483.50 |

**PASS rule**: 3 different L19 values coexist; per-line calcs match;
header totals = sum of per-line. No global tax override.

**Hard fail rule**: if prototype forces one tax rate for the whole
SO = FAIL. DEC-SO-001.

---

### TC-PT-07: Rounding edge — 0.005 (HALF_EVEN)

Verify the prototype implements HALF_EVEN, not HALF_UP.

| Input | Value |
|---|---|
| L11 | 3 |
| L16 | 0.015 (= a unit price) |
| L19 | 0.13 |

**Expected**:
- L20 = 3 × 0.015 = 0.045 → HALF_EVEN at 2dp = 0.04 (since 0.045 is
  halfway between 0.04 and 0.05; HALF_EVEN picks the even one = 0.04).
  Wait, 0.04 vs 0.05: 4 is even, 5 is odd → 0.04 wins.
- Display: 0.0400 (L20 4dp) → 0.04 (footer).

| Field | Value |
|---|---|
| L20 | 0.04 (after HALF_EVEN) |
| L21 | 0.04 × 0.13 = 0.0052 → HALF_EVEN 2dp = 0.01 |
| L22 | 0.04 + 0.01 = 0.05 |

**PASS rule**: L20 = 0.04, NOT 0.05 (HALF_UP would give 0.05).

If prototype shows 0.05: likely using HALF_UP; technically wrong
per spec §4 (HALF_EVEN).

**Note**: this is a V1 detail-level concern. For prototype UX demo,
HALF_EVEN is the spec but a tolerance of "0.04 or 0.05" is acceptable
in V1.5+.

---

### TC-PT-08: Rounding edge — 2.5 (HALF_EVEN → 2, NOT 3)

| Input | Value |
|---|---|
| L11 | 1 |
| L16 | 2.5 |
| L19 | 0.13 |

| Field | Value |
|---|---|
| L20 | 1 × 2.5 = 2.5 (exact, no rounding needed) |
| L21 | 2.5 × 0.13 = 0.325 → HALF_EVEN 2dp = 0.32 (since 0.32 is even) |
| L22 | 2.5 + 0.32 = 2.82 |

**PASS rule**: L21 = 0.32, not 0.33. (HALF_UP would give 0.33.)

---

### TC-PT-09: Currency = CNY only (V1 multi-currency OFF)

| Aspect | Spec |
|---|---|
| Currency dropdown | Only `CNY` |
| ExchangeRate | 1.0, read-only or absent |
| Multi-currency | **NOT in V1** |

**PASS rule**: Currency dropdown has only CNY; no USD/EUR option.
If prototype shows multiple currencies: mark OPEN_QUESTION, not FAIL
(it's a future option, not a spec violation per se — but per DEC-SO-001
H19, multi-currency is V1.5+; for V1, only CNY).

---

### TC-PT-10: 含税 + Discount (含税 path with discount)

| Input | Value |
|---|---|
| L18 | `TaxInclusive` |
| L11 | 100 |
| L17 | 11.30 (user enters) |
| L19 | 0.13 |
| L23 | 0.10 (10%) |

**Expected**:
- L16 (derived) = 11.30 / 1.13 = 10.00 (exactly)
- L20 = 100 × 10.00 = 1000.00
- L24 (derived) = 1000 × 0.10 = 100.00
- L25 = 1000 - 100 = 900.00
- L21 = 900 × 0.13 = 117.00
- L22 = 900 + 117 = 1017.00 (= 100 × 11.30 × 0.9 = 1017.00 ✓)

**PASS rule**: L22 cross-checks as 1017.00 (= 100 × 11.30 × 0.9).
This is the same as if user had entered L17=11.30 directly with
discount, because 含税 × (1 - rate) = 含税净额. Good sanity check.

---

### TC-PT-11: Zero quantity (rejected)

| Input | Value |
|---|---|
| L11 | 0 |

**PASS rule**: Save blocked; L11 cell highlighted; "数量必须大于 0"
inline error.

---

### TC-PT-12: Negative price (rejected)

| Input | Value |
|---|---|
| L16 | -1.20 |

**PASS rule**: Save blocked; L16 cell highlighted; "单价不能为负"
inline error.

---

## 2. Aggregate summary

| TC | Pass criteria summary |
|---|---|
| TC-PT-01 | Plain 未税, 1.20 × 100 = 120.00 net, 15.60 tax, 135.60 gross |
| TC-PT-02 | Plain 含税, 961.00 × 10 → L16 derived 850.4424, 1105.58 tax, 9610.00 gross |
| TC-PT-03 | Both 含税↔未税 paths work (with rounding tolerance) |
| TC-PT-04 | 10% rate: L24 derived 920.00, L25 = 8280.00, L21 on L25 = 1076.40 |
| TC-PT-05 | 200 amount: L23 derived 0.1667, L25 = 1000.00, L21 = 130.00 |
| TC-PT-06 | 3 different L19 coexist, header sum correct |
| TC-PT-07 | HALF_EVEN: 0.045 → 0.04, not 0.05 |
| TC-PT-08 | HALF_EVEN: 0.325 → 0.32, not 0.33 |
| TC-PT-09 | Currency dropdown = CNY only (V1) |
| TC-PT-10 | 含税 + discount cross-check: L22 = 100 × 11.30 × 0.9 = 1017.00 |
| TC-PT-11 | L11 = 0 rejected |
| TC-PT-12 | L16 < 0 rejected |

**12 cases. Operator must run all 12 and confirm PASS.**

---

## 3. Operator-facing rules

- **Tolerance**: per-line ±0.01; header sum ±0.05. Anything larger = FAIL.
- **Rounding policy**: if the prototype uses HALF_UP instead of HALF_EVEN,
  it's a **spec deviation** but not necessarily FAIL. Record as
  `OPEN_QUESTION` (would be a separate fix in the API contract).
- **Hard fails** (automatic): tax rate coercion, derived field editable,
  wrong formula.

---

## 4. Cross-references

- `G1B1_SALESORDER_UX_COVERAGE_MATRIX.md` §3 (Pricing/Tax coverage)
- `G1B1_SALESORDER_MOCK_SCENARIOS.md` §1, §2, §3, §4, §5
- `G1B1_HARD_FAIL_CHECKLIST.md` HF-PT-* (Hard fail rules)
- `SALES_ORDER_BUSINESS_SPEC_V1.md` §3.4, §4 (FROZEN per DEC-SO-001/002)
