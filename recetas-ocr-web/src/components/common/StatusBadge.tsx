import type { ReactNode } from 'react';

const ESTADO_LABELS: Record<string, string> = {
  RECIBIDA: 'Recibida',
  RECIBIDO: 'Recibido',
  LEGIBLE: 'Legible',
  ILEGIBLE: 'Ilegible',
  CAPTURA_MANUAL_COMPLETA: 'Captura Manual',
  OCR_APROBADO: 'OCR Aprobado',
  OCR_BAJA_CONFIANZA: 'Baja Confianza',
  EXTRACCION_COMPLETA: 'Extracción Completa',
  EXTRACCION_INCOMPLETA: 'Extracción Incompleta',
  REVISADA: 'Revisada',
  RECHAZADA: 'Rechazada',
  RECHAZADO: 'Rechazado',
  PROCESANDO: 'Procesando',
  GRUPO_INCOMPLETO: 'Incompleto',
  REVISION_PENDIENTE: 'Revisión Pendiente',
  REVISADO_COMPLETO: 'Revisado',
  DATOS_FISCALES_INCOMPLETOS: 'Datos Fiscales Incompletos',
  PENDIENTE_AUTORIZACION: 'Pendiente Autorización',
  PENDIENTE_FACTURACION: 'Pendiente Facturación',
  PREFACTURA_GENERADA: 'Prefactura Generada',
  FACTURADA: 'Facturada',
  ERROR_TIMBRADO_MANUAL: 'Error Timbrado',
  REQUIERE_CAPTURA_MANUAL: 'Captura Manual',
};

function getClasses(estado: string): string {
  switch (estado) {
    case 'RECIBIDA':
    case 'RECIBIDO':
    case 'LEGIBLE':
      return 'bg-gray-100 text-gray-700';
    case 'OCR_APROBADO':
    case 'REVISADO_COMPLETO':
    case 'EXTRACCION_COMPLETA':
    case 'REVISADA':
      return 'bg-green-100 text-green-800';
    case 'OCR_BAJA_CONFIANZA':
    case 'EXTRACCION_INCOMPLETA':
    case 'DATOS_FISCALES_INCOMPLETOS':
    case 'PENDIENTE_AUTORIZACION':
    case 'PENDIENTE_FACTURACION':
    case 'PREFACTURA_GENERADA':
      return 'bg-yellow-100 text-yellow-800';
    case 'ILEGIBLE':
    case 'RECHAZADO':
    case 'RECHAZADA':
    case 'ERROR_TIMBRADO_MANUAL':
      return 'bg-red-100 text-red-800';
    case 'PROCESANDO':
    case 'REVISION_PENDIENTE':
    case 'GRUPO_INCOMPLETO':
      return 'bg-blue-100 text-blue-800';
    case 'FACTURADA':
      return 'bg-purple-100 text-purple-800';
    case 'REQUIERE_CAPTURA_MANUAL':
    case 'CAPTURA_MANUAL_COMPLETA':
      return 'bg-orange-100 text-orange-800';
    default:
      return 'bg-gray-100 text-gray-600';
  }
}

interface Props {
  estado: string;
  size?: 'sm' | 'md';
}

export function StatusBadge({ estado, size = 'md' }: Props): ReactNode {
  const label = ESTADO_LABELS[estado] ?? estado;
  const colorClass = getClasses(estado);
  const sizeClass = size === 'sm' ? 'text-xs px-2 py-0.5' : 'text-xs px-2.5 py-1';
  return (
    <span className={`inline-flex items-center rounded-full font-medium ${colorClass} ${sizeClass}`}>
      {label}
    </span>
  );
}
