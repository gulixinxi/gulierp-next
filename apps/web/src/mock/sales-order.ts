// Mock Data for SalesOrder UX Prototype
// zh-CN business expressions, all data is local mock — no API calls.
import type {
  SalesOrder,
  SalesOrderLine,
  CustomerOption,
  ContactOption,
  EmployeeOption,
  WarehouseOption,
  LocationOption,
  ItemOption,
  PaymentTermOption,
  CurrencyOption,
  TaxRateOption,
  ListColumnConfig,
  LineColumnConfig
} from '../types/sales-order';

// ===== MDM Reference Data =====
export const customers: CustomerOption[] = [
  { id: 'C0001', code: 'C0001', name: '上海宏盛电子科技有限公司', isActive: true, defaultPriceMode: 'TaxExclusive', defaultTaxRate: 0.13, defaultPaymentTermCode: 'M30', defaultCurrencyCode: 'CNY', salesPersonId: 'E001', shipToAddress: '上海市浦东新区张江高科技园区科苑路 88 号', contactName: '王志强' },
  { id: 'C0002', code: 'C0002', name: '深圳市新越精密制造有限公司', isActive: true, defaultPriceMode: 'TaxInclusive', defaultTaxRate: 0.13, defaultPaymentTermCode: 'M60', defaultCurrencyCode: 'CNY', salesPersonId: 'E002', shipToAddress: '深圳市宝安区西乡街道固戍社区一路 12 号', contactName: '李慧敏' },
  { id: 'C0003', code: 'C0003', name: '苏州瑞泰医疗器械有限公司', isActive: true, defaultPriceMode: 'TaxExclusive', defaultTaxRate: 0.06, defaultPaymentTermCode: 'M0', defaultCurrencyCode: 'CNY', salesPersonId: 'E001', shipToAddress: '苏州市工业园区星湖街 218 号', contactName: '陈昊' },
  { id: 'C0004', code: 'C0004', name: '北京天成自动化设备有限公司', isActive: true, defaultPriceMode: 'TaxInclusive', defaultTaxRate: 0.13, defaultPaymentTermCode: 'T30', defaultCurrencyCode: 'CNY', salesPersonId: 'E003', shipToAddress: '北京市昌平区回龙观东大街 101 号', contactName: '赵静' },
  { id: 'C0005', code: 'C0005', name: '广州明华食品包装有限公司', isActive: false, defaultPriceMode: 'TaxExclusive', defaultTaxRate: 0.13, defaultPaymentTermCode: 'M30', defaultCurrencyCode: 'CNY', salesPersonId: 'E002', shipToAddress: '广州市黄埔区开发大道 33 号', contactName: '黄丽萍' }
];

export const contacts: ContactOption[] = [
  { id: 'CT001', customerId: 'C0001', name: '王志强', phone: '138-0210-0001', email: 'wangzq@hongsheng.cn' },
  { id: 'CT002', customerId: 'C0001', name: '周敏', phone: '139-0210-0002', email: 'zhoumin@hongsheng.cn' },
  { id: 'CT003', customerId: 'C0002', name: '李慧敏', phone: '138-0755-0003', email: 'lihm@xinyue.cn' },
  { id: 'CT004', customerId: 'C0003', name: '陈昊', phone: '139-0512-0004', email: 'chenhao@ruitai.cn' },
  { id: 'CT005', customerId: 'C0004', name: '赵静', phone: '137-0100-0005', email: 'zhaojing@tiancheng.cn' }
];

export const employees: EmployeeOption[] = [
  { id: 'E001', code: 'E001', name: '张磊', role: 'Sales', orgId: 'O001', orgName: '华东销售部' },
  { id: 'E002', code: 'E002', name: '刘洋', role: 'Sales', orgId: 'O002', orgName: '华南销售部' },
  { id: 'E003', code: 'E003', name: '孙浩然', role: 'Sales', orgId: 'O003', orgName: '华北销售部' },
  { id: 'E004', code: 'E004', name: '吴海', role: 'SalesManager', orgId: 'O001', orgName: '华东销售部' },
  { id: 'E005', code: 'E005', name: '周强', role: 'SalesManager', orgId: 'O002', orgName: '华南销售部' }
];

export const warehouses: WarehouseOption[] = [
  { id: 'W001', code: 'W001', name: '上海主仓', locationMandatory: false },
  { id: 'W002', code: 'W002', name: '深圳南方仓', locationMandatory: true },
  { id: 'W003', code: 'W003', name: '北京中央仓', locationMandatory: false }
];

export const locations: LocationOption[] = [
  { id: 'L001', warehouseId: 'W001', code: 'A-01', name: 'A 区 1 排' },
  { id: 'L002', warehouseId: 'W001', code: 'A-02', name: 'A 区 2 排' },
  { id: 'L003', warehouseId: 'W001', code: 'B-01', name: 'B 区 1 排' },
  { id: 'L004', warehouseId: 'W002', code: 'C-01', name: 'C 区 1 排' },
  { id: 'L005', warehouseId: 'W002', code: 'C-02', name: 'C 区 2 排' },
  { id: 'L006', warehouseId: 'W003', code: 'D-01', name: 'D 区 1 排' }
];

export const items: ItemOption[] = [
  { id: 'I0001', code: 'I0001', name: '高精度轴承 6204-ZZ', spec: '20x47x14mm / 钢制 / 双面密封', uomId: 'U0001', uomName: '套', defaultSalesPriceExclTax: 28.00, defaultSalesPriceInclTax: 31.64, defaultTaxRate: 0.13, isActive: true, lotEnabled: false },
  { id: 'I0002', code: 'I0002', name: '304 不锈钢板材 1.5mm', spec: '1219x2438mm / 304 / 2B 表面', uomId: 'U0002', uomName: '张', defaultSalesPriceExclTax: 380.00, defaultSalesPriceInclTax: 429.40, defaultTaxRate: 0.13, isActive: true, lotEnabled: true },
  { id: 'I0003', code: 'I0003', name: '医用级硅胶管 Φ6×1.5', spec: '内径 6mm / 壁厚 1.5mm / 食品级', uomId: 'U0003', uomName: '米', defaultSalesPriceExclTax: 8.50, defaultSalesPriceInclTax: 9.01, defaultTaxRate: 0.06, isActive: true, lotEnabled: true },
  { id: 'I0004', code: 'I0004', name: '伺服电机 750W', spec: '750W / 220V / 3000rpm / 增量编码器', uomId: 'U0001', uomName: '台', defaultSalesPriceExclTax: 1850.00, defaultSalesPriceInclTax: 2090.50, defaultTaxRate: 0.13, isActive: true, lotEnabled: false },
  { id: 'I0005', code: 'I0005', name: '铝型材 40×40', spec: '40x40mm / 6 系 / 阳极氧化 / 黑色', uomId: 'U0004', uomName: '支', defaultSalesPriceExclTax: 65.00, defaultSalesPriceInclTax: 73.45, defaultTaxRate: 0.13, isActive: true, lotEnabled: false },
  { id: 'I0006', code: 'I0006', name: 'PET 包装膜 50μm', spec: '宽 1200mm / 厚 50μm / 食品级', uomId: 'U0005', uomName: '卷', defaultSalesPriceExclTax: 240.00, defaultSalesPriceInclTax: 254.40, defaultTaxRate: 0.06, isActive: true, lotEnabled: true },
  { id: 'I0007', code: 'I0007', name: '工业级 PLC 模块 16DI', spec: '16 路数字输入 / 24VDC / Modbus', uomId: 'U0001', uomName: '件', defaultSalesPriceExclTax: 1280.00, defaultSalesPriceInclTax: 1446.40, defaultTaxRate: 0.13, isActive: false, lotEnabled: false }
];

export const uoms = [
  { id: 'U0001', code: 'PCS', name: '套', decimals: 0 },
  { id: 'U0002', code: 'SHT', name: '张', decimals: 0 },
  { id: 'U0003', code: 'MTR', name: '米', decimals: 3 },
  { id: 'U0004', code: 'PCS', name: '支', decimals: 0 },
  { id: 'U0005', code: 'ROL', name: '卷', decimals: 3 }
];

export const paymentTerms: PaymentTermOption[] = [
  { code: 'M0', name: '款到发货', days: 0 },
  { code: 'M15', name: '15 天账期', days: 15 },
  { code: 'M30', name: '30 天账期', days: 30 },
  { code: 'M60', name: '60 天账期', days: 60 },
  { code: 'T30', name: '票到 30 天', days: 30 }
];

export const currencies: CurrencyOption[] = [
  { code: 'CNY', name: '人民币', symbol: '¥', rate: 1 },
  { code: 'USD', name: '美元', symbol: '$', rate: 7.18 },
  { code: 'EUR', name: '欧元', symbol: '€', rate: 7.85 }
];

export const taxRates: TaxRateOption[] = [
  { code: 'T13', name: '增值税 13%', rate: 0.13 },
  { code: 'T9', name: '增值税 9%', rate: 0.09 },
  { code: 'T6', name: '增值税 6%', rate: 0.06 },
  { code: 'T0', name: '增值税 0%', rate: 0 }
];

export const deliveryMethods = ['自提', '送货上门', '快递', '物流专线'];

// ===== Pricing Engine (matches SALES_ORDER_BUSINESS_SPEC_V1.md §4) =====
// F1: AmountExclTax = Qty * UnitPriceExclTax * (1 - DiscountRate) - DiscountAmount
// F2: TaxAmount = AmountExclTax * TaxRate
// F3: AmountInclTax = AmountExclTax + TaxAmount
// L17 (incl price) = L16 + L16*taxRate  (or L16 = L17 / (1+taxRate))
// L23 (rate) and L24 (amount) mutually exclusive input

export function round2(v: number): number {
  // HALF_EVEN, 2dp
  const r = Math.round((v + Number.EPSILON) * 100) / 100;
  return r;
}
export function round4(v: number): number {
  return Math.round((v + Number.EPSILON) * 10000) / 10000;
}

export function derivePriceFromExcl(excl: number, taxRate: number): number {
  return round2(excl * (1 + taxRate));
}
export function derivePriceFromIncl(incl: number, taxRate: number): number {
  if (taxRate === 0) return incl;
  return round4(incl / (1 + taxRate));
}

export interface LineComputationInput {
  quantity: number;
  unitPriceExclTax: number;
  unitPriceInclTax: number;
  lineTaxRate: number;
  discountRate: number;
  discountAmount: number;
  linePriceMode: PriceModeLike;
  userEdited: 'excl' | 'incl' | 'none';
}

type PriceModeLike = 'TaxInclusive' | 'TaxExclusive';

// Pure recompute of all derived fields on a line
export function recomputeLine(line: SalesOrderLine, userEdited: 'excl' | 'incl' | 'none' = 'none'): void {
  const tr = line.lineTaxRate || 0;
  // Ensure excl & incl consistency
  if (userEdited === 'excl') {
    line.unitPriceInclTax = derivePriceFromExcl(line.unitPriceExclTax, tr);
  } else if (userEdited === 'incl') {
    line.unitPriceExclTax = derivePriceFromIncl(line.unitPriceInclTax, tr);
  } else {
    // keep both — or default-fill
    if (line.linePriceMode === 'TaxExclusive') {
      line.unitPriceInclTax = derivePriceFromExcl(line.unitPriceExclTax, tr);
    } else {
      line.unitPriceExclTax = derivePriceFromIncl(line.unitPriceInclTax, tr);
    }
  }
  // Mutually exclusive discount: if rate > 0, amount derived from rate; else keep amount
  const qty = line.quantity || 0;
  const grossExcl = round4(qty * line.unitPriceExclTax);
  if (line.discountRate > 0 && line.discountAmount === 0) {
    line.discountAmount = round2(grossExcl * line.discountRate);
  } else if (line.discountAmount > 0 && line.discountRate === 0) {
    // derive rate from amount (display only)
    line.discountRate = grossExcl > 0 ? round4(line.discountAmount / grossExcl) : 0;
  } else if (line.discountRate > 0 && line.discountAmount > 0) {
    // user just edited rate? -> amount takes precedence per DEC-SO-002 mutual exclusivity
    // For UX demo: rate is the primary input — recompute amount
    line.discountAmount = round2(grossExcl * line.discountRate);
  }
  // F1: amountExcl = qty * priceExcl * (1 - rate) - amount
  line.amountExclTax = round4(qty * line.unitPriceExclTax * (1 - line.discountRate) - line.discountAmount);
  if (line.amountExclTax < 0) line.amountExclTax = 0;
  // F2
  line.taxAmount = round2(line.amountExclTax * tr);
  // F3
  line.amountInclTax = round2(line.amountExclTax + line.taxAmount);
  line.netAmountExclTax = line.amountExclTax;
  line.openQuantity = round4(line.quantity - line.executedQuantity - line.cancelledQuantity);
  if (line.openQuantity < 0) line.openQuantity = 0;
}

export function recomputeHeader(so: SalesOrder): void {
  so.totalQuantity = round4(so.lines.reduce((s, l) => s + (l.quantity || 0), 0));
  so.totalAmountExclTax = round2(so.lines.reduce((s, l) => s + l.amountExclTax, 0));
  so.totalTaxAmount = round2(so.lines.reduce((s, l) => s + l.taxAmount, 0));
  so.totalAmountInclTax = round2(so.lines.reduce((s, l) => s + l.amountInclTax, 0));
  so.totalDiscountAmount = round2(so.lines.reduce((s, l) => s + (l.discountAmount || 0), 0));
}

// ===== Helper factories =====
let _seq = 4;
export function nextSalesOrderNo(orderDate: string): string {
  const d = orderDate.replace(/-/g, '').slice(0, 8);
  _seq += 1;
  return `SO-${d}-${String(_seq).padStart(4, '0')}`;
}

export function makeBlankLine(lineNo: number, defaults: { warehouseId?: string; warehouseName?: string; deliveryDate?: string; taxRate: number; priceMode: PriceModeLike; }): SalesOrderLine {
  const line: SalesOrderLine = {
    lineNo,
    itemId: '',
    itemCode: '',
    itemName: '',
    itemSpec: '',
    uomId: '',
    uomName: '',
    quantity: 1,
    executedQuantity: 0,
    openQuantity: 1,
    reservedQuantity: 0,
    cancelledQuantity: 0,
    linePriceMode: defaults.priceMode,
    unitPriceExclTax: 0,
    unitPriceInclTax: 0,
    lineTaxRate: defaults.taxRate,
    discountRate: 0,
    discountAmount: 0,
    amountExclTax: 0,
    taxAmount: 0,
    amountInclTax: 0,
    netAmountExclTax: 0,
    lineDeliveryDate: defaults.deliveryDate,
    warehouseId: defaults.warehouseId,
    warehouseName: defaults.warehouseName,
    locationId: '',
    locationName: '',
    lotRequired: false,
    warehouseSource: 'inherited', // G1B-1R7: new lines inherit Header DefaultWarehouse
    memo: '',
    lineStatus: 'Open'
  };
  recomputeLine(line);
  return line;
}

// ===== Seed Mock Orders =====
const today = '2026-08-18';

function mkLine(i: number, itemIdx: number, qty: number, dRate = 0): SalesOrderLine {
  const item = items[itemIdx];
  const line: SalesOrderLine = {
    lineNo: i,
    itemId: item.id,
    itemCode: item.code,
    itemName: item.name,
    itemSpec: item.spec,
    uomId: item.uomId,
    uomName: item.uomName,
    quantity: qty,
    executedQuantity: 0,
    openQuantity: qty,
    reservedQuantity: 0,
    cancelledQuantity: 0,
    linePriceMode: 'TaxExclusive',
    unitPriceExclTax: item.defaultSalesPriceExclTax,
    unitPriceInclTax: item.defaultSalesPriceInclTax,
    lineTaxRate: item.defaultTaxRate,
    discountRate: dRate,
    discountAmount: 0,
    amountExclTax: 0,
    taxAmount: 0,
    amountInclTax: 0,
    netAmountExclTax: 0,
    warehouseId: 'W001',
    warehouseName: '上海主仓',
    locationId: 'L001',
    locationName: 'A 区 1 排',
    lotRequired: item.lotEnabled,
    warehouseSource: 'inherited',
    lineDeliveryDate: '2026-08-28',
    lineStatus: 'Open'
  };
  recomputeLine(line);
  return line;
}

function makeOrder(
  id: string,
  no: string,
  date: string,
  custIdx: number,
  empIdx: number,
  whIdx: number,
  ds: SalesOrder['documentStatus'],
  as: SalesOrder['approvalStatus'],
  es: SalesOrder['executionStatus'],
  lines: SalesOrderLine[]
): SalesOrder {
  const cust = customers[custIdx];
  const emp = employees[empIdx];
  const wh = warehouses[whIdx];
  const so: SalesOrder = {
    id, salesOrderNo: no, orderDate: date, requestedDeliveryDate: '2026-08-28',
    customerId: cust.id, customerCode: cust.code, customerName: cust.name,
    contactId: 'CT001', contactName: cust.contactName, customerOrderNo: `PO-${date.replace(/-/g, '')}-${id}`,
    shipToAddress: cust.shipToAddress,
    salesPersonId: emp.id, salesPersonName: emp.name, salesOrgId: emp.orgId, salesOrgName: emp.orgName,
    companyId: 'CMP001', companyName: '谷粒 ERP 有限公司',
    currencyCode: cust.defaultCurrencyCode, exchangeRate: 1,
    defaultPriceMode: cust.defaultPriceMode, defaultTaxRate: cust.defaultTaxRate,
    paymentTermCode: cust.defaultPaymentTermCode, settlementMethod: '转账',
    deliveryMethod: '送货上门', defaultWarehouseId: wh.id, defaultWarehouseName: wh.name,
    carrierId: '', trackingNo: '', memo: '', documentType: 'Normal',
    documentStatus: ds, approvalStatus: as, executionStatus: es,
    createdBy: emp.name, createdAt: `${date} 09:12:33`, updatedAt: `${date} 14:08:21`,
    approvedBy: as === 'Approved' ? '吴海' : undefined,
    approvedAt: as === 'Approved' ? `${date} 16:30:00` : undefined,
    concurrencyVersion: 1,
    lines, attachments: [], approvalHistory: [], auditLog: [], downstream: [],
    totalQuantity: 0, totalAmountExclTax: 0, totalTaxAmount: 0, totalAmountInclTax: 0, totalDiscountAmount: 0
  };
  recomputeHeader(so);
  return so;
}

const seedOrders: SalesOrder[] = [
  makeOrder('1', 'SO-20260815-0001', '2026-08-15', 0, 0, 0, 'Active', 'Approved', 'Partial', [
    mkLine(1, 0, 100),
    mkLine(2, 3, 2, 0.05),
    mkLine(3, 4, 30)
  ]),
  makeOrder('2', 'SO-20260816-0002', '2026-08-16', 1, 1, 1, 'Active', 'Pending', 'NotStarted', [
    mkLine(1, 1, 5),
    mkLine(2, 5, 20, 0.1)
  ]),
  makeOrder('3', 'SO-20260817-0003', '2026-08-17', 2, 0, 0, 'Draft', 'NotSubmitted', 'NotStarted', [
    mkLine(1, 2, 200),
    mkLine(2, 5, 10)
  ]),
  makeOrder('4', 'SO-20260814-0004', '2026-08-14', 3, 2, 2, 'Active', 'Rejected', 'NotStarted', [
    mkLine(1, 3, 50)
  ]),
  makeOrder('5', 'SO-20260813-0005', '2026-08-13', 0, 0, 0, 'Closed', 'Approved', 'Completed', [
    mkLine(1, 0, 200),
    mkLine(2, 1, 10)
  ]),
  makeOrder('6', 'SO-20260812-0006', '2026-08-12', 1, 1, 1, 'Cancelled', 'Withdrawn', 'NotStarted', [
    mkLine(1, 5, 50)
  ]),
  makeOrder('7', 'SO-20260810-0007', '2026-08-10', 2, 0, 0, 'Active', 'Approved', 'Completed', [
    mkLine(1, 2, 100),
    mkLine(2, 5, 30)
  ]),
  makeOrder('8', 'SO-20260818-0008', today, 0, 0, 0, 'Draft', 'NotSubmitted', 'NotStarted', [
    mkLine(1, 3, 1)
  ])
];

// Enrich with downstream + audit history based on state
seedOrders[0].downstream = [
  { id: 'SH1', docType: 'Shipment', docTypeLabel: '发货单', no: 'SH-20260816-0001', date: '2026-08-16', amount: 4180.0, status: '已发货' }
];
seedOrders[0].lines[0].executedQuantity = 50;
seedOrders[0].lines[0].openQuantity = 50;
seedOrders[0].approvalHistory = [
  { id: 'A1', step: '销售经理审批', approver: '吴海', approverRole: 'SalesManager', status: 'Approved', at: '2026-08-15 16:30:00', comment: '同意' }
];
seedOrders[1].approvalHistory = [
  { id: 'A2', step: '销售经理审批', approver: '周强', approverRole: 'SalesManager', status: 'Pending', at: '2026-08-16 10:20:00' }
];
seedOrders[3].approvalHistory = [
  { id: 'A3', step: '销售经理审批', approver: '吴海', approverRole: 'SalesManager', status: 'Rejected', at: '2026-08-14 15:40:00', comment: '价格低于底价,请重新核价' }
];
seedOrders[0].attachments = [
  { id: 'F1', name: '客户采购订单 PO-HS-0815.pdf', size: 245678, uploadedAt: '2026-08-15 09:15', uploadedBy: '张磊' },
  { id: 'F2', name: '技术规格确认单.docx', size: 87234, uploadedAt: '2026-08-15 09:16', uploadedBy: '张磊' }
];

for (const so of seedOrders) {
  so.auditLog = [
    { id: `${so.id}-a1`, action: 'Create', actionLabel: '新建', actor: so.createdBy, actorRole: 'Sales', at: so.createdAt },
    ...(so.approvalStatus !== 'NotSubmitted' ? [{ id: `${so.id}-a2`, action: 'Submit', actionLabel: '提交', actor: so.createdBy, actorRole: 'Sales', at: `${so.orderDate} 10:00:00` }] : []),
    ...(so.approvalStatus === 'Approved' ? [{ id: `${so.id}-a3`, action: 'Approve', actionLabel: '审核通过', actor: so.approvedBy || '吴海', actorRole: 'SalesManager', at: so.approvedAt || so.updatedAt }] : []),
    ...(so.approvalStatus === 'Rejected' ? [{ id: `${so.id}-a3`, action: 'Reject', actionLabel: '驳回', actor: '吴海', actorRole: 'SalesManager', at: so.updatedAt, reason: '价格低于底价,请重新核价' }] : []),
    ...(so.approvalStatus === 'Withdrawn' ? [{ id: `${so.id}-a3`, action: 'Withdraw', actionLabel: '撤回', actor: so.createdBy, actorRole: 'Sales', at: so.updatedAt, reason: '客户取消' }] : []),
    ...(so.documentStatus === 'Closed' ? [{ id: `${so.id}-a4`, action: 'Close', actionLabel: '关闭', actor: '张磊', actorRole: 'Sales', at: so.updatedAt, reason: '全部发货完成' }] : []),
    ...(so.documentStatus === 'Cancelled' ? [{ id: `${so.id}-a4`, action: 'Cancel', actionLabel: '取消', actor: so.createdBy, actorRole: 'Sales', at: so.updatedAt, reason: '客户取消订单' }] : [])
  ];
}

export const seedSalesOrders: SalesOrder[] = seedOrders;

// ===== List default columns (DEC-UX-001: per user, mock defaults) =====
export const defaultListColumns: ListColumnConfig[] = [
  { prop: 'salesOrderNo', label: '单据号', width: 160, visible: true, fixed: 'left' },
  { prop: 'orderDate', label: '订单日期', width: 110, visible: true },
  { prop: 'customerName', label: '客户', width: 240, visible: true },
  { prop: 'salesPersonName', label: '销售员', width: 90, visible: true },
  { prop: 'totalAmountInclTax', label: '价税合计', width: 130, visible: true },
  { prop: 'totalAmountExclTax', label: '未税金额', width: 130, visible: false },
  { prop: 'totalTaxAmount', label: '税额', width: 110, visible: false },
  { prop: 'documentStatus', label: '单据状态', width: 100, visible: true },
  { prop: 'approvalStatus', label: '审批状态', width: 100, visible: true },
  { prop: 'executionStatus', label: '执行状态', width: 100, visible: true },
  { prop: 'paymentTermCode', label: '付款条件', width: 110, visible: false },
  { prop: 'currencyCode', label: '币种', width: 80, visible: false },
  { prop: 'requestedDeliveryDate', label: '交货日期', width: 110, visible: false },
  { prop: 'createdBy', label: '创建人', width: 90, visible: false },
  { prop: 'createdAt', label: '创建时间', width: 160, visible: true },
  { prop: 'updatedAt', label: '更新时间', width: 160, visible: false }
];

export const defaultLineColumns: LineColumnConfig[] = [
  { prop: 'lineNo', label: '行号', width: 50, visible: true, core: true, fixed: 'left', aggregate: 'none' },
  { prop: 'item', label: '商品', width: 220, visible: true, core: true, fixed: 'left', aggregate: 'none' },
  { prop: 'itemSpec', label: '规格型号', width: 220, visible: true, core: true, aggregate: 'none' },
  { prop: 'uomName', label: '单位', width: 70, visible: true, core: true, aggregate: 'none' },
  { prop: 'quantity', label: '数量', width: 100, visible: true, core: true, aggregate: 'SUM' },
  { prop: 'linePriceMode', label: '行价格模式', width: 110, visible: true, core: false, aggregate: 'none' },
  { prop: 'unitPriceExclTax', label: '未税单价', width: 120, visible: true, core: true, aggregate: 'none' },
  { prop: 'unitPriceInclTax', label: '含税单价', width: 120, visible: true, core: true, aggregate: 'none' },
  { prop: 'lineTaxRate', label: '行税率', width: 100, visible: true, core: false, aggregate: 'none' },
  { prop: 'discountRate', label: '折扣率', width: 90, visible: true, core: false, aggregate: 'none' },
  { prop: 'discountAmount', label: '折扣额', width: 100, visible: true, core: false, aggregate: 'SUM' },
  { prop: 'amountExclTax', label: '未税金额', width: 110, visible: true, core: true, aggregate: 'SUM' },
  { prop: 'taxAmount', label: '税额', width: 100, visible: true, core: true, aggregate: 'SUM' },
  { prop: 'amountInclTax', label: '价税合计', width: 120, visible: true, core: true, aggregate: 'SUM' },
  { prop: 'warehouseName', label: '仓库', width: 120, visible: true, core: false, aggregate: 'none' },
  { prop: 'locationName', label: '库位', width: 110, visible: true, core: false, aggregate: 'none' },
  { prop: 'lineDeliveryDate', label: '交期', width: 120, visible: true, core: false, aggregate: 'none' },
  { prop: 'executedQuantity', label: '已执行', width: 80, visible: true, core: false, aggregate: 'none' },
  { prop: 'openQuantity', label: '未执行', width: 80, visible: true, core: false, aggregate: 'none' },
  { prop: 'memo', label: '备注', width: 140, visible: true, core: false, fixed: 'right', aggregate: 'none' }
];

export const currentUser = {
  id: 'E004',
  name: '吴海',
  role: 'SalesManager' as const,
  orgId: 'O001',
  orgName: '华东销售部'
};
