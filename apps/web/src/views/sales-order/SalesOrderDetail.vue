<template>
  <!-- SalesOrderDetail — read-only detail view per ERP_DOCUMENT_UX_REQUIREMENTS_V1.md §2 -->
  <div class="so-detail" v-if="so">
    <!-- Header bar -->
    <div class="detail-headerbar">
      <div class="headerbar-left">
        <el-button text @click="goBack">
          <el-icon><ArrowLeft /></el-icon>
          返回
        </el-button>
        <el-divider direction="vertical" />
        <div class="doc-meta">
          <span class="doc-no">{{ so.salesOrderNo }}</span>
          <span class="doc-type">{{ documentTypeLabel }}</span>
        </div>
        <div class="status-tags">
          <el-tooltip content="单据状态:文档生命周期(Draft/Active/Closed/Cancelled)" placement="bottom">
            <el-tag :type="documentStatusMap[so.documentStatus].tag" size="small" effect="dark">
              {{ documentStatusMap[so.documentStatus].label }}
            </el-tag>
          </el-tooltip>
          <el-tooltip content="审批状态:审批生命周期(NotSubmitted/Pending/Approved/Rejected/Withdrawn)" placement="bottom">
            <el-tag :type="approvalStatusMap[so.approvalStatus].tag" size="small" effect="light">
              {{ approvalStatusMap[so.approvalStatus].label }}
            </el-tag>
          </el-tooltip>
          <el-tooltip content="执行状态:执行生命周期(NotStarted/Partial/Completed)" placement="bottom">
            <el-tag :type="executionStatusMap[so.executionStatus].tag" size="small" effect="plain">
              {{ executionStatusMap[so.executionStatus].label }}
            </el-tag>
          </el-tooltip>
        </div>
      </div>
      <div class="headerbar-right">
        <el-button v-if="actions.edit" type="primary" @click="openEdit">
          <el-icon><EditPen /></el-icon>编辑
        </el-button>
        <el-button v-if="actions.approve" type="success" @click="onApprove">
          <el-icon><Select /></el-icon>审核通过
        </el-button>
        <el-button v-if="actions.approve" type="danger" @click="onReject">
          <el-icon><CloseBold /></el-icon>驳回
        </el-button>
        <el-button v-if="actions.withdraw" type="warning" @click="onWithdraw">
          <el-icon><Back /></el-icon>撤回
        </el-button>
        <el-button v-if="actions.resubmit" type="primary" @click="onResubmit">
          <el-icon><Promotion /></el-icon>重新提交
        </el-button>
        <el-button v-if="actions.cancel" type="danger" plain @click="onCancel">
          <el-icon><CircleClose /></el-icon>取消
        </el-button>
        <el-button v-if="actions.close" @click="onClose">
          <el-icon><Lock /></el-icon>关闭
        </el-button>
        <el-divider direction="vertical" />
        <el-button @click="onPrint"><el-icon><Printer /></el-icon>打印</el-button>
        <el-button @click="onCopy"><el-icon><CopyDocument /></el-icon>复制</el-button>
        <el-button v-if="actions.generateShipment" type="primary" plain @click="onGenerateShipment">
          <el-icon><Van /></el-icon>生成发货单
        </el-button>
      </div>
    </div>

    <!-- Reject banner -->
    <div v-if="so.approvalStatus === 'Rejected' && lastRejectReason" class="reject-banner">
      <el-icon><WarningFilled /></el-icon>
      <span><b>驳回原因:</b>{{ lastRejectReason }}</span>
      <span style="margin-left:auto;color:#909399;font-size:12px">{{ lastRejectAt }}</span>
    </div>

    <!-- Cancel/Close reason banner -->
    <div v-if="(so.documentStatus === 'Cancelled' || so.documentStatus === 'Closed') && lastActionReason" class="terminal-banner">
      <el-icon><InfoFilled /></el-icon>
      <span><b>{{ so.documentStatus === 'Cancelled' ? '取消原因' : '关闭原因' }}:</b>{{ lastActionReason }}</span>
    </div>

    <!-- Header info display -->
    <div class="detail-section">
      <div class="section-title">
        <el-icon><Document /></el-icon>
        <span>单据头信息</span>
      </div>
      <el-descriptions :column="4" border size="small">
        <el-descriptions-item label="单据编号">
          <span class="mono">{{ so.salesOrderNo }}</span>
        </el-descriptions-item>
        <el-descriptions-item label="订单日期">{{ so.orderDate }}</el-descriptions-item>
        <el-descriptions-item label="交货日期">{{ so.requestedDeliveryDate }}</el-descriptions-item>
        <el-descriptions-item label="单据类型">{{ documentTypeLabel }}</el-descriptions-item>
        <el-descriptions-item label="客户" :span="2">
          <span class="link">{{ so.customerName }}</span>
          <span class="mono small">({{ so.customerCode }})</span>
        </el-descriptions-item>
        <el-descriptions-item label="联系人">{{ so.contactName || '—' }}</el-descriptions-item>
        <el-descriptions-item label="客户订单号">{{ so.customerOrderNo || '—' }}</el-descriptions-item>
        <el-descriptions-item label="销售员">{{ so.salesPersonName }}</el-descriptions-item>
        <el-descriptions-item label="销售部门">{{ so.salesOrgName || '—' }}</el-descriptions-item>
        <el-descriptions-item label="公司主体">{{ so.companyName }}</el-descriptions-item>
        <el-descriptions-item label="币种">{{ so.currencyCode }} (汇率 {{ so.exchangeRate }})</el-descriptions-item>
        <el-descriptions-item label="付款条件">{{ paymentTermLabel }}</el-descriptions-item>
        <el-descriptions-item label="结算方式">{{ so.settlementMethod || '—' }}</el-descriptions-item>
        <el-descriptions-item label="价格模式">
          <el-tag size="small" effect="plain">{{ so.defaultPriceMode === 'TaxInclusive' ? '含税' : '未税' }}</el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="默认税率">{{ (so.defaultTaxRate * 100).toFixed(2) }}%</el-descriptions-item>
        <el-descriptions-item label="默认仓库">{{ so.defaultWarehouseName || '—' }}</el-descriptions-item>
        <el-descriptions-item label="交货方式">{{ so.deliveryMethod || '—' }}</el-descriptions-item>
        <el-descriptions-item label="交货地址" :span="4">{{ so.shipToAddress || '—' }}</el-descriptions-item>
        <el-descriptions-item label="备注" :span="4">{{ so.memo || '—' }}</el-descriptions-item>
        <el-descriptions-item label="创建人">{{ so.createdBy }}</el-descriptions-item>
        <el-descriptions-item label="创建时间">{{ so.createdAt }}</el-descriptions-item>
        <el-descriptions-item label="更新时间">{{ so.updatedAt }}</el-descriptions-item>
        <el-descriptions-item label="乐观锁版本">v{{ so.concurrencyVersion }}</el-descriptions-item>
      </el-descriptions>
    </div>

    <!-- Lines -->
    <div class="detail-section">
      <div class="section-title">
        <el-icon><Grid /></el-icon>
        <span>订单明细</span>
        <span class="section-sub">{{ so.lines.length }} 行</span>
      </div>

      <div class="lines-table-wrap" :style="{ '--line-table-total-width': lineTableTotalWidth + 'px' }">
        <el-table
          :data="so.lines"
          border
          stripe
          size="small"
          :header-cell-style="{ background: '#f1f5f9', padding: '0 !important' }"
          :cell-style="cellStyle"
        >
          <el-table-column label="行号" prop="lineNo" :width="lineColWidth('lineNo')" align="center" fixed="left">
            <template #header>
              <div class="th-two-tier"><div class="th-agg"></div><div class="th-label">行号</div></div>
            </template>
          </el-table-column>
          <el-table-column label="商品" :width="lineColWidth('item')" fixed="left">
            <template #header>
              <div class="th-two-tier"><div class="th-agg agg-left agg-bold">合计</div><div class="th-label">商品</div></div>
            </template>
            <template #default="{ row }">
              <div class="item-cell">
                <div class="item-code">{{ row.itemCode }}</div>
                <div class="item-name">{{ row.itemName }}</div>
              </div>
            </template>
          </el-table-column>
          <template v-for="col in visibleLineColumns" :key="col.prop">
            <el-table-column
              :prop="col.prop"
              :label="col.label"
              :width="col.width"
              :align="detailColAlign(col.prop)"
              :show-overflow-tooltip="col.prop === 'itemSpec'"
            >
              <template #header>
                <div class="th-two-tier">
                  <div class="th-agg" :class="aggClass(col)">
                    <el-tooltip
                      v-if="col.prop === 'quantity' && quantityAggMeta.multi"
                      content="存在多个计量单位，数量不汇总"
                      placement="bottom"
                      :show-after="150"
                    >
                      <span class="multi-uom-hint">多单位</span>
                    </el-tooltip>
                    <template v-else>{{ getAggregateDisplay(col) }}</template>
                  </div>
                  <div class="th-label">{{ col.label }}</div>
                </div>
              </template>
              <template #default="{ row }">
                <component :is="renderDetailCell(col, row)" />
              </template>
            </el-table-column>
          </template>
          <el-table-column label="行状态" width="80" align="center">
            <template #header>
              <div class="th-two-tier"><div class="th-agg"></div><div class="th-label">行状态</div></div>
            </template>
            <template #default="{ row }">
              <el-tag size="small" :type="row.lineStatus === 'Open' ? 'success' : 'info'" effect="plain">
                {{ row.lineStatus === 'Open' ? '未关闭' : '已关闭' }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column label="备注" prop="memo" :width="lineColWidth('memo')" show-overflow-tooltip>
            <template #header>
              <div class="th-two-tier"><div class="th-agg"></div><div class="th-label">备注</div></div>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </div>

    <!-- Tabs -->
    <div class="detail-section">
      <el-tabs v-model="activeTab" type="border-card">
        <el-tab-pane name="approval">
          <template #label>
            <el-icon><Checked /></el-icon> 审批记录
            <el-badge v-if="so.approvalHistory.length" :value="so.approvalHistory.length" type="primary" />
          </template>
          <div class="tab-content">
            <el-timeline v-if="so.approvalHistory.length">
              <el-timeline-item
                v-for="a in so.approvalHistory"
                :key="a.id"
                :type="approvalTimelineType(a.status)"
                :timestamp="a.at"
                placement="top"
              >
                <div class="approval-item">
                  <span class="step">{{ a.step }}</span>
                  <el-tag size="small" :type="approvalTagType(a.status)">{{ approvalStatusLabel(a.status) }}</el-tag>
                  <span class="approver">{{ a.approver }} ({{ roleLabel(a.approverRole) }})</span>
                  <div v-if="a.comment" class="comment">意见:{{ a.comment }}</div>
                </div>
              </el-timeline-item>
            </el-timeline>
            <el-empty v-else description="暂无审批记录" :image-size="60" />
          </div>
        </el-tab-pane>
        <el-tab-pane name="attachments">
          <template #label>
            <el-icon><Paperclip /></el-icon> 附件
            <el-badge v-if="so.attachments.length" :value="so.attachments.length" type="primary" />
          </template>
          <div class="tab-content">
            <el-table v-if="so.attachments.length" :data="so.attachments" size="small" border>
              <el-table-column prop="name" label="文件名" />
              <el-table-column label="大小" width="120">
                <template #default="{ row }">{{ (row.size / 1024).toFixed(1) }} KB</template>
              </el-table-column>
              <el-table-column prop="uploadedAt" label="上传时间" width="180" />
              <el-table-column prop="uploadedBy" label="上传人" width="100" />
              <el-table-column label="操作" width="160">
                <template #default>
                  <el-button text size="small" type="primary">预览</el-button>
                  <el-button text size="small">下载</el-button>
                </template>
              </el-table-column>
            </el-table>
            <el-empty v-else description="暂无附件" :image-size="60" />
          </div>
        </el-tab-pane>
        <el-tab-pane name="relations">
          <template #label><el-icon><Link /></el-icon> 来源/下游</template>
          <div class="tab-content">
            <div class="rel-section">
              <h4>来源单据</h4>
              <el-table :data="so.sourceDocument ? [so.sourceDocument] : []" size="small" border>
                <el-table-column label="单据类型" width="120">
                  <template #default="{ row }">{{ row.docTypeLabel }}</template>
                </el-table-column>
                <el-table-column prop="no" label="单号" />
                <el-table-column prop="date" label="日期" width="120" />
                <el-table-column label="金额" width="120" align="right">
                  <template #default="{ row }">¥ {{ fmtMoney(row.amount) }}</template>
                </el-table-column>
                <el-table-column prop="status" label="状态" width="100" />
                <el-table-column label="操作" width="120">
                  <template #default><el-button text size="small" type="primary">在新标签页打开</el-button></template>
                </el-table-column>
                <template #empty><el-empty description="无来源单据(本单为手工新建)" :image-size="50" /></template>
              </el-table>
            </div>
            <div class="rel-section">
              <h4>下游单据</h4>
              <el-table :data="so.downstream" size="small" border>
                <el-table-column label="单据类型" width="120">
                  <template #default="{ row }">{{ row.docTypeLabel }}</template>
                </el-table-column>
                <el-table-column prop="no" label="单号" />
                <el-table-column prop="date" label="日期" width="120" />
                <el-table-column label="金额" width="120" align="right">
                  <template #default="{ row }">¥ {{ fmtMoney(row.amount) }}</template>
                </el-table-column>
                <el-table-column prop="status" label="状态" width="100" />
                <el-table-column label="操作" width="120">
                  <template #default><el-button text size="small" type="primary">在新标签页打开</el-button></template>
                </el-table-column>
                <template #empty><el-empty description="暂无下游单据(审核通过后可生成发货单)" :image-size="50" /></template>
              </el-table>
            </div>
          </div>
        </el-tab-pane>
        <el-tab-pane name="audit">
          <template #label><el-icon><List /></el-icon> 操作日志</template>
          <div class="tab-content">
            <el-table :data="so.auditLog" size="small" border>
              <el-table-column prop="actionLabel" label="操作" width="120" />
              <el-table-column prop="actor" label="操作人" width="100" />
              <el-table-column label="角色" width="100">
                <template #default="{ row }">{{ roleLabel(row.actorRole) }}</template>
              </el-table-column>
              <el-table-column prop="at" label="时间" width="180" />
              <el-table-column prop="reason" label="原因" />
              <el-table-column prop="remark" label="备注" />
            </el-table>
          </div>
        </el-tab-pane>
        <el-tab-pane name="print">
          <template #label><el-icon><Printer /></el-icon> 打印</template>
          <div class="tab-content">
            <div class="print-section">
              <p class="hint">选择打印模板,预览后将生成 PDF 并记录打印日志。</p>
              <el-select v-model="printTemplate" style="width: 240px">
                <el-option label="标准销售订单模板" value="std" />
                <el-option label="客户专用模板" value="cust" />
                <el-option label="简版(无价格)" value="simple" />
              </el-select>
              <el-button type="primary" @click="onPrint"><el-icon><Printer /></el-icon>打印预览</el-button>
              <el-button>导出 PDF</el-button>
              <el-button>导出 Excel</el-button>
            </div>
          </div>
        </el-tab-pane>
      </el-tabs>
    </div>

    <!-- Reason dialog -->
    <el-dialog v-model="reasonDialog.visible" :title="reasonDialog.title" width="460px">
      <el-form>
        <el-form-item label="原因" required>
          <el-input v-model="reasonDialog.reason" type="textarea" :rows="3" :placeholder="reasonDialog.placeholder" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="reasonDialog.visible = false">取消</el-button>
        <el-button :type="reasonDialog.okType" @click="confirmReason">确认</el-button>
      </template>
    </el-dialog>
  </div>
  <el-empty v-else description="未找到该销售订单" />
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch, onMounted, h } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, ElTag } from 'element-plus';
import {
  ArrowLeft, EditPen, Select, CloseBold, Back, Promotion, CircleClose, Lock,
  Printer, CopyDocument, Van, Document, Grid, Paperclip, Link, List,
  Checked, WarningFilled, InfoFilled
} from '@element-plus/icons-vue';
import { useTabsStore } from '../../stores/tabs';
import { useSalesOrderStore } from '../../stores/sales-order';
import { paymentTerms, defaultLineColumns, uoms } from '../../mock/sales-order';
import type { SalesOrder, SalesOrderLine, LineColumnConfig } from '../../types/sales-order';
import {
  documentStatusMap, approvalStatusMap, executionStatusMap,
  fmtMoney, computeActions
} from '../../utils/status';

defineOptions({ name: 'SalesOrderDetail' });

const route = useRoute();
const router = useRouter();
const tabs = useTabsStore();
const soStore = useSalesOrderStore();

const so = ref<SalesOrder | null>(null);
const activeTab = ref('approval');
const printTemplate = ref('std');

// ===== Line column config (shared with Edit via localStorage — FIX 3) =====
const LS_LINE_COLS = 'erp.so.edit.lineColumns';
function loadLineColumns(): LineColumnConfig[] {
  // G1B-1R8: structural metadata (prop/label/core/fixed/aggregate) always from defaultLineColumns.
  // User prefs only override: visible, width, order (via array position in saved).
  const defaults = JSON.parse(JSON.stringify(defaultLineColumns)) as LineColumnConfig[];
  try {
    const raw = localStorage.getItem(LS_LINE_COLS);
    if (!raw) return defaults;
    const saved = JSON.parse(raw) as Array<{ prop: string; visible?: boolean; width?: number }>;
    const defaultMap = new Map(defaults.map(d => [d.prop, d]));
    const result: LineColumnConfig[] = [];
    const used = new Set<string>();
    for (const s of saved) {
      const dc = defaultMap.get(s.prop);
      if (!dc) continue;
      result.push({
        ...dc,
        visible: s.visible !== undefined ? s.visible : dc.visible,
        width: s.width || dc.width
      });
      used.add(s.prop);
    }
    for (const dc of defaults) {
      if (!used.has(dc.prop)) result.push({ ...dc });
    }
    return result;
  } catch (e) { /* ignore */ }
  return defaults;
}
const lineColumns = ref<LineColumnConfig[]>(loadLineColumns());
const visibleLineColumns = computed(() => lineColumns.value.filter(c => c.visible && !['lineNo', 'item', 'memo'].includes(c.prop)));
function lineColWidth(prop: string): number {
  return lineColumns.value.find(c => c.prop === prop)?.width || 100;
}
function detailColAlign(prop: string): 'center' | 'right' | undefined {
  if (prop === 'uomName') return 'center';
  if (['quantity', 'unitPriceExclTax', 'unitPriceInclTax', 'discountRate', 'discountAmount', 'amountExclTax', 'taxAmount', 'amountInclTax', 'executedQuantity', 'openQuantity'].includes(prop)) return 'right';
  return undefined;
}

// ===== Read-only cell renderer for Detail table =====
function renderDetailCell(col: LineColumnConfig, row: SalesOrderLine) {
  switch (col.prop) {
    case 'itemSpec':
      return h('span', { style: 'font-size:12.5px' }, row.itemSpec || '—');
    case 'uomName':
      return h('span', { style: 'text-align:center;display:block;width:100%' }, row.uomName);
    case 'quantity':
      return h('span', { class: 'num-cell', style: 'text-align:right;display:block' }, fmtMoney(row.quantity, 4));
    case 'linePriceMode':
      return h(ElTag, { size: 'small', effect: 'plain' }, () => row.linePriceMode === 'TaxInclusive' ? '含税' : '未税');
    case 'unitPriceExclTax':
      return h('span', { class: 'num-cell', style: 'text-align:right;display:block' }, fmtMoney(row.unitPriceExclTax, 4));
    case 'unitPriceInclTax':
      return h('span', { class: 'num-cell', style: 'text-align:right;display:block' }, fmtMoney(row.unitPriceInclTax));
    case 'lineTaxRate':
      return h('span', { style: 'text-align:center;display:block' }, (row.lineTaxRate * 100).toFixed(2) + '%');
    case 'discountRate':
      return h('span', { style: 'text-align:right;display:block' }, row.discountRate > 0 ? (row.discountRate * 100).toFixed(2) + '%' : '—');
    case 'discountAmount':
      return h('span', { class: 'num-cell', style: 'text-align:right;display:block' }, row.discountAmount > 0 ? fmtMoney(row.discountAmount) : '—');
    case 'amountExclTax':
      return h('span', { class: 'num-cell', style: 'text-align:right;display:block' }, fmtMoney(row.amountExclTax, 4));
    case 'taxAmount':
      return h('span', { class: 'num-cell', style: 'text-align:right;display:block' }, fmtMoney(row.taxAmount));
    case 'amountInclTax':
      return h('span', { class: 'num-cell strong', style: 'text-align:right;display:block;font-weight:700' }, fmtMoney(row.amountInclTax));
    case 'warehouseName':
      return h('span', { style: 'font-size:12.5px' }, row.warehouseName || '—');
    case 'locationName':
      return h('span', { style: 'font-size:12.5px' }, row.locationName || '—');
    case 'lineDeliveryDate':
      return h('span', { style: 'font-size:12.5px' }, row.lineDeliveryDate || '—');
    case 'executedQuantity':
      return h('span', { class: ['num-cell', row.executedQuantity > 0 && row.executedQuantity < row.quantity ? 'partial' : '', row.executedQuantity >= row.quantity ? 'done' : ''], style: 'text-align:right;display:block' }, fmtMoney(row.executedQuantity, 4));
    case 'openQuantity':
      return h('span', { class: ['num-cell', row.openQuantity > 0 ? 'open' : ''], style: 'text-align:right;display:block' }, fmtMoney(row.openQuantity, 4));
    default:
      return h('span', String((row as any)[col.prop] ?? ''));
  }
}

// ===== G1B-1R8: Two-Tier Aggregate Header — driven by same lineColumns =====
const lineTableTotalWidth = computed(() => {
  return lineColumns.value.reduce((s, c) => s + c.width, 0) + 80; // +80 for 行状态 column
});

// ===== G1B-1R9: Quantity Aggregate Semantics =====
// Do NOT sum quantities across different UOMs. Only show SUM when all lines share the same UOM.
// Precision follows the UOM's `decimals` field.
function getUomDecimals(uomId: string): number {
  const u = uoms.find(x => x.id === uomId);
  return u ? u.decimals : 2; // ERP default fallback: 2
}
const quantityAggMeta = computed<{ multi: boolean; uomId?: string; decimals: number }>(() => {
  if (!so.value) return { multi: false, decimals: 0 };
  const ids = Array.from(new Set(so.value.lines.map(l => l.uomId).filter(Boolean)));
  if (ids.length <= 1) {
    return { multi: false, uomId: ids[0], decimals: ids[0] ? getUomDecimals(ids[0]) : 0 };
  }
  // Multiple distinct UOMs — suppress quantity aggregate
  return { multi: true, decimals: 0 };
});

function getSummaryValue(prop: string): string {
  if (!so.value) return '';
  switch (prop) {
    case 'quantity': {
      // G1B-1R9: Multi-UOM → no total displayed (template shows "多单位" with tooltip instead)
      if (quantityAggMeta.value.multi) return '';
      return fmtMoney(so.value.totalQuantity, quantityAggMeta.value.decimals);
    }
    case 'discountAmount': return so.value.totalDiscountAmount > 0 ? '-¥' + fmtMoney(so.value.totalDiscountAmount) : '';
    case 'amountExclTax': return '¥' + fmtMoney(so.value.totalAmountExclTax);
    case 'taxAmount': return '¥' + fmtMoney(so.value.totalTaxAmount);
    case 'amountInclTax': return '¥' + fmtMoney(so.value.totalAmountInclTax);
    default: return '';
  }
}

function getAggregateDisplay(col: LineColumnConfig): string {
  if (col.aggregate === 'SUM') return getSummaryValue(col.prop);
  return '';
}

function aggClass(col: LineColumnConfig): string {
  if (col.prop === 'amountInclTax') return 'agg-right agg-grand';
  if (col.aggregate === 'SUM') return 'agg-right';
  return '';
}

const reasonDialog = reactive({
  visible: false,
  title: '',
  placeholder: '请输入原因',
  okType: 'danger' as 'danger' | 'primary' | 'warning',
  action: '' as 'reject' | 'cancel' | 'close' | 'withdraw',
  reason: ''
});

const actions = computed(() => so.value ? computeActions(so.value) : null as any);
const documentTypeLabel = computed(() => {
  if (!so.value) return '';
  return { Normal: '普通销售', Sample: '样品', Return: '退货' }[so.value.documentType];
});
const paymentTermLabel = computed(() => {
  if (!so.value) return '';
  return paymentTerms.find(p => p.code === so.value!.paymentTermCode)?.name || so.value.paymentTermCode;
});
const lastRejectReason = computed(() => {
  if (!so.value || so.value.approvalStatus !== 'Rejected') return '';
  const last = so.value.approvalHistory.slice().reverse().find(a => a.status === 'Rejected');
  return last?.comment || '';
});
const lastRejectAt = computed(() => {
  if (!so.value) return '';
  const last = so.value.approvalHistory.slice().reverse().find(a => a.status === 'Rejected');
  return last?.at || '';
});
const lastActionReason = computed(() => {
  if (!so.value) return '';
  const action = so.value.documentStatus === 'Cancelled' ? 'Cancel' : so.value.documentStatus === 'Closed' ? 'Close' : '';
  if (!action) return '';
  const last = so.value.auditLog.slice().reverse().find(a => a.action === action);
  return last?.reason || '';
});

function loadOrder(id: string) {
  const found = soStore.getById(id);
  if (found) {
    so.value = JSON.parse(JSON.stringify(found));
  } else {
    so.value = null;
  }
}

onMounted(() => loadOrder(route.params.id as string));
watch(() => route.params.id, (id) => { if (id) loadOrder(id as string); });

function goBack() {
  tabs.setActive('list-sales-order');
  router.push('/sales-order').catch(() => {});
}

function openEdit() {
  if (!so.value) return;
  tabs.openEdit(so.value.id, so.value.salesOrderNo, 'edit');
  router.push(`/sales-order/${so.value.id}/edit`).catch(() => {});
}

function onApprove() {
  if (!so.value) return;
  soStore.applyAction(so.value.id, 'approve');
  const updated = soStore.getById(so.value.id);
  if (updated) so.value = JSON.parse(JSON.stringify(updated));
  ElMessage.success('已审核通过');
}

function onReject() { openReason('驳回订单', '请输入驳回原因(必填)', 'danger', 'reject'); }
function onWithdraw() { openReason('撤回提交', '请输入撤回原因', 'warning', 'withdraw'); }
function onResubmit() {
  if (!so.value) return;
  soStore.applyAction(so.value.id, 'resubmit');
  const updated = soStore.getById(so.value.id);
  if (updated) so.value = JSON.parse(JSON.stringify(updated));
  ElMessage.success('已重新提交审批');
}
function onCancel() { openReason('取消订单', '请输入取消原因(必填)', 'danger', 'cancel'); }
function onClose() { openReason('关闭订单', '请输入关闭原因', 'primary', 'close'); }

function openReason(title: string, placeholder: string, okType: 'danger' | 'primary' | 'warning', action: 'reject' | 'cancel' | 'close' | 'withdraw') {
  reasonDialog.title = title;
  reasonDialog.placeholder = placeholder;
  reasonDialog.okType = okType;
  reasonDialog.action = action;
  reasonDialog.reason = '';
  reasonDialog.visible = true;
}

function confirmReason() {
  if (!reasonDialog.reason && (reasonDialog.action === 'reject' || reasonDialog.action === 'cancel')) {
    ElMessage.warning('原因必填');
    return;
  }
  if (!so.value) return;
  soStore.applyAction(so.value.id, reasonDialog.action as any, reasonDialog.reason);
  const updated = soStore.getById(so.value.id);
  if (updated) so.value = JSON.parse(JSON.stringify(updated));
  reasonDialog.visible = false;
  ElMessage.success(`${reasonDialog.title}成功`);
}

function onPrint() { ElMessage.info('打印预览(Mock)'); }
function onCopy() {
  if (!so.value) return;
  // delegate to store via list/edit flow
  ElMessage.success('已复制为草稿(请在列表中查看)');
}
function onGenerateShipment() { ElMessage.info('将跳转发货单创建页(Mock)'); }

function headerStyle() {
  return { background: '#f1f5f9', color: '#1e293b', fontWeight: 600, padding: '6px 8px', fontSize: '12px' };
}
function cellStyle() {
  return { padding: '5px 8px', fontSize: '12.5px' };
}

function approvalTimelineType(status: string): 'primary' | 'success' | 'danger' | 'warning' | 'info' {
  return { Pending: 'warning', Approved: 'success', Rejected: 'danger', Withdrawn: 'info' }[status] as any || 'info';
}
function approvalTagType(status: string): any {
  return { Pending: 'warning', Approved: 'success', Rejected: 'danger', Withdrawn: 'info' }[status] as any || 'info';
}
function approvalStatusLabel(status: string): string {
  return { Pending: '待审批', Approved: '已通过', Rejected: '已驳回', Withdrawn: '已撤回' }[status] || status;
}
function roleLabel(role: string): string {
  return { Sales: '销售员', SalesManager: '销售经理', Warehouse: '库管员', Finance: '财务' }[role] || role;
}
</script>

<style scoped>
.so-detail {
  display: flex;
  flex-direction: column;
  height: 100%;
  background: #f6f7fb;
  overflow: auto;
}
.detail-headerbar {
  background: #fff;
  border-bottom: 1px solid #e5e7eb;
  padding: 8px 16px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-shrink: 0;
  box-shadow: 0 1px 2px rgba(0,0,0,0.04);
  position: sticky;
  top: 0;
  z-index: 5;
}
.headerbar-left {
  display: flex;
  align-items: center;
  gap: 12px;
}
.doc-meta {
  display: flex;
  align-items: center;
  gap: 8px;
}
.doc-no {
  font-size: 16px;
  font-weight: 700;
  color: #1f2937;
}
.doc-type {
  background: #eff6ff;
  color: #1d4ed8;
  padding: 1px 8px;
  border-radius: 8px;
  font-size: 11px;
}
.status-tags {
  display: flex;
  gap: 4px;
}
.headerbar-right {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}
.reject-banner {
  background: #fef2f2;
  border-left: 4px solid #ef4444;
  border-bottom: 1px solid #fecaca;
  padding: 10px 16px;
  display: flex;
  align-items: center;
  gap: 8px;
  color: #991b1b;
  font-size: 13px;
  flex-shrink: 0;
}
.terminal-banner {
  background: #f3f4f6;
  border-left: 4px solid #6b7280;
  border-bottom: 1px solid #e5e7eb;
  padding: 10px 16px;
  display: flex;
  align-items: center;
  gap: 8px;
  color: #4b5563;
  font-size: 13px;
  flex-shrink: 0;
}
.detail-section {
  background: #fff;
  margin: 8px;
  border-radius: 6px;
  padding: 12px 16px;
  box-shadow: 0 1px 2px rgba(0,0,0,0.04);
}
.section-title {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 14px;
  font-weight: 600;
  color: #1f2937;
  margin-bottom: 12px;
  padding-bottom: 8px;
  border-bottom: 1px solid #f1f5f9;
}
.section-sub {
  font-size: 12px;
  font-weight: 400;
  color: #9ca3af;
  margin-left: 8px;
}
.mono { font-family: 'JetBrains Mono', 'Consolas', monospace; }
.mono.small { font-size: 11px; color: #9ca3af; margin-left: 4px; }
.link { color: #2563eb; cursor: pointer; }
/* G1B-1R8: Two-Tier Aggregate Header — no footer reorder hack.
   Each column's #header slot renders Tier 1 (aggregate) + Tier 2 (label). */
.lines-table-wrap :deep(.el-table__body),
.lines-table-wrap :deep(.el-table__header) {
  width: var(--line-table-total-width, 2600px) !important;
}
.lines-table-wrap :deep(.el-table__body-wrapper) {
  overflow-x: auto !important;
}
.th-two-tier {
  display: flex;
  flex-direction: column;
  align-items: stretch;
  height: 100%;
}
.th-agg {
  height: 22px;
  display: flex;
  align-items: center;
  padding: 0 8px;
  font-size: 12px;
  font-family: 'JetBrains Mono', 'Consolas', monospace;
  color: var(--text-primary, #1f2937);
  font-weight: 600;
  border-bottom: 1px solid var(--border-subtle, #e2e8f0);
  background: var(--bg-subtle, #f8fafc);
}
.th-agg.agg-left { justify-content: flex-start; }
.th-agg.agg-right { justify-content: flex-end; }
.th-agg.agg-bold { font-family: inherit; font-weight: 700; }
.th-agg.agg-grand {
  color: var(--primary-default, #2563eb);
  font-weight: 700;
}
.multi-uom-hint {
  display: inline-flex;
  align-items: center;
  padding: 1px 8px;
  border-radius: 4px;
  background: var(--bg-muted, #f1f5f9);
  color: var(--text-muted, #64748b);
  font-size: 12px;
  font-weight: 500;
  cursor: help;
}
.th-label {
  height: 22px;
  display: flex;
  align-items: center;
  padding: 0 8px;
  font-size: 12px;
  font-weight: 600;
  color: #1e293b;
}
.tab-content {
  padding: 8px 4px;
  min-height: 120px;
}
.rel-section { margin-bottom: 16px; }
.rel-section h4 {
  margin: 0 0 8px;
  font-size: 13px;
  color: #4b5563;
}
.print-section {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}
.print-section .hint {
  width: 100%;
  color: #6b7280;
  font-size: 12px;
  margin: 0 0 8px;
}
.approval-item {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}
.approval-item .step {
  font-weight: 600;
  color: #1f2937;
}
.approval-item .approver {
  color: #6b7280;
  font-size: 12px;
}
.approval-item .comment {
  width: 100%;
  color: #4b5563;
  font-size: 12px;
  padding-left: 4px;
  border-left: 2px solid #e5e7eb;
}
:deep(.num-cell) {
  font-family: 'JetBrains Mono', 'Consolas', monospace;
  display: inline-block;
  width: 100%;
  text-align: right;
}
:deep(.num-cell.strong) { color: #c2410c; font-weight: 700; }
:deep(.num-cell.partial) { color: #d97706; }
:deep(.num-cell.done) { color: #16a34a; }
:deep(.num-cell.open) { color: #2563eb; }
:deep(.num-cell.discount) { color: #d97706; }
.item-cell .item-code {
  font-size: 12px;
  color: #6b7280;
  font-family: monospace;
}
.item-cell .item-name {
  font-size: 12.5px;
  color: #1f2937;
}
</style>
