# GULIERP_MASTER_DATA_FOUNDATION_CLOSURE_MANIFEST_001

Date: 2026-08-29
Repository: `D:\guli\projects\gulierp-next`
Branch: `master`
Base HEAD: `139fe1e940258d71b85d328d88b4e1358c0f7b1e`

## Closure Rule

This manifest closes already-implemented MiniMax Foundation work. It does not
reimplement, regenerate migrations, refactor style, or commit raw operator
datasets.

## FOUNDATION_OWNED

- MasterDataCodeRule and MasterDataCodeSequenceState domain, persistence,
  service, bootstrap, startup registration, and focused tests.
- BusinessPartner server-side auto-code integration and additive address /
  mnemonic / administrative-region fields.
- Country and AdministrativeRegion domain, persistence, ISO 3166 seed, MCA
  operator import service, and reference-data API.
- MDM003, MDM004, and MDM005 migrations, designers, and model snapshot.
- Foundation-focused MDM, MDM integration, API compatibility tests.
- Operator scripts for MCA snapshot download and Foundation evidence collection.
- Foundation implementation and wave evidence reports.

## FOUNDATION_REQUIRED_DEPENDENCY

- BusinessPartner frontend API/types/page changes for auto-code UX, Country
  selector, CN region cascader, municipality path, international fallback,
  mnemonic search, legacy address preservation, and table-width fixes.
- Shared MDM drawer/list-toolbar/table sizing files required by the
  BusinessPartner page acceptance path.
- API startup placeholder fail-close required for honest operator evidence when
  no real PostgreSQL connection string is present.
- Sales facade regression assertion and Snowflake JSON converter compatibility
  needed to keep the current API test suite green after the additive MDM DTO
  shape.

## EXCLUDED_WIP

The following dirty files are intentionally excluded from this Foundation
closure commit:

- Login page hero/background assets.
- Router lazy-loading and Vite chunk optimization.
- Item list and SalesOrder visual/layout changes not required by this closure.
- Employee/Identity integration reports and tests.
- Generic dev stack scripts and quarantined helper scripts.
- Goal registry edits with unrelated active-goal history.
- Raw operator artifacts under `artifacts/operator/...`.
- `.stack-pids.json`.

## Verification Snapshot

- PowerShell parse: `tools/dev/gulierp-master-data-foundation-operator-evidence.ps1` PASS.
- PowerShell parse: `tools/dev/gulierp-download-mca-cn-region-snapshot.ps1` PASS.
- Sensitive scan over staged Foundation docs/scripts PASS.
- `dotnet build GuliERP.slnx --no-restore --disable-build-servers -m:1 -v:minimal` PASS after clearing Windows file-lock/build-server access issue.
- `dotnet test tests\GuliERP.Mdm.Tests\GuliERP.Mdm.Tests.csproj --no-restore --no-build` PASS: 319/319.
- `dotnet test tests\GuliERP.Api.Tests\GuliERP.Api.Tests.csproj --no-restore --no-build` PASS: 32/32.
- `npm run build` in `apps/web` PASS; Rollup PURE annotation warnings are upstream dependency noise.
- Current shell MDM integration test run failed because the connection string resolved to placeholder `CHANGE_ME`; classified as environment/baseline gate, not a new correctness defect. Existing MiniMax operator evidence records MDM Integration 12/12 PASS with real PostgreSQL.

## Operator Evidence Accepted For Closure

- Country seed count: 249.
- MCA AdministrativeRegion count: 3213.
- 山东 -> 济南 -> 区县 PASS.
- 河北 -> 石家庄 -> 区县 PASS.
- 北京 -> 区 PASS.
- PostgreSQL cross-process BusinessPartner code concurrency: 20/20 PASS.
- Browser smoke: 8/8 PASS.
- MDM tests: 319/319 PASS.
- MDM integration tests: 12/12 PASS in operator environment.
- API tests: 32/32 PASS.
- Frontend typecheck/build PASS.
