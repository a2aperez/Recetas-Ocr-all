import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { format } from 'date-fns'
import { ClipboardCheck, ChevronLeft, ChevronRight } from 'lucide-react'
import { revisionApi } from '@/api/revision.api'
import { PageHeader } from '@/components/common/PageHeader'
import { StatusBadge } from '@/components/common/StatusBadge'
import { DataTable } from '@/components/common/DataTable'
import type { ItemColaRevisionDto } from '@/api/revision.api'
import type { Column } from '@/components/common/DataTable'

const PAGE_SIZE = 20

function ConfidenceBar({ value }: { value: number | null }) {
  if (value === null) return <span className="text-xs text-gray-400">—</span>
  const pct = Math.round(value * 100)
  const color =
    pct >= 80 ? 'bg-green-500' : pct >= 60 ? 'bg-yellow-500' : 'bg-red-500'
  const textColor =
    pct >= 80 ? 'text-green-700' : pct >= 60 ? 'text-yellow-700' : 'text-red-700'
  return (
    <div className="flex items-center gap-2 min-w-[90px]">
      <div className="flex-1 h-2 bg-gray-200 rounded-full overflow-hidden">
        <div className={`h-2 rounded-full transition-all ${color}`} style={{ width: `${pct}%` }} />
      </div>
      <span className={`text-xs font-medium tabular-nums ${textColor}`}>{pct}%</span>
    </div>
  )
}

export default function ColaRevisionPage() {
  const navigate = useNavigate()
  const [page, setPage] = useState(1)

  const { data, isLoading } = useQuery({
    queryKey: ['revision', 'cola', page],
    queryFn: () => revisionApi.getCola(page, PAGE_SIZE),
    refetchInterval: 10_000,
  })

  const totalPages = data?.totalPages ?? 1

  const columns: Column<ItemColaRevisionDto>[] = [
    {
      key: 'folioGrupo',
      header: 'Grupo / Folio',
      render: r => (
        <span className="font-mono text-xs font-semibold text-gray-800">
          {r.folioGrupo ?? 'Sin folio'}
        </span>
      ),
    },
    {
      key: 'nombreAseguradora',
      header: 'Aseguradora',
      render: r => r.nombreAseguradora ?? '—',
    },
    {
      key: 'numeroHoja',
      header: 'Hoja #',
      render: r => (
        <span className="inline-flex items-center justify-center w-7 h-7 rounded-full bg-gray-100 text-xs font-semibold text-gray-700">
          {r.numeroHoja}
        </span>
      ),
    },
    {
      key: 'confianzaPromedio',
      header: 'Confianza OCR',
      render: r => <ConfidenceBar value={r.confianzaPromedio} />,
    },
    {
      key: 'estadoImagen',
      header: 'Estado',
      render: r => <StatusBadge estado={r.estadoImagen} size="sm" />,
    },
    {
      key: 'fechaEncolado',
      header: 'Encolado',
      render: r =>
        r.fechaEncolado
          ? format(new Date(r.fechaEncolado), 'dd/MM/yyyy HH:mm')
          : '—',
    },
    {
      key: 'accion',
      header: 'Acción',
      render: r => (
        <button
          onClick={() => navigate(`/revision/${r.idImagen}`)}
          className="inline-flex items-center gap-1.5 bg-blue-600 text-white px-3 py-1.5 rounded-lg text-xs font-semibold hover:bg-blue-700 transition-colors"
        >
          <ClipboardCheck className="w-3.5 h-3.5" />
          Revisar
        </button>
      ),
    },
  ]

  return (
    <div className="p-6">
      <PageHeader
        title="Cola de Revisión"
        subtitle={`${data?.total ?? '…'} imágenes pendientes · actualización automática cada 10 s`}
      />

      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
        <DataTable
          columns={columns}
          data={data?.items ?? []}
          isLoading={isLoading}
          emptyMessage="No hay imágenes pendientes de revisión"
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
