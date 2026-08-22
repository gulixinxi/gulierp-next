import type { Router } from 'vue-router';
import ErpShell from '../layouts/ErpShell.vue';

export function installSystemRoutes(router: Router): void {
  router.addRoute({
    path: '/system',
    component: ErpShell,
    redirect: '/system/enterprise-organization',
    meta: { requiresAuth: true, module: 'system' },
    children: [
      {
        path: 'enterprise-organization',
        name: 'system-enterprise-organization',
        component: () => import('../views/system/EnterpriseOrganization.vue'),
        meta: { title: '企业组织', module: 'system' },
      },
    ],
  });
}
