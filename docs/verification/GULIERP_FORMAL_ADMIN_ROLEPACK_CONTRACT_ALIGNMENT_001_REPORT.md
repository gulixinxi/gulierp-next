# GULIERP_FORMAL_ADMIN_ROLEPACK_CONTRACT_ALIGNMENT_001_REPORT

Date: 2026-08-27

## 1. Repo / branch / HEAD

- Repository: `D:/guli/projects/gulierp-next`
- Branch: `master`
- HEAD: `139fe1e940258d71b85d328d88b4e1358c0f7b1e`
- Latest HEAD subject: `docs(verification): add G3 R2B purchase order runtime report`

## 2. G3-R2B authority evidence

Current committed history contains the Purchase role-pack evolution:

- `b2ef495 feat(identity): add purchase operator permission pack`
- `68701b3 test(identity): cover purchase operator permission boundaries`
- `139fe1e docs(verification): add G3 R2B purchase order runtime report`

Production source confirms `ERP_PURCH_OPERATOR` exists, is part of
`EnterpriseBusinessRolePacks.InitialAdminRolePacks`, and is treated as a
formal business operator pack.

Authority status: `AUTHORIZED_BY_CURRENT_HEAD`

## 3. Current exact InitialAdminRolePacks

`EnterpriseBusinessRolePacks.InitialAdminRolePacks` currently contains exactly
4 business operator packs:

1. `ERP_MDM_OPERATOR`
2. `ERP_SALES_OPERATOR`
3. `ERP_EMPLOYEE_OPERATOR`
4. `ERP_PURCH_OPERATOR`

The initial enterprise admin receives these 4 business packs plus the separate
`ERP_SYSTEM_ADMIN` role assignment.

`ROLE_COUNT = 5`

## 4. ERP_SYSTEM_ADMIN exact 8

`ERP_SYSTEM_ADMIN` remains frozen at exactly 8 Identity administration
permissions:

1. `identity.organization.read`
2. `identity.organization.manage`
3. `identity.user.read`
4. `identity.user.manage`
5. `identity.role.read`
6. `identity.role.assign`
7. `identity.company.read`
8. `identity.company.switch`

No MDM, Sales, Employee, or Purchase permission was added to
`ERP_SYSTEM_ADMIN`.

## 5. ERP_EMPLOYEE_OPERATOR exact set

`ERP_EMPLOYEE_OPERATOR` remains exact 2:

1. `identity.employee.read`
2. `identity.employee.manage`

## 6. ERP_PURCH_OPERATOR exact set

`ERP_PURCH_OPERATOR` is exact 2:

1. `purchase.order.read`
2. `purchase.order.manage`

## 7. Effective permission union calculation

The initial admin effective permission union is the de-duplicated sum of:

- `ERP_SYSTEM_ADMIN`: 8
- `ERP_MDM_OPERATOR`: 16
- `ERP_SALES_OPERATOR`: 2
- `ERP_EMPLOYEE_OPERATOR`: 2
- `ERP_PURCH_OPERATOR`: 2

The role-pack boundary tests assert cross-pack disjointness, so the union is:

`EFFECTIVE_PERMISSION_COUNT = 30`

## 8. Stale integration contracts before / after

The current source evidence showed stale integration contract expectations from
the pre-G3-R2B role-pack shape. The user brief expected 5 stale role-pack
contract tests. Current code inspection and verification found these affected
integration contract points:

| Area | Fully-qualified test / helper | Before | After |
| --- | --- | --- | --- |
| Bootstrap role assignment count | `GuliERP.Identity.IntegrationTests.EnterpriseBootstrapAndOrganizationTreeFacts.CreateEnterpriseBootstrap_Creates_Independent_Business_Role_Packs_For_Admin` | Expected 4 assignments and did not assert `ERP_PURCH_OPERATOR` | Expects 5 assignments and asserts Purchase role + permissions |
| Repeated bootstrap counts | `GuliERP.Identity.IntegrationTests.EnterpriseBootstrapAndOrganizationTreeFacts.CreateEnterpriseBootstrap_Repeated_Run_Does_Not_Duplicate_Business_Role_Pack` | Expected 4 roles / 24 claims / 4 assignments | Expects 5 roles / 30 claims / 5 assignments |
| Existing enterprise ensure counts | `GuliERP.Identity.IntegrationTests.EnterpriseBootstrapAndOrganizationTreeFacts.ExistingEnterpriseEnsure_Creates_Missing_Business_Roles_And_Is_Idempotent` | Helper removed only MDM/Sales/Employee; test expected 4 roles / 24 claims / 4 assignments | Helper removes MDM/Sales/Employee/Purchase; test asserts first-run Employee/Purchase creation and expects 5 / 30 / 5 |
| Cross-tenant direct provisioner count | `GuliERP.Identity.IntegrationTests.EnterpriseRolePackCrossTenantFacts.SameTenant_DuplicateProvisionerCall_IsIdempotent_AndDiagnosticStaysClean` | Expected direct provisioner to create 3 business roles | Expects 4 business roles and checks Employee/Purchase idempotency |
| Cross-tenant direct provisioner coverage | `GuliERP.Identity.IntegrationTests.EnterpriseRolePackCrossTenantFacts.TenantB_Creates_SameNormalizedName_AfterTenantA_Succeeds_AndCapturesDiagnostic` | Only asserted MDM/Sales cross-tenant diagnostics | Asserts MDM/Sales/Employee/Purchase diagnostics |

Additional non-failing but stale source-grep coverage was tightened in:

- `GuliERP.Identity.IntegrationTests.EnterpriseRolePackCrossTenantFacts.TenantA_Creates_ErpMdmOperator_Succeeds`

After verification:

- `EnterpriseBootstrapAndOrganizationTreeFacts` + `EnterpriseRolePackCrossTenantFacts`: `20 / 20 PASS`
- `STALE_TEST_CONTRACT = 0`

## 9. Api.Tests stale source-grep before / after

Affected test:

- `GuliERP.Api.Tests.SalesRuntimeRegressionSourceFacts.SalesOrder_Runtime_Path_Uses_Real_Apis_And_No_Mock_Order_Source`

Before:

- Expected raw MDM client names: `listBusinessPartners` and `listItems`.

Current production/UI contract:

- SalesOrder context dropdowns use SalesOrder-scoped facade APIs:
  `listSalesOrderCustomers` and `listSalesOrderItems`.
- This preserves the role boundary: `ERP_SALES_OPERATOR` does not need
  `mdm.*` permissions.

After:

- Test still asserts real SalesOrder API usage and no mock source.
- Test now asserts the SalesOrder context facade function names.

Verification:

- `GuliERP.Api.Tests`: `32 / 32 PASS`

## 10. Release build result

Command:

```powershell
dotnet build GuliERP.slnx -c Release --nologo -m:1
```

Result:

- PASS
- 0 warnings
- 0 errors

Note:

- A running current-repo Release API process
  `apps/api/GuliERP.Api/bin/Release/net10.0/GuliERP.Api.dll` was identified and
  stopped before the Release build to avoid DLL lock.
- A later single-project `GuliERP.Api.Tests` build required network access for
  NuGet restore/audit and then passed with 0 warnings / 0 errors.

## 11. Non-PG test matrix

All requested non-PostgreSQL test projects passed in Release mode using the
current compiled outputs.

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

## 12. Remaining PG-only tests

Full `GuliERP.Identity.IntegrationTests` result:

- Total: 130
- Passed: 121
- Failed / blocked: 9
- Skipped: 0

The 9 remaining failures are classified as PostgreSQL environment/target
blockers, not stale role-pack contract failures:

1. `IdGenerationHiLoFacts.HiLo_PreGenerates_Id_Before_SaveChanges`
2. `IdGenerationHiLoFacts.HiLo_Persists_Long_Id`
3. `IdGenerationHiLoFacts.HiLo_FreshScopes_Produce_Distinct_Ids`
4. `IdGenerationHiLoFacts.HiLo_Does_Not_Collide_With_Existing_Id_Range`
5. `IdentityReferentialIntegrityFacts.FK_Tenant_RejectOnOrphan`
6. `IdentityReferentialIntegrityFacts.FK_Tenant_AcceptOnValid`
7. `IdentityReferentialIntegrityFacts.FK_DeleteBehavior_Restrict_TenantCannotBeDeletedWithCompanies`
8. `IdentityApplicationServiceFacts.ICompanySwitchingService_ResolveDefault_No_Membership_Returns_Null`
9. `G2_005_AuthorizationDataScopeFacts.RealPostgreSql_PersistedRoleClaimAndRoleAssignment_AuthorizesOnlyInsideTenantCompanyScope`

Observed blocker details:

- `Wrong DB target for ID-GEN-001. Expected gulierp_g2_003_test; actual gulierp.`
- `NpgsqlException`: host name could not be resolved.
- Explicit real PostgreSQL connection requirement from
  `ICompanySwitchingService_ResolveDefault_No_Membership_Returns_Null`.

Final remaining status:

`OPERATOR_DB_ENVIRONMENT_BLOCKED`

## 13. Historical 4 -> 5 contract evolution

Historical Employee milestone snapshots may correctly describe the pre-G3-R2B
state as 4 total admin roles:

- `ERP_SYSTEM_ADMIN`
- `ERP_MDM_OPERATOR`
- `ERP_SALES_OPERATOR`
- `ERP_EMPLOYEE_OPERATOR`

Current contract after G3-R2B is 5 total admin role assignments:

- the same 4 roles above
- plus `ERP_PURCH_OPERATOR`

This report records contract evolution. It does not rewrite historical
milestone reports as if Purchase had always existed.

## 14. Files changed

Changed by this Goal:

1. `tests/GuliERP.Identity.IntegrationTests/EnterpriseBootstrapAndOrganizationTreeFacts.cs`
2. `tests/GuliERP.Identity.IntegrationTests/EnterpriseRolePackCrossTenantFacts.cs`
3. `tests/GuliERP.Api.Tests/SalesRuntimeRegressionSourceFacts.cs`
4. `docs/verification/GULIERP_FORMAL_ADMIN_ROLEPACK_CONTRACT_ALIGNMENT_001_REPORT.md`

Pre-existing unrelated WIP still present and not modified by this Goal:

1. `docs/governance/GOAL_REGISTRY.md`
2. `docs/verification/GULIERP_EMPLOYEE_CODE_CONTRACT_RECONCILIATION_001_REPORT.md`
3. `docs/verification/GULIERP_NEXT_EMPLOYEE_OPERATOR_RUNTIME_FINAL_001_REPORT.md`
4. `tools/dev/.quarantine/`

## 15. Final Gate

- Production role-pack architecture changed: NO
- `ERP_PURCH_OPERATOR` deleted or weakened: NO
- `ERP_EMPLOYEE_OPERATOR` deleted or weakened: NO
- `ERP_SYSTEM_ADMIN` exact 8 modified: NO
- SalesOrder / Purchase / Inventory / CRM / HRM / UI new development: NO
- DB schema or migration modified: NO
- Legacy repo touched: NO
- PostgreSQL Operator Harness run: NO
- Commit: NO
- Push: NO

Final status:

`GULIERP_FORMAL_ADMIN_ROLEPACK_CONTRACT_ALIGNMENT_001_VERIFIED`

Suggested atomic commit group:

`COMMIT_GROUP_9 Formal Admin Role-Pack Contract Alignment`
