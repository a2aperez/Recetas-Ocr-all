using Microsoft.EntityFrameworkCore;
using RecetasOCR.Application.Common.Interfaces;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RecetasOCR.Infrastructure.Tests")]

namespace RecetasOCR.Infrastructure.Persistence;

/// <summary>
/// Parte del partial class: implementa IRecetasOcrDbContext y aplica
/// IEntityTypeConfiguration desde Persistence/Configurations/ vía
/// ApplyConfigurationsFromAssembly en OnModelCreatingPartial.
/// DbContext ya expone Set&lt;T&gt;(), SaveChangesAsync() y Database natively.
/// </summary>
public partial class RecetasOcrDbContext : IRecetasOcrDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RecetasOcrDbContext).Assembly);
    }
}
