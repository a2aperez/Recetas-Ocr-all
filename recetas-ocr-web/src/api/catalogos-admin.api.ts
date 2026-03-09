import axiosInstance from '../utils/axios.instance'
import type {
  AseguradoraDetalleDto,
  CrearAseguradoraDto,
  MedicamentoCatalogoDetalleDto,
  CrearMedicamentoDto,
  ParametroDto,
  ConfiguracionOcrDto,
  ActualizarConfiguracionOcrDto,
  ViaAdministracionDetalleDto,
  ModuloDto,
} from '../types/catalogos-admin.types'

export const catalogosAdminApi = {
  // Aseguradoras
  getAseguradoras: async (): Promise<AseguradoraDetalleDto[]> => {
    const { data } = await axiosInstance.get('/catalogos/aseguradoras')
    return data.data
  },
  crearAseguradora: async (dto: CrearAseguradoraDto) => {
    const { data } = await axiosInstance.post('/catalogos/aseguradoras', dto)
    return data.data
  },
  actualizarAseguradora: async (id: number, dto: Partial<AseguradoraDetalleDto>) => {
    const { data } = await axiosInstance.put(`/catalogos/aseguradoras/${id}`, dto)
    return data.data
  },

  // Medicamentos
  getMedicamentos: async (busqueda?: string, page = 1, pageSize = 20) => {
    const { data } = await axiosInstance.get('/catalogos/medicamentos', {
      params: { busqueda, page, pageSize },
    })
    return data.data
  },
  crearMedicamento: async (dto: CrearMedicamentoDto) => {
    const { data } = await axiosInstance.post('/catalogos/medicamentos', dto)
    return data.data
  },
  actualizarMedicamento: async (id: number, dto: Partial<MedicamentoCatalogoDetalleDto>) => {
    const { data } = await axiosInstance.put(`/catalogos/medicamentos/${id}`, dto)
    return data.data
  },

  // Parámetros
  getParametros: async (): Promise<ParametroDto[]> => {
    const { data } = await axiosInstance.get('/catalogos/parametros')
    return data.data
  },
  actualizarParametro: async (clave: string, valor: string) => {
    const { data } = await axiosInstance.put(`/catalogos/parametros/${clave}`, { valor })
    return data.data
  },

  // Configuración OCR
  getConfiguracionesOcr: async (): Promise<ConfiguracionOcrDto[]> => {
    const { data } = await axiosInstance.get('/catalogos/configuraciones-ocr')
    return data.data
  },
  actualizarConfiguracionOcr: async (id: number, dto: ActualizarConfiguracionOcrDto) => {
    const { data } = await axiosInstance.put(`/catalogos/configuraciones-ocr/${id}`, dto)
    return data.data
  },

  // Vías de administración
  getVias: async (): Promise<ViaAdministracionDetalleDto[]> => {
    const { data } = await axiosInstance.get('/catalogos/vias-administracion')
    return data.data
  },
  crearVia: async (clave: string, nombre: string) => {
    const { data } = await axiosInstance.post('/catalogos/vias-administracion', { clave, nombre })
    return data.data
  },
  actualizarVia: async (id: number, nombre: string, activo: boolean) => {
    const { data } = await axiosInstance.put(`/catalogos/vias-administracion/${id}`, { nombre, activo })
    return data.data
  },

  // Módulos
  getModulos: async (): Promise<ModuloDto[]> => {
    const { data } = await axiosInstance.get('/catalogos/modulos')
    return data.data
  },
}
