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
      <!-- B3.6-C Acceptance Debt — column 2 displays the row's
           `lineNo` field (the V1 line number) instead of the
           auto-incremented row index. This is the field the
           LineNo step-10 contract actually operates on, so the
           browser smoke can verify the contract end-to-end
           (initial 10, after delete + batch add 20 -> 30 -> 50). -->
      <el-table-column
        label="行号"
        prop="lineNo"
        width="70"
        fixed="left"
        align="center"
      />

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
              :disabled="String(u.status) !== '1' && u.status !== 'active'"
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
import type { Item, Uom, ItemListParams, UomListParams, PagedResult } from '../../types/mdm';
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
  /**
   * GULIERP_TRANSACTION_PHASE_B2 (Phase B.2 — SalesOrder frontend wire)
   * Optional override for the item fetch used by the inline item picker.
   * Defaults to `listItems` (`/api/v1/mdm/items`), which requires
   * `MdmPolicies.ItemRead`. Single-role operators such as SALES_OPERATOR
   * do NOT have that permission; the editor must pass a customFetcher
   * that hits the SalesOrder context facade (e.g. `listSalesOrderItems`).
   */
  customItemFetcher?: (params: ItemListParams) => Promise<PagedResult<Item>>;
  /**
   * Same boundary concern as customItemFetcher, but for the UoM list
   * loaded when an item is picked. Defaults to `listUoms`
   * (`/api/v1/mdm/uoms`, requires `MdmPolicies.UomRead`).
   */
  customUomFetcher?: (params: UomListParams) => Promise<PagedResult<Uom>>;
  /**
   * GULIERP_TRANSACTION_PHASE_B2_1 (Phase B.2.1 — Numeric Precision
   * alignment). Number of decimals to round the line amount fields
   * (netAmount, taxAmount, totalAmount) to in the client preview.
   * Default 4 preserves the original Phase A behaviour. Pass 2 to
   * match GULIERP_NUMERIC_PRECISION_STANDARD_V1 §7 (commercial
   * amount scale 2) so the preview matches what the V1 server
   * recomputes with `MidpointRounding.AwayFromZero`. For non-negative
   * numbers — which covers all sales/purchase commercial amounts —
   * `Math.round(n * factor) / factor` is exactly equivalent to
   * `MidpointRounding.AwayFromZero` (JS `Math.round` rounds .5 toward
   * positive infinity, which for x >= 0 is away from zero).
   */
  lineAmountScale?: 2 | 4;
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
  // B2.1: configurable line-amount scale. Default 4 preserves Phase A
  // behaviour. Sales Order (and any other commercial document) passes
  // 2 so the preview matches the V1 server's MidpointRounding.AwayFromZero
  // recomputation per GULIERP_NUMERIC_PRECISION_STANDARD_V1 §7.
  const scale = props.lineAmountScale ?? 4;
  const factor = scale === 2 ? 100 : 10000;
  const roundAmount = (n: number) => Math.round(n * factor) / factor;
  row.netAmount = roundAmount(qty * price * (1 - disc));
  row.taxAmount = roundAmount(row.netAmount * tax);
  row.totalAmount = roundAmount(row.netAmount + row.taxAmount);
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
    const fetchItems = props.customItemFetcher ?? listItems;
    const result = await fetchItems({
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
    const fetchUoms = props.customUomFetcher ?? listUoms;
    const uoms = await fetchUoms({ page: 1, pageSize: 100 });
    // B3.6-G — backend serialises `status` as an integer (1=active,
    // 2=inactive) on the context facade; the canonical /api/v1/mdm/uoms
    // still returns the UI string. Accept both so the inline picker
    // auto-fills the unit for the picked item. String() coerces the
    // int into its decimal form for the equality check; the typed
    // Uom's `status` is the string form so 'active' is also valid.
    const active = uoms.items.filter(
      (u) => String(u.status) === '1' || u.status === 'active',
    );
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

// --- B3.6-B: multi-select item batch append ---
// The existing `onPickerRowClick` above is intentionally left
// unchanged. The two flows share `recomputeRow` + `emitUpdate` +
// `emit('item-pick')` so that the editor (and TotalsBar) see the
// same observable events; only the entry point differs.

let batchUomCache: Uom[] | null = null;
let batchUomInflight: Promise<Uom[]> | null = null;

async function fetchActiveUomsForBatch(): Promise<Uom[]> {
  if (batchUomCache) return batchUomCache;
  if (batchUomInflight) return batchUomInflight;
  const fetchUoms = props.customUomFetcher ?? listUoms;
  batchUomInflight = (async () => {
    try {
      const uoms = await fetchUoms({ page: 1, pageSize: 100 });
      // B3.6-G — see onPickerRowClick: accept both int and string
      // status encodings; the predicate change is additive (no
      // contract change).
      return uoms.items.filter(
        (u) => String(u.status) === '1' || u.status === 'active',
      );
    } finally {
      batchUomInflight = null;
    }
  })();
  const result = await batchUomInflight;
  batchUomCache = result;
  return result;
}

// B3.6-B.1 §8 — LineNo allocation must be based on the current
// maximum lineNo, NOT on `localLines.length`. After a user deletes
// a row in the middle of the list, the previous formula
// `(localLines.length + index + 1) * 10` could produce a lineNo that
// collides with an existing row. The new formula reads the max
// lineNo across the surviving lines and assigns
// max + (index + 1) * STEP. The default STEP is 10 (matches the
// existing V1 contract; the B2 onAddLine in SalesOrderEdit uses
// the same 10-multiplier).
const LINE_NO_STEP = 10;
function nextLineNoForBatch(count: number): number {
  const maxExisting = localLines.reduce(
    (acc, l) => (l.lineNo > acc ? l.lineNo : acc),
    0,
  );
  return maxExisting + LINE_NO_STEP;
}

function buildNewRowFromItem(item: Item, baseTime: number, index: number, startLineNo: number): LineGridRow {
  return {
    clientId: `c-${baseTime}-${index}`,
    // B3.6-B.1 §8 — start from the first free lineNo and step
    // forward so a batch of N rows gets lineNo values
    // startLineNo, startLineNo + 10, … startLineNo + (N-1)*10.
    lineNo: startLineNo + index * LINE_NO_STEP,
    itemId: item.id,
    itemCode: item.code,
    itemName: item.name,
    // Per B3.6-B §五 #4 — pass through the Item's current spec; the
    // server is authoritative at save (B3.6-A snapshot rule).
    specification: item.specification,
    uomId: null,
    uomCode: undefined,
    uomName: undefined,
    uomOptions: undefined,
    uomById: undefined,
    // Per B3.6-B §五 #3 — explicit P0 defaults for new batch lines.
    quantity: 1,
    unitPrice: 0,
    discountRate: 0,
    taxRate: 0,
    netAmount: 0,
    taxAmount: 0,
    totalAmount: 0,
    remarks: undefined,
  };
}

function applyUomSelection(row: LineGridRow, active: Uom[], baseUomId?: string | null) {
  // Reuse the same selection logic as the inline picker, so the
  // B2.1 contract (item.baseUomId default; fall back to first active
  // UoM) is preserved for the multi-select flow.
  row.uomOptions = active;
  row.uomById = Object.fromEntries(active.map((u) => [u.id, u]));
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
}

export interface BatchAppendResult {
  added: number;
  skipped: number;
  skippedItems: Item[];
}

async function batchAppendItems(items: Item[]): Promise<BatchAppendResult> {
  if (props.readOnly) {
    return { added: 0, skipped: 0, skippedItems: [] };
  }
  if (!items || items.length === 0) {
    return { added: 0, skipped: 0, skippedItems: [] };
  }
  // Per B3.6-B §五 #7 — dedup by itemId. Already-present items are
  // reported back so the parent can show "已跳过 N 个已存在物料".
  const existing = new Set(
    localLines.map((l) => l.itemId).filter((x): x is string => !!x),
  );
  const fresh: Item[] = [];
  const skipped: Item[] = [];
  for (const it of items) {
    if (existing.has(it.id)) {
      skipped.push(it);
    } else {
      fresh.push(it);
      existing.add(it.id);
    }
  }
  if (fresh.length === 0) {
    return { added: 0, skipped: skipped.length, skippedItems: skipped };
  }
  // Per B3.6-B §五 #5 — fetch UoM once and reuse for every new row
  // (the inline picker fetches per row, which is wasteful for batch).
  let active: Uom[] = [];
  try {
    active = await fetchActiveUomsForBatch();
  } catch {
    // If UoM lookup fails entirely we still add the lines so the
    // user does not lose their selection; the editor will surface the
    // empty UoM state and they can pick a UoM manually per row.
  }
  const baseTime = Date.now();
  // B3.6-B.1 §8 — compute the starting lineNo once (max existing + step)
  // and pass it down so each new row increments from there.
  const startLineNo = nextLineNoForBatch(fresh.length);
  // Per B3.6-B §五 #8 — preserve the caller's selection order.
  fresh.forEach((item, i) => {
    const row = buildNewRowFromItem(item, baseTime, i, startLineNo);
    applyUomSelection(row, active, item.baseUomId);
    recomputeRow(row);
    localLines.push(row);
    emit('item-pick', row, item);
  });
  emitUpdate();
  return { added: fresh.length, skipped: skipped.length, skippedItems: skipped };
}

// Expose to the parent (SalesOrderEdit) so it can call
// batchAppendItems after receiving the `multi-select` event from
// ItemSelectorPopover. We do NOT change the public emit surface —
// the B2/B2.1 events remain identical.
defineExpose({ batchAppendItems });

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
