<template>
  <!--
    UserMenu — GULIERP_DESIGN_SYSTEM_001 / SHELL_MICRO_FIX (2026-08-23).
    Trigger (topbar): avatar + display name + chevron only.
      - The role chip is NOT shown here to avoid duplication with
        the dropdown's detail head role line.
      - The display name is shown ONLY in the topbar trigger;
        the dropdown detail head shows the larger identity card.
    Dropdown:
      · detail head (avatar 44 + name + 管理员 + 账号/租户/公司 meta)
      · 个人中心 (M2+, disabled)
      · 修改密码 (预留, disabled)
    Sign-out is NOT here — it lives in the standalone topbar
    button (per SHELL_MICRO_FIX Operator decision).
  -->
  <el-dropdown
    trigger="click"
    popper-class="gs-user-menu-popper"
    :teleported="true"
  >
    <button class="gs-user-chip" type="button" :aria-label="ariaLabel">
      <el-avatar :size="28" class="gs-user-avatar">{{ initials }}</el-avatar>
      <span class="gs-user-name">{{ displayName || '—' }}</span>
      <el-icon class="gs-org-chevron"><ArrowDown /></el-icon>
    </button>
    <template #dropdown>
      <el-dropdown-menu class="gs-user-dropdown-menu">
        <!-- Detail head: avatar + name + @username + role + tenant/company.
             SHELL_FINAL_MICRO_FIX_001: the @username row is shown in
             addition to the name to give the user a clear at-a-glance
             account handle, matching the Operator-specified pattern. -->
        <el-dropdown-item disabled class="gs-user-detail-item">
          <div class="gs-user-detail-head">
            <el-avatar :size="48" class="gs-user-avatar gs-user-avatar--lg">{{ initials }}</el-avatar>
            <div class="gs-user-detail-text">
              <div class="gs-user-detail-name">{{ displayName || '—' }}</div>
              <div v-if="userName" class="gs-user-detail-handle">@{{ userName }}</div>
              <div v-if="roleLabel" class="gs-user-detail-role">{{ roleLabel }}</div>
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
      </el-dropdown-menu>
    </template>
  </el-dropdown>
</template>

<script setup lang="ts">
// GULIERP_DESIGN_SYSTEM_001 / SHELL_MICRO_FIX.
// Identity surface only — no business actions, no sign-out.
// All action plumbing (sign-out, profile, change-password) lives
// in the topbar / future WorkItems. This component is a pure
// read-only wrapper around the auth store for identity display.
import { computed } from 'vue';
import { ArrowDown, User, Lock } from '@element-plus/icons-vue';
import { useAuthStore } from '../../stores/auth';

const auth = useAuthStore();

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
//   - Chinese display name (any length): first 1 character
//     (avoids duplicate-with-name on short Chinese names like "清清")
//   - English display name (whitespace-separated): first letter of first 2 words
//   - Fallback: first 1 character uppercase
const initials = computed<string>(() => {
  const name = (displayName.value || userName.value || '').trim();
  if (!name) return '?';
  if (/[\u4e00-\u9fa5]/.test(name)) {
    return name.slice(0, 1);
  }
  const parts = name.split(/\s+/).filter(Boolean);
  if (parts.length >= 2) {
    return (parts[0][0] + parts[1][0]).toUpperCase();
  }
  return name.slice(0, 1).toUpperCase();
});

// Role chip. Today only Platform Admin gets a chip (auth.isPlatformAdmin).
// Tenant / company role display is deferred to M2+ alongside the user-center.
const roleLabel = computed<string>(() => {
  if (auth.user?.isPlatformAdmin) return '管理员';
  return '';
});
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
  font-size: 18px;
  width: 48px !important;
  height: 48px !important;
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
  font-size: 15px;
  font-weight: 600;
  color: var(--text-primary);
  line-height: 1.3;
}
.gs-user-menu-popper .gs-user-detail-handle {
  font-size: 12px;
  color: var(--text-muted);
  line-height: 1.4;
  font-family: ui-monospace, SFMono-Regular, Consolas, monospace;
  margin-top: 1px;
}
.gs-user-menu-popper .gs-user-detail-role {
  font-size: 11px;
  font-weight: 600;
  color: var(--primary-default);
  line-height: 1.4;
  letter-spacing: 0.2px;
  margin-top: 4px;
  display: inline-block;
  padding: 1px 6px;
  border-radius: 3px;
  background: var(--primary-bg);
  border: 1px solid var(--primary-border);
  align-self: flex-start;
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
</style>
