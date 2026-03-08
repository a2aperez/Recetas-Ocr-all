import api from '@/utils/axios.instance'
import type { ApiResponse } from '@/types/auth.types'
import type { ImagenDto, OrigenImagen } from '@/types/imagen.types'

export const imagenesApi = {
  subir: async (
    idGrupo: string,
    archivo: File,
    origenImagen: OrigenImagen,
    onProgress?: (pct: number) => void
  ): Promise<string> => {
    const form = new FormData()
    form.append('idGrupo', idGrupo)
    form.append('archivo', archivo)
    form.append('origenImagen', origenImagen)
    const { data } = await api.post<ApiResponse<string>>('/imagenes', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
      onUploadProgress: onProgress
        ? (e) => {
            if (e.total) onProgress(Math.round((e.loaded * 100) / e.total))
          }
        : undefined,
    })
    return data.data!
  },

  listarPorGrupo: async (idGrupo: string): Promise<ImagenDto[]> => {
    const { data } = await api.get<ApiResponse<ImagenDto[]>>(`/imagenes/grupo/${idGrupo}`)
    return data.data!
  },

  obtener: async (id: string): Promise<ImagenDto> => {
    const { data } = await api.get<ApiResponse<ImagenDto>>(`/imagenes/${id}`)
    return data.data!
  },
}
