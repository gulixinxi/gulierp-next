namespace GuliERP.Crm.Domain.Enums;

/// <summary>
/// 所在地市
/// </summary>
public enum CompanyCity
{
    Weifang = 0,        // 潍坊
    Qingdao = 1,        // 青岛
    Yantai = 2,         // 烟台
    Jinan = 3,          // 济南
    Zibo = 4,           // 淄博
    Linyi = 5,          // 临沂
    ShandongOther = 6,  // 山东其他
    OutOfProvince = 99  // 省外
}

/// <summary>
/// 所属行业
/// </summary>
public enum CompanyIndustry
{
    Machinery = 0,          // 机械制造
    Textile = 1,            // 纺织服装
    Food = 2,               // 食品加工
    Chemical = 3,           // 化工
    BuildingMaterials = 4,  // 建材
    Agriculture = 5,        // 农业种植
    Trading = 6,            // 商贸流通
    Service = 7,            // 服务业
    Other = 99              // 其他
}

/// <summary>
/// 企业规模
/// </summary>
public enum CompanyScale
{
    Small = 0,      // ≤20人
    Medium = 1,     // 21-100人
    Large = 2,      // 101-300人
    XLarge = 3,     // 301-1000人
    XXLarge = 4,    // >1000人
    Unknown = 99    // 未知
}

/// <summary>
/// 年营收档位
/// </summary>
public enum CompanyRevenue
{
    Under5M = 0,        // ≤500万
    Between5M20M = 1,   // 500-2000万
    Between20M100M = 2, // 2000万-1亿
    Over100M = 3,       // >1亿
    Unknown = 99        // 未知
}

/// <summary>
/// 官网状态
/// </summary>
public enum WebsiteStatus
{
    Normal = 0,         // 正常
    CannotOpen = 1,     // 打不开
    NoHttps = 2,        // 无HTTPS
    MobileUnusable = 3, // 移动端不可用
    Outdated = 4,       // 内容陈旧
    NoWebsite = 5       // 无官网
}

/// <summary>
/// 来源渠道
/// </summary>
public enum SourceChannel
{
    Excel = 0,          // 存量Excel
    Government = 1,     // 政府公示
    Association = 2,    // 行业协会
    Bidding = 3,        // 招投标
    Map = 4,            // 地图
    SocialMedia = 5,    // 自媒体
    Referral = 6,       // 转介绍
    Other = 99          // 其他
}

/// <summary>
/// 主体类型（区分企业和非企业组织）
/// 企业=直接客户；协会/服务机构/园区/专业服务=渠道机会；教育/教培=AI工具及教育产品目标客户；政府/事业单位=非目标
/// </summary>
public enum SubjectType
{
    Enterprise = 0,     // 企业（公司/厂/店/个体工商户）
    Association = 1,    // 协会/商会/联合会/促进会（渠道机会）
    ServiceOrg = 2,     // 服务中心/创业中心（渠道机会）
    ProfessionalService = 3, // 会计师事务所/律师事务所/代理记账/咨询公司（渠道机会）
    Park = 4,           // 园区/管委会/基地（渠道机会）
    Government = 5,     // 政府机关（非目标）
    PublicInstitution = 6, // 事业单位：医院/博物馆/研究院（非目标）
    Education = 7,      // 学历教育：幼儿园/小学/初中/高中/大学/学院/技师学院（AI工具目标客户）
    Training = 8,       // 教育培训：托管/晚托/教培/督导/培训机构（教育产品目标客户）
    IndividualBusiness = 10, // 个体商户/门店：餐饮/零售/服务门店（POI地图标注目标客户）
    Other = 99          // 其他
}

/// <summary>
/// 线索池状态（Lead表用）
/// </summary>
public enum LeadStatus
{
    Pending = 0,    // 待认领
    Claimed = 1,    // 已认领
    Converted = 2,  // 已转化
    Discarded = 3   // 已废弃
}

/// <summary>
/// 客户主档状态（Company表用）
/// </summary>
public enum CustomerStatus
{
    ToContact = 0,  // 待联系
    Following = 1,  // 跟进中
    Quoted = 2,     // 已报价
    Deal = 3,       // 成交
    Paused = 4,     // 暂搁
    Invalid = 5     // 无效
}

/// <summary>
/// 意向评级
/// </summary>
public enum IntentRating
{
    A = 0,
    B = 1,
    C = 2,
    D = 3
}

/// <summary>
/// 企业情报信号类型
/// </summary>
public enum SignalType
{
    WebsiteAbnormal = 0,     // 网站异常/无法访问
    WebsiteMissing = 1,      // 官网缺失
    GeoChange = 2,           // GEO表现变化
    IcpChange = 3,           // ICP/工商信息变动
    LicenseExpiring = 4,     // 许可证到期
    Penalty = 5,             // 政府处罚/行政公示
    Bidding = 6,             // 招标/中标/采购机会
    Policy = 7,              // 政策/奖励/补贴
    Hiring = 8,              // 招聘/业务扩张
    ForeignTrade = 9,        // 外贸相关公开信息
    PoiMissing = 10,         // POI信息缺失（地图标注不完整）
    SuspectedDelisted = 11,  // 疑似被地图平台下架
    AnnouncementAlert = 12,  // 整顿公告（被监管部门通报）
    Other = 99               // 其他
}

/// <summary>
/// 信号处理状态
/// </summary>
public enum SignalStatus
{
    Pending = 0,    // 待处理
    Confirmed = 1,  // 已确认（有效信号）
    Ignored = 2,    // 已忽略
    Converted = 3   // 已转商机
}

/// <summary>
/// 证据验证状态
/// </summary>
public enum VerificationStatus
{
    NotQueried = 0,     // 未查询
    ConfirmedExist = 1, // 已确认存在
    ConfirmedAbsent = 2,// 已确认不存在
    NotFound = 3,       // 未查到
    Outdated = 4,       // 数据过期
    NeedManual = 5      // 需人工核实
}
