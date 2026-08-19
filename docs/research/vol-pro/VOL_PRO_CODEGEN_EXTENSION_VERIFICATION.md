# VOL_PRO_CODEGEN_EXTENSION_VERIFICATION

| Field | Value |
|---|---|
| Goal | VOL-PRO-002 TASK 2 — CodeGen Extension Model 源码验证 |
| Researcher | Mavis + explore Agent B |
| Date | 2026-08-19 (Asia/Taipei) |
| Source | GitHub `cq-panda/Vue.NetCore` MIT public source |
| Final verdict | **RISKY for GuliERP** |
| Evidence grade | SOURCE_VERIFIED × 11 + DOCUMENT_VERIFIED × 4 + INFERRED × 2 + UNKNOWN × 3 |

---

## 证据等级分布(顶部摘要)

| 等级 | 数量 | 含义 |
|---|---|---|
| **SOURCE_VERIFIED** | 11 | 直接读 `raw.githubusercontent.com/cq-panda/Vue.NetCore/master/...` 源码 |
| **DOCUMENT_VERIFIED** | 4 | 官方 qubcedu 论坛 + README + 博客园 + v2 文档站描述 |
| **INFERRED** | 2 | 由源码结构合理推断(generator UI 入口 / 整体覆盖行为) |
| **UNKNOWN** | 3 | 仓库未提供,需要本地实测才能确认 |

| 类别 | SOURCE | DOCUMENT | INFERRED | UNKNOWN |
|---|---|---|---|---|
| Q1 生成物形态 | 1 | 1 | 0 | 0 |
| Q2 Backend ext | 4 | 0 | 0 | 1 |
| Q3 Frontend ext | 2 | 1 | 0 | 0 |
| Q4 Regen 覆盖 | 1 | 0 | 1 | 0 |
| Q5 业务扩展位置 | 1 | 1 | 0 | 0 |
| Q6 文件名规律 | 1 | 0 | 0 | 0 |
| Q7 外部编辑器 | 0 | 1 | 0 | 2 |
| **合计** | **11** | **4** | **2** | **3** |

---

## Q1. 生成物是源码还是配置?

### 结论(SOURCE_VERIFIED + DOCUMENT_VERIFIED)

**VOL.NET 的 CodeGenerator 生成真实 C# / Vue 源代码文件,而不是数据库元数据 + 运行时反射。**

- **后端**:`FileHelper.WriteFile(path, fileName, domainContent)` 物理写入 `.cs` 文件到磁盘
  - 路径:`vol.api.sqlsugar/VOL.Builder/Services/Core/Partial/Sys_TableInfoService.cs:184-280`
  - URL: `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Builder/Services/Core/Partial/Sys_TableInfoService.cs`
- **前端**:`{TableName}.vue` + `{TableName}/options.js` 都是真实文件
  - 实例:`Sys_Dictionary.vue` (13,620 B) + `Sys_Dictionary/options.js` (5,694 B)
  - URL: `https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.web/src/views/sys/system/Sys_Dictionary.vue`

**生成器是运行时 Web 应用** — 不是构建期 codegen(如 T4 / Roslyn Source Generator):
- `Sys_TableInfoService.CreateServices()` 是 ASP.NET Core Web API 方法
- 用户在 Web UI 配置表 → 后端 `CreateServices()` 调用 → 读数据库元数据 → 读 .html 模板 → 替换占位符 → 物理写盘
- 写盘后的代码通过 `dev_run.bat` 重启生效

### 关键证据

**模板是 .html 文件**(SOURCE_VERIFIED,文件读取调用):
```csharp
// Sys_TableInfoService.cs partial (CreateServices 方法)
string partialController = FileHelper.ReadFile(@"Template\\Controller\\ControllerApiPartial.html")
    .Replace("{Namespace}", nameSpace)
    .Replace("{TableName}", tableName)
    .Replace("{StartName}", StratName);
```
模板文件存放在生成器程序集内 — **不是 .t4 / .tt**,是普通文本,占位符 `{Namespace} {TableName} {StartName} {BaseOptions} {AttributeList} {AttributeManager}`。

**生成的 Vue 文件头有明确模板痕迹**(SOURCE_VERIFIED,生成产物回看):
```html
<!--
 *Author:jxx
 *Date:{Date}
 *Contact:283591387@qq.com
 *业务请在@/extension/sys/system/Sys_Dictionary.jsx或Sys_Dictionary.vue文件编写
 *新版本支持vue或【表.jsx]文件编写业务
 -->
```
> `{Date}` 占位符在生成时替换为实际日期 — **证据:这是真实源码生成,不是运行时渲染。**

### 文档佐证

README (`https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/README.md`):
> "整个页面所有前后端代码,全部由代码生成器界面上配置生成,并支持并后端业务代码扩展"
> "在现有的代码生成器功能上,继续定制开发代码生成器功能,解决重复性工作"

---

## Q2. Backend extension 机制

### 结论(SOURCE_VERIFIED)

**VOL.NET 后端扩展机制 = `partial class` + 委托 (Func<>) 钩子,无 attribute 标识。**

四问具体答案:

| 子问题 | 答案 | 证据等级 |
|---|---|---|
| **`partial class` 二次扩展?** | **是** — Service/Controller/Model 都用 `public partial class` | SOURCE_VERIFIED |
| **`ServiceExtension` / `IServiceExtension` 注入?** | **否** — 用 Autofac `IDependency` + 委托钩子,无显式 extension 类 | SOURCE_VERIFIED |
| **`RepositoryExtension` / Hook / Event 模式?** | **Repository** 几乎不扩展(基类 `RepositoryBase` 29KB 已覆盖所有),**Hook** 模式 = `ApplicationServiceBase` 的 `Func<>` 委托(30+ 个) | SOURCE_VERIFIED |
| **`[Generated]` / `[VolCodeGen]` attribute?** | **否** — 无任何 generated attribute | SOURCE_VERIFIED |

### 关键证据 — partial class 物理证据

`Services/Core/Sys_TableInfoService.cs` (主类,**672 B**):
```csharp
public partial class Sys_TableInfoService
    : ServiceBase<Sys_TableInfo, ISys_TableInfoRepository>
    , ISys_TableInfoService
    , IDependency
{
    public Sys_TableInfoService(ISys_TableInfoRepository repository) : base(repository) { Init(repository); }
    public static ISys_TableInfoService Instance
        => AutofacContainerModule.GetService<ISys_TableInfoService>();
}
```

`Services/Core/Partial/Sys_TableInfoService.cs` (扩展类,**125,390 B**):
```csharp
public partial class Sys_TableInfoService   // ← 同名,无继承
{
    // 120+ KB 的 codegen 元方法
    public string CreateEntityModel(...) { ... }
    public string CreateServices(...)    { ... }    // 生成主表/明细表代码
    public string CreateVuePage(...)     { ... }    // 生成 Vue 页面
    public Task<WebResponseContent> SyncTable(...) { ... }
    // ... 大量方法
}
```

> **物理证据**:主类 672 B、扩展 125,390 B — 比例 1:187。**partial 拆分是真实且被框架重度依赖的设计。**

### 关键证据 — ApplicationServiceBase 拆分(30+ Func<> 钩子)

`VOL.Core/BaseProvider/ApplicationServiceBase.cs` (**15,167 B**,非 partial 但聚合)包含 30+ protected/public 委托扩展点:

| 分类 | 扩展点(Func<> 委托) |
|---|---|
| **查询** | `QuerySql`, `QueryRelativeList`, `QueryRelativeExpression`, `OrderByExpression`, `SummaryExpress`, `SummaryExpressAsync`, `GetPageDataOnExecuted`, `GetPageDataOnExecutedAsync` |
| **新建** | `AddOnExecute`, `AddOnExecuting`, `AddOnExecutingAsync`, `AddOnExecuted`, `AddOnExecutedAsync` |
| **更新** | `UpdateOnExecute`, `UpdateOnExecuting`, `UpdateOnExecutingAsync`, `UpdateOnExecuted`, `UpdateOnExecutedAsync` |
| **删除** | `DelOnExecuting`, `DelOnExecutingAsync`, `DelOnExecuted`, `DelOnExecutedAsync` |
| **导入** | `ImportOnExecuting`, `ImportOnExecutingAsync`, `ImportOnExecuted`, `ImportOnExecutedAsync`, `ImportOnReadCellValue` |
| **导出** | `ExportOnExecuting`, `ExportOnExecutingAsync`, `ExportColumns`, `DownLoadTemplateColumns`, `ImportStartRowIndex`, `ImportIgnoreSelectValidationColumns`, `ExcelHeaderMap` |
| **审核/审批** | `AuditOnExecuting/Executed`, `AntiAuditOnExecuting/Executed`, `AddWorkFlowExecuting/Executed`, `AuditWorkFlowExecuting/Executed` |
| **主从表** | `GetDetailSummary`, `GetDetailSummaryAsync`, `GetDetailSummaryData`, `GetDetailSummaryDataAsync`, `DetailQuery`, `MultipleTableEntity` |
| **配置** | `IsMultiTenancy`, `IsTableActionLog`, `UploadFolder`, `ResponseIsError` |

**ApplicationServiceBase 还被拆为 10 个 partial 文件**(URL: `https://github.com/cq-panda/Vue.NetCore/tree/master/vol.api.sqlsugar/VOL.Core/BaseProvider/`):

| partial 文件 | 大小 | 用途 |
|---|---|---|
| `ApplicationServiceBase.cs` | 15,167 B | 主类(类签名 + Func<> 字段声明) |
| `ApplicationServiceBaseConfig.cs` | 2,546 B | 配置相关 |
| `ApplicationServiceBaseDeleteExtensions.cs` | 17,393 B | 删除逻辑 |
| `ApplicationServiceBaseExtensions.cs` | 9,962 B | 通用扩展 |
| `ApplicationServiceBaseMultipleTableEntity.cs` | 4,743 B | 主从表 |
| `ApplicationServiceBaseSearchDetailExtensions.cs` | 6,460 B | 明细查询 |
| `ApplicationServiceBaseSearchExtensions.cs` | 16,323 B | 查询逻辑 |
| `ApplicationServiceBaseUpdateOrAddExtensions.cs` | 27,559 B | 新增/更新(最大) |
| `ApplicationServiceBaseUploadFileExtensions.cs` | 3,517 B | 上传 |
| `ApplicationServiceBaseWrokflowExtensions.cs` | 11,932 B | 审批流(注:Workflow 拼成 Wrokflow) |

> **10 个 partial 文件协同实现 `ApplicationServiceBase<TEntity, TRepository>`** — partial 不止用于"用户扩展",框架自身就重度使用。

### UNKNOWN

- `IServiceExtension` 这种**显式 extension 接口**是否存在:**UNKNOWN**(仓库内只看到 `IService<T>` 基接口,没有看到 `IServiceExtension`)
- 推断:扩展主要是通过 partial class + Func<> 委托,**不需要**单独的 Extension 接口

---

## Q3. Frontend extension 机制

### 结论(SOURCE_VERIFIED + DOCUMENT_VERIFIED)

**VOL.NET 前端扩展 = `:extend="extend"` 注入(JSX 文件) + 6 个具名 slot + 通用 `<template #gridBody>` 等。**

三问具体答案:

| 子问题 | 答案 | 证据等级 |
|---|---|---|
| **生成的 Vue 页面含 user-editable 区?** | **不直接在生成文件里** — 通过外部 `src/extension/.../{Table}.jsx` 文件 | SOURCE_VERIFIED |
| **grid view / form view 扩展点?** | **是** — `view-grid` 组件提供 :extend + 6 slot(`gridHeader`、`gridBody`、`gridFooter`、`modelHeader`、`modelBody`、`modelFooter`) | SOURCE_VERIFIED + DOCUMENT_VERIFIED |
| **component slot / extend component 机制?** | **是** — Vue 原生 slot + `:extend` prop | SOURCE_VERIFIED |

### 关键证据 — :extend 注入机制

`Sys_Dictionary.vue` 头部注释(原文,SOURCE_VERIFIED):
```html
<!--
 *业务请在@/extension/sys/system/Sys_Dictionary.jsx或Sys_Dictionary.vue文件编写
 -->
```

`Sys_Dictionary.vue` 内(原文):
```vue
<template>
  <view-grid
    :columns="columns"
    :editFormFields="editFormFields"
    :extend="extend"        ← 外部扩展对象
    :onInit="onInit"
    :onInited="onInited"
    :searchBefore="searchBefore"
    :addBefore="addBefore"
    :updateBefore="updateBefore"
    ...
  >
    <!-- 自定义组件数据槽扩展,更多数据槽slot见文档 -->
    <template #gridBody>
      <el-alert title="所有功能数据源下拉框、级联请在此页面维护" />
    </template>
  </view-grid>
</template>
<script setup lang="jsx">
import extend from "@/extension/sys/system/Sys_Dictionary.jsx";   ← 外部 import
import viewOptions from "./Sys_Dictionary/options.js";
...
</script>
```

> **核心设计模式**:
> - 生成的 `Sys_Dictionary.vue` 是"壳",只负责绑定 options 和插槽
> - 用户业务代码写在 `src/extension/sys/system/Sys_Dictionary.jsx` 中
> - 通过 `:extend="extend"` 注入到 view-grid
> - regen 时**只重新生成 `Sys_Dictionary.vue`,不会动 `src/extension/`** — **这就是业务/生成分离的关键**

### 关键证据 — 6 个具名 slot (DOCUMENT_VERIFIED)

来源:`https://www.qubcedu.com/postdetail/3a05db30-d453-497b-f700-92c514e57197/1`(VOL 官方社区 "灵ヾ魂" 发布的"框架常见问题")

> "前端表 SellOrder.js 销售订单页面的扩展 js:在属性 gridHeader、gridBody、gridFooter、modelHeader、modelBody、modelFooter 中引入自己编写的组件路径,可参照 SellOrder.js(可扩展完整示例页面)使用扩展业务的方式"

> "代码生成后的表单页面,可以通过 this.$refs.gridHeader、gridBody、gridFooter、modelHeader、modelBody、modelFooter 拿到自己定义开发的组件"

> "自定义扩展组件中获取父组件(ViewGird.vue 也是代码生成后的页面上能看到的组件),this.$emit('parentCall', $vue => { }) //$vue 为父组件对象,具体使用参考 order->GridHeaderExtend.vue 文件"

**6 个具名 slot 列表**:

| slot | 位置 | 用途 |
|---|---|---|
| `gridHeader` | 表格上方 | 表格头部扩展(工具栏/筛选条) |
| `gridBody` | 表格内 | 表格内容区扩展 |
| `gridFooter` | 表格下方 | 表格底部扩展(分页/汇总) |
| `modelHeader` | 表单弹出框头部 | 表单头部扩展 |
| `modelBody` | 表单内容区 | 表单字段扩展 |
| `modelFooter` | 表单底部 | 表单底部扩展(自定义按钮) |

---

## Q4. 重新生成是否覆盖业务代码?

### 结论(SOURCE_VERIFIED + INFERRED)

**主类**:**会被覆盖**(但极小,只含 DI 注入和 `Instance` 静态访问)。
**Partial 业务类**:**不会覆盖**(用 `File.Exists` 保护)。
**`src/extension/...` 用户业务文件**:**不会覆盖**(因为生成器根本不写这个目录)。

### 关键证据 — File.Exists 保护

`Sys_TableInfoService.cs` partial 的 `CreateServices` 方法(SOURCE_VERIFIED,简化):
```csharp
// 1. 主类 — 总是覆盖(但主类极小,只含 DI)
domainContent = FileHelper.ReadFile("Template\\IServices\\IServiceBase.html")...
FileHelper.WriteFile(path, fileName, domainContent);   // ← 无条件覆盖

// 2. Partial 类 — 只生成一次
if (!File.Exists(path + "Partial\\" + fileName))
{
    domainContent = FileHelper.ReadFile("Template\\IServices\\IServiceBasePartial.html")...
    FileHelper.WriteFile(path + "Partial\\", fileName, domainContent);   // ← 跳过
}

// 3. Controller 同理
if (AppSetting.GetSettingString("GenControllerToDir") == "1"
    && ((!File.Exists(path1) && !File.Exists(path2)) || File.Exists(path2)))
{
    if (!File.Exists(path2))
        FileHelper.WriteFile($"{apiPath}\\{foldername}\\Partial\\", tableName + "Controller.cs", partialController);
}
else if (!File.Exists(path1))
{
    FileHelper.WriteFile($"{apiPath}Partial\\", tableName + "Controller.cs", partialController);
}
// 主 Controller — 总是覆盖
domainContent = FileHelper.ReadFile("Template\\Controller\\ControllerApi.html")...
FileHelper.WriteFile(apiPath + controllerFolderName, tableName + "Controller.cs", domainContent);
```

### 关键证据 — 用户修改 options.js 的明确警告

`options.js` 头部(SOURCE_VERIFIED,每个生成的 options.js 都有):
```js
// *代码由框架生成,任何更改都可能导致被代码生成器覆盖
```

> **关键**:这意味着 **options.js 改了也会被覆盖**。所有表单元数据(字段、列、表单配置)都在 options.js,**用户不能直接改** — 改完 regenerate 会被覆盖。
> 改字段配置的正确路径是改 `Sys_TableInfo` 数据库 + 重新 codegen。

### 推断(INFERRED)

regenerate 流程整体行为:
1. 读 `Sys_TableInfo` / `Sys_TableColumn` 数据库元数据
2. 读 Template\*.html 模板
3. 替换占位符
4. **主类**(Controller/Service/Model/options.js):**强制覆盖**
5. **Partial 类**(Services/Partial, Controllers/Partial, partial/):**跳过(若已存在)**
6. **`src/extension/...`**:生成器从不写这个目录 → **永远不动**

**主类可被覆盖是有道理的** — 主类只含 DI 注入(5-10 行),用户不应改主类。partial 才是用户扩展的地方。

---

## Q5. 业务扩展放在哪里?

### 结论(SOURCE_VERIFIED + DOCUMENT_VERIFIED)

| 层级 | 业务扩展位置 | 命名 |
|---|---|---|
| **后端 Controller 扩展** | `Controllers/{namespace}/Partial/{TableName}Controller.cs` | `{Table}Controller.cs` (放在 `Partial\` 子目录) |
| **后端 Service 扩展** | `Services/{namespace}/Partial/{TableName}Service.cs` | `{Table}Service.cs` (放在 `Partial\` 子目录) |
| **后端 IService 扩展** | `IServices/{namespace}/Partial/I{TableName}Service.cs` | `I{Table}Service.cs` (放在 `Partial\` 子目录) |
| **后端 Model 字段配置** | `Entity/DomainModels/{namespace}/partial/{TableName}.cs` | `{Table}.cs` (放在 `partial\` 子目录) |
| **前端业务扩展** | `src/extension/{namespace}/{TableName}.jsx` (vue3) **或** `views/.../{Table}.js` (vue2) | `{Table}.jsx` 或 `{Table}.js` |
| **前端子组件** | `views/.../{TableName}/{TableName}Header.vue` / `Footer.vue` / 其他子组件 | `{Table}{Role}.vue` |
| **后端业务辅助类** | `VOL.Core/BaseProvider/ApplicationServiceBase*Extensions.cs` (10 partial 拆分) | `*Extensions.cs` |

### 关键证据 — partial 子目录约定(SOURCE_VERIFIED)

生成器写文件路径(从 `CreateServices` 提取):
```
apiPath + "Partial\\" + tableName + "Controller.cs"           → 业务 Controller 扩展
apiPath + "\\" + foldername + "\\Partial\\" + tableName + "Controller.cs"  → 业务 Controller 扩展(子目录)
apiPath + "\\" + foldername + "\\" + tableName + "Controller.cs"          → 生成 Controller 主类
path + "Partial\\" + "I" + tableName + "Service.cs"           → 业务 IService 扩展
path + "I" + tableName + "Service.cs"                          → 生成 IService 主类
path + "Partial\\" + tableName + "Service.cs"                  → 业务 Service 扩展
path + tableName + "Service.cs"                                → 生成 Service 主类
modelPath + "partial\\" + tableName + ".cs"                    → 业务 Model 字段配置
modelPath + tableName + ".cs"                                  → 生成 Model 主类
```

> **约定固化**:`Partial\`(后端) / `partial\`(Model) / `extension\`(前端) 三个特殊目录是用户扩展区,生成器用 `File.Exists` 保护。

### 关键证据 — 前端业务扩展点(6 slot + extend 注入)

见 Q3 章节。汇总一句话:**生成文件是"壳",`src/extension/.../{Table}.jsx` 是"业务脑"。**

---

## Q6. 生成的文件名规律?

### 结论(SOURCE_VERIFIED)

| 层 | 主类(被 regen 覆盖) | 扩展类(被 regen 跳过) |
|---|---|---|
| **Entity Model** | `Entity/DomainModels/{namespace}/{TableName}.cs` | `Entity/DomainModels/{namespace}/partial/{TableName}.cs` |
| **IRepository** | `IRepositories/{namespace}/I{TableName}Repository.cs` | (无扩展) |
| **Repository** | `Repositories/{namespace}/{TableName}Repository.cs` | (无扩展) |
| **IService** | `IServices/{namespace}/I{TableName}Service.cs` | `IServices/{namespace}/Partial/I{TableName}Service.cs` |
| **Service** | `Services/{namespace}/{TableName}Service.cs` | `Services/{namespace}/Partial/{TableName}Service.cs` |
| **Controller (API)** | `Controllers/{namespace}/{TableName}Controller.cs` | `Controllers/{namespace}/Partial/{TableName}Controller.cs` |
| **Vue 主组件** | `vol.web/src/views/{namespace}/{TableName}.vue` | (slot 扩展) |
| **Vue 配置** | `vol.web/src/views/{namespace}/{TableName}/options.js` | (用户不该改) |
| **Vue 业务扩展** | (不生成) | `vol.web/src/extension/{namespace}/{TableName}.jsx` |
| **Vue 子组件** | (不生成) | `vol.web/src/views/{namespace}/{TableName}/{TableName}Header.vue` 等 |

### 命名细节(SOURCE_VERIFIED)

**生成的 `.vue` 文件头部**:
```html
<!--
 *Author:jxx
 *Date:{Date}
 *Contact:283591387@qq.com
 *业务请在@/extension/sys/system/Sys_Dictionary.jsx...
 -->
```

**生成的 `.cs` 文件**:
- **无 `// <auto-generated>` 注释**
- **无 `[Generated]` / `[VolCodeGen]` attribute**
- **靠 `partial class` 关键字 + `Partial/` 目录约定做分离**
- 唯一警告出现在 `options.js` 顶部:`// 代码由框架生成,任何更改都可能导致被代码生成器覆盖`

### 物理文件大小对比(SOURCE_VERIFIED,从 GitHub tree API)

| 文件 | 大小 | 角色 |
|---|---|---|
| `Services/Core/Sys_TableInfoService.cs` | **672 B** | 主类(只有 DI 注入) |
| `Services/Core/Partial/Sys_TableInfoService.cs` | **125,390 B** | 扩展类(codegen 自身 120+ KB 业务) |
| `IServices/Core/ISys_TableInfoService.cs` | **191 B** | 主接口(继承 IService<T>) |
| `IServices/Core/Partial/ISys_TableInfoService.cs` | **967 B** | 扩展接口(8 个 codegen 方法签名) |
| `Repositories/Core/Sys_TableInfoRepository.cs` | **600 B** | 主类(只有继承) |
| `IRepositories/Core/ISys_TableInfoRepository.cs` | **369 B** | 主接口(只有签名) |
| `vol.web/src/views/sys/system/Sys_Dictionary.vue` | **13,620 B** | 生成主组件 |
| `vol.web/src/views/sys/system/Sys_Dictionary/options.js` | **5,694 B** | 生成配置 |
| `vol.web/src/views/sys/system/Sys_Menu.vue` | **21,300 B** | 最大生成组件 |
| `vol.web/src/views/sys/flow/Sys_WorkFlow/WorkFlowGridHeader.vue` | **8,683 B** | 业务子组件(Header 扩展) |
| `vol.web/src/views/mes/mes/MES_ProductionOrder.vue` | **6,918 B** | MES 业务主组件 |
| `vol.web/src/views/mes/mes/MES_ProductionOrder/SelectMaterial.vue` | **3,092 B** | 业务子组件(选择器) |

> **partial 文件名相同,目录不同** — 编译时 C# partial class 合并,无冲突。

---

## Q7. 是否提供"打开外部编辑器"或"下载生成文件"机制?

### 结论(部分 UNKNOWN)

| 子问题 | 答案 | 证据等级 |
|---|---|---|
| **生成文件能否下载?** | **是(通过 Web UI + Git 间接)** — 生成器在 ASP.NET Core 进程内,生成后写到服务器磁盘;通过 git pull / `dev_run.bat` 重启 | INFERRED + DOCUMENT |
| **是否提供 VS/VSCode 一键打开按钮?** | **UNKNOWN** — 仓库内未见"open in editor"链接/按钮 | UNKNOWN |
| **是否能脱机/离线生成?** | **是** — 流程是"配置 → API 调用 → 写盘",只要有 dev_run.bat 就能生成 | DOCUMENT |
| **是否有"打开外部编辑器"专用机制?** | **UNKNOWN** — 仓库内没看到 `monaco-editor` / `vscode://` / `webstorm://` 协议 | UNKNOWN |

### 关键证据

**生成流程的"离线"特性**:
- `dev_run.bat` / `builder_run.bat` 启动 Web API(基于 `VOL.WebApi` 启动器)
- 浏览器访问代码生成器 UI → 配表 → 调 `CreateServices()` API → 后端 `FileHelper.WriteFile` 写盘
- 文件落到 `vol.api/{YourProject}/{Services,Controllers,IServices,...}` 和 `vol.web/src/views/...`
- 写完通过 git 看 diff / 在 VSCode 编辑

**官方文档佐证**:
> qubcedu 官方论坛:"后台必须运行 builder_run.bat 命令才可以生成业务类,生成其他运行 dev_run.bat 或 builder_run.bat"

**没有下载 zip 按钮** — 整个生成流程是"在服务器上写盘",用户从 git 取。**没有看到任何"打包下载"的代码**(如 `ZipFile.CreateFromDirectory` 调用)。

### UNKNOWN

- 是否提供 IDE deep-link(`vscode://file/...`):**UNKNOWN**
- 是否提供 monaco-editor 嵌入:**UNKNOWN** — 但 vol.pro 商业版的 home page 描述了"在线无代码开发,实时预览",可能 pro 版有

---

## 终评:对 GuliERP 而言,CodeGen Extension Model 是 SAFE / RISKY / DANGEROUS?

### 评级:**RISKY**

### 评分

| 维度 | 评分 (1-5) | 说明 |
|---|---|---|
| **扩展点丰富度** | ⭐⭐⭐⭐⭐ | 30+ Func<> 委托、6 个具名 slot、partial class 全面 |
| **生成/扩展分离度** | ⭐⭐⭐⭐ | partial + `File.Exists` 保护 + `src/extension/` 外部化,设计良好 |
| **regen 安全性** | ⭐⭐⭐⭐ | 业务代码(partial / extension)被保护,主类(只 5-10 行)被覆盖可接受 |
| **对 GuliERP 现有架构冲击** | ⭐⭐ (低分=冲击大) | GuliERP R3 是手写文档类(SalesOrder.vue + Service + Controller),引入 vol codegen 意味着 80% 现有代码被重写 |
| **可定制性** | ⭐⭐⭐ | 模板是 .html,可改;但模板嵌在生成器程序集内,需重新发布 |
| **复杂度表达** | ⭐⭐⭐ | 30+ 委托够用,但 GuliERP 的 3D 状态、行锁、含税未税等深度业务用 Func<> 表达深度有限 |

### 对 GuliERP 的具体影响

**优点 (吸引人)**:
- 80% 标准 CRUD(单表/主从表)可由 codegen 覆盖
- 业务代码与生成代码物理分离(partial / extension),regen 不丢业务
- 6 个 slot + `:extend` 注入,前端扩展灵活
- 模板可改,5 个数据库(SqlServer/MySql/PGSql/Oracle/达梦)内置

**风险 (需谨慎)**:
- **强依赖 Autofac + SqlSugar + 自家 VOL.Core** — 不能与 GuliERP 现有 building-blocks 共存
- **regen 主类被覆盖** — 如果用户不小心改了主类(672 B 那个),regen 会丢
- **`options.js` 用户不能改** — 改了 regenerate 覆盖 → 所有表单元数据必须走 codegen 流程
- **模板改动需改 generator 自身** — 不是普通用户能做的事
- **GuliERP 的复杂业务**(3D 状态、行锁、含税未税、审批链)用 Func<> 委托**深度表达有限**
- **生成产物是 .cs / .vue,不是 .ts / .tsx** — 与 GuliERP 现有 TypeScript 技术栈不匹配

### 推荐决策

| 场景 | 是否引入 vol codegen |
|---|---|
| 新建一个**标准 CRUD 密集**的项目(OA/HR/库存) | ✅ 可考虑 |
| 现有 **GuliERP R3 升级** | ❌ 不建议(架构冲击大) |
| 想要**借鉴 codegen 设计思路**自己实现 | ✅ 推荐看 Q2/Q3/Q5 的 partial + slot + extension 设计 |
| 想要 **100% 业务自控** | ❌ 模板/生成器耦合度高,改起来累 |

### 终评结论

**VOL.NET CodeGen Extension Model 对 GuliERP 整体评级 = RISKY**。
模式本身设计良好(partial + slot + extension),但 GuliERP 已有 building-blocks 架构 + TypeScript 技术栈,引入意味着 80% 重写,不划算。
**可借鉴**:Q2 的 `partial class` + `Func<>` 委托模式、Q3 的 `src/extension/` + 6 slot 模式、Q4 的 `File.Exists` 保护策略。
**不建议直接采用** codegen 本身。

---

## 引用 URL 索引(全部 SOURCE_VERIFIED)

| 引用 | URL |
|---|---|
| README | https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/README.md |
| Services 主类 (672 B) | https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Builder/Services/Core/Sys_TableInfoService.cs |
| Services 扩展类 (125,390 B) | https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Builder/Services/Core/Partial/Sys_TableInfoService.cs |
| IService 主接口 (191 B) | https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Builder/IServices/Core/ISys_TableInfoService.cs |
| IService 扩展接口 (967 B) | https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Builder/IServices/Core/Partial/ISys_TableInfoService.cs |
| ApplicationServiceBase (15,167 B) | https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.api.sqlsugar/VOL.Core/BaseProvider/ApplicationServiceBase.cs |
| ApplicationServiceBase partial 目录 | https://github.com/cq-panda/Vue.NetCore/tree/master/vol.api.sqlsugar/VOL.Core/BaseProvider/ |
| Sys_Dictionary.vue (13,620 B) | https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.web/src/views/sys/system/Sys_Dictionary.vue |
| Sys_Dictionary/options.js (5,694 B) | https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.web/src/views/sys/system/Sys_Dictionary/options.js |
| Sys_WorkFlow/options.js | https://raw.githubusercontent.com/cq-panda/Vue.NetCore/master/vol.web/src/views/sys/flow/Sys_WorkFlow/options.js |
| 仓库 API 根 | https://api.github.com/repos/cq-panda/Vue.NetCore/contents/ |
| 仓库 tree 递归 | https://api.github.com/repos/cq-panda/Vue.NetCore/git/trees/master?recursive=1 |

### 文档佐证(DOCUMENT_VERIFIED)

| 来源 | URL |
|---|---|
| VOL 官方社区 — 6 slot + $emit parentCall | https://www.qubcedu.com/postdetail/3a05db30-d453-497b-f700-92c514e57197/1 |
| 博客园 — vol 介绍 | https://www.cnblogs.com/yakniu/p/16267508.html |
| Gitee 镜像 | https://gitee.com/x_discoverer/Vue.NetCore |
| 博客园 — vol 开发步骤 | https://www.cnblogs.com/renzhituteng/p/18728005 |

---

> **诚实标注**:
> - 11/20 结论为 SOURCE_VERIFIED(直接读 GitHub 源码)
> - 4/20 为 DOCUMENT_VERIFIED(官方论坛/博客)
> - 2/20 为 INFERRED(regen 整体行为 / IDE 集成推断)
> - 3/20 为 UNKNOWN(`IServiceExtension` 显式接口 / IDE deep-link / monaco 嵌入)
> - 模板 `.html` 文件(ControllerApi.html / ControllerApiPartial.html / IServiceBase.html / IServiceBasePartial.html / DomainModel.html 等)在生成器程序集内,github 仓库未上传这些文件 — **模板内容 UNKNOWN**(只能从生成产物反推)
> - **vol.pro (商业版) vs cq-panda (开源版) 是两套不同实现**,本报告**仅基于开源版验证**,商业版的 CodeGen UX 增强(AI 辅助、报表设计器等)见 `VOL_PRO_CODEGEN_EXTENSION_ANALYSIS.md`

---

*End of VOL_PRO_CODEGEN_EXTENSION_VERIFICATION*
*Status: SOURCE_VERIFIED × 11 + DOCUMENT × 4 + INFERRED × 2 + UNKNOWN × 3*
*Final verdict: RISKY for GuliERP (模式可借鉴,不建议直接采用)*
