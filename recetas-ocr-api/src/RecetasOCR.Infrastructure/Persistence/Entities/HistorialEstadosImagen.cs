using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("HistorialEstadosImagen", Schema = "aud")]
[Index("IdImagen", Name = "IX_AudImg_Imagen")]
public partial class HistorialEstadosImagen
{
    [Key]
    public long Id { get; set; }

    public Guid IdImagen { get; set; }

    public int? EstadoAnterior { get; set; }

    public int EstadoNuevo { get; set; }

    public Guid? IdUsuario { get; set; }

    [StringLength(300)]
    public string? Motivo { get; set; }

    public DateTime FechaCambio { get; set; }

    [ForeignKey("EstadoAnterior")]
    [InverseProperty("HistorialEstadosImagenEstadoAnteriorNavigations")]
    public virtual EstadosImagen? EstadoAnteriorNavigation { get; set; }

    [ForeignKey("EstadoNuevo")]
    [InverseProperty("HistorialEstadosImagenEstadoNuevoNavigations")]
    public virtual EstadosImagen EstadoNuevoNavigation { get; set; } = null!;

    [ForeignKey("IdImagen")]
    [InverseProperty("HistorialEstadosImagens")]
    public virtual Imagene IdImagenNavigation { get; set; } = null!;

    [ForeignKey("IdUsuario")]
    [InverseProperty("HistorialEstadosImagens")]
    public virtual Usuario? IdUsuarioNavigation { get; set; }
}
