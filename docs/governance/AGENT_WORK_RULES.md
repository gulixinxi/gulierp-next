# Agent Work Rules

## Repository Safety

- Do not modify `D:\guli\gulierp`.
- Read old project docs only for knowledge extraction.
- Do not migrate Admin.NET.
- Do not create Admin.NET adapters.
- Do not copy legacy Sales, Purchase or Inventory code.

## Single Writer Areas

Only the main execution agent writes:

- Solution/project structure.
- Governance documents.
- Shared schemas and foundation boundaries.
- Goal registry updates.

Parallel agents may only perform read-only analysis unless explicitly assigned
a scoped write.

## Business Work Rules

- Discovery documents may list facts and open questions.
- Do not invent business rules that the user has not confirmed.
- No formal Sales/Purchase/Inventory page may be created before
  `USER_UX_APPROVED`.

## Verification Rules

Run or report status for:

- Backend build.
- Backend tests.
- Frontend install/build.
- Git diff check.

