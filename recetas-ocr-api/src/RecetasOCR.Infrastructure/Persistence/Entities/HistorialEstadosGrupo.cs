using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("HistorialEstadosGrupo", Schema = "aud")]
[Index("IdGrupo", Name = "IX_AudGrp_Grupo")]
public partial class HistorialEstadosGrupo
{
    [Key]
    public long Id { get; set; }

    public Guid IdGrupo { get; set; }

    public int? EstadoAnterior { get; set; }

    public int EstadoNuevo { get; set; }

    public Guid? IdUsuario { get; set; }

    [StringLength(300)]
    public string? Motivo { get; set; }

    public DateTime FechaCambio { get; set; }

    [ForeignKey("EstadoAnterior")]
    [InverseProperty("HistorialEstadosGrupoEstadoAnteriorNavigations")]
    public virtual EstadosGrupo? EstadoAnteriorNavigation { get; set; }

    [ForeignKey("EstadoNuevo")]
    [InverseProperty("HistorialEstadosGrupoEstadoNuevoNavigations")]
    public virtual EstadosGrupo EstadoNuevoNavigation { get; set; } = null!;

    [ForeignKey("IdGrupo")]
    [InverseProperty("HistorialEstadosGrupos")]
    public virtual GruposRecetum IdGrupoNavigation { get; set; } = null!;

    [ForeignKey("IdUsuario")]
    [InverseProperty("HistorialEstadosGrupos")]
    public virtual Usuario? IdUsuarioNavigation { get; set; }
}
