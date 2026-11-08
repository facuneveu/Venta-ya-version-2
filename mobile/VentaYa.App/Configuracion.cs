namespace VentaYa.App;

/// <summary>Dirección de la API. Se puede cambiar desde la pantalla de login (sin recompilar) y se guarda en el teléfono.</summary>
public static class Configuracion
{
#if DEBUG
    // Emulador de Android: 10.0.2.2 es la PC donde corre la API (dotnet run --urls http://0.0.0.0:5080).
    // Con un celular real: poné la IP de tu PC (ej. http://192.168.0.10:5080/) o la URL desplegada.
    public const string UrlPorDefecto = "http://10.0.2.2:5080/";
#else
    public const string UrlPorDefecto = "https://TU-SITIO.monsterasp.net/";
#endif

    private const string Clave = "api_url";

    public static string ApiBaseUrl => Preferences.Default.Get(Clave, UrlPorDefecto);

    public static void GuardarUrl(string url)
    {
        url = url.Trim();
        if (!url.EndsWith('/')) url += "/";
        Preferences.Default.Set(Clave, url);
    }

    public static bool UrlValida(string url) =>
        Uri.TryCreate(url.Trim(), UriKind.Absolute, out var u) && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);
}
