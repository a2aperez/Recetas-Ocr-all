export interface AseguradoraDto {
  id: number
  nombre: string
  rfc: string | null
  activa: boolean
}

export interface ViaAdministracionDto {
  id: number
  clave: string
  descripcion: string
}

export interface EstadoDto {
  clave: string
  descripcion: string
  esEstadoFinal: boolean
}

export interface MedicamentoCatalogoDto {
  id: number
  nombreComercial: string
  sustanciaActiva: string | null
  presentacion: string | null
  laboratorio: string | null
  codigoBarras: string | null
}
