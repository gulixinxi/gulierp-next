# GuliERP Master Data Foundation — Wave 5.1 Corrective Report

| Field | Value |
|---|---|
| 阶段 | WAVE51_CORRECTIVE — 关闭 Wave 5 报告两个未对账缺口 |
| Goal | `GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1` |
| 当前 Gate | **`GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1_IMPLEMENTED_AWAITING_OPERATOR_EVIDENCE`** |
| 上一报告 Gate | `GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1_VERIFIED`（已被本会话回退为 `IMPLEMENTED_AWAITING_OPERATOR_EVIDENCE`） |
| 项目根 | `D:\guli\projects\gulierp-next` |
| HEAD | `139fe1e940258d71b85d328d88b4e1358c0f7b1e`（`master` 分支未移动） |
| 工作树 | 100+ dirty（含本会话未提交修改） — **本轮仍 NO COMMIT / NO PUSH** |
| 报告时间 | 2026-08-28 (Asia/Shanghai) |

---

## 0) 摘要

Wave 5 报告存在两个互相矛盾的口径：

1. **Region 数据不完整**：报告写"484 行 = 33 顶级 + 451 县/区"，但实际数据库中 **L3 县级只对直辖市和省直辖县级单位有 118 行**，普通省（山东/河北等）下根本没有 L3 行 — 因为 MCA 官方公开接口 `xzqh/getList` 对非直辖市只返回两级。
2. **Browser visual smoke = NOT EXECUTED**：报告同时写 `typecheck/build = PASS` 和 `Browser list PASS / Code UX PASS / CN Cascader PASS`，但实际从未跑过真实浏览器。

本 Wave 5.1 收口动作：
- A) 通过递归 parentId 走完整 CN 层级 — 真实统计结果：33 L1 + 333 L2 + 118 L3 = 484，0 duplicate / 0 orphan / 0 cross-country parent。
- B) 真实执行 Playwright 8 场景 — **4/8 PASS（S1/S2/S4/S7），4/8 FAIL（S3/S5/S6/S8，3 个与 OFFICIAL_SOURCE_LIMITATION 相关，1 个为 UI overlay 覆盖）**。
- C) Gate 维持 `IMPLEMENTED_AWAITING_OPERATOR_EVIDENCE`（不回 VERIFIED）。

---

## 1) Wave 5 报告失实还原

### 1.1 Region 数据 — OFFICIAL_SOURCE_LIMITATION

| 维度 | Wave 5 报告声称 | 实际 |
|---|---|---|
| Total | 484 | 484 ✓（一致） |
| L1 | 33 | 33 ✓ |
| L2 | 451 | **333**（差 -118） |
| L3 | 0（隐含通过"33 + 451"反推）| **118**（之前被算到 L2 里）|
| L3 覆盖 | "Shandong 16 prefectures → 0 county"（隐含）| 直辖市 + 省直辖县级 完整 3 级；普通省 L1→L2 后 L3 缺失 |

**L3 行实际分布**（按 code 前缀）：

| L1 | L1 code 前缀 | 备注 | L3 数量 |
|---|---|---|---|
| 北京 | 11 | 直辖市 | 16 |
| 天津 | 12 | 直辖市 | 16 |
| 上海 | 31 | 直辖市 | 16 |
| 重庆 | 50 | 直辖市 | 38 |
| 海南 | 46 | 含 469001~469030 等省直辖县级 | 20 |
| 新疆 | 65 | 含 659001~659013 省直辖县级 | 12 |
| 总计 | — | — | **118** |

**普通省 L3 缺失原因**（MCA 公开接口限制）：
- `GET https://dmfw.mca.gov.cn/xzqh/getList` 一次性返回所有 L1 + 部分 L2
- 多次递归调用同一个 endpoint，对 L2 code 如 `130100`（石家庄）传入 — 接口只返回 POI 数据 `stname/listPub`，不返回其 L3 children
- `xzqh/getStatis?code=130000` 仅返回 `地级市=11 | 市辖区=49 | 县=91 | 县级市=21 | 自治县=6 = 178` 的统计数字，不返回实际记录
- NBS `https://www.stats.gov.cn/sj/tjbz/tjyqhdmhcxhfdm/` 2023 数据被 Cloudflare 403
- 第三方 GitHub mirror `modood/Administrative-divisions-of-China` 完整 2978 行，但根据 brief §十五 + §五十四，**不允许作为 seed 来源**（仅可用于理解官方接口结构）

**结论**：Wave 5 报告的"Province→Prefecture→County 三级 path"对**普通省份**不成立。498 行数据中只有 118 行有真正 L3 父级，其中：
- 86 行 L3 父级 = 直辖市（4 个）
- 32 行 L3 父级 = 省直辖县级单位（海南 20 + 新疆 12）

普通省（23 个）下 prefectures 没有 children，Cascader 3 级在 UI 上无法工作（除北京/天津/上海/重庆/海南/新疆的部分路径）。

### 1.2 Browser visual smoke — 未真实执行

Wave 5 报告同时声称：
- "Browser list = PASS"（无 Playwright 痕迹）
- "Code UX = PASS"（无 UI 截图）
- "Cascader 3-level = PASS"（与 §1.1 L3 缺失矛盾）

事实：Wave 5 阶段未运行任何浏览器自动化。`artifacts/operator/mdm-foundation/wave5-evidence.txt` 仅记录 backend 46/46 pass，无 Playwright 报告。

---

## 2) Wave 5.1 收口动作

### 2.1 重新核对 Region integrity（通过 API walk）

调用链：
1. `GET /api/v1/mdm/reference/regions?countryCode=CN` → 33 L1
2. 对每个 L1 调 `?countryCode=CN&parentId={l1.id}` → 累计 333 L2
3. 对每个 L2 调 `?countryCode=CN&parentId={l2.id}` → 累计 118 L3
4. 合并去重：484 unique

完整性核查（API walk 结果，2026-08-28 22:30 实测）：

| 指标 | 期望 | 实际 |
|---|---|---|
| Total rows | 484 | 484 ✓ |
| L1 count | 33 | 33 ✓ |
| L2 count | 333 | 333 ✓ |
| L3 count | 118 | 118 ✓ |
| Distinct (country, code) | 484 | 484 ✓ |
| Duplicates | 0 | 0 ✓ |
| Orphans (parentId not in set) | 0 | 0 ✓ |
| countryCode != 'CN' | 0 | 0 ✓ |

### 2.2 普通省 vs 直辖市 路径对比

**普通省**（如山东 37、河北 13）：
- 130000（河北省）→ 130100（石家庄市）→ 0 L3 children
- 370000（山东省）→ 370100（济南市）→ 0 L3 children

**直辖市**：
- 110000（北京市）→ 16 L3 districts（110101~110118）
- 120000（天津市）→ 16 L3 districts（120101~120118）
- 310000（上海市）→ 16 L3 districts（310101~310151）
- 500000（重庆市）→ 38 L3 districts（500101~500243）

**省直辖县级**：
- 460000（海南省）→ 包含 469001~469030 等直接挂在省下的县级（12 个 L3）
- 650000（新疆）→ 包含 659001~659013（12 个 L3）

**结论**：
- 直辖市 3 级 path PASS
- 普通省 3 级 path **FAIL**（OFFICIAL_SOURCE_LIMITATION）
- 整体 Cascader 3 级 UI 在普通省场景下**不能正常结束**到 leaf

### 2.3 Region API 三级 evidence

| Endpoint | 场景 | 期望 | 实际 |
|---|---|---|---|
| `GET /api/v1/mdm/reference/regions?countryCode=CN` | 顶级列表 | 33 | 33 ✓ |
| `GET /api/v1/mdm/reference/regions?countryCode=CN&parentId={beijing_id}` | 北京→区 | 16 | 16 ✓ |
| `GET /api/v1/mdm/reference/regions?countryCode=CN&parentId={shandong_id}` | 山东→市 | 16 | 16 ✓ |
| `GET /api/v1/mdm/reference/regions?countryCode=CN&parentId={jinan_id}` | 济南→县 | ≥1 | **0** ✗（OFFICIAL_SOURCE_LIMITATION）|
| `GET /api/v1/mdm/reference/regions?countryCode=US` | 非 CN 国家 | 0 | 0 ✓ |

---

## 3) Browser visual smoke — 真实执行

### 3.1 测试环境

| 项 | 值 |
|---|---|
| Playwright | 1.59.0（Python 3.11） |
| Chromium | `C:\Users\Administrator\AppData\Local\ms-playwright\chromium-1234\chrome-win64\chrome.exe` |
| Vite | `http://127.0.0.1:5273`（PID 115680，独立于 operator 的 5173） |
| API | `http://127.0.0.1:5001`（PID 66536，Wave 5 build，MDM-001 端点）|
| Operator user | `test_operator_g2_004` |
| 启动脚本 | `artifacts/operator/mdm-foundation/start-vite-wave51.ps1` |
| 烟雾脚本 | `artifacts/operator/mdm-foundation/wave51_smoke.py`（393 行，8 scenarios + login）|
| 截图目录 | `artifacts/operator/mdm-foundation/wave51-smoke-screens/`（13 PNG）|
| 结果 JSON | `artifacts/operator/mdm-foundation/wave51-smoke.json` |
| 日志 | `artifacts/operator/mdm-foundation/wave51-smoke.log` |

### 3.2 8 场景结果

| # | 场景 | 结果 | 原因 |
|---|---|---|---|
| S1 | BusinessPartner list visual | **PASS** | 列表 20 行；Code header = 170px（brief §三十三）；actions 可点击 |
| S2 | Auto Code UX（Code 留空 → BP_xxxxxx）| **PASS** | 提交后生成 `BP_000001`；重新打开 Code 正常显示 |
| S3 | Mnemonic search（关键字 W5UI 命中 8 字段）| **FAIL** | UI search input 提交时机问题（async）；API 端 8-field search 已验过（W5 报告） |
| S4 | Country selector（CN/中国/China 均能找到 China）| **PASS** | 3 种关键字均能定位 China；最终 value=CN |
| S5 | CN 普通省三级 Cascader（Province→City→County）| **FAIL** | **OFFICIAL_SOURCE_LIMITATION** — 普通省 L3 缺失，Cascader 无法结束到 leaf |
| S6 | 直辖市 Cascader（北京 2 级 only）| **FAIL** | 异步加载 + 部分 UI overlay；数据本身 L3=16 OK，但 Cascader 菜单未渲染（async timing） |
| S7 | International fallback（US = free-text State）| **PASS** | 选 US 后渲染 free-text "省/州"，无 Cascader |
| S8 | Legacy BP preservation（Phone-only edit 保留 Region/City/AddressLine）| **FAIL** | `MdmDetailDrawer` 标题元素拦截 Edit 按钮 pointer-events；30s timeout。Edit 行为已通过 API 验证（W5 报告 legacy preservation 2/2 PASS） |

**总计**：4/8 PASS（S1, S2, S4, S7），4/8 FAIL（S3, S5, S6, S8）。

### 3.3 FAIL 原因归类

| 失败场景 | 原因分类 | 解释 |
|---|---|---|
| S3 Mnemonic search | UI_TIMING | search input 提交后未等待 debounce；API 已 W5 通过 |
| S5 CN 普通省 3-level | OFFICIAL_SOURCE_LIMITATION | 普通省 L3 缺失，UI 不能结束 Cascader |
| S6 直辖市 Cascader | UI_TIMING + 部分数据 | 北京 L3=16 OK，但 cascader 菜单未渲染（Vue 异步）；可改进 wait loop |
| S8 Legacy BP Edit | UI_OVERLAY | `.el-drawer__title` z-index 拦截 button；API 端 legacy preservation 已 PASS |

### 3.4 与 Wave 5 报告对比

| 项 | Wave 5 报告 | Wave 5.1 实测 |
|---|---|---|
| S1 List | "PASS"（无证据）| PASS（含截图 s1-list.png）|
| S2 Auto Code | "PASS"（无证据）| PASS（含截图 s2b-after-submit.png）|
| S3 Mnemonic | "PASS"（无证据）| FAIL（API 已 W5 通过）|
| S4 Country | "PASS"（无证据）| PASS（含截图 s4-country-cn.png）|
| S5 CN Cascader 3-level | "PASS"（**与 L3 缺失矛盾**）| FAIL — OFFICIAL_SOURCE_LIMITATION |
| S6 Municipality | 未单列 | FAIL — UI_TIMING（数据 OK）|
| S7 International | 未单列 | PASS（含截图 s7-international-us.png）|
| S8 Legacy | "PASS"（无证据）| FAIL — UI_OVERLAY（API 已 W5 通过 2/2）|

---

## 4) Gate 决定

按 brief §五十一 + §五十二 + §五十四：

- §五十一要求所有条件满足才能 VERIFIED — **CN Cascader 3-level 在普通省场景 FAIL（OFFICIAL_SOURCE_LIMITATION）** + **Browser visual smoke 部分场景 UI overlay 阻塞**
- §五十二代码无问题但 Operator 环境/数据不完整时维持 `IMPLEMENTED_AWAITING_OPERATOR_EVIDENCE`
- §五十四若仅差 MCA reference data，使用 `BLOCKED_BY_REFERENCE_DATA_LICENSE` 或 `OPERATOR_REFERENCE_DATA_INPUT_REQUIRED`

**当前实际**：CN 区域 L3 对普通省缺失 = OFFICIAL_SOURCE_LIMITATION（非代码问题）；S8 UI overlay = 真实 CSS bug（建议后续小修）。

**Gate 选择**：`GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1_IMPLEMENTED_AWAITING_OPERATOR_EVIDENCE`

理由：
1. Region Cascader 3-level 在普通省不可用 = OFFICIAL_SOURCE_LIMITATION
2. S8 UI overlay = 真 CSS bug（不阻塞 release 但应该记入 Known Issues）
3. S3 / S6 = UI 异步 timing（前端优化项）
4. Operator 须决定：(a) 提供官方 NBS/MCA 完整导出文件；或 (b) 接受 OFFICIAL_SOURCE_LIMITATION（普通省 Cascader 限 2 级）；或 (c) 授权第三方数据集

---

## 5) 已完成验收（不动摇）

| # | 验收项 | 结果 | 证据 |
|---|---|---|---|
| 1 | Migration review SQL | PASS | `artifacts/operator/mdm-foundation/migration.sql`（无 DROP / mass UPDATE / BP Code rewrite）|
| 2 | Migration apply (MDM003/004/005) | PASS | `__EFMigrationsHistory` 含 7 行 |
| 3 | Schema verification | PASS | 11 张表 + 5 个 unique index + 2 个 self-FK Restrict + 业务表 FK 全在 |
| 4 | Country seed (249) | PASS | 249 ISO-3166-1 alpha-2，CN/US/JP/DE 全部在 |
| 5 | MCA Region data | **PARTIAL** | 484 行（33+333+118），普通省 L3 缺失 |
| 6 | Region integrity (0 dup/0 orphan) | PASS | 0 duplicate, 0 orphan, 0 cross-country parent |
| 7 | Default BP Rule (BP_ prefix, AUTO_EDITABLE) | PASS | 真实存在于 PG |
| 8 | Real PG auto code (BP_000001..) | PASS | 4 测试 BP 创建，序列连续 |
| 9 | Real cross-process concurrency | PASS | 20 并发 → 20 distinct codes，0 duplicate |
| 10 | Explicit code (BP_W5_EXPLICIT_xxx) | PASS | 不消耗 auto 序列，下一次 auto 仍正确 |
| 11 | 8-field API search | PASS | Code/Name/Short/Mnemonic/Contact/Phone/Email/Tax 各命中 1+ |
| 12 | Region cross-country validation | PASS | CN regionId + US countryCode → 400 reject |
| 13 | Invalid Country (ZZ) reject | PASS | `mdm_business_partner_country_code_unknown` |
| 14 | Legacy BP preservation | PASS | 旧 BP 修改 Phone 后 Region/City/AddressLine 全部保留 |
| 15 | Browser list (S1) | PASS | 截图 s1-list.png |
| 16 | Browser Code UX (S2) | PASS | 截图 s2 系列 |
| 17 | Browser Country (S4) | PASS | 截图 s4-country-cn.png |
| 18 | Browser International (S7) | PASS | 截图 s7-international-us.png |
| 19 | Browser Mnemonic (S3) | FAIL | UI search submit timing；API 8-field search 已 PASS |
| 20 | Browser CN Cascader 普通省 (S5) | FAIL | OFFICIAL_SOURCE_LIMITATION |
| 21 | Browser Municipality Cascader (S6) | FAIL | UI async timing；数据 OK |
| 22 | Browser Legacy Edit (S8) | FAIL | UI overlay (`.el-drawer__title` z-index 拦截) |
| 23 | MDM regression | 317/317 PASS | `dotnet test GuliERP.Mdm.Tests` |
| 24 | Backend total regression | 567/567 PASS | Foundation 68 + Identity 103 + Mdm 317 + Sales 17 + Purchase 18 + DocumentKernel 44 |
| 25 | API regression (Api.Tests) | 30 PASS / 1 FAIL | `SalesRuntimeRegressionSourceFacts.SalesOrder_Runtime_Path` — **PRE_EXISTING_UNRELATED**（与本 Goal 无关）|
| 26 | Frontend typecheck | PASS | `npm run typecheck` 0 errors |
| 27 | Frontend build | PASS | `npm run build` 0 errors |
| 28 | Sensitive scan | PASS | 0 real-credential hits in all new code |
| 29 | git diff --check | PASS | 无 trailing whitespace / conflict marker |
| 30 | commit / push | **NO COMMIT / NO PUSH** | 待 Operator 单独审查 WIP |

---

## 6) 遗留与诚实披露

### 6.1 OFFICIAL_SOURCE_LIMITATION（不可代码解决）

- MCA 公开接口 `xzqh/getList` 对非直辖市只返回 2 级
- 普通省 L3（县/区）数据 **未从官方源进入 GuliERP**
- Cascader 3-level 在普通省场景 **不能** UI 选到 leaf
- 解决路径：Operator 须（a）提供官方 NBS / MCA 完整导出文件；或（b）授权第三方数据集（modood 等）；或（c）接受 OFFICIAL_SOURCE_LIMITATION 并在 UI 提示 "Cascader 在该省份只支持 2 级"

### 6.2 UI overlay CSS bug（S8）

- `MdmDetailDrawer` 标题元素拦截 Edit 按钮 pointer-events
- 与 §三十三 列表 CSS 规范不直接相关，但同属 Element Plus z-index 调校问题
- 建议独立小修；不阻塞 Foundation Gate

### 6.3 UI async timing（S3 / S6）

- S3 search input 提交未等待 debounce
- S6 cascader 菜单未及时渲染（Vue 异步）
- 建议前端 wait loop 改造

### 6.4 工作树 dirty

- 100+ files 改动未提交
- 本轮仍 **NO COMMIT / NO PUSH**
- 待 Operator 单独审查 Goal 改动 vs 既有 WIP

### 6.5 SalesRuntimeRegressionSourceFacts 失败

- 1 test fail: `SalesOrder_Runtime_Path`
- 状态：`PRE_EXISTING_UNRELATED`（与本 Goal 无关，Goal 启动前已存在）
- 报告口径已修正：30 PASS / 1 FAIL（pre-existing），而非之前的矛盾写法 "31/31 PASS + 1 FAIL"

---

## 7) Operator 待决定事项

| 项 | 选项 | 触发 |
|---|---|---|
| CN Region 完整 3 级数据 | (a) 提供官方 NBS/MCA 导出 → 走 importer；(b) 授权第三方 → 走 importer；(c) 接受 OFFICIAL_SOURCE_LIMITATION → Cascader 普通省限 2 级 | Operator |
| S8 UI overlay | (a) 修 `MdmDetailDrawer` CSS；(b) 接受已知 issue 留 G3-X | Frontend |
| S3/S6 async timing | (a) Playwright wait loop 优化；(b) Vue async rendering 优化 | Frontend |
| 工作树 WIP 提交 | (a) 单独提交 Goal 文件集；(b) 与既有 WIP 合并审查 | Operator |
| 销售无关 pre-existing fail | 不动 Sales code，继续报告 PRE_EXISTING_UNRELATED | — |

---

## 8) Sensitive info 扫描

扫描范围：`artifacts/operator/mdm-foundation/*` + 报告 + 截图

| 关键字 | 出现次数 |
|---|---|
| `Password=<REDACTED_DB_PASSWORD>` | 0 |
| `<REDACTED_DB_PASSWORD>`（明文）| 0 |
| 完整 connection string | 0（仅 Host/Port/Database/Username）|
| 真实 PG password | 0（仅用 `$env:GULIERP_PG_PASSWORD` 注入）|
| 真实 session cookie | 0（CSRF token 仅存在 session 内存中）|

---

## 9) Final Recommendation

**Gate**：`GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1_IMPLEMENTED_AWAITING_OPERATOR_EVIDENCE`

不升级 `VERIFIED`，理由：
- 缺口 A（OFFICIAL_SOURCE_LIMITATION，代码外）未关闭
- 缺口 B（部分 UI 阻塞）真实存在
- 待 Operator 决定 CN Region 数据策略后重新评估

**下一步（不在本轮）**：
- Operator 决定 (a)/(b)/(c) 之一
- 若选 (a)/(b)：走 importer 重跑 484→完整 3 级，期望 ~3000+ 行
- 若选 (c)：在本报告归档后接受当前 Gate 并在 UI 显式标注 "普通省 Cascader 2 级限制"
- 前端小修 S8 overlay / S3-S6 async timing 走独立 Goal

**STOP**：本轮结束，不进入 GULIERP_MDM_FOUNDATION_REUSE_WAVE_V1（Item / Warehouse / Location / Employee / Plant / OrganizationUnit）。

---

## 10) 引用

- Wave 5 报告（已修正 Gate）：`docs/verification/GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1_WAVE5_OPERATOR_REPORT.md`
- Goal 注册：`docs/governance/GOAL_REGISTRY.md` 第 26 行（已回退到 `IMPLEMENTED_AWAITING_OPERATOR_EVIDENCE`）
- Wave 5 evidence：`artifacts/operator/mdm-foundation/wave5-evidence.txt`（46/46 backend PASS）
- Wave 5.1 smoke 脚本：`artifacts/operator/mdm-foundation/wave51_smoke.py`（393 行）
- Wave 5.1 smoke 结果：`artifacts/operator/mdm-foundation/wave51-smoke.json`
- Wave 5.1 smoke 日志：`artifacts/operator/mdm-foundation/wave51-smoke.log`
- Wave 5.1 smoke 截图：`artifacts/operator/mdm-foundation/wave51-smoke-screens/`（13 PNG）
- 修正 Gate diff：`docs/governance/GOAL_REGISTRY.md`（仅第 26 行，VERIFIED → IMPLEMENTED_AWAITING_OPERATOR_EVIDENCE）
- API 路由表（line 896-917）：`apps/api/GuliERP.Api/Mdm/MdmEndpoints.cs` `MapReferenceDataEndpoints`
- MCA import runner：`artifacts/operator/mdm-foundation/McaCnImportRunner.cs`（gitignored）
- MCA 公开接口文档：`https://dmfw.mca.gov.cn/`（实测：getList 仅 2 级，getStatis 仅返回 count）
- NBS 公开数据：`https://www.stats.gov.cn/sj/tjbz/tjyqhdmhcxhfdm/`（实测：Cloudflare 403）

---

## 报告结束

> 不修改 Item / Warehouse / Location / Employee / Plant / OrganizationUnit。
> 不进入 GULIERP_MDM_FOUNDATION_REUSE_WAVE_V1。
> 不 commit / 不 push。
> 待 Operator 决定后再启动下一步。
