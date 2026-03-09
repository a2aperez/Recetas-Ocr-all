import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'react-hot-toast'
import { usuariosApi } from '../api/usuarios.api'
import type {
  FiltrosUsuarioDto,
  CrearUsuarioDto,
  CambiarPasswordDto,
  PermisoUsuarioDto,
} from '../types/usuario.types'

export function useUsuarios(filtros: FiltrosUsuarioDto) {
  return useQuery({
    queryKey: ['usuarios', 'list', filtros],
    queryFn: () => usuariosApi.listar(filtros),
  })
}

export function useUsuarioDetalle(id: string) {
  return useQuery({
    queryKey: ['usuarios', 'detail', id],
    queryFn: () => usuariosApi.obtener(id),
    enabled: !!id,
  })
}

export function useCrearUsuario() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (dto: CrearUsuarioDto) => usuariosApi.crear(dto),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['usuarios'] })
      toast.success('Usuario creado correctamente')
    },
    onError: (err: Error) => toast.error(err.message),
  })
}

export function useCambiarEstadoUsuario() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, activo }: { id: string; activo: boolean }) =>
      usuariosApi.cambiarEstado(id, activo),
    onSuccess: (_, vars) => {
      qc.invalidateQueries({ queryKey: ['usuarios'] })
      toast.success(vars.activo ? 'Usuario activado' : 'Usuario desactivado')
    },
    onError: (err: Error) => toast.error(err.message),
  })
}

export function useCambiarPassword() {
  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: CambiarPasswordDto }) =>
      usuariosApi.cambiarPassword(id, dto),
    onSuccess: () => toast.success('Contraseña actualizada correctamente'),
    onError: (err: Error) => toast.error(err.message),
  })
}

export function useAsignarPermisos() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, permisos }: { id: string; permisos: PermisoUsuarioDto[] }) =>
      usuariosApi.asignarPermisos(id, permisos),
    onSuccess: (_, vars) => {
      qc.invalidateQueries({ queryKey: ['usuarios', 'detail', vars.id] })
      toast.success('Permisos actualizados')
    },
    onError: (err: Error) => toast.error(err.message),
  })
}
