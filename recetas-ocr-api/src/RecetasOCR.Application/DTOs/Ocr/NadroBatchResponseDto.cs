using System.Text.Json.Serialization;

namespace RecetasOCR.Application.DTOs.Ocr;

// Respuesta raíz
public record NadroBatchResponseDto(
    [property: JsonPropertyName("success")]        bool Success,
    [property: JsonPropertyName("totalFiles")]      int TotalFiles,
    [property: JsonPropertyName("successful")]      int Successful,
    [property: JsonPropertyName("failed")]          int Failed,
    [property: JsonPropertyName("totalDurationMs")] int TotalDurationMs,
    [property: JsonPropertyName("results")]         List<NadroFileResultDto> Results);

// Resultado por archivo
public record NadroFileResultDto(
    [property: JsonPropertyName("fileName")]   string FileName,
    [property: JsonPropertyName("success")]    bool Success,
    [property: JsonPropertyName("data")]       NadroRecetaDataDto? Data,
    [property: JsonPropertyName("error")]      string? Error,
    [property: JsonPropertyName("durationMs")] int DurationMs);

// Datos de la receta
public record NadroRecetaDataDto(
    [property: JsonPropertyName("paginas")]  List<NadroPaginaDto> Paginas,
    [property: JsonPropertyName("metadata")] NadroMetadataDto Metadata);

// Una página/hoja
public record NadroPaginaDto(
    [property: JsonPropertyName("aseguradora_detectada")] string? AseguradoraDetectada,
    [property: JsonPropertyName("formato_receta")]        string? FormatoReceta,
    [property: JsonPropertyName("paciente")]              NadroPacienteDto? Paciente,
    [property: JsonPropertyName("medico")]                NadroMedicoDto? Medico,
    [property: JsonPropertyName("consulta")]              NadroConsultaDto? Consulta,
    [property: JsonPropertyName("medicamentos")]          List<NadroMedicamentoDto> Medicamentos,
    [property: JsonPropertyName("metadata_lectura")]      NadroMetadataLecturaDto MetadataLectura);

// Paciente
public record NadroPacienteDto(
    [property: JsonPropertyName("nombre_completo")]     string? NombreCompleto,
    [property: JsonPropertyName("apellido_paterno")]    string? ApellidoPaterno,
    [property: JsonPropertyName("apellido_materno")]    string? ApellidoMaterno,
    [property: JsonPropertyName("nombres")]             string? Nombres,
    [property: JsonPropertyName("fecha_nacimiento")]    string? FechaNacimiento,
    [property: JsonPropertyName("nomina")]              string? Nomina,
    [property: JsonPropertyName("credencial")]          string? Credencial,
    [property: JsonPropertyName("nur")]                 string? Nur,
    [property: JsonPropertyName("numero_autorizacion")] string? NumeroAutorizacion,
    [property: JsonPropertyName("elegibilidad")]        string? Elegibilidad,
    [property: JsonPropertyName("clave_dh")]            string? ClaveDh,
    [property: JsonPropertyName("clave_beneficiario")]  string? ClaveBeneficiario);

// Médico
public record NadroMedicoDto(
    [property: JsonPropertyName("nombre_completo")]    string? NombreCompleto,
    [property: JsonPropertyName("apellido_paterno")]   string? ApellidoPaterno,
    [property: JsonPropertyName("apellido_materno")]   string? ApellidoMaterno,
    [property: JsonPropertyName("nombres")]            string? Nombres,
    [property: JsonPropertyName("cedula_profesional")] string? CedulaProfesional,
    [property: JsonPropertyName("clave_medico")]       string? ClaveMedico,
    [property: JsonPropertyName("especialidad")]       string? Especialidad,
    [property: JsonPropertyName("institucion")]        string? Institucion,
    [property: JsonPropertyName("direccion")]          string? Direccion,
    [property: JsonPropertyName("telefono")]           string? Telefono);

// Consulta
public record NadroConsultaDto(
    [property: JsonPropertyName("fecha")]             string? Fecha,
    [property: JsonPropertyName("hora")]              string? Hora,
    [property: JsonPropertyName("diagnostico_texto")] string? DiagnosticoTexto,
    [property: JsonPropertyName("codigo_cie10")]      string? CodigoCie10);

// Medicamento
public record NadroMedicamentoDto(
    [property: JsonPropertyName("numero")]               int Numero,
    [property: JsonPropertyName("nombre_comercial")]     string? NombreComercial,
    [property: JsonPropertyName("sustancia_activa")]     string? SustanciaActiva,
    [property: JsonPropertyName("presentacion")]         string? Presentacion,
    [property: JsonPropertyName("dosis")]                string? Dosis,
    [property: JsonPropertyName("cantidad_texto")]       string? CantidadTexto,
    [property: JsonPropertyName("cantidad_numero")]      int? CantidadNumero,
    [property: JsonPropertyName("unidad_cantidad")]      string? UnidadCantidad,
    [property: JsonPropertyName("via_administracion")]   string? ViaAdministracion,
    [property: JsonPropertyName("frecuencia_texto")]     string? FrecuenciaTexto,
    [property: JsonPropertyName("frecuencia_expandida")] string? FrecuenciaExpandida,
    [property: JsonPropertyName("duracion_texto")]       string? DuracionTexto,
    [property: JsonPropertyName("duracion_dias")]        int? DuracionDias,
    [property: JsonPropertyName("indicaciones")]         string? Indicaciones,
    [property: JsonPropertyName("codigo_cie10")]         string? CodigoCie10,
    [property: JsonPropertyName("numero_autorizacion")]  string? NumeroAutorizacion,
    [property: JsonPropertyName("codigo_ean")]           string? CodigoEan);

// Metadata lectura
public record NadroMetadataLecturaDto(
    [property: JsonPropertyName("calidad_lectura")]    string CalidadLectura,
    [property: JsonPropertyName("porcentaje_lectura")] int PorcentajeLectura,
    [property: JsonPropertyName("campos_ilegibles")]   List<string> CamposIlegibles,
    [property: JsonPropertyName("notas")]              string? Notas);

// Metadata API
public record NadroMetadataDto(
    [property: JsonPropertyName("modelo")]        string Modelo,
    [property: JsonPropertyName("proveedor")]     string Proveedor,
    [property: JsonPropertyName("mime_type")]     string MimeType,
    [property: JsonPropertyName("tokens_input")]  int TokensInput,
    [property: JsonPropertyName("tokens_output")] int TokensOutput,
    [property: JsonPropertyName("duracion_ms")]   int DuracionMs);
