using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RecetasOCR.Infrastructure.Persistence;

/// <summary>
/// Factory para diseño en tiempo de desarrollo (dotnet ef migrations add, etc.).
/// Prioridad de configuración:
///   1. Variable de entorno DB_CONNECTION_STRING
///   2. appsettings.Development.json del proyecto API (ConnectionStrings:RecetasOCR)
/// No requiere Microsoft.Extensions.Configuration.FileExtensions.
/// </summary>
public class RecetasOcrDbContextFactory : IDesignTimeDbContextFactory<RecetasOcrDbContext>
{
    public RecetasOcrDbContext CreateDbContext(string[] args)
    {
        // 1. Variable de entorno tiene prioridad (CI/CD, contenedores)
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

        // 2. Fallback a appsettings.Development.json del proyecto API
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // La factory corre desde el directorio del proyecto Infrastructure;
            // appsettings.Development.json está en el proyecto API adyacente.
            var appsettingsPath = Path.GetFullPath(
                Path.Combine(Directory.GetCurrentDirectory(),
                    "..", "RecetasOCR.API", "appsettings.Development.json"));

            if (!File.Exists(appsettingsPath))
                throw new FileNotFoundException(
                    $"No se encontró appsettings.Development.json en: {appsettingsPath}. " +
                    "También puedes establecer la variable de entorno DB_CONNECTION_STRING.");

            using var doc = JsonDocument.Parse(File.ReadAllText(appsettingsPath));
            connectionString = doc.RootElement
                .GetProperty("ConnectionStrings")
                .GetProperty("RecetasOCR")
                .GetString()
                ?? throw new InvalidOperationException(
                    "ConnectionStrings:RecetasOCR no encontrado en appsettings.Development.json.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<RecetasOcrDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new RecetasOcrDbContext(optionsBuilder.Options);
    }
}
