# GULIERP_ADMINNET_PLATFORM_FOUNDATION_REFERENCE_AUDIT_001

| Field | Value |
|---|---|
| Goal | GULIERP_ADMINNET_PLATFORM_FOUNDATION_REFERENCE_AUDIT_001 |
| Nature | Read-only platform foundation / ERP backoffice UX reference audit |
| Reference source | Admin.NET.Pro public demo and public frontend resource/API surface |
| Target repository | `D:\guli\projects\gulierp-next` |
| Branch / HEAD | `master` / `139fe1e940258d71b85d328d88b4e1358c0f7b1e` |
| Positioning | `PLATFORM FOUNDATION REFERENCE`; `ERP BACKOFFICE UX REFERENCE` |
| Hard boundary | No demo write, no DB write, no code change, no migration, no commit, no push |

## 1. Executive conclusion

Admin.NET.Pro is useful to GuliERP as a mature platform-admin and backoffice-UX reference, not as an ERP business architecture source.

The strongest reusable reference is the platform foundation surface:

- user, role, menu, organization, position, permission and data-scope administration;
- dictionary and system-config separation;
- file upload/download/preview infrastructure;
- print-template infrastructure;
- operation/access/exception/HTTP log surfaces;
- task, notice, message and schedule administration;
- dense table/list/form operations, including search, pagination, row actions, batch actions, import/export and template download.

The strongest boundary is equally important:

- Admin.NET system logs are not ERP business audit trails.
- Admin.NET dictionaries are not ERP master data.
- Admin.NET menus/API permissions are not a substitute for GuliERP business capability permissions.
- Admin.NET workflow/code-gen/online low-code capabilities should not drive early GuliERP ERP design.
- Sales, Purchase, Inventory, Document Lineage, Posting/Reversal and partial receipt architecture must be designed separately from ERP requirements and current GuliERP contracts.

Final recommendation: use Admin.NET as a reference library for platform admin coverage and operator UX density; adapt only the concepts that reinforce GuliERP's greenfield architecture.

## 2. Evidence boundary

Demo evidence collected:

| Evidence type | Result | Confidence |
|---|---|---|
| Public demo login shell | Observed `Admin.NET.Pro`, login fields, captcha endpoint and login endpoint. | Direct browser/resource evidence |
| User-confirmed logged-in state | User confirmed the right-side in-app browser was logged in at `/dashboard/home`. | Human-confirmed UI state |
| Logged-in DOM automation | Not available in current tool state; the in-app browser DOM could not be read by automation. | `BROWSER_DOM_EVIDENCE_LIMITED` |
| Public frontend bundle/API surface | Public assets exposed API module chunks and endpoint strings for platform modules. | Direct resource evidence |
| GuliERP source comparison | Current source files inspected in shell, MDM, Identity, permissions, dictionary, numbering, table and form components. | Direct local source evidence |

This audit does not claim field-level logged-in page behavior unless supported by public resource evidence or current GuliERP source evidence.

## 3. Shell / Dashboard

Admin.NET reference:

- Public shell is a Vue/Element Plus style admin platform with module-based routing and dashboard entry.
- Frontend dependencies indicate a mature admin SPA stack: Vue, Pinia, vue-router, Element Plus style components, `vxe-table`, `vxe-pc-ui`, `exceljs`, `jspdf`, `hiprint`, barcode libraries, file preview libraries and LogicFlow-related dependencies.
- This is valuable as an ERP backoffice density reference: compact navigation, rich tables, configurable columns, operational toolbars and form-heavy administration.

Current GuliERP:

- Current shell already has a deliberate ERP-oriented layout: module rail, secondary menu, topbar, company switch/display, global search placeholder, message placeholder, tab strip and dirty-close handling.
- Current modules include Workbench, MDM, Sales, Purchase, Inventory, Production, Quality and System, with many future entries intentionally disabled.

Recommendation: `REFERENCE_UX`. Preserve the GuliERP shell direction. Use Admin.NET only for maturity checks on menu density, toolbar behavior, table conventions and admin-page completeness.

## 4. Organization / Position

Admin.NET reference evidence:

- Frontend API surface includes `sys-org-api` and `sys-pos-api`.
- Organization endpoints include add, delete, list, update, child-tree and user-child-tree style operations.
- Position endpoints include add, delete, list, page, update, import, export and template download.

Current GuliERP:

- `OrganizationUnit` is a company-scoped hierarchy with `ParentOrganizationUnitId` and type values such as Branch, Department, Team and Other.
- `Employee` links to `DepartmentId` and optionally to `UserId`.
- No standalone Position entity was found in the inspected current Identity/MDM source.

Gap:

| Capability | Current status | Priority | Recommendation |
|---|---|---|---|
| Company-scoped organization tree | `ALREADY_EXISTS` | P0 | `REUSE_EXISTING` |
| Department/team modeling | `PARTIAL` | P1 | `ADAPT` |
| Position/job title master | `MISSING` | P2 | `ADAPT` |
| Organization import/export | `MISSING` | P2 | `REFERENCE_UX` |

## 5. User / Role / Permission

Admin.NET reference evidence:

- `sys-user-api` includes user page/list-style management plus role grant, own-role, own-org, own-ext-org, reset password, change password, unlock login and status endpoints.
- `sys-role-api` includes role page/list management plus grant menu, grant API, grant data scope, grant user, role-table and status endpoints.
- `sys-menu-api` includes menu list, login menu tree, add, update, delete and status endpoints.

Current GuliERP:

- `GuliErpUser` is tenant-scoped; company access is modeled through `UserCompanyMembership`.
- `GuliErpRole` is tenant-scoped; `UserRoleAssignment` supports tenant-wide or company-scoped assignments through nullable `CompanyId`.
- `UserOrganizationMembership` supports multi-organization membership with a primary flag.
- Permission constants are business capability codes, not raw URL/menu-node copies.
- Existing role packs include system admin and ERP operator roles for MDM, Sales, Employee and Purchase.

Recommendation: `ADAPT`. Admin.NET gives a strong platform-admin coverage checklist, but GuliERP should keep its capability-code permission model and company-aware assignments.

## 6. Data Scope

Admin.NET reference evidence:

- `sys-role-api` includes `grantDataScope`, `ownOrgList` and related user/role surfaces.

Current GuliERP:

- `DataScopeMode` supports `CurrentCompany` and `AllAllowedCompanies`.
- `DataScopeLevel` supports `OwnData`, `DepartmentData`, `CompanyData` and `TenantData`.
- This is already more ERP-aligned than a generic organization-only data-scope model because it separates company access from department/tenant reach.

Recommendation: `REUSE_EXISTING` for the core contract; `REFERENCE_UX` for the role-grant page patterns.

## 7. Dictionary

Admin.NET reference evidence:

- `sys-dict-type-api` includes add, delete, detail, list, page, status, update, all-dict-list and data-list endpoints.
- `sys-dict-data-api` includes add, delete, detail, list, page, status, update and code-based data-list endpoints.

Current GuliERP:

- `DictionaryType` and `DictionaryItem` model tenant-scoped flat option sets.
- Current GuliERP dictionary boundary is already narrow: flat system-maintainable option sets only.
- MDM entities such as UOM, warehouse, item, business partner, payment method and numbering rules are separate business/master data surfaces.

### ADMINNET_DICTIONARY_BOUNDARY_RECOMMENDATION

| Data type | Admin.NET reference value | GuliERP decision |
|---|---|---|
| UI option lists | Mature reference | `ADAPT` |
| Enable/disable option values | Mature reference | `ADAPT` |
| System-maintained code/value options | Mature reference | `ADAPT` |
| UOM | Not dictionary in GuliERP | `REJECT` as dictionary |
| Warehouse/location | Not dictionary in GuliERP | `REJECT` as dictionary |
| Currency/tax/price/payment business rules | Not generic dictionary | `DESIGN_SEPARATELY` |
| Dynamic form/low-code fields | Outside current boundary | `DEFER` |

Recommendation: keep GuliERP's strict dictionary boundary. Reference Admin.NET's list/detail/status UX, not its physical implementation.

## 8. System Config

Admin.NET reference evidence:

- `sys-config-api` includes add, batch delete, batch update, detail, group list, list, page, public-key/value query and update endpoints.

Current GuliERP:

- Current system menu evidence is limited; Enterprise Organization is the visible implemented system page.
- No broad operator-facing system-config module was found in the inspected current source.

Recommendation: `P1 / ADAPT`. Add system config only for clearly platform-owned settings. Do not let system config become a hidden business-rule database.

## 9. File / Attachment

Admin.NET reference evidence:

- `sys-file-api` includes file page, folder, upload file/files/list, upload avatar/logo/carousel/signature, base64 upload, download, preview, file-by-ids, update and delete.
- Public dependencies indicate file-viewer support.

Current GuliERP:

- Broad search did not find a complete GuliERP file/attachment platform module.
- Form CSS includes upload-area styles, but that is not backend attachment infrastructure.

Recommendation: `P1 / ADAPT`. GuliERP needs a platform file service before ERP documents rely on attachments, but the domain boundary should be ERP-safe: ownership, retention, virus/size/type policy, tenant/company scope and audit metadata.

## 10. Import / Export

Admin.NET reference evidence:

- Position endpoints include import/export/template download.
- Logs include export endpoints.
- Frontend dependencies include `exceljs`.

Current GuliERP:

- Current list toolbar and table styles are ready for dense ERP operations.
- No generic import/export framework was verified for all MDM/platform pages in the inspected source.

Recommendation: `P1 / REFERENCE_UX`. Start with operator-safe exports and import templates for MDM/admin lists. Avoid silent destructive imports.

## 11. Print

Admin.NET reference evidence:

- `sys-print-api` includes add, copy, delete, page, print-by-name and update.
- Public dependencies include `hiprint`, `jspdf` and barcode libraries.

Current GuliERP:

- No complete print-template platform module was found in inspected source.

Recommendation: `P2 / ADAPT`. Treat print templates as a later platform capability after core ERP documents stabilize. Early Sales/Purchase/Inventory design should not be blocked by generic print-template completion.

## 12. Logs / Audit

Admin.NET reference evidence:

- Operation log: clear, detail, export, page, year/day stats.
- Visit log: clear, list, page, year/day stats.
- Exception log: clear, detail, export, page.
- HTTP log: detail, export, client name, list, page, year/day stats.

### SYSTEM_LOG_VS_ERP_AUDIT_BOUNDARY

| Area | Admin.NET reference | GuliERP boundary |
|---|---|---|
| Login/access trace | Platform security log | Platform log |
| HTTP/client exception trace | Platform observability | Platform log |
| Operator CRUD trace | Platform/system operation log | Useful but not enough for ERP audit |
| Sales order approval/posting | Not system log | ERP business audit |
| Inventory receipt/issue posting | Not system log | ERP business audit/ledger |
| Reversal/correction | Not system log | ERP business audit/ledger |

Recommendation: `ADAPT` platform logs, but `DESIGN_SEPARATELY` for ERP business audit, document history, posting ledger and reversal evidence.

## 13. Task / Notice / Notification

Admin.NET reference evidence:

- `sys-job-api` includes job detail, trigger, run, start, pause, cancel, persist, records and cluster list operations.
- `sys-schedule-api` includes add, delete, detail, page, status and update.
- `sys-notice-api` includes add, delete, last notice, notice page, received page, public, read/unread and update operations.
- `sys-message-api` includes send-all-user and send-user endpoints.

Current GuliERP:

- Topbar has a message placeholder, but no complete platform notice/message/job center was found in inspected source.

Recommendation: `P2 / ADAPT`. Notice and message center are valuable; job management must be protected and should not be exposed broadly in early ERP operator roles.

## 14. Workflow

Admin.NET reference evidence:

- Public dependencies include LogicFlow-related resources and optional/heavy workflow-like capabilities.
- This is bundle/dependency evidence only, not a verified ERP workflow implementation audit.

Current GuliERP:

- Sales and Purchase currently have simple status enums in implemented code.
- Frozen ERP document lifecycle concepts must come from GuliERP architecture docs and ERP requirements, not Admin.NET workflow availability.

Recommendation: `FUTURE / DEFER`. Do not introduce generic BPM as P0. First freeze document status, lineage, approval/posting boundaries and business audit requirements.

## 15. Table UX

### ADMINNET_TABLE_UX_REFERENCE_MATRIX

| UX capability | Admin.NET reference | Current GuliERP | Recommendation |
|---|---|---|---|
| Dense list layout | Strong reference via admin stack and table libraries | Existing compact ERP table CSS | `REFERENCE_UX` |
| Search/filter toolbar | Expected admin pattern | `MdmListToolbar` exists | `ADAPT` |
| Row actions | Expected admin pattern | CSS supports row actions | `ADAPT` |
| Batch actions | Expected admin pattern | CSS supports bulk bar | `ADAPT` |
| Pagination | Expected admin pattern | Current pages use list patterns | `ADAPT` |
| Column settings | Strong reference via table stack | CSS classes exist | `REFERENCE_UX` |
| Import/export/template | Endpoint evidence in platform modules | Partial/not generic | `P1 / ADAPT` |
| Dangerous actions | Present in admin platform | Must be permission-gated and confirmed | `ADAPT_WITH_GUARD` |

## 16. Form UX

| UX capability | Admin.NET reference | Current GuliERP | Recommendation |
|---|---|---|---|
| Drawer/modal edit forms | Mature admin convention | `MdmFormDrawer` exists | `REUSE_EXISTING` |
| Compact labels | Mature admin convention | Form CSS supports compact ERP form layout | `REUSE_EXISTING` |
| Multi-column header fields | Useful for ERP documents | Form CSS has 4-column document grid | `REUSE_EXISTING` |
| Lookup fields | Common in backoffice admin | Lookup input styles exist | `ADAPT` |
| Upload fields | File module reference exists | CSS only; backend missing | `P1 / ADAPT` |
| Sticky save/cancel footer | Mature admin convention | Drawer footer pattern exists | `REUSE_EXISTING` |

## 17. GuliERP Gap Matrix

| Capability | Current GuliERP status | Admin.NET reference status | Priority | Recommendation |
|---|---|---|---|---|
| ERP shell | `ALREADY_EXISTS` | Mature admin shell reference | P0 | `REUSE_EXISTING` |
| Organization tree | `ALREADY_EXISTS` | Mature platform org reference | P0 | `ADAPT` |
| Position management | `MISSING` | API surface present | P2 | `ADAPT` |
| User admin | `PARTIAL` | Mature API surface present | P1 | `ADAPT` |
| Role admin | `PARTIAL` | Mature grant surfaces present | P1 | `ADAPT` |
| Permission model | `ALREADY_BETTER` for ERP capability codes | Menu/API grant reference | P0 | `REUSE_EXISTING` |
| Data scope contract | `ALREADY_EXISTS` | Grant data scope reference | P1 | `REFERENCE_UX` |
| Dictionary | `ALREADY_EXISTS` | Mature dictionary surface | P1 | `ADAPT` |
| System config | `MISSING/PARTIAL` | Mature config surface | P1 | `ADAPT` |
| File/attachment | `MISSING` | Mature file surface | P1 | `ADAPT` |
| Import/export | `PARTIAL/MISSING` | Mature endpoint/tooling evidence | P1 | `REFERENCE_UX` |
| Print template | `MISSING` | Mature print surface | P2 | `ADAPT` |
| Platform logs | `PARTIAL` | Mature log surfaces | P1 | `ADAPT` |
| ERP business audit | `MISSING/PARTIAL` | Not sufficient | P0 | `DESIGN_SEPARATELY` |
| Notice/message | `MISSING/PARTIAL` | Mature reference | P2 | `ADAPT` |
| Job scheduler UI | `MISSING` | Mature reference | FUTURE | `DEFER` |
| Workflow/BPM | `MISSING` | Dependency/surface evidence only | FUTURE | `DEFER` |
| Code generator/low-code | `NOT_NEEDED` | Mature Admin.NET capability | REJECT | `REJECT` |
| Payment/SMS/email/LDAP/OAuth/plugin ecosystem | `NOT_NEEDED` for current slice | Public module evidence | FUTURE/REJECT | `DEFER` or `REJECT` |

## 18. Capabilities to reuse

Reuse current GuliERP direction:

- ERP shell architecture and company-aware navigation.
- Capability-code permission model.
- Tenant/company/user/organization membership modeling.
- MDM dictionary as flat option set only.
- Current list toolbar, form drawer and compact table/form CSS foundations.
- DocumentKernel numbering as platform capability, independent from Admin.NET.

## 19. Capabilities to adapt

Adapt from Admin.NET conceptually:

- User/role/org/position administration page coverage.
- Role grant UX for menu/action/API/data-scope style assignment, translated into GuliERP capabilities.
- Dictionary and config management page patterns.
- File/attachment platform service.
- Import/export/template-download workflows.
- Print-template management after ERP document formats stabilize.
- Platform logs and message/notice center.

## 20. UX to reference

Use Admin.NET as an operator-density benchmark:

- tables must support fast scanning, pagination, search, filters and compact row actions;
- dangerous actions should be permission-gated, confirmable and visible in logs;
- import/export should use templates and explicit validation feedback;
- grant pages should make assigned/unassigned state obvious;
- system admin pages should separate routine operator tasks from dangerous maintenance operations.

## 21. Capabilities to reject / defer

Reject as direct GuliERP architecture source:

- legacy runtime/auth/menu/web shell copying;
- Furion/SqlSugar/Admin.NET infrastructure as implementation baseline;
- code generation as the primary ERP delivery model;
- APIJSON/online database-edit surfaces;
- generic dictionary as ERP master-data storage;
- system operation logs as ERP audit ledger;
- Admin.NET workflow availability as proof of ERP approval/posting design.

Defer:

- generic BPM/workflow designer;
- scheduler cluster operations in ordinary UI;
- plugin marketplace/server monitor/database backup pages;
- payment/SMS/email/LDAP/OAuth integrations unless a product slice explicitly needs them.

## 22. P0 / P1 / P2 / Future

| Priority | Items |
|---|---|
| P0 | Preserve GuliERP greenfield boundaries; keep current shell; keep company-aware permissions/data scope; design ERP business audit separately; prevent dictionary/master-data confusion. |
| P1 | Complete platform user/role/org/data-scope admin UX; dictionary/config admin; file/attachment foundation; import/export framework; platform logs. |
| P2 | Position master; print-template management; notice/message center; richer table column settings; safer job visibility. |
| FUTURE | Workflow/BPM designer; scheduler cluster control; plugin/server/db-backup surfaces; external payment/SMS/email/LDAP/OAuth integrations. |
| REJECT | Direct Admin.NET implementation copy; code-gen-first ERP model; generic dictionary for ERP master data; system log as ERP ledger. |

## 23. Recommendations

1. Treat Admin.NET as a completeness checklist for platform admin pages, not a source of ERP business truth.
2. Implement GuliERP platform admin in this order: user/role/data-scope, dictionary/config, file attachment, import/export, platform logs.
3. Keep role and permission internals aligned to GuliERP capability codes, while borrowing grant-page UX from Admin.NET.
4. Keep dictionary limited to option sets; create real master-data modules for ERP-owned concepts.
5. Build ERP document audit, lineage, posting and reversal as separate business capabilities.
6. Mark workflow/BPM and low-code/code-generation capabilities as future/reference only until core ERP execution contracts are stable.

## 24. Final status

`GULIERP_ADMINNET_PLATFORM_FOUNDATION_REFERENCE_AUDIT_001_COMPLETE`

The audit is complete for platform foundation and ERP backoffice UX reference purposes. Logged-in DOM automation remains limited, so this report intentionally avoids unsupported page-field claims and relies on public frontend/API surface evidence plus current GuliERP source inspection.
