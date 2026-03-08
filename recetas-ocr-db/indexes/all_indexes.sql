-- Índices del sistema RecetasOCR
-- Todos los índices están incluidos en V001__baseline_v5.2.sql
-- Este archivo documenta los índices críticos y su propósito.
-- Usar como referencia al agregar nuevos índices en migraciones futuras.

/*
ÍNDICES CRÍTICOS — NO MODIFICAR SIN ANÁLISIS DE PLAN DE EJECUCIÓN

1. IX_Cola_Polling  (ocr.ColaProcesamiento)
   Columnas : EstadoCola, Bloqueado, Prioridad, FechaEncolado
   Propósito: Polling del Worker cada 3s — SELECT TOP 1 + UPDATE optimista
   Impacto  : Sin este índice el worker haría full scan en tabla de alta escritura

2. IX_Grupos_SinFolio  (rec.GruposReceta)
   Columnas : IdCliente, IdAseguradora, FechaConsulta
   Propósito: Agrupación de recetas sin folio (regla de negocio)

3. IX_Img_Grupo  (rec.Imagenes)
   Columnas : IdGrupo
   Propósito: Obtener todas las imágenes de un grupo (1..N)

4. IX_Usuarios_Email / IX_Usuarios_Username  (seg.Usuarios)
   Propósito: Login — búsqueda por credenciales

5. IX_CFDI_UUID  (fac.CFDI)
   Columnas : UUID UNIQUE
   Propósito: Identificador fiscal definitivo — búsquedas frecuentes del PAC

ÍNDICES FILTRADOS (columnas nullable)
   IX_Grupos_FolioBase    WHERE FolioBase IS NOT NULL
   IX_Img_FolioBase       WHERE FolioBase IS NOT NULL
   IX_Img_CapturaManual   WHERE EsCapturaManual = 1
   IX_Med_SustanciaActiva WHERE SustanciaActiva IS NOT NULL
   IX_Med_CIE10           WHERE CodigoCIE10 IS NOT NULL
*/
