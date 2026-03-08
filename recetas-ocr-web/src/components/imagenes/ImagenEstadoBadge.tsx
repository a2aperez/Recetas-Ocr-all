import { StatusBadge } from '@/components/common/StatusBadge'
import type { EstadoImagen } from '@/types/imagen.types'

interface Props {
  estado: EstadoImagen
  size?: 'sm' | 'md'
}

export function ImagenEstadoBadge({ estado, size = 'sm' }: Props) {
  return <StatusBadge estado={estado} size={size} />
}
