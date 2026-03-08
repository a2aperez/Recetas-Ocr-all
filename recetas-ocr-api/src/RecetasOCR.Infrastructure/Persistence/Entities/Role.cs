using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("Roles", Schema = "seg")]
[Index("Clave", Name = "UQ__Roles__E8181E11B0158D47", IsUnique = true)]
public partial class Role
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public string Clave { get; set; } = null!;

    [StringLength(100)]
    public string Nombre { get; set; } = null!;

    [StringLength(300)]
    public string? Descripcion { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaAlta { get; set; }

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [InverseProperty("IdRolNavigation")]
    public virtual ICollection<PermisosRol> PermisosRols { get; set; } = new List<PermisosRol>();

    [InverseProperty("IdRolNavigation")]
    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
