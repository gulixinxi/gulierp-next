import ElementPlus from 'element-plus';
import 'element-plus/dist/index.css';
import { createPinia } from 'pinia';
import { createApp } from 'vue';
import App from './App.vue';
import { router } from './router';
import './styles.css';
import { useAuthStore } from './stores/auth';
import { useCsrfStore } from './stores/csrf';

async function start() {
  const app = createApp(App);
  const pinia = createPinia();
  app.use(pinia);
  app.use(router);
  app.use(ElementPlus);

  // Register the global 401 listener early (before bootstrap) so any call failure
  // during /me gets routed correctly.
  const auth = useAuthStore();
  auth.ensureListener();

  // Pre-load CSRF first (idempotent) — credentials-include ensures the shared
  // antiforgery cookie is set for the initial /me and subsequent login call.
  try {
    const csrf = useCsrfStore();
    // Only fetch CSRF if we don't already have one (no-op on subsequent /me retries).
    // This is NOT required for GET /me (GET is safe method per §2.2), but seeding the
    // token here avoids a second round-trip right before the first POST /login.
    await csrf.ensure().catch(() => { /* swallow; bootstrap() handles errors */ });
  } catch { /* ignore — bootstrap() below is what gates the UI */ }

  // Resolve /me once BEFORE mounting. This is §8 "no flash" critical guarantee:
  // the splash screen in App.vue paints until `initialized===true`. We resolve auth state
  // synchronously at mount-time so the first rendered route matches auth state.
  await auth.bootstrap();

  app.mount('#app');
}

start();

