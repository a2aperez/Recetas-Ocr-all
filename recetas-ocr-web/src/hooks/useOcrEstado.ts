import { useQuery } from '@tanstack/react-query';
import { ESTADOS_IMAGEN_FINALES } from '@/types/imagen.types';
import { ocrApi } from '@/api/ocr.api';

/**
 * Polling del estado OCR de una imagen.
 * Se detiene automáticamente cuando llega a un estado final.
 * Intervalo: 3s (igual que el worker en el backend).
 */
export function useOcrEstado(idImagen: string | null) {
  return useQuery({
    queryKey: ['ocr', 'estado', idImagen],
    queryFn: () => ocrApi.getEstado(idImagen!),
    enabled: !!idImagen,
    refetchInterval: (query) => {
      const estado = query.state.data?.estadoImagen;
      if (!estado) return 3000;
      return ESTADOS_IMAGEN_FINALES.includes(estado) ? false : 3000;
    },
  });
}
