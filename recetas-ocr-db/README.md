# recetas-ocr-db

Sistema OCR de recetas médicas mexicanas para aseguradoras institucionales con facturación CFDI 4.0.  
BD: 44 tablas · 9 esquemas · sin SPs · sin Vistas · sin Triggers.

El script base completo está en [migrations/V001__baseline_v5.2.sql](migrations/V001__baseline_v5.2.sql).

## Requisitos

- SQL Server 2019+ o Azure SQL Database
- `sqlcmd` (link oficial: https://learn.microsoft.com/en-us/sql/tools/sqlcmd/sqlcmd-utility)
- Alternativa gráfica: Azure Data Studio

## Estructura de carpetas

````text
recetas-ocr-db/
├── migrations/  → Scripts versionados V{N}__descripcion.sql
├── seeds/       → Datos iniciales por categoría (ejecutar en orden numérico)
├── indexes/     → Documentación y scripts de índices críticos
├── verify/      → Queries de verificación post-instalación
├── scripts/     → Automatización bash y PowerShell
├── docs/        → Diagrama ER, guías de migraciones y aseguradoras
└── .github/     → CI/CD y plantilla de PR
