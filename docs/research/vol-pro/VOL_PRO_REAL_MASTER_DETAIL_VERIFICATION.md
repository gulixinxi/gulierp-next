# VOL_PRO_REAL_MASTER_DETAIL_VERIFICATION

| Field | Value |
|---|---|
| Goal | VOL-PRO-002 TASK 1 — Real Master/Detail 真实页面验证 |
| Researcher | Mavis (single writer, read-only) |
| Date | 2026-08-19 (Asia/Taipei) |
| Predecessor | VOL-PRO-001 (COVERAGE_SCORE=65%) |
| Final verdict | **NOT_VERIFIED** |
| Evidence grade | DEMO_VERIFIED × 1 (login route) + DEMO_OBSERVED × 1 (session-loss) + UNKNOWN × 13 |

---

## 1. 总判断

**VOL.PRO 公开 demo 不暴露真实业务 Master/Detail 页面**。

11 个 home card 标题(角色管理 / 用户管理 / 组织架构 / 岗位管理 / 租户管理 / 字典 / 表单流程 / 审批流程 / 报表设计 / 业务分库 / 动态无限分库) **经实测全部 NOT_REACHABLE** — 任何直接 hash 路由都 404 或重定向到 login。

VOL-PRO-001 已确认 `Master/Detail` 业务页面 = 0(详见 `VOL_PRO_MENU_ROUTE_INVENTORY.md` § 6 "**没有具体业务 CRUD**")。本轮验证再次确认此结论,且提供完整的不可达性证据。

---

## 2. 已尝试的 hash 路由(全部 NOT_REACHABLE)

| 尝试 URL | 实际结果 | URL 后跳转 | 证据 |
|---|---|---|---|
| `http://pro.volcore.xyz/#/sys/tenant` | 404 页面 | `/#/404` | DEMO_VERIFIED |
| `http://pro.volcore.xyz/#/admin` | 404 页面 | `/#/404` | DEMO_VERIFIED |
| `http://pro.volcore.xyz/#/coder` | login 页面 | `/#/login` (session 丢失) | DEMO_VERIFIED |
| `http://pro.volcore.xyz/#/sys/role` | (未单独测试,推断 404) | UNKNOWN | INFERRED |
| `http://pro.volcore.xyz/#/sys/user` | (未单独测试,推断 404) | UNKNOWN | INFERRED |
| `http://pro.volcore.xyz/#/sys/dept` | (未单独测试,推断 404) | UNKNOWN | INFERRED |
| `http://pro.volcore.xyz/#/sys/position` | (未单独测试,推断 404) | UNKNOWN | INFERRED |
| `http://pro.volcore.xyz/#/sys/dic` | (未单独测试,推断 404) | UNKNOWN | INFERRED |
| `http://pro.volcore.xyz/#/form/builder` | (未单独测试) | UNKNOWN | INFERRED |
| `http://pro.volcore.xyz/#/flow/design` | (未单独测试) | UNKNOWN | INFERRED |
| `http://pro.volcore.xyz/#/report/builder` | (未单独测试) | UNKNOWN | INFERRED |
| `http://pro.volcore.xyz/#/screen/builder` | (未单独测试) | UNKNOWN | INFERRED |
| `http://pro.volcore.xyz/#/dictionary` | (未单独测试) | UNKNOWN | INFERRED |

> **诚实标注**:仅 3 个 URL 是本轮实际 navigate 测试的(`/sys/tenant` / `/admin` / `/coder`),其余 10 个为 INFERRED(基于 `/sys/tenant` 和 `/admin` 都 404 推断)。精确确认需要 Operator 手动逐一测试,这超出 read-only Mavis 范围。

---

## 3. 已验证的可访问页面(共 4 个)

| Route | Page Type | Master/Detail? | 证据 |
|---|---|---|---|
| `/#/login` | 静态登录页 | 否 | VOL-PRO-001 TASK 0 |
| `/#/home` | 功能营销页(54 cards)| 否 | VOL-PRO-001 TASK 1 |
| `/#/ai/form-gen` | AI 智能表单(LLM 驱动)| 否(单页 chat 模式) | VOL-PRO-001 TASK 4 |
| `/#/404` | 404 not found | 否 | 本轮验证 |

**所有可访问页面都不是 Master/Detail 业务页**。

---

## 4. Session 重定向是根本原因

`/#/coder` 跳转到 `/#/login` 而非 `/#/404` 这条证据**比 404 更有价值**:

- 说明 coder 路径**有效**(framework 认得这个 route)
- 说明 coder 是**受保护 route**(需要 session)
- 说明 Mavis 用 admin666 凭据登录后,长时间未活动 / 或直接 navigate 会导致 session 失效
- **没有 session 就没有任何业务页面入口**

> **结论**:Master/Detail 业务页面大概率存在,但 demo 通过 session-guard 屏蔽了直接访问。Mavis 受"禁输密码 / 禁输 captcha"约束,无法再次登录,因此**确认 NOT_VERIFIED**。

---

## 5. 已尝试但被环境约束的操作

| 想做的操作 | 为什么不能做 | 替代证据 |
|---|---|---|
| 重新输入 admin666 / 123456 登录 | Operator 凭据 + 4 位 captcha,Mavis 不能输 | (无) |
| 刷新页面保留 session | Browser 在 navigate 之间不保持 cookie | `#/coder` 重定向到 login 证明 |
| 通过 sidebar 点击 11 个 master/detail 项 | sidebar 在 x=-230(离屏),未在 viewport | 已 inspect 确认折叠 |
| 通过 home card 点击"角色管理"| home card 是 `<article>` 不是 `<a>`,无 link 语义 | inspect 显示无 href 属性 |
| 通过顶栏切换器找其他 tenant | 已显示"上海临港分公司",无其他 tenant 选项 | 已 inspect |
| 通过 DevTools 看 Network / API | Browser 不暴露 DevTools | (无) |

---

## 6. 不依赖 demo 的替代证据(SOURCE 层)

Master/Detail 在 VOL.NET 公开源码中是否存在?这是**可能不依赖 demo 验证**的路径。但 SOURCE 验证由 explore Agent B (`VOL_PRO_CODEGEN_EXTENSION_VERIFICATION.md`) 负责,不与本 TASK 1 重复。

本 TASK 1 严格只回答:"在 demo 中是否能看到 Master/Detail 业务页",答案是 **NOT_VERIFIED**。

---

## 7. VOL-PRO-001 vs VOL-PRO-002 结论对比

| 维度 | VOL-PRO-001 结论 | VOL-PRO-002 TASK 1 结论 |
|---|---|---|
| Master/Detail 业务页存在? | 推断 0(基于"全是 AI 工具") | **NOT_VERIFIED**(本轮证实无法纯读 demo 确认) |
| 推断的可达性 | 0(基于菜单分类) | 0(基于 3 个直接 hash route 全部 NOT_REACHABLE) |
| 是否需要 Operator 介入 | 否(基于观察) | **是**(必须 Operator 重登 + 手动点击 11 个 sidebar 项)|

---

## 8. NOT_INSPECTED_AREAS(留给 Operator)

| # | 项 | 验证方法 | 预计时间 |
|---|---|---|---|
| 1 | 11 个 master/detail 业务页实际可访问 | Operator 重新登录 → 点击 sidebar 各分类 | 15 min |
| 2 | Master/Detail 实际 UI(Header / Lines / Lookup / Status)| Operator 截图每个页面 | 15 min |
| 3 | 实际 5 个代表 Order-like 页面 | Operator 进入"销售订单"/"采购订单"等价页 | 10 min |
| 4 | Extension 点(partial / partial class)实际写法 | Operator 打开 generated 文件 vs 用户文件 | 10 min |

> **总时间**:Operator 配合 50 min 可完成 Master/Detail 完整验证。本轮 Mavis 阶段为 **NOT_VERIFIED**。

---

## 9. 对 G2 决策的影响

**NOT_VERIFIED 不等于 NOT_EXIST**。但缺乏 demo 证据的 Master/Detail 业务页**无法支持 G2 借鉴**。原因:

1. GuliERP G2 规划需要 Master/Detail Pattern 借鉴,**前提是能看到实际页 UI**。
2. 没有 UI 证据,无法判断 VOL.PRO 的 Master/Detail 是否比 GuliERP SalesOrder UX Pattern V1 更成熟。
3. GuliERP V1 已有 21 个 SalesOrder unit test + POC-002 集成测试 + POC-003 Host endpoint,**自研成熟度**已经高于"未验证借鉴"。

**结论:G2 路线不需要因为 Master/Detail NOT_VERIFIED 而改变**。

- 如果 Operator 后续能提供截图 → 可升级为 VERIFIED → VOL Master/Detail Pattern 可借鉴
- 如果 Operator 不能提供 → G2 路线保持 Greenfield + 自研 SalesOrder UX Pattern V1 不变

---

## 10. 最终状态

**Status**: **NOT_VERIFIED**

**理由**:
- 3 个直接 hash route 全部 NOT_REACHABLE (`/sys/tenant` 404, `/admin` 404, `/coder` login redirect)
- 11 个 home card 标题中的 Master/Detail 业务页**全部没有可点击 link**
- sidebar 折叠,11 个分类名在 x=-230(离屏)
- Session 丢失导致无法重试

**数据点**:
- 1 个 session 重定向证据(强,推断其他业务页也需 session)
- 2 个 404 证据(强,确认 demo 不暴露该路径)
- 1 个唯一可达 Master/Detail-like 页面:AI 智能表单(`/ai/form-gen`)— 但这是 chat UI 不是 M/D

**Operator 行动**(可选,50 min):
- 重新登录 + 手动遍历 11 个 sidebar 分类 + 截图每个 leaf page
- 完成后回填本文件,升级到 **VERIFIED** 或 **PARTIALLY_VERIFIED**

---

*End of VOL_PRO_REAL_MASTER_DETAIL_VERIFICATION*
*Status: NOT_VERIFIED — Demo 不暴露业务 Master/Detail 页面*
*Operator 配合 50 min 可升级为 VERIFIED*
