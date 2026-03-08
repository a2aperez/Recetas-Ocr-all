using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecetasOCR.Infrastructure.Persistence.Entities;

namespace RecetasOCR.Infrastructure.Persistence.Configurations;

public class GrupoRecetaConfiguration : IEntityTypeConfiguration<GruposRecetum>
{
    public void Configure(EntityTypeBuilder<GruposRecetum> builder)
    {
        builder.ToTable("GruposReceta", "rec");
    }
}
