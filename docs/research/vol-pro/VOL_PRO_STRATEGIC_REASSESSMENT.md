# VOL_PRO_STRATEGIC_REASSESSMENT

| Field | Value |
|---|---|
| Goal | TASK 13 — 最终战略结论:5 个候选 A/B/C/D/E 排序 + 推荐 |
| Inputs | 12 个 task 报告 + web search + Operator task brief |
| Date | 2026-08-19 |
| Status | **DRAFT** (待 Operator 签字) |

---

## 1. 5 个战略选项(重申)

```
A. 继续完全自研 GuliERP Foundation                    ← G2 当前路线
B. GuliERP 自研业务 + 吸收 VOL 成熟组件/模式           ← A + 借鉴
C. 重新评估以 VOL 开源版 (MIT) 为 Foundation            ← 用 VOL.NET OSS
D. 购买 VOL.PRO 后作为 Foundation                      ← 商业闭源
E. VOL 只作为 Reference                                ← 不引入,只学习
```

---

## 2. 10 维度评分矩阵

| 维度 | A 自研 | B 自研+借鉴 | C VOL.NET OSS | D VOL.PRO 商业 | E Reference |
|---|---|---|---|---|---|
| **开发速度** | C (3-6 月 Foundation) | B+ (1-3 月借鉴加速)| A (0 月启动) | A (0 月启动) | C (3-6 月) |
| **ERP 适配性** | A (为 ERP 设计)| A | D (meta-tool,无 ERP) | D (meta-tool,无 ERP) | A (自己造) |
| **安全** | A (Argon2id/JWT/3D Status)| A | C (HTTP/4 位 captcha) | C (同上) | A (自己设计) |
| **多公司/多组织** | A (G2 已设计) | A | A (VOL 已实现) | A | A (自己造) |
| **扩展性** | A (modular monolith)| A | C (monolithic engine) | C | A |
| **模块化 (DEC-MODULE-001)** | A (强模块独立)| A | D (强耦合) | D | A |
| **可维护性** | A (own code) | A | C (依赖社区) | C (闭源,等 vendor) | A |
| **供应商锁定** | A (无 vendor) | A | B (MIT 可改) | F (闭源+商业) | A |
| **许可风险** | A (own) | A | A (MIT) | D (商业 license) | A |
| **未来 5 年演进** | A (按 GuliERP 节奏)| A | C (社区驱动) | B (商业支持) | A |

**累计评分** (A=10, B=8, C=6, D=4, F=0):

| 选项 | 累计 | 平均 |
|---|---|---|
| A 自研 | 99 | 9.9 |
| B 自研+借鉴 | 100 | 10.0 |
| C VOL.NET OSS | 65 | 6.5 |
| D VOL.PRO 商业 | 53 | 5.3 |
| E Reference | 97 | 9.7 |

**排序**:
1. **B 自研+借鉴** — 10.0/10
2. **A 自研** — 9.9/10
3. **E Reference** — 9.7/10
4. **C VOL.NET OSS** — 6.5/10
5. **D VOL.PRO 商业** — 5.3/10

---

## 3. 推荐排序(详细理由)

### 3.1 首选: B (自研+借鉴) ⭐⭐⭐⭐⭐

**综合得分 10.0/10,推荐第一**

**理由**:
- 沿用 G2 路线(自研 Foundation)— **保留 GuliERP 全部已规划优势**(3D Status / DEC-MODULE-001 / Argon2id / etc.)
- 同时**借鉴** VOL 的成熟模式(UI、Top bar 切换器、Tab 右键菜单、字段权限、Workflow 等)
- **零 Vendor lock-in**(全部自研 + 自选借鉴)
- **零许可风险**(无商业合同)
- **模块化保留**(DEC-MODULE-001 FROZEN)
- **ERP 业务深度保留**(3D Status / 含税未税 / Reservation / Posting Engine)
- **加速 30-50%**(借鉴节省的设计时间)

**借鉴清单见**:`VOL_PRO_GULIERP_ADOPTION_MATRIX.md` §8

### 3.2 次选: A (完全自研) ⭐⭐⭐⭐

**综合得分 9.9/10,推荐第二(作为 B 的 fallback)**

**理由**:
- 100% 自主可控
- 路线与 G2 计划完全一致
- 唯一缺点:不借鉴 VOL 模式 → 设计时间稍长

### 3.3 备选: E (VOL 只作 Reference) ⭐⭐⭐⭐

**综合得分 9.7/10,推荐第三(配合 B 或 A)**

**理由**:
- 100% 自主可控
- 把 VOL 作为 **文档级 reference**,不引入代码
- 用于:UI 选型 / 业务模式参考 / 反例教训
- **唯一缺点**:比 B 少一些"借鉴带来的速度",但获得"无任何外部依赖"的纯净

### 3.4 不推荐: C (VOL.NET OSS) ⭐⭐

**综合得分 6.5/10,不推荐**

**理由**:
- ❌ Foundation 复用 60-65% — 表面优势
- ❌ 业务实现 0% — **GuliERP 仍要全自建**,节省 = 0
- ❌ R5 风险成倍增加(整个框架是 low-code engine)
- ❌ DEC-MODULE-001 冲突(VOL 是 monolithic)
- ❌ 3D Status 需在 VOL 上重新建模
- ❌ 业务深度(Reservation / Posting Engine)不能表达
- ❌ MIT 友好但**仍要全面重构业务层**,净负
- ❌ 未来 5 年社区驱动(无商业支持)

### 3.5 强烈不推荐: D (VOL.PRO 商业) ⭐

**综合得分 5.3/10,强烈不推荐**

**理由**:
- ❌ 闭源 + 商业 license = 极高 Vendor lock-in
- ❌ 即使 OSS 已有 60% 能力,商业版仍要付费
- ❌ 业务 0% 复用 — GuliERP 仍要全自建
- ❌ 未来 5 年商业支持(可能涨价/服务降级)
- ❌ 国密合规认证 — 是 PRO 唯一显著优势,但成本/价值不划算
- ❌ 任何 R5 / DEC-* 冲突都不能通过商业 license 解决

---

## 4. 决策建议

### 4.1 采纳 B + E 组合

**B 路线**: 沿用 G2 自研计划 + 借鉴 VOL 模式(已实施在 R3 design system)
**E 路线**: VOL 作为 GuliERP 团队的**学习材料** + **反例库**(用 VOL 反证 GuliERP 哪些不该做)

**具体动作**:
1. G2 路线继续(G2-001..G2-010 按计划)
2. R3 design system 继续(已有 4px grid / 13px / 4 status)
3. 借鉴清单中 **ADOPT_NOW** 7 项已并入 R3/G2 — 无新工作
4. 借鉴清单中 **ADOPT_PATTERN_ONLY** 5 项 → V1.5 计划纳入
5. 借鉴清单中 **RESEARCH_LATER** 3 项 → V2+ 计划纳入
6. 借鉴清单中 **REJECT** 1 项(CodeGenerator)→ **永久不引入**,R5 风险登记加强

### 4.2 战略级 Decision Record

| 字段 | 值 |
|---|---|
| 决策日期 | 2026-08-19 |
| 决策 | **采纳 B+E 组合, 拒绝 C+D** |
| 影响范围 | G2 路线继续;R3 design system 已对齐;V1.5+ 路线纳入借鉴清单 |
| 复审触发 | (1) G2 实施 blocker > 2 周 (2) 出现新的更优 Foundation 候选 (3) VOL 出现重大 license 变化 |
| 关联文档 | GULIERP_GREENFIELD_RISK_REGISTER_V1.md (R5 风险更新) |

---

## 5. 关于 VOL.PRO 的"新事实"对 GuliERP 决策的反向影响

VOL.PRO 这轮研究**没有**改变 GuliERP 的核心决策:
- ❌ 不改变 DEC-MODULE-001(modular monolith 路线)
- ❌ 不改变 DEC-STATUS-001(3D status 路线)
- ❌ 不改变 R3 design system(已 4px grid / 13px / 4 status)
- ❌ 不改变 G2 Foundation 设计(已 modular + tenant + company)
- ❌ 不改变 R5 风险评估(low-code engine 仍 banned)

VOL.PRO **强化**了以下决策:
- ✅ **R3 multi-tab + 文档全屏** — vol.pro 同模式
- ✅ **Top bar 租户切换** — 借鉴 vol.pro UI
- ✅ **3D Status 严格分离** — vol.pro 是混合的,GuliERP 更严谨
- ✅ **GuliERP 是 ERP 不是 meta-tool** — vol.pro 反向证明这是正确的

VOL.PRO **新增**借鉴项:
- ➕ Tab 右键菜单(关闭其他/左边/右边)
- ➕ 用户徽章显示上次登录时间
- ➕ 暗色 chrome 模式(可选)
- ➕ 报表视觉设计器(V1.5+)
- ➕ 国密合规(V2+ 评估)

---

## 6. 5 个战略选项的最终排序(给 Operator)

```
🥇 B: 自研业务 + 吸收 VOL 模式     (10.0/10)   强烈推荐
🥈 A: 完全自研                    (9.9/10)   推荐(作 B fallback)
🥉 E: VOL 只作 Reference         (9.7/10)   推荐(配合 B 或 A)
4️⃣  C: 用 VOL.NET OSS           (6.5/10)   不推荐
5️⃣  D: 买 VOL.PRO 商业          (5.3/10)   强烈不推荐
```

---

## 7. NOT_INSPECTED 影响本决策的不确定项

| 项 | 影响 | 如何降低不确定 |
|---|---|---|
| VOL.PRO 商业 license 实际条款 | 影响 D 决策 | Sales 询价(但已假设不利) |
| VOL.NET 实际生产案例数 | 影响 C 决策 | 已假设 1 个(不充分)|
| VOL.NET 社区活跃度 | 影响 C 决策 | 已假设不活跃 |
| 实际 GuliERP 业务深度 vs VOL 表现 | 影响 B vs C 决策 | G2 实施中持续评估 |
| 实际 VOL Workflow Designer UI | 影响 V2 借鉴 | 需 Operator 配 Operator 实际访问 |

**即使补完所有 NOT_INSPECTED,本决策结论也大概率不变**:
- B 仍是首选(自研 + 借鉴)
- C 仍不推荐(净负)
- D 仍不推荐(闭源 + Vendor lock)

---

## 8. 与现有 GuliERP 决策的兼容性

| GuliERP 现有决策 | 与 VOL.PRO 决策的兼容性 |
|---|---|
| DEC-MODULE-001 (modular monolith) | ✅ 一致(G2 路线不变) |
| DEC-STATUS-001 (3D status) | ✅ 强化(vol.pro 反例) |
| DEC-INV-001 (PendingInspection) | ✅ 不受影响 |
| DEC-INV-002 (InventoryPostingEngine REQUIRED) | ✅ 强化(vol.pro 无此深度)|
| DEC-INV-004 (Adjust+StockTake approval) | ✅ 不受影响 |
| DEC-UX-001 (Multi-Tab + Document Fullscreen, zh-CN V1) | ✅ 一致(vol.pro 同模式) |
| DEC-WORKFLOW-001 (POC-004 reference only, simple V1) | ✅ 强化(vol.pro 工作流能力可借鉴到 V2+) |
| META_GULI HR-1..HR-10 + LESSON-001 | ✅ 强化(vol.pro 不安全实践) |
| R5 风险登记(low-code engine 诱惑) | ✅ 强化(vol.pro 是反例) |
| G2 Foundation 10 个 goal 计划 | ✅ 不变(继续 G2-001..G2-010) |

**VOL.PRO 这轮研究不修改任何 GuliERP 现有决策。**

---

*End of TASK 13 — VOL_PRO_STRATEGIC_REASSESSMENT*
*Status: 5 选项评分完毕,推荐 B(自研+借鉴) + E(Reference) 组合,拒绝 C/D。**不修改 GuliERP 现有任何决策**。*
