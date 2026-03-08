using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("PartidasPreFactura", Schema = "fac")]
public partial class PartidasPreFactura
{
    [Key]
    public Guid Id { get; set; }

    public Guid IdPreFactura { get; set; }

    public Guid? IdMedicamentoReceta { get; set; }

    public int NumeroLinea { get; set; }

    [StringLength(20)]
    public string ClaveProdServ { get; set; } = null!;

    [StringLength(10)]
    public string ClaveUnidad { get; set; } = null!;

    [StringLength(100)]
    public string? NoIdentificacion { get; set; }

    [StringLength(500)]
    public string Descripcion { get; set; } = null!;

    [Column(TypeName = "decimal(10, 4)")]
    public decimal Cantidad { get; set; }

    [Column(TypeName = "decimal(12, 4)")]
    public decimal ValorUnitario { get; set; }

    [Column(TypeName = "decimal(12, 2)")]
    public decimal Descuento { get; set; }

    [Column(TypeName = "decimal(12, 2)")]
    public decimal Importe { get; set; }

    [StringLength(3)]
    public string ObjetoImpuesto { get; set; } = null!;

    [Column("IVATasa", TypeName = "decimal(5, 4)")]
    public decimal Ivatasa { get; set; }

    [Column("IVAImporte", TypeName = "decimal(12, 2)")]
    public decimal Ivaimporte { get; set; }

    [Column("IEPSTasa", TypeName = "decimal(5, 4)")]
    public decimal Iepstasa { get; set; }

    [Column("IEPSImporte", TypeName = "decimal(12, 2)")]
    public decimal Iepsimporte { get; set; }

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [ForeignKey("IdMedicamentoReceta")]
    [InverseProperty("PartidasPreFacturas")]
    public virtual MedicamentosRecetum? IdMedicamentoRecetaNavigation { get; set; }

    [ForeignKey("IdPreFactura")]
    [InverseProperty("PartidasPreFacturas")]
    public virtual PreFactura IdPreFacturaNavigation { get; set; } = null!;
}
