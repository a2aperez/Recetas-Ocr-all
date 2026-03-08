# Diagrama ER — RecetasOCR v5.2

## Esquemas y relaciones principales

```
seg.Usuarios ──────────────────────────────────────────────────────────┐
  │ IdRol → seg.Roles                                                   │
  │ IdAseguradoraAsignada → cat.Aseguradoras                           │
  └─ seg.PermisosUsuario → seg.Modulos                                  │
                                                                        │
seg.Roles                                                               │
  └─ seg.PermisosRol → seg.Modulos                                      │
                                                                        │
cat.Aseguradoras (auto-referencia 2 niveles)                           │
  └─ IdAseguradoraPadre → cat.Aseguradoras                             │
                                                                        │
rec.Clientes                                                            │
  └─ rec.GruposReceta (1..N)                                            │
       │ IdAseguradora → cat.Aseguradoras                               │
       │ IdEstadoGrupo → cat.EstadosGrupo                               │
       │ IdUsuarioAlta → seg.Usuarios ◄─────────────────────────────────┘
       └─ rec.Imagenes (1..N)
            │ IdEstadoImagen → cat.EstadosImagen
            │ IdUsuarioSubida → seg.Usuarios
            │ OrigenImagen: CAMARA | GALERIA | API | ESCANER
            │
            ├─ ocr.ColaProcesamiento
            ├─ ocr.ResultadosOCR
            ├─ ocr.ResultadosExtraccion
            ├─ med.MedicamentosReceta (1..N por imagen)
            │    └─ IdMedicamentoCatalogo → cat.Medicamentos
            └─ rev.AsignacionesRevision → seg.Usuarios
                 └─ rev.RevisionesHumanas

fac.PreFacturas
  │ IdGrupo → rec.GruposReceta
  └─ fac.PartidasPreFactura → med.MedicamentosReceta
       └─ fac.CFDI (UUID único — inmutable)
            └─ fac.PartidasCFDI (inmutable)

aud.HistorialEstadosImagen  → rec.Imagenes
aud.HistorialEstadosGrupo   → rec.GruposReceta
aud.HistorialCorrecciones   → rec.Imagenes | rec.GruposReceta | med.MedicamentosReceta
aud.LogProcesamiento        → rec.Imagenes | rec.GruposReceta
```

## Flujo de estados de imagen
```
RECIBIDA → LEGIBLE → OCR_APROBADO → EXTRACCION_COMPLETA → REVISADA ✓
                   → OCR_BAJA_CONFIANZA → EXTRACCION_INCOMPLETA → REVISADA ✓
         → ILEGIBLE → CAPTURA_MANUAL_COMPLETA → REVISADA ✓
                                              → RECHAZADA ✗
```

## Flujo de estados de grupo
```
RECIBIDO → PROCESANDO → REVISION_PENDIENTE → REVISADO_COMPLETO
         → REQUIERE_CAPTURA_MANUAL ──────────────────────────────►
                                              ↓
                                   PENDIENTE_FACTURACION
                                              ↓
                                   PREFACTURA_GENERADA
                                              ↓
                                        FACTURADA ✓
                                  ERROR_TIMBRADO_MANUAL ↺
                                        RECHAZADO ✗
```
