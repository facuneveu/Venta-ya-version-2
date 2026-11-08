using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VentaYa.Api.Data;
using VentaYa.App.Core.Services;

namespace VentaYa.Tests.App;

/// <summary>La API real corriendo en memoria (sin SQL Server) para probar la app contra ella.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public ApiFactory()
    {
        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<VentaYaDbContext>().Database.EnsureCreated();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Database:MigrateOnStartup", "false");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<VentaYaDbContext>));
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<VentaYaDbContext>));
            var nombre = Guid.NewGuid().ToString();
            services.AddDbContext<VentaYaDbContext>(o => o.UseInMemoryDatabase(nombre));
        });
    }
}

public class NavegacionFalsa : INavegacion
{
    public List<(string Ruta, int? Id, bool Raiz)> Visitas { get; } = [];
    public int Volver { get; private set; }
    public string? Ultima => Visitas.Count > 0 ? Visitas[^1].Ruta : null;

    public Task IrAAsync(string ruta, int? id = null, bool raiz = false)
    {
        Visitas.Add((ruta, id, raiz));
        return Task.CompletedTask;
    }

    public Task VolverAsync() { Volver++; return Task.CompletedTask; }
}

public class DialogosFalsos : IDialogos
{
    public bool Respuesta { get; set; } = true;
    public List<string> Avisos { get; } = [];
    public Task AvisarAsync(string titulo, string mensaje) { Avisos.Add(mensaje); return Task.CompletedTask; }
    public Task<bool> ConfirmarAsync(string titulo, string mensaje, string aceptar = "Sí", string cancelar = "No") => Task.FromResult(Respuesta);
}

/// <summary>Un "teléfono" de pruebas: sesión + cliente API + navegación, conectados a la API en memoria.</summary>
public class Telefono
{
    public Sesion Sesion { get; }
    public IApiClient Api { get; }
    public NavegacionFalsa Nav { get; } = new();
    public DialogosFalsos Dialogos { get; } = new();
    public AlmacenEnMemoria Almacen { get; } = new();

    public Telefono(HttpClient http)
    {
        Sesion = new Sesion(Almacen);
        Api = new ApiClient(http, Sesion);
    }
}
