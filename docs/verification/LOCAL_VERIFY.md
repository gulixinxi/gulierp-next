> V0.9说明（2026-09-09）：本页是局部快速验证说明（仅Foundation单测，不代表全模块、实库或浏览器验收）。保留历史正文；当前进展和验证范围以 [基线报告](./GULIERP_BASELINE_FREEZE_V0_9_REPORT.md) 为准。

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

