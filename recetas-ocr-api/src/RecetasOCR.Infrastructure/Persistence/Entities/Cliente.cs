using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("Clientes", Schema = "rec")]
public partial class Cliente
{
    [Key]
    public Guid Id { get; set; }

    [StringLength(200)]
    public string NombreCompleto { get; set; } = null!;

    [StringLength(100)]
    public string? ApellidoPaterno { get; set; }

    [StringLength(100)]
    public string? ApellidoMaterno { get; set; }

    [StringLength(100)]
    public string? Nombre { get; set; }

    public DateOnly? FechaNacimiento { get; set; }

    [Column("RFC")]
    [StringLength(13)]
    public string? Rfc { get; set; }

    [Column("CURP")]
    [StringLength(18)]
    public string? Curp { get; set; }

    [StringLength(10)]
    public string? CodigoPostal { get; set; }

    public int? RegimenFiscalId { get; set; }

    [StringLength(200)]
    public string? Email { get; set; }

    public DateTime FechaAlta { get; set; }

    public DateTime FechaActualizacion { get; set; }

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [InverseProperty("IdClienteNavigation")]
    public virtual ICollection<GruposRecetum> GruposReceta { get; set; } = new List<GruposRecetum>();

    [InverseProperty("IdClienteNavigation")]
    public virtual ICollection<Receptore> Receptores { get; set; } = new List<Receptore>();

    [ForeignKey("RegimenFiscalId")]
    [InverseProperty("Clientes")]
    public virtual RegimenFiscal? RegimenFiscal { get; set; }
}
