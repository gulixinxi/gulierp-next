<template>
  <!--
    UserMenu — GULIERP_SHELL_FINAL_POLISH_001 (2026-08-23).
    Trigger (topbar): 32px avatar (generic UserFilled icon) + display
      name + chevron. The avatar does NOT show initials — the previous
      implementation rendered the first character of the display name
      inside the avatar, which produced a "清" inside the circle next
      to "清清" in the label (visual duplicate of the name's first
      char). A generic icon guarantees zero overlap with the name.
    Dropdown:
      · detail head (56px avatar + name + @username + role + tenant/company)
      · 个人中心 (M2+, disabled)
      · 修改密码 (预留, disabled)
    Sign-out is NOT here — it lives in the standalone topbar
    button (SHELL_MICRO_FIX Operator decision; unchanged in FINAL POLISH).
  -->
  <el-dropdown
    trigger="click"
    popper-class="gs-user-menu-popper"
    :teleported="true"
  >
    <button class="gs-user-chip" type="button" :aria-label="ariaLabel">
      <el-avatar :size="32" class="gs-user-avatar">
        <el-icon class="gs-user-avatar-icon"><UserFilled /></el-icon>
      </el-avatar>
      <span class="gs-user-name">{{ displayName || '—' }}</span>
      <el-icon class="gs-org-chevron"><ArrowDown /></el-icon>
    </button>
    <template #dropdown>
      <el-dropdown-menu class="gs-user-dropdown-menu">
        <!-- Detail head: avatar + name + @username + role + tenant/company. -->
        <el-dropdown-item disabled class="gs-user-detail-item">
          <div class="gs-user-detail-head">
            <el-avatar :size="56" class="gs-user-avatar gs-user-avatar--lg">
              <el-icon class="gs-user-avatar-icon gs-user-avatar-icon--lg"><UserFilled /></el-icon>
            </el-avatar>
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
// GULIERP_SHELL_FINAL_POLISH_001 (2026-08-23).
// FINAL POLISH drops the avatar-initials logic entirely. The trigger
// now shows a generic UserFilled icon inside the avatar circle; the
// display name appears exactly once (in the label). No more
// "first char of name" inside the avatar → no visual duplicate of
// the name's first character. This component remains a pure
// read-only wrapper around the auth store.
import { computed } from 'vue';
import { ArrowDown, User, UserFilled, Lock } from '@element-plus/icons-vue';
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
  display: inline-flex;
  align-items: center;
  justify-content: center;
}
.gs-user-avatar-icon {
  font-size: 18px;
}
.gs-user-avatar-icon--lg {
  font-size: 28px;
}
.gs-user-avatar--lg {
  width: 56px !important;
  height: 56px !important;
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
  padding: 14px 16px !important;
}
.gs-user-menu-popper .gs-user-detail-item:hover {
  background: transparent !important;
}
.gs-user-menu-popper .gs-user-detail-head {
  display: flex;
  align-items: center;
  gap: 14px;
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
