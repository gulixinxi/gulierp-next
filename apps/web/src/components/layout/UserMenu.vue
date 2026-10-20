<template>
  <!--
    UserMenu — GULIERP_SHELL_FINAL_POLISH_003 (2026-08-23).
    Enterprise ERP identity surface. NOT a SaaS / social product:
    no large colored avatar circles, no first-initial chips, no
    social-style profile card. The trigger is a compact icon +
    display name + chevron; the dropdown is a quiet identity card
    + plain menu items.

    GULIERP_DESIGN_SYSTEM_001 §5 (final polish refinement):
    - Trigger: icon (UserFilled, 16px) + name + chevron only.
      NO avatar circle. NO initials. NO role chip in the topbar.
    - Dropdown detail head: 32px UserFilled icon (auxiliary, not
      a dominant blue circle) + name + @username + tenant + company.
      The role line is removed from the detail head (a Platform
      Admin's role is now implicit; the topbar trigger does not
      show a role chip either, per FINAL POLISH 001).
    - Dropdown items: 个人中心, 修改密码. The "M2+" and "预留"
      dev-state tags are removed — production users should not see
      internal development phase labels.
    - Sign-out: NOT in the dropdown. The standalone topbar logout
      button (in ErpShell.vue) owns sign-out. The auth flow
      (warning confirm -> sign-out helper -> CSRF refresh +
      POST /auth/logout + state clear + redirect to /login) is
      unchanged and not re-implemented here.
  -->
  <el-dropdown
    trigger="click"
    popper-class="gs-user-menu-popper"
    :teleported="true"
    @command="onCommand"
  >
    <button class="gs-user-chip" type="button" :aria-label="ariaLabel">
      <el-icon :size="16" class="gs-user-icon"><UserFilled /></el-icon>
      <span class="gs-user-name">{{ displayName || '—' }}</span>
      <el-icon class="gs-org-chevron"><ArrowDown /></el-icon>
    </button>
    <template #dropdown>
      <el-dropdown-menu class="gs-user-dropdown-menu">
        <!-- Identity head: small icon + name + @handle + tenant/company.
             The icon is intentionally SUBTLE (32px slate, NOT a blue
             gradient circle) so the name remains the visual anchor. -->
        <el-dropdown-item disabled class="gs-user-detail-item">
          <div class="gs-user-detail-head">
            <el-icon :size="32" class="gs-user-detail-icon"><UserFilled /></el-icon>
            <div class="gs-user-detail-text">
              <div class="gs-user-detail-name">{{ displayName || '—' }}</div>
              <div v-if="userName" class="gs-user-detail-handle">@{{ userName }}</div>
              <div v-if="tenantName" class="gs-user-detail-meta">租户：{{ tenantName }}</div>
              <div v-if="companyName" class="gs-user-detail-meta">公司：{{ companyName }}</div>
            </div>
          </div>
        </el-dropdown-item>

        <el-dropdown-item disabled command="profile">
          <el-icon><User /></el-icon>
          <span>个人中心</span>
        </el-dropdown-item>

        <el-dropdown-item command="change-password">
          <el-icon><Lock /></el-icon>
          <span>修改密码</span>
        </el-dropdown-item>
      </el-dropdown-menu>
    </template>
  </el-dropdown>
  <el-dialog v-model="changeDialogVisible" title="修改密码" width="420px" append-to-body @closed="clearPasswords">
    <el-form label-position="top" @submit.prevent="onChangePassword">
      <el-form-item label="当前密码" required>
        <el-input v-model="currentPassword" type="password" show-password autocomplete="current-password" />
      </el-form-item>
      <el-form-item label="新密码" required>
        <el-input v-model="newPassword" type="password" show-password autocomplete="new-password" />
      </el-form-item>
      <el-form-item label="确认新密码" required>
        <el-input v-model="confirmPassword" type="password" show-password autocomplete="new-password" />
      </el-form-item>
      <p class="password-guidance">至少 15 个字符，可使用长密码短语。修改成功后需重新登录。</p>
    </el-form>
    <template #footer>
      <el-button @click="changeDialogVisible = false">取消</el-button>
      <el-button type="primary" :loading="changing" @click="onChangePassword">保存并重新登录</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
// GULIERP_SHELL_FINAL_POLISH_003 (2026-08-23).
// Identity surface only — read-only wrapper around the auth store.
// No business actions, no sign-out (lives in the topbar button),
// no role chip, no initials, no avatar circle.
import { computed, onBeforeUnmount, ref } from 'vue';
import { ArrowDown, User, UserFilled, Lock } from '@element-plus/icons-vue';
import { ElMessage } from 'element-plus';
import { useAuthStore } from '../../stores/auth';
import { changePassword } from '../../api/auth';

const auth = useAuthStore();
const changeDialogVisible = ref(false);
const changing = ref(false);
const currentPassword = ref('');
const newPassword = ref('');
const confirmPassword = ref('');

function clearPasswords(): void {
  currentPassword.value = '';
  newPassword.value = '';
  confirmPassword.value = '';
}

function onCommand(command: string): void {
  if (command === 'change-password') changeDialogVisible.value = true;
}

async function onChangePassword(): Promise<void> {
  if (changing.value) return;
  if (!currentPassword.value || newPassword.value.length < 15 || newPassword.value.length > 128) {
    ElMessage.warning('请填写当前密码和 15–128 个字符的新密码');
    return;
  }
  if (newPassword.value !== confirmPassword.value) {
    ElMessage.warning('两次输入的新密码不一致');
    return;
  }
  changing.value = true;
  try {
    await changePassword(currentPassword.value, newPassword.value);
    clearPasswords();
    changeDialogVisible.value = false;
    ElMessage.success('密码已修改，请重新登录');
    auth.finishPasswordChange();
  } catch (error) {
    const code = (error as { code?: string }).code;
    ElMessage.error(code === 'current_password_invalid'
      ? '当前密码不正确'
      : code === 'password_policy_violation'
        ? '新密码不符合安全要求，请换一个密码短语'
        : '修改失败，请稍后重试');
  } finally {
    changing.value = false;
  }
}

onBeforeUnmount(clearPasswords);

const displayName = computed<string>(() => auth.displayName);
const userName = computed<string>(() => auth.userName);
const tenantName = computed<string>(() => auth.tenantName);
const companyName = computed<string>(() => auth.companyName);

// ariaLabel: short description for the topbar trigger (used by screen readers).
const ariaLabel = computed<string>(() => {
  const who = displayName.value || userName.value || '未知用户';
  return `用户菜单 (${who})`;
});
</script>

<style scoped>
/* Topbar trigger: compact icon + name + chevron. 32px tall to
   match the height of the other topbar controls (logout button,
   company chip). No avatar circle, no role chip. */
.gs-user-chip {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  height: 32px;
  padding: 0 8px;
  border: 0;
  background: transparent;
  border-radius: 4px;
  cursor: pointer;
  color: var(--header-fg);
  font: inherit;
  transition: background 120ms ease;
}
.gs-user-chip:hover,
.gs-user-chip:focus-visible {
  background: var(--header-hover-bg);
  outline: 0;
}
.gs-user-icon {
  color: var(--header-fg-muted, rgba(255, 255, 255, 0.78));
  transition: color 120ms ease;
}
.gs-user-chip:hover .gs-user-icon {
  color: var(--header-fg);
}
.gs-user-name {
  font-size: 13px;
  max-width: 140px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  color: var(--header-fg);
}
.gs-org-chevron {
  color: var(--header-fg);
}
</style>

<style>
/* Dropdown popper lives at the document root (teleported). These
   styles are global so the popper can use them. Keep selectors
   specific enough not to bleed into other Element Plus dropdowns. */
.gs-user-menu-popper .gs-user-dropdown-menu {
  min-width: 260px;
  padding: 4px 0;
  border: 1px solid var(--border-default);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08);
}
.gs-user-menu-popper .gs-user-detail-item {
  cursor: default;
  padding: 14px 16px !important;
}
.gs-user-menu-popper .gs-user-detail-item:hover {
  background: transparent !important;
}
.gs-user-menu-popper .gs-user-detail-head {
  display: flex;
  align-items: center;
  gap: 12px;
}
.gs-user-menu-popper .gs-user-detail-icon {
  /* Subtle slate icon — NOT a blue gradient circle.
     The name is the visual anchor; the icon is a small auxiliary
     marker so the identity line still reads as "person + name". */
  color: var(--text-secondary, #5B738B);
  flex-shrink: 0;
  background: var(--bg-subtle, #F7F8FA);
  border: 1px solid var(--border-subtle, #E5EBF0);
  border-radius: 6px;
  padding: 8px;
  box-sizing: content-box;
}
.gs-user-menu-popper .gs-user-detail-text {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}
.gs-user-menu-popper .gs-user-detail-name {
  font-size: 15px;
  font-weight: 600;
  color: var(--text-primary, #1D2D3E);
  line-height: 1.3;
}
.gs-user-menu-popper .gs-user-detail-handle {
  font-size: 12px;
  color: var(--text-muted, #6B7C8C);
  line-height: 1.4;
  font-family: ui-monospace, SFMono-Regular, Consolas, monospace;
  margin-top: 1px;
}
.gs-user-menu-popper .gs-user-detail-meta {
  font-size: 12px;
  color: var(--text-secondary, #5B738B);
  line-height: 1.5;
}
</style>
