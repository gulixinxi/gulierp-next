<template>
  <!-- LookupDialog — PopWin-style helper (DEC-UX-001: PopWin 仅用于 Lookup / Quick View / Small Form)
       Single-select (default) + Multi-select (multiSelect=true) for Item lookup -->
  <el-dialog
    v-model="visible"
    :title="title"
    :width="multiSelect ? '860px' : '760px'"
    append-to-body
    destroy-on-close
    :close-on-click-modal="false"
    class="lookup-dialog"
    @keydown.esc="visible = false"
  >
    <div class="lookup-toolbar">
      <el-input
        v-model="keyword"
        placeholder="输入编码或名称搜索(支持模糊匹配)"
        clearable
        :prefix-icon="Search"
        size="default"
        style="width: 320px"
      />
      <el-checkbox v-if="entityType === 'customer' || entityType === 'item'" v-model="showInactive">包含停用</el-checkbox>
      <el-button @click="keyword = ''">清空</el-button>
      <span v-if="multiSelect && selectedRows.length" class="multi-summary">
        已选 <b>{{ selectedRows.length }}</b> 项
      </span>
    </div>
    <el-table
      ref="tableRef"
      :data="filtered"
      highlight-current-row
      :row-class-name="rowClass"
      height="380"
      border
      size="small"
      @row-click="onRowClick"
      @row-dblclick="onRowDblClick"
      @current-change="onCurrentChange"
      @selection-change="onSelectionChange"
    >
      <!-- Single-select: click row to select (highlight-current-row shows visual) -->
      <el-table-column v-if="!multiSelect" label="" width="48" align="center">
        <template #default="{ row }">
          <el-icon v-if="selectedId === row.id" class="selected-icon"><CircleCheckFilled /></el-icon>
          <el-icon v-else class="unselected-icon"><ChatDotRound /></el-icon>
        </template>
      </el-table-column>
      <!-- Multi-select: native el-table selection column -->
      <el-table-column v-if="multiSelect" type="selection" width="48" :selectable="(r: any) => r.isActive !== false" />
      <el-table-column
        v-for="col in columns"
        :key="col.prop"
        :prop="col.prop"
        :label="col.label"
        :width="col.width"
        :formatter="col.formatter"
      />
    </el-table>
    <div class="lookup-footer">
      <span class="hint">
        <template v-if="multiSelect">提示:勾选多个商品,点"加入订单"一次性生成多行;双击行仅选中单行</template>
        <template v-else>提示:单击行选中,双击行直接确定;Esc 关闭</template>
      </span>
      <div>
        <el-button @click="visible = false">取消</el-button>
        <el-button v-if="multiSelect" type="primary" :disabled="selectedRows.length === 0" @click="onMultiConfirm">
          加入订单({{ selectedRows.length }})
        </el-button>
        <el-button v-else type="primary" :disabled="!selectedId" @click="onConfirm">确定</el-button>
      </div>
    </div>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { CircleCheckFilled, ChatDotRound, Search } from '@element-plus/icons-vue';
import {
  customers, contacts, employees, warehouses, items,
  paymentTerms, currencies, taxRates, uoms, locations
} from '../mock/sales-order';

export type LookupEntity =
  | 'customer' | 'contact' | 'employee' | 'warehouse' | 'location'
  | 'item' | 'uom' | 'paymentTerm' | 'currency' | 'taxRate';

const props = defineProps<{
  modelValue: boolean;
  entityType: LookupEntity;
  title?: string;
  filter?: Record<string, any>;
  excludeIds?: string[];
  multiSelect?: boolean;
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', v: boolean): void;
  (e: 'confirm', payload: { id: string; label: string; raw: any }): void;
  (e: 'multi-confirm', payload: { rows: any[] }): void;
}>();

const visible = computed({
  get: () => props.modelValue,
  set: v => emit('update:modelValue', v)
});

const tableRef = ref();
const keyword = ref('');
const showInactive = ref(false);

// Single-select state
const selectedId = ref<string>('');
const selected = ref<any>(null);

// Multi-select state
const selectedRows = ref<any[]>([]);

interface Col { prop: string; label: string; width?: number; formatter?: (r: any, c: any, v: any) => string; }

const titleMap: Record<LookupEntity, string> = {
  customer: '客户查找',
  contact: '联系人查找',
  employee: '员工查找',
  warehouse: '仓库查找',
  location: '库位查找',
  item: '商品查找(支持多选)',
  uom: '单位查找',
  paymentTerm: '付款条件查找',
  currency: '币种查找',
  taxRate: '税率查找'
};

const title = computed(() => props.title || titleMap[props.entityType]);

const sourceData = computed<any[]>(() => {
  switch (props.entityType) {
    case 'customer': return customers;
    case 'contact':
      return contacts.filter(c => !props.filter?.customerId || c.customerId === props.filter.customerId);
    case 'employee': return employees.filter(e => e.role === (props.filter?.role || 'Sales'));
    case 'warehouse': return warehouses;
    case 'location':
      return locations.filter(l => !props.filter?.warehouseId || l.warehouseId === props.filter.warehouseId);
    case 'item': return items;
    case 'uom': return uoms;
    case 'paymentTerm': return paymentTerms;
    case 'currency': return currencies;
    case 'taxRate': return taxRates;
  }
});

const columns = computed<Col[]>(() => {
  switch (props.entityType) {
    case 'customer':
      return [
        { prop: 'code', label: '编码', width: 90 },
        { prop: 'name', label: '客户名称', width: 280 },
        { prop: 'defaultPriceMode', label: '默认价格模式', width: 130, formatter: (_r, _c, v) => v === 'TaxInclusive' ? '含税' : '未税' },
        { prop: 'defaultPaymentTermCode', label: '付款条件', width: 100 },
        { prop: 'isActive', label: '状态', width: 80, formatter: (_r, _c, v) => v ? '启用' : '停用' }
      ];
    case 'contact':
      return [
        { prop: 'name', label: '姓名', width: 120 },
        { prop: 'phone', label: '电话', width: 140 },
        { prop: 'email', label: '邮箱' }
      ];
    case 'employee':
      return [
        { prop: 'code', label: '工号', width: 90 },
        { prop: 'name', label: '姓名', width: 120 },
        { prop: 'orgName', label: '部门', width: 160 },
        { prop: 'role', label: '角色', width: 120, formatter: (_r, _c, v) => v === 'Sales' ? '销售员' : v === 'SalesManager' ? '销售经理' : v }
      ];
    case 'warehouse':
      return [
        { prop: 'code', label: '编码', width: 100 },
        { prop: 'name', label: '仓库名称' },
        { prop: 'locationMandatory', label: '库位必填', width: 100, formatter: (_r, _c, v) => v ? '是' : '否' }
      ];
    case 'location':
      return [
        { prop: 'code', label: '编码', width: 100 },
        { prop: 'name', label: '库位名称' }
      ];
    case 'item':
      return [
        { prop: 'code', label: '编码', width: 90 },
        { prop: 'name', label: '商品名称', width: 220 },
        { prop: 'spec', label: '规格型号' },
        { prop: 'uomName', label: '单位', width: 70 },
        { prop: 'defaultSalesPriceExclTax', label: '未税单价', width: 110, formatter: (_r, _c, v) => '¥' + Number(v).toFixed(2) },
        { prop: 'defaultSalesPriceInclTax', label: '含税单价', width: 110, formatter: (_r, _c, v) => '¥' + Number(v).toFixed(2) },
        { prop: 'isActive', label: '状态', width: 80, formatter: (_r, _c, v) => v ? '启用' : '停用' }
      ];
    case 'uom':
      return [
        { prop: 'code', label: '编码', width: 100 },
        { prop: 'name', label: '单位名称' }
      ];
    case 'paymentTerm':
      return [
        { prop: 'code', label: '编码', width: 100 },
        { prop: 'name', label: '名称' },
        { prop: 'days', label: '账期天数', width: 100 }
      ];
    case 'currency':
      return [
        { prop: 'code', label: '编码', width: 100 },
        { prop: 'name', label: '名称' },
        { prop: 'symbol', label: '符号', width: 80 },
        { prop: 'rate', label: '汇率', width: 100 }
      ];
    case 'taxRate':
      return [
        { prop: 'code', label: '编码', width: 100 },
        { prop: 'name', label: '名称' },
        { prop: 'rate', label: '税率', width: 100, formatter: (_r, _c, v) => (v * 100).toFixed(2) + '%' }
      ];
  }
});

const filtered = computed(() => {
  let rows = sourceData.value;
  if ((props.entityType === 'customer' || props.entityType === 'item') && !showInactive.value) {
    rows = rows.filter(r => r.isActive);
  }
  if (props.excludeIds?.length) {
    rows = rows.filter(r => !props.excludeIds!.includes(r.id));
  }
  const kw = keyword.value.trim().toLowerCase();
  if (kw) {
    rows = rows.filter(r => {
      const code = (r.code || '').toLowerCase();
      const name = (r.name || '').toLowerCase();
      const spec = (r.spec || '').toLowerCase();
      return code.includes(kw) || name.includes(kw) || spec.includes(kw);
    });
  }
  return rows;
});

// Reset state whenever dialog opens
watch(visible, v => {
  if (v) {
    keyword.value = '';
    showInactive.value = false;
    selectedId.value = '';
    selected.value = null;
    selectedRows.value = [];
    // Clear el-table selection state for multi-select
    if (tableRef.value && props.multiSelect) {
      tableRef.value.clearSelection();
    }
  }
});

function rowClass({ row }: { row: any }) {
  if (props.entityType === 'customer' || props.entityType === 'item') {
    return row.isActive ? '' : 'inactive-row';
  }
  return '';
}

function onRowClick(row: any) {
  if (props.multiSelect) {
    // Toggle selection in multi-select mode
    tableRef.value?.toggleRowSelection(row);
  } else {
    // Single-select: set selected via click
    selected.value = row;
    selectedId.value = row.id;
  }
}

function onRowDblClick(row: any) {
  if (props.multiSelect) {
    // In multi-select mode, double-click just selects (no auto-confirm)
    tableRef.value?.toggleRowSelection(row);
  } else {
    onConfirm(row);
  }
}

function onCurrentChange(row: any) {
  // current-change fires for highlight-current-row navigation
  // We do NOT use it to set selection in single mode (we use row-click)
}

function onSelectionChange(rows: any[]) {
  selectedRows.value = rows;
}

function onConfirm(rowOverride?: any) {
  // Vue's @click="onConfirm" passes a MouseEvent as the first arg — ignore it.
  // Only treat rowOverride as a real row if it's a plain data object (not an Event).
  if (rowOverride instanceof Event) {
    rowOverride = undefined;
  }
  let row = rowOverride;
  // Fallback 1: look up by selectedId (most reliable — survives re-renders)
  if (!row && selectedId.value) {
    row = sourceData.value.find(r => r.id === selectedId.value);
  }
  // Fallback 2: use selected.value if still set
  if (!row) {
    row = selected.value;
  }
  if (!row) return;
  const label = row.name || `${row.code} ${row.name || ''}`.trim() || row.code;
  emit('confirm', { id: row.id, label, raw: row });
  visible.value = false;
}

function onMultiConfirm() {
  if (selectedRows.value.length === 0) return;
  emit('multi-confirm', { rows: [...selectedRows.value] });
  visible.value = false;
}
</script>

<style scoped>
.lookup-toolbar {
  display: flex;
  gap: 12px;
  align-items: center;
  margin-bottom: 12px;
  flex-wrap: wrap;
}
.lookup-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: 12px;
}
.lookup-footer .hint {
  color: #909399;
  font-size: 12px;
}
.multi-summary {
  margin-left: auto;
  color: #16a34a;
  font-size: 13px;
  background: #f0fdf4;
  padding: 3px 10px;
  border-radius: 4px;
}
.selected-icon {
  color: #16a34a;
  font-size: 18px;
}
.unselected-icon {
  color: #d1d5db;
  font-size: 18px;
}
:deep(.inactive-row) {
  color: #c0c4cc;
}
:deep(.el-table__row) {
  cursor: pointer;
}
:deep(.el-table__row:hover) {
  background: #eff6ff !important;
}
</style>
