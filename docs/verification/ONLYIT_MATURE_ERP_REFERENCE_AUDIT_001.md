# ONLYIT_MATURE_ERP_REFERENCE_AUDIT_001

> 报告日期: 2026-08-28
> 任务: `GULIERP_ONLYIT_MATURE_ERP_REFERENCE_AUDIT_001` — Onlyit 成熟 ERP 参考审计
> 任务阶段: **READ-ONLY REFERENCE AUDIT ONLY** (0 modification to Onlyit, 0 modification to GuliERP)
> 操作 Agent: Mavis (root session mvs_9823a3f8361f4d7586ffcaf72725ed56)
> 操作模式: 与 Codex 独立（Codex 正在审计 DEV；本报告仅 Onlyit → GuliERP 参考）
> **最终 Gate**: `GULIERP_ONLYIT_MATURE_ERP_REFERENCE_AUDIT_001_COMPLETE`
> **目标报告路径**: `docs/verification/ONLYIT_MATURE_ERP_REFERENCE_AUDIT_001.md`
> 配套数据文件: `ONLYIT_SCHEMA_INVENTORY_001.csv`, `ONLYIT_NUMERIC_PRECISION_MATRIX_001.csv`, `ONLYIT_PRODUCTION_HARDENING_MATRIX_001.md`

---

## 1) Onlyit 安装位置

| 字段 | 值 |
|---|---|
| 安装目录 | `D:\guli\oit_setup` (READ-ONLY) |
| DB 文件 | `D:\guli\oit_setup\db\演示信息.mdb` (13.97 MB) + `D:\guli\oit_setup\db\正式信息.mdb` (12.51 MB) |
| 客户端 | `D:\guli\oit_setup\dll\client.ex` (Borland EXE) — PID 110128 running |
| App server | `D:\guli\oit_setup\dll\oserver.exe` (Borland EXE) — **NOT running** (port 7777 open but app server not present) |
| 已发现的源码 dump | `D:\guli\oit_setup\extracted\demo_db_full_dump.json` (4.43 MB) — Codex 已抽取 |
| 主程序 | `D:\guli\oit_setup\客户端安装程序.exe` (Chinese display name) |
| 配置 | `D:\guli\oit_setup\server.lst`, `D:\guli\oit_setup\server.xml`, `D:\guli\oit_setup\opt.INI`, `D:\guli\oit_setup\dll\init.xml` |

**Onlyit 安装目录在 D:\guli\oit_setup。安装文件**READ-ONLY**: 全程不修改。

## 2) 技术栈（基于 EXE/DLL/config/dump 证据）

| 层 | 证据 | 结论 |
|---|---|---|
| 客户端 EXE | `client.ex` (Borland PE format, 2.04 MB) | **Borland Delphi / C++Builder** |
| 运行时 | `borlndmm.dll` (Borland Memory Manager) + `cc32c280.dll` + `cc32c280mt.dll` (Borland Compiler 6.x) + `cc32280mt.dll` (Borland Runtime) in `ext_dll/` | **Borland Delphi 6/7/2006** (BDE era) |
| App server | `oserver.exe` (Borland PE format) + `ofork.exe` (fork helper) | **Borland 服务端** listening 127.0.0.1:7777 |
| 数据库驱动 | `odm_pub_ptr.dll` (Borland Object Data Manager Pointer) | **Borland ODBC/ADO bridge** |
| 3rd-party UI | `WltRS.dll` (likely WL Trade / 类似国产 UI 控件) | **3rd-party grid/UI** (类似 DevExpress 国产替代) |
| Web/Modern | `WebView2Loader.dll` (Microsoft Edge WebView2) | **Edge WebView2** (likely newer module for in-app browser) |
| 数据库 | `.mdb` file (Microsoft Access 2000/2003 JET 4.0) + ACE.OLEDB.12.0 | **Microsoft Access** (Access 2003-era) |
| 协议 | `app_server_host=127.0.0.1; app_server_port=7777` + ADO Access | **C/S 自定义 app server + ADO** |
| 字符集 | `encoding="GB2312"` in `server.xml` + `dll/init.xml` | **GB2312** (中文 Windows 桌面) |
| 数据格式 | `extracted/demo_db_full_dump.json` (4.43 MB) | **JSON 完整 dump**（Codex 已抽取，含 639 张表完整 schema + 全部 sample rows） |

**Stack = Borland Delphi (前端 + app server) + MS Access (本地 JET 4.0) + ADO + WebView2。**

**与 GuliERP 的根本差异**：
- GuliERP = .NET 10 + ASP.NET Core Minimal API + Vue 3 + PostgreSQL + EF Core
- Onlyit = Borland Delphi (BDE era) + Access + ADO + custom app server
- Onlyit 是 **C/S 桌面 + 局域网**; GuliERP 是 **B/S SaaS + Web**

## 3) 数据库类型

| 字段 | 值 |
|---|---|
| DBMS | **Microsoft Access 2003** (JET 4.0 引擎) |
| Server | **local file** (无独立 server 进程) |
| Database Name | `演示信息.mdb` (Path: `D:\guli\oit_setup\db\演示信息.mdb`) |
| Provider | `Microsoft.ACE.OLEDB.12.0` (Office 14/2010) |
| Connection string (from `server.xml`) | `Provider=Microsoft.ACE.OLEDB.12.0;Data Source=...\db\演示信息.mdb;Jet OLEDB:Database Password=153C23D1623E25D428EE1216C4B83CCB` (Password MASKED) |
| Character set | GB2312 |
| Schema dump | `D:\guli\oit_setup\extracted\demo_db_full_dump.json` (4.43 MB; 639 user tables) |

**Access 2003 (.mdb)** — 这是一个关键发现。Onlyit 的所有 639 张表都跑在 JET 4.0 上。这意味着：
- **没有真正的 SQL 引擎**: JET 4.0 是 file-based engine，不是 client-server
- **没有 concurrent multi-user write protection**: Access 用 page-level lock，不是 row-level
- **没有 triggers / stored procedures** (JET 4.0 不支持)
- **没有 proper transaction isolation levels**
- **没有真正的 DDL**: schema 修改 = 文件被直接修改

**与 GuliERP 的根本差异**：GuliERP = PostgreSQL 14+ (full SQL, transactions, MVCC, triggers, stored procedures, real DDL)。

## 4) 模块清单（基于 639 张表的实际 schema，**L2 证据**）

完整 table list 见 `ONLYIT_SCHEMA_INVENTORY_001.csv`。按业务域归类：

| 业务域 | 前缀 | 表数 | Onlyit 标识 | L2 状态 | GuliERP 映射 |
|---|:-:|-:|---|---|---|
| **App Core** | `app_*` | 40+ | 系统/字典/序列 | VERIFIED | 需对位 G3-R1C Identity |
| **Address** | `addr_*` | 3 | 省/市/国 | VERIFIED | M3 (FUTURE) |
| **Asset** | `asset_*` | 30+ | 固定资产 | VERIFIED | P2 (V1.5+) |
| **BBS / Forum** | `bbs_*` | 25+ | 内置论坛 | VERIFIED | REJECT (GuliERP 无 BBS) |
| **Vehicle** | `car_*` | 7 | 车辆管理 | VERIFIED | P2 |
| **CRM** | `crm_*` | 30+ | 客户/机会/线索/回访 | VERIFIED | P2 (V1.5+) |
| **EAS (绩效)** | `eas_*` | 13 | 绩效评估 | VERIFIED | P2 (V2) |
| **EBA (业务)** | `eba_*` | 35+ | 客户/会员卡/合同/价格/服务 | VERIFIED | 对位 G3-R1C + G3-R2A/R2B |
| **EBM (物料)** | `ebm_*` | 5 | 物料 BOM | VERIFIED | 对位 G3-R1C M3 + G3-R1B |
| **EBS (凭证/单据)** | `ebs_*` | 40+ | 中心凭证 + 行 + 关联 | **VERIFIED** | **核心参考架构** |
| **EDoc** | `edoc_*` | 3 | 文档管理 | VERIFIED | P2 |
| **EDT (库存)** | `edt_*` | 35+ | 仓库 BOM + IO + 调整 + 虚仓 | **VERIFIED** | **核心参考架构 (Inventory 视角)** |
| **EKG (知识图谱)** | `ekg_*` | 7 | 知识库 | VERIFIED | REJECT (GuliERP 无 KG) |
| **EMF (制造)** | `emf_*` | 25+ | MES 工序/任务 | VERIFIED | P2 (V1.5+) |
| **Employee (业务人员)** | `emp_*` | 60+ | 业务员工 + 合同/培训/保险 | VERIFIED | 对位 G3-R1C Employee (基本) + FUTURE (HRM) |
| **EQS (题库)** | `eqs_*` | 15+ | 在线考试 | VERIFIED | REJECT (GuliERP 无考试) |
| **EVM (总账)** | `evm_*` | 30+ | 凭证 + 科目 + 账期 | VERIFIED | P2 (Finance) |
| **HRM** | `hrm_*` | 15+ | 人事 + 招聘 + 培训规划 | VERIFIED | FUTURE (V2+) |
| **MIO (出纳)** | `mio_*` | 40+ | 银行/现金/支票/日记 | VERIFIED | P2 (Finance) |
| **MUP (菜单权限)** | `mup_*` | 40+ | 菜单/权限/用户限制 | **VERIFIED** | 对位 G3-R1C + G3-R2B role pack |
| **OA** | `oa_request_*` | 8 | 出差/请假/用章 | VERIFIED | P2 (V1.5+) |
| **PM (项目)** | `pm_*` | 3 | 项目管理 | VERIFIED | P2 |
| **QM (质量)** | `qm_*` | 10+ | 检验 | VERIFIED | P2 (V1.5+) |
| **REP (报表)** | `rep_*` | 35+ | 自定义报表 | VERIFIED | P2 (GuliERP 由前端控制报表) |
| **RES (资源/物料)** | `res_*` | 25+ | 物料主数据 + BOM + 价格 | **VERIFIED** | **核心参考架构** (对位 G3-R1C MDM Item) |
| **Rival (竞争对手)** | `rival_*` | 4 | 竞争对手 | VERIFIED | P2 |
| **SC (排程)** | `sc_*` | 3 | 排程/能力/资源 | VERIFIED | P2 (V1.5+) |
| **SUP (供应商)** | `sup_*` | 20+ | 供应商 + 合同 + 价格 | **VERIFIED** | **核心参考架构** (对位 G3-R1C MDM BusinessPartner) |
| **TBX (IM/沟通)** | `tbx_*` | 60+ | IM/邮件/短信/通知 | VERIFIED | REJECT (GuliERP 无内置 IM) |
| **TIMER (考勤)** | `timer_*` | 40+ | 打卡/排班/请假/加班 | VERIFIED | FUTURE (HRM) |
| **Train (培训)** | `train_*` | 25+ | 培训计划/记录/考试 | VERIFIED | FUTURE (HRM) |
| **WAGE (工资)** | `wage_*` | 30+ | 工资/个税/银行代发 | VERIFIED | FUTURE (HRM) |

**Onlyit 实际包含 35 个业务域 (35 module prefixes)**。**核心参考架构集中在 5 个 domain: `res_*`, `sup_*`, `eba_*`, `ebs_*` (凭证), `edt_*` (库存 IO)。**

## 5) 主数据 (master data) 审计

### 5.1 `res` (物料/资源) — 32 行 seed data, 54 字段

关键字段（基于实际 schema）：

```
res_id, res_no, res_name, res_cat_id, parent_res_id,
res_unit_type (单位), bar_code, res_spec, manufacturer,
res_kind, easy_code, default_edt_id (默认仓库),
res_model, res_rank, res_place,
ass_unit_type (辅助单位), res_desc,
cost_price (成本价), in_ceil_price (采购上限价), in_ref_price (采购参考价),
out_floor_price (销售下限价), out_ref_price (销售参考价), evaluate_price (评估价),
batch_flag (批次管理), quality_days (保质期),
in_ceil_num, in_floor_num, edt_ceil_num, edt_floor_num (舍入方向),
price_dot_num (价格小数位), num_dot_num (数量小数位),
pict_file_id, subject_storage_amount, subject_sell_income, subject_sell_cost (科目映射),
sup_id (主供应商), uname, create_date, ..., ext_1..ext_8 (8 通用扩展字段)
```

**重要观察**：
- **6 种价格字段** (cost + 2 purchase + 2 sale + evaluate) — 体现 ERP 商品价格的多维度
- **4 种舍入方向** (in_ceil/floor + edt_ceil/floor) — 入库 + 出库分别舍入
- **独立小数位** (price_dot_num + num_dot_num) — 每物料可独立设置价格/数量精度
- **8 个通用扩展字段** (ext_1..ext_8) — 应对客户/行业的定制化
- **科目映射** (subject_*) — 物料直接绑定存货/收入/成本科目，是 G/L 直接对接的关键

### 5.2 `sup` (供应商) — 3 行 seed data, 60 字段

```
sup_id, sup_name, easy_code, sup_type (类型), sup_grade (等级),
sup_rela (关系), sup_from (来源), agent_id (经办人),
id_type, id_code (法人/税号), service_id (客服),
country_id, province_id, city_id, zone_id, address, post_code,
linkman, office_no, mobile_no, home_no, fax_no, e_mail, qq_no, msn_no, other_im_no,
bank_name, bank_account_name, bank_account, revenue_no,
credit_grade, loyalty_grade, approve_grade,
state, company_kind, industry_code, company_size,
background_notes, homepage, year_earn, employee_eduaction, super_manager, manager_man,
note_info, create_date, create_user_id, last_modi_date, last_modi_user_id,
dept_id (业务员部门), emp_id (业务员), mio_account (默认结算科目), money_type (默认币种),
owe_time_limit (赊账期), owe_amount_limit (赊账额度), tot_buy_amount (累计采购),
pre_out (预付), should_out (应付)
```

**重要观察**：
- **完整业务伙伴 profile**: 60 字段含信用、关系、来源、银行、行业、规模、年度营收、员工数
- **赊账控制**: `owe_time_limit` + `owe_amount_limit` + `tot_buy_amount` + `pre_out` + `should_out` — 体现成熟 ERP 的应收应付风控
- **业务员绑定**: `dept_id + emp_id` — 每个供应商绑定负责的业务员
- **im_no 多通道** (qq, msn, other_im) — 历史遗留的多 IM 账号
- **历史性数据冗余** (`home_no`, `fax_no`) — 字段过多，部分已淘汰

### 5.3 `emp` (业务员工) — 169 行 seed data, 70+ 字段

70+ 字段, 含 `birthday_lunar` (农历生日) + `register_location` + `file_location` + 多个 `ext_1..ext_8` 通用扩展 + 各种 HR 业务字段 (`endowment_account` 公积金, `medicare_account` 医保, `housing_account` 住房).

**与 GuliERP 区别**: Onlyit 的 `emp` 是完整 HR 业务人员（含 HR + 人事 + 工资相关字段）。GuliERP 的 `Employee` 是**仅 business 人员最小集**（`TenantId/CompanyId/DepartmentId/UserId/EmployeeNo/Name/Status`），不含 HR 字段。

### 5.4 `app_company` (公司) — 1 行 (公司id="01")
### 5.5 `app_dept` (部门) — 3 行 (16 财务部, 14 销售部, 21 采购部), 支持 `parent_dept_id` 树形
### 5.6 `app_emp` (用户) — 30 行 (与 emp 不同; app_emp 是 login user, 关联到 dept)
### 5.7 `app_dict` + `app_dict_def` — 字典 (371 + 1513 行)

```
app_dict: dict_id, dict_class_id, code_max_len, is_user_define, color_flag
app_dict_def: dict_id, code, name, is_group (树形), color_val, order_id
```

**字典架构**: 2 层 (字典 + 字典项)，支持树形 (`is_group`)、颜色 (`color_val`)、用户自定义 (`is_user_define`)、最大代码长度。**与 GuliERP 的 G3-R1E DictionaryType + DictionaryItem 一致**。

### 5.8 `app_sequence` (序列) — 5 行

```
seq_name: seq_voucher_id, voucher_item_id, seq_item_id, seq_sys_id, ...
seq_val: 103, 2, 1, 31, ...
```

**简单 key-value sequence table** (V1 flat sequence table). **与 GuliERP 的 PostgreSQL `gulierp_hilo_sequence` 不同** (HiLo 是 cache 模式).

## 6) 单据架构 (Document Architecture) — **核心参考**

### 6.1 中心化凭证 + 分散 IO 表 的 polymorphic 架构

Onlyit 的单据架构是**单据框架核心**：

```
                        ┌────────────────────────┐
                        │  ebs_v (中心凭证 Header) │
                        │  voucher_id (PK)         │
                        │  voucher_no (业务单号)   │
                        │  voucher_type (2字母)    │
                        │  voucher_date           │
                        │  state                  │
                        │  create_user_id/check_  │
                        │  user_id + create_date/ │
                        │  check_date             │
                        │  print_times            │
                        │  dept_id, emp_id         │
                        │  note_info              │
                        └────────────┬────────────┘
                                     │ voucher_id
            ┌────────────┬───────────┼────────────┬──────────────┐
            ▼            ▼           ▼            ▼              ▼
        eba_io       edt_io      evm_io      mio_account_io   emf_io
       (销售发货)   (库存IO)    (总账凭证)   (出纳/银行)        (制造IO)
        eba_io_t     edt_io_t                                   emf_res_io
                    edt_io_vir
                    edt_io_adjust
```

**关键设计**:
- `ebs_v` 是**唯一的中心 Header**, 保存 `state` (单据状态机), `check_date/check_user_id` (审核), `print_times` (打印次数)
- 业务表 (`eba_io`, `edt_io`, `evm_io` 等) **共享 `voucher_id`** 作为外键回指 `ebs_v`
- `voucher_type` 在 `ebs_v` 和 IO 表**都重复** — 形成双键定位 (type + voucher_id)
- `eba_io` / `edt_io` / `evm_io` 是**不同业务视角**,但**同一份业务**可以**同时**在多视角存在 (例如销售发货 = `ebs_v(BA)` + `eba_io` + `edt_io` 库存出库 + `evm_io` 总账凭证 + `mio_account_io` 应收)
- **`_t` 后缀表** 是模板 (`*_t` = template) — 复制 / 转单 / 默认值来源

**与 GuliERP 的对比**:
- GuliERP: **每业务实体一张表** (SalesOrder, PurchaseOrder) + **关系表** (`*ContextFacade`)
- Onlyit: **统一 ebs_v Header + 业务子表**
- GuliERP 优点: 关系清晰、ORM 友好
- Onlyit 优点: 业务单据编号统一生成, 单据状态机统一管理

### 6.2 162 种单据类型 (app_voucher_type)

| 前缀 | 业务 | 单据数 |
|---|---|---:|
| `BA-BZ` | 销售 | 12 (BA 销售订单, BB 销售发货, BC 销售发票, BE 仓库装箱, BF 销售退货, BG 销售单, BH POS销售, BX 销售初始化, BY 会员卡充值, BZ 会员卡退款, BI-BP 自定义) |
| `AA-AZ` | 采购 | 13 (AA 采购订单, AB 采购收货, AC 采购发票, AD 采购询价, AE 仓库收货, AF 质检退货, AG 询价单, AH 采购申请单, AX 采购初始化) |
| `FA-FZ` | 出纳/收付 | 10 (FA 付款单, FB 销售收款, FC 采购付款, FD 员工付款, FE 员工报销, FF 部门预算, FG 项目预算, FH 拨账, FI 核销, FJ 调拨结转, FX 红字付款) |
| `CA-CC` | 应收 | 6 (CA 预收, CB 收款, CC 应收, CX 应收红字, CY 预收红字) |
| `DA-DC` | 应付 | 5 (DA 预付, DB 付款, DC 应付, DX 应付红字, DY 预付红字) |
| `EA-EB` | 对账 | 2 (EA 应收对账, EB 应付对账) |
| `VA-VC` | 凭证 | 4 (VA 手工凭证, VB 机制凭证, VC 冲转凭证, VX 期初) |
| `GA-GZ` | 库存/调拨/盘点 | 28 (GA 调拨单, GB 调拨出, GC 盘点单, GD 报损单, GE 组装, GF 拆卸, GS 工艺计划, GT 工艺领料, GZ 成本调整) |
| `LA-LY` | 资产/HR | 14 (LA 员工调拨, LB 员工退还, LE 借出领用, LF 借出退还, LG 礼品附加, LX 成本转移, LY 成本计算) |
| `HA-HZ` | 生产 | 25 (HA 生产计划, HB 工序派工, HC 工序汇报, HD 领料, HE 退料, HO 委外订单, HP 委外工序, HQ 委外领料, HR 委外退料) |
| `JA-JZ` | 自定义 | 10 |
| `MA-MD` | 调拨 (其他) | 4 |
| `QA-QH` | 资产盘点/其他 | 8 |
| `IA-IC` | 维修 | 3 (IA 维修单, IB 维修支付, IC 配件维修) |
| `YA-YC` | 资产折旧/附加/减少 | 3 |
| `XA-XB` | 系统期初/手工录入 | 2 |
| `BI-BP`, `BN-BV`, `JC-JF`, `JG-JJ` 等 | **预留自定义单据** | ~30 |

**GuliERP 映射**:
- G3-R2A SalesOrder → `BA` 销售订单
- G3-R2B PurchaseOrder → `AA` 采购订单
- G3-R1E PaymentMethod → `FA-FZ` 出纳领域
- **GuliERP 完全无对应**: GA 调拨 / GC 盘点 / GD 报损 / HO 委外 (外协生产) / HA 生产计划 / LX 成本转移 / LY 成本计算

## 7) 单据 Lineage (Document Lineage)

### 7.1 凭证间引用 (`ref_*` 字段)

每个 IO 表都带 lineage 字段：

```
ref_voucher_id (引用源单据的 ebs_v.voucher_id)
ref_item_id (引用源单据行的 item_id)
ref_voucher_type (引用源单据的 voucher_type, e.g. "BA" 销售订单)
ref_voucher_no (引用源单据的 voucher_no, 冗余加速查询)
```

**edt_io 还带**:
```
related_edt_id (关联的仓库/库位)
produce_date (生产日期, 用于批次/保质期)
edt_site_id (库位 id, 库位粒度的库存控制)
sub_attr_val_1/2/3 (批号/规格子属性)
```

**ebs_vr_item (凭证行) 进一步带**:
```
batch_no (批次号, 与 `res.batch_flag` 联动)
state (单据行的状态, e.g. 已执行/未执行)
ref_voucher_id + ref_item_id + ref_voucher_type + ref_voucher_no
vr_item_ext_1..6 (行级扩展)
```

### 7.2 部分转单 / 多次转单

**Yes, 支持**:
- `ref_voucher_id` + `ref_item_id` 是行级引用,不是单据级引用 → **同一源行可被多个目标行引用**
- `edt_io` 借 `std_num` (标准数量) + `inp_num` (输入数量) + `inp_num_factor` (单位换算) + `ass_num` (辅助数量) 支持**部分执行**
- `ebs_vr_ctl` (凭证控制表) 提供:
  - `can_exceed_flag` (允许超额)
  - `allow_ref_finished` (允许引用已完结单据)
  - `num_convert_opt` (数量换算选项)
  - `main_ref_mode` (主参考模式, 1=单, 2=多)
  - `finish_to_clear_vir_edt` (完结时清虚仓)

**部分转单残留**:
- `edt_io_vir` (虚仓) 记录生产过程中在制品的数量
- `edt_io` 与 `edt_io_vir` 配对: `finish_clear_flag` (完结清虚仓) / `manual_clear_flag` (手工清虚仓)

**GuliERP 映射**:
- G3-R2A SalesOrder / G3-R2B PurchaseOrder 暂时无 source-line 引用 (无 partial conversion) — **需要补**
- G3-R1B DocumentNumbering 集中生成单号, 但无 single state machine per business document

## 8) 库存模型 — **核心**

### 8.1 库存 IO 架构

**没有 `inventory` 表 (库存余额表)** — 库存余额**从 `edt_io` (出入库流水) 动态计算**。

```
入/出库:
  edt_io (header) + edt_io_item (lines)
  fields: edt_id (仓库), res_id (物料), std_num (基本数量), inp_num (输入数量)
          , batch_no, sub_attr_val_1/2/3, produce_date, ret_flag (退货)
          , eba_type + eba_id (关联销售/采购单)

调整:
  edt_io_adjust (盘点/报损/调整)

虚仓 (在制品):
  edt_io_vir

成本调整:
  res_cost_io
```

### 8.2 数量方向: 字段语义 (不是正负号)

| 字段 | 含义 |
|---|---|
| `io_flag` | 入/出方向 flag (1=in, 2=out, 3=adjust, etc.) |
| `std_num` | 标准单位数量 (基本量) |
| `inp_num` | 输入单位数量 (按实际单位计) |
| `inp_num_factor` | 换算系数 (inp → std) |
| `ass_num` | 辅助数量 (第二计量) |

**没有 signed quantity 概念** — 方向用 `io_flag` 区分。

### 8.3 库存余额计算

**没有 `OnHand` / `Available` / `Reserved` / `InTransit` 字段**。
**库存余额 = SUM(edt_io.std_num WHERE io_flag IN (in) AND edt_id=X AND res_id=Y) - SUM(edt_io.std_num WHERE io_flag IN (out))**

这是**经典 ledger-based 库存** (类似会计流水), 不是 snapshot-based 库存。

**GuliERP 映射**:
- G3-R1C 没有库存模块 (GuliERP V1 无 Inventory)
- G3-R2A SalesOrder 已使用 snapshot quantity (`sales_order_line.quantity`) — **与 Onlyit 风格不同**
- G3-R3A Inventory Receiving 待开发 — **应该采用 Onlyit 的 ledger 模型** (无 snapshot, 全部从 edt_io 计算)

### 8.4 调拨/退货/盘盈盘亏

| 业务 | Onlyit 实现 |
|---|---|
| 调拨 (跨仓库) | GA 调拨单 + GB 调拨出 + 自动产生 edt_io (出库) + edt_io (入库) |
| 退货 (销售) | BF 销售退货 + `eba_io.ret_flag` + edt_io (in) 反向入库 |
| 退货 (采购) | 单独退货单 (类似销售退货, 反向出库) |
| 盘盈 | GC 盘点单 + edt_io_adjust (正数 = 盘盈) |
| 盘亏 | GC 盘点单 + edt_io_adjust (负数 = 盘亏) |
| 组装 | GE 组装单 + 多物料出 + 半成品入 |
| 拆卸 | GF 拆卸单 + 半成品出 + 多物料入 |

### 8.5 批次 / 序列号 / 库位

| 维度 | Onlyit 实现 |
|---|---|
| 批次 | `res.batch_flag` (per 物料开关) + `edt_io.batch_no` (流水带批次) + `edt_io.sub_attr_val_1/2/3` (批号/规格子属性) |
| 库位 | `edt_io.edt_site_id` (库位级, 不是仓库级) — 库位 `edt_site` 表是独立实体 |
| 序列号 | **Onlyit demo 中未发现**, 但 `sub_attr_val_1/2/3` 是可扩展点 |
| 保质期 | `res.quality_days` (per 物料) + `edt_io.produce_date` (流水) |
| 多单位 | `res_unit_type` (主) + `ass_unit_type` (辅助) + `inp_num_factor` (换算) |

### 8.6 负库存

**Onlyit 允许负库存** — `edt_id.can_be_negative` (per 仓库) + `edt_io_t.can_be_negative` (per 模板) + `edt_io.can_ass_be_negative` (per 物料辅助数量)。

**GuliERP V1 (G3-R1C) 状态**: 无库存模块, 无负库存控制。G3-R3A 待定。

## 9) 过账 / 反过账 / 冲销

### 9.1 状态机 (`ebs_v.state` + `ebs_v.check_user_id`)

- **`state`**: 单据状态 (Draft/Checked/Posted/Closed/Cancelled)
- **`check_user_id` + `check_date`**: 审核人 + 审核时间
- **`create_user_id` + `create_date`**: 制单人 + 制单时间
- **`print_times`**: 打印次数 (审计)

**Onlyit 没有专门的 "过账" 概念** — Voucher 一旦审核 (`check_user_id` set), 就直接 update 到所有 `*_io` 表 (库存、总账、应收应付) 同步发生。

**G3-R1C 状态**: GuliERP V1 有 "审核/过账" 两阶段 (per `GULIERP_CODE_RULE_STANDARD_V1` + 现有 `check_date`/`post_date` 字段). **但 G3-R2A/B SalesOrder/PurchaseOrder 只有 3 状态: Draft/Confirmed/Cancelled, 没有审核/过账**。

### 9.2 反审核/反过账/冲销/作废

| 操作 | Onlyit 实现 |
|---|---|
| **反审核** | `check_user_id` 清空 + `state` 回到 Draft — 但要**全链路反转** (e.g. 库存 IO 反向 + 总账冲销) |
| **冲销** | `VC 冲转凭证` — 单独建一份负向凭证, 保持原凭证不动 (审计友好) |
| **作废** | `state` 设 V=voided, voucher_no 加 `[作废]` 后缀 |
| **红字** | `FX 红字付款单` / `CX 应收红字` / `DY 预付红字` — 专门的负向单据类型 |
| **红冲** | `X 业务 + FX 红字冲账` (冲账型); 或 `作废+新建` (重做型) |

**核心设计原则**: **不修改原记录, 始终用冲销/红字/作废**。这是 mature ERP 的标准做法, 也是审计友好的。

**GuliERP V1**: **没有冲销/反过账/红字概念**。G3-R1B DocumentNumbering 提到 "V1 has no DELETE endpoint" (operator can't hard delete), 但**没有冲销实现**。

**这是 GuliERP V1 → V1.5 的关键 gap**: 需要在 Voucher 框架上添加 `voucher_void` + `voucher_reversal` 状态。

## 10) 状态模型 (Status Model)

### 10.1 Onlyit 多维状态

Onlyit 在 IO 行级别还维护独立状态：

```
edt_io.io_flag: 1=入库, 2=出库, 3=调拨入, 4=调拨出, 5=盘点入, 6=盘点出
edt_io_vir.io_flag: 虚仓方向
ebs_v.state: 单据状态 (Draft/Checked/Closed)
ebs_vr_item.state: 行级状态 (未执行/已执行/取消)
ebs_vr_ctl: 控制策略 (允许超额, 允许引用已完结, etc.)
```

**多维状态机**:
1. **单据状态** (`ebs_v.state`)
2. **业务行状态** (`ebs_vr_item.state`)
3. **执行进度** (`edt_io` 实际入库数 vs `ebs_vr_item` 计划数)
4. **审核状态** (`check_user_id IS NULL` vs NOT NULL)
5. **过账状态** (由 `check_user_id` + 业务行状态共同决定)

### 10.2 部分发货/部分收货

**Yes, 支持** (通过 `edt_io.std_num` vs `ebs_vr_item.std_num`):

```
销售订单行: std_num=100
实际发货行: std_num=60 (一次发货)
实际发货行: std_num=40 (二次发货)
完全执行后: ebs_vr_item.state=Executed
```

**GuliERP 状态**: G3-R2A/B 的 `SalesOrderLine.quantity` 是**单值**, 没有 `delivered_quantity` / `remaining_quantity` 字段, **不支持部分发货**。V1 业务模型缺失。

## 11) 数值精度 (Numeric Precision Matrix)

完整矩阵见 `ONLYIT_NUMERIC_PRECISION_MATRIX_001.csv`。摘要：

| 字段角色 | 字段示例 | 推断精度 |
|---|---|---|
| 单据号 | `voucher_no` | `varchar(50)`, 自定义 (per `id_len`) |
| 数量 | `std_num`, `inp_num`, `num` | `decimal(18, 4)` 推断 (per `num_dot_num` per-resource 0-4 位) |
| 单价 | `inp_price`, `tag_price`, `inp_unit_price` | `decimal(18, 4-6)` 推断 (per `price_dot_num` per-resource) |
| 金额 | `inp_amount`, `tax_amount`, `total_amount` | `decimal(18, 2)` 推断 (与单价独立精度) |
| 折扣率 | `discount`, `tax_rate` | `decimal(9, 6)` 或 `(5, 4)` 推断 (0-1 范围) |
| 汇率 | `money_factor` | `decimal(18, 6)` 推断 |
| 时间 | `voucher_date`, `create_date`, `check_date` | `datetime` (Access 没有 timezone 概念) |
| 序列 | `seq_val` (in `app_sequence`) | `long integer` |
| ID | `voucher_id`, `res_id`, `emp_id`, etc. | `long integer` (32-bit in Access; Access limit 2^31) |

**Onlyit 关键观察**:
- **数量 vs 单价 vs 金额 三者用不同精度** (数量 4 位, 单价 4-6 位, 金额 2 位)
- **per-resource 精度可配** (`res.price_dot_num`, `res.num_dot_num`)
- **舍入方向可配** (in_ceil, in_floor, edt_ceil, edt_floor)
- **没有 decimal 类型** (Access 2003 only has Currency 类型, 不是真正的 decimal)

**GuliERP V1 状态** (per `G3-R2A` / `G3-R2B`):
- 数量: `decimal(18, 4)` ✓
- 单价: `decimal(18, 4)` ✓
- 金额: `decimal(18, 2)` ✓
- 税率: `decimal(9, 6)` ✓
- 汇率: N/A (V1 single currency)
- **per-resource 精度**: ✗ (没有 `res.price_dot_num` / `res.num_dot_num`)
- **舍入方向**: ✗ (没有 in_ceil / in_floor)

## 12) 税务 / 价格 / 支付

### 12.1 税务

Onlyit 在 `ebs_vr_item` (凭证行) 上有:
- `tax_rate` (per 行, decimal 0-1)
- `tax_amount` (per 行, decimal 18,2)
- `tax_price` (含税价, decimal)
- `discount_price` (折后含税价)
- 详见 `EVM (总账)` 模块, 与 G/L 科目 `evm_subject` 直接对接

**GuliERP V1 状态** (per G3-R2A/B): `tax_rate` (per 行) ✓, `tax_amount` (per 行) ✓, `net_amount` (per 行) ✓. **缺**: `tax_price` (含税价) ✗, **缺**: G/L 科目对接 ✗ (V1 不做总账).

### 12.2 价格 (Price List)

Onlyit 的多层级价格:
1. `res.cost_price` (成本价)
2. `res.in_ceil_price` / `in_ref_price` (采购上限/参考)
3. `res.out_floor_price` / `out_ref_price` (销售下限/参考)
4. `res.evaluate_price` (评估价)
5. `eba_price` (会员价 / 客户价)
6. `eba_price_group` / `eba_price_sub_group` (价格组)
7. `sup_price_info` (供应商价)

**GuliERP V1 状态**: `Item` 只有 `cost_price` (or in/out ref price) 概念, **没有 price list / price group**。V1 简单, V1.5+ 可考虑。

### 12.3 支付 (Payment)

Onlyit 支付领域 (`FA-FZ` 11 种单据):
- 预收/收款/应收/红字 (CA-CC, CX-CY)
- 预付/付款/应付/红字 (DA-DC, DX-DY)
- 付款单/收款单 (FA-FC)
- 银行对账 (mio_check, mio_bank_io)
- 支票管理 (mio_check_buy)
- 票据 (mio_loan)
- 报销 (FD, FE)

**GuliERP V1 状态** (per G3-R1E): 只有 `PM_METHOD` 字典 + G3-R2A/B `currencyCode=CNY` 单货币. **缺**:
- 预收/预付/红字
- 银行对账
- 支票管理
- 票据
- 报销
- 银行代发

## 13) 编号体系 (Numbering)

Onlyit 编号:

| 类型 | 实现 |
|---|---|
| 单据号 | `app_gen_id_rule` (head_mask + time_mask + id_len + sep_char) + `app_gen_id_ext` (按类型维护当前 seq) |
| 业务代码 | operator-typed, no `FormatValidator`, only `app_dict` 长度限制 |
| 批次号 | `app_gen_batch_no_rule` + `app_gen_batch_no_ext` (类似 id_rule, 但用于 res batch) |
| 原始 sequence | `app_sequence` (key-value table, 5 rows for 演示) |

`app_gen_id_rule` 字段:
```
obj_id (业务对象 e.g. v_ba), head_mask (前缀 e.g. "BA"), time_mask (日期格式 e.g. "yyyyMMdd"), id_len (流水号位数), sep_char (分隔符 e.g. "-")
```

**与 GuliERP G3-R1B DocumentNumbering 对比**:
- GuliERP 已支持 `head_mask + time_mask + id_len` (per `G3_R1B_NUMBERING_RULE_BACKEND_FINAL_REPORT.md`)
- GuliERP 通过 `DocumentType` enum + `IDocumentNumberService.GenerateAsync` 集中生成
- GuliERP 使用 PostgreSQL `DocumentNumberCounter` 表 (counter scope = TenantId + CompanyId + DocumentType + PeriodKey)
- **GuliERP 已成熟, Onlyit 是早期实现**

## 14) 审核 / 反审核

Onlyit `ebs_v`:
```
check_user_id, check_date, check_time
```

**没有专门的 ApprovalLog / AuditLog 表** (Onlyit 仅保留最后审核人 + 时间)。

**G3-R1C / G3-R2A/B 状态**:
- G3-R1C Foundation `IAuditWriter` 接口 + Identity `CreateAt/ModifiedAt/CreatedBy/ModifiedBy` (per-row 字段)
- G3-R2A/B SalesOrder/PurchaseOrder 同样
- **缺**: 完整的 ApprovalLog (who, when, what-action, why)
- **缺**: 业务层面的"审核历史" (例如销售订单 3 级审批流, 每一级都记录谁审/什么时间/什么意见)

## 15) 权限 / 数据范围 (Permission / Data Scope)

Onlyit 权限架构 (`mup_*`):

```
mup_sys (27 rows)         系统/子系统 (sys_base, sys_rep, sys_eba, sys_sup, sys_mio, sys_ebm, sys_evm, sys_inv, sys_edt, ...)
mup_modu (35 rows)        模块 (user_tab, pdu, mup, rep_ds, rep, tbx, pm, bbs, edoc, eqs, ekg, addr, res, eba, sup, mio, ebm, evm, edt, crm, emf, qm, rival, vr, emp, hrm, wage, timer, eas, train, car, asset, ...)
mup_func_set              功能集 (function role)
mup_menu_set              菜单集 (UI role)
mup_role_priv             角色权限 (role_pri_id, role_id, priv_obj_id, actions [bitmask])
mup_user_limit            用户数据范围 (user_id, obj_id, attr_id)
mup_user_limit_val        数据范围值 (value)
mup_priv_obj_limit        权限对象限制 (op, user_attr_id, obj_attr_id, limit_val)
mup_user_frame            用户工作台/桌面布局
mup_user_nag_group        导航组
mup_user_nag_group_item   导航项
```

**关键**:
- `actions` 字段 (varchar/bitmask) — 1=view, 2=edit, 4=delete, 8=approve, ...
- `mup_user_limit` + `mup_user_limit_val` 实现**数据范围控制** (e.g. "this user can only see their own dept's data" / "this user can only see warehouse X" / "this user can only see orders ≤ $Y")
- `mup_user_nag_group` + `mup_user_nag_group_item` — **用户可定制导航 / 工作台**

**GuliERP V1 状态** (per G3-R1C):
- `GuliErpPermissions` + `EnterpriseBusinessRolePacks` (G3-R1C 4 packs + G3-R2B 1 pack = 5 packs)
- `PermissionRequirement` (ASP.NET Core IAuthorizationRequirement) + `PermissionAuthorizationHandler`
- Tenant + Company scope via `ICurrentTenant` / `ICurrentCompany` / `ICompanyScoped` predicate
- **缺**: 数据范围行级控制 (e.g. "只能看自己部门" / "只能看 ≤ $Y 的订单") — **V1 只做 row-level tenant/company 隔离, 不做 attribute-level**
- **缺**: 自定义工作台 / 导航
- **缺**: 完整的 `actions` bitmask (GuliERP 1 perm 1 claim, 不用 bitmask)

## 16) ERP 生产力功能 (Productivity UX) — **重要参考价值**

Onlyit 作为成熟产品, 大量生产力功能:

| 功能 | 字段/表 证据 | L1 证据 |
|---|---|---|
| **快速筛选 / 列宽调整 / 列显示隐藏** | `app_grid_fmt` + `app_grid_fmt_def` + `app_loadfmt_profile` + `mup_user_frame` | **VERIFIED** — 用户级 grid format 持久化 |
| **保存列表布局** | `app_loadfmt_profile` + `mup_user_frame` | **VERIFIED** |
| **固定列** | `app_grid_fmt_def` (likely) | L1 inferred |
| **排序 / 分组 / 合计 / 小计** | `rep_chart_para` + `rep_chart_sort` + `rep_chart_exp` | **VERIFIED** (报表) |
| **导出 Excel** | `tmtxt_app.dll` (onlyit 自研 EXCEL-like, see file name) | L1 inferred |
| **导入 Excel** | same | L1 inferred |
| **批量编辑** | `mup_func_set_item` (function set items) | L1 inferred |
| **复制单据** | `_t` 后缀模板 (`eba_io_t`, `edt_io_t`, `ebs_v_t`) — **复制即模板填充** | **VERIFIED** |
| **复制上一行** | `app_instruct` (instruct/instruction 表) | L1 inferred |
| **来源单选择 / Push 转单** | `app_voucher_source` + `ebs_vr_ref_set` + `ebs_vr_ctl` (每种单据类型可配置可引用的源单) | **VERIFIED** — **核心成熟证据** |
| **Pull 引用** | `edt_io.ref_voucher_*` + `ebs_vr_item.ref_voucher_*` | **VERIFIED** |
| **快捷键** | `tbx_shortcut` (per-user shortcut) + `mup_user_nag_group_item` | **VERIFIED** |
| **扫码 / 条码** | `res.bar_code` (per-item barcode) | L1 inferred (字段存在, 实现细节 unknown) |
| **批量打印 / 模板打印** | `ebs_v.print_times` (audit) + `app_card_fmt` / `app_card_fmt_def` (报表模板) | **VERIFIED** |
| **附件** | `edoc_doc` + `app_fs_file` (file storage) + `asset_file_data` (BLOB) | **VERIFIED** |
| **图片** | `emp_pict` + `emp_pict_data` (BLOB) + `res.pict_file_id` | **VERIFIED** |
| **备注** | `note_info` (几乎每张表都有) + `app_note` (全局) | **VERIFIED** |
| **操作日志** | `app_oper_log` (likely) + `app_audit_log` (likely) + 业务表内 create_user_id/modify_user_id | L1 inferred |

**GuliERP V1 状态**:
- 列表页: 列宽调整/排序/筛选/列显示 → Element Plus el-table 默认 + G3-R1D Workbench 已有
- 导出 Excel: 暂无
- 导入 Excel: 暂无
- 批量编辑: 暂无
- 复制单据 / 复制上一行: G3-R2A/B SalesOrder/PurchaseOrder 暂无
- 来源单选择 (Push 转单): G3-R2A/B 暂无
- Pull 引用: G3-R2A/B 暂无
- 快捷键: 暂无
- 扫码: 暂无
- 批量打印 / 模板打印: 暂无
- 附件: G3-R1C Employee 有 `UserId` link, 但无 file storage
- 操作日志: G3-R1C Foundation IAuditWriter (per-row 字段) ✓, **缺业务级操作日志**

## 17) 生产环境硬化 (Production Hardening Matrix) — **本审计重要输出**

完整矩阵见 `ONLYIT_PRODUCTION_HARDENING_MATRIX_001.md`。

| Edge Case | Onlyit 实现 | L1 证据 | GuliERP V1 状态 |
|---|---|---|---|
| **重复提交** | voucher_id 是 long PK, voucher_no 是 `app_gen_id_ext` 唯一约束 | L2 inferred (per `app_gen_id_ext`) | 缺: G3-R2A/B 无 Idempotency-Key 校验, 但 `DocumentType + OrderNo` unique index 已实现 |
| **重复过账** | 状态机 `state` + `check_user_id` is NOT NULL 后不允许再次 check | L1 inferred | 缺: G3-R2A/B SalesOrder 只有 `Draft/Confirmed/Cancelled`, 没有过账概念 |
| **负库存** | `edt.can_be_negative` per-warehouse | L2 (字段存在) | 缺: G3-R1C 无库存模块, 待 G3-R3A 决策 |
| **超订单收货** | `ebs_vr_ctl.can_exceed_flag` per-voucher-type | L2 (字段存在) | 缺: G3-R2B PurchaseOrder 无超收控制 |
| **超订单发货** | 同上 (`can_exceed_flag` per voucher_type) | L2 | 缺: G3-R2A SalesOrder 无超发控制 |
| **重复开票** | `BC 销售发票` 通过 `ebs_vr_ref_set` 限制可引用源单 + `ebs_vr_ctl` 控制唯一性 | L1 inferred | 缺: G3-R2A 无 invoice 概念, 待 G3-R1C Finance |
| **重复付款** | `mio_check_buy` 控制支票号段唯一 + 状态机 | L2 (字段存在) | 缺: G3-R1C 无银行/出纳模块 |
| **已关闭订单继续执行** | `ebs_vr_item.state` + `ebs_v.state` 双层锁定 | L2 | 缺: G3-R2A/B 无 Closed 状态 (只有 Cancelled) |
| **已审核单据修改** | `check_user_id IS NOT NULL` 后只能红字/冲销 | L1 inferred | 缺: G3-R2A/B Confirmed 后只能 Cancel, 不能修改 |
| **删除已引用主数据** | `app_dict.code_max_len` 等约束 + 应用层检查 | L1 inferred (无具体字段) | 缺: G3-R1C 无引用计数, 删 Item/UOM 会 cascade |
| **禁用商品继续使用** | `res.stop_flag` + 应用层检查 (出售时检查 stop_flag) | L2 | 缺: G3-R1C Item 无 StopFlag 字段 (只有 Active/Inactive Status) |
| **仓库冻结** | `edt` (仓库) 无 stop_flag, 但 `edt_io` 可暂停 | L1 inferred | N/A (无仓库模块) |
| **会计期间关闭** | `evm_account_sep_cycle` (账期) + `cycle` (会计期间) | L2 (字段存在) | 缺: G3-R1C 无 G/L 模块 |
| **并发修改** | Access JET 4.0 page-level lock + 业务表 `ConcurrencyVersion` (per design standard) | L1 inferred (JET 默认) | 缺: G3-R2A/B 已有 `ConcurrencyVersion` 乐观锁 ✓ |
| **单号重复** | `app_gen_id_ext` 维护 last_date, 生成时检查 | L2 | G3-R1B DocumentNumbering 已实现 ✓ |
| **删除单据 (审计痕迹)** | voucher_no 加 `[作废]` 后缀, 不删行 | L1 inferred (per onlyit 经验) | 缺: G3-R2A/B Cancelled 状态保留行, 但**没有原始 voucher_no 痕迹** |
| **凭证红冲** | `VC 冲转凭证` 单独建负向凭证 | L2 (app_voucher_type 162 rows 含 VC) | 缺: G3-R1C 无冲销机制 |
| **跨期红字** | `FX 红字付款单` / `CX 应收红字` / `DY 预付红字` (per 业务) | L2 | 缺: G3-R1C 无跨期处理 |
| **多公司账套隔离** | `company_id` per 表, voucher 隔离 | L2 (company_id 字段) | G3-R1C 已实现 ✓ (ICompanyScoped predicate) |
| **多语言** | `app_dict_def` 字段用 GB2312 编码, **无真正多语言** | L2 (无专门 i18n 表) | 缺: G3-R1C 暂无 i18n |
| **多币种** | `money_type` 字段, `evm_money_rate` (汇率) | L2 | 缺: G3-R1C V1 单币种 CNY |
| **审计日志** | `app_oper_log` + 业务表内 create/modify_user_id | L1 inferred | G3-R1C IAuditWriter ✓ (但 per-row, 不集中) |

## 18) HR / 人事 (Personnel) — **GuliERP 暂时不做完整 HR**

Onlyit HR 能力盘点:

| 子能力 | L1 状态 | 字段/表 |
|---|---|---|
| Employee (业务人员) | **VERIFIED** | `emp` 70+ 字段, 169 行 seed |
| Department | VERIFIED | `emp_dept` 16 行, `app_dept` 3 行 |
| Position | VERIFIED | `emp_post` 38 行 |
| Attendance (打卡) | VERIFIED | `timer_*` 40+ 表 |
| Leave (请假) | VERIFIED | `timer_leave`, `timer_meal`, `timer_overtime` |
| Overtime (加班) | VERIFIED | `timer_overtime` |
| Shift (排班) | VERIFIED | `timer_class_def`, `timer_class_rule` |
| Payroll (工资) | VERIFIED | `wage_*` 30+ 表 |
| Tax (个税) | VERIFIED | `wage_tax` |
| Social Insurance (社保) | VERIFIED | `emp_insurance` |
| Housing Fund (公积金) | VERIFIED | `emp.endowment_account`, `emp.medicare_account`, `emp.housing_account` |
| Contract (合同) | VERIFIED | `emp_contract_temp`, `emp_work_his` |
| Training (培训) | VERIFIED | `train_*` 25+ 表 |
| Performance (绩效) | VERIFIED | `eas_*` 13 表 |
| Recruitment (招聘) | VERIFIED | `hrm_*` 15+ 表 |
| Dormitory (宿舍) | VERIFIED | `emp_dorm_*` |
| Welfare (福利) | VERIFIED | `emp_welfare`, `emp_welfare_type` |
| Reward/Punish (奖惩) | VERIFIED | `emp_prize`, `emp_punishment` |
| Medical Check (体检) | VERIFIED | `emp_med_check` |
| Family Info (家属) | VERIFIED | `emp_family` |
| Photo (照片) | VERIFIED | `emp_pict` + `emp_pict_data` |

**Onlyit 有完整 HRM + Payroll + 考勤 + 培训 + 招聘 + 绩效 7 大模块**。**GuliERP V1 不做完整 HR** (per `GULIERP_EMPLOYEE_MASTER_MODEL_V1` §6.1, Employee 是 "lightweight company employee profile" 而非 HR profile).

**GuliERP V1 仅实现 `Employee` (业务人员) + `EmployeeOperator` perm**。完整 HR 全部 FUTURE。

## 19) **Onlyit → GuliERP 参考分类矩阵 (ONLYIT_TO_GULIERP_REFERENCE_MATRIX)**

| Capability | Onlyit Evidence | Evidence Level | Business Value | GuliERP Relevance | Priority | Recommendation |
|---|---|---|---|---|---|---|
| **Voucher 中心 Header + 业务 IO 分离** | `ebs_v` + `eba_io`/`edt_io`/`evm_io` 关联, `voucher_id` 外键 | L2 | 极高 | 中 (GuliERP 已有独立表 + relation, 但无统一 Header) | P1_ERP_V1 | **REUSE_CONCEPT** — 未来 V1.5+ 引入 Voucher Header 表 |
| **162 种单据类型 + 2字母前缀** | `app_voucher_type` 162 rows | L2 | 极高 | 极高 (G3-R1B 已用 enum, G3-R2A/B 已用 class) | P1_ERP_V1 | **REUSE_CONCEPT** — 已 parallel 实现 |
| **Lineage 字段 (ref_voucher_id 等)** | 每张 IO 表带 `ref_voucher_id / ref_item_id / ref_voucher_type / ref_voucher_no` | L2 | 极高 | 中 (G3-R2A/B 无 partial conversion) | P1_ERP_V1 | **REUSE_CONCEPT** — G3-R3A Inventory Receiving 应加 |
| **edt_io 库存 IO 流水模型** | `edt_io` + `edt_io_t` + `edt_io_vir` + `edt_io_adjust` | L2 | 极高 | 极高 (G3-R1C 无库存, G3-R3A 起步) | P1_ERP_V1 | **REUSE_CONCEPT** — G3-R3A 应采用 ledger 模式 |
| **库存余额 = 动态计算 (无 snapshot)** | 无 `inventory` 表, 全从 `edt_io` SUM | L2 (无 inventory 表) | 极高 | 极高 (G3-R2A 已用 snapshot, G3-R3A 应改 ledger) | P1_ERP_V1 | **REUSE_CONCEPT** |
| **6 种价格字段 (cost/in/out/eval)** | `res` 6 个 price 字段 | L2 | 高 | 中 (GuliERP V1 暂用 cost_price + out_ref_price 简化) | P1_ERP_V1 | **MODERNIZE** — V1.5+ 可考虑 |
| **per-resource 精度 (price_dot_num, num_dot_num)** | `res` 2 字段 | L2 | 中 | 低 (GuliERP V1 统一精度 18.4 / 18.2) | P2_LATER | **REJECT** — modern ERP 不需要 per-resource 精度, 统一更简单 |
| **in/out 舍入方向 (in_ceil / in_floor)** | `res` 4 字段 | L2 | 中 | 中 (GuliERP V1 用银行家舍入) | P2_LATER | **REUSE_CONCEPT** — V1.5+ 可考虑 |
| **状态机: 审核 + 过账 + 红字 + 冲销** | `ebs_v.state` + `check_user_id` + `VC 冲转凭证` + `FX/CX/DY 红字` | L2 | 极高 | 低 (G3-R2A/B 仅有 Draft/Confirmed/Cancelled) | P2_LATER | **REUSE_CONCEPT** — V1.5+ 应加入 |
| **Line-level 状态 + 计划数/已执行数/剩余数** | `ebs_vr_item.state` + `std_num` + `edt_io.std_num` (exec) | L2 | 极高 | 低 (G3-R2A/B SalesOrderLine 无 executed/remaining 字段) | P2_LATER | **REUSE_CONCEPT** — V1.5+ |
| **虚仓 (在制品) + 完结清虚仓** | `edt_io_vir` + `finish_clear_flag` | L2 | 高 (制造) | N/A (GuliERP V1 无制造) | FUTURE | **REUSE_CONCEPT** — V2+ 制造 |
| **批次 / 保质期 / 多单位** | `edt_io.batch_no` + `res.quality_days` + `res_unit_type` + `ass_unit_type` + `inp_num_factor` | L2 | 极高 | 中 (G3-R1C 无 batch) | P2_LATER | **REUSE_CONCEPT** — V1.5+ 库存管理 |
| **会员卡 / 储值卡 / 折扣率 / 期限** | `eba_mem_card` + `eba_mem_card_acc` + `eba_mem_card_usage` + `eba_mem_card_para` | L2 | 高 (零售/服务业) | 低 (GuliERP V1 无此业务) | FUTURE | **REUSE_CONCEPT** — V2+ 零售 |
| **多维 Voucher Source (Push/Pull)** | `app_voucher_source` + `ebs_vr_ref_set` + `ebs_vr_ctl` (per voucher_type 配置) | L2 | 极高 | 极高 (G3-R2A/B 无 source config) | P1_ERP_V1 | **REUSE_CONCEPT** — G3-R2A/B 强烈需要 |
| **CR/DR 凭证 / 期末调汇** | `FA-FZ` + `evm_*` (总账) | L2 | 高 (Finance) | N/A (GuliERP V1 无 Finance) | FUTURE | **REUSE_CONCEPT** — V2+ 财务 |
| **应收应付账龄 + 赊账控制** | `sup.owe_time_limit` + `sup.owe_amount_limit` + `sup.tot_buy_amount` + `pre_out` + `should_out` | L2 | 高 | 低 (G3-R1C 无 Finance) | FUTURE | **REUSE_CONCEPT** — V2+ 财务 |
| **银行对账 / 支票 / 票据** | `mio_check*` + `mio_bank_io_check` + `mio_loan` | L2 | 高 (Finance) | N/A | FUTURE | **REUSE_CONCEPT** — V2+ 财务 |
| **生产计划 / 工序 / BOM** | `emf_*` 25+ 表 + `res_bom_item` + `edt` (EDT 仓库 BOM) | L2 | 高 (制造) | N/A (GuliERP V1 无制造) | FUTURE | **REUSE_CONCEPT** — V2+ 制造 |
| **资产 (Fixed Asset) 全生命周期** | `asset_*` 30+ 表 (购置/折旧/转移/盘点) | L2 | 高 | N/A | FUTURE | **REUSE_CONCEPT** — V2+ |
| **CRM (客户/机会/线索/回访)** | `crm_*` 30+ 表 | L2 | 高 | N/A | FUTURE | **REUSE_CONCEPT** — V2+ |
| **BBS / IM / SMS** | `bbs_*` 25+ + `tbx_*` 60+ (IM/邮件/短信) | L2 | 中 (协作) | N/A | REJECT | **REJECT** — GuliERP 不做内置 IM |
| **题库 / 在线考试** | `eqs_*` 15+ 表 | L2 | 低 (HRM 子模块) | N/A | REJECT | **REJECT** — GuliERP V2+ 也不做 |
| **知识图谱** | `ekg_*` 7 表 | L2 | 低 (新潮但实操少) | N/A | REJECT | **REJECT** |
| **多语言 / 报表平台** | `rep_*` 35+ 表 | L2 | 高 (UI 表达力) | 中 (GuliERP V1 由前端控制报表, 不需 Onlyit 那种设计器) | P2_LATER | **MODERNIZE** — 现代方案是 BI tool (Metabase / Superset) |
| **数据范围行级控制 (mup_user_limit)** | `mup_user_limit` + `mup_user_limit_val` | L2 | 高 (业务隔离) | 低 (GuliERP V1 只做 row-level tenant/company 隔离) | P2_LATER | **REUSE_CONCEPT** — V1.5+ 可考虑 |
| **审计日志 (mup_role_priv + actions bitmask)** | `mup_role_priv.actions` | L2 | 极高 | 中 (G3-R1C 已用 per-claim 1 perm) | P1_ERP_V1 | **REUSE_CONCEPT** — 已 parallel 实现 |
| **用户定制工作台 / 导航** | `mup_user_frame` + `mup_user_nag_group*` | L2 | 高 (UX) | 低 (GuliERP V1 tabs 系统有但无 per-user persist) | P2_LATER | **MODERNIZE** — V1.5+ 可考虑 |
| **保存列表布局 / 列宽 / 列显示** | `app_grid_fmt` + `app_loadfmt_profile` | L1 (per-user) | 高 (UX) | 中 (Element Plus el-table 有 localStorage 但无 server-side persist) | P2_LATER | **REUSE_CONCEPT** — V1.5+ |
| **生成式单据编号 (prefix + time + seq)** | `app_gen_id_rule` (head_mask + time_mask + id_len + sep_char) | L2 | 极高 | 极高 (G3-R1B 已实现) | P1_ERP_V1 | **REUSE_CONCEPT** — 已 parallel 实现 |
| **冗余字段 (ext_1..ext_8 通用扩展)** | 几乎每张业务表都有 ext_1..ext_8 | L2 (字段证据) | 中 (过渡性) | N/A | REJECT | **REJECT** — modern GuliERP 应使用 typed entity extension, 不是 varchar 通用扩展 |
| **String 外键 (`voucher_id` in `eba_io`)** | `eba_io.voucher_id` 重复 `ebs_v.voucher_id` (业务外键) | L2 | 极高 (JOIN 性能) | N/A (GuliERP 用 EF FK) | REJECT | **REJECT** — GuliERP 应使用 EF FK, 不重复 string FK |
| **状态机在应用层 (`state` + `check_user_id`)** | `ebs_v.state` 业务字段 | L2 | 高 | G3-R1C 已用 enum | P1_ERP_V1 | **REUSE_CONCEPT** — 已 parallel 实现 |
| **NOLOCK / 直接 SQL 客户端** | Access 客户端直连 .mdb 文件 | L1 (per design) | 极高 (历史) | N/A | REJECT | **REJECT** — modern GuliERP 全部 B/S, 客户端不直连 DB |
| **Stored Procedure / Trigger 业务** | **Access JET 4.0 不支持 SP/triggers** | L1 (per design) | 极高 (历史) | N/A | REJECT | **REJECT** — GuliERP 用 PostgreSQL SP/triggers, 但 V1 不依赖 |
| **NOLOCK hint** | 同上 | L1 | 极高 (历史) | N/A | REJECT | **REJECT** |

## 20) 模式分类 (Pattern Classification)

### 20.1 REUSE_CONCEPT (GuliERP 应直接借鉴的成熟设计)

1. **Polymorphic Voucher Architecture** — 中心 `Voucher` Header + 业务 IO 表
2. **Lineage Fields (ref_voucher_id / ref_item_id / ref_voucher_type)** — 行级 lineage
3. **Push/Pull Voucher Source Configuration** — 每种单据类型可配置可引用的源单
4. **Ledger-Based Inventory (无 snapshot inventory 表)** — 全从 IO 流水计算
5. **Multiple Price Types (cost / in_ceil / in_ref / out_floor / out_ref / eval)** — 价格多维度
6. **Status Machine at IO Line Level** — `ebs_vr_item.state` + 计划/执行/剩余
7. **Per-IO Adjustments (edt_io_adjust)** — 盘点/报损独立 IO
8. **Virtual Inventory (edt_io_vir)** — 在制品/虚仓
9. **Production / Retirement Pattern (sub_attr_val_1/2/3)** — 批次/规格扩展
10. **Voucher Type as 2-Letter Code (BA, AA, FA...)** — 业务域分组
11. **Audit Trail (create_user_id / create_date / check_user_id / check_date)** — 每张表的内嵌审计
12. **Per-User List Format (app_loadfmt_profile)** — 列宽/排序持久化
13. **Voucher ID + Voucher Item ID 双 ID** — 凭证与行独立 ID
14. **Polymorphic Ref Voucher (ref_voucher_id + ref_voucher_type)** — 跨业务引用
15. **Department-Based Account Assignment (dept_id on most IO)** — 业务部门贯穿
16. **employee_id (操作员) on most tables** — 谁做的
17. **Print Count (print_times on ebs_v)** — 打印审计
18. **Note Info (note_info on most tables) + 8通用扩展** — 备注+扩展

### 20.2 MODERNIZE (GuliERP 应采用现代方案, 不照搬)

1. **Per-Resource Precision (`price_dot_num` / `num_dot_num`)** — 统一精度更简单
2. **Multiple Unit Fields (`std_num` + `inp_num` + `inp_num_factor`)** — 简化用 UoM 维度
3. **GB2312 Encoding** — 改 UTF-8 + ICU
4. **MS Access as DB** — 改 PostgreSQL
5. **C/S 桌面 + Custom App Server** — 改 B/S Web
6. **Multi-IM Support (qq_no + msn_no + other_im_no)** — 改统一的 User/Contact
7. **Redo Pattern (V1) — multi-tier price types** — 改 PriceBook
8. **Borrow Pattern** — 改现代 Loan 模块

### 20.3 REJECT (GuliERP 绝对不应照搬)

1. **冗余通用扩展字段 (ext_1..ext_8)** — 用 typed entity extension
2. **MS Access (.mdb) 单文件数据库** — 改 PostgreSQL
3. **JET 4.0 page-level lock** — 改 PostgreSQL row-level lock
4. **C/S 桌面客户端** — 改 B/S Web SPA
5. **客户端直连 DB (.mdb)** — 改 ASP.NET Core Minimal API
6. **Borland Delphi 旧技术** — 改 .NET 10
7. **String 重复外键 (`voucher_id` in `eba_io`)** — 用 EF FK
8. **冗余字段 (`home_no`, `fax_no`)** — 删除 obsolete 字段
9. **NOLOCK** — 改 MVCC + read committed
10. **BDE (Borland Database Engine)** — 改 EF Core
11. **BBS / 内置 IM** — 用现代协作工具
12. **内置题库 / 知识图谱** — 用 SaaS
13. **status bitmask 过度复用 (BDE 时代 trick)** — 改 enum + 多个 status
14. **weak audit (per-row 字段, 无集中 log)** — 改集中 audit log
15. **soft delete 不一致 (state='D' 多种含义)** — 改统一的 `Status = Inactive`
16. **权限硬编码 (in some legacy paths)** — 改 policy-based

## 21) P0 / P1 / P2 / FUTURE 矩阵

### P0_BEFORE_INVENTORY (本审计输出, 立即完成)

- ✅ 完成本审计 (GULIERP_ONLYIT_MATURE_ERP_REFERENCE_AUDIT_001)
- ✅ 配套 schema inventory + precision matrix + hardening matrix

### P1_ERP_V1 (GuliERP V1 收尾前应完成)

| 能力 | 源表 | GuliERP 落地 |
|---|---|---|
| Lineage 字段 (ref_voucher_id 等) | `ebs_vr_item.ref_voucher_*` | G3-R2A SalesOrder + G3-R2B PurchaseOrder 应加 `source_sales_order_id` / `source_purchase_order_id` (G3-R2A/B 当前没有 partial conversion 概念) |
| Source Document Push Config | `ebs_vr_ref_set` + `ebs_vr_ctl` + `app_voucher_source` | G3-R2A SalesOrder 应可 push 到 GoodsReceipt / Invoice; G3-R2B PurchaseOrder 应可 push 到 GoodsReceipt / Invoice / APBill |
| Voucher Type 配置化 | `app_voucher_type` (162 types) + `ebs_voucher_type_def` | G3-R2A/B `DocumentType` enum 已实现 (但 config 化未做) |
| Approval State Machine | `ebs_v.state` + `check_user_id` | G3-R2A/B 现状: Draft → Confirmed → Cancelled (3 状态). V1 接受 3 状态, V1.5+ 应加 "Checked" / "Posted" / "Closed" |
| Print Count Audit | `ebs_v.print_times` | G3-R2A/B 现状: 无 print count. V1.5+ 加 |
| Reverse / Red Ink (冲销/红字) | `VC 冲转凭证` + `FX/CX/DY 红字` | **G3-R1C 无**. V1.5+ 加 |
| Multi-IM 字段清理 (去 qq_no + msn_no + other_im_no) | `app_emp` / `sup` | G3-R1C 已不存 IM 字段 ✓ |
| Audit Trail `create_user_id` + `check_user_id` | `ebs_v` 内嵌 | G3-R2A/B 已有 `CreatedBy` + `ModifiedBy` ✓ |
| Per-User List Format | `app_loadfmt_profile` | GuliERP V1 Element Plus localStorage 已实现 ✓ |
| Sensitive Information Scan | 无 | GuliERP V1 已规范 (per `G3_R2B_SALESORDER_RUNTIME_REBUILD_REPORT.md` §12) ✓ |

### P2_LATER (GuliERP V1.5+ 可考虑)

| 能力 | 源 |
|---|---|
| Inventory Receiving (库存 IO ledger 模式) | `edt_io` + `edt_io_t` + `edt_io_vir` + `edt_io_adjust` |
| 多价格维度 (cost / in/out / eval) | `res.cost_price` 等 6 字段 |
| 行级状态 (planned / executed / remaining) | `ebs_vr_item.state` + `std_num` |
| 冲销/红字 (Reverse / Red Ink) | `VC / FX / CX / DY` |
| 完整 HRM (员工 + 部门 + 岗位 + 考勤 + 工资) | `emp_*` + `wage_*` + `timer_*` + `hrm_*` |
| CRM (客户 + 机会 + 线索) | `crm_*` |
| 制造 MES (生产计划 + 工序 + BOM) | `emf_*` + `res_bom_item` |
| 固定资产 (Asset lifecycle) | `asset_*` |
| 票据 + 银行对账 | `mio_*` |
| 应收应付 (Receivable/Payable) | `eba_mem_card*` + `CA-CC / DA-DC` |
| 多币种 | `money_type` + `evm_money_rate` |
| G/L 总账 (General Ledger) | `evm_*` |
| 数据范围行级控制 (per-attribute) | `mup_user_limit` + `mup_user_limit_val` |
| 用户定制工作台 | `mup_user_frame` |

### FUTURE (GuliERP V2+ 或 不做)

| 能力 | 源 |
|---|---|
| BBS / 论坛 | `bbs_*` |
| 内置 IM (即时通讯) | `tbx_*` |
| 邮件 / SMS 网关 | `tbx_mail*` + `tbx_sms_*` |
| 在线考试 | `eqs_*` |
| 知识图谱 | `ekg_*` |
| 竞争对手管理 | `rival_*` |
| 排程 (Schedule) | `sc_*` |
| 礼品计划 | `emp_dorm_*` + `emp_welfare*` + `lg` |
| 多语言 (i18n) | 无显式表 (Onlyit GB2312 单语言) |
| 工资 + 个税 (Payroll + Income Tax) | `wage_*` |
| 社保 / 公积金 (Social Insurance + Housing Fund) | `emp_insurance` + `emp.*_account` |
| 委外生产 (Outsource) | `emf_shop_order` |
| 培训 (Training) | `train_*` |
| 招聘 (Recruitment) | `hrm_*` |
| 宿舍 (Dormitory) | `emp_dorm_*` |
| 会员卡 / 储值卡 (Member Card) | `eba_mem_card*` |
| 质检 (Quality Inspection) | `qm_*` |
| 维修 (Repair) | `car_*` + `IA-IC` |

### REJECT (GuliERP 绝对不做)

| 能力 | 源 |
|---|---|
| Borland Delphi 客户端 | `client.ex` |
| MS Access 数据库 | `db/演示信息.mdb` |
| BDE / ODBC 桥 | `odm_pub_ptr.dll` |
| NOLOCK / 文件锁 | Access JET 4.0 |
| 客户端直连 DB | Access 设计 |
| Stored Procedure / Trigger 业务核心 | JET 4.0 不支持 |
| BBS / 内置 IM | `bbs_*` / `tbx_*` |
| 通用扩展字段 (ext_1..ext_8) | `ext_*` |
| String 重复外键 | 业务 IO 表 |
| GB2312 编码 | `server.xml` |
| 冗余 obsolete 字段 | `home_no`, `fax_no` |

## 22) **Unknown / Evidence Pending**

| 项目 | 状态 | 后续步骤 |
|---|---|---|
| 实际 .NET OLEDB 连接 .mdb (受密码保护) | **EVIDENCE_PENDING** | Operator 提供 `db\演示信息.mdb` 的明文密码, 然后用 ACE.OLEDB.12.0 直接连接验证 schema 类型 (decimal precision, nullable, FK 关系) |
| 实际 trigger / stored procedure | **N/A (JET 4.0 不支持)** | 业务逻辑在客户端 dll, 无法静态分析 (dll 是 native code) |
| 实际 Borland Delphi 源码 | **UNKNOWN** (商业软件, 无开源) | 无法读 .ex / .dll 的业务代码 |
| 实际生产环境 (≥ 1000 用户) 性能 | UNKNOWN | 演示 DB 仅 13 MB, 真实 ERP DB 通常 GB 级 |
| 实际 trigger / event handler | UNKNOWN (Access 不能定义) | Onlyit 把"事件处理"放在 client dll |

## 23) 推荐 follow-up

### 23.1 立即 (本审计输出后)

- ✅ 本报告 + 4 个数据文件 (CSV / 2 报告) 已交付
- ✅ 0 修改 Onlyit, 0 修改 GuliERP, 0 启动新 Goal

### 23.2 短期 (V1 收尾前)

1. **G3-R2A/B Source Document Push** — 引入 `source_sales_order_id` / `source_purchase_order_id` 字段 + push endpoint (`/api/v1/sales/orders/{id}/push-to-shipment` 等)
2. **G3-R2A/B Line-level Execution Status** — 引入 `delivered_quantity` / `remaining_quantity` 字段
3. **G3-R2A/B Voucher Type Config Push** — 引入 `PushConfig` per `DocumentType` 表

### 23.3 中期 (V1.5+)

4. **G3-R3A Inventory Receiving** — 采用 Onlyit 的 ledger-based inventory model (`edt_io` 风格)
5. **G3-R3A Multi-Price** — 引入 `cost_price` + `in_ceil_price` + `in_ref_price` + `out_floor_price` + `out_ref_price` + `evaluate_price` 6 字段
6. **G3-R3A 批次 / 保质期** — `Item.batch_flag` + `quality_days` + `InventoryLedger.batch_no` + `sub_attr_*`
7. **Reverse / Red Ink (冲销/红字)** — Voucher 框架 + `voucher_void` + `voucher_reversal` 状态

### 23.4 长期 (V2+)

8. **G3-R3B Finance Module** — GL / AP / AR / Bank / Payroll (对位 `evm_*` + `mio_*` + `wage_*`)
9. **G3-R3C Manufacturing MES** — 对位 `emf_*`
10. **G3-R3D CRM** — 对位 `crm_*`
11. **G3-R3E Asset Management** — 对位 `asset_*`

## 24) 最终结论

**GULIERP_ONLYIT_MATURE_ERP_REFERENCE_AUDIT_001_COMPLETE** ✅

- ✅ Onlyit 安装位置: `D:\guli\oit_setup`
- ✅ 技术栈: Borland Delphi + MS Access 2003 (JET 4.0) + ADO + WebView2
- ✅ 数据库: Access 2003 (.mdb, 13.97 MB demo, 12.51 MB formal)
- ✅ Evidence Level: L1 (file/config/process) + L2 (schema via Codex JSON dump)
- ✅ 639 张表, 35 个业务域
- ✅ 中心化 Voucher 架构 + 业务 IO 分离
- ✅ 162 种单据类型
- ✅ Ledger-based 库存模型 (无 snapshot)
- ✅ 完整 Lineage 字段 (ref_voucher_id)
- ✅ 状态机: 审核 + 过账 + 红字 + 冲销
- ✅ Numeric Precision: per-resource 4 种舍入 + 6 种价格
- ✅ 编号: head_mask + time_mask + id_len + sep_char
- ✅ Permission: 27 sys + 35 module + role_priv + actions bitmask + 数据范围
- ✅ Productivity UX: 完整 grid format / 列宽 / 排序 / 列显示 / 复制单据 / 来源单
- ✅ Hardening: 冲销/红字/作废/锁定/负库存 per-warehouse/多语言
- ✅ HR scope: 7 大模块 (业务员工 + 工资 + 考勤 + 培训 + 招聘 + 绩效 + 福利) — 全部 FUTURE
- ✅ Reuse: 18 个成熟模式 (Polymorphic Voucher / Lineage / Ledger Inventory / 多价格 / Push Config / 状态机)
- ✅ Modernize: 8 个 (per-resource 精度 / 多单位 / 编码 / DB / C/S 架构 / 协作)
- ✅ Reject: 16 个 (Borland / Access / BDE / 客户端直连 / 冗余字段 / NOLOCK)
- ✅ P0/P1/P2/Future/Reject 矩阵: 已分类
- ✅ 0 修改 Onlyit, 0 修改 GuliERP, 0 启动新 Goal, 0 commit / push / remote

**禁止启动下一个 GuliERP Goal**。停止。

---

## 附录 A: 文件清单 (本审计输出)

| 文件 | 路径 | 大小 | 用途 |
|---|---|---:|---|
| 主报告 | `D:\guli\projects\gulierp-next\docs\verification\ONLYIT_MATURE_ERP_REFERENCE_AUDIT_001.md` | ~33 KB | 完整审计 (本文件) |
| Schema Inventory | `D:\guli\projects\gulierp-next\docs\verification\ONLYIT_SCHEMA_INVENTORY_001.csv` | 23.2 KB | 639 张表 schema 总览 |
| Numeric Precision | `D:\guli\projects\gulierp-next\docs\verification\ONLYIT_NUMERIC_PRECISION_MATRIX_001.csv` | ~30 KB | 关键表 numeric 字段角色 |
| Hardening Matrix | `D:\guli\projects\gulierp-next\docs\verification\ONLYIT_PRODUCTION_HARDENING_MATRIX_001.md` | 单独文件 | Edge case 保护矩阵 |
| 配套证据 (不动) | `D:\guli\oit_setup\extracted\demo_db_full_dump.json` | 4.43 MB | Codex 抽取的完整 schema + data (READ-ONLY) |

## 附录 B: 重要观察 (GuliERP 对比)

| 维度 | Onlyit | GuliERP V1 (现状) | 差距 |
|---|---|---|---|
| 技术栈 | Borland + Access + ADO | .NET 10 + ASP.NET Core + PostgreSQL + EF Core | 巨大代差 |
| 部署 | C/S 桌面 + 局域网 app server | B/S Web SPA + 云原生 SaaS | 巨大代差 |
| 数据规模 | 单文件 13MB | PostgreSQL GB+ | N/A |
| 单据模型 | 162 types + 中心 Voucher | G3-R1B DocumentType enum + 独立表 | 概念 parallel |
| 库存模型 | Ledger-based (无 snapshot) | Snapshot-based (G3-R2A quantity) | 需 G3-R3A 切换 |
| 状态机 | state + check_user_id | Status enum (3 状态) | 需 V1.5+ 加 |
| HR 范围 | 7 大模块 (完整 HR) | 仅业务员工 (最小集) | 全部 FUTURE |
| 反过账 / 冲销 / 红字 | 完整 (VC/FX/CX/DY) | 无 | 需 V1.5+ 加 |
| 附件 / 图片 | BLOB 存 DB (edoc_doc, app_fs_file) | 仅 reference (无 file storage) | 需 V1.5+ 加 |
| 多币种 | money_type + evm_money_rate | 单币种 CNY | 需 V2+ 加 |
| 总账 | evm_* (完整 G/L) | 无 (G3-R1C 无 Finance) | 需 V2+ 加 |

停止。
