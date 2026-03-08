using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("Especialidades", Schema = "cat")]
[Index("Clave", Name = "UQ__Especial__E8181E11D7BB908B", IsUnique = true)]
public partial class Especialidade
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public string Clave { get; set; } = null!;

    [StringLength(150)]
    public string Descripcion { get; set; } = null!;

    public bool Activo { get; set; }

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [InverseProperty("IdEspecialidadNavigation")]
    public virtual ICollection<GruposRecetum> GruposReceta { get; set; } = new List<GruposRecetum>();
}
