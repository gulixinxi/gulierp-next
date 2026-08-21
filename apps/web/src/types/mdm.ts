/**
 * MDM Type Definitions — Reconciled with:
 *   1. MDM_000_MASTER_DATA_CONVENTION_V1 (FROZEN, UI semantic strings)
 *   2. TRAE_MDM_001_API_HANDOFF.md (real backend DTOs, integer enums)
 *
 * Split into two layers:
 *   - API DTOs  → integer enums, concurrencyVersion, PagedResult<T> (wire format)
 *   - UI Types  → string enums, UI-derived fields (level/fullPath/categoryName)
 *   - Converters in each API client map between the two layers.
 */

// ============================================================
// Shared — PagedResult (matches backend §5)
// ============================================================
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

// ============================================================
// Shared — Status (string: UI semantic | int: backend wire)
// ============================================================
export type MasterDataStatus = 'active' | 'inactive';
export type MasterDataStatusInt = 1 | 2;  // 1=ACTIVE 2=INACTIVE

export interface MasterDataStatusOption {
  value: MasterDataStatus;
  label: string;
  tagType: 'success' | 'info' | 'warning';
}
export const STATUS_OPTIONS: MasterDataStatusOption[] = [
  { value: 'active', label: '启用', tagType: 'success' },
  { value: 'inactive', label: '停用', tagType: 'info' },
];
export function statusIntToUi(v: MasterDataStatusInt | undefined | null): MasterDataStatus {
  return v === 2 ? 'inactive' : 'active';
}
export function statusUiToInt(v: MasterDataStatus): MasterDataStatusInt {
  return v === 'inactive' ? 2 : 1;
}

// ============================================================
// UOM — API DTOs (integer enums) + UI types (string enums)
// ============================================================
// Wire (Handoff §4.1): dimension 1=COUNT 2=MASS 3=LENGTH 4=AREA 5=VOLUME 6=TIME
//                     kind 1=DISCRETE 2=SI
export type UomDimensionInt = 1 | 2 | 3 | 4 | 5 | 6;
export type UomKindInt = 1 | 2;

export interface UomDto {
  id: number;
  code: string;
  name: string;
  symbol: string | null;
  dimension: UomDimensionInt;
  kind: UomKindInt;
  status: MasterDataStatusInt;
  description: string | null;
  createdAt: string;
  modifiedAt: string;
  concurrencyVersion: number;
}
export interface CreateUomRequest {
  code: string;
  name: string;
  symbol: string | null;
  dimension: UomDimensionInt;
  kind: UomKindInt;
  description: string | null;
}
export interface UpdateUomRequest {
  name: string;
  symbol: string | null;
  status: MasterDataStatusInt;
  description: string | null;
  expectedConcurrencyVersion: number;
}
export interface UomListParams {
  keyword?: string;
  status?: MasterDataStatusInt;
  dimension?: UomDimensionInt;
  page?: number;
  pageSize?: number;
}

// UI types (string enums — used by existing Vue components)
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
const DIM_INT_MAP: Record<UomDimensionInt, UomDimension> = { 1:'COUNT', 2:'MASS', 3:'LENGTH', 4:'AREA', 5:'VOLUME', 6:'TIME' };
const DIM_UI_MAP: Record<UomDimension, UomDimensionInt> = { COUNT:1, MASS:2, LENGTH:3, AREA:4, VOLUME:5, TIME:6 };
const KIND_INT_MAP: Record<UomKindInt, UomKind> = { 1:'DISCRETE', 2:'SI' };
const KIND_UI_MAP: Record<UomKind, UomKindInt> = { DISCRETE:1, SI:2 };
export function dimIntToUi(v: UomDimensionInt): UomDimension { return DIM_INT_MAP[v]; }
export function dimUiToInt(v: UomDimension): UomDimensionInt { return DIM_UI_MAP[v]; }
export function kindIntToUi(v: UomKindInt): UomKind { return KIND_INT_MAP[v]; }
export function kindUiToInt(v: UomKind): UomKindInt { return KIND_UI_MAP[v]; }

export interface Uom {
  id: number;
  code: string;
  name: string;
  symbol: string | null;
  dimension: UomDimension;
  kind: UomKind;
  status: MasterDataStatus;
  description?: string;
  createdAt: string;
  updatedAt: string;
  // Optimistic concurrency (hidden from tables; carried through edit cycle).
  // Optional ONLY because the pre-existing SalesOrder Mock (NOT touched in this Goal) omits it.
  // Real API DTO always carries a number; page code defaults to 0 when missing.
  concurrencyVersion?: number;
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
// ItemCategory — API DTOs + UI types
// ============================================================
export interface ItemCategoryDto {
  id: number;
  parentId: number | null;
  code: string;
  name: string;
  status: MasterDataStatusInt;
  description: string | null;
  createdAt: string;
  modifiedAt: string;
  concurrencyVersion: number;
}
export interface CreateItemCategoryRequest {
  code: string;
  name: string;
  parentId: number | null;
  description: string | null;
}
export interface UpdateItemCategoryRequest {
  name: string;
  parentId: number | null;
  status: MasterDataStatusInt;
  description: string | null;
  expectedConcurrencyVersion: number;
}
export interface ItemCategoryListParams {
  keyword?: string;
  status?: MasterDataStatusInt;
  parentId?: number;
  page?: number;
  pageSize?: number;
}

// UI types
export interface ItemCategory {
  id: number;
  code: string;
  name: string;
  parentId: number | null;
  status: MasterDataStatus;
  description?: string;
  createdAt: string;
  updatedAt: string;
  /** Optional-only-for-SalesOrder-Mock (real API always provides). Default 0 when missing. */
  concurrencyVersion?: number;
}
export interface ItemCategoryForm {
  code: string;
  name: string;
  parentId: number | null;
  status: MasterDataStatus;
  description?: string;
}
/** UI-derived fields — computed from ParentId chain at display time. NOT persisted. */
export interface ItemCategoryUiDerived {
  level: number;
  fullPath: string;
  parentName?: string;
}
export type ItemCategoryListItem = ItemCategory & ItemCategoryUiDerived;

// ============================================================
// Item — API DTOs (integer itemNature) + UI types (string enum)
// ============================================================
// Wire itemNature: 1=MATERIAL 2=SEMI_FINISHED 3=FINISHED_GOOD 4=SERVICE
export type ItemNatureInt = 1 | 2 | 3 | 4;

export interface ItemDto {
  id: number;
  code: string;
  name: string;
  specification: string | null;
  categoryId: number | null;
  baseUomId: number;
  itemNature: ItemNatureInt;
  status: MasterDataStatusInt;
  description: string | null;
  createdAt: string;
  modifiedAt: string;
  concurrencyVersion: number;
}
export interface CreateItemRequest {
  code: string;
  name: string;
  specification: string | null;
  categoryId: number | null;
  baseUomId: number;
  itemNature: ItemNatureInt;
  description: string | null;
}
export interface UpdateItemRequest {
  name: string;
  specification: string | null;
  categoryId: number | null;
  baseUomId: number;
  itemNature: ItemNatureInt;
  status: MasterDataStatusInt;
  description: string | null;
  expectedConcurrencyVersion: number;
}
export interface ItemListParams {
  keyword?: string;
  status?: MasterDataStatusInt;
  categoryId?: number;
  itemNature?: ItemNatureInt;
  page?: number;
  pageSize?: number;
}

// UI types
export type ItemNature = 'MATERIAL' | 'SEMI_FINISHED' | 'FINISHED_GOOD' | 'SERVICE';
export const ITEM_NATURE_OPTIONS: { value: ItemNature; label: string }[] = [
  { value: 'MATERIAL', label: '原材料' },
  { value: 'SEMI_FINISHED', label: '半成品' },
  { value: 'FINISHED_GOOD', label: '成品' },
  { value: 'SERVICE', label: '服务' },
];
const NATURE_INT_MAP: Record<ItemNatureInt, ItemNature> = { 1:'MATERIAL', 2:'SEMI_FINISHED', 3:'FINISHED_GOOD', 4:'SERVICE' };
const NATURE_UI_MAP: Record<ItemNature, ItemNatureInt> = { MATERIAL:1, SEMI_FINISHED:2, FINISHED_GOOD:3, SERVICE:4 };
export function natureIntToUi(v: ItemNatureInt): ItemNature { return NATURE_INT_MAP[v]; }
export function natureUiToInt(v: ItemNature): ItemNatureInt { return NATURE_UI_MAP[v]; }

export interface Item {
  id: number;
  code: string;
  name: string;
  specification?: string;
  categoryId: number | null;
  baseUomId: number;
  itemNature: ItemNature;
  status: MasterDataStatus;
  description?: string;
  createdAt: string;
  updatedAt: string;
  /** Optional-only-for-SalesOrder-Mock (real API always provides). Default 0 when missing. */
  concurrencyVersion?: number;
  // UI-denormalized (resolved locally from categoryId/baseUomId references)
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
// Shared helpers — ItemCategory hierarchy derivation (used by ItemCategory + Item pages)
// ============================================================
export function deriveCategoryHierarchy(raw: ItemCategory[]): ItemCategoryListItem[] {
  const map = new Map(raw.map(c => [c.id, c]));
  function getLevel(id: number | null): number {
    if (id == null) return 0;
    const p = map.get(id);
    return p ? getLevel(p.parentId) + 1 : 0;
  }
  function getPath(id: number | null): string {
    if (id == null) return '';
    const c = map.get(id);
    if (!c) return '';
    const pp = getPath(c.parentId);
    return pp ? `${pp} / ${c.name}` : c.name;
  }
  return raw.map(c => {
    const parent = c.parentId != null ? map.get(c.parentId) : undefined;
    return {
      ...c,
      level: getLevel(c.id),
      fullPath: getPath(c.id) || c.name,
      parentName: parent?.name,
    };
  });
}

// ============================================================
// List table column config (legacy marker)
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
export type SemanticType = 'text' | 'code' | 'quantity' | 'enum' | 'status' | 'timestamp' | 'description';
export interface SemanticField { prop: string; label: string; type: SemanticType; }
