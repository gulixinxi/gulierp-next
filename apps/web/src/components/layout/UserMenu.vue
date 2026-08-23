<template>
  <!--
    UserMenu — GULIERP_SALES_ORDER_UI_REBASE_001 / M1 (2026-08-23).
    Encapsulates the top-bar user menu trigger + dropdown.
    Structurally ready for the upcoming logout / user-center work (M2+).
    Today it shows:
      - Avatar circle with initials
      - Display name (or username fallback)
      - Role chip (platform admin only; tenant/company role display is M2+)
      - Dropdown with: detail head, user-center placeholder (disabled), logout
    No business logic: the avatar initials, role text, and dropdown items are
    derived from auth store getters. The logout action is delegated to auth.signOut()
    (which already implements CSRF refresh + POST /auth/logout + state clear + redirect).
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
      <el-dropdown-menu>
        <el-dropdown-item disabled>
          <div class="gs-user-detail-head">
            <el-avatar :size="40" class="gs-user-avatar gs-user-avatar--lg">{{ initials }}</el-avatar>
            <div class="gs-user-detail-text">
              <div class="gs-user-detail-name">{{ displayName || '—' }}</div>
              <div v-if="userName && displayName !== userName" class="gs-user-detail-handle">@{{ userName }}</div>
              <div v-if="tenantName" class="gs-user-detail-meta">租户：{{ tenantName }}</div>
              <div v-if="companyName" class="gs-user-detail-meta">公司：{{ companyName }}</div>
            </div>
          </div>
        </el-dropdown-item>
        <el-dropdown-item disabled command="profile">
          <el-icon><User /></el-icon>
          <span>个人中心 (M2+)</span>
        </el-dropdown-item>
        <el-dropdown-item divided command="logout" :disabled="logoutLoading">
          <el-icon><SwitchButton /></el-icon>
          <span>{{ logoutLoading ? '退出中…' : '退出登录' }}</span>
        </el-dropdown-item>
      </el-dropdown-menu>
    </template>
  </el-dropdown>
</template>

<script setup lang="ts">
// GULIERP_SALES_ORDER_UI_REBASE_001 / M1.
// Read-only wrapper around the auth store. All state mutations go through
// auth.signOut() so the same lifecycle (CSRF refresh + POST + clear + redirect)
// is shared with the 401 listener.
import { computed, ref } from 'vue';
import { ElMessageBox } from 'element-plus';
import { ArrowDown, SwitchButton, User } from '@element-plus/icons-vue';
import { useAuthStore } from '../../stores/auth';

const auth = useAuthStore();
const logoutLoading = ref(false);

const displayName = computed<string>(() => auth.displayName);
const userName = computed<string>(() => auth.userName);
const tenantName = computed<string>(() => auth.tenantName);
const companyName = computed<string>(() => auth.companyName);

// ariaLabel: em-dash + displayName or userName or "未知用户" fallback.
const ariaLabel = computed<string>(() => {
  const who = displayName.value || userName.value || '未知用户';
  return `退出登录 (${who})`;
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
  if (auth.user?.isPlatformAdmin) return '平台管理员';
  return '';
});

async function onCommand(cmd: string): Promise<void> {
  if (cmd === 'logout') {
    await onLogout();
    return;
  }
  if (cmd === 'profile') {
    // M2+ feature: personal center (avatar upload, password change, etc.).
    // Today the item is disabled; no-op.
    return;
  }
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
    return; // user cancelled
  }
  logoutLoading.value = true;
  try {
    // auth.signOut() handles CSRF refresh + POST /auth/logout + state clear +
    // router.replace('/login'). It also surfaces server_logout_not_confirmed
    // to the login page if the backend call fails.
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
  height: 36px;
  padding: 0 10px;
  border: 0;
  background: transparent;
  border-radius: 6px;
  cursor: pointer;
  color: inherit;
  font: inherit;
  transition: background 120ms ease;
}
.gs-user-chip:hover,
.gs-user-chip:focus-visible {
  background: rgba(0, 0, 0, 0.04);
  outline: 0;
}
.gs-user-avatar {
  background: linear-gradient(135deg, #4f6bed, #6b8df0);
  color: #fff;
  font-weight: 600;
  font-size: 12px;
  letter-spacing: 0.5px;
}
.gs-user-avatar--lg {
  font-size: 16px;
}
.gs-user-name {
  font-size: 13px;
  max-width: 140px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.gs-user-role {
  margin-left: 4px;
}
.gs-user-detail-head {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 4px 8px;
  min-width: 240px;
}
.gs-user-detail-text {
  display: flex;
  flex-direction: column;
  gap: 2px;
}
.gs-user-detail-name {
  font-size: 14px;
  font-weight: 600;
  line-height: 1.4;
}
.gs-user-detail-handle {
  font-size: 12px;
  color: #6b7280;
}
.gs-user-detail-meta {
  font-size: 12px;
  color: #6b7280;
}
</style>