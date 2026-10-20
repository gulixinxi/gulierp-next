# GuliERP-Next

> An open-source ERP project under development, built on .NET 10 and Vue 3.
> 当前阶段：GULIERP_BASELINE_FREEZE_V0_9。基线整理中，尚不是完整生产就绪 ERP。

感谢 [LINUX DO](https://linux.do) 社区支持推广。

## Current status / 当前状态

| Module | Status / 状态 |
|---|---|
| Identity | 基础认证、组织及权限能力已实现 / Foundation implemented |
| MDM | 主数据基础能力已实现 / Core master data implemented |
| Sales Order | 订单基础能力已实现；不代表发货、库存和财务闭环 / Order slice implemented |
| Purchase Order | 订单基础能力已实现；实库验收仍需核对 / Basic order slice implemented |
| Inventory | 未完成 / Not implemented as a formal module |
| Manufacturing / Quality | 未完成 / Not implemented |
| Finance | 未完成 / Not implemented |
| Mobile / BI | 后续范围 / Future scope |

代码存在、自动化测试通过、实库验收、业务验收分别记录。历史报告中的测试数量与 PASS 仅适用于各自版本，不能证明当前工作树生产可用。

当前结果与限制见 [V0.9基线报告](docs/verification/GULIERP_BASELINE_FREEZE_V0_9_REPORT.md)、[工作树快照](docs/verification/GULIERP_WORKTREE_BASELINE_AUDIT_V1.md)和[变更分类](docs/verification/GULIERP_CHANGE_CLASSIFICATION_V1.md)。本地未提交变更未包含在HEAD中；克隆HEAD不等于获得当前基线候选工作树。

## Technology / 技术栈

- Backend: .NET 10, ASP.NET Core, EF Core 10. SDK resolution follows `global.json`; package versions follow `Directory.Packages.props`.
- Frontend: Vue 3, TypeScript, Vite, Element Plus, Pinia and Vue Router; exact installed versions follow the lockfile.
- Database: PostgreSQL via Npgsql. SQL Server is not claimed as a supported current runtime.
- Authentication: V1 Cookie session + CSRF protection + policy authorization. JWT/Bearer is reserved for future work, not the current login implementation.

`ERP_SYSTEM_ADMIN` does not automatically gain business permissions. Sales, Purchase and MDM permissions belong to their business role packs; current runtime authorization requires dedicated role verification.

## Local development / 本地开发

先准备符合 `global.json` 的 .NET SDK、满足 Vite 依赖要求的 Node.js、已安装依赖，以及由操作员确认的 PostgreSQL 测试环境。凭据通过受保护的本地环境配置提供，勿放入提交、聊天或日志。

已有开发启动入口：

```powershell
pwsh tools/dev/start-stack-with-web.ps1
```

该入口不是已验收的生产部署方案，依赖正确数据库配置、迁移状态和可用端口；实际地址以启动输出为准。本阶段不启动或迁移数据库，不提供默认账号。仓库当前未交付 Docker/Compose 部署文件，因此不提供 Docker 一键启动承诺。

## Verification / 验证

离线脱敏回归：

```powershell
pwsh tools/dev/baseline-v09-security-selftest.ps1
```

MDM单元验证必须显式排除实库种子测试：

```powershell
dotnet test tests/GuliERP.Mdm.Tests/GuliERP.Mdm.Tests.csproj --no-restore --filter "FullyQualifiedName!~GuliERP.Mdm.Tests.ReferenceSeedIntegrationFacts"
```

前端构建（依赖已安装）：

```powershell
cd apps/web
npm run build
```

完整的本阶段项目白名单和结果见基线报告。不要把未过滤的 `dotnet test` 当作安全离线入口：集成测试和种子测试可能访问/修改真实数据库。Operator证据脚本需要明确目标和授权，本次未运行。

## Next stage / 下一阶段

`GULIERP_INVENTORY_KERNEL_V1`：等待下一阶段指令；本阶段不新增库存、采购、销售功能，不修改数据库业务模型、API行为或前端业务页面。Manufacturing与Finance仍为后续能力，不纳入当前完成声明。

## Contributing and security

Review the baseline report and change classification before preparing a contribution. Preserve unrelated work and keep credentials out of code and logs. A green build alone is not database or operator acceptance.

For security reports, contact `chunqing0536@outlook.com` rather than opening a public issue containing sensitive details.

## License and community

[MIT](LICENSE).

- [GitHub Issues](https://github.com/gulixinxi/gulierp-next/issues)
- [GitHub Discussions](https://github.com/gulixinxi/gulierp-next/discussions)
- [Source repository](https://github.com/gulixinxi/gulierp-next)
