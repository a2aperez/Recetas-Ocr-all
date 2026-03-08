using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("GruposReceta", Schema = "rec")]
[Index("IdAseguradora", Name = "IX_Grupos_Aseguradora")]
[Index("IdEstadoGrupo", Name = "IX_Grupos_Estado")]
[Index("FechaConsulta", Name = "IX_Grupos_FechaConsulta")]
[Index("IdCliente", "IdAseguradora", "FechaConsulta", Name = "IX_Grupos_SinFolio")]
public partial class GruposRecetum
{
    [Key]
    public Guid Id { get; set; }

    [StringLength(100)]
    public string? FolioBase { get; set; }

    public Guid? IdCliente { get; set; }

    public int IdAseguradora { get; set; }

    public int IdFormatoReceta { get; set; }

    [Column("NUR")]
    [StringLength(100)]
    public string? Nur { get; set; }

    [StringLength(100)]
    public string? Credencial { get; set; }

    [StringLength(100)]
    public string? NumeroAutorizacion { get; set; }

    [StringLength(200)]
    public string? NombrePaciente { get; set; }

    [StringLength(100)]
    public string? ApellidoPaterno { get; set; }

    [StringLength(100)]
    public string? ApellidoMaterno { get; set; }

    [StringLength(100)]
    public string? NombrePac { get; set; }

    public DateOnly? FechaNacimientoPac { get; set; }

    [StringLength(100)]
    public string? NominaPaciente { get; set; }

    [StringLength(100)]
    public string? Elegibilidad { get; set; }

    [Column("ClaveDH")]
    [StringLength(50)]
    public string? ClaveDh { get; set; }

    [StringLength(100)]
    public string? ClaveBeneficiario { get; set; }

    [StringLength(50)]
    public string? ClaveMedicion { get; set; }

    [StringLength(200)]
    public string? NombreMedico { get; set; }

    [StringLength(100)]
    public string? ApellidoPaternoMedico { get; set; }

    [StringLength(100)]
    public string? ApellidoMaternoMedico { get; set; }

    [StringLength(100)]
    public string? NombreMedicoNombre { get; set; }

    [StringLength(50)]
    public string? CedulaMedico { get; set; }

    [StringLength(50)]
    public string? ClaveMedico { get; set; }

    public int? IdEspecialidad { get; set; }

    [StringLength(150)]
    public string? EspecialidadTexto { get; set; }

    [StringLength(300)]
    public string? DireccionMedico { get; set; }

    [StringLength(50)]
    public string? TelefonoMedico { get; set; }

    [StringLength(200)]
    public string? InstitucionMedico { get; set; }

    [Column("CodigoCIE10")]
    [StringLength(20)]
    public string? CodigoCie10 { get; set; }

    [StringLength(500)]
    public string? DescripcionDiagnostico { get; set; }

    public DateOnly? FechaConsulta { get; set; }

    public TimeOnly? HoraConsulta { get; set; }

    public int TotalImagenes { get; set; }

    public int TotalMedicamentos { get; set; }

    public int IdEstadoGrupo { get; set; }

    public Guid? IdUsuarioAlta { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaActualizacion { get; set; }

    public DateTime? FechaCompletado { get; set; }

    [StringLength(500)]
    public string? NotasGrupo { get; set; }

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [InverseProperty("IdGrupoNavigation")]
    public virtual ICollection<AsignacionesRevision> AsignacionesRevisions { get; set; } = new List<AsignacionesRevision>();

    [InverseProperty("IdGrupoNavigation")]
    public virtual ICollection<Cfdi> Cfdis { get; set; } = new List<Cfdi>();

    [InverseProperty("IdGrupoNavigation")]
    public virtual ICollection<HistorialCorreccione> HistorialCorrecciones { get; set; } = new List<HistorialCorreccione>();

    [InverseProperty("IdGrupoNavigation")]
    public virtual ICollection<HistorialEstadosGrupo> HistorialEstadosGrupos { get; set; } = new List<HistorialEstadosGrupo>();

    [ForeignKey("IdAseguradora")]
    [InverseProperty("GruposReceta")]
    public virtual Aseguradora IdAseguradoraNavigation { get; set; } = null!;

    [ForeignKey("IdCliente")]
    [InverseProperty("GruposReceta")]
    public virtual Cliente? IdClienteNavigation { get; set; }

    [ForeignKey("IdEspecialidad")]
    [InverseProperty("GruposReceta")]
    public virtual Especialidade? IdEspecialidadNavigation { get; set; }

    [ForeignKey("IdEstadoGrupo")]
    [InverseProperty("GruposReceta")]
    public virtual EstadosGrupo IdEstadoGrupoNavigation { get; set; } = null!;

    [ForeignKey("IdFormatoReceta")]
    [InverseProperty("GruposReceta")]
    public virtual FormatosRecetum IdFormatoRecetaNavigation { get; set; } = null!;

    [ForeignKey("IdUsuarioAlta")]
    [InverseProperty("GruposReceta")]
    public virtual Usuario? IdUsuarioAltaNavigation { get; set; }

    [InverseProperty("IdGrupoNavigation")]
    public virtual ICollection<Imagene> Imagenes { get; set; } = new List<Imagene>();

    [InverseProperty("IdGrupoNavigation")]
    public virtual ICollection<MedicamentosRecetum> MedicamentosReceta { get; set; } = new List<MedicamentosRecetum>();

    [InverseProperty("IdGrupoNavigation")]
    public virtual ICollection<PreFactura> PreFacturas { get; set; } = new List<PreFactura>();

    [InverseProperty("IdGrupoNavigation")]
    public virtual ICollection<RevisionesHumana> RevisionesHumanas { get; set; } = new List<RevisionesHumana>();

    [InverseProperty("IdGrupoNavigation")]
    public virtual ICollection<SolicitudesAutorizacion> SolicitudesAutorizacions { get; set; } = new List<SolicitudesAutorizacion>();
}
