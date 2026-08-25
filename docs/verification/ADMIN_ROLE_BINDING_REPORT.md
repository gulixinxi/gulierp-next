# ADMIN_ROLE_BINDING_REPORT

## 0. Run Metadata

- Repo:    D:\guli\projects\gulierp-next
- Branch:  master
- HEAD:    d74b98a
- Date:    2026-08-25 13:18:37 +08:00
- User:    admin
- DB conn: Host=192.168.2.228;Port=5432;Database=gulierp_g2_003_test;Username=gulidata;Password=<REDACTED>;Include Error Detail=true
- Mode:    READ-ONLY (psql -X -A -t, no writes)
- Tool:    tools/dev/diagnose-admin-role-binding.ps1

## 1. Q1 — Admin User Identity

~~~
83727350616817894|admin|83727350616817890|1|f
~~~

## 2. Q2 — Active UserCompanyMembership

~~~
83727350616817896|83727350616817890|83727350616817891|t|1
~~~

## 3. Q3 — All ERP_MDM_OPERATOR Roles (across all tenants)

~~~
83727350616817820|83726107798405120|ERP_MDM_OPERATOR|ERP MDM Operator|ERP MDM OPERATOR|t|1|2026-08-21 21:21:15.491121+08
83727350616817910|83727350616817890|ERP_MDM_OPERATOR|ERP MDM Operator|ERP MDM OPERATOR|t|1|2026-08-23 17:41:06.172866+08
~~~

## 4. Q4 — Duplicate Check (count by TenantId)

~~~
83726107798405120|1
83727350616817890|1
~~~

## 5. Q5 — RoleClaims on all ERP_MDM_OPERATOR Roles

~~~
83727350616817820|83726107798405120|gulierp.permission|mdm.business-partner.manage
83727350616817820|83726107798405120|gulierp.permission|mdm.business-partner.read
83727350616817820|83726107798405120|gulierp.permission|mdm.item-category.manage
83727350616817820|83726107798405120|gulierp.permission|mdm.item-category.read
83727350616817820|83726107798405120|gulierp.permission|mdm.item.manage
83727350616817820|83726107798405120|gulierp.permission|mdm.item.read
83727350616817820|83726107798405120|gulierp.permission|mdm.location.manage
83727350616817820|83726107798405120|gulierp.permission|mdm.location.read
83727350616817820|83726107798405120|gulierp.permission|mdm.uom.manage
83727350616817820|83726107798405120|gulierp.permission|mdm.uom.read
83727350616817820|83726107798405120|gulierp.permission|mdm.warehouse.manage
83727350616817820|83726107798405120|gulierp.permission|mdm.warehouse.read
83727350616817910|83727350616817890|gulierp.permission|mdm.business-partner.manage
83727350616817910|83727350616817890|gulierp.permission|mdm.business-partner.read
83727350616817910|83727350616817890|gulierp.permission|mdm.dictionary.manage
83727350616817910|83727350616817890|gulierp.permission|mdm.dictionary.read
83727350616817910|83727350616817890|gulierp.permission|mdm.item-category.manage
83727350616817910|83727350616817890|gulierp.permission|mdm.item-category.read
83727350616817910|83727350616817890|gulierp.permission|mdm.item.manage
83727350616817910|83727350616817890|gulierp.permission|mdm.item.read
83727350616817910|83727350616817890|gulierp.permission|mdm.location.manage
83727350616817910|83727350616817890|gulierp.permission|mdm.location.read
83727350616817910|83727350616817890|gulierp.permission|mdm.uom.manage
83727350616817910|83727350616817890|gulierp.permission|mdm.uom.read
83727350616817910|83727350616817890|gulierp.permission|mdm.warehouse.manage
83727350616817910|83727350616817890|gulierp.permission|mdm.warehouse.read
~~~

## 6. Q6 — Admin's UserRoleAssignments (gulierp_user_role_assignment)

~~~
83727350616817899|83727350616817890|83727350616817891|83727350616817894|83727350616817898|ERP_SYSTEM_ADMIN|1
83727350616817911|83727350616817890|83727350616817891|83727350616817894|83727350616817910|ERP_MDM_OPERATOR|1
83727350616817913|83727350616817890|83727350616817891|83727350616817894|83727350616817912|ERP_SALES_OPERATOR|1
83727350616818001|83727350616817890|83727350616817891|83727350616817894|83727350616818000|ERP_EMPLOYEE_OPERATOR|1
~~~

## 7. Q7 — Admin's legacy AspNetUserRoles (should be empty in GuliERP)

~~~

~~~

## 8. Q8 — Dictionary Claims on Admin's MDM Roles (focused)

~~~
83727350616817910|83727350616817890|ERP_MDM_OPERATOR|mdm.business-partner.manage
83727350616817910|83727350616817890|ERP_MDM_OPERATOR|mdm.business-partner.read
83727350616817910|83727350616817890|ERP_MDM_OPERATOR|mdm.dictionary.manage
83727350616817910|83727350616817890|ERP_MDM_OPERATOR|mdm.dictionary.read
83727350616817910|83727350616817890|ERP_MDM_OPERATOR|mdm.item-category.manage
83727350616817910|83727350616817890|ERP_MDM_OPERATOR|mdm.item-category.read
83727350616817910|83727350616817890|ERP_MDM_OPERATOR|mdm.item.manage
83727350616817910|83727350616817890|ERP_MDM_OPERATOR|mdm.item.read
83727350616817910|83727350616817890|ERP_MDM_OPERATOR|mdm.location.manage
83727350616817910|83727350616817890|ERP_MDM_OPERATOR|mdm.location.read
83727350616817910|83727350616817890|ERP_MDM_OPERATOR|mdm.uom.manage
83727350616817910|83727350616817890|ERP_MDM_OPERATOR|mdm.uom.read
83727350616817910|83727350616817890|ERP_MDM_OPERATOR|mdm.warehouse.manage
83727350616817910|83727350616817890|ERP_MDM_OPERATOR|mdm.warehouse.read
~~~

## 9. Q9 — All Claims on Admin's MDM Roles (consolidated)

~~~
83727350616817910|ERP_MDM_OPERATOR|83727350616817890|mdm.business-partner.manage;mdm.business-partner.read;mdm.dictionary.manage;mdm.dictionary.read;mdm.item-category.manage;mdm.item-category.read;mdm.item.manage;mdm.item.read;mdm.location.manage;mdm.location.read;mdm.uom.manage;mdm.uom.read;mdm.warehouse.manage;mdm.warehouse.read
~~~

## 10. Q10 — Admin's active role codes (full)

~~~
83727350616818000|ERP_EMPLOYEE_OPERATOR
83727350616817910|ERP_MDM_OPERATOR
83727350616817912|ERP_SALES_OPERATOR
83727350616817898|ERP_SYSTEM_ADMIN
~~~

## 11. Q11 — All Tenants (for cross-tenant duplicate interpretation)

~~~
83726107798405120|test_operator_g2_004_t|Operator evidence test tenant (test_operator_g2_004_t)|1
83727350616817720|web_preview_t|Operator evidence test tenant (web_preview_t)|1
83727350616817890|GULI|璋风矑|1
~~~

## 12. Findings (TO BE FILLED after this run)

- Admin user exists? (YES / NO)
- Admin is bound to which role code(s)? (list, from Q10)
- Admin's role has mdm.dictionary.read? (YES / NO, from Q8 / Q9)
- Admin's role has mdm.dictionary.manage? (YES / NO, from Q8 / Q9)
- Duplicate ERP_MDM_OPERATOR in same TenantId? (count, from Q4)

## 13. Interpretation Notes (Reference)

- If Q4 shows count > 1 for the formal Tenant (id 83727350616817890,
  the value of FormalTenantId in Bootstrap/Program.cs:90), the
  existing --ensure-formal-enterprise-business-role-pack is
  BLOCKED. The Provisioner's EnsureRolePackAsync throws
  InvalidOperationException("Duplicate role 'ERP_MDM_OPERATOR'
  exists in tenant {tenantId}.")
  (see modules/identity/.../EnterpriseBusinessRolePackProvisioner.cs:113-117).
- The two role IDs reported by the operator
  (83727350616817910 / 83727350616817820) are 990 apart, suggesting
  they were created in the same Snowflake worker run; consistent with
  a previous bootstrap retry that left a residue role behind.
- Q8 and Q9 are the only queries that actually answer the
  authorization question; the rest are context.
- The legacy AspNetUserRoles table is expected to be empty in
  GuliERP because the canonical join is gulierp_user_role_assignment.
  If Q7 returns rows, that would be a separate anomaly.

## 14. Raw JSON Dump (machine-readable)

~~~json
{
  "Q1_USER": "83727350616817894|admin|83727350616817890|1|f",
  "Q2_USER_COMPANY": "83727350616817896|83727350616817890|83727350616817891|t|1",
  "Q3_MDM_OPERATOR_ROLES": [
    "83727350616817820|83726107798405120|ERP_MDM_OPERATOR|ERP MDM Operator|ERP MDM OPERATOR|t|1|2026-08-21 21:21:15.491121+08",
    "83727350616817910|83727350616817890|ERP_MDM_OPERATOR|ERP MDM Operator|ERP MDM OPERATOR|t|1|2026-08-23 17:41:06.172866+08"
  ],
  "Q4_DUPLICATE_CHECK": [
    "83726107798405120|1",
    "83727350616817890|1"
  ],
  "Q5_ROLE_CLAIMS": [
    "83727350616817820|83726107798405120|gulierp.permission|mdm.business-partner.manage",
    "83727350616817820|83726107798405120|gulierp.permission|mdm.business-partner.read",
    "83727350616817820|83726107798405120|gulierp.permission|mdm.item-category.manage",
    "83727350616817820|83726107798405120|gulierp.permission|mdm.item-category.read",
    "83727350616817820|83726107798405120|gulierp.permission|mdm.item.manage",
    "83727350616817820|83726107798405120|gulierp.permission|mdm.item.read",
    "83727350616817820|83726107798405120|gulierp.permission|mdm.location.manage",
    "83727350616817820|83726107798405120|gulierp.permission|mdm.location.read",
    "83727350616817820|83726107798405120|gulierp.permission|mdm.uom.manage",
    "83727350616817820|83726107798405120|gulierp.permission|mdm.uom.read",
    "83727350616817820|83726107798405120|gulierp.permission|mdm.warehouse.manage",
    "83727350616817820|83726107798405120|gulierp.permission|mdm.warehouse.read",
    "83727350616817910|83727350616817890|gulierp.permission|mdm.business-partner.manage",
    "83727350616817910|83727350616817890|gulierp.permission|mdm.business-partner.read",
    "83727350616817910|83727350616817890|gulierp.permission|mdm.dictionary.manage",
    "83727350616817910|83727350616817890|gulierp.permission|mdm.dictionary.read",
    "83727350616817910|83727350616817890|gulierp.permission|mdm.item-category.manage",
    "83727350616817910|83727350616817890|gulierp.permission|mdm.item-category.read",
    "83727350616817910|83727350616817890|gulierp.permission|mdm.item.manage",
    "83727350616817910|83727350616817890|gulierp.permission|mdm.item.read",
    "83727350616817910|83727350616817890|gulierp.permission|mdm.location.manage",
    "83727350616817910|83727350616817890|gulierp.permission|mdm.location.read",
    "83727350616817910|83727350616817890|gulierp.permission|mdm.uom.manage",
    "83727350616817910|83727350616817890|gulierp.permission|mdm.uom.read",
    "83727350616817910|83727350616817890|gulierp.permission|mdm.warehouse.manage",
    "83727350616817910|83727350616817890|gulierp.permission|mdm.warehouse.read"
  ],
  "Q6_USER_ASSIGNMENTS": [
    "83727350616817899|83727350616817890|83727350616817891|83727350616817894|83727350616817898|ERP_SYSTEM_ADMIN|1",
    "83727350616817911|83727350616817890|83727350616817891|83727350616817894|83727350616817910|ERP_MDM_OPERATOR|1",
    "83727350616817913|83727350616817890|83727350616817891|83727350616817894|83727350616817912|ERP_SALES_OPERATOR|1",
    "83727350616818001|83727350616817890|83727350616817891|83727350616817894|83727350616818000|ERP_EMPLOYEE_OPERATOR|1"
  ],
  "Q7_LEGACY_USER_ROLES": null,
  "Q8_DICTIONARY_CLAIMS_ON_ADMIN_ROLES": [
    "83727350616817910|83727350616817890|ERP_MDM_OPERATOR|mdm.business-partner.manage",
    "83727350616817910|83727350616817890|ERP_MDM_OPERATOR|mdm.business-partner.read",
    "83727350616817910|83727350616817890|ERP_MDM_OPERATOR|mdm.dictionary.manage",
    "83727350616817910|83727350616817890|ERP_MDM_OPERATOR|mdm.dictionary.read",
    "83727350616817910|83727350616817890|ERP_MDM_OPERATOR|mdm.item-category.manage",
    "83727350616817910|83727350616817890|ERP_MDM_OPERATOR|mdm.item-category.read",
    "83727350616817910|83727350616817890|ERP_MDM_OPERATOR|mdm.item.manage",
    "83727350616817910|83727350616817890|ERP_MDM_OPERATOR|mdm.item.read",
    "83727350616817910|83727350616817890|ERP_MDM_OPERATOR|mdm.location.manage",
    "83727350616817910|83727350616817890|ERP_MDM_OPERATOR|mdm.location.read",
    "83727350616817910|83727350616817890|ERP_MDM_OPERATOR|mdm.uom.manage",
    "83727350616817910|83727350616817890|ERP_MDM_OPERATOR|mdm.uom.read",
    "83727350616817910|83727350616817890|ERP_MDM_OPERATOR|mdm.warehouse.manage",
    "83727350616817910|83727350616817890|ERP_MDM_OPERATOR|mdm.warehouse.read"
  ],
  "Q9_ALL_CLAIMS_ADMIN_ROLES": "83727350616817910|ERP_MDM_OPERATOR|83727350616817890|mdm.business-partner.manage;mdm.business-partner.read;mdm.dictionary.manage;mdm.dictionary.read;mdm.item-category.manage;mdm.item-category.read;mdm.item.manage;mdm.item.read;mdm.location.manage;mdm.location.read;mdm.uom.manage;mdm.uom.read;mdm.warehouse.manage;mdm.warehouse.read",
  "Q10_SYSTEM_ADMIN_CHECK": [
    "83727350616818000|ERP_EMPLOYEE_OPERATOR",
    "83727350616817910|ERP_MDM_OPERATOR",
    "83727350616817912|ERP_SALES_OPERATOR",
    "83727350616817898|ERP_SYSTEM_ADMIN"
  ],
  "Q11_TENANT_LIST": [
    "83726107798405120|test_operator_g2_004_t|Operator evidence test tenant (test_operator_g2_004_t)|1",
    "83727350616817720|web_preview_t|Operator evidence test tenant (web_preview_t)|1",
    "83727350616817890|GULI|璋风矑|1"
  ]
}
~~~
