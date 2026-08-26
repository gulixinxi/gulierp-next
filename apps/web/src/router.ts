import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import ErpShell from './layouts/ErpShell.vue';
import BootstrapStatus from './views/BootstrapStatus.vue';
import SalesOrderList from './views/sales-order/SalesOrderList.vue';
import SalesOrderEdit from './views/sales-order/SalesOrderEdit.vue';
import SalesOrderDetail from './views/sales-order/SalesOrderDetail.vue';
import PurchaseOrderList from './views/purchase/PurchaseOrderList.vue';
import PurchaseOrderEdit from './views/purchase/PurchaseOrderEdit.vue';
import PurchaseOrderDetail from './views/purchase/PurchaseOrderDetail.vue';
import { installAuthRoutes, installAuthGuard } from './router/auth';
import { installMdmRoutes } from './router/mdm';
import { installSystemRoutes } from './router/system';

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    component: ErpShell,
    children: [
      {
        path: '',
        redirect: '/sales/orders'
      },
      {
        path: 'sales/orders',
        name: 'SalesOrderList',
        component: SalesOrderList,
        meta: { title: '销售订单', module: 'sales' }
      },
      {
        path: 'sales-order',
        redirect: '/sales/orders'
      },
      {
        path: 'sales-order/:id/edit',
        name: 'SalesOrderEdit',
        component: SalesOrderEdit,
        props: true
      },
      {
        path: 'sales-order/:id',
        name: 'SalesOrderDetail',
        component: SalesOrderDetail,
        props: true
      },
      // G3-R2B: PurchaseOrder routes. Mirrors the G3-R2A
      // SalesOrder route shape. The list uses the canonical
      // /purchase/orders path so the navigation group's
      // "采购订单" entry is the same as the menu item; the
      // /purchase-orders/... path is the legacy tab-driven
      // form/detail path (matches the SalesOrder /sales-order/...
      // legacy path).
      {
        path: 'purchase/orders',
        name: 'PurchaseOrderList',
        component: PurchaseOrderList,
        meta: { title: '采购订单', module: 'purchase' }
      },
      {
        path: 'purchase-orders',
        redirect: '/purchase/orders'
      },
      {
        path: 'purchase-orders/:id/edit',
        name: 'PurchaseOrderEdit',
        component: PurchaseOrderEdit,
        props: true
      },
      {
        path: 'purchase-orders/:id',
        name: 'PurchaseOrderDetail',
        component: PurchaseOrderDetail,
        props: true
      }
    ]
  },
  {
    path: '/bootstrap',
    name: 'bootstrap-status',
    component: BootstrapStatus,
    meta: { public: true }
  }
];

export const router = createRouter({
  history: createWebHistory(),
  routes
});

// WEB-001: Register login, 403 + install no-flash auth guard.
installAuthRoutes(router);
installAuthGuard(router);

// WEB-PREVIEW-001: Register MDM routes (UOM / ItemCategory / Item).
// Surgically merged — preserves Auth + SalesOrder routes above.
installMdmRoutes(router);
installSystemRoutes(router);
