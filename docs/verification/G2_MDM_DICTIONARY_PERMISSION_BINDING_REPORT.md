# G2-MDM Dictionary Permission Binding Report

## 1. Context

- Goal: continue G2-MDM Runtime Acceptance.
- Runtime state from Operator: environment PASS, login PASS, Cookie PASS, CSRF PASS, API PASS.
- Current failure: creating `DictionaryType` returns HTTP `403 Authorization forbidden`.
- Interpretation: authentication is successful, but the authenticated user lacks the required authorization permission.
- Scope: Dictionary permission binding only.

## 2. Findings

### 2.1 Endpoint Policy

File:

- `apps/api/GuliERP.Api/Mdm/MdmEndpoints.cs`

Dictionary endpoints are protected and should remain protected:

- Dictionary read endpoints use `MdmPolicies.DictionaryRead`.
- Dictionary mutation endpoints use `MdmPolicies.DictionaryManage`.

Mutation endpoints checked:

- `POST /api/v1/mdm/dictionary-types`
- `PUT /api/v1/mdm/dictionary-types/{id}`
- `PATCH /api/v1/mdm/dictionary-types/{id}/status`
- `POST /api/v1/mdm/dictionary-types/{typeId}/items`
- `PUT /api/v1/mdm/dictionary-items/{id}`
- `PATCH /api/v1/mdm/dictionary-items/{id}/status`

Conclusion:

- Endpoint authorization policy is correct.
- No `RequireAuthorization` bypass is needed or allowed.

### 2.2 Permission Codes

File:

- `modules/mdm/GuliERP.Mdm.Application/MdmPermissions.cs`

Dictionary permission codes already exist:

- `mdm.dictionary.read`
- `mdm.dictionary.manage`

Conclusion:

- Permission constants are present.

### 2.3 Policy Mapping

Files:

- `modules/mdm/GuliERP.Mdm.Application/MdmPolicies.cs`
- `modules/mdm/GuliERP.Mdm.Infrastructure/DependencyInjection.cs`

Mappings already exist:

- `MdmPolicies.DictionaryRead = GuliERP.Permission:mdm.dictionary.read`
- `MdmPolicies.DictionaryManage = GuliERP.Permission:mdm.dictionary.manage`
- DI registers both policies with `PermissionRequirement`.

Conclusion:

- ASP.NET Core policy registration is present.

### 2.4 Role Pack / Bootstrap Binding

File:

- `modules/identity/GuliERP.Identity.Application/Authorization/EnterpriseBusinessRolePacks.cs`

Root cause found:

- `ERP_MDM_OPERATOR` had only the earlier 12 MDM permissions:
  - UOM
  - ItemCategory
  - Item
  - BusinessPartner
  - Warehouse
  - Location
- It did not include:
  - `mdm.dictionary.read`
  - `mdm.dictionary.manage`

This explains the runtime behavior:

- Login/Cookie/CSRF/API can all pass.
- Authenticated user can still receive 403 when `POST /api/v1/mdm/dictionary-types` requires `mdm.dictionary.manage`.

## 3. Code Change

Minimal code-side fix:

- Added `mdm.dictionary.read` and `mdm.dictionary.manage` to `EnterpriseBusinessRolePacks.MdmOperator`.
- Updated the `ERP MDM Operator` description to include Dictionary.
- Updated bootstrap/static and authorization regression tests from 12 MDM permissions to 14 MDM permissions.
- Updated operator helper comments that described the old 12-permission MDM pack.

Files intentionally changed:

- `modules/identity/GuliERP.Identity.Application/Authorization/EnterpriseBusinessRolePacks.cs`
- `tests/GuliERP.Identity.Bootstrap.Tests/WebPreview002MdmGrantFacts.cs`
- `tests/GuliERP.Identity.Bootstrap.Tests/BootstrapGrantMdmOperatorFacts.cs`
- `tests/GuliERP.Identity.IntegrationTests/MdmAuthorizationRegressionFacts.cs`
- `tools/GuliERP.Identity.Bootstrap/Program.cs`
- `tools/dev/provision-web-preview-user.ps1`
- `tools/dev/g2-004-bootstrap-operator-user.ps1`

No changes were made to:

- Identity architecture.
- Database schema.
- Migrations.
- `RequireAuthorization`.
- Authorization handler semantics.
- Direct database contents.

## 4. Role Decision

### ERP_SYSTEM_ADMIN

`ERP_SYSTEM_ADMIN` should not receive Dictionary permissions.

Reason:

- This role is the frozen system / Identity administration role.
- Existing boundary keeps it at the original 8 Identity administration permissions.
- MDM business permissions must stay in business operator roles, not system administration.

### ERP_MDM_OPERATOR

`ERP_MDM_OPERATOR` should own Dictionary management permissions.

Reason:

- `/mdm/dictionaries` is an MDM business operator surface.
- Dictionary CRUD follows the same read/manage authorization model as other MDM master data pages.
- The role pack provisioner is idempotent and can add newly expected claims to the existing role without direct manual DB edits.

Expected final MDM permission count:

- 14 total permissions.
- 7 read + 7 manage.

Expected Dictionary claims:

- `mdm.dictionary.read`
- `mdm.dictionary.manage`

## 5. Operator Apply Note

Existing databases will not gain the new role claims merely because the code changed.

Required runtime closure step:

- Re-run the approved bootstrap/provisioning path that uses `EnterpriseBusinessRolePacks.MdmOperator`.
- Then log out and log back in so the Cookie / authorization context is refreshed.

Do not:

- Directly write role claims with ad hoc SQL.
- Add wildcard permissions.
- Grant Dictionary permissions to `ERP_SYSTEM_ADMIN`.
- Remove authorization checks.

## 6. Verification

Static checks:

- `rg` confirmed no active source-side stale "12 MDM permissions" hardcoding remains in the changed source paths.
- `git diff --check` passed for the changed files.

Build checks:

- `dotnet build modules/identity/GuliERP.Identity.Application/GuliERP.Identity.Application.csproj -c Release --no-restore -m:1 ...`
  - PASS.
  - 1 transient file-lock retry warning, 0 errors.
- `dotnet build tests/GuliERP.Identity.IntegrationTests/GuliERP.Identity.IntegrationTests.csproj -c Release --no-restore -m:1 ...`
  - PASS, 0 warnings, 0 errors.
- `dotnet build tests/GuliERP.Identity.Bootstrap.Tests/GuliERP.Identity.Bootstrap.Tests.csproj -c Release --no-restore -m:1 ...`
  - PASS, 0 warnings, 0 errors.

Test checks:

- `dotnet test tests/GuliERP.Identity.Bootstrap.Tests/GuliERP.Identity.Bootstrap.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~WebPreview002MdmGrantFacts|FullyQualifiedName~BootstrapGrantMdmOperatorFacts" --nologo`
  - PASS: 9/9.
- `dotnet test tests/GuliERP.Identity.IntegrationTests/GuliERP.Identity.IntegrationTests.csproj -c Release --no-build --filter "FullyQualifiedName~MdmAuthorizationRegressionFacts" --nologo`
  - PASS: 4/4.

Note:

- An initial parallel `dotnet test` attempt hung without output and was stopped. The serial `--no-build` runs above are the accepted verification evidence for this report.

## 7. Final Status

`G2_MDM_DICTIONARY_PERMISSION_BINDING_CODE_READY`

Runtime expectation after approved role-pack apply and relogin:

- Existing correct administrator with `ERP_MDM_OPERATOR` should have `mdm.dictionary.manage`.
- `/mdm/dictionaries` CRUD should no longer fail with authorization 403 for missing Dictionary permission.

NO PUSH.
