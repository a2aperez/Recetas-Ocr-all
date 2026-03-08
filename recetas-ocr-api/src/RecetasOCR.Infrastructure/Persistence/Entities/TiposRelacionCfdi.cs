using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("TiposRelacionCFDI", Schema = "cat")]
[Index("Clave", Name = "UQ__TiposRel__E8181E1162D51431", IsUnique = true)]
public partial class TiposRelacionCfdi
{
    [Key]
    public int Id { get; set; }

    [StringLength(5)]
    public string Clave { get; set; } = null!;

    [StringLength(200)]
    public string Descripcion { get; set; } = null!;

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }
}
