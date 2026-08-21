<template>
  <!-- SalesOrderEdit — Create/Edit page per SALES_ORDER_BUSINESS_SPEC_V1.md §2/§3/§6
       G1B-1R2 fixes: scroll structure, save draft + Ctrl+S, header field settings, item multi-select -->
  <div class="so-edit" v-if="so">
    <!-- Status banner / header bar (sticky) -->
    <div class="edit-headerbar">
      <div class="headerbar-left">
        <el-button text @click="goBack">
          <el-icon><ArrowLeft /></el-icon>
          返回
        </el-button>
        <el-divider direction="vertical" />
        <span class="doc-no">{{ so.salesOrderNo }}</span>
        <div class="status-tags">
          <el-tag :type="documentStatusMap[so.documentStatus].tag" size="small" effect="dark">
            {{ documentStatusMap[so.documentStatus].label }}
          </el-tag>
          <el-tag :type="approvalStatusMap[so.approvalStatus].tag" size="small" effect="light">
            {{ approvalStatusMap[so.approvalStatus].label }}
          </el-tag>
          <el-tag :type="executionStatusMap[so.executionStatus].tag" size="small" effect="plain">
            {{ executionStatusMap[so.executionStatus].label }}
          </el-tag>
        </div>
        <span class="version-tag">v{{ so.concurrencyVersion }}</span>
        <span v-if="dirty" class="dirty-badge"><el-icon><WarningFilled /></el-icon>未保存</span>
      </div>
      <div class="headerbar-right">
        <!-- Primary: 保存草稿 (always visible when editable, prominent) -->
        <el-button v-if="editable" type="primary" size="default" @click="onSaveDraft">
          <el-icon><Check /></el-icon>保存草稿
          <span class="kbd-hint">Ctrl+S</span>
        </el-button>
        <el-button v-if="actions.submit" type="success" @click="onSubmit">
          <el-icon><Promotion /></el-icon>提交
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
        <el-button text @click="onPrint"><el-icon><Printer /></el-icon>打印</el-button>
        <el-button text @click="onCopy"><el-icon><CopyDocument /></el-icon>复制</el-button>
        <el-button v-if="actions.generateShipment" type="primary" plain @click="onGenerateShipment">
          <el-icon><Van /></el-icon>生成发货单
        </el-button>
      </div>
    </div>

    <!-- Reject reason banner -->
    <div v-if="so.approvalStatus === 'Rejected' && lastRejectReason" class="reject-banner">
      <el-icon><WarningFilled /></el-icon>
      <span><b>驳回原因:</b>{{ lastRejectReason }}</span>
      <span style="margin-left:auto;color:#909399;font-size:12px">{{ lastRejectAt }}</span>
    </div>

    <!-- Header form section -->
    <div class="edit-section">
      <div class="section-title">
        <el-icon><Document /></el-icon>
        <span>单据头信息</span>
        <span class="section-sub">{{ editable ? '可编辑' : '只读(单据已提交)' }}</span>
        <div class="section-tools">
          <el-button text size="small" @click="openFieldSettings">
            <el-icon><Setting /></el-icon>字段设置
          </el-button>
          <el-button text size="small" @click="showMoreFields = !showMoreFields">
            <el-icon><component :is="showMoreFields ? 'ArrowUp' : 'ArrowDown'" /></el-icon>
            {{ showMoreFields ? '收起更多' : '展开更多' }}
          </el-button>
        </div>
      </div>
      <el-form :model="so" label-width="92px" label-position="right" size="default" :disabled="!editable">
        <!-- Always-visible core fields -->
        <div class="form-grid core-grid">
          <el-form-item label="单据编号">
            <el-input v-model="so.salesOrderNo" placeholder="保存时自动生成 SO-YYYYMMDD-####" />
          </el-form-item>
          <el-form-item label="订单日期" required>
            <el-date-picker v-model="so.orderDate" type="date" value-format="YYYY-MM-DD" style="width:100%" />
          </el-form-item>
          <el-form-item label="交货日期" required>
            <el-date-picker v-model="so.requestedDeliveryDate" type="date" value-format="YYYY-MM-DD" style="width:100%" />
          </el-form-item>
          <el-form-item label="单据类型">
            <el-select v-model="so.documentType" style="width:100%">
              <el-option label="普通销售" value="Normal" />
              <el-option label="样品" value="Sample" />
              <el-option label="退货" value="Return" />
            </el-select>
          </el-form-item>
          <el-form-item label="客户" required>
            <el-input v-model="so.customerName" readonly placeholder="点击选择客户">
              <template #append>
                <el-button :icon="Search" :disabled="!editable" @click="openLookup('customer')" />
              </template>
            </el-input>
          </el-form-item>
          <el-form-item label="销售员" required>
            <el-input v-model="so.salesPersonName" readonly placeholder="点击选择销售员">
              <template #append>
                <el-button :icon="Search" :disabled="!editable" @click="openLookup('employee', { role: 'Sales' })" />
              </template>
            </el-input>
          </el-form-item>
          <el-form-item label="默认仓库">
            <el-input v-model="so.defaultWarehouseName" readonly placeholder="点击选择默认仓库">
              <template #append>
                <el-button :icon="Search" :disabled="!editable" @click="openLookup('warehouse')" />
              </template>
            </el-input>
          </el-form-item>
          <el-form-item label="付款条件" required>
            <el-select v-model="so.paymentTermCode" style="width:100%">
              <el-option v-for="p in paymentTerms" :key="p.code" :label="p.name" :value="p.code" />
            </el-select>
          </el-form-item>
        </div>

        <!-- Expandable "more" fields (controlled by showMoreFields toggle + field settings) -->
        <el-collapse-transition>
          <div v-show="showMoreFields" class="form-grid more-grid">
            <el-form-item v-if="fieldVisible.contactName" label="联系人">
              <el-input v-model="so.contactName" readonly placeholder="点击选择联系人">
                <template #append>
                  <el-button :icon="Search" :disabled="!editable || !so.customerId" @click="openLookup('contact', { customerId: so.customerId })" />
                </template>
              </el-input>
            </el-form-item>
            <el-form-item v-if="fieldVisible.customerOrderNo" label="客户订单号">
              <el-input v-model="so.customerOrderNo" placeholder="客户的采购单号(可选)" />
            </el-form-item>
            <el-form-item v-if="fieldVisible.shipToAddress" label="交货地址">
              <el-input v-model="so.shipToAddress" type="textarea" :rows="1" placeholder="收货地址" />
            </el-form-item>
            <el-form-item v-if="fieldVisible.salesOrgName" label="销售部门">
              <el-input v-model="so.salesOrgName" placeholder="所属销售部门" />
            </el-form-item>
            <el-form-item v-if="fieldVisible.companyName" label="公司主体">
              <el-input v-model="so.companyName" disabled />
            </el-form-item>
            <el-form-item v-if="fieldVisible.currencyCode" label="币种" required>
              <el-select v-model="so.currencyCode" @change="onCurrencyChange" style="width:100%">
                <el-option v-for="c in currencies" :key="c.code" :label="`${c.code} ${c.name}`" :value="c.code" />
              </el-select>
            </el-form-item>
            <el-form-item v-if="fieldVisible.exchangeRate" label="汇率">
              <el-input-number v-model="so.exchangeRate" :precision="4" :step="0.01" :min="0" style="width:100%" />
            </el-form-item>
            <el-form-item v-if="fieldVisible.settlementMethod" label="结算方式">
              <el-select v-model="so.settlementMethod" style="width:100%">
                <el-option label="转账" value="转账" />
                <el-option label="现金" value="现金" />
                <el-option label="汇票" value="汇票" />
                <el-option label="支票" value="支票" />
              </el-select>
            </el-form-item>
            <el-form-item v-if="fieldVisible.defaultPriceMode" label="价格模式" required>
              <el-radio-group v-model="so.defaultPriceMode">
                <el-radio value="TaxExclusive">未税</el-radio>
                <el-radio value="TaxInclusive">含税</el-radio>
              </el-radio-group>
            </el-form-item>
            <el-form-item v-if="fieldVisible.defaultTaxRate" label="默认税率">
              <el-select v-model="so.defaultTaxRate" style="width:100%">
                <el-option v-for="t in taxRates" :key="t.code" :label="t.name" :value="t.rate" />
              </el-select>
            </el-form-item>
            <el-form-item v-if="fieldVisible.deliveryMethod" label="交货方式">
              <el-select v-model="so.deliveryMethod" style="width:100%">
                <el-option v-for="m in deliveryMethods" :key="m" :label="m" :value="m" />
              </el-select>
            </el-form-item>
            <el-form-item v-if="fieldVisible.memo" label="备注" class="full-row">
              <el-input v-model="so.memo" type="textarea" :rows="2" placeholder="整单备注" />
            </el-form-item>
          </div>
        </el-collapse-transition>
      </el-form>
    </div>

    <!-- Lines grid section -->
    <div class="edit-section lines-section">
      <div class="section-title">
        <el-icon><Grid /></el-icon>
        <span>订单明细</span>
        <span class="section-sub">{{ so.lines.length }} 行 · 行级价格/税率/折扣独立(DEC-SO-001/002)</span>
        <div class="lines-toolbar">
          <el-button-group size="small">
            <el-button type="primary" :disabled="!editable" @click="addLine">
              <el-icon><Plus /></el-icon>新增行
            </el-button>
            <el-button type="primary" plain :disabled="!editable" @click="openItemMultiLookup">
              <el-icon><Goods /></el-icon>添加商品(多选)
            </el-button>
            <el-button :disabled="!editable" @click="insertLine">
              <el-icon><DocumentAdd /></el-icon>插入行
            </el-button>
            <el-button :disabled="!editable" @click="deleteSelected">
              <el-icon><Delete /></el-icon>删除行
            </el-button>
            <el-button :disabled="!editable" @click="copySelected">
              <el-icon><CopyDocument /></el-icon>复制行
            </el-button>
          </el-button-group>
          <el-divider direction="vertical" />
          <span class="price-mode-hint">行价格模式:</span>
          <el-radio-group v-model="bulkPriceMode" size="small" @change="onBulkPriceModeChange">
            <el-radio-button value="TaxExclusive">未税</el-radio-button>
            <el-radio-button value="TaxInclusive">含税</el-radio-button>
          </el-radio-group>
          <el-tooltip content="切换所有行的价格输入模式;已输入的价格自动按税率换算(DEC-SO-001)" placement="top">
            <el-icon class="info-icon"><InfoFilled /></el-icon>
          </el-tooltip>
          <el-divider direction="vertical" />
          <el-button text size="small" @click="openLineColSettings">
            <el-icon><Grid /></el-icon>列设置
          </el-button>
          <el-divider direction="vertical" />
          <el-button text size="small" :disabled="!editable" @click="applyDefaultWarehouseToAll">
            <el-icon><Box /></el-icon>应用默认仓库到全部明细
          </el-button>
        </div>
      </div>

      <div class="lines-table-wrap" :style="{ '--line-table-total-width': lineTableTotalWidth + 'px' }">
        <el-table
          ref="lineTableRef"
          :data="so.lines"
          border
          stripe
          size="small"
          height="420"
          row-key="lineNo"
          @selection-change="onLineSelectionChange"
          :row-class-name="lineRowClass"
          :header-cell-style="{ background: '#f1f5f9', padding: '0 !important' }"
          :cell-style="{ padding: '4px 4px', fontSize: '12.5px' }"
        >
          <el-table-column type="selection" width="38" fixed="left">
            <template #header>
              <div class="th-two-tier"><div class="th-agg"></div><div class="th-label"></div></div>
            </template>
          </el-table-column>
          <el-table-column label="行号" :width="lineColumns.find(c=>c.prop==='lineNo')?.width || 50" fixed="left" align="center">
            <template #header>
              <div class="th-two-tier"><div class="th-agg"></div><div class="th-label">行号</div></div>
            </template>
            <template #default="{ row }">{{ row.lineNo }}</template>
          </el-table-column>
          <el-table-column label="商品" :width="lineColumns.find(c=>c.prop==='item')?.width || 220" fixed="left">
            <template #header>
              <div class="th-two-tier"><div class="th-agg agg-left agg-bold">合计</div><div class="th-label">商品</div></div>
            </template>
            <template #default="{ row }">
              <div class="cell-item" @click="openItemLookup(row)">
                <div class="item-code">{{ row.itemCode || '请选择' }}</div>
                <div class="item-name">{{ row.itemName }}</div>
              </div>
            </template>
          </el-table-column>
          <template v-for="col in lineColumns.filter(c => c.visible && !['lineNo','item','memo'].includes(c.prop))" :key="col.prop">
            <el-table-column
              :prop="col.prop"
              :label="col.label"
              :width="col.width"
              :fixed="col.fixed"
              :align="['uomName','executedQuantity','openQuantity','amountExclTax','taxAmount','amountInclTax'].includes(col.prop) ? (col.prop === 'uomName' ? 'center' : 'right') : undefined"
              :class-name="['amountExclTax','taxAmount','amountInclTax'].includes(col.prop) ? 'num-col' : undefined"
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
                <component :is="renderLineCell(col, row)" />
              </template>
            </el-table-column>
          </template>
          <el-table-column label="备注" :width="lineColumns.find(c=>c.prop==='memo')?.width || 140" fixed="right">
            <template #header>
              <div class="th-two-tier"><div class="th-agg"></div><div class="th-label">备注</div></div>
            </template>
            <template #default="{ row }">
              <el-input v-model="row.memo" size="small" :disabled="!editable" placeholder="行备注" />
            </template>
          </el-table-column>
        </el-table>
      </div>

    </div>

    <!-- Bottom tabs -->
    <div class="edit-section tabs-section">
      <el-tabs v-model="activeTab" type="border-card">
        <el-tab-pane name="attachments">
          <template #label>
            <el-icon><Paperclip /></el-icon> 附件
            <el-badge v-if="so.attachments.length" :value="so.attachments.length" type="primary" />
          </template>
          <div class="tab-content">
            <div class="upload-area">
              <el-upload action="#" :auto-upload="false" :show-file-list="false" :on-change="onFileAdd">
                <el-button type="primary" plain size="small"><el-icon><Upload /></el-icon>上传附件</el-button>
              </el-upload>
              <span class="upload-hint">支持 PDF / Word / Excel / 图片,单文件最大 50MB(Mock)</span>
            </div>
            <el-table :data="so.attachments" size="small" border>
              <el-table-column prop="name" label="文件名" />
              <el-table-column label="大小" width="100">
                <template #default="{ row }">{{ (row.size / 1024).toFixed(1) }} KB</template>
              </el-table-column>
              <el-table-column prop="uploadedAt" label="上传时间" width="160" />
              <el-table-column prop="uploadedBy" label="上传人" width="90" />
              <el-table-column label="操作" width="140" align="center" class-name="att-actions-col">
                <template #default>
                  <div class="att-actions">
                    <el-tooltip content="预览" placement="top"><el-button text size="small" type="primary"><el-icon><View /></el-icon></el-button></el-tooltip>
                    <el-tooltip content="下载" placement="top"><el-button text size="small"><el-icon><Download /></el-icon></el-button></el-tooltip>
                    <el-tooltip content="删除" placement="top"><el-button text size="small" type="danger"><el-icon><Delete /></el-icon></el-button></el-tooltip>
                  </div>
                </template>
              </el-table-column>
            </el-table>
          </div>
        </el-tab-pane>
        <el-tab-pane name="approval">
          <template #label><el-icon><Checked /></el-icon> 审批记录</template>
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
                  <span class="approver">{{ a.approver }}({{ a.approverRole === 'SalesManager' ? '销售经理' : a.approverRole }})</span>
                  <div v-if="a.comment" class="comment">意见:{{ a.comment }}</div>
                </div>
              </el-timeline-item>
            </el-timeline>
            <el-empty v-else description="暂无审批记录" :image-size="60" />
          </div>
        </el-tab-pane>
        <el-tab-pane name="audit">
          <template #label><el-icon><List /></el-icon> 操作日志</template>
          <div class="tab-content">
            <el-table :data="so.auditLog" size="small" border>
              <el-table-column prop="actionLabel" label="操作" width="120" />
              <el-table-column prop="actor" label="操作人" width="100" />
              <el-table-column prop="actorRole" label="角色" width="100">
                <template #default="{ row }">{{ roleLabel(row.actorRole) }}</template>
              </el-table-column>
              <el-table-column prop="at" label="时间" width="160" />
              <el-table-column prop="reason" label="原因" />
              <el-table-column prop="remark" label="备注" />
            </el-table>
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
                <el-table-column label="操作" width="100">
                  <template #default><el-button text size="small" type="primary">打开</el-button></template>
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
                <el-table-column label="操作" width="100">
                  <template #default><el-button text size="small" type="primary">打开</el-button></template>
                </el-table-column>
                <template #empty><el-empty description="暂无下游单据(审核通过后可生成发货单)" :image-size="50" /></template>
              </el-table>
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

    <!-- Field settings drawer (per user, persisted via localStorage) -->
    <el-drawer v-model="fieldSettingsOpen" title="表头字段设置" size="380px">
      <div class="field-settings">
        <p class="hint-text">勾选需要在"更多信息"区显示的字段。核心录单字段始终显示,不受此设置影响。设置保存在当前用户(吴海),刷新后仍生效。</p>
        <div class="field-list">
          <div v-for="f in moreFields" :key="f.key" class="field-item">
            <el-checkbox v-model="fieldVisible[f.key]">{{ f.label }}</el-checkbox>
          </div>
        </div>
      </div>
      <template #footer>
        <el-button @click="fieldSettingsOpen = false">取消</el-button>
        <el-button @click="resetFieldSettings">恢复默认</el-button>
        <el-button type="primary" @click="saveFieldSettings">保存</el-button>
      </template>
    </el-drawer>

    <!-- Line column settings drawer (per user, persisted via localStorage) -->
    <el-drawer v-model="lineColDrawer" title="明细列设置 · 可拖拽排序" size="400px">
      <div class="gs-col-settings">
        <p class="gs-field-hint">核心列(商品/规格/单位/数量/未税单价/含税单价/未税金额/税额/价税合计)始终显示且不可隐藏。非核心列可勾选开关。拖拽左侧手柄调整顺序,输入框调整列宽(像素)。设置保存在 localStorage,刷新后仍生效。</p>
        <div class="gs-col-list">
          <div
            v-for="(col, idx) in lineColumns"
            :key="col.prop"
            class="gs-col-item"
            draggable="true"
            @dragstart="onLineDragStart(idx)"
            @dragover.prevent
            @drop="onLineDrop(idx)"
          >
            <el-icon class="gs-col-handle"><Rank /></el-icon>
            <el-checkbox v-model="col.visible" :disabled="col.core">{{ col.label }}</el-checkbox>
            <el-tag v-if="col.core" size="small" type="warning" effect="light" style="margin-left:6px">核心</el-tag>
            <el-input-number
              v-model="col.width"
              size="small"
              :min="50"
              :step="10"
              style="width:110px;margin-left:auto"
              controls-position="right"
            />
          </div>
        </div>
      </div>
      <template #footer>
        <el-button @click="lineColDrawer = false">取消</el-button>
        <el-button @click="resetLineColumns">恢复默认</el-button>
        <el-button type="primary" @click="saveLineColumns">保存</el-button>
      </template>
    </el-drawer>

    <!-- Lookup popwin (single) -->
    <LookupDialog
      v-model="lookupOpen"
      :entity-type="lookupEntity"
      :filter="lookupFilter"
      :multi-select="false"
      @confirm="onLookupConfirm"
    />

    <!-- Item multi-select popwin -->
    <LookupDialog
      v-model="itemMultiOpen"
      entity-type="item"
      :multi-select="true"
      :exclude-ids="existingItemIds"
      @multi-confirm="onItemMultiConfirm"
    />
  </div>
  <el-empty v-else description="未找到该销售订单" />
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch, onMounted, onBeforeUnmount, h } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, ElMessageBox, ElInputNumber, ElInput, ElSelect, ElOption, ElDatePicker, ElIcon, ElTag, ElButton, ElTooltip } from 'element-plus';
import {
  ArrowLeft, Check, Promotion, Select, CloseBold, Back, CircleClose, Lock, Printer,
  CopyDocument, Van, Search, Plus, DocumentAdd, Delete, Document, Grid, InfoFilled,
  Paperclip, Upload, Checked, List, Link, WarningFilled, View, Download, Setting,
  ArrowUp, ArrowDown, Goods, Rank, Box, RefreshRight
} from '@element-plus/icons-vue';
import { useTabsStore } from '../../stores/tabs';
import { useSalesOrderStore } from '../../stores/sales-order';
import {
  items, employees, contacts, paymentTerms, currencies, taxRates,
  deliveryMethods, makeBlankLine, recomputeLine, recomputeHeader, nextSalesOrderNo,
  defaultLineColumns, warehouses, locations, uoms
} from '../../mock/sales-order';
import type { SalesOrder, SalesOrderLine, PriceMode, LineColumnConfig } from '../../types/sales-order';
import {
  documentStatusMap, approvalStatusMap, executionStatusMap,
  fmtMoney, computeActions
} from '../../utils/status';
import LookupDialog, { type LookupEntity } from '../../components/LookupDialog.vue';

defineOptions({ name: 'SalesOrderEdit' });

const route = useRoute();
const router = useRouter();
const tabs = useTabsStore();
const soStore = useSalesOrderStore();

const so = ref<SalesOrder | null>(null);
const lineTableRef = ref();
const activeTab = ref('attachments');
const bulkPriceMode = ref<PriceMode>('TaxExclusive');
const dirty = ref<boolean>(false);
const showMoreFields = ref<boolean>(false);

const selectedLines = ref<SalesOrderLine[]>([]);
const lookupOpen = ref(false);
const lookupEntity = ref<LookupEntity>('customer');
const lookupFilter = ref<Record<string, any>>({});
let activeLine: SalesOrderLine | null = null;
let lookupContext: 'header' | 'line' = 'header';

// Item multi-select
const itemMultiOpen = ref(false);

const reasonDialog = reactive({
  visible: false,
  title: '',
  placeholder: '请输入原因',
  okType: 'danger' as 'danger' | 'primary' | 'warning',
  action: '' as 'reject' | 'cancel' | 'close' | 'withdraw',
  reason: ''
});

// ===== Field settings (per user, persisted) =====
const LS_FIELD_SETTINGS = 'erp.so.edit.fieldSettings';
const LS_SHOW_MORE = 'erp.so.edit.showMore';

const moreFields = [
  { key: 'contactName', label: '联系人' },
  { key: 'customerOrderNo', label: '客户订单号' },
  { key: 'shipToAddress', label: '交货地址' },
  { key: 'salesOrgName', label: '销售部门' },
  { key: 'companyName', label: '公司主体' },
  { key: 'currencyCode', label: '币种' },
  { key: 'exchangeRate', label: '汇率' },
  { key: 'settlementMethod', label: '结算方式' },
  { key: 'defaultPriceMode', label: '价格模式' },
  { key: 'defaultTaxRate', label: '默认税率' },
  { key: 'deliveryMethod', label: '交货方式' },
  { key: 'memo', label: '备注' }
];

const DEFAULT_FIELD_VISIBLE: Record<string, boolean> = {
  contactName: true,
  customerOrderNo: true,
  shipToAddress: true,
  salesOrgName: true,
  companyName: false,
  currencyCode: false,
  exchangeRate: false,
  settlementMethod: false,
  defaultPriceMode: true,
  defaultTaxRate: true,
  deliveryMethod: false,
  memo: true
};

function loadFieldSettings(): Record<string, boolean> {
  try {
    const raw = localStorage.getItem(LS_FIELD_SETTINGS);
    if (raw) {
      const parsed = JSON.parse(raw);
      return { ...DEFAULT_FIELD_VISIBLE, ...parsed };
    }
  } catch (e) { /* ignore */ }
  return { ...DEFAULT_FIELD_VISIBLE };
}

const fieldVisible = ref<Record<string, boolean>>(loadFieldSettings());
const fieldSettingsOpen = ref(false);

function openFieldSettings() {
  fieldSettingsOpen.value = true;
}

function saveFieldSettings() {
  localStorage.setItem(LS_FIELD_SETTINGS, JSON.stringify(fieldVisible.value));
  fieldSettingsOpen.value = false;
  ElMessage.success('字段设置已保存(本用户:吴海,刷新后仍生效)');
}

function resetFieldSettings() {
  fieldVisible.value = { ...DEFAULT_FIELD_VISIBLE };
  ElMessage.info('已恢复默认字段设置');
}

function loadShowMore(): boolean {
  return localStorage.getItem(LS_SHOW_MORE) === '1';
}

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
    // Follow saved order; use default's structural metadata
    const result: LineColumnConfig[] = [];
    const used = new Set<string>();
    for (const s of saved) {
      const dc = defaultMap.get(s.prop);
      if (!dc) continue; // prop no longer in defaults — drop
      result.push({
        ...dc,
        visible: s.visible !== undefined ? s.visible : dc.visible,
        width: s.width || dc.width
      });
      used.add(s.prop);
    }
    // Append any default columns not present in saved (new columns added after user's save)
    for (const dc of defaults) {
      if (!used.has(dc.prop)) result.push({ ...dc });
    }
    return result;
  } catch (e) { /* ignore */ }
  return defaults;
}
const lineColumns = ref<LineColumnConfig[]>(loadLineColumns());
const lineColDrawer = ref(false);
let lineDragIdx = -1;
function onLineDragStart(idx: number) { lineDragIdx = idx; }
function onLineDrop(idx: number) {
  if (lineDragIdx < 0 || lineDragIdx === idx) return;
  const arr = lineColumns.value;
  const moved = arr[lineDragIdx];
  arr.splice(lineDragIdx, 1);
  arr.splice(idx, 0, moved);
  lineDragIdx = -1;
}
function saveLineColumns() {
  localStorage.setItem(LS_LINE_COLS, JSON.stringify(lineColumns.value));
  lineColDrawer.value = false;
  ElMessage.success('明细列设置已保存(本用户:吴海,刷新后仍生效)');
}
function resetLineColumns() {
  lineColumns.value = JSON.parse(JSON.stringify(defaultLineColumns));
  ElMessage.info('已恢复默认明细列设置');
}
function openLineColSettings() {
  lineColDrawer.value = true;
}

const lineTableTotalWidth = computed(() => {
  return 38 + lineColumns.value.reduce((s, c) => s + c.width, 0);
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

// ===== G1B-1R8: Two-Tier Aggregate Header — driven by same lineColumns =====
// Aggregate value renders as Tier 1 inside each column's #header slot.
// No footer reorder hack; no dependency on el-table__footer-wrapper DOM order.
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

// ===== G1B-1R7: Apply default warehouse to all lines → all become 'inherited' =====
function applyDefaultWarehouseToAll() {
  if (!so.value) return;
  if (!so.value.defaultWarehouseId) {
    ElMessage.warning('请先在表头选择默认仓库');
    return;
  }
  ElMessageBox.confirm(
    `将把全部明细仓库改为"${so.value.defaultWarehouseName}"，并清除不兼容的库位，是否继续？`,
    '应用默认仓库到全部明细',
    { type: 'warning' }
  ).then(() => {
    let changed = 0;
    for (const line of so.value!.lines) {
      if (line.warehouseId !== so.value!.defaultWarehouseId || line.warehouseSource !== 'inherited') {
        line.warehouseId = so.value!.defaultWarehouseId;
        line.warehouseName = so.value!.defaultWarehouseName;
        line.warehouseSource = 'inherited';
        // Clear incompatible location
        if (line.locationId) {
          const loc = locations.find(l => l.id === line.locationId);
          if (!loc || loc.warehouseId !== line.warehouseId) {
            line.locationId = '';
            line.locationName = '';
          }
        }
        changed++;
      }
    }
    if (changed > 0) markDirty();
    ElMessage.success(`已应用默认仓库到全部明细(${changed} 行更新)`);
  }).catch(() => {});
}

// ===== G1B-1R8: Override → Inherited reverse path =====
// Explicit "跟随表头仓库" action: converts a user-overridden line back to inherited.
// Only this explicit action restores inherited state — selecting the same warehouse via Lookup is still 'override'.
function followHeaderWarehouse(line: SalesOrderLine) {
  if (!so.value) return;
  const headerWhId = so.value.defaultWarehouseId || '';
  const headerWhName = so.value.defaultWarehouseName || '';
  line.warehouseSource = 'inherited';
  line.warehouseId = headerWhId;
  line.warehouseName = headerWhName;
  // Location cascade: clear if incompatible with new warehouse (or header empty)
  if (line.locationId) {
    const loc = locations.find(l => l.id === line.locationId);
    if (!headerWhId || !loc || loc.warehouseId !== headerWhId) {
      line.locationId = '';
      line.locationName = '';
    }
  }
  markDirty();
  ElMessage.success(headerWhId ? `已跟随表头仓库:${headerWhName}` : '已跟随表头仓库(当前表头无默认仓库,已清空)');
}

function renderLineCell(col: LineColumnConfig, row: SalesOrderLine) {
  const disabled = !editable.value;
  switch (col.prop) {
    case 'itemSpec':
      return h(ElInput, {
        modelValue: row.itemSpec,
        'onUpdate:modelValue': (v: string | undefined) => { row.itemSpec = v ?? ''; markDirty(); },
        disabled,
        size: 'small'
      });
    case 'uomName':
      return h('span', { style: 'text-align:center;display:block;width:100%' }, row.uomName);
    case 'quantity':
      return h(ElInputNumber, {
        modelValue: row.quantity,
        'onUpdate:modelValue': (v: number | undefined) => { row.quantity = v ?? 0; onLineQtyChange(row); },
        min: 0,
        precision: 4,
        step: 1,
        size: 'small',
        controlsPosition: 'right',
        style: 'width:100%',
        disabled,
        onChange: () => onLineQtyChange(row)
      });
    case 'linePriceMode':
      return h(ElSelect, {
        modelValue: row.linePriceMode,
        'onUpdate:modelValue': (v: PriceMode | undefined) => { if (v) { row.linePriceMode = v; onLinePriceModeChange(row); } },
        size: 'small',
        disabled,
        style: 'width:100%',
        onChange: () => onLinePriceModeChange(row)
      }, {
        default: () => [
          h(ElOption, { label: '未税', value: 'TaxExclusive' }),
          h(ElOption, { label: '含税', value: 'TaxInclusive' })
        ]
      });
    case 'unitPriceExclTax':
      return h(ElInputNumber, {
        modelValue: row.unitPriceExclTax,
        'onUpdate:modelValue': (v: number | undefined) => { row.unitPriceExclTax = v ?? 0; onPriceExclChange(row); },
        min: 0,
        precision: 4,
        step: 1,
        size: 'small',
        controlsPosition: 'right',
        style: 'width:100%',
        disabled: disabled || row.linePriceMode === 'TaxInclusive',
        onChange: () => onPriceExclChange(row)
      });
    case 'unitPriceInclTax':
      return h(ElInputNumber, {
        modelValue: row.unitPriceInclTax,
        'onUpdate:modelValue': (v: number | undefined) => { row.unitPriceInclTax = v ?? 0; onPriceInclChange(row); },
        min: 0,
        precision: 2,
        step: 1,
        size: 'small',
        controlsPosition: 'right',
        style: 'width:100%',
        disabled: disabled || row.linePriceMode === 'TaxExclusive',
        onChange: () => onPriceInclChange(row)
      });
    case 'lineTaxRate':
      return h(ElSelect, {
        modelValue: row.lineTaxRate,
        'onUpdate:modelValue': (v: number | undefined) => { if (v !== undefined) { row.lineTaxRate = v; onTaxRateChange(row); } },
        size: 'small',
        disabled,
        style: 'width:100%',
        onChange: () => onTaxRateChange(row)
      }, {
        default: () => taxRates.map(t => h(ElOption, { key: t.code, label: t.name, value: t.rate }))
      });
    case 'discountRate':
      return h(ElInputNumber, {
        modelValue: row.discountRate,
        'onUpdate:modelValue': (v: number | undefined) => { row.discountRate = v ?? 0; onDiscountRateChange(row); },
        min: 0,
        max: 0.9999,
        step: 0.01,
        precision: 4,
        size: 'small',
        controlsPosition: 'right',
        style: 'width:100%',
        disabled: disabled || row.discountAmount > 0,
        onChange: () => onDiscountRateChange(row)
      });
    case 'discountAmount':
      return h(ElInputNumber, {
        modelValue: row.discountAmount,
        'onUpdate:modelValue': (v: number | undefined) => { row.discountAmount = v ?? 0; onDiscountAmountChange(row); },
        min: 0,
        precision: 2,
        step: 1,
        size: 'small',
        controlsPosition: 'right',
        style: 'width:100%',
        disabled: disabled || row.discountRate > 0,
        onChange: () => onDiscountAmountChange(row)
      });
    case 'amountExclTax':
      return h('span', { class: 'num-cell', style: 'text-align:right;display:block' }, fmtMoney(row.amountExclTax, 4));
    case 'taxAmount':
      return h('span', { class: 'num-cell', style: 'text-align:right;display:block' }, fmtMoney(row.taxAmount));
    case 'amountInclTax':
      return h('span', { class: 'num-cell strong', style: 'text-align:right;display:block;color:#c2410c;font-weight:700' }, fmtMoney(row.amountInclTax));
    case 'warehouseName':
      return h(ElInput, {
        modelValue: row.warehouseName,
        readonly: true,
        size: 'small',
        placeholder: '选择仓库',
        style: 'cursor: pointer',
        onClick: () => { if (!disabled) openLineLookup('warehouse', row); }
      }, {
        append: () => h('div', { class: 'wh-append-grp' }, [
          h(ElButton, {
            icon: Search,
            disabled,
            size: 'small',
            onClick: () => openLineLookup('warehouse', row)
          }),
          // G1B-1R8: "跟随表头仓库" button — only visible when this line is override
          ...(row.warehouseSource === 'override' && !disabled ? [
            h(ElTooltip as any, { content: '已单独指定,点击跟随表头仓库', placement: 'top' }, {
              default: () => h(ElButton, {
                icon: RefreshRight,
                size: 'small',
                onClick: () => followHeaderWarehouse(row)
              })
            })
          ] : [])
        ])
      });
    case 'locationName':
      return h(ElInput, {
        modelValue: row.locationName,
        readonly: true,
        size: 'small',
        placeholder: row.warehouseId ? '选择库位' : '请先选择仓库',
        style: row.warehouseId ? 'cursor: pointer' : '',
        onClick: () => {
          if (disabled) return;
          if (!row.warehouseId) { ElMessage.warning('请先选择仓库'); return; }
          openLineLookup('location', row, { warehouseId: row.warehouseId });
        }
      }, {
        append: () => h(ElButton, {
          icon: Search,
          disabled: disabled || !row.warehouseId,
          size: 'small',
          onClick: () => {
            if (!row.warehouseId) { ElMessage.warning('请先选择仓库'); return; }
            openLineLookup('location', row, { warehouseId: row.warehouseId });
          }
        })
      });
    case 'lineDeliveryDate':
      return h(ElDatePicker, {
        modelValue: row.lineDeliveryDate,
        'onUpdate:modelValue': (v: string | undefined) => { row.lineDeliveryDate = v ?? ''; markDirty(); },
        type: 'date',
        valueFormat: 'YYYY-MM-DD',
        size: 'small',
        disabled,
        style: 'width:100%'
      });
    case 'executedQuantity':
      return h('span', {
        class: ['num-cell', row.executedQuantity > 0 && row.executedQuantity < row.quantity ? 'partial' : '', row.executedQuantity >= row.quantity ? 'done' : ''],
        style: 'text-align:right;display:block'
      }, fmtMoney(row.executedQuantity, 0));
    case 'openQuantity':
      return h('span', {
        class: ['num-cell', row.openQuantity > 0 ? 'open' : ''],
        style: 'text-align:right;display:block'
      }, fmtMoney(row.openQuantity, 0));
    default:
      return h('span', String((row as any)[col.prop] ?? ''));
  }
}

watch(showMoreFields, v => localStorage.setItem(LS_SHOW_MORE, v ? '1' : '0'));

const editable = computed(() => so.value?.documentStatus === 'Draft');
const actions = computed(() => so.value ? computeActions(so.value) : null as any);

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

// Existing item IDs (to exclude from multi-select)
const existingItemIds = computed(() => so.value?.lines.filter(l => l.itemId).map(l => l.itemId) || []);

function loadOrder(id: string) {
  if (id === 'new') {
    const today = new Date().toISOString().slice(0, 10);
    const newSo: SalesOrder = {
      id: 'new',
      salesOrderNo: '(保存时自动生成)',
      orderDate: today,
      requestedDeliveryDate: today,
      customerId: '', customerCode: '', customerName: '',
      salesPersonId: 'E001', salesPersonName: '张磊', salesOrgId: 'O001', salesOrgName: '华东销售部',
      companyId: 'CMP001', companyName: '谷粒 ERP 有限公司',
      currencyCode: 'CNY', exchangeRate: 1,
      defaultPriceMode: 'TaxExclusive', defaultTaxRate: 0.13,
      paymentTermCode: 'M30', settlementMethod: '转账',
      deliveryMethod: '送货上门',
      documentType: 'Normal',
      documentStatus: 'Draft', approvalStatus: 'NotSubmitted', executionStatus: 'NotStarted',
      createdBy: '吴海', createdAt: new Date().toISOString().replace('T', ' ').slice(0, 19),
      updatedAt: new Date().toISOString().replace('T', ' ').slice(0, 19),
      concurrencyVersion: 1,
      lines: [],
      attachments: [], approvalHistory: [], auditLog: [], downstream: [],
      totalQuantity: 0, totalAmountExclTax: 0, totalTaxAmount: 0, totalAmountInclTax: 0, totalDiscountAmount: 0
    };
    so.value = newSo;
    addLine();
  } else {
    const found = soStore.getById(id);
    if (found) {
      so.value = JSON.parse(JSON.stringify(found));
    }
  }
  bulkPriceMode.value = so.value?.defaultPriceMode || 'TaxExclusive';
  dirty.value = false;
}

onMounted(() => {
  showMoreFields.value = loadShowMore();
  loadOrder(route.params.id as string);
  window.addEventListener('keydown', onGlobalKeydown);
});
onBeforeUnmount(() => {
  window.removeEventListener('keydown', onGlobalKeydown);
});

watch(() => route.params.id, (id) => { if (id) loadOrder(id as string); });

// ===== G1B-1R8: Header DefaultWarehouse change → propagate to 'inherited' lines only =====
// Includes clearing: when header warehouse is cleared (newWhId=''), inherited lines' warehouse + location are cleared.
// Lines with warehouseSource === 'override' are NOT touched (user explicitly set them).
watch(() => so.value?.defaultWarehouseId, (newWhId, oldWhId) => {
  if (!so.value || !editable.value) return;
  if (newWhId === oldWhId) return; // only skip if truly unchanged (allow empty string)
  const wh = newWhId ? warehouses.find(w => w.id === newWhId) : undefined;
  let changed = 0;
  for (const line of so.value.lines) {
    if (line.warehouseSource === 'inherited' || !line.warehouseSource) {
      line.warehouseId = newWhId || '';
      line.warehouseName = wh?.name || '';
      // Clear incompatible location (warehouse changed or cleared → location must be re-evaluated)
      if (line.locationId) {
        const loc = locations.find(l => l.id === line.locationId);
        if (!newWhId || !loc || loc.warehouseId !== newWhId) {
          line.locationId = '';
          line.locationName = '';
        }
      }
      changed++;
    }
  }
  if (changed > 0) markDirty();
});

// ===== Ctrl+S shortcut =====
function onGlobalKeydown(e: KeyboardEvent) {
  const isCtrlS = (e.ctrlKey || e.metaKey) && e.key === 's';
  if (isCtrlS && editable.value && so.value) {
    e.preventDefault();
    onSaveDraft();
  }
}

// ===== Actions =====
function goBack() {
  tabs.setActive('list-sales-order');
  router.push('/sales-order').catch(() => {});
}

function markDirty() {
  dirty.value = true;
  if (so.value) {
    const tab = tabs.tabs.find(t => t.id === `edit-${so.value!.id}`);
    if (tab) tabs.markDirty(tab.id, true);
  }
}

function markDirtyOff() {
  dirty.value = false;
  if (so.value) {
    const tab = tabs.tabs.find(t => t.id === `edit-${so.value!.id}`);
    if (tab) tabs.markDirty(tab.id, false);
  }
}

function onSaveDraft() {
  if (!so.value) return;
  // Allow saving draft without full validation (only critical fields)
  if (!so.value.orderDate) { ElMessage.warning('请选择订单日期'); return; }
  if (!so.value.customerId) { ElMessage.warning('请选择客户'); return; }
  if (!so.value.salesPersonId) { ElMessage.warning('请选择销售员'); return; }
  // Location mandatory validation (FIX 4): locationMandatory warehouse requires location
  for (const l of so.value.lines) {
    if (l.warehouseId) {
      const wh = warehouses.find(w => w.id === l.warehouseId);
      if (wh?.locationMandatory && !l.locationId) {
        ElMessage.warning(`第 ${l.lineNo} 行:${wh.name}要求选择库位`);
        return;
      }
    }
  }

  if (so.value.id === 'new') {
    so.value.id = `tmp-${Date.now()}`;
    so.value.salesOrderNo = nextSalesOrderNo(so.value.orderDate);
  }
  soStore.upsert(JSON.parse(JSON.stringify(so.value)));
  ElMessage.success(`草稿已保存:${so.value.salesOrderNo}`);
  markDirtyOff();
}

function onSave() { onSaveDraft(); }

function validateForSave(): boolean {
  if (!so.value!.customerId) { ElMessage.warning('请选择客户'); return false; }
  if (!so.value!.salesPersonId) { ElMessage.warning('请选择销售员'); return false; }
  if (!so.value!.orderDate) { ElMessage.warning('请选择订单日期'); return false; }
  if (so.value!.lines.length === 0) { ElMessage.warning('请至少添加一行明细'); return false; }
  for (const l of so.value!.lines) {
    if (!l.itemId) { ElMessage.warning(`第 ${l.lineNo} 行:请选择商品`); return false; }
    if (!l.quantity || l.quantity <= 0) { ElMessage.warning(`第 ${l.lineNo} 行:数量必须大于 0`); return false; }
    if (l.linePriceMode === 'TaxExclusive' && (!l.unitPriceExclTax || l.unitPriceExclTax < 0)) {
      ElMessage.warning(`第 ${l.lineNo} 行:未税单价必须大于等于 0`); return false;
    }
    if (l.linePriceMode === 'TaxInclusive' && (!l.unitPriceInclTax || l.unitPriceInclTax < 0)) {
      ElMessage.warning(`第 ${l.lineNo} 行:含税单价必须大于等于 0`); return false;
    }
  }
  return true;
}

function onSubmit() {
  if (!validateForSave()) return;
  if (so.value!.id === 'new') {
    so.value!.id = `tmp-${Date.now()}`;
    so.value!.salesOrderNo = nextSalesOrderNo(so.value!.orderDate);
  }
  soStore.upsert(JSON.parse(JSON.stringify(so.value!)));
  soStore.applyAction(so.value!.id, 'submit');
  const updated = soStore.getById(so.value!.id);
  if (updated) {
    so.value = JSON.parse(JSON.stringify(updated));
  }
  ElMessage.success(`已提交审批:${so.value!.salesOrderNo}`);
  markDirtyOff();
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
  const newNo = nextSalesOrderNo(so.value.orderDate);
  const cloned = soStore.clone(so.value.id, newNo);
  if (cloned) {
    ElMessage.success(`已复制为 ${newNo}`);
    tabs.openEdit(cloned.id, newNo, 'edit');
    router.push(`/sales-order/${cloned.id}/edit`).catch(() => {});
  }
}
function onGenerateShipment() { ElMessage.info('将跳转发货单创建页(Mock)'); }

// ===== Line operations =====
function addLine() {
  if (!so.value) return;
  const lineNo = so.value.lines.length + 1;
  const line = makeBlankLine(lineNo, {
    warehouseId: so.value.defaultWarehouseId,
    warehouseName: so.value.defaultWarehouseName,
    deliveryDate: so.value.requestedDeliveryDate,
    taxRate: so.value.defaultTaxRate,
    priceMode: so.value.defaultPriceMode
  });
  so.value.lines.push(line);
  markDirty();
}

function insertLine() {
  if (!so.value) return;
  const at = selectedLines.value[0]?.lineNo || so.value.lines.length;
  const lineNo = at;
  const line = makeBlankLine(lineNo, {
    warehouseId: so.value.defaultWarehouseId,
    warehouseName: so.value.defaultWarehouseName,
    deliveryDate: so.value.requestedDeliveryDate,
    taxRate: so.value.defaultTaxRate,
    priceMode: so.value.defaultPriceMode
  });
  so.value.lines.splice(lineNo - 1, 0, line);
  renumberLines();
  markDirty();
}

function deleteSelected() {
  if (!so.value) return;
  if (selectedLines.value.length === 0) {
    ElMessage.warning('请先选中要删除的行');
    return;
  }
  ElMessageBox.confirm(`确认删除选中的 ${selectedLines.value.length} 行?`, '提示', { type: 'warning' })
    .then(() => {
      const ids = new Set(selectedLines.value.map(l => l.lineNo));
      so.value!.lines = so.value!.lines.filter(l => !ids.has(l.lineNo));
      renumberLines();
      markDirty();
      ElMessage.success('已删除');
    })
    .catch(() => {});
}

function copySelected() {
  if (!so.value) return;
  if (selectedLines.value.length === 0) {
    ElMessage.warning('请先选中要复制的行');
    return;
  }
  const newLines: SalesOrderLine[] = [];
  for (const src of selectedLines.value) {
    const copy: SalesOrderLine = JSON.parse(JSON.stringify(src));
    copy.lineNo = so.value!.lines.length + 1 + newLines.length;
    newLines.push(copy);
  }
  so.value.lines.push(...newLines);
  renumberLines();
  markDirty();
  ElMessage.success(`已复制 ${selectedLines.value.length} 行`);
}

function renumberLines() {
  so.value!.lines.forEach((l, i) => { l.lineNo = i + 1; });
}

function onLineSelectionChange(rows: SalesOrderLine[]) {
  selectedLines.value = rows;
}

function lineRowClass({ row }: { row: SalesOrderLine }) {
  if (row.executedQuantity >= row.quantity && row.quantity > 0) return 'line-done';
  if (row.executedQuantity > 0) return 'line-partial';
  return '';
}

// ===== Pricing engine callbacks (DEC-SO-001, DEC-SO-002) =====
function onPriceExclChange(line: SalesOrderLine) {
  recomputeLine(line, 'excl');
  recomputeHeader(so.value!);
  markDirty();
}
function onPriceInclChange(line: SalesOrderLine) {
  recomputeLine(line, 'incl');
  recomputeHeader(so.value!);
  markDirty();
}
function onTaxRateChange(line: SalesOrderLine) {
  recomputeLine(line, line.linePriceMode === 'TaxInclusive' ? 'incl' : 'excl');
  recomputeHeader(so.value!);
  markDirty();
}
function onLinePriceModeChange(line: SalesOrderLine) {
  recomputeLine(line, line.linePriceMode === 'TaxInclusive' ? 'incl' : 'excl');
  recomputeHeader(so.value!);
  markDirty();
}
function onLineQtyChange(line: SalesOrderLine) {
  recomputeLine(line, 'none');
  recomputeHeader(so.value!);
  markDirty();
}
function onDiscountRateChange(line: SalesOrderLine) {
  if (line.discountRate > 0) line.discountAmount = 0;
  recomputeLine(line, 'none');
  recomputeHeader(so.value!);
  markDirty();
}
function onDiscountAmountChange(line: SalesOrderLine) {
  if (line.discountAmount > 0) line.discountRate = 0;
  recomputeLine(line, 'none');
  recomputeHeader(so.value!);
  markDirty();
}

function onBulkPriceModeChange(mode: any) {
  if (!so.value) return;
  for (const line of so.value.lines) {
    line.linePriceMode = mode as PriceMode;
    recomputeLine(line, mode === 'TaxInclusive' ? 'incl' : 'excl');
  }
  recomputeHeader(so.value);
  markDirty();
  ElMessage.success(`已切换所有行为${mode === 'TaxInclusive' ? '含税' : '未税'}价格模式`);
}

function onCurrencyChange(code: string) {
  if (!so.value) return;
  const c = currencies.find(x => x.code === code);
  if (c) {
    so.value.exchangeRate = c.rate;
    ElMessage.info(`币种已切换为 ${c.name},汇率 ${c.rate}`);
  }
}

// ===== Lookups =====
function openLookup(entity: LookupEntity, filter: Record<string, any> = {}) {
  lookupContext = 'header';
  lookupEntity.value = entity;
  lookupFilter.value = filter;
  lookupOpen.value = true;
}

function openLineLookup(entity: LookupEntity, line: SalesOrderLine, filter: Record<string, any> = {}) {
  lookupContext = 'line';
  activeLine = line;
  lookupEntity.value = entity;
  lookupFilter.value = filter;
  lookupOpen.value = true;
}

function openItemLookup(line: SalesOrderLine) {
  if (!editable.value) return;
  // Open single-select item lookup for editing one line
  openLineLookup('item', line);
}

function openItemMultiLookup() {
  if (!editable.value) return;
  itemMultiOpen.value = true;
}

function onLookupConfirm(payload: { id: string; label: string; raw: any }) {
  if (lookupContext === 'header') {
    if (!so.value) return;
    switch (lookupEntity.value) {
      case 'customer': {
        const c = payload.raw;
        so.value.customerId = c.id;
        so.value.customerCode = c.code;
        so.value.customerName = c.name;
        so.value.contactName = c.contactName;
        so.value.contactId = contacts.find(ct => ct.customerId === c.id)?.id;
        so.value.defaultPriceMode = c.defaultPriceMode;
        so.value.defaultTaxRate = c.defaultTaxRate;
        so.value.paymentTermCode = c.defaultPaymentTermCode;
        so.value.currencyCode = c.defaultCurrencyCode;
        so.value.shipToAddress = c.shipToAddress;
        so.value.salesPersonId = c.salesPersonId || so.value.salesPersonId;
        const sp = employees.find(e => e.id === so.value!.salesPersonId);
        if (sp) { so.value.salesPersonName = sp.name; so.value.salesOrgName = sp.orgName; }
        bulkPriceMode.value = so.value.defaultPriceMode;
        ElMessage.success(`已选择客户:${c.name}`);
        // Make sure more fields visible so user sees the auto-filled values
        showMoreFields.value = true;
        break;
      }
      case 'contact':
        so.value.contactId = payload.raw.id;
        so.value.contactName = payload.raw.name;
        break;
      case 'employee':
        so.value.salesPersonId = payload.raw.id;
        so.value.salesPersonName = payload.raw.name;
        so.value.salesOrgId = payload.raw.orgId;
        so.value.salesOrgName = payload.raw.orgName;
        break;
      case 'warehouse':
        so.value.defaultWarehouseId = payload.raw.id;
        so.value.defaultWarehouseName = payload.raw.name;
        break;
    }
  } else if (lookupContext === 'line' && activeLine) {
    const line = activeLine;
    switch (lookupEntity.value) {
      case 'item': {
        const it = payload.raw;
        line.itemId = it.id;
        line.itemCode = it.code;
        line.itemName = it.name;
        line.itemSpec = it.spec;
        line.uomId = it.uomId;
        line.uomName = it.uomName;
        line.unitPriceExclTax = it.defaultSalesPriceExclTax;
        line.unitPriceInclTax = it.defaultSalesPriceInclTax;
        line.lineTaxRate = it.defaultTaxRate;
        line.lotRequired = it.lotEnabled;
        recomputeLine(line, line.linePriceMode === 'TaxInclusive' ? 'incl' : 'excl');
        recomputeHeader(so.value!);
        ElMessage.success(`已添加商品:${it.name}`);
        break;
      }
      case 'warehouse':
        line.warehouseId = payload.raw.id;
        line.warehouseName = payload.raw.name;
        line.warehouseSource = 'override'; // G1B-1R7: user manually set this line's warehouse
        // Clear incompatible location (warehouse changed → old location may not belong to new warehouse)
        if (line.locationId) {
          const loc = locations.find(l => l.id === line.locationId);
          if (!loc || loc.warehouseId !== line.warehouseId) {
            line.locationId = '';
            line.locationName = '';
          }
        }
        break;
      case 'location':
        line.locationId = payload.raw.id;
        line.locationName = payload.raw.name;
        break;
    }
    activeLine = null;
  }
  markDirty();
}

// ===== Item multi-select confirm: generate N lines =====
function onItemMultiConfirm(payload: { rows: any[] }) {
  if (!so.value) return;
  let added = 0;
  for (const it of payload.rows) {
    const lineNo = so.value.lines.length + 1;
    const line = makeBlankLine(lineNo, {
      warehouseId: so.value.defaultWarehouseId,
      warehouseName: so.value.defaultWarehouseName,
      deliveryDate: so.value.requestedDeliveryDate,
      taxRate: it.defaultTaxRate,
      priceMode: so.value.defaultPriceMode
    });
    line.itemId = it.id;
    line.itemCode = it.code;
    line.itemName = it.name;
    line.itemSpec = it.spec;
    line.uomId = it.uomId;
    line.uomName = it.uomName;
    line.unitPriceExclTax = it.defaultSalesPriceExclTax;
    line.unitPriceInclTax = it.defaultSalesPriceInclTax;
    line.lineTaxRate = it.defaultTaxRate;
    line.lotRequired = it.lotEnabled;
    recomputeLine(line, line.linePriceMode === 'TaxInclusive' ? 'incl' : 'excl');
    so.value.lines.push(line);
    added++;
  }
  recomputeHeader(so.value);
  markDirty();
  ElMessage.success(`已加入 ${added} 行商品`);
}

// ===== Attachments =====
function onFileAdd(file: any) {
  if (!so.value) return;
  so.value.attachments.push({
    id: `F-${Date.now()}`,
    name: file.name,
    size: file.size,
    uploadedAt: new Date().toISOString().replace('T', ' ').slice(0, 19),
    uploadedBy: '吴海'
  });
  ElMessage.success(`已上传:${file.name}(Mock)`);
  markDirty();
}

// ===== Misc helpers =====
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
.so-edit {
  display: flex;
  flex-direction: column;
  min-height: 100%;
  background: #f6f7fb;
  /* NO overflow:hidden — let parent .erp-content scroll vertically */
}
.edit-headerbar {
  background: #fff;
  border-bottom: 1px solid #e5e7eb;
  padding: 8px 16px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
  position: sticky;
  top: 0;
  z-index: 10;
  box-shadow: 0 2px 4px rgba(0,0,0,0.04);
}
.headerbar-left {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}
.doc-no {
  font-size: 16px;
  font-weight: 700;
  color: #1f2937;
}
.status-tags {
  display: flex;
  gap: 4px;
}
.version-tag {
  background: #f3f4f6;
  color: #6b7280;
  padding: 2px 8px;
  border-radius: 8px;
  font-size: 11px;
  font-family: monospace;
}
.dirty-badge {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  background: #fef3c7;
  color: #92400e;
  padding: 2px 8px;
  border-radius: 8px;
  font-size: 11px;
  font-weight: 600;
}
.headerbar-right {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-wrap: wrap;
}
.kbd-hint {
  margin-left: 6px;
  background: rgba(255,255,255,0.2);
  padding: 1px 6px;
  border-radius: 3px;
  font-size: 10px;
  font-family: monospace;
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
}
.edit-section {
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
  border-bottom: 1px solid var(--bg-subtle);
  flex-wrap: wrap;
  position: sticky;
  top: 0;
  background: var(--bg-container);
  z-index: 5;
}
.section-sub {
  font-size: 12px;
  font-weight: 400;
  color: #9ca3af;
  margin-left: 8px;
}
.section-tools {
  margin-left: auto;
  display: flex;
  gap: 4px;
}
.form-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 0 16px;
}
.form-grid :deep(.el-form-item) {
  margin-bottom: 12px;
}
.form-grid :deep(.full-row) {
  grid-column: span 4;
}
.lines-section {
  display: flex;
  flex-direction: column;
  position: relative;
}
.lines-toolbar {
  margin-left: auto;
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}
.price-mode-hint {
  font-size: 12px;
  color: #6b7280;
}
.info-icon {
  color: #9ca3af;
  font-size: 14px;
  cursor: help;
}
/* KEY: lines-table-wrap wraps the el-table; el-table handles its own horizontal scroll
   internally when sum of column widths exceeds visible width. We do NOT set min-width
   on .el-table because that would suppress el-table's native horizontal scrollbar. */
.lines-table-wrap {
  width: 100%;
  overflow: visible;
  margin-bottom: 8px;
}
.lines-table-wrap :deep(.el-table) {
  width: 100% !important;
}
/* Ensure el-table's internal body wrapper shows horizontal scrollbar */
.lines-table-wrap :deep(.el-table__body-wrapper) {
  overflow-x: auto !important;
}
/* Make sure columns don't shrink — force them to keep their declared widths using CSS var */
.lines-table-wrap :deep(.el-table__body),
.lines-table-wrap :deep(.el-table__header) {
  width: var(--line-table-total-width, 2400px) !important;
}
/* G1B-1R8: Two-Tier Aggregate Header — no footer reorder hack.
   Each column's #header slot renders Tier 1 (aggregate) + Tier 2 (label).
   Both tiers share the same el-table column width/scroll/fixed — Single Column Model. */
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
/* Warehouse cell append group: search + follow buttons */
.wh-append-grp {
  display: flex;
  align-items: center;
  gap: 2px;
}
.tabs-section {
  margin-bottom: 16px;
}
.tab-content {
  padding: 8px 4px;
  min-height: 120px;
}
.upload-area {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 12px;
}
.upload-hint {
  font-size: 12px;
  color: #9ca3af;
}
/* Attachment actions: icon-only buttons on single line */
.att-actions {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 4px;
  flex-wrap: nowrap;
  white-space: nowrap;
}
.att-actions :deep(.el-button) {
  margin: 0;
  padding: 0 4px;
  min-height: 24px;
  height: 24px;
}
:deep(.att-actions-col .cell) {
  white-space: nowrap !important;
  overflow: visible !important;
  display: flex;
  justify-content: center;
  padding-left: 4px;
  padding-right: 4px;
}
.rel-section { margin-bottom: 16px; }
.rel-section h4 {
  margin: 0 0 8px;
  font-size: 13px;
  color: #4b5563;
}
.field-settings {
  padding: 8px 16px;
}
.hint-text {
  color: #6b7280;
  font-size: 12px;
  margin: 0 0 12px;
  line-height: 1.6;
}
.field-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.field-item {
  padding: 8px 10px;
  border: 1px solid #e5e7eb;
  border-radius: 4px;
  background: #fff;
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
:deep(.el-table__row.line-done) {
  background: #f0fdf4 !important;
}
:deep(.el-table__row.line-partial) {
  background: #fffbeb !important;
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
.cell-item {
  cursor: pointer;
  padding: 2px 4px;
  border-radius: 3px;
}
.cell-item:hover { background: #eff6ff; }
.item-code {
  font-size: 12px;
  color: #6b7280;
  font-family: monospace;
}
.item-name {
  font-size: 12.5px;
  color: #1f2937;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.gs-col-settings {
  padding: 8px 16px;
}
.gs-field-hint {
  color: #6b7280;
  font-size: 12px;
  margin: 0 0 12px;
  line-height: 1.6;
}
.gs-col-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
  max-height: 520px;
  overflow-y: auto;
  padding-right: 4px;
}
.gs-col-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 10px;
  border: 1px solid #e5e7eb;
  border-radius: 4px;
  background: #fff;
  user-select: none;
}
.gs-col-item[draggable="true"] {
  cursor: move;
}
.gs-col-handle {
  color: #9ca3af;
  font-size: 16px;
  cursor: grab;
  flex-shrink: 0;
}
.gs-col-handle:active {
  cursor: grabbing;
}
</style>
