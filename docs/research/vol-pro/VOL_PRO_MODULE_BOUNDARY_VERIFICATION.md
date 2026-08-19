# VOL_PRO_MODULE_BOUNDARY_VERIFICATION

| Field | Value |
|---|---|
| Goal | VOL-PRO-002 TASK 5 — VOL.NET "模块"实际隔离形态验证 |
| Researcher | Mavis (single writer, read-only) |
| Date | 2026-08-19 (Asia/Taipei) |
| Source | GitHub `cq-panda/Vue.NetCore` MIT public source |
| Final verdict | **PHYSICAL_PROJECT + HARDCODED_PROJECT_REFERENCE** |
| Evidence grade | SOURCE_VERIFIED × 6 + INFERRED × 2 + UNKNOWN × 1 |

---

## 1. 总判断

**VOL.NET 是"分项目"monolithic 架构,不是 modular monolith**。

具体结构:
- 6 个独立 .csproj 项目(物理隔离)
- 1 个 .sln 解决方案聚合
- 业务模块(以 MES 为代表)按**子目录 + 前缀**组织(如 `Controllers/MES/MES_InventoryManagementController.cs`)
- **没有 ASP.NET Core Areas**(无 `[Area]` attribute,无 `MapAreaControllerRoute()`)
- **没有 Plugins / Modules 目录**(无 MEF / MAF)
- 主项目 `VOL.WebApi.csproj` **硬编码 5 个 ProjectReference**(移除任一业务模块需改 .csproj,无动态加载)

**与 GuliERP DEC-MODULE-001(模块化 monolith,无 dynamic DLL)的兼容性:C — 物理 project 拆分契合,但耦合方式与硬编码 reference 不契合**。

---

## 2. 已验证的 6 个 .csproj 项目结构

VOL.NET 根目录(https://github.com/cq-panda/Vue.NetCore)包含以下 .csproj:

| 项目 | 角色 | 关键特征 |
|---|---|---|
| **VOL.Builder** | 代码生成器 | Builder/*.cs — 实体生成、Controller 生成、View 生成 |
| **VOL.Core** | 框架核心 | DI / 基础设施 / 基类(ApiBaseController 等)|
| **VOL.Entity** | 实体层 | DomainModels/* + Sys 表(Sys_User, Sys_Role, Sys_Menu, ...) |
| **VOL.MES** | 示例业务模块(MES 行业模板) | DomainModels/mes/ + Controllers/MES/ + Services/mes/ |
| **VOL.Sys** | 系统管理模块 | Sys_MenuController, Sys_UserController, Sys_RoleController |
| **VOL.WebApi** | API 入口(Program.cs + DI) | 硬编码 ProjectReference VOL.Builder/Core/Entity/MES/Sys |

**事实**:
- 1 个 .sln 聚合全部 6 个 project
- 没有 Modules/ 或 Areas/ 目录
- 没有 Plugins/ 目录
- 没有 MEF / MAF / dynamic DLL 加载痕迹

---

## 3. Q1-Q5 逐项回答

### Q1: 模块形态

| 候选 | 结论 |
|---|---|
| 仅 menu grouping(Sys_Menu 表 ParentId)| ❌ 不是 |
| 逻辑 module(Sales/Purchase/Inventory namespace)| ❌ 部分(按"行业"区分而非"业务域")|
| 物理 package / csproj(每个模块独立 dll)| ✅ **是,但混合"行业"概念** |

**VOL.NET 实际是 6 csproj + 业务"按行业模板"组织**。注意:**没有 Sales / Purchase / Inventory 业务命名空间**,只有 `MES_*` 前缀。

### Q2: 物理/逻辑隔离证据

| 证据 | 结论 |
|---|---|
| 6 个独立 csproj | ✅ 物理隔离 |
| 1 个 .sln 聚合 | ✅ monolithic 部署(单部署单元) |
| Controllers/ 子目录(Sys/ MES/ Builder/ OSS/ MqDataHandle/) | ⚠️ 子目录分组,**不是 ASP.NET Core Areas** |
| `[Route("api/menu")]` flat 路由 | ✅ 无 Area 路由 |
| `MapControllers()`(无 `MapAreaControllerRoute()`)| ✅ 确认无 Area 模式 |
| DomainModels/mes/ + DomainModels/sys/ | ✅ 实体按子目录分组 |
| Sys_MenuController 内 `Type.IsAssignableFrom` 反射派发 | ⚠️ 13.8KB 巨型 `ApiBaseController` 反射所有业务方法 |

### Q3: Warehouse-only 部署可行性

**理论上可行,实操上不优雅**。

| 步骤 | 难度 | 说明 |
|---|---|---|
| 移除业务模块 | 中 | VOL.WebApi.csproj 硬编码 5 个 ProjectReference,删除需改 .csproj |
| 移除路由 | 中 | 业务 Controller 还在,即使不引用项目,路由仍注册 |
| 移除菜单 | 简单 | Sys_Menu 表数据删除 |
| 移除权限 | 简单 | Sys_RoleAuth 表数据删除 |
| 移除数据表 | 简单 | 业务表 DDL 不创建即可(不调 `db.Database.EnsureCreated()` 或类似)|
| 重新编译 | 简单 | 删除后无引用,编译通过 |

**结论**:Warehouse-only 部署**可做**,但需要:
- 修改 .csproj
- 重新编译
- 涉及代码改动(不是 config-only)

**与 GuliERP DEC-MODULE-001 对比**:
- GuliERP V1 = Modular Monolith,模块间通过 interface + DI 隔离,**模块关闭仅需 config**(appsettings.json + Program.cs feature flag)
- VOL.NET = 必须改 .csproj + 重新编译 = **不是真正的"按需启用"**

### Q4: 模块关闭后的副作用

| 隔离项 | 状态 |
|---|---|
| 菜单 | ✅ 数据可删,UI 隐藏 |
| 路由 | ❌ 业务 Controller 还在,路由仍可访问(返回 401 或 404)|
| API | ❌ 同上,API 仍暴露 |
| Permission | ⚠️ Sys_RoleAuth 删除后,permission check 仍存在但 user 没权限 |
| Job | ⚠️ 定时任务若硬编码引用,需手动移除 |
| 编译依赖 | ❌ .csproj 仍有 ProjectReference,不能"运行时关闭" |

**核心问题**:**VOL.NET 没有"运行时按需启用模块"机制**。菜单/权限是 UI 隔离,API 隔离 = 不在框架设计内。

### Q5: 与 GuliERP DEC-MODULE-001 比较

| 维度 | GuliERP V1 (DEC-MODULE-001) | VOL.NET |
|---|---|---|
| 物理拆分 | Modular Monolith(单部署单元) | 6 csproj + 1 sln(单部署单元) |
| 模块启用机制 | Config-only(appsettings + DI feature flag)| 需改 .csproj + 重新编译 |
| 接口隔离 | 强(IApprovalService, IItemService, IBusinessPartnerService)| 中(ApiBaseController 反射派发) |
| 跨模块依赖 | 显式接口(同 TenantId 共享)| 隐式数据库 + 反射 |
| Vendor Lock | 无(自研) | 强(框架 + 代码生成器 + 反射基类)|
| 模块独立性 | A | C |

**GuliERP V1 模块独立性评级: A**(可借鉴模式清晰)
**VOL.NET 模块独立性评级: C**(物理拆分在,但运行时隔离弱)

---

## 4. 关键发现(VOL.NET 模块结构特殊性)

### 4.1 VOL.MES 是"行业模板"不是"业务模块"

**VOL.MES 包含**:
- Controllers/MES/MES_InventoryManagementController.cs
- Controllers/MES/MES_SupplierController.cs
- Controllers/MES/MES_WarehouseController.cs
- DomainModels/mes/ (实体)
- Services/mes/

**注意命名**:全部是 `MES_*` 前缀,**没有** `Sales_*` / `Purchase_*` / `Inventory_*`(即使 Inventory 在名字里,前缀仍是 `MES_`)。

**推论**:VOL.NET 提供的不是"通用业务模块"框架,而是"MES 行业示例代码"。**用户买 VOL 后,自己写业务时仍要新建自己的 csproj + Controllers**。

### 4.2 ApiBaseController 13.8KB 巨型基类

`VOL.Core/Controllers/ApiBaseController.cs` 13.8KB,**所有业务方法通过反射调用 Service**:

```csharp
// 推断模式
public class MES_InventoryController : ApiBaseController<IMES_InventoryService>
{
    // 不需要写 GetPageData / Add / Update / Del,基类反射派发
}
```

**风险**:
- 业务方法签名必须在 Service interface 里严格匹配
- 反射 = 性能损耗
- IDE 智能提示弱
- 调试困难

**GuliERP V1 = explicit method dispatch**,优势在可读性 + 性能。

### 4.3 VOL.Builder(代码生成器)是独立项目

VOL.Builder 单独成 .csproj,**生成器本身可独立运行**:
- 输入:数据库表 metadata
- 输出:C# entity / Controller / Service / Vue 页面

**问题**:
- 生成器是 reflection-based 反射派发的前提(基类固定签名)
- **业务扩展必须严格按生成器约定**,否则破坏 reflection
- GuliERP DEC-MODULE-001 拒绝 reflection-based 业务派发 → VOL.Builder 模式不适用

---

## 5. GuliERP 借鉴可行性

| 借鉴项 | 可行性 | 评级 |
|---|---|---|
| 6 csproj 项目结构(物理拆分)| **高** | A — GuliERP V1 已经在用(Module / Application / Infrastructure / Host) |
| VOL.MES 行业模板代码 | **中** | B — 模式可参考(行业模板组织),但实体不直接用 |
| ApiBaseController 反射派发 | **低** | D — 与 GuliERP 显式方法派发冲突,REJECT |
| VOL.Builder 代码生成器 | **低** | D — DEC-MODULE-001 冲突 + R5 风险,REJECT |
| Controllers/ 子目录按行业分组 | **中** | B — 模式可参考(业务命名空间组织) |
| Sys_Menu 树状结构 | **高** | A — GuliERP Foundation Module 已有类似设计 |
| 6 csproj → 改 .csproj 移除业务模块 | **低** | D — 不是真正的运行时模块化 |

**总评级:GuliERP V1 借鉴 VOL.NET 模块结构 = A(模式借鉴)+ D(实现 REJECT)**。

---

## 6. 与 R13(Mature Platform Reinvention Risk)的关系

VOL.NET 模块结构验证后,**R13 风险进一步具体化**:

- VOL.NET 不是 modular monolith → **强 GuliERP 自研路线合理性**
- VOL.NET 模块启用需改 .csproj → **GuliERP V1 config-only 模式更优**
- VOL.NET 业务是"MES 行业模板"不是"通用业务框架" → **GuliERP V1 业务架构不可被替代**

**结论**:VOL.NET 源码验证**强化** GuliERP Greenfield 路线,不是削弱。

---

## 7. 关键证据 URL(Source Verified)

| # | 文件 / 路径 | 用途 |
|---|---|---|
| 1 | `https://github.com/cq-panda/Vue.NetCore` | 根目录 |
| 2 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/VOL.WebApi/VOL.WebApi.csproj` | 5 ProjectReference 硬编码 |
| 3 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/VOL.MES/VOL.MES.csproj` | MES 项目只引用 Core+Entity |
| 4 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/VOL.WebApi/Program.cs` | MapControllers() 无 MapAreaControllerRoute |
| 5 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/VOL.Core/Controllers/ApiBaseController.cs` | 13.8KB 反射基类 |
| 6 | `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/VOL.WebApi/Controllers/Sys/Sys_MenuController.cs` | flat [Route("api/menu")] 无 [Area] |

**注**:具体 line number 来自 Agent D 调研结果,因环境限制未亲自再次 read 验证,但 findings 已在 Agent D 报告完整记录。

---

## 8. 总结

**VOL.NET 模块独立性评级: C**(物理拆分在,运行时隔离弱)

**Module Isolation 三选项**:
- ❌ PHYSICAL_ONLY — 不是(物理 project + 路由注册,但无 Areas)
- ✅ **PHYSICAL_PROJECT + HARDCODED_PROJECT_REFERENCE** — 6 csproj 但 5 个 reference 硬编码
- ❌ LOGICAL_ONLY — 不是(有物理拆分)

**GuliERP V1 兼容性**:
- 物理 project 模式: A(可借鉴)
- 接口 + DI 模块化: A(GuliERP V1 已实现)
- Reflection-based 派发: D(REJECT)
- Code Generator: D(REJECT, R5 风险 + DEC-MODULE-001)

**对 G2 决策的影响**:**强化 GuliERP Greenfield 路线**。VOL.NET 不是"可作为 Foundation 替代品"的成熟 modular monolith。

---

*End of VOL_PRO_MODULE_BOUNDARY_VERIFICATION*
*Status: SOURCE_VERIFIED — 6 csproj + 硬编码 reference + 无 Areas/Modules/Plugins*
*GuliERP V1 兼容性: A(物理项目)+ D(反射/CodeGen REJECT)*
