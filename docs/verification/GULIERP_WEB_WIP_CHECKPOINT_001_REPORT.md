# GULIERP_WEB_WIP_CHECKPOINT_001_REPORT

| Field | Value |
|---|---|
| Report date | 2026-08-21 13:45 +08:00 |
| Start HEAD | `45a07577ec3110d7f499946f0ef591e5e2ade254` |
| End HEAD | `0db2c43` |
| Branch | `master` |
| Commits created | 2 |
| Commit 3 status | **CANCELLED** — all config files already in commits 1+2; no independent content remaining |

---

## 1. Start & End HEAD

| | SHA | Message |
|---|---|---|
| Start | `45a0757` | feat(document-kernel): implement business document numbering foundation |
| End | `0db2c43` | feat(web): checkpoint sales order mock UX baseline |

## 2. Start & End git status

### Start status (before checkpoint)

**Modified (16 files):**
- `.gitignore`
- `apps/web/index.html`, `apps/web/package.json`, `apps/web/src/App.vue`, `apps/web/src/main.ts`, `apps/web/src/router.ts`, `apps/web/tsconfig.json`, `apps/web/vite.config.ts`
- `modules/foundation/.../ErrorCodes.cs`
- `modules/identity/.../Exceptions.cs`, `AuthenticationExceptionHandler.cs`, `AuthenticationService.cs`
- `tools/GuliERP.Identity.Bootstrap/Program.cs`
- `tools/dev/diagnose-operator-user.ps1`, `g2-004-operator-evidence.ps1`, `provision-web-preview-user.ps1`

**Untracked (~50+ files):**
- `apps/web/package-lock.json`, `apps/web/src/api/`, `apps/web/src/components/LookupDialog.vue`, `apps/web/src/mock/sales-order.ts`, `apps/web/src/router/auth.ts`, `apps/web/src/stores/auth.ts`, `apps/web/src/stores/csrf.ts`, `apps/web/src/stores/sales-order.ts`, `apps/web/src/types/auth.ts`, `apps/web/src/types/sales-order.ts`, `apps/web/src/utils/`, `apps/web/src/views/auth/`, `apps/web/src/views/sales-order/`, `apps/web/src/vite-env.d.ts`, `apps/web/tsconfig.tsbuildinfo`
- `data/`, `docs/architecture/G2_*.md`, `docs/audit/`, `docs/goals/`, `docs/governance/`, `docs/review/`, `docs/verification/`, `tools/dev/probe-backend.ps1`, `tools/dev/run-web-preview-backend.ps1`, `tools/discovery/`, test results

**Staged at start:** 0 (no HARD STOP needed)

### End status (after checkpoint)

**Modified (9 files — all non-frontend):**
- `.gitignore`
- `modules/foundation/.../ErrorCodes.cs`
- `modules/identity/.../Exceptions.cs`, `AuthenticationExceptionHandler.cs`, `AuthenticationService.cs`
- `tools/GuliERP.Identity.Bootstrap/Program.cs`
- `tools/dev/diagnose-operator-user.ps1`, `g2-004-operator-evidence.ps1`, `provision-web-preview-user.ps1`

**Untracked apps/web:** `apps/web/tsconfig.tsbuildinfo` (build artifact, excluded)
**Untracked non-frontend:** `data/`, `docs/*`, `tools/*`, test results — all preserved

## 3. Commit List

| # | SHA | Message | Files |
|---|---|---|---|
| 1 | `24c65ea` | feat(web): checkpoint authenticated SPA infrastructure | 14 |
| 2 | `0db2c43` | feat(web): checkpoint sales order mock UX baseline | 11 |
| 3 | — | CANCELLED: no independent content remaining | — |

## 4. Commit File Inventory

### Commit 1: `24c65ea` — Auth & HTTP Infrastructure (14 files, 3196 insertions)

**New files (10):**
| File | Purpose |
|---|---|
| `apps/web/src/api/auth.ts` | Auth API client (login, logout, me, company switch) |
| `apps/web/src/api/http.ts` | Fetch wrapper: CSRF, cookie auth, RFC 7807, retry |
| `apps/web/src/router/auth.ts` | Auth routes (login, 403) + no-flash guard |
| `apps/web/src/stores/auth.ts` | Auth Pinia store: bootstrap, signIn, signOut, listener |
| `apps/web/src/stores/csrf.ts` | CSRF token store: fetch + cache from /api/v1/auth/csrf |
| `apps/web/src/types/auth.ts` | Auth domain types: AuthState, AuthUser, ApiError |
| `apps/web/src/views/auth/Login.vue` | Login page with multi-path login + URL token injection |
| `apps/web/src/views/auth/Forbidden403.vue` | 403 forbidden page |
| `apps/web/src/vite-env.d.ts` | Vite client type declarations |
| `apps/web/package-lock.json` | npm lock file for reproducible installs |

**Modified files (4):**
| File | Changes |
|---|---|
| `apps/web/index.html` | HTML5 boilerplate (was bare div) |
| `apps/web/package.json` | Added @element-plus/icons-vue dependency |
| `apps/web/tsconfig.json` | Added vite/client types, skipLibCheck, esModuleInterop, .d.ts include |
| `apps/web/vite.config.ts` | Added API proxy to http://127.0.0.1:5000 |

### Commit 2: `0db2c43` — Sales Order Mock UX + Shell Integration (11 files, 4834 insertions)

**New files (8):**
| File | Lines | Purpose |
|---|---|---|
| `apps/web/src/views/sales-order/SalesOrderList.vue` | 579 | List page: search, filters, batch ops, column settings, saved views |
| `apps/web/src/views/sales-order/SalesOrderEdit.vue` | 1925 | Create/Edit: header form, editable line table, workflow actions |
| `apps/web/src/views/sales-order/SalesOrderDetail.vue` | 853 | Read-only detail view |
| `apps/web/src/stores/sales-order.ts` | 148 | Pinia mock store: CRUD + 3D status workflow |
| `apps/web/src/types/sales-order.ts` | 250 | Domain types: SalesOrder, SalesOrderLine, status enums |
| `apps/web/src/mock/sales-order.ts` | 428 | Seed data + recomputeLine/recomputeHeader logic |
| `apps/web/src/utils/status.ts` | 79 | Status maps (3D) + fmtMoney/fmtDate/fmtDateTime |
| `apps/web/src/components/LookupDialog.vue` | 366 | Customer/item/warehouse lookup dialog (mock data) |

**Modified files (3):**
| File | Changes |
|---|---|
| `apps/web/src/App.vue` | Auth bootstrap splash, localized error display, Suspense |
| `apps/web/src/main.ts` | Pinia + Router + Element Plus registration, auth.bootstrap() |
| `apps/web/src/router.ts` | Route wiring: ErpShell, sales order, auth, MDM |

## 5. Excluded Files & Reasons

| File | Reason |
|---|---|
| `apps/web/tsconfig.tsbuildinfo` | Build artifact — should not be version-controlled |
| `.gitignore` | Not frontend — excluded per scope (apps/web/** only) |
| `modules/**/*.cs` (4 files) | Backend — excluded per scope |
| `tools/**/*.ps1` (4 files) | Tools — excluded per scope |
| `data/`, `docs/*`, `tools/discovery/*` | Non-frontend — excluded per scope |

## 6. Verification Results

| Check | Command | Result |
|---|---|---|
| Typecheck (pre-commit) | `npx vue-tsc --noEmit` | PASS (0 errors) |
| Build (pre-commit) | `npx vite build` | PASS (7.01s) |
| git diff --check (pre-commit) | `git diff --check -- apps/web/` | PASS (CRLF warnings only) |
| Typecheck (post-commit) | `npx vue-tsc --noEmit` | PASS (0 errors) |
| Build (post-commit) | `npx vite build` | PASS (6.96s) |
| git diff --check (post-commit) | `git diff --check -- apps/web/` | PASS (no errors) |

## 7. Status Accuracy

### Sales Order
- **Status:** MOCK UX BASELINE — 100% in-memory mock data
- **Real API:** NO — no `api/salesOrder.ts` file exists
- **Mock store:** `stores/sales-order.ts` imports from `mock/sales-order.ts`
- **Document numbers:** Mock-generated by `nextSalesOrderNo()` — must be replaced by backend DocumentKernel
- **Amount calculation:** Frontend preview only — backend must recalculate on save

### Auth
- **Status:** VERIFIED — real API integration
- **Endpoints:** `/api/v1/auth/csrf`, `/api/v1/auth/login`, `/api/v1/auth/me`, `/api/v1/auth/logout`
- **Cookie auth:** YES — `credentials: 'include'` in fetch
- **CSRF:** YES — token fetched from `/api/v1/auth/csrf`, injected as `X-CSRF-TOKEN` header
- **RFC 7807:** YES — `ApiError` class with code, title, detail, requestId, traceId
- **localStorage tokens:** NO — cookie-based only
- **401 handling:** YES — no-flash redirect to /login via auth event listener

### MDM Pages
- **Status:** MOCK — pages committed in `2176628` using mock data from `mock/mdm.ts`
- **Backend API:** EXISTS (per handoff doc) but gate is `MDM_001_CODE_READY_OPERATOR_EVIDENCE_PENDING`
- **Frontend wiring:** NOT wired to real API — pages use mock data
- **Operator evidence:** NOT YET RUN — must run `tools/dev/mdm-001-operator-evidence.ps1`

### Shell & Left Sidebar
- **Status:** PARTIALLY COMPLETE
- **Committed in:** `2176628` (ErpShell.vue, tabs.ts, navigation.css)
- **Sidebar width:** 160px default, 136-220px range, resizable, collapsible
- **Menu items:** 9 modules, 3 with secondary menus, 4 items have routing
- **Hardcoded:** YES — menu items are hardcoded in ErpShell.vue, not config-driven
- **Permission filtering:** NO — all items visible
- **Multi-Tab:** Working — close current/left/right/others/all, right-click + more dropdown

## 8. Backend Files Touched

**NO.** Zero backend files were modified, staged, or committed in this checkpoint. All commits contain only `apps/web/**` files.

## 9. WIP Loss Check

**NO WIP LOST.** All pre-existing modified and untracked files remain in the working tree. The 9 remaining modified files and ~30+ untracked files/directories are unchanged.

## 10. Remaining Dirty Files

| Category | Count | Examples |
|---|---|---|
| Modified (backend) | 4 | ErrorCodes.cs, Exceptions.cs, AuthenticationExceptionHandler.cs, AuthenticationService.cs |
| Modified (tools) | 4 | Program.cs, diagnose-operator-user.ps1, g2-004-evidence.ps1, provision-web-preview-user.ps1 |
| Modified (root) | 1 | .gitignore |
| Untracked (build artifact) | 1 | tsconfig.tsbuildinfo |
| Untracked (docs) | ~20 | docs/architecture/, docs/audit/, docs/goals/, docs/governance/, docs/review/, docs/verification/ |
| Untracked (data/tools) | ~10 | data/, tools/dev/, tools/discovery/ |
| Untracked (test results) | ~6 | tests/*/TestResults/ |

## 11. Recommended Next MiniMax Task

**MDM-001 Operator Evidence** — run `tools/dev/mdm-001-operator-evidence.ps1` to upgrade gate from `CODE_READY_OPERATOR_EVIDENCE_PENDING` to `MDM_001_REAL_MASTER_DATA_VERIFIED`. This does NOT require any frontend changes and can proceed in the same working tree.

## 12. BLOCKED / HARD STOP Items

**NONE.** No hard stops were encountered during this checkpoint.

## 13. Final Gate Recommendation

**GULIERP_WEB_WIP_CHECKPOINT_001_COMPLETED**

- 2 atomic commits created (auth infra + sales order mock UX)
- 0 backend files touched
- 0 WIP lost
- Typecheck + Build PASS
- Mock vs real API boundary clearly documented in commit messages
- Remaining dirty files are all non-frontend, preserved for other agents
