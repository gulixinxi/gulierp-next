using GuliERP.Crm.Application;
using GuliERP.Crm.Domain.Enums;
using GuliERP.Crm.Infrastructure.Persistence;
using GuliERP.Crm.Infrastructure.Services;
using GuliERP.Foundation.Kernel;
using Microsoft.EntityFrameworkCore;
using Npgsql;

const string requiredDatabase = "gulierp_g2_003_test";
var connection = Environment.GetEnvironmentVariable("GULIERP_CRM_SMOKE_CONNECTION");
if (string.IsNullOrWhiteSpace(connection)
    || Environment.GetEnvironmentVariable("GULIERP_CRM_SMOKE_MODE") != "ROLLBACK_ONLY")
{
    throw new InvalidOperationException(
        "Set GULIERP_CRM_SMOKE_CONNECTION and GULIERP_CRM_SMOKE_MODE=ROLLBACK_ONLY to run.");
}

var target = new NpgsqlConnectionStringBuilder(connection);
if (!string.Equals(target.Database, requiredDatabase, StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException("CRM smoke test refuses a database other than the canonical test DB.");
}

var options = new DbContextOptionsBuilder<CrmDbContext>().UseNpgsql(connection).Options;
var tenant = new SmokeTenant(10001);
var companyScope = new SmokeCompany(20001);
await using var db = new CrmDbContext(options, tenant, companyScope);
if (!await db.Database.CanConnectAsync()) throw new InvalidOperationException("Test database is unavailable.");

long leadId = 0, customerId = 0, dealId = 0;
await using (var transaction = await db.Database.BeginTransactionAsync())
{
    try
    {
        var leads = new LeadService(db);
        var deals = new DealService(db);
        var name = $"CRM_SMOKE_{Guid.NewGuid():N}";
        var lead = await leads.CreateAsync(new CreateLeadRequest(
            name, null, null, "Smoke Contact", null, null, null, null, null,
            CompanyCity.Weifang, null, null, CompanyIndustry.Service, null,
            SourceChannel.Other, null, null, null, null), 10001, CancellationToken.None);
        leadId = lead.Id;
        Require(await leads.ClaimAsync(leadId, 10001, CancellationToken.None), "lead claim");

        var customer = await leads.ConvertAsync(new ConvertLeadRequest(
            leadId, name, null, null, CompanyCity.Weifang, null, null,
            CompanyIndustry.Service, null, null, false, null, null, null),
            10001, CancellationToken.None);
        customerId = customer.Id;
        Require(await db.Contacts.CountAsync(x => x.CrmCompanyId == customerId) == 1, "contact created");
        Require(await db.Activities.CountAsync(x => x.CrmCompanyId == customerId) == 1, "first activity created");

        var deal = await deals.CreateAsync(new CreateDealRequest(
            "CRM smoke deal", customerId, null, BusinessCategory.WebsiteBuild,
            1000m, null, null, null, 10001, null), 10001, CancellationToken.None);
        dealId = deal.Id;
        Require(await deals.UpdateStageAsync(dealId, DealStage.Proposal, CancellationToken.None), "deal stage");
        Require(await deals.WinAsync(dealId, CancellationToken.None), "deal won");

        companyScope.Set(20002);
        Require(await leads.GetByIdAsync(leadId, CancellationToken.None) is null, "cross-company lead read denied");
        Require(!await deals.LoseAsync(dealId, LostReason.Other, CancellationToken.None),
            "cross-company deal write denied");
        companyScope.Set(20001);
        Require((await db.Deals.SingleAsync(x => x.Id == dealId)).Status == DealStatus.Won,
            "deal status unchanged");
        Console.WriteLine("PASS: PostgreSQL CRM flow and company isolation inside rollback-only transaction.");
    }
    finally
    {
        await transaction.RollbackAsync();
    }
}

db.ChangeTracker.Clear();
Require(!await db.Leads.IgnoreQueryFilters().AnyAsync(x => x.Id == leadId), "lead rollback");
Require(!await db.Companies.IgnoreQueryFilters().AnyAsync(x => x.Id == customerId), "customer rollback");
Require(!await db.Contacts.IgnoreQueryFilters().AnyAsync(x => x.CrmCompanyId == customerId), "contact rollback");
Require(!await db.Activities.IgnoreQueryFilters().AnyAsync(x => x.CrmCompanyId == customerId), "activity rollback");
Require(!await db.Deals.IgnoreQueryFilters().AnyAsync(x => x.Id == dealId), "deal rollback");
Console.WriteLine("PASS: rollback left no CRM smoke records.");

static void Require(bool condition, string check)
{
    if (!condition) throw new InvalidOperationException($"CRM smoke check failed: {check}");
}

sealed class SmokeTenant(long id) : ICurrentTenant
{
    public long? Id => id;
    public string? Name => null;
    public bool IsAvailable => true;
    public IDisposable Change(long? tenantId) => throw new NotSupportedException();
}

sealed class SmokeCompany(long id) : ICurrentCompany
{
    private long _id = id;
    public long? Id => _id;
    public string? Name => null;
    public bool IsAvailable => true;
    public void Set(long id) => _id = id;
    public IDisposable Change(long? companyId) => throw new NotSupportedException();
}
