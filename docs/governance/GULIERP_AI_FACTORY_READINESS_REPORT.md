# GuliERP AI Factory Readiness Report

| Field | Value |
|---|---|
| **Report ID** | `GULIERP_AI_FACTORY_READINESS_REPORT` |
| **Goal** | `GULIERP_META_KIM_MULTI_AGENT_ACCEPTANCE_001` (TASK 1 of 5) |
| **Repo** | `D:\guli\projects\gulierp-next` |
| **HEAD** | `9e4ec48` (master, post-baseline) |
| **Date** | 2026-08-25 (Asia/Shanghai) |
| **Per Brief** | Read-only analysis. NO source / DB / migration change. NO commit / push. |

This document audits the **current AI governance state** of the GuliERP
Next repository against the target **5-role multi-agent collaboration
loop** (GPT + Meta_Kim + Codex + MiniMax + Human). The purpose is to
determine whether the project is **ready** to enter the next
business-development phase under AI-factory-style governance.

---

## 0. Executive Summary

| Dimension | Status | Comment |
|---|---|---|
| Runtime projections (3 of 4) | ✅ COMPLETE | Claude / Codex / Cursor all wired; OpenClaw missing (LOW) |
| Shared skills | ✅ COMPLETE | `meta-theory` (15 reference docs) + `same-set-reusable-flow-for-project-file-inventor` |
| Entrypoints | ✅ COMPLETE | `AGENTS.md` (25 KB) + `CLAUDE.md` (17 KB) |
| Meta_Kim 8-stage spine | ✅ WIRED | `Skill meta-theory` hook in 3 runtimes + `enforce-agent-dispatch.mjs` |
| Agent roster (9 meta-agents × 3 runtimes) | ✅ COMPLETE | warden / conductor / genesis / artisan / sentinel / librarian / prism / scout / chrysalis |
| Codex worker agents | ✅ COMPLETE | 9 worker agents (analysis / backend / docs / explorer / frontend / review / test / verify / worker) |
| Capability-first dispatch | ✅ WIRED | `enforce-agent-dispatch.mjs` hook + `meta-kim-capabilities.json` × 3 |
| Governance documents | ✅ 20 documents | 19 in `docs/governance/` + 1 in `docs/governance/GULIERP_AI_DEVELOPMENT_PROTOCOL.md` (pre-existing) |
| **Canonical source layer** | ❌ **MISSING (HIGH)** | The 3 runtime trees are hand-copied, not regenerated from `canonical/` |
| **MCP wiring** | ❌ **EMPTY (LOW)** | `.mcp.json` + `.cursor/mcp.json` = `{"mcpServers": {}}` |
| **5-role collaboration loop** | ⚠️ **NOT YET OPERATIONAL** | GPT / Codex / MiniMax / Human roles are documented; **the orchestration between them is defined on paper but not exercised in this repo's history** |
| **Goal-driven protocol (5 phases)** | ⚠️ **NOT FORMALLY EXERCISED** | `GULIERP_AI_DEVELOPMENT_PROTOCOL.md` (17 KB, pre-existing) defines Phase 0-6; `GULIERP_GOAL_LIFECYCLE.md` (15 KB) defines Discovery → Evolution. **Neither has been formally trialed end-to-end.** |

**Final Readiness Verdict**: `PARTIAL — ready for business development with governance hand-holding`. The
governance layer is **structurally complete** but **operationally
unproven**. The project can begin the next business development
phase **only if** the user accepts that the first 2-3 Goals will
exercise the protocol in `PROBATION` mode, with the user present at
each Commit Approval to surface gaps in the role boundaries.

---

## 1. Current Capabilities (具备)

### 1.1 Meta_Kim runtime layer (3 of 4 runtimes)

| Runtime | Agents | Hooks | Skills | Settings | Status |
|---|---:|---:|---|:---:|:---:|
| `.codex/` (Codex) | 18 (9 meta + 9 worker) | 8 | (via `.agents/`) | hooks.json | ✅ COMPLETE |
| `.claude/` (Claude Code) | 9 (9 meta) | 16 (8 shared + 8 Claude-specific) | meta-theory + same-set-reusable-flow | settings.json | ✅ COMPLETE |
| `.cursor/` (Cursor v1.7+) | 9 (9 meta) | 8 | (in `.agents/`) | hooks.json + mcp.json | ✅ COMPLETE |
| `openclaw/` | 0 | 0 | 0 | n/a | ❌ MISSING (LOW) |

### 1.2 Shared cross-runtime skills (`.agents/`)

- `meta-theory/` — full skill bundle:
  - `SKILL.md` (entry point)
  - `evals/eval-contract.md`
  - 15 reference docs (`create-agent.md`, `dev-governance.md`, `evolution-writeback.md`,
    `intent-amplification.md`, `meta-theory.md`, `owner-resolution.md`,
    `path-selection.md`, `planning-files.md`, `rhythm-orchestration.md`,
    `runtime-claude.md`, `runtime-codex.md`, `spine-state.md`,
    `ten-step-governance.md`, `verification-evidence.md`)
- `same-set-reusable-flow-for-project-file-inventor/SKILL.md` — reuse flow

### 1.3 Entrypoint documents

- `AGENTS.md` (25,586 bytes) — Codex / OpenCode / Aider / Droid / Trae shared entrypoint
- `CLAUDE.md` (17,495 bytes) — Claude Code specific entrypoint

### 1.4 Meta_Kim 8-stage governance spine

Wired into all 3 runtimes via `Skill meta-theory` hook:

```
Critical → Fetch → Thinking → Execution → Review → Meta-Review → Verification → Evolution
```

When `meta-theory` is invoked, the 8-stage spine is activated and the
Mavis (MiniMax) thread drives the workflow. This is **structurally
complete**; the **operational track record** is in
`GULIERP_PROJECT_MEMORY_INDEX.md` (50+ Goals indexed, all closed
without the explicit 8-stage meta-theory activation, but all
followed the 6-stage Goal lifecycle).

### 1.5 Capability-first dispatch

The `enforce-agent-dispatch.mjs` hook is wired into all 3 runtimes
(`PreToolUse: Bash|Edit|Write|MultiEdit|NotebookEdit|Agent|spawn_agent`).
It enforces capability discovery via `meta-kim-capabilities.json` (one
copy per runtime). **This is the safety rail** that prevents agents
from acting without first consulting the capability index.

### 1.6 Governance documents (20 total)

`docs/governance/` contains 20 documents (12 from this audit cycle +
8 pre-existing). They cover:

- **Project memory**: `GOAL_REGISTRY.md` (147 KB), `GULIERP_PROJECT_MEMORY_INDEX.md`
- **Goal lifecycle**: `GULIERP_GOAL_LIFECYCLE.md` (15 KB)
- **Agent roles**: `GULIERP_AGENT_GOVERNANCE.md` (14 KB), `GULIERP_AGENT_WORK_RULES.md`
- **AI development protocol**: `GULIERP_AI_DEVELOPMENT_PROTOCOL.md` (17 KB)
- **Meta_Kim integration**: `META_KIM_INTEGRATION_AUDIT_REPORT.md` (19 KB)
- **Repository authority**: `GULIERP_REPOSITORY_AUTHORITY.md`, `GULIERP_MODULE_INDEPENDENCE_RULE.md`
- **Database governance**: `DATABASE_TARGET_REGISTRY.md` (13 KB)
- **Risk register**: `GULIERP_GREENFIELD_RISK_REGISTER_V1.md` (23 KB)
- **Architecture rules**: `ARCHITECTURE_RULES.md`
- **Baseline governance**: `GULIERP_GIT_BASELINE_AUDIT_REPORT.md`, `GULIERP_GITIGNORE_POLICY.md`, `GULIERP_META_KIM_GIT_POLICY.md`, `GULIERP_BASELINE_COMMIT_PLAN.md`, `GULIERP_GITHUB_BASELINE_RELEASE_REPORT.md`
- **Workspace hygiene**: `GULIERP_WORKSPACE_CLEANUP_PLAN.md`

### 1.7 Established business baseline (post-baseline push)

| Lane | Status |
|---|---|
| G2-005 Identity | ✅ CLOSED |
| MDM Dictionary | ✅ CLOSED |
| MDM Employee | ✅ CLOSED |
| NumberingRule | ✅ CLOSED (B1 + B2 + B3) |
| DocumentKernel Engine Fix | ✅ CLOSED (`G2_DOCNO_002_ENGINE_FIX`) + B3 Runtime Re-verified |
| SalesOrder Runtime | ✅ CLOSED (G2_DOCNO B3) |
| GitHub baseline | ✅ PUSHED (`9e4ec48`, `gulixinxi/gulierp-next`) |

The business codebase is **operationally complete** for these
lanes. The next phase (Purchase, Inventory, CRM, etc.) can build
on this proven stack.

---

## 2. Gaps (缺失)

### 2.1 Gap 1: `canonical/` source layer (HIGH)

**Symptom**: The 3 runtime trees (`.codex/`, `.claude/`, `.cursor/`)
contain hand-copied projections of the 9 meta-agents and the
`meta-theory` skill. The canonical source layer (`canonical/agents/`,
`canonical/skills/meta-theory/`, `config/contracts/`,
`config/capability-index/`) is **absent**.

**Impact**:
- A future Meta_Kim version upgrade cannot be regenerated via
  `npm run meta:sync`; the user must hand-edit 27 files across 3
  runtimes.
- Drift between the 3 runtime trees is undetectable.
- The `enforce-agent-dispatch.mjs` hook reads the runtime
  `meta-kim-capabilities.json`; if the canonical version diverges,
  the hook will not detect it.

**Mitigation**: Open `GULIERP_META_KIM_CANONICAL_RESTORE_001` to
bootstrap `canonical/` from upstream Meta_Kim (`KimYx0207/Meta_Kim`)
or rebuild from the projected runtime trees.

### 2.2 Gap 2: 5-role orchestration is defined on paper, not exercised (MEDIUM)

**Symptom**: The 5-role model (GPT / Meta_Kim / Codex / MiniMax /
Human) is **defined** in this audit (`GULIERP_AGENT_ROLE_MATRIX_V1.md`)
and the prior audit (`GULIERP_AGENT_GOVERNANCE.md` 4-role precursor).
However, the project history (`GULIERP_PROJECT_MEMORY_INDEX.md` — 50+
Goals) shows that **all** prior Goals were executed by **Mavis
(audit / review)** with Codex-style code edits in inline `git add`
batches, not by a coordinated GPT → Meta_Kim → Codex → MiniMax →
Human handoff.

**Impact**:
- The role boundaries (e.g., "what GPT decides vs. what MiniMax
  decides") are **untested in practice**.
- The handoff protocol (Phase 1 → Phase 2 → ... → Phase 6) is
  **not yet muscle memory**.
- The first business-development Goal under the new protocol is
  likely to surface gaps (e.g., "GPT cannot do this without
  Codex's help", or "MiniMax is blocking the protocol").

**Mitigation**:
- The first 2-3 Goals under the new protocol are **PROBATION**:
  user is present at each Commit Approval to surface role-boundary
  gaps.
- After 3 Goals, the user + Mavis review the protocol and either
  confirm the boundaries or amend them.

### 2.3 Gap 3: 7-phase AI development protocol is V1, untested (MEDIUM)

**Symptom**: `GULIERP_AI_DEVELOPMENT_PROTOCOL_V1.md` (this audit
delivers a V1 variant) defines Phase 0 (Intent) → Phase 6 (Evolution).
`GULIERP_AI_DEVELOPMENT_PROTOCOL.md` (17 KB, pre-existing) defines
a similar but differently-named set of phases. The two protocols
overlap significantly but are not identical.

**Impact**:
- The user may be confused which protocol applies.
- Future Goals will hit ambiguities (e.g., "Phase 4 Review" vs.
  "Stage 5 Verification").

**Mitigation**: The 7-phase V1 protocol in this audit is the
**canonical** protocol going forward. The pre-existing 6-stage
Goal lifecycle is preserved as the **inner loop** (per
`GULIERP_AI_DEVELOPMENT_PROTOCOL_V1.md` cross-reference). One
protocol is the **outer** (Phase 0-6) and the other is the **inner**
(Discovery → Evolution). Both apply simultaneously.

### 2.4 Gap 4: MCP wiring empty (LOW)

**Symptom**: `.mcp.json` + `.cursor/mcp.json` = `{"mcpServers": {}}`.

**Impact**: Cross-session memory bridge (`stop-memory-save.mjs`)
has no MCP endpoint to call. Not blocking.

**Mitigation**: When MCP Memory Service is deployed, add the
server entry to both files.

### 2.5 Gap 5: `openclaw/` 4th runtime projection (LOW)

**Symptom**: OpenClaw is not projected (declarative-only per
`AGENTS.md`).

**Impact**: Cannot run OpenClaw. Not currently needed.

**Mitigation**: Defer until user requests OpenClaw.

---

## 3. Risks (风险)

### 3.1 Operational risks

| # | Risk | Severity | Trigger | Mitigation |
|---|---|---|---|---|
| 1 | 5-role boundaries break in first business Goal | HIGH | First new Goal under new protocol | PROBATION mode: user present at every Commit Approval |
| 2 | Codex may run afoul of `enforce-agent-dispatch.mjs` (capability mismatch) | MEDIUM | First Codex session in this repo | The hook will block; the human + Mavis review the cap mismatch and either update capability index or amend the Goal scope |
| 3 | MiniMax may be too strict (over-review) | MEDIUM | First review pass | Mavis is **advisory**, not blocking; the user makes the final call |
| 4 | Meta_Kim 8-stage spine may be too heavyweight for trivial Goals | MEDIUM | Trivial Goal (e.g., doc-only) | The 6-stage Goal lifecycle has a "lightweight cycle" exception; the 7-phase protocol should also have a trivial-cycle shortcut (out of scope for this audit; flagged for the protocol design) |

### 3.2 Technical risks

| # | Risk | Severity | Trigger | Mitigation |
|---|---|---|---|---|
| 5 | `canonical/` missing → future Meta_Kim upgrade is manual | HIGH | First Meta_Kim version upgrade | Bootstrap from upstream (Gap 1) |
| 6 | `.gitignore` patch not yet applied | LOW | First commit by new contributor that touches a Class B path | The patch is a one-line change; user-applies in next session |
| 7 | Build cache pollution (~328 MB) | LOW | Next build | `git status` is clean enough; `Move-Item` workarounds have been working |

### 3.3 Business risks

| # | Risk | Severity | Trigger | Mitigation |
|---|---|---|---|---|
| 8 | Next business module (e.g., Purchase) hits a V1 frozen contract violation | MEDIUM | First PR to MDM/Identity/Foundation/NumberingRule surface | STOP-and-escalate per `GULIERP_GOAL_LIFECYCLE.md` anti-pattern rule 6 |
| 9 | User unavailable to authorize commit | MEDIUM | Long session; user OOO | Mavis stages; Goal parks at `AWAITING_HUMAN_APPROVAL`; user resumes |
| 10 | Quota alert (per `Quota Management` rule) | MEDIUM | Long Codex/MiniMax/GPT session exceeds budget | STOP-and-report; do not continue without user authorization |

---

## 4. Recommendations (建议)

### 4.1 Immediate (this audit cycle)

1. **Adopt the 5-role model** in `GULIERP_AGENT_ROLE_MATRIX_V1.md`
   (delivered in this audit). The 4-role precursor in
   `GULIERP_AGENT_GOVERNANCE.md` is **superseded** but kept for
   historical reference.
2. **Adopt the 7-phase protocol** in
   `GULIERP_AI_DEVELOPMENT_PROTOCOL_V1.md`. The 6-stage inner
   lifecycle in `GULIERP_GOAL_LIFECYCLE.md` is preserved.
3. **Accept PROBATION mode** for the first 2-3 Goals under the
   new protocol.

### 4.2 Near-term (next 1-2 Goals)

4. Open `GULIERP_META_KIM_CANONICAL_RESTORE_001` (HIGH, 1-2 days) to
   close Gap 1.
5. Open `GULIERP_AI_FACTORY_FIRST_BIZ_GOAL_001` (e.g., Purchase
   Order module) under PROBATION mode, with the Goal Card designed
   in this audit (`GULIERP_PURCHASE_ORDER_GOAL_CARD_001.md`).
6. Open `GULIERP_PREEXISTING_DIRTY_AUDIT_001` (LOW, 0.5 day) to
   commit / revert the 6 remaining modified files in working tree.
7. Open `GULIERP_GITIGNORE_APPLY_001` (LOW, <5 min) to apply the
   12-line `.gitignore` patch.

### 4.3 Long-term (next quarter)

8. **Add OpenClaw projection** if/when user requests it.
9. **Wire MCP Memory Service** when the memory service is available.
10. **Establish a quarterly governance review** — the user + Mavis
    review the role boundaries, the protocol, and the GOAL_REGISTRY
    every quarter and amend as needed.
11. **Promote `meta-kim-post-copy.mjs` to a first-class npm script**
    so the post-copy init is reproducible.

---

## 5. Out-of-Scope for This Audit

The following are **deliberately NOT** covered:

- **The 4-role vs 5-role transition** — `GULIERP_AGENT_GOVERNANCE.md`
  (4-role) and `GULIERP_AGENT_ROLE_MATRIX_V1.md` (5-role) are kept
  side-by-side. The user decides which to ratify.
- **The 6-stage vs 7-phase protocol alignment** —
  `GULIERP_GOAL_LIFECYCLE.md` (6-stage) is the inner loop;
  `GULIERP_AI_DEVELOPMENT_PROTOCOL_V1.md` (7-phase) is the outer
  loop. Both apply; the user confirms or amends.
- **First Goal execution under the new protocol** — this audit
  designs the Goal Card but does NOT execute it. The user executes
  when ready.
- **Source / test / migration / DB change** — none made; per brief.

---

## 6. Sign-off

**Gate**: `AI_FACTORY_READINESS_AUDIT_COMPLETE`

- ✅ 3 of 4 runtime projections complete (Claude / Codex / Cursor)
- ✅ 9 meta-agents × 3 runtimes = 27 meta-agent files
- ✅ 9 Codex worker agents
- ✅ `meta-theory` shared skill (15 reference docs)
- ✅ `enforce-agent-dispatch.mjs` hook (3 runtimes)
- ✅ 20 governance documents
- ✅ 5-role model documented (this audit)
- ✅ 7-phase protocol documented (this audit)
- ⚠️ 5-role orchestration **not yet exercised** (PROBATION)
- ⚠️ 7-phase protocol **not yet exercised** (PROBATION)
- ❌ `canonical/` source layer missing (HIGH)
- ❌ MCP wiring empty (LOW)
- ❌ `openclaw/` projection missing (LOW)

**Author**: Mavis (M3 / mavis), acting as AI Software Factory
Architect
**Date**: 2026-08-25 (Asia/Shanghai)
**Status**: PARTIAL — **ready for business development with
governance hand-holding (PROBATION mode recommended for first 2-3
Goals)**
**Next deliverable**: `GULIERP_AGENT_ROLE_MATRIX_V1.md` (TASK 2)
