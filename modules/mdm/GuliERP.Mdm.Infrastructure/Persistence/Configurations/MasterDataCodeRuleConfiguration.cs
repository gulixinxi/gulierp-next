using GuliERP.Mdm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuliERP.Mdm.Infrastructure.Persistence.Configurations;

public sealed class MasterDataCodeRuleConfiguration : IEntityTypeConfiguration<MasterDataCodeRule>
{
    public void Configure(EntityTypeBuilder<MasterDataCodeRule> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseHiLo(MdmDbContext.HiLoSequenceName, MdmDbContext.HiLoSequenceSchema);

        b.Property(x => x.TenantId).IsRequired();
        b.Property(x => x.CompanyId).IsRequired(false);
        b.Property(x => x.WarehouseId).IsRequired(false);
        b.Property(x => x.EntityType).IsRequired().HasMaxLength(64);
        b.Property(x => x.SubType).HasMaxLength(64);
        b.Property(x => x.Mode).HasConversion<int>();
        b.Property(x => x.Prefix).IsRequired().HasMaxLength(12);
        b.Property(x => x.Separator).IsRequired().HasMaxLength(1);
        b.Property(x => x.SequenceLength).IsRequired();
        b.Property(x => x.StartValue).IsRequired();
        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.ConcurrencyVersion).IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.CompanyId, x.WarehouseId, x.EntityType, x.SubType })
            .IsUnique()
            .HasDatabaseName("ux_gulierp_master_code_rule_scope");
    }
}
