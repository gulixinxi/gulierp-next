# G2-MDM-UI-001C Employee Master UI Report

## Repo / HEAD

- Repo: `D:\guli\projects\gulierp-next`
- Branch: `master`
- HEAD: `db43a63 fix(mdm): align master data API client contracts`
- Date: 2026-08-25

## Backend Employee / Organization Asset Audit

Existing backend employee capability is exposed from the Organization API boundary:

- `GET /api/v1/organization/companies` lists companies available to the current authenticated user.
- `GET /api/v1/organization/companies/{companyId}/employees` exists as the legacy flat employee directory endpoint.
- `GET /api/v1/organization/companies/{companyId}/employees/paged` supports paged employee list with `departmentId`, `status`, `keyword`, `page`, and `pageSize`.
- `GET /api/v1/organization/employees/{id}` returns employee detail.
- `POST /api/v1/organization/employees` creates an employee.
- `PUT /api/v1/organization/employees/{id}` updates employee name / department with optimistic concurrency.
- `POST /api/v1/organization/employees/{id}/status` changes employee status with optimistic concurrency.

Application contract found:

- `EmployeeDto`: `id`, `tenantId`, `companyId`, `departmentId`, `userId`, `employeeNo`, `name`, `status`, `createdAt`, `createdBy`, `modifiedAt`, `modifiedBy`, `concurrencyVersion`.
- `EmployeeStatus`: `Active = 1`, `Inactive = 2`, `Left = 99`.
- `IEmployeeWriteService`: `CreateAsync`, `GetByIdAsync`, `UpdateAsync`, `ChangeStatusAsync`, `ListByCompanyAsync`.

No backend files were modified in this phase.

## Changed Files

- `apps/web/src/api/mdm/employee.ts`
- `apps/web/src/views/mdm/EmployeeList.vue`
- `apps/web/src/router/mdm.ts`
- `apps/web/src/layout/navigation.ts`
- `apps/web/src/views/mdm/MasterDataWorkbench.vue`
- `docs/verification/G2_MDM_UI_001C_EMPLOYEE_MASTER_UI_REPORT.md`

## Route

- Added `/mdm/employees`
- Route name: `mdm-employees`
- Component: `EmployeeList.vue`
- Meta title: `员工档案`

## Menu Entry

- Added Shell entry under the `主数据` module:
  - Label: `员工档案`
  - Route: `/mdm/employees`
  - Icon: `UserFilled`

The pre-existing disabled placeholder under `基础数据` was not changed to avoid a broader Shell navigation cleanup.

## Master Data Workbench Card

`MasterDataWorkbench.vue` now includes `员工档案` as a primary module card:

- Route: `/mdm/employees`
- Status: `真实 API`
- Description: `按公司查看员工号、姓名、部门、关联用户与状态`

`员工档案` was removed from the deferred module list. `基础字典` and `编号规则` remain deferred.

## API / Mock Status

- `EmployeeList.vue` imports `apps/web/src/api/mdm/employee.ts`.
- `EmployeeList.vue` does not import `apps/web/src/mock/mdm.ts`.
- `employee.ts` calls real Organization endpoints through the existing fetch HTTP client.
- The page lists companies from `/api/v1/organization/companies`, then lists employees from `/api/v1/organization/companies/{companyId}/employees/paged`.
- Create / update / status API functions are defined in the client because backend endpoints exist, but the first UI phase keeps create / edit / enable-disable controls disabled until runtime auth + CSRF CRUD evidence is available.

## Page Capability

- List: connected to real paged API.
- Search: sends `keyword` to the real paged API.
- Status display/filter: supports `Active`, `Inactive`, `Left`.
- Company selector: uses the authenticated user's company list and defaults to the first returned company.
- Create: visible entry exists, but opens a "后续接入" notice.
- Edit / enable-disable: visible as disabled row actions, reported as a follow-up gap.

## Verification

- `npm run typecheck`: PASS
- `npm run build`: PASS
- HTTP route verification:
  - `GET http://127.0.0.1:5173/mdm`: 200
  - `GET http://127.0.0.1:5173/mdm/employees`: 200
- API runtime verification:
  - `GET http://127.0.0.1:5000/api/v1/organization/companies`: 401

## Runtime Blocker

Runtime CRUD closure is blocked by unauthenticated session state:

- `authentication_required`
- missing authenticated browser/API session
- mutations would also require a valid CSRF token

No CRUD success is claimed in this report.

## Boundaries

- No Identity changes.
- No Bootstrap changes.
- No G2-005 changes.
- No migration changes.
- No database schema changes.
- No backend endpoint changes.
- No dictionary or numbering-rule work started.
- No purchase/sales document work started.
- NO PUSH.
