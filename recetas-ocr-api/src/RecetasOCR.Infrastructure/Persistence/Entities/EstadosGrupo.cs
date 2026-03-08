using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("EstadosGrupo", Schema = "cat")]
[Index("Clave", Name = "UQ__EstadosG__E8181E11B67A0865", IsUnique = true)]
public partial class EstadosGrupo
{
    [Key]
    public int Id { get; set; }

    [StringLength(60)]
    public string Clave { get; set; } = null!;

    [StringLength(150)]
    public string Descripcion { get; set; } = null!;

    public int Orden { get; set; }

    public bool EsFinal { get; set; }

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [InverseProperty("IdEstadoGrupoNavigation")]
    public virtual ICollection<GruposRecetum> GruposReceta { get; set; } = new List<GruposRecetum>();

    [InverseProperty("EstadoAnteriorNavigation")]
    public virtual ICollection<HistorialEstadosGrupo> HistorialEstadosGrupoEstadoAnteriorNavigations { get; set; } = new List<HistorialEstadosGrupo>();

    [InverseProperty("EstadoNuevoNavigation")]
    public virtual ICollection<HistorialEstadosGrupo> HistorialEstadosGrupoEstadoNuevoNavigations { get; set; } = new List<HistorialEstadosGrupo>();
}
