using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using VentaYa.App.Core.Models;

namespace VentaYa.App.Core.Services;

/// <summary>Error de la API con un mensaje listo para mostrar al usuario.</summary>
public class ApiException(string mensaje, HttpStatusCode status) : Exception(mensaje)
{
    public HttpStatusCode Status { get; } = status;
}

public interface IApiClient
{
    // Auth
    Task<AuthRespuestaDto> RegistroAsync(RegistroDto dto);
    Task<AuthRespuestaDto> LoginAsync(LoginDto dto);
    Task<YoDto> YoAsync();
    // Negocio
    Task<NegocioDto> CrearNegocioAsync(NegocioGuardarDto dto);
    Task<NegocioDto> EditarNegocioAsync(NegocioGuardarDto dto);
    Task<NegocioDto> CambiarPlanAsync(CambiarPlanDto dto);
    Task<PanelDto> PanelAsync();
    // Productos
    Task<List<ProductoDto>> ProductosAsync(string? busqueda = null, bool soloActivos = false);
    Task<ProductoDto> ProductoAsync(int id);
    Task<ProductoDto> CrearProductoAsync(ProductoGuardarDto dto);
    Task<ProductoDto> EditarProductoAsync(int id, ProductoGuardarDto dto);
    Task EliminarProductoAsync(int id);
    Task<ImagenDto> SubirImagenAsync(int productoId, Stream contenido, string nombreArchivo);
    Task EliminarImagenAsync(int productoId, int imagenId);
    // Pedidos
    Task<List<PedidoDto>> PedidosAsync(EstadoPedido? estado = null);
    Task<PedidoDto> PedidoAsync(int id);
    Task<PedidoDto> CrearPedidoAsync(PedidoCrearDto dto);
    Task<PedidoDto> CambiarEstadoAsync(int id, EstadoPedido estado);
    // Cupones
    Task<List<CuponDto>> CuponesAsync();
    Task<CuponDto> CrearCuponAsync(CuponGuardarDto dto);
    Task<CuponDto> EditarCuponAsync(int id, CuponGuardarDto dto);
    Task EliminarCuponAsync(int id);
    // Zonas de envío
    Task<List<ZonaDto>> ZonasAsync();
    Task<ZonaDto> CrearZonaAsync(ZonaGuardarDto dto);
    Task<ZonaDto> EditarZonaAsync(int id, ZonaGuardarDto dto);
    Task EliminarZonaAsync(int id);
    // Respuestas rápidas
    Task<List<RespuestaDto>> RespuestasAsync();
    Task<RespuestaDto> CrearRespuestaAsync(RespuestaGuardarDto dto);
    Task<RespuestaDto> EditarRespuestaAsync(int id, RespuestaGuardarDto dto);
    Task EliminarRespuestaAsync(int id);
    Task<RespuestaRenderizadaDto> RenderizarRespuestaAsync(int id, int? pedidoId = null);
    // Equipo
    Task<List<MiembroDto>> EquipoAsync();
    Task<MiembroDto> InvitarAsync(InvitarDto dto);
    Task RevocarAsync(int miembroId);
    Task<NegocioDto> AceptarInvitacionAsync(string token);
}

/// <summary>Cliente HTTP de la API REST de VentaYa. Agrega el JWT y traduce errores a ApiException.</summary>
public class ApiClient(HttpClient http, Sesion sesion) : IApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private sealed record Vacio;

    // ───── infraestructura ─────
    private async Task<T> EnviarAsync<T>(HttpMethod metodo, string ruta, object? cuerpo = null, HttpContent? contenido = null)
    {
        using var req = new HttpRequestMessage(metodo, ruta);
        if (sesion.Token is { } token) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (contenido is not null) req.Content = contenido;
        else if (cuerpo is not null) req.Content = JsonContent.Create(cuerpo, options: Json);

        HttpResponseMessage resp;
        try
        {
            resp = await http.SendAsync(req);
        }
        catch (HttpRequestException)
        {
            throw new ApiException("No hay conexión con el servidor. Revisá tu internet e intentá de nuevo.", 0);
        }
        catch (TaskCanceledException)
        {
            throw new ApiException("El servidor tardó demasiado en responder. Intentá de nuevo.", HttpStatusCode.RequestTimeout);
        }

        using (resp)
        {
            if (!resp.IsSuccessStatusCode) throw await CrearErrorAsync(resp);
            if (typeof(T) == typeof(Vacio) || resp.StatusCode == HttpStatusCode.NoContent) return default!;
            return (await resp.Content.ReadFromJsonAsync<T>(Json))!;
        }
    }

    private async Task<ApiException> CrearErrorAsync(HttpResponseMessage resp)
    {
        if (resp.StatusCode == HttpStatusCode.Unauthorized && sesion.Token is not null)
        {
            sesion.Cerrar(); // token vencido o inválido → la app vuelve al login
            return new ApiException("Tu sesión venció. Iniciá sesión de nuevo.", resp.StatusCode);
        }

        string mensaje = resp.StatusCode switch
        {
            HttpStatusCode.Forbidden => "No tenés permiso para hacer esto.",
            HttpStatusCode.NotFound => "No se encontró lo que buscabas.",
            _ => "Ocurrió un error inesperado. Intentá de nuevo.",
        };
        try
        {
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var raiz = doc.RootElement;
            if (raiz.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String)
                mensaje = error.GetString()!;                       // { "error": "..." } de las reglas de negocio
            else if (raiz.TryGetProperty("errors", out var errores) && errores.ValueKind == JsonValueKind.Object)
                mensaje = errores.EnumerateObject().SelectMany(p => p.Value.EnumerateArray())
                    .Select(e => e.GetString()).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
                    ?? "Revisá los datos ingresados."; // validación automática (400)
        }
        catch (JsonException) { /* respuesta sin JSON: se usa el mensaje por defecto */ }
        return new ApiException(mensaje, resp.StatusCode);
    }

    private Task<T> Get<T>(string ruta) => EnviarAsync<T>(HttpMethod.Get, ruta);
    private Task<T> Post<T>(string ruta, object? cuerpo = null) => EnviarAsync<T>(HttpMethod.Post, ruta, cuerpo);
    private Task<T> Put<T>(string ruta, object cuerpo) => EnviarAsync<T>(HttpMethod.Put, ruta, cuerpo);
    private Task Delete(string ruta) => EnviarAsync<Vacio>(HttpMethod.Delete, ruta);

    // ───── Auth ─────
    public Task<AuthRespuestaDto> RegistroAsync(RegistroDto dto) => Post<AuthRespuestaDto>("api/auth/registro", dto);
    public Task<AuthRespuestaDto> LoginAsync(LoginDto dto) => Post<AuthRespuestaDto>("api/auth/login", dto);
    public Task<YoDto> YoAsync() => Get<YoDto>("api/auth/yo");

    // ───── Negocio ─────
    public Task<NegocioDto> CrearNegocioAsync(NegocioGuardarDto dto) => Post<NegocioDto>("api/negocio", dto);
    public Task<NegocioDto> EditarNegocioAsync(NegocioGuardarDto dto) => Put<NegocioDto>("api/negocio", dto);
    public Task<NegocioDto> CambiarPlanAsync(CambiarPlanDto dto) => Put<NegocioDto>("api/negocio/plan", dto);
    public Task<PanelDto> PanelAsync() => Get<PanelDto>("api/panel");

    // ───── Productos ─────
    public Task<List<ProductoDto>> ProductosAsync(string? busqueda = null, bool soloActivos = false)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(busqueda)) query.Add("q=" + Uri.EscapeDataString(busqueda.Trim()));
        if (soloActivos) query.Add("soloActivos=true");
        return Get<List<ProductoDto>>("api/productos" + (query.Count > 0 ? "?" + string.Join("&", query) : ""));
    }
    public Task<ProductoDto> ProductoAsync(int id) => Get<ProductoDto>($"api/productos/{id}");
    public Task<ProductoDto> CrearProductoAsync(ProductoGuardarDto dto) => Post<ProductoDto>("api/productos", dto);
    public Task<ProductoDto> EditarProductoAsync(int id, ProductoGuardarDto dto) => Put<ProductoDto>($"api/productos/{id}", dto);
    public Task EliminarProductoAsync(int id) => Delete($"api/productos/{id}");

    public Task<ImagenDto> SubirImagenAsync(int productoId, Stream contenido, string nombreArchivo)
    {
        var form = new MultipartFormDataContent { { new StreamContent(contenido), "archivo", nombreArchivo } };
        return EnviarAsync<ImagenDto>(HttpMethod.Post, $"api/productos/{productoId}/imagenes", contenido: form);
    }
    public Task EliminarImagenAsync(int productoId, int imagenId) => Delete($"api/productos/{productoId}/imagenes/{imagenId}");

    // ───── Pedidos ─────
    public Task<List<PedidoDto>> PedidosAsync(EstadoPedido? estado = null) =>
        Get<List<PedidoDto>>("api/pedidos" + (estado is { } e ? $"?estado={e}" : ""));
    public Task<PedidoDto> PedidoAsync(int id) => Get<PedidoDto>($"api/pedidos/{id}");
    public Task<PedidoDto> CrearPedidoAsync(PedidoCrearDto dto) => Post<PedidoDto>("api/pedidos", dto);
    public Task<PedidoDto> CambiarEstadoAsync(int id, EstadoPedido estado) =>
        EnviarAsync<PedidoDto>(HttpMethod.Patch, $"api/pedidos/{id}/estado", new CambiarEstadoDto(estado));

    // ───── Cupones ─────
    public Task<List<CuponDto>> CuponesAsync() => Get<List<CuponDto>>("api/cupones");
    public Task<CuponDto> CrearCuponAsync(CuponGuardarDto dto) => Post<CuponDto>("api/cupones", dto);
    public Task<CuponDto> EditarCuponAsync(int id, CuponGuardarDto dto) => Put<CuponDto>($"api/cupones/{id}", dto);
    public Task EliminarCuponAsync(int id) => Delete($"api/cupones/{id}");

    // ───── Zonas de envío ─────
    public Task<List<ZonaDto>> ZonasAsync() => Get<List<ZonaDto>>("api/zonas-envio");
    public Task<ZonaDto> CrearZonaAsync(ZonaGuardarDto dto) => Post<ZonaDto>("api/zonas-envio", dto);
    public Task<ZonaDto> EditarZonaAsync(int id, ZonaGuardarDto dto) => Put<ZonaDto>($"api/zonas-envio/{id}", dto);
    public Task EliminarZonaAsync(int id) => Delete($"api/zonas-envio/{id}");

    // ───── Respuestas rápidas ─────
    public Task<List<RespuestaDto>> RespuestasAsync() => Get<List<RespuestaDto>>("api/respuestas-rapidas");
    public Task<RespuestaDto> CrearRespuestaAsync(RespuestaGuardarDto dto) => Post<RespuestaDto>("api/respuestas-rapidas", dto);
    public Task<RespuestaDto> EditarRespuestaAsync(int id, RespuestaGuardarDto dto) => Put<RespuestaDto>($"api/respuestas-rapidas/{id}", dto);
    public Task EliminarRespuestaAsync(int id) => Delete($"api/respuestas-rapidas/{id}");
    public Task<RespuestaRenderizadaDto> RenderizarRespuestaAsync(int id, int? pedidoId = null) =>
        Get<RespuestaRenderizadaDto>($"api/respuestas-rapidas/{id}/renderizada" + (pedidoId is { } p ? $"?pedidoId={p}" : ""));

    // ───── Equipo ─────
    public Task<List<MiembroDto>> EquipoAsync() => Get<List<MiembroDto>>("api/equipo");
    public Task<MiembroDto> InvitarAsync(InvitarDto dto) => Post<MiembroDto>("api/equipo/invitar", dto);
    public Task RevocarAsync(int miembroId) => Delete($"api/equipo/{miembroId}");
    public Task<NegocioDto> AceptarInvitacionAsync(string token) => Post<NegocioDto>($"api/equipo/aceptar/{Uri.EscapeDataString(token)}");
}
