using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("ResultadosExtraccion", Schema = "ocr")]
[Index("IdImagen", Name = "IX_Ext_Imagen")]
public partial class ResultadosExtraccion
{
    [Key]
    public long Id { get; set; }

    public Guid IdImagen { get; set; }

    [Column("IdResultadoOCR")]
    public long? IdResultadoOcr { get; set; }

    [Column("IdConfiguracionOCR")]
    public int? IdConfiguracionOcr { get; set; }

    [StringLength(50)]
    public string Motor { get; set; } = null!;

    [Column("JSONEstructurado")]
    public string? Jsonestructurado { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? ConfianzaExtraccion { get; set; }

    [StringLength(500)]
    public string? CamposFaltantes { get; set; }

    [StringLength(100)]
    public string? AseguradoraDetectada { get; set; }

    [StringLength(50)]
    public string? FormatoDetectado { get; set; }

    public int? TokensEntrada { get; set; }

    public int? TokensSalida { get; set; }

    [Column("CostoEstimadoUSD", TypeName = "decimal(10, 6)")]
    public decimal? CostoEstimadoUsd { get; set; }

    [StringLength(20)]
    public string? PromptVersion { get; set; }

    public DateTime FechaProceso { get; set; }

    public bool Exitoso { get; set; }

    [StringLength(300)]
    public string? MensajeError { get; set; }

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [ForeignKey("IdConfiguracionOcr")]
    [InverseProperty("ResultadosExtraccions")]
    public virtual ConfiguracionesOcr? IdConfiguracionOcrNavigation { get; set; }

    [ForeignKey("IdImagen")]
    [InverseProperty("ResultadosExtraccions")]
    public virtual Imagene IdImagenNavigation { get; set; } = null!;

    [ForeignKey("IdResultadoOcr")]
    [InverseProperty("ResultadosExtraccions")]
    public virtual ResultadosOcr? IdResultadoOcrNavigation { get; set; }
}
