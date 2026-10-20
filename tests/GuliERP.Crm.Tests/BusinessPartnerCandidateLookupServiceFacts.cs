using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Application;
using GuliERP.Mdm.Domain.Entities;
using GuliERP.Mdm.Domain.Enums;
using GuliERP.Mdm.Infrastructure.Mdm;
using GuliERP.Mdm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GuliERP.Crm.Tests;

/// <summary>
/// Unit tests for BusinessPartnerCandidateLookupService (MDM layer).
/// Covers: tenant filtering, credit code exact match, credit code duplicates,
/// credit code missing, name fuzzy match, no match, read-only behavior.
/// </summary>
public sealed class BusinessPartnerCandidateLookupServiceFacts
{
    private sealed class LookupFixture : IAsyncDisposable
    {
        public MdmDbContext Db { get; }
        public MutableCurrentTenant Tenant { get; }
        public BusinessPartnerCandidateLookupService Service { get; }

        private LookupFixture(MdmDbContext db, MutableCurrentTenant tenant, BusinessPartnerCandidateLookupService service)
        {
            Db = db;
            Tenant = tenant;
            Service = service;
        }

        public static LookupFixture Create(long tenantId = 50)
        {
            var db = new MdmDbContext(new DbContextOptionsBuilder<MdmDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options);
            var tenant = new MutableCurrentTenant(tenantId);
            var service = new BusinessPartnerCandidateLookupService(db, tenant);
            return new LookupFixture(db, tenant, service);
        }

        public async Task<BusinessPartner> AddPartnerAsync(
            long id, long tenantId, string name, string? taxNumber,
            string? shortName = null, BusinessPartnerRole role = BusinessPartnerRole.Customer)
        {
            var bp = new BusinessPartner
            {
                Id = id,
                TenantId = tenantId,
                Code = $"BP{id:D6}",
                Name = name,
                ShortName = shortName,
                TaxNumber = taxNumber,
                Role = role,
                Status = MasterDataStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                ModifiedAt = DateTimeOffset.UtcNow
            };
            Db.BusinessPartners.Add(bp);
            await Db.SaveChangesAsync();
            return bp;
        }

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    // ===== 同租户过滤 =====

    [Fact]
    public async Task FindCandidates_OnlyReturnsCurrentTenantPartners()
    {
        await using var fixture = LookupFixture.Create(tenantId: 50);
        await fixture.AddPartnerAsync(1, 50, "潍坊食品有限公司", "91370700MA12345678");
        await fixture.AddPartnerAsync(2, 99, "青岛食品有限公司", "91370700MA12345678"); // 同信用代码，不同租户

        var result = await fixture.Service.FindCandidatesAsync("91370700MA12345678", null);

        Assert.True(result.CreditCodeProvided);
        Assert.Equal(1, result.CandidateCount);
        Assert.Single(result.Candidates);
        Assert.Equal(1, result.Candidates[0].Id);
        Assert.Equal("潍坊食品有限公司", result.Candidates[0].Name);
    }

    [Fact]
    public async Task FindCandidates_NoTenantContext_ReturnsEmpty()
    {
        await using var fixture = LookupFixture.Create(tenantId: 50);
        await fixture.AddPartnerAsync(1, 50, "潍坊食品有限公司", "91370700MA12345678");
        fixture.Tenant.Set(null);

        var result = await fixture.Service.FindCandidatesAsync("91370700MA12345678", null);

        Assert.False(result.CreditCodeProvided);
        Assert.Equal(0, result.CandidateCount);
        Assert.Empty(result.Candidates);
    }

    // ===== 信用代码精确匹配 =====

    [Fact]
    public async Task FindCandidates_ExactTaxNumberMatch_ReturnsCandidateWithConfidence100()
    {
        await using var fixture = LookupFixture.Create();
        await fixture.AddPartnerAsync(1, 50, "山东良心食品有限公司", "91370700MA3C9X7K2E");

        var result = await fixture.Service.FindCandidatesAsync("91370700MA3C9X7K2E", null);

        Assert.True(result.CreditCodeProvided);
        Assert.Equal(1, result.CandidateCount);
        Assert.Equal("TaxNumber", result.Candidates[0].MatchMethod);
        Assert.Equal(100, result.Candidates[0].MatchConfidence);
    }

    [Fact]
    public async Task FindCandidates_TaxNumberIsNormalized_CaseInsensitive()
    {
        await using var fixture = LookupFixture.Create();
        await fixture.AddPartnerAsync(1, 50, "测试公司", "91370700MA3C9X7K2E");

        // 小写输入应匹配大写存储
        var result = await fixture.Service.FindCandidatesAsync("91370700ma3c9x7k2e", null);

        Assert.Equal(1, result.CandidateCount);
    }

    // ===== 信用代码重复 =====

    [Fact]
    public async Task FindCandidates_DuplicateTaxNumber_ReturnsAllCandidates()
    {
        await using var fixture = LookupFixture.Create();
        await fixture.AddPartnerAsync(1, 50, "公司A（旧）", "91370700DUP1234567");
        await fixture.AddPartnerAsync(2, 50, "公司A（新）", "91370700DUP1234567");

        var result = await fixture.Service.FindCandidatesAsync("91370700DUP1234567", null);

        Assert.Equal(2, result.CandidateCount);
        Assert.All(result.Candidates, c => Assert.Equal("TaxNumber", c.MatchMethod));
        Assert.All(result.Candidates, c => Assert.Equal(100, c.MatchConfidence));
        Assert.Contains(result.Candidates, c => c.Id == 1);
        Assert.Contains(result.Candidates, c => c.Id == 2);
    }

    // ===== 信用代码缺失 =====

    [Fact]
    public async Task FindCandidates_CreditCodeNull_OnlyNameLookup()
    {
        await using var fixture = LookupFixture.Create();
        await fixture.AddPartnerAsync(1, 50, "潍坊振华食品有限公司", null);

        var result = await fixture.Service.FindCandidatesAsync(null, "潍坊振华食品");

        Assert.False(result.CreditCodeProvided);
        Assert.Equal(1, result.CandidateCount);
        Assert.Equal("Name", result.Candidates[0].MatchMethod);
    }

    [Fact]
    public async Task FindCandidates_CreditCodeEmpty_OnlyNameLookup()
    {
        await using var fixture = LookupFixture.Create();
        await fixture.AddPartnerAsync(1, 50, "潍坊振华食品有限公司", null);

        var result = await fixture.Service.FindCandidatesAsync("   ", "潍坊振华食品");

        Assert.False(result.CreditCodeProvided);
        Assert.Equal(1, result.CandidateCount);
    }

    // ===== 名称模糊匹配 =====

    [Fact]
    public async Task FindCandidates_NameExactMatch_ReturnsConfidence95()
    {
        await using var fixture = LookupFixture.Create();
        await fixture.AddPartnerAsync(1, 50, "山东测试有限公司", null);

        var result = await fixture.Service.FindCandidatesAsync(null, "山东测试有限公司");

        Assert.Equal(1, result.CandidateCount);
        Assert.Equal("Name", result.Candidates[0].MatchMethod);
        Assert.Equal(95, result.Candidates[0].MatchConfidence);
    }

    [Fact]
    public async Task FindCandidates_NameSubstringMatch_ReturnsLowerConfidence()
    {
        await using var fixture = LookupFixture.Create();
        await fixture.AddPartnerAsync(1, 50, "山东测试有限公司潍坊分公司", null);

        var result = await fixture.Service.FindCandidatesAsync(null, "山东测试");

        Assert.Equal(1, result.CandidateCount);
        Assert.Equal("Name", result.Candidates[0].MatchMethod);
        Assert.InRange(result.Candidates[0].MatchConfidence, 40, 94);
    }

    [Fact]
    public async Task FindCandidates_ShortNameMatch_ReturnsCandidate()
    {
        await using var fixture = LookupFixture.Create();
        await fixture.AddPartnerAsync(1, 50, "山东测试有限公司全称", null, shortName: "测试公司");

        var result = await fixture.Service.FindCandidatesAsync(null, "测试公司");

        Assert.Equal(1, result.CandidateCount);
        Assert.Equal(90, result.Candidates[0].MatchConfidence);
    }

    // ===== 无匹配 =====

    [Fact]
    public async Task FindCandidates_NoMatch_ReturnsEmptyList()
    {
        await using var fixture = LookupFixture.Create();
        await fixture.AddPartnerAsync(1, 50, "存在的公司", "91370700EXIST00001");

        var result = await fixture.Service.FindCandidatesAsync("91370700NOTEXIST01", "不存在的公司名");

        Assert.True(result.CreditCodeProvided);
        Assert.Equal(0, result.CandidateCount);
        Assert.Empty(result.Candidates);
    }

    [Fact]
    public async Task FindCandidates_BothInputsNull_ReturnsEmpty()
    {
        await using var fixture = LookupFixture.Create();
        await fixture.AddPartnerAsync(1, 50, "存在的公司", "91370700EXIST00001");

        var result = await fixture.Service.FindCandidatesAsync(null, null);

        Assert.False(result.CreditCodeProvided);
        Assert.Equal(0, result.CandidateCount);
    }

    // ===== 名称误匹配风险 =====

    [Fact]
    public async Task FindCandidates_ShortNameQuery_MayReturnManyFalsePositives()
    {
        // 业务边界：短名称子串匹配可能产生大量误匹配。
        // 这不是bug——候选匹配的本质就是可能有误匹配，需人工确认。
        // 本测试验证：名称匹配的置信度低于95，且排序在TaxNumber匹配之后。
        await using var fixture = LookupFixture.Create();
        await fixture.AddPartnerAsync(1, 50, "山东食品有限公司", null);
        await fixture.AddPartnerAsync(2, 50, "山东机械有限公司", null);
        await fixture.AddPartnerAsync(3, 50, "山东化工有限公司", null);

        var result = await fixture.Service.FindCandidatesAsync(null, "山东");

        // 三个都包含"山东"，全部返回（候选，需人工确认）
        Assert.Equal(3, result.CandidateCount);
        // 全部是名称匹配，置信度低于95
        Assert.All(result.Candidates, c =>
        {
            Assert.Equal("Name", c.MatchMethod);
            Assert.True(c.MatchConfidence < 95,
                $"Name match confidence should be < 95, got {c.MatchConfidence}");
        });
    }

    [Fact]
    public async Task FindCandidates_NameMatch_NeverMarkedAsSyncedOrLinked()
    {
        // 业务边界：名称匹配的候选不得标记为"已同步"或"已关联"。
        await using var fixture = LookupFixture.Create();
        await fixture.AddPartnerAsync(1, 50, "潍坊测试公司", null);

        var result = await fixture.Service.FindCandidatesAsync(null, "潍坊测试公司");

        Assert.Equal(1, result.CandidateCount);
        Assert.Equal("Name", result.Candidates[0].MatchMethod);
        Assert.NotEqual("Synced", result.Candidates[0].MatchMethod);
        Assert.NotEqual("Linked", result.Candidates[0].MatchMethod);
        Assert.NotEqual("Associated", result.Candidates[0].MatchMethod);
    }

    [Fact]
    public async Task FindCandidates_NoMatch_IsNormalResult_NotError()
    {
        // 业务边界：没有匹配结果是正常结果，不是错误。
        await using var fixture = LookupFixture.Create();
        await fixture.AddPartnerAsync(1, 50, "存在的公司", "91370700EXIST0001");

        var result = await fixture.Service.FindCandidatesAsync("91370700NOTEXIST", "不存在的公司");

        Assert.True(result.CreditCodeProvided);
        Assert.Equal(0, result.CandidateCount);
        Assert.NotNull(result.Candidates);
        Assert.Empty(result.Candidates);
    }

    // ===== 排序 =====

    [Fact]
    public async Task FindCandidates_TaxNumberMatchRanksAboveNameMatch()
    {
        await using var fixture = LookupFixture.Create();
        await fixture.AddPartnerAsync(1, 50, "潍坊食品公司", "91370700MATCH0001");
        await fixture.AddPartnerAsync(2, 50, "潍坊食品公司分店", null); // 名称包含"潍坊食品公司"

        var result = await fixture.Service.FindCandidatesAsync("91370700MATCH0001", "潍坊食品公司");

        Assert.Equal(2, result.CandidateCount);
        Assert.Equal("TaxNumber", result.Candidates[0].MatchMethod);
        Assert.Equal(100, result.Candidates[0].MatchConfidence);
        Assert.Equal("Name", result.Candidates[1].MatchMethod);
    }

    // ===== 只读验证 =====

    [Fact]
    public async Task FindCandidates_DoesNotModifyDatabase()
    {
        await using var fixture = LookupFixture.Create();
        await fixture.AddPartnerAsync(1, 50, "测试公司", "91370700READONLY01");
        var countBefore = await fixture.Db.BusinessPartners.CountAsync();

        await fixture.Service.FindCandidatesAsync("91370700READONLY01", "测试公司");

        var countAfter = await fixture.Db.BusinessPartners.CountAsync();
        Assert.Equal(countBefore, countAfter);
    }

    // ===== 结果不暴露TenantId =====

    [Fact]
    public async Task FindCandidates_ResultDoesNotExposeTenantId()
    {
        await using var fixture = LookupFixture.Create();
        await fixture.AddPartnerAsync(1, 50, "测试公司", "91370700TENANT01");

        var result = await fixture.Service.FindCandidatesAsync("91370700TENANT01", null);

        var dtoType = typeof(BusinessPartnerCandidateDto);
        Assert.DoesNotContain(dtoType.GetProperties(), p => p.Name == "TenantId");
        Assert.Equal((int)BusinessPartnerRole.Customer, result.Candidates[0].Role);
        Assert.Equal((int)MasterDataStatus.Active, result.Candidates[0].Status);
    }
}
