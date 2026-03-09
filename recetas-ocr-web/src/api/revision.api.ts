import api from '@/utils/axios.instance'
import type { ApiResponse } from '@/types/auth.types'
import type { EstadoOcrDto } from '@/types/ocr.types'
import type { EstadoImagen, MedicamentoRecetaDto } from '@/types/imagen.types'
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

  aprobar: async (dto: {
    idImagen: string
    datosPaciente?: {
      nombrePaciente?: string
      apellidoPaterno?: string
      apellidoMaterno?: string
      nomina?: string
      credencial?: string
      nur?: string
      elegibilidad?: string
    }
    datosMedico?: {
      nombreMedico?: string
      cedulaMedico?: string
      especialidad?: string
    }
    datosConsulta?: {
      fechaConsulta?: string
      diagnosticoTexto?: string
      codigoCIE10?: string
    }
    medicamentos?: MedicamentoRecetaDto[]
    observaciones?: string
  }): Promise<void> => {
    await api.post('/revision/aprobar', dto)
  },

  rechazar: async (dto: {
    idImagen: string
    motivo: string
  }): Promise<void> => {
    await api.post('/revision/rechazar', dto)
  },

  corregirCampo: async (
    idImagen: string,
    tabla: string,
    campo: string,
    valorAnterior: string | null,
    valorNuevo: string,
    motivo: string
  ): Promise<void> => {
    await api.post<ApiResponse<EstadoOcrDto>>('/revision/corregir-campo', {
      idImagen,
      tabla,
      campo,
      valorAnterior,
      valorNuevo,
      motivo,
    })
  },
}
