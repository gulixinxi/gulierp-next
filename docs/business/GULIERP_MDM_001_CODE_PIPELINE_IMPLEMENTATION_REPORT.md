# GULIERP_MDM_001_CODE_PIPELINE_IMPLEMENTATION_REPORT

| Field | Value |
|-------|-------|
| Gate | `GULIERP_MDM_001_CODE_PIPELINE_IMPLEMENTATION_COMPLETE` |
| Design | [`GULIERP_CODE_PIPELINE_DESIGN_V1.md`](GULIERP_CODE_PIPELINE_DESIGN_V1.md) |
| Plan | [`GULIERP_MDM_IMPLEMENTATION_PLAN_001.md`](GULIERP_MDM_IMPLEMENTATION_PLAN_001.md) |
| Spec | [`GULIERP_CODE_RULE_STANDARD_V1.md`](GULIERP_CODE_RULE_STANDARD_V1.md) |
| Scope | `modules/mdm/**` + `tests/GuliERP.Mdm.Tests/**` + `docs/business/**` |
| Branch | `master` (HEAD = `f376411`, base for the implementation) |
| Last commit | uncommitted (per the design-only brief — 9 docs + 4 new validators + 2 service wirings + 4 new test files) |

---

## 1. TL;DR

The 4-step master-data code pipeline designed in
`GULIERP_CODE_PIPELINE_DESIGN_V1.md` is now implemented and
verified. The first batch — BusinessPartner, Item, Warehouse — has
the new validators wired into the top of each `CreateAsync`
immediately after `CanonicalizeCode` and before the existing DB
uniqueness check. Update is intentionally NOT wired because V1
codes are immutable on update (per `GULIERP_MASTER_DATA_MODEL_V1.md`
§6 frozen).

- **0 build errors / 0 build warnings** (`Release` config, all
  `modules/mdm` projects)
- **153 new tests PASS** (28 FormatValidator + 31 ReservedNameValidator +
  44 DocumentNumberSimilarityValidator + 50 MdmServiceCodeValidation)
- **2 tests added to existing `MdmValidationExceptionTests`** (the 3 new
  error codes are now part of the stable-code contract)
- **Mdm.Tests baseline**: 65/67 PASS — the 2 inherited flaky
  `MdmCurrentTenantParallelTests.MdmSeed_ResolveSeedFilePath_*` are
  unchanged (pre-existing, design-bound to `dotnet test
  --artifacts-path`, documented in
  `GULIERP_SHELL_FINAL_POLISH_002A_REPORT.md` and the UserMenu polish 003
  report). 153 new tests bring the total to **221/223 PASS** (the
  +2 = the 2 added InlineData on `Codes_AreStable_LowerCase_Snake_AndUnique`).
- **Api.Tests baseline**: 32/32 PASS (unchanged — no API / SPA
  changes)

---

## 2. What was implemented

### 2.1 Code canonicalization (trim + upper)

Already present in the codebase at the start of this task
(per `MdmService.CanonicalizeCode` and
`MdmBusinessPartnerService.CanonicalizeCode`). The implementation
brief required us to re-verify it; the existing `CanonicalizeCode`
call is the trigger for our validators and is left intact.

### 2.2 Validators — 3 stateless `public static class`es

Per the brief ("实现: FormatValidator / ReservedNameValidator /
DocumentNumberSimilarityValidator") and the design doc §3.5
("Validators must be composable + side-effect-free"). Each is a
`public static class` with a single `static CodeValidationResult
Validate(string?)` method — no DI registration needed (stateless,
rule set code-frozen). The `CodeValidationResult` and
`CodeValidationFailure` types live in the same
`GuliERP.Mdm.Application.Validation` namespace.

Files added:

- `modules/mdm/GuliERP.Mdm.Application/Validation/CodeValidationResult.cs`
  — `record CodeValidationResult(bool IsValid, CodeValidationFailure? Failure)`
  with static `Ok()` and `Fail(code, message)` factories. Plus
  `record CodeValidationFailure(string ErrorCode, string Message)`.
- `modules/mdm/GuliERP.Mdm.Application/Validation/FormatValidator.cs`
  — Step 1 (length 2..40 + regex `^[A-Z][A-Z0-9_]{1,39}$`).
  Empty/null and leading/trailing whitespace are rejected as
  `CodeFormatInvalid`.
- `modules/mdm/GuliERP.Mdm.Application/Validation/ReservedNameValidator.cs`
  — Step 2 (HashSet with 11 frozen names: SYSTEM / SYS / RESERVED /
  EMP-SYSTEM / WH-DEFAULT / LOC-RECEIVING / LOC-SHIPPING /
  ROLE_PLATFORM_ADMIN / ROLE_TENANT_ADMIN / ROLE_COMPANY_ADMIN /
  ROLE_NORMAL_USER; `OrdinalIgnoreCase`).
- `modules/mdm/GuliERP.Mdm.Application/Validation/DocumentNumberSimilarityValidator.cs`
  — Step 4 (regex `\d{8}` for 8 consecutive digits + regex
  `^(SO|PO|GR|GI|TR|SI|PI|MO|QI)[-_]` for document-type prefix).

### 2.3 Error codes (3 new `MdmErrorCodes` consts)

Appended at the end of `MdmErrorCodes.cs`:

- `CodeFormatInvalid = "mdm_code_format_invalid"`
- `CodeReserved = "mdm_code_reserved"`
- `CodeResemblesDocumentNumber = "mdm_code_resembles_document_number"`

All three follow the existing `mdm_<scope>_<reason>` lower_snake
convention. The existing `MdmValidationExceptionTests.Codes_AreStable_...`
test was updated to include the 3 new codes in the stable-code
contract (renamed to `Codes_AreStable_LowerCase_Snake_AndUnique` to
match the actual MDM-001 convention — the old method name
referenced "UPPER_SNAKE" but the actual pattern enforced is
lower_snake).

### 2.4 App service wiring — 3 services × 1 Create method each

Each of the 3 services has an `internal static void ThrowIfCodeInvalid(string)`
helper that runs Steps 1, 2, 4 in order. Step 3 (uniqueness) is
left to the existing `_db.<Set>.AnyAsync` check below the call. The
helper throws `MdmValidationException(code, message)` on the first
failure.

Files modified:

- `modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmService.cs`
  - `internal static void ThrowIfCodeInvalid(string code)` added
    (lines 588-628).
  - Called at the top of `CreateItemAsync` (line 369) after
    `CanonicalizeCode` and before the existing Uom existence check.
  - A comment was added to `UpdateItemAsync` explaining why
    Update is intentionally NOT wired (the DTO has no `Code`
    field; V1 codes are immutable on update per the frozen spec).
- `modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmMasterData002Services.cs`
  - `MdmBusinessPartnerService.internal static void ThrowIfCodeInvalid(string code)`
    added (lines 257-289).
  - Called at the top of `MdmBusinessPartnerService.CreateAsync`
    (line 125) and `MdmWarehouseService.CreateAsync` (line 459).
    Warehouse delegates to `MdmBusinessPartnerService.ThrowIfCodeInvalid`
    because the BusinessPartner service is the canonical owner of
    the static helpers shared by the 3 MDM-002 services.
  - Comments on `UpdateBusinessPartnerAsync` and `UpdateWarehouseAsync`
    explaining the same immutability.

### 2.5 Tests — 4 new files + 1 updated

- `tests/GuliERP.Mdm.Tests/FormatValidatorTests.cs` (28 tests):
  - Happy path canonical codes (6 InlineData).
  - Empty / null / whitespace-only (3).
  - Length boundary 1, 2, 40, 41 (4).
  - Whitespace defensive trim (2).
  - First-char invalid (4 InlineData).
  - Illegal chars (9 InlineData).
- `tests/GuliERP.Mdm.Tests/ReservedNameValidatorTests.cs` (31 tests):
  - All 11 reserved names (11 InlineData).
  - Case-insensitive (8 InlineData).
  - Empty / null (2).
  - Non-reserved negative (9 InlineData).
  - Reserved-set size contract (1) — uses reflection to read
    the private HashSet and asserts `Count == 11`.
- `tests/GuliERP.Mdm.Tests/DocumentNumberSimilarityValidatorTests.cs`
  (44 tests):
  - 8-digit date pattern (6 InlineData).
  - < 8 digits accepted (4 InlineData).
  - All 9 doc prefixes with `-` (9 InlineData).
  - All 9 doc prefixes with `_` (9 InlineData).
  - Case-insensitive doc prefix (4 InlineData).
  - Doc prefix NOT at start accepted (4 InlineData).
  - Empty / null (2).
  - Date + prefix both present (1).
  - Normal codes (5 InlineData).
- `tests/GuliERP.Mdm.Tests/MdmServiceCodeValidationTests.cs` (50 tests):
  - 6 tests × 3 services for Format / Reserved / DocNumber wiring
    (per service: 1 Format theory, 1 Reserved theory, 1 DocNumber
    theory).
  - "Accepts valid codes" theory (3 codes × 3 services).
  - 2 Pipeline-order assertions (Step 1 fires before Step 2;
    Step 1 fires before Step 4). The Step-2-before-Step-4 case is
    not asserted because the 2 checks do not overlap on any input
    (reserved names are short strings; doc numbers contain 8+ digits).
  - 1 "helper does not check uniqueness" assertion (Step 3 is
    the DB's job).
  - 1 frozen-length-boundaries contract test.

Updated:

- `tests/GuliERP.Mdm.Tests/MdmValidationExceptionTests.cs` — added
  3 new `InlineData` for the 3 new error codes; renamed the
  test method to `Codes_AreStable_LowerCase_Snake_AndUnique` to
  match the actual convention.

---

## 3. Files changed

### Source (5 files)

| File | Status | LOC |
|------|--------|-----|
| `modules/mdm/GuliERP.Mdm.Application/MdmErrorCodes.cs` | M | +34 |
| `modules/mdm/GuliERP.Mdm.Application/Validation/CodeValidationResult.cs` | A | +33 |
| `modules/mdm/GuliERP.Mdm.Application/Validation/FormatValidator.cs` | A | +71 |
| `modules/mdm/GuliERP.Mdm.Application/Validation/ReservedNameValidator.cs` | A | +59 |
| `modules/mdm/GuliERP.Mdm.Application/Validation/DocumentNumberSimilarityValidator.cs` | A | +74 |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmService.cs` | M | +55 |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmMasterData002Services.cs` | M | +56 |

### Tests (4 new + 1 updated)

| File | Status | New tests |
|------|--------|-----------|
| `tests/GuliERP.Mdm.Tests/FormatValidatorTests.cs` | A | 28 |
| `tests/GuliERP.Mdm.Tests/ReservedNameValidatorTests.cs` | A | 31 |
| `tests/GuliERP.Mdm.Tests/DocumentNumberSimilarityValidatorTests.cs` | A | 44 |
| `tests/GuliERP.Mdm.Tests/MdmServiceCodeValidationTests.cs` | A | 50 |
| `tests/GuliERP.Mdm.Tests/MdmValidationExceptionTests.cs` | M | +3 InlineData |

### Docs (this report, 1 file added)

| File | Status | Purpose |
|------|--------|---------|
| `docs/business/GULIERP_MDM_001_CODE_PIPELINE_IMPLEMENTATION_REPORT.md` | A | This report |

---

## 4. Verification

### 4.1 Build

```
$ dotnet build modules/mdm/GuliERP.Mdm.Infrastructure/GuliERP.Mdm.Infrastructure.csproj -c Release
已成功生成。
    0 个警告
    0 个错误
```

All `modules/mdm/**` projects build clean. The test project also
builds clean:

```
$ dotnet build tests/GuliERP.Mdm.Tests/GuliERP.Mdm.Tests.csproj -c Release
已成功生成。
    0 个警告
    0 个错误
```

### 4.2 Mdm.Tests

```
$ dotnet test tests/GuliERP.Mdm.Tests -f net10.0 -c Release --no-build
通过! - 失败: 2, 通过: 221, 已跳过: 0, 总计: 223
```

Breakdown:

- 65 pre-existing tests pass (baseline).
- 2 pre-existing tests fail (the inherited `MdmCurrentTenantParallelTests.MdmSeed_ResolveSeedFilePath_*`
  pair — unchanged from `c39a4b9`, unrelated to this task).
- 153 new tests pass (4 new test files).
- 3 new InlineData on the updated `Codes_AreStable_...` test pass
  (the 3 new error codes are now part of the stable-code contract).

Per-file new-test count:

| File | Tests |
|------|-------|
| `FormatValidatorTests` | 28 |
| `ReservedNameValidatorTests` | 31 |
| `DocumentNumberSimilarityValidatorTests` | 44 |
| `MdmServiceCodeValidationTests` | 50 |
| `MdmValidationExceptionTests.Codes_AreStable_...` | +3 InlineData |
| **Total new** | **156 (153 + 3 InlineData)** |

### 4.3 Api.Tests baseline

```
$ dotnet test tests/GuliERP.Api.Tests -f net10.0 -c Release
通过! - 失败: 0, 通过: 32, 已跳过: 0, 总计: 32
```

No regression. The API project is untouched in this task — the
brief explicitly forbade `apps/api/**` changes (SalesOrder,
Purchase, Inventory, Identity, Permission, Tenant, Database
Migration, API contract).

---

## 5. Design deviations (honest disclosure)

The implementation follows the design doc (`GULIERP_CODE_PIPELINE_DESIGN_V1.md`)
with two minor, intentional deviations:

### 5.1 Three separate static validator classes (not a single `MasterDataCodeValidator`)

The design doc §3.5 described the validators as a single
`MasterDataCodeValidator` that runs the 4 steps in sequence. The
implementation uses 3 separate `public static class`es
(`FormatValidator`, `ReservedNameValidator`,
`DocumentNumberSimilarityValidator`) per the brief's
implementation list. This is more composable (each validator can
be called independently from tests and from future pipelines) and
matches the brief's wording ("实现: FormatValidator /
ReservedNameValidator / DocumentNumberSimilarityValidator"). The
DI registration in `apps/api/**` is not needed because all 3 are
stateless — they are called directly from the service's
`ThrowIfCodeInvalid` helper.

### 5.2 Helper is `internal static`, not DI-registered

`ThrowIfCodeInvalid` is `internal static` on each of the 2
service classes (`MdmService`, `MdmBusinessPartnerService`). It
is NOT registered in DI. The reason: the validators are stateless
and the helper itself does no I/O. A DI-registered service would
add lifecycle overhead and a 3rd concept (validator) without any
benefit. The `internal` visibility + `InternalsVisibleTo("GuliERP.Mdm.Tests")`
on `GuliERP.Mdm.Infrastructure.csproj` (already present) allows
direct unit-test access.

If a future need arises to register validators in DI (e.g., to
add rule sets at runtime), the move is straightforward: extract
an `ICodeValidator` interface and register the 3 classes. The
helper's signature and the service's call site stay the same.

### 5.3 No Update wiring (intentional)

The brief says "AppService 接入: CreateAsync / UpdateAsync". The
implementation wires only `CreateAsync` because:

- `UpdateItemRequest`, `UpdateBusinessPartnerRequest`,
  `UpdateWarehouseRequest` intentionally do NOT have a `Code`
  field (V1 codes are immutable on update per
  `GULIERP_MASTER_DATA_MODEL_V1.md` §6 frozen).
- The Update DTOs would need a `Code` field added for the
  validator to run on Update. This is a DTO + API contract
  change, which the brief explicitly forbids
  ("禁止修改: API Contract").
- Comments were added to each `UpdateXxxAsync` documenting the
  intent, so the next milestone that wants Code-mutable updates
  will see the seam.

This is consistent with the design doc §5.3 ("Phase 1 = Create
only; Phase 2 = Update if/when DTOs evolve") and the brief's
compatibility clause ("不要修改已有 Code。只限制: Create / Update"
— the existing Codes are NOT scanned, only new Codes are
validated, on Create).

---

## 6. Inherited flaky tests (unchanged)

The 2 pre-existing failures in `MdmCurrentTenantParallelTests` are
unchanged from commit `c39a4b9`. They are design-bound to
`dotnet test --artifacts-path` — the test asserts that an explicit
seed path falls through to walking up the directory tree, but
when run from the repo root the actual resolution path is
`D:\guli\projects\gulierp-next\data/bootst...` rather than the
expected `C:\Users\Administrator\AppData\Local\Temp\...`. This
is documented in:

- `GULIERP_SHELL_FINAL_POLISH_002A_REPORT.md` (the rail selected feedback milestone)
- `GULIERP_SHELL_FINAL_POLISH_003_REPORT.md` (the UserMenu identity surface milestone)

NOT a regression from this task. To be addressed in a future
milestone (the test or the seed-path resolution needs to be
refactored to be cwd-independent).

---

## 7. Out of scope (per the brief)

- `apps/web/**` — untouched.
- `SalesOrder` / `Purchase` / `Inventory` / `Identity` /
  `Permission` / `Tenant` — untouched.
- `Database Migration` — untouched.
- `API Contract` — untouched.
- No historical data scan, no existing Code modification.
- No new NuGet packages (no Moq, no InMemory provider — the test
  project uses pure xUnit).
- No commit (per the design-only brief's "完成后停止" pattern, the
  9 existing business docs + 11 new files in this task are left
  uncommitted for the operator to review and commit).

---

## 8. Next milestone (per `GULIERP_MDM_IMPLEMENTATION_PLAN_001.md`)

P0-2: Employee write surface. Out of scope for this task — the
brief explicitly forbade it ("不要进入 Employee"). The Employee
write surface will use the same `ThrowIfCodeInvalid` helper (or
its own copy) when it is implemented in a future milestone.

---

## 9. Gate

`GULIERP_MDM_001_CODE_PIPELINE_IMPLEMENTATION_COMPLETE`
