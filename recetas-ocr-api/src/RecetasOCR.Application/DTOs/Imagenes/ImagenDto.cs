namespace RecetasOCR.Application.DTOs.Imagenes;

/// <summary>
/// Representación de rec.Imagenes para el cliente.
/// Excluye campos internos de procesamiento:
///   IntentosProceso, WorkerProcesando, FechaInicioWorker, ErrorProceso.
/// UrlBlobRaw siempre presente (NOT NULL en BD).
/// UrlBlobOcr: presente solo si la imagen fue evaluada como legible.
/// UrlBlobIlegible: presente solo si fue evaluada como ilegible (evidencia permanente).
/// </summary>
public record ImagenDto(
    Guid     Id,
    Guid     IdGrupo,
    int      NumeroHoja,
    string   UrlBlobRaw,
    string?  UrlBlobOcr,
    string?  UrlBlobIlegible,
    string   OrigenImagen,
    string   NombreArchivo,
    long?    TamanioBytes,
    DateTime FechaSubida,
    decimal? ScoreLegibilidad,
    bool?    EsLegible,
    string?  MotivoBajaCalidad,
    bool     EsCapturaManual,
    string   EstadoImagen,
    string?  ModificadoPor,
    DateTime FechaModificacion
);
