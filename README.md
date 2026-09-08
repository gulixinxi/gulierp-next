# GuliERP

> **Production-ready open source ERP foundation, built on .NET 8 + Vue 3.**
> 388 unit tests + multi-role write-path evidence — all green.

[English](#english) · [简体中文](#简体中文)

---

<a id="english"></a>

## Why another ERP?

| Pain point | Off-the-shelf options | Our answer |
|---|---|---|
| Data leaves the country | Domestic SaaS ERP | Self-host, your data stays put |
| Lock-in / hard to customize | Odoo / ERPNext forks | MIT licensed, modify freely |
| Hidden costs / contract trap | "Enterprise" pricing | Free, MIT, no strings |
| Weak engineering rigor | Marketing-heavy ERPs | Evidence-driven delivery, 388 tests |

GuliERP is the third path: an open, well-tested, self-hostable ERP you can actually own.

---

## What's working today (G3 stage)

| Module | Status | Tests | Notes |
|---|---|---|---|
| **Identity** | ✅ Production-ready | 93 / 93 | 4 roles, 9 system dictionaries, Policy auth, JWT |
| **MDM (Master Data)** | ✅ Production-ready | 278 / 278 | UOM, NumberingRule, Dictionary, Employee, PaymentMethod |
| **Sales** | ✅ Production-ready | 17 / 17 | Full CRUD + 5 cross-module context facades |
| **Inventory** | 🚧 Planned G4 | — | — |
| **Procurement** | 🚧 Planned G4 | — | — |
| **Production** | 🚧 Planned G4 | — | — |
| **Finance (GL)** | 🚧 Planned G5 | — | — |
| **Mobile** | 📅 Future | — | — |

> **Evidence-driven delivery.** Every stage produces a PowerShell script that exercises the actual code paths end-to-end. Reports live under `docs/verification/`.

---

## Tech stack

- **Backend** — .NET 8, ASP.NET Core, EF Core 8, MediatR
- **Frontend** — Vue 3, Vite 7, Element Plus 2, Pinia 3, Vue Router 4, TypeScript
- **Database** — SQL Server 2019+ / PostgreSQL 15+
- **Auth** — ASP.NET Core Identity + JWT + Policy-based authorization
- **Build verification** — PowerShell evidence scripts (4-level validation)

---

## Multi-role permission matrix (verified)

| Role | MDM read | MDM write | Sales | Sales context |
|---|:---:|:---:|:---:|:---:|
| `ERP_SALES_OPERATOR` | ❌ | ❌ | ✅ | ✅ |
| `ERP_MDM_OPERATOR`   | ✅ | ✅ | ❌ | ❌ |
| `ERP_EMPLOYEE_OPERATOR` | ❌ | ❌ | ❌ | ❌ |
| `ERP_SYSTEM_ADMIN`   | ❌ locked | ❌ locked | ❌ locked | ❌ locked |

> ⚠️ **`ERP_SYSTEM_ADMIN` is locked by design.** It can only manage users/roles via UI; it **cannot** call business APIs. This boundary is enforced by 9 unit tests — change it at your own risk.

---

## Quick start

### With Docker (recommended)

```bash
git clone https://github.com/gulixinxi/gulierp-next.git
cd gulierp-next
docker compose up -d
# Browser: http://localhost:8080
# Default accounts: see docs/QUICKSTART.md
```

### Without Docker

```bash
# Backend (requires .NET 8 SDK)
dotnet run --project apps/api/GuliERP.Api

# Frontend (requires Node 20+)
cd apps/web
npm install
npm run dev
```

---

## Verification

```bash
# All unit tests
dotnet test

# Sales order runtime evidence (49 / 49)
pwsh tools/dev/g3-r2a-salesorder-runtime-evidence.ps1

# Sales order web integration evidence (20 / 20)
pwsh tools/dev/g3-r2a-salesorder-web-evidence.ps1

# Master data write-path evidence (38 / 38)
pwsh tools/dev/g3-r1e-master-data-write-path-evidence.ps1
```

Full reports under `docs/verification/`.

---

## Architecture

See `docs/architecture.svg` for a single-page view of the four layers (Frontend / Backend / Database / Auth) plus the permission boundary.

For detailed per-stage design and verification reports:

- `docs/verification/G3_R1D_*` — Master data workbench
- `docs/verification/G3_R1E_*` — Payment method & write path
- `docs/verification/G3_R2A_*` — Sales order runtime rebuild

---

## Roadmap

- **G3** (current) — Identity, MDM, Sales ✅
- **G4** (next) — Inventory, Procurement, Production
- **G5** — Finance (GL, AP, AR)
- **Future** — Mobile client, Reporting / BI

---

## Contributing

We welcome PRs. Please:

1. Fork the repo
2. Create a feature branch (`git checkout -b feat/my-feature`)
3. Run the evidence scripts locally — they must all pass
4. Open a PR using the provided template

For bugs and feature requests, use the issue templates.

---

## Security

For security issues, **do not** open a public issue. Email `chunqing0536@outlook.com` directly. We will respond within 72 hours.

---

## License

[MIT](LICENSE). Use it, modify it, ship it commercially — just keep the copyright notice.

---

<a id="简体中文"></a>

## 简体中文

基于 .NET 8 + Vue 3 的国产开源 ERP 基础框架,**388 个单测 + 多角色写路径验证**全部通过。

### 为什么又造一个 ERP

- **国内 SaaS ERP**:数据出境、定制难、续费涨
- **Odoo / ERPNext**:学习成本高、二开生态碎、本地化不彻底
- **自己造一个**:可控、可改、可商用、不卡脖子

GuliERP 是第三条路。

### 已完成 / 计划中

| 模块 | 状态 | 单测 |
|---|---|---|
| 身份权限(Identity) | ✅ G3 | 93 / 93 |
| 主数据(MDM) | ✅ G3 | 278 / 278 |
| 销售订单(Sales) | ✅ G3 | 17 / 17 |
| 库存/采购/生产 | 🚧 G4 | — |
| 财务总账 | 🚧 G5 | — |
| 移动端 | 📅 未来 | — |

### 角色权限边界(已实测)

| 角色 | MDM 读 | MDM 写 | 销售 | 销售上下文 |
|---|:---:|:---:|:---:|:---:|
| `ERP_SALES_OPERATOR` | ❌ | ❌ | ✅ | ✅ |
| `ERP_MDM_OPERATOR` | ✅ | ✅ | ❌ | ❌ |
| `ERP_EMPLOYEE_OPERATOR` | ❌ | ❌ | ❌ | ❌ |
| `ERP_SYSTEM_ADMIN` | ❌ 锁死 | ❌ 锁死 | ❌ 锁死 | ❌ 锁死 |

> ⚠️ `ERP_SYSTEM_ADMIN` **锁死**——只能在 UI 层管用户/角色,不能调业务 API。9 个单测守护,改要慎重。

### 一键跑起来

```bash
git clone https://github.com/gulixinxi/gulierp-next.git
cd gulierp-next
docker compose up -d
# 浏览器:http://localhost:8080
# 默认账号:见 docs/QUICKSTART.md
```

### License

[MIT](LICENSE)。
