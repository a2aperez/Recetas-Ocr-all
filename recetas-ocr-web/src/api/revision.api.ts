import api from '@/utils/axios.instance'
import type { ApiResponse } from '@/types/auth.types'
import type { EstadoOcrDto } from '@/types/ocr.types'
import type { EstadoImagen } from '@/types/imagen.types'
import type { PagedResultDto } from './grupos-receta.api'

export interface CorregirCampoDto {
  idImagen: string
  tabla: string
  campo: string
  valorAnterior: string | null
  valorNuevo: string
  tipoCorreccion: string
}

export interface ItemColaRevisionDto {
  idImagen: string
  idGrupo: string
  folioGrupo: string | null
  nombreAseguradora: string | null
  numeroHoja: number
  estadoImagen: EstadoImagen
  confianzaPromedio: number | null
  fechaEncolado: string | null
  estadoCola: string | null
  esLegible: boolean | null
}

export const revisionApi = {
  getCola: async (page: number, pageSize: number): Promise<PagedResultDto<ItemColaRevisionDto>> => {
    const { data } = await api.get<ApiResponse<PagedResultDto<ItemColaRevisionDto>>>('/revision/cola', {
      params: { page, pageSize },
    })
    return data.data!
  },

  aprobar: async (idImagen: string, observaciones?: string): Promise<void> => {
    await api.post('/revision/aprobar', { idImagen, observaciones })
  },

  rechazar: async (idImagen: string, motivoRechazo: string): Promise<void> => {
    await api.post('/revision/rechazar', { idImagen, motivoRechazo })
  },

  corregirCampo: async (
    idImagen: string,
    tabla: string,
    campo: string,
    valorAnterior: string | null,
    valorNuevo: string,
    tipoCorreccion: string
  ): Promise<void> => {
    await api.post<ApiResponse<EstadoOcrDto>>('/revision/corregir-campo', {
      idImagen,
      tabla,
      campo,
      valorAnterior,
      valorNuevo,
      tipoCorreccion,
    })
  },
}
