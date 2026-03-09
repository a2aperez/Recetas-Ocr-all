import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import toast from 'react-hot-toast'
import { imagenesApi } from '@/api/imagenes.api'
import { ocrApi } from '@/api/ocr.api'
import type { OrigenImagen } from '@/types/imagen.types'

const ESTADOS_FINALES_OCR = [
  'OCR_APROBADO',
  'OCR_BAJA_CONFIANZA',
  'ILEGIBLE',
  'REVISADA',
  'RECHAZADA',
  'EXTRACCION_COMPLETA',
  'EXTRACCION_INCOMPLETA',
  'CAPTURA_MANUAL_COMPLETA',
]

export function useImagenesPorGrupo(idGrupo: string) {
  return useQuery({
    queryKey: ['imagenes', 'by-grupo', idGrupo],
    queryFn: () => imagenesApi.listarPorGrupo(idGrupo),
    enabled: !!idGrupo,
  })
}

export function useSubirImagen() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({
      idGrupo,
      archivo,
      origen,
      onProgress,
    }: {
      idGrupo: string
      archivo: File
      origen: OrigenImagen
      onProgress?: (pct: number) => void
    }) => imagenesApi.subir(idGrupo, archivo, origen, onProgress),
    onSuccess: (_, vars) => {
      qc.invalidateQueries({ queryKey: ['imagenes', 'by-grupo', vars.idGrupo] })
      qc.invalidateQueries({ queryKey: ['grupos-receta', 'detail', vars.idGrupo] })
      // Toast manejado por SubirImagenPage según resultado OCR
    },
    onError: (err: Error) => toast.error(err.message),
  })
}

export function useOcrEstadoImagen(idImagen: string) {
  return useQuery({
    queryKey: ['ocr', 'estado', idImagen],
    queryFn: () => ocrApi.getEstado(idImagen),
    enabled: !!idImagen,
    refetchInterval: (query) => {
      const estado = query.state.data?.estadoImagen
      return estado && ESTADOS_FINALES_OCR.includes(estado) ? false : 3000
    },
  })
}
