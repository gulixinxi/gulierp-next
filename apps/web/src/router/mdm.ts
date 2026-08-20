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
    redirect: '/mdm/items',
    meta: { requiresAuth: true, module: 'mdm' },
    children: [
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
    ],
  });
}
