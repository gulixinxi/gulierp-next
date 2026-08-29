using GuliERP.Mdm.Application;
using GuliERP.Mdm.Domain.Entities;
using GuliERP.Mdm.Infrastructure.Persistence;
using GuliERP.Mdm.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GuliERP.Mdm.Tests;

public sealed class MdmReferenceDataServiceFacts
{
    [Fact]
    public async Task EnsureSeed_Populates_All_249_Iso_Countries()
    {
        await using var fx = ReferenceDataFixture.Create();

        var result = await fx.Service.EnsureSeedAsync();

        Assert.Equal(Iso3166CountrySeedData.ExpectedRowCount, result.CountriesUpserted);
        var countries = await fx.Db.Countries.AsNoTracking().ToListAsync();
        Assert.Equal(249, countries.Count);
    }

    [Fact]
    public async Task EnsureSeed_Is_Idempotent_Second_Run_Reuses_Row_Count()
    {
        await using var fx = ReferenceDataFixture.Create();
        await fx.Service.EnsureSeedAsync();
        var second = await fx.Service.EnsureSeedAsync();
        Assert.Equal(Iso3166CountrySeedData.ExpectedRowCount, second.CountriesUpserted);
        var countries = await fx.Db.Countries.AsNoTracking().ToListAsync();
        Assert.Equal(249, countries.Count);
    }

    [Fact]
    public async Task GetCountryByCode_Normalizes_To_Upper()
    {
        await using var fx = ReferenceDataFixture.Create();
        await fx.Service.EnsureSeedAsync();

        var cn = await fx.Service.GetCountryByCodeAsync("cn");
        Assert.NotNull(cn);
        Assert.Equal("CN", cn!.Code);
        Assert.Equal("CHN", cn.Alpha3Code);
        Assert.Equal("中国", cn.Name);
        Assert.Equal("China", cn.EnglishName);
        Assert.True(cn.IsActive);
    }

    [Fact]
    public async Task GetCountryByCode_Mixed_Case_And_Spaces_Normalize()
    {
        await using var fx = ReferenceDataFixture.Create();
        await fx.Service.EnsureSeedAsync();

        var us = await fx.Service.GetCountryByCodeAsync("  us  ");
        Assert.NotNull(us);
        Assert.Equal("US", us!.Code);
    }

    [Fact]
    public async Task GetCountryByCode_Unknown_Returns_Null()
    {
        await using var fx = ReferenceDataFixture.Create();
        await fx.Service.EnsureSeedAsync();

        var result = await fx.Service.GetCountryByCodeAsync("ZZ");
        Assert.Null(result);
    }

    [Fact]
    public async Task ListCountries_Keyword_Filters_Code_Name_Alpha3()
    {
        await using var fx = ReferenceDataFixture.Create();
        await fx.Service.EnsureSeedAsync();

        // Code "JP" matches only Japan — no other Code contains "JP".
        var byCode = await fx.Service.ListCountriesAsync("JP");
        Assert.Single(byCode);
        Assert.Equal("JP", byCode[0].Code);

        // Alpha3 "JPN" matches only Japan.
        var byAlpha3 = await fx.Service.ListCountriesAsync("JPN");
        Assert.Single(byAlpha3);
        Assert.Equal("JP", byAlpha3[0].Code);

        // EnglishName "JAPAN" matches only Japan.
        var byName = await fx.Service.ListCountriesAsync("JAPAN");
        Assert.Single(byName);
        Assert.Equal("JP", byName[0].Code);

        // Substring: "ita" matches both Italy and Lithuania, so
        // the result is multiple rows. We just assert that Italy
        // is in the result.
        var byPartial = await fx.Service.ListCountriesAsync("ita");
        Assert.Contains(byPartial, c => c.Code == "IT");
    }

    [Fact]
    public async Task ListRegions_Empty_After_Seed_By_Default()
    {
        await using var fx = ReferenceDataFixture.Create();
        await fx.Service.EnsureSeedAsync();

        var cn = await fx.Service.ListRegionsAsync("CN");
        Assert.Empty(cn);
    }

    [Fact]
    public async Task ListRegions_Returns_Rows_After_Manual_Insert()
    {
        await using var fx = ReferenceDataFixture.Create();
        await fx.Service.EnsureSeedAsync();

        var now = DateTimeOffset.UtcNow;
        fx.Db.AdministrativeRegions.Add(new AdministrativeRegion
        {
            CountryCode = "CN",
            Code = "110000",
            Name = "北京市",
            ShortName = "京",
            ParentId = null,
            Level = 1,
            RegionType = "special-municipality",
            IsActive = true,
            SortOrder = 1,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        });
        await fx.Db.SaveChangesAsync();

        var roots = await fx.Service.ListRegionsAsync("CN");
        Assert.Single(roots);
        Assert.Equal("110000", roots[0].Code);
        Assert.Equal("北京市", roots[0].Name);
        Assert.Null(roots[0].ParentId);
        Assert.Equal(1, roots[0].Level);
    }

    [Fact]
    public async Task GetRegion_Returns_Manual_Inserted_Row()
    {
        await using var fx = ReferenceDataFixture.Create();
        var now = DateTimeOffset.UtcNow;
        var bj = new AdministrativeRegion
        {
            CountryCode = "CN", Code = "110000", Name = "北京市", ShortName = "京",
            Level = 1, RegionType = "special-municipality",
            IsActive = true, SortOrder = 1,
            CreatedAt = now, ModifiedAt = now, ConcurrencyVersion = 1,
        };
        fx.Db.AdministrativeRegions.Add(bj);
        await fx.Db.SaveChangesAsync();
        var bjDist = new AdministrativeRegion
        {
            CountryCode = "CN", Code = "110101", Name = "东城区", ParentId = bj.Id,
            Level = 2, RegionType = "district",
            IsActive = true, SortOrder = 1,
            CreatedAt = now, ModifiedAt = now, ConcurrencyVersion = 1,
        };
        fx.Db.AdministrativeRegions.Add(bjDist);
        await fx.Db.SaveChangesAsync();

        var fetched = await fx.Service.GetRegionAsync("CN", "110101");
        Assert.NotNull(fetched);
        Assert.Equal("东城区", fetched!.Name);
        Assert.Equal(bj.Id, fetched.ParentId);
    }

    [Fact]
    public async Task GetRegionChain_Returns_Root_To_Self_Ordered()
    {
        await using var fx = ReferenceDataFixture.Create();
        var now = DateTimeOffset.UtcNow;
        var root = new AdministrativeRegion
        {
            CountryCode = "CN", Code = "110000", Name = "北京市",
            Level = 1, RegionType = "special-municipality",
            IsActive = true, SortOrder = 1,
            CreatedAt = now, ModifiedAt = now, ConcurrencyVersion = 1,
        };
        fx.Db.AdministrativeRegions.Add(root);
        await fx.Db.SaveChangesAsync();
        var child = new AdministrativeRegion
        {
            CountryCode = "CN", Code = "110101", Name = "东城区",
            ParentId = root.Id, Level = 2, RegionType = "district",
            IsActive = true, SortOrder = 1,
            CreatedAt = now, ModifiedAt = now, ConcurrencyVersion = 1,
        };
        fx.Db.AdministrativeRegions.Add(child);
        await fx.Db.SaveChangesAsync();

        var chain = await fx.Service.GetRegionChainAsync("CN", "110101");
        Assert.Equal(2, chain.Count);
        Assert.Equal("110000", chain[0].Code);
        Assert.Equal("110101", chain[1].Code);
    }

    [Fact]
    public async Task EnsureMcaCnSeed_Imports_Province_City_County_Hierarchy()
    {
        await using var fx = ReferenceDataFixture.Create();
        var path = Path.Combine(Path.GetTempPath(), $"gulierp-mca-cn-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, """
        {
          "data": {
            "code": "00",
            "name": "中国",
            "level": 0,
            "type": "country",
            "children": [
              {
                "code": "110000000000",
                "name": "北京市",
                "level": 1,
                "type": "province",
                "children": [
                  {
                    "code": "110100000000",
                    "name": "市辖区",
                    "level": 2,
                    "type": "prefecture",
                    "children": [
                      {
                        "code": "110101000000",
                        "name": "东城区",
                        "level": 3,
                        "type": "county",
                        "children": []
                      }
                    ]
                  }
                ]
              }
            ]
          }
        }
        """);

        try
        {
            var result = await fx.Service.EnsureMcaCnSeedAsync(path);

            Assert.Equal(3, result.TotalSeen);
            Assert.Equal(3, result.Inserted);
            Assert.Equal(0, result.Rejected);
            var province = await fx.Service.GetRegionAsync("CN", "110000");
            var city = await fx.Service.GetRegionAsync("CN", "110100");
            var county = await fx.Service.GetRegionAsync("CN", "110101");
            Assert.NotNull(province);
            Assert.NotNull(city);
            Assert.NotNull(county);
            Assert.Null(province!.ParentId);
            Assert.Equal(province.Id, city!.ParentId);
            Assert.Equal(city.Id, county!.ParentId);
            var chain = await fx.Service.GetRegionChainAsync("CN", "110101");
            Assert.Equal(new[] { "110000", "110100", "110101" }, chain.Select(x => x.Code));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task EnsureMcaCnSeed_Imports_Operator_Snapshot_When_Present()
    {
        var repoRoot = FindRepoRoot();
        if (repoRoot is null) return;
        var snapshot = Path.Combine(
            repoRoot.FullName,
            "artifacts",
            "operator",
            "mdm-foundation",
            "mca-cn.json");
        if (!File.Exists(snapshot)) return;

        await using var fx = ReferenceDataFixture.Create();

        var result = await fx.Service.EnsureMcaCnSeedAsync(snapshot);

        Assert.True(result.TotalSeen >= 3000);
        Assert.Equal(0, result.Rejected);
        var all = await fx.Db.AdministrativeRegions.AsNoTracking()
            .Where(x => x.CountryCode == "CN")
            .ToListAsync();
        Assert.Equal(result.TotalSeen, all.Count);
        Assert.True(all.Count(x => x.ParentId == null) >= 30);
        Assert.Equal(all.Count, all.Select(x => x.Code).Distinct(StringComparer.Ordinal).Count());

        var chain = await fx.Service.GetRegionChainAsync("CN", "110101");
        Assert.True(chain.Count >= 2);
        Assert.Equal("110000", chain[0].Code);
        Assert.Equal("110101", chain[^1].Code);
    }

    [Fact]
    public async Task Seed_Matches_Iso_3166_1_Row_Count()
    {
        // Per brief §十五: seed row count must match the dataset
        // count so the manifest can be verified at runtime.
        await using var fx = ReferenceDataFixture.Create();
        await fx.Service.EnsureSeedAsync();

        var cn = await fx.Db.Countries.AsNoTracking().CountAsync();
        Assert.Equal(Iso3166CountrySeedData.ExpectedRowCount, cn);
    }

    private sealed class ReferenceDataFixture : IAsyncDisposable
    {
        private ReferenceDataFixture(MdmDbContext db, MdmReferenceDataService service)
        {
            Db = db;
            Service = service;
        }

        public MdmDbContext Db { get; }
        public MdmReferenceDataService Service { get; }

        public static ReferenceDataFixture Create()
        {
            var root = new InMemoryDatabaseRoot();
            var dbName = Guid.NewGuid().ToString("N");
            var options = new DbContextOptionsBuilder<MdmDbContext>()
                .UseInMemoryDatabase(dbName, root).Options;
            var db = new MdmDbContext(options);
            var service = new MdmReferenceDataService(
                db, NullLogger<MdmReferenceDataService>.Instance);
            return new ReferenceDataFixture(db, service);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
        }
    }

    private static DirectoryInfo? FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "GuliERP.slnx")))
            {
                return dir;
            }
            dir = dir.Parent;
        }
        return null;
    }
}
