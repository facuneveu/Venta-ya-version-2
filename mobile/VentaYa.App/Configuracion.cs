namespace VentaYa.App;

/// <summary>Dirección de la API. Se puede cambiar desde la pantalla de login (sin recompilar) y se guarda en el teléfono.</summary>
public static class Configuracion
{
#if DEBUG
    // Emulador de Android: 10.0.2.2 es la PC donde corre la API (dotnet run --urls http://0.0.0.0:5080).
    // Con un celular real: poné la IP de tu PC (ej. http://192.168.0.10:5080/) o la URL desplegada.
    public const string UrlEmulador = "http://10.0.2.2:5080/";

    // Celular real conectado a la misma WiFi que la PC de desarrollo: poné acá la IP de tu PC (ipconfig).
    public const string UrlCelularEnLan = "http://192.168.0.7:5080/";

    public static string UrlPorDefecto =>
        DeviceInfo.Current.DeviceType == DeviceType.Virtual ? UrlEmulador : UrlCelularEnLan;
#else
    public static string UrlPorDefecto => "https://TU-SITIO.runasp.net/";
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
