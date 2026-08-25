# G2 — Foundation Execution Plan

| Field | Value |
|---|---|
| Goal | Decompose G2 Foundation into small, independently shippable goals (G2-001..G2-010) |
| Entry Gate | `GULIERP_CORE_BUSINESS_SPEC_FROZEN` |
| Output Status | **DRAFT — for review only** |
| Author | Mavis (single writer, planner role) |
| Companion docs | G2 architecture drafts (TASK B–G), `META_GULI_GOVERNANCE_V1.md` |
| Codex quota note | Per the brief, Codex has no quota right now — this plan tags each goal as **MiniMax-safe**, **TRAE-feasible**, or **Codex-when-available** |
| Goal | Each sub-goal has a clear **Gate** that the Operator (not the Agent) flips |

> ## ⚠️ AUTHORITATIVE PHASE MAP — G2-002R1 update (2026-08-19)
>
> The phase map below was WRITTEN BEFORE the G2-002 goal brief and
> incorrectly scoped G2-002 to "Identity (Tenant/Company/Org/User/Role)".
> The actual G2-002 goal brief (see
> `docs/verification/G2_002_FOUNDATION_KERNEL_REPORT.md`) re-scoped
> G2-002 to **Foundation Cross-Cutting Kernel ONLY**, and the
> implementation matches that scope (Gate =
> `G2_002_FOUNDATION_KERNEL_VERIFIED`, commit `dbc29db`).
>
> **Authoritative phase map (G2-002R1, 2026-08-19):**
>
> | # | Goal | Actual scope | Gate | Status |
> |---|---|---|---|---|
> | G2-001 | Host & PostgreSQL | Boot host + Foundation schema migration + /health/live + /health/ready | `G2_001_HOST_POSTGRESQL_VERIFIED` | ✅ VERIFIED (commit `3673016`) |
> | G2-002 | **Foundation Cross-Cutting Kernel** | Exception boundary + RFC ProblemDetails + RequestContext + RequestId/TraceId + structured logging + config validation + /api/v1/system/ping | `G2_002_FOUNDATION_KERNEL_VERIFIED` | ✅ VERIFIED (commit `dbc29db`) |
> | G2-003 | **Identity & Organization Kernel** | Tenant/Company/Organization/User/Role tables + EF mappings + tenant scope middleware (no auth yet) | (NOT YET FLIPPED) | 🚧 **NOT STARTED** |
> | G2-004 | Authentication (Argon2id + JWT + Refresh) | login / refresh / logout / me endpoints | (NOT YET FLIPPED) | 🚧 NOT STARTED |
> | G2-005 | API Permission (Role → Permission) | `[RequirePermission(...)]` | (NOT YET FLIPPED) | 🚧 NOT STARTED |
> | ... | (rest unchanged) | ... | ... | ... |
>
> **Priority for any next Agent session**: the body of this file
> below is OBSOLETE for G2-002 — it is the pre-brief draft. The
> authoritative source of truth is:
> 1. `docs/governance/GOAL_REGISTRY.md` (gate flip + scope)
> 2. `docs/verification/G2_002_FOUNDATION_KERNEL_REPORT.md` (verified scope)
> 3. The frozen goal brief in the system reminder that started G2-002.
>
> **The legacy G2-002 = "Identity" text below is preserved for
> historical reference only. It MUST NOT be re-used to drive a future
> Goal, because that future Goal would re-implement Identity on top of
> a Cross-Cutting Kernel, which is the wrong layering.**

---

---

## 1. Mission

Break the G2 Foundation into **10 small, vertically-thin, horizontally-complete** goals. Each goal:

1. Has a **single, testable purpose**.
2. Touches the **smallest possible surface** (fewest files, fewest contracts).
3. Ends with a **Gate** the Operator flips after exercising it.
4. Is assigned to an **agent class** (MiniMax / TRAE / Codex-when-available).
5. Has a **Do Not Do** list to prevent scope creep.
6. Has a **clear "next"** — what is unblocked when this goal is green.

The 10 goals are ordered by **dependency**, not by priority. A goal may be done out of order only if the operator explicitly accepts the integration risk.

---

## 2. The 10 goals (overview)

| # | Title | Purpose | Agent | Est. | Depends on |
|---|---|---|---|---|---|
| G2-001 | Host & PostgreSQL | Boot a host that returns 200 on `/healthz` against a real PostgreSQL | MiniMax | 1 day | (none) |
| G2-002 | Identity (Tenant/Company/Org/User/Role) | Foundation tables + EF Core mappings; no auth yet | MiniMax | 1 day | G2-001 |
| G2-003 | Authentication (Argon2id + JWT + Refresh) | Login / refresh / logout / me endpoints | MiniMax | 1.5 day | G2-002 |
| G2-004 | API Permission (Role → Permission) | `[RequirePermission(...)]` works end-to-end | MiniMax | 0.5 day | G2-003 |
| G2-005 | Audit (write + read contract) | Every state change writes to `audit_entry`; `IAuditWriter` works | MiniMax | 0.5 day | G2-001 |
| G2-006 | Dictionary + Numbering | `IDictionaryQuery` + `INumberGenerator` real impls | MiniMax | 0.5 day | G2-002 |
| G2-007 | Module Runtime (`IModule` + Registry) | A stub `IModule` can be plugged in; edition packaging works | TRAE-friendly | 1 day | G2-001, G2-005 |
| G2-008 | Frontend Shell Integration (auth + tenant + menu) | The R3 design system shell talks to a real backend | TRAE | 1.5 day | G2-003, G2-004, G2-007 |
| G2-009 | IApprovalService V1 (simple approval) | The simple approval capability per TASK G | MiniMax | 1 day | G2-003, G2-005 |
| G2-010 | Foundation Runtime Acceptance | All 5 test suites green; `/healthz` 200; one round-trip works | MiniMax | 0.5 day | G2-001..G2-009 |

**Total estimate: ~9.5 working days of agent time**, plus operator review at each Gate. Realistic calendar: 2-3 weeks with operator review, Codex unavailability, and rework.

---

## 3. Per-goal detail

### G2-001 — Host & PostgreSQL

| Field | Value |
|---|---|
| **Purpose** | Prove the platform: a host boots, returns 200 on `/healthz`, connects to a real PostgreSQL, applies a single migration, and exits cleanly. |
| **Scope** | (1) `GuliERP.Host` empty project with `Program.cs`; (2) `GuliERP.Foundation.Infrastructure` with `GuliDbContext`; (3) one initial migration creating a single `meta` table; (4) `/healthz` and `/readyz` endpoints; (5) `appsettings.json` + `appsettings.Development.json` + env-var config; (6) logging setup; (7) exception boundary stub; (8) Testcontainers.PostgreSQL helper; (9) one smoke test. |
| **Reuse** | N/A (greenfield). |
| **Do Not Do** | Do NOT add any business code. Do NOT add authentication. Do NOT add tenants/users yet. Do NOT add a UI. Do NOT use Admin.NET / Furion / SqlSugar. |
| **Files** | `src/GuliERP.Host/Program.cs`, `src/GuliERP.Foundation.Infrastructure/GuliDbContext.cs`, `src/GuliERP.Foundation.Infrastructure/Migrations/...`, `tests/GuliERP.Foundation.RuntimeSmokeTests/HealthCheckTests.cs` |
| **Tests** | `RuntimeSmokeTests`: 1 test (`Health_Returns_200_When_DB_Up`); uses Testcontainers.PostgreSQL |
| **Gate** | `G2_001_HOST_AND_POSTGRES_READY` — flipped by Operator after they see CI pass + smoke evidence |
| **Recommended Agent** | MiniMax (Codex unavailable; TRAE not needed for backend) |
| **Est. Duration** | 1 day |
| **Unblocks** | G2-002, G2-005, G2-006, G2-007 |
| **Risks** | Local PostgreSQL setup; if Operator's box has no PostgreSQL, use docker-compose. See TASK I. |

### G2-002 — Identity (Tenant/Company/Org/User/Role)

| Field | Value |
|---|---|
| **Purpose** | Foundation tables exist, are queryable, and respect tenant isolation. No login yet. |
| **Scope** | (1) `tenant`, `company`, `organization`, `user`, `role`, `user_role`, `user_company` tables + EF Core mappings per TASK B §6.1 and TASK E; (2) `ITenantContext`, `ICompanyContext`, `IOrganizationContext` (last scoped to current org, V1 stub returns "all in tenant"); (3) a `UseTenantScope` middleware that reads `X-Tenant-Id` from header in V0 and is no-op in V1 (will switch to JWT in G2-003); (4) seed data: 1 tenant, 1 company, 1 root org, 1 user "admin", 1 role "TenantAdmin"; (5) CRUD endpoints for admin (no auth yet, or simple Basic auth placeholder); (6) one smoke test. |
| **Reuse** | The `concurrency_version` pattern from TASK E; the snowflake generator from G2-001. |
| **Do Not Do** | Do NOT add real authentication yet (G2-003). Do NOT add JWT. Do NOT add `IUserPasswordHasher` yet (added in G2-003). Do NOT add audit yet (G2-005). Do NOT add the organization tree queries yet (interface only). |
| **Files** | `src/GuliERP.Foundation.Domain/Identity/*`, `src/GuliERP.Foundation.Application/Identity/ICurrentUser.cs` (stub returns "admin"), `src/GuliERP.Foundation.Infrastructure/Identity/*Repository.cs`, `src/GuliERP.Host/Controllers/Admin/IdentityController.cs` (minimal API), `src/GuliERP.Host/Middleware/UseTenantScope.cs` |
| **Tests** | UnitTests: tenant isolation (a query for tenant A returns no rows from tenant B), `concurrency_version` increments on update; IntegrationTests (real DB): 1 test that creates 2 tenants + 2 users + 2 roles and verifies the global filter |
| **Gate** | `G2_002_IDENTITY_READY` — flipped by Operator after exercising the admin endpoints manually |
| **Recommended Agent** | MiniMax |
| **Est. Duration** | 1 day |
| **Unblocks** | G2-003, G2-006, G2-009 |

### G2-003 — Authentication (Argon2id + JWT + Refresh)

| Field | Value |
|---|---|
| **Purpose** | Users can log in and access protected endpoints. |
| **Scope** | (1) `users.password_hash` column (added to G2-002's migration or new migration); (2) `IUserPasswordHasher` using Argon2id; (3) `IJwtTokenService` (HS256, 15 min); (4) `IRefreshTokenService` (rotation, family revocation); (5) `/api/v1/auth/login`, `/refresh`, `/logout`, `/me` endpoints; (6) `[Authorize]` attribute wired to JWT bearer middleware; (7) `UseAuthentication` middleware in the pipeline; (8) lockout logic (5 attempts / 15 min); (9) `IUserPasswordHasher` config (memory, iterations); (10) one smoke test end-to-end (login → me → logout). |
| **Reuse** | The `users` table from G2-002. |
| **Do Not Do** | Do NOT add SSO / OAuth / OIDC. Do NOT add MFA. Do NOT add API keys. Do NOT add password reset (V1: admin-reset only via SQL or future G2-005+). |
| **Files** | `src/GuliERP.Foundation.Infrastructure/Auth/Argon2idPasswordHasher.cs`, `src/GuliERP.Foundation.Infrastructure/Auth/JwtTokenService.cs`, `src/GuliERP.Foundation.Infrastructure/Auth/RefreshTokenService.cs`, `src/GuliERP.Host/Endpoints/AuthEndpoints.cs`, `src/GuliERP.Host/Middleware/UseAuthentication.cs` |
| **Tests** | UnitTests: password hasher, JWT issuance, JWT validation, lockout; IntegrationTests: login → me round-trip, refresh rotation, replay-revokes-family; SmokeTests: end-to-end happy path |
| **Gate** | `G2_003_AUTH_READY` — flipped by Operator after manually logging in via the R3 shell and getting `/me` to return the user |
| **Recommended Agent** | MiniMax (no frontend work needed; backend only) |
| **Est. Duration** | 1.5 day |
| **Unblocks** | G2-004, G2-008, G2-009 |
| **Risks** | Argon2id library choice; **`Isopoh.Cryptography.Argon2`** is the standard. JWT key management (env var only). Refresh token family revocation is easy to get wrong — needs careful test. |

### G2-004 — API Permission (Role → Permission)

| Field | Value |
|---|---|
| **Purpose** | Business endpoints can declare a required permission; users without the permission get 403. |
| **Scope** | (1) `permission` and `role_permission` tables; (2) seed for 5 sample permissions (e.g. `sales.order.read`, `sales.order.create`, `sales.order.approve`, `inventory.item.read`, `admin.user.read`); (3) `IPermissionService.HasAsync(code)` with 5-min in-memory cache; (4) `[RequirePermission("...")]` attribute + `IPermissionAuthorizationHandler`; (5) `UsePermissionScope` middleware; (6) `IPermissionService.HasButtonAsync` stub (returns `false` for V1); (7) admin endpoint to grant/revoke permissions to roles; (8) one smoke test. |
| **Reuse** | The `user_role` and `role` tables from G2-002. |
| **Do Not Do** | Do NOT add data-scope, field-policy, row-policy, condition-policy. Do NOT add approval-limit. Do NOT add button-permission real impl. |
| **Files** | `src/GuliERP.Foundation.Domain/Identity/Permission.cs`, `src/GuliERP.Foundation.Application/Security/IPermissionService.cs`, `src/GuliERP.Foundation.Infrastructure/Security/PermissionService.cs`, `src/GuliERP.Host/Authorization/RequirePermissionAttribute.cs`, `src/GuliERP.Host/Authorization/PermissionAuthorizationHandler.cs`, `src/GuliERP.Host/Middleware/UsePermissionScope.cs` |
| **Tests** | UnitTests: `HasAsync` returns true/false correctly, cache invalidation; IntegrationTests: a user with the permission can call the endpoint, a user without it gets 403; ArchitectureTest: `[RequirePermission]` is on every business endpoint |
| **Gate** | `G2_004_PERMISSION_READY` |
| **Recommended Agent** | MiniMax |
| **Est. Duration** | 0.5 day |
| **Unblocks** | G2-008 |

### G2-005 — Audit (write + read contract)

| Field | Value |
|---|---|
| **Purpose** | Every state change writes to `audit_entry`. Operators can query audit by entity or actor. |
| **Scope** | (1) `audit_entry` table per TASK B §11 and TASK E; (2) `IAuditWriter.WriteAsync` impl; (3) EF Core `SaveChanges` interceptor for automatic `EntityAdded/Modified/Deleted` logging on `IAuditable` entities; (4) `IUserAuditQuery.GetByEntityAsync(type, id)` and `GetByActorAsync(userId, range)`; (5) `POST /api/v1/admin/audit/query` endpoint (admin-only); (6) DB role separation test (`gulierp_app` cannot DELETE from `audit_entry`); (7) one smoke test. |
| **Reuse** | The `audit_entry` schema from TASK E. |
| **Do Not Do** | Do NOT add an Audit Trail UI (V1.5+). Do NOT add audit export. Do NOT add partitioning (V1: single table; partition in V1.5). |
| **Files** | `src/GuliERP.Foundation.Domain/Audit/AuditEntry.cs`, `src/GuliERP.Foundation.Application/Audit/IAuditWriter.cs`, `src/GuliERP.Foundation.Application/Audit/IUserAuditQuery.cs`, `src/GuliERP.Foundation.Infrastructure/Audit/AuditWriter.cs`, `src/GuliERP.Foundation.Infrastructure/Audit/AuditInterceptor.cs`, `src/GuliERP.Foundation.Infrastructure/Audit/UserAuditQuery.cs`, `src/GuliERP.Host/Endpoints/Admin/AuditEndpoints.cs` |
| **Tests** | UnitTests: `AuditWriter` writes correct fields, filter drops sensitive fields; IntegrationTests: after a `Create` + `Update`, 2 audit rows exist; DB-role test: `gulierp_app` DELETE on `audit_entry` is rejected |
| **Gate** | `G2_005_AUDIT_READY` |
| **Recommended Agent** | MiniMax |
| **Est. Duration** | 0.5 day |
| **Unblocks** | G2-007, G2-009 |

### G2-006 — Dictionary + Numbering

| Field | Value |
|---|---|
| **Purpose** | Business modules can read dropdowns and generate document numbers. |
| **Scope** | (1) `dictionary`, `dictionary_item` tables; (2) `IDictionaryQuery.GetAsync(code)` with 5-min cache, `IDictionaryAdminService` for CRUD; (3) `number_sequence` table; (4) `INumberGenerator.NextAsync(code, date)` with collision re-roll; (5) seed: 5 sample dictionaries (`uom`, `currency`, `tax_rate`, `payment_term`, `country`); (6) admin endpoints to manage dictionaries; (7) one smoke test. |
| **Reuse** | The `users` table for `created_by` / `updated_by` in dictionary items. |
| **Do Not Do** | Do NOT add a Dictionary admin UI. Do NOT add a "reactive push" (V1.5). Do NOT add per-tenant dictionaries in V1 (single global set, but tenant-scoped via filter). |
| **Files** | `src/GuliERP.Foundation.Domain/Dictionary/*`, `src/GuliERP.Foundation.Application/Dictionary/IDictionaryQuery.cs`, `src/GuliERP.Foundation.Application/Numbering/INumberGenerator.cs`, `src/GuliERP.Foundation.Infrastructure/Dictionary/*`, `src/GuliERP.Foundation.Infrastructure/Numbering/NumberGenerator.cs` |
| **Tests** | UnitTests: cache hit/miss, number sequence with collision; IntegrationTests: dictionary CRUD, number generation with concurrent calls returning unique values |
| **Gate** | `G2_006_DICTIONARY_NUMBERING_READY` |
| **Recommended Agent** | MiniMax |
| **Est. Duration** | 0.5 day |
| **Unblocks** | (nothing else in G2; needed by Sales/Purchase/Inventory later) |

### G2-007 — Module Runtime (`IModule` + Registry)

| Field | Value |
|---|---|
| **Purpose** | A stub `IModule` can be plugged into the host; the registry enforces dependency order; edition packaging works. |
| **Scope** | (1) `IModule` interface per TASK D §2; (2) `IModuleRegistry` with topological sort + cycle detection; (3) `ModuleRuntimeBuilder` (extension method on `IServiceCollection`); (4) a stub `IModule` (`HelloModule : IModule`) with `GET /api/v1/hello`; (5) edition packaging via conditional `<ItemGroup>` in `.csproj`; (6) module record table + on-boot migration apply; (7) permission seeding (idempotent); (8) menu seeding (idempotent); (9) one smoke test. |
| **Reuse** | The migration runner skeleton from G2-001 / G2-002. |
| **Do Not Do** | Do NOT add real modules (Sales/Purchase/Inventory). Do NOT add dynamic DLL loading. Do NOT add edition-based tenant config (single global edition V1; per-tenant V1.5). |
| **Files** | `src/GuliERP.Module.Runtime/IModule.cs`, `src/GuliERP.Module.Runtime/IModuleRegistry.cs`, `src/GuliERP.Module.Runtime/ModuleRegistry.cs`, `src/GuliERP.Module.Runtime/ModuleRuntimeBuilder.cs`, `src/GuliERP.Host/Modules/HelloModule.cs` (stub), `src/GuliERP.Host/GuliERP.Host.csproj` (conditional `<ItemGroup>`) |
| **Tests** | UnitTests: topological sort, cycle detection, edition filter; IntegrationTests: HelloModule's `GET /api/v1/hello` returns 200 with edition=Warehouse (and 404 with edition=HelloDisabled); ArchitectureTests: M1–M9, M16, M18, M20 |
| **Gate** | `G2_007_MODULE_RUNTIME_READY` |
| **Recommended Agent** | MiniMax (the runtime is backend); TRAE-friendly because the conditional `.csproj` and edition concept are documented in the design system and could be cross-validated |
| **Est. Duration** | 1 day |
| **Unblocks** | G2-008 |
| **Risks** | Edition packaging is a `csproj` discipline; easy to break. Architecture tests are the safety net. |

### G2-008 — Frontend Shell Integration (auth + tenant + menu)

| Field | Value |
|---|---|
| **Purpose** | The R3 design system shell logs in, displays the current tenant + company, and renders the menu tree. |
| **Scope** | (1) `POST /api/v1/auth/login` integrated into the R3 shell's `ErpShell.vue` (login page → main shell); (2) JWT stored in memory (not localStorage); (3) `axios` interceptor adds `Authorization: Bearer ...` to every request; (4) `I18n` stub (zh-CN V1); (5) `X-Company-Id` switcher in the topbar; (6) menu tree fetched from `GET /api/v1/me/menus`; (7) fullscreen toggle preserved; (8) one E2E test (Playwright) that logs in and sees the menu. |
| **Reuse** | The R3 design system tokens and components (per `G1B1R3_DESIGN_SYSTEM_STATIC_REVIEW.md`); the G2-003 auth endpoints. |
| **Do Not Do** | Do NOT modify the R3 design system (visual layer). Do NOT add business pages. Do NOT add low-code engine. |
| **Files** | `apps/web/src/views/auth/Login.vue` (new), `apps/web/src/layouts/ErpShell.vue` (modify: wire auth), `apps/web/src/api/client.ts` (axios instance), `apps/web/src/stores/auth.ts` (Pinia), `apps/web/src/stores/menu.ts` (Pinia) |
| **Tests** | E2E: Playwright login → menu tree appears; unit: `auth.ts` token refresh logic |
| **Gate** | `G2_008_FRONTEND_SHELL_READY` — flipped by Operator after manually logging in to the R3 shell against the G2-003 backend |
| **Recommended Agent** | **TRAE** (this is frontend work; TRAE owns `apps/web/`) |
| **Est. Duration** | 1.5 day |
| **Unblocks** | The first business module (Sales) can now plug in |
| **Risks** | If the G2-003 backend is not stable, this goal will thrash. Run G2-008 in parallel with G2-004 (both depend on G2-003). |

### G2-009 — IApprovalService V1 (simple approval)

| Field | Value |
|---|---|
| **Purpose** | The V1 simple approval capability is available for business modules to use. |
| **Scope** | (1) `approval_history` table per TASK G §4; (2) `IApprovalService` interface per TASK G §3; (3) `ApprovalService` impl (5 methods, ~300 lines); (4) audit integration (every state transition writes to `audit_entry`); (5) permission integration (Submit/Approve/Reject/Withdraw all check the right permission); (6) self-approval forbidden; (7) one smoke test. |
| **Reuse** | The `audit_entry` from G2-005, the `users` from G2-002, the `IPermissionService` from G2-004. |
| **Do Not Do** | Do NOT add multi-step approval. Do NOT add routing. Do NOT add notifications. Do NOT add delegation. Do NOT add a workflow engine. |
| **Files** | `src/GuliERP.Foundation.Domain/Approval/ApprovalHistory.cs`, `src/GuliERP.Foundation.Application/Approval/IApprovalService.cs`, `src/GuliERP.Foundation.Application/Approval/ApprovalHistoryEntry.cs`, `src/GuliERP.Foundation.Application/Approval/ApprovalAction.cs`, `src/GuliERP.Foundation.Infrastructure/Approval/ApprovalService.cs`, `src/GuliERP.Foundation.Infrastructure/Approval/ApprovalHistoryConfiguration.cs` |
| **Tests** | UnitTests: Submit → Approve happy path, Reject happy path, Withdraw happy path, self-approval forbidden, concurrent approvals return 409; IntegrationTests: audit entry written for each transition; ArchitectureTest: no business module references `ApprovalService` directly (only `IApprovalService`) |
| **Gate** | `G2_009_APPROVAL_READY` |
| **Recommended Agent** | MiniMax |
| **Est. Duration** | 1 day |
| **Unblocks** | The first business module that needs approval (Sales) |

### G2-010 — Foundation Runtime Acceptance

| Field | Value |
|---|---|
| **Purpose** | The Operator runs the full G2 smoke + integration suite, and the Gate is `G2_FOUNDATION_IMPLEMENTED` (only the Operator can set this; the Agent cannot). |
| **Scope** | (1) Run all 5 test suites (UnitTests, ArchitectureTests, IntegrationTests, RuntimeSmokeTests, E2ETests); (2) Verify `/healthz` 200, `/readyz` 200, `/openapi.json` valid; (3) Verify a full login → me → logout round-trip in the running host; (4) Verify edition packaging (build `Warehouse` edition, verify Sales is not in the binary); (5) Verify architecture tests (M1–M20) all pass; (6) Verify secret scan (no `password`, `*Token`, `*Secret` in source); (7) Operator manual smoke (5–10 min of clicking through the shell). |
| **Reuse** | All previous goals' evidence. |
| **Do Not Do** | Do NOT skip the Operator manual smoke. Do NOT mark the Gate green without all 5 test suites green. Do NOT add new goals during this phase. |
| **Files** | `docs/verification/G2_FOUNDATION_RUNTIME_ACCEPTANCE_REPORT.md` |
| **Tests** | All 5 suites + the Operator manual smoke |
| **Gate** | **`G2_FOUNDATION_IMPLEMENTED`** — flipped by Operator |
| **Recommended Agent** | MiniMax (compile + run tests) + Operator (manual smoke + final Gate) |
| **Est. Duration** | 0.5 day (compile + tests) + operator time |
| **Unblocks** | The first business module (Sales) is now implementable |

---

## 4. Dependency graph

```
G2-001 (Host & PG)
   ├── G2-002 (Identity)
   │      ├── G2-003 (Auth)
   │      │      ├── G2-004 (Permission)
   │      │      │      └── G2-008 (Frontend Shell)  ← TRAE
   │      │      └── G2-009 (Approval)
   │      └── G2-006 (Dictionary + Numbering)
   ├── G2-005 (Audit)
   │      └── G2-007 (Module Runtime)
   │             └── G2-008 (Frontend Shell)  ← also depends on G2-004
   └── (everything depends on G2-001)
            └── G2-010 (Acceptance)
```

A **critical path** runs: G2-001 → G2-002 → G2-003 → G2-004 → G2-008 (≈ 5 days). The other goals can be parallelized where dependencies allow.

---

## 5. Parallelization matrix

| Slot 1 (MiniMax) | Slot 2 (MiniMax) | Slot 3 (TRAE) |
|---|---|---|
| G2-001 | — | — |
| G2-002 (after G2-001) | G2-005 (after G2-001) | — |
| G2-003 (after G2-002) | G2-006 (after G2-002) | — |
| G2-004 (after G2-003) | G2-007 (after G2-001, G2-005) | — |
| G2-008 prep: G2-009 (after G2-003) | — | G2-008 (after G2-003 + G2-004) |
| G2-010 (after all) | — | — |

When Codex becomes available, it can take G2-005 + G2-007 in parallel with the others (both are pure backend with clear contracts).

---

## 6. MiniMax / TRAE / Codex allocation

| Goal | Recommended | Why |
|---|---|---|
| G2-001 | MiniMax | Backend, .NET, no UI |
| G2-002 | MiniMax | Backend, .NET, no UI |
| G2-003 | MiniMax | Backend, .NET, no UI |
| G2-004 | MiniMax | Backend, .NET, no UI |
| G2-005 | MiniMax or Codex (when available) | Backend, .NET |
| G2-006 | MiniMax | Backend, .NET |
| G2-007 | MiniMax | Backend, .NET, but TRAE can review the conditional `.csproj` |
| G2-008 | TRAE | Frontend, Vue 3, `apps/web/**` (TRAE's workspace) |
| G2-009 | MiniMax | Backend, .NET |
| G2-010 | MiniMax (compile + tests) + Operator (manual) | Verification |

**Codex-when-available**: any of G2-005, G2-006, G2-007. All three are pure backend with clear contracts; Codex's strength is exactly that profile.

---

## 7. What this plan is NOT

- ❌ Not a promise of dates. Realistic duration depends on Codex availability, operator review pace, and rework.
- ❌ Not a commitment to skip any goal. Each Gate is **non-skippable**.
- ❌ Not a commitment to start the first business module (Sales) before G2-010 is green. The brief is clear: **Foundation first, then Sales**.
- ❌ Not a UI plan. G2-008 is the only UI-touching goal; it is bounded to auth + tenant + menu.

---

## 8. Open questions for Operator

| # | Question | Default if unanswered |
|---|---|---|
| Q1 | Is the Operator willing to provide a real PostgreSQL instance (or a docker-compose file) for G2-001? | Yes (or fall back to Testcontainers in CI + sqlite for local dev) |
| Q2 | Is the Operator willing to provide a real SMTP server for future notification? | Out of V1 scope; defer |
| Q3 | Is the Operator willing to run the manual smoke (G2-010) in 10 minutes? | Yes |
| Q4 | Is there a deadline for "first business module (Sales) starts"? | No — Foundation must be 100% green first |
| Q5 | Should the conditional `<ItemGroup>` edition be per-build-flag or per-environment? | Per-build-flag (`GULIERP_EDITION=Warehouse`); the binary is the truth |

---

## 9. Stage 0 deliverable (before any G2 goal starts)

Before G2-001, the Operator (or MiniMax) must ensure:

- [ ] `.NET 10 SDK` installed (see TASK I)
- [ ] `PostgreSQL 16` reachable (local docker or remote)
- [ ] `dotnet user-secrets` initialized for `GuliERP.Host` (dev only)
- [ ] `appsettings.Development.json` does NOT contain any real password
- [ ] `git` baseline clean (per META_GULI)
- [ ] Architecture test project skeleton created (`GuliERP.Foundation.ArchitectureTests`)
- [ ] The current `apps/web` R3 design system builds and runs (TRAE has confirmed)

---

## 10. Summary: the 10 sub-goals at a glance

| # | Goal | MiniMax | TRAE | Codex-when | Gate |
|---|---|---|---|---|---|
| G2-001 | Host & PostgreSQL | ✅ | | | `G2_001_HOST_AND_POSTGRES_READY` |
| G2-002 | Identity | ✅ | | | `G2_002_IDENTITY_READY` |
| G2-003 | Auth (Argon2id + JWT) | ✅ | | | `G2_003_AUTH_READY` |
| G2-004 | API Permission | ✅ | | | `G2_004_PERMISSION_READY` |
| G2-005 | Audit | ✅ | | ✅ | `G2_005_AUDIT_READY` |
| G2-006 | Dictionary + Numbering | ✅ | | ✅ | `G2_006_DICTIONARY_NUMBERING_READY` |
| G2-007 | Module Runtime | ✅ | review | ✅ | `G2_007_MODULE_RUNTIME_READY` |
| G2-008 | Frontend Shell Integration | | ✅ | | `G2_008_FRONTEND_SHELL_READY` |
| G2-009 | IApprovalService V1 | ✅ | | | `G2_009_APPROVAL_READY` |
| G2-010 | Foundation Runtime Acceptance | ✅ + Operator | | | `G2_FOUNDATION_IMPLEMENTED` (Operator only) |

**After G2-010, GuliERP has a real, runnable, testable, modular, multi-tenant Foundation.** The first business module (Sales) is then implementable as its own G3 goal.

---

*End of G2 Foundation Execution Plan — Status: DRAFT. Companion: TASK B–G architecture drafts.*
