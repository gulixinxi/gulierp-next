# GULIERP_BASELINE_FREEZE_V0_9_COMMIT_PLAN

Date: 2026-09-10
Status: DRAFT - DO NOT COMMIT YET

No commit is permitted from the current worktree because the V0.9 verified gate is not met.

## Gate Before Any Commit

All must be true before staging:

- sensitive scan candidateCount is zero and reviewed dispositions are recorded;
- all 79 inherited D_REVIEW_REQUIRED paths have per-file source, value, risk, and commit-group decisions;
- no C_TEMPORARY path is staged;
- real-DB tests remain explicitly excluded from the offline suite;
- dotnet build, offline backend suite, frontend typecheck/build, security selftest, sensitive scan, and diff check pass;
- candidate reproduction is proven from a committed or exact-index state.

## Proposed Logical Groups

1. baseline/security-redaction

Exact candidate files:

- tests/GuliERP.Mdm.Tests/ReferenceSeedIntegrationFacts.cs
- tools/dev/assert-gulierp-db-target.ps1
- tools/dev/g2-001-operator-evidence.ps1
- tools/dev/g2-003-operator-evidence.ps1
- tools/dev/g2-004-bootstrap-operator-user.ps1
- tools/dev/g2-004-operator-evidence.ps1
- tools/dev/g2-005-operator-evidence.ps1
- tools/dev/mdm-001-operator-evidence.ps1
- tools/dev/Mdm001Acceptance.Harness.ps1
- tools/dev/provision-web-preview-user.ps1
- tools/dev/baseline-v09-security-selftest.ps1
- tools/dev/baseline-sensitive-scan.py
- .env.example

2. baseline/offline-verification

Exact candidate files:

- tools/dev/verify-baseline-offline.ps1
- tests/GuliERP.Api.Tests/SalesRuntimeRegressionSourceFacts.cs

3. baseline/docs-freeze-reports

Exact candidate files:

- README.md
- docs/business/GULIERP_BUSINESS_ROADMAP_001.md
- docs/governance/GULIERP_PROJECT_MEMORY_INDEX.md
- docs/verification/LOCAL_VERIFY.md
- docs/verification/GULIERP_WORKTREE_BASELINE_AUDIT_V1.md
- docs/verification/GULIERP_CHANGE_CLASSIFICATION_V1.md
- docs/verification/GULIERP_BASELINE_FREEZE_V0_9_REPORT.md
- docs/verification/GULIERP_BASELINE_FREEZE_V0_9_CLOSURE_REPORT.md
- docs/verification/GULIERP_BASELINE_FREEZE_V0_9_COMMIT_PLAN.md
- docs/verification/GULIERP_D_REVIEW_REQUIRED_AUDIT_V1.md
- docs/verification/GULIERP_BASELINE_SENSITIVE_SCAN_ADJUDICATION_V1.md
- docs/verification/baseline-v09-closure/task_plan.md
- docs/verification/baseline-v09-closure/findings.md
- docs/verification/baseline-v09-closure/progress.md
- docs/verification/baseline-v09-closure/original-107.json
- docs/verification/baseline-v09-closure/start-worktree.json
- docs/verification/baseline-v09-closure/temp-audit.json
- docs/verification/baseline-v09-closure/d-review-evidence.json

4. inherited-sales-v1-candidate

Status: HOLD. The Sales backend/frontend/migration/test changes may be product-valid inherited WIP, but they include model/API/UI behavior changes that predate this Goal. They require a separate exact per-file review before staging.

5. inherited-reference-docs-candidate

Status: HOLD. Business/design/reference reports require authority and sensitive-candidate review before staging.

6. orphan-quarantine

Status: HOLD. Deleted LookupDialog/mock files and quarantine copies must be preserved locally until final deletion intent is approved. Do not stage temporary quarantine as source.

## Forbidden Staging

Never stage by directory or wildcard while this plan is active. Use explicit path staging only and inspect git diff --cached --name-only before each commit.
