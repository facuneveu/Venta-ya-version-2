using System.Globalization;
using VentaYa.App.Core.Models;

namespace VentaYa.App.Core.Services;

/// <summary>Nombres de las rutas de pantalla (en MAUI se registran en el Shell).</summary>
public static class Rutas
{
    public const string Login = "login";
    public const string Registro = "registro";
    public const string Onboarding = "onboarding";
    public const string Inicio = "inicio";
    public const string Pedidos = "pedidos";
    public const string PedidoDetalle = "pedido-detalle";
    public const string NuevoPedido = "nuevo-pedido";
    public const string Catalogo = "catalogo";
    public const string ProductoForm = "producto-form";
    public const string Mas = "mas";
    public const string Cupones = "cupones";
    public const string Zonas = "zonas";
    public const string Respuestas = "respuestas";
    public const string Equipo = "equipo";
    public const string Perfil = "perfil";
    public const string Plan = "plan";
}

/// <summary>Navegación entre pantallas. En MAUI la implementa el Shell; en los tests, un doble.</summary>
public interface INavegacion
{
    /// <summary>Va a una pantalla. 'raiz' la convierte en pantalla principal (sin botón atrás).</summary>
    Task IrAAsync(string ruta, int? id = null, bool raiz = false);
    Task VolverAsync();
}

/// <summary>Avisos y confirmaciones al usuario (en MAUI: DisplayAlert).</summary>
public interface IDialogos
{
    Task AvisarAsync(string titulo, string mensaje);
    Task<bool> ConfirmarAsync(string titulo, string mensaje, string aceptar = "Sí", string cancelar = "No");
}

/// <summary>Decide a qué pantalla ir después de iniciar sesión o abrir la app.</summary>
public static class FlujoAcceso
{
    public static async Task EntrarAsync(IApiClient api, Sesion sesion, INavegacion nav)
    {
        var yo = await api.YoAsync();
        sesion.Usuario = yo.Usuario;
        sesion.Negocio = yo.Negocio;
        await nav.IrAAsync(yo.Negocio is null ? Rutas.Onboarding : Rutas.Inicio, raiz: true);
    }
}

/// <summary>Estados permitidos de un pedido: igual que la tabla de transiciones del backend.</summary>
public static class ReglasEstado
{
    public static IReadOnlyList<EstadoPedido> Siguientes(EstadoPedido actual) => actual switch
    {
        EstadoPedido.Pendiente => [EstadoPedido.Confirmado, EstadoPedido.Pagado, EstadoPedido.Cancelado],
        EstadoPedido.Confirmado => [EstadoPedido.Pagado, EstadoPedido.Entregado, EstadoPedido.Cancelado],
        EstadoPedido.Pagado => [EstadoPedido.Entregado, EstadoPedido.Cancelado],
        _ => [],
    };
}

public static class Formato
{
    private static readonly CultureInfo Ar = new("es-AR");

    /// <summary>$6.700 (sin decimales si es entero, $6.700,50 si tiene centavos).</summary>
    public static string Dinero(decimal valor) =>
        "$" + (valor % 1 == 0 ? valor.ToString("#,##0", Ar) : valor.ToString("#,##0.00", Ar));

    /// <summary>Acepta "1500", "1500,50" o "1500.50". Devuelve false si no es un número válido.</summary>
    public static bool TryParseNumero(string? texto, out decimal valor)
    {
        valor = 0;
        if (string.IsNullOrWhiteSpace(texto)) return false;
        var limpio = texto.Trim().Replace("$", "").Replace(" ", "");
        if (limpio.Contains(',') && limpio.Contains('.')) limpio = limpio.Replace(".", ""); // 1.500,50 → 1500,50
        limpio = limpio.Replace(',', '.');
        return decimal.TryParse(limpio, NumberStyles.Number, CultureInfo.InvariantCulture, out valor);
    }

    public static string Estado(EstadoPedido e) => e.ToString();
}
