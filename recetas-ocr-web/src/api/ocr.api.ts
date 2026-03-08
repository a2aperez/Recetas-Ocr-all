import api from '@/utils/axios.instance'
import type { ApiResponse } from '@/types/auth.types'
import type { EstadoOcrDto, ResultadoOcrDetalleDto } from '@/types/ocr.types'
import type { PagedResultDto } from './grupos-receta.api'

export const ocrApi = {
  getEstado: async (idImagen: string): Promise<EstadoOcrDto> => {
    const { data } = await api.get<ApiResponse<EstadoOcrDto>>(`/ocr/imagen/${idImagen}/estado`)
    return data.data!
  },

  getResultado: async (idImagen: string): Promise<ResultadoOcrDetalleDto> => {
    const { data } = await api.get<ApiResponse<ResultadoOcrDetalleDto>>(`/ocr/imagen/${idImagen}/resultado`)
    return data.data!
  },

  reprocesar: async (idImagen: string, motivo: string): Promise<EstadoOcrDto> => {
    const { data } = await api.post<ApiResponse<EstadoOcrDto>>(
      `/ocr/imagen/${idImagen}/reprocesar`,
      { motivo }
    )
    return data.data!
  },

  getCola: async (
    estadoCola?: string,
    page?: number,
    pageSize?: number
  ): Promise<PagedResultDto<EstadoOcrDto>> => {
    const { data } = await api.get<ApiResponse<PagedResultDto<EstadoOcrDto>>>('/ocr/cola', {
      params: { estadoCola, page, pageSize },
    })
    return data.data!
  },
}
