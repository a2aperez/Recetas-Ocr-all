using MediatR;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs.Imagenes;

namespace RecetasOCR.Application.Features.Imagenes;

/// <summary>
/// Command para registrar una nueva imagen de receta en el sistema.
///
/// FLUJO GARANTIZADO:
///   1. Imagen sube a recetas-raw usando los bytes ya en memoria (sin re-lectura de stream).
///   2. OCR síncrono inmediato con los mismos bytes.
///   3. Si OCR falla → imagen queda en RECIBIDA y el Worker reintenta.
///
/// Implementa IAuditableCommand: el handler requiere usuario autenticado
/// para poblar IdUsuarioSubida y ModificadoPor.
/// </summary>
public record SubirImagenCommand(
    Guid   IdGrupo,
    string NombreArchivo,
    string MimeType,
    long   TamanioBytes,
    byte[] ArchivoBytes,
    string OrigenImagen
) : IRequest<ImagenDto>, IAuditableCommand;
