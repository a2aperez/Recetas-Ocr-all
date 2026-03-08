using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("Medicamentos", Schema = "cat")]
public partial class Medicamento
{
    [Key]
    public int Id { get; set; }

    [StringLength(200)]
    public string NombreComercial { get; set; } = null!;

    [StringLength(200)]
    public string? SustanciaActiva { get; set; }

    [StringLength(100)]
    public string? Presentacion { get; set; }

    [StringLength(100)]
    public string? Concentracion { get; set; }

    [Column("ClaveSAT")]
    [StringLength(20)]
    public string? ClaveSat { get; set; }

    [Column("ClaveUnidadSAT")]
    [StringLength(10)]
    public string? ClaveUnidadSat { get; set; }

    [Column("IVATasa", TypeName = "decimal(5, 2)")]
    public decimal Ivatasa { get; set; }

    [Column("IEPSTasa", TypeName = "decimal(5, 2)")]
    public decimal Iepstasa { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaAlta { get; set; }

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [InverseProperty("IdMedicamentoCatalogoNavigation")]
    public virtual ICollection<MedicamentosRecetum> MedicamentosReceta { get; set; } = new List<MedicamentosRecetum>();
}
