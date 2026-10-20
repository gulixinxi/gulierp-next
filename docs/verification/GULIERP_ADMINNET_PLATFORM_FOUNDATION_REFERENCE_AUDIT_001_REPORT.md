# GULIERP_ADMINNET_PLATFORM_FOUNDATION_REFERENCE_AUDIT_001_REPORT

| Field | Value |
|---|---|
| Goal | GULIERP_ADMINNET_PLATFORM_FOUNDATION_REFERENCE_AUDIT_001 |
| Status | `GULIERP_ADMINNET_PLATFORM_FOUNDATION_REFERENCE_AUDIT_001_COMPLETE` |
| Repository | `D:\guli\projects\gulierp-next` |
| Branch | `master` |
| HEAD | `139fe1e940258d71b85d328d88b4e1358c0f7b1e` |
| Task type | Read-only platform foundation / ERP backoffice UX reference audit |
| Outputs | `docs/business/GULIERP_ADMINNET_PLATFORM_FOUNDATION_REFERENCE_AUDIT_001.md`; `docs/verification/GULIERP_ADMINNET_PLATFORM_FOUNDATION_REFERENCE_AUDIT_001_REPORT.md` |
| Actions not performed | No demo write, no DB write, no code change, no migration, no commit, no push |

## 1. Repository guard

Verified current repository state:

```text
git rev-parse --show-toplevel -> D:/guli/projects/gulierp-next
git branch --show-current      -> master
git rev-parse HEAD             -> 139fe1e940258d71b85d328d88b4e1358c0f7b1e
```

Pre-existing dirty worktree was observed outside this audit. The audit did not clean, reset, commit or push.

## 2. Demo evidence boundary

The public demo was treated as a shared read-only reference environment.

Evidence status:

- Login page was directly observed with `Admin.NET.Pro`, username/password/captcha fields, captcha endpoint and login endpoint.
- User confirmed the right-side in-app browser was already logged in at `/dashboard/home`.
- Available automation could not read the right-side in-app browser DOM in this session.
- A separate isolated browser reached the login page, but captcha automation was not used as final evidence.
- Public frontend resources remained inspectable and exposed platform module chunks and endpoint strings.

Sensitive login details were not written into either output file.

## 3. Admin.NET frontend/API evidence used

Public asset/resource inspection showed a Vue-style admin SPA with dependencies and module chunks consistent with:

- Vue, Pinia and vue-router style SPA architecture.
- Element Plus style component usage.
- `vxe-table` / `vxe-pc-ui` table-heavy admin UX.
- `exceljs` import/export capability.
- `jspdf`, `hiprint` and barcode-related print capability.
- File preview/viewer dependencies.
- LogicFlow-related dependencies.

API module chunks/endpoints observed:

| Area | Evidence |
|---|---|
| Auth | `/api/sysAuth/captcha`, `/api/sysAuth/login` |
| User | `/api/sysUser/add`, `/baseInfo`, `/changePwd`, `/delete`, `/grantRole`, `/ownExtOrgList/{userId}`, `/ownRoleList/{userId}`, `/ownUserList`, `/page`, `/resetPwd`, `/setStatus`, `/unlockLogin`, `/updateBaseInfo`, `/update`, `/verifyPwdExpirationTime` |
| Role | `/api/sysRole/add`, `/allTableColumnList`, `/copy`, `/delete`, `/grantApi`, `/grantDataScope`, `/grantMenu`, `/grantRoleTable`, `/grantUser`, `/list`, `/ownMenuList`, `/ownOrgList`, `/ownUserList`, `/page`, `/roleApiList`, `/roleTable/{roleId}`, `/setStatus`, `/update`, `/userApiList`, `/userRoleTableList` |
| Menu | `/api/sysMenu/add`, `/delete`, `/list`, `/loginMenuTree`, `/setStatus`, `/update` |
| Organization | `/api/sysOrg/add`, `/childTree/{id}/{level}`, `/delete`, `/list`, `/update`, `/userChildTree/{level}` |
| Position | `/api/sysPos/add`, `/delete`, `/downloadTemplate`, `/export`, `/import`, `/list`, `/page`, `/update` |
| File | `/api/sysFile/delete`, `/downloadFile`, `/fileBase64`, `/fileByIds`, `/file`, `/folder`, `/page`, `/preview/{id}`, `/update`, `/uploadAvatar`, `/uploadCarousel/{tenantId}`, `/uploadFileFromBase64`, `/uploadFileList`, `/uploadFile`, `/uploadFiles`, `/uploadLogo/{tenantId}`, `/uploadSignature`, `/upload/downloadfile` |
| Print | `/api/sysPrint/add`, `/copy`, `/delete`, `/page`, `/print/{name}`, `/update` |
| Job | `/api/sysJob/addJobDetail`, `/addJobTrigger`, `/cancelJob`, `/cancelSleep`, `/clearJobTriggerRecord`, `/deleteJobDetail`, `/deleteJobTrigger`, `/jobClusterList`, `/jobTriggerList`, `/listJobGroup`, `/pageJobDetail`, `/pageJobTriggerRecord`, `/pauseAllJob`, `/pauseJob`, `/pauseTrigger`, `/persistAll`, `/runJob`, `/startAllJob`, `/startJob`, `/startTrigger` |
| Schedule | `/api/sysSchedule/add`, `/delete`, `/detail/{id}`, `/page`, `/setStatus`, `/update` |
| Notice | `/api/sysNotice/add`, `/delete`, `/lastNotice`, `/notice`, `/page`, `/pageReceived`, `/public`, `/setRead`, `/unReadList`, `/update` |
| Message | `/api/sysMessage/sendAllUser`, `/sendUser` |
| Operation log | `/api/sysLogOp/clear`, `/detail/{id}`, `/export`, `/page`, `/yearDayStats` |
| Visit log | `/api/sysLogVis/clear`, `/list`, `/page`, `/yearDayStats` |
| Exception log | `/api/sysLogEx/clear`, `/detail/{id}`, `/export`, `/page` |
| HTTP log | `/api/sysLogHttp/detail`, `/export`, `/httpClientName`, `/list`, `/page`, `/yearDayStats` |
| Config | `/api/sysConfig/add`, `/batchDelete`, `/batchUpdate`, `/configValueByCode/{code}`, `/delete`, `/detail`, `/groupList`, `/list`, `/page`, `/smPublicKey`, `/update` |
| Dictionary type | `/api/sysDictType/add`, `/allDictList`, `/dataList`, `/delete`, `/detail`, `/list`, `/page`, `/setStatus`, `/update` |
| Dictionary data | `/api/sysDictData/add`, `/dataList/{code}`, `/dataList`, `/delete`, `/detail`, `/list`, `/page`, `/setStatus`, `/update` |

Additional public module evidence included heavier optional areas such as code generation, APIJSON, payment, SMS, email, server monitor, plugin, database/backup, report, MQTT, LDAP and OAuth surfaces. These were classified as future/defer/reject for current GuliERP planning.

## 4. Current GuliERP files inspected

Shell and UI:

- `apps/web/src/layout/navigation.ts`
- `apps/web/src/router.ts`
- `apps/web/src/layouts/ErpShell.vue`
- `apps/web/src/components/mdm/MdmListToolbar.vue`
- `apps/web/src/components/mdm/MdmFormDrawer.vue`
- `apps/web/src/design-system/components/table.css`
- `apps/web/src/design-system/components/form.css`

Identity and authorization:

- `modules/identity/GuliERP.Identity.Domain/Entities/OrganizationUnit.cs`
- `modules/identity/GuliERP.Identity.Domain/Entities/Employee.cs`
- `modules/identity/GuliERP.Identity.Domain/Entities/GuliErpUser.cs`
- `modules/identity/GuliERP.Identity.Domain/Entities/GuliErpRole.cs`
- `modules/identity/GuliERP.Identity.Domain/Entities/UserRoleAssignment.cs`
- `modules/identity/GuliERP.Identity.Domain/Entities/UserOrganizationMembership.cs`
- `modules/identity/GuliERP.Identity.Domain/Entities/UserCompanyMembership.cs`
- `modules/identity/GuliERP.Identity.Application/Authorization/DataScopeContracts.cs`
- `modules/identity/GuliERP.Identity.Application/Authorization/GuliErpPermissions.cs`
- `modules/identity/GuliERP.Identity.Application/Authorization/EnterpriseBusinessRolePacks.cs`

MDM:

- `modules/mdm/GuliERP.Mdm.Domain/Entities/DictionaryType.cs`
- `modules/mdm/GuliERP.Mdm.Domain/Entities/DictionaryItem.cs`
- `modules/mdm/GuliERP.Mdm.Domain/Entities/NumberingRule.cs`
- `modules/mdm/GuliERP.Mdm.Application/Authorization/MdmPermissions.cs`
- `modules/mdm/GuliERP.Mdm.Application/MdmDtos.cs`

Search findings:

- No complete current GuliERP platform File/Attachment module was verified.
- No complete current GuliERP Print platform module was verified.
- No complete current GuliERP Task/Notice/Notification platform module was verified.
- No complete current GuliERP Workflow/BPM module was verified.
- No broad system config module beyond current visible system/enterprise organization evidence was verified.

## 5. Current GuliERP facts used

Shell:

- Two-level navigation with module rail and secondary menu.
- Topbar includes search, AI placeholder, message placeholder, company display/switch, user menu, logout and fullscreen.
- Tab strip includes close current/left/right/others/all and dirty-close handling.
- Real current routes include MDM pages, Sales/Purchase order pages and Enterprise Organization.

Identity:

- `OrganizationUnit` is company-scoped and hierarchical.
- `Employee` has department and optional user binding.
- User/company access is modeled through memberships, not direct `CompanyId` on the user.
- Role assignments may be company-scoped or tenant-wide.
- Organization membership supports multiple organizations and a primary flag.
- Data scope supports company mode plus own/department/company/tenant levels.

MDM:

- Dictionary is modeled as flat option-set type/item data.
- Numbering rules are a real MDM/admin surface while DocumentKernel remains the numbering engine boundary.
- MDM permission codes are stable business capability permissions.

UI:

- List toolbar, form drawer, compact table CSS and compact form CSS already provide a usable ERP admin foundation.

## 6. Verification result

The output audit is complete for the requested reference purpose:

`GULIERP_ADMINNET_PLATFORM_FOUNDATION_REFERENCE_AUDIT_001_COMPLETE`

Evidence limitations retained:

- Logged-in page DOM details were not automatically captured.
- The audit does not claim authenticated CRUD, mutation, page-field behavior or database state from the public demo.
- The audit uses Admin.NET public frontend/API surface as reference evidence and current GuliERP local source as comparison evidence.

## 7. Output file list

Created:

- `D:\guli\projects\gulierp-next\docs\business\GULIERP_ADMINNET_PLATFORM_FOUNDATION_REFERENCE_AUDIT_001.md`
- `D:\guli\projects\gulierp-next\docs\verification\GULIERP_ADMINNET_PLATFORM_FOUNDATION_REFERENCE_AUDIT_001_REPORT.md`

Not performed:

- no code edits;
- no database actions;
- no public demo mutations;
- no migrations;
- no commit;
- no push.
