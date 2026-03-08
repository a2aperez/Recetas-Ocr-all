using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("UnidadesSAT", Schema = "cat")]
[Index("Clave", Name = "UQ__Unidades__E8181E1106B7C51F", IsUnique = true)]
public partial class UnidadesSat
{
    [Key]
    public int Id { get; set; }

    [StringLength(10)]
    public string Clave { get; set; } = null!;

    [StringLength(100)]
    public string Nombre { get; set; } = null!;

    [StringLength(200)]
    public string? Descripcion { get; set; }

    public bool Activo { get; set; }

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [InverseProperty("ClaveUnidadSat")]
    public virtual ICollection<MedicamentosRecetum> MedicamentosReceta { get; set; } = new List<MedicamentosRecetum>();
}
