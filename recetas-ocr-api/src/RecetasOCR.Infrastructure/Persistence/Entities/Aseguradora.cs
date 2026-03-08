using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("Aseguradoras", Schema = "cat")]
[Index("Clave", Name = "UQ__Asegurad__E8181E11CDAE4F6C", IsUnique = true)]
public partial class Aseguradora
{
    [Key]
    public int Id { get; set; }

    public int? IdAseguradoraPadre { get; set; }

    [StringLength(50)]
    public string Clave { get; set; } = null!;

    [StringLength(150)]
    public string Nombre { get; set; } = null!;

    [StringLength(50)]
    public string? NombreCorto { get; set; }

    [StringLength(100)]
    public string? OperadorMedico { get; set; }

    [Column("RFC")]
    [StringLength(13)]
    public string? Rfc { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaAlta { get; set; }

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [InverseProperty("IdAseguradoraNavigation")]
    public virtual ICollection<Emisore> Emisores { get; set; } = new List<Emisore>();

    [InverseProperty("IdAseguradoraNavigation")]
    public virtual ICollection<GruposRecetum> GruposReceta { get; set; } = new List<GruposRecetum>();

    [ForeignKey("IdAseguradoraPadre")]
    [InverseProperty("InverseIdAseguradoraPadreNavigation")]
    public virtual Aseguradora? IdAseguradoraPadreNavigation { get; set; }

    [InverseProperty("IdAseguradoraPadreNavigation")]
    public virtual ICollection<Aseguradora> InverseIdAseguradoraPadreNavigation { get; set; } = new List<Aseguradora>();

    [InverseProperty("IdAseguradoraAsignadaNavigation")]
    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
