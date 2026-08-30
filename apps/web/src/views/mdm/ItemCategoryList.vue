<template>
  <!-- ItemCategoryList — Item Category master data (SECOND MODULE — reuses all MDM components)
       Hierarchy shown via fullPath + indented name column (no heavy tree framework)
       Reconciled with MDM_000_MASTER_DATA_CONVENTION_V1 (FROZEN §7): level/fullPath are UI-derived, NOT persisted backend fields -->
  <div class="mdm-list">
    <!-- Toolbar (reused) -->
    <MdmListToolbar
      v-model:search="searchKeyword"
      search-placeholder="搜索代码 / 名称"
      create-label="新建分类"
      @search="applyFilters"
      @create="openCreate"
    >
      <template #filters>
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

    <!-- Data table (reused pattern) -->
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
        <el-table-column type="index" label="#" width="48" fixed="left" />
        <el-table-column prop="code" label="代码" width="150" sortable show-overflow-tooltip>
          <template #default="{ row }">
            <span class="mdm-code">{{ row.code }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="name" label="名称" min-width="200">
          <template #default="{ row }">
            <span :style="{ paddingLeft: row.level * 16 + 'px' }" class="mdm-cat-name">
              <el-icon v-if="row.level > 0" class="mdm-indent-icon"><DArrowRight /></el-icon>
              {{ row.name }}
            </span>
          </template>
        </el-table-column>
        <el-table-column prop="fullPath" label="层级路径" min-width="240" show-overflow-tooltip>
          <template #default="{ row }">
            <span class="mdm-path">{{ row.fullPath }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="90" align="center">
          <template #default="{ row }">
            <MdmStatusBadge :status="row.status" />
          </template>
        </el-table-column>
        <el-table-column prop="description" label="说明" min-width="220" show-overflow-tooltip />
        <el-table-column prop="updatedAt" label="更新时间" width="165" sortable>
          <template #default="{ row }">
            {{ formatDate(row.updatedAt) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="130" fixed="right" align="center">
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
            message="暂无物料分类数据"
            create-label="新建分类"
            @create="openCreate"
          />
        </template>
      </el-table>
    </div>

    <!-- Pagination (reused) -->
    <MdmPagination
      v-model:current-page="page.current"
      v-model:page-size="page.size"
      :total="total"
    />
    <div v-if="error && !loading" class="mdm-error-banner">
      <el-alert :title="error" type="error" show-icon :closable="false" />
      <el-button type="primary" link style="margin-left:12px" @click="fetchCategories">重新加载</el-button>
    </div>

    <!-- Create/Edit Form Drawer (reused) -->
    <MdmFormDrawer
      v-model="formDrawerVisible"
      v-model:model="formData"
      :title="editingId ? '编辑物料分类' : '新建物料分类'"
      :rules="formRules"
      :submit-label="editingId ? '保存修改' : '创建'"
      @submit="handleSubmit"
    >
      <el-form-item label="代码" prop="code">
        <el-input v-model="formData.code" placeholder="如 RAW, FIN" :disabled="!!editingId" maxlength="30" />
      </el-form-item>
      <el-form-item label="名称" prop="name">
        <el-input v-model="formData.name" placeholder="如 原材料、成品" maxlength="50" />
      </el-form-item>
      <el-form-item label="上级分类" prop="parentId">
        <el-select v-model="formData.parentId" placeholder="选择上级分类（留空为根）" clearable filterable style="width: 100%">
          <el-option
            v-for="c in parentCategoryOptions"
            :key="c.id"
            :label="c.fullPath"
            :value="c.id"
          />
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

    <!-- Detail Drawer (reused) -->
    <MdmDetailDrawer
      v-model="detailDrawerVisible"
      :title="`分类详情 · ${detailData?.code || ''}`"
      @edit="openEditFromDetail"
    >
      <template #header>
        <div class="mdm-detail-title">
          <MdmStatusBadge :status="detailData?.status || 'active'" />
          <span class="mdm-detail-name">{{ detailData?.name }}</span>
        </div>
      </template>
      <el-descriptions-item label="代码">{{ detailData?.code }}</el-descriptions-item>
      <el-descriptions-item label="名称">{{ detailData?.name }}</el-descriptions-item>
      <el-descriptions-item label="层级路径">{{ detailData?.fullPath }}</el-descriptions-item>
      <el-descriptions-item label="层级深度">L{{ detailData?.level }}</el-descriptions-item>
      <el-descriptions-item label="上级分类">{{ detailData?.parentName || '（根分类）' }}</el-descriptions-item>
      <el-descriptions-item label="状态">{{ getStatusLabel(detailData?.status) }}</el-descriptions-item>
      <el-descriptions-item label="说明" :span="2">{{ detailData?.description || '—' }}</el-descriptions-item>
      <el-descriptions-item label="创建时间">{{ formatDate(detailData?.createdAt) }}</el-descriptions-item>
      <el-descriptions-item label="更新时间">{{ formatDate(detailData?.updatedAt) }}</el-descriptions-item>
    </MdmDetailDrawer>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue';
import { Download, DArrowRight } from '@element-plus/icons-vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import type { FormRules } from 'element-plus';

import MdmListToolbar from '../../components/mdm/MdmListToolbar.vue';
import MdmStatusBadge from '../../components/mdm/MdmStatusBadge.vue';
import MdmFormDrawer from '../../components/mdm/MdmFormDrawer.vue';
import MdmDetailDrawer from '../../components/mdm/MdmDetailDrawer.vue';
import MdmPagination from '../../components/mdm/MdmPagination.vue';
import MdmEmptyState from '../../components/mdm/MdmEmptyState.vue';
import MdmTableRowActions from '../../components/mdm/MdmTableRowActions.vue';

import { ApiError } from '../../api/http';
import * as icApi from '../../api/mdm/item-category';
import { STATUS_OPTIONS, statusUiToInt } from '../../types/mdm';
import type { ItemCategoryListItem, ItemCategoryForm, MasterDataStatus, ItemCategory } from '../../types/mdm';

// ===== Real API state =====
const categories = ref<ItemCategoryListItem[]>([]);
const total = ref(0);
const loading = ref(false);
const error = ref<string | null>(null);

const searchKeyword = ref('');
const filterStatus = ref<MasterDataStatus | ''>('');
const page = reactive({ current: 1, size: 20 });

async function fetchCategories() {
  loading.value = true;
  error.value = null;
  try {
    // Use paged list endpoint; hierarchy derivation + sorting done inside API client.
    const result = await icApi.listItemCategories({
      keyword: searchKeyword.value.trim() || undefined,
      status: filterStatus.value ? statusUiToInt(filterStatus.value) : undefined,
      page: page.current,
      pageSize: page.size,
    });
    categories.value = result.items;
    total.value = result.totalCount;
  } catch (e) {
    if (e instanceof ApiError) {
      if (e.code === 'authentication_required') throw e;
      const parts = [e.title];
      if (e.detail) parts.push(e.detail);
      if (e.requestId) parts.push(`(RequestId: ${e.requestId})`);
      error.value = parts.join(' — ');
    } else {
      error.value = '加载物料分类失败，请刷新重试';
    }
    categories.value = [];
    total.value = 0;
  } finally {
    loading.value = false;
  }
}
// Also fetch the FULL category tree (for parent-category options in the form).
const allCategories = ref<ItemCategoryListItem[]>([]);
const parentOptionsLoading = ref(false);
async function fetchAllForSelectors() {
  parentOptionsLoading.value = true;
  try {
    allCategories.value = await icApi.listAllCategories();
  } catch (e) {
    if (e instanceof ApiError && e.code === 'authentication_required') throw e;
    allCategories.value = [];
  } finally {
    parentOptionsLoading.value = false;
  }
}
onMounted(() => {
  fetchCategories();
  fetchAllForSelectors();
});

const pagedData = computed(() => categories.value);

// Parent category options (exclude self when editing)
const parentCategoryOptions = computed(() => {
  if (editingId.value == null) return allCategories.value;
  return allCategories.value.filter(c => c.id !== editingId.value);
});

function applyFilters() {
  page.current = 1;
  fetchCategories();
}

// ===== Form state =====
const formDrawerVisible = ref(false);
const editingId = ref<string | null>(null);
const editingConcurrency = ref(0);
const submitting = ref(false);
const formData = reactive<ItemCategoryForm>({
  code: '', name: '', parentId: null, status: 'active', description: '',
});

const formRules: FormRules<ItemCategoryForm> = {
  code: [{ required: true, message: '请输入分类代码', trigger: 'blur' }],
  name: [{ required: true, message: '请输入分类名称', trigger: 'blur' }],
};

function resetForm() {
  Object.assign(formData, { code: '', name: '', parentId: null, status: 'active', description: '' });
}

function openCreate() {
  editingId.value = null;
  editingConcurrency.value = 0;
  resetForm();
  formDrawerVisible.value = true;
}

async function openEdit(row: ItemCategory) {
  editingId.value = row.id;
  editingConcurrency.value = row.concurrencyVersion ?? 0;
  try {
    const fresh = await icApi.getItemCategory(row.id);
    Object.assign(formData, {
      code: fresh.code, name: fresh.name,
      parentId: fresh.parentId, status: fresh.status, description: fresh.description || '',
    });
    editingConcurrency.value = fresh.concurrencyVersion ?? 0;
  } catch (e) {
    Object.assign(formData, {
      code: row.code, name: row.name,
      parentId: row.parentId, status: row.status, description: row.description || '',
    });
  }
  formDrawerVisible.value = true;
}

function isCycleError(err: unknown): boolean {
  return err instanceof ApiError && err.code === 'mdm_item_category_cycle';
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
      await icApi.createItemCategory(formData);
      ElMessage.success('创建物料分类成功');
    } else {
      await icApi.updateItemCategory(editingId.value, formData, editingConcurrency.value);
      ElMessage.success('保存修改成功');
    }
    formDrawerVisible.value = false;
    await Promise.all([fetchCategories(), fetchAllForSelectors()]);
  } catch (e) {
    if (isCycleError(e)) {
      ElMessage.error('父级分类设置无效：会造成循环引用，请重新选择上级分类');
    } else if (isConcurrencyConflict(e)) {
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
const detailData = ref<ItemCategoryListItem | null>(null);

async function openDetail(row: ItemCategoryListItem) {
  try {
    const raw = await icApi.getItemCategory(row.id);
    // Merge full hierarchy onto it (re-derive with latest allCategories snapshot so fullPath is fresh)
    const enriched = (await icApi.listAllCategories()).find(c => c.id === row.id);
    detailData.value = enriched ?? {
      ...raw,
      level: row.level,
      fullPath: row.fullPath,
      parentName: row.parentName,
    };
  } catch (e) {
    detailData.value = row;
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

// ===== Status change =====
async function confirmDeactivate(row: ItemCategoryListItem) {
  try {
    await ElMessageBox.confirm(
      `确定停用"${row.name}"吗？停用后将不能用于新的业务单据，历史数据不受影响。`,
      '停用确认',
      { confirmButtonText: '停用', cancelButtonText: '取消', type: 'warning' },
    );
  } catch { return; }
  try {
    await icApi.setItemCategoryStatus(row, 'inactive');
    ElMessage.success('已停用');
    await Promise.all([fetchCategories(), fetchAllForSelectors()]);
  } catch (e) {
    if (isConcurrencyConflict(e)) ElMessage.warning('并发冲突：数据已被其他用户修改，请刷新后重试');
    else if (e instanceof ApiError) ElMessage.error(`${e.title}${e.detail ? ' — ' + e.detail : ''}`);
    else ElMessage.error('停用失败');
  }
}

async function confirmActivate(row: ItemCategoryListItem) {
  try {
    await ElMessageBox.confirm(`确定启用"${row.name}"吗？`, '启用确认',
      { confirmButtonText: '启用', cancelButtonText: '取消', type: 'info' });
  } catch { return; }
  try {
    await icApi.setItemCategoryStatus(row, 'active');
    ElMessage.success('已启用');
    await Promise.all([fetchCategories(), fetchAllForSelectors()]);
  } catch (e) {
    if (isConcurrencyConflict(e)) ElMessage.warning('并发冲突：数据已被其他用户修改，请刷新后重试');
    else if (e instanceof ApiError) ElMessage.error(`${e.title}${e.detail ? ' — ' + e.detail : ''}`);
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
   Page-specific accents (tree indent, path label) stay here. */
.mdm-cat-name {
  display: inline-flex;
  align-items: center;
  gap: 4px;
}
.mdm-indent-icon {
  color: var(--text-muted);
  font-size: 12px;
}
.mdm-path {
  color: var(--text-secondary);
  font-size: 13px;
}
</style>
