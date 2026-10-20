<template>
  <div class="crm-logs">
    <el-card shadow="never">
      <template #header>
        <div class="card-header">
          <span class="title">导入导出日志</span>
        </div>
      </template>
      <el-table :data="list" v-loading="loading" stripe>
        <el-table-column prop="operatedAt" label="操作时间" width="170">
          <template #default="{ row }">{{ formatDate(row.operatedAt) }}</template>
        </el-table-column>
        <el-table-column label="操作类型" width="100">
          <template #default="{ row }">{{ opTypeLabel(row.operationType) }}</template>
        </el-table-column>
        <el-table-column prop="scope" label="操作范围" min-width="200" />
        <el-table-column prop="successCount" label="成功" width="80" align="center" />
        <el-table-column prop="failedCount" label="失败" width="80" align="center" />
        <el-table-column prop="skippedCount" label="跳过" width="80" align="center" />
        <el-table-column label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="statusTagType(row.status)">{{ statusLabel(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="failureDetails" label="失败明细" min-width="200" show-overflow-tooltip />
      </el-table>
      <div class="pagination">
        <el-pagination
          v-model:current-page="page"
          v-model:page-size="pageSize"
          :total="total"
          layout="total, prev, pager, next"
          @current-change="loadData"
        />
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { ElMessage } from 'element-plus';
import { listImportExportLogs } from '../../api/crm';
import type { ImportExportLogDto } from '../../types/crm';

const loading = ref(false);
const list = ref<ImportExportLogDto[]>([]);
const total = ref(0);
const page = ref(1);
const pageSize = ref(20);

async function loadData() {
  loading.value = true;
  try {
    const result = await listImportExportLogs({ page: page.value, pageSize: pageSize.value });
    list.value = result.items;
    total.value = result.total;
  } catch (e: any) {
    ElMessage.error(e.message || '加载失败');
  } finally {
    loading.value = false;
  }
}

function opTypeLabel(t: number) { return ['导入', '导出', '批量删除', '批量改派'][t] || '未知'; }
function statusLabel(s: number) { return ['成功', '部分失败', '失败'][s] || '未知'; }
function statusTagType(s: number) { return ['success', 'warning', 'danger'][s] || 'info'; }
function formatDate(d: string) { return new Date(d).toLocaleString('zh-CN'); }

onMounted(loadData);
</script>

<style scoped>
.crm-logs { padding: 16px; }
.card-header { display: flex; justify-content: space-between; align-items: center; }
.title { font-size: 16px; font-weight: 600; }
.pagination { margin-top: 16px; display: flex; justify-content: flex-end; }
</style>
