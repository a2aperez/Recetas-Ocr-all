using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("Modulos", Schema = "seg")]
[Index("Clave", Name = "UQ__Modulos__E8181E118AA9F150", IsUnique = true)]
public partial class Modulo
{
    [Key]
    public int Id { get; set; }

    [StringLength(80)]
    public string Clave { get; set; } = null!;

    [StringLength(150)]
    public string Nombre { get; set; } = null!;

    [StringLength(300)]
    public string? Descripcion { get; set; }

    public bool Activo { get; set; }

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [InverseProperty("IdModuloNavigation")]
    public virtual ICollection<PermisosRol> PermisosRols { get; set; } = new List<PermisosRol>();

    [InverseProperty("IdModuloNavigation")]
    public virtual ICollection<PermisosUsuario> PermisosUsuarios { get; set; } = new List<PermisosUsuario>();
}
