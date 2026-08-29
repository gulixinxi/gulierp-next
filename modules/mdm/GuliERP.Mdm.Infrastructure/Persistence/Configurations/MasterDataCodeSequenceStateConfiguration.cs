using GuliERP.Mdm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuliERP.Mdm.Infrastructure.Persistence.Configurations;

public sealed class MasterDataCodeSequenceStateConfiguration : IEntityTypeConfiguration<MasterDataCodeSequenceState>
{
    public void Configure(EntityTypeBuilder<MasterDataCodeSequenceState> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseHiLo(MdmDbContext.HiLoSequenceName, MdmDbContext.HiLoSequenceSchema);

        b.Property(x => x.RuleId).IsRequired();
        b.Property(x => x.TenantId).IsRequired();
        b.Property(x => x.CompanyId).IsRequired(false);
        b.Property(x => x.WarehouseId).IsRequired(false);
        b.Property(x => x.CurrentValue).IsRequired();
        b.Property(x => x.LastGeneratedCode).HasMaxLength(64);
        b.Property(x => x.ConcurrencyVersion).IsConcurrencyToken();

        b.HasIndex(x => x.RuleId)
            .IsUnique()
            .HasDatabaseName("ux_gulierp_master_code_sequence_rule");

        b.HasOne(x => x.Rule)
            .WithOne(x => x.SequenceState)
            .HasForeignKey<MasterDataCodeSequenceState>(x => x.RuleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
