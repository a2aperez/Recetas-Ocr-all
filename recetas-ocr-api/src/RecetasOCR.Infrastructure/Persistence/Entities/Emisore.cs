using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("Emisores", Schema = "fac")]
public partial class Emisore
{
    [Key]
    public int Id { get; set; }

    public int IdAseguradora { get; set; }

    [Column("RFC")]
    [StringLength(13)]
    public string Rfc { get; set; } = null!;

    [StringLength(300)]
    public string RazonSocial { get; set; } = null!;

    public int RegimenFiscalId { get; set; }

    [StringLength(10)]
    public string CodigoPostal { get; set; } = null!;

    [StringLength(30)]
    public string? NoCertificado { get; set; }

    [StringLength(500)]
    public string? RutaCertificado { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaAlta { get; set; }

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [ForeignKey("IdAseguradora")]
    [InverseProperty("Emisores")]
    public virtual Aseguradora IdAseguradoraNavigation { get; set; } = null!;

    [InverseProperty("IdEmisorNavigation")]
    public virtual ICollection<PreFactura> PreFacturas { get; set; } = new List<PreFactura>();

    [ForeignKey("RegimenFiscalId")]
    [InverseProperty("Emisores")]
    public virtual RegimenFiscal RegimenFiscal { get; set; } = null!;
}
