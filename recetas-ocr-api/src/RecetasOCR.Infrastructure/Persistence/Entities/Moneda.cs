using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("Monedas", Schema = "cat")]
[Index("Clave", Name = "UQ__Monedas__E8181E11B3ACE8FF", IsUnique = true)]
public partial class Moneda
{
    [Key]
    public int Id { get; set; }

    [StringLength(5)]
    public string Clave { get; set; } = null!;

    [StringLength(100)]
    public string Descripcion { get; set; } = null!;

    public bool Activo { get; set; }

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [InverseProperty("Moneda")]
    public virtual ICollection<PreFactura> PreFacturas { get; set; } = new List<PreFactura>();
}
