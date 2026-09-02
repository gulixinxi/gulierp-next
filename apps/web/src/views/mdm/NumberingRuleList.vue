<template>
  <div class="mdm-list">
    <MdmListToolbar
      v-model:search="searchKeyword"
      search-placeholder="搜索单据类型 / 前缀"
      create-label="新建编号规则"
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

    <div class="gs-list-wrap">
      <el-table
        v-loading="loading"
        :data="pagedData"
        border
        stripe
        height="100%"
        size="small"
        row-key="id"
        :header-cell-style="{ padding: '0 8px' }"
        :cell-style="{ padding: '0 8px' }"
      >
        <el-table-column type="index" label="#" :width="COL.index" fixed="left" />
        <el-table-column prop="documentType" label="编号规则" width="140" sortable show-overflow-tooltip>
          <template #default="{ row }">
            <span class="mdm-code">{{ row.documentType }}</span>
          </template>
        </el-table-column>
        <el-table-column label="中文名称" width="110" show-overflow-tooltip>
          <template #default="{ row }">
            {{ documentTypeLabel(row.documentType) }}
          </template>
        </el-table-column>
        <el-table-column prop="prefix" label="Prefix" width="100" show-overflow-tooltip />
        <el-table-column prop="datePattern" label="DatePattern" min-width="120" show-overflow-tooltip />
        <el-table-column prop="sequenceLength" label="SequenceLength" width="110" align="center" />
        <el-table-column prop="resetMode" label="ResetMode" width="100" align="center">
          <template #default="{ row }">
            {{ resetModeLabel(row.resetMode) }}
          </template>
        </el-table-column>
        <el-table-column prop="status" label="Status" width="75" align="center">
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
            message="暂无编号规则数据"
            create-label="新建编号规则"
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
      <el-button type="primary" link style="margin-left:12px" @click="fetchNumberingRules">重新加载</el-button>
    </div>

    <MdmFormDrawer
      v-model="formDrawerVisible"
      v-model:model="formData"
      :title="editingId ? '编辑编号规则' : '新建编号规则'"
      :rules="formRules"
      :loading="submitting"
      :submit-label="editingId ? '保存修改' : '创建'"
      @submit="handleSubmit"
    >
      <el-form-item label="DocumentType" prop="documentType">
        <el-input
          v-model="formData.documentType"
          placeholder="如 SALES_ORDER"
          :disabled="!!editingId"
          maxlength="64"
        />
      </el-form-item>
      <el-form-item label="Prefix" prop="prefix">
        <el-input v-model="formData.prefix" placeholder="如 SO" maxlength="16" />
      </el-form-item>
      <el-form-item label="DatePattern" prop="datePattern">
        <el-select
          v-model="formData.datePattern"
          filterable
          allow-create
          default-first-option
          style="width: 100%"
        >
          <el-option v-for="opt in DATE_PATTERN_OPTIONS" :key="opt" :label="opt" :value="opt" />
        </el-select>
      </el-form-item>
      <el-form-item label="SequenceLength" prop="sequenceLength">
        <el-input-number v-model="formData.sequenceLength" :min="1" :max="12" :step="1" style="width: 100%" />
      </el-form-item>
      <el-form-item label="ResetMode" prop="resetMode">
        <el-select v-model="formData.resetMode" style="width: 100%">
          <el-option
            v-for="opt in NUMBERING_RULE_RESET_MODE_OPTIONS"
            :key="opt.value"
            :label="opt.label"
            :value="opt.value"
          />
        </el-select>
      </el-form-item>
      <el-form-item label="Status" prop="status">
        <el-select v-model="formData.status" style="width: 100%">
          <el-option v-for="opt in STATUS_OPTIONS" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
      </el-form-item>
    </MdmFormDrawer>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue';
import { Download } from '@element-plus/icons-vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import type { FormRules } from 'element-plus';

import MdmListToolbar from '../../components/mdm/MdmListToolbar.vue';
import MdmStatusBadge from '../../components/mdm/MdmStatusBadge.vue';
import MdmFormDrawer from '../../components/mdm/MdmFormDrawer.vue';
import MdmPagination from '../../components/mdm/MdmPagination.vue';
import MdmEmptyState from '../../components/mdm/MdmEmptyState.vue';
import MdmTableRowActions from '../../components/mdm/MdmTableRowActions.vue';
import { TABLE_COLUMN_PRESETS as COL } from '../../design-system/tableColumns';

import { ApiError } from '../../api/http';
import * as numberingRuleApi from '../../api/mdm/numberingRule';
import { STATUS_OPTIONS, statusUiToInt } from '../../types/mdm';
import type { MasterDataStatus } from '../../types/mdm';
import type {
  NumberingRule,
  NumberingRuleForm,
  NumberingRuleResetMode,
} from '../../api/mdm/numberingRule';
import { NUMBERING_RULE_RESET_MODE_OPTIONS } from '../../api/mdm/numberingRule';

const DATE_PATTERN_OPTIONS = ['YYYYMMDD', 'YYYYMM', 'YYYY'];

const numberingRules = ref<NumberingRule[]>([]);
const total = ref(0);
const loading = ref(false);
const error = ref<string | null>(null);

const searchKeyword = ref('');
const filterStatus = ref<MasterDataStatus | ''>('');
const page = reactive({ current: 1, size: 20 });

async function fetchNumberingRules() {
  loading.value = true;
  error.value = null;
  try {
    const result = await numberingRuleApi.listNumberingRules({
      keyword: searchKeyword.value.trim() || undefined,
      status: filterStatus.value ? statusUiToInt(filterStatus.value) : undefined,
      page: page.current,
      pageSize: page.size,
    });
    numberingRules.value = result.items;
    total.value = result.totalCount;
  } catch (e) {
    if (e instanceof ApiError) {
      if (e.code === 'authentication_required') throw e;
      const parts = [e.title];
      if (e.detail) parts.push(e.detail);
      if (e.requestId) parts.push(`(RequestId: ${e.requestId})`);
      error.value = parts.join(' — ');
    } else {
      error.value = '加载编号规则失败，请刷新重试';
    }
    numberingRules.value = [];
    total.value = 0;
  } finally {
    loading.value = false;
  }
}

onMounted(fetchNumberingRules);
watch(() => [page.current, page.size], fetchNumberingRules);

const pagedData = computed(() => numberingRules.value);

function applyFilters() {
  if (page.current === 1) {
    fetchNumberingRules();
  } else {
    page.current = 1;
  }
}

const formDrawerVisible = ref(false);
const editingId = ref<string | null>(null);
const editingConcurrency = ref(0);
const submitting = ref(false);
const formData = reactive<NumberingRuleForm>({
  documentType: '',
  prefix: '',
  datePattern: 'YYYYMMDD',
  sequenceLength: 6,
  resetMode: 'DAILY',
  status: 'active',
});

const formRules: FormRules = {
  documentType: [{ required: true, message: '请输入 DocumentType', trigger: 'blur' }],
  prefix: [
    { required: true, message: '请输入 Prefix', trigger: 'blur' },
    { pattern: /^[A-Za-z]+$/, message: 'Prefix 仅允许英文字母', trigger: 'blur' },
  ],
  datePattern: [{ required: true, message: '请输入 DatePattern', trigger: 'change' }],
  sequenceLength: [{ required: true, message: '请输入 SequenceLength', trigger: 'change' }],
  resetMode: [{ required: true, message: '请选择 ResetMode', trigger: 'change' }],
  status: [{ required: true, message: '请选择状态', trigger: 'change' }],
};

function resetForm() {
  Object.assign(formData, {
    documentType: '',
    prefix: '',
    datePattern: 'YYYYMMDD',
    sequenceLength: 6,
    resetMode: 'DAILY',
    status: 'active',
  });
}

function openCreate() {
  editingId.value = null;
  editingConcurrency.value = 0;
  resetForm();
  formDrawerVisible.value = true;
}

async function openEdit(row: NumberingRule) {
  editingId.value = row.id;
  editingConcurrency.value = row.concurrencyVersion ?? 0;
  try {
    const fresh = await numberingRuleApi.getNumberingRule(row.id);
    Object.assign(formData, {
      documentType: fresh.documentType,
      prefix: fresh.prefix,
      datePattern: fresh.datePattern,
      sequenceLength: fresh.sequenceLength,
      resetMode: fresh.resetMode,
      status: fresh.status,
    });
    editingConcurrency.value = fresh.concurrencyVersion ?? 0;
  } catch (e) {
    Object.assign(formData, {
      documentType: row.documentType,
      prefix: row.prefix,
      datePattern: row.datePattern,
      sequenceLength: row.sequenceLength,
      resetMode: row.resetMode,
      status: row.status,
    });
    if (e instanceof ApiError && e.code !== 'authentication_required') {
      ElMessage.warning('读取最新详情失败，显示列表快照');
    }
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
      await numberingRuleApi.createNumberingRule(formData);
      ElMessage.success('创建编号规则成功');
    } else {
      await numberingRuleApi.updateNumberingRule(editingId.value, formData, editingConcurrency.value);
      ElMessage.success('保存修改成功');
    }
    formDrawerVisible.value = false;
    await fetchNumberingRules();
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

async function confirmDeactivate(row: NumberingRule) {
  try {
    await ElMessageBox.confirm(
      `确定停用"${row.documentType}"编号规则吗？`,
      '停用确认',
      { confirmButtonText: '停用', cancelButtonText: '取消', type: 'warning' },
    );
  } catch { return; }
  await changeStatus(row, 'inactive');
}

async function confirmActivate(row: NumberingRule) {
  try {
    await ElMessageBox.confirm(
      `确定启用"${row.documentType}"编号规则吗？`,
      '启用确认',
      { confirmButtonText: '启用', cancelButtonText: '取消', type: 'info' },
    );
  } catch { return; }
  await changeStatus(row, 'active');
}

async function changeStatus(row: NumberingRule, target: MasterDataStatus) {
  try {
    await numberingRuleApi.setNumberingRuleStatus(row, target);
    ElMessage.success(target === 'active' ? '已启用' : '已停用');
    await fetchNumberingRules();
  } catch (e) {
    if (isConcurrencyConflict(e)) {
      ElMessage.warning('并发冲突：数据已被其他用户修改，请刷新后重试');
    } else if (e instanceof ApiError) {
      ElMessage.error(`${e.title}${e.detail ? ' — ' + e.detail : ''}`);
    } else {
      ElMessage.error(target === 'active' ? '启用失败' : '停用失败');
    }
  }
}

function resetModeLabel(mode?: NumberingRuleResetMode): string {
  return NUMBERING_RULE_RESET_MODE_OPTIONS.find(o => o.value === mode)?.label || '—';
}

// GULIERP_MDM_UI_FINAL_AUDIT_V1 (2026-09-02) — Chinese name
// mapping for DocumentType. Internal Key stays English; the
// Chinese name is for business operators. Per brief §6.
const DOCUMENT_TYPE_LABELS: Record<string, string> = {
  CUSTOMER: '客户编码',
  SUPPLIER: '供应商编码',
  BUSINESS_PARTNER: '客商编码',
  MATERIAL: '物料编码',
  ITEM: '物料编码',
  WAREHOUSE: '仓库编码',
  LOCATION: '库位编码',
  ITEM_CATEGORY: '物料分类编码',
  EMPLOYEE: '员工编码',
  SALES_ORDER: '销售订单',
  PURCHASE_ORDER: '采购订单',
  PAYMENT_METHOD: '付款方式编码',
};
function documentTypeLabel(type?: string): string {
  if (!type) return '—';
  return DOCUMENT_TYPE_LABELS[type] || type;
}

function formatDate(iso?: string): string {
  if (!iso) return '—';
  const d = new Date(iso);
  return d.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function exportData() {
  ElMessage.info('导出功能待后端支持');
}
</script>
