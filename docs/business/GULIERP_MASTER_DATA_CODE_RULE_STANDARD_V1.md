# GULIERP_MASTER_DATA_CODE_RULE_STANDARD_V1

| Field | Value |
|---|---|
| Goal | `GULIERP_MASTER_DATA_CODE_RULE_STANDARD_V1` |
| Nature | Standard / contract only |
| Repository | `D:\guli\projects\gulierp-next` |
| Predecessor | `GULIERP_MASTER_DATA_COMMON_FIELD_CONTRACT_V1_VERIFIED` |
| Status | `FROZEN` |

## 1. Purpose

This standard freezes the V1 rules for generating, validating, previewing and changing GuliERP master-data codes. It is a design contract only. It does not add services, tables, migrations, APIs, frontend code or tests.

The goal is simple, predictable, transaction-safe and configurable enough for ERP master data without turning code generation into a dynamic rule platform.

## 2. Scope

Covered code-bearing objects:

| Object | Owner | Classification |
|---|---|---|
| BusinessPartner | MDM | master data |
| Item | MDM | master data |
| ItemCategory | MDM | master data |
| UOM | MDM | global reference data |
| Warehouse | MDM | master data |
| Location | MDM | master data |
| Employee | Identity/Foundation | master data |
| Plant | Identity/Foundation | master data |
| OrganizationUnit / Department | Identity/Foundation | master data |
| Company | Identity/Foundation | master data |
| DictionaryType | MDM | reference data |
| DictionaryItem | MDM | reference data |

NumberingRule is configuration for transaction document numbering. It is not itself a master-data code consumer.

## 3. Preconditions

The predecessor common-field contract is frozen and verified:

`GULIERP_MASTER_DATA_COMMON_FIELD_CONTRACT_V1_VERIFIED`

Frozen predecessor facts consumed:

- Code / EmployeeNo is required for first-class master/reference data.
- TenantId is required by scope; CompanyId is not universal.
- UOM is global reference data.
- Item, UOM, Company and system DictionaryType use strict immutable code policy.
- BusinessPartner, Warehouse, Location, Employee, Plant, OrganizationUnit and non-system DictionaryItem use controlled-change policy.
- Current repository has format validation but no full master-data code-rule engine.

## 4. Terminology

| Term | Meaning |
|---|---|
| Master Data Code | Long-lived business identifier for a master/reference row. |
| Transaction Document Number | Number allocated to a transaction document such as a sales order. |
| Rule | Configuration that says how a code should be supplied or generated for an entity and scope. |
| Sequence State | Mutable counter state used by auto generation. |
| Preview | UI-facing display of a candidate code before save. |
| Reservation | A consumed, durable allocation of a code candidate before final create. |
| Consumption | The point where sequence state advances and a generated number is no longer reused. |
| MANUAL | User must provide Code. |
| AUTO | System generates Code at save; user cannot edit it during interactive create. |
| AUTO_EDITABLE | System suggests or generates Code, but user may override before create. |

`AUTO_IF_EMPTY` is not a separate V1 mode. It is the API behavior of `AUTO_EDITABLE`: missing Code generates; provided Code validates and uses the supplied value.

## 5. Document Number vs Master Data Code

Transaction Document Number and Master Data Code are separate systems.

| Aspect | Transaction Document Number | Master Data Code |
|---|---|---|
| Example | `SO-20260828-000001` | `BP_000001`, `ITEM_000001`, `WH_001` |
| Timing | Allocated during transaction document create | Allocated or validated during master-data create |
| Date dependency | Strong; period key is part of scope | None in V1 |
| Reset | Daily or monthly in current DocumentKernel | None in V1 |
| Mutability | Immutable | Entity-specific: strict immutable or controlled change |
| Scope | Tenant + Company + DocumentType + PeriodKey | Entity-specific tenant/company/warehouse scope |
| Continuity | More sensitive | Gap allowed |
| Service | `IDocumentNumberService` | Future `MasterDataCodeService` or equivalent |

`IDocumentNumberService` must not be called directly to generate master-data codes.

## 6. Existing Infrastructure Assessment

| Capability / artifact | Status | Reuse decision | Evidence summary |
|---|---|---|---|
| `IDocumentNumberService` | `IMPLEMENTED` | `NOT_REUSABLE` directly | Generates transaction document numbers only. Request requires DocumentType, TenantId, CompanyId, BusinessDate, optional idempotency key and ActorId. |
| `DocumentNumberCounter` | `IMPLEMENTED` | `REUSABLE_WITH_ADAPTER` concept only | Has persistent counter and unique scope, but scope includes DocumentType and PeriodKey. |
| DocumentKernel atomic increment | `IMPLEMENTED` | `REUSABLE_WITH_ADAPTER` concept only | Demonstrates atomic counter semantics; implementation uses PostgreSQL-specific upsert and returning. |
| DocumentKernel idempotency table | `IMPLEMENTED` | `PARTIAL` concept only | Useful for retry thinking, but master-data create should be transactional with the aggregate create path. |
| DocumentTypeProfile catalog | `IMPLEMENTED` | `NOT_REUSABLE` directly | Static document-type profile, not runtime master-data rule configuration. |
| MDM `NumberingRule` | `IMPLEMENTED` | `NOT_REUSABLE` directly | Operator-facing document-number rule record using DocumentType, Prefix, DatePattern, SequenceLength and ResetMode. |
| `MasterDataCodeValidator` | `IMPLEMENTED` | `REUSABLE_WITH_ADAPTER` | Runs format, reserved-name and document-number-similarity validation. |
| `FormatValidator` | `IMPLEMENTED` | `REUSABLE_WITH_ADAPTER` | Current regex is uppercase letters/digits/underscore, length 2..40. |
| `ReservedNameValidator` | `IMPLEMENTED` | `REUSABLE_WITH_ADAPTER` | Protects system/bootstrap names. |
| `DocumentNumberSimilarityValidator` | `IMPLEMENTED` | `REUSABLE_WITH_ADAPTER` | Keeps master-data codes from looking like document numbers. |
| Master-data next-code API | `MISSING` | new implementation required | No `NextCode`/`PreviewCode`/`GenerateCode` evidence for master data. |
| Master-data rule table | `MISSING` | new implementation required | No dedicated MasterDataCodeRule table. |
| Master-data sequence state | `MISSING` | new implementation required | No dedicated sequence state for BusinessPartner/Item/Warehouse/Location/Employee. |

DocumentNumberService answers:

| Question | Answer |
|---|---|
| True responsibility | Generate atomic transaction document numbers. |
| Persistent sequence | Yes, `DocumentNumberCounter`. |
| Tenant scope | Yes. |
| Company scope | Yes. |
| Reset | Yes, by profile period key. |
| Prefix | Yes, static document profile prefix. |
| Concurrency | Yes, atomic counter semantics. |
| Transaction | It performs its own counter/idempotency persistence; callers consume result during document create. |
| Preview | No preview API. |
| Direct fit for master data | No. Use ideas, not service/tables directly. |

## 7. Code Modes

| Mode | Frozen behavior |
|---|---|
| `MANUAL` | Create request must provide Code. Server normalizes, validates and checks uniqueness. No sequence is consumed. |
| `AUTO` | Interactive create does not allow user Code entry. Server allocates Code at save. |
| `AUTO_EDITABLE` | UI may show a server-provided candidate; create Code may be empty to generate or provided to override. Provided Code must pass manual validation and uniqueness. |

Mode is a rule property. It is not a separate DTO family.

## 8. Entity Code Strategy

| Entity | DefaultMode | AllowedModes | Mutability | RuleScope | UniquenessScope | SequenceScope | Prefix | SequenceLength | ResetPolicy | ImportOverride |
|---|---|---|---|---|---|---|---|---:|---|---|
| BusinessPartner | `AUTO_EDITABLE` | `MANUAL`, `AUTO_EDITABLE`, `AUTO` | `CONTROLLED_CHANGE` | Tenant | `(TenantId, Code)` | Tenant | `BP` | 6 | `NONE` | yes |
| Item | `AUTO_EDITABLE` | `MANUAL`, `AUTO_EDITABLE`, `AUTO` | `STRICT_IMMUTABLE` after create | Tenant | `(TenantId, Code)` | Tenant | `ITEM` | 6 | `NONE` | yes |
| ItemCategory | `MANUAL` | `MANUAL`, `AUTO_EDITABLE` | `CONTROLLED_CHANGE` | Tenant | `(TenantId, Code)` | Tenant | `CAT` | 4 | `NONE` | yes |
| UOM | `MANUAL` | `MANUAL` | `STRICT_IMMUTABLE` | Global | `(Code)` | none | none | n/a | `NONE` | yes for system/admin import |
| Warehouse | `AUTO_EDITABLE` | `MANUAL`, `AUTO_EDITABLE`, `AUTO` | `CONTROLLED_CHANGE` | Company | `(TenantId, CompanyId, Code)` | Company | `WH` | 3 | `NONE` | yes |
| Location | `AUTO_EDITABLE` | `MANUAL`, `AUTO_EDITABLE`, `AUTO` | `CONTROLLED_CHANGE` | Warehouse | `(TenantId, CompanyId, WarehouseId, Code)` | Warehouse | `LOC` | 4 | `NONE` | yes |
| Employee | `AUTO_EDITABLE` | `MANUAL`, `AUTO_EDITABLE`, `AUTO` | `CONTROLLED_CHANGE` | Company | `(CompanyId, EmployeeNo)` | Company | `EMP` | 6 | `NONE` | yes |
| Plant | `AUTO_EDITABLE` | `MANUAL`, `AUTO_EDITABLE`, `AUTO` | `CONTROLLED_CHANGE` | Company | `(CompanyId, Code)` | Company | `PLANT` | 3 | `NONE` | yes |
| OrganizationUnit / Department | `AUTO_EDITABLE` | `MANUAL`, `AUTO_EDITABLE`, `AUTO` | `CONTROLLED_CHANGE` | Company | `(CompanyId, Code)` | Company | `ORG` | 3 | `NONE` | yes |
| Company | `MANUAL` | `MANUAL` | `STRICT_IMMUTABLE` | Tenant | `(TenantId, Code)` | none | none | n/a | `NONE` | yes |
| DictionaryType | `MANUAL` | `MANUAL` | `STRICT_IMMUTABLE` for system rows | Tenant | `(TenantId, Code)` | none | none | n/a | `NONE` | yes |
| DictionaryItem | `MANUAL` | `MANUAL` | `CONTROLLED_CHANGE` for non-system rows | DictionaryType | `(TenantId, DictionaryTypeId, Code)` | none | none | n/a | `NONE` | yes |

## 9. Manual Code Contract

Manual Code validation is intentionally conservative for V1 and remains aligned with the frozen common-field contract.

| Rule | V1 behavior |
|---|---|
| Empty | Invalid when mode requires Code. |
| Trim | Server trims before validation. |
| Min length | 2. |
| Max length | 40. |
| Whitespace | Not allowed. |
| Control characters / newline | Not allowed. |
| Unicode / Chinese | Not allowed in canonical V1 Code. Use Name/ShortName/MnemonicCode for human-language text. |
| Letters | ASCII uppercase A-Z. |
| Digits | Allowed after the first character. |
| Underscore | Allowed after the first character. |
| Dash | Not allowed for interactive V1 master-data Code; it visually overlaps with document-number style. |
| Slash / dot | Not allowed in canonical V1 Code. Preserve legacy external codes only through controlled import mapping if needed. |
| Reserved codes | Invalid. |
| Document-number-like codes | Invalid. |

Legacy ERP imports may preserve existing external codes only through an explicit import override path with duplicate checks and normalization semantics. Interactive create remains stricter.

## 10. Code Normalization

Code is case-insensitive by business semantics. V1 stores canonical uppercase Code and treats `CUS001` and `cus001` as the same code.

No separate `NormalizedCode` column is required if every write path canonicalizes Code before persistence. A future implementation may add `NormalizedCode` or a functional unique index if needed for defense, but the contract must not depend on PostgreSQL-only features such as `citext`.

Database portability requirement:

- semantic uniqueness is on normalized uppercase Code;
- implementation may use application normalization plus ordinary unique indexes;
- provider-specific optimizations are allowed only behind the same semantic contract.

## 11. Prefix Contract

V1 uses a simple static prefix plus separator plus zero-padded sequence. It does not support a dynamic template language.

| Rule | Contract |
|---|---|
| Prefix type | Static configured prefix per active rule. |
| Max length | 12. |
| Min length | 1 for auto modes. |
| Characters | ASCII uppercase letters and digits; first character must be a letter. |
| Case | Stored uppercase. |
| Empty prefix | Allowed only for MANUAL-only rules with no sequence generation. |
| Separator | Default `_`; allowed values are `_` or empty. |
| Hyphen | Not used by V1 master-data auto generation. |
| Prefix change | Does not rewrite historical codes. |

Rendered code format:

```text
{Prefix}{Separator}{Sequence}
```

Examples:

```text
BP_000001
ITEM_000001
WH_001
LOC_0001
EMP_000001
```

## 12. Sequence Contract

| Field | V1 contract |
|---|---|
| SequenceLength | Required for auto modes; 1..12. |
| StartValue | Required for auto modes; default 1. |
| Increment | Required; V1 fixed to 1. |
| CurrentValue | Last consumed value. |
| NextValue | CurrentValue + Increment, rendered with zero padding. |
| Zero padding | Required to SequenceLength. |
| Overflow | Block generation with `CODE_SEQUENCE_EXHAUSTED`; admin must explicitly extend length or configure a new rule. |
| Automatic widening | Not allowed in V1. |

V1 does not require continuous numbers. It requires uniqueness and atomicity.

## 13. Reset Policy

Master-data codes do not reset by date in V1.

| Policy | V1 decision |
|---|---|
| `NONE` | Required default and only active V1 reset policy. |
| `YEARLY` | Not supported in V1. |
| `MONTHLY` | Not supported in V1. |
| `DAILY` | Not supported in V1. |

Business reason: master data is long-lived and not event-period based. Date reset would create needless collisions, make imports harder and blur the boundary with transaction document numbers.

## 14. Scope Contract

Rule scope, uniqueness scope and sequence scope are related but not identical.

| Entity | RuleScope | UniquenessScope | SequenceScope |
|---|---|---|---|
| BusinessPartner | Tenant | `(TenantId, Code)` | Tenant |
| Item | Tenant | `(TenantId, Code)` | Tenant |
| ItemCategory | Tenant | `(TenantId, Code)` | Tenant |
| UOM | Global reference | `(Code)` | none |
| Warehouse | Company | `(TenantId, CompanyId, Code)` | Company |
| Location | Warehouse | `(TenantId, CompanyId, WarehouseId, Code)` | Warehouse |
| Employee | Company | `(CompanyId, EmployeeNo)` | Company |
| Plant | Company | `(CompanyId, Code)` | Company |
| OrganizationUnit / Department | Company | `(CompanyId, Code)` | Company |
| Company | Tenant | `(TenantId, Code)` | none |
| DictionaryType | Tenant | `(TenantId, Code)` | none |
| DictionaryItem | DictionaryType | `(TenantId, DictionaryTypeId, Code)` | none |

Rule lookup key is limited to:

```text
TenantId + EntityType + optional CompanyId + optional WarehouseId + optional SubType
```

V1 default rules do not use arbitrary dimensions such as department, region, user, channel, date or custom conditions.

## 15. Uniqueness Contract

The database must enforce the final uniqueness boundary for persisted Code. The application must validate before save for friendly errors, but the database remains the last guard.

Location uniqueness is frozen as warehouse-wide for future code-rule implementation:

```text
(TenantId, CompanyId, WarehouseId, Code)
```

Reason: location codes such as `A01` and `BIN_001` are naturally reused in different warehouses. Company-wide uniqueness forces operators to encode warehouse identity twice and makes printed labels longer than necessary.

Existing current schema uses `(TenantId, CompanyId, Code)`. Moving to warehouse-wide uniqueness requires migration-time duplicate analysis and index replacement, not a silent code change.

## 16. BusinessPartner Code Strategy

BusinessPartner V1 uses one shared BusinessPartner sequence, not role-specific customer/supplier sequences.

Frozen default:

```text
BP_000001
```

Decision: BusinessPartner共号.

Rationale:

- Current model allows one partner to be Customer, Supplier or Both through a role flag.
- A supplier can later become a customer; role-specific code such as `SUP_000001` would become misleading.
- The business object is the partner, not the role.
- Customer/supplier filtering belongs to Role, not Code.
- If an enterprise insists on role-like prefixes, it can choose MANUAL mode or override a suggested code before create.

Rule lookup key does not include BusinessPartnerRole in V1. SubType is not used for BusinessPartner default generation.

## 17. Item Code Strategy

Item default mode is `AUTO_EDITABLE`, with `MANUAL` and `AUTO` also allowed.

Frozen default:

```text
ITEM_000001
```

Item.Code is strict immutable after create. AUTO_EDITABLE means editable before submit only. Once the row is created, ordinary update cannot change Item.Code.

ERP import and migration rules:

- imported items may preserve external SKU codes;
- provided import Code must pass normalization and uniqueness checks, or enter a controlled legacy import path;
- existing item codes are never regenerated.

## 18. Warehouse Strategy

Warehouse default mode is `AUTO_EDITABLE`.

Frozen default:

```text
WH_001
```

Rule scope and sequence scope are Company. Warehouse.Code is controlled-change, not free edit. Changes require dedicated permission, uniqueness validation and audit.

## 19. Location Strategy

Location default mode is `AUTO_EDITABLE`.

Frozen default:

```text
LOC_0001
```

Location uniqueness is warehouse-wide:

```text
(TenantId, CompanyId, WarehouseId, Code)
```

This allows:

```text
Warehouse A / A01
Warehouse B / A01
```

Location code changes are controlled because labels and inventory history may reference the code. Historical snapshots are not rewritten by a code change.

## 20. Employee / Organization Strategy

| Entity | Default | Rationale |
|---|---|---|
| Employee | `EMP_000001`, `AUTO_EDITABLE` | Reduces operator work while preserving HR import override. |
| OrganizationUnit / Department | `ORG_001`, `AUTO_EDITABLE` | Helps quick setup; company-specific department coding remains editable before create. |
| Plant | `PLANT_001`, `AUTO_EDITABLE` | Supports multi-site setup without making plant code manual-only. |
| Company | `MANUAL` | Company codes are legal/entity identifiers and should be explicitly chosen during bootstrap/admin setup. |

## 21. Concurrency Contract

Code generation must be atomic. Two concurrent create requests for the same active rule must never receive the same generated code.

Required semantics:

- sequence consumption is serialized per sequence scope;
- uniqueness is still enforced at final Code persistence;
- conflict retry is allowed;
- no application-only in-memory lock is sufficient;
- distributed lock is not required by the contract;
- implementation must remain database-neutral at the standard layer.

Acceptable implementation families:

- database row lock on sequence state;
- optimistic concurrency on sequence state with retry;
- provider-specific atomic update wrapped behind a portable abstraction;
- native sequence object only if the same semantic behavior is preserved across supported databases.

## 22. Transaction Contract

For generated codes, allocation and aggregate creation should occur inside the same business command transaction where practical.

Consumption rule:

- the code is consumed when the sequence state is advanced;
- if aggregate create later fails after sequence state advanced, the number is not reused;
- if validation fails before sequence state advances, no number is consumed.

This avoids unsafe recycling under concurrency and keeps the model simpler than a reservation/reclaim workflow.

## 23. Gap Policy

Master-data code policy is `GAP_ALLOWED`.

No legal or financial continuity requirement applies to BusinessPartner, Item, Warehouse, Location or Employee codes. Gaps can occur due to failed creates, retries, permission failures after allocation or explicit reservation if a later implementation adds reservation.

Uniqueness and auditability matter more than continuity.

## 24. Preview Contract

V1 preview is non-reserving by default.

| Mode | UI preview behavior | Guarantee |
|---|---|---|
| MANUAL | Empty editable Code field. | User-provided code is validated at save. |
| AUTO | Show "generated on save" or a clearly labeled candidate. | Candidate is not guaranteed unless allocated at save. |
| AUTO_EDITABLE | Show server candidate in editable field; user may override. | Candidate is not guaranteed until save. |

Frontend must never calculate sequence values. It may call a future server preview endpoint. Preview text must not imply reservation unless a future explicit reservation API exists.

V1 does not require reservation. If reservation is later added, it must have expiry, audit and gap-allowed behavior.

## 25. Controlled Change Contract

Controlled-change entities may change Code only through a dedicated code-change command, not through normal update.

Minimum rules:

- user has code-change permission;
- entity is not in a final/closed/decommissioned/left state;
- new Code passes normalization, reserved-name and document-number-similarity checks;
- new Code is unique in the entity uniqueness scope;
- audit records old Code, new Code, actor and timestamp;
- historical transaction snapshots are not rewritten;
- external integrations may emit a change event in later implementation;
- no workflow approval is required in V1.

Strict immutable entities reject all ordinary code changes after create.

## 26. Permission Contract

Capabilities required by future implementation:

| Capability | Purpose |
|---|---|
| `MasterData.CodeRule.Read` | View rule configuration. |
| `MasterData.CodeRule.Manage` | Create/update/activate/inactivate rules. |
| `MasterData.Code.Override` | Provide manual Code when rule mode allows generated default. |
| `MasterData.Code.Change` | Execute controlled post-create code change. |

Normal business users should not manage code rules or change existing codes. Create permission alone is not enough to override controlled code policy.

## 27. Rule Resolution

Rule resolution uses the smallest scope needed by the entity.

Resolution order:

1. Build lookup key from current tenant, entity type and entity-specific scope.
2. Find exactly one active rule for that key.
3. If mode is MANUAL and no rule exists, use manual validation fallback.
4. If mode is AUTO or AUTO_EDITABLE and no active rule exists, reject with `CODE_RULE_NOT_FOUND`.
5. If multiple active rules match the same logical key, reject as configuration error.

SubType is allowed by the model but not used by V1 default BusinessPartner generation. It is reserved for tightly bounded future cases, not a general dynamic rule dimension.

## 28. Bootstrap Defaults

New tenant/company bootstrap should create default active rules for auto-capable entities, while preserving MANUAL defaults for system/reference objects.

| Entity | Bootstrap default |
|---|---|
| BusinessPartner | `BP_000001`, tenant scope |
| Item | `ITEM_000001`, tenant scope |
| Warehouse | `WH_001`, company scope |
| Location | `LOC_0001`, warehouse scope; created when a warehouse is created or first location is needed |
| Employee | `EMP_000001`, company scope |
| Plant | `PLANT_001`, company scope |
| OrganizationUnit / Department | `ORG_001`, company scope |
| Company | no auto rule; manual bootstrap code |
| UOM | no auto rule; manual/system seed |
| DictionaryType | no auto rule; manual/system seed |
| DictionaryItem | no auto rule; manual/system seed |

If a bootstrap rule is inactive:

- AUTO and AUTO_EDITABLE interactive create reject with `CODE_RULE_INACTIVE`;
- MANUAL entities continue to validate user-provided Code;
- the system must not silently fall back from AUTO to MANUAL for ordinary users.

## 29. Existing Data Compatibility

Decision: `EXISTING_CODE_PRESERVED`.

Existing BusinessPartner, Item, Warehouse, Location, UOM, Company, OrganizationUnit, Employee and Plant codes must not be regenerated or rewritten when code rules are introduced.

Backfill behavior:

- create rules for future rows only;
- initialize sequence state at or above the max generated-like sequence for each scope where it is safe to parse;
- if existing codes do not match the new prefix/sequence pattern, keep them and let admins choose a StartValue;
- never mass-renumber historical master data.

## 30. Rule Change Compatibility

Changing a rule affects only future generated codes.

If prefix changes from `BP` to `PARTNER`, historical `BP_000001` stays unchanged.

Sequence behavior on rule change:

| Change | V1 behavior |
|---|---|
| Prefix change | Keep existing sequence state unless admin explicitly resets by creating a new rule generation line. |
| SequenceLength increase | Allowed; future rendering uses longer padding. |
| SequenceLength decrease | Reject if CurrentValue cannot fit. |
| StartValue change | Allowed only before any sequence consumption. |
| Mode change | Allowed with concurrency check; does not rewrite existing rows. |

V1 avoids automatic inference. Admin changes must be explicit and audited.

## 31. Import Contract

Imports and integrations may provide explicit Code even when the interactive default mode is AUTO or AUTO_EDITABLE.

| Create path | Code behavior |
|---|---|
| Interactive create | Follows rule mode. |
| Admin import | May preserve external Code with validation and uniqueness checks. |
| System migration | May preserve legacy Code under controlled import policy. |
| External integration | May provide Code only if granted override capability; otherwise generation follows rule. |

Import override must not consume a sequence when the provided Code is accepted. If Code is omitted during import for an auto-capable entity, normal generation applies.

## 32. API Direction

Future create DTOs should converge on one simple shape per entity:

```text
code: string?
```

Behavior:

| Mode | Create.Code Missing | Create.Code Provided | Update.Code Change | Import.Code Provided |
|---|---|---|---|---|
| MANUAL | `CODE_REQUIRED` | validate and use | reject normal update | validate and use |
| AUTO | generate at save | reject unless override capability | reject normal update | validate and use with import override |
| AUTO_EDITABLE | generate at save | validate and use | reject normal update; use code-change command for controlled entities | validate and use |

Do not create three DTO families for the three modes. Mode belongs to rule resolution and service behavior.

## 33. Error Semantics

Future implementation must distinguish these errors:

| Error | Meaning |
|---|---|
| `CODE_REQUIRED` | Code is required by MANUAL mode or explicit policy. |
| `CODE_DUPLICATE` | Normalized Code already exists in uniqueness scope. |
| `CODE_INVALID` | Format, reserved-name or document-number-similarity validation failed. |
| `CODE_RULE_NOT_FOUND` | Auto-capable create has no active rule. |
| `CODE_RULE_INACTIVE` | Matching rule exists but is inactive. |
| `CODE_SEQUENCE_EXHAUSTED` | Next sequence value cannot fit configured length. |
| `CODE_GENERATION_CONFLICT` | Concurrent generation failed after retry budget. |
| `CODE_CHANGE_NOT_ALLOWED` | Entity policy or status forbids post-create Code change. |
| `CODE_OVERRIDE_NOT_ALLOWED` | Caller tried to provide Code when mode/permission forbids override. |

## 34. Persistence Recommendation

Two-table model is recommended.

| Option | Verdict | Reason |
|---|---|---|
| Rule + CurrentValue in one table | rejected for V1 implementation guidance | Simple at first, but hot sequence updates dirty rule metadata, widen lock scope and complicate prefix/rule changes. |
| Rule table + Sequence State table | recommended | Separates stable configuration from hot counter state, keeps lock scope small and supports future audit/change behavior cleanly. |

Recommended logical tables:

| Table | Purpose |
|---|---|
| `MasterDataCodeRule` | Stable rule config: scope, entity, mode, prefix, separator, sequence length, start value, increment, reset policy, status, audit, concurrency. |
| `MasterDataCodeSequenceState` | Mutable sequence state: rule id, scope key, current value, last generated code, audit, concurrency. |

Minimum field classification:

| Field | Rule table | Sequence state |
|---|---|---|
| RuleId / Id | required | required |
| TenantId | required except global reference | required where scoped |
| CompanyId | optional by scope | optional by scope |
| WarehouseId | optional for Location | optional for Location |
| EntityType | required | required or inherited by rule id |
| SubType | optional, unused by default BP strategy | optional |
| Mode | required | not needed |
| Prefix | required for auto modes | not needed |
| Separator | required for auto modes | not needed |
| SequenceLength | required for auto modes | not needed |
| StartValue | required for auto modes | not needed |
| Increment | required, fixed 1 in V1 | not needed |
| ResetPolicy | required, fixed NONE in V1 | not needed |
| CurrentValue | not stored | required |
| LastGeneratedCode | not stored | optional |
| IsActive / Status | required | optional; follows rule |
| Audit | required | required |
| ConcurrencyVersion | required | required |

## 35. Database Portability

The standard is database-neutral. It must not depend on PostgreSQL-only primitives at the contract layer.

Avoid binding core semantics to:

- PostgreSQL sequence;
- `xmin`;
- `citext`;
- advisory locks;
- `RETURNING`;
- provider-specific upsert syntax.

Implementation may use provider-specific optimization behind an abstraction if tests prove the same semantics on supported databases such as openGauss, GaussDB, KingbaseES, DM, OceanBase and SQL Server.

Portable semantic requirements:

- one active logical rule per rule key;
- atomic increment per sequence scope;
- unique normalized Code per entity uniqueness scope;
- bounded retry on generation conflict;
- no frontend-side sequence calculation.

## 36. Entity Strategy Matrix

The entity strategy matrix is the same as Section 8 and is authoritative for default mode, allowed modes, mutability, rule scope, uniqueness scope, sequence scope, prefix, sequence length, reset policy and import override.

## 37. Rule Scope Matrix

| Entity | Rule key | Active-rule uniqueness |
|---|---|---|
| BusinessPartner | TenantId + EntityType | one active rule per tenant/entity |
| Item | TenantId + EntityType | one active rule per tenant/entity |
| ItemCategory | TenantId + EntityType | one active rule per tenant/entity when auto-enabled |
| Warehouse | TenantId + CompanyId + EntityType | one active rule per company/entity |
| Location | TenantId + CompanyId + WarehouseId + EntityType | one active rule per warehouse/entity |
| Employee | TenantId + CompanyId + EntityType | one active rule per company/entity |
| Plant | TenantId + CompanyId + EntityType | one active rule per company/entity |
| OrganizationUnit | TenantId + CompanyId + EntityType | one active rule per company/entity |
| UOM | EntityType only | no active auto rule in V1 |
| Company | TenantId + EntityType | no active auto rule in V1 |
| DictionaryType | TenantId + EntityType | no active auto rule in V1 |
| DictionaryItem | TenantId + DictionaryTypeId + EntityType | no active auto rule in V1 |

## 38. API Behavior Matrix

| Mode | Create.Code Missing | Create.Code Provided | Update.Code Change | Import.Code Provided |
|---|---|---|---|---|
| MANUAL | fail with `CODE_REQUIRED` | normalize, validate, uniqueness check, save | normal update rejects; code-change command only where policy allows | normalize, validate, uniqueness check, save |
| AUTO | generate atomically at save | fail without override capability | reject | accept only through import override |
| AUTO_EDITABLE | generate atomically at save | normalize, validate, uniqueness check, save without consuming sequence | normal update rejects; code-change command only where policy allows | normalize, validate, uniqueness check, save |

## 39. Migration Principles

Future implementation requires database migration, but this standard creates none.

Principles:

1. Add new rule and sequence-state tables.
2. Preserve all existing codes.
3. Add warehouse-wide Location uniqueness only after duplicate analysis.
4. Keep current manual create compatibility until UI/API switch is planned.
5. Seed default rules for new scopes without rewriting existing rows.
6. Initialize sequence state conservatively from existing generated-like codes.
7. Add permissions before exposing rule management UI.
8. Add concurrency tests before using auto generation in operator flows.

## 40. Implementation Work Packages

| WP | Name | Scope |
|---|---|---|
| WP1 | Code Rule Persistence | Add rule and sequence-state model/migration. |
| WP2 | Atomic Sequence Infrastructure | Implement provider-neutral atomic allocation abstraction and PostgreSQL adapter. |
| WP3 | MasterDataCodeService | Resolve rules, validate manual codes, generate codes and handle conflicts. |
| WP4 | Bootstrap Default Rules | Seed tenant/company/warehouse defaults. |
| WP5 | Permission Integration | Add read/manage/override/change capabilities. |
| WP6 | API Compatibility | Adapt create DTO/service behavior with nullable Code semantics where approved. |
| WP7 | BusinessPartner Integration | Apply BP shared sequence and controlled change. |
| WP8 | Item Integration | Apply Item auto-editable create and strict immutable post-create. |
| WP9 | Warehouse / Location Integration | Apply company and warehouse scoped generation and Location uniqueness migration. |
| WP10 | Employee / Organization Integration | Coordinate Identity-owned Employee, Plant and OrganizationUnit codes. |
| WP11 | UI Rule Configuration | Add rule management and create-form code display behavior. |
| WP12 | Regression / Concurrency Tests | Prove duplicate-free concurrent generation, import override and compatibility. |

## 41. Non-Goals

This standard does not include:

- production code changes;
- database changes;
- migrations;
- Entity/DTO/API/frontend/test edits;
- code-rule service implementation;
- preview API implementation;
- BusinessPartner, Item, Warehouse, Location or Employee page changes;
- address or region design;
- inventory receiving;
- purchase or sales new business modules;
- dynamic expression DSL;
- JavaScript templates;
- workflow approval;
- user-defined functions;
- unlimited scope dimensions;
- rule priority chains;
- generic metadata platform.

## 42. Frozen Decisions

| Decision | Frozen V1 outcome |
|---|---|
| Code modes | `MANUAL`, `AUTO`, `AUTO_EDITABLE`; no separate `AUTO_IF_EMPTY`. |
| DocumentNumberService reuse | Do not reuse service/tables directly; reuse concepts through adapter. |
| BusinessPartner default | `AUTO_EDITABLE`, prefix `BP`, shared BP sequence. |
| BusinessPartner role split | No role split. Customer/supplier/both is Role, not sequence dimension. |
| Item default | `AUTO_EDITABLE`, prefix `ITEM`; strict immutable after create. |
| Warehouse default | `AUTO_EDITABLE`, company scoped, prefix `WH`. |
| Location default | `AUTO_EDITABLE`, warehouse scoped, prefix `LOC`. |
| Location uniqueness | Warehouse-wide: `(TenantId, CompanyId, WarehouseId, Code)`. |
| Employee default | `AUTO_EDITABLE`, company scoped, prefix `EMP`. |
| Reset policy | `NONE` only. |
| Gap policy | `GAP_ALLOWED`; do not reclaim consumed numbers. |
| Preview | Non-reserving by default; final code allocated at save. |
| Normalization | Case-insensitive semantics by storing uppercase canonical Code. |
| Prefix model | Static prefix, `_` or empty separator, no DSL. |
| Sequence overflow | Block generation; admin explicitly extends or creates new rule line. |
| Transaction failure | If sequence was consumed, number is not reused. |
| Existing data | Preserve all existing codes; never mass-renumber. |
| Import override | Allowed for admin import/migration/integration with permission. |
| Rule inactive behavior | AUTO/AUTO_EDITABLE fail; no silent manual fallback. |
| Persistence model | Two tables: rule config plus sequence state. |
| Database portability | Contract is database-neutral; provider optimizations must be hidden behind abstraction. |

## 43. Final Standard Status

`GULIERP_MASTER_DATA_CODE_RULE_STANDARD_V1_FROZEN`
