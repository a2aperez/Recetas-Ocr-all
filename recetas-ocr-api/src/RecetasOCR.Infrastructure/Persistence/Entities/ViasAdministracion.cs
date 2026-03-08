using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("ViasAdministracion", Schema = "cat")]
[Index("Clave", Name = "UQ__ViasAdmi__E8181E1159818924", IsUnique = true)]
public partial class ViasAdministracion
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public string Clave { get; set; } = null!;

    [StringLength(100)]
    public string Descripcion { get; set; } = null!;

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [InverseProperty("IdViaAdministracionNavigation")]
    public virtual ICollection<MedicamentosRecetum> MedicamentosReceta { get; set; } = new List<MedicamentosRecetum>();
}
