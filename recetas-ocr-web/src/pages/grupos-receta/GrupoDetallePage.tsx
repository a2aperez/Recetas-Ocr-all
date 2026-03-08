import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { format } from 'date-fns'
import toast from 'react-hot-toast'
import {
  ArrowLeft, Upload, ClipboardCheck, Receipt,
  Image as ImageIcon, Pill, Clock,
  Pencil, Check, X, FileImage,
} from 'lucide-react'
import { gruposRecetaApi } from '@/api/grupos-receta.api'
import { StatusBadge } from '@/components/common/StatusBadge'
import { usePermisos } from '@/hooks/usePermisos'
import type { GrupoRecetaDetalleDto, ActualizarGrupoDto } from '@/types/grupo-receta.types'
import type { ImagenDto, MedicamentoRecetaDto } from '@/types/imagen.types'

// ─── helpers ────────────────────────────────────────────────────────────────

type TabId = 'imagenes' | 'medicamentos' | 'paciente' | 'historial'

const inputClass =
  'border border-blue-400 rounded px-2 py-1 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 w-full'

// ─── sub-components ──────────────────────────────────────────────────────────

function TabNav({
  tabs,
  active,
  onChange,
}: {
  tabs: { id: TabId; label: string; count?: number }[]
  active: TabId
  onChange: (id: TabId) => void
}) {
  return (
    <div className="flex gap-1 border-b border-gray-200 mb-6">
      {tabs.map(t => (
        <button
          key={t.id}
          onClick={() => onChange(t.id)}
          className={`px-4 py-2 text-sm font-medium whitespace-nowrap border-b-2 transition-colors ${
            active === t.id
              ? 'border-blue-600 text-blue-600'
              : 'border-transparent text-gray-500 hover:text-gray-700'
          }`}
        >
          {t.label}
          {t.count !== undefined && (
            <span
              className={`ml-1.5 text-xs rounded-full px-1.5 py-0.5 ${
                active === t.id ? 'bg-blue-100 text-blue-700' : 'bg-gray-100 text-gray-600'
              }`}
            >
              {t.count}
            </span>
          )}
        </button>
      ))}
    </div>
  )
}

// ─── TAB 1: Imágenes ─────────────────────────────────────────────────────────

function ImagenesTab({
  imagenes,
  idGrupo,
}: {
  imagenes: ImagenDto[]
  idGrupo: string
}) {
  const navigate = useNavigate()
  const { puedeEscribir: puedeSubir } = usePermisos('IMAGENES.SUBIR')

  if (imagenes.length === 0) {
    return (
      <div className="text-center py-16 text-gray-400">
        <FileImage className="w-12 h-12 mx-auto mb-3 opacity-40" />
        <p className="text-sm">No hay imágenes en este grupo</p>
        {puedeSubir && (
          <button
            onClick={() => navigate(`/imagenes/subir?idGrupo=${idGrupo}`)}
            className="mt-4 inline-flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors"
          >
            <Upload className="w-4 h-4" />
            Subir primera imagen
          </button>
        )}
      </div>
    )
  }

  return (
    <div>
      {puedeSubir && (
        <div className="flex justify-end mb-4">
          <button
            onClick={() => navigate(`/imagenes/subir?idGrupo=${idGrupo}`)}
            className="inline-flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors"
          >
            <Upload className="w-4 h-4" />
            Subir imagen
          </button>
        </div>
      )}

      <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-4">
        {imagenes.map(img => (
          <div
            key={img.id}
            className="bg-white rounded-xl border border-gray-200 overflow-hidden hover:shadow-md transition-shadow cursor-pointer"
            onClick={() => navigate(`/imagenes/${img.id}`)}
          >
            <div className="aspect-square bg-gray-100 relative">
              <img
                src={img.urlBlobRaw}
                alt={`Hoja ${img.numeroHoja}`}
                className="w-full h-full object-cover"
                onError={e => {
                  ;(e.target as HTMLImageElement).style.display = 'none'
                }}
              />
              <div className="absolute inset-0 flex items-center justify-center bg-gray-100 -z-0">
                <ImageIcon className="w-8 h-8 text-gray-300" />
              </div>
            </div>
            <div className="p-2">
              <p className="text-xs font-semibold text-gray-700 mb-1">Hoja {img.numeroHoja}</p>
              <StatusBadge estado={img.estadoImagen} size="sm" />
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}

// ─── TAB 2: Medicamentos ──────────────────────────────────────────────────────

function MedicamentosTab({ medicamentos }: { medicamentos: MedicamentoRecetaDto[] }) {
  if (medicamentos.length === 0) {
    return (
      <div className="text-center py-16 text-gray-400">
        <Pill className="w-12 h-12 mx-auto mb-3 opacity-40" />
        <p className="text-sm">Los medicamentos se extraen automáticamente del OCR</p>
      </div>
    )
  }

  return (
    <div className="overflow-x-auto rounded-lg border border-gray-200">
      <table className="min-w-full divide-y divide-gray-200 text-sm">
        <thead className="bg-gray-50">
          <tr>
            {[
              '#', 'Nombre Comercial', 'Sustancia Activa', 'Presentación',
              'Dosis', 'Cantidad', 'Frecuencia', 'Duración', 'Vía',
            ].map(h => (
              <th
                key={h}
                className="px-3 py-2.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider whitespace-nowrap"
              >
                {h}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="bg-white divide-y divide-gray-100">
          {medicamentos.map(m => (
            <tr key={m.id} className="hover:bg-gray-50">
              <td className="px-3 py-2 text-gray-500">{m.numeroPrescripcion}</td>
              <td className="px-3 py-2 font-medium">{m.nombreComercial ?? '—'}</td>
              <td className="px-3 py-2">{m.sustanciaActiva ?? '—'}</td>
              <td className="px-3 py-2">{m.presentacion ?? '—'}</td>
              <td className="px-3 py-2">{m.dosis ?? '—'}</td>
              <td className="px-3 py-2">
                {m.cantidadNumero != null
                  ? `${m.cantidadNumero} (${m.cantidadTexto ?? ''})`
                  : m.cantidadTexto ?? '—'}
              </td>
              <td className="px-3 py-2">{m.frecuenciaExpandida ?? m.frecuenciaTexto ?? '—'}</td>
              <td className="px-3 py-2">
                {m.duracionDias != null ? `${m.duracionDias} días` : m.duracionTexto ?? '—'}
              </td>
              <td className="px-3 py-2">{m.viaAdministracion ?? '—'}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

// ─── TAB 3: Datos del Paciente ────────────────────────────────────────────────

type PacienteField = keyof ActualizarGrupoDto

interface FieldRow {
  key: PacienteField
  label: string
  value: string | null | undefined
}

function PacienteTab({ grupo }: { grupo: GrupoRecetaDetalleDto }) {
  const qc = useQueryClient()
  const { puedeLeer: puedeEditar } = usePermisos('REVISION.APROBAR')
  const [editingField, setEditingField] = useState<PacienteField | null>(null)
  const [editValue, setEditValue] = useState('')

  const { mutate: guardar, isPending } = useMutation({
    mutationFn: (body: ActualizarGrupoDto) => gruposRecetaApi.actualizar(grupo.id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['grupos-receta', 'detail', grupo.id] })
      toast.success('Campo actualizado')
      setEditingField(null)
    },
    onError: (err: Error) => toast.error(err.message),
  })

  function startEdit(key: PacienteField, current: string | null | undefined) {
    setEditingField(key)
    setEditValue(current ?? '')
  }

  function cancelEdit() {
    setEditingField(null)
    setEditValue('')
  }

  function saveEdit() {
    if (!editingField) return
    guardar({ [editingField]: editValue || undefined })
  }

  const fields: FieldRow[] = [
    { key: 'nombrePaciente', label: 'Nombre', value: grupo.nombrePaciente },
    { key: 'apellidoPaterno', label: 'Apellido Paterno', value: grupo.apellidoPaterno },
    { key: 'apellidoMaterno', label: 'Apellido Materno', value: grupo.apellidoMaterno },
    { key: 'nombreMedico', label: 'Médico', value: grupo.nombreMedico },
    { key: 'fechaConsulta', label: 'Fecha de Consulta', value: grupo.fechaConsulta },
  ]

  return (
    <div className="max-w-lg space-y-4">
      {fields.map(f => (
        <div key={f.key} className="flex items-center gap-3">
          <span className="w-40 text-sm font-medium text-gray-500 shrink-0">{f.label}</span>

          {editingField === f.key ? (
            <>
              <input
                type={f.key === 'fechaConsulta' ? 'date' : 'text'}
                value={editValue}
                onChange={e => setEditValue(e.target.value)}
                className={inputClass}
                autoFocus
              />
              <button
                onClick={saveEdit}
                disabled={isPending}
                className="p-1.5 rounded-lg bg-green-600 text-white hover:bg-green-700 disabled:opacity-60 transition-colors"
              >
                <Check className="w-3.5 h-3.5" />
              </button>
              <button
                onClick={cancelEdit}
                className="p-1.5 rounded-lg bg-gray-200 text-gray-700 hover:bg-gray-300 transition-colors"
              >
                <X className="w-3.5 h-3.5" />
              </button>
            </>
          ) : (
            <>
              <span className="text-sm text-gray-900 flex-1">
                {f.key === 'fechaConsulta' && f.value
                  ? format(new Date(f.value), 'dd/MM/yyyy')
                  : (f.value ?? '—')}
              </span>
              {puedeEditar && (
                <button
                  onClick={() => startEdit(f.key, f.value)}
                  className="p-1.5 rounded-lg text-gray-400 hover:text-blue-600 hover:bg-blue-50 transition-colors"
                  title={`Editar ${f.label}`}
                >
                  <Pencil className="w-3.5 h-3.5" />
                </button>
              )}
            </>
          )}
        </div>
      ))}

      <div className="pt-4 border-t border-gray-100">
        <p className="text-xs text-gray-400">
          Aseguradora:{' '}
          <span className="font-medium text-gray-600">{grupo.nombreAseguradora}</span>
          {grupo.fechaCreacion && (
            <>
              {' · '}Creado el{' '}
              <span className="font-medium text-gray-600">
                {format(new Date(grupo.fechaCreacion), 'dd/MM/yyyy HH:mm')}
              </span>
            </>
          )}
          {grupo.modificadoPor && (
            <>
              {' · '}Modificado por{' '}
              <span className="font-medium text-gray-600">{grupo.modificadoPor}</span>
            </>
          )}
        </p>
      </div>
    </div>
  )
}

// ─── TAB 4: Historial ────────────────────────────────────────────────────────

function HistorialTab() {
  return (
    <div className="text-center py-16 text-gray-400">
      <Clock className="w-12 h-12 mx-auto mb-3 opacity-40" />
      <p className="text-sm font-medium text-gray-500">Historial de cambios</p>
      <p className="text-xs mt-1">
        El historial detallado estará disponible en una próxima versión.
      </p>
    </div>
  )
}

// ─── PAGE ────────────────────────────────────────────────────────────────────

export default function GrupoDetallePage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { puedeLeer: tieneAuditoria } = usePermisos('AUDITORIA.VER')
  const { puedeEscribir: puedeSubir } = usePermisos('IMAGENES.SUBIR')
  const { puedeLeer: puedeRevisar } = usePermisos('REVISION.VER')
  const { puedeLeer: puedeFacturar } = usePermisos('FACTURACION.VER')

  const [activeTab, setActiveTab] = useState<TabId>('imagenes')

  const { data: grupo, isLoading, isError } = useQuery({
    queryKey: ['grupos-receta', 'detail', id],
    queryFn: () => gruposRecetaApi.obtener(id!),
    enabled: !!id,
    refetchInterval: (query) => {
      const data = query.state.data as GrupoRecetaDetalleDto | undefined
      if (!data) return false
      const hasRecibida = data.imagenes.some(i => i.estadoImagen === 'RECIBIDA')
      const isProcessing = data.estadoGrupo === 'PROCESANDO'
      return hasRecibida || isProcessing ? 5_000 : false
    },
  })

  // sync tab visibility when audit permission loads
  useEffect(() => {
    if (activeTab === 'historial' && !tieneAuditoria) setActiveTab('imagenes')
  }, [tieneAuditoria, activeTab])

  if (isLoading) {
    return (
      <div className="p-6">
        <div className="animate-pulse space-y-4">
          <div className="h-8 w-64 bg-gray-200 rounded" />
          <div className="h-4 w-40 bg-gray-200 rounded" />
          <div className="h-48 bg-gray-100 rounded-xl" />
        </div>
      </div>
    )
  }

  if (isError || !grupo) {
    return (
      <div className="p-6 text-center">
        <p className="text-red-600 font-medium">No se pudo cargar el grupo.</p>
        <button
          onClick={() => navigate('/grupos-receta')}
          className="mt-4 text-sm text-blue-600 hover:underline"
        >
          Volver a la lista
        </button>
      </div>
    )
  }

  const tabs: { id: TabId; label: string; count?: number }[] = [
    { id: 'imagenes', label: 'Imágenes', count: grupo.imagenes.length },
    { id: 'medicamentos', label: 'Medicamentos', count: grupo.medicamentos.length },
    { id: 'paciente', label: 'Datos del Paciente' },
    ...(tieneAuditoria ? [{ id: 'historial' as TabId, label: 'Historial' }] : []),
  ]

  // Action buttons based on current estado
  const ESTADOS_PUEDE_SUBIR: GrupoRecetaDetalleDto['estadoGrupo'][] = [
    'RECIBIDO', 'REQUIERE_CAPTURA_MANUAL', 'GRUPO_INCOMPLETO',
  ]
  const ESTADOS_REVISION: GrupoRecetaDetalleDto['estadoGrupo'][] = ['REVISION_PENDIENTE']
  const ESTADOS_FACTURACION: GrupoRecetaDetalleDto['estadoGrupo'][] = [
    'REVISADO_COMPLETO', 'DATOS_FISCALES_INCOMPLETOS',
    'PENDIENTE_AUTORIZACION', 'PENDIENTE_FACTURACION',
  ]

  return (
    <div className="p-6">
      {/* Header */}
      <div className="flex items-start justify-between mb-6 flex-wrap gap-4">
        <div className="flex items-center gap-3">
          <button
            onClick={() => navigate('/grupos-receta')}
            className="p-1.5 rounded-lg text-gray-400 hover:text-gray-700 hover:bg-gray-100 transition-colors"
          >
            <ArrowLeft className="w-5 h-5" />
          </button>
          <div>
            <div className="flex items-center gap-3 flex-wrap">
              <h1 className="text-2xl font-bold text-gray-900">
                {grupo.folioBase ?? `Grupo sin folio`}
              </h1>
              <StatusBadge estado={grupo.estadoGrupo} />
            </div>
            <p className="text-sm text-gray-500 mt-0.5">
              {grupo.nombreAseguradora}
              {grupo.fechaConsulta &&
                ` · Consulta: ${format(new Date(grupo.fechaConsulta), 'dd/MM/yyyy')}`}
            </p>
          </div>
        </div>

        {/* Contextual action buttons */}
        <div className="flex items-center gap-2 flex-wrap">
          {puedeSubir && ESTADOS_PUEDE_SUBIR.includes(grupo.estadoGrupo) && (
            <button
              onClick={() => navigate(`/imagenes/subir?idGrupo=${grupo.id}`)}
              className="inline-flex items-center gap-2 border border-gray-300 bg-white text-gray-700 px-3 py-2 rounded-lg text-sm font-medium hover:bg-gray-50 transition-colors"
            >
              <Upload className="w-4 h-4" />
              Subir imagen
            </button>
          )}
          {puedeRevisar && ESTADOS_REVISION.includes(grupo.estadoGrupo) && (
            <button
              onClick={() => navigate('/revision')}
              className="inline-flex items-center gap-2 bg-yellow-500 text-white px-3 py-2 rounded-lg text-sm font-medium hover:bg-yellow-600 transition-colors"
            >
              <ClipboardCheck className="w-4 h-4" />
              Ir a revisión
            </button>
          )}
          {puedeFacturar && ESTADOS_FACTURACION.includes(grupo.estadoGrupo) && (
            <button
              onClick={() => navigate(`/facturacion/${grupo.id}/generar`)}
              className="inline-flex items-center gap-2 bg-blue-600 text-white px-3 py-2 rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors"
            >
              <Receipt className="w-4 h-4" />
              Generar CFDI
            </button>
          )}
          {grupo.estadoGrupo === 'FACTURADA' && puedeFacturar && (
            <button
              onClick={() => navigate('/facturacion')}
              className="inline-flex items-center gap-2 border border-blue-300 bg-blue-50 text-blue-700 px-3 py-2 rounded-lg text-sm font-medium hover:bg-blue-100 transition-colors"
            >
              <Receipt className="w-4 h-4" />
              Ver facturación
            </button>
          )}
        </div>
      </div>

      {/* Tabs */}
      <div className="bg-white rounded-xl border border-gray-200 p-6">
        <TabNav tabs={tabs} active={activeTab} onChange={setActiveTab} />

        {activeTab === 'imagenes' && (
          <ImagenesTab imagenes={grupo.imagenes} idGrupo={grupo.id} />
        )}
        {activeTab === 'medicamentos' && (
          <MedicamentosTab medicamentos={grupo.medicamentos} />
        )}
        {activeTab === 'paciente' && <PacienteTab grupo={grupo} />}
        {activeTab === 'historial' && tieneAuditoria && <HistorialTab />}
      </div>
    </div>
  )
}
