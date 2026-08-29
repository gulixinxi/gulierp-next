using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Application;
using GuliERP.Mdm.Domain.Entities;
using GuliERP.Mdm.Domain.Enums;
using GuliERP.Mdm.Infrastructure.Mdm;
using GuliERP.Mdm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GuliERP.Mdm.Tests;

/// <summary>
/// GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 — Wave 3
/// (2026-08-28) focused tests for the BusinessPartner
/// PostalAddress + MnemonicCode + Region binding + Country
/// validation extension. Per brief §二十, the focused test set
/// covers:
///   1. Create with MnemonicCode (round-trip)
///   2. Read returns MnemonicCode
///   3. Update MnemonicCode
///   4. Search by MnemonicCode
///   5. Search by ContactPerson
///   6. Search by Phone
///   7. Search by Email
///   8. Search by TaxNumber
///   9. Create with valid CountryCode
///  10. Create with invalid (unknown) CountryCode rejected
///  11. Create with RegionId matching CountryCode PASS
///  12. RegionId from another Country rejected
///  13. Legacy BP (RegionId = null) still loads with all text
///      fields preserved
///  14. Legacy text survives unrelated update (e.g. Phone change)
///  15. International free-text + RegionId null saves
///  16. Region selected => server snapshot derived from reference
///  17. Tenant isolation remains correct
///  18. Explicit existing Code remains unchanged
/// All tests use InMemory; cross-process / PG evidence is
/// Operator-side (Wave 5).
/// </summary>
public sealed class BusinessPartnerPostalAddressFacts
{
    // ----- 1. Create with MnemonicCode -----
    [Fact]
    public async Task Create_With_MnemonicCode_RoundTrips()
    {
        await using var fx = PostalAddressFixture.Create();
        await fx.SeedCountryAsync("CN");
        await fx.SeedCodeRuleAsync();

        var created = await fx.Service.CreateAsync(new CreateBusinessPartnerRequest(
            Code: "BP_MNEM_001",
            Name: "Mnemonic Partner",
            ShortName: "MP",
            Role: BusinessPartnerRole.Customer,
            ContactPerson: "Alice",
            Phone: "13900000001",
            Email: "alice@mp.example",
            AddressLine1: "Road 1",
            AddressLine2: null,
            City: "Shanghai",
            Region: "Shanghai",
            PostalCode: "200000",
            CountryCode: "CN",
            TaxNumber: null,
            MnemonicCode: "MP01",
            AdministrativeRegionId: null,
            Description: null));

        Assert.Equal("MP01", created.MnemonicCode);
        var fetched = await fx.Service.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal("MP01", fetched!.MnemonicCode);
    }

    // ----- 2. Update MnemonicCode -----
    [Fact]
    public async Task Update_MnemonicCode()
    {
        await using var fx = PostalAddressFixture.Create();
        await fx.SeedCountryAsync("CN");
        await fx.SeedCodeRuleAsync();
        var created = await fx.Service.CreateAsync(new CreateBusinessPartnerRequest(
            Code: "BP_MNEM_002", Name: "X", ShortName: null,
            Role: BusinessPartnerRole.Customer, ContactPerson: null, Phone: null, Email: null,
            AddressLine1: null, AddressLine2: null, City: null, Region: null,
            PostalCode: null, CountryCode: "CN", TaxNumber: null,
            MnemonicCode: "ORIG", AdministrativeRegionId: null, Description: null));

        var updated = await fx.Service.UpdateAsync(created.Id, new UpdateBusinessPartnerRequest(
            Name: "X", ShortName: null, Role: BusinessPartnerRole.Customer,
            ContactPerson: null, Phone: null, Email: null,
            AddressLine1: null, AddressLine2: null, City: null, Region: null,
            PostalCode: null, CountryCode: "CN", TaxNumber: null,
            MnemonicCode: "RENAMED", AdministrativeRegionId: null,
            Status: MasterDataStatus.Active, Description: null,
            ExpectedConcurrencyVersion: created.ConcurrencyVersion));

        Assert.NotNull(updated);
        Assert.Equal("RENAMED", updated!.MnemonicCode);
    }

    // ----- 3. Search by MnemonicCode -----
    [Fact]
    public async Task Search_By_MnemonicCode()
    {
        await using var fx = PostalAddressFixture.Create();
        await fx.SeedCountryAsync("CN");
        await fx.SeedCodeRuleAsync();
        await fx.CreateBPAsync("BP_FIND_001", "Apple", mnemonic: "APPL");
        await fx.CreateBPAsync("BP_FIND_002", "Banana", mnemonic: "BANA");
        await fx.CreateBPAsync("BP_FIND_003", "Cherry", mnemonic: "CHER");

        var page = await fx.Service.ListAsync(new BusinessPartnerListQuery(
            Keyword: "BANA", Role: null, Status: null, Page: 1, PageSize: 20));
        Assert.Single(page.Items);
        Assert.Equal("BP_FIND_002", page.Items[0].Code);
    }

    // ----- 4. Search by ContactPerson -----
    [Fact]
    public async Task Search_By_ContactPerson()
    {
        await using var fx = PostalAddressFixture.Create();
        await fx.SeedCountryAsync("CN");
        await fx.SeedCodeRuleAsync();
        await fx.CreateBPAsync("BP_CT_001", "Alpha", contact: "张三");
        await fx.CreateBPAsync("BP_CT_002", "Beta", contact: "李四");

        var page = await fx.Service.ListAsync(new BusinessPartnerListQuery(
            Keyword: "李四", Role: null, Status: null, Page: 1, PageSize: 20));
        Assert.Single(page.Items);
        Assert.Equal("BP_CT_002", page.Items[0].Code);
    }

    // ----- 5. Search by Phone -----
    [Fact]
    public async Task Search_By_Phone()
    {
        await using var fx = PostalAddressFixture.Create();
        await fx.SeedCountryAsync("CN");
        await fx.SeedCodeRuleAsync();
        await fx.CreateBPAsync("BP_PH_001", "Alpha", phone: "+86 21 1111 1111");
        await fx.CreateBPAsync("BP_PH_002", "Beta", phone: "+86 21 2222 2222");

        var page = await fx.Service.ListAsync(new BusinessPartnerListQuery(
            Keyword: "2222", Role: null, Status: null, Page: 1, PageSize: 20));
        Assert.Single(page.Items);
        Assert.Equal("BP_PH_002", page.Items[0].Code);
    }

    // ----- 6. Search by Email -----
    [Fact]
    public async Task Search_By_Email()
    {
        await using var fx = PostalAddressFixture.Create();
        await fx.SeedCountryAsync("CN");
        await fx.SeedCodeRuleAsync();
        await fx.CreateBPAsync("BP_EM_001", "Alpha", email: "alpha@foo.example");
        await fx.CreateBPAsync("BP_EM_002", "Beta", email: "beta@bar.example");

        var page = await fx.Service.ListAsync(new BusinessPartnerListQuery(
            Keyword: "@bar.example", Role: null, Status: null, Page: 1, PageSize: 20));
        Assert.Single(page.Items);
        Assert.Equal("BP_EM_002", page.Items[0].Code);
    }

    // ----- 7. Search by TaxNumber -----
    [Fact]
    public async Task Search_By_TaxNumber()
    {
        await using var fx = PostalAddressFixture.Create();
        await fx.SeedCountryAsync("CN");
        await fx.SeedCodeRuleAsync();
        await fx.CreateBPAsync("BP_TAX_001", "Alpha", tax: "TAX_AAA_001");
        await fx.CreateBPAsync("BP_TAX_002", "Beta", tax: "TAX_BBB_002");

        var page = await fx.Service.ListAsync(new BusinessPartnerListQuery(
            Keyword: "BBB", Role: null, Status: null, Page: 1, PageSize: 20));
        Assert.Single(page.Items);
        Assert.Equal("BP_TAX_002", page.Items[0].Code);
    }

    // ----- 8. Create with valid CountryCode -----
    [Fact]
    public async Task Create_With_Valid_CountryCode()
    {
        await using var fx = PostalAddressFixture.Create();
        await fx.SeedCountryAsync("CN");
        await fx.SeedCodeRuleAsync();

        var created = await fx.Service.CreateAsync(new CreateBusinessPartnerRequest(
            Code: "BP_CN_OK", Name: "Valid CN", ShortName: null,
            Role: BusinessPartnerRole.Customer, ContactPerson: null, Phone: null, Email: null,
            AddressLine1: null, AddressLine2: null, City: null, Region: null,
            PostalCode: null, CountryCode: "CN", TaxNumber: null,
            MnemonicCode: null, AdministrativeRegionId: null, Description: null));

        Assert.Equal("CN", created.CountryCode);
    }

    // ----- 9. Create with unknown CountryCode rejected -----
    [Fact]
    public async Task Create_With_Unknown_CountryCode_Rejected()
    {
        await using var fx = PostalAddressFixture.Create();
        await fx.SeedCodeRuleAsync(); // no CN seeded

        var ex = await Assert.ThrowsAsync<MdmValidationException>(() => fx.Service.CreateAsync(
            new CreateBusinessPartnerRequest(
                Code: "BP_BAD_CC", Name: "Bad CC", ShortName: null,
                Role: BusinessPartnerRole.Customer, ContactPerson: null, Phone: null, Email: null,
                AddressLine1: null, AddressLine2: null, City: null, Region: null,
                PostalCode: null, CountryCode: "ZZ", TaxNumber: null,
                MnemonicCode: null, AdministrativeRegionId: null, Description: null)));

        Assert.Equal(MdmErrorCodes.BusinessPartnerCountryCodeUnknown, ex.Code);
    }

    // ----- 10. Create with RegionId matching CountryCode PASS -----
    [Fact]
    public async Task Create_With_Region_Matching_Country_Pass()
    {
        await using var fx = PostalAddressFixture.Create();
        await fx.SeedCountryAsync("CN");
        await fx.SeedCodeRuleAsync();
        var regionId = await fx.SeedRegionAsync("CN", "110000", "北京市", level: 1);

        var created = await fx.Service.CreateAsync(new CreateBusinessPartnerRequest(
            Code: "BP_RG_OK", Name: "Beijing Inc", ShortName: null,
            Role: BusinessPartnerRole.Customer, ContactPerson: null, Phone: null, Email: null,
            AddressLine1: null, AddressLine2: null, City: "Beijing", Region: null,
            PostalCode: null, CountryCode: "CN", TaxNumber: null,
            MnemonicCode: null, AdministrativeRegionId: regionId, Description: null));

        Assert.Equal(regionId, created.AdministrativeRegionId);
        Assert.Equal("110000", created.RegionCodeSnapshot);
        Assert.Equal("北京市", created.RegionNameSnapshot);
    }

    // ----- 11. RegionId from another Country rejected -----
    [Fact]
    public async Task Create_With_Region_From_Another_Country_Rejected()
    {
        await using var fx = PostalAddressFixture.Create();
        await fx.SeedCountryAsync("CN");
        await fx.SeedCountryAsync("US");
        await fx.SeedCodeRuleAsync();
        var usRegionId = await fx.SeedRegionAsync("US", "06000", "California", level: 1);

        var ex = await Assert.ThrowsAsync<MdmValidationException>(() => fx.Service.CreateAsync(
            new CreateBusinessPartnerRequest(
                Code: "BP_CROSS", Name: "Cross", ShortName: null,
                Role: BusinessPartnerRole.Customer, ContactPerson: null, Phone: null, Email: null,
                AddressLine1: null, AddressLine2: null, City: null, Region: null,
                PostalCode: null, CountryCode: "CN", TaxNumber: null,
                MnemonicCode: null, AdministrativeRegionId: usRegionId, Description: null)));

        Assert.Equal(MdmErrorCodes.BusinessPartnerRegionCrossCountry, ex.Code);
    }

    // ----- 12. Legacy BP RegionId null still loads -----
    [Fact]
    public async Task Legacy_BP_With_Null_Region_Loads_All_Text_Fields()
    {
        await using var fx = PostalAddressFixture.Create();
        await fx.SeedCountryAsync("CN");
        await fx.SeedCodeRuleAsync();
        var created = await fx.Service.CreateAsync(new CreateBusinessPartnerRequest(
            Code: "BP_LEGACY", Name: "Legacy", ShortName: "LG",
            Role: BusinessPartnerRole.Customer, ContactPerson: "张老板",
            Phone: "021-12345678", Email: "lg@legacy.example",
            AddressLine1: "Old Road 1", AddressLine2: "Building 3",
            City: "Shanghai", Region: "上海市", PostalCode: "200000",
            CountryCode: "CN", TaxNumber: "LGD-TX-001",
            MnemonicCode: "LEGACY", AdministrativeRegionId: null,
            Description: "legacy 兼容测试"));

        var fetched = await fx.Service.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Null(fetched!.AdministrativeRegionId);
        Assert.Null(fetched.RegionCodeSnapshot);
        Assert.Null(fetched.RegionNameSnapshot);
        Assert.Equal("上海市", fetched.Region);
        Assert.Equal("Old Road 1", fetched.AddressLine1);
        Assert.Equal("Building 3", fetched.AddressLine2);
        Assert.Equal("LEGACY", fetched.MnemonicCode);
        Assert.Equal("LGD-TX-001", fetched.TaxNumber);
    }

    // ----- 13. Legacy text survives unrelated update (Phone change) -----
    [Fact]
    public async Task Legacy_Text_Survives_Unrelated_Update()
    {
        await using var fx = PostalAddressFixture.Create();
        await fx.SeedCountryAsync("CN");
        await fx.SeedCodeRuleAsync();
        var created = await fx.Service.CreateAsync(new CreateBusinessPartnerRequest(
            Code: "BP_KEEP", Name: "Keep", ShortName: null,
            Role: BusinessPartnerRole.Customer, ContactPerson: "张老板",
            Phone: "021-12345678", Email: "k@keep.example",
            AddressLine1: "Old Road 1", AddressLine2: null,
            City: "Shanghai", Region: "上海市", PostalCode: "200000",
            CountryCode: "CN", TaxNumber: "KEEP-TX-001",
            MnemonicCode: "KEEP", AdministrativeRegionId: null, Description: "desc"));

        // Update only Phone — every legacy address / contact / mnemonic
        // field must be preserved.
        var updated = await fx.Service.UpdateAsync(created.Id, new UpdateBusinessPartnerRequest(
            Name: "Keep", ShortName: null, Role: BusinessPartnerRole.Customer,
            ContactPerson: "张老板", Phone: "021-99999999", Email: "k@keep.example",
            AddressLine1: "Old Road 1", AddressLine2: null,
            City: "Shanghai", Region: "上海市", PostalCode: "200000",
            CountryCode: "CN", TaxNumber: "KEEP-TX-001",
            MnemonicCode: "KEEP", AdministrativeRegionId: null,
            Status: MasterDataStatus.Active, Description: "desc",
            ExpectedConcurrencyVersion: created.ConcurrencyVersion));

        Assert.NotNull(updated);
        Assert.Equal("021-99999999", updated!.Phone);
        Assert.Equal("Old Road 1", updated.AddressLine1);
        Assert.Equal("上海市", updated.Region);
        Assert.Equal("KEEP", updated.MnemonicCode);
        Assert.Equal("KEEP-TX-001", updated.TaxNumber);
        Assert.Equal("张老板", updated.ContactPerson);
        Assert.Null(updated.AdministrativeRegionId);
    }

    // ----- 14. International free-text + RegionId null saves -----
    [Fact]
    public async Task International_FreeText_RegionId_Null_Saves()
    {
        await using var fx = PostalAddressFixture.Create();
        await fx.SeedCountryAsync("US");
        await fx.SeedCodeRuleAsync();

        var created = await fx.Service.CreateAsync(new CreateBusinessPartnerRequest(
            Code: "BP_US_001", Name: "US Partner", ShortName: null,
            Role: BusinessPartnerRole.Supplier, ContactPerson: "John Doe",
            Phone: "+1 415 555 0100", Email: "john@us.example",
            AddressLine1: "100 Market St", AddressLine2: "Suite 200",
            City: "San Francisco", Region: "California",
            PostalCode: "94105", CountryCode: "US",
            TaxNumber: "US-EIN-12-3456789",
            MnemonicCode: "USP", AdministrativeRegionId: null,
            Description: "International free-text address — no region binding"));

        Assert.Equal("US", created.CountryCode);
        Assert.Equal("California", created.Region);
        Assert.Null(created.AdministrativeRegionId);
        Assert.Null(created.RegionCodeSnapshot);
    }

    // ----- 15. Region snapshot derived from reference data -----
    [Fact]
    public async Task Region_Snapshot_Derived_From_Reference_Data_Not_Client()
    {
        await using var fx = PostalAddressFixture.Create();
        await fx.SeedCountryAsync("CN");
        await fx.SeedCodeRuleAsync();
        var regionId = await fx.SeedRegionAsync("CN", "110100", "市辖区 (Beijing City proper)", level: 2);

        var created = await fx.Service.CreateAsync(new CreateBusinessPartnerRequest(
            Code: "BP_SNAP", Name: "Snap", ShortName: null,
            Role: BusinessPartnerRole.Customer, ContactPerson: null, Phone: null, Email: null,
            AddressLine1: null, AddressLine2: null, City: null, Region: null,
            PostalCode: null, CountryCode: "CN", TaxNumber: null,
            MnemonicCode: null, AdministrativeRegionId: regionId, Description: null));

        // The RegionNameSnapshot is the SERVER-derived value, not
        // any client-supplied text. The client cannot override it.
        Assert.Equal("市辖区 (Beijing City proper)", created.RegionNameSnapshot);
        Assert.Equal("110100", created.RegionCodeSnapshot);
    }

    // ----- 16. Tenant isolation -----
    [Fact]
    public async Task Tenant_Isolation_Remains_Correct()
    {
        await using var fx = PostalAddressFixture.Create();
        await fx.SeedCountryAsync("CN");
        await fx.SeedCodeRuleAsync(tenantId: 1);
        await fx.SeedCodeRuleAsync(tenantId: 2);

        var t1 = await fx.Service.CreateAsync(new CreateBusinessPartnerRequest(
            Code: "BP_T1", Name: "T1", ShortName: null,
            Role: BusinessPartnerRole.Customer, ContactPerson: null, Phone: null, Email: null,
            AddressLine1: null, AddressLine2: null, City: null, Region: null,
            PostalCode: null, CountryCode: "CN", TaxNumber: null,
            MnemonicCode: null, AdministrativeRegionId: null, Description: null));

        // Switch to tenant 2
        fx.CurrentTenant.Set(2);
        var fetchedT2 = await fx.Service.GetByIdAsync(t1.Id);
        Assert.Null(fetchedT2); // 404 semantics

        fx.CurrentTenant.Set(1);
        var fetchedT1 = await fx.Service.GetByIdAsync(t1.Id);
        Assert.NotNull(fetchedT1);
        Assert.Equal("BP_T1", fetchedT1!.Code);
    }

    // ----- 17. Explicit existing Code preserved -----
    [Fact]
    public async Task Explicit_Existing_Code_Preserved_After_Wave3()
    {
        await using var fx = PostalAddressFixture.Create();
        await fx.SeedCountryAsync("CN");
        await fx.SeedCodeRuleAsync();

        // Legacy long code (e.g. "BP_CUST_RETAIL_01") continues to work.
        var created = await fx.Service.CreateAsync(new CreateBusinessPartnerRequest(
            Code: "BP_CUST_RETAIL_01", Name: "Retail 01", ShortName: null,
            Role: BusinessPartnerRole.Customer, ContactPerson: null, Phone: null, Email: null,
            AddressLine1: null, AddressLine2: null, City: null, Region: null,
            PostalCode: null, CountryCode: "CN", TaxNumber: null,
            MnemonicCode: null, AdministrativeRegionId: null, Description: null));

        Assert.Equal("BP_CUST_RETAIL_01", created.Code);
    }

    // ----- 18. Empty Code auto-generates with Country validation -----
    [Fact]
    public async Task Empty_Code_With_Invalid_Country_Still_Rejects()
    {
        await using var fx = PostalAddressFixture.Create();
        await fx.SeedCodeRuleAsync();
        // No country seeded.

        var ex = await Assert.ThrowsAsync<MdmValidationException>(() => fx.Service.CreateAsync(
            new CreateBusinessPartnerRequest(
                Code: "",  // empty => auto-generate
                Name: "Auto CC", ShortName: null,
                Role: BusinessPartnerRole.Customer, ContactPerson: null, Phone: null, Email: null,
                AddressLine1: null, AddressLine2: null, City: null, Region: null,
                PostalCode: null, CountryCode: "ZZ", TaxNumber: null,
                MnemonicCode: null, AdministrativeRegionId: null, Description: null)));

        Assert.Equal(MdmErrorCodes.BusinessPartnerCountryCodeUnknown, ex.Code);
    }

    // ----- 19. Update with cleared region binding also clears snapshot -----
    [Fact]
    public async Task Update_Clears_Region_Binding_And_Snapshots()
    {
        await using var fx = PostalAddressFixture.Create();
        await fx.SeedCountryAsync("CN");
        await fx.SeedCodeRuleAsync();
        var regionId = await fx.SeedRegionAsync("CN", "110000", "北京市", level: 1);

        var created = await fx.Service.CreateAsync(new CreateBusinessPartnerRequest(
            Code: "BP_CLEAR", Name: "Clear", ShortName: null,
            Role: BusinessPartnerRole.Customer, ContactPerson: null, Phone: null, Email: null,
            AddressLine1: null, AddressLine2: null, City: null, Region: null,
            PostalCode: null, CountryCode: "CN", TaxNumber: null,
            MnemonicCode: null, AdministrativeRegionId: regionId, Description: null));
        Assert.Equal("北京市", created.RegionNameSnapshot);

        var updated = await fx.Service.UpdateAsync(created.Id, new UpdateBusinessPartnerRequest(
            Name: "Clear", ShortName: null, Role: BusinessPartnerRole.Customer,
            ContactPerson: null, Phone: null, Email: null,
            AddressLine1: null, AddressLine2: null, City: null, Region: null,
            PostalCode: null, CountryCode: "CN", TaxNumber: null,
            MnemonicCode: null, AdministrativeRegionId: null,
            Status: MasterDataStatus.Active, Description: null,
            ExpectedConcurrencyVersion: created.ConcurrencyVersion));

        Assert.NotNull(updated);
        Assert.Null(updated!.AdministrativeRegionId);
        Assert.Null(updated.RegionCodeSnapshot);
        Assert.Null(updated.RegionNameSnapshot);
    }

    // ----- Fixture -----
    private sealed class PostalAddressFixture : IAsyncDisposable
    {
        private PostalAddressFixture(
            MdmDbContext db,
            MdmBusinessPartnerService service,
            MutableCurrentTenant currentTenant)
        {
            Db = db;
            Service = service;
            CurrentTenant = currentTenant;
        }

        public MdmDbContext Db { get; }
        public MdmBusinessPartnerService Service { get; }
        public MutableCurrentTenant CurrentTenant { get; }

        public static PostalAddressFixture Create()
        {
            var db = new MdmDbContext(new DbContextOptionsBuilder<MdmDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options);
            var currentTenant = new MutableCurrentTenant(1);
            var currentUser = new MutableCurrentUser(99);
            var codeService = new MasterDataCodeService(
                db, NullLogger<MasterDataCodeService>.Instance);
            var service = new MdmBusinessPartnerService(
                db, currentTenant, currentUser, codeService,
                NullLogger<MdmBusinessPartnerService>.Instance);
            return new PostalAddressFixture(db, service, currentTenant);
        }

        public async Task SeedCountryAsync(string code)
        {
            if (await Db.Countries.AnyAsync(c => c.Code == code)) return;
            var now = DateTimeOffset.UtcNow;
            Db.Countries.Add(new Country
            {
                Code = code,
                Alpha3Code = code.Length == 2 ? code + "X" : null,
                Name = code,
                EnglishName = code,
                IsActive = true,
                SortOrder = 1,
                CreatedAt = now,
                ModifiedAt = now,
                ConcurrencyVersion = 1,
            });
            await Db.SaveChangesAsync();
        }

        public async Task SeedCodeRuleAsync(long tenantId = 1)
        {
            if (await Db.MasterDataCodeRules.AnyAsync(r => r.TenantId == tenantId
                && r.EntityType == "BusinessPartner")) return;
            var now = DateTimeOffset.UtcNow;
            var rule = new MasterDataCodeRule
            {
                TenantId = tenantId,
                EntityType = "BusinessPartner",
                Mode = MasterDataCodeMode.AutoEditable,
                Prefix = "BP",
                Separator = "_",
                SequenceLength = 6,
                StartValue = 1,
                IsActive = true,
                CreatedAt = now,
                ModifiedAt = now,
                ConcurrencyVersion = 1,
            };
            Db.MasterDataCodeRules.Add(rule);
            await Db.SaveChangesAsync();
            Db.MasterDataCodeSequenceStates.Add(new MasterDataCodeSequenceState
            {
                RuleId = rule.Id,
                TenantId = tenantId,
                CurrentValue = 0,
                CreatedAt = now,
                ModifiedAt = now,
                ConcurrencyVersion = 1,
            });
            await Db.SaveChangesAsync();
        }

        public async Task<long> SeedRegionAsync(string countryCode, string code, string name,
            int level, string? name2 = null)
        {
            var now = DateTimeOffset.UtcNow;
            var region = new AdministrativeRegion
            {
                CountryCode = countryCode,
                Code = code,
                Name = name2 ?? name,
                Level = level,
                RegionType = level == 1 ? "province" : "prefecture",
                IsActive = true,
                SortOrder = 1,
                CreatedAt = now,
                ModifiedAt = now,
                ConcurrencyVersion = 1,
            };
            Db.AdministrativeRegions.Add(region);
            await Db.SaveChangesAsync();
            return region.Id;
        }

        public async Task CreateBPAsync(
            string code, string name,
            string? mnemonic = null, string? contact = null, string? phone = null,
            string? email = null, string? tax = null)
        {
            await Service.CreateAsync(new CreateBusinessPartnerRequest(
                Code: code, Name: name, ShortName: null,
                Role: BusinessPartnerRole.Customer,
                ContactPerson: contact, Phone: phone, Email: email,
                AddressLine1: null, AddressLine2: null, City: null, Region: null,
                PostalCode: null, CountryCode: "CN", TaxNumber: tax,
                MnemonicCode: mnemonic, AdministrativeRegionId: null,
                Description: null));
        }

        public async ValueTask DisposeAsync() => await Db.DisposeAsync();
    }

    // Local mutable fakes for ICurrentTenant / ICurrentUser. We
    // cannot reuse the public MutableCurrentTenant in
    // NumberingRuleServiceFacts.cs because its companion
    // MutableCurrentUser is private to that class, and we do
    // not want to mutate the existing test class to expose it.
    // These fakes are intentionally minimal: only the
    // <c>Set</c> helper (for tenant switching in
    // Tenant_Isolation_Remains_Correct) and the read-only
    // <c>Id</c> used by the BusinessPartner service.
    public sealed class MutableCurrentTenant(long id) : ICurrentTenant
    {
        public long? Id { get; private set; } = id;
        public string? Name => null;
        public bool IsAvailable => Id.HasValue;
        public IDisposable Change(long? tenantId)
        {
            var previous = Id;
            Id = tenantId;
            return new Restore(() => Id = previous);
        }
        public void Set(long? tenantId) => Id = tenantId;
    }

    private sealed class MutableCurrentUser(long id) : ICurrentUser
    {
        public long? Id { get; private set; } = id;
        public string? UserName => "tester";
        public bool IsAuthenticated => Id.HasValue;
        public bool IsPlatformAdmin => false;
        public IDisposable Change(long? userId)
        {
            var previous = Id;
            Id = userId;
            return new Restore(() => Id = previous);
        }
    }

    private sealed class Restore(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }
}
