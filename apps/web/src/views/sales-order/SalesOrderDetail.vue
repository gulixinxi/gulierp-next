<template>
  <div class="so-detail" v-loading="loading">
    <div v-if="order" class="detail-head">
      <div>
        <h2>{{ order.orderNo }}</h2>
        <p>{{ order.customerNameSnapshot }} · {{ order.customerCodeSnapshot }}</p>
      </div>
      <div class="detail-actions">
        <el-tag :type="SALES_STATUS_TAG[order.status]" effect="light">{{ SALES_STATUS_LABEL[order.status] }}</el-tag>
        <el-button :disabled="order.status !== 1" @click="edit">编辑草稿</el-button>
        <el-button type="success" :disabled="order.status !== 1" @click="confirm">确认</el-button>
        <el-button type="danger" :disabled="order.status === 3" @click="cancel">取消</el-button>
      </div>
    </div>

    <el-descriptions v-if="order" border :column="3" class="detail-card">
      <el-descriptions-item label="订单日期">{{ order.orderDate }}</el-descriptions-item>
      <el-descriptions-item label="要求交期">{{ order.requestedDeliveryDate || '—' }}</el-descriptions-item>
      <el-descriptions-item label="币种">{{ order.currencyCode }}</el-descriptions-item>
      <el-descriptions-item label="不含税金额">{{ money(order.totalNetAmount) }}</el-descriptions-item>
      <el-descriptions-item label="税额">{{ money(order.totalTaxAmount) }}</el-descriptions-item>
      <el-descriptions-item label="价税合计">{{ money(order.totalAmount) }}</el-descriptions-item>
      <el-descriptions-item label="备注" :span="3">{{ order.remarks || '—' }}</el-descriptions-item>
    </el-descriptions>

    <el-table v-if="order" :data="order.lines" border size="small" class="line-table">
      <el-table-column prop="lineNo" label="行号" width="80" />
      <el-table-column prop="itemCodeSnapshot" label="物料编码" width="140" />
      <el-table-column prop="itemNameSnapshot" label="物料名称" min-width="180" />
      <el-table-column prop="uomNameSnapshot" label="单位" width="110" />
      <el-table-column prop="quantity" label="数量" width="110" align="right" />
      <el-table-column prop="unitPrice" label="单价" width="110" align="right" />
      <el-table-column prop="discountRate" label="折扣率" width="100" align="right" />
      <el-table-column prop="taxRate" label="税率" width="90" align="right" />
      <el-table-column prop="netAmount" label="不含税" width="120" align="right">
        <template #default="{ row }">{{ money(row.netAmount) }}</template>
      </el-table-column>
      <el-table-column prop="taxAmount" label="税额" width="120" align="right">
        <template #default="{ row }">{{ money(row.taxAmount) }}</template>
      </el-table-column>
      <el-table-column prop="totalAmount" label="价税合计" width="130" align="right">
        <template #default="{ row }">{{ money(row.totalAmount) }}</template>
      </el-table-column>
    </el-table>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useSalesOrderStore } from '../../stores/sales-order';
import { useTabsStore } from '../../stores/tabs';
import { SALES_STATUS_LABEL, SALES_STATUS_TAG } from '../../types/sales-order';

defineOptions({ name: 'SalesOrderDetail' });

const route = useRoute();
const router = useRouter();
const store = useSalesOrderStore();
const tabs = useTabsStore();
const loading = ref(false);
const id = computed(() => String(route.params.id));
const order = computed(() => store.current);

function money(v: number) {
  return new Intl.NumberFormat('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(v || 0);
}

async function load() {
  loading.value = true;
  try {
    const dto = await store.fetchDetail(id.value);
    tabs.openDetail(dto.id, dto.orderNo);
  } catch (err) {
    ElMessage.error((err as any)?.detail || (err as any)?.title || '销售订单加载失败');
  } finally {
    loading.value = false;
  }
}

function edit() {
  if (!order.value) return;
  tabs.openEdit(order.value.id, order.value.orderNo, 'edit');
  router.push(`/sales-order/${order.value.id}/edit`).catch(() => {});
}

async function confirm() {
  if (!order.value) return;
  await ElMessageBox.confirm('确认后订单客户和明细不可再修改，是否继续？', '确认销售订单');
  try {
    await store.confirm(order.value.id);
    ElMessage.success('销售订单已确认');
  } catch (err) {
    ElMessage.error((err as any)?.detail || (err as any)?.title || '确认失败');
  }
}

async function cancel() {
  if (!order.value) return;
  await ElMessageBox.confirm('确定取消该销售订单？', '取消销售订单', { type: 'warning' });
  try {
    await store.cancel(order.value.id);
    ElMessage.success('销售订单已取消');
  } catch (err) {
    ElMessage.error((err as any)?.detail || (err as any)?.title || '取消失败');
  }
}

onMounted(load);
</script>

<style scoped>
.so-detail { height: 100%; overflow: auto; background: var(--bg-container); padding: var(--section-gutter); }
.detail-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 14px; }
.detail-head h2 { margin: 0; font-size: 18px; }
.detail-head p { margin: 4px 0 0; color: var(--text-muted); font-size: 13px; }
.detail-actions { display: flex; gap: 8px; align-items: center; }
.detail-card { margin-bottom: 14px; }
.line-table { margin-top: 12px; }
</style>
