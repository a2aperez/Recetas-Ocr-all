using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("Usuarios", Schema = "seg")]
[Index("Email", Name = "IX_Usuarios_Email")]
[Index("IdRol", Name = "IX_Usuarios_Rol")]
[Index("Username", Name = "IX_Usuarios_Username")]
[Index("Username", Name = "UQ__Usuarios__536C85E46727C3A4", IsUnique = true)]
[Index("Email", Name = "UQ__Usuarios__A9D10534CDAA8012", IsUnique = true)]
public partial class Usuario
{
    [Key]
    public Guid Id { get; set; }

    [StringLength(100)]
    public string Username { get; set; } = null!;

    [StringLength(200)]
    public string Email { get; set; } = null!;

    [StringLength(500)]
    public string PasswordHash { get; set; } = null!;

    [StringLength(200)]
    public string NombreCompleto { get; set; } = null!;

    [StringLength(30)]
    public string? Telefono { get; set; }

    public int IdRol { get; set; }

    public int? IdAseguradoraAsignada { get; set; }

    public bool Activo { get; set; }

    public bool RequiereCambioPassword { get; set; }

    public DateTime? PasswordExpiraEn { get; set; }

    public int IntentosFallidos { get; set; }

    public DateTime? BloqueadoHasta { get; set; }

    [StringLength(500)]
    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpira { get; set; }

    public DateTime FechaAlta { get; set; }

    public DateTime FechaActualizacion { get; set; }

    public DateTime? UltimoAcceso { get; set; }

    [StringLength(100)]
    public string? CreadoPor { get; set; }

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [InverseProperty("IdUsuarioAsignadoNavigation")]
    public virtual ICollection<AsignacionesRevision> AsignacionesRevisionIdUsuarioAsignadoNavigations { get; set; } = new List<AsignacionesRevision>();

    [InverseProperty("IdUsuarioAsignoPorNavigation")]
    public virtual ICollection<AsignacionesRevision> AsignacionesRevisionIdUsuarioAsignoPorNavigations { get; set; } = new List<AsignacionesRevision>();

    [InverseProperty("IdUsuarioAltaNavigation")]
    public virtual ICollection<GruposRecetum> GruposReceta { get; set; } = new List<GruposRecetum>();

    [InverseProperty("IdUsuarioNavigation")]
    public virtual ICollection<HistorialCorreccione> HistorialCorrecciones { get; set; } = new List<HistorialCorreccione>();

    [InverseProperty("IdUsuarioNavigation")]
    public virtual ICollection<HistorialEstadosGrupo> HistorialEstadosGrupos { get; set; } = new List<HistorialEstadosGrupo>();

    [InverseProperty("IdUsuarioNavigation")]
    public virtual ICollection<HistorialEstadosImagen> HistorialEstadosImagens { get; set; } = new List<HistorialEstadosImagen>();

    [ForeignKey("IdAseguradoraAsignada")]
    [InverseProperty("Usuarios")]
    public virtual Aseguradora? IdAseguradoraAsignadaNavigation { get; set; }

    [ForeignKey("IdRol")]
    [InverseProperty("Usuarios")]
    public virtual Role IdRolNavigation { get; set; } = null!;

    [InverseProperty("IdUsuarioCapturaManualNavigation")]
    public virtual ICollection<Imagene> ImageneIdUsuarioCapturaManualNavigations { get; set; } = new List<Imagene>();

    [InverseProperty("IdUsuarioSubidaNavigation")]
    public virtual ICollection<Imagene> ImageneIdUsuarioSubidaNavigations { get; set; } = new List<Imagene>();

    [InverseProperty("IdUsuarioNavigation")]
    public virtual ICollection<LogAcceso> LogAccesos { get; set; } = new List<LogAcceso>();

    [InverseProperty("IdUsuarioNavigation")]
    public virtual ICollection<PermisosUsuario> PermisosUsuarios { get; set; } = new List<PermisosUsuario>();

    [InverseProperty("IdUsuarioAprobacionNavigation")]
    public virtual ICollection<PreFactura> PreFacturas { get; set; } = new List<PreFactura>();

    [InverseProperty("IdUsuarioRevisorNavigation")]
    public virtual ICollection<RevisionesHumana> RevisionesHumanas { get; set; } = new List<RevisionesHumana>();

    [InverseProperty("IdUsuarioNavigation")]
    public virtual ICollection<Sesione> Sesiones { get; set; } = new List<Sesione>();

    [InverseProperty("IdUsuarioSolicitaNavigation")]
    public virtual ICollection<SolicitudesAutorizacion> SolicitudesAutorizacions { get; set; } = new List<SolicitudesAutorizacion>();
}
