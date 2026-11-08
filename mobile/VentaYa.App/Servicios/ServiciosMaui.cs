using VentaYa.App.Core.Services;

namespace VentaYa.App.Servicios;

/// <summary>Guarda el token en el almacén seguro del teléfono (Keystore en Android).</summary>
public class AlmacenSecureStorage : IAlmacenSeguro
{
    private const string Clave = "ventaya_token";

    public async Task<string?> LeerAsync()
    {
        try { return await SecureStorage.Default.GetAsync(Clave); }
        catch { return null; } // algunos dispositivos fallan al leer: se trata como "sin sesión"
    }

    public async Task GuardarAsync(string token)
    {
        try { await SecureStorage.Default.SetAsync(Clave, token); }
        catch { /* si no se puede guardar, la sesión dura mientras la app esté abierta */ }
    }

    public Task BorrarAsync()
    {
        try { SecureStorage.Default.Remove(Clave); } catch { }
        return Task.CompletedTask;
    }
}

/// <summary>Navegación con Shell. Las pantallas "raíz" reemplazan toda la pila; el resto se apila con botón atrás.</summary>
public class ShellNavegacion : INavegacion
{
    private static readonly Dictionary<string, string> Raices = new()
    {
        [Rutas.Login] = "//login",
        [Rutas.Onboarding] = "//onboarding",
        [Rutas.Inicio] = "//principal/inicio",
        [Rutas.Pedidos] = "//principal/pedidos",
        [Rutas.Catalogo] = "//principal/catalogo",
        [Rutas.Mas] = "//principal/mas",
    };

    public Task IrAAsync(string ruta, int? id = null, bool raiz = false)
    {
        var destino = raiz ? (Raices.TryGetValue(ruta, out var r) ? r : "//" + ruta) : ruta;
        if (id is { } valor) destino += $"?id={valor}";
        return MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync(destino));
    }

    public Task VolverAsync() => MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync(".."));
}

public class DialogosMaui : IDialogos
{
    private static Page? Pagina => Shell.Current?.CurrentPage ?? Application.Current?.Windows.FirstOrDefault()?.Page;

    public Task AvisarAsync(string titulo, string mensaje) =>
        MainThread.InvokeOnMainThreadAsync(() => Pagina!.DisplayAlertAsync(titulo, mensaje, "Aceptar"));

    public Task<bool> ConfirmarAsync(string titulo, string mensaje, string aceptar = "Sí", string cancelar = "No") =>
        MainThread.InvokeOnMainThreadAsync(() => Pagina!.DisplayAlertAsync(titulo, mensaje, aceptar, cancelar));
}
