import { StatusBadge } from '@/components/common/StatusBadge'
import type { EstadoImagen } from '@/types/imagen.types'

interface Props {
  estado: EstadoImagen
  size?: 'sm' | 'md'
  esLegible?: boolean | null
  confianza?: number | null
}

function confianzaColor(pct: number) {
  if (pct >= 80) return { bar: 'bg-green-500', text: 'text-green-700' }
  if (pct >= 60) return { bar: 'bg-yellow-400', text: 'text-yellow-700' }
  return { bar: 'bg-red-500', text: 'text-red-600' }
}

export function ImagenEstadoBadge({ estado, size = 'sm', confianza }: Props) {
  const pct = confianza != null ? Math.max(0, Math.min(100, Math.round(confianza))) : null
  const colors = pct != null ? confianzaColor(pct) : null

  return (
    <div className="inline-flex flex-col gap-1">
      <StatusBadge estado={estado} size={size} />
      {pct != null && colors && (
        <div className="flex items-center gap-1.5">
          <div className="w-16 h-1.5 bg-gray-200 rounded-full overflow-hidden">
            <div
              className={`h-full rounded-full transition-all ${colors.bar}`}
              style={{ width: `${pct}%` }}
            />
          </div>
          <span className={`text-xs font-medium ${colors.text}`}>{pct}%</span>
        </div>
      )}
    </div>
  )
}
