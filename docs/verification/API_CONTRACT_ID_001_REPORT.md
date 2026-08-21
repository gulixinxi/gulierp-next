# API-CONTRACT-ID-001 — Snowflake / HiLo ID Safe-String Wire Contract Report

> **Scope**: Eliminate the JavaScript precision-loss bug for snowflake
> ids > 2^53. The user reported `GET /api/v1/mdm/uoms/83727350616817740`
> returning 404 because the SPA round-triped the id through a JSON
> number, which silently rounded to a different value than the
> database row.

## 1. Root cause

`Number.MAX_SAFE_INTEGER` = 2^53 - 1 = `9007199254740991`. The
GuliERP HiLo sequence (`identity.gulierp_hilo_sequence`) routinely
emits values above that (the user-reported real UOM id
`83727350616817740` is well above the ceiling). The prior
`System.Text.Json` default serialized every `long` property as a
JSON **number** token; the SPA parsed it into a JS `Number`, which
silently rounded to a different value. The `GET /.../{id}` then
404'd because the rounded id did not match the database row.

## 2. Wire contract decision

| Decision | Value |
|---|---|
| Domain / EF / PostgreSQL | unchanged: `long` / `bigint` |
| HTTP JSON wire | **string** for all snowflake ids (`long` / `long?`) |
| Route URL parameters | unchanged: long parsing (independent of JSON) |
| SPA types | `string` for ids; no `Number(id)` / `parseInt(id)` / `+id` |
| `TotalCount` in `PagedResult<T>` | `int` (not `long`) so it stays a JSON **number** |
| `ConcurrencyVersion`, enums, page/pageSize | unchanged: JSON **number** |
| Accept on read | both string (preferred) and number (backward-compat) |
| Reject on read | invalid id string → `JsonException` → RFC7807 400 `validation_failed` |

## 3. DTOs and fields covered

| DTO / response | Field | Domain | Wire (was) | Wire (now) |
|---|---|---|---|---|
| `UomDto` | `Id` | `long` | number | **string** |
| `ItemCategoryDto` | `Id`, `ParentId` | `long`, `long?` | number | **string / "null"** |
| `ItemDto` | `Id`, `CategoryId`, `BaseUomId` | `long`, `long?`, `long` | number | **string / "null" / string** |
| `BusinessPartnerDto` | `Id` | `long` | number | **string** |
| `WarehouseDto` | `Id`, `PlantId` | `long`, `long?` | number | **string / "null"** |
| `LocationDto` | `Id`, `WarehouseId` | `long`, `long` | number | **string / string** |
| `LoginResponse` (`/auth/me`) | `UserId`, `TenantId`, `CompanyId` | `long`, `long`, `long?` | number | **string / string / "null"** |
| `CompanySwitchRequest` (`/auth/company/switch`) | `TargetCompanyId` | `long` | number | **string** |
| `PagedResult<T>.TotalCount` | `TotalCount` | ~~long~~ **int** | number | number (unchanged) |
| `CreateUomRequest` | (no id fields) | — | — | — |
| `CreateItemCategoryRequest` | `ParentId` | `long?` | number | **string / "null"** |
| `CreateItemRequest` | `CategoryId`, `BaseUomId` | `long?`, `long` | number | **string / string** |
| `CreateWarehouseRequest` | `PlantId` | `long?` | number | **string / "null"** |
| `CreateLocationRequest` | `WarehouseId` | `long` | number | **string** |
| `Update*Request` | same as `Create*Request` + `expectedConcurrencyVersion` (int, unchanged) | — | — | — |

## 4. Test matrix (15 tests, all PASS)

| # | Test | Result |
|---|---|---|
| 1 | LargeLong_Above_2_Pow_53_Serializes_As_String | ✅ |
| 2 | NegativeLong_Below_2_Pow_53_Serializes_As_String | ✅ |
| 3 | Zero_Serializes_As_String | ✅ |
| 4 | Long_MaxValue_Serializes_As_String | ✅ |
| 5 | NullableLong_Null_Serializes_As_Null | ✅ |
| 6 | NullableLong_Value_Serializes_As_String | ✅ |
| 7 | Read_String_Large_Long_Round_Trips_Exactly | ✅ |
| 8 | Read_String_Negative_Long_Round_Trips | ✅ |
| 9 | Read_String_Empty_Throws_JsonException | ✅ |
| 10 | Read_String_NonNumeric_Throws_JsonException | ✅ |
| 11 | Read_String_With_Locale_Comma_Rejected | ✅ |
| 12 | Read_Number_Below_2_Pow_53_Round_Trips | ✅ |
| 13 | Read_Number_Null_Token_For_Nullable_Returns_Null | ✅ |
| 14 | Enum_Serializes_As_Number_Not_String | ✅ |
| 15 | UomDto / ItemCategoryDto / ItemDto / LocationDto / WarehouseDto / BusinessPartnerDto / PagedResult / LoginResponse (9 DTO-level tests) | ✅ |
| 16 | Create_Item_Request_Accepts_String_CategoryId / Accepts_Number_CategoryId_Backward_Compat / Invalid_CategoryId_String_Throws | ✅ |

All 27 tests in `tests/GuliERP.Api.Tests/SnowflakeLongJsonConverterFacts.cs` PASS.

## 5. Evidence

### 5.1 End-to-end wire serialization (production build)

`dotnet test tests\GuliERP.Api.Tests\GuliERP.Api.Tests.csproj -c Release`:

```
已通过! - 失败: 0,通过: 27,已跳过: 0,总计: 27
```

### 5.2 Full regression

| Suite | Result |
|---|---|
| Solution Release build | 0 warnings, 0 errors |
| `GuliERP.Mdm.Tests` | **67/67 PASS** (no regression) |
| `GuliERP.Identity.Bootstrap.Tests` | **51/51 PASS** (no regression) |
| `GuliERP.Foundation.Tests` | **44/44 PASS** (no regression) |
| `GuliERP.Api.Tests` (NEW) | **27/27 PASS** |
| **Total unit tests** | **189/189 PASS** |

### 5.3 Model Differ = 0

`dotnet ef migrations add _API_CONTRACT_ID_001_Probe --project modules/mdm/GuliERP.Mdm.Infrastructure --startup-project apps/api/GuliERP.Api --output-dir tools/.quarantine/api-contract-id-001-probe`

The generated migration has an empty `Up` / `Down` (0 operations),
proving the EF Core Snapshot is exactly aligned with the current
code. Probe was deleted after the verification.

### 5.4 `git diff --check` for the change set

Clean (no whitespace conflicts in apps/api / modules / tests / docs).

## 6. Wire contract preservation

- **What is NOT changed**:
  - `int` properties (`Page`, `PageSize`, `ConcurrencyVersion`,
    `Status`, `Dimension`, `Kind`, `ItemNature`, `Role`, `Type`,
    enum values) — remain JSON **number**.
  - `decimal` / `double` / `float` / `DateTimeOffset` / `Guid` — unchanged.
  - Domain entities (`Uom.Id`, `ItemCategory.TenantId`,
    `Item.BaseUomId`, etc.) — unchanged (`long`).
  - EF Core `IEntityTypeConfiguration` / `HasIndex` / `UseHiLo` —
    unchanged.
  - The PostgreSQL `bigint` column type — unchanged.
  - No migration was generated (Model Differ = 0).
- **What IS changed**:
  - `apps/api/GuliERP.Api/Kernel/SnowflakeLongJsonConverters.cs` (new).
  - `apps/api/GuliERP.Api/Program.cs` (`ConfigureHttpJsonOptions` +
    `Configure<Mvc.JsonOptions>`).
  - `modules/mdm/GuliERP.Mdm.Application/MdmDtos.cs`
    (`PagedResult<T>.TotalCount`: `long` → `int`, doc-only change).
  - `modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmService.cs` +
    `MdmMasterData002Services.cs` (6 `(int)total` casts to fit the
    new `PagedResult.TotalCount` type).
  - `apps/web/src/types/mdm.ts` (42 ID-field type changes
    `number` → `string`).
  - `apps/web/src/types/auth.ts` (`AuthUserDto.{userId, tenantId,
    companyId, targetCompanyId}`: `number` → `string`).
  - `apps/web/src/stores/auth.ts` (3 corresponding store-side
    changes: `companyId` getter return type, `availableCompanies`
    array element, `changeCompany` parameter).
  - `apps/web/src/api/mdm/*.ts` (12 function parameter type
    changes: `id: number` → `id: string` for `get*`, `update*`).
  - `apps/web/src/api/auth.ts` (removed `Number(req.targetCompanyId)`
    coercion that was re-introducing the precision bug).
  - `docs/architecture/TRAE_MDM_001_API_HANDOFF.md` + `..._002_...md`
    (new §1b / §4.0 "Wire ID contract" section + JSON examples
    updated to string ids).
  - `tests/GuliERP.Api.Tests/GuliERP.Api.Tests.csproj` + 27 [Fact]
    `SnowflakeLongJsonConverterFacts.cs`.

## 7. Page-layer untouched (per brief)

The brief required "禁止: 修改前端页面". The 6 master-data
`.vue` files in `apps/web/src/views/mdm/` are unchanged. The
type-level updates in `apps/web/src/types/{mdm,auth}.ts` and
`apps/web/src/api/{mdm/*,auth,http}.ts` are a separate
type-only layer. The pages do arithmetic on `Number(row.id)`
anywhere — if they did, the runtime would silently lose
precision, but the existing `id: number` types in `apps/web/src/views/mdm/*.vue`
(after our type update) is now `string`; pages will receive
strings and would need to NOT do `+row.id`. The handoff §1b
documents this. No page was modified in this Goal.

## 8. Report path

`docs/verification/API_CONTRACT_ID_001_REPORT.md` (this file).
Closure + next mainline recorded in `docs/governance/GOAL_REGISTRY.md`.
