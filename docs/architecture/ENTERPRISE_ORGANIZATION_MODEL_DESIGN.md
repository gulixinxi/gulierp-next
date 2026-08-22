# GuliERP Enterprise Organization Foundation

## Design Principles

This foundation keeps the product simple for small and medium enterprises while preserving the data boundaries needed by future enterprise customers.

- Tenant is the top isolation boundary.
- Company is the legal/business operating entity under a Tenant.
- Plant is the existing GuliERP term for Factory.
- OrganizationUnit is the existing GuliERP term for Department and team trees.
- Employee is a lightweight company employee profile, not a complete HR module.
- User, Role, role claims, and UserRoleAssignment remain the RBAC core.
- DataScope is reserved as a model/interface contract; this stage does not build a complex rule engine.

The default single-company experience is intentionally quiet: ordinary users do not need to choose Tenant, Company, or Factory. The system resolves their default Company from membership and the default Factory from Company setup.

## Data Model

The implemented model reuses the existing Identity schema instead of creating duplicate organization tables.

| Business Concept | Current Entity | Scope | Notes |
| --- | --- | --- | --- |
| Tenant | `Tenant` | Root | Customer / SaaS isolation boundary. |
| Company | `Company` | Tenant | Legal/business entity. |
| Factory | `Plant` | Company | Production/logistics site; `IsDefault` marks the default factory. |
| Department | `OrganizationUnit` | Company | Tree node; `Type=Department` represents departments. |
| Employee | `Employee` | Company | Lightweight profile linking Company, Department, and optional User. |
| User | `GuliErpUser` | Tenant | ASP.NET Core Identity user with GuliERP fields. |
| Role | `GuliErpRole` | Tenant | RBAC role definition. |
| Permission | Role claims | Tenant | Permission codes are stored as role claims. |

Minimal fields:

- Tenant: `Id`, `Code`, `Name`, `Status`, `CreatedAt`
- Company: `Id`, `TenantId`, `Code`, `Name`, `Status`
- Factory/Plant: `Id`, `TenantId`, `CompanyId`, `Code`, `Name`, `IsDefault`, `Status`
- Department/OrganizationUnit: `Id`, `TenantId`, `CompanyId`, `ParentOrganizationUnitId`, `Code`, `Name`, `Type`, `Status`
- Employee: `Id`, `TenantId`, `CompanyId`, `DepartmentId`, `UserId`, `EmployeeNo`, `Name`, `Status`

Business documents should be able to carry `TenantId`, `CompanyId`, and future `PlantId`, but this stage deliberately does not retrofit every business table.

## Initialization

The first-deployment initialization contract is:

- Input enterprise name: `山东谷粒机械有限公司`
- Tenant name: `山东谷粒机械有限公司`
- Company name: `山东谷粒机械有限公司`
- Factory/Plant name: `主工厂`
- Root organization unit: `公司`

The implementation is exposed through `IEnterpriseOrganizationInitializer`. It is idempotent for the same Tenant/Company code and creates the missing default Company, default Plant, or root OrganizationUnit if needed.

For compatibility with the existing project boundary, this is not exposed as an unauthenticated public HTTP write endpoint. Deployment/bootstrap tooling should invoke the service in a controlled startup or operator path.

## Permissions And DataScope

RBAC remains the authorization core:

- `GuliErpRole` defines roles.
- Permission codes are stored as role claims.
- `UserRoleAssignment` grants roles at Tenant-wide or Company scope.

Reserved DataScope levels:

- `OwnData`
- `DepartmentData`
- `CompanyData`
- `TenantData`

This stage only defines the model/interface vocabulary. It does not implement complex department-manager rules, workflow approval scopes, or a full IAM platform.

## Single Company Usage

For the default small-company deployment:

- Tenant and Company have the same display name.
- One default Factory/Plant is created as `主工厂`.
- The tenant admin has default Company membership.
- Ordinary users operate in their default Company and do not see Tenant/Company/Factory selection unless the product later enables switching.

## Multi Company Extension

Future enterprise expansion can add:

- Multiple Companies under one Tenant.
- Multiple Plants under each Company.
- Department trees per Company.
- Employee records linked to departments and optional login users.
- Company-scoped and Tenant-wide role assignments.
- DataScope enforcement for own, department, company, and tenant-level data.

The design intentionally avoids this stage's prohibited scope: group finance, legal consolidation, cross-company transactions, group approval systems, complex HR, and a full IAM product.

## Rollback

Rollback the database migration by dropping `identity.gulierp_employee`, dropping the `ux_gulierp_plant_company_default` index, and removing `identity.gulierp_plant."IsDefault"`.

Rollback the code by reverting the organization foundation commit(s). No existing MDM, SalesOrder, Workflow Lite, or API Runtime tables need to be changed back because this stage does not modify their schemas.
