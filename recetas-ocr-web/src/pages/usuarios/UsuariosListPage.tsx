import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { format } from 'date-fns'
import { Plus, Eye, ChevronLeft, ChevronRight, Power } from 'lucide-react'
import { PageHeader } from '@/components/common/PageHeader'
import { DataTable } from '@/components/common/DataTable'
import { ConfirmDialog } from '@/components/common/ConfirmDialog'
import { usePermisos } from '@/hooks/usePermisos'
import { useUsuarios, useCambiarEstadoUsuario } from '@/hooks/useUsuarios'
import type { Column } from '@/components/common/DataTable'
import type { UsuarioListaDto, FiltrosUsuarioDto } from '@/types/usuario.types'

function useDebounce<T>(value: T, delay: number): T {
  const [debounced, setDebounced] = useState<T>(value)
  useEffect(() => {
    const t = setTimeout(() => setDebounced(value), delay)
    return () => clearTimeout(t)
  }, [value, delay])
  return debounced
}

const PAGE_SIZE = 20

export default function UsuariosListPage() {
  const navigate = useNavigate()
  const { puedeEscribir: puedeAdministrar } = usePermisos('USUARIOS.ADMINISTRAR')

  const [busquedaInput, setBusquedaInput] = useState('')
  const [estadoFiltro, setEstadoFiltro] = useState<'todos' | 'activos' | 'inactivos'>('activos')
  const [page, setPage] = useState(1)
  const busqueda = useDebounce(busquedaInput, 400)

  useEffect(() => { setPage(1) }, [busqueda, estadoFiltro])

  const [confirm, setConfirm] = useState<{ open: boolean; usuario: UsuarioListaDto | null }>({
    open: false,
    usuario: null,
  })

  const filtros: FiltrosUsuarioDto = {
    ...(busqueda && { busqueda }),
    ...(estadoFiltro === 'activos' && { activo: true }),
    ...(estadoFiltro === 'inactivos' && { activo: false }),
    page,
    pageSize: PAGE_SIZE,
  }

  const { data, isLoading } = useUsuarios(filtros)
  const cambiarEstado = useCambiarEstadoUsuario()
  const totalPages = data?.totalPages ?? 1

  const columns: Column<UsuarioListaDto>[] = [
    {
      key: 'avatar',
      header: '',
      render: r => (
        <span className="inline-flex items-center justify-center w-8 h-8 rounded-full bg-blue-100 text-blue-700 text-sm font-semibold">
          {r.nombreCompleto.charAt(0).toUpperCase()}
        </span>
      ),
    },
    { key: 'username', header: 'Username' },
    { key: 'nombreCompleto', header: 'Nombre' },
    { key: 'email', header: 'Email' },
    {
      key: 'nombreRol',
      header: 'Rol',
      render: r => (
        <span className="inline-flex items-center rounded-full bg-indigo-100 text-indigo-700 text-xs font-medium px-2.5 py-0.5">
          {r.nombreRol}
        </span>
      ),
    },
    {
      key: 'activo',
      header: 'Estado',
      render: r => (
        <span
          className={`inline-flex items-center rounded-full text-xs font-medium px-2.5 py-0.5 ${
            r.activo ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'
          }`}
        >
          {r.activo ? 'Activo' : 'Inactivo'}
        </span>
      ),
    },
    {
      key: 'ultimoAcceso',
      header: 'Último acceso',
      render: r =>
        r.ultimoAcceso ? format(new Date(r.ultimoAcceso), 'dd/MM/yyyy HH:mm') : '—',
    },
    {
      key: 'acciones',
      header: 'Acciones',
      render: r => (
        <div className="flex items-center gap-3">
          <button
            onClick={() => navigate(`/usuarios/${r.id}`)}
            className="inline-flex items-center gap-1 text-blue-600 hover:text-blue-800 text-xs font-medium transition-colors"
          >
            <Eye className="w-3.5 h-3.5" />
            Ver
          </button>
          {puedeAdministrar && (
            <button
              onClick={() => setConfirm({ open: true, usuario: r })}
              className={`inline-flex items-center gap-1 text-xs font-medium transition-colors ${
                r.activo
                  ? 'text-red-500 hover:text-red-700'
                  : 'text-green-600 hover:text-green-800'
              }`}
            >
              <Power className="w-3.5 h-3.5" />
              {r.activo ? 'Desactivar' : 'Activar'}
            </button>
          )}
        </div>
      ),
    },
  ]

  function handleConfirmEstado() {
    if (!confirm.usuario) return
    cambiarEstado.mutate(
      { id: confirm.usuario.id, activo: !confirm.usuario.activo },
      { onSettled: () => setConfirm({ open: false, usuario: null }) },
    )
  }

  return (
    <div className="p-6">
      <PageHeader
        title="Usuarios"
        subtitle="Gestión de usuarios y permisos del sistema"
        actions={
          puedeAdministrar ? (
            <button
              onClick={() => navigate('/usuarios/nuevo')}
              className="inline-flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors"
            >
              <Plus className="w-4 h-4" />
              Nuevo Usuario
            </button>
          ) : undefined
        }
      />

      {/* Filtros */}
      <div className="flex flex-wrap gap-3 mb-5">
        <input
          type="text"
          value={busquedaInput}
          onChange={e => setBusquedaInput(e.target.value)}
          placeholder="Buscar por nombre, email o username..."
          className="flex-1 min-w-[220px] border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
        <select
          value={estadoFiltro}
          onChange={e => setEstadoFiltro(e.target.value as typeof estadoFiltro)}
          className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
        >
          <option value="todos">Todos</option>
          <option value="activos">Activos</option>
          <option value="inactivos">Inactivos</option>
        </select>
      </div>

      <DataTable
        columns={columns}
        data={data?.items ?? []}
        isLoading={isLoading}
        emptyMessage="No se encontraron usuarios"
      />

      {/* Paginación */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between mt-4">
          <p className="text-sm text-gray-500">
            Página {page} de {totalPages} — {data?.total ?? 0} usuarios
          </p>
          <div className="flex items-center gap-2">
            <button
              onClick={() => setPage(p => Math.max(1, p - 1))}
              disabled={page === 1}
              className="p-1.5 rounded border border-gray-300 disabled:opacity-40 hover:bg-gray-50 transition-colors"
            >
              <ChevronLeft className="w-4 h-4" />
            </button>
            <button
              onClick={() => setPage(p => Math.min(totalPages, p + 1))}
              disabled={page === totalPages}
              className="p-1.5 rounded border border-gray-300 disabled:opacity-40 hover:bg-gray-50 transition-colors"
            >
              <ChevronRight className="w-4 h-4" />
            </button>
          </div>
        </div>
      )}

      <ConfirmDialog
        open={confirm.open}
        title={confirm.usuario?.activo ? 'Desactivar usuario' : 'Activar usuario'}
        message={
          confirm.usuario?.activo
            ? `¿Desactivar a ${confirm.usuario?.nombreCompleto}? Se cerrarán todas sus sesiones activas.`
            : `¿Activar a ${confirm.usuario?.nombreCompleto}?`
        }
        confirmLabel={confirm.usuario?.activo ? 'Sí, desactivar' : 'Sí, activar'}
        variant={confirm.usuario?.activo ? 'danger' : 'primary'}
        isLoading={cambiarEstado.isPending}
        onConfirm={handleConfirmEstado}
        onCancel={() => setConfirm({ open: false, usuario: null })}
      />
    </div>
  )
}
