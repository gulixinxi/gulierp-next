/**
 * MDM Mock Data — STATIC PROTOTYPE ONLY
 *
 * This file is explicitly mock data. It is NOT a production API.
 * All IDs, codes, and values are fabricated for UI demonstration.
 * When backend MDM endpoints are ready, replace these with real API calls.
 */

import type { Uom, ItemCategory, Item } from '../types/mdm';

// ============================================================
// UOM Mock Data
// ============================================================

export const mockUoms: Uom[] = [
  { id: 1, code: 'PCS', name: '个', symbol: 'pc', decimalPlaces: 0, status: 'active', description: '计数单位', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: 2, code: 'KG', name: '千克', symbol: 'kg', decimalPlaces: 3, status: 'active', description: '重量单位', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: 3, code: 'G', name: '克', symbol: 'g', decimalPlaces: 3, status: 'active', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: 4, code: 'L', name: '升', symbol: 'L', decimalPlaces: 3, status: 'active', description: '体积单位', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: 5, code: 'M', name: '米', symbol: 'm', decimalPlaces: 2, status: 'active', description: '长度单位', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: 6, code: 'BOX', name: '箱', symbol: 'box', decimalPlaces: 0, status: 'active', description: '包装单位', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: 7, code: 'SET', name: '套', symbol: 'set', decimalPlaces: 0, status: 'active', description: '成套单位', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: 8, code: 'HOUR', name: '小时', symbol: 'h', decimalPlaces: 1, status: 'active', description: '服务计时单位', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: 9, code: 'M2', name: '平方米', symbol: 'm²', decimalPlaces: 2, status: 'draft', description: '面积单位', createdAt: '2025-02-01T10:00:00Z', updatedAt: '2025-02-01T10:00:00Z' },
  { id: 10, code: 'TNE', name: '吨', symbol: 't', decimalPlaces: 3, status: 'inactive', description: '大重量单位', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-03-01T12:00:00Z' },
];

// ============================================================
// ItemCategory Mock Data
// ============================================================

export const mockItemCategories: ItemCategory[] = [
  { id: 1, code: 'RAW', name: '原材料', parentId: null, status: 'active', description: '生产用原材料大类', level: 0, fullPath: '原材料', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: 2, code: 'RAW-STEEL', name: '钢材', parentId: 1, parentName: '原材料', status: 'active', level: 1, fullPath: '原材料 / 钢材', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: 3, code: 'RAW-STEEL-SS', name: '不锈钢', parentId: 2, parentName: '钢材', status: 'active', level: 2, fullPath: '原材料 / 钢材 / 不锈钢', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: 4, code: 'RAW-PLASTIC', name: '塑料', parentId: 1, parentName: '原材料', status: 'active', level: 1, fullPath: '原材料 / 塑料', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: 5, code: 'FIN', name: '成品', parentId: null, status: 'active', description: '最终销售产品', level: 0, fullPath: '成品', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: 6, code: 'FIN-ELEC', name: '电子产品', parentId: 5, parentName: '成品', status: 'active', level: 1, fullPath: '成品 / 电子产品', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: 7, code: 'FIN-MECH', name: '机械产品', parentId: 5, parentName: '成品', status: 'active', level: 1, fullPath: '成品 / 机械产品', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: 8, code: 'PKG', name: '包装材料', parentId: null, status: 'active', level: 0, fullPath: '包装材料', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
  { id: 9, code: 'FIN-ELEC-PH', name: '手机配件', parentId: 6, parentName: '电子产品', status: 'draft', level: 2, fullPath: '成品 / 电子产品 / 手机配件', createdAt: '2025-02-01T10:00:00Z', updatedAt: '2025-02-01T10:00:00Z' },
  { id: 10, code: 'SVC', name: '服务项目', parentId: null, status: 'active', description: '无形服务类', level: 0, fullPath: '服务项目', createdAt: '2025-01-15T08:00:00Z', updatedAt: '2025-01-15T08:00:00Z' },
];

// ============================================================
// Item Mock Data
// ============================================================

export const mockItems: Item[] = [
  { id: 1, code: 'ITEM-0001', name: '304不锈钢板 2mm', specification: '304 / 2mm / 1220x2440mm', categoryId: 3, categoryName: '不锈钢', baseUomId: 5, baseUomName: '米', itemType: 'goods', status: 'active', inventoryMethod: 'WEIGHTED_AVG', description: '标准304不锈钢板材', createdAt: '2025-01-20T09:00:00Z', updatedAt: '2025-01-20T09:00:00Z' },
  { id: 2, code: 'ITEM-0002', name: 'PP塑料颗粒', specification: 'PP-T30S', categoryId: 4, categoryName: '塑料', baseUomId: 2, baseUomName: '千克', itemType: 'goods', status: 'active', inventoryMethod: 'FIFO', createdAt: '2025-01-20T09:00:00Z', updatedAt: '2025-01-20T09:00:00Z' },
  { id: 3, code: 'ITEM-0003', name: '蓝牙耳机 X1', specification: 'V5.3 / ANC', categoryId: 6, categoryName: '电子产品', baseUomId: 1, baseUomName: '个', itemType: 'goods', status: 'active', inventoryMethod: 'WEIGHTED_AVG', description: '蓝牙5.3降噪耳机', createdAt: '2025-01-22T14:00:00Z', updatedAt: '2025-01-22T14:00:00Z' },
  { id: 4, code: 'ITEM-0004', name: '伺服电机 750W', specification: '750W / 3000rpm', categoryId: 7, categoryName: '机械产品', baseUomId: 1, baseUomName: '个', itemType: 'goods', status: 'active', inventoryMethod: 'SPECIFIC', createdAt: '2025-01-22T14:00:00Z', updatedAt: '2025-01-22T14:00:00Z' },
  { id: 5, code: 'ITEM-0005', name: '标准纸箱 500x300x200', specification: '5层瓦楞', categoryId: 8, categoryName: '包装材料', baseUomId: 6, baseUomName: '箱', itemType: 'package', status: 'active', inventoryMethod: 'FIFO', createdAt: '2025-01-22T14:00:00Z', updatedAt: '2025-01-22T14:00:00Z' },
  { id: 6, code: 'ITEM-0006', name: '安装调试服务', specification: '按次', categoryId: 10, categoryName: '服务项目', baseUomId: 8, baseUomName: '小时', itemType: 'service', status: 'active', createdAt: '2025-01-22T14:00:00Z', updatedAt: '2025-01-22T14:00:00Z' },
  { id: 7, code: 'ITEM-0007', name: '304不锈钢板 3mm', specification: '304 / 3mm / 1220x2440mm', categoryId: 3, categoryName: '不锈钢', baseUomId: 5, baseUomName: '米', itemType: 'goods', status: 'active', inventoryMethod: 'WEIGHTED_AVG', createdAt: '2025-01-20T09:00:00Z', updatedAt: '2025-01-20T09:00:00Z' },
  { id: 8, code: 'ITEM-0008', name: '手机壳 6.1寸', specification: '透明硅胶', categoryId: 9, categoryName: '手机配件', baseUomId: 1, baseUomName: '个', itemType: 'goods', status: 'draft', createdAt: '2025-02-05T11:00:00Z', updatedAt: '2025-02-05T11:00:00Z' },
  { id: 9, code: 'ITEM-0009', name: 'ABS塑料颗粒', specification: 'PA-747', categoryId: 4, categoryName: '塑料', baseUomId: 2, baseUomName: '千克', itemType: 'goods', status: 'active', inventoryMethod: 'FIFO', createdAt: '2025-01-20T09:00:00Z', updatedAt: '2025-01-20T09:00:00Z' },
  { id: 10, code: 'ITEM-0010', name: 'USB-C数据线 1m', specification: '2A / 编织', categoryId: 6, categoryName: '电子产品', baseUomId: 1, baseUomName: '个', itemType: 'goods', status: 'inactive', inventoryMethod: 'WEIGHTED_AVG', description: '已停产', createdAt: '2025-01-22T14:00:00Z', updatedAt: '2025-03-10T10:00:00Z' },
];

// ============================================================
// Lookup helpers for cross-reference in UI
// ============================================================

export function findUom(id: number | null): Uom | undefined {
  if (id == null) return undefined;
  return mockUoms.find(u => u.id === id);
}

export function findCategory(id: number | null): ItemCategory | undefined {
  if (id == null) return undefined;
  return mockItemCategories.find(c => c.id === id);
}

export function getCategoryPath(id: number | null): string {
  const cat = findCategory(id);
  return cat?.fullPath || '';
}
