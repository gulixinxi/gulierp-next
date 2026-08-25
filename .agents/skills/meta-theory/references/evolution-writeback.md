# Evolution Writeback

Evolution is for durable governance learning, not for routine status.

Write back only when the run changes long-term behavior, agent responsibility, install/release rules, capability indexes, hook contracts, or verification requirements.

Authority:

- Warden approves.
- Chrysalis coordinates.
- Target specialist performs writeback.

If there is no durable lesson, record `writebackDecision: none` with rationale.

## Use when

Use at run end, after verification failure, after repeated failure pattern, after dependency compatibility improves, or after prompt/governance boundary repair.

## Required inputs

- `verificationResult`
- `reviewPacket.findings`
- writeback proposal
- scar candidate
- Warden approval or rejection
- target registry or file path

## Do

- `meta-chrysalis` prepares `evolutionWritebackPacket` and `scarPacket`.
- `meta-warden` approves or rejects writeback.
- Target owner writes only its owned source.
- Generate `none-with-reason` when no durable learning exists.

## Do not

- Do not write "next time remember" without artifact.
- Do not self-approve Chrysalis writeback.
- Do not write to memory/graph/run-index as the only durable governance writeback.

## Required packet

`evolutionWritebackPacket`: `writebackDecision`, `decisionReason`, `writebacks`, `scarIds`, `nextRunReuseKey`, `wardenApproval`.

## Pass

- decision is `writeback` or `none-with-reason`.
- every scar has `failurePattern`, `preventionRule`, `test`, and `nextRunReuseKey`.
- every writeback has target and owner.

## Fail

- missing writebackDecision.
- scar lacks regression test.
- writeback target missing or self-approved.

## Block

Block public-ready if writebackDecision is missing. Block writeback if Warden approval is absent.

## Return to stage

Return to Review when failure evidence is unclear. Return to Verification when fix evidence is missing. Return to Thinking when target owner is unclear.

## Verification

Run `npm run meta:prompt:validate`, `npm run meta:foundational:validate`, and relevant governance tests.

## Writeback

Allowed targets include canonical agent prompts, canonical skill references, governance configs, capability indexes, validators, tests, and scar protocol records.

## Preserve

Preserve memory, graph, MCP, hooks, run-index, Graphify, and all foundational capabilities; writeback strengthens them instead of replacing them.
