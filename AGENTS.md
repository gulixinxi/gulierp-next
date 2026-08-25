# Meta_Kim for Codex

This file is the Codex entrypoint for maintaining Meta_Kim. Read it as the resident operating guide for this repository, not as a marketing overview.

## Fast Read

If you only keep five rules in mind:

- Meta_Kim is one cross-runtime governance system. Claude Code, Codex, OpenClaw, and Cursor are projections of the same canonical layer.
- `meta-warden` is the normal public front door. Other meta agents are backstage specialists.
- Dispatch is capability-first: describe the capability, search agents / skills / tools / capability indexes, then choose the best owner.
- Long-term behavior lives in `canonical/`, `config/contracts/`, and `config/capability-index/`. Runtime trees are projections unless explicitly documented otherwise.
- User-visible worker names must be coarse English business role-family names such as `frontend`, `backend`, or `test`, not scoped work items or host-generated personal nicknames. Localized trigger words may be recognized as input, but durable governance files stay English.

## Codex Output Rules

- On Windows, do not output raw Windows paths in normal Markdown text. Wrap paths in backticks and prefer forward slashes, for example `D:/path/to/project`.
- Do not paste full diffs or patches into chat after GitHub submit.
- After GitHub submit, report only the branch name, commit hash, PR URL when present, and a short summary.

## What This Repository Is

Do not read Meta_Kim as a folder full of unrelated prompt files.

Read it as:

**a cross-runtime architecture pack for intent amplification, governed through small replaceable meta units and projected into multiple AI runtimes.**

In this repo, `meta` means the smallest governable unit that supports intent amplification. A valid meta unit:

- owns one clear responsibility class
- states what it refuses, not only what it does
- can be reviewed on its own
- can be replaced or rolled back
- does not silently absorb unrelated responsibilities

## Source Of Truth

Edit these for durable behavior:

- `canonical/agents/*.md`
- `canonical/skills/meta-theory/SKILL.md`
- `canonical/skills/meta-theory/references/*.md`
- `canonical/runtime-assets/*`
- `config/contracts/`
- `config/capability-index/`

Treat these as generated mirrors or runtime adapters unless the task explicitly targets runtime wiring:

- `.claude/agents/*.md`
- `.claude/skills/meta-theory/`
- `.claude/hooks/`
- `.claude/settings.json`
- `.mcp.json`
- `.claude/capability-index/`
- `.codex/agents/*.toml`
- `.agents/skills/`
- `.codex/skills/` legacy compatibility mirrors, when present for cleanup
- `.codex/capability-index/`
- `.cursor/agents/*.md`
- `.cursor/skills/meta-theory/`
- `.cursor/mcp.json`
- `.cursor/capability-index/`
- `openclaw/skills/`
- `openclaw/workspaces/*`
- `openclaw/capability-index/`
- `openclaw/openclaw.template.json`

After changing canonical sources, sync projections instead of hand-forking runtime copies.

## Codex Runtime Map

When this repository is opened in Codex:

- `AGENTS.md` is this resident project guide.
- `.codex/agents/*.toml` contains Codex custom-agent mirrors for the Meta_Kim team. Codex is the only target here that uses agent TOML; `worker.toml` and `explorer.toml` are fallback adapters for built-in Codex roles, and `frontend.toml`, `backend.toml`, `test.toml`, `review.toml`, `analysis.toml`, `verify.toml`, and `docs.toml` are business-role adapters for hosts that honor named custom agents. None of these adapters become durable Meta_Kim owners.
- `.agents/skills/meta-theory/SKILL.md` is the Codex project skill mirror. Project-local `.codex/skills/meta-theory/` was a legacy compatibility mirror and should be removed by sync when present. The canonical source is `canonical/skills/meta-theory/SKILL.md`.
- `.codex/hooks.json` and `.codex/hooks/` carry Codex-compatible project hook wiring.
- `codex/config.toml.example` is generated from `canonical/runtime-assets/codex/config.toml.example`.

Cursor parity is maintained through `.cursor/agents/*.md`, `.cursor/skills/meta-theory/`, `.cursor/hooks.json`, `.cursor/hooks/`, `.cursor/mcp.json`, and `.cursor/capability-index/`.

Cross-runtime format boundary:

- Claude Code agents: `.claude/agents/*.md` with YAML frontmatter.
- Codex agents: `.codex/agents/*.toml` with `name`, `description`, `developer_instructions`, and optional ASCII `nickname_candidates`. Do not copy Codex TOML fields into Claude Code, Cursor, or OpenClaw.
- Cursor agents: `.cursor/agents/*.md` with YAML frontmatter plus `.cursor/rules/*.mdc` and `AGENTS.md` context.
- OpenClaw agents: `openclaw/workspaces/<agent>/` identity/workspace files plus `openclaw/openclaw.template.json`.

## Capability-First Dispatch

Meta_Kim does not start with "call agent X". It starts with "what capability is needed?"

For every non-query governed task, run capability search before execution. If the task touches runtime behavior, inspect `config/runtime-capability-matrix.json`. If it touches macOS, Windows, or WSL2, inspect `config/os-compatibility-matrix.json`. If it touches external reusable capability, inspect `config/capability-index/dependency-project-registry.json`. Reference-only projects are not dependencies; distill useful ideas into Meta_Kim stage data such as `config/governance/decision-pattern-catalog.json`.

Use this order:

```text
Need capability
-> Search repo canonical capability index
-> Search runtime mirror indexes
-> Search local runtime inventory
-> Search available skills and tools
-> Choose the best owner by boundary fit
-> Dispatch with explicit scope, deliverable, review owner, and verification owner
```

Capability-index fetch order:

```text
config/capability-index/
-> .claude/.codex/.cursor/openclaw capability-index mirrors
-> .meta-kim/state/{profile}/capability-index/
-> explicit compatibility degradation or capabilityGapPacket
```

Hardcoding a specific agent name before discovery is a shortcut, not the canonical method.

For a real execution demand, the default path must prove the whole provider chain before mutation: capability discovery, execution-agent search and selection, execution-agent creation capability search, skill search and selection, skill creation capability search, MCP provider search, command/runtime tool selection, and verification owner/path selection. This must happen as the natural Fetch -> Thinking route, not as a validator or hook rescue after the route is already weak.

### Mechanical Enforcement (Cross-Runtime)

Capability-first has a mechanical hook path on Claude Code, Codex, and Cursor, but the default mode is progressive. During the grace window it warns unless `META_KIM_CAPABILITY_GATE=block` is set; do not describe the default as immediate hard-deny. Hooks are last-resort fuses for key behavior only. They should block missing intent, missing Fetch evidence, missing capability discovery, missing owner/loadout, known-unsupported runtime/OS, missing memory strategy, or unsafe meta-agent mutation. They should not block merely because optional packet parameters are absent; detailed completeness belongs to validators, Review, and public-ready gates.

- **Claude Code**: enforced via the PreToolUse hook `enforce-agent-dispatch.mjs` (deny payload `{hookSpecificOutput.permissionDecision: "deny"}` when the effective mode is `block`). The gate covers `Agent` dispatches in stages `execution`, `review`, `meta_review`, `verification`, `evolution` unless `fetchRecord.capabilitySearchPerformed === true`. Discovery stages `critical`, `fetch`, `thinking` are exempt except for execution-intent dispatch before design-time readiness.
- **Codex CLI**: enforced via PreToolUse hook (same `enforce-agent-dispatch.mjs` script projected to `.codex/hooks/`). Matcher includes `"Bash|apply_patch|Edit|Write|MultiEdit|NotebookEdit|Agent|spawn_agent"`, but Codex hook coverage is runtime-version dependent; do not treat it as an all-tool policy engine. Registered at `scripts/runtime-hook-mapping.mjs:213-219`.
- **Cursor**: mechanically enforced via the official `preToolUse` hook surface with `failClosed: true` (crash defaults to deny). Uses exit code 2 + stderr deny reason or stdout JSON `{"permission":"deny",...}`. Registered at `scripts/runtime-hook-mapping.mjs:269-280`.
- **OpenClaw**: current Meta_Kim tool-blocking enforcement is declarative-only — hard refusal prose in workspace `HEARTBEAT.md` and `SOUL.md` (`executionBlock=true`). OpenClaw internal hooks cover command/lifecycle automation, and typed plugin hooks are the official blocking/canceling policy surface, but Meta_Kim has not installed a typed plugin enforcement adapter yet.

Override knob (all hook-equipped runtimes): `META_KIM_CAPABILITY_GATE=progressive|block|warn|off` (default `progressive`; set `block` env for immediate hard deny). Set `warn` to emit stderr warnings without denying, or `off` to disable the gate entirely. Runtime-payload schema selector: `META_KIM_HOOK_RUNTIME=claude|codex|cursor`.

Canonical hook source: `canonical/runtime-assets/claude/hooks/enforce-agent-dispatch.mjs`. The current runtime matrix and limits are documented in this Mechanical Enforcement section; do not add a separate source-of-truth document for the same rule.

## Meta-Theory Activation

Do not require humans to know or type command words. Treat ordinary natural-language executable requests as the primary entry path when they imply durable planning, execution, review, verification, prioritization, repair suggestions, or a validation checklist. Examples include "帮我整理成优先级、修复建议和验证清单", "这个页面不好看，帮我弄高级一点", "帮我规划并开始处理", or "review this and fix what matters".

Explicit triggers such as `/meta-theory`, `meta-theory`, `meta theory`, `run meta theory`, `execute meta theory`, `元理论`, or a `meta-theory` skill mention are maintainer shortcuts, not required user behavior.

Use the entry classifier behavior as the user-facing rule:

- plain durable work in natural language enters governed `standard_path`
- subjective or taste-dependent work enters Critical clarification before Fetch
- pure read-only questions stay on `fast_path`
- explicit meta-theory requests enter `regulated_path`

Codex must first run:

```text
Critical -> Fetch -> Thinking
```

That means:

- Critical locks the real outcome, pain/value, audience, success standard, non-goals, and only asks questions that change execution
- Fetch gathers evidence and capability facts that change the route, risk, owner, scope, or acceptance criteria
- Thinking selects expert lenses, compares viable paths, rejects weak paths, resolves owners/capabilities, and writes worker work orders before Execution
- Review checks whether Critical, Fetch, and Thinking were good enough before judging final output polish
- Execution work is dispatched to agents, skills, commands, MCP capabilities, runtime tools, or workers selected by Thinking instead of collapsing into the main thread

For Codex, explicit meta-theory activation is also explicit permission to use subagents. The main thread scopes, delegates, reviews, and synthesizes; it does not become the all-purpose executor for complex work.

### Production Correctness Before Execution

Meta-theory work must be correct before production starts, not rescued by Review or Verification after the fact.

Before editing files, inspect the current worktree and source files that will be changed: `git status --short`, `git diff --stat`, targeted diffs, targeted source reads, and repo-scoped search. This read-before-edit step belongs to Critical/Fetch. A hook that blocks read-only inspection is a governance defect to route to Sentinel/Conductor, not a reason to work blind.

Required internal stage records:

- Critical: `realIntent`, `successCriteria`, `nonGoals`, `blockingUnknowns`, `noQuotaClarification`
- Fetch: `evidence`, `decisionImpactMap`, `capabilityDiscovery`, `capabilityGap`, `contradictionLog`
- Thinking: `designFrame`, `workType`, `expertLens`, `consideredLanes`, `omittedLanesWithReason`, `workerTaskPackets`, `dependencyPolicy`
- Review: checks upstream Critical/Fetch/Thinking quality before result polish

Normal chat output for these stages must be localized, compact, and human-readable. Packet field names such as `realIntent`, `decisionImpactMap`, or `workerTaskPackets` may appear when useful, but they must be paired with human labels instead of being dumped as unexplained English keys.

Governance-quality fallback is forbidden. Missing intent, evidence, design, owner, capability, dependency readiness, or worker work order means `block`, `return_to_stage`, or `capabilityGapPacket`. Runtime compatibility fallback may remain for host limitations such as a chat card instead of a popup, but it does not count as governance readiness.

## Business Flow Before Execution

For executable work, plan the business flow before writing code or changing files. This is a dynamic workflow step, not a fixed checklist: classify the user's natural-language intent, choose lanes from evidence and dependency signals, and record omitted lanes with reasons. A web app, for example, may need separate lanes for:

- product direction
- UX flow
- UI system
- frontend
- backend
- database
- auth / security
- motion / interaction polish
- tests / QA
- release / install path
- feedback and evolution

Hard rules before Execution:

- Fuzzy goals require intent amplification and an acceptance record.
- Multi-path work requires best-path selection.
- Multi-lens judgment uses dynamic lens discovery; user-mentioned books, people, or theories are seeds/fallbacks, not a fixed list.
- Execution requires owner + weapon + verification owner.
- Dependency projects require input/output contracts before use.
- Codex subagents require explicit request or explicit governed task need, and hooks require trust review.
- OpenClaw skills require third-party risk and sandbox review.
- Cursor capabilities remain unknown/partial until verified; do not mark them native from projection files alone.
- Public-ready requires verification plus intent acceptance; workflow completion alone is not user-goal completion.
- Evolution must write back or record none-with-reason.

Not every task needs every lane. Do not force every wish-style request through the same lane set; omitted lanes should be intentional. The business-flow blueprint should explain:

- what user pain/value and success standard the run serves
- what capability is needed
- which existing agent / skill / tool was found
- whether an owner is reused, upgraded, or newly created
- which expert lenses are relevant and which are explicitly not applicable
- which lanes can run in parallel
- who merges the outputs
- how the result will be reviewed and verified

## Agent Display Names

Separate these three names:

- `ownerAgent`: the real governance or execution owner, for example `meta-conductor` or `frontend-developer`
- `roleDisplayName`: the short user-visible English business role family, for example `frontend`, `backend`, or `test`
- `runtimeInstanceAlias`: the host runtime's incidental nickname, if any

Rules:

- Do not show host-generated personal names as the primary agent name.
- Prefer short role names over long task descriptions.
- Do not put concrete work items into `roleDisplayName`; put shard or task scope in `roleInstanceId`, `shardScope`, `parallelGroup`, `dependsOn`, `mergeOwner`, and collision boundaries.
- If the same owner runs multiple parallel instances, keep the same coarse `roleDisplayName` and separate instances with `roleInstanceId`.

## Eight-Stage Spine

Meta_Kim's execution backbone is:

```text
Critical -> Fetch -> Thinking -> Execution -> Review -> Meta-Review -> Verification -> Evolution
```

The 11-phase business workflow is separate:

```text
direction -> planning -> execution -> review -> meta_review -> revision -> verify -> summary -> feedback -> evolve -> mirror
```

The relationship is simple:

- the 8-stage spine governs execution logic
- the business workflow governs run packaging and deliverable closure
- business phases do not rename or replace the spine

Machine-readable source: `config/contracts/core-loop-contract.json` defines each stage's required input/output, skip condition, gate condition, blocking vs progressive behavior, Verification fuse policy, Evolution writeback policy, and public-ready claim rule. The default runnable entry is `npm run meta:theory:run`, which emits a `coreLoop` summary in the governed execution artifact.

## Hidden Governance Packets

A governed run should leave enough structure to audit what happened. Important packets include:

- `coreLoop`
- `taskClassification`
- `cardPlanPacket`
- `businessFlowBlueprintPacket`
- `agentBlueprintPacket`
- `dispatchEnvelopePacket`
- `workerTaskPacket`
- `reviewPacket`
- `revisionResponses`
- `verificationResults`
- `summaryPacket`
- `evolutionWritebackPacket`

Do not claim a run is public-ready unless verification passed, summary closure exists, a single primary deliverable was maintained, and the deliverable chain is closed.

## Planning Files

When `planning-with-files` is installed and the task is not a pure query, create persistent planning state at Stage 3:

- `task_plan.md`
- `findings.md`
- `progress.md`

Do not infer that `planning-with-files` is missing only because it is absent from `.agents/skills/`. It is a core external dependency declared in `config/skills.json` and normally installed into runtime home skill directories such as `~/.codex/skills/planning-with-files/`, `~/.claude/skills/planning-with-files/`, `~/.cursor/skills/planning-with-files/`, or `~/.openclaw/skills/planning-with-files/`. Check the manifest, global runtime homes, and `npm run discover:global` before declaring it unavailable.

These files supplement protocol packets. They do not replace `businessFlowBlueprintPacket`, `dispatchEnvelopePacket`, or verification evidence. The Conductor or the main thread acting as Conductor is the sole writer.

## The Nine Meta Agents

- `meta-warden`: coordination, arbitration, final synthesis, Warden gate
- `meta-conductor`: workflow, stage sequencing, business-flow blueprint, rhythm control
- `meta-genesis`: `SOUL.md`, identity, persona, prompt architecture
- `meta-artisan`: skill / MCP / tool fit, capability loadout
- `meta-sentinel`: safety boundaries, permissions, hooks, rollback
- `meta-librarian`: memory, continuity, context policy
- `meta-prism`: quality review, drift detection, anti-slop review
- `meta-scout`: external capability discovery and evaluation
- `meta-chrysalis`: evolution signal aggregation and writeback coordination through Warden's gate

Meta agents govern. They do not become generic implementation workers when a better execution specialist exists.

## Correct Execution Shape

Anti-pattern:

```text
User: build a notification system
Assistant: immediately edits ten files as one undifferentiated worker
```

Correct pattern:

```text
User: build a notification system
Assistant:
1. Critical: clarify material ambiguity
2. Fetch: discover existing capabilities
3. Thinking: map lanes, owners, dependencies, and merge plan
4. Execution: dispatch bounded work to the right agents / skills
5. Review: inspect outputs against quality and boundaries
6. Meta-Review: verify the review standard when risk is high
7. Verification: run fresh checks
8. Evolution: record reusable patterns or decide no writeback
```

## Graphify

This repository has a knowledge graph under `graphify-out/`.

Rules:

- For broad architecture or codebase questions, use existing `graphify-out/GRAPH_REPORT.md` and `graphify-out/graph.json` as navigation indexes when present.
- Do not run a startup freshness gate merely because a graph exists. At the start of a run, check only whether graph artifacts are present and useful enough for navigation.
- Treat Graphify as a project map, not the final source of truth. Use graph queries or subgraph extraction to find relevant modules, concepts, and file anchors, then verify route-changing claims against the target source files.
- Agent and worker context should receive only graph slices, short hints, and file anchors relevant to that worker. Do not inject the full `graph.json`, full `GRAPH_REPORT.md`, or broad graph dumps into every worker.
- If `graphify-out/wiki/index.md` exists, use it for broad navigation instead of raw source browsing.
- Dirty `graphify-out/` files can be expected after hooks or incremental updates; dirty graph files are not a reason to skip graph context.
- `npm run meta:graphify:check` and `npm run meta:validate` compare the graph's built commit with current `git rev-parse HEAD` and fail when `GRAPH_REPORT.md` is stale. Use this as a verification/release/public-ready gate, not as a routine run-start cost.
- After modifying code files, run `npm run meta:graphify:rebuild` to keep the graph current across Windows, macOS, and Linux.

## Maintenance Loop

After changing canonical behavior, contracts, hooks, or runtime-facing docs:

1. `npm run meta:sync`
2. `npm run discover:global`
3. `npm run meta:check`
4. `npm run meta:check:global`
5. `npm run meta:release:smoke` before routine patch/minor release; use `npm run meta:verify:all` only for larger, risky, runtime, install, hook, dependency, or explicitly release-grade changes

Use these supporting commands as needed:

- `npm run meta:validate`
- `npm run meta:check:runtimes`
- `npm run meta:check:sync-coverage`
- `npm run meta:doctor:governance`
- `npm run meta:eval:agents`
- `npm run meta:eval:agents:live`
- `npm run meta:validate:run -- <artifact.json>`
- `npm run meta:index:runs -- <artifact-dir-or-file>`
- `npm run meta:query:runs -- --owner <agent>`
- `npm run migrate:meta-kim -- <source-dir> --apply`
- `npm run meta:graphify:check`
- `npm run meta:graphify:rebuild`
- `npm run meta:deps:install`
- `npm run meta:deps:install:all-runtimes`
- `npm run meta:deps:update`
- `npm run meta:deps:update:all-runtimes`
- `npm run meta:sync:global`
- `npm run prompt:next-iteration`

`npm run meta:release:smoke` is the default maintainer release check for low-risk prompt/doc/governance iterations. It runs projection sync, default capability-discovery smoke, and meta-theory tests. `npm run meta:verify:all` remains the full release-grade suite: runtime sync checks, project validation, graphify health, global sync checks, smoke-level runtime acceptance, setup tests, and meta-theory tests.

## Release Modes

Routine patch/minor releases should stay fast. If the change is prompt text, docs, changelog, version metadata, or narrow governance wording with no runtime wiring change, the default release path is:

1. `npm run meta:sync`
2. `npm run meta:capabilities:smoke`
3. `npm run meta:test:meta-theory`
4. `git diff --check`

This can be run directly as `npm run meta:release:smoke`, followed by `git diff --check`.

Upgrade to full release-grade verification only when the task changes install/update behavior, global sync, hooks, runtime matrix, provider registry, dependency compatibility, runtime probes, package contents, security-sensitive behavior, or when the user explicitly asks for full/live/release-grade evidence.

Release-grade work is stricter than a local green check. In that mode, before commit, push, tag, changelog/release-note update, or publication, the run must have current evidence for:

- all declared runtime install/update targets; if machine-local defaults select only one runtime, use explicit all-runtime target selection
- project sync, global sync, and global hooks when hooks are in scope
- runtime matrix, provider registry, dependency compatibility, and runtime probe
- a real execution-demand route that naturally selects owner, creation providers, skill, MCP provider, command/runtime tool, and verification owner/path
- live Claude, Codex, and OpenClaw evidence when those live targets are declared

Do not treat structural smoke, systemMessage/UI warning output, auth-present checks, skipped/needsAuth states, or config-only proof as live pass evidence. Those are valid diagnostics, not live completion. Validators and gates protect against empty or dangerous routes; they are not the primary mechanism that makes the default path correct.

## Install And Packaging Notes

- Node must satisfy the `package.json` engine requirement.
- `package.json` uses a `files` whitelist so GitHub / npm tarballs include the full `canonical/` tree.
- `node setup.mjs` installs selected platform projections and graphify wiring idempotently.
- Runtime target selection has two layers: repo defaults in `config/sync.json`, machine-active targets in `.meta-kim/local.overrides.json`.
- MCP Memory Service uses port `8000`.
- `stop-memory-save.mjs` saves session summaries to the MCP Memory Service on session end.

## Reading Order

For maintainers:

1. `README.md` or `README.zh-CN.md`
2. `AGENTS.md`
3. `CLAUDE.md` when touching Claude Code behavior
4. This `AGENTS.md` Mechanical Enforcement section when changing cross-runtime trigger, hook, review, verification, stop, or writeback behavior
5. `canonical/skills/meta-theory/references/dev-governance.md` for the long-form governed execution contract
