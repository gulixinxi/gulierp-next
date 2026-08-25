# GULIERP_EMPLOYEE_ROLE_PACK_TEST_ALIGNMENT_001_REPORT

> 报告日期: 2026-08-24
> 任务: `GULIERP_EMPLOYEE_ROLE_PACK_TEST_ALIGNMENT_001` — 同步 G2-EM-001B 已批准的 4-role contract 与现有 integration tests
> 任务阶段: **TEST CONTRACT ALIGNMENT**(不修改 production architecture)
> 操作 Agent: Mavis
> 报告路径: `docs/verification/GULIERP_EMPLOYEE_ROLE_PACK_TEST_ALIGNMENT_001_REPORT.md`
> 依赖: `GULIERP_EMPLOYEE_PERMISSION_BOUNDARY_FIX_001_VERIFIED` (Mavis side)

---

## 1) BEFORE — 3-role stale expectations

`GULIERP-ENTERPRISE-BOOTSTRAP-001` (commit `4b1cf8c`, CLOSED 2026-08-23) 冻结的 contract:

- `InitialAdminRolePacks` = `[MdmOperator, SalesOperator]` (2 个 business role packs)
- 新企业初始 admin 收到 3 个 role assignments:
  - `ERP_SYSTEM_ADMIN` (8 Identity administration permissions)
  - `ERP_MDM_OPERATOR` (12 MDM permissions)
  - `ERP_SALES_OPERATOR` (2 Sales permissions)
- 总有效 permission claims = `8 + 12 + 2 = 22`
- 总 role assignments = `3`
- 总 roles = `3`

旧的 integration test 断言(写于 G2-EM-001B 之前,假设上述 3-role 假设):
- `Assert.Equal(3, await db.Roles.CountAsync())` × 2 places
- `Assert.Equal(22, await db.RoleClaims.CountAsync(...))` × 2 places
- `Assert.Equal(3, await db.UserRoleAssignments.CountAsync())` × 2 places
- `Assert.Equal(2, await db.Roles.CountAsync())` × 1 place (CrossTenantFacts)
- `Assert.Single(await db.Roles.ToListAsync())` × 1 place (helper)
- `Assert.Single(await db.UserRoleAssignments.ToListAsync())` × 1 place (helper)

**Total: 9 numeric assertions across 5 tests + 1 helper + 1 comment** — 全部 stale,全部需要更新。

---

## 2) AFTER — 4-role frozen contract

`GULIERP_EMPLOYEE_PERMISSION_BOUNDARY_FIX_001` (2026-08-24, Mavis-CLOSED) 批准的 contract:

- `InitialAdminRolePacks` = `[MdmOperator, SalesOperator, EmployeeOperator]` (3 个 business role packs)
- 新企业初始 admin 收到 4 个 role assignments:
  - `ERP_SYSTEM_ADMIN` (**exact 8** Identity administration permissions, frozen)
  - `ERP_MDM_OPERATOR` (12 MDM permissions)
  - `ERP_SALES_OPERATOR` (2 Sales permissions)
  - **`ERP_EMPLOYEE_OPERATOR`** (**exact 2** Employee permissions: `identity.employee.read` + `identity.employee.manage`)
- 总有效 permission claims = `8 + 12 + 2 + 2 = 24`
- 总 role assignments = `4`
- 总 roles = `4`

**架构原则**(per user brief):
> System Administration permissions vs Business Operator permissions must remain separate.
> ERP_SYSTEM_ADMIN 仅 8 项 Identity 治理权限(不变)。
> 新增的 `ERP_EMPLOYEE_OPERATOR` 是独立的 Business Operator role pack。

**NOT a production architecture rollback** — test contract alignment to the new (already-approved) production design。

---

## 3) 11 个修改 (逐个列出 + 证明)

### 3.1 `tests/GuliERP.Identity.IntegrationTests/EnterpriseBootstrapAndOrganizationTreeFacts.cs`

#### Change 1: line 339

**Before**:
```csharp
Assert.Equal(3, await db.Roles.CountAsync());
```

**After**:
```csharp
// GULIERP_EMPLOYEE_ROLE_PACK_TEST_ALIGNMENT_001 (2026-08-24):
// The initial admin now receives 4 roles (1 system role +
// 3 business role packs: MDM, Sales, EmployeeOperator) and
// 24 effective permissions (8 + 12 + 2 + 2). The previous
// assertion (3 / 22 / 3) was the frozen contract for the
// pre-G2-EM-001B design.
Assert.Equal(4, await db.Roles.CountAsync());
```

**Justification**: G2-EM-001B 设计变更:initial admin 收 4 个 roles (System + 3 business)。`3` 是 pre-G2-EM-001B 的冻结值,`4` 是新 contract。

#### Change 2: line 340

**Before**: `Assert.Equal(22, await db.RoleClaims.CountAsync(...))`
**After**: `Assert.Equal(24, ...)` 
**Justification**: 8 (System) + 12 (MDM) + 2 (Sales) + 2 (Employee) = **24**。新增的 2 个是 `identity.employee.read` / `identity.employee.manage`,正是 `ERP_EMPLOYEE_OPERATOR` 的 exact permission set。

#### Change 3: line 341

**Before**: `Assert.Equal(3, await db.UserRoleAssignments.CountAsync())`
**After**: `Assert.Equal(4, ...)`
**Justification**: Initial admin 收 4 个 role assignment(1 system + 3 business)。

#### Change 4-6: line 373-375 (in `ExistingEnterpriseEnsure_Creates_Missing_Business_Roles_And_Is_Idempotent`)

**Before**: `3 / 22 / 3` (same as Changes 1-3)
**After**: `4 / 24 / 4` (same as Changes 1-3)
**Justification**: Same as Changes 1-3。该 test 也验证 idempotent re-provision 不创建重复 — 与 4-role contract 一致。

#### Change 7-8: helper `BootstrapAndRemoveBusinessRolesAsync` (line 619-633)

**Before**:
```csharp
var businessRoleIds = await db.Roles
    .Where(r => r.Code == EnterpriseBusinessRolePacks.MdmOperatorRoleCode
        || r.Code == EnterpriseBusinessRolePacks.SalesOperatorRoleCode)
    .Select(r => r.Id)
    .ToArrayAsync();
// ... remove all 3 ...
Assert.Single(await db.Roles.ToListAsync());
Assert.Single(await db.UserRoleAssignments.ToListAsync());
```

**After**:
```csharp
// GULIERP_EMPLOYEE_ROLE_PACK_TEST_ALIGNMENT_001 (2026-08-24):
// The initial admin now gets 3 business role packs
// (MdmOperator + SalesOperator + EmployeeOperator). The
// helper removes all 3 so the post-removal state is just
// ERP_SYSTEM_ADMIN (the system role is NOT a business role
// pack and is preserved).
var businessRoleIds = await db.Roles
    .Where(r => r.Code == EnterpriseBusinessRolePacks.MdmOperatorRoleCode
        || r.Code == EnterpriseBusinessRolePacks.SalesOperatorRoleCode
        || r.Code == EnterpriseBusinessRolePacks.EmployeeOperatorRoleCode)
    .Select(r => r.Id)
    .ToArrayAsync();
// ... remove all 3 ...
Assert.Equal(1, await db.Roles.CountAsync());
Assert.Equal(1, await db.UserRoleAssignments.CountAsync());
```

**Justification**:
- `Where` filter 加上 `EmployeeOperatorRoleCode`:helper 必须移除**全部 3 个** business role pack(原版只移除 2 个,留下 EmployeeOperator 未被删除,违反 test 意图)。
- `Assert.Single(...)` → `Assert.Equal(1, ...)`:helper 移除 3 个 business role pack 后,**只剩 1 个 role(ERP_SYSTEM_ADMIN) + 1 个 assignment**。System role 是独立的 system role,**不是** business role pack,**不应**被 helper 移除。

**重要修正**:第一次写 `Assert.Equal(2, ...)` 是错误分析 — 已修正为 `1`。System role 不算 business role pack。

### 3.2 `tests/GuliERP.Identity.IntegrationTests/EnterpriseRolePackCrossTenantFacts.cs`

#### Change 9: line 152

**Before**:
```csharp
// Database invariants for this isolated test: the
// EnterpriseBusinessRolePackProvisioner (used directly here)
// provisions only the 2 business roles (MDM + Sales). The
// ERP_SYSTEM_ADMIN role is created by the full
// IEnterpriseBootstrapService.CreateEnterpriseBootstrapAsync
// path, which is exercised separately in
// EnterpriseBootstrapAndOrganizationTreeFacts.
Assert.Equal(2, await db.Roles.CountAsync());
```

**After**:
```csharp
// Database invariants for this isolated test: the
// EnterpriseBusinessRolePackProvisioner (used directly here)
// provisions the 3 business roles (MDM + Sales +
// EmployeeOperator) per GULIERP_EMPLOYEE_ROLE_PACK_TEST_ALIGNMENT_001
// (2026-08-24). The ERP_SYSTEM_ADMIN role is created by
// the full IEnterpriseBootstrapService.CreateEnterpriseBootstrapAsync
// path, which is exercised separately in
// EnterpriseBootstrapAndOrganizationTreeFacts.
Assert.Equal(3, await db.Roles.CountAsync());
```

**Justification**:
- `EnterpriseBusinessRolePackProvisioner` (直接调用,不经 full bootstrap) 现在 provision 3 个 business role packs(Mdm + Sales + Employee)。
- `Assert.Equal(2, ...)` → `Assert.Equal(3, ...)`:直接反映新 provisioner 行为。
- 注释同步更新("provisions only the 2 business roles" → "provisions the 3 business roles")。

### 3.3 ERP_SYSTEM_ADMIN 仍然 exact 8 (锁定未变)

`GuliErpPermissions.EnterpriseSystemAdminPermissions` 数组**未修改**:
- 8 项 Identity administration permissions(locked since commit `4b1cf8c`):
  1. `identity.organization.read`
  2. `identity.organization.manage`
  3. `identity.user.read`
  4. `identity.user.manage`
  5. `identity.role.read`
  6. `identity.role.assign`
  7. `identity.company.read`
  8. `identity.company.switch`
- **NOT** 24 permissions — System role 的 8 项保持 frozen。
- 24 是 4 个 role 的**总和**(8 + 12 + 2 + 2),不是单个 role 的 permission count。

---

## 4) Build + Test Result

### 4.1 Build

```
$ dotnet build GuliERP.slnx -c Release --nologo
```

**Result: 0 errors, 0 warnings, 25/25 projects PASS** ✅

### 4.2 Affected Integration Tests (5 tests, all pass)

```
$ dotnet test tests/GuliERP.Identity.IntegrationTests/GuliERP.Identity.IntegrationTests.csproj -c Release --no-build --nologo \
    --filter 'FullyQualifiedName~CreateEnterpriseBootstrap_Repeated_Run_Does_Not_Duplicate_Business_Role_Pack|...|FullyQualifiedName~SameTenant_DuplicateProvisionerCall_IsIdempotent_AndDiagnosticStaysClean'

已通过! - 失败:     0，通过:    5，已跳过:     0，总计:    5，持续时间 1 s
```

**5/5 PASS** ✅

| Test | Result |
|---|---|
| `CreateEnterpriseBootstrap_Repeated_Run_Does_Not_Duplicate_Business_Role_Pack` | ✅ PASS |
| `ExistingEnterpriseEnsure_Creates_Missing_Business_Roles_And_Is_Idempotent` | ✅ PASS |
| `ExistingEnterpriseEnsure_Stops_When_Role_Has_Unexpected_Wildcard` | ✅ PASS |
| `ExistingEnterpriseEnsure_Backfills_Missing_Expected_Claims` | ✅ PASS |
| `SameTenant_DuplicateProvisionerCall_IsIdempotent_AndDiagnosticStaysClean` | ✅ PASS |

### 4.3 Regression check (no production code touched)

| Test Project | Result | Note |
|---|---|---|
| `GuliERP.Identity.Tests` | **84 / 84 PASS** | (unchanged) |
| `GuliERP.Identity.Bootstrap.Tests` | **64 / 64 PASS** | (unchanged) |
| `GuliERP.Api.Tests` | **32 / 32 PASS** | (unchanged) |
| `GuliERP.Foundation.Tests` | **68 / 68 PASS** | (unchanged) |
| `GuliERP.Mdm.Tests` | **221 / 223 PASS** | (unchanged; 2 inherited flaky) |
| `GuliERP.Sales.Tests` | **9 / 9 PASS** | (unchanged) |
| `GuliERP.DocumentKernel.Tests` | **44 / 44 PASS** | (unchanged) |

---

## 5) Files Changed (uncommitted, await user authorization)

| File | Change | Lines |
|---|---|---|
| `tests/GuliERP.Identity.IntegrationTests/EnterpriseBootstrapAndOrganizationTreeFacts.cs` | 3→4, 22→24 (×2), helper filter +Single→Equal(1, ...) | 339, 340, 341, 373, 374, 375, 619-622, 632-633 |
| `tests/GuliERP.Identity.IntegrationTests/EnterpriseRolePackCrossTenantFacts.cs` | 2→3 + comment update | 145-152 |
| `docs/verification/GULIERP_EMPLOYEE_ROLE_PACK_TEST_ALIGNMENT_001_REPORT.md` | new (this report) | - |

**Total: 2 test files modified, 1 report created, 0 production code changed**.

---

## 6) COMMIT_GROUP_6 推荐

**Atomic commit message suggestion**:
```
test(identity): GULIERP_EMPLOYEE_ROLE_PACK_TEST_ALIGNMENT_001 — align to 4-role contract

Updates integration tests to assert the post-G2-EM-001B 4-role
initial admin contract:
- 4 roles (System + Mdm + Sales + EmployeeOperator)
- 24 effective permissions (8 + 12 + 2 + 2)
- 4 user role assignments

NO production code changed. ERP_SYSTEM_ADMIN exact 8 frozen.
EnterpriseBusinessRolePacks.InitialAdminRolePacks stays at 3
business role packs (Mdm + Sales + Employee).

Helper BootstrapAndRemoveBusinessRolesAsync extended to remove
all 3 business role packs (was 2). Post-removal state is 1 role
(ERP_SYSTEM_ADMIN only, which is NOT a business role pack).

Cross-tenant test aligned to provisioner direct call provisions
3 business roles (was 2).

No Gate manipulation: tests now reflect the approved 4-role
production design (G2-EM-001B), not the pre-change 3-role design.
```

**Files** (3):
- `tests/GuliERP.Identity.IntegrationTests/EnterpriseBootstrapAndOrganizationTreeFacts.cs` (modified)
- `tests/GuliERP.Identity.IntegrationTests/EnterpriseRolePackCrossTenantFacts.cs` (modified)
- `docs/verification/GULIERP_EMPLOYEE_ROLE_PACK_TEST_ALIGNMENT_001_REPORT.md` (new)

**NO COMMIT performed** (per user `COMMIT RULE`).

---

## 7) Hard Stop Check

| Brief condition | This Goal? |
|---|---|
| Roll back `ERP_EMPLOYEE_OPERATOR` | NO |
| Roll back 4-role contract | NO |
| Modify `ERP_SYSTEM_ADMIN` exact 8 | NO (still 8) |
| Modify production `PermissionAuthorizationHandler` | NO |
| Employee new features | NO |
| Contact Profile | NO (still DESIGN_ONLY) |
| Vue | NO |
| SalesOrder / Purchase / Inventory | NO |
| DB Schema / Migration | NO |
| Legacy repo | NO (untouched) |

**0 hard-stops tripped.**

---

## 8) Final conclusion

**GULIERP_EMPLOYEE_ROLE_PACK_TEST_ALIGNMENT_001_VERIFIED** (Mavis side)

- ✅ 5/5 affected integration tests PASS
- ✅ 84/84 Identity.Tests unit tests PASS (no regression)
- ✅ 64/64 Identity.Bootstrap.Tests PASS (no regression)
- ✅ 32/32 Api.Tests PASS (no regression)
- ✅ All Mavis-side unit tests PASS (522/524, 2 inherited flaky unchanged)
- ✅ NO production code modified
- ✅ ERP_SYSTEM_ADMIN 仍然 exact 8
- ✅ 4-role contract 保留(G2-EM-001B 设计不变)
- ⏸️ PostgreSQL-requiring tests (Category C 9 tests) still OPERATOR_REQUIRED

**NEXT**: 用户可授权 COMMIT_GROUP_6,然后进入 SUB-GOAL 2 (`GULIERP_PERMISSION_TEST_FIXTURE_CLAIMTYPE_FIX_001`) 修复 11 个 pre-existing test fixture bug。

---

## 9) Honest Disclosure

1. **第一次断言错误**:helper 的 `Assert.Equal(2, ...)` 是第一次写错(误以为 EmployeeOperator 不被 helper 移除)。第二次已修正为 `Assert.Equal(1, ...)`(因为 helper 移除所有 3 个 business role packs,只剩 1 个 system role)。**已修正并验证 PASS**。
2. **没有修改 production code**:本任务纯 test 同步,不动 `GuliErpPermissions.EnterpriseSystemAdminPermissions` / `EnterpriseBusinessRolePacks` / `EnterpriseBusinessRolePackProvisioner`。
3. **9 个 PG-requiring integration tests 仍未实机运行**:Category C 失败未在本任务范围内(per `GULIERP_NEXT_OPERATOR_RUNTIME_VERIFICATION_001_REPORT.md` §14 后续 Operator 动作)。
4. **11 个 Category B (pre-existing test fixture bug) 仍未修复**:这是 SUB-GOAL 2 的范围,不在本任务。
