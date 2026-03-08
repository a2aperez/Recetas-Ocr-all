using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace RecetasOCR.Application.Common.Interfaces;

/// <summary>
/// Abstracción del DbContext para permitir mockeo en tests de Application
/// sin depender de EF Core real ni de la BD.
/// Implementada por RecetasOcrDbContext en Infrastructure.
///
/// Los DbSet&lt;T&gt; tipados se declaran aquí como Set&lt;T&gt;() dinámicamente
/// para evitar dependencia circular Application → Infrastructure.
/// Los handlers acceden a las entidades mediante context.Set&lt;TEntity&gt;().
/// </summary>
public interface IRecetasOcrDbContext
{
    /// <summary>
    /// Acceso genérico a cualquier DbSet del contexto.
    /// Uso en handlers: _ctx.Set&lt;GruposRecetum&gt;().Where(...).
    /// </summary>
    DbSet<TEntity> Set<TEntity>() where TEntity : class;

    /// <summary>
    /// Persiste todos los cambios pendientes en la base de datos.
    /// Siempre usar la sobrecarga con CancellationToken.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Acceso al DatabaseFacade para gestión de transacciones explícitas
    /// en handlers que requieren operaciones atómicas entre múltiples tablas.
    /// Ej: BeginTransactionAsync() en el flujo de facturación.
    /// </summary>
    DatabaseFacade Database { get; }
}
