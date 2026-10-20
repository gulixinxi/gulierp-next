# 谷粒 CRM 国内版 - 模块开发说明

> 开发日期：2026-10-17
> 基于：GuliERP Next（.NET 10 + Vue 3 + PostgreSQL）
> 状态：MVP 第一版完成（线索管理 + 客户管理）

---

## 一、已完成功能

### 1. 后端（.NET 10 + EF Core）

#### 领域层（Domain）
- `Lead` - 线索实体
- `Customer` - 客户实体（注：当前已重构为 `Company`，与 `mdm.BusinessPartner` 是两张独立的表，不合并。详见 `docs/CRM_ERP_Business_Boundary.md`）
- `Contact` - 联系人实体
- `FollowUpRecord` - 跟进记录实体
- 枚举：`LeadStatus`、`LeadSource`、`CustomerStatus`

#### 应用层（Application）
- `ILeadService` - 线索服务接口
- `ICustomerService` - 客户服务接口
- `IFollowUpService` - 跟进记录服务接口
- DTO：请求/响应对象
- 权限：`CrmPermissions`（6 个权限码）
- 策略：`CrmPolicies`（6 个授权策略）

#### 基础设施层（Infrastructure）
- `CrmDbContext` - 数据库上下文（schema: crm）
- `LeadService` - 线索服务实现
- `CustomerService` - 客户服务实现
- `FollowUpService` - 跟进记录服务实现
- `DependencyInjection` - 依赖注入注册

#### API 端点
- `GET /api/v1/crm/leads` - 线索列表
- `GET /api/v1/crm/leads/{id}` - 线索详情
- `POST /api/v1/crm/leads` - 创建线索
- `PUT /api/v1/crm/leads/{id}` - 更新线索
- `DELETE /api/v1/crm/leads/{id}` - 删除线索
- `POST /api/v1/crm/leads/{id}/convert` - 线索转化为客户
- `POST /api/v1/crm/leads/{id}/assign` - 分配线索
- `GET /api/v1/crm/customers` - 客户列表
- `GET /api/v1/crm/customers/{id}` - 客户详情
- `POST /api/v1/crm/customers` - 创建客户
- `PUT /api/v1/crm/customers/{id}` - 更新客户
- `DELETE /api/v1/crm/customers/{id}` - 删除客户
- `GET /api/v1/crm/customers/{id}/contacts` - 客户联系人列表
- `POST /api/v1/crm/customers/{id}/contacts` - 添加联系人
- `DELETE /api/v1/crm/customers/contacts/{contactId}` - 删除联系人
- `GET /api/v1/crm/follow-ups` - 跟进记录列表
- `POST /api/v1/crm/follow-ups` - 创建跟进记录
- `DELETE /api/v1/crm/follow-ups/{id}` - 删除跟进记录

### 2. 前端（Vue 3 + Element Plus）

#### 页面
- `LeadList.vue` - 线索管理列表（筛选、新建、编辑、删除、转化）
- `CustomerList.vue` - 客户管理列表（筛选、新建、编辑、删除）
- `CustomerDetail.vue` - 客户详情（基本信息、联系人、跟进记录）

#### API 封装
- `api/crm.ts` - 所有 CRM API 调用
- `types/crm.ts` - TypeScript 类型定义

#### 路由
- `/crm/leads` - 线索管理
- `/crm/customers` - 客户管理
- `/crm/customers/:id` - 客户详情

#### 导航菜单
- 新增「客户关系」模块（CRM）
  - 客户管理：线索管理、客户管理
  - 销售流程：商机管理（待开发）、合同管理（待开发）、回款管理（待开发）

---

## 二、数据库表结构

### crm.leads（线索表）
| 字段 | 类型 | 说明 |
|---|---|---|
| id | bigint | 主键（Snowflake） |
| tenant_id | bigint | 租户ID |
| company_id | bigint | 公司ID |
| lead_no | varchar(50) | 线索编号（唯一） |
| customer_name | varchar(200) | 客户名称 |
| contact_name | varchar(100) | 联系人 |
| phone | varchar(50) | 电话 |
| email | varchar(200) | 邮箱 |
| company_name | varchar(200) | 公司名称 |
| position | varchar(100) | 职位 |
| region | varchar(100) | 地区 |
| address | varchar(500) | 详细地址 |
| source | int | 来源（0电话/1网站/2展会/3转介绍/4社交/5广告/99其他） |
| status | int | 状态（0新线索/1跟进中/2已转化/3已无效） |
| owner_id | bigint | 负责人ID |
| requirement | text | 需求描述 |
| remarks | text | 备注 |
| converted_customer_id | bigint | 转化后的客户ID |
| converted_at | timestamptz | 转化时间 |
| created_at | timestamptz | 创建时间 |
| created_by | bigint | 创建人 |
| modified_at | timestamptz | 修改时间 |
| modified_by | bigint | 修改人 |
| concurrency_version | int | 并发版本 |

### crm.customers（客户表）
| 字段 | 类型 | 说明 |
|---|---|---|
| id | bigint | 主键 |
| tenant_id | bigint | 租户ID |
| company_id | bigint | 公司ID |
| customer_no | varchar(50) | 客户编号（唯一） |
| name | varchar(200) | 客户名称 |
| customer_type | varchar(20) | 客户类型（企业/个人） |
| status | int | 状态（0潜在/1意向/2成交/3流失） |
| industry | varchar(100) | 行业 |
| scale | varchar(50) | 规模 |
| region | varchar(100) | 地区 |
| address | varchar(500) | 地址 |
| website | varchar(200) | 官网 |
| owner_id | bigint | 负责人ID |
| level | varchar(10) | 等级（A/B/C/D） |
| source | varchar(50) | 来源 |
| remarks | text | 备注 |
| source_lead_id | bigint | 来源线索ID |
| 审计字段 | - | 同上 |

### crm.contacts（联系人表）
| 字段 | 类型 | 说明 |
|---|---|---|
| id | bigint | 主键 |
| customer_id | bigint | 客户ID |
| name | varchar(100) | 姓名 |
| position | varchar(100) | 职位 |
| department | varchar(100) | 部门 |
| mobile | varchar(50) | 手机 |
| phone | varchar(50) | 电话 |
| email | varchar(200) | 邮箱 |
| wechat | varchar(100) | 微信 |
| qq | varchar(50) | QQ |
| is_primary | boolean | 是否主要联系人 |
| remarks | text | 备注 |

### crm.follow_up_records（跟进记录表）
| 字段 | 类型 | 说明 |
|---|---|---|
| id | bigint | 主键 |
| target_type | varchar(20) | 关联类型（Lead/Customer） |
| target_id | bigint | 关联ID |
| follow_type | varchar(20) | 跟进方式（电话/拜访/邮件/微信/其他） |
| content | text | 跟进内容 |
| follow_at | timestamptz | 跟进时间 |
| next_follow_at | timestamptz | 下次跟进时间 |
| result | varchar(500) | 跟进结果 |
| follow_by_id | bigint | 跟进人ID |
| remarks | text | 备注 |

---

## 三、权限设计

### 权限码
| 权限码 | 说明 |
|---|---|
| crm.lead.read | 线索读取 |
| crm.lead.manage | 线索管理 |
| crm.customer.read | 客户读取 |
| crm.customer.manage | 客户管理 |
| crm.followup.read | 跟进记录读取 |
| crm.followup.manage | 跟进记录管理 |

### 建议角色
| 角色 | 权限 |
|---|---|
| CRM_ADMIN | 全部 CRM 权限 |
| CRM_SALES | 线索/客户/跟进的读写权限 |
| CRM_READONLY | 只读权限 |

> 注意：角色需要在 Identity 模块中创建并分配权限码。

---

## 四、待开发功能（Roadmap）

### 第二阶段（2周）
- [ ] 商机管理（Opportunity）
  - 商机阶段、金额、预计成交时间
  - 销售漏斗报表
- [ ] 合同管理（Contract）
  - 合同录入、审批、归档
  - 与销售订单关联
- [ ] 回款管理（Payment）
  - 回款计划、回款记录、逾期提醒

### 第三阶段（1周）
- [ ] 客户公海（客户池）
- [ ] 线索自动分配规则
- [ ] 跟进提醒（定时任务）
- [ ] 销售报表（业绩统计、客户分析）

### 第四阶段（持续）
- [ ] 与 Lead Center 集成（线索采集、企业审核）
- [ ] 与 GEO 集成（客户观察、评分）
- [ ] AI 辅助（客户画像、跟进建议）
- [ ] 移动端适配

---

## 五、设计调整建议（重要）

根据参考资料 `DOUBAO_CRM_ERP_LEAD_CENTER_DEVELOPMENT_REFERENCE_2026-10-17.md` 的建议：

### 当前设计（MVP）
- 独立的 `crm.customers` 表作为客户主档
- 优点：快速验证、不影响现有 MDM 模块
- 缺点：与 ERP 的 BusinessPartner 存在数据冗余

### 建议调整（第二阶段后）
1. **客户主档迁移到 BusinessPartner**
   - 复用 `mdm.business_partners` 表
   - CRM 模块通过 `business_partner_id` 关联
   - 避免双客户主档风险

2. **联系人关联到 BusinessPartner**
   - `crm.contacts.customer_id` → `business_partner_id`
   - 支持多联系人、多角色

3. **线索转化时创建 BusinessPartner**
   - 转化时在 MDM 中创建 BusinessPartner（Role=Customer）
   - CRM 保存扩展属性（等级、来源、负责人等）

4. **与销售订单打通**
   - 客户详情页显示相关销售订单
   - 商机转化时自动创建销售订单

### 迁移计划
- 第一阶段：当前独立表设计，快速验证业务流程
- 第二阶段：开发商机/合同/回款时，同步迁移到 BusinessPartner
- 第三阶段：数据迁移脚本，将 crm.customers 数据迁移到 business_partners

---

## 六、本地运行

### 前置条件
- .NET 10 SDK
- Node.js 18+
- PostgreSQL 数据库

### 后端启动
```bash
cd apps/api/GuliERP.Api
dotnet run
# 默认端口：5000（或查看 launchSettings.json）
```

### 前端启动
```bash
cd apps/web
npm install
npm run dev
# 默认端口：5173
```

### 数据库迁移
```bash
# 添加迁移
dotnet ef migrations add CRM001_Initial --project modules/crm/GuliERP.Crm.Infrastructure --startup-project apps/api/GuliERP.Api

# 更新数据库
dotnet ef database update --project modules/crm/GuliERP.Crm.Infrastructure --startup-project apps/api/GuliERP.Api
```

---

## 七、文件清单

### 新增文件（后端）
```
modules/crm/
├── GuliERP.Crm.Domain/
│   ├── GuliERP.Crm.Domain.csproj
│   ├── Entities/
│   │   ├── Lead.cs
│   │   ├── Customer.cs
│   │   ├── Contact.cs
│   │   └── FollowUpRecord.cs
│   └── Enums/
│       ├── LeadStatus.cs
│       ├── LeadSource.cs
│       └── CustomerStatus.cs
├── GuliERP.Crm.Application/
│   ├── GuliERP.Crm.Application.csproj
│   ├── CrmPermissions.cs
│   ├── CrmPolicies.cs
│   ├── CrmDtos.cs
│   └── ICrmServices.cs
└── GuliERP.Crm.Infrastructure/
    ├── GuliERP.Crm.Infrastructure.csproj
    ├── DependencyInjection.cs
    ├── Persistence/
    │   └── CrmDbContext.cs
    └── Services/
        ├── LeadService.cs
        ├── CustomerService.cs
        └── FollowUpService.cs
```

### 新增文件（前端）
```
apps/web/src/
├── api/crm.ts
├── types/crm.ts
└── views/crm/
    ├── LeadList.vue
    ├── CustomerList.vue
    └── CustomerDetail.vue
```

### 修改文件
- `apps/api/GuliERP.Api/GuliERP.Api.csproj` - 添加 CRM 项目引用
- `apps/api/GuliERP.Api/Program.cs` - 注册 CRM 服务和端点
- `apps/api/GuliERP.Api/Crm/CrmEndpoints.cs` - CRM API 端点
- `apps/web/src/router.ts` - 添加 CRM 路由
- `apps/web/src/layout/navigation.ts` - 添加 CRM 导航菜单

---

## 八、验证状态

- [x] CRM Domain 编译通过（0 警告 0 错误）
- [x] CRM Application 编译通过（0 警告 0 错误）
- [x] CRM Infrastructure 编译通过（0 警告 0 错误）
- [x] API 项目编译通过（0 警告 0 错误）
- [ ] 数据库迁移执行（需要 PostgreSQL 环境）
- [ ] 前端编译验证（需要 npm install）
- [ ] 端到端功能测试
- [ ] 权限角色配置

---

## 九、下一步行动

1. **配置数据库连接**
   - 在 `appsettings.json` 或环境变量中配置 PostgreSQL 连接字符串
   - 执行数据库迁移

2. **配置权限角色**
   - 在 Identity 模块中创建 CRM 相关角色
   - 为角色分配权限码

3. **前端验证**
   - 运行 `npm install` 和 `npm run dev`
   - 验证页面功能

4. **提交代码**
   - 创建新分支 `feature/crm-module`
   - 提交并推送到 GitHub

5. **第二阶段开发**
   - 商机管理
   - 合同管理
   - 回款管理
   - 客户主档迁移到 BusinessPartner

---

*本文档由豆包生成，基于 GuliERP Next 项目架构规范开发。*
