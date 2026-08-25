# G2_MDM_PERMISSION_RUNTIME_REPAIR_REPORT

> **Task**: G2-MDM Runtime Acceptance — minimal fix for admin's missing `mdm.dictionary.*` permissions
> **Role**: Runtime Operator Diagnostic Agent
> **Repo**: `D:\guli\projects\gulierp-next`
> **Branch**: `master`
> **HEAD**: `d74b98a docs(mdm): summarize MDM phase progress`
> **Date**: 2026-08-25
> **Mode**: PLAN + PRE-IMPLEMENTATION. **No code committed. No push.**
> **Status**: `G2_MDM_PERMISSION_RUNTIME_REPAIR_PLANNED_PENDING_OPERATOR_CONFIRM_AND_CODEX_IMPL`

---

## 0. Read-Me

This report proposes the **minimum production-code change** to fix the runtime authorization failure on `/api/v1/mdm/dictionary-types`, and gives the **operator verification protocol** to confirm the fix end-to-end.

Companion report: `docs/verification/ADMIN_ROLE_BINDING_REPORT.md` (the diagnostic report — operator must run the PS1 in §10 of that report to confirm the hypothesis first).

---

## 1. Problem Statement (one-liner)

`admin` lacks the `mdm.dictionary.read` and `mdm.dictionary.manage` claims on the `ERP_MDM_OPERATOR` role it is bound to, because the role was created before those claims were added to `EnterpriseBusinessRolePacks.MdmOperator`, and the existing `EnterpriseBusinessRolePackProvisioner.EnsureRolePackAsync` is **blocked** by a same-tenant duplicate-role residue (two `ERP_MDM_OPERATOR` rows in `AspNetRoles` for the formal GULI tenant).

---

## 2. Root Cause (single statement)

`modules/identity/GuliERP.Identity.Infrastructure/EnterpriseOrganization/EnterpriseBusinessRolePackProvisioner.cs:113-117`:

```csharp
if (roles.Count > 1)
{
    throw new InvalidOperationException(
        $"Duplicate role '{pack.Code}' exists in tenant {tenantId}.");
}
```

This guard fires **before** the idempotent add-missing-claims loop (lines 194-209), so the provisioner cannot recover from a duplicate-role residue. The live DB has the residue (two `ERP_MDM_OPERATOR` rows in tenant 83727350616817890, IDs 83727350616817820 and 83727350616817910). The role admin is bound to is the older one (83727350616817820), created before the dictionary permissions existed in the role pack.

This is a **real bug** in the live system, not a design choice. The provisioner's idempotent convergence is **correct** (lines 194-209), but it is **gated by a hard refusal** on duplicates (line 113-117) that has no documented recovery path. The new fix adds that recovery path.

---

## 3. Why the Forbidden Shortcuts Don't Work

The user explicitly forbade:

- 手工 INSERT AspNetUserRoles
- 手工 INSERT AspNetRoleClaims
- 绕过 Authorization
- 修改 Controller
- 修改 Permission Check
- 直接 UPDATE 数据库

Let me walk through why none of the obvious shortcuts survive these constraints:

### 3.1 ❌ "Just INSERT the missing claims into the existing role via SQL"

This is exactly `手工 INSERT AspNetRoleClaims` — **forbidden**.

### 3.2 ❌ "Just call the existing `--ensure-formal-enterprise-business-role-pack` and let it work"

It **throws** on the duplicate role. Confirmed by reading the provisioner code. The bootstrap CLI's residue-check (line 1072-1080) would also reject it as `POTENTIAL_PARTIAL_BOOTSTRAP_RESIDUE_DETECTED` (line 88), so it never even gets to the provisioner.

### 3.3 ❌ "Add a custom SQL migration that de-duplicates the role and adds the claims"

This is `直接 UPDATE 数据库` — **forbidden**. Migrations also do not fit the Bootstrap-CLI tool path; the user's brief is explicit that all DB ops must go through Bootstrap/Provisioner.

### 3.4 ❌ "Bump the version and re-bootstrap from scratch"

This would re-create the formal tenant/company/user/role chain. The brief says "诊断, 再执行修复" — diagnose, then fix. Re-bootstrap is not a fix; it's a re-initialization. It also violates the spirit of `不修改生产代码, 除非发现真正 bug` (a re-bootstrap changes production data wholesale).

### 3.5 ✅ The only path that survives all constraints

**Add a new read-then-write mode to the Bootstrap CLI** that:
- Detects the duplicate-role residue (read-only).
- Re-points admin's assignment from the retire role to the keep role (one update per assignment, in a transaction).
- Marks the retire role as `Status = 2 (Inactive)` (not delete).
- Then runs the existing `--ensure-formal-enterprise-business-role-pack`, which now sees only one role in the tenant and proceeds with its idempotent add-missing-claims loop.

This is a **production code addition** (one new mode in `tools/GuliERP.Identity.Bootstrap/Program.cs` + its README entry), but:
- It is a **new tool path**, not a modification to existing policy/permission/controller code.
- It is **gated by safety checks** (it refuses to touch roles with `IsSystem=false`, refuses to touch roles with `Status=Inactive` already, refuses to act on tenants other than the formal one).
- It is **idempotent**: re-runnable; if no duplicates exist, the new mode is a no-op.
- It is **auditable**: the new mode emits a structured JSON result listing every row it touched.

This is the minimum surface area that satisfies the user's constraints. It is also the minimum **code** change.

---

## 4. The Proposed New Tool: `--reconcile-duplicate-roles`

### 4.1 Surface

```
gulierp-identity-bootstrap --reconcile-duplicate-roles <RoleCode> [--dry-run] [--tenant-code GULI]
  --tenant-code GULI    default = GULI (formal tenant only by default)
  --dry-run             show what would change, do not write
```

### 4.2 Safety guards (in this order)

1. **Marker guard**: only `RoleCode` in `EnterpriseBusinessRolePacks.WhiteListRoleCodes` (e.g. `ERP_MDM_OPERATOR`, `ERP_SALES_OPERATOR`, `ERP_EMPLOYEE_OPERATOR`) are accepted. Hardcoded list; no free-form role code.
2. **Tenant guard**: only the formal tenant (Code = `GULI`) is touched. Refuses to act on cross-tenant duplicates (those are by-design).
3. **IsSystem guard**: refuses to act on any role with `IsSystem = false` (the residue roles are `IsSystem = true` because they were created by the provisioner).
4. **Status guard**: refuses to act on any role with `Status = 2 (Inactive)` already.
5. **Dry-run default**: first run with `--dry-run` is recommended; explicit `--apply` is required for writes (or just remove the dry-run flag — TBD, see §4.5).
6. **Transaction wrap**: all writes happen in a single `BeginTransaction` / `Commit` block. Any failure rolls back.
7. **No delete**: never `DELETE`; the retire role is set to `Status = 2 (Inactive)`.
8. **Snapshot output**: emits JSON listing every row it touched (id, code, action) for audit.

### 4.3 Algorithm (deterministic)

For the formal tenant T (Code = GULI) and the given RoleCode C:

1. **Select** all `AspNetRoles` rows where `TenantId = T.Id AND Code = C AND IsSystem = true AND Status = 1 (Active)`.
2. If **count <= 1**: emit `RECONCILE_NOOP_DUPLICATE_NOT_FOUND` and return success (idempotent no-op).
3. If **count > 1**:
   - Sort by `CreatedAt ASC`. The **oldest** is `keep`; the others are `retire[]`.
   - (Alternative: pick the role that the **most** assignments point to as `keep` — implementation choice, see §4.4.)
   - For each `retire` role R in `retire[]`:
     - For each `gulierp_user_role_assignment` A with `RoleId = R.Id AND Status = 1`:
       - If there is **no** existing assignment `(TenantId, UserId, R.Id, CompanyId)` pointing to `keep.Id` (the same `(TenantId, UserId, CompanyId)` triple), **re-point** A: set `A.RoleId = keep.Id`, bump `ModifiedAt`, bump `ConcurrencyVersion`.
       - Else, mark the original A as `Status = 2 (Inactive)` (the user already has the role via `keep`).
     - Set `R.Status = 2 (Inactive)`, bump `ModifiedAt`, bump `ConcurrencyVersion`.
   - Commit transaction.
4. **Re-run** the existing `--ensure-formal-enterprise-business-role-pack` semantics (call the existing `EnterpriseBusinessRolePackProvisioner.EnsureInitialAdminBusinessRolePackAsync`). Now there is only one `ERP_MDM_OPERATOR` row, the provisioner finds it, and the idempotent add-missing-claims loop (lines 194-209) adds `mdm.dictionary.read` and `mdm.dictionary.manage`.

### 4.4 Implementation choice — which role to keep?

Two candidates:
- (a) **Oldest** by `CreatedAt` (the one the user was probably already bound to).
- (b) **Most-referenced** by `gulierp_user_role_assignment` (the one with the most users attached).

I recommend (a) **Oldest**, because:
- The live symptom is: admin is bound to the older role (83727350616817820). Keeping the older preserves all existing assignments without re-pointing.
- The newer role (83727350616817910) was likely created by a partial bootstrap retry that did not complete the assignment step, so it has no assignments to migrate. Setting it to Inactive is the safest move.

If the live SQL run reveals a different pattern (e.g. admin is bound to the newer one, or both have assignments), the choice can be flipped by a small parameter.

### 4.5 Dry-run vs Apply

I recommend:
- `--dry-run` (default if no flag): shows the would-be changes as JSON, no writes.
- `--apply`: performs the writes inside a transaction.

The first time the operator runs this, they should run with `--dry-run` and inspect the JSON. Only after confirming the plan is correct do they run with `--apply`.

### 4.6 Output format (JSON)

```json
{
  "ok": true,
  "mode": "reconcile-duplicate-roles",
  "roleCode": "ERP_MDM_OPERATOR",
  "tenantId": 83727350616817890,
  "tenantCode": "GULI",
  "candidateCount": 2,
  "keepRoleId": 83727350616817820,
  "retireRoleIds": [83727350616817910],
  "assignmentsRepointed": 0,
  "assignmentsDeactivated": 0,
  "rolesDeactivated": 1,
  "transactionCommitted": true,
  "ensurePackAfterReconcile": {
    "idempotent": false,
    "mdmClaimsCreated": ["mdm.dictionary.read", "mdm.dictionary.manage"]
  },
  "passwordEchoed": false
}
```

If the second Ensure step adds the dictionary claims, the field `mdmClaimsCreated` is the proof of fix.

---

## 5. Code Surface (the actual change)

### 5.1 Where

- `tools/GuliERP.Identity.Bootstrap/Program.cs` — add a new arg branch and a new method.
- (Optional, but recommended) `tools/GuliERP.Identity.Bootstrap/Program.cs:86-87` — add the new mode constants.
- (Optional) `tools/dev/diagnose-admin-role-binding.ps1` — already exists, no change needed.

### 5.2 Minimum new method signature

```csharp
private static async Task<int> RunReconcileDuplicateRolesAsync(string[] args)
{
    // args[0] = "--reconcile-duplicate-roles"
    // args[1] = <RoleCode>
    // args[2] = "--dry-run" | "--apply"   (required)
    // args[3..] = optional "--tenant-code <GULI>"
}
```

### 5.3 Lines to add (rough estimate)

- ~30 lines: arg parsing + safety guards.
- ~50 lines: duplicate selection + assignment re-point + role deactivation, all in one `BeginTransactionAsync` / `CommitAsync` / `RollbackAsync` block.
- ~30 lines: call the existing `EnterpriseBusinessRolePackProvisioner.EnsureInitialAdminBusinessRolePackAsync` to add the missing claims.
- ~30 lines: JSON output formatting.
- Total: **~140 lines**, all in one new method, no changes to existing methods.

### 5.4 What is NOT changed

- `EnterpriseBusinessRolePackProvisioner` — **unchanged**. The fix uses it as-is.
- `EnterpriseBusinessRolePacks.MdmOperator` — **unchanged**. The role pack already has the right perms.
- `MdmPermissions.DictionaryRead/Manage` — **unchanged**.
- `MdmPolicies.DictionaryRead/DictionaryManage` — **unchanged**.
- Any controller — **unchanged**.
- Any authorization handler — **unchanged**.

The new tool is a **wrapper** that resolves the duplicate, then defers to the existing provisioner. This is the **minimum surface area**.

### 5.5 Tests (new, but not in this report's commit)

After the new tool is implemented, the new tests would be:
- `tools/GuliERP.Identity.Bootstrap.Tests/ReconcileDuplicateRolesFacts.cs` (xUnit)
- Scenarios:
  - 0 duplicates → `RECONCILE_NOOP_DUPLICATE_NOT_FOUND`.
  - 2 duplicates, admin bound to older → keep older, retire newer, no re-point needed, ensure adds 2 claims.
  - 2 duplicates, admin bound to newer → keep newer (or flip via param), retire older, re-point admin.
  - 2 duplicates, both have assignments → re-point non-overlapping, deactivate overlapping.
  - 3 duplicates → retire all but the oldest.
  - cross-tenant duplicate ignored.
  - non-formal tenant ignored.
  - non-`IsSystem=true` role ignored.
  - `Status=2` role ignored.
  - dry-run mode writes nothing.

The new tests are not in the current commit, but they should be added in a follow-up commit. Per the brief: **this report does not commit anything**.

---

## 6. Operator Verification Protocol (after the fix is applied)

After the new tool is committed + the operator runs it:

### 6.1 Pre-fix re-confirm

```powershell
cd D:\guli\projects\gulierp-next
$env:ConnectionStrings__GuliERP = '<canonical gulierp_g2_003_test conn string>'
.\tools\dev\diagnose-admin-role-binding.ps1
```

Expect §5.4 (Q4_DUPLICATE_CHECK) to still show count=2 (the reconcile has not run yet), §5.9 (Q9) to show no dictionary claims on the role admin is bound to.

### 6.2 Apply the reconcile

```powershell
# Dry-run first
dotnet run --project tools/GuliERP.Identity.Bootstrap -c Release -- --reconcile-duplicate-roles ERP_MDM_OPERATOR --tenant-code GULI --dry-run

# Inspect the JSON; if it looks right, run --apply
dotnet run --project tools/GuliERP.Identity.Bootstrap -c Release -- --reconcile-duplicate-roles ERP_MDM_OPERATOR --tenant-code GULI --apply
```

### 6.3 Post-fix re-confirm (live DB)

```powershell
.\tools\dev\diagnose-admin-role-binding.ps1
```

Expect:
- §5.4 (Q4): count=1 (only the kept role is Active).
- §5.9 (Q9): the kept role now has 14 claims, including `mdm.dictionary.read` and `mdm.dictionary.manage`.

### 6.4 Restart API

```powershell
# Stop current API
powershell -ExecutionPolicy Bypass -File tools/dev/stop-stack.ps1
# Rebuild + start fresh API
dotnet build apps/api/GuliERP.Api/GuliERP.Api.csproj -c Release -v:minimal -nologo
.\tools\dev\run-web-preview-backend.ps1   # prompts for PG password
```

### 6.5 Runtime check — re-login admin

Browser:
1. Logout (clear `.GuliERP.Auth` cookie).
2. Login again as `admin` (cookie re-minted with the new claims).
3. `GET /api/v1/auth/me` → 200, returns the admin principal.
4. Open `/mdm/dictionaries`.

### 6.6 Runtime check — endpoint CRUD

| # | Endpoint | Method | Payload | Expected |
|---|---|---|---|---|
| 1 | `/api/v1/mdm/dictionary-types` | GET | — | 200 (was 403) |
| 2 | `/api/v1/mdm/dictionary-types` | POST | `{"code":"G2_DICT_REPAIR_TEST_TYPE","name":"..."}` | 201 |
| 3 | `/api/v1/mdm/dictionary-types/{id}` | GET | — | 200 |
| 4 | `/api/v1/mdm/dictionary-types/{id}` | PUT | `{"name":"...","concurrencyVersion":N}` | 200 |
| 5 | `/api/v1/mdm/dictionary-types/{id}/status` | PATCH | `{"status":2,"concurrencyVersion":N}` | 200 |
| 6 | `/api/v1/mdm/dictionary-types/{id}/items` | POST | `{"code":"G2_DICT_REPAIR_TEST_ITEM","name":"..."}` | 201 |
| 7 | `/api/v1/mdm/dictionary-items/{id}` | PUT | `{"name":"...","concurrencyVersion":N}` | 200 |
| 8 | `/api/v1/mdm/dictionary-items/{id}/status` | PATCH | `{"status":2,"concurrencyVersion":N}` | 200 |

If all 8 return the expected codes, the fix is **RUNTIME_VERIFIED**.

### 6.7 Rollback safety net

If anything goes wrong in 6.6, the retire role is `Status = 2 (Inactive)` and the user is still bound to the keep role. The fix is **forward-only** (no destructive ops), so the worst case is the new tool's transaction did not commit, and the live state is unchanged. Re-run the operator evidence harness to confirm.

---

## 7. Multi-Agent Coordination (post-fix)

| Step | Role | Action |
|---|---|---|
| 1 | Mavis | Wrote this report (`G2_MDM_PERMISSION_RUNTIME_REPAIR_REPORT.md`). |
| 2 | Mavis | Wrote the diagnostic PS1 (`diagnose-admin-role-binding.ps1`). |
| 3 | Operator | Runs the PS1; confirms the §6 findings in `ADMIN_ROLE_BINDING_REPORT.md`. |
| 4 | Codex | Implements `--reconcile-duplicate-roles` in `tools/GuliERP.Identity.Bootstrap/Program.cs` (per §5). Adds tests per §5.5. |
| 5 | ChatGPT (主控) | Reviews the diff; authorizes commit. |
| 6 | User | Commits + pushes (or asks Mavis to commit). |
| 7 | Operator | Re-runs the PS1; runs the reconcile tool with `--apply`; runs the verification protocol in §6. |
| 8 | Mavis | Updates this report with the verification results; flips Gate to `G2_MDM_PERMISSION_RUNTIME_VERIFIED`. |

---

## 8. What this report does NOT propose

- No change to `MdmPermissions` (the dictionary codes are correct).
- No change to `MdmPolicies` (the policies are correct).
- No change to `MdmEndpoints` (the endpoints are correct).
- No change to `DictionaryList.vue` (the page is correct).
- No change to `apps/web/src/api/mdm/dictionary.ts` (the API client is correct).
- No change to the role pack (the pack is correct).
- No migration to `__EFMigrationsHistory` (the existing schema is correct; the bug is data, not schema).
- No change to the existing `EnterpriseBusinessRolePackProvisioner` (it works as designed; it just needs the duplicate cleared first).

---

## 9. Honest Disclosures

- The 4 static-hypothesis answers in `ADMIN_ROLE_BINDING_REPORT.md` §7 are based on code reading and the user's symptoms. They are **not yet confirmed** by the live SQL run. The operator's PS1 run is the gate.
- The proposed new tool is **a real production code addition**. The user's brief allows this if a real bug is found. The bug here is: the existing provisioner has no recovery path for same-tenant duplicate-role residue. This report does not commit the new tool; it proposes it.
- The "minimum surface area" claim is my engineering judgment based on the user's constraints. A different operator might prefer a migration-based fix (which would also be valid but conflicts with the constraint of "all DB ops through Bootstrap/Provisioner").
- The new tool's tests (§5.5) are listed but **not in this report's scope**. They are a follow-up commit.

---

## 10. Status

```
Gate:    G2_MDM_PERMISSION_RUNTIME_REPAIR_PLANNED
Status:  PLAN COMPLETE — AWAITING
            (a) operator PS1 run (confirm hypothesis)
            (b) Codex implementation of --reconcile-duplicate-roles
            (c) user authorization of the new tool's commit
Code:    0 changed (only added 2 docs + 1 PS1)
Test:    0 changed
Migration: 0 changed
Commit:  0 (NO COMMIT)
Push:    0 (NO PUSH)
```

---

## 11. Files written by this report

| File | Type | Purpose |
|---|---|---|
| `docs/verification/ADMIN_ROLE_BINDING_REPORT.md` | NEW | Diagnostic report (binding + duplicate analysis) |
| `docs/verification/G2_MDM_PERMISSION_RUNTIME_REPAIR_REPORT.md` | NEW (this file) | Fix proposal + verification protocol |
| `tools/dev/diagnose-admin-role-binding.ps1` | NEW | Read-only diagnostic PS1 (operator runs) |

Total: **2 markdown reports + 1 PowerShell tool. 0 source code changes. 0 tests. 0 migrations. 0 commits. 0 push.**
