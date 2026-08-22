<template>
  <div class="so-edit">
    <div class="edit-headerbar">
      <div class="headerbar-left">
        <el-button text @click="goBack"><el-icon><ArrowLeft /></el-icon>返回</el-button>
        <el-divider direction="vertical" />
        <span class="doc-no">{{ isCreate ? '新建销售订单' : orderNo }}</span>
        <div class="status-tags">
          <el-tag :type="statusTag" size="small" effect="dark">{{ statusLabel }}</el-tag>
          <el-tag type="info" size="small" effect="plain">未提交</el-tag>
          <el-tag type="info" size="small" effect="plain">未执行</el-tag>
        </div>
        <span class="version-tag">v{{ form.concurrencyVersion }}</span>
      </div>
      <div class="headerbar-right">
        <el-button type="primary" :loading="saving" @click="save"><el-icon><Check /></el-icon>保存草稿</el-button>
      </div>
    </div>

    <div class="edit-section">
      <div class="section-title">
        <el-icon><Document /></el-icon>
        <span>单据头信息</span>
        <span class="section-sub">客户、物料和单位均来自真实主数据</span>
      </div>
      <el-form :model="form" label-width="92px" label-position="right">
        <div class="form-grid core-grid">
          <el-form-item label="单据编号">
            <el-input :model-value="isCreate ? '保存时自动生成' : orderNo" disabled />
          </el-form-item>
          <el-form-item label="订单日期" required>
            <el-date-picker v-model="form.orderDate" type="date" value-format="YYYY-MM-DD" style="width:100%" />
          </el-form-item>
          <el-form-item label="交货日期">
            <el-date-picker v-model="form.requestedDeliveryDate" type="date" value-format="YYYY-MM-DD" clearable style="width:100%" />
          </el-form-item>
          <el-form-item label="单据类型">
            <el-select v-model="documentType" disabled style="width:100%">
              <el-option label="普通销售" value="Normal" />
            </el-select>
          </el-form-item>
          <el-form-item label="客户" required>
            <el-select v-model="form.customerId" filterable remote reserve-keyword placeholder="搜索真实启用客户" :remote-method="searchCustomers" :loading="customerLoading" style="width:100%">
              <el-option v-for="c in customers" :key="c.id" :label="`${c.name} (${c.code})`" :value="c.id" />
            </el-select>
          </el-form-item>
          <el-form-item label="销售员">
            <el-input model-value="当前登录用户" disabled />
          </el-form-item>
          <el-form-item label="币种">
            <el-input model-value="CNY" disabled />
          </el-form-item>
          <el-form-item label="备注" class="full-row">
            <el-input v-model="form.remarks" type="textarea" :rows="2" maxlength="2000" show-word-limit />
          </el-form-item>
        </div>
      </el-form>
    </div>

    <div class="edit-section lines-section">
      <div class="section-title">
        <el-icon><Grid /></el-icon>
        <span>订单明细</span>
        <span class="section-sub">{{ form.lines.length }} 行 · 数量、单价、折扣率、税率按行录入</span>
        <div class="lines-toolbar">
          <el-button-group size="small">
            <el-button type="primary" @click="addLine"><el-icon><Plus /></el-icon>新增行</el-button>
            <el-button @click="removeSelected"><el-icon><Delete /></el-icon>删除行</el-button>
          </el-button-group>
        </div>
      </div>
      <div class="lines-table-wrap">
        <el-table ref="lineTableRef" :data="form.lines" border stripe size="small" height="420" row-key="clientId" @selection-change="onLineSelectionChange" :header-cell-style="{ background: '#f1f5f9', padding: '0 !important' }" :cell-style="{ padding: '4px 4px', fontSize: '12.5px' }">
          <el-table-column type="selection" width="38" fixed="left" />
          <el-table-column label="行号" type="index" width="50" fixed="left" align="center" />
          <el-table-column label="商品" min-width="260" fixed="left">
            <template #default="{ row }">
              <el-select v-model="row.itemId" filterable remote reserve-keyword placeholder="搜索真实启用物料" :remote-method="searchItems" :loading="itemLoading" style="width:100%" @change="onItemChange(row)">
                <el-option v-for="it in items" :key="it.id" :label="`${it.name} (${it.code})`" :value="it.id" />
              </el-select>
            </template>
          </el-table-column>
          <el-table-column label="单位" width="140"><template #default="{ row }"><el-input v-model="row.uomName" disabled /></template></el-table-column>
          <el-table-column label="数量" width="140"><template #default="{ row }"><el-input-number v-model="row.quantity" :min="0.0001" :precision="4" style="width:100%" /></template></el-table-column>
          <el-table-column label="未税单价" width="140"><template #default="{ row }"><el-input-number v-model="row.unitPrice" :min="0" :precision="4" style="width:100%" /></template></el-table-column>
          <el-table-column label="折扣率" width="120"><template #default="{ row }"><el-input-number v-model="row.discountRate" :min="0" :max="1" :step="0.01" :precision="4" style="width:100%" /></template></el-table-column>
          <el-table-column label="税率" width="120"><template #default="{ row }"><el-input-number v-model="row.taxRate" :min="0" :max="1" :step="0.01" :precision="4" style="width:100%" /></template></el-table-column>
          <el-table-column label="价税合计" width="140" align="right"><template #default="{ row }"><span class="num-cell strong">{{ money(previewTotal(row)) }}</span></template></el-table-column>
          <el-table-column label="备注" min-width="160"><template #default="{ row }"><el-input v-model="row.remarks" placeholder="行备注" /></template></el-table-column>
          <el-table-column label="操作" width="80" fixed="right"><template #default="{ $index }"><el-button text type="danger" @click="removeLine($index)">删除</el-button></template></el-table-column>
        </el-table>
      </div>
      <div class="amount-summary">
        <span>金额汇总</span>
        <b>不含税 {{ money(summary.net) }}</b>
        <b>税额 {{ money(summary.tax) }}</b>
        <b class="grand">价税合计 {{ money(summary.total) }}</b>
      </div>
    </div>

    <div class="edit-section tabs-section">
      <el-tabs model-value="audit" type="border-card">
        <el-tab-pane label="附件" name="attachments"><el-empty description="附件将在文档服务接入后启用" :image-size="60" /></el-tab-pane>
        <el-tab-pane label="审批记录" name="approval"><el-empty description="暂无审批记录" :image-size="60" /></el-tab-pane>
        <el-tab-pane label="操作日志" name="audit"><el-empty description="保存后由后端审计记录提供" :image-size="60" /></el-tab-pane>
      </el-tabs>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage } from 'element-plus';
import { ArrowLeft, Check, Delete, Document, Grid, Plus } from '@element-plus/icons-vue';
import { listBusinessPartners } from '../../api/mdm/business-partner';
import { listItems } from '../../api/mdm/item';
import { listAllUomsActiveOnly } from '../../api/mdm/uom';
import { useSalesOrderStore } from '../../stores/sales-order';
import { useTabsStore } from '../../stores/tabs';
import type { BusinessPartner, Item, Uom } from '../../types/mdm';
import { SALES_STATUS_LABEL, SALES_STATUS_TAG, type SalesOrderStatus } from '../../types/sales-order';

interface EditLine {
  clientId: string;
  itemId: string;
  uomId: string | null;
  uomName: string;
  quantity: number;
  unitPrice: number;
  discountRate: number;
  taxRate: number;
  remarks?: string | null;
}

const route = useRoute();
const router = useRouter();
const store = useSalesOrderStore();
const tabs = useTabsStore();
const id = computed(() => String(route.params.id || 'new'));
const isCreate = computed(() => id.value === 'new');
const orderNo = ref('新建');
const status = ref<SalesOrderStatus>(1);
const statusLabel = computed(() => SALES_STATUS_LABEL[status.value]);
const statusTag = computed(() => SALES_STATUS_TAG[status.value]);
const saving = ref(false);
const documentType = ref('Normal');
const customers = ref<BusinessPartner[]>([]);
const items = ref<Item[]>([]);
const uoms = ref<Uom[]>([]);
const selectedLines = ref<EditLine[]>([]);
const customerLoading = ref(false);
const itemLoading = ref(false);

const form = reactive({
  customerId: '',
  orderDate: new Date().toISOString().slice(0, 10),
  requestedDeliveryDate: null as string | null,
  remarks: '',
  concurrencyVersion: 0,
  lines: [] as EditLine[],
});

const summary = computed(() => {
  const net = form.lines.reduce((s, l) => s + (l.quantity * l.unitPrice * (1 - l.discountRate)), 0);
  const tax = form.lines.reduce((s, l) => s + (l.quantity * l.unitPrice * (1 - l.discountRate) * l.taxRate), 0);
  return { net, tax, total: net + tax };
});

function money(v: number) {
  return new Intl.NumberFormat('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(v || 0);
}
function previewTotal(row: EditLine) {
  const net = row.quantity * row.unitPrice * (1 - row.discountRate);
  return Math.round((net * (1 + row.taxRate)) * 100) / 100;
}
function addLine() {
  form.lines.push({ clientId: `${Date.now()}-${form.lines.length}`, itemId: '', uomId: null, uomName: '', quantity: 1, unitPrice: 0, discountRate: 0, taxRate: 0.13 });
}
function removeLine(index: number) { form.lines.splice(index, 1); }
function onLineSelectionChange(rows: EditLine[]) { selectedLines.value = rows; }
function removeSelected() {
  if (!selectedLines.value.length) return ElMessage.warning('请先选中要删除的行');
  const ids = new Set(selectedLines.value.map(l => l.clientId));
  form.lines = form.lines.filter(l => !ids.has(l.clientId));
}
async function searchCustomers(keyword: string) {
  customerLoading.value = true;
  try {
    const result = await listBusinessPartners({ keyword, role: 1, status: 1, page: 1, pageSize: 50 });
    customers.value = result.items;
  } finally {
    customerLoading.value = false;
  }
}
async function searchItems(keyword: string) {
  itemLoading.value = true;
  try {
    const result = await listItems({ keyword, status: 1, page: 1, pageSize: 50 });
    items.value = result.items;
  } finally {
    itemLoading.value = false;
  }
}
function onItemChange(row: EditLine) {
  const item = items.value.find(x => x.id === row.itemId);
  if (!item) return;
  const uom = uoms.value.find(x => x.id === item.baseUomId);
  row.uomId = item.baseUomId;
  row.uomName = uom ? `${uom.name} (${uom.code})` : item.baseUomId;
}
function toRequest() {
  return {
    customerId: form.customerId,
    orderDate: form.orderDate,
    requestedDeliveryDate: form.requestedDeliveryDate || null,
    remarks: form.remarks || null,
    lines: form.lines.map(l => ({ itemId: l.itemId, uomId: l.uomId, quantity: l.quantity, unitPrice: l.unitPrice, discountRate: l.discountRate, taxRate: l.taxRate, remarks: l.remarks || null })),
  };
}
async function save() {
  if (!form.customerId) return ElMessage.warning('请选择真实启用客户');
  if (form.lines.length === 0 || form.lines.some(l => !l.itemId || !l.uomId)) return ElMessage.warning('请选择真实启用物料');
  saving.value = true;
  try {
    const saved = isCreate.value
      ? await store.createDraft(toRequest())
      : await store.updateDraft(id.value, { ...toRequest(), expectedConcurrencyVersion: form.concurrencyVersion });
    ElMessage.success('销售订单已保存');
    tabs.openDetail(saved.id, saved.orderNo);
    router.replace(`/sales-order/${saved.id}`).catch(() => {});
  } catch (err) {
    ElMessage.error((err as any)?.detail || (err as any)?.title || '保存失败');
  } finally {
    saving.value = false;
  }
}
function goBack() { router.push('/sales-order').catch(() => {}); }
async function loadExisting() {
  if (isCreate.value) { addLine(); return; }
  const dto = await store.fetchDetail(id.value);
  orderNo.value = dto.orderNo;
  status.value = dto.status;
  form.customerId = dto.customerId;
  form.orderDate = dto.orderDate;
  form.requestedDeliveryDate = dto.requestedDeliveryDate ?? null;
  form.remarks = dto.remarks || '';
  form.concurrencyVersion = dto.concurrencyVersion;
  customers.value = [{ id: dto.customerId, code: dto.customerCodeSnapshot, name: dto.customerNameSnapshot } as BusinessPartner];
  form.lines = dto.lines.map(l => ({ clientId: String(l.id), itemId: l.itemId, uomId: l.uomId, uomName: `${l.uomNameSnapshot} (${l.uomCodeSnapshot})`, quantity: l.quantity, unitPrice: l.unitPrice, discountRate: l.discountRate, taxRate: l.taxRate, remarks: l.remarks }));
}
onMounted(async () => {
  uoms.value = await listAllUomsActiveOnly();
  await Promise.all([searchCustomers(''), searchItems('')]);
  await loadExisting();
});
</script>

<style scoped>
.so-edit { display: flex; flex-direction: column; min-height: 100%; background: #f6f7fb; overflow: auto; }
.edit-headerbar { background: #fff; border-bottom: 1px solid #e5e7eb; padding: 8px 16px; display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 8px; position: sticky; top: 0; z-index: 10; box-shadow: 0 2px 4px rgba(0,0,0,0.04); }
.headerbar-left, .headerbar-right, .status-tags { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.doc-no { font-size: 16px; font-weight: 700; color: #1f2937; }
.version-tag { background: #f3f4f6; color: #6b7280; padding: 2px 8px; border-radius: 8px; font-size: 11px; font-family: monospace; }
.edit-section { background: #fff; margin: 8px; border-radius: 6px; padding: 12px 16px; box-shadow: 0 1px 2px rgba(0,0,0,0.04); }
.section-title { display: flex; align-items: center; gap: 6px; font-size: 14px; font-weight: 600; color: #1f2937; margin-bottom: 12px; padding-bottom: 8px; border-bottom: 1px solid var(--bg-subtle); flex-wrap: wrap; }
.section-sub { font-size: 12px; font-weight: 400; color: #9ca3af; margin-left: 8px; }
.form-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 0 16px; }
.form-grid :deep(.el-form-item) { margin-bottom: 12px; }
.form-grid :deep(.full-row) { grid-column: span 4; }
.lines-toolbar { margin-left: auto; display: flex; align-items: center; gap: 8px; }
.lines-table-wrap { width: 100%; overflow: visible; margin-bottom: 8px; }
.amount-summary { display: flex; justify-content: flex-end; gap: 24px; padding: 10px 4px 0; color: #475569; }
.amount-summary .grand, .num-cell.strong { color: #c2410c; font-weight: 700; }
.tabs-section { margin-bottom: 16px; }
</style>
