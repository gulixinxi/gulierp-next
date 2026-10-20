<template>
  <div class="crm-company-detail" v-loading="loading">
    <el-page-header @back="goBack" :content="company?.fullName || '客户详情'" />

    <el-card v-if="company" shadow="never" class="info-card">
      <el-descriptions :column="3" border>
        <el-descriptions-item label="客户编号">{{ company.companyNo }}</el-descriptions-item>
        <el-descriptions-item label="统一社会信用代码">{{ company.creditCode || '-' }}</el-descriptions-item>
        <el-descriptions-item label="所在城市">{{ cityLabel(company.city) }}</el-descriptions-item>
        <el-descriptions-item label="所属行业">{{ industryLabel(company.industry) }}</el-descriptions-item>
        <el-descriptions-item label="企业规模">{{ scaleLabel(company.scale) }}</el-descriptions-item>
        <el-descriptions-item label="客户状态">
          <el-tag :type="statusTagType(company.status)">{{ statusLabel(company.status) }}</el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="意向评级">
          <el-tag v-if="company.rating !== undefined" :type="ratingTagType(company.rating)">{{ ratingLabel(company.rating) }}</el-tag>
          <span v-else>-</span>
        </el-descriptions-item>
        <el-descriptions-item label="是否有官网">{{ company.hasWebsite ? '是' : '否' }}</el-descriptions-item>
        <el-descriptions-item label="官网域名">{{ company.websiteDomain || '-' }}</el-descriptions-item>
        <el-descriptions-item label="详细地址" :span="3">{{ company.address || '-' }}</el-descriptions-item>
        <el-descriptions-item label="备注" :span="3">{{ company.remarks || '-' }}</el-descriptions-item>
      </el-descriptions>
    </el-card>

    <el-tabs v-model="activeTab" class="detail-tabs">
      <!-- 联系人 -->
      <el-tab-pane label="联系人" name="contacts">
        <el-card shadow="never">
          <el-alert v-if="contactsError" type="warning" :closable="false" title="联系人未加载；请检查联系人读取权限。" />
          <template #header>
            <div class="tab-header">
              <span>联系人列表</span>
              <el-button v-if="can('crm.contact.manage')" type="primary" size="small" @click="showContactForm = true">添加联系人</el-button>
            </div>
          </template>
          <el-table :data="contacts" stripe>
            <el-table-column prop="name" label="姓名" width="100" />
            <el-table-column prop="position" label="职务" width="120" />
            <el-table-column prop="mobile" label="手机" width="130" />
            <el-table-column prop="wechat" label="微信" width="120" />
            <el-table-column prop="email" label="邮箱" width="180" />
            <el-table-column label="关键人" width="80">
              <template #default="{ row }">
                <el-tag v-if="row.isKeyPerson" type="success">是</el-tag>
                <span v-else>否</span>
              </template>
            </el-table-column>
            <el-table-column v-if="can('crm.contact.manage')" label="操作" width="100">
              <template #default="{ row }">
                <el-button link type="danger" size="small" @click="handleDeleteContact(row)">删除</el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-tab-pane>

      <!-- 商机 -->
      <el-tab-pane label="商机" name="deals">
        <el-card shadow="never">
          <el-alert v-if="dealsError" type="warning" :closable="false" title="商机未加载；请检查商机读取权限。" />
          <template #header>
            <div class="tab-header">
              <span>商机列表</span>
              <el-button v-if="can('crm.deal.manage')" type="primary" size="small" @click="showDealForm = true">新建商机</el-button>
            </div>
          </template>
          <el-table :data="deals" stripe>
            <el-table-column prop="dealNo" label="商机编号" width="140" />
            <el-table-column prop="name" label="商机名称" min-width="180" />
            <el-table-column label="业务品类" width="100">
              <template #default="{ row }">{{ categoryLabel(row.category) }}</template>
            </el-table-column>
            <el-table-column prop="expectedAmount" label="预计金额" width="120">
              <template #default="{ row }">¥{{ row.expectedAmount.toLocaleString() }}</template>
            </el-table-column>
            <el-table-column label="阶段" width="100">
              <template #default="{ row }">{{ stageLabel(row.stage) }}</template>
            </el-table-column>
            <el-table-column label="状态" width="90">
              <template #default="{ row }">
                <el-tag :type="dealStatusTagType(row.status)">{{ dealStatusLabel(row.status) }}</el-tag>
              </template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-tab-pane>

      <!-- 跟进记录 -->
      <el-tab-pane label="跟进记录" name="activities">
        <el-card shadow="never">
          <el-alert v-if="activitiesError" type="warning" :closable="false" title="跟进记录未加载；请检查跟进读取权限。" />
          <template #header>
            <div class="tab-header">
              <span>跟进记录</span>
              <el-button v-if="can('crm.activity.manage')" type="primary" size="small" @click="showActivityForm = true">新增跟进</el-button>
            </div>
          </template>
          <el-timeline>
            <el-timeline-item
              v-for="act in activities"
              :key="act.id"
              :timestamp="formatDate(act.followAt)"
              placement="top"
            >
              <el-card shadow="never" class="activity-card">
                <div class="activity-header">
                  <el-tag size="small">{{ followTypeLabel(act.followType) }}</el-tag>
                  <span class="activity-content">{{ act.content }}</span>
                </div>
                <div class="activity-next" v-if="act.nextAction">
                  <strong>下次动作：</strong>{{ act.nextAction }}
                  <span v-if="act.nextActionDate" class="next-date">（{{ act.nextActionDate }}）</span>
                </div>
              </el-card>
            </el-timeline-item>
          </el-timeline>
          <el-empty v-if="activities.length === 0" description="暂无跟进记录" />
        </el-card>
      </el-tab-pane>

      <!-- L2 销售情报卡 -->
      <el-tab-pane label="销售情报" name="intelligence">
        <div v-loading="intelLoading" class="intel-card">
          <!-- 推荐切入点 -->
          <el-card shadow="never" style="margin-bottom:12px">
            <template #header><b>推荐切入点</b></template>
            <el-timeline>
              <el-timeline-item v-for="(p, i) in getEntryPoints()" :key="i" :timestamp="`角度${i+1}`" placement="top">
                {{ p }}
              </el-timeline-item>
            </el-timeline>
          </el-card>

          <!-- 联系话术 -->
          <el-card shadow="never" style="margin-bottom:12px">
            <template #header>
              <div style="display:flex;justify-content:space-between;align-items:center">
                <b>联系话术建议</b>
                <el-button size="small" link type="primary" @click="copyTalkTrack">复制话术</el-button>
              </div>
            </template>
            <p style="line-height:1.8;color:#606266;margin:0">{{ getTalkTrack() }}</p>
          </el-card>

          <!-- 商机建议 -->
          <el-card shadow="never" style="margin-bottom:12px" v-if="suggestions.length > 0">
            <template #header><b>商机建议（{{ suggestions.length }}条）</b></template>
            <el-table :data="suggestions" size="small">
              <el-table-column prop="opportunityType" label="机会类型" width="110" />
              <el-table-column prop="reason" label="推荐理由" min-width="200" show-overflow-tooltip />
              <el-table-column prop="priority" label="优先级" width="70">
                <template #default="{ row }"><el-tag size="small" :type="row.priority==='高'?'danger':row.priority==='中高'?'warning':'info'">{{ row.priority }}</el-tag></template>
              </el-table-column>
              <el-table-column prop="suggestedAmount" label="建议报价" width="110" />
            </el-table>
          </el-card>

          <!-- 企业情报信号 -->
          <el-card shadow="never" v-if="signals.length > 0">
            <template #header><b>企业情报信号（{{ signals.length }}条）</b></template>
            <el-table :data="signals" size="small">
              <el-table-column label="类型" width="90">
                <template #default="{ row }">{{ ['网站异常','官网缺失','GEO变化','ICP变动','许可证到期','政府处罚','招标采购','政策补贴','招聘扩张','外贸信息','其他'][row.type] || row.type }}</template>
              </el-table-column>
              <el-table-column prop="title" label="标题" min-width="180" show-overflow-tooltip />
              <el-table-column prop="source" label="来源" width="80" />
              <el-table-column label="可信度" width="80">
                <template #default="{ row }">{{ row.confidence }}%</template>
              </el-table-column>
              <el-table-column label="状态" width="80">
                <template #default="{ row }"><el-tag size="small" :type="['warning','success','info','primary'][row.status]">{{ ['待处理','已确认','已忽略','已转商机'][row.status] }}</el-tag></template>
              </el-table-column>
            </el-table>
          </el-card>

          <el-empty v-if="!intelLoading && signals.length === 0 && suggestions.length === 0" description="暂无情报数据，运行线索自动化后自动生成" />
        </div>
      </el-tab-pane>

      <el-tab-pane v-if="can('mdm.business-partner.read')" label="ERP客商候选" name="erp-match">
        <el-card shadow="never">
          <el-alert type="info" :closable="false" title="仅查询可能匹配的ERP客商；结果需人工核对，不表示已关联或已同步。" />
          <div style="margin: 16px 0">
            <el-button type="primary" :loading="erpMatchLoading" @click="loadErpMatch">查询候选</el-button>
          </div>
          <el-alert v-if="erpMatchError" type="warning" :closable="false" :title="erpMatchError" />
          <template v-if="erpMatch">
            <el-empty v-if="erpMatch.candidateCount === 0" description="暂无候选客商" />
            <el-table v-else :data="erpMatch.candidates" stripe>
              <el-table-column prop="erpPartnerCode" label="ERP客商编号" width="150" />
              <el-table-column prop="erpPartnerName" label="名称" min-width="180" />
              <el-table-column prop="taxNumber" label="统一社会信用代码" min-width="170" />
              <el-table-column label="匹配方式" width="130">
                <template #default="{ row }">{{ row.matchMethod === 'TaxNumber' ? '信用代码' : '名称' }}</template>
              </el-table-column>
              <el-table-column prop="matchConfidence" label="匹配分" width="100" />
            </el-table>
          </template>
        </el-card>
      </el-tab-pane>
    </el-tabs>

    <!-- 添加联系人对话框 -->
    <el-dialog v-model="showContactForm" title="添加联系人" width="500px">
      <el-form :model="contactForm" label-width="80px">
        <el-form-item label="姓名" required>
          <el-input v-model="contactForm.name" />
        </el-form-item>
        <el-form-item label="职务">
          <el-input v-model="contactForm.position" />
        </el-form-item>
        <el-form-item label="手机">
          <el-input v-model="contactForm.mobile" />
        </el-form-item>
        <el-form-item label="微信">
          <el-input v-model="contactForm.wechat" />
        </el-form-item>
        <el-form-item label="邮箱">
          <el-input v-model="contactForm.email" />
        </el-form-item>
        <el-form-item label="设为关键人">
          <el-switch v-model="contactForm.isKeyPerson" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showContactForm = false">取消</el-button>
        <el-button type="primary" @click="handleAddContact">保存</el-button>
      </template>
    </el-dialog>

    <!-- 新增跟进对话框 -->
    <el-dialog v-model="showActivityForm" title="新增跟进记录" width="550px">
      <el-form :model="activityForm" label-width="100px">
        <el-form-item label="跟进方式" required>
          <el-select v-model="activityForm.followType">
            <el-option label="电话" :value="0" />
            <el-option label="微信" :value="1" />
            <el-option label="拜访" :value="2" />
            <el-option label="邮件" :value="3" />
            <el-option label="会议" :value="4" />
            <el-option label="其他" :value="99" />
          </el-select>
        </el-form-item>
        <el-form-item label="跟进内容" required>
          <el-input v-model="activityForm.content" type="textarea" :rows="3" placeholder="写对方说了什么，不是我做了什么" />
        </el-form-item>
        <el-form-item label="客户态度">
          <el-select v-model="activityForm.attitude" clearable>
            <el-option label="积极" :value="0" />
            <el-option label="中性" :value="1" />
            <el-option label="消极" :value="2" />
            <el-option label="未明确" :value="3" />
          </el-select>
        </el-form-item>
        <el-form-item label="下次动作" required>
          <el-input v-model="activityForm.nextAction" placeholder="具体动作，如发GEO检测报告" />
        </el-form-item>
        <el-form-item label="下次动作日期">
          <el-date-picker v-model="activityForm.nextActionDate" type="date" value-format="YYYY-MM-DD" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showActivityForm = false">取消</el-button>
        <el-button type="primary" @click="handleAddActivity">保存</el-button>
      </template>
    </el-dialog>

    <!-- 新建商机对话框 -->
    <el-dialog v-model="showDealForm" title="新建商机" width="520px">
      <el-form :model="dealForm" label-width="90px">
        <el-form-item label="商机名称" required>
          <el-input v-model="dealForm.name" placeholder="如：官网重建项目" />
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
        <el-button type="primary" @click="handleAddDeal">创建</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage } from 'element-plus';
import { getCompany, getErpMatchCandidates, getContactsByCompany, createContact, deleteContact, getActivitiesByCompany, createActivity, listDeals, createDeal, listSignals, getOpportunitySuggestions } from '../../api/crm';
import type { SignalDto, OpportunitySuggestion } from '../../api/crm';
import type { CompanyDto, ContactDto, ActivityDto, DealDto, CreateContactRequest, CreateActivityRequest, CreateDealRequest, ErpMatchResultDto } from '../../types/crm';
import { BusinessCategory, DealStage } from '../../types/crm';
import { useAuthStore } from '../../stores/auth';
import { useCrmPermission } from '../../composables/useCrmPermission';

const route = useRoute();
const router = useRouter();
const auth = useAuthStore();
const can = useCrmPermission();
const companyId = route.params.id as string;

const loading = ref(false);
const activeTab = ref('contacts');
const company = ref<CompanyDto | null>(null);
const contacts = ref<ContactDto[]>([]);
const activities = ref<ActivityDto[]>([]);
const deals = ref<DealDto[]>([]);
const contactsError = ref(false);
const activitiesError = ref(false);
const dealsError = ref(false);
const signals = ref<SignalDto[]>([]);
const suggestions = ref<OpportunitySuggestion[]>([]);
const intelLoading = ref(false);
const erpMatch = ref<ErpMatchResultDto | null>(null);
const erpMatchLoading = ref(false);
const erpMatchError = ref('');

const showContactForm = ref(false);
const showDealForm = ref(false);
const showActivityForm = ref(false);

const contactForm = reactive<CreateContactRequest>({
  crmCompanyId: companyId,
  name: '',
  position: '',
  mobile: '',
  wechat: '',
  email: '',
  isKeyPerson: false
});

const activityForm = reactive<CreateActivityRequest>({
  crmCompanyId: companyId,
  followType: 0,
  content: '',
  attitude: undefined,
  nextAction: '',
  nextActionDate: undefined
});

const dealForm = reactive<CreateDealRequest>({
  name: '',
  crmCompanyId: companyId,
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
    company.value = await getCompany(companyId);
    const [conts, acts, dls] = await Promise.allSettled([
      getContactsByCompany(companyId),
      getActivitiesByCompany(companyId),
      listDeals({ companyId, page: 1, pageSize: 100 })
    ]);
    contactsError.value = conts.status === 'rejected';
    activitiesError.value = acts.status === 'rejected';
    dealsError.value = dls.status === 'rejected';
    contacts.value = conts.status === 'fulfilled' ? conts.value : [];
    activities.value = acts.status === 'fulfilled' ? acts.value : [];
    deals.value = dls.status === 'fulfilled' ? dls.value.items : [];
  } catch (e: any) {
    ElMessage.error(e.message || '加载失败');
  } finally {
    loading.value = false;
  }
}

function goBack() {
  router.push('/crm/companies');
}

async function loadErpMatch() {
  erpMatchLoading.value = true;
  erpMatchError.value = '';
  erpMatch.value = null;
  try {
    erpMatch.value = await getErpMatchCandidates(companyId);
  } catch (error: any) {
    erpMatchError.value = error?.status === 403
      ? '需要同时具备 CRM 客户读取和 ERP 客商读取权限。'
      : error?.status === 404
        ? '客户不存在或无权访问。'
        : '候选查询失败，请稍后重试。';
  } finally {
    erpMatchLoading.value = false;
  }
}

// ===== L2 销售情报卡 =====
async function loadIntelligence() {
  intelLoading.value = true;
  try {
    const comp = company.value;
    if (!comp) return;
    // 该企业的信号
    const sigRes = await listSignals({ keyword: comp.fullName, pageSize: 50 });
    signals.value = sigRes.items.filter(s => s.companyName === comp.fullName);
    // 该企业的商机建议
    const sugRes = await getOpportunitySuggestions();
    suggestions.value = sugRes.items.filter(s => s.targetName === comp.fullName);
  } catch (e) {
    console.error('加载情报失败', e);
  } finally {
    intelLoading.value = false;
  }
}

// 推荐切入点（基于企业特征规则生成）
function getEntryPoints(): string[] {
  const comp = company.value;
  if (!comp) return [];
  const points: string[] = [];
  if (!comp.hasWebsite && !comp.websiteDomain) {
    points.push('企业暂无官网，建议从建站服务切入，报价5,000-20,000元');
  }
  if (comp.websiteStatus === 1 || comp.websiteStatus === 4) {
    points.push('官网状态异常（打不开/内容陈旧），建议从网站重建切入');
  }
  if (comp.industry === 2) points.push('食品加工行业，可结合食品安全追溯、AI质检等数字化场景');
  if (comp.industry === 0) points.push('机械制造行业，可结合设备AI巡检、生产数字化等场景');
  if (comp.industry === 3) points.push('化工行业，可结合EHS合规、安全数字化等场景');
  if (signals.value.length > 0) {
    points.push(`发现${signals.value.length}条企业情报信号，可作为联系切入点`);
  }
  if (points.length === 0) points.push('建议先电话了解企业当前数字化需求和预算');
  return points;
}

// 联系话术建议
function getTalkTrack(): string {
  const comp = company.value;
  if (!comp) return '';
  const industry = industryLabel(comp.industry);
  if (!comp.hasWebsite) {
    return `您好，我们是谷粒信息，专注${industry}企业的网站建设和数字化服务。注意到贵司目前没有官方网站，现在很多客户通过AI搜索找供应商，没有官网会丢失不少机会。我们可以帮您做一个适配AI搜索的企业官网，方便约个时间详细聊聊吗？`;
  }
  return `您好，我们是谷粒信息，专注企业AI可见性优化（GEO）。我们检测到贵司官网在豆包、DeepSeek等AI搜索中的可见性较低，潜在客户通过AI搜索时很难找到您。我们可以做一次免费诊断，方便约个时间聊聊吗？`;
}

function copyTalkTrack() {
  navigator.clipboard?.writeText(getTalkTrack()).then(() => ElMessage.success('话术已复制'));
}

async function handleAddContact() {
  if (!contactForm.name.trim()) {
    ElMessage.warning('请输入姓名');
    return;
  }
  try {
    await createContact(contactForm);
    ElMessage.success('添加成功');
    showContactForm.value = false;
    Object.assign(contactForm, { name: '', position: '', mobile: '', wechat: '', email: '', isKeyPerson: false });
    loadData();
  } catch (e: any) {
    ElMessage.error(e.message || '添加失败');
  }
}

async function handleDeleteContact(row: ContactDto) {
  try {
    await deleteContact(row.id);
    ElMessage.success('删除成功');
    loadData();
  } catch (e: any) {
    ElMessage.error(e.message || '删除失败');
  }
}

async function handleAddActivity() {
  if (!activityForm.content.trim()) {
    ElMessage.warning('请输入跟进内容');
    return;
  }
  if (!activityForm.nextAction.trim()) {
    ElMessage.warning('请输入下次动作');
    return;
  }
  try {
    activityForm.ownerId = auth.user?.userId;
    await createActivity(activityForm);
    ElMessage.success('添加成功');
    showActivityForm.value = false;
    Object.assign(activityForm, { content: '', attitude: undefined, nextAction: '', nextActionDate: undefined });
    loadData();
  } catch (e: any) {
    ElMessage.error(e.message || '添加失败');
  }
}

async function handleAddDeal() {
  if (!dealForm.name.trim()) {
    ElMessage.warning('请输入商机名称');
    return;
  }
  if (!dealForm.expectedAmount || dealForm.expectedAmount <= 0) {
    ElMessage.warning('请输入预计金额');
    return;
  }
  try {
    await createDeal(dealForm);
    ElMessage.success('商机创建成功');
    showDealForm.value = false;
    Object.assign(dealForm, {
      name: '', category: BusinessCategory.WebsiteBuild, expectedAmount: 0,
      stage: DealStage.InitialContact, expectedCloseMonth: '', winRate: 20, remarks: ''
    });
    loadData();
  } catch (e: any) {
    ElMessage.error(e.message || '创建失败');
  }
}

function statusLabel(s: number) { return ['待联系', '跟进中', '已报价', '成交', '暂搁', '无效'][s] || '未知'; }
function statusTagType(s: number) { return ['info', 'warning', 'primary', 'success', 'info', 'danger'][s] || 'info'; }
function ratingLabel(r: number) { return ['A', 'B', 'C', 'D'][r] || '-'; }
function ratingTagType(r: number) { return ['success', 'warning', 'info', 'danger'][r] || 'info'; }
function cityLabel(c: number) { return ['潍坊', '青岛', '烟台', '济南', '淄博', '临沂', '山东其他', '省外'][c] || '其他'; }
function industryLabel(i: number) { return ['机械制造', '纺织服装', '食品加工', '化工', '建材', '农业种植', '商贸流通', '服务业'][i] || '其他'; }
function scaleLabel(s?: number) { return s === undefined ? '-' : ['≤20人', '21-100人', '101-300人', '301-1000人', '>1000人', '未知'][s] || '未知'; }
function categoryLabel(c: number) { return ['企业建站', '网站优化', 'AI应用', 'GEO优化', '数字员工', '其他'][c] || '其他'; }
function stageLabel(s: number) { return ['初步接触', '需求确认', '方案报价', '商务谈判', '成交', '丢单'][s] || '未知'; }
function dealStatusLabel(s: number) { return ['进行中', '已成交', '已丢单', '暂搁'][s] || '未知'; }
function dealStatusTagType(s: number) { return ['warning', 'success', 'danger', 'info'][s] || 'info'; }
function followTypeLabel(t: number) { return ['电话', '微信', '拜访', '邮件', '会议', '其他'][t] || '其他'; }
function formatDate(d: string) { return new Date(d).toLocaleString('zh-CN'); }

onMounted(async () => {
  await loadData();
  loadIntelligence();
});
</script>

<style scoped>
.crm-company-detail { padding: 16px; }
.info-card { margin-top: 16px; margin-bottom: 16px; }
.detail-tabs { margin-top: 8px; }
.tab-header { display: flex; justify-content: space-between; align-items: center; }
.activity-card { margin-bottom: 8px; }
.activity-header { display: flex; align-items: flex-start; gap: 12px; }
.activity-content { flex: 1; }
.activity-next { margin-top: 8px; color: #666; }
.next-date { color: #e6a23c; }
.intel-card { padding: 4px 0; }
</style>
