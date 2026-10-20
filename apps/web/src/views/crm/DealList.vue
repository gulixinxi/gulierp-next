<template>
  <div class="crm-deal-list">
    <el-card shadow="never">
      <template #header>
        <div class="card-header">
          <span class="title">商机管理</span>
          <div>
            <el-button size="small" @click="handleExport" :icon="Download">导出Excel</el-button>
            <el-button type="primary" size="small" @click="showDealForm = true">新建商机</el-button>
          </div>
        </div>
      </template>
      <el-table :data="list" v-loading="loading" stripe>
        <el-table-column prop="dealNo" label="商机编号" width="140" />
        <el-table-column prop="name" label="商机名称" min-width="160" show-overflow-tooltip />
        <el-table-column prop="companyName" label="客户" width="150" show-overflow-tooltip />
        <el-table-column label="业务品类" width="100">
          <template #default="{ row }">{{ categoryLabel(row.category) }}</template>
        </el-table-column>
        <el-table-column prop="expectedAmount" label="预计金额" width="110">
          <template #default="{ row }">¥{{ row.expectedAmount.toLocaleString() }}</template>
        </el-table-column>
        <el-table-column label="阶段" width="150">
          <template #default="{ row }">
            <el-select
              v-model="row.stage"
              size="small"
              :disabled="row.status !== 0"
              @change="handleStageChange(row)"
              style="width:100%"
            >
              <el-option label="初步接触" :value="0" />
              <el-option label="需求确认" :value="1" />
              <el-option label="方案报价" :value="2" />
              <el-option label="商务谈判" :value="3" />
              <el-option label="成交" :value="4" />
              <el-option label="丢单" :value="5" />
            </el-select>
          </template>
        </el-table-column>
        <el-table-column label="赢率" width="70">
          <template #default="{ row }">{{ row.winRate ?? '-' }}%</template>
        </el-table-column>
        <el-table-column label="状态" width="80">
          <template #default="{ row }">
            <el-tag :type="statusTagType(row.status)" size="small">{{ statusLabel(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="150" fixed="right">
          <template #default="{ row }">
            <el-button
              link type="success" size="small"
              :disabled="row.status !== 0"
              @click="handleWin(row)"
            >赢单</el-button>
            <el-button
              link type="danger" size="small"
              :disabled="row.status !== 0"
              @click="openLoseDialog(row)"
            >输单</el-button>
          </template>
        </el-table-column>
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

    <!-- 新建商机对话框 -->
    <el-dialog v-model="showDealForm" title="新建商机" width="520px">
      <el-form :model="dealForm" label-width="90px">
        <el-form-item label="商机名称" required>
          <el-input v-model="dealForm.name" placeholder="如：官网重建项目" />
        </el-form-item>
        <el-form-item label="客户" required>
          <el-select v-model="dealForm.crmCompanyId" filterable placeholder="选择客户" style="width:100%">
            <el-option
              v-for="c in companyOptions"
              :key="c.id"
              :label="c.fullName"
              :value="String(c.id)"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="业务品类" required>
          <el-select v-model="dealForm.category">
            <el-option label="企业建站" :value="0" />
            <el-option label="网站优化" :value="1" />
            <el-option label="AI应用" :value="2" />
            <el-option label="GEO优化" :value="3" />
            <el-option label="数字员工" :value="4" />
            <el-option label="其他" :value="99" />
          </el-select>
        </el-form-item>
        <el-form-item label="预计金额" required>
          <el-input-number v-model="dealForm.expectedAmount" :min="0" :step="1000" :controls="false" style="width:100%" />
        </el-form-item>
        <el-form-item label="当前阶段">
          <el-select v-model="dealForm.stage">
            <el-option label="初步接触" :value="0" />
            <el-option label="需求确认" :value="1" />
            <el-option label="方案报价" :value="2" />
            <el-option label="商务谈判" :value="3" />
          </el-select>
        </el-form-item>
        <el-form-item label="预计成交月">
          <el-input v-model="dealForm.expectedCloseMonth" placeholder="如 2026-12" />
        </el-form-item>
        <el-form-item label="赢率(%)">
          <el-input-number v-model="dealForm.winRate" :min="0" :max="100" :controls="false" style="width:100%" />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="dealForm.remarks" type="textarea" :rows="2" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showDealForm = false">取消</el-button>
        <el-button type="primary" @click="handleCreateDeal">创建</el-button>
      </template>
    </el-dialog>

    <!-- 输单原因对话框 -->
    <el-dialog v-model="showLoseForm" title="标记输单" width="400px">
      <el-form label-width="80px">
        <el-form-item label="输单原因" required>
          <el-select v-model="loseReason">
            <el-option label="价格" :value="0" />
            <el-option label="竞争对手" :value="1" />
            <el-option label="无预算" :value="2" />
            <el-option label="时机不对" :value="3" />
            <el-option label="联系不上" :value="4" />
            <el-option label="其他" :value="99" />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showLoseForm = false">取消</el-button>
        <el-button type="danger" @click="handleLose">确认输单</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Download } from '@element-plus/icons-vue';
import {
  listDeals, createDeal, updateDealStage, winDeal, loseDeal, listCompanies, getCompany
} from '../../api/crm';
import type { DealDto, CreateDealRequest, CompanyDto } from '../../types/crm';
import { BusinessCategory, DealStage, LostReason } from '../../types/crm';
import { exportToExcel } from '../../utils/exportExcel';

const route = useRoute();
const router = useRouter();
const loading = ref(false);
const list = ref<DealDto[]>([]);
const total = ref(0);
const page = ref(1);
const pageSize = ref(20);

const showDealForm = ref(false);
const showLoseForm = ref(false);
const loseReason = ref<number>(LostReason.Price);
const currentDeal = ref<DealDto | null>(null);
const companyOptions = ref<CompanyDto[]>([]);

const dealForm = reactive<CreateDealRequest>({
  name: '',
  crmCompanyId: '',
  category: BusinessCategory.WebsiteBuild,
  expectedAmount: 0,
  stage: DealStage.InitialContact,
  expectedCloseMonth: '',
  winRate: 20,
  remarks: ''
});

async function loadData() {
  loading.value = true;
  try {
    const result = await listDeals({ page: page.value, pageSize: pageSize.value });
    list.value = result.items;
    total.value = result.total;
  } catch (e: any) {
    ElMessage.error(e.message || '加载失败');
  } finally {
    loading.value = false;
  }
}

async function loadCompanies() {
  try {
    const result = await listCompanies({ page: 1, pageSize: 200 });
    companyOptions.value = result.items;
  } catch (e: any) {
    ElMessage.error(e.message || '客户列表加载失败');
  }
}

async function handleCreateDeal() {
  if (!dealForm.name.trim()) { ElMessage.warning('请输入商机名称'); return; }
  if (!dealForm.crmCompanyId) { ElMessage.warning('请选择客户'); return; }
  if (!dealForm.expectedAmount || dealForm.expectedAmount <= 0) { ElMessage.warning('请输入预计金额'); return; }
  try {
    await createDeal(dealForm);
    ElMessage.success('商机创建成功');
    showDealForm.value = false;
    Object.assign(dealForm, {
      name: '', crmCompanyId: '', category: BusinessCategory.WebsiteBuild,
      expectedAmount: 0, stage: DealStage.InitialContact,
      expectedCloseMonth: '', winRate: 20, remarks: ''
    });
    loadData();
  } catch (e: any) {
    ElMessage.error(e.message || '创建失败');
  }
}

async function handleStageChange(row: DealDto) {
  try {
    await updateDealStage(row.id, row.stage);
    ElMessage.success(`已推进到「${stageLabel(row.stage)}」`);
    loadData();
  } catch (e: any) {
    ElMessage.error(e.message || '阶段更新失败');
    loadData();
  }
}

async function handleWin(row: DealDto) {
  try {
    await ElMessageBox.confirm(`确认将商机「${row.name}」标记为赢单？`, '赢单确认', { type: 'success' });
    await winDeal(row.id);
    ElMessage.success('已标记赢单');
    loadData();
  } catch (e: any) {
    if (e !== 'cancel') ElMessage.error(e.message || '操作失败');
  }
}

function openLoseDialog(row: DealDto) {
  currentDeal.value = row;
  loseReason.value = LostReason.Price;
  showLoseForm.value = true;
}

async function handleLose() {
  if (!currentDeal.value) return;
  try {
    await loseDeal(currentDeal.value.id, loseReason.value);
    ElMessage.success('已标记输单');
    showLoseForm.value = false;
    currentDeal.value = null;
    loadData();
  } catch (e: any) {
    ElMessage.error(e.message || '操作失败');
  }
}

function categoryLabel(c: number) { return ['企业建站', '网站优化', 'AI应用', 'GEO优化', '数字员工', '其他'][c] || '其他'; }
function stageLabel(s: number) { return ['初步接触', '需求确认', '方案报价', '商务谈判', '成交', '丢单'][s] || '未知'; }
function statusLabel(s: number) { return ['进行中', '已成交', '已丢单', '暂搁'][s] || '未知'; }
function statusTagType(s: number) { return ['warning', 'success', 'danger', 'info'][s] || 'info'; }

onMounted(async () => {
  await loadCompanies();
  loadData();

  // 从商机建议页跳转过来时，预填并自动打开创建对话框
  if (route.query.action === 'create') {
    const companyId = route.query.companyId as string | undefined;
    const category = route.query.category as string;
    const suggestion = route.query.suggestion as string;

    if (!companyId || !/^\d+$/.test(companyId)) {
      ElMessage.warning('商机建议缺少有效客户ID，请从客户主档选择客户。');
      router.replace({ path: '/crm/deals' });
      return;
    }
    try {
      const company = await getCompany(companyId);
      dealForm.crmCompanyId = String(company.id);
      dealForm.name = `${company.fullName} - ${suggestion || '商机'}`;
      if (!companyOptions.value.some(c => String(c.id) === String(company.id))) {
        companyOptions.value.push(company);
      }
    } catch {
      ElMessage.warning('客户不存在或无权访问，无法从建议预填商机。');
      router.replace({ path: '/crm/deals' });
      return;
    }
    if (category) {
      dealForm.category = Number(category);
    }
    if (suggestion) {
      dealForm.remarks = `商机来源：商机建议引擎 - ${suggestion}`;
    }
    showDealForm.value = true;

    // 清除 query，避免刷新重复打开
    router.replace({ path: '/crm/deals' });
  }
});

function handleExport() {
  if (list.value.length === 0) { ElMessage.warning('没有可导出的数据'); return; }
  exportToExcel(list.value, [
    { key: 'dealNo', label: '商机编号' },
    { key: 'name', label: '商机名称' },
    { key: 'companyName', label: '客户' },
    { key: 'category', label: '业务品类' },
    { key: 'stage', label: '阶段' },
    { key: 'amount', label: '预计金额' },
    { key: 'ownerName', label: '负责人' },
    { key: 'nextAction', label: '下一步动作' },
    { key: 'nextActionDate', label: '计划时间' },
    { key: 'createdAt', label: '创建时间' },
    { key: 'remarks', label: '备注' },
  ], '商机管理导出');
  ElMessage.success(`已导出 ${list.value.length} 个商机`);
}
</script>

<style scoped>
.crm-deal-list { padding: 16px; }
.card-header { display: flex; justify-content: space-between; align-items: center; }
.title { font-size: 16px; font-weight: 600; }
.pagination { margin-top: 16px; display: flex; justify-content: flex-end; }
</style>
