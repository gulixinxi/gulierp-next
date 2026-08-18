# Meta_Guli Governance V1

| Field | Value |
|---|---|
| Goal | G1A-FINAL — Operator Decision Writeback & Business Spec Freeze |
| Gate (entry) | `GULIERP_CORE_BUSINESS_SPEC_READY_FOR_UX` |
| Gate (exit) | `GULIERP_CORE_BUSINESS_SPEC_FROZEN` |
| Document status | **Frozen at G1A-FINAL** — top-level governance |
| Authority | This file is the **meta-governance**. It governs all other GuliERP governance. |
| Co-rule | `GULIERP_MODULE_INDEPENDENCE_RULE.md` (DEC-MODULE-001) |

> This is the lesson-record governance for GuliERP. It documents the
> development loop, the three product gates, and the
> "build-pass ≠ business-pass" lesson that comes from the failed POC.

---

## 1. The development loop (closed loop)

Every product-grade change in GuliERP Next MUST traverse the
following 14 steps. Skipping any step is a governance violation.

```
Intent
  → Evidence Fetch
    → Business Spec
      → Operator Business Decision
        → BUSINESS_SPEC_FROZEN
          → UX Prototype
            → USER_UX_APPROVED
              → Architecture
                → API Contract
                  → Implementation
                    → Review
                      → Meta Review
                        → Automated Verification
                          → OPERATOR_RUNTIME_ACCEPTED
                            → Evolution Writeback
```

### 1.1 Step definitions

| # | Step | Output | Owner | Required? |
|---|---|---|---|---|
| 1 | **Intent** | A one-sentence product/engineering need | user | YES |
| 2 | **Evidence Fetch** | Cited sources, evidence types, contradictions, gaps | Mavis | YES |
| 3 | **Business Spec** | `docs/product/specs/*.md` per scope | Mavis | YES |
| 4 | **Operator Business Decision** | `docs/product/specs/G1A_DECISIONS_V1.md` (or successor) | user | YES for blocking items |
| 5 | **BUSINESS_SPEC_FROZEN** | Gate advance in GOAL_REGISTRY | Mavis (mechanical) + user (sign-off) | YES |
| 6 | **UX Prototype** | Static Vue prototype, mock data | TRAE | YES before any code |
| 7 | **USER_UX_APPROVED** | User sign-off on UX | user | YES |
| 8 | **Architecture** | Module boundaries, dependencies, contracts | Mavis | YES |
| 9 | **API Contract** | `docs/api-contract/*.md` + OpenAPI | Mavis | YES |
| 10 | **Implementation** | Domain / Application / Infrastructure / Host | worker / Mavis | YES |
| 11 | **Review** | Code review, architecture tests, security | verifier | YES |
| 12 | **Meta Review** | Check the loop itself was respected | verifier / Mavis | YES |
| 13 | **Automated Verification** | Build / Unit / Integration / Runtime smoke | CI | YES but **not sufficient** |
| 14 | **OPERATOR_RUNTIME_ACCEPTED** | Real PC flow evidence (per `POC003_OPERATOR_EVIDENCE_PACK` pattern) | user (operator) | YES for PRODUCTIZATION |
| ↻ | **Evolution Writeback** | Update spec / decisions / lessons for next loop | Mavis | YES |

### 1.2 Hard rules

These rules are the **hard core** of this governance. They cannot be
overridden by individual goals.

| # | Rule | Why |
|---|---|---|
| HR-1 | **Automated Test PASS ≠ Business Goal PASS.** | Build / unit / integration / runtime smoke are necessary but never sufficient for a business module to be "done". |
| HR-2 | **Build PASS ≠ User Usability PASS.** | Code that compiles and runs does not mean the user can do real work with it. |
| HR-3 | **Route PASS ≠ ERP Product PASS.** | A 200 on a URL does not mean the product is correct. |
| HR-4 | **No formal UI / API / DB implementation before `USER_UX_APPROVED`.** | The failed POC shipped a SalesOrder PC flow with code that passed smoke but failed the user. Don't repeat. |
| HR-5 | **No `PRODUCTIZATION_VERIFIED` before `OPERATOR_RUNTIME_ACCEPTED`.** | A real operator (the user) must run the flow against real data. |
| HR-6 | **No silent governance change.** | All changes to governance files require an explicit decision record. |
| HR-7 | **No silent scope expansion.** | If a goal's scope grows, the goal must be replanned and re-approved. |
| HR-8 | **No code freeze without evidence.** | "Looks done" is not a closure. Evidence is a Closure Report. |
| HR-9 | **No `INFERENCE` marked Frozen.** | Only `USER_CONFIRMED` may be marked Frozen. (Per G1A §三 / §九.) |
| HR-10 | **No reusing failed POC implementation as truth.** | Anti-pattern inventory only. |

---

## 2. The three product gates

A **core business module** (Sales, Purchase, Inventory, Production,
Quality, Finance) MUST pass through these three gates before it can
be declared `PRODUCTIZATION_VERIFIED`:

```
G1: BUSINESS_SPEC_FROZEN
G2: USER_UX_APPROVED
G3: OPERATOR_RUNTIME_ACCEPTED
```

### 2.1 G1 — BUSINESS_SPEC_FROZEN

| Aspect | Value |
|---|---|
| Definition | User has confirmed all `BLOCKING_BEFORE_UX` items in the spec's confirmation checklist. |
| Owner | user (decision) + Mavis (record) |
| Output | `docs/governance/GOAL_REGISTRY.md` updated to `<module>_BUSINESS_SPEC_FROZEN` |
| Forbidden next step until passed | UX prototype may start (G1B) — but no API/DB/code for the module. |

### 2.2 G2 — USER_UX_APPROVED

| Aspect | Value |
|---|---|
| Definition | User has clicked through the static Vue prototype (TRAE), confirmed the screens match the spec and the user's mental model. |
| Owner | user |
| Output | `docs/verification/<module>_UX_APPROVAL_REPORT.md` (per spec UX requirement) |
| Forbidden next step until passed | API contract, code, migration. |

### 2.3 G3 — OPERATOR_RUNTIME_ACCEPTED

| Aspect | Value |
|---|---|
| Definition | Real PC flow with real data (e.g. PostgreSQL on NAS) executed by the operator. Trx files parsed, audit log verified, screenshots captured. Pattern: `docs/agent-handoff/POC003_OPERATOR_EVIDENCE_PACK.md`. |
| Owner | user (operator) |
| Output | `docs/verification/<module>_OPERATOR_RUNTIME_ACCEPTED_REPORT.md` + trx artifacts |
| Forbidden next step until passed | `PRODUCTIZATION_VERIFIED` gate. |

### 2.4 Relationship to existing gates

| Existing gate | Relationship |
|---|---|
| `GULIERP_GREENFIELD_BOOTSTRAPPED` (G0) | Pre-G1. Greenfield skeleton + SOT. |
| `GULIERP_CORE_BUSINESS_SPEC_READY_FOR_UX` (G1A pre-FINAL) | Pre-G1. Materials sufficient for UX. Spec is **not** Frozen. |
| `GULIERP_CORE_BUSINESS_SPEC_FROZEN` (G1A-FINAL) | **G1** for the core (Sales/Purchase/Inventory) as a whole. |
| `<module>_UX_APPROVED` (G1B) | **G2** for a specific module. |
| `<module>_BUSINESS_SPEC_FROZEN` (later) | **G1** for that module if it diverges from the core spec. |
| `<module>_OPERATOR_RUNTIME_ACCEPTED` (G1+runtime) | **G3** for a specific module. |
| `<module>_PRODUCTIZATION_VERIFIED` (V1) | Final gate. Requires G1+G2+G3. |

---

## 3. Evolution Writeback (the closed loop's loop)

After every G3 (or after every hard lesson), the system MUST run
**Evolution Writeback**:

1. Specs that were wrong → patch and reissue.
2. Decisions that were incomplete → add new DEC-xxx.
3. Architecture rules that proved insufficient → amend.
4. Anti-patterns that re-appeared → re-record in
   `FAILED_POC_REQUIREMENT_GAP_ANALYSIS.md`.
5. New lessons → add as `LESSON-NNN` in this file.

This is what closes the loop. Without this, every iteration is a
fresh start.

---

## 4. LESSON-001

### 4.1 Title

**"Automated Build/Test/Route/Runtime smoke all PASS, yet the ERP
business is not usable."**

### 4.2 Subject

The failed Sales / Purchase / Inventory POC on Admin.NET.

### 4.3 The data

| Signal | Status in failed POC |
|---|---|
| Build | PASS |
| Unit tests | PASS |
| Architecture tests | PASS |
| Runtime smoke | PASS |
| HTTP route | PASS |
| **Real user opens the screen, creates an order, ships goods, reconciles** | **FAIL — flow did not match business reality; fields wrong; states wrong; UX did not fit operator habits** |

The automated signals were all green. The product was not usable.

### 4.4 Root cause

1. **Metadata-driven UI/rule engine** (`JU_CtrlRule.SqlSchema ntext`
   with `AllowManualSql=1`, `JU_TemplateTableField` × 6,339 fields)
   meant no compile-time guarantee that any specific business rule
   was correct. The system was configurable, but configuration is not
   correctness.
2. **Domain knowledge was pushed to the database** (text-name
   relationships; 0 FK; 0 CHECK; computed balance) so the database
   could not catch business violations.
3. **The UI was reactive to metadata**, so designers could create
   shapes that compiled, ran, and stored data — but the shapes
   themselves were wrong (missing fields, wrong states, wrong
   permissions).
4. **The "evidence" was the smoke test**, which was the same code
   reading the same data — a closed loop that could not detect
   semantic errors.

### 4.5 Rule (FROZEN)

`META_GULI_GOVERNANCE_V1.md` HR-1 / HR-2 / HR-3 are the operational
form of this lesson.

| Rule | What it forbids |
|---|---|
| HR-1 | Treating automated test PASS as business PASS. |
| HR-2 | Treating build PASS as user-usability PASS. |
| HR-3 | Treating route PASS as product PASS. |
| HR-4 | Implementing core business UI/API/DB before `USER_UX_APPROVED`. |
| HR-5 | Declaring `PRODUCTIZATION_VERIFIED` before `OPERATOR_RUNTIME_ACCEPTED`. |

### 4.6 Counter-signal

What would have caught it:

| Counter-signal | When it would fire |
|---|---|
| Static UX prototype (G1B) reviewed by user before code | Before implementation |
| `USER_UX_APPROVED` gate | Before implementation |
| Real operator PC flow with real data | Before `PRODUCTIZATION_VERIFIED` |
| User reading `BUSINESS_SPEC` and answering all BLOCKING items | Before UX prototype |
| Architecture test: "no `text-name` business references" | At compile time, but only for new code |

The new GuliERP Next has all five counter-signals in place via
this governance. The failed POC had none.

### 4.7 Permanent record

This lesson is permanent. It cannot be deleted. Any agent or
human who works on GuliERP Next must read it before claiming any
business module is "done".

---

## 5. Anti-patterns to never repeat (cross-link)

See `FAILED_POC_REQUIREMENT_GAP_ANALYSIS.md` for the detailed list
(130+ gaps, 20 anti-patterns). The most critical anti-patterns are:

1. No metadata-driven business rules (no `ntext` SQL).
2. No text-name business relationships (no `nvarchar(256)` customer).
3. No manual "过账" screen.
4. No balance directly editable.
5. No `image`/`ntext` files in DB.
6. No `WorkflowStatus nvarchar(512)`.
7. No default password `123456`.
8. No `SpecViewRightFilter` only — must enforce at write time.
9. No "open metadata designer to change behaviour".
10. No `INFERENCE` marked Frozen.

---

## 6. The agent contract

Every agent (Mavis, worker, verifier, explore) MUST:

1. Read this file before any GuliERP task.
2. Apply the 14-step loop.
3. Apply the three product gates.
4. Apply the 10 hard rules.
5. Apply the LESSON-001.
6. Apply the module independence rule
   (`GULIERP_MODULE_INDEPENDENCE_RULE.md`).
7. Never claim a core business module is "done" without G3.

Failure to apply any of the above is a governance violation and
grounds for stopping the agent's run.

---

## 7. Cross-references

- `GULIERP_MODULE_INDEPENDENCE_RULE.md` (DEC-MODULE-001)
- `ARCHITECTURE_RULES.md`
- `AGENT_WORK_RULES.md`
- `BUSINESS_SOURCE_OF_TRUTH.md`
- `FAILED_POC_REQUIREMENT_GAP_ANALYSIS.md`
- `G1A_DECISIONS_V1.md`
- `G1A_FINAL_FREEZE_REPORT.md`

---

## 8. Frozen-state attestation

This document is **Frozen at G1A-FINAL** per the
`GULIERP_CORE_BUSINESS_SPEC_FROZEN` gate. Amendments require:

- New decision record (`DEC-META-NNN` or domain-equivalent).
- User approval.
- Re-issuance of this file.
