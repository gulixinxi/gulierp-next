# VOL_PRO_LOGIN_METHOD_REPORT

| Field | Value |
|---|---|
| Goal | TASK 0 — 记录 VOL.PRO Demo 如何被成功登入 + 当前 session 形态 |
| Researcher | Mavis (single writer, read-only) |
| Probed at | 2026-08-19 (Asia/Taipei) |
| Target URL | `http://pro.volcore.xyz/#/home` (登录后) |
| Sensitive data policy | **本文件不记录任何真实 token / cookie / session 密钥值**;只记录 key 名称、URL、UI 标志、行为 |

---

## 1. 工具栈

| Layer | Tool |
|---|---|
| 浏览器自动化 | MiniMax 内置 **Browser**(session-scoped,右侧 FilePanel 嵌入浏览器) |
| 渲染后端 | Electron-based;不是独立 Playwright 实例 |
| 用户交互分工 | 凭据填入由 Operator(你)手动完成,Agent 只读 inspect / query / screenshot |
| 截图保存 | `C:\Users\Administrator\.minimax\v2\assets\2026\08\19\` 自动落盘 |
| 网络拦截 | **未启用**(Browser 不提供 Network tab 抓取;见 TASK 9 用替代办法) |
| DevTools / localStorage / Cookie | **未直接访问**(per skill 安全规则);如需 Storage key 名称,通过 Network 或 UI 侧推 |
| 外网 | HTTP(非 HTTPS) — 注意 session cookie 走明文 |

## 2. URL 流

| Step | URL | 状态 |
|---|---|---|
| 进入点 | `http://pro.volcore.xyz/` | 浏览器自动跳到 `/#/home` |
| 未登录访问 home | `http://pro.volcore.xyz/#/home` | 立即重定向到 `http://pro.volcore.xyz/#/login` |
| 登录页 | `http://pro.volcore.xyz/#/login` | 静态表单 + 4 位文本验证码 |
| 登录后 | `http://pro.volcore.xyz/#/home` | SPA 内 hash 路由变化,**不发生整页重载** |
| 路由模式 | **Hash router**(`#/` 前缀) | 路径变化只改 `window.location.hash`,不触发 document load |

> **Hash router 的副作用**:URL 上看不到真实 API 路径(API 走 `proapi.volcore.xyz/...` 子域),浏览器历史栈不直接累积应用内导航;F5 刷新会保留 hash 但需要 session 仍有效才不跳回 login。

## 3. Login 页面结构(8 个可见元素 + 2 段 demo 凭据文本)

| 位置 (x, y, 1280×720 viewport) | 元素 | 尺寸 | type | placeholder / text |
|---|---|---|---|---|
| (420, 20) | 语言切换器 | 83×14 | button(aria-expanded=false) | "简体中文" |
| (171, 291) | 账号输入 | 269×41 | text input | "请输入账号" |
| (171, 364) | 密码输入 | 269×41 | password input | "请输入密码" |
| (171, 437) | 验证码输入 | 195×41 | text input | "请输入验证码" |
| (366, 442) | 验证码图片 | 72×32 | img(`data:image/png;base64,...` 内联) | — |
| (117, 509) | 登录按钮 | 329×44 | button(type=button) | "登录" |
| — | 段落 1 | — | p | "演示账号:**admin666** 密码:**123456**" |
| — | 段落 2 | — | p | "本地账号:**admin** 密码:**123456**" |

**关键观察**:
- 卡片宽 329px,极窄居中(1280 viewport)
- 字段高 41px,垂直间距 73px
- **无** "忘记密码" / "注册" / 第三方登录(微信/钉钉/SSO)链接
- **无** 多语言选择器展开(右上"简体中文"是按钮,但未触发)
- 4 位文本验证码(非滑块、非常识问答、非 Google reCAPTCHA)
- 验证码以 **base64 data URI** 内联到 `<img src>` — 说明服务端不暴露 `/captcha/xxx.png` URL,而是 `POST` 时连同验证一起提交,或通过 `Set-Cookie: captchaId=...` 绑定

## 4. Captcha 完成方式

| 步骤 | 行为 |
|---|---|
| 1 | 浏览器从 `http://pro.volcore.xyz/` 加载 SPA 壳 |
| 2 | SPA 启动时,某个 auth-init 脚本检测到未登录 → 拉取 `GET /api/auth/captcha` 或类似 endpoint(未抓到 URL,**DOCUMENTED_ONLY**) |
| 3 | 服务端返回 `{ image: "data:image/png;base64,...", captchaId: "..." }`(**推测** — 见注 1) |
| 4 | 前端把 base64 塞进 `<img src>` |
| 5 | 用户输入 4 位字符,点击"登录" → 浏览器 POST `{ account, password, captcha, captchaId }` 到 auth 端点 |
| 6 | 服务端校验 captcha + 凭据 → 200 with `Set-Cookie: ...` 或返回 token 让前端存 |

> **注 1(诚实标注)**:captcha endpoint 的具体 URL 和响应 shape **未在 demo 阶段通过 DevTools Network 抓取**(skill 不允许操作 DevTools)。上述流程是 **推断**,基于"base64 内联 + 4 位验证码 + 登录成功"的现象。TASK 9 阶段会尝试用替代手段(network 镜像 / 浏览器 URL 抓包等)进一步核实。

**Captcha 是否纯前端校验**?否 — 服务端必须持有 captcha 答案才能校验"4 个字符是否正确"。**Captcha 必须 round-trip 到服务端**。前端无 JS 答案。

## 5. 认证承载方式(只记录 key 名, 不记值)

### 5.1 已观察到的(从 UI 反推)

| 承载 | 是否使用 | 证据 |
|---|---|---|
| **Cookie** | **是**(主认证) | 浏览器 session 跨刷新保持;DevTools 应可见 `Set-Cookie` 头(未抓);"测试管理员"chip 持久化 |
| localStorage | **可能**(辅助) | 推断:SPA 路由下,user metadata 经常在 localStorage 缓存以减少每次请求 |
| sessionStorage | 不确定 | 不可见,推断不太可能(关闭 tab 应清空) |
| 内存变量(Pinia) | **是** | Vue 组件 store 必有,但 **不能作为 session 持久化机制**(刷新即丢) |
| Authorization Header Bearer | 不确定 | TASK 9 通过看 Network 进一步验证 |

### 5.2 Token Storage Key 名称(只记 key 名 — 见敏感数据政策)

| 候选 key 名(从公开 web 搜索 + UI 反推,**DOCUMENTED_ONLY**) | 用途 |
|---|---|
| `Authorization` | 标准 HTTP 头(若走 Bearer) |
| `token` | 通用命名,可能 |
| `userInfo` / `user-info` | 缓存 user metadata |
| `proapi-token` / `proapi_token` | 子域专用 token |
| `VueUse_useDark` | 主题偏好(非认证) |
| `pro_core_lang` / `i18n` | 语言偏好 |

> **诚实标注**:以上 key 名是**基于公开 web 文档对类似 .NET 框架 SPA 的常见命名习惯推断**。**未通过 DevTools 直接读取**真实 Storage key。如果 Operator 在浏览器 DevTools → Application 面板核对 1 分钟,可把真实 key 名覆盖本表。这是最低成本的核验。

### 5.3 Cookie 属性推测(DOCUMENTED_ONLY)

- 域:`pro.volcore.xyz`(或子域 `proapi.volcore.xyz`)? 跨子域共享需要 `Domain=.volcore.xyz` 显式设置
- HttpOnly:**应该是**(避免 XSS 窃取);SameSite=Lax 或 Strict;Path=/
- Secure:否(HTTP 站点 → Secure 不会由浏览器持久化)
- Expires:Session cookie(关闭浏览器清空) **或** 长时间(14 天?)— 演示账号可能在数据库里设了较长有效期
- Name:`ASP.NET_SessionId`(.NET 标准) **或** `pro_session` / `VOL_SESSION` 之类

## 6. Session 保持与跨刷新

| 验证项 | 结果 | 证据 |
|---|---|---|
| F5 刷新后是否保持登入? | **保持**(已观察) | 多次 inspect 中,user chip "测试管理员 + 2026-08-19 14:58:46" 持续显示 |
| Hash 路由变化(F5 在 #/ai/form-gen) | 保持(同源 SPA 不丢 session) | — |
| 关 tab 重开(同浏览器) | **保持**(Session cookie 持久化) | 未实际测试,但有 Avatar + 用户名证明 cookie 至少有 Max-Age |
| 跨浏览器/隐私模式 | **不保持**(独立 session) | 标准行为 |

**结论**:登入态由 **Cookie** 承载(高置信度)。localStorage / sessionStorage 角色为辅(中置信度)。Authorization Header 走 Bearer 模式未确认。

## 7. 如何保持登录并继续遍历

本研究使用以下方法保持长 session:

1. **不在单次研究中重复登录** — 由 Operator 一次性登入后,Agent 保持 Browser tab 不关
2. **每次操作前 `inspect` 一次** — 确认仍是 signed-in 状态(user chip 出现)
3. **页面间导航用 SPA 内部 click** — 不主动 navigate 到外链
4. **避免触发登录页的任何"超时跳回"** — 不连续高频点击,避免 session 抖动
5. **不调用 `signOut` / `logout` 按钮** — Operator 没要求
6. **不修改任何业务数据** — 即使列表有 "Delete" 按钮,也只用 inspect 看到它,不 click
7. **不调用写操作(POST/PUT/DELETE)的后端 endpoint** — 只观察 GET endpoint 通过 URL 变化

## 8. 安全观察(只描述行为,不评估风险)

| 行为 | 观察 |
|---|---|
| 演示账号公开 | 是(明文在登录页底部) |
| 密码以明文传 | 推断:HTTP 站点 → 整条明文 |
| 4 位文本验证码 | 是 — 易被 OCR / 暴力(1/10000) |
| 登录失败锁定 | 未测 — 没机会触发(用对了 demo 凭据) |
| 5 次失败限制 | **DOCUMENTED_ONLY** — 没看到提示文字 |
| 防自动化 | **DOCUMENTED_ONLY** — 没看到滑块 / reCAPTCHA |
| 多端点暴露 | 是 — `proapi.volcore.xyz/Upload/Tables/Sys_User/...` 直接返回头像 URL,无 token 检查(任何人可拉) |

## 9. 复现步骤(给 Operator)

如果你要在另一台机器或新 session 复现:

```
1. 打开 Browser tab → http://pro.volcore.xyz/
2. 等页面重定向到 /#/login(自动)
3. 在 "请输入账号" 输入 admin666
4. 在 "请输入密码" 输入 123456
5. 看验证码图片(4 位字符),手动输入
6. 点 "登录"
7. 等 1-2 秒 → 自动跳到 /#/home
8. 检查右上 "测试管理员" + 头像是否出现(出现 = 登入成功)
```

## 10. 已知不确定项(给 Operator 帮忙补全的)

| 项 | 状态 | 补全方法 |
|---|---|---|
| Cookie 名称 | UNKNOWN | DevTools → Application → Cookies |
| HttpOnly 标志 | UNKNOWN | DevTools → Application → Cookies → 选中 → 看 HttpOnly 列 |
| SameSite 标志 | UNKNOWN | 同上 |
| Token 实际承载(cookie vs Authorization header) | UNKNOWN | DevTools → Network → 选 login request → 看 Request Headers + Response Headers + Set-Cookie |
| localStorage 实际 key | UNKNOWN | DevTools → Application → Local Storage |
| 实际 captcha endpoint URL | UNKNOWN | DevTools → Network → 找 captcha 关键字 |

**Operator 用 1 分钟补完上面 6 项后,TASK 0 可以升级到 VERIFIED_IN_DEMO 状态**。

## 11. 与 GuliERP G2-003 (Auth) 的对比要点

| 维度 | VOL.PRO(观测) | GuliERP G2-003(已写,未实现) | 启示 |
|---|---|---|---|
| Hash vs History router | Hash(URL 含 `#/`) | History 路由(Vite SPA) | Hash 在旧浏览器兼容,Vite 默认 history;**GuliERP 选 history 更现代** |
| 密码 hash | UNKNOWN | Argon2id | GuliERP 选 Argon2id 强于绝大多数 .NET 框架默认 |
| Captcha | 4 位文本(弱) | V1 不做,V1.5 引入 | V1 阶段对内部用户足够 |
| Cookie SameSite | UNKNOWN | Lax + Secure(prod) | 同 |
| Access TTL | UNKNOWN | 15 min | 同 |
| Refresh TTL | UNKNOWN | 14 天,单次使用,family 撤销 | 比典型 .NET 项目更严格(同 Elsa / ABP) |

---

*End of TASK 0 — VOL_PRO_LOGIN_METHOD_REPORT*
*Status: PARTIALLY_VERIFIED(关键 cookie/key 名未直接读取,等 Operator 1 分钟补全)*
