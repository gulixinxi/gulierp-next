<template>
  <div class="signal-center">
    <h2 class="page-title">企业情报信号 <span class="subtitle">（Signal Center，外部采集的企业动态）</span></h2>

    <el-card v-loading="loading" shadow="never">
      <template #header>
        <div class="card-header">
          <div class="filters">
            <el-select v-model="filterStatus" placeholder="状态" clearable size="small" style="width:110px" @change="loadData">
              <el-option label="待处理" :value="0" />
              <el-option label="已确认" :value="1" />
              <el-option label="已忽略" :value="2" />
              <el-option label="已转商机" :value="3" />
            </el-select>
            <el-select v-model="filterType" placeholder="信号类型" clearable size="small" style="width:140px" @change="loadData">
              <el-option v-for="(label, val) in typeLabels" :key="val" :label="label" :value="Number(val)" />
            </el-select>
            <el-input v-model="keyword" placeholder="搜索企业/标题" size="small" style="width:180px" clearable @clear="loadData" @keyup.enter="loadData" />
            <el-button size="small" type="primary" @click="loadData">查询</el-button>
          </div>
          <span>共 {{ total }} 条</span>
        </div>
      </template>

      <el-table :data="signals" size="small" stripe>
        <el-table-column label="状态" width="80">
          <template #default="{ row }">
            <el-tag :type="statusTag(row.status)" size="small">{{ statusLabel(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="类型" width="100">
          <template #default="{ row }">{{ typeLabels[row.type] || row.type }}</template>
        </el-table-column>
        <el-table-column prop="companyName" label="企业" min-width="160" show-overflow-tooltip />
        <el-table-column prop="title" label="信号标题" min-width="200" show-overflow-tooltip />
        <el-table-column prop="source" label="来源" width="90" />
        <el-table-column label="可信度" width="80">
          <template #default="{ row }">
            <el-progress :percentage="row.confidence" :stroke-width="8" :show-text="false" />
          </template>
        </el-table-column>
        <el-table-column label="采集时间" width="160">
          <template #default="{ row }">{{ formatTime(row.collectedAt) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="160" fixed="right">
          <template #default="{ row }">
            <el-button v-if="can('crm.lead.manage') && row.status === 0" size="small" type="success" link @click="handleStatus(row.id, 1)">确认</el-button>
            <el-button v-if="can('crm.lead.manage') && row.status === 0" size="small" type="info" link @click="handleStatus(row.id, 2)">忽略</el-button>
            <el-button v-if="can('crm.lead.manage') && row.status === 1" size="small" type="primary" link @click="handleStatus(row.id, 3)">转商机</el-button>
            <el-button size="small" link @click="showDetail(row)">详情</el-button>
          </template>
        </el-table-column>
      </el-table>

      <el-pagination
        v-model:current-page="page"
        v-model:page-size="pageSize"
        :total="total"
        :page-sizes="[10, 20, 50]"
        layout="total, sizes, prev, pager, next"
        @size-change="loadData"
        @current-change="loadData"
        style="margin-top:12px; justify-content:flex-end"
      />

      <el-empty v-if="!loading && signals.length === 0" description="暂无信号，运行线索自动化后会自动生成" />
    </el-card>

    <!-- 信号详情抽屉 -->
    <el-drawer v-model="detailVisible" title="信号详情与证据" size="560px">
      <div v-if="currentSignal" class="detail">
        <p><b>企业：</b>{{ currentSignal.companyName }}</p>
        <p><b>类型：</b>{{ typeLabels[currentSignal.type] }}</p>
        <p><b>标题：</b>{{ currentSignal.title }}</p>
        <p><b>来源：</b>{{ currentSignal.source }}</p>
        <p v-if="currentSignal.sourceUrl"><b>来源URL：</b><el-link :href="currentSignal.sourceUrl" target="_blank">{{ currentSignal.sourceUrl }}</el-link></p>
        <p><b>可信度：</b>{{ currentSignal.confidence }}%</p>
        <p><b>采集时间：</b>{{ formatTime(currentSignal.collectedAt) }}</p>
        <p v-if="currentSignal.eventAt"><b>事件时间：</b>{{ formatTime(currentSignal.eventAt) }}</p>

        <el-divider content-position="left">信号详情</el-divider>
        <pre class="detail-text">{{ currentSignal.detail || '（无）' }}</pre>

        <!-- 证据追溯区 -->
        <el-divider content-position="left">证据追溯</el-divider>
        <p><b>证据类型：</b>{{ currentSignal.evidenceType || '未标注' }}</p>
        <p v-if="currentSignal.queryParams"><b>查询条件：</b><code>{{ currentSignal.queryParams }}</code></p>
        <div v-if="currentSignal.rawContent" class="raw-content">
          <b>原始内容：</b>
          <pre class="detail-text">{{ currentSignal.rawContent }}</pre>
        </div>

        <!-- 验证状态 -->
        <el-divider content-position="left">验证状态</el-divider>
        <p>
          <b>当前状态：</b>
          <el-tag :type="verificationTag(currentSignal.verificationStatus)" size="small">
            {{ verificationLabel(currentSignal.verificationStatus) }}
          </el-tag>
          <span v-if="currentSignal.verifiedAt" style="margin-left:8px;color:#909399;font-size:12px">
            {{ formatTime(currentSignal.verifiedAt) }}
          </span>
        </p>
        <p style="color:#909399;font-size:12px">
          注意：未查询到证件不代表企业没有证件；禁止将推测性缺口当作已证实事实。
        </p>
        <div v-if="can('crm.lead.manage')" class="verify-actions">
          <el-button size="small" type="success" @click="verify(1)">已确认存在</el-button>
          <el-button size="small" type="danger" @click="verify(2)">已确认不存在</el-button>
          <el-button size="small" type="warning" @click="verify(3)">未查到</el-button>
          <el-button size="small" type="info" @click="verify(4)">数据过期</el-button>
          <el-button size="small" @click="verify(5)">需人工核实</el-button>
        </div>

        <el-divider v-if="currentSignal.handleNote" />
        <p v-if="currentSignal.handleNote"><b>处理备注：</b>{{ currentSignal.handleNote }}</p>
      </div>
    </el-drawer>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { ElMessage } from 'element-plus';
import { listSignals, updateSignalStatus, updateSignalVerification, type SignalDto } from '../../api/crm';
import { useCrmPermission } from '../../composables/useCrmPermission';

const can = useCrmPermission();
const loading = ref(false);
const signals = ref<SignalDto[]>([]);
const total = ref(0);
const page = ref(1);
const pageSize = ref(20);
const filterStatus = ref<number | undefined>();
const filterType = ref<number | undefined>();
const keyword = ref('');
const detailVisible = ref(false);
const currentSignal = ref<SignalDto | null>(null);

const typeLabels: Record<number, string> = {
  0: '网站异常', 1: '官网缺失', 2: 'GEO变化', 3: 'ICP变动',
  4: '许可证到期', 5: '政府处罚', 6: '招标采购', 7: '政策补贴',
  8: '招聘扩张', 9: '外贸信息', 99: '其他'
};

function statusLabel(s: number): string {
  return ['待处理', '已确认', '已忽略', '已转商机'][s] || String(s);
}
function statusTag(s: number): string {
  return ['warning', 'success', 'info', 'primary'][s] || 'info';
}
function formatTime(t?: string): string {
  if (!t) return '-';
  return new Date(t).toLocaleString('zh-CN', { hour12: false });
}

async function loadData() {
  loading.value = true;
  try {
    const res = await listSignals({
      type: filterType.value, status: filterStatus.value,
      keyword: keyword.value || undefined, page: page.value, pageSize: pageSize.value
    });
    signals.value = res.items;
    total.value = res.total;
  } catch (e) {
    console.error(e);
  } finally {
    loading.value = false;
  }
}

async function handleStatus(id: number, status: number) {
  try {
    await updateSignalStatus(id, status);
    ElMessage.success('操作成功');
    loadData();
  } catch (e) {
    ElMessage.error('操作失败');
  }
}

function showDetail(row: SignalDto) {
  currentSignal.value = row;
  detailVisible.value = true;
}

function verificationLabel(s: number): string {
  return ['未查询', '已确认存在', '已确认不存在', '未查到', '数据过期', '需人工核实'][s] || String(s);
}
function verificationTag(s: number): string {
  return ['info', 'success', 'danger', 'warning', 'info', 'warning'][s] || 'info';
}
async function verify(status: number) {
  if (!currentSignal.value) return;
  try {
    const updated = await updateSignalVerification(currentSignal.value.id, status);
    currentSignal.value = updated;
    // 同步更新列表中的记录
    const idx = signals.value.findIndex(s => s.id === updated.id);
    if (idx >= 0) signals.value[idx] = updated;
    ElMessage.success(`已标记为「${verificationLabel(status)}」`);
  } catch (e) {
    ElMessage.error('验证操作失败');
  }
}

onMounted(loadData);
</script>

<style scoped>
.signal-center { padding: 16px; }
.page-title { margin: 0 0 16px; font-size: 20px; }
.subtitle { font-size: 13px; color: #909399; font-weight: normal; }
.card-header { display: flex; justify-content: space-between; align-items: center; }
.filters { display: flex; gap: 8px; align-items: center; }
.detail p { margin: 6px 0; }
.raw-content { margin: 6px 0; }
.detail-text { white-space: pre-wrap; word-break: break-all; background: #f5f7fa; padding: 10px; border-radius: 4px; font-size: 13px; max-height: 300px; overflow: auto; }
.verify-actions { display: flex; flex-wrap: wrap; gap: 6px; margin-top: 8px; }
</style>
