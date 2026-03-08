# Copilot Instructions — Sistema OCR Recetas Médicas

## Contexto del proyecto

Sistema para digitalizar recetas médicas mexicanas de aseguradoras institucionales
(Banxico, Bancomext, LYFC, Banobras, Banorte, BBVA, Banamex, Nafin).
El usuario toma fotos desde la cámara del dispositivo o las importa desde la galería.
Una receta puede ser 1 o N imágenes (hojas). El OCR se delega a una API externa
(Google Vision / Azure Document Intelligence / AWS Textract / API propia).
El sistema genera CFDIs 4.0 timbrados a través de un PAC.

## Repositorios del proyecto

| Repo | Stack | Puerto local |
|---|---|---|
| `recetas-ocr-db` | SQL Server 2019 / Azure SQL | 1433 |
| `recetas-ocr-api` | .NET 9 Web API + Worker | 5000 |
| `recetas-ocr-web` | React 18 + TypeScript + Vite | 5173 |

## Reglas de negocio críticas — leer antes de generar cualquier código

### Imágenes
- Toda imagen se sube SIEMPRE al blob `recetas-raw` sin excepción — `UrlBlobRaw` es NOT NULL.
- Si la API OCR la evalúa como legible → copia adicional a `recetas-ocr` (`UrlBlobOCR`).
- Si es ilegible → copia adicional a `recetas-ilegibles` (`UrlBlobIlegible`).
- Las ilegibles NUNCA se eliminan, son evidencia permanente.
- El origen de la imagen se registra: `CAMARA | GALERIA | API | ESCANER`.
- Metadatos EXIF se capturan cuando vienen (fecha real de toma, GPS, modelo de dispositivo).

### Agrupación de recetas
- Una consulta médica = 1 `GrupoReceta` con 1..N imágenes (`rec.Imagenes`).
- Agrupación CON folio: por `FolioBase` (ej: GE-15226548875).
- Agrupación SIN folio: por `IdCliente + IdAseguradora + FechaConsulta`.
- Cada imagen = generalmente 1 medicamento (excepcionalmente 2 en Bancomext manuscrito).

### Aseguradoras
- Jerarquía de 2 niveles: aseguradora padre → 0..N sub-aseguradoras (hijos).
- `IdAseguradoraPadre IS NULL` = raíz. `IdAseguradoraPadre NOT NULL` = sub-aseguradora.
- No se permiten más de 2 niveles (validar en aplicación y en BD).
- MediProses, Vita y Bupa son OPERADORES INTERMEDIOS, no aseguradoras reales.

### OCR
- El procesamiento OCR lo hace una API externa configurada en `cfg.ConfiguracionesOCR`.
- El Worker (.NET 9 BackgroundService) hace polling a `ocr.ColaProcesamiento` cada 3s.
- Bloqueo optimista: `Bloqueado=1 + WorkerProcesando` para evitar doble procesamiento.
- Umbral de confianza mínimo en `cfg.Parametros` clave `OCR_CONFIANZA_MINIMA` (default 70).
- Resultado por debajo del umbral → estado `OCR_BAJA_CONFIANZA`, no se rechaza automáticamente.
- Toda llamada a la API se registra en `ocr.ResultadosOCR` (petición + respuesta + costo + JSON completo).
- Sin Service Bus, sin Tesseract, sin librerías OCR locales.

### Revisión humana
- TODA receta requiere revisión humana antes de facturar — sin excepción.
- Si una imagen es ilegible, el revisor hace captura manual (`EsCapturaManual=1`).
- Toda corrección manual queda en `aud.HistorialCorrecciones` (tabla, campo, valor anterior, valor nuevo, usuario, fecha).

### Facturación CFDI 4.0
- Solo grupos en estado `REVISADO_COMPLETO` avanzan a facturación.
- Medicamentos: clave SAT 51101500, IVA 0% en México.
- `fac.PartidasCFDI` es INMUTABLE — nunca se modifica después del timbrado.
- El UUID del CFDI es el identificador fiscal definitivo.
- XML y PDF del CFDI se guardan en Blob Storage (`cfdi-xml` y `cfdi-pdf`).

### Auditoría
- Toda tabla operativa tiene `ModificadoPor NVARCHAR(200)` y `FechaModificacion DATETIME2`.
- Las tablas de log/historial son append-only: `seg.LogAcceso`, `aud.Historial*`, `aud.LogProcesamiento`.
- Los cambios de estado se registran en `aud.HistorialEstadosImagen` y `aud.HistorialEstadosGrupo`.

## Esquemas de la base de datos

```
seg  → seguridad: Roles, Modulos, PermisosRol, Usuarios, PermisosUsuario, Sesiones, LogAcceso
cat  → catálogos: Aseguradoras, FormatosReceta, EstadosImagen, EstadosGrupo,
                  ViasAdministracion, Especialidades, Medicamentos,
                  ClavesSAT, UnidadesSAT, RegimenFiscal, UsoCFDI,
                  MetodosPago, FormasPago, Monedas, TiposRelacionCFDI
cfg  → configuración: Parametros, ConfiguracionesOCR
rec  → recetas: Clientes, GruposReceta, Imagenes
med  → medicamentos: MedicamentosReceta
ocr  → procesamiento: ColaProcesamiento, ResultadosOCR, ResultadosExtraccion
rev  → revisión: AsignacionesRevision, RevisionesHumanas
fac  → facturación: Emisores, Receptores, SolicitudesAutorizacion,
                    PreFacturas, PartidasPreFactura, CFDI, PartidasCFDI
aud  → auditoría: HistorialEstadosImagen, HistorialEstadosGrupo,
                  HistorialCorrecciones, LogProcesamiento
```

## Roles del sistema

| Rol | Descripción |
|---|---|
| SUPERADMIN | Acceso total, configura integraciones y parámetros |
| ADMIN | Gestiona usuarios, catálogos, aseguradoras |
| OPERADOR_OCR | Sube imágenes (cámara/galería), consulta estados |
| REVISOR | Revisa y aprueba imágenes, hace captura manual |
| FACTURISTA | Genera pre-facturas y timbra CFDIs |
| AUDITOR | Solo lectura en todo el sistema |

## Contenedores Blob Storage Azure

| Contenedor | Uso |
|---|---|
| `recetas-raw` | Toda imagen recibida — SIEMPRE |
| `recetas-ocr` | Copia de imágenes legibles |
| `recetas-ilegibles` | Copia de imágenes ilegibles — evidencia permanente |
| `cfdi-xml` | Archivos XML timbrados |
| `cfdi-pdf` | PDFs de CFDIs |

## Convenciones de nombres

- **BD**: tablas en `PascalCase`, columnas en `PascalCase`, esquemas en minúsculas.
- **API .NET**: clases en `PascalCase`, métodos en `PascalCase`, variables en `camelCase`.
- **React**: componentes en `PascalCase`, hooks en `camelCase` con prefijo `use`, archivos `.tsx`.
- **Endpoints REST**: kebab-case → `/api/grupos-receta`, `/api/imagenes`, `/api/ocr/cola`.
- **Español para dominio**: nombres de clases de dominio en español (GrupoReceta, ImagenReceta).
- **Inglés para infraestructura**: repositorios, servicios, interfaces de infraestructura en inglés.

---

# Copilot Instructions — recetas-ocr-api (.NET 9)

## Perfil esperado
Desarrollador Backend Senior con dominio de .NET 9, Clean Architecture,
Entity Framework Core, JWT, Azure Blob Storage, patrones CQRS con MediatR,
y consumo de APIs externas de OCR.

## Stack completo

```
.NET 9
ASP.NET Core Web API
Entity Framework Core 9          ← acceso a datos (code-first desde BD existente)
MediatR 12                       ← CQRS (Commands + Queries)
FluentValidation                 ← validaciones
AutoMapper                       ← mapeo entidad ↔ DTO
Serilog                          ← logging estructurado
Azure.Storage.Blobs              ← Blob Storage (subida de imágenes y CFDIs)
Azure.Identity                   ← autenticación con Azure
Microsoft.AspNetCore.Authentication.JwtBearer  ← JWT
BCrypt.Net-Next                  ← hash de contraseñas
Polly                            ← reintentos y circuit breaker para API OCR externa
Refit o HttpClientFactory        ← cliente HTTP tipado para API OCR externa
Bogus                            ← datos de prueba (solo en tests)
xUnit + Moq + FluentAssertions   ← testing
```

## Estructura del repositorio

```
recetas-ocr-api/
├── src/
│   ├── RecetasOCR.API/                  ← proyecto Web API (startup, controllers, middlewares)
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs
│   │   │   ├── GruposRecetaController.cs
│   │   │   ├── ImagenesController.cs
│   │   │   ├── OcrController.cs
│   │   │   ├── RevisionController.cs
│   │   │   ├── FacturacionController.cs
│   │   │   ├── UsuariosController.cs
│   │   │   └── CatalogosController.cs
│   │   ├── Middlewares/
│   │   │   ├── ExceptionHandlerMiddleware.cs
│   │   │   └── AuditMiddleware.cs       ← inyecta ModificadoPor en cada request
│   │   ├── Extensions/
│   │   │   ├── ServiceCollectionExtensions.cs
│   │   │   └── SwaggerExtensions.cs
│   │   └── Program.cs
│   │
│   ├── RecetasOCR.Application/          ← lógica de negocio (CQRS, validaciones, DTOs)
│   │   ├── Common/
│   │   │   ├── Interfaces/
│   │   │   │   ├── IBlobStorageService.cs
│   │   │   │   ├── IOcrApiService.cs
│   │   │   │   └── ICurrentUserService.cs
│   │   │   └── Behaviors/
│   │   │       ├── ValidationBehavior.cs
│   │   │       └── LoggingBehavior.cs
│   │   ├── Features/
│   │   │   ├── Auth/
│   │   │   ├── GruposReceta/
│   │   │   ├── Imagenes/
│   │   │   ├── Ocr/
│   │   │   ├── Revision/
│   │   │   └── Facturacion/
│   │   └── DTOs/
│   │
│   ├── RecetasOCR.Domain/               ← entidades, enums, reglas de dominio
│   │   ├── Entities/
│   │   ├── Enums/
│   │   │   ├── EstadoImagen.cs
│   │   │   ├── EstadoGrupo.cs
│   │   │   ├── OrigenImagen.cs
│   │   │   └── EstadoCola.cs
│   │   └── Exceptions/
│   │
│   ├── RecetasOCR.Infrastructure/       ← EF Core, repos, blob, OCR client
│   │   ├── Persistence/
│   │   │   ├── RecetasOcrDbContext.cs
│   │   │   ├── Configurations/          ← IEntityTypeConfiguration por tabla
│   │   │   └── Repositories/
│   │   ├── Services/
│   │   │   ├── BlobStorageService.cs
│   │   │   ├── OcrApiService.cs         ← llama a la API externa con Polly
│   │   │   └── JwtService.cs
│   │   └── BackgroundServices/
│   │       └── OcrWorkerService.cs      ← polling a ocr.ColaProcesamiento cada 3s
│   │
│   └── RecetasOCR.Worker/               ← proyecto separado BackgroundService
│       └── Program.cs
│
└── tests/
    ├── RecetasOCR.Application.Tests/
    ├── RecetasOCR.Infrastructure.Tests/
    └── RecetasOCR.API.Tests/
```

## Cómo importar / instalar dependencias

```bash
# 1. Clonar repo
git clone https://github.com/org/recetas-ocr-api.git
cd recetas-ocr-api

# 2. Restaurar paquetes
dotnet restore

# 3. Paquetes del proyecto API
cd src/RecetasOCR.API
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.ApplicationInsights
dotnet add package Swashbuckle.AspNetCore

# 4. Paquetes del proyecto Application
cd ../RecetasOCR.Application
dotnet add package MediatR
dotnet add package FluentValidation.DependencyInjectionExtensions
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection

# 5. Paquetes del proyecto Infrastructure
cd ../RecetasOCR.Infrastructure
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Azure.Storage.Blobs
dotnet add package Azure.Identity
dotnet add package BCrypt.Net-Next
dotnet add package Polly.Extensions.Http
dotnet add package Refit.HttpClientFactory

# 6. Paquetes de tests
cd ../../tests/RecetasOCR.Application.Tests
dotnet add package xunit
dotnet add package Moq
dotnet add package FluentAssertions
dotnet add package Bogus
```

## Configuración (appsettings.json)

```json
{
  "ConnectionStrings": {
    "RecetasOCR": "Server=localhost;Database=RecetasOCR;User Id=sa;Password=...;TrustServerCertificate=true"
  },
  "Jwt": {
    "SecretKey": "CAMBIAR_EN_PRODUCCION_MIN_32_CHARS",
    "ExpirationMinutes": 60,
    "RefreshTokenDays": 30,
    "Issuer": "recetas-ocr-api",
    "Audience": "recetas-ocr-web"
  },
  "AzureBlobStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;...",
    "ContainerRaw": "recetas-raw",
    "ContainerOcr": "recetas-ocr",
    "ContainerIlegible": "recetas-ilegibles",
    "ContainerCfdiXml": "cfdi-xml",
    "ContainerCfdiPdf": "cfdi-pdf"
  },
  "OcrWorker": {
    "PollingIntervalSeconds": 3,
    "LockTimeoutMinutes": 30,
    "WorkerName": "WORKER-01"
  },
  "Serilog": {
    "MinimumLevel": "Information"
  }
}
```

## Patrones y convenciones

### CQRS con MediatR — estructura de un feature
```csharp
// Query
public record GetGrupoRecetaQuery(Guid Id) : IRequest<GrupoRecetaDto>;

// Command
public record SubirImagenCommand(
    Guid IdGrupo,
    IFormFile Archivo,
    string OrigenImagen,   // CAMARA | GALERIA | API | ESCANER
    string? ModeloDispositivo
) : IRequest<ImagenDto>;

// Handler siempre recibe ICurrentUserService para ModificadoPor
public class SubirImagenCommandHandler : IRequestHandler<SubirImagenCommand, ImagenDto>
{
    private readonly ICurrentUserService _currentUser;
    // ...
}
```

### Entidades de dominio — siempre con ModificadoPor
```csharp
public abstract class AuditableEntity
{
    public string? ModificadoPor { get; set; }
    public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;
}

// Todas las entidades operativas heredan de AuditableEntity
public class GrupoReceta : AuditableEntity { ... }
public class ImagenReceta : AuditableEntity { ... }
```

### AuditMiddleware — inyección automática de ModificadoPor
```csharp
// El middleware extrae el username del JWT y lo pone disponible
// para que los handlers lo usen al persistir cambios.
// ICurrentUserService.Username → se escribe en ModificadoPor de cada entidad modificada.
```

### Servicio OCR externo — IOcrApiService
```csharp
public interface IOcrApiService
{
    // Evalúa legibilidad y ejecuta OCR. Usa Polly para reintentos.
    Task<OcrResultado> ProcesarImagenAsync(string urlBlobRaw, Guid idImagen, CancellationToken ct);
}

// La implementación lee cfg.ConfiguracionesOCR para saber qué proveedor usar.
// Registra cada llamada en ocr.ResultadosOCR (petición + respuesta + costo + JSON completo).
// Si la confianza < cfg.Parametros[OCR_CONFIANZA_MINIMA] → estado OCR_BAJA_CONFIANZA.
// Nunca lanza excepción por baja confianza, solo marca el estado.
```

### OcrWorkerService — polling sin Service Bus
```csharp
// BackgroundService que cada 3s ejecuta:
// 1. SELECT TOP 1 de ocr.ColaProcesamiento WHERE EstadoCola='PENDIENTE' AND Bloqueado=0
//    ORDER BY Prioridad ASC, FechaEncolado ASC
// 2. UPDATE SET Bloqueado=1, WorkerProcesando=@WorkerName, FechaBloqueo=GETUTCDATE()
//    WHERE Id=@Id AND Bloqueado=0    ← bloqueo optimista
// 3. Si UPDATE afectó 1 fila → procesar. Si 0 filas → otro worker la tomó, saltar.
// 4. Llamar a IOcrApiService.ProcesarImagenAsync(...)
// 5. Actualizar ocr.ColaProcesamiento, rec.Imagenes y ocr.ResultadosOCR
// 6. Registrar en aud.LogProcesamiento
```

### Blob Storage — regla de los 3 contenedores
```csharp
// IBlobStorageService siempre sube a raw primero.
// Según EsLegible, sube copia adicional a ocr o ilegibles.
// Los blobs ilegibles NUNCA se eliminan.
public interface IBlobStorageService
{
    Task<string> SubirRawAsync(Stream imagen, string nombreArchivo, CancellationToken ct);
    Task<string> SubirOcrAsync(Stream imagen, string nombreArchivo, CancellationToken ct);
    Task<string> SubirIlegibleAsync(Stream imagen, string nombreArchivo, CancellationToken ct);
    Task<string> SubirCfdiXmlAsync(Stream xml, string nombreArchivo, CancellationToken ct);
    Task<string> SubirCfdiPdfAsync(Stream pdf, string nombreArchivo, CancellationToken ct);
}
```

### Autorización por rol + módulo
```csharp
// Usar policy personalizada que verifica seg.PermisosRol y seg.PermisosUsuario
// El permiso individual (PermisosUsuario) sobreescribe al del rol.
// Si Denegado=1 en PermisosUsuario → denegar aunque el rol lo permita.
[Authorize(Policy = "IMAGENES.SUBIR")]
[HttpPost("imagenes")]
public async Task<IActionResult> SubirImagen(...) { }
```

### Respuestas de API — siempre envelope
```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();
}
```

## Prohibiciones en este proyecto
- ❌ Sin lógica de negocio en Controllers — solo reciben, delegan a MediatR y responden.
- ❌ Sin acceso directo a BD desde Controllers.
- ❌ Sin librerías OCR locales (Tesseract, etc.) — el OCR siempre es via API externa.
- ❌ Sin guardar contraseñas en texto plano.
- ❌ Sin exponer `PasswordHash` ni `ApiKeyEncriptada` en ningún DTO de respuesta.
- ❌ Sin `catch (Exception)` vacíos — siempre loggear con Serilog.
- ❌ Sin `async void` — siempre `async Task`.

## Prompts optimizados para Copilot en este repo

### Crear un feature CQRS completo
```
Crea el feature completo para {nombre de la operación} siguiendo Clean Architecture.
Incluye:
- Record Command o Query en Application/Features/{Módulo}/
- Handler que use ICurrentUserService para ModificadoPor
- FluentValidation Validator
- DTO de respuesta
- Método en el Controller correspondiente con [Authorize(Policy="...")]
- Registrar en aud.LogProcesamiento al finalizar
Reglas de negocio: {describir aquí}
```

### Crear endpoint de subida de imagen
```
Crea el endpoint POST /api/imagenes para subir una imagen de receta médica.
Reglas:
- Aceptar multipart/form-data con campos: archivo, idGrupo, origenImagen (CAMARA|GALERIA|API|ESCANER)
- Validar tamaño máximo según cfg.Parametros[MAX_TAMANIO_IMAGEN_MB]
- Subir SIEMPRE a blob recetas-raw primero (IBlobStorageService.SubirRawAsync)
- Crear registro en rec.Imagenes con estado RECIBIDA
- Encolar en ocr.ColaProcesamiento con prioridad 5
- Registrar en aud.LogProcesamiento paso=COLA
- Retornar ApiResponse<ImagenDto> con la URL del blob raw y el estado
```

### Crear el OcrWorkerService
```
Crea el BackgroundService OcrWorkerService que hace polling a ocr.ColaProcesamiento.
Intervalo: cfg.Parametros[OCR_WORKER_POLLING_SEG] (default 3s).
Bloqueo optimista: UPDATE con WHERE Bloqueado=0, verificar RowsAffected=1.
Llamar a IOcrApiService.ProcesarImagenAsync con la URL del blob raw.
Actualizar rec.Imagenes.EsLegible, ScoreLegibilidad, MotivoBajaCalidad.
Si legible: subir a recetas-ocr, estado OCR_APROBADO o OCR_BAJA_CONFIANZA según umbral.
Si ilegible: subir a recetas-ilegibles, estado ILEGIBLE (nunca eliminar el raw).
Registrar resultado en ocr.ResultadosOCR y en aud.LogProcesamiento.
```
