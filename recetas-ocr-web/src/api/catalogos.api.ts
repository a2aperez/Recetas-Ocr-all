import api from '@/utils/axios.instance'
import type { ApiResponse } from '@/types/auth.types'
import type {
  AseguradoraDto,
  ViaAdministracionDto,
  EstadoDto,
  MedicamentoCatalogoDto,
} from '@/types/catalogos.types'
import type { PagedResultDto } from './grupos-receta.api'

export const catalogosApi = {
  getAseguradoras: async (): Promise<AseguradoraDto[]> => {
    const { data } = await api.get<ApiResponse<AseguradoraDto[]>>('/catalogos/aseguradoras')
    return data.data!
  },

  getMedicamentos: async (
    busqueda?: string,
    page?: number,
    pageSize?: number
  ): Promise<PagedResultDto<MedicamentoCatalogoDto>> => {
    const { data } = await api.get<ApiResponse<PagedResultDto<MedicamentoCatalogoDto>>>(
      '/catalogos/medicamentos',
      { params: { busqueda, page, pageSize } }
    )
    return data.data!
  },

  getViasAdministracion: async (): Promise<ViaAdministracionDto[]> => {
    const { data } = await api.get<ApiResponse<ViaAdministracionDto[]>>(
      '/catalogos/vias-administracion'
    )
    return data.data!
  },

  getEstadosImagen: async (): Promise<EstadoDto[]> => {
    const { data } = await api.get<ApiResponse<EstadoDto[]>>('/catalogos/estados-imagen')
    return data.data!
  },

  getEstadosGrupo: async (): Promise<EstadoDto[]> => {
    const { data } = await api.get<ApiResponse<EstadoDto[]>>('/catalogos/estados-grupo')
    return data.data!
  },
}
