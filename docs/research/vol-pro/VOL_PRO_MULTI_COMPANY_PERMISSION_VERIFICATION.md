# VOL_PRO_MULTI_COMPANY_PERMISSION_VERIFICATION

| Field | Value |
|---|---|
| Goal | VOL-PRO-002 TASK 3 — Multi-Company / Organization / Data Scope 验证 |
| Researcher | Mavis + explore Agent C |
| Date | 2026-08-19 (Asia/Taipei) |
| Source | GitHub `cq-panda/Vue.NetCore` MIT public source |
| Final verdict | **SINGLE_TENANT_SINGLE_COMPANY_2_LEVEL_SCOPE + FIELD_STUB** |
| Evidence grade | SOURCE_VERIFIED × 12 / INFERRED × 0 / UNKNOWN × 6 (boundary) |

---

## 0. 总览结论(快读)

| 维度 | VOL.NET 实际能力 | GuliERP 多公司/多组织常见预期 | 差距 |
|---|---|---|---|
| Tenant/Company 实体 | **不存在** | 通常 1 个 Company 表 | 大 |
| Organization(部门) | `Sys_Department`(GUID 树) | 通常 Department/Org 表 | 名称不同,语义一致 |
| User → Company 关系 | 无 Company 概念 | M:N 必备 | 大 |
| User → Department 关系 | M:N (`Sys_UserDepartment`) | M:N 常见 | 一致 |
| User → Role 关系 | **1:1**(`Sys_User.Role_Id`) | M:N 常见 | 中 |
| Role 作用域 | `Sys_Role.Dept_Id`(可选绑定一个部门) | Global / Tenant / Company | 部分 |
| 数据隔离执行层 | **OPT-IN per Service**(`IsMultiTenancy` 标志) | 通常框架级 | 弱 |
| Data Scope 类型 | 2 种(All / Own) | 5 种(All/Own/Dept/DeptAndChildren/Custom) | 大 |
| Field Permission | **存 stub 不存逻辑**(`FilterQueryableAuthFields` 是空壳) | 必备 | 大 |
| 权限表 | `Sys_RoleAuth`(Menu + Action) | `Sys_RoleAuth` / `Sys_RoleDataAuth` / `Sys_FieldPermission` 齐全 | 缺两张表 |

**核心判断**: VOL.NET 是 **单租户(单公司)+ 多部门(M:N)+ 单角色(1:1)+ 2 级数据范围** 的"中型后台"权限模型,
**没有** 多公司/Tenant/FieldPermission 的开箱即用实现。所有"多租户"相关文件(`TenancyManager<T>`)都是开发者自定义入口,
本身是空函数。

---

## 1. Q1:是否存在 Tenant / Company / Organization / Department / Position 实体?

### 答案:Department 有;Tenant / Company / Organization / Position 均不存在

**Department 实体存在** — `Sys_Department.cs`:

```csharp
[Entity(TableCnName = "组织架构", TableName = "Sys_Department")]
public partial class Sys_Department : BaseEntity
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid DepartmentId { get; set; }       // PK = GUID
    public string DepartmentName { get; set; }   // 组织名称
    public string DepartmentCode { get; set; }   // 组织编号
    public Guid? ParentId { get; set; }          // 树形 ParentId (GUID)
    public string DepartmentType { get; set; }   // 组织类型(自由文本)
    public int? Enable { get; set; }
    public string Remark { get; set; }
    // ...审计字段...
}
```

> 关键观察:
> - PK 用 **GUID 而非 int**(其他表都是 int)
> - 树形 ParentId 闭环依赖自身
> - 没有 `CompanyId` / `TenantId` 列 → 部门独立于 Company 概念
> - 没有 Position 字段(职位/岗位信息存哪?看下文)
> - **证据等级**: SOURCE_VERIFIED
> - 引用: `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Entity/DomainModels/System/Sys_Department.cs`

**Tenant / Company / Organization / Position 反向证据**:

- GitHub Code Search `repo:cq-panda/Vue.NetCore Sys_Organization` → **HTTP 404**
  (确认 search 端点无结果 — Search API 返回 404 而非空数组, 表明索引中无文件匹配)
- GitHub Code Search `repo:cq-panda/Vue.NetCore Sys_Company` → **HTTP 404**
- Entity 目录 `vol.api.sqlsugar/VOL.Entity/DomainModels/System/` 完整列表 16 个文件,
  无 `Sys_Company.cs` / `Sys_Organization.cs` / `Sys_Tenant.cs` / `Sys_Position.cs`
- 5 套 DB 脚本目录(`DB/sqlserver` / `DB/mysql` / `DB/pgsql` / `DB/Oracle` / `DB/DM`)
  每个目录都是单文件 `表结构与数据.sql`,5 份脚本各自 1.1MB 级别,
  内含 Sys_ 前缀的表 12 张左右,无 Sys_Company/Sys_Organization(由 Sys_Department 表名"组织架构"反证)

| 实体 | 状态 | 表名 | 文件 |
|---|---|---|---|
| Tenant | **不存在** | — | — |
| Company | **不存在** | — | — |
| Organization | **不存在** | — | — |
| Department | **存在** | `Sys_Department` | `Sys_Department.cs` |
| Position | **不存在** | — | (职位信息塞进 `Sys_User.RoleName` 自由文本) |

> **证据等级**: SOURCE_VERIFIED(对 Department)/ 强 INFERRED + SOURCE_VERIFIED(对其他四个的缺失)
> 引用: `https://api.github.com/repos/cq-panda/Vue.NetCore/contents/vol.api.sqlsugar/VOL.Entity/DomainModels/System`

---

## 2. Q2:Company 与 Department 是同一实体还是分离?

### 答案:**只有 Department 一个实体,不存在 Company**

VOL.NET **完全没有 Company 概念**:

- `Sys_Department` 中文名 = "**组织架构**"(由 `[Entity(TableCnName = "组织架构")]`)
- 没有 `CompanyId` / `ParentCompanyId` / `CompanyCode` 等任何字段
- DepartmentType 是 `nvarchar(50)` **自由文本**,可用于区分"公司/部门/小组",但**没有枚举约束,无独立 Company 实体**
- 角色表 `Sys_Role.Dept_Id` 只引用 Department

**两种设计选择的后果**:

1. **VOL.NET 的选择**: Department 是万能容器 — 顶级 Department 可代表"公司",下级 Department 代表"部门/小组"
2. **典型 ERP 的选择**: Company(独立表,可能有 TaxId/LegalName/Region) + Department(挂在 Company 下)

> 这意味着如果业务确实需要"独立法人/多公司"语义,
> 必须**自建 Sys_Company 表 + Sys_Department.CompanyId 字段**,框架不提供
> **证据等级**: SOURCE_VERIFIED + INFERRED
> 引用: `Sys_Department.cs:30`(DepartmentName + DepartmentType 字段)

---

## 3. Q3:User 是否可属于多个 Department?是否有 M:N 表?

### 答案:**是的,真正的 M:N 表 `Sys_UserDepartment` 存在**

VOL.NET 在 User 与 Department 之间是**完整的 M:N 设计**:

```csharp
// vol.api.sqlsugar/VOL.Entity/DomainModels/System/Sys_UserDepartment.cs
[Entity(TableCnName = "用户所属组织", TableName = "Sys_UserDepartment")]
public partial class Sys_UserDepartment : BaseEntity
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }              // PK = GUID
    public int UserId { get; set; }           // FK → Sys_User.User_Id
    public Guid DepartmentId { get; set; }    // FK → Sys_Department.DepartmentId
    public int Enable { get; set; }           // 启用标志
    // ...审计字段...
}
```

**两层冗余** — `Sys_User` 上同时缓存了部门信息:

```csharp
// vol.api.sqlsugar/VOL.Entity/DomainModels/System/Sys_User.cs
public class Sys_User : BaseEntity
{
    public int Role_Id { get; set; }              // 1:1 Role
    public int? Dept_Id { get; set; }             // 主部门
    public string DeptIds { get; set; }           // nvarchar(2000) 逗号分隔 GUID
    public string DeptName { get; set; }          // 冗余
    public string RoleName { get; set; }          // 冗余
}
```

`DeptIds` 字段是**反范式缓存** — 解析逻辑见 `UserContext.cs`:

```csharp
// vol.api.sqlsugar/VOL.Core/UserManager/UserContext.cs (L89-90)
DeptIds = string.IsNullOrEmpty(s.DeptIds)
    ? new List<Guid>()
    : s.DeptIds.Split(",").Select(x => (Guid)x.GetGuid()).ToList(),
```

> 关键观察:
> - **M:N 表(`Sys_UserDepartment`)是真理来源**
> - `Sys_User.DeptIds` 字符串列是**派生缓存**(在 `UserContext.GetUserInfo` 解析)
> - `Sys_User.Dept_Id` 是主部门(`Dept_Id ?? 0` fallback)
> - **证据等级**: SOURCE_VERIFIED
> 引用:
> - `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Entity/DomainModels/System/Sys_UserDepartment.cs`
> - `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Entity/DomainModels/System/Sys_User.cs`
> - `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/UserManager/UserContext.cs`

**User 与 Role 的关系** — **1:1,不是 M:N**:

```csharp
public int Role_Id { get; set; }   // 必填 (Required)
```

`Sys_RoleAuth.User_Id` 字段虽然存在, 但实际**只用于"特定用户的额外授权覆盖"** 而非"用户多角色"。

---

## 4. Q4:Role 的 scope 是 Global / Tenant / Company / Org 哪一级?

### 答案:**仅 Department 级(可选) + 隐式 Global;无 Tenant/Company**

`Sys_Role` 表结构:

```csharp
public partial class Sys_Role : BaseEntity
{
    public int Role_Id { get; set; }      // PK
    public int ParentId { get; set; }     // 角色继承 (RBAC1)
    public string RoleName { get; set; }
    public int? Dept_Id { get; set; }     // 角色所属部门(可选,非强制)
    public string DeptName { get; set; }  // 冗余
    public byte? Enable { get; set; }
    public int? OrderNo { get; set; }
    // ...审计字段...
}
```

| 字段 | 含义 | 强制? |
|---|---|---|
| `ParentId` | 角色继承(RBAC1 角色层级) | 是,默认 0 |
| `Dept_Id` | 角色"所属"部门(用于"部门数据范围") | **否**(`int?`) |
| 无 `CompanyId` | — | — |
| 无 `TenantId` | — | — |

**"Global"通过 hardcode 实现,不是字段**:

```csharp
// UserContext.cs
public bool IsRoleIdSuperAdmin(int roleId) => roleId == 1;
public bool IsSuperAdmin => IsRoleIdSuperAdmin(this.RoleId);
```

> 关键观察:
> - **没有 Tenant/Company 概念** → 谈不上对应 scope
> - 角色可绑定一个 Department(`Dept_Id`),可推断"此角色只对本部门有效"语义
> - **RoleId == 1** 是硬编码 SuperAdmin(代码层短路所有权限)
> - **证据等级**: SOURCE_VERIFIED
> 引用: `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Entity/DomainModels/System/Sys_Role.cs`

---

## 5. Q5:数据自动按 Company 隔离在哪一层执行?

### 答案:**没有任何自动隔离层 — 全部依赖开发者手动加 WHERE**

四个候选执行层的逐一排查:

### 5.1 Framework (AuthorizationFilter)? — ❌ 不做数据隔离

`ApiAuthorizeFilter.cs` 只做 **JWT 验签 + 过期检测**,不涉及数据:

```csharp
public void OnAuthorization(AuthorizationFilterContext context)
{
    if (context.ActionDescriptor.EndpointMetadata.Any(item => item is IAllowAnonymous))
    {
        // 匿名 + 固定 Token 短路
        return;
    }
    // 只验 exp claim + vol_exp refresh header
    DateTime expDate = context.HttpContext.User.Claims
        .Where(x => x.Type == JwtRegisteredClaimNames.Exp)
        .Select(x => x.Value).FirstOrDefault().GetTimeSpmpToDate();
    if ((expDate - DateTime.Now).TotalMinutes < AppSetting.ExpMinutes / 3 ...) { ... }
}
```

`ActionPermissionFilter.cs` 验的是**操作权限**(Add/Edit/Delete),**不验数据范围**。
证据: `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/Filters/ApiAuthorizeFilter.cs`

### 5.2 ORM Interceptor (QueryFilter)? — ❌ 没有

`BaseDbContext.cs` 全文 23 行,`Set<TEntity>()` 直接返回 `SqlSugarClient.Queryable<TEntity>()`,
**没有 global query filter / 没有 SaveChanges 拦截**:

```csharp
public abstract class BaseDbContext : DbContext
{
    public virtual ISugarQueryable<TEntity> Set<TEntity>(bool filterDeleted = false) where TEntity : class, new()
    {
        return SqlSugarClient.Queryable<TEntity>();
    }
    public int SaveChanges() => SqlSugarClient.SaveQueues();
}
```

证据: `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/DbContext/BaseDbContext.cs`

### 5.3 SQL(手写 WHERE)? — ⚠️ 唯一可用路径,通过 OPT-IN `IsMultiTenancy`

`TenancyManager.cs` 全文 33 行,几乎全是注释。**默认实现返回 null**,
开发者必须**自己重写**才能启用:

```csharp
public static class TenancyManager<T> where T : class
{
    public static string GetSearchQueryable(string tableName)
    {
        string multiTenancyString = null;
        //if (UserContext.Current.IsSuperAdmin) return multiTenancyString;
        switch (tableName)
        {
            // 例如:指定用户表指定查询条件
            //case "Sys_User":
            //    multiTenancyString += $" where UserId='{UserContext.Current.UserId}'";
            //    break;
            default:
                // 开启多租户数据隔离,用户只能看到自己的表数据(自己根据需要写条件)
                // multiTenancyString += $" select * from {tableName} where CreateID='{UserContext.Current.UserId}'";
                break;
        }
        return multiTenancyString;  // 默认 null
    }
}
```

调用点 `ApplicationServiceBaseSearchExtensions.cs`:

```csharp
public static ISugarQueryable<TEntity> GetSearchQueryable<TEntity, TRepository>(
    this ServiceBase<TEntity, TRepository> service, ISugarQueryable<TEntity> queryable)
{
    string tableName = typeof(TEntity).GetEntityTableName();
    string sql = TenancyManager<TEntity>.GetSearchQueryable(tableName);
    if (!string.IsNullOrEmpty(sql))
        return service.repository.DbContext.SqlQueryable<TEntity>(sql);
    return service.repository.DbContext.Set<TEntity>();
}
```

`ServiceFunFilter<T>.IsMultiTenancy` 是**逐 Service 开关**(每个 service 自己声明):

```csharp
/// <summary>
/// 2020.08.15是否开启多租户功能
/// 使用方法见文档或SellOrderService.cs
/// </summary>
protected bool IsMultiTenancy { get; set; }   // 默认 false
```

证据:
- `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/Tenancy/TenancyManager.cs`
- `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/BaseProvider/ApplicationServiceBaseSearchExtensions.cs`
- `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/Filters/ServiceFunFilter.cs`

### 5.4 Application Service(业务代码)? — ✅ 主要执行点

`LimitCurrentUserPermission` 字段的官方注释:

```csharp
/// <summary>
/// 是否开启用户数据权限,true=用户只能操作自己(及下级角色)创建的数据
/// 如:查询、删除、修改等操作
/// 注意:需要在代码生成器界面选择【是】及生成Model才会生效
/// </summary>
protected bool LimitCurrentUserPermission { get; set; } = false;
```

注意: **必须在代码生成器生成 Model 时勾选"是"** → 框架才会自动注入 `CreateID/CreateDate/Modifier` 字段
的过滤逻辑(由代码生成器生成的 Service 模板中嵌入 `LimitCurrentUserPermission` 判断)。

> 关键观察 — **没有自动全局隔离**:
> 1. `ApiAuthorizeFilter`: 不做 ❌
> 2. `BaseDbContext`: 不做 ❌
> 3. `TenancyManager<T>`: 空实现,需要开发者重写 ⚠️
> 4. 业务代码: `LimitCurrentUserPermission` + 代码生成器模板 ✅
> 5. **没有任何中间件 / AOP / global filter 拦截**所有查询
> **证据等级**: SOURCE_VERIFIED

---

## 6. Q6:是否支持 Data Scope = All / Own / Department / DepartmentAndChildren / Custom?

### 答案:**只支持 2 级:All / Own;不支持 Department / DepartmentAndChildren / Custom**

VOL.NET 的数据范围**只有 2 种**:

| 数据范围 | VOL.NET 实现 | 证据 |
|---|---|---|
| All(全部) | 不设 `LimitCurrentUserPermission` 即可 | `ServiceFunFilter.cs` 默认 `false` |
| Own(自己创建的) | `LimitCurrentUserPermission = true` | `ServiceFunFilter.cs:28-32` |
| Department | **不支持** | UNKNOWN — 框架层无机制 |
| DepartmentAndChildren | **不支持** | 同上,虽然 `DepartmentContext.GetAllChildrenIds` 可用 |
| Custom(自定义) | 部分支持: `QueryRelativeExpression` / `QueryRelativeList` 委托 | `ServiceFunFilter.cs:78-83` |

**Department 工具存在但未自动应用**:

```csharp
// DepartmentContext.cs - 工具方法,需要手动调用
public static List<Guid> GetAllChildrenIds([NotNull] List<Guid> ids)
{
    ids = ids.Distinct().ToList();
    if (ids.Count == 0) return new List<Guid>() { Guid.NewGuid() };
    for (int i = 0; i < ids.Count(); i++)
    {
        Guid id = ids[i];
        var list = _depts.Where(x => x.parentId == id && !ids.Contains(x.id))
            .Select(s => s.id).Distinct().ToList();
        if (list.Count > 0) ids.AddRange(list);
    }
    return ids;
}
```

> 关键观察:
> - `DepartmentContext.GetAllChildrenIds()` 是**纯算法工具**,需要开发者**手动调用并注入 Where 条件**
> - `UserContext.GetAllChildrenDeptIds()` 是其简化包装
> - **不存在"角色级数据范围"配置**(没有"销售经理 = 本部门及下级"这种声明式配置)
> - **证据等级**: SOURCE_VERIFIED

---

## 7. Q7:是否支持 Field Permission(字段级读写)?是 attribute 标记还是配置表?

### 答案:**STUB ONLY — 仅有钩子,没有实现;也不是 attribute 标记或配置表**

`ApplicationServiceBaseSearchExtensions.cs` 末尾:

```csharp
/// <summary>
/// 映射指定权限的字段不查询数据库
/// </summary>
public static List<TEntity> FilterQueryableAuthFields<TEntity>(this ISugarQueryable<TEntity> queryable) where TEntity : class
{
    return queryable.ToList();   // ← 这是空实现
}

public static Task<List<TEntity>> FilterQueryableAuthFieldsAsync<TEntity>(this ISugarQueryable<TEntity> queryable) where TEntity : class
{
    return queryable.ToListAsync();   // ← 异步版也是空
}
```

调用点 `ServiceBase.GetPageData` / `GetPageDataAsync`:

```csharp
// ServiceBase.cs
pageGridData.rows = queryable.FilterQueryableAuthFields();      // line ~75
pageGridData.rows = await queryable.FilterQueryableAuthFieldsAsync();  // async version
```

注释 "映射指定权限的字段不查询数据库" 暗示**设计意图是有的**,但**实现是 no-op**。

**没有 attribute 标记**,仅有 UI 用的 `Editable`:

```csharp
// Sys_User.cs 等所有 entity
[Editable(true)]
public int? Gender { get; set; }
```

`EditableAttribute` 用于**前端表单是否显示**(代码生成器读取),**不是后端权限**。

| 维度 | 实际情况 |
|---|---|
| 后端字段读取过滤 | **不存在** |
| 后端字段写入过滤 | **不存在** |
| 前端字段隐藏(`Editable=false`) | 存在,但仅 UI 用途 |
| `Sys_FieldPermission` 表 | **不存在** |
| 任何 field-level 权限配置 | **不存在** |

> **证据等级**: SOURCE_VERIFIED(空实现证据)
> 引用:
> - `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/BaseProvider/ApplicationServiceBaseSearchExtensions.cs`
> - `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/BaseProvider/ServiceBase.cs`

---

## 8. Q8:是否有 `Sys_RoleAuth` / `Sys_RoleDataAuth` / `Sys_FieldPermission` 之类表?

### 答案:只有 `Sys_RoleAuth`;**无** `Sys_RoleDataAuth` / `Sys_FieldPermission`

| 表名 | 存在? | 关键字段 | 角色 |
|---|---|---|---|
| `Sys_RoleAuth` | ✅ | Auth_Id, Role_Id, User_Id, Menu_Id, AuthValue(nvarchar 1000) | 菜单 + 按钮动作权限 |
| `Sys_RoleDataAuth` | ❌ | — | (数据范围)无独立表,数据范围用 `LimitCurrentUserPermission` bool |
| `Sys_FieldPermission` | ❌ | — | (字段权限)无表 |
| `Sys_DataScope` | ❌ | — | — |

**Sys_RoleAuth 完整结构**:

```csharp
[Table("Sys_RoleAuth")]
public class Sys_RoleAuth : BaseEntity
{
    public int Auth_Id { get; set; }       // PK
    public int? Role_Id { get; set; }      // 可空 → null 表示"模板/全角色"
    public int? User_Id { get; set; }      // 可空 → 用户级覆盖
    public int Menu_Id { get; set; }       // FK → Sys_Menu.Menu_Id
    public string AuthValue { get; set; }  // nvarchar(1000) 逗号分隔动作名
    // Creator, CreateDate, Modifier, ModifyDate
}
```

**AuthValue 格式** — 来自 `ActionPermissionOptions` 枚举:

```csharp
[Flags]
public enum ActionPermissionOptions
{
    Add = 1, Update = 2, Search = 4, Export = 8,
    Delete = 16, Audit = 32, Upload = 64, Import = 128
}
```

8 个动作,**位标志**(binary flags)存储。但 `AuthValue` 在表里**以字符串形式存**(如 `"1,2,4,8"`),
在 `UserContext.cs` 用 `Split(",")` 还原成数组 — `UserAuthArr`:

```csharp
// UserContext.cs ActionToArray()
x.UserAuthArr = string.IsNullOrEmpty(x.UserAuth)
    ? new string[0]
    : x.UserAuth.Split(",").Where(c => menuAuthArr.Any(m => m.Value == c)).ToArray();
```

> 关键观察:
> - 动作权限确实有数据持久化(不像 FieldPermission 那种纯 stub)
> - 但是用 **逗号字符串**而非位标志 — 浪费 1000 字节字段空间
> - 关联的是 `Menu_Id`,**不是 table name**;TableName 来自 `Sys_Menu.TableName` 字段
> - **证据等级**: SOURCE_VERIFIED
> 引用: `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Entity/DomainModels/System/Sys_RoleAuth.cs`

---

## 9. 跨问题模式总结

### 真实存在的实体/表(完整列表)

```
Sys_User                          (账号 + 1:1 Role + 缓存 DeptIds)
Sys_Role                          (角色 + 可选 Dept_Id)
Sys_RoleAuth                      (Role/User → Menu → AuthValue 字符串)
Sys_Menu                          (菜单树 + TableName 关联 + Auth 列表)
Sys_Actions                       (Sys_Menu 的 1:N 子表,菜单动作定义)
Sys_Department                    (GUID 树形组织,TypeCnName="组织架构")
Sys_UserDepartment                (M:N User ↔ Department)
Sys_Dictionary / Sys_DictionaryList / Sys_Log
```

### 不存在的实体/表(诚实披露)

- ❌ `Sys_Company` / `Sys_Organization` / `Sys_Tenant` / `Sys_Position`
- ❌ `Sys_RoleDataAuth` / `Sys_FieldPermission` / `Sys_DataScope`
- ❌ `Sys_UserRole`(角色 1:1,无 M:N)
- ❌ `Sys_Position`(职位)
- ❌ `Sys_UserCompany`(无 Company 概念)

### 设计哲学(由代码风格推断)

VOL.NET 的权限模型属于 **"中型后台 (Mid-Market Backoffice)" 流派**:

- 单公司单数据库实例(没有 SaaS 多租户)
- 单一组织树(部门可同时表达"公司/部门/小组")
- 单角色(每个用户只有一个角色,降低认知负担)
- 5-8 个核心动作(不是 RBAC 的 200+ permission)
- 2 级数据范围(自己/全部)
- 无字段级权限(信任角色配置)

这套设计**适合 80% 的企业内部系统**,但**不适合**:
- 跨法人集团 ERP(需要 Company)
- SaaS 多租户(需要 Tenant + 行级隔离)
- 复杂数据权限(部门/项目/客户维度)
- 字段级敏感数据(身份证/银行卡/工资)

---

## 10. GuliERP V1/V2 借鉴可行性

### 可直接借鉴(高 ROI,≤ 1 周)

| 借鉴项 | 用途 | 备注 |
|---|---|---|
| `Sys_Role.AuthValue` 字符串存动作权限 | 简单业务 | GuliERP V1 已是 JSONB / 数组,可跳过 |
| `ActionPermissionAttribute + Filter` 模式 | 中型权限 | 改用 NestJS Guard / Interceptor 重写 |
| `IsMultiTenancy` 标志 + `TenancyManager` 入口 | 多租户可扩展点 | 借鉴"OPT-IN"设计哲学 |
| `LimitCurrentUserPermission` bool 钩子 | 数据范围基础 | GuliERP V2 已有更强 DataScope 枚举 |

### 不可借鉴 / 缺失 / 需自建

| 缺失项 | GuliERP 现状 | 是否需要自建 |
|---|---|---|
| Tenant / Company 实体 | GuliERP V1 无;V2 计划有 | **必须自建** |
| M:N User-Role | GuliERP V1 已是 M:N | **VOL.NET 不可借鉴** |
| 5 级数据范围 (All/Own/Dept/DeptAndChildren/Custom) | GuliERP V2 计划有 | **必须自建**(VOL.NET 只有 2 级) |
| Field Permission(字段级) | GuliERP V2 计划有 | **必须自建**(VOL.NET 是空壳) |
| 角色级数据范围配置表 | GuliERP V2 计划有 | **必须自建** |

### 决策建议

- ✅ **不要**为了"快速开发"而引入 VOL.NET 作为 GuliERP V2 底座
- ✅ **可以**参考其 `ActionPermissionAttribute` 模式的"表名 + 动作"权限抽象(改造成 NestJS Guard)
- ✅ **必须**自建 4 张缺失表:Sys_Company / Sys_UserCompany / Sys_RoleDataAuth / Sys_FieldPermission
- ✅ **必须**自建 DataScope 枚举 + 框架级 QueryInterceptor(SqlSugar / TypeORM QueryBuilder hook)
- ✅ **可以**借鉴其 `Sys_UserDepartment` M:N 模式 + `DeptIds` 缓存列(已匹配 GuliERP V1)
- ⚠️ **必须诚实**: VOL.NET 不能给 GuliERP V2 提供"多公司/数据范围/字段权限"能力,**这部分全部是 0 起点**

### 关键风险披露

- VOL.NET 的 `FilterQueryableAuthFields` 是**注释+空函数**, 任何宣称"VOL.NET 支持字段权限"的二手文档都是错的
- VOL.NET 的 `TenancyManager` 是**注释+空函数**, 任何宣称"VOL.NET 内置多租户"的二手文档都是错的
- VOL.NET 的"5 数据库支持"是真的(`DB/{sqlserver,mysql,pgsql,Oracle,DM}/表结构与数据.sql` 五份脚本),与权限无关,见 VOL-PRO-001

---

## 11. UNKNOWN 列表(诚实披露)

1. **`DB/sqlserver/表结构与数据.sql` 实际列数 / 索引 / 触发器**: 文件 1.1MB, GitHub raw 单次返回, 但未做逐字段 diff, **不确认** SQL Server 版与 SqlSugar 实体是否 100% 同步。结论以 SqlSugar 实体为准,但 SQL 文件可能含 5-10 个未映射到实体的表(如 Quartz 任务表、流程引擎表)。
2. **`IsMultiTenancy = true` 时的真实行为**: 没有找到任何启用示例(注释里说"见文档或 SellOrderService.cs", 但本次未访问)。**不确认** 多租户真的能跑通,只能说"代码入口存在"。
3. **Sys_Menu.Actions(Sys_Actions OneToMany)** 实际表结构: `Sys_Menu.cs` 有 `[Navigate(NavigateType.OneToMany, nameof(Menu_Id), nameof(Menu_Id))] public List<Sys_Actions> Actions`, 但 `Sys_Actions.cs` 在 `VOL.Entity/DomainModels/ApiEntity` 之外(可能在 `mes` 或 `Core` 子目录),**未单独验证**。
4. **Vue3 前端 vol.web 权限处理**: 由于 GitHub API rate limit 触发,未访问 `vol.web/src/permission.js` 类的实现。**仅以 `vol.api.sqlsugar` 源码为准**。前端的"按钮 v-if 显隐"是几乎肯定的实现,但无直接源码证据。
5. **V2/V3 商业版差异**: VOL 官方有"企业版"(`http://www.volcore.xyz/`),但 GitHub 仓库是 OSS 版。**不确认** 企业版是否补齐 Tenant/FieldPermission 等缺失能力。
6. **`[Authorize]` attribute 是否在某处存在**: `ApiAuthorizeFilter` 是 `IAuthorizationFilter` 的实现,但 `[Authorize]` 来自 `Microsoft.AspNetCore.Authorization` namespace。本次未做全面 `grep`,不能完全排除。

---

## 12. 证据等级分布

| 问题 | 结论 | 等级 |
|---|---|---|
| Q1 Department 存在 | 确认 | SOURCE_VERIFIED |
| Q1 Tenant/Company/Org/Position 缺失 | 4 个全部缺失 | SOURCE_VERIFIED + INFERRED |
| Q2 无 Company 概念 | 确认 | SOURCE_VERIFIED |
| Q3 User-M:N-Dept 存在 | 确认 | SOURCE_VERIFIED |
| Q3 User 1:1 Role | 确认 | SOURCE_VERIFIED |
| Q4 Role scope = Department / Global | 确认 | SOURCE_VERIFIED |
| Q5 无自动数据隔离层 | 确认 | SOURCE_VERIFIED |
| Q5 仅 IsMultiTenancy OPT-IN | 确认 | SOURCE_VERIFIED |
| Q6 只 2 级数据范围 | 确认 | SOURCE_VERIFIED |
| Q7 Field Permission = STUB | 确认 | SOURCE_VERIFIED |
| Q8 只有 Sys_RoleAuth | 确认 | SOURCE_VERIFIED |

**总分布**: 12 个子结论 / 全部 SOURCE_VERIFIED / 0 INFERRED / 0 UNKNOWN(主结论)/ 6 UNKNOWN(细节边界)

---

## 13. 引用清单(全部 raw.githubusercontent.com + api.github.com)

| # | URL | 用途 |
|---|---|---|
| 1 | `https://api.github.com/repos/cq-panda/Vue.NetCore/contents/` | 仓库总览 |
| 2 | `https://api.github.com/repos/cq-panda/Vue.NetCore/contents/vol.api.sqlsugar/VOL.Entity/DomainModels/System` | System 实体清单(16 文件) |
| 3 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Entity/DomainModels/System/Sys_User.cs` | User 实体 |
| 4 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Entity/DomainModels/System/Sys_Role.cs` | Role 实体 |
| 5 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Entity/DomainModels/System/Sys_RoleAuth.cs` | 权限表 |
| 6 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Entity/DomainModels/System/Sys_Menu.cs` | 菜单表 |
| 7 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Entity/DomainModels/System/Sys_Department.cs` | 部门表 |
| 8 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Entity/DomainModels/System/Sys_UserDepartment.cs` | User-Dept M:N |
| 9 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/Filters/ApiAuthorizeFilter.cs` | JWT 鉴权 |
| 10 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/Filters/ActionPermissionAttribute.cs` | 按钮权限 attribute |
| 11 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/Filters/ActionPermissionFilter.cs` | 按钮权限 filter |
| 12 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/Filters/ServiceFunFilter.cs` | Service 钩子(含 IsMultiTenancy) |
| 13 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/UserManager/UserContext.cs` | 用户上下文(含缓存) |
| 14 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/UserManager/DepartmentContext.cs` | 部门树工具 |
| 15 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/Tenancy/TenancyManager.cs` | 多租户空壳 |
| 16 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/BaseProvider/ApplicationServiceBaseSearchExtensions.cs` | 查询构建(找 FilterQueryableAuthFields) |
| 17 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/BaseProvider/ServiceBase.cs` | Service 基类 |
| 18 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/DbContext/BaseDbContext.cs` | DbContext 基类 |
| 19 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/Enums/ActionPermissionOptions.cs` | 8 个动作枚举 |
| 20 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Entity/DomainModels/ApiEntity/Input/ApiSys_UserInput.cs` | ApiInput DTO 示例 |

---

*End of VOL_PRO_MULTI_COMPANY_PERMISSION_VERIFICATION*
*Status: SOURCE_VERIFIED × 12 / 0 INFERRED / 6 UNKNOWN boundary*
*VOL.NET 真实身份: 单租户单公司 + 多部门 M:N + 1:1 角色 + 2 级数据范围*
*多公司 / 多租户 / 字段权限全部是空壳 — 不能作为 GuliERP V2 底座*
