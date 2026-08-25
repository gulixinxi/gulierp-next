# GULIERP_CONTACT_PROFILE_001_DESIGN_REPORT

> Goal: design the V1 Contact Profile capability for GuliERP —
> a unified contact storage that supports BOTH `Employee`
> (internal people) and `BusinessPartner` (external
> counterparties) without duplicating schema. This is a
> **design freeze** — no code, no entity, no database, no
> migration, no API, no Vue changes ship with this PR. The
> output is the contract that the implementation milestone(s)
> build on.
> Authority: `docs/architecture/MDM_000_MASTER_DATA_CONVENTION_V1.md` +
> `docs/governance/GULIERP_MODULE_INDEPENDENCE_RULE.md` +
> `docs/governance/META_GULI_GOVERNANCE_V1.md` +
> `docs/business/GULIERP_MASTER_DATA_MODEL_V1.md` (FROZEN) +
> `docs/business/GULIERP_CODE_RULE_STANDARD_V1.md` (FROZEN) +
> `docs/business/GULIERP_EMPLOYEE_MASTER_MODEL_V1.md` (FROZEN) +
> `docs/business/GULIERP_EMPLOYEE_MASTER_001_DESIGN_REPORT.md` (FROZEN) +
> `docs/business/GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md` (FROZEN) +
> `docs/business/GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE_IMPLEMENTATION_REPORT.md` (COMPLETE).

Date: 2026-08-24
Status: **CONTACT_PROFILE_001_DESIGN_REPORT_FROZEN**

---

## 0. One-line summary

V1 ships a single `ContactProfile` table with a polymorphic
`OwnerType` (Employee | BusinessPartner) + `OwnerId` FK,
supporting **1:0..1** cardinality for `Employee` and **1:N**
cardinality for `BusinessPartner`. Employee V1 stays untouched
(no Mobile/Email/WeChat fields on the Employee row). QR codes
are **dynamically generated** from a `QRCodePayload` JSON
record (no image blob storage). The capability lives in a
new `modules/contact/` module so both Identity and MDM can
consume it without violating `GULIERP_MODULE_INDEPENDENCE_RULE`.

---

## 1. Background analysis

### 1.1 The current state

GuliERP V1 has 2 owner types that need contact information:

1. **`Employee`** (Identity module) — internal people
   (salesperson, buyer, warehouse keeper, accountant). Per
   `GULIERP_EMPLOYEE_MASTER_MODEL_V1.md` §5, V1 has **no**
   contact fields on the `Employee` row. The `GuliErpUser`
   (sibling table) carries `Email` + `PhoneNumber` (inherited
   from `IdentityUser<long>`).

2. **`BusinessPartner`** (MDM module) — external
   counterparties (customer / supplier / both). Per
   `GULIERP_MASTER_DATA_MODEL_V1.md` §3.1, V1 has a **single**
   `ContactPerson / Phone / Email` triple on the
   `BusinessPartner` row (multi-contact is V2+ per the
   frozen model).

### 1.2 The duplication problem (the brief's premise)

If V1.5+ ships contact features per owner type:

- `EmployeeContact` (Identity) — V1.5+ adds Mobile / Email /
  WeChat / WeCom for the salesperson's customer engagement.
- `CustomerContact` (MDM) — V1.5+ adds multi-contact for the
  customer company (王总 / 采购经理 / 财务).
- `SupplierContact` (MDM) — same as Customer, for the
  supplier company.

Three tables, same shape, same `Mobile / Email / WeChat /
WeCom / QRCode` columns. This is a textbook V1.5+ duplication
trap.

### 1.3 The unified model: a single `ContactProfile` table

A single `ContactProfile` table that is **polymorphically
attached** to either an Employee or a BusinessPartner
(via `OwnerType + OwnerId`). The Employee and BusinessPartner
rows stay unchanged; the contact information lives in a
separate, shared table.

This is the same pattern as:
- SAP's "Party" concept (Party is the polymorphic owner; Person
  and Organization are specializations). GuliERP's V1
  simplification is `Employee + BusinessPartner + ContactProfile`
  (no Party supertype).
- Odoo / Dynamics 365's "res.partner" polymorphic address
  book (every address book entry attaches to a Partner
  supertype).
- The polymorphic `OwnerId` pattern in a single-table
  inheritance system.

---

## 2. ERP vs CRM boundary (the hard "no")

The brief is **explicit**: this design is ERP master data,
NOT CRM. The forbidden CRM scope (per the brief):

- 客户跟进 (customer follow-up)
- 商机 (opportunities)
- 销售漏斗 (sales funnel)
- 拜访记录 (visit records)
- 聊天记录 (chat records)
- 营销活动 (marketing campaigns)
- 客户生命周期 (customer lifecycle)

### 2.1 What the V1 Contact Profile is

A **passive** address book entry. It holds:
- The contact's name + role + contact channels.
- A polymorphic owner pointer (which Employee / BusinessPartner
  this contact belongs to).
- A status (Active / Inactive).
- A dynamic QR code generator (for WeChat / WeCom / Email /
  Phone).
- A `IsPrimary` flag (V1 only meaningful for Employee; V1.5+
  extends to 1:N for Employee with multiple primary candidates).

### 2.2 What the V1 Contact Profile is NOT

- **Not a customer engagement record.** No last-contacted-at,
  no follow-up cadence, no engagement score, no lead source,
  no campaign attribution, no lifecycle stage.
- **Not a CRM contact.** No sales pipeline link, no
  opportunity link, no visit log, no chat history, no email
  blast unsubscribe token.
- **Not a chat participant.** No WeChat / WeCom **chat
  history** is stored; only the WeChat / WeCom **identifier**
  (the wxid / userid) is stored, so a future CRM module can
  USE the identifier to start a chat (out of scope).
- **Not an audit log.** The audit trail is the standard
  `CreatedAt / CreatedBy / ModifiedAt / ModifiedBy /
  ConcurrencyVersion` cross-cutting contract (per
  `GULIERP_MASTER_DATA_MODEL_V1.md` §8). No contact-event
  log.

### 2.3 Future CRM scope (V2+ / V3+)

When a future CRM module ships (out of V1 scope), it will
build **on top of** the ContactProfile:
- `CrmContactEngagement` — last contacted, follow-up cadence
  (V2+).
- `CrmOpportunity` — sales pipeline, linked to
  `BusinessPartner` + the `ContactProfile` row of the
  decision-maker (V2+).
- `CrmVisit` — field visit log, linked to
  `BusinessPartner` + `ContactProfile` (V2+).
- `CrmChatThread` — WeChat / WeCom chat history archive
  (V2+; the **identifier** is in V1 ContactProfile, the
  **history** is V2+).
- `CrmLifecycleStage` — Lead / Opportunity / Customer /
  Churned stages (V2+).

The V1 `ContactProfile` is the foundation these CRM tables
attach to. V1 is a clean, passive address book; CRM is the
engagement layer on top.

---

## 3. Contact Profile V1 model

### 3.1 Entity fields (V1 frozen)

| Field | Type | Required | Default | Notes |
|-------|------|----------|---------|-------|
| `Id` | `long` (HiLo) | system | HiLo | `gulierp_hilo_sequence` |
| `TenantId` | `long` | system | from `ICurrentTenant` | `IMultiTenant` |
| `CompanyId` | `long` | system | from `ICurrentCompany` | `ICompanyScoped` |
| `OwnerType` | `ContactProfileOwnerType` (enum) | **required** | n/a | `Employee` \| `BusinessPartner` (V1 frozen 2-value enum) |
| `OwnerId` | `long` | **required** | n/a | Polymorphic FK to `Employee.Id` or `BusinessPartner.Id` (no DB-level FK constraint; application-level check) |
| `Name` | `string` | **required** | n/a | 1..200 chars; trim before persist |
| `Mobile` | `string?` | optional | null | E.164 format (`+86 138 0013 8000`); max 32 chars; trim spaces |
| `Phone` | `string?` | optional | null | Landline (`010-1234 5678`); max 32 chars; **V2 deferred per the brief** (see §3.3) |
| `Email` | `string?` | optional | null | RFC 5322; max 200 chars; lowercased before persist |
| `WeChatId` | `string?` | optional | null | The WeChat `wxid_xxxx` identifier; max 64 chars |
| `WeComId` | `string?` | optional | null | The Enterprise WeChat `userid`; max 64 chars |
| `Position` | `string?` | optional | null | Display role at the owner (e.g. "采购经理", "财务联系人"); max 100 chars |
| `Remark` | `string?` | optional | null | Free text; max 500 chars |
| `IsPrimary` | `bool` | optional | `false` | V1: only one ContactProfile per Employee is primary (the rest are Inactive or `IsPrimary = false`); V1.5+ extends to multi-primary with explicit ordering |
| `Status` | `ContactProfileStatus` (enum) | optional | `Active` | `Active` \| `Inactive` (V1 frozen 2-value enum; the cross-cutting `MasterDataStatus` pattern) |
| `CreatedAt` | `DateTimeOffset` | system | now (UTC) | audit |
| `CreatedBy` | `long?` | system | `ICurrentUser.Id` | audit (nullable for system-seeded rows) |
| `ModifiedAt` | `DateTimeOffset` | system | now (UTC) | audit; bumped on every update |
| `ModifiedBy` | `long?` | system | `ICurrentUser.Id` | audit |
| `ConcurrencyVersion` | `int` | system | 1 | EF Core optimistic concurrency; bumped on every update |

### 3.2 The polymorphic FK pattern (V1)

The `OwnerType + OwnerId` combination is a classic polymorphic
FK. The trade-offs:

| Approach | Pro | Con |
|----------|-----|-----|
| **Polymorphic FK (V1 chosen)** | 1 table, 1 service, 1 query path; clean V1 | No DB-level FK constraint; must enforce at app layer |
| 2 tables (one per owner) | Type-safe FK | Duplicated schema; 2 services; 2 query paths |
| Party supertype | Type-safe FK; extensible to N owner types | Requires retrofit of Employee + BusinessPartner to inherit from Party; bigger migration |

V1 ships the **polymorphic FK** because:
- It minimizes V1 surface (1 table, 1 service, 1 query path).
- The cross-table integrity is enforced at the **application
  layer** (the App service validates that `OwnerId` exists in
  the right table; the API endpoint's 404/400 response is the
  V1 contract).
- The V1 cardinality is bounded (2 owner types); the polymorphic
  FK is safe.
- The polymorphic FK is **forward-compatible** with V1.5+ if
  a future design goal adds a 3rd owner type (e.g., External
  Consultant) — the discriminator pattern is already in
  place.

The V1 EF Core configuration locks the polymorphic FK with:
- A regular `HasIndex` on `(OwnerType, OwnerId)` for query
  performance.
- A **partial unique index** on `(OwnerType, OwnerId)` filtered
  by `OwnerType = 'Employee'` — this enforces the **1:0..1
  cardinality** for Employee at the DB level.
- A **check constraint** `OwnerId > 0` (system-wide invariant).
- No DB-level FK on `OwnerId` (because a single column cannot
  FK to 2 different tables in PostgreSQL). The application
  layer validates the FK existence in the App service.

### 3.3 Field V1 vs V2 classification

| Field | V1? | Reason |
|-------|-----|--------|
| `Id` | V1 | System ID |
| `TenantId`, `CompanyId` | V1 | `ICompanyScoped` scope |
| `OwnerType` | V1 | Discriminator |
| `OwnerId` | V1 | FK to Employee / BusinessPartner |
| `Name` | V1 | Display name (required) |
| `Mobile` | V1 | Most common contact channel |
| `Email` | V1 | Universal contact channel |
| `WeChatId` | V1 | Common in CN; needed for WeChat QR code generation |
| `WeComId` | V1 | Enterprise WeChat (B2B) |
| `QRCodePayload` | V1 | **JSON record (not blob)** — the page calls `GET /contact-profiles/{id}/qrcode?type=...&size=...` and the server generates a PNG on demand |
| `Position` | V1 | Display role (e.g. "采购经理") |
| `Remark` | V1 | Free text |
| `IsPrimary` | V1 | Flag for V1.5+ 1:N extension |
| `Status` | V1 | Lifecycle (Active / Inactive) |
| `CreatedAt` / `CreatedBy` / `ModifiedAt` / `ModifiedBy` / `ConcurrencyVersion` | V1 | Cross-cutting contract |
| `Phone` (landline) | **V2** | Less common; brief explicitly defers it |
| `AvatarUrl` | **V2** | Image storage / CDN |
| `Tags` (string[] or jsonb) | **V2** | CRM-style tagging; not in V1 (no CRM scope) |
| `Source` (enum: Manual / Wechat / Wecom) | **V2** | CRM attribution (forbidden scope) |
| `LastContactedAt` / `LastContactedBy` | **V2** | CRM engagement (forbidden scope) |
| `Gender` / `Birthday` / `Nationality` | **V2** | HR / privacy; deferred per `GULIERP_EMPLOYEE_MASTER_MODEL_V1.md` §5.2 |
| `WeChat / WeCom chat thread / history` | **V2** | CRM (forbidden scope) |
| `Engagement score` / `Lifecycle stage` | **V2** | CRM (forbidden scope) |
| `Multi-language name` (e.g. 中文 + English) | **V2** | Per `GULIERP_MASTER_DATA_MODEL_V1.md` deferred |

### 3.4 The polymorphic FK check (V1)

The App service `IContactProfileService` enforces:

```csharp
private async Task EnsureOwnerExistsAsync(
    ContactProfileOwnerType ownerType, long ownerId, CancellationToken ct)
{
    switch (ownerType)
    {
        case ContactProfileOwnerType.Employee:
            var emp = await _employeeRepository.GetByIdAsync(ownerId, ct);
            if (emp is null) throw new ContactProfileValidationException(
                IdentityErrorCodes.EmployeeNotFound, ...);
            // also check TenantId + CompanyId match
            break;
        case ContactProfileOwnerType.BusinessPartner:
            var bp = await _bpRepository.GetByIdAsync(ownerId, ct);
            if (bp is null) throw new ContactProfileValidationException(
                MdmErrorCodes.BusinessPartnerNotFound, ...);
            // TenantId must match (BP is IMultiTenant only)
            break;
        default:
            throw new ContactProfileValidationException(
                ContactErrorCodes.InvalidOwnerType, ...);
    }
}
```

The V1 `OwnerType` enum is **2-value** (Employee, BusinessPartner).
A V1.5+ "External Consultant" would add a 3rd value + extend
the switch. The V1 design is forward-compatible.

### 3.5 The 1:0..1 cardinality for Employee (V1)

The V1 cardinality for Employee is **1:0..1**: an Employee
can have at most one ContactProfile (because in V1, we model
"the employee's primary contact channels"; V1.5+ will extend
to 1:N for "personal mobile + work mobile + personal email +
work email + ...").

Enforced at the DB level by a partial unique index:

```sql
CREATE UNIQUE INDEX ux_gulierp_contact_employee
    ON gulierp_contact_profile (OwnerId)
    WHERE OwnerType = 'Employee';
```

This index is **partial** (filtered by `OwnerType = 'Employee'`)
and does NOT affect BusinessPartner rows (where 1:N is the
natural cardinality).

In the EF Core configuration, this is expressed as:

```csharp
b.HasIndex(c => c.OwnerId)
    .IsUnique()
    .HasFilter("\"OwnerType\" = 'Employee'")
    .HasDatabaseName("ux_gulierp_contact_employee");
```

### 3.6 The 1:N cardinality for BusinessPartner (V1)

The V1 cardinality for BusinessPartner is **1:N**: a
BusinessPartner can have 0..N ContactProfiles (王总 / 采购经理
/ 财务联系人). The existing `BusinessPartner.ContactPerson`
field stays for backward compat (V1 wire contract) — the
ContactProfile table is the new authoritative source, and the
V1.5+ migration will deprecate the inline `ContactPerson` field
(per the brief's "不要进入 CRM 范围" rule, the deprecation is
NOT in this PR).

Enforced at the DB level by **no** unique constraint on
`(OwnerType, OwnerId)` for `OwnerType = 'BusinessPartner'`. A
regular non-unique index on `(OwnerType, OwnerId)` exists for
query performance.

### 3.7 The `IsPrimary` flag (V1)

The V1 `IsPrimary` flag has 2 meanings:

- **For Employee (1:0..1):** every ContactProfile row for an
  Employee is effectively "primary" (there's only one). The
  flag is a no-op for Employee in V1; it's there to support
  the V1.5+ transition to 1:N for Employee.
- **For BusinessPartner (1:N):** the row with `IsPrimary = true`
  is the "default" contact for that BusinessPartner (used when
  a document needs to reference a contact but the user did
  not pick one). The V1 read endpoints sort by
  `(IsPrimary DESC, Name ASC)` so the primary row is the
  first in the list.

V1.5+ will add a "primary selection" workflow (the page lets
the operator click a star icon to set the primary row).

---

## 4. Employee ↔ ContactProfile relationship

### 4.1 Decision: **1:0..1 (V1)**, 1:N (V1.5+)

The brief explicitly asks: "是一对一？还是一对多？考虑：一个员工可能：一个工作手机号，一个微信，一个企业微信。是否支持多个联系方式留给 V2。"

**Decision:** V1 is **1:0..1** (an Employee has at most one
ContactProfile). V1.5+ extends to **1:N** (an Employee can
have multiple ContactProfiles with different `Position` values:
"工作手机号" / "个人手机号" / "工作微信" / "个人微信" / "工作邮箱"
/ "个人邮箱").

The reasoning:

- V1's `Employee` entity is intentionally minimal (no Mobile /
  Email / WeChat fields, per the design freeze in
  `GULIERP_EMPLOYEE_MASTER_MODEL_V1.md` §5.2). The first
  contact channel for an Employee is the `GuliErpUser.Email` +
  `GuliErpUser.PhoneNumber` (inherited from `IdentityUser<long>`).
- Adding **one** ContactProfile per Employee is a reasonable V1
  addition (covers the WeChat / WeCom / non-auth-email gap).
- Adding **multiple** ContactProfiles per Employee in V1 is
  premature (the Employee write surface is not yet shipped;
  the use cases for "personal mobile vs work mobile" are not
  yet defined).
- The 1:0..1 cardinality is enforced at the DB level by the
  partial unique index.
- The 1:N extension in V1.5+ requires **no** schema change
  (the polymorphic FK is already there; the partial unique
  index is dropped, the V1.5+ page adds the "add contact"
  workflow).

### 4.2 The "do not add contact fields to Employee" rule

Per the brief: "Employee V1: 只负责：员工身份、组织关系、User绑定。不要加入：CRM字段。"

**Decision:** V1 does NOT add `Mobile / Email / WeChat / WeCom`
fields to the `Employee` entity. The `Employee` row stays
minimal (per the design freeze). The contact information lives
in the `ContactProfile` table.

The `GuliErpUser` row (sibling of `Employee`) continues to
carry the auth-channel `Email` + `PhoneNumber` (inherited from
`IdentityUser<long>`). The future "Employee + User + Contact"
3-way link is:

- `Employee.UserId → GuliErpUser` (the auth link; V1).
- `Employee → ContactProfile` (1:0..1, via polymorphic FK;
  V1.5+ extends to 1:N).
- `GuliErpUser` is the auth-channel identity (Email,
  PhoneNumber, 2FA).
- `ContactProfile` is the "channels this person is reachable
  on" (Mobile, WeChat, WeCom, Position, QRCode).

### 4.3 The "what's the contact for an Employee who has no ContactProfile" case

In V1, an Employee without a ContactProfile has NO WeChat /
WeCom / Mobile / Email-in-addition-to-auth. The page shows
"暂无联系方式" with an "新增" button (gated by
`contact.profile.manage`).

If the user wants to see the Employee's auth-channel email /
phone, they read the linked `GuliErpUser` row. This is a
read-only cross-reference (the Employee write surface does NOT
touch `GuliErpUser.Email` / `PhoneNumber` per
`GULIERP_EMPLOYEE_MASTER_MODEL_V1.md` §7.2).

---

## 5. BusinessPartner ↔ ContactProfile relationship

### 5.1 Decision: **1:N (V1)**

The brief explicitly asks for 1:N with the example
"山东XX机械有限公司 → 王总 / 采购经理 / 财务联系人".

**Decision:** V1 supports 1:N cardinality for BusinessPartner.
A BusinessPartner can have 0..N ContactProfiles. Each row
represents a specific contact person (王总, 采购经理, 财务
联系人). The `IsPrimary` flag marks the default contact (used
when a document needs to pick a contact but the user did not
pick one explicitly).

### 5.2 The `BusinessPartner.ContactPerson` (existing field)

The existing `BusinessPartner.ContactPerson` field (single
string) stays in V1 for backward compat. The V1 wire contract
is unchanged. The new `ContactProfile` table is the
authoritative source for multi-contact support; the V1
ContactProfile list endpoint can return the same data shape
as the existing `EmployeeDirectoryEntryDto` (a list of
`ContactProfile` rows).

**V1.5+ migration plan (NOT in this PR):**
1. Deprecate `BusinessPartner.ContactPerson` (mark as
   "transitional, use ContactProfile for new code").
2. The `BusinessPartnerList.vue` shows the primary
   `ContactProfile` row's `Name` as the contact (in place of
   the deprecated `ContactPerson` field).
3. After 2 releases, remove the `BusinessPartner.ContactPerson`
   field.

### 5.3 The BusinessPartner write service ↔ ContactProfile

V1.5+ will add a contact-management section to the
`BusinessPartnerList.vue` page (similar to the future Employee
contact-management section). The V1 ContactProfile write
endpoints accept `OwnerType = BusinessPartner` + `OwnerId =
{BusinessPartner.Id}`. The endpoint is in the MDM module's
contact API (NOT in the Identity module's Employee API).

The Identity module's Employee contact endpoint accepts
`OwnerType = Employee` + `OwnerId = {Employee.Id}`. The endpoint
is in the Identity module's contact API (NOT in the MDM
module's contact API).

Both endpoints are backed by the SAME `IContactProfileService`
(per §3.2 polymorphic FK).

---

## 6. QR code design (no blob storage)

### 6.1 The principle: dynamic generation from payload

Per the brief: "二维码应该根据数据动态生成。" The DB stores
**only the payload** (the WeChat `wxid_xxxx` or the WeCom
`userid`), not the image. The image is generated on demand.

### 6.2 The `QRCodePayload` data model

```csharp
namespace GuliERP.Contact.Application.QrCode;

/// <summary>
/// A typed payload for QR code generation. The ContactProfile
/// row stores the <see cref="Type"/> + <see cref="Value"/>
/// tuple; the QR code image is generated on demand by
/// <see cref="IQRCodeService"/>.
/// </summary>
public sealed record QRCodePayload(
    QRCodeType Type,
    string Value,
    string? Label = null)
{
    /// <summary>Validate the payload. Throws on invalid.</summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Value))
            throw new ContactProfileValidationException(
                ContactErrorCodes.QRCodePayloadValueEmpty, ...);
        if (Value.Length > 4096)
            throw new ContactProfileValidationException(
                ContactErrorCodes.QRCodePayloadValueTooLong, ...);
    }

    /// <summary>Render the payload to a URL / string that
    /// gets encoded into the QR code image.</summary>
    public string ToQRContent() => Type switch
    {
        QRCodeType.WeChat => $"weixin://contact?username={Uri.EscapeDataString(Value)}",
        QRCodeType.WeCom => $"wecom://contacts/profile/{Uri.EscapeDataString(Value)}",
        QRCodeType.Email => $"mailto:{Uri.EscapeDataString(Value)}",
        QRCodeType.Phone => $"tel:{Uri.EscapeDataString(Value)}",
        QRCodeType.Url => Value,
        _ => throw new ContactProfileValidationException(
            ContactErrorCodes.InvalidQRCodeType, ...),
    };
}

public enum QRCodeType
{
    WeChat = 1,
    WeCom = 2,
    Email = 3,
    Phone = 4,
    Url = 5,
}
```

### 6.3 The `IQRCodeService` (Application interface)

```csharp
namespace GuliERP.Contact.Application.QrCode;

public interface IQRCodeService
{
    /// <summary>
    /// Generate a QR code PNG image for the given payload. The
    /// image is generated on demand; nothing is cached or
    /// stored.
    /// </summary>
    /// <param name="payload">The typed payload (WeChat wxid,
    /// WeCom userid, Email, Phone, or URL).</param>
    /// <param name="options">Image options (size, error
    /// correction level, foreground / background colors).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The PNG byte stream.</returns>
    Task<byte[]> GeneratePngAsync(
        QRCodePayload payload,
        QRCodeOptions options,
        CancellationToken ct = default);
}

public sealed record QRCodeOptions(
    int SizeInPixels = 256,                   // square edge length
    QRCodeErrorCorrection ErrorCorrection = QRCodeErrorCorrection.M,
    string ForegroundColor = "#000000",
    string BackgroundColor = "#FFFFFF")
{
    public void Validate()
    {
        if (SizeInPixels is < 64 or > 1024)
            throw new ContactProfileValidationException(
                ContactErrorCodes.QRCodeSizeOutOfRange, ...);
    }
}

public enum QRCodeErrorCorrection
{
    L = 1,  // ~7%
    M = 2,  // ~15% (default)
    Q = 3,  // ~25%
    H = 4,  // ~30%
}
```

### 6.4 The `QRCodeService` implementation

The implementation lives in
`modules/contact/GuliERP.Contact.Infrastructure/QrCode/QRCodeService.cs`.

**Library choice:** **QRCoder** (MIT license, ~500 KB, no
external network deps, pure C# .NET 10 compatible). It's the
most popular .NET QR code library (10k+ stars on GitHub, mature).

```csharp
using QRCoder;

namespace GuliERP.Contact.Infrastructure.QrCode;

public sealed class QRCodeService : IQRCodeService
{
    public Task<byte[]> GeneratePngAsync(
        QRCodePayload payload, QRCodeOptions options, CancellationToken ct)
    {
        payload.Validate();
        options.Validate();

        var content = payload.ToQRContent();
        using var generator = new QRCodeGenerator();
        using var qrData = generator.CreateQrCode(
            content, (QRCodeGenerator.ECCLevel)(int)options.ErrorCorrection);
        using var png = qrData.GetGraphic(
            pixelsPerModule: (int)Math.Ceiling(options.SizeInPixels / 41.0),
            darkColorHex: options.ForegroundColor,
            lightColorHex: options.BackgroundColor);
        return Task.FromResult(png.GetBytes());
    }
}
```

QRCoder is added to `modules/contact/GuliERP.Contact.Infrastructure/GuliERP.Contact.Infrastructure.csproj`
as a NuGet package reference (`QRCoder` version 1.6.0+, MIT
license, 100% offline, no CDN / network).

### 6.5 The DB does NOT store the image

The DB stores ONLY:
- `WeChatId: string?` (the wxid)
- `WeComId: string?` (the userid)
- `Email: string?` (the email address)
- `Mobile: string?` / `Phone: string?` (V2) (the phone number)

The QR code image is computed by `GET /api/v1/contact-profiles/{id}/qrcode?type=wechat&size=256`
on demand. The response is `Content-Type: image/png` with
the PNG bytes in the body. The page renders the image via
`<img :src="qrCodeUrl">` where `qrCodeUrl` is the API URL.

The image is also cacheable (HTTP `Cache-Control: max-age=3600`)
because the underlying `WeChatId` / `WeComId` is immutable
(the user can change the WeChat ID in V1, but the cached
image becomes stale on update; the V1 API endpoint adds a
`?v={ModifiedAt.Ticks}` query param to bust the cache on
update).

### 6.6 The `QRCodePayload` vs the `WeChatId` / `WeComId` columns

The `ContactProfile` row stores `WeChatId` + `WeComId` (the
raw identifiers). The `QRCodePayload` is a **typed** wrapper
that combines `QRCodeType` + `Value` for the API endpoint. The
API endpoint looks up the `WeChatId` from the row, then
constructs a `QRCodePayload(QRCodeType.WeChat, row.WeChatId)`
and renders.

The DB does NOT store a `QRCodePayload` column. The
`QRCodePayload` is purely a runtime concept (the API endpoint
constructs it from the row's columns on demand).

---

## 7. Module location and dependency direction

### 7.1 The module location decision

The Contact Profile capability is consumed by **2 modules**:
- **Identity** (for `Employee` contacts) — per
  `GULIERP_EMPLOYEE_MASTER_001_DESIGN_REPORT` §8.
- **MDM** (for `BusinessPartner` contacts) — per the
  existing `BusinessPartner` page.

Per `GULIERP_MODULE_INDEPENDENCE_RULE`:
- Identity cannot import MDM (`modules/identity/**` has zero
  references to `GuliERP.Mdm.*` per `git grep`).
- MDM cannot import Identity (same).
- Both can import Foundation (downward, allowed).

### 7.2 The chosen approach: **new `modules/contact/` module**

The cleanest V1 structure: a new shared module
`modules/contact/` with 3 projects:

```
modules/contact/
├── GuliERP.Contact.Domain/           (entity + enums + FK rules)
│   └── GuliERP.Contact.Domain.csproj
├── GuliERP.Contact.Application/      (interfaces + DTOs + validators)
│   └── GuliERP.Contact.Application.csproj
└── GuliERP.Contact.Infrastructure/   (EF Core + IQRCodeService impl + DI)
    └── GuliERP.Contact.Infrastructure.csproj
```

Dependency graph:

```
Foundation           ←─   (downward, allowed)
   ↑
   │
Contact             ←─   (downward, allowed)
   ↑
   ├── Identity       ←─  (downward, allowed)
   └── MDM            ←─  (downward, allowed)
```

- Foundation depends on: nothing.
- Contact depends on: Foundation.
- Identity depends on: Foundation, Contact.
- MDM depends on: Foundation, Contact.

No circular imports. The pattern is the same as the existing
`modules/document-kernel/` module (the shared Document kernel
used by Sales / Purchase / Inventory).

### 7.3 Why NOT Foundation directly?

Foundation is the **leaf kernel** (per
`GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md` §5.1). It
contains abstractions (`IMultiTenant`, `ICompanyScoped`,
`ICurrentUser`, `ICodeValidationContext`, etc.) and cross-
cutting error codes. It is **not** the place for domain
entities (which have FK relationships, EF Core mappings, etc.).

Putting `ContactProfile` directly in Foundation would:
- Expand Foundation's scope (from "kernel" to "kernel + domain").
- Make Foundation depend on EF Core for the entity (it
  already does, but only for the `FoundationDbContext`).
- Mix domain concepts with kernel abstractions.

The new `modules/contact/` module is the correct separation.

### 7.4 Why NOT MDM module alone?

Putting `ContactProfile` in MDM would work for BusinessPartner
(the existing MDM owner type) but would require Identity to
import MDM — which is forbidden.

### 7.5 Why NOT a polymorphic FK in Foundation?

Same as §7.3 — Foundation is for kernel abstractions, not
domain entities.

### 7.6 The `IContactProfileService` interface

The Application layer interface:

```csharp
namespace GuliERP.Contact.Application;

public interface IContactProfileService
{
    Task<ContactProfileDto> CreateAsync(
        CreateContactProfileRequest request, CancellationToken ct = default);

    Task<ContactProfileDto?> GetByIdAsync(
        long id, CancellationToken ct = default);

    Task<PagedResult<ContactProfileDto>> ListAsync(
        ContactProfileListQuery query, CancellationToken ct = default);

    Task<ContactProfileDto?> UpdateAsync(
        long id, UpdateContactProfileRequest request, CancellationToken ct = default);

    Task<ContactProfileDto> SetStatusAsync(
        long id, SetContactProfileStatusRequest request, CancellationToken ct = default);

    Task<byte[]> GenerateQRCodePngAsync(
        long id, QRCodeType type, QRCodeOptions options, CancellationToken ct = default);
}

public sealed record ContactProfileListQuery(
    ContactProfileOwnerType? OwnerType,
    long? OwnerId,
    ContactProfileStatus? Status,
    string? Keyword,
    int Page,
    int PageSize);
```

Both Identity and MDM import `GuliERP.Contact.Application`
(downward, allowed). The implementation lives in
`GuliERP.Contact.Infrastructure` (the EF Core + QRCoder glue).

---

## 8. Permission design

### 8.1 The decision: 2 new top-level permissions

The brief asks: "设计权限：例如：contact.profile.read / contact.profile.manage。分析：是否复用：identity.employee.read / identity.employee.manage。"

**Decision:** add **2 new top-level permissions** in
`GuliErpPermissions` (the Identity-side catalog — even though
the capability is shared, the catalog is the natural extension
point because Identity is the "auth" module):

- `GuliErpPermissions.ContactProfileRead = "contact.profile.read"`
- `GuliErpPermissions.ContactProfileManage = "contact.profile.manage"`

Plus the matching policies in `GuliErpAuthorizationPolicies`:
- `GuliErpAuthorizationPolicies.ContactProfileRead = Prefix + "contact.profile.read"`
- `GuliErpAuthorizationPolicies.ContactProfileManage = Prefix + "contact.profile.manage"`

The `contact.profile.*` permissions are **owner-agnostic** —
they authorize access to the `ContactProfile` entity itself,
regardless of whether the owner is an Employee or a
BusinessPartner. The owner-specific permissions (e.g.,
`identity.employee.read`, `mdm.business_partner.read`) are
**layered** on top:

| Endpoint | Required permissions |
|----------|---------------------|
| `GET /api/v1/contact-profiles?ownerType=Employee&ownerId=...` | `ContactProfileRead` AND `IdentityEmployeeRead` |
| `POST /api/v1/organization/employees/{id}/contacts` (or similar) | `ContactProfileManage` AND `IdentityEmployeeManage` |
| `GET /api/v1/contact-profiles?ownerType=BusinessPartner&ownerId=...` | `ContactProfileRead` AND `MdmBusinessPartnerRead` |
| `POST /api/v1/mdm/business-partners/{id}/contacts` (or similar) | `ContactProfileManage` AND `MdmBusinessPartnerManage` |
| `GET /api/v1/contact-profiles/{id}/qrcode?type=wechat` | `ContactProfileRead` (no owner check; the QR code is public to anyone with ContactProfileRead) |

The "AND" logic enforces the "see an Employee's contact only
if you can also see the Employee" rule. A user with only
`IdentityEmployeeRead` (but not `ContactProfileRead`) sees the
Employee but not the contact.

### 8.2 Why not reuse `identity.employee.*`?

The brief asks whether to reuse `identity.employee.read /
identity.employee.manage` for the contact capability. The
answer is **no, for 3 reasons**:

1. **The capability is owner-agnostic.** The same
   `ContactProfile` table serves BOTH Employee and
   BusinessPartner. A permission named `identity.employee.*`
   cannot be reused for BusinessPartner contacts.

2. **The owner check is layered.** An Employee contact read
   requires BOTH `IdentityEmployeeRead` AND `ContactProfileRead`.
   A single permission cannot express this layering cleanly.

3. **Forward-compatibility with new owner types.** V1.5+ may
   add an "External Consultant" owner type. The
   `contact.profile.*` permissions are reusable; the
   `identity.employee.*` permissions are not.

### 8.3 The `EnterpriseSystemAdminPermissions` update

The existing `EnterpriseSystemAdminPermissions` array in
`GuliErpPermissions` (the 8 permissions that the system admin
role gets by default) gets 2 new entries:

```csharp
public static readonly string[] EnterpriseSystemAdminPermissions =
{
    IdentityOrganizationRead,
    IdentityOrganizationManage,
    IdentityUserRead,
    IdentityUserManage,
    IdentityRoleRead,
    IdentityRoleAssign,
    IdentityCompanyRead,
    IdentityCompanySwitch,
    ContactProfileRead,    // NEW
    ContactProfileManage,  // NEW
};
```

A system admin can read + manage any ContactProfile by default.

---

## 9. API design (read-only, no implementation)

### 9.1 The 9 endpoints (frozen V1)

| # | Method | Route | Permission (primary) | Permission (layered) |
|---|--------|-------|----------------------|----------------------|
| 1 | POST | `/api/v1/contact-profiles` | `ContactProfileManage` | (depends on `OwnerType`) |
| 2 | GET | `/api/v1/contact-profiles/{id}` | `ContactProfileRead` | (depends on `OwnerType`) |
| 3 | PUT | `/api/v1/contact-profiles/{id}` | `ContactProfileManage` | (depends on `OwnerType`) |
| 4 | POST | `/api/v1/contact-profiles/{id}/status` | `ContactProfileManage` | (depends on `OwnerType`) |
| 5 | GET | `/api/v1/contact-profiles?ownerType=&ownerId=&status=&keyword=&page=&pageSize=` | `ContactProfileRead` | (depends on `OwnerType`) |
| 6 | GET | `/api/v1/contact-profiles/{id}/qrcode?type=wechat&size=256` | `ContactProfileRead` | (the QR code is public to any reader) |
| 7 | GET | `/api/v1/identity/employees/{employeeId}/contacts` (Employee contact list shortcut) | `ContactProfileRead` | `IdentityEmployeeRead` |
| 8 | POST | `/api/v1/identity/employees/{employeeId}/contacts` (Employee contact create shortcut) | `ContactProfileManage` | `IdentityEmployeeManage` |
| 9 | GET | `/api/v1/mdm/business-partners/{partnerId}/contacts` (BusinessPartner contact list shortcut) | `ContactProfileRead` | `MdmBusinessPartnerRead` |

Endpoints 7-9 are the "convenience" routes that map the
owner-typed URL to the polymorphic-typed contact endpoint.
The convenience routes are thin wrappers that:
1. Verify the caller has both `ContactProfileRead` (or
   `Manage`) AND the owner-specific read/manage permission.
2. Look up the owner (Employee or BusinessPartner) to get
   its `TenantId + CompanyId` for scope.
3. Call endpoint 5 with the `OwnerType + OwnerId` filter.

The convenience routes are owned by Identity (for #7, #8) and
MDM (for #9). The polymorphic endpoint (#5) is owned by the
Contact module.

### 9.2 The DTO contracts (frozen V1)

```csharp
// ContactProfileDto — the wire shape
public sealed record ContactProfileDto(
    long Id,
    long TenantId,
    long CompanyId,
    ContactProfileOwnerType OwnerType,
    long OwnerId,
    string Name,
    string? Mobile,
    string? Phone,                  // V2 deferred
    string? Email,
    string? WeChatId,
    string? WeComId,
    string? Position,
    string? Remark,
    bool IsPrimary,
    ContactProfileStatus Status,
    DateTimeOffset CreatedAt,
    long? CreatedBy,
    DateTimeOffset ModifiedAt,
    long? ModifiedBy,
    int ConcurrencyVersion);

// CreateContactProfileRequest
public sealed record CreateContactProfileRequest(
    ContactProfileOwnerType OwnerType,
    long OwnerId,
    string Name,
    string? Mobile,
    string? Phone,                  // V2 deferred
    string? Email,
    string? WeChatId,
    string? WeComId,
    string? Position,
    string? Remark,
    bool IsPrimary = false);

// UpdateContactProfileRequest
public sealed record UpdateContactProfileRequest(
    string Name,
    string? Mobile,
    string? Phone,                  // V2 deferred
    string? Email,
    string? WeChatId,
    string? WeComId,
    string? Position,
    string? Remark,
    bool IsPrimary,
    int ExpectedConcurrencyVersion);

// SetContactProfileStatusRequest
public sealed record SetContactProfileStatusRequest(
    ContactProfileStatus Status,
    int ExpectedConcurrencyVersion);
```

### 9.3 The new error codes (V1, frozen)

12 new error codes in a new `ContactErrorCodes` class
(located in `modules/contact/GuliERP.Contact.Application/ContactErrorCodes.cs`):

| Constant | Wire code | Thrown when |
|----------|-----------|-------------|
| `ContactProfileNotFound` | `contact_profile_not_found` | `ContactProfile.Id` does not exist |
| `ContactProfileCrossCompany` | `contact_profile_cross_company` | `ContactProfile.Id` exists but in a different Company (404 to avoid leaking existence) |
| `ContactProfileFormatInvalid` | `contact_profile_format_invalid` | Step 1 of the 4-step pipeline (format regex) — reuses Foundation's `FormatValidator` |
| `ContactProfileReserved` | `contact_profile_reserved` | Step 2 of the pipeline — reuses Foundation's `ReservedNameValidator` (the "EMP-SYSTEM" rule applies to ContactProfile.Name? see §9.4) |
| `ContactProfileResemblesDocumentNumber` | `contact_profile_resembles_document_number` | Step 4 of the pipeline — reuses Foundation's `DocumentNumberSimilarityValidator` |
| `ContactProfileDuplicate` | `contact_profile_duplicate` | Step 3 of the pipeline (uniqueness within owner scope) |
| `ContactProfileConcurrencyConflict` | `contact_profile_concurrency_conflict` | `ConcurrencyVersion` mismatch on Update / SetStatus |
| `InvalidOwnerType` | `contact_profile_invalid_owner_type` | The `OwnerType` enum value is unknown |
| `OwnerNotFound` | `contact_profile_owner_not_found` | The `OwnerId` does not exist in the corresponding table |
| `OwnerCrossTenant` | `contact_profile_owner_cross_tenant` | The owner exists but in a different Tenant |
| `QRCodeValueEmpty` | `contact_profile_qr_code_value_empty` | The `WeChatId` / `WeComId` is empty when a QR code is requested |
| `QRCodeSizeOutOfRange` | `contact_profile_qr_code_size_out_of_range` | The requested QR code size is < 64 or > 1024 |

### 9.4 The "Name field validation" decision (V1)

The brief lists `Name` as a V1 field for ContactProfile. The
`FormatValidator` (4-step pipeline) applies to **code fields**,
not **name fields**. The ContactProfile `Name` is a free-text
display name (not a code).

**Decision:** ContactProfile `Name` does NOT go through the
4-step code pipeline. The App service validates:
- Required (1..200 chars, trim before persist).
- No control characters (`\x00-\x1F`).
- The 4 reserved names from `GULIERP_CODE_RULE_STANDARD_V1.md`
  §7 (`SYSTEM / SYS / RESERVED / EMP-SYSTEM`) are also
  forbidden for `ContactProfile.Name` (the same reserved set
  + 1 more for Contact: `CONTACT-SYSTEM` — see the V1
  bootstrap row).

The 4-step pipeline (Format / Reserved / Uniqueness /
Doc-number) applies ONLY to the `ContactProfile.Code` field
(which is V2+, per §3.3). V1 has no `Code` field on
ContactProfile; the natural identifier is the polymorphic
`OwnerType + OwnerId` for the 1:0..1 case (Employee) or the
auto-incremented `Id` for the 1:N case (BusinessPartner).

### 9.5 The future CRM extension space

The V1 wire contract is forward-compatible with the future
CRM module:

- A V2+ CRM module adds `CrmContactEngagement` (a sibling
  table to `ContactProfile`). The V1 `ContactProfile` is the
  primary key (`ContactProfileId`).
- A V2+ CRM module adds `CrmOpportunity` (linked to
  `BusinessPartnerId` + `ContactProfileId`). The V1
  `ContactProfile` is the primary key (`ContactProfileId`).
- A V2+ CRM module adds `CrmChatThread` (linked to
  `ContactProfileId` + the WeChat/WeCom identifier). The V1
  `ContactProfile.WeChatId` / `WeComId` is the join key.
- A V2+ CRM module extends `ContactProfile` with the
  `Source / LastContactedAt / Engagement score` fields (V2+).
  The V1 schema does not have these fields, so the V2+ CRM
  migration adds them.

The V1 contract is intentionally minimal. The V1 design
**leaves room** for the V2+ CRM extension without breaking the
V1 wire.

---

## 10. Vue page planning (read-only, no implementation)

### 10.1 The "员工详情 联系方式 Tab" question

The brief asks: "员工详情：联系方式 Tab. 客户详情：联系人 Tab. 是否复用：MDM Design System."

**Decision (V1):**

- **Employee Detail page** — the existing Employee page (per
  `GULIERP_EMPLOYEE_MASTER_001_DESIGN_REPORT.md` §8) does
  NOT have a Tab structure. V1 keeps the Employee detail as a
  single drawer (`MdmDetailDrawer`-style; in this case, a new
  `EmployeeDetailDrawer`). The Contact information is shown
  **inline** in the drawer (no Tab structure).

- **BusinessPartner Detail page** — the existing BusinessPartner
  page (`apps/web/src/views/mdm/BusinessPartnerList.vue`)
  does NOT have a Tab structure either. V1 keeps the
  BusinessPartner detail as a single drawer. The Contact
  list is shown **inline** in the drawer.

**Why no Tab in V1?** Because the existing Employee +
BusinessPartner page patterns are "single drawer" (no Tabs).
The V1 Contact Profile is **inline** in the existing drawer.
V1.5+ may add a Tab structure if the contact-related fields
grow to warrant it.

### 10.2 The "inline contact section" pattern

The new contact section is added to the existing
`MdmDetailDrawer` (reusable) with a new prop:
`MdmDetailDrawer :contactProfiles="contactProfiles"`.

Actually, the simpler approach: each page renders the
existing drawer PLUS a new "ContactListPanel" component
inline:

```vue
<MdmDetailDrawer :row="row" ...>
  <template #default>
    <!-- existing V1 detail content -->
    <el-descriptions :column="2" border>
      <el-descriptions-item label="员工编号">...</el-descriptions-item>
      ...
    </el-descriptions>
  </template>
  <template #contact>
    <ContactListPanel
      :owner-type="OwnerType.Employee"
      :owner-id="row.id"
      :can-manage="hasPermission('contact.profile.manage')"
    />
  </template>
</MdmDetailDrawer>
```

The `ContactListPanel` is a new reusable component
(`apps/web/src/components/contact/ContactListPanel.vue`) that:
- Fetches the ContactProfile list for the given owner.
- Renders a mini-table (Name + Position + Mobile + WeChat +
  WeCom + Email + IsPrimary + Status).
- Renders a QR code preview for the WeChat / WeCom row
  (click to expand).
- Has a "新增联系方式" button (gated by `contact.profile.manage`).
- Has a "设为主联系人" action (gated by `contact.profile.manage`).
- Uses the same `MdmListToolbar` + `MdmPagination` + `MdmEmptyState`
  + `MdmStatusBadge` + `MdmTableRowActions` MDM Design System
  components.

### 10.3 The "SalesOrderDetail tabs" precedent

The existing `SalesOrderDetail.vue` (line 73) uses `el-tabs`
+ `el-tab-pane` for tabs (审批记录 / 附件 / 来源/下游 / 操作日志).
This is the established tab pattern in the codebase.

V1 does NOT use the tab pattern for Employee / BusinessPartner
detail (the inline contact section is simpler). V1.5+ may
introduce tabs if the contact section grows to 4+ sub-sections
(e.g. 联系方式 / 沟通记录 / 商机 / 任务).

### 10.4 The MDM Design System reuse

The new `ContactListPanel` reuses the 7 Mdm components:
- `MdmListToolbar` — search + "新增" button.
- `MdmStatusBadge` — Active / Inactive badge (adds a
  `type="contact"` prop to support the `ContactProfileStatus`
  2-state enum; the existing MasterData 2-state enum is the
  default).
- `MdmFormDrawer` — Create / Edit drawer.
- `MdmDetailDrawer` — Detail view.
- `MdmPagination` — Paged list.
- `MdmEmptyState` — Empty result.
- `MdmTableRowActions` — Row actions (set primary / edit / set
  status).

The `MdmStatusBadge` refactor from
`GULIERP_EMPLOYEE_MASTER_IMPLEMENTATION_READY_REPORT.md` §9.2
(adds a `statusType` prop) extends to support
`contact-profile` as a 3rd type (in addition to `master-data`
and `employee`).

### 10.5 The QR code preview

The new `ContactQRCodePreview` component
(`apps/web/src/components/contact/ContactQRCodePreview.vue`)
displays a small QR code thumbnail (128x128 px) next to the
WeChat / WeCom row. Clicking the thumbnail opens a full-size
preview (256x256 px) in a new drawer.

The API call: `GET /api/v1/contact-profiles/{id}/qrcode?type=wechat&size=256`
→ `Content-Type: image/png` → `<img :src="qrCodeUrl">`.

The component reuses the `MdmFormDrawer` for the full-size
preview.

---

## 11. Test planning

### 11.1 The 5 categories (per the brief)

| Category | Test count | Project | Pattern |
|----------|-----------|---------|---------|
| **Domain** | ~7 | `tests/GuliERP.Contact.Tests` (NEW) | Entity contract (no DB): V1 fields only, polymorphic FK validation, `IsPrimary` semantics, `Status` 2-state enum |
| **Application** | ~25 | `tests/GuliERP.Contact.Tests` | xUnit + reflection: 4-step pipeline on `Name` (reuses `FormatValidator`), QR code generation, polymorphic FK existence check, status lifecycle, concurrency |
| **API** | ~10 | `tests/GuliERP.Contact.IntegrationTests` (NEW) | PostgreSQL + EF Core, full host boot: end-to-end HTTP via `WebApplicationFactory`, the 9 endpoints |
| **Permission** | ~3 | `tests/GuliERP.Contact.Tests` + integration | The 2 + 1 + 1 layered-permission rules (Identity read + Contact read, MDM read + Contact read, etc.) |
| **Tenant isolation** | ~4 | `tests/GuliERP.Contact.Tests` + integration | Reflection-based: the App service applies `Where(c => c.TenantId == ... && c.CompanyId == ...)`; cross-Company access denied; cross-Tenant access denied |
| **Total** | **~49** | (matches the brief's 5-category split) | |

### 11.2 The test invariants locked

- **Polymorphic FK check**: the App service's
  `EnsureOwnerExistsAsync` method is called BEFORE insert /
  update. The unit test fails if any future refactor skips
  the check.
- **1:0..1 for Employee**: the partial unique index
  `ux_gulierp_contact_employee` enforces the cardinality.
  The integration test asserts the second insert throws
  `DbUpdateException` (or a similar DB-level error) wrapped in
  a `ContactProfileValidationException` with code
  `ContactProfileDuplicate` (or similar).
- **4-step pipeline on `Name`**: reuses the just-shipped
  Foundation `FormatValidator` + `ReservedNameValidator`. The
  unit test asserts the 11-name reserved set is honored.
- **Status lifecycle**: 2-state enum (Active / Inactive). The
  unit test asserts the 4 transitions (Active → Inactive, and
  vice versa) are allowed. (V1 has no "Left" / "Archived"
  state; the cross-cutting `MasterDataStatus` 2-state is
  the V1 truth.)
- **QR code generation**: the unit test asserts the
  `QRCodeService.GeneratePngAsync` returns a valid PNG
  byte stream for each `QRCodeType` (WeChat / WeCom / Email
  / Phone / Url) with the expected `Content-Length` and the
  expected first bytes (PNG signature `89 50 4E 47`).
- **No CRM scope**: the architecture test fails if the
  ContactProfile entity adds any of the V2+ fields
  (`Source` / `LastContactedAt` / `Engagement score` /
  `Lifecycle stage` / `Phone` / `AvatarUrl` / `Tags`).

### 11.3 The out-of-scope test surface

- **No front-end test** for the new `ContactListPanel` /
  `ContactQRCodePreview` components. Consistent with the
  precedent (the Employee + BusinessPartner + 6 MDM pages
  have no front-end test framework today).
- **No performance / load test** for the QR code generation.
  The QRCoder library is fast (< 10 ms per image on modern
  hardware); the page caches the image via HTTP
  `Cache-Control: max-age=3600`.
- **No DB migration test.** The V1 `ContactProfile` is a new
  entity; the V1 migration is part of the implementation
  milestone (NOT in this design).

---

## 12. V1 / V2 boundary (the explicit list)

### 12.1 V1 includes (the explicit V1 contract)

- **Entity**: `ContactProfile` with the 19 V1 fields
  (§3.1).
- **Polymorphic FK**: `OwnerType` (Employee | BusinessPartner) +
  `OwnerId` (long).
- **Cardinality**: 1:0..1 for Employee (enforced by partial
  unique index), 1:N for BusinessPartner (no constraint).
- **9 endpoints** (§9.1) with the 2 new permissions.
- **`ContactListPanel` + `ContactQRCodePreview` Vue
  components**, integrated into the existing
  `MdmDetailDrawer` inline.
- **`QRCodeService` dynamic generation** (no image blob
  storage).
- **The 12 new `ContactErrorCodes` constants**.

### 12.2 V2+ (explicitly NOT in this design)

- **Phone (landline)** field — V2+ (the brief explicitly
  defers it; we use Mobile only in V1).
- **AvatarUrl** — V2+ (image storage / CDN).
- **Tags** — V2+ (CRM-style tagging).
- **Source / LastContactedAt / Engagement score / Lifecycle
  stage** — V2+ (CRM scope, FORBIDDEN per the brief).
- **WeChat / WeCom chat history** — V2+ (CRM scope, FORBIDDEN).
- **Multi-language name** — V2+.
- **External Consultant** owner type — V2+ (V1 freezes the
  `OwnerType` enum to 2 values; the 3rd value is a V2+
  design goal).
- **1:N for Employee** — V2+ (V1 freezes the cardinality to
  1:0..1 for Employee; the partial unique index is dropped
  in V2+; the page adds the "add contact" workflow).
- **V1.5+ ContactProfile page on Employee + BusinessPartner
  detail** — V1.5+ (V1 keeps the inline section; V1.5+ may
  introduce a Tab structure if the section grows).
- **V1.5+ deprecate `BusinessPartner.ContactPerson`** — V1.5+
  (NOT in this PR).
- **V2+ Customer / Vendor analytics** — V2+ (out of ERP scope).
- **V2+ AI Employee (LLM-based sales assistant)** — V2+ (per
  the brief's "与AI员工未来结合分析"; see §13).

### 12.3 The forward-compatibility checklist

| V2+ need | How V1 supports it |
|----------|---------------------|
| Add a 3rd `OwnerType` (e.g., External Consultant) | The `OwnerType` enum is `enum ContactProfileOwnerType { Employee, BusinessPartner }`; adding a 3rd value is a non-breaking change to the EF Core mapping + the App service's `EnsureOwnerExistsAsync` switch. |
| 1:N for Employee | Drop the partial unique index `ux_gulierp_contact_employee`; the `IsPrimary` flag is already there; the page adds the "add contact" workflow. |
| Multi-contact for BusinessPartner (already supported) | The polymorphic FK already supports 1:N for BusinessPartner. V1 ships this. |
| WeChat / WeCom chat history | The `WeChatId` / `WeComId` columns are the join key for the future `CrmChatThread` table. V1 stores only the identifier. |
| Lead source attribution | Add the `Source` enum + column in V2+. |
| AI Employee (LLM-based sales assistant) | The V1 `ContactProfile` is the data source the AI Employee reads. See §13. |

---

## 13. Future AI Employee integration analysis

### 13.1 The use case (V2+ AI Employee module)

A V2+ AI Employee module (e.g., `CrmAiEmployee`) reads
`ContactProfile` data to power an LLM-based sales
assistant. The AI Employee uses the contact data to:
- **Suggest the right contact** for a sales call ("the
  primary contact for this BusinessPartner is 王总, but
  采购经理 usually has the budget authority").
- **Compose outreach messages** ("send a WeChat message to
  采购经理 about the new product launch").
- **Track engagement** ("the last time we contacted 王总
  was 3 weeks ago, he responded positively").

### 13.2 The V1 data exposed to the future AI Employee

The V1 `ContactProfile` table exposes:
- `OwnerType` + `OwnerId` — which Employee / BusinessPartner
  the contact belongs to.
- `Name` + `Position` — who the contact is.
- `Mobile` + `WeChatId` + `WeComId` + `Email` — how to reach
  the contact.
- `IsPrimary` + `Status` — which contact is the default; which
  contacts are active.

The V1 data is **sufficient** for the V2+ AI Employee's
"compose outreach" use case. The AI Employee does NOT need
the V2+ CRM fields (engagement score, lifecycle stage, etc.)
to send a message; it can use the V1 data + the V2+ CRM
fields when they exist.

### 13.3 The forward-compatibility surface

The V1 design explicitly leaves the following hooks for the
V2+ AI Employee module:

- **The polymorphic `OwnerType` enum** — a 3rd value
  (`ExternalConsultant`) can be added without breaking the
  V1 contract. The AI Employee can query
  `ContactProfile?OwnerType=ExternalConsultant` in V2+.
- **The `IsPrimary` flag** — the AI Employee can prioritize
  primary contacts.
- **The `Status` field** — the AI Employee skips Inactive
  contacts.
- **The `Position` field** — the AI Employee uses
  `Position` to match contacts to roles (e.g., "采购经理" for
  purchasing decisions).
- **The `Remark` field** — the AI Employee can use free-text
  remarks to personalize outreach (e.g., "王总 prefers
  WeChat over phone").

### 13.4 The V1 API surface for the AI Employee

The V1 API exposes 9 endpoints (§9.1). The V2+ AI Employee
module will use:
- `GET /api/v1/contact-profiles?ownerType=BusinessPartner&ownerId=...`
  — to fetch the contact list for a BusinessPartner.
- `GET /api/v1/contact-profiles/{id}/qrcode?type=wechat&size=256`
  — to render the WeChat QR code for the AI Employee's UI
  (so the human sales rep can scan + add the contact on
  WeChat).
- (V2+) `POST /api/v1/contact-profiles/{id}/engagement` — to
  record a "contacted at" event (V2+ CRM scope).

The V1 API is **forward-compatible** with the V2+ AI Employee
use case. No V1 changes are needed.

### 13.5 The privacy + security boundary

The V1 API exposes PII (Mobile, Email, WeChat, WeCom) via
the `ContactProfileRead` permission. A V2+ AI Employee
module must:
- Have the `ContactProfileRead` permission (granted via the
  EnterpriseSystemAdmin role or a future dedicated CRM role).
- NOT log the PII to external LLM providers (the
  privacy contract is the AI Employee's, not the Contact
  Profile's).
- Redact the PII when displaying the contact in the AI
  Employee's UI (the UI shows "王总 / 采购经理" but not the
  phone number unless the user clicks "show").

The V1 design does not add new privacy controls (those are
V2+ AI Employee's responsibility). The V1 `ContactProfile`
is just the data store; the privacy + redaction is the
consumer's responsibility.

---

## 14. Authority chain

This document inherits its authority from:
- `docs/architecture/MDM_000_MASTER_DATA_CONVENTION_V1.md` —
  the MDM master-data convention.
- `docs/governance/GULIERP_MODULE_INDEPENDENCE_RULE.md` —
  the module ownership rule (the primary reason for the new
  `modules/contact/` module).
- `docs/governance/META_GULI_GOVERNANCE_V1.md` — the
  cross-cutting governance rule.
- `docs/business/GULIERP_MASTER_DATA_MODEL_V1.md` (FROZEN) —
  the V1 master-data vocabulary (the V1 `BusinessPartner`
  + `Employee` + `Contact` triple).
- `docs/business/GULIERP_CODE_RULE_STANDARD_V1.md` (FROZEN) —
  the V1 code rule standard.
- `docs/business/GULIERP_EMPLOYEE_MASTER_MODEL_V1.md` (FROZEN) —
  the V1 Employee model (this design extends the V1 model
  with the polymorphic ContactProfile).
- `docs/business/GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md`
  (FROZEN) — the V1 Foundation code pipeline (this design
  reuses the 4-step pipeline for the `ContactProfile.Name`
  field).

This document does NOT modify any of the above. It locks the
V1 Contact Profile design to the level of detail required by
the implementation milestone.

---

## 15. Gate

`GULIERP_CONTACT_PROFILE_001_DESIGN_COMPLETE`

This gate fires when:
1. The V1 Contact Profile entity is frozen (§3).
2. The Employee + BusinessPartner cardinality is decided
   (1:0..1 + 1:N; §3.5, §3.6, §4, §5).
3. The QR code design is frozen (dynamic generation, no blob;
   §6).
4. The module location is decided (new `modules/contact/`;
   §7).
5. The permissions are decided (2 new top-level permissions,
   layered with owner permissions; §8).
6. The API contract is frozen (9 endpoints; §9).
7. The Vue page plan is decided (inline contact section; §10).
8. The test plan is decided (§11).
9. The V1 / V2 boundary is explicit (§12).
10. The AI Employee forward-compatibility is analyzed (§13).

The implementation milestone opens after this gate fires.

---

## 16. Honest disclosure (residual gaps and design choices)

1. **No Party supertype in V1.** The brief asks whether to
   abstract a `Party` supertype (SAP-style). V1 does NOT.
   The polymorphism via `OwnerType + OwnerId` is sufficient
   for V1; a `Party` supertype would add complexity without
   immediate benefit. V1.5+ may add `Party` if a 3rd owner
   type (e.g., External Consultant) is introduced.
2. **The `BusinessPartner.ContactPerson` field stays in V1.**
   The new `ContactProfile` table is the authoritative source
   for multi-contact; the inline `ContactPerson` field is
   preserved for backward compat. V1.5+ deprecates it.
3. **The QR code image is generated server-side (not
   client-side).** This avoids loading the QRCoder
   JavaScript library into the browser (it would be ~500 KB
   minified). The page calls the API endpoint, gets the PNG
   bytes, renders via `<img>`.
4. **The V1 `Name` field is validated by a subset of the
   4-step pipeline** (just the `FormatValidator`'s regex +
   the `ReservedNameValidator`'s reserved set; no uniqueness
   check, no doc-number pattern). The `Name` is a free-text
   display name, not a code. The pipeline applies to the
   `ContactProfile.Code` field (V2+).
5. **The 4 reserved names from the V1 frozen spec
   (`SYSTEM / SYS / RESERVED / EMP-SYSTEM`) are forbidden for
   `ContactProfile.Name`.** Plus 1 more for Contact: a
   `CONTACT-SYSTEM` reserved name (for a future bootstrap
   admin Contact Profile).
6. **The new `modules/contact/` module is a heavy lift** (3
   new projects, 2 new test projects, 1 new EF Core
   `DbContext`, 1 new DI registration, 1 new slnx entry).
   The implementation milestone will be ~5-7 days. The
   `GULIERP_EMPLOYEE_MASTER_001_IMPLEMENTATION` is paused
   until the Contact module ships (per the brief).
7. **No QRCoder license verification was done in this
   design.** The implementation milestone must verify the
   QRCoder license is MIT-compatible (it is; but the
   verification is a project-policy check).
8. **No Phone (landline) field in V1.** The brief explicitly
   defers it. The `ContactProfile.Phone` field is reserved
   in the entity (commented as "V2 deferred") but is not
   exposed in the V1 wire contract.
9. **No V1.5+ deprecation of `BusinessPartner.ContactPerson`
   in this PR.** V1.5+ will deprecate it (with a 2-release
   transition window).
10. **No V1.5+ 1:N for Employee in this PR.** V1 freezes
    the cardinality to 1:0..1 (the partial unique index
    enforces it). V1.5+ drops the partial unique index and
    adds the multi-contact workflow.
