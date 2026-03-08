using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("LogAcceso", Schema = "seg")]
[Index("Evento", "FechaEvento", Name = "IX_LogAcceso_Evento")]
public partial class LogAcceso
{
    [Key]
    public long Id { get; set; }

    public Guid? IdUsuario { get; set; }

    [StringLength(50)]
    public string Evento { get; set; } = null!;

    [StringLength(500)]
    public string? Detalle { get; set; }

    [StringLength(50)]
    public string? IpOrigen { get; set; }

    [StringLength(500)]
    public string? UserAgent { get; set; }

    [StringLength(200)]
    public string? Dispositivo { get; set; }

    public DateTime FechaEvento { get; set; }

    [ForeignKey("IdUsuario")]
    [InverseProperty("LogAccesos")]
    public virtual Usuario? IdUsuarioNavigation { get; set; }
}
