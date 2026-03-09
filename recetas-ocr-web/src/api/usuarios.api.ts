import api from '@/utils/axios.instance'
import type { ApiResponse } from '@/types/auth.types'
import type { PagedResultDto } from './grupos-receta.api'
import type {
  UsuarioListaDto,
  UsuarioDetalleDto,
  CrearUsuarioDto,
  CrearUsuarioResponseDto,
  CambiarPasswordDto,
  PermisoUsuarioDto,
  FiltrosUsuarioDto,
} from '@/types/usuario.types'

export const usuariosApi = {
  listar: async (filtros: FiltrosUsuarioDto): Promise<PagedResultDto<UsuarioListaDto>> => {
    const { data } = await api.get<ApiResponse<PagedResultDto<UsuarioListaDto>>>(
      '/usuarios',
      { params: filtros }
    )
    return data.data!
  },

  obtener: async (id: string): Promise<UsuarioDetalleDto> => {
    const { data } = await api.get<ApiResponse<UsuarioDetalleDto>>(`/usuarios/${id}`)
    return data.data!
  },

  perfil: async (): Promise<UsuarioDetalleDto> => {
    const { data } = await api.get<ApiResponse<UsuarioDetalleDto>>('/usuarios/perfil')
    return data.data!
  },

  crear: async (dto: CrearUsuarioDto): Promise<CrearUsuarioResponseDto> => {
    const { data } = await api.post<ApiResponse<CrearUsuarioResponseDto>>('/usuarios', dto)
    return data.data!
  },

  cambiarEstado: async (id: string, activo: boolean): Promise<void> => {
    await api.put(`/usuarios/${id}/estado`, { activo })
  },

  cambiarPassword: async (id: string, dto: CambiarPasswordDto): Promise<void> => {
    await api.put(`/usuarios/${id}/password`, dto)
  },

  asignarPermisos: async (id: string, permisos: PermisoUsuarioDto[]): Promise<void> => {
    await api.put(`/usuarios/${id}/permisos`, { permisos })
  },
}
