import { useState } from 'react'
import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { ArrowLeft, Copy, Check, AlertTriangle } from 'lucide-react'
import { catalogosApi } from '@/api/catalogos.api'
import { PageHeader } from '@/components/common/PageHeader'
import { useCrearUsuario } from '@/hooks/useUsuarios'
import type { CrearUsuarioResponseDto } from '@/types/usuario.types'

// ── Schema ────────────────────────────────────────────────────────────────────

const schema = z.object({
  username: z
    .string()
    .min(3, 'Mínimo 3 caracteres')
    .max(50, 'Máximo 50 caracteres')
    .regex(/^[a-zA-Z0-9._]+$/, 'Solo letras, números, puntos y guiones bajos'),
  email: z.string().email('Email inválido').max(200),
  nombreCompleto: z.string().min(2, 'Mínimo 2 caracteres').max(200),
  apellidoPaterno: z.string().optional(),
  apellidoMaterno: z.string().optional(),
  idRol: z.number({ required_error: 'Selecciona un rol', invalid_type_error: 'Selecciona un rol' }).positive('Selecciona un rol'),
})

type FormData = z.infer<typeof schema>

const inputClass =
  'w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-50 disabled:text-gray-400'

const errorClass = 'mt-1 text-xs text-red-500'

// ── Password Dialog ───────────────────────────────────────────────────────────

function PasswordDialog({
  result,
  onIrDetalle,
  onCrearOtro,
}: {
  result: CrearUsuarioResponseDto
  onIrDetalle: () => void
  onCrearOtro: () => void
}) {
  const [copied, setCopied] = useState(false)

  function handleCopiar() {
    navigator.clipboard.writeText(result.passwordTemporal)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/40 backdrop-blur-sm" />
      <div className="relative w-full max-w-md mx-4 bg-white rounded-xl shadow-xl">
        {/* Header */}
        <div className="flex items-center gap-3 p-5 border-b border-gray-100">
          <span className="inline-flex items-center justify-center w-9 h-9 rounded-full bg-green-100">
            <Check className="w-5 h-5 text-green-600" />
          </span>
          <div>
            <h3 className="text-base font-semibold text-gray-900">Usuario creado</h3>
            <p className="text-xs text-gray-500">{result.usuario.username} — {result.usuario.nombreRol}</p>
          </div>
        </div>

        {/* Password */}
        <div className="p-5 space-y-4">
          <div>
            <p className="text-sm font-medium text-gray-700 mb-2">Contraseña temporal:</p>
            <div className="flex items-center gap-2 bg-gray-50 border border-gray-200 rounded-lg px-3 py-2">
              <code className="flex-1 text-base font-mono font-bold tracking-widest text-gray-900">
                {result.passwordTemporal}
              </code>
              <button
                onClick={handleCopiar}
                className="inline-flex items-center gap-1 text-xs text-blue-600 hover:text-blue-800 font-medium transition-colors"
              >
                {copied ? (
                  <Check className="w-4 h-4 text-green-600" />
                ) : (
                  <Copy className="w-4 h-4" />
                )}
                {copied ? 'Copiado' : 'Copiar'}
              </button>
            </div>
          </div>

          <div className="flex items-start gap-2 bg-amber-50 border border-amber-200 rounded-lg p-3">
            <AlertTriangle className="w-4 h-4 text-amber-600 shrink-0 mt-0.5" />
            <p className="text-xs text-amber-700">
              Comparte esta contraseña de forma segura. El usuario deberá cambiarla al ingresar por primera vez.
            </p>
          </div>
        </div>

        {/* Actions */}
        <div className="flex justify-end gap-3 px-5 pb-5">
          <button
            onClick={onCrearOtro}
            className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
          >
            Crear otro
          </button>
          <button
            onClick={onIrDetalle}
            className="px-4 py-2 text-sm font-medium bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
          >
            Ir al detalle
          </button>
        </div>
      </div>
    </div>
  )
}

// ── Page ──────────────────────────────────────────────────────────────────────

export default function NuevoUsuarioPage() {
  const navigate = useNavigate()
  const crearUsuario = useCrearUsuario()
  const [createdResult, setCreatedResult] = useState<CrearUsuarioResponseDto | null>(null)

  const { data: roles, isLoading: loadingRoles } = useQuery({
    queryKey: ['catalogos', 'roles'],
    queryFn: catalogosApi.getRoles,
    staleTime: 1000 * 60 * 5,
  })

  const {
    register,
    handleSubmit,
    control,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormData>({ resolver: zodResolver(schema) })

  async function onSubmit(data: FormData) {
    try {
      const result = await crearUsuario.mutateAsync({
        username: data.username,
        email: data.email,
        nombreCompleto: data.nombreCompleto,
        apellidoPaterno: data.apellidoPaterno || undefined,
        apellidoMaterno: data.apellidoMaterno || undefined,
        idRol: data.idRol,
      })
      setCreatedResult(result)
    } catch {
      // error surfaced by onError in useMutation
    }
  }

  function handleIrDetalle() {
    if (createdResult) navigate(`/usuarios/${createdResult.usuario.id}`)
  }

  function handleCrearOtro() {
    setCreatedResult(null)
    reset()
  }

  return (
    <div className="p-6 max-w-2xl mx-auto">
      <PageHeader
        title="Nuevo Usuario"
        subtitle="Crea un nuevo usuario con contraseña temporal"
        actions={
          <button
            onClick={() => navigate(-1)}
            className="inline-flex items-center gap-2 text-gray-600 hover:text-gray-900 text-sm"
          >
            <ArrowLeft className="w-4 h-4" />
            Volver
          </button>
        }
      />

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
        {/* Sección: Datos de acceso */}
        <div className="bg-white rounded-xl border border-gray-200 p-6">
          <h2 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-4">
            Datos de acceso
          </h2>
          <div className="space-y-4">
            {/* Username */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Username <span className="text-red-500">*</span>
              </label>
              <input
                {...register('username')}
                type="text"
                placeholder="ej. jperez"
                className={inputClass}
              />
              {errors.username && <p className={errorClass}>{errors.username.message}</p>}
            </div>

            {/* Email */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Email <span className="text-red-500">*</span>
              </label>
              <input
                {...register('email')}
                type="email"
                placeholder="ej. jperez@empresa.com"
                className={inputClass}
              />
              {errors.email && <p className={errorClass}>{errors.email.message}</p>}
            </div>

            {/* Rol */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Rol <span className="text-red-500">*</span>
              </label>
              <Controller
                name="idRol"
                control={control}
                render={({ field }) => (
                  <select
                    value={field.value ?? ''}
                    onChange={e => field.onChange(e.target.value ? Number(e.target.value) : undefined)}
                    disabled={loadingRoles}
                    className={inputClass}
                  >
                    <option value="">
                      {loadingRoles ? 'Cargando roles...' : 'Selecciona un rol'}
                    </option>
                    {roles?.map(r => (
                      <option key={r.id} value={r.id}>
                        {r.nombre}
                      </option>
                    ))}
                  </select>
                )}
              />
              {errors.idRol && <p className={errorClass}>{errors.idRol.message}</p>}
            </div>
          </div>
        </div>

        {/* Sección: Datos personales */}
        <div className="bg-white rounded-xl border border-gray-200 p-6">
          <h2 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-4">
            Datos personales
          </h2>
          <div className="space-y-4">
            {/* Nombre completo */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Nombre completo <span className="text-red-500">*</span>
              </label>
              <input
                {...register('nombreCompleto')}
                type="text"
                placeholder="ej. Juan Pérez López"
                className={inputClass}
              />
              {errors.nombreCompleto && (
                <p className={errorClass}>{errors.nombreCompleto.message}</p>
              )}
            </div>

            <div className="grid grid-cols-2 gap-4">
              {/* Apellido paterno */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Apellido paterno
                </label>
                <input
                  {...register('apellidoPaterno')}
                  type="text"
                  placeholder="Opcional"
                  className={inputClass}
                />
              </div>

              {/* Apellido materno */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Apellido materno
                </label>
                <input
                  {...register('apellidoMaterno')}
                  type="text"
                  placeholder="Opcional"
                  className={inputClass}
                />
              </div>
            </div>
          </div>
        </div>

        {/* Actions */}
        <div className="flex justify-end gap-3">
          <button
            type="button"
            onClick={() => navigate(-1)}
            className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
          >
            Cancelar
          </button>
          <button
            type="submit"
            disabled={isSubmitting || crearUsuario.isPending}
            className="px-5 py-2 text-sm font-medium bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 transition-colors"
          >
            {isSubmitting || crearUsuario.isPending ? 'Creando...' : 'Crear usuario'}
          </button>
        </div>
      </form>

      {/* Password dialog */}
      {createdResult && (
        <PasswordDialog
          result={createdResult}
          onIrDetalle={handleIrDetalle}
          onCrearOtro={handleCrearOtro}
        />
      )}
    </div>
  )
}
