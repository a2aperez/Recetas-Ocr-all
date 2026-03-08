using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using RecetasOCR.Infrastructure.Persistence.Entities;

namespace RecetasOCR.Infrastructure.Persistence;

public partial class RecetasOcrDbContext : DbContext
{
    public RecetasOcrDbContext(DbContextOptions<RecetasOcrDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Aseguradora> Aseguradoras { get; set; }

    public virtual DbSet<AsignacionesRevision> AsignacionesRevisions { get; set; }

    public virtual DbSet<Cfdi> Cfdis { get; set; }

    public virtual DbSet<ClavesSat> ClavesSats { get; set; }

    public virtual DbSet<Cliente> Clientes { get; set; }

    public virtual DbSet<ColaProcesamiento> ColaProcesamientos { get; set; }

    public virtual DbSet<ConfiguracionesOcr> ConfiguracionesOcrs { get; set; }

    public virtual DbSet<Emisore> Emisores { get; set; }

    public virtual DbSet<Especialidade> Especialidades { get; set; }

    public virtual DbSet<EstadosGrupo> EstadosGrupos { get; set; }

    public virtual DbSet<EstadosImagen> EstadosImagens { get; set; }

    public virtual DbSet<FormasPago> FormasPagos { get; set; }

    public virtual DbSet<FormatosRecetum> FormatosReceta { get; set; }

    public virtual DbSet<GruposRecetum> GruposReceta { get; set; }

    public virtual DbSet<HistorialCorreccione> HistorialCorrecciones { get; set; }

    public virtual DbSet<HistorialEstadosGrupo> HistorialEstadosGrupos { get; set; }

    public virtual DbSet<HistorialEstadosImagen> HistorialEstadosImagens { get; set; }

    public virtual DbSet<Imagene> Imagenes { get; set; }

    public virtual DbSet<LogAcceso> LogAccesos { get; set; }

    public virtual DbSet<LogProcesamiento> LogProcesamientos { get; set; }

    public virtual DbSet<Medicamento> Medicamentos { get; set; }

    public virtual DbSet<MedicamentosRecetum> MedicamentosReceta { get; set; }

    public virtual DbSet<MetodosPago> MetodosPagos { get; set; }

    public virtual DbSet<Modulo> Modulos { get; set; }

    public virtual DbSet<Moneda> Monedas { get; set; }

    public virtual DbSet<Parametro> Parametros { get; set; }

    public virtual DbSet<PartidasCfdi> PartidasCfdis { get; set; }

    public virtual DbSet<PartidasPreFactura> PartidasPreFacturas { get; set; }

    public virtual DbSet<PermisosRol> PermisosRols { get; set; }

    public virtual DbSet<PermisosUsuario> PermisosUsuarios { get; set; }

    public virtual DbSet<PreFactura> PreFacturas { get; set; }

    public virtual DbSet<Receptore> Receptores { get; set; }

    public virtual DbSet<RegimenFiscal> RegimenFiscals { get; set; }

    public virtual DbSet<ResultadosExtraccion> ResultadosExtraccions { get; set; }

    public virtual DbSet<ResultadosOcr> ResultadosOcrs { get; set; }

    public virtual DbSet<RevisionesHumana> RevisionesHumanas { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Sesione> Sesiones { get; set; }

    public virtual DbSet<SolicitudesAutorizacion> SolicitudesAutorizacions { get; set; }

    public virtual DbSet<TiposRelacionCfdi> TiposRelacionCfdis { get; set; }

    public virtual DbSet<UnidadesSat> UnidadesSats { get; set; }

    public virtual DbSet<UsoCfdi> UsoCfdis { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<ViasAdministracion> ViasAdministracions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Modern_Spanish_CI_AI");

        modelBuilder.Entity<Aseguradora>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Asegurad__3214EC0719AE7BC6");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaAlta).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.IdAseguradoraPadreNavigation).WithMany(p => p.InverseIdAseguradoraPadreNavigation).HasConstraintName("FK__Asegurado__IdAse__4BAC3F29");
        });

        modelBuilder.Entity<AsignacionesRevision>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Asignaci__3214EC07D24F0A0B");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Estado).HasDefaultValue("PENDIENTE");
            entity.Property(e => e.FechaAsignacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.IdGrupoNavigation).WithMany(p => p.AsignacionesRevisions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Asignacio__IdGru__308E3499");

            entity.HasOne(d => d.IdImagenNavigation).WithMany(p => p.AsignacionesRevisions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Asignacio__IdIma__2F9A1060");

            entity.HasOne(d => d.IdUsuarioAsignadoNavigation).WithMany(p => p.AsignacionesRevisionIdUsuarioAsignadoNavigations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Asignacio__IdUsu__318258D2");

            entity.HasOne(d => d.IdUsuarioAsignoPorNavigation).WithMany(p => p.AsignacionesRevisionIdUsuarioAsignoPorNavigations).HasConstraintName("FK__Asignacio__IdUsu__345EC57D");
        });

        modelBuilder.Entity<Cfdi>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CFDI__3214EC07FCCBE354");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Estado).HasDefaultValue("VIGENTE");
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Version).HasDefaultValue("4.0");

            entity.HasOne(d => d.IdGrupoNavigation).WithMany(p => p.Cfdis)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CFDI__IdGrupo__7DCDAAA2");

            entity.HasOne(d => d.IdPreFacturaNavigation).WithMany(p => p.Cfdis)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CFDI__IdPreFactu__7CD98669");
        });

        modelBuilder.Entity<ClavesSat>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ClavesSA__3214EC076D929B36");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Tipo).HasDefaultValue("MEDICAMENTO");
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Clientes__3214EC072D7B9C72");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.FechaActualizacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaAlta).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.RegimenFiscal).WithMany(p => p.Clientes).HasConstraintName("FK__Clientes__Regime__625A9A57");
        });

        modelBuilder.Entity<ColaProcesamiento>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ColaProc__3214EC074537344B");

            entity.HasIndex(e => e.WorkerProcesando, "IX_Cola_Worker").HasFilter("([WorkerProcesando] IS NOT NULL)");

            entity.Property(e => e.EstadoCola).HasDefaultValue("PENDIENTE");
            entity.Property(e => e.FechaEncolado).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.MaxIntentos).HasDefaultValue(3);
            entity.Property(e => e.Prioridad).HasDefaultValue(5);

            entity.HasOne(d => d.IdConfiguracionOcrNavigation).WithMany(p => p.ColaProcesamientos).HasConstraintName("FK__ColaProce__IdCon__18B6AB08");

            entity.HasOne(d => d.IdImagenNavigation).WithMany(p => p.ColaProcesamientos)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ColaProce__IdIma__1209AD79");
        });

        modelBuilder.Entity<ConfiguracionesOcr>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Configur__3214EC079548ED8F");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaActualizacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaAlta).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.MaxReintentos).HasDefaultValue(3);
            entity.Property(e => e.TimeoutSegundos).HasDefaultValue(30);
        });

        modelBuilder.Entity<Emisore>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Emisores__3214EC07ED22DFAD");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaAlta).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.IdAseguradoraNavigation).WithMany(p => p.Emisores)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Emisores__IdAseg__40C49C62");

            entity.HasOne(d => d.RegimenFiscal).WithMany(p => p.Emisores)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Emisores__Regime__41B8C09B");
        });

        modelBuilder.Entity<Especialidade>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Especial__3214EC077ABF0643");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
        });

        modelBuilder.Entity<EstadosGrupo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EstadosG__3214EC0760647680");

            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
        });

        modelBuilder.Entity<EstadosImagen>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EstadosI__3214EC07C1FC1E31");

            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
        });

        modelBuilder.Entity<FormasPago>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__FormasPa__3214EC079DAD5DF9");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
        });

        modelBuilder.Entity<FormatosRecetum>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Formatos__3214EC075EDE1769");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
        });

        modelBuilder.Entity<GruposRecetum>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GruposRe__3214EC07B7E5DD73");

            entity.HasIndex(e => e.IdCliente, "IX_Grupos_Cliente").HasFilter("([IdCliente] IS NOT NULL)");

            entity.HasIndex(e => e.FolioBase, "IX_Grupos_FolioBase").HasFilter("([FolioBase] IS NOT NULL)");

            entity.HasIndex(e => e.Nur, "IX_Grupos_NUR").HasFilter("([NUR] IS NOT NULL)");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.FechaActualizacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.IdAseguradoraNavigation).WithMany(p => p.GruposReceta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GruposRec__IdAse__69FBBC1F");

            entity.HasOne(d => d.IdClienteNavigation).WithMany(p => p.GruposReceta).HasConstraintName("FK__GruposRec__IdCli__690797E6");

            entity.HasOne(d => d.IdEspecialidadNavigation).WithMany(p => p.GruposReceta).HasConstraintName("FK__GruposRec__IdEsp__6BE40491");

            entity.HasOne(d => d.IdEstadoGrupoNavigation).WithMany(p => p.GruposReceta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GruposRec__IdEst__6EC0713C");

            entity.HasOne(d => d.IdFormatoRecetaNavigation).WithMany(p => p.GruposReceta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GruposRec__IdFor__6AEFE058");

            entity.HasOne(d => d.IdUsuarioAltaNavigation).WithMany(p => p.GruposReceta).HasConstraintName("FK__GruposRec__IdUsu__6FB49575");
        });

        modelBuilder.Entity<HistorialCorreccione>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Historia__3214EC076ED6DD0E");

            entity.HasIndex(e => e.IdGrupo, "IX_AudCorr_Grupo").HasFilter("([IdGrupo] IS NOT NULL)");

            entity.HasIndex(e => e.IdImagen, "IX_AudCorr_Imagen").HasFilter("([IdImagen] IS NOT NULL)");

            entity.Property(e => e.FechaCorreccion).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.IdGrupoNavigation).WithMany(p => p.HistorialCorrecciones).HasConstraintName("FK__Historial__IdGru__1D4655FB");

            entity.HasOne(d => d.IdImagenNavigation).WithMany(p => p.HistorialCorrecciones).HasConstraintName("FK__Historial__IdIma__1C5231C2");

            entity.HasOne(d => d.IdMedicamentoNavigation).WithMany(p => p.HistorialCorrecciones).HasConstraintName("FK__Historial__IdMed__1E3A7A34");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.HistorialCorrecciones).HasConstraintName("FK__Historial__IdUsu__1F2E9E6D");
        });

        modelBuilder.Entity<HistorialEstadosGrupo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Historia__3214EC0777067842");

            entity.Property(e => e.FechaCambio).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.EstadoAnteriorNavigation).WithMany(p => p.HistorialEstadosGrupoEstadoAnteriorNavigations).HasConstraintName("FK__Historial__Estad__1699586C");

            entity.HasOne(d => d.EstadoNuevoNavigation).WithMany(p => p.HistorialEstadosGrupoEstadoNuevoNavigations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Historial__Estad__178D7CA5");

            entity.HasOne(d => d.IdGrupoNavigation).WithMany(p => p.HistorialEstadosGrupos)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Historial__IdGru__15A53433");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.HistorialEstadosGrupos).HasConstraintName("FK__Historial__IdUsu__1881A0DE");
        });

        modelBuilder.Entity<HistorialEstadosImagen>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Historia__3214EC073093B72F");

            entity.Property(e => e.FechaCambio).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.EstadoAnteriorNavigation).WithMany(p => p.HistorialEstadosImagenEstadoAnteriorNavigations).HasConstraintName("FK__Historial__Estad__0FEC5ADD");

            entity.HasOne(d => d.EstadoNuevoNavigation).WithMany(p => p.HistorialEstadosImagenEstadoNuevoNavigations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Historial__Estad__10E07F16");

            entity.HasOne(d => d.IdImagenNavigation).WithMany(p => p.HistorialEstadosImagens)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Historial__IdIma__0EF836A4");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.HistorialEstadosImagens).HasConstraintName("FK__Historial__IdUsu__11D4A34F");
        });

        modelBuilder.Entity<Imagene>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Imagenes__3214EC074DA566C6");

            entity.HasIndex(e => e.EsCapturaManual, "IX_Img_CapturaManual").HasFilter("([EsCapturaManual]=(1))");

            entity.HasIndex(e => e.FolioBase, "IX_Img_FolioBase").HasFilter("([FolioBase] IS NOT NULL)");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.FechaActualizacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaSubida).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.NumeroHoja).HasDefaultValue(1);
            entity.Property(e => e.OrigenImagen).HasDefaultValue("CAMARA");

            entity.HasOne(d => d.IdEstadoImagenNavigation).WithMany(p => p.Imagenes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Imagenes__IdEsta__7E02B4CC");

            entity.HasOne(d => d.IdGrupoNavigation).WithMany(p => p.Imagenes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Imagenes__IdGrup__76619304");

            entity.HasOne(d => d.IdSesionNavigation).WithMany(p => p.Imagenes).HasConstraintName("FK__Imagenes__IdSesi__7A3223E8");

            entity.HasOne(d => d.IdUsuarioCapturaManualNavigation).WithMany(p => p.ImageneIdUsuarioCapturaManualNavigations).HasConstraintName("FK__Imagenes__IdUsua__7D0E9093");

            entity.HasOne(d => d.IdUsuarioSubidaNavigation).WithMany(p => p.ImageneIdUsuarioSubidaNavigations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Imagenes__IdUsua__793DFFAF");
        });

        modelBuilder.Entity<LogAcceso>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LogAcces__3214EC07D33BA314");

            entity.HasIndex(e => e.IdUsuario, "IX_LogAcceso_Usuario").HasFilter("([IdUsuario] IS NOT NULL)");

            entity.Property(e => e.FechaEvento).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.LogAccesos).HasConstraintName("FK__LogAcceso__IdUsu__4B7734FF");
        });

        modelBuilder.Entity<LogProcesamiento>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LogProce__3214EC071A3F540C");

            entity.HasIndex(e => e.IdImagen, "IX_Log_Imagen").HasFilter("([IdImagen] IS NOT NULL)");

            entity.Property(e => e.FechaEvento).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Nivel).HasDefaultValue("INFO");
        });

        modelBuilder.Entity<Medicamento>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Medicame__3214EC079E8AAC48");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaAlta).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
        });

        modelBuilder.Entity<MedicamentosRecetum>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Medicame__3214EC07F473491B");

            entity.HasIndex(e => e.CodigoCie10, "IX_Med_CIE10").HasFilter("([CodigoCIE10] IS NOT NULL)");

            entity.HasIndex(e => e.IdMedicamentoCatalogo, "IX_Med_Catalogo").HasFilter("([IdMedicamentoCatalogo] IS NOT NULL)");

            entity.HasIndex(e => e.SustanciaActiva, "IX_Med_SustanciaActiva").HasFilter("([SustanciaActiva] IS NOT NULL)");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.FechaActualizacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.NumeroPrescripcion).HasDefaultValue(1);

            entity.HasOne(d => d.ClaveSat).WithMany(p => p.MedicamentosReceta).HasConstraintName("FK__Medicamen__Clave__09746778");

            entity.HasOne(d => d.ClaveUnidadSat).WithMany(p => p.MedicamentosReceta).HasConstraintName("FK__Medicamen__Clave__0A688BB1");

            entity.HasOne(d => d.IdGrupoNavigation).WithMany(p => p.MedicamentosReceta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Medicamen__IdGru__05A3D694");

            entity.HasOne(d => d.IdImagenNavigation).WithMany(p => p.MedicamentosReceta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Medicamen__IdIma__04AFB25B");

            entity.HasOne(d => d.IdMedicamentoCatalogoNavigation).WithMany(p => p.MedicamentosReceta).HasConstraintName("FK__Medicamen__IdMed__0697FACD");

            entity.HasOne(d => d.IdViaAdministracionNavigation).WithMany(p => p.MedicamentosReceta).HasConstraintName("FK__Medicamen__IdVia__0880433F");
        });

        modelBuilder.Entity<MetodosPago>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__MetodosP__3214EC07FF98708A");

            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
        });

        modelBuilder.Entity<Modulo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Modulos__3214EC079A5C553E");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
        });

        modelBuilder.Entity<Moneda>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Monedas__3214EC07ECDF74A5");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
        });

        modelBuilder.Entity<Parametro>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Parametr__3214EC076B1239C5");

            entity.Property(e => e.FechaActualizacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaAlta).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Tipo).HasDefaultValue("STRING");
        });

        modelBuilder.Entity<PartidasCfdi>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Partidas__3214EC0798EA0B5D");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.ObjetoImpuesto).HasDefaultValue("02");

            entity.HasOne(d => d.IdCfdiNavigation).WithMany(p => p.PartidasCfdis)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PartidasC__IdCFD__056ECC6A");
        });

        modelBuilder.Entity<PartidasPreFactura>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Partidas__3214EC0769FBB11D");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.ClaveProdServ).HasDefaultValue("51101500");
            entity.Property(e => e.ClaveUnidad).HasDefaultValue("H87");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.ObjetoImpuesto).HasDefaultValue("02");

            entity.HasOne(d => d.IdMedicamentoRecetaNavigation).WithMany(p => p.PartidasPreFacturas).HasConstraintName("FK__PartidasP__IdMed__6F7F8B4B");

            entity.HasOne(d => d.IdPreFacturaNavigation).WithMany(p => p.PartidasPreFacturas)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PartidasP__IdPre__6E8B6712");
        });

        modelBuilder.Entity<PermisosRol>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Permisos__3214EC07B973003B");

            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.IdModuloNavigation).WithMany(p => p.PermisosRols)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PermisosR__IdMod__25518C17");

            entity.HasOne(d => d.IdRolNavigation).WithMany(p => p.PermisosRols)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PermisosR__IdRol__245D67DE");
        });

        modelBuilder.Entity<PermisosUsuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Permisos__3214EC07056B76F5");

            entity.Property(e => e.FechaAlta).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.IdModuloNavigation).WithMany(p => p.PermisosUsuarios)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PermisosU__IdMod__3A4CA8FD");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.PermisosUsuarios)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PermisosU__IdUsu__395884C4");
        });

        modelBuilder.Entity<PreFactura>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PreFactu__3214EC07B58DE7DC");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Estado).HasDefaultValue("GENERADA");
            entity.Property(e => e.Exportacion).HasDefaultValue("01");
            entity.Property(e => e.FechaGeneracion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.TipoCambio).HasDefaultValue(1.0000m);
            entity.Property(e => e.TipoComprobante).HasDefaultValue("I");
            entity.Property(e => e.Version).HasDefaultValue("4.0");

            entity.HasOne(d => d.FormaPago).WithMany(p => p.PreFacturas)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PreFactur__Forma__5D60DB10");

            entity.HasOne(d => d.IdEmisorNavigation).WithMany(p => p.PreFacturas)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PreFactur__IdEmi__589C25F3");

            entity.HasOne(d => d.IdGrupoNavigation).WithMany(p => p.PreFacturas)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PreFactur__IdGru__57A801BA");

            entity.HasOne(d => d.IdReceptorNavigation).WithMany(p => p.PreFacturas)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PreFactur__IdRec__59904A2C");

            entity.HasOne(d => d.IdUsuarioAprobacionNavigation).WithMany(p => p.PreFacturas).HasConstraintName("FK__PreFactur__IdUsu__68D28DBC");

            entity.HasOne(d => d.MetodoPago).WithMany(p => p.PreFacturas)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PreFactur__Metod__5C6CB6D7");

            entity.HasOne(d => d.Moneda).WithMany(p => p.PreFacturas)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PreFactur__Moned__5E54FF49");

            entity.HasOne(d => d.UsoCfdi).WithMany(p => p.PreFacturas)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PreFactur__UsoCF__5F492382");
        });

        modelBuilder.Entity<Receptore>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Receptor__3214EC07B918A35E");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaAlta).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.IdClienteNavigation).WithMany(p => p.Receptores).HasConstraintName("FK__Receptore__IdCli__4865BE2A");

            entity.HasOne(d => d.RegimenFiscal).WithMany(p => p.Receptores)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Receptore__Regim__4959E263");
        });

        modelBuilder.Entity<RegimenFiscal>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RegimenF__3214EC073C381DC8");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
        });

        modelBuilder.Entity<ResultadosExtraccion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Resultad__3214EC070E894085");

            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaProceso).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Motor).HasDefaultValue("API_EXTERNA_OCR");

            entity.HasOne(d => d.IdConfiguracionOcrNavigation).WithMany(p => p.ResultadosExtraccions).HasConstraintName("FK__Resultado__IdCon__27F8EE98");

            entity.HasOne(d => d.IdImagenNavigation).WithMany(p => p.ResultadosExtraccions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Resultado__IdIma__2610A626");

            entity.HasOne(d => d.IdResultadoOcrNavigation).WithMany(p => p.ResultadosExtraccions).HasConstraintName("FK__Resultado__IdRes__2704CA5F");
        });

        modelBuilder.Entity<ResultadosOcr>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Resultad__3214EC0772FA38D3");

            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaProceso).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IdiomaDetectado).HasDefaultValue("spa");
            entity.Property(e => e.PaginasProcesadas).HasDefaultValue(1);

            entity.HasOne(d => d.IdConfiguracionOcrNavigation).WithMany(p => p.ResultadosOcrs).HasConstraintName("FK__Resultado__IdCon__1D7B6025");

            entity.HasOne(d => d.IdImagenNavigation).WithMany(p => p.ResultadosOcrs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Resultado__IdIma__1C873BEC");
        });

        modelBuilder.Entity<RevisionesHumana>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Revision__3214EC0762C8D4C2");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaRevision).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.IdAsignacionNavigation).WithMany(p => p.RevisionesHumanas).HasConstraintName("FK__Revisione__IdAsi__3B0BC30C");

            entity.HasOne(d => d.IdGrupoNavigation).WithMany(p => p.RevisionesHumanas)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Revisione__IdGru__3A179ED3");

            entity.HasOne(d => d.IdImagenNavigation).WithMany(p => p.RevisionesHumanas)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Revisione__IdIma__39237A9A");

            entity.HasOne(d => d.IdUsuarioRevisorNavigation).WithMany(p => p.RevisionesHumanas)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Revisione__IdUsu__3BFFE745");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Roles__3214EC07D439BAEF");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaAlta).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
        });

        modelBuilder.Entity<Sesione>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Sesiones__3214EC0767DF6C29");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Estado).HasDefaultValue("ACTIVA");
            entity.Property(e => e.FechaInicio).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaUltimaActividad).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Sesiones)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Sesiones__IdUsua__45BE5BA9");
        });

        modelBuilder.Entity<SolicitudesAutorizacion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Solicitu__3214EC07FB734954");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Estado).HasDefaultValue("PENDIENTE");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaSolicitud).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.IdGrupoNavigation).WithMany(p => p.SolicitudesAutorizacions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Solicitud__IdGru__5006DFF2");

            entity.HasOne(d => d.IdUsuarioSolicitaNavigation).WithMany(p => p.SolicitudesAutorizacions).HasConstraintName("FK__Solicitud__IdUsu__52E34C9D");
        });

        modelBuilder.Entity<TiposRelacionCfdi>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TiposRel__3214EC078DE24EE4");

            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
        });

        modelBuilder.Entity<UnidadesSat>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Unidades__3214EC074DF2133F");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
        });

        modelBuilder.Entity<UsoCfdi>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UsoCFDI__3214EC07F0B61C44");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Usuarios__3214EC07AD48FAFE");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaActualizacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaAlta).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.IdAseguradoraAsignadaNavigation).WithMany(p => p.Usuarios).HasConstraintName("FK__Usuarios__IdAseg__2FCF1A8A");

            entity.HasOne(d => d.IdRolNavigation).WithMany(p => p.Usuarios)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Usuarios__IdRol__2EDAF651");
        });

        modelBuilder.Entity<ViasAdministracion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ViasAdmi__3214EC0768AE4129");

            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
