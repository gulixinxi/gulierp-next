using GuliERP.Crm.Application;
using GuliERP.Crm.Domain.Enums;
using GuliERP.Crm.Infrastructure.Services;
using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GuliERP.Api.Crm;

public static class CrmEndpoints
{
    public static IEndpointRouteBuilder MapCrmEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/crm").WithTags("CRM");

        // ===== 团队成员（供分配/负责人显示）=====
        group.MapGet("/team-members", async (UserManager<GuliErpUser> userManager, ICurrentTenant currentTenant) =>
        {
            if (currentTenant.Id is not long tenantId || tenantId <= 0)
                return Results.Forbid();

            var users = userManager.Users
                .Where(u => u.TenantId == tenantId && u.Status == UserStatus.Active)
                .OrderBy(u => u.Id)
                .Select(u => new { id = u.Id, userName = u.UserName, displayName = u.DisplayName })
                .ToList();
            return Results.Ok(users);
        }).RequireAuthorization(CrmPolicies.LeadRead);

        // ===== 线索池（Lead）=====
        var leads = group.MapGroup("/leads");

        leads.MapGet("", async (
                [FromQuery] string? keyword,
                [FromQuery] LeadStatus? status,
                [FromQuery] CompanyCity? city,
                [FromQuery] CompanyIndustry? industry,
                [FromQuery] IntentRating? rating,
                [FromQuery] long? ownerId,
                [FromQuery] SourceChannel? source,
                [FromQuery] SubjectType? subjectType,
                [FromQuery] string? tag,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                ILeadService svc,
                CancellationToken ct) =>
            {
                var query = new LeadListQuery(keyword, status, city, industry, rating, ownerId, source, subjectType, tag, page ?? 1, pageSize ?? 20);
                return Results.Ok(await svc.ListAsync(query, ct));
            })
            .RequireAuthorization(CrmPolicies.LeadRead);

        leads.MapGet("/{id:long}", async (long id, ILeadService svc, CancellationToken ct) =>
            {
                var dto = await svc.GetByIdAsync(id, ct);
                return dto is null ? Results.NotFound() : Results.Ok(dto);
            })
            .RequireAuthorization(CrmPolicies.LeadRead);

        leads.MapPost("", async (
                [FromBody] CreateLeadRequest request,
                HttpContext http,
                ILeadService svc,
                CancellationToken ct) =>
            {
                var userId = GetUserId(http);
                var created = await svc.CreateAsync(request, userId, ct);
                return Results.Created($"/api/v1/crm/leads/{created.Id}", created);
            })
            .RequireAuthorization(CrmPolicies.LeadManage);

        leads.MapPut("/{id:long}", async (
                long id,
                [FromBody] UpdateLeadRequest request,
                ILeadService svc,
                CancellationToken ct) =>
            {
                var updated = await svc.UpdateAsync(id, request, ct);
                return updated is null ? Results.NotFound() : Results.Ok(updated);
            })
            .RequireAuthorization(CrmPolicies.LeadManage);

        leads.MapDelete("/{id:long}", async (long id, ILeadService svc, CancellationToken ct) =>
            {
                var ok = await svc.DeleteAsync(id, ct);
                return ok ? Results.NoContent() : Results.NotFound();
            })
            .RequireAuthorization(CrmPolicies.LeadManage);

        // 认领
        leads.MapPost("/{id:long}/claim", async (
                long id,
                HttpContext http,
                ILeadService svc,
                CancellationToken ct) =>
            {
                var userId = GetUserId(http);
                if (!userId.HasValue) return Results.Unauthorized();
                var ok = await svc.ClaimAsync(id, userId.Value, ct);
                return ok ? Results.NoContent() : Results.BadRequest("线索不存在或已被认领");
            })
            .RequireAuthorization(CrmPolicies.LeadClaim);

        // 管理员分配线索给指定成员
        leads.MapPost("/{id:long}/assign", async (
                long id,
                [FromBody] long ownerId,
                ILeadService svc,
                UserManager<GuliErpUser> userManager,
                CancellationToken ct) =>
            {
                var target = await userManager.FindByIdAsync(ownerId.ToString());
                if (target is null || target.Status != UserStatus.Active)
                    return Results.BadRequest("指定成员不存在或已停用");
                var ok = await svc.AssignAsync(id, ownerId, ct);
                return ok ? Results.NoContent() : Results.BadRequest("线索不存在");
            })
            .RequireAuthorization(CrmPolicies.LeadManage);

        // 废弃
        leads.MapPost("/{id:long}/discard", async (long id, ILeadService svc, CancellationToken ct) =>
            {
                var ok = await svc.DiscardAsync(id, ct);
                return ok ? Results.NoContent() : Results.NotFound();
            })
            .RequireAuthorization(CrmPolicies.LeadManage);

        // 更新标签
        leads.MapPut("/{id:long}/tags", async (
                long id,
                [FromBody] List<string> tags,
                ILeadService svc,
                CancellationToken ct) =>
            {
                var ok = await svc.UpdateTagsAsync(id, tags, ct);
                return ok ? Results.NoContent() : Results.NotFound();
            })
            .RequireAuthorization(CrmPolicies.LeadManage);

        // 转化为客户主档
        leads.MapPost("/{id:long}/convert", async (
                long id,
                [FromBody] ConvertLeadRequest request,
                HttpContext http,
                ILeadService svc,
                CancellationToken ct) =>
            {
                var userId = GetUserId(http);
                var req = request with { LeadId = id };
                var company = await svc.ConvertAsync(req, userId, ct);
                return Results.Ok(company);
            })
            .RequireAuthorization(CrmPolicies.LeadConvert);

        // 重新计算AI评分
        leads.MapPost("/{id:long}/score", async (long id, ILeadService svc, CancellationToken ct) =>
            {
                var score = await svc.CalculateAiScoreAsync(id, ct);
                return Results.Ok(new { score });
            })
            .RequireAuthorization(CrmPolicies.LeadManage);

        // 批量导入
        leads.MapPost("/batch-import", async (
                [FromBody] IEnumerable<CreateLeadRequest> requests,
                HttpContext http,
                ILeadService svc,
                CancellationToken ct) =>
            {
                var userId = GetUserId(http);
                var result = await svc.BatchImportAsync(requests, userId, ct);
                return Results.Ok(result);
            })
            .RequireAuthorization(CrmPolicies.LeadImport);

        // ===== 客户主档（Company）=====
        var companies = group.MapGroup("/companies");

        companies.MapGet("", async (
                [FromQuery] string? keyword,
                [FromQuery] CustomerStatus? status,
                [FromQuery] CompanyCity? city,
                [FromQuery] CompanyIndustry? industry,
                [FromQuery] IntentRating? rating,
                [FromQuery] long? ownerId,
                [FromQuery] bool? hasWebsite,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                ICompanyService svc,
                CancellationToken ct) =>
            {
                var query = new CompanyListQuery(keyword, status, city, industry, rating, ownerId, hasWebsite, page ?? 1, pageSize ?? 20);
                return Results.Ok(await svc.ListAsync(query, ct));
            })
            .RequireAuthorization(CrmPolicies.CompanyRead);

        companies.MapGet("/{id:long}", async (long id, ICompanyService svc, CancellationToken ct) =>
            {
                var dto = await svc.GetByIdAsync(id, ct);
                return dto is null ? Results.NotFound() : Results.Ok(dto);
            })
            .RequireAuthorization(CrmPolicies.CompanyRead);

        companies.MapPost("", async (
                [FromBody] CreateCompanyRequest request,
                HttpContext http,
                ICompanyService svc,
                CancellationToken ct) =>
            {
                var userId = GetUserId(http);
                var created = await svc.CreateAsync(request, userId, ct);
                return Results.Created($"/api/v1/crm/companies/{created.Id}", created);
            })
            .RequireAuthorization(CrmPolicies.CompanyManage);

        companies.MapPut("/{id:long}", async (
                long id,
                [FromBody] UpdateCompanyRequest request,
                ICompanyService svc,
                CancellationToken ct) =>
            {
                var updated = await svc.UpdateAsync(id, request, ct);
                return updated is null ? Results.NotFound() : Results.Ok(updated);
            })
            .RequireAuthorization(CrmPolicies.CompanyManage);

        companies.MapDelete("/{id:long}", async (long id, ICompanyService svc, CancellationToken ct) =>
            {
                var ok = await svc.DeleteAsync(id, ct);
                return ok ? Results.NoContent() : Results.NotFound();
            })
            .RequireAuthorization(CrmPolicies.CompanyManage);

        // 分配负责人
        companies.MapPost("/{id:long}/assign", async (
                long id,
                [FromBody] long ownerId,
                ICompanyService svc,
                CancellationToken ct) =>
            {
                var ok = await svc.AssignAsync(id, ownerId, ct);
                return ok ? Results.NoContent() : Results.NotFound();
            })
            .RequireAuthorization(CrmPolicies.CompanyAssign);

        // 更新标签
        companies.MapPut("/{id:long}/tags", async (
                long id,
                [FromBody] List<string> tags,
                ICompanyService svc,
                CancellationToken ct) =>
            {
                var ok = await svc.UpdateTagsAsync(id, tags, ct);
                return ok ? Results.NoContent() : Results.NotFound();
            })
            .RequireAuthorization(CrmPolicies.CompanyManage);

        // 更新状态
        companies.MapPost("/{id:long}/status", async (
                long id,
                [FromBody] CustomerStatus status,
                ICompanyService svc,
                CancellationToken ct) =>
            {
                var ok = await svc.UpdateStatusAsync(id, status, ct);
                return ok ? Results.NoContent() : Results.NotFound();
            })
            .RequireAuthorization(CrmPolicies.CompanyManage);

        // 更新评级
        companies.MapPost("/{id:long}/rating", async (
                long id,
                [FromBody] IntentRating rating,
                ICompanyService svc,
                CancellationToken ct) =>
            {
                var ok = await svc.UpdateRatingAsync(id, rating, false, ct);
                return ok ? Results.NoContent() : Results.NotFound();
            })
            .RequireAuthorization(CrmPolicies.CompanyManage);

        // 今日待办
        companies.MapGet("/tasks/today", async (
                [FromQuery] long? ownerId,
                ICompanyService svc,
                CancellationToken ct) =>
            {
                return Results.Ok(await svc.GetTodayTasksAsync(ownerId, ct));
            })
            .RequireAuthorization(CrmPolicies.CompanyRead, CrmPolicies.ActivityRead);

        // ERP客商候选匹配（只读，不写关联关系）
        // 双重权限：crm.company.read + mdm.business-partner.read
        // 查询结果统一称"候选匹配"，不显示"已同步"或"已关联"
        companies.MapGet("/{id:long}/erp-match", async (
                long id,
                ICompanyErpMatchService svc,
                CancellationToken ct) =>
            {
                var result = await svc.GetCandidatesAsync(id, ct);
                return result is null
                    ? Results.NotFound(new { error = "客户不存在或无权访问" })
                    : Results.Ok(result);
            })
            .RequireAuthorization(CrmPolicies.CompanyErpMatch);

        // ===== 联系人（Contact）=====
        var contacts = group.MapGroup("/contacts");

        contacts.MapGet("/by-company/{companyId:long}", async (long companyId, IContactService svc, CancellationToken ct) =>
            {
                return Results.Ok(await svc.GetByCompanyAsync(companyId, ct));
            })
            .RequireAuthorization(CrmPolicies.ContactRead);

        contacts.MapGet("/{id:long}", async (long id, IContactService svc, CancellationToken ct) =>
            {
                var dto = await svc.GetByIdAsync(id, ct);
                return dto is null ? Results.NotFound() : Results.Ok(dto);
            })
            .RequireAuthorization(CrmPolicies.ContactRead);

        contacts.MapPost("", async (
                [FromBody] CreateContactRequest request,
                IContactService svc,
                CancellationToken ct) =>
            {
                return await CreateWithBadRequestAsync(
                    () => svc.CreateAsync(request, ct),
                    c => c.Id,
                    "/api/v1/crm/contacts");
            })
            .RequireAuthorization(CrmPolicies.ContactManage);

        contacts.MapPut("/{id:long}", async (
                long id,
                [FromBody] UpdateContactRequest request,
                IContactService svc,
                CancellationToken ct) =>
            {
                var updated = await svc.UpdateAsync(id, request, ct);
                return updated is null ? Results.NotFound() : Results.Ok(updated);
            })
            .RequireAuthorization(CrmPolicies.ContactManage);

        contacts.MapDelete("/{id:long}", async (long id, IContactService svc, CancellationToken ct) =>
            {
                var ok = await svc.DeleteAsync(id, ct);
                return ok ? Results.NoContent() : Results.NotFound();
            })
            .RequireAuthorization(CrmPolicies.ContactManage);

        // 设为关键人
        contacts.MapPost("/{id:long}/key-person", async (long id, IContactService svc, CancellationToken ct) =>
            {
                var ok = await svc.SetKeyPersonAsync(id, ct);
                return ok ? Results.NoContent() : Results.NotFound();
            })
            .RequireAuthorization(CrmPolicies.ContactManage);

        // ===== 商机（Deal）=====
        var deals = group.MapGroup("/deals");

        deals.MapGet("", async (
                [FromQuery] string? keyword,
                [FromQuery] DealStatus? status,
                [FromQuery] DealStage? stage,
                [FromQuery] BusinessCategory? category,
                [FromQuery] long? ownerId,
                [FromQuery] long? companyId,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                IDealService svc,
                CancellationToken ct) =>
            {
                var query = new DealListQuery(keyword, status, stage, category, ownerId, companyId, page ?? 1, pageSize ?? 20);
                return Results.Ok(await svc.ListAsync(query, ct));
            })
            .RequireAuthorization(CrmPolicies.DealRead);

        deals.MapGet("/{id:long}", async (long id, IDealService svc, CancellationToken ct) =>
            {
                var dto = await svc.GetByIdAsync(id, ct);
                return dto is null ? Results.NotFound() : Results.Ok(dto);
            })
            .RequireAuthorization(CrmPolicies.DealRead);

        deals.MapPost("", async (
                [FromBody] CreateDealRequest request,
                HttpContext http,
                IDealService svc,
                CancellationToken ct) =>
            {
                var userId = GetUserId(http);
                return await CreateWithBadRequestAsync(
                    () => svc.CreateAsync(request, userId, ct),
                    d => d.Id,
                    "/api/v1/crm/deals");
            })
            .RequireAuthorization(CrmPolicies.DealManage);

        deals.MapPut("/{id:long}", async (
                long id,
                [FromBody] UpdateDealRequest request,
                IDealService svc,
                CancellationToken ct) =>
            {
                var updated = await svc.UpdateAsync(id, request, ct);
                return updated is null ? Results.NotFound() : Results.Ok(updated);
            })
            .RequireAuthorization(CrmPolicies.DealManage);

        deals.MapDelete("/{id:long}", async (long id, IDealService svc, CancellationToken ct) =>
            {
                var ok = await svc.DeleteAsync(id, ct);
                return ok ? Results.NoContent() : Results.NotFound();
            })
            .RequireAuthorization(CrmPolicies.DealManage);

        // 更新阶段
        deals.MapPost("/{id:long}/stage", async (
                long id,
                [FromBody] DealStage stage,
                IDealService svc,
                CancellationToken ct) =>
            {
                var ok = await svc.UpdateStageAsync(id, stage, ct);
                return ok ? Results.NoContent() : Results.NotFound();
            })
            .RequireAuthorization(CrmPolicies.DealManage);

        // 成交
        deals.MapPost("/{id:long}/win", async (long id, IDealService svc, CancellationToken ct) =>
            {
                var ok = await svc.WinAsync(id, ct);
                return ok ? Results.NoContent() : Results.NotFound();
            })
            .RequireAuthorization(CrmPolicies.DealManage);

        // 丢单
        deals.MapPost("/{id:long}/lose", async (
                long id,
                [FromBody] LostReason reason,
                IDealService svc,
                CancellationToken ct) =>
            {
                var ok = await svc.LoseAsync(id, reason, ct);
                return ok ? Results.NoContent() : Results.NotFound();
            })
            .RequireAuthorization(CrmPolicies.DealManage);

        // ===== 跟进记录（Activity）=====
        var activities = group.MapGroup("/activities");

        activities.MapGet("/by-company/{companyId:long}", async (long companyId, IActivityService svc, CancellationToken ct) =>
            {
                return Results.Ok(await svc.GetByCompanyAsync(companyId, ct));
            })
            .RequireAuthorization(CrmPolicies.ActivityRead);

        activities.MapGet("/by-deal/{dealId:long}", async (long dealId, IActivityService svc, CancellationToken ct) =>
            {
                return Results.Ok(await svc.GetByDealAsync(dealId, ct));
            })
            .RequireAuthorization(CrmPolicies.ActivityRead);

        activities.MapGet("/{id:long}", async (long id, IActivityService svc, CancellationToken ct) =>
            {
                var dto = await svc.GetByIdAsync(id, ct);
                return dto is null ? Results.NotFound() : Results.Ok(dto);
            })
            .RequireAuthorization(CrmPolicies.ActivityRead);

        activities.MapPost("", async (
                [FromBody] CreateActivityRequest request,
                HttpContext http,
                IActivityService svc,
                CancellationToken ct) =>
            {
                var userId = GetUserId(http);
                return await CreateWithBadRequestAsync(
                    () => svc.CreateAsync(request, userId, ct),
                    a => a.Id,
                    "/api/v1/crm/activities");
            })
            .RequireAuthorization(CrmPolicies.ActivityManage);

        activities.MapPut("/{id:long}", async (
                long id,
                [FromBody] UpdateActivityRequest request,
                IActivityService svc,
                CancellationToken ct) =>
            {
                var updated = await svc.UpdateAsync(id, request, ct);
                return updated is null ? Results.NotFound() : Results.Ok(updated);
            })
            .RequireAuthorization(CrmPolicies.ActivityManage);

        activities.MapDelete("/{id:long}", async (long id, IActivityService svc, CancellationToken ct) =>
            {
                var ok = await svc.DeleteAsync(id, ct);
                return ok ? Results.NoContent() : Results.NotFound();
            })
            .RequireAuthorization(CrmPolicies.ActivityManage);

        activities.MapPost("/{id:long}/complete-task", async (long id, IActivityService svc, CancellationToken ct) =>
            {
                var ok = await svc.CompleteTaskAsync(id, ct);
                return ok ? Results.Ok(new { success = true }) : Results.NotFound();
            })
            .RequireAuthorization(CrmPolicies.ActivityManage);

        // ===== 导入导出日志（ImportExportLog）=====
        var logs = group.MapGroup("/logs");

        logs.MapGet("", async (
                [FromQuery] OperationType? operationType,
                [FromQuery] LogStatus? status,
                [FromQuery] DateTimeOffset? from,
                [FromQuery] DateTimeOffset? to,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                IImportExportLogService svc,
                CancellationToken ct) =>
            {
                var query = new ImportExportLogListQuery(operationType, status, from, to, page ?? 1, pageSize ?? 20);
                return Results.Ok(await svc.ListAsync(query, ct));
            })
            .RequireAuthorization(CrmPolicies.LogRead);

        logs.MapGet("/{id:long}", async (long id, IImportExportLogService svc, CancellationToken ct) =>
            {
                var dto = await svc.GetByIdAsync(id, ct);
                return dto is null ? Results.NotFound() : Results.Ok(dto);
            })
            .RequireAuthorization(CrmPolicies.LogRead);

        // ===== 经营看板 =====
        group.MapGet("/dashboard/stats", async (
                ILeadService leadSvc,
                ICompanyService companySvc,
                IDealService dealSvc,
                CancellationToken ct) =>
            {
                var leads = await leadSvc.ListAsync(new LeadListQuery(null, null, null, null, null, null, null, null, null, 1, 10000), ct);
                var companies = await companySvc.ListAsync(new CompanyListQuery(null, null, null, null, null, null, null, 1, 10000), ct);
                var deals = await dealSvc.ListAsync(new DealListQuery(null, null, null, null, null, null, 1, 10000), ct);

                var leadCount = leads.Total;
                var companyCount = companies.Total;
                var activeDeals = deals.Items.Where(d => d.Status == DealStatus.InProgress).ToList();
                var wonDeals = deals.Items.Where(d => d.Status == DealStatus.Won).ToList();
                var lostDeals = deals.Items.Where(d => d.Status == DealStatus.Lost).ToList();

                var activeAmount = activeDeals.Sum(d => d.ExpectedAmount);
                var wonAmount = wonDeals.Sum(d => d.ExpectedAmount);

                // 按阶段统计
                var stageStats = Enum.GetValues<DealStage>()
                    .Where(s => s <= DealStage.Negotiation)
                    .Select(s => new
                    {
                        stage = (int)s,
                        label = s.ToString(),
                        count = activeDeals.Count(d => d.Stage == s)
                    })
                    .ToList();

                // 按负责人统计
                var ownerStats = activeDeals
                    .Where(d => d.OwnerId.HasValue)
                    .GroupBy(d => d.OwnerId!.Value)
                    .Select(g => new
                    {
                        ownerId = g.Key,
                        dealCount = g.Count(),
                        amount = g.Sum(d => d.ExpectedAmount)
                    })
                    .ToList();

                // 逾期跟进任务统计
                var todayTasks = await companySvc.GetTodayTasksAsync(null, ct);
                var overdueTasks = todayTasks.Where(t => t.IsOverdue).ToList();

                return Results.Ok(new
                {
                    funnel = new
                    {
                        leads = leadCount,
                        companies = companyCount,
                        activeDeals = activeDeals.Count,
                        wonDeals = wonDeals.Count
                    },
                    conversion = new
                    {
                        leadToCompany = companyCount > 0 ? Math.Round((double)companyCount / leadCount * 100, 1) : 0,
                        companyToDeal = companyCount > 0 ? Math.Round((double)activeDeals.Count / companyCount * 100, 1) : 0,
                        dealToWin = deals.Items.Count > 0 ? Math.Round((double)wonDeals.Count / deals.Items.Count * 100, 1) : 0
                    },
                    amount = new
                    {
                        active = activeAmount,
                        won = wonAmount,
                        lost = lostDeals.Sum(d => d.ExpectedAmount)
                    },
                    byStage = stageStats,
                    byOwner = ownerStats,
                    summary = new
                    {
                        totalDeals = deals.Total,
                        lostDeals = lostDeals.Count
                    },
                    overdue = new
                    {
                        count = overdueTasks.Count,
                        todayCount = todayTasks.Count - overdueTasks.Count,
                        items = overdueTasks.Take(10).Select(t => new
                        {
                            activityId = t.ActivityId,
                            companyId = t.CompanyId,
                            companyName = t.CompanyName,
                            nextAction = t.NextAction,
                            nextActionDate = t.NextActionDate.ToString("yyyy-MM-dd")
                        }).ToList()
                    }
                });
            })
            .RequireAuthorization(CrmPolicies.LeadRead, CrmPolicies.CompanyRead,
                CrmPolicies.DealRead, CrmPolicies.ActivityRead);

        // ===== 商机建议（规则引擎）=====
        group.MapGet("/opportunity-suggestions", async (
                ILeadService leadSvc,
                ICompanyService companySvc,
                CancellationToken ct) =>
            {
                var leads = await leadSvc.ListAsync(new LeadListQuery(null, null, null, null, null, null, null, null, null, 1, 10000), ct);
                var companies = await companySvc.ListAsync(new CompanyListQuery(null, null, null, null, null, null, null, 1, 10000), ct);

                var suggestions = new List<object>();

                // 规则R1: 无官网 → 建站机会
                // 规则R2: 有官网+来源是网站测绘 → GEO优化机会
                // 规则R3: 食品/机械/化工行业+有官网 → AI数字化机会
                // 规则R4: 高意向评分 → 优先级加成
                foreach (var lead in leads.Items.Where(l => l.Status == LeadStatus.Pending || l.Status == LeadStatus.Claimed))
                {
                    var hasWebsite = !string.IsNullOrWhiteSpace(lead.WebsiteDomain);
                    var isQuakeSource = lead.Source == SourceChannel.Other || lead.Source == SourceChannel.Map;
                    var isTargetIndustry = lead.Industry is CompanyIndustry.Machinery or CompanyIndustry.Food or CompanyIndustry.Chemical or CompanyIndustry.Agriculture;
                    var isHighIntent = lead.AiScore >= 70 || lead.Rating == IntentRating.A;
                    var hasBaiduTag = lead.Tags != null && lead.Tags.Contains("百度推广");
                    var isIndividualBusiness = lead.SubjectType == SubjectType.IndividualBusiness;
                    var hasPoiTag = lead.Tags != null && (lead.Tags.Contains("POI客户") || lead.Tags.Contains("待标注") || lead.Tags.Contains("已下架") || lead.Tags.Contains("待重新上架") || lead.Tags.Contains("抖音地址"));
                    var hasDelistedTag = lead.Tags != null && lead.Tags.Contains("已下架");
                    var hasDouyinTag = lead.Tags != null && lead.Tags.Contains("抖音地址");

                    if (!hasWebsite)
                    {
                        suggestions.Add(new
                        {
                            targetType = "lead", targetId = lead.Id, targetName = lead.CompanyName,
                            opportunityType = "企业建站", reason = "该企业暂无官网，适合建站服务切入",
                            evidence = $"WebsiteDomain为空; 来源={lead.Source}",
                            priority = isHighIntent ? "高" : "中高",
                            suggestedAmount = "5,000-20,000",
                            productCategory = (int)BusinessCategory.WebsiteBuild
                        });
                    }
                    else if (isQuakeSource || hasBaiduTag)
                    {
                        var geoPriority = hasBaiduTag ? "高" : (isHighIntent ? "高" : "中");
                        var geoReason = hasBaiduTag
                            ? "企业已投放百度推广（有付费习惯），GEO优化可降低获客成本、提升AI搜索可见性"
                            : "企业有官网但来自网站测绘渠道，AI可见性可能较差";
                        suggestions.Add(new
                        {
                            targetType = "lead", targetId = lead.Id, targetName = lead.CompanyName,
                            opportunityType = "GEO优化", reason = geoReason,
                            evidence = $"域名={lead.WebsiteDomain}; 来源={lead.Source}; 百度推广标签={hasBaiduTag}",
                            priority = geoPriority,
                            suggestedAmount = "3,000-10,000",
                            productCategory = (int)BusinessCategory.GEOOpt
                        });
                    }
                    if (hasWebsite && isTargetIndustry)
                    {
                        suggestions.Add(new
                        {
                            targetType = "lead", targetId = lead.Id, targetName = lead.CompanyName,
                            opportunityType = "AI数字化服务", reason = $"{lead.Industry}行业企业，可试点AI应用/数字化服务",
                            evidence = $"行业={lead.Industry}; 有官网={hasWebsite}",
                            priority = "中",
                            suggestedAmount = "10,000-50,000",
                            productCategory = (int)BusinessCategory.AIApp
                        });
                    }
                    // 规则R5: 个体商户/门店 → 地图标注服务
                    if (isIndividualBusiness || hasPoiTag)
                    {
                        var poiPriority = hasDelistedTag ? "高" : (isHighIntent ? "高" : "中");
                        var poiReason = hasDelistedTag
                            ? "店铺已被地图平台下架，有重新上架需求"
                            : (isIndividualBusiness ? "个体商户/门店客户，地图信息完善可提升到店客流" : "标记为POI客户，需要地图标注服务");
                        suggestions.Add(new
                        {
                            targetType = "lead", targetId = lead.Id, targetName = lead.CompanyName,
                            opportunityType = "地图标注", reason = poiReason,
                            evidence = $"主体类型={lead.SubjectType}; 标签={string.Join(",", lead.Tags ?? new List<string>())}",
                            priority = poiPriority,
                            suggestedAmount = "300-1,500",
                            productCategory = (int)BusinessCategory.PoiAnnotation
                        });
                    }
                    // 规则R6: 抖音地址标签 → 抖音/快手地址添加服务
                    if (hasDouyinTag)
                    {
                        suggestions.Add(new
                        {
                            targetType = "lead", targetId = lead.Id, targetName = lead.CompanyName,
                            opportunityType = "抖音地址添加", reason = "客户需要在抖音/快手等短视频平台添加店铺地址定位",
                            evidence = $"标签包含抖音地址",
                            priority = "中",
                            suggestedAmount = "200-800",
                            productCategory = (int)BusinessCategory.DouyinAddress
                        });
                    }
                }

                foreach (var comp in companies.Items.Where(c => c.Status == CustomerStatus.ToContact || c.Status == CustomerStatus.Following))
                {
                    var hasWebsite = comp.HasWebsite || !string.IsNullOrWhiteSpace(comp.WebsiteDomain);
                    var isTargetIndustry = comp.Industry is CompanyIndustry.Machinery or CompanyIndustry.Food or CompanyIndustry.Chemical or CompanyIndustry.Agriculture;
                    var isHighIntent = comp.Rating == IntentRating.A;
                    var siteAbnormal = comp.WebsiteStatus == WebsiteStatus.CannotOpen || comp.WebsiteStatus == WebsiteStatus.Outdated || comp.WebsiteStatus == WebsiteStatus.NoWebsite;

                    if (!hasWebsite)
                    {
                        suggestions.Add(new
                        {
                            targetType = "company", targetId = comp.Id, targetName = comp.FullName,
                            opportunityType = "企业建站", reason = "客户主档记录无官网，适合建站服务",
                            evidence = $"HasWebsite={comp.HasWebsite}; WebsiteDomain={comp.WebsiteDomain}",
                            priority = isHighIntent ? "高" : "中高",
                            suggestedAmount = "5,000-20,000",
                            productCategory = (int)BusinessCategory.WebsiteBuild
                        });
                    }
                    if (siteAbnormal)
                    {
                        suggestions.Add(new
                        {
                            targetType = "company", targetId = comp.Id, targetName = comp.FullName,
                            opportunityType = "网站重建", reason = "官网状态异常或无法访问，需要重建或修复",
                            evidence = $"WebsiteStatus={comp.WebsiteStatus}",
                            priority = "高",
                            suggestedAmount = "8,000-30,000",
                            productCategory = (int)BusinessCategory.WebsiteOpt
                        });
                    }
                    if (hasWebsite && isTargetIndustry && !siteAbnormal)
                    {
                        suggestions.Add(new
                        {
                            targetType = "company", targetId = comp.Id, targetName = comp.FullName,
                            opportunityType = "AI数字化服务", reason = $"{comp.Industry}行业客户，可试点AI应用",
                            evidence = $"行业={comp.Industry}; 有官网={hasWebsite}",
                            priority = "中",
                            suggestedAmount = "10,000-50,000",
                            productCategory = (int)BusinessCategory.AIApp
                        });
                    }
                }

                return Results.Ok(new { total = suggestions.Count, items = suggestions });
            })
            .RequireAuthorization(CrmPolicies.LeadRead, CrmPolicies.CompanyRead);

        // ===== 企业情报信号 =====
        group.MapGet("/signals", async (
                ISignalService svc,
                [AsParameters] SignalListQuery query,
                CancellationToken ct) =>
            {
                var result = await svc.ListAsync(query with { Page = query.Page < 1 ? 1 : query.Page, PageSize = query.PageSize is < 1 or > 200 ? 20 : query.PageSize }, ct);
                return Results.Ok(result);
            })
            .RequireAuthorization(CrmPolicies.LeadRead);

        group.MapPost("/signals", async (
                ISignalService svc,
                CreateSignalCommand cmd,
                CancellationToken ct) =>
            {
                var result = await svc.CreateAsync(cmd, ct);
                return Results.Created($"/api/v1/crm/signals/{result.Id}", result);
            })
            .RequireAuthorization(CrmPolicies.LeadManage);

        group.MapPut("/signals/{id:long}/status", async (
                long id,
                ISignalService svc,
                UpdateSignalStatusCommand cmd,
                CancellationToken ct) =>
            {
                try
                {
                    var result = await svc.UpdateStatusAsync(id, cmd, ct);
                    return Results.Ok(result);
                }
                catch (KeyNotFoundException ex)
                {
                    return Results.NotFound(new { detail = ex.Message });
                }
            })
            .RequireAuthorization(CrmPolicies.LeadManage);

        group.MapPut("/signals/{id:long}/verification", async (
                long id,
                ISignalService svc,
                UpdateVerificationCommand cmd,
                CancellationToken ct) =>
            {
                try
                {
                    var result = await svc.UpdateVerificationAsync(id, cmd, ct);
                    return Results.Ok(result);
                }
                catch (KeyNotFoundException ex)
                {
                    return Results.NotFound(new { detail = ex.Message });
                }
            })
            .RequireAuthorization(CrmPolicies.LeadManage);

        // ===== Quake 线索采集 =====
        var quake = group.MapGroup("/quake");
        quake.MapGet("/search", async (
                string query,
                int? size,
                bool? force,
                QuakeSearchService svc,
                QuakeQueryHistoryService historySvc,
                CancellationToken ct) =>
            {
                try
                {
                    // 检查冷却期（非强制查询时）
                    if (force != true)
                    {
                        var cooldown = await historySvc.CheckCooldownAsync(query, 30, ct);
                        if (cooldown != null)
                        {
                            return Results.Ok(new
                            {
                                total = cooldown.ResultCount,
                                items = Array.Empty<object>(),
                                fromCache = true,
                                lastQueriedAt = cooldown.QueriedAt,
                                message = $"该条件已于 {cooldown.QueriedAt:yyyy-MM-dd} 查询过（{cooldown.ResultCount}条结果），30天冷却期内可使用\"强制重新查询\""
                            });
                        }
                    }
                    var results = await svc.SearchAsync(query, size ?? 20, ct);
                    // 记录查询历史
                    await historySvc.RecordAsync(query, results.Count, 0, ct);
                    return Results.Ok(new { total = results.Count, items = results, fromCache = false });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { detail = ex.Message });
                }
            })
            .RequireAuthorization(CrmPolicies.LeadManage);

        quake.MapPost("/import", async (
                List<QuakeImportItem> items,
                ILeadService leadSvc,
                HttpContext http,
                CancellationToken ct) =>
            {
                var userId = GetUserId(http);
                int created = 0, skipped = 0;
                foreach (var item in items)
                {
                    try
                    {
                        var cmd = new CreateLeadRequest(
                            CompanyName: item.CompanyName ?? item.Domain,
                            CreditCode: null,
                            WebsiteDomain: item.Domain,
                            ContactName: null,
                            ContactPosition: null,
                            Mobile: null,
                            Email: null,
                            Wechat: null,
                            Phone: null,
                            City: CompanyCity.Weifang,
                            District: null,
                            Address: null,
                            Industry: CompanyIndustry.Other,
                            Scale: null,
                            Source: SourceChannel.Other,
                            SourceUrl: null,
                            ExternalKey: null,
                            ExternalSource: "Quake",
                            Remarks: $"Quake采集: {item.Title} | {item.City} | IP:{item.Ip}",
                            Tags: item.HasBaiduAds ? new List<string> { "百度推广" } : null);
                        await leadSvc.CreateAsync(cmd, userId, ct);
                        created++;
                    }
                    catch
                    {
                        skipped++;
                    }
                }
                return Results.Ok(new { created, skipped, total = items.Count });
            })
            .RequireAuthorization(CrmPolicies.LeadManage);

        // Quake 查询冷却检查：相同条件30天内查过则返回最近记录
        quake.MapGet("/history/check", async (
                string query,
                int? cooldownDays,
                QuakeQueryHistoryService svc,
                CancellationToken ct) =>
            {
                var record = await svc.CheckCooldownAsync(query, cooldownDays ?? 30, ct);
                if (record == null)
                    return Results.Ok(new { inCooldown = false });
                return Results.Ok(new
                {
                    inCooldown = true,
                    queriedAt = record.QueriedAt,
                    resultCount = record.ResultCount,
                    importedCount = record.ImportedCount,
                    queryText = record.QueryText
                });
            })
            .RequireAuthorization(CrmPolicies.LeadManage);

        // Quake 查询历史列表
        quake.MapGet("/history", async (
                int? limit,
                QuakeQueryHistoryService svc,
                CancellationToken ct) =>
            {
                var list = await svc.GetRecentAsync(limit ?? 20, ct);
                return Results.Ok(new { total = list.Count, items = list });
            })
            .RequireAuthorization(CrmPolicies.LeadManage);

        // ===== ICP 备案查询 =====
        var icp = group.MapGroup("/icp");
        icp.MapPost("/lookup", async (
                List<string> domains,
                IcpLookupService svc,
                CancellationToken ct) =>
            {
                try
                {
                    var results = await svc.LookupBatchAsync(domains, ct);
                    var found = results.Count(r => r.Found);
                    return Results.Ok(new { total = results.Count, found, items = results });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { detail = ex.Message });
                }
            })
            .RequireAuthorization(CrmPolicies.LeadManage);

        // ===== 网站检测 =====
        var wc = group.MapGroup("/website-check");
        wc.MapPost("", async (
                List<string> domains,
                WebsiteCheckService svc,
                CancellationToken ct) =>
            {
                try
                {
                    var results = await svc.CheckBatchAsync(domains, ct);
                    int accessible = results.Count(r => r.Accessible);
                    int hasIssues = results.Count(r => r.Issues.Count > 0);
                    // 不返回 HTML 原文
                    var items = results.Select(r => new
                    {
                        r.Domain,
                        r.Accessible,
                        r.HttpsSupported,
                        r.ResponseMs,
                        r.Title,
                        r.Description,
                        r.HasViewport,
                        r.HasFavicon,
                        r.ContentLength,
                        r.Issues,
                        r.Error,
                        r.HasBaiduAds,
                        r.BaiduAdsType
                    });
                    return Results.Ok(new { total = results.Count, accessible, hasIssues, items });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { detail = ex.Message });
                }
            })
            .RequireAuthorization(CrmPolicies.LeadManage);

        // ===== GEO 检测 =====
        var geo = group.MapGroup("/geo-check");
        geo.MapPost("", async (
                List<string> domains,
                GeoCheckService svc,
                CancellationToken ct) =>
            {
                try
                {
                    var results = await svc.CheckBatchAsync(domains, ct);
                    int checkedCount = results.Count(r => r.Checked);
                    int criticalCount = results.Count(r => r.Band == "critical");
                    return Results.Ok(new { total = results.Count, geoChecked = checkedCount, critical = criticalCount, items = results });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { detail = ex.Message });
                }
            })
            .RequireAuthorization(CrmPolicies.LeadManage);

        // 单位反查：根据单位名称+可选网址，批量查ICP/网站状态/GEO
        var reverse = group.MapGroup("/reverse-lookup");
        reverse.MapPost("", async (
                ReverseLookupRequest req,
                ReverseLookupService svc,
                CancellationToken ct) =>
            {
                try
                {
                    var items = req.Items.Select(i => new ReverseLookupItem
                    {
                        CompanyName = i.CompanyName,
                        Website = i.Website
                    }).ToList();
                    var results = await svc.LookupBatchAsync(items, req.IncludeGeo, ct);
                    return Results.Ok(new { total = results.Count, items = results });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { detail = ex.Message });
                }
            })
            .RequireAuthorization(CrmPolicies.LeadManage);

        return routes;
    }

    private static long? GetUserId(HttpContext http)
    {
        // 与系统其他地方一致，使用 NameIdentifier（同时兼容 "sub"）
        var claim = http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? http.User.FindFirst("sub")?.Value;
        return long.TryParse(claim, out var id) ? id : null;
    }

    /// <summary>
    /// Wraps a CRM create call so that InvalidOperationException
    /// (e.g. CrmCompanyId not found) returns 400 instead of 500.
    /// </summary>
    private static async Task<IResult> CreateWithBadRequestAsync<T>(
        Func<Task<T>> createFunc,
        Func<T, long> idSelector,
        string locationPrefix) where T : class
    {
        try
        {
            var created = await createFunc();
            return Results.Created($"{locationPrefix}/{idSelector(created)}", created);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}

/// <summary>Quake 批量导入项</summary>
public class QuakeImportItem
{
    public string Domain { get; set; } = "";
    public string? CompanyName { get; set; }
    public string? Title { get; set; }
    public string? Ip { get; set; }
    public string? City { get; set; }
    public bool HasBaiduAds { get; set; }
}
