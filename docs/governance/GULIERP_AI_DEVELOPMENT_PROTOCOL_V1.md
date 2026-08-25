# GuliERP AI Development Protocol V1

| Field | Value |
|---|---|
| **Document ID** | `GULIERP_AI_DEVELOPMENT_PROTOCOL_V1` |
| **Version** | v1 (initial) |
| **Date** | 2026-08-25 (Asia/Shanghai) |
| **Author** | Mavis (M3 / mavis), acting as AI Software Factory Architect |
| **Status** | PROPOSAL — awaiting user ratification |
| **Supersedes** | `GULIERP_AI_DEVELOPMENT_PROTOCOL.md` (V0, 17 KB) is **not deleted** but is superseded by this V1 |
| **Companions** | `GULIERP_AI_FACTORY_READINESS_REPORT.md` (TASK 1), `GULIERP_AGENT_ROLE_MATRIX_V1.md` (TASK 2) |
| **Inner loop** | `GULIERP_GOAL_LIFECYCLE.md` (6-stage Discovery → Evolution) |

This document defines the **7-phase AI development protocol** for
GuliERP Next's AI Software Factory. Each phase has explicit
**inputs**, **owner**, **outputs**, and **acceptance criteria**.

The protocol is the **outer loop** (Phase 0 Intent → Phase 6
Evolution). The 6-stage Goal lifecycle from
`GULIERP_GOAL_LIFECYCLE.md` is the **inner loop** that runs inside
each phase. The two protocols compose: outer → inner → outer → ...

---

## 0. The 7-Phase Loop

```
Phase 0: Intent
    ↓
Phase 1: Goal
    ↓
Phase 2: Architecture
    ↓
Phase 3: Execution
    ↓
Phase 4: Review
    ↓
Phase 5: Verification
    ↓
Phase 6: Evolution
```

The 7 phases map to the 5-role handoff as follows:

| Phase | Primary Role | Support Roles |
|---|---|---|
| 0 Intent | Human (1-2 sentence intent) | — |
| 1 Goal | GPT | Meta_Kim (librarian context) |
| 2 Architecture | GPT | MiniMax (review) + Meta_Kim (sentinel risk check) |
| 3 Execution | Codex | Meta_Kim (conductor workflow) + MiniMax (real-time review) |
| 4 Review | MiniMax | Meta_Kim (prism drift) |
| 5 Verification | MiniMax | GPT (gate sign-off) + Meta_Kim (prism) |
| 6 Evolution | Meta_Kim (chrysalis) | MiniMax (writeback content) |

---

## 1. Phase 0 — Intent

### 1.1 Definition

The Human issues a **1-2 sentence intent** describing what they
want. The intent is **unstructured** and may reference prior
Goals, contracts, or features by name.

### 1.2 Inputs

- A user message (chat or task brief)
- (Optional) prior context from `docs/governance/`,
  `docs/architecture/`, or `GOAL_REGISTRY.md`

### 1.3 Owner

- **Human** (the user)

### 1.4 Outputs

- A chat message, task brief, or issue
- (Implicit) the user's intent is recorded in chat history

### 1.5 Acceptance Criteria

- [ ] The intent is clear enough for GPT to propose a Goal Card
- [ ] The user is present and reachable for follow-up

### 1.6 Anti-patterns

- ❌ "Just implement this" without a feature description
- ❌ "Fix the bug" without identifying which bug
- ❌ Multi-intent messages (one user message with 5 unrelated requests)

---

## 2. Phase 1 — Goal

### 2.1 Definition

GPT converts the intent into a **Goal Card** with explicit scope,
owner, success criteria, and Forbidden follow-ups.

### 2.2 Inputs

- Phase 0 intent
- `GULIERP_PROJECT_MEMORY_INDEX.md` (50+ Goals indexed for cross-Goal context)
- `GULIERP_GOAL_LIFECYCLE.md` (the inner 6-stage lifecycle)

### 2.3 Owner

- **GPT** (architect role)
- Support: **Meta_Kim** (librarian provides prior-Goal context)

### 2.4 Outputs

- A **Goal Card** file (template below)
- File path: `docs/governance/<GOAL_ID>_GOAL_CARD.md` (or
  `docs/planning/<GOAL_ID>_*_PLAN.md` for non-canonical Goals)

**Goal Card template** (minimal):
```markdown
# Goal Card: <GOAL_ID>

| Field | Value |
|---|---|
| Goal ID | <GOAL_ID> |
| Title | <short title> |
| Owner | GPT (architect) |
| Status | PROPOSAL → APPROVED → IN-FLIGHT → VERIFIED → COMMITTED → EVOLVED |
| Entry gate | <which prior Goal must be closed> |
| Scope | <in-scope items, 3-7 bullet points> |
| Out of scope | <out-of-scope items, 3-5 bullet points> |
| Forbidden follow-ups | <what MUST NOT be done in this Goal> |
| Success criteria | <how the Gate is measured> |
| Size estimate | <1h / 1d / 1w / 2w> |
| Agent分工 | GPT / Meta_Kim / Codex / MiniMax / Human |
| Execution Plan | <high-level shard list> |
| Review Plan | <what MiniMax reviews at each checkpoint> |
| Verification Plan | <how MiniMax verifies the Gate> |
```

### 2.5 Acceptance Criteria

- [ ] Goal Card exists at the canonical path
- [ ] All required fields are filled
- [ ] Entry gate is satisfied (prior Goal closed)
- [ ] Size estimate is present
- [ ] The user has reviewed the Goal Card

### 2.6 Anti-patterns

- ❌ Goal without a Goal Card
- ❌ Goal Card without an Entry gate
- ❌ Goal Card without Success criteria
- ❌ Goal Card that overlaps an active Goal
- ❌ Goal Card that violates a prior Goal's Forbidden follow-up

---

## 3. Phase 2 — Architecture

### 3.1 Definition

GPT produces the **architecture decision** for the Goal: which
files change, which contracts apply, which tests are required,
which migrations are needed, which V1 freezes are touched.

### 3.2 Inputs

- Phase 1 Goal Card
- `docs/architecture/` (frozen standards + drafts)
- `docs/business/` (frozen models)
- V1 frozen contract list (per `GULIERP_PROJECT_MEMORY_INDEX.md`)

### 3.3 Owner

- **GPT** (architect role)
- Support: **MiniMax** (architecture review) + **Meta_Kim** (sentinel
  risk check against V1 freezes)

### 3.4 Outputs

- `docs/planning/<GOAL_ID>_*_ARCHITECTURE_DECISION.md` (or
  `*_REVIEW.md` for fixes)
- Sections: problem statement, 2+ approaches, chosen approach,
  V1 freeze impact, Entry gate, Forbidden follow-ups, Verification criteria

### 3.5 Acceptance Criteria

- [ ] Architecture document exists
- [ ] 2+ approaches considered (or rationale for single approach)
- [ ] V1 freeze impact assessed (zero / with mitigation)
- [ ] MiniMax has reviewed the architecture (sign-off in the doc)
- [ ] The user has approved the architecture

### 3.6 Anti-patterns

- ❌ Architecture that violates a V1 freeze (without explicit unfreeze)
- ❌ Architecture that touches 3+ unrelated modules
- ❌ Architecture without an alternative considered

---

## 4. Phase 3 — Execution

### 4.1 Definition

Codex implements the Goal: source code, tests, migration (if
applicable), and local build / test verification. Codex may
dispatch parallel shards (per `GULIERP_GOAL_LIFECYCLE.md` §5
parallelism rules).

### 4.2 Inputs

- Phase 2 architecture document
- Phase 1 Goal Card
- `enforce-agent-dispatch.mjs` hook (capability enforcement)
- `enforce-agent-dispatch` capability index
  (`meta-kim-capabilities.json`)

### 4.3 Owner

- **Codex** (executor role)
- Support: **Meta_Kim** (conductor workflow) + **MiniMax** (real-time review on hot files)

### 4.4 Outputs

- Source code modifications (per the architecture)
- Test additions
- Migration (if applicable) with `*_MIGRATION_PLAN.md` entry
- Local build result (0 errors, 0 warnings, all tests pass)
- (No commit yet; Codex does NOT commit)

### 4.5 Acceptance Criteria

- [ ] Build: 0 errors, 0 warnings
- [ ] New tests: pass
- [ ] No regression on existing tests
- [ ] No scope drift (changes within declared scope)
- [ ] No V1 frozen contract touched without explicit authorization
- [ ] Local runtime smoke (if applicable) passes

### 4.6 Anti-patterns

- ❌ `git commit` by Codex (Human does this)
- ❌ Test marked `Skip` to make a build pass
- ❌ Scope drift (> 50 lines outside declared scope)
- ❌ V1 frozen contract touched without unfreeze

---

## 5. Phase 4 — Review

### 5.1 Definition

MiniMax reviews the Codex output: code review, test review,
documentation review, architecture stress-test. The review is
**focused on whether the Goal's Success criteria are met**, not
on subjective style.

### 5.2 Inputs

- Phase 3 Codex output (staged changes, local test results)
- Phase 1 Goal Card (Success criteria)
- Phase 2 Architecture document
- V1 frozen contract list

### 5.3 Owner

- **MiniMax** (audit role)
- Support: **Meta_Kim** (`meta-prism` for drift detection)

### 5.4 Outputs

- Review findings (file:line:rule references)
- Risk register updates (if new risks surface)
- Architecture stress-test results
- Review sign-off (or BLOCKED with reasons)

### 5.5 Acceptance Criteria

- [ ] Review covers all Success criteria from the Goal Card
- [ ] Findings are file:line:rule-anchored (no "looks wrong" findings)
- [ ] Drift detection runs (`meta-prism`)
- [ ] Honest disclosure of: failures, contradictions, deferred work
- [ ] Sign-off or BLOCKED is explicit

### 5.6 Anti-patterns

- ❌ "Looks good" without file:line evidence
- ❌ Subjective style criticism (this is a code review, not a beauty contest)
- ❌ Patching the code (MiniMax stays read-only; Codex does the fix)
- ❌ Marking Phase 5 as complete from Phase 4

---

## 6. Phase 5 — Verification

### 6.1 Definition

MiniMax writes the **Verification report** that closes the Goal's
Gate. The report is the durable evidence that the Goal was done
right. It is also the document that `GOAL_REGISTRY.md` references.

### 6.2 Inputs

- Phase 4 Review findings
- Phase 3 Codex output (final, after any fixes from review)
- Phase 1 Goal Card (Success criteria)
- All test results, build results, runtime smoke results

### 6.3 Owner

- **MiniMax** (audit role)
- Support: **GPT** (architect sign-off) + **Meta_Kim** (`meta-prism` aggregation)

### 6.4 Outputs

- `docs/verification/<GOAL_ID>_*_VERIFICATION_REPORT.md`
- Sections: test results (numbers), runtime evidence, honest
  disclosures, **Final Gate** (e.g., `<GOAL_ID>_VERIFIED` or
  `<GOAL_ID>_BLOCKED`), recommended next action

### 6.5 Acceptance Criteria

- [ ] Verification report exists at the canonical path
- [ ] Final Gate is stated explicitly
- [ ] Test results are concrete numbers (e.g., "44/44 PASS", "0 errors / 0 warnings")
- [ ] Runtime evidence is present (if Operator env was used)
- [ ] Honest disclosures are present (failures / contradictions / deferred)
- [ ] Recommended next action is stated (commit / re-run / escalate)

### 6.6 Anti-patterns

- ❌ Verification report without numbers
- ❌ Verification report without Final Gate
- ❌ Marking a Goal verified when any V1 contract is being broken
- ❌ Hiding failures or contradictions in the report

---

## 7. Phase 6 — Evolution

### 7.1 Definition

The Goal is **committed and closed**. Meta_Kim (chrysalis) writes
the **evolution writeback** if a reusable pattern was discovered.
The user commits and pushes.

### 7.2 Inputs

- Phase 5 Verification report (Goal is verified)
- The user has authorized the commit

### 7.3 Owner

- **Meta_Kim** (`meta-chrysalis`) for evolution writeback
- **Human** for `git commit` + `git push`
- Support: **MiniMax** for writeback content

### 7.4 Outputs

- 1+ `git commit` (Human executes)
- (Optional) `git push` to remote (Human authorizes separately)
- `GOAL_REGISTRY.md` updated (move from "Active" to "Previous Active")
- (Optional) `docs/governance/EVOLUTION_WRITEBACK.md` if a reusable
  pattern emerged
- Memory write decision (User / Agent / Project memory)

### 7.5 Acceptance Criteria

- [ ] `git commit` succeeded with a conventional commit message
- [ ] `git push` authorized (if applicable)
- [ ] `GOAL_REGISTRY.md` updated
- [ ] Evolution writeback written (if a pattern was discovered)
- [ ] The Goal moves to "Previous Active" in `GOAL_REGISTRY.md`
- [ ] Next Mainline is recorded

### 7.6 Anti-patterns

- ❌ `git commit --amend` on a pushed commit
- ❌ `git push --force`
- ❌ Committing without the user's explicit authorization
- ❌ Marking the Goal as EVOLVED before the commit + push + GOAL_REGISTRY update

---

## 8. The Inner Loop (Goal Lifecycle)

Each phase above may invoke the **6-stage inner Goal lifecycle** from
`GULIERP_GOAL_LIFECYCLE.md`:

```
Discovery → Architecture Decision → Implementation →
Verification → Commit Approval → Evolution
```

The inner loop runs **inside** a phase. For example, Phase 3
Execution may contain:

```
Phase 3:
  Discovery: Codex reads the Phase 2 architecture
  Architecture Decision: Codex confirms the file-level plan
  Implementation: Codex writes the code
  Verification: Codex runs local tests
  Commit Approval: (NOT yet — that's Phase 6)
  Evolution: (NOT yet — that's Phase 6)
```

The inner loop is a **finer-grained** control within each phase.
The outer loop is the **goal-to-goal handoff**.

---

## 9. Trivial-Goal Shortcut

For Goals that are:
- < 1 hour of work
- Pure documentation (no source change)
- Pure verification (no code change)
- Pure governance (no source / DB / migration change)

The 7 phases collapse to:

```
GPT proposes (1 message) → Mavis produces artifact (1 session) → Human approves
```

No Codex execution, no MiniMax full review, no Meta_Kim full
activation. Examples:
- `GULIERP_META_KIM_GOVERNANCE_AUDIT` (this audit cycle)
- `GULIERP_PROJECT_MEMORY_INDEX` (the 50+ Goals index)
- `GULIERP_WORKSPACE_CLEANUP_PLAN` (the .gitignore proposal)

These are typically **read-only** Goals (analysis + document
creation only) and are exempt from the full 7-phase cycle.

---

## 10. Compatibility with Meta_Kim 8-Stage Spine

The 7-phase outer protocol is the **project-level** spine. The
Meta_Kim 8-stage spine is the **agent-level** spine that runs
**inside** Mavis's thread when the `meta-theory` skill is
activated. They align as follows:

| Project Phase (this doc) | Meta_Kim Agent Stage | Notes |
|---|---|---|
| 0 Intent | — | Human-only |
| 1 Goal | Critical | "What is the actual ask?" |
| 2 Architecture | Fetch + Thinking | "What capability, what owner?" |
| 3 Execution | Execution | "Code, test, build" |
| 4 Review | Review + Meta-Review | "Is the work done? Is the review standard adequate?" |
| 5 Verification | Verification | "Does it actually pass?" |
| 6 Evolution | Evolution | "What reusable pattern emerged?" |

When `meta-theory` is active, the Mavis thread drives the 8-stage
spine inside phases 2-6. This document is the **outer wrapper**
that connects multiple Mavis sessions and the human.

---

## 11. Real-World Example: G2_DOCNO_002 (V0 protocol)

The `G2_DOCNO_002_ENGINE_FIX` Goal followed the **V0** (precursor)
6-stage lifecycle:

| Phase (V0) | Artifact | Outcome |
|---|---|---|
| Discovery | `G2_DOCNO_001_B3_SALES_E2E_VERIFICATION_REPORT.md` (prior Goal) | Found `NpgsqlOperationInProgressException` at `DocumentNumberService.cs:217` |
| Architecture Decision | `G2_DOCNO_002_ENGINE_FIX_REVIEW.md` (21 KB) | Chose Plan A (1-line `CloseAsync`), rejected Plans B/C/D |
| Implementation | `DocumentNumberService.cs` (+1 line), `DocumentKernelConnectionFixture.cs` (+37 lines), `DocumentNumberCounterFacts.cs` (+50 lines) | Build 0/0; 16/16 integration tests pass |
| Verification | `G2_DOCNO_002_ENGINE_FIX_REPORT.md` (28 KB) | Gate: `G2_DOCNO_002_ENGINE_FIX_VERIFIED` |
| Commit Approval | (staged; user authorized) | 5 files committed (`ecf613e`) |
| Evolution | (deferred to after commit) | Update `GOAL_REGISTRY.md`; record reusable pattern in `EVOLUTION_WRITEBACK.md` |

Followed by `G2_DOCNO_002_B3_RUNTIME_FINAL_VERIFY`:
- Implementation: (none — read-only runtime recovery)
- Verification: `G2_DOCNO_002_B3_RUNTIME_FINAL_REPORT.md` (24 KB) — Gate:
  `G2_DOCNO_VERIFIED` (SO#1 + SO#2 both 201)

This is an example of the V0 protocol working in practice. The V1
protocol (this document) is a **refinement** of V0 with explicit
Meta_Kim / MiniMax separation and 7 phases.

---

## 12. Sign-off

**Gate**: `GULIERP_AI_DEVELOPMENT_PROTOCOL_V1_PROPOSAL`

- ✅ 7 phases defined (Intent / Goal / Architecture / Execution / Review / Verification / Evolution)
- ✅ Each phase has explicit Inputs / Owner / Outputs / Acceptance Criteria / Anti-patterns
- ✅ Inner 6-stage Goal lifecycle cross-referenced
- ✅ Trivial-Goal shortcut documented
- ✅ Meta_Kim 8-stage spine cross-referenced
- ✅ V0 example (G2_DOCNO_002) traced through the protocol
- ✅ 5-role ownership matrix (per `GULIERP_AGENT_ROLE_MATRIX_V1.md`)

**Author**: Mavis (M3 / mavis)
**Date**: 2026-08-25 (Asia/Shanghai)
**Status**: PROPOSAL — awaiting user ratification
**Next deliverable**: Purchase Order Goal Card (TASK 4)
