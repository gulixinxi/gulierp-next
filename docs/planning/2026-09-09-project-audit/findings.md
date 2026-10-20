# 审计发现

- 基线 master / 042558d，34 修改、3 删除、54 未跟踪（创建本次文件前）；已有 WIP 保留。
- V1 冻结范围要求 InventoryPostingEngine；库存目前只有 README 占位。
- README .NET8/JWT/SQLServer+PostgreSQL 与实际 net10.0/Cookie/Npgsql 不符；采购已有实现。
- npm run build 通过；7 个测试项目共639项，637通过，2失败。
- API 测试失败是旧 UI 标题断言；MDM 混入真实数据库 seed 测试，打印连接串，EventLog权限错误阻断。需先隔离和脱敏，不以提权绕过。
- 数值精度已有 VERIFIED 报告，不重复规划从零研究；行级溯源、过账冲销仍需核实冻结。
- 未找到项目 .github/workflows 或 Docker/compose 文件，与 README 快速启动承诺存在缺口。
