# Local Verify

Run:

```powershell
pwsh tools/verify.ps1
```

Commands covered:

- Backend build: `dotnet build apps/api/GuliERP.Api/GuliERP.Api.csproj`
- Backend tests: `dotnet test tests/GuliERP.Foundation.Tests/GuliERP.Foundation.Tests.csproj`
- Frontend install: `npm install` in `apps/web`
- Frontend build: `npm run build` in `apps/web`
- Git diff check: `git diff --check`

