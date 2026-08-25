# Spine State

The canonical spine is:

Critical -> Fetch -> Thinking -> Execution -> Review -> Meta-Review -> Verification -> Evolution.

## Critical-Fetch Intent Loop

Critical and Fetch may form a bounded loop to translate wishful user input into structured intent:

1. Critical: initial intent translation (low-confidence dimensions marked).
2. Fetch: read project context to fill low-confidence gaps.
3. Critical (return): update intent with context-enriched understanding.
4. If uncertain dimensions remain and loop budget allows, repeat step 2-3.
5. Present IntentCard for user confirmation.
6. Lock intent, proceed to formal Fetch -> Thinking.

Loop control fields:
- `criticalFetchLoopCount`: number of Critical-Fetch round trips completed (starts at 0).
- `criticalFetchLoopMax`: hard upper bound, default 3.
- `intentCard`: the Intent Confirmation Card presented to the user.
- `intentConfirmationState`: `pending` | `confirmed` | `corrected` | `skipped`.
- `intentConfirmationTimestamp`: when the user confirmed or corrected.
- `intentCorrectionPayload`: user's correction text when `corrected`.

Pass condition for exiting the loop: `intentConfirmationState` is `confirmed` or `skipped` (with allowed skip reason), AND all Critical required outputs are filled.

Adaptive termination: the dispatcher may exit the loop early when all intent dimensions reach high confidence, even if loop budget remains. Record `earlyExitReason` when this happens.

## Required Outputs

- Critical: `surfaceRequest`, `realProductProblem`, `realIntent`, `userPainValue`, `successCriteria`, `intentFrameAssessment`, `undecidedUserChoices`, `nonGoals`, `blockingUnknowns`, `noQuotaClarification`, `intentCard`, `intentConfirmationState`.
- Fetch: `evidence`, `decisionImpactMap`, `capabilityDiscovery`, `capabilityGap`, `contradictionLog`.
- Thinking: `designFrame`, `workType`, `expertLens`, `minimalFixPath`, `tenXPathShift`, `chosenRationale`, `omittedTenXWithReason`, `consideredLanes`, `omittedLanesWithReason`, `workerTaskPackets`, `dependencyPolicy`.
- Execution: `workerResultPackets`, `fileCompletionList`, `workerExecutionEvidence`.
- Review: `reviewPacket`.
- Meta-Review: standard checks on `reviewPacket`.
- Verification: `verificationPacket`.
- Evolution: `evolutionWritebackPacket`.
- User-facing closure: `whyChanged`, `whatChanged`, `userImpact`, `verificationEvidence`, `remainingLimits`.

## Hidden Skeleton

- `stageState`: current spine stage.
- `controlState`: normal, skip, interrupt, override, iteration, intentional_silence, degraded.
- `gateState`: pending, pass, fail, rework, blocked.
- `surfaceState`: silent, notice, decision.

These hidden state values are runtime-state fields. Public readiness is recorded separately in summary and public surface packets; do not overload `surfaceState` with `internal-ready` or `public-ready`.

Protocol packets live in `config/contracts/workflow-contract.json`.

## Degraded Mode Pass Conditions

When `controlState=degraded`:
- Fetch pass requires `capabilityDiscovery.searchLog` with checked sources and results (even empty).
- Thinking pass requires `capabilityGapPacket` with `currentAgentsChecked` and `degradationReason`.
- Review pass requires `degradedFlag: true` and `reviewerRole: "main-thread-degraded"`.
- Verification pass requires `degradedFlag: true` and `humanAcceptanceRequired: true`.
- `surfaceState` may be `silent` or `notice` but cannot become a public-ready claim. Summary and public surface packets must stay internal-only; `public-ready` is forbidden in degraded mode.


## Use when

Use when stage state, packet transitions, and gate readiness affects route, owner, risk, acceptance, verification, public-ready, or evolution writeback.

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
