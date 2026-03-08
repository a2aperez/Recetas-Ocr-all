using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("Receptores", Schema = "fac")]
public partial class Receptore
{
    [Key]
    public Guid Id { get; set; }

    public Guid? IdCliente { get; set; }

    [Column("RFC")]
    [StringLength(13)]
    public string Rfc { get; set; } = null!;

    [StringLength(300)]
    public string NombreRazonSocial { get; set; } = null!;

    public int RegimenFiscalId { get; set; }

    [StringLength(10)]
    public string CodigoPostal { get; set; } = null!;

    [StringLength(200)]
    public string? Email { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaAlta { get; set; }

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [ForeignKey("IdCliente")]
    [InverseProperty("Receptores")]
    public virtual Cliente? IdClienteNavigation { get; set; }

    [InverseProperty("IdReceptorNavigation")]
    public virtual ICollection<PreFactura> PreFacturas { get; set; } = new List<PreFactura>();

    [ForeignKey("RegimenFiscalId")]
    [InverseProperty("Receptores")]
    public virtual RegimenFiscal RegimenFiscal { get; set; } = null!;
}
