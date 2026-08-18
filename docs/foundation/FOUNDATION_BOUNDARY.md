# Foundation Boundary

G0 designs boundaries only. It does not implement a full permission platform.

## Phase One Boundary Entities

| Entity | Boundary |
|---|---|
| User | Identity subject, login name, activation state; no auth implementation in G0 |
| Role | Permission grouping; no full RBAC implementation in G0 |
| UserRole | User-role assignment boundary |
| Tenant | Tenant isolation root |
| Company | Legal/business company boundary |
| Organization | Org tree boundary for future data scope |
| Permission | Permission code/action boundary |
| Audit | Operation/design audit boundary |
| Dictionary | Controlled vocabulary/reference data boundary |

## Reserved For Future Goals

| Entity | Reason |
|---|---|
| Menu | Future navigation authorization |
| ButtonPermission | Future action-level UI policy |
| DataScope | Future row-level policy |
| FieldPolicy | Future field-level read/write policy |
| ApprovalLimit | Future approval authorization |
| Workflow | Future workflow engine boundary |

