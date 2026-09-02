<template>
  <!-- UomList — Unit of Measure master data list (FIRST MDM MODULE — establishes pattern)
       Reuses: MdmListToolbar, MdmStatusBadge, MdmFormDrawer, MdmDetailDrawer, MdmPagination, MdmEmptyState
       Reconciled with MDM_000_MASTER_DATA_CONVENTION_V1 (FROZEN §6):
       - decimalPlaces REMOVED (precision DEFERRED to Business Semantic Precision Convention)
       - dimension + kind ADDED (frozen V1 fields)
       - status: active/inactive only (draft removed) -->
  <div class="mdm-list">
    <!-- Toolbar (reused component) -->
    <MdmListToolbar
      v-model:search="searchKeyword"
      search-placeholder="搜索代码 / 名称 / 符号"
      create-label="新建计量单位"
      @search="applyFilters"
      @create="openCreate"
    >
      <template #filters>
        <el-select v-model="filterDimension" placeholder="量纲" clearable style="width: 110px" @change="applyFilters">
          <el-option v-for="opt in DIMENSION_OPTIONS" :key="opt.value" :label="opt.label" :value="opt.value" />
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

    <!-- Data table -->
    <div class="gs-list-wrap">
      <el-table
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
        <el-table-column prop="code" label="代码" :width="COL.code" sortable show-overflow-tooltip>
          <template #default="{ row }">
            <span class="mdm-code">{{ row.code }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="name" label="名称" min-width="200" show-overflow-tooltip />
        <el-table-column prop="symbol" label="符号" width="90">
          <template #default="{ row }">
            {{ row.symbol || '—' }}
          </template>
        </el-table-column>
        <el-table-column prop="dimension" label="量纲" width="100" align="center">
          <template #default="{ row }">
            {{ dimensionLabel(row.dimension) }}
          </template>
        </el-table-column>
        <el-table-column prop="kind" label="类型" width="100" align="center">
          <template #default="{ row }">
            {{ kindLabel(row.kind) }}
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" :width="COL.status" align="center">
          <template #default="{ row }">
            <MdmStatusBadge :status="row.status" />
          </template>
        </el-table-column>
        <el-table-column prop="description" label="说明" :min-width="COL.descriptionMin" show-overflow-tooltip />
        <el-table-column prop="updatedAt" label="更新时间" :width="COL.datetime" sortable>
          <template #default="{ row }">
            {{ formatDate(row.updatedAt) }}
          </template>
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
            message="暂无计量单位数据"
            create-label="新建计量单位"
            @create="openCreate"
          />
        </template>
      </el-table>
    </div>

    <!-- Pagination (reused component) -->
    <MdmPagination
      v-model:current-page="page.current"
      v-model:page-size="page.size"
      :total="total"
    />
    <!-- API failure banner (do NOT confuse with empty data) -->
    <div v-if="error && !loading" class="mdm-error-banner">
      <el-alert :title="error" type="error" show-icon :closable="false" />
      <el-button type="primary" link style="margin-left:12px" @click="fetchUoms">重新加载</el-button>
    </div>

    <!-- Create/Edit Form Drawer (reused component) -->
    <MdmFormDrawer
      v-model="formDrawerVisible"
      v-model:model="formData"
      :title="editingId ? '编辑计量单位' : '新建计量单位'"
      :rules="formRules"
      :submit-label="editingId ? '保存修改' : '创建'"
      @submit="handleSubmit"
    >
      <el-form-item label="代码" prop="code">
        <el-input v-model="formData.code" placeholder="如 PCS, KG" :disabled="!!editingId" maxlength="20" />
      </el-form-item>
      <el-form-item label="名称" prop="name">
        <el-input v-model="formData.name" placeholder="如 个、千克" maxlength="50" />
      </el-form-item>
      <el-form-item label="符号">
        <el-input v-model="formData.symbol" placeholder="留空表示无符号（计数类）" maxlength="20" />
      </el-form-item>
      <el-form-item label="量纲" prop="dimension">
        <el-select v-model="formData.dimension" style="width: 100%">
          <el-option v-for="opt in DIMENSION_OPTIONS" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
      </el-form-item>
      <el-form-item label="类型" prop="kind">
        <el-select v-model="formData.kind" style="width: 100%">
          <el-option v-for="opt in KIND_OPTIONS" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
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

    <!-- Detail Drawer (reused component) -->
    <MdmDetailDrawer
      v-model="detailDrawerVisible"
      :title="`计量单位详情 · ${detailData?.code || ''}`"
      @edit="openEditFromDetail"
    >
      <template #header>
        <div class="mdm-detail-title">
          <MdmStatusBadge :status="detailData?.status || 'active'" />
          <span class="mdm-detail-name">{{ detailData?.name }}</span>
          <span class="mdm-detail-symbol">符号：{{ detailData?.symbol || '—' }}</span>
        </div>
      </template>
      <el-descriptions-item label="代码">{{ detailData?.code }}</el-descriptions-item>
      <el-descriptions-item label="名称">{{ detailData?.name }}</el-descriptions-item>
      <el-descriptions-item label="符号">{{ detailData?.symbol || '—' }}</el-descriptions-item>
      <el-descriptions-item label="量纲">{{ dimensionLabel(detailData?.dimension) }}</el-descriptions-item>
      <el-descriptions-item label="类型">{{ kindLabel(detailData?.kind) }}</el-descriptions-item>
      <el-descriptions-item label="状态">{{ getStatusLabel(detailData?.status) }}</el-descriptions-item>
      <el-descriptions-item label="说明">{{ detailData?.description || '—' }}</el-descriptions-item>
      <el-descriptions-item label="创建时间">{{ formatDate(detailData?.createdAt) }}</el-descriptions-item>
      <el-descriptions-item label="更新时间">{{ formatDate(detailData?.updatedAt) }}</el-descriptions-item>
    </MdmDetailDrawer>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue';
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
import * as uomApi from '../../api/mdm/uom';
import {
  STATUS_OPTIONS, DIMENSION_OPTIONS, KIND_OPTIONS,
  dimUiToInt, statusUiToInt,
} from '../../types/mdm';
import type { Uom, UomForm, UomDimension, UomKind, MasterDataStatus } from '../../types/mdm';

// ===== Real API list state =====
const uoms = ref<Uom[]>([]);
const total = ref(0);
const loading = ref(false);
const error = ref<string | null>(null);

const searchKeyword = ref('');
const filterStatus = ref<MasterDataStatus | ''>('');
const filterDimension = ref<UomDimension | ''>('');
const page = reactive({ current: 1, size: 20 });

async function fetchUoms() {
  loading.value = true;
  error.value = null;
  try {
    const result = await uomApi.listUoms({
      keyword: searchKeyword.value.trim() || undefined,
      status: filterStatus.value ? statusUiToInt(filterStatus.value) : undefined,
      dimension: filterDimension.value ? dimUiToInt(filterDimension.value) : undefined,
      page: page.current,
      pageSize: page.size,
    });
    uoms.value = result.items;
    total.value = result.totalCount;
  } catch (e) {
    if (e instanceof ApiError) {
      if (e.code === 'authentication_required') throw e; // let global guard redirect
      const parts = [e.title];
      if (e.detail) parts.push(e.detail);
      if (e.requestId) parts.push(`(RequestId: ${e.requestId})`);
      error.value = parts.join(' — ');
    } else {
      error.value = '加载计量单位失败，请刷新重试';
    }
    uoms.value = [];
    total.value = 0;
  } finally {
    loading.value = false;
  }
}
onMounted(fetchUoms);

const pagedData = computed(() => uoms.value);

function applyFilters() {
  page.current = 1;
  fetchUoms();
}

// ===== Form state =====
const formDrawerVisible = ref(false);
const editingId = ref<string | null>(null);
const editingConcurrency = ref(0);
const submitting = ref(false);
const formData = reactive<UomForm>({
  code: '', name: '', symbol: null, dimension: 'COUNT', kind: 'DISCRETE', status: 'active', description: '',
});

const formRules: FormRules = {
  code: [{ required: true, message: '请输入计量单位代码', trigger: 'blur' }],
  name: [{ required: true, message: '请输入计量单位名称', trigger: 'blur' }],
  dimension: [{ required: true, message: '请选择量纲', trigger: 'change' }],
  kind: [{ required: true, message: '请选择类型', trigger: 'change' }],
};

function resetForm() {
  Object.assign(formData, { code: '', name: '', symbol: null, dimension: 'COUNT', kind: 'DISCRETE', status: 'active', description: '' });
}

function openCreate() {
  editingId.value = null;
  editingConcurrency.value = 0;
  resetForm();
  formDrawerVisible.value = true;
}

async function openEdit(row: Uom) {
  editingId.value = row.id;
  editingConcurrency.value = row.concurrencyVersion ?? 0;
  // Re-read via API for the authoritative DTO (fresh concurrencyVersion + fields).
  try {
    const fresh = await uomApi.getUom(row.id);
    Object.assign(formData, {
      code: fresh.code, name: fresh.name, symbol: fresh.symbol,
      dimension: fresh.dimension, kind: fresh.kind,
      status: fresh.status, description: fresh.description || '',
    });
    editingConcurrency.value = fresh.concurrencyVersion ?? 0;
  } catch (e) {
    // Fall back to row values so the user can still attempt work;
    // concurrency mismatch will be caught at save time.
    Object.assign(formData, {
      code: row.code, name: row.name, symbol: row.symbol,
      dimension: row.dimension, kind: row.kind,
      status: row.status, description: row.description || '',
    });
  }
  formDrawerVisible.value = true;
}

function isConcurrencyConflict(err: unknown): boolean {
  return err instanceof ApiError && (
    err.code === 'mdm_validation_failed' && /concurrency|version|并发/i.test(err.detail || '')
  );
}

async function handleSubmit() {
  submitting.value = true;
  try {
    if (editingId.value == null) {
      await uomApi.createUom(formData);
      ElMessage.success('创建计量单位成功');
    } else {
      await uomApi.updateUom(editingId.value, formData, editingConcurrency.value);
      ElMessage.success('保存修改成功');
    }
    formDrawerVisible.value = false;
    await fetchUoms();
  } catch (e) {
    if (isConcurrencyConflict(e)) {
      ElMessage.warning('并发冲突：数据已被其他用户修改，请刷新后重试');
    } else if (e instanceof ApiError) {
      const parts = [e.title];
      if (e.detail) parts.push(e.detail);
      if (e.requestId) parts.push(`(${e.requestId})`);
      ElMessage.error(parts.join(' — '));
    } else {
      ElMessage.error('保存失败，请重试');
    }
  } finally {
    submitting.value = false;
  }
}

// ===== Detail state =====
const detailDrawerVisible = ref(false);
const detailData = ref<Uom | null>(null);

async function openDetail(row: Uom) {
  try {
    detailData.value = await uomApi.getUom(row.id);
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
function dimensionLabel(dim?: UomDimension): string {
  return DIMENSION_OPTIONS.find(o => o.value === dim)?.label || '—';
}
function kindLabel(k?: UomKind): string {
  return KIND_OPTIONS.find(o => o.value === k)?.label || '—';
}

// ===== Status change (active/inactive, NO delete) =====
async function confirmDeactivate(row: Uom) {
  try {
    await ElMessageBox.confirm(
      `确定停用"${row.name}"吗？停用后将不能用于新的业务单据，历史数据不受影响。`,
      '停用确认',
      { confirmButtonText: '停用', cancelButtonText: '取消', type: 'warning' },
    );
  } catch { return; }
  try {
    await uomApi.setUomStatus(row, 'inactive');
    ElMessage.success('已停用');
    await fetchUoms();
  } catch (e) {
    if (isConcurrencyConflict(e)) {
      ElMessage.warning('并发冲突：数据已被其他用户修改，请刷新后重试');
    } else if (e instanceof ApiError) {
      ElMessage.error(`${e.title}${e.detail ? ' — ' + e.detail : ''}`);
    } else {
      ElMessage.error('停用失败');
    }
  }
}

async function confirmActivate(row: Uom) {
  try {
    await ElMessageBox.confirm(`确定启用"${row.name}"吗？`, '启用确认',
      { confirmButtonText: '启用', cancelButtonText: '取消', type: 'info' });
  } catch { return; }
  try {
    await uomApi.setUomStatus(row, 'active');
    ElMessage.success('已启用');
    await fetchUoms();
  } catch (e) {
    if (isConcurrencyConflict(e)) {
      ElMessage.warning('并发冲突：数据已被其他用户修改，请刷新后重试');
    } else if (e instanceof ApiError) {
      ElMessage.error(`${e.title}${e.detail ? ' — ' + e.detail : ''}`);
    } else {
      ElMessage.error('启用失败');
    }
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
   Page-specific accent: the unit symbol hint on the detail drawer. */
.mdm-detail-symbol {
  color: var(--text-muted);
  font-size: 13px;
}
</style>
