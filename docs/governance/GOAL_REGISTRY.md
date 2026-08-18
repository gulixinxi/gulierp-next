# Goal Registry

## Active Goal

| Field | Value |
|---|---|
| Goal | G1A — Core Business Specification Freeze |
| Gate | `GULIERP_CORE_BUSINESS_SPEC_READY_FOR_UX` |
| Status | Spec inputs produced; awaiting user decisions on `G1A_OPERATOR_CONFIRMATION_CHECKLIST.md` |
| Scope | Sales/Purchase/Inventory business spec, FAILED POC gap analysis, UX requirements, evidence matrix, operator confirmation checklist, module scope layering |
| Non-goals | Formal Sales/Purchase/Inventory implementation, UX prototype code, API contract |

## G1A Verification Notes

| Check | Status | Note |
|---|---|---|
| Spec files produced (8) | PASS | `docs/product/specs/*.md` (8 files) |
| Evidence-type classification (USER_CONFIRMED / HANDOFF / DEV_* / FAILED_POC / INFERENCE / OPEN_QUESTION) | PASS | every field/rule tagged; no `INFERENCE` marked Frozen |
| FAILED POC gap analysis | PASS | `FAILED_POC_REQUIREMENT_GAP_ANALYSIS.md` covers 130+ concrete gaps |
| 12-section reverse self-audit | PASS | no POC-as-truth, no DEV-tech-as-spec, no INFERENCE-as-Frozen, all required actions covered, Inventory is not CRUD, no super table |
| Git diff check | N/A | G0 working tree was untracked; G1A adds new untracked spec files only — no source code change |
| Git commit | N/A | task G1A forbids commit/tag/push/rebase; user to commit when ready |
| Backend / Frontend build | N/A | task G1A explicitly forbids .NET / npm — no build expected |

## G1A Residual Risks

| Risk | Mitigation |
|---|---|
| No `USER_CONFIRMED` evidence currently exists | Spec explicitly tags every field; `G1A_OPERATOR_CONFIRMATION_CHECKLIST.md` consolidates decisions |
| Some status/transition names are `INFERENCE` | User must confirm via S-BU-4 / P-BU-5 |
| Inventory spec is dense — risk of user skipping the event-driven vs manual posting distinction | `INVENTORY_BUSINESS_SPEC_V1.md` §7.1 and §7.2 are bolded |
| Tax model has 2 valid interpretations (header vs line rate) | OQ-SO-2 / OQ-PO same in checklist; default = header only |

## Previous Goals

### G0 — GuliERP Greenfield Bootstrap & Business Source of Truth

| Field | Value |
|---|---|
| Gate | `GULIERP_GREENFIELD_BOOTSTRAPPED` |
| Status | Bootstrapped with environment verification gaps |
| Scope | Engineering skeleton, governance, business source of truth, discovery docs |
| Non-goals | Formal Sales/Purchase/Inventory implementation |

### G0 Verification Notes

| Check | Status | Note |
|---|---|---|
| Backend build | Blocked | No .NET SDK installed; runtime only |
| Backend tests | Blocked | No .NET SDK installed; runtime only |
| Frontend install | Blocked | npm cache-only mode; dependency not cached |
| Frontend build | Blocked | `vue-tsc` unavailable because install was blocked |
| Git diff check | PASS | `git diff --check` returned 0 |
| Target path | Blocked | `D:\guli\gulierp-next` creation required escalation, which was rejected by system usage limit |
| Git commit | Blocked | `.git` index write required escalation, which was rejected by system usage limit |

## Gate Rules

- A goal may not start implementation work for core business documents until
  the UI approval gate is satisfied.
- Failed POC code remains reference only.
- Every goal must record verification status and known blockers.
- `GULIERP_CORE_BUSINESS_SPEC_READY_FOR_UX` is the input gate to UX prototype
  (G1B). It does NOT mark the spec as `USER_CONFIRMED` or `FROZEN`.
- `BUSINESS_SPEC_FROZEN` is a separate, future gate that requires the user
  to answer all `BLOCKING_BEFORE_UX` items in
  `G1A_OPERATOR_CONFIRMATION_CHECKLIST.md`.

## Next Goal

| Field | Value |
|---|---|
| Goal | G1B — Core UX Prototype (static, no business implementation) |
| Entry Gate | `GULIERP_CORE_BUSINESS_SPEC_READY_FOR_UX` (current Gate) + user answers to BLOCKING_BEFORE_UX in `G1A_OPERATOR_CONFIRMATION_CHECKLIST.md` |
| Executor | TRAE (static high-fidelity prototype) — not Mavis |
| Hard Stop | No formal Sales/Purchase/Inventory API, database, or page may be created before `USER_UX_APPROVED` |
