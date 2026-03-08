using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("ConfiguracionesOCR", Schema = "cfg")]
[Index("Nombre", Name = "UQ__Configur__75E3EFCF13D75F51", IsUnique = true)]
public partial class ConfiguracionesOcr
{
    [Key]
    public int Id { get; set; }

    [StringLength(100)]
    public string Nombre { get; set; } = null!;

    [StringLength(80)]
    public string Proveedor { get; set; } = null!;

    [StringLength(500)]
    public string UrlBase { get; set; } = null!;

    [StringLength(1000)]
    public string? ApiKeyEncriptada { get; set; }

    [StringLength(100)]
    public string? Modelo { get; set; }

    [StringLength(20)]
    public string? Version { get; set; }

    public int TimeoutSegundos { get; set; }

    public int MaxReintentos { get; set; }

    [Column("CostoPorImagenUSD", TypeName = "decimal(10, 6)")]
    public decimal CostoPorImagenUsd { get; set; }

    public bool EsPrincipal { get; set; }

    public bool Activo { get; set; }

    public string? ConfigJson { get; set; }

    public DateTime FechaAlta { get; set; }

    public DateTime FechaActualizacion { get; set; }

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [InverseProperty("IdConfiguracionOcrNavigation")]
    public virtual ICollection<ColaProcesamiento> ColaProcesamientos { get; set; } = new List<ColaProcesamiento>();

    [InverseProperty("IdConfiguracionOcrNavigation")]
    public virtual ICollection<ResultadosExtraccion> ResultadosExtraccions { get; set; } = new List<ResultadosExtraccion>();

    [InverseProperty("IdConfiguracionOcrNavigation")]
    public virtual ICollection<ResultadosOcr> ResultadosOcrs { get; set; } = new List<ResultadosOcr>();
}
