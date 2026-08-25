# GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE_IMPLEMENTATION_REPORT

| Field | Value |
|-------|-------|
| Gate | `GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE_IMPLEMENTATION_COMPLETE` |
| Design | [`GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md`](../business/GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md) (FROZEN) |
| Plan | [`GULIERP_FOUNDATION_CODE_PIPELINE_MIGRATION_PLAN_001.md`](../business/GULIERP_FOUNDATION_CODE_PIPELINE_MIGRATION_PLAN_001.md) (FROZEN) |
| Branch | `master` (HEAD = `f376411`, base for the implementation) |
| Last commit | uncommitted (per the "no commit unless explicitly requested" preference; operator reviews and commits as a single atomic migration commit) |

---

## 1. TL;DR

The 4-step master-data code pipeline (3 validators + result type +
new `MasterDataCodeValidator` facade + 3 strategy interfaces) has
been promoted from `GuliERP.Mdm.Application.Validation` to
`GuliERP.Foundation.Validation`. The MDM module's services now
call the Foundation pipeline via a new `ForMdm` factory that
wires the MDM-namespaced error codes. The 3 static validators
took a new `ICodeValidationContext` parameter (no backward-
compat overload; the migration is a hard break in the
signature, atomic).

- **0 build errors / 0 build warnings** for the full
  solution (`dotnet build GuliERP.slnx -c Release`).
- **Foundation.Tests: 68/68 PASS**, including **24 new tests**
  (14 MasterDataCodeValidator + 6 ICodeRuleProvider + 4
  FoundationArchitecture).
- **Mdm.Tests baseline preserved: 221/223 PASS** — the 2
  pre-existing inherited flaky `MdmCurrentTenantParallelTests`
  remain flaky (no regression). All 153 of the
  `MdmErrorCodes.*`-related tests pass.
- **Api.Tests: 32/32 PASS** (unchanged).
- **Sales.Tests: 9/9 PASS** (unchanged).
- **Identity.Tests: 22/22 PASS** (unchanged).
- **Identity.Bootstrap.Tests: 64/64 PASS** (unchanged).
- **DocumentKernel.Tests: 44/44 PASS** (unchanged).

The wire contract is unchanged. `MdmErrorCodes.CodeFormatInvalid`
+ `MdmErrorCodes.CodeReserved` +
`MdmErrorCodes.CodeResemblesDocumentNumber` are still the
3 error codes that the MDM API endpoints return in the
ProblemDetails `code` field.

---

## 2. What was implemented

### 2.1 Foundation `Validation/` namespace (NEW)

Per `GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md` §3.1 +
`GULIERP_FOUNDATION_CODE_PIPELINE_MIGRATION_PLAN_001.md` §3.1,
9 new files in `modules/foundation/GuliERP.Foundation/Validation/`:

| File | Role | LOC |
|------|------|-----|
| `CodeValidationResult.cs` | `CodeValidationResult` + `CodeValidationFailure` records (moved from MDM) | +57 |
| `FormatValidator.cs` | Step 1 validator (moved from MDM; signature changed) | +85 |
| `ReservedNameValidator.cs` | Step 2 validator (moved; signature changed) | +71 |
| `DocumentNumberSimilarityValidator.cs` | Step 4 validator (moved; signature changed) | +88 |
| `ICodeValidationContext.cs` | NEW data interface | +69 |
| `CodeValidationContext.cs` | NEW concrete record + `Default` static | +36 |
| `ICodeValidator.cs` | NEW strategy interface (V1.5+ shape) | +49 |
| `ICodeRuleProvider.cs` | NEW rule-set interface + `V1FrozenRuleProvider` implementation | +90 |
| `MasterDataCodeValidator.cs` | NEW static facade (runs 4 steps in order) | +52 |

### 2.2 The 3 validators' new signature

All 3 validators changed from `Validate(string?)` to
`Validate(string?, ICodeValidationContext)`. The `context`
parameter carries the module-specific error codes that the
validators write into the `CodeValidationFailure.ErrorCode`.

Per the brief, the change is a hard break (no backward-compat
overload). The build at the Foundation level passes; the build
at the MDM level was broken until the call-site update in
`MdmService.cs` + `MdmMasterData002Services.cs` (Phase 5).

### 2.3 MDM `Validation/CodeValidationContextExtensions.cs` (NEW)

The only file in `GuliERP.Mdm.Application.Validation` after the
migration. Provides the `ForMdm(string entityScope, long tenantId,
long? companyId)` static factory that constructs a
`CodeValidationContext` with the 3 MDM-namespaced error codes.

The factory lives in the MDM module (NOT in Foundation) to
avoid a cyclic import: the Foundation would need to reference
`MdmErrorCodes` if it owned the factory. Per
`MIGRATION_PLAN_001.md` §3.3.1.

### 2.4 The MDM services' new helper signature

`MdmService.ThrowIfCodeInvalid` and
`MdmBusinessPartnerService.ThrowIfCodeInvalid` changed from
1-arg to 3-arg: `(string code, long tenantId, long? companyId)`.
The helper now constructs a `CodeValidationContext` via
`ForMdm(...)` and calls `MasterDataCodeValidator.Validate(code,
context)`. The wire contract (the error code written to the
`MdmValidationException`) is unchanged.

The 3 call sites are updated:
- `MdmService.CreateItemAsync` (line 375) — passes
  `(code, tenantId, companyId: null)` (Item is Tenant-scoped).
- `MdmBusinessPartnerService.CreateAsync` (line 128) — passes
  `(code, tenantId, companyId: null)` (BusinessPartner is
  Tenant-scoped).
- `MdmWarehouseService.CreateAsync` (line 463) — calls
  `MdmBusinessPartnerService.ThrowIfCodeInvalid(code, tenantId,
  companyId)` (Warehouse is Tenant+Company-scoped).

### 2.5 Foundation `Kernel/ErrorCodes` extension

3 new constants appended to
`modules/foundation/GuliERP.Foundation/Kernel/ErrorCodes.cs`:
- `CodeFormatInvalid = "code_format_invalid"`
- `CodeReserved = "code_reserved"`
- `CodeResemblesDocumentNumber = "code_resembles_document_number"`

These are the FALLBACK codes when a module does not provide
its own error code via `ICodeValidationContext.FormatInvalidErrorCode`
etc. The MDM module uses the MDM-namespaced codes (the
fallback codes are unused for the MDM flow).

### 2.6 Foundation `DependencyInjection.cs` extension

1 new line: `services.AddSingleton<ICodeRuleProvider>(V1FrozenRuleProvider.Instance);`

Per the brief, **no other DI refactor** is shipped. The
`ICodeValidator` interface is defined for V1.5+ shape but is
NOT DI-registered. The 3 static validators are NOT
DI-registered (they remain static classes, matching the prior
MDM pattern).

### 2.7 `tests/GuliERP.Foundation.Tests/` (NEW project)

A new xUnit test project with 3 new test files:

| File | Tests | Pattern |
|------|-------|---------|
| `MasterDataCodeValidatorTests.cs` | 14 | Tests the facade runs the 4 steps in order, honors the context's error codes, throws on null context. |
| `ICodeRuleProviderTests.cs` | 6 | Tests the V1 frozen values (11 reserved names, 9 doc prefixes, V1 format regex + length range). |
| `FoundationArchitectureTests.cs` | 4 | Tests the Foundation assembly does NOT reference any business module; the 10 expected types exist; the 3 new ErrorCodes constants exist; the DI registration is discoverable. |

Total new tests: **24**. Pre-existing test files
(`FoundationBoundaryTests.cs` + `Kernel/RequestIdValidatorTests.cs`)
give the project a baseline of 44 tests, for a total of 68 tests
in the project.

### 2.8 MDM test files re-targeted (NO MOVE)

Per the brief ("确保 现有 Mdm tests 不降低"), the 3 test
files (`FormatValidatorTests.cs` /
`ReservedNameValidatorTests.cs` /
`DocumentNumberSimilarityValidatorTests.cs`) STAY in
`tests/GuliERP.Mdm.Tests/`. They are re-targeted:
- `using GuliERP.Mdm.Application.Validation;` →
  `using GuliERP.Foundation.Validation;`
- Every `FormatValidator.Validate(code)` →
  `FormatValidator.Validate(code, MdmContext)` (where
  `MdmContext` is a static field that wires the MDM-namespaced
  error codes).
- Same pattern for the other 2 validator test files.

The 1 wiring test file (`MdmServiceCodeValidationTests.cs`,
50 tests) is re-targeted:
- Every `MdmService.ThrowIfCodeInvalid(code)` →
  `MdmService.ThrowIfCodeInvalid(code, tenantId: 0,
  companyId: null)`.
- Same for `MdmBusinessPartnerService.ThrowIfCodeInvalid`.

The Mdm.Tests test count is **unchanged** at 153 (28 + 31 + 44
+ 50). The total Mdm.Tests result is still 221/223 (153 + 68
others - 2 inherited flaky).

This is a **design deviation** from
`MIGRATION_PLAN_001.md` §5.1, which suggested moving the 3
test files to the new Foundation.Tests project. The brief's
strict requirement ("现有 Mdm tests 不降低") was honored by
keeping the test files in Mdm.Tests. The deviation is recorded
here.

### 2.9 Files deleted (per `MIGRATION_PLAN_001.md` §4.3)

| File | Reason |
|------|--------|
| `modules/mdm/GuliERP.Mdm.Application/Validation/CodeValidationResult.cs` | moved to Foundation |
| `modules/mdm/GuliERP.Mdm.Application/Validation/FormatValidator.cs` | moved to Foundation |
| `modules/mdm/GuliERP.Mdm.Application/Validation/ReservedNameValidator.cs` | moved to Foundation |
| `modules/mdm/GuliERP.Mdm.Application/Validation/DocumentNumberSimilarityValidator.cs` | moved to Foundation |

The `modules/mdm/GuliERP.Mdm.Application/Validation/` directory
is kept (it contains the new `CodeValidationContextExtensions.cs`).

---

## 3. Files changed

### 3.1 Source (5 modified, 10 new)

**New (10):**

| Path | Size |
|------|------|
| `modules/foundation/GuliERP.Foundation/Validation/CodeValidationResult.cs` | 1.6 KB |
| `modules/foundation/GuliERP.Foundation/Validation/FormatValidator.cs` | 2.7 KB |
| `modules/foundation/GuliERP.Foundation/Validation/ReservedNameValidator.cs` | 2.4 KB |
| `modules/foundation/GuliERP.Foundation/Validation/DocumentNumberSimilarityValidator.cs` | 3.0 KB |
| `modules/foundation/GuliERP.Foundation/Validation/ICodeValidationContext.cs` | 2.4 KB |
| `modules/foundation/GuliERP.Foundation/Validation/CodeValidationContext.cs` | 1.3 KB |
| `modules/foundation/GuliERP.Foundation/Validation/ICodeValidator.cs` | 1.7 KB |
| `modules/foundation/GuliERP.Foundation/Validation/ICodeRuleProvider.cs` | 3.2 KB |
| `modules/foundation/GuliERP.Foundation/Validation/MasterDataCodeValidator.cs` | 1.9 KB |
| `modules/mdm/GuliERP.Mdm.Application/Validation/CodeValidationContextExtensions.cs` | 2.5 KB |
| `tests/GuliERP.Foundation.Tests/GuliERP.Foundation.Tests.csproj` | 0.6 KB |
| `tests/GuliERP.Foundation.Tests/MasterDataCodeValidatorTests.cs` | 5.1 KB |
| `tests/GuliERP.Foundation.Tests/ICodeRuleProviderTests.cs` | 3.4 KB |
| `tests/GuliERP.Foundation.Tests/FoundationArchitectureTests.cs` | 7.0 KB |

**Modified (5):**

| Path | Change |
|------|--------|
| `modules/foundation/GuliERP.Foundation/Kernel/ErrorCodes.cs` | +3 consts |
| `modules/foundation/GuliERP.Foundation/DependencyInjection.cs` | +1 using + 1 line for `ICodeRuleProvider` |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmService.cs` | +1 using; `ThrowIfCodeInvalid` 1-arg → 3-arg; 1 call site updated |
| `modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmMasterData002Services.cs` | +1 using; `ThrowIfCodeInvalid` 1-arg → 3-arg; 2 call sites updated |
| `tests/GuliERP.Mdm.Tests/FormatValidatorTests.cs` | re-targeted (using + MdmContext + 2-arg call) |
| `tests/GuliERP.Mdm.Tests/ReservedNameValidatorTests.cs` | re-targeted (using + MdmContext + 2-arg call) |
| `tests/GuliERP.Mdm.Tests/DocumentNumberSimilarityValidatorTests.cs` | re-targeted (using + MdmContext + 2-arg call) |
| `tests/GuliERP.Mdm.Tests/MdmServiceCodeValidationTests.cs` | re-targeted (3-arg helper call) |

**Modified (1 solution file):**

| Path | Change |
|------|--------|
| `GuliERP.slnx` | No change needed (the `GuliERP.Foundation.Tests` project was already in the slnx at line 32; an accidental duplicate was added at line 41 and then removed) |

### 3.2 Deleted (4)

| Path | Reason |
|------|--------|
| `modules/mdm/GuliERP.Mdm.Application/Validation/CodeValidationResult.cs` | moved to Foundation |
| `modules/mdm/GuliERP.Mdm.Application/Validation/FormatValidator.cs` | moved to Foundation |
| `modules/mdm/GuliERP.Mdm.Application/Validation/ReservedNameValidator.cs` | moved to Foundation |
| `modules/mdm/GuliERP.Mdm.Application/Validation/DocumentNumberSimilarityValidator.cs` | moved to Foundation |

### 3.3 Out-of-scope (NOT changed, per the brief)

- `apps/web/**` — untouched.
- `SalesOrder` / `PurchaseOrder` / `Inventory` / `Production` /
  `Quality` — untouched.
- `Identity` (the `GuliErpUser` / `GuliErpRole` /
  `UserCompanyMembership` / `UserOrganizationMembership` /
  `UserRoleAssignment` / `Employee` entities) — untouched.
  The Employee write service is a future
  `GULIERP_HR_001_EMPLOYEE_WRITE_V1` milestone (the brief
  explicitly forbids it here).
- `Permission` / `Tenant` — untouched.
- `Database Migration` — untouched (no migration; the
  `foundation` schema is unchanged).
- `API Contract` — untouched (the existing MDM endpoints
  return the same `MdmErrorCodes.Code*` values in the
  ProblemDetails `code` field; the wire is unchanged).

---

## 4. Verification

### 4.1 Build

```
$ dotnet build GuliERP.slnx -c Release
已成功生成。
    0 个警告
    0 个错误
```

The full solution (9 projects including the new
`GuliERP.Foundation.Tests`) builds clean. The
`modules/foundation/GuliERP.Foundation` project builds with the
new `Validation/` namespace; the `modules/mdm/GuliERP.Mdm.Infrastructure`
project builds with the new `ThrowIfCodeInvalid` signature.

### 4.2 Foundation tests

```
$ dotnet test tests/GuliERP.Foundation.Tests -f net10.0 -c Release --no-build
通过! - 失败: 0, 通过: 68, 已跳过: 0, 总计: 68
```

Per-file new-test count:
- `MasterDataCodeValidatorTests`: 14
- `ICodeRuleProviderTests`: 6
- `FoundationArchitectureTests`: 4
- **Total new: 24** (the 44 pre-existing tests are
  `FoundationBoundaryTests.cs` + `Kernel/RequestIdValidatorTests.cs`)

### 4.3 Mdm tests (regression)

```
$ dotnet test tests/GuliERP.Mdm.Tests -f net10.0 -c Release --no-build
通过! - 失败: 2, 通过: 221, 已跳过: 0, 总计: 223
```

The 2 failures are the same pre-existing inherited flaky
`MdmCurrentTenantParallelTests.MdmSeed_ResolveSeedFilePath_*`
tests (design-bound to `dotnet test --artifacts-path` cwd
resolution, unchanged from the baseline at commit `c39a4b9`).

All 153 Mdm-code-related tests pass (28 Format + 31 Reserved +
44 DocSim + 50 SvcWiring). The Mdm test count is **unchanged**
(153 expected pass + 68 other tests = 221 pass; -2 inherited
flaky = 221/223).

### 4.4 Other test projects (regression check)

| Project | Test count | Result |
|---------|-----------|--------|
| `GuliERP.Api.Tests` | 32/32 | PASS (unchanged) |
| `GuliERP.Sales.Tests` | 9/9 | PASS (unchanged) |
| `GuliERP.Identity.Tests` | 22/22 | PASS (unchanged) |
| `GuliERP.Identity.Bootstrap.Tests` | 64/64 | PASS (unchanged) |
| `GuliERP.DocumentKernel.Tests` | 44/44 | PASS (unchanged) |

**No regression on any test project.**

### 4.5 The 4-step pipeline (regression check)

The 4-step pipeline is unchanged. The 3 validators in
`GuliERP.Foundation.Validation` have the same logic as the old
MDM-namespace versions. The Mdm.Tests test suite (103 validator
tests + 50 wiring tests) verifies the behavior end-to-end.

### 4.6 Secret scan / NuGet vuln

No new NuGet packages. The `GuliERP.Foundation` project
references are unchanged (EF Core + Npgsql). The new
`GuliERP.Foundation.Tests` project adds 3 xUnit packages
(`xunit` / `xunit.runner.visualstudio` / `Microsoft.NET.Test.Sdk`),
the same packages used by every other test project in the
solution.

### 4.7 Architecture: Foundation depends on no module

The new `FoundationArchitectureTests` includes a
reflection-based test that walks every public type in the
Foundation assembly and asserts that no type references
any assembly starting with `GuliERP.Mdm`,
`GuliERP.Identity`, `GuliERP.Sales`, `GuliERP.Purchase`,
`GuliERP.Inventory`, `GuliERP.Production`, `GuliERP.Quality`,
`GuliERP.DocumentKernel`, or `GuliERP.HR`.

The test passes (the Foundation assembly has no upward
dependency).

---

## 5. Design deviations from the migration plan

Per `MIGRATION_PLAN_001.md`, the design called for moving the 3
test files (`FormatValidatorTests` /
`ReservedNameValidatorTests` /
`DocumentNumberSimilarityValidatorTests`) from
`tests/GuliERP.Mdm.Tests/` to `tests/GuliERP.Foundation.Tests/`.
The Mdm test count would drop from 153 to 50; the Foundation
test count would grow from 44 to 147 (103 moved + 24 new).

The brief explicitly requires "确保 现有 Mdm tests 不降低"
("the existing Mdm tests don't decrease"). The strictest
interpretation is "Mdm.Tests project count is unchanged at 153".

**Decision:** the 3 test files STAY in `tests/GuliERP.Mdm.Tests/`.
The `using` statement + 2-arg call signature are re-targeted.
The Mdm.Tests project count is preserved at 153. The
`Foundation.Tests` project gets only the 3 NEW test files (24
new tests), not the 103 moved tests.

**Result:**
- `tests/GuliERP.Mdm.Tests/`: 153 code-pipeline tests (unchanged
  count, slightly re-targeted body).
- `tests/GuliERP.Foundation.Tests/`: 24 new tests (14
  MasterDataCodeValidator + 6 ICodeRuleProvider + 4
  FoundationArchitecture), plus 44 pre-existing tests = 68
  total.

The deviation is recorded here for the operator's review.

---

## 6. The brief's "CodeDocumentSimilarity" name

The brief uses the term `MdmErrorCodes.CodeDocumentSimilarity`
in the "保留" list. The actual existing constant is
`MdmErrorCodes.CodeResemblesDocumentNumber` (= "mdm_code_resembles_document_number").

**Decision:** the existing name `MdmErrorCodes.CodeResemblesDocumentNumber`
is preserved (per the brief's "保留" intent). The brief's
`CodeDocumentSimilarity` appears to be a colloquial reference
to the validator class name `DocumentNumberSimilarityValidator`,
not the error code. No new `CodeDocumentSimilarity` constant
is created.

The 3 preserved MDM error codes are:
- `MdmErrorCodes.CodeFormatInvalid = "mdm_code_format_invalid"`
  (unchanged).
- `MdmErrorCodes.CodeReserved = "mdm_code_reserved"`
  (unchanged).
- `MdmErrorCodes.CodeResemblesDocumentNumber = "mdm_code_resembles_document_number"`
  (unchanged; the brief's `CodeDocumentSimilarity` is the
  validator class name, not the error code).

---

## 7. The brief's "不进行 DI 重构" / "不进行 Tenant Rule 扩展"

Per the brief, the milestone is a **minimum viable Foundation
promote** without V1.5+ refactors. Concretely:

- **No DI refactor** beyond the 1-line `ICodeRuleProvider`
  registration. The 3 static validators do NOT implement
  `ICodeValidator` in V1 (V1.5+ will wrap them as adapters).
  The `MasterDataCodeValidator` facade is a `public static
  class` (no DI).
- **No Tenant Rule extension.** The `V1FrozenRuleProvider`
  returns the V1 frozen values for every context. No per-Tenant
  rule set. No per-Company rule set. The `ICodeRuleProvider`
  interface is defined for V1.5+ shape but the V1
  implementation is a singleton that returns the same values
  for every call.

The interface definitions (`ICodeValidator` /
`ICodeRuleProvider`) are SHIPPED in V1 because they lock the
V1.5+ shape; the V1 static classes do NOT use them. V1.5+ is
a separate design goal (`GULIERP_FOUNDATION_002_V15_DI_REFACTOR`).

---

## 8. Out-of-scope (per the brief)

- `apps/web/**` — untouched.
- `SalesOrder` / `Purchase` / `Inventory` / `Identity` /
  `Permission` / `Tenant` — untouched.
- `Database Migration` — untouched.
- `API Contract` — untouched.
- **Employee write surface** — the brief explicitly forbids
  entering Employee. The Employee write service is a future
  `GULIERP_HR_001_EMPLOYEE_WRITE_V1` milestone that DEPENDS on
  this Foundation promote.
- **Sales / Purchase / Inventory error code extension** —
  V1.5+ when those modules ship code-bearing entities.
- **Auto-coding engine** — V2+ per the V1 frozen model.
- **Internal reference number on documents** — V2+.
- **V1.5+ DI refactor** — separate design goal.

---

## 9. Pending follow-ups

The following are explicit follow-ups (NOT in this milestone):

1. **`GULIERP_HR_001_EMPLOYEE_WRITE_V1`** (the Employee write
   surface). DEPENDS on this Foundation promote. Adds the
   `ForIdentity` factory + the 12 new
   `IdentityErrorCodes.EmployeeCode*` consts + the
   `EmployeeWriteService.ThrowIfEmployeeCodeInvalid` helper
   that calls the new `MasterDataCodeValidator`.
2. **`GULIERP_FOUNDATION_002_V15_DI_REFACTOR`** (V1.5+).
   Wrap the 3 static validators as `ICodeValidator` adapters;
   DI-register the collection; add a DI-resolving facade.
3. **`GULIERP_FOUNDATION_003_V15_RULE_PROVIDER_PER_TENANT`**
   (V1.5+). Per-Tenant rule set layering on top of the V1
   frozen set.

---

## 10. Gate

`GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE_IMPLEMENTATION_COMPLETE`

The implementation milestone is complete. The next milestone
(`GULIERP_HR_001_EMPLOYEE_WRITE_V1`) opens after this gate
fires.

---

## 11. Honest disclosure (residual gaps)

1. **The 2 pre-existing inherited flaky tests** in
   `GuliERP.Mdm.Tests.MdmCurrentTenantParallelTests` are NOT
   fixed. They are documented in
   `GULIERP_MDM_001_CODE_PIPELINE_IMPLEMENTATION_REPORT` §6
   and the prior shell-polish reports. NOT a regression.
2. **The 3 MDM validator test files were NOT moved** to
   `GuliERP.Foundation.Tests` (per the design deviation in
   §5). The 103 tests stay in Mdm.Tests. The Foundation.Tests
   project has only the 24 new tests.
3. **`GuliERP.slnx`** had a duplicate Foundation.Tests entry
   at line 41 (added accidentally during this milestone; the
   original entry at line 32 was already there). The duplicate
   was removed. The slnx now has only 1 entry for
   `tests/GuliERP.Foundation.Tests/GuliERP.Foundation.Tests.csproj`.
4. **The brief uses `MdmErrorCodes.CodeDocumentSimilarity`**
   which doesn't exist; the actual name is
   `MdmErrorCodes.CodeResemblesDocumentNumber`. The existing
   name is preserved (§6).
5. **The Foundation DI registers only `ICodeRuleProvider`**
   (1 line). The 3 static validators are NOT DI-registered
   (V1.5+). Per the brief's "不进行 DI 重构".
6. **Backend dev-server (PID 46776)** is still down from the
   prior milestones; the operator must restart it with
   `tools/dev/run-web-preview-backend.ps1` (with PGPASSWORD) to
   verify the unchanged API behavior live. The
   `MdmErrorCodes.Code*` values returned in ProblemDetails are
   unchanged, so the API contract is preserved.
