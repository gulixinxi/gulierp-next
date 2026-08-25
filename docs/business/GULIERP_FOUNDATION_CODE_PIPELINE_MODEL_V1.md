# GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1

> Goal: lock the V1 contract for promoting the master-data code
> validation pipeline (3 validators + `CodeValidationResult` +
> a new `MasterDataCodeValidator` facade) from the MDM module
> (`GuliERP.Mdm.Application.Validation`) to the Foundation
> module (`GuliERP.Foundation.Validation`). The promotion
> enables the Identity / Sales / Purchase / Inventory modules
> to reuse the same 4-step code pipeline without violating
> the `GULIERP_MODULE_INDEPENDENCE_RULE`. This is a **design
> freeze** — no code, no entity, no database, no migration
> changes ship with this PR. The output is the contract that
> the next design milestone
> (`GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE`, the
> implementation of the promote) builds on.
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
> this baseline (audit + reference analysis + frozen V1 model).

Date: 2026-08-24
Status: **FOUNDATION_CODE_PIPELINE_MODEL_V1_FROZEN**

---

## 1. Scope

This document defines the V1 contract for the Foundation
Code Pipeline:

- **The 3 validators** (format / reserved-name /
  document-number-similarity) and their move to Foundation.
- **The `CodeValidationResult` + `CodeValidationFailure`
  types** and their move to Foundation.
- **A new `MasterDataCodeValidator` static facade** in
  Foundation that runs the 4-step pipeline in sequence.
- **A new `ICodeValidator` strategy interface** for V1.5+
  DI-based extension (not used in V1, but defined for the
  V1 contract).
- **A new `ICodeRuleProvider` strategy interface** for
  V1.5+ rule-set variation per scope (not used in V1,
  but defined for the V1 contract).
- **A new `ICodeValidationContext` data interface** (and a
  concrete `CodeValidationContext` record) that the
  validators consume.
- **The error code namespace split**: Foundation owns
  generic `code_format_invalid` / `code_reserved` /
  `code_resembles_document_number` constants; each module
  (MDM, Identity, Sales, ...) continues to own its
  module-namespaced codes.
- **The dependency direction**: Foundation knows nothing
  about any module; modules know about Foundation.

This document does **NOT** define:

- **The application service wiring** (the implementation
  milestone handles that).
- **The test project split** (the implementation milestone
  handles that — see `MIGRATION_PLAN_001`).
- **The `IdentityErrorCodes` extension** for Employee
  (`identity_employee_code_format_invalid` etc.) — that
  is in the
  `GULIERP_HR_001_EMPLOYEE_WRITE_V1` implementation
  milestone.
- **The Sales / Purchase / Inventory error code
  extension** — V1.5+ when those modules ship.
- **The auto-coding engine** (V2+ per the V1 model).
- **The internal reference number on documents** (V2+).
- **Any DI-registered `ICodeValidator` /
  `ICodeRuleProvider` instance** (V1.5+; V1 uses the
  static facade only).

---

## 2. Why a Foundation promotion (problem statement)

### 2.1 The current state (as-shipped)

The `GULIERP_MDM_001_CODE_PIPELINE_IMPLEMENTATION_COMPLETE`
task shipped the 4-step code pipeline in the MDM module:

- `modules/mdm/GuliERP.Mdm.Application/Validation/CodeValidationResult.cs`
- `modules/mdm/GuliERP.Mdm.Application/Validation/FormatValidator.cs`
- `modules/mdm/GuliERP.Mdm.Application/Validation/ReservedNameValidator.cs`
- `modules/mdm/GuiERP.Mdm.Application/Validation/DocumentNumberSimilarityValidator.cs`

Plus the integration points in the MDM services:

- `modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmService.cs`
  — `internal static void ThrowIfCodeInvalid(string code)`
- `modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmMasterData002Services.cs`
  — `MdmBusinessPartnerService.internal static void ThrowIfCodeInvalid(string code)`
  + `MdmWarehouseService.CreateAsync` calls into it

Plus 3 error codes in `MdmErrorCodes`:

- `CodeFormatInvalid = "mdm_code_format_invalid"`
- `CodeReserved = "mdm_code_reserved"`
- `CodeResemblesDocumentNumber = "mdm_code_resembles_document_number"`

Plus 4 test files in `tests/GuliERP.Mdm.Tests/`:

- `FormatValidatorTests.cs` (28 tests)
- `ReservedNameValidatorTests.cs` (31 tests)
- `DocumentNumberSimilarityValidatorTests.cs` (44 tests)
- `MdmServiceCodeValidationTests.cs` (50 tests)

The 3 validators hard-code `MdmErrorCodes.CodeFormatInvalid`
/ `.CodeReserved` / `.CodeResemblesDocumentNumber` as the
error code in their `CodeValidationResult.Fail(...)` calls.
This is fine as long as only MDM uses the pipeline.

### 2.2 The future consumer set (per V1 + V1.5+ roadmaps)

The pipeline will be consumed by:

| Module     | Entity that uses a code       | V1 / V1.5+ | Document / design                       |
|------------|--------------------------------|-------------|------------------------------------------|
| **MDM**    | Item / ItemCategory / Uom / BusinessPartner / Warehouse / Location | V1 (shipped) | `GULIERP_MDM_001_CODE_PIPELINE_*` |
| **Identity**| Employee (`EmployeeCode`)     | V1 (designed) | `GULIERP_EMPLOYEE_MASTER_MODEL_V1.md` §4 |
| **Sales**  | SalesOrder document number (line refs Item, BP, Uom) | V1 (live) | `BUSINESS_DOCUMENT_NUMBERING_V1`; the SalesOrder itself does not need a master-data code, but the Sales **module** may add new code-bearing entities in V1.5+ (e.g., SalesQuotation, SalesContract) |
| **Purchase** | (V1.5+) new code-bearing entities (e.g., PurchaseRequisition, PurchaseContract) | V1.5+ | `GULIERP_PURCHASE_001_V15_DESIGN` (TBD) |
| **Inventory** | (V1.5+) new code-bearing entities (e.g., InventoryAdjustment reason codes) | V1.5+ | `GULIERP_INVENTORY_001_V15_DESIGN` (TBD) |
| **Document-kernel** | The document-number regex (`DocumentTypeProfile.Prefix`) is already separate; the V1 code pipeline does NOT cover document numbers (those are system-allocated, not operator-typed). | n/a | The 2 systems (operator-typed master data codes + system-allocated document numbers) are FROZEN as separate (per `GULIERP_CODE_RULE_STANDARD_V1.md` §1). |

The Identity / Sales / Purchase / Inventory modules CANNOT
import from the MDM module (per
`GULIERP_MODULE_INDEPENDENCE_RULE`). So the validators must
move to a module they all CAN import from — the Foundation
module.

### 2.3 The hard-coded error code problem

The current 3 validators hard-code the `MdmErrorCodes.*`
codes. When the Identity module calls
`FormatValidator.Validate(...)` for an Employee, it would
get back `MdmErrorCodes.CodeFormatInvalid` (= "mdm_code_format_invalid")
in the `CodeValidationFailure.ErrorCode`. That is wrong:
the Employee write service should throw
`IdentityErrorCodes.EmployeeCodeFormatInvalid` (= "identity_employee_code_format_invalid").

**Solution:** the validators must accept the error codes as
a parameter (via `ICodeValidationContext`), not hard-code
them. The Foundation namespace owns a generic set of
fallback codes (`code_format_invalid` etc.) for callers
that do not specify.

---

## 3. The new namespace layout

### 3.1 Target namespace structure (V1, frozen)

```
GuliERP.Foundation
├── GuliERP.Foundation.csproj                  (existing — no change)
├── DependencyInjection.cs                    (existing — AddGuliErpFoundation extension)
├── FoundationBoundary.cs                     (existing — no change)
├── FoundationDbContext.cs                    (existing — no change)
├── ModelBoundaries.cs                        (existing — no change)
├── Kernel/
│   ├── ErrorCodes.cs                         (existing — GuliERP-wide error codes; EXTEND with 3 new generic codes)
│   ├── IRequestContextAccessor.cs            (existing)
│   ├── RequestContext.cs                     (existing)
│   ├── RequestContextAccessor.cs             (existing)
│   ├── RequestIdValidator.cs                 (existing)
│   └── TenantCompanyContextContracts.cs      (existing — IMultiTenant etc.)
├── Validation/                               (NEW namespace)
│   ├── CodeValidationResult.cs               (moved from MDM)
│   ├── CodeValidationContext.cs              (NEW — concrete record implementing ICodeValidationContext)
│   ├── ICodeValidationContext.cs             (NEW — data interface)
│   ├── ICodeValidator.cs                     (NEW — strategy interface for V1.5+)
│   ├── ICodeRuleProvider.cs                  (NEW — strategy interface for V1.5+)
│   ├── FormatValidator.cs                    (moved from MDM; signature changed)
│   ├── ReservedNameValidator.cs              (moved from MDM; signature changed)
│   ├── DocumentNumberSimilarityValidator.cs  (moved from MDM; signature changed)
│   └── MasterDataCodeValidator.cs            (NEW — static facade; runs 4 steps)
├── Migrations/                               (existing — no change for the validators; the migration
│                                                     history table is unchanged. The new types are
│                                                     in C# only, no schema change.)
└── (no other new files)
```

### 3.2 The source-of-truth rule (frozen)

After the promote, the **3 validators + `CodeValidationResult` +
the new `MasterDataCodeValidator` live ONLY in
`GuliERP.Foundation.Validation`**. The MDM module's
`GuliERP.Mdm.Application.Validation` namespace is **deleted**
(after the migration cuts over; see `MIGRATION_PLAN_001`).

A `git grep` after the migration lands should find:

- Zero references to `GuliERP.Mdm.Application.Validation.*` in
  any non-MDM module's source code.
- Zero references to `FormatValidator` / `ReservedNameValidator` /
  `DocumentNumberSimilarityValidator` in the
  `GuliERP.Mdm.Application.Validation` namespace.
- One canonical copy of each in
  `GuliERP.Foundation.Validation`.

### 3.3 Namespace ownership rules (frozen)

| Namespace                          | Owner   | May import from         | May NOT import from         |
|------------------------------------|---------|-------------------------|------------------------------|
| `GuliERP.Foundation.Validation`    | Foundation | `GuliERP.Foundation` (Kernel etc.) | Any `GuliERP.Mdm.*` / `GuliERP.Identity.*` / `GuliERP.Sales.*` / `GuliERP.Purchase.*` / `GuliERP.Inventory.*` / `GuliERP.DocumentKernel.*` / any business module |
| `GuliERP.Mdm.Application.Validation` | (deleted after migration) | n/a | n/a |
| `GuliERP.Mdm.Infrastructure.Mdm.ThrowIfCodeInvalid` (the static helper) | MDM    | `GuliERP.Foundation.Validation` + `GuliERP.Mdm.Application` | (none additional) |
| `GuliERP.Identity.Infrastructure.Employee.ThrowIfEmployeeCodeInvalid` (the static helper, future) | Identity | `GuliERP.Foundation.Validation` + `GuliERP.Identity.Application` | `GuliERP.Mdm.*` |

The Foundation Validation namespace is **leaf-level** in
the dependency graph. It knows nothing about any module. The
modules know about Foundation.

---

## 4. The interface contract (V1, frozen)

### 4.1 `ICodeValidationContext` (data interface)

```csharp
namespace GuliERP.Foundation.Validation;

/// <summary>
/// Per-call context for the code validation pipeline. Carries the
/// entity scope, the (Tenant, Company) scope, and the module-
/// specific error codes the validators should write into the
/// returned <see cref="CodeValidationResult.Failure"/>.
/// </summary>
public interface ICodeValidationContext
{
    /// <summary>
    /// A short label for the calling entity, used in log entries and
    /// debug messages (e.g. "MdmBusinessPartner", "IdentityEmployee").
    /// Free-form string; the Foundation does not interpret it.
    /// </summary>
    string EntityScope { get; }

    /// <summary>
    /// The Tenant the code belongs to. Used for per-Tenant reserved
    /// set extension in V1.5+ (V1 uses a global reserved set; the
    /// TenantId is recorded for audit / log only).
    /// </summary>
    long TenantId { get; }

    /// <summary>
    /// The Company the code belongs to (nullable for system-scoped
    /// codes like UoM). Used for per-Company rule extension in
    /// V1.5+. V1 records the value for audit / log only.
    /// </summary>
    long? CompanyId { get; }

    /// <summary>
    /// The error code string to write when the format check fails
    /// (Step 1). The module owns the namespace; Foundation owns
    /// the generic fallback ("code_format_invalid") for callers
    /// that do not specify a module-specific code.
    /// </summary>
    string FormatInvalidErrorCode { get; }

    /// <summary>
    /// The error code string to write when the reserved-name check
    /// fails (Step 2). Default: "code_reserved".
    /// </summary>
    string ReservedErrorCode { get; }

    /// <summary>
    /// The error code string to write when the
    /// document-number-similarity check fails (Step 4). Default:
    /// "code_resembles_document_number".
    /// </summary>
    string ResemblesDocumentNumberErrorCode { get; }
}
```

### 4.2 `CodeValidationContext` (concrete record)

```csharp
namespace GuliERP.Foundation.Validation;

/// <summary>
/// The default concrete implementation of <see cref="ICodeValidationContext"/>.
/// Modules call its constructor directly (or use the
/// <see cref="ForMdm"/> / <see cref="ForIdentity"/> / etc. factories
/// to get a pre-configured record).
/// </summary>
public sealed record CodeValidationContext(
    string EntityScope,
    long TenantId,
    long? CompanyId,
    string FormatInvalidErrorCode,
    string ReservedErrorCode,
    string ResemblesDocumentNumberErrorCode) : ICodeValidationContext
{
    /// <summary>
    /// The Foundation default: generic error codes. Use this
    /// when a module does not have a module-specific error
    /// code namespace (V1+ bootstrap; not the V1.5+ target).
    /// </summary>
    public static CodeValidationContext Default { get; } = new(
        EntityScope: "Unknown",
        TenantId: 0,
        CompanyId: null,
        FormatInvalidErrorCode: "code_format_invalid",
        ReservedErrorCode: "code_reserved",
        ResemblesDocumentNumberErrorCode: "code_resembles_document_number");

    /// <summary>
    /// The MDM context factory. The 3 error codes match
    /// <c>MdmErrorCodes.CodeFormatInvalid / CodeReserved /
    /// CodeResemblesDocumentNumber</c> (the existing
    /// module-specific names).
    /// </summary>
    public static CodeValidationContext ForMdm(
        string entityScope, long tenantId, long? companyId) => new(
        EntityScope: entityScope,
        TenantId: tenantId,
        CompanyId: companyId,
        FormatInvalidErrorCode: MdmErrorCodes.CodeFormatInvalid,
        ReservedErrorCode: MdmErrorCodes.CodeReserved,
        ResemblesDocumentNumberErrorCode: MdmErrorCodes.CodeResemblesDocumentNumber);

    // The ForIdentity / ForSales / ForPurchase / ForInventory
    // factories are added by their respective implementation
    // milestones (Employee write surface ships ForIdentity; the
    // others are V1.5+).
}
```

**Note:** the `MdmErrorCodes.*` constants still exist in
`MdmErrorCodes` (the MDM module owns the module-specific
error codes). The `ForMdm` factory references them. The
Foundation `CodeValidationContext` does NOT own the MDM
codes — it borrows them via the factory.

### 4.3 `ICodeValidator` (strategy interface, V1.5+ shape)

```csharp
namespace GuliERP.Foundation.Validation;

/// <summary>
/// Strategy interface for a single code-pipeline step. V1 uses
/// the 3 static validator classes (FormatValidator /
/// ReservedNameValidator / DocumentNumberSimilarityValidator);
/// V1.5+ may add a DI-registered instance list (e.g., a
/// "Length 2..40 + regex" validator + a "no whitespace" validator
/// as 2 separate steps). The interface is defined in V1 so the
/// V1 static classes can be trivially wrapped in V1.5+.
/// </summary>
public interface ICodeValidator
{
    /// <summary>
    /// The pipeline step this validator represents. The V1 fixed
    /// values are 1 (format), 2 (reserved), 4 (doc-number).
    /// Step 3 (uniqueness) is the DB's job and is NOT an
    /// <see cref="ICodeValidator"/>.
    /// </summary>
    int Step { get; }

    /// <summary>
    /// A short identifier for the validator (e.g. "Format",
    /// "ReservedName", "DocumentNumberSimilarity"). Used in
    /// log entries.
    /// </summary>
    string ValidatorName { get; }

    /// <summary>
    /// Run the validator. The returned <see cref="CodeValidationResult"/>
    /// is Ok() on pass; on fail, the <see cref="CodeValidationResult.Failure"/>'s
    /// <c>ErrorCode</c> is the context's corresponding error code
    /// (e.g. <c>context.FormatInvalidErrorCode</c> for Step 1).
    /// </summary>
    CodeValidationResult Validate(string? code, ICodeValidationContext context);
}
```

**V1 usage:** none directly. The 3 static validator classes
do NOT implement `ICodeValidator` in V1 (they are
`public static class`). V1.5+ may wrap them as adapters.

**V1.5+ usage (future):** a DI-registered
`IEnumerable<ICodeValidator>` is resolved by the
`MasterDataCodeValidator` facade. Each validator is run in
`Step` order. The facade is backward-compatible with the V1
static-class callers because it exposes the same
`Validate(string?, ICodeValidationContext)` method.

### 4.4 `ICodeRuleProvider` (rule-set interface, V1.5+ shape)

```csharp
namespace GuliERP.Foundation.Validation;

/// <summary>
/// Provides the rules for a code-pipeline step in a given
/// context. V1 uses a hard-coded rule set in each static
/// validator; V1.5+ may swap the rule set per scope (e.g., a
/// Tenant that wants to add a Tenant-specific reserved name,
/// or a Tenant that wants to allow an extra character in
/// codes).
/// </summary>
public interface ICodeRuleProvider
{
    /// <summary>
    /// The format rules for the given context: (min length, max
    /// length, regex pattern). V1 returns the V1 frozen values
    /// (2, 40, <c>^[A-Z][A-Z0-9_]{1,39}$</c>) for every context.
    /// </summary>
    (int MinLength, int MaxLength, Regex FormatPattern) GetFormatRules(
        ICodeValidationContext context);

    /// <summary>
    /// The reserved-name set for the given context. V1 returns
    /// the 11-name V1 frozen set for every context. V1.5+ may
    /// allow Tenant-scoped extension (the canonical V1 reserved
    /// set is always included; the Tenant may add more).
    /// </summary>
    IReadOnlySet<string> GetReservedCodes(ICodeValidationContext context);

    /// <summary>
    /// The document-type prefixes that trigger a "looks like a
    /// document number" rejection. V1 returns the 9-prefix
    /// V1 frozen set (<c>SO / PO / GR / GI / TR / SI / PI / MO / QI</c>)
    /// for every context. V1.5+ may allow extension.
    /// </summary>
    IReadOnlyList<string> GetDocumentNumberPrefixes(ICodeValidationContext context);
}

/// <summary>
/// The V1 default rule provider. Returns the V1 frozen values
/// for every context. Registered as a singleton in the
/// Foundation DI container.
/// </summary>
public sealed class V1FrozenRuleProvider : ICodeRuleProvider
{
    public static readonly V1FrozenRuleProvider Instance = new();

    private static readonly Regex V1FormatPattern =
        new(@"^[A-Z][A-Z0-9_]{1,39}$", RegexOptions.Compiled);

    private static readonly HashSet<string> V1ReservedCodes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "SYSTEM", "SYS", "RESERVED",
            "EMP-SYSTEM", "WH-DEFAULT",
            "LOC-RECEIVING", "LOC-SHIPPING",
            "ROLE_PLATFORM_ADMIN", "ROLE_TENANT_ADMIN",
            "ROLE_COMPANY_ADMIN", "ROLE_NORMAL_USER"
        };

    private static readonly IReadOnlyList<string> V1DocNumberPrefixes =
        new[] { "SO", "PO", "GR", "GI", "TR", "SI", "PI", "MO", "QI" };

    public (int MinLength, int MaxLength, Regex FormatPattern) GetFormatRules(
        ICodeValidationContext context) =>
        (2, 40, V1FormatPattern);

    public IReadOnlySet<string> GetReservedCodes(
        ICodeValidationContext context) => V1ReservedCodes;

    public IReadOnlyList<string> GetDocumentNumberPrefixes(
        ICodeValidationContext context) => V1DocNumberPrefixes;
}
```

**V1 usage:** none directly. The 3 static validator
classes hard-code the V1 rules.

**V1.5+ usage (future):** a DI-registered `ICodeRuleProvider`
is consumed by the V1.5+ validator implementations. The
V1.5+ `MasterDataCodeValidator` may resolve the rule
provider from DI and pass it to the validators.

### 4.5 `CodeValidationResult` + `CodeValidationFailure` (moved from MDM)

```csharp
namespace GuliERP.Foundation.Validation;

/// <summary>
/// Result of a single code-validation check. Ok when
/// <see cref="IsValid"/> is true; otherwise <see cref="Failure"/>
/// carries the error code (from the
/// <see cref="ICodeValidationContext"/>) and a human-readable
/// message.
/// </summary>
public sealed record CodeValidationResult
{
    public bool IsValid { get; }
    public CodeValidationFailure? Failure { get; }

    private CodeValidationResult(
        bool isValid, CodeValidationFailure? failure = null)
    {
        IsValid = isValid;
        Failure = failure;
    }

    public static CodeValidationResult Ok() => new(true);

    public static CodeValidationResult Fail(string errorCode, string message)
        => new(false, new CodeValidationFailure(errorCode, message));
}

/// <summary>
/// Carries the machine-readable error code + a human-readable
/// message returned by a validator. The App service translates
/// this into a module-specific exception (e.g., MdmValidationException
/// for the MDM module, IdentityValidationException for the
/// Identity module) which the API endpoint maps to 400 + ProblemDetails.
/// </summary>
public sealed record CodeValidationFailure(string ErrorCode, string Message);
```

**No API change** from the existing Mdm-namespace version.
The class is moved verbatim. The `Fail(string, string)`
factory takes a `string errorCode` argument — this is the
key change that enables module-specific error codes: the
caller passes the context's corresponding error code
instead of the hard-coded MDM one.

### 4.6 The 3 static validator classes (moved + signature changed)

```csharp
namespace GuliERP.Foundation.Validation;

/// <summary>
/// FormatValidator — Step 1 of the master-data code pipeline.
/// Per GULIERP_CODE_PIPELINE_DESIGN_V1 §3.5 + CODE_RULE_STANDARD_V1 §2.1.
/// Rule: ^[A-Z][A-Z0-9_]{1,39}$ (length 2..40).
/// </summary>
public static class FormatValidator
{
    public static CodeValidationResult Validate(
        string? code, ICodeValidationContext context)
    {
        // ... body unchanged, but error codes are now
        // context.FormatInvalidErrorCode (not MdmErrorCodes.CodeFormatInvalid) ...
    }
}

public static class ReservedNameValidator
{
    public static CodeValidationResult Validate(
        string? code, ICodeValidationContext context)
    {
        // ... body unchanged, but error code is context.ReservedErrorCode ...
    }
}

public static class DocumentNumberSimilarityValidator
{
    public static CodeValidationResult Validate(
        string? code, ICodeValidationContext context)
    {
        // ... body unchanged, but error code is context.ResemblesDocumentNumberErrorCode ...
    }
}
```

**Breaking change (signature):** the existing
`Validate(string?)` signature (no `context` parameter) is
**replaced** by `Validate(string?, ICodeValidationContext)`.
The MDM call sites are updated to pass the context. There
is no backward-compatible overload — the design is a hard
promote, not a shim.

**Why a hard break?** The validators currently hard-code
`MdmErrorCodes.Code*`. There is no way to "backward-
compatibly" pass a different error code without changing
the signature. A new overload would create 2 ways to do the
same thing, which is a maintenance burden. The migration
plan (§3 of `MIGRATION_PLAN_001`) handles the call-site
update.

### 4.7 `MasterDataCodeValidator` (new static facade)

```csharp
namespace GuliERP.Foundation.Validation;

/// <summary>
/// The recommended entry point for the 4-step code pipeline.
/// Runs Steps 1, 2, 4 in order; Step 3 (uniqueness) is the
/// DB's job and is checked separately by the caller. Throws
/// nothing — returns a CodeValidationResult; the caller
/// translates to its module-specific exception.
/// </summary>
public static class MasterDataCodeValidator
{
    public static CodeValidationResult Validate(
        string? code, ICodeValidationContext context)
    {
        // Step 1: format (length + regex)
        var step1 = FormatValidator.Validate(code, context);
        if (!step1.IsValid) return step1;

        // Step 2: reserved name (system-wide set)
        var step2 = ReservedNameValidator.Validate(code, context);
        if (!step2.IsValid) return step2;

        // Step 3: uniqueness is the DB's job.

        // Step 4: no-document-number pattern
        var step4 = DocumentNumberSimilarityValidator.Validate(code, context);
        if (!step4.IsValid) return step4;

        return CodeValidationResult.Ok();
    }
}
```

**Recommended usage** in every module's helper:

```csharp
// In MdmService.ThrowIfCodeInvalid (post-migration):
internal static void ThrowIfCodeInvalid(string code, long tenantId, long? companyId)
{
    var context = CodeValidationContext.ForMdm(
        entityScope: "MdmBusinessPartner",
        tenantId: tenantId,
        companyId: companyId);

    var result = MasterDataCodeValidator.Validate(code, context);
    if (!result.IsValid)
    {
        throw new MdmValidationException(
            result.Failure!.ErrorCode, result.Failure.Message);
    }
}

// In EmployeeWriteService.ThrowIfEmployeeCodeInvalid (future):
internal static void ThrowIfEmployeeCodeInvalid(string code, long tenantId, long companyId)
{
    var context = new CodeValidationContext(
        EntityScope: "IdentityEmployee",
        TenantId: tenantId,
        CompanyId: companyId,
        FormatInvalidErrorCode: IdentityErrorCodes.EmployeeCodeFormatInvalid,
        ReservedErrorCode: IdentityErrorCodes.EmployeeCodeReserved,
        ResemblesDocumentNumberErrorCode: IdentityErrorCodes.EmployeeCodeResemblesDocumentNumber);

    var result = MasterDataCodeValidator.Validate(code, context);
    if (!result.IsValid)
    {
        throw new IdentityValidationException(
            result.Failure!.ErrorCode, result.Failure.Message);
    }
}
```

### 4.8 Foundation error codes (new, 3 constants)

Appended to `modules/foundation/GuliERP.Foundation/Kernel/ErrorCodes.cs`:

```csharp
// ============================================================
// GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE_DESIGN —
// Foundation-level generic error codes for the code pipeline.
// These are the FALLBACK codes when a module does not
// provide a module-specific code in CodeValidationContext.
// Per GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1 §4.2.
// ============================================================

public const string CodeFormatInvalid = "code_format_invalid";
public const string CodeReserved = "code_reserved";
public const string CodeResemblesDocumentNumber = "code_resembles_document_number";
```

The 3 codes are in the Foundation `ErrorCodes` class. The
existing `MdmErrorCodes.Code*` codes stay in `MdmErrorCodes`
(unchanged). The new `IdentityErrorCodes.EmployeeCode*` codes
are added by the Employee write implementation milestone.

### 4.9 DI registration (the V1 shape)

The Foundation's `AddGuliErpFoundation` extension method
adds:

```csharp
// In GuliERP.Foundation.DependencyInjection:

services.AddSingleton<ICodeRuleProvider>(V1FrozenRuleProvider.Instance);
// Note: the 3 validators are static, not DI-registered.
// ICodeValidator is not DI-registered in V1 (V1.5+ will).
```

The MDM / Identity / Sales / Purchase / Inventory modules
do NOT need to add any DI registration for the validators
themselves (they are static). The `ICodeRuleProvider` is
registered in Foundation (a singleton), and the V1.5+
modules that need a custom rule provider can override.

---

## 5. The dependency direction (V1, frozen)

### 5.1 The frozen dependency graph

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

### 5.2 The MDM Application → Foundation Validation move

| Before (current)                                      | After (post-migration)                               |
|-------------------------------------------------------|------------------------------------------------------|
| `GuliERP.Mdm.Application.Validation.FormatValidator` | `GuliERP.Foundation.Validation.FormatValidator`     |
| `GuliERP.Mdm.Application.Validation.ReservedNameValidator` | `GuliERP.Foundation.Validation.ReservedNameValidator` |
| `GuliERP.Mdm.Application.Validation.DocumentNumberSimilarityValidator` | `GuliERP.Foundation.Validation.DocumentNumberSimilarityValidator` |
| `GuliERP.Mdm.Application.Validation.CodeValidationResult` | `GuliERP.Foundation.Validation.CodeValidationResult` |
| (none)                                                | `GuliERP.Foundation.Validation.MasterDataCodeValidator` |
| (none)                                                | `GuliERP.Foundation.Validation.ICodeValidationContext` |
| (none)                                                | `GuliERP.Foundation.Validation.CodeValidationContext` |
| (none)                                                | `GuliERP.Foundation.Validation.ICodeValidator`       |
| (none)                                                | `GuliERP.Foundation.Validation.ICodeRuleProvider`    |
| (none)                                                | `GuliERP.Foundation.Validation.V1FrozenRuleProvider` |

### 5.3 The MDM call site update (signature change)

The `MdmService.ThrowIfCodeInvalid` helper changes from
1-arg to 3-arg:

```csharp
// Before:
internal static void ThrowIfCodeInvalid(string code)

// After:
internal static void ThrowIfCodeInvalid(string code, long tenantId, long? companyId)
```

The `MdmBusinessPartnerService.ThrowIfCodeInvalid` changes
the same way. The 3 call sites in `MdmService.CreateItemAsync`
+ `MdmBusinessPartnerService.CreateAsync` +
`MdmWarehouseService.CreateAsync` (the last calls into the
first via `MdmBusinessPartnerService.ThrowIfCodeInvalid(code)`)
are updated to pass `tenantId` + `companyId`.

### 5.4 Future consumer call shape (V1.5+)

When the Sales / Purchase / Inventory modules ship a
code-bearing entity in V1.5+, the shape is:

```csharp
// In a future Sales module's helper:
internal static void ThrowIfSalesCodeInvalid(string code, long tenantId, long? companyId)
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
            result.Failure!.ErrorCode, result.Failure.Message);
    }
}
```

The Foundation namespace knows nothing about the
`SalesErrorCodes` / `SalesValidationException` types. The
Sales module owns them; the Foundation owns the pipeline.

---

## 6. The 4-step pipeline (V1, frozen, unchanged from prior)

The 4 steps are unchanged from
`GULIERP_CODE_PIPELINE_DESIGN_V1.md` §3.5:

1. **Format check** — `^[A-Z][A-Z0-9_]{1,39}$` (length 2..40).
2. **Reserved-name check** — reject any code in the
   11-name V1 frozen set.
3. **Uniqueness check** — the DB's job (EF Core unique index
   + the existing `_db.<Set>.AnyAsync` check).
4. **No-document-number-pattern check** — reject any code
   containing 8 consecutive digits (YYYYMMDD) or starting
   with a document-type prefix
   (`SO/PO/GR/GI/TR/SI/PI/MO/QO`).

The validators live in Foundation, but the RULES (Step 1
regex, Step 2 set, Step 4 prefixes) are frozen in
`GULIERP_CODE_RULE_STANDARD_V1.md` §2 + §3 + §4. The
Foundation `V1FrozenRuleProvider` returns those frozen
values.

---

## 7. Test plan summary (V1, frozen; full plan in MIGRATION_PLAN_001)

### 7.1 Test type matrix

| Type             | New project                                         | New tests | Pattern |
|------------------|-----------------------------------------------------|-----------|---------|
| Unit (validator) | `tests/GuliERP.Foundation.Tests` (NEW)              | ~70       | Pure xUnit; no DB; no Moq; mirrors existing MDM tests |
| Integration      | (none — the validators are pure functions, no I/O) | 0         | n/a     |
| Architecture     | `tests/GuliERP.Foundation.Tests`                    | ~3        | Reflection-based (Foundation depends on no module) |

### 7.2 Per-validator test count (after the move)

| Validator                       | Tests (current) | Tests (after move) | Project                     |
|----------------------------------|-----------------|--------------------|------------------------------|
| `FormatValidator`                | 28              | 28                 | `GuliERP.Foundation.Tests`   |
| `ReservedNameValidator`          | 31              | 31                 | `GuliERP.Foundation.Tests`   |
| `DocumentNumberSimilarityValidator` | 44          | 44                 | `GuliERP.Foundation.Tests`   |
| `MasterDataCodeValidator` (NEW) | n/a             | ~6                 | `GuliERP.Foundation.Tests`   |
| `ICodeRuleProvider` (NEW)        | n/a             | ~3                 | `GuliERP.Foundation.Tests`   |
| **Subtotal — Foundation**        | **103**         | **112**            | (3 MDM wiring tests stay in MDM; see below) |
| **Stay in `GuliERP.Mdm.Tests`**  |                 |                    |                              |
| `MdmService.ThrowIfCodeInvalid` wiring (Step 1/2/4 + error code) | 50 | ~50 (re-targeted to call `MasterDataCodeValidator`) | `GuliERP.Mdm.Tests` |
| **Total**                        | **153**         | **~162**           | (the 9-test delta = the 3 NEW facade + 3 NEW rule provider + 3 NEW architecture) |

### 7.3 The MDM wiring tests stay in MDM

The `MdmServiceCodeValidationTests.cs` file (50 tests)
asserts that `MdmService.ThrowIfCodeInvalid` throws
`MdmValidationException` with the right MDM error code.
These tests are re-targeted after the migration to call
the new `MdmService.ThrowIfCodeInvalid(code, tenantId,
companyId)` (3-arg) signature. The assertions are unchanged
(`MdmErrorCodes.CodeFormatInvalid` etc.) because the
`ForMdm` factory wires the MDM codes.

The test count does not drop to 0; the tests verify the
MDM-specific wiring (the foundation-level tests verify
the validators themselves; the MDM tests verify the
MDM-specific throw + error code mapping).

---

## 8. The migration risk (V1, frozen; full risk in MIGRATION_PLAN_001)

### 8.1 Risk 1: signature break

The 3 validators' `Validate(string?)` signature is
replaced by `Validate(string?, ICodeValidationContext)`.
All 3 call sites in MDM must be updated. The migration
plan handles this with an atomic migration commit.

**Mitigation:** the migration is one PR. The `MdmService`
+ `MdmMasterData002Services` change in the same commit.
Build + test pass together.

### 8.2 Risk 2: test file split

The 4 test files currently in `GuliERP.Mdm.Tests` split
into:

- 3 files in a new `tests/GuliERP.Foundation.Tests/`
  project (the validator unit tests).
- 1 file stays in `GuliERP.Mdm.Tests` (the MDM wiring
  test, retargeted to the new 3-arg helper).

**Mitigation:** the new `GuliERP.Foundation.Tests` project
is created in the same PR. The build + test counts are
matched (existing 153 stay green; new ~9 added).

### 8.3 Risk 3: the 2 pre-existing inherited flaky tests

The 2 `MdmCurrentTenantParallelTests.MdmSeed_ResolveSeedFilePath_*`
flaky tests are in `GuliERP.Mdm.Tests`. They are NOT
moved. They remain flaky (the design does not fix them;
that is a separate milestone).

### 8.4 Risk 4: no DI registration for the 3 static validators

V1 ships the 3 validators as `public static class`. They
are NOT DI-registered. The `MasterDataCodeValidator`
facade is also `public static class`. This is consistent
with the existing V1 patterns (the `MdmService.ThrowIfCodeInvalid`
helper is also `internal static` — no DI).

**Mitigation:** the V1.5+ refactor (not in this design)
adds `ICodeValidator` DI-registered instances. The V1
static classes are wrappers around the V1.5+ DI instances
in V1.5+. The V1.5+ refactor is its own design goal
(recorded in `MIGRATION_PLAN_001` §6 as "Future Work").

---

## 9. Out of scope (V1, frozen)

- **The V1.5+ DI-registered validator instances.** The
  `ICodeValidator` interface is defined for the V1.5+ shape;
  the V1.5+ implementation is a separate design goal.
- **The V1.5+ rule provider customization per Tenant.**
  `ICodeRuleProvider` is defined for the V1.5+ shape; the
  V1.5+ implementation is a separate design goal.
- **The `ForIdentity` / `ForSales` / `ForPurchase` /
  `ForInventory` factories.** These are added by their
  respective implementation milestones (Employee write
  surface ships `ForIdentity`; the others are V1.5+).
- **The Sales / Purchase / Inventory error code extension.**
  V1.5+ when those modules ship.
- **The auto-coding engine** (V2+ per the V1 model).
- **The internal reference number on documents** (V2+).
- **The V1.5+ Employee write surface** — that is the
  `GULIERP_HR_001_EMPLOYEE_WRITE_V1` milestone, which
  DEPENDS on this Foundation promote.

---

## 10. Authority chain

This document inherits its authority from:

- `docs/architecture/MDM_000_MASTER_DATA_CONVENTION_V1.md`
  (the MDM master-data convention; the canonical source
  for `Id` / `Code` / `Name` / `Status` / `TenantId` /
  `CompanyId` / audit / concurrency rules).
- `docs/governance/GULIERP_MODULE_INDEPENDENCE_RULE.md` (the
  module ownership rule; the canonical reason for the
  Foundation promote).
- `docs/governance/META_GULI_GOVERNANCE_V1.md` (the
  cross-cutting governance rule).
- `docs/business/GULIERP_CODE_RULE_STANDARD_V1.md` (FROZEN)
  — the V1 code rule standard this design respects.
- `docs/business/GULIERP_MASTER_DATA_MODEL_V1.md` (FROZEN)
  — the V1 master-data vocabulary this design respects.
- `docs/business/GULIERP_CODE_PIPELINE_DESIGN_V1.md` (FROZEN)
  — the V1 code pipeline design (4 steps; the rules
  frozen in this document are unchanged).
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

This document does NOT modify any of the above. It locks
the V1 Foundation code pipeline contract to the level of
detail required by the migration milestone.

---

## 11. One-line summary

The V1 master-data code pipeline (3 validators + result type
+ new `MasterDataCodeValidator` facade + 3 strategy
interfaces) moves from `GuliERP.Mdm.Application.Validation`
to `GuliERP.Foundation.Validation`; the 3 static validators
take a new `ICodeValidationContext` parameter that carries
the module-specific error codes, so the Identity / Sales /
Purchase / Inventory modules can reuse the same 4-step
pipeline without depending on the MDM module, preserving
`GULIERP_MODULE_INDEPENDENCE_RULE`.
