import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { format } from 'date-fns';
import { gruposRecetaApi } from '@/api/grupos-receta.api';
import { facturacionApi } from '@/api/facturacion.api';
import { ocrApi } from '@/api/ocr.api';
import { PageHeader } from '@/components/common/PageHeader';
import { StatusBadge } from '@/components/common/StatusBadge';
import { DataTable } from '@/components/common/DataTable';
import { usePermisos } from '@/hooks/usePermisos';
import type { GrupoRecetaDto } from '@/types/grupo-receta.types';
import type { Column } from '@/components/common/DataTable';

const today = format(new Date(), 'yyyy-MM-dd');
const REFETCH_MS = 30_000;

function MetricCard({ label, value, color }: { label: string; value: number | undefined; color: string }) {
  return (
    <div className={`bg-white rounded-xl border border-gray-200 p-5 flex flex-col gap-1 shadow-sm`}>
      <p className="text-xs font-medium text-gray-500 uppercase tracking-wide">{label}</p>
      <p className={`text-3xl font-bold ${color}`}>
        {value === undefined ? (
          <span className="inline-block w-12 h-8 bg-gray-200 rounded animate-pulse" />
        ) : (
          value
        )}
      </p>
    </div>
  );
}

const COLUMNS: Column<GrupoRecetaDto>[] = [
  { key: 'folioBase', header: 'Folio', render: (r) => r.folioBase ?? '—' },
  { key: 'nombreAseguradora', header: 'Aseguradora' },
  {
    key: 'nombrePaciente',
    header: 'Paciente',
    render: (r) =>
      [r.nombrePaciente, r.apellidoPaterno, r.apellidoMaterno].filter(Boolean).join(' ') || '—',
  },
  {
    key: 'estadoGrupo',
    header: 'Estado',
    render: (r) => <StatusBadge estado={r.estadoGrupo} size="sm" />,
  },
  {
    key: 'fechaCreacion',
    header: 'Fecha',
    render: (r) => format(new Date(r.fechaCreacion), 'dd/MM/yyyy HH:mm'),
  },
];

export default function DashboardPage() {
  const navigate = useNavigate();
  const { puedeLeer: puedeSubir } = usePermisos('IMAGENES.SUBIR');

  const { data: gruposHoy } = useQuery({
    queryKey: ['grupos-receta', 'hoy'],
    queryFn: () => gruposRecetaApi.listar({ fechaDesde: today, fechaHasta: today, page: 1, pageSize: 1 }),
    refetchInterval: REFETCH_MS,
  });

  const { data: enRevision } = useQuery({
    queryKey: ['grupos-receta', 'revision'],
    queryFn: () => gruposRecetaApi.listar({ estadoGrupo: 'REVISION_PENDIENTE', page: 1, pageSize: 1 }),
    refetchInterval: REFETCH_MS,
  });

  const { data: facturadosHoy } = useQuery({
    queryKey: ['facturacion', 'hoy'],
    queryFn: () => facturacionApi.listar({ fechaDesde: today, page: 1, pageSize: 1 }),
    refetchInterval: REFETCH_MS,
  });

  const { data: colaOcr } = useQuery({
    queryKey: ['ocr', 'cola', 'pendiente'],
    queryFn: () => ocrApi.getCola('PENDIENTE', 1, 1),
    refetchInterval: REFETCH_MS,
  });

  const { data: recentData, isLoading: loadingRecent } = useQuery({
    queryKey: ['grupos-receta', 'list', { page: 1, pageSize: 10 }],
    queryFn: () => gruposRecetaApi.listar({ page: 1, pageSize: 10 }),
    refetchInterval: REFETCH_MS,
  });

  const columns: Column<GrupoRecetaDto>[] = [
    ...COLUMNS,
    {
      key: 'acciones',
      header: 'Acciones',
      render: (r) => (
        <button
          onClick={() => navigate(`/grupos-receta/${r.id}`)}
          className="text-blue-600 hover:underline text-xs font-medium"
        >
          Ver detalle
        </button>
      ),
    },
  ];

  return (
    <div className="p-6">
      <PageHeader
        title="Dashboard"
        subtitle="Resumen general del sistema"
        actions={
          puedeSubir ? (
            <button
              onClick={() => navigate('/grupos-receta/nuevo')}
              className="bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium px-4 py-2 rounded-lg transition-colors"
            >
              + Nuevo Grupo
            </button>
          ) : undefined
        }
      />

      {/* Métricas */}
      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4 mb-8">
        <MetricCard label="Grupos hoy" value={gruposHoy?.total} color="text-blue-600" />
        <MetricCard label="En revisión" value={enRevision?.total} color="text-yellow-600" />
        <MetricCard label="Facturados hoy" value={facturadosHoy?.total} color="text-purple-600" />
        <MetricCard label="Cola OCR pendiente" value={colaOcr?.total} color="text-orange-600" />
      </div>

      {/* Tabla reciente */}
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
    </div>
  );
}
