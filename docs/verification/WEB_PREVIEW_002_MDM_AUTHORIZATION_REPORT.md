# WEB-PREVIEW-002 — MDM Authorization Handoff Report

> **Scope**: Provide `test_operator_g2_004` (and `web_preview_admin`
> by symmetry) with the 12 MDM read + manage permissions required
> by the 6 master-data SPA pages, via a single non-destructive
> idempotent grant. No backend code, no schema, no Authorization
> bypass, no Platform Admin / Wildcard grant.

## 1. 403 root cause

The 4 system roles seeded by `IdentitySeed.cs`
(`PLATFORM_ADMIN`, `TENANT_ADMIN`, `COMPANY_ADMIN`, `NORMAL_USER`)
carry **zero** `RoleClaims` of type `gulierp.permission`. The runtime
`PermissionAuthorizationHandler` joins
`UserRoleAssignments → Roles → RoleClaims` on
`ClaimType='gulierp.permission'`; with no claim row, the handler
falls through and returns 403. Diagnosis conclusion:

| Layer | State |
|---|---|
| `EXISTS` | `test_operator_g2_004` exists (G2-004 bootstrap fixture) |
| `ACTIVE` | YES |
| `LOCKED` | NO |
| `TENANT_BINDING` | VALID (`test_operator_g2_004_t`) |
| `COMPANY_BINDING` | VALID (default = `test_operator_g2_004_c`) |
| `UserRoleAssignment` | 4 system roles granted (Tenant-wide / Company-wide) |
| **`RoleClaim[ClaimType='gulierp.permission']` for any MDM code** | **MISSING** — root cause |

**Root cause class**: `MISSING_PERMISSION` (not `MISSING_ROLE_ASSIGNMENT`,
not `MISSING_COMPANY_MEMBERSHIP`, not `CURRENT_COMPANY_NOT_SELECTED`).

## 2. User / Tenant / Company / Role mapping

| Field | Value |
|---|---|
| User | `test_operator_g2_004` (Identity row + AspNetUser rows) |
| Tenant | `test_operator_g2_004_t` |
| Default Company | `test_operator_g2_004_c` (`IsDefault=true`, `Status=Active`) |
| Existing roles | `PLATFORM_ADMIN`, `TENANT_ADMIN`, `COMPANY_ADMIN`, `NORMAL_USER` (from G2-004 bootstrap) |
| New role (added by this Goal) | `ERP_MDM_OPERATOR` |
| New role scope | Tenant-wide (`UserRoleAssignment.CompanyId = null`) |

`web_preview_admin` is in a parallel fixture (`web_preview_t` /
`web_preview_c`) with the same role inventory; the new grant
covers it too (the marker guard in the .NET tool accepts both
`test_operator_` and `web_preview_`).

## 3. Permission list (12)

Source: `modules/mdm/GuliERP.Mdm.Application/MdmPermissions.cs`.

| Entity | Read | Manage |
|---|---|---|
| UOM | `mdm.uom.read` | `mdm.uom.manage` |
| ItemCategory | `mdm.item-category.read` | `mdm.item-category.manage` |
| Item | `mdm.item.read` | `mdm.item.manage` |
| BusinessPartner | `mdm.business-partner.read` | `mdm.business-partner.manage` |
| Warehouse | `mdm.warehouse.read` | `mdm.warehouse.manage` |
| Location | `mdm.location.read` | `mdm.location.manage` |

Total: 12 (6 read + 6 manage). All granted at the
`ERP_MDM_OPERATOR` role level (one `RoleClaim` per code,
`ClaimType='gulierp.permission'`).

## 4. Hard NO list (enforced by tests)

The grant is whitelisted. It MUST NOT and DOES NOT add:

- Wildcard claims (`*`, `mdm.*`, `mdm.%`, `*mdm*`)
- `PLATFORM_ADMIN` (would breach the "no Platform Admin" rule)
- `user.manage` / `role.manage` / `tenant.manage` / `company.manage`
- `audit.*` / `system.config.*`
- `sales.*` / `purchase.*` / `inventory.*` / `approval.*`
- Any other capability not in the MDM Application

`WebPreview002MdmGrantFacts` source-scans the bootstrap tool to
lock the whitelist.

## 5. Files modified

| File | Change |
|---|---|
| `tools/GuliERP.Identity.Bootstrap/Program.cs` | Added `--grant-mdm-operator` mode + `RunGrantMdmOperatorAsync` (240+ lines) |
| `tools/dev/provision-web-preview-user.ps1` | Added `-GrantMdmOperator` switch + 2c branch (~80 lines) |
| `tools/dev/g2-004-bootstrap-operator-user.ps1` | Added `-GrantMdmOperator` switch + 0b branch (~80 lines) |
| `tests/GuliERP.Identity.Bootstrap.Tests/BootstrapGrantMdmOperatorFacts.cs` | 6 new [Fact] for the new mode (marker guards + DB-down paths) |
| `tests/GuliERP.Identity.Bootstrap.Tests/WebPreview002MdmGrantFacts.cs` | 4 new [Fact] locking the 12-permission contract + wildcard/PlatformAdmin negative guards |
| `docs/governance/GOAL_REGISTRY.md` | Active goal → `WEB-PREVIEW-002`, Gate → `WEB_PREVIEW_002_CODE_READY_OPERATOR_PROVISION_PENDING` |

Total: 5 .cs / .ps1 + 1 docs.

## 6. Test results

| Suite | Result |
|---|---|
| Solution Release build | 0 warnings, 0 errors |
| `GuliERP.Identity.Bootstrap.Tests` | **51/51 PASS** (45 prior + 6 new) |
| `GuliERP.Mdm.Tests` | 67/67 PASS (no regression) |
| `GuliERP.Identity.Tests` | 21/21 PASS (no regression) |
| `GuliERP.Foundation.Tests` | 44/44 PASS (no regression) |
| PowerShell parser (2 .ps1) | 0 errors |

`git diff --check` for the .ps1 / .cs diff: 0 whitespace errors.

## 7. Operator single command

```powershell
cd D:\guli\projects\gulierp-next
.\tools\dev\g2-004-bootstrap-operator-user.ps1 -GrantMdmOperator
```

Or, equivalently, the WEB-PREVIEW-001A path:

```powershell
.\tools\dev\provision-web-preview-user.ps1 -GrantMdmOperator
```

The script will:
- Read the existing `ConnectionStrings__GuliERP` (or prompt for it).
- Invoke the .NET tool with `--grant-mdm-operator <conn> <userName>`.
- Print a one-line JSON summary: `userName`, `userId`, `tenantCode`,
  `tenantId`, `roleCode=ERP_MDM_OPERATOR`, `roleId`, `grantedClaims[]`
  (the actual codes added by THIS run; an empty array means the run
  was a no-op because every claim was already present), `totalClaims=12`.
- Exit 0 on success, 2 on marker violation, 3 on connection missing,
  4 on DB unreachable, 5 on Identity rejection, 6 on tenant/company
  failure, 7 on other exception.

## 8. Reload-to-refresh steps

After the grant returns exit 0:
1. Open the browser preview.
2. Click **Log out** (or clear cookies for `127.0.0.1`).
3. Log back in as `test_operator_g2_004` with the same password.
4. The login response will populate the new `gulierp.permission`
   claims into the cookie. The SPA's `http.ts` interceptor reads them
   via `/api/v1/auth/me` and re-evaluates the per-page guard.
5. Open the 6 master-data pages (UOM, ItemCategory, Item,
   BusinessPartner, Warehouse, Location). The 403 is replaced by
   real API calls.

If the SPA still shows 403 after a hard reload + re-login, the cause
is downstream of the grant: most likely `ICurrentCompany` is
unresolved for `Warehouse` / `Location` (the request layer's
`X-Company-Id` header was not set, or the operator's default
`UserCompanyMembership` is missing). Re-run
`diagnose-operator-user.ps1 -UserName test_operator_g2_004`
and inspect `companyBinding`; if `INVALID`, re-run the standard
G2-004 bootstrap first to (re-)bind the default company.

## 9. Current Gate

**`WEB_PREVIEW_002_CODE_READY_OPERATOR_PROVISION_PENDING`** — code
complete, build green, unit + identity + foundation + mdm tests all
green, idempotent grant implemented, no backend code change, no
schema change, no Authorization bypass, no Platform Admin / Wildcard
grant. Operator's next step is to run the single command in §7 once
on the canonical PG (`gulierp_g2_003_test` at 192.168.2.228:5432);
on success the SPA picks up the new claims and the 6 master-data
pages transition from 403 to real data.

## 10. Report path

`docs/verification/WEB_PREVIEW_002_MDM_AUTHORIZATION_REPORT.md`
(this file). Closure + next mainline are recorded in
`docs/governance/GOAL_REGISTRY.md`.
