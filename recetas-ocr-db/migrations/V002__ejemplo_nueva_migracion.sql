-- Migration : V002__ejemplo_nueva_migracion
-- Autor     : {nombre}
-- Fecha     : {fecha}
-- Descripción: {descripción del cambio y su motivación}
-- ⚠️  Este archivo es una PLANTILLA. Renombrar con descripción real antes de usar.

USE RecetasOCR;
GO

-- Ejemplo: agregar columna idempotente
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'rec.GruposReceta')
    AND   name = N'NombreColumna'
)
BEGIN
    ALTER TABLE rec.GruposReceta
    ADD NombreColumna NVARCHAR(100) NULL;

    PRINT 'Columna NombreColumna agregada a rec.GruposReceta';
END
GO

-- Agregar índice si aplica
-- CREATE INDEX IX_GruposReceta_NombreColumna
--     ON rec.GruposReceta (NombreColumna)
--     WHERE NombreColumna IS NOT NULL;
-- GO
