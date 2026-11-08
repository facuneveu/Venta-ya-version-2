using Microsoft.Extensions.Logging;
using VentaYa.App.Core.Services;
using VentaYa.App.Core.ViewModels;
using VentaYa.App.Pages;
using VentaYa.App.Servicios;

namespace VentaYa.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        var s = builder.Services;

        // ── Servicios (una sola instancia para toda la app) ──
        s.AddSingleton<IAlmacenSeguro, AlmacenSecureStorage>();
        s.AddSingleton<Sesion>();
        s.AddSingleton(_ => new HttpClient { BaseAddress = new Uri(Configuracion.ApiBaseUrl), Timeout = TimeSpan.FromSeconds(25) });
        s.AddSingleton<IApiClient, ApiClient>();
        s.AddSingleton<INavegacion, ShellNavegacion>();
        s.AddSingleton<IDialogos, DialogosMaui>();

        // ── ViewModels y pantallas (una instancia nueva cada vez que se abren) ──
        s.AddTransient<SplashViewModel>();       s.AddTransient<SplashPage>();
        s.AddTransient<LoginViewModel>();        s.AddTransient<LoginPage>();
        s.AddTransient<RegistroViewModel>();     s.AddTransient<RegistroPage>();
        s.AddTransient<OnboardingViewModel>();   s.AddTransient<OnboardingPage>();
        s.AddTransient<InicioViewModel>();       s.AddTransient<InicioPage>();
        s.AddTransient<PedidosViewModel>();      s.AddTransient<PedidosPage>();
        s.AddTransient<PedidoDetalleViewModel>(); s.AddTransient<PedidoDetallePage>();
        s.AddTransient<NuevoPedidoViewModel>();  s.AddTransient<NuevoPedidoPage>();
        s.AddTransient<CatalogoViewModel>();     s.AddTransient<CatalogoPage>();
        s.AddTransient<ProductoFormViewModel>(); s.AddTransient<ProductoFormPage>();
        s.AddTransient<MasViewModel>();          s.AddTransient<MasPage>();
        s.AddTransient<CuponesViewModel>();      s.AddTransient<CuponesPage>();
        s.AddTransient<ZonasViewModel>();        s.AddTransient<ZonasPage>();
        s.AddTransient<RespuestasViewModel>();   s.AddTransient<RespuestasPage>();
        s.AddTransient<EquipoViewModel>();       s.AddTransient<EquipoPage>();
        s.AddTransient<PerfilViewModel>();       s.AddTransient<PerfilPage>();
        s.AddTransient<PlanViewModel>();         s.AddTransient<PlanPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
