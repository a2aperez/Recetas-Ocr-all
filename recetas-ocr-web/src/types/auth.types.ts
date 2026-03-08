export interface PermisoDto {
  modulo: string
  puedeLeer: boolean
  puedeEscribir: boolean
  puedeEliminar: boolean
}

export interface UsuarioSesionDto {
  id: string
  username: string
  nombreCompleto: string
  email: string
  rol: string
  permisos: PermisoDto[]
}

export interface LoginRequestDto {
  username: string
  password: string
}

export interface LoginResponseDto {
  token: string
  refreshToken: string
  expiraEn: string
  usuario: UsuarioSesionDto
}

export interface ApiResponse<T> {
  success: boolean
  data: T | null
  message: string | null
  errors: string[]
}
