<!--
  GuliERP Login Page — Production-grade.
  PC-first. Uses GuliERP Design Tokens (see design-system/tokens/color.css).
  Never: copies Admin.NET visuals, stores password in any form, enumerates credential errors.

  UX behaviors implemented:
  - Client-side required validation (empty username / empty password)
  - Loading spinner + disabled controls during submit
  - Enter key submits when focus is inside username/password
  - 401 invalid_credentials → generic "Invalid username or password" (enumeration defense DEC-AUTH-006)
  - 503 / network error → "Service unavailable" + Retry button
  - CSRF session expired → clear message + retry login
  - Success → router replace to intended route or /sales-order
--->
<template>
  <div class="login-canvas">
    <div class="login-rail-decoration" aria-hidden="true">
      <div class="rail-inner">
        <div class="brand-mark">谷</div>
        <div class="brand-text">
          <div class="brand-title">GuliERP</div>
          <div class="brand-subtitle">企业资源计划 · 销售版</div>
        </div>
        <div class="rail-footnote">
          Secure Sign-in · Cookie-based Authentication<br />
          XSRF-Protected · HTTPS Only
        </div>
      </div>
    </div>

    <div class="login-form-area">
      <div class="login-card" role="main" aria-labelledby="login-title">
        <header class="login-head">
          <h1 id="login-title" class="login-title">欢迎登录</h1>
          <p class="login-sub">请使用您的账号登录 GuliERP</p>
        </header>

        <el-alert
          v-if="lastError && serviceUnavailable"
          type="error"
          :closable="false"
          show-icon
          class="login-alert"
          title="服务暂时不可用"
          description="无法连接到后端服务器，请确认后端服务已启动后点击 重新连接。"
        >
          <template #default>
            <el-button class="mt-2" size="small" type="primary" plain @click="tryReload">重新连接</el-button>
          </template>
        </el-alert>

        <el-alert
          v-else-if="lastError && csrfExpired"
          type="warning"
          :closable="false"
          show-icon
          class="login-alert"
          title="安全会话已过期"
          description="页面停留太久，请重新登录。"
        />

        <el-alert
          v-else-if="lastError && invalidCreds"
          type="error"
          :closable="false"
          show-icon
          class="login-alert"
          title="用户名或密码错误"
        />

        <el-alert
          v-else-if="lastError"
          type="error"
          :closable="false"
          show-icon
          class="login-alert"
          :title="fallbackErrorTitle"
        />

        <el-form
          ref="formRef"
          :model="form"
          :rules="rules"
          label-position="top"
          size="default"
          class="login-form"
          @keyup.enter="onSubmit"
          @submit.prevent="onSubmit"
        >
          <el-form-item label="用户名" prop="userName" required>
            <el-input
              v-model="form.userName"
              placeholder="请输入用户名"
              autocomplete="username"
              :disabled="loading"
              clearable
            />
          </el-form-item>

          <el-form-item label="密码" prop="password" required>
            <el-input
              v-model="form.password"
              type="password"
              placeholder="请输入密码"
              autocomplete="current-password"
              show-password
              :disabled="loading"
            />
          </el-form-item>

          <!-- Tenant code — optional per backend contract (LoginRequest.TenantCode = string? = null).
               Null triggers backend demo tenant fallback (IdentitySeed.DemoTenantCode).
               Users enter their real tenant code in production. No hardcoded default. -->
          <el-form-item label="租户代码" prop="tenantCode">
            <el-input
              v-model="form.tenantCode"
              placeholder="留空使用默认租户"
              autocomplete="organization"
              :disabled="loading"
              clearable
              class="tenant-input"
            />
          </el-form-item>

          <el-form-item class="submit-row">
            <el-button
              type="primary"
              size="large"
              class="submit-btn"
              :loading="loading"
              :disabled="loading"
              native-type="submit"
            >
              <span v-if="!loading">登 录</span>
              <span v-else>正在登录…</span>
            </el-button>
          </el-form-item>
        </el-form>

        <footer class="login-foot">
          <span>© 2026 GuliERP · 谷粒 ERP 有限公司</span>
          <span v-if="envHint" class="env-hint">{{ envHint }}</span>
        </footer>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, reactive, ref, watch } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import type { FormInstance, FormRules } from 'element-plus';
import { useAuthStore } from '../../stores/auth';

const router = useRouter();
const route = useRoute();
const auth = useAuthStore();

const formRef = ref<FormInstance>();
const loading = ref(false);

const form = reactive({
  userName: '',
  password: '',
  tenantCode: '',  // empty = null = backend demo fallback (not hardcoded "default")
});

const rules: FormRules = {
  userName: [
    { required: true, message: '请输入用户名', trigger: ['blur', 'submit'] },
    { min: 2, message: '用户名至少 2 个字符', trigger: 'blur' },
  ],
  password: [
    { required: true, message: '请输入密码', trigger: ['blur', 'submit'] },
    { min: 1, message: '请输入密码', trigger: 'blur' },
  ],
};

const lastError = computed(() => auth.lastError);
const invalidCreds = computed(() => lastError.value?.code === 'invalid_credentials');
// WEB-PREVIEW-001R1: treat service_unavailable AND internal_error as the
// localized "服务暂时不可用" banner. Both cases share the same UI semantics:
// "can't reach backend / something went wrong — retry". Enumerated codes we
// know nothing about (unrecognized strings) still fall to the last alert.
const serviceUnavailable = computed(() => {
  const c = lastError.value?.code;
  return c === 'service_unavailable' || c === 'internal_error';
});
const csrfExpired = computed(() => lastError.value?.code === 'csrf_validation_failed');

// WEB-PREVIEW-001R1: final-fallback localized title so the last branch never
// shows raw English strings. The cross-layer canonical titles stay EN in
// stores/API layer; only the Login UI translates them to 中文 (per UX spec).
const fallbackErrorTitle = computed(() => {
  const code = lastError.value?.code;
  if (code === 'company_access_denied') return '访问被拒绝';
  if (code === 'validation_failed') return '表单填写有误';
  if (code === 'server_logout_not_confirmed') return '服务端注销未确认';
  // Unrecognized — safe generic Chinese umbrella. NEVER leak raw EN text.
  return '登录失败，请稍后重试';
});
const envHint = computed(() => {
  const host = window.location.hostname;
  if (host === 'localhost' || host === '127.0.0.1') return 'Local Dev';
  if (host.includes('staging') || host.includes('test')) return 'Staging';
  return '';
});

// If the user lands on /login?redirect=%2Fsales-order%2F3%2Fedit → use it as intended route
watch(() => route.query, (q) => {
  const redirect = q.redirect;
  if (typeof redirect === 'string' && redirect.startsWith('/')) {
    auth.setIntendedRoute(redirect);
  }
}, { immediate: true });

// If already authenticated, fast-forward (e.g. browser back after successful login).
// We don't flash the form.
if (auth.isAuthenticated) {
  const r = auth.consumeIntendedRoute() || '/sales-order';
  router.replace(r).catch(() => {});
}

async function onSubmit(): Promise<void> {
  if (!formRef.value) return;
  auth.clearLastError();
  // Async validate the form first
  let ok = false;
  try { ok = await formRef.value.validate(); } catch { ok = false; }
  if (!ok || loading.value) return;

  loading.value = true;
  try {
    await auth.signIn({
      userName: form.userName,
      password: form.password,
      tenantCode: form.tenantCode.trim() || null,
    });
    // SUCCESS — go to intended route or shell default.
    const target = auth.consumeIntendedRoute() || '/sales-order';
    await router.replace(target).catch(() => {});
  } catch {
    // Error was already classified in auth store (lastError). UI will render via computed above.
    // Clear password ONLY on credential failure (defense-in-depth); keep username for convenience.
    if (invalidCreds.value) form.password = '';
  } finally {
    loading.value = false;
  }
}

function tryReload(): void {
  // Full page reload causes fresh /csrf + clears any stale local state (best recovery for 503).
  window.location.reload();
}

// Security defense-in-depth: never leave password in memory after the component unmounts.
// (Reactive proxy values are observable from devtools; scrubbing reduces window of exposure.)
onBeforeUnmount(() => {
  form.password = '';
  form.userName = '';
});
</script>

<style scoped>
/* ================================================================
   Login page layout. Consumes GuliERP Design Tokens directly
   (no Tailwind). Matches the Rail=#0F172A / Canvas=#F8FAFC spec.
   ================================================================ */
.login-canvas {
  min-height: 100vh;
  width: 100%;
  display: flex;
  background: var(--bg-canvas, #F8FAFC);
}

/* Left 40% decorative rail */
.login-rail-decoration {
  display: none;
}
@media (min-width: 960px) {
  .login-rail-decoration {
    display: flex;
    width: 42%;
    background: var(--color-slate-900, #0F172A);
    color: var(--text-inverse, #FFFFFF);
    padding: 56px 48px;
    flex-direction: column;
    justify-content: space-between;
  }
  .rail-inner { height: 100%; display: flex; flex-direction: column; justify-content: space-between; max-width: 380px; }
  .brand-mark {
    width: 56px; height: 56px; border-radius: 12px;
    background: linear-gradient(135deg, var(--color-blue-500, #0284C7), var(--color-blue-700, #075985));
    display: flex; align-items: center; justify-content: center;
    font-size: 28px; font-weight: 700; color: #fff; letter-spacing: 0.5px;
    margin-bottom: 20px;
    box-shadow: 0 8px 24px rgba(2, 132, 199, 0.25);
  }
  .brand-title { font-size: 28px; font-weight: 700; letter-spacing: 0.5px; }
  .brand-subtitle { font-size: 14px; color: var(--color-slate-400, #94A3B8); margin-top: 4px; }
  .rail-footnote {
    font-size: 12px; line-height: 1.7;
    color: var(--color-slate-500, #64748B);
  }
}

/* Right form area (always 100% on mobile, 58% on desktop) */
.login-form-area {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 32px 20px;
}

.login-card {
  width: 100%;
  max-width: 400px;
  background: var(--bg-container, #FFFFFF);
  border: 1px solid var(--bg-muted, #E2E8F0);
  border-radius: 10px;
  padding: 36px 32px 24px;
  box-shadow: 0 1px 2px rgba(15, 23, 42, 0.04), 0 8px 24px rgba(15, 23, 42, 0.04);
}

.login-head { margin-bottom: 20px; }
.login-title {
  font-size: 22px;
  line-height: 1.3;
  color: var(--text-primary, #0F172A);
  font-weight: 700;
  margin: 0 0 4px;
}
.login-sub {
  font-size: 13px;
  color: var(--text-muted, #64748B);
  margin: 0;
}

.login-alert { margin-bottom: 18px; }

.login-form { width: 100%; }

/* Tenant is rarely changed; make it visually secondary */
.tenant-input :deep(.el-input__wrapper) {
  background: var(--bg-subtle, #F1F5F9);
}
:deep(.el-form-item__label) {
  color: var(--text-secondary, #334155);
  font-weight: 500;
  font-size: 13px;
}

.submit-row { margin-top: 8px; margin-bottom: 4px; }
.submit-btn {
  width: 100%;
  height: 44px;
  font-size: 15px;
  font-weight: 600;
  letter-spacing: 2px;
  /* Design system primary button colors */
  --el-button-bg-color: var(--color-blue-500, #0284C7);
  --el-button-hover-bg-color: var(--color-blue-600, #0369A1);
  --el-button-active-bg-color: var(--color-blue-700, #075985);
  --el-button-border-color: transparent;
  --el-button-hover-border-color: transparent;
  --el-button-active-border-color: transparent;
  border-radius: 6px;
}

.login-foot {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: 24px;
  padding-top: 16px;
  border-top: 1px solid var(--bg-muted, #E2E8F0);
  font-size: 12px;
  color: var(--text-muted, #64748B);
}
.env-hint {
  padding: 2px 8px;
  border-radius: 10px;
  background: var(--bg-subtle, #F1F5F9);
  color: var(--text-muted, #64748B);
  font-family: ui-monospace, SFMono-Regular, Consolas, monospace;
  font-size: 11px;
}
</style>
