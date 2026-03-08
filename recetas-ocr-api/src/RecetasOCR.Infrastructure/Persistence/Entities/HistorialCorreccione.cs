using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("HistorialCorrecciones", Schema = "aud")]
public partial class HistorialCorreccione
{
    [Key]
    public long Id { get; set; }

    public Guid? IdImagen { get; set; }

    public Guid? IdGrupo { get; set; }

    public Guid? IdMedicamento { get; set; }

    [StringLength(100)]
    public string Tabla { get; set; } = null!;

    [StringLength(100)]
    public string Campo { get; set; } = null!;

    [StringLength(500)]
    public string? ValorAnterior { get; set; }

    [StringLength(500)]
    public string? ValorNuevo { get; set; }

    [StringLength(50)]
    public string TipoCorreccion { get; set; } = null!;

    public Guid? IdUsuario { get; set; }

    public DateTime FechaCorreccion { get; set; }

    [ForeignKey("IdGrupo")]
    [InverseProperty("HistorialCorrecciones")]
    public virtual GruposRecetum? IdGrupoNavigation { get; set; }

    [ForeignKey("IdImagen")]
    [InverseProperty("HistorialCorrecciones")]
    public virtual Imagene? IdImagenNavigation { get; set; }

    [ForeignKey("IdMedicamento")]
    [InverseProperty("HistorialCorrecciones")]
    public virtual MedicamentosRecetum? IdMedicamentoNavigation { get; set; }

    [ForeignKey("IdUsuario")]
    [InverseProperty("HistorialCorrecciones")]
    public virtual Usuario? IdUsuarioNavigation { get; set; }
}
