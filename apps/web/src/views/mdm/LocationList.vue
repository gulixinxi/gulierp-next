<template>
  <!-- LocationList — Location master data per TRAE_MDM_002_API_HANDOFF.md §4/§6/§8.
       Warehouse dropdown is populated from the real Warehouse API (no static
       array). warehouseId may arrive via route query (?warehouseId=N) when the
       operator enters from the Warehouse page's "查看库位" action.
       Cross-scope: mdm_location_parent_warehouse_cross_scope → "所选仓库不属于
       当前公司或无权访问". 404 → "数据不存在或无权访问". -->
  <div class="mdm-list">
    <MdmListToolbar
      v-model:search="searchKeyword"
      search-placeholder="搜索库位代码 / 名称"
      create-label="新建库位"
      @search="applyFilters"
      @create="openCreate"
    >
      <template #filters>
        <el-select
          v-model="filterWarehouseId"
          placeholder="所属仓库"
          clearable
          filterable
          style="width: 180px"
          @change="applyFilters"
        >
          <el-option
            v-for="w in warehouses"
            :key="w.id"
            :label="`${w.code} · ${w.name}`"
            :value="w.id"
            :disabled="w.status !== 'active'"
          />
        </el-select>
        <el-select v-model="filterType" placeholder="库位类型" clearable style="width: 120px" @change="applyFilters">
          <el-option v-for="opt in LOCATION_TYPE_OPTIONS" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
        <el-select v-model="filterStatus" placeholder="状态" clearable style="width: 100px" @change="applyFilters">
          <el-option v-for="opt in STATUS_OPTIONS" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
      </template>
      <template #actions>
        <el-button text @click="exportData">
          <el-icon><Download /></el-icon>
          导出
        </el-button>
      </template>
    </MdmListToolbar>

    <div class="gs-list-wrap">
      <el-table
        v-loading="loading"
        :data="pagedData"
        border
        stripe
        height="100%"
        size="small"
        row-key="id"
        @row-dblclick="openDetail"
        :header-cell-style="{ padding: '0 8px' }"
        :cell-style="{ padding: '0 8px' }"
      >
        <el-table-column type="index" label="#" :width="COL.index" fixed="left" />
        <el-table-column prop="code" label="库位代码" :width="COL.code" sortable show-overflow-tooltip>
          <template #default="{ row }">
            <span class="mdm-code">{{ row.code }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="name" label="库位名称" min-width="110" show-overflow-tooltip sortable />
        <el-table-column prop="warehouseName" label="所属仓库" min-width="140" show-overflow-tooltip>
          <template #default="{ row }">
            {{ row.warehouseCode ? `${row.warehouseCode} · ${row.warehouseName || ''}` : (row.warehouseName || '—') }}
          </template>
        </el-table-column>
        <el-table-column prop="type" label="类型" :width="COL.type" align="center">
          <template #default="{ row }">{{ typeLabel(row.type) }}</template>
        </el-table-column>
        <el-table-column prop="aisle" label="通道" width="60" align="center">
          <template #default="{ row }">{{ row.aisle || '—' }}</template>
        </el-table-column>
        <el-table-column prop="bay" label="货位" width="60" align="center">
          <template #default="{ row }">{{ row.bay || '—' }}</template>
        </el-table-column>
        <el-table-column prop="shelf" label="层" width="60" align="center">
          <template #default="{ row }">{{ row.shelf || '—' }}</template>
        </el-table-column>
        <el-table-column prop="status" label="状态" :width="COL.status" align="center">
          <template #default="{ row }">
            <MdmStatusBadge :status="row.status" />
          </template>
        </el-table-column>
        <el-table-column prop="updatedAt" label="更新时间" :width="COL.datetime" sortable>
          <template #default="{ row }">{{ formatDate(row.updatedAt) }}</template>
        </el-table-column>
        <el-table-column label="操作" :width="COL.actions" fixed="right" align="center">
          <template #default="{ row }">
            <MdmTableRowActions
              :status="row.status"
              @edit="openEdit(row)"
              @deactivate="confirmDeactivate(row)"
              @activate="confirmActivate(row)"
            />
          </template>
        </el-table-column>
        <template #empty>
          <MdmEmptyState
            message="暂无库位数据"
            create-label="新建库位"
            @create="openCreate"
          />
        </template>
      </el-table>
    </div>

    <MdmPagination
      v-model:current-page="page.current"
      v-model:page-size="page.size"
      :total="total"
    />
    <div v-if="error && !loading" class="mdm-error-banner">
      <el-alert :title="error" type="error" show-icon :closable="false" />
      <el-button type="primary" link style="margin-left:12px" @click="fetchList">重新加载</el-button>
    </div>

    <!-- Create/Edit Form Drawer -->
    <MdmFormDrawer
      v-model="formDrawerVisible"
      v-model:model="formData"
      :title="editingId ? '编辑库位' : '新建库位'"
      :rules="formRules"
      :loading="submitting"
      :submit-label="editingId ? '保存修改' : '创建'"
      @submit="handleSubmit"
    >
      <el-form-item label="所属仓库" prop="warehouseId">
        <el-select
          v-model="formData.warehouseId"
          placeholder="选择所属仓库"
          filterable
          style="width: 100%"
          @change="onFormWarehouseChange"
        >
          <el-option
            v-for="w in warehouses"
            :key="w.id"
            :label="`${w.code} · ${w.name}`"
            :value="w.id"
            :disabled="w.status !== 'active'"
          />
        </el-select>
      </el-form-item>
      <el-form-item label="库位代码" prop="code">
        <el-input v-model="formData.code" placeholder="如 A-1-1" :disabled="!!editingId" maxlength="40" />
      </el-form-item>
      <el-form-item label="库位名称" prop="name">
        <el-input v-model="formData.name" placeholder="库位全称" maxlength="200" />
      </el-form-item>
      <el-form-item label="库位类型" prop="type">
        <el-select v-model="formData.type" style="width: 100%">
          <el-option v-for="opt in LOCATION_TYPE_OPTIONS" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
      </el-form-item>
      <el-form-item label="通道">
        <el-input v-model="formData.aisle" placeholder="如 A" maxlength="20" />
      </el-form-item>
      <el-form-item label="货位">
        <el-input v-model="formData.bay" placeholder="如 1" maxlength="20" />
      </el-form-item>
      <el-form-item label="层">
        <el-input v-model="formData.shelf" placeholder="如 1" maxlength="20" />
      </el-form-item>
      <el-form-item label="状态" prop="status">
        <el-select v-model="formData.status" style="width: 100%">
          <el-option v-for="opt in STATUS_OPTIONS" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
      </el-form-item>
      <el-form-item label="说明">
        <el-input v-model="formData.description" type="textarea" :rows="2" maxlength="200" show-word-limit />
      </el-form-item>
    </MdmFormDrawer>

    <!-- Detail Drawer -->
    <MdmDetailDrawer
      v-model="detailDrawerVisible"
      :title="`库位详情 · ${detailData?.code || ''}`"
      @edit="openEditFromDetail"
    >
      <template #header>
        <div class="mdm-detail-title">
          <MdmStatusBadge :status="detailData?.status || 'active'" />
          <span class="mdm-detail-name">{{ detailData?.name }}</span>
          <el-tag v-if="detailData?.type" size="small" effect="light">{{ typeLabel(detailData.type) }}</el-tag>
        </div>
      </template>
      <el-descriptions :column="2" border size="small">
        <el-descriptions-item label="库位代码">{{ detailData?.code }}</el-descriptions-item>
        <el-descriptions-item label="库位名称">{{ detailData?.name }}</el-descriptions-item>
        <el-descriptions-item label="所属仓库">
          {{ detailData?.warehouseCode ? `${detailData.warehouseCode} · ${detailData.warehouseName || ''}` : (detailData?.warehouseName || '—') }}
        </el-descriptions-item>
        <el-descriptions-item label="库位类型">{{ typeLabel(detailData?.type) }}</el-descriptions-item>
        <el-descriptions-item label="通道">{{ detailData?.aisle || '—' }}</el-descriptions-item>
        <el-descriptions-item label="货位">{{ detailData?.bay || '—' }}</el-descriptions-item>
        <el-descriptions-item label="层">{{ detailData?.shelf || '—' }}</el-descriptions-item>
        <el-descriptions-item label="状态">{{ getStatusLabel(detailData?.status) }}</el-descriptions-item>
        <el-descriptions-item label="说明">{{ detailData?.description || '—' }}</el-descriptions-item>
      </el-descriptions>
      <el-descriptions :column="2" border size="small" style="margin-top: 16px">
        <el-descriptions-item label="创建时间">{{ formatDate(detailData?.createdAt) }}</el-descriptions-item>
        <el-descriptions-item label="更新时间">{{ formatDate(detailData?.updatedAt) }}</el-descriptions-item>
      </el-descriptions>
    </MdmDetailDrawer>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import { Download } from '@element-plus/icons-vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import type { FormRules } from 'element-plus';

import MdmListToolbar from '../../components/mdm/MdmListToolbar.vue';
import MdmStatusBadge from '../../components/mdm/MdmStatusBadge.vue';
import MdmFormDrawer from '../../components/mdm/MdmFormDrawer.vue';
import MdmDetailDrawer from '../../components/mdm/MdmDetailDrawer.vue';
import MdmPagination from '../../components/mdm/MdmPagination.vue';
import MdmEmptyState from '../../components/mdm/MdmEmptyState.vue';
import MdmTableRowActions from '../../components/mdm/MdmTableRowActions.vue';
import { TABLE_COLUMN_PRESETS as COL } from '../../design-system/tableColumns';

import { ApiError } from '../../api/http';
import * as locApi from '../../api/mdm/location';
import * as whApi from '../../api/mdm/warehouse';
import {
  STATUS_OPTIONS,
  LOCATION_TYPE_OPTIONS,
  locTypeUiToInt,
  statusUiToInt,
} from '../../types/mdm';
import type {
  Location,
  LocationForm,
  LocationType,
  Warehouse,
  MasterDataStatus,
} from '../../types/mdm';

const route = useRoute();

// ===== Real API list state =====
const list = ref<Location[]>([]);
const total = ref(0);
const loading = ref(false);
const error = ref<string | null>(null);

const searchKeyword = ref('');
const filterWarehouseId = ref<string | ''>('');
const filterType = ref<LocationType | ''>('');
const filterStatus = ref<MasterDataStatus | ''>('');
const page = reactive({ current: 1, size: 20 });

// Warehouse reference data (populated from real Warehouse API per Handoff §12)
const warehouses = ref<Warehouse[]>([]);

async function fetchWarehouses() {
  try {
    warehouses.value = await whApi.listAllWarehousesActiveOnly();
  } catch (e) {
    if (e instanceof ApiError && e.code === 'authentication_required') throw e;
    // Non-fatal: the dropdown will be empty; the operator can retry via reload.
    warehouses.value = [];
  }
}

async function fetchList() {
  loading.value = true;
  error.value = null;
  try {
    const result = await locApi.listLocations({
      keyword: searchKeyword.value.trim() || undefined,
      warehouseId: filterWarehouseId.value || undefined,
      type: filterType.value ? locTypeUiToInt(filterType.value) : undefined,
      status: filterStatus.value ? statusUiToInt(filterStatus.value) : undefined,
      page: page.current,
      pageSize: page.size,
    });
    // Denormalize warehouse display names onto the page list.
    list.value = locApi.joinLocationWarehouse(result.items, warehouses.value);
    total.value = result.totalCount;
  } catch (e) {
    if (e instanceof ApiError) {
      if (e.code === 'authentication_required') throw e;
      if (e.status === 404) {
        error.value = '数据不存在或无权访问';
      } else {
        const parts = [e.title];
        if (e.detail) parts.push(e.detail);
        if (e.requestId) parts.push(`(RequestId: ${e.requestId})`);
        error.value = parts.join(' — ');
      }
    } else {
      error.value = '加载库位数据失败，请刷新重试';
    }
    list.value = [];
    total.value = 0;
  } finally {
    loading.value = false;
  }
}

onMounted(async () => {
  // Load warehouse references first so the first locations fetch can denormalize.
  await fetchWarehouses();
  // Honor ?warehouseId=ID from route (Warehouse page "查看库位").
  // API-CONTRACT-ID-001: warehouseId is an opaque string — NEVER Number() it.
  const qWh = route.query?.warehouseId;
  if (typeof qWh === 'string' && qWh.length > 0) {
    filterWarehouseId.value = qWh;
  }
  await fetchList();
});

const pagedData = computed(() => list.value);

function applyFilters() {
  page.current = 1;
  fetchList();
}

// ===== Form state =====
const formDrawerVisible = ref(false);
const editingId = ref<string | null>(null);
const editingConcurrency = ref(0);
const submitting = ref(false);
const formData = reactive<LocationForm>(emptyForm());

function emptyForm(): LocationForm {
  return {
    warehouseId: null,
    code: '', name: '', type: 'BIN',
    aisle: '', bay: '', shelf: '',
    status: 'active', description: '',
  };
}

const formRules: FormRules = {
  warehouseId: [{ required: true, message: '请选择所属仓库', trigger: 'change' }],
  code: [{ required: true, message: '请输入库位代码', trigger: 'blur' }],
  name: [{ required: true, message: '请输入库位名称', trigger: 'blur' }],
  type: [{ required: true, message: '请选择库位类型', trigger: 'change' }],
};

function resetForm() {
  Object.assign(formData, emptyForm());
  // Pre-fill warehouseId from the current list filter (UX nicety).
  if (filterWarehouseId.value) formData.warehouseId = filterWarehouseId.value;
}

function openCreate() {
  editingId.value = null;
  editingConcurrency.value = 0;
  resetForm();
  formDrawerVisible.value = true;
}

async function openEdit(row: Location) {
  editingId.value = row.id;
  editingConcurrency.value = row.concurrencyVersion ?? 0;
  try {
    const fresh = await locApi.getLocation(row.id);
    fillForm(fresh);
    editingConcurrency.value = fresh.concurrencyVersion ?? 0;
  } catch (e) {
    fillForm(row);
  }
  formDrawerVisible.value = true;
}

function fillForm(d: Location) {
  Object.assign(formData, {
    warehouseId: d.warehouseId,
    code: d.code, name: d.name, type: d.type,
    aisle: d.aisle || '', bay: d.bay || '', shelf: d.shelf || '',
    status: d.status, description: d.description || '',
  });
}

/**
 * When the form's Warehouse selection changes, refresh the warehouse options
 * (per Handoff §12: "Warehouse change → refresh valid options"). We re-fetch
 * the active warehouse list so a freshly-created or re-activated warehouse
 * becomes selectable without a full page reload.
 */
async function onFormWarehouseChange() {
  await fetchWarehouses();
}

function isConcurrencyConflict(err: unknown): boolean {
  return err instanceof ApiError && err.code === 'mdm_validation_failed'
    && /concurrency|version|并发/i.test(err.detail || '');
}

function formatApiError(e: ApiError): string {
  // Cross-scope: parent warehouse not in current (TenantId, CompanyId).
  if (e.code === 'mdm_location_parent_warehouse_cross_scope') {
    return '所选仓库不属于当前公司或无权访问';
  }
  const parts = [e.title];
  if (e.detail) parts.push(e.detail);
  if (e.requestId) parts.push(`(${e.requestId})`);
  return parts.join(' — ');
}

async function handleSubmit() {
  submitting.value = true;
  try {
    if (editingId.value == null) {
      await locApi.createLocation(formData);
      ElMessage.success('创建库位成功');
    } else {
      await locApi.updateLocation(editingId.value, formData, editingConcurrency.value);
      ElMessage.success('保存修改成功');
    }
    formDrawerVisible.value = false;
    await fetchList();
  } catch (e) {
    if (isConcurrencyConflict(e)) {
      ElMessage.warning('并发冲突：数据已被其他用户修改，请刷新后重试');
    } else if (e instanceof ApiError) {
      ElMessage.error(formatApiError(e));
    } else if (e instanceof Error && e.message) {
      ElMessage.error(e.message);
    } else {
      ElMessage.error('保存失败，请重试');
    }
  } finally {
    submitting.value = false;
  }
}

// ===== Detail state =====
const detailDrawerVisible = ref(false);
const detailData = ref<Location | null>(null);

async function openDetail(row: Location) {
  try {
    const raw = await locApi.getLocation(row.id);
    const [withRefs] = locApi.joinLocationWarehouse([raw], warehouses.value);
    detailData.value = withRefs;
  } catch (e) {
    detailData.value = row;
    if (e instanceof ApiError && e.code !== 'authentication_required') {
      ElMessage.warning('读取最新详情失败，显示列表快照');
    }
  }
  detailDrawerVisible.value = true;
}

function openEditFromDetail() {
  if (detailData.value) {
    openEdit(detailData.value);
    detailDrawerVisible.value = false;
  }
}

// ===== Helpers =====
function formatDate(iso?: string): string {
  if (!iso) return '—';
  const d = new Date(iso);
  return d.toLocaleString('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' });
}
function getStatusLabel(status?: MasterDataStatus): string {
  return STATUS_OPTIONS.find(o => o.value === status)?.label || '—';
}
function typeLabel(t?: LocationType): string {
  return LOCATION_TYPE_OPTIONS.find(o => o.value === t)?.label || '—';
}

// ===== Status change =====
async function confirmDeactivate(row: Location) {
  try {
    await ElMessageBox.confirm(
      `确定停用库位"${row.name}"吗？停用后将不能用于新的业务单据。`,
      '停用确认',
      { confirmButtonText: '停用', cancelButtonText: '取消', type: 'warning' },
    );
  } catch { return; }
  try {
    await locApi.setLocationStatus(row, 'inactive');
    ElMessage.success('已停用');
    await fetchList();
  } catch (e) {
    if (isConcurrencyConflict(e)) ElMessage.warning('并发冲突：数据已被其他用户修改，请刷新后重试');
    else if (e instanceof ApiError) ElMessage.error(formatApiError(e));
    else ElMessage.error('停用失败');
  }
}

async function confirmActivate(row: Location) {
  try {
    await ElMessageBox.confirm(`确定启用库位"${row.name}"吗？`, '启用确认',
      { confirmButtonText: '启用', cancelButtonText: '取消', type: 'info' });
  } catch { return; }
  try {
    await locApi.setLocationStatus(row, 'active');
    ElMessage.success('已启用');
    await fetchList();
  } catch (e) {
    if (isConcurrencyConflict(e)) ElMessage.warning('并发冲突：数据已被其他用户修改，请刷新后重试');
    else if (e instanceof ApiError) ElMessage.error(formatApiError(e));
    else ElMessage.error('启用失败');
  }
}

function exportData() {
  ElMessage.info('导出功能待后端支持');
}
</script>

<style scoped>
/* GULIERP_PAGE_THEME_AUDIT_001 / Phase 1 (2026-08-23).
   .mdm-list, .mdm-code, .mdm-detail-title, .mdm-detail-name,
   .mdm-error-banner are shared (see design-system/components/mdm-page.css).
   This page has no page-specific accents. */
</style>
