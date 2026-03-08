using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("PermisosUsuario", Schema = "seg")]
[Index("IdUsuario", "IdModulo", Name = "UQ_PermisoUsuarioModulo", IsUnique = true)]
public partial class PermisosUsuario
{
    [Key]
    public int Id { get; set; }

    public Guid IdUsuario { get; set; }

    public int IdModulo { get; set; }

    public bool PuedeLeer { get; set; }

    public bool PuedeEscribir { get; set; }

    public bool PuedeEliminar { get; set; }

    public bool Denegado { get; set; }

    [StringLength(200)]
    public string? Motivo { get; set; }

    public DateTime FechaAlta { get; set; }

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [ForeignKey("IdModulo")]
    [InverseProperty("PermisosUsuarios")]
    public virtual Modulo IdModuloNavigation { get; set; } = null!;

    [ForeignKey("IdUsuario")]
    [InverseProperty("PermisosUsuarios")]
    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
