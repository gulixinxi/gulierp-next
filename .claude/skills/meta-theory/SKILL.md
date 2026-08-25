---
name: meta-theory
version: 3.0.0
author: KimYx0207
user-invocable: true
trigger: "元理论|执行元理论|跑元理论|meta theory|run meta theory|execute meta theory|meta-theory|agent governance|intent amplification|governance|治理|重构|多文件|跨模块|debug|fix|验证|verification"
tools:
  - shell
  - filesystem
  - browser
  - memory
description: |
  Meta_Kim executable governance dispatcher. It classifies the run, loads only needed references, preserves foundational capabilities and runtime-native abilities, routes owner + weapon + dependency + runtime + OS + verification, and closes only with evidence, intent acceptance, and writeback decision.
---

# Meta-Theory Dispatcher

## Purpose
Run Meta_Kim as an executable governance system, not a theory essay. The main thread locks intent, gathers evidence, chooses route, delegates bounded work, reviews, verifies, and synthesizes. It must not become a generic implementation worker for non-trivial work.

Machine contract: `config/contracts/core-loop-contract.json` is the compact default-path contract for this skill. It binds ordinary durable work and explicit meta-theory shortcuts to `npm run meta:theory:run`, requires the eight-stage spine, and defines which gates block, warn, or stay progressive.

## Trigger
Activate from ordinary natural-language durable work, not only from command words. If the user asks to plan and start work, organize priorities, produce repair suggestions, build a verification checklist, fix a non-trivial issue, handle multi-file execution, run review/verification, or resolve subjective/taste-dependent quality, classify the entry and choose the governed route automatically. Explicit `/meta-theory`, `meta-theory`, or `元理论` mentions are maintainer shortcuts, not required human behavior.

## Path classification
- `fast_path`: read-only query, no mutation, no durable artifact. Output may be direct, but evidence claims still need source.
- `standard_path`: ordinary executable work. Use the 8-stage spine and capability-first route.
- `regulated_path`: governance, security, runtime, dependency, release, public-ready, or cross-platform work. Require full spine, Review, Meta-Review, Verification, and Evolution.

## Canonical spine
Critical -> Fetch -> Thinking -> Execution -> Review -> Meta-Review -> Verification -> Evolution.

You are the DISPATCHER, not the executor. Use Agent tool / `Agent(...)` dispatch only after Fetch evidence and Thinking owner resolution prove the route.

Fetch-first capability matching principle: Fetch gathers evidence, then Thinking performs capability match, never hardcoded agent-name matching. Gate 1: Clarity Check blocks unclear intent before Fetch. Gate 2: Dispatch-Not-Execute blocks self-execution and requires a named owner, weapon, and verification owner.

## Architecture Type Pre-judgment

Important: Architecture Type Distinction. Meta Architecture means agent governance, collaboration relationships, and responsibility boundaries. Project Technical Architecture means code organization, tech stack, and design patterns; redirect that lane to an architect or backend-architect capability when the needed owner is technical implementation rather than Meta_Kim governance.

## Dynamic Flow Selection

- Type A: prompt/reference/contract hardening.
- Type B: agent/skill/owner creation or upgrade.
- Type C: external capability, tool, MCP, dependency, or web evidence discovery.
- Type D: review, verification, rollback, public-ready, or warning closure.
- Type E: orchestration, planning files, business-flow, and cross-runtime release.

## Stage map

| # | Stage | Action | Interaction |
|---|---|---|---|
| 1 | Critical | clarify intent first, lock user pain, value, success criteria, non-goals, permissions, and Architecture Type; for wishful or ambiguous input, enter Critical-Fetch intent loop: translate intent -> read context -> enrich intent -> present IntentCard for user confirmation (up to `criticalFetchLoopMax` rounds) | If a required intent dimension is missing and the answer changes route, scope, risk, or non-goal, set `choiceSurfaceState = critical_clarification_allowed` and ask before proceeding. Do not present execution options during Critical. Present an IntentCard after context-enriched intent translation; user confirms or corrects through the runtime adapter's verified choice surface, or a chat decision card fallback. |
| 2 | Fetch | gather online/web and local evidence, confirm the problem, list candidate solutions with sources, extract material claims, run targeted read-only baseline verification when it changes the route, discover retrieval capabilities, complete a multi-type capability inventory before Thinking, and build a change fact card before any file mutation | If evidence suggests multiple valid paths with different trade-offs, surface the options in the user's language before Thinking. If current external facts or third-party capability claims matter, `meta-scout` or an equivalent evidence owner must finish source-backed research before Thinking. |
| 3 | Thinking | determine needed execution capabilities across governance agents, execution agents, skills, scripts, commands, MCP capabilities/providers/tools, runtime tools, plugins/connectors, retrieval capabilities, dependency/external packages, and run-scoped workerTasks; match existing capabilities; create or upgrade only for gaps; bind the file delivery contract; plan DAG/parallel/serial lanes with `mergeOwner` | Present at least 2 candidate paths with a recommended default. Ask the user to confirm the chosen path before Execution. |
| 4 | Execution | run multi-agent work using the agents, skills, scripts, commands, MCP capabilities, runtime tools, plugins/connectors, retrieval capabilities, dependencies, and tools selected by Thinking artifacts | No interaction unless route-changing discovery occurs mid-execution — then pause and inform. |
| 5 | Review | meta-prism checks upstream Critical, Fetch, Thinking, and result quality | If review finds issues that require user preference (quality vs speed trade-off), ask before proceeding. |
| 6 | Meta-Review | meta-warden verifies Review standard and public-ready gate | No interaction. Internal governance check. |
| 7 | Verification | run real tests with fresh evidence and `verificationPacket.fixEvidence` | No interaction. Run checks and record evidence. |
| 8 | Evolution | after Warden approval, directly edit the target agent definition or SOUL.md for meta-agent lessons; execution-agent gaps use `capabilityGapPacket` + Type B pipeline | No interaction. Record writeback decision. |

## User Interaction

**MANDATORY**: Use the current runtime adapter's verified native choice surface at key decision points. Keep the canonical card contract platform-neutral; renderer-specific schemas and tool names belong in runtime references such as `runtime-claude.md` or `runtime-codex.md`, not in the generic contract. If the native surface is unavailable or returns empty, fall back to a localized chat decision card and wait for the user's explicit reply.

**When to ask:**

| Stage | When to Ask | Example |
|---|---|---|
| Critical | Intent dimension missing, answer changes route/scope/risk/non-goal | "This could be a quick fix or a full rewrite. Which direction?" |
| Fetch | Evidence shows multiple paths with different trade-offs | "I found approach A (faster) and B (more thorough). Which?" |
| Thinking | Choosing between solution paths with different scope/cost | "Minimal fix: 2 hours. Ten-x shift: 2 days. Your call?" |
| Review | Issues found that need user preference to resolve | "Quality concern: rebuild or patch?" |

**Question format:**
- 2–4 meaningful options with clear trade-offs
- One recommended default labeled clearly
- User's language, not internal packet field names
- Stop and wait — do not proceed until the user answers

**Do not ask:**
- Ritual questions or stage-by-stage confirmation spam
- Just because the work is read-only; ask only when read-only analysis still exposes route-changing choices
- Questions during Meta-Review, Verification, or Evolution (these are mechanical checks)

## Type A: Prompt / Reference / Contract Hardening

Dispatch to `meta-prism` for prompt executability review and `meta-warden` for final gate. The main thread is not the executor. Use Agent tool dispatch when the task has more than a direct query or >3 sentences of change. Output: reviewed contract diff, `workerResultPackets[].fileCompletionList`, workerExecutionEvidence, and verification evidence.

## Type B: Agent / Skill / Owner Creation Or Upgrade

Dispatch to `meta-genesis` for identity/prompt architecture, `meta-artisan` for capability loadout, then `meta-prism` and `meta-warden` for review. Optional: `meta-sentinel`, `meta-librarian`, `meta-conductor`. Existing owner wins; owner upgrade or project-local creation is allowed only when Fetch proves a gap. Execution-agent evolution uses this Type B pipeline, not direct edit.

## Type C: External Capability / Tool / Dependency Discovery

Fetch scans local capability index, runtime mirrors, local runtime inventory, MCP, package scripts, installed skills, global capabilities, `findskill`, external capability discovery, specialist ecosystem search such as everything-claude-code, and `meta-scout` external evidence. Optional owners: `meta-prism`, `meta-sentinel`, `meta-scout`. Use Agent tool dispatch only after Thinking binds an owner; the DISPATCHER does not execute discovery side effects. If no callable owner exists, return to Thinking with `capabilityGapPacket`; do not use temporary fallback.

## Type D: Review / Verification / Rollback / Public-Ready

Dispatch to `meta-prism` and `meta-warden`; optional `meta-scout`, `meta-sentinel`, `meta-chrysalis`. Stage 4 owner prohibition: never dispatch Type: general-purpose, runtime alias, or governance agent as implementation worker. Public-ready requires verification evidence, userGoalDone, warning classification, and Warden gate.

## Type E: Orchestration / Business Flow / Release

Dispatch to `meta-conductor` for dynamic business-flow blueprint and parallel lane orchestration, then `meta-warden` for synthesis. Conductor must classify the user's natural-language intent, choose lanes by evidence and dependency signals, record omitted lanes with reasons, and only then fan out worker tasks. Thinking to Execution may use `agent-teams-playbook` only when there are 2+ independent parallel worker lanes / two or more parallel worker lane candidates. Independent sub-tasks must be parallelized when safe; avoid fake parallelism.

Routine Type E release work defaults to lightweight smoke when the change is low-risk prompt/docs/governance wording, changelog, or version metadata. Use `meta:release:smoke` plus `git diff --check`, then commit/tag/publish without upgrading to full live gates unless risk or the user asks. Release-grade Type E is reserved for install/update, global sync, hooks, runtime matrix, provider registry, dependency compatibility, probes, package contents, security-sensitive behavior, or explicit full/live evidence requests; detailed evidence chains live in `dev-governance.md`, `owner-resolution.md`, and `verification-evidence.md`. Validators, gates, and hooks are protection, not the engine; if they patch missing route evidence after the fact, return to Thinking before public-ready or release.

## Dispatch Self-Check
Before Stage 4, record the minimum Protocol-first Dispatch evidence: intent, Fetch evidence, a Thinking route, selected owner, owner loadout, memory strategy, and Review standard. Preferred artifacts are `runHeader`, `dispatchBoard`, `businessFlowBlueprintPacket`, `agentBlueprintPacket`, `ownerDiscoveryPacket`, and `workerTaskPackets`, but hooks must not require every optional field before useful work can continue. `agentInvocationState`: `idle -> discovered -> matched -> dispatched -> returned/escalated`. `workerTaskPackets` should include `dependsOn`, `parallelGroup`, and `mergeOwner` when the task has multiple lanes; single-lane work may record a compact task node. `ownerDiscoveryPacket` should list repo canonical owners, runtime mirror owners, project runtime agents, local global agents, reusable skill/command/hook/rule/prompt/MCP/plugin/tool providers, and the Critical / Fetch / Thinking / Review governance-stage owners checked before any create or upgrade decision. Option Exploration is MANDATORY when materially different paths exist: compare ≥2 solution paths with Pros / Cons or Decision Record, or record `no_branching_choice` with evidence. Apply Skip-Level Self-Reflection Gate and Escalation Signals before dispatch.

## Fetch Evidence Inventory

Research -> Inventory -> Thinking Handoff. Fetch first records the question, source requirements, retrieval capability readiness, and multi-type capability inventory. Thinking determines needed execution capabilities after Fetch, matches existing capabilities, and creates or upgrades only for gaps. Fetch material claims include version, price, third-party, platform, current web state, dependency, provider, and tool assertions. If current facts matter, set `contentEvidencePacket.researchRequired = true`, run `researchCapabilityDiscovery`, and prefer `web_search`, `url_fetch`, `docs_lookup`, `browser_open`, `mcp_search`, or `plugin_search` before route design. If research is blocked, return `blocked` with `user_fallback` rather than guessing. Run command/script discovery by package.json script scan and npm run inventory. Apply DRY conflict detection: overlap detect, duplicate reject, and keep one owner per capability. Capability selection ROI = (Task Coverage x Usage Frequency) / (Context Cost + Learning Curve).

Fetch discovery minimum checklist: before Thinking, search at least these locations (even if results are empty):
- canonical sources and capability indexes: `canonical/agents/`, `canonical/skills/`, `canonical/runtime-assets/`, `config/capability-index/*.json`, and runtime capability-index mirrors
- Claude Code project and global inventories: `.claude/agents/`, `.claude/skills/`, `.claude/commands/`, `.claude/hooks/`, `.claude/settings.json`, `~/.claude/agents/`, `~/.claude/skills/`, `~/.claude/commands/`, `~/.claude/hooks/`, and `~/.claude/settings.json`
- Codex project and global inventories: `.codex/agents/`, `.agents/skills/`, `.codex/commands/`, `.codex/hooks/`, `.codex/hooks.json`, `.codex/config.toml`, `~/.codex/agents/`, `~/.codex/skills/`, `~/.codex/commands/`, `~/.codex/hooks/`, `~/.codex/hooks.json`, `~/.codex/config.toml`, and `~/.agents/skills/`
- Cursor project and global inventories: `.cursor/agents/`, `.cursor/skills/`, `.cursor/rules/`, `.cursor/prompts/`, `.cursor/hooks/`, `.cursor/hooks.json`, `.cursor/mcp.json`, `~/.cursor/agents/`, `~/.cursor/skills/`, `~/.cursor/rules/`, `~/.cursor/prompts/`, `~/.cursor/hooks/`, and `~/.cursor/hooks.json`
- OpenClaw project and global inventories: `openclaw/workspaces/`, `openclaw/skills/`, `openclaw/hooks/`, `openclaw/openclaw.template.json`, `~/.openclaw/openclaw.json`, `~/.openclaw/workspace-*`, `~/.openclaw/skills/`, `~/.openclaw/hooks/`, and `~/.agents/skills/`
- runtime package and command providers: `package.json` scripts, local scripts, runtime commands, hooks, validators, prompts/rules, setup/status/doctor/sync/install routes
- `.mcp.json`, runtime MCP config such as `.codex/config.toml` and `.cursor/mcp.json`, MCP server/tool inventory, and connector/plugin inventories
- `config/runtime-capability-matrix.json`, `config/os-compatibility-matrix.json`, `config/capability-index/dependency-project-registry.json`, and dependency/external package registries
- retrieval capability inventory: `web_search`, `url_fetch`, `docs_lookup`, `browser_open`, `mcp_search`, `plugin_search`, `local_only`, and user-supplied source paths

Pass condition: `fetchPacket.capabilityDiscovery.searchLog` exists with checked sources and results, including empty or unavailable source entries; `fetchPacket.capabilityDiscovery.capabilityInventory` covers all capability types and all runtime-specific paths that affect the route; and planned file mutation records `fileChangeFactCard`; detailed schema lives in `dev-governance.md`.

Fetch angle decomposition: for research or analysis tasks (when `contentEvidencePacket.researchRequired = true`), decompose the core question into N semantically distinct search angles before searching. Each angle must target a different aspect; rephrasing the same angle is forbidden. Output: `contentEvidencePacket.searchAngles = [{angle, keywords, expectedCoverage}]`. Default N=3; increase for complex multi-domain questions.

Graphify knowledge policy: Graphify is an agent capability, not a context dump. At run start, use existing `graphify-out/GRAPH_REPORT.md`, `graphify-out/graph.json`, or wiki indexes as navigation if present; do not run a global freshness check or rebuild just because a graph exists. Use graph queries and subgraph slices to locate relevant modules, concepts, and file anchors, then verify route-changing claims against source files. Inject only worker-relevant graph slices, short hints, and file anchors; never inject the full `graph.json`, full `GRAPH_REPORT.md`, or broad graph dumps into every worker. After code, canonical, contract, or runtime-facing doc mutation, rebuild Graphify in Verification/Evolution; reserve `meta:graphify:check` for verification, release, public-ready, or explicit graph validation.

Execution-agent identity must stay abstract and provider-first. Durable `executionAgentCard` content may describe a reusable capability class, boundaries, abstract dependencies, inputs, and outputs; it must not contain repo paths, file lists, tickets, one-run work instructions, `todayTask`, `scopeFiles`, `deliverableLink`, or `verifySteps`. Match existing agents, skills, commands, hooks, rules/prompts, MCP tools, runtime tools, and plugins before creating or upgrading an execution agent. Put concrete work in `workerTaskPackets`, `capabilityBindings`, and `orchestrationTaskBoardPacket` only. If a card cannot be written without concrete task binding, return to Thinking and reuse an existing owner/provider or emit `capabilityGapPacket`. When `GapDecision = create_agent`, require a `GeneratedAgentSpec` review artifact with flow position, handoff, loadout slots, scoped memory, gap policy, verification policy, install projection, and identity-cleanliness proof before any agent file is written.

Capability scan UX: full global scans happen on install, update, explicit refresh, missing cache, cache older than 14 days, missing required provider, or high-risk provider routes. Normal execution reads cached global inventory, performs a lightweight project scan, shows only counts/top candidates/source refs, and avoids dumping full provider definitions into chat. If the last full scan is older than 2 weeks, tell the user this run will update first to match newly added content and reach the best capability route, then refresh before execution.

## Warden Entry Gate

Governed meta-theory entry enters through `meta-warden` entry gate, whether it was triggered by ordinary natural language or by an explicit maintainer shortcut. Then `meta-conductor` owns the evidence lane and sequencing, `meta-scout` owns external evidence, and `meta-prism` audits Critical, Fetch, and Thinking quality before output polish. All meta agents are dispatch targets: `meta-warden`, `meta-conductor`, `meta-scout`, `meta-artisan`, `meta-genesis`, `meta-sentinel`, `meta-librarian`, `meta-prism`, `meta-chrysalis`.

## Product Reasoning Contract

Translate the surface request into the real product problem. Compare minimal fix against ten-x path challenge and path shift. Final user-facing closure states the root goal, what this run did, whether it still fits the root direction, whether delivery is complete or partial, chosen rationale, why changed / why change, what changed, where changed, user impact, verification, complexity added or avoided, remaining limits, deferred work, and next action.

## Decision Cross-Validation Gate

For PR, issue, release, compatibility, public-ready, or skill-prioritization decisions, the answer must survive adversarial cross-validation before it is treated as done. Record the evidence snapshot time, source state matrix, confidence labels, counterevidence, contradiction log, falsification checks, and replay commands. Re-check current external state when it can change, such as open PRs, open issues, comments, labels, review state, release status, package versions, or platform support.

Decision recommendations must bind one primary risk lane and at most one secondary lane to the next executable gate. For example, runtime/setup/install/sync changes bind to cross-runtime contract design; localized routing or choice-surface changes bind to multilingual QA; review finding or release-closure work binds to review closure discipline; hook/dispatch/state/validator changes bind to state-machine failure modeling. When two lanes compete, choose by failure cost: user install/runtime breakage, wrong execution or repeated hook blocks, incomplete public closure, then localized user route failure.

Fail the gate if the decision relies on stale PR/issue state, a single source with no countercheck, unlabeled inference, unverifiable "verified" claims, command-pass equals user-goal-done, or four equal priorities with no next action. A cross-validatable decision must let an independent reviewer replay the evidence and either reach the same conclusion or see exactly which contradiction changed the route.

## Human-Readable Stage Feedback

Stage updates must be compact, human, and in the resolved user language. Record internally with packet field name / internal keys and debug traces, but show human label and human-readable label in user-facing output. Mention Critical, Fetch, Thinking, Review only when the stage status matters. Keep token use low and avoid dumping raw packet fields unless the user asks for debug.

Public status surface uses `runStatusEnvelope`, `publicLabels`, `.meta-kim/state/{profile}/active-run.json`, and `.meta-kim/state/{profile}/runs/{runId}/status.json`. Apply runtime/tool selected output language first, then latest input language. Do not hardcode labels. The public notice must not expose internal protocol fields such as `Preflight` or `conversation_fallback` unless debug is requested.

User experience truth boundary: users should not need to run `npm` scripts, inspect JSON, or know packet names to understand a governed run. Internal artifacts such as `ownerDiscoveryPacket`, `orchestrationTaskBoardPacket`, `workerTaskPackets`, and command output are evidence, not the user experience itself. When reporting orchestration, show compact localized notices for progress, route, owner handoff, blockers, and verification, and explicitly avoid claiming that users can experience a feature when it only exists as an internal artifact or maintainer-only command.

Natural-language trigger rule: the user does not need to ask for stages, agents, skills, MCPs, commands, packets, or reports. If a normal human task triggers meta-theory, the dispatcher must automatically translate the internal route into a plain-language stage plan: what this stage does, what capability/loadout will be used, what result the user will see, and what starts next. Technical names may appear only as a compact backing loadout for traceability; the primary explanation must read like an operation handoff, not a protocol dump.

Interactive execution communication: during multi-stage work, the dispatcher must report progress to the user at natural transition points — not only at the pre-decision gate. Report triggers: (1) Fetch complete — brief evidence summary and route impact, (2) Thinking complete — chosen path and trade-offs, (3) each Execution phase complete — what was done and what remains, (4) Review findings that change scope — surface them immediately, (5) route-changing discovery mid-execution — pause and inform. Each report is a compact notice (max 3 bullets), not a full packet dump. If the discovery changes scope, owner, or risk, upgrade the notice to a Decision card requiring user input. This "communicate while working" pattern keeps the user informed and in control without requiring them to ask for status.

## Dynamic business-flow capability matrix

Fetch expands executable deliverables into a Business-flow capability matrix by intent signals, not by a fixed template. The candidate lane universe may include product, research, content, UX, UI, frontend, backend, database, integration, security, motion, accessibility, browser QA, performance, release, feedback, and evolution. Thinking selects only the lanes justified by the current task, records omitted lanes with reasons, binds dependencies and merge owner per selected lane, and uses fan-out / synthesize / adversarial verification when the task has independent work streams.

Selected dynamic lanes must synthesize project-scoped agent profiles from the current project profile and capability requirements. Thinking records them in `projectAgentBlueprintPacket` with `ownerMode = project-agent-profile`, a pinned `capabilityProfileId`, `capabilityLoadout`, `roleSoulPolicy`, project memory strategy, `externalEvidencePolicy`, `localBaselineComparison`, and `knowledgeGraphPolicy`. Current external claims such as platform rules, provider/API capability, dependency versions, pricing, compliance, security, release, or third-party workflow feasibility require source-backed Fetch evidence through `web_search`, `url_fetch`, `docs_lookup`, `browser_open`, `mcp_search`, or equivalent runtime retrieval before route lock; if that evidence is unavailable, block or return to Fetch instead of guessing. Every selected lane must also compare against local reality before dispatch: canonical agents/skills/contracts, capability indexes, runtime mirrors, package scripts, MCP configs, OS/runtime matrices, project memory, and graph navigation slices when available. Execution then creates only run-scoped worker instances from those profiles. Capability updates change the capability profile for future runs; in-flight workers keep the pinned profile. A synthesized profile becomes a durable high-quality project agent file only through `GapDecision = create_agent` plus the Type B `GeneratedAgentSpec` review path.

## Evolution target map

| Gap type | Evolution target |
|---|---|
| prompt gap | canonical skill or reference contract |
| agent boundary gap | target agent definition / SOUL.md |
| capability gap | `capabilityGapPacket` then Type B owner upgrade |
| dependency gap | dependency registry and compatibility validator |
| runtime/OS gap | runtime matrix or OS matrix |
| warning/hook scar | validator, hook policy, regression test |

## Stage packet table

| Stage | Required packet | Pass condition |
|---|---|---|
| Critical | `intentPacket`, `taskClassification` | outcome, success criteria, non-goals, permissions, blocking unknowns recorded |
| Fetch | `fetchPacket`, `foundationalCapabilityPreservationPacket`, `dependencyCapabilityAuditPacket`, `fileChangeFactCard` when mutation is planned | evidence changes route/risk/owner/verification or records no-impact; planned file changes have purpose, consumer, overlap, data-shape, and user-instruction evidence |
| Thinking | `dispatchBoard`, `workerTaskPackets`, `routeScoreBreakdown` | selected route has owner + weapon + dependency policy + runtime + OS + verification owner; worker tasks bind target files to their consumer and delivery contract |
| Execution | `workerResultPackets`, `workerExecutionEvidence` | bounded tasks produce declared artifacts and evidence |
| Review | `reviewPacket.findings` | upstream Critical/Fetch/Thinking and output quality are reproducibly checked |
| Meta-Review | review-standard checks on `reviewPacket` | Review catches native/foundational/dependency/intent/public-ready/evolution risks |
| Verification | `verificationPacket`, `verificationEvidence` | fresh commands/logs/artifacts/human acceptance bind claims |
| Evolution | `evolutionWritebackPacket`, `scarPacket` | writeback or `none-with-reason` with next-run reuse key |

## Required Fetch config

Before Execution, inspect the relevant local source of truth:

- `config/runtime-capability-matrix.json` for Claude Code, Codex, Cursor, OpenClaw support.
- `config/os-compatibility-matrix.json` for macOS, Windows, Linux, WSL2 support.
- `config/capability-index/weapon-registry.json` for weapons.
- `config/capability-index/dependency-project-registry.json` and `.meta-kim/state/default/dependency-capability-index.json` for dependencies.
- `config/skills.json`, runtime projections, MCP configs, hooks, package scripts, Graphify, Memory, and repository search for foundational capabilities.

## Abstract foundational capability triggers

Do not narrow Meta_Kim to a few named skills. Treat named packages such as `findskill`, `hookprompt`, `planning-with-files`, and `skill-creator` as examples inside a wider abstract capability surface described by `config/contracts/prompt-abstract-capability-contract.json`.

Hardcode capability families and conflict rules in prompts; discover concrete providers at run time:

| Capability family | Trigger in the prompt | Conflict boundary |
|---|---|---|
| `governance-orchestration` | Durable planning, governance, review, verification, prioritization, repair, runtime, or release work | Governance agents route and review; they do not become generic implementation workers. |
| `capability-discovery-and-retrieval` | Owner, tool, dependency, current fact, provider, or verification path affects the route | `findskill` and external search are run-scoped Fetch inputs, not permanent agent identity bindings. |
| `prompt-intake-optimization` | User prompt submission or prompt optimization request | `hookprompt` may add prompt context; it must not override user intent, PRD decisions, meta-theory route, or planning state. |
| `planning-continuity` | Non-query durable work needs resume, progress, evidence, route, or acceptance continuity | Planning files are update-only continuity state: append, refine, or mark superseded; do not overwrite or reset `task_plan.md`, `findings.md`, or `progress.md`. |
| `skill-agent-tool-creation` | Fetch proves a reusable capability gap after existing providers are checked | `skill-creator`, create-agent, or tool creation starts only after gap proof, review, and Warden-approved durable writeback. |
| `runtime-native-surfaces` | Runtime-facing route, projection, hook, command, skill, agent, MCP, choice, sandbox, or approval behavior | Preserve native, partial, unknown, and blocked states; do not fake or replace runtime-native abilities. |
| `execution-tools-and-commands` | Real execution, file edits, browser/UI proof, command output, or validator/test evidence is needed | Select tools by owner, permission, runtime, OS, and verification; command pass is evidence, not user-goal completion. |
| `mcp-external-provider-and-plugin` | External data, external tool, provider SDK, plugin, connector, dependency, or MCP affects the route | Configured or installed providers are not live proof; external writes, credentials, paid actions, and mutations need approval and verification. |
| `memory-graph-and-observability` | Prior decisions, project map, graph freshness, run state, trace, or evidence continuity affects route or acceptance | Memory and graph guide navigation; verify route-changing claims against source files or fresh artifacts. |
| `safety-hooks-and-permissions` | Unsafe mutation, missing dispatch evidence, hook loop, install/update, sandbox, approval, or credential risk appears | Hooks are last-resort fuses, not planners; repeated blocks return to the responsible stage. |
| `verification-eval-and-release` | A prompt, route, runtime, release, or user goal is claimed complete | Do not relabel smoke, config validation, skipped, needsAuth, or old artifacts as live/release-grade proof. |
| `user-interaction-and-i18n` | Route-changing ambiguity, user choice, progress notice, or output-language handling is visible | Ask only route-changing questions and preserve locale; renderer schemas stay in runtime adapters. |

## Native ability preservation

Governance may add trigger, evidence, trust review, approval, sandbox, fallback, verification, and risk boundaries. It must not delete, downgrade, or replace runtime-native abilities for Claude Code, Codex, Cursor, or OpenClaw. Unknown or partial native abilities stay `unknown` or `partial` until verified; they are not removed.

## Foundational capability preservation

Do not delete or hide existing Skills, WebSearch, web search, browser, online research, fetch, filesystem, shell, command, apply_patch, edit, MCP, memory, Graphify, graph, hooks, scripts, validators, commands, rules, agents, subagents, approval, sandbox, permission mode, runtime tools, setup, uninstall, status, doctor, sync, install, or verification capability. If a capability is risky or unavailable, mark `needs_probe`, `unknown`, `partial`, `requires_approval`, `requires_trust_review`, `reference_only`, or `not_for_execution_route`.

## Dependency compatibility

Dependencies are retained and routed by state, not deleted by score.

- score `<50`: `blocked_for_execution`, evidence/reference only, generate upgrade/probe suggestion.
- score `50-69`: `needs_upgrade_or_probe`, no automatic execution.
- score `70-84`: `confirm_or_fetch_more`, requires user confirmation or more evidence.
- score `>=85`: eligible only with invocation path, verification method, owner, weapon, runtime support, OS support, and verification owner.

`Kim_Decision` is a decision protocol candidate, not a code executor. Discover it through `META_KIM_DEP_ROOTS`, sibling repo scan, installed skill paths, registry, or external reference. Never hardcode a personal path. Valid states: `local_inspected_protocol`, `installed_skill_candidate`, `external_reference`, `internalized_pattern`, `blocked`, `not_for_code_execution`, `eligible_for_decision_route`, `needs_probe`.

## Execution gate

Execution may start only when the key behavior gate is true (or degraded mode is explicitly active with recorded degradation reason). Hooks enforce this minimum; fuller packet shape is validated by validators and Review:

- `realIntent`, success criteria, non-goals, and blocking unknowns are recorded.
- Fetch evidence and capability discovery are complete enough for the chosen path.
- Existing-owner discovery has checked enough available agents, skills, commands, MCP tools, runtime tools, and prompt/rule providers to justify the selected owner/loadout; skipped sources appear as blockers or no-impact evidence.
- Route score is `>=85`, or a branch-changing user choice accepts a `70-84` route; simple single-path work may record `no_branching_choice`.
- Owner is not `general-purpose`, not a runtime alias, and not a governance agent acting as implementation worker.
- Owner has a usable loadout: skill, command, MCP capability, runtime tool, normal tool, or abstract prompt.
- Runtime/OS support is not known-unsupported; unknown or partial support is recorded with a probe/degraded route.
- Memory strategy exists (`project_only`, `cross_project_readonly`, `none-with-reason`, or equivalent).
- For file mutation, `fileChangeFactCard` exists and each target file has a consumer/distribution path, overlap decision, and data-shape note when applicable.
- Review standard is known. Verification owner, rollback, dependency eligibility, and detailed packet fields are required for public-ready, not as universal hook blockers.

Worker output schema validation: when `workerTaskPacket.output` defines an expected structure, the dispatcher (or receiving agent) must validate the worker result against that structure before accepting it. On mismatch, the worker retries (up to 2 attempts) before reporting failure. Record `workerResultPacket.schemaValidationAttempts = [{attempt, passed, violationDetail}]`. This prevents format drift between Thinking's output contract and Execution's actual return.

## Review gate

Review must check upstream chain before output polish:

- Critical locked the right user outcome and success criteria.
- Fetch evidence changed or justified the route.
- Thinking selected owner + weapon + dependency + runtime + OS + verification.
- Planned file changes were justified by `fileChangeFactCard`; no new file exists only because the worker found a convenient place to write.
- Execution evidence is reproducible.
- No foundational capability or runtime-native ability was deleted or downgraded.
- No reference-only dependency entered execution.

Adversarial verify pattern: when Review runs for `regulated_path` or when the user requests cross-check, spawn N independent skeptic reviewers (default N=3). Each skeptic receives a different lens (correctness, security, completeness) and must explicitly try to refute each finding. A finding survives only if a majority (>= ceil(N/2) refutations fail). Record per-finding vote tallies in `reviewPacket.findings[].adversarialVotes = [{lens, verdict, refutationEvidence}]`. In degraded mode, the main thread applies the same checklist with `degradedFlag: true` and records self-assessment as one vote (not a majority).

## Verification gate

Do not claim verified unless a command, log, artifact, or human acceptance record supports the claim. Command pass is not `userGoalDone`. Template validation is not strict run validation.

Live and release-grade evidence are stricter than structural checks; smoke, config validation, UI/systemMessage output, auth-present checks, and skipped/needsAuth states cannot be relabeled as live pass. Routine low-risk releases may ship after the smoke path the user selected; release-grade claims wait for the evidence chain in `verification-evidence.md`.

## Evolution gate

Every run ends with `writebackDecision = writeback` or `none-with-reason`. Durable failures require a scar with `failurePattern`, `preventionRule`, `test`, and `nextRunReuseKey`. Evolution writeback needs Warden approval; Chrysalis coordinates; target owners update their own sources.

## Reference loading

Load only references needed for the run:

- `path-selection.md`: route scoring and path bands.
- `owner-resolution.md`: owner + weapon + dependency route.
- `runtime-codex.md`: Codex-specific sandbox, approval, subagent, hook, and choice behavior.
- `runtime-claude.md`: Claude Code-specific native question surface, Agent / Skill / Command / prompt / MCP dispatch, and no-self-execution behavior.
- `verification-evidence.md`: verified claim and public-ready evidence.
- `intent-amplification.md`: real intent, first action, pass/kill, userGoalDone.
- `evolution-writeback.md`: writeback, scars, reuse keys.
- `rhythm-orchestration.md`: choice/card timing.
- `planning-files.md`: `task_plan.md`, `findings.md`, `progress.md`.
- `create-agent.md`: new/changed owner design.
- `spine-state.md`: stage state and packet transitions.
- `dev-governance.md`: full-flow compact index.
- `ten-step-governance.md`: business workflow compatibility.
- `meta-theory.md`: background only; do not load as execution contract unless theory terms are disputed.

## User-facing compact output rule
Use the user's language. For Chinese input, output Chinese stage summaries. Each visible stage summary should be at most three bullets unless final reporting requires more.

## No fake owner
Reject `general-purpose`, temporary fallback, runtime nickname, missing owner, or governance agent as implementation worker. Return to Thinking with a `capabilityGapPacket`.

## No general-purpose fallback

Compatibility fallback may preserve runtime usability. Governance fallback may not hide missing intent, owner, weapon, dependency, evidence, or verification. Missing governance readiness blocks or returns to the responsible stage.

## No public-ready without userGoalDone

`public-ready` requires `intentAmplificationScore >= 90`, `publicReadyScore >= 90`, `userGoalDone = true`, verification evidence, no unresolved high/critical findings, and writeback decision.

## No deletion of foundational capabilities

Prompt cleanup may delete vague language only. It may not remove Skills, WebSearch, browser, research, filesystem, shell, apply_patch, MCP, memory, graph, Graphify, hooks, scripts, commands, runtime tools, validators, setup, sync, install, uninstall, or projections.

## No removal of runtime native abilities

Meta_Kim adds boundaries around Claude Code, Codex, Cursor, and OpenClaw native abilities. It must not replace native UI, approval, sandbox, hooks, skills, agents, commands, MCP, or rules with fake Meta_Kim equivalents.

## No dependency deletion due to low score

Low-score, unknown, partial, uninstalled, external, high-risk, or reference-only dependencies stay registered. They may be blocked from execution, marked for probe, downgraded to evidence/reference, or assigned upgrade suggestions.

## No Hook loop

Hooks are last-resort fuses, not the main governance engine. Execution must pass the key behavior preflight before mutation: intent, evidence, capability discovery, runtime/OS not known-unsupported, owner, owner loadout across skill/command/MCP/tool/prompt, memory strategy, and Review standard. Detailed dependency eligibility, rollback, verification owner, warning classification, and writeback reservation are validator/public-ready gates unless their absence makes execution unsafe. A Hook block must include `returnToStage`, `repairOwner`, `repairAction`, `allowedNextAction`, and `forbiddenRetry`. The same Hook reason may block once; the second same-reason block enters `hookRepairMode`; a third same-hook block stops Execution and creates `hookFailurePacket`. Never retry the same blocked action unchanged.

## Degraded Mode

When Agent dispatch is unavailable or no matching owner exists after capability discovery, the spine enters degraded mode instead of silently skipping stages.

Required actions:
- Record `capabilityGapPacket` with `currentAgentsChecked` and `currentProvidersChecked`.
- Record `degradationReason`: `tool_limitation`, `no_matching_owner`, or `permission_blocked`.
- Downgrade `surfaceState` to `internal-ready`; never claim `public-ready` in degraded mode.
- Log which spine stages execute in degraded mode vs fully executed.
- Review and Verification stages: read the relevant meta-agent definition for criteria, apply the same checklist, and record `degradedFlag: true` with `reviewerRole: "main-thread-degraded"`.
- Verification: add `humanAcceptanceRequired: true` when no independent verification owner exists.

Forbidden:
- Silently skip any stage.
- Claim `public-ready` while in degraded mode.
- Self-execute when Thinking assigned a different owner (unless degraded mode explicitly overrides and records the override reason).

## Real testing and warning classification

Script validation is necessary but not sufficient. Public-ready requires real route fixtures, strict run artifact validation, dependency discovery output, runtime/OS probe output, and warning review. Warnings must be classified as `BLOCKING_WARNING`, `FIXABLE_WARNING`, `ENVIRONMENT_WARNING`, `EXPECTED_WARNING`, `DEPRECATED_WARNING`, or `NOISE_WARNING`. Unclassified warnings, unresolved Hook blocks, unresolved high/critical findings, missing verification evidence, or missing `userGoalDone` keep `publicReady=false`.
