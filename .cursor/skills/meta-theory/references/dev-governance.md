# Development Governance Flow - Compact Index

This file is the compatibility anchor for Type C development governance. Detailed rules are split across sibling references:

- `path-selection.md`
- `spine-state.md`
- `runtime-codex.md`
- `owner-resolution.md`
- `verification-evidence.md`
- `planning-files.md`
- `evolution-writeback.md`

## Invocation Principle

Meta-theory starts with capability-first routing, not hardcoded names. The main thread coordinates. Execution work is delegated to the selected owner, skill, command, MCP capability, runtime tool, or worker.

Meta agents govern:

- Fetch sub-agent returns evidence, not final decisions.
- Thinking sub-agent returns plans and packets, not patches.
- Review sub-agent verifies quality and evidence.
- Execution workers produce the deliverable.

## Task Classes And No-Agent Exception

`taskClass` categories:

- `Q`: pure query / read-only explanation.
- `A`: analysis with durable reasoning or review.
- `P`: plan / proposal / architecture route.
- `S`: strategy / governance / multi-run direction.

No-agent exception applies only when all four are false: no file/code/config change, no external side effect, no durable artifact, and no handoff.

## Capability Discovery And Owner Resolution

Fetch-first pattern: Search -> Match -> Invoke.

Discovery order:

1. local repo evidence
2. canonical capability index
3. runtime mirror indexes
4. local runtime inventory
5. external search
6. specialist ecosystem
7. owner-resolution decision

Owner-resolution branches: existing owner, upgrade existing owner, create owner, or capability gap. Temporary fallback owner is forbidden. Missing capability blocks, returns to Thinking, or queues a `capabilityGapPacket` for Scout.

Default execution route proof: for a real execution demand, the route must naturally resolve the full provider chain before mutation. Evidence must name the selected execution owner, the checked execution-agent sources, the agent creation capability or reason no creation is needed, the selected skill and checked skill sources, the skill creation capability or reason no creation is needed, the MCP provider or no-impact reason, the command/runtime tool, and the verification owner plus verification method. This proof belongs to Fetch and Thinking, not to a validator after Execution.

Change facts before mutation: for any non-trivial, new-file, data-file, runtime-facing, or hook-gated file change, Fetch creates `fileChangeFactCard` before Execution. It records target files, the consumer/caller/distribution path, same-purpose overlap search and reuse decision, data fields/structure/date formats with redacted or synthetic examples when relevant, and the current user instruction verbatim. Thinking binds those facts into `workerTaskPackets` so Execution writes files because they have a delivery contract, not because the worker needed somewhere to put output.

Step 1.7 Business-flow capability matrix: for executable deliverables, Fetch expands the work into product, UX, UI, frontend, backend, database, auth/security, motion, accessibility, browser QA, performance, release, install, feedback, and evolution lanes. Each lane records needed capability, owner candidates, dependency, and omission reason. Interface Integration Contract Layer adds `interfaceIntegrationContractPacket` for `third_party_integration` and internal integration work: interface_contract, provider_adapter, permission, contract_test, observability, rollout_rollback, blocking_unknown, and auth/signature evidence.

User-visible agent naming: `roleDisplayName` is a short business role such as frontend, backend, test, review, analysis, verify, or docs. Runtime nicknames and random personal aliases belong only in `runtimeInstanceAlias`. Put shard detail in `roleInstanceId` or `shardScope`, not the visible role name.

Parallelism rule: the same owner may run multiple instances only with shardKey, shardScope, workspaceIsolation, artifactNamespace, collisionPolicy, shared `parallelGroup`, and one `mergeOwner`. Missing shard or merge evidence is fake parallelism.

## Complexity Routing

Simple routing: non-explicit, low-risk 1-file work may compress execution to Execution -> Review -> Verification -> Evolution after the dispatcher has established that no Critical, Fetch, or Thinking decision would change route, scope, risk, owner, permissions, non-goals, or acceptance. Explicit `meta-theory` activation and governed work still enter through Critical -> Fetch -> Thinking before Execution; simple routing is not a bypass for governance entry.

Medium routing: 2-5 files -> full 8-stage spine.

Complex routing: >5 files, cross-system dependency, cross-module dependency, multi-team work, install/release risk, or security-sensitive changes upgrade to 8-stage + 11-phase business workflow.

Security-sensitive changes trigger upgrade. Cross-system dependency triggers upgrade. The file scope threshold is 5 files; 6 files is complex.

Legacy 10-stage wording is not the current routing model.

## Core 8-Stage Spine

The current spine is:

Critical -> Fetch -> Thinking -> Execution -> Review -> Meta-Review -> Verification -> Evolution.

## STAGE 4: Execution

Execution dispatches from Thinking artifacts: `agentBlueprintPacket`, `dispatchEnvelopePacket`, `dispatchBoard`, and `workerTaskPackets` when they are available. Hooks enforce only the key behavior minimum: intent, Fetch evidence, capability discovery, selected owner, owner loadout, runtime/OS not known-unsupported, memory strategy, and Review standard. Selected capabilities may be agents, skills, commands, MCP capabilities, runtime tools, abstract prompts, or file-set capabilities. Execution is multi-agent when the task has independent lanes.

Stage transitions:

- Critical -> Fetch after intent, scope, non-goals, permissions, and task classification are clear. When user input is wishful or ambiguous, Critical and Fetch form a bounded loop: Critical does initial intent translation, Fetch reads project context to fill gaps, then Critical updates the intent with context-enriched understanding. The loop repeats adaptively up to `criticalFetchLoopMax` (default 3). Exit requires an IntentCard confirmation by the user (or an allowed skip with recorded reason). See `spine-state.md` Critical-Fetch Intent Loop for full field definitions.
- Fetch -> Thinking after decision-grade evidence, capability discovery, and contradictions are recorded.
- Thinking -> Execution after option exploration or `no_branching_choice`, owner/loadout selection, memory strategy, and enough dispatch evidence to execute. Full packet shape is validated by validators and Review; hooks must not block merely because optional fields are absent.
- Review -> Meta-Review -> Verification when risk or review quality needs a standard check.
- Evolution is final and cannot precede Verification.

Hidden skeleton:

- `stageState`: Critical -> Fetch -> Thinking -> Execution -> Review -> Meta-Review -> Verification -> Evolution
- `controlState`: `normal`, `skip`, `interrupt`, `override`, `iteration`, `intentional_silence`, `degraded`
- `gateState`: `pending`, `pass`, `fail`, `rework`, `blocked`
- `surfaceState`: `silent`, `notice`, `decision`

Skip / interrupt / override must return to the main chain. `controlState=skip` skips a stage only with a recorded reason; interrupt pauses the stage; iteration re-enters the stage after failed verification. `intentional_silence` means no card because intervention has no clear gain. Public readiness is not `surfaceState`; it lives in summary/public surface packets and remains blocked until verification and Warden gates close.

## Degraded Mode

When Agent dispatch is unavailable or no matching owner exists after capability discovery, the spine enters `controlState=degraded`:

- Fetch must still run discovery and record `capabilityDiscovery.searchLog`. Skipping discovery is forbidden even in degraded mode.
- Thinking must still resolve owners and record `capabilityGapPacket`. If no owner matches, `degradationReason` explains why.
- Review: read the relevant meta-agent definition (e.g. `meta-prism` for review criteria), apply the same checklist, record `reviewPacket` with `degradedFlag: true` and `reviewerRole: "main-thread-degraded"`.
- Meta-Review: same pattern as Review, reading `meta-warden` criteria.
- Verification: run verifySteps with `degradedFlag: true`, add `humanAcceptanceRequired: true` when no independent verification owner exists.
- `surfaceState` may be `silent` or `notice` but cannot become a public-ready claim. Summary/public surface packets must remain internal-only; claiming `public-ready` in degraded mode is forbidden.
- The dispatcher may self-execute in degraded mode only with explicit `degradationReason` and `degradedFlag: true` recorded before mutation.

## User Interaction Policy

Decision vs Notice bifurcation:

- Decision asks the user to choose because outcome, scope, owner, risk, or acceptance changes.
- Notice reports state without asking.

Non-trivial execution needs one consolidated Decision after Fetch and Thinking, unless the skip is `trivial`, `no_branching_choice`, or `explicit_auto_proceed` with rationale. Read-only/queryBypass is a safety and path-classification boundary; it is not enough by itself to skip a choice surface when branch-changing options exist.

Codex visible multi-option choice rule: visible Decisions include at least two options and a recommended default.

## Interactive Execution Communication

During multi-stage work, the dispatcher must communicate at natural transition points — not only at the pre-decision gate. This "communicate while working" pattern is mandatory for non-trivial tasks.

Report triggers:
- Fetch complete: brief evidence summary + route impact.
- Thinking complete: chosen path + trade-offs + why alternatives were rejected.
- Each Execution phase complete: what was done + what remains + any blockers.
- Review findings that change scope: surface immediately as a Decision card.
- Route-changing discovery mid-execution: pause and inform before continuing.

Each report is a compact notice (max 3 bullets). If a discovery changes scope, owner, or risk, upgrade to a Decision requiring user input. The user should never need to ask "what's happening?" during a non-trivial run.

## Fetch And Thinking Boundary

Fetch collects evidence and capability candidates. It does not finalize execution owners.

Thinking selects owners, expert lenses, dependencies, and worker work orders. `candidateTaskShape` may sketch lanes, but it is not `dispatchEnvelopePacket`, `dispatchBoard`, or `workerTaskPackets`.

Option Exploration is mandatory for non-trivial work: at least 2 solution paths, Pros / Cons, rejected alternatives, and a Decision Record.

## Protocol-First Dispatch Artifacts

Stage 4 may not start before:

- `runHeader`
- `taskClassification`
- `fetchPacket`
- `contentEvidencePacket`
- `fileChangeFactCard` when file mutation is planned
- `preDecisionOptionFrame`
- `dispatchEnvelopePacket`
- `dispatchBoard`
- `orchestrationTaskBoardPacket`
- `workerTaskPackets`

Protocol-first dispatch requires capability binding, allowed/blocked capabilities, review owner, verification owner, dependencies, `parallelGroup`, `mergeOwner`, and file collision policy.

## Planning Files Supplement

When planning files are enabled at Stage 3, create or update:

- `task_plan.md`
- `findings.md`
- `progress.md`

They supplement packets. They do not replace `businessFlowBlueprintPacket`, `dispatchEnvelopePacket`, `workerTaskPackets`, or verification evidence.

## Execution

Dispatch from Thinking artifacts and selected capabilities. Execution may be parallel or sequential based on dependencies. `agent-teams-playbook` is used only after Thinking and before Execution when there are 2+ independent parallel worker lanes.

Surgical hygiene: touch only files required by the task, remove only unused code caused by the change, and preserve unrelated user changes.

If a write-time hook blocks with a request for file facts, return to Fetch/Thinking, present the `fileChangeFactCard` in user-facing language, and retry the same operation. Do not disable the hook, invent fake callers, or retry unchanged without the facts.

## Review

Review validates owner coverage, protocol compliance, quality, security where selected, UX where selected, and AI-slop risks in agent/system definitions.

Reviewers must be able to perform read-only inspection and allowed validation commands. A review that cannot inspect evidence is not a real review.

Review must also check that each new or changed file has a recorded target, consumer/distribution path, overlap decision, and data-shape note where relevant.

## Verification

Verification uses fresh evidence, not "I tested it" claims. For every "verified" claim, answer:

- who tested it
- what command/check ran
- what output, screenshot, log, or artifact records the result
- what happens on failure

`workerExecutionEvidence` binds to `verifySteps` by `verifyStepRef`; `json-output` must be parseable JSON. `verificationPacket.fixEvidence` is structured and links finding, action, verifier, evidence refs, result artifact, result, and failure disposition.

`accepted_risk` can close only with `riskOwner`, `riskReason`, and `expiryOrRevisitTrigger`; public-ready must stay false unless the release gate explicitly permits that risk.

Insufficient evidence -> mark `INSUFFICIENT_EVIDENCE`, return to Verification, or reopen Review.

Live evidence classification:

- `structural_smoke`: config, schema, projection, matrix, or startup checks. Useful, but not live.
- `ui_warning_or_system_message`: visible warning, prompt injection surface, or UI/systemMessage output. Useful, but not live unless the runtime proves it entered model/tool context and affected the invocation artifact.
- `skipped_or_needs_auth`: honest incomplete state with blocker and retry path. Never a pass.
- `runtime_live_pass`: a real target-runtime invocation produced a recoverable assistant/tool artifact and passed runtime-specific scoring.

Do not combine categories to imply a stronger category. If Claude, Codex, or OpenClaw live is requested, each declared target must be either `runtime_live_pass` or explicitly incomplete; structural smoke cannot substitute for live.

## Rollback Protocol

Rollback is not failure.

Rollback decision flow: Verification FAIL -> count affected file count -> decide rollback level -> record action -> re-enter the correct stage.

Levels:

- file-level rollback: 1 file rollback, targeted revert; `git checkout` may be a human-approved option.
- sub-task rollback: 2-3 files rollback or sub-task revert.
- partial rollback: mixed success/failure; keep success and rollback fail when dependency boundaries allow it.
- full rollback: >3 files rollback, cross-module rollback, cross-contamination >3 files, or `git stash` full safety path.

Full rollback re-enters Stage 1 Critical for scope and risk reset, then may return to Stage 3 Thinking to re-decompose.

## Silence And Card Overload

Forced silence triggers at >=3 consecutive high-density push rounds. Use Pause for digestion when attention budget is exceeded. Overload rule silence is deliberate, not inaction.

Iteration cards have `max_iterations` (default 3). If `max_iterations` is exceeded, escalate to Warden instead of looping. The `interrupt_trigger` mechanism preempts the current queue only when a recorded trigger condition is satisfied.

Undefined behavior defaults to silence/pause and escalates to Warden.

## Summary And Public Display

Public display requires verified run, summary closure, single primary deliverable, closed deliverable chain, and consolidated deliverable. Do not show public-ready when verification is incomplete.

Release/publication closure has two modes. Routine low-risk patch/minor releases use the maintainer smoke path: projection sync, default capability-discovery smoke, meta-theory tests, whitespace diff check, changelog/release notes, commit, tag, push, and publish. Upgrade to the stricter release-grade chain only when the change touches install/update, global sync, hooks, runtime matrix, provider registry, dependency compatibility, runtime probes, package contents, security-sensitive behavior, or the user asks for full/live evidence. Release-grade closure records all-runtime update scope, project sync evidence, global sync evidence, global hooks evidence when hooks are in scope, runtime matrix validation, provider registry validation, dependency compatibility, runtime probe evidence or blocker, real execution-demand route proof, live runtime evidence for declared live targets, changelog/release-note readiness, security audit evidence, and Warden approval.

## Evolution

Evolution receives `verificationPacket` results and produces `evolutionWritebackPacket`. Warden approves; Chrysalis coordinates; target specialist performs writeback. No durable lesson means `writebackDecision: none`.

## What It Is Not

It is not a shortcut to hardcoded agents, a license for governance agents to implement product work, a way to skip evidence, or a way to convert a failed verification into a release claim.


## Use when

Use when full development governance and cross-runtime execution flow affects route, owner, risk, acceptance, verification, public-ready, or evolution writeback.

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
