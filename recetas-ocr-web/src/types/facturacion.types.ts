export interface ConceptoFacturaDto {
  descripcion: string
  cantidad: number
  claveUnidad: string
  claveProdServ: string
  valorUnitario: number
  importe: number
  descuento: number | null
  impuestos: number
}

export interface PreFacturaDto {
  id: string
  idGrupo: string
  rfcEmisor: string
  rfcReceptor: string
  nombreReceptor: string
  usoCfdi: string
  metodoPago: string
  formaPago: string
  conceptos: ConceptoFacturaDto[]
  subtotal: number
  descuento: number
  impuestos: number
  total: number
  estado: string
  fechaCreacion: string
}

export interface CfdiDto {
  id: string
  idPreFactura: string
  idGrupo: string
  uuid: string | null
  serie: string | null
  folio: string | null
  rfcEmisor: string
  rfcReceptor: string
  total: number
  estado: string
  xmlUrl: string | null
  pdfUrl: string | null
  fechaTimbrado: string | null
  fechaCreacion: string
}

export interface FacturaResumenDto {
  id: string
  idGrupo: string
  folioGrupo: string | null
  nombrePaciente: string | null
  rfcReceptor: string
  total: number
  estado: string
  uuid: string | null
  fechaTimbrado: string | null
  fechaCreacion: string
}

export interface FiltrosFacturaDto {
  idAseguradora?: number
  estado?: string
  fechaDesde?: string
  fechaHasta?: string
  rfcReceptor?: string
  page: number
  pageSize: number
}

export interface DatosFiscalesDto {
  rfcReceptor: string
  nombreReceptor: string
  usoCfdi: string
  metodoPago: string
  formaPago: string
  regimenFiscalReceptor?: string
  domicilioFiscalReceptor?: string
}
