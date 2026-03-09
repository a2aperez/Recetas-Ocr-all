import type { PermisoDto } from './auth.types'

export interface RolDto {
  id: number
  clave: string
  nombre: string
  descripcion: string
}

export interface UsuarioListaDto {
  id: string
  username: string
  email: string
  nombreCompleto: string
  nombreRol: string
  activo: boolean
  ultimoAcceso: string | null
  fechaAlta: string
}

export interface UsuarioDetalleDto extends UsuarioListaDto {
  permisos: PermisoDto[]
  requiereCambioPassword: boolean
  idRol: number
}

export interface CrearUsuarioDto {
  username: string
  email: string
  nombreCompleto: string
  apellidoPaterno?: string
  apellidoMaterno?: string
  idRol: number
}

export interface CrearUsuarioResponseDto {
  usuario: UsuarioDetalleDto
  passwordTemporal: string
}

export interface CambiarPasswordDto {
  passwordActual: string
  passwordNuevo: string
  passwordConfirmacion: string
}

export interface PermisoUsuarioDto {
  modulo: string
  puedeLeer: boolean
  puedeEscribir: boolean
  puedeEliminar: boolean
  denegado: boolean
}

export interface FiltrosUsuarioDto {
  busqueda?: string
  activo?: boolean
  page: number
  pageSize: number
}
