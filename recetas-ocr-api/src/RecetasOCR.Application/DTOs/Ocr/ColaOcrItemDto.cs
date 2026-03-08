namespace RecetasOCR.Application.DTOs.Ocr;

public record ColaOcrItemDto(
    long      Id,
    Guid      IdImagen,
    string?   NombreArchivo,
    string    EstadoCola,
    int       Prioridad,
    int       Intentos,
    int       MaxIntentos,
    bool      Bloqueado,
    string?   WorkerProcesando,
    DateTime  FechaEncolado,
    DateTime? FechaInicioProceso
);
