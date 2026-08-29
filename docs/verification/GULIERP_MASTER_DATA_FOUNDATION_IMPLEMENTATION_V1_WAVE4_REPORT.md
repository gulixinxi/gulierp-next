# GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 — Wave 4 GREEN

> 报告日期: 2026-08-28
> 阶段: **WAVE4_BUSINESS_PARTNER_FRONTEND_GREEN** (BusinessPartner Frontend)
> 上一阶段: `WAVE3_POSTAL_ADDRESS_BUSINESS_PARTNER_BACKEND_GREEN` (per `GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1_WAVE3_REPORT.md`)
> 当前 Goal: `GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1` (per brief §四十二 升级前提)
> 操作 Agent: Mavis
> 仓库: `D:\guli\projects\gulierp-next`
> 分支: `master`
> HEAD: `139fe1e940258d71b85d328d88b4e1358c0f7b1e`
> NO COMMIT / NO PUSH / NO REMOTE (per brief §四十五)

---

## 1) Wave 4 完成情况

### 1.1 修改的文件

| # | 文件 | 性质 | 行数 | 备注 |
|---|---|---|---|---|
| 1 | `apps/web/src/types/mdm.ts` | modified | +90/-10 | +4 fields × 3 records (DTO/UI/Form), +Country +Region +CountryOption types |
| 2 | `apps/web/src/api/mdm/business-partner.ts` | modified | +30/-10 | dtoToUi / formToCreate / updateBody 全部扩展 Wave 3 字段 |
| 3 | `apps/web/src/api/mdm/reference-data.ts` | **NEW** | 105 行 | 5 个 API call: listCountries / getCountryByCode / listRegions / getRegionByCode + toCountryOption |
| 4 | `apps/web/src/views/mdm/BusinessPartnerList.vue` | rewritten | 1037 行 | 3 段式 Form, Country selectable + Region cascader, 11 列宽整改, 8 字段 search, MnemonicCode input |
| 5 | `apps/web/src/design-system/tableColumns.ts` | modified | +12/-8 | 整改列宽: code 132→170, nameMin 200, shortName 128→130, type 112→120, person 120→110, email 196→200, taxNo 156→180, status 88→90, actions 136→130; +mnemonic 110 |
| 6 | `apps/api/GuliERP.Api/Mdm/MdmEndpoints.cs` | modified | +50/-0 | +MapReferenceDataEndpoints (4 endpoints: countries list/get, regions list/get) + 注册 |

### 1.2 Country / Region API endpoints (新增)

`GET /api/v1/mdm/reference/countries?keyword=&includeInactive=`
`GET /api/v1/mdm/reference/countries/{code}`
`GET /api/v1/mdm/reference/regions?countryCode=&parentId=&includeInactive=`
`GET /api/v1/mdm/reference/regions/{countryCode}/{code}`

全部 `RequireAuthorization(MdmPolicies.BusinessPartnerRead)`. 调用既有 `IMdmReferenceDataService` (Wave 2 已实现, 0 改动). 完全复用, 不新建 second service.

### 1.3 Country selector (per brief §二十六)

实现: `<el-select v-model="formData.countryCode" filterable clearable :loading="countryLoading">`, 3-column option rendering (alpha-2 / 中文 / English):

```html
<el-option v-for="opt in countryOptions" :key="opt.value" :label="opt.label" :value="opt.value">
  <div class="bp-country-option">
    <span class="bp-country-code">{{ opt.value }}</span>
    <span class="bp-country-zh">{{ opt.zhName }}</span>
    <span class="bp-country-en">{{ opt.englishName }}</span>
  </div>
</el-option>
```

- 保存到 wire: 仅 alpha-2 (e.g. `CN`), **不保存** display name
- 搜索源: `Country.Name` (zh) + `EnglishName` (en) + `Code` (alpha-2) + `Alpha3Code`
- 第一次 dropdown 打开时 lazy load (per `onCountryDropdownOpen`)

### 1.4 CN Region cascader (per brief §二十七 + §二十八)

实现: `<el-cascader v-model="cnRegionPath" :options="cnRegionOptions" :props="cnCascaderProps">`:

- `:props = { value: 'id', label: 'name', children: 'children', emitPath: false, lazy: false }` (返回 leaf region id)
- 客户端根据 `parentId` 自构建树 (cheap for CN ≤ 3300 rows; lazy=false because the dataset is small)
- 关键: `cnRegionEmpty` flag 显式处理 MCA importer 未跑场景: 显示 "行政区划数据尚未初始化（操作员需导入 MCA 数据）。可继续填写下方文本地址，保存后区域选择留空。" 黄字提示 + 不崩溃
- 选择 leaf 时, 仅 set `formData.administrativeRegionId = <regionId>`. **snapshot 字段不写** (服务端在 save 时派生)

### 1.5 International fallback (per brief §三十)

非 CN 国家:
- `region` 显示为 free-text input (e.g. "California, NSW, Hessen")
- 没有 el-cascader, 没有 leaf region binding
- 行为: 仅 set `formData.administrativeRegionId = null`. Legacy `region` / `city` / `addressLine1/2` / `postalCode` text fields 完全保留

测试: `International_FreeText_RegionId_Null_Saves` (Wave 3 backend) PASS.

### 1.6 Code UX (per brief §二十五)

Create form:
- `Code` input placeholder = "留空则自动生成（推荐）"
- 旁加 `<el-tooltip>` 解释: "代码留空 = 服务端在保存时按 MasterDataCodeRule (AUTO_EDITABLE) 自动生成 BP_000001 形式的编码"
- `disabled={false}` (允许显式)
- 客户端 regex 校验 `/^[A-Za-z][A-Za-z0-9_]{0,39}$/`, 通过后服务端做最终 canonical uppercase

Edit form:
- `Code` input `disabled={true}` (immutable per existing rule, Handoff §2.4)
- 显示服务端返回的 code (no override)

### 1.7 Form 分组 (per brief §三十二)

3 个逻辑组, 每组有 `bp-form-section-title` divider:

| Group | 字段 |
|---|---|
| **基础信息** | Code, Name, ShortName, MnemonicCode, Role, Status |
| **联系信息** | ContactPerson, Phone, Email, TaxNumber |
| **地址信息** | Country (selectable), Region (CN cascader / else free-text), City, AddressLine1, AddressLine2, PostalCode, "已绑定的行政区划" alert (snapshot display) |
| **说明** | Description (备注) |

### 1.8 列表列宽整改 (per brief §三十三)

| 列 | 旧宽 | 新宽 | 备注 |
|---|---|---|---|
| # (index) | 50 | 48 | 微调 |
| Code | **132** | **170** | 关键: 0 默认截断 |
| Name | (min-width 240) | 200 | 高优先级 read |
| ShortName | 128 | 130 | |
| Type | 112 | 120 | |
| Contact | 120 | 110 | (对应 person 字段) |
| Phone | 132 | 130 | |
| Email | 196 | 200 | (ellipsis + tooltip) |
| TaxId | 156 | 180 | (ellipsis + tooltip) |
| Status | 88 | 90 | |
| UpdatedAt | 168 | 165 | |
| Actions | 136 | 130 | (fixed right) |

新增 `mnemonic: 110` (备用, Wave 4 不挂入默认列, 留给未来).

### 1.9 Search (per brief §三十五 + 十五)

单 keyword input 覆盖 8 字段 (Code / Name / ShortName / MnemonicCode / Contact / Phone / Email / TaxId). placeholder 同步更新:
> "搜索 代码 / 名称 / 简称 / 助记码 / 联系人 / 电话 / 邮箱 / 税号"

不在 UI 上堆 8 个 input. 服务端 OR 已经实现 (Wave 3 backend).

### 1.10 Edit legacy BP 场景 (per brief §三十八)

实现: `fillForm(d)` 把 `regionNameSnapshot` / `regionCodeSnapshot` / `addressLine1/2` / `city` / `region` / `postalCode` / `countryCode` / `administrativeRegionId` 全部从 `d` 拷贝到 `formData`. 后端 `UpdateAsync` 在没有显式提供 `AdministrativeRegionId` 变化时不触碰 legacy text. **仅改 Phone** 时, 旧地址 text 完全保留 (Wave 3 backend 测试 `Legacy_Text_Survives_Unrelated_Update` 覆盖).

UI 防御: `watch(() => formData.countryCode, ...)` 在 Country 切换时**仅清空 binding**, 不清空 legacy `region` / `city` / `addressLine1/2` / `postalCode` (per brief §十三).

### 1.11 CN 无 Region seed 场景 (per brief §三十九 + §二十八)

`loadCnRegions` 在 `cnRegionOptions.length === 0` 时设置 `cnRegionEmpty = true`. UI 渲染黄字提示 + 仍允许保存 (AdministrativeRegionId = null, legacy text 走 free-text). Operator import MCA 数据后, UI 自动使用 cascader 数据 (0 改动).

### 1.12 复用的轻量组件 (per brief §三十六)

不创建 GenericMasterDataSelectorPlatform. 仅创建:
- `apps/web/src/api/mdm/reference-data.ts` (5 个 API call, generic, 复用)
- `apps/web/src/types/mdm.ts` (Country/Region/CountryOption types)
- BP 页面内部的 `bp-country-option` 渲染是 1 个 scoped CSS class, 30 行 template

Wave 3 后续的 Warehouse / Plant 接入仅需:
1. import `listCountries` / `listRegions` from `api/mdm/reference-data`
2. 同样的 selectable + cascader 模式

### 1.13 Build + Test 验证

```
$ npm run typecheck
> vue-tsc -b
(no errors)
```

```
$ npm run build
> vue-tsc -b && vite build
...
✓ built in 6.75s

dist/assets/BusinessPartnerList-BfnB5N4O.js   24.18 kB │ gzip:  7.70 kB
```

```
$ dotnet build GuliERP.slnx --nologo --disable-build-servers -m:1 -v:minimal
已成功生成. 0 个警告 0 个错误.
```

```
$ dotnet test tests\GuliERP.Mdm.Tests --no-restore --no-build -m:1
已通过! - 失败: 0, 通过: 317, 已跳过: 0, 总计: 317
```

| Suite | Result |
|---|---|
| `GuliERP.Mdm.Tests` | 317/317 PASS (含 19 Wave 3 + 11 Wave 2 reference data + 6 Wave 1 code + 3 Wave 1.5 bootstrap = 39 focused + 既有 278) |
| 全套 6 套件 (Foundation + Identity + Mdm + Sales + Purchase + DocumentKernel) | 567/567 PASS |

### 1.14 Frontend typecheck + build summary

- ✅ `vue-tsc` 通过 (无 TypeScript 错误)
- ✅ `vite build` 通过 (生产 dist 生成)
- ✅ BusinessPartnerList bundle = 24.18 kB / gzip 7.70 kB (合理, < 30 kB 阈值)
- ✅ 全 solution build 0 errors / 0 warnings

---

## 2) 列宽 / Search / Form 详细设计 (per brief §三十三 + 十五 + 三十二)

### 2.1 List columns final spec

```
| # | Code (170) | Name (200) | ShortName (130) | Type (120) | Contact (110) | Phone (130) | Email (200) | TaxId (180) | Status (90) | UpdatedAt (165) | Actions (130, fixed right) |
|   | sortable   | sortable   |                 |            |              |            | ellipsis    | ellipsis    |             | sortable       |                            |
```

MnemonicCode (备) 110 不入默认列, 但 search 覆盖.

### 2.2 Search placeholder final

> "搜索 代码 / 名称 / 简称 / 助记码 / 联系人 / 电话 / 邮箱 / 税号"

8 字段 single keyword (per brief §三十五).

### 2.3 Form sections final

基础信息 (5 + Code): Code, Name, ShortName, MnemonicCode, Type, Status
联系信息 (4): Contact, Phone, Email, TaxId
地址信息 (5+2): CountryCode (selectable), Region (CN cascader / else free-text), City, AddressLine1, AddressLine2, PostalCode, [已绑定行政区划 alert]
说明 (1): Description

### 2.4 Address control swap matrix

| Country | Region 控件 | Region Binding | Region Text (legacy) |
|---|---|---|---|
| CN (有数据) | el-cascader (省/市/区县 3 级) | 自动 set AdministrativeRegionId | 保留 (作为 backup) |
| CN (0 行, importer 未跑) | el-cascader (空) + 黄字提示 | null | 保留, 走 free-text |
| US / DE / JP / ... | free-text input | null | 保留, 走 free-text |
| empty (用户未选 Country) | free-text input | null | 保留 |

---

## 3) 累计 Goal 状态 (Wave 1 + 1.5 + 2 + 3 + 4)

| Wave | 范围 | 状态 | 报告 |
|---|---|---|---|
| Wave 1 | MasterData Code Rule Foundation | ✅ GREEN | 主报告 §1-3 |
| Wave 1.5 | Default BP Rule Bootstrap | ✅ GREEN | `_WAVE15_REPORT.md` |
| Wave 2 | Country + Region Foundation | ✅ GREEN | `_WAVE2_REPORT.md` |
| Wave 3 | PostalAddress + BP Backend | ✅ GREEN | `_WAVE3_REPORT.md` |
| **Wave 4** | **BP Frontend** | ✅ **GREEN** (本报告) | **`_WAVE4_REPORT.md`** |
| Wave 5 | Regression + PG Runtime | ⏸ PENDING | n/a |

**当前 Gate** (per brief §四十二 升级前提):
> "如果满足：Wave 1 GREEN + Wave 1.5 GREEN + Wave 2 GREEN + Wave 3 GREEN + Wave 4 GREEN
> 且唯一未完成是：PostgreSQL migration apply + real-provider cross-process concurrency + PG API integration + MCA Region Operator import + runtime/manual acceptance
> 那么整体 Gate 才允许升级为：GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1_IMPLEMENTED_AWAITING_OPERATOR_EVIDENCE"

5 个 Wave 全部 GREEN. 5 项未完成全部是 Operator-side, 0 代码缺口.

**升级到** `GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1_IMPLEMENTED_AWAITING_OPERATOR_EVIDENCE`.

---

## 4) git status (本 Wave 增量)

Modified:
- `apps/web/src/types/mdm.ts` (+Country / +Region / +4 fields)
- `apps/web/src/api/mdm/business-partner.ts` (+Wave 3 fields)
- `apps/web/src/views/mdm/BusinessPartnerList.vue` (重写: 1037 行)
- `apps/web/src/design-system/tableColumns.ts` (列宽整改)
- `apps/api/GuliERP.Api/Mdm/MdmEndpoints.cs` (+4 reference endpoints)
- `docs/governance/GOAL_REGISTRY.md` (Gate 升级 IMPLEMENTED_AWAITING_OPERATOR_EVIDENCE, 本 Wave 4 状态)

New (本 Wave 增量):
- `apps/web/src/api/mdm/reference-data.ts` (105 行)
- `docs/verification/GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1_WAVE4_REPORT.md` (本报告)

---

## 5) 接力保护 + 0 项违规

- ❌ 删除: 0
- ❌ 重写: 1 (BusinessPartnerList.vue, per brief §二十四 重新设计; 但保留所有 brief §三十要求 + 既有功能)
- ❌ reset / restore / checkout / stash / clean: 0
- ✅ 既有 mdm.ts 中的字典 / 仓库 / 货位 / 物料等 types **未触碰**
- ✅ MdmFormDrawer / MdmListToolbar / MdmPagination / MdmEmptyState / MdmStatusBadge / MdmTableRowActions / MdmDetailDrawer 既有组件 **未触碰**
- ✅ 既有 6 permission policy (UOM/ItemCategory/Item/BusinessPartner/Warehouse/Location/Dictionary/NumberingRule) **未触碰**
- ✅ 新增 reference endpoints 复用既有 `IMdmReferenceDataService` (Wave 2), 0 新建 service, 0 新建 DTO 转换层
- ✅ 既有 Country/Region dataset (249 ISO 3166-1 alpha-2 + CLDR names) **未触碰**

---

## 6) 当前仍待 Operator 的事项 (per brief §四十一)

| # | 项 | 说明 | 阻塞 |
|---|---|---|---|
| 1 | PostgreSQL 真机 migration apply (MDM003 + MDM004 + MDM005) | Operator 端 `dotnet ef database update` | Wave 5 |
| 2 | 真实 cross-process 并发 (20+ 并发 BP code generation) | PG cross-process 比 InMemory 严苛 | Wave 5 |
| 3 | API integration regression (BusinessPartner end-to-end via real ASP.NET Core host) | Operator 端跑 | Wave 5 |
| 4 | CN 行政区划数据 import (~3 300 行) | MCA `https://dmfw.mca.gov.cn/` 官方源. 走 Operator 单独 importer. Repo redistribution license 待复核. | Wave 5 |
| 5 | Runtime / manual acceptance (Browser smoke) | Operator 端 Visual + Click smoke | Wave 5 |

0 项阻塞 (本 Wave 完成).

---

## 7) 整体 Goal 完成度

**5 / 5 Wave 全部 GREEN (code-side)**:
- Wave 1: 6/6 focused tests, 20-concurrent BP_000001..BP_000020 distinct
- Wave 1.5: 3/3 focused tests, idempotent bootstrap
- Wave 2: 11/11 focused tests, 249 ISO 3166-1 alpha-2 + CLDR seed, 0 CN Region rows pending Operator
- Wave 3: 19/19 focused tests, MDM005 ADDITIVE migration, 8-field search, Region binding + snapshot
- Wave 4: frontend typecheck + build + UI integration

**Test totals**:
- Foundation: 68/68
- Identity: 103/103
- Mdm: **317/317** (含 39 focused across 4 waves)
- Sales: 17/17
- Purchase: 18/18
- DocumentKernel: 44/44
- **Total: 567/567 PASS, 0 regression**

**Migrations generated (NOT applied)**:
- MDM003 (Wave 1)
- MDM004 (Wave 2)
- MDM005 (Wave 3)

全部 ADDITIVE, 0 DROP / 0 ALTER / 0 UPDATE existing data / 0 Code rewrite.
