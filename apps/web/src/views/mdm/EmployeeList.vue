<template>
  <div class="mdm-list">
    <MdmListToolbar
      v-model:search="searchKeyword"
      search-placeholder="搜索员工号 / 姓名"
      create-label="新建员工"
      @search="applyFilters"
      @create="showDeferredCreate"
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
      <span class="mdm-context-hint">（员工列表来自 Organization Employee API）</span>
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
        <el-table-column prop="departmentId" label="部门 ID" min-width="170" show-overflow-tooltip>
          <template #default="{ row }">{{ row.departmentId || '—' }}</template>
        </el-table-column>
        <el-table-column prop="userId" label="关联用户 ID" min-width="170" show-overflow-tooltip>
          <template #default="{ row }">{{ row.userId || '—' }}</template>
        </el-table-column>
        <el-table-column prop="modifiedAt" label="更新时间" width="170" sortable>
          <template #default="{ row }">{{ formatDate(row.modifiedAt) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="160" fixed="right" align="center">
          <template #default>
            <el-button text size="small" type="primary" disabled>编辑</el-button>
            <span class="mdm-action-sep">|</span>
            <el-button text size="small" type="primary" disabled>启停</el-button>
          </template>
        </el-table-column>
        <template #empty>
          <MdmEmptyState
            message="暂无员工档案数据"
            create-label="新建员工"
            :show-create="false"
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
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue';
import { ElMessage } from 'element-plus';
import { OfficeBuilding, Refresh } from '@element-plus/icons-vue';

import MdmListToolbar from '../../components/mdm/MdmListToolbar.vue';
import MdmPagination from '../../components/mdm/MdmPagination.vue';
import MdmEmptyState from '../../components/mdm/MdmEmptyState.vue';
import { ApiError } from '../../api/http';
import * as employeeApi from '../../api/mdm/employee';
import type {
  CompanyDirectoryEntryDto,
  EmployeeDto,
  EmployeeStatusFilter,
} from '../../api/mdm/employee';
import {
  EMPLOYEE_STATUS_OPTIONS,
  employeeStatusLabel,
  employeeStatusType,
} from '../../api/mdm/employee';

const companies = ref<CompanyDirectoryEntryDto[]>([]);
const selectedCompanyId = ref('');
const employees = ref<EmployeeDto[]>([]);
const total = ref(0);
const loading = ref(false);
const error = ref<string | null>(null);

const searchKeyword = ref('');
const filterStatus = ref<EmployeeStatusFilter>('');
const page = reactive({ current: 1, size: 20 });

const selectedCompanyName = computed(() => {
  const current = companies.value.find(company => company.id === selectedCompanyId.value);
  return current ? `${current.name}（${current.code}）` : '—';
});

async function loadCompanies() {
  const rows = await employeeApi.listCompaniesForEmployeeMaster();
  companies.value = rows;
  if (!selectedCompanyId.value && rows.length > 0) {
    selectedCompanyId.value = rows[0].id;
  }
}

function renderApiError(e: ApiError): string {
  const parts = [e.title];
  if (e.detail) parts.push(e.detail);
  if (e.requestId) parts.push(`(RequestId: ${e.requestId})`);
  return parts.join(' — ');
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

function handleCompanyChange() {
  page.current = 1;
  fetchEmployees();
}

function refresh() {
  fetchEmployees();
}

function showDeferredCreate() {
  ElMessage.info('员工新增 / 编辑 / 启停将在运行态授权闭环验收后接入');
}

function formatDate(value?: string | null): string {
  if (!value) return '—';
  return new Date(value).toLocaleString('zh-CN', { hour12: false });
}

watch(() => [page.current, page.size], fetchEmployees);

onMounted(initialize);
</script>
