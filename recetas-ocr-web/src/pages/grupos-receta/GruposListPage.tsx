import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { format } from 'date-fns'
import { Plus, ChevronLeft, ChevronRight, Eye } from 'lucide-react'
import { useQuery } from '@tanstack/react-query'
import { gruposRecetaApi } from '@/api/grupos-receta.api'
import { catalogosApi } from '@/api/catalogos.api'
import { PageHeader } from '@/components/common/PageHeader'
import { DataTable } from '@/components/common/DataTable'
import { StatusBadge } from '@/components/common/StatusBadge'
import { usePermisos } from '@/hooks/usePermisos'
import type { GrupoRecetaDto, FiltrosGrupoDto, EstadoGrupo } from '@/types/grupo-receta.types'
import type { Column } from '@/components/common/DataTable'

const ESTADOS_GRUPO: EstadoGrupo[] = [
  'RECIBIDO', 'REQUIERE_CAPTURA_MANUAL', 'PROCESANDO', 'GRUPO_INCOMPLETO',
  'REVISION_PENDIENTE', 'REVISADO_COMPLETO', 'DATOS_FISCALES_INCOMPLETOS',
  'PENDIENTE_AUTORIZACION', 'PENDIENTE_FACTURACION', 'PREFACTURA_GENERADA',
  'FACTURADA', 'ERROR_TIMBRADO_MANUAL', 'RECHAZADO',
]

const ESTADO_LABELS: Record<EstadoGrupo, string> = {
  RECIBIDO: 'Recibido',
  REQUIERE_CAPTURA_MANUAL: 'Captura Manual',
  PROCESANDO: 'Procesando',
  GRUPO_INCOMPLETO: 'Incompleto',
  REVISION_PENDIENTE: 'Revisión Pendiente',
  REVISADO_COMPLETO: 'Revisado',
  DATOS_FISCALES_INCOMPLETOS: 'Datos Fiscales Incompletos',
  PENDIENTE_AUTORIZACION: 'Pendiente Autorización',
  PENDIENTE_FACTURACION: 'Pendiente Facturación',
  PREFACTURA_GENERADA: 'Prefactura Generada',
  FACTURADA: 'Facturada',
  ERROR_TIMBRADO_MANUAL: 'Error Timbrado',
  RECHAZADO: 'Rechazado',
}

function useDebounce<T>(value: T, delay: number): T {
  const [debounced, setDebounced] = useState<T>(value)
  useEffect(() => {
    const t = setTimeout(() => setDebounced(value), delay)
    return () => clearTimeout(t)
  }, [value, delay])
  return debounced
}

const PAGE_SIZE = 15

export default function GruposListPage() {
  const navigate = useNavigate()
  const { puedeEscribir: puedeSubir } = usePermisos('IMAGENES.SUBIR')

  const [idAseguradora, setIdAseguradora] = useState<number | ''>('')
  const [estadoGrupo, setEstadoGrupo] = useState<EstadoGrupo | ''>('')
  const [busquedaInput, setBusquedaInput] = useState('')
  const [fechaDesde, setFechaDesde] = useState('')
  const [fechaHasta, setFechaHasta] = useState('')
  const [page, setPage] = useState(1)

  const busqueda = useDebounce(busquedaInput, 400)

  useEffect(() => { setPage(1) }, [idAseguradora, estadoGrupo, busqueda, fechaDesde, fechaHasta])

  const filtros: FiltrosGrupoDto = {
    ...(idAseguradora !== '' && { idAseguradora }),
    ...(estadoGrupo !== '' && { estadoGrupo }),
    ...(busqueda && { busqueda }),
    ...(fechaDesde && { fechaDesde }),
    ...(fechaHasta && { fechaHasta }),
    page,
    pageSize: PAGE_SIZE,
  }

  const { data, isLoading } = useQuery({
    queryKey: ['grupos-receta', 'list', filtros],
    queryFn: () => gruposRecetaApi.listar(filtros),
  })

  const { data: aseguradoras } = useQuery({
    queryKey: ['catalogos', 'aseguradoras'],
    queryFn: catalogosApi.getAseguradoras,
    staleTime: 1000 * 60 * 5,
  })

  const totalPages = data?.totalPages ?? 1

  const columns: Column<GrupoRecetaDto>[] = [
    { key: 'folioBase', header: 'Folio', render: r => r.folioBase ?? '—' },
    { key: 'nombreAseguradora', header: 'Aseguradora' },
    {
      key: 'nombrePaciente',
      header: 'Paciente',
      render: r =>
        [r.nombrePaciente, r.apellidoPaterno, r.apellidoMaterno].filter(Boolean).join(' ') || '—',
    },
    { key: 'nombreMedico', header: 'Médico', render: r => r.nombreMedico ?? '—' },
    {
      key: 'estadoGrupo',
      header: 'Estado',
      render: r => <StatusBadge estado={r.estadoGrupo} size="sm" />,
    },
    {
      key: 'totalImagenes',
      header: 'Imágenes',
      render: r => (
        <span className="inline-flex items-center justify-center w-6 h-6 rounded-full bg-gray-100 text-xs font-semibold text-gray-700">
          {r.totalImagenes}
        </span>
      ),
    },
    {
      key: 'totalMedicamentos',
      header: 'Medicamentos',
      render: r => (
        <span className="inline-flex items-center justify-center w-6 h-6 rounded-full bg-gray-100 text-xs font-semibold text-gray-700">
          {r.totalMedicamentos}
        </span>
      ),
    },
    {
      key: 'fechaCreacion',
      header: 'Fecha',
      render: r => format(new Date(r.fechaCreacion), 'dd/MM/yyyy HH:mm'),
    },
    {
      key: 'acciones',
      header: 'Acciones',
      render: r => (
        <button
          onClick={() => navigate(`/grupos-receta/${r.id}`)}
          className="inline-flex items-center gap-1 text-blue-600 hover:text-blue-800 text-xs font-medium transition-colors"
        >
          <Eye className="w-3.5 h-3.5" />
          Ver detalle
        </button>
      ),
    },
  ]

  return (
    <div className="p-6">
      <PageHeader
        title="Grupos de Receta"
        subtitle="Gestión y seguimiento de grupos de recetas médicas"
        actions={
          puedeSubir ? (
            <button
              onClick={() => navigate('/grupos-receta/nuevo')}
              className="inline-flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors"
            >
              <Plus className="w-4 h-4" />
              Nuevo Grupo
            </button>
          ) : undefined
        }
      />

      {/* Filters */}
      <div className="bg-white rounded-xl border border-gray-200 p-4 mb-6">
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-12 gap-3">
          <div className="lg:col-span-4">
            <label className="block text-xs font-medium text-gray-500 mb-1">Búsqueda</label>
            <input
              type="text"
              placeholder="Folio, paciente, médico..."
              value={busquedaInput}
              onChange={e => setBusquedaInput(e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <div className="lg:col-span-3">
            <label className="block text-xs font-medium text-gray-500 mb-1">Aseguradora</label>
            <select
              value={idAseguradora}
              onChange={e =>
                setIdAseguradora(e.target.value === '' ? '' : Number(e.target.value))
              }
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="">Todas</option>
              {aseguradoras?.map(a => (
                <option key={a.id} value={a.id}>
                  {a.nombre}
                </option>
              ))}
            </select>
          </div>

          <div className="lg:col-span-2">
            <label className="block text-xs font-medium text-gray-500 mb-1">Estado</label>
            <select
              value={estadoGrupo}
              onChange={e => setEstadoGrupo(e.target.value as EstadoGrupo | '')}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="">Todos</option>
              {ESTADOS_GRUPO.map(e => (
                <option key={e} value={e}>
                  {ESTADO_LABELS[e]}
                </option>
              ))}
            </select>
          </div>

          <div className="lg:col-span-3 grid grid-cols-2 gap-2">
            <div>
              <label className="block text-xs font-medium text-gray-500 mb-1">Desde</label>
              <input
                type="date"
                value={fechaDesde}
                onChange={e => setFechaDesde(e.target.value)}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-500 mb-1">Hasta</label>
              <input
                type="date"
                value={fechaHasta}
                onChange={e => setFechaHasta(e.target.value)}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>
        </div>
      </div>

      {/* Table */}
      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
        <DataTable
          columns={columns}
          data={data?.items ?? []}
          isLoading={isLoading}
          emptyMessage="No se encontraron grupos con los filtros seleccionados"
        />

        {!isLoading && (
          <div className="flex items-center justify-between px-4 py-3 border-t border-gray-200 bg-gray-50">
            <p className="text-sm text-gray-500">
              Página <span className="font-medium">{page}</span> de{' '}
              <span className="font-medium">{totalPages}</span> ·{' '}
              <span className="font-medium">{data?.total ?? 0}</span> registros
            </p>
            <div className="flex items-center gap-2">
              <button
                onClick={() => setPage(p => Math.max(1, p - 1))}
                disabled={page <= 1}
                className="inline-flex items-center gap-1 px-3 py-1.5 text-sm font-medium rounded-lg border border-gray-300 bg-white disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50 transition-colors"
              >
                <ChevronLeft className="w-4 h-4" />
                Anterior
              </button>
              <button
                onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                disabled={page >= totalPages}
                className="inline-flex items-center gap-1 px-3 py-1.5 text-sm font-medium rounded-lg border border-gray-300 bg-white disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50 transition-colors"
              >
                Siguiente
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
