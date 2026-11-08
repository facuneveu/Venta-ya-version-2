using VentaYa.App.Core.Services;

namespace VentaYa.App;

public partial class App : Application
{
    public App(Sesion sesion, INavegacion navegacion)
    {
        InitializeComponent();

        // Si el token vence (o se cierra la sesión) en cualquier pantalla, se vuelve al login.
        sesion.SesionCerrada += () => MainThread.BeginInvokeOnMainThread(async () =>
        {
            try { await navegacion.IrAAsync(Rutas.Login, raiz: true); } catch { /* la app todavía está arrancando */ }
        });
    }

    protected override Window CreateWindow(IActivationState? activationState) => new Window(new AppShell());
}
