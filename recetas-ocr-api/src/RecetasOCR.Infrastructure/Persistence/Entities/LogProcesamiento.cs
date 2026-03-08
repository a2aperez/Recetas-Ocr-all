using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("LogProcesamiento", Schema = "aud")]
[Index("FechaEvento", Name = "IX_Log_Fecha")]
[Index("Nivel", "FechaEvento", Name = "IX_Log_Nivel")]
public partial class LogProcesamiento
{
    [Key]
    public long Id { get; set; }

    public Guid? IdImagen { get; set; }

    public Guid? IdGrupo { get; set; }

    [StringLength(80)]
    public string Paso { get; set; } = null!;

    [StringLength(10)]
    public string Nivel { get; set; } = null!;

    [StringLength(500)]
    public string? Mensaje { get; set; }

    public string? Detalle { get; set; }

    public int? DuracionMs { get; set; }

    [StringLength(100)]
    public string? Servidor { get; set; }

    public DateTime FechaEvento { get; set; }
}
