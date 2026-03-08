using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("AsignacionesRevision", Schema = "rev")]
[Index("Estado", Name = "IX_Rev_Estado")]
[Index("IdImagen", Name = "IX_Rev_Imagen")]
[Index("IdUsuarioAsignado", "Estado", Name = "IX_Rev_Usuario")]
public partial class AsignacionesRevision
{
    [Key]
    public Guid Id { get; set; }

    public Guid IdImagen { get; set; }

    public Guid IdGrupo { get; set; }

    public Guid IdUsuarioAsignado { get; set; }

    [StringLength(50)]
    public string TipoRevision { get; set; } = null!;

    public DateTime FechaAsignacion { get; set; }

    public DateTime? FechaLimite { get; set; }

    public DateTime? FechaInicio { get; set; }

    public DateTime? FechaTermino { get; set; }

    [StringLength(30)]
    public string Estado { get; set; } = null!;

    public Guid? IdUsuarioAsignoPor { get; set; }

    [StringLength(300)]
    public string? Notas { get; set; }

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [ForeignKey("IdGrupo")]
    [InverseProperty("AsignacionesRevisions")]
    public virtual GruposRecetum IdGrupoNavigation { get; set; } = null!;

    [ForeignKey("IdImagen")]
    [InverseProperty("AsignacionesRevisions")]
    public virtual Imagene IdImagenNavigation { get; set; } = null!;

    [ForeignKey("IdUsuarioAsignado")]
    [InverseProperty("AsignacionesRevisionIdUsuarioAsignadoNavigations")]
    public virtual Usuario IdUsuarioAsignadoNavigation { get; set; } = null!;

    [ForeignKey("IdUsuarioAsignoPor")]
    [InverseProperty("AsignacionesRevisionIdUsuarioAsignoPorNavigations")]
    public virtual Usuario? IdUsuarioAsignoPorNavigation { get; set; }

    [InverseProperty("IdAsignacionNavigation")]
    public virtual ICollection<RevisionesHumana> RevisionesHumanas { get; set; } = new List<RevisionesHumana>();
}
