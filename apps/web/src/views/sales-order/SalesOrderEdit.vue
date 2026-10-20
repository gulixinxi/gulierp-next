<!--
  GULIERP_TRANSACTION_PHASE_B2 (Phase B.2 — SalesOrder frontend wire)
  SalesOrderEdit — Real Sales Order editor wired to the Phase A shared
  components and the G3-R2A backend. Replaces the earlier custom-built
  draft with a Section A/B/C/D layout that honours
  GULIERP_TRANSACTION_DOCUMENT_UX_CONTRACT_V1 §4 and
  GULIERP_SALES_ORDER_REDESIGN_V1 §14.

  Three actions (UX Contract §10):
    - 保存草稿 (Save Draft)            : create or update, no status change
    - 提交确认 (Submit / Confirm)      : save then transition Draft → Confirmed
    - 取消单据 (Cancel Document)       : transition to Cancelled

  3 places where we cross the SALES_OPERATOR boundary (G3-R1C):
    - Customer → PartnerSelectorPopover with customFetcher = listSalesOrderCustomers
    - Item     → LineGrid with customItemFetcher = listSalesOrderItems
    - UoM      → LineGrid with customUomFetcher = listSalesOrderUoms
  These three fetcher overrides are the §三十一 D "minimal adapter" the brief
  allows; default behaviour is unchanged when no fetcher is provided.
-->
<template>
  <div class="so-edit" v-loading="loading">
    <!-- Sticky Header Bar -->
    <div class="edit-headerbar">
      <div class="headerbar-left">
        <el-button text @click="goBack">
          <el-icon><ArrowLeft /></el-icon>返回
        </el-button>
        <el-divider direction="vertical" />
        <span class="doc-no">{{ docNoLabel }}</span>
        <div class="status-tags">
          <el-tag :type="SALES_STATUS_TAG[status]" size="small" effect="dark">
            {{ SALES_STATUS_LABEL[status] }}
          </el-tag>
        </div>
        <span class="version-tag">v{{ form.concurrencyVersion }}</span>
      </div>
      <div class="headerbar-right">
        <el-button :loading="saving" :disabled="!canEdit" @click="onSaveDraft">
          <el-icon><Check /></el-icon>保存草稿
        </el-button>
        <el-button type="success" :loading="confirming" :disabled="!canSubmit" @click="onSubmitConfirm">
          <el-icon><Select /></el-icon>提交确认
        </el-button>
        <el-button type="danger" plain :loading="cancelling" :disabled="!canCancel" @click="onCancel">
          <el-icon><CircleClose /></el-icon>取消单据
        </el-button>
      </div>
    </div>

    <!-- Section A: Master Table (主表) — B3.6-G renames
         "业务主信息" → "主表" (paired with Section C 明细表) -->
    <div class="edit-section">
      <div class="section-title">
        <el-icon><Document /></el-icon>
        <span>主表</span>
        <span class="section-sub">客户、日期、币种、默认仓库、收货地址按当前真实业务填写</span>
      </div>
      <el-form :model="form" label-width="100px" label-position="right">
        <div class="form-grid core-grid">
          <el-form-item label="单据编号">
            <el-input :model-value="isCreate ? '保存时自动生成' : orderNo" disabled />
          </el-form-item>
          <el-form-item label="订单日期" required>
            <el-date-picker
              v-model="form.orderDate"
              type="date"
              value-format="YYYY-MM-DD"
              style="width:100%"
              :disabled="!canEditHeaderCore"
            />
          </el-form-item>
          <el-form-item label="要求交货日期">
            <el-date-picker
              v-model="form.requestedDeliveryDate"
              type="date"
              value-format="YYYY-MM-DD"
              clearable
              style="width:100%"
              :disabled="!canEditRequestedDeliveryDate"
            />
          </el-form-item>
          <el-form-item label="币种">
            <el-input model-value="CNY" disabled />
          </el-form-item>
          <el-form-item label="客户" required>
            <PartnerSelectorPopover
              v-model="form.customerId"
              role="Customer"
              placeholder="点击选择真实启用客户"
              :custom-fetcher="fetchSalesOrderCustomers"
              @select="onCustomerSelect"
            />
          </el-form-item>
          <el-form-item label="客户编码">
            <el-input :model-value="form.customerCodeSnapshot" disabled />
          </el-form-item>
          <el-form-item label="默认仓库">
            <WarehouseSelectorPopover
              v-model="form.warehouseId"
              :selected-warehouse="warehouseProjection"
              placeholder="点击选择仓库（可选）"
              :disabled="!canEditHeaderCore"
              @select="onWarehouseSelect"
            />
          </el-form-item>
          <!-- B3.6-G — F-UI-02: 收货地址 moves from Section B to
               Section A, after 默认仓库. Data semantics unchanged
               (still a header snapshot field); only the visual
               placement shifts. -->
          <el-form-item label="收货地址" class="full-row">
            <el-input
              v-model="form.deliveryAddressSnapshot"
              type="textarea"
              :rows="2"
              maxlength="500"
              show-word-limit
              placeholder="收货地址（提交确认时锁定快照）"
              :disabled="!canEditDeliveryAddress"
            />
          </el-form-item>
        </div>
      </el-form>
    </div>

    <!-- Section B: Supplemental — B3.6-G keeps the title and the
         region semantics ("跨单据扩展区"), but compresses the
         remaining field (备注) by reducing rows from 3 to 2.
         收货地址 moved to Section A. -->
    <div class="edit-section">
      <div class="section-title">
        <el-icon><Document /></el-icon>
        <span>补充信息</span>
        <span class="section-sub">备注</span>
      </div>
      <el-form :model="form" label-width="100px" label-position="right">
        <el-form-item label="备注" class="full-row">
          <el-input
            v-model="form.remarks"
            type="textarea"
            :rows="2"
            maxlength="2000"
            show-word-limit
            :disabled="!canEditRemarks"
          />
        </el-form-item>
      </el-form>
    </div>

    <!-- Section C: Detail Table (明细表) — B3.6-G renames
         "单据明细" → "明细表" (paired with Section A 主表) -->
    <div class="edit-section lines-section">
      <div class="section-title">
        <el-icon><Grid /></el-icon>
        <span>明细表</span>
        <span class="section-sub">{{ form.lines.length }} 行 · 数量、单价、折扣率、税率按行录入</span>
      </div>
      <!-- B3.6-B.1 — multi-select item entry toolbar. The existing
           single "添加行" button (LineGrid toolbar) is preserved; the
           new "批量添加物料" button opens a dialog containing the
           ItemSelectorPopover in 'multi' mode.

           We use el-dialog (not el-popover) because nesting an
           ItemSelectorPopover (which is itself an el-popover) inside
           another el-popover is unstable in Element Plus — the
           outer popover's reference slot's click event is not
           reliably wired. A dialog does not have this nesting issue
           and works as a stable container for the multi-select flow. -->
      <div class="line-batch-toolbar">
        <el-button
          type="primary"
          plain
          :disabled="!canEditLines"
          :icon="Goods"
          @click="openBatchPicker"
        >
          批量添加物料
        </el-button>
        <span class="line-batch-hint">
          提示：点击物料行即可勾选/取消；底部“确认添加”一次性写入明细。
        </span>
      </div>
      <el-dialog
        v-model="batchPickerVisible"
        title="批量添加物料"
        width="600"
        :close-on-click-modal="false"
        :show-close="true"
        append-to-body
        destroy-on-close
      >
        <ItemSelectorPopover
          :model-value="null"
          mode="multi"
          inline
          placeholder="点击选择物料（可多选）"
          :custom-fetcher="fetchSalesOrderItems"
          @multi-select="onMultiAdd"
        />
      </el-dialog>
      <LineGrid
        ref="lineGridRef"
        v-model:lines="form.lines"
        :read-only="!canEditLines"
        :line-amount-scale="2"
        :custom-item-fetcher="fetchSalesOrderItems"
        :custom-uom-fetcher="fetchSalesOrderUoms"
        @add="onAddLine"
        @remove="onRemoveLine"
        @remove-selected="onRemoveSelected"
      />
    </div>

    <!-- Section D: Totals (sticky bottom) -->
    <TotalsBar :lines="form.lines" currency-code="CNY" />

    <!-- Concurrency Conflict Modal -->
    <ConcurrencyConflictModal
      v-model="conflictVisible"
      :request-id="conflictInfo.requestId"
      @refresh="onRefreshAfterConflict"
      @cancel="conflictVisible = false"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { ArrowLeft, Check, CircleClose, Document, Goods, Grid, Select } from '@element-plus/icons-vue';
import { ApiError } from '../../api/http';
import {
  listSalesOrderCustomers,
  listSalesOrderItems,
  listSalesOrderUoms,
  listSalesOrderWarehouses,
} from '../../api/sales-order-context';
import { useSalesOrderStore } from '../../stores/sales-order';
import { useTabsStore } from '../../stores/tabs';
import type { BusinessPartner, Item, Uom, Warehouse, PagedResult } from '../../types/mdm';
import {
  SALES_STATUS_LABEL,
  SALES_STATUS_TAG,
  type CreateSalesOrderRequest,
  type SalesOrderStatus,
  type UpdateSalesOrderRequest,
} from '../../types/sales-order';
import type { LineGridRow } from '../../components/transaction/LineGrid.vue';
import LineGrid from '../../components/transaction/LineGrid.vue';
import ItemSelectorPopover from '../../components/transaction/ItemSelectorPopover.vue';
import TotalsBar from '../../components/transaction/TotalsBar.vue';
import ConcurrencyConflictModal from '../../components/transaction/ConcurrencyConflictModal.vue';
import PartnerSelectorPopover from '../../components/transaction/PartnerSelectorPopover.vue';
import WarehouseSelectorPopover from '../../components/transaction/WarehouseSelectorPopover.vue';

defineOptions({ name: 'SalesOrderEdit' });

const route = useRoute();
const router = useRouter();
const store = useSalesOrderStore();
const tabs = useTabsStore();

const id = computed(() => String(route.params.id || 'new'));
const isCreate = computed(() => id.value === 'new');

const orderNo = ref('新建');
const status = ref<SalesOrderStatus>(1);
const loading = ref(false);
const saving = ref(false);
const confirming = ref(false);
const cancelling = ref(false);
const conflictVisible = ref(false);
const conflictInfo = reactive<{ actorName?: string; occurredAt?: string; requestId?: string }>({});

const form = reactive({
  customerId: '' as string,
  customerCodeSnapshot: '',
  customerNameSnapshot: '',
  orderDate: new Date().toISOString().slice(0, 10),
  requestedDeliveryDate: null as string | null,
  // B3.6-A additions.
  warehouseId: null as string | null,
  warehouseCode: '',
  warehouseName: '',
  deliveryAddressSnapshot: '' as string,
  remarks: '' as string,
  concurrencyVersion: 0,
  lines: [] as LineGridRow[],
});

// B3.6-A — V1 Sales §6 editability boundary:
// Draft:        full edit on every field.
// Confirmed:    only Remarks, RequestedDeliveryDate, DeliveryAddressSnapshot
//               remain editable; everything else frozen.
// Cancelled:    no editing at all.
const canEdit = computed(() => status.value === 1 && !loading.value);
const canEditDraftOnly = computed(() => canEdit.value);
const canEditConfirmedSoftFields = computed(
  () => status.value === 2 && !loading.value
);
const canEditHeaderCore = computed(() => canEditDraftOnly.value);
const canEditRequestedDeliveryDate = computed(
  () => canEditDraftOnly.value || canEditConfirmedSoftFields.value
);
const canEditDeliveryAddress = computed(
  () => canEditDraftOnly.value || canEditConfirmedSoftFields.value
);

// B3.6-C UI Integration — explicit warehouse code/name projection
// for the popover. The form already keeps the snapshot code/name
// (loaded from the server at loadExisting or captured by
// onWarehouseSelect). Pass them through so the popover can render
// `${code} · ${name}` even when the warehouse is not in the
// current `hits` (the legacy popover rendered empty in that case).
const warehouseProjection = computed(() => {
  if (!form.warehouseId) return null;
  return {
    id: form.warehouseId,
    code: form.warehouseCode || null,
    name: form.warehouseName || null,
  };
});
const canEditRemarks = computed(
  () => canEditDraftOnly.value || canEditConfirmedSoftFields.value
);
const canEditLines = computed(() => canEditDraftOnly.value);
const canSubmit = computed(
  () => status.value === 1 && !!form.customerId && form.lines.length > 0
);
const canCancel = computed(() => status.value === 1 || status.value === 2); // Draft or Confirmed
const docNoLabel = computed(() =>
  isCreate.value ? '新建销售订单' : (orderNo.value || '加载中')
);

// ---- Custom fetchers (Phase A boundary adapter for SALES_OPERATOR) ----
// The context facade endpoints require only `SalesOrderRead` (which
// SALES_OPERATOR has) instead of `MdmPolicies.*Read` (which it does not).
// Cast is safe: the context facade returns the same MDM DTO shape.
async function fetchSalesOrderCustomers(
  params: { keyword?: string; page?: number; pageSize?: number }
): Promise<PagedResult<BusinessPartner>> {
  const r = await listSalesOrderCustomers(params);
  return r as unknown as PagedResult<BusinessPartner>;
}
async function fetchSalesOrderItems(
  params: { keyword?: string; page?: number; pageSize?: number }
): Promise<PagedResult<Item>> {
  const r = await listSalesOrderItems(params);
  return r as unknown as PagedResult<Item>;
}
async function fetchSalesOrderUoms(
  params: { keyword?: string; page?: number; pageSize?: number }
): Promise<PagedResult<Uom>> {
  const r = await listSalesOrderUoms(params);
  return r as unknown as PagedResult<Uom>;
}

// ---- Validation (P0 minimum per brief §二十三) ----
function validate(): string | null {
  if (!form.customerId) return '请选择真实启用客户';
  if (form.lines.length === 0) return '请至少添加一条明细行';
  for (let i = 0; i < form.lines.length; i++) {
    const l = form.lines[i];
    if (!l.itemId) return `第 ${i + 1} 行未选择物料`;
    if (!l.uomId) return `第 ${i + 1} 行未选择单位`;
    if (l.quantity <= 0) return `第 ${i + 1} 行数量必须大于 0`;
    if (l.unitPrice < 0) return `第 ${i + 1} 行单价不能为负`;
    if (l.discountRate < 0 || l.discountRate > 1) {
      return `第 ${i + 1} 行折扣率必须在 0% 到 100% 之间`;
    }
    if (l.taxRate < 0 || l.taxRate > 1) {
      return `第 ${i + 1} 行税率必须在 0% 到 100% 之间`;
    }
  }
  return null;
}

function buildLinesRequest() {
  return form.lines.map((l) => ({
    itemId: l.itemId!,
    uomId: l.uomId,
    quantity: l.quantity,
    unitPrice: l.unitPrice,
    discountRate: l.discountRate,
    taxRate: l.taxRate,
    remarks: l.remarks || null,
    // B3.6-A: server is authoritative; the client carries the last value
    // it saw so the snapshot is preserved across page reloads.
    specificationSnapshot: l.specification ?? null,
  }));
}

function buildCreateRequest(): CreateSalesOrderRequest {
  return {
    customerId: form.customerId,
    orderDate: form.orderDate,
    requestedDeliveryDate: form.requestedDeliveryDate || null,
    remarks: form.remarks || null,
    lines: buildLinesRequest(),
    // B3.6-A additions. Sales P0: both nullable; empty string treated as null.
    warehouseId: form.warehouseId || null,
    deliveryAddressSnapshot: form.deliveryAddressSnapshot?.trim() || null,
  };
}

function buildUpdateRequest(): UpdateSalesOrderRequest {
  return {
    ...buildCreateRequest(),
    expectedConcurrencyVersion: form.concurrencyVersion,
  };
}

function applyServerState(dto: { id: string; orderNo: string; status: SalesOrderStatus; concurrencyVersion: number; customerCodeSnapshot: string; customerNameSnapshot: string }) {
  orderNo.value = dto.orderNo;
  status.value = dto.status;
  form.concurrencyVersion = dto.concurrencyVersion;
  form.customerCodeSnapshot = dto.customerCodeSnapshot;
  form.customerNameSnapshot = dto.customerNameSnapshot;
}

// ---- Save Draft (UX Contract §10: 保存草稿) ----
async function onSaveDraft() {
  const err = validate();
  if (err) {
    ElMessage.warning(err);
    return;
  }
  saving.value = true;
  try {
    const saved = isCreate.value
      ? await store.createDraft(buildCreateRequest())
      : await store.updateDraft(id.value, buildUpdateRequest());
    applyServerState(saved);
    ElMessage.success('销售订单已保存');
    if (isCreate.value) {
      // After create, switch from create-mode to update-mode.
      tabs.openEdit(saved.id, saved.orderNo, 'edit');
      await router.replace(`/sales-orders/${saved.id}/edit`).catch(() => {});
    }
  } catch (err) {
    handleApiError(err, '保存失败');
  } finally {
    saving.value = false;
  }
}

// ---- Submit / Confirm (UX Contract §10: 提交确认) ----
async function onSubmitConfirm() {
  const err = validate();
  if (err) {
    ElMessage.warning(err);
    return;
  }
  await ElMessageBox.confirm(
    '提交确认后单据将不可再修改客户和明细，是否继续？',
    '提交确认销售订单'
  );
  saving.value = true;
  confirming.value = true;
  try {
    // Persist first (create or update), then transition Draft → Confirmed.
    const persisted = isCreate.value
      ? await store.createDraft(buildCreateRequest())
      : await store.updateDraft(id.value, buildUpdateRequest());
    applyServerState(persisted);
    if (isCreate.value) {
      tabs.openEdit(persisted.id, persisted.orderNo, 'edit');
      await router.replace(`/sales-orders/${persisted.id}/edit`).catch(() => {});
    }
    const confirmed = await store.confirm(persisted.id);
    applyServerState(confirmed);
    ElMessage.success('销售订单已确认');
    await router.push(`/sales-orders/${confirmed.id}`).catch(() => {});
  } catch (err) {
    handleApiError(err, '提交确认失败');
  } finally {
    saving.value = false;
    confirming.value = false;
  }
}

// ---- Cancel (UX Contract §10: 取消单据) ----
async function onCancel() {
  const targetId = isCreate.value ? store.current?.id : id.value;
  if (!targetId) {
    ElMessage.warning('当前单据尚未保存，无法取消');
    return;
  }
  let reason: string | null = null;
  if (status.value === 2) {
    // Confirmed → require reason (V1 Sales §9: server rejects blank reason).
    const result = await ElMessageBox.prompt(
      '已确认单据需填写取消原因',
      '取消销售订单',
      {
        confirmButtonText: '确定取消',
        cancelButtonText: '不取消',
        inputPattern: /.+/,
        inputErrorMessage: '原因必填',
        inputValue: '',
      }
    ).catch(() => ({ value: null as string | null }));
    if (result.value === null) return;
    reason = result.value.trim() || null;
  } else {
    const confirmed = await ElMessageBox.confirm('确定取消该销售订单？', '取消销售订单', {
      type: 'warning',
    }).catch(() => null);
    if (confirmed === null) return;
  }
  cancelling.value = true;
  try {
    // B3.6-A: reason passes through; null for Draft cancel, required for Confirmed.
    const cancelled = await store.cancel(targetId, reason);
    applyServerState(cancelled);
    ElMessage.success('销售订单已取消');
    await router.push(`/sales-orders/${cancelled.id}`).catch(() => {});
  } catch (err) {
    handleApiError(err, '取消失败');
  } finally {
    cancelling.value = false;
  }
}

// ---- Customer / Warehouse select ----
function onCustomerSelect(partner: BusinessPartner) {
  form.customerCodeSnapshot = partner.code;
  form.customerNameSnapshot = partner.name;
}
function onWarehouseSelect(warehouse: Warehouse) {
  form.warehouseCode = warehouse.code;
  form.warehouseName = warehouse.name;
}

// ---- Line grid handlers ----
// B3.6-B: template ref to LineGrid for batchAppendItems calls.
const lineGridRef = ref<InstanceType<typeof LineGrid> | null>(null);
// B3.6-B.1 — multi-select item entry dialog. The button opens the
// dialog; the dialog closes on 确认添加 (programmatic via
// `batchPickerVisible = false` after a successful multi-select) or
// when the user clicks the dialog's close button.
const batchPickerVisible = ref(false);
function openBatchPicker() {
  if (!canEditLines.value) return;
  batchPickerVisible.value = true;
}

function onAddLine() {
  form.lines.push({
    clientId: `c-${Date.now()}-${form.lines.length}`,
    lineNo: (form.lines.length + 1) * 10,
    itemId: null,
    itemCode: undefined,
    itemName: undefined,
    specification: undefined,
    uomId: null,
    uomCode: undefined,
    uomName: undefined,
    quantity: 1,
    unitPrice: 0,
    discountRate: 0,
    taxRate: 0,
    netAmount: 0,
    taxAmount: 0,
    totalAmount: 0,
    remarks: undefined,
  });
}

// B3.6-B: receive `multi-select` from ItemSelectorPopover and forward
// the items to LineGrid.batchAppendItems. The dedup + UoM + line
// creation logic lives inside LineGrid; this handler is the thin
// glue and surfaces the per-popover "skipped" feedback to the user.
async function onMultiAdd(items: Item[]) {
  batchPickerVisible.value = false;
  if (!items || items.length === 0) {
    return;
  }
  const grid = lineGridRef.value;
  if (!grid || typeof grid.batchAppendItems !== 'function') {
    ElMessage.error('批量添加物料失败：明细组件未就绪');
    return;
  }
  try {
    const result = await grid.batchAppendItems(items);
    const codeList = (result.skippedItems ?? [])
      .map((it) => it.code)
      .filter((c): c is string => !!c)
      .slice(0, 5)
      .join('、');
    const more = (result.skippedItems?.length ?? 0) > 5 ? '…' : '';
    if (result.added > 0 && result.skipped > 0) {
      ElMessage.success(
        `已新增 ${result.added} 行；已跳过 ${result.skipped} 个已存在物料${codeList ? `（${codeList}${more}）` : ''}`,
      );
    } else if (result.added > 0) {
      ElMessage.success(`已新增 ${result.added} 行物料`);
    } else if (result.skipped > 0) {
      ElMessage.warning(`所选 ${result.skipped} 个物料均已存在，未新增${codeList ? `（${codeList}${more}）` : ''}`);
    }
  } catch (err) {
    handleApiError(err, '批量添加物料失败');
  }
}
function onRemoveLine(clientId: string) {
  form.lines = form.lines.filter((l) => l.clientId !== clientId);
}
function onRemoveSelected(clientIds: string[]) {
  const ids = new Set(clientIds);
  form.lines = form.lines.filter((l) => !ids.has(l.clientId));
}

// ---- Centralised error handling with concurrency-conflict detection ----
function handleApiError(err: unknown, fallbackMsg: string) {
  if (err instanceof ApiError) {
    if (err.code === 'concurrency_conflict') {
      conflictVisible.value = true;
      conflictInfo.requestId = err.requestId;
      return;
    }
    ElMessage.error(err.detail || err.title || fallbackMsg);
    return;
  }
  const anyErr = err as { detail?: string; title?: string; message?: string };
  ElMessage.error(anyErr?.detail || anyErr?.title || anyErr?.message || fallbackMsg);
}

// ---- Refresh after concurrency conflict (UX Contract §11) ----
async function onRefreshAfterConflict() {
  conflictVisible.value = false;
  if (!isCreate.value) {
    await loadExisting();
    ElMessage.info('已刷新为最新版本');
  }
}

// ---- Navigation ----
function goBack() {
  if (isCreate.value) {
    router.push('/sales-orders').catch(() => {});
  } else {
    router.push(`/sales-orders/${id.value}`).catch(() => {});
  }
}

// ---- Load existing ----
async function loadExisting() {
  if (isCreate.value) {
    orderNo.value = '新建';
    status.value = 1;
    form.concurrencyVersion = 0;
    if (form.lines.length === 0) onAddLine();
    return;
  }
  loading.value = true;
  try {
    const dto = await store.fetchDetail(id.value);
    orderNo.value = dto.orderNo;
    status.value = dto.status;
    form.customerId = dto.customerId;
    form.customerCodeSnapshot = dto.customerCodeSnapshot;
    form.customerNameSnapshot = dto.customerNameSnapshot;
    form.orderDate = dto.orderDate;
    form.requestedDeliveryDate = dto.requestedDeliveryDate ?? null;
    // B3.6-A additions.
    form.warehouseId = dto.warehouseId ?? null;
    form.deliveryAddressSnapshot = dto.deliveryAddressSnapshot || '';
    form.remarks = dto.remarks || '';
    form.concurrencyVersion = dto.concurrencyVersion;
    form.lines = dto.lines.map((l) => ({
      clientId: `c-${l.id}`,
      lineNo: l.lineNo,
      itemId: l.itemId,
      itemCode: l.itemCodeSnapshot,
      itemName: l.itemNameSnapshot,
      specification: l.specificationSnapshot ?? undefined,
      uomId: l.uomId,
      uomCode: l.uomCodeSnapshot,
      uomName: l.uomNameSnapshot,
      quantity: l.quantity,
      unitPrice: l.unitPrice,
      discountRate: l.discountRate,
      taxRate: l.taxRate,
      netAmount: l.netAmount,
      taxAmount: l.taxAmount,
      totalAmount: l.totalAmount,
      remarks: l.remarks || undefined,
    }));
    // B3.6-H: hydrate uomOptions/uomById for each loaded line so
    // the LineGrid el-select can resolve the bound uomId back to
    // a label (without this, the reload render falls back to the
    // raw uomId string because the option list is empty). The
    // status predicate matches the B3.6-G fix (accept both int 1
    // and string 'active' encodings from the context facade).
    const uomsRes = await fetchSalesOrderUoms({ page: 1, pageSize: 100 });
    const activeUoms: Uom[] = uomsRes.items.filter(
      (u) => String(u.status) === '1' || u.status === 'active',
    );
    const uomByIdMap: Record<string, Uom> = Object.fromEntries(
      activeUoms.map((u) => [u.id, u]),
    );
    form.lines = form.lines.map((l) => ({
      ...l,
      uomOptions: activeUoms,
      uomById: uomByIdMap,
    }));
  } catch (err) {
    handleApiError(err, '销售订单加载失败');
  } finally {
    loading.value = false;
  }
}

onMounted(loadExisting);
</script>

<style scoped>
.so-edit {
  display: flex;
  flex-direction: column;
  min-height: 100%;
  background: #f6f7fb;
  overflow: auto;
}
.edit-headerbar {
  background: #fff;
  border-bottom: 1px solid #e5e7eb;
  padding: 8px 16px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
  position: sticky;
  top: 0;
  z-index: 10;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.04);
}
.headerbar-left,
.headerbar-right,
.status-tags {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}
.doc-no {
  font-size: 16px;
  font-weight: 700;
  color: #1f2937;
}
.version-tag {
  background: #f3f4f6;
  color: #6b7280;
  padding: 2px 8px;
  border-radius: 8px;
  font-size: 11px;
  font-family: monospace;
}
.edit-section {
  background: #fff;
  margin: 8px;
  border-radius: 6px;
  padding: 12px 16px;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04);
}
/* B3.6-B: multi-select item entry toolbar. Sits between the
   section title and the LineGrid. */
.line-batch-toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
  margin-bottom: 8px;
}
.line-batch-hint {
  color: var(--text-muted);
  font-size: 12px;
}
.section-title {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 14px;
  font-weight: 600;
  color: #1f2937;
  margin-bottom: 12px;
  padding-bottom: 8px;
  border-bottom: 1px solid var(--bg-subtle);
  flex-wrap: wrap;
}
.section-sub {
  font-size: 12px;
  font-weight: 400;
  color: #9ca3af;
  margin-left: 8px;
}
.form-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0 16px;
}
.form-grid :deep(.el-form-item) {
  margin-bottom: 12px;
}
.form-grid :deep(.full-row) {
  grid-column: 1 / -1;
}
.lines-section {
  margin-bottom: 80px; /* leave room for sticky TotalsBar */
}
</style>
