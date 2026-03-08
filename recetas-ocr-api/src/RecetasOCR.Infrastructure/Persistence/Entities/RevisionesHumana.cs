using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("RevisionesHumanas", Schema = "rev")]
public partial class RevisionesHumana
{
    [Key]
    public Guid Id { get; set; }

    public Guid IdImagen { get; set; }

    public Guid IdGrupo { get; set; }

    public Guid? IdAsignacion { get; set; }

    [StringLength(50)]
    public string TipoRevision { get; set; } = null!;

    [StringLength(20)]
    public string Resultado { get; set; } = null!;

    [StringLength(300)]
    public string? MotivoRechazo { get; set; }

    public Guid IdUsuarioRevisor { get; set; }

    public DateTime FechaRevision { get; set; }

    public int? DuracionMinutos { get; set; }

    [StringLength(500)]
    public string? Observaciones { get; set; }

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [ForeignKey("IdAsignacion")]
    [InverseProperty("RevisionesHumanas")]
    public virtual AsignacionesRevision? IdAsignacionNavigation { get; set; }

    [ForeignKey("IdGrupo")]
    [InverseProperty("RevisionesHumanas")]
    public virtual GruposRecetum IdGrupoNavigation { get; set; } = null!;

    [ForeignKey("IdImagen")]
    [InverseProperty("RevisionesHumanas")]
    public virtual Imagene IdImagenNavigation { get; set; } = null!;

    [ForeignKey("IdUsuarioRevisor")]
    [InverseProperty("RevisionesHumanas")]
    public virtual Usuario IdUsuarioRevisorNavigation { get; set; } = null!;
}
