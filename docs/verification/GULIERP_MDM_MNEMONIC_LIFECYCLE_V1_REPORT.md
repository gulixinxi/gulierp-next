# GULIERP_MDM_MNEMONIC_LIFECYCLE_V1_REPORT

Date: 2026-08-31
Repository: `D:\guli\projects\gulierp-next`

## Scope

This round implements the final mnemonic-code UI lifecycle review before the
Master Data UI freeze.

The product reference is U8-style master-data lookup convenience. The technical
implementation is GuliERP-owned and uses `pinyin-pro` in the SPA instead of
copying legacy database functions or triggers.

## Implemented Rules

- `MnemonicCode` remains an ordinary persisted field submitted through existing
  MDM APIs.
- The browser suggests a mnemonic from the record name with `pinyin-pro`.
- The suggestion uses first letters, strips tones and punctuation, uppercases
  the result, and caps it at 40 characters.
- If the operator has not edited the mnemonic, changing the name refreshes the
  suggestion.
- If the operator manually edits the mnemonic, later name changes do not
  overwrite it.
- Clearing the mnemonic returns the field to automatic suggestion mode.
- Existing backend search and persistence behavior are preserved.

## Touched Surfaces

- BusinessPartner create/edit form.
- Item create/edit form.
- Shared frontend mnemonic utility.
- `apps/web` dependency manifest.

## Verification

- `npm install pinyin-pro`: PASS, installed `pinyin-pro@3.29.3`, audit 0
  vulnerabilities.
- `npm run typecheck`: PASS.
- `npm run build`: PASS.

Known build note: Rollup still prints existing `@vueuse/core` PURE annotation
warnings; this is upstream dependency noise and not introduced by the mnemonic
lifecycle change.
