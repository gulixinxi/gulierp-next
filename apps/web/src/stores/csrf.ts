// CSRF Token Store — IN-MEMORY ONLY (NEVER localStorage).
// Follows TRAE_FRONTEND_AUTH_HANDOFF.md §8 pattern exactly.
// Token is server-rotated; the shared antiforgery cookie is HttpOnly (JS-agnostic).
import { defineStore } from 'pinia';
import type { CsrfTokenResponse } from '../types/auth';

const CSRF_ENDPOINT = '/api/v1/auth/csrf';

export const useCsrfStore = defineStore('csrf', {
  state: () => ({
    token: '' as string,
    headerName: 'X-CSRF-TOKEN' as const,
    _initialized: false,
  }),
  getters: {
    ready(state): boolean {
      return state.token.length > 0;
    },
  },
  actions: {
    /**
     * Fetch a fresh antiforgery token. Mandatory before ANY state-changing call.
     * credentials: 'include' ensures the response sets/updates the shared antiforgery cookie.
     * Safe to call multiple times (calling again = rotate).
     */
    async refresh(): Promise<void> {
      const resp = await fetch(CSRF_ENDPOINT, {
        method: 'GET',
        credentials: 'include',       // HARD RULE §2.1 / §9-5
        headers: { 'Accept': 'application/json' },
      });
      if (!resp.ok) {
        throw new Error(`CSRF refresh failed: HTTP ${resp.status}`);
      }
      const data = (await resp.json()) as CsrfTokenResponse;
      if (!data?.requestToken || !data?.headerName) {
        throw new Error('CSRF response missing requestToken/headerName');
      }
      this.token = data.requestToken;
      this.headerName = data.headerName;
      this._initialized = true;
    },

    /** Idempotent ensure — only fetch if currently empty. For bootstrap. */
    async ensure(): Promise<void> {
      if (!this._initialized || this.token.length === 0) {
        await this.refresh();
      }
    },

    /**
     * Wipe the in-memory token (logout / hard 401). The cookie is cleared by the backend
     * (POST /logout → 204), not by this store.
     */
    clear(): void {
      this.token = '';
      this._initialized = false;
    },
  },
});
