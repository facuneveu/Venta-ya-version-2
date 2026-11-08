using Microsoft.EntityFrameworkCore;
using VentaYa.Api.Data;
using VentaYa.Api.Dtos;
using VentaYa.Api.Models;
using VentaYa.Api.Services;

namespace VentaYa.Tests;

/// <summary>Tests de los servicios contra una base en memoria (EF Core InMemory).</summary>
public class PedidosServiceTests
{
    private static VentaYaDbContext NuevoContexto()
    {
        var options = new DbContextOptionsBuilder<VentaYaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new VentaYaDbContext(options);
        db.Database.EnsureCreated(); // incluye los 3 planes sembrados
        return db;
    }

    private static async Task<Negocio> NuevoNegocioAsync(VentaYaDbContext db, int planId = PlanesIds.Pago)
    {
        var usuario = new Usuario { Nombre = "Dueño", Email = $"{Guid.NewGuid():N}@test.com", PasswordHash = "x" };
        var negocio = new Negocio
        {
            Propietario = usuario, PlanId = planId, Nombre = "Kiosco", Telefono = "2954000000",
            Slug = Guid.NewGuid().ToString("N"), EnvioHabilitado = true,
        };
        db.Negocios.Add(negocio);
        await db.SaveChangesAsync();
        await db.Entry(negocio).Reference(n => n.Plan).LoadAsync();
        return negocio;
    }

    private static PedidosService Servicio(VentaYaDbContext db) => new(db, new PlanService(db));

    private static PedidoCrearDto Dto(int productoId, int cantidad, int? zonaId = null, string? cupon = null) =>
        new("Ana", "2954111222", [new ItemPedidoCrearDto(productoId, cantidad)],
            MetodoPago.Efectivo, zonaId, cupon, zonaId is null ? null : "Calle 123");

    [Fact]
    public async Task CrearPedido_CalculaTotalConEnvioYCupon()
    {
        var db = NuevoContexto();
        var negocio = await NuevoNegocioAsync(db);
        var producto = new Producto { NegocioId = negocio.Id, Nombre = "Alfajor", Precio = 1000 };
        var zona = new ZonaEnvio { NegocioId = negocio.Id, Nombre = "Centro", Precio = 300 };
        var cupon = new Cupon { NegocioId = negocio.Id, Codigo = "PROMO10", Tipo = TipoCupon.Porcentaje, Valor = 10 };
        db.AddRange(producto, zona, cupon);
        await db.SaveChangesAsync();

        var pedido = await Servicio(db).CrearAsync(negocio, Dto(producto.Id, 2, zona.Id, "promo10"));

        Assert.Equal(2000m, pedido.Subtotal);
        Assert.Equal(200m, pedido.DescuentoAplicado);
        Assert.Equal(300m, pedido.CostoEnvio);
        Assert.Equal(2100m, pedido.Total); // 2000 - 200 + 300
        Assert.Equal(EstadoPedido.Pendiente, pedido.Estado);
    }

    [Fact]
    public async Task CrearPedido_DescuentaStock_YLoDevuelveAlCancelar()
    {
        var db = NuevoContexto();
        var negocio = await NuevoNegocioAsync(db);
        var producto = new Producto { NegocioId = negocio.Id, Nombre = "Alfajor", Precio = 1000, Stock = 10 };
        db.Productos.Add(producto);
        await db.SaveChangesAsync();
        var servicio = Servicio(db);

        var pedido = await servicio.CrearAsync(negocio, Dto(producto.Id, 4));
        Assert.Equal(6, producto.Stock);

        await servicio.CambiarEstadoAsync(negocio, pedido.Id, EstadoPedido.Cancelado);
        Assert.Equal(10, producto.Stock);
    }

    [Fact]
    public async Task CrearPedido_ConStockInsuficiente_Falla()
    {
        var db = NuevoContexto();
        var negocio = await NuevoNegocioAsync(db);
        var producto = new Producto { NegocioId = negocio.Id, Nombre = "Alfajor", Precio = 1000, Stock = 2 };
        db.Productos.Add(producto);
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => Servicio(db).CrearAsync(negocio, Dto(producto.Id, 3)));
        Assert.Contains("Solo quedan 2", ex.Message);
    }

    [Fact]
    public async Task CrearPedido_ConProductoDeOtroNegocio_Falla()
    {
        var db = NuevoContexto();
        var mio = await NuevoNegocioAsync(db);
        var otro = await NuevoNegocioAsync(db);
        var ajeno = new Producto { NegocioId = otro.Id, Nombre = "Ajeno", Precio = 500 };
        db.Productos.Add(ajeno);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ReglaNegocioException>(() => Servicio(db).CrearAsync(mio, Dto(ajeno.Id, 1)));
    }

    [Fact]
    public async Task CrearPedido_ConCuponVencido_Falla()
    {
        var db = NuevoContexto();
        var negocio = await NuevoNegocioAsync(db);
        var producto = new Producto { NegocioId = negocio.Id, Nombre = "Alfajor", Precio = 1000 };
        var cupon = new Cupon { NegocioId = negocio.Id, Codigo = "VIEJO", Tipo = TipoCupon.MontoFijo, Valor = 100, VenceEn = DateTime.UtcNow.AddDays(-1) };
        db.AddRange(producto, cupon);
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => Servicio(db).CrearAsync(negocio, Dto(producto.Id, 1, null, "VIEJO")));
        Assert.Equal("El cupón está vencido", ex.Message);
    }

    [Fact]
    public async Task CrearPedido_ReutilizaClientePorTelefono()
    {
        var db = NuevoContexto();
        var negocio = await NuevoNegocioAsync(db);
        var producto = new Producto { NegocioId = negocio.Id, Nombre = "Alfajor", Precio = 1000 };
        db.Productos.Add(producto);
        await db.SaveChangesAsync();
        var servicio = Servicio(db);

        await servicio.CrearAsync(negocio, Dto(producto.Id, 1));
        await servicio.CrearAsync(negocio, Dto(producto.Id, 1));

        Assert.Equal(1, await db.Clientes.CountAsync());
        Assert.Equal(2, await db.Pedidos.CountAsync());
    }

    [Fact]
    public async Task PlanGratis_LimitaProductosA10()
    {
        var db = NuevoContexto();
        var negocio = await NuevoNegocioAsync(db, PlanesIds.Gratis);
        for (var i = 0; i < 10; i++) db.Productos.Add(new Producto { NegocioId = negocio.Id, Nombre = $"P{i}", Precio = 1 });
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => new PlanService(db).ValidarNuevoProductoAsync(negocio));
        Assert.Equal(403, ex.Status);
    }

    [Fact]
    public async Task PlanPago_NoTieneLimites()
    {
        var db = NuevoContexto();
        var negocio = await NuevoNegocioAsync(db, PlanesIds.Pago);
        for (var i = 0; i < 25; i++) db.Productos.Add(new Producto { NegocioId = negocio.Id, Nombre = $"P{i}", Precio = 1 });
        await db.SaveChangesAsync();

        await new PlanService(db).ValidarNuevoProductoAsync(negocio); // no lanza
    }

    [Fact]
    public async Task PruebaVencida_VuelveAlLimiteDelPlanGratis()
    {
        var db = NuevoContexto();
        var negocio = await NuevoNegocioAsync(db, PlanesIds.Prueba);
        negocio.TrialExpiraEn = DateTime.UtcNow.AddDays(-1);

        var limites = await new PlanService(db).ObtenerLimitesAsync(negocio);
        Assert.Equal(10, limites.MaxProductos);
        Assert.Equal(30, limites.MaxPedidosMes);
    }

    [Fact]
    public async Task PlanGratis_LimitaPedidosPorMes()
    {
        var db = NuevoContexto();
        var negocio = await NuevoNegocioAsync(db, PlanesIds.Gratis);
        var producto = new Producto { NegocioId = negocio.Id, Nombre = "Alfajor", Precio = 1000 };
        var cliente = new Cliente { Nombre = "Ana", Telefono = "2954111222" };
        db.AddRange(producto, cliente);
        await db.SaveChangesAsync();
        for (var i = 0; i < 30; i++)
            db.Pedidos.Add(new Pedido { NegocioId = negocio.Id, ClienteId = cliente.Id });
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => Servicio(db).CrearAsync(negocio, Dto(producto.Id, 1)));
        Assert.Equal(403, ex.Status);
    }
}
