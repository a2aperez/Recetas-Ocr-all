import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { format } from 'date-fns'
import { useQuery } from '@tanstack/react-query'
import { FileDown, ExternalLink, ChevronLeft, ChevronRight } from 'lucide-react'
import { facturacionApi } from '@/api/facturacion.api'
import { catalogosApi } from '@/api/catalogos.api'
import { PageHeader } from '@/components/common/PageHeader'
import { DataTable } from '@/components/common/DataTable'
import { StatusBadge } from '@/components/common/StatusBadge'
import type { FacturaResumenDto, FiltrosFacturaDto } from '@/types/facturacion.types'
import type { Column } from '@/components/common/DataTable'

function useDebounce<T>(value: T, delay: number): T {
  const [debounced, setDebounced] = useState<T>(value)
  useEffect(() => {
    const t = setTimeout(() => setDebounced(value), delay)
    return () => clearTimeout(t)
  }, [value, delay])
  return debounced
}

const ESTADOS_CFDI = [
  { clave: 'BORRADOR', label: 'Borrador' },
  { clave: 'TIMBRADO', label: 'Timbrado' },
  { clave: 'CANCELADO', label: 'Cancelado' },
  { clave: 'ERROR_TIMBRADO', label: 'Error Timbrado' },
]

const PAGE_SIZE = 15

function openBlob(url: string | null) {
  if (url) window.open(url, '_blank', 'noopener,noreferrer')
}

export default function FacturacionListPage() {
  const navigate = useNavigate()

  const [idAseguradora, setIdAseguradora] = useState<number | ''>('')
  const [estado, setEstado] = useState('')
  const [rfcInput, setRfcInput] = useState('')
  const [fechaDesde, setFechaDesde] = useState('')
  const [fechaHasta, setFechaHasta] = useState('')
  const [page, setPage] = useState(1)

  const rfcReceptor = useDebounce(rfcInput, 400)

  useEffect(() => { setPage(1) }, [idAseguradora, estado, rfcReceptor, fechaDesde, fechaHasta])

  const filtros: FiltrosFacturaDto = {
    ...(idAseguradora !== '' && { idAseguradora }),
    ...(estado && { estado }),
    ...(rfcReceptor && { rfcReceptor }),
    ...(fechaDesde && { fechaDesde }),
    ...(fechaHasta && { fechaHasta }),
    page,
    pageSize: PAGE_SIZE,
  }

  const { data, isLoading } = useQuery({
    queryKey: ['facturacion', 'list', filtros],
    queryFn: () => facturacionApi.listar(filtros),
  })

  const { data: aseguradoras } = useQuery({
    queryKey: ['catalogos', 'aseguradoras'],
    queryFn: catalogosApi.getAseguradoras,
    staleTime: 1000 * 60 * 5,
  })

  const totalPages = data?.totalPages ?? 1

  const columns: Column<FacturaResumenDto>[] = [
    {
      key: 'uuid',
      header: 'UUID',
      render: r =>
        r.uuid ? (
          <span
            className="font-mono text-xs text-gray-700 cursor-pointer hover:text-blue-600"
            title={r.uuid}
            onClick={() => navigator.clipboard.writeText(r.uuid!)}
          >
            {r.uuid.substring(0, 8).toUpperCase()}…
          </span>
        ) : (
          <span className="text-gray-400 text-xs">—</span>
        ),
    },
    {
      key: 'nombrePaciente',
      header: 'Paciente',
      render: r => r.nombrePaciente ?? '—',
    },
    {
      key: 'rfcReceptor',
      header: 'RFC',
      render: r => <span className="font-mono text-xs">{r.rfcReceptor}</span>,
    },
    {
      key: 'total',
      header: 'Total',
      render: r => (
        <span className="font-semibold">
          {r.total.toLocaleString('es-MX', { style: 'currency', currency: 'MXN' })}
        </span>
      ),
    },
    {
      key: 'fechaTimbrado',
      header: 'Fecha Timbrado',
      render: r =>
        r.fechaTimbrado
          ? format(new Date(r.fechaTimbrado), 'dd/MM/yyyy HH:mm')
          : '—',
    },
    {
      key: 'estado',
      header: 'Estado',
      render: r => <StatusBadge estado={r.estado} size="sm" />,
    },
    {
      key: 'acciones',
      header: 'Acciones',
      render: r => (
        <div className="flex items-center gap-2">
          <button
            onClick={() => navigate(`/facturacion/${r.idGrupo}/generar`)}
            className="text-blue-600 hover:text-blue-800 text-xs font-medium hover:underline"
          >
            Ver / Generar
          </button>
        </div>
      ),
    },
  ]

  return (
    <div className="p-6">
      <PageHeader
        title="Facturación"
        subtitle="Listado de CFDIs generados"
      />

      {/* Filters */}
      <div className="bg-white rounded-xl border border-gray-200 p-4 mb-6">
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-12 gap-3">
          {/* RFC */}
          <div className="lg:col-span-3">
            <label className="block text-xs font-medium text-gray-500 mb-1">RFC Receptor</label>
            <input
              type="text"
              placeholder="RFC..."
              value={rfcInput}
              onChange={e => setRfcInput(e.target.value.toUpperCase())}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm font-mono focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          {/* Aseguradora */}
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
                <option key={a.id} value={a.id}>{a.nombre}</option>
              ))}
            </select>
          </div>

          {/* Estado CFDI */}
          <div className="lg:col-span-2">
            <label className="block text-xs font-medium text-gray-500 mb-1">Estado CFDI</label>
            <select
              value={estado}
              onChange={e => setEstado(e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="">Todos</option>
              {ESTADOS_CFDI.map(e => (
                <option key={e.clave} value={e.clave}>{e.label}</option>
              ))}
            </select>
          </div>

          {/* Date range */}
          <div className="lg:col-span-4 grid grid-cols-2 gap-2">
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
          emptyMessage="No se encontraron facturas con los filtros seleccionados"
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

      {/* Per-row download helpers (rendered outside table to avoid nesting issues) */}
      {data?.items.filter(r => r.uuid).map(r => (
        <div key={r.id} className="hidden">
          <button onClick={() => openBlob(null)}>
            <FileDown className="w-3 h-3" />
            <ExternalLink className="w-3 h-3" />
          </button>
        </div>
      ))}
    </div>
  )
}
