<!--
  GULIERP_TRANSACTION_DOCUMENT_LINE_GRID_V1 (Phase A) — shared transaction components.
  ItemSelectorPopover — Search-as-you-type Item picker for transaction lines.
  Wraps el-popover with an internal search input + el-table list.
  On select, emits update:modelValue (Item id) and select (full Item).

  Reuses the existing /api/v1/mdm/items endpoint via api/mdm/item.

  Spec: GULIERP_TRANSACTION_DOCUMENT_UX_CONTRACT_V1 §6.
-->
<template>
  <el-popover
    :visible="visible"
    placement="bottom-start"
    :width="480"
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
        v-loading="loading"
        :data="hits"
        height="280"
        size="small"
        stripe
        highlight-current-row
        @row-click="onRowClick"
      >
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
    </div>
  </el-popover>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { Search, Goods } from '@element-plus/icons-vue';
import { listItems } from '../../api/mdm/item';
import type { Item, ItemListParams } from '../../types/mdm';

const props = defineProps<{
  modelValue: string | null;
  placeholder?: string;
}>();

const emit = defineEmits<{
  'update:modelValue': [value: string | null];
  select: [item: Item];
}>();

const visible = ref(false);
const keyword = ref('');
const loading = ref(false);
const hits = ref<Item[]>([]);
let searchToken = 0;

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
    const result = await listItems(params);
    if (myToken === searchToken) {
      hits.value = result.items;
    }
  } catch {
    if (myToken === searchToken) hits.value = [];
  } finally {
    if (myToken === searchToken) loading.value = false;
  }
}

function onRowClick(row: Item) {
  emit('update:modelValue', row.id);
  emit('select', row);
  visible.value = false;
  keyword.value = '';
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
</style>
