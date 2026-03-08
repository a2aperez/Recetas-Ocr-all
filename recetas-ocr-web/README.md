# recetas-ocr-web — React 18 + TypeScript + Vite

Frontend del sistema OCR de recetas médicas.

## Requisitos
- Node.js 20+
- API corriendo en http://localhost:5000

## Inicio rápido

```bash
# 1. Instalar dependencias
npm install

# 2. Inicializar shadcn/ui (primera vez)
npx shadcn@latest init
npx shadcn@latest add button card dialog table badge input select tabs

# 3. Copiar variables de entorno
cp .env.example .env.local
# Editar VITE_API_BASE_URL si el API corre en otro puerto

# 4. Levantar dev server
npm run dev
# http://localhost:5173
```

## Estructura
```
src/
  api/         → clientes HTTP por dominio (axios)
  components/  → UI reutilizable (common) + por feature
  hooks/       → custom hooks: useAuth, usePermisos, useOcrEstado
  pages/       → páginas por módulo
  router/      → AppRouter + ProtectedRoute (permiso granular)
  store/       → Zustand: auth.store
  types/       → tipos TypeScript alineados con la BD
  utils/       → axios.instance con interceptores JWT
```

## Reglas clave
- Sin `any` en TypeScript
- Estado del servidor → TanStack Query (no Zustand)
- JWT en sessionStorage (no localStorage)
- Subida de imagen: elegir entre CamaraCaptura (react-webcam) o GaleriaImport (react-dropzone)
- Polling OCR: useOcrEstado se detiene automáticamente en estados finales
