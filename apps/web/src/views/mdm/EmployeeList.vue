<template>
  <div class="mdm-list">
    <MdmListToolbar
      v-model:search="searchKeyword"
      search-placeholder="搜索员工号 / 姓名"
      create-label="新建员工"
      @search="applyFilters"
      @create="openCreate"
    >
      <template #filters>
        <el-select
          v-model="selectedCompanyId"
          placeholder="公司"
          style="width: 180px"
          @change="handleCompanyChange"
        >
          <el-option
            v-for="company in companies"
            :key="company.id"
            :label="company.name"
            :value="company.id"
          />
        </el-select>
        <el-select v-model="filterStatus" placeholder="状态" clearable style="width: 100px" @change="applyFilters">
          <el-option v-for="opt in EMPLOYEE_STATUS_OPTIONS" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
      </template>
      <template #actions>
        <el-button text @click="refresh">
          <el-icon><Refresh /></el-icon>
          刷新
        </el-button>
      </template>
    </MdmListToolbar>

    <div class="mdm-context-bar">
      <el-icon><OfficeBuilding /></el-icon>
      <span>当前公司：{{ selectedCompanyName }}</span>
      <span class="mdm-context-hint">（员工新增 / 编辑 / 启停由后端按当前登录公司上下文校验）</span>
    </div>

    <div class="gs-list-wrap">
      <el-table
        v-loading="loading"
        :data="employees"
        border
        stripe
        height="100%"
        size="small"
        row-key="id"
        @row-dblclick="openEdit"
        :header-cell-style="{ padding: '0 8px' }"
        :cell-style="{ padding: '0 8px' }"
      >
        <el-table-column type="index" label="#" width="50" fixed="left" />
        <el-table-column prop="employeeNo" label="员工号" width="140" sortable show-overflow-tooltip>
          <template #default="{ row }">
            <span class="mdm-code">{{ row.employeeNo }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="name" label="姓名" min-width="160" show-overflow-tooltip sortable />
        <el-table-column prop="status" label="状态" width="90" align="center">
          <template #default="{ row }">
            <el-tag :type="employeeStatusType(row.status)" size="small" effect="light">
              {{ employeeStatusLabel(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="departmentId" label="部门" min-width="180" show-overflow-tooltip>
          <template #default="{ row }">{{ departmentLabel(row.departmentId) }}</template>
        </el-table-column>
        <el-table-column prop="userId" label="关联用户 ID" min-width="170" show-overflow-tooltip>
          <template #default="{ row }">{{ row.userId || '—' }}</template>
        </el-table-column>
        <el-table-column prop="modifiedAt" label="更新时间" width="170" sortable>
          <template #default="{ row }">{{ formatDate(row.modifiedAt) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="150" fixed="right" align="center">
          <template #default="{ row }">
            <div class="mdm-row-actions">
              <el-button
                text
                size="small"
                type="primary"
                :disabled="row.status === 99"
                @click="openEdit(row)"
              >编辑</el-button>
              <span class="mdm-action-sep">|</span>
              <el-button
                v-if="row.status === 1"
                text
                size="small"
                type="warning"
                @click="confirmDeactivate(row)"
              >停用</el-button>
              <el-button
                v-else-if="row.status === 2"
                text
                size="small"
                type="success"
                @click="confirmActivate(row)"
              >启用</el-button>
              <el-button v-else text size="small" type="info" disabled>已离职</el-button>
            </div>
          </template>
        </el-table-column>
        <template #empty>
          <MdmEmptyState
            message="暂无员工档案数据"
            create-label="新建员工"
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
      <el-button type="primary" link style="margin-left:12px" @click="refresh">重新加载</el-button>
    </div>

    <MdmFormDrawer
      v-model="formDrawerVisible"
      v-model:model="formData"
      :title="editingId ? '编辑员工' : '新建员工'"
      :rules="formRules"
      :loading="submitting"
      :submit-label="editingId ? '保存修改' : '创建'"
      @submit="handleSubmit"
    >
      <el-form-item label="公司">
        <el-input :model-value="selectedCompanyName" disabled />
      </el-form-item>
      <el-form-item label="员工号" prop="employeeNo">
        <el-input
          v-model="formData.employeeNo"
          placeholder="如 EMP_001，需大写字母开头"
          :disabled="!!editingId"
          maxlength="40"
        />
      </el-form-item>
      <el-form-item label="姓名" prop="name">
        <el-input v-model="formData.name" placeholder="员工姓名" maxlength="200" />
      </el-form-item>
      <el-form-item label="部门">
        <el-select
          v-model="formData.departmentId"
          placeholder="选择部门（可留空）"
          clearable
          filterable
          style="width: 100%"
        >
          <el-option
            v-for="dept in departments"
            :key="dept.id"
            :label="`${dept.code} · ${dept.name}`"
            :value="dept.id"
            :disabled="dept.status !== 'Active'"
          />
        </el-select>
      </el-form-item>
      <el-form-item label="状态">
        <el-input :model-value="editingStatusLabel" disabled />
      </el-form-item>
    </MdmFormDrawer>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import type { FormRules } from 'element-plus';
import { OfficeBuilding, Refresh } from '@element-plus/icons-vue';

import MdmListToolbar from '../../components/mdm/MdmListToolbar.vue';
import MdmFormDrawer from '../../components/mdm/MdmFormDrawer.vue';
import MdmPagination from '../../components/mdm/MdmPagination.vue';
import MdmEmptyState from '../../components/mdm/MdmEmptyState.vue';
import { ApiError } from '../../api/http';
import * as employeeApi from '../../api/mdm/employee';
import type {
  CompanyDirectoryEntryDto,
  EmployeeDto,
  EmployeeForm,
  EmployeeStatusFilter,
  OrganizationDirectoryEntryDto,
} from '../../api/mdm/employee';
import {
  EMPLOYEE_STATUS_OPTIONS,
  employeeStatusLabel,
  employeeStatusType,
} from '../../api/mdm/employee';

const companies = ref<CompanyDirectoryEntryDto[]>([]);
const departments = ref<OrganizationDirectoryEntryDto[]>([]);
const selectedCompanyId = ref('');
const employees = ref<EmployeeDto[]>([]);
const total = ref(0);
const loading = ref(false);
const error = ref<string | null>(null);

const searchKeyword = ref('');
const filterStatus = ref<EmployeeStatusFilter>('');
const page = reactive({ current: 1, size: 20 });

const formDrawerVisible = ref(false);
const editingId = ref<string | null>(null);
const editingStatus = ref<EmployeeDto['status']>(1);
const editingConcurrency = ref(0);
const submitting = ref(false);
const formData = reactive<EmployeeForm>(emptyForm());

const selectedCompanyName = computed(() => {
  const current = companies.value.find(company => company.id === selectedCompanyId.value);
  return current ? `${current.name}（${current.code}）` : '—';
});

const editingStatusLabel = computed(() => {
  if (!editingId.value) return employeeStatusLabel(1);
  return employeeStatusLabel(editingStatus.value);
});

const formRules: FormRules = {
  employeeNo: [
    { required: true, message: '请输入员工号', trigger: 'blur' },
    {
      pattern: /^[A-Z][A-Z0-9_]{1,39}$/,
      message: '员工号需大写字母开头，仅允许大写字母、数字、下划线，长度 2-40',
      trigger: 'blur',
    },
  ],
  name: [{ required: true, message: '请输入员工姓名', trigger: 'blur' }],
};

function emptyForm(): EmployeeForm {
  return {
    employeeNo: '',
    name: '',
    departmentId: null,
    userId: null,
  };
}

function resetForm() {
  Object.assign(formData, emptyForm());
  editingStatus.value = 1;
}

async function loadCompanies() {
  const rows = await employeeApi.listCompaniesForEmployeeMaster();
  companies.value = rows;
  if (!selectedCompanyId.value && rows.length > 0) {
    selectedCompanyId.value = rows[0].id;
  }
}

async function loadDepartments() {
  if (!selectedCompanyId.value) {
    departments.value = [];
    return;
  }
  try {
    departments.value = await employeeApi.listDepartmentsForEmployeeMaster(selectedCompanyId.value);
  } catch (e) {
    if (e instanceof ApiError && e.code === 'authentication_required') throw e;
    departments.value = [];
  }
}

function renderApiError(e: ApiError): string {
  if (e.code === 'identity_employee_cross_company') return '员工不属于当前公司或无权访问';
  if (e.code === 'identity_employee_department_cross_company') return '所选部门不属于当前公司或无权访问';
  if (e.code === 'identity_employee_department_inactive') return '所选部门不是启用状态';
  if (e.code === 'identity_employee_code_duplicate') return '员工号已存在，请更换后重试';
  if (e.code === 'identity_employee_code_format_invalid') return '员工号或姓名格式不符合后端校验规则';
  if (e.code === 'identity_employee_code_reserved') return '员工号为系统保留名称，请更换';
  if (e.code === 'identity_employee_code_resembles_document_number') return '员工号不能使用类似业务单据编号的格式';
  if (e.code === 'identity_employee_already_left') return '员工已离职，不能再编辑或启用';
  if (e.code === 'identity_employee_concurrency_conflict') return '并发冲突：数据已被其他用户修改，请刷新后重试';
  const parts = [e.title];
  if (e.detail) parts.push(e.detail);
  if (e.requestId) parts.push(`(RequestId: ${e.requestId})`);
  return parts.join(' — ');
}

function isConcurrencyConflict(err: unknown): boolean {
  return err instanceof ApiError && (
    err.code === 'identity_employee_concurrency_conflict'
    || /concurrency|version|并发/i.test(err.detail || '')
  );
}

function validateEmployeeForm(): boolean {
  const employeeNo = formData.employeeNo.trim();
  const name = formData.name.trim();
  if (!editingId.value && employeeNo.length === 0) {
    ElMessage.warning('请输入员工号');
    return false;
  }
  if (!editingId.value && !/^[A-Z][A-Z0-9_]{1,39}$/.test(employeeNo)) {
    ElMessage.warning('员工号需大写字母开头，仅允许大写字母、数字、下划线，长度 2-40');
    return false;
  }
  if (name.length === 0) {
    ElMessage.warning('请输入员工姓名');
    return false;
  }
  if (name.length > 200) {
    ElMessage.warning('员工姓名不能超过 200 个字符');
    return false;
  }
  return true;
}

async function fetchEmployees() {
  if (!selectedCompanyId.value) {
    employees.value = [];
    total.value = 0;
    return;
  }

  loading.value = true;
  error.value = null;
  try {
    const result = await employeeApi.listEmployees({
      companyId: selectedCompanyId.value,
      keyword: searchKeyword.value.trim() || undefined,
      status: filterStatus.value || undefined,
      page: page.current,
      pageSize: page.size,
    });
    employees.value = result.items;
    total.value = result.totalCount;
  } catch (e) {
    if (e instanceof ApiError) {
      if (e.code === 'authentication_required') throw e;
      error.value = renderApiError(e);
    } else {
      error.value = '加载员工档案失败，请刷新重试';
    }
    employees.value = [];
    total.value = 0;
  } finally {
    loading.value = false;
  }
}

async function initialize() {
  loading.value = true;
  error.value = null;
  try {
    await loadCompanies();
    await loadDepartments();
    await fetchEmployees();
  } catch (e) {
    if (e instanceof ApiError) {
      if (e.code === 'authentication_required') throw e;
      error.value = renderApiError(e);
    } else {
      error.value = '加载员工档案失败，请刷新重试';
    }
    employees.value = [];
    total.value = 0;
  } finally {
    loading.value = false;
  }
}

function applyFilters() {
  page.current = 1;
  fetchEmployees();
}

async function handleCompanyChange() {
  page.current = 1;
  resetForm();
  await loadDepartments();
  await fetchEmployees();
}

async function refresh() {
  await Promise.all([loadDepartments(), fetchEmployees()]);
}

function openCreate() {
  if (!selectedCompanyId.value) {
    ElMessage.warning('请先选择公司');
    return;
  }
  editingId.value = null;
  editingConcurrency.value = 0;
  resetForm();
  formDrawerVisible.value = true;
}

async function openEdit(row: EmployeeDto) {
  if (row.status === 99) {
    ElMessage.info('离职员工为终态，不能编辑');
    return;
  }
  editingId.value = row.id;
  editingConcurrency.value = row.concurrencyVersion ?? 0;
  try {
    const fresh = await employeeApi.getEmployee(row.id);
    Object.assign(formData, {
      employeeNo: fresh.employeeNo,
      name: fresh.name,
      departmentId: fresh.departmentId,
      userId: fresh.userId,
    });
    editingStatus.value = fresh.status;
    editingConcurrency.value = fresh.concurrencyVersion ?? 0;
  } catch (e) {
    if (e instanceof ApiError && e.code === 'authentication_required') throw e;
    Object.assign(formData, {
      employeeNo: row.employeeNo,
      name: row.name,
      departmentId: row.departmentId,
      userId: row.userId,
    });
    editingStatus.value = row.status;
  }
  formDrawerVisible.value = true;
}

async function handleSubmit() {
  if (!validateEmployeeForm()) return;
  submitting.value = true;
  try {
    if (editingId.value == null) {
      await employeeApi.createEmployeeFromForm(formData);
      ElMessage.success('创建员工成功');
    } else {
      await employeeApi.updateEmployeeFromForm(editingId.value, formData, editingConcurrency.value);
      ElMessage.success('保存修改成功');
    }
    formDrawerVisible.value = false;
    await fetchEmployees();
  } catch (e) {
    if (isConcurrencyConflict(e)) {
      ElMessage.warning('并发冲突：数据已被其他用户修改，请刷新后重试');
    } else if (e instanceof ApiError) {
      ElMessage.error(renderApiError(e));
    } else if (e instanceof Error && e.message) {
      ElMessage.error(e.message);
    } else {
      ElMessage.error('保存失败，请重试');
    }
  } finally {
    submitting.value = false;
  }
}

async function confirmDeactivate(row: EmployeeDto) {
  try {
    await ElMessageBox.confirm(
      `确定停用员工"${row.name}"吗？停用后该员工不能用于新的授权或业务操作。`,
      '停用确认',
      { confirmButtonText: '停用', cancelButtonText: '取消', type: 'warning' },
    );
  } catch { return; }
  try {
    await employeeApi.setEmployeeActiveStatus(row, 2);
    ElMessage.success('已停用');
    await fetchEmployees();
  } catch (e) {
    if (isConcurrencyConflict(e)) ElMessage.warning('并发冲突：数据已被其他用户修改，请刷新后重试');
    else if (e instanceof ApiError) ElMessage.error(renderApiError(e));
    else ElMessage.error('停用失败');
  }
}

async function confirmActivate(row: EmployeeDto) {
  try {
    await ElMessageBox.confirm(`确定启用员工"${row.name}"吗？`, '启用确认',
      { confirmButtonText: '启用', cancelButtonText: '取消', type: 'info' });
  } catch { return; }
  try {
    await employeeApi.setEmployeeActiveStatus(row, 1);
    ElMessage.success('已启用');
    await fetchEmployees();
  } catch (e) {
    if (isConcurrencyConflict(e)) ElMessage.warning('并发冲突：数据已被其他用户修改，请刷新后重试');
    else if (e instanceof ApiError) ElMessage.error(renderApiError(e));
    else ElMessage.error('启用失败');
  }
}

function departmentLabel(departmentId?: string | null): string {
  if (!departmentId) return '—';
  const dept = departments.value.find(d => d.id === departmentId);
  return dept ? `${dept.code} · ${dept.name}` : departmentId;
}

function formatDate(value?: string | null): string {
  if (!value) return '—';
  return new Date(value).toLocaleString('zh-CN', { hour12: false });
}

watch(() => [page.current, page.size], fetchEmployees);

onMounted(initialize);
</script>

<style scoped>
/* Employee uses the same MDM list shell and row action styling as UOM.
   Page-specific context copy stays local to this page. */
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
