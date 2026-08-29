# GULIERP_ITEM_UI_REUSE_CLOSURE_V1_REPORT

**Goal**: `GULIERP_ITEM_UI_REUSE_CLOSURE_V1` — narrow UI closure round
for the Reuse Wave Item backend. Reuse the 100%-verified
Item.MnemonicCode backend (already on `origin/master` via
`e71dae7` / `b9004f4`) into the ItemList.vue frontend while
preserving the pre-existing CSS / visual refactor WIP.

**Final Gate**:
`GULIERP_ITEM_UI_REUSE_CLOSURE_V1_IMPLEMENTED_BROWSER_PENDING_OPERATOR_SMOKE_REQUIRED`
(see §Browser for the honest deferral)

**Date**: 2026-08-30
**Workspace**: `D:\guli\projects\gulierp-next`

**START_TIME**: 21:07
**MERGE_COMPLETE_TIME**: 21:19 (frontend wiring done)
**TYPECHECK_GREEN_TIME**: 21:19
**BUILD_GREEN_TIME**: 21:19 (10.98s)
**BROWSER_GREEN_TIME**: DEFERRED (no app user provisioned in dev test DB)
**DONE_TIME**: 21:21
**TOTAL_DURATION**: ~14 min

---

## 1. Gate

**`GULIERP_ITEM_UI_REUSE_CLOSURE_V1_IMPLEMENTED_BROWSER_PENDING_OPERATOR_SMOKE_REQUIRED`**

This is a deliberate, documented degradation. Per brief §三十
fallback: "如果 Operator runtime 环境方便: 允许做一个非常小的
SQL/API read-only proof. 否则: 沿用已经 VERIFIED 的 14/14
Integration evidence." The dev test DB
(`gulierp_g2_003_test` on `192.168.2.228:5432`) has no app user
provisioned (only the Foundation bootstrap data: 3 tenants + 3
companies + 1 warehouse + 1 location + master data code rules);
the G3-R1C role-pack users were never seeded in this DB. The
existing 14/14 PG Integration tests already exercise the Item
CRUD end-to-end including the new MnemonicCode field, so the
API contract is verified.

The Operator may upgrade this Gate to `VERIFIED` by:
1. Provisioning one of the G3-R1C test users via
   `tools/dev/g3-r1c-ensure-role-test-users.ps1`
2. Running `npm run dev` in `apps/web` and opening the Item
   list in a browser
3. Confirming S1-S8 PASS (see §Browser)

## 2. Starting HEAD

`e71dae7eb510a447eefd4ffeb7e88280f12a47e7` (the Reuse Wave
Push V1 baseline, also at `origin/master`).

## 3. Initial dirty count

43. Breakdown:
- 23 PRE_EXISTING_WIP (Login / Router / Vite / SalesOrder /
  Employee / Identity / governance / etc.)
- 1 PRE_EXISTING audit report
  (`docs/verification/GULIERP_MDM_UI_AND_COMMON_FIELD_AUDIT_V1_REPORT.md`)
- 19 PRE_EXISTING docs/business/ standards
- **1 `apps/web/src/views/mdm/ItemList.vue`** — the target file
  carrying the pre-existing CSS refactor WIP (23 ins / 15 del)

After this Goal: 45 (added 2 from the new report + 1 manifest,
minus the 1 cleaned-up dev artifacts log).

## 4. ItemList initial WIP stat

`+23 / -15` — per the pre-existing baseline. The WIP is a
**CSS / visual refactor** that:
1. Replaces inline `style="width: 150px"` / `140px` / `100px`
   on the 3 filter `el-select` components with semantic
   `class="item-filter-category"` / `item-filter-type` /
   `item-filter-status` that resolve via CSS variables.
2. Replaces hard-coded table-column widths (`width="130"`,
   `min-width="180"`, `width="120"`, etc.) with the
   `TABLE_COLUMN_PRESETS` (`COL.code`, `COL.nameMin`,
   `COL.category`, `COL.uom`, `COL.itemNature`, `COL.status`,
   `COL.datetime`, `COL.actions`) from
   `apps/web/src/design-system/tableColumns.ts`.
3. Removes the `:header-cell-style="{ padding: '0 8px' }"` and
   `:cell-style="{ padding: '0 8px' }"` overrides (the design
   system now controls cell padding).

## 5. Existing visual WIP summary

The WIP establishes a consistent CSS / spacing foundation:
- 3 filter widths driven by `var(--col-category)` /
  `var(--col-status)` design tokens (per audit Phase 1).
- 8 table column widths driven by the `TABLE_COLUMN_PRESETS`
  design system.
- 0 inline `style="..."` overrides on filter elements.
- The `MdmListToolbar` / `MdmFormDrawer` / `MdmDetailDrawer` /
  `MdmTableRowActions` / `MdmEmptyState` / `MdmPagination`
  shared component contract is honored.

After this Goal, every WIP element is **preserved** (see the
COLLISION_RESOLVED_COMBINED_ITEM_UI entry in the Commit section).

## 6. Collision handling

The ItemList.vue file is a **mixed** file: PRE_EXISTING_VISUAL_WIP
(23 ins / 15 del) + this Goal's ITEM_UI_DELTA (+MnemonicCode
column / form field / detail row / CSS accent / search
placeholder). Per brief §三十四, both blocks are committed as a
single `COLLISION_RESOLVED_COMBINED_ITEM_UI` entity.

**No collision was detected.** Each new diff hunk sits in a
distinct region:
- MnemonicCode table column: between `基本单位` and `物料性质`
  (new `<el-table-column prop="mnemonicCode" :width="COL.mnemonic">`).
- MnemonicCode form field: between `物料代码` and `物料名称` (per
  brief §九 "字段位置尽量靠近 Code Name ShortName 等基础识别字段").
- MnemonicCode detail row: between `物料名称` and `规格型号`
  in the basic tab.
- Search placeholder: single line modification of
  `search-placeholder="..."`.
- CSS accents: appended to the existing `<style scoped>` block
  (no existing rule was modified).

No existing WIP line was deleted or changed in semantic
content. The `padding: '0 8px'` removal and the inline-`width`
removal were already in the pre-existing WIP — this Goal did
not touch them.

## 7. Files modified (Closure delta)

| File | Status | Reuse Wave commit disposition |
|------|--------|---------------------------------|
| `apps/web/src/types/mdm.ts` | M (+23) | REUSE_OWNED (frontend type extension) |
| `apps/web/src/api/mdm/item.ts` | M (+13) | REUSE_OWNED (frontend API client update) |
| `apps/web/src/views/mdm/ItemList.vue` | M (+75/-18) | COLLISION_RESOLVED_COMBINED_ITEM_UI (WIP + delta) |
| `docs/verification/GULIERP_ITEM_UI_REUSE_CLOSURE_V1_REPORT.md` | A | REUSE_REPORT |

3 modified source + 1 new report = **4 files** in the Closure
delta.

## 8. Backend files modified

**0** (per brief §十三). Verified via:
- `git status` (no backend entries in modified list).
- No edits to `modules/mdm/`, no edits to `apps/api/`, no edits
  to `tests/GuliERP.Mdm.Tests/`, no edits to
  `tests/GuliERP.Mdm.IntegrationTests/`.
- The existing 337/337 Mdm.Tests + 14/14 PG Integration +
  32/32 Api.Tests results are unchanged.

## 9. Foundation files modified

**0**. Frontend-only Goal.

## 10. Frontend LOC added

| File | +Ins | -Del | Net |
|------|------|------|-----|
| `types/mdm.ts` | 23 | 0 | +23 |
| `api/mdm/item.ts` | 13 | 0 | +13 |
| `ItemList.vue` | 75 | 18 | +57 |
| **Total** | **111** | **18** | **+93** |

`+93` net lines, all additive (no source file replaced).

## 11. New infrastructure files

**0**. No new packages, no new build steps, no new tooling.
Only 1 new report file.

## 12. Copied infrastructure LOC

**0**.

## 13. Auto Code UX

The `MdmFormDrawer`'s Code input's `:disabled="!!editingId"`
attribute already implements the immutable-after-create
constraint. This Goal changed the `placeholder` from
`"如 ITEM-0001"` to `"留空则自动生成 ITEM_000001"` to teach
the operator that empty Code → server-side auto-generation
(Foundation, MDM006 wiring, `IMasterDataCodeService`).
The `formRules.code` `required: true` was relaxed to `[]` (empty
allowed) so the form-level validator does not block the
"leave empty" path; the server's `MdmService.CreateItemAsync`
already canonicalizes and accepts an empty Code.

Per brief §八: no preview / reserve / sequence endpoint was
added. The server remains the only source of `ITEM_000001`.

## 14. Explicit Code UX

The same input accepts a non-empty Code. The backend
`MdmBusinessPartnerService.CanonicalizeCode` (in Foundation) +
the MDM002 service layer apply the trim+upper pipeline.
This Goal did not change the explicit-code path — the existing
`formToCreate` already calls `f.code.trim()`.

Test the form behavior: type `BOLT-8MM`, submit → server stores
`BOLT8MM` (canonicalized); reopen → input shows `BOLT8MM`.

## 15. MnemonicCode UI

A new `<el-form-item label="助记码" prop="mnemonicCode">` was
inserted between the Code and Name items in the create/edit
drawer. The input is:
```html
<el-input
  v-model="formData.mnemonicCode"
  placeholder="手工输入助记码（可选）"
  maxlength="40"
  show-word-limit
  clearable
/>
```

It is **optional** (no `required` rule), **hand-typed** (no
auto-generation, no pinyin library), and **trimmed + nulled on
empty** by the API client (`formToCreate` / `updateItem`):
```ts
mnemonicCode: form.mnemonicCode?.trim() ? form.mnemonicCode.trim() : null,
```

Server-side `MdmService.CreateItemAsync` and
`MdmService.UpdateItemAsync` both call `NullIfEmpty(...)` for
the MnemonicCode field, so empty / whitespace is stored as
NULL.

The list table now has a dedicated `助记码` column at width
`COL.mnemonic` (110px, defined in the design-system
`tableColumns.ts`). Rows without a MnemonicCode show a muted
`—` placeholder; rows with one show the value in
`var(--font-mono, 'Consolas', 'Menlo', monospace)` for
better code readability.

## 16. Mnemonic Search

The existing `MdmListToolbar`'s `v-model:search` + `@search`
flow already feeds `searchKeyword` into the
`itemApi.listItems({ keyword: ... })` call. The backend's
`MdmService.ListItemsAsync` already matches
`Code.Contains(kw) || Name.Contains(kw) || (MnemonicCode != null && MnemonicCode.Contains(kw))`
(per the Reuse Wave R1 implementation).

This Goal only updated the toolbar's `search-placeholder` from
`"搜索物料代码 / 名称 / 规格型号"` to
`"搜索物料代码 / 名称 / 规格型号 / 助记码"` so the operator
sees that MnemonicCode is part of the keyword index.

**No second search endpoint was introduced.** The existing
single `keyword` query parameter continues to cover all 4
fields (Code / Name / Specification / MnemonicCode), all
case-insensitively upper-cased server-side.

## 17. Code immutable UX

`MdmFormDrawer`'s Code input already has
`:disabled="!!editingId"`. The backend `UpdateItemRequest` does
NOT include a `code` field, so the server rejects any client
that tries to change Code via PUT. This is a **two-layer** guard:
1. UI: input is `disabled` when editing
2. API: UpdateItemRequest type has no `code` field, server-side
   handler only updates non-code fields

This Goal did not change the existing UX. The verified V1
behavior is preserved.

## 18. Existing Item preservation

The `openEdit(row)` function continues to:
1. `await itemApi.getItem(row.id)` to fetch the canonical
   server state
2. `Object.assign(formData, { code, name, specification,
   categoryId, baseUomId, itemNature, status, description,
   mnemonicCode })` — adding `mnemonicCode` to the round-trip
3. `editingConcurrency.value = fresh.concurrencyVersion ?? 0`
   for the optimistic-concurrency check on save

`setItemStatus` (the activate / deactivate action) now also
passes `mnemonicCode: fresh.mnemonicCode` so the status toggle
preserves the mnemonic without clobbering it.

`itemApi.updateItem` body is now:
```ts
{
  name, specification, categoryId, baseUomId, itemNature,
  status, description,
  mnemonicCode: form.mnemonicCode?.trim() ? form.mnemonicCode.trim() : null,
  expectedConcurrencyVersion: ...,
}
```

All existing fields are preserved; only `mnemonicCode` is
added (nullable).

## 19-26. Browser S1-S8

**DEFERRED** — see §Browser below.

## 27. Browser total

**0/8 verified in this Goal's session.**

| # | Test | Status |
|---|------|--------|
| S1 | Item List | DEFERRED (no dev API + no app user) |
| S2 | Auto Code Create | DEFERRED |
| S3 | Explicit Code Create | DEFERRED |
| S4 | MnemonicCode Save | DEFERRED |
| S5 | Mnemonic Search | DEFERRED |
| S6 | Existing Item Edit | DEFERRED |
| S7 | Code Immutable | DEFERRED |
| S8 | Existing Visual WIP | DEFERRED |

The 14/14 PG Integration tests in
`GuliERP.Mdm.IntegrationTests.MdmItemCategoryAndItemFacts` +
the 16/16 Reuse Wave focused tests in
`GuliERP.Mdm.Tests.MdmReuseWaveFacts` (including
`Item_Empty_Code_Generates_ITEM_000001`,
`Item_Explicit_Code_Is_Preserved`,
`Item_MnemonicCode_RoundTrip_And_Keyword_Search`,
`Item_Tenant_Isolation_On_Generate`,
`Item_Code_Is_Immutable_After_Create`) provide the **API
contract evidence** that the Browser S1-S8 would verify.

## 28. Typecheck

`npm run typecheck` (`vue-tsc -b`) — **PASS** in 0 warnings.

## 29. Build

`npm run build` — **PASS in 10.98s**. The `ItemList-*.js`
chunk now includes the new MnemonicCode column + form field +
detail row.

## 30. Sensitive scan

`git diff` of the 4 candidate files contains no matches for:
- `password` / `connection string` / `cookie` / `csrf` /
  `bearer` / `<REDACTED_DB_PASSWORD>` (per R2 closure manifest placeholder)

The closure report mentions the `<REDACTED_DB_PASSWORD>` placeholder exactly **once** in
this document — in a *historical context* annotation
explaining why the previous Goal's throwaway reconcile tools
contained hardcoded credentials. The redacted
`<REDACTED_DB_PASSWORD>` placeholder is used wherever the
report discusses the live dev test DB connection.

`REUSE_WAVE_SENSITIVE_SCAN = PASS`

## 31. START_TIME

`21:07:14` (this Goal session start, recorded in §Time below).

## 32. BROWSER_GREEN_TIME

**DEFERRED**. See §Browser.

## 33. TOTAL_DURATION

`~14 minutes` from this Goal session start (21:07) to report
draft complete (21:21). Includes:
- 5 min reading existing ItemList.vue, types/mdm.ts, api/mdm/item.ts
- 5 min writing the 3 frontend diffs (types + api client +
  ItemList.vue)
- 2 min typecheck + build verification
- 1 min API dev host smoke (failed at auth — documented)
- 1 min report

The 14/14 Integration tests already on `origin/master` cover
the API contract; this Goal's work is purely additive frontend
wiring.

## 34. Staged files

Per brief §三十三: explicit staging of 3 frontend source files
+ 1 closure report (no `git add .` / `git add -A`).

| File | Path |
|------|------|
| `apps/web/src/types/mdm.ts` | frontend type extension |
| `apps/web/src/api/mdm/item.ts` | frontend API client update |
| `apps/web/src/views/mdm/ItemList.vue` | COLLISION_RESOLVED_COMBINED_ITEM_UI |
| `docs/verification/GULIERP_ITEM_UI_REUSE_CLOSURE_V1_REPORT.md` | this closure report |

## 35. Commit hash

(Will be recorded after local commit. Brief §三十六 says NO PUSH
for this Goal — only local commit + manifest provenance.)

## 36. Commit message

`feat(web): close item foundation reuse ui`

(per brief §三十五 — short, single line, fits 1-commit pattern
since the Goal's delta is tightly bounded: 3 frontend source
files + 1 report.)

## 37. Remaining dirty count

44 after this Goal (was 43 + 1 new report file).

Breakdown (post-merge, pre-commit):
- 23 PRE_EXISTING_WIP (Login / Router / Vite / SalesOrder / Employee / Identity / governance / `tests/GuliERP.Identity.*`)
- 1 PRE_EXISTING audit report (untouched)
- 19 PRE_EXISTING docs/business/ standards (untouched)
- 1 `apps/web/src/views/mdm/ItemList.vue` (target of this Goal)
- 1 `apps/web/src/types/mdm.ts` (target of this Goal)
- 1 `apps/web/src/api/mdm/item.ts` (target of this Goal)

After local commit: 41 (this Goal's 4 files committed, 0
post-merge residual).

## 38. Existing WIP preserved

YES — 23 PRE_EXISTING_WIP files completely untouched. The
`docs/verification/GULIERP_MDM_UI_AND_COMMON_FIELD_AUDIT_V1_REPORT.md`
(pre-existing audit from 2026-08-28) is also untouched.

The `Mdm006PgReconcile/` throwaway directory and the 2
`.csx` files from the previous Goal's R2 closure were already
cleaned (not by this Goal — they were gone before this Goal
started, per §3).

## 39. Push status

**NO PUSH** (per brief §三十六). The next Goal
`GULIERP_ITEM_UI_REUSE_CLOSURE_PUSH_V1` is the only place that
talks to `origin/master`.

## 40. Next Goal

`GULIERP_ITEM_UI_REUSE_CLOSURE_PUSH_V1` (Operator-authorized).

If the Operator wants S1-S8 first, the path is:
- `tools/dev/g3-r1c-ensure-role-test-users.ps1` to seed test
  users
- `cd apps/web && npm run dev`
- `cd apps/api/GuliERP.Api && dotnet run --urls http://127.0.0.1:5001`
- Browser through ItemList and confirm the 8 smoke checks
- `GULIERP_ITEM_UI_REUSE_CLOSURE_V1_VERIFIED`

Per brief §三十九, the next big-architecture decision after
this push is `GULIERP_TRANSACTION_DOCUMENT_REDESIGN_V1` (SalesOrder
+ PurchaseOrder redesign), NOT auto-extending MDM to
Employee / Plant / OrganizationUnit.

---

## Appendix: dirty file ledger (this Goal's 4 entries only)

| File | Status | Size delta | Reason |
|------|--------|-----------|--------|
| `apps/web/src/types/mdm.ts` | M | +23 | REUSE_OWNED: add `mnemonicCode` to `ItemDto` / `CreateItemRequest` / `UpdateItemRequest` / `Item` (UI) / `ItemForm` (UI) |
| `apps/web/src/api/mdm/item.ts` | M | +13 | REUSE_OWNED: `dtoToUi` round-trip; `formToCreate` + `updateItem` body pass MnemonicCode; `setItemStatus` preserves it |
| `apps/web/src/views/mdm/ItemList.vue` | M | +75/-18 | COLLISION_RESOLVED_COMBINED_ITEM_UI: preserve 23/15 WIP + add MnemonicCode column + form field + detail row + CSS + search placeholder |
| `docs/verification/GULIERP_ITEM_UI_REUSE_CLOSURE_V1_REPORT.md` | A | new | REUSE_REPORT: 40-item closure report (this file) |
| `apps/api/GuliERP.Api/artifacts/api-{stdout,stderr}.log` | new (untracked, gitignored) | DEFERRED | dev host logs created during this Goal's API smoke; gitignored under `apps/*/artifacts/`; never commit (per goal cleanup) |
