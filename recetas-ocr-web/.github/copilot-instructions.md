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

# Copilot Instructions — recetas-ocr-web (React 18 + TypeScript)

## Perfil esperado
Desarrollador Frontend Senior con dominio de React 18, TypeScript estricto,
Vite, TanStack Query, React Hook Form, Zustand y diseño de interfaces
para flujos de trabajo operativos (dashboards, colas de revisión, formularios complejos).

## Stack completo

```
React 18 + TypeScript (strict mode)
Vite 5                               ← bundler y dev server
React Router v6                      ← navegación
TanStack Query (React Query) v5      ← server state, caché, refetch
Zustand                              ← client state (sesión, usuario actual)
React Hook Form + Zod                ← formularios y validación
Axios                                ← cliente HTTP (con interceptores JWT)
shadcn/ui + Tailwind CSS             ← componentes y estilos
Lucide React                         ← iconos
React Dropzone                       ← subida de imágenes
React Webcam                         ← captura desde cámara del dispositivo
date-fns                             ← manejo de fechas
React Hot Toast                      ← notificaciones
Vitest + React Testing Library       ← testing
```

## Estructura del repositorio

```
recetas-ocr-web/
├── public/
├── src/
│   ├── api/                         ← clientes HTTP por dominio
│   │   ├── auth.api.ts
│   │   ├── grupos-receta.api.ts
│   │   ├── imagenes.api.ts
│   │   ├── ocr.api.ts
│   │   ├── revision.api.ts
│   │   ├── facturacion.api.ts
│   │   └── catalogos.api.ts
│   ├── components/
│   │   ├── ui/                      ← componentes shadcn/ui (no editar)
│   │   ├── common/                  ← componentes reutilizables del proyecto
│   │   │   ├── PageHeader.tsx
│   │   │   ├── DataTable.tsx
│   │   │   ├── StatusBadge.tsx
│   │   │   ├── ConfirmDialog.tsx
│   │   │   └── LoadingSpinner.tsx
│   │   ├── imagenes/
│   │   │   ├── CamaraCaptura.tsx    ← React Webcam para fotos en vivo
│   │   │   ├── GaleriaImport.tsx    ← React Dropzone para importar desde galería
│   │   │   ├── ImagenPreview.tsx
│   │   │   └── ImagenEstadoBadge.tsx
│   │   ├── grupos-receta/
│   │   ├── revision/
│   │   └── facturacion/
│   ├── hooks/                       ← custom hooks por dominio
│   │   ├── useAuth.ts
│   │   ├── useGruposReceta.ts
│   │   ├── useImagenes.ts
│   │   ├── useOcrEstado.ts          ← polling de estado OCR
│   │   └── usePermisos.ts           ← verificación de permisos por módulo
│   ├── pages/
│   │   ├── auth/
│   │   │   └── LoginPage.tsx
│   │   ├── dashboard/
│   │   │   └── DashboardPage.tsx
│   │   ├── grupos-receta/
│   │   │   ├── GruposListPage.tsx
│   │   │   ├── GrupoDetallePage.tsx
│   │   │   └── NuevoGrupoPage.tsx
│   │   ├── imagenes/
│   │   │   ├── SubirImagenPage.tsx  ← elige entre cámara o galería
│   │   │   └── ImagenDetallePage.tsx
│   │   ├── revision/
│   │   │   ├── ColaRevisionPage.tsx
│   │   │   └── RevisionImagenPage.tsx
│   │   ├── facturacion/
│   │   │   ├── FacturacionListPage.tsx
│   │   │   └── GenerarCfdiPage.tsx
│   │   ├── usuarios/
│   │   └── configuracion/
│   ├── store/                       ← Zustand stores
│   │   ├── auth.store.ts            ← usuario, token, refresh token
│   │   └── ui.store.ts              ← sidebar, notificaciones
│   ├── types/                       ← tipos TypeScript alineados con DTOs de la API
│   │   ├── auth.types.ts
│   │   ├── grupo-receta.types.ts
│   │   ├── imagen.types.ts
│   │   ├── ocr.types.ts
│   │   ├── revision.types.ts
│   │   └── facturacion.types.ts
│   ├── utils/
│   │   ├── axios.instance.ts        ← interceptores JWT + refresh token
│   │   └── permisos.utils.ts
│   ├── router/
│   │   ├── AppRouter.tsx
│   │   └── ProtectedRoute.tsx       ← verifica rol + permiso antes de renderizar
│   └── main.tsx
├── .env.example
├── vite.config.ts
└── tsconfig.json
```

## Cómo importar / instalar dependencias

```bash
# 1. Clonar repo
git clone https://github.com/org/recetas-ocr-web.git
cd recetas-ocr-web

# 2. Instalar dependencias base
npm install

# Si se crea desde cero (nuevo proyecto):
npm create vite@latest recetas-ocr-web -- --template react-ts
cd recetas-ocr-web

# 3. Dependencias de producción
npm install react-router-dom
npm install @tanstack/react-query
npm install zustand
npm install react-hook-form @hookform/resolvers zod
npm install axios
npm install lucide-react
npm install react-dropzone
npm install react-webcam
npm install date-fns
npm install react-hot-toast
npm install tailwindcss @tailwindcss/vite

# 4. shadcn/ui (inicializar una vez)
npx shadcn@latest init
# Agregar componentes según se necesiten:
npx shadcn@latest add button card dialog table badge input select tabs

# 5. Dependencias de desarrollo
npm install -D vitest @testing-library/react @testing-library/jest-dom
npm install -D @types/react-webcam
```

## Variables de entorno (.env)

```bash
VITE_API_BASE_URL=http://localhost:64094/api
VITE_APP_NAME=RecetasOCR
VITE_MAX_IMAGE_SIZE_MB=15
```

## Patrones y convenciones

### Tipos alineados con la BD y la API
```typescript
// Siempre usar los mismos nombres que la BD (en español para dominio)
export type OrigenImagen = 'CAMARA' | 'GALERIA' | 'API' | 'ESCANER';
export type EstadoImagen =
  | 'RECIBIDA' | 'LEGIBLE' | 'ILEGIBLE'
  | 'CAPTURA_MANUAL_COMPLETA'
  | 'OCR_APROBADO' | 'OCR_BAJA_CONFIANZA'
  | 'EXTRACCION_COMPLETA' | 'EXTRACCION_INCOMPLETA'
  | 'REVISADA' | 'RECHAZADA';

export type EstadoGrupo =
  | 'RECIBIDO' | 'REQUIERE_CAPTURA_MANUAL' | 'PROCESANDO'
  | 'GRUPO_INCOMPLETO' | 'REVISION_PENDIENTE' | 'REVISADO_COMPLETO'
  | 'DATOS_FISCALES_INCOMPLETOS' | 'PENDIENTE_AUTORIZACION'
  | 'PENDIENTE_FACTURACION' | 'PREFACTURA_GENERADA'
  | 'FACTURADA' | 'ERROR_TIMBRADO_MANUAL' | 'RECHAZADO';
```

### Subida de imagen — flujo cámara vs galería
```typescript
// SubirImagenPage.tsx elige el modo; ambos producen el mismo resultado:
// un File + OrigenImagen que se envían al mismo endpoint POST /api/imagenes

// CamaraCaptura.tsx → usa react-webcam
// GaleriaImport.tsx → usa react-dropzone
// El campo origenImagen en el FormData lo setea el componente según el modo activo.
```

### Axios con interceptores JWT
```typescript
// axios.instance.ts
// - Adjunta Authorization: Bearer {token} en cada request
// - Si 401 → intenta refresh token automáticamente
// - Si refresh falla → logout y redirigir a /login
// - Loggear errores con console.error en desarrollo, silencioso en producción
```

### Hook de permisos
```typescript
// usePermisos.ts — verifica permisos del usuario actual
const { puedeEscribir, puedeLeer } = usePermisos('IMAGENES.SUBIR');
// Usar en componentes para mostrar/ocultar botones según permisos
```

### TanStack Query — convenciones de queryKey
```typescript
// Siempre arrays descriptivos:
['grupos-receta', 'list', filters]
['grupos-receta', 'detail', id]
['imagenes', 'by-grupo', idGrupo]
['ocr', 'estado', idImagen]     ← polling con refetchInterval
['revision', 'cola']
['facturacion', 'cfdi', id]
```

### Polling de estado OCR
```typescript
// useOcrEstado.ts — refresca el estado de la imagen mientras está en proceso
const { data: estado } = useQuery({
  queryKey: ['ocr', 'estado', idImagen],
  queryFn: () => ocrApi.getEstado(idImagen),
  refetchInterval: (data) =>
    // Dejar de hacer polling cuando llegue a estado final
    ['REVISADA','RECHAZADA','EXTRACCION_COMPLETA','EXTRACCION_INCOMPLETA',
     'OCR_BAJA_CONFIANZA','ILEGIBLE'].includes(data?.estado)
    ? false : 3000,
});
```

### ProtectedRoute — verificación de rol y módulo
```typescript
// Verificar que el usuario tiene el permiso requerido antes de renderizar la página
// Si no tiene permiso → redirigir a /sin-permiso (no a /login)
// Si no está autenticado → redirigir a /login
<ProtectedRoute requiredPermission="REVISION.APROBAR">
  <RevisionImagenPage />
</ProtectedRoute>
```

## Prohibiciones en este proyecto
- ❌ Sin `any` en TypeScript — usar tipos estrictos o `unknown`.
- ❌ Sin llamadas directas a `fetch` — siempre usar la instancia de Axios configurada.
- ❌ Sin lógica de negocio en componentes — mover a custom hooks o al API layer.
- ❌ Sin estado global para datos del servidor — usar TanStack Query, no Zustand para eso.
- ❌ Sin guardar el JWT en localStorage — usar httpOnly cookies o memory + refresh en cookie.
- ❌ Sin mostrar errores técnicos al usuario — traducir a mensajes legibles en español.
- ❌ Sin hardcodear URLs — siempre usar `import.meta.env.VITE_API_BASE_URL`.

## Prompts optimizados para Copilot en este repo

### Crear página con tabla y filtros
```
Crea la página {NombrePage}.tsx para listar {entidad} con:
- useQuery con queryKey: ['{entidad}', 'list', filters]
- Filtros: {lista de filtros}
- Columnas de la tabla: {lista de columnas}
- Paginación del servidor (page, pageSize)
- Badge de estado usando StatusBadge con colores según EstadoImagen/EstadoGrupo
- ProtectedRoute con permiso '{MODULO.ACCION}'
Usar shadcn/ui DataTable, Select, Input y Button.
```

### Crear flujo de subida de imagen
```
Crea el componente SubirImagenPage.tsx con dos modos de captura:
1. CAMARA: usar CamaraCaptura.tsx (react-webcam) — botón "Tomar foto"
2. GALERIA: usar GaleriaImport.tsx (react-dropzone) — drag & drop o click
El usuario elige el modo con tabs (shadcn Tabs).
Al confirmar: POST /api/imagenes como multipart/form-data con campos:
  archivo: File, idGrupo: string, origenImagen: 'CAMARA' | 'GALERIA'
Mostrar progreso de subida y redirigir al detalle del grupo al terminar.
```

### Crear pantalla de revisión humana
```
Crea RevisionImagenPage.tsx para que el revisor apruebe o rechace una imagen.
Mostrar: imagen original (UrlBlobRaw), resultado OCR (TextoCompleto),
campos extraídos editables (medicamento, dosis, CIE10, médico).
Si EsLegible=false: mostrar formulario de captura manual en lugar del OCR.
Botones: Aprobar (POST /api/revision/aprobar) | Rechazar con motivo (POST /api/revision/rechazar).
Toda edición de campo debe llamar PATCH /api/revision/corregir-campo para registrar
en aud.HistorialCorrecciones.
Permiso requerido: 'REVISION.APROBAR'.
```
