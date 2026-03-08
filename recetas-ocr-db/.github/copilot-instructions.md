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

# Copilot Instructions — recetas-ocr-db

## Perfil esperado
Desarrollador DBA / Backend Senior con dominio de SQL Server 2019+, Azure SQL,
patrones de migración incremental y diseño de BD para sistemas transaccionales de salud.

## Stack
- SQL Server 2019+ / Azure SQL Database
- Collation: `Modern_Spanish_CI_AI`
- Migraciones: scripts SQL versionados (sin ORMs para migraciones de BD)
- Control de versiones: cada script lleva prefijo `V{número}__descripcion.sql`

## Estructura del repositorio

```
recetas-ocr-db/
├── migrations/
│   ├── V001__baseline_v5.2.sql        ← script base completo (RecetasOCR_v5.2.sql)
│   ├── V002__descripcion_cambio.sql
│   └── ...
├── seeds/
│   ├── 01_roles.sql
│   ├── 02_modulos_permisos.sql
│   ├── 03_catalogos_sat.sql
│   ├── 04_aseguradoras.sql
│   └── 05_medicamentos.sql
├── indexes/
│   └── all_indexes.sql
├── docs/
│   └── diagrama_er.md
└── README.md
```

## Cómo importar / ejecutar

```bash
# 1. Clonar repo
git clone https://github.com/org/recetas-ocr-db.git

# 2. Ejecutar baseline contra SQL Server local o Azure SQL
sqlcmd -S localhost -U sa -P TuPassword -i migrations/V001__baseline_v5.2.sql

# 3. Ejecutar seeds en orden
sqlcmd -S localhost -U sa -P TuPassword -d RecetasOCR -i seeds/01_roles.sql
sqlcmd -S localhost -U sa -P TuPassword -d RecetasOCR -i seeds/02_modulos_permisos.sql
# ... continuar en orden numérico

# 4. Para Azure SQL (reemplazar cadena de conexión)
sqlcmd -S tcp:servidor.database.windows.net,1433 \
       -U adminuser -P TuPassword \
       -d RecetasOCR \
       --authentication-method ActiveDirectoryPassword \
       -i migrations/V001__baseline_v5.2.sql
```

## Reglas para escribir scripts SQL en este proyecto

### Estilo y formato
- Siempre usar esquema explícito: `seg.Usuarios`, nunca solo `Usuarios`.
- Toda tabla nueva debe incluir al final:
  ```sql
  ModificadoPor       NVARCHAR(200),
  FechaModificacion   DATETIME2   NOT NULL DEFAULT GETUTCDATE()
  ```
- Las tablas de auditoría (`aud.*`, `seg.LogAcceso`) son la excepción: son append-only, no llevan esos campos.
- Usar `UNIQUEIDENTIFIER DEFAULT NEWSEQUENTIALID()` para PKs de tablas transaccionales (GruposReceta, Imagenes, etc.).
- Usar `INT IDENTITY(1,1)` para PKs de catálogos (`cat.*`, `seg.Roles`, `seg.Modulos`).
- Usar `BIGINT IDENTITY(1,1)` para tablas de alto volumen append-only (`ocr.ColaProcesamiento`, `aud.LogProcesamiento`).

### Migraciones
- Cada migration es idempotente: usar `IF NOT EXISTS` antes de `CREATE TABLE`, `ALTER TABLE`.
- Nunca hacer `DROP TABLE` en migraciones de producción — usar soft delete o renombrar.
- Toda migration nueva empieza con:
  ```sql
  -- Migration : V{N}__descripcion
  -- Autor     : {nombre}
  -- Fecha     : {fecha}
  -- Descripción: {qué hace y por qué}
  USE RecetasOCR;
  GO
  ```
- Los índices que acompañan a una migración van en el mismo archivo, al final.

### Índices
- Siempre nombrar índices: `IX_{Tabla}_{Columnas}`.
- El índice de polling del worker es crítico, no modificar sin análisis:
  ```sql
  -- IX_Cola_Polling sobre (EstadoCola, Bloqueado, Prioridad, FechaEncolado)
  ```
- Para columnas nullable usar índices filtrados: `WHERE ColumnaX IS NOT NULL`.
- Revisar plan de ejecución antes de agregar índices en tablas con >100k filas.

### Prohibiciones estrictas
- ❌ Sin Stored Procedures.
- ❌ Sin Vistas.
- ❌ Sin Triggers.
- ❌ Sin cursores.
- ❌ Sin lógica de negocio en la BD — solo constraints de integridad.
- ❌ No guardar contraseñas en texto plano (`PasswordHash` siempre bcrypt).
- ❌ No guardar API Keys en texto plano (`ApiKeyEncriptada` siempre encriptada en aplicación).

### Constraints importantes a respetar
```sql
-- Aseguradoras: máximo 2 niveles (hijo no puede ser padre de otro)
CONSTRAINT CK_Aseguradoras_NivelMax CHECK (IdAseguradoraPadre IS NULL OR IdAseguradoraPadre <> Id)

-- Permisos únicos por rol+módulo
CONSTRAINT UQ_PermisoRolModulo UNIQUE (IdRol, IdModulo)

-- Permisos únicos por usuario+módulo
CONSTRAINT UQ_PermisoUsuarioModulo UNIQUE (IdUsuario, IdModulo)

-- UUID de CFDI siempre único
UUID NVARCHAR(36) NOT NULL UNIQUE
```

## Prompts optimizados para Copilot en este repo

### Crear migración nueva
```
Crea el script de migración V{N}__{descripcion}.sql para agregar la tabla {nombre}
al esquema {esquema} de la BD RecetasOCR. La tabla debe:
- Seguir las convenciones de este proyecto (PK UNIQUEIDENTIFIER/INT según aplique)
- Incluir ModificadoPor y FechaModificacion al final
- Ser idempotente con IF NOT EXISTS
- Incluir los índices necesarios al final del mismo archivo
Campos requeridos: {lista de campos con tipo y restricciones}
```

### Agregar índice
```
Crea un índice optimizado para la siguiente consulta frecuente en {esquema.Tabla}:
{describe el WHERE y ORDER BY de la consulta}
Sigue la convención de nombres IX_{Tabla}_{Columnas} y usa índice filtrado si hay NULLs.
```

### Agregar columna a tabla existente
```
Crea la migración para agregar la columna {nombre} de tipo {tipo} a {esquema.Tabla}.
Debe ser idempotente. Si la columna admite NULL, no es necesario DEFAULT.
Si es NOT NULL, proporcionar DEFAULT para no romper filas existentes.
Actualizar también el comentario de la tabla en el script base si aplica.
```
