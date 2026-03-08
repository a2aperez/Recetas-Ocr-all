-- ============================================================
-- SISTEMA OCR — RECETAS MÉDICAS MEXICANAS
-- Base de Datos : RecetasOCR
-- Versión       : 5.2 (orden corregido — sin errores de FK)
-- Compatibilidad: SQL Server 2019+ / Azure SQL
-- Collation     : Modern_Spanish_CI_AI
-- Orden de creación:
--   1. Esquemas
--   2. cat.* (catálogos base sin dependencias externas)
--   3. seg.* (referencia cat.Aseguradoras)
--   4. cfg.*
--   5. rec.* med.* ocr.* rev.* fac.* aud.*
--   6. Índices
--   7. Datos iniciales
-- ============================================================

USE master;
GO

IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'RecetasOCR')
    CREATE DATABASE RecetasOCR COLLATE Modern_Spanish_CI_AI;
GO

USE RecetasOCR;
GO

-- ============================================================
-- 1. ESQUEMAS
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'seg') EXEC('CREATE SCHEMA seg');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'cat') EXEC('CREATE SCHEMA cat');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'cfg') EXEC('CREATE SCHEMA cfg');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'rec') EXEC('CREATE SCHEMA rec');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'med') EXEC('CREATE SCHEMA med');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'ocr') EXEC('CREATE SCHEMA ocr');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'rev') EXEC('CREATE SCHEMA rev');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'fac') EXEC('CREATE SCHEMA fac');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'aud') EXEC('CREATE SCHEMA aud');
GO

-- ============================================================
-- 2. CAT — CATÁLOGOS (sin dependencias externas, van primero)
-- ============================================================

-- Aseguradoras — debe ir ANTES de seg.Usuarios que la referencia
CREATE TABLE cat.Aseguradoras (
    Id                  INT             IDENTITY(1,1)   PRIMARY KEY,
    IdAseguradoraPadre  INT             NULL REFERENCES cat.Aseguradoras(Id),
    Clave               NVARCHAR(50)    NOT NULL UNIQUE,
    Nombre              NVARCHAR(150)   NOT NULL,
    NombreCorto         NVARCHAR(50),
    OperadorMedico      NVARCHAR(100),
    RFC                 NVARCHAR(13),
    Activo              BIT             NOT NULL DEFAULT 1,
    FechaAlta           DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT CK_Aseguradoras_NivelMax
        CHECK (IdAseguradoraPadre IS NULL OR IdAseguradoraPadre <> Id),
    ModificadoPor       NVARCHAR(200),
    FechaModificacion   DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE cat.FormatosReceta (
    Id          INT             IDENTITY(1,1)   PRIMARY KEY,
    Clave       NVARCHAR(50)    NOT NULL UNIQUE,
    Descripcion NVARCHAR(100)   NOT NULL,
    Activo      BIT             NOT NULL DEFAULT 1,
    ModificadoPor       NVARCHAR(200),
    FechaModificacion   DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE cat.EstadosImagen (
    Id          INT             IDENTITY(1,1)   PRIMARY KEY,
    Clave       NVARCHAR(60)    NOT NULL UNIQUE,
    Descripcion NVARCHAR(150)   NOT NULL,
    Orden       INT             NOT NULL,
    EsFinal     BIT             NOT NULL DEFAULT 0,
    ModificadoPor       NVARCHAR(200),
    FechaModificacion   DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE cat.EstadosGrupo (
    Id          INT             IDENTITY(1,1)   PRIMARY KEY,
    Clave       NVARCHAR(60)    NOT NULL UNIQUE,
    Descripcion NVARCHAR(150)   NOT NULL,
    Orden       INT             NOT NULL,
    EsFinal     BIT             NOT NULL DEFAULT 0,
    ModificadoPor       NVARCHAR(200),
    FechaModificacion   DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE cat.ViasAdministracion (
    Id          INT             IDENTITY(1,1)   PRIMARY KEY,
    Clave       NVARCHAR(50)    NOT NULL UNIQUE,
    Descripcion NVARCHAR(100)   NOT NULL,
    ModificadoPor       NVARCHAR(200),
    FechaModificacion   DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE cat.Especialidades (
    Id          INT             IDENTITY(1,1)   PRIMARY KEY,
    Clave       NVARCHAR(50)    NOT NULL UNIQUE,
    Descripcion NVARCHAR(150)   NOT NULL,
    Activo      BIT             NOT NULL DEFAULT 1,
    ModificadoPor       NVARCHAR(200),
    FechaModificacion   DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE cat.Medicamentos (
    Id              INT             IDENTITY(1,1)   PRIMARY KEY,
    NombreComercial NVARCHAR(200)   NOT NULL,
    SustanciaActiva NVARCHAR(200),
    Presentacion    NVARCHAR(100),
    Concentracion   NVARCHAR(100),
    ClaveSAT        NVARCHAR(20),
    ClaveUnidadSAT  NVARCHAR(10),
    IVATasa         DECIMAL(5,2)    NOT NULL DEFAULT 0.00,
    IEPSTasa        DECIMAL(5,2)    NOT NULL DEFAULT 0.00,
    Activo          BIT             NOT NULL DEFAULT 1,
    FechaAlta       DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    ModificadoPor       NVARCHAR(200),
    FechaModificacion   DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE cat.ClavesSAT (
    Id          INT             IDENTITY(1,1)   PRIMARY KEY,
    Clave       NVARCHAR(20)    NOT NULL UNIQUE,
    Descripcion NVARCHAR(300)   NOT NULL,
    Tipo        NVARCHAR(50)    NOT NULL DEFAULT 'MEDICAMENTO',
    Activo      BIT             NOT NULL DEFAULT 1,
    ModificadoPor       NVARCHAR(200),
    FechaModificacion   DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE cat.UnidadesSAT (
    Id          INT             IDENTITY(1,1)   PRIMARY KEY,
    Clave       NVARCHAR(10)    NOT NULL UNIQUE,
    Nombre      NVARCHAR(100)   NOT NULL,
    Descripcion NVARCHAR(200),
    Activo      BIT             NOT NULL DEFAULT 1,
    ModificadoPor       NVARCHAR(200),
    FechaModificacion   DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE cat.RegimenFiscal (
    Id              INT             IDENTITY(1,1)   PRIMARY KEY,
    Clave           NVARCHAR(10)    NOT NULL UNIQUE,
    Descripcion     NVARCHAR(200)   NOT NULL,
    AplicaFisica    BIT             NOT NULL DEFAULT 0,
    AplicaMoral     BIT             NOT NULL DEFAULT 0,
    Activo          BIT             NOT NULL DEFAULT 1,
    ModificadoPor       NVARCHAR(200),
    FechaModificacion   DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE cat.UsoCFDI (
    Id              INT             IDENTITY(1,1)   PRIMARY KEY,
    Clave           NVARCHAR(10)    NOT NULL UNIQUE,
    Descripcion     NVARCHAR(200)   NOT NULL,
    AplicaFisica    BIT             NOT NULL DEFAULT 0,
    AplicaMoral     BIT             NOT NULL DEFAULT 0,
    Activo          BIT             NOT NULL DEFAULT 1,
    ModificadoPor       NVARCHAR(200),
    FechaModificacion   DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE cat.MetodosPago (
    Id          INT             IDENTITY(1,1)   PRIMARY KEY,
    Clave       NVARCHAR(5)     NOT NULL UNIQUE,
    Descripcion NVARCHAR(100)   NOT NULL,
    ModificadoPor       NVARCHAR(200),
    FechaModificacion   DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE cat.FormasPago (
    Id          INT             IDENTITY(1,1)   PRIMARY KEY,
    Clave       NVARCHAR(5)     NOT NULL UNIQUE,
    Descripcion NVARCHAR(100)   NOT NULL,
    Activo      BIT             NOT NULL DEFAULT 1,
    ModificadoPor       NVARCHAR(200),
    FechaModificacion   DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE cat.Monedas (
    Id          INT             IDENTITY(1,1)   PRIMARY KEY,
    Clave       NVARCHAR(5)     NOT NULL UNIQUE,
    Descripcion NVARCHAR(100)   NOT NULL,
    Activo      BIT             NOT NULL DEFAULT 1,
    ModificadoPor       NVARCHAR(200),
    FechaModificacion   DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE cat.TiposRelacionCFDI (
    Id          INT             IDENTITY(1,1)   PRIMARY KEY,
    Clave       NVARCHAR(5)     NOT NULL UNIQUE,
    Descripcion NVARCHAR(200)   NOT NULL,
    ModificadoPor       NVARCHAR(200),
    FechaModificacion   DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO

-- ============================================================
-- 3. SEG — SEGURIDAD (ahora sí puede referenciar cat.Aseguradoras)
-- ============================================================

CREATE TABLE seg.Roles (
    Id          INT             IDENTITY(1,1)   PRIMARY KEY,
    Clave       NVARCHAR(50)    NOT NULL UNIQUE,
    Nombre      NVARCHAR(100)   NOT NULL,
    Descripcion NVARCHAR(300),
    Activo      BIT             NOT NULL DEFAULT 1,
    FechaAlta   DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    ModificadoPor       NVARCHAR(200),
    FechaModificacion   DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE seg.Modulos (
    Id          INT             IDENTITY(1,1)   PRIMARY KEY,
    Clave       NVARCHAR(80)    NOT NULL UNIQUE,
    Nombre      NVARCHAR(150)   NOT NULL,
    Descripcion NVARCHAR(300),
    Activo      BIT             NOT NULL DEFAULT 1,
    ModificadoPor       NVARCHAR(200),
    FechaModificacion   DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE seg.PermisosRol (
    Id              INT             IDENTITY(1,1)   PRIMARY KEY,
    IdRol           INT             NOT NULL REFERENCES seg.Roles(Id),
    IdModulo        INT             NOT NULL REFERENCES seg.Modulos(Id),
    PuedeLeer       BIT             NOT NULL DEFAULT 0,
    PuedeEscribir   BIT             NOT NULL DEFAULT 0,
    PuedeEliminar   BIT             NOT NULL DEFAULT 0,
    CONSTRAINT UQ_PermisoRolModulo UNIQUE (IdRol, IdModulo),
    ModificadoPor       NVARCHAR(200),
    FechaModificacion   DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO

-- seg.Usuarios ahora puede referenciar cat.Aseguradoras sin problema
CREATE TABLE seg.Usuarios (
    Id                      UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    Username                NVARCHAR(100)       NOT NULL UNIQUE,
    Email                   NVARCHAR(200)       NOT NULL UNIQUE,
    PasswordHash            NVARCHAR(500)       NOT NULL,
    NombreCompleto          NVARCHAR(200)       NOT NULL,
    Telefono                NVARCHAR(30),
    IdRol                   INT                 NOT NULL REFERENCES seg.Roles(Id),
    IdAseguradoraAsignada   INT                 REFERENCES cat.Aseguradoras(Id),
    Activo                  BIT                 NOT NULL DEFAULT 1,
    RequiereCambioPassword  BIT                 NOT NULL DEFAULT 0,
    PasswordExpiraEn        DATETIME2,
    IntentosFallidos        INT                 NOT NULL DEFAULT 0,
    BloqueadoHasta          DATETIME2,
    RefreshToken            NVARCHAR(500),
    RefreshTokenExpira      DATETIME2,
    FechaAlta               DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    FechaActualizacion      DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    UltimoAcceso            DATETIME2,
    CreadoPor               NVARCHAR(100),
    ModificadoPor           NVARCHAR(200),
    FechaModificacion       DATETIME2           NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE seg.PermisosUsuario (
    Id              INT             IDENTITY(1,1)   PRIMARY KEY,
    IdUsuario       UNIQUEIDENTIFIER NOT NULL REFERENCES seg.Usuarios(Id),
    IdModulo        INT             NOT NULL REFERENCES seg.Modulos(Id),
    PuedeLeer       BIT             NOT NULL DEFAULT 0,
    PuedeEscribir   BIT             NOT NULL DEFAULT 0,
    PuedeEliminar   BIT             NOT NULL DEFAULT 0,
    Denegado        BIT             NOT NULL DEFAULT 0,
    Motivo          NVARCHAR(200),
    FechaAlta       DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_PermisoUsuarioModulo UNIQUE (IdUsuario, IdModulo),
    ModificadoPor       NVARCHAR(200),
    FechaModificacion   DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE seg.Sesiones (
    Id                      UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    IdUsuario               UNIQUEIDENTIFIER    NOT NULL REFERENCES seg.Usuarios(Id),
    JwtTokenId              NVARCHAR(100)       NOT NULL UNIQUE,
    RefreshToken            NVARCHAR(500)       NOT NULL UNIQUE,
    Dispositivo             NVARCHAR(200),
    TipoDispositivo         NVARCHAR(50),
    SistemaOperativo        NVARCHAR(100),
    VersionApp              NVARCHAR(20),
    IpOrigen                NVARCHAR(50),
    UserAgent               NVARCHAR(500),
    FechaInicio             DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    FechaExpiracion         DATETIME2           NOT NULL,
    FechaUltimaActividad    DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    Estado                  NVARCHAR(20)        NOT NULL DEFAULT 'ACTIVA',
    MotivoRevocacion        NVARCHAR(200)
);
GO

CREATE TABLE seg.LogAcceso (
    Id          BIGINT          IDENTITY(1,1)   PRIMARY KEY,
    IdUsuario   UNIQUEIDENTIFIER REFERENCES seg.Usuarios(Id),
    Evento      NVARCHAR(50)    NOT NULL,
    Detalle     NVARCHAR(500),
    IpOrigen    NVARCHAR(50),
    UserAgent   NVARCHAR(500),
    Dispositivo NVARCHAR(200),
    FechaEvento DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO

-- ============================================================
-- 4. CFG — CONFIGURACIÓN
-- ============================================================

CREATE TABLE cfg.Parametros (
    Id                  INT             IDENTITY(1,1)   PRIMARY KEY,
    Clave               NVARCHAR(100)   NOT NULL UNIQUE,
    Valor               NVARCHAR(1000)  NOT NULL,
    Descripcion         NVARCHAR(300),
    Tipo                NVARCHAR(20)    NOT NULL DEFAULT 'STRING',
    EsSecreto           BIT             NOT NULL DEFAULT 0,
    FechaAlta           DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    FechaActualizacion  DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    ModificadoPor       NVARCHAR(100),
    FechaModificacion   DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE cfg.ConfiguracionesOCR (
    Id                  INT             IDENTITY(1,1)   PRIMARY KEY,
    Nombre              NVARCHAR(100)   NOT NULL UNIQUE,
    Proveedor           NVARCHAR(80)    NOT NULL,
    UrlBase             NVARCHAR(500)   NOT NULL,
    ApiKeyEncriptada    NVARCHAR(1000),
    Modelo              NVARCHAR(100),
    Version             NVARCHAR(20),
    TimeoutSegundos     INT             NOT NULL DEFAULT 30,
    MaxReintentos       INT             NOT NULL DEFAULT 3,
    CostoPorImagenUSD   DECIMAL(10,6)   NOT NULL DEFAULT 0,
    EsPrincipal         BIT             NOT NULL DEFAULT 0,
    Activo              BIT             NOT NULL DEFAULT 1,
    ConfigJson          NVARCHAR(MAX),
    FechaAlta           DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    FechaActualizacion  DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    ModificadoPor       NVARCHAR(200),
    FechaModificacion   DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO

-- ============================================================
-- 5. REC — RECETAS E IMÁGENES
-- ============================================================

CREATE TABLE rec.Clientes (
    Id                  UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    NombreCompleto      NVARCHAR(200)       NOT NULL,
    ApellidoPaterno     NVARCHAR(100),
    ApellidoMaterno     NVARCHAR(100),
    Nombre              NVARCHAR(100),
    FechaNacimiento     DATE,
    RFC                 NVARCHAR(13),
    CURP                NVARCHAR(18),
    CodigoPostal        NVARCHAR(10),
    RegimenFiscalId     INT                 REFERENCES cat.RegimenFiscal(Id),
    Email               NVARCHAR(200),
    FechaAlta           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    FechaActualizacion  DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    ModificadoPor       NVARCHAR(200),
    FechaModificacion   DATETIME2           NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE rec.GruposReceta (
    Id                      UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    FolioBase               NVARCHAR(100),
    IdCliente               UNIQUEIDENTIFIER    REFERENCES rec.Clientes(Id),
    IdAseguradora           INT                 NOT NULL REFERENCES cat.Aseguradoras(Id),
    IdFormatoReceta         INT                 NOT NULL REFERENCES cat.FormatosReceta(Id),
    NUR                     NVARCHAR(100),
    Credencial              NVARCHAR(100),
    NumeroAutorizacion      NVARCHAR(100),
    NombrePaciente          NVARCHAR(200),
    ApellidoPaterno         NVARCHAR(100),
    ApellidoMaterno         NVARCHAR(100),
    NombrePac               NVARCHAR(100),
    FechaNacimientoPac      DATE,
    NominaPaciente          NVARCHAR(100),
    Elegibilidad            NVARCHAR(100),
    ClaveDH                 NVARCHAR(50),
    ClaveBeneficiario       NVARCHAR(100),
    ClaveMedicion           NVARCHAR(50),
    NombreMedico            NVARCHAR(200),
    ApellidoPaternoMedico   NVARCHAR(100),
    ApellidoMaternoMedico   NVARCHAR(100),
    NombreMedicoNombre      NVARCHAR(100),
    CedulaMedico            NVARCHAR(50),
    ClaveMedico             NVARCHAR(50),
    IdEspecialidad          INT                 REFERENCES cat.Especialidades(Id),
    EspecialidadTexto       NVARCHAR(150),
    DireccionMedico         NVARCHAR(300),
    TelefonoMedico          NVARCHAR(50),
    InstitucionMedico       NVARCHAR(200),
    CodigoCIE10             NVARCHAR(20),
    DescripcionDiagnostico  NVARCHAR(500),
    FechaConsulta           DATE,
    HoraConsulta            TIME,
    TotalImagenes           INT                 NOT NULL DEFAULT 0,
    TotalMedicamentos       INT                 NOT NULL DEFAULT 0,
    IdEstadoGrupo           INT                 NOT NULL REFERENCES cat.EstadosGrupo(Id),
    IdUsuarioAlta           UNIQUEIDENTIFIER    REFERENCES seg.Usuarios(Id),
    FechaCreacion           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    FechaActualizacion      DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    FechaCompletado         DATETIME2,
    NotasGrupo              NVARCHAR(500),
    ModificadoPor           NVARCHAR(200),
    FechaModificacion       DATETIME2           NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE rec.Imagenes (
    Id                      UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    IdGrupo                 UNIQUEIDENTIFIER    NOT NULL REFERENCES rec.GruposReceta(Id),
    NumeroHoja              INT                 NOT NULL DEFAULT 1,
    UrlBlobRaw              NVARCHAR(500)       NOT NULL,
    UrlBlobOCR              NVARCHAR(500),
    UrlBlobIlegible         NVARCHAR(500),
    NombreArchivo           NVARCHAR(200)       NOT NULL,
    TamanioBytes            BIGINT,
    FormatoImagen           NVARCHAR(10),
    AnchoPixeles            INT,
    AltoPixeles             INT,
    ResolucionDPI           INT,
    OrigenImagen            NVARCHAR(20)        NOT NULL DEFAULT 'CAMARA',
    FechaTomaFoto           DATETIME2,
    GpsLatitud              DECIMAL(9,6),
    GpsLongitud             DECIMAL(9,6),
    ModeloDispositivo       NVARCHAR(200),
    SistemaOperativo        NVARCHAR(100),
    IdUsuarioSubida         UNIQUEIDENTIFIER    NOT NULL REFERENCES seg.Usuarios(Id),
    IdSesion                UNIQUEIDENTIFIER    REFERENCES seg.Sesiones(Id),
    FechaSubida             DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    IpOrigen                NVARCHAR(50),
    ScoreLegibilidad        DECIMAL(5,2),
    EsLegible               BIT,
    MotivoBajaCalidad       NVARCHAR(200),
    FolioCompleto           NVARCHAR(100),
    FolioBase               NVARCHAR(100),
    SufijoFolio             NVARCHAR(10),
    CodigoCOU               NVARCHAR(50),
    EsCapturaManual         BIT                 NOT NULL DEFAULT 0,
    IdUsuarioCapturaManual  UNIQUEIDENTIFIER    REFERENCES seg.Usuarios(Id),
    FechaCapturaManual      DATETIME2,
    NotasCapturaManual      NVARCHAR(300),
    IdEstadoImagen          INT                 NOT NULL REFERENCES cat.EstadosImagen(Id),
    FechaActualizacion      DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    ErrorProceso            NVARCHAR(500),
    IntentosProceso         INT                 NOT NULL DEFAULT 0,
    ModificadoPor           NVARCHAR(200),
    FechaModificacion       DATETIME2           NOT NULL DEFAULT GETUTCDATE()
);
GO

-- ============================================================
-- 6. MED
-- ============================================================

CREATE TABLE med.MedicamentosReceta (
    Id                      UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    IdImagen                UNIQUEIDENTIFIER    NOT NULL REFERENCES rec.Imagenes(Id),
    IdGrupo                 UNIQUEIDENTIFIER    NOT NULL REFERENCES rec.GruposReceta(Id),
    IdMedicamentoCatalogo   INT                 REFERENCES cat.Medicamentos(Id),
    NumeroPrescripcion      INT                 NOT NULL DEFAULT 1,
    CodigoCIE10             NVARCHAR(20),
    DescripcionCIE10        NVARCHAR(300),
    NombreComercial         NVARCHAR(200),
    SustanciaActiva         NVARCHAR(200),
    Presentacion            NVARCHAR(200),
    Dosis                   NVARCHAR(100),
    CodigoEAN               NVARCHAR(50),
    CantidadTexto           NVARCHAR(100),
    CantidadNumero          INT,
    UnidadCantidad          NVARCHAR(50),
    IdViaAdministracion     INT                 REFERENCES cat.ViasAdministracion(Id),
    FrecuenciaTexto         NVARCHAR(200),
    FrecuenciaExpandida     NVARCHAR(200),
    DuracionTexto           NVARCHAR(100),
    DuracionDias            INT,
    IndicacionesCompletas   NVARCHAR(500),
    NumeroAutorizacion      NVARCHAR(100),
    FechaSurtido            DATE,
    ClaveProvFarm           NVARCHAR(50),
    ClaveSATId              INT                 REFERENCES cat.ClavesSAT(Id),
    ClaveUnidadSATId        INT                 REFERENCES cat.UnidadesSAT(Id),
    PrecioUnitario          DECIMAL(10,2),
    Importe                 DECIMAL(10,2),
    IVATasa                 DECIMAL(5,2)        NOT NULL DEFAULT 0.00,
    IEPSTasa                DECIMAL(5,2)        NOT NULL DEFAULT 0.00,
    FechaCreacion           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    FechaActualizacion      DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    ModificadoPor           NVARCHAR(200),
    FechaModificacion       DATETIME2           NOT NULL DEFAULT GETUTCDATE()
);
GO

-- ============================================================
-- 7. OCR
-- ============================================================

CREATE TABLE ocr.ColaProcesamiento (
    Id                  BIGINT          IDENTITY(1,1)   PRIMARY KEY,
    IdImagen            UNIQUEIDENTIFIER NOT NULL REFERENCES rec.Imagenes(Id),
    UrlBlobRaw          NVARCHAR(500)   NOT NULL,
    Prioridad           INT             NOT NULL DEFAULT 5,
    Intentos            INT             NOT NULL DEFAULT 0,
    MaxIntentos         INT             NOT NULL DEFAULT 3,
    FechaEncolado       DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    FechaInicioProceso  DATETIME2,
    FechaFinProceso     DATETIME2,
    WorkerProcesando    NVARCHAR(100),
    Bloqueado           BIT             NOT NULL DEFAULT 0,
    FechaBloqueo        DATETIME2,
    EstadoCola          NVARCHAR(20)    NOT NULL DEFAULT 'PENDIENTE',
    ErrorMensaje        NVARCHAR(500),
    IdConfiguracionOCR  INT             REFERENCES cfg.ConfiguracionesOCR(Id),
    ModificadoPor       NVARCHAR(200),
    FechaModificacion   DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE ocr.ResultadosOCR (
    Id                  BIGINT          IDENTITY(1,1)   PRIMARY KEY,
    IdImagen            UNIQUEIDENTIFIER NOT NULL REFERENCES rec.Imagenes(Id),
    IdConfiguracionOCR  INT             REFERENCES cfg.ConfiguracionesOCR(Id),
    ProveedorOCR        NVARCHAR(80)    NOT NULL,
    ModeloUsado         NVARCHAR(100),
    VersionAPI          NVARCHAR(20),
    UrlEndpointLlamado  NVARCHAR(500),
    RequestIdExterno    NVARCHAR(200),
    FechaPeticion       DATETIME2       NOT NULL,
    FechaRespuesta      DATETIME2,
    DuracionMs          INT,
    TextoCompleto       NVARCHAR(MAX),
    ConfianzaPromedio   DECIMAL(5,2),
    IdiomaDetectado     NVARCHAR(10)    DEFAULT 'spa',
    PaginasProcesadas   INT             NOT NULL DEFAULT 1,
    ResponseJsonCompleto NVARCHAR(MAX),
    CostoEstimadoUSD    DECIMAL(10,6)   NOT NULL DEFAULT 0,
    FechaProceso        DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    Exitoso             BIT             NOT NULL DEFAULT 0,
    CodigoErrorHTTP     INT,
    MensajeError        NVARCHAR(500),
    ModificadoPor       NVARCHAR(200),
    FechaModificacion   DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE ocr.ResultadosExtraccion (
    Id                      BIGINT          IDENTITY(1,1)   PRIMARY KEY,
    IdImagen                UNIQUEIDENTIFIER NOT NULL REFERENCES rec.Imagenes(Id),
    IdResultadoOCR          BIGINT          REFERENCES ocr.ResultadosOCR(Id),
    IdConfiguracionOCR      INT             REFERENCES cfg.ConfiguracionesOCR(Id),
    Motor                   NVARCHAR(50)    NOT NULL DEFAULT 'API_EXTERNA_OCR',
    JSONEstructurado        NVARCHAR(MAX),
    ConfianzaExtraccion     DECIMAL(5,2),
    CamposFaltantes         NVARCHAR(500),
    AseguradoraDetectada    NVARCHAR(100),
    FormatoDetectado        NVARCHAR(50),
    TokensEntrada           INT,
    TokensSalida            INT,
    CostoEstimadoUSD        DECIMAL(10,6),
    PromptVersion           NVARCHAR(20),
    FechaProceso            DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    Exitoso                 BIT             NOT NULL DEFAULT 0,
    MensajeError            NVARCHAR(300),
    ModificadoPor           NVARCHAR(200),
    FechaModificacion       DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO

-- ============================================================
-- 8. REV
-- ============================================================

CREATE TABLE rev.AsignacionesRevision (
    Id                  UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    IdImagen            UNIQUEIDENTIFIER    NOT NULL REFERENCES rec.Imagenes(Id),
    IdGrupo             UNIQUEIDENTIFIER    NOT NULL REFERENCES rec.GruposReceta(Id),
    IdUsuarioAsignado   UNIQUEIDENTIFIER    NOT NULL REFERENCES seg.Usuarios(Id),
    TipoRevision        NVARCHAR(50)        NOT NULL,
    FechaAsignacion     DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    FechaLimite         DATETIME2,
    FechaInicio         DATETIME2,
    FechaTermino        DATETIME2,
    Estado              NVARCHAR(30)        NOT NULL DEFAULT 'PENDIENTE',
    IdUsuarioAsignoPor  UNIQUEIDENTIFIER    REFERENCES seg.Usuarios(Id),
    Notas               NVARCHAR(300),
    ModificadoPor       NVARCHAR(200),
    FechaModificacion   DATETIME2           NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE rev.RevisionesHumanas (
    Id                  UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    IdImagen            UNIQUEIDENTIFIER    NOT NULL REFERENCES rec.Imagenes(Id),
    IdGrupo             UNIQUEIDENTIFIER    NOT NULL REFERENCES rec.GruposReceta(Id),
    IdAsignacion        UNIQUEIDENTIFIER    REFERENCES rev.AsignacionesRevision(Id),
    TipoRevision        NVARCHAR(50)        NOT NULL,
    Resultado           NVARCHAR(20)        NOT NULL,
    MotivoRechazo       NVARCHAR(300),
    IdUsuarioRevisor    UNIQUEIDENTIFIER    NOT NULL REFERENCES seg.Usuarios(Id),
    FechaRevision       DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    DuracionMinutos     INT,
    Observaciones       NVARCHAR(500),
    ModificadoPor       NVARCHAR(200),
    FechaModificacion   DATETIME2           NOT NULL DEFAULT GETUTCDATE()
);
GO

-- ============================================================
-- 9. FAC
-- ============================================================

CREATE TABLE fac.Emisores (
    Id                  INT             IDENTITY(1,1)   PRIMARY KEY,
    IdAseguradora       INT             NOT NULL REFERENCES cat.Aseguradoras(Id),
    RFC                 NVARCHAR(13)    NOT NULL,
    RazonSocial         NVARCHAR(300)   NOT NULL,
    RegimenFiscalId     INT             NOT NULL REFERENCES cat.RegimenFiscal(Id),
    CodigoPostal        NVARCHAR(10)    NOT NULL,
    NoCertificado       NVARCHAR(30),
    RutaCertificado     NVARCHAR(500),
    Activo              BIT             NOT NULL DEFAULT 1,
    FechaAlta           DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    ModificadoPor       NVARCHAR(200),
    FechaModificacion   DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE fac.Receptores (
    Id                  UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    IdCliente           UNIQUEIDENTIFIER    REFERENCES rec.Clientes(Id),
    RFC                 NVARCHAR(13)        NOT NULL,
    NombreRazonSocial   NVARCHAR(300)       NOT NULL,
    RegimenFiscalId     INT                 NOT NULL REFERENCES cat.RegimenFiscal(Id),
    CodigoPostal        NVARCHAR(10)        NOT NULL,
    Email               NVARCHAR(200),
    Activo              BIT                 NOT NULL DEFAULT 1,
    FechaAlta           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    ModificadoPor       NVARCHAR(200),
    FechaModificacion   DATETIME2           NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE fac.SolicitudesAutorizacion (
    Id                  UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    IdGrupo             UNIQUEIDENTIFIER    NOT NULL REFERENCES rec.GruposReceta(Id),
    NumeroAutorizacion  NVARCHAR(100),
    FechaSolicitud      DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    FechaRespuesta      DATETIME2,
    Estado              NVARCHAR(20)        NOT NULL DEFAULT 'PENDIENTE',
    Observaciones       NVARCHAR(300),
    IdUsuarioSolicita   UNIQUEIDENTIFIER    REFERENCES seg.Usuarios(Id),
    ModificadoPor       NVARCHAR(200),
    FechaModificacion   DATETIME2           NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE fac.PreFacturas (
    Id                      UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    IdGrupo                 UNIQUEIDENTIFIER    NOT NULL REFERENCES rec.GruposReceta(Id),
    IdEmisor                INT                 NOT NULL REFERENCES fac.Emisores(Id),
    IdReceptor              UNIQUEIDENTIFIER    NOT NULL REFERENCES fac.Receptores(Id),
    TipoComprobante         NVARCHAR(1)         NOT NULL DEFAULT 'I',
    Version                 NVARCHAR(5)         NOT NULL DEFAULT '4.0',
    MetodoPagoId            INT                 NOT NULL REFERENCES cat.MetodosPago(Id),
    FormaPagoId             INT                 NOT NULL REFERENCES cat.FormasPago(Id),
    MonedaId                INT                 NOT NULL REFERENCES cat.Monedas(Id),
    UsoCFDIId               INT                 NOT NULL REFERENCES cat.UsoCFDI(Id),
    TipoCambio              DECIMAL(10,4)       NOT NULL DEFAULT 1.0000,
    Exportacion             NVARCHAR(3)         NOT NULL DEFAULT '01',
    Subtotal                DECIMAL(12,2)       NOT NULL DEFAULT 0,
    Descuento               DECIMAL(12,2)       NOT NULL DEFAULT 0,
    TotalIVA                DECIMAL(12,2)       NOT NULL DEFAULT 0,
    TotalIEPS               DECIMAL(12,2)       NOT NULL DEFAULT 0,
    Total                   DECIMAL(12,2)       NOT NULL DEFAULT 0,
    Estado                  NVARCHAR(30)        NOT NULL DEFAULT 'GENERADA',
    FechaGeneracion         DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    FechaAprobacion         DATETIME2,
    IdUsuarioAprobacion     UNIQUEIDENTIFIER    REFERENCES seg.Usuarios(Id),
    IntentosTimbrado        INT                 NOT NULL DEFAULT 0,
    UltimoErrorTimbrado     NVARCHAR(500),
    ObservacionesFiscales   NVARCHAR(500),
    ModificadoPor           NVARCHAR(200),
    FechaModificacion       DATETIME2           NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE fac.PartidasPreFactura (
    Id                  UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    IdPreFactura        UNIQUEIDENTIFIER    NOT NULL REFERENCES fac.PreFacturas(Id),
    IdMedicamentoReceta UNIQUEIDENTIFIER    REFERENCES med.MedicamentosReceta(Id),
    NumeroLinea         INT                 NOT NULL,
    ClaveProdServ       NVARCHAR(20)        NOT NULL DEFAULT '51101500',
    ClaveUnidad         NVARCHAR(10)        NOT NULL DEFAULT 'H87',
    NoIdentificacion    NVARCHAR(100),
    Descripcion         NVARCHAR(500)       NOT NULL,
    Cantidad            DECIMAL(10,4)       NOT NULL,
    ValorUnitario       DECIMAL(12,4)       NOT NULL,
    Descuento           DECIMAL(12,2)       NOT NULL DEFAULT 0,
    Importe             DECIMAL(12,2)       NOT NULL,
    ObjetoImpuesto      NVARCHAR(3)         NOT NULL DEFAULT '02',
    IVATasa             DECIMAL(5,4)        NOT NULL DEFAULT 0.0000,
    IVAImporte          DECIMAL(12,2)       NOT NULL DEFAULT 0,
    IEPSTasa            DECIMAL(5,4)        NOT NULL DEFAULT 0.0000,
    IEPSImporte         DECIMAL(12,2)       NOT NULL DEFAULT 0,
    ModificadoPor       NVARCHAR(200),
    FechaModificacion   DATETIME2           NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE fac.CFDI (
    Id                      UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    IdPreFactura            UNIQUEIDENTIFIER    NOT NULL REFERENCES fac.PreFacturas(Id),
    IdGrupo                 UNIQUEIDENTIFIER    NOT NULL REFERENCES rec.GruposReceta(Id),
    UUID                    NVARCHAR(36)        NOT NULL UNIQUE,
    FechaTimbrado           DATETIME2           NOT NULL,
    Version                 NVARCHAR(5)         NOT NULL DEFAULT '4.0',
    RFCEmisor               NVARCHAR(13)        NOT NULL,
    NombreEmisor            NVARCHAR(300),
    RFCReceptor             NVARCHAR(13)        NOT NULL,
    NombreReceptor          NVARCHAR(300),
    Total                   DECIMAL(12,2)       NOT NULL,
    SelloCFDI               NVARCHAR(MAX),
    SelloSAT                NVARCHAR(MAX),
    CadenaOriginalSAT       NVARCHAR(MAX),
    NoCertificadoSAT        NVARCHAR(30),
    NoCertificadoEmisor     NVARCHAR(30),
    UrlBlobXML              NVARCHAR(500)       NOT NULL,
    UrlBlobPDF              NVARCHAR(500),
    NombrePAC               NVARCHAR(100),
    RespuestaJsonPAC        NVARCHAR(MAX),
    Estado                  NVARCHAR(30)        NOT NULL DEFAULT 'VIGENTE',
    FechaCancelacion        DATETIME2,
    MotivoCancelacion       NVARCHAR(200),
    UUIDSustitucion         NVARCHAR(36),
    FechaCreacion           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    ModificadoPor           NVARCHAR(200),
    FechaModificacion       DATETIME2           NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE fac.PartidasCFDI (
    Id                  UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    IdCFDI              UNIQUEIDENTIFIER    NOT NULL REFERENCES fac.CFDI(Id),
    NumeroLinea         INT                 NOT NULL,
    ClaveProdServ       NVARCHAR(20)        NOT NULL,
    ClaveUnidad         NVARCHAR(10)        NOT NULL,
    NoIdentificacion    NVARCHAR(100),
    Descripcion         NVARCHAR(500)       NOT NULL,
    Cantidad            DECIMAL(10,4)       NOT NULL,
    ValorUnitario       DECIMAL(12,4)       NOT NULL,
    Descuento           DECIMAL(12,2)       NOT NULL DEFAULT 0,
    Importe             DECIMAL(12,2)       NOT NULL,
    ObjetoImpuesto      NVARCHAR(3)         NOT NULL DEFAULT '02',
    IVATasa             DECIMAL(5,4)        NOT NULL DEFAULT 0.0000,
    IVAImporte          DECIMAL(12,2)       NOT NULL DEFAULT 0,
    IEPSTasa            DECIMAL(5,4)        NOT NULL DEFAULT 0.0000,
    IEPSImporte         DECIMAL(12,2)       NOT NULL DEFAULT 0,
    ModificadoPor       NVARCHAR(200),
    FechaModificacion   DATETIME2           NOT NULL DEFAULT GETUTCDATE()
);
GO

-- ============================================================
-- 10. AUD
-- ============================================================

CREATE TABLE aud.HistorialEstadosImagen (
    Id              BIGINT          IDENTITY(1,1)   PRIMARY KEY,
    IdImagen        UNIQUEIDENTIFIER NOT NULL REFERENCES rec.Imagenes(Id),
    EstadoAnterior  INT             REFERENCES cat.EstadosImagen(Id),
    EstadoNuevo     INT             NOT NULL REFERENCES cat.EstadosImagen(Id),
    IdUsuario       UNIQUEIDENTIFIER REFERENCES seg.Usuarios(Id),
    Motivo          NVARCHAR(300),
    FechaCambio     DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE aud.HistorialEstadosGrupo (
    Id              BIGINT          IDENTITY(1,1)   PRIMARY KEY,
    IdGrupo         UNIQUEIDENTIFIER NOT NULL REFERENCES rec.GruposReceta(Id),
    EstadoAnterior  INT             REFERENCES cat.EstadosGrupo(Id),
    EstadoNuevo     INT             NOT NULL REFERENCES cat.EstadosGrupo(Id),
    IdUsuario       UNIQUEIDENTIFIER REFERENCES seg.Usuarios(Id),
    Motivo          NVARCHAR(300),
    FechaCambio     DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE aud.HistorialCorrecciones (
    Id              BIGINT          IDENTITY(1,1)   PRIMARY KEY,
    IdImagen        UNIQUEIDENTIFIER REFERENCES rec.Imagenes(Id),
    IdGrupo         UNIQUEIDENTIFIER REFERENCES rec.GruposReceta(Id),
    IdMedicamento   UNIQUEIDENTIFIER REFERENCES med.MedicamentosReceta(Id),
    Tabla           NVARCHAR(100)   NOT NULL,
    Campo           NVARCHAR(100)   NOT NULL,
    ValorAnterior   NVARCHAR(500),
    ValorNuevo      NVARCHAR(500),
    TipoCorreccion  NVARCHAR(50)    NOT NULL,
    IdUsuario       UNIQUEIDENTIFIER REFERENCES seg.Usuarios(Id),
    FechaCorreccion DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE aud.LogProcesamiento (
    Id          BIGINT          IDENTITY(1,1)   PRIMARY KEY,
    IdImagen    UNIQUEIDENTIFIER,
    IdGrupo     UNIQUEIDENTIFIER,
    Paso        NVARCHAR(80)    NOT NULL,
    Nivel       NVARCHAR(10)    NOT NULL DEFAULT 'INFO',
    Mensaje     NVARCHAR(500),
    Detalle     NVARCHAR(MAX),
    DuracionMs  INT,
    Servidor    NVARCHAR(100),
    FechaEvento DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO

-- ============================================================
-- 11. ÍNDICES
-- ============================================================

-- seg
CREATE INDEX IX_Usuarios_Email       ON seg.Usuarios (Email);
CREATE INDEX IX_Usuarios_Username    ON seg.Usuarios (Username);
CREATE INDEX IX_Usuarios_Rol         ON seg.Usuarios (IdRol);
CREATE INDEX IX_Sesiones_Usuario     ON seg.Sesiones (IdUsuario, Estado);
CREATE INDEX IX_Sesiones_JwtId       ON seg.Sesiones (JwtTokenId);
CREATE INDEX IX_LogAcceso_Usuario    ON seg.LogAcceso (IdUsuario) WHERE IdUsuario IS NOT NULL;
CREATE INDEX IX_LogAcceso_Evento     ON seg.LogAcceso (Evento, FechaEvento);

-- rec.GruposReceta
CREATE INDEX IX_Grupos_FolioBase     ON rec.GruposReceta (FolioBase)                        WHERE FolioBase IS NOT NULL;
CREATE INDEX IX_Grupos_Aseguradora   ON rec.GruposReceta (IdAseguradora);
CREATE INDEX IX_Grupos_FechaConsulta ON rec.GruposReceta (FechaConsulta);
CREATE INDEX IX_Grupos_Estado        ON rec.GruposReceta (IdEstadoGrupo);
CREATE INDEX IX_Grupos_Cliente       ON rec.GruposReceta (IdCliente)                         WHERE IdCliente IS NOT NULL;
CREATE INDEX IX_Grupos_NUR           ON rec.GruposReceta (NUR)                               WHERE NUR IS NOT NULL;
CREATE INDEX IX_Grupos_SinFolio      ON rec.GruposReceta (IdCliente, IdAseguradora, FechaConsulta);

-- rec.Imagenes
CREATE INDEX IX_Img_Grupo            ON rec.Imagenes (IdGrupo);
CREATE INDEX IX_Img_Estado           ON rec.Imagenes (IdEstadoImagen);
CREATE INDEX IX_Img_FolioBase        ON rec.Imagenes (FolioBase)                             WHERE FolioBase IS NOT NULL;
CREATE INDEX IX_Img_FechaSubida      ON rec.Imagenes (FechaSubida);
CREATE INDEX IX_Img_Origen           ON rec.Imagenes (OrigenImagen);
CREATE INDEX IX_Img_CapturaManual    ON rec.Imagenes (EsCapturaManual)                       WHERE EsCapturaManual = 1;
CREATE INDEX IX_Img_Usuario          ON rec.Imagenes (IdUsuarioSubida);

-- med
CREATE INDEX IX_Med_Imagen           ON med.MedicamentosReceta (IdImagen);
CREATE INDEX IX_Med_Grupo            ON med.MedicamentosReceta (IdGrupo);
CREATE INDEX IX_Med_NombreComercial  ON med.MedicamentosReceta (NombreComercial);
CREATE INDEX IX_Med_SustanciaActiva  ON med.MedicamentosReceta (SustanciaActiva)             WHERE SustanciaActiva IS NOT NULL;
CREATE INDEX IX_Med_CIE10            ON med.MedicamentosReceta (CodigoCIE10)                 WHERE CodigoCIE10 IS NOT NULL;
CREATE INDEX IX_Med_Catalogo         ON med.MedicamentosReceta (IdMedicamentoCatalogo)       WHERE IdMedicamentoCatalogo IS NOT NULL;

-- ocr (CRÍTICO para el Worker)
CREATE INDEX IX_Cola_Polling         ON ocr.ColaProcesamiento (EstadoCola, Bloqueado, Prioridad, FechaEncolado);
CREATE INDEX IX_Cola_Imagen          ON ocr.ColaProcesamiento (IdImagen);
CREATE INDEX IX_Cola_Worker          ON ocr.ColaProcesamiento (WorkerProcesando)             WHERE WorkerProcesando IS NOT NULL;
CREATE INDEX IX_OCR_Imagen           ON ocr.ResultadosOCR (IdImagen);
CREATE INDEX IX_OCR_Proveedor        ON ocr.ResultadosOCR (ProveedorOCR);
CREATE INDEX IX_Ext_Imagen           ON ocr.ResultadosExtraccion (IdImagen);

-- rev
CREATE INDEX IX_Rev_Imagen           ON rev.AsignacionesRevision (IdImagen);
CREATE INDEX IX_Rev_Usuario          ON rev.AsignacionesRevision (IdUsuarioAsignado, Estado);
CREATE INDEX IX_Rev_Estado           ON rev.AsignacionesRevision (Estado);

-- fac
CREATE INDEX IX_PreFac_Grupo         ON fac.PreFacturas (IdGrupo);
CREATE INDEX IX_PreFac_Estado        ON fac.PreFacturas (Estado);
CREATE INDEX IX_CFDI_UUID            ON fac.CFDI (UUID);
CREATE INDEX IX_CFDI_Grupo           ON fac.CFDI (IdGrupo);
CREATE INDEX IX_CFDI_RFCReceptor     ON fac.CFDI (RFCReceptor);
CREATE INDEX IX_CFDI_Estado          ON fac.CFDI (Estado);

-- aud
CREATE INDEX IX_AudImg_Imagen        ON aud.HistorialEstadosImagen (IdImagen);
CREATE INDEX IX_AudGrp_Grupo         ON aud.HistorialEstadosGrupo (IdGrupo);
CREATE INDEX IX_AudCorr_Imagen       ON aud.HistorialCorrecciones (IdImagen)                 WHERE IdImagen IS NOT NULL;
CREATE INDEX IX_AudCorr_Grupo        ON aud.HistorialCorrecciones (IdGrupo)                  WHERE IdGrupo  IS NOT NULL;
CREATE INDEX IX_Log_Fecha            ON aud.LogProcesamiento (FechaEvento);
CREATE INDEX IX_Log_Nivel            ON aud.LogProcesamiento (Nivel, FechaEvento);
CREATE INDEX IX_Log_Imagen           ON aud.LogProcesamiento (IdImagen)                      WHERE IdImagen IS NOT NULL;
GO

-- ============================================================
-- 12. DATOS INICIALES
-- ============================================================

-- ROLES
INSERT INTO seg.Roles (Clave, Nombre, Descripcion) VALUES
('SUPERADMIN',    'Super Administrador', 'Acceso total a la plataforma.'),
('ADMIN',         'Administrador',       'Gestiona usuarios, catálogos y aseguradoras.'),
('OPERADOR_OCR',  'Operador OCR',        'Sube imágenes desde cámara o galería.'),
('REVISOR',       'Revisor',             'Revisa y aprueba imágenes procesadas.'),
('FACTURISTA',    'Facturista',          'Genera pre-facturas y gestiona el timbrado CFDI 4.0.'),
('AUDITOR',       'Auditor',             'Acceso de solo lectura a todos los módulos.');
GO

-- MÓDULOS
INSERT INTO seg.Modulos (Clave, Nombre) VALUES
('IMAGENES.SUBIR',          'Subir imágenes de recetas'),
('IMAGENES.VER',            'Ver imágenes y su estado'),
('IMAGENES.ELIMINAR',       'Eliminar imágenes (solo borradores)'),
('GRUPOS.VER',              'Ver grupos de recetas'),
('GRUPOS.EDITAR',           'Editar datos de grupos'),
('REVISION.VER',            'Ver cola de revisión'),
('REVISION.APROBAR',        'Aprobar o rechazar imágenes'),
('REVISION.CAPTURA_MANUAL', 'Hacer captura manual de imágenes ilegibles'),
('FACTURACION.VER',         'Ver facturas y prefacturas'),
('FACTURACION.GENERAR',     'Generar prefacturas'),
('FACTURACION.TIMBRAR',     'Enviar a PAC y timbrar CFDI'),
('FACTURACION.CANCELAR',    'Cancelar CFDIs'),
('CATALOGOS.VER',           'Ver catálogos del sistema'),
('CATALOGOS.EDITAR',        'Editar catálogos'),
('USUARIOS.VER',            'Ver usuarios de la plataforma'),
('USUARIOS.ADMINISTRAR',    'Crear, editar, desactivar usuarios'),
('CONFIG.VER',              'Ver configuración del sistema'),
('CONFIG.EDITAR',           'Editar parámetros y configuración API OCR'),
('AUDITORIA.VER',           'Ver logs y auditoría del sistema'),
('REPORTES.VER',            'Ver reportes y dashboards'),
('OCR.CONFIGURAR',          'Configurar proveedores de API OCR');
GO

-- PERMISOS POR ROL
INSERT INTO seg.PermisosRol (IdRol, IdModulo, PuedeLeer, PuedeEscribir, PuedeEliminar)
SELECT r.Id, m.Id, 1, 1, 1
FROM seg.Roles r CROSS JOIN seg.Modulos m
WHERE r.Clave = 'SUPERADMIN';

INSERT INTO seg.PermisosRol (IdRol, IdModulo, PuedeLeer, PuedeEscribir, PuedeEliminar)
SELECT r.Id, m.Id, 1,
    CASE WHEN m.Clave IN ('FACTURACION.TIMBRAR','FACTURACION.CANCELAR') THEN 0 ELSE 1 END,
    CASE WHEN m.Clave IN ('FACTURACION.TIMBRAR','FACTURACION.CANCELAR') THEN 0 ELSE 1 END
FROM seg.Roles r CROSS JOIN seg.Modulos m
WHERE r.Clave = 'ADMIN';

INSERT INTO seg.PermisosRol (IdRol, IdModulo, PuedeLeer, PuedeEscribir, PuedeEliminar)
SELECT r.Id, m.Id,
    CASE WHEN m.Clave IN ('IMAGENES.VER','GRUPOS.VER','CATALOGOS.VER','REPORTES.VER') THEN 1 ELSE 0 END,
    CASE WHEN m.Clave = 'IMAGENES.SUBIR' THEN 1 ELSE 0 END, 0
FROM seg.Roles r CROSS JOIN seg.Modulos m
WHERE r.Clave = 'OPERADOR_OCR';

INSERT INTO seg.PermisosRol (IdRol, IdModulo, PuedeLeer, PuedeEscribir, PuedeEliminar)
SELECT r.Id, m.Id,
    CASE WHEN m.Clave IN ('IMAGENES.VER','GRUPOS.VER','REVISION.VER','CATALOGOS.VER','REPORTES.VER') THEN 1 ELSE 0 END,
    CASE WHEN m.Clave IN ('REVISION.APROBAR','REVISION.CAPTURA_MANUAL','GRUPOS.EDITAR') THEN 1 ELSE 0 END, 0
FROM seg.Roles r CROSS JOIN seg.Modulos m
WHERE r.Clave = 'REVISOR';

INSERT INTO seg.PermisosRol (IdRol, IdModulo, PuedeLeer, PuedeEscribir, PuedeEliminar)
SELECT r.Id, m.Id,
    CASE WHEN m.Clave IN ('IMAGENES.VER','GRUPOS.VER','REVISION.VER','FACTURACION.VER','CATALOGOS.VER','REPORTES.VER') THEN 1 ELSE 0 END,
    CASE WHEN m.Clave IN ('FACTURACION.GENERAR','FACTURACION.TIMBRAR','FACTURACION.CANCELAR') THEN 1 ELSE 0 END, 0
FROM seg.Roles r CROSS JOIN seg.Modulos m
WHERE r.Clave = 'FACTURISTA';

INSERT INTO seg.PermisosRol (IdRol, IdModulo, PuedeLeer, PuedeEscribir, PuedeEliminar)
SELECT r.Id, m.Id, 1, 0, 0
FROM seg.Roles r CROSS JOIN seg.Modulos m
WHERE r.Clave = 'AUDITOR';
GO

-- USUARIO SUPERADMIN
-- ⚠️ CAMBIAR PasswordHash antes del primer despliegue
-- Generar con: BCrypt.Net.BCrypt.HashPassword("TuPassword", 12)
INSERT INTO seg.Usuarios (Username, Email, PasswordHash, NombreCompleto, IdRol, RequiereCambioPassword)
SELECT 'superadmin', 'admin@recetasocr.com',
    '$2a$12$PLACEHOLDER_HASH_CAMBIAR_EN_PRODUCCION',
    'Administrador del Sistema', Id, 1
FROM seg.Roles WHERE Clave = 'SUPERADMIN';
GO

-- PARÁMETROS
INSERT INTO cfg.Parametros (Clave, Valor, Descripcion, Tipo, EsSecreto) VALUES
('JWT_SECRET_KEY',             'CAMBIAR_EN_PRODUCCION_MIN_32_CHARS', 'Llave secreta JWT',                         'STRING', 1),
('JWT_EXPIRACION_MINUTOS',     '60',                                 'Minutos de vida del JWT',                   'NUMBER', 0),
('REFRESH_TOKEN_DIAS',         '30',                                 'Días de vida del refresh token',            'NUMBER', 0),
('MAX_INTENTOS_LOGIN',         '5',                                  'Intentos fallidos antes de bloqueo',        'NUMBER', 0),
('BLOQUEO_MINUTOS',            '30',                                 'Minutos de bloqueo tras fallos',            'NUMBER', 0),
('BLOB_CONTAINER_RAW',         'recetas-raw',                        'Contenedor blob imágenes crudas',           'STRING', 0),
('BLOB_CONTAINER_OCR',         'recetas-ocr',                        'Contenedor blob imágenes legibles',         'STRING', 0),
('BLOB_CONTAINER_ILEGIBLE',    'recetas-ilegibles',                  'Contenedor blob imágenes ilegibles',        'STRING', 0),
('BLOB_CONTAINER_CFDI_XML',    'cfdi-xml',                           'Contenedor blob XML CFDIs',                 'STRING', 0),
('BLOB_CONTAINER_CFDI_PDF',    'cfdi-pdf',                           'Contenedor blob PDF CFDIs',                 'STRING', 0),
('OCR_CONFIANZA_MINIMA',       '70',                                 'Score mínimo OCR para aprobado',            'NUMBER', 0),
('OCR_WORKER_POLLING_SEG',     '3',                                  'Segundos entre polling del worker',         'NUMBER', 0),
('OCR_BLOQUEO_TIMEOUT_MIN',    '30',                                 'Minutos para liberar bloqueos huérfanos',   'NUMBER', 0),
('MAX_TAMANIO_IMAGEN_MB',      '15',                                 'Tamaño máximo de imagen en MB',             'NUMBER', 0),
('FORMATOS_IMAGEN_PERMITIDOS', 'jpg,jpeg,png,heic,heif,pdf,webp',    'Extensiones permitidas',                    'STRING', 0),
('REVISION_TIMEOUT_HORAS',     '24',                                 'Horas para timeout de revisión pendiente',  'NUMBER', 0);
GO

-- CONFIGURACIÓN OCR
INSERT INTO cfg.ConfiguracionesOCR (Nombre, Proveedor, UrlBase, Modelo, TimeoutSegundos, CostoPorImagenUSD, EsPrincipal, Activo, ConfigJson) VALUES
('Google Vision API',          'GOOGLE_VISION',          'https://vision.googleapis.com/v1/images:annotate',                  'document_text_detection', 30, 0.001500, 0, 0, '{"features":["DOCUMENT_TEXT_DETECTION"],"language_hints":["es"]}'),
('Azure Document Intelligence','AZURE_DOC_INTELLIGENCE', 'https://ENDPOINT.cognitiveservices.azure.com/formrecognizer',        'prebuilt-document',        45, 0.010000, 0, 0, '{"api_version":"2023-07-31","locale":"es-MX"}'),
('AWS Textract',               'AWS_TEXTRACT',           'https://textract.us-east-1.amazonaws.com',                          'DetectDocumentText',       30, 0.001500, 0, 0, '{"FeatureTypes":["TABLES","FORMS"]}'),
('API OCR Personalizada',      'CUSTOM_API',             'https://TU_API_OCR/v1/process',                                     'medical-receipts-mx',      60, 0.000500, 1, 1, '{"language":"es","format":"medical","country":"MX"}');
GO

-- ASEGURADORAS PADRE
INSERT INTO cat.Aseguradoras (IdAseguradoraPadre, Clave, Nombre, NombreCorto, OperadorMedico) VALUES
(NULL, 'BANXICO',    'Banco de México',                      'Banxico',    'Propio'),
(NULL, 'BANCOMEXT',  'Banco Nacional de Comercio Exterior',  'Bancomext',  'MediProses'),
(NULL, 'LYFC',       'Jubilados de Luz y Fuerza del Centro', 'LYFC',       'MediProses'),
(NULL, 'BANOBRAS',   'Banco Nacional de Obras y Servicios',  'Banobras',   'MediProses'),
(NULL, 'BANORTE',    'Grupo Financiero Banorte',             'Banorte',    'Propio'),
(NULL, 'BBVA',       'BBVA México',                          'BBVA',       'Vita/Bupa'),
(NULL, 'BANAMEX',    'Banco Nacional de México',             'Banamex',    'Vita/Bupa'),
(NULL, 'NAFIN',      'Nacional Financiera',                  'Nafin',      'MediProses'),
(NULL, 'PARTICULAR', 'Consultorio Particular',               'Particular', 'Propio'),
(NULL, 'OTRO',       'Otra Aseguradora',                     'Otro',       NULL);
GO

-- ASEGURADORAS HIJO
INSERT INTO cat.Aseguradoras (IdAseguradoraPadre, Clave, Nombre, NombreCorto, OperadorMedico)
SELECT Id, 'BBVA_BUPA', 'BBVA México — Red Bupa', 'BBVA Bupa', 'Bupa' FROM cat.Aseguradoras WHERE Clave = 'BBVA';

INSERT INTO cat.Aseguradoras (IdAseguradoraPadre, Clave, Nombre, NombreCorto, OperadorMedico)
SELECT Id, 'BBVA_VITA', 'BBVA México — Red Vita', 'BBVA Vita', 'Vita' FROM cat.Aseguradoras WHERE Clave = 'BBVA';

INSERT INTO cat.Aseguradoras (IdAseguradoraPadre, Clave, Nombre, NombreCorto, OperadorMedico)
SELECT Id, 'BANAMEX_BUPA', 'Banamex — Red Bupa', 'BNX Bupa', 'Bupa' FROM cat.Aseguradoras WHERE Clave = 'BANAMEX';
GO

-- FORMATOS RECETA
INSERT INTO cat.FormatosReceta (Clave, Descripcion) VALUES
('DIGITAL',    'Receta digital impresa'),
('MANUSCRITO', 'Receta completamente manuscrita'),
('MIXTO',      'Receta con campos manuscritos e impresos');
GO

-- ESTADOS IMAGEN
INSERT INTO cat.EstadosImagen (Clave, Descripcion, Orden, EsFinal) VALUES
('RECIBIDA',                'Imagen recibida y subida al blob raw',      1, 0),
('LEGIBLE',                 'Evaluada como legible por API OCR',          2, 0),
('ILEGIBLE',                'Ilegible, requiere captura manual',          2, 0),
('CAPTURA_MANUAL_COMPLETA', 'Datos capturados manualmente por revisor',   3, 0),
('OCR_APROBADO',            'OCR exitoso, confianza >= umbral',           3, 0),
('OCR_BAJA_CONFIANZA',      'OCR con confianza menor al umbral',          3, 0),
('EXTRACCION_COMPLETA',     'Campos extraídos completos por API',         4, 0),
('EXTRACCION_INCOMPLETA',   'Extracción con campos faltantes',            4, 0),
('REVISADA',                'Aprobada por revisor humano',                5, 1),
('RECHAZADA',               'Rechazada por revisor humano',               5, 1);
GO

-- ESTADOS GRUPO
INSERT INTO cat.EstadosGrupo (Clave, Descripcion, Orden, EsFinal) VALUES
('RECIBIDO',                  'Grupo creado, recibiendo imágenes',      1, 0),
('REQUIERE_CAPTURA_MANUAL',   'Al menos una imagen es ilegible',        2, 0),
('PROCESANDO',                'En proceso OCR vía API externa',          2, 0),
('GRUPO_INCOMPLETO',          'Faltan imágenes por procesar o revisar', 3, 0),
('REVISION_PENDIENTE',        'Pendiente de revisión humana',           3, 0),
('REVISADO_COMPLETO',         'Todas las imágenes revisadas y OK',      4, 0),
('DATOS_FISCALES_INCOMPLETOS','Faltan datos para generar CFDI 4.0',     5, 0),
('PENDIENTE_AUTORIZACION',    'Esperando autorización de farmacia',     5, 0),
('PENDIENTE_FACTURACION',     'Listo para facturar',                    6, 0),
('PREFACTURA_GENERADA',       'Pre-factura generada',                   7, 0),
('FACTURADA',                 'CFDI timbrado exitosamente',             8, 1),
('ERROR_TIMBRADO_MANUAL',     'Error de timbrado, requiere atención',   7, 0),
('RECHAZADO',                 'Grupo rechazado, no se facturará',       8, 1);
GO

-- CATÁLOGOS MÉDICOS
INSERT INTO cat.ViasAdministracion (Clave, Descripcion) VALUES
('ORAL','Vía oral'),('TOPICA','Tópica'),('IV','Intravenosa'),('IM','Intramuscular'),
('OFTALMICA','Gotas oftálmicas'),('NASAL','Vía nasal'),('SUBLINGUAL','Sublingual'),
('ENTERAL','Enteral / sonda'),('OTRO','Otra vía');
GO

INSERT INTO cat.Especialidades (Clave, Descripcion) VALUES
('GERIATRIA','Geriatría'),('CARDIOLOGIA','Cardiología'),('DERMATOLOGIA_PED','Dermatología Pediátrica'),
('DERMATOLOGIA','Dermatología'),('ORTOPEDIA','Ortopedia y Traumatología'),
('MEDICINA_GENERAL','Medicina General'),('MEDICINA_INTERNA','Medicina Interna'),
('OFTALMOLOGIA','Oftalmología'),('ANGIOLOGIA','Angiología y Cirugía Vascular'),
('PEDIATRIA','Pediatría'),('ENDOCRINOLOGIA','Endocrinología'),('NEUROLOGIA','Neurología'),
('PSIQUIATRIA','Psiquiatría'),('OTRO','Otra especialidad');
GO

-- CATÁLOGOS SAT
INSERT INTO cat.ClavesSAT (Clave, Descripcion, Tipo) VALUES
('51101500','Medicamentos y productos farmacéuticos - genérico','MEDICAMENTO'),
('51101501','Antibióticos','MEDICAMENTO'),
('51101502','Analgésicos y antipiréticos','MEDICAMENTO'),
('51101503','Medicamentos cardiovasculares','MEDICAMENTO'),
('51101504','Medicamentos para diabetes','MEDICAMENTO'),
('51101505','Medicamentos oftálmicos','MEDICAMENTO'),
('51101506','Suplementos, probióticos y vitaminas','MEDICAMENTO'),
('51101507','Antiinflamatorios no esteroideos (AINEs)','MEDICAMENTO'),
('51101508','Antihipertensivos y diuréticos','MEDICAMENTO'),
('51101509','Antidepresivos y ansiolíticos','MEDICAMENTO'),
('51101510','Medicamentos dermatológicos tópicos','MEDICAMENTO'),
('51101511','Anticoagulantes y antitrombóticos','MEDICAMENTO'),
('85121500','Servicios médicos generales','SERVICIO'),
('85121600','Servicios médicos de especialidad','SERVICIO');
GO

INSERT INTO cat.UnidadesSAT (Clave, Nombre, Descripcion) VALUES
('H87','Pieza','Unidad contable de artículos'),('BX','Caja','Caja de medicamentos'),
('BT','Botella','Frasco / botella'),('GRM','Gramo','Gramo'),('MLT','Mililitro','Mililitro'),
('KGM','Kilogramo','Kilogramo'),('MGM','Miligramo','Miligramo'),('MCG','Microgramo','Microgramo'),
('E51','Trabajo','Servicio realizado'),('ACT','Actividad','Actividad');
GO

INSERT INTO cat.RegimenFiscal (Clave, Descripcion, AplicaFisica, AplicaMoral) VALUES
('601','General de Ley Personas Morales',0,1),('603','Personas Morales con Fines no Lucrativos',0,1),
('605','Sueldos y Salarios e Ingresos Asimilados',1,0),('606','Arrendamiento',1,0),
('608','Demás ingresos',1,0),('612','Personas Físicas con Actividades Empresariales',1,0),
('616','Sin obligaciones fiscales',1,0),('621','Incorporación Fiscal',1,0),
('626','Régimen Simplificado de Confianza',1,1);
GO

INSERT INTO cat.UsoCFDI (Clave, Descripcion, AplicaFisica, AplicaMoral) VALUES
('G01','Adquisición de mercancias',1,1),('G03','Gastos en general',1,1),
('D01','Honorarios médicos, dentales y hospitalarios',1,0),
('D02','Gastos médicos por incapacidad o discapacidad',1,0),
('D07','Primas por seguros de gastos médicos',1,0),
('S01','Sin efectos fiscales',1,1),('CP01','Pagos',1,1);
GO

INSERT INTO cat.MetodosPago (Clave, Descripcion) VALUES
('PUE','Pago en una sola exhibición'),('PPD','Pago en parcialidades o diferido');
GO

INSERT INTO cat.FormasPago (Clave, Descripcion) VALUES
('01','Efectivo'),('02','Cheque nominativo'),('03','Transferencia electrónica'),
('04','Tarjeta de crédito'),('28','Tarjeta de débito'),('29','Tarjeta de servicios'),('99','Por definir');
GO

INSERT INTO cat.Monedas (Clave, Descripcion) VALUES ('MXN','Peso Mexicano'),('USD','Dólar americano'),('EUR','Euro');
GO

INSERT INTO cat.TiposRelacionCFDI (Clave, Descripcion) VALUES
('01','Nota de crédito de los documentos relacionados'),
('02','Nota de débito de los documentos relacionados'),
('03','Devolución de mercancía sobre facturas o traslados previos'),
('04','Sustitución de los CFDI previos'),
('05','Traslados de mercancias facturados previamente'),
('06','Factura generada por los traslados previos'),
('07','CFDI por aplicación de anticipo');
GO

-- MEDICAMENTOS
INSERT INTO cat.Medicamentos (NombreComercial, SustanciaActiva, Presentacion, Concentracion, ClaveSAT, ClaveUnidadSAT, IVATasa) VALUES
('Kombiglyze XR',    'Saxagliptina + Metformina', 'tabletas',    '5/1000mg',  '51101504','H87',0),
('Elatec',           'Pentoxifilina',              'tabletas',    '1000mg',    '51101503','H87',0),
('Eliquis',          'Apixabán',                   'tabletas',    '2.5mg',     '51101511','H87',0),
('Icaden',           'Isoconazol',                 'crema',       '10mg/g',    '51101510','BT', 0),
('Vilzermet',        'Vildagliptina + Metformina', 'comprimidos', '50/500mg',  '51101504','H87',0),
('Natrilix-SR',      'Indapamida',                 'comprimidos', '1.5mg',     '51101508','H87',0),
('Indometacina',     'Indometacina',               'crema',       '2.5%',      '51101507','BT', 0),
('Weserix',          'Etoricoxib',                 'tabletas',    '90mg',      '51101507','H87',0),
('Fapris',           'Desvenlafaxina',             'tabletas',    '100mg',     '51101509','H87',0),
('Olopatadina',      'Olopatadina',                'gotas',       '0.2%',      '51101505','BT', 0),
('Betahistina',      'Betahistina',                'tabletas',    '24mg',      '51101500','H87',0),
('Psyllium Plantago','Psyllium Plantago',           'polvo',       NULL,        '51101506','BX', 0),
('Concor',           'Bisoprolol',                 'tabletas',    '5mg',       '51101503','H87',0),
('Tegreto LC',       'Carbamazepina',              'tabletas',    '200mg',     '51101500','H87',0),
('Eulrox',           'Levotiroxina',               'tabletas',    '88mcg',     '51101500','H87',0),
('Amably',           'Trimebutina',                'cápsulas',    '40mg',      '51101500','H87',0),
('Ciproxina',        'Ciprofloxacino',             'suspensión',  '250mg/5ml', '51101501','BT', 0),
('Enterogermina',    'Bacillus clausii',            'ampolletas',  '4 billones','51101506','BX', 0),
('Ondansetrón',      'Ondansetrón',                'tabletas',    '4mg',       '51101500','H87',0);
GO

-- ============================================================
-- RESUMEN FINAL
-- ============================================================
PRINT '================================================================';
PRINT ' RecetasOCR v5.2 — Script ejecutado exitosamente (orden corregido)';
PRINT '----------------------------------------------------------------';
PRINT ' Esquemas  : seg, cat, cfg, rec, med, ocr, rev, fac, aud  (9)';
PRINT ' Tablas    : 44 en total';
PRINT ' Índices   : 47';
PRINT ' Roles     : 6 | Módulos: 21 | Aseguradoras: 13';
PRINT ' Sin FKs rotas — orden de creación corregido';
PRINT '================================================================';
GO
