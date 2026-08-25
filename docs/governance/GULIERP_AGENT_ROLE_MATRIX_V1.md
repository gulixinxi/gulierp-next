# GuliERP Agent Role Matrix V1

| Field | Value |
|---|---|
| **Document ID** | `GULIERP_AGENT_ROLE_MATRIX_V1` |
| **Version** | v1 (initial) |
| **Date** | 2026-08-25 (Asia/Shanghai) |
| **Author** | Mavis (M3 / mavis), acting as AI Software Factory Architect |
| **Status** | PROPOSAL — awaiting user ratification |
| **Supersedes** | `GULIERP_AGENT_GOVERNANCE.md` (4-role precursor) is **not deleted** but is superseded by this 5-role matrix |
| **Companions** | `GULIERP_AI_FACTORY_READINESS_REPORT.md` (TASK 1), `GULIERP_AI_DEVELOPMENT_PROTOCOL_V1.md` (TASK 3) |

This document defines the **5-role collaboration matrix** for
GuliERP Next's AI Software Factory. Each role has a **single
primary owner** (the AI model or human), a **scoped responsibility
set**, and a **mapped Meta_Kim meta-agent** (where applicable).

---

## 0. Why 5 Roles (and not 4)

The precursor `GULIERP_AGENT_GOVERNANCE.md` defined 4 roles
(GPT / Codex / Mavis / Human). This V1 refines to 5 roles by
**splitting Mavis into two**:

| V1 (this) | Precursor V0 | Reason for split |
|---|---|---|
| Meta_Kim (governance layer) | (not in V0) | Meta_Kim 8-stage spine + 9 meta-agents are a distinct role: they govern the workflow, not the content |
| MiniMax (audit / review) | Mavis (audit / review) | Mavis becomes a specific role name; the audit functions remain |
| Codex (executor) | Codex (executor) | Unchanged |
| GPT (architect) | GPT (architect) | Unchanged |
| Human (approver) | Human (approver) | Unchanged |

The 5-role model is more **explicit** about the governance layer
(Meta_Kim) and the audit layer (MiniMax), and it makes the
5-party handoff (GPT → Meta_Kim → Codex → MiniMax → Human)
**visible** in the role boundary table.

---

## 1. Role Boundary Matrix (5 roles × 11 responsibilities)

The following table assigns **each** responsibility to a **single
primary owner** (with secondary support). No responsibility is
shared between roles; if a role boundary is unclear, the
responsibility escalates to the next role (typically Human).

### 1.1 GPT (Architect Role)

**Primary owner**: GPT-4-class reasoning model (or equivalent) acting
as the design and decision layer.

| Responsibility | Primary | Support |
|---|---|---|
| **产品方向 (Product direction)** | GPT | Human |
| **Goal 拆解 (Goal decomposition)** | GPT | Meta_Kim |
| **架构决策 (Architecture decisions)** | GPT | MiniMax (review) |
| **优先级 (Prioritization)** | GPT | Human |
| **V1 freeze 触发 (V1 freeze trigger)** | GPT | Human (ratify) |

**Mapped Meta_Kim meta-agents**:
- `meta-warden` (final synthesis on major decisions)
- `meta-genesis` (persona / prompt architecture)
- `meta-conductor` (workflow blueprint)

**Examples (this repo)**:
- `docs/architecture/MDM_000_MASTER_DATA_CONVENTION_V1.md` (V1 freeze)
- `docs/architecture/G2_FOUNDATION_ARCHITECTURE_V1_DRAFT.md` (G2 standards)
- `docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md` (Numbering V1)
- `docs/architecture/ID_STRATEGY_FINAL_DECISION.md` (HiLo decision)

**Forbidden**:
- Write production code (Codex does this)
- Run tests (Codex does this)
- Self-approve (Human does this)

---

### 1.2 Meta_Kim (Governance Role)

**Primary owner**: Meta_Kim 8-stage governance spine (9 meta-agents:
`meta-warden`, `meta-conductor`, `meta-genesis`, `meta-artisan`,
`meta-sentinel`, `meta-librarian`, `meta-prism`, `meta-scout`,
`meta-chrysalis`).

| Responsibility | Primary | Support |
|---|---|---|
| **Agent 调度 (Agent dispatch)** | Meta_Kim (`meta-warden`) | Codex / MiniMax |
| **流程治理 (Process governance)** | Meta_Kim (`meta-conductor`) | Human |
| **Memory 管理 (Memory management)** | Meta_Kim (`meta-librarian`) | MiniMax |
| **Evidence 管理 (Evidence management)** | Meta_Kim (`meta-prism`) | MiniMax |
| **Capability 索引 (Capability index)** | Meta_Kim (`meta-scout` + `meta-artisan`) | — |
| **Risk 检查 (Risk checking)** | Meta_Kim (`meta-sentinel`) | MiniMax |
| **Evolution writeback (pattern reuse)** | Meta_Kim (`meta-chrysalis`) | MiniMax |

**Outputs**:
- `GOAL_REGISTRY.md` (librarian)
- `EVOLUTION_WRITEBACK.md` (chrysalis)
- Hook enforcement (sentinel)
- Agent roster updates (artisan + scout)
- Workflow templates (conductor)
- Audit packet aggregation (prism)

**Forbidden**:
- Make architectural decisions (GPT does this)
- Write production code (Codex does this)
- Self-approve (Human does this)

---

### 1.3 Codex (Executor Role)

**Primary owner**: Codex-class code generation model (or equivalent)
acting as the implementation layer.

| Responsibility | Primary | Support |
|---|---|---|
| **后端开发 (Backend development)** | Codex | MiniMax (review) |
| **前端开发 (Frontend development)** | Codex | MiniMax (review) |
| **测试 (Tests: unit + integration + e2e)** | Codex | MiniMax (review) |
| **修复 (Bug fixes)** | Codex | MiniMax (review) |
| **Migration 实现 (Migration implementation)** | Codex | GPT (architect) + MiniMax (review) |
| **Local build + test execution** | Codex | — |

**Mapped Codex worker agents** (per `.codex/agents/`):
- `backend.toml`, `frontend.toml`, `test.toml`, `worker.toml`
- `review.toml`, `verify.toml` (auxiliary)
- `analysis.toml`, `docs.toml`, `explorer.toml` (auxiliary)

**Forbidden**:
- Define Goal scope (GPT does this)
- Sign off on own work (MiniMax does this)
- Commit (Human does this)
- Touch frozen V1 contracts without explicit unfreeze authorization

---

### 1.4 MiniMax (Audit Role)

**Primary owner**: MiniMax-class review model (or equivalent) acting
as the verification and review layer.

| Responsibility | Primary | Support |
|---|---|---|
| **Code Review** | MiniMax | Meta_Kim (`meta-prism`) |
| **架构审核 (Architecture review)** | MiniMax | GPT (architect) |
| **风险检查 (Risk check)** | MiniMax | Meta_Kim (`meta-sentinel`) |
| **验收 (Acceptance / Verification)** | MiniMax | Meta_Kim (`meta-prism`) |
| **Verification report writing** | MiniMax | Meta_Kim (`meta-librarian`) |
| **Honest disclosure (failure / contradiction / deferred work)** | MiniMax | — |
| **Drift detection (canonical vs runtime, contract vs implementation)** | MiniMax | Meta_Kim (`meta-prism`) |

**Mapped Meta_Kim meta-agents**:
- `meta-prism` (drift detection, anti-slop)
- `meta-librarian` (continuity of prior decisions)
- `meta-warden` (final synthesis on whether a Gate is achieved)
- `meta-sentinel` (audit-time safety check)

**Forbidden**:
- Write production code (Codex does this)
- Make architectural decisions (GPT does this)
- Commit (Human does this)
- Mark a Goal verified if any V1 frozen contract is being broken
  (must escalate to GPT + Human)

---

### 1.5 Human (Approver Role)

**Primary owner**: The user (the human developer / project lead).

| Responsibility | Primary | Support |
|---|---|---|
| **最终决策 (Final decision)** | Human | — |
| **Commit 授权 (Commit authorization)** | Human | — |
| **Push 授权 (Push authorization)** | Human | — |
| **商业判断 (Business judgment)** | Human | GPT (architect) |
| **V1 freeze 批准 (V1 freeze ratification)** | Human | GPT (architect) |
| **生产 promotion (Production promotion)** | Human | Codex (CI) |
| **Dispute arbitration (between roles)** | Human | — |
| **Escalation to user when roles disagree** | (escalation receiver) | — |

**Forbidden**:
- Write production code (Codex does this)
- Write documentation (MiniMax does this)
- Run test suites (Codex does this)
- Bypass the protocol (the protocol is the only path)

---

## 2. Cross-Role Handoff Protocol

### 2.1 Standard handoff (5-party)

```
┌─────────┐  propose   ┌─────────┐  decompose  ┌─────────┐
│   GPT   │ ──────────→│ Meta_Kim│ ──────────→│  Codex  │
│architect│            │ 8-stage │            │executor │
└────▲────┘            └────┬────┘            └────┬────┘
     │                      │ review              │ produce
     │                      ▼                      ▼
     │                 ┌─────────┐            ┌─────────┐
     │                 │ MiniMax │←──verify ──│  Codex  │
     │                 │  audit  │            │ (impl)  │
     │                 └────┬────┘            └─────────┘
     │                      │ sign-off
     │                      ▼
     │                 ┌─────────┐
     └───── approve ──│  Human  │
                       │  final  │
                       └────┬────┘
                            │ commit
                            ▼
                       (next Goal)
```

### 2.2 Handoff content at each step

| Step | From | To | Content |
|---|---|---|---|
| 1 | Human | GPT | Intent (1-2 sentences) |
| 2 | GPT | Meta_Kim | Goal Card (see `GULIERP_PURCHASE_ORDER_GOAL_CARD_001.md` for the template) |
| 3 | Meta_Kim | Codex | Execution Plan (shards, dependencies, owners) |
| 4 | Codex | MiniMax | Implementation (code + tests + migration) + self-test results |
| 5 | MiniMax | GPT | Verification Report (findings, Gate, recommendations) |
| 6 | GPT | Human | Decision (approve / amend / reject) |
| 7 | Human | (remote) | `git commit` + `git push` |

### 2.3 Handoff rules

- **Each handoff is documented**: the producing role writes the
  artifact; the consuming role acknowledges the artifact in writing
  (typically a chat reply).
- **No role is skipped**: even for trivial Goals, all 5 parties are
  informed (the trivial-cycle shortcut collapses phases, not
  roles).
- **Each handoff is reversible**: the producing role can amend the
  artifact up to the point of the consuming role's acknowledgment.

---

## 3. Lane Ownership Matrix (14 lanes)

The following table assigns **each business lane** to a primary
owner + support, in the spirit of the 4-role precursor but
**refined** for the 5-role model.

| Business Lane | Primary Owner | Support | Final Approver |
|---|---|---|---|
| **V1 architecture freeze** | GPT | MiniMax (review) | Human |
| **Goal scope design** | GPT | Meta_Kim (librarian context) | Human |
| **Source code (backend / frontend)** | Codex | MiniMax (review) | Human (commit) |
| **Database migration** | Codex | GPT (design) + MiniMax (review) | Human (commit) |
| **Test design + implementation** | Codex | MiniMax (review) | Human (commit) |
| **Documentation (architecture / design)** | GPT | MiniMax (review) | Human (commit) |
| **Documentation (verification / report)** | MiniMax | GPT (review) | Human (commit) |
| **Repository governance / audit** | MiniMax | Meta_Kim (sentinel) | Human (commit) |
| **Meta_Kim integration** | MiniMax | Codex (apply sync) | Human (commit) |
| **Goal Gate verification** | MiniMax | GPT (sign-off) | Human (commit) |
| **Build / runtime hygiene** | Codex | MiniMax (audit) | Human (commit) |
| **Pre-flight cleanup (test data)** | MiniMax | — | Human (consent) |
| **Production promotion** | Human | Codex (CI) | Human |
| **Dispute arbitration** | Human | — | — |

---

## 4. Conflict Resolution

| Conflict Type | Resolution |
|---|---|
| GPT vs Codex scope disagreement | GPT wins. Codex re-scopes to the plan. |
| Codex vs MiniMax implementation disagreement | MiniMax files findings; if Codex disagrees, escalate to Human. |
| MiniMax vs Human Gate approval | Human wins. MiniMax re-runs with more evidence if Human requests. |
| Meta_Kim (governance) vs any role | Meta_Kim wins on governance questions (e.g., "this is a V1 contract, you cannot break it"); loses on content questions (e.g., "what should the new feature look like"). |
| Multiple parallel Codex shards colliding on same file | MiniMax serializes (one shard waits for the other to commit-stash-unstash). |
| MiniMax finds a frozen-contract violation | STOP. Escalate to GPT and Human. Do not patch. |

---

## 5. Stop Conditions (System-wide)

Any role MUST stop and escalate when:

1. **A V1 frozen contract is being broken** (e.g., changing
   `FormatValidator.cs` regex without explicit unfreeze authorization).
2. **A migration is being added without a `*_MIGRATION_PLAN.md`** entry
   in `docs/planning/`.
3. **A test is being marked `Skip` to make a build pass** (test-gaming,
   not contract reconciliation).
4. **A drop / delete / production-data-touching command is being considered**.
5. **A long-running operation exceeds its time budget** (e.g.,
   `dotnet build` > 10 min, `dotnet test` > 10 min without progress).
6. **A quota alert fires** (Codex CLI, OpenAI API, ChatGPT Plus/Pro,
   MiniMax API). Per `Quota Management` rule, stop and confirm before
   continuing.

---

## 6. Compatibility with Meta_Kim 9-agent Roster

This 5-role model is a **business-role layer** that composes WITH
the 9-agent Meta_Kim roster, not a replacement for it:

| Business Role | Meta_Kim Agents Invoked |
|---|---|
| GPT | `meta-warden`, `meta-genesis`, `meta-conductor` |
| Meta_Kim | All 9 meta-agents (it IS Meta_Kim) |
| Codex | Codex built-in `backend`/`frontend`/`test`/`worker` agents; `meta-artisan` (skill fit); `meta-sentinel` (guardrails) |
| MiniMax | `meta-prism` (drift), `meta-librarian` (memory), `meta-warden` (final synthesis), `meta-sentinel` (audit safety) |
| Human | (above the agent layer; final authority) |

The shared `meta-theory` skill, the `enforce-agent-dispatch.mjs`
hook, and the `graphify-context.mjs` hook apply to all 3 AI roles
(GPT, Codex, MiniMax). The Human does not invoke agents directly;
the Human receives the deliverable from MiniMax and makes the
commit / push / approve call.

---

## 7. Out-of-Scope

The following are **NOT** covered by this matrix:

- **GuliERP product roadmap** — owned by user (this is operational
  governance, not product planning).
- **TRAE frontend handoff** — see `docs/architecture/TRAE_*_HANDOFF.md`
  for the cross-team protocol; this matrix does NOT replace that.
- **Production deployment** — owned by the human + the Operator
  runtime.
- **Cost / pricing decisions** — owned by user.

---

## 8. Sign-off

**Gate**: `GULIERP_AGENT_ROLE_MATRIX_V1_PROPOSAL`

- ✅ 5 roles defined: GPT (architect) / Meta_Kim (governance) / Codex
  (executor) / MiniMax (audit) / Human (approver)
- ✅ 11 primary responsibilities assigned (no sharing)
- ✅ Lane ownership matrix (14 lanes)
- ✅ Handoff protocol (7-step, all 5 parties)
- ✅ Conflict resolution (6 conflict types)
- ✅ System-wide stop conditions (6)
- ✅ Compatibility with Meta_Kim 9-agent roster
- ✅ Out-of-scope explicit

**Author**: Mavis (M3 / mavis)
**Date**: 2026-08-25 (Asia/Shanghai)
**Status**: PROPOSAL — awaiting user ratification
**Next deliverable**: `GULIERP_AI_DEVELOPMENT_PROTOCOL_V1.md` (TASK 3)
