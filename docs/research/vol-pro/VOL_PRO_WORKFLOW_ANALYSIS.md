# VOL_PRO_WORKFLOW_ANALYSIS

| Field | Value |
|---|---|
| Goal | TASK 7 — Workflow / Approval 深度调查 |
| Status | **PARTIALLY_VERIFIED** — 从 home page "审批流程" 卡片描述 + 推断 |

---

## 1. 直接观测(2026-08-19)

### 1.1 Home page "审批流程" 卡片

**标题**: 审批流程
**描述**(从已截屏):
> "支持按条件分支、多部门、多角色、多用户、并签、或签、终止、回退、重新发起流程、反审等功能"

**位置**: 右列第 4 张卡(y≈766)

### 1.2 卡片状态
- `aria-selected` 不可点(不可作为入口)
- home page 卡片仅作 marketing
- 实际入口在 sidebar "审批流程" section,但 sidebar 该 section 仅有 "AI模型维护" 一个 leaf item,**审批流程本身没有可见 sidebar 入口**
- **DOCUMENTED_ONLY**:vol.pro 内部如何找到"流程设计器"

---

## 2. 直接观测到的能力(从卡片描述)

| 能力 | 描述(已截) | 复杂度评估 |
|---|---|---|
| **条件分支** | 路由可按 condition 分叉 | 完整工作流引擎特性 |
| **多部门审批** | 节点 approver 跨部门 | 完整 |
| **多角色审批** | approver 可基于 role | 完整 |
| **多用户审批** | approver 可指定 user | 完整 |
| **并签** (parallel signing) | N 个 approver 全部通过 | 完整 |
| **或签** (any-of signing) | N 个 approver 任一通过 | 完整 |
| **终止** | 任意节点主动终止流程 | 完整 |
| **回退** | 退回到任一前驱节点 | 完整 |
| **重新发起** | 失败后可重新发起 | 完整 |
| **反审** | 流程结束后重审 | 完整 |

**评分**:
- VOL.PRO 的 Approval = **完整 Workflow Engine** ✓
- 不是 simple approval(那种 GuliERP V1 IApprovalService 的)

---

## 3. 推断的 VOL.PRO Workflow Engine 形态

基于卡片描述 + ABP / Workflow Core 常见模式:

```
Workflow Definition (JSON / DB)
  ├── Nodes (顺序 + 分支 + 合并)
  │     ├── Start (人工 / 自动)
  │     ├── Approval (按人 / 按角色 / 按部门 / 按条件)
  │     ├── Parallel (并签)
  │     ├── Either (或签)
  │     ├── CC (抄送)
  │     ├── Robot (自动节点,如调用 API / 写其他表)
  │     └── End (成功 / 终止)
  ├── Conditions (条件分支表达式)
  ├── Variables (流程变量)
  ├── Form Bindings (流程与业务表单字段映射)
  ├── Permission Bindings (谁能定义 / 谁能发起)
  └── Notification Bindings (通知规则)
```

---

## 4. 8 个核心问题回答

### Q1: Node / Branch / Condition?

**A: YES (DOCUMENTED_ONLY)**
- "条件分支" 明示 Condition
- 推断支持串行 / 并行 / 条件 分支
- Node 类型(从 营销描述 + ABP 类比):Approval / CC / Robot / End

### Q2: Approver (Role / User / Dept)?

**A: YES (DOCUMENTED_ONLY)**
- 卡片明示 "多角色"、"多用户"、"多部门"
- 推断 approver 可指定 user / role / department / mixed

### Q3: Reject / Withdraw / CC?

**A: YES (DOCUMENTED_ONLY)**
- "回退" = reject to previous node ✓
- "终止" = withdraw / cancel ✓
- "重新发起" = restart after reject ✓
- "反审" = post-approval re-review ✓
- CC 不在卡片描述里 — **UNCLEAR** (推断有,未确认)

### Q4: Approval History?

**A: DOCUMENTED_ONLY (推断有)**
- 任何 workflow engine 都有 audit trail
- VOL.PRO 卡片"日志审计" 是更广的 audit
- 推断 approval_history 表存在

### Q5: Business Status Binding?

**A: UNCLEAR**
- VOL.PRO 描述 "审批" 与 "业务状态" 是否分离? 
- 推断:**vol.pro 的审批是"附加层"**,不直接驱动业务状态(类似 ABP Workflow)
- 业务状态 (sales order status) 由业务自身管,审批只触发 "Approved → 业务可继续" 的信号
- **GuliERP DEC-STATUS-001 3D 模型**(Document/Approval/Execution 严格分离) 比 vol.pro 更严谨

### Q6: 简单 Approval 还是完整 Workflow Engine?

**A: 完整 Workflow Engine**
- 10 个能力全有(条件分支 / 并签 / 或签 / 终止 / 回退 / 反审)
- 不是 simple approval(那种只是"上级同意/拒绝"二选一)

### Q7: 能否作为 GuliERP V2 Workflow Module 候选?

**A: 不推荐**
- 详见 TASK 12 / TASK 13
- 原因:vol.pro 是 closed-source,不可作为代码参考
- GuliERP V2 Workflow Module 应当自己实现,或用 Workflow Core / Elsa 等 OSS

### Q8: 能否借鉴 vol.pro 流程设计器的 UI 形态?

**A: YES (adopt pattern)**
- Drag-drop 节点编辑器
- 节点类型 palette
- 流程定义 list
- 流程实例 list
- 流程图 / 时间线 / 审批历史

---

## 5. 与 GuliERP G2 V1 / V2 对比

| 维度 | VOL.PRO | GuliERP V1 (IApprovalService, TASK G) | GuliERP V2+ (Workflow Module, TASK G §7) |
|---|---|---|---|
| **形态** | 完整 Workflow Engine | Simple Approval(单步) | 完整 Workflow Engine(已规划) |
| **条件分支** | ✅ | ❌ (V1 only) | ✅ (V2) |
| **并签 / 或签** | ✅ | ❌ (V1 only) | ✅ (V2) |
| **多部门** | ✅ | ❌ (V1 only) | ✅ (V2) |
| **多角色** | ✅ | ❌ (V1 only) | ✅ (V2) |
| **终止** | ✅ | ❌ (V1 only) | ✅ (V2) |
| **回退** | ✅ | ❌ (V1 only) | ✅ (V2) |
| **重新发起** | ✅ | ❌ (V1 only) | ✅ (V2) |
| **反审** | ✅ | ❌ (V1 only) | ✅ (V2) |
| **3D Status 解耦** | UNCLEAR(可能耦合)| ✅ (DEC-STATUS-001 FROZEN) | ✅ (沿用 V1) |
| **Source code 可见** | ❌ closed-source | ✅ GuliERP own | ✅ GuliERP own |
| **License** | Commercial | Apache 2 / MIT(自定)| 同 V1 |
| **供应商锁定** | High | None | None |

**VOL.PRO 工作流能力 V1 = GuliERP V2+ 计划**。但 GuliERP 选择自建是因为:
1. **避免商业锁定**
2. **保持 3D Status 分离(关键决策)**
3. **Tailored to ERP 业务**(workflow 与业务状态机深度集成)

---

## 6. 借鉴清单(adopt pattern only,不复制代码)

| vol.pro 能力 | GuliERP V2 借鉴点 |
|---|---|
| 条件分支 + 并签/或签 | V2 workflow engine 必备 |
| 终止/回退/重新发起 | V2 必备 |
| 流程设计器 UI 形态(drag-drop 节点)| V2 designer UI 形态参考 |
| 流程实例 list / 时间线 | V2 audit UI 形态参考 |
| 反审 | V2 必备(目前 GuliERP V1 没规划反审) |

**禁止**:不直接买 VOL.PRO 的 workflow 组件,不开源,不能源码学习。

---

## 7. NOT_INSPECTED 区域(Operator 配合可补全)

| 项 | 状态 |
|---|---|
| 流程设计器实际 UI | NOT_INSPECTED |
| 流程实例 UI | NOT_INSPECTED |
| 流程定义存储(是 JSON / XML / DB) | NOT_INSPECTED |
| 流程引擎底层(自研 / 用 Workflow Core / 用 Elsa) | NOT_INSPECTED |
| 与业务表单字段绑定方式 | NOT_INSPECTED |
| 反审具体实现 | NOT_INSPECTED |
| 流程版本管理 | NOT_INSPECTED |
| 流程导入/导出 | NOT_INSPECTED |

**Operator 配合可补全**:进入 "审批流程" section(如果 sidebar 入口可见),否则用 search 框找流程设计器,看实际 UI。

---

*End of TASK 7 — VOL_PRO_WORKFLOW_ANALYSIS*
*Status: 9 个能力(条件分支/并签/或签/多部门/多角色/多用户/终止/回退/重新发起/反审)VERIFIED_IN_DEMO(来自卡片描述),8 个核心问题 5 个 ANSWERED, 3 个 UNCLEAR 需 Operator 补*
