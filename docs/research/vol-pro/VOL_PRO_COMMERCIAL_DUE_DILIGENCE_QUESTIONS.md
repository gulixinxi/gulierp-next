# VOL_PRO_COMMERCIAL_DUE_DILIGENCE_QUESTIONS

| Field | Value |
|---|---|
| Goal | VOL-PRO-002 TASK 6 — 商业 Due Diligence 问题清单(给 Operator 询问 VOL 销售用) |
| Researcher | Mavis (single writer, read-only) |
| Date | 2026-08-19 (Asia/Taipei) |
| Total questions | 15 |
| Output type | 给 Operator 在与 VOL 销售接触时直接使用 |

---

## 1. 用途

本文件用于**Operator 与 VOL 销售首次正式接触**时一次性问完所有 15 个商业问题。每问都对应一个 GuliERP 决策点,**没问到 = 没法判断**。禁止 Operator 自由发挥或自行回答 — 所有回答必须来自 VOL 销售并以书面形式(邮件 / 合同条款)确认。

**绝对禁止 Mavis 自行猜测**:
- 价格 / 续费 / 退款政策 / 二次销售条款 / 商业 License 数量
- 5000 元具体版本的 License 范围
- 升级路线 / 售后支持响应时间

**VOL.PRO OSS 已是 MIT 验证**(VOL-PRO-001 确认 `github.com/cq-panda/Vue.NetCore` MIT License,1.4k forks)。但**商业版 License 必须销售书面确认** — MIT 仅覆盖 OSS 代码,商业版增强代码(AK AI 模块 / Print Designer / Workflow Designer / Print Designer / 国密等)不在 MIT 范围。

---

## 2. 15 个核心问题

### Q1. 5000 元具体版本的范围
"5000 元买到的版本具体包含哪些源代码、模块、功能?是否有书面功能清单?"

**为什么必须问**:5000 元是 marketing 宣传数字,商业上**几乎一定不包含 PRO 全部源代码**。需要销售出示书面功能清单,逐项核对是否含:
- ✅ 多 ORM (EF Core 8 + SqlSugar)
- ✅ 多 DB (SqlServer / MySql / PGSql / Oracle / 达梦)
- ⚠️ AI 智能表单 / AI 智能建表 / AI 辅助开发 / AI 辅助生成(15+ AI 卡)
- ⚠️ 工作流设计器(Visual Designer)
- ⚠️ 打印设计器
- ⚠️ 代码生成器(Code Generator)
- ⚠️ 表单设计器(Visual Form Designer)
- ⚠️ 大屏设计器
- ⚠️ 国密算法
- ⚠️ 移动端 uniapp 完整源码

### Q2. 完整源码范围
"5000 元版本是否包含 **VOL.NET 后端 + 前端 + 移动端**的**全部源代码**?是否包括 .csproj / .vue / package.json / docker-compose / 部署脚本?"

**为什么必须问**:"源码"定义模糊 — 是仅 framework 内核、还是包含 demo 业务、还是包含完整 OSS 复制 + PRO 增强。需要销售出示**源码目录树**。

### Q3. PRO-only 源码
"PRO 版相比 VOL.NET OSS 多了哪些**专有源码**?这些专有代码是闭源 binary(DLL)还是开放源码?是否允许修改?"

**为什么必须问**:
- 闭源 DLL = vendor lock-in 严重,无法 debug,无法 patch
- 开放源码 = 可借鉴可改造,GuliERP G2 路线可行
- 若 AI / Print Designer / 国密 是闭源,**GuliERP V1 仍可基于 OSS 部分自研 AI / Print / 国密**(参考 VOL.NET 公开 API)

### Q4. 代码生成器源码
"代码生成器(CodeGen / VolCodeBuilder) 是否包含在 5000 元版本?生成器本身代码是否开放?生成产物(generated code)是否允许修改?重新生成时是否覆盖业务代码?"

**为什么必须问**:
- VOL-PRO-001 已确认 CodeGen 是 GuliERP 决策的 REJECT 项(R5 风险 + DEC-MODULE-001 冲突)
- 但若销售宣称"已含 CodeGen",需要明确其能力范围 + 业务扩展机制
- partial class 模式 / service extension / hook / event 机制是否就绪?

### Q5. 授权项目数
"5000 元购买后,可以在多少个项目中部署?同一个公司不同子公司算几个?同一个项目不同环境(dev / staging / prod)算几个?"

**为什么必须问**:
- ERP 系统常涉及多公司 / 多工厂 / 多环境
- 某些 vendor 按项目 / 按 instance / 按公司收费
- 若仅 1 个项目,GuliERP 这种多公司 ERP 立即不适用

### Q6. 授权客户数
"5000 元购买后,系统内可以注册多少个最终用户?并发数限制?API 调用次数限制?"

**为什么必须问**:
- ERP 经常 50-500 用户
- 商业版常按 user / concurrent user 收费
- "无限用户" 通常是 marketing 措辞,需销售书面确认

### Q7. 商业交付权利
"基于 5000 元版本二次开发的产品,是否可以**转售**给第三方客户?是否需要向 VOL 支付 royalty?是否需要公开 VOL 版权?"

**为什么必须问**:
- GuliERP 不一定是终端用户,可能做 OEM / SaaS 服务
- MIT OSS 允许商业转售(MIT 条款),但 PRO 增强代码可能有额外限制
- 版权 attribution 需求(是否需要在 About 页面写"Powered by VOL.PRO")

### Q8. 二次销售(转售 + SaaS 化)
"如果基于 5000 元版本开发 SaaS 平台对外提供服务,是否需要额外付费?是否限制 SaaS 租户数量?"

**为什么必须问**:
- SaaS 化 = 多租户对外提供服务,与 Q5 不同
- 商业版常禁止直接转售或要求分账

### Q9. 版权要求
"5000 元版本是否需要在所有页面 / 文档 / About 页面保留 VOL 版权信息?是否可以修改版权页?是否可以去除 'Powered by VOL'?"

**为什么必须问**:
- 客户常要求去除 vendor 品牌
- ERP 系统常需定制 About 页面
- 版权要求影响 UI 自定义

### Q10. 升级政策
"购买后 1 年内 / 3 年内,是否可以免费升级到新版本?升级是否包含 PRO 新功能?升级是否需要重新购买?"

**为什么必须问**:
- ERP 经常 5-10 年使用
- 升级费用累积可能超过首次购买
- 部分 vendor 升级需重新购买 = vendor lock

### Q11. 售后支持
"5000 元版本包含多久的售后支持?支持方式(电话 / 邮件 / 工单 / 远程)?响应时间 SLA?是否提供定制开发服务?"

**为什么必须问**:
- ERP 系统常需要 vendor 支持解决底层问题
- 部分商业版只卖源码不提供支持
- 远程调试 / 二开培训 是否含

### Q12. 源码更新机制
"PRO 后续出新功能 / Bug 修复 / 安全补丁,如何同步给 5000 元客户?是否提供 Git 私有仓库访问?还是只发新版本压缩包?"

**为什么必须问**:
- ERP 系统常需快速响应 CVE / Bug
- 私有 Git 仓库 vs 压缩包 = 集成难度差异巨大
- 5 年后是否还能拿到源码更新 = 系统生命周期决定性

### Q13. 多人开发授权
"如果 GuliERP 团队有 N 个开发人员需要看 / 改 / 部署 5000 元版本源码,是否需要按人头付费?"

**为什么必须问**:
- GuliERP 团队 5-10 人
- 部分 vendor 按 developer seat 收费
- 源码阅读权 vs 修改权 vs 部署权可能分开

### Q14. 未来版本路线
"VOL 未来 2 年的产品路线是什么?PRO 是否会拆成多个 SKU(基础 / AI / Print / 国密)?5000 元版本是否会被淘汰?"

**为什么必须问**:
- ERP 决策影响 5-10 年
- 若 5000 元版本 1 年后被淘汰,等于买了 stop-ware
- vendor 可能为推新版故意限缩旧版支持

### Q15. 退款 / 试用政策
"5000 元版本是否提供退款?是否提供试用?试用版是否功能受限?试用版到期后数据如何处理?"

**为什么必须问**:
- ERP 决策金额大,需 PoC 验证
- 试用版是否功能完整 vs 限制
- 数据迁移 / 导出的承诺

---

## 3. 商业决策矩阵(基于回答打分)

| 维度 | 关键问题 | GuliERP 决策影响 |
|---|---|---|
| **可商用性** | Q1, Q2, Q3 | 决定是否走 D 路线 |
| **可扩展性** | Q3, Q4, Q13 | 决定是否走 C 路线 |
| **Vendor Lock** | Q9, Q10, Q12, Q14 | 决定 vendor lock 风险等级 |
| **TCO 估算** | Q1, Q5, Q6, Q10, Q11, Q13 | 5 年总成本 |
| **转售权利** | Q7, Q8 | 决定 GuliERP 商业模式 |
| **长期可用性** | Q10, Q12, Q14 | 决定 5-10 年产品生命周期 |

---

## 4. 决策 Gate(基于回答)

| 销售回答模式 | GuliERP 决策 |
|---|---|
| Q1-Q15 全部书面承诺,无闭源 binary,无用户数限制,转售免 royalty,免费升级 3+ 年 | **可走 D 路线重新评估** |
| Q3 说有闭源 binary(AI / Print Designer / 国密),Q4 CodeGen 闭源 | **D 路线拒绝,继续 B+E** |
| Q5 / Q6 有项目数 / 用户数硬限制, 且限制数 ≪ GuliERP 业务需求 | **D 路线拒绝** |
| Q9 要求保留 vendor 版权, 且客户业务要求隐藏 vendor | **D 路线拒绝** |
| Q12 仅压缩包更新,无 Git 仓库,5 年后停止支持 | **D 路线拒绝(vendor lock 风险高)** |

**任一项不利回答 → D 路线拒绝 → 维持 B+E**。

---

## 5. NOT_INFERABLE 标注

以下问题**Mavis 不能猜**,必须 Operator 询问销售并书面确认:

- Q1 5000 元具体功能清单(必须销售出示)
- Q5 / Q6 项目数 / 用户数实际限制(必须合同)
- Q7 / Q8 商业转售权利(必须合同)
- Q9 版权要求细节(必须合同)
- Q10 / Q12 / Q14 升级 / 更新 / 路线(必须销售确认 + 合同条款)
- Q11 售后 SLA(必须合同 SLA 条款)
- Q13 developer seat 政策(必须合同)
- Q15 退款 / 试用政策(必须合同)

**总问题数**:15
**不能推断的问题数**:13 (Q3, Q4, Q11 可基于 VOL.NET 公开信息推断)

---

## 6. 期望产出(Operator 端)

Operator 询问销售后,请将回答整理成:

```markdown
## VOL 销售回答记录

**销售姓名**:[   ]
**公司**:[   ]
**回答日期**:[   ]
**回答形式**:[邮件 / 会议 / 微信 / 合同]
**Q1**:[   ]
**Q2**:[   ]
...
**Q15**:[   ]

## 总结
- D 路线可行? [是/否/部分]
- 风险点: [   ]
- 下一步: [   ]
```

完成后回填到 `docs/research/vol-pro/VOL_PRO_COMMERCIAL_DUE_DILIGENCE_ANSWERS.md`(Operator 端另写)。

---

## 7. 后续行动

1. **Operator 安排销售接触** — 1 次会议或邮件
2. **Mavis 不会自行联系销售** — 越权
3. **Mavis 不主动催 Operator** — 决策权在 Operator
4. **基于回答,GuliERP G2 路线**:
   - 全部有利 → 升级评估 D 路线
   - 任一不利 → 维持 B+E 路线,本文件归档为"商业 Due Diligence 备查"

---

*End of VOL_PRO_COMMERCIAL_DUE_DILIGENCE_QUESTIONS*
*Status: 15 questions ready for Operator*
*Mavis 不推断商业细节,等待销售书面回答*
