export type OrigenImagen = 'CAMARA' | 'GALERIA' | 'API' | 'ESCANER'

export type EstadoImagen =
  | 'RECIBIDA'
  | 'PROCESANDO'
  | 'LEGIBLE'
  | 'ILEGIBLE'
  | 'CAPTURA_MANUAL_COMPLETA'
  | 'OCR_APROBADO'
  | 'OCR_BAJA_CONFIANZA'
  | 'EXTRACCION_COMPLETA'
  | 'EXTRACCION_INCOMPLETA'
  | 'REVISION_PENDIENTE'
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
  id?: string
  numeroPrescripcion?: number
  nombreComercial: string | null
  sustanciaActiva: string | null
  presentacion: string | null
  dosis: string | null
  cantidad: number | null
  cantidadTexto: string | null
  cantidadNumero: number | null
  unidadCantidad: string | null
  viaAdministracion: string | null
  frecuenciaTexto: string | null
  frecuenciaExpandida: string | null
  duracionTexto: string | null
  duracionDias: number | null
  indicaciones: string | null
  indicacionesCompletas: string | null
  codigoCIE10: string | null
  codigoEAN: string | null
  fuenteDato: string | null
}

// Datos simplificados de medicamento tal como los extrae el OCR
export interface MedicamentoOcrDto {
  nombreComercial: string | null
  sustanciaActiva: string | null
  presentacion: string | null
  dosis: string | null
  cantidadTexto: string | null
  numeroPrescripcion: number | null
}

export interface DatosOcrExtraidosDto {
  // Paciente
  nombrePaciente: string | null
  apellidoPaterno: string | null
  apellidoMaterno: string | null
  nomina: string | null
  credencial: string | null
  nur: string | null
  numeroAutorizacion: string | null
  elegibilidad: string | null
  // Médico
  nombreMedico: string | null
  cedulaMedico: string | null
  especialidad: string | null
  institucion: string | null
  // Consulta
  fechaConsulta: string | null
  diagnosticoTexto: string | null
  codigoCIE10: string | null
  // Medicamentos extraídos
  medicamentos: MedicamentoRecetaDto[]
  // Metadata OCR
  confianzaPromedio: number
  calidadLectura: 'alta' | 'media' | 'baja' | string
  camposIlegibles: string[]
  notas: string | null
  esConfianzaBaja: boolean
}

export interface ImagenConOcrDto extends ImagenDto {
  datosOcr: DatosOcrExtraidosDto | null
}
