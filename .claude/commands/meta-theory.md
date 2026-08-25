---
description: Run the Meta_Kim meta-theory governance dispatcher
argument-hint: <request>
---

Use the machine-governed Meta_Kim entry for this request:

$ARGUMENTS

Default product runtime path:

```bash
npm run meta:theory:run -- "$ARGUMENTS"
```

Then reopen the report with:

```bash
npm run meta:theory:report -- latest
```

This command is not a prose-only reminder. For explicit `/meta-theory` requests,
project/repo understanding, architecture analysis, commercialization strategy,
market or competitor reasoning, governance review, release work, runtime work,
or any non-trivial durable task, run the machine entry above or state the exact
blocking reason.

Minimum route:

1. Classify the request with the Meta_Kim entry rules.
2. Run Fetch before Thinking. Project understanding must inspect local project
   evidence such as README, AGENTS, package scripts, canonical/config contracts,
   capability index, MCP/runtime configs, and Graphify navigation when present.
3. If the answer depends on current market, competitor, platform, provider,
   pricing, dependency, version, or official documentation facts, prove an
   available retrieval path or return blocked to Fetch.
4. Use the run artifact and report as the evidence surface. Do not replace the
   machine route with a short manual summary.

If `npm run meta:theory:run` is unavailable in the current directory, say that
the machine entry is unavailable and list the checked path. Do not continue as
if governed Fetch happened.

## Prompt Acceptance

This command adapter binds `governance-orchestration`, `capability-discovery-and-retrieval`, `runtime-native-surfaces`, `execution-tools-and-commands`, and `verification-eval-and-release`. It is a runtime entry surface for the shared Meta_Kim dispatcher, not a second governance source.

## Required inputs

- User request from `$ARGUMENTS`.
- Current project directory with `package.json`.
- Available Claude Code command and shell capability.

## Pass

- The request is routed through `npm run meta:theory:run -- "$ARGUMENTS"` or a precise blocked reason is returned.
- Fetch evidence is produced by the machine artifact before route claims.
- The report is reopened with `npm run meta:theory:report -- latest` or the returned runId when user-readable closure is needed.

## Fail

- The command gives a prose-only answer for non-trivial durable work.
- It claims governed Fetch happened without a run artifact or checked blocked path.
- It treats this runtime command file as the canonical source instead of the shared skill and contract.

## Block

Block when the project directory does not contain the expected package script, shell execution is unavailable, or the request requires current external evidence but no retrieval path exists.

## Return to stage

Return to Fetch when local project evidence, Graphify, provider state, or current external facts are missing. Return to Thinking when capability discovery exists but no owner/loadout route is selected.

## Verification

Run `npm run meta:prompt:validate` after editing this command adapter. For live command behavior, verify the produced run artifact and report rather than this prompt text alone.

## Preserve

Preserve Claude Code native slash-command behavior, shell/tool permissions, MCP, hooks, skills, agents, memory, graph, and project rule files. This adapter may add governance routing but must not replace native runtime ability.
