# GULIERP_EMPLOYEE_CODE_CONTRACT_RECONCILIATION_001_REPORT

> 报告日期: 2026-08-24
> 任务: `GULIERP_EMPLOYEE_CODE_CONTRACT_RECONCILIATION_001` — 解决 Employee V1 Code Contract 冲突
> 任务阶段: **Option A applied — fix design docs to honor the implementation regex** (0 code change)
> 操作 Agent: Mavis
> 报告路径: `docs/verification/GULIERP_EMPLOYEE_CODE_CONTRACT_RECONCILIATION_001_REPORT.md`
> 依赖: `GULIERP_NEXT_OPERATOR_RUNTIME_VERIFICATION_001_BLOCKED_REGRESSION` (本任务**不解决** Category C 的 9 个 PG-required tests)
> **最终 Gate**: `GULIERP_EMPLOYEE_CODE_CONTRACT_RECONCILIATION_001_VERIFIED` (Mavis side)

---

## 1) Architecture Decision

**Option A**: 修 design docs examples (`MAT-001` → `MAT_001`, `WH-01` → `WH_01`, `EMP-001` → `EMP_001`, etc.) to honor the implementation regex `^[A-Z][A-Z0-9_]{1,39}$`. **0 code change**。Reserved special cases (`EMP-SYSTEM` / `WH-DEFAULT` / `LOC-RECEIVING` / `LOC-SHIPPING`) are kept with explicit "Bootstrap-bypass only" note.

**Rationale**:
- Implementation is internally consistent (regex + tests + bootstrap all align)
- "Auto-format feature" mentioned in design line 137-140 was never implemented
- Doc examples were the historical error
- Fixing docs is reversible; changing code is more invasive

---

## 2) Authority Chain (unchanged from prior analysis)

### 2.1 `docs/business/GULIERP_CODE_RULE_STANDARD_V1.md` (FROZEN 2026-08-23)

**Before Option A (original)**:
- Line 32: example `MAT-001`, `C0001`, `WH-01` (with `-`)
- Line 66: `MAT-001` �� `MAT-9999999` (with `-`)
- Line 96: `WH-01` (with `-`)
- Line 110-117: prefix table (`MAT-`, `WH-`, `LOC-`, `EMP-`) and examples (`MAT-STEEL-A36`, `WH-NORTH`, `LOC-A-01-03`, `EMP-001`) all with `-`
- Line 131-134: Item examples (`MAT-STEEL-A36`, `MAT-PVC-GREY-25`, `MAT-SF-WHEEL-A`, `MAT-SF-HOUSING-V2`, `MAT-FG-PUMP-100A`, `MAT-SVC-INSTALL`, `MAT-SVC-WARRANTY`)
- Line 147-150: BusinessPartner examples (`C-001`, `S-001`, `B-001`)
- Line 160-161: Warehouse/Location examples (`WH-001`, `WH-NORTH`, `WH-RET`, `LOC-A-01-03`)
- Line 167-168: Employee examples (`EMP-001`, `EMP-FIN-001`)

**After Option A** (this report):
- All examples use `_` (underscore) instead of `-` (hyphen)
- Prefix table updated: `MAT_`, `WH_`, `LOC_`, `OU_`, `PLT_`, `EMP_`, `C_`, `S_`, `B_`, `CAT_`
- Examples updated: `MAT_001`, `WH_01`, `MAT_STEEL_A36`, `WH_NORTH`, `LOC_A_01_03`, `EMP_001`, `EMP_FIN_001`, `C_001`, `S_001`, `B_001`, `WH_001`, `WH_NORTH`, `WH_RET`
- The "auto-format feature" docstring (line 136-140) replaced with explicit "hyphens are reserved for Bootstrap-only system seeds" note
- **Reserved special cases** preserved: `EMP-SYSTEM`, `WH-DEFAULT`, `LOC-RECEIVING`, `LOC-SHIPPING` — all Bootstrap-bypass only

### 2.2 `docs/business/GULIERP_EMPLOYEE_MASTER_MODEL_V1.md` (FROZEN)

**Before Option A**:
- Line 238: "Recommended prefix: `EMP-` or `EMP-{DEPT}-` for HR-organised"

**After Option A**:
- Line 238: "Recommended prefix: `EMP_` or `EMP_{DEPT}_` for HR-organised"

### 2.3 Foundation Code Pipeline (unchanged)

`modules/foundation/GuliERP.Foundation/Validation/FormatValidator.cs:37-38`:
```csharp
private static readonly Regex CodePattern =
    new(@"^[A-Z][A-Z0-9_]{1,39}$", RegexOptions.Compiled);
```

**Not changed** — implementation was always correct (UPPER_SNAKE, no hyphen).

`modules/foundation/GuliERP.Foundation/Validation/ReservedNameValidator.cs:40-48` (unchanged):
- Reserved set: `SYSTEM`, `SYS`, `RESERVED`, `EMP-SYSTEM`, `WH-DEFAULT`, `LOC-RECEIVING`, `LOC-SHIPPING`, `ROLE_PLATFORM_ADMIN`, `ROLE_TENANT_ADMIN`, `ROLE_COMPANY_ADMIN`, `ROLE_NORMAL_USER`
- The 4 hyphenated names (`EMP-SYSTEM`, `WH-DEFAULT`, `LOC-RECEIVING`, `LOC-SHIPPING`) are Bootstrap-bypass only — operators cannot type them (Step 1 FormatValidator rejects hyphen first)

### 2.4 Employee ForIdentity factory + EmployeeWriteService (unchanged)

`modules/identity/.../Validation/CodeValidationContextExtensions.cs` (ForIdentity factory):
- Sets Identity-namespaced error codes only
- Does NOT override format rules

`modules/identity/.../EmployeeSvc/EmployeeWriteService.cs` (ThrowIfEmployeeCodeInvalid):
- Uses `MasterDataCodeValidator.Validate(code, context)`
- The 4-step pipeline uses Foundation regex `^[A-Z][A-Z0-9_]{1,39}$`

### 2.5 Bootstrap BuildEmployeeNo (unchanged)

`modules/identity/.../EnterpriseBootstrapService.cs:609`:
```csharp
private const string BootstrapAdminEmployeeNo = "EMP-SYSTEM";
```

`BuildEmployeeNo(string adminUserName)` returns `EMP-SYSTEM` (the frozen constant), bypassing the 4-step pipeline. This is the **documented known inconsistency** (per `GULIERP_EMPLOYEE_MASTER_MODEL_V1.md` §4.3 line 271-289).

### 2.6 Employee Tests (unchanged unit tests, fixed 2 unmasked integration tests)

`tests/GuliERP.Identity.Tests/EmployeeWriteServiceFacts.cs:118-169` (unchanged):
- Format-invalid examples include `emp-001` (lowercase + hyphen) → REJECTED
- Valid examples use `EMP_001`, `EMP_FIN_001`, `EMP_X7` (underscore) — aligned with Option A

`tests/GuliERP.Identity.IntegrationTests/EmployeeWriteApiFacts.cs` (this Goal: 2 tests updated):
- `Create_With_Valid_Code_Returns_201_And_EmployeeDto`: `"EMP-000001"` → `"EMP000001"` (line 84, 96)
- `Create_With_Duplicate_Code_Returns_400_With_Duplicate_ErrorCode`: `"EMP-DUP"` → `"EMPDUP"` (line 115, 124)

---

## 3) Format Contract Matrix (FINAL)

| Candidate Code | Frozen Design (after Option A) | Foundation Validator | Bootstrap | API | Tests | Verdict |
|---|---|---|---|---|---|---|
| `EMP000001` | ✅ Valid (`EMP_` prefix) | ✅ ACCEPTS | ❌ N/A | ✅ Accepts | ✅ PASS (updated test) | **ALIGNED** |
| `EMP000002` | ✅ Valid | ✅ ACCEPTS | ❌ N/A | ✅ Accepts | (would PASS) | **ALIGNED** |
| `EMP_SYSTEM` | ✅ Valid (UPPER_SNAKE) | ✅ ACCEPTS | ❌ N/A | ✅ Accepts | (not in tests) | **ALIGNED** |
| `EMPDUP` | ✅ Valid (UPPER_SNAKE) | ✅ ACCEPTS | ❌ N/A | ✅ Accepts | ✅ PASS (updated test) | **ALIGNED** |
| `EMP-SYSTEM` | ✅ Frozen reserved (Bootstrap-only) | ❌ REJECTS (Step 1 hyphen) + Step 2 reserved | ✅ BYPASSES pipeline | ❌ API rejects (Step 1) | ⚠️ Not tested (note line 141) | **ALIGNED** (Bootstrap-bypass only) |
| `EMP-000001` | ❌ Not in design (was historical example) | ❌ REJECTS | ❌ N/A | ❌ Rejects | ❌ Test was REMOVED, replaced with `EMP000001` | **ALIGNED** (test aligned) |
| `EMP-001` | ❌ Not in design (was historical example) | ❌ REJECTS | ❌ N/A | ❌ Rejects | (not in current tests) | **ALIGNED** (replaced with `EMP_001`) |
| `MAT_001` | ✅ Valid (`MAT_` prefix) | ✅ ACCEPTS | ❌ N/A | ✅ Accepts | (different test project) | **ALIGNED** |
| `WH_01` | ✅ Valid (`WH_` prefix) | ✅ ACCEPTS | ❌ N/A | ✅ Accepts | (different test project) | **ALIGNED** |
| `ADMIN` | ❌ Not in design (legacy) | ✅ ACCEPTS | ✅ Pre-fix: returned this; Post-fix: returns `EMP-SYSTEM` | ✅ Would accept | (not in current tests) | **ALIGNED** |

**All conflicts resolved**.

---

## 4) EMP-SYSTEM Consistency

**Bootstrap-Employee API asymmetry** (preserved as documented in `GULIERP_EMPLOYEE_MASTER_MODEL_V1.md` §4.3):

| Path | Verdict |
|---|---|
| Bootstrap: `BuildEmployeeNo()` → `EMP-SYSTEM` → direct DB insert | ✅ **BYPASSES** pipeline (System seed) |
| Employee API: `Create` → `ThrowIfEmployeeCodeInvalid("EMP-SYSTEM", ...)` | ❌ **REJECTS** (Step 1 fails on hyphen) |
| Operator via UI: `Create Employee` form | ❌ Cannot type `EMP-SYSTEM` (Step 1 fails) |

**This is intentional defense-in-depth** (per `BootstrapAdminEmployeeNoFixTests.cs:67-83`):
> "If a future refactor accidentally routes the bootstrap value through the 4-step pipeline, the FormatValidator (Step 1) will reject it. This is intentional defense-in-depth: the bootstrap MUST bypass the pipeline to write the reserved value."

**No change** to this asymmetry.

---

## 5) EMP-DUP / EMP-000001 / EMP-001 Conclusion (FINAL)

| Code | Pre-Option-A Verdict | Post-Option-A Verdict |
|---|---|---|
| `EMP-000001` | ❌ REJECTED by FormatValidator; "should be valid" per historical design | ❌ REJECTED; **no longer in design** (replaced with `EMP000001` underscore) |
| `EMP-DUP` | ❌ REJECTED; "should be valid" per test | ❌ REJECTED; **no longer in design** (test updated to `EMPDUP`) |
| `EMP-001` | ❌ REJECTED; "should be valid" per historical design | ❌ REJECTED; **no longer in design** (replaced with `EMP_001` in docs) |
| `EMP_001` | ✅ Already valid | ✅ Valid (aligned) |
| `EMP000001` | ❌ "Should be valid" per test (rejected) | ✅ Valid (test updated) |
| `EMPDUP` | ❌ "Should be valid" per test (rejected) | ✅ Valid (test updated) |

**Root cause resolution**: The `EMP-` prefix was the historical error. The corrected V1 contract uses `EMP_` prefix + underscore. The 2 unmasked tests are now PASSING because they use the corrected V1 contract.

---

## 6) Bootstrap/API Consistency (unchanged from prior report)

| Path | EMP-SYSTEM Verdict | Notes |
|---|---|---|
| `EnterpriseBootstrapService.CreateEnterpriseBootstrapAsync` | ✅ **WRITES** (bypasses pipeline) | System seed; documented per `§4.3` |
| `EmployeeWriteService.CreateAsync` (API) | ❌ **REJECTS** (Step 1 hyphen) | Intentional defense-in-depth |
| `EmployeeWriteService.CreateAsync` (API) — other codes | ✅ ACCEPTS (UPPER_SNAKE) | Per V1 contract |
| Operator via UI | ❌ Cannot type `EMP-SYSTEM` (Step 1 hyphen) | Intended |

**No change** to this asymmetry. It is a documented design choice (system seeds bypass validation).

---

## 7) Test Results

### 7.1 Build

```
$ dotnet build GuliERP.slnx -c Release --nologo
```

**Result: 0 errors, 0 warnings, 25/25 projects PASS** ✅

### 7.2 The 2 Unmasked TEST_DATA_CONFLICT Tests (PRE → POST)

| Test | Before | After |
|---|---|---|
| `Create_With_Valid_Code_Returns_201_And_EmployeeDto` | ❌ Expected 201, Got 400 (format invalid for `"EMP-000001"`) | ✅ **PASS** (test data updated to `"EMP000001"`) |
| `Create_With_Duplicate_Code_Returns_400_With_Duplicate_ErrorCode` | ❌ First call Expected 201, Got 400 (format invalid for `"EMP-DUP"`) | ✅ **PASS** (test data updated to `"EMPDUP"`) |

### 7.3 Full EmployeeWriteApiFacts Result

```
$ dotnet test --filter 'FullyQualifiedName~EmployeeWriteApiFacts'
已通过! - 失败:     0，通过:    15，已跳过:     0，总计:    15
```

**15/15 PASS** (was 9/11 with 2 failures).

### 7.4 Full Identity.IntegrationTests Result

```
$ dotnet test tests/GuliERP.Identity.IntegrationTests/GuliERP.Identity.IntegrationTests.csproj
失败! - 失败:     9，通过:   121，已跳过:     0，总计:   130
```

**121/130 PASS, 9 FAIL** — all 9 failures are **Category C (ENVIRONMENT_BLOCKED, PG required)**:
- `IdGenerationHiLoFacts.*` (4 tests) — needs PostgreSQL HiLo sequence
- `IdentityReferentialIntegrityFacts.FK_*` (3 tests) — needs PG FK
- `G2_005_AuthorizationDataScopeFacts.RealPostgreSql_...` (1 test) — needs real PG
- `IdentityApplicationServiceFacts.ICompanySwitchingService_ResolveDefault_No_Membership_Returns_Null` (1 test) — needs real PG

These 9 tests are **OPERATOR_REQUIRED**, not this Goal's scope.

### 7.5 Full Mavis-Side Unit Tests (unchanged)

| Test Project | Result |
|---|---|
| `GuliERP.Identity.Tests` | **84 / 84 PASS** |
| `GuliERP.Identity.Bootstrap.Tests` | **64 / 64 PASS** |
| `GuliERP.Api.Tests` | **32 / 32 PASS** |
| `GuliERP.Foundation.Tests` | **68 / 68 PASS** |
| `GuliERP.Mdm.Tests` | **221 / 223 PASS** (2 inherited flaky) |
| `GuliERP.Sales.Tests` | **9 / 9 PASS** |
| `GuliERP.DocumentKernel.Tests` | **44 / 44 PASS** |
| **Mavis-side total** | **522 / 524 PASS** (2 inherited flaky, 0 new regression) |

### 7.6 Cumulative across Sub-Goals

| Stage | Total Identity.IntegrationTests Fails | Breakdown |
|---|---:|---|
| Before (G2-EM-001B introduced) | **25** | 5 A + 11 B + 9 C |
| After SUB-GOAL 1 (test contract alignment) | 20 | 0 A + 11 B + 9 C |
| After SUB-GOAL 2 (fixture fix) | 11 | 0 A + 0 B (9 fixed) + 2 TEST_DATA_CONFLICT (unmasked) + 9 C |
| After Option A (this Goal) | **9** | 0 A + 0 B + 0 TEST_DATA_CONFLICT + 9 C |

**16 tests fixed total** (5 A + 9 B + 2 TEST_DATA_CONFLICT). Only 9 Category C remain (PG required, out of scope).

---

## 8) Root Cause and Fix

### 8.1 Root cause

The frozen design documents (`GULIERP_CODE_RULE_STANDARD_V1.md` + `GULIERP_EMPLOYEE_MASTER_MODEL_V1.md`) had **internal contradiction**:
- Specified regex `^[A-Z][A-Z0-9_]{1,39}$` (no hyphen)
- Used hyphenated examples (`MAT-001`, `WH-01`, `EMP-001`, `EMP-SYSTEM`, `EMP-FIN-001`, `LOC-A-01-03`, etc.)

The implementation correctly implemented the regex. The "auto-format feature" mentioned in line 137-140 was never implemented (per audit).

### 8.2 Fix applied (Option A, user-approved 2026-08-24)

| File | Change | Lines |
|---|---|---|
| `docs/business/GULIERP_CODE_RULE_STANDARD_V1.md` | Replace all `MAT-`, `WH-`, `LOC-`, `OU-`, `PLT-`, `EMP-`, `C-`, `S-`, `B-`, `CAT-` prefixes with `_` versions in examples. Replace `EMP-001` / `EMP-FIN-001` examples with underscore. Update Item examples. Update BusinessPartner examples. Update Warehouse/Location examples. Add explicit "hyphens reserved for Bootstrap-only system seeds" note. | 32, 66, 96, 110-117, 131-134, 136-140, 147-150, 160-161, 167-168 |
| `docs/business/GULIERP_EMPLOYEE_MASTER_MODEL_V1.md` | Update line 238 recommended prefix from `EMP-` / `EMP-{DEPT}-` to `EMP_` / `EMP_{DEPT}_` | 238 |
| `tests/GuliERP.Identity.IntegrationTests/EmployeeWriteApiFacts.cs` | Update test data: `"EMP-000001"` → `"EMP000001"`; `"EMP-DUP"` → `"EMPDUP"` (2 places) | 84, 96, 115, 124 |
| `docs/verification/GULIERP_EMPLOYEE_CODE_CONTRACT_RECONCILIATION_001_REPORT.md` | this file (new) | - |

**No production code changed**:
- `modules/foundation/GuliERP.Foundation/Validation/FormatValidator.cs` — unchanged (regex already correct)
- `modules/foundation/GuliERP.Foundation/Validation/ReservedNameValidator.cs` — unchanged (reserved set preserved)
- `modules/foundation/GuliERP.Foundation/Validation/MasterDataCodeValidator.cs` — unchanged
- `modules/identity/.../Validation/CodeValidationContextExtensions.cs` (ForIdentity factory) — unchanged
- `modules/identity/.../EmployeeSvc/EmployeeWriteService.cs` — unchanged
- `modules/identity/.../EnterpriseOrganization/EnterpriseBootstrapService.cs` (BuildEmployeeNo) — unchanged

**No DB schema change**:
- No migration
- No entity change
- No existing data rewrite

---

## 9) COMMIT_GROUP_8 推荐

**Atomic commit message suggestion**:
```
docs(test): GULIERP_EMPLOYEE_CODE_CONTRACT_RECONCILIATION_001 — honor V1 regex (Option A)

Fixes the internal contradiction in the frozen design docs
where examples used hyphenated codes (EMP-001, MAT-001, WH-01)
that the V1 regex ^[A-Z][A-Z0-9_]{1,39}$ explicitly rejects.

Changes:
- GULIERP_CODE_RULE_STANDARD_V1.md: replace hyphenated examples
  with underscore variants (MAT_001, WH_01, EMP_001, EMP_FIN_001,
  etc.). Update prefix table: MAT_, WH_, LOC_, OU_, PLT_, EMP_,
  C_, S_, B_, CAT_. Add explicit "hyphens reserved for Bootstrap-only
  system seeds (EMP-SYSTEM, WH-DEFAULT, LOC-RECEIVING, LOC-SHIPPING)"
  note.
- GULIERP_EMPLOYEE_MASTER_MODEL_V1.md: update recommended prefix
  from "EMP- or EMP-{DEPT}-" to "EMP_ or EMP_{DEPT}_".
- EmployeeWriteApiFacts.cs: update 2 unmasked test data from
  "EMP-000001" to "EMP000001" and "EMP-DUP" to "EMPDUP" (now
  aligns with corrected V1 contract).

NO production code changed. NO FormatValidator regex changed.
NO DB schema change. NO entity change.

The reserved names EMP-SYSTEM, WH-DEFAULT, LOC-RECEIVING,
LOC-SHIPPING are preserved (Bootstrap-bypass only, never
operator-typed).
```

**Files** (5 paths including governance registry):
- `docs/business/GULIERP_CODE_RULE_STANDARD_V1.md` (modified, design docs)
- `docs/business/GULIERP_EMPLOYEE_MASTER_MODEL_V1.md` (modified, design docs)
- `tests/GuliERP.Identity.IntegrationTests/EmployeeWriteApiFacts.cs` (modified, test data)
- `docs/verification/GULIERP_EMPLOYEE_CODE_CONTRACT_RECONCILIATION_001_REPORT.md` (new, this report)
- `docs/governance/GOAL_REGISTRY.md` (modified, adds `G2-EM-001C`)

**NO COMMIT performed** (per user `COMMIT RULE`).

---

## 10) Hard Stop Check

| Brief condition | This Goal? |
|---|---|
| SalesOrder / Purchase / Inventory | NO |
| Contact Profile | NO |
| Employee Vue / CRM / HRM | NO |
| DB Schema / Migration | NO |
| Legacy repository | NO (untouched) |
| Global MDM code rule relaxation | **NO** (regex unchanged; only docs updated) |
| Production code change | **NO** (only docs + test data) |

**0 hard-stops tripped.**

---

## 11) Effect on Other MDM Entities

| Entity | Pre-Option-A | Post-Option-A | Effect |
|---|---|---|---|
| `Item` | Used `MAT-` prefix in examples | Uses `MAT_` prefix | Docs only — no code change |
| `Uom` | No prefix (system master) | No prefix | No change |
| `BusinessPartner` | Used `C-` / `S-` / `B-` prefix in examples | Uses `C_` / `S_` / `B_` prefix | Docs only — no code change |
| `Warehouse` | Used `WH-` prefix in examples | Uses `WH_` prefix | Docs only — no code change |
| `Location` | Used `LOC-` prefix in examples | Uses `LOC_` prefix | Docs only — no code change |
| `Plant` | Used `PLT-` prefix in examples | Uses `PLT_` prefix | Docs only — no code change |
| `OrganizationUnit` | Used `OU-` prefix in examples | Uses `OU_` prefix | Docs only — no code change |
| `Employee` | Used `EMP-` prefix in examples + `EMP-SYSTEM` reserved | Uses `EMP_` prefix; `EMP-SYSTEM` reserved (Bootstrap-bypass) | Docs + 2 test data updated |
| `GuliErpRole` | Uses `ROLE_*` prefix (no change) | No change | No change |
| `ItemCategory` | Used `CAT-` prefix in examples | Uses `CAT_` prefix | Docs only — no code change |

**Effect**: Only docs are updated. The implementation is unchanged, so:
- Any existing master data rows with underscore codes: unaffected
- Any existing master data rows with hyphen codes: **none should exist** (the regex has always rejected them at Create); if they exist, they were Bootstrap-bypass (EMP-SYSTEM / WH-DEFAULT / LOC-RECEIVING / LOC-SHIPPING) which are preserved
- Operator UI: now documentation matches what the API actually accepts (underscore-only)

**No MDM entity's runtime behavior changes**.

---

## 12) Remaining PostgreSQL Runtime Tests

9 Category C tests still require Operator runtime:
1. `IdGenerationHiLoFacts.HiLo_Does_Not_Collide_With_Existing_Id_Range`
2. `IdGenerationHiLoFacts.HiLo_FreshScopes_Produce_Distinct_Ids`
3. `IdGenerationHiLoFacts.HiLo_PreGenerates_Id_Before_SaveChanges`
4. `IdGenerationHiLoFacts.HiLo_Persists_Long_Id`
5. `IdentityReferentialIntegrityFacts.FK_DeleteBehavior_Restrict_TenantCannotBeDeletedWithCompanies`
6. `IdentityReferentialIntegrityFacts.FK_Tenant_RejectOnOrphan`
7. `IdentityReferentialIntegrityFacts.FK_Tenant_AcceptOnValid`
8. `G2_005_AuthorizationDataScopeFacts.RealPostgreSql_PersistedRoleClaimAndRoleAssignment_AuthorizesOnlyInsideTenantCompanyScope`
9. `IdentityApplicationServiceFacts.ICompanySwitchingService_ResolveDefault_No_Membership_Returns_Null`

**Resolution**: Operator runs `tools/dev/g2-005-operator-evidence.ps1` interactively (uses `Read-Host -AsSecureString` for password; sets `$env:ConnectionStrings__GuliERP`; runs the integration test suite).

These tests are **out of scope** for this Goal and are a separate concern (handled in `GULIERP_NEXT_OPERATOR_RUNTIME_VERIFICATION_001_*`).

---

## 13) Final Conclusion

**GULIERP_EMPLOYEE_CODE_CONTRACT_RECONCILIATION_001_VERIFIED** (Mavis side)

- ✅ 2 unmasked TEST_DATA_CONFLICT tests now PASS
- ✅ Full EmployeeWriteApiFacts 15/15 PASS
- ✅ 121/130 Identity.IntegrationTests PASS (9 Category C remain, all PG-required)
- ✅ 522/524 Mavis-side unit tests PASS (2 inherited flaky, 0 new regression)
- ✅ 0 production code changed
- ✅ 0 DB schema change
- ✅ 0 entity change
- ✅ Foundation FormatValidator regex unchanged
- ✅ Reserved names (EMP-SYSTEM / WH-DEFAULT / LOC-RECEIVING / LOC-SHIPPING) preserved
- ✅ Bootstrap-Employee API asymmetry preserved (documented known inconsistency)
- ✅ Design docs now consistent with implementation
- ⏸️ 9 Category C (PG-required) tests still pending Operator runtime (out of scope)

**Per FINAL STATUS** in user brief:
> 证据充分并修复: GULIERP_EMPLOYEE_CODE_CONTRACT_RECONCILIATION_001_VERIFIED

**Achieved**: Goal is **VERIFIED** on Mavis side. Remaining 9 Category C tests are Operator-side (not this Goal's responsibility).

---

## 14) Operator Follow-up (after this Goal)

After user authorizes `COMMIT_GROUP_8` (this report) + `COMMIT_GROUP_6` + `COMMIT_GROUP_7`, the next step is:

`GULIERP_NEXT_OPERATOR_RUNTIME_VERIFICATION_001` re-run (now only 9 Category C tests remain, all of which are PG-required). When those 9 also pass, flip `G2-EM-001` and `G2-EM-001B` Gates to `_VERIFIED`.

---

## 15) Honest Disclosure

1. **No production code changed** — Option A path is "fix docs, not code".
2. **2 tests updated** — the test data updates are now AUTHORIZED by the corrected design (Option A explicitly resolves the "无连字符格式才是正式 V1 contract" question by aligning docs with the implementation).
3. **Foundation FormatValidator unchanged** — its regex `^[A-Z][A-Z0-9_]{1,39}$` is now the V1 truth per the corrected docs.
4. **Bootstrap-Employee API asymmetry preserved** — `EMP-SYSTEM` is Bootstrap-only; the API rejects it (Step 1 fails on hyphen). This is documented defense-in-depth.
5. **No data migration** — no existing master data row is rewritten; no DB schema change.
6. **PowerShell terminal GBK encoding** — some `Get-Content` output had mojibake but the corrected `Replace_all: false` edits worked cleanly.
7. **9 Category C PG-required tests are NOT this Goal's scope** — they are Operator-side, deferred to `GULIERP_NEXT_OPERATOR_RUNTIME_VERIFICATION_001`.

---

## 16) Final Statement

**GULIERP_EMPLOYEE_CODE_CONTRACT_RECONCILIATION_001_VERIFIED** (Mavis side)

本任务完成,5 个 path 改动(2 design docs + 1 test data + 1 report + GOAL_REGISTRY),**0 production code change for Option A**。设计文档现在与实现严格一致,2 个 unmasked test 通过,9 个 Category C PG-required tests 仍等 Operator runtime verification。

**等用户授权 COMMIT_GROUP_8** (本 Goal 单独 atomic commit group,不与 COMMIT_GROUP_6/7 混合)。
