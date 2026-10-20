<template>
  <div class="quake-search-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <span>Quake 线索采集</span>
          <span class="hint">从 360 Quake 网络空间测绘搜索目标站点，一键导入线索池</span>
        </div>
      </template>

      <!-- 搜索条件 -->
      <div class="search-bar">
        <el-select v-model="preset" placeholder="快捷查询" style="width:180px" @change="applyPreset">
          <el-option label="潍坊 食品企业" value="wf_food" />
          <el-option label="潍坊 机械制造" value="wf_machinery" />
          <el-option label="潍坊 化工企业" value="wf_chemical" />
          <el-option label="潍坊 纺织服装" value="wf_textile" />
          <el-option label="潍坊 无HTTPS站点" value="wf_nohttps" />
          <el-option label="自定义" value="custom" />
        </el-select>
        <el-input v-model="query" placeholder='Quake 查询语法，如 city_cn: "潍坊" AND title: "食品"' style="flex:1;min-width:300px" />
        <el-input-number v-model="size" :min="5" :max="50" :step="5" style="width:110px" />
        <el-button type="primary" :loading="searching" @click="doSearch">搜索</el-button>
        <el-button v-if="lastQueryInfo" type="warning" @click="doForceSearch" :loading="searching">
          强制重新查询
        </el-button>
      </div>

      <!-- 冷却期提示 -->
      <el-alert v-if="cooldownMessage" :title="cooldownMessage" type="warning" :closable="false" show-icon style="margin-top:10px" />

      <!-- 搜索结果 -->
      <div v-if="results.length > 0" class="results-area">
        <div class="results-toolbar">
          <span>共 {{ results.length }} 条结果，已选 {{ selected.length }} 条
            <span v-if="enrichedCount > 0" style="color:#67c23a"> | ICP补全 {{ enrichedCount }} 家</span>
            <span v-if="checkedCount > 0" style="color:#e6a23c"> | 检测 {{ checkedCount }} 站</span>
          </span>
          <div>
            <el-button type="warning" :disabled="results.length === 0" :loading="enriching" @click="doEnrich">
              ICP补全
            </el-button>
            <el-button type="primary" :disabled="results.length === 0" :loading="checking" @click="doWebsiteCheck">
              网站检测
            </el-button>
            <el-button type="info" :disabled="results.length === 0" :loading="geoChecking" @click="doGeoCheck">
              GEO检测
            </el-button>
            <el-button type="danger" :disabled="issueSites.length === 0" :loading="signaling" @click="doCreateSignals">
              问题转信号 ({{ issueSites.length }})
            </el-button>
            <el-button type="success" :disabled="selected.length === 0" :loading="importing" @click="doImport">
              导入选中 ({{ selected.length }})
            </el-button>
            <el-button type="warning" :disabled="results.length === 0" :loading="importingAll" @click="doImportAll">
              全部导入 ({{ results.length }})
            </el-button>
          </div>
        </div>
        <el-table :data="results" @selection-change="onSelect" style="margin-top:10px" max-height="500">
          <el-table-column type="selection" width="45" />
          <el-table-column prop="domain" label="域名/IP" min-width="140">
            <template #default="{ row }">
              <el-link :href="'http://' + row.domain" target="_blank" type="primary">{{ row.domain }}</el-link>
            </template>
          </el-table-column>
          <el-table-column prop="title" label="网站标题" min-width="140" show-overflow-tooltip />
          <el-table-column label="ICP企业名称" min-width="160" show-overflow-tooltip>
            <template #default="{ row }">
              <span v-if="row.icpUnitName" style="color:#67c23a">{{ row.icpUnitName }}</span>
              <span v-else-if="row.icpQueried" style="color:#f56c6c">无备案</span>
              <span v-else style="color:#909399">待补全</span>
            </template>
          </el-table-column>
          <el-table-column label="检测结果" width="120">
            <template #default="{ row }">
              <el-tag v-if="!row.siteChecked" type="info" size="small">未检测</el-tag>
              <template v-else>
                <el-tag :type="row.siteIssues?.length ? 'danger' : 'success'" size="small">
                  {{ row.siteAccessible ? (row.siteIssues?.length || 0) + '个问题' : '无法访问' }}
                </el-tag>
              </template>
            </template>
          </el-table-column>
          <el-table-column label="检测问题" min-width="200">
            <template #default="{ row }">
              <el-popover v-if="row.siteIssues?.length" placement="top" :width="300" trigger="click">
                <template #reference>
                  <el-link type="danger" size="small">{{ row.siteIssues[0] }}<span v-if="row.siteIssues.length>1"> 等{{row.siteIssues.length}}项</span></el-link>
                </template>
                <div style="font-size:13px">
                  <p v-for="(iss,i) in row.siteIssues" :key="i" style="margin:4px 0;color:#f56c6c">• {{iss}}</p>
                  <p style="margin:6px 0 0;color:#909399;font-size:12px">HTTPS:{{row.siteHttps?'是':'否'}} | 移动端:{{row.siteViewport?'适配':'不适配'}} | 响应:{{row.siteMs}}ms</p>
                </div>
              </el-popover>
              <span v-else-if="row.siteChecked" style="color:#67c23a;font-size:12px">正常</span>
            </template>
          </el-table-column>
          <el-table-column label="百度推广" width="100">
            <template #default="{ row }">
              <el-tag v-if="row.hasBaiduAds" type="warning" size="small" effect="dark">
                {{ row.baiduAdsType || '百度推广' }}
              </el-tag>
              <span v-else-if="row.siteChecked" style="color:#909399;font-size:12px">未检测到</span>
              <span v-else style="color:#c0c4cc;font-size:12px">-</span>
            </template>
          </el-table-column>
          <el-table-column label="GEO评分" width="110">
            <template #default="{ row }">
              <el-tag v-if="!row.geoChecked" type="info" size="small">未检测</el-tag>
              <el-tag v-else :type="geoBandType(row.geoBand)" size="small">
                {{ row.geoScore }}分 {{ row.geoBand }}
              </el-tag>
            </template>
          </el-table-column>
        </el-table>
      </div>

      <el-empty v-else-if="!searching" description="输入查询条件开始搜索" />
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { apiGet, apiPost } from '../../api/http';

interface QuakeSite {
  domain: string;
  ip?: string;
  port?: number;
  title?: string;
  service?: string;
  city?: string;
  province?: string;
  isp?: string;
  os?: string;
  icpUnitName?: string;
  icpLicence?: string;
  icpQueried?: boolean;
  siteChecked?: boolean;
  siteAccessible?: boolean;
  siteHttps?: boolean;
  siteViewport?: boolean;
  siteMs?: number;
  siteIssues?: string[];
  hasBaiduAds?: boolean;
  baiduAdsType?: string;
  geoChecked?: boolean;
  geoScore?: number;
  geoBand?: string;
  geoError?: string;
}

const preset = ref('wf_food');
const query = ref('city_cn: "潍坊" AND title: "食品"');
const size = ref(20);
const searching = ref(false);
const cooldownMessage = ref('');
const lastQueryInfo = ref<any>(null);
const importing = ref(false);
const importingAll = ref(false);
const enriching = ref(false);
const checking = ref(false);
const geoChecking = ref(false);
const signaling = ref(false);
const results = ref<QuakeSite[]>([]);
const selected = ref<QuakeSite[]>([]);

const enrichedCount = computed(() => results.value.filter(r => r.icpUnitName).length);
const checkedCount = computed(() => results.value.filter(r => r.siteChecked).length);
const issueSites = computed(() => results.value.filter(r => r.siteChecked && r.siteIssues && r.siteIssues.length > 0));

function geoBandType(band?: string): string {
  switch (band) {
    case 'excellent': return 'success';
    case 'good': return 'success';
    case 'foundation': return 'warning';
    case 'critical': return 'danger';
    default: return 'info';
  }
}

const presets: Record<string, string> = {
  wf_food: 'city_cn: "潍坊" AND title: "食品"',
  wf_machinery: 'city_cn: "潍坊" AND title: "机械"',
  wf_chemical: 'city_cn: "潍坊" AND title: "化工"',
  wf_textile: 'city_cn: "潍坊" AND title: "纺织"',
  wf_nohttps: 'city_cn: "潍坊" AND service: "http"',
  custom: ''
};

function applyPreset(val: string) {
  if (val !== 'custom' && presets[val]) {
    query.value = presets[val];
  }
}

function onSelect(rows: QuakeSite[]) {
  selected.value = rows;
}

async function doSearch() {
  if (!query.value.trim()) {
    ElMessage.warning('请输入查询条件');
    return;
  }
  searching.value = true;
  results.value = [];
  cooldownMessage.value = '';
  lastQueryInfo.value = null;
  try {
    const res: any = await apiGet('/api/v1/crm/quake/search', {
      params: { query: query.value, size: size.value }
    });
    if (res.fromCache) {
      // 冷却期内，显示提示
      cooldownMessage.value = res.message || '该查询条件在30天内已查询过，可点击"强制重新查询"跳过冷却期';
      lastQueryInfo.value = { queriedAt: res.lastQueriedAt, resultCount: res.total };
      ElMessage.warning('该条件在冷却期内，已跳过查询以节省额度');
    } else {
      results.value = res.items || [];
      ElMessage.success(`找到 ${results.value.length} 条结果`);
    }
  } catch (e: any) {
    ElMessage.error(e?.response?.data?.detail || '搜索失败，请检查查询语法');
  } finally {
    searching.value = false;
  }
}

async function doForceSearch() {
  if (!query.value.trim()) return;
  searching.value = true;
  results.value = [];
  cooldownMessage.value = '';
  lastQueryInfo.value = null;
  try {
    const res: any = await apiGet('/api/v1/crm/quake/search', {
      params: { query: query.value, size: size.value, force: true }
    });
    results.value = res.items || [];
    ElMessage.success(`强制查询完成，找到 ${results.value.length} 条结果`);
  } catch (e: any) {
    ElMessage.error(e?.response?.data?.detail || '搜索失败');
  } finally {
    searching.value = false;
  }
}

async function doEnrich() {
  if (results.value.length === 0) return;
  enriching.value = true;
  try {
    const domains = results.value.map(r => r.domain);
    const res = await apiPost<{ total: number; found: number; items: any[] }>('/api/v1/crm/icp/lookup', domains);
    // 将 ICP 结果回填到结果列表
    for (const icp of res.items) {
      const idx = results.value.findIndex(r => r.domain === icp.domain);
      if (idx >= 0) {
        results.value[idx].icpQueried = true;
        if (icp.found) {
          results.value[idx].icpUnitName = icp.unitName;
          results.value[idx].icpLicence = icp.serviceLicence;
        }
      }
    }
    ElMessage.success(`补全完成：查到 ${res.found} 家企业，${res.total - res.found} 条无备案`);
  } catch (e: any) {
    ElMessage.error(e?.response?.data?.detail || 'ICP补全失败');
  } finally {
    enriching.value = false;
  }
}

async function doWebsiteCheck() {
  if (results.value.length === 0) return;
  checking.value = true;
  try {
    const domains = results.value.map(r => r.domain);
    const res = await apiPost<{ total: number; accessible: number; hasIssues: number; items: any[] }>('/api/v1/crm/website-check', domains);
    for (const chk of res.items) {
      const idx = results.value.findIndex(r => r.domain === chk.domain);
      if (idx >= 0) {
        results.value[idx].siteChecked = true;
        results.value[idx].siteAccessible = chk.accessible;
        results.value[idx].siteHttps = chk.httpsSupported;
        results.value[idx].siteViewport = chk.hasViewport;
        results.value[idx].siteMs = chk.responseMs;
        results.value[idx].siteIssues = chk.issues || [];
        results.value[idx].hasBaiduAds = chk.hasBaiduAds || false;
        results.value[idx].baiduAdsType = chk.baiduAdsType;
      }
    }
    ElMessage.success(`检测完成：可访问 ${res.accessible}/${res.total}，${res.hasIssues} 站有问题`);
  } catch (e: any) {
    ElMessage.error(e?.response?.data?.detail || '网站检测失败');
  } finally {
    checking.value = false;
  }
}

async function doGeoCheck() {
  if (results.value.length === 0) return;
  geoChecking.value = true;
  try {
    const domains = results.value.map(r => r.domain);
    const res = await apiPost<{ total: number; geoChecked: number; critical: number; items: any[] }>('/api/v1/crm/geo-check', domains);
    for (const g of res.items) {
      const idx = results.value.findIndex(r => r.domain === g.domain);
      if (idx >= 0) {
        results.value[idx].geoChecked = g.checked;
        results.value[idx].geoScore = g.score;
        results.value[idx].geoBand = g.band;
        results.value[idx].geoError = g.error;
      }
    }
    ElMessage.success(`GEO检测完成：${res.geoChecked}/${res.total} 站，${res.critical} 站急需优化`);
  } catch (e: any) {
    ElMessage.error(e?.response?.data?.detail || 'GEO检测失败');
  } finally {
    geoChecking.value = false;
  }
}

async function doCreateSignals() {
  if (issueSites.value.length === 0) return;
  signaling.value = true;
  let created = 0;
  try {
    for (const site of issueSites.value) {
      const companyName = site.icpUnitName || site.title || site.domain;
      const detail = `检测发现 ${site.siteIssues!.length} 个问题：\n` + site.siteIssues!.map(i => `• ${i}`).join('\n') +
        `\n\nHTTPS:${site.siteHttps ? '支持' : '不支持'} | 移动端:${site.siteViewport ? '适配' : '不适配'} | 响应:${site.siteMs}ms`;
      try {
        await apiPost('/api/v1/crm/signals', {
          leadId: null,
          companyRefId: null,
          companyName,
          type: 0, // WebsiteAbnormal
          title: `网站检测异常：${site.domain}`,
          detail,
          source: '网站自动检测',
          sourceUrl: `http://${site.domain}`,
          eventAt: new Date().toISOString(),
          confidence: 90,
          evidenceType: '网站检测',
          queryParams: `domain=${site.domain}`,
          rawContent: JSON.stringify({ https: site.siteHttps, viewport: site.siteViewport, issues: site.siteIssues, responseMs: site.siteMs })
        });
        created++;
      } catch { /* 单条失败不中断 */ }
    }
    ElMessage.success(`已创建 ${created} 条网站异常信号`);
  } catch (e: any) {
    ElMessage.error('创建信号失败');
  } finally {
    signaling.value = false;
  }
}

async function doImport() {
  if (selected.value.length === 0) return;
  importing.value = true;
  try {
    const items = selected.value.map(s => ({
      domain: s.domain,
      companyName: s.icpUnitName || s.title?.replace(/[【\[\(].*?[】\]\)]/g, '').trim() || s.domain,
      title: s.title,
      ip: s.ip,
      city: s.city,
      hasBaiduAds: s.hasBaiduAds || false
    }));
    const res = await apiPost<{ created: number; skipped: number; total: number }>('/api/v1/crm/quake/import', items);
    ElMessage.success(`导入完成：新增 ${res.created} 条，跳过 ${res.skipped} 条`);
  } catch (e: any) {
    ElMessage.error('导入失败');
  } finally {
    importing.value = false;
  }
}

async function doImportAll() {
  if (results.value.length === 0) return;
  // 确认对话框
  try {
    await ElMessageBox.confirm(
      `将导入全部 ${results.value.length} 条结果（重复企业会自动跳过），是否继续？`,
      '全部导入确认',
      { confirmButtonText: '确定导入', cancelButtonText: '取消', type: 'warning' }
    );
  } catch {
    return;
  }
  importingAll.value = true;
  try {
    const items = results.value.map((s: any) => ({
      domain: s.domain,
      companyName: s.icpUnitName || s.title?.replace(/[【\[\(].*?[】\]\)]/g, '').trim() || s.domain,
      title: s.title,
      ip: s.ip,
      city: s.city,
      hasBaiduAds: s.hasBaiduAds || false
    }));
    const res = await apiPost<{ created: number; skipped: number; total: number }>('/api/v1/crm/quake/import', items);
    ElMessage.success(`全部导入完成：新增 ${res.created} 条，跳过 ${res.skipped} 条`);
  } catch (e: any) {
    ElMessage.error('导入失败');
  } finally {
    importingAll.value = false;
  }
}
</script>

<style scoped>
.card-header { display: flex; justify-content: space-between; align-items: center; }
.hint { color: #909399; font-size: 12px; font-weight: normal; }
.search-bar { display: flex; gap: 10px; align-items: center; flex-wrap: wrap; margin-bottom: 16px; }
.results-toolbar { display: flex; justify-content: space-between; align-items: center; margin-top: 16px; }
</style>
