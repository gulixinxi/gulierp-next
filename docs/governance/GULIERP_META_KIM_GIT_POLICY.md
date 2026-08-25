# GuliERP Meta_Kim Git Policy

| Field | Value |
|---|---|
| **Document ID** | `GULIERP_META_KIM_GIT_POLICY` |
| **Version** | v1 (initial) |
| **Date** | 2026-08-25 (Asia/Shanghai) |
| **Author** | Mavis (M3 / mavis), acting as Git Governance + Release Manager |
| **Status** | PROPOSAL — awaiting user ratification |
| **Companions** | `GULIERP_GIT_BASELINE_AUDIT_REPORT.md`, `META_KIM_INTEGRATION_AUDIT_REPORT.md` |

This document defines **which Meta_Kim files are part of the GitHub
baseline** (source of truth) and **which are personal runtime state**
(must NOT be committed).

---

## 0. Why This Matters

Meta_Kim has two distinct file categories that look similar but have
very different Git semantics:

1. **Projections** (`.codex/`, `.claude/`, `.cursor/`, `.agents/`) —
   multi-runtime configuration. **MUST be committed** to ensure all
   team members use the same governance. Per `AGENTS.md`:
   > User-visible worker names must be coarse English business
   > role-family names such as `frontend`, `backend`, or `test`.
   > Localized trigger words may be recognized as input, but durable
   > governance files stay English.

2. **State** (`.meta-kim/state/`, `.runtime-browser-profile/`,
   `.stack-logs/`) — per-host runtime artifacts. **MUST NOT be
   committed** (regenerable, host-specific).

This document makes the distinction explicit and records the policy
for the GitHub baseline.

---

## 1. MUST Commit (projections + entrypoints)

### 1.1 Runtime Projections

| Path | Sub-paths | Files in baseline |
|---|---|---:|
| `.codex/` | `agents/`, `capability-index/`, `commands/`, `hooks/`, `hooks.json` | ~30 |
| `.claude/` | `agents/`, `capability-index/`, `commands/`, `hooks/`, `skills/`, `settings.json` | ~36 |
| `.cursor/` | `agents/`, `capability-index/`, `hooks/`, `rules/`, `skills/`, `hooks.json`, `mcp.json` | ~26 |
| `.agents/` | `skills/meta-theory/`, `skills/same-set-reusable-flow-for-project-file-inventor/` | 17 |

**Why commit**: The Meta_Kim 8-stage governance spine, the agent
rosters, and the hook surfaces must be **identical** for every team
member. If a developer does not commit these, their runtime will
behave differently and may bypass governance.

**Per-file policy**:
- `*.md` agent definitions — always commit
- `*.toml` Codex agent definitions — always commit
- `*.mjs` hook scripts — always commit
- `*.json` settings / hooks.json / capability-index — always commit
- `.mdc` Cursor rule files — always commit

**No exceptions** for the projections.

### 1.2 Top-Level Files

| File | Why commit | Notes |
|---|---|---|
| `AGENTS.md` | Codex / OpenCode / Aider / Droid / Trae shared entrypoint | 25 KB, single source of truth for "how to use this repo" |
| `CLAUDE.md` | Claude Code specific entrypoint | 17 KB |
| `meta-kim-post-copy.mjs` | Post-copy installer script | 7.8 KB |
| `gulierp-next` (16 KB binary) | CLI entrypoint | Likely Node-compiled (per `AGENTS.md`); commit and document as `meta-kim-cli` |
| `global.json` | .NET SDK pin (10.0.100) | 82 bytes |
| `dotnet-tools.json` | dotnet-ef registration | 195 bytes |
| `GuliERP.slnx` | Solution file | (already committed) |
| `README.md` | Project identity | 565 bytes (already committed) |
| `.gitignore` | Source tree policy | (already committed, 18 lines) |

### 1.3 docs/governance/ (governance documents)

All 9 untracked files in `docs/governance/` are **MUST commit**:

| File | Bytes | Source |
|---|---:|---|
| `GULIERP_AGENT_GOVERNANCE.md` | 14,079 | This audit |
| `GULIERP_AI_DEVELOPMENT_PROTOCOL.md` | 17,019 | This audit |
| `GULIERP_GOAL_LIFECYCLE.md` | 15,725 | This audit |
| `GULIERP_GREENFIELD_RISK_REGISTER_V1.md` | 23,305 | Pre-existing (untracked) |
| `GULIERP_META_KIM_GOVERNANCE_READY_REPORT.md` | 15,715 | This audit |
| `GULIERP_PROJECT_MEMORY_INDEX.md` | 18,944 | This audit |
| `GULIERP_REPOSITORY_AUTHORITY.md` | 8,717 | Pre-existing (untracked) |
| `GULIERP_WORKSPACE_CLEANUP_PLAN.md` | 13,872 | This audit |
| `META_KIM_INTEGRATION_AUDIT_REPORT.md` | 19,459 | This audit |

**Why commit**: These are the **durable governance rules** that future
Goals will follow. They are the equivalent of a Constitution for the
project.

### 1.4 Sub-rule: governance files stay English

Per `AGENTS.md`:
> Localized trigger words may be recognized as input, but durable
> governance files stay English.

Therefore, governance files in `docs/governance/` are written in
**English with Chinese where explicitly required by the user**. Future
Goals should preserve this convention.

---

## 2. MUST NOT Commit (per-host runtime state)

### 2.1 Post-Copy State

| Path | What it is | Why not commit |
|---|---|---|
| `.meta-kim/state/` | Post-copy init marker, crash-recovery state | Per-host, regenerable, idempotent |
| `.meta-kim/state/default/post-copy-init.json` | Init marker (timestamp) | Per-host |

**Action**: gitignore (per `GULIERP_GITIGNORE_POLICY.md`).

### 2.2 Browser Cache

| Path | What it is | Why not commit |
|---|---|---|
| `.runtime-browser-profile/` | OS-level browser cache (907 files / 158 MB) | Per-host, never source of truth |

**Action**: gitignore.

### 2.3 Log Dumps

| Path | What it is | Why not commit |
|---|---|---|
| `.stack-logs/` | Backend log dumps (18 files / 62 KB) | Per-session, already captured in docs/verification/ |

**Action**: gitignore.

### 2.4 Build Pollution

| Path | What it is | Why not commit |
|---|---|---|
| `*.bak`, `*.bak2` | Build pollution backups from `Move-Item` workarounds | Recovery-only, not source |
| `bin/`, `obj/` (newly built) | .NET build output | Already gitignored (existing) |
| `apps/*/bin/`, `apps/*/obj/` | New build output | Already gitignored via `bin/`/`obj/` |
| `modules/*/bin/`, `modules/*/obj/` | New build output | Already gitignored |
| `tests/*/TestResults/` | TRX test output | Regenerable |
| `*.tsbuildinfo` | TypeScript incremental build | Convention |

**Action**: gitignore (per `GULIERP_GITIGNORE_POLICY.md`).

### 2.5 Workspace-Internal Artifacts

| Path | What it is | Why not commit |
|---|---|---|
| `artifacts/` | Build recovery + PG query helper + API logs (1,286 files / 167 MB) | Workspace-internal, ephemeral |

**Action**: gitignore.

---

## 3. Class C — Defer to Future Goals

The following are **intentionally NOT in this baseline**. They are
preserved as `??` untracked in `git status` until a dedicated Goal
commits them.

| Path | Reason for deferral | Future Goal |
|---|---|---|
| `data/bootstrap/reference/*` (14 files) | Durable business data, but not verified as consumed by any tool | `GULIERP_BOOTSTRAP_REFERENCE_DATA_001` |
| `tools/.quarantine/*` (10+ files) | Intentionally quarantined probe outputs | `GULIERP_QUARANTINE_CLEANUP_001` (decide commit / delete) |
| `tools/discovery/base-000/`, `mdm-000d/`, `sup-001/` | Discovery output, reproducible from source | `GULIERP_DISCOVERY_OUTPUT_001` (decide commit / delete) |

---

## 4. Compatibility with Meta_Kim Documentation

The policy in this document is consistent with:

- `AGENTS.md` ("canonical/ is the source; runtime trees are projections")
  — except `canonical/` itself is missing (per
  `META_KIM_INTEGRATION_AUDIT_REPORT.md` Gap 1, HIGH priority).
- `meta-kim-post-copy.mjs` — references `.meta-kim/state/default/` for
  crash recovery; this policy gitignores it (the marker is
  regenerable).
- `AGENTS.md` Codex Output Rules — "do not output raw Windows paths
  in normal Markdown text" — respected in this document.
- `AGENTS.md` worker naming — "coarse English business role-family
  names" — preserved.

---

## 5. Risks

| Risk | Mitigation |
|---|---|
| Team member's runtime diverges because they didn't pull the projections | The baseline commits are atomic; `git pull` brings the projections. |
| Someone force-adds a state file (e.g., `.meta-kim/state/...`) | The `.gitignore` patch (per TASK 2) blocks this; if force-added, the `git diff` review catches it. |
| Governance file drift between English (canonical) and Chinese (chat) | The policy explicitly preserves this dual-language pattern; chat uses Chinese, durable files use English with Chinese where required. |
| `.agents/` shared skills drift between runtimes | The shared `meta-theory` skill in `.agents/skills/meta-theory/` is the canonical source; the per-runtime copies in `.claude/skills/`, `.cursor/skills/` are projections. |
| Someone commits a personal secret to `secrets/` | The `secrets/` gitignore pattern blocks it; the `.claude/settings.json` deny list provides defense in depth. |

---

## 6. Per-Runtime Policy Summary

### 6.1 Claude Code (`.claude/`)

| Sub-path | Policy |
|---|---|
| `agents/` | Commit all 9 meta-agent `.md` files |
| `capability-index/` | Commit `meta-kim-capabilities.json` |
| `commands/` | Commit slash commands |
| `hooks/` | Commit all 16 `.mjs` hook scripts |
| `skills/` | Commit `meta-theory` and other skill bundles |
| `settings.json` | Commit (permissions + hook surface) |

### 6.2 Codex (`.codex/`)

| Sub-path | Policy |
|---|---|
| `agents/` | Commit all 18 `.toml` agent files (9 meta + 9 worker) |
| `capability-index/` | Commit `meta-kim-capabilities.json` |
| `commands/` | Commit `meta-theory.md` slash command |
| `hooks/` | Commit all 8 `.mjs` hook scripts |
| `hooks.json` | Commit hook wiring |

### 6.3 Cursor (`.cursor/`)

| Sub-path | Policy |
|---|---|
| `agents/` | Commit all 9 meta-agent `.md` files |
| `capability-index/` | Commit `meta-kim-capabilities.json` |
| `hooks/` | Commit all 8 `.mjs` hook scripts |
| `rules/` | Commit all 4 `.mdc` rule files |
| `skills/` | Commit skill bundles |
| `hooks.json` | Commit hook wiring |
| `mcp.json` | Commit (currently empty `{"mcpServers": {}}`) |

### 6.4 Shared (`.agents/`)

| Sub-path | Policy |
|---|---|
| `skills/meta-theory/` | Commit all 17 files (SKILL.md + 15 reference docs + 1 eval) |
| `skills/same-set-reusable-flow-for-project-file-inventor/` | Commit SKILL.md |

---

## 7. Sign-off

**Gate**: `GULIERP_META_KIM_GIT_POLICY_V1_PROPOSAL`

- ✅ MUST commit: 4 runtime projections + 5 entrypoints + 9 governance files
- ✅ MUST NOT commit: 4 categories of per-host state (post-copy / browser / logs / build)
- ✅ Class C: 3 deferred groups (data / quarantine / discovery)
- ✅ Compatible with `AGENTS.md` and `meta-kim-post-copy.mjs`
- ✅ Per-runtime policy table for 4 runtimes
- ✅ 5 risks identified with mitigations

**Author**: Mavis (M3 / mavis)
**Date**: 2026-08-25 (Asia/Shanghai)
**Status**: PROPOSAL — awaiting user ratification
**Next deliverable**: `GULIERP_BASELINE_COMMIT_PLAN.md` (TASK 4)
