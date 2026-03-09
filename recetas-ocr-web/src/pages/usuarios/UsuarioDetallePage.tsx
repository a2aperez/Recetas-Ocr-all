import { useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { format } from 'date-fns'
import { ArrowLeft, Shield, Activity, User, Key } from 'lucide-react'
import { PageHeader } from '@/components/common/PageHeader'
import { ConfirmDialog } from '@/components/common/ConfirmDialog'
import { usePermisos } from '@/hooks/usePermisos'
import { useAuthStore } from '@/store/auth.store'
import {
  useUsuarioDetalle,
  useCambiarEstadoUsuario,
  useCambiarPassword,
  useAsignarPermisos,
} from '@/hooks/useUsuarios'
import type { PermisoUsuarioDto } from '@/types/usuario.types'

// ── Helpers ───────────────────────────────────────────────────────────────────

type TabId = 'informacion' | 'permisos' | 'actividad'

const inputClass =
  'w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-50 disabled:text-gray-400'
const errorClass = 'mt-1 text-xs text-red-500'

function TabNav({
  tabs,
  active,
  onChange,
}: {
  tabs: { id: TabId; label: string; icon: React.ReactNode }[]
  active: TabId
  onChange: (id: TabId) => void
}) {
  return (
    <div className="flex gap-1 border-b border-gray-200 mb-6">
      {tabs.map(t => (
        <button
          key={t.id}
          onClick={() => onChange(t.id)}
          className={`inline-flex items-center gap-2 px-4 py-2.5 text-sm font-medium whitespace-nowrap border-b-2 transition-colors ${
            active === t.id
              ? 'border-blue-600 text-blue-600'
              : 'border-transparent text-gray-500 hover:text-gray-700'
          }`}
        >
          {t.icon}
          {t.label}
        </button>
      ))}
    </div>
  )
}

// ── Password Schema ───────────────────────────────────────────────────────────

const passwordSchema = z
  .object({
    passwordActual: z.string().min(1, 'Requerido'),
    passwordNuevo: z.string().min(8, 'Mínimo 8 caracteres'),
    passwordConfirmacion: z.string().min(1, 'Requerido'),
  })
  .refine(d => d.passwordNuevo === d.passwordConfirmacion, {
    message: 'Las contraseñas no coinciden',
    path: ['passwordConfirmacion'],
  })

type PasswordForm = z.infer<typeof passwordSchema>

// ── Sub-components ────────────────────────────────────────────────────────────

function CambiarPasswordDialog({
  idUsuario,
  onClose,
}: {
  idUsuario: string
  onClose: () => void
}) {
  const cambiarPassword = useCambiarPassword()

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<PasswordForm>({ resolver: zodResolver(passwordSchema) })

  async function onSubmit(data: PasswordForm) {
    try {
      await cambiarPassword.mutateAsync({ id: idUsuario, dto: data })
      onClose()
    } catch {
      // error surfaced by mutation
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/40 backdrop-blur-sm" onClick={onClose} />
      <div className="relative w-full max-w-sm mx-4 bg-white rounded-xl shadow-xl">
        <div className="flex items-center gap-2 p-5 border-b border-gray-100">
          <Key className="w-4 h-4 text-gray-500" />
          <h3 className="text-base font-semibold text-gray-900">Cambiar contraseña</h3>
        </div>
        <form onSubmit={handleSubmit(onSubmit)} className="p-5 space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Contraseña actual <span className="text-red-500">*</span>
            </label>
            <input {...register('passwordActual')} type="password" className={inputClass} />
            {errors.passwordActual && (
              <p className={errorClass}>{errors.passwordActual.message}</p>
            )}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Nueva contraseña <span className="text-red-500">*</span>
            </label>
            <input {...register('passwordNuevo')} type="password" className={inputClass} />
            {errors.passwordNuevo && (
              <p className={errorClass}>{errors.passwordNuevo.message}</p>
            )}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Confirmar contraseña <span className="text-red-500">*</span>
            </label>
            <input {...register('passwordConfirmacion')} type="password" className={inputClass} />
            {errors.passwordConfirmacion && (
              <p className={errorClass}>{errors.passwordConfirmacion.message}</p>
            )}
          </div>
          <div className="flex justify-end gap-3 pt-2">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={cambiarPassword.isPending}
              className="px-4 py-2 text-sm font-medium bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 transition-colors"
            >
              {cambiarPassword.isPending ? 'Guardando...' : 'Cambiar'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

// ── Page ──────────────────────────────────────────────────────────────────────

export default function UsuarioDetallePage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const currentUserId = useAuthStore(s => s.usuario?.id)
  const { puedeEscribir: puedeAdministrar } = usePermisos('USUARIOS.ADMINISTRAR')

  const [activeTab, setActiveTab] = useState<TabId>('informacion')
  const [showPasswordDialog, setShowPasswordDialog] = useState(false)
  const [confirmEstado, setConfirmEstado] = useState(false)

  const { data: usuario, isLoading } = useUsuarioDetalle(id ?? '')
  const cambiarEstado = useCambiarEstadoUsuario()
  const asignarPermisos = useAsignarPermisos()

  // ── Permisos tab local state ─────────────────────────────────────────────
  const [permisosEdit, setPermisosEdit] = useState<Record<
    string,
    { puedeLeer: boolean; puedeEscribir: boolean; puedeEliminar: boolean; denegado: boolean }
  > | null>(null)

  // Initialize edit state from loaded usuario
  const permisosActivos = (() => {
    if (permisosEdit) return permisosEdit
    if (!usuario) return {}
    return Object.fromEntries(
      usuario.permisos.map(p => [
        p.modulo,
        {
          puedeLeer: p.puedeLeer,
          puedeEscribir: p.puedeEscribir,
          puedeEliminar: p.puedeEliminar,
          denegado: false,
        },
      ]),
    )
  })()

  function togglePermiso(
    modulo: string,
    campo: 'puedeLeer' | 'puedeEscribir' | 'puedeEliminar' | 'denegado',
  ) {
    setPermisosEdit(prev => {
      const base = prev ?? permisosActivos
      const current = base[modulo] ?? {
        puedeLeer: false,
        puedeEscribir: false,
        puedeEliminar: false,
        denegado: false,
      }
      const updated = { ...current, [campo]: !current[campo] }
      // Si se activa Denegado, limpiar los demás
      if (campo === 'denegado' && updated.denegado) {
        updated.puedeLeer = false
        updated.puedeEscribir = false
        updated.puedeEliminar = false
      }
      // Si se activa cualquier permiso, quitar Denegado
      if (campo !== 'denegado' && updated[campo]) {
        updated.denegado = false
      }
      return { ...base, [modulo]: updated }
    })
  }

  function handleGuardarPermisos() {
    if (!id) return
    const permisos: PermisoUsuarioDto[] = Object.entries(permisosActivos).map(
      ([modulo, p]) => ({ modulo, ...p }),
    )
    asignarPermisos.mutate({ id, permisos })
  }

  function handleConfirmEstado() {
    if (!id || !usuario) return
    cambiarEstado.mutate(
      { id, activo: !usuario.activo },
      { onSettled: () => setConfirmEstado(false) },
    )
  }

  // ── Loading / Error ──────────────────────────────────────────────────────
  if (isLoading) {
    return (
      <div className="p-6">
        <div className="animate-pulse space-y-4">
          <div className="h-8 bg-gray-200 rounded w-48" />
          <div className="h-4 bg-gray-100 rounded w-32" />
          <div className="h-64 bg-gray-100 rounded-xl" />
        </div>
      </div>
    )
  }

  if (!usuario) {
    return (
      <div className="p-6 text-center text-gray-500">
        <p>Usuario no encontrado.</p>
        <button
          onClick={() => navigate('/usuarios')}
          className="mt-4 text-blue-600 text-sm hover:underline"
        >
          Volver a la lista
        </button>
      </div>
    )
  }

  const esPropioUsuario = currentUserId === usuario.id
  const tabs = [
    { id: 'informacion' as TabId, label: 'Información', icon: <User className="w-4 h-4" /> },
    ...(puedeAdministrar
      ? [{ id: 'permisos' as TabId, label: 'Permisos', icon: <Shield className="w-4 h-4" /> }]
      : []),
    { id: 'actividad' as TabId, label: 'Actividad', icon: <Activity className="w-4 h-4" /> },
  ]

  return (
    <div className="p-6 max-w-4xl mx-auto">
      <PageHeader
        title={usuario.nombreCompleto}
        subtitle={`@${usuario.username} · ${usuario.nombreRol}`}
        actions={
          <div className="flex items-center gap-3">
            <button
              onClick={() => navigate(-1)}
              className="inline-flex items-center gap-2 text-gray-600 hover:text-gray-900 text-sm"
            >
              <ArrowLeft className="w-4 h-4" />
              Volver
            </button>
            {puedeAdministrar && (
              <button
                onClick={() => setConfirmEstado(true)}
                className={`px-4 py-2 text-sm font-medium rounded-lg transition-colors ${
                  usuario.activo
                    ? 'bg-red-50 text-red-600 hover:bg-red-100 border border-red-200'
                    : 'bg-green-50 text-green-700 hover:bg-green-100 border border-green-200'
                }`}
              >
                {usuario.activo ? 'Desactivar usuario' : 'Activar usuario'}
              </button>
            )}
          </div>
        }
      />

      <TabNav tabs={tabs} active={activeTab} onChange={setActiveTab} />

      {/* ── TAB: Información ──────────────────────────────────────────────── */}
      {activeTab === 'informacion' && (
        <div className="space-y-5">
          <div className="bg-white rounded-xl border border-gray-200 p-6">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-5">
              <InfoRow label="Username" value={usuario.username} />
              <InfoRow label="Email" value={usuario.email} />
              <InfoRow label="Nombre completo" value={usuario.nombreCompleto} />
              <InfoRow
                label="Rol"
                value={
                  <span className="inline-flex items-center rounded-full bg-indigo-100 text-indigo-700 text-xs font-medium px-2.5 py-0.5">
                    {usuario.nombreRol}
                  </span>
                }
              />
              <InfoRow
                label="Estado"
                value={
                  <span
                    className={`inline-flex items-center rounded-full text-xs font-medium px-2.5 py-0.5 ${
                      usuario.activo ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'
                    }`}
                  >
                    {usuario.activo ? 'Activo' : 'Inactivo'}
                  </span>
                }
              />
              <InfoRow
                label="Último acceso"
                value={
                  usuario.ultimoAcceso
                    ? format(new Date(usuario.ultimoAcceso), 'dd/MM/yyyy HH:mm')
                    : '—'
                }
              />
            </div>

            {usuario.requiereCambioPassword && (
              <div className="mt-4 flex items-center gap-2 text-xs text-amber-700 bg-amber-50 rounded-lg px-3 py-2 border border-amber-200">
                <Key className="w-3.5 h-3.5 shrink-0" />
                Este usuario debe cambiar su contraseña al próximo inicio de sesión.
              </div>
            )}
          </div>

          {/* Cambiar contraseña — solo para el propio usuario */}
          {esPropioUsuario && (
            <div className="bg-white rounded-xl border border-gray-200 p-5">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-900">Contraseña</p>
                  <p className="text-xs text-gray-500 mt-0.5">
                    Cambia tu contraseña de acceso al sistema
                  </p>
                </div>
                <button
                  onClick={() => setShowPasswordDialog(true)}
                  className="inline-flex items-center gap-2 px-4 py-2 text-sm font-medium bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
                >
                  <Key className="w-4 h-4" />
                  Cambiar contraseña
                </button>
              </div>
            </div>
          )}
        </div>
      )}

      {/* ── TAB: Permisos ─────────────────────────────────────────────────── */}
      {activeTab === 'permisos' && puedeAdministrar && (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <div className="flex items-center justify-between px-5 py-4 border-b border-gray-100">
            <p className="text-sm font-medium text-gray-700">
              Permisos individuales del usuario
            </p>
            <button
              onClick={handleGuardarPermisos}
              disabled={asignarPermisos.isPending}
              className="px-4 py-2 text-sm font-medium bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 transition-colors"
            >
              {asignarPermisos.isPending ? 'Guardando...' : 'Guardar permisos'}
            </button>
          </div>

          <div className="overflow-x-auto">
            <table className="min-w-full text-sm">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-5 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider w-40">
                    Módulo
                  </th>
                  {(['puedeLeer', 'puedeEscribir', 'puedeEliminar', 'denegado'] as const).map(
                    col => (
                      <th
                        key={col}
                        className="px-4 py-3 text-center text-xs font-semibold text-gray-500 uppercase tracking-wider"
                      >
                        {col === 'puedeLeer'
                          ? 'Leer'
                          : col === 'puedeEscribir'
                          ? 'Escribir'
                          : col === 'puedeEliminar'
                          ? 'Eliminar'
                          : 'Denegado'}
                      </th>
                    ),
                  )}
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {Object.entries(permisosActivos).map(([modulo, p]) => (
                  <tr key={modulo} className="hover:bg-gray-50 transition-colors">
                    <td className="px-5 py-3">
                      <span className="font-mono text-xs text-gray-700">{modulo}</span>
                    </td>
                    {(['puedeLeer', 'puedeEscribir', 'puedeEliminar', 'denegado'] as const).map(
                      campo => (
                        <td key={campo} className="px-4 py-3 text-center">
                          <input
                            type="checkbox"
                            checked={p[campo]}
                            onChange={() => togglePermiso(modulo, campo)}
                            className={`w-4 h-4 rounded cursor-pointer ${
                              campo === 'denegado'
                                ? 'accent-red-600'
                                : 'accent-blue-600'
                            }`}
                          />
                        </td>
                      ),
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="px-5 py-3 bg-gray-50 border-t border-gray-100">
            <p className="text-xs text-gray-500">
              Los permisos aquí configurados sobreescriben los del rol asignado.{' '}
              <strong>Denegado</strong> cancela cualquier permiso del rol.
            </p>
          </div>
        </div>
      )}

      {/* ── TAB: Actividad ────────────────────────────────────────────────── */}
      {activeTab === 'actividad' && (
        <div className="bg-white rounded-xl border border-gray-200 p-6">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-5">
            <InfoRow
              label="Fecha de alta"
              value={format(new Date(usuario.fechaAlta), 'dd/MM/yyyy HH:mm')}
            />
            <InfoRow
              label="Último acceso"
              value={
                usuario.ultimoAcceso
                  ? format(new Date(usuario.ultimoAcceso), 'dd/MM/yyyy HH:mm')
                  : 'Nunca'
              }
            />
            <InfoRow
              label="Estado de cuenta"
              value={
                <span
                  className={`inline-flex items-center rounded-full text-xs font-medium px-2.5 py-0.5 ${
                    usuario.activo ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'
                  }`}
                >
                  {usuario.activo ? 'Activa' : 'Desactivada'}
                </span>
              }
            />
            <InfoRow
              label="Requiere cambio de contraseña"
              value={
                <span
                  className={`inline-flex items-center rounded-full text-xs font-medium px-2.5 py-0.5 ${
                    usuario.requiereCambioPassword
                      ? 'bg-amber-100 text-amber-700'
                      : 'bg-gray-100 text-gray-600'
                  }`}
                >
                  {usuario.requiereCambioPassword ? 'Sí' : 'No'}
                </span>
              }
            />
          </div>
        </div>
      )}

      {/* ── Dialogs ───────────────────────────────────────────────────────── */}
      {showPasswordDialog && id && (
        <CambiarPasswordDialog idUsuario={id} onClose={() => setShowPasswordDialog(false)} />
      )}

      <ConfirmDialog
        open={confirmEstado}
        title={usuario.activo ? 'Desactivar usuario' : 'Activar usuario'}
        message={
          usuario.activo
            ? `¿Desactivar a ${usuario.nombreCompleto}? Se cerrarán todas sus sesiones activas.`
            : `¿Activar a ${usuario.nombreCompleto}?`
        }
        confirmLabel={usuario.activo ? 'Sí, desactivar' : 'Sí, activar'}
        variant={usuario.activo ? 'danger' : 'primary'}
        isLoading={cambiarEstado.isPending}
        onConfirm={handleConfirmEstado}
        onCancel={() => setConfirmEstado(false)}
      />
    </div>
  )
}

// ── InfoRow helper ────────────────────────────────────────────────────────────

function InfoRow({
  label,
  value,
}: {
  label: string
  value: React.ReactNode
}) {
  return (
    <div>
      <dt className="text-xs font-medium text-gray-500 uppercase tracking-wide">{label}</dt>
      <dd className="mt-1 text-sm text-gray-900">{value}</dd>
    </div>
  )
}
