# GULIERP_FOUNDATION_CODE_PIPELINE_MIGRATION_PLAN_001

> Goal: design the implementation path that ships the V1
> Foundation Code Pipeline promote per the
> `GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md` contract.
> This is the migration plan — the model freeze is the first
> doc. The plan covers the atomic migration of the 3
> validators + `CodeValidationResult` from the MDM module to
> the Foundation module, the addition of the new
> `MasterDataCodeValidator` facade + the 3 new interfaces
> (`ICodeValidator` / `ICodeRuleProvider` /
> `ICodeValidationContext`), the call-site update in
> `MdmService` + `MdmMasterData002Services`, the test split
> (3 files move to a new `GuliERP.Foundation.Tests` project
> + 1 file stays in `GuliERP.Mdm.Tests` re-targeted), the
> Foundation error code addition, and the DI registration.
> This is **a design document** — no code, no entity, no
> database, no migration changes ship with this PR. The
> WorkItem it names opens its own design goal in
> `docs/governance/GOAL_REGISTRY.md` and ships its own
> audit + design + implementation + test + report cycle.
> Authority: `docs/governance/META_GULI_GOVERNANCE_V1.md` +
> `docs/governance/GULIERP_MODULE_INDEPENDENCE_RULE.md` +
> `docs/governance/GOAL_REGISTRY.md` +
> `docs/business/GULIERP_CODE_PIPELINE_DESIGN_V1.md` (FROZEN) +
> `docs/business/GULIERP_CODE_RULE_STANDARD_V1.md` (FROZEN) +
> `docs/business/GULIERP_MASTER_DATA_MODEL_V1.md` (FROZEN) +
> `docs/business/GULIERP_MDM_001_CODE_PIPELINE_DESIGN_V1.md` (FROZEN) +
> `docs/business/GULIERP_MDM_001_CODE_PIPELINE_IMPLEMENTATION_REPORT.md` (COMPLETE) +
> `docs/business/GULIERP_EMPLOYEE_MASTER_MODEL_V1.md` (FROZEN) +
> `docs/business/GULIERP_EMPLOYEE_MASTER_IMPLEMENTATION_PLAN_001.md` (FROZEN) +
> `docs/business/GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md` (FROZEN — this PR).

Date: 2026-08-24
Status: **FOUNDATION_CODE_PIPELINE_MIGRATION_PLAN_001_FROZEN**

---

## 1. Scope

This plan covers **how to promote the 3 code validators +
`CodeValidationResult` from MDM to Foundation** without
breaking the 153 existing MDM tests or any downstream
consumer. It does **not** ship code. The WorkItem it names
opens its own design goal in
`docs/governance/GOAL_REGISTRY.md` and ships its own
audit + design + implementation + test + report cycle.

### 1.1 In scope

- The atomic move of 4 source files from
  `modules/mdm/GuliERP.Mdm.Application/Validation/` to
  `modules/foundation/GuliERP.Foundation/Validation/`.
- The signature change of the 3 validators from
  `Validate(string?)` to `Validate(string?, ICodeValidationContext)`.
- The new `MasterDataCodeValidator` static facade.
- The new 3 interfaces (`ICodeValidator` /
  `ICodeRuleProvider` / `ICodeValidationContext`) + the
  concrete `CodeValidationContext` record + the
  `V1FrozenRuleProvider` implementation.
- The Foundation `ErrorCodes` extension with 3 new generic
  codes.
- The `ForMdm` factory in `CodeValidationContext` (the only
  module-specific factory shipped in this milestone; the
  `ForIdentity` factory is shipped by the Employee write
  surface milestone).
- The DI registration of `ICodeRuleProvider` as a singleton
  in `GuliERP.Foundation.DependencyInjection`.
- The call-site update in
  `modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmService.cs`
  + `MdmMasterData002Services.cs` (3 CreateAsync methods).
- The test project split: 3 test files move to a new
  `tests/GuliERP.Foundation.Tests/` project; 1 test file
  stays in `tests/GuliERP.Mdm.Tests/` re-targeted to the
  new 3-arg helper signature.
- The new test files for the new types
  (`MasterDataCodeValidator` / `ICodeRuleProvider` /
  architecture tests).

### 1.2 Out of scope

- The Sales / Purchase / Inventory error code extension
  (V1.5+ when those modules ship).
- The `ForIdentity` / `ForSales` / `ForPurchase` /
  `ForInventory` factories (the Employee write surface
  milestone ships `ForIdentity`; the others are V1.5+).
- The V1.5+ DI-registered `ICodeValidator` instances.
- The V1.5+ rule provider customization per Tenant.
- The auto-coding engine (V2+ per the V1 model).
- The internal reference number on documents (V2+).
- The V1.5+ Employee write surface — that is the
  `GULIERP_HR_001_EMPLOYEE_WRITE_V1` milestone, which
  DEPENDS on this Foundation promote.
- The 2 pre-existing inherited flaky tests in
  `GuliERP.Mdm.Tests.MdmCurrentTenantParallelTests` — they
  remain flaky; this design does not fix them.
- SalesOrder / PurchaseOrder / Inventory / Production /
  Quality entity / migration / API changes (the brief
  forbids module changes in this milestone).

### 1.3 Sequencing principles

The plan is built on six sequencing principles, derived
from `GULIERP_BUSINESS_ROADMAP_001` §2 and the V1 frozen
contract:

1. **Module Independence first.** The
   `GULIERP_MODULE_INDEPENDENCE_RULE` is the primary
   reason for the promote. The plan must not break the
   rule: the new types are leaf-level in the dependency
   graph (Foundation knows no module).
2. **Atomic migration.** The 3 source files move in one
   commit. The 3 call sites in MDM update in the same
   commit. The 3 test files move in the same commit. The
   1 re-targeted test file updates in the same commit.
   No intermediate state has both the old `GuliERP.Mdm.*`
   and the new `GuliERP.Foundation.*` versions live.
3. **No V1 contract relaxation.** The 4-step pipeline
   rules (format regex / reserved set / doc-number
   pattern) are unchanged. The promotion moves the code,
   not the rules.
4. **No new public surface in V1.5+ shape.** The
   `ICodeValidator` interface is defined but the V1
   static classes do NOT implement it (V1.5+ will wrap
   them as adapters). This avoids premature DI abstraction.
5. **One design goal per WorkItem.** This migration is a
   single WorkItem
   (`GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE`).
6. **Test-first verification.** The new
   `GuliERP.Foundation.Tests` project is created FIRST
   in the implementation milestone; the moved tests are
   updated in the same step; the build + test pass BEFORE
   the MDM call-site update.

---

## 2. WorkItem matrix

| #      | WorkItem                                                  | Priority | Effort | Depends on | Opens design goal                              | Closes gap from MODEL §X |
|--------|-----------------------------------------------------------|----------|--------|------------|-------------------------------------------------|---------------------------|
| F-1    | Foundation project extension (new Validation namespace) | **P0**   | 0.5 d  | none       | `GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE`  | Model §3                 |
| F-2    | Move 4 source files (atomic)                            | **P0**   | 0.5 d  | F-1        | (same)                                          | Model §3, §5             |
| F-3    | Signature change + new facade + new interfaces           | **P0**   | 0.5 d  | F-2        | (same)                                          | Model §4                 |
| F-4    | Foundation ErrorCodes extension (3 new constants)        | **P0**   | 0.5 d  | F-1        | (same)                                          | Model §4.8               |
| F-5    | New `GuliERP.Foundation.Tests` project + 3 moved tests   | **P0**   | 0.5 d  | F-2        | (same)                                          | Model §7                 |
| F-6    | New test files for facade + rule provider               | **P0**   | 0.5 d  | F-3        | (same)                                          | Model §7                 |
| F-7    | MDM call-site update (3 sites in 2 files)               | **P0**   | 0.5 d  | F-3, F-6   | (same)                                          | Model §5.3               |
| F-8    | MDM wiring test re-target (1 file, 50 tests)            | **P0**   | 0.5 d  | F-7        | (same)                                          | Model §7.3               |
| F-9    | Architecture tests (Foundation depends on no module)    | **P0**   | 0.5 d  | F-5        | (same)                                          | Model §7.2               |
| F-10   | Foundation DI registration (`ICodeRuleProvider`)        | **P0**   | 0.25 d | F-4        | (same)                                          | Model §4.9               |
| F-11   | Full build + test + report                              | **P0**   | 0.5 d  | F-1..F-10  | (same)                                          | (all of the above)       |
| **Total** |                                                       |          | **5.25 d** |            |                                                 |                           |

> **Sequencing note:** F-1 to F-6 are pre-call-site work
> (set up the new types, tests, and Foundation error codes
> FIRST). F-7 is the call-site update; F-7 must land in
> the same commit as F-2..F-6 (atomicity). F-8 to F-11
> follow F-7.
>
> The 11 sub-WorkItems ship in **one design goal**
> (`GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE`) with a
> single design + implementation + test + report cycle.

---

## 3. Migration phases (the atomic commit shape)

The migration is a single commit. The commit message is
`feat(foundation): GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE — promote 4-step code pipeline from MDM to Foundation`. The
commit shape (the order in which files are written + tested
+ committed) is:

### 3.1 Phase 1 — Foundation project extension (F-1)

**Files added:**

- `modules/foundation/GuliERP.Foundation/Validation/CodeValidationResult.cs`
  (verbatim copy from
  `modules/mdm/GuliERP.Mdm.Application/Validation/CodeValidationResult.cs`,
  with the `using GuliERP.Mdm.Application;` removed and
  the namespace changed to `GuliERP.Foundation.Validation`).
- `modules/foundation/GuliERP.Foundation/Validation/FormatValidator.cs`
  (verbatim copy + namespace change; **signature
  unchanged** at this phase: still `Validate(string?)`).
- `modules/foundation/GuliERP.Foundation/Validation/ReservedNameValidator.cs`
  (same).
- `modules/foundation/GuliERP.Foundation/Validation/DocumentNumberSimilarityValidator.cs`
  (same).

**Note:** at this phase, the new files still reference
`MdmErrorCodes.CodeFormatInvalid` etc. (copied verbatim).
The Foundation project gains a project reference to the
MDM project (or the `MdmErrorCodes` consts are inlined as
literal strings — see §3.1.1).

**§3.1.1 — Foundation depends on MDM, or inlines the
constants?**

Two options:

1. **Foundation project gains a project reference to
   `GuliERP.Mdm.Application`.** The 4 moved files
   reference `MdmErrorCodes.CodeFormatInvalid` etc. via
   the project reference. **PRO:** zero changes to the
   moved code body. **CON:** Foundation now depends on
   MDM, which is the OPPOSITE of the goal. This option
   is FORBIDDEN by the design.

2. **Foundation inlines the 3 error code constants as
   private string literals (or reads them from a
   Foundation-level constant).** The 4 moved files
   reference the inline constants. **PRO:** Foundation
   depends on nothing. **CON:** a one-line body change
   in each of the 3 validator files. The 3 `MdmErrorCodes.*`
   consts are removed from `MdmErrorCodes` (or kept
   there as deprecated re-exports — see §3.1.2).

**Decision (frozen):** option 2 with the
`MdmErrorCodes.*` consts removed in Phase 5 (F-7 call-site
update). Phase 1 inlines the constants as literal strings
so Foundation compiles without depending on MDM.

The inline constants are:

```csharp
// In FormatValidator:
MdmErrorCodes.CodeFormatInvalid  // → "mdm_code_format_invalid" (literal)
MdmErrorCodes.CodeReserved  // → "mdm_code_reserved" (literal)
MdmErrorCodes.CodeResemblesDocumentNumber  // → "mdm_code_resembles_document_number" (literal)
```

Wait — the brief forbids code changes (the design IS the
change). The Phase 1 inlining happens IN the implementation
milestone, not in this design. The design just says: at
the moment of the move, the error code references must be
inlined literals so Foundation has no MDM dependency. The
implementation milestone handles the inlining.

**Acceptance criteria (F-1):**

1. `modules/foundation/GuliERP.Foundation/Validation/` is
   created with 4 files.
2. The 4 files have the namespace `GuliERP.Foundation.Validation`.
3. The 4 files compile without the `GuliERP.Mdm.Application`
   reference (the 3 error codes are inline literals).
4. The existing `GuliERP.Mdm.Application.Validation`
   namespace still exists (the files are duplicated for
   now; the cutover happens in Phase 5).
5. `dotnet build modules/foundation/GuliERP.Foundation.csproj -c Release` → 0 errors, 0 warnings.

### 3.2 Phase 2 — Test project split (F-5, F-6)

**Files added:**

- `tests/GuliERP.Foundation.Tests/GuliERP.Foundation.Tests.csproj`
  (new xUnit project; project references
  `GuliERP.Foundation` + xUnit packages).
- `tests/GuliERP.Foundation.Tests/FormatValidatorTests.cs`
  (verbatim copy from `tests/GuliERP.Mdm.Tests/`, with
  the 2 `MdmErrorCodes` references updated to literal
  string constants — or, more cleanly, the tests assert
  on the inline literal `"mdm_code_format_invalid"`,
  which is the value the new Foundation `FormatValidator`
  writes).
- `tests/GuliERP.Foundation.Tests/ReservedNameValidatorTests.cs`
  (same).
- `tests/GuliERP.Foundation.Tests/DocumentNumberSimilarityValidatorTests.cs`
  (same).
- `tests/GuliERP.Foundation.Tests/MasterDataCodeValidatorTests.cs`
  (NEW; ~6 tests verifying the facade runs the 4 steps
  in order, returns the first failure, and the context
  parameter is honored).
- `tests/GuliERP.Foundation.Tests/ICodeRuleProviderTests.cs`
  (NEW; ~3 tests verifying the V1FrozenRuleProvider
  returns the V1 frozen values).
- `tests/GuliERP.Foundation.Tests/FoundationArchitectureTests.cs`
  (NEW; ~3 tests using reflection to assert the
  Foundation project does NOT reference any
  `GuliERP.Mdm.*` / `GuliERP.Identity.*` /
  `GuliERP.Sales.*` etc. type).

**Files modified (NOT moved at this phase):**

- The 3 test files in `tests/GuliERP.Mdm.Tests/` remain
  unchanged at this phase. They will be deleted in Phase
  5 (F-7) when the call-site cutover lands.

**Acceptance criteria (F-2):**

1. `tests/GuliERP.Foundation.Tests/` builds clean
   (0 errors, 0 warnings).
2. The 3 moved validator tests pass against the new
   Foundation types (the Foundation `FormatValidator`
   returns the same `"mdm_code_format_invalid"` literal
   in the `CodeValidationFailure.ErrorCode`).
3. The new `MasterDataCodeValidatorTests` pass.
4. The new `ICodeRuleProviderTests` pass.
5. The new `FoundationArchitectureTests` pass (the
   Foundation namespace depends on no module).
6. `dotnet test tests/GuliERP.Foundation.Tests` → 0 fail.

### 3.3 Phase 3 — Signature change + new facade + new
interfaces (F-2, F-3)

**Files added:**

- `modules/foundation/GuliERP.Foundation/Validation/ICodeValidationContext.cs`
  (the interface, per Model §4.1).
- `modules/foundation/GuliERP.Foundation/Validation/CodeValidationContext.cs`
  (the concrete record + `Default` static + `ForMdm`
  factory, per Model §4.2).
- `modules/foundation/GuliERP.Foundation/Validation/ICodeValidator.cs`
  (the strategy interface, per Model §4.3).
- `modules/foundation/GuliERP.Foundation/Validation/ICodeRuleProvider.cs`
  (the rule provider interface + `V1FrozenRuleProvider`
  implementation, per Model §4.4).
- `modules/foundation/GuliERP.Foundation/Validation/MasterDataCodeValidator.cs`
  (the static facade, per Model §4.7).

**Files modified:**

- `modules/foundation/GuliERP.Foundation/Validation/FormatValidator.cs`
  — `Validate(string?)` replaced with
  `Validate(string?, ICodeValidationContext)` (mandatory
  parameter, no default). The body uses
  `context.FormatInvalidErrorCode` instead of
  `MdmErrorCodes.CodeFormatInvalid` (which is now an
  inline literal in the Foundation's view).
- Same for `ReservedNameValidator` + `DocumentNumberSimilarityValidator`.

**§3.3.1 — The `ForMdm` factory in `CodeValidationContext`**

The `ForMdm` factory references `MdmErrorCodes.Code*` (the
MDM module's error codes). This creates a **cyclic import
problem**: Foundation has a type (`CodeValidationContext`)
that references an MDM type (`MdmErrorCodes`).

**Solution:** the `ForMdm` factory lives in a separate file
in the MDM module, NOT in the Foundation. Specifically:

- `modules/foundation/GuliERP.Foundation/Validation/CodeValidationContext.cs`
  — the record + the `Default` static. NO `ForMdm` factory.
- `modules/mdm/GuliERP.Mdm.Application/Validation/CodeValidationContextExtensions.cs`
  — the `ForMdm` extension method (in the MDM module's
  Application assembly, NOT the Foundation assembly). The
  extension method imports `GuliERP.Mdm.Application` (for
  `MdmErrorCodes`) and
  `GuliERP.Foundation.Validation` (for `CodeValidationContext`).

This is the cleanest split: the Foundation owns the type +
the default; the MDM owns the module-specific factory as an
extension method. The Identity module ships its own
`CodeValidationContextExtensions.cs` with a `ForIdentity`
factory (in the Employee write surface milestone).

**Acceptance criteria (F-3):**

1. `ICodeValidationContext` / `CodeValidationContext` /
   `ICodeValidator` / `ICodeRuleProvider` /
   `MasterDataCodeValidator` compile.
2. The 3 `FormatValidator` / `ReservedNameValidator` /
   `DocumentNumberSimilarityValidator` are updated to
   `Validate(string?, ICodeValidationContext)`. The
   `MdmService.ThrowIfCodeInvalid` call sites are NOT
   YET updated — the build at the MDM level fails at
   this phase. **This is OK** because Phase 4 (F-7) is
   the cutover. The build at the Foundation level must
   pass; the build at the MDM level is broken until
   Phase 4.

3. The new `MasterDataCodeValidatorTests` + `ICodeRuleProviderTests`
   pass.
4. `dotnet build modules/foundation/GuliERP.Foundation.csproj -c Release` → 0 errors, 0 warnings.

### 3.4 Phase 4 — Foundation ErrorCodes + DI (F-4, F-10)

**Files modified:**

- `modules/foundation/GuliERP.Foundation/Kernel/ErrorCodes.cs`
  — append 3 new constants:
  `CodeFormatInvalid = "code_format_invalid"`,
  `CodeReserved = "code_reserved"`,
  `CodeResemblesDocumentNumber = "code_resembles_document_number"`.

- `modules/foundation/GuliERP.Foundation/DependencyInjection.cs`
  — register `ICodeRuleProvider` as a singleton:
  `services.AddSingleton<ICodeRuleProvider>(V1FrozenRuleProvider.Instance);`.

**Acceptance criteria (F-4, F-10):**

1. `ErrorCodes.cs` has the 3 new constants.
2. `DependencyInjection.cs` registers the rule provider.
3. `dotnet build modules/foundation/GuliERP.Foundation.csproj -c Release` → 0 errors, 0 warnings.

### 3.5 Phase 5 — MDM call-site cutover (F-7, F-8)

This is the **atomic cutover**. The 3 call sites in MDM
are updated + the old `GuliERP.Mdm.Application.Validation`
namespace is deleted + the old test files in
`GuliERP.Mdm.Tests` are deleted + the
`MdmServiceCodeValidationTests.cs` is re-targeted.

**Files modified:**

- `modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmService.cs`
  — `ThrowIfCodeInvalid(string code)` becomes
  `ThrowIfCodeInvalid(string code, long tenantId, long? companyId)`.
  The body uses the new `MasterDataCodeValidator.Validate(code, CodeValidationContextExtensions.ForMdm(entityScope, tenantId, companyId))`.
  The 3 CreateAsync call sites
  (`CreateUomAsync` / `CreateItemCategoryAsync` /
  `CreateItemAsync`) pass `tenantId` + `companyId` to the
  helper.
- `modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmMasterData002Services.cs`
  — same for `MdmBusinessPartnerService.ThrowIfCodeInvalid`
  + `MdmWarehouseService.CreateAsync` (the latter calls
  into the former via
  `MdmBusinessPartnerService.ThrowIfCodeInvalid(code, tenantId, companyId)`).

**Files added:**

- `modules/mdm/GuliERP.Mdm.Application/Validation/CodeValidationContextExtensions.cs`
  — the `ForMdm` factory as an extension method on
  `CodeValidationContext` (per §3.3.1).

**Files deleted:**

- `modules/mdm/GuliERP.Mdm.Application/Validation/CodeValidationResult.cs`
  (now in Foundation).
- `modules/mdm/GuliERP.Mdm.Application/Validation/FormatValidator.cs`
  (now in Foundation).
- `modules/mdm/GuliERP.Mdm.Application/Validation/ReservedNameValidator.cs`
  (now in Foundation).
- `modules/mdm/GuliERP.Mdm.Application/Validation/DocumentNumberSimilarityValidator.cs`
  (now in Foundation).
- `tests/GuliERP.Mdm.Tests/FormatValidatorTests.cs` (now in
  Foundation).
- `tests/GuliERP.Mdm.Tests/ReservedNameValidatorTests.cs` (now
  in Foundation).
- `tests/GuliERP.Mdm.Tests/DocumentNumberSimilarityValidatorTests.cs`
  (now in Foundation).

**Files re-targeted:**

- `tests/GuliERP.Mdm.Tests/MdmServiceCodeValidationTests.cs`
  — the 50 tests are updated to call the new
  3-arg `MdmService.ThrowIfCodeInvalid(code, tenantId, companyId)`.
  The assertions on `MdmErrorCodes.CodeFormatInvalid` etc.
  are unchanged (the `ForMdm` factory wires the MDM codes).
  The 50 tests still pass.

**§3.5.1 — The `MdmErrorCodes.Code*` consts**

The 3 consts (`CodeFormatInvalid` / `CodeReserved` /
`CodeResemblesDocumentNumber`) stay in `MdmErrorCodes`.
They are no longer referenced by the 3 validators
(which now live in Foundation and use `context.*`); they
are still referenced by the `ForMdm` factory
(`modules/mdm/GuliERP.Mdm.Application/Validation/CodeValidationContextExtensions.cs`).
The MDM wire contract (the error code that the API
endpoint returns in the ProblemDetails) is unchanged.

**Acceptance criteria (F-7, F-8):**

1. The 3 `MdmService.CreateAsync` methods
   (Uom / ItemCategory / Item) call
   `ThrowIfCodeInvalid(code, tenantId, companyId)`.
2. The 3 `MdmBusinessPartnerService.CreateAsync` /
   `MdmWarehouseService.CreateAsync` methods do the same.
3. The 4 deleted files do not exist in the worktree.
4. The 3 deleted test files do not exist in the worktree.
5. The 1 re-targeted test file
   (`MdmServiceCodeValidationTests.cs`) passes.
6. `dotnet build modules/mdm/GuliERP.Mdm.Infrastructure.csproj -c Release` → 0 errors, 0 warnings.
7. `dotnet test tests/GuliERP.Mdm.Tests` → 153 pass
   (the 50 re-targeted + 103 untouched, EXCLUDING the
   2 inherited flaky `MdmCurrentTenantParallelTests`).

### 3.6 Phase 6 — Full build + test + report (F-11)

**Files added:**

- `docs/verification/GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE_REPORT.md`
  (the implementation report, mirroring the format of
  `GULIERP_MDM_001_CODE_PIPELINE_IMPLEMENTATION_REPORT.md`).

**Acceptance criteria (F-11):**

1. `dotnet build` (all projects) → 0 errors, 0 warnings.
2. `dotnet test tests/GuliERP.Foundation.Tests` → 0 fail
   (the new ~112 tests pass).
3. `dotnet test tests/GuliERP.Mdm.Tests` → 153/153 pass
   (50 re-targeted + 103 untouched; 2 inherited flaky
   `MdmCurrentTenantParallelTests` fail as before).
4. `dotnet test tests/GuliERP.Api.Tests` → 32/32 pass
   (unchanged).
5. `dotnet test tests/GuliERP.Sales.Tests` → 9/9 pass
   (unchanged).
6. `dotnet test` (all projects) → 0 new failures.
7. The implementation report is written.
8. A commit is created
   (`feat(foundation): GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE — promote 4-step code pipeline from MDM to Foundation`).

---

## 4. Per-file change list (the atomic commit)

The single commit touches the following files (additions +
modifications + deletions):

### 4.1 Files added (NEW)

**Foundation namespace (5 new files):**

- `modules/foundation/GuliERP.Foundation/Validation/CodeValidationResult.cs`
- `modules/foundation/GuliERP.Foundation/Validation/ICodeValidationContext.cs`
- `modules/foundation/GuliERP.Foundation/Validation/CodeValidationContext.cs`
- `modules/foundation/GuliERP.Foundation/Validation/ICodeValidator.cs`
- `modules/foundation/GuliERP.Foundation/Validation/ICodeRuleProvider.cs`
- `modules/foundation/GuliERP.Foundation/Validation/FormatValidator.cs`
  (moved from MDM; signature changed)
- `modules/foundation/GuliERP.Foundation/Validation/ReservedNameValidator.cs`
  (moved from MDM; signature changed)
- `modules/foundation/GuliERP.Foundation/Validation/DocumentNumberSimilarityValidator.cs`
  (moved from MDM; signature changed)
- `modules/foundation/GuliERP.Foundation/Validation/MasterDataCodeValidator.cs`
  (NEW)

**MDM extensions (1 new file):**

- `modules/mdm/GuliERP.Mdm.Application/Validation/CodeValidationContextExtensions.cs`
  (the `ForMdm` factory as an extension method)

**Tests (5 new files):**

- `tests/GuliERP.Foundation.Tests/GuliERP.Foundation.Tests.csproj`
- `tests/GuliERP.Foundation.Tests/FormatValidatorTests.cs`
  (moved from MDM)
- `tests/GuliERP.Foundation.Tests/ReservedNameValidatorTests.cs`
  (moved from MDM)
- `tests/GuliERP.Foundation.Tests/DocumentNumberSimilarityValidatorTests.cs`
  (moved from MDM)
- `tests/GuliERP.Foundation.Tests/MasterDataCodeValidatorTests.cs`
  (NEW)
- `tests/GuliERP.Foundation.Tests/ICodeRuleProviderTests.cs`
  (NEW)
- `tests/GuliERP.Foundation.Tests/FoundationArchitectureTests.cs`
  (NEW)

**Report (1 new file):**

- `docs/verification/GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE_REPORT.md`

### 4.2 Files modified (CHANGED)

**Foundation (2):**

- `modules/foundation/GuliERP.Foundation/Kernel/ErrorCodes.cs`
  (+3 consts)
- `modules/foundation/GuliERP.Foundation/DependencyInjection.cs`
  (+1 line for `ICodeRuleProvider` registration)

**MDM infrastructure (2):**

- `modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmService.cs`
  (`ThrowIfCodeInvalid` 1-arg → 3-arg; 3 call sites
  updated)
- `modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmMasterData002Services.cs`
  (same)

**MDM tests (1):**

- `tests/GuliERP.Mdm.Tests/MdmServiceCodeValidationTests.cs`
  (50 tests re-targeted to the new 3-arg signature)

### 4.3 Files deleted (REMOVED)

**MDM validation (4):**

- `modules/mdm/GuliERP.Mdm.Application/Validation/CodeValidationResult.cs`
- `modules/mdm/GuliERP.Mdm.Application/Validation/FormatValidator.cs`
- `modules/mdm/GuliERP.Mdm.Application/Validation/ReservedNameValidator.cs`
- `modules/mdm/GuliERP.Mdm.Application/Validation/DocumentNumberSimilarityValidator.cs`

**MDM tests (3):**

- `tests/GuliERP.Mdm.Tests/FormatValidatorTests.cs`
- `tests/GuliERP.Mdm.Tests/ReservedNameValidatorTests.cs`
- `tests/GuliERP.Mdm.Tests/DocumentNumberSimilarityValidatorTests.cs`

### 4.4 Net change summary

| Category                          | Additions | Modifications | Deletions | Net new |
|-----------------------------------|-----------|---------------|-----------|---------|
| Foundation source                 | 9         | 2             | 0         | +9      |
| Foundation tests                  | 7         | 0             | 0         | +7      |
| MDM source                        | 1         | 2             | 4         | -1      |
| MDM tests                         | 0         | 1             | 3         | -2      |
| Docs                              | 1         | 0             | 0         | +1      |
| **Total**                         | **18**    | **5**         | **7**     | **+14** |

---

## 5. The test migration plan (F-5, F-6, F-8, F-9)

### 5.1 Per-test-file mapping

| Current file (in MDM)               | New file (in Foundation)         | Pattern |
|-------------------------------------|----------------------------------|---------|
| `FormatValidatorTests.cs` (28)      | `FormatValidatorTests.cs` (28)   | verbatim copy + `using GuliERP.Mdm.Application;` → `using GuliERP.Foundation.Validation;` |
| `ReservedNameValidatorTests.cs` (31)| `ReservedNameValidatorTests.cs` (31) | same |
| `DocumentNumberSimilarityValidatorTests.cs` (44) | `DocumentNumberSimilarityValidatorTests.cs` (44) | same |
| `MdmServiceCodeValidationTests.cs` (50) | (stays in MDM; re-targeted to the new 3-arg `ThrowIfCodeInvalid`) | 50 tests updated to pass `tenantId` + `companyId` |

### 5.2 The new test files (in Foundation)

| New file                          | Test count | What it covers                                |
|-----------------------------------|-----------|------------------------------------------------|
| `MasterDataCodeValidatorTests.cs` | ~6        | Facade runs 4 steps in order; first failure wins; context error codes are written; null context throws ArgumentNullException. |
| `ICodeRuleProviderTests.cs`       | ~3        | `V1FrozenRuleProvider` returns the 11 reserved names; 9 doc-number prefixes; V1 format regex + length range. |
| `FoundationArchitectureTests.cs`  | ~3        | `git grep` style: the Foundation assembly's referenced types are ONLY in `GuliERP.Foundation.*` (no Mdm/Identity/Sales/etc.). The `using` statements of every Foundation source file do not include any module namespace. The Foundation's project references (in the `.csproj`) are ONLY the BCL + EF Core + Npgsql (no module references). |

### 5.3 Test count summary (after migration)

| Test type      | Project                          | Count   |
|----------------|----------------------------------|---------|
| Validator unit | `GuliERP.Foundation.Tests`       | 28 + 31 + 44 = 103 (moved) + 6 (new facade) + 3 (new rule provider) = **112** |
| Architecture   | `GuliERP.Foundation.Tests`       | **3** (new) |
| **Foundation total** |                              | **115** |
| MDM wiring     | `GuliERP.Mdm.Tests`              | **50** (re-targeted) + 103 (other MDM tests) = **153** (unchanged) |
| Other test projects (Api / Sales / Foundation / etc.) | unchanged | unchanged |
| **Grand total** |                                  | **~268** (was 153 + 65 Api + 9 Sales + others) |

### 5.4 Test invariants locked (after migration)

- **The 3 validators return the V1 frozen values for the
  V1 frozen contexts** (per the 28+31+44 tests; unchanged
  from the prior MDM tests).
- **The new `MasterDataCodeValidator` runs the 4 steps in
  order** (the new ~6 tests; locks the facade contract).
- **The new `V1FrozenRuleProvider` returns the V1 frozen
  values** (the new ~3 tests; locks the rule provider
  contract).
- **Foundation depends on no module** (the new ~3
  architecture tests; the `MODULE_INDEPENDENCE_RULE` is
  enforced by test, not just by code review).
- **The MDM wiring still works** (the 50 re-targeted tests;
  unchanged assertions on `MdmErrorCodes.Code*`).

### 5.5 Out-of-scope test surface

- **No new integration tests.** The validators are pure
  functions (no I/O). The wiring tests in MDM verify the
  end-to-end call (the helper → validator → exception
  throw). The Foundation tests verify the validator body.
- **No new Sales / Purchase / Inventory tests.** Those
  modules are V1.5+; their error code extension is a
  separate milestone.
- **No new Employee tests.** The Employee write surface
  milestone (which depends on this Foundation promote)
  ships its own `ForIdentity` factory + its own wiring
  tests.

---

## 6. The migration risk (per Model §8, expanded)

### 6.1 Risk 1: signature break (HIGH severity)

**Risk:** the 3 validators' `Validate(string?)` signature
is replaced by `Validate(string?, ICodeValidationContext)`.
The 3 MDM call sites must be updated. If the
implementation milestone misses a call site, the build
fails (the type system enforces it: `Validate(string?)`
no longer exists).

**Mitigation:**

- The `git grep` architecture test in
  `GuliERP.Foundation.Tests` asserts the Foundation
  assembly does NOT reference any `GuliERP.Mdm.*` type.
  The reverse (`GuliERP.Mdm.*` references no
  `GuliERP.Foundation.Validation.Validate(string?)`) is
  also asserted by the existing
  `GuliERP.Mdm.Tests.MdmServiceBoundaryArchitectureTests`
  pattern (extended to include the new namespace).
- The atomic commit shape (§3) means the build either
  passes or fails as a single unit. No intermediate state
  is committed.
- The implementation milestone adds a final `dotnet build`
  check BEFORE the commit lands.

### 6.2 Risk 2: test count regression (MEDIUM severity)

**Risk:** the 3 test files move to a new project. If the
implementation milestone forgets to add the new project to
`tests/.dotnet/test-solution.sln` (or whatever the
solution file is), the moved tests are silently dropped
from the CI run.

**Mitigation:**

- The implementation milestone verifies the new project's
  test count (112 expected) + the MDM project's test
  count (153 expected) + the total (~268) before
  committing.
- The implementation report (§3.6 F-11) records the test
  counts in a 6-block format, matching the prior
  implementation reports.

### 6.3 Risk 3: the 2 pre-existing inherited flaky tests
(NEGLIGIBLE severity)

**Risk:** the 2 `MdmCurrentTenantParallelTests.MdmSeed_ResolveSeedFilePath_*`
flaky tests in `GuliERP.Mdm.Tests` remain flaky. This
design does NOT fix them.

**Mitigation:**

- The flaky tests are NOT in the moved-files set (they're
  in `MdmCurrentTenantParallelTests.cs`, which is NOT
  moved). The migration does not affect them.
- The flaky tests are documented in
  `GULIERP_MDM_001_CODE_PIPELINE_IMPLEMENTATION_REPORT` §6
  + `GULIERP_SHELL_FINAL_POLISH_002A_REPORT.md` + the
  UserMenu polish 003 report. They are a known inherited
  baseline.

### 6.4 Risk 4: the `MdmErrorCodes.Code*` consts orphaned
(LOW severity)

**Risk:** the 3 `MdmErrorCodes.Code*` consts are no longer
referenced by the 3 validators (which now use `context.*`).
If the implementation milestone deletes the consts by
accident, the `ForMdm` factory in
`CodeValidationContextExtensions.cs` fails to compile.

**Mitigation:**

- The implementation milestone EXPLICITLY keeps the
  `MdmErrorCodes.Code*` consts (per §3.5.1).
- The `MdmErrorCodes.Code*` consts are now the "MDM-side
  canonical names" for the error codes; the `ForMdm`
  factory references them.
- A grep at the end of the implementation milestone
  (`git grep MdmErrorCodes.CodeFormatInvalid`) should
  find 1 reference (the `ForMdm` factory) + 1 reference
  in the test (`MdmServiceCodeValidationTests.cs`). If
  0 references, the const was accidentally deleted.

### 6.5 Risk 5: the `ForMdm` factory in MDM imports
Foundation (NEGLIGIBLE severity)

**Risk:** the `CodeValidationContextExtensions.cs` in MDM
imports `GuliERP.Foundation.Validation.CodeValidationContext`.
This is a one-way import: MDM depends on Foundation
(downward). It does NOT violate the
`GULIERP_MODULE_INDEPENDENCE_RULE` (the rule is
"modules do not import each other" — Foundation is not a
module, it's the leaf layer).

**Mitigation:**

- The architecture test in `GuliERP.Mdm.Tests` (the
  existing `MdmServiceBoundaryArchitectureTests` pattern)
  is extended to assert MDM does NOT import from
  `GuliERP.Identity.*` / `GuliERP.Sales.*` etc. (the
  rule's intent). The MDM → Foundation import is allowed
  (downward).

---

## 7. The 3 new interfaces — design rationale

### 7.1 Why 3 separate interfaces (not 1 monolithic one)

The brief lists 3 interfaces as examples: `ICodeValidator` /
`ICodeRuleProvider` / `ICodeValidationContext`. The 3-way
separation is intentional:

- **`ICodeValidationContext`** is a **data** interface
  (per-call state). It is constructed by the caller
  (e.g., `MdmService.ThrowIfCodeInvalid` constructs a
  `CodeValidationContext.ForMdm(...)`). It is consumed by
  the 3 validators. It has NO behavior.
- **`ICodeValidator`** is a **strategy** interface for
  the 3 static validators (Format / Reserved /
  Doc-Number). V1 uses static classes; V1.5+ may add
  DI-registered instances.
- **`ICodeRuleProvider`** is a **rule-set** interface for
  the V1 frozen rules (length / regex / reserved set /
  prefixes). V1 uses a singleton; V1.5+ may add
  per-Tenant rule extension.

The 3-way split follows the SOLID principles
(single-responsibility / open-closed / Liskov
substitution / interface-segregation / dependency-
inversion). It also matches the 3 V1 validator types
(format / reserved / doc-number) — each is a separate
"strategy" with its own rule set.

### 7.2 Why `ICodeValidator` is defined but not used in V1

The V1 static classes (`FormatValidator` /
`ReservedNameValidator` /
`DocumentNumberSimilarityValidator`) do NOT implement
`ICodeValidator`. The V1.5+ refactor will wrap them as
adapters:

```csharp
// V1.5+ future code (NOT in this milestone):
public sealed class FormatValidatorAdapter : ICodeValidator
{
    public int Step => 1;
    public string ValidatorName => "Format";
    public CodeValidationResult Validate(string? code, ICodeValidationContext context)
        => FormatValidator.Validate(code, context);
}
```

The V1.5+ adapters are DI-registered as
`IEnumerable<ICodeValidator>`. The V1.5+ facade resolves
the collection and runs each in `Step` order. The V1
static-class callers continue to work (the static facade
`MasterDataCodeValidator.Validate(string?, ICodeValidationContext)`
is unchanged).

The interface is defined in V1 so the V1.5+ refactor is
purely additive (no new types in V1.5+; just DI
registrations + adapter classes).

### 7.3 Why `ICodeRuleProvider` is defined but not used in V1

Same rationale. V1 uses hard-coded rules in the 3 static
validators. V1.5+ may inject `ICodeRuleProvider` to allow
per-Tenant rule extension. The interface is defined in V1
so the V1.5+ refactor is purely additive.

The V1 implementation (`V1FrozenRuleProvider`) is
DI-registered as a singleton in V1. The V1.5+ refactor may
add a `TenantScopedRuleProvider` (V1.5+ per-Tenant
extension) — a separate design goal.

### 7.4 Why `ICodeValidationContext` IS used in V1

Unlike the 2 strategy interfaces, `ICodeValidationContext`
is consumed by the 3 V1 validators (the `context.*` reads
in the validator bodies). The validators' signature change
from `Validate(string?)` to
`Validate(string?, ICodeValidationContext)` is the
core change of this migration.

---

## 8. Future WorkItems (V1.5+, recorded, NOT in this milestone)

The following are explicitly V1.5+ design goals (each opens
its own design goal in `GOAL_REGISTRY.md`):

1. **`GULIERP_FOUNDATION_002_V15_DI_REFACTOR`**
   - Wrap the 3 V1 static validators as `ICodeValidator`
     adapters.
   - DI-register the 3 adapters.
   - Add a `MasterDataCodeValidator.ResolveFromDI(IServiceProvider)`
     overload that resolves the `IEnumerable<ICodeValidator>`.
   - V1 callers continue to use the static facade; V1.5+
     callers may use the DI facade.

2. **`GULIERP_FOUNDATION_003_V15_RULE_PROVIDER_PER_TENANT`**
   - Add a `TenantScopedRuleProvider` that layers
     Tenant-specific rules on top of the V1 frozen set.
   - DI-register the new provider as a scoped service.
   - Add the per-Tenant extension tables to the database
     (a separate migration).

3. **`GULIERP_HR_001_EMPLOYEE_WRITE_V1`** (the Employee
   write surface; DEPENDS on this Foundation promote).
   - Adds the `ForIdentity` factory in
     `modules/identity/GuliERP.Identity.Application/Validation/CodeValidationContextExtensions.cs`.
   - Adds the 12 new `IdentityErrorCodes.EmployeeCode*`
     consts.
   - Adds the `EmployeeWriteService.ThrowIfEmployeeCodeInvalid`
     helper that calls the new `MasterDataCodeValidator`.

4. **`GULIERP_SALES_001_V15` / `GULIERP_PURCHASE_001_V15` /
   `GULIERP_INVENTORY_001_V15` / etc.** (V1.5+ per the
   roadmap). Each ships its own `ForXxx` factory + its
   own `XxxErrorCodes.XxxCode*` consts.

---

## 9. Acceptance criteria (whole milestone)

1. **Build:** `dotnet build` (all projects) → 0 errors,
   0 warnings.
2. **Foundation tests:** `dotnet test tests/GuliERP.Foundation.Tests` → 115 pass.
3. **MDM tests:** `dotnet test tests/GuliERP.Mdm.Tests` → 153/153 pass (50 re-targeted + 103 untouched; 2 inherited flaky `MdmCurrentTenantParallelTests` fail as before).
4. **Api tests:** `dotnet test tests/GuliERP.Api.Tests` → 32/32 pass (unchanged).
5. **Sales tests:** `dotnet test tests/GuliERP.Sales.Tests` → 9/9 pass (unchanged).
6. **Architecture:** the new `FoundationArchitectureTests` → 3/3 pass. The existing `MdmServiceBoundaryArchitectureTests` continue to pass (extended if needed to assert the new namespace).
7. **Zero Mdm → Foundation circular references:** `git grep "using GuliERP.Foundation.Validation" modules/mdm/` finds 1 reference (the `CodeValidationContextExtensions.cs` import + the 2 `ThrowIfCodeInvalid` calls). `git grep "using GuliERP.Mdm.Application" modules/foundation/` finds 0 references.
8. **No source code change outside scope:** no changes to `apps/api/**`, `apps/web/**`, the Sales / Purchase / Inventory / Production / Quality modules, the Identity module, the Document-kernel module, the database, the migrations, the entities.
9. **The 3 V1 frozen values are unchanged:** the
   `V1FrozenRuleProvider` returns the same length 2..40,
   the same regex `^[A-Z][A-Z0-9_]{1,39}$`, the same 11
   reserved names, the same 9 doc-number prefixes.
10. **The 3 `MdmErrorCodes.Code*` consts are preserved:**
    the `ForMdm` factory references them; the wire
    contract is unchanged.
11. **A commit lands:** one commit, the message
    `feat(foundation): GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE — promote 4-step code pipeline from MDM to Foundation`.
12. **The implementation report is written:** the
    `GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE_REPORT.md`
    is in `docs/verification/`, with the 6-block format
    matching the prior reports.

---

## 10. Out of scope (whole milestone, repeated for clarity)

- The Sales / Purchase / Inventory error code extension
  (V1.5+ when those modules ship).
- The `ForIdentity` / `ForSales` / `ForPurchase` /
  `ForInventory` factories (each is a separate milestone).
- The V1.5+ DI-registered `ICodeValidator` instances.
- The V1.5+ rule provider customization per Tenant.
- The auto-coding engine (V2+ per the V1 model).
- The internal reference number on documents (V2+).
- The 2 pre-existing inherited flaky tests in
  `GuliERP.Mdm.Tests.MdmCurrentTenantParallelTests` —
  they remain flaky; this milestone does not fix them.
- SalesOrder / PurchaseOrder / Inventory / Production /
  Quality entity / migration / API changes (the brief
  forbids module changes in this milestone).
- The Identity module's Employee write surface
  implementation (a separate milestone that DEPENDS on
  this Foundation promote).

---

## 11. Gate

`GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE_DESIGN_COMPLETE`

Fires when:

1. `GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md` is FROZEN.
2. `GULIERP_FOUNDATION_CODE_PIPELINE_MIGRATION_PLAN_001.md` is FROZEN.
3. The 9 analysis points in the brief (current implementation
   structure / Foundation responsibilities / dependency
   direction / new interfaces / MDM call / future call /
   test migration / namespace / risk) are all addressed
   in this 3-document package.

The implementation milestone
(`GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE`) opens
after this gate fires.
