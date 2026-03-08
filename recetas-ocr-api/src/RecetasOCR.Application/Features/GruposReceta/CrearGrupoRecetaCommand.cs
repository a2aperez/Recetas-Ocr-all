using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.GruposReceta;

namespace RecetasOCR.Application.Features.GruposReceta;

// ──────────────────────────────────────────────────────────────────────────────
// Result
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Resultado de CrearGrupoRecetaCommand.
/// Creado=true  → el grupo fue insertado ahora.
/// Creado=false → ya existía un grupo con el mismo FolioBase o la misma
///               combinación IdCliente+IdAseguradora+FechaConsulta.
///               No se generó ningún duplicado.
/// </summary>
public record CrearGrupoRecetaResult(GrupoRecetaDto Grupo, bool Creado);

// ──────────────────────────────────────────────────────────────────────────────
// Command
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Crea un grupo de receta aplicando la regla de agrupación:
///   1. Si FolioBase != null    → buscar por FolioBase.
///   2. Si FolioBase == null    → buscar por IdCliente + IdAseguradora + FechaConsulta.
///   3. Si no existe            → INSERT con estado RECIBIDO.
///   4. Si ya existe            → retornar el existente (Creado=false).
/// El campo IdFormatoReceta se resuelve al primer formato activo de cat.FormatosReceta.
/// Implementa IAuditableCommand: requiere usuario autenticado.
/// </summary>
public record CrearGrupoRecetaCommand(
    int       IdAseguradora,
    string?   FolioBase,
    Guid?     IdCliente,
    DateOnly  FechaConsulta,
    string?   NombrePaciente = null,
    string?   NombreMedico   = null
) : IRequest<CrearGrupoRecetaResult>, IAuditableCommand;

// ──────────────────────────────────────────────────────────────────────────────
// Validator
// ──────────────────────────────────────────────────────────────────────────────

public class CrearGrupoRecetaCommandValidator : AbstractValidator<CrearGrupoRecetaCommand>
{
    public CrearGrupoRecetaCommandValidator()
    {
        RuleFor(x => x.IdAseguradora)
            .GreaterThan(0)
            .WithMessage("El IdAseguradora es obligatorio y debe ser mayor a 0.");

        RuleFor(x => x.FolioBase)
            .MaximumLength(100)
            .WithMessage("El FolioBase no puede exceder 100 caracteres.")
            .When(x => x.FolioBase is not null);

        RuleFor(x => x.FechaConsulta)
            .Must(f => f != default)
            .WithMessage("La FechaConsulta es obligatoria.");

        RuleFor(x => x.NombrePaciente)
            .MaximumLength(200)
            .WithMessage("El nombre del paciente no puede exceder 200 caracteres.")
            .When(x => x.NombrePaciente is not null);

        RuleFor(x => x.NombreMedico)
            .MaximumLength(200)
            .WithMessage("El nombre del médico no puede exceder 200 caracteres.")
            .When(x => x.NombreMedico is not null);
    }
}

// ──────────────────────────────────────────────────────────────────────────────
// Handler
// ──────────────────────────────────────────────────────────────────────────────

public class CrearGrupoRecetaCommandHandler(
    IRecetasOcrDbContext db,
    ICurrentUserService  currentUser,
    ILogger<CrearGrupoRecetaCommandHandler> logger)
    : IRequestHandler<CrearGrupoRecetaCommand, CrearGrupoRecetaResult>
{
    public async Task<CrearGrupoRecetaResult> Handle(
        CrearGrupoRecetaCommand command,
        CancellationToken       cancellationToken)
    {
        var ahora    = DateTime.UtcNow;
        var username = currentUser.Username;
        var userId   = currentUser.UserId;

        // ── 1. Buscar grupo existente (regla de agrupación) ────────────
        Guid? idGrupoExistente = null;

        if (command.FolioBase is not null)
        {
            // Regla A: agrupar por FolioBase dentro de la misma aseguradora
            var folioA = command.FolioBase;
            var asegA  = command.IdAseguradora;

            var result = await db.Database
                .SqlQuery<GuidRow>($"""
                    SELECT TOP 1 Id
                    FROM   rec.GruposReceta
                    WHERE  FolioBase     = {folioA}
                      AND  IdAseguradora = {asegA}
                    """)
                .FirstOrDefaultAsync(cancellationToken);

            idGrupoExistente = result?.Id;
        }
        else if (command.IdCliente is not null)
        {
            // Regla B: agrupar por IdCliente + IdAseguradora + FechaConsulta
            var clienteB = command.IdCliente;
            var asegB    = command.IdAseguradora;
            var fechaB   = command.FechaConsulta;

            var result = await db.Database
                .SqlQuery<GuidRow>($"""
                    SELECT TOP 1 Id
                    FROM   rec.GruposReceta
                    WHERE  IdCliente      = {clienteB}
                      AND  IdAseguradora  = {asegB}
                      AND  FechaConsulta  = {fechaB}
                    """)
                .FirstOrDefaultAsync(cancellationToken);

            idGrupoExistente = result?.Id;
        }

        // ── 2. Si ya existe, retornar sin crear duplicado ──────────────
        if (idGrupoExistente.HasValue)
        {
            var grupoExistente = await CargarGrupoAsync(
                idGrupoExistente.Value, db, cancellationToken);

            logger.LogInformation(
                "[GruposReceta] Grupo existente reutilizado | Id: {Id} | Folio: {Folio}",
                idGrupoExistente.Value, command.FolioBase);

            return new(grupoExistente, Creado: false);
        }

        // ── 3. Crear nuevo grupo ───────────────────────────────────────

        // 3a. Resolver IdEstadoGrupo = RECIBIDO
        var estadoRecibido = await db.Database
            .SqlQuery<IdRow>($"""
                SELECT Id FROM cat.EstadosGrupo WHERE Clave = 'RECIBIDO'
                """)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                "No se encontró el estado RECIBIDO en cat.EstadosGrupo.");

        // 3b. Resolver IdFormatoReceta — primer formato activo disponible
        var formatoReceta = await db.Database
            .SqlQuery<IdRow>($"""
                SELECT TOP 1 Id FROM cat.FormatosReceta ORDER BY Id
                """)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                "No existen registros en cat.FormatosReceta.");

        var nuevoId       = Guid.NewGuid();
        var idCliente     = command.IdCliente;
        var idAseguradora = command.IdAseguradora;
        var folioBase     = command.FolioBase;
        var fechaConsulta = command.FechaConsulta;
        var nombrePac     = command.NombrePaciente;
        var nombreMed     = command.NombreMedico;

        // 3c. INSERT rec.GruposReceta
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO rec.GruposReceta
                (Id, IdAseguradora, IdFormatoReceta, FolioBase, IdCliente,
                 FechaConsulta, NombrePaciente, NombreMedico,
                 TotalImagenes, TotalMedicamentos,
                 IdEstadoGrupo, IdUsuarioAlta,
                 FechaCreacion, FechaActualizacion,
                 ModificadoPor, FechaModificacion)
            VALUES
                ({nuevoId}, {idAseguradora}, {formatoReceta.Id}, {folioBase}, {idCliente},
                 {fechaConsulta}, {nombrePac}, {nombreMed},
                 0, 0,
                 {estadoRecibido.Id}, {userId},
                 {ahora}, {ahora},
                 {username}, {ahora})
            """, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "[GruposReceta] Nuevo grupo creado | Id: {Id} | Folio: {Folio} | Aseguradora: {Aseg}",
            nuevoId, command.FolioBase, command.IdAseguradora);

        var grupoNuevo = await CargarGrupoAsync(nuevoId, db, cancellationToken);

        return new(grupoNuevo, Creado: true);
    }

    // ── Carga un grupo por Id con el JOIN de aseguradora y estado ──────

    private static async Task<GrupoRecetaDto> CargarGrupoAsync(
        Guid                 id,
        IRecetasOcrDbContext db,
        CancellationToken    ct)
    {
        var row = await db.Database
            .SqlQuery<GetGruposRecetaQueryHandler.GrupoRow>($"""
                SELECT
                    g.Id, g.FolioBase, g.IdCliente, g.IdAseguradora,
                    a.Nombre              AS NombreAseguradora,
                    g.Nur, g.NombrePaciente, g.ApellidoPaterno, g.ApellidoMaterno,
                    g.NombreMedico, g.CedulaMedico, g.EspecialidadTexto,
                    g.CodigoCIE10         AS CodigoCie10,
                    g.DescripcionDiagnostico, g.FechaConsulta,
                    g.TotalImagenes, g.TotalMedicamentos,
                    eg.Clave              AS EstadoGrupo,
                    g.FechaCreacion, g.FechaActualizacion,
                    g.ModificadoPor, g.FechaModificacion
                FROM   rec.GruposReceta     g
                INNER JOIN cat.EstadosGrupo  eg ON eg.Id = g.IdEstadoGrupo
                INNER JOIN cat.Aseguradoras  a  ON a.Id  = g.IdAseguradora
                WHERE  g.Id = {id}
                """)
            .FirstAsync(ct);

        return GetGruposRecetaQueryHandler.MapToDto(row);
    }

    private sealed record IdRow(int Id);
    private sealed record GuidRow(Guid Id);
}
