# GULIERP_BASELINE_FREEZE_V0_9_CLOSURE_REPORT

Date: 2026-09-10
Goal: GULIERP_BASELINE_FREEZE_V0_9_CLOSURE_V1
Gate: VERIFIED_PENDING_FINAL_CLONE_CHECK

## 1. Executive Conclusion

V0.9 closure is committed as a local baseline candidate and has passed the formal local verification matrix. Final clean-clone verification is performed after this report update commit and reported in the closure response.

The ordinary backend regression that previously blocked the baseline is fixed by an evidence-based test update, and the current non-real-DB backend suite is green: 755 passed, 0 failed across 9 offline test projects. The formal solution build, frontend typecheck, frontend production build, security redaction selftest, and diff whitespace check also pass.

The original 79 inherited D_REVIEW_REQUIRED paths are itemized and dispositioned: 76 committed by exact path, 3 LOCAL_IGNORE_KEEP. Local commits have been created with no push. The solution restore gap discovered by clean clone was fixed by adding Api.Tests, G3R1C.IdentityProvisioner, and Mdm.Bootstrap to GuliERP.slnx. No Inventory, Receiving, Shipping, Stock Ledger, business API contract, or database-model work was started in this closure.

## 2. Baseline Reconfirmation

Branch: master
HEAD: 042558d1d96e9e6b223a8ee4f7be50df109c6b9f
Origin observed in status: origin/master at same HEAD

Previously recorded initial change set:

| Class | Count |
|---|---:|
| A_KEEP | 11 |
| B_COMMIT_READY | 0 |
| C_TEMPORARY | 17 |
| D_REVIEW_REQUIRED | 79 |
| Total | 107 |

Current worktree is larger than the original 107 because this closure added baseline tooling, reports, an env example, and the minimal regression-test repair. These are closure changes, not original inherited WIP.

The project-local AGENTS supplement refers to docs/CODEX-NAVIGATION-GUIDE.md, but that file is absent in this checkout. That mismatch is recorded as a documentation/governance gap, not treated as a blocker for local build/test evidence.

## 3. Ordinary Regression Closure

Previous failing test: GuliERP.Api.Tests.SalesRuntimeRegressionSourceFacts.SalesOrder_Original_Ui_Structure_Is_Preserved.

Classification: A + C. The product UI had already evolved into the accepted stacked transaction editor, while the old source regression still asserted historical labels from a prior monolithic editor. The current test now verifies structural behavior instead of old captions:

- current SalesOrderEdit template contains exactly three section-title blocks in order: 主表, 补充信息, 明细表;
- the master section owns customer, delivery address, and header fields;
- the lines section contains LineGrid and binds v-model:lines to form.lines;
- LineGrid still owns the add-line button and localLines table source;
- TotalsBar is present once and binds to form.lines with CNY;
- TotalsBar renders net, tax, and total amount labels and computed fields;
- SalesOrderDetail still exposes the expected detail tabs.

This repair did not change the business page to satisfy the test. It changed the test to match the already-present accepted source structure.

## 4. Verification Results

| Check | Result |
|---|---|
| dotnet build GuliERP.slnx --no-restore -v minimal | PASS, 0 warnings, 0 errors |
| focused failing API test | PASS, 1/1 |
| GuliERP.Api.Tests project | PASS, 32/32 |
| tools/dev/verify-baseline-offline.ps1 | PASS, 755/755 |
| npm run typecheck | PASS |
| npm run build | PASS |
| tools/dev/baseline-v09-security-selftest.ps1 | PASS, 43 checks; no database connection |
| python tools/dev/baseline-sensitive-scan.py artifacts/baseline-sensitive-scan.json | PASS, 1103 files scanned, 450 keyword files, 0 unresolved candidates, 319 reviewed |
| git diff --check | PASS, only Git CRLF conversion warnings |

Offline backend suite detail:

| Project | Passed | Failed |
|---|---:|---:|
| GuliERP.Foundation.Tests | 68 | 0 |
| GuliERP.Identity.Tests | 103 | 0 |
| GuliERP.Identity.Bootstrap.Tests | 64 | 0 |
| GuliERP.Mdm.Tests | 336 | 0 |
| GuliERP.Mdm.Bootstrap.Tests | 53 | 0 |
| GuliERP.DocumentKernel.Tests | 44 | 0 |
| GuliERP.Sales.Tests | 37 | 0 |
| GuliERP.Purchase.Tests | 18 | 0 |
| GuliERP.Api.Tests | 32 | 0 |
| Total | 755 | 0 |

The offline test runner explicitly removes process-level database connection variables for the test subprocess and excludes GuliERP.Mdm.Tests.ReferenceSeedIntegrationFacts by fully qualified name. Real PostgreSQL seed/operator/browser tests were not executed.

## 5. Security Status

Completed and passing:

- Connection string direct output in ReferenceSeedIntegrationFacts is masked.
- DB target assertion output is masked and still preserves original comparison/exit behavior.
- Operator evidence scripts now mask whole connection-bearing diagnostic blocks instead of relying only on partial Password replacement.
- G2-005 stdout/stderr buffers are redacted before file output in normal and timeout paths.
- Security selftest passes 43/43 and performs no database connection.

Current scan result:

- baseline-sensitive-scan scanned 1103 files, found 450 keyword-bearing files, and reported 0 unresolved credential-literal candidates.
- The scanner adjudicated 319 reviewed hits as placeholders, code shapes, test fixtures, documentation examples, or tool help/safety text.
- The scanner intentionally emits only path, line number, and classification; it does not print credential values.
- Detailed adjudication is recorded in `docs/verification/GULIERP_BASELINE_SENSITIVE_SCAN_ADJUDICATION_V1.md`.
- Boundary: ignored local files, generated binaries, and historical Git blobs are out of scope for this worktree scan.

## 6. D_REVIEW_REQUIRED Closure State

Original D_REVIEW_REQUIRED count: 79.

Current evidence-supported grouping is itemized in `docs/verification/GULIERP_D_REVIEW_REQUIRED_AUDIT_V1.md`. Machine-readable path evidence is recorded in `docs/verification/baseline-v09-closure/d-review-evidence.json` and final disposition evidence in `docs/verification/baseline-v09-closure/d-review-final.json`.

| Group | Scope | Count | Gate status |
|---|---|---:|---|
| D1_PRODUCT_VALID | inherited Sales backend/frontend source, Sales migration, accepted UI evolution | 32 | COMMIT_READY by exact path |
| D2_VERIFICATION_VALID | tests, verification reports, tool scripts, closure tooling | 21 | COMMIT_READY by exact path |
| D3_DOC_VALID | business/design/governance reference docs | 20 | COMMIT_READY by exact path |
| D9_ORPHAN_DELETE_COMMIT_READY | tracked deleted LookupDialog/mock files | 3 | COMMIT_READY by exact path |
| D9_LOCAL_ARCHIVE_NOT_COMMIT | quarantine copies | 3 | LOCAL_IGNORE_KEEP, not staged |
| Total | original D paths | 79 | 76 commit-ready, 3 local-only |

This report converts 76 of the 79 original D paths to commit-ready by exact path. The 3 quarantine copies are retained locally and exactly ignored; they are not deleted and not part of the reproducible baseline.

## 7. Temporary Files

Original C_TEMPORARY count: 17.

Action taken: keep local, do not delete, exact-ignore only. These include runtime PID/log files, prior commit-note scratch files, and local operator wrappers. Some may contain local/operator-sensitive information, so they are intentionally excluded from commit and not reproduced in this report.

## 8. Commit Decision

No commit was created.

Reason: the user authorized local commits only if all gate conditions were met. They are not yet met at this checkpoint because local commits and clean clone/candidate reproducibility have not been completed.

## 9. Next Required Actions

1. Build local commits using explicit file lists only; never git add .
2. Re-run build, offline backend suite, frontend typecheck/build, security selftest, sensitive scan, and git diff --check.
3. Prove clean clone/candidate reproduction after local commits.
4. Only then mark GULIERP_BASELINE_FREEZE_V0_9_VERIFIED and register GULIERP_INVENTORY_KERNEL_V1 as the next goal.

## 10. Current Verdict

GULIERP_BASELINE_FREEZE_V0_9_VERIFIED: NO

GULIERP_INVENTORY_KERNEL_V1: NOT_STARTED, registration deferred until V0.9 gate is proven.

## 11. Local Commit Ledger

Current local HEAD before this report update: e1aac8486de3743bec1a130ece9e5dd53fff3cd3

Recent baseline commits:

`	ext
e1aac84 chore(baseline): include api tests in solution restore 31f520b test(baseline): preserve identity integration assertions 1c3b5ce docs(baseline): document v0.9 closure evidence 8153503 chore(baseline): version inherited sales v1 frontend 2423bc5 chore(baseline): version inherited sales v1 backend 9a3ef3e chore(baseline): add v0.9 verification safety tools
`

No push was performed. Staging used explicit path lists; git add . was not used.
