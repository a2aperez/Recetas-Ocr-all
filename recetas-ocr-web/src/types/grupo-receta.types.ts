import type { ImagenDto, MedicamentoRecetaDto } from './imagen.types'

export type EstadoGrupo =
  | 'RECIBIDO'
  | 'REQUIERE_CAPTURA_MANUAL'
  | 'PROCESANDO'
  | 'GRUPO_INCOMPLETO'
  | 'REVISION_PENDIENTE'
  | 'REVISADO_COMPLETO'
  | 'DATOS_FISCALES_INCOMPLETOS'
  | 'PENDIENTE_AUTORIZACION'
  | 'PENDIENTE_FACTURACION'
  | 'PREFACTURA_GENERADA'
  | 'FACTURADA'
  | 'ERROR_TIMBRADO_MANUAL'
  | 'RECHAZADO'

export interface GrupoRecetaDto {
  id: string
  folioBase: string | null
  idAseguradora: number
  nombreAseguradora: string
  nombrePaciente: string | null
  apellidoPaterno: string | null
  apellidoMaterno: string | null
  nombreMedico: string | null
  fechaConsulta: string | null
  estadoGrupo: EstadoGrupo
  totalImagenes: number
  totalMedicamentos: number
  fechaCreacion: string
  modificadoPor: string | null
}

export interface GrupoRecetaDetalleDto extends GrupoRecetaDto {
  imagenes: ImagenDto[]
  medicamentos: MedicamentoRecetaDto[]
}

export interface FiltrosGrupoDto {
  idAseguradora?: number
  estadoGrupo?: EstadoGrupo
  fechaDesde?: string
  fechaHasta?: string
  busqueda?: string
  page: number
  pageSize: number
}

export interface CrearGrupoRecetaDto {
  idAseguradora: number
  folioBase?: string
  fechaConsulta: string
  nombrePaciente?: string
  nombreMedico?: string
}

export interface ActualizarGrupoDto {
  nombrePaciente?: string
  apellidoPaterno?: string
  apellidoMaterno?: string
  nombreMedico?: string
  fechaConsulta?: string
}
