import { useState, useRef, useEffect } from 'react'
import api from '@/utils/axios.instance'
import { useParams, useNavigate, useSearchParams } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { format } from 'date-fns'
import toast from 'react-hot-toast'
import {
  ArrowLeft, ZoomIn, ZoomOut, RotateCcw, Check, X, RefreshCw,
  Plus, Trash2, Eye, ChevronDown, CheckCircle, AlertCircle,
} from 'lucide-react'
import { imagenesApi } from '@/api/imagenes.api'
import { gruposRecetaApi } from '@/api/grupos-receta.api'
import { revisionApi } from '@/api/revision.api'
import { ocrApi } from '@/api/ocr.api'
import { StatusBadge } from '@/components/common/StatusBadge'
import type { ImagenDto, MedicamentoRecetaDto } from '@/types/imagen.types'
import type { GrupoRecetaDetalleDto } from '@/types/grupo-receta.types'

// ─── Simple modal ─────────────────────────────────────────────────────────────

function Modal({
  open, onClose, title, children,
}: {
  open: boolean
  onClose: () => void
  title: string
  children: React.ReactNode
}) {
  if (!open) return null
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/50" onClick={onClose} />
      <div className="relative bg-white rounded-xl shadow-xl w-full max-w-md mx-4 p-6 z-10">
        <h3 className="text-base font-semibold text-gray-900 mb-4">{title}</h3>
        {children}
      </div>
    </div>
  )
}

// ─── Image viewer ─────────────────────────────────────────────────────────────

function useBlobUrl(imagenId: string | undefined, tipo: 'raw' | 'ocr'): string | null {
  const [url, setUrl] = useState<string | null>(null)
  useEffect(() => {
    if (!imagenId) return
    let objectUrl: string | null = null
    let cancelled = false
    api.get(`/imagenes/${imagenId}/${tipo}`, { responseType: 'blob' })
      .then(r => {
        if (cancelled) return
        objectUrl = URL.createObjectURL(r.data as Blob)
        setUrl(objectUrl)
      })
      .catch(() => { if (!cancelled) setUrl(null) })
    return () => {
      cancelled = true
      if (objectUrl) URL.revokeObjectURL(objectUrl)
    }
  }, [imagenId, tipo])
  return url
}

function ImageViewer({ imagen }: { imagen: ImagenDto }) {
  const [showOcr, setShowOcr] = useState(false)
  const [zoom, setZoom] = useState(1)
  const containerRef = useRef<HTMLDivElement>(null)

  const rawUrl = useBlobUrl(imagen.id, 'raw')
  const ocrUrl = useBlobUrl(imagen.urlBlobOcr ? imagen.id : undefined, 'ocr')
  const src = showOcr && ocrUrl ? ocrUrl : rawUrl

  return (
    <div className="flex flex-col gap-3 h-full">
      {/* Controls */}
      <div className="flex items-center gap-2 flex-wrap">
        {imagen.urlBlobOcr && (
          <button
            onClick={() => setShowOcr(v => !v)}
            className={`inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium border transition-colors ${
              showOcr
                ? 'bg-blue-600 text-white border-blue-600'
                : 'bg-white text-gray-600 border-gray-300 hover:bg-gray-50'
            }`}
          >
            <Eye className="w-3.5 h-3.5" />
            {showOcr ? 'Ver original' : 'Ver OCR'}
          </button>
        )}
        <button
          onClick={() => setZoom(z => Math.min(3, z + 0.25))}
          className="p-1.5 rounded-lg border border-gray-300 hover:bg-gray-50 transition-colors"
          title="Acercar"
        >
          <ZoomIn className="w-4 h-4" />
        </button>
        <button
          onClick={() => setZoom(z => Math.max(0.5, z - 0.25))}
          className="p-1.5 rounded-lg border border-gray-300 hover:bg-gray-50 transition-colors"
          title="Alejar"
        >
          <ZoomOut className="w-4 h-4" />
        </button>
        <button
          onClick={() => setZoom(1)}
          className="p-1.5 rounded-lg border border-gray-300 hover:bg-gray-50 transition-colors"
          title="Restablecer zoom"
        >
          <RotateCcw className="w-4 h-4" />
        </button>
        <span className="text-xs text-gray-400 ml-1">{Math.round(zoom * 100)}%</span>
      </div>

      {/* Image */}
      <div
        ref={containerRef}
        className="flex-1 overflow-auto rounded-xl border border-gray-200 bg-gray-900 flex items-start justify-center p-2 min-h-[300px]"
      >
        {src ? (
          <img
            src={src}
            alt={`Hoja ${imagen.numeroHoja}`}
            style={{ transform: `scale(${zoom})`, transformOrigin: 'top center', transition: 'transform 0.15s' }}
            className="max-w-full rounded"
          />
        ) : (
          <div className="flex flex-col items-center justify-center gap-3 text-gray-500 py-12">
            <div className="w-8 h-8 border-2 border-gray-400 border-t-transparent rounded-full animate-spin" />
            <p className="text-sm">Cargando imagen...</p>
          </div>
        )}
      </div>

      {/* Meta info */}
      <div className="bg-gray-50 rounded-lg border border-gray-200 p-3 grid grid-cols-2 gap-y-1.5 text-xs">
        <div>
          <span className="text-gray-500">Hoja</span>
          <span className="ml-2 font-medium text-gray-900">#{imagen.numeroHoja}</span>
        </div>
        <div>
          <span className="text-gray-500">Origen</span>
          <span className="ml-2 font-medium text-gray-900">{imagen.origenImagen}</span>
        </div>
        <div>
          <span className="text-gray-500">Subida</span>
          <span className="ml-2 font-medium text-gray-900">
            {format(new Date(imagen.fechaSubida), 'dd/MM/yyyy HH:mm')}
          </span>
        </div>
        {imagen.scoreLegibilidad !== null && (
          <div>
            <span className="text-gray-500">Score</span>
            <span className="ml-2 font-medium text-gray-900">
              {Math.round(imagen.scoreLegibilidad * 100)}%
            </span>
          </div>
        )}
        <div className="col-span-2">
          <StatusBadge estado={imagen.estadoImagen} size="sm" />
        </div>
      </div>
    </div>
  )
}

// ─── Editable field ───────────────────────────────────────────────────────────

function EditableField({
  label, value, idImagen, tabla, campo, tipoCorreccion = 'MANUAL',
}: {
  label: string
  value: string | null | undefined
  idImagen: string
  tabla: string
  campo: string
  tipoCorreccion?: string
}) {
  const [current, setCurrent] = useState(value ?? '')
  const [saved, setSaved] = useState(false)

  async function onBlur() {
    const prev = value ?? ''
    if (current === prev) return
    try {
      await revisionApi.corregirCampo(idImagen, tabla, campo, prev || null, current, tipoCorreccion)
      setSaved(true)
      setTimeout(() => setSaved(false), 2000)
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al guardar')
    }
  }

  return (
    <div>
      <label className="block text-xs font-medium text-gray-500 mb-1">
        {label}
        {saved && <span className="ml-2 text-green-500">✓ guardado</span>}
      </label>
      <input
        type="text"
        value={current}
        onChange={e => setCurrent(e.target.value)}
        onBlur={onBlur}
        placeholder="—"
        className="w-full border border-gray-300 rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
      />
    </div>
  )
}

// ─── Medication row ───────────────────────────────────────────────────────────

type MedFields = Omit<MedicamentoRecetaDto, 'id' | 'numeroPrescripcion'>

function MedicamentoRow({
  med, idImagen, onRemove,
}: {
  med: MedicamentoRecetaDto
  idImagen: string
  onRemove?: () => void
}) {
  const [open, setOpen] = useState(false)
  const tabla = 'MedicamentosReceta'

  const fields: { label: string; campo: keyof MedFields }[] = [
    { label: 'Nombre comercial', campo: 'nombreComercial' },
    { label: 'Sustancia activa', campo: 'sustanciaActiva' },
    { label: 'Presentación', campo: 'presentacion' },
    { label: 'Dosis', campo: 'dosis' },
    { label: 'Cantidad', campo: 'cantidadTexto' },
    { label: 'Frecuencia', campo: 'frecuenciaTexto' },
    { label: 'Duración', campo: 'duracionTexto' },
    { label: 'Vía administración', campo: 'viaAdministracion' },
    { label: 'Indicaciones completas', campo: 'indicacionesCompletas' },
  ]

  return (
    <div className="border border-gray-200 rounded-xl overflow-hidden">
      <button
        onClick={() => setOpen(v => !v)}
        className="w-full flex items-center justify-between px-4 py-3 bg-gray-50 hover:bg-gray-100 transition-colors text-left"
      >
        <span className="text-sm font-semibold text-gray-800">
          #{med.numeroPrescripcion}{' '}
          {med.nombreComercial ?? med.sustanciaActiva ?? 'Medicamento sin nombre'}
        </span>
        <div className="flex items-center gap-2">
          {onRemove && (
            <span
              role="button"
              onClick={e => { e.stopPropagation(); onRemove() }}
              className="p-1 rounded text-gray-400 hover:text-red-500 hover:bg-red-50 transition-colors"
              title="Eliminar"
            >
              <Trash2 className="w-3.5 h-3.5" />
            </span>
          )}
          <ChevronDown
            className={`w-4 h-4 text-gray-500 transition-transform ${open ? 'rotate-180' : ''}`}
          />
        </div>
      </button>

      {open && (
        <div className="p-4 grid grid-cols-1 sm:grid-cols-2 gap-3">
          {fields.map(f => (
            <EditableField
              key={f.campo}
              label={f.label}
              value={med[f.campo] as string | null}
              idImagen={idImagen}
              tabla={tabla}
              campo={f.campo}
            />
          ))}
        </div>
      )}
    </div>
  )
}

// ─── OCR Banner ──────────────────────────────────────────────────────────────

function OcrBanner({ imagen }: { imagen: ImagenDto }) {
  const score = imagen.scoreLegibilidad != null
    ? `${imagen.scoreLegibilidad.toFixed(0)}%`
    : null

  if (imagen.estadoImagen === 'OCR_APROBADO') {
    return (
      <div className="mb-5 flex items-center gap-3 bg-green-50 border border-green-200 rounded-xl px-4 py-3">
        <CheckCircle className="w-5 h-5 text-green-500 shrink-0" />
        <p className="text-sm font-medium text-green-800">
          Alta confianza{score ? ` (${score})` : ''} — solo confirma los datos
        </p>
      </div>
    )
  }

  if (imagen.estadoImagen === 'OCR_BAJA_CONFIANZA') {
    return (
      <div className="mb-5 flex items-center gap-3 bg-yellow-50 border border-yellow-200 rounded-xl px-4 py-3">
        <AlertCircle className="w-5 h-5 text-yellow-500 shrink-0" />
        <p className="text-sm font-medium text-yellow-800">
          Confianza baja{score ? ` (${score})` : ''} — revisa cada campo
        </p>
      </div>
    )
  }

  if (imagen.estadoImagen === 'ILEGIBLE') {
    return (
      <div className="mb-5 flex items-center gap-3 bg-red-50 border border-red-200 rounded-xl px-4 py-3">
        <AlertCircle className="w-5 h-5 text-red-500 shrink-0" />
        <p className="text-sm font-medium text-red-800">
          Imagen ilegible — captura manual requerida
        </p>
      </div>
    )
  }

  return (
    <div className="mb-5 flex items-center gap-3 bg-blue-50 border border-blue-200 rounded-xl px-4 py-3">
      <CheckCircle className="w-5 h-5 text-blue-500 shrink-0" />
      <p className="text-sm font-medium text-blue-800">
        ✓ OCR completado — revisa y confirma los datos extraídos
      </p>
    </div>
  )
}

// ─── Data panel ───────────────────────────────────────────────────────────────

function DatosPanel({
  imagen, grupo,
}: {
  imagen: ImagenDto
  grupo: GrupoRecetaDetalleDto
}) {
  const [meds, setMeds] = useState<MedicamentoRecetaDto[]>(() => grupo.medicamentos)
  const tabla = 'GruposReceta'

  function addMedicamento() {
    const nuevo: MedicamentoRecetaDto = {
      id: `new-${Date.now()}`,
      numeroPrescripcion: meds.length + 1,
      nombreComercial: null, sustanciaActiva: null, presentacion: null,
      dosis: null,
      cantidad: null, cantidadTexto: null, cantidadNumero: null, unidadCantidad: null,
      frecuenciaTexto: null, frecuenciaExpandida: null,
      duracionTexto: null, duracionDias: null,
      indicaciones: null, indicacionesCompletas: null,
      viaAdministracion: null,
      codigoCIE10: null, codigoEAN: null, fuenteDato: null,
    }
    setMeds(prev => [...prev, nuevo])
  }

  function removeMedicamento(id: string) {
    setMeds(prev => prev.filter(m => m.id !== id))
  }

  return (
    <div className="space-y-5">
      <div>
        <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3">Datos del paciente</p>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <EditableField label="Nombre" value={grupo.nombrePaciente} idImagen={imagen.id} tabla={tabla} campo="nombrePaciente" />
          <EditableField label="Apellido Paterno" value={grupo.apellidoPaterno} idImagen={imagen.id} tabla={tabla} campo="apellidoPaterno" />
          <EditableField label="Apellido Materno" value={grupo.apellidoMaterno} idImagen={imagen.id} tabla={tabla} campo="apellidoMaterno" />
          <EditableField label="Médico" value={grupo.nombreMedico} idImagen={imagen.id} tabla={tabla} campo="nombreMedico" />
          <EditableField label="Fecha de consulta" value={grupo.fechaConsulta} idImagen={imagen.id} tabla={tabla} campo="fechaConsulta" />
        </div>
      </div>

      <div>
        <div className="flex items-center justify-between mb-3">
          <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide">
            Medicamentos ({meds.length})
          </p>
          <button
            onClick={addMedicamento}
            className="inline-flex items-center gap-1.5 text-xs font-medium text-blue-600 hover:text-blue-800 transition-colors"
          >
            <Plus className="w-3.5 h-3.5" />
            Agregar medicamento
          </button>
        </div>

        {meds.length === 0 ? (
          <p className="text-sm text-gray-400 text-center py-6">
            No hay medicamentos. Usa «+ Agregar medicamento» para capturar manualmente.
          </p>
        ) : (
          <div className="space-y-2">
            {meds.map(m => (
              <MedicamentoRow
                key={m.id}
                med={m}
                idImagen={imagen.id}
                onRemove={m.id?.startsWith('new-') ? () => removeMedicamento(m.id!) : undefined}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  )
}

// ─── Dialogs ──────────────────────────────────────────────────────────────────

function AprobarDialog({
  open, onClose, idImagen, numeroHoja, onDone,
}: {
  open: boolean; onClose: () => void
  idImagen: string; numeroHoja: number
  onDone: () => void
}) {
  const [obs, setObs] = useState('')
  const { mutate, isPending } = useMutation({
    mutationFn: () => revisionApi.aprobar({ idImagen, observaciones: obs || undefined }),
    onSuccess: () => {
      toast.success('Imagen aprobada correctamente')
      onDone()
    },
    onError: (err: Error) => toast.error(err.message),
  })

  return (
    <Modal open={open} onClose={onClose} title={`¿Confirmar aprobación de imagen #${numeroHoja}?`}>
      <div className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Observaciones (opcional)</label>
          <textarea
            rows={3}
            value={obs}
            onChange={e => setObs(e.target.value)}
            placeholder="Notas adicionales..."
            className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-500"
          />
        </div>
        <div className="flex justify-end gap-3">
          <button onClick={onClose} className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors">
            Cancelar
          </button>
          <button
            onClick={() => mutate()}
            disabled={isPending}
            className="inline-flex items-center gap-2 px-5 py-2 text-sm font-semibold text-white bg-green-600 rounded-lg hover:bg-green-700 disabled:opacity-60 transition-colors"
          >
            {isPending && <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />}
            <Check className="w-4 h-4" />
            Aprobar
          </button>
        </div>
      </div>
    </Modal>
  )
}

function RechazarDialog({
  open, onClose, idImagen, onDone,
}: {
  open: boolean; onClose: () => void
  idImagen: string
  onDone: () => void
}) {
  const [motivo, setMotivo] = useState('')
  const { mutate, isPending } = useMutation({
    mutationFn: () => revisionApi.rechazar({ idImagen, motivo }),
    onSuccess: () => {
      toast.success('Imagen rechazada')
      onDone()
    },
    onError: (err: Error) => toast.error(err.message),
  })

  return (
    <Modal open={open} onClose={onClose} title="Rechazar imagen">
      <div className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Motivo de rechazo <span className="text-red-500">*</span>
          </label>
          <textarea
            rows={3}
            value={motivo}
            onChange={e => setMotivo(e.target.value)}
            placeholder="Describe el motivo del rechazo..."
            className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-red-500"
          />
        </div>
        <div className="flex justify-end gap-3">
          <button onClick={onClose} className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors">
            Cancelar
          </button>
          <button
            onClick={() => mutate()}
            disabled={isPending || !motivo.trim()}
            className="inline-flex items-center gap-2 px-5 py-2 text-sm font-semibold text-white bg-red-600 rounded-lg hover:bg-red-700 disabled:opacity-60 transition-colors"
          >
            {isPending && <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />}
            <X className="w-4 h-4" />
            Rechazar
          </button>
        </div>
      </div>
    </Modal>
  )
}

// ─── PAGE ─────────────────────────────────────────────────────────────────────

export default function RevisionImagenPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const qc = useQueryClient()

  const ocrParam = searchParams.get('ocr') === 'true'

  const [showAprobar, setShowAprobar] = useState(false)
  const [showRechazar, setShowRechazar] = useState(false)

  // Queries
  const { data: imagen, isLoading: loadingImg } = useQuery({
    queryKey: ['imagenes', id],
    queryFn: () => imagenesApi.obtener(id!),
    enabled: !!id,
  })

  const { data: grupo, isLoading: loadingGrupo } = useQuery({
    queryKey: ['grupos-receta', 'detail', imagen?.idGrupo],
    queryFn: () => gruposRecetaApi.obtener(imagen!.idGrupo),
    enabled: !!imagen?.idGrupo,
  })

  // Reprocesar OCR
  const { mutate: reprocesar, isPending: isReprocesando } = useMutation({
    mutationFn: (motivo: string) => ocrApi.reprocesar(id!, motivo),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['imagenes', id] })
      toast('Imagen enviada a reprocesar', { icon: 'ℹ️' })
    },
    onError: (err: Error) => toast.error(err.message),
  })

  function onRevisionDone() {
    navigate('/revision')
  }

  const isLoading = loadingImg || loadingGrupo

  if (isLoading || !imagen || !grupo) {
    return (
      <div className="p-6">
        <div className="animate-pulse space-y-4">
          <div className="h-8 w-64 bg-gray-200 rounded" />
          <div className="h-96 bg-gray-100 rounded-xl" />
        </div>
      </div>
    )
  }

  const puedeReprocesar = imagen.estadoImagen === 'ILEGIBLE'

  return (
    <div className="p-6">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center gap-3">
          <button
            onClick={() => navigate('/revision')}
            className="p-1.5 rounded-lg text-gray-400 hover:text-gray-700 hover:bg-gray-100 transition-colors"
          >
            <ArrowLeft className="w-5 h-5" />
          </button>
          <div>
            <div className="flex items-center gap-3">
              <h1 className="text-xl font-bold text-gray-900">
                Revisión — Hoja #{imagen.numeroHoja}
              </h1>
              <StatusBadge estado={imagen.estadoImagen} />
            </div>
            <p className="text-sm text-gray-500 mt-0.5">
              Grupo: {grupo.folioBase ?? 'Sin folio'} · {grupo.nombreAseguradora}
            </p>
          </div>
        </div>
        {puedeReprocesar && (
          <button
            onClick={() => reprocesar('Revisión manual solicitó reprocesar')}
            disabled={isReprocesando}
            className="inline-flex items-center gap-2 border border-gray-300 bg-white text-gray-700 px-3 py-2 rounded-lg text-sm font-medium hover:bg-gray-50 disabled:opacity-60 transition-colors"
          >
            {isReprocesando ? (
              <span className="w-4 h-4 border-2 border-gray-400/30 border-t-gray-600 rounded-full animate-spin" />
            ) : (
              <RefreshCw className="w-4 h-4" />
            )}
            Reprocesar OCR
          </button>
        )}
      </div>

      {/* OCR Banner */}
      {ocrParam && <OcrBanner imagen={imagen} />}

      {/* 2-column layout */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Left — image */}
        <div className="bg-white rounded-xl border border-gray-200 p-4">
          <ImageViewer imagen={imagen} />
        </div>

        {/* Right — data */}
        <div className="bg-white rounded-xl border border-gray-200 p-5 overflow-y-auto max-h-[75vh]">
          <p className="text-xs font-semibold text-gray-400 uppercase tracking-wide mb-4">
            Datos extraídos — edita y guarda con Tab / clic fuera del campo
          </p>
          <DatosPanel imagen={imagen} grupo={grupo} />
        </div>
      </div>

      {/* Footer actions */}
      <div className="mt-6 flex items-center justify-end gap-3 pt-4 border-t border-gray-200">
        <button
          onClick={() => setShowRechazar(true)}
          className="inline-flex items-center gap-2 bg-red-600 text-white px-5 py-2.5 rounded-lg text-sm font-semibold hover:bg-red-700 transition-colors"
        >
          <X className="w-4 h-4" />
          Rechazar
        </button>
        <button
          onClick={() => setShowAprobar(true)}
          className="inline-flex items-center gap-2 bg-green-600 text-white px-5 py-2.5 rounded-lg text-sm font-semibold hover:bg-green-700 transition-colors"
        >
          <Check className="w-4 h-4" />
          {imagen.estadoImagen === 'OCR_APROBADO'
            ? 'Confirmar y aprobar'
            : imagen.estadoImagen === 'OCR_BAJA_CONFIANZA'
            ? 'Validar y aprobar'
            : imagen.estadoImagen === 'ILEGIBLE'
            ? 'Guardar captura manual'
            : 'Aprobar'}
        </button>
      </div>

      {/* Dialogs */}
      <AprobarDialog
        open={showAprobar}
        onClose={() => setShowAprobar(false)}
        idImagen={imagen.id}
        numeroHoja={imagen.numeroHoja}
        onDone={onRevisionDone}
      />
      <RechazarDialog
        open={showRechazar}
        onClose={() => setShowRechazar(false)}
        idImagen={imagen.id}
        onDone={onRevisionDone}
      />
    </div>
  )
}
