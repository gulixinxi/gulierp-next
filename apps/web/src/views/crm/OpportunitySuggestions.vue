<template>
  <div class="opp-suggestions">
    <h2 class="page-title">商机建议 <span class="subtitle">（规则引擎自动生成，人工确认后转商机）</span></h2>

    <el-card v-loading="loading" shadow="never">
      <template #header>
        <div class="card-header">
          <span>共 {{ suggestions.length }} 条建议</span>
          <el-button type="primary" size="small" @click="loadData">刷新</el-button>
        </div>
      </template>

      <el-table :data="sortedSuggestions" size="small" stripe>
        <el-table-column label="优先级" width="80">
          <template #default="{ row }">
            <el-tag :type="priorityTag(row.priority)" size="small">{{ row.priority }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="targetName" label="企业名称" min-width="180" show-overflow-tooltip />
        <el-table-column label="类型" width="70">
          <template #default="{ row }">
            <el-tag size="small" :type="row.targetType === 'lead' ? 'info' : 'warning'">{{ row.targetType === 'lead' ? '线索' : '客户' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="opportunityType" label="机会类型" width="110" />
        <el-table-column prop="reason" label="推荐理由" min-width="200" show-overflow-tooltip />
        <el-table-column prop="evidence" label="证据" min-width="180" show-overflow-tooltip />
        <el-table-column prop="suggestedAmount" label="建议报价" width="120" />
        <el-table-column label="操作" width="100" fixed="right">
          <template #default="{ row }">
            <el-button size="small" type="primary" link @click="goCreateDeal(row)">{{ row.targetType === 'lead' ? '先转客户' : '转商机' }}</el-button>
          </template>
        </el-table-column>
      </el-table>

      <el-empty v-if="!loading && suggestions.length === 0" description="暂无商机建议，先导入企业线索" />
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { ElMessage } from 'element-plus';
import { getOpportunitySuggestions, type OpportunitySuggestion } from '../../api/crm';

const router = useRouter();
const loading = ref(false);
const suggestions = ref<OpportunitySuggestion[]>([]);

const priorityOrder: Record<string, number> = { '高': 0, '中高': 1, '中': 2, '低': 3 };
const sortedSuggestions = computed(() =>
  [...suggestions.value].sort((a, b) => (priorityOrder[a.priority] ?? 9) - (priorityOrder[b.priority] ?? 9))
);

function priorityTag(p: string): string {
  if (p === '高') return 'danger';
  if (p === '中高') return 'warning';
  return 'info';
}

async function loadData() {
  loading.value = true;
  try {
    const res = await getOpportunitySuggestions();
    suggestions.value = res.items || [];
  } catch (e) {
    console.error('加载商机建议失败', e);
  } finally {
    loading.value = false;
  }
}

function goCreateDeal(row: OpportunitySuggestion) {
  if (row.targetType === 'lead') {
    ElMessage.info('请先将线索转为客户，再创建商机。');
    router.push({ path: '/crm/leads', query: { keyword: row.targetName } });
    return;
  }

  router.push({
    path: '/crm/deals',
    query: { action: 'create', companyId: String(row.targetId), category: String(row.productCategory), suggestion: row.opportunityType }
  });
}

onMounted(loadData);
</script>

<style scoped>
.opp-suggestions { padding: 16px; }
.page-title { margin: 0 0 16px; font-size: 20px; }
.subtitle { font-size: 13px; color: #909399; font-weight: normal; }
.card-header { display: flex; justify-content: space-between; align-items: center; }
</style>
