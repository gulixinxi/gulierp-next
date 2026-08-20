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
        <el-table-column type="index" label="#" width="50" fixed="left" />
        <el-table-column prop="code" label="代码" width="100" sortable show-overflow-tooltip>
          <template #default="{ row }">
            <span class="mdm-code">{{ row.code }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="name" label="名称" width="120" show-overflow-tooltip />
        <el-table-column prop="symbol" label="符号" width="80">
          <template #default="{ row }">
            {{ row.symbol || '—' }}
          </template>
        </el-table-column>
        <el-table-column prop="dimension" label="量纲" width="90" align="center">
          <template #default="{ row }">
            {{ dimensionLabel(row.dimension) }}
          </template>
        </el-table-column>
        <el-table-column prop="kind" label="类型" width="80" align="center">
          <template #default="{ row }">
            {{ kindLabel(row.kind) }}
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="80" align="center">
          <template #default="{ row }">
            <MdmStatusBadge :status="row.status" />
          </template>
        </el-table-column>
        <el-table-column prop="description" label="说明" min-width="180" show-overflow-tooltip />
        <el-table-column prop="updatedAt" label="更新时间" width="160" sortable>
          <template #default="{ row }">
            {{ formatDate(row.updatedAt) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="120" fixed="right" align="center">
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
      :total="filteredData.length"
    />

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
import { ref, reactive, computed } from 'vue';
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

import { mockUoms } from '../../mock/mdm';
import { STATUS_OPTIONS, DIMENSION_OPTIONS, KIND_OPTIONS } from '../../types/mdm';
import type { Uom, UomForm, UomDimension, UomKind, MasterDataStatus } from '../../types/mdm';

// ===== List state =====
const searchKeyword = ref('');
const filterStatus = ref<MasterDataStatus | ''>('');
const filterDimension = ref<UomDimension | ''>('');
const page = reactive({ current: 1, size: 20 });

const filteredData = computed(() => {
  let list = mockUoms;
  const kw = searchKeyword.value.trim().toLowerCase();
  if (kw) {
    list = list.filter(u =>
      u.code.toLowerCase().includes(kw) ||
      u.name.toLowerCase().includes(kw) ||
      (u.symbol || '').toLowerCase().includes(kw)
    );
  }
  if (filterStatus.value) {
    list = list.filter(u => u.status === filterStatus.value);
  }
  if (filterDimension.value) {
    list = list.filter(u => u.dimension === filterDimension.value);
  }
  return list;
});

const pagedData = computed(() => {
  const start = (page.current - 1) * page.size;
  return filteredData.value.slice(start, start + page.size);
});

function applyFilters() {
  page.current = 1;
}

// ===== Form state =====
const formDrawerVisible = ref(false);
const editingId = ref<number | null>(null);
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
  resetForm();
  formDrawerVisible.value = true;
}

function openEdit(row: Uom) {
  editingId.value = row.id;
  Object.assign(formData, {
    code: row.code, name: row.name, symbol: row.symbol,
    dimension: row.dimension, kind: row.kind,
    status: row.status, description: row.description || '',
  });
  formDrawerVisible.value = true;
}

function handleSubmit() {
  // MOCK: no real API call. Just show success message.
  ElMessage.success(editingId.value ? '保存成功（Mock）' : '创建成功（Mock）');
  formDrawerVisible.value = false;
}

// ===== Detail state =====
const detailDrawerVisible = ref(false);
const detailData = ref<Uom | null>(null);

function openDetail(row: Uom) {
  detailData.value = row;
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
  // MOCK: no real API. Update local status.
  row.status = 'inactive';
  ElMessage.success('已停用（Mock）');
}

async function confirmActivate(row: Uom) {
  try {
    await ElMessageBox.confirm(
      `确定启用"${row.name}"吗？`,
      '启用确认',
      { confirmButtonText: '启用', cancelButtonText: '取消', type: 'info' },
    );
  } catch { return; }
  row.status = 'active';
  ElMessage.success('已启用（Mock）');
}

function exportData() {
  ElMessage.info('导出功能待后端支持');
}
</script>

<style scoped>
.mdm-list {
  display: flex;
  flex-direction: column;
  height: 100%;
  gap: 0;
}
.mdm-code {
  font-family: 'SF Mono', 'Fira Code', monospace;
  font-weight: 600;
  color: var(--primary-default, #0284C7);
}
.mdm-detail-title {
  display: flex;
  align-items: center;
  gap: 12px;
}
.mdm-detail-name {
  font-size: 18px;
  font-weight: 600;
  color: var(--text-primary, #0F172A);
}
.mdm-detail-symbol {
  color: var(--text-muted, #64748B);
  font-size: 13px;
}
</style>
