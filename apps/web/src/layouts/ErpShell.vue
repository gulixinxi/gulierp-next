<template>
  <!-- ErpShell — Two-level ERP nav: Module Rail (60px) + Secondary Menu (resizable)
       Multi-Tab + Document Fullscreen main shell (DEC-UX-001, FROZEN)
       GULIERP_DESIGN_SYSTEM_001_ENTERPRISE_FIORI_THEME:
         - Topbar: solid enterprise blue (#0A6ED1) with white text
         - Sidebar (rail + secondary): slate #354A5F
         - Brand line: "GuliERP" only (GuliERP Next / G1B-1R3 Design System
           labels removed by design system spec) -->
  <div class="gs-shell" :class="{ 'is-fullscreen': tabs.fullscreen }">
    <!-- Top brand bar -->
    <header v-show="!tabs.fullscreen" class="gs-topbar">
      <div class="brand">
        <div class="brand-logo">谷</div>
        <div class="brand-text">
          <div class="brand-title">GuliERP</div>
        </div>
      </div>
      <div class="gs-topbar-center">
        <el-popover
          v-model:visible="searchVisible"
          placement="bottom-start"
          trigger="click"
          width="380"
          popper-class="gs-search-popover"
        >
          <template #reference>
            <button class="gs-search-trigger" type="button">
              <el-icon><Search /></el-icon>
              <span>搜索客户 / 单据 / 物料 / 供应商</span>
            </button>
          </template>
          <div class="gs-search-panel">
            <el-input
              v-model="menuSearch"
              placeholder="搜索功能入口"
              clearable
              :prefix-icon="Search"
              @keyup.enter="openFirstSearchResult"
            />
            <div class="gs-search-results">
              <button
                v-for="item in searchedMenuItems"
                :key="item.id"
                class="gs-search-result"
                type="button"
                :disabled="item.disabled"
                @click="openNavigationItem(item)"
              >
                <el-icon><component :is="resolveIcon(item.icon)" /></el-icon>
                <span>{{ item.label }}</span>
                <small v-if="item.disabled">{{ item.placeholder || '待开发' }}</small>
              </button>
              <div v-if="searchedMenuItems.length === 0" class="gs-search-empty">没有匹配的入口</div>
            </div>
          </div>
        </el-popover>
      </div>
      <div class="gs-topbar-right">
        <el-popover placement="bottom" trigger="click" width="280">
          <template #reference>
            <el-button text size="small" title="AI 助手">
              <el-icon><MagicStick /></el-icon>
              AI
            </el-button>
          </template>
          <div class="gs-safe-panel">
            <strong>AI 助手</strong>
            <p>待接入，不调用模型或后端接口。</p>
          </div>
        </el-popover>
        <el-popover placement="bottom" trigger="click" width="280">
          <template #reference>
            <el-button text size="small" title="消息">
              <el-icon><Bell /></el-icon>
              消息
            </el-button>
          </template>
          <div class="gs-safe-panel">
            <strong>消息</strong>
            <p>消息中心待开发，当前不展示伪造数量。</p>
          </div>
        </el-popover>

        <!-- Company switch / display -->
        <el-dropdown
          v-if="auth.hasMultipleCompanies"
          trigger="click"
          @command="onChangeCompany"
          :teleported="true"
        >
          <button class="gs-org-chip gs-org-chip--interactive" :class="{ 'is-loading': companyLoading }" type="button" :title="companyTooltip">
            <el-icon><OfficeBuilding /></el-icon>
            <span>{{ companyPrimaryLabel }}</span>
            <el-icon class="gs-org-chevron" :class="{ 'is-spin': companyLoading }">
              <component :is="companyLoading ? Loading : ArrowDown" />
            </el-icon>
          </button>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item
                v-for="c in auth.availableCompanies"
                :key="c.companyId"
                :command="c.companyId"
                :disabled="c.companyId === auth.companyId || companyLoading"
              >
                <span class="gs-coy-name">{{ c.companyName || c.companyCode || ('公司 ' + c.companyId) }}</span>
                <el-tag v-if="c.companyId === auth.companyId" size="small" type="primary" effect="plain">当前</el-tag>
              </el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
        <span v-else class="gs-org-chip" :title="companyTooltip">
          <el-icon><OfficeBuilding /></el-icon>
          <span>{{ companyPrimaryLabel }}</span>
        </span>

        <!--
          User menu: GULIERP_DESIGN_SYSTEM_001 / SHELL_MICRO_FIX (2026-08-23).
          Trigger shows avatar + display name + chevron only (no role chip,
          no duplicate displayName). The dropdown detail head carries the
          role + 账号/租户/公司 meta; the menu items are 个人中心 (M2+)
          and 修改密码 (预留). Sign-out is NOT here — it is the standalone
          topbar button below (Operator decision: enterprise ERP, high
          frequency desktop operation, not hidden in a dropdown).
        -->
        <UserMenu />

        <!--
          Standalone topbar logout (SHELL_MICRO_FIX, 2026-08-23).
          Visual: white text by default (matches topbar), danger color on
          hover only. NOT a permanent red button — the spec is explicit
          about "no always-on red". Click → ElMessageBox confirm →
          auth.signOut() (CSRF refresh + POST /auth/logout + state clear
          + redirect to /login).
        -->
        <el-button
          text
          size="small"
          class="gs-logout-btn"
          :loading="logoutLoading"
          title="退出登录"
          aria-label="退出登录"
          @click="onLogout"
        >
          <el-icon><SwitchButton /></el-icon>
          <span>退出</span>
        </el-button>

        <el-button text size="small" @click="tabs.toggleFullscreen()">
          <el-icon><FullScreen /></el-icon>
          进入全屏编辑
        </el-button>
      </div>
    </header>

    <div class="gs-body">
      <!-- ===== Module Rail (60px, always visible except fullscreen) ===== -->
      <nav v-show="!tabs.fullscreen" class="gs-rail" aria-label="Module navigation">
        <div
          v-for="m in modules"
          :key="m.key"
          class="gs-rail-item"
          :class="{ 'is-active': activeModule === m.key }"
          @click="onModuleClick(m)"
        >
          <el-icon class="gs-rail-icon"><component :is="resolveIcon(m.icon)" /></el-icon>
          <span>{{ m.shortLabel }}</span>
          <span class="gs-rail-tooltip">{{ m.label }}</span>
        </div>
      </nav>

      <!-- ===== Secondary Menu (resizable, collapsible; hidden only when folded) ===== -->
      <aside
        v-show="!tabs.fullscreen"
        class="gs-secondary"
        :class="{ 'is-collapsed': secondaryCollapsed }"
        :style="secondaryStyle"
      >
        <div class="gs-secondary-head">
          <span class="gs-secondary-title">{{ currentModuleLabel }}</span>
        </div>
        <div class="gs-menu-list">
          <template v-for="group in activeModuleNavigation.groups" :key="group.label">
            <div class="gs-menu-group-title">{{ group.label }}</div>
            <button
              v-for="item in group.items"
              :key="item.id"
              class="gs-menu-item"
              type="button"
              :class="{ 'is-active': isNavigationItemActive(item), 'is-disabled': item.disabled }"
              :title="item.disabled ? (item.placeholder || '待开发') : item.label"
              :disabled="item.disabled"
              @click="openNavigationItem(item)"
            >
              <el-icon><component :is="resolveIcon(item.icon)" /></el-icon>
              <span>{{ item.label }}</span>
              <span v-if="item.disabled" class="gs-menu-badge-pending">待开发</span>
            </button>
          </template>
        </div>
        <!-- Resize handle -->
        <div
          v-show="!secondaryCollapsed"
          class="gs-secondary-resizer"
          @mousedown="startResize"
          title="拖动调整二级菜单宽度"
        ></div>
      </aside>

      <!-- ===== Main content with tabs ===== -->
      <main class="gs-main">
        <!-- Tab strip + Top secondary toggle (panel toggle at left of tab strip) -->
        <div class="gs-tabstrip">
          <!-- Top Secondary Toggle (lightweight, left of tabs) -->
          <button
            v-show="!tabs.fullscreen"
            class="gs-paneltoggle"
            @click="toggleSecondary"
            :title="secondaryCollapsed ? '展开菜单' : '收起菜单'"
          >
            <el-icon v-if="secondaryCollapsed"><ArrowRight /></el-icon>
            <el-icon v-else><ArrowLeft /></el-icon>
          </button>
          <!-- Scrollable tab list (overflow-x handles many tabs) -->
          <div class="gs-tab-list">
            <div
              v-for="t in tabs.tabs"
              :key="t.id"
              class="gs-tab"
              :class="{ 'is-active': tabs.activeId === t.id, 'is-dirty': t.dirty }"
              @click="onTabClick(t)"
              @contextmenu.prevent="onTabContextMenu($event, t)"
            >
              <el-icon v-if="t.kind === 'list'" class="gs-tab-icon"><Document /></el-icon>
              <el-icon v-else-if="t.kind === 'edit'" class="gs-tab-icon"><EditPen /></el-icon>
              <el-icon v-else class="gs-tab-icon"><Tickets /></el-icon>
              <span class="gs-tab-title">{{ t.title }}</span>
              <span v-if="t.dirty" class="gs-dirty-dot" title="有未保存更改">●</span>
              <el-icon v-if="t.closable" class="gs-tab-close" @click.stop="onTabClose(t)"><Close /></el-icon>
            </div>
          </div>
          <div class="gs-tabstrip-actions">
            <!-- More menu: batch close (for users who don't know right-click) -->
            <el-dropdown trigger="click" @command="onMoreCommand">
              <el-button text size="small" title="批量关闭">
                <el-icon><MoreFilled /></el-icon>
              </el-button>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item command="closeCurrent" :disabled="!currentTabClosable">关闭当前</el-dropdown-item>
                  <el-dropdown-item command="closeLeft" :disabled="!canCloseLeft">关闭左侧</el-dropdown-item>
                  <el-dropdown-item command="closeRight" :disabled="!canCloseRight">关闭右侧</el-dropdown-item>
                  <el-dropdown-item command="closeOthers" :disabled="!canCloseOthers">关闭其他</el-dropdown-item>
                  <el-dropdown-item command="closeAll" divided>关闭全部</el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
            <el-button text size="small" @click="tabs.toggleFullscreen()">
              <el-icon><FullScreen /></el-icon>
              <span v-if="tabs.fullscreen">退出全屏</span><span v-else>全屏</span>
            </el-button>
            <el-button text size="small" @click="refreshCurrent">
              <el-icon><RefreshRight /></el-icon>
            </el-button>
          </div>
        </div>

        <!-- Tab context menu (right-click) -->
        <Teleport to="body">
          <div
            v-if="ctxMenu.visible"
            class="gs-tab-ctxmenu"
            :style="{ left: ctxMenu.x + 'px', top: ctxMenu.y + 'px' }"
            @click.stop
          >
            <div class="gs-ctxmenu-item" :class="{ 'is-disabled': !ctxMenu.tab?.closable }" @click="ctxAction('closeCurrent')">关闭当前</div>
            <div class="gs-ctxmenu-item" :class="{ 'is-disabled': !canCloseLeftCtx }" @click="ctxAction('closeLeft')">关闭左侧</div>
            <div class="gs-ctxmenu-item" :class="{ 'is-disabled': !canCloseRightCtx }" @click="ctxAction('closeRight')">关闭右侧</div>
            <div class="gs-ctxmenu-item" :class="{ 'is-disabled': !canCloseOthersCtx }" @click="ctxAction('closeOthers')">关闭其他</div>
            <div class="gs-ctxmenu-divider"></div>
            <div class="gs-ctxmenu-item" @click="ctxAction('closeAll')">关闭全部</div>
          </div>
        </Teleport>

        <!-- Tab content -->
        <div class="gs-content">
          <router-view :key="tabs.activeId" />
        </div>
      </main>
    </div>

    <!-- Dirty-close confirm -->
    <el-dialog v-model="dirtyConfirm.visible" title="存在未保存的更改" width="420px">
      <p>当前 Tab "{{ dirtyConfirm.title }}" 存在未保存的更改,确认要关闭吗?</p>
      <p style="color:var(--text-muted);font-size:13px">关闭后未保存的修改将丢失。</p>
      <template #footer>
        <el-button @click="dirtyConfirm.visible = false">取消</el-button>
        <el-button type="danger" @click="forceClose">放弃并关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, reactive, ref, onBeforeUnmount, onMounted, watch } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useTabsStore, type ErpTab } from '../stores/tabs';
import { useAuthStore } from '../stores/auth';
import UserMenu from '../components/layout/UserMenu.vue';
import {
  HomeFilled, Sell, ShoppingCart, Box, Coin, Setting, Tools,
  Document, EditPen, Tickets, Close, FullScreen, Bell,
  UserFilled, ArrowLeft, ArrowRight, Van, Money, DataLine,
  TrendCharts, Goods, OfficeBuilding as _OB, Avatar, Files, RefreshRight,
  SwitchButton, ArrowDown, Loading, ScaleToOriginal, MoreFilled,
  Search, MagicStick
} from '@element-plus/icons-vue';
import {
  findNavigationItemByRoute,
  findNavigationModuleByRoute,
  shellNavigation,
  type ShellNavigationItem,
  type ShellNavigationModule,
} from '../layout/navigation';

// Alias: OfficeBuilding icon is used in template as <OfficeBuilding />
// (we re-export with that exact name since we import it as _OB to avoid collision with the component tag)
const OfficeBuilding = _OB;

const tabs = useTabsStore();
const router = useRouter();
const route = useRoute();
const auth = useAuthStore();

const dirtyConfirm = reactive({ visible: false, title: '', id: '' });
const companyLoading = ref(false);
const searchVisible = ref(false);
const menuSearch = ref('');
const companyPrimaryLabel = computed(() =>
  stripTrailingCompanyCode(auth.companyName, auth.companyCode)
  || auth.companyCode
  || '公司');
const companyTooltip = computed(() =>
  auth.companyCode && companyPrimaryLabel.value !== auth.companyCode
    ? `${companyPrimaryLabel.value} (${auth.companyCode})`
    : companyPrimaryLabel.value);

function stripTrailingCompanyCode(name?: string, code?: string): string {
  const text = (name || '').trim();
  const suffix = (code || '').trim();
  if (!text || !suffix) return text;
  const ascii = ` (${suffix})`;
  const full = `（${suffix}）`;
  if (text.endsWith(ascii)) return text.slice(0, -ascii.length).trim();
  if (text.endsWith(full)) return text.slice(0, -full.length).trim();
  return text;
}

// ===== Top bar actions: Company switch + Standalone logout =====
async function onChangeCompany(companyId: string): Promise<void> {
  if (companyLoading.value || companyId === auth.companyId) return;
  companyLoading.value = true;
  try {
    await auth.changeCompany(companyId);
    ElMessage.success({
      message: `已切换到 ${auth.companyName || '公司 ' + companyId}`,
      duration: 2000,
    });
  } catch (err) {
    const title = (err as any)?.title || '切换失败';
    ElMessage.error({ message: title, duration: 2500 });
  } finally {
    companyLoading.value = false;
  }
}

// SHELL_MICRO_FIX: standalone topbar logout (GULIERP_DESIGN_SYSTEM_001).
// Visual: white text default, danger color on hover only (see
// .gs-logout-btn). auth.signOut() handles CSRF refresh + POST /auth/logout
// + state clear + router.replace('/login'). Local `finally` ensures the
// button un-stucks regardless of signOut failure.
const logoutLoading = ref(false);
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



const iconMap = {
  HomeFilled, Sell, ShoppingCart, Box, Coin, Setting, Tools,
  Document, Tickets, Bell, UserFilled, Van, Money, DataLine,
  TrendCharts, Goods, OfficeBuilding, Avatar, Files, ScaleToOriginal,
};

const modules = computed(() => shellNavigation.filter(canAccessModule));
const activeModule = ref<ShellNavigationModule['key']>('sales');
const activeModuleNavigation = computed(() =>
  modules.value.find(m => m.key === activeModule.value) ?? modules.value[0] ?? shellNavigation[0]);
const currentModuleLabel = computed(() => activeModuleNavigation.value?.label || '');
const allNavigationItems = computed(() =>
  modules.value.flatMap(module => module.groups.flatMap(group => group.items.filter(canAccessItem))));
const searchedMenuItems = computed(() => {
  const query = menuSearch.value.trim().toLowerCase();
  if (!query) return allNavigationItems.value.filter(item => item.route).slice(0, 8);
  return allNavigationItems.value
    .filter(item => item.label.toLowerCase().includes(query) || item.id.toLowerCase().includes(query))
    .slice(0, 8);
});

function resolveIcon(name: string) {
  return iconMap[name as keyof typeof iconMap] ?? Files;
}

function canAccessModule(module: ShellNavigationModule): boolean {
  return !module.permissionKey && !module.disabled;
}

function canAccessItem(item: ShellNavigationItem): boolean {
  return !item.permissionKey;
}

watch(
  () => route.path,
  (p) => {
    if (!p) return;
    const currentModule = findNavigationModuleByRoute(p);
    if (currentModule) {
      activeModule.value = currentModule.key;
    }

    const existingTab = tabs.tabs.find(t => t.route === p);
    if (existingTab) {
      tabs.setActive(existingTab.id);
      return;
    }

    const navItem = findNavigationItemByRoute(p);
    const title = navItem?.tabTitle || navItem?.label || route.meta?.title as string | undefined;
    const id = navItem?.id || (route.name ? `list-${String(route.name)}` : '');
    if (title && id && !p.includes('/login')) {
      tabs.ensureList(id, title, navItem?.tabKind || 'list', p);
    }
  },
  { immediate: true }
);

function onModuleClick(m: ShellNavigationModule) {
  const moduleChanged = m.key !== activeModule.value;
  if (moduleChanged && secondaryCollapsed.value) {
    secondaryCollapsed.value = false;
    localStorage.setItem(LS_COLLAPSE_KEY, '0');
  }
  activeModule.value = m.key;
  const firstRoute = m.groups.flatMap(group => group.items).find(item => item.route && !item.disabled);
  if (firstRoute) {
    openNavigationItem(firstRoute);
  }
}

function isNavigationItemActive(item: ShellNavigationItem): boolean {
  return tabs.activeId === item.id || (!!item.route && route.path.startsWith(item.route));
}

function openNavigationItem(item: ShellNavigationItem): void {
  if (item.disabled || !item.route) {
    ElMessage.info(item.placeholder || '功能待开发');
    return;
  }
  searchVisible.value = false;
  tabs.ensureList(item.id, item.tabTitle || item.label, item.tabKind || 'list', item.route);
  router.push(item.route).catch(() => {});
}

function openFirstSearchResult(): void {
  const item = searchedMenuItems.value.find(i => !i.disabled && i.route);
  if (item) openNavigationItem(item);
}

// ===== Secondary menu width + collapse (persisted via localStorage) =====
// SHELL_FINAL_MICRO_FIX_001 (2026-08-23): the secondary menu is now a
// LIGHT surface (white), and the spec tightens its width to the
// 160–180px range. The Module Rail was also tightened from 60px to 56px
// (see design-system/tokens/sizing.css). Stored values from previous
// sessions are clamped into the new range on load so an old 216/280
// value never reappears.
const LS_WIDTH_KEY    = 'erp.shell.secondaryWidth';
const LS_COLLAPSE_KEY = 'erp.shell.secondaryCollapsed';
const DEFAULT_W = 168;
const MIN_W = 160;
const MAX_W = 180;

// Clamp any out-of-range stored value (e.g. 240px from a
// previous session with the old 216/280 defaults) to the
// new 136–220 range so the sidebar is never wider than MAX_W.
const _storedW = Number(localStorage.getItem(LS_WIDTH_KEY));
const secondaryWidth = ref<number>(
  Number.isNaN(_storedW) || _storedW < MIN_W || _storedW > MAX_W
    ? DEFAULT_W
    : _storedW
);
const secondaryCollapsed = ref<boolean>(localStorage.getItem(LS_COLLAPSE_KEY) === '1');

const secondaryStyle = computed(() => ({
  width: secondaryCollapsed.value ? '0px' : secondaryWidth.value + 'px',
  minWidth: secondaryCollapsed.value ? '0px' : '0px'
}));

let resizing = false;
let startX = 0;
let startW = 0;

function startResize(e: MouseEvent) {
  resizing = true;
  startX = e.clientX;
  startW = secondaryWidth.value;
  document.body.classList.add('menu-resizing');
  document.addEventListener('mousemove', onResizeMove);
  document.addEventListener('mouseup', stopResize);
  e.preventDefault();
}
function onResizeMove(e: MouseEvent) {
  if (!resizing) return;
  const delta = e.clientX - startX;
  secondaryWidth.value = Math.max(MIN_W, Math.min(MAX_W, startW + delta));
}
function stopResize() {
  if (!resizing) return;
  resizing = false;
  document.body.classList.remove('menu-resizing');
  document.removeEventListener('mousemove', onResizeMove);
  document.removeEventListener('mouseup', stopResize);
  localStorage.setItem(LS_WIDTH_KEY, String(secondaryWidth.value));
}
function toggleSecondary() {
  secondaryCollapsed.value = !secondaryCollapsed.value;
  localStorage.setItem(LS_COLLAPSE_KEY, secondaryCollapsed.value ? '1' : '0');
  // Expand returns to user's last remembered width (already in secondaryWidth)
}

onBeforeUnmount(() => { stopResize(); });

// ===== Tab operations =====
function refreshCurrent() {
  const cur = tabs.tabs.find(t => t.id === tabs.activeId);
  if (cur) router.replace(cur.route).catch(() => {});
}
function onTabClick(t: ErpTab) {
  tabs.setActive(t.id);
  router.push(t.route).catch(() => {});
}
function onTabClose(t: ErpTab) {
  if (t.dirty) {
    dirtyConfirm.visible = true;
    dirtyConfirm.title = t.title;
    dirtyConfirm.id = t.id;
    return;
  }
  tabs.closeTab(t.id);
  const next = tabs.tabs.find(t2 => t2.id === tabs.activeId);
  if (next) router.push(next.route).catch(() => {});
}
function forceClose() {
  const id = dirtyConfirm.id;
  dirtyConfirm.visible = false;
  tabs.closeTab(id);
  const next = tabs.tabs.find(t => t.id === tabs.activeId);
  if (next) router.push(next.route).catch(() => {});
}

// ===== Tab context menu (WEB-UX-SHELL-001) =====
const ctxMenu = reactive({
  visible: false,
  x: 0,
  y: 0,
  tab: null as ErpTab | null,
});

// Computed flags for "more" dropdown (based on active tab)
const currentTabClosable = computed(() => {
  const t = tabs.tabs.find(t2 => t2.id === tabs.activeId);
  return t?.closable ?? false;
});
const canCloseLeft = computed(() => {
  const idx = tabs.tabs.findIndex(t => t.id === tabs.activeId);
  return idx > 0 && tabs.tabs.slice(0, idx).some(t => t.closable);
});
const canCloseRight = computed(() => {
  const idx = tabs.tabs.findIndex(t => t.id === tabs.activeId);
  return idx >= 0 && tabs.tabs.slice(idx + 1).some(t => t.closable);
});
const canCloseOthers = computed(() => {
  return tabs.tabs.filter(t => t.closable && t.id !== tabs.activeId).length > 0;
});

// Same flags for the right-click context menu (based on right-clicked tab)
const canCloseLeftCtx = computed(() => {
  if (!ctxMenu.tab) return false;
  const idx = tabs.tabs.findIndex(t => t.id === ctxMenu.tab!.id);
  return idx > 0 && tabs.tabs.slice(0, idx).some(t => t.closable);
});
const canCloseRightCtx = computed(() => {
  if (!ctxMenu.tab) return false;
  const idx = tabs.tabs.findIndex(t => t.id === ctxMenu.tab!.id);
  return idx >= 0 && tabs.tabs.slice(idx + 1).some(t => t.closable);
});
const canCloseOthersCtx = computed(() => {
  if (!ctxMenu.tab) return false;
  return tabs.tabs.filter(t => t.closable && t.id !== ctxMenu.tab!.id).length > 0;
});

function onTabContextMenu(e: MouseEvent, t: ErpTab) {
  // Clamp to viewport so the menu never overflows.
  const x = Math.min(e.clientX, window.innerWidth - 180);
  const y = Math.min(e.clientY, window.innerHeight - 220);
  ctxMenu.visible = true;
  ctxMenu.x = x;
  ctxMenu.y = y;
  ctxMenu.tab = t;
}

function closeCtxMenu() {
  ctxMenu.visible = false;
  ctxMenu.tab = null;
}

function ctxAction(action: 'closeCurrent' | 'closeLeft' | 'closeRight' | 'closeOthers' | 'closeAll') {
  const t = ctxMenu.tab;
  closeCtxMenu();
  if (!t && action !== 'closeAll') return;
  executeBatchClose(action, t?.id || tabs.activeId);
}

function onMoreCommand(cmd: string) {
  executeBatchClose(cmd as any, tabs.activeId);
}

function executeBatchClose(action: 'closeCurrent' | 'closeLeft' | 'closeRight' | 'closeOthers' | 'closeAll', tabId: string) {
  if (action === 'closeCurrent') {
    const tab = tabs.tabs.find(t => t.id === tabId);
    if (!tab?.closable) return;
    if (tab.dirty) {
      dirtyConfirm.visible = true;
      dirtyConfirm.title = tab.title;
      dirtyConfirm.id = tab.id;
      return;
    }
    tabs.closeTab(tabId);
  } else if (action === 'closeLeft') {
    tabs.closeLeft(tabId);
  } else if (action === 'closeRight') {
    tabs.closeRight(tabId);
  } else if (action === 'closeOthers') {
    tabs.closeOthers(tabId);
  } else if (action === 'closeAll') {
    tabs.closeAll();
  }
  // After batch close, navigate to the new active tab.
  const next = tabs.tabs.find(t => t.id === tabs.activeId);
  if (next) router.push(next.route).catch(() => {});
}

// Global listeners: click-outside and Esc to close context menu.
function onGlobalClickClose() { closeCtxMenu(); }
function onGlobalEscClose(e: KeyboardEvent) { if (e.key === 'Escape') closeCtxMenu(); }
onMounted(() => {
  document.addEventListener('click', onGlobalClickClose);
  document.addEventListener('keydown', onGlobalEscClose);
});
onBeforeUnmount(() => {
  document.removeEventListener('click', onGlobalClickClose);
  document.removeEventListener('keydown', onGlobalEscClose);
});
</script>

<style scoped>
/* WEB-001: Top-bar overrides for company switch / user menu.
   The base design tokens live in design-system/components/navigation.css.
   These additions are component-level interaction details. */

.gs-org-chip {
  border: 0;
  background: transparent;
  color: inherit;
  font: inherit;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  white-space: nowrap;
  padding: 2px 8px;
  border-radius: 999px;
}
.gs-org-chip--interactive {
  cursor: pointer;
  transition: background 120ms ease;
}
.gs-org-chip--interactive:hover {
  background: rgba(255, 255, 255, 0.14);
}
.gs-org-chip.is-loading,
.gs-org-chip.is-loading:hover {
  cursor: progress;
  background: rgba(255, 255, 255, 0.05);
}
.gs-org-chevron {
  font-size: 12px;
  opacity: 0.75;
  transition: transform 180ms ease;
}
.gs-org-chevron.is-spin {
  animation: shell-spin 0.9s linear infinite;
}
@keyframes shell-spin { to { transform: rotate(360deg); } }

.gs-user-chip {
  border: 0;
  background: transparent;
  color: inherit;
  font: inherit;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  cursor: pointer;
  padding: 2px 8px;
  border-radius: 999px;
  position: relative;
  z-index: 2;
  transition: background 120ms ease;
}
.gs-user-chip:hover { background: rgba(255, 255, 255, 0.08); }

.gs-user-name { font-weight: 500; }
.gs-user-handle {
  color: #94A3B8;
  font-size: 12px;
  font-family: ui-monospace, SFMono-Regular, Consolas, monospace;
}

.gs-coy-name { margin-right: 10px; }
.gs-search-trigger {
  width: min(360px, 32vw);
  height: 32px;
  border: 1px solid rgba(255, 255, 255, 0.16);
  border-radius: 6px;
  background: rgba(255, 255, 255, 0.08);
  color: #d1d5db;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 0 12px;
  cursor: pointer;
  text-align: left;
}
.gs-search-trigger:hover {
  border-color: rgba(255, 255, 255, 0.28);
  color: #fff;
}
.gs-search-panel {
  display: flex;
  flex-direction: column;
  gap: 10px;
}
.gs-search-results {
  max-height: 280px;
  overflow: auto;
}
.gs-search-result {
  width: 100%;
  border: 0;
  background: transparent;
  color: var(--text-primary, #0f172a);
  display: grid;
  grid-template-columns: 20px 1fr auto;
  align-items: center;
  gap: 8px;
  padding: 8px;
  border-radius: 6px;
  cursor: pointer;
  text-align: left;
}
.gs-search-result:hover {
  background: var(--bg-subtle, #f8fafc);
}
.gs-search-result:disabled {
  color: var(--text-muted, #64748b);
  cursor: not-allowed;
}
.gs-search-result small,
.gs-search-empty {
  color: var(--text-muted, #64748b);
  font-size: 12px;
}
.gs-search-empty {
  padding: 16px 8px;
  text-align: center;
}
.gs-safe-panel {
  line-height: 1.6;
}
.gs-safe-panel p {
  margin: 6px 0 0;
  color: var(--text-muted, #64748b);
  font-size: 13px;
}
.gs-menu-item {
  border: 0;
  width: 100%;
  font: inherit;
}
.gs-user-detail-head {
  line-height: 1.5;
  cursor: default;
  min-width: 180px;
}
.gs-user-detail-name {
  font-weight: 600;
  font-size: 13px;
  color: var(--text-primary, #0F172A);
}
.gs-user-detail-user,
.gs-user-detail-company,
.gs-user-detail-tenant {
  font-size: 12px;
  color: var(--text-muted, #64748B);
  margin-top: 2px;
}

/* SHELL_MICRO_FIX: standalone topbar logout button.
   GULIERP_DESIGN_SYSTEM_001 spec is explicit: NOT a permanent red
   button. Default = white (matches topbar), hover/focus = danger
   color. Loading state keeps the danger tint so the in-flight
   intent is visible. */
.gs-logout-btn {
  color: var(--header-fg) !important;
  margin: 0 4px;
}
.gs-logout-btn:hover,
.gs-logout-btn:focus-visible {
  background: var(--danger-bg) !important;
  color: var(--danger-default) !important;
}
.gs-logout-btn.is-loading,
.gs-logout-btn.is-loading:hover {
  background: var(--danger-bg) !important;
  color: var(--danger-default) !important;
}
</style>

<style>
.gs-user-menu-popper {
  z-index: 3000 !important;
}
</style>
