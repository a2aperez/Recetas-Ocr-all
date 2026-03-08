using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("Parametros", Schema = "cfg")]
[Index("Clave", Name = "UQ__Parametr__E8181E115D22611F", IsUnique = true)]
public partial class Parametro
{
    [Key]
    public int Id { get; set; }

    [StringLength(100)]
    public string Clave { get; set; } = null!;

    [StringLength(1000)]
    public string Valor { get; set; } = null!;

    [StringLength(300)]
    public string? Descripcion { get; set; }

    [StringLength(20)]
    public string Tipo { get; set; } = null!;

    public bool EsSecreto { get; set; }

    public DateTime FechaAlta { get; set; }

    public DateTime FechaActualizacion { get; set; }

    [StringLength(100)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }
}
