// Auth Store — MINIMAL runtime-only state (never persisted).
// Hard rules:
//   - No password saved anywhere.
//   - No auth cookie / JWT / XSRF token in localStorage or sessionStorage.
//   - Company switch: server round-trip FIRST, then update (never optimistic fake switch — §10).
//   - Session expiry: clear memory state, redirect login preserving intended route (§12).
import { defineStore } from 'pinia';
// WEB-PREVIEW-001R2: useRouter() MUST NOT be called inside Pinia store actions
// (Vue 3 inject() requires setup() context — actions are not setup()).
// Import the router singleton directly. Late-binding is safe: the listener
// fires at RUNTIME (much later than module load), by which time router.ts has
// finished exporting `router`. This eliminates the inject() warning AND the
// window.location.replace hard-reload fallback that caused the redirect loop.
import { router } from '../router';
import { useCsrfStore } from './csrf';
import { login as apiLogin, logout as apiLogout, me as apiMe, switchCompany as apiSwitchCompany } from '../api/auth';
import { onAuthEvent } from '../api/http';
import type { AuthUserDto, AuthStatus, AuthProblem } from '../types/auth';

export const useAuthStore = defineStore('auth', {
  state: () => ({
    status: 'idle' as AuthStatus,
    initialized: false as boolean,
    user: null as AuthUserDto | null,
    lastError: null as AuthProblem | null,
    intendedRoute: null as string | null,
    // Snapshot for rollback on failed company switch (preserve old user DTO)
    _preSwitchUser: null as AuthUserDto | null,
    _authListenerOff: null as (() => void) | null,
  }),
  getters: {
    isAuthenticated(s): boolean {
      return s.status === 'authenticated' && s.user !== null;
    },
    // Convenience helpers for App Shell
    displayName(s): string {
      return s.user?.displayName || s.user?.userName || '';
    },
    userName(s): string {
      return s.user?.userName || '';
    },
    companyName(s): string {
      // backend DTO only provides companyCode by default; when name not available, fall back to code
      return s.user?.companyCode || '';
    },
    companyId(s): string | null {
      return s.user?.companyId ?? null;
    },
    tenantName(s): string {
      return s.user?.tenantCode || '';
    },
    // BACKEND_COMPANY_LIST_CONTRACT_GAP: The backend LoginResponse does NOT include
    // availableCompanies. There is no HTTP endpoint exposing
    // ICompanyDirectoryService.ListForCurrentUserAsync. Until a backend endpoint
    // is added, the company switcher CANNOT work with real data.
    // hasMultipleCompanies is always false — this is HONEST, not a bug.
    hasMultipleCompanies(_s): boolean {
      return false;
    },
    availableCompanies(s): Array<{ companyId: string; companyCode: string; companyName?: string }> {
      // Return only the current company (from /me) as a single-item list.
      // This is NOT a real "available companies" source — it's a UI fallback
      // so the shell can display the current company label.
      if (s.user && s.user.companyId != null && s.user.companyCode != null) {
        return [{ companyId: s.user.companyId, companyCode: s.user.companyCode }];
      }
      return [];
    },
  },
  actions: {
    /**
     * Register the global 401 authentication_required listener (§12 session expiry).
     * Called once on app bootstrap; idempotent.
     */
    ensureListener() {
      if (this._authListenerOff) return;
      this._authListenerOff = onAuthEvent((code) => {
        if (code === 'authentication_required') {
          // WEB-PREVIEW-001R2: wasOnProtectedRoute snapshot BEFORE reset.
          // This listener handles SESSION EXPIRY mid-session — i.e., the
          // user WAS authenticated and just got a 401 (cookie expired).
          // Initial-bootstrap /me 401 is NOT a session-expiry event:
          //   * wasOnProtectedRoute = false (status was 'loading' or 'idle')
          //   * bootstrap()'s catch already calls _resetUnauthenticated()
          //   * no redirect needed — user is already where they should be
          // Previously this listener fired the redirect UNCONDITIONALLY,
          // which on initial /login load caused window.location.replace('/login')
          // → hard browser reload → bootstrap re-runs → /me 401 → re-fire
          // → INFINITE RELOAD LOOP.
          const wasOnProtectedRoute = this.isAuthenticated;
          this._resetUnauthenticated();
          if (!wasOnProtectedRoute) {
            // Not session-expiry (e.g., initial bootstrap 401 on /login).
            // Stay on current route — bootstrap() already set the proper
            // unauthenticated state. No redirect, no reload.
            return;
          }
          // Capture where the user was so we can bring them back after login.
          if (!this.intendedRoute) {
            const path = window.location.pathname + window.location.search;
            if (path && path !== '/' && path !== '/login' && !path.startsWith('/login')) {
              this.intendedRoute = path;
            }
          }
          // WEB-PREVIEW-001R2: SPA navigation via router singleton (NOT useRouter()).
          // No window.location.replace — that caused hard browser reload.
          // Use { name: 'login' } so query params don't carry over spuriously.
          // Swallow redirect aborts (e.g., during ongoing navigation).
          router.replace({ name: 'login' }).catch(() => { /* swallow redirect aborts */ });
        }
        // csrf_validation_failed: the http module already retried once. If it still propagated up,
        // we surface it to the UI via lastError — a hard reload by user is the safest recovery.
        if (code === 'csrf_validation_failed') {
          this.lastError = {
            type: 'about:blank',
            title: 'Session expired',
            detail: 'Your security session has expired. Please reload the page or sign in again.',
            status: 400,
            code: 'csrf_validation_failed',
          };
        }
      });
    },

    /**
     * App bootstrap: resolve /me once.
     *
     * 200 → authenticated (go to shell, no flash).
     * 401 authentication_required → mark unauthenticated (login page renders, no flash).
     *
     * This bootstrap is what §8 (App Bootstrap) requires:
     *   "Avoid flashing a business page then redirecting to login."
     * The root route will render a very lightweight "booting..." shell until initialized===true.
     */
    async bootstrap(): Promise<void> {
      this.ensureListener();
      if (this.status !== 'loading') this.status = 'loading';
      try {
        const user = await apiMe();
        // §4: /me 200 → authenticated, same DTO as login.
        this.user = user;
        this.status = 'authenticated';
        this.lastError = null;
      } catch (err) {
        // 401 authentication_expected is the ONLY expected non-crash path here.
        const code = (err as any)?.code;
        if (code === 'authentication_required') {
          this._resetUnauthenticated();
        } else {
          // Network / 5xx / etc. Record error but still mark initialized
          // (the login page can show "service unavailable" banner and offer retry).
          this.status = 'error';
          this.lastError = {
            type: (err as any)?.type || 'about:blank',
            title: (err as any)?.title || 'Service unavailable',
            detail: (err as any)?.detail || '',
            status: (err as any)?.status || 0,
            code: code || 'service_unavailable',
          };
          // Still treat as "initialized to unauthenticated" so UI can proceed.
          this.user = null;
        }
      } finally {
        this.initialized = true;
      }
    },

    /**
     * Sign in action.
     *
     * Flow per §3:
     *   1. Ensure fresh CSRF token (GET /csrf — credentials include'd; refresh in-memory token).
     *   2. POST /login with CSRF header + userName/password/tenantCode.
     *   3. 200 → store user DTO, set authenticated.
     *   4. 401 → ApiError with code=invalid_credentials (already uniform by backend; NEVER enumerate).
     *
     * SECURITY: The `password` field is passed straight through to apiPost (over HTTPS)
     * and is NEVER written to state, Pinia, localStorage, URL, or console.
     * We don't copy `req` into this function's stack — the login module handles trimming.
     */
    async signIn(req: { userName: string; password: string; tenantCode?: string | null }): Promise<void> {
      this.lastError = null;
      this.status = 'loading';
      // §3: refresh CSRF token immediately before login so it's not stale.
      const csrf = useCsrfStore();
      try {
        await csrf.refresh();
      } catch {
        // CSRF endpoint unreachable → service unavailable. Don't even attempt login.
        this.status = 'error';
        this.lastError = {
          type: 'about:blank',
          title: 'Service unavailable',
          detail: 'Could not establish a secure session. Please try again.',
          status: 503,
          code: 'service_unavailable',
        };
        throw this.lastError;
      }
      try {
        const user = await apiLogin({
          userName: req.userName,
          password: req.password,
          tenantCode: req.tenantCode ?? null,
        });
        // Login success: the backend has SET the auth cookie (HttpOnly — JS can't touch it).
        this.user = user;
        this.status = 'authenticated';
        // Post-login token rotation — §5 note says company-switch/... may need fresh CSRF.
        // Best practice: refresh CSRF after successful login so we have a token paired with the new auth cookie.
        try { await csrf.refresh(); } catch { /* non-fatal */ }
      } catch (err) {
        const code = (err as any)?.code;
        // Per §3.3 DEC-AUTH-006 enumeration defense: accept ONLY invalid_credentials as the 401 code.
        if (code === 'invalid_credentials') {
          this.status = 'unauthenticated';
          this.lastError = {
            type: (err as any)?.type || 'about:blank',
            title: 'Invalid username or password',   // Generic. NEVER hint which one.
            detail: '',                                // Never echo back detail (enumeration defense §3.3).
            status: 401,
            code: 'invalid_credentials',
          };
        } else if (code === 'csrf_validation_failed') {
          // Retry path exhausted. Surface generic "session expired" UI.
          this.status = 'unauthenticated';
          this.lastError = {
            type: 'about:blank',
            title: 'Session expired',
            detail: 'Please try signing in again.',
            status: 400,
            code: 'csrf_validation_failed',
          };
        } else if (code === 'validation_failed') {
          // Field-level validation errors (empty userName, empty password, tenant missing).
          this.status = 'unauthenticated';
          this.lastError = {
            type: (err as any)?.type || 'about:blank',
            title: (err as any)?.title || 'Please check the form',
            detail: '',
            status: (err as any)?.status || 400,
            code: 'validation_failed',
            errors: (err as any)?.errors,
          };
        } else if (code === 'service_unavailable') {
          this.status = 'error';
          this.lastError = {
            type: 'about:blank',
            title: 'Service unavailable',
            detail: 'Could not reach the authentication server.',
            status: 503,
            code: 'service_unavailable',
          };
        } else {
          // Generic / unknown — safe umbrella. Never leak backend trace.
          this.status = 'error';
          this.lastError = {
            type: 'about:blank',
            title: 'Something went wrong',
            detail: 'Please try again.',
            status: (err as any)?.status || 500,
            code: (code || 'internal_error') as any,
          };
        }
        throw this.lastError;
      }
    },

    /**
     * Sign out — §14.
     *
     * Lifecycle per §7: refreshCsrf() → POST /auth/logout → 204 → clear state → router login.
     *
     * §6 Logout Failure Semantics:
     *   If backend logout call fails (network / 5xx), local state is still cleared
     *   (so the user leaves the UI), but we record `SERVER_LOGOUT_NOT_CONFIRMED`
     *   in lastError so the login page can show a warning. We do NOT claim
     *   "logout succeeded" when the server hasn't confirmed. No retry framework.
     */
    async signOut(options?: { silent?: boolean }): Promise<void> {
      const csrf = useCsrfStore();
      let serverConfirmed = false;
      try {
        // §7/§14: Refresh CSRF immediately before logout so token is fresh.
        try { await csrf.refresh(); } catch { /* if refresh fails, try logout anyway */ }
        // Network call — WITH fresh CSRF header, WITH cookie credentials.
        await apiLogout();
        serverConfirmed = true;
      } catch {
        // §6: Backend logout NOT confirmed. Clear local state anyway (UX must leave),
        // but record the warning so login page can surface it.
        serverConfirmed = false;
      } finally {
        // Wipe memory state regardless — user intent is to leave.
        this._resetUnauthenticated();
        csrf.clear();
        if (!serverConfirmed) {
          // Record that server-side session may still be active.
          // The auth cookie (HttpOnly) may still be valid until it expires.
          this.lastError = {
            type: 'about:blank',
            title: '服务端注销未确认',
            detail: '本地已退出，但服务端会话可能仍然有效。建议关闭浏览器以彻底清除会话。',
            status: 0,
            code: 'server_logout_not_confirmed' as any,
          };
        }
      }
      // Go to login (silent flag is for programmatic 401 triggers; don't double-redirect).
      if (!options?.silent) {
        // WEB-PREVIEW-001R2: same fix as ensureListener — useRouter() is
        // illegal in store actions. Use router singleton. No hard reload
        // fallback — that caused post-logout redirect loops when called
        // from listener context.
        router.replace({ name: 'login' }).catch(() => { /* swallow redirect aborts */ });
      }
    },

    /**
     * Company switch — §10.
     *
     * HARD RULE — §10 + negative rule: NEVER just change `frontend companyId`. MUST call
     * POST /company/switch with CSRF. The response returns the new DTO (with new companyId
     * inside the re-minted auth cookie). ONLY then do we update the local user.
     *
     * Rollback semantics: If 403/400, restore the PRE-SWITCH snapshot so no UI "ghost switch"
     * has occurred. Operators never see an optimistic fake switch.
     */
    async changeCompany(targetCompanyId: string): Promise<void> {
      if (!this.user) throw new Error('Not authenticated');
      if (targetCompanyId === this.user.companyId) return;   // no-op

      // §5: Refresh CSRF fresh before the call to avoid stale-token race.
      const csrf = useCsrfStore();
      try { await csrf.refresh(); } catch { /* call may still succeed, rely on auto-retry in http layer */ }

      // Preserve old user snapshot BEFORE call so rollback is atomic.
      this._preSwitchUser = JSON.parse(JSON.stringify(this.user));

      try {
        await apiSwitchCompany({ targetCompanyId });
        // §13: After switch success, re-fetch /me to refresh full current context.
        // The switch endpoint re-mints the auth cookie; /me reads back the authoritative state.
        const refreshedUser = await apiMe();
        this.user = refreshedUser;
        this.lastError = null;
      } catch (err) {
        // FAILURE — restore old state. UI shows the previous Company unchanged.
        if (this._preSwitchUser) {
          this.user = this._preSwitchUser;
        }
        this._preSwitchUser = null;
        const code = (err as any)?.code;
        if (code === 'company_access_denied') {
          this.lastError = {
            type: (err as any)?.type || 'about:blank',
            title: 'Company access denied',
            detail: 'You do not have access to that company.',
            status: 403,
            code: 'company_access_denied',
          };
        } else if (code === 'invalid_company_selection') {
          this.lastError = {
            type: (err as any)?.type || 'about:blank',
            title: 'Company not available',
            detail: 'That company is not available for your account.',
            status: 400,
            code: 'invalid_company_selection',
          };
        } else if (code === 'authentication_required') {
          // session died mid-switch — let global listener handle it
          throw err;
        } else {
          this.lastError = {
            type: 'about:blank',
            title: 'Could not change company',
            detail: 'Please try again.',
            status: (err as any)?.status || 500,
            code: (code || 'internal_error') as any,
          };
        }
        throw this.lastError;
      } finally {
        this._preSwitchUser = null;
      }
    },

    setIntendedRoute(route: string | null): void {
      // Validate: no off-site redirects allowed. Must start with '/' and be same-origin path.
      if (route === null) { this.intendedRoute = null; return; }
      if (typeof route !== 'string' || !route.startsWith('/') || route.startsWith('//')) return;
      this.intendedRoute = route;
    },

    consumeIntendedRoute(): string | null {
      const r = this.intendedRoute;
      this.intendedRoute = null;
      return r;
    },

    clearLastError(): void { this.lastError = null; },

    /** Internal: reset state to clean unauthenticated (no persistence). */
    _resetUnauthenticated(): void {
      this.user = null;
      this.status = 'unauthenticated';
      // NOTE: DO NOT clear intendedRoute here — it's captured BEFORE this call during 401.
      this.lastError = null;
      this._preSwitchUser = null;
    },
  },
});
