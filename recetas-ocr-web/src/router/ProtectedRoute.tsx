import { Navigate, Outlet } from 'react-router-dom';
import { useAuthStore } from '@/store/auth.store';
import { tienePermiso } from '@/utils/permisos.utils';

interface Props {
  requiredPermission?: string;
}

export function ProtectedRoute({ requiredPermission }: Props) {
  const { isAuthenticated } = useAuthStore();

  if (!isAuthenticated) return <Navigate to="/login" replace />;

  if (requiredPermission && !tienePermiso(requiredPermission))
    return <Navigate to="/sin-permiso" replace />;

  return <Outlet />;
}
