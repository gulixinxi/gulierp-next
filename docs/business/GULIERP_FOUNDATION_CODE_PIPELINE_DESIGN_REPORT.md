# GULIERP_FOUNDATION_CODE_PIPELINE_DESIGN_REPORT

> Goal: deliver the V1 Code Pipeline promote design as a
> **3-document package** (Model V1 + Migration Plan 001 +
> this Design Report). This Report is the **cover sheet**
> that explicitly answers the 9 analysis points in the
> brief and points the reader to the deep-dive documents.
> It does **not** ship code, entity, database, migration,
> or module changes. The work is a design freeze only.
> Authority: `docs/architecture/MDM_000_MASTER_DATA_CONVENTION_V1.md` +
> `docs/governance/GULIERP_MODULE_INDEPENDENCE_RULE.md` +
> `docs/governance/META_GULI_GOVERNANCE_V1.md` +
> `docs/business/GULIERP_CODE_PIPELINE_DESIGN_V1.md` (FROZEN) +
> `docs/business/GULIERP_CODE_RULE_STANDARD_V1.md` (FROZEN) +
> `docs/business/GULIERP_MASTER_DATA_MODEL_V1.md` (FROZEN) +
> `docs/business/GULIERP_MDM_001_CODE_PIPELINE_DESIGN_V1.md` (FROZEN) +
> `docs/business/GULIERP_MDM_001_CODE_PIPELINE_IMPLEMENTATION_REPORT.md` (COMPLETE) +
> `docs/business/GULIERP_EMPLOYEE_MASTER_MODEL_V1.md` (FROZEN) +
> `docs/business/GULIERP_EMPLOYEE_MASTER_IMPLEMENTATION_PLAN_001.md` (FROZEN) +
> `docs/business/GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md` (FROZEN — companion doc) +
> `docs/business/GULIERP_FOUNDATION_CODE_PIPELINE_MIGRATION_PLAN_001.md` (FROZEN — companion doc).

Date: 2026-08-24
Status: **FOUNDATION_CODE_PIPELINE_DESIGN_REPORT_FROZEN**

---

## 1. Executive summary

### 1.1 TL;DR

The V1 4-step code pipeline (3 validators + result type)
moves from the MDM module to the Foundation module. The
move enables the Identity / Sales / Purchase / Inventory
modules to reuse the same pipeline without depending on
the MDM module, preserving the
`GULIERP_MODULE_INDEPENDENCE_RULE`. The 3 static
validators get a new `ICodeValidationContext` parameter
that carries the module-specific error codes, so the
Identity module can throw
`IdentityErrorCodes.EmployeeCodeFormatInvalid` instead of
`MdmErrorCodes.CodeFormatInvalid`. A new
`MasterDataCodeValidator` static facade is the recommended
entry point. 3 new interfaces (`ICodeValidator` /
`ICodeRuleProvider` / `ICodeValidationContext`) are defined
for the V1.5+ DI refactor; in V1 they are used as
data (the context) + future-extensibility hooks (the 2
strategy interfaces are not used directly in V1).

### 1.2 The 3-document package

| # | Document                                                       | Size  | Purpose                                                  |
|---|----------------------------------------------------------------|-------|----------------------------------------------------------|
| 1 | `GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md`                 | 43.1 KB | The V1 contract freeze (11 sections; deep dive)          |
| 2 | `GULIERP_FOUNDATION_CODE_PIPELINE_MIGRATION_PLAN_001.md`        | 42.7 KB | The migration plan (11 sections; 11 sub-WorkItems; atomic commit shape) |
| 3 | `GULIERP_FOUNDATION_CODE_PIPELINE_DESIGN_REPORT.md`            | this   | The cover sheet (9-point brief answers + cross-references) |

### 1.3 Gate

`GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE_DESIGN_COMPLETE`

Fires when all 9 analysis points in §3–§11 are answered
AND docs 1 + 2 are FROZEN. Both companion docs are already
in `docs/business/` (uncommitted, per the "no commit
unless explicitly requested" preference; operator reviews
and commits as a single design-goal commit).

---

## 2. Scope of this design

### 2.1 In scope (per the brief)

- Current Code Pipeline implementation structure.
- Foundation module current responsibilities.
- Dependency direction (Foundation = leaf, modules depend
  on Foundation, modules do not depend on each other).
- New interface design (`ICodeValidator` /
  `ICodeRuleProvider` / `ICodeValidationContext`).
- How MDM calls the new pipeline.
- How Sales / Purchase / Inventory will call the new
  pipeline (V1.5+).
- Test migration plan (3 files move, 1 file re-targets,
  5 new test files).
- Namespace planning (`GuliERP.Foundation.Validation`).
- Migration risk analysis.

### 2.2 Out of scope (per the brief)

- **No C# / Entity / Database / Migration changes.** This
  is a design freeze only.
- **No module code changes** (Sales / Purchase / Inventory
  / Production / Quality / Identity / Document-kernel).
  The Foundation promote is the only code-area change
  scoped for the implementation milestone.
- **No V1.5+ DI-registered validator instances.** The
  `ICodeValidator` interface is defined but the V1
  static classes do NOT implement it (V1.5+ will wrap
  them as adapters).
- **No V1.5+ rule provider customization per Tenant.**
- **No auto-coding engine** (V2+ per the V1 model).
- **No internal reference number on documents** (V2+).
- **No Employee write surface implementation.** That is
  the `GULIERP_HR_001_EMPLOYEE_WRITE_V1` milestone, which
  DEPENDS on this Foundation promote.
- **No front-end test framework.**

---

## 3. Analysis Point 1 — Current Code Pipeline structure

### 3.1 Where it lives today

The 4-step code pipeline (just shipped as
`GULIERP_MDM_001_CODE_PIPELINE_IMPLEMENTATION_COMPLETE`)
lives in 3 layers of the MDM module:

| Layer            | File                                                                                    | Role |
|------------------|-----------------------------------------------------------------------------------------|------|
| Application      | `modules/mdm/GuliERP.Mdm.Application/Validation/CodeValidationResult.cs`              | `CodeValidationResult` + `CodeValidationFailure` records |
| Application      | `modules/mdm/GuliERP.Mdm.Application/Validation/FormatValidator.cs`                    | Step 1 (format) |
| Application      | `modules/mdm/GuliERP.Mdm.Application/Validation/ReservedNameValidator.cs`              | Step 2 (reserved) |
| Application      | `modules/mdm/GuliERP.Mdm.Application/Validation/DocumentNumberSimilarityValidator.cs`  | Step 4 (no-doc-number) |
| Application      | `modules/mdm/GuliERP.Mdm.Application/MdmErrorCodes.cs` (+3 consts)                     | `CodeFormatInvalid` / `CodeReserved` / `CodeResemblesDocumentNumber` |
| Infrastructure   | `modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmService.cs` (`ThrowIfCodeInvalid` helper) | orchestrates the 4 steps + throws `MdmValidationException` |
| Infrastructure   | `modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmMasterData002Services.cs` (`MdmBusinessPartnerService.ThrowIfCodeInvalid` + 3 CreateAsync call sites) | same |

### 3.2 What's wrong with the current location

The 3 validators hard-code `MdmErrorCodes.Code*` as the
error code. When the Identity module calls
`FormatValidator.Validate(...)` for an Employee, it would
get back `MdmErrorCodes.CodeFormatInvalid` (= "mdm_code_format_invalid")
in the `CodeValidationFailure.ErrorCode`. That is wrong:
the Employee write service should throw
`IdentityErrorCodes.EmployeeCodeFormatInvalid` (= "identity_employee_code_format_invalid").

The hard-coded error code is the core reason for the
Foundation promote: the validators must accept the error
codes as a parameter (via `ICodeValidationContext`), not
hard-code them.

### 3.3 The full deep-dive

See [`GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md` §2](GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md).

---

## 4. Analysis Point 2 — Foundation module current
responsibilities

### 4.1 What Foundation owns today

The `GuliERP.Foundation` module is small and focused on
the **kernel layer**:

| File                                          | Role                                                |
|-----------------------------------------------|------------------------------------------------------|
| `Kernel/ErrorCodes.cs`                        | GuliERP-wide error codes (lower_snake format)        |
| `Kernel/IMultiTenant` / `ICompanyScoped` / `IOrganizationScoped` / `IPlantScoped` | Entity scope marker interfaces |
| `Kernel/ICurrentTenant` / `ICurrentCompany` / `ICurrentUser` | Per-request scope accessors |
| `Kernel/IDataFilter`                          | Generic filter disable contract                      |
| `Kernel/IRequestContextAccessor` / `RequestContext` / `RequestContextAccessor` / `RequestIdValidator` | AsyncLocal-backed request context |
| `FoundationBoundary.cs` + `ModelBoundaries.cs`| G0 boundary catalogue (which entities Foundation owns / reserves) |
| `DependencyInjection.cs` (`AddGuliErpFoundation`) | Single composition entry-point                |
| `FoundationDbContext.cs` + Migrations/      | The Foundation's own `FoundationDbContext` for the `foundation` schema |

**Total Foundation source:** 10 .cs files (excluding
Migrations / Designer / Snapshot). The Foundation is a
**leaf layer** in the dependency graph.

### 4.2 What Foundation does NOT own today

- **No business logic.** Foundation has no UoM / Item /
  Customer / Supplier / Warehouse / Employee logic.
- **No entity beyond the G0 boundary catalogue.** The
  `FoundationDbContext` is a placeholder; the actual
  Foundation tables (Audit / Dictionary) are deferred.
- **No Validation namespace.** The current Foundation has
  no `Validation/` subdirectory.
- **No `ICodeValidator` / `ICodeRuleProvider` /
  `ICodeValidationContext`.** These are new in this
  design.

### 4.3 The "leaf layer" rule (frozen)

The Foundation is the lowest layer. It knows nothing about
any module. Every other module may import from Foundation
(downward). Modules do NOT import from each other
(per `GULIERP_MODULE_INDEPENDENCE_RULE`).

The new `GuliERP.Foundation.Validation` namespace follows
this rule: it knows nothing about MDM / Identity / Sales /
Purchase / Inventory / Document-kernel.

### 4.4 The full deep-dive

See [`GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md` §3](GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md).

---

## 5. Analysis Point 3 — Dependency direction

### 5.1 The frozen dependency graph (after the migration)

```
                              ┌──────────────────────┐
                              │  GuliERP.Foundation  │
                              │  (Validation/        │
                              │   Kernel/ etc.)      │
                              └──────────┬───────────┘
                                         │ (may import)
                ┌────────────────────────┼────────────────────────┐
                │                        │                        │
                ▼                        ▼                        ▼
   ┌────────────────────┐  ┌────────────────────┐  ┌────────────────────┐
   │  GuliERP.Mdm       │  │  GuliERP.Identity   │  │  GuliERP.Sales /   │
   │  (Application/     │  │  (Application/     │  │  Purchase / Inv /  │
   │   Infrastructure)  │  │   Infrastructure)  │  │  Production / Qual │
   └────────────────────┘  └────────────────────┘  └────────────────────┘
                │                        │                        │
                └────────────────────────┼────────────────────────┘
                                         │ (composition root)
                                         ▼
                          ┌──────────────────────────┐
                          │  GuliERP.Api (Host)      │
                          │  (the composition root)  │
                          └──────────────────────────┘
```

- **Foundation has no upward dependency.** It knows nothing
  about MDM, Identity, Sales, etc.
- **All modules have a downward dependency to Foundation.**
  They may import `GuliERP.Foundation.Validation`,
  `GuliERP.Foundation.Kernel`, etc.
- **Modules do NOT import each other.** MDM does not import
  from Identity; Identity does not import from MDM; etc.
  (This is the `GULIERP_MODULE_INDEPENDENCE_RULE`.)
- **The API Host is the composition root.** It imports from
  all modules + Foundation; it is the only place where
  cross-module wiring is allowed.

### 5.2 The cross-module anti-pattern this design prevents

**Without the Foundation promote**, the V1.5+ Employee
write service would have to do this:

```csharp
// In EmployeeWriteService (Identity module, V1.5+):
using GuliERP.Mdm.Application.Validation;  // ❌ Identity → MDM dependency

internal static void ThrowIfEmployeeCodeInvalid(string code)
{
    var r = FormatValidator.Validate(code);  // ❌ returns MdmErrorCodes.CodeFormatInvalid
    if (!r.IsValid)
    {
        throw new IdentityValidationException(
            r.Failure!.ErrorCode,  // ❌ wrong code (mdm_code_format_invalid)
            r.Failure.Message);
    }
}
```

**After the Foundation promote**, the V1.5+ Employee write
service does this:

```csharp
// In EmployeeWriteService (Identity module, V1.5+):
using GuliERP.Foundation.Validation;  // ✓ Identity → Foundation (downward, allowed)

internal static void ThrowIfEmployeeCodeInvalid(string code, long tenantId, long companyId)
{
    var context = new CodeValidationContext(
        EntityScope: "IdentityEmployee",
        TenantId: tenantId,
        CompanyId: companyId,
        FormatInvalidErrorCode: IdentityErrorCodes.EmployeeCodeFormatInvalid,  // ✓
        ReservedErrorCode: IdentityErrorCodes.EmployeeCodeReserved,
        ResemblesDocumentNumberErrorCode: IdentityErrorCodes.EmployeeCodeResemblesDocumentNumber);

    var r = MasterDataCodeValidator.Validate(code, context);  // ✓
    if (!r.IsValid)
    {
        throw new IdentityValidationException(
            r.Failure!.ErrorCode,  // ✓ "identity_employee_code_format_invalid"
            r.Failure.Message);
    }
}
```

The Identity module gets the right error code without
importing from MDM.

### 5.3 The full deep-dive

See [`GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md` §5](GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md).

---

## 6. Analysis Point 4 — New interface design

The brief lists 3 interfaces as examples:
`ICodeValidator` / `ICodeRuleProvider` /
`ICodeValidationContext`. The design ships all 3.

### 6.1 `ICodeValidationContext` (data interface)

The **only interface used directly in V1** (consumed by
the 3 validators). Carries the per-call state: entity
scope, tenant id, company id, and the 3 module-specific
error codes.

```csharp
public interface ICodeValidationContext
{
    string EntityScope { get; }           // "MdmBusinessPartner" / "IdentityEmployee"
    long TenantId { get; }
    long? CompanyId { get; }
    string FormatInvalidErrorCode { get; }  // "mdm_code_format_invalid" or "identity_employee_code_format_invalid"
    string ReservedErrorCode { get; }        // "mdm_code_reserved" or "identity_employee_code_reserved"
    string ResemblesDocumentNumberErrorCode { get; }
}
```

The concrete `CodeValidationContext` record (per Model
§4.2) lives in
`modules/foundation/GuliERP.Foundation/Validation/CodeValidationContext.cs`.
The `ForMdm` extension method lives in
`modules/mdm/GuliERP.Mdm.Application/Validation/CodeValidationContextExtensions.cs`
(per Migration Plan §3.3.1 — to avoid the cyclic import
problem). The `ForIdentity` extension method ships with
the Employee write surface milestone.

### 6.2 `ICodeValidator` (strategy interface, V1.5+ shape)

The strategy interface for the 3 static validators. **Not
used directly in V1** (the static classes do NOT implement
it). V1.5+ wraps them as adapters and DI-registers the
collection.

```csharp
public interface ICodeValidator
{
    int Step { get; }          // 1 / 2 / 4 (3 is the DB's job)
    string ValidatorName { get; }  // "Format" / "ReservedName" / "DocumentNumberSimilarity"
    CodeValidationResult Validate(string? code, ICodeValidationContext context);
}
```

### 6.3 `ICodeRuleProvider` (rule-set interface, V1.5+ shape)

The interface for the V1 frozen rules (format regex /
reserved set / doc-number prefixes). **V1 ships the
`V1FrozenRuleProvider` implementation + DI-registers it
as a singleton.** V1.5+ may add a `TenantScopedRuleProvider`
for per-Tenant rule extension.

```csharp
public interface ICodeRuleProvider
{
    (int MinLength, int MaxLength, Regex FormatPattern) GetFormatRules(
        ICodeValidationContext context);
    IReadOnlySet<string> GetReservedCodes(ICodeValidationContext context);
    IReadOnlyList<string> GetDocumentNumberPrefixes(
        ICodeValidationContext context);
}
```

### 6.4 Why 3 separate interfaces (not 1)

The 3-way split follows the SOLID principles (per Model
§7.1):

- `ICodeValidationContext` is a **data** interface
  (per-call state, no behavior).
- `ICodeValidator` is a **strategy** interface for the
  V1.5+ DI refactor (V1 uses static classes; V1.5+
  wraps them as adapters).
- `ICodeRuleProvider` is a **rule-set** interface for the
  V1 frozen rules (V1 uses a singleton; V1.5+ may add
  per-Tenant extension).

### 6.5 The full deep-dive

See [`GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md` §4](GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md).

---

## 7. Analysis Point 5 — How MDM calls the new pipeline

### 7.1 The new call shape

After the migration, the MDM service call is:

```csharp
// In MdmService.ThrowIfCodeInvalid (post-migration):
internal static void ThrowIfCodeInvalid(
    string code, long tenantId, long? companyId)
{
    var context = CodeValidationContextExtensions.ForMdm(
        entityScope: "MdmBusinessPartner",  // or "MdmItem" / "MdmUom" / etc.
        tenantId: tenantId,
        companyId: companyId);

    var result = MasterDataCodeValidator.Validate(code, context);
    if (!result.IsValid)
    {
        throw new MdmValidationException(
            result.Failure!.ErrorCode,
            result.Failure.Message);
    }
}
```

The `ForMdm` extension method constructs a
`CodeValidationContext` with the 3 MDM-namespaced error
codes (per Model §4.2 + Migration Plan §3.3.1):

```csharp
// In modules/mdm/GuliERP.Mdm.Application/Validation/CodeValidationContextExtensions.cs:
public static class CodeValidationContextExtensions
{
    public static CodeValidationContext ForMdm(
        this CodeValidationContext _,  // extension method marker
        string entityScope, long tenantId, long? companyId) => new(
        EntityScope: entityScope,
        TenantId: tenantId,
        CompanyId: companyId,
        FormatInvalidErrorCode: MdmErrorCodes.CodeFormatInvalid,
        ReservedErrorCode: MdmErrorCodes.CodeReserved,
        ResemblesDocumentNumberErrorCode: MdmErrorCodes.CodeResemblesDocumentNumber);
}
```

(Actually, the implementation may use a static factory
method instead of an extension method — both shapes are
acceptable. The decision is the implementation milestone's
choice; the model freezes the API contract.)

### 7.2 The 3 call-site updates in MDM

Per Migration Plan §3.5, the 3 call sites in 2 files are
updated in the same atomic commit:

| File                                                | Method                                       | Change |
|-----------------------------------------------------|----------------------------------------------|--------|
| `MdmService.cs`                                     | `CreateUomAsync`                              | pass `tenantId` + `companyId` to `ThrowIfCodeInvalid` |
| `MdmService.cs`                                     | `CreateItemCategoryAsync`                     | same |
| `MdmService.cs`                                     | `CreateItemAsync`                             | same |
| `MdmMasterData002Services.cs` (`MdmBusinessPartnerService`) | `CreateAsync` (BusinessPartner)              | same |
| `MdmMasterData002Services.cs` (`MdmWarehouseService`) | `CreateAsync` (Warehouse)                     | calls `MdmBusinessPartnerService.ThrowIfCodeInvalid(code, tenantId, companyId)` |

### 7.3 The 3 MdmErrorCodes.Code* consts are preserved

The 3 MDM-namespaced error codes stay in `MdmErrorCodes`
(unchanged). The `ForMdm` factory references them. The
wire contract (the error code that the API endpoint
returns in the ProblemDetails) is **unchanged**.

### 7.4 The full deep-dive

See [`GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md` §5.3](GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md)
and [`GULIERP_FOUNDATION_CODE_PIPELINE_MIGRATION_PLAN_001.md` §3.5](GULIERP_FOUNDATION_CODE_PIPELINE_MIGRATION_PLAN_001.md).

---

## 8. Analysis Point 6 — How Sales / Purchase / Inventory
will call (V1.5+)

### 8.1 The future call shape (V1.5+)

When the Sales / Purchase / Inventory modules ship a
code-bearing entity in V1.5+, the shape is identical to
MDM's (per Model §5.4):

```csharp
// In a future Sales module's helper:
internal static void ThrowIfSalesCodeInvalid(
    string code, long tenantId, long? companyId)
{
    var context = new CodeValidationContext(
        EntityScope: "SalesQuotation",  // or "SalesContract" etc.
        TenantId: tenantId,
        CompanyId: companyId,
        FormatInvalidErrorCode: SalesErrorCodes.QuotationCodeFormatInvalid,
        ReservedErrorCode: SalesErrorCodes.QuotationCodeReserved,
        ResemblesDocumentNumberErrorCode: SalesErrorCodes.QuotationCodeResemblesDocumentNumber);

    var result = MasterDataCodeValidator.Validate(code, context);
    if (!result.IsValid)
    {
        throw new SalesValidationException(
            result.Failure!.ErrorCode,
            result.Failure.Message);
    }
}
```

The Foundation namespace knows nothing about
`SalesErrorCodes` / `SalesValidationException`. The Sales
module owns them; the Foundation owns the pipeline.

### 8.2 Per-module factory pattern (mirrors MDM)

Each module ships a `CodeValidationContextExtensions.cs`
in its own Application assembly:

| Module        | Factory name | File |
|---------------|--------------|------|
| **MDM**       | `ForMdm`     | `modules/mdm/GuliERP.Mdm.Application/Validation/CodeValidationContextExtensions.cs` (shipped in this migration) |
| **Identity**  | `ForIdentity`| `modules/identity/GuliERP.Identity.Application/Validation/CodeValidationContextExtensions.cs` (shipped in the Employee write surface milestone) |
| **Sales**     | `ForSales`   | V1.5+ (TBD) |
| **Purchase**  | `ForPurchase`| V1.5+ (TBD) |
| **Inventory** | `ForInventory`| V1.5+ (TBD) |
| **Production**| `ForProduction`| V1.5+ (TBD) |
| **Quality**   | `ForQuality` | V1.5+ (TBD) |

Each factory is a one-method class that returns a
`CodeValidationContext` with the module's error codes
baked in. The Foundation owns the `CodeValidationContext`
type; the modules own the factory.

### 8.3 Why this works without cross-module coupling

The pattern is:

1. The Foundation owns `CodeValidationContext` (the data
   type) + the 3 validators + the `MasterDataCodeValidator`
   facade.
2. Each module owns its `XxxErrorCodes.XxxCode*` consts
   (its error code namespace).
3. Each module ships a `CodeValidationContextExtensions.cs`
   that imports `GuliERP.Foundation.Validation` (downward,
   allowed) + its own `XxxErrorCodes` (own).
4. The module's helper uses the extension to construct the
   context + calls `MasterDataCodeValidator.Validate`.

No module imports another module. The Foundation is the
single source of truth for the validation logic.

### 8.4 The full deep-dive

See [`GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md` §5.4](GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md).

---

## 9. Analysis Point 7 — Test migration plan

### 9.1 The 3-way split (per Migration Plan §5)

| Test file (current, in `GuliERP.Mdm.Tests`)  | Action | New file (in `GuliERP.Foundation.Tests`) | New file (stays in `GuliERP.Mdm.Tests`) |
|----------------------------------------------|--------|------------------------------------------|------------------------------------------|
| `FormatValidatorTests.cs` (28 tests)        | MOVE   | `FormatValidatorTests.cs` (28 tests, namespace updated) | (deleted) |
| `ReservedNameValidatorTests.cs` (31 tests)  | MOVE   | `ReservedNameValidatorTests.cs` (31 tests, namespace updated) | (deleted) |
| `DocumentNumberSimilarityValidatorTests.cs` (44 tests) | MOVE | `DocumentNumberSimilarityValidatorTests.cs` (44 tests, namespace updated) | (deleted) |
| `MdmServiceCodeValidationTests.cs` (50 tests) | RE-TARGET | (none) | `MdmServiceCodeValidationTests.cs` (50 tests, updated to call the new 3-arg `MdmService.ThrowIfCodeInvalid`) |

### 9.2 The 5 new test files (in Foundation)

| New file (in `GuliERP.Foundation.Tests`) | Test count | Purpose |
|------------------------------------------|-----------|---------|
| `MasterDataCodeValidatorTests.cs`        | ~6        | Facade runs the 4 steps in order; first failure wins; context error codes are written; null context throws ArgumentNullException. |
| `ICodeRuleProviderTests.cs`              | ~3        | `V1FrozenRuleProvider` returns the 11 reserved names + 9 doc-number prefixes + V1 format regex + length range. |
| `FoundationArchitectureTests.cs`         | ~3        | `git grep` style: the Foundation assembly's referenced types are ONLY in `GuliERP.Foundation.*` (no Mdm/Identity/Sales/etc.). The `using` statements of every Foundation source file do not include any module namespace. The Foundation's project references (in the `.csproj`) are ONLY the BCL + EF Core + Npgsql (no module references). |
| `FormatValidatorTests.cs` (moved)        | 28        | Verbatim copy of the existing test; namespace updated. |
| `ReservedNameValidatorTests.cs` (moved)  | 31        | Same. |
| `DocumentNumberSimilarityValidatorTests.cs` (moved) | 44 | Same. |
| **Foundation total**                     | **~115**  | (103 moved + 12 new) |

### 9.3 The MDM wiring tests stay (re-targeted)

The 50 tests in `MdmServiceCodeValidationTests.cs` are
updated to call the new 3-arg
`MdmService.ThrowIfCodeInvalid(code, tenantId, companyId)`.
The assertions on `MdmErrorCodes.Code*` are unchanged
because the `ForMdm` factory wires the MDM codes.

The test count does NOT drop to 0. The MDM tests verify
the MDM-specific wiring (the helper → validator →
exception throw with the MDM error code). The Foundation
tests verify the validator body.

### 9.4 The test invariants locked after migration

- **The 3 validators return the V1 frozen values for the
  V1 frozen contexts** (the 28+31+44 tests; unchanged).
- **The new `MasterDataCodeValidator` runs the 4 steps in
  order** (the new ~6 tests; locks the facade contract).
- **The new `V1FrozenRuleProvider` returns the V1 frozen
  values** (the new ~3 tests; locks the rule provider
  contract).
- **Foundation depends on no module** (the new ~3
  architecture tests; the `MODULE_INDEPENDENCE_RULE` is
  enforced by test, not just by code review).
- **The MDM wiring still works** (the 50 re-targeted
  tests; unchanged assertions on `MdmErrorCodes.Code*`).

### 9.5 The full deep-dive

See [`GULIERP_FOUNDATION_CODE_PIPELINE_MIGRATION_PLAN_001.md` §5](GULIERP_FOUNDATION_CODE_PIPELINE_MIGRATION_PLAN_001.md).

---

## 10. Analysis Point 8 — Namespace planning

### 10.1 The target namespace structure (V1, frozen)

```
GuliERP.Foundation
├── GuliERP.Foundation.csproj
├── DependencyInjection.cs
├── FoundationBoundary.cs
├── FoundationDbContext.cs
├── ModelBoundaries.cs
├── Kernel/                              (existing — no change)
│   ├── ErrorCodes.cs                    (EXTEND with 3 new generic codes)
│   ├── IRequestContextAccessor.cs
│   ├── RequestContext.cs
│   ├── RequestContextAccessor.cs
│   ├── RequestIdValidator.cs
│   └── TenantCompanyContextContracts.cs
├── Validation/                          (NEW namespace)
│   ├── CodeValidationResult.cs          (moved from MDM)
│   ├── ICodeValidationContext.cs        (NEW — data interface)
│   ├── CodeValidationContext.cs         (NEW — concrete record + Default)
│   ├── ICodeValidator.cs                (NEW — strategy interface, V1.5+ shape)
│   ├── ICodeRuleProvider.cs             (NEW — rule-set interface, V1.5+ shape)
│   ├── V1FrozenRuleProvider.cs          (NEW — V1 rule implementation)
│   ├── FormatValidator.cs               (moved from MDM; signature changed)
│   ├── ReservedNameValidator.cs         (moved from MDM; signature changed)
│   ├── DocumentNumberSimilarityValidator.cs (moved from MDM; signature changed)
│   └── MasterDataCodeValidator.cs       (NEW — static facade)
└── Migrations/                          (existing — no schema change; the new
                                          types are in C# only, no migration)
```

### 10.2 The deleted MDM namespace (after migration)

```
GuliERP.Mdm.Application/
└── Validation/                          (DELETED after migration)
    ├── CodeValidationResult.cs          (deleted)
    ├── FormatValidator.cs               (deleted)
    ├── ReservedNameValidator.cs         (deleted)
    ├── DocumentNumberSimilarityValidator.cs (deleted)
    └── CodeValidationContextExtensions.cs (NEW — the ForMdm factory)
```

The `CodeValidationContextExtensions.cs` is the ONLY file
that stays in the MDM module's `Validation` namespace.
It is a one-method extension that imports from
`GuliERP.Foundation.Validation` (downward) + uses
`MdmErrorCodes.Code*` (own).

### 10.3 The source-of-truth rule (frozen)

After the promote:

- **One canonical copy** of `FormatValidator` /
  `ReservedNameValidator` /
  `DocumentNumberSimilarityValidator` /
  `CodeValidationResult` in
  `GuliERP.Foundation.Validation`.
- **Zero references** to `GuliERP.Mdm.Application.Validation.*`
  in any non-MDM module's source code.
- **Zero references** to `FormatValidator` /
  `ReservedNameValidator` /
  `DocumentNumberSimilarityValidator` in
  `GuliERP.Mdm.Application.Validation`.
- The only file in the MDM module's `Validation`
  namespace is the new `CodeValidationContextExtensions.cs`
  (the `ForMdm` factory).

### 10.4 The full deep-dive

See [`GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md` §3](GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md).

---

## 11. Analysis Point 9 — Migration risk

### 11.1 Risk matrix

| # | Risk | Severity | Mitigation |
|---|------|----------|------------|
| 1 | Signature break (the 3 validators' `Validate(string?)` → `Validate(string?, ICodeValidationContext)`) | HIGH | Atomic commit shape; build fails if any call site is missed; `dotnet build` check before commit. |
| 2 | Test count regression (3 test files move to a new project; if the new project is not in the solution, the tests are silently dropped) | MEDIUM | Implementation milestone verifies the test counts (112 Foundation + 153 MDM + 32 Api + 9 Sales + others = unchanged + new) before committing. |
| 3 | The 2 pre-existing inherited flaky `MdmCurrentTenantParallelTests` tests | NEGLIGIBLE | Not in the moved-files set; migration does not affect them; documented in prior reports. |
| 4 | The `MdmErrorCodes.Code*` consts orphaned (the 3 validators no longer reference them; the `ForMdm` factory does) | LOW | Implementation milestone EXPLICITLY keeps the consts; a `git grep` at the end of the milestone should find 1 reference (the `ForMdm` factory) + 1 reference in the test. |
| 5 | The `ForMdm` factory in MDM imports Foundation (downward, allowed by the rule) | NEGLIGIBLE | The `MODULE_INDEPENDENCE_RULE` allows modules to import Foundation (downward); the rule forbids modules importing each other. |

### 11.2 The atomic commit shape (the key mitigation)

Per Migration Plan §3, the migration is a single commit.
The commit shape is:

1. **Phase 1:** add the 4 new files in Foundation
   (verbatim copies + namespace change + error codes
   inlined as literals).
2. **Phase 2:** create the new `GuliERP.Foundation.Tests`
   project + move 3 test files + add 3 new test files.
3. **Phase 3:** add the 5 new files in Foundation
   (interfaces + facade) + signature-change the 3
   validators. The build at the Foundation level passes;
   the build at the MDM level fails (call sites not yet
   updated).
4. **Phase 4:** add the 3 new `ErrorCodes` consts + the
   `ICodeRuleProvider` DI registration.
5. **Phase 5 (the cutover):** update the 3 MDM call sites
   + delete the 4 old MDM source files + delete the 3
   old MDM test files + re-target the 1 MDM wiring test +
   add the `ForMdm` extension method. The build at the
   MDM level now passes.
6. **Phase 6:** full build + test + report.

The 5 phases are committed as ONE commit. No intermediate
state is committed.

### 11.3 The test coverage during the migration

At each phase, the test coverage is verified:

| Phase | Foundation tests | MDM tests | Other tests | Notes |
|-------|------------------|-----------|-------------|-------|
| 1     | 0 (new project)   | 153 (untouched) | unchanged | Foundation has new files but no tests yet. |
| 2     | 112 (new)         | 153 (untouched) | unchanged | Foundation tests pass; MDM tests still pass. |
| 3     | 112 (new) + 3 (arch) | 0 (build broken) | unchanged | Foundation tests pass; MDM build broken. |
| 4     | 112 + 3 (arch)    | 0 (build broken) | unchanged | Foundation tests pass; MDM build still broken. |
| 5     | 112 + 3 (arch)    | 153 (re-targeted) | unchanged | All builds pass; all tests pass. |
| 6     | 112 + 3 (arch)    | 153 (re-targeted) | unchanged | All builds pass; all tests pass; report written. |

The atomic commit shape means the intermediate states
(Phase 3 + Phase 4 with the broken MDM build) are never
committed. The committed state is Phase 6 (all builds +
tests pass).

### 11.4 The full deep-dive

See [`GULIERP_FOUNDATION_CODE_PIPELINE_MIGRATION_PLAN_001.md` §6](GULIERP_FOUNDATION_CODE_PIPELINE_MIGRATION_PLAN_001.md).

---

## 12. Honest disclosure (known gaps and out-of-scope)

1. **The V1.5+ DI refactor is deferred.** The
   `ICodeValidator` interface is defined but the V1
   static classes do NOT implement it. The V1.5+ refactor
   (wrapping the statics as adapters) is a separate design
   goal (`GULIERP_FOUNDATION_002_V15_DI_REFACTOR`).
2. **The V1.5+ per-Tenant rule extension is deferred.**
   The `V1FrozenRuleProvider` returns the V1 frozen values
   for every context. The V1.5+ per-Tenant extension is a
   separate design goal
   (`GULIERP_FOUNDATION_003_V15_RULE_PROVIDER_PER_TENANT`).
3. **The Employee write surface is deferred.** The
   `ForIdentity` factory + the 12 new
   `IdentityErrorCodes.EmployeeCode*` consts + the
   `EmployeeWriteService.ThrowIfEmployeeCodeInvalid` helper
   ship in the `GULIERP_HR_001_EMPLOYEE_WRITE_V1`
   milestone, which DEPENDS on this Foundation promote.
4. **The Sales / Purchase / Inventory error code extension
   is deferred.** V1.5+ when those modules ship
   code-bearing entities.
5. **The 2 pre-existing inherited flaky tests in
   `GuliERP.Mdm.Tests.MdmCurrentTenantParallelTests`** are
   NOT in this design's scope; they remain flaky.
6. **No front-end test framework** — this design has no
   UI impact.
7. **The auto-coding engine is V2+** (per the V1 frozen
   model).
8. **The internal reference number on documents is V2+**
   (per the V1 frozen model).
9. **The brief says "MIGRATION 风险" (migration risk)** —
   this is the migration of the C# code (the source
   files, the call sites, the test files). It is NOT a
   database migration; the brief's first prohibition
   ("修改 Database / 修改 Migration") makes that explicit.
   The 3 new `GuliERP.Foundation.Validation` types are
   C# only; the `foundation` schema is unchanged.

---

## 13. Authority chain

This document and its 2 companion documents inherit
authority from:

- `docs/architecture/MDM_000_MASTER_DATA_CONVENTION_V1.md`
  — the MDM master-data convention.
- `docs/governance/GULIERP_MODULE_INDEPENDENCE_RULE.md` —
  the module ownership rule (the primary reason for the
  Foundation promote).
- `docs/governance/META_GULI_GOVERNANCE_V1.md` — the
  cross-cutting governance rule.
- `docs/business/GULIERP_CODE_RULE_STANDARD_V1.md` (FROZEN)
  — the V1 code rule standard (the 4-step rules are
  unchanged; the validators' code moves, the rules
  freeze).
- `docs/business/GULIERP_MASTER_DATA_MODEL_V1.md` (FROZEN)
  — the V1 master-data vocabulary.
- `docs/business/GULIERP_CODE_PIPELINE_DESIGN_V1.md`
  (FROZEN) — the V1 code pipeline design (4 steps; the
  rules frozen in this design are unchanged).
- `docs/business/GULIERP_MDM_001_CODE_PIPELINE_DESIGN_V1.md`
  (FROZEN) — the V1 code pipeline as it exists today in
  MDM; this design promotes it to Foundation.
- `docs/business/GULIERP_MDM_001_CODE_PIPELINE_IMPLEMENTATION_REPORT.md`
  (COMPLETE) — the V1 implementation that this design
  promotes.
- `docs/business/GULIERP_EMPLOYEE_MASTER_MODEL_V1.md`
  (FROZEN) — the Employee master-data design that depends
  on the Foundation promote.
- `docs/business/GULIERP_EMPLOYEE_MASTER_IMPLEMENTATION_PLAN_001.md`
  (FROZEN) — the Employee write surface plan that
  references the Foundation promote as a prerequisite.

This design does NOT modify any of the above. It locks
the V1 Foundation code pipeline contract to the level of
detail required by the migration milestone, and answers
the 9 analysis points in the brief.

---

## 14. One-line summary

The V1 master-data code pipeline (3 validators + result
type + new `MasterDataCodeValidator` facade + 3 strategy
interfaces) moves from `GuliERP.Mdm.Application.Validation`
to `GuliERP.Foundation.Validation`; the 3 static validators
take a new `ICodeValidationContext` parameter that carries
the module-specific error codes, so the Identity / Sales /
Purchase / Inventory modules can reuse the same 4-step
pipeline without depending on the MDM module, preserving
`GULIERP_MODULE_INDEPENDENCE_RULE`; the migration is a
single atomic commit with 18 new files, 5 modified files,
7 deleted files, and ~115 new tests in a new
`GuliERP.Foundation.Tests` project.
