<template>
  <!-- No-flash bootstrap splash: renders BEFORE the first navigation paints. -->
  <Transition name="fade" mode="out-in">
    <div v-if="booting" key="boot" class="boot-root" role="status" aria-live="polite">
      <div class="boot-card">
        <div class="boot-logo">谷</div>
        <div class="boot-title">GuliERP</div>
        <div class="boot-sub">正在连接安全会话…</div>
        <div class="boot-spinner" aria-hidden="true"></div>
        <div v-if="bootError" class="boot-error">
          <!-- WEB-PREVIEW-001R1: localized boot-error titles. NEVER pass the raw
               English lastError.title straight through — classifyError keeps the
               canonical enumeration in EN but the UI contract is Chinese. -->
          <p class="boot-error-title">{{ localizedBootErrorTitle }}</p>
          <p v-if="localizedBootErrorDetail" class="boot-error-detail">{{ localizedBootErrorDetail }}</p>
          <button class="boot-retry" type="button" @click="retryBoot">重新连接</button>
        </div>
      </div>
    </div>
    <Suspense v-else key="app">
      <template #default>
        <router-view v-slot="{ Component }">
          <component :is="Component" />
        </router-view>
      </template>
      <template #fallback>
        <div class="boot-root">
          <div class="boot-card">
            <div class="boot-spinner" aria-hidden="true"></div>
            <div class="boot-sub">加载中…</div>
          </div>
        </div>
      </template>
    </Suspense>
  </Transition>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useAuthStore } from './stores/auth';

const auth = useAuthStore();
// main.ts calls auth.bootstrap() BEFORE mount; for defense-in-depth we also guard here.
const booting = computed(() => !auth.initialized);
const bootError = computed(() => {
  if (auth.status === 'error') return auth.lastError;
  return null;
});

// WEB-PREVIEW-001R1: canonical code → Chinese localized title/detail.
// The lastError.title from classifyError is EN (it stays that way because
// cross-layer contracts use EN codes). The splash UI always displays 中文.
const localizedBootErrorTitle = computed(() => {
  const code = bootError.value?.code;
  if (code === 'service_unavailable') return '服务暂时不可用';
  if (code === 'internal_error') return '连接时出现错误';
  if (code === 'validation_failed') return '请求参数错误';
  if (code === 'company_access_denied') return '访问被拒绝';
  return '连接时出现错误';
});
const localizedBootErrorDetail = computed(() => {
  const code = bootError.value?.code;
  if (code === 'service_unavailable') return '无法连接到后端服务器，请检查后端服务是否已启动。';
  return bootError.value?.detail || '';
});

async function retryBoot() {
  auth.clearLastError();
  try {
    await auth.bootstrap();
  } catch {
    /* state is set inside bootstrap */
  }
}

// Programmatic kick-in — used only if main.ts's pre-mount bootstrap was
// interrupted. Idempotent; bootstrap() early-returns once initialized.
onMounted(async () => {
  if (!auth.initialized) await auth.bootstrap();
});
</script>

<style>
/* Bootstrap splash lives at global CSS so it paints *instantly* before any JS runs. */
.boot-root {
  position: fixed; inset: 0;
  background: var(--bg-canvas, #F8FAFC);
  display: flex; align-items: center; justify-content: center;
  z-index: 9999;
}
.boot-card { text-align: center; max-width: 320px; padding: 24px; }
.boot-logo {
  width: 56px; height: 56px; border-radius: 12px; margin: 0 auto 16px;
  background: linear-gradient(135deg, var(--color-blue-500, #0284C7), var(--color-blue-700, #075985));
  color: #fff; font-size: 28px; font-weight: 700;
  display: flex; align-items: center; justify-content: center;
  box-shadow: 0 8px 24px rgba(2, 132, 199, 0.25);
}
.boot-title {
  font-size: 22px; font-weight: 700; color: var(--text-primary, #0F172A);
  margin-bottom: 4px; letter-spacing: 0.5px;
}
.boot-sub { font-size: 13px; color: var(--text-muted, #64748B); }
.boot-spinner {
  width: 28px; height: 28px; margin: 20px auto 0;
  border: 2px solid var(--bg-muted, #E2E8F0);
  border-top-color: var(--color-blue-500, #0284C7);
  border-radius: 50%; animation: boot-spin 0.9s linear infinite;
}
@keyframes boot-spin { to { transform: rotate(360deg); } }

.boot-error { margin-top: 18px; padding: 12px; border-radius: 8px; background: #FEF2F2; border: 1px solid #FECACA; }
.boot-error-title { margin: 0 0 4px; font-size: 13px; font-weight: 600; color: var(--status-danger, #DC2626); }
.boot-error-detail { margin: 0 0 10px; font-size: 12px; color: #B91C1C; }
.boot-retry {
  appearance: none; border: 0; cursor: pointer;
  background: var(--color-blue-500, #0284C7); color: #fff;
  padding: 6px 14px; border-radius: 6px; font-size: 12.5px; font-weight: 500;
}
.boot-retry:hover { background: var(--color-blue-600, #0369A1); }

.fade-enter-active, .fade-leave-active { transition: opacity 0.15s ease; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
</style>

