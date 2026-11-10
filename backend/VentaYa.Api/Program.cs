using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using VentaYa.Api.Data;
using VentaYa.Api.Middleware;
using VentaYa.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Base de datos (SQL Server) ──
builder.Services.AddDbContext<VentaYaDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:Default en la configuración")));

// ── Servicios propios ──
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<JwtService>();
builder.Services.AddScoped<NegocioActual>();
builder.Services.AddScoped<PlanService>();
builder.Services.AddScoped<PedidosService>();

// ── Autenticación con JWT ──
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException("Falta Jwt:Key (mínimo 32 caracteres) en la configuración");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false; // dejamos los claims con su nombre original ("sub", "email")
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };
    });
builder.Services.AddAuthorization();

// ── Controllers + Swagger ──
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter())); // enums como texto
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "VentaYa API", Version = "v1", Description = "API REST de gestión de pedidos para pequeños comercios" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Pegá el token que devuelve /api/auth/login",
    });
    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", doc)] = []
    });
});

var app = builder.Build();

// Crea/actualiza la base automáticamente al iniciar (útil en el hosting).
string? errorMigracion = null;
if (app.Configuration.GetValue("Database:MigrateOnStartup", true))
{
    // Si la base no está accesible (p. ej. connection string incorrecta) la API IGUAL arranca,
    // así Swagger y /api/estado permiten ver el motivo en vez de un "500.30 failed to start" sin explicación.
    try
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<VentaYaDbContext>().Database.Migrate();
    }
    catch (Exception ex)
    {
        errorMigracion = ex.GetBaseException().Message;
        app.Logger.LogError(ex, "No se pudieron aplicar las migraciones al iniciar");
    }
}

app.UseMiddleware<ErrorMiddleware>();
app.UseStaticFiles(); // sirve /uploads (imágenes de productos)
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "VentaYa API v1"));
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// Diagnóstico público: dice si la API y la base de datos funcionan. Útil para verificar el despliegue.
// El detalle del error (sin credenciales) se puede ocultar con "Diagnostico:MostrarDetalle": false.
app.MapGet("/api/estado", async (VentaYaDbContext db, IConfiguration config) =>
{
    var detalle = config.GetValue("Diagnostico:MostrarDetalle", true);
    static string Recortar(string? s) => s is null ? "" : (s.Length > 300 ? s[..300] : s);
    try
    {
        var conecta = await db.Database.CanConnectAsync();
        var pendientes = conecta ? (await db.Database.GetPendingMigrationsAsync()).Count() : -1;
        var ok = conecta && pendientes == 0;
        return Results.Json(new
        {
            api = "ok",
            baseDeDatos = conecta ? "ok" : "sin conexion",
            migracionesPendientes = pendientes,
            detalle = detalle ? Recortar(errorMigracion) : null,
        }, statusCode: ok ? 200 : 503);
    }
    catch (Exception ex)
    {
        return Results.Json(new { api = "ok", baseDeDatos = "error", detalle = detalle ? Recortar(ex.GetBaseException().Message) : null }, statusCode: 503);
    }
}).AllowAnonymous().WithTags("Diagnóstico");

app.Run();

public partial class Program { } // para poder probar la API en los tests
