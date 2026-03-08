using RecetasOCR.Application;
using RecetasOCR.Infrastructure.BackgroundServices;
using RecetasOCR.Infrastructure.Extensions;
using Serilog;

// ── Bootstrap logger (captura errores durante el arranque) ──────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Iniciando RecetasOCR.Worker");

    var builder = Host.CreateApplicationBuilder(args);

    // ── 1. Configuración: appsettings.json + variables de entorno ────────────
    builder.Configuration
        .AddJsonFile("appsettings.json",            optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json",
                     optional: true,  reloadOnChange: true)
        .AddEnvironmentVariables();

    // ── 2. Serilog idéntico al API (lee de appsettings Serilog section) ──────
    builder.Services.AddSerilog((_, cfg) =>
        cfg.ReadFrom.Configuration(builder.Configuration)
           .Enrich.FromLogContext()
           .Enrich.WithEnvironmentName()
           .Enrich.WithMachineName());

    // ── 3. Application + Infrastructure (EF Core, Blob, OCR, JWT, Parámetros) ─
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // ── 4. Worker de OCR ─────────────────────────────────────────────────────
    // El intervalo de polling se lee de IParametrosService[OCR_WORKER_POLLING_SEG]
    // dentro de cada ciclo del worker (permite cambio en caliente desde la BD).
    builder.Services.AddHostedService<OcrWorkerService>();

    await builder.Build().RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "RecetasOCR.Worker terminó de forma inesperada");
}
finally
{
    Log.CloseAndFlush();
}
