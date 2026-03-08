namespace RecetasOCR.Domain.Enums;

/// <summary>
/// Origen de la imagen subida por el usuario.
/// CAMARA  : foto tomada en el momento con la cámara del dispositivo.
/// GALERIA : imagen importada desde la galería/almacenamiento del dispositivo.
/// API     : subida directamente via integración de API.
/// ESCANER : digitalizada con escáner físico.
/// </summary>
public enum OrigenImagen
{
    Camara,
    Galeria,
    Api,
    Escaner
}
