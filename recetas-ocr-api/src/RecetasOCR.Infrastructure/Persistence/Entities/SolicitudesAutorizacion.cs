using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("SolicitudesAutorizacion", Schema = "fac")]
public partial class SolicitudesAutorizacion
{
    [Key]
    public Guid Id { get; set; }

    public Guid IdGrupo { get; set; }

    [StringLength(100)]
    public string? NumeroAutorizacion { get; set; }

    public DateTime FechaSolicitud { get; set; }

    public DateTime? FechaRespuesta { get; set; }

    [StringLength(20)]
    public string Estado { get; set; } = null!;

    [StringLength(300)]
    public string? Observaciones { get; set; }

    public Guid? IdUsuarioSolicita { get; set; }

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [ForeignKey("IdGrupo")]
    [InverseProperty("SolicitudesAutorizacions")]
    public virtual GruposRecetum IdGrupoNavigation { get; set; } = null!;

    [ForeignKey("IdUsuarioSolicita")]
    [InverseProperty("SolicitudesAutorizacions")]
    public virtual Usuario? IdUsuarioSolicitaNavigation { get; set; }
}
