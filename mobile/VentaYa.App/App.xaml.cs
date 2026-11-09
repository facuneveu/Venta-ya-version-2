using VentaYa.App.Core.Services;

namespace VentaYa.App;

public partial class App : Application
{
    public App(Sesion sesion, INavegacion navegacion)
    {
        InitializeComponent();

        // La interfaz está diseñada en claro: se ignora el modo oscuro del teléfono (si no, el texto queda blanco sobre fondo claro).
        UserAppTheme = AppTheme.Light;

        // Si el token vence (o se cierra la sesión) en cualquier pantalla, se vuelve al login.
        sesion.SesionCerrada += () => MainThread.BeginInvokeOnMainThread(async () =>
        {
            try { await navegacion.IrAAsync(Rutas.Login, raiz: true); } catch { /* la app todavía está arrancando */ }
        });
    }

    protected override Window CreateWindow(IActivationState? activationState) => new Window(new AppShell());
}
