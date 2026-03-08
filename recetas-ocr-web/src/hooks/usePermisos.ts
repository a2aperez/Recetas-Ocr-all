import { useAuthStore } from '../store/auth.store'

export function usePermisos(modulo: string) {
  const permisos = useAuthStore(s => s.usuario?.permisos ?? [])
  const permiso = permisos.find(p => p.modulo === modulo)
  return {
    puedeLeer: permiso?.puedeLeer ?? false,
    puedeEscribir: permiso?.puedeEscribir ?? false,
    puedeEliminar: permiso?.puedeEliminar ?? false,
  }
}
