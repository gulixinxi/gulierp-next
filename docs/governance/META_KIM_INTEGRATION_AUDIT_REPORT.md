# Meta_Kim Integration Audit Report

| Field | Value |
|---|---|
| **Report ID** | `META_KIM_INTEGRATION_AUDIT_REPORT` |
| **Goal** | `GULIERP_META_KIM_GOVERNANCE_AUDIT` (TASK 1 of 6) |
| **Authored** | 2026-08-25 (Asia/Shanghai) |
| **Author** | Mavis (M3 / mavis), acting as Meta_Kim governance reviewer |
| **Scope** | Repository integration surface (no source / test / migration / DB modification) |
| **Repo HEAD** | `ecf613e` (master) |
| **Per Brief** | Analysis only. NO commit / NO push. |

---

## 0. Executive Summary

The GuliERP Next repository **projects Meta_Kim into three of four runtimes** (Claude Code,
Codex, Cursor v1.7+) with full agent rosters, capability indexes, hooks, and shared skills.
The fourth runtime (OpenClaw) is **not projected**. The shared Meta_Kim skills and the
`meta-theory` 8-stage governance spine are wired into the `Skill` matcher for both Claude
and Codex runtimes.

The **canonical source layer** (`canonical/agents/`, `canonical/skills/`,
`canonical/runtime-assets/`, `config/contracts/`, `config/capability-index/`) is **NOT
present** in the repository. The runtime trees (`/.claude`, `/.codex`, `/.cursor`)
contain **projected copies** but no upstream source of truth. This is the highest-priority
governance risk: future Meta_Kim upgrades cannot be regenerated from the repository;
any divergence between runtime trees must be hand-resolved.

The `.meta-kim/state/` directory referenced by `meta-kim-post-copy.mjs` is also **absent**,
so the post-copy initialization marker is not yet created. This is benign for read paths
but blocks the post-copy bootstrap completion event.

| Surface | Status |
|---|---|
| `.codex/` (Codex runtime projection) | ✅ COMPLETE (19 agents, 1 capability-index, 1 command, 8 hooks, hooks.json) |
| `.claude/` (Claude Code runtime projection) | ✅ COMPLETE (9 meta-agents, 1 capability-index, 1 skill bundle, 16 hooks, settings.json) |
| `.cursor/` (Cursor runtime projection) | ✅ COMPLETE (9 meta-agents, 1 capability-index, 4 rules, 8 hooks, skills, hooks.json, mcp.json) |
| `.agents/` (shared cross-runtime skills) | ✅ COMPLETE (2 skills: `meta-theory`, `same-set-reusable-flow-for-project-file-inventor`) |
| `openclaw/` (OpenClaw runtime projection) | ❌ **MISSING** |
| `canonical/` (Meta_Kim source layer) | ❌ **MISSING** — runtime trees are bare copies |
| `config/` (contracts / capability-index source) | ❌ **MISSING** |
| `.meta-kim/` (state directory) | ❌ **MISSING** (referenced by `meta-kim-post-copy.mjs`) |
| `AGENTS.md` (Codex entrypoint) | ✅ PRESENT (25,586 bytes) |
| `CLAUDE.md` (Claude entrypoint) | ✅ PRESENT (17,495 bytes) |
| `.mcp.json` (root MCP wiring) | ⚠️ EMPTY `{"mcpServers": {}}` — no MCP servers registered |
| `.cursor/mcp.json` | ⚠️ EMPTY |
| `.claude/settings.json` permissions | ✅ HAS deny-list for `.env`, `.pem`, `.key`, `secrets/` |
| `meta-kim-post-copy.mjs` (installer) | ✅ PRESENT (7,885 bytes) |
| `gulierp-next` (CLI entrypoint binary) | ✅ PRESENT (16,812 bytes) |
| `global.json` (.NET SDK pin) | ✅ PRESENT (10.0.100, rollForward=latestFeature) |
| `dotnet-tools.json` (dotnet-ef) | ✅ PRESENT |
| `README.md` | ✅ PRESENT (565 bytes, current gate `GULIERP_GREENFIELD_BOOTSTRAPPED`) |

**Final Gate**: `META_KIM_INTEGRATION_AUDIT_COMPLETE_WITH_4_GAPS`.

---

## 1. What IS Present

### 1.1 Runtime Projections

#### `.codex/` (Codex)
- `agents/` — 18 TOML files: 9 meta-agents (`meta-artisan`, `meta-chrysalis`,
  `meta-conductor`, `meta-genesis`, `meta-librarian`, `meta-prism`, `meta-scout`,
  `meta-sentinel`, `meta-warden`) + 9 worker agents (`analysis`, `backend`, `docs`,
  `explorer`, `frontend`, `review`, `test`, `verify`, `worker`)
- `capability-index/meta-kim-capabilities.json` — capability discovery source
- `commands/meta-theory.md` — `/meta-theory` slash command
- `hooks/` — 8 hooks: `activate-meta-theory-spine`, `bash-readonly-whitelist`,
  `enforce-agent-dispatch`, `graphify-context`, `hook-i18n`, `skip-reminder`,
  `spine-state`, `utils`
- `hooks.json` — `PreToolUse` (Bash|apply_patch|Edit|Write|MultiEdit|NotebookEdit|Agent|spawn_agent → enforce-agent-dispatch + graphify-context) and `Skill` (`meta-theory` → activate-meta-theory-spine)

#### `.claude/` (Claude Code)
- `agents/` — 9 meta-agents in Markdown (no built-in worker agents because Claude uses
  the global `mavis` / `explore` / `worker` / `verifier` roster via the `Agent` tool)
- `capability-index/meta-kim-capabilities.json` — capability discovery source
- `commands/` — slash commands (count not enumerated in this audit)
- `hooks/` — 16 hooks: the 8 Codex-shared hooks PLUS 8 Claude-specific:
  `post-format`, `post-typecheck`, `post-console-log-warn`, `stop-compaction`,
  `stop-console-log-audit`, `stop-completion-guard`, `stop-spine-cleanup`,
  `subagent-context`
- `skills/` — 2 skill bundles: `meta-theory`, `same-set-reusable-flow-for-project-file-inventor`
- `settings.json` — permissions (deny list for secrets) + full hook surface
  (PreToolUse, PostToolUse, SubagentStart, Stop)

#### `.cursor/` (Cursor v1.7+)
- `agents/` — 9 meta-agents in Markdown
- `capability-index/meta-kim-capabilities.json` — capability discovery source
- `rules/` — 4 rule files: `graphify.mdc`, `meta-choice-surface.mdc`,
  `meta-enforcement.mdc`, `meta-theory-dispatch.mdc`
- `hooks/` — 8 hooks (Codex-shared)
- `skills/` — skill bundles
- `hooks.json` — Codex-shared hook wiring
- `mcp.json` — empty `{"mcpServers": {}}` (no MCP servers registered)

#### `.agents/` (cross-runtime shared)
- `skills/meta-theory/` — full skill bundle with `SKILL.md` + 15 reference docs
  (`create-agent.md`, `dev-governance.md`, `evolution-writeback.md`,
  `intent-amplification.md`, `meta-theory.md`, `owner-resolution.md`,
  `path-selection.md`, `planning-files.md`, `rhythm-orchestration.md`,
  `runtime-claude.md`, `runtime-codex.md`, `spine-state.md`,
  `ten-step-governance.md`, `verification-evidence.md`) + `evals/eval-contract.md`
- `skills/same-set-reusable-flow-for-project-file-inventor/` — reuse flow skill

### 1.2 Entrypoint Documents

- `AGENTS.md` (25,586 bytes) — Codex entrypoint. Documents the 5-rule "Fast Read" (meta-warden
  as public front door; capability-first dispatch; canonical/ as source of truth; .claude/.codex/
  .cursor/ as projections; user-visible worker names must be coarse English role-family names).
  This document is shared across Codex, OpenCode, Aider, Droid, Trae.
- `CLAUDE.md` (17,495 bytes) — Claude Code entrypoint. Project-specific Claude wiring.

### 1.3 Tooling

- `meta-kim-post-copy.mjs` (7,885 bytes) — post-copy installer. References
  `.meta-kim/state/default/post-copy-init.json` (currently not present).
- `gulierp-next` (16,812 bytes) — compiled CLI entrypoint.
- `global.json` (82 bytes) — pins .NET SDK to 10.0.100, `rollForward=latestFeature`.
- `dotnet-tools.json` (195 bytes) — registers `dotnet-ef` 10.0.11.
- `README.md` (565 bytes) — current gate: `GULIERP_GREENFIELD_BOOTSTRAPPED`.

### 1.4 Building Blocks

- `building-blocks/documents/`, `events/`, `numbering/`, `printing/`, `workflow/`
  — the 5 sub-modules that compose GuliERP Next's domain layer (visible as sibling
  sub-projects under `building-blocks/`).

### 1.5 Working Tree State (post G2_DOCNO_002 staging)

- 5 files staged for commit (per `G2_DOCNO_FINAL_CLEANUP_AND_COMMIT_PREP`):
  - `modules/document-kernel/.../DocumentNumberService.cs` (M)
  - `tests/.../DocumentKernelConnectionFixture.cs` (M)
  - `tests/.../DocumentNumberCounterFacts.cs` (M)
  - `docs/verification/G2_DOCNO_002_ENGINE_FIX_REPORT.md` (A)
  - `docs/verification/G2_DOCNO_002_B3_RUNTIME_FINAL_REPORT.md` (A)
- 39 unstaged modified files (pre-existing dirty, not part of any active goal)
- 2,648 untracked files (most of which are build pollution, runtime-browser-profile,
  TestResults, .stack-logs, artifacts/)

---

## 2. What IS MISSING (4 Gaps)

### 2.1 Gap 1: `canonical/` source layer (HIGH PRIORITY)

Meta_Kim's published architecture specifies that runtime trees (`.claude/`, `.codex/`,
`.cursor/`, `openclaw/`) are **projections** of a single canonical source layer
(`canonical/agents/`, `canonical/skills/meta-theory/`, `canonical/runtime-assets/`).
`AGENTS.md` itself states:
> Long-term behavior lives in `canonical/`, `config/contracts/`, and
> `config/capability-index/`. Runtime trees are projections unless explicitly
> documented otherwise.

The `canonical/` directory is absent. This means:
- The 9 meta-agent Markdown files in `.claude/agents/` and `.cursor/agents/` are
  duplicated hand-copies, not regenerated from a source.
- The 9 meta-agent TOML files in `.codex/agents/` are likewise hand-copies.
- A future Meta_Kim version upgrade cannot be applied by running a sync script
  (`npm run meta:sync`); the user must hand-edit 27 files across 3 runtimes.
- Drift between the three runtimes is possible and is **not** detected by any
  hook in the current setup.

**Risk**: HIGH — governance is brittle and upgrade is manual.

**Fix**: Either
1. (Recommended) `npm run meta:bootstrap` or `node meta-kim-post-copy.mjs --auto`
   to regenerate the canonical layer; OR
2. Create `canonical/`, `config/contracts/`, `config/capability-index/` from
   upstream Meta_Kim (cloned from KimYx0207/Meta_Kim @ main).
3. (Backup) Manually re-export from `.codex/agents/*.toml` (the TOML format
   is closer to the canonical schema than Markdown).

### 2.2 Gap 2: `config/` contracts + capability-index source (MEDIUM PRIORITY)

The `config/contracts/` and `config/capability-index/` directories are absent. These
hold the **long-term behavior contracts** (agent dispatch contracts, owner-resolution
contracts, evolution-writeback contracts) that runtime hooks consult via
`config/capability-index/meta-kim-capabilities.json` (which IS present, but is a copy,
not a source).

**Risk**: MEDIUM — capability discovery still works (the JSON is present), but
contract evolution is hard to govern.

**Fix**: Same as Gap 1 — bootstrap from upstream or rebuild from the projected
copies.

### 2.3 Gap 3: `openclaw/` fourth runtime projection (LOW PRIORITY)

`AGENTS.md` lists 4 runtimes: Claude Code, Codex, OpenClaw, Cursor. Only 3 of 4
projections are present. OpenClaw is a more experimental runtime; absence is
**not blocking** but is an inconsistency with the documented architecture.

**Risk**: LOW — OpenClaw is not currently a target runtime for this project.

**Fix**: Defer to when the user explicitly requests OpenClaw. Document the
absence in `GULIERP_AGENT_GOVERNANCE.md` (this audit's TASK 2 deliverable).

### 2.4 Gap 4: `.meta-kim/state/default/` (LOW PRIORITY)

`meta-kim-post-copy.mjs` line 20 references `stateDir = join(rootDir, ".meta-kim",
"state", "default")` and `autoMarkerPath = join(stateDir, "post-copy-init.json")`.
This directory does not exist. The post-copy auto-init marker has never been written.

**Risk**: LOW — the marker is for crash-recovery and post-copy de-duplication; it
is not on the hot path. The absence means the post-copy step will re-run on next
invocation, which is idempotent.

**Fix**: Run `node meta-kim-post-copy.mjs --auto` once. It will create
`.meta-kim/state/default/post-copy-init.json` (or skip if already created).

### 2.5 Gap 5 (BONUS): empty `.mcp.json` and `.cursor/mcp.json` (LOW PRIORITY)

Both files contain only `{"mcpServers": {}}` — no MCP servers registered. The
MCP Memory Service (port 8000) mentioned in `AGENTS.md` is therefore not wired
in. This is a runtime capability loss: the cross-session memory bridge
(`stop-memory-save.mjs`) has no MCP endpoint to call.

**Risk**: LOW — the current goals do not depend on MCP memory; this is a
forward-looking concern.

**Fix**: When MCP Memory Service is deployed, update both files with a server
entry such as:
```json
{
  "mcpServers": {
    "memory": {
      "type": "sse",
      "url": "http://127.0.0.1:8000/sse"
    }
  }
}
```

---

## 3. Hook Surface Comparison (claude vs codex)

| Hook | Claude | Codex | Cursor |
|---|:---:|:---:|:---:|
| `activate-meta-theory-spine` (Skill `meta-theory`) | ✅ | ✅ | ✅ |
| `enforce-agent-dispatch` (PreToolUse Bash\|Edit\|Write\|Agent\|...) | ✅ | ✅ | ✅ |
| `graphify-context` (PreToolUse Bash) | ✅ | ✅ | ✅ |
| `bash-readonly-whitelist` | ✅ | ✅ | ✅ |
| `hook-i18n` | ✅ | ✅ | ✅ |
| `skip-reminder` | ✅ | ✅ | ✅ |
| `spine-state` | ✅ | ✅ | ✅ |
| `utils` | ✅ | ✅ | ✅ |
| `post-format` (PostToolUse Edit\|Write) | ✅ | ❌ | ❌ |
| `post-typecheck` (PostToolUse Edit\|Write) | ✅ | ❌ | ❌ |
| `post-console-log-warn` (PostToolUse Edit\|Write) | ✅ | ❌ | ❌ |
| `subagent-context` (SubagentStart) | ✅ | ❌ | ❌ |
| `stop-compaction` (Stop) | ✅ | ❌ | ❌ |
| `stop-console-log-audit` (Stop) | ✅ | ❌ | ❌ |
| `stop-completion-guard` (Stop) | ✅ | ❌ | ❌ |
| `stop-spine-cleanup` (Stop) | ✅ | ❌ | ❌ |

**Observation**: The 8 Claude-specific hooks (post-*, stop-*, subagent-context) are
Claude Code runtime features (`PostToolUse`, `SubagentStart`, `Stop` events) that do
not exist in Codex or Cursor hook schemas. The fact that the hooks are missing from
Codex/Cursor is therefore **correct**, not a gap.

---

## 4. Agent Roster Comparison

| Agent | Claude | Codex | Cursor | Notes |
|---|:---:|:---:|:---:|---|
| `meta-warden` | ✅ | ✅ | ✅ | Public front door (per `AGENTS.md`) |
| `meta-conductor` | ✅ | ✅ | ✅ | Workflow / stage sequencing |
| `meta-genesis` | ✅ | ✅ | ✅ | SOUL / persona / prompt architecture |
| `meta-artisan` | ✅ | ✅ | ✅ | Skill / MCP / tool fit |
| `meta-sentinel` | ✅ | ✅ | ✅ | Safety / permissions / hooks |
| `meta-librarian` | ✅ | ✅ | ✅ | Memory / continuity / context policy |
| `meta-prism` | ✅ | ✅ | ✅ | Quality review / drift detection |
| `meta-scout` | ✅ | ✅ | ✅ | External capability discovery |
| `meta-chrysalis` | ✅ | ✅ | ✅ | Evolution signal aggregation |
| `analysis` (worker) | ❌ | ✅ | ❌ | Codex-only worker |
| `backend` (worker) | ❌ | ✅ | ❌ | Codex-only worker |
| `docs` (worker) | ❌ | ✅ | ❌ | Codex-only worker |
| `explorer` (worker) | ❌ | ✅ | ❌ | Codex-only worker |
| `frontend` (worker) | ❌ | ✅ | ❌ | Codex-only worker |
| `review` (worker) | ❌ | ✅ | ❌ | Codex-only worker |
| `test` (worker) | ❌ | ✅ | ❌ | Codex-only worker |
| `verify` (worker) | ❌ | ✅ | ❌ | Codex-only worker |
| `worker` (worker) | ❌ | ✅ | ❌ | Codex-only worker |

**Observation**: Claude and Cursor do NOT have built-in worker agents because
their host runtimes provide generic worker primitives (Claude `Agent` tool with
`mavis`/`explore`/`worker`/`verifier`; Cursor multi-agent). Codex requires
explicit TOML-defined workers. This is **correct architecture**, not a gap.

`AGENTS.md` specifies:
> User-visible worker names must be coarse English business role-family names
> such as `frontend`, `backend`, or `test`, not scoped work items or
> host-generated personal nicknames.

The 8 Codex worker agents match this rule. Claude/Cursor worker resolution falls
back to the host runtime's coarse role primitives.

---

## 5. Risks (Ranked)

| # | Risk | Priority | Trigger | Impact | Mitigation |
|---|---|---|---|---|---|
| 1 | `canonical/` source layer missing | HIGH | Any future Meta_Kim upgrade | Hand-edit 27 files across 3 runtimes, drift undetected | Bootstrap from upstream (`npm run meta:bootstrap`) |
| 2 | Capability drift between 3 runtime trees | HIGH | Hand-edits to one runtime not propagated | Inconsistent governance, hook bypass possible | Add `meta:sync:check` CI step |
| 3 | `.mcp.json` empty | MEDIUM | Need cross-session memory | Cannot bridge sessions | Add MCP server entry when memory service deployed |
| 4 | `openclaw/` projection missing | LOW | User requests OpenClaw | Cannot run OpenClaw runtime | Bootstrap when requested |
| 5 | `.meta-kim/state/default/` missing | LOW | Next `meta-kim-post-copy.mjs --auto` run | Auto-init re-runs (idempotent) | Run `meta-kim-post-copy.mjs --auto` once |
| 6 | 39 unstaged modified files (pre-existing) | MEDIUM | Untracked dirty working tree | Bystander effects in `git diff --stat` | Clean working tree before commit (out of audit scope) |
| 7 | 2,648 untracked files (build pollution) | MEDIUM | Builds / runtime-browser-profile / TestResults | Visual noise, accidental commit risk | Add `.gitignore` rules (per `GULIERP_WORKSPACE_CLEANUP_PLAN.md`) |
| 8 | 5 staged files awaiting manual commit | MEDIUM | User not yet executed `git commit` | Engine fix unmerged to master | User review + `git commit` (deferred per user direction) |

---

## 6. Recommendations

### 6.1 Immediate (this audit cycle, no source change)

1. **Document the canonical/ gap** in `GULIERP_AGENT_GOVERNANCE.md` (TASK 2 deliverable).
2. **Add `.gitignore` rules** for `artifacts/`, `.stack-logs/`, `TestResults/`,
   `*.bak`, `.meta-kim/state/`, `apps/*/bin`, `apps/*/obj`, `tests/*/bin`,
   `tests/*/obj`, `tools/*/bin`, `tools/*/obj` (per
   `GULIERP_WORKSPACE_CLEANUP_PLAN.md`, TASK 5 deliverable).
3. **Open a follow-up Goal** `GULIERP_META_KIM_CANONICAL_RESTORE_001` to either
   bootstrap `canonical/` from upstream Meta_Kim or rebuild from projected
   runtime trees. This is the highest-impact gap.

### 6.2 Near-term (next Goal cycle)

4. **Bootstrap `canonical/` from upstream**: `git clone KimYx0207/Meta_Kim /tmp/meta-kim-upstream && cp -r /tmp/meta-kim-upstream/canonical .` then `npm run meta:sync` to regenerate runtime trees.
5. **Wire MCP Memory Service** to `.mcp.json` and `.cursor/mcp.json` when the memory service is available.
6. **Add CI step** `npm run meta:check:global` to detect capability drift between runtime trees.

### 6.3 Long-term

7. **Add OpenClaw projection** if/when the user requests it. Document its absence
   in `GULIERP_AGENT_GOVERNANCE.md` until then.
8. **Promote `meta-kim-post-copy.mjs` to a first-class npm script** in
   `package.json` so the post-copy init is reproducible.

---

## 7. Out-of-Scope for This Audit

This audit deliberately did NOT cover:

- **Source code quality / correctness** — out of scope; would require a separate audit
  (e.g., G2_R0_FOUNDATION_CRITICAL_REVIEW at 59 KB, already exists in
  `docs/review/`).
- **Database / migration review** — `DATABASE_TARGET_REGISTRY.md` (13 KB) already
  covers PostgreSQL target governance.
- **DocumentKernel engine fix correctness** — covered by
  `G2_DOCNO_002_ENGINE_FIX_REPORT.md` + `G2_DOCNO_002_B3_RUNTIME_FINAL_REPORT.md`
  (both staged for commit).
- **Build / runtime hygiene beyond working tree** —
  `GULIERP_WORKSPACE_CLEANUP_PLAN.md` (TASK 5 deliverable) covers the
  `.gitignore` + cleanup recommendations.

---

## 8. Sign-off

**Gate**: `META_KIM_INTEGRATION_AUDIT_COMPLETE_WITH_4_GAPS`

- ✅ 4 runtime projections surveyed (3 complete, 1 missing)
- ✅ 18 Codex agents + 9 Claude agents + 9 Cursor agents inventoried
- ✅ 16 Claude hooks + 8 Codex hooks + 8 Cursor hooks inventoried
- ✅ 2 shared skills in `.agents/` confirmed
- ✅ `AGENTS.md` + `CLAUDE.md` present
- ❌ `canonical/` missing (HIGH)
- ❌ `config/` missing (MEDIUM)
- ❌ `openclaw/` missing (LOW)
- ❌ `.meta-kim/state/` missing (LOW)
- ⚠️ `.mcp.json` + `.cursor/mcp.json` empty (LOW)

**Author**: Mavis (M3 / mavis)
**Date**: 2026-08-25 (Asia/Shanghai)
**Next deliverable**: `GULIERP_AGENT_GOVERNANCE.md` (TASK 2)
