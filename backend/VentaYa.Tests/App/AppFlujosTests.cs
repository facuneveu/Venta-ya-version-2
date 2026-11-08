using System.Net;
using VentaYa.App.Core.Models;
using VentaYa.App.Core.Services;
using VentaYa.App.Core.ViewModels;

namespace VentaYa.Tests.App;

/// <summary>
/// Pruebas de integración de la app móvil: los ViewModels (la lógica de cada pantalla) hablan con la API
/// real corriendo en memoria. Es lo mismo que hará la app MAUI, sin necesitar un emulador.
/// </summary>
public class AppFlujosTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _fabrica;
    public AppFlujosTests(ApiFactory fabrica) => _fabrica = fabrica;

    private Telefono NuevoTelefono() => new(_fabrica.CreateClient());

    private static string EmailNuevo() => $"u{Guid.NewGuid():N}@test.com";

    private static async Task RegistrarYCrearNegocioAsync(Telefono t, bool envios = false)
    {
        var registro = new RegistroViewModel(t.Api, t.Sesion, t.Nav)
        { Nombre = "Facu", Email = EmailNuevo(), Password = "secreta123", Confirmacion = "secreta123" };
        await registro.CrearCuentaCommand.ExecuteAsync(null);
        var onboarding = new OnboardingViewModel(t.Api, t.Sesion, t.Nav)
        { Nombre = "Kiosco Test", Telefono = "2954111222", EnvioHabilitado = envios };
        await onboarding.CrearNegocioCommand.ExecuteAsync(null);
    }

    private static async Task<ProductoDto> CrearProductoAsync(Telefono t, string nombre, string precio, string stock = "")
    {
        var form = new ProductoFormViewModel(t.Api, t.Nav, t.Dialogos) { Nombre = nombre, Precio = precio, Stock = stock };
        await form.GuardarCommand.ExecuteAsync(null);
        Assert.Null(form.Error);
        return (await t.Api.ProductosAsync()).Single(p => p.Nombre == nombre);
    }

    // ───── acceso ─────
    [Fact]
    public async Task Registro_LlevaAlOnboarding_YElOnboardingAlInicio()
    {
        var t = NuevoTelefono();
        var registro = new RegistroViewModel(t.Api, t.Sesion, t.Nav)
        { Nombre = "Facu", Email = EmailNuevo(), Password = "secreta123", Confirmacion = "secreta123" };

        await registro.CrearCuentaCommand.ExecuteAsync(null);

        Assert.True(t.Sesion.EstaLogueado);
        Assert.Equal((Rutas.Onboarding, null, true), t.Nav.Visitas.Single());

        var onboarding = new OnboardingViewModel(t.Api, t.Sesion, t.Nav) { Nombre = "Café Ñandú", Telefono = "2954111222" };
        await onboarding.CrearNegocioCommand.ExecuteAsync(null);

        Assert.Equal(Rutas.Inicio, t.Nav.Ultima);
        Assert.Equal("Propietario", t.Sesion.Negocio!.Rol);
        Assert.StartsWith("cafe-nandu", t.Sesion.Negocio.Slug);
    }

    [Fact]
    public async Task Registro_ValidaLosCamposAntesDeLlamarALaApi()
    {
        var t = NuevoTelefono();
        var vm = new RegistroViewModel(t.Api, t.Sesion, t.Nav)
        { Nombre = "Facu", Email = EmailNuevo(), Password = "123", Confirmacion = "123" };
        await vm.CrearCuentaCommand.ExecuteAsync(null);
        Assert.Contains("6 caracteres", vm.Error);

        vm.Password = "secreta123"; vm.Confirmacion = "otra";
        await vm.CrearCuentaCommand.ExecuteAsync(null);
        Assert.Contains("no coinciden", vm.Error);
        Assert.False(t.Sesion.EstaLogueado);
        Assert.Empty(t.Nav.Visitas);
    }

    [Fact]
    public async Task Login_ConClaveIncorrecta_MuestraElErrorYNoNavega()
    {
        var t = NuevoTelefono();
        var email = EmailNuevo();
        await t.Api.RegistroAsync(new RegistroDto("Facu", email, "secreta123"));

        var login = new LoginViewModel(t.Api, t.Sesion, t.Nav) { Email = email, Password = "mala" };
        await login.IngresarCommand.ExecuteAsync(null);

        Assert.Contains("incorrectos", login.Error);
        Assert.True(login.HayError);
        Assert.False(login.EstaOcupado);
        Assert.Empty(t.Nav.Visitas);
    }

    [Fact]
    public async Task Login_ConNegocio_VaDirectoAlInicio()
    {
        var t = NuevoTelefono();
        await RegistrarYCrearNegocioAsync(t);
        var email = t.Sesion.Usuario!.Email;

        var otro = NuevoTelefono(); // otro dispositivo, sin sesión
        var login = new LoginViewModel(otro.Api, otro.Sesion, otro.Nav) { Email = email, Password = "secreta123" };
        await login.IngresarCommand.ExecuteAsync(null);

        Assert.Equal((Rutas.Inicio, null, true), otro.Nav.Visitas.Single());
        Assert.Equal("Kiosco Test", otro.Sesion.Negocio!.Nombre);
    }

    [Fact]
    public async Task Splash_ConSesionGuardada_EntraDirecto_YSinSesionVaAlLogin()
    {
        var t = NuevoTelefono();
        await RegistrarYCrearNegocioAsync(t);

        var reabierta = NuevoTelefono();
        await reabierta.Almacen.GuardarAsync(t.Sesion.Token!); // la app se vuelve a abrir con el token guardado
        await new SplashViewModel(reabierta.Api, reabierta.Sesion, reabierta.Nav).IniciarCommand.ExecuteAsync(null);
        Assert.Equal(Rutas.Inicio, reabierta.Nav.Ultima);

        var sinSesion = NuevoTelefono();
        await new SplashViewModel(sinSesion.Api, sinSesion.Sesion, sinSesion.Nav).IniciarCommand.ExecuteAsync(null);
        Assert.Equal(Rutas.Login, sinSesion.Nav.Ultima);
    }

    [Fact]
    public async Task Splash_ConTokenInvalido_BorraLaSesionYVaAlLogin()
    {
        var t = NuevoTelefono();
        await t.Almacen.GuardarAsync("token-que-no-sirve");
        await new SplashViewModel(t.Api, t.Sesion, t.Nav).IniciarCommand.ExecuteAsync(null);

        Assert.Equal(Rutas.Login, t.Nav.Ultima);
        Assert.False(t.Sesion.EstaLogueado);
    }

    [Fact]
    public async Task TokenVencido_CierraLaSesion_YAvisaParaVolverAlLogin()
    {
        var t = NuevoTelefono();
        await RegistrarYCrearNegocioAsync(t);
        var avisos = 0;
        t.Sesion.SesionCerrada += () => avisos++;

        await t.Almacen.GuardarAsync("x");
        typeof(Sesion).GetProperty(nameof(Sesion.Token))!.SetValue(t.Sesion, "token-roto"); // simula un token vencido

        var ex = await Assert.ThrowsAsync<ApiException>(() => t.Api.PanelAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, ex.Status);
        Assert.Equal(1, avisos);
        Assert.False(t.Sesion.EstaLogueado);
    }

    // ───── catálogo ─────
    [Fact]
    public async Task ProductoForm_ValidaPrecioYStock_ConMensajesClaros()
    {
        var t = NuevoTelefono();
        await RegistrarYCrearNegocioAsync(t);
        var form = new ProductoFormViewModel(t.Api, t.Nav, t.Dialogos) { Nombre = "Alfajor", Precio = "abc" };

        await form.GuardarCommand.ExecuteAsync(null);
        Assert.Equal("Ingresá un precio válido.", form.Error);

        form.Precio = "1.500,50"; form.Stock = "-3";
        await form.GuardarCommand.ExecuteAsync(null);
        Assert.Contains("stock", form.Error);
        Assert.Empty(await t.Api.ProductosAsync());
    }

    [Fact]
    public async Task Catalogo_CrearEditarBuscarYEliminar()
    {
        var t = NuevoTelefono();
        await RegistrarYCrearNegocioAsync(t);
        var alfajor = await CrearProductoAsync(t, "Alfajor", "1500,50", "10");
        await CrearProductoAsync(t, "Gaseosa", "2200");
        Assert.Equal(1500.50m, alfajor.Precio);

        var catalogo = new CatalogoViewModel(t.Api, t.Nav);
        await catalogo.CargarCommand.ExecuteAsync(null);
        Assert.Equal(2, catalogo.Productos.Count);
        catalogo.Busqueda = "Gas"; // (la base en memoria distingue mayúsculas; SQL Server no)
        await catalogo.CargarCommand.ExecuteAsync(null);
        Assert.Equal("Gaseosa", catalogo.Productos.Single().Nombre);

        var edicion = new ProductoFormViewModel(t.Api, t.Nav, t.Dialogos) { Id = alfajor.Id };
        await edicion.CargarCommand.ExecuteAsync(null);
        Assert.Equal("Alfajor", edicion.Nombre);
        edicion.Precio = "1800";
        await edicion.GuardarCommand.ExecuteAsync(null);
        Assert.Equal(1800m, (await t.Api.ProductoAsync(alfajor.Id)).Precio);

        await edicion.EliminarCommand.ExecuteAsync(null);
        Assert.Single(await t.Api.ProductosAsync());
    }

    [Fact]
    public async Task ProductoForm_ElProductoVendidoNoSeElimina_YMuestraElMotivo()
    {
        var t = NuevoTelefono();
        await RegistrarYCrearNegocioAsync(t);
        var p = await CrearProductoAsync(t, "Alfajor", "1000");
        await t.Api.CrearPedidoAsync(new PedidoCrearDto("Ana", "2954000111", [new(p.Id, 1)]));

        var form = new ProductoFormViewModel(t.Api, t.Nav, t.Dialogos) { Id = p.Id, Nombre = "Alfajor" };
        await form.EliminarCommand.ExecuteAsync(null);

        Assert.Contains("Desactivalo", form.Error);
        Assert.Equal(HttpStatusCode.Conflict, (await Assert.ThrowsAsync<ApiException>(() => t.Api.EliminarProductoAsync(p.Id))).Status);
    }

    // ───── pedidos ─────
    [Fact]
    public async Task NuevoPedido_RespetaElStock_CalculaElTotalEstimado_YCreaElPedido()
    {
        var t = NuevoTelefono();
        await RegistrarYCrearNegocioAsync(t, envios: true);
        var alfajor = await CrearProductoAsync(t, "Alfajor", "1000", "3");
        await t.Api.CrearZonaAsync(new ZonaGuardarDto("Centro", 300));
        await t.Api.CrearCuponAsync(new CuponGuardarDto("PROMO10", TipoCupon.Porcentaje, 10));

        var vm = new NuevoPedidoViewModel(t.Api, t.Sesion, t.Nav);
        await vm.CargarCommand.ExecuteAsync(null);
        Assert.Single(vm.Zonas);

        var linea = vm.Lineas.Single();
        for (var i = 0; i < 5; i++) vm.SumarCommand.Execute(linea); // intenta pedir 5 pero hay 3
        Assert.Equal(3, linea.Cantidad);
        vm.RestarCommand.Execute(linea);
        Assert.Equal(2, linea.Cantidad);

        vm.Zona = vm.Zonas.Single();
        Assert.Equal(2300m, vm.TotalEstimado); // 2 x 1000 + 300 de envío

        await vm.CrearCommand.ExecuteAsync(null); // faltan los datos del cliente
        Assert.Contains("cliente", vm.Error);

        vm.ClienteNombre = "Ana"; vm.ClienteTelefono = "2954000111"; vm.Direccion = "Calle 123"; vm.CodigoCupon = "promo10";
        var volverAntes = t.Nav.Volver; // (crear el producto ya había vuelto una vez)
        await vm.CrearCommand.ExecuteAsync(null);

        Assert.Null(vm.Error);
        var pedido = (await t.Api.PedidosAsync()).Single();
        Assert.Equal(2100m, pedido.Total); // 2000 - 200 (cupón 10 %) + 300
        Assert.Equal(volverAntes + 1, t.Nav.Volver);
        Assert.Equal((Rutas.PedidoDetalle, (int?)pedido.Id, false), t.Nav.Visitas.Last());
        Assert.Equal(1, (await t.Api.ProductoAsync(alfajor.Id)).Stock);
    }

    [Fact]
    public async Task PedidoDetalle_CambiaDeEstado_PidiendoConfirmacionAlCancelar()
    {
        var t = NuevoTelefono();
        await RegistrarYCrearNegocioAsync(t);
        var p = await CrearProductoAsync(t, "Alfajor", "1000", "10");
        var pedido = await t.Api.CrearPedidoAsync(new PedidoCrearDto("Ana", "2954000111", [new(p.Id, 4)]));

        var detalle = new PedidoDetalleViewModel(t.Api, t.Dialogos) { Id = pedido.Id };
        await detalle.CargarCommand.ExecuteAsync(null);
        Assert.Equal([EstadoPedido.Confirmado, EstadoPedido.Pagado, EstadoPedido.Cancelado], detalle.EstadosPermitidos);

        await detalle.CambiarEstadoCommand.ExecuteAsync(EstadoPedido.Pagado);
        Assert.Equal(EstadoPedido.Pagado, detalle.Pedido!.Estado);
        Assert.Equal([EstadoPedido.Entregado, EstadoPedido.Cancelado], detalle.EstadosPermitidos);

        t.Dialogos.Respuesta = false; // el usuario se arrepiente
        await detalle.CambiarEstadoCommand.ExecuteAsync(EstadoPedido.Cancelado);
        Assert.Equal(EstadoPedido.Pagado, detalle.Pedido.Estado);
        Assert.Equal(6, (await t.Api.ProductoAsync(p.Id)).Stock);

        t.Dialogos.Respuesta = true;
        await detalle.CambiarEstadoCommand.ExecuteAsync(EstadoPedido.Cancelado);
        Assert.Equal(EstadoPedido.Cancelado, detalle.Pedido.Estado);
        Assert.Empty(detalle.EstadosPermitidos);
        Assert.Equal(10, (await t.Api.ProductoAsync(p.Id)).Stock); // el stock volvió
    }

    [Fact]
    public async Task Pedidos_FiltraPorEstado_YElInicioMuestraElResumen()
    {
        var t = NuevoTelefono();
        await RegistrarYCrearNegocioAsync(t);
        var p = await CrearProductoAsync(t, "Gaseosa", "2200");
        var a = await t.Api.CrearPedidoAsync(new PedidoCrearDto("Ana", "2954000111", [new(p.Id, 1)]));
        await t.Api.CrearPedidoAsync(new PedidoCrearDto("Beto", "2954000222", [new(p.Id, 2)]));
        await t.Api.CambiarEstadoAsync(a.Id, EstadoPedido.Pagado);

        var lista = new PedidosViewModel(t.Api, t.Nav);
        await lista.CargarCommand.ExecuteAsync(null);
        Assert.Equal(2, lista.Pedidos.Count);
        await lista.FiltrarCommand.ExecuteAsync(EstadoPedido.Pagado);
        Assert.Equal("Ana", lista.Pedidos.Single().ClienteNombre);
        await lista.FiltrarCommand.ExecuteAsync(EstadoPedido.Entregado);
        Assert.True(lista.SinPedidos);

        var inicio = new InicioViewModel(t.Api, t.Sesion, t.Nav);
        await inicio.CargarCommand.ExecuteAsync(null);
        Assert.Equal("$6.600", inicio.VentasMes); // 1 x 2200 + 2 x 2200
        Assert.Equal(1, inicio.Cantidad("Pendiente"));
        Assert.Equal(2, inicio.UltimosPedidos.Count);
        Assert.StartsWith("Plan Prueba · te quedan", inicio.ResumenPlan);
        Assert.Equal("Kiosco Test", inicio.NombreNegocio);
    }

    [Fact]
    public async Task Inicio_AvisaCuandoSeAcercaAlLimiteDelPlanGratis()
    {
        var t = NuevoTelefono();
        await RegistrarYCrearNegocioAsync(t);
        await t.Api.CambiarPlanAsync(new CambiarPlanDto("Gratis"));
        for (var i = 0; i < 8; i++) await CrearProductoAsync(t, $"Producto {i}", "100");

        var inicio = new InicioViewModel(t.Api, t.Sesion, t.Nav);
        await inicio.CargarCommand.ExecuteAsync(null);
        Assert.Equal("Usaste 8 de 10 productos de tu plan.", inicio.AvisoPlan);

        await CrearProductoAsync(t, "Producto 8", "100");
        await CrearProductoAsync(t, "Producto 9", "100");
        var form = new ProductoFormViewModel(t.Api, t.Nav, t.Dialogos) { Nombre = "Excedido", Precio = "100" };
        await form.GuardarCommand.ExecuteAsync(null);
        Assert.Contains("límite de 10 productos", form.Error); // el mensaje del servidor llega a la pantalla
    }

    // ───── cliente HTTP ─────
    [Fact]
    public async Task SinConexion_ElErrorEsAmigable()
    {
        var sesion = new Sesion(new AlmacenEnMemoria());
        var api = new ApiClient(new HttpClient(new FallaDeRed()) { BaseAddress = new Uri("http://localhost/") }, sesion);

        var ex = await Assert.ThrowsAsync<ApiException>(() => api.LoginAsync(new LoginDto("a@a.com", "x")));
        Assert.Contains("No hay conexión", ex.Message);
        Assert.Equal(0, (int)ex.Status);
    }

    private class FallaDeRed : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("sin red");
    }

    // ───── utilidades ─────
    [Theory]
    [InlineData(6700, "$6.700")]
    [InlineData(1234567.5, "$1.234.567,50")]
    [InlineData(0, "$0")]
    public void Formato_Dinero(double valor, string esperado) => Assert.Equal(esperado, Formato.Dinero((decimal)valor));

    [Theory]
    [InlineData("1500", 1500)]
    [InlineData("1500,50", 1500.50)]
    [InlineData("1500.50", 1500.50)]
    [InlineData("1.500,50", 1500.50)]
    [InlineData("$ 2200", 2200)]
    public void Formato_ParseaNumerosAlEstiloArgentino(string texto, double esperado)
    {
        Assert.True(Formato.TryParseNumero(texto, out var valor));
        Assert.Equal((decimal)esperado, valor);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData(null)]
    public void Formato_RechazaTextoInvalido(string? texto) => Assert.False(Formato.TryParseNumero(texto, out _));
}
