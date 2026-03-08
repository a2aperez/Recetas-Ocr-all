import { Outlet, NavLink, useNavigate } from 'react-router-dom';
import { useAuthStore } from '@/store/auth.store';
import { usePermisos } from '@/hooks/usePermisos';
import { authApi } from '@/api/auth.api';

interface NavItem {
  label: string;
  icon: string;
  to: string;
  permiso?: string;
}

const NAV_ITEMS: NavItem[] = [
  { label: 'Dashboard',        icon: '🏠', to: '/dashboard' },
  { label: 'Grupos de Receta', icon: '📋', to: '/grupos-receta',  permiso: 'GRUPOS.VER' },
  { label: 'Subir Imágenes',   icon: '📷', to: '/imagenes/subir', permiso: 'IMAGENES.SUBIR' },
  { label: 'Cola de Revisión', icon: '🔍', to: '/revision',        permiso: 'REVISION.VER' },
  { label: 'Facturación',      icon: '🧾', to: '/facturacion',     permiso: 'FACTURACION.VER' },
  { label: 'Usuarios',         icon: '👥', to: '/usuarios',        permiso: 'USUARIOS.ADMINISTRAR' },
];

function SidebarLink({ item }: { item: NavItem }) {
  const perm = usePermisos(item.permiso ?? '');
  if (item.permiso && !perm.puedeLeer) return null;
  return (
    <NavLink
      to={item.to}
      className={({ isActive }) =>
        `flex items-center gap-3 px-4 py-2.5 rounded-lg text-sm font-medium transition-colors ${
          isActive
            ? 'bg-blue-600 text-white'
            : 'text-gray-300 hover:bg-gray-700 hover:text-white'
        }`
      }
    >
      <span>{item.icon}</span>
      <span>{item.label}</span>
    </NavLink>
  );
}

export function AppLayout() {
  const { usuario, logout } = useAuthStore();
  const navigate = useNavigate();

  const handleLogout = async () => {
    try { await authApi.logout(); } catch { /* ignore */ }
    logout();
    navigate('/login', { replace: true });
  };

  return (
    <div className="flex h-screen bg-gray-100">
      {/* Sidebar */}
      <aside className="w-64 bg-gray-900 flex flex-col shrink-0">
        <div className="px-6 py-5 border-b border-gray-700">
          <h1 className="text-white font-bold text-lg tracking-wide">RecetasOCR</h1>
        </div>
        <nav className="flex-1 px-3 py-4 space-y-1 overflow-y-auto">
          {NAV_ITEMS.map((item) => (
            <SidebarLink key={item.to} item={item} />
          ))}
        </nav>
      </aside>

      {/* Main */}
      <div className="flex-1 flex flex-col min-w-0">
        {/* Header */}
        <header className="bg-white border-b border-gray-200 px-6 py-3 flex items-center justify-between shrink-0">
          <div />
          <div className="flex items-center gap-4">
            <div className="text-right">
              <p className="text-sm font-medium text-gray-900">{usuario?.nombreCompleto}</p>
              <p className="text-xs text-gray-500">{usuario?.rol}</p>
            </div>
            <button
              onClick={handleLogout}
              className="text-sm text-red-600 hover:text-red-700 font-medium px-3 py-1.5 rounded border border-red-200 hover:bg-red-50 transition-colors"
            >
              Cerrar sesión
            </button>
          </div>
        </header>

        {/* Content */}
        <main className="flex-1 overflow-y-auto">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
