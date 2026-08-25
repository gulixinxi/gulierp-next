# GULIERP_CODE_PIPELINE_DESIGN_V1

> Goal: design the unified ERP master-data code validation
> pipeline. Today the existing implementation enforces only
> Step 3 (uniqueness); Steps 1 (format), 2 (reserved-name),
> and 4 (no-document-number-pattern) are missing. This
> document designs the rule model, validator architecture,
> error-code design, Application service integration, unit
> test plan, and concrete examples for BusinessPartner / Item /
> Warehouse.
> **This is a DESIGN document.** No code, no migration, no
> DB change, no API change, no Vue page change ship with this
> PR. The implementation lands in a future design goal
> (`GULIERP_MDM_001_CODE_PIPELINE`) and ships its own audit +
> design + implementation + test + report cycle.
> Authority: `docs/governance/META_GULI_GOVERNANCE_V1.md` +
> `docs/governance/GULIERP_MODULE_INDEPENDENCE_RULE.md` +
> `docs/business/GULIERP_CODE_RULE_STANDARD_V1.md` +
> `docs/business/GULIERP_MDM_COMPLETION_GAP_ANALYSIS_001.md` +
> `docs/business/GULIERP_MDM_IMPLEMENTATION_PLAN_001.md`.

Date: 2026-08-24
Status: **CODE_PIPELINE_DESIGN_V1_FROZEN**

---

## 1. Goal and non-goal

### 1.1 Goal

Bring the existing MDM Application service in line with the
**frozen** `GULIERP_CODE_RULE_STANDARD_V1` §8 — a 4-step
validation pipeline. After this design lands (and the
implementation follows), every master-data create / update
goes through:

1. **Format check** — regex `^[A-Z][A-Z0-9_]{1,39}$`.
2. **Reserved-name check** — reject `SYSTEM / SYS / RESERVED /
   EMP-SYSTEM / WH-DEFAULT / LOC-RECEIVING / LOC-SHIPPING /
   ROLE_PLATFORM_ADMIN / ROLE_TENANT_ADMIN / ROLE_COMPANY_ADMIN
   / ROLE_NORMAL_USER`.
3. **Uniqueness check** — already implemented (DB unique
   index + App service check).
4. **No-document-number-pattern check** — reject any code
   containing 8 consecutive digits OR starting with
   `SO/PO/GR/GI/TR/SI/PI/MO/QI`.

### 1.2 Non-goal

- **Auto-coding engine** (V2/V3 per `CODE_RULE_STANDARD_V1`
  §9). This design is for the **manual** path. Operators type
  codes; the validator enforces the rule.
- **Reserved-name management UI** (admin tool to add new
  reserved codes). The reserved set is **code-frozen** in
  this design; adding a new reserved code requires a design
  goal.
- **Per-tenant customization** (Yonyou NC Cloud pattern).
  V1 ships one global rule set. A future per-tenant override
  is a separate WorkItem.
- **PII / GDPR handling** of codes. The validator does not
  store or log PII; it only returns a structured error.

---

## 2. Code rule model

### 2.1 The 4-step validation rules (frozen)

| Step | Rule name             | Source                                                            | Error code (new)             |
|------|-----------------------|-------------------------------------------------------------------|------------------------------|
| 1    | `CheckFormat`         | `^[A-Z][A-Z0-9_]{1,39}$`                                           | `InvalidCodeFormat`          |
| 2    | `CheckReservedName`   | `SYSTEM / SYS / RESERVED / EMP-SYSTEM / WH-DEFAULT / LOC-RECEIVING / LOC-SHIPPING / ROLE_PLATFORM_ADMIN / ROLE_TENANT_ADMIN / ROLE_COMPANY_ADMIN / ROLE_NORMAL_USER` | `ReservedCode` |
| 3    | `CheckUniqueness`     | scoped to `(TenantId, Code)` or `(CompanyId, Code)`             | (existing — DB unique index) |
| 4    | `CheckResemblesDocumentNumber` | no 8 consecutive digits + no `^(SO|PO|GR|GI|TR|SI|PI|MO|QI)[-_]` | `ResemblesDocumentNumber` |

### 2.2 The rule table (single source of truth, code-frozen)

```csharp
/// <summary>
/// MasterDataCodeRules — the FROZEN rule set for the GuliERP
/// master-data code validation pipeline. Mirrors
/// GULIERP_CODE_RULE_STANDARD_V1 §8 verbatim. Any change to a
/// value here requires updating the V1 standard first.
/// </summary>
public static class MasterDataCodeRules
{
    // -------- Step 1: format --------
    public const string CodeRegexPattern = @"^[A-Z][A-Z0-9_]{1,39}$";
    public const int MinLength = 2;
    public const int MaxLength = 40;

    // -------- Step 2: reserved names --------
    public static readonly IReadOnlySet<string> ReservedCodes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SYSTEM", "SYS", "RESERVED",
            "EMP-SYSTEM", "WH-DEFAULT",
            "LOC-RECEIVING", "LOC-SHIPPING",
            "ROLE_PLATFORM_ADMIN", "ROLE_TENANT_ADMIN",
            "ROLE_COMPANY_ADMIN", "ROLE_NORMAL_USER"
        };

    // -------- Step 3: uniqueness (NOT in this class — DB-level) --------
    // The Application service relies on the EF Core unique index
    // on (TenantId, Code) or (CompanyId, Code) per entity. The
    // validator does NOT do the DB check; it only handles steps
    // 1, 2, 4. The error code path is the existing
    // DbUpdateException → MdmErrorCode.MdmUniqueConstraintViolated.

    // -------- Step 4: document-number pattern --------
    public static readonly IReadOnlyList<string> DocumentTypePrefixes =
        new[] { "SO", "PO", "GR", "GI", "TR", "SI", "PI", "MO", "QI" };

    /// <summary>
    /// 8 consecutive digits anywhere in the code (catches
    /// "20240101" anywhere — start, middle, or end of the code).
    /// The V1 standard §3.1 forbids a date pattern in a code.
    /// </summary>
    private static readonly Regex DateInCodePattern =
        new(@"\d{8}", RegexOptions.Compiled);

    /// <summary>
    /// Document-type prefix at the start of the code, optionally
    /// followed by `-` or `_` separator. The V1 standard §3.1
    /// forbids a document-number-style prefix in a code.
    /// Case-insensitive (the code is canonicalized to upper
    /// BEFORE this check).
    /// </summary>
    public static readonly Regex DocPrefixPattern =
        new(@"^(SO|PO|GR|GI|TR|SI|PI|MO|QI)[-_]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
}
```

### 2.3 The 4 master-data code types (V1)

The validator is generic over `MasterDataCodeType`. Each entity
maps to one type. The mapping is a constant table on the
validator's consumer (the App service), not on the validator
itself — the validator does not need to know which entity it
is validating.

```csharp
public enum MasterDataCodeType
{
    // Identity scope (V1)
    TenantCode,
    CompanyCode,
    PlantCode,
    OrganizationUnitCode,
    RoleCode,
    EmployeeNo,

    // MDM scope (V1)
    UomCode,
    ItemCategoryCode,
    ItemCode,
    BusinessPartnerCode,
    WarehouseCode,
    LocationCode,

    // V1.5 deferred (Tax / Brand / PaymentTerm per P1 plan)
    BrandCode,
    TaxSchemeCode,
    TaxRateCode,
    PaymentTermCode
}
```

> **Why enum, not attribute on the entity?** The validator is
> decoupled from the entity type. The App service decides
> which `MasterDataCodeType` to pass; the validator does the
> same 4 checks regardless. This keeps the validator testable
> in isolation and reusable when a new entity needs a code
> check.

---

## 3. Validator architecture

### 3.1 Module placement

```
GuliERP.Foundation.Kernel         ← (existing) TenantId, CompanyId, audit
GuliERP.Mdm.Application           ← (existing) IMdmService, DTOs
   └─ MasterDataCodeValidator      ← (NEW) IMasterDataCodeValidator + impl
   └─ MasterDataCodeValidationContext  ← (NEW) record
   └─ CodeValidationResult / Failure  ← (NEW) records
GuliERP.Mdm.Infrastructure        ← (existing) DI registration
   └─ DependencyInjection.cs       ← (modify) add validator to DI
```

The validator lives in `GuliERP.Mdm.Application` (NOT
Infrastructure, NOT Foundation). Reason: the rule set is a
business rule, not a persistence concern. The validator is
pure logic; it has no DB or HTTP dependency. Tests can instantiate
it with `new MasterDataCodeValidator()` and assert behavior
without mocking.

### 3.2 Interface

```csharp
namespace GuliERP.Mdm.Application;

/// <summary>
/// MasterDataCodeValidator — the unified 4-step code
/// validation pipeline per GULIERP_CODE_PIPELINE_DESIGN_V1 §2.
/// 
/// <para>
/// Step 3 (uniqueness) is NOT part of this interface. The
/// Application service combines this validator with the
/// EF Core unique index on the entity table. The split is
/// intentional: this validator is pure / no-IO, the unique
/// check is the DB's job.
/// </para>
/// </summary>
public interface IMasterDataCodeValidator
{
    /// <summary>
    /// Run the full pipeline (Steps 1, 2, 4). Returns the first
    /// failure or Ok. Step 3 (uniqueness) is the caller's job.
    /// </summary>
    CodeValidationResult Validate(MasterDataCodeValidationContext context);

    /// <summary>Step 1 only — format check.</summary>
    CodeValidationResult CheckFormat(string code);

    /// <summary>Step 2 only — reserved-name check.</summary>
    CodeValidationResult CheckReservedName(string code);

    /// <summary>Step 4 only — no-document-number-pattern check.</summary>
    CodeValidationResult CheckResemblesDocumentNumber(string code);
}
```

### 3.3 Context record

```csharp
/// <summary>
/// Input to IMasterDataCodeValidator.Validate. Carries the code
/// being checked, the code type, the tenant (and company, if
/// company-scoped), and the entity id (null = create, non-null
/// = update). The validator is stateless; the context carries
/// everything the rules need.
/// </summary>
public sealed record MasterDataCodeValidationContext(
    string Code,
    MasterDataCodeType CodeType,
    long TenantId,
    long? CompanyId = null,
    long? EntityIdForUpdate = null
);
```

### 3.4 Result records

```csharp
public sealed record CodeValidationResult
{
    public bool IsValid { get; }
    public CodeValidationFailure? Failure { get; }
    public static CodeValidationResult Ok { get; } =
        new(true, null);
    private CodeValidationResult(
        bool isValid, CodeValidationFailure? failure)
    {
        IsValid = isValid;
        Failure = failure;
    }

    public static CodeValidationResult Fail(MdmErrorCode code,
        string message, string? detail = null)
        => new(false, new CodeValidationFailure(code, message, detail));
}

public sealed record CodeValidationFailure(
    MdmErrorCode ErrorCode,
    string Message,
    string? Detail
);
```

### 3.5 Implementation outline (skeleton)

```csharp
public sealed class MasterDataCodeValidator : IMasterDataCodeValidator
{
    public CodeValidationResult Validate(
        MasterDataCodeValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Canonicalize — defensive. The App service SHOULD
        // canonicalize before calling; if it forgets, the
        // canonicalization here makes the validator a
        // single-source-of-truth.
        var code = (context.Code ?? string.Empty).Trim().ToUpperInvariant();

        if (code.Length == 0)
        {
            return CodeValidationResult.Fail(
                MdmErrorCode.CodeCannotBeEmpty,
                "代码不能为空。");
        }

        // Step 1: format
        var step1 = CheckFormat(code);
        if (!step1.IsValid) return step1;

        // Step 2: reserved name
        var step2 = CheckReservedName(code);
        if (!step2.IsValid) return step2;

        // Step 3: uniqueness is the caller's job (DB unique index).

        // Step 4: document-number pattern
        var step4 = CheckResemblesDocumentNumber(code);
        if (!step4.IsValid) return step4;

        return CodeValidationResult.Ok;
    }

    public CodeValidationResult CheckFormat(string code)
    {
        if (code.Length < MasterDataCodeRules.MinLength ||
            code.Length > MasterDataCodeRules.MaxLength)
        {
            return CodeValidationResult.Fail(
                MdmErrorCode.InvalidCodeFormat,
                $"代码长度必须在 {MasterDataCodeRules.MinLength}–" +
                $"{MasterDataCodeRules.MaxLength} 字符之间。",
                $"实际长度 {code.Length}。");
        }

        if (!Regex.IsMatch(code, MasterDataCodeRules.CodeRegexPattern))
        {
            return CodeValidationResult.Fail(
                MdmErrorCode.InvalidCodeFormat,
                "代码必须以大写字母开头,仅含大写字母 / 数字 / 下划线。",
                $"实际值: {code}。");
        }

        return CodeValidationResult.Ok;
    }

    public CodeValidationResult CheckReservedName(string code)
    {
        if (MasterDataCodeRules.ReservedCodes.Contains(code))
        {
            return CodeValidationResult.Fail(
                MdmErrorCode.ReservedCode,
                $"代码 \"{code}\" 是系统保留关键字,不能使用。",
                "请改用业务相关的代码。");
        }

        return CodeValidationResult.Ok;
    }

    public CodeValidationResult CheckResemblesDocumentNumber(
        string code)
    {
        if (MasterDataCodeRules.DateInCodePattern.IsMatch(code))
        {
            return CodeValidationResult.Fail(
                MdmErrorCode.ResemblesDocumentNumber,
                $"代码 \"{code}\" 包含 8 位连续数字,与单据号格式冲突。",
                "主数据代码不能含日期。");
        }

        if (MasterDataCodeRules.DocPrefixPattern.IsMatch(code))
        {
            var prefix = MasterDataCodeRules
                .DocumentTypePrefixes
                .First(p => code.StartsWith(p,
                    StringComparison.OrdinalIgnoreCase));
            return CodeValidationResult.Fail(
                MdmErrorCode.ResemblesDocumentNumber,
                $"代码 \"{code}\" 以单据号前缀 \"{prefix}\" 开头," +
                "与单据号格式冲突。",
                "主数据代码不能以单据类型前缀开头。");
        }

        return CodeValidationResult.Ok;
    }
}
```

### 3.6 DI registration

```csharp
// In GuliERP.Mdm.Infrastructure/DependencyInjection.cs:
public static IServiceCollection AddGuliErpMdmApplication(
    this IServiceCollection services)
{
    services.AddSingleton<IMasterDataCodeValidator,
        MasterDataCodeValidator>();
    // ... existing registrations
    return services;
}
```

Singleton is appropriate: the validator is stateless and the
compiled `Regex` objects are thread-safe.

### 3.7 Step ordering rationale (why 1 → 2 → 4)

| Step | Why in this order                                        |
|------|----------------------------------------------------------|
| 1    | Format first — catches 90% of typos. Fast. Cheap.       |
| 2    | Reserved-name second — exact-match lookup, O(1) hash set. |
| 4    | Document-number pattern last — regex scan, slightly more expensive than the O(1) checks. |
| 3    | Uniqueness is the DB's job. Not in the validator.        |

Each step is short-circuited: a failure at step 1 returns
immediately; steps 2 and 4 are not evaluated. This makes the
happy path (all-pass) a single pass; the failure path is
early-exit.

---

## 4. Error code design

### 4.1 Existing `MdmErrorCodes.cs` (current state)

```csharp
public static class MdmErrorCodes
{
    public const string UomNotFound = "mdm_uom_not_found";
    public const string UomDuplicateCode = "mdm_uom_duplicate_code";
    public const string ItemNotFound = "mdm_item_not_found";
    public const string ItemDuplicateCode = "mdm_item_duplicate_code";
    public const string ItemCategoryNotFound = "mdm_item_category_not_found";
    public const string ItemCategoryDuplicateCode =
        "mdm_item_category_duplicate_code";
    public const string ItemCategorySelfParent = "mdm_item_category_self_parent";
    public const string ItemCategoryCycleDetected =
        "mdm_item_category_cycle_detected";
    public const string ItemBaseUomNotFound = "mdm_item_base_uom_not_found";
    public const string BusinessPartnerNotFound =
        "mdm_business_partner_not_found";
    public const string BusinessPartnerDuplicateCode =
        "mdm_business_partner_duplicate_code";
    public const string WarehouseNotFound = "mdm_warehouse_not_found";
    public const string WarehouseDuplicateCode =
        "mdm_warehouse_duplicate_code";
    public const string LocationNotFound = "mdm_location_not_found";
    public const string LocationDuplicateCode = "mdm_location_duplicate_code";
    public const string MdmUniqueConstraintViolated =
        "mdm_unique_constraint_violated";
    public const string MdmConcurrencyConflict = "mdm_concurrency_conflict";
    // (etc.)
}
```

### 4.2 New error codes (to add)

| Code                                  | When fired                                 | HTTP status | UI message                                            |
|---------------------------------------|--------------------------------------------|-------------|-------------------------------------------------------|
| `mdm_code_format_invalid`             | Step 1 failed                              | 400         | "代码格式不正确。请使用大写字母、数字、下划线,2–40 字符。" |
| `mdm_code_reserved`                   | Step 2 failed                              | 400         | "代码 \"{code}\" 是系统保留关键字。"                  |
| `mdm_code_resembles_document_number`  | Step 4 failed                              | 400         | "代码与单据号格式冲突。请换一个不含日期或单据前缀的代码。" |
| `mdm_code_empty`                      | Defensive — code is null / whitespace    | 400         | "代码不能为空。"                                     |

> The codes are **machine-readable** (`mdm_code_format_invalid`)
> and have a human-readable message (the `Message` field of
> `CodeValidationFailure`). The UI surfaces the message; the
> code is for log / test / analytics.

### 4.3 How the App service surfaces the error

The validator returns `CodeValidationResult` (with a
`Failure` payload). The App service wraps a non-Ok result in
an `MdmValidationException` and throws. The API endpoint
catches `MdmValidationException` and maps the
`MdmErrorCode` to a `ProblemDetails` 400 response. The Vue
form drawer displays the `Message` field.

```csharp
// In the App service:
if (!result.IsValid)
{
    throw new MdmValidationException(
        result.Failure!.ErrorCode,
        result.Failure.Message,
        result.Failure.Detail);
}
```

### 4.4 Where to add the new codes (file location)

`modules/mdm/GuliERP.Mdm.Application/MdmErrorCodes.cs` — the
existing constants file. The 4 new codes are added as
`public const string` members, alphabetically sorted. No
breaking change (existing callers use the existing constants).

### 4.5 Reserved-name set is NOT in the error code

The reserved-name set is in `MasterDataCodeRules.ReservedCodes`
(§2.2). It is a **constant**, not an error code. The error
code (`mdm_code_reserved`) is the single code that the
validator fires when ANY reserved name is matched. If a
future admin needs to add a new reserved name, they update
the constant table + the V1 standard doc; no new error code
is needed.

---

## 5. Application service integration

### 5.1 The integration point

The validator is wired into the App service via constructor
injection. The two existing App services (`MdmService` and
`MdmMasterData002Services`) each inject
`IMasterDataCodeValidator` once and call it on every
`Create*Async` and `Update*Async` entry point.

```csharp
public sealed class MdmService : IMdmService
{
    private readonly IMasterDataCodeValidator _codeValidator;
    // ... existing injected fields

    public MdmService(
        // ... existing constructor params
        IMasterDataCodeValidator codeValidator)
    {
        // ...
        _codeValidator = codeValidator;
    }
}
```

### 5.2 Where to call the validator (in the create / update flow)

The validator is called **as the first line of business logic**
in `CreateXxxAsync` and `UpdateXxxAsync`, BEFORE any DB
operation. The flow is:

```
Request → auth check → CANONICALIZE (trim + upper) → VALIDATE (4 steps)
        → DB uniqueness check (existing) → persist → return DTO
```

If `Validate` returns non-Ok, the App service throws
`MdmValidationException` immediately; no DB is touched.

### 5.3 Example: MdmService.CreateItemAsync (before)

```csharp
public async Task<ItemDto> CreateItemAsync(
    CreateItemRequest request, CancellationToken ct = default)
{
    // 1. canonicalize
    var code = (request.Code ?? string.Empty).Trim().ToUpperInvariant();
    var name = (request.Name ?? string.Empty).Trim();
    if (string.IsNullOrEmpty(name))
        throw new MdmValidationException(
            MdmErrorCodes.ItemNameRequired, "...");

    // 2. uniqueness (current — DB-level)
    var dupe = await _db.Items
        .AnyAsync(x => x.TenantId == _currentTenant.Id
                    && x.Code == code, ct);
    if (dupe)
        throw new MdmValidationException(
            MdmErrorCodes.ItemDuplicateCode, "...");

    // 3. resolve FKs
    // 4. create entity
    // 5. save
    // 6. return DTO
}
```

### 5.4 Example: MdmService.CreateItemAsync (after)

```csharp
public async Task<ItemDto> CreateItemAsync(
    CreateItemRequest request, CancellationToken ct = default)
{
    // 1. canonicalize
    var code = (request.Code ?? string.Empty).Trim().ToUpperInvariant();
    var name = (request.Name ?? string.Empty).Trim();
    if (string.IsNullOrEmpty(name))
        throw new MdmValidationException(
            MdmErrorCodes.ItemNameRequired, "...");

    // 2. NEW: 4-step code validation (steps 1, 2, 4)
    var codeContext = new MasterDataCodeValidationContext(
        code,
        MasterDataCodeType.ItemCode,
        _currentTenant.Id ?? 0L,
        CompanyId: null,
        EntityIdForUpdate: null);
    var codeResult = _codeValidator.Validate(codeContext);
    if (!codeResult.IsValid)
    {
        throw new MdmValidationException(
            codeResult.Failure!.ErrorCode,
            codeResult.Failure.Message,
            codeResult.Failure.Detail);
    }

    // 3. uniqueness (existing — DB-level)
    var dupe = await _db.Items
        .AnyAsync(x => x.TenantId == _currentTenant.Id
                    && x.Code == code, ct);
    if (dupe)
        throw new MdmValidationException(
            MdmErrorCodes.ItemDuplicateCode, "...");

    // 4. resolve FKs
    // 5. create entity
    // 6. save
    // 7. return DTO
}
```

The diff is small (3 lines: instantiate context, call Validate,
throw on fail). The same pattern applies to every `Create*Async`
and `Update*Async` in `MdmService` and
`MdmMasterData002Services`.

### 5.5 Update path (special case)

For `Update*Async`, the code IS the same on update (codes are
immutable in V1). But the validator is still called on update
because:

- A future WorkItem may add code edit (e.g. for typo
  correction). The validator should be ready.
- The canonicalization is shared (trim + upper) — even on
  update, the canonicalized value is what reaches the DB.
- The reserved-name check is defense-in-depth.

The `EntityIdForUpdate` field in the context is `null` for
update too in V1 (codes are immutable, so the validator does
not need to know which entity is being updated). A future
code-edit WorkItem will populate it.

### 5.6 Canonicalization order (trim + upper)

The App service canonicalizes (trim + upper) BEFORE calling
the validator. The validator also canonicalizes defensively
(§3.5). This double-canonicalization is intentional: the App
service's canonicalization is the contract (the value reaches
the DB), the validator's is the safety net (the rules apply
to the canonical form).

### 5.7 Where the canonicalization lives

The canonicalization is a small utility — the App service
already does it. The design does NOT add a new
`MasterDataCodeCanonicalizer` class. The App service keeps
its existing one-liner:

```csharp
var code = (request.Code ?? string.Empty).Trim().ToUpperInvariant();
```

If a future WorkItem extracts the canonicalization to a
shared utility, the validator will accept the canonical form
without re-canonicalizing (a future optimization, not V1).

### 5.8 Where the App service maps errors to HTTP

`apps/api/GuliERP.Api/Foundation/ExceptionHandler.cs` already
maps `MdmValidationException` to a `ProblemDetails` 400
response. The 4 new error codes (Step 1, 2, 4, defensive empty)
inherit this mapping. **No change to the exception handler.**

---

## 6. Unit test plan

### 6.1 `MasterDataCodeValidatorTests` (new file in
`tests/GuliERP.Mdm.Tests/`)

#### 6.1.1 `CheckFormat` tests (15 tests)

| # | Input                       | Expected | Reason                                 |
|---|-----------------------------|----------|----------------------------------------|
| 1 | `KGM`                       | Ok       | 3-char UoM code                        |
| 2 | `MAT-STEEL-A36`             | Ok       | 14-char Item code (hyphenated by operator? NO — hyphen not allowed, this is a NEW test for step 1) → FAIL: hyphens are not in the regex |
| 3 | `MAT_STEEL_A36`             | Ok       | 14-char Item code (underscore)         |
| 4 | `UPPER_SNAKE_2`             | Ok       | 13-char Item code                      |
| 5 | `ABC123XYZ`                 | Ok       | mixed letters + digits                 |
| 6 | `A`                         | Fail     | too short (length 1)                   |
| 7 | `A` + 40 chars              | Fail     | too long (length 41)                   |
| 8 | `lowercase`                 | Fail     | lowercase not allowed                  |
| 9 | `1STARTS-WITH-DIGIT`        | Fail     | leading digit not allowed              |
| 10 | `DOUBLE__UNDERSCORE`       | Fail     | double underscore                     |
| 11 | `HAS SPACE`                 | Fail     | whitespace not allowed                 |
| 12 | `HAS-DASH`                  | Fail     | hyphen not allowed                     |
| 13 | (empty string)              | Fail     | defensive — `mdm_code_empty`           |
| 14 | `null`                      | Fail     | defensive — `mdm_code_empty`           |
| 15 | `AB`                        | Ok       | boundary: min length 2                 |

(Re-test #2 — `MAT-STEEL-A36` with hyphen — is intentionally
FAIL per the V1 standard §2.1. The `CODE_RULE_STANDARD_V1` §4.1
says "hyphens are NOT allowed in the base rule; if an operator
wants a hyphen, they use the Application service's
auto-format feature, which translates hyphens to underscores
before persisting" — i.e. the App service accepts hyphens,
canonicalizes them to underscores, and validates. The
**validator** does NOT accept hyphens. The 15 tests above
verify the validator; the auto-format is a separate
WorkItem.)

#### 6.1.2 `CheckReservedName` tests (15 tests)

| # | Input                  | Expected | Reason                              |
|---|------------------------|----------|-------------------------------------|
| 1 | `CUSTOMER001`          | Ok       | normal customer code                |
| 2 | `MAT-STEEL`            | Ok       | normal item code                    |
| 3 | `SYSTEM`               | Fail     | reserved                            |
| 4 | `system`               | Fail     | reserved (case-insensitive)         |
| 5 | `SYS`                   | Fail     | reserved                            |
| 6 | `RESERVED`              | Fail     | reserved                            |
| 7 | `EMP-SYSTEM`            | Fail     | reserved                            |
| 8 | `WH-DEFAULT`            | Fail     | reserved                            |
| 9 | `LOC-RECEIVING`         | Fail     | reserved                            |
| 10 | `LOC-SHIPPING`         | Fail     | reserved                            |
| 11 | `ROLE_PLATFORM_ADMIN`  | Fail     | reserved                            |
| 12 | `ROLE_TENANT_ADMIN`    | Fail     | reserved                            |
| 13 | `ROLE_COMPANY_ADMIN`   | Fail     | reserved                            |
| 14 | `ROLE_NORMAL_USER`     | Fail     | reserved                            |
| 15 | `RESERVED-MINE` (not in the set) | Ok | valid (just happens to contain the substring "RESERVED") |

#### 6.1.3 `CheckResemblesDocumentNumber` tests (10 tests)

| # | Input                       | Expected | Reason                              |
|---|-----------------------------|----------|-------------------------------------|
| 1 | `CUSTOMER001`              | Ok       | no 8 digits, no doc prefix         |
| 2 | `20240101`                 | Fail     | 8 consecutive digits                |
| 3 | `SO-20240101-0001`         | Fail     | 8 digits AND doc prefix              |
| 4 | `PO-ABC-001`               | Fail     | doc prefix `PO`                     |
| 5 | `PO_ABC_001`               | Fail     | doc prefix `PO` (underscore separator) |
| 6 | `po-abc-001`               | Fail     | doc prefix (case-insensitive)       |
| 7 | `GR-001`                   | Fail     | doc prefix `GR`                     |
| 8 | `GI-X-01`                  | Fail     | doc prefix `GI`                     |
| 9 | `MAT-2024`                 | Ok       | only 4 digits, no doc prefix         |
| 10 | `MAT-20240101-A`          | Fail     | 8 consecutive digits in middle      |

#### 6.1.4 Full `Validate` tests (8 tests)

| # | Input                    | Code type              | Expected step  | Reason                          |
|---|--------------------------|------------------------|-----------------|---------------------------------|
| 1 | `KGM` (valid)            | UomCode                | Ok              | all 3 steps pass                 |
| 2 | `lowercase` (format fail)| UomCode                | 1               | Step 1 fails first               |
| 3 | `SYSTEM` (reserved)     | BusinessPartnerCode    | 2               | Step 1 passes, Step 2 fails    |
| 4 | `20240101` (date fail)   | ItemCode               | 4               | Step 1, 2 pass, Step 4 fails   |
| 5 | `PO-ABC-001` (doc fail)  | WarehouseCode          | 4               | Step 4 fails on doc prefix      |
| 6 | `lowercase` (format fail) | UomCode              | 1 (only)        | short-circuit: step 2 not run  |
| 7 | `SYS` (reserved)        | ItemCode               | 2 (only)        | short-circuit: step 4 not run  |
| 8 | (empty string)           | ItemCode               | 0 (defensive)   | empty code fails first         |

> Tests 6, 7, 8 are critical — they assert the short-circuit
> behavior. If a future refactor changes the order, the
> short-circuit tests catch it.

#### 6.1.5 Total unit tests in `MasterDataCodeValidatorTests`: **48**

### 6.2 `MdmEntityCodeValidationTests` (new file, integration
with the App service)

These tests assert the App service uses the validator
correctly. They DO NOT need a real DB; they mock the validator
and assert the throw / pass behavior.

#### 6.2.1 Per-entity tests (6 entities × 3 scenarios = 18)

For each of `BusinessPartner / Item / ItemCategory / UoM /
Warehouse / Location`:

- `CreateAsync_WithFormatInvalidCode_Throws` — `lowercase`
  code → `MdmValidationException(MdmErrorCodes.InvalidCodeFormat)`.
- `CreateAsync_WithReservedCode_Throws` — `SYSTEM` code →
  `MdmValidationException(MdmErrorCodes.ReservedCode)`.
- `CreateAsync_WithDocPatternCode_Throws` — `20240101` code
  → `MdmValidationException(MdmErrorCodes.ResemblesDocumentNumber)`.
- `UpdateAsync_WithFormatInvalidCode_Throws` — same.
- `UpdateAsync_WithReservedCode_Throws` — same.
- `UpdateAsync_WithDocPatternCode_Throws` — same.

(6 entities × 6 = **36** tests)

#### 6.2.2 Cross-entity tests (3 tests)

- `Validator_CalledOncePerCreate` — mock the validator,
  assert it's called exactly once per `Create*Async`.
- `Validator_CalledOncePerUpdate` — same for update.
- `Validator_NotCalledOnGet` — list / get endpoints do NOT
  invoke the validator (read paths are not validated).

#### 6.2.3 Total tests in `MdmEntityCodeValidationTests`: **39**

### 6.3 Existing test surface — no regression

- The existing `MdmEntityContractTests` continues to PASS
  (the contract test asserts the entity has the V1 fields;
  the new validator does not change the entity).
- The existing `MdmDtosTests` continues to PASS (the DTOs are
  unchanged).
- The existing integration tests in
  `MdmUomFacts / MdmItemCategoryAndItemFacts /
  MdmBusinessPartnerWarehouseLocationFacts` continue to PASS
  (they use spec-compliant codes; the new validator accepts
  them).
- The `MdmServiceBoundaryArchitectureTests` continues to
  PASS (the service boundary is unchanged; the validator is
  an internal collaborator).

### 6.4 Total new tests

- `MasterDataCodeValidatorTests`: **48** (unit)
- `MdmEntityCodeValidationTests`: **39** (unit, mocked)
- **Total: 87 new tests.** Coverage: `Mdm.Tests` goes from
  65/67 to ~150/152 (the 2 inherited flaky tests remain).

### 6.5 No frontend tests

The Vue pages' error display is manual-verified (the
`ProblemDetails` 400 response carries the `MdmErrorCode`
machine code; the page surfaces the `title` / `detail` field).
A frontend test framework is its own WorkItem (V1.5+).

---

## 7. BusinessPartner / Item / Warehouse examples

### 7.1 BusinessPartner

#### 7.1.1 Where it lives

- **Entity:** `modules/mdm/GuliERP.Mdm.Domain/Entities/BusinessPartner.cs`
- **Service:** `modules/mdm/GuliERP.Mdm.Application/IMdmMasterData002Services.cs` → `IMdmBusinessPartnerService`
- **Service implementation:** `modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmMasterData002Services.cs`
- **DTOs:** `modules/mdm/GuliERP.Mdm.Application/MdmDtos.cs` (already
  has `BusinessPartnerDto / CreateBusinessPartnerRequest /
  UpdateBusinessPartnerRequest`)
- **API:** `apps/api/.../Mdm/MdmEndpoints.cs` (already wired)
- **Page:** `apps/web/src/views/mdm/BusinessPartnerList.vue`
- **Tests:** `tests/GuliERP.Mdm.IntegrationTests/MdmBusinessPartnerWarehouseLocationFacts.cs`

#### 7.1.2 The integration

In `MdmMasterData002Services.CreateBusinessPartnerAsync`
(or whatever the implementation method is named), add the
validator call:

```csharp
public async Task<BusinessPartnerDto> CreateAsync(
    CreateBusinessPartnerRequest request, CancellationToken ct)
{
    ArgumentNullException.ThrowIfNull(request);

    // 1. Canonicalize
    var code = (request.Code ?? "").Trim().ToUpperInvariant();
    var name = (request.Name ?? "").Trim();
    if (string.IsNullOrEmpty(name))
        throw new MdmValidationException(
            MdmErrorCodes.BusinessPartnerNameRequired, "...");

    // 2. NEW: 4-step code validation
    var codeContext = new MasterDataCodeValidationContext(
        code,
        MasterDataCodeType.BusinessPartnerCode,
        TenantId: _currentTenant.Id ?? 0L,
        CompanyId: null,  // BP is Tenant-scoped, not Company-scoped
        EntityIdForUpdate: null);
    var codeResult = _codeValidator.Validate(codeContext);
    if (!codeResult.IsValid)
    {
        throw new MdmValidationException(
            codeResult.Failure!.ErrorCode,
            codeResult.Failure.Message,
            codeResult.Failure.Detail);
    }

    // 3. Uniqueness (existing)
    var dupe = await _db.BusinessPartners
        .AnyAsync(x => x.TenantId == _currentTenant.Id
                    && x.Code == code, ct);
    if (dupe)
        throw new MdmValidationException(
            MdmErrorCodes.BusinessPartnerDuplicateCode,
            $"业务伙伴代码 {code} 已存在。");

    // 4. create + save + return (existing)
}
```

#### 7.1.3 Example inputs that pass / fail

| `request.Code` (operator) | Canonical | Step 1 | Step 2 | Step 4 | Result     |
|--------------------------|-----------|--------|--------|--------|------------|
| `C-001`                  | `C-001`   | Ok     | Ok     | Ok     | **Ok** → create |
| `c-001`                  | `C-001`   | Ok     | Ok     | Ok     | **Ok** → create (lowercase normalized) |
| `lowercase`              | `LOWERCASE` | **Fail** | —      | —      | `InvalidCodeFormat` |
| `SYSTEM`                 | `SYSTEM`  | Ok     | **Fail** | —     | `ReservedCode` |
| `20240101`               | `20240101` | Ok    | Ok     | **Fail** | `ResemblesDocumentNumber` |
| `SO-C-001`               | `SO-C-001` | Ok    | Ok     | **Fail** | `ResemblesDocumentNumber` |
| `C-001` (duplicate)      | `C-001`   | Ok     | Ok     | Ok     | **Fail** (Step 3 — uniqueness) `BusinessPartnerDuplicateCode` |
| (empty)                  | (empty)   | **Fail** | —     | —      | `CodeCannotBeEmpty` |

#### 7.1.4 The Vue page error display

The page already handles 400 from the API. The new
`MdmErrorCode`s carry a `title` ("代码格式不正确。") that
the form drawer shows in the alert. The page does not need
to change beyond reading the new code; the existing error
display works.

### 7.2 Item

#### 7.2.1 Where it lives

- **Entity:** `modules/mdm/GuliERP.Mdm.Domain/Entities/Item.cs`
- **Service:** `IMdmService` (the older MDM-001 service)
- **Service implementation:** `MdmService` in
  `modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmService.cs`
- **DTOs:** `MdmDtos.cs` (`ItemDto / CreateItemRequest / UpdateItemRequest`)
- **API + page + tests:** same as BusinessPartner

#### 7.2.2 The integration (different from BP because it's in `MdmService`, not `MdmMasterData002Services`)

The `MdmService` class is the older one (MDM-001). It needs
the same wiring. The pattern is identical to §7.1.2:

```csharp
public async Task<ItemDto> CreateItemAsync(
    CreateItemRequest request, CancellationToken ct)
{
    ArgumentNullException.ThrowIfNull(request);

    var code = (request.Code ?? "").Trim().ToUpperInvariant();
    var name = (request.Name ?? "").Trim();
    if (string.IsNullOrEmpty(name))
        throw new MdmValidationException(
            MdmErrorCodes.ItemNameRequired, "...");

    // 2. NEW: 4-step code validation
    var codeContext = new MasterDataCodeValidationContext(
        code,
        MasterDataCodeType.ItemCode,
        TenantId: _currentTenant.Id ?? 0L,
        CompanyId: null,  // Item is Tenant-scoped
        EntityIdForUpdate: null);
    var codeResult = _codeValidator.Validate(codeContext);
    if (!codeResult.IsValid)
    {
        throw new MdmValidationException(
            codeResult.Failure!.ErrorCode,
            codeResult.Failure.Message,
            codeResult.Failure.Detail);
    }

    // 3. Uniqueness (existing) + Item-specific checks
    //    (BaseUomId exists, optional CategoryId in same tenant, etc.)
    // 4. create + save + return (existing)
}
```

#### 7.2.3 Example inputs

| `request.Code`  | Canonical       | Result                    |
|-----------------|-----------------|---------------------------|
| `MAT-STEEL-A36` | `MAT-STEEL-A36` | **Fail** Step 1 (hyphen not allowed) — but the canonicalize step turns the hyphen into... NO. Canonicalize only trims + uppers. Hyphen stays. So Step 1 fails. → `InvalidCodeFormat`. The operator sees: "代码必须以大写字母开头,仅含大写字母 / 数字 / 下划线。" |
| `MAT_STEEL_A36` | `MAT_STEEL_A36` | **Ok** (underscore)         |
| `MAT-20240101`  | `MAT-20240101`  | **Fail** Step 4 (8 digits) |
| `MAT-2024`      | `MAT-2024`      | **Ok** (only 4 digits)     |
| `PO-ITEM-001`   | `PO-ITEM-001`   | **Fail** Step 4 (doc prefix `PO`) |
| `MAT-PROD`      | `MAT-PROD`      | **Ok** (no reserved, no date, no doc prefix) |

#### 7.2.4 Special case: `MAT-2024-A` (8 digits in middle)

Per the `DateInCodePattern` regex `\d{8}`, **any** 8
consecutive digits fail. So `MAT-20240101-A` (date in the
middle) fails Step 4. The operator sees: "代码包含 8 位
连续数字,与单据号格式冲突。"

This is intentional. An Item code is supposed to be a stable
identifier; embedding a date in it would create year-based
"archives" that should not exist in master data.

### 7.3 Warehouse

#### 7.3.1 Where it lives

- **Entity:** `modules/mdm/GuliERP.Mdm.Domain/Entities/Warehouse.cs`
- **Service:** `IMdmMasterData002Services.IMdmWarehouseService`
- **Service implementation:** `MdmMasterData002Services.cs` (same
  file as BP, sibling method)
- **DTOs:** `MdmDtos.cs` (`WarehouseDto / CreateWarehouseRequest / UpdateWarehouseRequest`)
- **API + page + tests:** same as BP

#### 7.3.2 The integration

Warehouse is **company-scoped** (per V1 model §5.1). The
context must include `CompanyId`:

```csharp
public async Task<WarehouseDto> CreateAsync(
    CreateWarehouseRequest request, CancellationToken ct)
{
    ArgumentNullException.ThrowIfNull(request);

    var code = (request.Code ?? "").Trim().ToUpperInvariant();
    var name = (request.Name ?? "").Trim();
    if (string.IsNullOrEmpty(name))
        throw new MdmValidationException(
            MdmErrorCodes.WarehouseNameRequired, "...");

    // 2. NEW: 4-step code validation
    var codeContext = new MasterDataCodeValidationContext(
        code,
        MasterDataCodeType.WarehouseCode,
        TenantId: _currentTenant.Id ?? 0L,
        CompanyId: _currentCompany.Id,  // Warehouse IS company-scoped
        EntityIdForUpdate: null);
    var codeResult = _codeValidator.Validate(codeContext);
    if (!codeResult.IsValid)
    {
        throw new MdmValidationException(
            codeResult.Failure!.ErrorCode,
            codeResult.Failure.Message,
            codeResult.Failure.Detail);
    }

    // 3. Uniqueness (existing — scoped to (TenantId, CompanyId, Code))
    // 4. create + save + return (existing)
}
```

#### 7.3.3 Example inputs

| `request.Code`    | Canonical        | Result     |
|-------------------|------------------|------------|
| `WH-NORTH`        | `WH-NORTH`       | **Ok**     |
| `WH-DEFAULT`      | `WH-DEFAULT`     | **Fail** Step 2 (reserved) |
| `WH-2024`         | `WH-2024`        | **Ok** (4 digits) |
| `WH-20240101`     | `WH-20240101`    | **Fail** Step 4 (8 digits) |
| `GR-WH-001`       | `GR-WH-001`      | **Fail** Step 4 (doc prefix `GR`) |
| `WH-PROD`         | `WH-PROD`        | **Ok**     |
| `WH-NORTH` (duplicate within same Company) | `WH-NORTH` | **Fail** Step 3 (uniqueness) |

---

## 8. Backward compatibility (the "grandfather" question)

### 8.1 The risk

The current GuliERP production data may have codes that
violate the new rules:

- Lowercase codes (e.g. `customer-001` instead of `CUSTOMER-001`):
  lowercase is normalized to upper by the existing App service
  canonicalization. So the validation sees `CUSTOMER-001`,
  which passes.
- Hyphen-containing codes (e.g. `MAT-STEEL-A36`): the operator
  typed a hyphen, the App service did NOT canonicalize the
  hyphen to underscore (it does trim + upper only). So the
  validator fails with `InvalidCodeFormat`. **Existing data
  with hyphens would fail the new validation on the next
  Update.** This is a real risk.
- 8-digit codes: any code with a date pattern fails Step 4.
- Doc-prefix codes: any code starting with `SO/PO/...` fails
  Step 4.

### 8.2 The mitigation

Per `GULIERP_MDM_IMPLEMENTATION_PLAN_001` §4.4 (P0-1
acceptance): **the validation fires on Create / Update only,
not on Read.** Existing data is untouched. A "cleanup
migration" is its own WorkItem; that migration deprecates
bad codes (e.g. translates hyphens to underscores) but does
not change the V1 contract.

### 8.3 The deployment plan

The future implementation PR (when this design is implemented)
must ship:

- The validator (this design).
- The new error codes.
- The unit tests (§6).
- A "tenant data audit" report (a one-time script that scans
  every tenant for bad codes; the report is attached to the
  PR as evidence of grandfather policy).
- A "cleanup migration" deferred WorkItem (the design, not
  the implementation).

### 8.4 What this design does NOT do

This design does NOT propose a tenant data cleanup. It is the
**rule** + the **validator** + the **error codes**. The
cleanup is its own future WorkItem.

---

## 9. Acceptance criteria (Phase 2 — the future code PR)

The Phase 1 design is complete. The future code WorkItem
opens a new design goal. Its acceptance criteria are:

1. **The validator exists** in
   `GuliERP.Mdm.Application.MasterDataCodeValidator.cs`,
   implements `IMasterDataCodeValidator`, registered in DI.
2. **The 4 error codes** are added to `MdmErrorCodes.cs`.
3. **All 6 MDM App services** (`MdmService` Uom +
   ItemCategory + Item; `MdmMasterData002Services` BP +
   Warehouse + Location) call the validator on every
   `Create*Async` and `Update*Async`.
4. **Canonicalization** is done by the App service (trim +
   upper) BEFORE the validator is called.
5. **The 87 new unit tests** PASS.
6. **All existing tests** (65/67 unit + 6 integration +
   shared metadata) still PASS.
7. **No new DB migration** (no schema change; no data
   backfill).
8. **The Vue pages** display the new error messages
   (`mdm_code_format_invalid` / `mdm_code_reserved` /
   `mdm_code_resembles_document_number`).
9. **A `GULIERP_MDM_001_CODE_PIPELINE_REPORT.md`** closes
   the goal.

---

## 10. One-line summary

The unified master-data code validation pipeline is
designed: 4 frozen steps (format / reserved / uniqueness /
no-doc-number-pattern), 1 validator (`IMasterDataCodeValidator`
+ `MasterDataCodeValidator`) in `GuliERP.Mdm.Application`,
3 new error codes (`mdm_code_format_invalid` /
`mdm_code_reserved` / `mdm_code_resembles_document_number`)
in `MdmErrorCodes.cs`, 6 App services wired to call the
validator as the first line of business logic on every
Create / Update, 87 new unit tests planned, 0 DB
migrations, 0 schema changes. Step 1, 2, 4 (the missing
3 steps) are added. Step 3 (uniqueness) stays the DB's job.
Existing data is grandfathered; a future cleanup migration
WorkItem handles bad codes. The implementation lands in
the next `GULIERP_MDM_001_CODE_PIPELINE` design goal.
