import { useState, useEffect } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import toast from 'react-hot-toast'
import { Eye, EyeOff, Plus, Save } from 'lucide-react'
import { PageHeader } from '@/components/common/PageHeader'
import { catalogosAdminApi } from '@/api/catalogos-admin.api'
import { catalogosApi } from '@/api/catalogos.api'
import type {
  AseguradoraDetalleDto,
  MedicamentoCatalogoDetalleDto,
  ParametroDto,
  ConfiguracionOcrDto,
  ViaAdministracionDetalleDto,
} from '@/types/catalogos-admin.types'
import type { RolDto } from '@/types/usuario.types'

// ── Shared styles ─────────────────────────────────────────────────────────────

const inputCls =
  'w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-50 disabled:text-gray-400'
const errorCls = 'mt-1 text-xs text-red-500'

// ── Types ─────────────────────────────────────────────────────────────────────

type TabId = 'aseguradoras' | 'medicamentos' | 'parametros' | 'ocr' | 'vias' | 'roles'

interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

// ── useDebounce ───────────────────────────────────────────────────────────────

function useDebounce<T>(value: T, delay: number): T {
  const [debounced, setDebounced] = useState(value)
  useEffect(() => {
    const t = setTimeout(() => setDebounced(value), delay)
    return () => clearTimeout(t)
  }, [value, delay])
  return debounced
}

// ── TabNav ────────────────────────────────────────────────────────────────────

const TABS: { id: TabId; label: string }[] = [
  { id: 'aseguradoras', label: 'Aseguradoras' },
  { id: 'medicamentos', label: 'Medicamentos' },
  { id: 'parametros', label: 'Parámetros' },
  { id: 'ocr', label: 'OCR' },
  { id: 'vias', label: 'Vías Admin' },
  { id: 'roles', label: 'Roles' },
]

function TabNav({ active, onChange }: { active: TabId; onChange: (id: TabId) => void }) {
  return (
    <div className="flex gap-1 border-b border-gray-200 mb-6 overflow-x-auto">
      {TABS.map(t => (
        <button
          key={t.id}
          onClick={() => onChange(t.id)}
          className={`px-4 py-2.5 text-sm font-medium whitespace-nowrap border-b-2 transition-colors ${
            active === t.id
              ? 'border-blue-600 text-blue-600'
              : 'border-transparent text-gray-500 hover:text-gray-700'
          }`}
        >
          {t.label}
        </button>
      ))}
    </div>
  )
}

// ── Dialog wrapper ────────────────────────────────────────────────────────────

function Dialog({
  title,
  onClose,
  children,
}: {
  title: string
  onClose: () => void
  children: React.ReactNode
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/40 backdrop-blur-sm" onClick={onClose} />
      <div className="relative bg-white rounded-xl shadow-xl w-full max-w-md mx-4 p-6 space-y-4 max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between">
          <h3 className="text-base font-semibold text-gray-900">{title}</h3>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 text-xl leading-none">
            ×
          </button>
        </div>
        {children}
      </div>
    </div>
  )
}

// ── Badge ─────────────────────────────────────────────────────────────────────

function Badge({ activo }: { activo: boolean }) {
  return (
    <span
      className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${
        activo ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'
      }`}
    >
      {activo ? 'Activo' : 'Inactivo'}
    </span>
  )
}

// ── Toggle Switch ─────────────────────────────────────────────────────────────

function Switch({
  checked,
  onChange,
  disabled,
}: {
  checked: boolean
  onChange: (v: boolean) => void
  disabled?: boolean
}) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      disabled={disabled}
      onClick={() => onChange(!checked)}
      className={`relative inline-flex h-5 w-9 rounded-full transition-colors focus:outline-none ${
        checked ? 'bg-blue-600' : 'bg-gray-300'
      } ${disabled ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer'}`}
    >
      <span
        className={`inline-block h-4 w-4 rounded-full bg-white shadow transform transition-transform mt-0.5 ${
          checked ? 'translate-x-4' : 'translate-x-0.5'
        }`}
      />
    </button>
  )
}

// ── Field ─────────────────────────────────────────────────────────────────────

function Field({
  label,
  error,
  children,
}: {
  label: string
  error?: string
  children: React.ReactNode
}) {
  return (
    <div>
      <label className="block text-sm font-medium text-gray-700 mb-1">{label}</label>
      {children}
      {error && <p className={errorCls}>{error}</p>}
    </div>
  )
}

// ═══════════════════════════════════════════════════════════════════════════════
// TAB — Aseguradoras
// ═══════════════════════════════════════════════════════════════════════════════

const rfcRegex = /^[A-ZÑ&]{3,4}\d{6}[A-Z\d]{3}$/

const aseguradoraSchema = z.object({
  nombre: z.string().min(1, 'Requerido'),
  clave: z.string().min(1, 'Requerido'),
  razonSocial: z.string().min(1, 'Requerido'),
  rfc: z.string().regex(rfcRegex, 'RFC inválido (ej: XAXX010101000)'),
  idAseguradoraPadre: z.number().nullable().optional(),
})

type AseguradoraForm = z.infer<typeof aseguradoraSchema>

function AseguradoraDialog({
  editing,
  raices,
  onClose,
  onSaved,
}: {
  editing: AseguradoraDetalleDto | null
  raices: AseguradoraDetalleDto[]
  onClose: () => void
  onSaved: () => void
}) {
  const qc = useQueryClient()
  const isEdit = editing !== null

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    formState: { errors },
  } = useForm<AseguradoraForm>({
    resolver: zodResolver(aseguradoraSchema),
    defaultValues: editing
      ? {
          nombre: editing.nombre,
          clave: editing.clave,
          razonSocial: editing.razonSocial,
          rfc: editing.rfc,
          idAseguradoraPadre: editing.idAseguradoraPadre,
        }
      : { idAseguradoraPadre: null },
  })

  const crear = useMutation({
    mutationFn: (dto: AseguradoraForm) =>
      catalogosAdminApi.crearAseguradora({
        nombre: dto.nombre,
        clave: dto.clave,
        razonSocial: dto.razonSocial,
        rfc: dto.rfc,
        idAseguradoraPadre: dto.idAseguradoraPadre ?? undefined,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin', 'aseguradoras'] })
      toast.success('Aseguradora creada')
      onSaved()
    },
    onError: () => toast.error('Error al guardar'),
  })

  const actualizar = useMutation({
    mutationFn: (dto: AseguradoraForm) =>
      catalogosAdminApi.actualizarAseguradora(editing!.id, {
        nombre: dto.nombre,
        razonSocial: dto.razonSocial,
        rfc: dto.rfc,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin', 'aseguradoras'] })
      toast.success('Aseguradora actualizada')
      onSaved()
    },
    onError: () => toast.error('Error al guardar'),
  })

  const onSubmit = (data: AseguradoraForm) => {
    if (isEdit) actualizar.mutate(data)
    else crear.mutate(data)
  }

  const isPending = crear.isPending || actualizar.isPending
  const padreValue = watch('idAseguradoraPadre')

  return (
    <Dialog title={isEdit ? 'Editar aseguradora' : 'Nueva aseguradora'} onClose={onClose}>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
        {!isEdit && (
          <Field label="Clave" error={errors.clave?.message}>
            <input
              {...register('clave', {
                onChange: e => setValue('clave', e.target.value.toUpperCase()),
              })}
              className={inputCls}
              placeholder="ASEG"
            />
          </Field>
        )}
        <Field label="Nombre" error={errors.nombre?.message}>
          <input {...register('nombre')} className={inputCls} />
        </Field>
        <Field label="Razón social" error={errors.razonSocial?.message}>
          <input {...register('razonSocial')} className={inputCls} />
        </Field>
        <Field label="RFC" error={errors.rfc?.message}>
          <input
            {...register('rfc', {
              onChange: e => setValue('rfc', e.target.value.toUpperCase()),
            })}
            className={inputCls}
            placeholder="XAXX010101000"
          />
        </Field>
        {!isEdit && (
          <Field label="Aseguradora padre (opcional)">
            <select
              className={inputCls}
              value={padreValue ?? ''}
              onChange={e =>
                setValue(
                  'idAseguradoraPadre',
                  e.target.value === '' ? null : Number(e.target.value),
                )
              }
            >
              <option value="">Sin padre (raíz)</option>
              {raices.map(r => (
                <option key={r.id} value={r.id}>
                  {r.nombre} ({r.clave})
                </option>
              ))}
            </select>
          </Field>
        )}
        <div className="flex justify-end gap-2 pt-2">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 text-sm border rounded-lg text-gray-600 hover:bg-gray-50"
          >
            Cancelar
          </button>
          <button
            type="submit"
            disabled={isPending}
            className="px-4 py-2 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50"
          >
            {isPending ? 'Guardando...' : 'Guardar'}
          </button>
        </div>
      </form>
    </Dialog>
  )
}

function TabAseguradoras() {
  const qc = useQueryClient()
  const [dialog, setDialog] = useState<'new' | AseguradoraDetalleDto | null>(null)

  const { data: aseguradoras = [], isLoading } = useQuery({
    queryKey: ['admin', 'aseguradoras'],
    queryFn: catalogosAdminApi.getAseguradoras,
  })

  const toggleActivo = useMutation({
    mutationFn: (a: AseguradoraDetalleDto) =>
      catalogosAdminApi.actualizarAseguradora(a.id, { activo: !a.activo }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['admin', 'aseguradoras'] }),
    onError: () => toast.error('Error al cambiar estado'),
  })

  const raices = aseguradoras.filter(a => a.idAseguradoraPadre === null)

  return (
    <div className="space-y-4">
      <div className="flex justify-end">
        <button
          onClick={() => setDialog('new')}
          className="inline-flex items-center gap-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium px-4 py-2 rounded-lg"
        >
          <Plus className="w-4 h-4" /> Nueva Aseguradora
        </button>
      </div>

      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-gray-50">
            <tr>
              {['Clave', 'Nombre', 'RFC', 'Padre', 'Estado', 'Acciones'].map(h => (
                <th
                  key={h}
                  className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                >
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {isLoading ? (
              <tr>
                <td colSpan={6} className="px-4 py-8 text-center text-gray-400">
                  Cargando...
                </td>
              </tr>
            ) : aseguradoras.length === 0 ? (
              <tr>
                <td colSpan={6} className="px-4 py-8 text-center text-gray-400">
                  Sin registros
                </td>
              </tr>
            ) : (
              aseguradoras.map(a => (
                <tr key={a.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 font-mono text-xs">{a.clave}</td>
                  <td className="px-4 py-3 font-medium">{a.nombre}</td>
                  <td className="px-4 py-3 text-gray-600">{a.rfc}</td>
                  <td className="px-4 py-3 text-gray-500">{a.nombrePadre ?? 'Raíz'}</td>
                  <td className="px-4 py-3">
                    <button onClick={() => toggleActivo.mutate(a)} title="Cambiar estado">
                      <Badge activo={a.activo} />
                    </button>
                  </td>
                  <td className="px-4 py-3">
                    <button
                      onClick={() => setDialog(a)}
                      className="text-blue-600 hover:text-blue-800 text-xs font-medium"
                    >
                      Editar
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {dialog !== null && (
        <AseguradoraDialog
          editing={dialog === 'new' ? null : dialog}
          raices={raices}
          onClose={() => setDialog(null)}
          onSaved={() => setDialog(null)}
        />
      )}
    </div>
  )
}

// ═══════════════════════════════════════════════════════════════════════════════
// TAB — Medicamentos
// ═══════════════════════════════════════════════════════════════════════════════

const medicamentoSchema = z.object({
  nombreComercial: z.string().min(1, 'Requerido'),
  sustanciaActiva: z.string().optional(),
  presentacion: z.string().optional(),
  codigoEAN: z
    .string()
    .refine(v => !v || /^\d{13}$/.test(v), 'Debe ser 13 dígitos')
    .optional(),
  claveSAT: z.string().optional(),
  activo: z.boolean().optional(),
})

type MedicamentoForm = z.infer<typeof medicamentoSchema>

function MedicamentoDialog({
  editing,
  onClose,
  onSaved,
}: {
  editing: MedicamentoCatalogoDetalleDto | null
  onClose: () => void
  onSaved: () => void
}) {
  const qc = useQueryClient()
  const isEdit = editing !== null

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    formState: { errors },
  } = useForm<MedicamentoForm>({
    resolver: zodResolver(medicamentoSchema),
    defaultValues: editing
      ? {
          nombreComercial: editing.nombreComercial,
          sustanciaActiva: editing.sustanciaActiva ?? '',
          presentacion: editing.presentacion ?? '',
          codigoEAN: editing.codigoEAN ?? '',
          claveSAT: editing.claveSAT ?? '',
          activo: editing.activo,
        }
      : { claveSAT: '51101500', activo: true },
  })

  const activoValue = watch('activo')

  const crear = useMutation({
    mutationFn: (dto: MedicamentoForm) =>
      catalogosAdminApi.crearMedicamento({
        nombreComercial: dto.nombreComercial,
        sustanciaActiva: dto.sustanciaActiva || undefined,
        presentacion: dto.presentacion || undefined,
        codigoEAN: dto.codigoEAN || undefined,
        claveSAT: dto.claveSAT || undefined,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin', 'medicamentos'] })
      toast.success('Medicamento creado')
      onSaved()
    },
    onError: () => toast.error('Error al guardar'),
  })

  const actualizar = useMutation({
    mutationFn: (dto: MedicamentoForm) =>
      catalogosAdminApi.actualizarMedicamento(editing!.id, {
        nombreComercial: dto.nombreComercial,
        sustanciaActiva: dto.sustanciaActiva || undefined,
        presentacion: dto.presentacion || undefined,
        codigoEAN: dto.codigoEAN || undefined,
        claveSAT: dto.claveSAT || undefined,
        activo: dto.activo,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin', 'medicamentos'] })
      toast.success('Medicamento actualizado')
      onSaved()
    },
    onError: () => toast.error('Error al guardar'),
  })

  const onSubmit = (data: MedicamentoForm) => {
    if (isEdit) actualizar.mutate(data)
    else crear.mutate(data)
  }

  const isPending = crear.isPending || actualizar.isPending

  return (
    <Dialog title={isEdit ? 'Editar medicamento' : 'Nuevo medicamento'} onClose={onClose}>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
        <Field label="Nombre comercial *" error={errors.nombreComercial?.message}>
          <input {...register('nombreComercial')} className={inputCls} />
        </Field>
        <Field label="Sustancia activa" error={errors.sustanciaActiva?.message}>
          <input {...register('sustanciaActiva')} className={inputCls} />
        </Field>
        <Field label="Presentación" error={errors.presentacion?.message}>
          <input {...register('presentacion')} className={inputCls} placeholder="Tabletas 500mg" />
        </Field>
        <Field label="Código EAN (13 dígitos)" error={errors.codigoEAN?.message}>
          <input {...register('codigoEAN')} className={inputCls} maxLength={13} />
        </Field>
        <Field label="Clave SAT" error={errors.claveSAT?.message}>
          <input {...register('claveSAT')} className={inputCls} placeholder="51101500" />
        </Field>
        {isEdit && (
          <div className="flex items-center justify-between py-2">
            <span className="text-sm font-medium text-gray-700">Activo</span>
            <Switch checked={activoValue ?? true} onChange={v => setValue('activo', v)} />
          </div>
        )}
        <div className="flex justify-end gap-2 pt-2">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 text-sm border rounded-lg text-gray-600 hover:bg-gray-50"
          >
            Cancelar
          </button>
          <button
            type="submit"
            disabled={isPending}
            className="px-4 py-2 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50"
          >
            {isPending ? 'Guardando...' : 'Guardar'}
          </button>
        </div>
      </form>
    </Dialog>
  )
}

function TabMedicamentos() {
  const [busqueda, setBusqueda] = useState('')
  const [page, setPage] = useState(1)
  const [dialog, setDialog] = useState<'new' | MedicamentoCatalogoDetalleDto | null>(null)
  const debouncedBusqueda = useDebounce(busqueda, 400)

  const { data, isLoading } = useQuery({
    queryKey: ['admin', 'medicamentos', debouncedBusqueda, page],
    queryFn: () => catalogosAdminApi.getMedicamentos(debouncedBusqueda || undefined, page, 20),
  })

  const result = data as PagedResult<MedicamentoCatalogoDetalleDto> | undefined

  useEffect(() => {
    setPage(1)
  }, [debouncedBusqueda])

  return (
    <div className="space-y-4">
      <div className="flex gap-3">
        <input
          value={busqueda}
          onChange={e => setBusqueda(e.target.value)}
          placeholder="Buscar medicamento..."
          className={`${inputCls} max-w-xs`}
        />
        <button
          onClick={() => setDialog('new')}
          className="inline-flex items-center gap-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium px-4 py-2 rounded-lg ml-auto"
        >
          <Plus className="w-4 h-4" /> Nuevo Medicamento
        </button>
      </div>

      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-gray-50">
            <tr>
              {[
                'Nombre comercial',
                'Sustancia activa',
                'Presentación',
                'Clave SAT',
                'EAN',
                'Estado',
                '',
              ].map(h => (
                <th
                  key={h}
                  className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                >
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {isLoading ? (
              <tr>
                <td colSpan={7} className="px-4 py-8 text-center text-gray-400">
                  Cargando...
                </td>
              </tr>
            ) : !result?.items?.length ? (
              <tr>
                <td colSpan={7} className="px-4 py-8 text-center text-gray-400">
                  Sin resultados
                </td>
              </tr>
            ) : (
              result.items.map(m => (
                <tr key={m.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 font-medium">{m.nombreComercial}</td>
                  <td className="px-4 py-3 text-gray-600">{m.sustanciaActiva ?? '—'}</td>
                  <td className="px-4 py-3 text-gray-600">{m.presentacion ?? '—'}</td>
                  <td className="px-4 py-3 font-mono text-xs">{m.claveSAT ?? '—'}</td>
                  <td className="px-4 py-3 font-mono text-xs">{m.codigoEAN ?? '—'}</td>
                  <td className="px-4 py-3">
                    <Badge activo={m.activo} />
                  </td>
                  <td className="px-4 py-3">
                    <button
                      onClick={() => setDialog(m)}
                      className="text-blue-600 hover:text-blue-800 text-xs font-medium"
                    >
                      Editar
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {result && result.total > 20 && (
        <div className="flex items-center justify-between text-sm text-gray-600">
          <span>{result.total} resultados</span>
          <div className="flex gap-2">
            <button
              disabled={page === 1}
              onClick={() => setPage(p => p - 1)}
              className="px-3 py-1 border rounded disabled:opacity-40 hover:bg-gray-50"
            >
              ← Anterior
            </button>
            <span className="px-3 py-1">Pág. {page}</span>
            <button
              disabled={page * 20 >= result.total}
              onClick={() => setPage(p => p + 1)}
              className="px-3 py-1 border rounded disabled:opacity-40 hover:bg-gray-50"
            >
              Siguiente →
            </button>
          </div>
        </div>
      )}

      {dialog !== null && (
        <MedicamentoDialog
          editing={dialog === 'new' ? null : dialog}
          onClose={() => setDialog(null)}
          onSaved={() => setDialog(null)}
        />
      )}
    </div>
  )
}

// ═══════════════════════════════════════════════════════════════════════════════
// TAB — Parámetros
// ═══════════════════════════════════════════════════════════════════════════════

function ParametroRow({ param }: { param: ParametroDto }) {
  const qc = useQueryClient()
  const [localValor, setLocalValor] = useState(param.valor)
  const isDirty = localValor !== param.valor

  const actualizar = useMutation({
    mutationFn: () => catalogosAdminApi.actualizarParametro(param.clave, localValor),
    onSuccess: () => {
      toast.success(`Parámetro ${param.clave} actualizado`)
      qc.invalidateQueries({ queryKey: ['admin', 'parametros'] })
    },
    onError: () => toast.error('Error al guardar'),
  })

  const tipo = param.tipoDato.toUpperCase()

  return (
    <tr className="hover:bg-gray-50">
      <td className="px-4 py-3">
        <span className="font-mono text-xs bg-gray-100 px-2 py-1 rounded">{param.clave}</span>
      </td>
      <td className="px-4 py-3 text-gray-600 max-w-xs text-sm">{param.descripcion}</td>
      <td className="px-4 py-3">
        <span className="text-xs bg-blue-50 text-blue-700 px-2 py-0.5 rounded">
          {param.tipoDato}
        </span>
      </td>
      <td className="px-4 py-3">
        {tipo === 'BOOLEAN' ? (
          <Switch
            checked={localValor === 'true' || localValor === '1'}
            onChange={v => setLocalValor(v ? 'true' : 'false')}
          />
        ) : (
          <input
            type={tipo === 'INT' || tipo === 'DECIMAL' ? 'number' : 'text'}
            value={localValor}
            onChange={e => setLocalValor(e.target.value)}
            className="border border-gray-300 rounded px-2 py-1 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 w-40"
          />
        )}
      </td>
      <td className="px-4 py-3">
        {isDirty && (
          <button
            onClick={() => actualizar.mutate()}
            disabled={actualizar.isPending}
            className="inline-flex items-center gap-1 text-xs bg-green-600 text-white px-2 py-1 rounded hover:bg-green-700 disabled:opacity-50"
          >
            <Save className="w-3 h-3" />
            {actualizar.isPending ? '...' : 'Guardar'}
          </button>
        )}
      </td>
    </tr>
  )
}

function TabParametros() {
  const { data: parametros = [], isLoading } = useQuery({
    queryKey: ['admin', 'parametros'],
    queryFn: catalogosAdminApi.getParametros,
  })

  return (
    <div className="space-y-4">
      <div className="bg-amber-50 border border-amber-200 rounded-lg px-4 py-3 text-sm text-amber-800">
        ⚠ Cambiar estos valores afecta el comportamiento del sistema. Los cambios aplican en
        menos de 5 minutos (caché).
      </div>
      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-gray-50">
            <tr>
              {['Clave', 'Descripción', 'Tipo', 'Valor', 'Guardar'].map(h => (
                <th
                  key={h}
                  className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                >
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {isLoading ? (
              <tr>
                <td colSpan={5} className="px-4 py-8 text-center text-gray-400">
                  Cargando...
                </td>
              </tr>
            ) : parametros.length === 0 ? (
              <tr>
                <td colSpan={5} className="px-4 py-8 text-center text-gray-400">
                  Sin parámetros
                </td>
              </tr>
            ) : (
              parametros.map(p => <ParametroRow key={p.clave} param={p} />)
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}

// ═══════════════════════════════════════════════════════════════════════════════
// TAB — Configuración OCR
// ═══════════════════════════════════════════════════════════════════════════════

function OcrDialog({
  config,
  onClose,
  onSaved,
}: {
  config: ConfiguracionOcrDto
  onClose: () => void
  onSaved: () => void
}) {
  const qc = useQueryClient()
  const [showKey, setShowKey] = useState(false)
  const [esPrincipal, setEsPrincipal] = useState(config.esPrincipal)
  const [activo, setActivo] = useState(config.activo)
  const [apiKey, setApiKey] = useState('')
  const [configJson, setConfigJson] = useState(config.configJson ?? '')
  const [saving, setSaving] = useState(false)

  async function handleSave() {
    setSaving(true)
    try {
      await catalogosAdminApi.actualizarConfiguracionOcr(config.id, {
        nombre: config.nombre,
        urlBase: config.urlBase,
        apiKey: apiKey || undefined,
        esPrincipal,
        activo,
        configJson: configJson || undefined,
      })
      qc.invalidateQueries({ queryKey: ['admin', 'config-ocr'] })
      toast.success('Configuración OCR actualizada')
      onSaved()
    } catch {
      toast.error('Error al guardar')
    } finally {
      setSaving(false)
    }
  }

  return (
    <Dialog title={`Editar: ${config.nombre}`} onClose={onClose}>
      <div className="space-y-3">
        <div className="bg-gray-50 rounded-lg p-3 text-sm space-y-1">
          <p>
            <span className="font-medium">Nombre:</span> {config.nombre}
          </p>
          <p>
            <span className="font-medium">URL:</span> {config.urlBase}
          </p>
          <p>
            <span className="font-medium">Proveedor:</span> {config.proveedor}
          </p>
        </div>

        <Field label="Nueva API Key (opcional)">
          <div className="relative">
            <input
              type={showKey ? 'text' : 'password'}
              value={apiKey}
              onChange={e => setApiKey(e.target.value)}
              placeholder="Dejar vacío para no cambiar"
              className={`${inputCls} pr-10`}
            />
            <button
              type="button"
              onClick={() => setShowKey(v => !v)}
              className="absolute right-2 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600"
            >
              {showKey ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
            </button>
          </div>
        </Field>

        <div className="flex items-center justify-between py-1">
          <span className="text-sm font-medium text-gray-700">Es principal</span>
          <Switch checked={esPrincipal} onChange={setEsPrincipal} />
        </div>

        <div className="flex items-center justify-between py-1">
          <span className="text-sm font-medium text-gray-700">Activo</span>
          <Switch checked={activo} onChange={setActivo} />
        </div>

        <Field label="Config JSON (opcional)">
          <textarea
            value={configJson}
            onChange={e => setConfigJson(e.target.value)}
            rows={3}
            className={inputCls}
            placeholder='{"modelo": "gemini-pro"}'
          />
        </Field>

        <div className="flex justify-end gap-2 pt-2">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 text-sm border rounded-lg text-gray-600 hover:bg-gray-50"
          >
            Cancelar
          </button>
          <button
            onClick={handleSave}
            disabled={saving}
            className="px-4 py-2 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50"
          >
            {saving ? 'Guardando...' : 'Guardar'}
          </button>
        </div>
      </div>
    </Dialog>
  )
}

function TabConfigOcr() {
  const [editing, setEditing] = useState<ConfiguracionOcrDto | null>(null)

  const { data: configs = [], isLoading } = useQuery({
    queryKey: ['admin', 'config-ocr'],
    queryFn: catalogosAdminApi.getConfiguracionesOcr,
  })

  return (
    <div className="space-y-4">
      {isLoading ? (
        <div className="text-center py-8 text-gray-400">Cargando...</div>
      ) : configs.length === 0 ? (
        <div className="text-center py-8 text-gray-400">Sin configuraciones</div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {configs.map(c => (
            <div key={c.id} className="bg-white rounded-xl border border-gray-200 p-5 space-y-2.5">
              <div className="flex items-start justify-between gap-2">
                <div className="flex items-center gap-2 flex-wrap">
                  {c.esPrincipal && (
                    <span className="inline-flex items-center gap-1 bg-amber-100 text-amber-700 text-xs font-medium px-2 py-0.5 rounded-full">
                      ★ Principal
                    </span>
                  )}
                  <h3 className="font-semibold text-gray-900">{c.nombre}</h3>
                </div>
                <Badge activo={c.activo} />
              </div>
              <div className="text-sm text-gray-600 space-y-1">
                <p>
                  <span className="font-medium">URL:</span> {c.urlBase}
                </p>
                <p>
                  <span className="font-medium">API Key:</span>{' '}
                  <span className="font-mono">{c.apiKeyParcial}</span>
                </p>
                <p>
                  <span className="font-medium">Proveedor:</span> {c.proveedor}
                </p>
              </div>
              <div className="pt-1">
                <button
                  onClick={() => setEditing(c)}
                  className="text-sm text-blue-600 hover:text-blue-800 font-medium"
                >
                  Editar
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {editing && (
        <OcrDialog
          config={editing}
          onClose={() => setEditing(null)}
          onSaved={() => setEditing(null)}
        />
      )}
    </div>
  )
}

// ═══════════════════════════════════════════════════════════════════════════════
// TAB — Vías de Administración
// ═══════════════════════════════════════════════════════════════════════════════

const viaSchema = z.object({
  clave: z.string().min(1, 'Requerido'),
  nombre: z.string().min(1, 'Requerido'),
})

type ViaForm = z.infer<typeof viaSchema>

function ViaDialog({
  editing,
  onClose,
  onSaved,
}: {
  editing: ViaAdministracionDetalleDto | null
  onClose: () => void
  onSaved: () => void
}) {
  const qc = useQueryClient()
  const isEdit = editing !== null

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors },
  } = useForm<ViaForm>({
    resolver: zodResolver(viaSchema),
    defaultValues: editing ? { clave: editing.clave, nombre: editing.nombre } : {},
  })

  const crear = useMutation({
    mutationFn: (dto: ViaForm) => catalogosAdminApi.crearVia(dto.clave, dto.nombre),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin', 'vias'] })
      toast.success('Vía creada')
      onSaved()
    },
    onError: () => toast.error('Error al guardar'),
  })

  const actualizar = useMutation({
    mutationFn: (dto: ViaForm) =>
      catalogosAdminApi.actualizarVia(editing!.id, dto.nombre, editing!.activo),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin', 'vias'] })
      toast.success('Vía actualizada')
      onSaved()
    },
    onError: () => toast.error('Error al guardar'),
  })

  const onSubmit = (data: ViaForm) => {
    if (isEdit) actualizar.mutate(data)
    else crear.mutate(data)
  }

  const isPending = crear.isPending || actualizar.isPending

  return (
    <Dialog title={isEdit ? 'Editar vía' : 'Nueva vía de administración'} onClose={onClose}>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
        <Field label="Clave" error={errors.clave?.message}>
          <input
            {...register('clave', {
              onChange: e => setValue('clave', e.target.value.toUpperCase()),
            })}
            disabled={isEdit}
            className={inputCls}
            placeholder="IV"
          />
        </Field>
        <Field label="Nombre" error={errors.nombre?.message}>
          <input {...register('nombre')} className={inputCls} placeholder="Intravenosa" />
        </Field>
        <div className="flex justify-end gap-2 pt-2">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 text-sm border rounded-lg text-gray-600 hover:bg-gray-50"
          >
            Cancelar
          </button>
          <button
            type="submit"
            disabled={isPending}
            className="px-4 py-2 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50"
          >
            {isPending ? 'Guardando...' : 'Guardar'}
          </button>
        </div>
      </form>
    </Dialog>
  )
}

function TabViasAdmin() {
  const qc = useQueryClient()
  const [dialog, setDialog] = useState<'new' | ViaAdministracionDetalleDto | null>(null)

  const { data: vias = [], isLoading } = useQuery({
    queryKey: ['admin', 'vias'],
    queryFn: catalogosAdminApi.getVias,
  })

  const toggleActivo = useMutation({
    mutationFn: (v: ViaAdministracionDetalleDto) =>
      catalogosAdminApi.actualizarVia(v.id, v.nombre, !v.activo),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['admin', 'vias'] }),
    onError: () => toast.error('Error al cambiar estado'),
  })

  return (
    <div className="space-y-4">
      <div className="flex justify-end">
        <button
          onClick={() => setDialog('new')}
          className="inline-flex items-center gap-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium px-4 py-2 rounded-lg"
        >
          <Plus className="w-4 h-4" /> Nueva vía
        </button>
      </div>

      {isLoading ? (
        <div className="text-center py-8 text-gray-400">Cargando...</div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          {vias.map(v => (
            <div
              key={v.id}
              className="bg-white rounded-lg border border-gray-200 px-4 py-3 flex items-center gap-3"
            >
              <span className="font-mono text-xs bg-gray-100 px-2 py-1 rounded shrink-0">
                {v.clave}
              </span>
              <span className="flex-1 text-sm font-medium">{v.nombre}</span>
              <button onClick={() => toggleActivo.mutate(v)} title="Cambiar estado">
                <Badge activo={v.activo} />
              </button>
              <button
                onClick={() => setDialog(v)}
                className="text-blue-600 hover:text-blue-800 text-xs font-medium"
              >
                Editar
              </button>
            </div>
          ))}
        </div>
      )}

      {dialog !== null && (
        <ViaDialog
          editing={dialog === 'new' ? null : dialog}
          onClose={() => setDialog(null)}
          onSaved={() => setDialog(null)}
        />
      )}
    </div>
  )
}

// ═══════════════════════════════════════════════════════════════════════════════
// TAB — Roles y Módulos
// ═══════════════════════════════════════════════════════════════════════════════

function TabRolesModulos() {
  const { data: roles = [], isLoading: loadingRoles } = useQuery({
    queryKey: ['catalogos', 'roles'],
    queryFn: catalogosApi.getRoles,
  })

  const { data: modulos = [], isLoading: loadingModulos } = useQuery({
    queryKey: ['admin', 'modulos'],
    queryFn: catalogosAdminApi.getModulos,
  })

  return (
    <div className="space-y-6">
      <div className="bg-blue-50 border border-blue-200 rounded-lg px-4 py-3 text-sm text-blue-800">
        Los permisos individuales se asignan en la sección{' '}
        <strong>Usuarios → Permisos</strong>. Los roles definen el conjunto base de permisos
        heredados.
      </div>

      <div>
        <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-3">
          Roles
        </h3>
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50">
              <tr>
                {['ID', 'Clave', 'Nombre', 'Descripción'].map(h => (
                  <th
                    key={h}
                    className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                  >
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {loadingRoles ? (
                <tr>
                  <td colSpan={4} className="px-4 py-8 text-center text-gray-400">
                    Cargando...
                  </td>
                </tr>
              ) : roles.length === 0 ? (
                <tr>
                  <td colSpan={4} className="px-4 py-8 text-center text-gray-400">
                    Sin roles
                  </td>
                </tr>
              ) : (
                (roles as RolDto[]).map(r => (
                  <tr key={r.id} className="hover:bg-gray-50">
                    <td className="px-4 py-3 text-gray-500 font-mono text-xs">{r.id}</td>
                    <td className="px-4 py-3 font-mono text-xs font-medium">{r.clave}</td>
                    <td className="px-4 py-3">{r.nombre}</td>
                    <td className="px-4 py-3 text-gray-600 max-w-xs">{r.descripcion}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      <div>
        <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-3">
          Módulos del sistema
        </h3>
        {loadingModulos ? (
          <div className="text-gray-400 text-sm">Cargando...</div>
        ) : (
          <div className="flex flex-wrap gap-2">
            {modulos.map(m => (
              <span
                key={m.modulo}
                className="inline-flex items-center px-3 py-1 rounded-full text-xs font-mono font-medium bg-gray-100 text-gray-700 border border-gray-200"
              >
                {m.modulo}
              </span>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}

// ═══════════════════════════════════════════════════════════════════════════════
// PAGE
// ═══════════════════════════════════════════════════════════════════════════════

export default function AdminCatalogosPage() {
  const [tab, setTab] = useState<TabId>('aseguradoras')

  return (
    <div className="p-6 space-y-6">
      <PageHeader
        title="Administración de Catálogos"
        subtitle="Gestiona aseguradoras, medicamentos, parámetros del sistema y configuración OCR"
      />
      <TabNav active={tab} onChange={setTab} />
      {tab === 'aseguradoras' && <TabAseguradoras />}
      {tab === 'medicamentos' && <TabMedicamentos />}
      {tab === 'parametros' && <TabParametros />}
      {tab === 'ocr' && <TabConfigOcr />}
      {tab === 'vias' && <TabViasAdmin />}
      {tab === 'roles' && <TabRolesModulos />}
    </div>
  )
}
