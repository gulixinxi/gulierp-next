<template>
  <!-- SalesOrderList — list view (G1B-1R3: migrated to GuliERP Design System classes) -->
  <div class="so-list">
    <!-- Top toolbar -->
    <div class="gs-list-toolbar">
      <div class="gs-toolbar-left">
        <el-input
          v-model="filters.keyword"
          placeholder="搜索单号 / 客户 / 客户订单号"
          clearable
          :prefix-icon="SearchIcon"
          style="width: 280px"
          @keyup.enter="applyFilters"
          @clear="applyFilters"
        />
        <el-button type="primary" plain @click="advancedOpen = !advancedOpen">
          <el-icon><Filter /></el-icon>
          <span>高级筛选</span>
          <el-badge v-if="activeFilterCount" :value="activeFilterCount" type="warning" />
        </el-button>
        <el-select v-model="filters.documentStatus" placeholder="单据状态" clearable style="width: 130px" @change="applyFilters">
          <el-option v-for="k,v in docStatusOptions" :key="k" :label="(v as any).label" :value="k as any" />
        </el-select>
        <el-select v-model="filters.approvalStatus" placeholder="审批状态" clearable style="width: 130px" @change="applyFilters">
          <el-option v-for="k,v in apprStatusOptions" :key="k" :label="(v as any).label" :value="k as any" />
        </el-select>
      </div>
      <div class="gs-toolbar-right">
        <el-button text @click="openColumnSettings">
          <el-icon><Setting /></el-icon>
          列设置
        </el-button>
        <el-button text @click="openSavedView">
          <el-icon><Star /></el-icon>
          我的视图
        </el-button>
        <el-button text>
          <el-icon><Download /></el-icon>
          导出
        </el-button>
        <el-divider direction="vertical" />
        <el-button type="primary" @click="onNew">
          <el-icon><Plus /></el-icon>
          新建销售订单
        </el-button>
      </div>
    </div>

    <!-- Advanced filter -->
    <el-collapse-transition>
      <div v-show="advancedOpen" class="gs-advanced-filter">
        <el-form :model="filters" label-width="90px" size="default" label-position="right">
          <div class="gs-filter-grid">
            <el-form-item label="客户">
              <el-input :model-value="filters.customerId ? customerLabel(filters.customerId) : ''" readonly placeholder="点击选择客户">
                <template #append>
                  <el-button :icon="SearchIcon" @click="openLookup('customer')" />
                </template>
              </el-input>
            </el-form-item>
            <el-form-item label="销售员">
              <el-select v-model="filters.salesPersonId" placeholder="全部" clearable filterable style="width:100%">
                <el-option v-for="e in salesPeople" :key="e.id" :label="e.name" :value="e.id" />
              </el-select>
            </el-form-item>
            <el-form-item label="执行状态">
              <el-select v-model="filters.executionStatus" placeholder="全部" clearable style="width:100%">
                <el-option v-for="k,v in execStatusOptions" :key="k" :label="(v as any).label" :value="k as any" />
              </el-select>
            </el-form-item>
            <el-form-item label="订单日期从">
              <el-date-picker v-model="filters.dateFrom" type="date" value-format="YYYY-MM-DD" placeholder="开始日期" style="width:100%" />
            </el-form-item>
            <el-form-item label="订单日期至">
              <el-date-picker v-model="filters.dateTo" type="date" value-format="YYYY-MM-DD" placeholder="结束日期" style="width:100%" />
            </el-form-item>
          </div>
          <div style="text-align:right">
            <el-button @click="resetFilters">重置</el-button>
            <el-button type="primary" @click="applyFilters">应用筛选</el-button>
          </div>
        </el-form>
      </div>
    </el-collapse-transition>

    <!-- Bulk action bar -->
    <transition name="bulk-slide">
      <div v-if="selectedRows.length > 0" class="gs-bulk-bar">
        <span>已选中 <b>{{ selectedRows.length }}</b> 条</span>
        <el-button size="small" type="primary" @click="bulkSubmit">批量提交</el-button>
        <el-button size="small" @click="bulkApprove">批量审核</el-button>
        <el-button size="small" @click="bulkExport">批量导出</el-button>
        <el-button size="small" text @click="clearSelection">取消</el-button>
      </div>
    </transition>

    <!-- Data table -->
    <div class="gs-list-wrap">
      <el-table
        ref="tableRef"
        :data="pagedData"
        border
        stripe
        height="100%"
        size="small"
        row-key="id"
        @row-dblclick="onRowDblClick"
        @selection-change="onSelectionChange"
        @sort-change="onSortChange"
        :header-cell-style="{ padding: '0 8px' }"
        :cell-style="{ padding: '0 8px' }"
      >
        <el-table-column type="selection" width="40" fixed="left" />
        <el-table-column type="index" label="#" width="50" fixed="left" :index="indexMethod" />
        <template v-for="col in visibleColumns" :key="col.prop">
          <el-table-column
            :prop="col.prop"
            :label="col.label"
            :width="col.width"
            :fixed="col.fixed"
            :sortable="sortableProps.includes(col.prop) ? 'custom' : false"
            show-overflow-tooltip
          >
            <template #default="scope">
              <component :is="renderCell(col.prop, scope.row)" />
            </template>
          </el-table-column>
        </template>
        <el-table-column label="操作" width="200" fixed="right" align="center" class-name="actions-col">
          <template #default="{ row }">
            <div class="gs-row-actions">
              <el-button text size="small" type="primary" @click="openDetail(row)">查看</el-button>
              <el-button text size="small" type="primary" :disabled="!computeActions(row).edit" @click="openEdit(row)">编辑</el-button>
              <el-button text size="small" @click="onCopy(row)">复制</el-button>
            </div>
          </template>
        </el-table-column>
        <template #empty>
          <div class="empty-state">
            <el-icon :size="40" :color="borderDefault"><Document /></el-icon>
            <p>暂无销售订单数据</p>
            <el-button type="primary" @click="onNew">
              <el-icon><Plus /></el-icon>
              新建第一张销售订单
            </el-button>
          </div>
        </template>
      </el-table>
    </div>

    <!-- Pagination -->
    <div class="gs-list-pagination">
      <span class="gs-summary-text">
        共 <b>{{ filteredData.length }}</b> 条 · 已选 <b>{{ selectedRows.length }}</b> 条
      </span>
      <el-pagination
        v-model:current-page="page.current"
        v-model:page-size="page.size"
        :total="filteredData.length"
        :page-sizes="[20, 50, 100]"
        layout="sizes, prev, pager, next, jumper"
        background
      />
    </div>

    <!-- Column settings drawer -->
    <el-drawer v-model="columnDrawer" title="列设置 · 可拖拽排序" size="360px">
      <div class="gs-col-settings">
        <p class="gs-field-hint">勾选显示列,拖拽调整顺序。设置保存在当前用户(吴海)。</p>
        <el-checkbox v-model="colWidthAuto" label="自动列宽" />
        <div class="gs-col-list">
          <div
            v-for="(col, idx) in localColumns"
            :key="col.prop"
            class="gs-col-item"
            draggable="true"
            @dragstart="onDragStart(idx)"
            @dragover.prevent
            @drop="onDrop(idx)"
          >
            <el-icon class="gs-col-handle"><Rank /></el-icon>
            <el-checkbox v-model="col.visible">{{ col.label }}</el-checkbox>
            <span class="col-prop">{{ col.prop }}</span>
          </div>
        </div>
      </div>
      <template #footer>
        <el-button @click="columnDrawer = false">取消</el-button>
        <el-button @click="resetColumns">恢复默认</el-button>
        <el-button type="primary" @click="saveColumns">保存</el-button>
      </template>
    </el-drawer>

    <!-- Saved view dialog -->
    <el-dialog v-model="savedViewDialog" title="我的视图(每用户)" width="460px">
      <div class="saved-view">
        <p class="gs-field-hint">当前视图包含 {{ activeFilterCount }} 个筛选条件 + {{ visibleColumns.length }} 列。保存为我的视图后,下次打开列表时自动应用。</p>
        <el-input v-model="savedViewName" placeholder="视图名称,如:我的待审订单">
          <template #append>
            <el-button @click="saveView" type="primary">保存</el-button>
          </template>
        </el-input>
        <el-divider />
        <div class="saved-view-list">
          <div class="sv-item is-default">
            <el-icon><Star /></el-icon>
            <span>默认视图</span>
            <span class="badge">当前默认</span>
          </div>
          <div class="sv-item">
            <el-icon><Tickets /></el-icon>
            <span>我的待审订单</span>
            <el-button text size="small" @click="applyView('待审')">应用</el-button>
            <el-button text size="small" @click="setDefaultView">设为默认</el-button>
          </div>
        </div>
      </div>
    </el-dialog>

    <!-- Lookup popwin -->
    <LookupDialog
      v-model="lookupOpen"
      :entity-type="lookupEntity"
      :filter="lookupFilter"
      @confirm="onLookupConfirm"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, reactive, ref, h, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { ElTag, ElMessage } from 'element-plus';
import { Search as SearchIcon, Plus, Filter, Setting, Download, Star, Document, Rank, Tickets } from '@element-plus/icons-vue';
import { useTabsStore } from '../../stores/tabs';
import { useSalesOrderStore } from '../../stores/sales-order';
import { defaultListColumns, customers, employees } from '../../mock/sales-order';
import type { SalesOrder, SalesOrderListFilters, ListColumnConfig, DocumentStatus, ApprovalStatus, ExecutionStatus } from '../../types/sales-order';
import {
  documentStatusMap, approvalStatusMap, executionStatusMap,
  fmtMoney, fmtDate, fmtDateTime, computeActions
} from '../../utils/status';
import LookupDialog, { type LookupEntity } from '../../components/LookupDialog.vue';

defineOptions({ name: 'SalesOrderList' });

const router = useRouter();
const tabs = useTabsStore();
const soStore = useSalesOrderStore();

const filters = reactive<SalesOrderListFilters>({
  keyword: '', documentStatus: '', approvalStatus: '', executionStatus: '',
  customerId: undefined, salesPersonId: undefined, dateFrom: undefined, dateTo: undefined
});
const advancedOpen = ref(false);
const page = reactive({ current: 1, size: 20 });
const selectedRows = ref<SalesOrder[]>([]);
const tableRef = ref();
const borderDefault = '#CBD5E1';

// Columns (per user, mock persisted in localStorage)
const LS_LIST_COLS = 'erp.so.list.columns';
function loadListColumns() {
  try {
    const raw = localStorage.getItem(LS_LIST_COLS);
    if (raw) {
      const parsed = JSON.parse(raw);
      // merge by prop to keep new default columns from future migrations
      const map = new Map(parsed.map((c: any) => [c.prop, c]));
      return defaultListColumns.map(dc => map.get(dc.prop) || dc);
    }
  } catch (e) { /* ignore */ }
  return JSON.parse(JSON.stringify(defaultListColumns));
}
const localColumns = ref<ListColumnConfig[]>(loadListColumns());
const colWidthAuto = ref(false);
const columnDrawer = ref(false);
const savedViewDialog = ref(false);
const savedViewName = ref('');

const salesPeople = computed(() => employees.filter(e => e.role === 'Sales'));
const docStatusOptions = documentStatusMap;
const apprStatusOptions = approvalStatusMap;
const execStatusOptions = executionStatusMap;
const sortableProps = ['salesOrderNo', 'orderDate', 'totalAmountInclTax', 'createdAt', 'updatedAt'];

const visibleColumns = computed(() => localColumns.value.filter(c => c.visible));

const activeFilterCount = computed(() => {
  let n = 0;
  if (filters.keyword) n++;
  if (filters.documentStatus) n++;
  if (filters.approvalStatus) n++;
  if (filters.executionStatus) n++;
  if (filters.customerId) n++;
  if (filters.salesPersonId) n++;
  if (filters.dateFrom) n++;
  if (filters.dateTo) n++;
  return n;
});

const filteredData = computed(() => {
  let rows = soStore.list();
  const kw = filters.keyword.trim().toLowerCase();
  if (kw) {
    rows = rows.filter(r =>
      r.salesOrderNo.toLowerCase().includes(kw) ||
      r.customerName.toLowerCase().includes(kw) ||
      (r.customerOrderNo || '').toLowerCase().includes(kw)
    );
  }
  if (filters.documentStatus) rows = rows.filter(r => r.documentStatus === filters.documentStatus);
  if (filters.approvalStatus) rows = rows.filter(r => r.approvalStatus === filters.approvalStatus);
  if (filters.executionStatus) rows = rows.filter(r => r.executionStatus === filters.executionStatus);
  if (filters.customerId) rows = rows.filter(r => r.customerId === filters.customerId);
  if (filters.salesPersonId) rows = rows.filter(r => r.salesPersonId === filters.salesPersonId);
  if (filters.dateFrom) rows = rows.filter(r => r.orderDate >= filters.dateFrom!);
  if (filters.dateTo) rows = rows.filter(r => r.orderDate <= filters.dateTo!);
  if (sortState.value.prop) {
    const prop = sortState.value.prop;
    const dir = sortState.value.order === 'ascending' ? 1 : -1;
    rows = [...rows].sort((a, b) => {
      const va = (a as any)[prop];
      const vb = (b as any)[prop];
      if (typeof va === 'number') return (va - vb) * dir;
      return String(va || '').localeCompare(String(vb || '')) * dir;
    });
  }
  return rows;
});

const pagedData = computed(() => {
  const start = (page.current - 1) * page.size;
  return filteredData.value.slice(start, start + page.size);
});

const sortState = ref<{ prop: string; order: 'ascending' | 'descending' | null }>({ prop: 'orderDate', order: 'descending' });

const lookupOpen = ref(false);
const lookupEntity = ref<LookupEntity>('customer');
const lookupFilter = ref<Record<string, any>>({});

function openLookup(entity: LookupEntity, filter: Record<string, any> = {}) {
  lookupEntity.value = entity;
  lookupFilter.value = filter;
  lookupOpen.value = true;
}

function onLookupConfirm(payload: { id: string; label: string; raw: any }) {
  if (lookupEntity.value === 'customer') {
    filters.customerId = payload.id;
  }
}

function customerLabel(id: string) {
  return customers.find(c => c.id === id)?.name || id;
}

function applyFilters() {
  page.current = 1;
}

function resetFilters() {
  filters.keyword = '';
  filters.documentStatus = '';
  filters.approvalStatus = '';
  filters.executionStatus = '';
  filters.customerId = undefined;
  filters.salesPersonId = undefined;
  filters.dateFrom = undefined;
  filters.dateTo = undefined;
  page.current = 1;
}

function onRowDblClick(row: SalesOrder) {
  openDetail(row);
}

function openDetail(row: SalesOrder) {
  tabs.openDetail(row.id, row.salesOrderNo);
  router.push(`/sales-order/${row.id}`).catch(() => {});
}

function openEdit(row: SalesOrder) {
  tabs.openEdit(row.id, row.salesOrderNo, 'edit');
  router.push(`/sales-order/${row.id}/edit`).catch(() => {});
}

function onNew() {
  tabs.openEdit('new', '新建', 'create');
  router.push('/sales-order/new/edit').catch(() => {});
}

function onCopy(row: SalesOrder) {
  const newNo = `SO-${row.orderDate.replace(/-/g, '')}-${String(Math.floor(Math.random() * 9000) + 1000)}`;
  const cloned = soStore.clone(row.id, newNo);
  if (cloned) {
    ElMessage.success(`已复制 ${row.salesOrderNo} → ${newNo}(草稿)`);
    tabs.openEdit(cloned.id, newNo, 'edit');
    router.push(`/sales-order/${cloned.id}/edit`).catch(() => {});
  }
}

function onSelectionChange(rows: SalesOrder[]) {
  selectedRows.value = rows;
}
function clearSelection() { tableRef.value?.clearSelection(); }

function bulkSubmit() {
  const canSubmit = selectedRows.value.filter(r => computeActions(r).submit);
  ElMessage.success(`已提交 ${canSubmit.length} 张订单(Mock)`);
  clearSelection();
}
function bulkApprove() {
  const canApprove = selectedRows.value.filter(r => computeActions(r).approve);
  ElMessage.success(`已审核 ${canApprove.length} 张订单(Mock)`);
  clearSelection();
}
function bulkExport() {
  ElMessage.success(`已开始导出 ${selectedRows.value.length} 条数据到 Excel(Mock)`);
}

function onSortChange({ prop, order }: { prop: string; order: 'ascending' | 'descending' | null }) {
  sortState.value = { prop, order };
}

function indexMethod(idx: number) {
  return (page.current - 1) * page.size + idx + 1;
}

// Column rendering per prop
function renderCell(prop: string, row: SalesOrder) {
  switch (prop) {
    case 'salesOrderNo':
      return h('a', {
        class: 'cell-link',
        onClick: (e: Event) => { e.stopPropagation(); openDetail(row); }
      }, row.salesOrderNo);
    case 'customerName':
      return h('span', { title: row.customerCode }, row.customerName);
    case 'totalAmountInclTax':
    case 'totalAmountExclTax':
    case 'totalTaxAmount':
    case 'totalDiscountAmount':
      return h('span', { class: 'gs-num-cell' }, fmtMoney((row as any)[prop]));
    case 'totalQuantity':
      return h('span', { class: 'gs-num-cell' }, fmtMoney((row as any)[prop], 0));
    case 'documentStatus': {
      const m = documentStatusMap[row.documentStatus];
      return h(ElTag, { type: m.tag, effect: 'light', size: 'small' }, () => m.label);
    }
    case 'approvalStatus': {
      const m = approvalStatusMap[row.approvalStatus];
      return h(ElTag, { type: m.tag, effect: 'light', size: 'small' }, () => m.label);
    }
    case 'executionStatus': {
      const m = executionStatusMap[row.executionStatus];
      return h(ElTag, { type: m.tag, effect: 'light', size: 'small' }, () => m.label);
    }
    case 'paymentTermCode': {
      const pt = (row as any)[prop];
      return h('span', pt);
    }
    case 'currencyCode':
      return h('span', row.currencyCode);
    case 'orderDate':
    case 'requestedDeliveryDate':
      return h('span', fmtDate((row as any)[prop]));
    case 'createdAt':
    case 'updatedAt':
      return h('span', fmtDateTime((row as any)[prop]));
    case 'salesPersonName':
    case 'createdBy':
      return h('span', (row as any)[prop]);
    default:
      return h('span', String((row as any)[prop] ?? ''));
  }
}

// Column settings
let dragIdx = -1;
function onDragStart(idx: number) { dragIdx = idx; }
function onDrop(idx: number) {
  if (dragIdx < 0 || dragIdx === idx) return;
  const arr = localColumns.value;
  const moved = arr[dragIdx];
  arr.splice(dragIdx, 1);
  arr.splice(idx, 0, moved);
  dragIdx = -1;
}

function openColumnSettings() {
  columnDrawer.value = true;
}

function saveColumns() {
  localStorage.setItem(LS_LIST_COLS, JSON.stringify(localColumns.value));
  columnDrawer.value = false;
  ElMessage.success('列设置已保存(本用户:吴海,刷新后仍生效)');
}

function resetColumns() {
  localColumns.value = JSON.parse(JSON.stringify(defaultListColumns));
  ElMessage.info('已恢复默认列设置');
}

function openSavedView() {
  savedViewDialog.value = true;
}

function saveView() {
  if (!savedViewName.value) {
    ElMessage.warning('请输入视图名称');
    return;
  }
  ElMessage.success(`视图 "${savedViewName.value}" 已保存`);
  savedViewDialog.value = false;
  savedViewName.value = '';
}

function applyView(name: string) {
  ElMessage.success(`已应用视图 "${name}"`);
  savedViewDialog.value = false;
}

function setDefaultView() {
  ElMessage.success('已设为默认视图(下次打开列表时自动应用)');
}

onMounted(() => {
  // Ensure list tab is active when entering list view
  tabs.setActive('list-sales-order');
});
</script>

<style scoped>
.so-list {
  display: flex;
  flex-direction: column;
  height: 100%;
  background: var(--bg-container);
}
.gs-list-wrap {
  flex: 1;
  min-height: 0;
  overflow: hidden;
  padding: 0;
  margin: var(--section-gutter) var(--section-gutter) 0;
}
.empty-state {
  padding: 60px 0;
  text-align: center;
  color: var(--text-muted);
}
.empty-state p { margin: 12px 0; font-size: 14px; }

.col-prop { color: var(--text-muted); font-size: 11px; margin-left: auto; font-family: var(--erp-mono-font); }
.saved-view-list { margin-top: 8px; }
.sv-item {
  display: flex;
  align-items: center;
  gap: var(--gap);
  padding: 10px 8px;
  border: 1px solid var(--border-default);
  border-radius: var(--radius-sm);
  margin-bottom: 6px;
  font-size: var(--erp-font-size);
}
.sv-item.is-default { background: #FFFBEB; border-color: #FDE68A; }
.sv-item .badge { background: #FEF3C7; color: #92400E; padding: 1px 8px; border-radius: 8px; font-size: 11px; margin-left: auto; }
.sv-item .el-button { margin-left: auto; }

:deep(.cell-link) { color: var(--color-blue-600); text-decoration: none; cursor: pointer; font-weight: 500; }
:deep(.cell-link:hover) { text-decoration: underline; }
:deep(.el-table__body) .el-table__row:hover { background: var(--primary-bg) !important; }

.bulk-slide-enter-active, .bulk-slide-leave-active { transition: all .2s; }
.bulk-slide-enter-from, .bulk-slide-leave-to { transform: translateY(-100%); opacity: 0; }
</style>
