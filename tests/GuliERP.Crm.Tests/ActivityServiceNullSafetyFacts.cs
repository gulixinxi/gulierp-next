using GuliERP.Crm.Application;
using GuliERP.Crm.Domain.Entities;
using GuliERP.Crm.Domain.Enums;
using GuliERP.Crm.Infrastructure.Persistence;
using GuliERP.Crm.Infrastructure.Services;
using GuliERP.Foundation.Kernel;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GuliERP.Crm.Tests;

/// <summary>
/// Tests for ActivityService null-safety fixes (NextAction/Content null handling).
/// </summary>
public class ActivityServiceNullSafetyFacts
{
    private static CrmDbContext CreateDb(ICurrentTenant? tenant = null, ICurrentCompany? company = null)
    {
        return new CrmDbContext(
            new DbContextOptionsBuilder<CrmDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options,
            tenant ?? new MutableCurrentTenant(50),
            company ?? new MutableCurrentCompany(51));
    }

    private static async Task<long> SeedCompanyAsync(CrmDbContext db)
    {
        var company = new Company
        {
            Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000,
            CompanyNo = "CO000001",
            FullName = "测试公司",
            Source = SourceChannel.Other,
            Status = CustomerStatus.ToContact,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow
        };
        db.Companies.Add(company);
        await db.SaveChangesAsync();
        return company.Id;
    }

    [Fact]
    public async Task CreateAsync_NullNextAction_DoesNotThrow()
    {
        using var db = CreateDb();
        var companyId = await SeedCompanyAsync(db);
        var service = new ActivityService(db);

        var result = await service.CreateAsync(
            new CreateActivityRequest(companyId, null!, FollowType.Phone, null!, "测试内容", null, null!, null!, null!, null!, null!),
            userId: 1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.NextAction);
        Assert.Equal("测试内容", result.Content);
    }

    [Fact]
    public async Task CreateAsync_NullContent_DoesNotThrow()
    {
        using var db = CreateDb();
        var companyId = await SeedCompanyAsync(db);
        var service = new ActivityService(db);

        var result = await service.CreateAsync(
            new CreateActivityRequest(companyId, null!, FollowType.Phone, null, null!, null!, "下一步", null!, null, null, null),
            userId: 1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.Content);
        Assert.Equal("下一步", result.NextAction);
    }

    [Fact]
    public async Task CreateAsync_BothNull_DoesNotThrow()
    {
        using var db = CreateDb();
        var companyId = await SeedCompanyAsync(db);
        var service = new ActivityService(db);

        var result = await service.CreateAsync(
            new CreateActivityRequest(companyId, null!, FollowType.Phone, null, null!, null, null!, null!, null!, null!, null!),
            userId: 1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.Content);
        Assert.Equal(string.Empty, result.NextAction);
    }

    [Fact]
    public async Task CreateAsync_WhitespaceContent_TrimsToEmpty()
    {
        using var db = CreateDb();
        var companyId = await SeedCompanyAsync(db);
        var service = new ActivityService(db);

        var result = await service.CreateAsync(
            new CreateActivityRequest(companyId, null!, FollowType.Phone, null, "   ", null, "  下一步  ", null!, null, null, null),
            userId: 1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.Content);
        Assert.Equal("下一步", result.NextAction);
    }

    [Fact]
    public async Task UpdateAsync_NullNextAction_DoesNotThrow()
    {
        using var db = CreateDb();
        var companyId = await SeedCompanyAsync(db);
        var service = new ActivityService(db);

        var created = await service.CreateAsync(
            new CreateActivityRequest(companyId, null!, FollowType.Phone, null, "原内容", null!, "原下一步", null!, null, null, null),
            userId: 1, CancellationToken.None);

        var updated = await service.UpdateAsync(created.Id,
            new UpdateActivityRequest(FollowType.Visit, DateTimeOffset.UtcNow, null!, null, null!, null!, null!, null!, null!),
            CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal(string.Empty, updated!.Content);
        Assert.Equal(string.Empty, updated.NextAction);
    }
}
