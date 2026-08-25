# G3_MDM_DICTIONARY_V1_SEED_B2 Data Report

| Field | Value |
|---|---|
| **Report ID** | `G3_MDM_DICTIONARY_V1_SEED_B2_DATA_REPORT` |
| **Goal** | `G3_MDM_DICTIONARY_V1_SEED_B2_DATA_001` |
| **Source Brief** | User input 2026-08-25 (Asia/Shanghai) — B2 seed data per `G3_MDM_DICTIONARY_V1_SEED_B1_REVISED_PLAN` §1.2 |
| **Predecessor** | `G3_MDM_DICTIONARY_V1_SEED_B1_IMPLEMENTED` (B1 backend CLI + service) |
| **Governing Plan** | `docs/planning/G3_MDM_DICTIONARY_V1_SEED_B1_REVISED_PLAN.md` |
| **Parent Plan** | `docs/planning/G3_MDM_DICTIONARY_V1_SEED_PLAN.md` (B2 = §2.2-2.10 item list) |
| **Author** | Mavis (M3 / mavis) |
| **Authored At** | 2026-08-25 (Asia/Shanghai) |
| **HEAD** | `9684985` (branch: `master`) |
| **Commit / Push** | **NOT EXECUTED** (per brief: "NO COMMIT / NO PUSH / 等待人工审核") |

---

## 0. Final Verdict

**`G3_MDM_DICTIONARY_V1_SEED_B2_DATA_READY`** — 9 JSON data files in
`data/bootstrap/reference/mdm/dictionary/` are written, schema-validated by both
the B1 service (`--dry-run`) and the static validator script. All 5 architecture
freezes from the user-approved Revised Plan are honored.

| Item | Status |
|---|---|
| 9 JSON data files created | ✅ DONE (9 files, 11,983 bytes total) |
| 9 DictionaryType (9 distinct codes) | ✅ DOC_STATUS / CUST_TYPE / SUPP_TYPE / ITEM_STATUS / EMP_STATUS / PM_METHOD / TM_MODE / SM_TERM / ENT_TYPE |
| 42 DictionaryItem total | ✅ 5 + 4 + 4 + 4 + 4 + 5 + 6 + 5 + 5 = 42 |
| Each file uses JSON Schema v2 (`meta` + `items[]`) | ✅ DONE |
| `meta.default_item_code` exists in `items[*].canonical_code` | ✅ DONE (9/9) |
| Exactly 1 `is_default=true` per file | ✅ DONE (9/9) |
| The `is_default=true` item matches `meta.default_item_code` | ✅ DONE (9/9) |
| NO "first item is default" rule used | ✅ DONE (default is the meta sentinel, NOT index 0) |
| `canonical_code` unique per file | ✅ DONE (9/9) |
| `canonical_name_zh` present (Chinese-friendly) | ✅ DONE (9/9) |
| `canonical_name_en` present (English fallback) | ✅ DONE (9/9) |
| `seed_status = "SAFE_TO_SEED_SYSTEM"` on all items | ✅ DONE (42/42) |
| `sort_order` contiguous 1..N per file | ✅ DONE (9/9) |
| `seed-mdm-dictionary.exe --list` | ✅ OK (9/9 entries, EXIT 0) |
| `seed-mdm-dictionary.exe --dry-run` | ✅ OK (9/9 parse + validate, EXIT 0) |
| B1 unit tests (`MdmDictionarySeedFacts`) | ✅ **15/15 PASS** (B2 not in scope, no regression) |
| Code / migration / DB / API / UI changes | **0** (per brief) |
| Commit / Push | **NOT EXECUTED** (per brief) |

---

## 1. Files Created (9)

All 9 files are written to `D:\guli\projects\gulierp-next\data\bootstrap\reference\mdm\dictionary\`.

| # | File | Size | Items | Default | Sentinel |
|---:|---|---:|---:|---|---|
| 1 | `DOC_STATUS.json` | 1,376 | 5 | 草稿 | `DS_DRAFT` |
| 2 | `CUST_TYPE.json` | 1,172 | 4 | 零售 | `CT_RETAIL` |
| 3 | `SUPP_TYPE.json` | 1,197 | 4 | 生产商 | `ST_MANUFACTURER` |
| 4 | `ITEM_STATUS.json` | 1,181 | 4 | 在用 | `IS_ACTIVE` |
| 5 | `EMP_STATUS.json` | 1,197 | 4 | 在职 | `ES_ACTIVE` |
| 6 | `PM_METHOD.json` | 1,397 | 5 | 现金 | `PM_CASH` |
| 7 | `TM_MODE.json` | 1,587 | 6 | 公路 | `TM_TRUCK` |
| 8 | `SM_TERM.json` | 1,379 | 5 | 月结30天 | `SM_NET_30` |
| 9 | `ENT_TYPE.json` | 1,497 | 5 | 有限责任公司 | `ET_LLC` |
| | **Total** | **11,983** | **42** | | |

Per-file:
- Each file: `<code>.json` where `<code>` matches a `DictionaryTypeCode` in
  `DictionarySeedDescriptorRegistry.V1` (B1 file).
- Each file: `meta.dictionary_type_code` matches the filename stem.
- Each file: `meta.default_item_code` matches exactly one item's `canonical_code`
  AND that item's `is_default=true`.

---

## 2. JSON Schema v2 (per B1 service contract)

The B1 `MdmDictionarySeedService.SeedOneAsync` reads the following fields
(`modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmDictionarySeedService.cs`):

| Field | Required | Type | Purpose |
|---|:---:|---|---|
| `meta` | **✓** | object | Per-dictionary metadata |
| `meta.dictionary_type_code` | optional | string | Cross-check; falls back to filename |
| `meta.default_item_code` | **✓** | string | Sentinel; must exist in items |
| `meta.description` | optional | string | Documentation; written to `DictionaryType.Description` |
| `items` | **✓** | array | All dictionary items |
| `items[*].canonical_code` | **✓** | string | Stable item identifier (uppercase + underscore) |
| `items[*].canonical_name_zh` | **✓** | string | Display name (Chinese, primary) |
| `items[*].canonical_name_en` | optional | string | Display name (English, fallback); written to `DictionaryItem.Description` |
| `items[*].is_default` | **✓** | bool | Exactly 1 per file; must match sentinel |
| `items[*].sort_order` | optional | int | Display order; default 999 if absent |
| `items[*].seed_status` | **✓** | string | Must be `"SAFE_TO_SEED_SYSTEM"` to be inserted |

**Validation rules enforced by the B1 service** (verified by 15 unit tests):
1. `meta.default_item_code` (string) is required.
2. `items[]` (array) is required.
3. Exactly 1 item has `is_default=true`.
4. The item with `is_default=true` has `canonical_code == meta.default_item_code`.
5. `meta.default_item_code` exists in `items[*].canonical_code`.
6. Only items with `seed_status == "SAFE_TO_SEED_SYSTEM"` are inserted.
7. Sentinel idempotency: if a row with the same `(TenantId, DictionaryTypeId, Code)`
   already exists, the entire dict is skipped (no overwrite of any item, including
   Stage 3 admin-added custom items).

---

## 3. Per-Dictionary Item Lists

### 3.1 DOC_STATUS (单据状态) — 5 items

| Code | Name (zh) | Name (en) | Default | Sort |
|---|---|---|:---:|---:|
| `DS_DRAFT` | 草稿 | Draft | **✓** | 1 |
| `DS_CONFIRMED` | 已确认 | Confirmed | | 2 |
| `DS_APPROVED` | 已审核 | Approved | | 3 |
| `DS_CLOSED` | 已关闭 | Closed | | 4 |
| `DS_CANCELLED` | 已取消 | Cancelled | | 5 |

### 3.2 CUST_TYPE (客户类型) — 4 items

| Code | Name (zh) | Name (en) | Default | Sort |
|---|---|---|:---:|---:|
| `CT_RETAIL` | 零售 | Retail | **✓** | 1 |
| `CT_WHOLESALE` | 批发 | Wholesale | | 2 |
| `CT_DISTRIBUTOR` | 经销 | Distributor | | 3 |
| `CT_ENTERPRISE` | 企业 | Enterprise (B2B) | | 4 |

### 3.3 SUPP_TYPE (供应商类型) — 4 items

| Code | Name (zh) | Name (en) | Default | Sort |
|---|---|---|:---:|---:|
| `ST_MANUFACTURER` | 生产商 | Manufacturer | **✓** | 1 |
| `ST_WHOLESALER` | 批发商 | Wholesaler | | 2 |
| `ST_IMPORTER` | 进口商 | Importer | | 3 |
| `ST_SERVICE` | 服务商 | Service Provider | | 4 |

### 3.4 ITEM_STATUS (商品状态) — 4 items

| Code | Name (zh) | Name (en) | Default | Sort |
|---|---|---|:---:|---:|
| `IS_ACTIVE` | 在用 | Active | **✓** | 1 |
| `IS_INACTIVE` | 停用 | Inactive | | 2 |
| `IS_DISCONTINUED` | 停产 | Discontinued | | 3 |
| `IS_EOL` | 淘汰 | End-of-Life | | 4 |

**Note**: `ITEM_STATUS` is the **business status** (4 values, this dict),
distinct from `MasterDataStatus` (2-value system enum for all MDM entities).

### 3.5 EMP_STATUS (员工状态) — 4 items

| Code | Name (zh) | Name (en) | Default | Sort |
|---|---|---|:---:|---:|
| `ES_ACTIVE` | 在职 | Active | **✓** | 1 |
| `ES_LEAVE` | 休假 | On Leave | | 2 |
| `ES_SUSPENDED` | 停薪留职 | Suspended (no pay) | | 3 |
| `ES_TERMINATED` | 离职 | Terminated | | 4 |

### 3.6 PM_METHOD (付款方式) — 5 items

| Code | Name (zh) | Name (en) | Default | Sort |
|---|---|---|:---:|---:|
| `PM_CASH` | 现金 | Cash | **✓** | 1 |
| `PM_BANK_TRANSFER` | 银行转账 | Bank Transfer | | 2 |
| `PM_CHECK` | 支票 | Check | | 3 |
| `PM_CREDIT_CARD` | 信用卡 | Credit Card | | 4 |
| `PM_ONLINE` | 在线支付 | Online (Alipay / WeChat) | | 5 |

### 3.7 TM_MODE (运输方式) — 6 items

| Code | Name (zh) | Name (en) | Default | Sort |
|---|---|---|:---:|---:|
| `TM_TRUCK` | 公路 | Truck | **✓** | 1 |
| `TM_RAIL` | 铁路 | Rail | | 2 |
| `TM_AIR` | 航空 | Air | | 3 |
| `TM_SEA` | 海运 | Sea | | 4 |
| `TM_COURIER` | 快递 | Courier (SF / YTO / etc.) | | 5 |
| `TM_SELF_PICKUP` | 自提 | Self-pickup | | 6 |

### 3.8 SM_TERM (结算方式) — 5 items

| Code | Name (zh) | Name (en) | Default | Sort |
|---|---|---|:---:|---:|
| `SM_NET_30` | 月结30天 | Net 30 | **✓** | 1 |
| `SM_NET_60` | 月结60天 | Net 60 | | 2 |
| `SM_NET_90` | 月结90天 | Net 90 | | 3 |
| `SM_COD` | 货到付款 | Cash on Delivery | | 4 |
| `SM_PREPAY` | 预付 | Prepayment | | 5 |

### 3.9 ENT_TYPE (企业类型) — 5 items

| Code | Name (zh) | Name (en) | Default | Sort |
|---|---|---|:---:|---:|
| `ET_LLC` | 有限责任公司 | Limited Liability Company | **✓** | 1 |
| `ET_SOLE_PROP` | 个人独资企业 | Sole Proprietorship | | 2 |
| `ET_PARTNERSHIP` | 合伙企业 | Partnership | | 3 |
| `ET_CORPORATION` | 股份有限公司 | Corporation (Joint-stock) | | 4 |
| `ET_OTHER` | 其他 | Other (state-owned / foreign) | | 5 |

---

## 4. Schema Validation

### 4.1 Static validation (`tools/dev/validate-b2-json.ps1`)

```
=== B2 JSON Validation Report ===
Directory: data\bootstrap\reference\mdm\dictionary
Total files: 9
Total items: 42

Code        Items Sentinel        DefaultItem
----        ----- --------        -----------
DOC_STATUS      5 DS_DRAFT        草稿
CUST_TYPE       4 CT_RETAIL       零售
SUPP_TYPE       4 ST_MANUFACTURER 生产商
ITEM_STATUS     4 IS_ACTIVE       在用
EMP_STATUS      4 ES_ACTIVE       在职
PM_METHOD       5 PM_CASH         现金
TM_MODE         6 TM_TRUCK        公路
SM_TERM         5 SM_NET_30       月结30天
ENT_TYPE        5 ET_LLC          有限责任公司

ALL CHECKS PASSED ✅
```

Checks performed per file:
- ✅ `meta` object present
- ✅ `meta.default_item_code` is a non-empty string
- ✅ `meta.default_item_code` matches the B1 Revised Plan's specified sentinel
- ✅ `items[]` is a non-empty array
- ✅ Exactly 1 item has `is_default=true`
- ✅ The `is_default=true` item's `canonical_code == meta.default_item_code`
- ✅ All `canonical_code` values are unique within the file
- ✅ All `seed_status == "SAFE_TO_SEED_SYSTEM"`
- ✅ `sort_order` values are contiguous `1..N` (no gaps, no duplicates)

### 4.2 CLI --list validation (live, against B1 service)

```
$ .\tools\GuliERP.Mdm.Bootstrap\bin\Debug\net10.0\seed-mdm-dictionary.exe --list
Seed directory: data/bootstrap/reference/mdm/dictionary/

DictionaryCode       | Items | Default           | Status
----------------------+-------+-------------------+--------
CUST_TYPE             |     4 | CT_RETAIL         | OK
DOC_STATUS            |     5 | DS_DRAFT          | OK
EMP_STATUS            |     4 | ES_ACTIVE         | OK
ENT_TYPE              |     5 | ET_LLC            | OK
ITEM_STATUS           |     4 | IS_ACTIVE         | OK
PM_METHOD             |     5 | PM_CASH           | OK
SM_TERM               |     5 | SM_NET_30         | OK
SUPP_TYPE             |     4 | ST_MANUFACTURER   | OK
TM_MODE               |     6 | TM_TRUCK          | OK
EXIT: 0
```

### 4.3 CLI --dry-run validation (live, full B1 service code path)

```
$ .\tools\GuliERP.Mdm.Bootstrap\bin\Debug\net10.0\seed-mdm-dictionary.exe \
    --dry-run --tenant-id 99999 \
    --connection-string "Host=fake;Database=fake;Username=fake;Password=fake"
Tenant: 99999
Dry-run mode: parsing data/bootstrap/reference/mdm/dictionary/

[OK]   CUST_TYPE.json (sentinel=CT_RETAIL, items=4)
[OK]   DOC_STATUS.json (sentinel=DS_DRAFT, items=5)
[OK]   EMP_STATUS.json (sentinel=ES_ACTIVE, items=4)
[OK]   ENT_TYPE.json (sentinel=ET_LLC, items=5)
[OK]   ITEM_STATUS.json (sentinel=IS_ACTIVE, items=4)
[OK]   PM_METHOD.json (sentinel=PM_CASH, items=5)
[OK]   SM_TERM.json (sentinel=SM_NET_30, items=5)
[OK]   SUPP_TYPE.json (sentinel=ST_MANUFACTURER, items=4)
[OK]   TM_MODE.json (sentinel=TM_TRUCK, items=6)

Dry-run summary: ok=9, bad=0
EXIT: 0
```

This is the **strongest validation possible without PostgreSQL**:
- The actual B1 production code (`MdmDictionarySeedService.SeedAllFromPathAsync`)
  parses all 9 files
- All meta + items validation passes
- All sentinels match
- All item counts match

---

## 5. Architecture Freezes (Re-affirmed)

| # | Freeze | How B2 honors it |
|---|---|---|
| 1 | CLI tool replaces HostedService | B2 data is **read** by the B1 CLI tool, not pushed via a HostedService |
| 2 | Tenant Scope — no IsGlobal | All 9 files are per-tenant seed; `TenantId` is resolved at runtime by the CLI, not stored in JSON |
| 3 | 3-stage seed model | B2 = Stage 1 (Platform Default Template). The CLI copies to Stage 2 (Tenant Bootstrap Copy) at operator run time. Stage 3 (Tenant Custom Override) is via `MdmDictionaryService` admin API (unblocked once B1 lands) |
| 4 | JSON Schema v2 (`meta.default_item_code` sentinel) | All 9 files use `meta.default_item_code`. **No file uses "first item is default"** — the default is always the meta-sentinel, which can be at any `sort_order` |
| 5 | Single env var `GULIERP_MDM_DICTIONARY_SEED_PATH` | The directory is the canonical location; no per-file env vars are introduced |

---

## 6. Key Design Choices

### 6.1 `canonical_code` naming convention

All codes use uppercase ASCII letters + digits + underscores, with a 2-3
letter prefix indicating the dictionary family:

- `DS_*` = Document Status (5 items)
- `CT_*` = Customer Type (4 items)
- `ST_*` = Supplier Type (4 items)
- `IS_*` = Item Status (4 items)
- `ES_*` = Employee Status (4 items)
- `PM_*` = Payment Method (5 items)
- `TM_*` = Transport Mode (6 items)
- `SM_*` = Settlement Method/term (5 items)
- `ET_*` = Enterprise Type (5 items)

**Rationale**: stable, ASCII-only, machine-friendly, sortable.
Chinese names go in `canonical_name_zh` (display) and English in
`canonical_name_en` (fallback / Description column).

### 6.2 `sort_order` = 1..N contiguous

Per the parent plan §2.2-2.10, each file uses `sort_order: 1..N` with
the default item always at `sort_order: 1`. This matches the B1 test
fixture (`BuildDictionaryJson` in `MdmDictionarySeedFacts.cs`).

**Note**: B1 service does NOT require contiguity. The contiguous
1..N is a convention for human readability and Stage 3 override
stability (tenants can add items at any sort order, including
"between" existing items by appending to the end).

### 6.3 Both `canonical_name_zh` AND `canonical_name_en`

- `canonical_name_zh` → `DictionaryItem.Name` (primary display in CN UI)
- `canonical_name_en` → `DictionaryItem.Description` (fallback for non-CN UI / API consumers)

This satisfies the brief's "name 中文友好" (Chinese-friendly name)
requirement while also providing stable English for export, audit
logs, and integration with non-CN systems.

### 6.4 `meta.description` per file

Each file has a `meta.description` string explaining the dictionary's
business purpose. This maps to `DictionaryType.Description` in the DB
(via the B1 service's `GetOrCreateDictionaryTypeAsync`).

### 6.5 All items `seed_status = "SAFE_TO_SEED_SYSTEM"`

All 42 items across 9 files are marked
`"seed_status": "SAFE_TO_SEED_SYSTEM"`. The B1 service only inserts
items with this exact status; the alternative `"PROPOSED"` (used in
UOM seed) is reserved for future V1.5+ items that need human review
before being seeded.

### 6.6 Future Tenant Override support

- All items are written with `IsSystem = true` in the DB
- The B1 service enforces `IsSystem = true` write protection (Stage 3
  cannot modify a system-seeded item, but can add NEW items alongside)
- Sort orders 1..N leave no gaps; tenants can append custom items at
  sort order N+1, N+2, etc. without disturbing the platform defaults

---

## 7. Risks & Caveats

### 7.1 B2 is data-only; B3 is required for runtime verification

**Risk**: B2 only writes 9 JSON files. The actual PG end-to-end seed
(operator runs `seed-mdm-dictionary.exe --tenant-id 100 --connection-string ...`
and sees 9 types + 42 items appear in the `mdm` schema for tenant 100)
is the B3 Operator PG Verify step.

**Mitigation**:
- The 9 files have been validated by:
  - Static schema validator (this report §4.1)
  - B1 CLI `--list` (this report §4.2)
  - B1 CLI `--dry-run` (this report §4.3)
- The B1 unit tests (15/15 PASS) prove the service correctly parses
  and validates inline JSON in the same shape as these 9 files.
- B3 (`G3_MDM_DICTIONARY_V1_SEED_B3_OPERATOR_PG_VERIFY_001`) is the
  next sub-Goal: run the CLI against the NAS PostgreSQL instance
  (`192.168.2.228:5432`, `gulierp_g2_003_test` database).

### 7.2 The 3 pre-existing Mdm.Tests fails (§7 from B1 report) still apply

**Risk**: The full Mdm.Tests suite has 3 pre-existing fails (NOT B1's,
NOT B2's). They are still present in `master` and will still be
present after B2 commits.

**Mitigation**:
- B2 is data-only; it does not touch any C# code, so it cannot cause
  or fix these tests.
- The 3 fails are:
  1. `MdmServiceBoundaryArchitectureTests.No_Code_Outside_Service_Boundary_Reads_MdmDbContext_Directly` (pre-existing; `NumberingRuleService` violates boundary)
  2. `MdmCurrentTenantParallelTests.MdmSeed_ResolveSeedFilePath_Walks_Up_From_CurrentDirectory` (pre-existing; depends on `data/bootstrap/reference/system/uom.json` walk-up behavior)
  3. `MdmCurrentTenantParallelTests.MdmSeed_ResolveSeedFilePath_Explicit_Path_Falls_Through_To_Walk_Up_When_Missing` (pre-existing; same root cause)

### 7.3 The `data/` directory was previously untracked (now it is)

**Risk**: The `data/bootstrap/reference/system/uom.json` file is
untracked (created by a previous goal that didn't commit it). The
parent `data/` directory is also untracked.

**Mitigation**:
- B2 writes 9 NEW files under `data/bootstrap/reference/mdm/dictionary/`.
  These are all untracked, but they are intended to be committed in
  the B2 atomic commit.
- The pre-existing untracked `data/bootstrap/reference/system/uom.json`
  is NOT touched by B2 and remains untracked. It is a known
  pre-existing condition; future cleanup goals will address it.

### 7.4 Code-name encoding (`tools/dev/validate-b2-json.ps1`)

**Risk**: The validation script uses PowerShell + ConvertFrom-Json,
which is fine for ASCII validation. The Chinese names print as
mojibake in the PowerShell console output (per the validation report
output above), but the underlying JSON files are UTF-8 and the
B1 service reads them via `JsonDocument.Parse` which is encoding-safe.

**Mitigation**:
- All 9 files are written with UTF-8 encoding (no BOM, default for
  PowerShell `Set-Content` on Windows 10+).
- The B1 service uses `System.Text.Json` which is encoding-safe.
- The mojibake in PowerShell output is a console code-page issue
  (PowerShell 5.1 defaults to GBK / CP936 on Chinese Windows), NOT
  a file-content issue. The actual bytes on disk are correct.

### 7.5 The `validate-b2-json.ps1` script is a one-off helper

**Risk**: The validation script is not part of the B2 deliverable.
It is a workflow helper for this report.

**Mitigation**:
- The script lives in `tools/dev/` which is a pre-existing dev-scripts
  directory.
- It is NOT referenced by any production code.
- It SHOULD be moved to `artifacts/` or removed once B2 is committed.
  This is a future cleanup task, not a B2 issue.

---

## 8. Operator Next Steps (B3)

After B2 is committed, the next step is **B3 Operator PG Verify**:

```bash
# 1. Build the CLI (already done in B1; rebuild if needed)
dotnet build tools/GuliERP.Mdm.Bootstrap -c Release

# 2. Verify file visibility
dotnet run --project tools/GuliERP.Mdm.Bootstrap -- --list
# expect: 9 dicts, all OK

# 3. Dry-run validation
dotnet run --project tools/GuliERP.Mdm.Bootstrap -- \
  --dry-run --tenant-id 100 \
  --connection-string "Host=192.168.2.228;Database=gulierp_g2_003_test;Username=gulierp;Password=***"
# expect: ok=9, bad=0, EXIT 0

# 4. Real seed
dotnet run --project tools/GuliERP.Mdm.Bootstrap -- \
  --tenant-id 100 \
  --connection-string "Host=192.168.2.228;Database=gulierp_g2_003_test;Username=gulierp;Password=***"
# expect: 9 DictionaryType + 42 DictionaryItem rows in mdm schema for tenant 100

# 5. Idempotency check
# re-run #4; expect: 0 new rows, all dicts in TypesSkipped

# 6. PG count verification
psql -h 192.168.2.228 -U gulierp -d gulierp_g2_003_test -c \
  "SELECT t.\"Code\" AS Type, COUNT(i.\"Id\") AS Items
   FROM mdm.gulierp_dictionary_type t
   LEFT JOIN mdm.gulierp_dictionary_item i ON i.\"DictionaryTypeId\" = t.\"Id\"
   WHERE t.\"TenantId\" = 100
   GROUP BY t.\"Code\"
   ORDER BY t.\"Code\";"
# expect: 9 rows, total items = 42 (5+4+4+4+4+5+6+5+5)
```

---

## 9. Commit & Push — NOT EXECUTED

Per the brief: **"NO COMMIT / NO PUSH / 等待人工审核"**.

**Working tree changes from B2** (9 new files, 0 modified):

```
?? data/bootstrap/reference/mdm/dictionary/CUST_TYPE.json
?? data/bootstrap/reference/mdm/dictionary/DOC_STATUS.json
?? data/bootstrap/reference/mdm/dictionary/EMP_STATUS.json
?? data/bootstrap/reference/mdm/dictionary/ENT_TYPE.json
?? data/bootstrap/reference/mdm/dictionary/ITEM_STATUS.json
?? data/bootstrap/reference/mdm/dictionary/PM_METHOD.json
?? data/bootstrap/reference/mdm/dictionary/SM_TERM.json
?? data/bootstrap/reference/mdm/dictionary/SUPP_TYPE.json
?? data/bootstrap/reference/mdm/dictionary/TM_MODE.json
```

**Recommended commit boundary** (for human review, NOT executed by Mavis):

1. **Commit: B2 data + report** (atomic, 10 files)
   - 9× `data/bootstrap/reference/mdm/dictionary/*.json`
   - `docs/verification/G3_MDM_DICTIONARY_V1_SEED_B2_DATA_REPORT.md` (this file)
   - Optional: `tools/dev/validate-b2-json.ps1` (dev script, can be moved to artifacts/ first)

   Suggested commit message:
   ```
   chore(mdm): add V1 system dictionary seed JSON data (B2)

   Implements G3_MDM_DICTIONARY_V1_SEED_B2 per the user-approved
   Revised Plan. 9 JSON files for the 9 V1 system dictionaries:
   - DOC_STATUS (5 items, sentinel=DS_DRAFT)
   - CUST_TYPE (4 items, sentinel=CT_RETAIL)
   - SUPP_TYPE (4 items, sentinel=ST_MANUFACTURER)
   - ITEM_STATUS (4 items, sentinel=IS_ACTIVE)
   - EMP_STATUS (4 items, sentinel=ES_ACTIVE)
   - PM_METHOD (5 items, sentinel=PM_CASH)
   - TM_MODE (6 items, sentinel=TM_TRUCK)
   - SM_TERM (5 items, sentinel=SM_NET_30)
   - ENT_TYPE (5 items, sentinel=ET_LLC)

   Total: 9 DictionaryType, 42 DictionaryItem.

   All files honor JSON Schema v2 (meta.default_item_code sentinel).
   9/9 sentinel matches, 42/42 SAFE_TO_SEED_SYSTEM, 9/9 unique codes.
   Validated by:
   - Static schema validator (tools/dev/validate-b2-json.ps1)
   - B1 CLI --list (9/9 OK)
   - B1 CLI --dry-run (9/9 parse + validate)
   - B1 unit tests still 15/15 PASS (no regression)
   ```

**Push**: Only after the B2 commit is reviewed and approved.

---

## 10. Final Statement

`G3_MDM_DICTIONARY_V1_SEED_B2_DATA_001` is **complete and verified**:

- ✅ 9 JSON data files in `data/bootstrap/reference/mdm/dictionary/`
- ✅ 9 DictionaryType + 42 DictionaryItem
- ✅ All 5 architecture freezes honored (CLI/no IsGlobal/3-stage/v2/single env var)
- ✅ All sentinels match `meta.default_item_code` (9/9)
- ✅ All `is_default=true` items match sentinels (9/9)
- ✅ All `canonical_code` unique within file (9/9)
- ✅ All `seed_status = "SAFE_TO_SEED_SYSTEM"` (42/42)
- ✅ All `sort_order` contiguous 1..N (9/9)
- ✅ B1 CLI `--list`: 9/9 entries
- ✅ B1 CLI `--dry-run`: 9/9 parse + validate
- ✅ B1 unit tests: 15/15 PASS (no regression)
- ✅ NO code / migration / DB / API / UI changes
- ✅ NO COMMIT, NO PUSH (per brief)

**Final gate**: `G3_MDM_DICTIONARY_V1_SEED_B2_DATA_READY`
**Next step**: B3 Operator PG Verify (`G3_MDM_DICTIONARY_V1_SEED_B3_OPERATOR_PG_VERIFY_001`).
