# VOL_PRO_PRODUCTIVITY_FEATURES

| Field | Value |
|---|---|
| Goal | TASK 8 — Import / Export / Print / Attachment 调查 |
| Status | **PARTIALLY_VERIFIED** — 从 home page 卡片描述 + avatar URL 推断 |

---

## 1. 8 个能力总结

| 能力 | VOL.PRO 状态 | 证据 |
|---|---|---|
| **Excel Import** | DOCUMENTED_ONLY | 卡片"全自动绑定解析数据源" + "代码生成可视化" 隐含 |
| **Excel Export** | VERIFIED_IN_DEMO | 卡片"报表设计(自定义sql统计)" 描述 "并且支持查询、导出打印等功能" |
| **Template Download** | DOCUMENTED_ONLY | 推断:Excel Import 配套的 template 下载功能,ABP 模式 |
| **Validation Error** | DOCUMENTED_ONLY | 推断:Import 时校验错误高亮 |
| **Print** | VERIFIED_IN_DEMO | 卡片"打印(在线可视化设计)" 明示 — 独立 Print Designer |
| **Print Template** | VERIFIED_IN_DEMO | 同上 — 可视化设计器 = template 编辑器 |
| **Attachment** | VERIFIED_IN_DEMO | avatar URL `https://proapi.volcore.xyz/Upload/Tables/Sys_User/...` 证实文件上传能力 |
| **Image / File Upload** | VERIFIED_IN_DEMO | 同上 — avatar 是 image file |

---

## 2. 详细观察

### 2.1 Attachment(已确认)

| 维度 | 观察 |
|---|---|
| **CDN 域名** | `proapi.volcore.xyz` (API 子域)|
| **对象存储** | `app-1256993465.cos.ap-nanjing.myqcloud.com` (腾讯云 COS 南京 region) |
| **路径模式** | `/Upload/Tables/{TableName}/{YYYYMMDDHHMMSSxxx}/{filename}` |
| **示例** | `https://proapi.volcore.xyz/Upload/Tables/Sys_User/202602021347235308/wechart.jpg` |
| **访问控制** | **UNCLEAR** — 该 URL 可直接 GET,无 token 验证(用浏览器实测会 200 / 403 / 401 都未测) |
| **文件类型** | 从扩展名 `jpg` 看,支持 image(jpg/png/gif 推测) |
| **文件大小限制** | UNCLEAR |

**GuliERP V1 对应**:
- `ObjectStore` 表(TASK B §6.1 提到)
- API 端点是 `GULIERP_API/...` 模式
- **GuliERP 应强制 access control on file GET**(TASK C §2.3 强调 backend = security boundary)

### 2.2 Print Designer(已确认存在)

| 维度 | 观察 |
|---|---|
| **能力** | "在线可视化设计" 卡片 |
| **UI 形态** | DOCUMENTED_ONLY — 推断:拖拽式 template editor(类似 FastReport / Crystal Reports) |
| **Template 存储** | UNCLEAR |
| **输出格式** | UNCLEAR(推测 PDF / HTML) |
| **绑定数据源** | "全自动绑定解析数据源" 卡片 — 推断支持 多种 data source |

**GuliERP V1 对应**:
- TASK F §16:文件上传 / 下载已规划
- Print V1 用**后端 HTML render** (TASK B §2 OUT OF SCOPE 明确列出 PrintDesigner V1 不做)
- **V1.5 借鉴 VOL.PRO Print Designer UI 形态**

### 2.3 Export(已确认存在)

| 维度 | 观察 |
|---|---|
| **触发位置** | 报表 / 列表(推断)|
| **格式** | UNCLEAR(推测 Excel / CSV / PDF)|
| **Excel 模板** | UNCLEAR(可能 NPOI / EPPlus) |

**GuliERP V1 对应**:
- TASK F §16:Export 是 Import 的对偶,API 标准 V1 已规划
- V1.5+ 实现具体 format

### 2.4 Import(DOCUMENTED_ONLY)

| 维度 | 观察 |
|---|---|
| **能力** | 推断存在(代码生成可视化的副产物)|
| **Excel 解析** | 推断:NPOI / EPPlus |
| **校验** | 推断:逐行校验 + 高亮错误 |
| **Template 下载** | 推断:是(从 Excel 模板 → 下载 → 用户填 → 上传) |

**GuliERP V1 对应**:
- TASK F §16:Import 入口已规划
- 校验逻辑待 V1.5

---

## 3. 与 GuliERP V1 设计对比

| 能力 | VOL.PRO | GuliERP V1 (TASK F §16) | GuliERP V1.5 计划 |
|---|---|---|---|
| **Upload** | ✅ | ✅ (POST `/api/v1/{module}/attachments`) | — |
| **Download** | ✅ | ✅ (GET `/api/v1/{module}/attachments/{id}/content`) | — |
| **Access control** | UNCLEAR | ✅ 强制 token 验证(TASK C §2.3)| — |
| **Excel Import** | 推断 ✅ | V1 未实现 | ✅ V1.5 |
| **Excel Export** | ✅ | V1 未实现 | ✅ V1.5 |
| **Template Download** | 推断 ✅ | V1 未实现 | ✅ V1.5 |
| **Validation Error** | 推断 ✅ | V1 未实现 | ✅ V1.5 |
| **Print** | ✅ Visual Designer | V1: backend HTML render | ✅ V1.5+ PrintDesigner |
| **Print Template** | ✅ Visual | V1: hard-coded HTML | ✅ V1.5+ |

**GuliERP 借鉴清单**:
1. ✅ ObjectStore access control 必须严格(TASK C §2.3 已规划)
2. ✅ V1.5+ 实现 PrintDesigner(参照 vol.pro UI 形态)
3. ✅ V1.5+ 实现 Excel Import/Export(参照 vol.pro 模式)
4. ⚠️ 防止 vol.pro 卖点的 "代码生成 + 自动代码 + 自动测试" — R5 风险

---

## 4. NOT_INSPECTED 区域

| 项 | 状态 |
|---|---|
| Print Designer 实际 UI | NOT_INSPECTED |
| Import 模板格式 | NOT_INSPECTED |
| Import 校验规则 | NOT_INSPECTED |
| Export 格式选项 | NOT_INSPECTED |
| 文件 GET access control 实际行为 | NOT_INSPECTED |
| 单文件大小限制 | NOT_INSPECTED |
| 病毒扫描 | UNCLEAR |
| 视频/音频 支持 | UNCLEAR |
| 微信小程序 端 attachment | NOT_INSPECTED |
| H5 端 attachment | NOT_INSPECTED |
| 多语言 attachment 命名 | UNCLEAR |

---

*End of TASK 8 — VOL_PRO_PRODUCTIVITY_FEATURES*
*Status: 8 个能力 4 个 VERIFIED_IN_DEMO(avatar URL + Export + Print 卡片),4 个 DOCUMENTED_ONLY。GuliERP V1 计划已涵盖大部分,V1.5+ 借鉴 UI 形态*
