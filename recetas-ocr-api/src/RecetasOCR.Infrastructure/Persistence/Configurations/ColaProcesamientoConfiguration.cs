using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecetasOCR.Infrastructure.Persistence.Entities;

namespace RecetasOCR.Infrastructure.Persistence.Configurations;

public class ColaProcesamientoConfiguration : IEntityTypeConfiguration<ColaProcesamiento>
{
    public void Configure(EntityTypeBuilder<ColaProcesamiento> builder)
    {
        builder.ToTable("ColaProcesamiento", "ocr");

        // Índice compuesto de polling: worker filtra por EstadoCola='PENDIENTE',
        // Bloqueado=0, ordenado por Prioridad y FechaEncolado.
        // HasDatabaseName referencia el mismo nombre que el [Index] attribute
        // de la entidad (IX_Cola_Polling) para evitar índice duplicado.
        builder.HasIndex(e => new { e.EstadoCola, e.Bloqueado, e.Prioridad, e.FechaEncolado })
            .HasDatabaseName("IX_Cola_Polling");
    }
}
