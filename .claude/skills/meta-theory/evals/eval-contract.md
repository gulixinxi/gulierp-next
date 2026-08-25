# SKILL.md Refactor Verification Contract

## Decision Logic Checklist (must survive refactor)

- [ ] Clarity Gate: ask only outcome-branching questions whose answers change deliverable, audience/value, acceptance, owner/capability, permission/risk, or non-goal
- [ ] Architecture Type Pre-judgment: Meta Architecture vs Project Technical Architecture
- [ ] Type A-E routing: 5 types with distinct continuations
- [ ] Fetch-first 3-step: keyword scan → search owner → score+invoke
- [ ] Keyword scan table: tdd/review/security/debug/architecture/frontend/backend/database/DEFAULT

## Execution Steps Checklist (must survive refactor)

- [ ] 8-stage spine: Critical→Fetch→Thinking→Execution→Review→Meta-Review→Verification→Evolution
- [ ] Planning files: task_plan.md, findings.md, progress.md
- [ ] Gate 3 validation: 5-point checklist (agent assigned/no skip-level/correct agents/no gaps/complexity)
- [ ] Factory Station: Genesis→Artisan sequential, Scout/Sentinel/Librarian conditional parallel
- [ ] Type B 5-step pipeline: Discovery→Pre-design→Design→Review→Integration
- [ ] Station Deliverable Contract: Warden/Genesis/Artisan/Sentinel/Librarian/Conductor/Prism/Scout

## Conditions & Triggers Checklist (must survive refactor)

- [ ] Measurable dispatch triggers: 3+ files read / 20+ lines code / multi-module / any file mod / mid-execution catch
- [ ] FORBIDDEN PATHS: 6 anti-patterns listed
- [ ] Gate 3 non-skippable, FAIL override = governance violation
- [ ] User confirmation or explicit allowed skip required before Execution after Fetch evidence and Thinking option framing
- [ ] Capability gap resolution ladder: existing owner → owner upgrade/project-local creation → block or return to Thinking with `capabilityGapPacket`
- [ ] agentInvocationState lifecycle: idle→discovered→matched→dispatched→returned/escalated

## Boundaries Checklist (must survive refactor)

- [ ] Hardcoded agent names FORBIDDEN
- [ ] meta-theory is dispatcher, NOT executor (>3 sentences = violation)
- [ ] Self-Check 4 questions: skip-level/hardcoded/capability-gap/user-bypass
- [ ] Read-only mode still delegable (doesn't revoke agent authorization)

## Evolution Rules Checklist (must survive refactor)

- [ ] Direct over indirect: edit agent SOUL.md, not memory files
- [ ] Evolution writeback table: 7 gap types with targets

## Test Prompts

1. **Type A test**: "Review whether meta-conductor's definition complies with Five Criteria"
   - Expected: classify Type A, Fetch-first search quality review capability, dispatch quality audit agent

2. **Type C test**: "Add retry mechanism to stop-memory-save hook"
   - Expected: classify Type C, measurable trigger (file modification), dispatch execution agent

3. **Ambiguity test**: "Optimize the project"
   - Expected: Clarity Gate fires (≥2 dimensions ambiguous), ask before proceeding

4. **Simple task test**: "What does line 10 in this file mean"
   - Expected: no dispatch needed, answer directly (single file, single question, no modification)

5. **Type B test**: "Create a new meta-auditor agent for runtime health audit"
   - Expected: classify Type B, Factory Station activates, Genesis→Artisan sequential pipeline
