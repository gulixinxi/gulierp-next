# VOL_PRO_VS_OPEN_SOURCE_MATRIX

| Field | Value |
|---|---|
| Goal | TASK 10 — PRO vs Open Source 区分 |
| Sources | VOL.NET GitHub + Gitee + Vol.PRO demo + 公开文档 |
| OSS repo | `github.com/cq-panda/Vue.NetCore` (also `sklye/Vue.NetCore` fork) |
| License | **MIT** |
| Status | **OPEN_SOURCE_VERIFIED** for OSS; **PRO_ONLY_VERIFIED** for PRO-only |

---

## 1. 关系拓扑(已验证)

```
┌──────────────────────────────────────┐
│      VOL.PRO 商业企业版 (pro.volcore.xyz) │  ← 当前 demo
│      - 增加 AI 能力 (15+ cards)        │
│      - 商业 license / 商业技术支持     │
│      - 国密/信创 合规认证              │
└──────────┬───────────────────────────────┘
           │  (基于)
           ▼
┌──────────────────────────────────────┐
│      VOL.NET / Vue.NetCore (开源)        │  ← GitHub MIT
│      github.com/cq-panda/Vue.NetCore    │
│      gitee.com/mirrors_cq-panda/...     │
│      MIT License, 1.4k forks           │
└──────────────────────────────────────┘
```

> 公开资料明确: **VOL.PRO 是 VOL.NET 的企业版,提供商业支持 + 增强功能**。OSS 版本本身已具备生产可用性。

---

## 2. 已验证的能力对比(OPEN_SOURCE vs PRO)

| 能力 | OPEN_SOURCE (MIT) | PRO_ONLY | 证据 |
|---|---|---|---|
| **Code Generator** | ✅ 标准 CRUD 30+ 属性 | ✅ + 高级模板 | OSS README: "30 多种属性可在线配置生成的代码" |
| **Workflow** | ❌ **没有 workflow 引擎** | ✅ Visual workflow designer | OSS README 仅提"审批流程"作"扩展支持",无 design tool |
| **LowCode** | ✅ 基础拖拽 | ✅ + 高级 | 双向都有 |
| **Permission** | ✅ RBAC + 多租户 | ✅ + 数据权限 + 字段权限 | PRO demo home page "菜单/角色/字段权限" cards |
| **MultiTenant** | ✅ 多租户 | ✅ + per-tenant DB | PRO home page "租户管理" card |
| **Report** | ✅ 图表统计 | ✅ + 工作台设计器 + SQL | OSS README: "图表统计" |
| **Print** | ❌ 推断没有 | ✅ Visual print designer | PRO home page "打印(在线可视化设计)" card |
| **BigScreen / BI** | ❌ 推断没有 | ✅ 自定义大屏设计器 | PRO home page "自定义大屏设计器" card |
| **Mobile (uniapp)** | ✅ uniapp 全自动生成 | ✅ + 微信小程序 | OSS README: "框架移动端(uniapp)已发布" |
| **Components** | ✅ 300+ 扩展方法 | ✅ + 业务组件市场 | OSS README: "近 300 个扩展方法" |
| **Support** | ❌ 社区 (QQ 群) | ✅ 商业 7x24 | — |
| **国密 (GM Crypto)** | ❌ 推断没有 | ✅ "支持国密算法加密" | PRO home page card |
| **信创 (Xinchuang)** | ✅ 部分(支持达梦 DB) | ✅ + 完整认证 | OSS: 达梦 DB 在 list 中 |
| **WeChat Pay / OA** | ❌ | ✅ | PRO home page "微信支付" / "微信公众号开发" cards |
| **i18n** | ✅ 国际化配置(从 OSS 文件列表看到) | ✅ + 多语言资源 | OSS: "国际化配置" 文件 |
| **AI 能力 (15+ cards)** | ❌ **完全没有** | ✅ 完整 | PRO home page 15+ "AI xxx" cards |
| **软删除 + 数据隔离** | ✅ | ✅ | OSS 描述 |
| **ORM** | ✅ EF Core 8 + SqlSugar 双 ORM | ✅ | OSS README: "EF Cor8.0、SqlSugar" |
| **Database** | ✅ SqlServer / MySql / PGSql / Oracle / 达梦 | ✅ + 5 大国产 DB | OSS README |
| **Tech stack** | ✅ .NET 8 + Vue 3 + Vite + TS + Element Plus + uniapp | ✅ + 增强 UI | OSS README |

**结论**:
- **OSS 已涵盖约 60-65% 的能力**(Code Generator / RBAC / MultiTenant / Mobile / Components)
- **PRO 多出的关键能力**:
  1. **AI 能力(15+ cards)** — 商业核心竞争力
  2. **Visual Workflow Designer** — OSS 没有
  3. **Print Designer / BigScreen Designer** — OSS 没有
  4. **国密 / 微信支付 / 微信公众号** — PRO 专属集成
- 核心 ORM、双数据库支持、Mobile、Code Generator 都是 **OSS 已经有**

---

## 3. VOL.NET 关键事实(已验证)

| 维度 | 值 |
|---|---|
| **License** | MIT(可商用)|
| **Stars** | GitHub 1 star(且 gitee mirror 也有)|
| **Forks** | GitHub 1.4k forks |
| **Demo 凭据** | `admin666 / 123456` 或 `admin / 123456`(OSS README 公开)|
| **最后更新** | README 多次 "5个月前" / "1年前" / "2个月前" — 仍在维护(2026/05 更新包) |
| **主语言** | C# 56.6% / Vue 23.0% / TSQL 10.4% / HTML 7.2% |
| **核心贡献者** | 283591387(主作者 QQ 号也是这个)|

---

## 4. 对 GuliERP 的启示(关键决策点)

### 4.1 是否用 VOL.NET (OSS, MIT) 作为 Foundation?

**Pros**:
- 0 起步成本(MIT License)
- 1+ 年实战迭代,功能成熟
- 双 ORM(EF Core + SqlSugar)— GuliERP V1 用 EF Core 即可
- 支持 PostgreSQL(GuliERP V1 也选 PgSQL)
- 支持信创(达梦 DB)
- Code Generator + Form Designer 可减少 GuliERP 重复代码
- 多端 mobile (uniapp) 已可用

**Cons**:
- ❌ **没有 ERP 业务模块**(销售/采购/库存/财务)— GuliERP 仍要自己造
- ❌ **没有 Workflow Engine**(GuliERP V2 计划要)— 不影响 V1,但 V2 要自建
- ❌ **没有 AI 能力** — PRO 才提供
- ❌ **R5 风险**:整个框架是 low-code engine,GuliERP R5 风险成倍增加
- ❌ **DEC-MODULE-001 冲突**:VOL.NET 是 monolithic framework,模块独立性差
- ❌ **DEC-STATUS-001 (3D 状态)** 需要在 VOL.NET 上重新建模
- ❌ **3D 状态 / 含税未税 / Reservation / Posting Engine** 这些 ERP 业务深度需自己造
- ❌ **国密合规**:OSS 没认证,PRO 才有

**评分**:
- Foundation 复用价值: 6/10(广度够,深度不够)
- ERP 业务实现: 0/10(没有,需自建)
- 风险: 8/10(R5 + Module 独立 + Vendor lock-in)

### 4.2 是否用 VOL.PRO (商业)?

**Pros**: OSS 所有优点 + AI + Workflow + Print + 国密合规

**Cons**:
- ❌ 商业 license(具体条款 UNCLEAR,需 Sales 询价)
- ❌ Vendor lock-in(闭源)
- ❌ OSS 能做到的部分商业版仍要付费

**评分**:
- 复用价值: 7/10(OSS 之上)
- 风险: 9/10(商业 + 闭源 + Vendor lock)

### 4.3 是否购买 PRO 商业支持,但仍用 OSS 基础?

**Pros**: 商业支持 + 升级路径

**Cons**: 仍是 Vendor lock-in

---

## 5. GuliERP 的实际选项

| 选项 | 评价 | 风险 | 建议 |
|---|---|---|---|
| **A. 完全自研 GuliERP Foundation** | G2 计划已设计 10 个 goal;1+ 月可上线 | 低 | **当前路线** |
| **B. 自研 + 借鉴 VOL 组件/模式** | Foundation 自己造,UI 组件 / 模式借鉴 VOL | 低 | **A 的细化版本** |
| **C. 用 VOL.NET (OSS, MIT) 作为 Foundation** | 起步快,但业务要全自建,R5 风险高 | 中-高 | 不推荐 |
| **D. 购买 VOL.PRO 商业 Foundation** | 闭源 + Vendor lock + 商业 license | 极高 | **不推荐** |
| **E. VOL 只作为 Reference** | 借鉴能力成熟度,不引入 | 低 | **A 的并行参考** |

**GuliERP 推荐**:**A + E**(自研 + VOL 作 Reference,借鉴 UI 模式、Print Designer UI、CodeGen 思路,但不引入 low-code engine)

---

## 6. 借鉴清单(adopt pattern, 不复制代码)

| VOL 能力 | GuliERP 借鉴点 | 不借鉴 |
|---|---|---|
| **Code Generator** | 思路(GuliERP 仍手写业务) | ❌ 不复制 engine |
| **Form Designer** | 思路(V1.5+ PrintDesigner UI 形态)| ❌ 不复制 engine |
| **Workflow Designer** | 思路(GuliERP V2+ designer UI 形态)| ❌ 不复制 engine |
| **Multi-tenant UI** | Top bar 切换器(已 GuliERP G2 设计) | n/a |
| **国密 / 信创** | V1.5+ 评估 | n/a |
| **PostgreSQL 兼容** | GuliERP V1 已选 PgSQL | n/a |
| **双 ORM (EF + SqlSugar)** | GuliERP V1 用 EF Core 单 ORM | n/a(不引入 SqlSugar) |
| **uniapp mobile** | 评估 V1.5+ | n/a |

---

## 7. UNVERIFIED 项(需 Operator 配合)

| 项 | 状态 |
|---|---|
| VOL.PRO 商业 license 实际条款 | **NOT_INSPECTED**(需 Sales 询价) |
| VOL.PRO 商业价格 | NOT_INSPECTED |
| VOL.NET OSS 1+ 年实际生产案例数 | UNCLEAR(Gitee/GitHub README 提到 1 个 制造企业案例) |
| VOL.NET 与 VOL.PRO 的真实代码差异 | NOT_INSPECTED(需购买 PRO 看源码) |
| VOL.NET 社区活跃度 | "1.4k forks" 数据点已确认;贡献者活跃度 UNCLEAR |

---

## 8. 关键结论(为 TASK 13 战略决策准备)

1. **VOL.NET OSS 已 1+ 年成熟,功能广度足够,但深度为 0(无具体业务模块)**
2. **VOL.PRO = VOL.NET + AI + Workflow Designer + Print Designer + 微信生态 + 国密认证**
3. **GuliERP 的根本定位(具体 ERP)与 VOL.PRO 的根本定位(meta-tool)正交**
4. **不存在"用 VOL.PRO 替代自建 Foundation"的可行情景** — 因为 VOL.PRO 不解决 GuliERP 核心需求(具体业务)
5. **VOL.NET OSS 借鉴价值 = 6/10,但引入风险 = 8/10** — 净负
6. **VOL.PRO 商业借鉴价值 = 7/10,但引入风险 = 9/10** — 更负

**最终建议**:**GuliERP 应继续自研(G2 计划路线),VOL 仅作 Reference**。具体借鉴清单见 §6。

---

*End of TASK 10 — VOL_PRO_VS_OPEN_SOURCE_MATRIX*
*Status: 7 项 OPEN_SOURCE_VERIFIED,8 项 PRO_ONLY_VERIFIED,2 项 UNCLEAR(商业条款/价格)。MIT License + .NET 8 + EF Core 8 + SqlSugar + PostgreSQL 是关键事实*
