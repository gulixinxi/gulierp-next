# GuliERP Enterprise Bootstrap Design

## 1. Design Goal

This design implements the first-run enterprise initialization flow for
GuliERP-next without expanding the product into a group ERP, HR system, or
IAM platform.

The goal is simple:

- create the minimum enterprise organization rows required for a new customer;
- keep the administrator experience focused on Company, Plant, Department, and Employee;
- preserve the existing Identity Foundation and RBAC boundary;
- leave future business modules a stable Tenant / Company / Plant organization base.

## 2. Initialization Flow

The bootstrap entry point is an Application Service:

`IEnterpriseBootstrapService.CreateEnterpriseBootstrapAsync`

Input:

- enterprise name
- administrator account
- administrator display name

The service creates the following rows in one controlled transaction:

- `Tenant`
- `Company`
- default `Plant` named `主工厂`
- root `OrganizationUnit` named `公司`
- administrator `GuliErpUser`
- administrator `Employee`
- default `UserCompanyMembership`
- primary `UserOrganizationMembership`

The HTTP endpoint is:

`POST /api/v1/organization/bootstrap`

This endpoint requires authentication and the current user must be a platform
administrator. It returns a DTO containing IDs only. It never returns EF
entities and is not an anonymous enterprise registration endpoint.

Duplicate initialization for the same normalized enterprise code fails with
`409 Conflict` and code `enterprise_bootstrap_already_exists`.

## 3. Data Model Relationship

The bootstrap flow reuses the existing foundation model:

```text
Tenant
  Company
    Plant
    OrganizationUnit
      Employee
        GuliErpUser
```

Important boundaries:

- `Tenant` remains the isolation boundary.
- `Company` remains the legal entity under a tenant.
- `Plant` remains the logistics / production site dimension.
- `OrganizationUnit` remains the department/team tree.
- `Employee` is a lightweight employee profile linked to an optional login user.
- `GuliErpUser` and Identity tables remain the credential and authentication foundation.

Business documents are expected to add `TenantId`, `CompanyId`, and `PlantId`
when their module is ready, but this task does not retrofit SalesOrder,
Workflow Lite, or MDM tables.

## 4. Query Model

The organization console reads:

`GET /api/v1/organization/tree`

The returned DTO shape is:

```text
Company
  Plants
  OrganizationUnits
    Employees
    Children
```

The query service scopes by current tenant. If a current company is available,
the result is limited to that company. If no current company is available and
the user is not a platform administrator, the service uses
`UserCompanyMembership` to limit visible companies.

When no tenant context exists, the service safely returns an empty company list.

## 5. Permission Boundary

RBAC remains the primary permission model.

Implemented now:

- bootstrap endpoint requires authentication;
- bootstrap write action additionally requires platform administrator context;
- organization tree is read-only and scoped by current tenant/company/user context.

Reserved for later:

- role-to-permission policy for organization management;
- DataScope levels such as own data, department data, company data, tenant data;
- administrator screens for editing departments, employees, and role grants.

This task does not introduce a second login system, external IAM, or custom
authorization engine.

## 6. Admin Console

The web console adds:

`系统设置 / 企业组织`

The page is read-first:

- enterprise information summary;
- plant list;
- department tree;
- employee list.

The console uses the existing Vue / Element Plus shell, router, API client, and
authentication state.

## 7. Business Module Integration

Future business modules should consume the organization foundation through
application-layer contracts and current context contracts:

- use `ICurrentTenant` for tenant isolation;
- use `ICurrentCompany` for company scope;
- add `PlantId` only when the module has plant-specific behavior;
- never infer Tenant from Company without an explicit boundary check;
- never read platform administrator flags directly from the user entity.

Recommended adoption order:

1. add Tenant / Company / Plant fields to new module entities as they are built;
2. resolve defaults from the current user/company context;
3. add read filters in application services before adding UI selectors;
4. introduce DataScope enforcement only after role and permission management is ready.

## 8. Rollback Notes

Code rollback is isolated to the organization bootstrap service, organization
tree query service, API endpoint additions, web console route/page/menu, tests,
and this document.

Database rollback for bootstrap-created data should remove rows in dependency
order:

1. user organization memberships;
2. user company memberships;
3. employees;
4. users;
5. organization units;
6. plants;
7. companies;
8. tenants.

No migration is added by this task.
