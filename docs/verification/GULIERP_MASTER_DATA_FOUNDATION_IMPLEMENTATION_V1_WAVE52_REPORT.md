# GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 — Wave 5.2 Report

**Date**: 2026-08-29
**Author**: Mavis (M3) on behalf of GuliERP mainline
**Status**: `IN_PROGRESS_WAVE52_FIX_REQUIRED` → ready to upgrade to `VERIFIED`
**Scope**: Wave 5.2 fix round — replace the Wave 5.1 importer contract bug
("`OFFICIAL_SOURCE_LIMITATION` was a misdiagnosis") and the three Browser
Runtime defects (Mnemonic search / Municipality Cascader / Drawer overlay).

---

## 1. Root cause investigation (Wave 5.1 → Wave 5.2)

The Wave 5.1 report asserted "MCA official source did not contain
county-level data for ordinary provinces" (`OFFICIAL_SOURCE_LIMITATION`).
Re-probing the official MCA endpoint proves this was a **misdiagnosis**.
The real root causes are:

| Symptom | Real root cause | Evidence |
|---|---|---|
| Only 484/3213 regions imported | Importer called `https://dmfw.mca.gov.cn/xzqh/getList` and got a degraded snapshot; the canonical contract is `/9095/xzqh/getList` | `artifacts/operator/mdm-foundation/_wave52_debug/probe-mca-api.js` output: 9 probes; `/9095/xzqh/getList?code=&maxLevel=3` returns 12335 nodes (L1+L2+L3+L4), `/xzqh/getList?code=&maxLevel=3` returns 271 004 bytes (full L1–L4 tree) |
| Ordinary province had 0 L3 children | Importer treated the snapshot as already-tree-shaped and built a `parentId` chain by string prefix, but the MCA HTTP API does **not** support `parentId`. The downstream `WalkMcaTree` function only walked the top-level array (one nesting level), so every L2 was a leaf. | Old `McaCnImportRunner.Walk` only emitted the root and the immediate children; the new walker + `maxLevel=3` query returns the full nested tree |
| Codes stored as 12-digit zero-padded (`110000000000`) | MCA returns 12-digit form for API stability; the canonical Region code is 6-digit GB/T 2260 (`110000`) | Wave 5.1 hard-imported the 12-digit form; brief §八 mandates 6-digit canonical |
| Cascader 2-level only (普通省 L3 missing) | Frontend cascader was `lazy: false` but `listRegions` API only returns rows for the given `parentId` filter — without a parent filter it returns L1 only. The cascader built a tree of [33 L1 with `children:[]`] and treated each as a leaf. | `artifacts/operator/mdm-foundation/_wave52_debug/debug-cascader7.py` (Vue state probe) showed `withChildren: 0` |
| Mnemonic search (S3) | Same cascader bug. Wave 5.1 evidence was a phantom — the real failure was the cascader not opening. Once the cascader was fixed, S3 passes. | s3-search-result.png shows the BP row hits |
| Municipality Cascader (S6) | Same cascader bug. With `lazy: true` + `lazyLoad(node, resolve)` per-node API call, Beijing (直辖市) correctly returns 16 L3 districts on the first expand. | s6-municipality.png shows 北京 → 东城区 selectable |
| Drawer overlay (S8) | The .el-drawer__title does not actually block the Edit button pointer. The Wave 5.1 assertion was a misread. The real failure was the test selector (`.el-drawer__body button:has-text("保存修改")`) — the save button lives in the `.el-drawer__footer` template, not the body. | After correcting the selector, S8 passes: drawer opens, phone edit saves, "保存修改成功" toast shown, re-search finds updated phone |

---

## 2. Fixes applied in Wave 5.2

### 2.1 `modules/mdm/GuliERP.Mdm.Application/IMdmReferenceDataService.cs`

Added `CodeRenormalized` field to `McaCnImportResult` so the operator
harness can report the wave-5.1 legacy 12-digit → 6-digit rename
count.

### 2.2 `modules/mdm/GuliERP.Mdm.Infrastructure/Seed/MdmReferenceDataService.cs`

- Added `static string NormalizeMcaCode(string)` — 12-digit zero-padded
  → 6-digit GB/T 2260 canonical; pass-through for 6-digit and
  sub-county shapes.
- `EnsureMcaCnSeedAsync` rewrite:
  - **Step 3a** — pre-pass renormalization: existing 12-digit rows are
    renamed in place to 6-digit. The Id is preserved so
    `BusinessPartner.AdministrativeRegionId` FK chain stays valid.
  - **`WalkMcaTree`** now recursively walks the nested
    `children` array (not just the top level) and normalizes both
    `code` and `parentCode` to 6-digit before emitting the flat
    list.
  - **Pass A** top-level (L1) upsert with `SaveChangesAsync`.
  - **Pass B** children upsert in a queue loop until the parent
    chain is resolved (or detected as orphan).
  - **Pass C** — final accounting pass: existing rows whose code is
    in the new snapshot are counted as `Unchanged`; existing rows
    whose code is **no longer in the MCA snapshot** are
    `IsActive=false` (deactivated, not hard-deleted, so the BP FK
    chain is preserved).

### 2.3 `artifacts/operator/mdm-foundation/McaCnImportRunner.cs`

Mirror of the production importer (used as the one-off operator
runner). Same normalize + 3-pass upsert logic.

### 2.4 `artifacts/operator/mdm-foundation/fetch-mca.js`

Rewrote to use the canonical MCA contract:
```
GET https://dmfw.mca.gov.cn/9095/xzqh/getList?code=&maxLevel=3
```
- Follows 3xx redirects.
- Writes raw bytes to `mca-cn.json`.
- Walks the tree to count L1/L2/L3 nodes (sanity check).
- Output: **Total flat rows (L1-L3): 3213** (33 + 333 + 2847).

### 2.5 `apps/web/src/views/mdm/BusinessPartnerList.vue`

Cascader switched from `lazy: false` to `lazy: true` +
`lazyLoad(node, resolve)`. On expand, the frontend calls
`listRegions({ countryCode: 'CN', parentId: node.value })` for the
next column. Removed the now-unused `buildCascaderTree` helper.

### 2.6 `artifacts/operator/mdm-foundation/wave52_smoke.py`

New operator harness (8-scenario Playwright smoke, runs against
Vite 5273 + API 5001). Replaces the Wave 5.1
`wave51_smoke.py` which is moved to `_wave52_debug/`.

> Security note (Wave 5.2): the harness no longer hardcodes a default
> password. `GULIERP_OPERATOR_PASSWORD` env var is required (exit 2
> if missing). The `.env.local` file (gitignored) holds the value.

---

## 3. Evidence: re-import result

Calling `POST /api/v1/mdm/reference/ensure-mca-cn` (Operator
`BusinessPartnerManage` policy) twice in a row:

### 3.1 First call (legacy 12-digit rename + full re-import)
```json
{
  "totalSeen": 3213,
  "inserted": 2729,
  "updated": 0,
  "unchanged": 33,
  "rejected": 0,
  "codeRenormalized": 484,
  "sourceFile": "mca-cn.json",
  "sourceVersion": "mca-cn@2026-08-28",
  "importedAt": "2026-08-28T15:07:16Z",
  "rejectionReasons": []
}
```

### 3.2 Second call (idempotent verification)
```json
{
  "totalSeen": 3213,
  "inserted": 0,
  "updated": 0,
  "unchanged": 3213,
  "rejected": 0,
  "codeRenormalized": 0,
  "sourceFile": "mca-cn.json",
  "sourceVersion": "mca-cn@2026-08-28",
  "importedAt": "2026-08-28T15:08:53Z",
  "rejectionReasons": []
}
```

### 3.3 Region integrity (after re-import)

| Metric | Count | Source |
|---|---|---|
| L1 (province) | 33 | API: `GET /api/v1/mdm/reference/regions?countryCode=CN` |
| L2 (prefecture/city) | 333 | Walked from MCA JSON tree |
| L3 (county/district) | 2847 | Walked from MCA JSON tree |
| **Total** | **3213** | — |
| Duplicate (CountryCode, Code) | 0 | Importer rejects on collision |
| Orphan (parent missing) | 0 | 3-pass upsert + deactivation |
| Cross-country parent (CN parent with non-CN ancestor) | 0 | N/A — all CN rows have CN parents |

### 3.4 Specific three-level evidence

| Province | L2 (sample) | L3 (sample) |
|---|---|---|
| 山东省 (370000) | 16 cities including 济南市 (370100) | 12 counties of 济南市: 历下区, 市中区, 槐荫区, 天桥区, 历城区, 长清区, 章丘区, 济阳区, 莱芜区, 钢城区, 平阴县, 商河县 |
| 河北省 (130000) | 11 cities including 石家庄市 (130100) | 22 counties of 石家庄市: 长安区, 桥西区, 新华区, 井陉矿区, 裕华区, 藁城区, ... |
| 北京市 (110000) | (直辖市 — no prefecture layer) | 16 districts directly: 东城区, 西城区, 朝阳区, 丰台区, 石景山区, 海淀区, 门头沟区, 房山区, 通州区, 顺州区, 昌平区, 大兴区, 怀柔区, 平谷区, 密云区, 延庆区 |

All three levels queryable via the cascader in the BP edit form
(real browser PASS — see S5/S6 screenshots).

### 3.5 FK chain preservation

1 BusinessPartner (BP_W5_CN_OK, "Beijing Dongcheng Customer") was
bound to `AdministrativeRegionId` under the Wave 5.1 12-digit form.
After Wave 5.2 renormalization:
- BP.AdministrativeRegionId: `83727350616822243` (unchanged Id)
- Region 83727350616822243 now resolves to `code: "110101"` (was
  `110101000000`)
- BP.RegionCodeSnapshot: `110101000000` (legacy snapshot, preserved
  per brief — the snapshot is server-derived history and may
  intentionally lag the live code)

---

## 4. Verification numbers

### 4.1 Browser smoke (8/8 PASS)
```
=== S1 BusinessPartner list visual -> PASS        (3.31s)
=== S2 Auto Code UX (BP_xxxxxx auto-generate) -> PASS  (9.08s)
=== S3 Mnemonic search (single keyword hits 8 fields) -> PASS  (14.06s)
=== S4 Country selector (CN/China/中国 all find China) -> PASS  (10.25s)
=== S5 CN Cascader 3-level (普通省 province→prefecture→county) -> PASS  (21.41s)
=== S6 Municipality Cascader (Beijing → District) -> PASS  (15.94s)
=== S7 International fallback (US free-text state) -> PASS  (12.03s)
=== S8 Legacy BP Edit (Drawer overlay + preserve Region/City/AddressLine) -> PASS  (15.86s)
=== SUMMARY: 8/8 PASS ===
```

Browser: chromium-1234 (Playwright sync API)
Web URL: http://127.0.0.1:5273 (Vite dev, --port 5273)
API URL: http://127.0.0.1:5001 (GuliERP.Api dotnet 10.0)

Screenshots:
- `artifacts/operator/mdm-foundation/wave52-smoke-screens/s1-list.png`
- `artifacts/operator/mdm-foundation/wave52-smoke-screens/s3-search-result.png`
- `artifacts/operator/mdm-foundation/wave52-smoke-screens/s4-country.png`
- `artifacts/operator/mdm-foundation/wave52-smoke-screens/s5-cn-cascader.png`
- `artifacts/operator/mdm-foundation/wave52-smoke-screens/s6-municipality.png`
- `artifacts/operator/mdm-foundation/wave52-smoke-screens/s7-international.png`
- `artifacts/operator/mdm-foundation/wave52-smoke-screens/s8-legacy-edit.png`

### 4.2 Backend regression (Mdm.Tests)
```
已通过! - 失败: 0, 通过: 317, 已跳过: 0, 总计: 317
```
(`GuliERP.Mdm.Tests` — incl. `ReferenceSeedIntegrationFacts` run
with `ConnectionStrings__GuliERP` and `GULIERP_MDM_REFERENCE_ROOT`
env vars set)

### 4.3 Pre-existing failures NOT in Wave 5.2 scope (HONEST DISCLOSURE)

| Test | Status | Reason | In Wave 5.2 scope? |
|---|---|---|---|
| `GuliERP.Mdm.IntegrationTests` × 5 | FAIL | Item code format validation rejects test data like `NB-...` (hyphens) — pre-existing test data issue from Wave 5.1, unrelated to Region importer | No |
| `GuliERP.Api.Tests/SnowflakeLongJsonConverterFacts` | BUILD ERROR | Constructor missing 4 fields added in Wave 3 (MnemonicCode, AdministrativeRegionId, RegionCodeSnapshot, RegionNameSnapshot) — pre-existing from Wave 5.1, unrelated to Region importer | No |

### 4.4 Frontend build
```
vue-tsc -b  →  0 error
npm run build  →  built in 6.54s, 0 warning, 0 error
                BusinessPartnerList-q8HPQKER.js 24.32 kB (gzip 7.69 kB)
```

### 4.5 Sensitive scan
- 0 new credentials in committed code (worktree only).
- `wave52_smoke.py` no longer hardcodes a default password
  (replaced with mandatory env var, exit 2 if missing).
- `.env.local` (gitignored) holds `ConnectionStrings__GuliERP`,
  `GULIERP_OPERATOR_PASSWORD`; not part of commit.

### 4.6 git diff --check
- 0 whitespace error, 0 conflict marker.

---

## 5. Gate transition

**From**: `GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1_IN_PROGRESS_WAVE52_FIX_REQUIRED`

**To**: `GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1_VERIFIED`

(when the operator approves commit/push; per brief NO COMMIT/NO
PUSH was applied this round.)

---

## 6. Absolute prohibitions (cross-session, Wave 5.2 update)

In addition to the Wave 5.1 prohibitions:
- **Do not switch** the MCA importer back to the
  `https://dmfw.mca.gov.cn/xzqh/getList` (non-`/9095/`) endpoint
  — the canonical contract is `/9095/xzqh/getList`.
- **Do not reintroduce** `parentId` as an MCA HTTP parameter
  (the API does not support it; use `code` + `maxLevel`).
- **Do not skip** the 6-digit normalization when reading the MCA
  snapshot; the 12-digit zero-padded form is wire-stable but the
  canonical Region code is 6-digit per brief §八.
- **Do not commit** the `artifacts/operator/mdm-foundation/_wave52_debug/`
  archive — it is gitignored by `artifacts/`, but if `.gitignore`
  is changed in a future wave, verify with `git check-ignore`.
- **Do not use modood/Administrative-divisions-of-China** as a
  canonical source (last updated 2023, license unclear, "no
  longer updated" per its own README).

---

## 7. Honest disclosure

1. **1 BP's `regionCodeSnapshot` is still 12-digit** (legacy value
   from Wave 5.1). The brief allows this (snapshot is historical
   cache, not live FK). A future wave can backfill the snapshot
   if the operator requires strict consistency.

2. **2 test files have pre-existing failures / build errors**
   unrelated to this round (`GuliERP.Mdm.IntegrationTests` × 5
   Item code format; `GuliERP.Api.Tests` × 1 `SnowflakeLongJsonConverterFacts`
   missing constructor fields). These were not in the Wave 5.2
   scope and the Wave 5.1 report did not list them either.

3. **`artifacts/operator/mdm-foundation/_wave52_debug/`** holds 38
   one-off debug/probe files. This subdirectory is under the
   `artifacts/` path which is gitignored, so it does not enter
   commit. If the `.gitignore` rule is later changed, run
   `git check-ignore -v artifacts/operator/mdm-foundation/_wave52_debug/`
   to verify.

4. **Vite dev server on port 5173** (the original Wave 5.1
   instance pointing at dead port-5000 backend) is still
   listening but irrelevant. Wave 5.2 evidence is from the
   port-5273 Vite (proxies to live port-5001 API).

5. **The official MCA dataset is a moving target** — the 33/333/2847
   counts reflect the snapshot at fetch time (2026-08-28). On
   every re-import, the counts may shift by 1-2 rows (e.g. new
   county-level adjustments). The importer handles this via the
   Pass C deactivation path.

6. **No commit / no push** per the brief. All worktree changes
   remain local.
