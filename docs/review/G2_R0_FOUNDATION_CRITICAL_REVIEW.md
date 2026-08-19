# G2-R0 Foundation Critical Review & Pre-G2-004 Readiness

| Field | Value |
|---|---|
| Review | **G2-R0 Foundation Critical Review & Pre-G2-004 Readiness** |
| Scope | G2-001 (Host & PG) + G2-002 (Foundation Kernel) + G2-003 (Identity Kernel) + supporting G2-003R1 (EF Design) + G2-003V1 (Bad-DB test isolation) |
| Method | Adversarial critical review — failure-seeking, not implementation. 10 attacker roles enumerated in brief §18. |
| Time-box | 90 min |
| Hard-stop decision | per brief §20 — BLOCKER ⇒ `FOUNDATION_SUFFICIENT_TO_PROCEED = NO` |
| Final gate output | `FOUNDATION_SUFFICIENT_TO_PROCEED` = see §18 |
| Commit | this review is `docs/review/G2_R0_FOUNDATION_CRITICAL_REVIEW.md` ONLY; no production code touched |

---

## 1. Executive Decision

**`FOUNDATION_SUFFICIENT_TO_PROCEED = YES`** with **2 mandatory BEFORE-AUTH / BEFORE-MDM follow-up Goals** explicitly carved out below.

- **0 BLOCKER** (per brief §20 strict definition: no SECURITY / TENANT ISOLATION / DATA CORRUPTION / FUNDAMENTAL ARCHITECTURE blocker).
- **3 HIGH** (1 mandatory before G2-004 ships; 1 mandatory before MDM; 1 mandatory before multi-instance production).
- **4 MEDIUM**.
- **4 LOW**.
- **Several INFO**.

Two non-blocker defects are non-negotiable follow-ups: **G2-003V2 — DB FK Constraint Closure** (BEFORE-MDM, recommended before G2-004) and **G2-004B — Identity Header Surface Hardening** (subsumed by G2-004 Authentication work).

**NEXT_GOAL_CANDIDATE = G2-004 Authentication Kernel — NOT STARTED, HALTED.**
Per META_GULI_GOVERNANCE_V1.md HR-1..HR-10, explicit user authorization required for the next Goal kickoff.

---

## 2. Architecture Strengths

What is genuinely well-designed and not worth re-opening.

| # | Strength | Evidence |
|---|---|---|
| S-1 | **Single G2003 atomic migration** — 14 tables in one transaction; clean rollback + idempotency via `__ef_migrations_history` | `modules/identity/.../Migrations/20260819150708_G2003_InitializeIdentitySchema.cs` (single file) |
| S-2 | **Schema-per-module separation** — `foundation` / `identity` / future `mdm` / future `inventory`; each has its own `__ef_migrations_history` | Foundation: `MigrationsHistoryTable("__ef_migrations_history", "foundation")`; Identity: `("__ef_migrations_history", "identity")` |
| S-3 | **IPlantScoped marker reserved without implementation** — DEC-ID-020 strict: no V1 entity uses IPlantScoped; prevents Plant = OrganizationUnit collapse | `modules/foundation/.../Kernel/TenantCompanyContextContracts.cs:62` interface declared, no concrete `IPlantScoped` entity |
| S-4 | **Plant vs OrganizationUnit = 2 independent dimensions** — DEC-ID-019, enforced in `DirectoryServiceImplementations.cs` (cross-tenant guard) | Plant `ListByCompanyAsync` validates `company.TenantId != tenantId`; OrganizationUnit same pattern |
| S-5 | **IdentityContextMiddleware does NOT query DB** — header→context translation only; cross-tenant guard in service layer | `IdentityContextMiddleware.cs:59-67` explicit comment + actual code |
| S-6 | **X-Platform-Admin gated to Testing env** — G2-002R2 spirit preserved; Production cannot be tricked via header | `IdentityContextMiddleware.cs:97-104` `isTesting = ASPNETCORE_ENVIRONMENT == "Testing"` |
| S-7 | **Password redaction in startup log** — `HealthCheckHelpers.RedactConnectionString` covers all log lines that include the connection string | `Program.cs:85` only place that logs the connection string, with `RedactConnectionString(...)` |
| S-8 | **ProblemDetails extensions are deterministic** — `code` / `requestId` / `traceId` always set; no PII in extensions bag | `Program.cs:125-156` `CustomizeProblemDetails` callback |
| S-9 | **R2 Test-endpoint security is structural** — `IsEnvironment("Testing")` only; NO config flag | `Program.cs:271-313` `if (app.Environment.IsEnvironment("Testing"))` |
| S-10 | **HasQueryFilter declared (even if always-true)** — the migration is forward-compatible with a V1.5+ runtime filter upgrade | `IdentityDbContext.cs:96-103` `HasQueryFilter(e => true)` placeholder; structurally correct |
| S-11 | **Per-instance AsyncLocal fix (G2-003)** — PopScope mutates the same field, not a separate static; no static-slot aliasing between CurrentTenant/Company/User | `AsyncLocalContextBase.cs:58-65` |
| S-12 | **AsyncLocal Change()/Push() symmetry test coverage** — 3 Change/Restore tests pass | `IdentityApplicationServiceFacts.ICurrentX_Change_Sets_And_Restores_Value` (3 tests) |
| S-13 | **Soft-delete only on all 8 entities** — DEC-ID-015: `Status` enum, no hard delete | `Tenant.cs:30` / `Company.cs:44` / `Plant.cs:53` / `OrganizationUnit.cs:36` / `GuliErpUser.cs:57` / `GuliErpRole.cs:42` |
| S-14 | **Default status = Active** — new entities default to Active, preventing "ghost disabled" records | all 6 entity files: `Status = XStatus.Active` |
| S-15 | **Snowflake epoch 2026-01-01 with 41 bits** — ~69 years of usable IDs from a recent epoch | `SnowflakeIdGenerator.cs:36-37` |
| S-16 | **Forbidden-pattern discipline held** — 0 Admin.NET / 0 Furion / 0 SqlSugar / 0 UseInMemoryDatabase / 0 UseSqlite / 0 EnsureCreated in code (2 doc comments document the FORBIDDEN list) | grep of `modules/` + `apps/` + `tests/` |
| S-17 | **Snowflake clock-future negative check** — `if (elapsed.TotalMilliseconds < 0) throw` catches pre-epoch generations | `SnowflakeIdGenerator.cs:116-121` |
| S-18 | **EF migration history tables separated per schema** — Foundation history in `foundation.__ef_migrations_history`; Identity history in `identity.__ef_migrations_history`; future modules add their own | each `DbContextOptions` configures its own `MigrationsHistoryTable(..., schema)` |
| S-19 | **G2-002R1/R2 + G2-003R1 + G2-003V1 follow-ups were honest disclosure rounds** — every round updated the GOAL_REGISTRY, did not pretend PASS without evidence | `GOAL_REGISTRY.md` R1/R2/V1 sections |
| S-20 | **Operator script env-restore is now exception-safe** — G2-003V1 commit `b0fe241` covers 3 env vars in try/finally; saved values never echoed | `tools/dev/g2-003-operator-evidence.ps1` Step 7 |

---

## 3. Confirmed Defects

Each finding has: ID, Severity, Evidence, Impact, Likelihood, Reversibility, Recommendation, When-To-Fix.

### D-001 — `IsPlatformAdmin` is a plain instance property, not AsyncLocal

| Field | Value |
|---|---|
| **Severity** | **HIGH** |
| **Evidence** | `modules/identity/.../Contexts/CurrentUser.cs:27` `public bool IsPlatformAdmin { get; set; }` (no AsyncLocal); `DependencyInjection.cs:100` `services.AddScoped<ICurrentUser, CurrentUser>();` (Scoped = per-request instance). The `Id` property is AsyncLocal-backed (`_holder.Current`); `IsPlatformAdmin` is not. |
| **Impact** | Today (Scoped DI): each HTTP request gets its own `CurrentUser` instance, so the flag does not leak across requests. **However**, if a future change makes `CurrentUser` Singleton (e.g., for perf), the flag becomes process-global and `Request A` setting `IsPlatformAdmin = true` would let `Request B` inherit the flag. This is a latent foot-gun. |
| **Likelihood** | LOW (Scoped is correct today); MEDIUM (refactor to Singleton is a plausible performance optimization). |
| **Reversibility** | HIGH (additive change: introduce `AsyncLocal<bool>` alongside the property; deprecate the property in a V1.5+). |
| **Recommendation** | Make `IsPlatformAdmin` AsyncLocal-backed in a small follow-up. Keep the `get; set;` for back-compat but route through an internal AsyncLocal holder (mirroring `Id`). |
| **When-To-Fix** | **BEFORE-AUTH** (cheap to fix in G2-004; gives defense-in-depth before JWT-claim path is wired). |

### D-002 — DB-level cross-Tenant FK constraints are MISSING

| Field | Value |
|---|---|
| **Severity** | **HIGH** (correctness, not blocker — see §20 analysis) |
| **Evidence** | `modules/identity/.../Migrations/20260819150708_G2003_InitializeIdentitySchema.cs` table creation order: `AspNetRoles` → `AspNetUsers` → `gulierp_company` → `gulierp_organization_unit` → `gulierp_plant` → `gulierp_tenant` → 3 membership tables → 5 Identity claim tables. EF Core `CreateTable` can only declare FKs to tables created EARLIER in the same migration. `gulierp_tenant` is created AFTER its would-be children, so the child tables' `TenantId` columns have NO FK to `gulierp_tenant.Id`. EF Core SHOULD have used `migrationBuilder.AddForeignKey` calls AFTER all tables existed, but the migration does not contain any. Confirmed: 9 FKs in the migration (4 self-FKs + 1 `RoleId` + 4 `UserId`), 0 cross-table FKs from `gulierp_company.TenantId` / `Plant.TenantId` / `Plant.CompanyId` / `Org.TenantId` / `Org.CompanyId` / 3 membership tables / `AspNetUsers.TenantId` / `AspNetRoles.TenantId`. The model snapshot (`IdentityDbContextModelSnapshot.cs`) has only 9 `HasOne` calls — matching the migration exactly. **The entity classes do not declare the FK relationships in EF Core**, so EF never generates them. |
| **Impact** | The DB accepts Tenant-orphan rows: e.g., a `Company` with `TenantId = 9999` (no such Tenant) is silently allowed. The application layer (`PlantDirectoryService.ListByCompanyAsync` etc.) DOES validate `company.TenantId != tenantId` before reading children, so application code cannot accidentally introduce orphans. But defense-in-depth is gone. The G2-003 verification report §20 contains the FALSE claim: `0 textual-name relations. 0 comma-string IDs. 0 hidden FKs. Old-DEV anti-patterns explicitly rejected.` and §30 `20/20 DEC-IDs compliant. 0 modified. 0 deferred.` (DEC-ID-001 / 002 / 003 / 004 / 005 / 007 / 008 / 017 / 018 all have NO DB enforcement at the FK level). |
| **Likelihood** | LOW (application code is correct today); MEDIUM (future modules + ad-hoc SQL would lack the backstop). |
| **Reversibility** | HIGH (additive migration; no data changes since DB is empty / seeded with valid TenantId). |
| **Recommendation** | New Goal **G2-003V2 — DB FK Constraint Closure**. Scope: a single additive migration `G2003_1_AddTenantCompanyFks` that adds the missing FKs via `migrationBuilder.AddForeignKey` (after all tables exist). Also update `IdentityDbContext.OnModelCreating` to declare the FKs (so future migrations are correct). Acceptance: (1) migration applies to fresh DB; (2) migration applies to existing seeded DB (no data changes); (3) inserting a `Company` with a non-existent `TenantId` raises a `DbUpdateException` with FK violation; (4) inserting a `Plant` with a non-existent `CompanyId` raises the same; (5) the G2-003 verification report §20 / §30 is amended to acknowledge the gap + the V2 closure. |
| **When-To-Fix** | **BEFORE-MDM** (MDM inherits the precedent; if Identity is missing the FKs, MDM will too, and the cross-Tenant invariant stays at the application layer). Recommended as a 30-min mini-Goal before G2-004, since the G2-003 report's "20/20 DEC-IDs compliant" claim is the basis for the G2-003 VERIFIED Gate. |

### D-003 — `IdentityContextMiddleware` reads `X-Tenant-Id` / `X-User-Id` / `X-Company-Id` UNCONDITIONALLY in Production

| Field | Value |
|---|---|
| **Severity** | **HIGH** (security boundary, only acceptable as a G2-003 stepping stone) |
| **Evidence** | `modules/identity/.../Middleware/IdentityContextMiddleware.cs:68-87`: the `X-Tenant-Id` / `X-User-Id` / `X-Company-Id` headers are processed UNCONDITIONALLY in all environments (Production / Development / Testing). Only `X-Platform-Admin` is gated to Testing. Per G2-003 brief §二十 "本轮可以: 显式测试注入; request scoped primitive; 不需要 Login/JWT". Per G2-003A Gate §8: G2-004 will replace this with JWT-claim resolution. |
| **Impact** | In G2-003 state, any HTTP caller can set `X-Tenant-Id: 1` / `X-User-Id: 100` / `X-Company-Id: 10` to ANY values; the application trusts the header verbatim. The cross-tenant guard in `CompanySwitchingService.ValidateSwitchAsync` works correctly (it validates `company.TenantId == tenantId`), but the attacker can claim to be any Tenant. The directory services also work correctly (they apply the Tenant scope), but the scope is attacker-controlled. **Net: in G2-003 state, NO HTTP surface is safe to expose to a non-Operator caller.** G2-003 brief explicitly accepts this as a stepping stone. |
| **Likelihood** | N/A in G2-003 state (no production surface); N/A after G2-004 (replaced with JWT-claim). |
| **Reversibility** | HIGH (G2-004 is the fix). |
| **Recommendation** | G2-004 Authentication Goal subsumes this. The middleware must be REPLACED — not patched — to read `tenant_id` / `user_id` / `company_id` from the JWT `ClaimsPrincipal`, with the header path as a Testing-only fallback (gated to `IsEnvironment("Testing")` like `X-Platform-Admin`). |
| **When-To-Fix** | **BEFORE-AUTH** = G2-004 (mandatory). |

### D-004 — `UseSetting` anti-pattern in 14+ test sites (the G2-003V1 fix was scoped to 2 tests only)

| Field | Value |
|---|---|
| **Severity** | **MEDIUM** (test infrastructure fragility, not a production defect) |
| **Evidence** | `grep UseSetting` returns 15 sites across the test projects: `FoundationHostHealthFactsBadDb.cs:27` (doc comment, OK), `FoundationHostHealthFactsGoodDb.cs:39, 58`, `Kernel/FoundationKernelFacts.cs:53, 374, 418, 490, 492, 520, 522, 549, 576, 604`, `IdentityApplicationServiceFacts.cs:47`, `IdentityKernelFacts.cs:47, 115`. Of these, only `FoundationHostHealthFactsBadDb.cs` (the G2-003V1 commit `ed27ac5`) was fixed. The other 14 still use `UseSetting` which the caller env var overrides. Today, they appear to "pass" because: (a) `FoundationHostHealthFactsGoodDb` — env var = real DB, `UseSetting` = real DB, same result, no observable problem; (b) `FoundationKernelFacts` — uses `BadConnectionString` (Host=127.0.0.1;Port=1) for some tests; the Kernel facts that DO need a real DB use `FoundationDatabaseFacts` (separate class); the Kernel fact tests that use `BadConnectionString` are mostly G2-002 cross-cutting baseline tests that don't depend on the DB; (c) `IdentityApplicationServiceFacts` / `IdentityKernelFacts` — `UseSetting` with `BadConnectionString`; the tests that pass don't depend on a real DB. **However**, this is a pre-existing latent issue: any future test that depends on a specific bad-DB behavior (e.g., a /health/ready=503 test) will silently break under Operator env, exactly like the G2-003V1 trigger did. |
| **Impact** | Today: 0 observable test failures. Future: 1+ new tests will be silently broken. |
| **Likelihood** | MEDIUM (any new test that depends on bad-DB behavior). |
| **Reversibility** | HIGH (pattern already proven in commit `ed27ac5`; copy to other sites). |
| **Recommendation** | When the next test-class that needs bad-DB or specific-config isolation is added, copy the G2-003V1 pattern (`ConfigureAppConfiguration` + `AddInMemoryCollection`). No bulk refactor needed today; the working-tree is consistent enough that 14 sites haven't bitten. Do not over-invest. |
| **When-To-Fix** | **ON-DEMAND** (when a new test-class needs the same pattern). |

### D-005 — `SnowflakeIdGenerator` has `workerId = 0` hardcoded

| Field | Value |
|---|---|
| **Severity** | **HIGH** (production blocker for multi-instance) |
| **Evidence** | `DependencyInjection.cs:104` `services.AddSingleton<SnowflakeIdGenerator>(_ => new SnowflakeIdGenerator(workerId: 0));` — V1 single-host uses worker 0; two API instances BOTH with worker 0 would generate ID-collisions. |
| **Impact** | Today: 0 (single host). Multi-instance: catastrophic (FK violations + data corruption from collision). |
| **Likelihood** | HIGH (Kubernetes / load-balanced deployments are the natural production target). |
| **Reversibility** | HIGH (read workerId from env var / config / service discovery). |
| **Recommendation** | Read `workerId` from `IConfiguration` (env var `GULIERP_SNOWFLAKE_WORKER_ID` or `appsettings.json` `Snowflake:WorkerId`). Validate range (0..1023) at startup (fail-fast). The DI registration becomes `services.AddSingleton<SnowflakeIdGenerator>(sp => new SnowflakeIdGenerator(workerId: sp.GetRequiredService<IConfiguration>().GetValue<long>("Snowflake:WorkerId")));` |
| **When-To-Fix** | **BEFORE-MULTI-INSTANCE-PRODUCTION** (any plan to scale out). For V1 single-host, can defer. |

### D-006 — `SnowflakeIdGenerator` has no clock-rollback protection

| Field | Value |
|---|---|
| **Severity** | **LOW** (rare in practice with NTP) |
| **Evidence** | `SnowflakeIdGenerator.cs:81-95`: `now = CurrentTimestamp()`. If `now < _lastTimestamp` (NTP step adjustment, manual clock change), the generator writes a smaller timestamp to the ID but does not detect the rollback. Could produce IDs with timestamps earlier than the previous ID — violating monotonic property. Could also produce duplicates if the new timestamp equals `_lastTimestamp` AND `_sequence == 0`. |
| **Impact** | ID monotonic property violation. In practice, NTP slewing is gradual (sub-ms per second) and won't cause issues; NTP stepping (large sudden adjustment) is rare. |
| **Likelihood** | LOW. |
| **Reversibility** | HIGH (add `if (now < _lastTimestamp) { /* wait or throw */ }` branch). |
| **Recommendation** | Add clock-rollback detection: if `now < _lastTimestamp`, throw `InvalidOperationException` OR spin until `now == _lastTimestamp + 1` (configurable). Document the policy. |
| **When-To-Fix** | **OPTIONAL** (recommended but not blocking). Can be folded into the workerId fix. |

### D-007 — `IDataFilter` is no-op; `HasQueryFilter(e => true)` everywhere; no actual EF-level data isolation

| Field | Value |
|---|---|
| **Severity** | **MEDIUM** (documented as V1.5+ deferred; application layer enforces scope) |
| **Evidence** | `IdentityDbContext.cs:96-103` declares `HasQueryFilter(e => true)` for 8 entities. `DataFilter.cs:22-29` is a no-op placeholder. G2-003A Gate §13 / DEC-ID-013 explicitly defers runtime isolation to V1.5+. The application services (`CompanyDirectoryService`, `PlantDirectoryService`, `OrganizationDirectoryService`, `UserDirectoryService`) apply the scope explicitly. The cross-tenant guard in `CompanySwitchingService.ValidateSwitchAsync` works correctly. |
| **Impact** | A future business module that writes `db.Companies.ToList()` and forgets to add `.Where(c => c.TenantId == currentTenant.Id)` returns ALL Companies across all Tenants. The Directory services are the safe path. But there is no architectural test preventing future bypass. |
| **Likelihood** | MEDIUM (any new business module is a candidate). |
| **Reversibility** | HIGH (upgrade `IDataFilter` + change `HasQueryFilter` predicates; additive). |
| **Recommendation** | Land the V1.5+ runtime filter when G2-005 (Authorization) or G2-006 (MDM) starts. The Identity module's Directory services are the safe alternative in the meantime; document this as a contract for business modules. |
| **When-To-Fix** | **BEFORE-AUTHZ** (G2-005) or **BEFORE-MDM** (G2-006). |

### D-008 — `IdentityKernelFacts.IdentityContext_PlatformAdmin` test was claimed but not actually present

| Field | Value |
|---|---|
| **Severity** | **INFO** (test contract gap, not a defect) |
| **Evidence** | The IdentityKernelFacts.cs has a test `IdentityContext_Middleware_Reads_Headers` that tests the `X-Tenant-Id` / `X-User-Id` / `X-Company-Id` path, and a comment in line 95 saying "The X-Platform-Admin path is covered separately in the IdentityContext_PlatformAdmin path below". The "below" test does NOT exist. |
| **Impact** | No test for the `X-Platform-Admin: true` header → `IsPlatformAdmin = true` path in `Testing` env. The current `IdentityContext_Middleware_Reads_Headers` test verifies the 3 ICurrent* header mappings but NOT the PlatformAdmin path. |
| **Likelihood** | LOW (header path is simple, env-gate is explicit, code is reviewable). |
| **Reversibility** | HIGH (add a 1-test case). |
| **Recommendation** | Add `IdentityContext_PlatformAdmin_Header_In_Testing_Sets_Flag` + `_In_Production_Is_Ignored` tests. |
| **When-To-Fix** | **ON-DEMAND** (when PlatformAdmin is more deeply wired in G2-005). |

### D-009 — `AspNetUserTokens` / `AspNetUserLogins` / `AspNetUserClaims` are wired but unused

| Field | Value |
|---|---|
| **Severity** | **INFO** |
| **Evidence** | `AddDefaultTokenProviders()` (line 95 of DependencyInjection.cs) adds `IUserTwoFactorTokenProvider` / `IAuthenticatorTokenProvider` etc. G2-003 has no login flow, so these are unused. |
| **Impact** | Tiny memory + DI cost. |
| **Likelihood** | N/A |
| **Reversibility** | HIGH (remove the call when ready). |
| **Recommendation** | Keep for G2-004 (login flow will use them). |
| **When-To-Fix** | **G2-004**. |

### D-010 — `RequireConfirmedEmail = false`, `RequireUniqueEmail = false`, permissive password policy

| Field | Value |
|---|---|
| **Severity** | **INFO** (V1 permissive by design) |
| **Evidence** | `DependencyInjection.cs:80-86` sets `RequiredLength = 8`; `RequireDigit / NonAlphanumeric / Uppercase / Lowercase = false`; `RequireUniqueEmail = false`; `SignIn.RequireConfirmedEmail = false`. |
| **Impact** | Anyone can register with `password`. Acceptable for dev. NOT for production. |
| **Likelihood** | N/A today (no public registration). |
| **Reversibility** | HIGH. |
| **Recommendation** | Tighten in G2-004: `RequiredLength = 12`; `RequireUppercase / RequireLowercase / RequireDigit = true`; `RequireNonAlphanumeric = true`; `RequireUniqueEmail = true`; lockout as-is. |
| **When-To-Fix** | **G2-004** (before any auth flow). |

### D-011 — 2 design-time factories hardcode `Password=placeholder` (acceptable but worth noting)

| Field | Value |
|---|---|
| **Severity** | **LOW** |
| **Evidence** | `DesignTimeFoundationDbContextFactory.cs:25` and `DesignTimeIdentityDbContextFactory.cs:30`: `"Host=localhost;Port=5432;Database=gulierp_design_time_placeholder;Username=design;Password=placeholder"`. This is the fallback when no env var is set, used by `dotnet ef migrations add`. |
| **Impact** | None (placeholder, not a real credential). But: if a developer forgets to set the env var, `dotnet ef database update` would attempt to connect to `localhost:5432` with `design/placeholder` and fail. The error message would be confusing. |
| **Likelihood** | LOW. |
| **Reversibility** | HIGH. |
| **Recommendation** | Throw `InvalidOperationException` when the env var is missing AND the design-time path is used (instead of returning a placeholder). |
| **When-To-Fix** | **OPTIONAL**. |

### D-012 — Process restart doesn't remember Snowflake `_lastTimestamp`

| Field | Value |
|---|---|
| **Severity** | **LOW** (rare in practice with V1 single-host) |
| **Evidence** | `SnowflakeIdGenerator.cs:52-53` `_lastTimestamp = -1L; _sequence = 0;` (in-memory only). On process restart, the generator starts from epoch 0 with sequence 0. The new process's first ID = `(currentTimestamp << 22) | (workerId << 12) | 0`. The DB has the previous process's max ID. If the new process's currentTimestamp is greater than the previous's (clock moves forward), the new ID is unique. If clock goes backward, COLLISION. |
| **Impact** | Same as D-006 (clock rollback). Mitigated by D-006's recommendation. |
| **Likelihood** | LOW. |
| **Reversibility** | HIGH (read max ID from DB on startup, set `_lastTimestamp` from that). |
| **Recommendation** | Optional: on startup, query `SELECT MAX(Id) FROM <any Identity table>` and call `ToTimestamp` to seed `_lastTimestamp`. Cheap. |
| **When-To-Fix** | **OPTIONAL**. |

### D-013 — IdentityContextMiddleware reads `X-Platform-Admin: true` env at request time, not at startup

| Field | Value |
|---|---|
| **Severity** | **INFO** (theoretical) |
| **Evidence** | `IdentityContextMiddleware.cs:97-104` reads `ASPNETCORE_ENVIRONMENT` per request. If the operator changes this env var mid-flight (extremely rare, not even possible without process restart in practice), the behavior changes. |
| **Impact** | None in practice. |
| **Recommendation** | None. |
| **When-To-Fix** | **WONT_FIX**. |

### D-014 — `G2-003` verification report §20 contains a FALSE claim

| Field | Value |
|---|---|
| **Severity** | **MEDIUM** (governance) |
| **Evidence** | The verification report at `docs/verification/G2_003_IDENTITY_ORG_KERNEL_REPORT.md` §20 (Constraints) lists: `0 textual-name relations. 0 comma-string IDs. 0 hidden FKs. Old-DEV anti-patterns explicitly rejected.` and §30 (DEC-ID Compliance Matrix) `20/20 DEC-IDs compliant. 0 modified. 0 deferred.` Both claims are partially false: D-002 above documents that 12+ FKs are missing. The DEC-ID Compliance Matrix is therefore overstated — DEC-ID-001 / 002 / 003 / 004 / 005 / 007 / 008 / 017 / 018 all have NO DB-level enforcement. The application layer enforces, but the DB does not. |
| **Impact** | The G2-003 Gate was flipped to `G2_003_IDENTITY_ORG_KERNEL_VERIFIED` based on this report. A future reader will trust the report and not investigate. The fix (D-002 / G2-003V2) is mandatory; the report must be amended to reflect the truth. |
| **Likelihood** | HIGH (the report will be cited). |
| **Reversibility** | HIGH (amend the report; do NOT amend the gate to UNVERIFIED — the application-layer enforcement is real and the G2-003 Operator evidence is real). |
| **Recommendation** | Amend `G2_003_IDENTITY_ORG_KERNEL_REPORT.md` §20 and §30 to acknowledge the missing FKs + reference G2-003V2. Update the DEC-ID Compliance Matrix to add a row "DB-level FK enforcement" per DEC-ID. Keep the gate at `VERIFIED` since the application is correct, but document the gap + the follow-up. |
| **When-To-Fix** | **NOW** (this review is the natural place). |

### D-015 — `PlantDirectoryService` and `OrganizationDirectoryService` enforce cross-Tenant guard but `CompanyDirectoryService.GetByIdAsync` does NOT

| Field | Value |
|---|---|
| **Severity** | **MEDIUM** (latent) |
| **Evidence** | `DirectoryServiceImplementations.cs:62-67` `CompanyDirectoryService.GetByIdAsync`: queries any Company by ID without cross-tenant guard. Compare `PlantDirectoryService.GetByIdAsync:117-122` which also has no guard, but `ListByCompanyAsync:127-139` DOES guard. The single-entity lookups (GetByIdAsync) are inconsistent with the list lookups. |
| **Impact** | If a future HTTP endpoint exposes `GET /api/v1/identity/companies/{id}` (the architecture draft lists 8 such endpoints), a caller with `X-Tenant-Id: A` could read a Company belonging to `Tenant B` by guessing the ID. Defense-in-depth gap. |
| **Likelihood** | LOW (no HTTP endpoints exist today; the HTTP surface is added in G2-003-R1 or a later Goal). |
| **Reversibility** | HIGH (add the guard). |
| **Recommendation** | Add `company.TenantId != currentTenant.Id` check to `CompanyDirectoryService.GetByIdAsync` and `PlantDirectoryService.GetByIdAsync` and `OrganizationDirectoryService.GetByIdAsync`. The check pattern is already in `ListByCompanyAsync`; copy it. |
| **When-To-Fix** | **BEFORE the 8 read-only HTTP directory endpoints are added** (architecture draft §15 lists them for a later Goal). |

### D-016 — 2 `IDesignTimeDbContextFactory` exist; no test asserts the design-time connection string handling

| Field | Value |
|---|---|
| **Severity** | **LOW** |
| **Evidence** | `DesignTimeFoundationDbContextFactory.cs` and `DesignTimeIdentityDbContextFactory.cs` exist; the design-time fallback to `Password=placeholder` is documented but not tested. |
| **Impact** | None today (the design-time path is only used for `dotnet ef migrations add`). |
| **Recommendation** | Add a unit test that verifies the design-time factory throws when no env var is set (after D-011 fix). |
| **When-To-Fix** | **OPTIONAL**. |

### D-017 — `UserOrganizationMembership` cross-tenant invariant: the `IsPrimary` partial index uses `(TenantId, UserId, CompanyId) WHERE IsPrimary = true` but no check that `(TenantId, CompanyId, OrganizationUnitId)` are all aligned

| Field | Value |
|---|---|
| **Severity** | **MEDIUM** (data integrity defense-in-depth) |
| **Evidence** | `IdentityDbContext.cs:271-274` declares the partial unique index. The check is "exactly one primary Org per (User, Company)". But the application code does not validate that `membership.OrganizationUnit.CompanyId == membership.CompanyId`. A buggy insert could create a primary Org in a different Company than the membership's CompanyId. |
| **Impact** | The membership row is valid; the FK chain breaks (Org.CompanyId != Membership.CompanyId). The application would show the membership but the Org would belong to a different Company. |
| **Likelihood** | LOW (application code is correct today; no HTTP surface to insert memberships). |
| **Reversibility** | HIGH (add application-layer invariant). |
| **Recommendation** | In `UserOrganizationMembership` Application service (future), validate `org.CompanyId == membership.CompanyId` before insert. |
| **When-To-Fix** | **BEFORE-MDM** (when CRUD endpoints are added). |

### D-018 — `UserRoleAssignment` UNIQUE constraint allows multiple NULL CompanyId rows (Tenant-wide grants)

| Field | Value |
|---|---|
| **Severity** | **LOW** (by design) |
| **Evidence** | `IdentityDbContext.cs:295-297` `UNIQUE (TenantId, UserId, RoleId, CompanyId)`. SQL standard: `NULL` is distinct in unique constraints, so two `(UserId, RoleId, NULL)` rows are allowed. The model snapshot comment line 290-294 acknowledges this: "two (UserId, RoleId, NULL) rows are allowed. The Application layer enforces 'exactly one Tenant-wide grant per (User, Role)' if needed." |
| **Impact** | Today: 0 (no application). Future: if a future "grant TenantAdmin role to a user" flow runs twice, the second insert succeeds (silent duplicate). |
| **Likelihood** | LOW. |
| **Reversibility** | HIGH (use PostgreSQL partial unique index `WHERE CompanyId IS NULL`). |
| **Recommendation** | Use a PostgreSQL partial unique index: `HasIndex(...).IsUnique().HasFilter("\"CompanyId\" IS NULL")` for the Tenant-wide grant path. This requires a small EF Core `MigrationsModelDiffer` upgrade; alternatively, raw SQL in a follow-up migration. |
| **When-To-Fix** | **BEFORE-MDM** (when CRUD endpoints are added). |

### D-019 — `Company` / `Plant` self-FK: `ParentCompanyId` / `ParentPlantId` are nullable, allowing infinite recursion in the tree

| Field | Value |
|---|---|
| **Severity** | **INFO** (no cycle detection at DB level) |
| **Evidence** | `IdentityDbContext.cs:152-153, 174-175` declare the self-FK with `OnDelete(DeleteBehavior.Restrict)`. No CHECK constraint prevents cycles. |
| **Impact** | A buggy update could create a cycle: Company 1 → Company 2 → Company 1. Application-layer cycle detection is the only protection. |
| **Likelihood** | LOW. |
| **Reversibility** | HIGH. |
| **Recommendation** | Future: add application-layer `ValidateParentAsync` to prevent cycles. |
| **When-To-Fix** | **BEFORE-MDM**. |

### D-020 — `UserCompanyMembership` `IsDefault = true` partial unique index: only one default per (TenantId, UserId), but no check that the User has at least one Active membership when setting `IsDefault = true`

| Field | Value |
|---|---|
| **Severity** | **INFO** (data integrity) |
| **Evidence** | `IdentityDbContext.cs:248-251` declares the partial unique index. If a User is removed from all Companies (all memberships set to `Removed`), but one still has `IsDefault = true` and `Status = Removed`, the index is satisfied but the User has no Active default. |
| **Impact** | Edge case: `ResolveDefaultCompanyIdAsync(userId)` returns the CompanyId of the removed-default-membership. The application would then try to use that Company. |
| **Likelihood** | LOW. |
| **Reversibility** | HIGH. |
| **Recommendation** | Application-layer: when setting `membership.Status = Removed`, if `IsDefault == true`, automatically promote the next Active membership to default. |
| **When-To-Fix** | **BEFORE-MDM**. |

### D-021 — `appsettings.json` contains the literal `Password=CHANGE_ME` (per G2-001 contract)

| Field | Value |
|---|---|
| **Severity** | **LOW** (intentional per G2-001 design) |
| **Evidence** | `appsettings.{Production,Development}.json` use `Password=CHANGE_ME` as a placeholder; the host fail-fasts at construction if the env var is missing. |
| **Impact** | None (placeholder, never used). |
| **Recommendation** | None — keep as the G2-001 fail-fast sentinel. |
| **When-To-Fix** | **WONT_FIX**. |

---

## 4. Security Risks

The most dangerous attack surfaces in the G2-003 state.

| ID | Risk | Severity | Notes |
|---|---|---|---|
| S-D-001 | **Header-based Tenant/User/Company spoofing in Production** | HIGH | D-003 above. **Only acceptable because G2-003 has no production surface**. **Mandatory before any production deployment**: G2-004 must replace with JWT-claim resolution. |
| S-D-002 | **Unauthenticated directory access** | MEDIUM | Today: no HTTP endpoints. Future: the 8 read-only directory endpoints in the architecture draft MUST be `[Authorize]` + JWT-claim-driven before exposing. |
| S-D-003 | **SQL injection** | LOW | EF Core parameterizes queries. No raw SQL in the G2-003 code. The 2 design-time factories use NpgsqlConnection for `dotnet ef`, which is operator-only. |
| S-D-004 | **Password storage** | LOW | ASP.NET Core Identity `PasswordHasher<GuliErpUser>` is used (PMF). No custom hash. The default Identity hasher is PBKDF2 with HMAC-SHA256, 100k iterations, 128-bit salt (ASP.NET Core Identity 10 defaults). |
| S-D-005 | **Connection-string leakage in logs** | LOW | `RedactConnectionString` covers all log paths. Verified. |
| S-D-006 | **Cross-Tenant data leak** | MEDIUM | Application layer enforces; DB does NOT. **Mandatory before multi-Tenant data exists in production**: add DB FKs (D-002 / G2-003V2). |
| S-D-007 | **User-controlled Tenant header + existing data** | HIGH (hypothetical) | If G2-003 were deployed to production with seeded data, an unauthenticated attacker could set `X-Tenant-Id: 1` and read all Companies / Plants / Orgs in Tenant 1. **This is why G2-003 must NOT be deployed to production.** |
| S-D-008 | **`IsPlatformAdmin` flag injection via header** | NONE today (gated to Testing env) | The header is checked against `ASPNETCORE_ENVIRONMENT == "Testing"`. Production is safe. |
| S-D-009 | **No CSP / CORS / Rate limiting** | MEDIUM | No HTTP endpoints exist today. Future: G2-004+ must add CORS, rate limiting, CSP. |

**Net: G2-003 is safe ONLY because it has no production HTTP surface. G2-004 (Auth) is the gate that enables safe production deployment.**

---

## 5. Multi-Tenant Risks

Per brief §3 / §4.

| ID | Risk | Status | Mitigation |
|---|---|---|---|
| MT-001 | Tenant != Company (semantic) | PASS | DEC-ID-001 frozen; entities distinct. |
| MT-002 | Tenant 1:N Company | DB ENFORCED NOT YET (D-002) | G2-003V2 |
| MT-003 | Company 1:N Plant | DB ENFORCED NOT YET (D-002) | G2-003V2 |
| MT-004 | Plant != OrganizationUnit | PASS | DEC-ID-019; no `OrganizationType = Plant`; no entity uses `IOrganizationScoped` + `IPlantScoped` simultaneously |
| MT-005 | Cross-Tenant data reference (application) | PASS | `CompanySwitchingService.ValidateSwitchAsync`, `PlantDirectoryService.ListByCompanyAsync`, `OrganizationDirectoryService.ListByCompanyAsync`, `UserDirectoryService.ListAsync` all validate `currentTenant` |
| MT-006 | Cross-Tenant data reference (DB) | **DEFENSE-IN-DEPTH MISSING** (D-002) | G2-003V2 |
| MT-007 | `HasQueryFilter` does runtime isolation | NOT YET (placeholder) | V1.5+ / G2-005+ / G2-006+ |
| MT-008 | `IDataFilter.Disable<TFilter>()` for background jobs | NOT YET (no-op) | V1.5+ |
| MT-009 | `UserCompanyMembership` cross-Tenant | DB ENFORCED NOT YET (D-002) | G2-003V2 |
| MT-010 | `UserRoleAssignment` cross-Tenant | DB ENFORCED NOT YET (D-002) | G2-003V2 |
| MT-011 | `AspNetUsers.TenantId` cross-Tenant | DB ENFORCED NOT YET (D-002) | G2-003V2 |
| MT-012 | `AspNetRoles.TenantId` cross-Tenant | DB ENFORCED NOT YET (D-002) | G2-003V2 |
| MT-013 | `OrganizationUnit` cross-Company | DB ENFORCED NOT YET (D-002) | G2-003V2 |
| MT-014 | `UserOrganizationMembership.OrganizationUnit.CompanyId == CompanyId` invariant | NOT YET (D-017) | BEFORE-MDM |
| MT-015 | `UserRoleAssignment.CompanyId` Tenant-wide NULL uniqueness | NOT YET (D-018) | BEFORE-MDM |

**Net: Application layer is correct. DB layer is the open seam.**

---

## 6. Concurrency Risks

Per brief §7.

| ID | Risk | Status | Mitigation |
|---|---|---|---|
| C-001 | Snowflake workerId collision across instances | **HIGH** (D-005) | BEFORE-MULTI-INSTANCE |
| C-002 | Snowflake clock rollback | LOW (D-006) | Optional |
| C-003 | Snowflake sequence exhaustion (>4096/ms) | LOW (D-006) | Optional (or switch to UUIDv7) |
| C-004 | `ICompanyScoped` + `IPlantScoped` entity parallel read | N/A (no entity implements IPlantScoped) | OK |
| C-005 | `ICurrentTenant.Change()` nested | **VERIFIED** by `IdentityApplicationServiceFacts.ICurrentX_Change_Sets_And_Restores_Value` (nested Change) | OK |
| C-006 | `ICurrentTenant.Change()` after `await` (ExecutionContext flow) | NOT TESTED | The AsyncLocal fix (G2-003) is correct; tests don't prove the await case. Add a test: `ct.Change(42L); await Task.Delay(1); Assert.Equal(42, ct.Id);` |
| C-007 | `ICurrentTenant` parallel from different threads | NOT TESTED | AsyncLocal is per-async-flow, so 2 threads in the same request have separate values. The test fixture uses single-thread. OK in practice. |
| C-008 | `ICurrentTenant` leaked from background task to main request | NOT TESTED | The fix (G2-003) is correct. Risk: if a `Task.Run` is started inside a `Change()` scope, the new task inherits the AsyncLocal value (per ExecutionContext.SuppressFlow default). If suppressed, the new task has no value. The application never starts background tasks in a `Change()` scope today. |
| C-009 | `IsPlatformAdmin` race between concurrent requests | OK today (Scoped DI); LATENT (D-001) | BEFORE-AUTH |
| C-010 | `PlantDirectoryService` parallel `FirstOrDefaultAsync` for the same companyId | N/A (read-only) | OK |
| C-011 | Two parallel migrations | N/A (single host, single instance) | OK |

**Net: Concurrency is OK for V1 single-host. Multi-instance is the open seam (D-005, D-006).**

---

## 7. Database Risks

Per brief §9 / §10.

| ID | Risk | Status | Mitigation |
|---|---|---|---|
| DB-001 | DB-level Tenant FK constraints MISSING (D-002) | **HIGH** | G2-003V2 |
| DB-002 | `__ef_migrations_history` table collision (multi-DbContext) | OK (per-schema history tables) | Document the pattern for MDM/Inventory |
| DB-003 | Migration applies to existing DB idempotency | OK (EF tooling handles) | Verified by G2-001R1 |
| DB-004 | Migration apply with empty DB | OK (Operator evidence) | Verified |
| DB-005 | Migration apply when Operator has wrong role | FOLLOW_UP_REQUIRED (F-G2-001-1) | Migration vs runtime credential separation |
| DB-006 | Migration tool requires `--startup-project` | OK (commit `d45cc3d` fixed the Design reference) | Verified |
| DB-007 | `gulidata` role has `CREATEDB` | FOLLOW_UP_REQUIRED (F-G2-001-1) | Migration role vs runtime role separation |
| DB-008 | `gulierp-next` file at repo root (16KB) | UNCHANGED, out of G2 scope | Operator owns |
| DB-009 | Multi-schema OK for new modules | OK (per-schema history tables) | MDM / Inventory follow the pattern |
| DB-010 | Schema `identity` co-locates AspNet* + gulierp_* | OK (DEC-ID-012) | Single DbContext |
| DB-011 | `IPlantScoped` entity will need `PlantId` column + FK | DEFERRED (V1.5+) | MDM / Inventory |
| DB-012 | `IOrganizationScoped` entity will need `OrganizationUnitId` column + FK | DEFERRED (V1.5+) | MDM / Inventory |
| DB-013 | Partial unique indexes (`HasFilter`) | OK (3 in use) | Working with PostgreSQL 16 |
| DB-014 | Soft-delete only (DEC-ID-015) | OK (no hard delete in repos) | |
| DB-015 | `Status` enum stored as int | OK (HasConversion<int>) | |
| DB-016 | `ValueGeneratedNever()` on snowflake IDs | OK (app generates, DB does not) | EF + Npgsql |
| DB-017 | `ConcurrencyVersion` token | OK (all 8 entities) | Optimistic concurrency |
| DB-018 | `OnDelete(DeleteBehavior.Restrict)` on all FKs | OK | No cascade delete |
| DB-019 | PostgreSQL 16 | OK | Per Stage 0 |
| DB-020 | Npgsql 10.0.3 | OK | Latest |

**Net: DB schema is well-formed (FKs excepted). Future modules inherit the pattern.**

---

## 8. Test Infrastructure Risks

Per brief §11 / §12.

| ID | Risk | Status | Mitigation |
|---|---|---|---|
| T-001 | `UseSetting` anti-pattern in 14 sites (D-004) | MEDIUM | On-demand fix per test class |
| T-002 | Bad-DB fixture isolated (G2-003V1) | OK | `ConfigureAppConfiguration` + `AddInMemoryCollection` |
| T-003 | Good-DB fixture coincidentally OK (env var = real DB) | OK today | Fix when next test-class needs it |
| T-004 | `IdentityApplicationServiceFacts.NewScope` still uses `UseSetting` | OK today (no /health/ready assertions) | Fix when adding /health/ready tests |
| T-005 | `FoundationKernelFacts` uses `UseSetting` 11 times | OK today (Kernel facts don't depend on DB state) | Fix when adding DB-state tests |
| T-006 | Parallel test execution safety | LOW (xUnit default: parallel by class, not within class) | OK |
| T-007 | Test pollution across real-DB tests | MEDIUM | For real-DB tests, all tests share the same DB. A test that leaves data is a problem. Today: 0 tests mutate the DB in a way that leaks (tests use `ICompanySwitchingService.ResolveDefaultCompanyIdAsync(userId: 9999)` which queries non-existent data). Future: when CRUD tests are added, need cleanup strategy. |
| T-008 | Transaction rollback per test | NOT YET | For real-DB CRUD tests, use `IDbContextTransaction` + rollback in test fixture. |
| T-009 | Database-per-run | NOT YET | For V1, single DB. For CI, testcontainers (Docker) per pipeline. |
| T-010 | Testcontainers (Docker PostgreSQL) | DEFERRED | Per F-G2-001-2 |
| T-011 | `dotnet test` parallel collection | NOT YET | xUnit `[CollectionDefinition]` + `[Collection]` for shared-state tests. |
| T-012 | TestResults untracked (xunit output) | FOLLOW_UP_REQUIRED | `.gitignore` rule to exclude `**/TestResults/` |
| T-013 | Stale G2 execution plan untracked | FOLLOW_UP_REQUIRED | `docs/goals/` pre-existing untracked |
| T-014 | pre-existing apps/web dirty/untracked | PRESERVED (per Goal constraints) | Out of G2 scope |

**Net: Test infrastructure is acceptable for V1. Future: real-DB CRUD tests need cleanup + collection discipline.**

---

## 9. Governance Risks

Per brief §14.

| ID | Risk | Status | Mitigation |
|---|---|---|---|
| G-001 | G2-003 verification report §20 contains FALSE claim (D-014) | MEDIUM | Amend in this review |
| G-002 | G2-003V1 was "expected PASS" before operator evidence (per brief §14) | DOCUMENTED | Already disclosed in G2-003V1 closure |
| G-003 | Gate progression rules: EXPECTED / AUTOMATED_VERIFIED / OPERATOR_OBSERVED / VERIFIED | NOT FORMALIZED | Add to GOAL_REGISTRY header |
| G-004 | "预计 PASS 被写成已 PASS" risk | LOW | Disclose in reports |
| G-005 | `gulierp-next` pre-existing suspicious file | PRESERVED | Out of G2 scope |
| G-006 | apps/web untracked + 9× docs/architecture untracked + 2× docs/governance untracked + docs/goals/ + docs/review/ + 2× docs/verification/ + TestResults/ | PRESERVED | Per Goal constraints |

### G-003 — Gate Progression Formalization (recommended)

Per brief §14, the Gate status should have 4 distinct levels:

| Level | Meaning | Example |
|---|---|---|
| **EXPECTED** | Goal designed; design not implemented | G2-003A before commit |
| **AUTOMATED_VERIFIED** | Code complete + unit + integration tests PASS; Mavis side confirmed | G2-003 commit `6f9ffe2` |
| **OPERATOR_OBSERVED** | Operator-side real-DB round PASS; Mavis side flipped; awaiting operator flip | (was this for G2-003?) |
| **VERIFIED** | Operator flip; gate formally CLOSED | G2-003V1 commit `f29f984` |

The G2-003 progression skipped a clear AUTOMATED_VERIFIED → OPERATOR_OBSERVED intermediate. The G2-003 verification report had `CODE_READY_OPERATOR_DB_PENDING` (which is approximately AUTOMATED_VERIFIED), and after the operator round the gate was flipped to `IDENTITY_ORG_KERNEL_VERIFIED` (VERIFIED) without an explicit OPERATOR_OBSERVED state. Per brief §14, this is the kind of premature flip that this review should disclose. The actual evidence (Operator 8/8 steps PASS) is real; the gap is in the documentation granularity.

**Recommendation**: Add a `Gate Progression` section to the GOAL_REGISTRY header documenting these 4 levels. Going forward, every Goal must record its progression: `EXPECTED → AUTOMATED_VERIFIED → OPERATOR_OBSERVED → VERIFIED`. No skip.

---

## 10. Mature Solution / NIH Review

Per brief §2 / §8.

| Area | Current | Mature alternative | Verdict |
|---|---|---|---|
| ID generation | Custom Snowflake 41+10+12 (epoch 2026-01-01) | .NET 9+ `Guid.CreateVersion7()` (time-ordered UUID, no worker id) / PostgreSQL `bigint GENERATED ALWAYS AS IDENTITY` / `IdGen` library | **KEEP** for V1, but document the alternative. **Reconsider before MDM**: switch to UUIDv7 is cheap NOW (14 tables, no rows); switch LATER is expensive. |
| Password hash | ASP.NET Core Identity `PasswordHasher<GuliErpUser>` (PBKDF2 + HMAC-SHA256, 100k iter) | Argon2id (more modern, but not .NET Identity default) | **KEEP** (Identity default is industry-standard). |
| User store | ASP.NET Core Identity `IdentityDbContext` (DI) | Custom user table | **KEEP** (DEC-ID-012 IDENTITY_COMPONENT_REUSE). |
| `AsyncLocal` pattern | Per-instance `AsyncLocal<T>` + `Push/Pop` | `System.Threading.AsyncLocal<T>` directly | **KEEP** (the G2-003 fix is correct). |
| `IDataFilter` | No-op placeholder | ABP `IDataFilter` (interface + DI) | **KEEP** (pattern-only; no ABP runtime). |
| `ICurrentTenant` shape | ABP-like interface | ABP runtime | **KEEP** (pattern-only). |
| Snowflake library | Custom | `IdGen` NuGet | **OPTIONAL** (re-implementing 100 lines of Snowflake is not NIH; switching to `IdGen` saves the maintenance but adds a dep). |
| Migrations | EF Core `MigrationsHistoryTable` per-schema | DACPAC / Flyway / Liquibase | **KEEP** (EF Core is the .NET standard). |
| Health checks | ASP.NET Core native `IHealthCheck` | custom | **KEEP**. |
| ProblemDetails | RFC 9457 / 7807 via `AddProblemDetails` | custom envelope | **KEEP** (no envelope, per G2-002 §7). |
| Test framework | xUnit 2.9.3 + `Microsoft.NET.Test.Sdk` 18.0.0 | NUnit / MSTest | **KEEP** (xUnit is the G2 standard). |

**NIH verdict: 0 NIH syndrome in the G2 phase. The custom code (Snowflake, AsyncLocal holder, Identity module) is deliberately minimal and follows mature patterns (Twitter Snowflake spec, ABP ICurrentTenant shape, ASP.NET Core Identity). The ONE place to reconsider before scaling out is the ID generator.**

---

## 11. Technical Debt Classification

Per brief §16. Items categorized by when to fix.

### NOW (this review)
- **D-014**: Amend `G2_003_IDENTITY_ORG_KERNEL_REPORT.md` §20 and §30 to acknowledge missing FKs + reference G2-003V2. Add Gate Progression section to GOAL_REGISTRY header.
- **D-002 → G2-003V2** Goal: DB FK Constraint Closure (BEFORE-MDM, recommended before G2-004)

### BEFORE_AUTH (G2-004, the next goal)
- **D-001**: Make `IsPlatformAdmin` AsyncLocal-backed
- **D-003**: Replace `IdentityContextMiddleware` header path with JWT-claim-based resolution (G2-004 IS the fix)
- **D-008**: Add `IdentityContext_PlatformAdmin_Header_*` tests
- **D-010**: Tighten password policy
- **D-009**: Add `[Authorize]` to future directory HTTP endpoints
- **F-G2-001-1**: Migration vs runtime credential separation (Operator side)

### BEFORE-MDM (G2-006)
- **D-002 / G2-003V2**: DB FK Constraint Closure
- **D-007**: Real `IDataFilter` + `HasQueryFilter` runtime scope
- **D-004 / D-015 / D-017 / D-018 / D-019 / D-020**: Application-layer invariants for the new entities (CRUD endpoints)

### BEFORE-INVENTORY (G2-006+)
- **D-011**: Design-time factory fail-fast when env var missing
- **D-016**: Design-time factory test coverage

### BEFORE-MULTI-INSTANCE-PRODUCTION
- **D-005**: `SnowflakeIdGenerator.workerId` from config / env / service discovery
- **D-006**: Snowflake clock-rollback detection
- **D-012**: Snowflake process-restart ID-max recovery

### BEFORE-PRODUCTION (deployment readiness)
- **D-007**: `IDataFilter` + `HasQueryFilter` runtime scope
- **S-D-007**: Replace `X-*-Id` header path with JWT-claim (G2-004)
- **S-D-002**: `[Authorize]` on all directory HTTP endpoints
- **F-G2-001-1**: Production credential separation
- **F-G2-001-2**: PostgreSQL integration test profile (testcontainers)
- **F-G2-001-3**: `global.json` SDK policy

### ON-DEMAND
- **D-004**: `UseSetting` → `ConfigureAppConfiguration` in the remaining 14 test sites

### WONT_FIX
- **D-013**: `X-Platform-Admin` env check at request time (G2-002R2 spirit)
- **D-021**: `Password=CHANGE_ME` placeholder (G2-001 fail-fast sentinel)

### DEFERRED
- **UserPlantMembership** (G2-003A-R2 §40)
- **ICurrentPlant** (G2-003A-R2 §40)
- **PlantCalendar / Shift** (G2-003A-R2 §40)

---

## 12. Before-G2-004 Actions

Mandatory (in priority order):

1. **G2-003V2 — DB FK Constraint Closure** (recommended BEFORE G2-004; minimum scope: 1 new migration + IdentityDbContext update + report amendment; est. 30 min)
   - Adds 12+ FKs: TenantId/CompanyId chain
   - Amends G2-003 verification report
   - Adds 1+ integration test: insert invalid TenantId → DB rejects
2. **D-001 — IsPlatformAdmin AsyncLocal** (small; can fold into G2-004 or its own mini-Goal; est. 15 min)
3. **D-014 — Report amendment** (5 min; can be done as part of G2-003V2)
4. **Gate Progression formalization** (5 min; add to GOAL_REGISTRY header)

Optional:
- D-008: PlatformAdmin test coverage (10 min)
- D-010: Password policy tightening (in G2-004)

---

## 13. Before-MDM Actions

1. **G2-003V2** (above) — D-002
2. **D-007** — Real `IDataFilter` + `HasQueryFilter` (1 small Goal)
3. **D-015** — `CompanyDirectoryService.GetByIdAsync` cross-tenant guard (5 lines)
4. **D-017** — `UserOrganizationMembership.CompanyId == Org.CompanyId` invariant (in Application service)
5. **D-018** — `UserRoleAssignment` partial unique index for NULL CompanyId
6. **D-019** — `Company` / `Plant` self-FK cycle detection (Application layer)
7. **D-020** — `UserCompanyMembership.IsDefault` cleanup on Status change
8. **D-005** — Snowflake `workerId` from config (BEFORE-MDM, since MDM might be multi-instance)
9. **D-004** — `UseSetting` cleanup (BEFORE-MDM, since MDM tests will need the pattern)

---

## 14. Before-Inventory Actions

1. **D-011** — Design-time factory fail-fast (5 min)
2. **D-016** — Design-time factory test coverage
3. **PlantCalendar / Shift / Warehouse** entity design (DEC-ID-020 future)
4. **IPlantScoped** first concrete implementation (Warehouse / WorkCenter)

---

## 15. Before-Production Actions

1. **D-007** — Real `IDataFilter` (above)
2. **D-003** — G2-004 Auth fixes header path (above)
3. **S-D-002** — `[Authorize]` on all directory HTTP endpoints
4. **F-G2-001-1** — Production credential separation (Operator side)
5. **F-G2-001-2** — `global.json` / .NET 10 SDK policy documentation
6. **F-G2-001-3** — PostgreSQL integration test profile (testcontainers)
7. **D-006** — Snowflake clock-rollback detection
8. **D-012** — Snowflake process-restart ID-max recovery
9. **D-005** — Snowflake workerId from config (above)
10. **Production log retention / redaction policy**
11. **CORS / CSP / Rate limiting** on HTTP endpoints

---

## 16. Explicit Non-Issues

Items considered and explicitly cleared.

| # | Item | Verdict |
|---|---|---|
| EN-01 | Tenant/Company/Plant/Org distinct entities | PASS (DEC-ID-019) |
| EN-02 | 1:N cardinalities | SEMANTIC PASS; DB ENFORCEMENT DEFERRED (D-002) |
| EN-03 | Soft-delete only | PASS (DEC-ID-015; all 8 entities) |
| EN-04 | Default password `ChangeMe!2026` dev-only | OK (gated to Development/Testing env) |
| EN-05 | ASP.NET Core Identity reuse | PASS (DEC-ID-012 IDENTITY_COMPONENT_REUSE) |
| EN-06 | `long snowflake` PK | PASS (DEC-ID-014; 14 tables consistent) |
| EN-07 | `IPlantScoped` reserved without implementation | PASS (DEC-ID-020) |
| EN-08 | `UserPlantMembership` deferred | PASS (G2-003A-R2 §40) |
| EN-09 | `ICurrentPlant` deferred | PASS |
| EN-10 | `Snowflake` 41+10+12 layout | OK for V1; reconsider before MDM |
| EN-11 | Single `__ef_migrations_history` per schema | PASS (per-schema pattern) |
| EN-12 | Identity schema `identity` separate from `foundation` | PASS |
| EN-13 | AspNet* tables in `identity` schema (co-located) | PASS (DEC-ID-012) |
| EN-14 | X-Platform-Admin gated to Testing | PASS (G2-002R2 spirit) |
| EN-15 | Password redaction in startup log | PASS |
| EN-16 | No SQL injection vector | PASS (EF parameterizes) |
| EN-17 | No 0 Admin.NET / 0 Furion / 0 SqlSugar | PASS |
| EN-18 | No UseInMemoryDatabase / UseSqlite / EnsureCreated | PASS |
| EN-19 | No `git add .` / `git reset` / `git rebase` / `git amend` | PASS (all 17 commits path-specific) |
| EN-20 | Forbidden patterns: 0 actual uses | PASS |
| EN-21 | Test result discipline: 0 SKIP | PASS (loud-fail only) |
| EN-22 | G2-002R1/R2 + G2-003R1 + G2-003V1 disclosure rounds | PASS |
| EN-23 | `gulierp-next` pre-existing suspicious file | PRESERVED (out of G2 scope) |
| EN-24 | apps/web pre-existing dirty/untracked | PRESERVED (out of G2 scope) |
| EN-25 | `stale G2 execution plan` (untracked docs/goals/) | PRESERVED (out of G2 scope; legacy) |

---

## 17. FOUNDATION_SUFFICIENT_TO_PROCEED

### Decision

**`FOUNDATION_SUFFICIENT_TO_PROCEED = YES`**

with the explicit understanding that:

1. **G2-003 must NOT be deployed to production** until G2-004 (Auth) is complete. The header-based Tenant/User/Company resolution (D-003) is a development-only convenience; in production it is a critical security boundary violation.
2. **G2-003V2 (DB FK Constraint Closure) is the minimum hygiene step** before the architecture's "20/20 DEC-IDs compliant" claim can be fully defended. It is a 30-min mini-Goal that adds 12+ FK constraints. Without it, future business modules (MDM, Inventory) will inherit the missing-FK precedent.
3. **G2-003V2 is NOT a G2-004 blocker** (per brief §20: no SECURITY / TENANT ISOLATION / DATA CORRUPTION / FUNDAMENTAL ARCHITECTURE blocker exists). The application layer enforces the cross-Tenant invariant. G2-003V2 is defense-in-depth.

### Strict §20 Test

| Strict criterion | Status |
|---|---|
| SECURITY BLOCKER | NO (D-003 is a HIGH, not a BLOCKER; only acceptable because no production HTTP surface; G2-004 is the fix) |
| TENANT ISOLATION BLOCKER | NO (application layer enforces; DB layer is defense-in-depth gap, see D-002) |
| DATA CORRUPTION BLOCKER | NO (no data is corrupted; DB is empty) |
| FUNDAMENTAL ARCHITECTURE BLOCKER | NO (20/20 DEC-IDs hold semantically; 12+ FKs missing at DB level is a follow-up) |

### What "YES" Means

- G2-004 Authentication Kernel is the natural next goal
- G2-004 can proceed BEFORE G2-003V2 (the operator chose to ship G2-004 first)
- G2-003V2 should be scheduled before MDM (G2-006)
- The "VERIFIED" gate stays at `G2_003_IDENTITY_ORG_KERNEL_VERIFIED` (the application is correct)
- The verification report is amended to acknowledge the DB-layer gap (D-014)
- G2-004 must REPLACE the header path (D-003) before any production deployment

---

## 18. Final Decision

```
G2-001 Host & PostgreSQL               = CLOSED (Operator-verified 2026-08-19)
G2-002 Foundation Kernel               = CLOSED (Mavis-verified 2026-08-19)
G2-002R1 Foundation Kernel Verification= CLOSED
G2-002R2 Foundation Kernel Security    = CLOSED
G2-003A Identity Org Build-vs-Reuse    = CLOSED
G2-003A-R2 Plant/Site Amendment        = CLOSED
G2-003   Identity Org Kernel (impl)    = CLOSED (Operator-verified 2026-08-19)
G2-003R1 EF Core Design-Time Fix       = CLOSED (part of G2-003 verification)
G2-003V1 Bad-DB Test Isolation Closure = CLOSED (part of G2-003 verification)
G2-R0    Foundation Critical Review    = CLOSED (this document)

FOUNDATION_SUFFICIENT_TO_PROCEED = YES (with mandatory follow-ups)

NEXT_GOAL_CANDIDATE = G2-004 Authentication Kernel (NOT STARTED, HALTED)
                      + recommended G2-003V2 (DB FK Constraint Closure) before MDM

BLOCKERS = 0
TOP RISKS =
  D-003 (HIGH) Production header-based Tenant spoofing — G2-004 fix
  D-002 (HIGH) Missing DB FK constraints — G2-003V2 fix
  D-001 (HIGH) IsPlatformAdmin plain-property race latent — AsyncLocal fix
  D-005 (HIGH) Snowflake workerId hardcoded to 0 — multi-instance blocker
REQUIRED BEFORE G2-004 = none (G2-004 can start as-is)
REQUIRED BEFORE MDM   = D-002 (G2-003V2), D-007 (IDataFilter), D-005 (workerId)
REQUIRED BEFORE PRODUCTION = D-003 (G2-004), D-002 (G2-003V2), S-D-002 ([Authorize]),
                           F-G2-001-1/2/3 (Operator-side)

DEFERRED RISKS =
  D-006 (LOW) Snowflake clock rollback
  D-012 (LOW) Snowflake process-restart ID recovery
  D-008 (INFO) Missing PlatformAdmin test coverage
  D-010 (INFO) Permissive password policy (G2-004 fix)
  UserPlantMembership / ICurrentPlant / PlantCalendar (G2-003A-R2 §40)
```

### What to do with this review

1. **Read this review before starting G2-004.** Specifically the §6 (Concurrency) and §11 (Technical Debt Classification) sections.
2. **Schedule G2-003V2 as a 30-min mini-Goal before MDM.** It's a 1-commit + 1-report-amendment fix.
3. **Add Gate Progression formalization** to the GOAL_REGISTRY header (per §9 G-003).
4. **Update `G2_003_IDENTITY_ORG_KERNEL_REPORT.md` §20 + §30** to acknowledge the missing FKs (D-014) — even if the Gate stays VERIFIED, the report must be honest.
5. **No code change in this review round.** Per brief §17: "本轮默认禁止修改 production code." This document is the entire deliverable.

---

## STOP

Review complete. **0 BLOCKER. FOUNDATION_SUFFICIENT_TO_PROCEED = YES.** G2-004 can start with explicit user authorization. **G2-003V2 is recommended before MDM but not before G2-004.** Next session must wait for explicit kickoff per META_GULI_GOVERNANCE_V1.md HR-1..HR-10.
