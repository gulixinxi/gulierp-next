using GuliERP.Crm.Application;
using GuliERP.Crm.Domain.Entities;
using GuliERP.Crm.Domain.Enums;
using GuliERP.Crm.Infrastructure.Persistence;
using GuliERP.Crm.Infrastructure.Services;
using GuliERP.Mdm.Application;
using GuliERP.Mdm.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GuliERP.Crm.Tests;

/// <summary>
/// Unit tests for CompanyErpMatchService (CRM layer).
/// Covers: tenant filtering, company access scope, credit code missing,
/// no match, MDM lookup delegation (no direct MdmDbContext), read-only.
/// </summary>
public sealed class CompanyErpMatchServiceFacts
{
    private sealed class MatchFixture : IAsyncDisposable
    {
        public CrmDbContext CrmDb { get; }
        public Mock<IBusinessPartnerCandidateLookupService> MdmLookup { get; }
        public MutableCurrentTenant Tenant { get; }
        public MutableCurrentCompany Company { get; }
        public CompanyErpMatchService Service { get; }

        private MatchFixture(
            CrmDbContext crmDb,
            Mock<IBusinessPartnerCandidateLookupService> mdmLookup,
            MutableCurrentTenant tenant,
            MutableCurrentCompany company,
            CompanyErpMatchService service)
        {
            CrmDb = crmDb;
            MdmLookup = mdmLookup;
            Tenant = tenant;
            Company = company;
            Service = service;
        }

        public static MatchFixture Create(long tenantId = 50, long? companyId = 51)
        {
            var tenant = new MutableCurrentTenant(tenantId);
            var company = new MutableCurrentCompany(companyId);
            var crmDb = new CrmDbContext(new DbContextOptionsBuilder<CrmDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options, tenant, company);
            var mdmLookup = new Mock<IBusinessPartnerCandidateLookupService>();
            var service = new CompanyErpMatchService(
                crmDb, mdmLookup.Object, tenant, company);
            return new MatchFixture(crmDb, mdmLookup, tenant, company, service);
        }

        public async Task<Company> AddCrmCompanyAsync(
            long id, long tenantId, long companyId, string fullName,
            string? creditCode = null, long? ownerId = null)
        {
            // Switch context to the target tenant/company so SaveChanges
            // auto-fills the scope (cross-tenant explicit values are rejected).
            using var tScope = Tenant.Change(tenantId);
            using var cScope = Company.Change(companyId);

            var c = new Company
            {
                Id = id,
                CompanyNo = $"CO{id:D6}",
                FullName = fullName,
                CreditCode = creditCode,
                City = CompanyCity.Weifang,
                Industry = CompanyIndustry.Food,
                Source = SourceChannel.Other,
                Status = CustomerStatus.ToContact,
                OwnerId = ownerId,
                CreatedAt = DateTimeOffset.UtcNow,
                ModifiedAt = DateTimeOffset.UtcNow
            };
            CrmDb.Companies.Add(c);
            await CrmDb.SaveChangesAsync();
            return c;
        }

        /// <summary>Setup MDM lookup to return specific candidates.</summary>
        public void SetupMdmLookup(params BusinessPartnerCandidateDto[] candidates)
        {
            MdmLookup.Setup(s => s.FindCandidatesAsync(
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BusinessPartnerCandidateLookupResult(
                    candidates.Any(c => c.MatchMethod == "TaxNumber"),
                    candidates.Length,
                    candidates));
        }

        public static BusinessPartnerCandidateDto MakeCandidate(
            long id, string name, string? taxNumber, string matchMethod, int confidence)
            => new(
                id, $"BP{id:D6}", name, null, (int)BusinessPartnerRole.Customer,
                taxNumber, null, null, null, null, null,
                (int)MasterDataStatus.Active, matchMethod, confidence);

        public ValueTask DisposeAsync() => CrmDb.DisposeAsync();
    }

    // ===== 同租户过滤 =====

    [Fact]
    public async Task GetCandidates_CompanyFromOtherTenant_ReturnsNull()
    {
        await using var fixture = MatchFixture.Create(tenantId: 50);
        await fixture.AddCrmCompanyAsync(1, 99, 51, "其他租户的公司");

        var result = await fixture.Service.GetCandidatesAsync(1);

        Assert.Null(result);
        fixture.MdmLookup.Verify(s => s.FindCandidatesAsync(
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetCandidates_NoTenantContext_ReturnsNull()
    {
        await using var fixture = MatchFixture.Create(tenantId: 50);
        await fixture.AddCrmCompanyAsync(1, 50, 51, "测试公司");
        fixture.Tenant.Set(null);

        var result = await fixture.Service.GetCandidatesAsync(1);

        Assert.Null(result);
    }

    // ===== CRM公司访问范围（CompanyId）=====

    [Fact]
    public async Task GetCandidates_CompanyFromOtherCompany_ReturnsNull()
    {
        await using var fixture = MatchFixture.Create(tenantId: 50, companyId: 51);
        await fixture.AddCrmCompanyAsync(1, 50, 99, "其他公司的客户"); // CompanyId=99 != 51

        var result = await fixture.Service.GetCandidatesAsync(1);

        Assert.Null(result);
        fixture.MdmLookup.Verify(s => s.FindCandidatesAsync(
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetCandidates_CompanyContextNull_DeniesAccess()
    {
        await using var fixture = MatchFixture.Create(tenantId: 50, companyId: null);
        await fixture.AddCrmCompanyAsync(1, 50, 99, "任意公司的客户");
        fixture.SetupMdmLookup();

        var result = await fixture.Service.GetCandidatesAsync(1);

        Assert.Null(result);
        fixture.MdmLookup.Verify(s => s.FindCandidatesAsync(
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetCandidates_CompanyFromSameCompany_Succeeds()
    {
        await using var fixture = MatchFixture.Create(tenantId: 50, companyId: 51);
        await fixture.AddCrmCompanyAsync(1, 50, 51, "本公司的客户");
        fixture.SetupMdmLookup();

        var result = await fixture.Service.GetCandidatesAsync(1);

        Assert.NotNull(result);
        Assert.Equal("本公司的客户", result!.CrmCompanyName);
    }

    // ===== 信用代码缺失 =====

    [Fact]
    public async Task GetCandidates_CreditCodeMissing_PassesNullToMdmLookup()
    {
        await using var fixture = MatchFixture.Create();
        await fixture.AddCrmCompanyAsync(1, 50, 51, "无信用代码公司", creditCode: null);
        fixture.SetupMdmLookup();

        await fixture.Service.GetCandidatesAsync(1);

        fixture.MdmLookup.Verify(s => s.FindCandidatesAsync(
            null, "无信用代码公司", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCandidates_CreditCodeMissing_ResultMarksCreditCodeProvidedFalse()
    {
        await using var fixture = MatchFixture.Create();
        await fixture.AddCrmCompanyAsync(1, 50, 51, "无信用代码公司", creditCode: null);
        fixture.SetupMdmLookup();

        var result = await fixture.Service.GetCandidatesAsync(1);

        Assert.NotNull(result);
        Assert.False(result!.CreditCodeProvided);
    }

    [Fact]
    public async Task GetCandidates_CreditCodePresent_PassesToMdmLookup()
    {
        await using var fixture = MatchFixture.Create();
        await fixture.AddCrmCompanyAsync(1, 50, 51, "有信用代码公司", creditCode: "91370700MA12345678");
        fixture.SetupMdmLookup();

        await fixture.Service.GetCandidatesAsync(1);

        fixture.MdmLookup.Verify(s => s.FindCandidatesAsync(
            "91370700MA12345678", "有信用代码公司", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ===== 无匹配 =====

    [Fact]
    public async Task GetCandidates_NoMdmMatch_ReturnsEmptyCandidates()
    {
        await using var fixture = MatchFixture.Create();
        await fixture.AddCrmCompanyAsync(1, 50, 51, "测试公司", creditCode: "91370700NOMATCH01");
        fixture.SetupMdmLookup(); // 空候选

        var result = await fixture.Service.GetCandidatesAsync(1);

        Assert.NotNull(result);
        Assert.Equal(0, result!.CandidateCount);
        Assert.Empty(result.Candidates);
    }

    // ===== 候选匹配结果映射 =====

    [Fact]
    public async Task GetCandidates_MapsMdmCandidatesToErpMatchCandidateDto()
    {
        await using var fixture = MatchFixture.Create();
        await fixture.AddCrmCompanyAsync(1, 50, 51, "测试公司", creditCode: "91370700MAP00001");
        var candidate = MatchFixture.MakeCandidate(100, "MDM客商A", "91370700MAP00001", "TaxNumber", 100);
        fixture.SetupMdmLookup(candidate);

        var result = await fixture.Service.GetCandidatesAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result!.CandidateCount);
        Assert.Equal(100, result.Candidates[0].ErpPartnerId);
        Assert.Equal("MDM客商A", result.Candidates[0].ErpPartnerName);
        Assert.Equal((int)BusinessPartnerRole.Customer, result.Candidates[0].ErpPartnerRole);
        Assert.Equal((int)MasterDataStatus.Active, result.Candidates[0].Status);
        Assert.Equal("91370700MAP00001", result.Candidates[0].TaxNumber);
        Assert.Equal("TaxNumber", result.Candidates[0].MatchMethod);
        Assert.Equal(100, result.Candidates[0].MatchConfidence);
    }

    [Fact]
    public async Task GetCandidates_ResultDoesNotContainSyncedStatus()
    {
        await using var fixture = MatchFixture.Create();
        await fixture.AddCrmCompanyAsync(1, 50, 51, "测试公司");
        fixture.SetupMdmLookup(MatchFixture.MakeCandidate(100, "MDM客商", null, "Name", 80));

        var result = await fixture.Service.GetCandidatesAsync(1);

        // 验证DTO中没有"已同步"相关字段
        var resultType = typeof(ErpMatchResultDto);
        Assert.DoesNotContain(resultType.GetProperties(), p =>
            p.Name.Contains("Sync", StringComparison.OrdinalIgnoreCase) ||
            p.Name.Contains("Linked", StringComparison.OrdinalIgnoreCase) ||
            p.Name.Contains("Associated", StringComparison.OrdinalIgnoreCase));

        var candidateType = typeof(ErpMatchCandidateDto);
        Assert.DoesNotContain(candidateType.GetProperties(), p =>
            p.Name.Contains("Sync", StringComparison.OrdinalIgnoreCase) ||
            p.Name.Contains("Linked", StringComparison.OrdinalIgnoreCase) ||
            p.Name.Contains("Associated", StringComparison.OrdinalIgnoreCase));
    }

    // ===== 不直接使用MdmDbContext（架构验证）=====

    [Fact]
    public void CompanyErpMatchService_DependsOnLookupInterface_NotMdmDbContext()
    {
        var constructors = typeof(CompanyErpMatchService).GetConstructors();
        Assert.NotEmpty(constructors);

        var paramTypes = constructors.SelectMany(c => c.GetParameters().Select(p => p.ParameterType)).ToList();

        // 必须依赖IBusinessPartnerCandidateLookupService
        Assert.Contains(typeof(IBusinessPartnerCandidateLookupService), paramTypes);

        // 不得依赖MdmDbContext
        var mdmDbContextType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Array.Empty<Type>(); }
            })
            .FirstOrDefault(t => t.Name == "MdmDbContext");
        if (mdmDbContextType is not null)
        {
            Assert.DoesNotContain(mdmDbContextType, paramTypes);
        }
    }

    // ===== 只读验证 =====

    [Fact]
    public async Task GetCandidates_DoesNotModifyCrmDatabase()
    {
        await using var fixture = MatchFixture.Create();
        await fixture.AddCrmCompanyAsync(1, 50, 51, "测试公司");
        fixture.SetupMdmLookup();
        var countBefore = await fixture.CrmDb.Companies.CountAsync();

        await fixture.Service.GetCandidatesAsync(1);

        var countAfter = await fixture.CrmDb.Companies.CountAsync();
        Assert.Equal(countBefore, countAfter);
    }

    // ===== 客户不存在 =====

    [Fact]
    public async Task GetCandidates_CompanyNotFound_ReturnsNull()
    {
        await using var fixture = MatchFixture.Create();

        var result = await fixture.Service.GetCandidatesAsync(999);

        Assert.Null(result);
        fixture.MdmLookup.Verify(s => s.FindCandidatesAsync(
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ===== 查询时间戳 =====

    [Fact]
    public async Task GetCandidates_ResultIncludesQueriedAt()
    {
        await using var fixture = MatchFixture.Create();
        await fixture.AddCrmCompanyAsync(1, 50, 51, "测试公司");
        fixture.SetupMdmLookup();

        var before = DateTimeOffset.UtcNow;
        var result = await fixture.Service.GetCandidatesAsync(1);
        var after = DateTimeOffset.UtcNow;

        Assert.NotNull(result);
        Assert.InRange(result!.QueriedAt, before, after);
    }
}
