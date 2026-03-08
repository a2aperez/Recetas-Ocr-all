using MediatR;
using RecetasOCR.Application.Common.Interfaces;

namespace RecetasOCR.Application.Features.Imagenes;

/// <summary>
/// Command para registrar una nueva imagen de receta en el sistema.
///
/// FLUJO GARANTIZADO:
///   1. Imagen siempre sube a recetas-raw (UrlBlobRaw NOT NULL).
///   2. Se encola en ocr.ColaProcesamiento — el Worker hace el OCR.
///   3. IOcrApiService NO se llama desde aquí.
///
/// Implementa IAuditableCommand: el handler requiere usuario autenticado
/// para poblar IdUsuarioSubida y ModificadoPor.
/// </summary>
public record SubirImagenCommand(
    Guid   IdGrupo,
    Stream Archivo,
    string NombreArchivo,
    string MimeType,
    long   TamanioBytes,
    string OrigenImagen
) : IRequest<Guid>, IAuditableCommand;
