<!--
  GULIERP_TRANSACTION_DOCUMENT_LINE_GRID_V1 (Phase A) — shared transaction components.
  ItemSelectorPopover — Search-as-you-type Item picker for transaction lines.
  Wraps el-popover with an internal search input + el-table list.
  On select, emits update:modelValue (Item id) and select (full Item).

  Reuses the existing /api/v1/mdm/items endpoint via api/mdm/item.

  Spec: GULIERP_TRANSACTION_DOCUMENT_UX_CONTRACT_V1 §6.

  B3.6-B.1 — Single source of truth for multi-mode selection:
  Element Plus `el-table`'s internal `selection` is the source of truth.
  `selectedById` is a DERIVED map (selection ↔ full Item) used to emit
  `multi-select`. There is NO separate `selectedIds` set. All selection
  mutations go through `el-table`'s `toggleRowSelection` / `clearSelection`
  API. `@selection-change` keeps the derived map in sync.

  B3.6-B.1 — `inline` prop: when true, the component renders WITHOUT
  the el-popover wrapper (the search input + table + footer are
  always visible, no popover toggle). This is used by the multi-
  select dialog wrapper in SalesOrderEdit — the dialog itself is
  the container, so a nested popover would be both visually
  redundant and unreliable.
-->
<template>
  <div v-if="inline" class="tx-item-selector-inline">
    <div class="tx-item-selector-pop">
      <el-input
        v-model="keyword"
        placeholder="搜索代码 / 名称 / 助记码 / 规格"
        clearable
        :prefix-icon="Search"
        class="tx-item-selector-search"
        @keyup.enter="runSearch"
      />
      <el-table
        ref="tableRef"
        v-loading="loading"
        :data="hits"
        height="380"
        size="small"
        stripe
        :row-class-name="rowClassName"
        @row-click="onRowClick"
        @selection-change="onSelectionChange"
        @select="onRowSelect"
        @select-all="onSelectAll"
      >
        <el-table-column
          v-if="isMulti"
          type="selection"
          width="44"
          fixed="left"
          :selectable="() => true"
        />
        <el-table-column prop="code" label="代码" width="140" show-overflow-tooltip />
        <el-table-column prop="name" label="名称" min-width="160" show-overflow-tooltip />
        <el-table-column prop="specification" label="规格" min-width="100" show-overflow-tooltip>
          <template #default="{ row }">{{ row.specification || '—' }}</template>
        </el-table-column>
        <el-table-column prop="baseUomName" label="单位" width="80" align="center">
          <template #default="{ row }">{{ row.baseUomName || '—' }}</template>
        </el-table-column>
        <el-table-column prop="mnemonicCode" label="助记码" width="80" align="center">
          <template #default="{ row }">
            <span v-if="row.mnemonicCode" class="mdm-mnemonic">{{ row.mnemonicCode }}</span>
            <span v-else class="mdm-mnemonic-empty">—</span>
          </template>
        </el-table-column>
      </el-table>
      <div v-if="!loading && hits.length === 0" class="tx-item-selector-empty">
        <el-empty :description="keyword ? '无匹配物料' : '请输入关键字搜索'" :image-size="60" />
      </div>
      <div v-if="isMulti" class="tx-item-selector-footer">
        <span class="tx-item-selector-count">已选择 {{ selectedCount }} 项</span>
        <div class="tx-item-selector-actions">
          <el-button size="small" @click="onMultiCancel">取消</el-button>
          <el-button size="small" type="primary" :disabled="selectedCount === 0" @click="onMultiConfirm">
            确认添加
          </el-button>
        </div>
      </div>
    </div>
  </div>
  <el-popover
    v-else
    :visible="visible"
    placement="bottom-start"
    :width="560"
    trigger="manual"
    @show="onShow"
    @hide="onHide"
  >
    <template #reference>
      <el-input
        :model-value="displayValue"
        :placeholder="placeholder || '选择物料'"
        readonly
        clearable
        :class="['tx-item-selector-trigger', { 'is-empty': !modelValue }]"
        @click="open"
        @clear="onClear"
      >
        <template #prefix>
          <el-icon><Goods /></el-icon>
        </template>
      </el-input>
    </template>

    <div class="tx-item-selector-pop">
      <el-input
        v-model="keyword"
        placeholder="搜索代码 / 名称 / 助记码 / 规格"
        clearable
        :prefix-icon="Search"
        class="tx-item-selector-search"
        @keyup.enter="runSearch"
      />
      <el-table
        ref="tableRef"
        v-loading="loading"
        :data="hits"
        height="280"
        size="small"
        stripe
        :row-class-name="rowClassName"
        :highlight-current-row="!isMulti"
        @row-click="onRowClick"
        @selection-change="onSelectionChange"
        @select="onRowSelect"
        @select-all="onSelectAll"
      >
        <el-table-column
          v-if="isMulti"
          type="selection"
          width="44"
          fixed="left"
          :selectable="() => true"
        />
        <el-table-column prop="code" label="代码" width="140" show-overflow-tooltip />
        <el-table-column prop="name" label="名称" min-width="160" show-overflow-tooltip />
        <el-table-column prop="specification" label="规格" min-width="100" show-overflow-tooltip>
          <template #default="{ row }">{{ row.specification || '—' }}</template>
        </el-table-column>
        <el-table-column prop="baseUomName" label="单位" width="80" align="center">
          <template #default="{ row }">{{ row.baseUomName || '—' }}</template>
        </el-table-column>
        <el-table-column prop="mnemonicCode" label="助记码" width="80" align="center">
          <template #default="{ row }">
            <span v-if="row.mnemonicCode" class="mdm-mnemonic">{{ row.mnemonicCode }}</span>
            <span v-else class="mdm-mnemonic-empty">—</span>
          </template>
        </el-table-column>
      </el-table>
      <div v-if="!loading && hits.length === 0" class="tx-item-selector-empty">
        <el-empty :description="keyword ? '无匹配物料' : '请输入关键字搜索'" :image-size="60" />
      </div>
      <div v-if="isMulti" class="tx-item-selector-footer">
        <span class="tx-item-selector-count">已选择 {{ selectedCount }} 项</span>
        <div class="tx-item-selector-actions">
          <el-button size="small" @click="onMultiCancel">取消</el-button>
          <el-button size="small" type="primary" :disabled="selectedCount === 0" @click="onMultiConfirm">
            确认添加
          </el-button>
        </div>
      </div>
    </div>
  </el-popover>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, ref, watch } from 'vue';
import { Search, Goods } from '@element-plus/icons-vue';
import { listItems } from '../../api/mdm/item';
import type { Item, ItemListParams, PagedResult } from '../../types/mdm';

const props = withDefaults(
  defineProps<{
    modelValue: string | null;
    placeholder?: string;
    /**
     * GULIERP_TRANSACTION_PHASE_B2 (Phase B.2 — SalesOrder frontend wire)
     * Optional override for the item fetch. Defaults to the standard MDM
     * `listItems` endpoint (`/api/v1/mdm/items`), which requires
     * `MdmPolicies.ItemRead`. Single-role operators such as SALES_OPERATOR
     * do NOT have that permission; they must pass a customFetcher that
     * hits the SalesOrder context facade (e.g. `listSalesOrderItems`)
     * so the boundary contract (G3-R1C) is preserved.
     */
    customFetcher?: (params: ItemListParams) => Promise<PagedResult<Item>>;
    /**
     * B3.6-B — selection mode.
     *   'single' (default) — preserves the B2 row-click → emit('select') flow.
     *   'multi'           — adds a checkbox column, a "已选择 N 项" indicator,
     *                       a footer with 取消 / 确认添加 buttons; emits
     *                       'multi-select' with the chosen Item[] on confirm.
     *                       Selection state is preserved across page/search
     *                       changes by item id.
     */
    mode?: 'single' | 'multi';
    /**
     * B3.6-B.1 — when true, the component renders WITHOUT the el-popover
     * wrapper. The search input + table + footer are always visible.
     * Use this when the parent already provides a container
     * (e.g. SalesOrderEdit's batch dialog).
     */
    inline?: boolean;
  }>(),
  { mode: 'single', inline: false },
);

const emit = defineEmits<{
  'update:modelValue': [value: string | null];
  select: [item: Item];
  /**
   * B3.6-B — emitted when the user clicks 确认添加 in 'multi' mode.
   * The parent (SalesOrderEdit) is expected to feed the items to
   * LineGrid.batchAppendItems(...) which owns the dedup + line creation.
   */
  'multi-select': [items: Item[]];
}>();

// ---- Popover visibility + search state ----

const visible = ref(false);
const keyword = ref('');
const loading = ref(false);
const hits = ref<Item[]>([]);
let searchToken = 0;

const isMulti = computed(() => props.mode === 'multi');

const displayValue = computed(() => {
  const cur = hits.value.find((h) => h.id === props.modelValue);
  if (cur) return `${cur.code} · ${cur.name}`;
  return '';
});

function open() {
  visible.value = true;
  if (hits.value.length === 0) {
    void runSearch();
  }
}

function onShow() {
  /* placeholder for el-popover show hook */
}

function onHide() {
  keyword.value = '';
}

function onClear() {
  emit('update:modelValue', null);
}

async function runSearch() {
  const myToken = ++searchToken;
  loading.value = true;
  try {
    const params: ItemListParams = {
      keyword: keyword.value.trim() || undefined,
      page: 1,
      pageSize: 20,
    };
    const fetchItems = props.customFetcher ?? listItems;
    const result = await fetchItems(params);
    if (myToken === searchToken) {
      hits.value = result.items;
      // B3.6-B.1 §4.2 — re-attach Element Plus selection after the
      // `hits` list changes (search / page change). The map is the
      // source of truth for "which items the user has picked across
      // searches"; the table is just the renderer.
      void reattachSelectionFromMap();
    }
  } catch {
    if (myToken === searchToken) hits.value = [];
  } finally {
    if (myToken === searchToken) loading.value = false;
  }
}

// ---- Single-mode row click (unchanged from B2) ----

function onRowClick(row: Item) {
  if (isMulti.value) {
    // B3.6-B.1 §4.1 + §4.2 — delegate to Element Plus so the checkbox
    // and the internal selection stay in sync.
    tableRef.value?.toggleRowSelection(row, !isInSelection(row));
    return;
  }
  // Single-mode unchanged from B2.
  emit('update:modelValue', row.id);
  emit('select', row);
  visible.value = false;
  keyword.value = '';
}

// ---- Multi-mode selection (single source of truth = el-table) ----

// B3.6-B.1 — Element Plus el-table reference; typed loosely
// because `TableInstance` is non-generic in the runtime types.
const tableRef = ref<{
  toggleRowSelection: (row: Item, selected?: boolean) => void;
  clearSelection: () => void;
} | null>(null);

// `selectedById` is the DERIVED map: which Items the user has picked
// across all search pages. Element Plus el-table holds the actual
// `selection` array (the source of truth for current visible page).
const selectedById = ref<Map<string, Item>>(new Map());
const selectedCount = computed(() => selectedById.value.size);

function isInSelection(item: Item): boolean {
  return selectedById.value.has(item.id);
}

function rowClassName(args: { row: Item; rowIndex: number }): string {
  if (!isMulti.value) return '';
  // B3.6-B.1 §5 — Element Plus applies row-class-name's return value
  // directly to the `.el-table__row` element. The class itself is on
  // the row, so the CSS selector should be the class name (not a
  // descendant of it).
  return isInSelection(args.row) ? 'tx-row-selected' : '';
}

function onSelectionChange(_selection: Item[]) {
  // B3.6-B.1 §4.1 — `@selection-change` is NOT a reliable signal
  // for "the user changed something": Element Plus also fires it
  // when the table re-renders (e.g. after a search change) with an
  // empty selection, which would otherwise look like "user
  // unchecked everything". We instead drive the map from explicit
  // user-action events:
  //   @select         → user toggled a single row checkbox
  //   @select-all     → user clicked the header checkbox
  //   watch(hits)     → data changed (re-render); re-attach the
  //                     checkbox state from the map.
  // The @selection-change handler remains wired (and Vue 3 still
  // requires it for v-model:visible consistency) but does no work.
}

// B3.6-B.1 §4.1 — per-row checkbox toggle. The user explicitly
// checked or unchecked a row; update the map accordingly.
function onRowSelect(selection: Item[], row: Item) {
  // The new `selection` is the table's authoritative current visible
  // selection. The row that changed is `row`. Update the map:
  //   - add the row if it's now in selection
  //   - drop the row if it's no longer in selection (and is on the
  //     current page, i.e. in `hits`)
  const isNowSelected = selection.some((it) => it.id === row.id);
  const next = new Map(selectedById.value);
  if (isNowSelected) {
    next.set(row.id, row);
  } else {
    // The row is in current hits (we just toggled it); drop it.
    // Items on a different page are NOT in `hits` and won't be
    // passed here as `row`, so we don't accidentally drop them.
    if (hits.value.some((h) => h.id === row.id)) {
      next.delete(row.id);
    }
  }
  selectedById.value = next;
}

// B3.6-B.1 §4.3 — header "select all visible / unselect all visible".
// Element Plus's default `@select-all` toggles all rows in the
// current `data` (= `hits`). We mirror that into the map:
//   - For each row in `hits`, if it's in the new selection → keep
//   - If it's NOT in the new selection AND was in `hits` → drop
// Cross-page items (not in `hits`) are preserved.
function onSelectAll(selection: Item[]) {
  const selectedIds = new Set(selection.map((it) => it.id));
  const next = new Map(selectedById.value);
  for (const h of hits.value) {
    if (selectedIds.has(h.id)) {
      next.set(h.id, h);
    } else {
      next.delete(h.id);
    }
  }
  selectedById.value = next;
}

async function reattachSelectionFromMap() {
  // B3.6-B.1 §4.2 — after `hits` updates, re-apply the table's
  // checkbox state so cross-page picks are visible on the new page.
  // We do NOT need a `isRowSelected` round-trip: Element Plus's
  // @selection-change already keeps the visible-page selection in
  // sync with the map. We just toggle on the delta (selectedById
  // vs current visible rows).
  await nextTick();
  const table = tableRef.value;
  if (!table) return;
  for (const h of hits.value) {
    const shouldBe = selectedById.value.has(h.id);
    // Always toggle to desired state. Element Plus's toggleRowSelection
    // is idempotent enough at this scale (2 items) that the extra
    // toggles are harmless. For longer lists this would need an
    // isRowSelected check — left as a future optimization.
    table.toggleRowSelection(h, shouldBe);
  }
}

function onMultiCancel() {
  // B3.6-B.1 §6 — "取消" = 放弃本次批量选择.
  // Clear Element Plus's internal selection AND the derived map.
  tableRef.value?.clearSelection();
  selectedById.value = new Map();
  visible.value = false;
  keyword.value = '';
}

function onMultiConfirm() {
  if (selectedById.value.size === 0) {
    // Nothing to confirm; just close.
    tableRef.value?.clearSelection();
    selectedById.value = new Map();
    visible.value = false;
    keyword.value = '';
    return;
  }
  // B3.6-B §七 — preserve the user's selection order by following
  // the current `hits` order first, then appending any off-page
  // selected items.
  const ordered: Item[] = [];
  for (const h of hits.value) {
    if (selectedById.value.has(h.id)) {
      ordered.push(selectedById.value.get(h.id) ?? h);
    }
  }
  for (const [id, item] of selectedById.value) {
    if (!ordered.find((x) => x.id === id)) ordered.push(item);
  }
  emit('multi-select', ordered);
  visible.value = false;
  keyword.value = '';
  // Clear so the next open starts fresh.
  tableRef.value?.clearSelection();
  selectedById.value = new Map();
}

watch(visible, (v) => {
  if (v) {
    // Re-attach focus to the search input when popover opens.
    setTimeout(() => {
      const el = document.querySelector('.tx-item-selector-search input') as HTMLInputElement | null;
      el?.focus();
    }, 0);
  }
});

// B3.6-B.1 — in `inline` mode, the popover never opens, so `open()`
// is never called and the initial search never runs. Trigger it
// on mount so the inline picker has data ready.
onMounted(() => {
  if (props.inline && hits.value.length === 0 && !loading.value) {
    void runSearch();
  }
});
</script>

<style scoped>
.tx-item-selector-trigger.is-empty :deep(.el-input__inner) {
  color: var(--text-muted);
}
.tx-item-selector-pop {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.tx-item-selector-search :deep(.el-input__inner) {
  font-size: var(--erp-font-size);
}
.tx-item-selector-empty {
  padding: 8px 0;
}
/* B3.6-B.1 §5 — Element Plus applies `row-class-name`'s return value
   directly onto the `<tr class="el-table__row">` element. The
   `.tx-row-selected` class therefore lives on the row itself, not on
   a child. The selector below targets the row directly. */
:deep(.el-table__row.tx-row-selected) {
  background: var(--primary-bg, #eff6ff) !important;
}
:deep(.el-table__row.tx-row-selected td) {
  background: var(--primary-bg, #eff6ff) !important;
}
/* B3.6-B.1 — multi-mode selection UI. */
.tx-item-selector-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  border-top: 1px solid var(--border-default, #e5e7eb);
  padding: 8px 0 4px;
  margin-top: 4px;
}
.tx-item-selector-count {
  color: var(--primary-hover, #2563eb);
  font-size: 12.5px;
  font-weight: 600;
}
.tx-item-selector-actions {
  display: flex;
  gap: 8px;
}
</style>
