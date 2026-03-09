using RecetasOCR.Application.DTOs.Imagenes;

namespace RecetasOCR.Application.DTOs;

public class OcrResultadoDto
{
    public Guid IdImagen { get; set; }
    public bool Exitoso { get; set; }
    public bool EsLegible { get; set; }
    public bool EsConfianzaBaja { get; set; }
    public decimal ConfianzaPromedio { get; set; }
    public string CalidadLectura { get; set; } = "";
    public List<string> CamposIlegibles { get; set; } = new();
    public string? Notas { get; set; }
    public string? ErrorMensaje { get; set; }
    public long DuracionMs { get; set; }

    // Datos de la receta
    public string? AseguradoraDetectada { get; set; }
    public string? FormatoReceta { get; set; }

    // Paciente
    public string? NombrePaciente { get; set; }
    public string? ApellidoPaterno { get; set; }
    public string? ApellidoMaterno { get; set; }
    public string? NombresPaciente { get; set; }
    public string? Nomina { get; set; }
    public string? Credencial { get; set; }
    public string? NUR { get; set; }
    public string? NumeroAutorizacion { get; set; }
    public string? Elegibilidad { get; set; }

    // Médico
    public string? NombreMedico { get; set; }
    public string? CedulaMedico { get; set; }
    public string? EspecialidadMedico { get; set; }
    public string? InstitucionMedico { get; set; }

    // Consulta
    public string? FechaConsulta { get; set; }
    public string? DiagnosticoTexto { get; set; }
    public string? CodigoCIE10 { get; set; }

    // Medicamentos
    public List<MedicamentoOcrDto> Medicamentos { get; set; } = new();

    public static OcrResultadoDto Fallido(string error) => new()
    {
        Exitoso      = false,
        EsLegible    = false,
        ErrorMensaje = error
    };
}
