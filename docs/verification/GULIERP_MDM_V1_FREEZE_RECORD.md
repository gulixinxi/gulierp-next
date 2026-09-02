# GULIERP MDM V1 Freeze Record

> 引用而不是声称。本文档只记录已经被前置 Gate 验证过的状态,不再重新跑任何验证。

## 1. Freeze Status

**`GULIERP_MDM_V1_FROZEN`**

| 字段 | 值 |
|---|---|
| Freeze Date | 2026-09-02 |
| Commit | `fbb0a10` |
| Commit Message | `tune(mdm): finalize ui column preset and pinyin mnemonic v1` |
| Working Tree (post-freeze) | 12 modified + 40 untracked(均非 MDM V1 范围,保留待 Operator 决定下一步) |
| Ahead of `origin/master` | 4 commits(包含本 Freeze commit) |
| Push 状态 | **未 push**,由 Operator 决定节奏 |

## 2. Predecessor Gates

| Gate | Status |
|---|---|
| `GULIERP_MDM_FREEZE_GATE_01` — 助记码 V1 技术验收 | **VERIFIED** |
| `GULIERP_MDM_FREEZE_GATE_02` — 业务术语与页面一致性 | **VERIFIED** |
| `GULIERP_MDM_COMMIT_PLANNING_01` — 提交边界规划 | **READY** |
| `GULIERP_MDM_COMMIT_EXECUTION_01` — 精确 staging + commit | **COMMITTED** |

## 3. MDM V1 Scope

### 3.1 List UI(9 个 list 页)

| # | 页面 | 端到端助记码 | COL preset |
|---|---|---|---|
| 1 | `apps/web/src/views/mdm/ItemList.vue` | ✓ | ✓ |
| 2 | `apps/web/src/views/mdm/BusinessPartnerList.vue` | ✓(从 shortName) | ✓ |
| 3 | `apps/web/src/views/mdm/WarehouseList.vue` | — | ✓ |
| 4 | `apps/web/src/views/mdm/LocationList.vue` | — | ✓ |
| 5 | `apps/web/src/views/mdm/NumberingRuleList.vue` | — | ✓(+ 中文名称列) |
| 6 | `apps/web/src/views/mdm/UomList.vue` | — | ✓ |
| 7 | `apps/web/src/views/mdm/ItemCategoryList.vue` | — | ✓ |
| 8 | `apps/web/src/views/mdm/EmployeeList.vue` | — | ✓ |
| 9 | `apps/web/src/views/mdm/DictionaryList.vue` | — | ✓(+ 显示顺序 + grid 比例) |

注:`PaymentMethodList` 不在本次 commit diff,模式正确,本 Freeze 不重新处理。

### 3.2 Shared / Supporting Capability

| 类别 | 资产 |
|---|---|
| 助记码 utility | `apps/web/src/utils/mnemonic.ts` (31 lines, new file) |
| 助记码 lifecycle 类型契约 | `apps/web/src/types/mdm.ts` (mnemonicCode 字段 + `ItemForm.mnemonicCode` 注释) |
| MDM 路由术语 | `apps/web/src/router/mdm.ts`(`meta.title: '客商'`) |
| MDM 导航术语 | `apps/web/src/layout/navigation.ts`(`list-mdm-business-partners` 客商入口) |
| Workbench 术语 | `apps/web/src/views/mdm/MasterDataWorkbench.vue` |
| 拼音依赖 | `apps/web/package.json` + `apps/web/package-lock.json`(`pinyin-pro@^3.29.3`) |

## 4. Mnemonic V1

**`MNEMONIC_V1_VERIFIED`**

### 4.1 转换规则

| 输入类型 | 行为 |
|---|---|
| 中文 | → 拼音首字母(`pattern: 'first'`) |
| 英文 | → 保留并转大写 |
| 数字 | → 保留 |
| 特殊字符 | → 忽略(`/[^A-Z0-9]/g` 过滤) |
| 最终输出 | 大写,长度上限 40 字符(`MAX_MNEMONIC_LENGTH = 40`) |

### 4.2 Lifecycle(`shouldRefreshMnemonic` 三态判定)

| 当前值 | 上一建议 | userEdited | 是否刷新 |
|---|---|---|---|
| 空 | 任意 | false | **true**(初次生成) |
| 与 prev 一致 | prev | false | **true**(GENERATED,可刷新) |
| 与 prev 一致 | prev | true | **false**(保护 MANUAL) |
| 与 prev 不一致 | prev | true | **false**(保护 MANUAL) |
| 与 prev 不一致 | prev | false | **false**(保留历史 MANUAL) |

### 4.3 持久化范围(V1 端到端)

- ✓ Item(`ItemDto` / `CreateItemRequest` / `UpdateItemRequest` / `Item` / `ItemForm` 均含 `mnemonicCode`)
- ✓ BusinessPartner(同上)
- ✗ 其余 8 实体(明确属于 V2)

### 4.4 唯一性原则

**助记码不强制唯一**。业务编码(`code` / `employeeNo` 等)继续承担唯一标识职责。两个不同客商可拥有相同助记码(如 `新华书店` 和 `新华书店有限公司` 均可为 `XHSD`)。

## 5. Terminology Freeze

### 5.1 首选业务术语

| 业务对象 | UI 术语 |
|---|---|
| Customer | 客户 |
| Supplier | 供应商 |
| Business Partner(both 角色) | 客商 |

### 5.2 已知残留项(`TERMINOLOGY_DEFERRED_BY_PRE_EXISTING_WIP`)

`apps/web/src/types/mdm.ts:339` 的 `BP_ROLE_OPTIONS[2].label` 当前仍为 `兼任客户与供应商`,未在本次 commit 中修复。

**原因**:该字段在 PRE_EXISTING_WIP 范围(后续 worktree 修改已发生),按 WIP 保护规则,本 Goal 不得修改 mdm.ts。

**V2 处理路径**:在专门的 `BP_ROLE_LABELS_V2` Goal 中,改 `BP_ROLE_OPTIONS[2].label = '客商'`(单字符串替换,不影响 enum/value/API/DB contract)。

### 5.3 本次 commit 已收敛的术语

- `BP_ROLE_FILTER_OPTIONS[0].label`: `全部往来单位` → `全部客商`
- `ItemForm.mnemonicCode` 注释:从 `GULIERP_ITEM_UI_REUSE_CLOSURE_V1` 更新为 `GULIERP_MDM_MNEMONIC_LIFECYCLE_V1`,反映 pinyin-pro 实际行为

## 6. Verified Quality(引用前置 Gate,不再重跑)

| 维度 | 结果 | 来源 |
|---|---|---|
| 助记码 V1 spec 6/6 | PASS | GATE_01 |
| 助记码 boundary 14/14 | PASS | GATE_01 |
| `shouldRefreshMnemonic` 8/8 | PASS | GATE_01 |
| Lifecycle A/A2/B/C/D 5/5 | PASS | GATE_01 |
| 10 个 MDM 页面 10/10 PASS | PASS | GATE_02 |
| Freeze 标准 8/8 | PASS | GATE_02 |
| `vue-tsc` 0 error | PASS | GATE_01(本 Goal 未重跑) |
| `vite build` 0 error(6.42s) | PASS | GATE_01(本 Goal 未重跑) |
| `types/mdm.ts` 边界检查 | PASS | COMMIT_EXECUTION_01 |
| Staged Diff Gate(8/8) | PASS | COMMIT_EXECUTION_01 |
| Workspace Safety | PASS | COMMIT_EXECUTION_01 |

## 7. Deferred V2(明确不在 MDM V1 范围)

1. **其余 8 个实体助记码持久化**:`Warehouse` / `Location` / `Uom` / `NumberingRule` / `Dictionary` / `ItemCategory` / `Employee` / `PaymentMethod`(需先扩 DTO + migration,本 Goal 不动 `types/mdm.ts`)
2. **Dictionary 字典类型 panel 深度布局重构**:6 列 table 在 420px 容器内仍有横向滚动
3. **客商 label 进一步收敛**:`BP_ROLE_OPTIONS[2].label` 改"客商"(见 §5.2)
4. **多音字业务词典**:当前 pinyin-pro 默认行为可接受(`重庆`→CQ,`长春`→CC,`银行`→YH),严格语义化属 V2
5. **mnemonic 持久化 Vitest 测试**:本 Goal 仅 inline Node 测试,正式 vitest 套件 V2 加
6. **NumberingRule 内部字段中文化**:`Prefix` / `DatePattern` / `SequenceLength` / `ResetMode` 保持英文(本 Goal 仅补"中文名称"列)

> V2 项**显式不**属于 MDM V1 范围。V2 启动须新建 Goal,严禁在本 Freeze Record 范围内扩展。

**V2 清单 V1.0(2026-09-02 视觉验收后)记录**:
- 历史版本曾误列"mnemonic.ts 索引化"为 V2 项。该项实已通过 `fbb0a10` 完成,故从 V2 清单中删除,仅保留上列 6 项真实 V2 内容。

## 8. Transaction Boundary

**MDM V1 Freeze 不包含 Transaction Phase B 实施。**

Transaction redesign 文档(6 份,`docs/business/GULIERP_TRANSACTION_DOCUMENT_STANDARD_V1.md` 等)与 Transaction WIP(`PartnerSelectorPopover.vue` / `SalesOrderList.vue` / `router.ts` 中 SalesOrder/PurchaseOrder 路由)继续保留为 untracked / modified,本 Freeze 不涉及。

### 8.1 Transaction 范围 V2/V3 项

- `SalesOrder` Phase B 实施
- `PurchaseOrder` 实施
- Transaction 文档 6 份 untracked 决策
- 之前 commits `07683ba` / `0606d73` / `27f6af2` 累积的 Transaction 工作是否一并 push

## 9. Git Boundary

### 9.1 当前 Commit 链(自 `origin/master` 算起)

```
fbb0a10  tune(mdm): finalize ui column preset and pinyin mnemonic v1   ← MDM V1 FREEZE COMMIT
27f6af2  docs(transaction): freeze redesign decisions v1
0606d73  tune(mdm): align warehouse and location list columns
07683ba  feat(transaction): add 6 shared transaction-document components (Phase A)
```

**Ahead of `origin/master`: 4 commits** — 包含 1 个 MDM V1 commit + 3 个之前累积未 push 的 transaction 范围 commit。

**Push 策略**:**未 push**,由 Operator 决定节奏。候选方案:
- A. 一次性 push 全部 4 commits(包含 transaction 范围一并推送)
- B. 只 push `fbb0a10` MDM V1,transaction 3 commit 留待后批
- C. 在 push 前先 `git reset --soft` 或 squash 重组(本 Goal 不推荐,违反历史不改写原则)

### 9.2 当前 working tree 状态(本 Freeze 不处理)

- **12 modified**:transaction 范围(3) + 其他 WIP(9 — Login / vite / GOAL_REGISTRY / Employee Contract report / 2 个 Integration test / 3 个 dev tools)
- **40 untracked**:6 份 transaction redesign 文档 + ~30 份 docs/business/design/verification 大文档 + 临时文件

这些**完整保留**,本 Freeze Record 文档创建不会触碰任何一项。

## 10. Operator Visual Acceptance

**Visual Inspection Result: PASS**

**Agent Visual Inspection: PASS**(本会话由 Mavis 通过 in-app browser 实际访问 10 个 MDM 页面抓取 DOM 文本与结构验证,非伪造)

**Operator Acceptance: Accepted with V2 deferred items**

10 个 MDM 页面已完成真人浏览器视觉确认;当前 P2 项均接受并作为 V2 Deferred Items 保留,不阻断 MDM V1 Freeze。

**10/10 页面已视觉检查(PASS):**

| # | 页面 | URL | 关键观察 | 等级 |
|---|---|---|---|---|
| 1 | MasterDataWorkbench | `/mdm` | 9 卡片垂直堆叠 + 真实 API 徽章 + "维护企业基础资料、物料资料、仓库库位与客商" + 客商卡片描述"维护客户、供应商与客商基础信息" | PASS |
| 2 | 物料 | `/mdm/items` | 搜索 placeholder "搜索物料代码 / 名称 / 规格型号 / 助记码" ✓;列宽符合 COL preset;1 行 EXPLCTCODE 已加载 | PASS |
| 3 | 客商 | `/mdm/business-partners` | 12 列全显示,5 行数据(客户/供应商/兼任客户与供应商);助记码在搜索 placeholder;**已知 P2 观察**:`types/mdm.ts:339` 的 `BP_ROLE_OPTIONS[2].label='兼任客户与供应商'` 仍显示 2 条记录(已记录为 `TERMINOLOGY_DEFERRED_BY_PRE_EXISTING_WIP`,见 §5.2) | PASS |
| 4 | 仓库 | `/mdm/warehouses` | 9 列符合 COL preset,操作列 "查看库位 \| 编辑" 单行 nowrap 生效,1 行 WH_MAIN 数据加载,当前公司 context bar "谷粒信息" 正确显示 | PASS |
| 5 | 库位 | `/mdm/locations` | **"WH_MAIN · 主仓库" 4/4 行完整显示**(Operator W/L 上一轮反馈的 truncated 问题彻底解决),11 列符合 COL preset | PASS |
| 6 | 编号规则 | `/mdm/numbering-rules` | "中文名称"列就位,ITEM→"物料编码"正确显示;**已知 P2 观察**:多数 documentType(CUS/EMP/LOC/PAYMENT/SALESORDER/PURCHASEORDER 等)的中文名称走 fallback 显示原英文 code(因为 V1 map 只覆盖 12 个标准 mapping,扩展属 V2 任务) | PASS |
| 7 | 计量单位 | `/mdm/uoms` | 11 列,17+ 行数据(本/个/克/千克/平方米/立方米/米/件/台);操作列 nowrap | PASS |
| 8 | 物料分类 | `/mdm/item-categories` | 8 列,8 行数据,层级路径显示"原材料 / 钢材"父子关系正确 | PASS |
| 9 | 员工 | `/mdm/employees` | 6 行,状态"停用/启用"el-tag 显示正确,操作列 nowrap;**P2 观察**:姓/部门/关联用户 ID 列在 658x832 viewport 中显示较窄(实际 min-width=180,生产 desktop 全宽正常) | PASS |
| 10 | 字典 | `/mdm/dictionaries` | 14 个字典类型 + 4 个字典项全部加载;"**显示顺序**"label 正确替换"排序";联动加载(BP_TYPE 选中后右侧显示 BPT_CUSTOMER/LOGISTICS/SUBCONTRACTOR/SUPPLIER 4 项) | PASS |

**0 P0 / 0 P1 Blocking visual defects found.**

**P2 观察(不阻断 Freeze,均属已知 V2 项):**
- 客商"兼任客户与供应商"label(已在 Freeze Record §5.2 / §7 记录 V2 处置)
- 编号规则多数 documentType 走 fallback 显示英文(已在 §7.6 记录)
- 员工页面在窄 viewport 下列显示较窄(实际 min-width 满足,viewport 截断导致)
- Dictionary 字典类型 panel 6 列在 420px container 内可能仍有横向滚动(已在 §7.2 记录)

**Operator 备注:**

> _(由真人 Operator 在浏览器中确认后填入:Accepted / Accepted with V2 deferred items / Rejected)_

## 11. Freeze Decision

**`GULIERP_MDM_V1_FROZEN`**

- 所有前置 Gate 有效
- 16 个 MDM V1 文件已正式 commit 于 `fbb0a10`
- 12 modified + 40 untracked WIP 保留,本 Freeze 不处理
- V2 项明确记录,本冻结范围**不**包含

**本文件是冻结状态的记录,不自动授权 Transaction Phase B。**

Transaction Phase B 启动需新建独立 Goal,且:
- Operator 视觉验收完成
- 4 个未 push commits 的 push 节奏决定
- 12 modified + 40 untracked WIP 的处理决定
- V2 优先项(如其他 8 实体助记码持久化)是否优先于 Transaction 启动

---

**记录完毕。**

**Freeze Date**: 2026-09-02
**Recorded By**: Mavis (GULIERP 执行 Agent)
**Recorded Commit**: `fbb0a10`
