import api from '@/utils/axios.instance'
import type { ApiResponse } from '@/types/auth.types'
import type {
  GrupoRecetaDto,
  GrupoRecetaDetalleDto,
  FiltrosGrupoDto,
  CrearGrupoRecetaDto,
  ActualizarGrupoDto,
} from '@/types/grupo-receta.types'

export interface PagedResultDto<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
  totalPages: number
}

export const gruposRecetaApi = {
  listar: async (filtros: FiltrosGrupoDto): Promise<PagedResultDto<GrupoRecetaDto>> => {
    const { data } = await api.get<ApiResponse<PagedResultDto<GrupoRecetaDto>>>('/grupos-receta', {
      params: filtros,
    })
    return data.data!
  },

  obtener: async (id: string): Promise<GrupoRecetaDetalleDto> => {
    const { data } = await api.get<ApiResponse<GrupoRecetaDetalleDto>>(`/grupos-receta/${id}`)
    return data.data!
  },

  crear: async (body: CrearGrupoRecetaDto): Promise<GrupoRecetaDto> => {
    const { data } = await api.post<ApiResponse<GrupoRecetaDto>>('/grupos-receta', body)
    return data.data!
  },

  actualizar: async (id: string, body: ActualizarGrupoDto): Promise<GrupoRecetaDto> => {
    const { data } = await api.patch<ApiResponse<GrupoRecetaDto>>(`/grupos-receta/${id}`, body)
    return data.data!
  },
}
