<template>
  <div class="mdm-dictionary-page">
    <section class="mdm-dictionary-panel mdm-dictionary-panel--types">
      <MdmListToolbar
        v-model:search="typeKeyword"
        search-placeholder="搜索类型代码 / 名称"
        create-label="新建字典类型"
        @search="applyTypeFilters"
        @create="openCreateType"
      >
        <template #filters>
          <el-select v-model="typeStatus" placeholder="状态" clearable style="width: 100px" @change="applyTypeFilters">
            <el-option v-for="opt in STATUS_OPTIONS" :key="opt.value" :label="opt.label" :value="opt.value" />
          </el-select>
        </template>
      </MdmListToolbar>

      <div class="gs-list-wrap mdm-dictionary-types-wrap">
        <el-table
          v-loading="typeLoading"
          :data="dictionaryTypes"
          border
          stripe
          height="100%"
          size="small"
          row-key="id"
          highlight-current-row
          :header-cell-style="{ padding: '0 8px' }"
          :cell-style="{ padding: '0 8px' }"
          @current-change="selectType"
          @row-dblclick="openEditType"
        >
          <el-table-column prop="code" label="类型代码" width="130" show-overflow-tooltip>
            <template #default="{ row }">
              <span class="mdm-code">{{ row.code }}</span>
            </template>
          </el-table-column>
          <el-table-column prop="name" label="类型名称" min-width="95" show-overflow-tooltip />
          <el-table-column prop="status" label="状态" width="65" align="center">
            <template #default="{ row }">
              <MdmStatusBadge :status="row.status" />
            </template>
          </el-table-column>
          <el-table-column prop="isSystem" label="系统" width="60" align="center">
            <template #default="{ row }">
              <el-tag size="small" :type="row.isSystem ? 'warning' : 'info'" effect="plain">
                {{ row.isSystem ? '是' : '否' }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="sortOrder" label="排序" width="65" align="right" />
          <el-table-column label="操作" width="120" fixed="right" align="center">
            <template #default="{ row }">
              <div class="mdm-row-actions">
                <el-button text size="small" type="primary" :disabled="row.isSystem" @click.stop="openEditType(row)">
                  编辑
                </el-button>
                <span class="mdm-action-sep">|</span>
                <el-button
                  v-if="row.status === 'active'"
                  text
                  size="small"
                  type="warning"
                  :disabled="row.isSystem"
                  @click.stop="confirmTypeStatus(row, 'inactive')"
                >停用</el-button>
                <el-button
                  v-else
                  text
                  size="small"
                  type="success"
                  :disabled="row.isSystem"
                  @click.stop="confirmTypeStatus(row, 'active')"
                >启用</el-button>
              </div>
            </template>
          </el-table-column>
          <template #empty>
            <MdmEmptyState message="暂无基础字典类型" create-label="新建字典类型" @create="openCreateType" />
          </template>
        </el-table>
      </div>

      <MdmPagination
        v-model:current-page="typePage.current"
        v-model:page-size="typePage.size"
        :total="typeTotal"
      />

      <div v-if="typeError && !typeLoading" class="mdm-error-banner">
        <el-alert :title="typeError" type="error" show-icon :closable="false" />
        <el-button type="primary" link style="margin-left:12px" @click="fetchTypes">重新加载</el-button>
      </div>
    </section>

    <section class="mdm-dictionary-panel mdm-dictionary-panel--items">
      <header class="mdm-dictionary-items-header">
        <div>
          <h2>{{ selectedType?.name || '字典项' }}</h2>
          <p>{{ selectedType ? selectedType.code : '请选择左侧字典类型' }}</p>
        </div>
        <el-tag v-if="selectedType?.isSystem" type="warning" effect="plain">系统字典</el-tag>
      </header>

      <MdmListToolbar
        v-model:search="itemKeyword"
        search-placeholder="搜索项代码 / 名称 / 值"
        create-label="新建字典项"
        @search="applyItemFilters"
        @create="openCreateItem"
      >
        <template #filters>
          <el-select v-model="itemStatus" placeholder="状态" clearable style="width: 100px" @change="applyItemFilters">
            <el-option v-for="opt in STATUS_OPTIONS" :key="opt.value" :label="opt.label" :value="opt.value" />
          </el-select>
        </template>
      </MdmListToolbar>

      <div class="gs-list-wrap mdm-dictionary-items-wrap">
        <el-table
          v-loading="itemLoading"
          :data="dictionaryItems"
          border
          stripe
          height="100%"
          size="small"
          row-key="id"
          :header-cell-style="{ padding: '0 8px' }"
          :cell-style="{ padding: '0 8px' }"
          @row-dblclick="openEditItem"
        >
          <el-table-column prop="code" label="项代码" width="150" show-overflow-tooltip>
            <template #default="{ row }">
              <span class="mdm-code">{{ row.code }}</span>
            </template>
          </el-table-column>
          <el-table-column prop="name" label="项名称" min-width="90" show-overflow-tooltip />
          <el-table-column prop="value" label="值" min-width="95" show-overflow-tooltip />
          <el-table-column prop="status" label="状态" width="65" align="center">
            <template #default="{ row }">
              <MdmStatusBadge :status="row.status" />
            </template>
          </el-table-column>
          <el-table-column prop="isDefault" label="默认" width="60" align="center">
            <template #default="{ row }">
              <el-tag v-if="row.isDefault" size="small" type="success" effect="plain">默认</el-tag>
              <span v-else>—</span>
            </template>
          </el-table-column>
          <el-table-column prop="isSystem" label="系统" width="60" align="center">
            <template #default="{ row }">
              <el-tag size="small" :type="row.isSystem ? 'warning' : 'info'" effect="plain">
                {{ row.isSystem ? '是' : '否' }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="sortOrder" label="排序" width="65" align="right" />
          <el-table-column prop="description" label="说明" min-width="100" show-overflow-tooltip />
          <el-table-column label="操作" width="120" fixed="right" align="center">
            <template #default="{ row }">
              <div class="mdm-row-actions">
                <el-button text size="small" type="primary" :disabled="row.isSystem" @click.stop="openEditItem(row)">
                  编辑
                </el-button>
                <span class="mdm-action-sep">|</span>
                <el-button
                  v-if="row.status === 'active'"
                  text
                  size="small"
                  type="warning"
                  :disabled="row.isSystem"
                  @click.stop="confirmItemStatus(row, 'inactive')"
                >停用</el-button>
                <el-button
                  v-else
                  text
                  size="small"
                  type="success"
                  :disabled="row.isSystem"
                  @click.stop="confirmItemStatus(row, 'active')"
                >启用</el-button>
              </div>
            </template>
          </el-table-column>
          <template #empty>
            <MdmEmptyState
              :message="selectedType ? '暂无字典项数据' : '请选择字典类型'"
              create-label="新建字典项"
              @create="openCreateItem"
            />
          </template>
        </el-table>
      </div>

      <MdmPagination
        v-model:current-page="itemPage.current"
        v-model:page-size="itemPage.size"
        :total="itemTotal"
      />

      <div v-if="itemError && !itemLoading" class="mdm-error-banner">
        <el-alert :title="itemError" type="error" show-icon :closable="false" />
        <el-button type="primary" link style="margin-left:12px" @click="fetchItems">重新加载</el-button>
      </div>
    </section>

    <MdmFormDrawer
      v-model="typeDrawerVisible"
      v-model:model="typeForm"
      :title="editingTypeId ? '编辑字典类型' : '新建字典类型'"
      :rules="typeRules"
      :loading="typeSubmitting"
      :submit-label="editingTypeId ? '保存修改' : '创建'"
      @submit="submitType"
    >
      <el-form-item label="类型代码" prop="code">
        <el-input v-model="typeForm.code" placeholder="如 ORDER_STATUS" :disabled="!!editingTypeId" maxlength="40" />
      </el-form-item>
      <el-form-item label="类型名称" prop="name">
        <el-input v-model="typeForm.name" placeholder="如 订单状态" maxlength="200" />
      </el-form-item>
      <el-form-item label="状态" prop="status">
        <el-select v-model="typeForm.status" style="width: 100%">
          <el-option v-for="opt in STATUS_OPTIONS" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
      </el-form-item>
      <el-form-item label="排序" prop="sortOrder">
        <el-input-number v-model="typeForm.sortOrder" :min="0" :max="999999" style="width: 100%" />
      </el-form-item>
      <el-form-item label="系统项">
        <el-tag :type="editingTypeIsSystem ? 'warning' : 'info'" effect="plain">
          {{ editingTypeIsSystem ? '是' : '否' }}
        </el-tag>
      </el-form-item>
      <el-form-item label="说明">
        <el-input v-model="typeForm.description" type="textarea" :rows="3" maxlength="500" show-word-limit />
      </el-form-item>
    </MdmFormDrawer>

    <MdmFormDrawer
      v-model="itemDrawerVisible"
      v-model:model="itemForm"
      :title="editingItemId ? '编辑字典项' : '新建字典项'"
      :rules="itemRules"
      :loading="itemSubmitting"
      :submit-label="editingItemId ? '保存修改' : '创建'"
      @submit="submitItem"
    >
      <el-form-item label="所属类型">
        <el-input :model-value="selectedType?.name || '未选择'" disabled />
      </el-form-item>
      <el-form-item label="项代码" prop="code">
        <el-input v-model="itemForm.code" placeholder="如 APPROVED" :disabled="!!editingItemId" maxlength="40" />
      </el-form-item>
      <el-form-item label="项名称" prop="name">
        <el-input v-model="itemForm.name" placeholder="如 已审核" maxlength="200" />
      </el-form-item>
      <el-form-item label="值" prop="value">
        <el-input v-model="itemForm.value" placeholder="用于业务保存或展示的值" maxlength="200" />
      </el-form-item>
      <el-form-item label="状态" prop="status">
        <el-select v-model="itemForm.status" style="width: 100%">
          <el-option v-for="opt in STATUS_OPTIONS" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
      </el-form-item>
      <el-form-item label="排序" prop="sortOrder">
        <el-input-number v-model="itemForm.sortOrder" :min="0" :max="999999" style="width: 100%" />
      </el-form-item>
      <el-form-item label="默认项">
        <el-switch v-model="itemForm.isDefault" active-text="是" inactive-text="否" />
      </el-form-item>
      <el-form-item label="系统项">
        <el-tag :type="editingItemIsSystem ? 'warning' : 'info'" effect="plain">
          {{ editingItemIsSystem ? '是' : '否' }}
        </el-tag>
      </el-form-item>
      <el-form-item label="说明">
        <el-input v-model="itemForm.description" type="textarea" :rows="3" maxlength="500" show-word-limit />
      </el-form-item>
    </MdmFormDrawer>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import type { FormRules } from 'element-plus';

import MdmEmptyState from '../../components/mdm/MdmEmptyState.vue';
import MdmFormDrawer from '../../components/mdm/MdmFormDrawer.vue';
import MdmListToolbar from '../../components/mdm/MdmListToolbar.vue';
import MdmPagination from '../../components/mdm/MdmPagination.vue';
import MdmStatusBadge from '../../components/mdm/MdmStatusBadge.vue';
import { TABLE_COLUMN_PRESETS as COL } from '../../design-system/tableColumns';
import { ApiError } from '../../api/http';
import * as dictionaryApi from '../../api/mdm/dictionary';
import { STATUS_OPTIONS, statusUiToInt } from '../../types/mdm';
import type { MasterDataStatus } from '../../types/mdm';
import type {
  DictionaryItem,
  DictionaryItemForm,
  DictionaryType,
  DictionaryTypeForm,
} from '../../api/mdm/dictionary';

const dictionaryTypes = ref<DictionaryType[]>([]);
const selectedTypeId = ref<string | null>(null);
const typeTotal = ref(0);
const typeLoading = ref(false);
const typeError = ref<string | null>(null);
const typeKeyword = ref('');
const typeStatus = ref<MasterDataStatus | ''>('');
const typePage = reactive({ current: 1, size: 20 });

const dictionaryItems = ref<DictionaryItem[]>([]);
const itemTotal = ref(0);
const itemLoading = ref(false);
const itemError = ref<string | null>(null);
const itemKeyword = ref('');
const itemStatus = ref<MasterDataStatus | ''>('');
const itemPage = reactive({ current: 1, size: 20 });

const selectedType = computed(() =>
  dictionaryTypes.value.find(item => item.id === selectedTypeId.value) || null);

function apiErrorMessage(e: unknown, fallback: string): string {
  if (e instanceof ApiError) {
    if (e.code === 'authentication_required') throw e;
    const parts = [e.title];
    if (e.detail) parts.push(e.detail);
    if (e.requestId) parts.push(`(RequestId: ${e.requestId})`);
    return parts.join(' — ');
  }
  return fallback;
}

function isConcurrencyConflict(err: unknown): boolean {
  return err instanceof ApiError && (
    err.code === 'mdm_validation_failed' && /concurrency|version|并发/i.test(err.detail || '')
  );
}

async function fetchTypes() {
  typeLoading.value = true;
  typeError.value = null;
  try {
    const result = await dictionaryApi.listDictionaryTypes({
      keyword: typeKeyword.value.trim() || undefined,
      status: typeStatus.value ? statusUiToInt(typeStatus.value) : undefined,
      page: typePage.current,
      pageSize: typePage.size,
    });
    dictionaryTypes.value = result.items;
    typeTotal.value = result.totalCount;
    if (selectedTypeId.value && !result.items.some(item => item.id === selectedTypeId.value)) {
      selectedTypeId.value = result.items[0]?.id ?? null;
    } else if (!selectedTypeId.value && result.items.length > 0) {
      selectedTypeId.value = result.items[0].id;
    }
  } catch (e) {
    typeError.value = apiErrorMessage(e, '加载字典类型失败，请刷新重试');
    dictionaryTypes.value = [];
    typeTotal.value = 0;
    selectedTypeId.value = null;
  } finally {
    typeLoading.value = false;
  }
}

async function fetchItems() {
  if (!selectedTypeId.value) {
    dictionaryItems.value = [];
    itemTotal.value = 0;
    itemError.value = null;
    return;
  }
  itemLoading.value = true;
  itemError.value = null;
  try {
    const result = await dictionaryApi.listDictionaryItems(selectedTypeId.value, {
      keyword: itemKeyword.value.trim() || undefined,
      status: itemStatus.value ? statusUiToInt(itemStatus.value) : undefined,
      page: itemPage.current,
      pageSize: itemPage.size,
    });
    dictionaryItems.value = result.items;
    itemTotal.value = result.totalCount;
  } catch (e) {
    itemError.value = apiErrorMessage(e, '加载字典项失败，请刷新重试');
    dictionaryItems.value = [];
    itemTotal.value = 0;
  } finally {
    itemLoading.value = false;
  }
}

onMounted(fetchTypes);

watch(() => [typePage.current, typePage.size], fetchTypes);
watch(() => [itemPage.current, itemPage.size], fetchItems);
watch(selectedTypeId, () => {
  itemPage.current = 1;
  fetchItems();
});

function applyTypeFilters() {
  typePage.current = 1;
  fetchTypes();
}

function applyItemFilters() {
  itemPage.current = 1;
  fetchItems();
}

function selectType(row: DictionaryType | null) {
  selectedTypeId.value = row?.id ?? null;
}

const typeDrawerVisible = ref(false);
const editingTypeId = ref<string | null>(null);
const editingTypeConcurrency = ref(0);
const editingTypeIsSystem = ref(false);
const typeSubmitting = ref(false);
const typeForm = reactive<DictionaryTypeForm>({
  code: '',
  name: '',
  description: '',
  status: 'active',
  sortOrder: 0,
});

const typeRules: FormRules = {
  code: [{ required: true, message: '请输入字典类型代码', trigger: 'blur' }],
  name: [{ required: true, message: '请输入字典类型名称', trigger: 'blur' }],
  status: [{ required: true, message: '请选择状态', trigger: 'change' }],
  sortOrder: [{ required: true, message: '请输入排序号', trigger: 'change' }],
};

function resetTypeForm() {
  Object.assign(typeForm, {
    code: '',
    name: '',
    description: '',
    status: 'active',
    sortOrder: 0,
  });
}

function openCreateType() {
  editingTypeId.value = null;
  editingTypeConcurrency.value = 0;
  editingTypeIsSystem.value = false;
  resetTypeForm();
  typeDrawerVisible.value = true;
}

async function openEditType(row: DictionaryType) {
  if (row.isSystem) {
    ElMessage.warning('系统字典类型由系统维护，不能在页面中编辑');
    return;
  }
  editingTypeId.value = row.id;
  editingTypeConcurrency.value = row.concurrencyVersion;
  editingTypeIsSystem.value = row.isSystem;
  try {
    const fresh = await dictionaryApi.getDictionaryType(row.id);
    Object.assign(typeForm, {
      code: fresh.code,
      name: fresh.name,
      description: fresh.description || '',
      status: fresh.status,
      sortOrder: fresh.sortOrder,
    });
    editingTypeConcurrency.value = fresh.concurrencyVersion;
    editingTypeIsSystem.value = fresh.isSystem;
  } catch (e) {
    Object.assign(typeForm, {
      code: row.code,
      name: row.name,
      description: row.description || '',
      status: row.status,
      sortOrder: row.sortOrder,
    });
  }
  typeDrawerVisible.value = true;
}

async function submitType() {
  if (!typeForm.code.trim() || !typeForm.name.trim()) {
    ElMessage.warning('请填写字典类型代码和名称');
    return;
  }
  typeSubmitting.value = true;
  try {
    if (editingTypeId.value == null) {
      const created = await dictionaryApi.createDictionaryType(typeForm);
      selectedTypeId.value = created.id;
      ElMessage.success('创建字典类型成功');
    } else {
      await dictionaryApi.updateDictionaryType(editingTypeId.value, typeForm, editingTypeConcurrency.value);
      ElMessage.success('保存字典类型成功');
    }
    typeDrawerVisible.value = false;
    await fetchTypes();
  } catch (e) {
    if (isConcurrencyConflict(e)) {
      ElMessage.warning('并发冲突：数据已被其他用户修改，请刷新后重试');
    } else {
      ElMessage.error(apiErrorMessage(e, '保存字典类型失败，请重试'));
    }
  } finally {
    typeSubmitting.value = false;
  }
}

async function confirmTypeStatus(row: DictionaryType, target: MasterDataStatus) {
  if (row.isSystem) {
    ElMessage.warning('系统字典类型受保护，不能在页面中启停');
    return;
  }
  const action = target === 'active' ? '启用' : '停用';
  try {
    await ElMessageBox.confirm(`确定${action}"${row.name}"吗？`, `${action}确认`, {
      confirmButtonText: action,
      cancelButtonText: '取消',
      type: target === 'active' ? 'info' : 'warning',
    });
  } catch { return; }
  try {
    await dictionaryApi.setDictionaryTypeStatus(row, target);
    ElMessage.success(`已${action}`);
    await fetchTypes();
  } catch (e) {
    if (isConcurrencyConflict(e)) {
      ElMessage.warning('并发冲突：数据已被其他用户修改，请刷新后重试');
    } else {
      ElMessage.error(apiErrorMessage(e, `${action}字典类型失败`));
    }
  }
}

const itemDrawerVisible = ref(false);
const editingItemId = ref<string | null>(null);
const editingItemConcurrency = ref(0);
const editingItemIsSystem = ref(false);
const itemSubmitting = ref(false);
const itemForm = reactive<DictionaryItemForm>({
  code: '',
  name: '',
  value: '',
  description: '',
  status: 'active',
  sortOrder: 0,
  isDefault: false,
});

const itemRules: FormRules = {
  code: [{ required: true, message: '请输入字典项代码', trigger: 'blur' }],
  name: [{ required: true, message: '请输入字典项名称', trigger: 'blur' }],
  value: [{ required: true, message: '请输入字典项值', trigger: 'blur' }],
  status: [{ required: true, message: '请选择状态', trigger: 'change' }],
  sortOrder: [{ required: true, message: '请输入排序号', trigger: 'change' }],
};

function resetItemForm() {
  Object.assign(itemForm, {
    code: '',
    name: '',
    value: '',
    description: '',
    status: 'active',
    sortOrder: 0,
    isDefault: false,
  });
}

function openCreateItem() {
  if (!selectedTypeId.value) {
    ElMessage.warning('请先选择字典类型');
    return;
  }
  editingItemId.value = null;
  editingItemConcurrency.value = 0;
  editingItemIsSystem.value = false;
  resetItemForm();
  itemDrawerVisible.value = true;
}

async function openEditItem(row: DictionaryItem) {
  if (row.isSystem) {
    ElMessage.warning('系统字典项由系统维护，不能在页面中编辑');
    return;
  }
  editingItemId.value = row.id;
  editingItemConcurrency.value = row.concurrencyVersion;
  editingItemIsSystem.value = row.isSystem;
  try {
    const fresh = await dictionaryApi.getDictionaryItem(row.id);
    Object.assign(itemForm, {
      code: fresh.code,
      name: fresh.name,
      value: fresh.value,
      description: fresh.description || '',
      status: fresh.status,
      sortOrder: fresh.sortOrder,
      isDefault: fresh.isDefault,
    });
    editingItemConcurrency.value = fresh.concurrencyVersion;
    editingItemIsSystem.value = fresh.isSystem;
  } catch (e) {
    Object.assign(itemForm, {
      code: row.code,
      name: row.name,
      value: row.value,
      description: row.description || '',
      status: row.status,
      sortOrder: row.sortOrder,
      isDefault: row.isDefault,
    });
  }
  itemDrawerVisible.value = true;
}

async function submitItem() {
  if (!selectedTypeId.value) {
    ElMessage.warning('请先选择字典类型');
    return;
  }
  if (!itemForm.code.trim() || !itemForm.name.trim() || !itemForm.value.trim()) {
    ElMessage.warning('请填写字典项代码、名称和值');
    return;
  }
  itemSubmitting.value = true;
  try {
    if (editingItemId.value == null) {
      await dictionaryApi.createDictionaryItem(selectedTypeId.value, itemForm);
      ElMessage.success('创建字典项成功');
    } else {
      await dictionaryApi.updateDictionaryItem(editingItemId.value, itemForm, editingItemConcurrency.value);
      ElMessage.success('保存字典项成功');
    }
    itemDrawerVisible.value = false;
    await fetchItems();
  } catch (e) {
    if (isConcurrencyConflict(e)) {
      ElMessage.warning('并发冲突：数据已被其他用户修改，请刷新后重试');
    } else {
      ElMessage.error(apiErrorMessage(e, '保存字典项失败，请重试'));
    }
  } finally {
    itemSubmitting.value = false;
  }
}

async function confirmItemStatus(row: DictionaryItem, target: MasterDataStatus) {
  if (row.isSystem) {
    ElMessage.warning('系统字典项受保护，不能在页面中启停');
    return;
  }
  const action = target === 'active' ? '启用' : '停用';
  try {
    await ElMessageBox.confirm(`确定${action}"${row.name}"吗？`, `${action}确认`, {
      confirmButtonText: action,
      cancelButtonText: '取消',
      type: target === 'active' ? 'info' : 'warning',
    });
  } catch { return; }
  try {
    await dictionaryApi.setDictionaryItemStatus(row, target);
    ElMessage.success(`已${action}`);
    await fetchItems();
  } catch (e) {
    if (isConcurrencyConflict(e)) {
      ElMessage.warning('并发冲突：数据已被其他用户修改，请刷新后重试');
    } else {
      ElMessage.error(apiErrorMessage(e, `${action}字典项失败`));
    }
  }
}
</script>

<style scoped>
.mdm-dictionary-page {
  display: grid;
  /* GULIERP_MDM_UI_FINAL_AUDIT_V1 (2026-09-02) — per brief §7
     the left types panel needs enough room for the 6-column
     table (code+name+status+isSystem+sortOrder+actions ≈ 710+).
     Bumped min from 360 → 420 and the ratio to 1fr:1.5fr so
     the type names are not truncated at 1366px screens. The
     right items panel still dominates at wider viewports. */
  grid-template-columns: minmax(420px, 1fr) minmax(520px, 1.5fr);
  gap: 12px;
  min-height: 100%;
  padding: 16px;
  background: var(--bg-page);
}

.mdm-dictionary-panel {
  display: flex;
  min-width: 0;
  min-height: calc(100vh - 128px);
  flex-direction: column;
  gap: 10px;
  padding: 12px;
  background: var(--bg-container);
  border: 1px solid var(--border-subtle);
  border-radius: 8px;
}

.mdm-dictionary-types-wrap,
.mdm-dictionary-items-wrap {
  min-height: 420px;
}

.mdm-dictionary-items-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  min-height: 48px;
  padding: 0 2px 8px;
  border-bottom: 1px solid var(--border-subtle);
}

.mdm-dictionary-items-header h2 {
  margin: 0;
  color: var(--text-primary);
  font-size: 18px;
  font-weight: 600;
  line-height: 1.35;
}

.mdm-dictionary-items-header p {
  margin: 4px 0 0;
  color: var(--text-secondary);
  font-size: 13px;
  line-height: 1.4;
}

@media (max-width: 1100px) {
  .mdm-dictionary-page {
    grid-template-columns: 1fr;
  }

  .mdm-dictionary-panel {
    min-height: 520px;
  }
}

/* GULIERP_MDM_DICTIONARY_UI_FIX_01 (2026-09-02) — local
   header cell nowrap. Element Plus .cell allows wrap by default;
   "显示顺序" (4 hanzi) at 75px would otherwise be wrapped to two
   lines. Scoped to .mdm-dictionary-page so other list pages are
   unaffected. */
.mdm-dictionary-page :deep(.el-table th > .cell) {
  white-space: nowrap;
}

.mdm-dictionary-page :deep(.el-table td > .cell) {
  white-space: nowrap;
}

/* Action column: keep the 编辑 | 停用 cluster on one line even
   when the button is long. Local to Dictionary. */
.mdm-dictionary-page :deep(.mdm-row-actions) {
  white-space: nowrap;
}
</style>
