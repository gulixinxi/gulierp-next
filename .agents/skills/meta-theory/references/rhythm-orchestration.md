# Rhythm orchestration — full reference

> Distilled into the canonical meta-theory reference set; aligns with the Meta_Kim methodology.

## Core proposition

**Mature systems must split, compose, govern — and deal cards.**

Orchestration is not only sequencing (who goes first). It includes **rhythm**: what to deliver when — and when not to.

---

## Attention cost model

### Law 1: Dealing has a cost

> **Every message to the user costs attention.**

| Scenario | Cost |
|----------|------|
| One suggestion | Uses user attention bandwidth |
| One new todo | Competes with existing todos |
| One more agent goal | Dilutes current focus |
| One more pushed task | Raises cognitive load |

Each extra message competes with prior ones. Attention, bandwidth, and throughput are finite.

### Law 2: Timing changes value

> **Mature systems do not say everything they know — they say what matters most when it matters most.**

Same information, different moment → very different value:

- Security warning while user is executing → high value (prevents harm)
- New task while user is digesting last output → low value (overload)
- Evolution suggestion right after a completed round → medium value (room to absorb)

### Law 3: Silence is design

> **Doing nothing is not always failure. Sometimes it is the optimal action.**

---

## Event card deck

### Design philosophy

From open-world quest design: **surface freedom, hidden ideal rhythm.**

The user feels free; optimal delivery order is designed. Touchpoints surface what should appear now.

### Card schema

```yaml
card:
  id: string           # e.g. "guide-01"
  type: enum           # clarify / shrink-scope / options / execute / verify / fix / rollback / risk / nudge / pause
  priority: 1-10       # default priority (10 = highest)
  cost: low|mid|high   # attention cost tier
  precondition: string # e.g. "requirements clear"
  skip_condition: string # e.g. "user already knows"
  interrupt_trigger: string # preempt condition
  delivery_shell: string   # see intent-amplification
  max_iterations: number   # for iteration cards (default 3)
```

### Ten cards (aligned with the canonical theory reference)

| Card | Original name | Trigger | Action | Attention | Philosophy |
|------|---------------|---------|--------|-----------|------------|
| **Clarify** | Clarify | Ambiguous need | ≤2 rounds of questions | low | Gather before act |
| **Shrink scope** | Scope shrink | Repo too large / many files / clashes | Narrow boundary | low | Complexity → reckless edits |
| **Options** | Options | Clear need, many paths | Routes + tradeoffs + pick | mid | Map before code |
| **Execute** | Execute | Plan done, risk OK | Assign metas, change code | high | Plan before code |
| **Verify** | Verify | Execution done | Build / types / deps / reqs | mid | First pass ≠ correct |
| **Fix** | Fix | Verify fails | Repair until pass | mid | Cap iterations |
| **Rollback** | Rollback | Risk or blast radius grows | Last stable state | high | Retreat = maturity |
| **Risk** | Risk | Shared parts / auth / global / multi-party | Surface risk; preempt | high | Safety / global first |
| **Nudge** | Nudge | User stuck, light touch | Low-cost next step | low | Helpful, not loud |
| **Pause** | Pause | Streak done / digest / ≥3 high-cost | Stop pushing; short status | zero | Designed silence |

### Dealing rules (priority order)

1. **Default**: deal by `priority`
2. **After each card**: evaluate next `skip_condition` — if true, skip
3. **After ≥3 consecutive `high` cost**: force **Pause**
4. **If `interrupt_trigger` fires**: preempting card to front
5. **Iteration cards**: at most `max_iterations`; else escalate to Warden

### Dealing flow

```
[Current card done]
  ↓
Check next skip_condition
  ├─ satisfied → skip, continue
  └─ not satisfied → check interrupt queue
       ├─ preempt → move to front
       └─ no preempt → check pause rule
            ├─ ≥3 high in a row → force Pause
            └─ else → deal next
```

---

## Seven heuristics (open world → AI systems)

### 1. Freedom on top, ideal order underneath

Orchestration meta decides not only order but **when** to speak, **when** to stay silent, what comes first vs later, when to preempt, when to skip.

**Apply**: orchestration becomes rhythm control, not only task order.

### 2. Deal interface

NPCs, boards, campsites are **delivery interfaces**, not the content itself.

**Apply**: chat, notifications, dashboards, agent replies are dealers. Some metas deliver rather than author.

| Channel | When | Attention |
|---------|------|-----------|
| Direct reply | Live interaction, immediate feedback | high |
| Write file | Large artifact, persistent, async read | low |
| Spawn subagent | Specialist work | mid |
| Wait for user | Needs input / decision | zero (waiting) |
| Notification / digest | Background work, status | low |

### 3. Pause mechanism

Pause does not advance work; it **reduces noise**, prevents overload, leaves room to digest, preserves a sense of exploration.

**Triggers**:

- ≥3 dense pushes in a row
- User did not respond to last output
- Information density above digest threshold

**During pause**:

- No new tasks
- Short status (“Progress: X/Y done”)
- Wait for user to drive next step

### 4. Cost-aware dealing

Every push competes with prior pushes.

**Rules**:

- Before push: is this more valuable *now* than in five minutes?
- If no → do not push
- If unsure → downgrade to file write (lower cost)

### 5. Skip mechanism

Skipping is attention management.

**Skip when**:

- User already knows
- Context already contains it
- Budget exhausted after dense streak → skip or downgrade

### 6. Emergency governance

Some signals can preempt the default rhythm.

| Source | Signal | Preempt type |
|--------|--------|--------------|
| Sentinel | Security / permission issue | Safety — highest |
| Prism | Severe quality drift (e.g. pass_rate < 0.5) | Quality |
| User | “Urgent” / “now” | User |
| System | Resource / timeout / error | System |

**Sentinel → Conductor**: `{type: "interrupt", source: "sentinel", severity: "critical", detail: "..."}` → pause deck, safety card to front.

**Prism → Conductor**: `{type: "interrupt", source: "prism", severity: "high", detail: "..."}` → critical now; high before next card.

### 7. Same intent, many shells

Core stable; shell swaps by scenario.

**Apply**: on each card, choose **what** to deal and **which shell** (intent amplification).

---

## Relationship to other threads

```
Meta (split)
  ↓ independent schedulable units
Organizational mirror (compose)
  ↓ structure and collaboration
Rhythm orchestration (deal) ← this layer
  ↓ paced dealing strategy
Intent amplification (deliver)
  ↓ structured output under rhythm
```

### Conductor

Conductor **executes** rhythm; this doc is the **method**. Implementation (deck data, deal function, pause/preempt) lives in the canonical `meta-conductor` agent definition and is projected into each runtime mirror.

### Warden

Escalation for pause and conflicts:

- Iteration exceeds `max_iterations` → Warden decides
- Conflicting preempts → Warden arbitrates

### Sentinel / Prism

**Send** preempt signals; Conductor receives and applies.

---

## Scenarios

### 1. Happy path

```
Clarify(low) → Shrink(low) → Options(mid) → Execute(high) → Verify(mid)
→ Fix(mid) → [evolution scan] → done
```

### 2. Overload → pause

```
Execute(high) → Verify(high) → Fix-1(high)
→ [3× high] → forced Pause → "Revision round 2: 1/3 checks passed"
→ user continues → Fix-2(mid)
```

### 3. Emergency preempt

```
Execute(high) → [Sentinel alert] → pause deck
→ Risk (highest) → fix security → resume → Verify(mid)
```

### 4. Rollback

```
Execute(high) → [scope explodes] → Rollback(high)
→ stable state → reassess → Shrink(low) → re-Execute
```


## Use when

Use when card timing, silence, interruption, and user choice rhythm affects route, owner, risk, acceptance, verification, public-ready, or evolution writeback.

## Required inputs

- Latest user request and `intentPacket`
- `fetchPacket` evidence that changes decision
- runtime and OS targets when tools or dependencies are involved
- relevant config, registry, script, or artifact path

## Do

- Assign an owner for each action.
- Produce a checkable packet or artifact.
- Bind pass/fail to evidence, threshold, or command output.
- Preserve existing foundational and native runtime capabilities.

## Do not

- Do not delete skills, dependencies, web/browser/research, shell, filesystem, apply_patch, MCP, memory, graph, hooks, scripts, runtime tools, or native platform abilities.
- Do not use vague advice without trigger, output, evidence, and writeback.
- Do not route reference-only or unknown dependencies into execution.

## Required packet

`referenceContractPacket`: `referenceId`, `trigger`, `requiredInputs`, `actions`, `outputs`, `passCriteria`, `failCriteria`, `blockConditions`, `returnStage`, `verification`, `writebackTarget`.

## Pass

- At least one action has owner, input, output, and verification.
- Pass criteria include numeric threshold, required field list, command, artifact, or human acceptance record.
- Unsupported, unknown, or partial capability is marked rather than removed.

## Fail

- Instruction is only theory or roleplay.
- No block condition exists for missing evidence, unsupported runtime/OS, fake owner, or missing verification.
- Public-ready can be claimed without userGoalDone and evidence.

## Block

Block Execution when owner, weapon, dependency eligibility, runtime support, OS support, verification owner, or rollback boundary is missing. Block public-ready when verification evidence, intent acceptance, writebackDecision, or high/critical closure is missing.

## Return to stage

Return to Critical for intent gaps, Fetch for evidence/support gaps, Thinking for route gaps, Execution for missing artifact, Review for open findings, Verification for missing proof, and Evolution for missing writeback.

## Verification

Run the most specific validator for this reference plus `npm run meta:prompt:validate`. Use command/log/artifact/human acceptance evidence, not a narrative claim.

## Writeback

Write durable improvements to canonical references, governance configs, capability indexes, validators, tests, or scars. If no durable change exists, record `none-with-reason`.

## Preserve

Preserve Skills, WebSearch/browser/research, filesystem, shell, apply_patch, MCP, memory, Graphify, graph, hooks, commands, rules, agents, subagents, approval, sandbox, runtime tools, package scripts, setup, sync, install, uninstall, status, doctor, validators, and runtime projections.
