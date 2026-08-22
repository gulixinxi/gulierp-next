<template>
  <div class="so-list">
    <div class="gs-list-toolbar">
      <div class="gs-toolbar-left">
        <el-input v-model="filters.keyword" placeholder="搜索单号 / 客户编码 / 客户名称" clearable :prefix-icon="SearchIcon" style="width: 300px" @keyup.enter="load" @clear="load" />
        <el-select v-model="filters.status" placeholder="状态" clearable style="width: 130px" @change="load">
          <el-option :value="1" label="草稿" />
          <el-option :value="2" label="已确认" />
          <el-option :value="3" label="已取消" />
        </el-select>
        <el-date-picker v-model="dateRange" type="daterange" value-format="YYYY-MM-DD" start-placeholder="开始日期" end-placeholder="结束日期" style="width: 260px" @change="load" />
      </div>
      <div class="gs-toolbar-right">
        <el-button @click="load"><el-icon><Refresh /></el-icon>刷新</el-button>
        <el-button type="primary" @click="onNew"><el-icon><Plus /></el-icon>新建销售订单</el-button>
      </div>
    </div>

    <div class="gs-list-wrap">
      <el-table v-loading="store.loading" :data="store.items" border stripe height="100%" size="small" row-key="id" @row-dblclick="openDetail">
        <el-table-column type="index" label="#" width="54" fixed="left" />
        <el-table-column prop="orderNo" label="单据号" width="170" fixed="left">
          <template #default="{ row }"><a class="cell-link" @click.stop="openDetail(row)">{{ row.orderNo }}</a></template>
        </el-table-column>
        <el-table-column prop="customerNameSnapshot" label="客户" min-width="220">
          <template #default="{ row }"><span :title="row.customerCodeSnapshot">{{ row.customerNameSnapshot }}</span></template>
        </el-table-column>
        <el-table-column prop="orderDate" label="订单日期" width="120" />
        <el-table-column prop="requestedDeliveryDate" label="要求交期" width="120" />
        <el-table-column prop="status" label="状态" width="100">
          <template #default="{ row }"><el-tag :type="statusTag(row.status)" effect="light" size="small">{{ statusLabel(row.status) }}</el-tag></template>
        </el-table-column>
        <el-table-column prop="currencyCode" label="币种" width="80" />
        <el-table-column prop="totalAmount" label="价税合计" width="130" align="right">
          <template #default="{ row }">{{ money(row.totalAmount) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="180" fixed="right" align="center">
          <template #default="{ row }">
            <el-button text type="primary" size="small" @click="openDetail(row)">查看</el-button>
            <el-button text type="primary" size="small" :disabled="row.status !== 1" @click="openEdit(row)">编辑</el-button>
          </template>
        </el-table-column>
        <template #empty>
          <div class="empty-state">
            <el-icon :size="40"><Document /></el-icon>
            <p>暂无真实销售订单</p>
            <el-button type="primary" @click="onNew">新建第一张销售订单</el-button>
          </div>
        </template>
      </el-table>
    </div>

    <div class="gs-list-pagination">
      <span class="gs-summary-text">共 <b>{{ store.totalCount }}</b> 条</span>
      <el-pagination v-model:current-page="store.page" v-model:page-size="store.pageSize" :total="store.totalCount" :page-sizes="[20, 50, 100]" layout="sizes, prev, pager, next, jumper" background @change="load" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import { ElMessage } from 'element-plus';
import { Document, Plus, Refresh, Search as SearchIcon } from '@element-plus/icons-vue';
import { useTabsStore } from '../../stores/tabs';
import { useSalesOrderStore } from '../../stores/sales-order';
import type { SalesOrderListItemDto, SalesOrderStatus } from '../../types/sales-order';
import { SALES_STATUS_LABEL, SALES_STATUS_TAG } from '../../types/sales-order';

defineOptions({ name: 'SalesOrderList' });

const router = useRouter();
const tabs = useTabsStore();
const store = useSalesOrderStore();
const dateRange = ref<[string, string] | null>(null);
const filters = reactive<{ keyword: string; status?: SalesOrderStatus }>({ keyword: '' });

function money(v: number) {
  return new Intl.NumberFormat('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(v || 0);
}

function statusLabel(status: SalesOrderStatus) {
  return SALES_STATUS_LABEL[status];
}

function statusTag(status: SalesOrderStatus) {
  return SALES_STATUS_TAG[status];
}

async function load() {
  try {
    await store.fetchList({
      keyword: filters.keyword || undefined,
      status: filters.status,
      orderDateFrom: dateRange.value?.[0],
      orderDateTo: dateRange.value?.[1],
    });
  } catch (err) {
    ElMessage.error((err as any)?.detail || (err as any)?.title || '销售订单加载失败');
  }
}

function openDetail(row: SalesOrderListItemDto) {
  tabs.openDetail(row.id, row.orderNo);
  router.push(`/sales-order/${row.id}`).catch(() => {});
}

function openEdit(row: SalesOrderListItemDto) {
  tabs.openEdit(row.id, row.orderNo, 'edit');
  router.push(`/sales-order/${row.id}/edit`).catch(() => {});
}

function onNew() {
  tabs.openEdit('new', '新建销售订单', 'create');
  router.push('/sales-order/new/edit').catch(() => {});
}

onMounted(() => {
  tabs.setActive('list-sales-order');
  load();
});
</script>

<style scoped>
.so-list { display: flex; flex-direction: column; height: 100%; background: var(--bg-container); }
.gs-list-wrap { flex: 1; min-height: 0; overflow: hidden; padding: 0; margin: var(--section-gutter) var(--section-gutter) 0; }
.empty-state { padding: 56px 0; text-align: center; color: var(--text-muted); }
.cell-link { color: var(--color-blue-600); text-decoration: none; cursor: pointer; font-weight: 500; }
</style>
