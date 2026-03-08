using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RecetasOCR.API.Authorization;
using RecetasOCR.API.Middlewares;
using RecetasOCR.Application;
using RecetasOCR.Application.Common.Interfaces;
using RecetasOCR.Infrastructure.Extensions;
using Serilog;

// ── Bootstrap Serilog (captura errores durante el arranque) ─────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── 1. Serilog desde configuración ──────────────────────────────────────
    builder.Host.UseSerilog((ctx, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration));

    // ── 2. Application + Infrastructure ─────────────────────────────────────
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // ── ICurrentUserService (lee HttpContext.User tras autenticación) ────────
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    // ── Authorization con PermisoPolicyProvider dinámico ─────────────────────
    builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermisoPolicyProvider>();
    builder.Services.AddScoped<IAuthorizationHandler, PermisoRequirementHandler>();

    // ── 3. JWT Authentication ────────────────────────────────────────────────
    var jwtSection = builder.Configuration.GetSection("Jwt");
    var secretKey  = jwtSection["SecretKey"]
                     ?? throw new InvalidOperationException("Falta Jwt:SecretKey.");

    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = new SymmetricSecurityKey(
                                               Encoding.UTF8.GetBytes(secretKey)),
                ValidateIssuer   = true,
                ValidIssuer      = jwtSection["Issuer"],
                ValidateAudience = true,
                ValidAudience    = jwtSection["Audience"],
                ValidateLifetime = true,
                ClockSkew        = TimeSpan.FromMinutes(1)
            };
        });

    // ── 4. Authorization ─────────────────────────────────────────────────────
    builder.Services.AddAuthorization();

    // ── 5. CORS (solo Development) ───────────────────────────────────────────
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DevCors", policy =>
            policy.AllowAnyOrigin()      // Permite CUALQUIER origen
                  .AllowAnyHeader()      // Permite CUALQUIER header
                  .AllowAnyMethod());    // Permite CUALQUIER método (GET, POST, PUT, DELETE, etc.)
    });

    // ── 6. Controllers + Swagger con Bearer token ────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title   = "RecetasOCR API",
            Version = "v1",
            Description = "API de procesamiento OCR de recetas médicas y facturación CFDI 4.0"
        });

        // Evita conflictos de OperationId cuando varios controllers tienen
        // métodos con el mismo nombre (ej: GetAll, GetById, GetCola).
        c.CustomOperationIds(e =>
            $"{e.ActionDescriptor.RouteValues["controller"]}_{e.ActionDescriptor.RouteValues["action"]}");

        // Evita conflictos de schemaId cuando dos tipos en namespaces distintos
        // tienen el mismo nombre de clase (ej: EstadoOcrDto en Ocr vs Imagenes).
        c.CustomSchemaIds(type => type.FullName!.Replace("+", "."));

        // Esquema de seguridad Bearer
        var scheme = new OpenApiSecurityScheme
        {
            Name         = "Authorization",
            Type         = SecuritySchemeType.Http,
            Scheme       = "bearer",
            BearerFormat = "JWT",
            In           = ParameterLocation.Header,
            Description  = "Ingresa el token JWT. Ejemplo: eyJhbGci..."
        };
        c.AddSecurityDefinition("Bearer", scheme);
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id   = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // ────────────────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ── 7. Middlewares en orden ──────────────────────────────────────────────
    // Serilog request logging (antes del exception handler para logear también los errores)
    app.UseSerilogRequestLogging();

    // Exception handler personalizado → JSON estándar sin stack trace
    app.UseMiddleware<ExceptionHandlerMiddleware>();

    // CORS completamente abierto (sin restricciones en desarrollo)
    app.UseCors("DevCors");

    if (app.Environment.IsDevelopment())
    {
        // ── 8. Swagger solo en Development ──────────────────────────────────
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "RecetasOCR API v1");
            c.RoutePrefix = "swagger";
        });
    }
    else
    {
        // ── Headers de seguridad estrictos para Production ──────────────────
        app.Use(async (context, next) =>
        {
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
            context.Response.Headers["Content-Security-Policy"] = 
                "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:;";

            await next();
        });
    }

    app.UseAuthentication();
    app.UseAuthorization();

    // Middleware de auditoría (lee ClaimsPrincipal ya resuelto) 
    app.UseMiddleware<AuditMiddleware>();

    //app.MapGet("/dev/hash/{password}", (string password) =>
    //BCrypt.Net.BCrypt.HashPassword(password, 11));


    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "RecetasOCR.API terminó inesperadamente durante el arranque.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
