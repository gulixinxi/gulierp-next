<template>
  <div class="crm-reverse-lookup">
    <el-card shadow="never">
      <template #header>
        <div class="card-header">
          <span class="title">单位反查</span>
          <div>
            <el-button @click="showImport = true" :icon="Upload">上传单位清单</el-button>
            <el-button @click="showManual = true" :icon="Edit">手动添加</el-button>
            <el-button type="primary" @click="startLookup" :icon="Search" :loading="lookuping" :disabled="items.length === 0">
              开始反查
            </el-button>
          </div>
        </div>
      </template>

      <el-alert type="info" :closable="false" style="margin-bottom: 12px;">
        上传单位清单 Excel（含"单位名称"列，可选"网址"列），系统自动查询 ICP 备案、网站状态、GEO 评分，并生成商机建议。
      </el-alert>

      <el-table :data="items" stripe border>
        <el-table-column type="index" label="#" width="50" />
        <el-table-column prop="companyName" label="单位名称" min-width="180" />
        <el-table-column prop="website" label="输入网址" width="180">
          <template #default="{ row }">
            <span v-if="row.website">{{ row.website }}</span>
            <span v-else style="color: #999;">-</span>
          </template>
        </el-table-column>
        <el-table-column label="有网站" width="80">
          <template #default="{ row }">
            <el-tag v-if="row.result" :type="row.result.hasWebsite ? 'success' : 'danger'" size="small">
              {{ row.result.hasWebsite ? '有' : '无' }}
            </el-tag>
            <span v-else>-</span>
          </template>
        </el-table-column>
        <el-table-column label="网站状态" width="100">
          <template #default="{ row }">
            <el-tag v-if="row.result" :type="websiteStatusType(row.result.websiteStatus)" size="small">
              {{ row.result.websiteStatus }}
            </el-tag>
            <span v-else>-</span>
          </template>
        </el-table-column>
        <el-table-column label="ICP备案" width="120">
          <template #default="{ row }">
            <span v-if="row.result?.hasIcp === true" style="color: #67c23a;">{{ row.result.icpLicense }}</span>
            <span v-else-if="row.result?.hasIcp === false" style="color: #e6a23c;">未备案</span>
            <span v-else>-</span>
          </template>
        </el-table-column>
        <el-table-column label="备案企业" min-width="150">
          <template #default="{ row }">
            {{ row.result?.icpCompanyName || '-' }}
          </template>
        </el-table-column>
        <el-table-column label="GEO评分" width="90">
          <template #default="{ row }">
            <el-tag v-if="row.result?.geoScore != null" :type="geoScoreType(row.result.geoScore)" size="small">
              {{ row.result.geoScore }}
            </el-tag>
            <span v-else>-</span>
          </template>
        </el-table-column>
        <el-table-column label="商机建议" min-width="200">
          <template #default="{ row }">
            <span v-if="row.result?.suggestion" style="color: #409eff;">{{ row.result.suggestion }}</span>
            <span v-else>-</span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="120" fixed="right">
          <template #default="{ row, $index }">
            <el-button v-if="row.result" size="small" @click="importLead(row)">导入线索</el-button>
            <el-button size="small" type="danger" @click="items.splice($index, 1)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div v-if="items.length === 0" style="text-align: center; padding: 40px; color: #999;">
        暂无数据，请上传单位清单或手动添加
      </div>
    </el-card>

    <!-- 上传对话框 -->
    <el-dialog v-model="showImport" title="上传单位清单" width="500px">
      <el-upload
        drag
        action="#"
        :auto-upload="false"
        :on-change="handleFile"
        accept=".xls,.xlsx,.csv"
      >
        <el-icon class="el-icon--upload"><upload-filled /></el-icon>
        <div class="el-upload__text">将 Excel 文件拖到此处，或<em>点击上传</em></div>
        <template #tip>
          <div class="el-upload__tip">需包含"单位名称"列，可选"网址"列</div>
        </template>
      </el-upload>
    </el-dialog>

    <!-- 手动添加对话框 -->
    <el-dialog v-model="showManual" title="手动添加单位" width="400px">
      <el-form :model="manualForm" label-width="80px">
        <el-form-item label="单位名称" required>
          <el-input v-model="manualForm.companyName" placeholder="如：山东XX食品有限公司" />
        </el-form-item>
        <el-form-item label="网址">
          <el-input v-model="manualForm.website" placeholder="如：www.example.com（可选）" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showManual = false">取消</el-button>
        <el-button type="primary" @click="addManual">添加</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { ElMessage } from 'element-plus';
import { Upload, Edit, Search, UploadFilled } from '@element-plus/icons-vue';
import { readSpreadsheetRows } from '../../utils/spreadsheet';
import { apiPost } from '../../api/http';

interface LookupItem {
  companyName: string;
  website?: string;
  result?: any;
}

const items = ref<LookupItem[]>([]);
const lookuping = ref(false);
const showImport = ref(false);
const showManual = ref(false);
const manualForm = reactive({ companyName: '', website: '' });

async function handleFile(file: any) {
  try {
    const rows = await readSpreadsheetRows(file.raw);
      let count = 0;
      rows.forEach(row => {
        const name = row['单位名称'] || row['公司名称'] || row['企业名称'] || row['name'] || row['Name'];
        if (name && String(name).trim()) {
          const website = row['网址'] || row['网站'] || row['官网'] || row['website'] || row['Website'];
          items.value.push({
            companyName: String(name).trim(),
            website: website ? String(website).trim() : undefined
          });
          count++;
        }
      });
      ElMessage.success(`已导入 ${count} 个单位`);
      showImport.value = false;
  } catch (err) {
    ElMessage.error('文件解析失败，请检查格式');
  }
}

function addManual() {
  if (!manualForm.companyName.trim()) {
    ElMessage.warning('请输入单位名称');
    return;
  }
  items.value.push({
    companyName: manualForm.companyName.trim(),
    website: manualForm.website.trim() || undefined
  });
  manualForm.companyName = '';
  manualForm.website = '';
  showManual.value = false;
}

async function startLookup() {
  if (items.value.length === 0) {
    ElMessage.warning('请先添加单位');
    return;
  }
  lookuping.value = true;
  try {
    const payload = {
      items: items.value.map(i => ({ companyName: i.companyName, website: i.website })),
      includeGeo: true
    };
    const res: any = await apiPost('/api/v1/crm/reverse-lookup', payload);
    if (res?.items) {
      res.items.forEach((r: any, i: number) => {
        if (items.value[i]) items.value[i].result = r;
      });
      ElMessage.success(`反查完成：${res.items.length} 个单位`);
    }
  } catch (err: any) {
    ElMessage.error(err?.response?.data?.detail || '反查失败');
  } finally {
    lookuping.value = false;
  }
}

function websiteStatusType(status?: string) {
  if (!status) return 'info';
  if (status === '正常') return 'success';
  if (status === '无HTTPS') return 'warning';
  return 'danger';
}

function geoScoreType(score: number) {
  if (score >= 70) return 'success';
  if (score >= 40) return 'warning';
  return 'danger';
}

async function importLead(row: LookupItem) {
  try {
    const payload: any = {
      companyName: row.companyName,
      source: 6, // 转介绍/其他
      websiteDomain: row.result?.domain || row.website,
      remarks: row.result?.suggestion ? `反查建议：${row.result.suggestion}` : undefined
    };
    if (row.result?.icpCompanyName) {
      payload.companyName = row.result.icpCompanyName;
      payload.remarks = `原单位名：${row.companyName}；${payload.remarks || ''}`;
    }
    await apiPost('/api/v1/crm/leads', payload);
    ElMessage.success('已导入线索池');
  } catch (err: any) {
    ElMessage.error(err?.response?.data?.detail || '导入失败');
  }
}
</script>

<style scoped>
.crm-reverse-lookup { padding: 16px; }
.card-header { display: flex; justify-content: space-between; align-items: center; }
.title { font-size: 16px; font-weight: 600; }
</style>
