export interface AseguradoraDto {
  id: number
  clave: string
  nombre: string
  razonSocial: string | null
  rfc: string | null
  activo: boolean
}

export interface ViaAdministracionDto {
  id: number
  clave: string
  nombre: string
}

export interface EstadoDto {
  id: number
  clave: string
  nombre: string
  descripcion: string | null
}

export interface MedicamentoCatalogoDto {
  id: number
  nombreComercial: string
  sustanciaActiva: string | null
  presentacion: string | null
  concentracion: string | null
}
