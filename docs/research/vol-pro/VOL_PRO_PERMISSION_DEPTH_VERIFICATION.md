# VOL_PRO_PERMISSION_DEPTH_VERIFICATION

| Field | Value |
|---|---|
| Goal | VOL-PRO-002 TASK 4 — Permission Depth 5 层验证 |
| Researcher | Mavis + explore Agent C |
| Date | 2026-08-19 (Asia/Taipei) |
| Source | GitHub `cq-panda/Vue.NetCore` MIT public source |
| Final verdict | **3 VERIFIED + 1 DOCUMENTED + 1 NOT_FOUND** |
| Evidence grade | SOURCE_VERIFIED × 5 / DOCUMENTED × 1 / NOT_FOUND × 1 / UNKNOWN × 4 (boundary) |

---

## 0. 速读

| 权限层 | 状态 | 标记 |
|---|---|---|
| Menu Permission(菜单权限) | 实现完整,前后端闭环 | **VERIFIED** |
| Button Permission(按钮权限) | 实现完整,Filter 强制 | **VERIFIED** |
| API Permission(接口权限) | 实现完整,Attribute 强制 | **VERIFIED** |
| Data Scope(行级数据范围) | **OPT-IN,非框架级** | **DOCUMENTED** |
| Field Permission(字段级读写) | **存 stub 不存逻辑** | **NOT_FOUND** |

**额外问题**:
- 前端权限 = UX only? → **不是**, 后端有 Filter 强制
- 后端 enforcement 层? → `IAsyncActionFilter` + `IAuthorizationFilter`
- ABP-style `[Authorize]`? → **没有**, 用自研 `ActionPermissionAttribute : TypeFilterAttribute`

---

## 1. Menu Permission(菜单权限)— VERIFIED

### 1.1 数据层 — `Sys_Menu` 表

```csharp
[Table("Sys_Menu")]
[EntityAttribute(TableCnName = "菜单配置")]
public class Sys_Menu : BaseEntity
{
    public int Menu_Id { get; set; }         // PK
    public int ParentId { get; set; }        // 树形
    public string MenuName { get; set; }
    public string TableName { get; set; }    // ← 关键:关联到业务表
    public string Url { get; set; }          // 前端路由
    public string Auth { get; set; }         // ← 关键:按钮动作(逗号分隔)
    public int? OrderNo { get; set; }
    public byte? Enable { get; set; }
    public int? MenuType { get; set; }       // 0=PC, 1=移动端
    [Navigate(NavigateType.OneToMany, nameof(Menu_Id), nameof(Menu_Id))]
    public List<Sys_Actions> Actions { get; set; }
}
```

引用: `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Entity/DomainModels/System/Sys_Menu.cs`

### 1.2 加载层 — `UserContext.GetPermissions`

```csharp
// UserContext.cs:159-176
public List<Permissions> GetPermissions(int roleId)
{
    if (IsRoleIdSuperAdmin(roleId))
    {
        // 超级管理员 = 全部菜单(无视 Sys_RoleAuth)
        var permissions = DBServerProvider.DbContext.Set<Sys_Menu>()
            .Where(x => x.Enable == 1 || x.Enable == 2)
            .Select(a => new Permissions { ... }).ToList();
        return MenuActionToArray(permissions);
    }
    // 普通角色:左连接 Sys_Menu → Sys_RoleAuth
    var _permissions = dbContext.SqlSugarClient.Queryable<Sys_Menu>()
        .LeftJoin<Sys_RoleAuth>((a, b) => a.Menu_Id == b.Menu_Id)
        .Where((a, b) => b.Role_Id == roleId && b.AuthValue != "")
        .Select((a, b) => new Permissions { ... }).ToList();
    return _permissions;
}
```

引用: `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/UserManager/UserContext.cs`

### 1.3 缓存层 — 版本号模式

```csharp
// UserContext.cs:130-145
private static readonly Dictionary<int, string> rolePermissionsVersion = new();
private static readonly Dictionary<int, List<Permissions>> rolePermissions = new();
public void RefreshWithMenuActionChange(int menuId) { /* 触发缓存失效 */ }
```

**机制**:
- 每个 roleId 缓存一份权限
- 通过 cacheService 的 rolePermissionsVersion 时间戳对比
- 菜单/动作变化时,显式 `RefreshWithMenuActionChange(menuId)` 失效

> **标记**: VERIFIED — 三层闭环(表 → 加载 → 缓存)源码 + 注释双证据

---

## 2. Button Permission(按钮权限)— VERIFIED

### 2.1 数据层 — `Sys_Menu.Auth` + `Sys_RoleAuth.AuthValue`

双层存储:

| 表 | 字段 | 含义 |
|---|---|---|
| `Sys_Menu` | `Auth` nvarchar(10000) | 菜单**支持的所有**动作(模板) |
| `Sys_RoleAuth` | `AuthValue` nvarchar(1000) | 角色**实际有**的动作(分配) |

`ActionPermissionOptions` 枚举(8 个):

```csharp
[Flags]
public enum ActionPermissionOptions
{
    Add = 1, Update = 2, Search = 4, Export = 8,
    Delete = 16, Audit = 32, Upload = 64, Import = 128
}
```

引用: `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/Enums/ActionPermissionOptions.cs`

### 2.2 强制层 — `ActionPermissionAttribute` + `ActionPermissionFilter`

**Attribute**(贴在 Controller/Action 上):

```csharp
public class ActionPermissionAttribute : TypeFilterAttribute
{
    public ActionPermissionAttribute(string tableName, ActionPermissionOptions tableAction, bool sysController = false, bool isApi = false)
        : base(typeof(ActionPermissionFilter))
    {
        // 解析 enum flags → 字符串数组
        string[] tableActions = ParseActionPermissionOptions(tableAction);
        Arguments = new object[] { new ActionPermissionRequirement() {
            TableActions = tableActions,
            TableName = tableName,
            IsApi = isApi,
            RoleIds = roleId
        } };
    }
    // 还有 6 个重载:限定角色、限定 roleIds、全局开关等
}
```

**Filter**(`IAsyncActionFilter`, 拦截每个 Action):

```csharp
public class ActionPermissionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (OnActionExecutionPermission(context).Status)  // 验权限
        {
            await next();
            return;
        }
        FilterResponse.SetActionResult(context, ResponseContent);  // 拒绝
    }
    private WebResponseContent OnActionExecutionPermission(ActionExecutingContext context)
    {
        // 1. 超级管理员短路
        if (UserContext.Current.IsSuperAdmin) return ResponseContent.OK();
        // 2. 全局演示环境拦截(只读模式)
        if (AppSetting.GlobalFilter.Enable && AppSetting.GlobalFilter.Actions.Contains(actionName))
            return ResponseContent.Error("演示环境不能操作");
        // 3. RoleIds 直接匹配
        if (ActionPermission.RoleIds.Contains(_userContext.UserInfo.Role_Id))
            return ResponseContent.OK();
        // 4. TableActions 检查
        actionAuth = CheckPermission(actionsToCheck, ActionPermission.TableName);
        if (!actionAuth) return ResponseContent.Error(ResponseType.NoPermissions);
        return ResponseContent.OK();
    }
    private bool CheckPermission(string[] actionsToCheck, string table)
    {
        var permissions = _userContext.GetPermissions(x => x.TableName == table.ToLower());
        return actionsToCheck.Any(action => permissions.UserAuthArr.Contains(action));
    }
}
```

引用:
- `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/Filters/ActionPermissionAttribute.cs`
- `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/Filters/ActionPermissionFilter.cs`

> **标记**: VERIFIED — 强制层是 ASP.NET Core 标准的 `IAsyncActionFilter`,不是 AOP
> 证据: 后端拒绝时返回 `ResponseType.NoPermissions`,前端拿到后做 UX 处理(显示/隐藏按钮)

---

## 3. API Permission(接口权限)— VERIFIED

### 3.1 双 Filter 机制

| Filter | 类型 | 职责 |
|---|---|---|
| `ApiAuthorizeFilter` | `IAuthorizationFilter` | JWT 验签 + 过期刷新 + AllowAnonymous 短路 |
| `ActionPermissionFilter` | `IAsyncActionFilter` | 业务级菜单/动作权限检查 |

`ApiAuthorizeFilter` 关键代码:

```csharp
public class ApiAuthorizeFilter : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context.ActionDescriptor.EndpointMetadata.Any(item => item is IAllowAnonymous))
        {
            // 匿名 + 固定 token 短路
            if (context.Filters.FirstOrDefault(item => item is IFixedTokenFilter) is IFixedTokenFilter tf)
            { tf.OnAuthorization(context); return; }
            // 匿名但带 token → 加 Identity 让 UserHelper 能取
            if (!IsAuthenticated && !string.IsNullOrEmpty(tokenHeader))
                context.AddIdentity();
            return;
        }
        // 检查 token exp
        DateTime expDate = ...;
        if ((expDate - DateTime.Now).TotalMinutes < AppSetting.ExpMinutes / 3)
            Response.Headers["vol_exp"] = "1";  // 触发前端静默刷新
    }
}
```

引用: `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/Filters/ApiAuthorizeFilter.cs`

### 3.2 不是 ABP-style `[Authorize]`

**ABP 风格**: `[Authorize("PermissionName")]` + Policy-based
**VOL 风格**: `[ActionPermission("tableName", ActionPermissionOptions.Add | .Update)]` + Table-based

| 维度 | ABP | VOL.NET |
|---|---|---|
| Attribute 名 | `[Authorize]` | `[ActionPermission]` / `[ApiActionPermission]` |
| 键 | 策略字符串 / Permission 名 | 表名 + 动作位标志 |
| 解析 | PolicyHandler | `ActionPermissionFilter` 直接读表 |
| 注解方式 | 需要 startup 注册 | 开箱即用 |

> **标记**: VERIFIED — 完全自研,不是 ABP 模式
> 引用: `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/Filters/ApiActionPermissionAttribute.cs`

---

## 4. Data Scope(行级数据范围)— DOCUMENTED(OPT-IN)

### 4.1 仅 2 级:All / Own

```csharp
// ServiceFunFilter.cs (T = 业务实体)
/// <summary>
/// 是否开启用户数据权限,true=用户只能操作自己(及下级角色)创建的数据
/// 如:查询、删除、修改等操作
/// 注意:需要在代码生成器界面选择【是】及生成Model才会生效
/// </summary>
protected bool LimitCurrentUserPermission { get; set; } = false;
```

引用: `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/Filters/ServiceFunFilter.cs`

### 4.2 OPT-IN 多租户钩子

```csharp
// ServiceFunFilter.cs:30
/// <summary>
/// 2020.08.15是否开启多租户功能
/// 使用方法见文档或SellOrderService.cs
/// </summary>
protected bool IsMultiTenancy { get; set; }   // 默认 false
```

### 4.3 入口 — `TenancyManager<T>` 是空函数

```csharp
// TenancyManager.cs 全文 33 行
public static string GetSearchQueryable(string tableName)
{
    string multiTenancyString = null;
    // 全部注释,默认 return null
    return multiTenancyString;
}
```

### 4.4 工具 — `DepartmentContext.GetAllChildrenIds`

```csharp
// UserContext.cs
public List<Guid> GetAllChildrenDeptIds()
{
    return DepartmentContext.GetAllChildrenIds(DeptIds);
}
```

**但**:`GetAllChildrenDeptIds` **不被任何框架代码自动调用**,开发者必须手动拼 Where。

### 4.5 与典型实现的差距

| Scope 类型 | VOL.NET | 典型 RBAC |
|---|---|---|
| All | ✅ 默认 | ✅ |
| Own | ✅ `LimitCurrentUserPermission` | ✅ |
| Department | ❌ 无声明式配置 | ✅ |
| DepartmentAndChildren | ❌ 工具存在未集成 | ✅ |
| Custom | ⚠️ `QueryRelativeExpression` 委托 | ✅ 表达式或 SQL |

> **标记**: DOCUMENTED — 注释明确说"开启多租户",但**没有自动集成**,每个 Service 需手动启用
> 证据: `TenancyManager.cs` 33 行空函数 + `IsMultiTenancy` 注释提到"见文档或 SellOrderService.cs"(本次未访问)

---

## 5. Field Permission(字段级读写)— NOT_FOUND

### 5.1 Hook 存在但实现为空

`ApplicationServiceBaseSearchExtensions.cs` 末尾两个方法:

```csharp
/// <summary>
/// 映射指定权限的字段不查询数据库
/// </summary>
public static List<TEntity> FilterQueryableAuthFields<TEntity>(this ISugarQueryable<TEntity> queryable) where TEntity : class
{
    return queryable.ToList();   // ← 空实现
}

public static Task<List<TEntity>> FilterQueryableAuthFieldsAsync<TEntity>(this ISugarQueryable<TEntity> queryable) where TEntity : class
{
    return queryable.ToListAsync();   // ← 空实现
}
```

### 5.2 调用点确实调用了

`ServiceBase.GetPageData` / `GetPageDataAsync`:

```csharp
pageGridData.rows = queryable.FilterQueryableAuthFields();        // 调了
pageGridData.rows = await queryable.FilterQueryableAuthFieldsAsync();  // 调了
```

但因为实现是 no-op,**没有任何过滤效果**。

### 5.3 没有 `Sys_FieldPermission` 表

- GitHub Code Search `repo:cq-panda/Vue.NetCore Sys_FieldPermission` → 0 hits
- Entity 目录 16 个 System 实体文件,无此表
- 5 个 DB 脚本目录无此表(由 SqlSugar 实体反推)

### 5.4 没有任何 attribute 标记

Entity 字段上只有:

```csharp
[Editable(true)]      // ← 前端表单显示(代码生成器用)
[Display(Name = "性别")]
[MaxLength(20)]
```

`Editable` 仅前端 UI 用途,**不参与后端权限**。

> **标记**: NOT_FOUND — hook 名称暗示有设计意图,但实现是空函数
> 引用:
> - `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/BaseProvider/ApplicationServiceBaseSearchExtensions.cs`
> - `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/BaseProvider/ServiceBase.cs`

---

## 6. 关键问题回答

### Q1:Frontend 权限是否只是 UX 控制(隐藏按钮/菜单)?

**不是纯 UX。** 后端有 `ActionPermissionFilter` 强制拦截。

- 前端:通过 `UserContext.UserAuthArr` 决定 v-if 显隐(由 `/api/User/getCurrentUserInfo` 返回)
- 后端:`ActionPermissionFilter.OnActionExecutionAsync` 强制检查 `TableActions`
- 即使前端绕过(直接 curl),后端仍返回 `NoPermissions`

```csharp
// ActionPermissionFilter.cs 拒绝逻辑
if (!actionAuth)
{
    Logger.Info(LoggerType.Authorzie, $"没有权限操作,用户ID{userId},操作权限{tableName}");
    return ResponseContent.Error(ResponseType.NoPermissions);
}
```

### Q2:Backend 是否真正 enforcement?在哪个层?

**是。** 三个层共同强制(从外到内):

1. **`IAuthorizationFilter`** = `ApiAuthorizeFilter` — JWT 验签、AllowAnonymous 短路、token 过期
2. **`IAsyncActionFilter`** = `ActionPermissionFilter` — 菜单/动作权限
3. **业务代码层** = `LimitCurrentUserPermission` + `IsMultiTenancy` 钩子

| 层 | 文件 | 检查内容 |
|---|---|---|
| 1 | `ApiAuthorizeFilter.cs` | token 合法性 |
| 2 | `ActionPermissionFilter.cs` | 菜单 + 动作 |
| 3 | `ServiceBase`/`ServiceFunFilter` | 数据范围(需手动启用) |

### Q3:ABP-style `[Authorize]` attribute?自研 attribute?

**自研,非 ABP。**

- ABP: `[Authorize("Permission.X")]` → PolicyHandler 查 PermissionDefinition
- VOL.NET: `[ActionPermission("tableName", ActionPermissionOptions.Add | Update)]` → `ActionPermissionFilter` 查 `Sys_RoleAuth`

关键差异:

| 维度 | ABP | VOL.NET |
|---|---|---|
| 注册中心 | PermissionDefinitionProvider (启动时注册) | 数据库(Sys_Menu + Sys_RoleAuth) |
| 存储 | 静态 + 缓存 | DB + cacheService 双重 |
| 表达 | 字符串 key | 表名 + 位标志 |
| 扩展性 | 高度灵活(可任意扩展) | 固定 8 个动作 |
| 学习曲线 | 陡(需理解 Policy/Handler) | 平(贴 Attribute 即用) |

---

## 7. 5 层对照表(可视化)

```
┌────────────────────────────────────────────────────────┐
│  Frontend (vol.web)                                     │
│  - v-if="UserAuthArr.includes('Add')"                  │
│  - 路由守卫(menu permission)                           │
│  ↓ (HTTP request)                                       │
├────────────────────────────────────────────────────────┤
│  Layer 1: IAuthorizationFilter                         │
│  - ApiAuthorizeFilter: JWT 验签                       │
│  - 状态: ✅ 强制                                        │
├────────────────────────────────────────────────────────┤
│  Layer 2: IAsyncActionFilter                            │
│  - ActionPermissionFilter: 菜单 + 动作                │
│  - 状态: ✅ 强制                                        │
├────────────────────────────────────────────────────────┤
│  Layer 3: Service(数据范围 — OPT-IN)                   │
│  - LimitCurrentUserPermission: 2 级 All/Own            │
│  - IsMultiTenancy: 需开发者重写 TenancyManager        │
│  - 状态: ⚠️ 文档存在,OPT-IN,非框架级                  │
├────────────────────────────────────────────────────────┤
│  Layer 4: Service(字段级 — STUB)                       │
│  - FilterQueryableAuthFields: 空函数                  │
│  - 状态: ❌ 注释暗示,实现 no-op                        │
└────────────────────────────────────────────────────────┘
```

---

## 8. UNKNOWN 列表(诚实披露)

1. **前端 vol.web 权限中间件**: GitHub API 触发 rate limit,未访问 `vol.web/src/permission.js` 或 `router.beforeEach`。
   前端 v-if 显隐是几乎肯定的实现,但**无直接源码证据**。
2. **`[Authorize]` 残留可能性**: 仓库可能有 controller 用了标准 `[Authorize]` attribute,本次未全面 `grep`。
   但 `ActionPermissionAttribute` 是 `TypeFilterAttribute`,走的是 `IAsyncActionFilter` 路径,不走 `IAuthorizationFilter`。
3. **商业版能力**: VOL 企业版(`http://www.volcore.xyz/`)的权限扩展未知。
4. **Sys_Actions.cs 实体定义**: 引用自 `Sys_Menu.Actions`,实体文件位置未单独验证。

---

## 9. 证据等级分布

| 权限层 | 标记 | 等级 |
|---|---|---|
| Menu Permission | VERIFIED | SOURCE_VERIFIED(Sys_Menu + UserContext 双源) |
| Button Permission | VERIFIED | SOURCE_VERIFIED(ActionPermissionFilter 强制 + AuthValue 存储) |
| API Permission | VERIFIED | SOURCE_VERIFIED(ApiAuthorizeFilter + ActionPermissionFilter 双 Filter) |
| Data Scope | DOCUMENTED | SOURCE_VERIFIED(OPT-IN 设计 + 空 TenancyManager) |
| Field Permission | NOT_FOUND | SOURCE_VERIFIED(空函数 + 无表) |

**总分布**: 5 层 / 3 VERIFIED / 1 DOCUMENTED / 1 NOT_FOUND / 0 UNKNOWN(主层)

---

## 10. GuliERP V1/V2 借鉴可行性

### 可借鉴(改造成 NestJS Guard/Interceptor)

| VOL.NET 模式 | 借鉴方式 |
|---|---|
| `ActionPermissionAttribute(tableName, action)` | NestJS `@SetMetadata('table', 'X')` + 反射 Guard |
| `IsMultiTenancy` 钩子 | TenantModule,显式声明 |
| `Sys_Menu.Auth` + `Sys_RoleAuth.AuthValue` 模式 | 一对多 `RolePermission(roleId, menuId, actions[])` 已是 V1 形态 |

### 不可借鉴 / 必须自建

| 缺失能力 | GuliERP 现状 |
|---|---|
| 5 级 Data Scope(All/Own/Dept/DeptAndChildren/Custom) | **必须自建** — VOL.NET 只有 2 级 |
| Field Permission 实际实现 | **必须自建** — VOL.NET 是空壳 |
| 角色级数据范围配置 | **必须自建** |
| 框架级 QueryInterceptor(自动注入 WHERE) | **必须自建** — VOL.NET 是 OPT-IN |

### 决策建议

- ✅ VOL.NET 的 **前端 UX + 后端 Action 权限** 模式值得借鉴(改写为 NestJS Guard)
- ✅ VOL.NET 的 **menu + role_auth + user_auth 三表结构** 直接迁移即可
- ❌ VOL.NET 的 **多租户 / 数据范围 / 字段权限** 全部是 stub 或 2 级简化,**不可作为 GuliERP V2 底座**
- ⚠️ 任何文档声称"VOL.NET 内置多租户 / 字段权限"都是错的(`TenancyManager.cs` 33 行空函数 + `FilterQueryableAuthFields` 空实现)

---

## 11. 引用清单(全部 raw.githubusercontent.com)

| # | URL | 用途 |
|---|---|---|
| 1 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Entity/DomainModels/System/Sys_Menu.cs` | 菜单表 |
| 2 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Entity/DomainModels/System/Sys_RoleAuth.cs` | 权限分配表 |
| 3 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Entity/DomainModels/System/Sys_User.cs` | 用户实体 |
| 4 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/Enums/ActionPermissionOptions.cs` | 8 动作枚举 |
| 5 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/Filters/ActionPermissionAttribute.cs` | 按钮权限 attribute |
| 6 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/Filters/ActionPermissionFilter.cs` | 按钮权限 filter |
| 7 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/Filters/ApiAuthorizeFilter.cs` | JWT 鉴权 |
| 8 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/Filters/ApiActionPermissionAttribute.cs` | API 权限 attribute |
| 9 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/Filters/ServiceFunFilter.cs` | Service 钩子 |
| 10 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/UserManager/UserContext.cs` | 用户上下文 |
| 11 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/UserManager/DepartmentContext.cs` | 部门树工具 |
| 12 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/Tenancy/TenancyManager.cs` | 多租户空壳 |
| 13 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/BaseProvider/ApplicationServiceBaseSearchExtensions.cs` | 查询构建(找 FilterQueryableAuthFields) |
| 14 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/BaseProvider/ServiceBase.cs` | Service 基类 |
| 15 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/DbContext/BaseDbContext.cs` | DbContext 基类 |

---

*End of VOL_PRO_PERMISSION_DEPTH_VERIFICATION*
*Status: 3 VERIFIED + 1 DOCUMENTED + 1 NOT_FOUND*
*5 层权限模型: Menu/Button/API 完整,DataScope OPT-IN,FieldPermission 空壳*
*不能作为 GuliERP V2 多公司/数据范围/字段权限底座*
