using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Application.DTOs;
using RecetasOCR.Infrastructure.BackgroundServices;
using RecetasOCR.Infrastructure.Persistence;
using RecetasOCR.Infrastructure.Persistence.Entities;
using Xunit;

namespace RecetasOCR.Infrastructure.Tests.BackgroundServices;

// ─────────────────────────────────────────────────────────────────────────────
// Testable subclass: overrides the raw-SQL optimistic-lock call
// (InMemory provider does not support ExecuteSqlAsync) and exposes
// the internal ProcesarSiguienteAsync for direct test invocation.
// ─────────────────────────────────────────────────────────────────────────────
internal sealed class TestableOcrWorkerService : OcrWorkerService
{
    private readonly int _lockResult;

    public TestableOcrWorkerService(
        IServiceScopeFactory          scopeFactory,
        ILogger<OcrWorkerService>     logger,
        int                           lockResult = 1)
        : base(scopeFactory, logger)
    {
        _lockResult = lockResult;
    }

    /// <summary>
    /// lockResult = 1  → this worker acquired the lock (happy path).
    /// lockResult = 0  → another worker already holds the lock (skip processing).
    /// </summary>
    protected override Task<int> TryAcquireLockAsync(
        RecetasOcrDbContext db, long itemId, CancellationToken ct)
        => Task.FromResult(_lockResult);

    public Task InvokeProcesarAsync(CancellationToken ct = default)
        => ProcesarSiguienteAsync(ct);
}

// ─────────────────────────────────────────────────────────────────────────────
// Test class
// ─────────────────────────────────────────────────────────────────────────────
public class OcrWorkerServiceTests
{
    // ── Well-known test constants ─────────────────────────────────────────────
    private const string BlobRawUrl      = "https://storage.test/raw/receta.jpg";
    private const string BlobOcrUrl      = "https://storage.test/ocr/receta.jpg";
    private const string BlobIlegibleUrl = "https://storage.test/ilegible/receta.jpg";
    private const string NombreArchivo   = "receta.jpg";

    // Estado IDs mirroring seeds below
    private const int EstadoEnColaId             = 1;
    private const int EstadoAprobadoId           = 2;
    private const int EstadoBajaConfianzaId      = 3;
    private const int EstadoIlegibleId           = 4;
    private const int EstadoGrupoEnProcesoId     = 1;
    private const int EstadoGrupoCapturaManualId = 2;

    // ── Infrastructure helpers ────────────────────────────────────────────────

    /// <summary>Creates an isolated InMemory DbContext (unique name per call).</summary>
    private static RecetasOcrDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<RecetasOcrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new RecetasOcrDbContext(opts);
    }

    /// <summary>
    /// Seeds catalog tables, one grupo, one imagen, and one PENDIENTE queue item.
    /// Returns (grupoId, imagenId) for assertions.
    /// </summary>
    private static (Guid grupoId, Guid imagenId) SeedQueue(
        RecetasOcrDbContext db,
        int  intentos  = 0,
        bool bloqueado = false)
    {
        var grupoId  = Guid.NewGuid();
        var imagenId = Guid.NewGuid();
        var now      = DateTime.UtcNow;

        db.EstadosImagens.AddRange(
            new EstadosImagen { Id = EstadoEnColaId,          Clave = "EN_COLA",            Descripcion = "En cola"          },
            new EstadosImagen { Id = EstadoAprobadoId,        Clave = "OCR_APROBADO",       Descripcion = "OCR aprobado"     },
            new EstadosImagen { Id = EstadoBajaConfianzaId,   Clave = "OCR_BAJA_CONFIANZA", Descripcion = "Baja confianza"   },
            new EstadosImagen { Id = EstadoIlegibleId,        Clave = "ILEGIBLE",           Descripcion = "Ilegible"         }
        );
        db.EstadosGrupos.AddRange(
            new EstadosGrupo { Id = EstadoGrupoEnProcesoId,     Clave = "EN_PROCESO",              Descripcion = "En proceso",    Orden = 1, EsFinal = false },
            new EstadosGrupo { Id = EstadoGrupoCapturaManualId, Clave = "REQUIERE_CAPTURA_MANUAL", Descripcion = "Captura manual", Orden = 2, EsFinal = false }
        );
        db.GruposReceta.Add(new GruposRecetum
        {
            Id                 = grupoId,
            IdEstadoGrupo      = EstadoGrupoEnProcesoId,
            IdAseguradora      = 1,
            IdFormatoReceta    = 1,
            FechaCreacion      = now,
            FechaActualizacion = now,
            FechaModificacion  = now,
        });
        db.Imagenes.Add(new Imagene
        {
            Id                 = imagenId,
            IdGrupo            = grupoId,
            NumeroHoja         = 1,
            UrlBlobRaw         = BlobRawUrl,
            NombreArchivo      = NombreArchivo,
            OrigenImagen       = "CAMARA",
            FechaSubida        = now,
            FechaActualizacion = now,
            IdEstadoImagen     = EstadoEnColaId,
            IdUsuarioSubida    = Guid.NewGuid(),
            EsCapturaManual    = false,
        });
        db.ColaProcesamientos.Add(new ColaProcesamiento
        {
            IdImagen          = imagenId,
            UrlBlobRaw        = BlobRawUrl,
            Prioridad         = 5,
            Intentos          = intentos,
            MaxIntentos       = 3,
            FechaEncolado     = now,
            FechaModificacion = now,
            EstadoCola        = "PENDIENTE",
            Bloqueado         = bloqueado,
        });
        db.SaveChanges();

        return (grupoId, imagenId);
    }

    // ── Mock factories ────────────────────────────────────────────────────────

    private static (Mock<IOcrApiService>, Mock<IBlobStorageService>, Mock<IParametrosService>) DefaultMocks()
    {
        var ocr   = new Mock<IOcrApiService>();
        var blob  = new Mock<IBlobStorageService>();
        var param = new Mock<IParametrosService>();

        // Blob download returns an empty readable stream
        blob.Setup(b => b.DescargarAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream());

        // Params fall back to their supplied default unless overridden per test
        param.Setup(p => p.ObtenerIntAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((string _, int def, CancellationToken _) => def);

        return (ocr, blob, param);
    }

    private static OcrResultadoDto OcrSuccess(bool esLegible, bool esConfianzaBaja, decimal confianza = 90m)
        => new OcrResultadoDto
        {
            Exitoso = true,
            EsLegible = esLegible,
            EsConfianzaBaja = esConfianzaBaja,
            ConfianzaPromedio = confianza,
            Notas = esLegible ? null : "Imagen borrosa"
        };

    private static OcrResultadoDto OcrFailure(string msg = "Timeout")
        => new OcrResultadoDto
        {
            Exitoso = false,
            EsLegible = false,
            EsConfianzaBaja = true,
            ConfianzaPromedio = 0m,
            ErrorMensaje = msg
        };

    private TestableOcrWorkerService BuildWorker(
        RecetasOcrDbContext       db,
        Mock<IOcrApiService>      ocrMock,
        Mock<IBlobStorageService> blobMock,
        Mock<IParametrosService>  paramMock,
        int                       lockResult = 1)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddSingleton(ocrMock.Object);
        services.AddSingleton(blobMock.Object);
        services.AddSingleton(paramMock.Object);

        var sp           = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
        return new TestableOcrWorkerService(scopeFactory, NullLogger<OcrWorkerService>.Instance, lockResult);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CASO 1 — Cola vacía → no llama a IOcrApiService
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task ColaVacia_NoLlamaIOcrApiService()
    {
        using var db = CreateDb();
        // No queue items seeded

        var (ocrMock, blobMock, paramMock) = DefaultMocks();
        var worker = BuildWorker(db, ocrMock, blobMock, paramMock);

        await worker.InvokeProcesarAsync();

        ocrMock.Verify(
            o => o.ProcesarImagenAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CASO 2 — Imagen legible, buena confianza → OCR_APROBADO + blob en recetas-ocr
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task ImagenLegible_BuenaConfianza_EstadoOcrAprobado_BlobOcrSubido()
    {
        using var db = CreateDb();
        var (_, imagenId) = SeedQueue(db);

        var (ocrMock, blobMock, paramMock) = DefaultMocks();
        ocrMock.Setup(o => o.ProcesarImagenAsync(BlobRawUrl, imagenId, It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(OcrSuccess(esLegible: true, esConfianzaBaja: false, confianza: 95m));
        blobMock.Setup(b => b.SubirOcrAsync(It.IsAny<Stream>(), NombreArchivo, It.IsAny<CancellationToken>()))
                .ReturnsAsync(BlobOcrUrl);

        var worker = BuildWorker(db, ocrMock, blobMock, paramMock);
        await worker.InvokeProcesarAsync();

        var imagen = db.Imagenes.Single(i => i.Id == imagenId);
        imagen.IdEstadoImagen.Should().Be(EstadoAprobadoId);
        imagen.UrlBlobOcr.Should().Be(BlobOcrUrl);
        imagen.EsLegible.Should().BeTrue();
        imagen.ScoreLegibilidad.Should().Be(95m);

        var item = db.ColaProcesamientos.Single(c => c.IdImagen == imagenId);
        item.EstadoCola.Should().Be("COMPLETADO");
        item.Bloqueado.Should().BeFalse();

        blobMock.Verify(b => b.SubirOcrAsync(      It.IsAny<Stream>(), NombreArchivo, It.IsAny<CancellationToken>()), Times.Once);
        blobMock.Verify(b => b.SubirIlegibleAsync( It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CASO 3 — Imagen legible, baja confianza → OCR_BAJA_CONFIANZA + blob en recetas-ocr
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task ImagenLegible_BajaConfianza_EstadoOcrBajaConfianza_BlobOcrSubido()
    {
        using var db = CreateDb();
        var (_, imagenId) = SeedQueue(db);

        var (ocrMock, blobMock, paramMock) = DefaultMocks();
        ocrMock.Setup(o => o.ProcesarImagenAsync(BlobRawUrl, imagenId, It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(OcrSuccess(esLegible: true, esConfianzaBaja: true, confianza: 55m));
        blobMock.Setup(b => b.SubirOcrAsync(It.IsAny<Stream>(), NombreArchivo, It.IsAny<CancellationToken>()))
                .ReturnsAsync(BlobOcrUrl);

        var worker = BuildWorker(db, ocrMock, blobMock, paramMock);
        await worker.InvokeProcesarAsync();

        var imagen = db.Imagenes.Single(i => i.Id == imagenId);
        imagen.IdEstadoImagen.Should().Be(EstadoBajaConfianzaId);
        imagen.UrlBlobOcr.Should().Be(BlobOcrUrl);
        imagen.EsLegible.Should().BeTrue();
        imagen.ScoreLegibilidad.Should().Be(55m);

        var item = db.ColaProcesamientos.Single(c => c.IdImagen == imagenId);
        item.EstadoCola.Should().Be("COMPLETADO");

        blobMock.Verify(b => b.SubirOcrAsync(     It.IsAny<Stream>(), NombreArchivo, It.IsAny<CancellationToken>()), Times.Once);
        blobMock.Verify(b => b.SubirIlegibleAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CASO 4 — Imagen ilegible → ILEGIBLE + blob en recetas-ilegibles
    //          Verificar que blob raw NO fue eliminado (IBlobStorageService
    //          no tiene EliminarAsync — es una garantía arquitectónica) y que
    //          SubirOcrAsync NUNCA se llamó.
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task ImagenIlegible_EstadoIlegible_BlobSubidoAIlegibles_RawNoEliminado()
    {
        using var db = CreateDb();
        var (grupoId, imagenId) = SeedQueue(db);

        var (ocrMock, blobMock, paramMock) = DefaultMocks();
        ocrMock.Setup(o => o.ProcesarImagenAsync(BlobRawUrl, imagenId, It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(OcrSuccess(esLegible: false, esConfianzaBaja: true, confianza: 10m));
        blobMock.Setup(b => b.SubirIlegibleAsync(It.IsAny<Stream>(), NombreArchivo, It.IsAny<CancellationToken>()))
                .ReturnsAsync(BlobIlegibleUrl);

        var worker = BuildWorker(db, ocrMock, blobMock, paramMock);
        await worker.InvokeProcesarAsync();

        var imagen = db.Imagenes.Single(i => i.Id == imagenId);
        imagen.IdEstadoImagen.Should().Be(EstadoIlegibleId);
        imagen.UrlBlobIlegible.Should().Be(BlobIlegibleUrl);
        imagen.EsLegible.Should().BeFalse();

        // Grupo debe requerir captura manual
        var grupo = db.GruposReceta.Single(g => g.Id == grupoId);
        grupo.IdEstadoGrupo.Should().Be(EstadoGrupoCapturaManualId);

        var item = db.ColaProcesamientos.Single(c => c.IdImagen == imagenId);
        item.EstadoCola.Should().Be("COMPLETADO");

        // El blob raw NO fue "eliminado" — IBlobStorageService no expone EliminarAsync.
        // Verificar además que SubirOcrAsync jamás se invocó (solo ilegibles va al contenedor de ilegibles).
        blobMock.Verify(b => b.SubirOcrAsync(     It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        blobMock.Verify(b => b.SubirIlegibleAsync(It.IsAny<Stream>(), NombreArchivo,      It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CASO 5 — API falla 1 vez → re-encola, NO marca FALLIDO
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task ApiFalla_PrimerIntento_ReencolaYNoMarcaFallido()
    {
        using var db = CreateDb();
        var (_, imagenId) = SeedQueue(db, intentos: 0);

        var (ocrMock, blobMock, paramMock) = DefaultMocks();
        ocrMock.Setup(o => o.ProcesarImagenAsync(It.IsAny<string>(), imagenId, It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(OcrFailure("Timeout"));
        paramMock.Setup(p => p.ObtenerIntAsync("OCR_MAX_INTENTOS", It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(3);

        var worker = BuildWorker(db, ocrMock, blobMock, paramMock);
        await worker.InvokeProcesarAsync();

        var item = db.ColaProcesamientos.Single(c => c.IdImagen == imagenId);
        item.EstadoCola.Should().Be("PENDIENTE", "debe re-encolarse, no marcarse FALLIDO");
        item.Intentos.Should().Be(1);
        item.Bloqueado.Should().BeFalse();

        // Ningún blob fue subido
        blobMock.Verify(b => b.SubirOcrAsync(     It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        blobMock.Verify(b => b.SubirIlegibleAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CASO 6 — API falla MaxIntentos → marca FALLIDO + imagen ILEGIBLE
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task ApiFalla_MaxIntentos_MarcaFallido_ImagenIlegible()
    {
        using var db = CreateDb();
        // intentos=2, maxIntentos=3 → after Intentos++ == 3 >= 3 → FALLIDO
        var (_, imagenId) = SeedQueue(db, intentos: 2);

        var (ocrMock, blobMock, paramMock) = DefaultMocks();
        ocrMock.Setup(o => o.ProcesarImagenAsync(It.IsAny<string>(), imagenId, It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(OcrFailure("Service unavailable"));
        paramMock.Setup(p => p.ObtenerIntAsync("OCR_MAX_INTENTOS", It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(3);

        var worker = BuildWorker(db, ocrMock, blobMock, paramMock);
        await worker.InvokeProcesarAsync();

        var item = db.ColaProcesamientos.Single(c => c.IdImagen == imagenId);
        item.EstadoCola.Should().Be("FALLIDO");
        item.Intentos.Should().Be(3);
        item.Bloqueado.Should().BeFalse();

        var imagen = db.Imagenes.Single(i => i.Id == imagenId);
        imagen.IdEstadoImagen.Should().Be(EstadoIlegibleId,
            "imagen debe marcarse ILEGIBLE al agotar todos los reintentos");

        // Audit trail del fallo
        db.LogProcesamientos
          .Where(l => l.IdImagen == imagenId && l.Paso == "OCR_FIN")
          .Should().HaveCount(1);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CASO 7 — Bloqueo optimista: segundo worker (lock falla) no procesa
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task BloqueoOptimista_SegundoWorker_SkipeaCuandoLockFalla()
    {
        using var db = CreateDb();
        var (_, imagenId) = SeedQueue(db);

        var (ocrMock, blobMock, paramMock) = DefaultMocks();

        // workerB simulates the case where the SQL UPDATE returns 0 rows
        // (the lock was already acquired by another worker instance).
        var workerB = BuildWorker(db, ocrMock, blobMock, paramMock, lockResult: 0);
        await workerB.InvokeProcesarAsync();

        ocrMock.Verify(
            o => o.ProcesarImagenAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "el worker B no debe procesar la imagen si no pudo adquirir el lock optimista");

        // Item must remain untouched — still PENDIENTE, still unlocked
        var item = db.ColaProcesamientos.Single(c => c.IdImagen == imagenId);
        item.EstadoCola.Should().Be("PENDIENTE");
        item.Bloqueado.Should().BeFalse();
    }
}
