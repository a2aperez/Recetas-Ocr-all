import { useState, useCallback } from 'react'
import { useSearchParams, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { ArrowLeft, Upload, Camera, Images, X, CheckCircle, AlertCircle, Loader2 } from 'lucide-react'
import { gruposRecetaApi } from '@/api/grupos-receta.api'
import { CamaraCaptura } from '@/components/imagenes/CamaraCaptura'
import { GaleriaImport } from '@/components/imagenes/GaleriaImport'
import { useSubirImagen } from '@/hooks/useImagenes'
import { PageHeader } from '@/components/common/PageHeader'
import type { OrigenImagen } from '@/types/imagen.types'

// ─── Types ───────────────────────────────────────────────────────────────────

type FileOrigen = OrigenImagen

interface PendingFile {
  id: string
  file: File
  origen: FileOrigen
  progress: number
  status: 'pendiente' | 'subiendo' | 'ok' | 'error'
  errorMsg?: string
  preview: string | null
}

type TabId = 'camara' | 'galeria'

// ─── helpers ─────────────────────────────────────────────────────────────────

function formatBytes(b: number) {
  if (b < 1024) return `${b} B`
  if (b < 1024 * 1024) return `${(b / 1024).toFixed(1)} KB`
  return `${(b / (1024 * 1024)).toFixed(1)} MB`
}

function makePreview(file: File): string | null {
  return file.type.startsWith('image/') ? URL.createObjectURL(file) : null
}

// ─── Group Selector (shown when no idGrupo in URL) ───────────────────────────

function GrupoSelector({ onSelect }: { onSelect: (id: string) => void }) {
  const navigate = useNavigate()
  const [page] = useState(1)
  const { data, isLoading, isError } = useQuery({
    queryKey: ['grupos-receta', 'list', { page, pageSize: 20 }],
    queryFn: () => gruposRecetaApi.listar({ page, pageSize: 20 }),
  })

  const grupos = data?.items ?? []

  return (
    <div className="p-6 max-w-lg mx-auto">
      <PageHeader
        title="Subir Imágenes"
        subtitle="Selecciona el grupo de receta al que pertenecen las imágenes"
        actions={
          <button
            onClick={() => navigate('/grupos-receta/nuevo')}
            className="inline-flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors"
          >
            + Nuevo grupo
          </button>
        }
      />
      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
        {isLoading ? (
          <div className="p-10 flex flex-col items-center gap-2 text-gray-400">
            <Loader2 className="w-7 h-7 animate-spin text-blue-500" />
            <span className="text-sm">Cargando grupos...</span>
          </div>
        ) : isError ? (
          <div className="p-10 text-center">
            <AlertCircle className="w-8 h-8 text-red-400 mx-auto mb-2" />
            <p className="text-sm font-medium text-gray-700">Error al cargar los grupos</p>
            <p className="text-xs text-gray-400 mt-1">Verifica la conexión con el servidor</p>
          </div>
        ) : grupos.length === 0 ? (
          <div className="p-10 text-center">
            <Upload className="w-10 h-10 text-gray-300 mx-auto mb-3" />
            <p className="text-sm font-semibold text-gray-700">No hay grupos de receta</p>
            <p className="text-xs text-gray-400 mt-1 mb-4">
              Crea un grupo primero para poder subir imágenes
            </p>
            <button
              onClick={() => navigate('/grupos-receta/nuevo')}
              className="inline-flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors"
            >
              + Crear primer grupo
            </button>
          </div>
        ) : (
          <ul className="divide-y divide-gray-100">
            {grupos.map(g => (
              <li key={g.id}>
                <button
                  onClick={() => onSelect(g.id)}
                  className="w-full px-4 py-3 text-left hover:bg-blue-50 transition-colors"
                >
                  <p className="text-sm font-medium text-gray-900">
                    {g.folioBase ?? 'Sin folio'}
                  </p>
                  <p className="text-xs text-gray-400">
                    {g.nombreAseguradora} · {g.estadoGrupo}
                  </p>
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  )
}

// ─── PAGE ────────────────────────────────────────────────────────────────────

export default function SubirImagenPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const { mutateAsync: subirImagen } = useSubirImagen()

  const [idGrupoOverride, setIdGrupoOverride] = useState<string | null>(null)
  const idGrupo = searchParams.get('idGrupo') ?? idGrupoOverride

  const [activeTab, setActiveTab] = useState<TabId>('galeria')
  const [pendingFiles, setPendingFiles] = useState<PendingFile[]>([])
  const [uploading, setUploading] = useState(false)

  // ── Event handlers ────────────────────────────────────────────────────────

  function addFiles(files: File[], origen: FileOrigen) {
    const newEntries: PendingFile[] = files.map(f => ({
      id: `${f.name}-${f.size}-${Date.now()}-${Math.random()}`,
      file: f,
      origen,
      progress: 0,
      status: 'pendiente',
      preview: makePreview(f),
    }))
    setPendingFiles(prev => {
      // Dedupe by name+size
      const existingKeys = new Set(prev.map(p => `${p.file.name}-${p.file.size}`))
      return [...prev, ...newEntries.filter(e => !existingKeys.has(`${e.file.name}-${e.file.size}`))]
    })
  }

  const onCaptura = useCallback(
    (file: File) => {
      addFiles([file], 'CAMARA')
      setActiveTab('galeria') // switch to list view after capture
    },
    // eslint-disable-next-line react-hooks/exhaustive-deps
    []
  )

  const onArchivosGaleria = useCallback((files: File[]) => {
    // GaleriaImport manages its own internal list; we only get what's valid
    setPendingFiles(prev => {
      const existingKeys = new Set(
        prev.filter(p => p.origen === 'GALERIA').map(p => `${p.file.name}-${p.file.size}`)
      )
      const fresh = files
        .filter(f => !existingKeys.has(`${f.name}-${f.size}`))
        .map(
          (f): PendingFile => ({
            id: `${f.name}-${f.size}-${Date.now()}-${Math.random()}`,
            file: f,
            origen: 'GALERIA',
            progress: 0,
            status: 'pendiente',
            preview: makePreview(f),
          })
        )
      // merge: keep camara items + rebuild galeria items from fresh list
      const cameraItems = prev.filter(p => p.origen === 'CAMARA')
      const galeriaItems: PendingFile[] = files.map(f => {
        const existing = prev.find(
          p => p.origen === 'GALERIA' && p.file.name === f.name && p.file.size === f.size
        )
        return (
          existing ?? {
            id: `${f.name}-${f.size}-${Date.now()}-${Math.random()}`,
            file: f,
            origen: 'GALERIA',
            progress: 0,
            status: 'pendiente',
            preview: makePreview(f),
          }
        )
      })
      void fresh // suppress lint
      return [...cameraItems, ...galeriaItems]
    })
  }, [])

  function removeFile(id: string) {
    setPendingFiles(prev => prev.filter(p => p.id !== id))
  }

  // ── Upload all ────────────────────────────────────────────────────────────

  async function subirTodo() {
    if (!idGrupo) return
    setUploading(true)

    const toUpload = pendingFiles.filter(p => p.status === 'pendiente')

    for (const entry of toUpload) {
      setPendingFiles(prev =>
        prev.map(p => (p.id === entry.id ? { ...p, status: 'subiendo', progress: 0 } : p))
      )
      try {
        await subirImagen({
          idGrupo,
          archivo: entry.file,
          origen: entry.origen,
          onProgress: (pct) =>
            setPendingFiles(prev =>
              prev.map(p => (p.id === entry.id ? { ...p, progress: pct } : p))
            ),
        })
        setPendingFiles(prev =>
          prev.map(p => (p.id === entry.id ? { ...p, status: 'ok', progress: 100 } : p))
        )
      } catch (err) {
        const msg = err instanceof Error ? err.message : 'Error desconocido'
        setPendingFiles(prev =>
          prev.map(p =>
            p.id === entry.id ? { ...p, status: 'error', errorMsg: msg } : p
          )
        )
      }
    }

    setUploading(false)

    // If all succeeded, navigate away
    const latest = pendingFiles.map(p =>
      toUpload.find(t => t.id === p.id) ? { ...p, status: 'ok' as const } : p
    )
    const allOk = latest.every(p => p.status === 'ok')
    if (allOk) navigate(`/grupos-receta/${idGrupo}`)
  }

  // ── No group selected ─────────────────────────────────────────────────────

  if (!idGrupo) {
    return <GrupoSelector onSelect={id => setIdGrupoOverride(id)} />
  }

  // ── Pending list stats ────────────────────────────────────────────────────

  const pendienteCount = pendingFiles.filter(p => p.status === 'pendiente').length
  const okCount = pendingFiles.filter(p => p.status === 'ok').length
  const errorCount = pendingFiles.filter(p => p.status === 'error').length
  const hasAnyPending = pendienteCount > 0

  // ── Render ────────────────────────────────────────────────────────────────

  return (
    <div className="p-6 max-w-2xl mx-auto">
      <PageHeader
        title="Subir Imágenes"
        subtitle={`Grupo: ${idGrupo}`}
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

      <div className="space-y-6">
        {/* Tabs */}
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          {/* Tab nav */}
          <div className="flex border-b border-gray-200">
            {(['galeria', 'camara'] as TabId[]).map(tab => (
              <button
                key={tab}
                onClick={() => setActiveTab(tab)}
                className={`flex-1 flex items-center justify-center gap-2 py-3 text-sm font-medium border-b-2 transition-colors ${
                  activeTab === tab
                    ? 'border-blue-600 text-blue-600 bg-blue-50/50'
                    : 'border-transparent text-gray-500 hover:text-gray-700'
                }`}
              >
                {tab === 'camara' ? (
                  <><Camera className="w-4 h-4" /> Cámara</>
                ) : (
                  <><Images className="w-4 h-4" /> Galería</>
                )}
              </button>
            ))}
          </div>

          {/* Tab content */}
          <div className="p-5">
            {activeTab === 'camara' ? (
              <CamaraCaptura
                onCaptura={onCaptura}
                onCancelar={() => setActiveTab('galeria')}
              />
            ) : (
              <GaleriaImport onArchivos={onArchivosGaleria} />
            )}
          </div>
        </div>

        {/* Pending files list */}
        {pendingFiles.length > 0 && (
          <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
            <div className="px-4 py-3 border-b border-gray-100 flex items-center justify-between">
              <p className="text-sm font-semibold text-gray-800">
                Archivos a subir
                <span className="ml-2 text-xs font-normal text-gray-400">
                  {okCount > 0 && `${okCount} subidos · `}
                  {errorCount > 0 && `${errorCount} con error · `}
                  {pendienteCount} pendiente{pendienteCount !== 1 ? 's' : ''}
                </span>
              </p>
              {!uploading && pendienteCount > 0 && (
                <button
                  onClick={() =>
                    setPendingFiles(prev => prev.filter(p => p.status !== 'ok'))
                  }
                  className="text-xs text-gray-400 hover:text-red-500 transition-colors"
                >
                  Limpiar completados
                </button>
              )}
            </div>

            <ul className="divide-y divide-gray-100">
              {pendingFiles.map(entry => (
                <li key={entry.id} className="flex items-center gap-3 p-3">
                  {/* Thumbnail */}
                  <div className="w-12 h-12 rounded-lg overflow-hidden bg-gray-100 shrink-0 flex items-center justify-center">
                    {entry.preview ? (
                      <img
                        src={entry.preview}
                        alt={entry.file.name}
                        className="w-full h-full object-cover"
                      />
                    ) : (
                      <Upload className="w-5 h-5 text-gray-400" />
                    )}
                  </div>

                  {/* Info + progress */}
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 mb-0.5">
                      <p className="text-sm font-medium text-gray-800 truncate">
                        {entry.file.name}
                      </p>
                      <span className="text-xs text-gray-400 shrink-0">
                        {formatBytes(entry.file.size)}
                      </span>
                      <span className="text-xs bg-gray-100 text-gray-500 px-1.5 py-0.5 rounded shrink-0">
                        {entry.origen === 'CAMARA' ? 'Cámara' : 'Galería'}
                      </span>
                    </div>

                    {entry.status === 'subiendo' && (
                      <div className="w-full bg-gray-200 rounded-full h-1.5 mt-1">
                        <div
                          className="bg-blue-600 h-1.5 rounded-full transition-all"
                          style={{ width: `${entry.progress}%` }}
                        />
                      </div>
                    )}
                    {entry.status === 'error' && (
                      <p className="text-xs text-red-500 flex items-center gap-1 mt-0.5">
                        <AlertCircle className="w-3 h-3" />
                        {entry.errorMsg}
                      </p>
                    )}
                  </div>

                  {/* Status icon / remove */}
                  <div className="shrink-0">
                    {entry.status === 'subiendo' && (
                      <Loader2 className="w-5 h-5 text-blue-500 animate-spin" />
                    )}
                    {entry.status === 'ok' && (
                      <CheckCircle className="w-5 h-5 text-green-500" />
                    )}
                    {entry.status === 'error' && (
                      <AlertCircle className="w-5 h-5 text-red-500" />
                    )}
                    {entry.status === 'pendiente' && !uploading && (
                      <button
                        onClick={() => removeFile(entry.id)}
                        className="p-1.5 rounded-lg text-gray-400 hover:text-red-500 hover:bg-red-50 transition-colors"
                        title="Quitar"
                      >
                        <X className="w-4 h-4" />
                      </button>
                    )}
                  </div>
                </li>
              ))}
            </ul>

            {/* Upload button */}
            <div className="px-4 py-3 border-t border-gray-100 flex justify-end">
              <button
                onClick={subirTodo}
                disabled={uploading || !hasAnyPending}
                className="inline-flex items-center gap-2 bg-blue-600 text-white px-5 py-2.5 rounded-lg text-sm font-semibold hover:bg-blue-700 disabled:opacity-60 disabled:cursor-not-allowed transition-colors"
              >
                {uploading ? (
                  <Loader2 className="w-4 h-4 animate-spin" />
                ) : (
                  <Upload className="w-4 h-4" />
                )}
                {uploading
                  ? 'Subiendo...'
                  : `Subir todo (${pendienteCount})`}
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
