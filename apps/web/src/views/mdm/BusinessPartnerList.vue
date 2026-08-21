<template>
  <!-- BusinessPartnerList — reusable page for 客户档案 / 供应商 / 全部往来单位.
       Route meta.defaultRole ('customer' | 'supplier' | undefined) sets the
       INITIAL role filter only; the operator may switch freely afterwards.
       Per TRAE_MDM_002_API_HANDOFF.md §2/§5/§8/§9.
       No mock fallback. 404 → "数据不存在或无权访问". -->
  <div class="mdm-list">
    <MdmListToolbar
      v-model:search="searchKeyword"
      search-placeholder="搜索代码 / 名称 / 简称"
      :create-label="createLabel"
      @search="applyFilters"
      @create="openCreate"
    >
      <template #filters>
        <el-select
          v-model="filterRole"
          placeholder="往来单位类型"
          clearable
          style="width: 150px"
          @change="applyFilters"
        >
          <el-option
            v-for="opt in BP_ROLE_FILTER_OPTIONS"
            :key="opt.value"
            :label="opt.label"
            :value="opt.value"
          />
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
        <el-table-column type="index" label="#" width="50" fixed="left" />
        <el-table-column prop="code" label="代码" width="120" sortable show-overflow-tooltip>
          <template #default="{ row }">
            <span class="mdm-code">{{ row.code }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="name" label="名称" min-width="180" show-overflow-tooltip sortable />
        <el-table-column prop="shortName" label="简称" width="120" show-overflow-tooltip>
          <template #default="{ row }">{{ row.shortName || '—' }}</template>
        </el-table-column>
        <el-table-column prop="role" label="类型" width="120" align="center">
          <template #default="{ row }">
            <el-tag :type="roleTagType(row.role)" size="small" effect="light">{{ roleLabel(row.role) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="contactPerson" label="联系人" width="100" show-overflow-tooltip>
          <template #default="{ row }">{{ row.contactPerson || '—' }}</template>
        </el-table-column>
        <el-table-column prop="phone" label="电话" width="140" show-overflow-tooltip>
          <template #default="{ row }">{{ row.phone || '—' }}</template>
        </el-table-column>
        <el-table-column prop="email" label="邮箱" width="180" show-overflow-tooltip>
          <template #default="{ row }">{{ row.email || '—' }}</template>
        </el-table-column>
        <el-table-column prop="taxNumber" label="税号" width="160" show-overflow-tooltip>
          <template #default="{ row }">{{ row.taxNumber || '—' }}</template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="80" align="center">
          <template #default="{ row }">
            <MdmStatusBadge :status="row.status" />
          </template>
        </el-table-column>
        <el-table-column prop="updatedAt" label="更新时间" width="160" sortable>
          <template #default="{ row }">{{ formatDate(row.updatedAt) }}</template>
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
            :message="emptyMessage"
            :create-label="createLabel"
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
      :title="editingId ? '编辑往来单位' : '新建往来单位'"
      :rules="formRules"
      :loading="submitting"
      :submit-label="editingId ? '保存修改' : '创建'"
      @submit="handleSubmit"
    >
      <el-form-item label="代码" prop="code">
        <el-input v-model="formData.code" placeholder="如 CUST-0001" :disabled="!!editingId" maxlength="40" />
      </el-form-item>
      <el-form-item label="名称" prop="name">
        <el-input v-model="formData.name" placeholder="往来单位全称" maxlength="200" />
      </el-form-item>
      <el-form-item label="简称">
        <el-input v-model="formData.shortName" placeholder="可选" maxlength="40" />
      </el-form-item>
      <el-form-item label="类型" prop="role">
        <el-select v-model="formData.role" style="width: 100%">
          <el-option v-for="opt in BP_ROLE_OPTIONS" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
      </el-form-item>
      <el-form-item label="联系人">
        <el-input v-model="formData.contactPerson" maxlength="50" />
      </el-form-item>
      <el-form-item label="电话">
        <el-input v-model="formData.phone" maxlength="50" />
      </el-form-item>
      <el-form-item label="邮箱" prop="email">
        <el-input v-model="formData.email" placeholder="含 @" maxlength="100" />
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
      <el-form-item label="税号">
        <el-input v-model="formData.taxNumber" maxlength="50" />
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
      :title="`往来单位详情 · ${detailData?.code || ''}`"
      @edit="openEditFromDetail"
    >
      <template #header>
        <div class="mdm-detail-title">
          <MdmStatusBadge :status="detailData?.status || 'active'" />
          <span class="mdm-detail-name">{{ detailData?.name }}</span>
          <el-tag v-if="detailData?.role" :type="roleTagType(detailData.role)" size="small" effect="light">
            {{ roleLabel(detailData.role) }}
          </el-tag>
        </div>
      </template>
      <el-descriptions :column="2" border size="small">
        <el-descriptions-item label="代码">{{ detailData?.code }}</el-descriptions-item>
        <el-descriptions-item label="名称">{{ detailData?.name }}</el-descriptions-item>
        <el-descriptions-item label="简称">{{ detailData?.shortName || '—' }}</el-descriptions-item>
        <el-descriptions-item label="类型">{{ roleLabel(detailData?.role) }}</el-descriptions-item>
        <el-descriptions-item label="联系人">{{ detailData?.contactPerson || '—' }}</el-descriptions-item>
        <el-descriptions-item label="电话">{{ detailData?.phone || '—' }}</el-descriptions-item>
        <el-descriptions-item label="邮箱">{{ detailData?.email || '—' }}</el-descriptions-item>
        <el-descriptions-item label="税号">{{ detailData?.taxNumber || '—' }}</el-descriptions-item>
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

import { ApiError } from '../../api/http';
import * as bpApi from '../../api/mdm/business-partner';
import {
  STATUS_OPTIONS,
  BP_ROLE_OPTIONS,
  BP_ROLE_FILTER_OPTIONS,
  roleFilterToInt,
  statusUiToInt,
} from '../../types/mdm';
import type {
  BusinessPartner,
  BusinessPartnerForm,
  BusinessPartnerRole,
  BusinessPartnerRoleFilter,
  MasterDataStatus,
} from '../../types/mdm';

// ===== Route-driven default role (meta.defaultRole) =====
const route = useRoute();
const defaultRole = computed<BusinessPartnerRoleFilter>(
  () => (route.meta?.defaultRole as BusinessPartnerRoleFilter | undefined) ?? 'all',
);
const createLabel = computed(() =>
  defaultRole.value === 'customer' ? '新建客户'
    : defaultRole.value === 'supplier' ? '新建供应商'
      : '新建往来单位',
);
const emptyMessage = computed(() =>
  defaultRole.value === 'customer' ? '暂无客户数据'
    : defaultRole.value === 'supplier' ? '暂无供应商数据'
      : '暂无往来单位数据',
);

// ===== Real API list state =====
const list = ref<BusinessPartner[]>([]);
const total = ref(0);
const loading = ref(false);
const error = ref<string | null>(null);

const searchKeyword = ref('');
const filterRole = ref<BusinessPartnerRoleFilter>('all');
const filterStatus = ref<MasterDataStatus | ''>('');
const page = reactive({ current: 1, size: 20 });

async function fetchList() {
  loading.value = true;
  error.value = null;
  try {
    const result = await bpApi.listBusinessPartners({
      keyword: searchKeyword.value.trim() || undefined,
      role: roleFilterToInt(filterRole.value),
      status: filterStatus.value ? statusUiToInt(filterStatus.value) : undefined,
      page: page.current,
      pageSize: page.size,
    });
    list.value = result.items;
    total.value = result.totalCount;
  } catch (e) {
    if (e instanceof ApiError) {
      if (e.code === 'authentication_required') throw e; // global guard redirects
      if (e.status === 404) {
        error.value = '数据不存在或无权访问';
      } else {
        const parts = [e.title];
        if (e.detail) parts.push(e.detail);
        if (e.requestId) parts.push(`(RequestId: ${e.requestId})`);
        error.value = parts.join(' — ');
      }
    } else {
      error.value = '加载往来单位数据失败，请刷新重试';
    }
    list.value = [];
    total.value = 0;
  } finally {
    loading.value = false;
  }
}

onMounted(() => {
  // Apply route-driven default role on initial mount only.
  filterRole.value = defaultRole.value;
  fetchList();
});

const pagedData = computed(() => list.value);

function applyFilters() {
  page.current = 1;
  fetchList();
}

// ===== Form state =====
const formDrawerVisible = ref(false);
const editingId = ref<number | null>(null);
const editingConcurrency = ref(0);
const submitting = ref(false);
const formData = reactive<BusinessPartnerForm>(emptyForm());

function emptyForm(): BusinessPartnerForm {
  return {
    code: '', name: '', shortName: '',
    role: defaultRole.value === 'supplier' ? 'SUPPLIER'
      : defaultRole.value === 'customer' ? 'CUSTOMER'
        : 'CUSTOMER',
    contactPerson: '', phone: '', email: '',
    addressLine1: '', addressLine2: '', city: '', region: '',
    postalCode: '', countryCode: '', taxNumber: '',
    status: 'active', description: '',
  };
}

const formRules: FormRules = {
  code: [{ required: true, message: '请输入往来单位代码', trigger: 'blur' }],
  name: [{ required: true, message: '请输入往来单位名称', trigger: 'blur' }],
  role: [{ required: true, message: '请选择类型', trigger: 'change' }],
  email: [{ type: 'email', message: '邮箱格式不正确', trigger: 'blur' }],
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

async function openEdit(row: BusinessPartner) {
  editingId.value = row.id;
  editingConcurrency.value = row.concurrencyVersion ?? 0;
  // Re-read via API for the authoritative DTO (fresh concurrencyVersion + fields).
  try {
    const fresh = await bpApi.getBusinessPartner(row.id);
    fillForm(fresh);
    editingConcurrency.value = fresh.concurrencyVersion ?? 0;
  } catch (e) {
    // Fall back to row snapshot; concurrency mismatch will be caught at save time.
    fillForm(row);
  }
  formDrawerVisible.value = true;
}

function fillForm(d: BusinessPartner) {
  Object.assign(formData, {
    code: d.code, name: d.name, shortName: d.shortName || '',
    role: d.role, contactPerson: d.contactPerson || '', phone: d.phone || '',
    email: d.email || '', addressLine1: d.addressLine1 || '',
    addressLine2: d.addressLine2 || '', city: d.city || '', region: d.region || '',
    postalCode: d.postalCode || '', countryCode: d.countryCode || '',
    taxNumber: d.taxNumber || '', status: d.status, description: d.description || '',
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
      await bpApi.createBusinessPartner(formData);
      ElMessage.success('创建往来单位成功');
    } else {
      await bpApi.updateBusinessPartner(editingId.value, formData, editingConcurrency.value);
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

function formatApiError(e: ApiError): string {
  if (e.code === 'mdm_location_parent_warehouse_cross_scope') {
    return '所选仓库不属于当前公司或无权访问';
  }
  const parts = [e.title];
  if (e.detail) parts.push(e.detail);
  if (e.requestId) parts.push(`(${e.requestId})`);
  return parts.join(' — ');
}

// ===== Detail state =====
const detailDrawerVisible = ref(false);
const detailData = ref<BusinessPartner | null>(null);

async function openDetail(row: BusinessPartner) {
  try {
    detailData.value = await bpApi.getBusinessPartner(row.id);
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
function roleLabel(role?: BusinessPartnerRole): string {
  return BP_ROLE_OPTIONS.find(o => o.value === role)?.label || '—';
}
function roleTagType(role?: BusinessPartnerRole): 'primary' | 'success' | 'warning' {
  if (role === 'CUSTOMER') return 'primary';
  if (role === 'SUPPLIER') return 'success';
  return 'warning';
}
function formatAddress(d: BusinessPartner | null): string {
  if (!d) return '';
  return [d.addressLine1, d.addressLine2, d.city, d.region, d.postalCode]
    .filter(Boolean).join(' ');
}

// ===== Status change =====
async function confirmDeactivate(row: BusinessPartner) {
  try {
    await ElMessageBox.confirm(
      `确定停用"${row.name}"吗？停用后将不能用于新的业务单据，历史数据不受影响。`,
      '停用确认',
      { confirmButtonText: '停用', cancelButtonText: '取消', type: 'warning' },
    );
  } catch { return; }
  try {
    await bpApi.setBusinessPartnerStatus(row, 'inactive');
    ElMessage.success('已停用');
    await fetchList();
  } catch (e) {
    if (isConcurrencyConflict(e)) ElMessage.warning('并发冲突：数据已被其他用户修改，请刷新后重试');
    else if (e instanceof ApiError) ElMessage.error(formatApiError(e));
    else ElMessage.error('停用失败');
  }
}

async function confirmActivate(row: BusinessPartner) {
  try {
    await ElMessageBox.confirm(`确定启用"${row.name}"吗？`, '启用确认',
      { confirmButtonText: '启用', cancelButtonText: '取消', type: 'info' });
  } catch { return; }
  try {
    await bpApi.setBusinessPartnerStatus(row, 'active');
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
.mdm-error-banner {
  display: flex;
  align-items: center;
  padding: 8px 12px;
  background: var(--bg-surface, #FFF);
  border-top: 1px solid var(--border-subtle, #E2E8F0);
}
</style>
