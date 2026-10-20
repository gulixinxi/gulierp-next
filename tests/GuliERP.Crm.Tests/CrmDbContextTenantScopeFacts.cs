using GuliERP.Crm.Domain.Entities;
using GuliERP.Crm.Domain.Enums;
using GuliERP.Crm.Infrastructure.Persistence;
using GuliERP.Foundation.Kernel;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GuliERP.Crm.Tests;

/// <summary>
/// Verifies that CrmDbContext.SaveChanges automatically fills TenantId/CompanyId
/// for newly added ICompanyScoped entities from the current context.
/// </summary>
public class CrmDbContextTenantScopeFacts
{
    private static CrmDbContext CreateDb(
        ICurrentTenant? tenant = null,
        ICurrentCompany? company = null,
        string? databaseName = null)
    {
        return new CrmDbContext(
            new DbContextOptionsBuilder<CrmDbContext>()
                .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString("N"))
                .Options,
            tenant,
            company);
    }

    private static Lead MakeLead(string name = "测试企业") => new()
    {
        Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000 + Random.Shared.Next(1000),
        CompanyName = name,
        Source = SourceChannel.Other,
        Status = LeadStatus.Pending,
        CreatedAt = DateTimeOffset.UtcNow,
        ModifiedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task LeadReads_RequireMatchingTenantAndCompany()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        long leadId;
        using (var writer = CreateDb(new MutableCurrentTenant(50), new MutableCurrentCompany(51), databaseName))
        {
            var lead = MakeLead();
            leadId = lead.Id;
            writer.Leads.Add(lead);
            await writer.SaveChangesAsync();
        }

        using (var otherCompany = CreateDb(new MutableCurrentTenant(50), new MutableCurrentCompany(52), databaseName))
        {
            Assert.Empty(await otherCompany.Leads.ToListAsync());
            Assert.Null(await otherCompany.Leads.FindAsync(leadId));
        }

        using (var otherTenant = CreateDb(new MutableCurrentTenant(53), new MutableCurrentCompany(51), databaseName))
        {
            Assert.Empty(await otherTenant.Leads.ToListAsync());
        }

        using (var owner = CreateDb(new MutableCurrentTenant(50), new MutableCurrentCompany(51), databaseName))
        {
            Assert.NotNull(await owner.Leads.FindAsync(leadId));
        }
    }

    [Fact]
    public async Task QuakeHistory_RequiresMatchingTenantAndCompany()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        using (var writer = CreateDb(new MutableCurrentTenant(50), new MutableCurrentCompany(51), databaseName))
        {
            var history = new QuakeQueryHistory
            {
                QueryText = "city:weifang",
                QueryHash = "test-hash",
                QueriedAt = DateTime.UtcNow
            };
            writer.QuakeQueryHistories.Add(history);
            await writer.SaveChangesAsync();
            Assert.Equal(50, history.TenantId);
            Assert.Equal(51, history.CompanyId);
        }

        using var otherCompany = CreateDb(new MutableCurrentTenant(50), new MutableCurrentCompany(52), databaseName);
        Assert.Empty(await otherCompany.QuakeQueryHistories.ToListAsync());
        using var otherTenant = CreateDb(new MutableCurrentTenant(53), new MutableCurrentCompany(51), databaseName);
        Assert.Empty(await otherTenant.QuakeQueryHistories.ToListAsync());
        using var owner = CreateDb(new MutableCurrentTenant(50), new MutableCurrentCompany(51), databaseName);
        Assert.Single(await owner.QuakeQueryHistories.ToListAsync());
        using var noCompany = CreateDb(new MutableCurrentTenant(50), new MutableCurrentCompany(null), databaseName);
        Assert.Empty(await noCompany.QuakeQueryHistories.ToListAsync());
    }

    [Fact]
    public async Task SaveChangesAsync_FillsTenantAndCompanyFromContext_WhenEntityHasZeroScope()
    {
        var tenant = new MutableCurrentTenant(50);
        var company = new MutableCurrentCompany(51);
        using var db = CreateDb(tenant, company);

        var lead = MakeLead();
        db.Leads.Add(lead);
        await db.SaveChangesAsync();

        Assert.Equal(50, lead.TenantId);
        Assert.Equal(51, lead.CompanyId);
    }

    [Fact]
    public async Task SaveChanges_FillsTenantAndCompanyFromContext_WhenEntityHasZeroScope()
    {
        var tenant = new MutableCurrentTenant(50);
        var company = new MutableCurrentCompany(51);
        using var db = CreateDb(tenant, company);

        var lead = MakeLead();
        db.Leads.Add(lead);
        db.SaveChanges();

        Assert.Equal(50, lead.TenantId);
        Assert.Equal(51, lead.CompanyId);
    }

    [Fact]
    public async Task SaveChangesAsync_PreservesExplicitScope_WhenMatchesContext()
    {
        var tenant = new MutableCurrentTenant(50);
        var company = new MutableCurrentCompany(51);
        using var db = CreateDb(tenant, company);

        var lead = MakeLead();
        lead.TenantId = 50;
        lead.CompanyId = 51;
        db.Leads.Add(lead);
        await db.SaveChangesAsync();

        Assert.Equal(50, lead.TenantId);
        Assert.Equal(51, lead.CompanyId);
    }

    [Fact]
    public async Task SaveChangesAsync_Throws_WhenTenantContextUnavailableAndEntityHasZeroTenant()
    {
        var company = new MutableCurrentCompany(51);
        using var db = CreateDb(tenant: null, company: company);

        var lead = MakeLead();
        db.Leads.Add(lead);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        Assert.Contains("tenant scope", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveChangesAsync_Throws_WhenCompanyContextUnavailableAndEntityHasZeroCompany()
    {
        var tenant = new MutableCurrentTenant(50);
        using var db = CreateDb(tenant: tenant, company: null);

        var lead = MakeLead();
        db.Leads.Add(lead);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        Assert.Contains("company scope", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveChangesAsync_FillsScopeForAllCompanyScopedEntities()
    {
        var tenant = new MutableCurrentTenant(50);
        var company = new MutableCurrentCompany(51);
        using var db = CreateDb(tenant, company);

        var leadId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;
        var lead = MakeLead();
        lead.Id = leadId;

        var companyEntity = new Company
        {
            Id = leadId + 1,
            CompanyNo = "CO000001",
            FullName = "测试客户",
            Source = SourceChannel.Other,
            Status = CustomerStatus.ToContact,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow
        };

        var contact = new Contact
        {
            Id = leadId + 2,
            CrmCompanyId = companyEntity.Id,
            Name = "张三",
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow
        };

        var deal = new Deal
        {
            Id = leadId + 3,
            DealNo = "DE000001",
            Name = "测试商机",
            CrmCompanyId = companyEntity.Id,
            Category = BusinessCategory.Other,
            Stage = DealStage.InitialContact,
            Status = DealStatus.InProgress,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow
        };

        var activity = new Activity
        {
            Id = leadId + 4,
            CrmCompanyId = companyEntity.Id,
            FollowType = FollowType.Other,
            FollowAt = DateTimeOffset.UtcNow,
            Content = "测试跟进",
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow
        };

        db.Leads.Add(lead);
        db.Companies.Add(companyEntity);
        db.Contacts.Add(contact);
        db.Deals.Add(deal);
        db.Activities.Add(activity);
        await db.SaveChangesAsync();

        Assert.Equal(50, lead.TenantId);
        Assert.Equal(51, lead.CompanyId);
        Assert.Equal(50, companyEntity.TenantId);
        Assert.Equal(51, companyEntity.CompanyId);
        Assert.Equal(50, contact.TenantId);
        Assert.Equal(51, contact.CompanyId);
        Assert.Equal(50, deal.TenantId);
        Assert.Equal(51, deal.CompanyId);
        Assert.Equal(50, activity.TenantId);
        Assert.Equal(51, activity.CompanyId);
    }

    [Fact]
    public async Task SaveChangesAsync_DoesNotFillScopeForNonCompanyScopedEntity()
    {
        var tenant = new MutableCurrentTenant(50);
        var company = new MutableCurrentCompany(51);
        using var db = CreateDb(tenant, company);

        var history = new QuakeQueryHistory
        {
            Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000,
            QueryText = "test query",
            QueryHash = "hash123",
            ResultCount = 5,
            ImportedCount = 3,
            QueriedAt = DateTime.UtcNow
        };

        db.QuakeQueryHistories.Add(history);
        await db.SaveChangesAsync();

        // QuakeQueryHistory does not implement ICompanyScoped; no exception and no scope.
        Assert.NotNull(history);
    }

    [Fact]
    public async Task SaveChangesAsync_DoesNotOverwriteScope_OnModifiedEntities()
    {
        var tenant = new MutableCurrentTenant(50);
        var company = new MutableCurrentCompany(51);
        using var db = CreateDb(tenant, company);

        var lead = MakeLead();
        db.Leads.Add(lead);
        await db.SaveChangesAsync();
        Assert.Equal(50, lead.TenantId);

        // Modify the entity (not Added state) — scope should not be touched.
        lead.CompanyName = "更新后的名称";
        await db.SaveChangesAsync();

        Assert.Equal(50, lead.TenantId);
        Assert.Equal(51, lead.CompanyId);
        Assert.Equal("更新后的名称", lead.CompanyName);
    }

    [Fact]
    public async Task SaveChangesAsync_RejectsCrossTenantExplicitScope()
    {
        var tenant = new MutableCurrentTenant(50);
        var company = new MutableCurrentCompany(51);
        using var db = CreateDb(tenant, company);

        var lead = MakeLead();
        lead.TenantId = 999; // different tenant
        lead.CompanyId = 51; // same company
        db.Leads.Add(lead);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        Assert.Contains("Cross-tenant", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveChangesAsync_RejectsCrossCompanyExplicitScope()
    {
        var tenant = new MutableCurrentTenant(50);
        var company = new MutableCurrentCompany(51);
        using var db = CreateDb(tenant, company);

        var lead = MakeLead();
        lead.TenantId = 50; // same tenant
        lead.CompanyId = 888; // different company
        db.Leads.Add(lead);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        Assert.Contains("Cross-company", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveChangesAsync_AcceptsExplicitScopeMatchingContext()
    {
        var tenant = new MutableCurrentTenant(50);
        var company = new MutableCurrentCompany(51);
        using var db = CreateDb(tenant, company);

        var lead = MakeLead();
        lead.TenantId = 50;
        lead.CompanyId = 51;
        db.Leads.Add(lead);
        await db.SaveChangesAsync();

        Assert.Equal(50, lead.TenantId);
        Assert.Equal(51, lead.CompanyId);
    }
}
