# GuliERP Next

Greenfield ERP foundation for GuliERP.

This repository is not an Admin.NET fork, not a legacy GuliERP refactor, and not
an Admin.NET adapter. Legacy assets may be read only as business knowledge.

## Goal Gate

Current gate: `GULIERP_GREENFIELD_BOOTSTRAPPED`

## Stack

- Backend: .NET 10, ASP.NET Core native, EF Core, PostgreSQL
- Frontend: Vue 3, TypeScript, Vite, Element Plus, Pinia, Vue Router

## Local Verify

```powershell
pwsh tools/verify.ps1
```

The backend commands require a .NET 10 SDK. The frontend commands require Node
and npm.

