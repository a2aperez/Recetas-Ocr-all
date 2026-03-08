import type { EstadoImagen } from './imagen.types'

export interface EstadoOcrDto {
  idImagen: string
  estadoImagen: EstadoImagen
  estadoCola: string | null
  intentos: number | null
  maxIntentos: number | null
  bloqueado: boolean | null
  fechaEncolado: string | null
  fechaInicioProceso: string | null
  fechaFinProceso: string | null
  confianzaPromedio: number | null
  esLegible: boolean | null
  motivoBajaCalidad: string | null
  proveedorOcr: string | null
  modeloUsado: string | null
  duracionMs: number | null
  exitoso: boolean | null
}

export interface ResultadoOcrDetalleDto extends EstadoOcrDto {
  responseJsonCompleto: string | null
  textoCompleto: string | null
  camposFaltantes: string | null
  aseguradoraDetectada: string | null
  formatoDetectado: string | null
  tokensEntrada: number | null
  tokensSalida: number | null
  costoEstimadoUsd: number | null
}
