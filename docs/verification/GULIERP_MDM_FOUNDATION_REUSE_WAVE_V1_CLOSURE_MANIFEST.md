# GULIERP_MDM_FOUNDATION_REUSE_WAVE_V1_CLOSURE_MANIFEST

**Purpose**: Local provenance + classification of every file in
the Reuse Wave delta (`GULIERP_MDM_FOUNDATION_REUSE_WAVE_V1`,
the Implementation Goal + the Schema-and-Operator Closure Goal).
Two boundary commits, **NO PUSH** — this is a local commit
exercise only. The next Goal `GULIERP_MDM_FOUNDATION_REUSE_WAVE_V1_PUSH`
is the only place that talks to `origin/master`.

**Date**: 2026-08-30
**Starting HEAD**: `e6e3dc8` (`docs(verification): close master-data foundation`)
**Starting dirty**: 73 (43 pre-existing WIP + 30 Reuse Wave)
**Pre-existing excluded count**: 43 (Login / Router / Vite / SalesOrder / Employee / Identity WIP)
**Reuse Wave candidate count**: 30 (12 modified production + 11 new untracked production/migration/test/tool + 3 reports + 4 throwaway excluded)

---

## Closure rule summary

| Decision bucket | Count | Disposition |
|-----------------|-------|-------------|
| COMMIT A — `feat(mdm): reuse foundation for warehouse location and item` | 22 files | Stage + commit |
| COMMIT B — `docs(verification): verify mdm foundation reuse wave` | 2 files | Stage + commit (R1 report + R2 report) |
| INTENTIONALLY_EXCLUDED — `tools/dev/Mdm006PgReconcile/` | 1 dir | Untracked throwaway, not in any commit |
| INTENTIONALLY_EXCLUDED — `tools/dev/MDM006_PG_RECONCILE.csx` | 1 file | Untracked throwaway, not in any commit |
| INTENTIONALLY_EXCLUDED — `tools/dev/tmp-query-migration-history.csx` | 1 file | Untracked throwaway probe, not in any commit |
| INTENTIONALLY_EXCLUDED — `apps/web/src/views/mdm/ItemList.vue` | 1 file | PRE_EXISTING_UI_WIP_COLLISION (23 ins / 15 del) — not a Reuse Wave change |
| INTENTIONALLY_EXCLUDED — `docs/verification/GULIERP_MDM_UI_AND_COMMON_FIELD_AUDIT_V1_REPORT.md` | 1 file | PRE_EXISTING untracked report (created 2026-08-28, before this Goal) — unrelated to Reuse Wave |
| INTENTIONALLY_EXCLUDED — 42 other unrelated dirty files | 42 | PRE_EXISTING_WIP preserved verbatim (Login / Router / Vite / SalesOrder / Employee / Identity / governance / etc.) |
| **RESIDUAL after Commit A + B** | 0 Reuse Wave files | **REUSE_WAVE_RESIDUAL_DIRTY = 0** |

---

## Manifest table

Path | Status | R1/R2 | Classification | RequiredForReuse | CommitDisposition | Evidence
---|---|---|---|---|---|---
`modules/mdm/GuliERP.Mdm.Application/IMdmCodeRuleBootstrapService.cs` | M | R1 | REUSE_OWNED | yes — Warehouse/Location/Item default rule interface | Commit A | +82 lines; 6 new method signatures (per-Company Warehouse, per-Warehouse Location, per-Tenant Item × 2 each = 6); Foundation engine interface UNCHANGED
`modules/mdm/GuliERP.Mdm.Application/MdmDtos.cs` | M | R1 | REUSE_OWNED | yes — ItemDto / CreateItemRequest / UpdateItemRequest add `MnemonicCode` field | Commit A | +24 lines; 3 records get new positional `MnemonicCode` parameter (last position, named-arg compatible with all existing callers)
`modules/mdm/GuliERP.Mdm.Domain/Entities/Item.cs` | M | R1 | REUSE_OWNED | yes — Item.MnemonicCode nullable property (40 char) | Commit A | +9 lines; `string? MnemonicCode` field with doc
`modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmMasterData002Services.cs` | M | R1 | REUSE_OWNED | yes — Warehouse + Location CreateAsync wire `_codeService.GenerateNextAsync` | Commit A | +95 lines; 2 new ctor overloads (test-only back-compat) + Warehouse/Location service-layer pre-check changes
`modules/mdm/GuliERP.Mdm.Infrastructure/Mdm/MdmService.cs` | M | R1 | REUSE_OWNED | yes — Item CreateAsync wire + MnemonicCode round-trip | Commit A | +56 lines; 1 new ctor overload + 1 GenerateNextAsync call + MnemonicCode search expansion
`modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/Configurations/ItemConfiguration.cs` | M | R1 | REUSE_OWNED | yes — HasIndex for MnemonicCode (non-unique, search) | Commit A | +12 lines; `b.HasIndex(i => i.MnemonicCode).HasDatabaseName("ix_gulierp_item_mnemoniccode")`
`modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmCodeRuleBootstrapService.cs` | M | R1 | REUSE_OWNED (PROFILE_EXTENSION) | yes — 6 new entity default rules + CreateRuleAndStateAsync shared helper | Commit A | +380 lines; 3 new entity × 2 scopes = 6 new methods + 1 shared private helper; writes to existing MasterDataCodeRule + MasterDataCodeSequenceState
`modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmCodeRuleBootstrapStartupService.cs` | M | R1 | REUSE_OWNED (PROFILE_EXTENSION) | yes — startup service now bootstraps Item + Warehouse + Location defaults (was BP-only) | Commit A | +59 lines; calls the 3 new "for all" methods at startup
`modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/20260829041719_MDM006_ItemMnemonicCode_Regen.cs` | A | R1+R2 | REUSE_MIGRATION | yes — REAL `AddColumn MnemonicCode` + `CreateIndex ix_gulierp_item_mnemoniccode` | Commit A | EF migration entry; semantically the canonical MDM006 (Final is a noop, marker is a noop)
`modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/20260829041719_MDM006_ItemMnemonicCode_Regen.Designer.cs` | A | R1 | REUSE_MIGRATION | yes — post-state model snapshot | Commit A | auto-generated by `dotnet ef migrations add`
`modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/20260829042008_MDM006_ItemMnemonicCode_Final.cs` | A | R1+R2 | REUSE_MIGRATION (NOOP) | yes — `__EFMigrationsHistory` chain alignment | Commit A | this Goal's hygiene: collapsed into noop (work is owned by Regen)
`modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/20260829042008_MDM006_ItemMnemonicCode_Final.Designer.cs` | A | R2 | REUSE_MIGRATION | yes — clone of Regen Designer, diff = 0 to match noop | Commit A | this Goal's hygiene
`modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/20260829121622_MDM006_ItemMnemonicCode.cs` | A | R1+R2 | REUSE_MIGRATION (NOOP MARKER) | yes — human-readable traceability of the Wave | Commit A | noop Up/Down; Designer model = post-state so diff with Final = 0
`modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/20260829121622_MDM006_ItemMnemonicCode.Designer.cs` | A | R1 | REUSE_MIGRATION | yes — post-state snapshot | Commit A | auto-generated
`modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/20260829044322_MDM007_LocationWarehouseScopedCodeUniqueness.cs` | A | R2 | REUSE_MIGRATION | yes — `DROP INDEX IF EXISTS` + `CREATE UNIQUE INDEX IF NOT EXISTS` (constraint swap) | Commit A | idempotent, fresh-DB-safe
`modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/20260829044322_MDM007_LocationWarehouseScopedCodeUniqueness.Designer.cs` | A | R2 | REUSE_MIGRATION | yes — auto-generated | Commit A |
`modules/mdm/GuliERP.Mdm.Infrastructure/Migrations/MdmDbContextModelSnapshot.cs` | M | R1+R2 | REUSE_MIGRATION | yes — snapshot tracks MDM006 (Item.MnemonicCode + index) + MDM007 (Location warehouse-scoped index) | Commit A | +11 lines; Item block + Location block updated
`modules/mdm/GuliERP.Mdm.Infrastructure/Persistence/Configurations/LocationConfiguration.cs` | M | R2 | REUSE_OWNED | yes — Location unique index now `(TenantId, CompanyId, WarehouseId, Code)` | Commit A | +20 lines; index swap + doc
`modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmMasterDataSeedService.cs` | M | R1 | REUSE_REQUIRED_DEPENDENCY (NOT format-only) | yes — `new CreateItemRequest(..., MnemonicCode: null)` adds the new positional parameter | Commit A | +1/-1; the single change adds the Reuse-Wave-introduced `MnemonicCode` parameter (not format-only)
`tests/GuliERP.Api.Tests/SnowflakeLongJsonConverterFacts.cs` | M | R1 | REUSE_REQUIRED_TEST_UPDATE | yes — `new ItemDto(..., MnemonicCode: null)` adds the new positional parameter | Commit A | +2/-1; the change is required for `ItemDto` to compile with the Reuse-Wave-added field
`tests/GuliERP.Mdm.Tests/MdmDtosTests.cs` | M | R1 | REUSE_TEST | yes — `ItemDto_Exposes_Tenant_Scoped_Fields` test asserts new `MnemonicCode` round-trip | Commit A | +3/-1; `Assert.Equal("STEEL-PLATE", dto.MnemonicCode);` added
`tests/GuliERP.Mdm.Tests/MdmReuseWaveFacts.cs` | A | R1+R2 | REUSE_TEST | yes — 16 + 2 = 18 focused reuse + location persistence tests | Commit A | the 16 Reuse Wave tests + 2 Location persistence tests added in R2
`tests/GuliERP.Mdm.IntegrationTests/MdmItemCategoryAndItemFacts.cs` | M | R1 | REUSE_REQUIRED_TEST_UPDATE | yes — 3 callers of `new CreateItemRequest(...)` add the new `MnemonicCode: null` parameter | Commit A | +12 lines; required to compile
`tests/GuliERP.Mdm.IntegrationTests/MdmBusinessPartnerWarehouseLocationFacts.cs` | M | R1+R2 | REUSE_TEST | yes — 2 new PG persistence tests (same-warehouse dup + cross-warehouse same-Code) | Commit A | +136 lines; 2 new [Fact] tests
`tools/dev/gulierp-mdm007-location-warehouse-precheck.ps1` | A | R2 | REUSE_OPERATOR_TOOL | yes — Operator precheck script (per brief §七) | Commit A | +131 lines; psql-only, no hardcoded credential, reads -ConnectionString from caller
`docs/verification/GULIERP_MDM_FOUNDATION_REUSE_WAVE_V1_REPORT.md` | A | R1 | REUSE_REPORT | yes — R1 40-item implementation report | Commit B | 24.5 KB
`docs/verification/GULIERP_MDM_FOUNDATION_REUSE_WAVE_V1_SCHEMA_AND_OPERATOR_CLOSURE_REPORT.md` | A | R2 | REUSE_REPORT | yes — R2 40-item closure report | Commit B | 22.8 KB
`docs/verification/GULIERP_MDM_FOUNDATION_REUSE_WAVE_V1_CLOSURE_MANIFEST.md` | A | R2 | REUSE_REPORT (MANIFEST) | yes — this file (boundary provenance) | Commit B | this file
`apps/web/src/views/mdm/ItemList.vue` | M | n/a | **PRE_EXISTING_UI_WIP_COLLISION** | n/a | **EXCLUDE** | 23 ins / 15 del CSS refactor; NOT a Reuse Wave change; preserved verbatim per brief §八
`docs/verification/GULIERP_MDM_UI_AND_COMMON_FIELD_AUDIT_V1_REPORT.md` | A | n/a | **PRE_EXISTING** (created 2026-08-28, before this Goal) | n/a | **EXCLUDE** | unrelated audit report, not produced by Reuse Wave
`tools/dev/Mdm006PgReconcile/` (dir) | A | R2 | **THROWAWAY_OPERATOR_TOOL** | n/a | **EXCLUDE** (untracked, never staged) | R2 once-off DB reconcile program; not shipping; contains hardcoded `<REDACTED_DB_PASSWORD>` — disposing outside the repo would have been preferred but Windows safety policy blocked `mavis-trash`; since untracked, the file simply does not enter the commit
`tools/dev/MDM006_PG_RECONCILE.csx` | A | R2 | **THROWAWAY_OPERATOR_TOOL** | n/a | **EXCLUDE** (untracked, never staged) | R2 dotnet-script variant of the above; same content family
`tools/dev/tmp-query-migration-history.csx` | A | R2 | **TEMP_DEBUG** | n/a | **EXCLUDE** (untracked, never staged) | one-off probe script
`tools/dev/.quarantine/` (dir) | n/a | **PRE_EXISTING** | n/a | **EXCLUDE** | Foundation / G3 Goals' commit artifacts; not Reuse Wave
`tools/dev/_*.ps1` etc. | M | n/a | **PRE_EXISTING_WIP** (g3 / Foundation) | n/a | **EXCLUDE** | unrelated previous-Goal work
`apps/web/src/router.ts`, `Login.vue`, `SalesOrderList.vue`, `vite.config.ts` | M | n/a | **PRE_EXISTING_WIP** | n/a | **EXCLUDE** | unrelated
`docs/governance/GOAL_REGISTRY.md` | M | n/a | **PRE_EXISTING_WIP** | n/a | **EXCLUDE** | unrelated governance
`docs/verification/GULIERP_EMPLOYEE_CODE_CONTRACT_RECONCILIATION_001_REPORT.md` | M | n/a | **PRE_EXISTING_WIP** | n/a | **EXCLUDE** | unrelated
`tests/GuliERP.Identity.IntegrationTests/EnterpriseRolePackCrossTenantFacts.cs` | M | n/a | **PRE_EXISTING_WIP** | n/a | **EXCLUDE** | unrelated Identity test
`tests/GuliERP.Identity.Bootstrap.Tests/EnterpriseBootstrapAndOrganizationTreeFacts.cs` | M | n/a | **PRE_EXISTING_WIP** | n/a | **EXCLUDE** | unrelated Identity test
`tools/dev/check-runtime.ps1`, `start-stack.ps1`, `stop-stack.ps1` | M | n/a | **PRE_EXISTING_WIP** | n/a | **EXCLUDE** | unrelated tooling

---

## Sensitive scan (Commit A candidates only)

Scanned: `git diff` for all 22 Commit A candidate files.

| Pattern checked | Match? |
|----------------|--------|
| real DB password | **NO** |
| complete connection string | **NO** |
| cookies | **NO** |
| CSRF token value | **NO** |
| Bearer token | **NO** |
| Operator credentials (Operator 5000) | **NO** |

The ONLY credential reference in Commit A is in the Operator
precheck script's `-ConnectionString` parameter, which is read
from the caller (no hardcoded value). All throwaway tools that
contained a hardcoded test-DB password (see `<REDACTED_DB_PASSWORD>`
in the throwaway row above) are EXCLUDED (untracked, not staged).

`REUSE_WAVE_SENSITIVE_SCAN = PASS`

---

## Foundation Core modified count

**0**.

`MasterDataCodeService.cs` — UNCHANGED
`MasterDataCodeRule.cs` — UNCHANGED
`MasterDataCodeSequenceState.cs` — UNCHANGED
`Country.cs` — UNCHANGED
`AdministrativeRegion.cs` — UNCHANGED
`IMdmReferenceDataService.cs` — UNCHANGED
`MdmReferenceDataService.cs` — UNCHANGED
`BusinessPartner.cs` (and Foundation) — UNCHANGED
`reference-data.ts` (frontend) — UNCHANGED
Existing Cascader infrastructure — UNCHANGED

## Foundation Profile Extension count

**1** — `MdmCodeRuleBootstrapService.cs` + its interface
`IMdmCodeRuleBootstrapService.cs`. The 6 new methods +
`CreateRuleAndStateAsync` shared helper are profile-level
additions that write to the existing `MasterDataCodeRule` +
`MasterDataCodeSequenceState` tables. No engine change.

## Unknown provenance count

**0**.

Every Commit A candidate has explicit evidence in the manifest
table. The 3 EXCLUDED throwaway tools are explicitly
`THROWAWAY_OPERATOR_TOOL` / `TEMP_DEBUG` (not UNKNOWN). The
`GULIERP_MDM_UI_AND_COMMON_FIELD_AUDIT_V1_REPORT.md` is
`PRE_EXISTING` (created 2026-08-28 before this Goal — verified
via `LastWriteTime`).

## Migrations committed

| File | Status | Effect |
|------|--------|--------|
| `20260829041719_MDM006_ItemMnemonicCode_Regen.cs` | **REAL** (was applied to test PG during R2) | `AddColumn MnemonicCode` + `CreateIndex ix_gulierp_item_mnemoniccode` |
| `20260829042008_MDM006_ItemMnemonicCode_Final.cs` | **NOOP** (this Goal's hygiene) | nothing — index already added by Regen |
| `20260829121622_MDM006_ItemMnemonicCode.cs` | **NOOP MARKER** | nothing — human-readable Wave traceability |
| `20260829044322_MDM007_LocationWarehouseScopedCodeUniqueness.cs` | **REAL** (pending for production deploy) | `DROP INDEX IF EXISTS ux_gulierp_location_tenant_company_code` + `CREATE UNIQUE INDEX IF NOT EXISTS ux_gulierp_location_tenant_company_warehouse_code` |

Total: 4 migration entries, 8 files (4 .cs + 4 .Designer.cs).
Migration history is `IMMUTABLE_ONCE_APPLIED` (per brief §十六);
the Final noop and marker noop are kept to honor
`__EFMigrationsHistory` chain integrity.

---

## Unknown + PRE_EXISTING_RAW artifacts check

| Check | Result |
|-------|--------|
| throwaway secrets removed | **3 untracked files (not in commit)**; safety policy blocked `mavis-trash`, so they remain on disk but are EXCLUDED from any commit |
| hardcoded credentials remaining in working tree | **YES** — inside the 3 throwaway tools (Mdm006PgReconcile/Program.cs, MDM006_PG_RECONCILE.csx, tmp-query-migration-history.csx) — but ALL 3 are untracked and never staged |
| ItemList.vue staged? | **NO** — pre-existing WIP, EXCLUDED |
| SnowflakeLongJsonConverterFacts unrelated diff in commit? | **NO** — the only diff is the Reuse-Wave-required `MnemonicCode: null` constructor argument |
| raw artifacts (runtime logs / cookies / PG dumps / screenshots) | **NONE** in Commit A or B |
| sensitive scan | **PASS** |
| `GULIERP_MDM_UI_AND_COMMON_FIELD_AUDIT_V1_REPORT.md` provenance | `LastWriteTime=2026-08-28 16:49:34`, created BEFORE this Goal — pre-existing, EXCLUDED |

---

## REUSE_WAVE_RESIDUAL after Commit A + B

After staging Commit A (22 files) + Commit B (3 files including
this manifest), the Reuse Wave's residual dirty count is
**0**.

Items remaining dirty after Commit A + B (counted 73 → 73 - 25 = 48
remaining) are ALL pre-existing unrelated WIP. None of them is a
Reuse Wave candidate.
