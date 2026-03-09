import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { format } from 'date-fns'
import { FileText, Eye, Receipt, Cpu, Image, Plus, Camera, Search } from 'lucide-react'
import { gruposRecetaApi } from '@/api/grupos-receta.api'
import { facturacionApi } from '@/api/facturacion.api'
import { ocrApi } from '@/api/ocr.api'
import { PageHeader } from '@/components/common/PageHeader'
import { StatusBadge } from '@/components/common/StatusBadge'
import { DataTable } from '@/components/common/DataTable'
import ErrorBoundary from '@/components/common/ErrorBoundary'
import { usePermisos } from '@/hooks/usePermisos'
import { useAuthStore } from '@/store/auth.store'
import type { GrupoRecetaDto } from '@/types/grupo-receta.types'
import type { Column } from '@/components/common/DataTable'
import type { ReactNode } from 'react'

const today = format(new Date(), 'yyyy-MM-dd')
const REFETCH_MS = 30_000

// ── Metric Card ───────────────────────────────────────────────────────────────

interface MetricCardProps {
  label: string
  value: number | undefined
  icon: ReactNode
  iconBg: string
  valueColor: string
  alert?: boolean
  procesando?: boolean
  isError?: boolean
}

function MetricCard({ label, value, icon, iconBg, valueColor, alert, procesando, isError }: MetricCardProps) {
  return (
    <div className="bg-white rounded-xl border border-gray-200 p-5 shadow-sm flex items-start justify-between gap-3">
      <div className="flex flex-col gap-1 min-w-0">
        <p className="text-xs font-medium text-gray-500 uppercase tracking-wide">{label}</p>
        <div className="flex items-end gap-2">
          <p className={`text-3xl font-bold leading-none ${valueColor}`}>
            {isError ? (
              <span title="Error al cargar datos" className="text-red-400 text-2xl">!</span>
            ) : value === undefined ? (
              <span className="inline-block w-10 h-8 bg-gray-200 rounded animate-pulse" />
            ) : (
              value
            )}
          </p>
          {alert && !!value && value > 0 && (
            <span className="mb-0.5 inline-flex h-2 w-2 rounded-full bg-red-500 animate-ping" />
          )}
        </div>
        {procesando && !!value && value > 0 && (
          <p className="text-xs text-purple-500 flex items-center gap-1">
            <span className="inline-block w-3 h-3 border-2 border-purple-400 border-t-transparent rounded-full animate-spin" />
            procesando...
          </p>
        )}
      </div>
      <div className={`shrink-0 inline-flex items-center justify-center w-10 h-10 rounded-xl ${iconBg}`}>
        {icon}
      </div>
    </div>
  )
}

// ── Table columns ─────────────────────────────────────────────────────────────

function buildColumns(navigate: ReturnType<typeof useNavigate>): Column<GrupoRecetaDto>[] {
  return [
    {
      key: 'folio',
      header: 'Folio',
      render: (r) =>
        r.folioBase ? (
          <span className="font-mono text-xs">{r.folioBase}</span>
        ) : (
          <span className="font-mono text-xs text-gray-400">{r.id.slice(0, 8)}</span>
        ),
    },
    { key: 'nombreAseguradora', header: 'Aseguradora' },
    {
      key: 'paciente',
      header: 'Paciente',
      render: (r) =>
        [r.nombrePaciente, r.apellidoPaterno, r.apellidoMaterno].filter(Boolean).join(' ') ||
        <span className="text-gray-400">Sin nombre</span>,
    },
    {
      key: 'estadoGrupo',
      header: 'Estado',
      render: (r) => <StatusBadge estado={r.estadoGrupo} size="sm" />,
    },
    {
      key: 'totalImagenes',
      header: 'Imágenes',
      render: (r) => (
        <span className="inline-flex items-center gap-1 text-gray-600">
          <Image className="w-3.5 h-3.5 text-gray-400" />
          {r.totalImagenes}
        </span>
      ),
    },
    {
      key: 'fechaCreacion',
      header: 'Fecha',
      render: (r) => format(new Date(r.fechaCreacion), 'dd/MM/yyyy HH:mm'),
    },
    {
      key: 'acciones',
      header: '',
      render: (r) => (
        <button
          onClick={() => navigate(`/grupos-receta/${r.id}`)}
          className="text-blue-600 hover:text-blue-800 text-xs font-medium transition-colors"
        >
          Ver
        </button>
      ),
    },
  ]
}

// ── Page ──────────────────────────────────────────────────────────────────────

export default function DashboardPage() {
  const navigate = useNavigate()
  const usuario = useAuthStore((s) => s.usuario)
  const { puedeEscribir: puedeSubirImagen } = usePermisos('IMAGENES.SUBIR')
  const { puedeLeer: puedeVerRevision } = usePermisos('REVISION.VER')

  const { data: gruposHoy, isError: errorGruposHoy } = useQuery({
    queryKey: ['dashboard', 'grupos-hoy'],
    queryFn: () => gruposRecetaApi.listar({ fechaDesde: today, fechaHasta: today, page: 1, pageSize: 1 }),
    refetchInterval: REFETCH_MS,
    retry: false,
  })

  const { data: enRevision, isError: errorEnRevision } = useQuery({
    queryKey: ['dashboard', 'en-revision'],
    queryFn: () => gruposRecetaApi.listar({ estadoGrupo: 'REVISION_PENDIENTE', page: 1, pageSize: 1 }),
    refetchInterval: REFETCH_MS,
    retry: false,
  })

  const { data: facturadosHoy, isError: errorFacturados } = useQuery({
    queryKey: ['dashboard', 'facturados-hoy'],
    queryFn: () => facturacionApi.listar({ fechaDesde: today, fechaHasta: today, page: 1, pageSize: 1 }),
    refetchInterval: REFETCH_MS,
    retry: false,
  })

  const { data: colaOcr, isError: errorColaOcr } = useQuery({
    queryKey: ['dashboard', 'cola-ocr'],
    queryFn: () => ocrApi.getCola('PENDIENTE', 1, 1),
    refetchInterval: REFETCH_MS,
    retry: false,
  })

  const { data: recentData, isLoading: loadingRecent } = useQuery({
    queryKey: ['grupos-receta', 'list', { page: 1, pageSize: 10 }],
    queryFn: () => gruposRecetaApi.listar({ page: 1, pageSize: 10 }),
    refetchInterval: REFETCH_MS,
  })

  const columns = buildColumns(navigate)
  const revisionCount = enRevision?.total ?? 0

  return (
    <ErrorBoundary fallback={
      <div className="p-6 text-center text-gray-600">
        Error cargando dashboard.{' '}
        <button
          onClick={() => window.location.reload()}
          className="text-blue-600 underline hover:text-blue-800"
        >
          Reintentar
        </button>
      </div>
    }>
    <div className="p-6 space-y-8">
      <PageHeader
        title="Dashboard"
        subtitle={`Bienvenido, ${usuario?.nombreCompleto ?? ''}`}
      />

      {/* ── Métricas ──────────────────────────────────────────────────────── */}
      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
        <MetricCard
          label="Grupos hoy"
          value={gruposHoy?.total}
          icon={<FileText className="w-5 h-5 text-blue-600" />}
          iconBg="bg-blue-50"
          valueColor="text-blue-600"
          isError={errorGruposHoy}
        />
        <MetricCard
          label="En revisión"
          value={enRevision?.total}
          icon={<Eye className="w-5 h-5 text-yellow-600" />}
          iconBg="bg-yellow-50"
          valueColor="text-yellow-600"
          alert
          isError={errorEnRevision}
        />
        <MetricCard
          label="Facturados hoy"
          value={facturadosHoy?.total}
          icon={<Receipt className="w-5 h-5 text-green-600" />}
          iconBg="bg-green-50"
          valueColor="text-green-600"
          isError={errorFacturados}
        />
        <MetricCard
          label="Cola OCR pendiente"
          value={colaOcr?.total}
          icon={<Cpu className="w-5 h-5 text-purple-600" />}
          iconBg="bg-purple-50"
          valueColor="text-purple-600"
          procesando
          isError={errorColaOcr}
        />
      </div>

      {/* ── Últimos grupos ────────────────────────────────────────────────── */}
      <div>
        <h2 className="text-sm font-semibold text-gray-700 mb-3 uppercase tracking-wide">
          Últimos grupos
        </h2>
        <DataTable<GrupoRecetaDto>
          columns={columns}
          data={recentData?.items ?? []}
          isLoading={loadingRecent}
          emptyMessage="No hay grupos registrados"
        />
      </div>

      {/* ── Acciones rápidas ──────────────────────────────────────────────── */}
      {(puedeSubirImagen || puedeVerRevision) && (
        <div>
          <h2 className="text-sm font-semibold text-gray-700 mb-3 uppercase tracking-wide">
            Acciones rápidas
          </h2>
          <div className="flex flex-wrap gap-3">
            {puedeSubirImagen && (
              <>
                <button
                  onClick={() => navigate('/grupos-receta/nuevo')}
                  className="inline-flex items-center gap-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium px-4 py-2 rounded-lg transition-colors"
                >
                  <Plus className="w-4 h-4" />
                  Nuevo Grupo
                </button>
                <button
                  onClick={() => navigate('/imagenes/subir')}
                  className="inline-flex items-center gap-2 bg-white hover:bg-gray-50 text-gray-700 text-sm font-medium px-4 py-2 rounded-lg border border-gray-300 transition-colors"
                >
                  <Camera className="w-4 h-4" />
                  Subir Imagen
                </button>
              </>
            )}
            {puedeVerRevision && (
              <button
                onClick={() => navigate('/revision')}
                className="inline-flex items-center gap-2 bg-white hover:bg-gray-50 text-gray-700 text-sm font-medium px-4 py-2 rounded-lg border border-gray-300 transition-colors"
              >
                <Search className="w-4 h-4" />
                Cola Revisión
                {revisionCount > 0 && (
                  <span className="inline-flex items-center justify-center w-5 h-5 rounded-full bg-red-500 text-white text-xs font-bold">
                    {revisionCount > 99 ? '99+' : revisionCount}
                  </span>
                )}
              </button>
            )}
          </div>
        </div>
      )}
    </div>
    </ErrorBoundary>
  )
}
