# Guli CRM 与 GuliERP 客商业务边界说明

> 版本：V1.0
> 日期：2026-10-19
> 适用：Guli Platform V2（一套应用、一次部署，同时包含 GuliERP 与 Guli CRM）
> 状态：已确认，作为后续开发的业务规则依据

---

## 一、核心原则：两张表，不合并

### 1.1 crm.Company 与 mdm.BusinessPartner 是两张独立的表

| 维度 | crm.Company（CRM客户） | mdm.BusinessPartner（ERP客商） |
|---|---|---|
| 业务目的 | 获客、跟进、商机管理 | 采购、销售、库存、财务结算 |
| 覆盖范围 | 潜在客户、意向客户、成交客户 | 供应商、客户、合作伙伴 |
| 数据所有者 | Guli CRM 业务领域 | GuliERP 业务领域 |
| 租户范围 | TenantId + CompanyId（公司级） | TenantId（租户级） |
| 创建时机 | 线索转化或手动录入 | 采购/销售业务需要时创建 |

### 1.2 典型场景：文体店可以是ERP客商但不是CRM客户

- 某文体店向本公司供应办公用品 → 在 mdm.BusinessPartner 中作为供应商存在
- 但该文体店不是本公司的销售目标客户 → 不在 crm.Company 中
- **结论**：不能因为 mdm.BusinessPartner 中有记录，就自动在 crm.Company 中创建客户

---

## 二、Guli CRM 的业务职责边界

### 2.1 CRM 负责到订单交接为止

```
线索 → 跟进 → 商机 → 报价 → [订单交接] → ERP流程
```

- CRM 负责：获客、线索培育、客户跟进、商机推进、报价管理
- 订单交接后：
  - **使用 GuliERP 的客户**：订单进入 ERP 销售流程（销售订单→发货→收款）
  - **使用外部 ERP 的客户**：CRM 向客户的外部系统交接订单信息，**不强制在本系统创建 ERP 客商**

### 2.2 不得强制创建 ERP 客商

- CRM 客户使用外部 ERP 时，由 CRM 负责向其系统交接
- 不得因"CRM中有客户"就自动在 mdm.BusinessPartner 中创建记录
- 是否创建 ERP 客商，由业务需要决定，不是 CRM 的前置条件

---

## 三、候选匹配的定位：辅助查询，不是关联关系

### 3.1 GET /api/v1/crm/companies/{id}/erp-match 的性质

- **只读查询**：不写任何表，不创建关联，不修改数据
- **候选匹配**：返回可能是同一家企业的 ERP 客商列表
- **需人工确认**：匹配结果仅供参考，不得自动标记为"已关联"或"已同步"

### 3.2 三种状态的严格区分

| 状态 | 含义 | 是否写库 | 谁来操作 |
|---|---|---|---|
| **字段匹配** | 只读查询结果，信用代码或名称相似 | 否 | 系统自动查询 |
| **人工确认关联** | 用户确认两家是同一家企业，写入关联表 | 是（关联表） | 有权限的用户手动操作 |
| **已同步** | CRM客户信息已写入MDM客商表 | 是（MDM表） | 后续任务，本轮不实现 |

**当前实现只有"字段匹配"，不涉及后两种状态。**

### 3.3 名称匹配的误匹配风险

- 名称匹配使用子串匹配（Contains），可能产生误匹配
- 例如搜索"山东"会匹配所有名称含"山东"的公司
- 名称匹配置信度低于95，排序在信用代码精确匹配（100）之后
- **所有名称匹配的候选必须人工确认**，不得直接用于业务决策

### 3.4 没有匹配是正常结果

- 查询返回空列表 = 当前租户下没有相似的 ERP 客商
- 这不是错误，不代表客户数据有问题
- 客户可以完全独立于 ERP 客商存在

---

## 四、权限边界

### 4.1 候选匹配端点的双重权限

```
GET /api/v1/crm/companies/{id}/erp-match
需要同时具备：
  - crm.company.read（CRM客户读取权限）
  - mdm.business-partner.read（MDM客商读取权限）
```

- 两个权限是 **AND** 关系，缺一不可
- 当前没有任何角色拥有 `mdm.business-partner.read` 权限
- **不给整个 Normal User 角色授予该权限**（会扩大所有普通用户的访问范围）
- 后续如需开放，应针对特定角色或特定用户精细授权

### 4.2 租户与公司范围

- MDM客商查询：自动过滤当前租户（TenantId），不暴露其他租户数据
- CRM客户查询：过滤当前租户 + 当前公司（TenantId + CompanyId）
- 跨公司访问：如果当前用户的公司上下文与客户所属公司不一致，返回null（不泄露存在性）

---

## 五、技术实现约束

### 5.1 CRM 不得直接引用 MdmDbContext

- CRM 通过 MDM 应用层契约 `IBusinessPartnerCandidateLookupService` 查询
- 不直接引用 `MdmDbContext`，保持模块边界
- 架构测试 `MdmServiceBoundaryArchitectureTests` 强制执行此约束

### 5.2 只读保证

- `BusinessPartnerCandidateLookupService`：全部使用 `AsNoTracking()`，无 `SaveChanges`
- `CompanyErpMatchService`：全部使用 `AsNoTracking()`，无 `SaveChanges`
- 单元测试验证：调用查询后，数据库状态不变

### 5.3 技术标识保持不变

- 程序集名、命名空间、schema、表名、权限码不因本次功能而改名
- `crm.Company` 和 `mdm.BusinessPartner` 表名保持不变
- `crm.company.read` 和 `mdm.business-partner.read` 权限码保持不变

---

## 六、后续候选任务（本轮不实现）

以下任务需业务验证后再决定是否实施，本轮不做：

1. **人工确认关联**：设计关联表（crm_company_mdm_partner_link），用户手动确认后写入
2. **客户信息同步到MDM**：将CRM客户信息写入mdm.BusinessPartner（需决定同步策略）
3. **商机生成销售订单**：CRM商机赢单后自动在ERP创建销售订单
4. **Sales/Purchase schema迁移**：当前数据库中这两个schema不存在，迁移未应用
5. **权限精细授权**：决定哪些角色可以使用候选匹配功能

---

## 七、与早期文档的差异

`CRM_MODULE_DEVELOPMENT.md` 中"Customer实体（后续迁移到BusinessPartner）"的表述已过时。
根据本边界说明：
- crm.Company 不会迁移到 mdm.BusinessPartner
- 两张表长期共存，各自服务于不同业务领域
- 候选匹配是可选的辅助查询，不是迁移的前置步骤

---

**全文完**
