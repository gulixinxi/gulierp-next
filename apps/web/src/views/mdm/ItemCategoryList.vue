<template>
  <!-- ItemCategoryList — Item Category master data (SECOND MODULE — reuses all MDM components)
       Hierarchy shown via fullPath + indented name column (no heavy tree framework) -->
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
        <el-table-column type="index" label="#" width="50" fixed="left" />
        <el-table-column prop="code" label="代码" width="140" sortable show-overflow-tooltip>
          <template #default="{ row }">
            <span class="mdm-code">{{ row.code }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="name" label="名称" width="160">
          <template #default="{ row }">
            <span :style="{ paddingLeft: row.level * 16 + 'px' }" class="mdm-cat-name">
              <el-icon v-if="row.level > 0" class="mdm-indent-icon"><DArrowRight /></el-icon>
              {{ row.name }}
            </span>
          </template>
        </el-table-column>
        <el-table-column prop="fullPath" label="层级路径" min-width="200" show-overflow-tooltip>
          <template #default="{ row }">
            <span class="mdm-path">{{ row.fullPath }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="80" align="center">
          <template #default="{ row }">
            <MdmStatusBadge :status="row.status" />
          </template>
        </el-table-column>
        <el-table-column prop="description" label="说明" min-width="160" show-overflow-tooltip />
        <el-table-column prop="updatedAt" label="更新时间" width="160" sortable>
          <template #default="{ row }">
            {{ formatDate(row.updatedAt) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="140" fixed="right" align="center">
          <template #default="{ row }">
            <el-button text size="small" type="primary" @click="openDetail(row)">详情</el-button>
            <el-button text size="small" type="primary" @click="openEdit(row)">编辑</el-button>
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
      :total="filteredData.length"
    />

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
          <MdmStatusBadge :status="detailData?.status || 'draft'" />
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
import { ref, reactive, computed } from 'vue';
import { Download, DArrowRight } from '@element-plus/icons-vue';
import { ElMessage } from 'element-plus';
import type { FormRules } from 'element-plus';

import MdmListToolbar from '../../components/mdm/MdmListToolbar.vue';
import MdmStatusBadge from '../../components/mdm/MdmStatusBadge.vue';
import MdmFormDrawer from '../../components/mdm/MdmFormDrawer.vue';
import MdmDetailDrawer from '../../components/mdm/MdmDetailDrawer.vue';
import MdmPagination from '../../components/mdm/MdmPagination.vue';
import MdmEmptyState from '../../components/mdm/MdmEmptyState.vue';

import { mockItemCategories } from '../../mock/mdm';
import { STATUS_OPTIONS } from '../../types/mdm';
import type { ItemCategory, ItemCategoryForm, MasterDataStatus } from '../../types/mdm';

// ===== List state =====
const searchKeyword = ref('');
const filterStatus = ref<MasterDataStatus | ''>('');
const page = reactive({ current: 1, size: 20 });

const filteredData = computed(() => {
  let list = mockItemCategories;
  const kw = searchKeyword.value.trim().toLowerCase();
  if (kw) {
    list = list.filter(c =>
      c.code.toLowerCase().includes(kw) ||
      c.name.toLowerCase().includes(kw)
    );
  }
  if (filterStatus.value) {
    list = list.filter(c => c.status === filterStatus.value);
  }
  // Sort by fullPath to maintain hierarchy order
  return list.sort((a, b) => a.fullPath.localeCompare(b.fullPath));
});

const pagedData = computed(() => {
  const start = (page.current - 1) * page.size;
  return filteredData.value.slice(start, start + page.size);
});

// Parent category options for form select (exclude self when editing)
const parentCategoryOptions = computed(() => {
  return mockItemCategories.filter(c => c.id !== editingId.value);
});

function applyFilters() {
  page.current = 1;
}

// ===== Form state =====
const formDrawerVisible = ref(false);
const editingId = ref<number | null>(null);
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
  resetForm();
  formDrawerVisible.value = true;
}

function openEdit(row: ItemCategory) {
  editingId.value = row.id;
  Object.assign(formData, {
    code: row.code, name: row.name,
    parentId: row.parentId, status: row.status, description: row.description || '',
  });
  formDrawerVisible.value = true;
}

function handleSubmit() {
  ElMessage.success(editingId.value ? '保存成功（Mock）' : '创建成功（Mock）');
  formDrawerVisible.value = false;
}

// ===== Detail state =====
const detailDrawerVisible = ref(false);
const detailData = ref<ItemCategory | null>(null);

function openDetail(row: ItemCategory) {
  detailData.value = row;
  detailDrawerVisible.value = true;
}

function openEditFromDetail() {
  if (detailData.value) {
    openEdit(detailData.value);
    detailDrawerVisible.value = false;
  }
}

// ===== Helpers (reused from UOM pattern) =====
function formatDate(iso?: string): string {
  if (!iso) return '—';
  const d = new Date(iso);
  return d.toLocaleString('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' });
}

function getStatusLabel(status?: MasterDataStatus): string {
  return STATUS_OPTIONS.find(o => o.value === status)?.label || '—';
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
}
.mdm-code {
  font-family: 'SF Mono', 'Fira Code', monospace;
  font-weight: 600;
  color: var(--primary-default, #0284C7);
}
.mdm-cat-name {
  display: inline-flex;
  align-items: center;
  gap: 4px;
}
.mdm-indent-icon {
  color: var(--text-muted, #64748B);
  font-size: 12px;
}
.mdm-path {
  color: var(--text-secondary, #334155);
  font-size: 13px;
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
</style>
