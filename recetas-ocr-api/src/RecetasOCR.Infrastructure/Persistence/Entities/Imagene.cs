using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RecetasOCR.Infrastructure.Persistence.Entities;

[Table("Imagenes", Schema = "rec")]
[Index("IdEstadoImagen", Name = "IX_Img_Estado")]
[Index("FechaSubida", Name = "IX_Img_FechaSubida")]
[Index("IdGrupo", Name = "IX_Img_Grupo")]
[Index("OrigenImagen", Name = "IX_Img_Origen")]
[Index("IdUsuarioSubida", Name = "IX_Img_Usuario")]
public partial class Imagene
{
    [Key]
    public Guid Id { get; set; }

    public Guid IdGrupo { get; set; }

    public int NumeroHoja { get; set; }

    [StringLength(500)]
    public string UrlBlobRaw { get; set; } = null!;

    [Column("UrlBlobOCR")]
    [StringLength(500)]
    public string? UrlBlobOcr { get; set; }

    [StringLength(500)]
    public string? UrlBlobIlegible { get; set; }

    [StringLength(200)]
    public string NombreArchivo { get; set; } = null!;

    public long? TamanioBytes { get; set; }

    [StringLength(10)]
    public string? FormatoImagen { get; set; }

    public int? AnchoPixeles { get; set; }

    public int? AltoPixeles { get; set; }

    [Column("ResolucionDPI")]
    public int? ResolucionDpi { get; set; }

    [StringLength(20)]
    public string OrigenImagen { get; set; } = null!;

    public DateTime? FechaTomaFoto { get; set; }

    [Column(TypeName = "decimal(9, 6)")]
    public decimal? GpsLatitud { get; set; }

    [Column(TypeName = "decimal(9, 6)")]
    public decimal? GpsLongitud { get; set; }

    [StringLength(200)]
    public string? ModeloDispositivo { get; set; }

    [StringLength(100)]
    public string? SistemaOperativo { get; set; }

    public Guid IdUsuarioSubida { get; set; }

    public Guid? IdSesion { get; set; }

    public DateTime FechaSubida { get; set; }

    [StringLength(50)]
    public string? IpOrigen { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? ScoreLegibilidad { get; set; }

    public bool? EsLegible { get; set; }

    [StringLength(200)]
    public string? MotivoBajaCalidad { get; set; }

    [StringLength(100)]
    public string? FolioCompleto { get; set; }

    [StringLength(100)]
    public string? FolioBase { get; set; }

    [StringLength(10)]
    public string? SufijoFolio { get; set; }

    [Column("CodigoCOU")]
    [StringLength(50)]
    public string? CodigoCou { get; set; }

    public bool EsCapturaManual { get; set; }

    public Guid? IdUsuarioCapturaManual { get; set; }

    public DateTime? FechaCapturaManual { get; set; }

    [StringLength(300)]
    public string? NotasCapturaManual { get; set; }

    public int IdEstadoImagen { get; set; }

    public DateTime FechaActualizacion { get; set; }

    [StringLength(500)]
    public string? ErrorProceso { get; set; }

    public int IntentosProceso { get; set; }

    [StringLength(200)]
    public string? ModificadoPor { get; set; }

    public DateTime FechaModificacion { get; set; }

    [InverseProperty("IdImagenNavigation")]
    public virtual ICollection<AsignacionesRevision> AsignacionesRevisions { get; set; } = new List<AsignacionesRevision>();

    [InverseProperty("IdImagenNavigation")]
    public virtual ICollection<ColaProcesamiento> ColaProcesamientos { get; set; } = new List<ColaProcesamiento>();

    [InverseProperty("IdImagenNavigation")]
    public virtual ICollection<HistorialCorreccione> HistorialCorrecciones { get; set; } = new List<HistorialCorreccione>();

    [InverseProperty("IdImagenNavigation")]
    public virtual ICollection<HistorialEstadosImagen> HistorialEstadosImagens { get; set; } = new List<HistorialEstadosImagen>();

    [ForeignKey("IdEstadoImagen")]
    [InverseProperty("Imagenes")]
    public virtual EstadosImagen IdEstadoImagenNavigation { get; set; } = null!;

    [ForeignKey("IdGrupo")]
    [InverseProperty("Imagenes")]
    public virtual GruposRecetum IdGrupoNavigation { get; set; } = null!;

    [ForeignKey("IdSesion")]
    [InverseProperty("Imagenes")]
    public virtual Sesione? IdSesionNavigation { get; set; }

    [ForeignKey("IdUsuarioCapturaManual")]
    [InverseProperty("ImageneIdUsuarioCapturaManualNavigations")]
    public virtual Usuario? IdUsuarioCapturaManualNavigation { get; set; }

    [ForeignKey("IdUsuarioSubida")]
    [InverseProperty("ImageneIdUsuarioSubidaNavigations")]
    public virtual Usuario IdUsuarioSubidaNavigation { get; set; } = null!;

    [InverseProperty("IdImagenNavigation")]
    public virtual ICollection<MedicamentosRecetum> MedicamentosReceta { get; set; } = new List<MedicamentosRecetum>();

    [InverseProperty("IdImagenNavigation")]
    public virtual ICollection<ResultadosExtraccion> ResultadosExtraccions { get; set; } = new List<ResultadosExtraccion>();

    [InverseProperty("IdImagenNavigation")]
    public virtual ICollection<ResultadosOcr> ResultadosOcrs { get; set; } = new List<ResultadosOcr>();

    [InverseProperty("IdImagenNavigation")]
    public virtual ICollection<RevisionesHumana> RevisionesHumanas { get; set; } = new List<RevisionesHumana>();
}
