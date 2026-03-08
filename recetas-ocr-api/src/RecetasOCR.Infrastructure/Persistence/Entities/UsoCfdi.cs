using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("UsoCFDI", Schema = "cat")]
[Index("Clave", Name = "UQ__UsoCFDI__E8181E1101A9E35D", IsUnique = true)]
public partial class UsoCfdi
{
    [Key]
    public int Id { get; set; }

    [StringLength(10)]
    public string Clave { get; set; } = null!;

    [StringLength(200)]
    public string Descripcion { get; set; } = null!;

    public bool AplicaFisica { get; set; }

    public bool AplicaMoral { get; set; }

    public bool Activo { get; set; }

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [InverseProperty("UsoCfdi")]
    public virtual ICollection<PreFactura> PreFacturas { get; set; } = new List<PreFactura>();
}
