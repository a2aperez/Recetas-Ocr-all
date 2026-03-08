using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("MedicamentosReceta", Schema = "med")]
[Index("IdGrupo", Name = "IX_Med_Grupo")]
[Index("IdImagen", Name = "IX_Med_Imagen")]
[Index("NombreComercial", Name = "IX_Med_NombreComercial")]
public partial class MedicamentosRecetum
{
    [Key]
    public Guid Id { get; set; }

    public Guid IdImagen { get; set; }

    public Guid IdGrupo { get; set; }

    public int? IdMedicamentoCatalogo { get; set; }

    public int NumeroPrescripcion { get; set; }

    [Column("CodigoCIE10")]
    [StringLength(20)]
    public string? CodigoCie10 { get; set; }

    [Column("DescripcionCIE10")]
    [StringLength(300)]
    public string? DescripcionCie10 { get; set; }

    [StringLength(200)]
    public string? NombreComercial { get; set; }

    [StringLength(200)]
    public string? SustanciaActiva { get; set; }

    [StringLength(200)]
    public string? Presentacion { get; set; }

    [StringLength(100)]
    public string? Dosis { get; set; }

    [Column("CodigoEAN")]
    [StringLength(50)]
    public string? CodigoEan { get; set; }

    [StringLength(100)]
    public string? CantidadTexto { get; set; }

    public int? CantidadNumero { get; set; }

    [StringLength(50)]
    public string? UnidadCantidad { get; set; }

    public int? IdViaAdministracion { get; set; }

    [StringLength(200)]
    public string? FrecuenciaTexto { get; set; }

    [StringLength(200)]
    public string? FrecuenciaExpandida { get; set; }

    [StringLength(100)]
    public string? DuracionTexto { get; set; }

    public int? DuracionDias { get; set; }

    [StringLength(500)]
    public string? IndicacionesCompletas { get; set; }

    [StringLength(100)]
    public string? NumeroAutorizacion { get; set; }

    public DateOnly? FechaSurtido { get; set; }

    [StringLength(50)]
    public string? ClaveProvFarm { get; set; }

    [Column("ClaveSATId")]
    public int? ClaveSatid { get; set; }

    [Column("ClaveUnidadSATId")]
    public int? ClaveUnidadSatid { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal? PrecioUnitario { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal? Importe { get; set; }

    [Column("IVATasa", TypeName = "decimal(5, 2)")]
    public decimal Ivatasa { get; set; }

    [Column("IEPSTasa", TypeName = "decimal(5, 2)")]
    public decimal Iepstasa { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaActualizacion { get; set; }

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [ForeignKey("ClaveSatid")]
    [InverseProperty("MedicamentosReceta")]
    public virtual ClavesSat? ClaveSat { get; set; }

    [ForeignKey("ClaveUnidadSatid")]
    [InverseProperty("MedicamentosReceta")]
    public virtual UnidadesSat? ClaveUnidadSat { get; set; }

    [InverseProperty("IdMedicamentoNavigation")]
    public virtual ICollection<HistorialCorreccione> HistorialCorrecciones { get; set; } = new List<HistorialCorreccione>();

    [ForeignKey("IdGrupo")]
    [InverseProperty("MedicamentosReceta")]
    public virtual GruposRecetum IdGrupoNavigation { get; set; } = null!;

    [ForeignKey("IdImagen")]
    [InverseProperty("MedicamentosReceta")]
    public virtual Imagene IdImagenNavigation { get; set; } = null!;

    [ForeignKey("IdMedicamentoCatalogo")]
    [InverseProperty("MedicamentosReceta")]
    public virtual Medicamento? IdMedicamentoCatalogoNavigation { get; set; }

    [ForeignKey("IdViaAdministracion")]
    [InverseProperty("MedicamentosReceta")]
    public virtual ViasAdministracion? IdViaAdministracionNavigation { get; set; }

    [InverseProperty("IdMedicamentoRecetaNavigation")]
    public virtual ICollection<PartidasPreFactura> PartidasPreFacturas { get; set; } = new List<PartidasPreFactura>();
}
