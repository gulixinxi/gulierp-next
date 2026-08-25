# GuliERP Greenfield Risk Register V1

| Field | Value |
|---|---|
| Goal | Capture, score, and assign mitigation for the top greenfield risks |
| Entry Gate | `GULIERP_CORE_BUSINESS_SPEC_FROZEN` |
| Output Status | **DRAFT — for review only** |
| Author | Mavis (single writer, governance role) |
| Companion docs | `META_GULI_GOVERNANCE_V1.md` (HR-1..HR-10), `GULIERP_MODULE_INDEPENDENCE_RULE.md` (DEC-MODULE-001), all G2 architecture drafts |
| Update cadence | At every Gate transition; at minimum quarterly |
| Owner | The Operator (PM-level), with named owners per risk |

---

## 1. Mission

A greenfield project has a **unique risk profile**: no prior code constrains decisions, but every decision is irreversible in the sense that it becomes the foundation. This register captures the **top 12 risks** that could derail the GuliERP greenfield initiative, ranks them by likelihood × impact, names a mitigation, and defines a **hard stop** (a condition under which work must halt until the risk is reduced).

**Use of this document**: at any decision point (a Gate flip, a new goal, a new module), the relevant risks are re-read. If a risk's likelihood has materially increased, the mitigation is re-applied. If a risk's **hard stop** has been triggered, work halts.

---

## 2. Scoring rubric

| Likelihood | Meaning |
|---|---|
| Low (L) | < 20% — would require unusual combination of events |
| Medium (M) | 20-60% — has occurred in similar projects, or one trigger is already visible |
| High (H) | > 60% — has occurred in this project, or two or more triggers are visible |

| Impact | Meaning |
|---|---|
| Low (I) | Localized; one goal delayed by ≤ 1 day; no customer-visible damage |
| Medium (M) | Cross-goal; one phase delayed by ≤ 1 week; recoverable with rework |
| High (H) | Project-level; one quarter of work wasted; or a customer-visible bug that takes ≥ 1 month to remediate |
| Critical (C) | Tenant data leak, security breach, regulatory violation, or project-fatal |

**Risk score** = Likelihood × Impact. We list the top 12 in order of score, then by ID for stable reference.

---

## 3. The 12 risks

### R1 — AI-generated tests pass green but business logic is wrong (per META_GULI HR-1, HR-2)

| Field | Value |
|---|---|
| **Likelihood** | **High** — Codex-style agents love to write tests that pass against their own implementation; the bug is that the implementation is wrong, not the test |
| **Impact** | High — a sales order that "passes 200 tests" but computes the wrong tax is a financial loss + a credibility loss |
| **Detection** | Architecture review (does the test exercise the real business invariant?), Operator walkthrough (does it match the FROZEN spec?), 3rd-party code review (do unrelated reviewers agree?) |
| **Mitigation** | (a) Every test must reference a frozen business spec section (e.g. `// SPEC: SALES_ORDER_BUSINESS_SPEC_V1 §5.2`); (b) Operator manual smoke at every Gate; (c) **architecture tests are first-class citizens** (NetArchTest rules M1–M20); (d) FROZEN specs cannot be edited without Operator approval; (e) Independent Reviewer (this author) re-reads every Gate-flipping PR |
| **Hard Stop** | If at any Gate the Operator asks "can you just verify this passes CI?" without reading the test code → STOP. The test must be human-readable and human-traceable. |
| **Owner** | Operator (test-traceability policy) + Mavis (architecture tests) + 3rd-party reviewer (independent review) |

### R2 — UX implementation starts before `SALES_ORDER_UX_APPROVED` (per META_GULI HR-4)

| Field | Value |
|---|---|
| **Likelihood** | **Medium** — natural pressure to "just build it"; the current status is `SALES_ORDER_UX_READY_FOR_OPERATOR_REVIEW` (per `G1B1_SALESORDER_UX_PROTOTYPE_REPORT.md`) |
| **Impact** | High — every hour of implementation on un-approved UX is throwaway when the Operator changes their mind |
| **Detection** | Goal registry: a goal with "implement SalesOrder" before `SALES_ORDER_UX_APPROVED` is a red flag; code review of new `.vue` files in `apps/web/src/views/sales-order/` before the UX is approved |
| **Mitigation** | (a) `SALES_ORDER_UX_APPROVED` is **reserved for the Operator only** — no Agent (including Mavis) may set this Gate; (b) the goal registry's `G2_FOUNDATION_EXECUTION_PLAN.md` explicitly says "Foundation first, then Sales"; (c) any new `apps/web/**` file is reviewed for UX-spec adherence |
| **Hard Stop** | If a PR adds a SalesOrder `.vue` file before `SALES_ORDER_UX_APPROVED`, reject the PR and freeze the file. |
| **Owner** | Operator (Gate policy) + TRAE (UI implementation discipline) |

### R3 — Agents invent their own business rules (per META_GULI LESSON-001)

| Field | Value |
|---|---|
| **Likelihood** | **High** — every agent (MiniMax / TRAE / Codex) will be tempted to "fill in the gap" when a spec is silent; the FROZEN specs are extensive but not exhaustive |
| **Impact** | High — a business rule invented by an agent is **not validated by the Operator** and may contradict the FROZEN spec or the customer's actual workflow |
| **Detection** | Code review comparing implementation to FROZEN spec; PR comments asking "where in the spec does this come from?"; a new rule appears in code without a corresponding spec update |
| **Mitigation** | (a) **No silent gap-filling**: when a spec is silent, the agent writes an `OPEN_QUESTION` doc, not code; (b) the FROZEN specs are the **only source of business truth** (per `G1A_DECISIONS_V1`); (c) META_GULI LESSON-001 explicitly bans this; (d) every "new rule" PR must be rejected unless it points to a spec section |
| **Hard Stop** | If an agent implements a business rule not in the FROZEN spec, the code is reverted and the agent is re-prompted. |
| **Owner** | All agents (discipline) + Mavis (this author, catches it in review) + Operator (final word) |

### R4 — Foundation scope creep (the "while we're at it" trap)

| Field | Value |
|---|---|
| **Likelihood** | **High** — every goal is a temptation to "just add" one more thing; the Foundation is the substrate, but "while we're at it, let me add IAM SSO / MFA / field-policy / row-policy" is the path to a 6-month Foundation |
| **Impact** | High — Foundation delays every business module by the same amount; Sales cannot start until Foundation is green |
| **Detection** | PR review: any change to `GuliERP.Foundation.*` that adds a new public interface not in `G2_FOUNDATION_ARCHITECTURE_V1_DRAFT.md` is a red flag; the weekly "scope check" reviews the PR list against the plan |
| **Mitigation** | (a) The Foundation's scope is **frozen** by `G2_FOUNDATION_ARCHITECTURE_V1_DRAFT.md`; (b) `INTERFACE_NOW_IMPLEMENT_LATER` items have a stub that returns the safe default; (c) any new Foundation PR requires a corresponding update to the architecture draft (a "scope ticket"); (d) the Operator can defer a feature with "V1.5" and the architecture draft is the single source of truth |
| **Hard Stop** | If a goal's PR list exceeds 2× the goal's estimated file count, the goal is split. |
| **Owner** | Mavis (architecture draft owner) + Operator (Gate policy) |

### R5 — Low-code generic engine temptation (the "Admin.NET reflection" trap)

| Field | Value |
|---|---|
| **Likelihood** | **Medium-High** — the old project (`D:\guli\gulierp`) was a low-code generic engine; an agent that has seen the old code may unconsciously mirror it |
| **Impact** | High — a generic engine cannot express 3D status, reservation, posting, or approval limits; the moment we need a non-trivial rule, the abstraction breaks and we have a worse codebase than if we had no engine |
| **Detection** | Code review: presence of `Reflection`, `Type.GetProperty`, `Activator.CreateInstance`, `Expression.Compile`, `dynamic`, or any "entity framework over reflection" pattern in business code; architecture test that fails any of these |
| **Mitigation** | (a) `DEC-MODULE-001` FROZEN rule explicitly bans dynamic DLL / MEF / MAF; (b) the architecture draft (TASK D) lists this anti-pattern in §12; (c) any PR introducing these patterns is rejected on sight; (d) the Operator reads the "Generic Low-Code Temptation" check at every Gate |
| **Hard Stop** | If a `dynamic` or `Reflection.Emit` line appears in production code, the PR is rejected and the author re-trained on the architecture draft. |
| **Owner** | All agents (discipline) + Mavis (architecture review) + Operator (Gate) |

### R6 — Module coupling (the "Sales reaches into Inventory" trap)

| Field | Value |
|---|---|
| **Likelihood** | **High** — it is **so much easier** to `using GuliERP.Inventory.Domain.Item` from Sales than to call `IItemQuery`; the temptation is constant |
| **Impact** | High — direct coupling makes modules inseparable; edition packaging breaks; migrations become impossible; "Inventory" must always be deployed when "Sales" is |
| **Detection** | Architecture test M3, M6, M7, M8, M9 (per TASK D §10); code review of `using` statements in `GuliERP.Sales.*` |
| **Mitigation** | (a) `DEC-MODULE-001` FROZEN rule + 9 architecture tests; (b) cross-module data goes **only** through `Application.Contracts`; (c) the Operator runs the architecture test suite at every Gate; (d) any new cross-module reference is a red flag |
| **Hard Stop** | If the architecture test suite fails, the build fails. There is no "we'll fix it next sprint". |
| **Owner** | Mavis (architecture tests) + Operator (Gate) |

### R7 — Security deferred too far ("we'll add auth in V1.5")

| Field | Value |
|---|---|
| **Likelihood** | **Medium** — natural pressure to ship a Foundation "without auth" so we can move faster; every dev wants to defer security to "later" |
| **Impact** | Critical — a Foundation without auth is a foundation that leaks; once data is in, retrofitting auth is a 6-month project |
| **Detection** | The G2 execution plan has auth in G2-003 (the 3rd goal); any reordering that defers auth past G2-005 is a red flag; review of `GuliERP.Foundation.Host/Program.cs` for `[Authorize]` on every business endpoint |
| **Mitigation** | (a) `G2_SECURITY_ARCHITECTURE_V1_DRAFT.md` (TASK C) defines what ships in G2-003 and what is deferred to V1.5+; (b) `META_GULI HR-4` says "no real auth before UX approval", but **for the backend**, auth must be in the substrate, not bolted on; (c) the G2-003 Gate is non-skippable; (d) every business endpoint has `[Authorize]` + `[RequirePermission]` from day one |
| **Hard Stop** | If a business endpoint exists without `[Authorize]`, it is a Critical-severity bug. The PR is reverted. |
| **Owner** | Mavis (security draft owner) + Operator (Gate) |

### R8 — Inventory ledger correctness (the "negative stock" trap)

| Field | Value |
|---|---|
| **Likelihood** | **High** — Inventory is the most failure-prone module in any ERP; off-by-one, race conditions, double-posting, and lot/serial mistakes are endemic |
| **Impact** | Critical — a wrong inventory number is a financial loss; "we have 100 units in stock but actually have 0" means we shipped 100 units we don't have |
| **Detection** | (a) `DEC-INV-002` (FROZEN): `InventoryPostingEngine` is REQUIRED in V1; (b) negative stock is **strict default off**; (c) lot per-item flag; (d) integration tests that exercise the 4 inventory operations (stock-in, stock-out, transfer, adjustment) under concurrency |
| **Mitigation** | (a) `InventoryPostingEngine` is a **mandatory boundary** in the Inventory module; (b) every mutation goes through the engine, not direct SQL; (c) the engine is unit-tested for the 12 critical scenarios; (d) the engine emits an audit entry per posting; (e) reconciliation report is run daily; (f) the Operator's "10-minute test" includes "post a stock-out that should be allowed" and "post a stock-out that should be blocked" |
| **Hard Stop** | If a posting skips the engine (direct SQL, direct repo write), it is a Critical bug. The PR is reverted. |
| **Owner** | Inventory module owner (TBD) + Mavis (architecture review) + Operator (Gate) |

### R9 — Concurrent Agent file collision (two agents edit the same file)

| Field | Value |
|---|---|
| **Likelihood** | **Medium** — even with the "single writer" rule, two agents may concurrently produce drafts that both touch the same `docs/` file |
| **Impact** | Medium — recoverable (re-merge), but wastes time and risks losing the better version |
| **Detection** | `git status` before commit shows unexpected diff; the agent's pre-commit step runs `git diff --check`; PR review catches it |
| **Mitigation** | (a) **Single writer pattern**: this report's author is the only one who writes final docs; (b) max 6 parallel agents, all **read-only** research; (c) the writer merges in priority order (this author); (d) any agent that wants to write to `docs/**` must declare the file path in its prompt; (e) if two agents claim the same file, the earlier one wins; (f) the Operator resolves disputes |
| **Hard Stop** | If a PR has merge conflicts because two agents edited the same file, the writer (this author) re-merges manually; the lower-priority agent's output is discarded. |
| **Owner** | Mavis (writer) + Operator (escalation) |

### R10 — Git baseline missing (the "we don't have a clean starting point" trap)

| Field | Value |
|---|---|
| **Likelihood** | **Low** — `G2_FOUNDATION_EXECUTION_PLAN.md` § 9 calls for a clean `git status` as the Stage 0 contract; the current G0 state is "untracked docs/" |
| **Impact** | Medium — without a clean baseline, every commit is suspect; rollbacks are hard; CI cannot gate on "diff vs main" |
| **Detection** | `git status` shows uncommitted changes at the start of any goal; CI fails on dirty tree |
| **Mitigation** | (a) Stage 0: an atomic commit "docs(architecture): prepare G2 foundation architecture, security, runtime, DB, API, approval, environment, risk register, TRAE R3 review" lands the G2 preparation as a single commit; (b) CI gates on `git status --porcelain` being empty; (c) **no** `push` / `tag` / `rebase` / `reset --hard` in V1; (d) every goal starts with a clean tree |
| **Hard Stop** | If `git status` shows dirty tree at the start of a goal, the goal is delayed by 1 day to commit. |
| **Owner** | Mavis (this author, Stage 0 commit) + Operator (CI policy) |

### R11 — DEV technical debt contamination (the "copy-paste from old project" trap)

| Field | Value |
|---|---|
| **Likelihood** | **High** — `D:\guli\gulierp` has 1+ year of code with known anti-patterns (0 FK, text-name relations, dynamic SQL, weak constraints); an agent that has read it may "borrow" the wrong patterns |
| **Impact** | High — the new project becomes a slightly-cleaner copy of the old; the architectural improvements are lost |
| **Detection** | Code review comparing new code to `D:\guli\gulierp` patterns; the presence of `0` foreign keys, `text` PKs, `SqlSugar`, `Furion`, `Admin.NET` in new code |
| **Mitigation** | (a) The old project is **read-only**; we do not copy code, only learn from its anti-patterns; (b) `G2_POSTGRESQL_ENGINEERING_STANDARD_V1_DRAFT.md` (TASK E) §20 explicitly lists the DEV anti-patterns and bans them; (c) the architecture test M12 bans Admin.NET / Furion / SqlSugar / Identity; (d) the Operator's gate check explicitly looks for "did this PR borrow a DEV pattern?"; (e) `G2_FOUNDATION_MINIMUM_SCOPE_DISCOVERY.md` already enumerates the lessons |
| **Hard Stop** | If a new file contains `using SqlSugar` or `using Furion` or `using Admin.NET`, the PR is rejected on sight. |
| **Owner** | Mavis (architecture review) + Operator (Gate) |

### R12 — UI Design drift (the "R4 quietly changes the rules" trap)

| Field | Value |
|---|---|
| **Likelihood** | **Medium** — TRAE will continue iterating on the R3 design system; an R4 may introduce a color or spacing that is not in the token set, slowly eroding the source-of-truth discipline |
| **Impact** | Medium — UI drift is slow but cumulative; the day we want to do a brand refresh, we discover 200 ad-hoc colors |
| **Detection** | The `G1B1R3_DESIGN_SYSTEM_STATIC_REVIEW.md` (TASK A) lists 5 raw-hex findings as **LOW**; an R4 review should not increase the count; a CI linter can grep for hex codes outside `apps/web/src/design-system/tokens/*.css` |
| **Mitigation** | (a) The R3 design system review is a **standing review** for every R4+ change; (b) the architecture test (future) lints for `#[0-9a-fA-F]{3,8}` outside the tokens folder; (c) the Operator's Gate includes a "design system hygiene" check; (d) the 5 R3 findings are tracked as a backlog (deferred to R4 cleanup, not blocking) |
| **Hard Stop** | If a `R{n+1}` review finds more than the 5 R3 raw-hex findings, the design system is considered drifting and the R{n+1} is paused. |
| **Owner** | TRAE (UI discipline) + Mavis (this author, periodic R{n} review) + Operator (Gate) |

---

## 4. Risk register (sorted by score)

| # | Risk | L | I | Score | Owner | Hard stop |
|---|---|---|---|---|---|---|
| R1 | AI tests pass but logic is wrong | H | H | 16 | All | Reject test if not human-traceable |
| R3 | Agents invent business rules | H | H | 16 | All | Revert + re-prompt |
| R4 | Foundation scope creep | H | H | 16 | Mavis + Operator | Split goal |
| R5 | Low-code engine temptation | M-H | H | 12 | All | Reject `dynamic`/`Reflection` |
| R6 | Module coupling | H | H | 12 | Mavis + Operator | Build fails |
| R7 | Security deferred too far | M | C | 12 | Mavis + Operator | Revert unauth endpoint |
| R8 | Inventory ledger correctness | H | C | 12 | Inventory owner | Revert non-engine write |
| R11 | DEV tech debt contamination | H | H | 12 | Mavis + Operator | Reject SqlSugar/Furion |
| R2 | UX impl before approved | M | H | 8 | Operator + TRAE | Reject PR + freeze file |
| R9 | Concurrent Agent collision | M | M | 4 | Mavis + Operator | Re-merge, discard lower |
| R10 | Git baseline missing | L | M | 2 | Mavis + Operator | Delay goal 1 day |
| R12 | UI design drift | M | M | 4 | TRAE + Mavis | Pause R{n+1} if findings grow |

(Scores are notional; the important thing is the relative ordering and the hard stops.)

---

## 5. META_GULI alignment

Every risk in this register maps to one or more META_GULI rules:

| META_GULI rule | Risks |
|---|---|
| HR-1 (no raw stack) | R1, R7 |
| HR-2 (unified error) | R1 |
| HR-3 (correlation id) | R1, R7 |
| HR-4 (no real auth before UX approval — for the application's *internal* auth, not the login UI) | R2, R7 |
| HR-7 (audit) | R1, R8 |
| HR-9 (no secrets in logs) | R7, R11 |
| LESSON-001 (no silent gap-filling) | R3, R5 |

A new META_GULI rule is a chance to add a risk; a resolved risk may retire a META_GULI rule. The two documents evolve together.

---

## 6. Detection signals (per risk, listed as code/CI/review patterns)

| Risk | Detection signal |
|---|---|
| R1 | Every test PR has a `// SPEC: <doc> §<x>` comment. CI fails if missing. |
| R2 | `git diff` shows a new `apps/web/src/views/sales-order/*.vue` before `SALES_ORDER_UX_APPROVED` |
| R3 | A new business rule appears in code without a corresponding spec update (CI compares PR diff to `docs/product/specs/**` PR diff) |
| R4 | PR count for a goal > 2× estimate |
| R5 | Grep for `Reflection`, `Activator.CreateInstance`, `dynamic` in `src/GuliERP.*/Application/**` |
| R6 | NetArchTest M3, M6, M7, M8, M9 fail |
| R7 | `[AllowAnonymous]` on a non-auth endpoint (CI lint) |
| R8 | Any `INSERT INTO inventory_*` outside `InventoryPostingEngine` (CI grep) |
| R9 | `git status` shows uncommitted changes from two agents in the same file |
| R10 | `git status --porcelain` is non-empty at goal start |
| R11 | `using SqlSugar` / `using Furion` / `using Admin.NET` anywhere (CI grep) |
| R12 | Hex color literals in `apps/web/src/**/*.vue` or `apps/web/src/**/*.css` outside `design-system/tokens/` (CI grep) |

**Each signal is a CI gate. A signal that fires is a Goal-blocker, not a warning.**

---

## 7. Mitigation cost (rough estimate)

| Risk | Cost to mitigate | Cost of NOT mitigating |
|---|---|---|
| R1 | Low (test discipline) | Critical (financial loss) |
| R2 | Low (Gate policy) | Medium (throwaway code) |
| R3 | Low (rule adherence) | High (rewrites) |
| R4 | Low (discipline) | High (delays) |
| R5 | Low (review) | Critical (architecture rot) |
| R6 | Low (architecture tests) | Critical (edition packaging breaks) |
| R7 | Low (auth in plan) | Critical (data leak) |
| R8 | Medium (engine + tests) | Critical (financial loss) |
| R9 | Low (writer pattern) | Low (rework) |
| R10 | Low (commit discipline) | Medium (rollback pain) |
| R11 | Low (review) | High (architecture rot) |
| R12 | Low (CI lint) | Medium (UI drift) |

**The mitigations are all cheap. The failure modes are all expensive.** That asymmetry is why the risks deserve this register.

---

## 8. Review and update

This register is reviewed:

- **At every Gate transition** (G2-001 through G2-010, then every business module's Gate)
- **At every new decision** in `G1A_DECISIONS_V1` or `GULIERP_MODULE_INDEPENDENCE_RULE`
- **At least quarterly** even if no Gate

A new risk is added when:
- A new META_GULI rule is created.
- A new module introduces a new failure mode.
- A new technology choice (e.g. switching from PostgreSQL to MySQL) creates new risk.

A risk is **retired** when:
- Its mitigation is fully implemented and tested.
- The Operator declares it resolved.

---

## 9. Open questions for Operator

| # | Question | Default if unanswered |
|---|---|---|
| Q1 | Should each risk have a **named owner** (a person), or is the role ("Inventory module owner") sufficient? | Role is sufficient for V1 (no team yet) |
| Q2 | Should the risk register live in `docs/governance/` (current) or in a separate `docs/risk/`? | `docs/governance/` is fine |
| Q3 | At what Gate should the first **mandatory re-review** happen? | G2-005 (Audit Gate) — by then, R1, R7, R8, R11 have had a chance to fire |
| Q4 | Should the CI grep for `using SqlSugar` etc. be a separate goal, or part of the architecture-test goal (G2-007)? | Part of G2-007 (NetArchTest rules M1–M20 are the right home) |
| Q5 | If a risk's hard stop fires in the middle of a goal, who has the final say: the Agent or the Operator? | The Operator. The hard stop is a halt-and-escalate, not a halt-and-discard. |

---

## 10. Summary

The GuliERP greenfield is **high-opportunity and high-risk**. The opportunity is a clean, modern, modular, multi-tenant ERP. The risk is the same as for any greenfield: scope creep, technology drift, agent-invented rules, security deferred too far, and module coupling. This register names the top 12, scores them, names mitigations, and defines hard stops.

**The single most important rule**: at every Gate, the Operator re-reads this register. If any risk's likelihood has materially increased, the mitigation is re-applied. If a hard stop has fired, work halts.

---

*End of GuliERP Greenfield Risk Register V1 — Status: DRAFT. Companion: META_GULI_GOVERNANCE_V1.md, G2 architecture drafts.*
