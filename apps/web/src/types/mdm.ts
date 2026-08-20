/**
 * MDM Type Definitions — Static UX Prototype
 *
 * NOTE: These types represent UI-level contracts only.
 * Backend ID strategy and DB schema are NOT frozen (Codex pending ID Strategy Final Decision).
 * Field names follow current business spec evidence; do NOT treat as final API contract.
 */

// ============================================================
// Shared
// ============================================================

export type MasterDataStatus = 'active' | 'inactive' | 'draft';

export interface MasterDataStatusOption {
  value: MasterDataStatus;
  label: string;
  tagType: 'success' | 'info' | 'warning';
}

export const STATUS_OPTIONS: MasterDataStatusOption[] = [
  { value: 'active', label: '启用', tagType: 'success' },
  { value: 'inactive', label: '停用', tagType: 'info' },
  { value: 'draft', label: '草稿', tagType: 'warning' },
];

// ============================================================
// UOM — Unit of Measure
// ============================================================

export interface Uom {
  id: number;
  code: string;           // e.g. "PCS", "KG", "L"
  name: string;           // e.g. "个", "千克", "升"
  symbol: string;         // display symbol, e.g. "pc", "kg"
  decimalPlaces: number;  // precision for quantity display (0-4)
  status: MasterDataStatus;
  description?: string;
  createdAt: string;
  updatedAt: string;
}

export interface UomForm {
  code: string;
  name: string;
  symbol: string;
  decimalPlaces: number;
  status: MasterDataStatus;
  description?: string;
}

// ============================================================
// ItemCategory
// ============================================================

export interface ItemCategory {
  id: number;
  code: string;           // e.g. "RAW", "FIN", "PKG"
  name: string;           // e.g. "原材料", "成品", "包装材料"
  parentId: number | null; // hierarchy: null = root
  parentName?: string;     // denormalized for display
  status: MasterDataStatus;
  description?: string;
  level: number;           // 0 = root
  fullPath: string;        // e.g. "原材料 / 钢材 / 不锈钢"
  createdAt: string;
  updatedAt: string;
}

export interface ItemCategoryForm {
  code: string;
  name: string;
  parentId: number | null;
  status: MasterDataStatus;
  description?: string;
}

// ============================================================
// Item
// ============================================================

export type ItemType = 'goods' | 'service' | 'package';
export type InventoryMethod = 'FIFO' | 'LIFO' | 'WEIGHTED_AVG' | 'SPECIFIC';

export interface Item {
  id: number;
  code: string;                // e.g. "ITEM-0001"
  name: string;                // display name
  specification?: string;      // spec/型号
  categoryId: number | null;
  categoryName?: string;       // denormalized
  baseUomId: number | null;
  baseUomName?: string;       // denormalized
  itemType: ItemType;
  status: MasterDataStatus;
  // Inventory-related (only display fields with frozen evidence)
  inventoryMethod?: InventoryMethod;
  // Reserved sections — fields NOT frozen are omitted, not mocked with fake data
  // Future tabs: Sales, Purchase, Attributes
  description?: string;
  createdAt: string;
  updatedAt: string;
}

export interface ItemForm {
  code: string;
  name: string;
  specification?: string;
  categoryId: number | null;
  baseUomId: number | null;
  itemType: ItemType;
  status: MasterDataStatus;
  inventoryMethod?: InventoryMethod;
  description?: string;
}

// ============================================================
// List table column config (reused pattern from SalesOrder)
// ============================================================

export interface MdmColumnConfig {
  prop: string;
  label: string;
  width: number;
  visible: boolean;
  fixed?: 'left' | 'right' | false;
}

// ============================================================
// Semantic type markers (§8 — display contract only, no precision engine)
// ============================================================

export type SemanticType =
  | 'text'
  | 'code'
  | 'quantity'    // decimal with UOM-specific precision
  | 'enum'
  | 'status'
  | 'timestamp'
  | 'description';

export interface SemanticField {
  prop: string;
  label: string;
  type: SemanticType;
  precisionFromUom?: boolean; // for quantity fields: precision derived from UOM.decimalPlaces
}
