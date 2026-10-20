<!--
  GULIERP_TRANSACTION_DOCUMENT_LINE_GRID_V1 (Phase A) — transient dev preview.
  Renders all 6 shared transaction components in one page with mock data,
  so the agent can visually verify rendering + keyboard nav. NOT a
  production page. To be removed after Phase A acceptance.
-->
<template>
  <div class="tx-preview">
    <h2>Transaction Components Preview</h2>
    <p class="tx-preview-hint">
      临时验收页(Phase A 共享组件预览)。所有数据为 mock。生产页面将在 Phase B (Sales) / Phase D (Purchase) 落地。
    </p>

    <section class="tx-preview-section">
      <h3>1. 三个 Selector Popover</h3>
      <div class="tx-preview-grid-3">
        <div>
          <label>ItemSelector</label>
          <ItemSelectorPopover
            v-model="form.itemId"
            placeholder="选择物料"
            @select="onItemSelect"
          />
          <p class="tx-preview-debug">modelValue = {{ form.itemId || 'null' }}</p>
        </div>
        <div>
          <label>PartnerSelector (Customer)</label>
          <PartnerSelectorPopover
            v-model="form.partnerId"
            role="Customer"
            placeholder="选择客户"
            @select="onPartnerSelect"
          />
          <p class="tx-preview-debug">modelValue = {{ form.partnerId || 'null' }}</p>
        </div>
        <div>
          <label>WarehouseSelector</label>
          <WarehouseSelectorPopover
            v-model="form.warehouseId"
            placeholder="选择仓库"
            @select="onWarehouseSelect"
          />
          <p class="tx-preview-debug">modelValue = {{ form.warehouseId || 'null' }}</p>
        </div>
      </div>
    </section>

    <section class="tx-preview-section">
      <h3>2. ConcurrencyConflictModal</h3>
      <el-button type="warning" @click="conflictVisible = true">模拟 409 并发冲突</el-button>
      <ConcurrencyConflictModal
        v-model="conflictVisible"
        :actor-name="'张三'"
        :occurred-at="'2026-08-30T08:55:00+08:00'"
        request-id="req-9d3577b05eb4dd9965cde5ab8d77401"
      />
    </section>

    <section class="tx-preview-section">
      <h3>3. LineGrid + TotalsBar(Transaction Editor 内核)</h3>
      <p class="tx-preview-hint">行数 / 总数量 / 净额 / 税额 / 价税合计 实时计算。Tab 切换 cell,Enter 触发 cell change。</p>
      <LineGrid
        v-model:lines="lines"
        @add="onAddLine"
        @remove="onRemoveLine"
        @remove-selected="onRemoveSelected"
        @item-pick="onItemPick"
        @uom-change="onUomChange"
      />
      <TotalsBar :lines="lines" currency-code="CNY" />
    </section>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { ElMessage } from 'element-plus';
import {
  ItemSelectorPopover,
  PartnerSelectorPopover,
  WarehouseSelectorPopover,
  ConcurrencyConflictModal,
  LineGrid,
  TotalsBar,
} from '../../components/transaction';
import type { LineGridRow } from '../../components/transaction';
import type { Item, BusinessPartner, Warehouse } from '../../types/mdm';

const form = reactive({
  itemId: null as string | null,
  partnerId: null as string | null,
  warehouseId: null as string | null,
});

function onItemSelect(item: Item) {
  ElMessage.success(`Selected item: ${item.code} · ${item.name}`);
}
function onPartnerSelect(bp: BusinessPartner) {
  ElMessage.success(`Selected partner: ${bp.code} · ${bp.name}`);
}
function onWarehouseSelect(wh: Warehouse) {
  ElMessage.success(`Selected warehouse: ${wh.code} · ${wh.name}`);
}

const conflictVisible = ref(false);

// Pre-populated mock lines so the LineGrid demonstrates money recalculation
// on cell changes.
const lines = ref<LineGridRow[]>([
  {
    clientId: 'a-1',
    lineNo: 1,
    itemId: 'mock-item-1',
    itemCode: 'DEMO_CHAIR',
    itemName: '示例办公椅',
    specification: 'demo-spec',
    uomId: 'mock-uom-1',
    uomCode: 'PIECE',
    uomName: '件',
    uomOptions: [
      { id: 'mock-uom-1', code: 'PIECE', name: '件', status: 'active' } as any,
      { id: 'mock-uom-2', code: 'BOX', name: '箱', status: 'active' } as any,
    ],
    uomById: {
      'mock-uom-1': { id: 'mock-uom-1', code: 'PIECE', name: '件', status: 'active' } as any,
      'mock-uom-2': { id: 'mock-uom-2', code: 'BOX', name: '箱', status: 'active' } as any,
    },
    quantity: 5,
    unitPrice: 200,
    discountRate: 0,
    taxRate: 0.13,
    netAmount: 1000,
    taxAmount: 130,
    totalAmount: 1130,
  },
  {
    clientId: 'a-2',
    lineNo: 2,
    itemId: null, // pre-pick
    itemCode: undefined,
    itemName: undefined,
    specification: undefined,
    uomId: null,
    uomCode: undefined,
    uomName: undefined,
    quantity: 0,
    unitPrice: 0,
    discountRate: 0,
    taxRate: 0,
    netAmount: 0,
    taxAmount: 0,
    totalAmount: 0,
  },
]);

function onAddLine() {
  const newLine: LineGridRow = {
    clientId: `c-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
    lineNo: lines.value.length + 1,
    itemId: null,
    uomId: null,
    quantity: 0,
    unitPrice: 0,
    discountRate: 0,
    taxRate: 0,
    netAmount: 0,
    taxAmount: 0,
    totalAmount: 0,
  };
  lines.value = [...lines.value, newLine];
}

function onRemoveLine(clientId: string) {
  lines.value = lines.value.filter((l) => l.clientId !== clientId);
}

function onRemoveSelected(clientIds: string[]) {
  lines.value = lines.value.filter((l) => !clientIds.includes(l.clientId));
}

function onItemPick(row: LineGridRow, item: Item) {
  ElMessage.success(`Picked ${item.code} into row ${row.lineNo}`);
}

function onUomChange(row: LineGridRow, uomId: string) {
  ElMessage.info(`UOM changed on row ${row.lineNo} -> ${uomId}`);
}

void ElMessage;
</script>

<style scoped>
.tx-preview {
  padding: 16px 24px 32px;
  max-width: 1280px;
  margin: 0 auto;
}
.tx-preview h2 {
  margin: 0 0 4px;
  font-size: 20px;
}
.tx-preview-hint {
  color: var(--text-muted);
  font-size: 13px;
  margin: 0 0 16px;
}
.tx-preview-section {
  background: var(--bg-elevated, #fff);
  border: 1px solid var(--border-default);
  border-radius: 6px;
  padding: 16px 20px;
  margin-bottom: 16px;
}
.tx-preview-section h3 {
  margin: 0 0 12px;
  font-size: 15px;
  color: var(--text-secondary);
}
.tx-preview-grid-3 {
  display: grid;
  grid-template-columns: 1fr 1fr 1fr;
  gap: 16px;
}
.tx-preview-grid-3 label {
  display: block;
  font-size: 12px;
  color: var(--text-muted);
  margin-bottom: 4px;
}
.tx-preview-debug {
  margin: 6px 0 0;
  font-size: 11px;
  color: var(--text-placeholder);
  font-family: var(--erp-mono-font);
}
</style>
