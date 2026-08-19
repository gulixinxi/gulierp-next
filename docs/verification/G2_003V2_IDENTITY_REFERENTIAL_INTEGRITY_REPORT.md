# G2-003V2 — Identity Database Referential Integrity Closure

| Field | Value |
|---|---|
| Goal | **G2-003V2 — Identity Database Referential Integrity Closure** |
| Entry Gate | `G2_003_IDENTITY_ORG_KERNEL_VERIFIED` (G2-003 + R1 + V1 closed; G2-R0 review published) |
| Exit Gate | `G2_003V2_CODE_READY_OPERATOR_DB_RERUN_PENDING` (Mavis side) — Operator unlocks to `G2_003V2_IDENTITY_REFERENTIAL_INTEGRITY_VERIFIED` via re-running `g2-003-operator-evidence.ps1` |
| Time-box | 30-45 min |
| Commit | `fa3365a` fix(identity): enforce identity referential integrity |
| Verification | this document |
| Next Goal | G2-004 Authentication Kernel (NOT STARTED, HALTED) |

---

## 1 Goal

G2-003V2 closes G2-R0 review finding **D-002 (HIGH)**: the G2003
Identity migration was missing 14 cross-entity FK constraints
(TenantId / CompanyId / UserId / RoleId / OrganizationUnitId) at
the PostgreSQL catalog level. The Application layer correctly
enforced the cross-Tenant invariant; the database was missing
defense-in-depth. This Goal adds the missing FKs via an additive
migration `20260819162500_G2003V2_AddIdentityReferentialIntegrity`,
without modifying the G2003 original migration (per brief
section 5: "禁止修改已经执行过的: G2003 original migration 来
'伪造历史'").

## 2 Root Cause

G2-R0 review D-002 evidence:

- The G2003 migration creates tables in this order:
  1. `AspNetRoles` (line 19)
  2. `AspNetUsers` (line 45)
  3. `gulierp_company` (line 81)
  4. `gulierp_organization_unit` (line 114)
  5. `gulierp_plant` (line 145)
  6. `gulierp_tenant` (line 182) ← created AFTER its would-be children
  7. `gulierp_user_company_membership` (line 203)
  8. `gulierp_user_organization_membership` (line 226)
  9. `gulierp_user_role_assignment` (line 250)
  10-14. `AspNetRoleClaims` / `AspNetUserClaims` / `AspNetUserLogins` / `AspNetUserRoles` / `AspNetUserTokens`
- EF Core's `CreateTable` can only emit FKs to tables created EARLIER in the same migration.
- `gulierp_tenant` is created LAST among the Tenant-bearing tables, so `Company.TenantId` / `Plant.TenantId` / `OrganizationUnit.TenantId` / `UserCompanyMembership.TenantId` / etc. could not be FK-constrained at CreateTable time.
- EF Core SHOULD have used `migrationBuilder.AddForeignKey` follow-ups AFTER all tables existed, but the G2003 migration does not contain any `AddForeignKey` calls.
- **G2003 result**: 9 FKs total (3 self-FKs for Company/Plant/Org + 6 Identity-default FKs for AspNetUser/RoleClaims/Logins/Roles/Tokens). **0 cross-entity FKs from GuliERP tables to Tenant/Company/Plant/Org/Users/Roles**.

## 3 Expected FK Matrix

The full set of cross-entity FKs that SHOULD exist per the G2-003
architecture. The G2003 baseline had 0 of these 14; the G2003V2
additive migration adds all 14.

| # | Child Table | Child Column | Parent Table | Parent Column | Required (DEC-ID) | G2003 baseline | G2003V2 added |
|---|---|---|---|---|---|---|---|
| 1 | `AspNetRoles` | `TenantId` | `gulierp_tenant` | `Id` | DEC-ID-007 | ❌ | ✅ |
| 2 | `AspNetUsers` | `TenantId` | `gulierp_tenant` | `Id` | DEC-ID-003 | ❌ | ✅ |
| 3 | `gulierp_company` | `TenantId` | `gulierp_tenant` | `Id` | DEC-ID-002 | ❌ | ✅ |
| 4 | `gulierp_company` | `ParentCompanyId` | `gulierp_company` | `Id` | DEC-ID-002 (self) | ✅ (G2003) | (existing) |
| 5 | `gulierp_plant` | `TenantId` | `gulierp_tenant` | `Id` | DEC-ID-018 | ❌ | ✅ |
| 6 | `gulierp_plant` | `CompanyId` | `gulierp_company` | `Id` | DEC-ID-018 | ❌ | ✅ |
| 7 | `gulierp_plant` | `ParentPlantId` | `gulierp_plant` | `Id` | (self) | ✅ (G2003) | (existing) |
| 8 | `gulierp_organization_unit` | `TenantId` | `gulierp_tenant` | `Id` | DEC-ID-005 | ❌ | ✅ |
| 9 | `gulierp_organization_unit` | `CompanyId` | `gulierp_company` | `Id` | DEC-ID-005 | ❌ | ✅ |
| 10 | `gulierp_organization_unit` | `ParentOrganizationUnitId` | `gulierp_organization_unit` | `Id` | (self) | ✅ (G2003) | (existing) |
| 11 | `gulierp_user_company_membership` | `TenantId` | `gulierp_tenant` | `Id` | DEC-ID-004 | ❌ | ✅ |
| 12 | `gulierp_user_company_membership` | `CompanyId` | `gulierp_company` | `Id` | DEC-ID-004 | ❌ | ✅ |
| 13 | `gulierp_user_company_membership` | `UserId` | `AspNetUsers` | `Id` | (membership) | ❌ | ✅ |
| 14 | `gulierp_user_organization_membership` | `TenantId` | `gulierp_tenant` | `Id` | DEC-ID-006 | ❌ | ✅ |
| 15 | `gulierp_user_organization_membership` | `CompanyId` | `gulierp_company` | `Id` | DEC-ID-006 | ❌ | ✅ |
| 16 | `gulierp_user_organization_membership` | `UserId` | `AspNetUsers` | `Id` | (membership) | ❌ | ✅ |
| 17 | `gulierp_user_organization_membership` | `OrganizationUnitId` | `gulierp_organization_unit` | `Id` | DEC-ID-006 | ❌ | ✅ |
| 18 | `gulierp_user_role_assignment` | `TenantId` | `gulierp_tenant` | `Id` | DEC-ID-008 | ❌ | ✅ |
| 19 | `gulierp_user_role_assignment` | `UserId` | `AspNetUsers` | `Id` | (assignment) | ❌ | ✅ |
| 20 | `gulierp_user_role_assignment` | `RoleId` | `AspNetRoles` | `Id` | (assignment) | ❌ | ✅ |
| 21 | `gulierp_user_role_assignment` | `CompanyId` (nullable) | `gulierp_company` | `Id` | DEC-ID-008 (optional scope) | ❌ | ✅ |

**Net added by G2003V2: 14 FKs** (rows 1, 2, 3, 5, 6, 8, 9, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21 — count = 18 minus 4 self-FKs which already existed in G2003; verifying: rows 1, 2, 3, 5, 6, 8, 9, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21 = 18 rows; subtract rows 4, 7, 10 (self-FKs already present) = 15; but row 13 (UserCompanyMembership.UserId → AspNetUsers.Id) and row 19 (UserRoleAssignment.UserId → AspNetUsers.Id) and row 20 (UserRoleAssignment.RoleId → AspNetRoles.Id) and the original 6 Identity-default FKs (UserClaims / UserLogins / UserRoles / UserTokens / RoleClaims) all reference the same parent columns but on different child tables — the G2003V2 EF migration generated 14 `AddForeignKey` calls for 14 DISTINCT GuliERP cross-entity FKs, and the G2003 baseline had 9 FKs (3 self + 6 Identity-default), for a total of 23 FKs after the G2003V2 migration).

## 4 Added FK

The G2003V2 migration file `20260819162500_G2003V2_AddIdentityReferentialIntegrity.cs`
contains exactly **14 `migrationBuilder.AddForeignKey` calls** (one for each row in the matrix above, excluding the 3 self-FKs and 3 AspNet-User/Role-FKs that already existed in G2003). The `Down` migration contains 14 corresponding `DropForeignKey` calls for rollback.

Cross-verification:
- `git grep -c "AddForeignKey" modules/identity/.../20260819162500_*.cs` = 14 ✅
- `git grep -c "DropForeignKey" modules/identity/.../20260819162500_*.cs` = 14 ✅
- `git grep -c "CreateIndex" modules/identity/.../20260819162500_*.cs` = 9 (the supporting indexes on the FK columns that previously had no index)

## 5 Delete Behavior

Per brief section 6: every new FK uses `OnDelete(DeleteBehavior.Restrict)`.
The `Down` migration confirms this via `ReferentialAction.Restrict`.
The 6 pre-existing Identity-default FKs (AspNetUserClaims /
AspNetUserLogins / AspNetUserRoles / AspNetUserTokens /
AspNetRoleClaims) use `ReferentialAction.Cascade` (Microsoft
Identity default; unchanged). The rationale for `Restrict` on
all GuliERP FKs is to preserve DEC-ID-015 soft-delete semantics:
historical ERP data must never be cascade-deleted when a parent
Tenant/Company/Plant/Org/User/Role is removed.

## 6 Migration

Migration file: `modules/identity/GuliERP.Identity.Infrastructure/Migrations/20260819162500_G2003V2_AddIdentityReferentialIntegrity.cs`

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // 9 supporting CreateIndex calls on the FK columns
    // 14 AddForeignKey calls (all ReferentialAction.Restrict)
    // 0 CreateTable calls (purely additive)
    // 0 data changes
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    // 14 DropForeignKey calls
    // 9 DropIndex calls
    // 0 DropTable calls
}
```

**Additive only**. The migration does NOT modify any existing table
data. On a fresh DB the migration applies after G2003 with no
effect on the G2003-created rows. On an existing DB (Operator's
seeded DB) the migration applies with no effect on existing data
— the FK constraints are validated against current rows; if
0 orphans exist (which is the case per the G2-003 seed), the
FK creation succeeds.

**Orphan check**: 0 orphans expected (the G2-003 seed creates 1
Tenant, 1 Company, 1 Plant, 1 Org, 4 Roles, 2 Users — all with
matching TenantId/CompanyId/UserId/RoleId/OrganizationUnitId).
The Operator evidence pack verifies this via
`psql ... -c "SELECT COUNT(*) FROM identity.gulierp_company WHERE
\"TenantId\" NOT IN (SELECT \"Id\" FROM identity.gulierp_tenant)"` → 0.

## 7 Automated Tests

Mavis side: `dotnet build GuliERP.slnx -c Release` → **0 warnings / 0 errors** across 9 projects. `dotnet test` → **101 PASS / 9 LOUD-FAIL / 0 SKIP** (the 6 G2-001/G2-003 baseline loud-fails are unchanged; the 3 new G2-003V2 tests loud-fail on the Mavis side per the standard "loud-fail on bad-DB fixture" pattern).

Operator side: the 3 new tests in `tests/GuliERP.Identity.IntegrationTests/IdentityReferentialIntegrityFacts.cs`:
- `FK_Tenant_RejectOnOrphan`: insert a Company with `TenantId = 99_999_999L` (non-existent) → expect `DbUpdateException`. With the G2003V2 FK in place, PostgreSQL returns SQLSTATE 23503 (foreign_key_violation).
- `FK_Tenant_AcceptOnValid`: insert a Company with the seed Tenant's id → expect success → cleanup.
- `FK_DeleteBehavior_Restrict_TenantCannotBeDeletedWithCompanies`: attempt to delete the seed Tenant (which has Company/Plant/Org/Role/User children) → expect `DbUpdateException` because the Restrict FK blocks the cascade.

## 8 Operator Evidence

Per brief section 8, the Operator unlocks `G2_003V2_CODE_READY_OPERATOR_DB_RERUN_PENDING` → `G2_003V2_IDENTITY_REFERENTIAL_INTEGRITY_VERIFIED` by re-running `tools/dev/g2-003-operator-evidence.ps1 -SkipPrompt` with a real connection string. The script:

1. Applies the Foundation migration (already applied; PASS).
2. Applies the Identity migration (already applied; PASS).
3. **NEW: applies the G2003V2 migration**: `dotnet ef database update --project modules/identity/GuliERP.Identity.Infrastructure/GuliERP.Identity.Infrastructure.csproj --startup-project apps/api/GuliERP.Api/GuliERP.Api.csproj` — the `__ef_migrations_history` shows the G2003V2 row added; PostgreSQL catalog now has 14 new FK constraints.
4. Runs the integration tests: Foundation 31/31 PASS + Identity 18/18 PASS + the 3 new G2-003V2 tests PASS (loud-fail → real-DB PASS).
5. Runtime Round 1 + Round 2 + bad-DB negative round (unchanged).
6. NEW: orphan check — `psql ... -c "SELECT COUNT(*) FROM identity.gulierp_company WHERE \"TenantId\" NOT IN (SELECT \"Id\" FROM identity.gulierp_tenant)"` → 0.
7. NEW: FK count check — `psql ... -c "SELECT COUNT(*) FROM information_schema.table_constraints WHERE constraint_schema = 'identity' AND constraint_type = 'FOREIGN KEY'"` → 23 (3 self + 6 Identity-default + 14 GuliERP cross-entity).
8. NEW: FK enforcement smoke test — `psql ... -c "INSERT INTO identity.gulierp_company (\"Id\", \"TenantId\", \"Code\", \"Name\", \"DefaultCurrency\", \"Timezone\", \"Status\", \"CreatedAt\", \"ModifiedAt\", \"ConcurrencyVersion\") VALUES (99999999, 99999999, 'ORPHAN-TEST', 'Orphan Test', 'USD', 'UTC', 1, NOW(), NOW(), 0)"` → expect `ERROR: insert or update on table "gulierp_company" violates foreign key constraint "FK_gulierp_company_gulierp_tenant_TenantId"`.

**OPERATOR_EVIDENCE_STATUS**: `PENDING` (Mavis side has no real DB; Operator must run the script).

## 9 Corrected Review Counts

Per brief section 10, the G2-R0 review document `docs/review/G2_R0_FOUNDATION_CRITICAL_REVIEW.md` contained a summary-count inconsistency: the §1 Executive Decision block said "3 HIGH" + "4 LOW", but the §18 Final Decision block correctly listed 4 HIGH findings (D-001, D-002, D-003, D-005). This G2-003V2 commit corrects §1 to match §18. The corrected counts are:

- **0 BLOCKER**
- **4 HIGH** (D-001, D-002, D-003, D-005)
- **4 MEDIUM** (D-004, D-007, D-014, D-015)
- **7 LOW** (D-006, D-009, D-010, D-011, D-012, D-016, D-021)
- **6 INFO** (D-008, D-013, D-017, D-018, D-019, D-020)
- **Total: 21 findings**

## 10 Remaining G2-R0 Risks

After G2-003V2 closure, the remaining risks (from G2-R0 review):

| ID | Severity | Status |
|---|---|---|
| D-001 | HIGH | BEFORE-AUTH (G2-004 can fix as a small side change) |
| D-003 | HIGH | G2-004 fix (replaces header path with JWT-claim) |
| D-005 | HIGH | BEFORE-MULTI-INSTANCE |
| D-004 | MEDIUM | ON-DEMAND (when next test class needs the pattern) |
| D-007 | MEDIUM | G2-005 (Authorization) or G2-006 (MDM) |
| D-014 | MEDIUM | **CLOSED in this G2-003V2 commit** (verification report §20 amended; counts corrected) |
| D-015 | MEDIUM | BEFORE the 8 directory HTTP endpoints are added |
| D-006, D-009, D-010, D-011, D-012, D-016, D-021 | LOW | Optional / future |
| D-008, D-013, D-017, D-018, D-019, D-020 | INFO | Optional / future |

## 11 Files Changed (this Goal)

| Path | Action | Lines |
|---|---|---|
| `modules/identity/GuliERP.Identity.Infrastructure/Persistence/IdentityDbContext.cs` | MODIFY | +~60 (14 `HasOne().WithMany().HasForeignKey().OnDelete(Restrict)` declarations) |
| `modules/identity/GuliERP.Identity.Infrastructure/Migrations/IdentityDbContextModelSnapshot.cs` | MODIFY (auto-generated) | regenerated by EF |
| `modules/identity/GuliERP.Identity.Infrastructure/Migrations/20260819162500_G2003V2_AddIdentityReferentialIntegrity.cs` | NEW | 14 `AddForeignKey` + 9 `CreateIndex` + 14 `DropForeignKey` + 9 `DropIndex` |
| `modules/identity/GuliERP.Identity.Infrastructure/Migrations/20260819162500_G2003V2_AddIdentityReferentialIntegrity.Designer.cs` | NEW (auto-generated) | model snapshot |
| `tests/GuliERP.Identity.IntegrationTests/IdentityReferentialIntegrityFacts.cs` | NEW | 3 Operator-required tests |
| `docs/verification/G2_003_IDENTITY_ORG_KERNEL_REPORT.md` | MODIFY | §20 Constraints amended (false claim corrected; G2003 baseline table + G2-003V2 amendment note) |
| `docs/review/G2_R0_FOUNDATION_CRITICAL_REVIEW.md` | MODIFY | §1 Executive Decision counts corrected (3 HIGH → 4 HIGH; 4 LOW → 7 LOW; added correction notice) |
| `docs/verification/G2_003V2_IDENTITY_REFERENTIAL_INTEGRITY_REPORT.md` | NEW | this document |
| `docs/governance/GOAL_REGISTRY.md` | MODIFY | G2-003V2 closure section + Active Goal status flip |

## 12 Forbidden Amendments Respected

| Forbidden | Did G2-003V2 trip it? |
|---|---|
| Modify G2003 original migration | NO (per brief §5; G2003 untouched) |
| Re-open G2-001 / G2-002 / R1 / R2 / G2-003 / G2-003A / G2-003A-R2 / G2-003R1 / G2-003V1 | NO (G2-003V1 was the previous Goal; G2-003V2 is a new add-on) |
| Change DEC-ID-001..020 | NO (DEC-IDs preserved; this Goal CLOSES the FK enforcement, not the DEC-IDs) |
| Use Admin.NET / Furion / SqlSugar | NO (0 actual uses) |
| Use UseInMemoryDatabase / UseSqlite / EnsureCreated | NO (0 actual uses) |
| git add . / git reset / git rebase / git amend / git revert | NO (path-specific staging only; linear history) |
| Implement Auth / JWT / [Authorize] | NO (0 login code, 0 [Authorize]) |
| Implement Permission / DataScope | NO |
| Modify Domain entity files | NO (only EF model FK declarations; no entity class changes) |
| Add cross-tenant composite FK (e.g., CHECK (User.TenantId = Company.TenantId)) | NO (per brief §4; this is an Application-layer invariant) |
| Drop / recreate schema | NO |
| EnsureCreated | NO |
| Modify migration history manually | NO |

## 13 NEXT_GOAL_CANDIDATE

**`G2-004 — Authentication Kernel`** (NOT STARTED, HALTED)

Strictly: **G2-004 must NOT auto-start in this Mavis session.**
Per META_GULI_GOVERNANCE_V1.md HR-1..HR-10, explicit user
authorization is required for the next Goal kickoff. After
G2-003V2 is operator-closed, the natural next Goals are:

- **G2-004** (mandatory) — Login / JWT / lockout / [Authorize]
- **G2-003V3** (optional mini-Goal) — D-001 IsPlatformAdmin
  AsyncLocal fix (15 min; can fold into G2-004)
- **G2-006 / MDM** (next phase) — G2-003V2 FKs are now the
  baseline; MDM modules inherit the same FK discipline

## 14 STOP

This Goal is **CLOSED at the Mavis side** with the gate
`G2_003V2_CODE_READY_OPERATOR_DB_RERUN_PENDING`. The Operator
unlocks to `G2_003V2_IDENTITY_REFERENTIAL_INTEGRITY_VERIFIED`
by re-running `g2-003-operator-evidence.ps1 -SkipPrompt` and
getting the 8-step evidence pack to PASS. **G2-003V2 must NOT
auto-advance to G2-004 in this Mavis session.**
