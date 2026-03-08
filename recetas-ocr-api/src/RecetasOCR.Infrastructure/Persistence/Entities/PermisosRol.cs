using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("PermisosRol", Schema = "seg")]
[Index("IdRol", "IdModulo", Name = "UQ_PermisoRolModulo", IsUnique = true)]
public partial class PermisosRol
{
    [Key]
    public int Id { get; set; }

    public int IdRol { get; set; }

    public int IdModulo { get; set; }

    public bool PuedeLeer { get; set; }

    public bool PuedeEscribir { get; set; }

    public bool PuedeEliminar { get; set; }

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [ForeignKey("IdModulo")]
    [InverseProperty("PermisosRols")]
    public virtual Modulo IdModuloNavigation { get; set; } = null!;

    [ForeignKey("IdRol")]
    [InverseProperty("PermisosRols")]
    public virtual Role IdRolNavigation { get; set; } = null!;
}
