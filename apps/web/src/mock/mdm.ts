/**
 * MDM Mock Data — STATIC PROTOTYPE ONLY (MOCK_ONLY)
 *
 * Reconciled with MDM_000_MASTER_DATA_CONVENTION_V1 (FROZEN).
 * - UOM: decimalPlaces removed; dimension + kind added; status active/inactive only
 * - ItemCategory: level/fullPath are UI-derived (computed), not persisted mock fields
 * - Item: itemType replaced with itemNature; inventoryMethod removed; package removed
 *
 * This is NOT a production API. All values are fabricated for UI demonstration.
 * When backend MDM endpoints are ready, replace with real API calls.
 */

import type { Uom, ItemCategory, ItemCategoryListItem, Item } from '../types/mdm';

// ============================================================
// UOM Mock Data — Frozen §6 (13 DEV items, SAFE_TO_SEED_SYSTEM)
// ============================================================

export const mockUoms: Uom[] = [
  { id: '1', code: 'PCS', name: '个', symbol: 'pc', dimension: 'COUNT', kind: 'DISCRETE', status: 'active', description: '计数单位', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: '2', code: 'KG', name: '千克', symbol: 'kg', dimension: 'MASS', kind: 'SI', status: 'active', description: '质量单位', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: '3', code: 'G', name: '克', symbol: 'g', dimension: 'MASS', kind: 'SI', status: 'active', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: '4', code: 'L', name: '升', symbol: 'L', dimension: 'VOLUME', kind: 'SI', status: 'active', description: '体积单位', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: '5', code: 'M', name: '米', symbol: 'm', dimension: 'LENGTH', kind: 'SI', status: 'active', description: '长度单位', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: '6', code: 'BOX', name: '箱', symbol: 'box', dimension: 'COUNT', kind: 'DISCRETE', status: 'active', description: '包装单位', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: '7', code: 'SET', name: '套', symbol: 'set', dimension: 'COUNT', kind: 'DISCRETE', status: 'active', description: '成套单位', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: '8', code: 'HOUR', name: '小时', symbol: 'h', dimension: 'TIME', kind: 'SI', status: 'active', description: '服务计时单位', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: '9', code: 'M2', name: '平方米', symbol: 'm²', dimension: 'AREA', kind: 'SI', status: 'active', description: '面积单位', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: '10', code: 'TNE', name: '吨', symbol: 't', dimension: 'MASS', kind: 'SI', status: 'inactive', description: '大重量单位', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-03-01T12:00:00Z' },
  { id: '11', code: 'BENG', name: '本', symbol: null, dimension: 'COUNT', kind: 'DISCRETE', status: 'active', description: '书籍计数', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: '12', code: 'TAO', name: '套', symbol: null, dimension: 'COUNT', kind: 'DISCRETE', status: 'active', description: '家具计数', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: '13', code: 'ZHANG', name: '张', symbol: null, dimension: 'COUNT', kind: 'DISCRETE', status: 'active', description: '平板计数', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
];

// ============================================================
// ItemCategory Mock Data — Frozen §7
// level and fullPath are UI-derived, computed from parentId chain.
// ============================================================

const rawCategories: ItemCategory[] = [
  { id: '1', code: 'RAW', name: '原材料', parentId: null, status: 'active', description: '生产用原材料大类', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: '2', code: 'RAW-STEEL', name: '钢材', parentId: '1', status: 'active', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: '3', code: 'RAW-STEEL-SS', name: '不锈钢', parentId: '2', status: 'active', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: '4', code: 'RAW-PLASTIC', name: '塑料', parentId: '1', status: 'active', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: '5', code: 'FIN', name: '成品', parentId: null, status: 'active', description: '最终销售产品', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: '6', code: 'FIN-ELEC', name: '电子产品', parentId: '5', status: 'active', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: '7', code: 'FIN-MECH', name: '机械产品', parentId: '5', status: 'active', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: '8', code: 'PKG', name: '包装材料', parentId: null, status: 'active', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: '9', code: 'FIN-ELEC-PH', name: '手机配件', parentId: '6', status: 'inactive', createdAt: '2025-02-01T10:00:00Z', updatedAt: '2025-03-01T12:00:00Z' },
  { id: '10', code: 'SVC', name: '服务项目', parentId: null, status: 'active', description: '无形服务类', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
];

/**
 * UI-derived: compute level + fullPath from parentId chain.
 * This function exists ONLY in mock/UI layer — backend will compute or the frontend
 * will derive from API response. NOT a persisted field.
 */
function computeDerivedFields(cats: ItemCategory[]): ItemCategoryListItem[] {
  const map = new Map(cats.map(c => [c.id, c]));
  function getLevel(id: string | null): number {
    if (id == null) return 0;
    const parent = map.get(id);
    if (!parent) return 0;
    return getLevel(parent.parentId) + 1;
  }
  function getFullPath(id: string | null): string {
    if (id == null) return '';
    const cat = map.get(id);
    if (!cat) return '';
    const parentPath = getFullPath(cat.parentId);
    return parentPath ? `${parentPath} / ${cat.name}` : cat.name;
  }
  return cats.map(c => {
    const parent = c.parentId != null ? map.get(c.parentId) : undefined;
    return {
      ...c,
      level: getLevel(c.id),
      fullPath: getFullPath(c.id) || c.name,
      parentName: parent?.name,
    };
  });
}

export const mockItemCategories: ItemCategoryListItem[] = computeDerivedFields(rawCategories);

// ============================================================
// Item Mock Data — Frozen §8 (itemNature replaces itemType; no inventoryMethod)
// ============================================================

export const mockItems: Item[] = [
  { id: '1', code: 'ITEM-0001', name: '304不锈钢板 2mm', specification: '304 / 2mm / 1220x2440mm', categoryId: '3', baseUomId: '5', itemNature: 'MATERIAL', status: 'active', description: '标准304不锈钢板材', createdAt: '2025-01-20T09:00:00Z', updatedAt: '2025-01-20T09:00:00Z' },
  { id: '2', code: 'ITEM-0002', name: 'PP塑料颗粒', specification: 'PP-T30S', categoryId: '4', baseUomId: '2', itemNature: 'MATERIAL', status: 'active', createdAt: '2025-01-20T09:00:00Z', updatedAt: '2025-01-20T09:00:00Z' },
  { id: '3', code: 'ITEM-0003', name: '蓝牙耳机 X1', specification: 'V5.3 / ANC', categoryId: '6', baseUomId: '1', itemNature: 'FINISHED_GOOD', status: 'active', description: '蓝牙5.3降噪耳机', createdAt: '2025-01-22T14:00:00Z', updatedAt: '2025-01-22T14:00:00Z' },
  { id: '4', code: 'ITEM-0004', name: '伺服电机 750W', specification: '750W / 3000rpm', categoryId: '7', baseUomId: '1', itemNature: 'FINISHED_GOOD', status: 'active', createdAt: '2025-01-22T14:00:00Z', updatedAt: '2025-01-22T14:00:00Z' },
  { id: '5', code: 'ITEM-0005', name: '标准纸箱 500x300x200', specification: '5层瓦楞', categoryId: '8', baseUomId: '6', itemNature: 'MATERIAL', status: 'active', createdAt: '2025-01-22T14:00:00Z', updatedAt: '2025-01-22T14:00:00Z' },
  { id: '6', code: 'ITEM-0006', name: '安装调试服务', specification: '按次', categoryId: '10', baseUomId: '8', itemNature: 'SERVICE', status: 'active', createdAt: '2025-01-22T14:00:00Z', updatedAt: '2025-01-22T14:00:00Z' },
  { id: '7', code: 'ITEM-0007', name: '304不锈钢板 3mm', specification: '304 / 3mm / 1220x2440mm', categoryId: '3', baseUomId: '5', itemNature: 'MATERIAL', status: 'active', createdAt: '2025-01-20T09:00:00Z', updatedAt: '2025-01-20T09:00:00Z' },
  { id: '8', code: 'ITEM-0008', name: '手机壳 6.1寸', specification: '透明硅胶', categoryId: '9', baseUomId: '1', itemNature: 'SEMI_FINISHED', status: 'inactive', createdAt: '2025-02-05T11:00:00Z', updatedAt: '2025-03-10T10:00:00Z' },
  { id: '9', code: 'ITEM-0009', name: 'ABS塑料颗粒', specification: 'PA-747', categoryId: '4', baseUomId: '2', itemNature: 'MATERIAL', status: 'active', createdAt: '2025-01-20T09:00:00Z', updatedAt: '2025-01-20T09:00:00Z' },
  { id: '10', code: 'ITEM-0010', name: 'USB-C数据线 1m', specification: '2A / 编织', categoryId: '6', baseUomId: '1', itemNature: 'FINISHED_GOOD', status: 'inactive', description: '已停产', createdAt: '2025-01-22T14:00:00Z', updatedAt: '2025-03-10T10:00:00Z' },
];

// ============================================================
// Lookup helpers for cross-reference in UI
// ============================================================

export function findUom(id: string | null): Uom | undefined {
  if (id == null) return undefined;
  return mockUoms.find(u => u.id === id);
}

export function findCategory(id: string | null): ItemCategoryListItem | undefined {
  if (id == null) return undefined;
  return mockItemCategories.find(c => c.id === id);
}

export function getCategoryPath(id: string | null): string {
  const cat = findCategory(id);
  return cat?.fullPath || '';
}
