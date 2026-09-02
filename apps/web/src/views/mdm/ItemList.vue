<template>
  <!-- ItemList — Item master data (THIRD MODULE — reuses all MDM components + adds tabbed detail)
       This is the key page. Detail drawer includes tabbed sections:
       Basic | Inventory | Sales | Purchase | Attributes
       Only Basic + Inventory have content (frozen evidence). Others are reserved placeholders.

       Reconciled with MDM_000_MASTER_DATA_CONVENTION_V1 (FROZEN §8):
       - itemType → itemNature (MATERIAL / SEMI_FINISHED / FINISHED_GOOD / SERVICE)
       - inventoryMethod removed (deferred; no business evidence in V1)
       - status default 'active' (only ACTIVE / INACTIVE in V1; no 'draft') -->
  <div class="mdm-list">
    <!-- Toolbar (reused) -->
    <MdmListToolbar
      v-model:search="searchKeyword"
      search-placeholder="搜索物料代码 / 名称 / 规格型号 / 助记码"
      create-label="新建物料"
      @search="applyFilters"
      @create="openCreate"
    >
      <template #filters>
        <el-select v-model="filterCategory" placeholder="分类" clearable filterable class="item-filter-category" @change="applyFilters">
          <el-option
            v-for="c in itemCategories"
            :key="c.id"
            :label="c.fullPath"
            :value="c.id"
          />
        </el-select>
        <el-select v-model="filterType" placeholder="物料性质" clearable class="item-filter-type" @change="applyFilters">
          <el-option v-for="opt in ITEM_NATURE_OPTIONS" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
        <el-select v-model="filterStatus" placeholder="状态" clearable class="item-filter-status" @change="applyFilters">
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
      >
        <el-table-column type="index" label="#" :width="COL.index" fixed="left" />
        <el-table-column prop="code" label="物料代码" :width="COL.code" sortable show-overflow-tooltip>
          <template #default="{ row }">
            <span class="mdm-code">{{ row.code }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="name" label="物料名称" :min-width="COL.nameMin" show-overflow-tooltip sortable />
        <el-table-column prop="specification" label="规格型号" :width="COL.spec" show-overflow-tooltip />
        <el-table-column prop="categoryName" label="分类" :width="COL.category" show-overflow-tooltip>
          <template #default="{ row }">
            {{ row.categoryName || '—' }}
          </template>
        </el-table-column>
        <el-table-column label="基本单位" :width="COL.uom" align="center">
          <template #default="{ row }">{{ findUom(row.baseUomId)?.name || '—' }}</template>
        </el-table-column>
        <!-- GULIERP_ITEM_UI_REUSE_CLOSURE_V1 (2026-08-30) — optional
             mnemonic column. Backend keyword search already covers
             MnemonicCode (case-insensitive upper). The column is
             present so the operator sees the round-trip value; it
             stays at the design-system preset width (110). -->
        <el-table-column prop="mnemonicCode" label="助记码" :width="COL.mnemonic" show-overflow-tooltip>
          <template #default="{ row }">
            <span v-if="row.mnemonicCode" class="mdm-mnemonic">{{ row.mnemonicCode }}</span>
            <span v-else class="mdm-mnemonic-empty">—</span>
          </template>
        </el-table-column>
        <el-table-column prop="itemNature" label="物料性质" :width="COL.itemNature" align="center">
          <template #default="{ row }">
            {{ natureLabel(row.itemNature) }}
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" :width="COL.status" align="center">
          <template #default="{ row }">
            <MdmStatusBadge :status="row.status" />
          </template>
        </el-table-column>
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
      :total="total"
    />
    <div v-if="error && !loading" class="mdm-error-banner">
      <el-alert :title="error" type="error" show-icon :closable="false" />
      <el-button type="primary" link style="margin-left:12px" @click="fetchItems">重新加载</el-button>
    </div>

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
        <el-input v-model="formData.code" placeholder="留空则自动生成 ITEM_000001" :disabled="!!editingId" maxlength="30" />
      </el-form-item>
      <!-- GULIERP_ITEM_UI_REUSE_CLOSURE_V1 (2026-08-30) — MnemonicCode
           field per Common Field Contract §二十八: optional,
           auto-suggested from name via pinyin-pro, manually editable,
           max 40. Server-side
           NullIfEmpty trims and stores null on empty. -->
      <el-form-item label="助记码" prop="mnemonicCode">
        <el-input
          v-model="formData.mnemonicCode"
          placeholder="按物料名称自动建议，可手工覆盖"
          maxlength="40"
          show-word-limit
          clearable
          @input="onMnemonicInput"
        />
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
            v-for="c in itemCategories"
            :key="c.id"
            :label="c.fullPath"
            :value="c.id"
          />
        </el-select>
      </el-form-item>
      <el-form-item label="基本单位" prop="baseUomId">
        <el-select v-model="formData.baseUomId" placeholder="选择计量单位" clearable filterable style="width: 100%">
          <el-option
            v-for="u in activeUoms"
            :key="u.id"
            :label="`${u.name} (${u.code})`"
            :value="u.id"
            :disabled="u.status !== 'active'"
          />
        </el-select>
      </el-form-item>
      <el-form-item label="物料性质" prop="itemNature">
        <el-radio-group v-model="formData.itemNature">
          <el-radio v-for="opt in ITEM_NATURE_OPTIONS" :key="opt.value" :value="opt.value">{{ opt.label }}</el-radio>
        </el-radio-group>
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

    <!-- Detail Drawer with Tabs (reused MdmDetailDrawer + tabbed content) -->
    <MdmDetailDrawer
      v-model="detailDrawerVisible"
      :title="`物料详情 · ${detailData?.code || ''}`"
      @edit="openEditFromDetail"
    >
      <template #header>
        <div class="mdm-detail-title">
          <MdmStatusBadge :status="detailData?.status || 'active'" />
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
            <el-descriptions-item label="助记码">
              {{ detailData?.mnemonicCode || '—' }}
            </el-descriptions-item>
            <el-descriptions-item label="规格型号">{{ detailData?.specification || '—' }}</el-descriptions-item>
            <el-descriptions-item label="物料分类">{{ detailData?.categoryName || '—' }}</el-descriptions-item>
            <el-descriptions-item label="基本单位">{{ findUom(detailData?.baseUomId ?? null)?.name || '—' }}</el-descriptions-item>
            <el-descriptions-item label="物料性质">{{ natureLabel(detailData?.itemNature) }}</el-descriptions-item>
            <el-descriptions-item label="状态">{{ getStatusLabel(detailData?.status) }}</el-descriptions-item>
            <el-descriptions-item label="说明">{{ detailData?.description || '—' }}</el-descriptions-item>
          </el-descriptions>
        </el-tab-pane>

        <el-tab-pane label="库存" name="inventory">
          <el-descriptions :column="2" border size="small">
            <el-descriptions-item label="基本单位">{{ findUom(detailData?.baseUomId ?? null)?.name || '—' }}</el-descriptions-item>
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
import { ref, reactive, computed, onMounted, watch } from 'vue';
import { Download, Box, ShoppingCart, Goods, Collection } from '@element-plus/icons-vue';
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
import * as itemApi from '../../api/mdm/item';
import * as icApi from '../../api/mdm/item-category';
import * as uomApi from '../../api/mdm/uom';
import {
  STATUS_OPTIONS, ITEM_NATURE_OPTIONS,
  statusUiToInt,
} from '../../types/mdm';
import type {
  Item, ItemForm, ItemNature, MasterDataStatus,
  ItemCategoryListItem, Uom,
} from '../../types/mdm';
import { generateMnemonicCode, shouldRefreshMnemonic } from '../../utils/mnemonic';

// ===== Real API state =====
const items = ref<Item[]>([]);
const total = ref(0);
const loading = ref(false);
const error = ref<string | null>(null);
const itemCategories = ref<ItemCategoryListItem[]>([]);
const activeUoms = ref<Uom[]>([]);
const selectorsLoading = ref(false);

const searchKeyword = ref('');
const filterCategory = ref<string | ''>('');
const filterType = ref<ItemNature | ''>('');
const filterStatus = ref<MasterDataStatus | ''>('');
const page = reactive({ current: 1, size: 20 });

/** Look up UOM by id — used in table column + detail drawer. */
function findUom(id: string | null | undefined): Uom | undefined {
  if (id == null) return undefined;
  return activeUoms.value.find(u => u.id === id);
}

async function fetchItems() {
  loading.value = true;
  error.value = null;
  try {
    const natureWire = filterType.value
      ? (filterType.value === 'MATERIAL' ? 1
        : filterType.value === 'SEMI_FINISHED' ? 2
        : filterType.value === 'FINISHED_GOOD' ? 3 : 4) as 1 | 2 | 3 | 4
      : undefined;
    const result = await itemApi.listItems({
      keyword: searchKeyword.value.trim() || undefined,
      status: filterStatus.value ? statusUiToInt(filterStatus.value) : undefined,
      categoryId: filterCategory.value || undefined,
      itemNature: natureWire,
      page: page.current,
      pageSize: page.size,
    });
    // Denormalize categoryName / baseUomName display fields
    items.value = itemApi.joinItemReferences(result.items, itemCategories.value, activeUoms.value);
    total.value = result.totalCount;
  } catch (e) {
    if (e instanceof ApiError) {
      if (e.code === 'authentication_required') throw e;
      const parts = [e.title];
      if (e.detail) parts.push(e.detail);
      if (e.requestId) parts.push(`(RequestId: ${e.requestId})`);
      error.value = parts.join(' — ');
    } else {
      error.value = '加载物料数据失败，请刷新重试';
    }
    items.value = [];
    total.value = 0;
  } finally {
    loading.value = false;
  }
}

async function fetchSelectors() {
  selectorsLoading.value = true;
  try {
    const [cats, uoms] = await Promise.all([
      icApi.listAllCategories(),
      uomApi.listAllUomsActiveOnly(),
    ]);
    itemCategories.value = cats;
    activeUoms.value = uoms;
  } catch (e) {
    if (e instanceof ApiError && e.code === 'authentication_required') throw e;
  } finally {
    selectorsLoading.value = false;
  }
}

onMounted(async () => {
  // Load selectors first, so the first items fetch can denormalize display names.
  await fetchSelectors();
  await fetchItems();
});

const pagedData = computed(() => items.value);

function applyFilters() {
  page.current = 1;
  fetchItems();
}

// ===== Form state =====
const formDrawerVisible = ref(false);
const editingId = ref<string | null>(null);
const editingConcurrency = ref(0);
const submitting = ref(false);
const formData = reactive<ItemForm>({
  code: '', name: '', specification: '', categoryId: null,
  baseUomId: null, itemNature: 'MATERIAL', status: 'active',
  description: '',
  // GULIERP_ITEM_UI_REUSE_CLOSURE_V1 — empty string on Create.
  // The server's NullIfEmpty trims and stores null. The form
  // sends undefined → null when blank.
  mnemonicCode: '',
});
const mnemonicEditedByUser = ref(false);
const lastMnemonicSuggestion = ref('');

const formRules: FormRules<ItemForm> = {
  // GULIERP_ITEM_UI_REUSE_CLOSURE_V1 — Code is OPTIONAL on
  // Create (server auto-generates ITEM_000001 if blank).
  // Required only when explicitly set; we keep the previous
  // required rule disabled so the operator is not blocked from
  // creating with empty Code.
  code: [],
  name: [{ required: true, message: '请输入物料名称', trigger: 'blur' }],
  categoryId: [{ required: true, message: '请选择物料分类', trigger: 'change' }],
  baseUomId: [{ required: true, message: '请选择基本单位', trigger: 'change' }],
};

function resetForm() {
  Object.assign(formData, {
    code: '', name: '', specification: '', categoryId: null,
    baseUomId: null, itemNature: 'MATERIAL', status: 'active',
    description: '',
    mnemonicCode: '',
  });
  mnemonicEditedByUser.value = false;
  lastMnemonicSuggestion.value = '';
}

function openCreate() {
  editingId.value = null;
  editingConcurrency.value = 0;
  resetForm();
  formDrawerVisible.value = true;
}

async function openEdit(row: Item) {
  editingId.value = row.id;
  editingConcurrency.value = row.concurrencyVersion ?? 0;
  try {
    const fresh = await itemApi.getItem(row.id);
    fillForm(fresh);
    editingConcurrency.value = fresh.concurrencyVersion ?? 0;
  } catch (e) {
    fillForm(row);
  }
  formDrawerVisible.value = true;
}

function fillForm(item: Item) {
  const suggestedMnemonic = generateMnemonicCode(item.name);
  Object.assign(formData, {
    code: item.code, name: item.name, specification: item.specification || '',
    categoryId: item.categoryId, baseUomId: item.baseUomId,
    itemNature: item.itemNature, status: item.status,
    description: item.description || '',
    // GULIERP_ITEM_UI_REUSE_CLOSURE_V1 — round-trip the
    // MnemonicCode. The form sends the value back via
    // formToCreate / updateItem (preserves null on blank).
    mnemonicCode: item.mnemonicCode || '',
  });
  lastMnemonicSuggestion.value = suggestedMnemonic;
  mnemonicEditedByUser.value = !!item.mnemonicCode
    && item.mnemonicCode.trim() !== suggestedMnemonic;
}

function isConcurrencyConflict(err: unknown): boolean {
  return err instanceof ApiError && (
    err.code === 'mdm_validation_failed' && /concurrency|version|并发/i.test(err.detail || '')
  );
}

watch(() => formData.name, (next) => {
  const generated = generateMnemonicCode(next);
  if (shouldRefreshMnemonic(formData.mnemonicCode, lastMnemonicSuggestion.value, mnemonicEditedByUser.value)) {
    formData.mnemonicCode = generated;
    lastMnemonicSuggestion.value = generated;
  }
});

function onMnemonicInput(value: string) {
  const trimmed = value.trim();
  if (!trimmed) {
    mnemonicEditedByUser.value = false;
    const generated = generateMnemonicCode(formData.name);
    formData.mnemonicCode = generated;
    lastMnemonicSuggestion.value = generated;
    return;
  }
  mnemonicEditedByUser.value = trimmed !== lastMnemonicSuggestion.value;
}

async function handleSubmit() {
  submitting.value = true;
  try {
    if (editingId.value == null) {
      await itemApi.createItem(formData);
      ElMessage.success('创建物料成功');
    } else {
      await itemApi.updateItem(editingId.value, formData, editingConcurrency.value);
      ElMessage.success('保存修改成功');
    }
    formDrawerVisible.value = false;
    await fetchItems();
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
const detailData = ref<Item | null>(null);
const detailTab = ref('basic');

async function openDetail(row: Item) {
  try {
    const raw = await itemApi.getItem(row.id);
    const [withRefs] = itemApi.joinItemReferences([raw], itemCategories.value, activeUoms.value);
    detailData.value = withRefs;
  } catch (e) {
    detailData.value = row;
  }
  detailTab.value = 'basic';
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
function natureLabel(nature?: ItemNature): string {
  return ITEM_NATURE_OPTIONS.find(o => o.value === nature)?.label || '—';
}

// ===== Status change =====
async function confirmDeactivate(row: Item) {
  try {
    await ElMessageBox.confirm(
      `确定停用"${row.name}"吗？停用后将不能用于新的业务单据，历史数据不受影响。`,
      '停用确认',
      { confirmButtonText: '停用', cancelButtonText: '取消', type: 'warning' },
    );
  } catch { return; }
  try {
    await itemApi.setItemStatus(row, 'inactive');
    ElMessage.success('已停用');
    await fetchItems();
  } catch (e) {
    if (isConcurrencyConflict(e)) ElMessage.warning('并发冲突：数据已被其他用户修改，请刷新后重试');
    else if (e instanceof ApiError) ElMessage.error(`${e.title}${e.detail ? ' — ' + e.detail : ''}`);
    else ElMessage.error('停用失败');
  }
}

async function confirmActivate(row: Item) {
  try {
    await ElMessageBox.confirm(`确定启用"${row.name}"吗？`, '启用确认',
      { confirmButtonText: '启用', cancelButtonText: '取消', type: 'info' });
  } catch { return; }
  try {
    await itemApi.setItemStatus(row, 'active');
    ElMessage.success('已启用');
    await fetchItems();
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
   Page-specific accents stay here (ItemList has extra spec / tabs). */
.mdm-detail-spec {
  color: var(--text-muted);
  font-size: 13px;
}
.item-filter-category {
  width: 150px;
}
.item-filter-type {
  width: var(--col-category);
}
.item-filter-status {
  width: var(--col-status);
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
  color: var(--text-muted);
}
.mdm-tab-placeholder p {
  font-size: 13px;
}
/* GULIERP_ITEM_UI_REUSE_CLOSURE_V1 (2026-08-30) — MnemonicCode
   list column rendering. Empty placeholder uses muted color
   so the column doesn't visually shout on rows without a
   mnemonic. */
.mdm-mnemonic {
  font-family: var(--font-mono, 'Consolas', 'Menlo', monospace);
  font-size: 12px;
  color: var(--text-default);
}
.mdm-mnemonic-empty {
  color: var(--text-muted);
  font-size: 12px;
}
</style>
