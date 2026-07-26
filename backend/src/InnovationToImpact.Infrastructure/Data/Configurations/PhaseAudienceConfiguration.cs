using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class PhaseAudienceConfiguration : IEntityTypeConfiguration<PhaseAudience>
{
    private static readonly (int PhaseIdx, string RoleCode)[] SeedAudiences =
    {
        (0, RoleCodes.Submitter),
        (1, RoleCodes.Supervisor),
        (2, RoleCodes.Evaluator),
        (3, RoleCodes.Judge),
        (3, RoleCodes.Supervisor),
        (4, RoleCodes.Supervisor),
        (5, RoleCodes.Supervisor),
        (6, RoleCodes.Supervisor),
    };

    public void Configure(EntityTypeBuilder<PhaseAudience> builder)
    {
        builder.ToTable("PhaseAudiences");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.RoleCode).IsRequired().HasMaxLength(50);
        builder.HasIndex(p => p.PhaseIdx);

        builder.HasData(SeedAudiences.Select((a, index) => new PhaseAudience
        {
            Id = new Guid($"00000000-0000-0000-0035-{(index + 1):D12}"),
            PhaseIdx = a.PhaseIdx,
            RoleCode = a.RoleCode,
        }));
    }
}
