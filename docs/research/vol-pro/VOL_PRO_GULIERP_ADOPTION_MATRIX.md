# VOL_PRO_GULIERP_ADOPTION_MATRIX

| Field | Value |
|---|---|
| Goal | TASK 12 — GuliERP 借鉴/拒绝 决策矩阵 |
| Categories | **ADOPT_NOW** / **ADOPT_PATTERN_ONLY** / **RESEARCH_LATER** / **REJECT** |

---

## 1. 完整评价矩阵(19 个维度)

| 维度 | VOL.PRO 成熟度 | GuliERP V1 状态 | 决策 | 理由 |
|---|---|---|---|---|
| **Navigation** | A (深色 chrome, 多级 sidebar, multi-tab) | A (R3 design system 已设计) | **ADOPT_NOW** | R3 已规划,继续 |
| **Design System** | A (4px grid, 13px, semantic 4 status) | A (R3 已定 tokens) | **ADOPT_NOW** | R3 同步 |
| **Table** | A 推断(高性能表格) | A (R3 EP table) | **ADOPT_NOW** | — |
| **Form** | B (AI 表单 vs 普通 ERP 表单) | A (R3 standard form) | **ADOPT_PATTERN_ONLY** | 借鉴 LLM 增量修改思路(开发期),不引入给终端用户 |
| **MasterDetail** | A 推断(代码生成可视化) | A (R3 SalesOrder pattern) | **ADOPT_PATTERN_ONLY** | 借鉴 1-n 自动化机制(开发期)|
| **Lookup** | A 推断(下拉Table自定义搜索) | A (R3 gs-lookup-input) | **ADOPT_NOW** | R3 已规划 |
| **CodeGenerator** | A (整个产品就是 CodeGen) | D (GuliERP **无** CodeGen) | **REJECT** | R5 风险 + DEC-MODULE-001 冲突 + GuliERP 是 ERP 不是 ERP-builder |
| **Workflow** | A (完整 engine,10 个能力)| V1 stub (IApprovalService 单步)| **ADOPT_PATTERN_ONLY** | V1 沿用 IApprovalService,V2 借鉴 UI 形态 + 节点能力 |
| **Permission** | A (RBAC + 菜单/角色/字段 3 层) | V1 stub (IPermissionService.HasAsync) | **ADOPT_PATTERN_ONLY** | V1 接口 + stub,V2+ 完整实现 |
| **MultiCompany** | A (Tenant + Company 1:N) | A (G2 设计 1:N) | **ADOPT_PATTERN_ONLY** | G2 设计已借鉴 Top bar 切换器 UI |
| **DataScope** | A (4+ Scope 模式) | V1 stub (IDataScopePolicy) | **RESEARCH_LATER** | V2+ 借鉴实现 |
| **Dictionary** | A 推断 | A (G2 设计) | **ADOPT_NOW** | G2 已设计 |
| **ImportExport** | A (Export 卡片) | V1 未实现 | **RESEARCH_LATER** | V1.5+ 借鉴格式 |
| **Print** | A (Visual Designer) | V1 backend HTML | **RESEARCH_LATER** | V1.5+ 借鉴 PrintDesigner UI |
| **Audit** | A (日志审计卡片) | A (G2 IAuditWriter) | **ADOPT_NOW** | G2 已设计 |
| **Module System** | n/a (VOL 是 monolithic) | A (G2 IModule 设计) | **ADOPT_NOW** | GuliERP 自研,不参考 |
| **API Architecture** | A (ABP 风格 REST) | A (G2 TASK F 设计) | **ADOPT_NOW** | 路线相似,但 GuliERP 禁止万能 CRUD / dynamic SQL |
| **Backend Framework** | .NET 8 + EF Core + SqlSugar | .NET 10 + EF Core 10 + Npgsql | **ADOPT_NOW** | GuliERP 自选更现代 |
| **License Model** | MIT (OSS) / 商业 (PRO) | Apache 2 / MIT (GuliERP 自定) | **ADOPT_NOW** | GuliERP 选 own source |

---

## 2. ADOPT_NOW(立即采用)— 7 项

1. **Navigation pattern** — Multi-tab + 深色 chrome 双层 + 可折叠 sidebar(R3 已规划)
2. **Design System tokens** — 4px grid + 13px base + 4 状态色(R3 已定)
3. **Table component** — R3 EP table 已实现
4. **Lookup pattern** — R3 gs-lookup-input 已实现
5. **Dictionary** — G2 IDictionaryQuery 已设计
6. **Audit** — G2 IAuditWriter 已设计
7. **Module System + API + Backend + License** — GuliERP 自研路线已确定,不需借鉴

---

## 3. ADOPT_PATTERN_ONLY(借鉴模式,不复制)— 5 项

1. **Form** — 借鉴 LLM 增量修改思路(开发期 dev tool),**不**给终端用户
2. **MasterDetail** — 借鉴 1-n 自动化机制(开发期)
3. **Workflow** — V1 沿用 GuliERP `IApprovalService`,V2 借鉴 UI 形态
4. **Permission** — V1 接口 + stub,V2+ 完整实现
5. **MultiCompany UI** — 借鉴 Top bar 切换器(已 G2 设计)

---

## 4. RESEARCH_LATER(V1.5+ 评估)— 3 项

1. **DataScope** — V2+ 借鉴 4+ Scope 模式
2. **ImportExport** — V1.5+ 借鉴 Export UI 与格式
3. **Print** — V1.5+ 借鉴 PrintDesigner UI 形态

---

## 5. REJECT(明确不采用)— 1 项

1. **CodeGenerator** — **禁止**
   - 理由:R5 风险(GuliERP 风险登记已记入)
   - 理由:DEC-MODULE-001 冲突(VOL 是 monolithic,GuliERP 是 modular)
   - 理由:GuliERP 定位是 **ERP** 不是 **ERP-builder**
   - 理由:无法表达 3D 状态 / 含税未税 / Reservation / Posting Engine 等业务深度

---

## 6. 综合决策:5 个战略选项

| 选项 | 评价 | 风险 | 推荐 |
|---|---|---|---|
| **A. 完全自研 GuliERP Foundation** | G2 计划 10 个 goal 已设计 | 低 | ✅ 当前路线 |
| **B. 自研业务 + 借鉴 VOL 模式** | A 的细化;在 R3 design / Permission / Workflow 借鉴 | 低 | ✅ **强烈推荐 A+B** |
| **C. 重新评估以 VOL 开源版 (MIT) 为 Foundation** | Foundation 复用 60-65%,业务 0% 自建 | 中-高(R5 + Module 独立) | ❌ 不推荐 |
| **D. 购买 VOL.PRO 商业 Foundation** | 闭源 + Vendor lock + 商业 license | 极高 | ❌ **不推荐** |
| **E. VOL 只作为 Reference** | 借鉴能力成熟度,不引入 | 低 | ✅ **A+B+E 一起用** |

### 6.1 最终推荐排序

**强烈推荐**: **A + B + E** 组合
- A (自研 Foundation) — 主路线
- B (借鉴 VOL 模式) — 加速设计 / 决策
- E (VOL 作 Reference) — 不引入,只学习

**不推荐**: C, D
- C (VOL.NET OSS 替代) — 净负,业务 0 复用 + R5 风险
- D (VOL.PRO 商业) — 闭源 + 极高 Vendor lock

---

## 7. R5 风险更新(给 GuliERP 风险登记)

**R5 风险登记追加**:
- VOL.NET 是 low-code engine(GitHub 1.4k forks, MIT)
- VOL.PRO 是 VOL.NET 的商业版(加 AI + Workflow Designer + Print Designer + 国密)
- 两者均与 **DEC-MODULE-001 冲突**(都是 monolithic framework,而非 modular)
- 两者均与 **GuliERP 定位冲突**(是"造 ERP 的工具"而非 ERP)
- 两者均与 **R5 风险 high**(GuliERP 风险登记 R5 描述"low-code generic engine")

**结论**:即使 OSS 免费,也不应作为 Foundation 引入(净负)。PRO 商业版 + 闭源 + Vendor lock 更不推荐。

---

## 8. 借鉴清单(具体可执行项)

### 8.1 立即采用(R3 已规划或 G2 已设计)

- 4px grid + 13px base
- 4 状态色(danger/warning/success/info)
- 60px topbar + 280px sidebar + 可折叠
- Multi-tab + close ×
- Top bar 显示租户切换 + 语言切换 + 用户徽章(含上次登录时间)

### 8.2 短期借鉴(V1.5+ — 1-3 个月)

- Tab 右键菜单(关闭左边/右边/其他/刷新)
- 顶栏"上次登录时间"显示
- 报表导出按钮
- PrintDesigner 雏形(从 HTML 模板 → visual designer)

### 8.3 中期借鉴(V2+ — 3-12 个月)

- Workflow Designer UI(drag-drop 节点)
- DataScope 配置 UI(4+ 模式)
- 字段权限 Mask UI
- CodeGen 思路(在 GuliERP 内部 dev tool,不是 terminal feature)

### 8.4 长期借鉴(V3+ — 1+ 年)

- AI 辅助开发(在 dev tool 内部,非 terminal)
- 国密合规认证
- 信创认证
- Mobile (uniapp)

---

## 9. 决策记录(给 Operator / 后续 Agent)

- **决策日期**: 2026-08-19
- **决策人**: Mavis (after Operator 通过 VOL-PRO-001 task 启动)
- **决策状态**: DRAFT(待 Operator 签字)
- **决策有效期**: 至 G2 Foundation Implemented 完成为止
- **复审触发**:
  - Operator 决定改路线
  - G2 Foundation 实施遇到 2 周以上 blocker
  - VOL.NET/VOL.PRO 出现重大 license 变化
  - 出现新的更优 Foundation 候选(如 Elsa / ABP 商业版)

---

*End of TASK 12 — VOL_PRO_GULIERP_ADOPTION_MATRIX*
*Status: 19 个维度评分,7 ADOPT_NOW,5 ADOPT_PATTERN_ONLY,3 RESEARCH_LATER,1 REJECT。最终推荐 A+B+E(自研+借鉴模式+Reference);不推荐 C/D(OSS/PRO 替代 Foundation)*
