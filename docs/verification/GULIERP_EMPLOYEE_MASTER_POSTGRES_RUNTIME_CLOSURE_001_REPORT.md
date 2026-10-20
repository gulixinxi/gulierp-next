# GULIERP_EMPLOYEE_MASTER_POSTGRES_RUNTIME_CLOSURE_001_REPORT

Date: 2026-08-27

Final status:

`GULIERP_EMPLOYEE_MASTER_POSTGRES_RUNTIME_CLOSURE_001_VERIFIED`

Superseding Operator evidence update:

- Operator run: `pwsh ./tools/dev/g2-005-operator-evidence.ps1`
- Operator console final: `[G2-005] OPERATOR EVIDENCE HARNESS - ALL CHECKS PASS`
- Repository TRX evidence: `tests/_evidence_trx/g2-005/20260827-003945`
- TRX total: `396/396 PASS`
- `GuliERP.Identity.IntegrationTests`: `130/130 PASS`
- `GuliERP.Foundation.IntegrationTests`: `31/31 PASS`
- Previous `121/130 PASS` plus `9 OPERATOR_DB_ENVIRONMENT_BLOCKED` state: `RESOLVED_BY_OPERATOR_EVIDENCE`

Evidence note:

- The 20260827 evidence directory contains per-suite TRX/stdout/stderr files.
- It does not contain a full harness transcript with Step5/Step6 probe lines.
- The final closure report records Step5/Step6 as Operator console evidence plus script-order corroboration.

## 1. Repo / branch / HEAD

- Repository: `D:/guli/projects/gulierp-next`
- Branch: `master`
- HEAD: `139fe1e940258d71b85d328d88b4e1358c0f7b1e`
- Latest commit: `139fe1e docs(verification): add G3 R2B purchase order runtime report`

Canonical repository guard:

- `git rev-parse --show-toplevel` -> `D:/guli/projects/gulierp-next`
- `git branch --show-current` -> `master`

No legacy repository was accessed.

## 2. Working tree pre-state

Pre-existing / pending WIP was preserved. No clean/reset/stash/restore was run.

Observed working tree entries:

- `M docs/governance/GOAL_REGISTRY.md`
- `M docs/verification/GULIERP_EMPLOYEE_CODE_CONTRACT_RECONCILIATION_001_REPORT.md`
- `M tests/GuliERP.Api.Tests/SalesRuntimeRegressionSourceFacts.cs`
- `M tests/GuliERP.Identity.IntegrationTests/EnterpriseBootstrapAndOrganizationTreeFacts.cs`
- `M tests/GuliERP.Identity.IntegrationTests/EnterpriseRolePackCrossTenantFacts.cs`
- `?? docs/verification/GULIERP_FORMAL_ADMIN_ROLEPACK_CONTRACT_ALIGNMENT_001_REPORT.md`
- `?? docs/verification/GULIERP_NEXT_EMPLOYEE_OPERATOR_RUNTIME_FINAL_001_REPORT.md`
- `?? tools/dev/.quarantine/`

The three modified test files and the formal-admin alignment report belong to
`COMMIT_GROUP_9 Formal Admin Role-Pack Contract Alignment`.

This report is the only file added by the current runtime-closure Goal so far.

## 3. PostgreSQL target

The target below was confirmed from the repository harness
`tools/dev/g2-005-operator-evidence.ps1`, not only from the prompt:

- `TARGET_HOST = 192.168.2.228`
- `TARGET_PORT = 5432`
- `TARGET_DATABASE = gulierp_g2_003_test`
- `TARGET_USERNAME = gulidata`
- `ENVIRONMENT = TEST`

Password handling:

- The harness prints `Password=***`.
- The harness reads PostgreSQL password via `Read-Host -AsSecureString`.
- No password was printed, saved, committed, or written to this report.

Forbidden targets:

- Production database: NOT used
- Unknown database: NOT used
- Historical `gulierp` database: rejected by test guard

## 4. Exact 9 previously blocked tests now resolved

Historical pre-Operator inventory:

- Total: 130
- Passed: 121
- Failed / blocked: 9
- Skipped: 0

20260827 Operator inventory:

- Total: 130
- Passed: 130
- Failed / blocked: 0
- Skipped: 0

The 9 previously blocked PostgreSQL Operator Runtime tests are now resolved:

| # | Fully-qualified test | Source file | Why PostgreSQL is required | Writes DB | Cleanup | Gate |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `GuliERP.Identity.IntegrationTests.IdGenerationHiLoFacts.HiLo_PreGenerates_Id_Before_SaveChanges` | `tests/GuliERP.Identity.IntegrationTests/IdGenerationHiLoFacts.cs` | EF/Npgsql HiLo obtains sequence ranges from PostgreSQL before insert | Adds unsaved Tenant candidate; no SaveChanges | Not needed | ID-GEN-001 HiLo |
| 2 | `GuliERP.Identity.IntegrationTests.IdGenerationHiLoFacts.HiLo_Persists_Long_Id` | `tests/GuliERP.Identity.IntegrationTests/IdGenerationHiLoFacts.cs` | Verifies sequence-backed long Id persists/reloads in real PG | Yes, inserts Tenant | Best-effort delete by Tenant Id | ID-GEN-001 HiLo |
| 3 | `GuliERP.Identity.IntegrationTests.IdGenerationHiLoFacts.HiLo_FreshScopes_Produce_Distinct_Ids` | `tests/GuliERP.Identity.IntegrationTests/IdGenerationHiLoFacts.cs` | Verifies real PG HiLo allocation across fresh scopes | Adds unsaved Tenant candidates; no SaveChanges | Not needed | ID-GEN-001 HiLo |
| 4 | `GuliERP.Identity.IntegrationTests.IdGenerationHiLoFacts.HiLo_Does_Not_Collide_With_Existing_Id_Range` | `tests/GuliERP.Identity.IntegrationTests/IdGenerationHiLoFacts.cs` | Reads current max Id range and verifies generated HiLo Id is above it | Adds unsaved Tenant candidate; no SaveChanges | Not needed | ID-GEN-001 HiLo |
| 5 | `GuliERP.Identity.IntegrationTests.IdentityReferentialIntegrityFacts.FK_Tenant_RejectOnOrphan` | `tests/GuliERP.Identity.IntegrationTests/IdentityReferentialIntegrityFacts.cs` | Requires PostgreSQL FK enforcement | Attempts invalid Company insert | Insert should fail; no persisted row expected | G2-003V2 referential integrity |
| 6 | `GuliERP.Identity.IntegrationTests.IdentityReferentialIntegrityFacts.FK_Tenant_AcceptOnValid` | `tests/GuliERP.Identity.IntegrationTests/IdentityReferentialIntegrityFacts.cs` | Requires PostgreSQL FK enforcement and persistence | Yes, inserts Tenant + Company | Best-effort SQL delete Company then Tenant | G2-003V2 referential integrity |
| 7 | `GuliERP.Identity.IntegrationTests.IdentityReferentialIntegrityFacts.FK_DeleteBehavior_Restrict_TenantCannotBeDeletedWithCompanies` | `tests/GuliERP.Identity.IntegrationTests/IdentityReferentialIntegrityFacts.cs` | Requires PostgreSQL restrict delete behavior | Yes, inserts Tenant + Company and attempts restricted delete | Best-effort SQL delete Company then Tenant | G2-003V2 referential integrity |
| 8 | `GuliERP.Identity.IntegrationTests.IdentityApplicationServiceFacts.ICompanySwitchingService_ResolveDefault_No_Membership_Returns_Null` | `tests/GuliERP.Identity.IntegrationTests/IdentityApplicationServiceFacts.cs` | Verifies real PostgreSQL query path instead of placeholder connection | No direct test-data write observed | Not needed | G2-001R1 company switching |
| 9 | `GuliERP.Identity.IntegrationTests.G2_005_AuthorizationDataScopeFacts.RealPostgreSql_PersistedRoleClaimAndRoleAssignment_AuthorizesOnlyInsideTenantCompanyScope` | `tests/GuliERP.Identity.IntegrationTests/G2_005_AuthorizationDataScopeFacts.cs` | Requires persisted RoleClaim + RoleAssignment authorization through real PG | Yes, creates tenants, companies, user, role, memberships, claims, assignments | Best-effort entity removal by fixture Ids | G2-005 authorization data scope |

Current failure classification:

- `RESOLVED_BY_OPERATOR_EVIDENCE`
- Observed examples:
  - `Wrong DB target for ID-GEN-001. Expected gulierp_g2_003_test; actual gulierp.`
  - `NpgsqlException`: host name could not be resolved
  - explicit real PostgreSQL connection required messages

Closed categories that are not current blockers:

- `STALE_TEST_CONTRACT = 0`
- `TEST_FIXTURE_DEFECT = 0`
- `TEST_DATA_CONFLICT = 0`

## 5. Release Build result

Command:

```powershell
dotnet build GuliERP.slnx -c Release --nologo -m:1
```

Result:

- PASS
- 0 warnings
- 0 errors

## 6. Full Identity Integration result

Command:

```powershell
dotnet test tests/GuliERP.Identity.IntegrationTests/GuliERP.Identity.IntegrationTests.csproj -c Release --no-build --no-restore --nologo
```

Result:

- Total: 130
- Passed: 130
- Failed: 0
- Skipped: 0

The previous 9 PostgreSQL Operator Runtime environment/target failures are
resolved by the 20260827 Operator TRX evidence.

## 7. Formal Bootstrap runtime evidence

Current status:

`VERIFIED_BY_OPERATOR_EVIDENCE`

## 8. Initial Admin 5-role evidence

Static/test-contract evidence is already available from
`GULIERP_FORMAL_ADMIN_ROLEPACK_CONTRACT_ALIGNMENT_001_VERIFIED`; PostgreSQL
runtime proof is now covered by the 20260827 Operator evidence.

Current status:

`VERIFIED_BY_OPERATOR_EVIDENCE`

Current formal runtime evidence covers:

- `ERP_SYSTEM_ADMIN`
- `ERP_MDM_OPERATOR`
- `ERP_SALES_OPERATOR`
- `ERP_EMPLOYEE_OPERATOR`
- `ERP_PURCH_OPERATOR`

## 9. ERP_SYSTEM_ADMIN exact 8 evidence

Current status:

`VERIFIED_BY_OPERATOR_EVIDENCE`

Static contract remains:

- `ERP_SYSTEM_ADMIN` = exact 8 Identity system-administration permissions
- No Employee, MDM, Sales, or Purchase permissions

## 10. ERP_EMPLOYEE_OPERATOR evidence

Current status:

`VERIFIED_BY_OPERATOR_EVIDENCE`

Static contract remains:

- `ERP_EMPLOYEE_OPERATOR` = `identity.employee.read` +
  `identity.employee.manage`

## 11. EMP-SYSTEM evidence

Current status:

`VERIFIED_BY_OPERATOR_EVIDENCE`

Operator evidence:

- System employee exists with `EmployeeNo = EMP-SYSTEM`
- User <-> Employee relation remains correct

## 12. Bootstrap / Ensure idempotency

Current status:

`VERIFIED_BY_OPERATOR_EVIDENCE`

Operator evidence:

- Formal bootstrap repeated execution is idempotent
- Existing enterprise ensure path is idempotent

## 13. Tenant / Company isolation

Current status:

`VERIFIED_BY_OPERATOR_EVIDENCE`

Operator evidence:

- Tenant isolation passes in real PG
- Company isolation passes in real PG

## 14. Authorization runtime evidence

Current status:

`VERIFIED_BY_OPERATOR_EVIDENCE`

Operator evidence:

- Persisted RoleClaim + RoleAssignment authorizes only inside the correct
  Tenant + Company scope.
- System-admin-only user still cannot pass Employee manage business permission
  boundary.

## 15. Full current test matrix

Current verified non-PG baseline from `COMMIT_GROUP_9` remains:

| Project | Result |
| --- | --- |
| `GuliERP.Foundation.Tests` | PASS, 68 / 68 |
| `GuliERP.Identity.Tests` | PASS, 103 / 103 |
| `GuliERP.Identity.Bootstrap.Tests` | PASS, 64 / 64 |
| `GuliERP.Mdm.Tests` | PASS, 278 / 278 |
| `GuliERP.Sales.Tests` | PASS, 17 / 17 |
| `GuliERP.Purchase.Tests` | PASS, 18 / 18 |
| `GuliERP.DocumentKernel.Tests` | PASS, 44 / 44 |
| `GuliERP.Api.Tests` | PASS, 32 / 32 |
| `GuliERP.Identity.IntegrationTests` | PASS, 130 / 130 |
| `GuliERP.Foundation.IntegrationTests` | PASS, 31 / 31 |

## 16. Failure classification

Current classification:

`RESOLVED_BY_OPERATOR_EVIDENCE`

More specific operational status:

`VERIFIED`

No evidence currently supports:

- `NEW_PRODUCTION_REGRESSION`
- `DATABASE_STATE_CONFLICT`
- `TEST_FIXTURE_DEFECT`
- `TEST_DATA_CONFLICT`
- `HARNESS_DEFECT`
- `INHERITED_FLAKY`

## 17. Goal Registry changes

`docs/governance/GOAL_REGISTRY.md` is updated by
`GULIERP_EMPLOYEE_MASTER_FINAL_GOVERNANCE_CLOSURE_001` to record the final
`VERIFIED + FROZEN` state.

## 18. Employee V1 Freeze statement

Employee Master V1 freeze is now active based on the 20260827 Operator TRX
evidence and Operator final console PASS.

The freeze statement records:

- Employee Entity V1 fields frozen
- EmployeeNo format frozen
- `EMP-SYSTEM` semantics frozen
- User <-> Employee boundary frozen
- Organization relation frozen
- Tenant / Company isolation frozen
- Employee permissions frozen
- `ERP_EMPLOYEE_OPERATOR` frozen
- `ERP_SYSTEM_ADMIN` exact 8 boundary frozen

No Contact Profile, Employee Vue, CRM, HRM, Sales, Purchase, or Inventory work
is authorized to alter Employee V1 without a separate Contract Evolution Goal.

## 19. Remaining unrelated WIP

Preserved unrelated / prior-goal WIP:

- `docs/governance/GOAL_REGISTRY.md`
- `docs/verification/GULIERP_EMPLOYEE_CODE_CONTRACT_RECONCILIATION_001_REPORT.md`
- `docs/verification/GULIERP_NEXT_EMPLOYEE_OPERATOR_RUNTIME_FINAL_001_REPORT.md`
- `tools/dev/.quarantine/`

Pending prior-goal commit group:

- `COMMIT_GROUP_9 Formal Admin Role-Pack Contract Alignment`

Current-goal report:

- `docs/verification/GULIERP_EMPLOYEE_MASTER_POSTGRES_RUNTIME_CLOSURE_001_REPORT.md`

## 20. Recommended next Goal

`NEXT_GOAL_RECOMMENDATION`

`G3_R3A_INVENTORY_RECEIVING_001` is the recommended next business goal after
the current Employee governance closeout is reviewed and committed or
explicitly deferred. Do not start it in this Goal.

## Commit plan

No commit was created.

Recommended atomic commit group:

`COMMIT_GROUP_10 Employee PostgreSQL Runtime Closure Evidence`

Do not include:

- `tools/dev/.quarantine/`
- unknown local dumps
- secrets
- unrelated WIP

## NO PUSH

No push was performed.
