<template>
  <div class="crm-lead-list">
    <el-card shadow="never">
      <template #header>
        <div class="card-header">
          <span class="title">线索池</span>
          <div>
            <el-button @click="handleExport" :icon="Download">导出Excel</el-button>
            <el-button @click="showImport = true" :icon="Upload">批量导入</el-button>
            <el-button type="primary" @click="showCreate = true" :icon="Plus">新建线索</el-button>
          </div>
        </div>
      </template>

      <el-form :inline="true" :model="filters" class="search-form">
        <el-form-item label="关键词">
          <el-input v-model="filters.keyword" placeholder="公司名/联系人/手机" clearable @keyup.enter="loadData" />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="filters.status" placeholder="全部" clearable>
            <el-option label="待认领" :value="0" />
            <el-option label="已认领" :value="1" />
            <el-option label="已转化" :value="2" />
            <el-option label="已废弃" :value="3" />
          </el-select>
        </el-form-item>
        <el-form-item label="评级">
          <el-select v-model="filters.rating" placeholder="全部" clearable>
            <el-option label="A" :value="0" />
            <el-option label="B" :value="1" />
            <el-option label="C" :value="2" />
            <el-option label="D" :value="3" />
          </el-select>
        </el-form-item>
        <el-form-item label="负责人">
          <el-select v-model="filters.ownerId" placeholder="全部" clearable>
            <el-option v-for="m in teamMembers" :key="m.id" :label="m.displayName || m.userName" :value="m.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="主体类型">
          <el-select v-model="filters.subjectType" placeholder="全部" clearable>
            <el-option label="企业" :value="0" />
            <el-option label="协会/商会" :value="1" />
            <el-option label="服务中心" :value="2" />
            <el-option label="专业服务(会计/律师/咨询)" :value="3" />
            <el-option label="园区/基地" :value="4" />
            <el-option label="政府机关" :value="5" />
            <el-option label="事业单位" :value="6" />
            <el-option label="学历教育(学校/幼儿园/大学)" :value="7" />
            <el-option label="教育培训(托管/教培/督导)" :value="8" />
            <el-option label="个体商户/门店(餐饮/零售/服务)" :value="10" />
          </el-select>
        </el-form-item>
        <el-form-item label="标签">
          <el-input v-model="filters.tag" placeholder="按标签筛选" clearable style="width:140px" @keyup.enter="loadData" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="loadData" :icon="Search">搜索</el-button>
          <el-button @click="resetFilters" :icon="Refresh">重置</el-button>
          <el-button v-if="currentUserId" :type="filters.ownerId === currentUserId ? 'warning' : 'default'" @click="filterMine">我的线索</el-button>
        </el-form-item>
      </el-form>

      <el-table :data="list" v-loading="loading" stripe>
        <el-table-column prop="companyName" label="公司名称" min-width="180" />
        <el-table-column label="主体类型" width="130">
          <template #default="{ row }">
            <el-tag :type="subjectTypeTagType(row.subjectType)" size="small">{{ subjectTypeLabel(row.subjectType) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="contactName" label="联系人" width="100" />
        <el-table-column prop="mobile" label="手机" width="120" />
        <el-table-column prop="websiteDomain" label="官网" width="150" show-overflow-tooltip />
        <el-table-column label="AI评分" width="80">
          <template #default="{ row }">
            <el-tag :type="scoreTagType(row.aiScore)">{{ row.aiScore ?? '-' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="评级" width="70">
          <template #default="{ row }">
            <el-tag v-if="row.rating !== undefined" :type="ratingTagType(row.rating)">{{ ratingLabel(row.rating) }}</el-tag>
            <span v-else>-</span>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="90">
          <template #default="{ row }">
            <el-tag :type="statusTagType(row.status)">{{ statusLabel(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="负责人" width="100">
          <template #default="{ row }">
            <span>{{ ownerName(row.ownerId) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="标签" min-width="150">
          <template #default="{ row }">
            <el-tag v-for="tag in (row.tags || [])" :key="tag" size="small" effect="plain" style="margin:2px">
              {{ tag }}
            </el-tag>
            <el-button link type="primary" size="small" @click="handleEditTags(row)">编辑</el-button>
          </template>
        </el-table-column>
        <el-table-column prop="createdAt" label="创建时间" width="160">
          <template #default="{ row }">{{ formatDate(row.createdAt) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="260" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleClaim(row)" v-if="row.status === 0">认领</el-button>
            <el-button link type="primary" size="small" @click="handleAssign(row)">分配</el-button>
            <el-button link type="primary" size="small" @click="handleConvert(row)" v-if="row.status !== 2 && row.status !== 3">转化</el-button>
            <el-button link type="warning" size="small" @click="handleDiscard(row)" v-if="row.status !== 3">废弃</el-button>
            <el-button link type="danger" size="small" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination">
        <el-pagination
          v-model:current-page="filters.page"
          v-model:page-size="filters.pageSize"
          :total="total"
          :page-sizes="[10, 20, 50]"
          layout="total, sizes, prev, pager, next"
          @size-change="loadData"
          @current-change="loadData"
        />
      </div>
    </el-card>

    <el-dialog v-model="showCreate" title="新建线索" width="600px">
      <el-form :model="form" label-width="100px">
        <el-form-item label="公司名称" required>
          <el-input v-model="form.companyName" />
        </el-form-item>
        <el-form-item label="统一社会信用代码">
          <el-input v-model="form.creditCode" maxlength="18" placeholder="18位，国内企业去重首选" />
        </el-form-item>
        <el-form-item label="官网域名">
          <el-input v-model="form.websiteDomain" placeholder="如 example.com" />
        </el-form-item>
        <el-form-item label="联系人">
          <el-input v-model="form.contactName" />
        </el-form-item>
        <el-form-item label="手机">
          <el-input v-model="form.mobile" />
        </el-form-item>
        <el-form-item label="邮箱">
          <el-input v-model="form.email" />
        </el-form-item>
        <el-form-item label="来源">
          <el-select v-model="form.source">
            <el-option label="存量Excel" :value="0" />
            <el-option label="政府公示" :value="1" />
            <el-option label="行业协会" :value="2" />
            <el-option label="招投标" :value="3" />
            <el-option label="地图" :value="4" />
            <el-option label="自媒体" :value="5" />
            <el-option label="转介绍" :value="6" />
            <el-option label="其他" :value="99" />
          </el-select>
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="form.remarks" type="textarea" :rows="3" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showCreate = false">取消</el-button>
        <el-button type="primary" @click="handleCreate" :loading="creating">创建</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="showConvert" title="转化为客户主档" width="600px">
      <el-form :model="convertForm" label-width="100px">
        <el-form-item label="公司全称" required>
          <el-input v-model="convertForm.fullName" />
        </el-form-item>
        <el-form-item label="统一社会信用代码">
          <el-input v-model="convertForm.creditCode" maxlength="18" />
        </el-form-item>
        <el-form-item label="所在城市" required>
          <el-select v-model="convertForm.city">
            <el-option label="潍坊" :value="0" />
            <el-option label="青岛" :value="1" />
            <el-option label="烟台" :value="2" />
            <el-option label="济南" :value="3" />
            <el-option label="其他" :value="99" />
          </el-select>
        </el-form-item>
        <el-form-item label="所属行业" required>
          <el-select v-model="convertForm.industry">
            <el-option label="机械制造" :value="0" />
            <el-option label="纺织服装" :value="1" />
            <el-option label="食品加工" :value="2" />
            <el-option label="化工" :value="3" />
            <el-option label="建材" :value="4" />
            <el-option label="农业种植" :value="5" />
            <el-option label="商贸流通" :value="6" />
            <el-option label="服务业" :value="7" />
            <el-option label="其他" :value="99" />
          </el-select>
        </el-form-item>
        <el-form-item label="是否有官网">
          <el-switch v-model="convertForm.hasWebsite" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showConvert = false">取消</el-button>
        <el-button type="primary" @click="handleConvertConfirm" :loading="converting">确认转化</el-button>
      </template>
    </el-dialog>

    <!-- 批量导入对话框 -->
    <el-dialog v-model="showImport" title="批量导入线索" width="600px">
      <el-alert type="info" :closable="false" style="margin-bottom:12px;">
        <template #title>
          Excel 列名：公司名称、统一社会信用代码、官网域名、联系人、手机、邮箱、来源、备注。公司名称必填，信用代码用于去重。
        </template>
      </el-alert>
      <el-upload
        drag
        :auto-upload="false"
        :on-change="handleFileChange"
        :limit="1"
        accept=".xls,.xlsx,.csv"
      >
        <el-icon class="el-icon--upload"><upload-filled /></el-icon>
        <div class="el-upload__text">将 Excel 文件拖到此处，或<em>点击选择</em></div>
      </el-upload>
      <div v-if="parsedRows.length > 0" style="margin-top:12px;">
        <el-descriptions :column="2" border size="small">
          <el-descriptions-item label="解析行数">{{ parsedRows.length }}</el-descriptions-item>
          <el-descriptions-item label="去重后提交">{{ dedupedRows.length }}</el-descriptions-item>
          <el-descriptions-item label="Excel内重复">{{ parsedRows.length - dedupedRows.length }}</el-descriptions-item>
          <el-descriptions-item label="无效行(缺公司名)" :span="2">{{ invalidRows }}</el-descriptions-item>
        </el-descriptions>
      </div>
      <div v-if="importResult" style="margin-top:12px;">
        <el-result icon="success" title="导入完成" :sub-title="`共${importResult.total}条，新增${importResult.imported}条，重复跳过${importResult.skipped}条，失败${importResult.failed}条`" />
      </div>
      <template #footer>
        <el-button @click="showImport = false">关闭</el-button>
        <el-button type="primary" @click="handleImport" :loading="importing" :disabled="dedupedRows.length === 0">开始导入</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="showAssign" title="分配线索" width="420px">
      <div style="margin-bottom:12px;color:#666;">
        企业：{{ currentLead?.companyName }}
      </div>
      <el-select v-model="assignTarget" placeholder="选择负责人" style="width:100%">
        <el-option v-for="m in teamMembers" :key="m.id" :label="(m.displayName || m.userName)" :value="m.id" />
      </el-select>
      <template #footer>
        <el-button @click="showAssign = false">取消</el-button>
        <el-button type="primary" @click="confirmAssign">确认分配</el-button>
      </template>
    </el-dialog>

    <!-- 标签编辑对话框 -->
    <el-dialog v-model="showTagsDialog" title="编辑标签" width="400px">
      <div style="margin-bottom:12px">
        <el-input
          v-model="tagsInput"
          placeholder="输入标签后按回车添加"
          @keyup.enter="addTag"
        >
          <template #append>
            <el-button @click="addTag">添加</el-button>
          </template>
        </el-input>
      </div>
      <div style="min-height:60px">
        <el-tag
          v-for="tag in tagsList"
          :key="tag"
          closable
          style="margin:4px"
          @close="removeTag(tag)"
        >
          {{ tag }}
        </el-tag>
        <span v-if="tagsList.length === 0" style="color:#909399;font-size:13px">暂无标签</span>
      </div>
      <div style="margin-top:12px;font-size:12px;color:#909399">
        常用标签：
        <el-link v-for="t in ['百度推广','重点客户','已报价','待回访','高意向','无官网','GEO低分']" :key="t" type="primary" style="margin-right:8px" @click="() => { if(!tagsList.includes(t)) tagsList.push(t); }">{{t}}</el-link>
      </div>
      <template #footer>
        <el-button @click="showTagsDialog = false">取消</el-button>
        <el-button type="primary" @click="saveTags">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Plus, Search, Refresh, Upload, UploadFilled, Download } from '@element-plus/icons-vue';
import { exportToExcel } from '../../utils/exportExcel';
import { listLeads, createLead, deleteLead, claimLead, assignLead, discardLead, convertLead, batchImportLeads, listTeamMembers, updateLeadTags } from '../../api/crm';
import type { LeadDto, CreateLeadRequest, ConvertLeadRequest } from '../../types/crm';
import type { BatchImportResult, TeamMember } from '../../api/crm';
import { readSpreadsheetRows } from '../../utils/spreadsheet';

const route = useRoute();

// 行业自动推断：根据企业名称关键词
function inferIndustry(companyName: string, explicitIndustry?: string): number {
  // Excel 中有明确行业列时优先使用
  if (explicitIndustry) {
    const map: Record<string, number> = {
      '机械': 0, '机械制造': 0, '设备': 0,
      '纺织': 1, '服装': 1,
      '食品': 2, '食品加工': 2, '餐饮': 2,
      '化工': 3, '化学': 3,
      '建材': 4, '建筑材料': 4,
      '农业': 5, '种植': 5, '养殖': 5,
      '商贸': 6, '贸易': 6, '进出口': 6,
      '服务': 7, '服务业': 7, '科技': 7,
    };
    for (const [key, val] of Object.entries(map)) {
      if (explicitIndustry.includes(key)) return val;
    }
  }
  const name = companyName;
  const rules: Array<{ keywords: string[]; industry: number }> = [
    { keywords: ['食品', '餐饮', '粮油', '饮料', '调味', '肉食', '水产', '乳业', '烘焙', '糖果', '茶叶', '农产品加工'], industry: 2 },
    { keywords: ['机械', '设备', '重工', '机床', '泵阀', '轴承', '齿轮', '电机', '模具', '五金', '铸造', '锻造'], industry: 0 },
    { keywords: ['纺织', '服装', '印染', '面料', '纱线', '家纺', '针织', '制衣'], industry: 1 },
    { keywords: ['化工', '化学', '涂料', '塑料', '橡胶', '化肥', '农药', '新材料', '精细化工'], industry: 3 },
    { keywords: ['建材', '水泥', '陶瓷', '玻璃', '石材', '保温', '装饰', '门窗', '管材'], industry: 4 },
    { keywords: ['农业', '种植', '养殖', '苗木', '果蔬', '畜牧', '饲料', '种业'], industry: 5 },
    { keywords: ['贸易', '商贸', '进出口', '销售', '经销', '供应链', '商贸有限公司'], industry: 6 },
    { keywords: ['服务', '咨询', '物流', '科技', '网络', '传媒', '广告', '软件', '信息'], industry: 7 },
  ];
  for (const rule of rules) {
    if (rule.keywords.some(k => name.includes(k))) return rule.industry;
  }
  return 99; // 其他
}

const loading = ref(false);
const creating = ref(false);
const converting = ref(false);
const list = ref<LeadDto[]>([]);
const total = ref(0);
const showCreate = ref(false);
const showConvert = ref(false);
const showImport = ref(false);
const currentLead = ref<LeadDto | null>(null);
const importing = ref(false);
const parsedRows = ref<CreateLeadRequest[]>([]);
const dedupedRows = ref<CreateLeadRequest[]>([]);
const invalidRows = ref(0);
const importResult = ref<BatchImportResult | null>(null);
const teamMembers = ref<TeamMember[]>([]);
const assignTarget = ref<number | null>(null);
const showAssign = ref(false);

const filters = reactive({
  keyword: '',
  status: undefined as number | undefined,
  rating: undefined as number | undefined,
  ownerId: undefined as number | undefined,
  subjectType: undefined as number | undefined,
  tag: '',
  page: 1,
  pageSize: 20
});

// 标签编辑
const showTagsDialog = ref(false);
const tagsEditLead = ref<LeadDto | null>(null);
const tagsInput = ref('');
const tagsList = ref<string[]>([]);

// 当前登录用户 id（用于"我的线索"过滤）
const currentUserId = ref<number | null>(null);

function ownerName(ownerId?: number | null): string {
  if (!ownerId) return '-';
  const m = teamMembers.value.find(x => x.id === Number(ownerId));
  return m ? (m.displayName || m.userName) : `#${ownerId}`;
}

async function handleAssign(row: LeadDto) {
  currentLead.value = row;
  assignTarget.value = row.ownerId ? Number(row.ownerId) : null;
  showAssign.value = true;
}

function handleEditTags(row: LeadDto) {
  tagsEditLead.value = row;
  tagsList.value = [...(row.tags || [])];
  tagsInput.value = '';
  showTagsDialog.value = true;
}

function addTag() {
  const t = tagsInput.value.trim();
  if (t && !tagsList.value.includes(t)) {
    tagsList.value.push(t);
  }
  tagsInput.value = '';
}

function removeTag(tag: string) {
  tagsList.value = tagsList.value.filter(t => t !== tag);
}

async function saveTags() {
  if (!tagsEditLead.value) return;
  try {
    await updateLeadTags(String(tagsEditLead.value.id), tagsList.value);
    tagsEditLead.value.tags = [...tagsList.value];
    ElMessage.success('标签已更新');
    showTagsDialog.value = false;
  } catch (e: any) {
    ElMessage.error(e?.response?.data?.detail || '更新失败');
  }
}

async function confirmAssign() {
  if (!currentLead.value || !assignTarget.value) {
    ElMessage.warning('请选择负责人');
    return;
  }
  try {
    await assignLead(currentLead.value.id, assignTarget.value);
    ElMessage.success('分配成功');
    showAssign.value = false;
    loadData();
  } catch (e: any) {
    ElMessage.error(e.message || '分配失败');
  }
}

function filterMine() {
  if (currentUserId.value) {
    filters.ownerId = filters.ownerId === currentUserId.value ? undefined : currentUserId.value;
    filters.page = 1;
    loadData();
  }
}

const form = reactive<CreateLeadRequest>({
  companyName: '',
  creditCode: '',
  websiteDomain: '',
  contactName: '',
  mobile: '',
  email: '',
  source: 99,
  remarks: ''
});

const convertForm = reactive<ConvertLeadRequest>({
  leadId: '',
  fullName: '',
  creditCode: '',
  city: 0,
  industry: 0,
  hasWebsite: false
});

async function loadData() {
  loading.value = true;
  try {
    const result = await listLeads(filters);
    list.value = result.items;
    total.value = result.total;
  } catch (e: any) {
    ElMessage.error(e.message || '加载失败');
  } finally {
    loading.value = false;
  }
}

function resetFilters() {
  filters.keyword = '';
  filters.status = undefined;
  filters.rating = undefined;
  filters.ownerId = undefined;
  filters.tag = '';
  filters.page = 1;
  loadData();
}

onMounted(async () => {
  if (typeof route.query.keyword === 'string') {
    filters.keyword = route.query.keyword;
  }
  try {
    teamMembers.value = await listTeamMembers();
    // 从 localStorage 或 auth 获取当前用户 id
    const auth = localStorage.getItem('auth');
    if (auth) {
      const parsed = JSON.parse(auth);
      currentUserId.value = parsed.id || parsed.userId || null;
    }
  } catch { /* 忽略 */ }
  loadData();
});

function handleExport() {
  if (list.value.length === 0) { ElMessage.warning('没有可导出的数据'); return; }
  exportToExcel(list.value, [
    { key: 'companyName', label: '公司名称' },
    { key: 'subjectType', label: '主体类型' },
    { key: 'industry', label: '行业' },
    { key: 'contactName', label: '联系人' },
    { key: 'mobile', label: '手机' },
    { key: 'phone', label: '电话' },
    { key: 'email', label: '邮箱' },
    { key: 'websiteDomain', label: '官网' },
    { key: 'address', label: '地址' },
    { key: 'status', label: '状态' },
    { key: 'rating', label: '评级' },
    { key: 'aiScore', label: 'AI评分' },
    { key: 'source', label: '来源' },
    { key: 'ownerId', label: '负责人ID' },
    { key: 'createdAt', label: '创建时间' },
    { key: 'remarks', label: '备注' },
  ], '线索池导出');
  ElMessage.success(`已导出 ${list.value.length} 条线索`);
}

async function handleCreate() {
  if (!form.companyName.trim()) {
    ElMessage.warning('请输入公司名称');
    return;
  }
  creating.value = true;
  try {
    await createLead(form);
    ElMessage.success('创建成功');
    showCreate.value = false;
    Object.assign(form, { companyName: '', creditCode: '', websiteDomain: '', contactName: '', mobile: '', email: '', source: 99, remarks: '' });
    loadData();
  } catch (e: any) {
    ElMessage.error(e.message || '创建失败');
  } finally {
    creating.value = false;
  }
}

async function handleClaim(row: LeadDto) {
  try {
    await claimLead(row.id);
    ElMessage.success('认领成功');
    loadData();
  } catch (e: any) {
    ElMessage.error(e.message || '认领失败');
  }
}

function handleConvert(row: LeadDto) {
  currentLead.value = row;
  convertForm.leadId = row.id;
  convertForm.fullName = row.companyName;
  showConvert.value = true;
}

async function handleConvertConfirm() {
  if (!convertForm.fullName.trim()) {
    ElMessage.warning('请输入公司全称');
    return;
  }
  converting.value = true;
  try {
    await convertLead(currentLead.value!.id, convertForm);
    ElMessage.success('转化成功，已创建客户主档');
    showConvert.value = false;
    loadData();
  } catch (e: any) {
    ElMessage.error(e.message || '转化失败');
  } finally {
    converting.value = false;
  }
}

async function handleDiscard(row: LeadDto) {
  try {
    await ElMessageBox.confirm('确认废弃该线索？', '提示', { type: 'warning' });
    await discardLead(row.id);
    ElMessage.success('已废弃');
    loadData();
  } catch { /* cancelled */ }
}

async function handleDelete(row: LeadDto) {
  try {
    await ElMessageBox.confirm('确认删除该线索？', '提示', { type: 'warning' });
    await deleteLead(row.id);
    ElMessage.success('删除成功');
    loadData();
  } catch { /* cancelled */ }
}

// ===== 批量导入 =====
function normalizeCreditCode(code?: string): string {
  return (code || '').trim().toUpperCase().replace(/\s/g, '');
}

async function handleFileChange(file: any) {
  importResult.value = null;
  try {
    const json = await readSpreadsheetRows(file.raw);
      const rows: CreateLeadRequest[] = [];
      let invalid = 0;
      for (const r of json) {
        const companyName = String(r['公司名称'] || r['companyName'] || r['企业名称'] || '').trim();
        if (!companyName) { invalid++; continue; }
        const explicitIndustry = String(r['行业'] || r['所属行业'] || r['industry'] || '').trim();
        rows.push({
          companyName,
          creditCode: normalizeCreditCode(String(r['统一社会信用代码'] || r['creditCode'] || r['信用代码'] || '')),
          websiteDomain: String(r['官网域名'] || r['websiteDomain'] || r['官网'] || '').trim().toLowerCase(),
          contactName: String(r['联系人'] || r['contactName'] || '').trim(),
          mobile: String(r['手机'] || r['mobile'] || r['联系电话'] || '').trim(),
          email: String(r['邮箱'] || r['email'] || '').trim(),
          industry: inferIndustry(companyName, explicitIndustry),
          source: 0, // 存量Excel
          remarks: String(r['备注'] || r['remarks'] || '').trim()
        });
      }
      parsedRows.value = rows;
      invalidRows.value = invalid;
      // 前端预去重：信用代码 > 公司名 > 域名 > 手机
      const seen = new Set<string>();
      const deduped: CreateLeadRequest[] = [];
      for (const row of rows) {
        let key = '';
        if (row.creditCode) key = 'cc:' + row.creditCode;
        else if (row.companyName) key = 'name:' + row.companyName;
        else if (row.websiteDomain) key = 'dom:' + row.websiteDomain;
        else if (row.mobile) key = 'mob:' + row.mobile;
        else key = 'row:' + Math.random();
        if (!seen.has(key)) { seen.add(key); deduped.push(row); }
      }
      dedupedRows.value = deduped;
      ElMessage.success(`解析成功：${rows.length}条有效，去重后${deduped.length}条`);
  } catch (err: any) {
    ElMessage.error('Excel解析失败：' + (err.message || '格式错误'));
  }
}

async function handleImport() {
  if (dedupedRows.value.length === 0) { ElMessage.warning('没有可导入的数据'); return; }
  importing.value = true;
  try {
    const result = await batchImportLeads(dedupedRows.value);
    importResult.value = result;
    ElMessage.success(`导入完成：新增${result.imported}条，跳过${result.skipped}条`);
    loadData();
  } catch (e: any) {
    ElMessage.error(e.message || '导入失败');
  } finally {
    importing.value = false;
  }
}

function statusLabel(s: number) { return ['待认领', '已认领', '已转化', '已废弃'][s] || '未知'; }
function statusTagType(s: number) { return ['info', 'warning', 'success', 'danger'][s] || 'info'; }
function subjectTypeLabel(s: number) {
  const map: Record<number, string> = { 0: '企业', 1: '协会/商会', 2: '服务中心', 3: '专业服务', 4: '园区/基地', 5: '政府机关', 6: '事业单位', 7: '学历教育', 8: '教育培训', 10: '个体商户/门店', 99: '其他' };
  return map[s] || '其他';
}
function subjectTypeTagType(s: number) {
  // 企业+教培+个体商户=success(绿)，渠道机会=warning(橙)，学历教育+事业单位+政府=info(灰)
  if (s === 0 || s === 8 || s === 10) return 'success';
  if (s >= 1 && s <= 4) return 'warning';
  return 'info';
}
function ratingLabel(r: number) { return ['A', 'B', 'C', 'D'][r] || '-'; }
function ratingTagType(r: number) { return ['success', 'warning', 'info', 'danger'][r] || 'info'; }
function scoreTagType(score?: number) {
  if (score === undefined) return 'info';
  if (score >= 70) return 'success';
  if (score >= 50) return 'warning';
  return 'info';
}
function formatDate(d: string) { return new Date(d).toLocaleString('zh-CN'); }
</script>

<style scoped>
.crm-lead-list { padding: 16px; }
.card-header { display: flex; justify-content: space-between; align-items: center; }
.title { font-size: 16px; font-weight: 600; }
.search-form { margin-bottom: 16px; }
.pagination { margin-top: 16px; display: flex; justify-content: flex-end; }
</style>
