using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("MetodosPago", Schema = "cat")]
[Index("Clave", Name = "UQ__MetodosP__E8181E11BD46A536", IsUnique = true)]
public partial class MetodosPago
{
    [Key]
    public int Id { get; set; }

    [StringLength(5)]
    public string Clave { get; set; } = null!;

    [StringLength(100)]
    public string Descripcion { get; set; } = null!;

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [InverseProperty("MetodoPago")]
    public virtual ICollection<PreFactura> PreFacturas { get; set; } = new List<PreFactura>();
}
