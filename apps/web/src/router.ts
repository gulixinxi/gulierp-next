import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import ErpShell from './layouts/ErpShell.vue';
import BootstrapStatus from './views/BootstrapStatus.vue';
import LeadList from './views/crm/LeadList.vue';
import CompanyList from './views/crm/CompanyList.vue';
import CompanyDetail from './views/crm/CompanyDetail.vue';
import DealList from './views/crm/DealList.vue';
import TodayTasks from './views/crm/TodayTasks.vue';
import Dashboard from './views/crm/Dashboard.vue';
import OpportunitySuggestions from './views/crm/OpportunitySuggestions.vue';
import SignalCenter from './views/crm/SignalCenter.vue';
import QuakeSearch from './views/crm/QuakeSearch.vue';
import CompanyReverseLookup from './views/crm/CompanyReverseLookup.vue';
import ImportExportLogs from './views/crm/ImportExportLogs.vue';
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
        path: 'sales-orders',
        name: 'SalesOrderList',
        component: () => import('./views/sales-order/SalesOrderList.vue'),
        meta: { title: '销售订单', module: 'sales' }
      },
      {
        path: 'sales-orders/:id/edit',
        name: 'SalesOrderEdit',
        component: () => import('./views/sales-order/SalesOrderEdit.vue'),
        props: true
      },
      {
        path: 'sales-orders/:id',
        name: 'SalesOrderDetail',
        component: () => import('./views/sales-order/SalesOrderDetail.vue'),
        props: true
      },
      // B3.6-C UI Integration — historical /sales/orders and
      // /sales-order/... entries are kept as safe redirects to
      // the canonical /sales-orders/... path. New menu link
      // + all new code point at the plural path; the singular
      // path is preserved for any in-flight deep links from
      // the B2 / B3.6-A / B3.6-B.1 windows.
      {
        path: 'sales/orders',
        redirect: '/sales-orders'
      },
      {
        path: 'sales-order',
        redirect: '/sales-orders'
      },
      {
        // vue-router path-string redirect does NOT substitute
        // path params; use a function redirect so `:id` is
        // forwarded correctly.
        path: 'sales-order/:id/edit',
        redirect: (to) => ({ path: `/sales-orders/${to.params.id}/edit` })
      },
      {
        path: 'sales-order/:id',
        redirect: (to) => ({ path: `/sales-orders/${to.params.id}` })
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
        component: () => import('./views/purchase/PurchaseOrderList.vue'),
        meta: { title: '采购订单', module: 'purchase' }
      },
      {
        path: 'purchase-orders',
        redirect: '/purchase/orders'
      },
      {
        path: 'purchase-orders/:id/edit',
        name: 'PurchaseOrderEdit',
        component: () => import('./views/purchase/PurchaseOrderEdit.vue'),
        props: true
      },
      {
        path: 'purchase-orders/:id',
        name: 'PurchaseOrderDetail',
        component: () => import('./views/purchase/PurchaseOrderDetail.vue'),
        props: true
      },
      // GULIERP_TRANSACTION_DOCUMENT_LINE_GRID_V1 (Phase A).
      // Transient dev preview for the 6 shared transaction components.
      // NOT a production page; will be removed after Phase A acceptance.
      {
        path: 'dev/tx-preview',
        name: 'TxComponentsPreview',
        component: () => import('./views/dev/TxComponentsPreview.vue'),
        meta: { title: 'Transaction Components Preview (dev)', module: 'dev' }
      },
      // CRM 模块
      {
        path: 'crm/dashboard',
        name: 'CrmDashboard',
        component: Dashboard,
        meta: { title: '经营看板', module: 'crm' }
      },
      {
        path: 'crm/leads',
        name: 'CrmLeadList',
        component: LeadList,
        meta: { title: '线索池', module: 'crm' }
      },
      {
        path: 'crm/companies',
        name: 'CrmCompanyList',
        component: CompanyList,
        meta: { title: '客户主档', module: 'crm' }
      },
      {
        path: 'crm/companies/:id',
        name: 'CrmCompanyDetail',
        component: CompanyDetail,
        props: true
      },
      {
        path: 'crm/deals',
        name: 'CrmDealList',
        component: DealList,
        meta: { title: '商机管理', module: 'crm' }
      },
      {
        path: 'crm/opportunities',
        name: 'CrmOpportunitySuggestions',
        component: OpportunitySuggestions,
        meta: { title: '商机建议', module: 'crm' }
      },
      {
        path: 'crm/signals',
        name: 'CrmSignalCenter',
        component: SignalCenter,
        meta: { title: '情报信号', module: 'crm' }
      },
      {
        path: 'crm/quake',
        name: 'CrmQuakeSearch',
        component: QuakeSearch,
        meta: { title: '线索采集', module: 'crm' }
      },
      {
        path: 'crm/reverse-lookup',
        name: 'CrmReverseLookup',
        component: CompanyReverseLookup,
        meta: { title: '单位反查', module: 'crm' }
      },
      {
        path: 'crm/tasks',
        name: 'CrmTodayTasks',
        component: TodayTasks,
        meta: { title: '今日待办', module: 'crm' }
      },
      {
        path: 'crm/logs',
        name: 'CrmImportExportLogs',
        component: ImportExportLogs,
        meta: { title: '导入导出日志', module: 'crm' }
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
