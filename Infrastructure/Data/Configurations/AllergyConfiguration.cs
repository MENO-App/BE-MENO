using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public sealed class AllergyConfiguration : IEntityTypeConfiguration<Allergy>
{
    public void Configure(EntityTypeBuilder<Allergy> builder)
    {
        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasData(
          new Allergy { AllergyId = new Guid("11111111-1111-1111-1111-111111111111"), Name = "Gluten" },
                new Allergy { AllergyId = new Guid("22222222-2222-2222-2222-222222222222"), Name = "Laktos" },
                new Allergy { AllergyId = new Guid("33333333-3333-3333-3333-333333333333"), Name = "Mjölkprotein" },
                new Allergy { AllergyId = new Guid("44444444-4444-4444-4444-444444444444"), Name = "Ägg" },
                new Allergy { AllergyId = new Guid("55555555-5555-5555-5555-555555555555"), Name = "Nötter" },
                new Allergy { AllergyId = new Guid("66666666-6666-6666-6666-666666666666"), Name = "Jordnötter" },
                new Allergy { AllergyId = new Guid("77777777-7777-7777-7777-777777777777"), Name = "Soja" },
                new Allergy { AllergyId = new Guid("88888888-8888-8888-8888-888888888888"), Name = "Fisk" },
                new Allergy { AllergyId = new Guid("99999999-9999-9999-9999-999999999999"), Name = "Skaldjur" },
                new Allergy { AllergyId = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Name = "Sesam" },
                new Allergy { AllergyId = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), Name = "Annan" }
        );
    }
}
