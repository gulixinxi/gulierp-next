using GuliERP.Crm.Application;
using GuliERP.Crm.Domain.Entities;
using GuliERP.Crm.Domain.Enums;
using GuliERP.Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GuliERP.Crm.Infrastructure.Services;

public class LeadService : ILeadService
{
    private readonly CrmDbContext _db;

    public LeadService(CrmDbContext db) => _db = db;

    public async Task<PagedResult<LeadDto>> ListAsync(LeadListQuery query, CancellationToken ct)
    {
        var q = _db.Leads.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            q = q.Where(x => x.CompanyName.Contains(kw) || x.ContactName!.Contains(kw) || x.Mobile!.Contains(kw));
        }
        if (query.Status.HasValue) q = q.Where(x => x.Status == query.Status.Value);
        if (query.City.HasValue) q = q.Where(x => x.City == query.City.Value);
        if (query.Industry.HasValue) q = q.Where(x => x.Industry == query.Industry.Value);
        if (query.Rating.HasValue) q = q.Where(x => x.Rating == query.Rating.Value);
        if (query.OwnerId.HasValue) q = q.Where(x => x.OwnerId == query.OwnerId.Value);
        if (query.Source.HasValue) q = q.Where(x => x.Source == query.Source.Value);
        if (query.SubjectType.HasValue) q = q.Where(x => x.SubjectType == query.SubjectType.Value);
        if (!string.IsNullOrWhiteSpace(query.Tag))
            q = q.Where(x => x.Tags.Contains(query.Tag));

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(x => x.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => ToDto(x))
            .ToListAsync(ct);

        return new PagedResult<LeadDto>(items, total, query.Page, query.PageSize);
    }

    public async Task<LeadDto?> GetByIdAsync(long id, CancellationToken ct)
    {
        var lead = await _db.Leads.FirstOrDefaultAsync(x => x.Id == id, ct);
        return lead is null ? null : ToDto(lead);
    }

    public async Task<LeadDto> CreateAsync(CreateLeadRequest request, long? userId, CancellationToken ct)
    {
        // 单条创建去重保护（与批量导入相同规则）
        var normalizedCreditCode = NormalizeCreditCode(request.CreditCode);
        var companyName = request.CompanyName.Trim();
        var domain = request.WebsiteDomain;
        var mobile = request.Mobile;

        var duplicate = await _db.Leads.FirstOrDefaultAsync(x =>
            (!string.IsNullOrEmpty(normalizedCreditCode) && x.CreditCode == normalizedCreditCode) ||
            (string.IsNullOrEmpty(normalizedCreditCode) && x.CompanyName == companyName) ||
            (string.IsNullOrEmpty(normalizedCreditCode) && !string.IsNullOrEmpty(domain) && x.WebsiteDomain == domain) ||
            (string.IsNullOrEmpty(normalizedCreditCode) && !string.IsNullOrEmpty(mobile) && x.Mobile == mobile), ct);

        if (duplicate != null)
        {
            // 已存在重复记录，返回已有记录，不创建新的
            return ToDto(duplicate);
        }

        var lead = new Lead
        {
            Id = SnowflakeId(),
            CompanyName = companyName,
            CreditCode = normalizedCreditCode,
            WebsiteDomain = request.WebsiteDomain,
            ContactName = request.ContactName,
            ContactPosition = request.ContactPosition,
            Mobile = request.Mobile,
            Email = request.Email,
            Wechat = request.Wechat,
            Phone = request.Phone,
            City = request.City,
            District = request.District,
            Address = request.Address,
            Industry = request.Industry ?? InferIndustry(companyName),
            Scale = request.Scale,
            SubjectType = InferSubjectType(companyName),
            Source = request.Source,
            SourceUrl = request.SourceUrl,
            CollectedAt = DateTimeOffset.UtcNow,
            ExternalKey = request.ExternalKey,
            ExternalSource = request.ExternalSource,
            Status = LeadStatus.Pending,
            Remarks = request.Remarks,
            Tags = request.Tags ?? new List<string>(),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = userId,
            ModifiedAt = DateTimeOffset.UtcNow,
            ModifiedBy = userId
        };

        // 自动计算AI评分
        lead.AiScore = CalculateScore(lead);
        lead.Rating = ScoreToRating(lead.AiScore.Value);
        lead.IsRatingSuggested = true;

        _db.Leads.Add(lead);
        await _db.SaveChangesAsync(ct);
        return ToDto(lead);
    }

    public async Task<LeadDto?> UpdateAsync(long id, UpdateLeadRequest request, CancellationToken ct)
    {
        var lead = await _db.Leads.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (lead is null) return null;

        lead.CompanyName = request.CompanyName.Trim();
        lead.CreditCode = NormalizeCreditCode(request.CreditCode);
        lead.WebsiteDomain = request.WebsiteDomain;
        lead.ContactName = request.ContactName;
        lead.ContactPosition = request.ContactPosition;
        lead.Mobile = request.Mobile;
        lead.Email = request.Email;
        lead.Wechat = request.Wechat;
        lead.Phone = request.Phone;
        lead.City = request.City;
        lead.District = request.District;
        lead.Address = request.Address;
        lead.Industry = request.Industry;
        lead.Scale = request.Scale;
        lead.Source = request.Source;
        lead.SourceUrl = request.SourceUrl;
        lead.Rating = request.Rating;
        if (request.Rating.HasValue) lead.IsRatingSuggested = false;
        lead.Remarks = request.Remarks;
        lead.ModifiedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return ToDto(lead);
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken ct)
    {
        var lead = await _db.Leads.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (lead is null) return false;
        _db.Leads.Remove(lead);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// 认领线索
    /// </summary>
    public async Task<bool> ClaimAsync(long id, long ownerId, CancellationToken ct)
    {
        var lead = await _db.Leads.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (lead is null || lead.Status != LeadStatus.Pending) return false;

        lead.Status = LeadStatus.Claimed;
        lead.OwnerId = ownerId;
        lead.ClaimedAt = DateTimeOffset.UtcNow;
        lead.ModifiedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// 废弃线索
    /// </summary>
    public async Task<bool> DiscardAsync(long id, CancellationToken ct)
    {
        var lead = await _db.Leads.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (lead is null) return false;

        lead.Status = LeadStatus.Discarded;
        lead.ModifiedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdateTagsAsync(long id, List<string> tags, CancellationToken ct)
    {
        var lead = await _db.Leads.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (lead is null) return false;

        lead.Tags = tags.Distinct().ToList();
        lead.ModifiedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> AssignAsync(long id, long ownerId, CancellationToken ct)
    {
        var lead = await _db.Leads.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (lead is null) return false;

        // 管理员分配：覆盖负责人，状态变为已认领（除非已转化/已废弃）
        if (lead.Status == LeadStatus.Pending)
        {
            lead.Status = LeadStatus.Claimed;
            lead.ClaimedAt = DateTimeOffset.UtcNow;
        }
        lead.OwnerId = ownerId;
        lead.ModifiedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// 转化为客户主档
    /// 1. 创建Company（客户主档）
    /// 2. 如果有联系人信息，自动创建Contact
    /// 3. 更新Lead状态为已转化，记录转化信息
    /// 4. 自动创建第一条跟进记录（"线索转化"）
    /// </summary>
    public async Task<CompanyDto> ConvertAsync(ConvertLeadRequest request, long? userId, CancellationToken ct)
    {
        var lead = await _db.Leads.FirstOrDefaultAsync(x => x.Id == request.LeadId, ct);
        if (lead is null)
            throw new InvalidOperationException($"线索 {request.LeadId} 不存在");
        if (lead.Status == LeadStatus.Converted)
            throw new InvalidOperationException($"线索 {request.LeadId} 已转化");

        // 1. 创建客户主档
        var company = new Company
        {
            Id = SnowflakeId(),
            CompanyNo = await GenerateCompanyNo(ct),
            FullName = request.FullName.Trim(),
            CreditCode = request.CreditCode,
            ShortName = request.ShortName,
            WebsiteDomain = lead.WebsiteDomain,
            City = request.City,
            District = request.District,
            Address = request.Address,
            Industry = request.Industry,
            Scale = request.Scale,
            Revenue = request.Revenue,
            HasWebsite = request.HasWebsite,
            WebsiteStatus = request.WebsiteStatus,
            Source = lead.Source,
            SourceUrl = lead.SourceUrl,
            CollectedAt = lead.CollectedAt,
            OwnerId = request.OwnerId ?? lead.OwnerId,
            Status = CustomerStatus.ToContact,
            Rating = lead.Rating,
            IsRatingSuggested = lead.IsRatingSuggested,
            ExternalKey = lead.ExternalKey,
            ExternalSource = lead.ExternalSource,
            Remarks = request.Remarks,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = userId,
            ModifiedAt = DateTimeOffset.UtcNow,
            ModifiedBy = userId
        };
        _db.Companies.Add(company);

        // 2. 自动创建联系人（如果线索中有联系人信息）
        if (!string.IsNullOrWhiteSpace(lead.ContactName))
        {
            var contact = new Contact
            {
                Id = SnowflakeId(),
                CrmCompanyId = company.Id,
                Name = lead.ContactName,
                Position = lead.ContactPosition,
                Mobile = lead.Mobile,
                Phone = lead.Phone,
                Wechat = lead.Wechat,
                Email = lead.Email,
                IsKeyPerson = true,
                CreatedAt = DateTimeOffset.UtcNow,
                ModifiedAt = DateTimeOffset.UtcNow
            };
            _db.Contacts.Add(contact);
        }

        // 3. 自动创建第一条跟进记录
        var activity = new Activity
        {
            Id = SnowflakeId(),
            CrmCompanyId = company.Id,
            FollowType = FollowType.Other,
            FollowAt = DateTimeOffset.UtcNow,
            Content = $"线索转化：从线索池「{lead.CompanyName}」转化为客户主档",
            NextAction = "首次联系客户",
            NextActionDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
            OwnerId = company.OwnerId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = userId,
            ModifiedAt = DateTimeOffset.UtcNow,
            ModifiedBy = userId
        };
        _db.Activities.Add(activity);

        // 4. 更新线索状态
        lead.Status = LeadStatus.Converted;
        lead.ConvertedToCompanyId = company.Id;
        lead.ConvertedAt = DateTimeOffset.UtcNow;
        lead.ConvertedBy = userId;
        lead.ModifiedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        return new CompanyDto(
            company.Id, company.CompanyNo, company.FullName, company.CreditCode, company.ShortName,
            company.WebsiteDomain, company.City, company.District, company.Address, company.Industry,
            company.Scale, company.Revenue, company.HasWebsite, company.WebsiteStatus, company.Source,
            company.SourceUrl, company.CollectedAt, company.OwnerId, company.Status, company.Rating,
            company.IsRatingSuggested, company.ExternalKey, company.ExternalSource, company.Remarks,
            company.Tags.ToList(), company.CreatedAt, company.CreatedBy);
    }

    /// <summary>
    /// 计算AI评分（可解释规则）
    /// </summary>
    public async Task<int> CalculateAiScoreAsync(long id, CancellationToken ct)
    {
        var lead = await _db.Leads.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (lead is null) return 0;

        var score = CalculateScore(lead);
        lead.AiScore = score;
        lead.Rating = ScoreToRating(score);
        lead.IsRatingSuggested = true;
        lead.ModifiedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return score;
    }

    /// <summary>
    /// 批量导入线索（带去重）
    /// </summary>
    public async Task<BatchImportResult> BatchImportAsync(IEnumerable<CreateLeadRequest> leads, long? userId, CancellationToken ct)
    {
        var successCount = 0;
        var skipCount = 0;
        var failCount = 0;
        var total = 0;

        foreach (var request in leads)
        {
            total++;
            // 去重规则（国内企业）：
            // 1. 统一社会信用代码（最权威，有就用这个）
            // 2. 公司全称精确匹配
            // 3. 官网域名（辅助）
            // 4. 手机号（辅助）
            var normalizedCreditCode = NormalizeCreditCode(request.CreditCode);
            var exists = await _db.Leads.AnyAsync(x =>
                (!string.IsNullOrEmpty(normalizedCreditCode) && x.CreditCode == normalizedCreditCode) ||
                (string.IsNullOrEmpty(normalizedCreditCode) && x.CompanyName == request.CompanyName.Trim()) ||
                (string.IsNullOrEmpty(normalizedCreditCode) && !string.IsNullOrEmpty(request.WebsiteDomain) && x.WebsiteDomain == request.WebsiteDomain) ||
                (string.IsNullOrEmpty(normalizedCreditCode) && !string.IsNullOrEmpty(request.Mobile) && x.Mobile == request.Mobile), ct);

            if (exists)
            {
                skipCount++;
                continue;
            }

            try
            {
                var lead = new Lead
                {
                    Id = SnowflakeId(),
                    CompanyName = request.CompanyName.Trim(),
                    CreditCode = normalizedCreditCode,
                    WebsiteDomain = request.WebsiteDomain,
                    ContactName = request.ContactName,
                    ContactPosition = request.ContactPosition,
                    Mobile = request.Mobile,
                    Email = request.Email,
                    Wechat = request.Wechat,
                    Phone = request.Phone,
                    City = request.City,
                    District = request.District,
                    Address = request.Address,
                    Industry = request.Industry,
                    Scale = request.Scale,
                    Source = request.Source,
                    SourceUrl = request.SourceUrl,
                    CollectedAt = DateTimeOffset.UtcNow,
                    ExternalKey = request.ExternalKey,
                    ExternalSource = request.ExternalSource,
                    Status = LeadStatus.Pending,
                    Remarks = request.Remarks,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = userId,
                    ModifiedAt = DateTimeOffset.UtcNow,
                    ModifiedBy = userId
                };

                lead.AiScore = CalculateScore(lead);
                lead.Rating = ScoreToRating(lead.AiScore.Value);
                lead.IsRatingSuggested = true;

                _db.Leads.Add(lead);
                successCount++;
            }
            catch
            {
                failCount++;
            }
        }

        await _db.SaveChangesAsync(ct);
        return new BatchImportResult(total, successCount, skipCount, failCount);
    }

    #region 私有方法

    private static int CalculateScore(Lead lead)
    {
        int score = 0;

        // 有明确联系人 +20
        if (!string.IsNullOrWhiteSpace(lead.ContactName)) score += 20;

        // 有手机号 +15
        if (!string.IsNullOrWhiteSpace(lead.Mobile)) score += 15;

        // 有官网 +15
        if (!string.IsNullOrWhiteSpace(lead.WebsiteDomain)) score += 15;

        // 有邮箱 +10
        if (!string.IsNullOrWhiteSpace(lead.Email)) score += 10;

        // 所在地市=潍坊（本地服务能力最强）+15
        if (lead.City == CompanyCity.Weifang) score += 15;

        // 企业规模≥101人 +10
        if (lead.Scale is CompanyScale.Large or CompanyScale.XLarge or CompanyScale.XXLarge) score += 10;

        // 来源=转介绍 +15
        if (lead.Source == SourceChannel.Referral) score += 15;

        // 有"百度推广"标签（有付费习惯，高意向）+20
        if (lead.Tags != null && lead.Tags.Contains("百度推广")) score += 20;

        // 有"重点客户"标签 +10
        if (lead.Tags != null && lead.Tags.Contains("重点客户")) score += 10;

        return Math.Min(score, 100);
    }

    private static IntentRating ScoreToRating(int score)
    {
        return score >= 70 ? IntentRating.A :
               score >= 50 ? IntentRating.B :
               score >= 30 ? IntentRating.C :
               IntentRating.D;
    }

    private static LeadDto ToDto(Lead x) => new(
        x.Id, x.CompanyName, x.CreditCode, x.WebsiteDomain, x.ContactName, x.ContactPosition,
        x.Mobile, x.Email, x.Wechat, x.Phone, x.City, x.District, x.Address,
        x.Industry, x.Scale, x.SubjectType, x.Source, x.SourceUrl, x.CollectedAt, x.ExternalKey,
        x.ExternalSource, x.Status, x.AiScore, x.Rating, x.IsRatingSuggested,
        x.OwnerId, x.ClaimedAt, x.ConvertedToCompanyId, x.ConvertedAt, x.ConvertedBy,
        x.Remarks, x.Tags.ToList(), x.CreatedAt, x.CreatedBy);

    /// <summary>
    /// 统一社会信用代码标准化：去空格、转大写
    /// </summary>
    private static string? NormalizeCreditCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        return code.Trim().ToUpperInvariant();
    }

    private async Task<string> GenerateCompanyNo(CancellationToken ct)
    {
        var date = DateTime.Now.ToString("yyyyMMdd");
        var count = await _db.Companies.CountAsync(x => x.CompanyNo.StartsWith($"CO{date}"), ct);
        return $"CO{date}{(count + 1):D4}";
    }

    private static long SnowflakeId() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000 + Random.Shared.Next(1000);

    /// <summary>
    /// 根据企业名称关键词自动推断行业
    /// 优先级：食品 > 机械 > 纺织 > 化工 > 建材 > 农业 > 商贸 > 服务 > 其他
    /// </summary>
    private static CompanyIndustry InferIndustry(string companyName)
    {
        if (string.IsNullOrEmpty(companyName)) return CompanyIndustry.Other;

        var rules = new (string[] Keywords, CompanyIndustry Industry)[]
        {
            (new[] { "食品", "餐饮", "粮油", "饮料", "调味", "肉食", "水产", "乳业", "烘焙", "糖果", "茶叶", "农产品加工" }, CompanyIndustry.Food),
            (new[] { "机械", "设备", "重工", "机床", "泵阀", "轴承", "齿轮", "电机", "模具", "五金", "铸造", "锻造" }, CompanyIndustry.Machinery),
            (new[] { "纺织", "服装", "印染", "面料", "纱线", "家纺", "针织", "制衣" }, CompanyIndustry.Textile),
            (new[] { "化工", "化学", "涂料", "塑料", "橡胶", "化肥", "农药", "新材料", "精细化工" }, CompanyIndustry.Chemical),
            (new[] { "建材", "水泥", "陶瓷", "玻璃", "石材", "保温", "装饰", "门窗", "管材" }, CompanyIndustry.BuildingMaterials),
            (new[] { "农业", "种植", "养殖", "苗木", "果蔬", "畜牧", "饲料", "种业" }, CompanyIndustry.Agriculture),
            (new[] { "贸易", "商贸", "进出口", "经销", "供应链" }, CompanyIndustry.Trading),
            (new[] { "服务", "咨询", "物流", "科技", "网络", "传媒", "广告", "软件", "信息" }, CompanyIndustry.Service),
        };

        foreach (var rule in rules)
        {
            if (rule.Keywords.Any(k => companyName.Contains(k)))
                return rule.Industry;
        }
        return CompanyIndustry.Other;
    }

    /// <summary>
    /// 根据企业名称关键词推断主体类型
    /// 优先级：政府 > 事业单位 > 协会 > 专业服务 > 服务机构 > 园区 > 企业
    /// </summary>
    private static SubjectType InferSubjectType(string companyName)
    {
        if (string.IsNullOrEmpty(companyName)) return SubjectType.Other;

        // 政府机关（优先级最高，避免"XX局有限公司"误判）
        var govKeywords = new[] { "人民政府", "政府办公室", "局", "委员会", "街道办", "乡政府", "镇政府", "管委会", "管理委员会", "办公厅", "办事处" };
        if (govKeywords.Any(k => companyName.Contains(k)) && !companyName.Contains("有限公司") && !companyName.Contains("公司"))
            return SubjectType.Government;

        // 学历教育（幼儿园/小学/中学/大学/学院等，AI工具目标客户）
        var eduKeywords = new[] { "幼儿园", "小学", "初中", "高中", "中学", "大学", "学院", "技师学院", "职业学院", "大专", "中专", "技校", "学校" };
        if (eduKeywords.Any(k => companyName.Contains(k)) && !companyName.Contains("有限公司") && !companyName.Contains("公司"))
            return SubjectType.Education;

        // 教育培训（托管/晚托/教培/督导/培训机构，教育产品目标客户）
        var trainKeywords = new[] { "托管", "晚托", "教培", "培训学校", "培训机构", "培训中心", "教育培训", "教育咨询", "教育科技", "教育服务", "教育发展", "督导", "研学", "早教", "亲子", "益智" };
        if (trainKeywords.Any(k => companyName.Contains(k)))
            return SubjectType.Training;

        // 事业单位（医院/博物馆/研究院等，非目标）
        var pubKeywords = new[] { "医院", "卫生院", "博物馆", "烈士陵园", "研究院", "研究所", "图书馆", "文化馆", "体育馆", "电视台", "报社", "疾控中心", "卫生监督" };
        if (pubKeywords.Any(k => companyName.Contains(k)) && !companyName.Contains("有限公司") && !companyName.Contains("公司"))
            return SubjectType.PublicInstitution;

        // 协会/商会/联合会
        var assocKeywords = new[] { "协会", "商会", "联合会", "促进会", "学会", "研究会", "联盟", "同业公会" };
        if (assocKeywords.Any(k => companyName.Contains(k)))
            return SubjectType.Association;

        // 专业服务机构（会计师/律师/代理记账/咨询）
        var profKeywords = new[] { "会计师事务所", "律师事务所", "税务师事务所", "代理记账", "财务咨询", "企业管理咨询", "管理咨询", "咨询有限公司", "咨询服务有限公司", "知识产权代理", "商标代理", "审计" };
        if (profKeywords.Any(k => companyName.Contains(k)))
            return SubjectType.ProfessionalService;

        // 服务中心/创业中心
        var svcKeywords = new[] { "服务中心", "活动中心", "交流中心", "创业中心", "孵化中心", "技术中心" };
        if (svcKeywords.Any(k => companyName.Contains(k)) && !companyName.Contains("有限公司"))
            return SubjectType.ServiceOrg;

        // 园区/基地
        var parkKeywords = new[] { "产业园", "工业园", "科技园", "创业园", "孵化器", "基地", "物流园", "电商园" };
        if (parkKeywords.Any(k => companyName.Contains(k)) && !companyName.Contains("有限公司") && !companyName.Contains("公司"))
            return SubjectType.Park;

        // 个体商户/门店（餐饮/零售/服务门店，POI地图标注目标客户）
        var indiKeywords = new[] { "饭店", "饭馆", "餐饮店", "餐厅", "餐馆", "小吃", "面馆", "火锅店", "烧烤", "奶茶", "咖啡", "烘焙", "蛋糕", "超市", "便利店", "商店", "商行", "经营部", "工作室", "理发店", "美容院", "美甲", "足浴", "按摩", "维修部", "五金店", "服装店", "鞋店", "药店", "诊所", "宠物店", "花店", "水果店", "蔬菜店", "肉店", "粮油", "烟酒", "宾馆", "旅馆", "民宿", "网吧", "棋牌室", "健身房", "游泳馆", "停车场", "加油站", "洗车", "汽车维修", "电动车", "手机店", "电器店", "家具店", "建材店", "农资", "种子", "化肥", "饲料", "兽医", "屠宰", "豆腐坊", "磨坊", "作坊", "加工厂", "经营店", "批发部", "经销处", "服务部", "维修中心", "美食店", "饺子馆", "包子铺", "粥铺", "快餐店", "大排档", "卤味", "熟食", "生鲜", "母婴", "童装", "眼镜店", "钟表店", "打字复印", "广告制作", "装饰装修", "家政", "保洁", "搬家", "开锁", "管道疏通" };
        if (indiKeywords.Any(k => companyName.Contains(k)) && !companyName.Contains("有限公司") && !companyName.Contains("公司") && !companyName.Contains("集团"))
            return SubjectType.IndividualBusiness;

        // 默认企业
        return SubjectType.Enterprise;
    }

    #endregion
}
