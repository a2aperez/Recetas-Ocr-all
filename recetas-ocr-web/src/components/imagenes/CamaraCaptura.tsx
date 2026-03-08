import { useRef, useCallback, useState } from 'react'
import Webcam from 'react-webcam'
import { Camera, RefreshCw, Check, X, AlertTriangle } from 'lucide-react'

interface Props {
  onCaptura: (file: File) => void
  onCancelar: () => void
}

export function CamaraCaptura({ onCaptura, onCancelar }: Props) {
  const webcamRef = useRef<Webcam>(null)
  const [snapshot, setSnapshot] = useState<string | null>(null)
  const [cameraError, setCameraError] = useState<string | null>(null)

  const tomarFoto = useCallback(() => {
    const src = webcamRef.current?.getScreenshot()
    if (src) setSnapshot(src)
  }, [])

  function usarFoto() {
    if (!snapshot) return
    // Convert base64 data URL → File
    const byteStr = atob(snapshot.split(',')[1])
    const arr = new Uint8Array(byteStr.length)
    for (let i = 0; i < byteStr.length; i++) arr[i] = byteStr.charCodeAt(i)
    const blob = new Blob([arr], { type: 'image/jpeg' })
    const file = new File([blob], `captura-${Date.now()}.jpg`, { type: 'image/jpeg' })
    onCaptura(file)
  }

  if (cameraError) {
    return (
      <div className="flex flex-col items-center justify-center py-16 text-center gap-4">
        <AlertTriangle className="w-12 h-12 text-yellow-500" />
        <p className="font-medium text-gray-800">Cámara no disponible</p>
        <p className="text-sm text-gray-500 max-w-xs">{cameraError}</p>
        <button
          onClick={onCancelar}
          className="text-sm text-blue-600 hover:underline"
        >
          Usar galería en su lugar
        </button>
      </div>
    )
  }

  return (
    <div className="flex flex-col items-center gap-5">
      {snapshot ? (
        // Preview of captured photo
        <div className="relative rounded-xl overflow-hidden shadow-md w-full max-w-md">
          <img src={snapshot} alt="Foto capturada" className="w-full" />
        </div>
      ) : (
        // Live webcam feed
        <div className="rounded-xl overflow-hidden shadow-md w-full max-w-md bg-black">
          <Webcam
            ref={webcamRef}
            screenshotFormat="image/jpeg"
            screenshotQuality={0.92}
            videoConstraints={{ facingMode: 'environment' }}
            className="w-full"
            onUserMediaError={(err) =>
              setCameraError(
                typeof err === 'string' ? err : (err as DOMException).message
              )
            }
          />
        </div>
      )}

      {/* Action buttons */}
      <div className="flex items-center gap-4">
        {/* Cancel */}
        <button
          onClick={onCancelar}
          className="p-3 rounded-full border border-gray-300 text-gray-600 hover:bg-gray-100 transition-colors"
          title="Cancelar"
        >
          <X className="w-5 h-5" />
        </button>

        {snapshot ? (
          <>
            {/* Retry */}
            <button
              onClick={() => setSnapshot(null)}
              className="flex items-center gap-2 px-4 py-2.5 rounded-full border border-gray-300 text-gray-700 hover:bg-gray-100 text-sm font-medium transition-colors"
            >
              <RefreshCw className="w-4 h-4" />
              Reintentar
            </button>
            {/* Use photo */}
            <button
              onClick={usarFoto}
              className="flex items-center gap-2 px-5 py-2.5 rounded-full bg-green-600 text-white hover:bg-green-700 text-sm font-semibold transition-colors"
            >
              <Check className="w-4 h-4" />
              Usar esta foto
            </button>
          </>
        ) : (
          // Shutter button
          <button
            onClick={tomarFoto}
            className="w-16 h-16 rounded-full bg-white border-4 border-blue-600 flex items-center justify-center hover:bg-blue-50 active:scale-95 transition-all shadow-md"
            title="Tomar foto"
          >
            <Camera className="w-7 h-7 text-blue-600" />
          </button>
        )}
      </div>
    </div>
  )
}
