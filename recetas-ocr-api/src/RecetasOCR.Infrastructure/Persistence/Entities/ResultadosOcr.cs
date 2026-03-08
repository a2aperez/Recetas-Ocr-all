using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("ResultadosOCR", Schema = "ocr")]
[Index("IdImagen", Name = "IX_OCR_Imagen")]
[Index("ProveedorOcr", Name = "IX_OCR_Proveedor")]
public partial class ResultadosOcr
{
    [Key]
    public long Id { get; set; }

    public Guid IdImagen { get; set; }

    [Column("IdConfiguracionOCR")]
    public int? IdConfiguracionOcr { get; set; }

    [Column("ProveedorOCR")]
    [StringLength(80)]
    public string ProveedorOcr { get; set; } = null!;

    [StringLength(100)]
    public string? ModeloUsado { get; set; }

    [Column("VersionAPI")]
    [StringLength(20)]
    public string? VersionApi { get; set; }

    [StringLength(500)]
    public string? UrlEndpointLlamado { get; set; }

    [StringLength(200)]
    public string? RequestIdExterno { get; set; }

    public DateTime FechaPeticion { get; set; }

    public DateTime? FechaRespuesta { get; set; }

    public int? DuracionMs { get; set; }

    public string? TextoCompleto { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? ConfianzaPromedio { get; set; }

    [StringLength(10)]
    public string? IdiomaDetectado { get; set; }

    public int PaginasProcesadas { get; set; }

    public string? ResponseJsonCompleto { get; set; }

    [Column("CostoEstimadoUSD", TypeName = "decimal(10, 6)")]
    public decimal CostoEstimadoUsd { get; set; }

    public DateTime FechaProceso { get; set; }

    public bool Exitoso { get; set; }

    [Column("CodigoErrorHTTP")]
    public int? CodigoErrorHttp { get; set; }

    [StringLength(500)]
    public string? MensajeError { get; set; }

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [ForeignKey("IdConfiguracionOcr")]
    [InverseProperty("ResultadosOcrs")]
    public virtual ConfiguracionesOcr? IdConfiguracionOcrNavigation { get; set; }

    [ForeignKey("IdImagen")]
    [InverseProperty("ResultadosOcrs")]
    public virtual Imagene IdImagenNavigation { get; set; } = null!;

    [InverseProperty("IdResultadoOcrNavigation")]
    public virtual ICollection<ResultadosExtraccion> ResultadosExtraccions { get; set; } = new List<ResultadosExtraccion>();
}
