import { useState, useCallback, useEffect, useRef } from 'react'
import { useSearchParams, useNavigate } from 'react-router-dom'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft, Upload, Camera, Images, X, CheckCircle, AlertCircle } from 'lucide-react'
import { gruposRecetaApi } from '@/api/grupos-receta.api'
import { imagenesApi } from '@/api/imagenes.api'
import { CamaraCaptura } from '@/components/imagenes/CamaraCaptura'
import { GaleriaImport } from '@/components/imagenes/GaleriaImport'
import { PageHeader } from '@/components/common/PageHeader'
import toast from 'react-hot-toast'
import type { ImagenConOcrDto } from '@/types/imagen.types'

// ─── Types ───────────────────────────────────────────────────────────────────

interface ArchivoEnCola {
  uid: string
  file: File
  origen: 'CAMARA' | 'GALERIA'
  estado: 'pendiente' | 'subiendo' | 'ocr' | 'completado' | 'error'
  progreso: number
  error?: string
  resultado?: ImagenConOcrDto
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
            <div className="w-7 h-7 border-2 border-blue-500 border-t-transparent rounded-full animate-spin" />
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

// ─── OCR Loading Overlay ──────────────────────────────────────────────────────

function OcrOverlay({ progreso }: { progreso: number }) {
  const label =
    progreso < 20 ? 'Subiendo imagen...' :
    progreso < 90 ? 'Extrayendo datos de la receta...' :
    'Guardando resultados...'

  return (
    <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center">
      <div className="bg-white rounded-xl p-8 max-w-sm w-full mx-4 text-center shadow-2xl">
        <div className="w-16 h-16 border-4 border-blue-500 border-t-transparent rounded-full animate-spin mx-auto mb-4" />
        <h3 className="text-lg font-semibold text-gray-900 mb-2">Procesando OCR...</h3>
        <p className="text-sm text-gray-500 mb-4">
          Analizando la receta médica con inteligencia artificial.
          Esto puede tomar entre 10 y 30 segundos.
        </p>
        <div className="w-full bg-gray-200 rounded-full h-2">
          <div
            className="bg-blue-500 h-2 rounded-full transition-all duration-500"
            style={{ width: `${progreso}%` }}
          />
        </div>
        <p className="text-xs text-gray-400 mt-2">{label}</p>
      </div>
    </div>
  )
}

// ─── PAGE ────────────────────────────────────────────────────────────────────

export default function SubirImagenPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const qc = useQueryClient()

  const [idGrupoOverride, setIdGrupoOverride] = useState<string | null>(null)
  const idGrupo = searchParams.get('idGrupo') ?? idGrupoOverride

  const [activeTab, setActiveTab] = useState<TabId>('galeria')
  const [archivos, setArchivos] = useState<ArchivoEnCola[]>([])
  const [subiendo, setSubiendo] = useState(false)
  const [progreso, setProgreso] = useState(0)

  // Interval ref for simulated OCR progress
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null)

  // Simulate progress from 20% up to 88% during OCR wait
  useEffect(() => {
    if (subiendo) {
      intervalRef.current = setInterval(() => {
        setProgreso(prev => prev < 88 ? Math.min(prev + 5, 88) : prev)
      }, 800)
    } else {
      if (intervalRef.current) {
        clearInterval(intervalRef.current)
        intervalRef.current = null
      }
    }
    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current)
    }
  }, [subiendo])

  // ── Helpers ───────────────────────────────────────────────────────────────

  function setArchivoEstado(
    uid: string,
    estado: ArchivoEnCola['estado'],
    prog = 0,
    resultado?: ImagenConOcrDto,
    error?: string
  ) {
    setArchivos(prev =>
      prev.map(a => a.uid === uid ? { ...a, estado, progreso: prog, resultado, error } : a)
    )
  }

  const addFiles = useCallback((files: File[], origen: 'CAMARA' | 'GALERIA') => {
    setArchivos(prev => {
      const existingKeys = new Set(prev.map(a => `${a.file.name}-${a.file.size}`))
      const nuevos: ArchivoEnCola[] = files
        .filter(f => !existingKeys.has(`${f.name}-${f.size}`))
        .map(f => ({
          uid: crypto.randomUUID(),
          file: f,
          origen,
          estado: 'pendiente',
          progreso: 0,
          preview: makePreview(f),
        }))
      return [...prev, ...nuevos]
    })
  }, [])

  const onCaptura = useCallback((file: File) => {
    addFiles([file], 'CAMARA')
    setActiveTab('galeria')
  }, [addFiles])

  const onArchivosGaleria = useCallback((files: File[]) => {
    setArchivos(prev => {
      const cameraItems = prev.filter(a => a.origen === 'CAMARA')
      const existingGaleria = new Set(
        prev.filter(a => a.origen === 'GALERIA').map(a => `${a.file.name}-${a.file.size}`)
      )
      const galeriaItems: ArchivoEnCola[] = files.map(f => {
        const existing = prev.find(
          a => a.origen === 'GALERIA' && a.file.name === f.name && a.file.size === f.size
        )
        return existing ?? {
          uid: crypto.randomUUID(),
          file: f,
          origen: 'GALERIA',
          estado: 'pendiente',
          progreso: 0,
          preview: makePreview(f),
        }
      })
      void existingGaleria
      return [...cameraItems, ...galeriaItems]
    })
  }, [])

  // ── Upload ────────────────────────────────────────────────────────────────

  async function handleSubirTodo() {
    if (!idGrupo) return
    const pendientes = archivos.filter(a => a.estado === 'pendiente')
    if (pendientes.length === 0) return

    setSubiendo(true)
    setProgreso(0)

    // Process one at a time; navigate immediately after the first
    for (const archivo of pendientes) {
      setArchivoEstado(archivo.uid, 'subiendo', 0)

      try {
        const resultado = await imagenesApi.subir(
          idGrupo,
          archivo.file,
          archivo.origen,
          (pct) => setProgreso(pct) // 0-20 from upload progress
        )

        setProgreso(100)
        setArchivoEstado(archivo.uid, 'completado', 100, resultado)

        // Pre-populate imagen cache so revision page has immediate data
        qc.setQueryData(['imagenes', resultado.id], resultado)

        // Invalidate caches to force fresh fetch of related data
        qc.invalidateQueries({ queryKey: ['imagenes', 'by-grupo', idGrupo] })
        qc.invalidateQueries({ queryKey: ['grupos-receta', 'detail', idGrupo] })

        setSubiendo(false)

        if (resultado.datosOcr !== null) {
          const confianza = resultado.datosOcr.confianzaPromedio
          if (resultado.estadoImagen === 'ILEGIBLE') {
            toast.error('Imagen ilegible — completa los datos manualmente')
            navigate(`/revision/${resultado.id}?fromUpload=true&manual=true`)
          } else if (resultado.datosOcr.esConfianzaBaja) {
            toast(`OCR completado con confianza ${confianza.toFixed(0)}% — revisa los datos`, { icon: '⚠️' })
            navigate(`/revision/${resultado.id}?fromUpload=true`)
          } else {
            toast.success(`✓ OCR exitoso (${confianza.toFixed(0)}% confianza) — confirma los datos`)
            navigate(`/revision/${resultado.id}?fromUpload=true`)
          }
        } else {
          toast('Imagen subida. El OCR se procesará en breve.', { icon: '⚠️' })
          navigate(`/grupos-receta/${idGrupo}`)
        }

        return // Navigate after first — don't batch

      } catch (err: unknown) {
        const msg = err instanceof Error ? err.message : 'Error al subir'
        setArchivoEstado(archivo.uid, 'error', 0, undefined, msg)
        toast.error(`Error: ${msg}`)
        setSubiendo(false)
        return
      }
    }
  }

  // ── No group selected ─────────────────────────────────────────────────────

  if (!idGrupo) {
    return <GrupoSelector onSelect={id => setIdGrupoOverride(id)} />
  }

  // ── Derived state ─────────────────────────────────────────────────────────

  const pendienteCount = archivos.filter(a => a.estado === 'pendiente').length
  const okCount        = archivos.filter(a => a.estado === 'completado').length
  const errorCount     = archivos.filter(a => a.estado === 'error').length

  // ── Render ────────────────────────────────────────────────────────────────

  return (
    <>
      {/* OCR overlay */}
      {subiendo && <OcrOverlay progreso={progreso} />}

      <div className="p-6 max-w-2xl mx-auto">
        <PageHeader
          title="Subir Imagen"
          subtitle={`Grupo: ${idGrupo.slice(0, 8)}...`}
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
            <div className="flex border-b border-gray-200">
              {(['galeria', 'camara'] as TabId[]).map(t => (
                <button
                  key={t}
                  onClick={() => setActiveTab(t)}
                  className={`flex-1 flex items-center justify-center gap-2 py-3 text-sm font-medium border-b-2 transition-colors ${
                    activeTab === t
                      ? 'border-blue-600 text-blue-600 bg-blue-50/50'
                      : 'border-transparent text-gray-500 hover:text-gray-700'
                  }`}
                >
                  {t === 'camara' ? (
                    <><Camera className="w-4 h-4" /> Cámara</>
                  ) : (
                    <><Images className="w-4 h-4" /> Galería</>
                  )}
                </button>
              ))}
            </div>
            <div className="p-5">
              {activeTab === 'camara' ? (
                <CamaraCaptura onCaptura={onCaptura} onCancelar={() => setActiveTab('galeria')} />
              ) : (
                <GaleriaImport onArchivos={onArchivosGaleria} />
              )}
            </div>
          </div>

          {/* Queue list */}
          {archivos.length > 0 && (
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
                {!subiendo && okCount > 0 && (
                  <button
                    onClick={() => setArchivos(a => a.filter(x => x.estado !== 'completado'))}
                    className="text-xs text-gray-400 hover:text-red-500 transition-colors"
                  >
                    Limpiar completados
                  </button>
                )}
              </div>

              <ul className="divide-y divide-gray-100">
                {archivos.map(archivo => (
                  <li key={archivo.uid} className="flex items-center gap-3 p-3">
                    {/* Thumbnail */}
                    <div className="w-12 h-12 rounded-lg overflow-hidden bg-gray-100 shrink-0 flex items-center justify-center">
                      {archivo.preview ? (
                        <img
                          src={archivo.preview}
                          alt={archivo.file.name}
                          className="w-full h-full object-cover"
                        />
                      ) : (
                        <Upload className="w-5 h-5 text-gray-400" />
                      )}
                    </div>

                    {/* Info */}
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 mb-0.5">
                        <p className="text-sm font-medium text-gray-800 truncate">
                          {archivo.file.name}
                        </p>
                        <span className="text-xs text-gray-400 shrink-0">
                          {formatBytes(archivo.file.size)}
                        </span>
                        <span className="text-xs bg-gray-100 text-gray-500 px-1.5 py-0.5 rounded shrink-0">
                          {archivo.origen === 'CAMARA' ? 'Cámara' : 'Galería'}
                        </span>
                      </div>
                      {archivo.estado === 'error' && (
                        <p className="text-xs text-red-500 flex items-center gap-1 mt-0.5">
                          <AlertCircle className="w-3 h-3" />
                          {archivo.error}
                        </p>
                      )}
                      {archivo.estado === 'pendiente' && (
                        <p className="text-xs text-gray-400 mt-0.5">En espera</p>
                      )}
                      {(archivo.estado === 'subiendo' || archivo.estado === 'ocr') && (
                        <p className="text-xs text-blue-500 animate-pulse mt-0.5">
                          {archivo.progreso < 20 ? 'Subiendo...' : 'Procesando OCR...'}
                        </p>
                      )}
                    </div>

                    {/* Status icon / remove */}
                    <div className="shrink-0">
                      {archivo.estado === 'completado' && (
                        <CheckCircle className="w-5 h-5 text-green-500" />
                      )}
                      {archivo.estado === 'error' && (
                        <AlertCircle className="w-5 h-5 text-red-500" />
                      )}
                      {archivo.estado === 'pendiente' && !subiendo && (
                        <button
                          onClick={() => setArchivos(a => a.filter(x => x.uid !== archivo.uid))}
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

              {/* Footer */}
              <div className="px-4 py-3 border-t border-gray-100 flex justify-end">
                <button
                  onClick={handleSubirTodo}
                  disabled={subiendo || pendienteCount === 0}
                  className="inline-flex items-center gap-2 bg-blue-600 text-white px-5 py-2.5 rounded-lg text-sm font-semibold hover:bg-blue-700 disabled:opacity-60 disabled:cursor-not-allowed transition-colors"
                >
                  <Upload className="w-4 h-4" />
                  ⬆ Subir {pendienteCount} imagen{pendienteCount !== 1 ? 'es' : ''}
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </>
  )
}

