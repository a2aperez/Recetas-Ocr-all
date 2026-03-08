import api from '@/utils/axios.instance'
import type { ApiResponse } from '@/types/auth.types'
import type {
  PreFacturaDto,
  CfdiDto,
  FacturaResumenDto,
  FiltrosFacturaDto,
  DatosFiscalesDto,
} from '@/types/facturacion.types'
import type { PagedResultDto } from './grupos-receta.api'

export const facturacionApi = {
  getPreFactura: async (idGrupo: string): Promise<PreFacturaDto> => {
    const { data } = await api.get<ApiResponse<PreFacturaDto>>(
      `/facturacion/grupo/${idGrupo}/prefactura`
    )
    return data.data!
  },

  generarPreFactura: async (idGrupo: string): Promise<PreFacturaDto> => {
    const { data } = await api.post<ApiResponse<PreFacturaDto>>(
      `/facturacion/grupo/${idGrupo}/generar-prefactura`
    )
    return data.data!
  },

  actualizarDatosFiscales: async (
    idGrupo: string,
    datos: DatosFiscalesDto
  ): Promise<void> => {
    await api.put(`/facturacion/grupo/${idGrupo}/datos-fiscales`, datos)
  },

  timbrar: async (idPreFactura: string): Promise<CfdiDto> => {
    const { data } = await api.post<ApiResponse<CfdiDto>>(
      `/facturacion/prefactura/${idPreFactura}/timbrar`
    )
    return data.data!
  },

  getCfdisGrupo: async (idGrupo: string): Promise<CfdiDto[]> => {
    const { data } = await api.get<ApiResponse<CfdiDto[]>>(
      `/facturacion/grupo/${idGrupo}/cfdis`
    )
    return data.data!
  },

  listar: async (filtros: FiltrosFacturaDto): Promise<PagedResultDto<FacturaResumenDto>> => {
    const { data } = await api.get<ApiResponse<PagedResultDto<FacturaResumenDto>>>(
      '/facturacion',
      { params: filtros }
    )
    return data.data!
  },
}
