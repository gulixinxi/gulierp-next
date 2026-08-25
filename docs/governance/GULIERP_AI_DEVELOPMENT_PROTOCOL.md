# GuliERP AI Development Protocol

| Field | Value |
|---|---|
| **Document ID** | `GULIERP_AI_DEVELOPMENT_PROTOCOL` |
| **Version** | v1 (initial) |
| **Date** | 2026-08-25 (Asia/Shanghai) |
| **Author** | Mavis (M3 / mavis), acting as Meta_Kim governance reviewer |
| **Status** | PROPOSAL — awaiting user ratification |
| **Companions** | `GULIERP_AGENT_GOVERNANCE.md`, `GULIERP_GOAL_LIFECYCLE.md` |

This document defines the **continuous loop** by which GuliERP Next's
Goals are produced. It is the operational counterpart to the role
specification (`GULIERP_AGENT_GOVERNANCE.md`) and the lifecycle
(`GULIERP_GOAL_LIFECYCLE.md`).

---

## 0. The Continuous Loop

```
┌─────────┐   propose   ┌─────────┐  decompose  ┌─────────┐
│   GPT   │ ──────────→ │ Meta_Kim│ ──────────→ │  Codex  │
│ architect│             │ Mavis   │             │ executor│
└────▲────┘             └────┬────┘             └────┬────┘
     │                       │ review                │ produce
     │                       ▼                       ▼
     │                  ┌─────────┐            ┌─────────┐
     └────── approve ───│  Mavis  │←─ verify ──│  Codex  │
                         │  audit  │            │ (impl)  │
                         └────┬────┘            └─────────┘
                              │ sign-off
                              ▼
                         ┌─────────┐
                         │  Human  │
                         │ final   │
                         │ approval│
                         └────┬────┘
                              │ commit
                              ▼
                         (next Goal)
```

The cycle is **per-Goal**, not per-session. A single Goal may span
multiple Mavis sessions, multiple Codex sessions, and multiple Human
turns. The protocol is the **glue** that holds the multi-session work
together.

---

## 1. Phase 1 — Goal Proposal (GPT → Human)

### 1.1 Trigger

A Goal is proposed when:
- The user issues a high-level intent ("add CRM module", "fix the
  numbering bug", "document the architecture")
- A closed Goal's `Next Goal` field points to a candidate
- An open contradiction surfaces (e.g., two tests fail with conflicting
  contracts; a frozen contract is found to be broken)

### 1.2 Artifacts Produced

- `docs/planning/<GOAL_ID>_*_DISCOVERY.md` (or `_ASSET_AUDIT_REPORT.md`)
  — initial scope survey
- `docs/planning/<GOAL_ID>_*_ARCHITECTURE_DECISION.md` (or `_PLAN.md` or
  `_REVIEW.md`) — fix approach chosen, alternatives listed
- Chat-thread proposal with: Goal ID, problem statement, proposed fix,
  expected Gate, size estimate

### 1.3 Owner

- **GPT (architect)** drafts the proposal
- **Mavis (auditor)** may pre-review the proposal if the Goal is
  architecturally sensitive
- **Human** may amend, defer, or reject the proposal

### 1.4 Stop Conditions

- Goal is too large (> 1 week estimated) → split into sub-Goals
- Goal is too small (< 1 hour estimated) → merge with the next Goal
- Goal requires business decision the user has not made → stop and ask
- Goal is forbidden by a previous Goal's `Forbidden follow-up` clause →
  surface the conflict and stop

---

## 2. Phase 2 — Capability Discovery + Decomposition (Mavis)

### 1.1 Trigger

Goal is approved by the Human.

### 2.2 Activities

- **Capability discovery**: search `config/capability-index/`, runtime
  capability-index mirrors, `canonical/agents/`, runtime agent rosters,
  skills, MCP tools, commands, and prior Goal history for a reusable
  capability match.
- **Decomposition**: split the Goal into **execution shards** (Codex
  shards or Mavis audit shards). Each shard has a clear deliverable,
  scope, and acceptance criteria.
- **Owner assignment**: each shard is assigned an owner agent. If no
  owner exists, Mavis flags the gap and proposes a new agent.
- **Dependency graph**: which shards can run in parallel; which must
  serialize; which depend on a Human input.

### 2.3 Artifacts Produced

- Mavis-internal `taskClassification` + `cardPlanPacket` +
  `businessFlowBlueprintPacket` + `agentBlueprintPacket` +
  `dispatchEnvelopePacket` packets (per Meta_Kim 8-stage spine)
- `docs/planning/<GOAL_ID>_*_EXECUTION_PLAN.md` (optional, only if the
  Goal is large enough to warrant a separate document)

### 2.4 Owner

- **Mavis (auditor)** runs the discovery + decomposition
- **GPT (architect)** may validate the architecture-mapping part of the
  plan

### 2.5 Stop Conditions

- No agent / skill / tool can be found for a required capability → flag
  and ask the user
- Decomposition produces > 5 parallel shards → simplify or split into
  sub-Goals
- Decomposition produces < 2 shards (i.e., the Goal is trivial) → use
  the lighter-weight 4-stage cycle (see §7)

---

## 3. Phase 3 — Execution (Codex + Mavis)

### 3.1 Trigger

Mavis has dispatched the shards.

### 3.2 Activities

- **Codex (executor)** runs the implementation shard(s):
  - Source code modifications per the plan
  - Test additions
  - Migration (if applicable)
  - Local build (`dotnet build`) + local tests (`dotnet test`)
- **Mavis (auditor)** runs the audit shard(s):
  - Code review
  - Test review
  - Documentation review
  - Architecture stress-test

### 3.3 Parallelism Rules

- Multiple Codex shards can run in parallel if they touch **disjoint
  files** (no overlap).
- Multiple Mavis audit shards can run in parallel.
- **One writer per file at a time.** If two shards both want to edit
  the same file, they must serialize.
- A Mavis audit shard MUST wait for the corresponding Codex shard to
  complete (or the audit can run on staged changes mid-way, but it
  must be repeated on the final state).

### 3.4 Artifacts Produced

- Source code modifications (staged in git, NOT committed)
- Test additions (staged, NOT committed)
- Mavis-internal review packets (per Meta_Kim 8-stage spine)

### 3.5 Owner

- **Codex (executor)** for implementation shards
- **Mavis (auditor)** for audit shards
- **Human** is **NOT** involved in this phase (Human enters at Phase 5)

### 3.6 Stop Conditions

- A shard produces > 50 lines of changes outside the Goal's declared
  scope → flag scope drift, stop, ask for re-scope
- A shard's tests fail in a way that the fix is not obvious → stop,
  surface the failure, ask the user
- A shard touches a V1 frozen contract without explicit unfreeze → stop,
  escalate to GPT
- A shard's build takes > 10 min without progress → kill, retry with
  fresh state
- A quota alert fires (per `Quota Management` rule) → stop, report
  to user

---

## 4. Phase 4 — Verification (Mavis)

### 4.1 Trigger

Codex reports the shards are complete and self-tests pass locally.

### 4.2 Activities

- **Mavis (auditor)** runs the Verification stage (per
  `GULIERP_GOAL_LIFECYCLE.md`):
  - Re-run all tests (including regression on prior Goals)
  - Run a clean build
  - Verify Gate evidence (if Operator runtime required, document
    the gap; otherwise run the smoke verification)
  - Write `docs/verification/<GOAL_ID>_*_VERIFICATION_REPORT.md`
  - State the Final Gate explicitly
  - State the recommended next action (commit / re-run / escalate)

### 4.3 Artifacts Produced

- `docs/verification/<GOAL_ID>_*_VERIFICATION_REPORT.md` (mandatory)

### 4.4 Owner

- **Mavis (auditor)** runs Verification single-handedly
- **GPT (architect)** may be consulted if a Gate is ambiguous

### 4.5 Stop Conditions

- Any test fails that should pass → stop, do NOT mark the Goal verified
- The Final Gate cannot be defined cleanly → stop, escalate to GPT
  + Human

---

## 5. Phase 5 — Approval (Human)

### 5.1 Trigger

Mavis posts the Verification report to the user with a `Final Gate`.

### 5.2 Activities

- **Human** reads the report
- **Human** decides: approve / reject / amend
- If approve: Human explicitly authorizes the commit message + content
- If reject: Human states the reason; loop back to Phase 2 or 3
- If amend: Human states the amendment; loop back to Phase 2 or 3

### 5.3 Artifacts Produced

- `git commit` invocation (by Human, not by agent)
- A `*_APPROVAL.md` or chat-thread confirmation (for audit trail)

### 5.4 Owner

- **Human** is the sole decision-maker in this phase
- **Mavis** may be asked for clarifications but does NOT push for
  approval

### 5.5 Stop Conditions

- Human is unavailable → agent parks the Goal in `GOAL_REGISTRY.md`
  with status `AWAITING_HUMAN_APPROVAL` and waits
- Human requests a re-run → loop back to Phase 2

---

## 6. Phase 6 — Commit + Evolution (Mavis + Human)

### 6.1 Trigger

Human approves the commit.

### 6.2 Activities

- **Human** runs `git commit` (no `--push`)
- **Mavis** updates `GOAL_REGISTRY.md`: move Goal from "Active" to
  "Previous Active Goal (superseded)" section
- **Mavis** writes the Evolution writeback if a reusable pattern was
  discovered (per `GULIERP_GOAL_LIFECYCLE.md` Evolution stage):
  - `docs/governance/EVOLUTION_WRITEBACK.md` (if a Meta_Kim update is
    needed)
  - Cross-link to a future Goal that depends on the discovered pattern
  - Memory write decision (User / Agent / Project memory)
- **Human** decides when to `git push` (typically batched, not per-Goal)

### 6.3 Artifacts Produced

- 1+ git commit(s) on `master`
- `GOAL_REGISTRY.md` updated
- Optionally: `docs/governance/EVOLUTION_WRITEBACK.md` (if a pattern
  is reusable)

### 6.4 Owner

- **Human** is the sole committer
- **Mavis** runs the post-commit housekeeping

### 6.5 Stop Conditions

- `git commit` fails (e.g., pre-commit hook rejects) → Human reviews
  the hook error and decides
- `git push` is needed urgently → Human authorizes; this is rare and
  should be explicit

---

## 7. Lightweight Cycle (Trivial Goals)

For Goals that are:
- < 1 hour of work
- Pure documentation (no source change)
- Pure verification (no code change)
- Pure governance (no source / DB / migration change)

The 6-phase cycle collapses to:

```
GPT proposes (1 message) → Mavis produces artifact (1 session) → Human approves
```

No Codex execution, no parallel shards, no Meta_Kim 8-stage spine.
Examples:
- `GULIERP_AGENT_GOVERNANCE` (this document)
- `GULIERP_PROJECT_MEMORY_INDEX` (this audit bundle)
- `GULIERP_WORKSPACE_CLEANUP_PLAN` (this audit bundle)

These are typically **read-only** Goals (analysis + document creation
only) and are exempt from the full cycle.

---

## 8. Multi-Session Continuity

A single Goal may span **multiple Mavis sessions** because of:
- Session timeout / context exhaustion
- Quota alert requiring stop + resume later
- User interruption (user works on something else, then returns)
- Operator runtime evidence that takes days to collect

For multi-session Goals, the protocol is:

1. **Mavis** writes a `task_plan.md` + `findings.md` + `progress.md` set
   (per `planning-with-files` skill) at the start of each session
2. **Mavis** updates these files at every stage transition
3. **Mavis** writes a session-end summary to native memory
4. **Human** can resume the Goal at any time by quoting the Goal ID
5. **Mavis** reads `GOAL_REGISTRY.md` + native memory to recover the
   Goal state on resume

---

## 9. Example: G2_DOCNO_002 (the Goal in flight during this audit)

This Goal is a **mid-weight cycle** (1-2 days of work):

| Phase | Activity | Owner | Output |
|---|---|---|---|
| 1 | Goal proposed by user message "执行 G2_DOCNO_002_ENGINE_FIX" | Human (proposed) | (chat input) |
| 2 | Mavis reviewed trigger report, plan review (G2_DOCNO_002_ENGINE_FIX_REVIEW.md) | Mavis | Plan accepted |
| 3 | Codex applied 1-line fix; added 2 test files; ran 44/44 unit + 16/16 integration | Codex | Staged changes (M 3 files) |
| 4 | Mavis wrote engine fix report (28 KB) and B3 final report (24 KB) | Mavis | 2 reports, Gate `G2_DOCNO_002_ENGINE_FIX_VERIFIED` + `G2_DOCNO_VERIFIED` |
| 5 | (in flight — awaiting user `commit it`) | Human | (awaiting) |
| 6 | (deferred until commit) | Mavis + Human | (awaiting) |

This Goal also generated **the audit bundle you are reading now**
(`GULIERP_META_KIM_GOVERNANCE_*`) as a **side Goal** (lightweight cycle,
pure documentation, no source change).

---

## 10. Handoff Checklists

### 10.1 GPT → Mavis (Phase 1 → Phase 2)

- [ ] Goal ID assigned
- [ ] Problem statement in 1 paragraph
- [ ] 2+ fix approaches listed (if applicable)
- [ ] Chosen approach with rationale
- [ ] Entry gate stated
- [ ] Forbidden follow-ups stated
- [ ] Gate criteria stated
- [ ] Size estimate stated
- [ ] User approval obtained

### 10.2 Mavis → Codex (Phase 2 → Phase 3)

- [ ] Shards defined with disjoint file ownership
- [ ] Each shard has a clear deliverable
- [ ] Each shard has acceptance criteria
- [ ] Each shard has an owner agent
- [ ] Dependency graph drawn
- [ ] Forbidden follow-ups restated to Codex

### 10.3 Codex → Mavis (Phase 3 → Phase 4)

- [ ] Source / test / migration changes staged
- [ ] Local build passes (0 errors, 0 warnings)
- [ ] Local tests pass (numbers reported)
- [ ] No new regression on prior Goals' tests
- [ ] No scope drift (changes within declared scope)
- [ ] No frozen contract touched without explicit authorization

### 10.4 Mavis → Human (Phase 4 → Phase 5)

- [ ] `docs/verification/<GOAL_ID>_*_REPORT.md` written
- [ ] Final Gate stated explicitly
- [ ] Recommended next action stated
- [ ] Honest disclosures included (failures, contradictions, deferred work)
- [ ] All required evidence (test numbers, build, runtime) included

### 10.5 Human → Mavis (Phase 5 → Phase 6)

- [ ] User explicitly authorizes the commit
- [ ] Commit message agreed
- [ ] Commit content reviewed (`git diff --cached --check` clean)
- [ ] No `git push` without separate authorization

### 10.6 Mavis → (next Goal) (Phase 6 → end)

- [ ] `GOAL_REGISTRY.md` updated
- [ ] Goal status moved from "Active" to "Previous"
- [ ] Evolution writeback written (if applicable)
- [ ] Next Mainline recorded

---

## 11. Failure Modes and Recovery

| Failure | Recovery |
|---|---|
| GPT proposes a Goal that overlaps an active Goal | Mavis detects via `GOAL_REGISTRY.md`; loop back to Phase 1; merge or sequence |
| Codex shard's local tests pass but Operator runtime fails | Mavis reports; loop back to Phase 3; fix and re-test |
| Mavis Verification finds a regression on a prior Goal | Stop; flag the regression; do NOT mark the new Goal verified; loop back to Phase 3 |
| Human rejects the commit | Loop back to Phase 2 or 3 per the rejection reason; re-verify after fix |
| Quota alert fires mid-cycle | Stop, report to user, wait for user decision; per `Quota Management` rule, do NOT continue without explicit authorization |
| `git push` accidentally executed | Stop, surface the error, ask user to decide whether to force-push back (rare, requires user) |
| Multiple Codex shards collide on the same file | Mavis serializes (one shard waits for the other to commit-stash-unstash) |
| Mavis audit fails to define a clean Gate | Escalate to GPT; do NOT mark verified |

---

## 12. Compatibility with Meta_Kim 8-Stage Spine

The 6 phases in this document map to Meta_Kim's 8-stage spine as follows:

| Phase (this doc) | Meta_Kim Stage | Notes |
|---|---|---|
| 1 (Goal Proposal) | Critical | "What is the actual ask?" |
| 2 (Capability Discovery) | Fetch + Thinking | "What capability, what owner?" |
| 3 (Execution) | Execution | "Code, test, build" |
| 4 (Verification) | Review + Meta-Review + Verification | "Is the work done? Is the review standard adequate? Does it actually pass?" |
| 5 (Approval) | (Human gate) | Human is above the agent layer |
| 6 (Commit + Evolution) | Evolution | "What reusable pattern emerged?" |

When `meta-theory` skill is active, the Mavis thread drives the
8-stage spine inside Phase 3 (Execution) and Phase 4 (Verification).
This document is the **outer wrapper** that connects multiple
Mavis sessions and the human.

---

## 13. Sign-off

**Gate**: `GULIERP_AI_DEVELOPMENT_PROTOCOL_V1_PROPOSAL`

- ✅ 6-phase continuous loop defined
- ✅ Each phase has trigger, activities, artifacts, owner, stop conditions
- ✅ Parallelism rules (disjoint file ownership, one writer per file)
- ✅ Multi-session continuity (planning files + memory writeback)
- ✅ Lightweight cycle for trivial Goals
- ✅ 6 handoff checklists
- ✅ 8 failure modes with recovery
- ✅ Compatibility with Meta_Kim 8-stage spine
- ✅ Real example: G2_DOCNO_002 traced through the protocol

**Author**: Mavis (M3 / mavis)
**Date**: 2026-08-25 (Asia/Shanghai)
**Status**: PROPOSAL — awaiting user ratification
**Next deliverable**: `GULIERP_META_KIM_GOVERNANCE_READY_REPORT.md` (TASK 7 / final)
