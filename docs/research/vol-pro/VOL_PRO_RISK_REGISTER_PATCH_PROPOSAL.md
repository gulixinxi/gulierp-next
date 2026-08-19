# VOL_PRO_RISK_REGISTER_PATCH_PROPOSAL

| Field | Value |
|---|---|
| Goal | VOL-PRO-002 TASK 8 — Risk Register R13 Patch 提案(不直接修改 R1-R12 现有 RAG) |
| Researcher | Mavis (single writer, read-only proposal) |
| Date | 2026-08-19 (Asia/Taipei) |
| Target | `docs/governance/GULIERP_GREENFIELD_RISK_REGISTER_V1.md` |
| Proposal status | **PROPOSAL_ONLY** — 等待 Operator 决定是否合并到 V1 正式版 |
| R13 主题 | **Mature Platform Reinvention Risk** |

---

## 1. 提案背景

VOL-PRO-001 + VOL-PRO-002 系统性验证了 VOL.NET (MIT) + VOL.PRO (商业) 实际能力。**核心发现**:

- VOL.NET 是 MIT 开源 1.4k forks,.NET 8 + EF Core 8 + SqlSugar + JWT,2018 年开始活跃维护
- VOL.PRO 是商业增强,在 VOL.NET 基础上加 AI / Print Designer / Workflow Designer / 国密
- VOL.PRO 是 **meta-tool**(造 ERP 的工具),不是 ERP,**0 业务模块**
- VOL.NET OSS 已成熟到"可作为 Foundation 候选"

**这一证据暴露 GuliERP 当前 R1-R12 Risk Register 缺失一个关键风险**:

> **风险 R13**:团队 / Operator 在 GuliERP 进度受阻时,可能产生"不如用 VOL 算了"的想法;若决策切换,**等同于推翻 DEC-MODULE-001 / DEC-STATUS-001 / R3 / G2 全部现有决策**。

此风险在 VOL-PRO-001 之前是"潜在",**VOL-PRO-001 + VOL-PRO-002 验证后变成"现实"**。

---

## 2. R13 风险定义(提案)

### 2.1 Risk Statement(风险陈述)

> 在 GuliERP G2 Foundation 实施阶段,因以下任一原因,Operator / 团队可能产生"重评估为 VOL Foundation"的诱惑:
> 1. G2 Foundation 实施工作量超出预期
> 2. GuliERP 关键能力(User / Role / Permission / Audit / Dictionary)重复造轮子
> 3. VOL.PRO 商业营销(54 home cards)强烈引导尝试
> 4. 团队对 GuliERP 模块独立性 / 长期路线信心不足
>
> 一旦切换,**等同于推翻 R1-R12 全部 RAG 决策**,GuliERP 重启 6-12 个月。

### 2.2 Likelihood(可能性)

**HIGH** — VOL-PRO-001 决策矩阵中 B+E(自研 + Reference)是维持路线,但 R5 风险登记已把"low-code engine 诱惑"列为强化项。R13 在 VOL.NET MIT 商业可行性确认后,**可能性从 LOW 升为 HIGH**。

### 2.3 Impact(影响)

**CATASTROPHIC** — 切换到 VOL Foundation 等同于:
- 推翻 DEC-MODULE-001(modular monolith) → VOL 是 monolithic
- 推翻 DEC-STATUS-001(3D 状态) → VOL 用 9-string
- 推翻 R3 Design System → VOL 用 Element Plus
- 推翻 G2 Foundation 10 计划 → 改为适配 VOL API
- 6-12 个月返工
- 业务团队信心打击
- Operator / 团队时间成本

### 2.4 Detection(检测)

| 检测信号 | 阈值 | 检测方法 |
|---|---|---|
| Operator 主动询问"要不要试试 VOL" | ≥ 1 次 | 会话日志 |
| G2 实施时间表延后 | > 1 周 | G2 进度 vs 计划 |
| 团队成员 / 外部建议"用现成方案" | ≥ 1 次 | 会议纪要 |
| VOL 销售主动联系 Operator | ≥ 1 次 | Operator 反馈 |
| R3 / G2 路线被质疑 | ≥ 1 次 | Operator 反馈 |

### 2.5 Mitigation(缓解)

| 缓解措施 | 负责 | 时机 |
|---|---|---|
| 锁定 G2 路线不重启 | Operator | 立即 |
| R13 加入正式 RAG | Operator | 本周 |
| 商业 Due Diligence 启动(15 问) | Operator | 立即 |
| VOL.NET 仅作"Pattern Reference"使用,不改 GuliERP 决策 | Mavis | 持续 |
| G2 实施每 4 周审计:进度 vs R3 / DEC-MODULE-001 | Mavis | 持续 |
| 拒绝基于 marketing card 的借鉴决策 | Mavis | 持续 |

### 2.6 Hard Stop(硬停止条件)

**以下任一条件触发 → 立即 STOP G2,Operator 强制重评**:

1. Operator 决定走 C 路线(以 VOL.NET OSS 为 Foundation) → 立即 STOP,启动 G3 重新规划
2. Operator 决定走 D 路线(买 VOL.PRO 为 Foundation) → 立即 STOP,启动 G3 重新规划
3. G2 实施连续 8 周无可见产出(0 文档 / 0 代码) → 强制重评是否资源不够
4. 出现"VOL.NET 闭源 binary 必需"决策点(无法避开) → 立即 STOP

**触发任一 Hard Stop → 不得在 Mavis 自主下重启动,需 Operator 显式确认**。

---

## 3. R13 与 R1-R12 的关系

| 现有 Risk | 与 R13 关系 |
|---|---|
| R5 low-code engine 诱惑 | R5 已识别 low-code 风险。R13 升级为"现实诱惑" |
| R6 框架选型反复 | R6 是"切框架"风险。R13 是"切 Foundation 整体"风险,粒度更大 |
| R7 跨团队决策不一致 | R7 是横向协调。R13 是单点 Operator 决策(更严重) |
| R10 厂商锁定 | R10 是 OSS 锁。R13 是商业锁 + 整体路线锁 |

**R13 不替代 R1-R12,是叠加关系**。R13 是"特定路径"(mature platform 整体切换)的精准风险。

---

## 4. 提案的两种合并方式(Operator 决定)

### 方式 A:独立 R13(推荐)
- R1-R12 保持不变
- 新增 R13 = Mature Platform Reinvention Risk
- 优点:风险定义精准,不影响现有 12 个 risk 决策
- 缺点:RAG 文件从 R12 → R13,版本号升 V2

### 方式 B:归入 R5(low-code engine 诱惑)
- R5 加一段"R5a: 整体 Foundation 切换诱惑"
- R5 升级为 R5a + R5b + R5c(多子风险)
- 优点:风险集中管理
- 缺点:R5 变得复杂,需要重新评审

**Mavis 建议方式 A**(R13 独立),理由是 R13 影响范围(G2 全部)与 R5(low-code partial)粒度差异过大。

---

## 5. 提案不直接修改的原因

本提案**只创建独立文件,不直接修改** `GULIERP_GREENFIELD_RISK_REGISTER_V1.md`,原因:

1. R1-R12 是 Operator 多次 approved 的冻结决策
2. 合并 R13 是 governance 决策,需 Operator 显式确认
3. 提案作为 evidence pack 备查,Operator 可对比后决定
4. 任何 Mavis 自主合并 = 越权

**Operator 决定合并方式后**,Mavis 会按 Operator 指示合并到 V1(或 V2)RAG。

---

## 6. 提案合并的预计工作量

- 若方式 A:5 min(直接 append R13 到 R12 后)
- 若方式 B:20 min(改写 R5,加入 R5a/R5b/R5c,评审整个 R5 章节)
- 不合并:0 min(本文件作 evidence pack 备查)

---

## 7. R13 内容预览(若 Operator 选择方式 A)

```markdown
## R13 — Mature Platform Reinvention Risk

| Field | Value |
|---|---|---|
| Risk | 团队 / Operator 在 GuliERP 进度受阻或受外部营销影响,产生"重评估为 VOL Foundation"的诱惑,等同推翻 R1-R12 全部决策 |
| Likelihood | HIGH(VOL-PRO-001 + VOL-PRO-002 验证后升级) |
| Impact | CATASTROPHIC(6-12 个月返工 + DEC-MODULE-001 / DEC-STATUS-001 / R3 / G2 全部推翻) |
| Detection | Operator 主动询问"试试 VOL" / G2 延后 > 1 周 / VOL 销售主动联系 / R3 或 G2 路线被质疑 ≥ 1 次 |
| Mitigation | (1) 锁定 G2 路线不重启 (2) R13 入正式 RAG (3) 商业 Due Diligence 启动 15 问 (4) VOL.NET 仅作 Pattern Reference (5) G2 实施每 4 周审计 |
| Hard Stop | (1) Operator 决定 C 路线 → STOP (2) Operator 决定 D 路线 → STOP (3) G2 连续 8 周无产出 → 强制重评 (4) VOL.NET 闭源 binary 必需 → STOP |
| Owner | Operator |
| Created | 2026-08-19 |
| Source | VOL-PRO-001 + VOL-PRO-002 实证 |
| Status | PROPOSAL — 待 Operator 合并批准 |
```

---

## 8. 总结

**R13 风险已实证存在**,不可忽略:
- VOL.NET MIT + 1.4k forks 商业可用性 → 切换阻力大幅降低
- VOL.PRO 54 home cards 营销 → 持续干扰
- 6-12 个月 G2 路线实施窗口 → 暴露期长
- 团队对长期路线信心依赖 → 决策点不可控

**Operator 行动**:
1. 决定 R13 是否合并到 RAG(方式 A 或 B)
2. 决定 Hard Stop 条件是否过严(8 周 vs 12 周)
3. 决定是否启动商业 Due Diligence(TASK 6,15 问)

**Mavis 不主动合并**,等待 Operator 显式指示。

---

*End of VOL_PRO_RISK_REGISTER_PATCH_PROPOSAL*
*Status: PROPOSAL_ONLY — R13 提案待 Operator 合并批准*
*不直接修改 R1-R12 现有决策*
