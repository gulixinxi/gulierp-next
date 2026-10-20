namespace GuliERP.Crm.Domain.Enums;

/// <summary>
/// 决策角色
/// </summary>
public enum DecisionRole
{
    DecisionMaker = 0,  // 决策人
    Influencer = 1,     // 影响者
    User = 2,           // 使用者
    Contact = 3         // 对接人
}

/// <summary>
/// 业务品类
/// </summary>
public enum BusinessCategory
{
    WebsiteBuild = 0,   // 企业建站
    WebsiteOpt = 1,     // 网站优化
    AIApp = 2,          // AI应用
    GEOOpt = 3,         // GEO优化
    DigitalEmployee = 4,// 数字员工
    PoiAnnotation = 5,  // 地图标注（POI信息完善）
    DouyinAddress = 6,  // 抖音/快手地址添加
    StoreInfoComplete = 7, // 门店信息完善（多平台同步）
    Other = 99          // 其他
}

/// <summary>
/// 商机阶段
/// </summary>
public enum DealStage
{
    InitialContact = 0, // 初步接触
    Requirement = 1,    // 需求确认
    Proposal = 2,       // 方案报价
    Negotiation = 3,    // 商务谈判
    Won = 4,            // 成交
    Lost = 5            // 丢单
}

/// <summary>
/// 商机状态
/// </summary>
public enum DealStatus
{
    InProgress = 0, // 进行中
    Won = 1,        // 已成交
    Lost = 2,       // 已丢单
    Paused = 3      // 暂搁
}

/// <summary>
/// 丢单原因
/// </summary>
public enum LostReason
{
    Price = 0,          // 价格
    Competitor = 1,     // 竞品
    NoBudget = 2,       // 无预算
    BadTiming = 3,      // 时机不对
    NoContact = 4,      // 联系不上
    Other = 99          // 其他
}

/// <summary>
/// 跟进方式
/// </summary>
public enum FollowType
{
    Phone = 0,      // 电话
    Wechat = 1,     // 微信
    Visit = 2,      // 拜访
    Email = 3,      // 邮件
    Meeting = 4,    // 会议
    Other = 99      // 其他
}

/// <summary>
/// 客户态度
/// </summary>
public enum CustomerAttitude
{
    Positive = 0,   // 积极
    Neutral = 1,    // 中性
    Negative = 2,   // 消极
    Unclear = 3     // 未明确
}

/// <summary>
/// 操作类型（导入导出日志）
/// </summary>
public enum OperationType
{
    Import = 0,         // 导入
    Export = 1,         // 导出
    BatchDelete = 2,    // 批量删除
    BatchReassign = 3   // 批量改派
}

/// <summary>
/// 日志状态
/// </summary>
public enum LogStatus
{
    Success = 0,        // 成功
    PartialFailed = 1,  // 部分失败
    Failed = 2          // 失败
}
