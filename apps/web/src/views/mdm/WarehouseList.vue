<template>
  <!-- WarehouseList — Warehouse master data per TRAE_MDM_002_API_HANDOFF.md §3.
       Current Company context comes from the auth cookie scope (no client-side
       companyId forging). plantId is an opaque reference (V2+ Production);
       shown in detail only. No Location Count is fabricated (§13).
       Clear entry to Location: toolbar "库位管理" + per-row "查看库位". -->
  <div class="mdm-list">
    <MdmListToolbar
      v-model:search="searchKeyword"
      search-placeholder="搜索仓库代码 / 名称"
      create-label="新建仓库"
      @search="applyFilters"
      @create="openCreate"
    >
      <template #filters>
        <el-select v-model="filterType" placeholder="仓库类型" clearable style="width: 120px" @change="applyFilters">
          <el-option v-for="opt in WAREHOUSE_TYPE_OPTIONS" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
        <el-select v-model="filterStatus" placeholder="状态" clearable style="width: 100px" @change="applyFilters">
          <el-option v-for="opt in STATUS_OPTIONS" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
      </template>
      <template #actions>
        <el-button text @click="goAllLocations">
          <el-icon><Grid /></el-icon>
          库位管理
        </el-button>
        <el-button text @click="exportData">
          <el-icon><Download /></el-icon>
          导出
        </el-button>
      </template>
    </MdmListToolbar>

    <!-- Current company context display (read-only; from auth store) -->
    <div class="mdm-context-bar">
      <el-icon><OfficeBuilding /></el-icon>
      <span>当前公司：{{ auth.companyName || ('公司 ' + auth.companyId) || '—' }}</span>
      <span class="mdm-context-hint">（仓库范围由后端按当前公司上下文过滤）</span>
    </div>

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
        <el-table-column type="index" label="#" width="50" fixed="left" />
        <el-table-column prop="code" label="仓库代码" width="130" sortable show-overflow-tooltip>
          <template #default="{ row }">
            <span class="mdm-code">{{ row.code }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="name" label="仓库名称" min-width="180" show-overflow-tooltip sortable />
        <el-table-column prop="type" label="类型" width="100" align="center">
          <template #default="{ row }">{{ typeLabel(row.type) }}</template>
        </el-table-column>
        <el-table-column prop="city" label="所在城市" width="120" show-overflow-tooltip>
          <template #default="{ row }">{{ row.city || '—' }}</template>
        </el-table-column>
        <el-table-column prop="countryCode" label="国家" width="80" align="center">
          <template #default="{ row }">{{ row.countryCode || '—' }}</template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="80" align="center">
          <template #default="{ row }">
            <MdmStatusBadge :status="row.status" />
          </template>
        </el-table-column>
        <el-table-column prop="updatedAt" label="更新时间" width="160" sortable>
          <template #default="{ row }">{{ formatDate(row.updatedAt) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="160" fixed="right" align="center">
          <template #default="{ row }">
            <el-button text size="small" type="primary" @click="goLocations(row)">查看库位</el-button>
            <span class="mdm-action-sep">|</span>
            <el-button text size="small" type="primary" @click="openEdit(row)">编辑</el-button>
          </template>
        </el-table-column>
        <template #empty>
          <MdmEmptyState
            message="暂无仓库数据"
            create-label="新建仓库"
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
      :title="editingId ? '编辑仓库' : '新建仓库'"
      :rules="formRules"
      :loading="submitting"
      :submit-label="editingId ? '保存修改' : '创建'"
      @submit="handleSubmit"
    >
      <el-form-item label="仓库代码" prop="code">
        <el-input v-model="formData.code" placeholder="如 WH-001" :disabled="!!editingId" maxlength="40" />
      </el-form-item>
      <el-form-item label="仓库名称" prop="name">
        <el-input v-model="formData.name" placeholder="仓库全称" maxlength="200" />
      </el-form-item>
      <el-form-item label="仓库类型" prop="type">
        <el-select v-model="formData.type" style="width: 100%">
          <el-option v-for="opt in WAREHOUSE_TYPE_OPTIONS" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
      </el-form-item>
      <el-form-item label="Plant ID">
        <el-input v-model="formData.plantId" placeholder="选填（生产模块引用，V2+）" maxlength="20" />
      </el-form-item>
      <el-form-item label="地址">
        <el-input v-model="formData.addressLine1" placeholder="地址行 1" maxlength="200" />
      </el-form-item>
      <el-form-item label=" ">
        <el-input v-model="formData.addressLine2" placeholder="地址行 2（选填）" maxlength="200" />
      </el-form-item>
      <el-form-item label="城市">
        <el-input v-model="formData.city" maxlength="100" />
      </el-form-item>
      <el-form-item label="省/州">
        <el-input v-model="formData.region" maxlength="100" />
      </el-form-item>
      <el-form-item label="邮编">
        <el-input v-model="formData.postalCode" maxlength="20" />
      </el-form-item>
      <el-form-item label="国家代码" prop="countryCode">
        <el-input v-model="formData.countryCode" placeholder="如 CN, US（2 位）" maxlength="2" />
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
      :title="`仓库详情 · ${detailData?.code || ''}`"
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
        <el-descriptions-item label="仓库代码">{{ detailData?.code }}</el-descriptions-item>
        <el-descriptions-item label="仓库名称">{{ detailData?.name }}</el-descriptions-item>
        <el-descriptions-item label="仓库类型">{{ typeLabel(detailData?.type) }}</el-descriptions-item>
        <el-descriptions-item label="Plant ID">{{ detailData?.plantId ?? '—' }}</el-descriptions-item>
        <el-descriptions-item label="地址">{{ formatAddress(detailData) || '—' }}</el-descriptions-item>
        <el-descriptions-item label="国家代码">{{ detailData?.countryCode || '—' }}</el-descriptions-item>
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
import { useRouter } from 'vue-router';
import { Download, Grid, OfficeBuilding } from '@element-plus/icons-vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import type { FormRules } from 'element-plus';

import MdmListToolbar from '../../components/mdm/MdmListToolbar.vue';
import MdmStatusBadge from '../../components/mdm/MdmStatusBadge.vue';
import MdmFormDrawer from '../../components/mdm/MdmFormDrawer.vue';
import MdmDetailDrawer from '../../components/mdm/MdmDetailDrawer.vue';
import MdmPagination from '../../components/mdm/MdmPagination.vue';
import MdmEmptyState from '../../components/mdm/MdmEmptyState.vue';

import { ApiError } from '../../api/http';
import { useAuthStore } from '../../stores/auth';
import * as whApi from '../../api/mdm/warehouse';
import {
  STATUS_OPTIONS,
  WAREHOUSE_TYPE_OPTIONS,
  whTypeUiToInt,
  statusUiToInt,
} from '../../types/mdm';
import type {
  Warehouse,
  WarehouseForm,
  WarehouseType,
  MasterDataStatus,
} from '../../types/mdm';

const auth = useAuthStore();
const router = useRouter();

// ===== Real API list state =====
const list = ref<Warehouse[]>([]);
const total = ref(0);
const loading = ref(false);
const error = ref<string | null>(null);

const searchKeyword = ref('');
const filterType = ref<WarehouseType | ''>('');
const filterStatus = ref<MasterDataStatus | ''>('');
const page = reactive({ current: 1, size: 20 });

async function fetchList() {
  loading.value = true;
  error.value = null;
  try {
    const result = await whApi.listWarehouses({
      keyword: searchKeyword.value.trim() || undefined,
      type: filterType.value ? whTypeUiToInt(filterType.value) : undefined,
      status: filterStatus.value ? statusUiToInt(filterStatus.value) : undefined,
      page: page.current,
      pageSize: page.size,
    });
    list.value = result.items;
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
      error.value = '加载仓库数据失败，请刷新重试';
    }
    list.value = [];
    total.value = 0;
  } finally {
    loading.value = false;
  }
}
onMounted(fetchList);

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
const formData = reactive<WarehouseForm>(emptyForm());

function emptyForm(): WarehouseForm {
  return {
    code: '', name: '', type: 'PHYSICAL', plantId: '',
    addressLine1: '', addressLine2: '', city: '', region: '',
    postalCode: '', countryCode: '', status: 'active', description: '',
  };
}

const formRules: FormRules = {
  code: [{ required: true, message: '请输入仓库代码', trigger: 'blur' }],
  name: [{ required: true, message: '请输入仓库名称', trigger: 'blur' }],
  type: [{ required: true, message: '请选择仓库类型', trigger: 'change' }],
  countryCode: [
    { pattern: /^[A-Za-z]{2}$/, message: '请输入 2 位国家代码（如 CN）', trigger: 'blur' },
  ],
};

function resetForm() {
  Object.assign(formData, emptyForm());
}

function openCreate() {
  editingId.value = null;
  editingConcurrency.value = 0;
  resetForm();
  formDrawerVisible.value = true;
}

async function openEdit(row: Warehouse) {
  editingId.value = row.id;
  editingConcurrency.value = row.concurrencyVersion ?? 0;
  try {
    const fresh = await whApi.getWarehouse(row.id);
    fillForm(fresh);
    editingConcurrency.value = fresh.concurrencyVersion ?? 0;
  } catch (e) {
    fillForm(row);
  }
  formDrawerVisible.value = true;
}

function fillForm(d: Warehouse) {
  Object.assign(formData, {
    code: d.code, name: d.name, type: d.type,
    plantId: d.plantId != null ? String(d.plantId) : '',
    addressLine1: d.addressLine1 || '', addressLine2: d.addressLine2 || '',
    city: d.city || '', region: d.region || '',
    postalCode: d.postalCode || '', countryCode: d.countryCode || '',
    status: d.status, description: d.description || '',
  });
}

function isConcurrencyConflict(err: unknown): boolean {
  return err instanceof ApiError && err.code === 'mdm_validation_failed'
    && /concurrency|version|并发/i.test(err.detail || '');
}

async function handleSubmit() {
  submitting.value = true;
  try {
    if (editingId.value == null) {
      await whApi.createWarehouse(formData);
      ElMessage.success('创建仓库成功');
    } else {
      await whApi.updateWarehouse(editingId.value, formData, editingConcurrency.value);
      ElMessage.success('保存修改成功');
    }
    formDrawerVisible.value = false;
    await fetchList();
  } catch (e) {
    if (isConcurrencyConflict(e)) {
      ElMessage.warning('并发冲突：数据已被其他用户修改，请刷新后重试');
    } else if (e instanceof ApiError) {
      const parts = [e.title];
      if (e.detail) parts.push(e.detail);
      if (e.requestId) parts.push(`(${e.requestId})`);
      ElMessage.error(parts.join(' — '));
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
const detailData = ref<Warehouse | null>(null);

async function openDetail(row: Warehouse) {
  try {
    detailData.value = await whApi.getWarehouse(row.id);
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
function typeLabel(t?: WarehouseType): string {
  return WAREHOUSE_TYPE_OPTIONS.find(o => o.value === t)?.label || '—';
}
function formatAddress(d: Warehouse | null): string {
  if (!d) return '';
  return [d.addressLine1, d.addressLine2, d.city, d.region, d.postalCode]
    .filter(Boolean).join(' ');
}

// ===== Status change =====
async function confirmDeactivate(row: Warehouse) {
  // Per Handoff §6: V1 does NOT block deactivating a Warehouse with Active
  // Locations, but the SPA warns the operator.
  try {
    await ElMessageBox.confirm(
      `确定停用仓库"${row.name}"吗？停用后该仓库不能用于新的业务单据。\n注意：如该仓库下仍有启用状态的库位，建议先停用相关库位。`,
      '停用确认',
      { confirmButtonText: '停用', cancelButtonText: '取消', type: 'warning' },
    );
  } catch { return; }
  try {
    await whApi.setWarehouseStatus(row, 'inactive');
    ElMessage.success('已停用');
    await fetchList();
  } catch (e) {
    if (isConcurrencyConflict(e)) ElMessage.warning('并发冲突：数据已被其他用户修改，请刷新后重试');
    else if (e instanceof ApiError) ElMessage.error(`${e.title}${e.detail ? ' — ' + e.detail : ''}`);
    else ElMessage.error('停用失败');
  }
}

async function confirmActivate(row: Warehouse) {
  try {
    await ElMessageBox.confirm(`确定启用仓库"${row.name}"吗？`, '启用确认',
      { confirmButtonText: '启用', cancelButtonText: '取消', type: 'info' });
  } catch { return; }
  try {
    await whApi.setWarehouseStatus(row, 'active');
    ElMessage.success('已启用');
    await fetchList();
  } catch (e) {
    if (isConcurrencyConflict(e)) ElMessage.warning('并发冲突：数据已被其他用户修改，请刷新后重试');
    else if (e instanceof ApiError) ElMessage.error(`${e.title}${e.detail ? ' — ' + e.detail : ''}`);
    else ElMessage.error('启用失败');
  }
}

// ===== Navigation to Location (clear entry per §12) =====
function goAllLocations() {
  router.push('/mdm/locations').catch(() => {});
}
function goLocations(row: Warehouse) {
  router.push({ path: '/mdm/locations', query: { warehouseId: String(row.id) } }).catch(() => {});
}

function exportData() {
  ElMessage.info('导出功能待后端支持');
}
</script>

<style scoped>
/* GULIERP_PAGE_THEME_AUDIT_001 / Phase 1 (2026-08-23).
   .mdm-list, .mdm-code, .mdm-detail-title, .mdm-detail-name,
   .mdm-error-banner, :deep(.mdm-action-sep) are shared
   (see design-system/components/mdm-page.css).
   Page-specific: the warehouse context bar (current warehouse label). */
.mdm-context-bar {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 6px 12px;
  font-size: 13px;
  color: var(--text-muted);
  background: var(--bg-container);
  border-bottom: 1px solid var(--border-subtle);
}
.mdm-context-hint {
  font-size: 12px;
  color: var(--text-disabled);
}
</style>
