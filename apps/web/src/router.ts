import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import ErpShell from './layouts/ErpShell.vue';
import BootstrapStatus from './views/BootstrapStatus.vue';
import SalesOrderList from './views/sales-order/SalesOrderList.vue';
import SalesOrderEdit from './views/sales-order/SalesOrderEdit.vue';
import SalesOrderDetail from './views/sales-order/SalesOrderDetail.vue';
import { installAuthRoutes, installAuthGuard } from './router/auth';
import { installMdmRoutes } from './router/mdm';

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    component: ErpShell,
    children: [
      {
        path: '',
        redirect: '/sales-order'
      },
      {
        path: 'sales-order',
        name: 'SalesOrderList',
        component: SalesOrderList
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
