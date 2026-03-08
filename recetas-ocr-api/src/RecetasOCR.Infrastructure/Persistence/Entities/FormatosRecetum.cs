using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("FormatosReceta", Schema = "cat")]
[Index("Clave", Name = "UQ__Formatos__E8181E110DB6048B", IsUnique = true)]
public partial class FormatosRecetum
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public string Clave { get; set; } = null!;

    [StringLength(100)]
    public string Descripcion { get; set; } = null!;

    public bool Activo { get; set; }

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [InverseProperty("IdFormatoRecetaNavigation")]
    public virtual ICollection<GruposRecetum> GruposReceta { get; set; } = new List<GruposRecetum>();
}
