<template>
  <div class="crm-today-tasks">
    <el-card shadow="never">
      <template #header>
        <div class="card-header">
          <div style="display:flex;align-items:center;gap:12px;">
            <span class="title">今日待办</span>
            <el-radio-group v-model="filterMode" size="small" @change="loadData">
              <el-radio-button value="mine">我的</el-radio-button>
              <el-radio-button value="all" v-if="isAdmin">全部</el-radio-button>
            </el-radio-group>
          </div>
          <el-tag type="danger" v-if="overdueCount > 0">逾期 {{ overdueCount }} 条</el-tag>
        </div>
      </template>
      <el-table :data="tasks" v-loading="loading" stripe>
        <el-table-column label="状态" width="80">
          <template #default="{ row }">
            <el-tag :type="row.isOverdue ? 'danger' : 'success'" size="small">
              {{ row.isOverdue ? '已逾期' : '今日' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="companyName" label="客户名称" min-width="200" />
        <el-table-column prop="nextAction" label="下次动作" min-width="250" />
        <el-table-column prop="nextActionDate" label="计划日期" width="120" />
        <el-table-column label="操作" width="160">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="goCompany(row.companyId)">去跟进</el-button>
            <el-button link type="success" size="small" @click="handleComplete(row)">标记完成</el-button>
          </template>
        </el-table-column>
      </el-table>
      <el-empty v-if="tasks.length === 0 && !loading" description="今日暂无待办" />
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { ElMessage } from 'element-plus';
import { getTodayTasks, completeActivityTask } from '../../api/crm';
import type { TodayTaskDto } from '../../types/crm';
import { useAuthStore } from '../../stores/auth';

const router = useRouter();
const auth = useAuthStore();
const loading = ref(false);
const tasks = ref<TodayTaskDto[]>([]);
const filterMode = ref<'mine' | 'all'>('mine');

const isAdmin = computed(() => auth.user?.isPlatformAdmin || auth.user?.userName === 'admin');
const currentUserId = computed(() => auth.user?.userId ? Number(auth.user.userId) : null);

const overdueCount = computed(() => tasks.value.filter(t => t.isOverdue).length);

async function loadData() {
  loading.value = true;
  try {
    const ownerId = filterMode.value === 'mine' && currentUserId.value ? currentUserId.value : undefined;
    tasks.value = await getTodayTasks(ownerId);
  } catch (e: any) {
    ElMessage.error(e.message || '加载失败');
  } finally {
    loading.value = false;
  }
}

function goCompany(id: string) {
  router.push(`/crm/companies/${id}`);
}

async function handleComplete(row: TodayTaskDto) {
  try {
    await completeActivityTask(row.activityId);
    ElMessage.success('已标记完成');
    loadData();
  } catch (e: any) {
    ElMessage.error(e.message || '操作失败');
  }
}

onMounted(loadData);
</script>

<style scoped>
.crm-today-tasks { padding: 16px; }
.card-header { display: flex; justify-content: space-between; align-items: center; }
.title { font-size: 16px; font-weight: 600; }
</style>
