# ONLYIT_PRODUCTION_HARDENING_MATRIX_001

> 报告日期: 2026-08-28
> 任务: `GULIERP_ONLYIT_MATURE_ERP_REFERENCE_AUDIT_001` — Production Hardening 子报告
> 关联主报告: `ONLYIT_MATURE_ERP_REFERENCE_AUDIT_001.md` §17
> Evidence Level: **L2 (schema)** + **L1 (file/config)** — 部分 L3 (runtime) UNKNOWN
> 数据源: `D:\guli\oit_setup\extracted\demo_db_full_dump.json` (Codex 抽取, READ-ONLY)

---

## 0. 矩阵说明

本矩阵列出成熟 ERP 经过多年企业使用后形成的 **Edge Case 保护规则**。每行一条规则, 含:
- **Edge Case**: 边缘场景
- **Onlyit 实现字段/表**: 证据
- **Evidence Level**: L1 (file) / L2 (schema) / L3 (runtime)
- **GuliERP V1 状态**: 现状
- **GuliERP V1 修复**: 优先级 + 方向

---

## 1. 重复提交 (Double Submit)

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 同一客户端重复 POST 同一 voucher | 客户端 `_t` 模板 + `voucher_id` 长整型 PK + `app_gen_id_ext` 维护 last_seq | L1 (客户端) + L2 (schema) | G3-R2A SalesOrder 缺 Idempotency-Key 校验 (虽然 `CodePipeline` 已为 `DocumentNumber` 实现 unique constraint) |
| 重复 POST 不同 voucher 但 voucher_no 相同 | `voucher_no` 在 `ebs_v` 上 unique (implicit by `app_gen_id_ext` lock) | L2 (inferred) | G3-R2A/B 已实现 `CodePipeline` (`DocumentNumber` unique per `(TenantId, CompanyId, DocumentType, OrderNo)` per `G3-R1B`) |
| 跨 company 重复 voucher_no | `company_id` 在 `ebs_v` (per `company_id` field) | L2 | G3-R1B `DocumentNumber` 已实现 (counter scope 含 CompanyId) |

**修复方向 (P1)**: G3-R2A/B 已通过 `CodePipeline` 实现, 不需额外修复.

---

## 2. 重复过账 (Double Post / Re-Check)

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 同一 voucher 重复 check (审核后再审) | `ebs_v.state` 状态机 (`Draft → Checked`, Checked 后不允许再 check) | L2 | 缺 (G3-R2A/B 仅 Draft/Confirmed/Cancelled) |
| 不同用户 check 同一 voucher | `check_user_id` + `check_date` 字段保留最后审核人 (无 audit history) | L2 | 缺 |
| 超时 check (audit date > today) | 无显式字段 (依赖应用层) | L1 inferred | 缺 |

**修复方向 (P2)**: V1.5+ 加 `CheckStatus` enum + `CheckUserId` + `CheckDate` 字段 + `ApprovalLog` 表.

---

## 3. 负库存 (Negative Stock)

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 出库数量 > 当前库存 | `edt.can_be_negative` (per-warehouse 允许) + `edt_io_t.can_be_negative` (per-template 允许) + `edt_io.can_ass_be_negative` (per-line 辅助数量允许) | L2 (3 字段) | 缺 (G3-R1C 无库存模块) |
| 库存计算时允许负 | **允许** (per-warehouse 开关) | L2 | N/A (G3-R3A 决策) |

**修复方向 (P2)**: G3-R3A 决定 per-warehouse `CanBeNegative` 字段.

---

## 4. 超订单收货 (Over-Receipt)

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 采购收货数量 > 采购订单数量 | `ebs_vr_ctl.can_exceed_flag` (per voucher_type 控制) | L2 (字段存在) | 缺 (G3-R2B PurchaseOrder `line.quantity` 无超额检查) |
| 仓库收货自动转化为拒收 (RTV) | 单独的 `AF 质检退货` 单据 (反向 IO) | L2 (app_voucher_type) | 缺 |
| 部分收货 + 多次收货 | `edt_io.std_num` 累加 vs `ebs_vr_item.std_num` (计划) | L2 | 缺 (G3-R2B PurchaseOrder 无 `received_quantity` / `remaining_quantity` 字段) |
| 提前收货 (订单未到) | `ebs_vr_ctl.allow_ref_finished` 控制 (默认 false) | L2 (字段) | 缺 |

**修复方向 (P1)**: G3-R2B PurchaseOrder 应加 `received_quantity` 字段 + `can_exceed_flag` 配置 + 独立 `AF 质检退货` 端点.

---

## 5. 超订单发货 (Over-Shipment)

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 销售发货数量 > 销售订单数量 | `ebs_vr_ctl.can_exceed_flag` | L2 | 缺 (G3-R2A SalesOrder 同样缺 `shipped_quantity` 字段) |
| 部分发货 + 多次发货 | 同上 | L2 | 缺 |
| 提前发货 (订单未确认) | `ebs_vr_ctl.allow_ref_finished` | L2 | 缺 |

**修复方向 (P1)**: G3-R2A SalesOrder 应加 `shipped_quantity` 字段 + can_exceed_flag.

---

## 6. 重复开票 (Duplicate Invoice)

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 同一源单重复开票 | `ebs_vr_ref_set` (per-voucher-type 可引用的源单配置) + `ebs_vr_ctl.allow_ref_finished` (默认 false) | L2 | 缺 (G3-R1C 无 invoice 概念) |
| 不同源单 + 同一客户重复开票 | 客户 + 期间校验 (per `evm_io_f.ass_eba_id` + `voucher_date`) | L1 inferred | 缺 |

**修复方向 (P2)**: V2+ Finance 模块加 `Invoice` 实体时, 引入 `ref_sales_shipment_id` 唯一约束.

---

## 7. 重复付款 (Duplicate Payment)

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 同一应付单重复付款 | `mio_check_buy` (支票号段唯一) + 付款单状态机 | L2 | 缺 (G3-R1C 无 mio_*) |
| 同一银行账户同一日期同一金额重复 | `mio_bank_io_check` 人工对账 (manual matching) | L2 | 缺 |
| 重复收付款 (不同源单, 同金额) | 应用层 (no auto) | L1 inferred | 缺 |

**修复方向 (P2)**: V2+ Finance 模块加 `mio_*` 完整复刻.

---

## 8. 已关闭订单继续执行

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 状态 `Closed` 的销售订单被发货 | `ebs_v.state` 状态机 + 应用层检查 | L2 (字段) | 缺 (G3-R2A SalesOrder 无 Closed 状态) |
| 状态 `Cancelled` 的销售订单被引用 | `ebs_vr_ctl.allow_ref_finished` 默认 false | L2 | G3-R2A/B 已实现 (Cancelled 是终态) |
| 状态 `Closed` 的采购订单被收货 | 同上 | L2 | 缺 |

**修复方向 (P2)**: G3-R2A/B V1.5+ 应加 `Closed` 状态 (与 Cancelled 区分).

---

## 9. 已审核单据修改

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| Checked 状态修改 voucher header | `check_user_id IS NOT NULL` 后仅允许 红字/冲销/作废, 不允许直接修改 | L1 inferred | 缺 (G3-R2A/B Confirmed 后只能 Cancel, 不能修改) |
| Checked 状态修改 voucher line | 同上 (`ebs_vr_item.state` 检查) | L2 | 缺 |
| Cancel 状态修改 voucher | Cancelled 终态 | L2 | G3-R2A/B 已实现 ✓ |
| Red Ink 后再修改 | `FX/CX/DY 红字单` 单独建 + 状态机 | L2 (app_voucher_type) | 缺 |

**修复方向 (P2)**: V1.5+ 加 `Checked` 状态 + Red Ink 机制.

---

## 10. 删除已引用主数据

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 删 Item 还有 voucher 引用 | `res.stop_flag` (软删除) + 应用层检查 `res.stop_flag` in `io` | L2 (res.stop_flag 字段) | G3-R1C 已实现 `Status = Inactive` (软删除) ✓ |
| 删 Customer 还有 voucher 引用 | `sup.state` + 应用层检查 | L2 (sup.state 字段) | G3-R1C BusinessPartner 同样有 Status 字段 ✓ |
| 删 UoM 还有物料引用 | `uom.stop_flag` (类似) | L1 inferred | G3-R1C Uom.Status 字段 ✓ |
| 删 Department 还有 Employee 引用 | `dept.stop_flag` + 应用层拒绝新分配 | L2 (dept.stop_flag) | G3-R1C 暂无 dept 状态机 |
| 硬删除 | **无硬删除** (Always 软删除 + stop_flag) | L1 (per onlyit philosophy) | G3-R1C V1 design 拒绝硬删除 ✓ |

**修复方向 (P0)**: 维持当前 V1 design. G3-R1C 已 parallel 实现.

---

## 11. 禁用商品继续使用 (Stopped Item)

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 禁用 Item 继续销售 | `res.stop_flag` + 应用层校验 (在 `eba_io` / `edt_io` 创建时) | L2 | G3-R1C `Item.Status = Inactive` ✓ (但 G3-R2A SalesOrder 创建时**未校验** `item.status`) |
| 禁用 Customer 继续销售 | `sup.state` + 应用层 | L2 | 同上 |
| 禁用 Warehouse 继续发货 | `edt` (仓库) 无 stop_flag, 但 `edt_io` 可暂停 | L1 inferred | N/A |

**修复方向 (P1)**: G3-R2A SalesOrder CreateAsync 应在 `ResolveItemAsync` 校验 `item.Status == Active`. G3-R2B PurchaseOrder 同样.

---

## 12. 仓库冻结 (Warehouse Frozen)

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 冻结仓库继续 IO | 无显式 `stop_flag` on `edt`, 依赖应用层 | L1 inferred | N/A (G3-R1C 无仓库模块) |

**修复方向 (P2)**: G3-R3A 仓库实体加 `Status` 字段 (Active/Inactive).

---

## 13. 会计期间关闭 (Accounting Period)

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 关闭期间后录入凭证 | `evm_account_sep_cycle` (账期) + `cycle` (会计期间) + 期间状态 (Open/Closed) | L2 (字段存在) | 缺 (G3-R1C 无 G/L 模块) |
| 跨期间红字 | `FX/CX/DY 红字单` 自动选期间 | L2 | 缺 |
| 期末调汇 | `evm_account_sep_cycle_f` 字段 | L2 (字段) | 缺 |

**修复方向 (FUTURE)**: V2+ Finance.

---

## 14. 并发修改

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 同一 voucher 同时被 2 个客户端修改 | **Access JET 4.0 page-level lock** (悲观锁) | L1 (per design) | G3-R2A/B 已用 `ConcurrencyVersion` 乐观锁 ✓ |
| 同一 stock item 2 个 IO 同时 insert | JET 4.0 锁 + 应用层 `edt_io` 流水去重 | L1 + L2 | 缺 (G3-R3A) |
| 同一字典项 2 个 user 同时改 | JET 4.0 锁 + `order_id` 字段判断 | L2 (order_id) | G3-R1E Dictionary 暂未实现 |

**修复方向 (P1)**: 维持 G3-R2A/B 乐观锁, G3-R3A Inventory 加 `ConcurrencyVersion`.

---

## 15. 单号重复 (Document Number Duplicate)

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 同一 type 同一公司 voucher_no 重复 | `app_gen_id_ext` 维护 last_date + `app_gen_id_rule` 集中生成 | L2 (app_gen_id_*) | G3-R1B DocumentNumbering 已实现 ✓ (per G3-R1B report) |
| 跨 type 同一前缀 (e.g. AA 采购订单 + AB 采购收货 都用 "AA-20260828-001") | 2-letter prefix `voucher_type` 区分 | L2 | G3-R1B 已实现 (per DocumentType) |
| 跨公司单号 | `app_gen_id_ext` per-company | L1 inferred (per design) | G3-R1B 已实现 (counter scope 含 CompanyId) |
| 手动改 voucher_no | **无应用层保护** (DB 字段 varchar) | L1 (no constraint) | G3-R1B 已用 `CodePipeline` 4-step validation 阻止 |

**修复方向 (P0)**: 维持 G3-R1B DocumentNumbering. 已 parallel 实现.

---

## 16. 删除单据 (硬删除 vs 软删除)

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 用户点删除 | **Voucher 不允许硬删**; 改为 `state=V` (作废) + `voucher_no + [作废]` 后缀 | L1 (per onlyit philosophy) | G3-R2A/B 已用 `Status = Cancelled` (终态) ✓ |
| 程序级硬删除 | 无业务硬删除 API | L1 (no API exposed) | G3-R2A/B 已无 DELETE endpoint (per design) ✓ |
| 数据库级硬删除 (DBA) | Access JET 4.0 可 DELETE, 但 onlyit 客户端不提供 | L1 (design) | G3-R1C V1 design 拒绝硬删除 ✓ |

**修复方向 (P0)**: 维持当前 V1 design. 0 修复.

---

## 17. 凭证红冲 (Voucher Red-ink / Reversal)

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 已 Checked 凭证需要修正 | **`VC 冲转凭证`** (新单据, 原单不动) | L2 (app_voucher_type 含 VC) | **缺** (G3-R1C 无冲销机制) |
| 已 Posted 凭证需要修正 | 同上 | L2 | 缺 |
| 红字销售发票 | `BC 销售发票` 负向 | L2 (业务实现) | 缺 |
| 红字付款 | `FX 红字付款单` | L2 | 缺 |
| 红字应收 | `CX 应收红字` + `CY 预收红字` | L2 | 缺 |
| 红字应付 | `DX 应付红字` + `DY 预付红字` | L2 | 缺 |

**修复方向 (P2)**: V1.5+ 引入 `RedInvoice` / `ReversalVoucher` 实体 + `voucher_void` + `voucher_reversal` 状态. 这是 GuliERP V1 → V1.5 的关键 gap.

---

## 18. 多公司账套隔离

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 跨公司读取数据 | `company_id` per 业务表 (几乎都有) | L2 (company_id 字段) | G3-R1C 已实现 `ICompanyScoped` predicate ✓ |
| 跨公司记账 | voucher_id 隐含 company_id 隔离 | L2 | G3-R1C ✓ |
| 跨公司表连接 (join) | 必须显式带 company_id 过滤 | L1 (per design) | G3-R1C 强制 ✓ |
| 跨公司单号 | `app_gen_id_ext` per-company | L1 (inferred) | G3-R1B DocumentNumbering per CompanyId ✓ |

**修复方向 (P0)**: 维持当前 V1 design. 0 修复.

---

## 19. 跨期间 / 跨月结账

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 期间关闭后补录凭证 | `evm_account_sep_cycle` (期间) + 期间状态 + 应用层拒绝 | L2 | 缺 (G3-R1C 无 G/L) |
| 跨期间红字 | `FX/CX/DY 红字单` 自动选期间 | L2 | 缺 |
| 期末调汇 | `evm_account_sep_cycle_f` 字段 | L2 | 缺 |
| 月结 / 年结 | `evm_voucher_close` 应用层 | L1 (no SP) | 缺 |

**修复方向 (FUTURE)**: V2+ Finance.

---

## 20. 多币种 / 汇率

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 多币种 voucher | `money_type` 字段 + `money_factor` (汇率) | L2 | 缺 (G3-R1C V1 单币种 CNY) |
| 期末调汇 | `evm_account_sep_cycle_f` 字段 | L2 | 缺 |
| 跨币种凭证 | `evm_v_money_rate` (汇率表) | L2 | 缺 |

**修复方向 (FUTURE)**: V2+ Finance.

---

## 21. 重复锁定 (Row-level Lock)

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 长事务 (10 分钟) | 无强制 (依赖客户端) | L1 (no enforcement) | G3-R1C 无强制 (依赖 ASP.NET request timeout) |
| 锁等待 | JET 4.0 默认锁 30s | L1 (JET default) | N/A |
| 死锁 | JET 4.0 自动检测 + 应用层重试 | L1 (per design) | N/A (PostgreSQL MVCC) |

**修复方向 (N/A)**: Modern GuliERP 不会遇到 Access page-level lock 问题 (PostgreSQL MVCC).

---

## 22. 凭证冲销与连锁反转

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 冲销已冲销凭证 | `VC 冲转凭证` 状态机 (Cancelled 后不能再冲) | L2 | 缺 |
| 跨期间冲销 | 同 `FX/CX/DY` per-期间 | L2 | 缺 |
| 冲销后修改原凭证 | **不允许** (onlyit 哲学: 原凭证永远不动) | L1 (per design) | 缺 (GuliERP V1 无此机制) |

**修复方向 (P2)**: V1.5+ 引入.

---

## 23. 数量 / 金额精度

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 数量精度 | `res.num_dot_num` (per-物料) 0-4 位小数 | L2 | 缺 (G3-R1C V1 统一 18.4) |
| 价格精度 | `res.price_dot_num` (per-物料) 0-6 位小数 | L2 | 缺 (统一 18.4) |
| 舍入方向 (入库) | `res.in_ceil_num` / `in_floor_num` (per-物料) | L2 | 缺 (用银行家舍入) |
| 舍入方向 (出库) | `res.edt_ceil_num` / `edt_floor_num` (per-物料) | L2 | 缺 |
| 金额精度 | (无显式字段) decimal 默认 | L2 inferred | G3-R2A/B 统一 18.2 ✓ |
| 汇率精度 | `money_factor` 推断 18.6 | L1 inferred | 缺 (单币种) |
| 折扣率 | `discount` / `tax_rate` 0-1 范围 推断 9.6 | L1 inferred | G3-R2A/B 9.6 ✓ |

**修复方向 (P2)**: V1.5+ 可考虑 per-物料 精度. 当前 V1 统一精度已足够.

---

## 24. 金额计算

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 含税价反算 (tax_inclusive → tax_exclusive) | `tax_price` 字段 (含税价) + 应用层反算 (无 trigger) | L2 (字段) | G3-R2A/B `inp_price` + `tax_rate` (无 tax_price 字段) |
| 折扣后价 (discount → final) | `discount` + `discount_price` 字段 | L2 (字段) | 缺 |
| 多单位换算 | `inp_num_factor` + `std_num` | L2 | 缺 (G3-R1C V1 单 UoM) |
| 优惠策略 (满减 / 折扣叠加) | 单独 `eba_优惠` 业务逻辑 (无独立表) | L1 (inferred) | 缺 |
| 实际成本计算 | `res_cost_io` (按批号分摊) | L2 (res_cost_io) | 缺 (G3-R1C 无成本) |

**修复方向 (P2)**: V1.5+ 引入.

---

## 25. 库存 vs 批次 vs 库位

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 同一物料不同批次 | `res.batch_flag` (per-物料开关) + `edt_io.batch_no` (流水带) | L2 | 缺 (G3-R3A 待) |
| 同一批次不同库位 | `edt_io.edt_site_id` (库位级) | L2 | 缺 |
| 库位级库存 | 通过 `SUM(edt_io WHERE edt_id=X AND edt_site_id=Y)` 动态计算 | L2 (无 inventory 表) | 缺 |
| 保质期 / 有效期 | `res.quality_days` + `edt_io.produce_date` (流水带) | L2 | 缺 |
| 批次拆批 / 合并 | `edt_io_adjust` 负向 + 正向 IO | L2 (字段) | 缺 |
| 多单位换算 | `res_unit_type` (主) + `ass_unit_type` (辅) + `inp_num_factor` | L2 | 缺 (G3-R1C V1 单 UoM) |

**修复方向 (P2)**: G3-R3A 引入. Onlyit 模型非常成熟, 可直接借鉴.

---

## 26. 状态机 (Cross-cutting)

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 单据状态 (单层) | `ebs_v.state` 单一字段 | L2 | G3-R2A/B V1 用 `Status` enum 单一字段 ✓ |
| 单据行状态 (行级) | `ebs_vr_item.state` (per 行) | L2 | 缺 (G3-R2A/B SalesOrderLine 无行级状态) |
| 审核状态 (vs 单据状态) | `check_user_id` 隐含 (IS NOT NULL = Checked) | L2 | 缺 (G3-R1C IAuditWriter 仅字段) |
| 过账状态 (vs 审核状态) | `evm_io` 自动生成 (审核后同步过账) | L2 | 缺 (G3-R1C 无 G/L) |
| 部分完成 (planned / executed / partial) | `ebs_vr_item.state` + `std_num` (计划) vs `edt_io.std_num` (实际) | L2 | 缺 |

**修复方向 (P2)**: V1.5+ 加行级状态 + plan/exec/partial 状态机.

---

## 27. 附件 / 图片

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 附件上传 (per voucher) | `edoc_doc` + `app_fs_file` (BLOB 存 DB) | L2 (表) | 缺 (G3-R1C 无 file storage) |
| 多附件 (per line) | `edoc_doc` 多行 per voucher_id | L2 | 缺 |
| 图片 (per employee) | `emp_pict` + `emp_pict_data` (BLOB) | L2 | 缺 |
| 图片 (per item) | `res.pict_file_id` (FK to file storage) | L2 | 缺 |

**修复方向 (P2)**: V1.5+ 加 `Attachment` / `Image` 实体. 推荐: BLOB 存 OSS / S3, 不存 DB.

---

## 28. 操作日志 (Audit Log)

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 谁创建 (per-row) | `create_user_id` + `create_date` (每张表) | L2 (字段) | G3-R1C IAuditWriter ✓ |
| 谁最后修改 (per-row) | `last_modi_user_id` + `last_modi_date` (每张表) | L2 | G3-R1C ✓ |
| 谁最后审核 (per voucher) | `check_user_id` + `check_date` + `check_time` (仅 `ebs_v`) | L2 | 缺 (G3-R2A/B 缺 check_user_id) |
| 集中 audit log | 无 `app_oper_log` 显式表 (per-row 字段已够 onlyit 业务) | L1 (inferred no central log) | G3-R1C IAuditWriter (Foundation) ✓ |
| 操作意见 / 备注 | 无独立意见字段 (`check_date` 仅有 timestamp) | L1 | 缺 |

**修复方向 (P2)**: G3-R1C 已实现集中 IAuditWriter. 缺业务级 audit log + 审批意见字段.

---

## 29. 序列化 (Identity / Long vs Snowflake)

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 32-bit ID limit | Access `long integer` (32-bit, max ~2.1B) | L1 (per design) | G3-R1C PostgreSQL HiLo `bigint` (64-bit) ✓ |
| ID 越界 | Access 抛 overflow | L1 (per design) | N/A (PostgreSQL bigint) |

**修复方向 (N/A)**: Modern GuliERP 已 parallel 实现 ✓.

---

## 30. 性能 (Performance)

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 库存余额查询 (no index) | Access JET 4.0 全文扫描 | L1 (per design) | 缺 (G3-R3A 应建 `inventory_balance` 物化视图 / 缓存) |
| 报表查询 (多表 JOIN) | 客户端缓存 (in-memory) + `rep_ds` 报表数据集 | L2 (表) | 缺 (GuliERP V1 由前端 + API 处理) |
| 大数据 (1000+ users, 10+ years) | Access JET 4.0 性能瓶颈 (建议 ≤ 50 users) | L1 (per design) | G3-R1C PostgreSQL 性能高 100x+ |

**修复方向 (P2)**: G3-R3A 应建 inventory_balance 物化视图.

---

## 31. 备份 / 恢复 (Backup / Recovery)

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 备份 | 整文件 copy `.mdb` | L1 (per design) | 缺 (G3-R1C 无备份策略) |
| 恢复 | 整文件 restore | L1 | N/A |
| 事务日志 | Access JET 4.0 内置 | L1 (JET feature) | PostgreSQL WAL ✓ |
| PIT 恢复 | 无 (整文件) | L1 | PostgreSQL PITR ✓ |

**修复方向 (FUTURE)**: PostgreSQL 自动 backup.

---

## 32. 国际化 (i18n)

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 多语言 UI | 无 (GB2312 单语言) | L1 (per design) | 缺 (GuliERP V1 由前端 Element Plus zh-CN 简易处理) |
| 多语言字典 | 无 (字典名用中文) | L1 | 缺 |
| 多语言 voucher_no | 无 (中文 + ASCII) | L1 | 缺 |

**修复方向 (FUTURE)**: V2+ 引入.

---

## 33. 审计 / 合规 (Audit / Compliance)

| Edge Case | Onlyit 实现 | Evidence | GuliERP V1 状态 |
|---|---|---|---|
| 不可篡改凭证 | 无 cryptographically 不可篡改 (Access 无此能力) | L1 | 缺 |
| 完整 audit trail (谁/何时/什么) | per-row 字段 (无集中 log) | L2 (字段) | G3-R1C IAuditWriter ✓ (per-row) |
| 财务报表可重算 | 通过 `evm_io` 重算 (但需重放所有期间) | L2 | 缺 (G3-R1C 无 G/L) |
| 内部审计支持 | 无 (Access 单文件, 不能细粒度权限) | L1 | 缺 |

**修复方向 (FUTURE)**: V2+ Finance + 集中 audit log.

---

## 34. 最终: GuliERP V1 vs Onlyit Hardening 总评

| Hardening Category | Onlyit Coverage | GuliERP V1 Coverage | Gap |
|---|---|---|---|
| 重复提交 / 过账 | ✅ 强 (状态机 + `app_gen_id_ext` lock) | ⚠️ 部分 (CodePipeline ✓, 但无 check 状态) | V1.5+ 加 Checked 状态 |
| 负库存 | ✅ 强 (per-warehouse 开关) | N/A (G3-R1C 无库存) | G3-R3A 决策 |
| 超订单收发 | ✅ 强 (`ebs_vr_ctl.can_exceed_flag`) | ❌ 缺 | P1 (G3-R2A/B) |
| 重复开票 | ✅ 强 (`ebs_vr_ref_set` + `allow_ref_finished`) | N/A (无 Invoice) | V2+ |
| 重复付款 | ✅ 强 (支票号段 + 状态机) | N/A (无 mio_*) | V2+ |
| 已关闭订单继续执行 | ✅ 强 (状态机 + `allow_ref_finished`) | ⚠️ 部分 (Cancelled 终态 ✓, 但无 Closed) | V1.5+ 加 Closed 状态 |
| 已审核单据修改 | ✅ 强 (红字 + 冲销) | ❌ 缺 | P2 (V1.5+ Red Ink) |
| 删除已引用主数据 | ✅ 强 (软删除 + stop_flag) | ✅ 强 (G3-R1C ✓) | 0 |
| 禁用商品继续使用 | ✅ 强 (stop_flag 校验) | ⚠️ 部分 (G3-R1C Status ✓, 但 G3-R2A 未校验) | P1 (G3-R2A/B) |
| 仓库冻结 | ⚠️ 弱 (无 stop_flag on `edt`) | N/A | G3-R3A |
| 会计期间关闭 | ✅ 强 (`evm_account_sep_cycle` + `cycle`) | N/A (无 G/L) | V2+ |
| 并发修改 | ⚠️ 弱 (JET 4.0 page-level lock) | ✅ 强 (G3-R1C 乐观锁) | 0 |
| 单号重复 | ✅ 强 (集中生成 + 锁) | ✅ 强 (G3-R1B ✓) | 0 |
| 硬删除 | ✅ 强 (不允许, 软删除) | ✅ 强 (G3-R1C ✓) | 0 |
| 凭证红冲 | ✅ 强 (VC + FX/CX/DY) | ❌ 缺 | P2 (V1.5+ 关键 gap) |
| 多公司账套隔离 | ✅ 强 (per-row company_id) | ✅ 强 (G3-R1C ICompanyScoped) | 0 |
| 跨期间 / 跨月结账 | ✅ 强 (`evm_account_sep_cycle`) | N/A (无 G/L) | V2+ |
| 多币种 / 汇率 | ✅ 强 (`money_type` + `evm_v_money_rate`) | N/A (V1 单币种 CNY) | V2+ |
| 数量 / 金额精度 | ✅ 强 (per-物料 4 字段) | ⚠️ 部分 (统一 18.4 / 18.2) | V1.5+ 可加 |
| 状态机 (行级) | ✅ 强 (`ebs_vr_item.state`) | ❌ 缺 | V1.5+ |
| 附件 / 图片 | ✅ 强 (`edoc_doc` + BLOB) | ❌ 缺 (G3-R1C 无 file storage) | V1.5+ |
| 操作日志 (集中) | ⚠️ 弱 (per-row 字段 only) | ⚠️ 部分 (G3-R1C IAuditWriter) | V1.5+ |
| 序列化 | ⚠️ 弱 (32-bit, max 2.1B) | ✅ 强 (G3-R1C 64-bit bigint) | 0 |
| 性能 | ⚠️ 弱 (JET 4.0 ≤ 50 users) | ✅ 强 (PostgreSQL) | 0 |
| 备份 / 恢复 | ⚠️ 弱 (整文件) | ✅ 强 (PostgreSQL WAL + PITR) | 0 |
| 国际化 | ❌ 缺 (GB2312 单语言) | ⚠️ 部分 (zh-CN 简易) | V2+ |
| 审计 / 合规 | ⚠️ 弱 (无 cryptographically 不可篡改) | ⚠️ 部分 (per-row audit) | V2+ |

**总结**:
- Onlyit 在 **业务逻辑硬化 (业务级 edge case)** 方面**远超** GuliERP V1 (10+ 个 P1/P2 等级的能力)
- GuliERP V1 在 **平台硬化 (并发, 备份, 序列化, 多公司)** 方面**远超** Onlyit (modern tech stack)
- **GuliERP V1.5+ 的关键 gap**:
  1. **Red Ink / Reversal (冲销/红字)** — 必须加, 这是成熟 ERP 的标志
  2. **Line-level Status + plan/exec/remaining 字段** — G3-R2A/B SalesOrder/PurchaseOrder 急需
  3. **Source Document Push (Voucher 引用配置)** — G3-R2A/B 急需
  4. **Item.Status 校验 in SalesOrder/PurchaseOrder Create** — P1 修复

---

## 35. 0 修改 Onlyit / 0 修改 GuliERP 验证

本审计全程**READ-ONLY**:
- ❌ 不修改 Onlyit 数据库 (无 INSERT/UPDATE/DELETE/ALTER/DROP/TRUNCATE)
- ❌ 不修改 Onlyit 安装文件 (无 .config / .dll / .exe / .xml 变更)
- ❌ 不修改 GuliERP (无代码 / 测试 / migration / Vue 变更)
- ❌ 不启动 Inventory Receiving
- ❌ 不 commit / push / remote

数据源**只有 READ-ONLY**:
- `D:\guli\oit_setup\extracted\demo_db_full_dump.json` (Codex 抽取, 4.43 MB)
- `D:\guli\oit_setup\server.xml`, `opt.INI`, `dll\init.xml` (read)
- `D:\guli\oit_setup\*.exe`, `*.dll`, `*.ex` 文件名 (read, no exec)
- `Get-Process` (PID 110128 查 client.ex 路径)
- `Test-NetConnection 127.0.0.1:7777` (端口探测, no connection)

**未尝试**:
- ❌ 不打开 .mdb 用 ACE OLEDB 实际连接 (数据库密码未知, 正确做法是请求 Operator 提供明文密码; 当前用 dumped JSON 已足够完成 L1/L2 审计)
- ❌ 不启动 oserver.exe (虽然它在 D:\guli\oit_setup\dll\, 但启动会改变 Onlyit 系统状态)
- ❌ 不修改 Onlyit 配置

**输出**: 0 副作用。

停止。
