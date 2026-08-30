<!--
  GULIERP_TRANSACTION_DOCUMENT_LINE_GRID_V1 (Phase A) — shared transaction components.
  PartnerSelectorPopover — Search-as-you-type BusinessPartner picker.
  Used as Customer (Sales) or Supplier (Purchase) selector.

  Reuses /api/v1/mdm/business-partners via api/mdm/business-partner.

  Spec: GULIERP_TRANSACTION_DOCUMENT_UX_CONTRACT_V1 §7.
-->
<template>
  <el-popover
    :visible="visible"
    placement="bottom-start"
    :width="520"
    trigger="manual"
    @show="onShow"
    @hide="onHide"
  >
    <template #reference>
      <el-input
        :model-value="displayValue"
        :placeholder="placeholder || defaultPlaceholder"
        readonly
        clearable
        :class="['tx-partner-selector-trigger', { 'is-empty': !modelValue }]"
        @click="open"
        @clear="onClear"
      >
        <template #prefix>
          <el-icon><User /></el-icon>
        </template>
      </el-input>
    </template>

    <div class="tx-partner-selector-pop">
      <el-input
        v-model="keyword"
        :placeholder="`搜索代码 / 名称 / 简称 / 助记码`"
        clearable
        :prefix-icon="Search"
        class="tx-partner-selector-search"
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
        <el-table-column prop="code" label="代码" width="120" show-overflow-tooltip />
        <el-table-column prop="name" label="名称" min-width="180" show-overflow-tooltip />
        <el-table-column prop="shortName" label="简称" width="100" show-overflow-tooltip>
          <template #default="{ row }">{{ row.shortName || '—' }}</template>
        </el-table-column>
        <el-table-column prop="mnemonicCode" label="助记码" width="80" align="center">
          <template #default="{ row }">
            <span v-if="row.mnemonicCode" class="mdm-mnemonic">{{ row.mnemonicCode }}</span>
            <span v-else class="mdm-mnemonic-empty">—</span>
          </template>
        </el-table-column>
        <el-table-column prop="role" label="角色" width="80" align="center" />
        <el-table-column prop="status" label="状态" width="80" align="center">
          <template #default="{ row }">{{ statusLabel(row.status) }}</template>
        </el-table-column>
      </el-table>
      <div v-if="!loading && hits.length === 0" class="tx-partner-selector-empty">
        <el-empty :description="keyword ? '无匹配业务伙伴' : '请输入关键字搜索'" :image-size="60" />
      </div>
    </div>
  </el-popover>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { Search, User } from '@element-plus/icons-vue';
import { listBusinessPartners } from '../../api/mdm/business-partner';
import { roleUiToInt } from '../../types/mdm';
import type { BusinessPartner, BusinessPartnerListParams, BusinessPartnerRole } from '../../types/mdm';

const props = defineProps<{
  modelValue: string | null;
  /**
   * Restrict to partners of these roles. Pass 'Customer' to list customers
   * only, 'Supplier' to list suppliers only, 'Both' for either. The server
   * may also enforce this; the client filter is best-effort.
   */
  role?: 'Customer' | 'Supplier' | 'Both';
  placeholder?: string;
}>();

const emit = defineEmits<{
  'update:modelValue': [value: string | null];
  select: [partner: BusinessPartner];
}>();

const visible = ref(false);
const keyword = ref('');
const loading = ref(false);
const hits = ref<BusinessPartner[]>([]);
let searchToken = 0;

const defaultPlaceholder = computed(() => {
  if (props.role === 'Customer') return '选择客户';
  if (props.role === 'Supplier') return '选择供应商';
  return '选择客户 / 供应商';
});

const displayValue = computed(() => {
  const cur = hits.value.find((h) => h.id === props.modelValue);
  if (cur) return `${cur.code} · ${cur.name}`;
  return '';
});

function statusLabel(status: string): string {
  return status === 'active' ? '启用' : '停用';
}

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
    const params: BusinessPartnerListParams = {
      keyword: keyword.value.trim() || undefined,
      page: 1,
      pageSize: 20,
    };
    if (props.role === 'Customer' || props.role === 'Supplier') {
      params.role = roleUiToInt(props.role as BusinessPartnerRole);
    }
    const result = await listBusinessPartners(params);
    if (myToken === searchToken) {
      hits.value = result.items;
    }
  } catch {
    if (myToken === searchToken) hits.value = [];
  } finally {
    if (myToken === searchToken) loading.value = false;
  }
}

function onRowClick(row: BusinessPartner) {
  emit('update:modelValue', row.id);
  emit('select', row);
  visible.value = false;
  keyword.value = '';
}

watch(visible, (v) => {
  if (v) {
    setTimeout(() => {
      const el = document.querySelector('.tx-partner-selector-search input') as HTMLInputElement | null;
      el?.focus();
    }, 0);
  }
});
</script>

<style scoped>
.tx-partner-selector-trigger.is-empty :deep(.el-input__inner) {
  color: var(--text-muted);
}
.tx-partner-selector-pop {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.tx-partner-selector-search :deep(.el-input__inner) {
  font-size: var(--erp-font-size);
}
.tx-partner-selector-empty {
  padding: 8px 0;
}
</style>
