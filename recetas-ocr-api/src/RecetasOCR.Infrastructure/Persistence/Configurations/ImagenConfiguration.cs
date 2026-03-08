using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecetasOCR.Infrastructure.Persistence.Entities;

namespace RecetasOCR.Infrastructure.Persistence.Configurations;

public class ImagenConfiguration : IEntityTypeConfiguration<Imagene>
{
    public void Configure(EntityTypeBuilder<Imagene> builder)
    {
        builder.ToTable("Imagenes", "rec");

        // Índice compuesto para queries de cola de revisión filtradas por grupo + estado
        builder.HasIndex(e => new { e.IdGrupo, e.IdEstadoImagen })
            .HasDatabaseName("IX_Img_Grupo_Estado");
    }
}
