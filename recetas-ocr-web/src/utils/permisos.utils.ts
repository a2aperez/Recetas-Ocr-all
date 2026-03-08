import { useAuthStore } from '../store/auth.store'

export function tienePermiso(
  modulo: string,
  accion: 'puedeLeer' | 'puedeEscribir' | 'puedeEliminar' = 'puedeLeer'
): boolean {
  const permisos = useAuthStore.getState().usuario?.permisos ?? []
  const permiso = permisos.find(p => p.modulo === modulo)
  return permiso?.[accion] ?? false
}
