<template>
  <!-- ItemList — Item master data (THIRD MODULE — reuses all MDM components + adds tabbed detail)
       This is the key page. Detail drawer includes tabbed sections:
       Basic | Inventory | Sales | Purchase | Attributes
       Only Basic + Inventory have content (frozen evidence). Others are reserved placeholders. -->
  <div class="mdm-list">
    <!-- Toolbar (reused) -->
    <MdmListToolbar
      v-model:search="searchKeyword"
      search-placeholder="搜索物料代码 / 名称 / 规格型号"
      create-label="新建物料"
      @search="applyFilters"
      @create="openCreate"
    >
      <template #filters>
        <el-select v-model="filterCategory" placeholder="分类" clearable filterable style="width: 150px" @change="applyFilters">
          <el-option
            v-for="c in mockItemCategories"
            :key="c.id"
            :label="c.fullPath"
            :value="c.id"
          />
        </el-select>
        <el-select v-model="filterType" placeholder="类型" clearable style="width: 110px" @change="applyFilters">
          <el-option label="物品" value="goods" />
          <el-option label="服务" value="service" />
          <el-option label="包装" value="package" />
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
        <el-table-column prop="code" label="物料代码" width="130" sortable show-overflow-tooltip>
          <template #default="{ row }">
            <span class="mdm-code">{{ row.code }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="name" label="物料名称" min-width="180" show-overflow-tooltip sortable />
        <el-table-column prop="specification" label="规格型号" width="160" show-overflow-tooltip />
        <el-table-column prop="categoryName" label="分类" width="120" show-overflow-tooltip>
          <template #default="{ row }">
            {{ row.categoryName || '—' }}
          </template>
        </el-table-column>
        <el-table-column prop="baseUomName" label="基本单位" width="90" align="center" />
        <el-table-column prop="itemType" label="类型" width="80" align="center">
          <template #default="{ row }">
            {{ typeLabel(row.itemType) }}
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="80" align="center">
          <template #default="{ row }">
            <MdmStatusBadge :status="row.status" />
          </template>
        </el-table-column>
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
            message="暂无物料数据"
            create-label="新建物料"
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
      :title="editingId ? '编辑物料' : '新建物料'"
      :rules="formRules"
      :submit-label="editingId ? '保存修改' : '创建'"
      @submit="handleSubmit"
    >
      <el-form-item label="物料代码" prop="code">
        <el-input v-model="formData.code" placeholder="如 ITEM-0001" :disabled="!!editingId" maxlength="30" />
      </el-form-item>
      <el-form-item label="物料名称" prop="name">
        <el-input v-model="formData.name" placeholder="物料全称" maxlength="100" />
      </el-form-item>
      <el-form-item label="规格型号">
        <el-input v-model="formData.specification" placeholder="型号 / 规格" maxlength="100" />
      </el-form-item>
      <el-form-item label="物料分类" prop="categoryId">
        <el-select v-model="formData.categoryId" placeholder="选择分类" clearable filterable style="width: 100%">
          <el-option
            v-for="c in mockItemCategories"
            :key="c.id"
            :label="c.fullPath"
            :value="c.id"
          />
        </el-select>
      </el-form-item>
      <el-form-item label="基本单位" prop="baseUomId">
        <el-select v-model="formData.baseUomId" placeholder="选择计量单位" clearable filterable style="width: 100%">
          <el-option
            v-for="u in mockUoms"
            :key="u.id"
            :label="`${u.name} (${u.code})`"
            :value="u.id"
            :disabled="u.status !== 'active'"
          />
        </el-select>
      </el-form-item>
      <el-form-item label="物料类型" prop="itemType">
        <el-radio-group v-model="formData.itemType">
          <el-radio value="goods">物品</el-radio>
          <el-radio value="service">服务</el-radio>
          <el-radio value="package">包装</el-radio>
        </el-radio-group>
      </el-form-item>
      <el-form-item label="状态" prop="status">
        <el-select v-model="formData.status" style="width: 100%">
          <el-option v-for="opt in STATUS_OPTIONS" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
      </el-form-item>
      <el-form-item v-if="formData.itemType === 'goods'" label="计价方法">
        <el-select v-model="formData.inventoryMethod" placeholder="选择计价方法" clearable style="width: 100%">
          <el-option label="先进先出 (FIFO)" value="FIFO" />
          <el-option label="后进先出 (LIFO)" value="LIFO" />
          <el-option label="加权平均" value="WEIGHTED_AVG" />
          <el-option label="个别计价" value="SPECIFIC" />
        </el-select>
      </el-form-item>
      <el-form-item label="说明">
        <el-input v-model="formData.description" type="textarea" :rows="2" maxlength="200" show-word-limit />
      </el-form-item>
    </MdmFormDrawer>

    <!-- Detail Drawer with Tabs (reused MdmDetailDrawer + tabbed content) -->
    <MdmDetailDrawer
      v-model="detailDrawerVisible"
      :title="`物料详情 · ${detailData?.code || ''}`"
      @edit="openEditFromDetail"
    >
      <template #header>
        <div class="mdm-detail-title">
          <MdmStatusBadge :status="detailData?.status || 'draft'" />
          <span class="mdm-detail-name">{{ detailData?.name }}</span>
          <span v-if="detailData?.specification" class="mdm-detail-spec">{{ detailData.specification }}</span>
        </div>
      </template>

      <!-- Tabbed sections: Basic | Inventory | Sales | Purchase | Attributes -->
      <el-tabs v-model="detailTab" class="mdm-detail-tabs">
        <el-tab-pane label="基本信息" name="basic">
          <el-descriptions :column="2" border size="small">
            <el-descriptions-item label="物料代码">{{ detailData?.code }}</el-descriptions-item>
            <el-descriptions-item label="物料名称">{{ detailData?.name }}</el-descriptions-item>
            <el-descriptions-item label="规格型号">{{ detailData?.specification || '—' }}</el-descriptions-item>
            <el-descriptions-item label="物料分类">{{ detailData?.categoryName || '—' }}</el-descriptions-item>
            <el-descriptions-item label="基本单位">{{ detailData?.baseUomName || '—' }}</el-descriptions-item>
            <el-descriptions-item label="物料类型">{{ typeLabel(detailData?.itemType) }}</el-descriptions-item>
            <el-descriptions-item label="状态">{{ getStatusLabel(detailData?.status) }}</el-descriptions-item>
            <el-descriptions-item label="说明">{{ detailData?.description || '—' }}</el-descriptions-item>
          </el-descriptions>
        </el-tab-pane>

        <el-tab-pane label="库存" name="inventory">
          <el-descriptions :column="2" border size="small">
            <el-descriptions-item label="计价方法">{{ invMethodLabel(detailData?.inventoryMethod) }}</el-descriptions-item>
            <el-descriptions-item label="基本单位">{{ detailData?.baseUomName || '—' }}</el-descriptions-item>
          </el-descriptions>
          <div class="mdm-tab-placeholder">
            <el-icon :size="32"><Box /></el-icon>
            <p>更多库存属性待业务 spec 冻结后补充</p>
          </div>
        </el-tab-pane>

        <!-- Reserved sections — NOT frozen, no fake data -->
        <el-tab-pane label="销售" name="sales">
          <div class="mdm-tab-placeholder">
            <el-icon :size="32"><ShoppingCart /></el-icon>
            <p>销售属性 — 待业务 spec 冻结</p>
          </div>
        </el-tab-pane>
        <el-tab-pane label="采购" name="purchase">
          <div class="mdm-tab-placeholder">
            <el-icon :size="32"><Goods /></el-icon>
            <p>采购属性 — 待业务 spec 冻结</p>
          </div>
        </el-tab-pane>
        <el-tab-pane label="属性" name="attributes">
          <div class="mdm-tab-placeholder">
            <el-icon :size="32"><Collection /></el-icon>
            <p>扩展属性 — 待业务 spec 冻结</p>
          </div>
        </el-tab-pane>
      </el-tabs>

      <!-- Meta info -->
      <el-descriptions :column="2" border size="small" style="margin-top: 16px">
        <el-descriptions-item label="创建时间">{{ formatDate(detailData?.createdAt) }}</el-descriptions-item>
        <el-descriptions-item label="更新时间">{{ formatDate(detailData?.updatedAt) }}</el-descriptions-item>
      </el-descriptions>
    </MdmDetailDrawer>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed } from 'vue';
import { Download, Box, ShoppingCart, Goods, Collection } from '@element-plus/icons-vue';
import { ElMessage } from 'element-plus';
import type { FormRules } from 'element-plus';

import MdmListToolbar from '../../components/mdm/MdmListToolbar.vue';
import MdmStatusBadge from '../../components/mdm/MdmStatusBadge.vue';
import MdmFormDrawer from '../../components/mdm/MdmFormDrawer.vue';
import MdmDetailDrawer from '../../components/mdm/MdmDetailDrawer.vue';
import MdmPagination from '../../components/mdm/MdmPagination.vue';
import MdmEmptyState from '../../components/mdm/MdmEmptyState.vue';

import { mockItems, mockItemCategories, mockUoms } from '../../mock/mdm';
import { STATUS_OPTIONS } from '../../types/mdm';
import type { Item, ItemForm, ItemType, MasterDataStatus, InventoryMethod } from '../../types/mdm';

// ===== List state =====
const searchKeyword = ref('');
const filterCategory = ref<number | ''>('');
const filterType = ref<ItemType | ''>('');
const filterStatus = ref<MasterDataStatus | ''>('');
const page = reactive({ current: 1, size: 20 });

const filteredData = computed(() => {
  let list = mockItems;
  const kw = searchKeyword.value.trim().toLowerCase();
  if (kw) {
    list = list.filter(i =>
      i.code.toLowerCase().includes(kw) ||
      i.name.toLowerCase().includes(kw) ||
      (i.specification || '').toLowerCase().includes(kw)
    );
  }
  if (filterCategory.value) {
    list = list.filter(i => i.categoryId === filterCategory.value);
  }
  if (filterType.value) {
    list = list.filter(i => i.itemType === filterType.value);
  }
  if (filterStatus.value) {
    list = list.filter(i => i.status === filterStatus.value);
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
const formData = reactive<ItemForm>({
  code: '', name: '', specification: '', categoryId: null,
  baseUomId: null, itemType: 'goods', status: 'active',
  inventoryMethod: 'WEIGHTED_AVG', description: '',
});

const formRules: FormRules<ItemForm> = {
  code: [{ required: true, message: '请输入物料代码', trigger: 'blur' }],
  name: [{ required: true, message: '请输入物料名称', trigger: 'blur' }],
  categoryId: [{ required: true, message: '请选择物料分类', trigger: 'change' }],
  baseUomId: [{ required: true, message: '请选择基本单位', trigger: 'change' }],
};

function resetForm() {
  Object.assign(formData, {
    code: '', name: '', specification: '', categoryId: null,
    baseUomId: null, itemType: 'goods', status: 'active',
    inventoryMethod: 'WEIGHTED_AVG', description: '',
  });
}

function openCreate() {
  editingId.value = null;
  resetForm();
  formDrawerVisible.value = true;
}

function openEdit(row: Item) {
  editingId.value = row.id;
  Object.assign(formData, {
    code: row.code, name: row.name, specification: row.specification || '',
    categoryId: row.categoryId, baseUomId: row.baseUomId,
    itemType: row.itemType, status: row.status,
    inventoryMethod: row.inventoryMethod || 'WEIGHTED_AVG',
    description: row.description || '',
  });
  formDrawerVisible.value = true;
}

function handleSubmit() {
  ElMessage.success(editingId.value ? '保存成功（Mock）' : '创建成功（Mock）');
  formDrawerVisible.value = false;
}

// ===== Detail state =====
const detailDrawerVisible = ref(false);
const detailData = ref<Item | null>(null);
const detailTab = ref('basic');

function openDetail(row: Item) {
  detailData.value = row;
  detailTab.value = 'basic';
  detailDrawerVisible.value = true;
}

function openEditFromDetail() {
  if (detailData.value) {
    openEdit(detailData.value);
    detailDrawerVisible.value = false;
  }
}

// ===== Helpers (reused from pattern) =====
function formatDate(iso?: string): string {
  if (!iso) return '—';
  const d = new Date(iso);
  return d.toLocaleString('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' });
}

function getStatusLabel(status?: MasterDataStatus): string {
  return STATUS_OPTIONS.find(o => o.value === status)?.label || '—';
}

function typeLabel(type?: ItemType): string {
  const map: Record<ItemType, string> = { goods: '物品', service: '服务', package: '包装' };
  return type ? map[type] : '—';
}

function invMethodLabel(method?: InventoryMethod): string {
  if (!method) return '—';
  const map: Record<InventoryMethod, string> = {
    FIFO: '先进先出 (FIFO)',
    LIFO: '后进先出 (LIFO)',
    WEIGHTED_AVG: '加权平均',
    SPECIFIC: '个别计价',
  };
  return map[method];
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
.mdm-detail-title {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}
.mdm-detail-name {
  font-size: 18px;
  font-weight: 600;
  color: var(--text-primary, #0F172A);
}
.mdm-detail-spec {
  color: var(--text-muted, #64748B);
  font-size: 13px;
}
.mdm-detail-tabs {
  margin-bottom: 8px;
}
.mdm-tab-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  padding: 24px 0;
  color: var(--text-muted, #64748B);
}
.mdm-tab-placeholder p {
  font-size: 13px;
}
</style>
