// Tabs store — Multi-Tab main interaction (DEC-UX-001, FROZEN)
// Detail pages open in new tabs; list & detail coexist; fullscreen supported.
import { defineStore } from 'pinia';
import { ref } from 'vue';

export interface ErpTab {
  id: string;          // unique tab id
  kind: 'list' | 'detail' | 'edit';
  title: string;
  route: string;       // full path used by router-link/replace
  closable: boolean;
  soId?: string;       // SalesOrder id (detail/edit)
  dirty?: boolean;     // unsaved changes (warning on close)
}

const LIST_TAB: ErpTab = {
  id: 'list-sales-order',
  kind: 'list',
  title: '销售订单',
  route: '/sales-order',
  closable: false
};

export const useTabsStore = defineStore('erp-tabs', () => {
  const tabs = ref<ErpTab[]>([{ ...LIST_TAB }]);
  const activeId = ref<string>(LIST_TAB.id);
  const fullscreen = ref<boolean>(false);

  function setActive(id: string) {
    activeId.value = id;
  }

  function openDetail(soId: string, salesOrderNo: string) {
    const id = `detail-${soId}`;
    const existing = tabs.value.find(t => t.id === id);
    if (existing) {
      activeId.value = existing.id;
      return existing;
    }
    const tab: ErpTab = {
      id,
      kind: 'detail',
      title: salesOrderNo,
      route: `/sales-order/${soId}`,
      closable: true,
      soId
    };
    tabs.value.push(tab);
    activeId.value = tab.id;
    return tab;
  }

  function openEdit(soId: string, salesOrderNo: string, mode: 'create' | 'edit') {
    const id = `edit-${soId}`;
    const existing = tabs.value.find(t => t.id === id);
    if (existing) {
      activeId.value = existing.id;
      return existing;
    }
    const tab: ErpTab = {
      id,
      kind: 'edit',
      title: mode === 'create' ? '新建销售订单' : `编辑 ${salesOrderNo}`,
      route: `/sales-order/${soId}/edit`,
      closable: true,
      soId
    };
    tabs.value.push(tab);
    activeId.value = tab.id;
    return tab;
  }

  function ensureList(id: string, title: string, kind: 'list', route: string) {
    const existing = tabs.value.find(t => t.id === id);
    if (existing) {
      activeId.value = existing.id;
      return existing;
    }
    // List tabs opened AFTER the pinned home are closable.
    // The home tab (first tab, closable:false) is pinned and
    // can never be closed by batch operations.
    const tab: ErpTab = { id, kind, title, route, closable: id !== LIST_TAB.id };
    tabs.value.push(tab);
    activeId.value = tab.id;
    return tab;
  }

  // ===== Batch close operations (WEB-UX-SHELL-001) =====
  // All batch operations respect closable:false (pinned home is preserved).

  function closeLeft(tabId: string) {
    const idx = tabs.value.findIndex(t => t.id === tabId);
    if (idx < 0) return;
    tabs.value = tabs.value.filter((t, i) => i >= idx || !t.closable);
  }

  function closeRight(tabId: string) {
    const idx = tabs.value.findIndex(t => t.id === tabId);
    if (idx < 0) return;
    tabs.value = tabs.value.filter((t, i) => i <= idx || !t.closable);
  }

  function closeOthers(tabId: string) {
    tabs.value = tabs.value.filter(t => t.id === tabId || !t.closable);
    activeId.value = tabId;
  }

  function closeAll() {
    tabs.value = tabs.value.filter(t => !t.closable);
    const first = tabs.value[0];
    activeId.value = first ? first.id : '';
  }

  function closeTab(id: string) {
    const idx = tabs.value.findIndex(t => t.id === id);
    if (idx < 0) return;
    if (!tabs.value[idx].closable) return;
    tabs.value.splice(idx, 1);
    if (activeId.value === id) {
      const prev = tabs.value[Math.max(0, idx - 1)];
      activeId.value = prev.id;
    }
  }

  function markDirty(id: string, dirty: boolean) {
    const t = tabs.value.find(t => t.id === id);
    if (t) t.dirty = dirty;
  }

  function toggleFullscreen(v?: boolean) {
    fullscreen.value = v === undefined ? !fullscreen.value : v;
  }

  return { tabs, activeId, fullscreen, setActive, openDetail, openEdit, ensureList, closeTab, closeLeft, closeRight, closeOthers, closeAll, markDirty, toggleFullscreen };
});
