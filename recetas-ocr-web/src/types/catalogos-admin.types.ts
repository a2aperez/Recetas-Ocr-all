export interface AseguradoraDetalleDto {
  id: number
  nombre: string
  clave: string
  razonSocial: string
  rfc: string
  idAseguradoraPadre: number | null
  nombrePadre: string | null
  activo: boolean
}

export interface CrearAseguradoraDto {
  nombre: string
  clave: string
  razonSocial: string
  rfc: string
  idAseguradoraPadre?: number
}

export interface MedicamentoCatalogoDetalleDto {
  id: number
  nombreComercial: string
  sustanciaActiva: string | null
  presentacion: string | null
  codigoEAN: string | null
  claveSAT: string | null
  activo: boolean
}

export interface CrearMedicamentoDto {
  nombreComercial: string
  sustanciaActiva?: string
  presentacion?: string
  codigoEAN?: string
  claveSAT?: string
}

export interface ParametroDto {
  id: string
  clave: string
  valor: string
  descripcion: string
  tipoDato: string
  activo: boolean
}

export interface ConfiguracionOcrDto {
  id: number
  nombre: string
  urlBase: string
  apiKeyParcial: string // solo primeros 8 chars + '****'
  proveedor: string
  esPrincipal: boolean
  activo: boolean
  configJson: string | null
}

export interface ActualizarConfiguracionOcrDto {
  nombre: string
  urlBase: string
  apiKey?: string // solo si se quiere cambiar
  esPrincipal: boolean
  activo: boolean
  configJson?: string
}

export interface ViaAdministracionDetalleDto {
  id: number
  clave: string
  nombre: string
  activo: boolean
}

export interface ModuloDto {
  modulo: string
}
