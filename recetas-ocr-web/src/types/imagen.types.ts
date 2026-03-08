export type OrigenImagen = 'CAMARA' | 'GALERIA' | 'API' | 'ESCANER'

export type EstadoImagen =
  | 'RECIBIDA'
  | 'LEGIBLE'
  | 'ILEGIBLE'
  | 'CAPTURA_MANUAL_COMPLETA'
  | 'OCR_APROBADO'
  | 'OCR_BAJA_CONFIANZA'
  | 'EXTRACCION_COMPLETA'
  | 'EXTRACCION_INCOMPLETA'
  | 'REVISADA'
  | 'RECHAZADA'

export const ESTADOS_IMAGEN_FINALES: EstadoImagen[] = [
  'REVISADA', 'RECHAZADA',
  'EXTRACCION_COMPLETA', 'EXTRACCION_INCOMPLETA',
  'OCR_BAJA_CONFIANZA', 'ILEGIBLE',
]

export interface ImagenDto {
  id: string
  idGrupo: string
  numeroHoja: number
  urlBlobRaw: string
  urlBlobOcr: string | null
  urlBlobIlegible: string | null
  origenImagen: OrigenImagen
  nombreArchivo: string
  tamanioBytes: number
  fechaSubida: string
  scoreLegibilidad: number | null
  esLegible: boolean | null
  motivoBajaCalidad: string | null
  esCapturaManual: boolean
  estadoImagen: EstadoImagen
  modificadoPor: string | null
  fechaModificacion: string
}

export interface MedicamentoRecetaDto {
  id: string
  numeroPrescripcion: number
  nombreComercial: string | null
  sustanciaActiva: string | null
  presentacion: string | null
  dosis: string | null
  cantidadTexto: string | null
  cantidadNumero: number | null
  frecuenciaTexto: string | null
  frecuenciaExpandida: string | null
  duracionTexto: string | null
  duracionDias: number | null
  indicacionesCompletas: string | null
  viaAdministracion: string | null
}
