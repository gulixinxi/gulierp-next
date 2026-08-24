/**
 * MDM Routes — Static Prototype
 *
 * MANUAL_ROUTE_MERGE_REQUIRED:
 *   router.ts is currently dirty (pre-existing + WEB-001 auth changes).
 *   Do NOT directly modify router.ts. Instead, call installMdmRoutes(router)
 *   from router.ts when safe to merge.
 *
 * Usage (add to router.ts after auth route installation):
 *   import { installMdmRoutes } from './router/mdm';
 *   installMdmRoutes(router);
 */

import type { Router } from 'vue-router';
import ErpShell from '../layouts/ErpShell.vue';

export function installMdmRoutes(router: Router): void {
  router.addRoute({
    path: '/mdm',
    component: ErpShell,
    meta: { requiresAuth: true, module: 'mdm' },
    children: [
      {
        path: '',
        name: 'mdm-workbench',
        component: () => import('../views/mdm/MasterDataWorkbench.vue'),
        meta: { title: '主数据中心', module: 'mdm' },
      },
      {
        path: 'uoms',
        name: 'mdm-uoms',
        component: () => import('../views/mdm/UomList.vue'),
        meta: { title: '计量单位', module: 'mdm' },
      },
      {
        path: 'item-categories',
        name: 'mdm-item-categories',
        component: () => import('../views/mdm/ItemCategoryList.vue'),
        meta: { title: '物料分类', module: 'mdm' },
      },
      {
        path: 'items',
        name: 'mdm-items',
        component: () => import('../views/mdm/ItemList.vue'),
        meta: { title: '物料', module: 'mdm' },
      },
      // ---- MDM-002: BusinessPartner / Warehouse / Location (real API) ----
      // Single reusable BusinessPartnerList component serves customers /
      // suppliers / all — route meta.defaultRole sets the INITIAL role filter
      // (Handoff §5 bit-flag: 1=Customer matches Customer|Both;
      // 2=Supplier matches Supplier|Both).
      {
        path: 'business-partners',
        name: 'mdm-business-partners',
        component: () => import('../views/mdm/BusinessPartnerList.vue'),
        meta: { title: '往来单位', module: 'mdm', defaultRole: 'all' },
      },
      {
        path: 'customers',
        name: 'mdm-customers',
        component: () => import('../views/mdm/BusinessPartnerList.vue'),
        meta: { title: '客户档案', module: 'mdm', defaultRole: 'customer' },
      },
      {
        path: 'suppliers',
        name: 'mdm-suppliers',
        component: () => import('../views/mdm/BusinessPartnerList.vue'),
        meta: { title: '供应商', module: 'mdm', defaultRole: 'supplier' },
      },
      {
        path: 'warehouses',
        name: 'mdm-warehouses',
        component: () => import('../views/mdm/WarehouseList.vue'),
        meta: { title: '仓库', module: 'mdm' },
      },
      {
        path: 'locations',
        name: 'mdm-locations',
        component: () => import('../views/mdm/LocationList.vue'),
        meta: { title: '库位', module: 'mdm' },
      },
    ],
  });
}
