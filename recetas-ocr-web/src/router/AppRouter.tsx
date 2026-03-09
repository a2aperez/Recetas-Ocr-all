import { createBrowserRouter, RouterProvider, Navigate } from 'react-router-dom';
import { lazy, Suspense } from 'react';
import { ProtectedRoute } from './ProtectedRoute';
import { AppLayout } from '@/components/layout/AppLayout';

const LoginPage          = lazy(() => import('@/pages/auth/LoginPage'));
const DashboardPage      = lazy(() => import('@/pages/dashboard/DashboardPage'));
const GruposListPage     = lazy(() => import('@/pages/grupos-receta/GruposListPage'));
const NuevoGrupoPage     = lazy(() => import('@/pages/grupos-receta/NuevoGrupoPage'));
const GrupoDetallePage   = lazy(() => import('@/pages/grupos-receta/GrupoDetallePage'));
const SubirImagenPage    = lazy(() => import('@/pages/imagenes/SubirImagenPage'));
const ImagenDetallePage  = lazy(() => import('@/pages/imagenes/ImagenDetallePage'));
const ColaRevisionPage   = lazy(() => import('@/pages/revision/ColaRevisionPage'));
const RevisionImagenPage = lazy(() => import('@/pages/revision/RevisionImagenPage'));
const FacturacionListPage = lazy(() => import('@/pages/facturacion/FacturacionListPage'));
const GenerarCfdiPage    = lazy(() => import('@/pages/facturacion/GenerarCfdiPage'));
const CatalogosPage      = lazy(() => import('@/pages/catalogos/CatalogosPage'));
const UsuariosListPage   = lazy(() => import('@/pages/usuarios/UsuariosListPage'));
const NuevoUsuarioPage   = lazy(() => import('@/pages/usuarios/NuevoUsuarioPage'));
const UsuarioDetallePage = lazy(() => import('@/pages/usuarios/UsuarioDetallePage'));

const Loading = () => (
  <div className="flex items-center justify-center h-screen">
    <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" />
  </div>
);

const router = createBrowserRouter([
  { path: '/login', element: <Suspense fallback={<Loading />}><LoginPage /></Suspense> },

  {
    element: <ProtectedRoute />,
    children: [
      {
        element: <AppLayout />,
        children: [
          { index: true, element: <Navigate to="/dashboard" replace /> },
          { path: '/dashboard', element: <Suspense fallback={<Loading />}><DashboardPage /></Suspense> },
          { path: '/catalogos', element: <Suspense fallback={<Loading />}><CatalogosPage /></Suspense> },
          { path: '/sin-permiso', element: <div className="p-8 text-center text-red-600 text-lg">Sin permisos para acceder a esta sección.</div> },

          {
            element: <ProtectedRoute requiredPermission="GRUPOS.VER" />,
            children: [
              { path: '/grupos-receta', element: <Suspense fallback={<Loading />}><GruposListPage /></Suspense> },
              { path: '/grupos-receta/:id', element: <Suspense fallback={<Loading />}><GrupoDetallePage /></Suspense> },
            ],
          },
          {
            element: <ProtectedRoute requiredPermission="IMAGENES.SUBIR" />,
            children: [
              { path: '/grupos-receta/nuevo', element: <Suspense fallback={<Loading />}><NuevoGrupoPage /></Suspense> },
              { path: '/imagenes/subir', element: <Suspense fallback={<Loading />}><SubirImagenPage /></Suspense> },
            ],
          },
          {
            element: <ProtectedRoute requiredPermission="IMAGENES.VER" />,
            children: [
              { path: '/imagenes/:id', element: <Suspense fallback={<Loading />}><ImagenDetallePage /></Suspense> },
            ],
          },
          {
            element: <ProtectedRoute requiredPermission="REVISION.VER" />,
            children: [
              { path: '/revision', element: <Suspense fallback={<Loading />}><ColaRevisionPage /></Suspense> },
            ],
          },
          {
            element: <ProtectedRoute requiredPermission="REVISION.APROBAR" />,
            children: [
              { path: '/revision/:id', element: <Suspense fallback={<Loading />}><RevisionImagenPage /></Suspense> },
            ],
          },
          {
            element: <ProtectedRoute requiredPermission="FACTURACION.VER" />,
            children: [
              { path: '/facturacion', element: <Suspense fallback={<Loading />}><FacturacionListPage /></Suspense> },
            ],
          },
          {
            element: <ProtectedRoute requiredPermission="FACTURACION.GENERAR" />,
            children: [
              { path: '/facturacion/:idGrupo/generar', element: <Suspense fallback={<Loading />}><GenerarCfdiPage /></Suspense> },
            ],
          },
          {
            element: <ProtectedRoute requiredPermission="USUARIOS.ADMINISTRAR" />,
            children: [
              { path: '/usuarios', element: <Suspense fallback={<Loading />}><UsuariosListPage /></Suspense> },
              { path: '/usuarios/nuevo', element: <Suspense fallback={<Loading />}><NuevoUsuarioPage /></Suspense> },
              { path: '/usuarios/:id', element: <Suspense fallback={<Loading />}><UsuarioDetallePage /></Suspense> },
            ],
          },
        ],
      },
    ],
  },

  { path: '*', element: <Navigate to="/dashboard" replace /> },
]);

export function AppRouter() {
  return <RouterProvider router={router} />;
}
