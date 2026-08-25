---
name: same-set-reusable-flow-for-project-file-inventor
version: 0.1.0
author: Meta_Kim
user-invocable: false
candidateType: skill
sourceGapId: gap-create-skill-same-set-reusable-flow--c89068df05
approvalEvidence: warden-approved-file-inventory-skill-2026-06-05
trigger: "project file inventory|file classification|repo cleanup|项目文件整理|文件用途整理|清理项目"
tools:
  - shell
  - filesystem
description: |
  Reusable Meta_Kim file inventory classification flow. It helps separate durable sources, generated evidence, runtime mirrors, temporary state, and risky unknowns before cleanup or commit.
---

# Project File Inventory Classification

Use this skill when a Meta_Kim workspace has too many files and the user needs to know which are useful, generated, risky, or safe to ignore.

## Goal

Produce a source-backed inventory, not a cleanup guess. The output should help the user decide what to keep, commit, ignore, or inspect again.

## Required Evidence

Start with read-only evidence:

1. `git status --short --branch`
2. `git ls-files`
3. `git ls-files --others --exclude-standard`
4. `git check-ignore -v <path>` for suspicious ignored paths
5. `rg -n "<filename-or-command>" package.json docs scripts tests canonical config` for tracked candidates
6. `npm run` script inventory from `package.json`

Do not classify a tracked file as removable only because it has few references. Check package scripts, tests, docs, sync scripts, setup scripts, and contracts first.

## Classification Buckets

- **Core source**: `canonical/`, `config/contracts/`, `config/capability-index/`, durable scripts, package scripts, and tests that define behavior.
- **Runtime projection**: `.claude/`, `.agents/`, `.cursor/`, `openclaw/`, `.codex/` mirrors generated from canonical sources or sync scripts.
- **Generated evidence**: `.meta-kim/state/default/`, graph outputs, reports, SQLite run state, smoke artifacts, and temporary proof files.
- **Planning continuity**: `task_plan.md`, `findings.md`, `progress.md`; keep during active work even if ignored.
- **Reference docs**: tracked docs and plans that explain product, architecture, contracts, or release decisions.
- **Suspicious or cleanup candidate**: files with no package, test, docs, sync, install, contract, or runtime evidence.

## Output Format

Return a compact table with:

- path or pattern
- bucket
- why it exists
- evidence command
- recommendation: keep, commit, ignore, regenerate, inspect again, or cleanup candidate
- risk if deleted

End with a short "do not delete yet" list for any file whose role is uncertain.

## Boundaries

- Do not auto-write canonical state without Warden approval.
- Keep one-run task details in workerTaskPackets, not durable identity.
- Do not delete files as part of inventory classification unless the user separately approves a cleanup action.
- Do not treat runtime mirrors or generated reports as source of truth when canonical or config sources exist.

## Prompt Acceptance

This skill binds the `execution-tools-and-commands`, `capability-discovery-and-retrieval`, `planning-continuity`, `runtime-native-surfaces`, and `verification-eval-and-release` abstract capability families. It may inspect filesystem, Git, scripts, config, docs, and runtime projections, but it does not mutate the workspace by itself.

## Pass

- Inventory output separates source, runtime projection, generated evidence, planning continuity, reference docs, and cleanup candidates.
- Each recommendation names the evidence command that supports it.
- Active `task_plan.md`, `findings.md`, and `progress.md` are preserved as update-only continuity state.
- Unknown or weakly evidenced files are marked `inspect again` rather than deleted or ignored.

## Fail

- A tracked file is called removable without checking scripts, tests, docs, setup, sync, contracts, and runtime projections.
- Runtime mirrors, generated reports, or one-run artifacts are treated as the canonical source of truth.
- The skill recommends deletion, untracking, or canonical writeback without explicit user approval and Warden approval where required.

## Block

Block cleanup or write actions when evidence is missing, Git state is unclear, the file is active planning state, or the user has not explicitly approved mutation.

## Return to stage

Return to Fetch when file purpose or source-of-truth status is unclear. Return to Thinking when the inventory exposes a capability gap or a cleanup path with competing trade-offs. Return to Verification when the classification depends on generated evidence that needs a fresh command.

## Verification

Use read-only commands such as `git status --short --branch`, `git ls-files`, `git ls-files --others --exclude-standard`, `git check-ignore -v <path>`, targeted `rg`, package script inspection, and the most specific validator for the affected file class. Run `npm run meta:prompt:validate` after editing this skill.

## Preserve

Preserve Skills, WebSearch/browser/research, filesystem, shell, apply_patch, MCP, memory, Graphify, graph, hooks, commands, rules, agents, subagents, approval, sandbox, runtime tools, package scripts, setup, sync, install, uninstall, status, doctor, validators, runtime projections, and active planning files.
