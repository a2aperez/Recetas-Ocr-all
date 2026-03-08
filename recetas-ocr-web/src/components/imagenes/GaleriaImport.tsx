import { useCallback, useState } from 'react'
import { useDropzone } from 'react-dropzone'
import { Upload, X, AlertCircle, FileImage } from 'lucide-react'

const MAX_MB = Number(import.meta.env.VITE_MAX_IMAGE_SIZE_MB ?? 20)
const MAX_BYTES = MAX_MB * 1024 * 1024

const ACCEPTED_TYPES: Record<string, string[]> = {
  'image/jpeg': ['.jpg', '.jpeg'],
  'image/png': ['.png'],
  'image/heic': ['.heic'],
  'application/pdf': ['.pdf'],
}

interface FileEntry {
  file: File
  preview: string | null
  error: string | null
}

interface Props {
  onArchivos: (files: File[]) => void
}

export function GaleriaImport({ onArchivos }: Props) {
  const [entries, setEntries] = useState<FileEntry[]>([])
  const [dropErrors, setDropErrors] = useState<string[]>([])

  const onDrop = useCallback(
    (accepted: File[], rejected: import('react-dropzone').FileRejection[]) => {
      setDropErrors([])

      const newErrors: string[] = rejected.map(r => {
        const name = r.file.name
        const code = r.errors[0]?.code
        if (code === 'file-too-large')
          return `"${name}" excede el límite de ${MAX_MB} MB`
        if (code === 'file-invalid-type')
          return `"${name}" tipo no permitido`
        return `"${name}" no se pudo agregar`
      })
      if (newErrors.length) setDropErrors(newErrors)

      const newEntries: FileEntry[] = accepted.map(f => ({
        file: f,
        preview: f.type.startsWith('image/') ? URL.createObjectURL(f) : null,
        error: f.size > MAX_BYTES ? `Excede ${MAX_MB} MB` : null,
      }))

      setEntries(prev => {
        const updated = [...prev, ...newEntries]
        onArchivos(updated.filter(e => !e.error).map(e => e.file))
        return updated
      })
    },
    [onArchivos]
  )

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: ACCEPTED_TYPES,
    maxSize: MAX_BYTES,
    multiple: true,
  })

  function removeEntry(idx: number) {
    setEntries(prev => {
      const next = prev.filter((_, i) => i !== idx)
      onArchivos(next.filter(e => !e.error).map(e => e.file))
      return next
    })
  }

  function formatSize(bytes: number) {
    if (bytes < 1024) return `${bytes} B`
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
  }

  return (
    <div className="space-y-4">
      {/* Drop zone */}
      <div
        {...getRootProps()}
        className={`border-2 border-dashed rounded-xl p-10 text-center cursor-pointer transition-colors ${
          isDragActive
            ? 'border-blue-500 bg-blue-50'
            : 'border-gray-300 hover:border-blue-400 hover:bg-gray-50'
        }`}
      >
        <input {...getInputProps()} />
        <Upload className="w-10 h-10 mx-auto mb-3 text-gray-400" />
        {isDragActive ? (
          <p className="text-sm font-medium text-blue-600">Suelta los archivos aquí...</p>
        ) : (
          <>
            <p className="text-sm font-medium text-gray-700">
              Arrastra archivos o{' '}
              <span className="text-blue-600 underline">haz clic para seleccionar</span>
            </p>
            <p className="text-xs text-gray-400 mt-1">
              JPG, PNG, HEIC, PDF · Máx. {MAX_MB} MB por archivo
            </p>
          </>
        )}
      </div>

      {/* Drop errors */}
      {dropErrors.length > 0 && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-3 space-y-1">
          {dropErrors.map((e, i) => (
            <p key={i} className="text-xs text-red-600 flex items-center gap-1.5">
              <AlertCircle className="w-3.5 h-3.5 shrink-0" />
              {e}
            </p>
          ))}
        </div>
      )}

      {/* File previews */}
      {entries.length > 0 && (
        <div className="space-y-2">
          {entries.map((entry, idx) => (
            <div
              key={idx}
              className={`flex items-center gap-3 p-2.5 rounded-lg border ${
                entry.error
                  ? 'border-red-200 bg-red-50'
                  : 'border-gray-200 bg-white'
              }`}
            >
              {/* Thumbnail */}
              <div className="w-12 h-12 rounded-lg overflow-hidden bg-gray-100 shrink-0 flex items-center justify-center">
                {entry.preview ? (
                  <img
                    src={entry.preview}
                    alt={entry.file.name}
                    className="w-full h-full object-cover"
                  />
                ) : (
                  <FileImage className="w-6 h-6 text-gray-400" />
                )}
              </div>

              {/* Info */}
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-gray-800 truncate">{entry.file.name}</p>
                <p className="text-xs text-gray-400">{formatSize(entry.file.size)}</p>
                {entry.error && (
                  <p className="text-xs text-red-500 flex items-center gap-1 mt-0.5">
                    <AlertCircle className="w-3 h-3" />
                    {entry.error}
                  </p>
                )}
              </div>

              {/* Remove */}
              <button
                onClick={() => removeEntry(idx)}
                className="p-1.5 rounded-lg text-gray-400 hover:text-red-500 hover:bg-red-50 transition-colors shrink-0"
                title="Quitar archivo"
              >
                <X className="w-4 h-4" />
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
