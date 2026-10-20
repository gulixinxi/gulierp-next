<template>
  <div class="so-list">
    <div class="gs-list-toolbar">
      <div class="gs-toolbar-left">
        <el-input v-model="filters.keyword" placeholder="搜索单号 / 客户编码 / 客户名称" clearable :prefix-icon="SearchIcon" class="so-search" @keyup.enter="applyFilters" @clear="applyFilters" />
        <el-button type="primary" plain @click="advancedOpen = !advancedOpen">
          <el-icon><Filter /></el-icon><span>高级筛选</span>
          <el-badge v-if="activeFilterCount" :value="activeFilterCount" type="warning" />
        </el-button>
        <el-select v-model="filters.status" placeholder="单据状态" clearable class="so-status-filter" @change="applyFilters">
          <el-option :value="1" label="草稿" />
          <el-option :value="2" label="已确认" />
          <el-option :value="3" label="已取消" />
        </el-select>
      </div>
      <div class="gs-toolbar-right">
        <el-button text @click="columnDrawer = true"><el-icon><Setting /></el-icon>列设置</el-button>
        <el-button text @click="savedViewDialog = true"><el-icon><Star /></el-icon>我的视图</el-button>
        <el-button text @click="exportUnsupported"><el-icon><Download /></el-icon>导出</el-button>
        <el-divider direction="vertical" />
        <el-button @click="load"><el-icon><Refresh /></el-icon>刷新</el-button>
        <el-button type="primary" @click="onNew"><el-icon><Plus /></el-icon>新建销售订单</el-button>
      </div>
    </div>

    <el-collapse-transition>
      <div v-show="advancedOpen" class="gs-advanced-filter">
        <el-form label-width="90px" label-position="right">
          <div class="gs-filter-grid">
            <el-form-item label="客户">
              <el-select v-model="filters.customerId" filterable remote clearable reserve-keyword placeholder="搜索真实启用客户" :remote-method="searchCustomers" :loading="customerLoading" style="width:100%" @change="applyFilters">
                <el-option v-for="c in customers" :key="c.id" :label="`${c.name} (${c.code})`" :value="c.id" />
              </el-select>
            </el-form-item>
            <el-form-item label="订单日期从">
              <el-date-picker v-model="filters.orderDateFrom" type="date" value-format="YYYY-MM-DD" placeholder="开始日期" style="width:100%" />
            </el-form-item>
            <el-form-item label="订单日期至">
              <el-date-picker v-model="filters.orderDateTo" type="date" value-format="YYYY-MM-DD" placeholder="结束日期" style="width:100%" />
            </el-form-item>
          </div>
          <div style="text-align:right">
            <el-button @click="resetFilters">重置</el-button>
            <el-button type="primary" @click="applyFilters">应用筛选</el-button>
          </div>
        </el-form>
      </div>
    </el-collapse-transition>

    <transition name="bulk-slide">
      <div v-if="selectedRows.length > 0" class="gs-bulk-bar">
        <span>已选中 <b>{{ selectedRows.length }}</b> 条</span>
        <el-button size="small" @click="batchUnsupported">批量提交</el-button>
        <el-button size="small" @click="batchUnsupported">批量审核</el-button>
        <el-button size="small" @click="exportUnsupported">批量导出</el-button>
        <el-button size="small" text @click="clearSelection">取消</el-button>
      </div>
    </transition>

    <div class="gs-list-wrap">
      <el-table ref="tableRef" v-loading="store.loading" :data="visibleRows" border stripe height="100%" size="small" row-key="id" @row-dblclick="openDetail" @selection-change="onSelectionChange">
        <el-table-column type="selection" :width="COL.selection" fixed="left" />
        <el-table-column type="index" label="#" :width="COL.index" fixed="left" :index="indexMethod" />
        <template v-for="col in visibleColumns" :key="col.prop">
          <el-table-column :prop="col.prop" :label="col.label" :width="col.width" :fixed="col.fixed" show-overflow-tooltip>
            <template #default="{ row }"><component :is="renderCell(col.prop, row)" /></template>
          </el-table-column>
        </template>
        <el-table-column label="操作" :width="COL.documentActions" fixed="right" align="center">
          <template #default="{ row }">
            <div class="gs-row-actions">
              <el-button text size="small" type="primary" @click="openDetail(row)">查看</el-button>
              <el-button text size="small" type="primary" :disabled="row.status !== 1" @click="openEdit(row)">编辑</el-button>
            </div>
          </template>
        </el-table-column>
        <template #empty>
          <div class="empty-state">
            <el-icon :size="40"><Document /></el-icon>
            <p>{{ loadFailed ? '销售订单加载失败' : '暂无真实销售订单数据' }}</p>
            <el-button v-if="!loadFailed" type="primary" @click="onNew"><el-icon><Plus /></el-icon>新建第一张销售订单</el-button>
          </div>
        </template>
      </el-table>
    </div>

    <div class="gs-list-pagination">
      <span class="gs-summary-text">共 <b>{{ store.totalCount }}</b> 条 · 已选 <b>{{ selectedRows.length }}</b> 条</span>
      <el-pagination v-model:current-page="store.page" v-model:page-size="store.pageSize" :total="store.totalCount" :page-sizes="[20, 50, 100]" layout="sizes, prev, pager, next, jumper" background @change="load" />
    </div>

    <el-drawer v-model="columnDrawer" title="列设置 · 当前用户" size="360px">
      <div class="gs-col-settings">
        <p class="gs-field-hint">勾选显示列，调整后立即影响当前真实列表视图。</p>
        <div class="gs-col-list">
          <div v-for="col in localColumns" :key="col.prop" class="gs-col-item">
            <el-checkbox v-model="col.visible">{{ col.label }}</el-checkbox>
            <span class="col-prop">{{ col.prop }}</span>
          </div>
        </div>
      </div>
      <template #footer>
        <el-button @click="resetColumns">恢复默认</el-button>
        <el-button type="primary" @click="columnDrawer = false">完成</el-button>
      </template>
    </el-drawer>

    <el-dialog v-model="savedViewDialog" title="我的视图" width="460px">
      <p class="gs-field-hint">当前保留原视图入口；视图持久化将在独立配置模块接入。</p>
      <div class="saved-view-list">
        <div class="sv-item is-default"><el-icon><Star /></el-icon><span>默认视图</span><span class="badge">当前默认</span></div>
      </div>
      <template #footer><el-button type="primary" @click="savedViewDialog = false">知道了</el-button></template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, h, onMounted, reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import { ElMessage, ElTag } from 'element-plus';
import { Search as SearchIcon, Plus, Filter, Setting, Download, Star, Document, Refresh } from '@element-plus/icons-vue';
import { listSalesOrderCustomers } from '../../api/sales-order-context';
import { listAllWarehousesActiveOnly } from '../../api/mdm/warehouse';
import { TABLE_COLUMN_PRESETS as COL } from '../../design-system/tableColumns';
import { useTabsStore } from '../../stores/tabs';
import { useSalesOrderStore } from '../../stores/sales-order';
import type { BusinessPartner, Warehouse } from '../../types/mdm';
import type { ListColumnConfig, SalesOrderListItemDto, SalesOrderStatus } from '../../types/sales-order';
import { SALES_STATUS_LABEL, SALES_STATUS_TAG } from '../../types/sales-order';

defineOptions({ name: 'SalesOrderList' });

const router = useRouter();
const tabs = useTabsStore();
const store = useSalesOrderStore();
const tableRef = ref();
const advancedOpen = ref(false);
const columnDrawer = ref(false);
const savedViewDialog = ref(false);
const selectedRows = ref<SalesOrderListItemDto[]>([]);
const customers = ref<BusinessPartner[]>([]);
const customerLoading = ref(false);
const loadFailed = ref(false);
const filters = reactive<{ keyword: string; status?: SalesOrderStatus; customerId?: string; orderDateFrom?: string; orderDateTo?: string }>({ keyword: '' });

const defaultColumns: ListColumnConfig[] = [
  { prop: 'orderNo', label: '单据号', width: COL.documentNo, visible: true, fixed: 'left' },
  { prop: 'customerNameSnapshot', label: '客户', width: COL.partnerName, visible: true },
  { prop: 'orderDate', label: '订单日期', width: COL.date, visible: true },
  { prop: 'requestedDeliveryDate', label: '要求交期', width: COL.date, visible: true },
  { prop: 'status', label: '单据状态', width: COL.status, visible: true },
  { prop: 'currencyCode', label: '币种', width: COL.currency, visible: true },
  // B3.6-A additions. The list DTO carries warehouseId (FK) and
  // confirmedAt (audit); warehouse code/name display is a follow-up
  // (B3.6-C) and would require extending the list projection.
  { prop: 'warehouseId', label: '仓库', width: COL.code, visible: true },
  { prop: 'confirmedAt', label: '确认时间', width: COL.datetime, visible: true },
  { prop: 'totalNetAmount', label: '不含税金额', width: COL.money, visible: true },
  { prop: 'totalTaxAmount', label: '税额', width: COL.money, visible: true },
  { prop: 'totalAmount', label: '价税合计', width: COL.money, visible: true },
  { prop: 'modifiedAt', label: '更新时间', width: COL.datetime, visible: true },
];
const localColumns = ref<ListColumnConfig[]>(defaultColumns.map(c => ({ ...c })));
const visibleColumns = computed(() => localColumns.value.filter(c => c.visible));
const activeFilterCount = computed(() => [filters.keyword, filters.status, filters.customerId, filters.orderDateFrom, filters.orderDateTo].filter(v => v !== undefined && v !== '').length);
const visibleRows = computed(() => filters.customerId ? store.items.filter(r => r.customerId === filters.customerId) : store.items);

function money(v: number, decimals = 2) {
  return new Intl.NumberFormat('zh-CN', { minimumFractionDigits: decimals, maximumFractionDigits: decimals }).format(v || 0);
}
async function load() {
  loadFailed.value = false;
  try {
    await store.fetchList({ keyword: filters.keyword || undefined, status: filters.status, orderDateFrom: filters.orderDateFrom, orderDateTo: filters.orderDateTo });
  } catch (err) {
    loadFailed.value = true;
    ElMessage.error((err as any)?.detail || (err as any)?.title || '销售订单加载失败');
  }
}
async function searchCustomers(keyword: string) {
  customerLoading.value = true;
  try {
    // G3-R2A: switch from /api/v1/mdm/business-partners (which
    // requires MdmPolicies.BusinessPartnerRead that SALES_OPERATOR
    // does not have) to the SalesOrder context facade endpoint at
    // /api/v1/sales/orders/context/customers (which requires only
    // SalesOrderRead — preserves the G3-R1C boundary contract).
    const result = await listSalesOrderCustomers({ keyword, page: 1, pageSize: 50 });
    customers.value = result.items as unknown as BusinessPartner[];
  } catch (err) {
    ElMessage.error((err as any)?.detail || (err as any)?.title || '客户搜索失败');
  } finally {
    customerLoading.value = false;
  }
}
function applyFilters() { store.page = 1; load(); }
function resetFilters() {
  filters.keyword = '';
  filters.status = undefined;
  filters.customerId = undefined;
  filters.orderDateFrom = undefined;
  filters.orderDateTo = undefined;
  applyFilters();
}
function resetColumns() { localColumns.value = defaultColumns.map(c => ({ ...c })); }
function onSelectionChange(rows: SalesOrderListItemDto[]) { selectedRows.value = rows; }
function clearSelection() { tableRef.value?.clearSelection(); }
function exportUnsupported() { ElMessage.info('导出将接入正式报表/导出服务，当前不生成本地假文件'); }
function batchUnsupported() { ElMessage.info('批量动作将接入正式工作流，本阶段不伪造成功'); }
function openDetail(row: SalesOrderListItemDto) { tabs.openDetail(row.id, row.orderNo); router.push(`/sales-orders/${row.id}`).catch(() => {}); }
function openEdit(row: SalesOrderListItemDto) { tabs.openEdit(row.id, row.orderNo, 'edit'); router.push(`/sales-orders/${row.id}/edit`).catch(() => {}); }
function onNew() { tabs.openEdit('new', '新建销售订单', 'create'); router.push('/sales-orders/new/edit').catch(() => {}); }
function indexMethod(idx: number) { return (store.page - 1) * store.pageSize + idx + 1; }
function renderCell(prop: string, row: SalesOrderListItemDto) {
  switch (prop) {
    case 'orderNo': return h('a', { class: 'cell-link', onClick: (e: Event) => { e.stopPropagation(); openDetail(row); } }, row.orderNo);
    case 'customerNameSnapshot': return h('span', { title: row.customerCodeSnapshot }, row.customerNameSnapshot);
    case 'status': return h(ElTag, { type: SALES_STATUS_TAG[row.status], effect: 'light', size: 'small' }, () => SALES_STATUS_LABEL[row.status]);
    case 'totalNetAmount':
    case 'totalTaxAmount':
    case 'totalAmount': return h('span', { class: 'gs-num-cell' }, money((row as any)[prop]));
    case 'modifiedAt': return h('span', row.modifiedAt || row.createdAt);
    // B3.6-A additions.
    // B3.6-C UI Integration — list column shows warehouse
    // code/name via a client-side join against the active-warehouse
    // map. The V1 list DTO carries only warehouseId; the projection
    // is built once on mount and refreshed on failure. Falls back
    // to the raw id when the warehouse is not in the map (e.g.
    // inactive warehouse or race before load completes).
    case 'warehouseId': {
      const id = row.warehouseId;
      if (!id) return h('span', { class: 'gs-num-cell' }, '—');
      const w = warehouseMap.value.get(String(id));
      if (w) {
        return h('span', w.name ? `${w.code} · ${w.name}` : w.code);
      }
      return h('span', { class: 'gs-num-cell' }, String(id));
    }
    case 'confirmedAt': return h('span', row.confirmedAt || '—');
    default: return h('span', String((row as any)[prop] ?? ''));
  }
}
// B3.6-C UI Integration — client-side warehouse projection map.
const warehouseMap = ref<Map<string, Warehouse>>(new Map());
async function loadWarehouseMap() {
  try {
    const list = await listAllWarehousesActiveOnly();
    const m = new Map<string, Warehouse>();
    for (const w of list) m.set(String(w.id), w);
    warehouseMap.value = m;
  } catch {
    // keep previous map on failure; column will fall back to raw id
  }
}

onMounted(() => {
  tabs.setActive('list-sales-order');
  searchCustomers('');
  load();
  void loadWarehouseMap();
});
</script>

<style scoped>
.so-list { display: flex; flex-direction: column; height: 100%; background: var(--bg-container); }
.so-search {
  width: var(--toolbar-search-width);
  max-width: 100%;
}
.so-status-filter {
  width: var(--col-code);
}
.gs-list-wrap { flex: 1; min-height: 0; overflow: hidden; padding: 0; margin: var(--layout-section-gap) var(--layout-section-gap) 0; }
.empty-state { padding: 60px 0; text-align: center; color: var(--text-muted); }
.empty-state p { margin: 12px 0; font-size: 14px; }
.col-prop { color: var(--text-muted); font-size: 11px; margin-left: auto; font-family: var(--erp-mono-font); }
.saved-view-list { margin-top: 8px; }
.sv-item { display: flex; align-items: center; gap: var(--gap); padding: 10px 8px; border: 1px solid var(--border-default); border-radius: var(--radius-sm); margin-bottom: 6px; font-size: var(--erp-font-size); }
.sv-item.is-default { background: var(--warning-bg); border-color: var(--warning-default); }
.sv-item .badge { background: var(--warning-bg); color: var(--warning-hover); padding: 1px 8px; border-radius: 8px; font-size: 11px; margin-left: auto; }
:deep(.cell-link) { color: var(--primary-hover); text-decoration: none; cursor: pointer; font-weight: 500; }
:deep(.cell-link:hover) { text-decoration: underline; }
:deep(.gs-num-cell) { font-family: var(--erp-mono-font); display: inline-block; width: 100%; text-align: right; }
.bulk-slide-enter-active, .bulk-slide-leave-active { transition: all .2s; }
.bulk-slide-enter-from, .bulk-slide-leave-to { transform: translateY(-100%); opacity: 0; }
</style>
