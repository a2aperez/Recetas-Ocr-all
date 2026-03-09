import { useState, useEffect } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Search, ChevronLeft, ChevronRight } from 'lucide-react'
import { catalogosApi } from '@/api/catalogos.api'
import { PageHeader } from '@/components/common/PageHeader'
import { DataTable } from '@/components/common/DataTable'
import { StatusBadge } from '@/components/common/StatusBadge'
import type { Column } from '@/components/common/DataTable'
import type {
  AseguradoraDto,
  MedicamentoCatalogoDto,
} from '@/types/catalogos.types'

type Tab = 'aseguradoras' | 'medicamentos' | 'vias' | 'estados'

const TABS: { id: Tab; label: string }[] = [
  { id: 'aseguradoras', label: 'Aseguradoras' },
  { id: 'medicamentos', label: 'Medicamentos' },
  { id: 'vias', label: 'Vías de Administración' },
  { id: 'estados', label: 'Estados' },
]

function useDebounce<T>(value: T, delay: number): T {
  const [debounced, setDebounced] = useState<T>(value)
  useEffect(() => {
    const t = setTimeout(() => setDebounced(value), delay)
    return () => clearTimeout(t)
  }, [value, delay])
  return debounced
}

const PAGE_SIZE = 15

export default function CatalogosPage() {
  const [tab, setTab] = useState<Tab>('aseguradoras')
  const [busquedaInput, setBusquedaInput] = useState('')
  const [page, setPage] = useState(1)
  const busqueda = useDebounce(busquedaInput, 400)

  useEffect(() => { setPage(1) }, [busqueda])

  const { data: aseguradoras, isLoading: loadingAseg } = useQuery({
    queryKey: ['catalogos', 'aseguradoras'],
    queryFn: catalogosApi.getAseguradoras,
    staleTime: 1000 * 60 * 5,
  })

  const { data: medicamentos, isLoading: loadingMed } = useQuery({
    queryKey: ['catalogos', 'medicamentos', busqueda, page],
    queryFn: () => catalogosApi.getMedicamentos(busqueda || undefined, page, PAGE_SIZE),
    enabled: tab === 'medicamentos',
    staleTime: 1000 * 60 * 2,
  })

  const { data: vias, isLoading: loadingVias } = useQuery({
    queryKey: ['catalogos', 'vias'],
    queryFn: catalogosApi.getViasAdministracion,
    staleTime: 1000 * 60 * 10,
  })

  const { data: estadosImagen, isLoading: loadingEstImg } = useQuery({
    queryKey: ['catalogos', 'estados-imagen'],
    queryFn: catalogosApi.getEstadosImagen,
    staleTime: 1000 * 60 * 10,
  })

  const { data: estadosGrupo, isLoading: loadingEstGrupo } = useQuery({
    queryKey: ['catalogos', 'estados-grupo'],
    queryFn: catalogosApi.getEstadosGrupo,
    staleTime: 1000 * 60 * 10,
  })

  const colsAseg: Column<AseguradoraDto>[] = [
    {
      key: 'clave',
      header: 'Clave',
      render: r => (
        <span className="font-mono text-xs bg-gray-100 text-gray-700 px-2 py-0.5 rounded">
          {r.clave}
        </span>
      ),
    },
    { key: 'nombre', header: 'Nombre' },
    {
      key: 'razonSocial',
      header: 'Razón Social',
      render: r => r.razonSocial ?? <span className="text-gray-400">—</span>,
    },
    {
      key: 'rfc',
      header: 'RFC',
      render: r =>
        r.rfc ? (
          <span className="font-mono text-xs">{r.rfc}</span>
        ) : (
          <span className="text-gray-400">—</span>
        ),
    },
    {
      key: 'activo',
      header: 'Estado',
      render: r => (
        <span
          className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
            r.activo ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-700'
          }`}
        >
          {r.activo ? 'Activo' : 'Inactivo'}
        </span>
      ),
    },
  ]

  const colsMed: Column<MedicamentoCatalogoDto>[] = [
    { key: 'nombreComercial', header: 'Nombre Comercial' },
    {
      key: 'sustanciaActiva',
      header: 'Sustancia Activa',
      render: r => r.sustanciaActiva ?? <span className="text-gray-400">—</span>,
    },
    {
      key: 'presentacion',
      header: 'Presentación',
      render: r => r.presentacion ?? <span className="text-gray-400">—</span>,
    },
    {
      key: 'concentracion',
      header: 'Concentración',
      render: r => r.concentracion ?? <span className="text-gray-400">—</span>,
    },
  ]

  const totalPages = medicamentos?.totalPages ?? 1

  return (
    <div className="p-6">
      <PageHeader
        title="Catálogos"
        subtitle="Catálogos de referencia del sistema (solo lectura)"
      />

      {/* Tab Nav */}
      <div className="flex gap-1 border-b border-gray-200 mb-6">
        {TABS.map(t => (
          <button
            key={t.id}
            onClick={() => setTab(t.id)}
            className={`px-4 py-2.5 text-sm font-medium border-b-2 transition-colors ${
              tab === t.id
                ? 'border-blue-600 text-blue-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
            }`}
          >
            {t.label}
          </button>
        ))}
      </div>

      {/* TAB: Aseguradoras */}
      {tab === 'aseguradoras' && (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <DataTable
            columns={colsAseg}
            data={aseguradoras ?? []}
            isLoading={loadingAseg}
            emptyMessage="No hay aseguradoras registradas"
          />
        </div>
      )}

      {/* TAB: Medicamentos */}
      {tab === 'medicamentos' && (
        <div>
          <div className="bg-white rounded-xl border border-gray-200 p-4 mb-4">
            <div className="relative max-w-sm">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400 pointer-events-none" />
              <input
                type="text"
                placeholder="Buscar por nombre, sustancia o código..."
                value={busquedaInput}
                onChange={e => setBusquedaInput(e.target.value)}
                className="w-full pl-9 pr-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>

          <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
            <DataTable
              columns={colsMed}
              data={medicamentos?.items ?? []}
              isLoading={loadingMed}
              emptyMessage="No se encontraron medicamentos"
            />

            {!loadingMed && (
              <div className="flex items-center justify-between px-4 py-3 border-t border-gray-200 bg-gray-50">
                <p className="text-sm text-gray-500">
                  Página <span className="font-medium">{page}</span> de{' '}
                  <span className="font-medium">{totalPages}</span> ·{' '}
                  <span className="font-medium">{medicamentos?.total ?? 0}</span> registros
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
      )}

      {/* TAB: Vías de Administración */}
      {tab === 'vias' && (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          {loadingVias ? (
            <div className="p-6 space-y-3">
              {Array.from({ length: 8 }).map((_, i) => (
                <div key={i} className="h-4 bg-gray-200 rounded animate-pulse" />
              ))}
            </div>
          ) : (
            <table className="min-w-full divide-y divide-gray-200 text-sm">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider w-32">
                    Clave
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">
                    Nombre
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100 bg-white">
                {(vias ?? []).length === 0 ? (
                  <tr>
                    <td colSpan={2} className="px-4 py-10 text-center text-gray-400">
                      Sin vías de administración registradas
                    </td>
                  </tr>
                ) : (
                  (vias ?? []).map(v => (
                    <tr key={v.id} className="hover:bg-gray-50">
                      <td className="px-4 py-3">
                        <span className="font-mono text-xs bg-gray-100 text-gray-700 px-2 py-0.5 rounded">
                          {v.clave}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-gray-700">{v.nombre}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          )}
        </div>
      )}

      {/* TAB: Estados */}
      {tab === 'estados' && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <div className="bg-white rounded-xl border border-gray-200 p-5">
            <h3 className="text-sm font-semibold text-gray-700 mb-4 pb-2 border-b border-gray-100">
              Estados de Imagen
            </h3>
            {loadingEstImg ? (
              <div className="space-y-2">
                {Array.from({ length: 8 }).map((_, i) => (
                  <div key={i} className="h-8 bg-gray-200 rounded animate-pulse" />
                ))}
              </div>
            ) : (
              <div className="divide-y divide-gray-100">
                {(estadosImagen ?? []).map(e => (
                  <div key={e.clave} className="flex items-center gap-3 py-2.5">
                    <StatusBadge estado={e.clave} />
                    <span className="text-sm text-gray-500">{e.nombre}</span>
                  </div>
                ))}
              </div>
            )}
          </div>

          <div className="bg-white rounded-xl border border-gray-200 p-5">
            <h3 className="text-sm font-semibold text-gray-700 mb-4 pb-2 border-b border-gray-100">
              Estados de Grupo
            </h3>
            {loadingEstGrupo ? (
              <div className="space-y-2">
                {Array.from({ length: 6 }).map((_, i) => (
                  <div key={i} className="h-8 bg-gray-200 rounded animate-pulse" />
                ))}
              </div>
            ) : (
              <div className="divide-y divide-gray-100">
                {(estadosGrupo ?? []).map(e => (
                  <div key={e.clave} className="flex items-center gap-3 py-2.5">
                    <StatusBadge estado={e.clave} />
                    <span className="text-sm text-gray-500">{e.nombre}</span>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  )
}
