using VentaYa.App.Core.Models;
using VentaYa.App.Core.Services;
using VentaYa.App.Core.ViewModels;

namespace VentaYa.Tests.App;

/// <summary>Pruebas de la pestaña "Más": cupones, envíos, respuestas, equipo, perfil, plan y cierre de sesión.</summary>
public class AppMasTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _fabrica;
    public AppMasTests(ApiFactory fabrica) => _fabrica = fabrica;

    private Telefono NuevoTelefono() => new(_fabrica.CreateClient());

    private static async Task<Telefono> DuenioConNegocioAsync(ApiFactory f)
    {
        var t = new Telefono(f.CreateClient());
        var email = $"d{Guid.NewGuid():N}@test.com";
        await new RegistroViewModel(t.Api, t.Sesion, t.Nav) { Nombre = "Dueña", Email = email, Password = "secreta123", Confirmacion = "secreta123" }
            .CrearCuentaCommand.ExecuteAsync(null);
        await new OnboardingViewModel(t.Api, t.Sesion, t.Nav) { Nombre = "Kiosco Más", Telefono = "2954111222", EnvioHabilitado = true }
            .CrearNegocioCommand.ExecuteAsync(null);
        return t;
    }

    [Fact]
    public async Task Cupones_Crear_Editar_ValidarYEliminar()
    {
        var t = await DuenioConNegocioAsync(_fabrica);
        var vm = new CuponesViewModel(t.Api, t.Dialogos) { Codigo = "promo10", Valor = "150" };

        await vm.GuardarCommand.ExecuteAsync(null);
        Assert.Equal("El porcentaje no puede superar 100.", vm.Error);

        vm.Valor = "10";
        await vm.GuardarCommand.ExecuteAsync(null);
        Assert.Null(vm.Error);
        Assert.Equal("PROMO10", vm.Cupones.Single().Codigo);
        Assert.Equal("", vm.Codigo); // el formulario se limpió

        vm.Codigo = "promo10"; vm.Valor = "5";
        await vm.GuardarCommand.ExecuteAsync(null);
        Assert.Contains("Ya existe un cupón", vm.Error); // mensaje del servidor

        vm.EditarCommand.Execute(vm.Cupones.Single());
        vm.Valor = "20";
        await vm.GuardarCommand.ExecuteAsync(null);
        Assert.Equal(20m, vm.Cupones.Single().Valor);

        await vm.EliminarCommand.ExecuteAsync(vm.Cupones.Single());
        Assert.Empty(vm.Cupones);
    }

    [Fact]
    public async Task Zonas_Crear_Editar_YEliminar()
    {
        var t = await DuenioConNegocioAsync(_fabrica);
        var vm = new ZonasViewModel(t.Api, t.Dialogos) { Nombre = "Centro", Precio = "abc" };

        await vm.GuardarCommand.ExecuteAsync(null);
        Assert.Contains("válido", vm.Error);

        vm.Precio = "350,50";
        await vm.GuardarCommand.ExecuteAsync(null);
        Assert.Equal(350.50m, vm.Zonas.Single().Precio);

        vm.EditarCommand.Execute(vm.Zonas.Single());
        vm.Activa = false;
        await vm.GuardarCommand.ExecuteAsync(null);
        Assert.False(vm.Zonas.Single().Activa);

        await vm.EliminarCommand.ExecuteAsync(vm.Zonas.Single());
        Assert.Empty(vm.Zonas);
    }

    [Fact]
    public async Task Respuestas_ReemplazanLasVariablesDeUnPedido()
    {
        var t = await DuenioConNegocioAsync(_fabrica);
        var producto = await t.Api.CrearProductoAsync(new ProductoGuardarDto("Gaseosa", null, 2200, null));
        var pedido = await t.Api.CrearPedidoAsync(new PedidoCrearDto("Ana", "2954000111", [new(producto.Id, 3)]));

        var vm = new RespuestasViewModel(t.Api, t.Dialogos) { Titulo = "Cobro", Contenido = "Hola {nombre}, tu total es {total}" };
        await vm.GuardarCommand.ExecuteAsync(null);

        var texto = await vm.TextoParaCompartirAsync(vm.Respuestas.Single(), pedido.Id);
        Assert.Equal("Hola Ana, tu total es $6.600", texto);
    }

    [Fact]
    public async Task Respuestas_ElPlanGratisLimitaA5_YElMensajeLlegaALaPantalla()
    {
        var t = await DuenioConNegocioAsync(_fabrica);
        await t.Api.CambiarPlanAsync(new CambiarPlanDto("Gratis"));
        var vm = new RespuestasViewModel(t.Api, t.Dialogos);
        for (var i = 0; i < 5; i++)
        {
            vm.Titulo = $"R{i}"; vm.Contenido = "texto";
            await vm.GuardarCommand.ExecuteAsync(null);
        }
        vm.Titulo = "Sexta"; vm.Contenido = "texto";
        await vm.GuardarCommand.ExecuteAsync(null);
        Assert.Contains("límite de 5 respuestas", vm.Error);
    }

    [Fact]
    public async Task Equipo_Invitar_AceptarComoEmpleado_YRevocar()
    {
        var duenio = await DuenioConNegocioAsync(_fabrica);
        var equipo = new EquipoViewModel(duenio.Api, duenio.Dialogos) { EmailInvitado = "sin-arroba" };
        await equipo.InvitarCommand.ExecuteAsync(null);
        Assert.Contains("email válido", equipo.Error);

        equipo.EmailInvitado = "empleado@test.com";
        await equipo.InvitarCommand.ExecuteAsync(null);
        Assert.False(string.IsNullOrEmpty(equipo.CodigoGenerado));
        Assert.Single(equipo.Miembros);

        // El empleado se registra y, en vez de crear un negocio, usa el código
        var emp = NuevoTelefono();
        await new RegistroViewModel(emp.Api, emp.Sesion, emp.Nav)
        { Nombre = "Empleado", Email = $"e{Guid.NewGuid():N}@test.com", Password = "secreta123", Confirmacion = "secreta123" }
            .CrearCuentaCommand.ExecuteAsync(null);
        var onboarding = new OnboardingViewModel(emp.Api, emp.Sesion, emp.Nav);
        await onboarding.AceptarInvitacionCommand.ExecuteAsync(null);
        Assert.Contains("Pegá el código", onboarding.Error);
        onboarding.CodigoInvitacion = equipo.CodigoGenerado!;
        await onboarding.AceptarInvitacionCommand.ExecuteAsync(null);

        Assert.Equal(Rutas.Inicio, emp.Nav.Ultima);
        Assert.False(emp.Sesion.EsPropietario);
        var menu = new MasViewModel(emp.Sesion, emp.Nav, emp.Dialogos);
        Assert.False(menu.EsPropietario); // el empleado no ve Equipo, Perfil ni Plan
        Assert.Equal("Kiosco Más", menu.NombreNegocio);

        await equipo.CargarCommand.ExecuteAsync(null);
        Assert.True(equipo.Miembros.Single().Aceptado);
        await equipo.RevocarCommand.ExecuteAsync(equipo.Miembros.Single());
        Assert.Empty(equipo.Miembros);
        await Assert.ThrowsAsync<ApiException>(() => emp.Api.ProductosAsync());
    }

    [Fact]
    public async Task Perfil_GuardaLosCambios_YPlan_CambiaLosLimites()
    {
        var t = await DuenioConNegocioAsync(_fabrica);

        var perfil = new PerfilViewModel(t.Api, t.Sesion, t.Dialogos);
        Assert.Equal("Kiosco Más", perfil.Nombre);
        perfil.Nombre = "Kiosco Renovado"; perfil.Telefono = "123";
        await perfil.GuardarCommand.ExecuteAsync(null);
        Assert.Contains("teléfono", perfil.Error);
        perfil.Telefono = "2954999999";
        await perfil.GuardarCommand.ExecuteAsync(null);
        Assert.Equal("Kiosco Renovado", t.Sesion.Negocio!.Nombre);
        Assert.Single(t.Dialogos.Avisos);

        var plan = new PlanViewModel(t.Api, t.Sesion);
        await plan.CambiarCommand.ExecuteAsync("Gratis");
        Assert.Equal("Gratis", plan.PlanActual);
        Assert.Equal(10, plan.Uso!.MaxProductos);
        await plan.CambiarCommand.ExecuteAsync("Pago");
        Assert.Null(plan.Uso!.MaxProductos);
    }

    [Fact]
    public async Task CerrarSesion_PideConfirmacion_YVuelveAlLogin()
    {
        var t = await DuenioConNegocioAsync(_fabrica);
        var menu = new MasViewModel(t.Sesion, t.Nav, t.Dialogos);
        var cerradas = 0;
        t.Sesion.SesionCerrada += () => cerradas++;

        t.Dialogos.Respuesta = false;
        await menu.CerrarSesionCommand.ExecuteAsync(null);
        Assert.True(t.Sesion.EstaLogueado);

        t.Dialogos.Respuesta = true;
        await menu.CerrarSesionCommand.ExecuteAsync(null);
        Assert.False(t.Sesion.EstaLogueado);
        Assert.Equal((Rutas.Login, null, true), t.Nav.Visitas.Last());
        Assert.Equal(1, cerradas);
    }
}
