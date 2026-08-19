using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Prodhab.Api.Configuracion;
using Prodhab.Api.Data;
using Prodhab.Api.Infrastructure;
using Prodhab.Api.Services;

const string CorsDesarrollo = "CorsDesarrollo";

var builder = WebApplication.CreateBuilder(args);

// En producción no se puede arrancar con una clave JWT vacía: arrancar así dejaría
// la firma de tokens sin protección real.
if (builder.Environment.IsProduction() &&
    string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Key"]))
{
    throw new InvalidOperationException(
        "Falta Jwt:Key en Production. Configure la variable de entorno Jwt__Key (mínimo 32 caracteres) antes de arrancar.");
}

// Add services to the container.

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<AlmacenamientoOptions>(
    builder.Configuration.GetSection("Almacenamiento"));

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

builder.Services.Configure<AdminInicialOptions>(builder.Configuration.GetSection("AdminInicial"));

// 10 MB del archivo + margen para el resto del multipart.
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 12_000_000;
});

// El usuario actual sale de los claims de la petición.
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IExpedienteService, ExpedienteService>();
builder.Services.AddScoped<IDatosFormularioService, DatosFormularioService>();
builder.Services.AddScoped<IValidadorArchivo, ValidadorArchivo>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<ISubsanacionService, SubsanacionService>();
builder.Services.AddScoped<IRevisionService, RevisionService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // patrón del proyecto
        var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = "sub"
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Orígenes permitidos por configuración: "Cors:AllowedOrigins" (localhost:5173 en Development;
// el dominio real del front de PRODHAB por variables de entorno en producción).
var origenesPermitidos = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsDesarrollo, policy =>
        policy.WithOrigins(origenesPermitidos)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

if (origenesPermitidos.Length == 0)
{
    app.Logger.LogWarning("Cors:AllowedOrigins está vacío; ningún origen podrá llamar a la API.");
}

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors(CorsDesarrollo);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var adminInicial = scope.ServiceProvider.GetRequiredService<IOptions<AdminInicialOptions>>().Value;
    await DbSeeder.SeedAdminInicialAsync(db, adminInicial, app.Logger);
}

app.Run();
