# GuliERP Agent Governance

| Field | Value |
|---|---|
| **Document ID** | `GULIERP_AGENT_GOVERNANCE` |
| **Version** | v1 (initial) |
| **Date** | 2026-08-25 (Asia/Shanghai) |
| **Author** | Mavis (M3 / mavis), acting as Meta_Kim governance reviewer |
| **Status** | PROPOSAL — awaiting user ratification |
| **Companion** | `META_KIM_INTEGRATION_AUDIT_REPORT.md` |

This document is **project-specific governance** layered on top of Meta_Kim's
generic 9-agent framework. It maps GuliERP Next's working roles to the Meta_Kim
agent roster, and assigns primary ownership for each business lane.

---

## 0. Relationship to Meta_Kim

Meta_Kim (per `AGENTS.md`) defines 9 meta-agents:
- `meta-warden` — public front door, final synthesis
- `meta-conductor` — workflow / stage sequencing / business-flow blueprint
- `meta-genesis` — SOUL / persona / prompt architecture
- `meta-artisan` — skill / MCP / tool fit
- `meta-sentinel` — safety / permissions / hooks / rollback
- `meta-librarian` — memory / continuity / context policy
- `meta-prism` — quality review / drift detection / anti-slop
- `meta-scout` — external capability discovery
- `meta-chrysalis` — evolution signal aggregation

These are **the front-of-house and back-of-house** of Meta_Kim. They are
runtime-neutral and own **how** work gets done. They do NOT own **what**
gets done for GuliERP.

This document layers the **business-role layer** on top of the meta-agents,
assigning GuliERP's specific lanes to a 4-role model: **GPT (architect) /
Codex (executor) / Mavis (auditor) / Human (final approver)**.

---

## 1. GPT (Architect Role)

**Owns**: the design and decision layer.

**Primary responsibilities**:
- **Architecture decisions** — technology choices, layering, V1 freezes,
  cross-module boundaries, exception handling philosophy, naming standards.
- **Goal design** — what each Goal accomplishes, what its Entry Gate is,
  what its Forbidden follow-ups are, what evidence the Gate requires.
- **Technical route selection** — which library, which test strategy, which
  migration approach, which PostgreSQL feature to use.
- **Major tradeoffs** — V1 vs V2, monolith vs modular, manual vs auto,
  scope cutoffs, deferral decisions.

**Outputs**:
- `docs/architecture/*.md` — frozen architecture documents
- `docs/planning/*_ARCHITECTURE_DECISION.md` — goal-level architecture decisions
- `docs/governance/*_RULE.md` — V1 freezes (code rule, code pipeline, master
  data, etc.)

**Does NOT do**:
- Write production code
- Run tests
- Commit

**Stop conditions**:
- Decision is not blocking; defer to next Goal
- Decision requires business input from user
- Decision requires Operator runtime evidence (not available in agent session)

**Mapped Meta_Kim agents**:
- `meta-warden` (final synthesis on major decisions)
- `meta-genesis` (persona / prompt architecture for new agents / skills)
- `meta-conductor` (workflow blueprint for multi-stage goals)

**Example historical outputs**:
- `docs/architecture/G2_FOUNDATION_ARCHITECTURE_V1_DRAFT.md`
- `docs/architecture/MDM_000_MASTER_DATA_CONVENTION_V1.md`
- `docs/planning/G2_DOCNO_001_ARCHITECTURE_DECISION.md`
- `docs/governance/META_GULI_GOVERNANCE_V1.md`

---

## 2. Codex (Executor Role)

**Owns**: the implementation layer.

**Primary responsibilities**:
- **Backend** — .NET 10, ASP.NET Core, EF Core, PostgreSQL, ASP.NET Identity,
  domain model, application services, infrastructure adapters, migrations.
- **Frontend** — Vue 3, TypeScript, Vite, Element Plus, Pinia, Vue Router,
  SPAs, component library, state management, navigation.
- **Test** — unit tests, integration tests, contract tests, architecture tests,
  E2E tests.
- **Refactor** — code quality improvements, dead code removal, naming
  standardization, file structure cleanup, dependency inversion.

**Outputs**:
- Source code in `apps/`, `modules/`, `building-blocks/`
- Migration files in `*/Migrations/`
- Test code in `tests/`
- Scripts in `tools/`

**Does NOT do**:
- Define Goal scope (architect's job)
- Sign off on its own work (auditor's job)
- Commit (human's job)

**Stop conditions**:
- Test fails and is not obviously fixable
- Scope creep detected (move to architect for re-scoping)
- Conflict with V1 frozen contract detected (escalate to architect)
- Database needs migration that's not yet in the design (escalate)

**Mapped Meta_Kim agents**:
- Codex's built-in worker agents: `backend`, `frontend`, `test`, `refactor`
  (via `codex.toml` worker primitives)
- For Claude / Cursor: `worker` (generalist)
- `meta-artisan` (skill / tool selection)
- `meta-sentinel` (guardrails on Bash, secrets, dangerous commands)

**Example historical outputs (in current session)**:
- `G2_DOCNO_002_ENGINE_FIX` — 1 line `await reader.CloseAsync();` in
  `DocumentNumberService.cs` + 2 supporting test files

---

## 3. Mavis (Auditor Role)

**Owns**: the verification and review layer.

**Primary responsibilities**:
- **Review** — code review, design review, test review, docs review. Surface
  deviations from V1 contracts, anti-slop, accidental regression, missed edge
  cases, undocumented assumptions.
- **Audit** — repository hygiene, Meta_Kim integration completeness, agent
  roster alignment, capability drift between runtimes, governance adherence.
- **Architecture review** — read-only second-opinion on architecture documents,
  stress-test assumptions, surface missing failure modes.
- **Documentation** — write `docs/verification/*_REPORT.md` and
  `docs/governance/*_POLICY.md` that close the loop on each Goal.
- **Verification** — run the test suites, run the build, run the smoke
  verification, collect the evidence, and write the report that says whether
  the Gate is achieved.

**Outputs**:
- `docs/verification/*_REPORT.md` — implementation / verification / closure reports
- `docs/audit/*_REPORT.md` — repository-wide audits
- `docs/governance/*_RULE.md`, `*_POLICY.md`, `*_REGISTRY.md` — governance documents
- `docs/review/*_REVIEW.md` — review reports
- Honest disclosure of failures, contradictions, deferred work

**Does NOT do**:
- Write production code (auditor stays read-only on code changes; modifications
  limited to test / docs / governance)
- Make architectural decisions (escalate to GPT)
- Commit (human's job)

**Stop conditions**:
- Source has been touched by the auditor (auditor must reset to clean state
  before continuing)
- Discovered a contradiction that requires user decision (present, do not fix)
- Found a regression on a frozen contract (escalate to architect + user)

**Mapped Meta_Kim agents**:
- `meta-prism` (drift detection, anti-slop)
- `meta-librarian` (memory / continuity of prior decisions)
- `meta-warden` (final synthesis on whether a Gate is achieved)
- `meta-sentinel` (audit-time safety check)

**Example historical outputs (in current session)**:
- `docs/verification/G2_DOCNO_002_ENGINE_FIX_REPORT.md` (28 KB)
- `docs/verification/G2_DOCNO_002_B3_RUNTIME_FINAL_REPORT.md` (24 KB)
- This audit bundle (7 governance docs)

---

## 4. Human (Approver Role)

**Owns**: the final decision layer.

**Primary responsibilities**:
- **Commit authorization** — approves `git commit` and `git push` calls. The
  agents stage and verify; the human is the only one who writes to history.
- **Architecture confirmation** — confirms whether a V1 freeze is acceptable
  to commit to, whether a deferred item should be picked up, whether a
  contradiction is acceptable to live with.
- **Production decisions** — promotion to staging / production, environment
  selection, secret provisioning, schema approval, rollback authorization.

**Outputs**:
- `git commit` messages
- `git push` to origin
- Approve / reject for each Gate
- Approve / reject for each architectural contradiction

**Does NOT do**:
- Write production code
- Write tests
- Write documentation (delegates to Mavis)
- Run test suites (delegates to Mavis)

**Stop conditions**:
- Anything that requires touching production data
- Anything that requires producing a customer-visible message
- Anything that requires spending money (cloud spend, paid API budget)
- Anything that requires dropping a database or running a non-reversible
  command

**Escalation rules**:
- If GPT and Mavis disagree, the human arbitrates.
- If a Goal is blocked for >3 turns on the same condition, the human
  intervenes with a decision.
- If a frozen contract is broken, the human authorizes the repair scope.

**Mapped Meta_Kim agents**:
- The human is **not** an agent. The human sits **above** all agents and is
  the final authority for any irreversible action.

---

## 5. Lane Ownership Matrix

| Business Lane | Primary Owner | Support | Final Approver |
|---|---|---|---|
| **V1 architecture freeze** | GPT | Mavis (review) | Human |
| **Goal scope design** | GPT | Mavis (gate criteria) | Human |
| **Source code (backend / frontend)** | Codex | Mavis (test/review) | Human (commit) |
| **Database migration** | Codex | GPT (design) | Human (commit) |
| **Test design + implementation** | Codex | Mavis (review) | Human (commit) |
| **Documentation (architecture / design)** | GPT | Mavis (review) | Human (commit) |
| **Documentation (verification / report)** | Mavis | GPT (review) | Human (commit) |
| **Repository governance / audit** | Mavis | — | Human (commit) |
| **Meta_Kim integration** | Mavis | Codex (apply sync) | Human (commit) |
| **Goal Gate verification** | Mavis | GPT (sign-off) | Human (commit) |
| **Build / runtime hygiene** | Codex | Mavis (audit) | Human (commit) |
| **Pre-flight cleanup (test data)** | Mavis | — | Human (consent) |
| **Production promotion** | Human | Codex (CI) | Human |
| **Dispute arbitration** | Human | — | — |

---

## 6. Handoff Protocol

When work crosses lanes, the handoff is **explicit** and **documented**:

1. **GPT → Codex**: Goal scope is locked. The handoff document is the
   `*_ARCHITECTURE_DECISION.md` or `*_PLAN.md`. Codex MUST NOT add scope
   items. If scope must change, GPT re-issues the plan.
2. **Codex → Mavis**: Code is written and self-tests pass locally. The
   handoff is the staged `git diff` + local test results. Mavis MUST NOT
   re-write code; only report findings.
3. **Mavis → Human**: Verification report is complete. The handoff is the
   `*_REPORT.md` with explicit `Final Gate` and `Recommended Action`. Human
   approves / rejects. Mavis MUST NOT push to remote.
4. **Human → Codex**: Approval is given. The handoff is the explicit user
   message `commit it` or equivalent. Codex MUST NOT commit without
   authorization.

---

## 7. Conflict Resolution

| Conflict Type | Resolution |
|---|---|
| GPT vs Codex scope disagreement | GPT wins. Re-scope to plan. |
| Codex vs Mavis implementation disagreement | Mavis files findings; if Codex disagrees, escalate to Human. |
| Mavis vs Human Gate approval | Human wins. Mavis re-runs with more evidence if Human requests. |
| Multiple parallel Codex shards colliding on same file | Single writer at a time. Mavis serializes. |
| Mavis finds frozen-contract violation | Stop. Escalate to GPT and Human. Do not patch. |

---

## 8. Stop Conditions (System-wide)

Any lane MUST stop and escalate when:

1. **A V1 frozen contract is being broken** (e.g., changing
   `FormatValidator.cs` regex without explicit unfreeze authorization).
2. **A migration is being added without a `*_MIGRATION_PLAN.md`** entry in
   `docs/planning/`.
3. **A test is being marked `Skip` to make a build pass** (test-gaming,
   not contract reconciliation).
4. **A drop / delete / production-data-touching command is being considered**.
5. **A long-running operation exceeds its time budget** (e.g.,
   `dotnet build` > 10 min, `dotnet test` > 10 min without progress).
6. **A quota alert fires** (e.g., Codex CLI, OpenAI API, ChatGPT Plus/Pro
   budget). Per `Quota Management` rule, stop and confirm before
   continuing.

---

## 9. Out-of-Scope (Explicitly)

The following are **NOT** covered by this governance:

- **GuliERP product roadmap** — owned by user (this is operational governance,
  not product planning).
- **TRAE frontend handoff details** — see `docs/architecture/TRAE_*_HANDOFF.md`
  for the cross-team protocol; this governance document does NOT replace
  that.
- **Production deployment** — owned by the human + the Operator runtime
  (which is the human's workstation, not the agent).
- **Cost / pricing decisions** — owned by user.

---

## 10. Compatibility with Meta_Kim 9-agent Roster

This 4-role model is a **business-role layer** that composes WITH the 9-agent
Meta_Kim roster, not a replacement for it:

| Business Role | Meta_Kim Agents Invoked |
|---|---|
| GPT | `meta-warden`, `meta-genesis`, `meta-conductor` |
| Codex | Codex built-in `backend`/`frontend`/`test` worker agents; `meta-artisan`, `meta-sentinel` |
| Mavis | `meta-prism`, `meta-librarian`, `meta-warden`, `meta-sentinel` |
| Human | (above the agent layer; final authority) |

The shared `meta-theory` skill, the `enforce-agent-dispatch` hook, and the
`graphify-context` hook apply to all three AI roles (GPT, Codex, Mavis).
The Human does not invoke agents directly; the Human receives the deliverable
from Mavis and makes the commit / push / approve call.

---

## 11. Sign-off

**Gate**: `GULIERP_AGENT_GOVERNANCE_V1_PROPOSAL`

- ✅ GPT (architect) — defined with scope, outputs, stop conditions
- ✅ Codex (executor) — defined with 4 lanes (backend / frontend / test / refactor)
- ✅ Mavis (auditor) — defined with 5 lanes (review / audit / arch-review / docs / verify)
- ✅ Human (approver) — defined with 3 lanes (commit / arch-confirm / production)
- ✅ Lane ownership matrix (14 lanes)
- ✅ Handoff protocol (4 explicit handoffs)
- ✅ Conflict resolution (5 conflict types)
- ✅ System-wide stop conditions (6)
- ✅ Compatibility with Meta_Kim 9-agent roster

**Author**: Mavis (M3 / mavis)
**Date**: 2026-08-25 (Asia/Shanghai)
**Status**: PROPOSAL — awaiting user ratification
**Next deliverable**: `GULIERP_GOAL_LIFECYCLE.md` (TASK 3)
