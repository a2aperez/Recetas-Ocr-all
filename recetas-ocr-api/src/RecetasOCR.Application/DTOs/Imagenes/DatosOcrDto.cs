namespace RecetasOCR.Application.DTOs.Imagenes;

/// <summary>
/// Datos completos extraídos por OCR, incluidos en la respuesta de SubirImagen
/// para que el front pueda mostrar la información sin una segunda llamada.
/// </summary>
public class DatosOcrExtraidosDto
{
    // Paciente
    public string? NombrePaciente     { get; set; }
    public string? ApellidoPaterno    { get; set; }
    public string? ApellidoMaterno    { get; set; }
    public string? Nomina             { get; set; }
    public string? Credencial         { get; set; }
    public string? NUR                { get; set; }
    public string? NumeroAutorizacion { get; set; }
    public string? Elegibilidad       { get; set; }

    // Médico
    public string? NombreMedico { get; set; }
    public string? CedulaMedico { get; set; }
    public string? Especialidad { get; set; }

    // Consulta
    public string? FechaConsulta    { get; set; }
    public string? DiagnosticoTexto { get; set; }
    public string? CodigoCIE10      { get; set; }

    // Medicamentos extraídos
    public List<MedicamentoOcrDto> Medicamentos { get; set; } = new();

    // Metadata OCR
    public decimal      ConfianzaPromedio { get; set; }
    public string       CalidadLectura    { get; set; } = "";
    public List<string> CamposIlegibles   { get; set; } = new();
    public string?      Notas             { get; set; }
    public bool         EsConfianzaBaja   { get; set; }
}

public class MedicamentoOcrDto
{
    public int?    Numero             { get; set; }
    public string? NombreComercial    { get; set; }
    public string? SustanciaActiva    { get; set; }
    public string? Presentacion       { get; set; }
    public string? Dosis              { get; set; }
    public string? CantidadTexto      { get; set; }
    public int?    CantidadNumero     { get; set; }
    public string? UnidadCantidad     { get; set; }
    public string? ViaAdministracion  { get; set; }
    public string? FrecuenciaTexto    { get; set; }
    public string? FrecuenciaExpandida { get; set; }
    public string? DuracionTexto      { get; set; }
    public int?    DuracionDias       { get; set; }
    public string? Indicaciones       { get; set; }
    public string? CodigoCIE10        { get; set; }
    public string? CodigoEAN          { get; set; }
    public int?    NumeroPrescripcion { get; set; }
}
