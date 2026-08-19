# VOL_PRO_API_OBSERVATION

| Field | Value |
|---|---|
| Goal | TASK 9 — Network / API 观察(只读正常使用页面)|
| Policy | 不攻击 / 不枚举未授权 API / 不绕过权限;只记 path / method / shape, 不记 token / cookie value |
| Status | **PARTIALLY_VERIFIED** — Network tab 不可直接抓,大部分从 URL 模式 + 卡片描述 推断 |

---

## 1. 已观察到的 API 端点(直接 + 强推断)

### 1.1 Avatar 文件下载端点(已直接观察 URL)

```
GET https://proapi.volcore.xyz/Upload/Tables/Sys_User/202602021347235308/wechart.jpg
```

- **Domain**: `proapi.volcore.xyz` (API 子域, 与主站 `pro.volcore.xyz` 分离)
- **Path pattern**: `/Upload/Tables/{TableName}/{YYYYMMDDHHMMSSxxx}/{filename}`
- **示例表名**: `Sys_User`
- **示例 timestamp**: `202602021347235308` (推断 = 2026-02-02 13:47:23.5308, 精确到 ms)
- **File extension**: `jpg` (image)
- **Authentication**: **UNCLEAR** (URL 可直接访问,无 token in URL)
- **Access control**: **UNCLEAR** (推测 公开可读 or 需 cookie)

### 1.2 通知 / QR 资源(已直接观察 URL)

```
GET https://app-1256993465.cos.ap-nanjing.myqcloud.com/wechat.jpg
GET https://app-1256993465.cos.ap-nanjing.myqcloud.com/Android.png
GET https://app-1256993465.cos.ap-nanjing.myqcloud.com/H5.png
```

- **Domain**: `app-1256993465.cos.ap-nanjing.myqcloud.com` (腾讯云 COS **南京 region**)
- **Bucket**: `app-1256993465`
- **Path pattern**: `/{filename}.{ext}`
- **用途**: 4 个下载二维码(微信小程序 / iOS / Android / H5)— 公开 CDN
- **Authentication**: **无**(完全公开)
- **结论**:营销资源走第三方 CDN,不走自家 API server

### 1.3 API Server(已观察域名 + 推断路径)

**API Server**: `proapi.volcore.xyz` (推断 ASP.NET Core Web API)

**推断的 API path patterns**(从 avatar URL + 常见 ABP / VOL.NET 模式):

| 类别 | 推断 path | HTTP method | 证据 |
|---|---|---|---|
| File upload | `POST /api/file/upload` 或类似 | POST | 推断 — 必有 file upload 端点 |
| File download | `GET /Upload/Tables/{Table}/{Timestamp}/{filename}` | GET | 已观察 |
| Auth login | `POST /api/TokenAuth/Authenticate` 或 `POST /api/account/login` | POST | 推断 — VOL.NET 常用 |
| Auth refresh | `POST /api/TokenAuth/Refresh` 或 `POST /api/account/refresh` | POST | 推断 |
| Captcha | `GET /api/TokenAuth/GetCaptcha` 或 `POST /api/TokenAuth/GetCaptcha` | GET/POST | 推断 — 4 位 captcha 必有 endpoint |
| User info | `GET /api/services/app/User/GetCurrent` 或 `GET /api/account/profile` | GET | 推断 |
| CRUD list | `GET /api/services/app/{Entity}/GetAll` (ABP 模式) | GET | 推断 |
| CRUD by id | `GET /api/services/app/{Entity}/Get` | GET | 推断 |
| CRUD create | `POST /api/services/app/{Entity}/Create` | POST | 推断 |
| CRUD update | `PUT /api/services/app/{Entity}/Update` | PUT | 推断 |
| CRUD delete | `DELETE /api/services/app/{Entity}/Delete` | DELETE | 推断 |
| Generic dynamic | `POST /api/services/app/dynamic/{action}` 或 `POST /api/dynamic/{entity}/{action}` | POST | 推断 — code generator 通常产生 dynamic endpoint |
| AI chat (LLM) | `POST /api/ai/chat` 或 `POST /api/llm/invoke` | POST (SSE 或 WebSocket) | 推断 — DeepSeek 集成 |
| CodeGen preview | `GET /api/codegen/preview` | GET | 推断 — 卡片"代码/预览" |
| CodeGen download | `GET /api/codegen/download` | GET | 推断 — 卡片"下载" |

> **诚实标注**:以上 13 个 endpoint **全部推断**,未实测。**没有 Network tab 抓包**(skill 不提供,DevTools 也不直接访问)。

---

## 2. 推断的 Response Envelope 模式

从 ABP / VOL.NET 通用模式 + 推断:

### 2.1 成功响应

```json
{
  "success": true,
  "result": {
    "id": 12345,
    ...
  }
}
```

### 2.2 错误响应(ABP 模式)

```json
{
  "success": false,
  "error": {
    "code": "SalesOrder:LineQuantityInvalid",
    "message": "数量必须大于 0",
    "details": "...",
    "validationErrors": [
      { "field": "lines[0].quantity", "message": "Required" }
    ]
  },
  "unAuthorizedRequest": false
}
```

### 2.3 列表响应(ABP 模式)

```json
{
  "success": true,
  "result": {
    "totalCount": 1234,
    "items": [ ... ]
  }
}
```

> **DOCUMENTED_ONLY** — 没有 dev console 验证。

---

## 3. Auth 模式(从 chip UI 推断)

| 维度 | 推断 |
|---|---|
| **Auth scheme** | **Bearer token (JWT)** — ABP 模式 + 国家 .NET 项目标配 |
| **Header** | `Authorization: Bearer <token>` |
| **Cookie 同时** | **可能** — 浏览器 session 跨刷新保持,可能用 cookie + bearer |
| **Refresh** | **可能** — 长 session 需要 refresh(否则 24h 后被踢出) |
| **CSRF** | 推断:有(双 token 模式) |

**注**:Skill 规则不允许直接读 Cookie / Token 验证。这只是从 URL + UI 行为推断。

---

## 4. Pagination 模式(推断)

ABP 模式:

```
GET /api/services/app/{Entity}/GetAll?maxResultCount=20&skipCount=0&sorting=...
```

Response:

```json
{
  "result": { "totalCount": 1234, "items": [...] }
}
```

**字段**: `maxResultCount` / `skipCount` / `sorting` (ABP 模式) — 不是 GuliERP TASK F 设计的 `page=1&pageSize=20&sort=...`

---

## 5. Filter / Sort 模式(推断)

| 维度 | 推断 |
|---|---|
| **Filter** | 推断:每个 entity 自己的 filter DTO,例如 `GetAllInput` (ABP 模式) |
| **Sort** | 字符串,多字段用逗号分隔,前缀 `-` = desc |
| **Search** | 推断: `filter` 参数,内部解析为 SQL `LIKE` (常见做法) |

**与 GuliERP TASK F 对比**:
- GuliERP TASK F §10 用 `?filter[field]=value` 形式(typed, by-field)
- VOL.PRO (推测) 用 `?filter=...` 通用形式(less typed)
- **GuliERP 更安全**(白名单字段) — 这是 GuliERP 的优势

---

## 6. API Contract 类型(推断)

| 类型 | VOL.PRO | 证据 |
|---|---|---|
| **传统 REST API** | ✅ | ABP 模式标配 |
| **Generic CRUD** | ✅ | `GetAll` / `Get` / `Create` / `Update` / `Delete` 五件套 |
| **Metadata-driven API** | ✅ 推断有 | 卡片"代码生成可视化" — 生成的 CRUD endpoint 通常是 metadata-driven |
| **Business API** | ✅ | 业务逻辑层 endpoint,如 `POST /sales/orders/confirm` |
| **Dynamic SQL endpoint** | ✅ 推断有 | 卡片"报表设计(自定义sql统计)" — 后端可能执行任意 SQL |

**GuliERP TASK F 立场**:
- 禁止万能 CRUD endpoint
- 禁止 dynamic table endpoint
- 禁止客户端任意 SQL / filter expression

VOL.PRO (推测) 在这些点上**与 GuliERP 立场冲突**:
- VOL.PRO 必须支持万能 CRUD(否则 code generator 不能产出多表)
- VOL.PRO 报表设计器支持自定义 SQL(可能允许前端传 SQL 字符串)

**这是 VOL.PRO 灵活性的代价 = 安全风险**。GuliERP 应不跟随。

---

## 7. Error format 推断

ABP 模式(VOL.PRO 大概率) + 推断:

| HTTP | code | message | 用途 |
|---|---|---|---|
| 200/201/204 | success=true | (data) | 成功 |
| 400 | validation error | "field x invalid" | 客户端错误 |
| 401 | unauthorized | "..." | 未登入 |
| 403 | forbidden | "..." | 权限不够 |
| 404 | entity not found | "..." | 资源不存在 |
| 500 | internal error | "An error occurred" | 服务端错误 |
| 500 | concurrency | "..." | 乐观并发冲突 |

**与 GuliERP TASK F §7 一致性**:
- 4xx vs 5xx 分类 ✓
- unified error envelope ✓ (推断)
- 不返回 raw stack ✓(推断)
- ❌ 可能与 GuliERP 的 `{ error: { code, message, details, traceId, requestId, documentation } }` 略有差异(具体 shape 没验证)

---

## 8. NOT_INSPECTED 区域

| 项 | 状态 |
|---|---|
| 实际 API 路径(全部) | DOCUMENTED_ONLY / 推断 |
| 实际 Response shape | DOCUMENTED_ONLY |
| 实际 Error envelope | DOCUMENTED_ONLY |
| 实际 Pagination 参数名 | DOCUMENTED_ONLY |
| 实际 Filter 参数 | DOCUMENTED_ONLY |
| 实际 Bearer token TTL | NOT_INSPECTED |
| 实际 refresh TTL | NOT_INSPECTED |
| 实际 Auth header name(仅 name, 不值)| 推断: `Authorization` |
| 实际 CSRF 机制 | NOT_INSPECTED |
| 实际 rate limiting | NOT_INSPECTED |
| 实际 backend 框架(.NET 6/7/8/9/10?)| NOT_INSPECTED |
| 实际 ORM(EF Core / SqlSugar / Dapper)| NOT_INSPECTED |
| 实际数据库(SqlServer / MySQL / Pg)| NOT_INSPECTED |

**Operator 配合 1 分钟**:
1. 打开 DevTools → Network 标签
2. 任意点击一个菜单 / tab
3. 看 Request URL(确认 path 模式)
4. 看 Response Headers(找 `X-Powered-By` 之类)
5. 看 Request Payload(确认 shape)

**强烈建议 Operator 做这一步** — 能把本任务从推断升级到 VERIFIED。

---

## 9. 总结

**VOL.PRO API 形态推断**:传统 ABP.NET 风格 REST + metadata-driven code generation + dynamic SQL for reports。

**GuliERP TASK F 立场**:
- 同样的 REST 风格 ✓
- 但禁止万能 CRUD + 禁止 dynamic SQL endpoint — 与 VOL.PRO 冲突
- 这是**安全 vs 灵活性的 tradeoff**,GuliERP 选安全

**借鉴清单**:
- 借鉴:标准 REST shape + 4xx/5xx 分类 + unified error envelope(整体 GuliERP TASK F 已规划)
- 不借鉴:万能 CRUD、metadata-driven API、动态 SQL endpoint(违反 TASK F §3)

---

*End of TASK 9 — VOL_PRO_API_OBSERVATION*
*Status: PARTIALLY_VERIFIED — 2 个 endpoint 直接观察(proapi.volcore.xyz, app-1256993465.cos),13 个 endpoint 推断。Operator 1 分钟 DevTools 可升级到 VERIFIED*
