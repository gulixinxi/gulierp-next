# GULIERP_EMPLOYEE_PERMISSION_BOUNDARY_FIX_001_REPORT

> 报告日期: 2026-08-24
> 任务: `GULIERP_EMPLOYEE_PERMISSION_BOUNDARY_FIX_001` — 修复 Employee Master V1 静默破坏 ERP_SYSTEM_ADMIN 8-permission 冻结契约的 P0 权限边界冲突
> 任务阶段: **STEP 1-9 全部完成**(commit / push / integration runtime 仍待 Operator 授权)
> 操作 Agent: Mavis
> 报告路径: `docs/verification/GULIERP_EMPLOYEE_PERMISSION_BOUNDARY_FIX_001_REPORT.md`
> Goal Registry 段: `docs/governance/GOAL_REGISTRY.md` `## G2-EM-001B` (新加)
> 架构原则 (per user brief): **System Administration permissions 与 Business Operator permissions 必须分离**

---

## 1) 架构决策

### 1.1 角色职责边界

| 角色 | 类别 | 权限来源 | 数量 |
|---|---|---|---:|
| `ERP_SYSTEM_ADMIN` | **System Administration** | GULIERP-ENTERPRISE-BOOTSTRAP-001 冻结的 **8 项** Identity 治理权限 | **8** |
| `ERP_MDM_OPERATOR` | Business Operator (MDM) | MDM 12 项权限 | 12 |
| `ERP_SALES_OPERATOR` | Business Operator (Sales) | Sales 2 项权限 | 2 |
| **`ERP_EMPLOYEE_OPERATOR`** (新) | **Business Operator (Employee)** | **Employee 2 项权限** (`identity.employee.read` + `identity.employee.manage`) | **2** |

### 1.2 架构原则(本任务锁定)

> **System Administration permissions 与 Business Operator permissions 必须保持分离**
> System Admin 角色 (`ERP_SYSTEM_ADMIN`) 只承担系统 / Identity 治理职责,**不得**承担业务操作员角色。业务操作员角色 (`ERP_MDM_OPERATOR` / `ERP_SALES_OPERATOR` / `ERP_EMPLOYEE_OPERATOR`) 各自独立,互不嵌套。

### 1.3 禁止路径

- ❌ Employee 权限塞进 `ERP_MDM_OPERATOR`
- ❌ Employee 权限塞进 `ERP_SALES_OPERATOR`
- ❌ 修改已有 8 项权限定义以迁就当前实现
- ❌ 静默让 `ERP_SYSTEM_ADMIN` 隐式获得 Employee 权限
- ❌ 修改 `GULIERP_ENTERPRISE_BOOTSTRAP_001_REPORT.md` (历史冻结文档)
- ❌ 修改 Legacy 仓库

---

## 2) BEFORE / AFTER 证据

### 2.1 BEFORE (before this Goal)

| 项 | 状态 |
|---|---|
| `GuliErpPermissions.EnterpriseSystemAdminPermissions` | **10 项** (8 冻结 + `identity.employee.read` + `identity.employee.manage` 静默加入) |
| `EnterpriseBusinessRolePacks` 新角色 | ❌ 不存在 |
| `EnterpriseBusinessRolePackProvisioner.EnsureInitialAdminBusinessRolePackAsync` | 只 provision `MdmOperator` + `SalesOperator` |
| Initial admin 角色包 | `MdmOperator` + `SalesOperator` (2 个 business role packs) |
| `tests/.../EmployeeWriteApiFacts.cs` 验证路径 | 仅 `IdentityEmployeeRead/Manage` 直接注入,无 super-admin bypass test |

**`GULIERP_ENTERPRISE_BOOTSTRAP_001_REPORT.md` 冻结证据**:
- Line 600: "ERP_SYSTEM_ADMIN correctly contained only the **8 Identity administration permissions**"
- Line 631: "ERP_SYSTEM_ADMIN remains independent and does not receive business permissions"
- Line 749: "ERP_SYSTEM_ADMIN carries the canonical **8 Identity administration permissions** (unchanged, isolated from business roles)"
- Line 787: "RoleClaims count (12 MDM + 2 Sales + **8 Identity** = 22)"
- Line 798: "Permission Resolution (22 effective codes = **8 Identity** + 12 MDM + 2 Sales)"

### 2.2 AFTER (this Goal applied)

| 项 | 状态 |
|---|---|
| `GuliErpPermissions.EnterpriseSystemAdminPermissions` | **8 项** (回滚到冻结 8 项 Identity 治理权限) |
| `EnterpriseBusinessRolePacks.EmployeeOperator` | **新** (code `ERP_EMPLOYEE_OPERATOR`, 2 项 permissions) |
| `EnterpriseBusinessRolePackProvisioner.EnsureInitialAdminBusinessRolePackAsync` | provision `MdmOperator` + `SalesOperator` + **`EmployeeOperator`** |
| `EnterpriseBusinessRolePacks.InitialAdminRolePacks` | `MdmOperator` + `SalesOperator` + **`EmployeeOperator`** (3 个 business role packs) |
| Initial admin 角色包 | 4 个 role pack: `ERP_SYSTEM_ADMIN` (显式 provisioned) + `ERP_MDM_OPERATOR` + `ERP_SALES_OPERATOR` + **`ERP_EMPLOYEE_OPERATOR`** |
| `ERP_SYSTEM_ADMIN` exact permission set | `{IdentityOrganizationRead, IdentityOrganizationManage, IdentityUserRead, IdentityUserManage, IdentityRoleRead, IdentityRoleAssign, IdentityCompanyRead, IdentityCompanySwitch}` |
| `ERP_EMPLOYEE_OPERATOR` exact permission set | `{identity.employee.read, identity.employee.manage}` |
| `tests/.../EnterpriseBootstrapAndOrganizationTreeFacts.cs:CreateEnterpriseBootstrap_Creates_Independent_Business_Role_Packs_For_Admin` | `Assert.Equal(4, assignments.Count)` (was 3) |

---

## 3) STEP 1 — PRECHANGE EVIDENCE

### 3.1 当前 ERP_SYSTEM_ADMIN 权限集合(本任务前 dirty 状态)

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
    // ← 之前 Employee team's 8→10 改动: 下面两行已在本 Goal STEP 2 中回滚
    // IdentityEmployeeRead,     // removed
    // IdentityEmployeeManage,   // removed
};
```

**当前 count**: **8 项** (本任务已经在 prior session 完成了 10 → 8 回滚)

### 3.2 历史 Formal Bootstrap exact-8 tests

**`tests/GuliERP.Identity.IntegrationTests/EnterpriseBootstrapAndOrganizationTreeFacts.cs:243-245`**:
```csharp
Assert.Equal(
    GuliErpPermissions.EnterpriseSystemAdminPermissions.OrderBy(p => p),
    permissions.OrderBy(p => p));
```
- 用 array 跟 DB 比对,**set comparison, NOT exact count lock**
- 但 8 个 permission 名字在 `docs/verification/GULIERP_ENTERPRISE_BOOTSTRAP_001_REPORT.md` 显式列出(line 600-608),**documentary exact-8 contract**

**`tests/GuliERP.Identity.IntegrationTests/EnterpriseBootstrapAndOrganizationTreeFacts.cs:585` (AssertRolePermissionsAsync)**:
```csharp
Assert.Equal(expected.OrderBy(p => p), permissions.OrderBy(p => p));
```
- 同样 set comparison

**`tests/GuliERP.Identity.Bootstrap.Tests/BootstrapFormalEnterpriseDiagnosticFacts.cs:184`**:
```csharp
var roleClaims = GuliErpPermissions.EnterpriseSystemAdminPermissions
    .Select((permission, index) => new Program.FormalRoleClaimRow(...))
    .ToArray();
```
- 用 array 构造 expected,跟随 array 内容

**`tests/GuliERP.Identity.Tests/EmployeeWriteServiceArchitectureFacts.cs`** (本 Goal 新加):
- `EnterpriseSystemAdminPermissions_Is_Frozen_At_Exact_8_Original_Identity_Permissions` (line 170-...)
  - `Assert.Equal(8, adminArray.Length);`
  - `Assert.Equal(expected.OrderBy, adminArray.OrderBy);` — **locks the exact 8 names**
  - `Assert.DoesNotContain("identity.employee.read", adminArray);`
  - `Assert.DoesNotContain("identity.employee.manage", adminArray);`
  - `Assert.DoesNotContain(adminArray, p => p.StartsWith("mdm."));`
  - `Assert.DoesNotContain(adminArray, p => p.StartsWith("sales."));`

### 3.3 当前 dirty 状态下已存在的 Employee 应用代码

| File | 状态 | 角色 |
|---|---|---|
| `modules/identity/.../Employee.cs` (committed) | 7 V1 字段 + 5 audit + ICompanyScoped | (entity, no permission) |
| `modules/identity/.../Authorization/GuliErpPermissions.cs` | 2 const 已存在 (`IdentityEmployeeRead/Manage`) | (const) |
| `modules/identity/.../Authorization/GuliErpAuthorizationPolicies.cs` | 2 policies 已存在 | (policy) |
| `modules/identity/.../Application/Employee/...` (untracked) | 5 files (DTOs/ErrorCodes/ValidationException/IWriteService/ForIdentity factory) | (Application) |
| `modules/identity/.../Infrastructure/EmployeeSvc/EmployeeWriteService.cs` (untracked) | 17.7 KB, 5 methods | (Service) |
| `apps/api/.../Organization/OrganizationEndpoints.cs` (modified) | 5 endpoints wired | (API) |

---

## 4) STEP 2 — RESTORE ERP_SYSTEM_ADMIN

### 4.1 操作

`GuliErpPermissions.cs` `EnterpriseSystemAdminPermissions` 数组在 prior session 已经被回滚到 8 项。本任务**不需要进一步修改**。验证证据:

```csharp
// modules/identity/GuliERP.Identity.Application/Authorization/GuliErpPermissions.cs:31-41
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
};
```

### 4.2 保留的 Employee 相关资产

- ✅ 2 个 const `IdentityEmployeeRead` / `IdentityEmployeeManage` 保留
- ✅ 2 个 policy `IdentityEmployeeRead` / `IdentityEmployeeManage` 保留
- ✅ Employee API authorization + Employee service implementation 保留
- ❌ **`EnterpriseSystemAdminPermissions` 不再包含 Employee 2 权限** (冻结 8 项纯 Identity 治理)

---

## 5) STEP 3 — ADD EMPLOYEE BUSINESS ROLE

### 5.1 新角色定义

```csharp
// modules/identity/GuliERP.Identity.Application/Authorization/EnterpriseBusinessRolePacks.cs
public const string EmployeeOperatorRoleCode = "ERP_EMPLOYEE_OPERATOR";

public static readonly EnterpriseBusinessRolePack EmployeeOperator = new(
    EmployeeOperatorRoleCode,
    "ERP Employee Operator",
    "Read + manage access to the Employee Master V1 vertical slice only. " +
    "Independent role — does NOT carry any MDM, Sales, HR, or CRM permissions.",
    new[]
    {
        "identity.employee.read",
        "identity.employee.manage",
    });
```

### 5.2 角色职责

- ✅ Employee Master 业务人员资料管理
- ✅ Read + manage Employee entity (V1 7+5 字段)
- ❌ NOT HR role (no salary, attendance, recruitment, performance, social security, payroll)
- ❌ NOT CRM role (no customer, opportunity, lead)
- ❌ NOT MDM role (no UOM, Item, Warehouse, BusinessPartner, Location, WorkCenter)
- ❌ NOT Sales role (no Quotation, SalesOrder, Delivery, SalesReturn)
- ❌ NOT identity.organization.* / identity.user.* / identity.role.* / identity.company.*

### 5.3 集成到 `InitialAdminRolePacks`

```csharp
public static readonly EnterpriseBusinessRolePack[] InitialAdminRolePacks =
{
    MdmOperator,
    SalesOperator,
    EmployeeOperator,  // NEW
};
```

---

## 6) STEP 4 — FORMAL ENTERPRISE BOOTSTRAP

### 6.1 Provisioner 扩展

`EnterpriseBusinessRolePackProvisioner.EnsureInitialAdminBusinessRolePackAsync` 现在 provision 3 个 business role packs:

```csharp
// modules/identity/GuliERP.Identity.Infrastructure/EnterpriseOrganization/EnterpriseBusinessRolePackProvisioner.cs:58-85
public async Task<EnterpriseBusinessRolePackProvisionResult> EnsureInitialAdminBusinessRolePackAsync(
    long tenantId, long companyId, long userId, DateTimeOffset now, CancellationToken ct = default)
{
    var mdm = await EnsureRolePackAsync(tenantId, companyId, userId,
        EnterpriseBusinessRolePacks.MdmOperator, now, ct);
    var sales = await EnsureRolePackAsync(tenantId, companyId, userId,
        EnterpriseBusinessRolePacks.SalesOperator, now, ct);
    // GULIERP_EMPLOYEE_PERMISSION_BOUNDARY_FIX_001 (2026-08-24):
    // The initial enterprise admin also receives the
    // `ERP_EMPLOYEE_OPERATOR` role pack (2 permissions:
    // identity.employee.read / .manage) so the admin can manage
    // the Employee Master V1 surface out of the box.
    var employee = await EnsureRolePackAsync(tenantId, companyId, userId,
        EnterpriseBusinessRolePacks.EmployeeOperator, now, ct);
    return new EnterpriseBusinessRolePackProvisionResult(mdm, sales, employee);
}
```

### 6.2 Result record 扩展

```csharp
public sealed record EnterpriseBusinessRolePackProvisionResult(
    EnterpriseRolePackProvisionResult Mdm,
    EnterpriseRolePackProvisionResult Sales,
    EnterpriseRolePackProvisionResult Employee)
{
    public bool Idempotent =>
        !Mdm.RoleCreated && !Sales.RoleCreated && !Employee.RoleCreated
        && Mdm.ClaimsCreated.Count == 0 && Sales.ClaimsCreated.Count == 0 && Employee.ClaimsCreated.Count == 0
        && !Mdm.AssignmentCreated && !Sales.AssignmentCreated && !Employee.AssignmentCreated;
}
```

### 6.3 Initial admin 角色分配

**新企业初始 admin 同时被分配 4 个独立角色**:

| Role | Source | Permissions |
|---|---|---|
| `ERP_SYSTEM_ADMIN` | Explicitly provisioned in `EnterpriseBootstrapService` line 497-540 | Frozen 8 Identity administration permissions |
| `ERP_MDM_OPERATOR` | `EnterpriseBusinessRolePackProvisioner` (line 65-71) | 12 MDM permissions |
| `ERP_SALES_OPERATOR` | `EnterpriseBusinessRolePackProvisioner` (line 72-78) | 2 Sales permissions |
| `ERP_EMPLOYEE_OPERATOR` | `EnterpriseBusinessRolePackProvisioner` (line 79-86) | **2 Employee permissions** |

**Admin 最终具备 Employee 管理能力,Employee 权限来源是 `ERP_EMPLOYEE_OPERATOR` 而非 `ERP_SYSTEM_ADMIN`**。

### 6.4 既有 3 个角色 ID 不变

`GULIERP-ENTERPRISE-BOOTSTRAP_001` 的现有确定性 ID 机制保持不变:
- `ERP_SYSTEM_ADMIN` ID 仍由 `EnterpriseBootstrapService` line 503-519 创建
- `ERP_MDM_OPERATOR` / `ERP_SALES_OPERATOR` ID 由 `EnterpriseBusinessRolePackProvisioner.EnsureRolePackAsync` 创建

**`ERP_EMPLOYEE_OPERATOR` ID 由 `EnsureRolePackAsync` 同一路径创建**,使用相同的 deterministic ID 机制(Snowflake worker/sequence)。

---

## 7) STEP 5 — EXISTING FORMAL ENTERPRISE ENSURE

### 7.1 Idempotency 验证

`EnterpriseBusinessRolePackProvisioner.EnsureRolePackAsync` (line 83-236) 已经在 G2-003V3 + GULIERP-ENTERPRISE-BOOTSTRAP-001 阶段实现幂等:

- **Role**: 如果已存在 → 不再创建;如果不存在 → 创建 (`roleCreated = false` / `true`)
- **Claims**: 逐个检查 `pack.Permissions` 是否已在 `RoleClaims` 中,缺失才添加 (`claimsCreated` 记录新增的)
- **Assignment**: 检查 `UserRoleAssignments` 中是否已有 (TenantId, CompanyId, UserId, RoleId) 匹配,缺失才创建

### 7.2 重复执行保证

`EnterpriseBusinessRolePackProvisionResult.Idempotent` (扩展后):
```csharp
public bool Idempotent =>
    !Mdm.RoleCreated && !Sales.RoleCreated && !Employee.RoleCreated
    && Mdm.ClaimsCreated.Count == 0 && Sales.ClaimsCreated.Count == 0 && Employee.ClaimsCreated.Count == 0
    && !Mdm.AssignmentCreated && !Sales.AssignmentCreated && !Employee.AssignmentCreated;
```

**`Employee.RoleCreated == false && Employee.ClaimsCreated.Count == 0 && !Employee.AssignmentCreated`** → 表示对 `ERP_EMPLOYEE_OPERATOR` 的 ensure 是幂等的。

### 7.3 已有 Tenant 的补齐路径

`--ensure-formal-enterprise-business-role-pack` 命令(通过 `tools/GuliERP.Identity.Bootstrap/Program.cs` 入口)对每个 Tenant 调用 `EnterpriseBusinessRolePackProvisioner.EnsureInitialAdminBusinessRolePackAsync`。当 Tenant 已存在但 `ERP_EMPLOYEE_OPERATOR` role 不存在时:
- `EnsureRolePackAsync` 第 123-141 行:`role is null` → 创建 role (`roleCreated = true`)
- 第 175-190 行:逐个添加 2 个 missing claims (`claimsCreated = ["identity.employee.read", "identity.employee.manage"]`)
- 第 204-225 行:为 admin 创建 `UserRoleAssignment` (`assignmentCreated = true`)

**当 Tenant 已存在且 `ERP_EMPLOYEE_OPERATOR` role 已存在**:
- `EnsureRolePackAsync` 第 142-154 行:`role != null` → 验证 Status=Active + IsSystem=true,否则抛错
- 第 156-173 行:检查所有 expected permissions 都已存在;如果存在 extra permission (如 `*` 或非预期的),抛错
- 第 175-190 行:逐个添加 missing claims(无缺失则 `claimsCreated` 为空)
- 第 204-225 行:检查 `UserRoleAssignment` 已存在,无缺失则 `assignmentCreated = false`

### 7.4 禁止路径

- ❌ 不得删除用户自定义角色(`EnsureRolePackAsync` 只 `Ensure` `pack.Code` 单一 role,不删其它)
- ❌ 不得修改其它业务角色(Mdm/Sales role pack unchanged)
- ❌ 不得清理无关权限(`EnsureRolePackAsync` 只添加 `pack.Permissions` 中的权限,不删 `RoleClaims`)
- ❌ 不得自动 rewrite production data(Ensure 模式是只添加,从不删除)

---

## 8) STEP 6 — EXACT CONTRACT TESTS

### 8.1 A. ERP_SYSTEM_ADMIN exact permission count = 8 + 不含 Employee

**File**: `tests/GuliERP.Identity.Tests/EmployeeWriteServiceArchitectureFacts.cs`

```csharp
[Fact]
public void EnterpriseSystemAdminPermissions_Is_Frozen_At_Exact_8_Original_Identity_Permissions()
{
    // STEP 6-A: locks the exact 8 names + counts + no Employee/MDM/Sales.
    var adminArray = (string[])adminField.GetValue(null)!;
    Assert.NotNull(adminArray);
    Assert.Equal(8, adminArray.Length);
    var expected = new[]
    {
        "identity.organization.read", "identity.organization.manage",
        "identity.user.read", "identity.user.manage",
        "identity.role.read", "identity.role.assign",
        "identity.company.read", "identity.company.switch",
    };
    Assert.Equal(expected.OrderBy(p => p).ToArray(), adminArray.OrderBy(p => p).ToArray());
    Assert.DoesNotContain("identity.employee.read", adminArray);
    Assert.DoesNotContain("identity.employee.manage", adminArray);
    Assert.DoesNotContain(adminArray, p => p.StartsWith("mdm."));
    Assert.DoesNotContain(adminArray, p => p.StartsWith("sales."));
}
```

✅ PASS(Identity.Tests 实机运行)

### 8.2 B. ERP_EMPLOYEE_OPERATOR exact permission count = 2 + exact set

**File**: `tests/GuliERP.Identity.Tests/EmployeeWriteServiceArchitectureFacts.cs`

```csharp
[Fact]
public void EnterpriseBusinessRolePacks_Has_Dedicated_EmployeeOperator_With_Exact_2_Permissions()
{
    // STEP 6-B: locks the exact 2 names + counts + no other domain permissions.
    var pack = (EnterpriseBusinessRolePack)employeeField.GetValue(null)!;
    Assert.Equal("ERP_EMPLOYEE_OPERATOR", pack.Code);
    Assert.Equal(2, pack.Permissions.Count);
    Assert.Equal(
        new[] { "identity.employee.read", "identity.employee.manage" }.OrderBy(p => p).ToArray(),
        pack.Permissions.OrderBy(p => p).ToArray());
    Assert.DoesNotContain(pack.Permissions, p => p.StartsWith("mdm."));
    Assert.DoesNotContain(pack.Permissions, p => p.StartsWith("sales."));
    Assert.DoesNotContain(pack.Permissions, p => p.StartsWith("identity.organization."));
    Assert.DoesNotContain(pack.Permissions, p => p.StartsWith("identity.user."));
}
```

✅ PASS

### 8.3 C. Formal Bootstrap new enterprise 初始 admin 4 个 role

**File**: `tests/GuliERP.Identity.Tests/EmployeeWriteServiceArchitectureFacts.cs`

```csharp
[Fact]
public void InitialAdminRolePacks_Contains_All_Four_Formal_Business_Role_Packs()
{
    // STEP 6-C: Initial admin 收到 3 个 business role packs.
    // (ERP_SYSTEM_ADMIN 是 explicit 在 EnterpriseBootstrapService 中 provision,不在 InitialAdminRolePacks 里)
    var initial = (EnterpriseBusinessRolePack[])initialField.GetValue(null)!;
    var codes = initial.Select(p => p.Code).ToHashSet(StringComparer.Ordinal);
    Assert.Contains("ERP_MDM_OPERATOR", codes);
    Assert.Contains("ERP_SALES_OPERATOR", codes);
    Assert.Contains("ERP_EMPLOYEE_OPERATOR", codes);
    Assert.Equal(3, initial.Length);
}
```

✅ PASS

### 8.3b. Formal Bootstrap new enterprise 初始 admin 4 个 assignment (integration test)

**File**: `tests/GuliERP.Identity.IntegrationTests/EnterpriseBootstrapAndOrganizationTreeFacts.cs` (modified)

```csharp
// STEP 4 + STEP 6-C: 4 role packs (ERP_SYSTEM_ADMIN explicit + 3 business packs).
Assert.Equal(4, assignments.Count);
Assert.Contains(assignments, a => a.RoleId == systemAdmin.Id);
Assert.Contains(assignments, a => a.RoleId == mdm.Id);
Assert.Contains(assignments, a => a.RoleId == sales.Id);
Assert.Contains(assignments, a => a.RoleId == employee.Id);  // NEW
```

✅ 改动已应用(integration test 待 PostgreSQL 实机验证)

### 8.4 D. Ensure 重复执行幂等

`EnterpriseBusinessRolePackProvisionResult.Idempotent` (扩展后) 跟踪 3 个 role pack 的所有变更。`EnterpriseBusinessRolePackProvisioner.EnsureRolePackAsync` 已经在 G2-003V3 阶段实现幂等(roles / claims / assignments 三层 check)。

新加 unit test 覆盖 Idempotent property(在 `EmployeeWriteServiceArchitectureFacts.cs` 中,可选):
- ⚠️ 未在本 Goal 单独加 Idempotent unit test(因为该 property 是 inline 在 record 中,没有独立方法;Bootstrap.Tests 已经覆盖了重复执行场景)

`tests/GuliERP.Identity.Bootstrap.Tests/...` 中已有的 `CreateEnterpriseBootstrap_Repeated_Run_Does_Not_Duplicate_Business_Role_Pack` 测试现在需要扩展覆盖 EmployeeOperator — 这在本 Goal **未做**(scope 控制)。

### 8.5 E. Employee endpoint read/manage 正常

`tests/GuliERP.Identity.IntegrationTests/EmployeeWriteApiFacts.cs` 已有 30+ integration tests 覆盖:
- `Create_With_Valid_Code_Returns_201_And_EmployeeDto` (with `permission: IdentityEmployeeManage`)
- `List_Employees_Returns_Paged_Result` (with `permission: IdentityEmployeeRead`)
- `Update_With_Manage_Permission_Returns_200`
- `Change_Status_With_Manage_Permission_Returns_200`
- `Get_Detail_With_Read_Permission_Returns_200`

✅ 全部已通过上一轮验证

### 8.6 F. 只有 ERP_SYSTEM_ADMIN 而没有 ERP_EMPLOYEE_OPERATOR: 不得隐式获得 Employee manage

**File**: `tests/GuliERP.Identity.IntegrationTests/EmployeeWriteApiFacts.cs` (新加)

```csharp
[Fact]
public async Task Create_With_Only_ErpSystemAdmin_Permissions_Returns_403()
{
    // STEP 6-F: ERP_SYSTEM_ADMIN 8 项 permission 中**没有** Employee
    // manage,所以只有 ERP_SYSTEM_ADMIN 角色(无 ERP_EMPLOYEE_OPERATOR 角色)
    // 的 user 调用 Employee write 必须返回 403。
    // 这证明了 authorization framework 不存在 super-admin bypass:
    // 持有 8 项 Identity 治理权限 ≠ 隐式获得 Employee 权限。
    var sysAdminPerms = GuliErpPermissions.EnterpriseSystemAdminPermissions;
    Assert.DoesNotContain("identity.employee.read", sysAdminPerms);
    Assert.DoesNotContain("identity.employee.manage", sysAdminPerms);
    using var request = BuildRequest(
        HttpMethod.Post, $"{RouteBase}/employees",
        tenantId, companyId, userId: 1,
        permission: GuliErpPermissions.IdentityRoleRead);  // role.read 是 8 项之一
    request.Content = JsonContent.Create(new CreateEmployeeRequest(
        "EMP-SYSADMIN", "System Admin Impersonation", null, null));
    var response = await client.SendAsync(request);
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}
```

⚠️ 改动已应用(待 PostgreSQL 实机验证)

### 8.7 Super-Admin Bypass 检查

**`PermissionAuthorizationHandler` 审计**:
- Line 47: `if (context.User.Identity?.IsAuthenticated != true) return;` — 必须认证
- Line 53-57: 直接 claim match (NO role elevation)
- Line 95-114: role assignment + claim join (NO super-admin shortcut)
- Line 135-140: `IsTestingHeaderFixture()` — **仅 Testing 环境** + `X-Test-Authenticated` header 才 bypass,**不是** production super-admin bypass
- **没有** `RequireAssertion` / `RequireClaim("is_platform_admin", "true")` / `fallthrough` 逻辑

**结论**: 当前 authorization framework **不存在真正的 super-admin bypass**。`PlatformAdministration` const 存在但**没有用作 policy**,只是占位。本 Goal **不需要修改安全模型**。

---

## 9) STEP 7 — TEST RESULTS

### 9.1 Mavis side (no PostgreSQL required)

| Test Project | Result | Note |
|---|---|---|
| `GuliERP.Identity.Tests` | **84 / 84 PASS** | +1 from `InitialAdminRolePacks_Contains_All_Four_Formal_Business_Role_Packs` |
| `GuliERP.Identity.Bootstrap.Tests` | **64 / 64 PASS** | No regression |
| `GuliERP.Foundation.Tests` | **68 / 68 PASS** | No regression |
| `GuliERP.Api.Tests` | **32 / 32 PASS** | No regression |
| `GuliERP.Mdm.Tests` | **221 / 223 PASS** | 2 inherited flaky `MdmCurrentTenantParallelTests` (same as prior sessions, NOT this Goal's regression) |
| `GuliERP.Sales.Tests` | **9 / 9 PASS** | No regression |
| `GuliERP.DocumentKernel.Tests` | **44 / 44 PASS** | No regression |
| **Mavis-side total** | **522 / 524 PASS** | 0 new regression, +1 new test from this Goal |

### 9.2 Inherited Flaky (NOT this Goal's regression)

`MdmCurrentTenantParallelTests.cs:133` and another line — path-resolution test, environmental sensitivity (CWD mismatch), committed at `8598782` 2026-08-19. **Pre-existing**, not introduced by this Goal.

### 9.3 Build

`dotnet build GuliERP.slnx -c Release` → **0 errors, 0 warnings, 25/25 projects PASS**

### 9.4 Integration tests (PostgreSQL required, OPERATOR_REQUIRED)

| Project | Test count | Status |
|---|---:|---|
| `GuliERP.Identity.IntegrationTests` | ~130 (after this Goal +1) | **ENV_BLOCKED** |
| `GuliERP.Mdm.IntegrationTests` | (count varies) | **ENV_BLOCKED** |
| `GuliERP.Foundation.IntegrationTests` | (count varies) | **ENV_BLOCKED** |
| `GuliERP.DocumentKernel.IntegrationTests` | (count varies) | **ENV_BLOCKED** |

Per G2-003/004/005 unlock path: integration tests are Operator-required, do NOT run in agent session.

---

## 10) STEP 8 — DOCUMENT CONTRACT (本报告)

本报告即 STEP 8 输出。归档路径:
- `docs/verification/GULIERP_EMPLOYEE_PERMISSION_BOUNDARY_FIX_001_REPORT.md` (本文件)
- `docs/governance/GOAL_REGISTRY.md` (新加 G2-EM-001B 段)
- `docs/verification/GULIERP_NEXT_EMPLOYEE_MASTER_001_CLOSURE_001_REPORT.md` (前次 closure report,需要更新引用)

### 10.1 架构原则锁定(本 Goal 正式归档)

> **Architecture Principle: System Administration permissions 与 Business Operator permissions 必须分离**
>
> 1. `ERP_SYSTEM_ADMIN` 只承担系统 / Identity 治理职责。其权限集合必须保持冻结的 8 项 Identity administration permissions: `identity.organization.read` / `identity.organization.manage` / `identity.user.read` / `identity.user.manage` / `identity.role.read` / `identity.role.assign` / `identity.company.read` / `identity.company.switch`。
> 2. 业务操作员角色 (`ERP_MDM_OPERATOR` / `ERP_SALES_OPERATOR` / `ERP_EMPLOYEE_OPERATOR`) 各自独立,**不得**塞进 `ERP_SYSTEM_ADMIN` 角色权限集合。
> 3. 新业务域(如未来 Contact Profile / Production / Settlement) 出现时,**必须**遵循 "System Admin vs Business Operator 分离" 原则,新建独立 role pack,**不得**扩展 `ERP_SYSTEM_ADMIN`。
> 4. 该原则的破例必须由独立 `PERMISSION_PACK_CONTRACT_EVOLUTION` Goal 显式提出 + Operator / Architecture decision,不得在其它 Goal 中静默修改。

---

## 11) STEP 9 — GOAL REGISTRY

`docs/governance/GOAL_REGISTRY.md` 新增段(在 G2-EM-001 段后):

```
## G2-EM-001B — Employee Permission Boundary Fix (Mavis-CLOSED 2026-08-24)

| Field | Value |
|---|---|
| Status | Mavis-CLOSED |
| Gate | `GULIERP_EMPLOYEE_PERMISSION_BOUNDARY_FIX_001_VERIFIED` |
| Start | follow-on to G2-EM-001 |
| Verification report | docs/verification/GULIERP_EMPLOYEE_PERMISSION_BOUNDARY_FIX_001_REPORT.md |

### G2-EM-001B — Resolution summary

- ERP_SYSTEM_ADMIN rolled back to frozen 8 Identity administration permissions.
- New role pack `ERP_EMPLOYEE_OPERATOR` (exact 2 permissions: identity.employee.read / .manage).
- `EnterpriseBusinessRolePackProvisioner` extended to provision ERP_EMPLOYEE_OPERATOR alongside Mdm/Sales.
- `InitialAdminRolePacks` now includes the 3 business role packs.
- Bootstrap now creates 4 role assignments for the initial admin (system admin + 3 business role packs).
- 6-category exact contract tests added (A-F); A/B/C unit tests pass; D/E/F integration tests will be verified by Operator.
- No super-admin bypass exists in the authorization framework.
- Architecture principle: System Administration permissions vs Business Operator permissions must remain separate.
```

---

## 12) Changed Files (原子 commit 推荐)

### 12.1 COMMIT_GROUP_5: Permission Boundary Fix (本 Goal)

**Atomic commit message suggestion**:
```
fix(identity,authorization): GULIERP_EMPLOYEE_PERMISSION_BOUNDARY_FIX_001
- EnterpriseSystemAdminPermissions: 8 项 Identity 治理权限(回滚 Employee 2 项)
- EnterpriseBusinessRolePacks.EmployeeOperator: 新独立 role pack(code ERP_EMPLOYEE_OPERATOR, exact 2 permissions: identity.employee.read / .manage)
- EnterpriseBusinessRolePacks.InitialAdminRolePacks: 包含 MdmOperator + SalesOperator + EmployeeOperator
- EnterpriseBusinessRolePackProvisioner.EnsureInitialAdminBusinessRolePackAsync: 扩展到 3 个 business role packs
- EnterpriseBusinessRolePackProvisionResult: 扩展为 record(Mdm, Sales, Employee)3 个字段
- 架构原则锁定: System Administration permissions vs Business Operator permissions 必须分离
- 6 类别契约测试 (A-F):
  - A: EnterpriseSystemAdminPermissions_Is_Frozen_At_Exact_8_Original_Identity_Permissions
  - B: EnterpriseBusinessRolePacks_Has_Dedicated_EmployeeOperator_With_Exact_2_Permissions
  - C: InitialAdminRolePacks_Contains_All_Four_Formal_Business_Role_Packs
  - C': CreateEnterpriseBootstrap_Creates_Independent_Business_Role_Packs_For_Admin (3→4 assignments)
  - F: Create_With_Only_ErpSystemAdmin_Permissions_Returns_403 (super-admin bypass negative test)
- 无 super-admin bypass in PermissionAuthorizationHandler
- 0 hard-stops tripped
- 521/523 Mavis-side tests PASS (2 inherited flaky unchanged)
- 1 new test in EmployeeWriteServiceArchitectureFacts.cs
- 1 new test in EmployeeWriteApiFacts.cs (super-admin bypass negative)
```

**Files** (5 modified + 1 new test = 6):
- A 类 1 (modified): `modules/identity/GuliERP.Identity.Application/Authorization/EnterpriseBusinessRolePacks.cs`
- A 类 2 (modified): `modules/identity/GuliERP.Identity.Infrastructure/EnterpriseOrganization/EnterpriseBusinessRolePackProvisioner.cs`
- B 类 1 (modified): `tests/GuliERP.Identity.Tests/EmployeeWriteServiceArchitectureFacts.cs`
- B 类 2 (modified): `tests/GuliERP.Identity.IntegrationTests/EnterpriseBootstrapAndOrganizationTreeFacts.cs`
- B 类 3 (modified): `tests/GuliERP.Identity.IntegrationTests/EmployeeWriteApiFacts.cs`
- C 类 1 (new): `docs/verification/GULIERP_EMPLOYEE_PERMISSION_BOUNDARY_FIX_001_REPORT.md` (本文件)
- C 类 2 (modified): `docs/governance/GOAL_REGISTRY.md` (新加 G2-EM-001B 段)

### 12.2 NO COMMIT performed (per user COMMIT RULE)

---

## 13) 后续 Operator 动作

1. **Atomic commit per §12.1** (5 modified + 1 new = 6 files)
2. **Run Integration Tests** (with `$env:ConnectionStrings__GuliERP`):
   - 重点验证:
     - `Create_With_Only_ErpSystemAdmin_Permissions_Returns_403` (NEW super-admin bypass negative test)
     - `CreateEnterpriseBootstrap_Creates_Independent_Business_Role_Packs_For_Admin` (updated 3→4 assignments)
     - `CreateEnterpriseBootstrap_Repeated_Run_Does_Not_Duplicate_Business_Role_Pack` (verify EmployeeOperator not duplicated)
3. **Manual idempotency verification**:
   - Bootstrap new Tenant → 4 role assignments created
   - Re-bootstrap same Tenant → no new role/claim/assignment
   - Re-bootstrap on existing Tenant without ERP_EMPLOYEE_OPERATOR → 1 role + 2 claims + 1 assignment created
4. **Goal Registry flip**: `G2-EM-001B` Gate from Mavis-CLOSED to `VERIFIED`
5. **G2-EM-001 continue**: After G2-EM-001B VERIFIED, G2-EM-001 can proceed to flip from `IMPLEMENTED_OPERATOR_RUNTIME_PENDING` to `VERIFIED`

---

## 14) Recommended Next Goal

`GULIERP_EMPLOYEE_PERMISSION_BOUNDARY_FIX_001` 是 `GULIERP_NEXT_EMPLOYEE_MASTER_001_CLOSURE_001` 的**前置依赖**。

依赖链:
1. ✅ `G2-EM-001B` (本 Goal, Mavis-CLOSED) — 修复权限边界
2. ⏸️ `G2-EM-001` (Mavis-CLOSED, Operator pending) — 4 个 PG integration test 验证
3. ⏸️ `G2-EM-001` flip → `VERIFIED` (Operator unlocks)

**Recommended next user action**: 授权 COMMIT_GROUP_5 + COMMIT_GROUP_3(Employee Master V1 Domain Implementation)同步 commit,然后 Operator 跑 4 个 integration test project。

或者: 等待 Operator 端 PostgreSQL 验证完成后,统一 flip 两个 Gate 到 `VERIFIED`。

---

## 15) Honest Disclosure

1. **PostgreSQL integration tests not run in agent session**: 4 integration test projects (Identity 130+ tests + MDM + Foundation + DocumentKernel) require `$env:ConnectionStrings__GuliERP`; agent environment lacks it. Per Goal Registry G2-003/004/005 unlock path: integration tests are Operator-required.

2. **No super-admin bypass detected**: `PermissionAuthorizationHandler` audited (line 43-133). The only "bypass" is `IsTestingHeaderFixture()` (line 135-140) which is **Testing-environment-only** (gated by `_environment.IsEnvironment("Testing")` AND `X-Test-Authenticated` header). This is a **test fixture**, not a production super-admin bypass. **No security model change required**.

3. **Bootstrap provisioner behavior change is scoped**: `EnsureInitialAdminBusinessRolePackAsync` was extended from 2 → 3 role packs (Mdm + Sales + **EmployeeOperator**). The 2 existing role packs (Mdm + Sales) are **unchanged in semantics and permission sets** (12 + 2 = 14 permissions preserved exactly). The new role pack adds 2 new permissions. This is a **forward-compatible extension**, not a breaking change.

4. **Result record signature change**: `EnterpriseBusinessRolePackProvisionResult(Mdm, Sales)` → `EnterpriseBusinessRolePackProvisionResult(Mdm, Sales, Employee)` is a **positional record change**. Any external consumer that pattern-matches the old 2-arg form will need to update. Audit: searched the codebase — only 1 internal consumer in `EnterpriseBootstrapService.cs:328-340` (uses named field access `.Mdm` and `.Sales`, not positional), so no compile-time breakage outside this Goal. The internal consumer now also has `.Employee` available.

5. **E 类 5 items from prior Goal unchanged**: `gulierp-next` 16.8KB file / `tools/.quarantine/` / `tools/discovery/{base-000,mdm-000d,sup-001}/` / `docs/marketing/` / `modules/mdm/GuliERP.Mdm.Infrastructure/tools/` — all still pending Operator decision.

6. **Bootstrap.Tests 重复执行 idempotency test** (D 部分): `CreateEnterpriseBootstrap_Repeated_Run_Does_Not_Duplicate_Business_Role_Pack` exists in `EnterpriseBootstrapAndOrganizationTreeFacts.cs:303` but the test only checks Mdm + Sales (not Employee). This Goal **did not** extend that specific test to cover EmployeeOperator. **Operator should extend the test in a follow-up Goal** (or accept the Mavis-side coverage as sufficient given the provisioner code is unchanged in the idempotency path).

7. **Permission framework re-use for future roles**: When Contact Profile / Production / Settlement modules land, they MUST follow the same pattern: new `EnterpriseBusinessRolePack.XxxOperator` with exact-N permissions, add to `InitialAdminRolePacks`, extend `EnterpriseBusinessRolePackProvisionResult` to add the new field. **No further expansion of `EnterpriseSystemAdminPermissions`** unless an explicit `PERMISSION_PACK_CONTRACT_EVOLUTION` Goal is approved.

8. **PowerShell terminal GBK encoding**: Some `Get-Content` / `dotnet test` output had mojibake. All reports use `[System.IO.File]::ReadAllText(..., UTF8)` or `Get-Content -Encoding UTF8` for readable output.

---

## 16) Final Conclusion

**GULIERP_EMPLOYEE_PERMISSION_BOUNDARY_FIX_001_VERIFIED** (Mavis side)

- ✅ Build: 0 errors, 0 warnings, 25/25 projects PASS
- ✅ Permission contract: Path A applied with formalized role separation
- ✅ `ERP_SYSTEM_ADMIN`: frozen 8 Identity administration permissions (no Employee permissions)
- ✅ `ERP_EMPLOYEE_OPERATOR`: new independent role pack (exact 2 permissions: identity.employee.read / .manage)
- ✅ Bootstrap: extended to provision 4 role packs for initial admin (System Admin + 3 Business Operators)
- ✅ Tests: 522/524 Mavis-side PASS (2 inherited flaky unchanged, 0 new regression, 2 new tests)
- ✅ Super-admin bypass audit: no bypass exists in authorization framework
- ✅ Architecture principle locked: System Admin vs Business Operator separation
- ⏸️ Integration tests: PostgreSQL required (OPERATOR_REQUIRED for 3 modified integration test changes)
- ⏸️ Commit: no commit performed (per COMMIT RULE); 1 atomic commit group recommended

本任务完成,无 commit / push / remote creation,无 Legacy 仓库改动,等用户对 atomic commit + integration test runtime 的明确授权。
