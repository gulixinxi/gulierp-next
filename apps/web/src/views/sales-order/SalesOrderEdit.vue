<template>
  <div class="so-edit">
    <div class="doc-head">
      <div>
        <h2>{{ isCreate ? '新建销售订单' : `编辑 ${orderNo}` }}</h2>
        <p>客户、物料和单位均来自真实主数据；金额由后端最终计算。</p>
      </div>
      <div class="doc-actions">
        <el-button @click="goBack">返回</el-button>
        <el-button type="primary" :loading="saving" @click="save">保存草稿</el-button>
      </div>
    </div>

    <el-form class="doc-form" label-width="110px">
      <div class="form-grid">
        <el-form-item label="客户">
          <el-select v-model="form.customerId" filterable remote reserve-keyword placeholder="搜索真实启用客户" :remote-method="searchCustomers" :loading="customerLoading" style="width:100%">
            <el-option v-for="c in customers" :key="c.id" :label="`${c.name} (${c.code})`" :value="c.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="订单日期">
          <el-date-picker v-model="form.orderDate" type="date" value-format="YYYY-MM-DD" style="width:100%" />
        </el-form-item>
        <el-form-item label="要求交期">
          <el-date-picker v-model="form.requestedDeliveryDate" type="date" value-format="YYYY-MM-DD" clearable style="width:100%" />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="form.remarks" maxlength="2000" show-word-limit />
        </el-form-item>
      </div>
    </el-form>

    <div class="line-toolbar">
      <strong>订单明细</strong>
      <el-button type="primary" plain @click="addLine">新增行</el-button>
    </div>
    <el-table :data="form.lines" border size="small">
      <el-table-column label="#" type="index" width="48" />
      <el-table-column label="物料" min-width="260">
        <template #default="{ row }">
          <el-select v-model="row.itemId" filterable remote reserve-keyword placeholder="搜索真实启用物料" :remote-method="searchItems" :loading="itemLoading" style="width:100%" @change="onItemChange(row)">
            <el-option v-for="it in items" :key="it.id" :label="`${it.name} (${it.code})`" :value="it.id" />
          </el-select>
        </template>
      </el-table-column>
      <el-table-column label="单位" width="150">
        <template #default="{ row }">
          <el-input v-model="row.uomName" disabled />
        </template>
      </el-table-column>
      <el-table-column label="数量" width="130">
        <template #default="{ row }"><el-input-number v-model="row.quantity" :min="0.0001" :precision="4" style="width:100%" /></template>
      </el-table-column>
      <el-table-column label="单价" width="130">
        <template #default="{ row }"><el-input-number v-model="row.unitPrice" :min="0" :precision="4" style="width:100%" /></template>
      </el-table-column>
      <el-table-column label="折扣率" width="120">
        <template #default="{ row }"><el-input-number v-model="row.discountRate" :min="0" :max="1" :step="0.01" :precision="4" style="width:100%" /></template>
      </el-table-column>
      <el-table-column label="税率" width="120">
        <template #default="{ row }"><el-input-number v-model="row.taxRate" :min="0" :max="1" :step="0.01" :precision="4" style="width:100%" /></template>
      </el-table-column>
      <el-table-column label="预览金额" width="130" align="right">
        <template #default="{ row }">{{ money(previewTotal(row)) }}</template>
      </el-table-column>
      <el-table-column label="操作" width="80">
        <template #default="{ $index }"><el-button text type="danger" @click="removeLine($index)">删除</el-button></template>
      </el-table-column>
    </el-table>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage } from 'element-plus';
import { listBusinessPartners } from '../../api/mdm/business-partner';
import { listItems } from '../../api/mdm/item';
import { listAllUomsActiveOnly } from '../../api/mdm/uom';
import { useSalesOrderStore } from '../../stores/sales-order';
import { useTabsStore } from '../../stores/tabs';
import type { BusinessPartner, Item, Uom } from '../../types/mdm';

interface EditLine {
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
const saving = ref(false);
const customers = ref<BusinessPartner[]>([]);
const items = ref<Item[]>([]);
const uoms = ref<Uom[]>([]);
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

function money(v: number) {
  return new Intl.NumberFormat('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(v || 0);
}
function previewTotal(row: EditLine) {
  const net = row.quantity * row.unitPrice * (1 - row.discountRate);
  return Math.round((net * (1 + row.taxRate)) * 100) / 100;
}
function addLine() {
  form.lines.push({ itemId: '', uomId: null, uomName: '', quantity: 1, unitPrice: 0, discountRate: 0, taxRate: 0.13 });
}
function removeLine(index: number) {
  form.lines.splice(index, 1);
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
    lines: form.lines.map(l => ({
      itemId: l.itemId,
      uomId: l.uomId,
      quantity: l.quantity,
      unitPrice: l.unitPrice,
      discountRate: l.discountRate,
      taxRate: l.taxRate,
      remarks: l.remarks || null,
    })),
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

function goBack() {
  router.push('/sales-order').catch(() => {});
}

async function loadExisting() {
  if (isCreate.value) {
    addLine();
    return;
  }
  const dto = await store.fetchDetail(id.value);
  orderNo.value = dto.orderNo;
  form.customerId = dto.customerId;
  form.orderDate = dto.orderDate;
  form.requestedDeliveryDate = dto.requestedDeliveryDate ?? null;
  form.remarks = dto.remarks || '';
  form.concurrencyVersion = dto.concurrencyVersion;
  customers.value = [{ id: dto.customerId, code: dto.customerCodeSnapshot, name: dto.customerNameSnapshot } as BusinessPartner];
  form.lines = dto.lines.map(l => ({
    itemId: l.itemId,
    uomId: l.uomId,
    uomName: `${l.uomNameSnapshot} (${l.uomCodeSnapshot})`,
    quantity: l.quantity,
    unitPrice: l.unitPrice,
    discountRate: l.discountRate,
    taxRate: l.taxRate,
    remarks: l.remarks,
  }));
}

onMounted(async () => {
  uoms.value = await listAllUomsActiveOnly();
  await Promise.all([searchCustomers(''), searchItems('')]);
  await loadExisting();
});
</script>

<style scoped>
.so-edit { height: 100%; overflow: auto; background: var(--bg-container); padding: var(--section-gutter); }
.doc-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 14px; }
.doc-head h2 { margin: 0; font-size: 18px; }
.doc-head p { margin: 4px 0 0; color: var(--text-muted); font-size: 13px; }
.doc-actions { display: flex; gap: 8px; }
.doc-form { border: 1px solid var(--border-default); padding: 16px 16px 0; margin-bottom: 12px; }
.form-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 0 16px; }
.line-toolbar { display: flex; justify-content: space-between; align-items: center; margin: 12px 0; }
</style>
