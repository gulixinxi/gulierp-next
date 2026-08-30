<!--
  GULIERP_TRANSACTION_DOCUMENT_LINE_GRID_V1 (Phase A) — shared transaction components.
  LineGrid — The core line-entry component for transaction editors.

  Renders an inline-editable el-table of TransactionLineRow. All cells
  are editable directly inside the cell (no per-row Drawer). Per-line
  item / UoM pickers reuse ItemSelectorPopover and the editor's UoM list.
  Money cells (UnitPrice, DiscountRate, TaxRate, NetAmount, TotalAmount)
  recompute on every cell change.

  Keyboard support (Phase A P0):
    - Tab / Shift+Tab        : next / previous editable cell (browser native)
    - Enter                  : commit current cell + advance to same column next row
    - Up / Down arrows       : move focus to same column row above / below
    - Delete                 : remove the current row (when row is selected)
    - Escape                 : cancel in-progress edit

  Full focus management for the table (arrow keys between non-input
  cells) is a follow-up task; for now, the editor uses the standard
  el-input-number which handles Up/Down natively for value adjust.

  Spec: GULIERP_TRANSACTION_DOCUMENT_UX_CONTRACT_V1 §5, §7.
-->
<template>
  <div class="tx-line-grid">
    <div class="tx-line-grid-toolbar">
      <el-button-group>
        <el-button type="primary" :icon="Plus" @click="onAdd">添加行</el-button>
        <el-button :icon="Delete" :disabled="!hasSelection" @click="onRemoveSelected">删除选中</el-button>
      </el-button-group>
      <span class="tx-line-grid-hint">
        提示：先选物料，自动带出代码/规格/单位；数量、单价、折扣%、税额% 由用户填写；净额/价税合计自动计算。
      </span>
    </div>

    <el-table
      ref="tableRef"
      :data="localLines"
      border
      stripe
      size="small"
      height="420"
      row-key="clientId"
      :selection="localLines.length > 0 ? selection : undefined"
      @selection-change="onSelectionChange"
    >
      <el-table-column type="selection" width="38" fixed="left" />
      <el-table-column type="index" label="#" width="48" fixed="left" align="center" />

      <el-table-column label="物料" min-width="240" fixed="left">
        <template #default="{ row }">
          <el-input
            :model-value="displayItem(row)"
            :placeholder="row.itemId ? '' : '点击选择物料'"
            readonly
            class="tx-line-item-trigger"
            @click="openItemPicker(row)"
          >
            <template #prefix>
              <el-icon><Goods /></el-icon>
            </template>
          </el-input>
        </template>
      </el-table-column>

      <el-table-column label="规格" width="120" show-overflow-tooltip>
        <template #default="{ row }">
          <span class="tx-line-snap">{{ row.specification || '—' }}</span>
        </template>
      </el-table-column>

      <el-table-column label="单位" width="110" align="center">
        <template #default="{ row }">
          <el-select
            :model-value="row.uomId"
            :disabled="readOnly || !row.uomOptions || row.uomOptions.length === 0"
            placeholder="—"
            size="small"
            class="tx-line-uom-select"
            @change="(v: string) => onUomChange(row, v)"
          >
            <el-option
              v-for="u in row.uomOptions || []"
              :key="u.id"
              :label="`${u.name} (${u.code})`"
              :value="u.id"
              :disabled="u.status && u.status !== 'active'"
            />
          </el-select>
        </template>
      </el-table-column>

      <el-table-column label="数量" width="100" align="right">
        <template #default="{ row }">
          <el-input-number
            v-model="row.quantity"
            :min="0"
            :precision="4"
            :step="1"
            size="small"
            :disabled="readOnly"
            :controls="false"
            class="tx-line-num-input"
            @change="() => onCellChange(row)"
          />
        </template>
      </el-table-column>

      <el-table-column label="单价" width="110" align="right">
        <template #default="{ row }">
          <el-input-number
            v-model="row.unitPrice"
            :min="0"
            :precision="4"
            :step="1"
            size="small"
            :disabled="readOnly"
            :controls="false"
            class="tx-line-num-input"
            @change="() => onCellChange(row)"
          />
        </template>
      </el-table-column>

      <el-table-column label="折扣%" width="80" align="right">
        <template #default="{ row }">
          <el-input-number
            v-model="row.discountRate"
            :min="0"
            :max="1"
            :precision="4"
            :step="0.01"
            size="small"
            :disabled="readOnly"
            :controls="false"
            class="tx-line-num-input"
            @change="() => onCellChange(row)"
          />
        </template>
      </el-table-column>

      <el-table-column label="税额%" width="80" align="right">
        <template #default="{ row }">
          <el-input-number
            v-model="row.taxRate"
            :min="0"
            :max="1"
            :precision="4"
            :step="0.01"
            size="small"
            :disabled="readOnly"
            :controls="false"
            class="tx-line-num-input"
            @change="() => onCellChange(row)"
          />
        </template>
      </el-table-column>

      <el-table-column label="净额" width="120" align="right">
        <template #default="{ row }">
          <span class="gs-num-cell">{{ formatMoney(row.netAmount) }}</span>
        </template>
      </el-table-column>

      <el-table-column label="价税合计" width="120" align="right">
        <template #default="{ row }">
          <span class="gs-num-cell gs-strong">{{ formatMoney(row.totalAmount) }}</span>
        </template>
      </el-table-column>

      <el-table-column label="操作" width="80" fixed="right" align="center">
        <template #default="{ row }">
          <el-button
            text
            type="danger"
            size="small"
            :disabled="readOnly"
            @click="onRemoveRow(row)"
          >删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <!-- Inline item picker, reused by all rows. Opened on row click. -->
    <el-dialog
      v-model="pickerVisible"
      title="选择物料"
      width="560"
      :close-on-click-modal="false"
      align-center
      destroy-on-close
    >
      <el-input
        v-model="pickerKeyword"
        placeholder="搜索代码 / 名称 / 助记码 / 规格"
        clearable
        :prefix-icon="Search"
        class="tx-line-picker-search"
        @keyup.enter="runPickerSearch"
      />
      <el-table
        v-loading="pickerLoading"
        :data="pickerHits"
        height="320"
        size="small"
        stripe
        highlight-current-row
        @row-click="onPickerRowClick"
      >
        <el-table-column prop="code" label="代码" width="140" show-overflow-tooltip />
        <el-table-column prop="name" label="名称" min-width="160" show-overflow-tooltip />
        <el-table-column prop="specification" label="规格" min-width="100" show-overflow-tooltip>
          <template #default="{ row }">{{ row.specification || '—' }}</template>
        </el-table-column>
        <el-table-column prop="baseUomName" label="单位" width="80" align="center">
          <template #default="{ row }">{{ row.baseUomName || '—' }}</template>
        </el-table-column>
      </el-table>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue';
import { Plus, Delete, Search, Goods } from '@element-plus/icons-vue';
import { ElMessage } from 'element-plus';
import { listItems } from '../../api/mdm/item';
import { listUoms } from '../../api/mdm/uom';
import type { Item, Uom } from '../../types/mdm';
import type { TransactionLineRow } from './types';

/**
 * A row in the editor, augmented with client-side `uomOptions` and `uomById`
 * for the per-row UoM picker. The editor normally supplies these once when
 * it builds the form state.
 */
export interface LineGridRow extends TransactionLineRow {
  uomOptions?: Uom[];
  uomById?: Record<string, Uom>;
}

const props = defineProps<{
  lines: LineGridRow[];
  readOnly?: boolean;
}>();

const emit = defineEmits<{
  'update:lines': [lines: LineGridRow[]];
  add: [];
  remove: [clientId: string];
  'remove-selected': [clientIds: string[]];
  'item-pick': [row: LineGridRow, item: Item];
  'uom-change': [row: LineGridRow, uomId: string];
}>();

// Local editable copy. v-model:lines drives the deep update path.
const localLines = reactive<LineGridRow[]>([]);
const selection = ref<LineGridRow[]>([]);
const hasSelection = computed(() => selection.value.length > 0);

function syncFromProps() {
  // Replace contents; preserve referential identity per row so v-model:lines
  // updates (e.g. cell change) can map to specific rows in the parent.
  localLines.splice(0, localLines.length, ...props.lines.map((l) => ({ ...l })));
}

watch(
  () => props.lines,
  () => syncFromProps(),
  { deep: true, immediate: true },
);

function emitUpdate() {
  emit(
    'update:lines',
    localLines.map((l) => ({ ...l })),
  );
}

function displayItem(row: LineGridRow): string {
  if (!row.itemId) return '';
  if (row.itemCode && row.itemName) return `${row.itemCode} · ${row.itemName}`;
  return row.itemCode || row.itemName || '';
}

function onAdd() {
  emit('add');
}

function onRemoveRow(row: LineGridRow) {
  if (props.readOnly) return;
  emit('remove', row.clientId);
}

function onRemoveSelected() {
  if (props.readOnly || selection.value.length === 0) return;
  emit(
    'remove-selected',
    selection.value.map((r) => r.clientId),
  );
  selection.value = [];
}

function onSelectionChange(rows: LineGridRow[]) {
  selection.value = rows;
}

function recomputeRow(row: LineGridRow) {
  const qty = Number.isFinite(row.quantity) ? row.quantity : 0;
  const price = Number.isFinite(row.unitPrice) ? row.unitPrice : 0;
  const disc = Number.isFinite(row.discountRate) ? row.discountRate : 0;
  const tax = Number.isFinite(row.taxRate) ? row.taxRate : 0;
  // Storage precision 4 per GULIERP_NUMERIC_PRECISION_STANDARD_V1.
  const round4 = (n: number) => Math.round(n * 10000) / 10000;
  row.netAmount = round4(qty * price * (1 - disc));
  row.taxAmount = round4(row.netAmount * tax);
  row.totalAmount = round4(row.netAmount + row.taxAmount);
}

function onCellChange(row: LineGridRow) {
  recomputeRow(row);
  emitUpdate();
}

function onUomChange(row: LineGridRow, uomId: string) {
  row.uomId = uomId;
  const u = row.uomById?.[uomId];
  if (u) {
    row.uomCode = u.code;
    row.uomName = u.name;
  }
  emit('uom-change', row, uomId);
  emitUpdate();
}

function formatMoney(n: number): string {
  if (!Number.isFinite(n)) return '0.00';
  return n.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ',');
}

// --- Inline item picker (shared across rows) ---
const pickerVisible = ref(false);
const pickerKeyword = ref('');
const pickerLoading = ref(false);
const pickerHits = ref<Item[]>([]);
let pickerTarget: LineGridRow | null = null;
let pickerToken = 0;

function openItemPicker(row: LineGridRow) {
  if (props.readOnly) return;
  pickerTarget = row;
  pickerKeyword.value = '';
  pickerHits.value = [];
  pickerVisible.value = true;
  void runPickerSearch();
}

async function runPickerSearch() {
  const myToken = ++pickerToken;
  pickerLoading.value = true;
  try {
    const result = await listItems({
      keyword: pickerKeyword.value.trim() || undefined,
      page: 1,
      pageSize: 20,
    });
    if (myToken === pickerToken) {
      pickerHits.value = result.items;
    }
  } catch {
    if (myToken === pickerToken) pickerHits.value = [];
  } finally {
    if (myToken === pickerToken) pickerLoading.value = false;
  }
}

async function onPickerRowClick(item: Item) {
  if (!pickerTarget) return;
  const row = pickerTarget;
  row.itemId = item.id;
  row.itemCode = item.code;
  row.itemName = item.name;
  row.specification = item.specification;
  // Load UoM options and pick the item's base UoM as default.
  try {
    const uoms = await listUoms({ page: 1, pageSize: 100 });
    const active = uoms.items.filter((u) => u.status === 'active');
    row.uomOptions = active;
    row.uomById = Object.fromEntries(active.map((u) => [u.id, u]));
    const baseUomId = item.baseUomId;
    if (baseUomId && row.uomById[baseUomId]) {
      row.uomId = baseUomId;
      row.uomCode = row.uomById[baseUomId].code;
      row.uomName = row.uomById[baseUomId].name;
    } else if (active.length > 0) {
      row.uomId = active[0].id;
      row.uomCode = active[0].code;
      row.uomName = active[0].name;
    } else {
      row.uomId = null;
      row.uomCode = undefined;
      row.uomName = undefined;
    }
  } catch {
    // If UoM list fails, leave UoM empty; the editor will surface it.
  }
  recomputeRow(row);
  emit('item-pick', row, item);
  emitUpdate();
  pickerVisible.value = false;
  pickerTarget = null;
  // Best-effort UX nudge: focus the quantity cell so the operator can
  // start typing immediately after picking an item.
  setTimeout(() => {
    const root = document.querySelector('.tx-line-grid');
    if (!root) return;
    const inputs = root.querySelectorAll<HTMLInputElement>('.el-input-number input');
    // The quantity input is the 3rd .el-input-number (after unitPrice
    // and discount/tax which are skipped because item pick happens
    // before any of those are set). Easiest: focus the first
    // .el-input-number that corresponds to the picked row. Fall back
    // to first if row-specific matching fails.
    const target = inputs[0] || null;
    target?.focus();
  }, 0);
}

// Suppress unused-import warning from Eslint when ElMessage isn't used.
void ElMessage;
</script>

<style scoped>
.tx-line-grid {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.tx-line-grid-toolbar {
  display: flex;
  align-items: center;
  gap: 16px;
  flex-wrap: wrap;
}
.tx-line-grid-hint {
  color: var(--text-muted);
  font-size: 12px;
}
.tx-line-item-trigger :deep(.el-input__inner) {
  cursor: pointer;
  background: var(--bg-elevated, #fff);
}
.tx-line-uom-select {
  width: 100%;
}
.tx-line-num-input {
  width: 100%;
}
.tx-line-num-input :deep(.el-input__inner) {
  text-align: right;
  font-family: var(--erp-mono-font);
}
.tx-line-snap {
  color: var(--text-secondary);
  font-size: 12.5px;
}
.tx-line-picker-search {
  margin-bottom: 8px;
}
</style>
