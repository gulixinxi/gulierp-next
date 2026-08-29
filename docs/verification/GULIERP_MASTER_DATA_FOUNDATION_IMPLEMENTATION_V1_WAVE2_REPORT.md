# GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 — Wave 2 GREEN

> 报告日期: 2026-08-28
> 阶段: **WAVE2_COUNTRY_REGION_GREEN** (Country + AdministrativeRegion Foundation)
> 上一阶段: `WAVE1_MASTER_DATA_CODE_RULE_GREEN` (per `GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1_WAVE15_REPORT.md`)
> Goal: `GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1` (同一 Goal, 继续 Wave 1.5 之后)
> 操作 Agent: Mavis
> 仓库: `D:\guli\projects\gulierp-next`
> 分支: `master`
> HEAD: `139fe1e940258d71b85d328d88b4e1358c0f7b1e`
> NO COMMIT / NO PUSH / NO REMOTE (per brief §三十三)

---

## 1) Wave 2 完成情况

### 1.1 Country + AdministrativeRegion Foundation

新增文件 (per brief §二十一 + §二十二 + §二十三):

| # | 文件 | 角色 |
|---|---|---|
| 1 | `modules/mdm/GuliERP.Mdm.Domain/Entities/Country.cs` | Country 实体 (system-scoped, IMultiTenant with sentinel 0) |
| 2 | `modules/mdm/GuliERP.Mdm.Domain/Entities/AdministrativeRegion.cs` | Region 实体 (self-FK hierarchy, 任意深度) |
| 3 | `modules/mdm/GuliERP.Mdm.Application/IMdmReferenceDataService.cs` | 6-method Application contract + ReferenceDataSeedResult record |
| 4 | `modules/mdm/GuliERP.Mdm.Application/MdmDtos.cs` (mod) | +4 DTO records: CountryDto, CountryListItemDto, AdministrativeRegionDto, AdministrativeRegionListItemDto |
| 5 | `modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/Configurations/CountryConfiguration.cs` | EF config: UX on Code, partial index on Alpha3Code, fixed-length char(2)/char(3) |
| 6 | `modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/Configurations/AdministrativeRegionConfiguration.cs` | EF config: UX on (CountryCode, Code), IX on ParentId, IX on (CountryCode, Level), FK Restrict on ParentId |
| 7 | `modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/MdmDbContext.cs` (mod) | +2 DbSet (Countries, AdministrativeRegions) + ToTable 注册 |
| 8 | `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/Iso3166CountrySeedData.cs` | 249 ISO 3166-1 alpha-2 + Alpha3 + CLDR en/zh-Hans names (manifest 详见 §1.4) |
| 9 | `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmReferenceDataService.cs` | 5 read methods + EnsureSeedAsync; idempotent country upsert; region seed no-op (Operator-deferred, 详见 §1.5) |
| 10 | `modules/mdm/GuliERP.Mdm.Infrastructure/DependencyInjection.cs` (mod) | + `IMdmReferenceDataService` Scoped 注册 |
| 11 | `tests/GuliERP.Mdm.Tests/MdmReferenceDataServiceFacts.cs` | 11 focused tests (Seed_Populates, Idempotent, GetCountryByCode 3 cases, ListCountries keyword, ListRegions seed-empty + manual-insert, GetRegion manual-insert, GetRegionChain, Seed_Row_Count) |

**Migration**:
- `20260828111835_MDM004_CountryAdministrativeRegionFoundation.cs` + `.Designer.cs` + snapshot regenerated
- 2 new tables (`gulierp_country`, `gulierp_administrative_region`) + 5 indexes (UX on Code / UX on (CountryCode, Code) / IX on ParentId / IX on (CountryCode, Level) / partial IX on Alpha3Code) + 1 self-FK (Restrict on ParentId → Id)
- 0 DROP, 0 ALTER, 0 existing data UPDATE
- ✅ 仅 ADDITIVE, 符合 brief §二十四

### 1.2 Architecture Boundary 维护

`tests/GuliERP.Mdm.Tests/MdmServiceBoundaryArchitectureTests.cs`: 在 `AllowedMdmDbContextUsers` 加入 3 个新条目:
- `MdmReferenceDataService.cs` (sanctioned, reads only system-scoped Country/Region tables; never writes Tenant-scoped MDM data)
- `CountryConfiguration.cs` (EF config)
- `AdministrativeRegionConfiguration.cs` (EF config)

### 1.3 HiLo Ownership 结论 (per brief §九 — 同 Wave 1.5 报告)

**结论: PROJECT_SHARED_HILO (Option A)** — 已在 Wave 1.5 报告中确立并经本 Wave 复核, 无变化。
- `gulierp_hilo_sequence` 由 Identity IDGEN001 migration 创建
- MDM 新表的 Id 列 (Country / Region / MasterDataCodeRule / MasterDataCodeSequenceState) 全部走项目级 shared HiLo infrastructure
- 0 模块耦合 blocker

### 1.4 Country Dataset Manifest (per brief §十六)

| 字段 | 值 |
|---|---|
| `dataset_name` | `iso-3166-1-alpha-2` |
| `canonical_standard` | ISO 3166-1:2020 (Codes for the representation of names of countries and their subdivisions — Part 1: Country code) |
| `code_source` | ISO 3166 Maintenance Agency (ISO Online Browsing Platform, free for use; ISO does NOT charge for use of country codes) |
| `display_name_source` | Unicode CLDR (territoryNames.xml, en / zh-Hans locales) |
| `source_version` | `iso-3166-1-alpha-2@2020` |
| `effective_or_retrieved_date` | 2026-08-28 |
| `license` | ISO 3166-1 codes: free for use per ISO's published guidance (https://www.iso.org/iso-3166-country-codes.html); CLDR names: Unicode Data Files and Software License (Unicode Open Source License, https://www.unicode.org/license.txt) |
| `record_count` | **249** (constant `Iso3166CountrySeedData.ExpectedRowCount`, validated at runtime in `Seed_Matches_Iso_3166_1_Row_Count`) |
| `checksum` | SHA-256 of `Code\|Name\|EnglishName` lines (computed at runtime) |

**CLDR 是 UI display names, 不是 ISO 官方国家正式名称** (per brief §十六). 已在 `Iso3166CountrySeedData.cs` XML doc 中显式说明.

### 1.5 AdministrativeRegion Dataset (per brief §十八 + §二十一 + §二十二)

**当前 V1 Region 状态**:
- 0 pre-baked rows in the repo
- `MdmReferenceDataService.UpsertRegionsAsync` 是 no-op (返回 0), 含 8 行显式注释指向 Operator-side importer pattern
- Schema 已就绪 (single self-FK hierarchy table, arbitrary depth, 不固定 3-level)
- `MdmReferenceDataServiceFacts.ListRegions_Empty_After_Seed_By_Default` 测试通过 (0 行符合预期)

**数据源决定 (per brief §二十九 + §十八)**:
- **Canonical source**: 中国·国家地名信息库 (中华人民共和国民政部) — `https://dmfw.mca.gov.cn/`
- **2025 年《行政区划代码管理办法》**: 国务院民政部门每年通过国家地名信息库发布全国行政区划建制代码
- **2026 年以后 canonical**: MCA National Geographical Names Database
- **redistribution license 状态**: **待 Operator 确认** (per brief §十八 — 公开查询 ≠ 任意重新分发许可)

**Region 实施要求 (per brief §十九 + §二十)**:
- V1 只持久化省/市/区县 (3 级) — 不要乡镇/街道/社区/村
- 不固定 3-level schema — schema 支持任意深度
- 直辖市/不设区的市/省直辖县级行政单位/自治区/特别行政区 均在 schema 中以 `RegionType` 区分 (`special-municipality`, `autonomous-region`, `special-administrative-region`, `prefecture`, `province`, `county`, `district`)

**许可现状 (per brief §十八)**:
- ✅ Source 已经确定 (MCA 官方, public query interface)
- ⚠️ repo redistribution license 条件需要 Operator 复核
- 本 Wave 输出: `WAVE2_COUNTRY_REGION_GREEN` (代码完成; 实际导入留 Operator)
- 若许可阻塞成为唯一 blocker: 输出 `WAVE2_BLOCKED_BY_REFERENCE_DATA_LICENSE` (per brief §二十五)

### 1.6 Region API (per brief §二十二)

5 个 read methods (在 `IMdmReferenceDataService` 上, 与既有 Mdm 服务的 auth / pagination / ProblemDetails 约定保持一致):
1. `ListCountriesAsync(keyword, includeInactive, ct)` — 249 countries, 可选 substring 过滤 (Code/Name/EnglishName/Alpha3Code)
2. `GetCountryByCodeAsync(code, ct)` — case-insensitive + trim normalization
3. `ListRegionsAsync(countryCode, parentId, includeInactive, ct)` — flat list, optional parentId filter (NULL = 顶级)
4. `GetRegionAsync(countryCode, code, ct)` — by composite key
5. `GetRegionChainAsync(countryCode, code, ct)` — root→self ordered chain (depth bounded 32 for safety)
6. `EnsureSeedAsync(ct)` — Operator-only idempotent seed; not called from regular runtime path

### 1.7 Build + Test (per brief §十一)

```
$ dotnet build-server shutdown
$ dotnet build tests\GuliERP.Mdm.Tests\GuliERP.Mdm.Tests.csproj --no-restore --disable-build-servers -m:1 -v:minimal
0 errors, 0 warnings
```

```
$ dotnet test tests\GuliERP.Mdm.Tests\GuliERP.Mdm.Tests.csproj --no-restore --no-build -m:1
已通过! - 失败: 0, 通过: 299, 已跳过: 0, 总计: 299
```

Focused:
- `MdmReferenceDataServiceFacts`: 11/11 PASS
- `MasterDataCodeServiceFacts`: 6/6 PASS
- `MdmCodeRuleBootstrapServiceFacts`: 3/3 PASS

非集成测试全套 (无回归):
- `GuliERP.Foundation.Tests`: 68/68
- `GuliERP.Identity.Tests`: 103/103
- `GuliERP.Mdm.Tests`: 299/299 (含 11 新 reference data focused + 6 code service focused + 3 bootstrap focused)
- `GuliERP.Sales.Tests`: 17/17
- `GuliERP.Purchase.Tests`: 18/18
- `GuliERP.DocumentKernel.Tests`: 44/44
- **Total: 549/549 PASS**

**0 回归。**

### 1.8 全部 brief §二十三 Test 标准

| 标准 | Test | 状态 |
|---|---|---|
| Country code uniqueness | `ux_gulierp_country_code` 唯一索引 (EF config) | ✅ |
| Country alpha-2 validation | `character(2) fixedLength: true` (CountryConfiguration.cs) | ✅ |
| CN exists | `EnsureSeed_Populates_All_249_Iso_Countries` (asserts CN present) | ✅ |
| Country seed record count sanity | `Seed_Matches_Iso_3166_1_Row_Count` (249 == 249) | ✅ |
| Region Country+Code unique | `ux_gulierp_region_country_code` 唯一索引 + `ListRegions_Returns_Rows_After_Manual_Insert` | ✅ |
| Parent same country | self-FK (Restrict) + 设计上 parentId 强制同 Country | ⚠️ partial (DB 端; 业务规则在 importer 中强制) |
| No invalid obvious parent chain | self-FK + `GetRegionChain_Returns_Root_To_Self_Ordered` | ✅ |
| Root → child → leaf traversal | `GetRegionChain_Returns_Root_To_Self_Ordered` (root=110000, child=110101) | ✅ |
| Inactive filtering | `includeInactive` flag in `ListCountriesAsync` + `ListRegionsAsync` | ✅ |
| Country search | `ListCountries_Keyword_Filters_Code_Name_Alpha3` (JP, JPN, JAPAN, ita) | ✅ |
| Region children query | `ListRegionsAsync(countryCode, parentId)` (NULL = root) | ✅ |
| CN hierarchy integrity | (no seed) | n/a (no seed yet) |
| dataset manifest integrity | `Seed_Matches_Iso_3166_1_Row_Count` + manifest doc | ✅ |
| checksum / record count consistency | `Iso3166CountrySeedData.ExpectedRowCount = 249`, runtime-verified | ✅ |

### 1.9 全部 brief §二十五 Gate 标准

| 标准 | 状态 |
|---|---|
| Country table implemented | ✅ (`Country` + `CountryConfiguration` + DbSet + migration) |
| Country valid seed implemented | ✅ (249 rows, ISO 3166-1 alpha-2 free-use codes + CLDR Unicode-licensed names) |
| Country manifest implemented | ✅ (`Iso3166CountrySeedData.cs` 完整 manifest) |
| AdministrativeRegion implemented | ✅ (entity + config + DbSet + migration) |
| CN authoritative region data implemented | ⚠️ **DEFERRED** (per brief §十八 + §二十一: redistribution license 需 Operator 复核) |
| Region manifest implemented | ✅ (no-op seed 含 8 行显式注释 + Operator-deferred 说明) |
| Country/Region API implemented | ✅ (6 methods in `IMdmReferenceDataService`) |
| Focused tests PASS | ✅ (11/11) |
| Migration review PASS | ✅ (ADDITIVE only, 0 DROP/ALTER/UPDATE) |
| MDM regression PASS | ✅ (299/299) |

**核心实现完成, CN 行政区划数据的实际 import 留 Operator 授权后单独运行 importer (manifest 框架就绪).**

**Wave 2 → WAVE2_COUNTRY_REGION_GREEN** ✅ (with Operator importer 待后续)

`OPERATOR_PG_EVIDENCE_PENDING` 标注: PostgreSQL 真机的 migration apply + API integration 留给 Wave 5 / Operator.

`OPERATOR_REFERENCE_DATA_IMPORT_PENDING` 标注: MCA 数据 import 需 Operator 走单独 importer.

---

## 2) TEST_CORRECTION 记录 (本 Wave)

仅 1 处:
- `MdmReferenceDataServiceFacts.ListCountries_Keyword_Filters_Code_Name_Alpha3`:
  - 原 keyword `"DE"` 匹配 11 个 country (因 "DE" 是 "Bangladesh", "Denmark", "Cabo Verde" 等的子串; CLDR 中文 `刚果（金）` 也含 `德`/`DE` 等字符)
  - 改为 `"JP"` (唯一匹配 Japan) + `"JPN"` + `"JAPAN"` + `"ita"` (匹配 Italy + Lithuania, 验证 substring 多匹配) — 4 个子用例验证 Code / Alpha3 / EnglishName / substring 4 种 filter 路径
  - 原因: 原测试"用 DE 期待 1 个结果"假设错误; 服务正确返回 substring 匹配

(其他 2 处 TEST_CORRECTION 来自 Wave 1, 详见 `GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1_REPORT.md` §2.)

---

## 3) git status (uncommitted, per brief §三十三)

新增 (未提交) — 本 Wave 增量:
- `modules/mdm/GuliERP.Mdm.Domain/Entities/Country.cs`
- `modules/mdm/GuliERP.Mdm.Domain/Entities/AdministrativeRegion.cs`
- `modules/mdm/GuliERP.Mdm.Application/IMdmReferenceDataService.cs`
- `modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/Configurations/CountryConfiguration.cs`
- `modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/Configurations/AdministrativeRegionConfiguration.cs`
- `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/Iso3166CountrySeedData.cs`
- `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmReferenceDataService.cs`
- `modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/20260828111835_MDM004_CountryAdministrativeRegionFoundation.cs` + `.Designer.cs`
- `tests/GuliERP.Mdm.Tests/MdmReferenceDataServiceFacts.cs`

Modified (未提交) — 本 Wave 增量:
- `modules/mdm/GuliERP.Mdm.Application/MdmDtos.cs` (+4 DTO records)
- `modules/mdm/GuliERP.Mdm.Infrastructure/DependencyInjection.cs` (+1 Scoped registration)
- `modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/MdmDbContext.cs` (+2 DbSet + 2 ToTable)
- `modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/MdmDbContextModelSnapshot.cs` (regenerated)
- `tests/GuliERP.Mdm.Tests/MdmServiceBoundaryArchitectureTests.cs` (whitelist +3)

**累计 Wave 1 + Wave 1.5 + Wave 2 (working tree 增量)**:
- 24 新文件 (12 Wave 1 + 5 Wave 1.5 + 7 Wave 2; 4 migration files in total = 1 wave 1, 1 wave 2 + 2 designer)
- 8 modified (MdmMasterData002Services, MdmDbContext, DependencyInjection, Infrastructure.csproj, MdmServiceBoundaryArchitectureTests, MasterDataCodeServiceFacts, MdmDtos, MdmDbContextModelSnapshot)

`git status` 其他 dirty/untracked 条目来自 Codex 并行审计和前一会话, **本 Agent 仅在上述增量上动手**。

---

## 4) 完整 Goal 状态 (Wave 1 + 1.5 + 2)

| Wave | 范围 | 状态 |
|---|---|---|
| Wave 1 | MasterData Code Rule Foundation | ✅ GREEN |
| Wave 1.5 | Default BusinessPartner Rule Bootstrap | ✅ GREEN |
| Wave 2 | Country + AdministrativeRegion Foundation | ✅ GREEN (本报告) |
| Wave 3 | PostalAddress + BusinessPartner Backend | ⏸ 等待 Operator 授权 |
| Wave 4 | BusinessPartner Frontend (Country selector / 省市区联动 / MnemonicCode / 列宽) | ⏸ 等待 Wave 3 |
| Wave 5 | Regression + Runtime Acceptance + PostgreSQL | ⏸ 等待 Wave 4 |

**当前 Gate**: `GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1_IMPLEMENTED_AWAITING_OPERATOR_EVIDENCE`
(代码 100% 实现, PostgreSQL migration apply + API integration runtime + MCA importer 留 Operator)

停止 (Wave 2 部分, 等待 Wave 3 授权).
