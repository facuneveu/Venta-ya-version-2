using VentaYa.App.Core.Services;
using VentaYa.App.Pages;

namespace VentaYa.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Pantallas que se apilan sobre las pestañas (tienen botón "atrás")
        Routing.RegisterRoute(Rutas.Registro, typeof(RegistroPage));
        Routing.RegisterRoute(Rutas.PedidoDetalle, typeof(PedidoDetallePage));
        Routing.RegisterRoute(Rutas.NuevoPedido, typeof(NuevoPedidoPage));
        Routing.RegisterRoute(Rutas.ProductoForm, typeof(ProductoFormPage));
        Routing.RegisterRoute(Rutas.Cupones, typeof(CuponesPage));
        Routing.RegisterRoute(Rutas.Zonas, typeof(ZonasPage));
        Routing.RegisterRoute(Rutas.Respuestas, typeof(RespuestasPage));
        Routing.RegisterRoute(Rutas.Equipo, typeof(EquipoPage));
        Routing.RegisterRoute(Rutas.Perfil, typeof(PerfilPage));
        Routing.RegisterRoute(Rutas.Plan, typeof(PlanPage));
    }
}
