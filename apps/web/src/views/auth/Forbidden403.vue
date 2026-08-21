<!--
  403 Forbound Page — §13 Boundary of WEB-001.
  PURPOSE: Show "access denied" when a user is authenticated but not authorized for a page.
  EXPLICITLY OUT OF SCOPE for this gate: Role tree, Permission tree, Button permission, DataScope.
  Do NOT add any links to permission management.
--->
<template>
  <div class="forbidden-wrap">
    <div class="forbidden-card">
      <div class="forbidden-code" aria-hidden="true">403</div>
      <h1 class="forbidden-title">无权访问该页面</h1>
      <p class="forbidden-detail">您的账号已认证，但当前没有访问该资源的权限。</p>
      <ul class="forbidden-tips">
        <li>请确认您选择了正确的公司 / 租户。</li>
        <li>如需申请权限，请联系系统管理员。</li>
      </ul>
      <div class="forbidden-actions">
        <el-button type="primary" @click="goHome">返回首页</el-button>
        <el-button plain @click="goBack">返回上一页</el-button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router';
const router = useRouter();
function goHome() { router.replace('/sales-order').catch(() => {}); }
function goBack() {
  if (window.history.length > 1) router.back();
  else goHome();
}
</script>

<style scoped>
.forbidden-wrap {
  min-height: 100%;
  background: var(--bg-canvas, #F8FAFC);
  display: flex; align-items: center; justify-content: center;
  padding: 60px 20px;
}
.forbidden-card {
  background: var(--bg-container, #FFFFFF);
  border: 1px solid var(--bg-muted, #E2E8F0);
  border-radius: 10px;
  padding: 48px 40px;
  max-width: 520px;
  width: 100%;
  text-align: left;
  box-shadow: 0 1px 2px rgba(15, 23, 42, 0.04), 0 8px 24px rgba(15, 23, 42, 0.04);
}
.forbidden-code {
  font-size: 56px;
  font-weight: 800;
  color: var(--color-blue-500, #0284C7);
  letter-spacing: -1px;
  margin-bottom: 8px;
  line-height: 1;
}
.forbidden-title {
  font-size: 20px; font-weight: 700; color: var(--text-primary, #0F172A);
  margin: 0 0 8px;
}
.forbidden-detail {
  font-size: 13px; color: var(--text-secondary, #334155); margin: 0 0 16px;
}
.forbidden-tips {
  font-size: 12.5px; color: var(--text-muted, #64748B);
  padding-left: 18px; margin: 0 0 24px; line-height: 1.8;
}
.forbidden-actions { display: flex; gap: 10px; }
</style>
