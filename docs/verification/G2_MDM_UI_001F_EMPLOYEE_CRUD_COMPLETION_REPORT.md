# G2-MDM-UI-001F Employee CRUD Completion Report

## 1. Repo / Branch / HEAD

- Repo: `D:\guli\projects\gulierp-next`
- Branch: `master`
- HEAD at implementation time: `2badaf9 docs(mdm): define UOM reference path for master data UI`
- Scope: Employee Master frontend CRUD completion following the UOM canonical reference path.
- Commit policy: report first, no commit in this round.
- Push policy: NO PUSH.

## 2. Backend Employee Endpoint Contract

Employee Master is owned by the Organization / Identity bounded context, not by `/api/v1/mdm`.

Endpoint source:

- `apps/api/GuliERP.Api/Organization/OrganizationEndpoints.cs`

Application contract:

- `modules/identity/GuliERP.Identity.Application/Employee/EmployeeDtos.cs`
- `modules/identity/GuliERP.Identity.Application/Employee/IEmployeeWriteService.cs`

Service implementation:

- `modules/identity/GuliERP.Identity.Infrastructure/EmployeeSvc/EmployeeWriteService.cs`

Tests:

- `tests/GuliERP.Identity.Tests/EmployeeWriteServiceFacts.cs`
- `tests/GuliERP.Identity.IntegrationTests/EmployeeWriteApiFacts.cs`

### 2.1 Endpoints

| Purpose | Method / Path | Authorization |
| --- | --- | --- |
| Company selector | `GET /api/v1/organization/companies` | `IdentityCompanyRead` |
| Department selector | `GET /api/v1/organization/companies/{companyId}/departments` | authenticated organization group |
| Employee list | `GET /api/v1/organization/companies/{companyId}/employees/paged` | `IdentityEmployeeRead` |
| Employee detail | `GET /api/v1/organization/employees/{id}` | `IdentityEmployeeRead` |
| Create employee | `POST /api/v1/organization/employees` | `IdentityEmployeeManage` |
| Update employee | `PUT /api/v1/organization/employees/{id}` | `IdentityEmployeeManage` |
| Change status | `POST /api/v1/organization/employees/{id}/status` | `IdentityEmployeeManage` |

Mutation endpoints are unsafe methods and therefore use the existing frontend `api/http.ts` behavior: cookie credentials plus CSRF token through `X-CSRF-TOKEN`.

### 2.2 DTO Fields

`EmployeeDto`:

- `Id`
- `TenantId`
- `CompanyId`
- `DepartmentId`
- `UserId`
- `EmployeeNo`
- `Name`
- `Status`
- `CreatedAt`
- `CreatedBy`
- `ModifiedAt`
- `ModifiedBy`
- `ConcurrencyVersion`

V1 explicitly does not expose contact fields. `Phone`, `Email`, `Mobile`, `WeChatId`, `WeComId`, `QRCode`, and `Position` are not part of this goal.

### 2.3 Request Shapes

Create:

- `CreateEmployeeRequest(EmployeeNo, Name, DepartmentId, UserId)`
- Required by service validation: `EmployeeNo`, `Name`
- `DepartmentId` optional, but when supplied it must belong to the current Tenant + Company and be Active.
- `UserId` optional, but not surfaced in the current UI because there is no approved user selector in this goal.

Update:

- `UpdateEmployeeRequest(Name, DepartmentId, ExpectedConcurrencyVersion)`
- `EmployeeNo` is immutable in V1.
- `UserId` is immutable in V1.
- `ExpectedConcurrencyVersion` is required.

Status:

- `SetEmployeeStatusRequest(Status, ExpectedConcurrencyVersion)`
- `ExpectedConcurrencyVersion` is required.

### 2.4 EmployeeNo Rule

The service canonicalizes `EmployeeNo` by trimming and uppercasing before validation. The frontend still guides operators to enter the valid canonical shape directly:

- starts with uppercase letter
- uppercase letters, digits, and underscore only
- length 2-40
- examples accepted by tests: `EMP_001`, `EMP_FIN_001`, `EMP_X7`, `EMPLOYEE_TEST_001`
- examples rejected by tests: lowercase, hyphenated codes, codes starting with digit/underscore, reserved names, and codes resembling document numbers

### 2.5 Status Values

`EmployeeStatus`:

- `1` = `Active` / 在职
- `2` = `Inactive` / 停用
- `99` = `Left` / 离职

The UI implements enable/disable for `Active <-> Inactive`. `Left` is terminal; the page disables edit and reactivation for left employees.

### 2.6 Tenant / Company Scope

- Tenant and company are resolved from the authenticated request context.
- The list URL includes `companyId`, and the backend requires it to match the current company context.
- Create and update do not accept `companyId` in the body.
- The frontend keeps the existing selected company logic and does not fabricate company scope.

## 3. Modified Files

- `apps/web/src/api/mdm/employee.ts`
- `apps/web/src/views/mdm/EmployeeList.vue`
- `docs/verification/G2_MDM_UI_001F_EMPLOYEE_CRUD_COMPLETION_REPORT.md`

No backend files were changed.

## 4. UOM Pattern Reuse

Employee now follows the UOM reference path where the backend contract allows it:

- real API client remains the only data source
- page uses `MdmListToolbar`, `MdmFormDrawer`, `MdmPagination`, and `MdmEmptyState`
- create/edit share one drawer
- code field is disabled in edit mode
- edit re-reads detail first to obtain the fresh `ConcurrencyVersion`
- status change re-reads detail first through the API client before posting status
- save/status success refreshes the list
- `authentication_required` is re-thrown for global auth handling
- API errors render operator-facing messages
- concurrency conflicts get a specific warning
- empty state remains separate from API failure

Differences from UOM are intentional:

- Employee endpoints live under `/api/v1/organization`.
- Employee status has three values; only `Active` and `Inactive` participate in enable/disable.
- Employee uses a dedicated status endpoint instead of updating status through the normal update endpoint.
- Employee does not expose phone/email in V1.

## 5. Implementation Status

### 5.1 Create

Implemented.

- Toolbar and empty-state create actions now open the Employee form drawer.
- Fields:
  - company display, read-only
  - employeeNo, required for create
  - name, required
  - departmentId, optional selector from real department directory
  - status display, read-only
- Calls `createEmployeeFromForm`, which maps to `POST /api/v1/organization/employees`.
- Saves and refreshes the list on success.

### 5.2 Edit

Implemented.

- Row edit and row double-click open the same drawer.
- Edit first calls `GET /api/v1/organization/employees/{id}` to refresh authoritative fields and `ConcurrencyVersion`.
- `employeeNo` is disabled in edit because backend V1 makes it immutable.
- Calls `updateEmployeeFromForm`, which maps to `PUT /api/v1/organization/employees/{id}`.
- Saves and refreshes the list on success.
- `Left` employees are terminal and cannot be edited.

### 5.3 Enable / Disable

Implemented.

- Active employees show `停用`.
- Inactive employees show `启用`.
- Left employees show disabled `已离职`.
- Both enable and disable ask for confirmation.
- The API client re-reads the employee before posting status to use a fresh `ConcurrencyVersion`.
- Calls `POST /api/v1/organization/employees/{id}/status`.

## 6. Field Mapping

| UI field | Backend request / DTO | Notes |
| --- | --- | --- |
| `employeeNo` | `CreateEmployeeRequest.EmployeeNo`, `EmployeeDto.EmployeeNo` | create only; immutable on edit |
| `name` | `CreateEmployeeRequest.Name`, `UpdateEmployeeRequest.Name` | required, max 200 |
| `departmentId` | `CreateEmployeeRequest.DepartmentId`, `UpdateEmployeeRequest.DepartmentId` | optional; selected from current company departments |
| `status` | `EmployeeDto.Status`, `SetEmployeeStatusRequest.Status` | changed only by status action |
| `companyId` | list route parameter and backend request context | not sent in create/update body |
| `userId` | `EmployeeDto.UserId` | displayed only; no edit path in V1 UI |
| `phone/email` | not supported | not added |

## 7. Form Validation

Frontend validation:

- `employeeNo` required on create.
- `employeeNo` pattern: `/^[A-Z][A-Z0-9_]{1,39}$/`.
- `name` required.
- `name` max length 200.
- Department selector only lists departments returned by the current company's real Organization directory endpoint.

Backend remains source of truth for:

- reserved employee numbers
- document-number-like codes
- duplicate employee numbers
- cross-company department references
- inactive department references
- concurrency conflicts
- terminal `Left` status behavior

## 8. Verification

Commands were executed from `D:\guli\projects\gulierp-next\apps\web` because the repository root has no `package.json`.

- `npm run typecheck`: PASS
- `npm run build`: PASS

Build warnings observed and unchanged:

- Rollup removed two `/* #__PURE__ */` annotations from `node_modules/@vueuse/core`.
- Vite/esbuild reported duplicate generated `modelModifiers` keys in `src/components/mdm/MdmFormDrawer.vue`.
- Large chunk warning after minification.

## 9. HTTP Route Verification

Vite dev server command:

- `npm run dev -- --host 127.0.0.1`

Observed port:

- `http://127.0.0.1:5174/` because `5173` was already in use.

Route checks:

- `GET http://127.0.0.1:5174/mdm`: HTTP 200
- `GET http://127.0.0.1:5174/mdm/employees`: HTTP 200

The temporary Vite server was stopped after verification.

## 10. Runtime CRUD Result

True authenticated runtime mutation was not completed in this automated run.

Evidence:

- `GET http://127.0.0.1:5174/api/v1/organization/companies`: HTTP 401
- This is the expected `authentication_required` result when no ASP.NET Core Identity login cookie is present.

Runtime blocker:

- `authentication_required`
- missing authenticated session cookie
- missing browser-obtained CSRF token for unsafe methods

No authentication bypass was used. CRUD should be manually verified in a browser session with a real operator login:

1. Start API.
2. Start Web.
3. Login in browser.
4. Open `/mdm/employees`.
5. Confirm list loads.
6. Create a test employee with a valid `EMP_...` employee number.
7. Edit the employee name or department.
8. Disable and re-enable the employee.
9. Confirm list refresh and Network calls include cookie credentials and `X-CSRF-TOKEN` for mutations.

## 11. Boundary Statements

- no Identity changes
- no Bootstrap changes
- no G2-005 changes
- no migration changes
- no database schema changes
- no base dictionary changes
- no numbering rule changes
- no purchase/sales/inventory document changes
- unrelated WIP preserved
- NO PUSH
