---
name: meta-theory
description: Run the Meta_Kim meta-theory governance dispatcher
args: <request>
---

Use the Meta_Kim `meta-theory` skill for this request:

$ARGUMENTS

Resolve the skill from the first available directory skill root:

1. `~/.codex/skills/meta-theory/SKILL.md`
2. `.agents/skills/meta-theory/SKILL.md`

Follow the skill's clarity, capability-discovery, dispatch, review, verification, and evolution discipline. If a required runtime capability is missing, state the missing capability and the exact checked path instead of guessing.

Default product runtime path:

For governed execution that should produce an auditable artifact, run:

```bash
npm run meta:theory:run -- "$ARGUMENTS"
```

Then use `npm run meta:theory:report -- latest` or the returned runId to reopen the user-readable report. This is the default artifact path for `/meta-theory` governed execution: Warden entry gate -> Conductor orchestration -> CapabilityGap decisions -> workerTaskPackets -> runtime projection evidence -> Warden writeback decision -> run report.

Codex execution rule:

**DISPATCH IS MANDATORY.** The main thread is the dispatcher, never the executor. Before producing >3 sentences of execution-layer analysis yourself, STOP and dispatch via `spawn_agent` instead.

- This `/meta-theory` invocation is explicit user authorization to use Codex sub-agent delegation and parallel agent work.
- Use `agent-teams-playbook` only after Thinking and before Execution when the plan has 2+ independent parallel worker lanes. Resolve it from the first available skill root (`~/.codex/skills/agent-teams-playbook/SKILL.md`, `.agents/skills/agent-teams-playbook/SKILL.md`).
- Then use Codex `spawn_agent` with capability-matched Meta_Kim agents. The main thread clarifies, routes, verifies, and synthesizes; it must not do multi-agent execution work by itself.
- If `spawn_agent` is unavailable, record the blocked reason — do not silently continue as main-thread execution.

## Prompt Acceptance

This command adapter binds `governance-orchestration`, `capability-discovery-and-retrieval`, `runtime-native-surfaces`, `execution-tools-and-commands`, `safety-hooks-and-permissions`, and `verification-eval-and-release`. It is the Codex runtime entry surface for the shared Meta_Kim dispatcher, not the canonical governance source.

## Required inputs

- User request from `$ARGUMENTS`.
- The project `meta-theory` skill from a configured skill root.
- Codex agent delegation capability or an explicit blocked reason when unavailable.
- Project `package.json` with `meta:theory:run` for auditable artifact generation.

## Pass

- The command resolves the shared skill, records capability discovery, and dispatches via `spawn_agent` for execution-layer analysis when available.
- If `spawn_agent` is unavailable, it reports the exact missing capability and does not continue as if agent dispatch happened.
- Governed execution that needs an artifact uses `npm run meta:theory:run -- "$ARGUMENTS"` and reopens the report by runId or `latest`.

## Fail

- The main thread writes a long execution-layer answer while this command required delegation and no blocked reason was recorded.
- A run artifact, worker result, or verification result is claimed without evidence.
- Runtime-specific Codex fields are copied into Claude Code, Cursor, or OpenClaw prompts.

## Block

Block execution when the skill cannot be resolved, `spawn_agent` is unavailable for required delegation, the package script is missing, or safety hooks deny a mutation. The blocked response must name the checked path or missing capability.

## Return to stage

Return to Fetch when skill roots, capability indexes, package scripts, MCP config, or provider state are missing. Return to Thinking when dispatch owner/loadout is unresolved.

## Verification

Run `npm run meta:prompt:validate` after editing this command adapter. For runtime behavior, verify Codex run artifacts, worker result packets, and fresh command output instead of prompt text alone.

## Preserve

Preserve Codex native agents, skills, hooks, MCP, shell, filesystem, apply_patch, sandbox, approvals, memory, graph, and runtime config. Meta_Kim may route and guard these abilities but must not replace or hide them.
