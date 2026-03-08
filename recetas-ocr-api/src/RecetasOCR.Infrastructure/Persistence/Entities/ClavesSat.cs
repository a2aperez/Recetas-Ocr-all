using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("ClavesSAT", Schema = "cat")]
[Index("Clave", Name = "UQ__ClavesSA__E8181E114CEEF96D", IsUnique = true)]
public partial class ClavesSat
{
    [Key]
    public int Id { get; set; }

    [StringLength(20)]
    public string Clave { get; set; } = null!;

    [StringLength(300)]
    public string Descripcion { get; set; } = null!;

    [StringLength(50)]
    public string Tipo { get; set; } = null!;

    public bool Activo { get; set; }

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [InverseProperty("ClaveSat")]
    public virtual ICollection<MedicamentosRecetum> MedicamentosReceta { get; set; } = new List<MedicamentosRecetum>();
}
