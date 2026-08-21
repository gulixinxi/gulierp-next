import vue from '@vitejs/plugin-vue';
import { defineConfig } from 'vite';

// WEB-PREVIEW-001: minimal dev-only proxy so relative /api calls reach the
// real ASP.NET Core backend. Reuses the existing relative-/api strategy in
// apps/web/src/api/http.ts — no second transport layer.
// Override target via VITE_API_TARGET env var if backend port differs.
const API_TARGET = process.env.VITE_API_TARGET || 'http://127.0.0.1:5000';

export default defineConfig({
  plugins: [vue()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: API_TARGET,
        changeOrigin: true,
        secure: false,
        // Backend may run under a path-prefix-less origin; cookie domain
        // rewriting is handled by changeOrigin + same-origin browser view.
      }
    }
  }
});

