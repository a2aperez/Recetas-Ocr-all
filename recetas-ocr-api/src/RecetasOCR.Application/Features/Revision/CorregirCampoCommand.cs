using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Domain.Enums;

namespace RecetasOCR.Application.Features.Revision;

// ──────────────────────────────────────────────────────────────────────────────
// Command
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Registra la corrección de un campo en el historial de auditoría.
/// SOLO hace INSERT en aud.HistorialCorrecciones — no modifica ninguna otra tabla.
/// La aplicación del valor nuevo (si aplica) la hace el handler específico
/// de cada entidad; este command es únicamente para trazabilidad.
/// TipoCorreccion debe ser un valor del enum TipoCorreccion del dominio.
/// </summary>
public record CorregirCampoCommand(
    Guid    IdImagen,
    string  Tabla,
    string  Campo,
    string? ValorAnterior,
    string  ValorNuevo,
    string  TipoCorreccion,
    Guid?   IdGrupo      = null,
    Guid?   IdMedicamento = null
) : IRequest<Unit>, IAuditableCommand;

// ──────────────────────────────────────────────────────────────────────────────
// Validator
// ──────────────────────────────────────────────────────────────────────────────

public class CorregirCampoCommandValidator : AbstractValidator<CorregirCampoCommand>
{
    private static readonly string[] _tiposCorreccionValidos =
        Enum.GetNames<TipoCorreccion>();

    public CorregirCampoCommandValidator()
    {
        RuleFor(x => x.IdImagen)
            .NotEmpty()
            .WithMessage("El IdImagen es obligatorio.");

        RuleFor(x => x.Tabla)
            .NotEmpty()
            .WithMessage("La tabla es obligatoria.")
            .MaximumLength(100)
            .WithMessage("La tabla no puede exceder 100 caracteres.");

        RuleFor(x => x.Campo)
            .NotEmpty()
            .WithMessage("El campo es obligatorio.")
            .MaximumLength(100)
            .WithMessage("El campo no puede exceder 100 caracteres.");

        RuleFor(x => x.ValorNuevo)
            .NotEmpty()
            .WithMessage("El valor nuevo es obligatorio.")
            .MaximumLength(500)
            .WithMessage("El valor nuevo no puede exceder 500 caracteres.");

        RuleFor(x => x.ValorAnterior)
            .MaximumLength(500)
            .WithMessage("El valor anterior no puede exceder 500 caracteres.")
            .When(x => x.ValorAnterior is not null);

        RuleFor(x => x.TipoCorreccion)
            .NotEmpty()
            .WithMessage("El tipo de corrección es obligatorio.")
            .Must(t => _tiposCorreccionValidos.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"TipoCorreccion debe ser: {string.Join(", ", _tiposCorreccionValidos)}.");
    }
}

// ──────────────────────────────────────────────────────────────────────────────
// Handler
// ──────────────────────────────────────────────────────────────────────────────

public class CorregirCampoCommandHandler(
    IRecetasOcrDbContext db,
    ICurrentUserService  currentUser,
    ILogger<CorregirCampoCommandHandler> logger)
    : IRequestHandler<CorregirCampoCommand, Unit>
{
    // ── Allowlists — NEVER use user-supplied campo directly as column name ──────

    /// <summary>Frontend camelCase campo → rec.GruposReceta column name (PascalCase).</summary>
    private static readonly Dictionary<string, string> _gruposColumnas = new(StringComparer.OrdinalIgnoreCase)
    {
        { "nombrePaciente",    "NombrePaciente"          },
        { "apellidoPaterno",   "ApellidoPaterno"         },
        { "apellidoMaterno",   "ApellidoMaterno"         },
        { "nombreMedico",      "NombreMedico"            },
        { "cedulaMedico",      "CedulaMedico"            },
        { "especialidad",      "EspecialidadTexto"       },
        { "fechaConsulta",     "FechaConsulta"           },
        { "diagnosticoTexto",  "DescripcionDiagnostico"  },
        { "diagnostico",       "DescripcionDiagnostico"  },
    };

    /// <summary>Frontend camelCase campo → med.MedicamentosReceta column name (PascalCase).</summary>
    private static readonly Dictionary<string, string> _medColumnas = new(StringComparer.OrdinalIgnoreCase)
    {
        { "nombreComercial",      "NombreComercial"      },
        { "sustanciaActiva",      "SustanciaActiva"      },
        { "presentacion",         "Presentacion"         },
        { "dosis",                "Dosis"                },
        { "cantidadTexto",        "CantidadTexto"        },
        { "frecuenciaTexto",      "FrecuenciaTexto"      },
        { "frecuenciaExpandida",  "FrecuenciaExpandida"  },
        { "duracionTexto",        "DuracionTexto"        },
        { "viaAdministracion",    "ViaAdministracion"    },
        { "indicacionesCompletas","IndicacionesCompletas" },
    };

    public async Task<Unit> Handle(
        CorregirCampoCommand command,
        CancellationToken    cancellationToken)
    {
        var ahora     = DateTime.UtcNow;
        var usuarioId = currentUser.UserId!.Value;
        var usuario   = currentUser.Username ?? "sistema";

        // ── 1. Auditoría ────────────────────────────────────────────────────────
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO aud.HistorialCorrecciones
                (IdImagen, IdGrupo, IdMedicamento,
                 Tabla, Campo, ValorAnterior, ValorNuevo,
                 TipoCorreccion, IdUsuario, FechaCorreccion)
            VALUES
                ({command.IdImagen}, {command.IdGrupo}, {command.IdMedicamento},
                 {command.Tabla}, {command.Campo},
                 {command.ValorAnterior}, {command.ValorNuevo},
                 {command.TipoCorreccion}, {usuarioId}, {ahora})
            """, cancellationToken);

        // ── 2. Aplicar actualización de datos ───────────────────────────────────
        switch (command.Tabla?.ToUpperInvariant())
        {
            case "GRUPOSRECETA":
                if (_gruposColumnas.TryGetValue(command.Campo, out var colGrupo))
                {
                    // Para FechaConsulta usamos TRY_CAST para no romper con strings inválidos
                    if (colGrupo == "FechaConsulta")
                    {
                        await db.Database.ExecuteSqlAsync($"""
                            UPDATE rec.GruposReceta
                            SET    FechaConsulta      = TRY_CAST({command.ValorNuevo} AS DATE),
                                   FechaModificacion  = {ahora},
                                   ModificadoPor      = {usuario}
                            WHERE  Id = (SELECT TOP 1 IdGrupo FROM rec.Imagenes WHERE Id = {command.IdImagen})
                            """, cancellationToken);
                    }
                    else
                    {
                        // Para otros campos, necesitamos usar ExecuteSqlRaw con FormattableString
                        var sql = $@"
                            UPDATE rec.GruposReceta
                            SET    [{colGrupo}] = @p0,
                                   FechaModificacion = @p1,
                                   ModificadoPor = @p2
                            WHERE  Id = (SELECT TOP 1 IdGrupo FROM rec.Imagenes WHERE Id = @p3)";

                        await db.Database.ExecuteSqlRawAsync(sql,
                            new object[] { command.ValorNuevo, ahora, usuario, command.IdImagen },
                            cancellationToken);
                    }
                }
                break;

            case "MEDICAMENTOSRECETA":
                if (_medColumnas.TryGetValue(command.Campo, out var colMed)
                    && command.IdMedicamento.HasValue)
                {
                    var sql = $@"
                        UPDATE med.MedicamentosReceta
                        SET    [{colMed}] = @p0,
                               FechaModificacion = @p1,
                               ModificadoPor = @p2
                        WHERE  Id = @p3";

                    await db.Database.ExecuteSqlRawAsync(sql,
                        new object[] { command.ValorNuevo, ahora, usuario, command.IdMedicamento.Value },
                        cancellationToken);
                }
                break;
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "[Revision] Corrección aplicada | Imagen: {IdImagen} | {Tabla}.{Campo} '{Ant}' → '{Nvo}'",
            command.IdImagen, command.Tabla, command.Campo,
            command.ValorAnterior, command.ValorNuevo);

        return Unit.Value;
    }
}
