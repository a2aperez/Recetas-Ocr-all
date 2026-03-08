-- Seed 03 — Aseguradoras y sub-aseguradoras
-- Jerarquía: padre (IdAseguradoraPadre = NULL) → hijos
-- Detectadas en recetas escaneadas:
--   Padres: BANXICO, BANCOMEXT, LYFC, BANOBRAS, BANORTE, BBVA, BANAMEX, NAFIN, PARTICULAR
--   Hijos : BBVA_BUPA, BBVA_VITA, BANAMEX_BUPA
-- Datos ya incluidos en el baseline; agregar nuevas aseguradoras aquí.
USE RecetasOCR;
GO
PRINT 'Seed 03 — aseguradoras ya incluidas en el baseline V001';
GO
