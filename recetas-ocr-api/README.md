# recetas-ocr-api — .NET 9 Web API + Worker

API REST y Worker de procesamiento OCR para el sistema de recetas médicas.

## Requisitos
- .NET 9 SDK
- SQL Server 2019+ (BD creada con recetas-ocr-db)
- Azure Storage Account (o Azurite para desarrollo local)

## Inicio rápido

```bash
# 1. Restaurar dependencias
dotnet restore

# 2. Configurar conexión (editar o usar user-secrets)
dotnet user-secrets set "ConnectionStrings:RecetasOCR" \
  "Server=localhost;Database=RecetasOCR;User Id=sa;Password=...;TrustServerCertificate=true" \
  --project src/RecetasOCR.API

# 3. Levantar la API
dotnet run --project src/RecetasOCR.API

# 4. Levantar el Worker (terminal separada)
dotnet run --project src/RecetasOCR.Worker

# 5. Swagger en desarrollo
# http://localhost:64094/swagger
```

## Scaffold EF Core desde BD existente

```bash
cd src/RecetasOCR.Infrastructure
dotnet ef dbcontext scaffold \
  "Server=localhost;Database=RecetasOCR;User Id=sa;Password=...;TrustServerCertificate=true" \
  Microsoft.EntityFrameworkCore.SqlServer \
  --output-dir Persistence/Entities \
  --context RecetasOcrDbContext \
  --context-dir Persistence \
  --schemas seg cat cfg rec med ocr rev fac aud \
  --no-onconfiguring \
  --data-annotations \
  --force
```

## Estructura
```
src/
  RecetasOCR.Domain/          → Entidades, Enums, Excepciones
  RecetasOCR.Application/     → CQRS (Commands/Queries/Handlers), DTOs, Interfaces
  RecetasOCR.Infrastructure/  → EF Core, BlobStorage, OcrApiService, Worker
  RecetasOCR.API/             → Controllers, Middlewares, Program.cs
  RecetasOCR.Worker/          → BackgroundService independiente
tests/
  RecetasOCR.Application.Tests/
```

## Reglas clave
- Sin lógica en Controllers — solo delegar a MediatR
- Sin librerías OCR locales — IOcrApiService llama a API externa
- ModificadoPor en toda escritura (viene de ICurrentUserService)
- Toda imagen a blob recetas-raw PRIMERO, siempre
