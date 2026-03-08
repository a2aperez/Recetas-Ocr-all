import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import toast from 'react-hot-toast'
import { ArrowLeft } from 'lucide-react'
import { gruposRecetaApi } from '@/api/grupos-receta.api'
import { catalogosApi } from '@/api/catalogos.api'
import { PageHeader } from '@/components/common/PageHeader'
import type { GrupoRecetaDto } from '@/types/grupo-receta.types'

interface CrearGrupoResponse extends GrupoRecetaDto {
  creado?: boolean
}

const schema = z.object({
  idAseguradora: z
    .number({ required_error: 'Selecciona aseguradora', invalid_type_error: 'Selecciona aseguradora' })
    .positive('Selecciona aseguradora'),
  folioBase: z.string().optional(),
  fechaConsulta: z.string().min(1, 'Fecha requerida'),
  nombrePaciente: z.string().optional(),
  nombreMedico: z.string().optional(),
})

type FormData = z.infer<typeof schema>

const inputClass =
  'w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-50 disabled:text-gray-400'

export default function NuevoGrupoPage() {
  const navigate = useNavigate()
  const qc = useQueryClient()

  const { data: aseguradoras, isLoading: loadingAseg } = useQuery({
    queryKey: ['catalogos', 'aseguradoras'],
    queryFn: catalogosApi.getAseguradoras,
    staleTime: 1000 * 60 * 5,
  })

  const { mutateAsync, isPending } = useMutation({
    mutationFn: (data: FormData) =>
      gruposRecetaApi.crear({
        idAseguradora: data.idAseguradora,
        folioBase: data.folioBase || undefined,
        fechaConsulta: data.fechaConsulta,
        nombrePaciente: data.nombrePaciente || undefined,
        nombreMedico: data.nombreMedico || undefined,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['grupos-receta'] })
    },
    onError: (err: Error) => toast.error(err.message),
  })

  const {
    register,
    handleSubmit,
    control,
    formState: { errors },
  } = useForm<FormData>({ resolver: zodResolver(schema) })

  async function onSubmit(data: FormData) {
    try {
      const grupo = (await mutateAsync(data)) as CrearGrupoResponse
      if (grupo.creado === false) {
        toast('Ya existe un grupo con ese folio, redirigiendo...', { icon: 'ℹ️' })
      } else {
        toast.success('Grupo creado')
      }
      navigate(`/grupos-receta/${grupo.id}`)
    } catch {
      // error already surfaced by onError
    }
  }

  return (
    <div className="p-6 max-w-2xl mx-auto">
      <PageHeader
        title="Nuevo Grupo de Receta"
        subtitle="Registra un nuevo grupo para gestionar recetas médicas"
        actions={
          <button
            onClick={() => navigate(-1)}
            className="inline-flex items-center gap-2 text-gray-600 hover:text-gray-900 text-sm"
          >
            <ArrowLeft className="w-4 h-4" />
            Volver
          </button>
        }
      />

      <div className="bg-white rounded-xl border border-gray-200 p-6">
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
          {/* Aseguradora */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Aseguradora <span className="text-red-500">*</span>
            </label>
            <Controller
              name="idAseguradora"
              control={control}
              render={({ field }) => (
                <select
                  value={field.value ?? ''}
                  onChange={e =>
                    field.onChange(e.target.value === '' ? undefined : Number(e.target.value))
                  }
                  disabled={loadingAseg}
                  className={inputClass}
                >
                  <option value="">
                    {loadingAseg ? 'Cargando...' : 'Selecciona aseguradora...'}
                  </option>
                  {aseguradoras?.map(a => (
                    <option key={a.id} value={a.id}>
                      {a.nombre}
                    </option>
                  ))}
                </select>
              )}
            />
            {errors.idAseguradora && (
              <p className="mt-1 text-xs text-red-500">{errors.idAseguradora.message}</p>
            )}
          </div>

          {/* Folio Base */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Folio Base
              <span className="ml-2 text-xs text-gray-400 font-normal">
                Dejar vacío si no tiene folio
              </span>
            </label>
            <input
              {...register('folioBase')}
              type="text"
              placeholder="Ej. RX-2024-001"
              className={inputClass}
            />
          </div>

          {/* Fecha Consulta */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Fecha de Consulta <span className="text-red-500">*</span>
            </label>
            <input {...register('fechaConsulta')} type="date" className={inputClass} />
            {errors.fechaConsulta && (
              <p className="mt-1 text-xs text-red-500">{errors.fechaConsulta.message}</p>
            )}
          </div>

          {/* Nombre Paciente */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Nombre del Paciente
            </label>
            <input
              {...register('nombrePaciente')}
              type="text"
              placeholder="Opcional — se extrae del OCR automáticamente"
              className={inputClass}
            />
          </div>

          {/* Nombre Médico */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Nombre del Médico
            </label>
            <input
              {...register('nombreMedico')}
              type="text"
              placeholder="Opcional — se extrae del OCR automáticamente"
              className={inputClass}
            />
          </div>

          {/* Actions */}
          <div className="flex items-center justify-end gap-3 pt-2 border-t border-gray-100">
            <button
              type="button"
              onClick={() => navigate(-1)}
              className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={isPending}
              className="px-6 py-2 text-sm font-medium text-white bg-blue-600 rounded-lg hover:bg-blue-700 disabled:opacity-60 disabled:cursor-not-allowed transition-colors inline-flex items-center gap-2"
            >
              {isPending && (
                <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
              )}
              {isPending ? 'Creando...' : 'Crear Grupo'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
