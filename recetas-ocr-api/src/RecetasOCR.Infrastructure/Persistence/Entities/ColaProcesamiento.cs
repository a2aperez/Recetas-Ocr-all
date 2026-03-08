using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("ColaProcesamiento", Schema = "ocr")]
[Index("IdImagen", Name = "IX_Cola_Imagen")]
[Index("EstadoCola", "Bloqueado", "Prioridad", "FechaEncolado", Name = "IX_Cola_Polling")]
public partial class ColaProcesamiento
{
    [Key]
    public long Id { get; set; }

    public Guid IdImagen { get; set; }

    [StringLength(500)]
    public string UrlBlobRaw { get; set; } = null!;

    public int Prioridad { get; set; }

    public int Intentos { get; set; }

    public int MaxIntentos { get; set; }

    public DateTime FechaEncolado { get; set; }

    public DateTime? FechaInicioProceso { get; set; }

    public DateTime? FechaFinProceso { get; set; }

    [StringLength(100)]
    public string? WorkerProcesando { get; set; }

    public bool Bloqueado { get; set; }

    public DateTime? FechaBloqueo { get; set; }

    [StringLength(20)]
    public string EstadoCola { get; set; } = null!;

    [StringLength(500)]
    public string? ErrorMensaje { get; set; }

    [Column("IdConfiguracionOCR")]
    public int? IdConfiguracionOcr { get; set; }

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [ForeignKey("IdConfiguracionOcr")]
    [InverseProperty("ColaProcesamientos")]
    public virtual ConfiguracionesOcr? IdConfiguracionOcrNavigation { get; set; }

    [ForeignKey("IdImagen")]
    [InverseProperty("ColaProcesamientos")]
    public virtual Imagene IdImagenNavigation { get; set; } = null!;
}
