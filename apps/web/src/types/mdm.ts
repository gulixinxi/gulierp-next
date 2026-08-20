/**
 * MDM Type Definitions — Reconciled with MDM_000_MASTER_DATA_CONVENTION_V1 (FROZEN)
 *
 * Source of truth: docs/architecture/MDM_000_MASTER_DATA_CONVENTION_V1.md
 * These types match the frozen domain contract. UI-derived fields are clearly separated.
 */

// ============================================================
// Shared — Status (§4: ACTIVE / INACTIVE only in V1)
// ============================================================

export type MasterDataStatus = 'active' | 'inactive';

export interface MasterDataStatusOption {
  value: MasterDataStatus;
  label: string;
  tagType: 'success' | 'info' | 'warning';
}

export const STATUS_OPTIONS: MasterDataStatusOption[] = [
  { value: 'active', label: '启用', tagType: 'success' },
  { value: 'inactive', label: '停用', tagType: 'info' },
];

// ============================================================
// UOM — Frozen §6
// ============================================================

export type UomDimension = 'COUNT' | 'MASS' | 'LENGTH' | 'AREA' | 'VOLUME' | 'TIME';
export type UomKind = 'DISCRETE' | 'SI';

export const DIMENSION_OPTIONS: { value: UomDimension; label: string }[] = [
  { value: 'COUNT', label: '计数' },
  { value: 'MASS', label: '质量' },
  { value: 'LENGTH', label: '长度' },
  { value: 'AREA', label: '面积' },
  { value: 'VOLUME', label: '体积' },
  { value: 'TIME', label: '时间' },
];

export const KIND_OPTIONS: { value: UomKind; label: string }[] = [
  { value: 'DISCRETE', label: '离散' },
  { value: 'SI', label: 'SI 单位' },
];

export interface Uom {
  id: number;
  code: string;           // unique globally (SYSTEM scope)
  name: string;
  symbol: string | null;  // nullable: null for COUNT items (BENG/TAO/ZHANG)
  dimension: UomDimension;
  kind: UomKind;
  status: MasterDataStatus;
  description?: string;
  createdAt: string;
  updatedAt: string;
}

export interface UomForm {
  code: string;
  name: string;
  symbol: string | null;
  dimension: UomDimension;
  kind: UomKind;
  status: MasterDataStatus;
  description?: string;
}

// ============================================================
// ItemCategory — Frozen §7
// ============================================================

/**
 * Domain fields — what the API persists (matches Frozen §7 exactly).
 * level and fullPath are NOT stored; they are UI-derived.
 */
export interface ItemCategory {
  id: number;
  code: string;           // unique per (TenantId, Code)
  name: string;
  parentId: number | null; // optional, nullable; cycle detection enforced
  status: MasterDataStatus;
  description?: string;
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

/**
 * UI-derived fields — computed from ParentId chain at display time.
 * These are NOT persisted backend fields (Frozen §7: DERIVED, not stored).
 */
export interface ItemCategoryUiDerived {
  level: number;          // 0 = root; computed from ParentId chain
  fullPath: string;       // e.g. "原材料 / 钢材 / 不锈钢"; computed on read
  parentName?: string;    // denormalized for display convenience
}

/** Combined type for list/table display — domain + UI derived */
export type ItemCategoryListItem = ItemCategory & ItemCategoryUiDerived;

// ============================================================
// Item — Frozen §8
// ============================================================

/**
 * ItemNature frozen enum (V1 §8):
 * MATERIAL, SEMI_FINISHED, FINISHED_GOOD, SERVICE
 * PACKAGE is DEFERRED (no business evidence; V2 SKU variant)
 */
export type ItemNature = 'MATERIAL' | 'SEMI_FINISHED' | 'FINISHED_GOOD' | 'SERVICE';

export const ITEM_NATURE_OPTIONS: { value: ItemNature; label: string }[] = [
  { value: 'MATERIAL', label: '原材料' },
  { value: 'SEMI_FINISHED', label: '半成品' },
  { value: 'FINISHED_GOOD', label: '成品' },
  { value: 'SERVICE', label: '服务' },
];

export interface Item {
  id: number;
  code: string;                // unique per (TenantId, Code)
  name: string;
  specification?: string;      // DEV 商品表.规格
  categoryId: number | null;   // optional FK to ItemCategory
  baseUomId: number;           // required FK to Uom
  itemNature: ItemNature;     // replaces old itemType
  status: MasterDataStatus;
  description?: string;
  createdAt: string;
  updatedAt: string;
  // UI-denormalized convenience fields (NOT in frozen domain contract; resolved at display time)
  categoryName?: string;
  baseUomName?: string;
}

export interface ItemForm {
  code: string;
  name: string;
  specification?: string;
  categoryId: number | null;
  baseUomId: number | null;
  itemNature: ItemNature;
  status: MasterDataStatus;
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
// Semantic type markers (§16 — DEFERRED, display contract only)
// ============================================================

export type SemanticType =
  | 'text'
  | 'code'
  | 'quantity'    // precision DEFERRED to Business Semantic Precision Convention
  | 'enum'
  | 'status'
  | 'timestamp'
  | 'description';

export interface SemanticField {
  prop: string;
  label: string;
  type: SemanticType;
}
