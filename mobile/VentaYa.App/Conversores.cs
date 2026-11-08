using System.Globalization;
using VentaYa.App.Core.Models;
using VentaYa.App.Core.Services;

namespace VentaYa.App;

/// <summary>decimal → "$6.700"</summary>
public class DineroConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is decimal d ? Formato.Dinero(d) : "";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>Estado del pedido → color de la etiqueta.</summary>
public class EstadoColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        EstadoPedido.Pendiente => Color.FromArgb("#B26A00"),
        EstadoPedido.Confirmado => Color.FromArgb("#1F4E79"),
        EstadoPedido.Pagado => Color.FromArgb("#2E7D5B"),
        EstadoPedido.Entregado => Color.FromArgb("#5C6B73"),
        EstadoPedido.Cancelado => Color.FromArgb("#B3261E"),
        _ => Colors.Gray,
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>Fecha UTC de la API → hora local del teléfono "08/11 19:30".</summary>
public class FechaLocalConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is DateTime f ? DateTime.SpecifyKind(f, DateTimeKind.Utc).ToLocalTime().ToString("dd/MM HH:mm") : "";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>int? → "10" o "ilimitado" (límites del plan).</summary>
public class LimiteConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int n ? n.ToString() : "ilimitado";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>"/uploads/productos/x.jpg" (ruta relativa de la API) → imagen con la URL completa del servidor.</summary>
public class ImagenUrlConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string ruta || string.IsNullOrWhiteSpace(ruta)) return null;
        var url = ruta.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? ruta : Configuracion.ApiBaseUrl.TrimEnd('/') + ruta;
        return ImageSource.FromUri(new Uri(url));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>Cualquier objeto no nulo → true (para mostrar u ocultar secciones).</summary>
public class NoNuloConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not null;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>int → true si es mayor a 0.</summary>
public class MayorACeroConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is int n && n > 0;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
