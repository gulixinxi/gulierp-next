<template>
  <!--
    UserMenu — GULIERP_DESIGN_SYSTEM_001_ENTERPRISE_FIORI_THEME (2026-08-23).
    Identity surface for the topbar:
      - Avatar circle with initials (uses Fiori primary gradient)
      - Display name (or username fallback)
      - Role chip (platform admin only; tenant/company role display is M2+)
      - Dropdown with:
          · detail head (name + role + account/tenant/company meta)
          · 个人中心 (M2+, disabled)
          · 修改密码 (预留, disabled — backend not implemented)
          · 退出登录 (danger color, IN dropdown only)

    Design rules (GULIERP_DESIGN_SYSTEM_001):
      - Topbar must remain visually quiet. There is NO standalone
        topbar logout button.
      - Sign-out is a deliberate, two-click intent: open user menu
        → click 退出登录 → ElMessageBox confirm → auth.signOut().
      - Dropdown text uses the dark-text-on-light-popper pair
        (text-primary on bg-container).
  -->
  <el-dropdown
    trigger="click"
    popper-class="gs-user-menu-popper"
    :teleported="true"
    @command="onCommand"
  >
    <button class="gs-user-chip" type="button" :aria-label="ariaLabel">
      <el-avatar :size="28" class="gs-user-avatar">{{ initials }}</el-avatar>
      <span class="gs-user-name">{{ displayName || '—' }}</span>
      <el-tag
        v-if="roleLabel"
        size="small"
        type="info"
        effect="plain"
        class="gs-user-role"
      >{{ roleLabel }}</el-tag>
      <el-icon class="gs-org-chevron"><ArrowDown /></el-icon>
    </button>
    <template #dropdown>
      <el-dropdown-menu class="gs-user-dropdown-menu">
        <!-- Detail head: name + role + account/tenant/company -->
        <el-dropdown-item disabled class="gs-user-detail-item">
          <div class="gs-user-detail-head">
            <el-avatar :size="44" class="gs-user-avatar gs-user-avatar--lg">{{ initials }}</el-avatar>
            <div class="gs-user-detail-text">
              <div class="gs-user-detail-name">{{ displayName || '—' }}</div>
              <div v-if="roleLabel" class="gs-user-detail-role">{{ roleLabel }}</div>
              <div v-if="userName" class="gs-user-detail-meta">账号：{{ userName }}</div>
              <div v-if="tenantName" class="gs-user-detail-meta">租户：{{ tenantName }}</div>
              <div v-if="companyName" class="gs-user-detail-meta">公司：{{ companyName }}</div>
            </div>
          </div>
        </el-dropdown-item>

        <el-dropdown-item disabled command="profile">
          <el-icon><User /></el-icon>
          <span>个人中心</span>
          <span class="gs-user-menu-tag">M2+</span>
        </el-dropdown-item>

        <el-dropdown-item disabled command="change-password">
          <el-icon><Lock /></el-icon>
          <span>修改密码</span>
          <span class="gs-user-menu-tag">预留</span>
        </el-dropdown-item>

        <el-dropdown-item divided command="logout" :disabled="logoutLoading" class="gs-user-menu-logout">
          <el-icon><SwitchButton /></el-icon>
          <span>{{ logoutLoading ? '退出中…' : '退出登录' }}</span>
        </el-dropdown-item>
      </el-dropdown-menu>
    </template>
  </el-dropdown>
</template>

<script setup lang="ts">
// GULIERP_DESIGN_SYSTEM_001_ENTERPRISE_FIORI_THEME.
// Logout is owned by this component (per spec: topbar stays quiet).
// auth.signOut() handles CSRF refresh + POST /auth/logout + state clear +
// router.replace('/login'). Server-side failures surface as ElMessage.error
// inside signOut(); the local `finally` un-sticks the button either way.
import { computed, ref } from 'vue';
import { ElMessageBox } from 'element-plus';
import { ArrowDown, SwitchButton, User, Lock } from '@element-plus/icons-vue';
import { useAuthStore } from '../../stores/auth';

const auth = useAuthStore();
const logoutLoading = ref(false);

const displayName = computed<string>(() => auth.displayName);
const userName = computed<string>(() => auth.userName);
const tenantName = computed<string>(() => auth.tenantName);
const companyName = computed<string>(() => auth.companyName);

// ariaLabel: short description for the topbar trigger (used by screen readers).
const ariaLabel = computed<string>(() => {
  const who = displayName.value || userName.value || '未知用户';
  return `用户菜单 (${who})`;
});

// Avatar initials:
//   - Chinese display name (length >= 2): last 2 characters
//   - English display name (whitespace-separated): first letter of first 2 words
//   - Fallback: first 2 characters
const initials = computed<string>(() => {
  const name = (displayName.value || userName.value || '').trim();
  if (!name) return '?';
  if (/[\u4e00-\u9fa5]/.test(name)) {
    return name.length >= 2 ? name.slice(-2) : name;
  }
  const parts = name.split(/\s+/).filter(Boolean);
  if (parts.length >= 2) {
    return (parts[0][0] + parts[1][0]).toUpperCase();
  }
  return name.slice(0, 2).toUpperCase();
});

// Role chip. Today only Platform Admin gets a chip (auth.isPlatformAdmin).
// Tenant / company role display is deferred to M2+ alongside the user-center.
const roleLabel = computed<string>(() => {
  if (auth.user?.isPlatformAdmin) return '管理员';
  return '';
});

async function onCommand(cmd: string): Promise<void> {
  if (cmd === 'logout') {
    await onLogout();
    return;
  }
  // profile / change-password are reserved (M2+); the dropdown items
  // are disabled and the user is not expected to reach here, but if
  // they do, do nothing.
}

async function onLogout(): Promise<void> {
  if (logoutLoading.value) return;
  try {
    await ElMessageBox.confirm('确认要退出当前账号吗？', '退出登录', {
      confirmButtonText: '退出',
      cancelButtonText: '取消',
      type: 'warning',
    });
  } catch {
    return; // user cancelled the confirm dialog
  }
  logoutLoading.value = true;
  try {
    await auth.signOut();
  } finally {
    logoutLoading.value = false;
  }
}
</script>

<style scoped>
.gs-user-chip {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  height: 32px;
  padding: 0 10px;
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
.gs-user-avatar {
  background: linear-gradient(135deg, #0A6ED1 0%, #085CAF 100%);
  color: #fff;
  font-weight: 600;
  font-size: 12px;
  letter-spacing: 0.5px;
}
.gs-user-avatar--lg {
  font-size: 16px;
  width: 44px !important;
  height: 44px !important;
}
.gs-user-name {
  font-size: 13px;
  max-width: 140px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  color: var(--header-fg);
}
.gs-user-role {
  margin-left: 4px;
  background: rgba(255, 255, 255, 0.16) !important;
  color: var(--header-fg) !important;
  border-color: var(--header-border) !important;
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
  min-width: 280px;
  padding: 4px 0;
}
.gs-user-menu-popper .gs-user-detail-item {
  cursor: default;
  padding: 12px 16px !important;
}
.gs-user-menu-popper .gs-user-detail-item:hover {
  background: transparent !important;
}
.gs-user-menu-popper .gs-user-detail-head {
  display: flex;
  align-items: center;
  gap: 12px;
}
.gs-user-menu-popper .gs-user-detail-text {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}
.gs-user-menu-popper .gs-user-detail-name {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
  line-height: 1.4;
}
.gs-user-menu-popper .gs-user-detail-role {
  font-size: 12px;
  font-weight: 600;
  color: var(--primary-default);
  line-height: 1.4;
  letter-spacing: 0.2px;
}
.gs-user-menu-popper .gs-user-detail-meta {
  font-size: 12px;
  color: var(--text-secondary);
  line-height: 1.5;
}
.gs-user-menu-popper .gs-user-menu-tag {
  margin-left: auto;
  font-size: 11px;
  padding: 1px 6px;
  border-radius: 3px;
  background: var(--bg-subtle);
  color: var(--text-muted);
  font-weight: 500;
}
.gs-user-menu-popper .gs-user-menu-logout {
  color: var(--danger-default) !important;
}
.gs-user-menu-popper .gs-user-menu-logout:hover {
  background: var(--danger-bg) !important;
  color: var(--danger-hover) !important;
}
.gs-user-menu-popper .gs-user-menu-logout .el-icon {
  color: var(--danger-default);
}
</style>
