<template>
  <!-- ErpShell — Two-level ERP nav: Module Rail (60px always) + Secondary Menu (resizable)
       Multi-Tab + Document Fullscreen main shell (DEC-UX-001, FROZEN)
       G1B-1R3: Design System refactor using GuliERP tokens & classes -->
  <div class="gs-shell" :class="{ 'is-fullscreen': tabs.fullscreen }">
    <!-- Top brand bar -->
    <header v-show="!tabs.fullscreen" class="gs-topbar">
      <div class="brand">
        <div class="brand-logo">谷</div>
        <div class="brand-text">
          <div class="brand-title">GuliERP <span class="brand-edition">销售版</span></div>
          <div class="brand-sub">GuliERP Next · G1B-1R3 Design System</div>
        </div>
      </div>
      <div class="gs-topbar-center">
        <!-- Company switch / display -->
        <el-dropdown
          v-if="auth.hasMultipleCompanies"
          trigger="click"
          @command="onChangeCompany"
        >
          <span class="gs-org-chip gs-org-chip--interactive" :class="{ 'is-loading': companyLoading }">
            <el-icon><OfficeBuilding /></el-icon>
            <span>{{ auth.companyName || '公司' + auth.companyId }}</span>
            <el-icon class="gs-org-chevron" :class="{ 'is-spin': companyLoading }">
              <component :is="companyLoading ? Loading : ArrowDown" />
            </el-icon>
          </span>
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
        <span v-else class="gs-org-chip">
          <el-icon><OfficeBuilding /></el-icon>
          <span>{{ auth.companyName || '公司' + auth.companyId }}</span>
        </span>

        <span class="gs-org-sep">/</span>

        <!-- User menu: display name / username + dropdown with logout -->
        <el-dropdown trigger="click" @command="onUserCommand">
          <span class="gs-user-chip">
            <el-icon><UserFilled /></el-icon>
            <span class="gs-user-name">{{ auth.displayName || auth.userName || '—' }}</span>
            <span v-if="auth.userName && auth.displayName && auth.userName !== auth.displayName" class="gs-user-handle">@{{ auth.userName }}</span>
            <el-icon class="gs-org-chevron"><ArrowDown /></el-icon>
          </span>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item disabled>
                <div class="gs-user-detail-head">
                  <div class="gs-user-detail-name">{{ auth.displayName || auth.userName }}</div>
                  <div v-if="auth.tenantName" class="gs-user-detail-tenant">租户：{{ auth.tenantName }}</div>
                </div>
              </el-dropdown-item>
              <el-dropdown-item divided command="logout" :disabled="logoutLoading">
                <el-icon><SwitchButton /></el-icon>
                <span>{{ logoutLoading ? '退出中…' : '退出登录' }}</span>
              </el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </div>
      <div class="gs-topbar-right">
        <el-button text size="small" @click="tabs.toggleFullscreen()">
          <el-icon><FullScreen /></el-icon>
          进入全屏编辑
        </el-button>
        <el-button text size="small">
          <el-icon><Bell /></el-icon>
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
          <el-icon class="gs-rail-icon"><component :is="m.icon" /></el-icon>
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
          <template v-if="activeModule === 'sales'">
            <div class="gs-menu-group-title">销售单据</div>
            <div
              class="gs-menu-item"
              :class="{ 'is-active': tabs.activeId === 'list-sales-order' }"
              @click="openSalesOrderList"
            >
              <el-icon><Tickets /></el-icon>
              <span>销售订单</span>
              <span class="gs-menu-badge">5</span>
            </div>
            <div class="gs-menu-item"><el-icon><Document /></el-icon><span>报价单</span></div>
            <div class="gs-menu-item"><el-icon><Van /></el-icon><span>发货单</span></div>
            <div class="gs-menu-item"><el-icon><Money /></el-icon><span>销售发票</span></div>
            <div class="gs-menu-group-title">销售报表</div>
            <div class="gs-menu-item"><el-icon><DataLine /></el-icon><span>销售业绩</span></div>
            <div class="gs-menu-item"><el-icon><TrendCharts /></el-icon><span>客户账龄</span></div>
          </template>
          <template v-else-if="activeModule === 'basic'">
            <div class="gs-menu-group-title">基础数据</div>
            <div
              class="gs-menu-item"
              :class="{ 'is-active': tabs.activeId === 'list-mdm-items' }"
              @click="openMdmItems"
            >
              <el-icon><Goods /></el-icon><span>商品档案</span>
            </div>
            <!-- MDM-WEB-002: 客户档案 / 供应商 reuse one BusinessPartnerList page;
                 route meta.defaultRole sets the initial role filter (Handoff §5). -->
            <div
              class="gs-menu-item"
              :class="{ 'is-active': tabs.activeId === 'list-mdm-customers' }"
              @click="openCustomers"
            >
              <el-icon><OfficeBuilding /></el-icon><span>客户档案</span>
            </div>
            <div
              class="gs-menu-item"
              :class="{ 'is-active': tabs.activeId === 'list-mdm-suppliers' }"
              @click="openSuppliers"
            >
              <el-icon><Avatar /></el-icon><span>供应商</span>
            </div>
            <div class="gs-menu-item is-disabled" title="员工档案功能待开发">
              <el-icon><UserFilled /></el-icon>
              <span>员工档案</span>
              <span class="gs-menu-badge-pending">待开发</span>
            </div>
            <div
              class="gs-menu-item"
              :class="{ 'is-active': tabs.activeId === 'list-mdm-warehouses' }"
              @click="openWarehouses"
            >
              <el-icon><Box /></el-icon><span>仓库</span>
            </div>
            <div
              class="gs-menu-item"
              :class="{ 'is-active': tabs.activeId === 'list-mdm-locations' }"
              @click="openLocations"
            >
              <el-icon><Files /></el-icon><span>库位</span>
            </div>
          </template>
          <template v-else-if="activeModule === 'mdm'">
            <div class="gs-menu-group-title">主数据</div>
            <div
              class="gs-menu-item"
              :class="{ 'is-active': tabs.activeId === 'list-mdm-uoms' }"
              @click="openMdmUoms"
            >
              <el-icon><ScaleToOriginal /></el-icon>
              <span>计量单位</span>
            </div>
            <div
              class="gs-menu-item"
              :class="{ 'is-active': tabs.activeId === 'list-mdm-item-categories' }"
              @click="openMdmItemCategories"
            >
              <el-icon><Files /></el-icon>
              <span>物料分类</span>
            </div>
            <div
              class="gs-menu-item"
              :class="{ 'is-active': tabs.activeId === 'list-mdm-items' }"
              @click="openMdmItems"
            >
              <el-icon><Goods /></el-icon>
              <span>物料</span>
            </div>
          </template>
          <template v-else>
            <div class="gs-menu-group-title">{{ currentModuleLabel }}</div>
            <div class="gs-menu-item" v-for="s in genericSubmenu" :key="s">
              <el-icon><Files /></el-icon>
              <span>{{ s }}</span>
            </div>
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
import {
  HomeFilled, Sell, ShoppingCart, Box, Coin, Setting, Tools,
  Document, EditPen, Tickets, Close, FullScreen, Bell,
  UserFilled, ArrowLeft, ArrowRight, Van, Money, DataLine,
  TrendCharts, Goods, OfficeBuilding as _OB, Avatar, Files, RefreshRight,
  SwitchButton, ArrowDown, Loading, ScaleToOriginal, MoreFilled
} from '@element-plus/icons-vue';

// Alias: OfficeBuilding icon is used in template as <OfficeBuilding />
// (we re-export with that exact name since we import it as _OB to avoid collision with the component tag)
const OfficeBuilding = _OB;

const tabs = useTabsStore();
const router = useRouter();
const route = useRoute();
const auth = useAuthStore();

const dirtyConfirm = reactive({ visible: false, title: '', id: '' });
const companyLoading = ref(false);
const logoutLoading = ref(false);

// ===== Top bar actions: Company switch + User menu =====
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

async function onUserCommand(cmd: 'logout' | string): Promise<void> {
  if (cmd !== 'logout') return;
  if (logoutLoading.value) return; // prevent double-click
  try {
    await ElMessageBox.confirm('确认要退出当前账号吗？', '退出登录', {
      confirmButtonText: '退出',
      cancelButtonText: '取消',
      type: 'warning',
    });
  } catch { return; /* cancel */ }
  logoutLoading.value = true;
  try {
    // The backend call is inside auth.signOut(). It clears memory state + pushes /login.
    // The auth cookie (HttpOnly) is cleared server-side on 204.
    await auth.signOut();
  } finally {
    logoutLoading.value = false;
  }
}

// ===== Module Rail definition =====
interface RailModule {
  key: 'home' | 'basic' | 'mdm' | 'sales' | 'purchase' | 'inventory' | 'production' | 'quality' | 'system';
  label: string;
  shortLabel: string;
  icon: any;
}
const modules: RailModule[] = [
  { key: 'home',       label: '工作台',   shortLabel: '工作台', icon: HomeFilled },
  { key: 'basic',      label: '基础数据', shortLabel: '基础',   icon: Tools },
  { key: 'mdm',        label: '主数据',   shortLabel: '主数据', icon: Files },
  { key: 'sales',      label: '销售管理', shortLabel: '销售',   icon: Sell },
  { key: 'purchase',   label: '采购管理', shortLabel: '采购',   icon: ShoppingCart },
  { key: 'inventory',  label: '库存管理', shortLabel: '库存',   icon: Box },
  { key: 'production', label: '生产管理', shortLabel: '生产',   icon: Goods },
  { key: 'quality',    label: '质量管理', shortLabel: '质量',   icon: Coin },
  { key: 'system',     label: '系统设置', shortLabel: '系统',   icon: Setting }
];

const activeModule = ref<RailModule['key']>('sales');
const currentModuleLabel = computed(() => modules.find(m => m.key === activeModule.value)?.label || '');
const genericSubmenu = computed(() => ['菜单1', '菜单2', '菜单3', '菜单4', '报表1', '报表2']);

// WEB-PREVIEW-001: keep activeModule in sync with current route so a fresh
// ErpShell mount (e.g. /mdm/* which is a separate top-level route) shows the
// correct secondary menu. immediate:true covers the initial mount case.
// Also syncs/creates tabs on direct URL navigation or browser refresh
// (WEB-UX-SHELL-001 §26: router变化 → active Tab同步).
watch(
  () => route.path,
  (p) => {
    if (!p) return;
    // MDM-001 master data (UOM/ItemCategory/Item) → 主数据 module;
    // MDM-002 BusinessPartner/Warehouse/Location → 基础数据 module, where
    // 客户档案 / 供应商 / 仓库 / 库位 menu items live (so the item the user
    // clicked stays visible + highlighted after navigation).
    if (
      p.startsWith('/mdm/uoms') ||
      p.startsWith('/mdm/item-categories') ||
      p.startsWith('/mdm/items')
    ) {
      activeModule.value = 'mdm';
    } else if (
      p.startsWith('/mdm/business-partners') ||
      p.startsWith('/mdm/customers') ||
      p.startsWith('/mdm/suppliers') ||
      p.startsWith('/mdm/warehouses') ||
      p.startsWith('/mdm/locations')
    ) {
      activeModule.value = 'basic';
    } else if (p.startsWith('/mdm')) {
      activeModule.value = 'mdm';
    } else if (p.startsWith('/sales-order')) {
      activeModule.value = 'sales';
    }

    // Tab sync: if a tab with this route already exists, activate it.
    const existingTab = tabs.tabs.find(t => t.route === p);
    if (existingTab) {
      tabs.setActive(existingTab.id);
      return;
    }
    // Auto-create a list tab for the current route if it has a title
    // in route meta (covers direct URL nav + browser refresh).
    const title = route.meta?.title as string | undefined;
    const name = route.name as string | undefined;
    if (title && name && !p.includes('/login')) {
      tabs.ensureList(`list-${name}`, title, 'list', p);
    }
  },
  { immediate: true }
);

function onModuleClick(m: RailModule) {
  // Rule C/D: switching to a DIFFERENT module auto-expands Secondary;
  // staying on the SAME module preserves any user-initiated collapsed state.
  const moduleChanged = m.key !== activeModule.value;
  if (moduleChanged && secondaryCollapsed.value) {
    secondaryCollapsed.value = false;
    localStorage.setItem(LS_COLLAPSE_KEY, '0');
  }
  activeModule.value = m.key;
  if (m.key === 'sales') openSalesOrderList();
  if (m.key === 'mdm') openMdmItems();
}
function openSalesOrderList() {
  tabs.ensureList('list-sales-order', '销售订单', 'list', '/sales-order');
  router.push('/sales-order').catch(() => {});
}

// ===== MDM navigation (WEB-PREVIEW-001) =====
function openMdmUoms() {
  tabs.ensureList('list-mdm-uoms', '计量单位', 'list', '/mdm/uoms');
  router.push('/mdm/uoms').catch(() => {});
}
function openMdmItemCategories() {
  tabs.ensureList('list-mdm-item-categories', '物料分类', 'list', '/mdm/item-categories');
  router.push('/mdm/item-categories').catch(() => {});
}
function openMdmItems() {
  tabs.ensureList('list-mdm-items', '物料', 'list', '/mdm/items');
  router.push('/mdm/items').catch(() => {});
}

// ===== MDM-WEB-002 navigation: BusinessPartner / Warehouse / Location =====
// 客户档案 / 供应商 reuse the same BusinessPartnerList page; route
// meta.defaultRole sets the initial role filter (Handoff §5 bit-flag).
function openCustomers() {
  tabs.ensureList('list-mdm-customers', '客户档案', 'list', '/mdm/customers');
  router.push('/mdm/customers').catch(() => {});
}
function openSuppliers() {
  tabs.ensureList('list-mdm-suppliers', '供应商', 'list', '/mdm/suppliers');
  router.push('/mdm/suppliers').catch(() => {});
}
function openWarehouses() {
  tabs.ensureList('list-mdm-warehouses', '仓库', 'list', '/mdm/warehouses');
  router.push('/mdm/warehouses').catch(() => {});
}
function openLocations() {
  tabs.ensureList('list-mdm-locations', '库位', 'list', '/mdm/locations');
  router.push('/mdm/locations').catch(() => {});
}

// ===== Secondary menu width + collapse (persisted via localStorage) =====
const LS_WIDTH_KEY    = 'erp.shell.secondaryWidth';
const LS_COLLAPSE_KEY = 'erp.shell.secondaryCollapsed';
// WEB-UX-SHELL-001: sidebar width reduced from 216/180/280 to
// 160/136/220 so the main content area gets more space on
// 1080p / 1366×768 screens. Chinese 4–6 char menu labels
// still render correctly at 136px.
const DEFAULT_W = 160;
const MIN_W = 136;
const MAX_W = 220;

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
  display: inline-flex;
  align-items: center;
  gap: 6px;
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
  cursor: pointer;
  padding: 2px 8px;
  border-radius: 999px;
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
.gs-user-detail-tenant {
  font-size: 12px;
  color: var(--text-muted, #64748B);
  margin-top: 2px;
}
</style>
