<template>
  <!--
    BusinessPartnerList — reusable page for 客户档案 / 供应商 / 全部客商.
    GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 - Wave 4 (2026-08-28):
      * Code UX: AUTO_EDITABLE. Empty Code on Create = auto-generate.
        "留空则自动生成" help text. Edit disables Code (immutable).
      * CountryCode: searchable el-select from Country reference data.
      * CN Region: el-cascader over AdministrativeRegion. Graceful
        fallback when CN has 0 rows (Operator MCA importer not run yet).
      * International: free-text State / City / Address Line fields.
      * MnemonicCode: optional short code, case-insensitive search.
      * Search: single keyword across Code / Name / ShortName /
        MnemonicCode / Contact / Phone / Email / TaxId (8 fields).
      * Form grouping: 3 logical groups (基础信息 / 联系信息 / 地址信息).
      * List column widths: per brief §三十三. Code 170 (no truncation),
        Name 200, ShortName 130, Type 120, Contact 110, Phone 130,
        Email 200 (ellipsis + tooltip), TaxId 180, Status 90,
        UpdatedAt 165, Actions 130 (fixed right).
    No mock fallback. 404 → "数据不存在或无权访问".
  -->
  <div class="mdm-list">
    <MdmListToolbar
      v-model:search="searchKeyword"
      search-placeholder="搜索 代码 / 名称 / 简称 / 助记码 / 联系人 / 电话 / 邮箱 / 税号"
      :create-label="createLabel"
      @search="applyFilters"
      @create="openCreate"
    >
      <template #filters>
        <el-select
          v-model="filterRole"
          placeholder="客商类型"
          clearable
          class="bp-filter-role"
          @change="applyFilters"
        >
          <el-option
            v-for="opt in BP_ROLE_FILTER_OPTIONS"
            :key="opt.value"
            :label="opt.label"
            :value="opt.value"
          />
        </el-select>
        <el-select v-model="filterStatus" placeholder="状态" clearable class="bp-filter-status" @change="applyFilters">
          <el-option v-for="opt in STATUS_OPTIONS" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
      </template>
      <template #actions>
        <el-button text @click="exportData">
          <el-icon><Download /></el-icon>
          导出
        </el-button>
      </template>
    </MdmListToolbar>

    <div class="gs-list-wrap">
      <el-table
        v-loading="loading"
        :data="pagedData"
        border
        stripe
        height="100%"
        size="small"
        row-key="id"
        @row-dblclick="openDetail"
      >
        <el-table-column type="index" label="#" :width="COL.index" fixed="left" />
        <el-table-column prop="code" label="代码" :width="COL.code" sortable show-overflow-tooltip>
          <template #default="{ row }">
            <span class="mdm-code">{{ row.code }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="name" label="名称" :min-width="COL.partnerName" show-overflow-tooltip sortable />
        <el-table-column prop="shortName" label="简称" :width="COL.shortName" show-overflow-tooltip>
          <template #default="{ row }">{{ row.shortName || '—' }}</template>
        </el-table-column>
        <el-table-column prop="role" label="类型" :width="COL.type" align="center">
          <template #default="{ row }">
            <el-tag :type="roleTagType(row.role)" size="small" effect="light">{{ roleLabel(row.role) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="contactPerson" label="联系人" :width="COL.person" show-overflow-tooltip>
          <template #default="{ row }">{{ row.contactPerson || '—' }}</template>
        </el-table-column>
        <el-table-column prop="phone" label="电话" :width="COL.phone" show-overflow-tooltip>
          <template #default="{ row }">{{ row.phone || '—' }}</template>
        </el-table-column>
        <el-table-column prop="email" label="邮箱" :width="COL.email" show-overflow-tooltip>
          <template #default="{ row }">{{ row.email || '—' }}</template>
        </el-table-column>
        <el-table-column prop="taxNumber" label="税号" :width="COL.taxNo" show-overflow-tooltip>
          <template #default="{ row }">{{ row.taxNumber || '—' }}</template>
        </el-table-column>
        <el-table-column prop="status" label="状态" :width="COL.status" align="center">
          <template #default="{ row }">
            <MdmStatusBadge :status="row.status" />
          </template>
        </el-table-column>
        <el-table-column prop="updatedAt" label="更新时间" :width="COL.datetime" sortable>
          <template #default="{ row }">{{ formatDate(row.updatedAt) }}</template>
        </el-table-column>
        <el-table-column label="操作" :width="COL.actions" fixed="right" align="center">
          <template #default="{ row }">
            <MdmTableRowActions
              :status="row.status"
              @edit="openEdit(row)"
              @deactivate="confirmDeactivate(row)"
              @activate="confirmActivate(row)"
            />
          </template>
        </el-table-column>
        <template #empty>
          <MdmEmptyState
            :message="emptyMessage"
            :create-label="createLabel"
            @create="openCreate"
          />
        </template>
      </el-table>
    </div>

    <MdmPagination
      v-model:current-page="page.current"
      v-model:page-size="page.size"
      :total="total"
    />
    <div v-if="error && !loading" class="mdm-error-banner">
      <el-alert :title="error" type="error" show-icon :closable="false" />
      <el-button type="primary" link style="margin-left:12px" @click="fetchList">重新加载</el-button>
    </div>

    <!--
      Create/Edit Form Drawer — 3 logical groups per brief §三十二.
      Group 1: 基础信息 (Code, Name, ShortName, MnemonicCode, Type, Status)
      Group 2: 联系信息 (Contact, Phone, Email, TaxId)
      Group 3: 地址信息 (Country, Region, City, AddressLine1/2, PostalCode)
      Region control swaps by Country: CN → cascader from reference data,
      others → free-text State / City.
    -->
    <MdmFormDrawer
      v-model="formDrawerVisible"
      v-model:model="formData"
      :title="editingId ? '编辑客商' : '新建客商'"
      :rules="formRules"
      :loading="submitting"
      :submit-label="editingId ? '保存修改' : '创建'"
      @submit="handleSubmit"
    >
      <!-- Group 1: 基础信息 -->
      <div class="bp-form-section">
        <div class="bp-form-section-title">基础信息</div>
        <el-form-item label="代码" prop="code">
          <el-input
            v-model="formData.code"
            placeholder="留空则自动生成（推荐）"
            :disabled="!!editingId"
            maxlength="40"
            show-word-limit
          >
            <template #append v-if="!editingId">
              <el-tooltip content="代码留空 = 服务端在保存时按 MasterDataCodeRule (AUTO_EDITABLE) 自动生成 BP_000001 形式的编码" placement="top">
                <el-icon><QuestionFilled /></el-icon>
              </el-tooltip>
            </template>
          </el-input>
        </el-form-item>
        <el-form-item label="名称" prop="name">
          <el-input v-model="formData.name" placeholder="客商全称" maxlength="200" />
        </el-form-item>
        <el-form-item label="简称">
          <el-input v-model="formData.shortName" placeholder="可选" maxlength="40" />
        </el-form-item>
        <el-form-item label="助记码">
          <el-input
            v-model="formData.mnemonicCode"
            placeholder="按名称自动建议，可手工覆盖"
            maxlength="40"
            show-word-limit
            clearable
            @input="onMnemonicInput"
          />
        </el-form-item>
        <el-form-item label="类型" prop="role">
          <el-select v-model="formData.role" style="width: 100%">
            <el-option v-for="opt in BP_ROLE_OPTIONS" :key="opt.value" :label="opt.label" :value="opt.value" />
          </el-select>
        </el-form-item>
        <el-form-item label="状态" prop="status">
          <el-select v-model="formData.status" style="width: 100%">
            <el-option v-for="opt in STATUS_OPTIONS" :key="opt.value" :label="opt.label" :value="opt.value" />
          </el-select>
        </el-form-item>
      </div>

      <!-- Group 2: 联系信息 -->
      <div class="bp-form-section">
        <div class="bp-form-section-title">联系信息</div>
        <el-form-item label="联系人">
          <el-input v-model="formData.contactPerson" maxlength="100" />
        </el-form-item>
        <el-form-item label="电话">
          <el-input v-model="formData.phone" maxlength="40" />
        </el-form-item>
        <el-form-item label="邮箱" prop="email">
          <el-input v-model="formData.email" placeholder="含 @" maxlength="200" />
        </el-form-item>
        <el-form-item label="税号">
          <el-input v-model="formData.taxNumber" maxlength="50" />
        </el-form-item>
      </div>

      <!-- Group 3: 地址信息 -->
      <div class="bp-form-section">
        <div class="bp-form-section-title">地址信息</div>
        <el-form-item label="国家/地区" prop="countryCode">
          <el-select
            v-model="formData.countryCode"
            placeholder="搜索 国家代码 / 中文名 / 英文名（如 CN、中国、China）"
            filterable
            clearable
            :loading="countryLoading"
            style="width: 100%"
            @visible-change="onCountryDropdownOpen"
            @clear="onCountryCleared"
          >
            <el-option
              v-for="opt in countryOptions"
              :key="opt.value"
              :label="opt.label"
              :value="opt.value"
            >
              <div class="bp-country-option">
                <span class="bp-country-code">{{ opt.value }}</span>
                <span class="bp-country-zh">{{ opt.zhName }}</span>
                <span class="bp-country-en">{{ opt.englishName }}</span>
              </div>
            </el-option>
          </el-select>
        </el-form-item>

        <!-- CN-only: Cascader over AdministrativeRegion. Falls back to a notice + free-text when CN rows = 0. -->
        <template v-if="isCnCountry">
          <el-form-item label="省/市/区县">
            <el-cascader
              v-model="cnRegionPath"
              :options="cnRegionOptions"
              :props="cnCascaderProps"
              :loading="cnRegionLoading"
              clearable
              filterable
              placeholder="选择省/市/区县（如 北京市 / 北京市 / 东城区）"
              style="width: 100%"
              @change="onCnRegionChange"
            />
            <div v-if="cnRegionEmpty" class="bp-region-hint">
              行政区划数据尚未初始化（操作员需导入 MCA 数据）。可继续填写下方文本地址，保存后区域选择留空。
            </div>
          </el-form-item>
        </template>
        <!-- International: free-text state / city / district -->
        <template v-else>
          <el-form-item label="省/州">
            <el-input v-model="formData.region" maxlength="100" placeholder="如 California, NSW, Hessen" />
          </el-form-item>
        </template>

        <el-form-item label="城市">
          <el-input v-model="formData.city" maxlength="100" />
        </el-form-item>
        <el-form-item label="地址行 1">
          <el-input v-model="formData.addressLine1" placeholder="街道、门牌号" maxlength="200" />
        </el-form-item>
        <el-form-item label="地址行 2">
          <el-input v-model="formData.addressLine2" placeholder="楼栋、单元、房间（选填）" maxlength="200" />
        </el-form-item>
        <el-form-item label="邮编">
          <el-input v-model="formData.postalCode" maxlength="20" />
        </el-form-item>

        <el-form-item v-if="formRegionNameSnapshot" label=" ">
          <el-alert
            :title="`已绑定的行政区划：${formRegionNameSnapshot}（${formRegionCodeSnapshot ?? ''}）`"
            type="info"
            :closable="false"
            show-icon
          />
        </el-form-item>
      </div>

      <div class="bp-form-section">
        <div class="bp-form-section-title">说明</div>
        <el-form-item label="备注">
          <el-input v-model="formData.description" type="textarea" :rows="2" maxlength="2000" show-word-limit />
        </el-form-item>
      </div>
    </MdmFormDrawer>

    <!-- Detail Drawer -->
    <MdmDetailDrawer
      v-model="detailDrawerVisible"
      :title="`客商详情 · ${detailData?.code || ''}`"
      @edit="openEditFromDetail"
    >
      <template #header>
        <div class="mdm-detail-title">
          <MdmStatusBadge :status="detailData?.status || 'active'" />
          <span class="mdm-detail-name">{{ detailData?.name }}</span>
          <el-tag v-if="detailData?.role" :type="roleTagType(detailData.role)" size="small" effect="light">
            {{ roleLabel(detailData.role) }}
          </el-tag>
        </div>
      </template>
      <el-descriptions :column="2" border size="small" title="基础信息">
        <el-descriptions-item label="代码">{{ detailData?.code }}</el-descriptions-item>
        <el-descriptions-item label="名称">{{ detailData?.name }}</el-descriptions-item>
        <el-descriptions-item label="简称">{{ detailData?.shortName || '—' }}</el-descriptions-item>
        <el-descriptions-item label="助记码">{{ detailData?.mnemonicCode || '—' }}</el-descriptions-item>
        <el-descriptions-item label="类型">{{ roleLabel(detailData?.role) }}</el-descriptions-item>
        <el-descriptions-item label="状态">{{ getStatusLabel(detailData?.status) }}</el-descriptions-item>
      </el-descriptions>
      <el-descriptions :column="2" border size="small" title="联系信息" style="margin-top: 16px">
        <el-descriptions-item label="联系人">{{ detailData?.contactPerson || '—' }}</el-descriptions-item>
        <el-descriptions-item label="电话">{{ detailData?.phone || '—' }}</el-descriptions-item>
        <el-descriptions-item label="邮箱">{{ detailData?.email || '—' }}</el-descriptions-item>
        <el-descriptions-item label="税号">{{ detailData?.taxNumber || '—' }}</el-descriptions-item>
      </el-descriptions>
      <el-descriptions :column="2" border size="small" title="地址信息" style="margin-top: 16px">
        <el-descriptions-item label="国家代码">{{ detailData?.countryCode || '—' }}</el-descriptions-item>
        <el-descriptions-item label="行政区划">
          <template v-if="detailData?.regionNameSnapshot">
            {{ detailData.regionNameSnapshot }}（{{ detailData.regionCodeSnapshot }}）
          </template>
          <template v-else>—</template>
        </el-descriptions-item>
        <el-descriptions-item label="省/州">{{ detailData?.region || '—' }}</el-descriptions-item>
        <el-descriptions-item label="城市">{{ detailData?.city || '—' }}</el-descriptions-item>
        <el-descriptions-item label="地址行 1">{{ detailData?.addressLine1 || '—' }}</el-descriptions-item>
        <el-descriptions-item label="地址行 2">{{ detailData?.addressLine2 || '—' }}</el-descriptions-item>
        <el-descriptions-item label="邮编">{{ detailData?.postalCode || '—' }}</el-descriptions-item>
      </el-descriptions>
      <el-descriptions :column="2" border size="small" title="其他" style="margin-top: 16px">
        <el-descriptions-item label="备注">{{ detailData?.description || '—' }}</el-descriptions-item>
        <el-descriptions-item label="更新时间">{{ formatDate(detailData?.updatedAt) }}</el-descriptions-item>
      </el-descriptions>
    </MdmDetailDrawer>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, watch } from 'vue';
import { useRoute } from 'vue-router';
import { Download, QuestionFilled } from '@element-plus/icons-vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import type { FormRules } from 'element-plus';

import MdmListToolbar from '../../components/mdm/MdmListToolbar.vue';
import MdmStatusBadge from '../../components/mdm/MdmStatusBadge.vue';
import MdmFormDrawer from '../../components/mdm/MdmFormDrawer.vue';
import MdmDetailDrawer from '../../components/mdm/MdmDetailDrawer.vue';
import MdmPagination from '../../components/mdm/MdmPagination.vue';
import MdmEmptyState from '../../components/mdm/MdmEmptyState.vue';
import MdmTableRowActions from '../../components/mdm/MdmTableRowActions.vue';
import { TABLE_COLUMN_PRESETS as COL } from '../../design-system/tableColumns';

import { ApiError } from '../../api/http';
import * as bpApi from '../../api/mdm/business-partner';
import {
  listCountries,
  toCountryOption,
  listRegions,
  getRegionByCode,
} from '../../api/mdm/reference-data';
import {
  STATUS_OPTIONS,
  BP_ROLE_OPTIONS,
  BP_ROLE_FILTER_OPTIONS,
  roleFilterToInt,
  statusUiToInt,
} from '../../types/mdm';
import { generateMnemonicCode, shouldRefreshMnemonic } from '../../utils/mnemonic';
import type {
  BusinessPartner,
  BusinessPartnerForm,
  BusinessPartnerRole,
  BusinessPartnerRoleFilter,
  Country,
  CountryOption,
  MasterDataStatus,
  AdministrativeRegion,
} from '../../types/mdm';

// ===== Route-driven default role (meta.defaultRole) =====
const route = useRoute();
const defaultRole = computed<BusinessPartnerRoleFilter>(
  () => (route.meta?.defaultRole as BusinessPartnerRoleFilter | undefined) ?? 'all',
);
const createLabel = computed(() =>
  defaultRole.value === 'customer' ? '新建客户'
    : defaultRole.value === 'supplier' ? '新建供应商'
      : '新建客商',
);
const emptyMessage = computed(() =>
  defaultRole.value === 'customer' ? '暂无客户数据'
    : defaultRole.value === 'supplier' ? '暂无供应商数据'
      : '暂无客商数据',
);

// ===== Real API list state =====
const list = ref<BusinessPartner[]>([]);
const total = ref(0);
const loading = ref(false);
const error = ref<string | null>(null);

const searchKeyword = ref('');
const filterRole = ref<BusinessPartnerRoleFilter>('all');
const filterStatus = ref<MasterDataStatus | ''>('');
const page = reactive({ current: 1, size: 20 });

async function fetchList() {
  loading.value = true;
  error.value = null;
  try {
    const result = await bpApi.listBusinessPartners({
      keyword: searchKeyword.value.trim() || undefined,
      role: roleFilterToInt(filterRole.value),
      status: filterStatus.value ? statusUiToInt(filterStatus.value) : undefined,
      page: page.current,
      pageSize: page.size,
    });
    list.value = result.items;
    total.value = result.totalCount;
  } catch (e) {
    if (e instanceof ApiError) {
      if (e.code === 'authentication_required') throw e; // global guard redirects
      if (e.status === 404) {
        error.value = '数据不存在或无权访问';
      } else {
        const parts = [e.title];
        if (e.detail) parts.push(e.detail);
        if (e.requestId) parts.push(`(RequestId: ${e.requestId})`);
        error.value = parts.join(' — ');
      }
    } else {
      error.value = '加载客商数据失败，请刷新重试';
    }
    list.value = [];
    total.value = 0;
  } finally {
    loading.value = false;
  }
}

onMounted(() => {
  filterRole.value = defaultRole.value;
  fetchList();
});

const pagedData = computed(() => list.value);

function applyFilters() {
  page.current = 1;
  fetchList();
}

// ===== Country + Region reference data =====
const countryOptions = ref<CountryOption[]>([]);
const countryLoading = ref(false);
const cnRegionOptions = ref<CascaderOption[]>([]);
const cnRegionLoading = ref(false);
const cnRegionEmpty = ref(false);
const cnRegionPath = ref<string[]>([]);
// GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 - Wave 5.2
// (2026-08-28). The listRegions API returns only the rows
// matching the parentId filter (or L1 only when parentId=null).
// The cascader therefore has to lazy-load children per expand
// event so the user can drill from L1 (province) -> L2
// (prefecture/city) -> L3 (county/district). With lazy=true the
// cascader panel calls lazyLoad(node, resolve) and uses the
// returned array as the next column.
const cnCascaderProps = {
  value: 'id',
  label: 'name',
  children: 'children',
  checkStrictly: false,
  emitPath: false, // return the leaf Region object as the value
  lazy: true,
  lazyLoad: async (node: { level: number; value?: string }, resolve: (children: CascaderOption[]) => void) => {
    // level: 0 = root placeholder emitted by el-cascader
    // level 1 = L1 (province) we already loaded eagerly
    // level 2 = L2 (city) — fetch children by parentId
    // level 3 = L3 (county) — leaves, no children
    try {
      if (node.level === 0) {
        // Eagerly load L1 once (used by both the empty
        // `regions?parentId=null` call and the watch on
        // countryCode change).
        const regions = await listRegions({ countryCode: 'CN', parentId: null, includeInactive: false });
        const opts = regions.map(toCascaderOption);
        cnRegionOptions.value = opts;
        cnRegionEmpty.value = opts.length === 0;
        resolve(opts);
      } else if (node.value) {
        const regions = await listRegions({ countryCode: 'CN', parentId: node.value, includeInactive: false });
        const opts = regions.map(toCascaderOption);
        resolve(opts);
      } else {
        resolve([]);
      }
    } catch (e) {
      resolve([]);
    }
  },
};
function toCascaderOption(r: AdministrativeRegion): CascaderOption {
  return {
    id: r.id,
    name: r.name,
    level: r.level,
    code: r.code,
    countryCode: r.countryCode,
    parentId: r.parentId,
    regionType: r.regionType,
  };
}
interface CascaderOption {
  id: string;
  name: string;
  level: number;
  code: string;
  countryCode: string;
  parentId: string | null;
  regionType: string;
  leaf?: boolean;
  children?: CascaderOption[];
}

async function loadCountries(keyword?: string) {
  countryLoading.value = true;
  try {
    const data: Country[] = await listCountries({ keyword, includeInactive: false });
    countryOptions.value = data.map(toCountryOption);
  } catch (e) {
    countryOptions.value = [];
  } finally {
    countryLoading.value = false;
  }
}

function onCountryDropdownOpen(open: boolean) {
  if (open && countryOptions.value.length === 0) {
    loadCountries();
  }
}

function onCountryCleared() {
  // Per brief §十三: clearing the Country must not erase the
  // legacy address text. We keep Region / City / AddressLine1/2 /
  // PostalCode intact and just clear the Region binding.
  formData.administrativeRegionId = null;
  cnRegionPath.value = [];
  cnRegionOptions.value = [];
  cnRegionEmpty.value = false;
}

// Build cascader options from a flat list of regions by
// composing parentId -> children. lazy=false; we load the full
// top-level list (CN currently has 0 rows in the repo, so this
// is cheap; once Operator imports MCA dataset the CN tree will
// grow to ~3 300 rows which is still acceptable for client-side
// rendering).
function buildCascaderTree(regions: AdministrativeRegion[]): CascaderOption[] {
  const byParent = new Map<string | null, CascaderOption[]>();
  const opts: CascaderOption[] = regions.map(r => ({
    id: r.id,
    name: r.name,
    level: r.level,
    code: r.code,
    countryCode: r.countryCode,
    parentId: r.parentId,
    regionType: r.regionType,
  }));
  for (const o of opts) {
    const key = o.parentId;
    if (!byParent.has(key)) byParent.set(key, []);
    byParent.get(key)!.push(o);
  }
  for (const o of opts) {
    o.children = byParent.get(o.id) ?? [];
  }
  return byParent.get(null) ?? [];
}

async function loadCnRegions() {
  if (formData.countryCode !== 'CN') {
    cnRegionOptions.value = [];
    cnRegionEmpty.value = false;
    return;
  }
  cnRegionLoading.value = true;
  try {
    // Wave 5.2: cascader is now lazy; eagerly preload only the
    // L1 (parentId=null) so the first column is rendered before
    // the user clicks. The lazyLoad callback above handles L2
    // and L3 on demand.
    const regions = await listRegions({ countryCode: 'CN', parentId: null, includeInactive: false });
    cnRegionOptions.value = regions.map(toCascaderOption);
    cnRegionEmpty.value = regions.length === 0;
  } catch (e) {
    cnRegionOptions.value = [];
    cnRegionEmpty.value = true;
  } finally {
    cnRegionLoading.value = false;
  }
}

function onCnRegionChange(value: string | null) {
  // value is the leaf region's id (we set emitPath=false).
  if (!value) {
    formData.administrativeRegionId = null;
    return;
  }
  formData.administrativeRegionId = value;
}

const isCnCountry = computed(() => formData.countryCode === 'CN');

// ===== Form state =====
const formDrawerVisible = ref(false);
const editingId = ref<string | null>(null);
const editingConcurrency = ref(0);
const submitting = ref(false);
const formData = reactive<BusinessPartnerForm>(emptyForm());
// Transient display-only fields from the server-derived snapshot.
// The form never writes these back; the wave-3 backend ignores
// them on the wire.
const formRegionCodeSnapshot = ref<string | null>(null);
const formRegionNameSnapshot = ref<string | null>(null);
const mnemonicEditedByUser = ref(false);
const lastMnemonicSuggestion = ref('');

function emptyForm(): BusinessPartnerForm {
  return {
    code: '', name: '', shortName: '', mnemonicCode: '',
    role: defaultRole.value === 'supplier' ? 'SUPPLIER'
      : defaultRole.value === 'customer' ? 'CUSTOMER'
        : 'CUSTOMER',
    contactPerson: '', phone: '', email: '',
    addressLine1: '', addressLine2: '', city: '', region: '',
    postalCode: '', countryCode: '', taxNumber: '',
    administrativeRegionId: null,
    status: 'active', description: '',
  };
}

const formRules: FormRules = {
  code: [
    { validator: (_rule, value, cb) => {
      // Edit: code is immutable + pre-loaded.
      if (editingId.value) return cb();
      // Create: empty is OK (auto-generate), but if provided must
      // match the 2..40 UPPER_SNAKE format. We let the server do
      // the canonical validation; here we just require
      // non-whitespace when provided.
      if (value && value.trim().length > 0) {
        if (!/^[A-Za-z][A-Za-z0-9_]{0,39}$/.test(value.trim())) {
          return cb(new Error('代码仅允许字母/数字/下划线，首字符为字母'));
        }
      }
      return cb();
    }, trigger: 'blur' },
  ],
  name: [{ required: true, message: '请输入客商名称', trigger: 'blur' }],
  role: [{ required: true, message: '请选择类型', trigger: 'change' }],
  email: [{ type: 'email', message: '邮箱格式不正确', trigger: 'blur' }],
  countryCode: [
    { validator: (_rule, value, cb) => {
      if (value && !/^[A-Za-z]{2}$/.test(value)) {
        return cb(new Error('请输入 2 位国家代码（如 CN）'));
      }
      return cb();
    }, trigger: 'blur' },
  ],
};

function resetForm() {
  Object.assign(formData, emptyForm());
  cnRegionPath.value = [];
  formRegionNameSnapshot.value = null;
  formRegionCodeSnapshot.value = null;
  mnemonicEditedByUser.value = false;
  lastMnemonicSuggestion.value = '';
}

async function openCreate() {
  editingId.value = null;
  editingConcurrency.value = 0;
  resetForm();
  // Pre-load Country options so the user can pick right away.
  if (countryOptions.value.length === 0) {
    loadCountries();
  }
  formDrawerVisible.value = true;
}

async function openEdit(row: BusinessPartner) {
  editingId.value = row.id;
  editingConcurrency.value = row.concurrencyVersion ?? 0;
  try {
    const fresh = await bpApi.getBusinessPartner(row.id);
    fillForm(fresh);
    editingConcurrency.value = fresh.concurrencyVersion ?? 0;
  } catch (e) {
    fillForm(row);
  }
  // Load Country + CN region (if applicable) for the form.
  if (countryOptions.value.length === 0) {
    loadCountries();
  }
  if (formData.countryCode === 'CN') {
    await loadCnRegions();
    if (formData.administrativeRegionId) {
      // Pre-populate the cascader path from the bound region.
      const bound = await getRegionByCode('CN', row.regionCodeSnapshot ?? '');
      if (bound) {
        cnRegionPath.value = [bound.id];
      }
    }
  }
  formDrawerVisible.value = true;
}

function fillForm(d: BusinessPartner) {
  const suggestedMnemonic = generateMnemonicCode(d.name);
  Object.assign(formData, {
    code: d.code, name: d.name, shortName: d.shortName || '',
    mnemonicCode: d.mnemonicCode || '',
    role: d.role, contactPerson: d.contactPerson || '', phone: d.phone || '',
    email: d.email || '', addressLine1: d.addressLine1 || '',
    addressLine2: d.addressLine2 || '', city: d.city || '', region: d.region || '',
    postalCode: d.postalCode || '', countryCode: d.countryCode || '',
    taxNumber: d.taxNumber || '',
    administrativeRegionId: d.administrativeRegionId || null,
    status: d.status, description: d.description || '',
  });
  // Server-derived snapshots. Display-only in the form layer;
  // the backend regenerates them on every save.
  formRegionNameSnapshot.value = d.regionNameSnapshot ?? null;
  formRegionCodeSnapshot.value = d.regionCodeSnapshot ?? null;
  lastMnemonicSuggestion.value = suggestedMnemonic;
  mnemonicEditedByUser.value = !!d.mnemonicCode
    && d.mnemonicCode.trim() !== suggestedMnemonic;
}

function isConcurrencyConflict(err: unknown): boolean {
  return err instanceof ApiError && err.code === 'mdm_validation_failed'
    && /concurrency|version|并发/i.test(err.detail || '');
}

// React to Country change in the form: when the user picks CN we
// load the cascader data; when they pick a non-CN Country we
// clear the binding but DO NOT clear legacy Region text.
watch(() => formData.countryCode, async (next, prev) => {
  if (next === prev) return;
  if (next === 'CN') {
    await loadCnRegions();
  } else {
    cnRegionOptions.value = [];
    cnRegionEmpty.value = false;
    cnRegionPath.value = [];
  }
  // Switching the Country clears the binding (snapshots are
  // derived on save from the new Region). Legacy Region text
  // is preserved as-is — it is the user's free-text fallback
  // for non-CN countries or for legacy historical rows.
  formData.administrativeRegionId = null;
});

watch(() => formData.name, (next) => {
  const generated = generateMnemonicCode(next);
  if (shouldRefreshMnemonic(formData.mnemonicCode, lastMnemonicSuggestion.value, mnemonicEditedByUser.value)) {
    formData.mnemonicCode = generated;
    lastMnemonicSuggestion.value = generated;
  }
});

function onMnemonicInput(value: string) {
  const trimmed = value.trim();
  if (!trimmed) {
    mnemonicEditedByUser.value = false;
    const generated = generateMnemonicCode(formData.name);
    formData.mnemonicCode = generated;
    lastMnemonicSuggestion.value = generated;
    return;
  }
  mnemonicEditedByUser.value = trimmed !== lastMnemonicSuggestion.value;
}

async function handleSubmit() {
  submitting.value = true;
  try {
    if (editingId.value == null) {
      await bpApi.createBusinessPartner(formData);
      ElMessage.success('创建客商成功');
    } else {
      await bpApi.updateBusinessPartner(editingId.value, formData, editingConcurrency.value);
      ElMessage.success('保存修改成功');
    }
    formDrawerVisible.value = false;
    await fetchList();
  } catch (e) {
    if (isConcurrencyConflict(e)) {
      ElMessage.warning('并发冲突：数据已被其他用户修改，请刷新后重试');
    } else if (e instanceof ApiError) {
      ElMessage.error(formatApiError(e));
    } else if (e instanceof Error && e.message) {
      ElMessage.error(e.message);
    } else {
      ElMessage.error('保存失败，请重试');
    }
  } finally {
    submitting.value = false;
  }
}

function formatApiError(e: ApiError): string {
  if (e.code === 'mdm_location_parent_warehouse_cross_scope') {
    return '所选仓库不属于当前公司或无权访问';
  }
  if (e.code === 'mdm_business_partner_country_code_unknown') {
    return '国家代码无效：仅接受当前已激活的 ISO 3166-1 alpha-2 代码';
  }
  if (e.code === 'mdm_business_partner_region_cross_country') {
    return '行政区划与所选国家不一致';
  }
  const parts = [e.title];
  if (e.detail) parts.push(e.detail);
  if (e.requestId) parts.push(`(${e.requestId})`);
  return parts.join(' — ');
}

// ===== Detail state =====
const detailDrawerVisible = ref(false);
const detailData = ref<BusinessPartner | null>(null);

async function openDetail(row: BusinessPartner) {
  try {
    detailData.value = await bpApi.getBusinessPartner(row.id);
  } catch (e) {
    detailData.value = row;
    if (e instanceof ApiError && e.code !== 'authentication_required') {
      ElMessage.warning('读取最新详情失败，显示列表快照');
    }
  }
  detailDrawerVisible.value = true;
}

function openEditFromDetail() {
  if (detailData.value) {
    openEdit(detailData.value);
    detailDrawerVisible.value = false;
  }
}

// ===== Helpers =====
function formatDate(iso?: string): string {
  if (!iso) return '—';
  const d = new Date(iso);
  return d.toLocaleString('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' });
}
function getStatusLabel(status?: MasterDataStatus): string {
  return STATUS_OPTIONS.find(o => o.value === status)?.label || '—';
}
function roleLabel(role?: BusinessPartnerRole): string {
  return BP_ROLE_OPTIONS.find(o => o.value === role)?.label || '—';
}
function roleTagType(role?: BusinessPartnerRole): 'primary' | 'success' | 'warning' {
  if (role === 'CUSTOMER') return 'primary';
  if (role === 'SUPPLIER') return 'success';
  return 'warning';
}

// ===== Status change =====
async function confirmDeactivate(row: BusinessPartner) {
  try {
    await ElMessageBox.confirm(
      `确定停用"${row.name}"吗？停用后将不能用于新的业务单据，历史数据不受影响。`,
      '停用确认',
      { confirmButtonText: '停用', cancelButtonText: '取消', type: 'warning' },
    );
  } catch { return; }
  try {
    await bpApi.setBusinessPartnerStatus(row, 'inactive');
    ElMessage.success('已停用');
    await fetchList();
  } catch (e) {
    if (isConcurrencyConflict(e)) ElMessage.warning('并发冲突：数据已被其他用户修改，请刷新后重试');
    else if (e instanceof ApiError) ElMessage.error(formatApiError(e));
    else ElMessage.error('停用失败');
  }
}

async function confirmActivate(row: BusinessPartner) {
  try {
    await ElMessageBox.confirm(`确定启用"${row.name}"吗？`, '启用确认',
      { confirmButtonText: '启用', cancelButtonText: '取消', type: 'info' });
  } catch { return; }
  try {
    await bpApi.setBusinessPartnerStatus(row, 'active');
    ElMessage.success('已启用');
    await fetchList();
  } catch (e) {
    if (isConcurrencyConflict(e)) ElMessage.warning('并发冲突：数据已被其他用户修改，请刷新后重试');
    else if (e instanceof ApiError) ElMessage.error(formatApiError(e));
    else ElMessage.error('启用失败');
  }
}

function exportData() {
  ElMessage.info('导出功能待后端支持');
}
</script>

<style scoped>
/* GULIERP_PAGE_THEME_AUDIT_001 / Phase 1 (2026-08-23).
   .mdm-list, .mdm-code, .mdm-detail-title, .mdm-detail-name,
   .mdm-error-banner are shared (see design-system/components/mdm-page.css).
   This page keeps only filter sizing that is specific to business partners. */
.bp-filter-role {
  width: 150px;
}
.bp-filter-status {
  width: var(--col-status);
}

/* GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 - Wave 4:
   3 logical form sections. */
.bp-form-section {
  margin-bottom: 8px;
}
.bp-form-section-title {
  font-size: 13px;
  font-weight: 600;
  color: var(--el-text-color-primary, #303133);
  margin: 16px 0 8px 0;
  padding-bottom: 4px;
  border-bottom: 1px solid var(--el-border-color-lighter, #ebeef5);
}

/* Country selector: custom 3-column option rendering. */
.bp-country-option {
  display: flex;
  align-items: center;
  gap: 8px;
}
.bp-country-code {
  font-family: var(--el-font-family-monospace, monospace);
  font-weight: 600;
  width: 28px;
  flex-shrink: 0;
}
.bp-country-zh {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
}
.bp-country-en {
  color: var(--el-text-color-secondary, #909399);
  font-size: 12px;
  flex-shrink: 0;
}

/* Region hint shown when CN has 0 rows. */
.bp-region-hint {
  margin-top: 4px;
  font-size: 12px;
  color: var(--el-color-warning, #e6a23c);
  line-height: 1.4;
}
</style>
