<template>
  <div v-if="order" class="so-detail" v-loading="loading">
    <div class="detail-headerbar">
      <div class="headerbar-left">
        <el-button text @click="goBack"><el-icon><ArrowLeft /></el-icon>返回</el-button>
        <el-divider direction="vertical" />
        <div class="doc-meta">
          <span class="doc-no">{{ order.orderNo }}</span>
          <span class="doc-type">普通销售</span>
        </div>
        <div class="status-tags">
          <el-tag :type="SALES_STATUS_TAG[order.status]" size="small" effect="dark">{{ SALES_STATUS_LABEL[order.status] }}</el-tag>
          <el-tag type="info" size="small" effect="plain">{{ order.status === 2 ? '已通过' : '未提交' }}</el-tag>
          <el-tag type="info" size="small" effect="plain">未执行</el-tag>
        </div>
      </div>
      <div class="headerbar-right">
        <el-button :disabled="order.status !== 1" type="primary" @click="edit"><el-icon><EditPen /></el-icon>编辑</el-button>
        <el-button :disabled="order.status !== 1" type="success" @click="confirm"><el-icon><Select /></el-icon>确认</el-button>
        <el-button :disabled="order.status === 3" type="danger" plain @click="cancel"><el-icon><CircleClose /></el-icon>取消</el-button>
        <el-divider direction="vertical" />
        <el-button @click="printUnsupported"><el-icon><Printer /></el-icon>打印</el-button>
      </div>
    </div>

    <div class="detail-section">
      <div class="section-title"><el-icon><Document /></el-icon><span>单据头信息</span></div>
      <el-descriptions :column="4" border size="small">
        <el-descriptions-item label="单据编号"><span class="mono">{{ order.orderNo }}</span></el-descriptions-item>
        <el-descriptions-item label="订单日期">{{ order.orderDate }}</el-descriptions-item>
        <el-descriptions-item label="交货日期">{{ order.requestedDeliveryDate || '—' }}</el-descriptions-item>
        <el-descriptions-item label="单据类型">普通销售</el-descriptions-item>
        <el-descriptions-item label="客户" :span="2"><span class="link">{{ order.customerNameSnapshot }}</span><span class="mono small">({{ order.customerCodeSnapshot }})</span></el-descriptions-item>
        <el-descriptions-item label="币种">{{ order.currencyCode }}</el-descriptions-item>
        <el-descriptions-item label="乐观锁版本">v{{ order.concurrencyVersion }}</el-descriptions-item>
        <el-descriptions-item label="不含税金额">{{ money(order.totalNetAmount) }}</el-descriptions-item>
        <el-descriptions-item label="税额">{{ money(order.totalTaxAmount) }}</el-descriptions-item>
        <el-descriptions-item label="价税合计">{{ money(order.totalAmount) }}</el-descriptions-item>
        <el-descriptions-item label="创建时间">{{ order.createdAt }}</el-descriptions-item>
        <el-descriptions-item label="更新时间">{{ order.modifiedAt }}</el-descriptions-item>
        <el-descriptions-item label="备注" :span="4">{{ order.remarks || '—' }}</el-descriptions-item>
      </el-descriptions>
    </div>

    <div class="detail-section">
      <div class="section-title">
        <el-icon><Grid /></el-icon>
        <span>订单明细</span>
        <span class="section-sub">{{ order.lines.length }} 行</span>
      </div>
      <div class="lines-table-wrap">
        <el-table :data="order.lines" border stripe size="small" :header-cell-style="{ background: '#f1f5f9', padding: '0 !important' }" :cell-style="{ padding: '5px 8px', fontSize: '12.5px' }">
          <el-table-column label="行号" prop="lineNo" width="70" fixed="left" align="center" />
          <el-table-column label="商品" min-width="220" fixed="left">
            <template #default="{ row }">
              <div class="item-cell"><div class="item-code">{{ row.itemCodeSnapshot }}</div><div class="item-name">{{ row.itemNameSnapshot }}</div></div>
            </template>
          </el-table-column>
          <el-table-column label="单位" prop="uomNameSnapshot" width="100" align="center" />
          <el-table-column label="数量" prop="quantity" width="120" align="right"><template #default="{ row }"><span class="num-cell">{{ money(row.quantity, 4) }}</span></template></el-table-column>
          <el-table-column label="未税单价" prop="unitPrice" width="120" align="right"><template #default="{ row }"><span class="num-cell">{{ money(row.unitPrice, 4) }}</span></template></el-table-column>
          <el-table-column label="折扣率" prop="discountRate" width="100" align="right"><template #default="{ row }">{{ rate(row.discountRate) }}</template></el-table-column>
          <el-table-column label="税率" prop="taxRate" width="90" align="right"><template #default="{ row }">{{ rate(row.taxRate) }}</template></el-table-column>
          <el-table-column label="不含税" prop="netAmount" width="120" align="right"><template #default="{ row }"><span class="num-cell">{{ money(row.netAmount) }}</span></template></el-table-column>
          <el-table-column label="税额" prop="taxAmount" width="120" align="right"><template #default="{ row }"><span class="num-cell">{{ money(row.taxAmount) }}</span></template></el-table-column>
          <el-table-column label="价税合计" prop="totalAmount" width="130" align="right"><template #default="{ row }"><span class="num-cell strong">{{ money(row.totalAmount) }}</span></template></el-table-column>
          <el-table-column label="备注" prop="remarks" min-width="160" show-overflow-tooltip />
        </el-table>
      </div>
    </div>

    <div class="detail-section">
      <el-tabs v-model="activeTab" type="border-card">
        <el-tab-pane name="approval">
          <template #label><el-icon><Checked /></el-icon> 审批记录</template>
          <el-empty description="暂无审批记录" :image-size="60" />
        </el-tab-pane>
        <el-tab-pane name="attachments">
          <template #label><el-icon><Paperclip /></el-icon> 附件</template>
          <el-empty description="暂无附件" :image-size="60" />
        </el-tab-pane>
        <el-tab-pane name="relations">
          <template #label><el-icon><Link /></el-icon> 来源/下游</template>
          <el-empty description="暂无来源或下游单据" :image-size="60" />
        </el-tab-pane>
        <el-tab-pane name="audit">
          <template #label><el-icon><List /></el-icon> 操作日志</template>
          <el-table :data="auditRows" size="small" border>
            <el-table-column prop="action" label="操作" width="120" />
            <el-table-column prop="actor" label="操作人" width="140" />
            <el-table-column prop="at" label="时间" width="220" />
            <el-table-column prop="remark" label="备注" />
          </el-table>
        </el-tab-pane>
      </el-tabs>
    </div>
  </div>
  <el-empty v-else description="未找到该销售订单" />
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { ArrowLeft, Checked, CircleClose, Document, EditPen, Grid, Link, List, Paperclip, Printer, Select } from '@element-plus/icons-vue';
import { useSalesOrderStore } from '../../stores/sales-order';
import { useTabsStore } from '../../stores/tabs';
import { SALES_STATUS_LABEL, SALES_STATUS_TAG } from '../../types/sales-order';

defineOptions({ name: 'SalesOrderDetail' });

const route = useRoute();
const router = useRouter();
const store = useSalesOrderStore();
const tabs = useTabsStore();
const loading = ref(false);
const activeTab = ref('approval');
const id = computed(() => String(route.params.id));
const order = computed(() => store.current);
const auditRows = computed(() => order.value ? [
  { action: 'Create/Update', actor: 'System', at: order.value.modifiedAt, remark: '后端审计日志接入后展示完整操作链' },
] : []);

function money(v: number, decimals = 2) {
  return new Intl.NumberFormat('zh-CN', { minimumFractionDigits: decimals, maximumFractionDigits: decimals }).format(v || 0);
}
function rate(v: number) { return `${((v || 0) * 100).toFixed(2)}%`; }
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
function goBack() { tabs.setActive('list-sales-order'); router.push('/sales-order').catch(() => {}); }
function edit() { if (!order.value) return; tabs.openEdit(order.value.id, order.value.orderNo, 'edit'); router.push(`/sales-order/${order.value.id}/edit`).catch(() => {}); }
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
function printUnsupported() { ElMessage.info('打印将接入正式模板服务，当前不生成假文件'); }
onMounted(load);
</script>

<style scoped>
.so-detail { display: flex; flex-direction: column; height: 100%; background: #f6f7fb; overflow: auto; }
.detail-headerbar { background: #fff; border-bottom: 1px solid #e5e7eb; padding: 8px 16px; display: flex; justify-content: space-between; align-items: center; flex-shrink: 0; box-shadow: 0 1px 2px rgba(0,0,0,0.04); position: sticky; top: 0; z-index: 5; }
.headerbar-left, .headerbar-right, .doc-meta, .status-tags { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.doc-no { font-size: 16px; font-weight: 700; color: #1f2937; }
.doc-type { background: #eff6ff; color: #1d4ed8; padding: 1px 8px; border-radius: 8px; font-size: 11px; }
.detail-section { background: #fff; margin: 8px; border-radius: 6px; padding: 12px 16px; box-shadow: 0 1px 2px rgba(0,0,0,0.04); }
.section-title { display: flex; align-items: center; gap: 6px; font-size: 14px; font-weight: 600; color: #1f2937; margin-bottom: 12px; padding-bottom: 8px; border-bottom: 1px solid #f1f5f9; }
.section-sub { font-size: 12px; font-weight: 400; color: #9ca3af; margin-left: 8px; }
.mono { font-family: 'JetBrains Mono', 'Consolas', monospace; }
.mono.small { font-size: 11px; color: #9ca3af; margin-left: 4px; }
.link { color: #2563eb; }
.item-cell .item-code { font-size: 12px; color: #6b7280; font-family: monospace; }
.item-cell .item-name { font-size: 12.5px; color: #1f2937; }
.num-cell { font-family: 'JetBrains Mono', 'Consolas', monospace; display: inline-block; width: 100%; text-align: right; }
.num-cell.strong { color: #c2410c; font-weight: 700; }
</style>
