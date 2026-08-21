/**
 * Auth Router — Dynamic route registration + no-flash auth guard.
 *
 * Why a SEPARATE module instead of modifying router.ts?
 *   router.ts would work too; but this module keeps WEB-001 ownership crisp.
 *   The router.ts file simply calls `installAuthRoutes(router)` before app mount.
 *
 * Two hard guarantees:
 *   1. NEVER show a business shell, render a `<router-view>`, or fire a backend call
 *      before `/me` bootstrap resolves (`initialized=true`).
 *   2. NEVER redirect from a protected page → /login with a visible flash. The
 *      `beforeEach` guard returns a redirect promise synchronously; Vue Router
 *      will only render the destination route.
 */
import type { Router, RouteLocationNormalized, NavigationGuardNext } from 'vue-router';
import { useAuthStore } from '../stores/auth';
import Login from '../views/auth/Login.vue';
import Forbidden403 from '../views/auth/Forbidden403.vue';

const PUBLIC_ROUTES = new Set(['login', '403', 'bootstrap-status']);

export function isPublicRouteName(name: unknown): boolean {
  return typeof name === 'string' && PUBLIC_ROUTES.has(name);
}

/** Register WEB-001 routes on the app-wide router. */
export function installAuthRoutes(router: Router): void {
  // /login — standalone (outside ErpShell).
  if (!router.hasRoute('login')) {
    router.addRoute({
      path: '/login',
      name: 'login',
      component: Login,
      meta: { public: true },
    });
  }
  // /403 — standalone page. Entry is allowed both authenticated & unauthenticated
  // (unauthenticated will be caught by the guard anyway).
  if (!router.hasRoute('403')) {
    router.addRoute({
      path: '/403',
      name: '403',
      component: Forbidden403,
      meta: { public: true },
    });
  }
  // Wildcard 404 → keep existing behavior. No changes here.
}

/**
 * No-flash authentication guard. Only effective AFTER `auth.bootstrap()` has resolved
 * (see main.ts: we `await` bootstrap before `app.mount` to prevent the pre-mount flash).
 *
 * This guard handles the dynamic case (user clicks links / browser history mid-session)
 * AND handles a "hard 401 mid-interaction" where the auth store flips unauthenticated.
 */
export function installAuthGuard(router: Router): void {
  router.beforeEach(async (to: RouteLocationNormalized, from: RouteLocationNormalized, next: NavigationGuardNext) => {
    const auth = useAuthStore();
    // Lazily ensure auth bootstrap (safe even if already resolved).
    // In normal flow main.ts already awaited it; this is a backstop.
    if (!auth.initialized) {
      // If we somehow hit the guard before bootstrap completed, defer.
      await auth.bootstrap();
    }

    const isPublic = to.meta?.public === true || isPublicRouteName(to.name);

    // --- Rule 1: Authenticated user hitting /login → forward to shell ---
    if (to.name === 'login' && auth.isAuthenticated) {
      const redirect = auth.consumeIntendedRoute();
      if (redirect && redirect !== '/login') {
        return next(redirect);
      }
      return next('/sales-order');
    }

    // --- Rule 2: Unauthenticated user hitting a protected route → login + capture intended ---
    if (!isPublic && !auth.isAuthenticated) {
      const full = to.fullPath || to.path;
      if (full && full !== '/' && !full.startsWith('/login')) {
        auth.setIntendedRoute(full);
      }
      return next({ name: 'login' });
    }

    // --- Rule 3: Otherwise proceed ---
    next();
  });
}
