using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VentaYa.App.Core.Models;
using VentaYa.App.Core.Services;

namespace VentaYa.App.Core.ViewModels;

/// <summary>Pantalla de arranque: con sesión guardada entra directo; si no, va al login.</summary>
public partial class SplashViewModel(IApiClient api, Sesion sesion, INavegacion nav) : BaseViewModel
{
    [RelayCommand]
    private async Task IniciarAsync()
    {
        await sesion.RestaurarAsync();
        if (!sesion.EstaLogueado) { await nav.IrAAsync(Rutas.Login, raiz: true); return; }
        try
        {
            await FlujoAcceso.EntrarAsync(api, sesion, nav);
        }
        catch (ApiException)
        {
            sesion.Cerrar();
            await nav.IrAAsync(Rutas.Login, raiz: true);
        }
    }
}

public partial class LoginViewModel(IApiClient api, Sesion sesion, INavegacion nav) : BaseViewModel
{
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _password = "";

    [RelayCommand]
    private Task IngresarAsync() => EjecutarAsync(async () =>
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrEmpty(Password))
        {
            Error = "Completá tu email y tu contraseña.";
            return;
        }
        var respuesta = await api.LoginAsync(new LoginDto(Email.Trim(), Password));
        await sesion.IniciarAsync(respuesta);
        Password = "";
        await FlujoAcceso.EntrarAsync(api, sesion, nav);
    });

    [RelayCommand]
    private Task IrARegistroAsync() => nav.IrAAsync(Rutas.Registro);
}

public partial class RegistroViewModel(IApiClient api, Sesion sesion, INavegacion nav) : BaseViewModel
{
    [ObservableProperty] private string _nombre = "";
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string _confirmacion = "";

    [RelayCommand]
    private Task CrearCuentaAsync() => EjecutarAsync(async () =>
    {
        if (string.IsNullOrWhiteSpace(Nombre) || string.IsNullOrWhiteSpace(Email))
        {
            Error = "Completá tu nombre y tu email.";
            return;
        }
        if (Password.Length < 6) { Error = "La contraseña debe tener al menos 6 caracteres."; return; }
        if (Password != Confirmacion) { Error = "Las contraseñas no coinciden."; return; }

        var respuesta = await api.RegistroAsync(new RegistroDto(Nombre.Trim(), Email.Trim(), Password));
        await sesion.IniciarAsync(respuesta);
        await FlujoAcceso.EntrarAsync(api, sesion, nav); // cuenta nueva → pasa por el onboarding
    });

    [RelayCommand]
    private Task VolverAsync() => nav.VolverAsync();
}

/// <summary>Onboarding: el usuario recién registrado crea su negocio.</summary>
public partial class OnboardingViewModel(IApiClient api, Sesion sesion, INavegacion nav) : BaseViewModel
{
    [ObservableProperty] private string _nombre = "";
    [ObservableProperty] private string _telefono = "";
    [ObservableProperty] private string _direccion = "";
    [ObservableProperty] private string _descripcion = "";
    [ObservableProperty] private bool _envioHabilitado;

    [RelayCommand]
    private Task CrearNegocioAsync() => EjecutarAsync(async () =>
    {
        if (string.IsNullOrWhiteSpace(Nombre)) { Error = "Ponele un nombre a tu negocio."; return; }
        if (Telefono.Trim().Length < 6) { Error = "Ingresá un teléfono de contacto válido."; return; }

        sesion.Negocio = await api.CrearNegocioAsync(new NegocioGuardarDto(
            Nombre.Trim(), Vacio(Descripcion), Telefono.Trim(), Vacio(Direccion), EnvioHabilitado));
        await nav.IrAAsync(Rutas.Inicio, raiz: true);
    });

    private static string? Vacio(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    // ───── Empleado: en lugar de crear un negocio, acepta la invitación del dueño ─────
    [ObservableProperty] private string _codigoInvitacion = "";

    [RelayCommand]
    private Task AceptarInvitacionAsync() => EjecutarAsync(async () =>
    {
        if (string.IsNullOrWhiteSpace(CodigoInvitacion)) { Error = "Pegá el código que te pasó el dueño del negocio."; return; }
        sesion.Negocio = await api.AceptarInvitacionAsync(CodigoInvitacion.Trim());
        await nav.IrAAsync(Rutas.Inicio, raiz: true);
    });
}
