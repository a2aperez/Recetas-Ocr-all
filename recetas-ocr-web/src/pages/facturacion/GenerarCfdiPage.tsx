import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { format } from 'date-fns'
import toast from 'react-hot-toast'
import {
  ArrowLeft, Download, Copy, Check, AlertCircle,
  RefreshCw, Stamp, FileText,
} from 'lucide-react'
import { facturacionApi } from '@/api/facturacion.api'
import { gruposRecetaApi } from '@/api/grupos-receta.api'
import { PageHeader } from '@/components/common/PageHeader'
import { StatusBadge } from '@/components/common/StatusBadge'
import { usePermisos } from '@/hooks/usePermisos'
import type { DatosFiscalesDto, ConceptoFacturaDto, PreFacturaDto, CfdiDto } from '@/types/facturacion.types'
import { useState } from 'react'

// ─── Catálogos SAT embebidos ─────────────────────────────────────────────────

const USOS_CFDI = [
  { clave: 'G01', desc: 'Adquisición de mercancias' },
  { clave: 'G03', desc: 'Gastos en general' },
  { clave: 'S01', desc: 'Sin efectos fiscales' },
  { clave: 'CP01', desc: 'Pagos' },
  { clave: 'D01', desc: 'Honorarios médicos y dentales' },
  { clave: 'D07', desc: 'Primas por seguros de gastos médicos' },
]

const METODOS_PAGO = [
  { clave: 'PUE', desc: 'PUE - Pago en una sola exhibición' },
  { clave: 'PPD', desc: 'PPD - Pago en parcialidades o diferido' },
]

const FORMAS_PAGO = [
  { clave: '01', desc: '01 - Efectivo' },
  { clave: '03', desc: '03 - Transferencia electrónica' },
  { clave: '04', desc: '04 - Tarjeta de crédito' },
  { clave: '28', desc: '28 - Tarjeta de débito' },
  { clave: '99', desc: '99 - Por definir' },
]

const REGIMENES_FISCALES = [
  { clave: '605', desc: 'Sueldos y salarios e ingresos asimilados' },
  { clave: '606', desc: 'Arrendamiento' },
  { clave: '608', desc: 'Demás ingresos' },
  { clave: '611', desc: 'Ingresos por dividendos' },
  { clave: '612', desc: 'Personas físicas con actividades empresariales' },
  { clave: '616', desc: 'Sin obligaciones fiscales' },
  { clave: '621', desc: 'Incorporación fiscal' },
  { clave: '622', desc: 'Actividades agrícolas, ganaderas, silvícolas' },
  { clave: '625', desc: 'Régimen de las actividades empresariales con ingresos a través de plataformas' },
  { clave: '626', desc: 'Régimen simplificado de confianza' },
]

// ─── Zod schema ──────────────────────────────────────────────────────────────

const RFC_REGEX = /^[A-Z&Ñ]{3,4}[0-9]{6}[A-Z0-9]{3}$/

const schema = z.object({
  rfcReceptor: z
    .string()
    .min(1, 'RFC requerido')
    .regex(RFC_REGEX, 'RFC inválido (ej. XAXX010101000)'),
  nombreReceptor: z.string().min(1, 'Nombre fiscal requerido'),
  usoCfdi: z.string().min(1, 'Selecciona uso CFDI'),
  metodoPago: z.string().min(1, 'Selecciona método de pago'),
  formaPago: z.string().min(1, 'Selecciona forma de pago'),
  regimenFiscalReceptor: z.string().optional(),
  domicilioFiscalReceptor: z.string().optional(),
})

type FormData = z.infer<typeof schema>

// ─── helpers ─────────────────────────────────────────────────────────────────

const inputClass =
  'w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-50 disabled:text-gray-400'

const labelClass = 'block text-sm font-medium text-gray-700 mb-1'

function SectionTitle({ children }: { children: React.ReactNode }) {
  return (
    <h2 className="text-base font-semibold text-gray-900 mb-4 flex items-center gap-2">
      {children}
    </h2>
  )
}

function MxCurrency({ value }: { value: number }) {
  return (
    <span>
      {value.toLocaleString('es-MX', { style: 'currency', currency: 'MXN' })}
    </span>
  )
}

// ─── Sección 2: Prefactura ───────────────────────────────────────────────────

function PrefacturaSection({
  idGrupo,
  prefactura,
  isLoading,
  onGenerar,
  isGenerating,
}: {
  idGrupo: string
  prefactura: PreFacturaDto | undefined
  isLoading: boolean
  onGenerar: () => void
  isGenerating: boolean
}) {
  const { puedeLeer: puedeTimbrar } = usePermisos('FACTURACION.TIMBRAR')
  const qc = useQueryClient()

  const { mutate: timbrar, isPending: isTimbrando } = useMutation({
    mutationFn: () => facturacionApi.timbrar(prefactura!.id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['facturacion', 'cfdis', idGrupo] })
      qc.invalidateQueries({ queryKey: ['facturacion', 'prefactura', idGrupo] })
      toast.success('CFDI timbrado correctamente')
    },
    onError: (err: Error) => toast.error(`Error al timbrar: ${err.message}`),
  })

  if (isLoading) {
    return (
      <div className="space-y-2">
        {[1, 2, 3].map(i => (
          <div key={i} className="h-10 bg-gray-100 rounded-lg animate-pulse" />
        ))}
      </div>
    )
  }

  if (!prefactura) {
    return (
      <div className="text-center py-10">
        <FileText className="w-10 h-10 mx-auto mb-3 text-gray-300" />
        <p className="text-sm text-gray-500 mb-4">No se ha generado una prefactura para este grupo</p>
        <button
          onClick={onGenerar}
          disabled={isGenerating}
          className="inline-flex items-center gap-2 bg-blue-600 text-white px-5 py-2.5 rounded-lg text-sm font-medium hover:bg-blue-700 disabled:opacity-60 transition-colors"
        >
          {isGenerating && (
            <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
          )}
          <RefreshCw className="w-4 h-4" />
          Generar prefactura
        </button>
      </div>
    )
  }

  const columns: ConceptoFacturaDto[] = prefactura.conceptos

  return (
    <div className="space-y-5">
      {/* Conceptos */}
      <div className="overflow-x-auto rounded-lg border border-gray-200">
        <table className="min-w-full divide-y divide-gray-200 text-sm">
          <thead className="bg-gray-50">
            <tr>
              {['Descripción', 'Cant.', 'Precio Unit.', 'Descuento', 'Impuestos', 'Importe'].map(h => (
                <th
                  key={h}
                  className="px-3 py-2.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider whitespace-nowrap"
                >
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-100">
            {columns.map((c, i) => (
              <tr key={i} className="hover:bg-gray-50">
                <td className="px-3 py-2">
                  <div>
                    <p className="font-medium text-gray-900">{c.descripcion}</p>
                    <p className="text-xs text-gray-400 font-mono">{c.claveProdServ} · {c.claveUnidad}</p>
                  </div>
                </td>
                <td className="px-3 py-2 text-center">{c.cantidad}</td>
                <td className="px-3 py-2"><MxCurrency value={c.valorUnitario} /></td>
                <td className="px-3 py-2">
                  {c.descuento ? <MxCurrency value={c.descuento} /> : '—'}
                </td>
                <td className="px-3 py-2"><MxCurrency value={c.impuestos} /></td>
                <td className="px-3 py-2 font-semibold"><MxCurrency value={c.importe} /></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Totals */}
      <div className="flex justify-end">
        <div className="bg-gray-50 rounded-xl border border-gray-200 p-4 w-64 space-y-2 text-sm">
          <div className="flex justify-between">
            <span className="text-gray-500">Subtotal</span>
            <span className="font-medium"><MxCurrency value={prefactura.subtotal} /></span>
          </div>
          {prefactura.descuento > 0 && (
            <div className="flex justify-between text-red-600">
              <span>Descuento</span>
              <span>-<MxCurrency value={prefactura.descuento} /></span>
            </div>
          )}
          <div className="flex justify-between">
            <span className="text-gray-500">IVA</span>
            <span className="font-medium"><MxCurrency value={prefactura.impuestos} /></span>
          </div>
          <div className="flex justify-between border-t border-gray-200 pt-2 text-base font-bold text-gray-900">
            <span>Total</span>
            <MxCurrency value={prefactura.total} />
          </div>
        </div>
      </div>

      {/* Timbrar button */}
      {puedeTimbrar && prefactura.estado === 'BORRADOR' && (
        <div className="flex justify-end">
          <button
            onClick={() => timbrar()}
            disabled={isTimbrando}
            className="inline-flex items-center gap-2 bg-green-600 text-white px-5 py-2.5 rounded-lg text-sm font-semibold hover:bg-green-700 disabled:opacity-60 transition-colors"
          >
            {isTimbrando ? (
              <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
            ) : (
              <Stamp className="w-4 h-4" />
            )}
            {isTimbrando ? 'Timbrando...' : 'Timbrar CFDI'}
          </button>
        </div>
      )}
    </div>
  )
}

// ─── Sección 3: CFDI timbrado ────────────────────────────────────────────────

function CfdiTimbradoSection({ cfdi }: { cfdi: CfdiDto }) {
  const [copied, setCopied] = useState(false)

  function copyUuid() {
    if (!cfdi.uuid) return
    navigator.clipboard.writeText(cfdi.uuid)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3 flex-wrap">
        <StatusBadge estado={cfdi.estado} />
        {cfdi.fechaTimbrado && (
          <span className="text-sm text-gray-500">
            Timbrado el {format(new Date(cfdi.fechaTimbrado), 'dd/MM/yyyy HH:mm')}
          </span>
        )}
      </div>

      {/* UUID */}
      {cfdi.uuid && (
        <div className="bg-gray-50 border border-gray-200 rounded-lg p-3 flex items-center gap-3">
          <div className="flex-1 min-w-0">
            <p className="text-xs text-gray-500 mb-0.5">UUID</p>
            <p className="font-mono text-sm text-gray-900 break-all">{cfdi.uuid}</p>
          </div>
          <button
            onClick={copyUuid}
            className="shrink-0 p-2 rounded-lg hover:bg-gray-200 transition-colors"
            title="Copiar UUID"
          >
            {copied ? (
              <Check className="w-4 h-4 text-green-600" />
            ) : (
              <Copy className="w-4 h-4 text-gray-500" />
            )}
          </button>
        </div>
      )}

      {/* Info row */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 text-sm">
        {cfdi.serie && (
          <div>
            <p className="text-xs text-gray-500">Serie</p>
            <p className="font-medium">{cfdi.serie}</p>
          </div>
        )}
        {cfdi.folio && (
          <div>
            <p className="text-xs text-gray-500">Folio</p>
            <p className="font-medium">{cfdi.folio}</p>
          </div>
        )}
        <div>
          <p className="text-xs text-gray-500">RFC Emisor</p>
          <p className="font-mono font-medium">{cfdi.rfcEmisor}</p>
        </div>
        <div>
          <p className="text-xs text-gray-500">RFC Receptor</p>
          <p className="font-mono font-medium">{cfdi.rfcReceptor}</p>
        </div>
        <div>
          <p className="text-xs text-gray-500">Total</p>
          <p className="font-semibold text-gray-900">
            <MxCurrency value={cfdi.total} />
          </p>
        </div>
      </div>

      {/* Download buttons */}
      <div className="flex items-center gap-3 pt-2">
        <button
          onClick={() => window.open(cfdi.xmlUrl!, '_blank', 'noopener,noreferrer')}
          disabled={!cfdi.xmlUrl}
          className="inline-flex items-center gap-2 border border-gray-300 bg-white text-gray-700 px-4 py-2 rounded-lg text-sm font-medium hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
        >
          <Download className="w-4 h-4" />
          Descargar XML
        </button>
        <button
          onClick={() => window.open(cfdi.pdfUrl!, '_blank', 'noopener,noreferrer')}
          disabled={!cfdi.pdfUrl}
          className="inline-flex items-center gap-2 border border-gray-300 bg-white text-gray-700 px-4 py-2 rounded-lg text-sm font-medium hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
        >
          <Download className="w-4 h-4" />
          Descargar PDF
        </button>
      </div>
    </div>
  )
}

// ─── PAGE ────────────────────────────────────────────────────────────────────

export default function GenerarCfdiPage() {
  const { idGrupo } = useParams<{ idGrupo: string }>()
  const navigate = useNavigate()
  const qc = useQueryClient()

  // ── queries ──────────────────────────────────────────────────────────────
  const { data: grupo } = useQuery({
    queryKey: ['grupos-receta', 'detail', idGrupo],
    queryFn: () => gruposRecetaApi.obtener(idGrupo!),
    enabled: !!idGrupo,
  })

  const {
    data: prefactura,
    isLoading: loadingPrefactura,
    error: prefacturaError,
  } = useQuery({
    queryKey: ['facturacion', 'prefactura', idGrupo],
    queryFn: () => facturacionApi.getPreFactura(idGrupo!),
    enabled: !!idGrupo,
    retry: false,
  })

  const { data: cfdis } = useQuery({
    queryKey: ['facturacion', 'cfdis', idGrupo],
    queryFn: () => facturacionApi.getCfdisGrupo(idGrupo!),
    enabled: !!idGrupo,
  })

  const cfdiTimbrado = cfdis?.find(c => c.estado === 'TIMBRADO')

  // ── mutations ─────────────────────────────────────────────────────────────
  const { mutate: actualizarDatosFiscales, isPending: isSavingDatos } = useMutation({
    mutationFn: (datos: DatosFiscalesDto) =>
      facturacionApi.actualizarDatosFiscales(idGrupo!, datos),
    onSuccess: () => toast.success('Datos fiscales guardados'),
    onError: (err: Error) => toast.error(err.message),
  })

  const { mutate: generarPrefactura, isPending: isGenerating } = useMutation({
    mutationFn: () => facturacionApi.generarPreFactura(idGrupo!),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['facturacion', 'prefactura', idGrupo] })
      toast.success('Prefactura generada')
    },
    onError: (err: Error) => {
      const msg = err.message
      // Try to parse list of required fields from error message
      if (msg.includes(':')) {
        const parts = msg.split(':')
        toast.error(
          <div>
            <p className="font-semibold">Faltan datos requeridos:</p>
            <ul className="list-disc list-inside text-sm mt-1">
              {parts[1].split(',').map((f, i) => (
                <li key={i}>{f.trim()}</li>
              ))}
            </ul>
          </div>,
          { duration: 6000 }
        )
      } else {
        toast.error(msg)
      }
    },
  })

  // ── form ──────────────────────────────────────────────────────────────────
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: prefactura
      ? {
          rfcReceptor: prefactura.rfcReceptor,
          nombreReceptor: prefactura.nombreReceptor,
          usoCfdi: prefactura.usoCfdi,
          metodoPago: prefactura.metodoPago,
          formaPago: prefactura.formaPago,
        }
      : {},
  })

  function onSaveDatos(data: FormData) {
    actualizarDatosFiscales(data)
  }

  // ── render ────────────────────────────────────────────────────────────────
  const prefacturaNotFound =
    prefacturaError &&
    (prefacturaError as { response?: { status?: number } }).response?.status === 404

  return (
    <div className="p-6 max-w-3xl mx-auto">
      <PageHeader
        title="Generar CFDI"
        subtitle={grupo ? `Grupo: ${grupo.folioBase ?? 'Sin folio'} · ${grupo.nombreAseguradora}` : 'Cargando...'}
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

      <div className="space-y-6">

        {/* ── SECCIÓN 1: Datos fiscales ─────────────────────────────────────── */}
        <div className="bg-white rounded-xl border border-gray-200 p-6">
          <SectionTitle>
            <FileText className="w-5 h-5 text-blue-600" />
            Datos fiscales del receptor
          </SectionTitle>

          <form onSubmit={handleSubmit(onSaveDatos)} className="space-y-4">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              {/* RFC */}
              <div>
                <label className={labelClass}>
                  RFC Receptor <span className="text-red-500">*</span>
                </label>
                <input
                  {...register('rfcReceptor')}
                  type="text"
                  placeholder="XAXX010101000"
                  className={`${inputClass} font-mono uppercase`}
                  onChange={e =>
                    (e.target.value = e.target.value.toUpperCase())
                  }
                />
                {errors.rfcReceptor && (
                  <p className="mt-1 text-xs text-red-500 flex items-center gap-1">
                    <AlertCircle className="w-3.5 h-3.5" />
                    {errors.rfcReceptor.message}
                  </p>
                )}
              </div>

              {/* Nombre fiscal */}
              <div>
                <label className={labelClass}>
                  Nombre / Razón Social <span className="text-red-500">*</span>
                </label>
                <input
                  {...register('nombreReceptor')}
                  type="text"
                  placeholder="Nombre fiscal completo"
                  className={inputClass}
                />
                {errors.nombreReceptor && (
                  <p className="mt-1 text-xs text-red-500">{errors.nombreReceptor.message}</p>
                )}
              </div>

              {/* Uso CFDI */}
              <div>
                <label className={labelClass}>
                  Uso CFDI <span className="text-red-500">*</span>
                </label>
                <select {...register('usoCfdi')} className={inputClass}>
                  <option value="">Selecciona...</option>
                  {USOS_CFDI.map(u => (
                    <option key={u.clave} value={u.clave}>
                      {u.clave} – {u.desc}
                    </option>
                  ))}
                </select>
                {errors.usoCfdi && (
                  <p className="mt-1 text-xs text-red-500">{errors.usoCfdi.message}</p>
                )}
              </div>

              {/* Método de pago */}
              <div>
                <label className={labelClass}>
                  Método de Pago <span className="text-red-500">*</span>
                </label>
                <select {...register('metodoPago')} className={inputClass}>
                  <option value="">Selecciona...</option>
                  {METODOS_PAGO.map(m => (
                    <option key={m.clave} value={m.clave}>{m.desc}</option>
                  ))}
                </select>
                {errors.metodoPago && (
                  <p className="mt-1 text-xs text-red-500">{errors.metodoPago.message}</p>
                )}
              </div>

              {/* Forma de pago */}
              <div>
                <label className={labelClass}>
                  Forma de Pago <span className="text-red-500">*</span>
                </label>
                <select {...register('formaPago')} className={inputClass}>
                  <option value="">Selecciona...</option>
                  {FORMAS_PAGO.map(f => (
                    <option key={f.clave} value={f.clave}>{f.desc}</option>
                  ))}
                </select>
                {errors.formaPago && (
                  <p className="mt-1 text-xs text-red-500">{errors.formaPago.message}</p>
                )}
              </div>

              {/* Régimen fiscal */}
              <div>
                <label className={labelClass}>Régimen Fiscal</label>
                <select {...register('regimenFiscalReceptor')} className={inputClass}>
                  <option value="">Selecciona (opcional)...</option>
                  {REGIMENES_FISCALES.map(r => (
                    <option key={r.clave} value={r.clave}>
                      {r.clave} – {r.desc}
                    </option>
                  ))}
                </select>
              </div>

              {/* Domicilio fiscal */}
              <div className="sm:col-span-2">
                <label className={labelClass}>Domicilio Fiscal (C.P.)</label>
                <input
                  {...register('domicilioFiscalReceptor')}
                  type="text"
                  placeholder="Código postal del receptor (ej. 06600)"
                  className={inputClass}
                  maxLength={5}
                />
              </div>
            </div>

            <div className="flex justify-end pt-2 border-t border-gray-100">
              <button
                type="submit"
                disabled={isSavingDatos}
                className="inline-flex items-center gap-2 bg-blue-600 text-white px-5 py-2 rounded-lg text-sm font-medium hover:bg-blue-700 disabled:opacity-60 transition-colors"
              >
                {isSavingDatos && (
                  <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                )}
                Guardar datos fiscales
              </button>
            </div>
          </form>
        </div>

        {/* ── SECCIÓN 2: Prefactura ─────────────────────────────────────────── */}
        <div className="bg-white rounded-xl border border-gray-200 p-6">
          <SectionTitle>
            <RefreshCw className="w-5 h-5 text-blue-600" />
            Prefactura
          </SectionTitle>

          <PrefacturaSection
            idGrupo={idGrupo!}
            prefactura={prefacturaNotFound ? undefined : prefactura}
            isLoading={loadingPrefactura && !prefacturaNotFound}
            onGenerar={() => generarPrefactura()}
            isGenerating={isGenerating}
          />
        </div>

        {/* ── SECCIÓN 3: CFDI timbrado ────────────────────────────────────── */}
        {cfdiTimbrado && (
          <div className="bg-white rounded-xl border border-gray-200 p-6">
            <SectionTitle>
              <Stamp className="w-5 h-5 text-green-600" />
              CFDI Timbrado
            </SectionTitle>
            <CfdiTimbradoSection cfdi={cfdiTimbrado} />
          </div>
        )}

      </div>
    </div>
  )
}
