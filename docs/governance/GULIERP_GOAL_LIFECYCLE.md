# GuliERP Goal Lifecycle

| Field | Value |
|---|---|
| **Document ID** | `GULIERP_GOAL_LIFECYCLE` |
| **Version** | v1 (initial) |
| **Date** | 2026-08-25 (Asia/Shanghai) |
| **Author** | Mavis (M3 / mavis), acting as Meta_Kim governance reviewer |
| **Status** | PROPOSAL — awaiting user ratification |
| **Companion** | `GULIERP_AGENT_GOVERNANCE.md` |
| **Replaces** | (none — this is the first formal Goal lifecycle spec) |

This document defines the **mandatory lifecycle** of a Goal in the GuliERP Next
project, from discovery through evolution, and the artifacts each stage must
produce.

---

## 0. Why a Goal Lifecycle?

A Goal without a lifecycle becomes:
- a permanent WIP that blocks the next Goal;
- a "frozen by accident" state with no decision recorded;
- a regression source when the next change touches the same surface;
- a knowledge black hole when the same contradiction is rediscovered in a
  later Goal.

GuliERP Next (per `GOAL_REGISTRY.md`) has historically had Goals spanning many
weeks. The lifecycle makes the **stage gates explicit** so that:
- A new Goal does not start before its entry gate is closed.
- A Goal does not move to "Verified" without a `_REPORT.md` artifact.
- A Goal does not move to "Committed" without user authorization.
- A Goal does not move to "Evolved" without a writeback decision.

---

## 1. The 6-Stage Spine

```
Discovery
   ↓
Architecture Decision
   ↓
Implementation
   ↓
Verification
   ↓
Commit Approval
   ↓
Evolution
```

Each stage has a **gate artifact** that must exist before the next stage can
begin. A stage cannot be skipped, but multiple stages can be merged in
**single-turn** execution (e.g., when the Goal is a 1-line fix with a known
fix, the Discovery + Architecture Decision stages may be implicit and the
Goal may enter Implementation directly).

| # | Stage | Owner (per `GULIERP_AGENT_GOVERNANCE.md`) | Gate Artifact | Stage Exit Criteria |
|---|---|---|---|---|
| 1 | Discovery | GPT (architect) | `docs/planning/*_ASSET_AUDIT_REPORT.md` | Scope surface mapped; known facts vs open questions distinguished |
| 2 | Architecture Decision | GPT (architect) | `docs/planning/*_ARCHITECTURE_DECISION.md` (or `*_PLAN.md` for small Goals) | One fix approach chosen, alternatives listed + rejected, freeze impact assessed |
| 3 | Implementation | Codex (executor) | Source code + tests + migration (if any) | Build 0/0; new tests pass; no regression on existing tests; surface unchanged per brief |
| 4 | Verification | Mavis (auditor) | `docs/verification/*_REPORT.md` | Local + Operator runtime evidence; Gate defined; Honest disclosures included |
| 5 | Commit Approval | Human (approver) | `git commit` (no push) | User explicitly authorizes the commit message and content |
| 6 | Evolution | Mavis (auditor) + `meta-chrysalis` | `docs/governance/EVOLUTION_WRITEBACK.md` (or `GOAL_REGISTRY.md` entry update) | Reusable patterns identified; governance rules updated; cross-Goal links recorded |

---

## 2. Goal ID Convention

Every Goal MUST have a stable ID. The convention is:

```
<PHASE>_<LANE>_<SEQUENCE>[_<SUFFIX>]
```

Examples from this project:
- `G2_DOCNO_001` — Phase G2, Lane DocumentNumber, sequence 001
- `G2_DOCNO_002` — Phase G2, Lane DocumentNumber, sequence 002
- `G2_MDM_DICT_001A` — Phase G2, Lane MDM, sub-lane Dictionary, sequence 001, sub-version A
- `G2_MDM_DICT_001B` — Phase G2, Lane MDM, sub-lane Dictionary, sequence 001, sub-version B
- `G2_MDM_UI_001E` — Phase G2, Lane MDM, sub-lane UI, sequence 001, sub-version E
- `G2_005` — Phase G2, sequence 005 (no lane prefix — pre-convention)
- `GULIERP_FOUNDATION_001` — Project, Lane Foundation, sequence 001
- `GULIERP_EMPLOYEE_MASTER_001` — Project, Lane Employee, sequence 001

For the **Gate** that ends the Goal:

```
<GOAL_ID>_<STATE>
```

States (in order):
- `_VERIFIED` — Verification stage complete, evidence collected
- `_CLOSED` — Commit Approval complete, work merged to master
- `_FROZEN` — Optional suffix indicating V1 contract freeze (e.g.,
  `MDM_000_MASTER_DATA_CONVENTION_FROZEN`)

Examples:
- `G2_DOCNO_002_ENGINE_FIX_VERIFIED`
- `G2_DOCNO_002_B3_RUNTIME_FINAL_VERIFIED`
- `MDM_000_MASTER_DATA_CONVENTION_FROZEN`

A Goal can have multiple Gates (e.g., one for the implementation, one for
the runtime verification). Each Gate is recorded in `GOAL_REGISTRY.md`.

---

## 3. Mandatory Artifacts Per Stage

### 3.1 Discovery Stage Artifacts

**Required** (one of the following):
- `docs/planning/*_ASSET_AUDIT_REPORT.md` — code / data asset survey
- `docs/planning/*_DISCOVERY.md` — for non-asset Goals
- `docs/research/*_VS_REUSE_GATE.md` — when the Goal is "build vs reuse"

**Content**:
- Surface map (files, tables, DTOs, endpoints, tests affected)
- Known facts (with evidence path)
- Ruled-out paths (with reason)
- Open questions (must be answered before Architecture Decision)
- Out-of-scope items (must be explicit)

**Size budget**: 10–30 KB; minimum 5 sections.

### 3.2 Architecture Decision Stage Artifacts

**Required** (one of the following):
- `docs/planning/*_ARCHITECTURE_DECISION.md` — for architectural Goals
- `docs/planning/*_PLAN.md` — for implementation Goals
- `docs/planning/*_REVIEW.md` — for fix Goals (lightweight plan + alternatives)

**Content**:
- Problem statement (one paragraph)
- 2–5 fix approaches, each with: description, pros, cons, risk, complexity
- Chosen approach with rationale
- V1 contract impact (does this break a frozen contract?)
- Entry gate (which previous Goal / state must be closed)
- Forbidden follow-ups (what MUST NOT be done in this Goal)
- Verification criteria (what evidence will prove success)

**Size budget**: 10–25 KB; minimum 5 sections.

### 3.3 Implementation Stage Artifacts

**Required**:
- Source code modifications (per brief scope)
- Test additions (per brief scope, minimum: regression test + happy-path test)
- Migration (if data model changes; with `*_MIGRATION_PLAN.md` if new)
- `docs/verification/*_IMPLEMENTATION_REPORT.md` (optional but recommended for
  Goals > 1 day of work)

**Forbidden in this stage** (without explicit user authorization):
- Commits (until Commit Approval stage)
- Pushes
- Schema changes (without a migration in the previous stage)
- Frozen contract modifications (without an unfreeze goal)
- Production database touches

### 3.4 Verification Stage Artifacts

**Required**:
- `docs/verification/*_VERIFICATION_REPORT.md` (or `*_RUNTIME_FINAL_REPORT.md`)

**Content**:
- Test results (numbers: passed/failed, build 0/0, etc.)
- Runtime evidence (if Operator env was used: specific log paths, screenshot
  paths, response payloads)
- Honest disclosures (failed runs, contradictions, deferred work, warnings)
- Final Gate (e.g., `G2_DOCNO_002_VERIFIED` or `G2_DOCNO_002_B3_RUNTIME_BLOCKED`)
- Recommended next action (commit / re-run / escalate / deprecate)

**Size budget**: 15–30 KB; minimum 8 sections.

### 3.5 Commit Approval Stage Artifacts

**Required**:
- `git status` showing the staged files
- `git diff --cached --check` (clean exit)
- Explicit user message `commit it` or `git commit` authorization
- `git commit` invocation (no `--push` flag)

**Forbidden in this stage** (without explicit user authorization):
- `git push`
- `git commit --amend` on already-pushed commits
- Force-push (`git push --force`)
- `git reset --hard`

### 3.6 Evolution Stage Artifacts

**Required** (one of the following):
- `docs/governance/EVOLUTION_WRITEBACK.md` — reusable pattern discovered
- `GOAL_REGISTRY.md` entry update (entry moved from "Active" to "Previous
  Active Goal (superseded)" section)
- Cross-link from this Goal to a future Goal that depends on the
  discovered pattern

**Content**:
- What was learned that generalizes beyond this Goal
- Which agent / hook / rule should be updated
- Why the update is durable (not a one-off)
- Memory write decision (User memory, Agent memory, or Project memory)

---

## 4. Stage Skip Rules

The 6 stages form a strict order, but a stage can be **collapsed** when:

| Stage | Can be collapsed with next when... |
|---|---|
| Discovery | (cannot be collapsed — must always exist) |
| Architecture Decision | Goal is a 1-line fix with a known fix and no architectural choice |
| Implementation | Goal is read-only (audit, documentation, research) |
| Verification | Goal produces no source code or test (pure documentation) |
| Commit Approval | (cannot be collapsed — human authorization is mandatory) |
| Evolution | (cannot be collapsed — writeback must be explicit) |

When stages are collapsed, the **content** of the collapsed stage is
absorbed into the next stage's artifact. For example, a 1-line fix Goal may
have only:
- `*_REVIEW.md` (combines Discovery + Architecture Decision)
- (Implementation happens in conversation, no report)
- `*_REPORT.md` (Verification)

---

## 5. Goal State Machine

```
   ┌────────────┐
   │ DISCOVERY  │  asset / scope / known facts
   └─────┬──────┘
         ↓
   ┌────────────┐
   │ DECISION   │  approach chosen
   └─────┬──────┘
         ↓
   ┌────────────┐
   │IMPLEMENTED │  code + tests + (optional migration)
   └─────┬──────┘
         ↓
   ┌────────────┐
   │ VERIFIED   │  report + Gate
   └─────┬──────┘
         ↓
   ┌────────────┐
   │ COMMITTED  │  in master
   └─────┬──────┘
         ↓
   ┌────────────┐
   │ EVOLVED    │  writeback / next Goal linked
   └────────────┘
```

Failure exits:
- `DISCOVERY → ABANDONED` — scope too large or no fix exists
- `DECISION → DEFERRED` — approach chosen but implementation blocked
- `IMPLEMENTED → FAILED` — test fails / build fails / contract broken
- `VERIFIED → BLOCKED` — runtime evidence missing
- `COMMITTED → REVERTED` — post-commit regression (rare)

Each failure state MUST produce an artifact (typically a section in the
Verification report or a dedicated `*_FAILURE_POSTMORTEM.md`).

---

## 6. Cross-Goal Links

Goals that depend on, supersede, or are blocked by other Goals MUST be
linked in the Goal description and in `GOAL_REGISTRY.md`. Example:

```
Goal: G2_DOCNO_002_ENGINE_FIX
  Depends on: G2_DOCNO_001_VERIFIED (entry gate)
  Supersedes: G2_DOCNO_001 (which had the B3 failure)
  Blocked by: (none)
  Next: G2_DOCNO_002_B3_RUNTIME_FINAL_VERIFY
```

`docs/governance/GOAL_REGISTRY.md` is the single source of truth for the
goal DAG and is updated at each Commit Approval / Evolution stage.

---

## 7. Anti-Patterns (Explicitly Forbidden)

The following Goal patterns are forbidden and trigger a stop + escalation:

1. **Goal without Discovery** — "just implement this feature" without an
   asset / scope survey. Reason: the agent will discover the surface
   mid-implementation, causing scope drift and re-work.
2. **Goal without Architecture Decision** — "just fix the bug" when multiple
   fix approaches exist (e.g., SQL change vs lifetime change vs
   application-layer fix). Reason: the chosen approach may break a frozen
   contract, or there may be a simpler fix that wasn't considered.
3. **Goal without Verification** — "the build passes, done" without a
   `_REPORT.md` that captures evidence, honest disclosures, and the
   Final Gate. Reason: the Gate is the contract with the user; without
   it, the Goal is unmeasurable.
4. **Goal that pushes without human authorization** — agent runs `git push`.
   Reason: push is irreversible in the team workflow sense (downstream
   pipelines trigger).
5. **Goal that commits test-gaming** — agent marks a failing test as `Skip`
   to make a build pass. Reason: the test is documenting a real
   contradiction; marking Skip hides the contradiction and will cause a
   future regression.
6. **Goal that modifies a frozen contract without unfreeze** — agent edits
   `FormatValidator.cs` regex, `EmployeeNo` schema, `mdm.MasterDataCodeRule`
   without an explicit unfreeze sub-goal. Reason: V1 freezes are
   coordination contracts; breaking them silently breaks every downstream
   consumer.
7. **Goal that runs > 3 turns in a blocked state** — agent retries the
   same failed action > 3 times without changing the approach. Reason:
   the action is the wrong one; the agent must surface the contradiction
   to the user and ask for guidance.

---

## 8. Compatibility with Meta_Kim 8-Stage Spine

Meta_Kim's published 8-stage spine is:

```
Critical → Fetch → Thinking → Execution → Review → Meta-Review → Verification → Evolution
```

This is the **agent-level** spine. The 6-stage Goal lifecycle in this
document is the **project-level** spine. They align as follows:

| Goal Stage (project) | Meta_Kim Stage (agent) | Notes |
|---|---|---|
| Discovery | Critical | "What is the actual ask?" |
| Architecture Decision | Fetch + Thinking | "What capability, what owner, what evidence?" |
| Implementation | Execution | "Code, test, build" |
| Verification | Review + Meta-Review + Verification | "Is the work done? Is the review standard adequate? Does it actually pass?" |
| Commit Approval | (human gate, not in Meta_Kim) | Human is above the agent |
| Evolution | Evolution | "What reusable pattern emerged?" |

The `meta-theory` skill, when active, drives the 8-stage spine inside the
Mavis thread for each Goal. The Goal lifecycle in this document is the
**outer wrapper** that connects multiple Mavis sessions and the human.

---

## 9. Example: G2_DOCNO_002 (recent Goal)

The `G2_DOCNO_002_ENGINE_FIX` Goal followed the lifecycle as follows:

| Stage | Artifact | Outcome |
|---|---|---|
| Discovery | `G2_DOCNO_001_B3_SALES_E2E_VERIFICATION_REPORT.md` (prior Goal) | Found `NpgsqlOperationInProgressException` at `DocumentNumberService.cs:217` |
| Architecture Decision | `G2_DOCNO_002_ENGINE_FIX_REVIEW.md` (21 KB) | Chose Plan A (1-line `CloseAsync`), rejected Plans B/C/D |
| Implementation | `DocumentNumberService.cs` (+1 line), `DocumentKernelConnectionFixture.cs` (+37 lines), `DocumentNumberCounterFacts.cs` (+50 lines) | Build 0/0; 16/16 integration tests pass |
| Verification | `G2_DOCNO_002_ENGINE_FIX_REPORT.md` (28 KB) | Gate: `G2_DOCNO_002_ENGINE_FIX_VERIFIED` |
| Commit Approval | (staged; awaiting user `git commit`) | 5 files staged, 1199 insertions, no push |
| Evolution | (deferred to after commit) | Update `GOAL_REGISTRY.md`; record reusable pattern in `EVOLUTION_WRITEBACK.md` |

Followed by `G2_DOCNO_002_B3_RUNTIME_FINAL_VERIFY` (Verification stage for
the API boundary):
- Implementation: (none — read-only runtime recovery)
- Verification: `G2_DOCNO_002_B3_RUNTIME_FINAL_REPORT.md` (24 KB) — Gate:
  `G2_DOCNO_VERIFIED` (SO#1 + SO#2 both 201)

---

## 10. Sign-off

**Gate**: `GULIERP_GOAL_LIFECYCLE_V1_PROPOSAL`

- ✅ 6-stage spine defined (Discovery / Architecture Decision / Implementation / Verification / Commit Approval / Evolution)
- ✅ Gate artifact per stage (6 artifacts)
- ✅ Goal ID convention (with examples)
- ✅ Final Gate naming convention (with examples)
- ✅ Stage skip rules
- ✅ Goal state machine (with failure exits)
- ✅ Cross-Goal linking rules
- ✅ 7 anti-patterns (forbidden)
- ✅ Alignment with Meta_Kim 8-stage spine
- ✅ Real example: G2_DOCNO_002 traced through the lifecycle

**Author**: Mavis (M3 / mavis)
**Date**: 2026-08-25 (Asia/Shanghai)
**Status**: PROPOSAL — awaiting user ratification
**Next deliverable**: `GULIERP_PROJECT_MEMORY_INDEX.md` (TASK 4)
