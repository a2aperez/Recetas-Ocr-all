using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("Sesiones", Schema = "seg")]
[Index("JwtTokenId", Name = "IX_Sesiones_JwtId")]
[Index("IdUsuario", "Estado", Name = "IX_Sesiones_Usuario")]
[Index("RefreshToken", Name = "UQ__Sesiones__DEA298DA02205374", IsUnique = true)]
[Index("JwtTokenId", Name = "UQ__Sesiones__EDF42BA313AD99E9", IsUnique = true)]
public partial class Sesione
{
    [Key]
    public Guid Id { get; set; }

    public Guid IdUsuario { get; set; }

    [StringLength(100)]
    public string JwtTokenId { get; set; } = null!;

    [StringLength(500)]
    public string RefreshToken { get; set; } = null!;

    [StringLength(200)]
    public string? Dispositivo { get; set; }

    [StringLength(50)]
    public string? TipoDispositivo { get; set; }

    [StringLength(100)]
    public string? SistemaOperativo { get; set; }

    [StringLength(20)]
    public string? VersionApp { get; set; }

    [StringLength(50)]
    public string? IpOrigen { get; set; }

    [StringLength(500)]
    public string? UserAgent { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime FechaExpiracion { get; set; }

    public DateTime FechaUltimaActividad { get; set; }

    [StringLength(20)]
    public string Estado { get; set; } = null!;

    [StringLength(200)]
    public string? MotivoRevocacion { get; set; }

    [ForeignKey("IdUsuario")]
    [InverseProperty("Sesiones")]
    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    [InverseProperty("IdSesionNavigation")]
    public virtual ICollection<Imagene> Imagenes { get; set; } = new List<Imagene>();
}
