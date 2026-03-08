using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("RegimenFiscal", Schema = "cat")]
[Index("Clave", Name = "UQ__RegimenF__E8181E11F8591989", IsUnique = true)]
public partial class RegimenFiscal
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

    [InverseProperty("RegimenFiscal")]
    public virtual ICollection<Cliente> Clientes { get; set; } = new List<Cliente>();

    [InverseProperty("RegimenFiscal")]
    public virtual ICollection<Emisore> Emisores { get; set; } = new List<Emisore>();

    [InverseProperty("RegimenFiscal")]
    public virtual ICollection<Receptore> Receptores { get; set; } = new List<Receptore>();
}
