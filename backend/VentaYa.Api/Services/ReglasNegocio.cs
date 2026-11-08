using VentaYa.Api.Models;

namespace VentaYa.Api.Services;

/// <summary>Error de regla de negocio: el middleware lo convierte en { "error": "..." } con su código HTTP.</summary>
public class ReglaNegocioException(string mensaje, int status = 400) : Exception(mensaje)
{
    public int Status { get; } = status;
}

/// <summary>
/// Reglas de negocio puras (sin base de datos), fáciles de probar con tests unitarios.
/// </summary>
public static class ReglasNegocio
{
    /// <summary>Descuento de un cupón sobre el subtotal (nunca supera el subtotal).</summary>
    public static decimal CalcularDescuento(Cupon cupon, decimal subtotal) =>
        cupon.Tipo == TipoCupon.Porcentaje
            ? Math.Round(subtotal * cupon.Valor / 100m, 2)
            : Math.Min(cupon.Valor, subtotal);

    /// <summary>Devuelve el motivo por el que el cupón no se puede usar, o null si es válido.</summary>
    public static string? ValidarCupon(Cupon cupon, decimal subtotal, int usosActuales, DateTime ahora)
    {
        if (!cupon.Activo) return "El cupón no está activo";
        if (cupon.VenceEn is { } vence && vence <= ahora) return "El cupón está vencido";
        if (subtotal < cupon.MontoMinimo) return $"El pedido mínimo para este cupón es ${cupon.MontoMinimo:0.##}";
        if (cupon.UsosMaximos is { } max && usosActuales >= max) return "El cupón alcanzó su límite de usos";
        return null;
    }

    private static readonly Dictionary<EstadoPedido, EstadoPedido[]> Transiciones = new()
    {
        [EstadoPedido.Pendiente] = [EstadoPedido.Confirmado, EstadoPedido.Pagado, EstadoPedido.Cancelado],
        [EstadoPedido.Confirmado] = [EstadoPedido.Pagado, EstadoPedido.Entregado, EstadoPedido.Cancelado],
        [EstadoPedido.Pagado] = [EstadoPedido.Entregado, EstadoPedido.Cancelado],
        [EstadoPedido.Entregado] = [],
        [EstadoPedido.Cancelado] = [],
    };

    public static bool TransicionValida(EstadoPedido desde, EstadoPedido hacia) =>
        Transiciones[desde].Contains(hacia);

    /// <summary>La prueba gratuita de 14 días vence y el negocio vuelve al plan Gratis.</summary>
    public static bool PruebaVencida(Plan plan, DateTime? trialExpiraEn, DateTime ahora) =>
        plan.Id == PlanesIds.Prueba && (trialExpiraEn is null || trialExpiraEn <= ahora);

    /// <summary>
    /// Reemplaza las variables de una respuesta rápida: {nombre}, {total} y {link}.
    /// Si falta el dato de una variable, se deja tal cual para que el dueño la complete.
    /// El total se muestra al estilo argentino: $6.700,50.
    /// </summary>
    public static string RenderizarRespuesta(string contenido, string? nombre, decimal? total, string? link)
    {
        var texto = contenido;
        if (nombre is not null) texto = texto.Replace("{nombre}", nombre, StringComparison.OrdinalIgnoreCase);
        if (link is not null) texto = texto.Replace("{link}", link, StringComparison.OrdinalIgnoreCase);
        if (total is { } t)
        {
            var formato = t.ToString("#,##0.##", System.Globalization.CultureInfo.InvariantCulture)
                .Replace(",", "#").Replace(".", ",").Replace("#", ".");
            texto = texto.Replace("{total}", "$" + formato, StringComparison.OrdinalIgnoreCase);
        }
        return texto;
    }

    /// <summary>Convierte un nombre en un slug de URL: "Café Ñandú" -> "cafe-nandu".</summary>
    public static string GenerarSlug(string nombre)
    {
        var normalizado = nombre.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var c in normalizado)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
            if (char.IsAsciiLetterOrDigit(c)) sb.Append(c);
            else if (char.IsWhiteSpace(c) || c == '-') sb.Append('-');
        }
        var slug = System.Text.RegularExpressions.Regex.Replace(sb.ToString(), "-+", "-").Trim('-');
        return slug.Length == 0 ? "negocio" : slug;
    }
}
