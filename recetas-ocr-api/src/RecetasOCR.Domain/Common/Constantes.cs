namespace RecetasOCR.Domain.Common;

/// <summary>
/// Constantes globales del dominio RecetasOCR.
/// Evitan magic strings dispersos en la solución.
/// Los valores corresponden exactamente a los registrados en la BD:
///   cfg.Parametros, cfg.ConfiguracionesOCR, seg.Modulos y Azure Blob Storage.
/// </summary>
public static class Constantes
{
    /// <summary>
    /// Nombres de los contenedores de Azure Blob Storage.
    /// Deben coincidir con appsettings.json → AzureBlobStorage.*
    /// y con los valores en cfg.Parametros de la BD.
    /// </summary>
    public static class BlobContainers
    {
        /// <summary>Toda imagen recibida se sube SIEMPRE aquí. UrlBlobRaw NOT NULL.</summary>
        public const string RAW = "recetas-raw";

        /// <summary>Copia de imágenes evaluadas como legibles por la API OCR.</summary>
        public const string OCR = "recetas-ocr";

        /// <summary>
        /// Copia de imágenes ilegibles. Evidencia permanente — NUNCA se eliminan.
        /// </summary>
        public const string ILEGIBLE = "recetas-ilegibles";

        /// <summary>Archivos XML timbrados por el PAC.</summary>
        public const string CFDI_XML = "cfdi-xml";

        /// <summary>PDFs de CFDIs generados.</summary>
        public const string CFDI_PDF = "cfdi-pdf";
    }

    /// <summary>
    /// Claves de cfg.Parametros usadas en los servicios de infraestructura y handlers.
    /// </summary>
    public static class Parametros
    {
        /// <summary>Score mínimo OCR para considerar aprobado (default 70).</summary>
        public const string OCR_CONFIANZA_MINIMA = "OCR_CONFIANZA_MINIMA";

        /// <summary>Tamaño máximo de imagen en MB permitido en la subida (default 15).</summary>
        public const string MAX_TAMANIO_IMAGEN_MB = "MAX_TAMANIO_IMAGEN_MB";

        /// <summary>Extensiones de imagen permitidas separadas por coma.</summary>
        public const string FORMATOS_IMAGEN_PERMITIDOS = "FORMATOS_IMAGEN_PERMITIDOS";

        /// <summary>Intentos fallidos de login antes de bloquear la cuenta (default 5).</summary>
        public const string MAX_INTENTOS_LOGIN = "MAX_INTENTOS_LOGIN";

        /// <summary>Minutos de bloqueo de cuenta tras agotar intentos (default 30).</summary>
        public const string BLOQUEO_MINUTOS = "BLOQUEO_MINUTOS";

        /// <summary>Segundos entre cada ciclo de polling del OcrWorkerService (default 3).</summary>
        public const string OCR_WORKER_POLLING_SEG = "OCR_WORKER_POLLING_SEG";
    }

    /// <summary>
    /// Claves de seg.Modulos usadas en la verificación de permisos.
    /// Deben coincidir exactamente con los INSERT de V001__baseline_v5.2.sql.
    /// </summary>
    public static class ModulosPermisos
    {
        public const string IMAGENES_SUBIR          = "IMAGENES.SUBIR";
        public const string IMAGENES_VER            = "IMAGENES.VER";
        public const string REVISION_VER            = "REVISION.VER";
        public const string REVISION_APROBAR        = "REVISION.APROBAR";
        public const string REVISION_CAPTURA_MANUAL = "REVISION.CAPTURA_MANUAL";
        public const string FACTURACION_VER         = "FACTURACION.VER";
        public const string FACTURACION_GENERAR     = "FACTURACION.GENERAR";
        public const string FACTURACION_TIMBRAR     = "FACTURACION.TIMBRAR";
        public const string USUARIOS_ADMINISTRAR    = "USUARIOS.ADMINISTRAR";
        public const string CONFIG_EDITAR           = "CONFIG.EDITAR";
        public const string AUDITORIA_VER           = "AUDITORIA.VER";
    }
}
