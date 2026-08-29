/**
 * MDM Type Definitions — Reconciled with:
 *   1. MDM_000_MASTER_DATA_CONVENTION_V1 (FROZEN, UI semantic strings)
 *   2. TRAE_MDM_001_API_HANDOFF.md + TRAE_MDM_002_API_HANDOFF.md (real backend DTOs)
 *   3. API-CONTRACT-ID-001 — Snowflake / HiLo ID wire contract (IDs are JSON strings)
 *
 * Split into two layers:
 *   - API DTOs  → integer enums, concurrencyVersion, PagedResult<T> (wire format)
 *   - UI Types  → string enums, UI-derived fields (level/fullPath/categoryName)
 *   - Converters in each API client map between the two layers.
 *
 * API-CONTRACT-ID-001 ID-as-string contract:
 *   - Every snowflake / HiLo id field on the wire (id, parentId, categoryId,
 *     baseUomId, warehouseId, plantId) is a JSON STRING, not a number.
 *   - The backend serializes with `SnowflakeLongJsonConverter` (Kernel).
 *   - The JS client MUST treat them as `string`; NEVER do `Number(id)`,
 *     `parseInt(id)`, or unary `+id` on a snowflake — that re-introduces
 *     the JavaScript 2^53 precision loss this contract is designed to
 *     prevent.
 *   - `concurrencyVersion`, `page`, `pageSize`, `totalCount`, the enums
 *     (status / dimension / kind / role / type / itemNature), and the
 *     `expectedConcurrencyVersion` integer remain plain `number`.
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
  id: string;
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
  id: string;
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
  id: string;
  parentId: string | null;
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
  parentId: string | null;
  description: string | null;
}
export interface UpdateItemCategoryRequest {
  name: string;
  parentId: string | null;
  status: MasterDataStatusInt;
  description: string | null;
  expectedConcurrencyVersion: number;
}
export interface ItemCategoryListParams {
  keyword?: string;
  status?: MasterDataStatusInt;
  parentId?: string;
  page?: number;
  pageSize?: number;
}

// UI types
export interface ItemCategory {
  id: string;
  code: string;
  name: string;
  parentId: string | null;
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
  parentId: string | null;
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
  id: string;
  code: string;
  name: string;
  specification: string | null;
  categoryId: string | null;
  baseUomId: string;
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
  categoryId: string | null;
  baseUomId: string;
  itemNature: ItemNatureInt;
  description: string | null;
}
export interface UpdateItemRequest {
  name: string;
  specification: string | null;
  categoryId: string | null;
  baseUomId: string;
  itemNature: ItemNatureInt;
  status: MasterDataStatusInt;
  description: string | null;
  expectedConcurrencyVersion: number;
}
export interface ItemListParams {
  keyword?: string;
  status?: MasterDataStatusInt;
  categoryId?: string;
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
  id: string;
  code: string;
  name: string;
  specification?: string;
  categoryId: string | null;
  baseUomId: string;
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
  categoryId: string | null;
  baseUomId: string | null;
  itemNature: ItemNature;
  status: MasterDataStatus;
  description?: string;
}

// ============================================================
// MDM-002 — BusinessPartner (per TRAE_MDM_002_API_HANDOFF.md §2)
// ============================================================
// Wire role: 0=None, 1=Customer, 2=Supplier, 3=Both (bit-flag)
export type BusinessPartnerRoleInt = 0 | 1 | 2 | 3;

/** UI role value (used in create/update form — the actual row role). */
export type BusinessPartnerRole = 'CUSTOMER' | 'SUPPLIER' | 'BOTH';

export const BP_ROLE_OPTIONS: { value: BusinessPartnerRole; label: string }[] = [
  { value: 'CUSTOMER', label: '客户' },
  { value: 'SUPPLIER', label: '供应商' },
  { value: 'BOTH', label: '兼任客户与供应商' },
];
const ROLE_INT_MAP: Record<BusinessPartnerRoleInt, BusinessPartnerRole> = { 0: 'CUSTOMER', 1: 'CUSTOMER', 2: 'SUPPLIER', 3: 'BOTH' };
const ROLE_UI_MAP: Record<BusinessPartnerRole, BusinessPartnerRoleInt> = { CUSTOMER: 1, SUPPLIER: 2, BOTH: 3 };
export function roleIntToUi(v: BusinessPartnerRoleInt): BusinessPartnerRole { return ROLE_INT_MAP[v]; }
export function roleUiToInt(v: BusinessPartnerRole): BusinessPartnerRoleInt { return ROLE_UI_MAP[v]; }

/**
 * List filter role. Per Handoff §5 the `role` query param is a bit-flag:
 * pass 1 to match Customer OR Both; pass 2 to match Supplier OR Both;
 * omit for no role filter (all).
 */
export type BusinessPartnerRoleFilter = 'all' | 'customer' | 'supplier';
export const BP_ROLE_FILTER_OPTIONS: { value: BusinessPartnerRoleFilter; label: string }[] = [
  { value: 'all', label: '全部往来单位' },
  { value: 'customer', label: '客户' },
  { value: 'supplier', label: '供应商' },
];
export function roleFilterToInt(v: BusinessPartnerRoleFilter): BusinessPartnerRoleInt | undefined {
  if (v === 'customer') return 1;
  if (v === 'supplier') return 2;
  return undefined; // all → omit role param
}

export interface BusinessPartnerDto {
  id: string;
  code: string;
  name: string;
  shortName: string | null;
  role: BusinessPartnerRoleInt;
  contactPerson: string | null;
  phone: string | null;
  email: string | null;
  addressLine1: string | null;
  addressLine2: string | null;
  city: string | null;
  region: string | null;
  postalCode: string | null;
  countryCode: string | null;
  taxNumber: string | null;
  // GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 - Wave 3 (2026-08-28)
  // additive fields. mnemonics + optional Region binding + server-
  // derived snapshots. UI never writes the snapshots.
  mnemonicCode: string | null;
  administrativeRegionId: string | null;
  regionCodeSnapshot: string | null;
  regionNameSnapshot: string | null;
  status: MasterDataStatusInt;
  description: string | null;
  createdAt: string;
  modifiedAt: string;
  concurrencyVersion: number;
}
export interface CreateBusinessPartnerRequest {
  code: string;
  name: string;
  shortName: string | null;
  role: BusinessPartnerRoleInt;
  contactPerson: string | null;
  phone: string | null;
  email: string | null;
  addressLine1: string | null;
  addressLine2: string | null;
  city: string | null;
  region: string | null;
  postalCode: string | null;
  countryCode: string | null;
  taxNumber: string | null;
  // Wave 3.
  mnemonicCode: string | null;
  administrativeRegionId: string | null;
  description: string | null;
}
export interface UpdateBusinessPartnerRequest {
  name: string;
  shortName: string | null;
  role: BusinessPartnerRoleInt;
  contactPerson: string | null;
  phone: string | null;
  email: string | null;
  addressLine1: string | null;
  addressLine2: string | null;
  city: string | null;
  region: string | null;
  postalCode: string | null;
  countryCode: string | null;
  taxNumber: string | null;
  // Wave 3.
  mnemonicCode: string | null;
  administrativeRegionId: string | null;
  status: MasterDataStatusInt;
  description: string | null;
  expectedConcurrencyVersion: number;
}
export interface BusinessPartnerListParams {
  keyword?: string;
  role?: BusinessPartnerRoleInt;
  status?: MasterDataStatusInt;
  page?: number;
  pageSize?: number;
}

// UI types
export interface BusinessPartner {
  id: string;
  code: string;
  name: string;
  shortName?: string;
  role: BusinessPartnerRole;
  contactPerson?: string;
  phone?: string;
  email?: string;
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  region?: string;
  postalCode?: string;
  countryCode?: string;
  taxNumber?: string;
  // GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 - Wave 3.
  mnemonicCode?: string;
  administrativeRegionId?: string;
  regionCodeSnapshot?: string;
  regionNameSnapshot?: string;
  status: MasterDataStatus;
  description?: string;
  createdAt: string;
  updatedAt: string;
  concurrencyVersion: number;
}
export interface BusinessPartnerForm {
  code: string;
  name: string;
  shortName: string;
  role: BusinessPartnerRole;
  contactPerson: string;
  phone: string;
  email: string;
  addressLine1: string;
  addressLine2: string;
  city: string;
  region: string;
  postalCode: string;
  countryCode: string;
  taxNumber: string;
  // Wave 3. mnemonicCode + Region binding (id only; snapshots
  // are server-derived, never written from the SPA).
  mnemonicCode: string;
  administrativeRegionId: string | null;
  status: MasterDataStatus;
  description: string;
}

// ============================================================
// MDM-002 — Warehouse (per TRAE_MDM_002_API_HANDOFF.md §3)
// ============================================================
// Wire type: 1=Physical, 2=Virtual, 3=Return
export type WarehouseTypeInt = 1 | 2 | 3;
export type WarehouseType = 'PHYSICAL' | 'VIRTUAL' | 'RETURN';
export const WAREHOUSE_TYPE_OPTIONS: { value: WarehouseType; label: string }[] = [
  { value: 'PHYSICAL', label: '实体仓' },
  { value: 'VIRTUAL', label: '虚拟仓' },
  { value: 'RETURN', label: '退货仓' },
];
const WH_TYPE_INT_MAP: Record<WarehouseTypeInt, WarehouseType> = { 1: 'PHYSICAL', 2: 'VIRTUAL', 3: 'RETURN' };
const WH_TYPE_UI_MAP: Record<WarehouseType, WarehouseTypeInt> = { PHYSICAL: 1, VIRTUAL: 2, RETURN: 3 };
export function whTypeIntToUi(v: WarehouseTypeInt): WarehouseType { return WH_TYPE_INT_MAP[v]; }
export function whTypeUiToInt(v: WarehouseType): WarehouseTypeInt { return WH_TYPE_UI_MAP[v]; }

export interface WarehouseDto {
  id: string;
  plantId: string | null;
  code: string;
  name: string;
  type: WarehouseTypeInt;
  addressLine1: string | null;
  addressLine2: string | null;
  city: string | null;
  region: string | null;
  postalCode: string | null;
  countryCode: string | null;
  status: MasterDataStatusInt;
  description: string | null;
  createdAt: string;
  modifiedAt: string;
  concurrencyVersion: number;
}
export interface CreateWarehouseRequest {
  plantId: string | null;
  code: string;
  name: string;
  type: WarehouseTypeInt;
  addressLine1: string | null;
  addressLine2: string | null;
  city: string | null;
  region: string | null;
  postalCode: string | null;
  countryCode: string | null;
  description: string | null;
}
export interface UpdateWarehouseRequest {
  plantId: string | null;
  name: string;
  type: WarehouseTypeInt;
  addressLine1: string | null;
  addressLine2: string | null;
  city: string | null;
  region: string | null;
  postalCode: string | null;
  countryCode: string | null;
  status: MasterDataStatusInt;
  description: string | null;
  expectedConcurrencyVersion: number;
}
export interface WarehouseListParams {
  keyword?: string;
  type?: WarehouseTypeInt;
  status?: MasterDataStatusInt;
  page?: number;
  pageSize?: number;
}

export interface Warehouse {
  id: string;
  plantId: string | null;
  code: string;
  name: string;
  type: WarehouseType;
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  region?: string;
  postalCode?: string;
  countryCode?: string;
  status: MasterDataStatus;
  description?: string;
  createdAt: string;
  updatedAt: string;
  concurrencyVersion: number;
}
export interface WarehouseForm {
  code: string;
  name: string;
  type: WarehouseType;
  plantId: string;
  addressLine1: string;
  addressLine2: string;
  city: string;
  region: string;
  postalCode: string;
  countryCode: string;
  status: MasterDataStatus;
  description: string;
}

// ============================================================
// MDM-002 — Location (per TRAE_MDM_002_API_HANDOFF.md §4)
// ============================================================
// Wire type: 1=Bin, 2=Shelf, 3=Zone, 4=Dock
export type LocationTypeInt = 1 | 2 | 3 | 4;
export type LocationType = 'BIN' | 'SHELF' | 'ZONE' | 'DOCK';
export const LOCATION_TYPE_OPTIONS: { value: LocationType; label: string }[] = [
  { value: 'BIN', label: '货位' },
  { value: 'SHELF', label: '货架' },
  { value: 'ZONE', label: '区域' },
  { value: 'DOCK', label: '月台' },
];
const LOC_TYPE_INT_MAP: Record<LocationTypeInt, LocationType> = { 1: 'BIN', 2: 'SHELF', 3: 'ZONE', 4: 'DOCK' };
const LOC_TYPE_UI_MAP: Record<LocationType, LocationTypeInt> = { BIN: 1, SHELF: 2, ZONE: 3, DOCK: 4 };
export function locTypeIntToUi(v: LocationTypeInt): LocationType { return LOC_TYPE_INT_MAP[v]; }
export function locTypeUiToInt(v: LocationType): LocationTypeInt { return LOC_TYPE_UI_MAP[v]; }

export interface LocationDto {
  id: string;
  warehouseId: string;
  code: string;
  name: string;
  type: LocationTypeInt;
  aisle: string | null;
  bay: string | null;
  shelf: string | null;
  status: MasterDataStatusInt;
  description: string | null;
  createdAt: string;
  modifiedAt: string;
  concurrencyVersion: number;
}
export interface CreateLocationRequest {
  warehouseId: string;
  code: string;
  name: string;
  type: LocationTypeInt;
  aisle: string | null;
  bay: string | null;
  shelf: string | null;
  description: string | null;
}
export interface UpdateLocationRequest {
  warehouseId: string;
  name: string;
  type: LocationTypeInt;
  aisle: string | null;
  bay: string | null;
  shelf: string | null;
  status: MasterDataStatusInt;
  description: string | null;
  expectedConcurrencyVersion: number;
}
export interface LocationListParams {
  keyword?: string;
  warehouseId?: string;
  type?: LocationTypeInt;
  status?: MasterDataStatusInt;
  page?: number;
  pageSize?: number;
}

export interface Location {
  id: string;
  warehouseId: string;
  code: string;
  name: string;
  type: LocationType;
  aisle?: string;
  bay?: string;
  shelf?: string;
  status: MasterDataStatus;
  description?: string;
  createdAt: string;
  updatedAt: string;
  concurrencyVersion: number;
  // UI-denormalized (resolved from warehouseId reference via Warehouse list API)
  warehouseName?: string;
  warehouseCode?: string;
}
export interface LocationForm {
  warehouseId: string | null;
  code: string;
  name: string;
  type: LocationType;
  aisle: string;
  bay: string;
  shelf: string;
  status: MasterDataStatus;
  description: string;
}

// ============================================================
// Shared helpers — ItemCategory hierarchy derivation (used by ItemCategory + Item pages)
// ============================================================
export function deriveCategoryHierarchy(raw: ItemCategory[]): ItemCategoryListItem[] {
  const map = new Map(raw.map(c => [c.id, c]));
  function getLevel(id: string | null): number {
    if (id == null) return 0;
    const p = map.get(id);
    return p ? getLevel(p.parentId) + 1 : 0;
  }
  function getPath(id: string | null): string {
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

// ============================================================
// GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 - Wave 2
// (2026-08-28) Country + AdministrativeRegion reference DTOs.
// Per brief §十五 + §二十一, V1 minimal fields. The wire format
// mirrors the C# DTOs (long id as string per Snowflake/HiLo
// string contract).
// ============================================================
export interface CountryListItem {
  id: string;
  code: string;          // ISO 3166-1 alpha-2
  alpha3Code: string | null;
  name: string;          // zh-Hans display name (CLDR)
  englishName: string;   // en display name (CLDR)
  isActive: boolean;
  sortOrder: number;
}
export interface Country extends CountryListItem {}

export interface AdministrativeRegionListItem {
  id: string;
  countryCode: string;
  code: string;          // In-country region code (CN: GB/T 2260)
  name: string;          // zh-Hans display name
  englishName: string | null;
  shortName: string | null;
  parentId: string | null;
  level: number;         // 0 = country-root, 1 = province, ...
  regionType: string;
  isActive: boolean;
  sortOrder: number;
}
export interface AdministrativeRegion extends AdministrativeRegionListItem {}

// ============================================================
// GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 - Wave 2
// (2026-08-28) Lightweight option shape for the Country
// searchable selector. The component is shared with the future
// Warehouse / Plant address flow (per brief §三十六).
// ============================================================
export interface CountryOption {
  value: string;        // alpha-2 code
  label: string;        // "{alpha-2} {name}" or "{alpha-2} {englishName}"
  alpha3Code: string | null;
  englishName: string;
  zhName: string;
}
