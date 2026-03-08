using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("PreFacturas", Schema = "fac")]
[Index("Estado", Name = "IX_PreFac_Estado")]
[Index("IdGrupo", Name = "IX_PreFac_Grupo")]
public partial class PreFactura
{
    [Key]
    public Guid Id { get; set; }

    public Guid IdGrupo { get; set; }

    public int IdEmisor { get; set; }

    public Guid IdReceptor { get; set; }

    [StringLength(1)]
    public string TipoComprobante { get; set; } = null!;

    [StringLength(5)]
    public string Version { get; set; } = null!;

    public int MetodoPagoId { get; set; }

    public int FormaPagoId { get; set; }

    public int MonedaId { get; set; }

    [Column("UsoCFDIId")]
    public int UsoCfdiid { get; set; }

    [Column(TypeName = "decimal(10, 4)")]
    public decimal TipoCambio { get; set; }

    [StringLength(3)]
    public string Exportacion { get; set; } = null!;

    [Column(TypeName = "decimal(12, 2)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(12, 2)")]
    public decimal Descuento { get; set; }

    [Column("TotalIVA", TypeName = "decimal(12, 2)")]
    public decimal TotalIva { get; set; }

    [Column("TotalIEPS", TypeName = "decimal(12, 2)")]
    public decimal TotalIeps { get; set; }

    [Column(TypeName = "decimal(12, 2)")]
    public decimal Total { get; set; }

    [StringLength(30)]
    public string Estado { get; set; } = null!;

    public DateTime FechaGeneracion { get; set; }

    public DateTime? FechaAprobacion { get; set; }

    public Guid? IdUsuarioAprobacion { get; set; }

    public int IntentosTimbrado { get; set; }

    [StringLength(500)]
    public string? UltimoErrorTimbrado { get; set; }

    [StringLength(500)]
    public string? ObservacionesFiscales { get; set; }

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [InverseProperty("IdPreFacturaNavigation")]
    public virtual ICollection<Cfdi> Cfdis { get; set; } = new List<Cfdi>();

    [ForeignKey("FormaPagoId")]
    [InverseProperty("PreFacturas")]
    public virtual FormasPago FormaPago { get; set; } = null!;

    [ForeignKey("IdEmisor")]
    [InverseProperty("PreFacturas")]
    public virtual Emisore IdEmisorNavigation { get; set; } = null!;

    [ForeignKey("IdGrupo")]
    [InverseProperty("PreFacturas")]
    public virtual GruposRecetum IdGrupoNavigation { get; set; } = null!;

    [ForeignKey("IdReceptor")]
    [InverseProperty("PreFacturas")]
    public virtual Receptore IdReceptorNavigation { get; set; } = null!;

    [ForeignKey("IdUsuarioAprobacion")]
    [InverseProperty("PreFacturas")]
    public virtual Usuario? IdUsuarioAprobacionNavigation { get; set; }

    [ForeignKey("MetodoPagoId")]
    [InverseProperty("PreFacturas")]
    public virtual MetodosPago MetodoPago { get; set; } = null!;

    [ForeignKey("MonedaId")]
    [InverseProperty("PreFacturas")]
    public virtual Moneda Moneda { get; set; } = null!;

    [InverseProperty("IdPreFacturaNavigation")]
    public virtual ICollection<PartidasPreFactura> PartidasPreFacturas { get; set; } = new List<PartidasPreFactura>();

    [ForeignKey("UsoCfdiid")]
    [InverseProperty("PreFacturas")]
    public virtual UsoCfdi UsoCfdi { get; set; } = null!;
}
