using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("EstadosImagen", Schema = "cat")]
[Index("Clave", Name = "UQ__EstadosI__E8181E112E687D6C", IsUnique = true)]
public partial class EstadosImagen
{
    [Key]
    public int Id { get; set; }

    [StringLength(60)]
    public string Clave { get; set; } = null!;

    [StringLength(150)]
    public string Descripcion { get; set; } = null!;

    public int Orden { get; set; }

    public bool EsFinal { get; set; }

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [InverseProperty("EstadoAnteriorNavigation")]
    public virtual ICollection<HistorialEstadosImagen> HistorialEstadosImagenEstadoAnteriorNavigations { get; set; } = new List<HistorialEstadosImagen>();

    [InverseProperty("EstadoNuevoNavigation")]
    public virtual ICollection<HistorialEstadosImagen> HistorialEstadosImagenEstadoNuevoNavigations { get; set; } = new List<HistorialEstadosImagen>();

    [InverseProperty("IdEstadoImagenNavigation")]
    public virtual ICollection<Imagene> Imagenes { get; set; } = new List<Imagene>();
}
