<!--
  GULIERP_TRANSACTION_DOCUMENT_LINE_GRID_V1 (Phase A) — shared transaction components.
  WarehouseSelectorPopover — Search-as-you-type Warehouse picker.
  Used as the document's default warehouse (header-level). Per-line
  warehouse override is P1.

  Reuses /api/v1/mdm/warehouses via api/mdm/warehouse.
  Loads only ACTIVE warehouses (typical operator use case).

  Spec: GULIERP_TRANSACTION_DOCUMENT_UX_CONTRACT_V1 §8.
-->
<template>
  <el-popover
    :visible="visible"
    placement="bottom-start"
    :width="440"
    trigger="manual"
    @show="onShow"
    @hide="onHide"
  >
    <template #reference>
      <el-input
        :model-value="displayValue"
        :placeholder="placeholder || '选择仓库'"
        readonly
        clearable
        :class="['tx-warehouse-selector-trigger', { 'is-empty': !modelValue }]"
        @click="open"
        @clear="onClear"
      >
        <template #prefix>
          <el-icon><House /></el-icon>
        </template>
      </el-input>
    </template>

    <div class="tx-warehouse-selector-pop">
      <el-input
        v-model="keyword"
        placeholder="搜索代码 / 名称"
        clearable
        :prefix-icon="Search"
        class="tx-warehouse-selector-search"
        @keyup.enter="runSearch"
      />
      <el-table
        v-loading="loading"
        :data="hits"
        height="280"
        size="small"
        stripe
        highlight-current-row
        @row-click="onRowClick"
      >
        <el-table-column prop="code" label="代码" width="140" show-overflow-tooltip />
        <el-table-column prop="name" label="名称" min-width="180" show-overflow-tooltip />
        <el-table-column prop="locationName" label="位置" width="120" show-overflow-tooltip>
          <template #default="{ row }">{{ row.locationName || '—' }}</template>
        </el-table-column>
      </el-table>
      <div v-if="!loading && hits.length === 0" class="tx-warehouse-selector-empty">
        <el-empty :description="keyword ? '无匹配仓库' : '请输入关键字搜索'" :image-size="60" />
      </div>
    </div>
  </el-popover>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { Search, House } from '@element-plus/icons-vue';
import { listAllWarehousesActiveOnly, getWarehouse } from '../../api/mdm/warehouse';
import type { Warehouse } from '../../types/mdm';

const props = defineProps<{
  modelValue: string | null;
  placeholder?: string;
  /**
   * B3.6-C UI Integration — explicit code/name projection from the
   * parent. When set, the trigger displays `${code} · ${name}`
   * even when the warehouse is not in the current `hits` (which
   * is the case for a loaded draft where the popover has not yet
   * been opened, or the previous search keyword did not match).
   */
  selectedWarehouse?: { id: string; code?: string | null; name?: string | null } | null;
}>();

const emit = defineEmits<{
  'update:modelValue': [value: string | null];
  select: [warehouse: Warehouse];
}>();

const visible = ref(false);
const keyword = ref('');
const loading = ref(false);
const hits = ref<Warehouse[]>([]);
let searchToken = 0;

const displayValue = computed(() => {
  const sw = props.selectedWarehouse;
  if (sw && sw.id && sw.id === props.modelValue && sw.code) {
    return sw.name ? `${sw.code} · ${sw.name}` : sw.code;
  }
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
  /* placeholder */
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
    // For warehouses, the list is small (<50 typical) and we want ACTIVE only.
    // We fetch all active warehouses and filter client-side by keyword.
    const all = await listAllWarehousesActiveOnly();
    const k = keyword.value.trim().toLowerCase();
    const filtered = k
      ? all.filter(
          (w) =>
            w.code.toLowerCase().includes(k) ||
            (w.name || '').toLowerCase().includes(k),
        )
      : all;
    if (myToken === searchToken) {
      hits.value = filtered.slice(0, 50);
    }
  } catch {
    if (myToken === searchToken) hits.value = [];
  } finally {
    if (myToken === searchToken) loading.value = false;
  }
}

function onRowClick(row: Warehouse) {
  emit('update:modelValue', row.id);
  emit('select', row);
  visible.value = false;
  keyword.value = '';
}

watch(visible, (v) => {
  if (v) {
    setTimeout(() => {
      const el = document.querySelector('.tx-warehouse-selector-search input') as HTMLInputElement | null;
      el?.focus();
    }, 0);
  }
});

// B3.6-C UI Integration — when the parent binds a warehouseId
// (e.g. a loaded draft) but the V1 server does not return the
// matching code/name, fetch the warehouse by id so the trigger
// can render `${code} · ${name}`. Runs once on mount; idempotent.
async function ensureCurrentWarehouseLoaded() {
  const id = props.modelValue;
  if (!id) return;
  if (hits.value.find((h) => h.id === id)) return;
  // First try getById (single fetch).
  try {
    const w = await getWarehouse(id);
    if (w && !hits.value.find((h) => h.id === w.id)) {
      hits.value = [w, ...hits.value];
    }
    return;
  } catch {
    // fall through to the list-and-filter path
  }
  try {
    const all = await listAllWarehousesActiveOnly();
    const w = all.find((x) => x.id === id);
    if (w && !hits.value.find((h) => h.id === w.id)) {
      hits.value = [w, ...hits.value];
    }
  } catch {
    /* ignore — popover will show empty until user opens it */
  }
}
onMounted(ensureCurrentWarehouseLoaded);
// Also re-run if modelValue changes after mount (e.g. another
// component updates the bound warehouseId).
watch(() => props.modelValue, ensureCurrentWarehouseLoaded);
</script>

<style scoped>
.tx-warehouse-selector-trigger.is-empty :deep(.el-input__inner) {
  color: var(--text-muted);
}
.tx-warehouse-selector-pop {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.tx-warehouse-selector-search :deep(.el-input__inner) {
  font-size: var(--erp-font-size);
}
.tx-warehouse-selector-empty {
  padding: 8px 0;
}
</style>
