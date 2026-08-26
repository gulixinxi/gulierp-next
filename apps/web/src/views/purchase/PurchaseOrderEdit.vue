<template>
  <div class="po-edit">
    <div class="edit-headerbar">
      <div class="headerbar-left">
        <el-button text @click="goBack"><el-icon><ArrowLeft /></el-icon>返回</el-button>
        <el-divider direction="vertical" />
        <span class="doc-no">{{ isCreate ? '新建采购订单' : orderNo }}</span>
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
        <span>采购头信息</span>
        <span class="section-sub">供应商和单据基本数据均来自真实接口</span>
      </div>
      <el-form :model="form" label-width="92px" label-position="right">
        <div class="form-grid core-grid">
          <el-form-item label="单据编号">
            <el-input :model-value="isCreate ? '保存时自动生成' : orderNo" disabled />
          </el-form-item>
          <el-form-item label="订单日期" required>
            <el-date-picker v-model="form.orderDate" type="date" value-format="YYYY-MM-DD" style="width:100%" />
          </el-form-item>
          <el-form-item label="预计交期">
            <el-date-picker v-model="form.expectedDeliveryDate" type="date" value-format="YYYY-MM-DD" clearable style="width:100%" />
          </el-form-item>
          <el-form-item label="单据类型">
            <el-select v-model="documentType" disabled style="width:100%">
              <el-option label="普通采购" value="Normal" />
            </el-select>
          </el-form-item>
          <el-form-item label="供应商" required>
            <el-select v-model="form.supplierId" filterable remote reserve-keyword placeholder="搜索真实启用供应商" :remote-method="searchSuppliers" :loading="supplierLoading" style="width:100%">
              <el-option v-for="s in suppliers" :key="s.id" :label="`${s.name} (${s.code})`" :value="s.id" />
            </el-select>
          </el-form-item>
          <el-form-item label="采购员">
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
        <span>采购明细</span>
        <span class="section-sub">{{ form.lines.length }} 行 · 数量、单价、折扣、税率在保存时由后端校验</span>
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
              <el-select v-model="row.itemId" filterable remote reserve-keyword placeholder="搜索真实启用商品" :remote-method="searchItems" :loading="itemLoading" style="width:100%" @change="onItemChange(row)">
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
        <span>合计：</span>
        <b>不含税 {{ money(summary.net) }}</b>
        <b>税额 {{ money(summary.tax) }}</b>
        <b class="grand">价税合计 {{ money(summary.total) }}</b>
      </div>
    </div>

    <div class="edit-section tabs-section">
      <el-tabs model-value="audit" type="border-card">
        <el-tab-pane label="附件" name="attachments"><el-empty description="无附加文档" :image-size="60" /></el-tab-pane>
        <el-tab-pane label="审批记录" name="approval"><el-empty description="无审批记录" :image-size="60" /></el-tab-pane>
        <el-tab-pane label="操作日志" name="audit"><el-empty description="保存完成后逐步提供" :image-size="60" /></el-tab-pane>
      </el-tabs>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage } from 'element-plus';
import { ArrowLeft, Check, Delete, Document, Grid, Plus } from '@element-plus/icons-vue';
// G3-R2B: switch from the standard MDM read APIs (which require
// mdm.* perms) to the PurchaseOrder context facade (which requires
// only PurchaseOrderRead — preserves the G3-R1C boundary contract).
import { listPurchaseOrderSuppliers, listPurchaseOrderItems, listPurchaseOrderUoms } from '../../api/purchase-order-context';
import { usePurchaseOrderStore } from '../../stores/purchase-order';
import { useTabsStore } from '../../stores/tabs';
import type { BusinessPartner, Item, Uom } from '../../types/mdm';
import { PURCH_STATUS_LABEL, PURCH_STATUS_TAG, type PurchaseOrderStatus } from '../../types/purchase-order';

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
const store = usePurchaseOrderStore();
const tabs = useTabsStore();
const id = computed(() => String(route.params.id || 'new'));
const isCreate = computed(() => id.value === 'new');
const orderNo = ref('新建');
const status = ref<PurchaseOrderStatus>(1);
const statusLabel = computed(() => PURCH_STATUS_LABEL[status.value]);
const statusTag = computed(() => PURCH_STATUS_TAG[status.value]);
const saving = ref(false);
const documentType = ref('Normal');
const suppliers = ref<BusinessPartner[]>([]);
const items = ref<Item[]>([]);
const uoms = ref<Uom[]>([]);
const selectedLines = ref<EditLine[]>([]);
const supplierLoading = ref(false);
const itemLoading = ref(false);

const form = reactive({
  supplierId: '',
  orderDate: new Date().toISOString().slice(0, 10),
  expectedDeliveryDate: null as string | null,
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
  if (!selectedLines.value.length) return ElMessage.warning('请先选择要删除的行');
  const ids = new Set(selectedLines.value.map(l => l.clientId));
  form.lines = form.lines.filter(l => !ids.has(l.clientId));
}
async function searchSuppliers(keyword: string) {
  supplierLoading.value = true;
  try {
    const result = await listPurchaseOrderSuppliers({ keyword, page: 1, pageSize: 50 });
    suppliers.value = result.items as unknown as BusinessPartner[];
  } finally {
    supplierLoading.value = false;
  }
}
async function searchItems(keyword: string) {
  itemLoading.value = true;
  try {
    const result = await listPurchaseOrderItems({ keyword, page: 1, pageSize: 50 });
    items.value = result.items as unknown as Item[];
  } finally {
    itemLoading.value = false;
  }
}
async function loadUoms() {
  // UOMs are not bound to a search field; load all active UOMs once.
  const result = await listPurchaseOrderUoms({ page: 1, pageSize: 200 });
  uoms.value = result.items as unknown as Uom[];
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
    supplierId: form.supplierId,
    orderDate: form.orderDate,
    expectedDeliveryDate: form.expectedDeliveryDate || null,
    remarks: form.remarks || null,
    lines: form.lines.map(l => ({ itemId: l.itemId, uomId: l.uomId, quantity: l.quantity, unitPrice: l.unitPrice, discountRate: l.discountRate, taxRate: l.taxRate, remarks: l.remarks || null })),
  };
}
async function save() {
  if (!form.supplierId) return ElMessage.warning('请选择真实启用供应商');
  if (form.lines.length === 0 || form.lines.some(l => !l.itemId || !l.uomId)) return ElMessage.warning('请选择真实启用商品');
  saving.value = true;
  try {
    const saved = isCreate.value
      ? await store.createDraft(toRequest())
      : await store.updateDraft(id.value, { ...toRequest(), expectedConcurrencyVersion: form.concurrencyVersion });
    ElMessage.success('采购订单已保存');
    tabs.openDetail(saved.id, saved.orderNo);
    router.replace(`/purchase-orders/${saved.id}`).catch(() => {});
  } catch (err) {
    ElMessage.error((err as any)?.detail || (err as any)?.title || '保存失败');
  } finally {
    saving.value = false;
  }
}
function goBack() { router.push('/purchase/orders').catch(() => {}); }
async function loadExisting() {
  if (isCreate.value) { addLine(); return; }
  const dto = await store.fetchDetail(id.value);
  orderNo.value = dto.orderNo;
  status.value = dto.status;
  form.supplierId = dto.supplierId;
  form.orderDate = dto.orderDate;
  form.expectedDeliveryDate = dto.expectedDeliveryDate ?? null;
  form.remarks = dto.remarks || '';
  form.concurrencyVersion = dto.concurrencyVersion;
  suppliers.value = [{ id: dto.supplierId, code: dto.supplierCodeSnapshot, name: dto.supplierNameSnapshot } as BusinessPartner];
  form.lines = dto.lines.map(l => ({ clientId: String(l.id), itemId: l.itemId, uomId: l.uomId, uomName: `${l.uomNameSnapshot} (${l.uomCodeSnapshot})`, quantity: l.quantity, unitPrice: l.unitPrice, discountRate: l.discountRate, taxRate: l.taxRate, remarks: l.remarks }));
}
onMounted(async () => {
  // G3-R2B: load UOMs via the PurchaseOrder context facade instead
  // of the standard mdm/uom endpoint.
  await loadUoms();
  await Promise.all([searchSuppliers(''), searchItems('')]);
  await loadExisting();
});
</script>

<style scoped>
.po-edit { display: flex; flex-direction: column; min-height: 100%; background: #f6f7fb; overflow: auto; }
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
