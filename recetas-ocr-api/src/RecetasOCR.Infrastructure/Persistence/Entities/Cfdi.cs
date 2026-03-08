using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("CFDI", Schema = "fac")]
[Index("Estado", Name = "IX_CFDI_Estado")]
[Index("IdGrupo", Name = "IX_CFDI_Grupo")]
[Index("Rfcreceptor", Name = "IX_CFDI_RFCReceptor")]
[Index("Uuid", Name = "IX_CFDI_UUID")]
[Index("Uuid", Name = "UQ__CFDI__65A475E6C75FC353", IsUnique = true)]
public partial class Cfdi
{
    [Key]
    public Guid Id { get; set; }

    public Guid IdPreFactura { get; set; }

    public Guid IdGrupo { get; set; }

    [Column("UUID")]
    [StringLength(36)]
    public string Uuid { get; set; } = null!;

    public DateTime FechaTimbrado { get; set; }

    [StringLength(5)]
    public string Version { get; set; } = null!;

    [Column("RFCEmisor")]
    [StringLength(13)]
    public string Rfcemisor { get; set; } = null!;

    [StringLength(300)]
    public string? NombreEmisor { get; set; }

    [Column("RFCReceptor")]
    [StringLength(13)]
    public string Rfcreceptor { get; set; } = null!;

    [StringLength(300)]
    public string? NombreReceptor { get; set; }

    [Column(TypeName = "decimal(12, 2)")]
    public decimal Total { get; set; }

    [Column("SelloCFDI")]
    public string? SelloCfdi { get; set; }

    [Column("SelloSAT")]
    public string? SelloSat { get; set; }

    [Column("CadenaOriginalSAT")]
    public string? CadenaOriginalSat { get; set; }

    [Column("NoCertificadoSAT")]
    [StringLength(30)]
    public string? NoCertificadoSat { get; set; }

    [StringLength(30)]
    public string? NoCertificadoEmisor { get; set; }

    [Column("UrlBlobXML")]
    [StringLength(500)]
    public string UrlBlobXml { get; set; } = null!;

    [Column("UrlBlobPDF")]
    [StringLength(500)]
    public string? UrlBlobPdf { get; set; }

    [Column("NombrePAC")]
    [StringLength(100)]
    public string? NombrePac { get; set; }

    [Column("RespuestaJsonPAC")]
    public string? RespuestaJsonPac { get; set; }

    [StringLength(30)]
    public string Estado { get; set; } = null!;

    public DateTime? FechaCancelacion { get; set; }

    [StringLength(200)]
    public string? MotivoCancelacion { get; set; }

    [Column("UUIDSustitucion")]
    [StringLength(36)]
    public string? Uuidsustitucion { get; set; }

    public DateTime FechaCreacion { get; set; }

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [ForeignKey("IdGrupo")]
    [InverseProperty("Cfdis")]
    public virtual GruposRecetum IdGrupoNavigation { get; set; } = null!;

    [ForeignKey("IdPreFactura")]
    [InverseProperty("Cfdis")]
    public virtual PreFactura IdPreFacturaNavigation { get; set; } = null!;

    [InverseProperty("IdCfdiNavigation")]
    public virtual ICollection<PartidasCfdi> PartidasCfdis { get; set; } = new List<PartidasCfdi>();
}
