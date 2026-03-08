-- Seed 05 — Configuración de proveedores OCR
-- ⚠️  COMPLETAR ANTES DE USAR EN PRODUCCIÓN:
--     Actualizar ApiKeyEncriptada y UrlBase con valores reales.
--     Solo 1 registro puede tener EsPrincipal=1.
USE RecetasOCR;
GO

-- Actualizar el proveedor activo (cambiar según el ambiente)
UPDATE cfg.ConfiguracionesOCR
SET    EsPrincipal = 0
WHERE  EsPrincipal = 1;

-- Activar el proveedor deseado (ejemplo: API OCR personalizada)
UPDATE cfg.ConfiguracionesOCR
SET    EsPrincipal       = 1,
       Activo            = 1,
       ApiKeyEncriptada  = N'{API_KEY_ENCRIPTADA_AQUI}',
       UrlBase           = N'https://TU_ENDPOINT/v1/ocr',
       ModificadoPor     = N'setup',
       FechaModificacion = GETUTCDATE()
WHERE  Clave = N'CUSTOM_API';
GO
