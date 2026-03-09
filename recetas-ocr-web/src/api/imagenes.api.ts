import api from '@/utils/axios.instance'
import type { ApiResponse } from '@/types/auth.types'
import type { ImagenDto, ImagenConOcrDto, OrigenImagen } from '@/types/imagen.types'

export const imagenesApi = {
  subir: async (
    idGrupo: string,
    archivo: File,
    origenImagen: OrigenImagen,
    onProgress?: (pct: number) => void
  ): Promise<ImagenConOcrDto> => {
    const form = new FormData()
    form.append('idGrupo', idGrupo)
    form.append('archivo', archivo)
    form.append('origenImagen', origenImagen)
    const { data } = await api.post<ApiResponse<ImagenConOcrDto>>('/imagenes', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
      timeout: 180000, // 3 minutos — OCR puede tardar
      onUploadProgress: onProgress
        ? (e) => {
            if (e.total) {
              // Upload es ~20% del tiempo total, OCR es el resto
              const pct = Math.round((e.loaded / e.total) * 20)
              onProgress(pct)
            }
          }
        : undefined,
    })
    return data.data!
  },

  listarPorGrupo: async (idGrupo: string): Promise<ImagenDto[]> => {
    const { data } = await api.get<ApiResponse<ImagenDto[]>>(`/imagenes/grupo/${idGrupo}`)
    return data.data!
  },

  obtener: async (id: string): Promise<ImagenConOcrDto> => {
    const { data } = await api.get<ApiResponse<ImagenConOcrDto>>(`/imagenes/${id}`)
    return data.data!
  },
}
