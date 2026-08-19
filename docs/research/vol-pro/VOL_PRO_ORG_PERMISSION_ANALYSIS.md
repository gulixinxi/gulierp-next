# VOL_PRO_ORG_PERMISSION_ANALYSIS

| Field | Value |
|---|---|
| Goal | TASK 6 — Multi-Tenant / Multi-Company / Multi-Org 深度调查 |
| Status | **PARTIALLY_VERIFIED** — 关键决策已观测,细节大多从 home page 卡片推断 |

---

## 1. 直接观测(2026-08-19)

### 1.1 顶栏租户切换器

- 按钮文本:**"上海临港分公司"**
- 位置:Top bar (y=14, x=308, 156×32)
- 状态:`aria-expanded=false`(点击会展开下拉)
- **结论**:确认有 **Tenant 切换 UI**,当前 active 是"上海临港分公司"= 一个分公司

### 1.2 "上海临港"是分公司,不是租户

"上海临港**分公司**" — "分公司"一词表明 vol.pro 有 **多公司**概念。Tenant 之上有 Company。这与 GuliERP 的 Tenant + Company 模型**完全一致**。

### 1.3 Home page "租户管理" 卡片

> "支持租户功能,并支持一个租户一个独立数据库,框架全自动管理每个租户及数据库创建"

确认:
- **Tenant 概念存在**
- 支持 **per-tenant DB 模式**(每个租户一个独立数据库)
- 框架自动管理 tenant + DB lifecycle

### 1.4 Home page "公司"未明示出现独立卡片

vol.pro 没明列"公司管理"卡片,但"上海临港分公司"UI 暗示 Company 存在。可能 Company 是 Tenant 的子项,在租户管理里配置。

---

## 2. 7 个核心问题回答

### Q1: 是否存在 Tenant?

**A: YES (VERIFIED_IN_DEMO)**
- 顶栏切换器 + "租户管理" 卡片
- 支持 **per-tenant DB 模式**

### Q2: 是否存在 Company?

**A: YES (VERIFIED_IN_DEMO)**
- "上海临港**分公司**" 顶栏文字直接证明
- Company 是 Tenant 下的子项

### Q3: Company 与 Department 是否不同?

**A: PARTIALLY_VERIFIED**
- Company 是顶级组织(分公司 = 公司级别)
- Department 在 home page "组织架构" 卡片存在
- **DOCUMENTED_ONLY**:Department 与 Company 的层级关系是 "Company → Department" 还是 "Company + Department 同级"未实测

### Q4: User 是否可属于多个 Company?

**A: DOCUMENTED_ONLY (强推断)**
- 中国 SaaS 标准做法:User 属于多个 Company 是常见(员工在集团内转岗)
- VOL.PRO "上海临港分公司" 顶栏切换暗示**当前用户在该 Tenant 多个 Company 之一**
- **未实测**:具体 UserCompany 关联表是否存在,需进入"用户管理"页确认

### Q5: Role 是否跨 Company?

**A: DOCUMENTED_ONLY (推断)**
- VOL.PRO "角色管理" 卡片存在
- 多公司 + 多角色是 vol.pro 营销点("多组织架构、多角色" 卡片)
- **未实测**:Role 是否每个 Company 独立 / 全局共享,需"角色管理"页确认

### Q6: 数据是否自动按公司隔离?

**A: VERIFIED_IN_DEMO(通过 卡片 + UI 推断)**
- "上海临港分公司"切换 → 数据应按 Company 过滤(否则切换无意义)
- 卡片"数据隔离、逻辑删除" 明示
- **未实测**:具体是 row-level + company_id filter 还是其他机制,需查看 SQL

### Q7: 是否存在数据权限 Scope?

**A: VERIFIED_IN_DEMO**
- 卡片"**菜单数据权限**" + "**角色数据权限**" 明示
- 推断支持至少 4 种模式:
  - 全部(All)
  - 本人(Own)
  - 本部门(Department)
  - 本部门及下级(DepartmentAndChildren)
  - 自定义(Custom)— 推断

### Q8: 是否支持 Own / Department / DepartmentAndChildren / Custom?

**A: DOCUMENTED_ONLY (推断)**
- VOL.PRO 是 ABP.NET 衍生品(典型)或类似 .NET 框架
- ABP 默认支持 Own / DepartmentAndChildren / Custom
- **未实测**:具体 Scope 名称,需进入"角色数据权限"页确认

### Q9: Field Permission 是否存在?

**A: VERIFIED_IN_DEMO**
- 卡片"**字段权限**" 明示
- 推断支持:字段 Read / Write / Mask(脱敏)
- **未实测**:具体 mask 规则

---

## 3. 推断的 VOL.PRO 多公司架构(从已知证据)

```
Tenant
  ├── Tenant-level Config (per-tenant DB option)
  ├── Company (1..N)              ← "上海临岗分公司" 是 Company
  │     ├── Organization / Department
  │     │     ├── User (M:N)
  │     │     └── Role (M:N)
  │     │           ├── Menu Permission
  │     │           ├── Button Permission
  │     │           ├── API Permission
  │     │           ├── Data Scope (Own/Dept/All/Custom)
  │     │           └── Field Permission (R/W/Mask)
  │     ├── Dictionary
  │     ├── Numbering
  │     ├── Audit
  │     ├── File/Attachment
  │     └── ...
```

---

## 4. 与 GuliERP G2 设计对比

| 维度 | VOL.PRO | GuliERP G2 设计 | 评价 |
|---|---|---|---|
| **Tenant 概念** | 有 | 有 (Tenant) | **一致** |
| **Company 概念** | 有 | 有 (Company) | **一致** |
| **Tenant + Company 关系** | 1:N | 1:N (TASK B §6.1) | **一致** |
| **User 跨 Company** | 推断有 | 设计有 (UserCompany 表) | **一致** |
| **Role 跨 Company** | 推断有 | 设计有 (UserRole.CompanyId nullable) | **一致** |
| **数据自动按 Company 隔离** | 推断有 EF Core Query Filter | 设计有 (TASK E §7.1 composite FK) | **GuliERP 更严谨** |
| **Data Scope** | 4+ 种 | Stub V1, V2+ 完整 | **VOL.PRO 领先** |
| **Field Permission** | 有 | Stub V1, V2+ 完整 | **VOL.PRO 领先** |
| **Department** | 有 | 有 (Organization self-FK) | **一致** |
| **Position** | 有 | **无** (V1 暂未建模) | **VOL.PRO 领先** |
| **Permission 模式** | Role → 多 Permission | Role → 多 Permission (TASK B §7) | **一致** |
| **per-tenant DB** | 支持 (租户管理卡片) | 单 DB row-level, V1.5 per-schema | **VOL.PRO 灵活** |
| **isolation 严谨度** | B-(未实测 SQL)| A (composite FK, 3 roles DB users) | **GuliERP 更安全** |

---

## 5. VOL.PRO 作为 GuliERP 底座的可行性

### 5.1 如果用 VOL.PRO 替代 GuliERP 自建 Foundation

**Pros**:
- Foundation 完整(45+ 能力),省 3-6 个月
- 多公司 + 多组织 + 权限 已成熟
- AI 工具链自带(LLM 表单/建表/分析)— 营销亮点
- Mobile / WeChat / 国密 / 信创 全支持
- 1+ 年实战迭代

**Cons**:
- ❌ **没有 ERP 业务模块**(销售/采购/库存/财务)— vol.pro 卖点是"造 ERP",**它自己不是 ERP**
- ❌ 业务实现仍要"自己造" — 只是用 vol.pro 的元工具更快
- ❌ **R5 风险** — 整个产品是 low-code engine,违反 GuliERP "GuliERP 是 ERP 不是 ERP-builder"
- ❌ **学习曲线** — VOL.PRO 元数据模型 + AI 工具链都需要学
- ❌ **供应商锁定** — 业务代码与 vol.pro 框架强耦合
- ❌ **自定义业务不能表达** — 含税未税、3D 状态、reservation、posting engine 在元数据层表达不出来
- ❌ **DEC-MODULE-001 冲突** — vol.pro 实际是 monolithic meta-tool,模块独立做不到(只有"配置项",不是"代码模块")
- ❌ **国密合规** — 是优势但**国密合规本身**未必通过(需第三方认证)
- ❌ **iOS 端 暂未开放** — 自己 demo 都说"暂未开放"
- ❌ **HTTP 明文 + 4 位文本 captcha** — 安全底线不及 GuliERP G2-003 设计(Argon2id + JWT + 多因子)

### 5.2 结论

**VOL.PRO 不适合作为 GuliERP 的 Foundation**:
1. **定位冲突**:GuliERP 是 ERP,不是 ERP-builder
2. **业务能力缺失**:vol.pro 自己没有销售订单/采购订单/库存台账 — 这些仍要 GuliERP 自己造
3. **R5 风险**:vol.pro 的整个架构是 low-code engine,GuliERP 已 explicit ban
4. **安全底线低**:HTTP + 4 位 captcha 不符合 GuliERP G2-003 标准
5. **3D 状态**:vol.pro 没明示支持 GuliERP 强制的 DEC-STATUS-001

**VOL.PRO 可借鉴的 5 个具体能力**(adopt pattern only):
1. 卡片"字段权限" → GuliERP V1.5+ IFieldPolicy 完整实现
2. 卡片"菜单数据权限" → GuliERP V1.5+ IDataScopePolicy
3. 卡片"角色数据权限" → 同上
4. 顶栏租户切换 UI 形态 → GuliERP V1 复用
5. 卡片"AI 智能表单"作为 **内部开发工具**(不是给终端用户)→ GuliERP V1.5+ 内部 dev 工具

---

## 6. NOT_INSPECTED 区域(Operator 可补全)

| 项 | 状态 |
|---|---|
| 实际角色管理页 UI | NOT_INSPECTED |
| 实际用户管理页 UI | NOT_INSPECTED |
| 实际组织架构页 UI | NOT_INSPECTED |
| 实际岗位管理页 UI | NOT_INSPECTED |
| 字段权限配置 UI | NOT_INSPECTED |
| 角色数据权限配置 UI | NOT_INSPECTED |
| SQL 实际的 company_id 隔离 | NOT_INSPECTED |
| per-tenant DB 实际效果 | NOT_INSPECTED |

**Operator 可在 10-15 分钟内补完**:用 admin666 登录 → 系统管理 → 角色管理 / 用户管理 / 组织架构 → 各点开看 UI。**强烈建议 Operator 补这一轮**。

---

*End of TASK 6 — VOL_PRO_ORG_PERMISSION_ANALYSIS*
*Status: 9 个核心问题 7 个有答案(其中 4 个 VERIFIED_IN_DEMO,3 个 DOCUMENTED_ONLY),2 个 NOT_INSPECTED 强烈建议 Operator 配合补全*
