# RecetasOCR — Workspace

Sistema OCR para digitalización de recetas médicas mexicanas con facturación CFDI 4.0.

## Repos del workspace

| Repo | Stack | Puerto |
|---|---|---|
| `recetas-ocr-db`  | SQL Server 2019 / Azure SQL | 1433 |
| `recetas-ocr-api` | .NET 9 Web API + Worker     | 5000 |
| `recetas-ocr-web` | React 18 + TypeScript       | 5173 |

## Orden de setup

```bash
# 1. BD — ejecutar script base
cd recetas-ocr-db
sqlcmd -S localhost -U sa -P TuPassword -i migrations/V001__baseline_v5.2.sql

# 2. API — levantar en dos terminales
cd recetas-ocr-api
dotnet run --project src/RecetasOCR.API      # terminal 1 → puerto 5000
dotnet run --project src/RecetasOCR.Worker   # terminal 2 → worker polling

# 3. Web — levantar frontend
cd recetas-ocr-web
npm install && npm run dev                    # terminal 3 → puerto 5173
```

## Instrucciones Copilot (Opción A)

Cada repo tiene su `.github/copilot-instructions.md` con:
- Contexto global del proyecto y todas las reglas de negocio
- Stack, estructura de carpetas, patrones y convenciones del repo
- Prompts optimizados listos para usar en Copilot Chat

Abrir en VS Code: `code recetas-ocr.code-workspace`

## Reglas de negocio inamovibles
- Toda imagen → blob `recetas-raw` SIEMPRE (UrlBlobRaw NOT NULL)
- Ilegibles NUNCA se eliminan (evidencia permanente)
- OCR = API externa (sin Tesseract ni librerías locales)
- Revisión humana obligatoria antes de facturar
- Solo grupos REVISADO_COMPLETO avanzan a CFDI 4.0
- Aseguradoras: máximo 2 niveles (padre → hijo)
- ModificadoPor + FechaModificacion en toda escritura
