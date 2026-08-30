<template>
  <!-- PaymentMethodList — read-only facade over V1 PM_METHOD DictionaryType
       (G3-R1E, 2026-08-26).
       Per docs/verification/G3_R1E_PAYMENT_METHOD_DISCOVERY.md:
       PaymentMethod is the 6th of 9 V1 system dictionaries
       (code=PM_METHOD). The V15 seed in
       data/bootstrap/reference/mdm/dictionary/PM_METHOD.json
       defines 5 items: 现金/银行转账/支票/信用卡/在线支付.
       The page is read-only V1; write goes through the
       Dictionary page (/mdm/dictionaries) where MDM_OPERATOR
       can manage the PM_METHOD type and its items directly. -->
  <div class="mdm-list">
    <MdmListToolbar
      v-model:search="searchKeyword"
      search-placeholder="搜索代码 / 名称"
      :show-create="false"
      @search="applyFilters"
    >
      <template #filters>
        <el-select v-model="filterStatus" placeholder="状态" clearable style="width: 100px" @change="applyFilters">
          <el-option v-for="opt in STATUS_OPTIONS" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
      </template>
      <template #actions>
        <el-button text @click="refresh">
          <el-icon><Refresh /></el-icon>
          刷新
        </el-button>
      </template>
    </MdmListToolbar>

    <!-- Read-only banner (G3-R1E design decision) -->
    <div class="mdm-context-bar">
      <el-icon><InfoFilled /></el-icon>
      <span>只读视图 — 付款方式是基础字典(PM_METHOD)。完整编辑请前往 <RouterLink to="/mdm/dictionaries">基础字典</RouterLink>。</span>
    </div>

    <div class="gs-list-wrap">
      <el-table
        v-loading="loading"
        :data="paymentMethods"
        border
        stripe
        height="100%"
        size="small"
        row-key="id"
        :header-cell-style="{ padding: '0 8px' }"
        :cell-style="{ padding: '0 8px' }"
      >
        <el-table-column type="index" label="#" width="48" fixed="left" />
        <el-table-column prop="code" label="代码" width="160" sortable show-overflow-tooltip>
          <template #default="{ row }">
            <span class="mdm-code">{{ row.code }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="name" label="名称" min-width="200" show-overflow-tooltip sortable />
        <el-table-column prop="isDefault" label="默认" width="80" align="center">
          <template #default="{ row }">
            <el-tag v-if="row.isDefault" type="success" size="small" effect="light">是</el-tag>
            <span v-else>—</span>
          </template>
        </el-table-column>
        <el-table-column prop="sortOrder" label="排序" width="90" align="center" sortable />
        <el-table-column prop="status" label="状态" width="90" align="center">
          <template #default="{ row }">
            <MdmStatusBadge :status="row.status" />
          </template>
        </el-table-column>
        <el-table-column prop="description" label="描述" min-width="220" show-overflow-tooltip>
          <template #default="{ row }">
            {{ row.description || '—' }}
          </template>
        </el-table-column>
        <el-table-column prop="updatedAt" label="更新时间" width="165" sortable>
          <template #default="{ row }">{{ formatDate(row.updatedAt) }}</template>
        </el-table-column>
      </el-table>
    </div>

    <div v-if="!loading && paymentMethods.length === 0 && !error" class="gs-empty-wrap">
      <MdmEmptyState
        title="暂无付款方式"
        description="当前租户尚未种子 PM_METHOD 字典(系统级默认种子)。可通过字典页签 (/mdm/dictionaries) 检查 PM_METHOD 是否存在，或由 operator 运行 seed-mdm-dictionary CLI 补种。"
      />
    </div>

    <div v-if="error" class="gs-error-banner">
      <el-alert type="error" :closable="false" show-icon :title="error" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { InfoFilled, Refresh } from '@element-plus/icons-vue';

import MdmEmptyState from '../../components/mdm/MdmEmptyState.vue';
import MdmListToolbar from '../../components/mdm/MdmListToolbar.vue';
import MdmStatusBadge from '../../components/mdm/MdmStatusBadge.vue';
import { ApiError } from '../../api/http';
import * as pmApi from '../../api/mdm/payment-method';
import { STATUS_OPTIONS, statusUiToInt } from '../../types/mdm';
import type { MasterDataStatus } from '../../types/mdm';
import type { PaymentMethod } from '../../api/mdm/payment-method';

const paymentMethods = ref<PaymentMethod[]>([]);
const total = ref(0);
const loading = ref(false);
const error = ref<string | null>(null);

const searchKeyword = ref('');
const filterStatus = ref<MasterDataStatus | ''>('');
const page = reactive({ current: 1, size: 50 });

async function fetchPaymentMethods() {
  loading.value = true;
  error.value = null;
  try {
    const result = await pmApi.listPaymentMethods({
      keyword: searchKeyword.value.trim() || undefined,
      status: filterStatus.value ? statusUiToInt(filterStatus.value) : undefined,
      page: page.current,
      pageSize: page.size,
    });
    paymentMethods.value = result.items;
    total.value = result.totalCount;
  } catch (e) {
    if (e instanceof ApiError) {
      if (e.code === 'authentication_required') throw e;
      const parts = [e.title];
      if (e.detail) parts.push(e.detail);
      if (e.requestId) parts.push(`(RequestId: ${e.requestId})`);
      error.value = parts.join(' — ');
    } else {
      error.value = '加载付款方式失败，请刷新重试';
    }
    paymentMethods.value = [];
    total.value = 0;
  } finally {
    loading.value = false;
  }
}

function applyFilters() {
  page.current = 1;
  fetchPaymentMethods();
}

function refresh() {
  ElMessage.info('刷新中…');
  fetchPaymentMethods();
}

function formatDate(iso: string): string {
  if (!iso) return '—';
  try {
    return new Date(iso).toLocaleString('zh-CN', { hour12: false });
  } catch {
    return iso;
  }
}

onMounted(fetchPaymentMethods);
</script>

<style scoped>
.mdm-context-bar {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 8px 12px;
  margin: 8px 0;
  background: var(--info-bg, #f4f4f5);
  border: 1px solid var(--border-subtle, #e4e4e7);
  border-radius: 4px;
  font-size: 13px;
  color: var(--text-secondary, #4a4a4a);
}
.mdm-context-bar a {
  color: var(--primary-default, #0a6ed1);
  text-decoration: none;
  margin: 0 4px;
}
.mdm-context-bar a:hover {
  text-decoration: underline;
}
.gs-empty-wrap {
  padding: 24px;
}
.gs-error-banner {
  padding: 12px 16px;
}
.mdm-code {
  font-family: var(--font-mono, 'SFMono-Regular', Consolas, 'Liberation Mono', monospace);
  font-size: 12px;
  color: var(--text-secondary);
}
</style>
