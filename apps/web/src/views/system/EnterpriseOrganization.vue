<template>
  <section class="org-console">
    <div class="org-toolbar">
      <div>
        <h1>企业组织</h1>
        <p>{{ activeCompany?.name || '当前企业组织结构' }}</p>
      </div>
      <el-button :icon="Refresh" :loading="loading" @click="load">刷新</el-button>
    </div>

    <el-alert
      v-if="error"
      class="org-alert"
      :type="errorType"
      :title="error"
      :description="errorDetail"
      show-icon
      :closable="false"
    />

    <el-empty v-if="!loading && !error && companies.length === 0" :description="emptyDescription" />

    <template v-else-if="!error">
      <div class="org-summary">
        <section class="org-panel">
          <div class="panel-label">企业信息</div>
          <div class="company-name">{{ activeCompany?.name || '-' }}</div>
          <div class="company-meta">
            <span>{{ activeCompany?.code || '-' }}</span>
            <span>{{ activeCompany?.status || '-' }}</span>
          </div>
        </section>
        <section class="org-panel">
          <div class="panel-label">工厂</div>
          <div class="metric">{{ plants.length }}</div>
        </section>
        <section class="org-panel">
          <div class="panel-label">部门</div>
          <div class="metric">{{ departmentCount }}</div>
        </section>
        <section class="org-panel">
          <div class="panel-label">员工</div>
          <div class="metric">{{ employees.length }}</div>
        </section>
      </div>

      <div class="org-grid">
        <section class="org-section">
          <div class="section-title">工厂列表</div>
          <el-table :data="plants" size="small" stripe height="260">
            <el-table-column prop="code" label="编码" width="120" />
            <el-table-column prop="name" label="名称" min-width="160" />
            <el-table-column label="默认" width="90">
              <template #default="{ row }">
                <el-tag v-if="row.isDefault" size="small" type="success">默认</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="status" label="状态" width="100" />
          </el-table>
        </section>

        <section class="org-section">
          <div class="section-title">部门树</div>
          <el-tree
            class="department-tree"
            :data="departmentTree"
            node-key="organizationUnitId"
            default-expand-all
            :props="{ label: 'name', children: 'children' }"
          />
        </section>
      </div>

      <section class="org-section">
        <div class="section-title">员工列表</div>
        <el-table :data="employees" size="small" stripe height="320">
          <el-table-column prop="employeeNo" label="员工号" width="140" />
          <el-table-column prop="name" label="姓名" min-width="160" />
          <el-table-column prop="departmentName" label="部门" min-width="160" />
          <el-table-column prop="userId" label="用户ID" min-width="180" />
          <el-table-column prop="status" label="状态" width="100" />
        </el-table>
      </section>
    </template>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { Refresh } from '@element-plus/icons-vue';
import { getOrganizationTree, type OrganizationCompanyNodeDto, type OrganizationEmployeeNodeDto, type OrganizationUnitNodeDto } from '../../api/organization';
import { ApiError } from '../../api/http';

const loading = ref(false);
const error = ref('');
const errorDetail = ref('');
const errorType = ref<'error' | 'warning'>('error');
const companies = ref<OrganizationCompanyNodeDto[]>([]);

const activeCompany = computed(() => companies.value[0] ?? null);
const plants = computed(() => activeCompany.value?.plants ?? []);
const departmentTree = computed(() => activeCompany.value?.organizationUnits ?? []);
const departmentCount = computed(() => countDepartments(departmentTree.value));
const employees = computed(() => flattenEmployees(departmentTree.value));
const emptyDescription = computed(() => '尚未初始化企业组织');

async function load() {
  loading.value = true;
  error.value = '';
  errorDetail.value = '';
  errorType.value = 'error';
  try {
    const data = await getOrganizationTree();
    companies.value = data.companies ?? [];
  } catch (err) {
    companies.value = [];
    const apiError = err instanceof ApiError ? err : null;
    const status = apiError?.status ?? (err as any)?.status;
    if (status === 401) {
      errorType.value = 'warning';
      error.value = '登录已失效';
      errorDetail.value = '请重新登录后再查看企业组织。';
    } else if (status === 403) {
      errorType.value = 'warning';
      error.value = '无权访问企业组织';
      errorDetail.value = '当前账号没有系统管理权限。';
    } else if (status === 404) {
      errorType.value = 'warning';
      error.value = '尚未初始化企业组织';
      errorDetail.value = '请由管理员完成企业初始化后再查看。';
    } else {
      error.value = apiError?.title || '企业组织加载失败';
      const requestText = apiError?.requestId ? `RequestId: ${apiError.requestId}` : '';
      const traceText = apiError?.traceId ? `TraceId: ${apiError.traceId}` : '';
      errorDetail.value = [apiError?.detail, requestText, traceText].filter(Boolean).join(' ');
    }
    ElMessage.error(error.value);
  } finally {
    loading.value = false;
  }
}

function countDepartments(nodes: OrganizationUnitNodeDto[]): number {
  return nodes.reduce((sum, node) => sum + 1 + countDepartments(node.children ?? []), 0);
}

function flattenEmployees(nodes: OrganizationUnitNodeDto[], departmentName = ''): Array<OrganizationEmployeeNodeDto & { departmentName: string }> {
  const rows: Array<OrganizationEmployeeNodeDto & { departmentName: string }> = [];
  for (const node of nodes) {
    rows.push(...(node.employees ?? []).map(employee => ({ ...employee, departmentName: node.name || departmentName })));
    rows.push(...flattenEmployees(node.children ?? [], node.name || departmentName));
  }
  return rows;
}

onMounted(load);
</script>

<style scoped>
.org-console {
  padding: 16px;
  background: var(--bg-canvas);
  min-height: 100%;
}
.org-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 12px;
}
.org-toolbar h1 {
  font-size: 20px;
  margin: 0 0 4px;
}
.org-toolbar p {
  margin: 0;
  color: var(--text-secondary);
  font-size: 13px;
}
.org-alert {
  margin-bottom: 12px;
}
.org-summary {
  display: grid;
  grid-template-columns: minmax(220px, 2fr) repeat(3, minmax(120px, 1fr));
  gap: 12px;
  margin-bottom: 12px;
}
.org-panel,
.org-section {
  background: var(--bg-container);
  border: 1px solid var(--border-default);
  border-radius: var(--radius-sm);
}
.org-panel {
  padding: 14px;
}
.panel-label,
.section-title {
  font-size: 12px;
  color: var(--text-muted);
  margin-bottom: 8px;
}
.company-name {
  font-size: 18px;
  font-weight: 600;
  color: var(--text-primary);
}
.company-meta {
  display: flex;
  gap: 12px;
  color: var(--text-secondary);
  margin-top: 8px;
  font-size: 13px;
}
.metric {
  font-size: 26px;
  font-weight: 700;
}
.org-grid {
  display: grid;
  grid-template-columns: minmax(320px, 1fr) minmax(320px, 1fr);
  gap: 12px;
  margin-bottom: 12px;
}
.org-section {
  padding: 12px;
}
.department-tree {
  height: 260px;
  overflow: auto;
}
@media (max-width: 900px) {
  .org-summary,
  .org-grid {
    grid-template-columns: 1fr;
  }
}
</style>
