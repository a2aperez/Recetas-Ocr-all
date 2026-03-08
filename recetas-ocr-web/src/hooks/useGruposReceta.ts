import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import toast from 'react-hot-toast'
import { gruposRecetaApi } from '@/api/grupos-receta.api'
import type { FiltrosGrupoDto } from '@/types/grupo-receta.types'

export function useGruposReceta(filtros: FiltrosGrupoDto) {
  return useQuery({
    queryKey: ['grupos-receta', 'list', filtros],
    queryFn: () => gruposRecetaApi.listar(filtros),
  })
}

export function useGrupoDetalle(id: string) {
  return useQuery({
    queryKey: ['grupos-receta', 'detail', id],
    queryFn: () => gruposRecetaApi.obtener(id),
    enabled: !!id,
  })
}

export function useCrearGrupo() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: gruposRecetaApi.crear,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['grupos-receta'] })
      toast.success('Grupo creado correctamente')
    },
    onError: (err: Error) => toast.error(err.message),
  })
}
