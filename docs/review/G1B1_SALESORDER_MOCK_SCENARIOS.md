# G1B-1 SalesOrder Mock Scenarios

| Field | Value |
|---|---|
| Reviewer | Mavis (Independent Review Agent) |
| Goal | G1B-1R — SalesOrder UX Independent Review Preparation |
| Subject | Mock data for TRAE G1B-1 SalesOrder UX prototype |
| Authority | `G1B1_SALESORDER_UX_COVERAGE_MATRIX.md`, `SALES_ORDER_BUSINESS_SPEC_V1.md` |
| Purpose | Provide TRAE / Operator with concrete, realistic mock data so the prototype can be exercised end-to-end. **This file is read-only input to TRAE; Mavis does not modify the prototype.** |

> **How to use**: each scenario below is a complete mock case. The
> prototype should let the operator create or open a document matching
> each scenario. The expected UX is described; if the prototype cannot
> produce the expected state, it is a FAIL on that scenario.

---

## 0. Master mock dataset (shared across all scenarios)

### 0.1 Customers (BusinessPartner, role=Customer)

| ID | Code | Name | Default Price Mode | Default Tax Rate | Address | Contact |
|---|---|---|---|---|---|---|
| `CUST-001` | `CUST-001` | 北京制造有限公司 | `TaxExclusive` | 0.13 | 北京市朝阳区建国路 88 号 | 王经理 / 13800000001 |
| `CUST-002` | `CUST-002` | 上海贸易股份有限公司 | `TaxInclusive` | 0.13 | 上海市浦东新区世纪大道 100 号 | 李总 / 13800000002 |
| `CUST-003` | `CUST-003` | 深圳电子科技有限公司 | `TaxExclusive` | 0.13 | 深圳市南山区科技园 5 号楼 | 张工 / 13800000003 |
| `CUST-004` | `CUST-004` | 杭州自动化设备厂 | `TaxExclusive` | 0.09 | 杭州市西湖区文三路 200 号 | 陈工 / 13800000004 |

### 0.2 Items (MDM)

| ID | Code | Name | Spec | BaseUom | LotEnabled | Sales Price (未税) | Sales Price (含税) |
|---|---|---|---|---|---|---|---|
| `ITEM-001` | `ITEM-001` | 标准紧固件 M8 | M8×20 304 不锈钢 | `PCS` | false | 1.20 | 1.36 |
| `ITEM-002` | `ITEM-002` | 工业控制板 | 16DI/16DO 24VDC | `PCS` | true | 850.00 | 961.00 |
| `ITEM-003` | `ITEM-003` | 伺服电机 | 750W 220V | `PCS` | true | 2300.00 | 2599.00 |
| `ITEM-004` | `ITEM-004` | 减速机 | RV-040 1:30 | `PCS` | false | 1200.00 | 1356.00 |
| `ITEM-005` | `ITEM-005` | 电缆 (米) | RVV 3×2.5 黑色 | `M` | false | 8.50 | 9.61 |
| `ITEM-006` | `ITEM-006` | 化工原料 | 异丙醇 99.9% | `KG` | true | 22.00 | 24.86 |

### 0.3 Warehouses and Locations

| Warehouse ID | Code | Name | Type | Location Mandatory Policy |
|---|---|---|---|---|
| `WH-001` | `WH-001` | 主仓 (北京) | Raw + Finished | Yes (per Company policy) |
| `WH-002` | `WH-002` | 成品仓 (上海) | Finished | No (optional) |
| `WH-003` | `WH-003` | 化工仓 (深圳) | Hazardous | Yes (per Item policy, hazmat items) |

| Warehouse | Location ID | Code | Name | Type |
|---|---|---|---|---|
| `WH-001` | `LOC-001-01` | `LOC-001-01` | A 区货架 1 排 | Storage |
| `WH-001` | `LOC-001-02` | `LOC-001-02` | A 区货架 2 排 | Storage |
| `WH-001` | `LOC-001-QC` | `LOC-001-QC` | 收货待检区 | Receiving |
| `WH-002` | `LOC-002-01` | `LOC-002-01` | 货架 1 | Storage |
| `WH-003` | (n/a) | (n/a) | (n/a — no locations) | (n/a) |

### 0.4 Employees (Sales role)

| ID | Code | Name | Department |
|---|---|---|---|
| `EMP-001` | `EMP-001` | 张三 (current user, Sales) | 销售一部 |
| `EMP-002` | `EMP-002` | 李四 (current user approver) | 销售经理办 |
| `EMP-003` | `EMP-003` | 王五 (peer Sales) | 销售二部 |

### 0.5 Pre-existing documents (for source / downstream)

| Doc | Type | Status | Notes |
|---|---|---|---|
| `QT-20260818-0001` | Quotation (mock) | Approved | Source for SO-20260818-0001 |
| `SH-20260818-0001` | Shipment (mock) | Active | Downstream of SO-20260818-0003 |

### 0.6 Quotation (mock for source)

```text
QT-20260818-0001
  DocStatus: Active
  AppStatus: Approved
  Customer: CUST-001
  Lines:
    L1: ITEM-001, Qty 100, UnitPrice (未税) 1.20
    L2: ITEM-005, Qty 50,  UnitPrice (未税) 8.50
```

### 0.7 Shipment (mock for downstream)

```text
SH-20260818-0001
  DocStatus: Active
  AppStatus: Approved
  ExecStatus: Partial
  Source: SO-20260818-0003
  Lines:
    L1: ITEM-002, Qty 5, Shipped 3, Open 2
```

### 0.8 Tax Schemes (mock dictionary)

| ID | Display |
|---|---|
| `13` | VAT 13% (一般纳税人 销售) |
| `9`  | VAT 9% (一般纳税人 销售特定) |
| `6`  | VAT 6% (一般纳税人 现代服务) |
| `0`  | 免税 |

---

## 1. Scenario: 普通未税销售订单 (Plain Tax-Exclusive SO)

| Field | Value |
|---|---|
| Scenario ID | `SCN-SO-01` |
| Title | Plain tax-exclusive single-line SO |
| DocStatus | `Draft` |
| AppStatus | `NotSubmitted` |
| ExecStatus | `NotStarted` |
| Customer | `CUST-001` (北京制造, TaxExclusive default) |
| Salesperson | `EMP-001` (张三) |
| Currency | `CNY` |
| Lines | 1 line |
| Tax rate | 13% |

### Mock data

```text
Header:
  H1 SalesOrderNo: (auto) SO-20260818-NNNN
  H2 OrderDate: 2026-08-18
  H4 RequestedDeliveryDate: 2026-08-25
  H7 Customer: CUST-001 (TaxExclusive default, TaxRate 0.13)
  H14 Salesperson: EMP-001
  H15 SalesDept: 销售一部
  H16 Company: COMPANY-001
  H19 Currency: CNY
  H21 DefaultPriceMode: TaxExclusive (from CUST-001)
  H22 DefaultTaxRate: 0.13
  H23 PaymentTerm: M30
  H27 DeliveryMethod: 送货
  H28 DefaultWarehouse: (none — user must pick)

Lines:
  L1 LineNo: 1
  L4 ItemId: ITEM-001 (标准紧固件 M8)
  L5 ItemName: 标准紧固件 M8
  L6 ItemSpec: M8×20 304 不锈钢
  L8 Uom: PCS
  L11 Quantity: 100
  L18 LinePriceMode: TaxExclusive
  L19 LineTaxRate: 0.13
  L16 UnitPriceExclTax: 1.20
  L17 UnitPriceInclTax: 1.36 (derived: 1.20 × 1.13)
  L23 DiscountRate: 0
  L24 DiscountAmount: 0
  L20 NetAmount: 120.0000 (100 × 1.20, HALF_EVEN 4dp)
  L21 TaxAmount: 15.60 (120 × 0.13)
  L22 GrossAmount: 135.60 (120 + 15.60)
  L25 NetAmountExclTax: 120.0000
  L27 Warehouse: WH-001
  L28 Location: LOC-001-01
  L29 LotRequired: false
```

### Expected UX

- List: SO not yet shown (it's Draft, only saved drafts show).
- Create → Save Draft: see auto-generated `SO-20260818-NNNN`.
- Detail in Draft: all header + line fields editable.
- L16 editable, L17 auto-derives on L16 change.
- L18 defaults to H21 (TaxExclusive). L19 defaults to H22 (0.13).
- L20 / L21 / L22 read-only, recompute live.
- L23 / L24 both 0, neither active; editing one disables the other.
- L27 defaults to (none) since H28 is empty; user must pick.
- L28 mandatory (per WH-001 policy).
- Summary footer: 合计数量 100, 合计未税 120.00, 合计税额 15.60, 合计含税 135.60.

### Operator walkthrough

1. Click 新建.
2. Verify auto-# appears immediately.
3. Set H7 = CUST-001.
4. Verify H21 auto-fills to TaxExclusive, H22 to 0.13.
5. Add 1 line. Set L4 = ITEM-001.
6. Set L11 = 100. Set L16 = 1.20.
7. Verify L17 = 1.36, L20 = 120.00, L21 = 15.60, L22 = 135.60.
8. Set L27 = WH-001. Verify L28 mandatory (cannot save without).
9. Save Draft. Status tags: Doc=Draft, App=NotSubmitted, Exec=NotStarted.

### PASS / FAIL

- **PASS** if all 9 steps work as expected, math is correct to 2dp, all
  3 status tags visible, L28 mandatory under WH-001.
- **FAIL** if any of: math wrong, status not 3D, L28 not mandatory, no
  auto-# appears, L17 doesn't derive from L16.

---

## 2. Scenario: 含税销售订单 (Tax-Inclusive SO)

| Field | Value |
|---|---|
| Scenario ID | `SCN-SO-02` |
| Title | Tax-inclusive single-line SO |
| DocStatus | `Draft` |
| AppStatus | `NotSubmitted` |
| ExecStatus | `NotStarted` |
| Customer | `CUST-002` (上海贸易, TaxInclusive default) |
| Tax rate | 13% |

### Mock data

```text
Header:
  H7 Customer: CUST-002 (TaxInclusive default, TaxRate 0.13)
  H21 DefaultPriceMode: TaxInclusive

Lines:
  L4 ItemId: ITEM-002 (工业控制板)
  L11 Quantity: 10
  L18 LinePriceMode: TaxInclusive
  L19 LineTaxRate: 0.13
  L16 UnitPriceExclTax: 850.00 (derived from L17)
  L17 UnitPriceInclTax: 961.00 (user enters this)
  L20 NetAmount: 8500.00 (10 × 850)
  L21 TaxAmount: 1105.00 (8500 × 0.13)
  L22 GrossAmount: 9605.00 (961.00 × 10)
```

### Expected UX

- L18 defaults to H21 (TaxInclusive).
- User can enter L17 directly (=961.00); L16 derives (=850.00).
- Footer: 合计数量 10, 合计未税 8500.00, 合计税额 1105.00, 合计含税 9605.00.

### PASS / FAIL

- **PASS** if L17 entered, L16 auto-derives correctly, math holds.
- **FAIL** if L16 doesn't derive or L18 doesn't default to H21.

---

## 3. Scenario: 同一张订单不同税率 (Same SO, mixed tax rates)

| Field | Value |
|---|---|
| Scenario ID | `SCN-SO-03` |
| Title | SO with 3 lines, each with a different tax rate |
| DocStatus | `Draft` |
| AppStatus | `NotSubmitted` |
| ExecStatus | `NotStarted` |
| Customer | `CUST-003` (深圳电子, TaxExclusive default) |
| Tax rates | 13%, 9%, 6% (per line) |

### Mock data (DEC-SO-001 explicit requirement)

```text
Header:
  H7 Customer: CUST-003 (TaxExclusive default, 13%)
  H22 DefaultTaxRate: 0.13

Lines:
  L1: ITEM-002, Qty 2, Price (未税) 850.00, L19=0.13
    L20: 1700.00, L21: 221.00, L22: 1921.00
  L2: ITEM-005, Qty 100, Price (未税) 8.50, L19=0.09
    L20: 850.00, L21: 76.50, L22: 926.50
  L3: ITEM-001, Qty 500, Price (未税) 1.20, L19=0.06
    L20: 600.00, L21: 36.00, L22: 636.00
```

### Expected UX

- H22 = 0.13 (default for new line).
- L1 created: L19 defaults to 0.13. Tax = 221.00.
- L2 added: L19 = 0.13 by default. User changes L19 to 0.09. L21
  recomputes to 76.50.
- L3 added: L19 = 0.13 by default. User changes L19 to 0.06. L21
  recomputes to 36.00.
- **Critical**: the same SO contains 3 different tax rates. This is
  per DEC-SO-001.
- Summary footer: 合计未税 3150.00, 合计税额 333.50, 合计含税 3483.50.

### PASS / FAIL

- **PASS** if 3 different L19 values coexist, each line's L21
  recomputes correctly, summary sums correctly.
- **FAIL** if prototype forces a single tax rate per SO (anti-pattern
  from failed POC). If the user cannot set different L19 per line =
  FAIL.

---

## 4. Scenario: 折扣率输入 (Discount rate)

| Field | Value |
|---|---|
| Scenario ID | `SCN-SO-04` |
| Title | Line-level discount via rate (10%) |
| Lines | 1 line, 10% discount |

### Mock data

```text
Lines:
  L1: ITEM-003 (伺服电机), Qty 4, Price (未税) 2300.00, L19=0.13
    L23 DiscountRate: 0.10
    L20 (pre-discount): 9200.00
    L25 (NetAmountExclTax, after discount): 8280.00 (= 9200 × 0.9)
    L21 (TaxAmount on L25): 1076.40
    L22 (GrossAmount): 9356.40
    L24 DiscountAmount: 920.00 (derived from L23 × L20)
```

### Expected UX

- User enters L23 = 0.10 (10%).
- L24 auto-fills to 920.00 (= L20 × L23).
- L24 becomes read-only (L23 is "active").
- L25 recomputes to 8280.00.
- L21 recomputes to 1076.40.
- L22 recomputes to 9356.40.
- Summary footer uses L25 (post-discount) for total net.

### PASS / FAIL

- **PASS** if L23 / L24 mutually exclusive input works, math correct.
- **FAIL** if both can be edited simultaneously, or L21 uses L20 (not
  L25) as base.

---

## 5. Scenario: 折扣额输入 (Discount amount)

| Field | Value |
|---|---|
| Scenario ID | `SCN-SO-05` |
| Title | Line-level discount via amount (200.00) |
| Lines | 1 line, 200 discount |

### Mock data

```text
Lines:
  L1: ITEM-004 (减速机), Qty 1, Price (未税) 1200.00, L19=0.13
    L24 DiscountAmount: 200.00
    L23 DiscountRate: 0.1667 (= 200/1200, derived, displayed as 16.67%)
    L20 (pre-discount): 1200.00
    L25 (NetAmountExclTax): 1000.00 (= 1200 - 200)
    L21 (TaxAmount): 130.00
    L22 (GrossAmount): 1130.00
```

### Expected UX

- User enters L24 = 200.00.
- L23 auto-fills to 0.1667 (16.67%).
- L23 becomes read-only.
- L25 = 1000.00, L21 = 130.00, L22 = 1130.00.

### PASS / FAIL

- **PASS** if L23 derives from L24, math correct.
- **FAIL** if user can type L24 and L23 simultaneously.

---

## 6. Scenario: 头默认仓库 + 行覆盖仓库 (Header default + line override)

| Field | Value |
|---|---|
| Scenario ID | `SCN-SO-06` |
| Title | H28 = WH-001 (主仓), L27 on line 2 = WH-002 (上海) |
| Lines | 2 lines, mixed warehouses |

### Mock data

```text
Header:
  H28 DefaultWarehouse: WH-001 (主仓)

Lines:
  L1: ITEM-001, Qty 100, L27=WH-001 (default, no change)
  L2: ITEM-002, Qty 5,   L27=WH-002 (override)
```

### Expected UX

- L1 added: L27 defaults to WH-001 (from H28).
- L2 added: L27 defaults to WH-001. User clicks L27 lookup, picks WH-002.
- L1 unchanged (L27 still WH-001).
- After Save, L27 is per-line, not overridden by H28 in summary.

### PASS / FAIL

- **PASS** if L2 override works and L1 unchanged.
- **FAIL** if L2 override causes L1 to also change to WH-002.

---

## 7. Scenario: 带 Location 的行 (Line with Location)

| Field | Value |
|---|---|
| Scenario ID | `SCN-SO-07` |
| Title | WH-001 (mandatory location policy) — line requires L28 |
| Lines | 1 line, WH-001 + LOC-001-01 |

### Mock data

```text
Header:
  H28 DefaultWarehouse: (none)

Lines:
  L1: ITEM-002, Qty 2, L27=WH-001, L28=LOC-001-01
```

### Expected UX

- User selects L27 = WH-001. L28 lookup opens, filtered to WH-001's
  locations (3 options).
- User picks L28 = LOC-001-01. Saved.
- Try save without L28: blocked (per WH-001 mandatory policy).
- Compare: L28 on WH-002 (not mandatory): L28 can be left empty.

### PASS / FAIL

- **PASS** if L28 lookup is filtered by L27's warehouse, L28 is
  mandatory under WH-001 policy.
- **FAIL** if L28 lookup shows all locations regardless of L27.

---

## 8. Scenario: 多商品、多单位、多交期 (Multi-item, multi-uom, multi-delivery)

| Field | Value |
|---|---|
| Scenario ID | `SCN-SO-08` |
| Title | 3 lines, different Uom / different delivery dates |
| Lines | 3 lines |

### Mock data

```text
Lines:
  L1: ITEM-001 (PCS), Qty 100, L26=2026-08-25, L27=WH-001
  L2: ITEM-005 (M, 米), Qty 50, L26=2026-09-01, L27=WH-001
  L3: ITEM-002 (PCS), Qty 10, L26=2026-09-15, L27=WH-002
```

### Expected UX

- 3 lines with different Uoms (PCS, M, PCS).
- Each L26 (line delivery date) is independent.
- Each L27 (line warehouse) is independent.
- L28 lookup on L3 (WH-002) shows only WH-002's locations.
- Summary footer: 合计数量 shows 3 separate quantities? Or 160 total?
  Per spec §2.5, 合计数量 is sum of L11. So 100 + 50 + 10 = 160
  (decimal-qty, no unit consideration at summary level for V1).

### PASS / FAIL

- **PASS** if 3 lines with different Uom / Delivery / Warehouse all
  render correctly; L28 lookup filtered by L27; summary sums L11.
- **FAIL** if L28 lookup shows all; if summary is missing.

---

## 9. Scenario: Draft 状态 (DocumentStatus=Draft)

| Field | Value |
|---|---|
| Scenario ID | `SCN-SO-09` |
| Title | Newly created SO, never submitted |
| State | `DocStatus=Draft / AppStatus=NotSubmitted / ExecStatus=NotStarted` |

### Mock data (saved as draft)

```text
SO-20260818-0099
  DocStatus: Draft
  AppStatus: NotSubmitted
  ExecStatus: NotStarted
  Customer: CUST-001
  Lines: 1 line, ITEM-001, Qty 50
```

### Expected UX

- Detail page: 3 status tags visible (grey, grey, grey).
- All header + line fields editable.
- Primary actions: 保存, 提交.
- Secondary actions: 复制, 打印, 导出, 删除.
- Tabs: 流程 (empty state), 附件 (empty), 下游 (empty), 源单
  (empty), 操作日志 (one entry: 创建), 打印.

### PASS / FAIL

- **PASS** if 3 status tags visible, all 3D, primary actions match
  spec §6.
- **FAIL** if status is single string, or actions mismatch.

---

## 10. Scenario: Pending Approval 状态

| Field | Value |
|---|---|
| Scenario ID | `SCN-SO-10` |
| Title | SO submitted, awaiting approval |
| State | `DocStatus=Active / AppStatus=Pending / ExecStatus=NotStarted` |

### Mock data (after Submit)

```text
SO-20260818-0098
  DocStatus: Active
  AppStatus: Pending
  ExecStatus: NotStarted
  SubmittedBy: EMP-001
  SubmittedAt: 2026-08-18 14:30
```

### Expected UX

- 3 status tags: Doc=Active (blue), App=Pending (yellow), Exec=NotStarted
  (grey).
- All header + line fields read-only.
- Primary actions: 审核通过, 驳回.
- Secondary actions: 撤回 (if current user = Submitter), 打印, 导出.
- Tabs: 流程 (active, shows "待 EMP-002 审批"), 操作日志 (Submit
  entry), 附件, 源单, 下游.
- No "保存" button (DocStatus ≠ Draft).

### PASS / FAIL

- **PASS** if 3D tags, action set per spec §6, Withdraw visible only for
  Submitter.
- **FAIL** if user can still edit fields, or actions don't match.

---

## 11. Scenario: Approved + Partial Execution 状态

| Field | Value |
|---|---|
| Scenario ID | `SCN-SO-11` |
| Title | SO approved, partial shipment generated |
| State | `DocStatus=Active / AppStatus=Approved / ExecStatus=Partial` |

### Mock data (after Approve + partial SH)

```text
SO-20260818-0097
  DocStatus: Active
  AppStatus: Approved
  ExecStatus: Partial
  Lines:
    L1: ITEM-002, Qty 10, ExecutedQuantity=3, OpenQuantity=7
    L2: ITEM-003, Qty 4,  ExecutedQuantity=0, OpenQuantity=4
  Downstream:
    SH-20260818-0002: 3 units of ITEM-002 shipped
```

### Expected UX

- 3 status tags: Doc=Active, App=Approved (green), Exec=Partial (yellow).
- All header + line fields read-only.
- L12 (ExecutedQuantity) and L13 (OpenQuantity) visible on lines,
  read-only.
- L15 (ReservedQuantity) = 10 (all approved and reserved).
- Primary action: 生成发货单 (for L2 not yet shipped).
- Secondary: 取消 (with reason), 关闭 (gated by Exec=Completed? not
  yet, so disabled), 打印, 导出.
- Tabs: 流程 (Approved node), 附件, 源单, 下游 (SH-20260818-0002
  listed), 操作日志 (Submit / Approve / SH-Generated entries).

### PASS / FAIL

- **PASS** if 3D tags, L12/L13/L15 visible and read-only, downstream
  SH listed, Close disabled (Exec ≠ Completed).
- **FAIL** if Close enabled prematurely, or L12/L13 editable.

---

## 12. Scenario: Closed / Cancelled 状态

### 12a. Closed (DEC-STATUS-001 closed = terminal)

```text
SO-20260818-0090
  DocStatus: Closed
  AppStatus: Approved
  ExecStatus: Completed
  ClosedAt: 2026-08-30
  ClosedBy: EMP-001
  CloseReason: "全部发货完成, 客户已确认"
```

Expected UX:
- 3 status tags all in terminal colors.
- All fields read-only.
- No primary actions.
- Secondary: 打印, 导出, 查看.
- 关闭 action not visible (already closed).

### 12b. Cancelled (with reason)

```text
SO-20260818-0089
  DocStatus: Cancelled
  AppStatus: Withdrawn
  ExecStatus: NotStarted
  CancelledAt: 2026-08-19
  CancelledBy: EMP-001
  CancelReason: "客户取消订单"
```

Expected UX:
- 3 status tags with Cancelled banner showing reason.
- All fields read-only.
- No primary actions.
- Secondary: 打印, 导出, 查看.
- A "复制" action is still available (per spec — copy creates new draft).

### PASS / FAIL

- **PASS** if 3D tags reflect Closed/Cancelled correctly, no
  primary actions, banner shows reason.
- **FAIL** if "Unapprove" or "Void" buttons appear (removed per
  DEC-STATUS-001).

---

## 13. Scenario: 客户地址 (Customer address)

| Field | Value |
|---|---|
| Scenario ID | `SCN-SO-13` |
| Title | SO with ship-to and bill-to different from customer |
| Customer | `CUST-001` (北京) |
| ShipTo | Different address (上海仓库发货) |
| BillTo | Different address (深圳开票) |

### Mock data

```text
Header:
  H7 Customer: CUST-001
  H10 InvoiceToBusinessPartnerId: CUST-001 (default) — optional BillTo
  H11 InvoiceToAddress: 深圳市南山区科技园 5 号楼 (different address)
  H12 ShipToBusinessPartnerId: CUST-001 (default) — optional ShipTo
  H13 ShipToAddress: 上海市浦东新区世纪大道 100 号 (different address)
```

### Expected UX

- H7, H12, H13 are independent.
- Default H12 = H7 (same customer). User can override.
- H10 = H7 by default. H11 = H7's default address; can override.
- Open spec H8, H10, H11, H12, H13 — these are V1.5? per evidence
  matrix; for prototype, show them as optional fields.

### PASS / FAIL

- **PASS** if H10/H11/H12/H13 are visible and independent.
- **FAIL** if not shown or if H10/H12 cannot differ from H7.

---

## 14. Scenario: 联系人 (Contact)

| Field | Value |
|---|---|
| Scenario ID | `SCN-SO-14` |
| Title | SO with customer contact selected |

### Mock data

```text
Header:
  H7 Customer: CUST-001
  H8 CustomerContact: CONTACT-001 (王经理, 13800000001)
```

### Expected UX

- H8 is lookup, filtered by `BusinessPartnerContactId` where
  `PartnerId=H7`.
- When H7 changes, H8 lookup refreshes (H8 may become invalid).

### PASS / FAIL

- **PASS** if H8 lookup filtered by H7 and refreshes on H7 change.
- **FAIL** if H8 shows all contacts regardless of H7.

---

## 15. Scenario: 附件 (Attachments)

| Field | Value |
|---|---|
| Scenario ID | `SCN-SO-15` |
| Title | SO with 2 mock attachments |

### Mock data

```text
SO-20260818-0095
  Attachments:
    1. 合同扫描件.pdf (2.3 MB) — 合同 PDF
    2. 技术协议.docx (480 KB) — 技术协议 Word 文档
```

### Expected UX

- Tab "附件" shows 2 files with size + filename.
- Click PDF: preview in modal.
- Click docx: download only.
- Upload button (mock) visible.
- File storage key pattern shown:
  `tenant/T-001/doc-type/SO/SO-20260818-0095/...`.

### PASS / FAIL

- **PASS** if tab works, preview for PDF, download for docx, key
  pattern shown.
- **FAIL** if tab missing or no preview.

---

## 16. Scenario: 来源单据 (Source document)

| Field | Value |
|---|---|
| Scenario ID | `SCN-SO-16` |
| Title | SO sourced from Quotation |

### Mock data

```text
SO-20260818-0094
  H36 SourceDocument: QT-20260818-0001 (Quotation)
  L3 SourceLineId: QT-20260818-0001.L1 / .L2
```

### Expected UX

- Tab "源单" with link to QT-20260818-0001.
- Click link: opens in **new tab** (DEC-UX-001 multi-tab).
- Source line mapping visible (which SO line came from which
  quotation line).
- Read-only (no edit on source).
- When source is missing, tab shows "暂无".

### PASS / FAIL

- **PASS** if source tab works, new tab opens, source line mapping
  visible.
- **FAIL** if source not shown or opens in same tab (loses list scroll).

---

## 17. Scenario: 下游发货单 (Downstream Shipment)

| Field | Value |
|---|---|
| Scenario ID | `SCN-SO-17` |
| Title | SO with generated Shipment |

### Mock data

```text
SO-20260818-0093
  Downstream:
    SH-20260818-0001 (Shipment) — partial (3 of 10 ITEM-002)
```

### Expected UX

- Tab "下游" with link to SH-20260818-0001.
- Click link: opens in **new tab** (DEC-UX-001 multi-tab).
- "生成发货单" button on detail when App=Approved.
- When downstream absent, tab shows "暂无".

### PASS / FAIL

- **PASS** if downstream tab works, new tab opens, button visible.
- **FAIL** if not shown.

---

## 18. Edge scenario: 数量=0 (rejected)

| Field | Value |
|---|---|
| Scenario ID | `SCN-SO-18` |
| Title | Try to save with Quantity=0 |

### Expected UX

- Click Save Draft with L11 = 0: see inline error "数量必须大于 0".
- L11 cell highlights red.
- Save blocked.

### PASS / FAIL

- **PASS** if L11=0 rejected, inline error shown, save blocked.
- **FAIL** if save succeeds or generic error.

---

## 19. Edge scenario: 单价<0 (rejected)

| Field | Value |
|---|---|
| Scenario ID | `SCN-SO-19` |
| Title | Try to save with UnitPrice < 0 |

### Expected UX

- L16 < 0: inline error "单价不能为负".
- Save blocked.

### PASS / FAIL

- **PASS** if L16 < 0 rejected.
- **FAIL** if accepted.

---

## 20. Edge scenario: 折扣率 ≥ 1 (rejected)

| Field | Value |
|---|---|
| Scenario ID | `SCN-SO-20` |
| Title | Try DiscountRate = 1.5 (150%) |

### Expected UX

- L23 ≥ 1.0: inline error "折扣率必须小于 100%".
- Save blocked.

### PASS / FAIL

- **PASS** if rejected.
- **FAIL** if accepted.

---

## 21. Edge scenario: 单据号重复 (race condition simulation)

| Field | Value |
|---|---|
| Scenario ID | `SCN-SO-21` |
| Title | Simulate 2 users opening same SO, both editing |

### Mock flow

- User A opens SO-20260818-0080 in tab 1.
- User B opens same SO in tab 2.
- User A saves (version N → N+1).
- User B tries to save (stale version N).
- User B sees: "数据已被其他用户更新, 请刷新后重试" with Refresh
  button.

### Expected UX

- Per ERP-VIS-001 §6 wording.
- Refresh button (not just "OK" or "Close").
- Save blocked.

### PASS / FAIL

- **PASS** if optimistic concurrency error shows exact wording + Refresh.
- **FAIL** if save silently overwrites or generic error.

---

## 22. Cross-scenario aggregate

| Scenario | Status combos covered | Pricing scenarios | Discount | Warehouse/Location | Action coverage |
|---|---|---|---|---|---|
| 1-3 | Draft | ✓ 含税+未税+混税 | — | — | Save |
| 4-5 | Draft | — | 率/额 | — | Save |
| 6-7 | Draft | — | — | 头默认+行覆盖, Location 必填 | Save |
| 8 | Draft | — | — | 多 Uom/多仓 | Save |
| 9 | Draft | — | — | — | 保存+提交 visible |
| 10 | Active+Pending | — | — | — | Approve/Reject/Withdraw |
| 11 | Active+Approved+Partial | — | — | — | GenerateShipment |
| 12a | Closed (terminal) | — | — | — | (none primary) |
| 12b | Cancelled (terminal) | — | — | — | Copy (still allowed) |
| 13-17 | Various | — | — | — | Address/Contact/Attachment/Source/Downstream |
| 18-21 | Validation edges | — | — | — | Inline errors + version conflict |

**Coverage rule**: TRAE should make all 21 scenarios possible in the
mock data. If the prototype cannot produce a scenario's expected state
when starting from this mock data, that scenario fails.

---

## 23. Operator quick-start

1. Open prototype, see list of SOs in various states.
2. Use these mock data as the test corpus.
3. For each scenario above, perform the steps.
4. Mark PASS / FAIL.
5. Aggregate per `G1B1_HARD_FAIL_CHECKLIST.md`.
6. If any FAIL, do NOT grant `SALES_ORDER_UX_APPROVED`.
