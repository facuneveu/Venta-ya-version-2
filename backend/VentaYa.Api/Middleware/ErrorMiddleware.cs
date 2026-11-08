using VentaYa.Api.Services;

namespace VentaYa.Api.Middleware;

/// <summary>
/// Convierte las ReglaNegocioException en una respuesta JSON { "error": "mensaje" }
/// con el código HTTP correspondiente (400, 403, 404, 409...). La app móvil muestra ese mensaje.
/// </summary>
public class ErrorMiddleware(RequestDelegate siguiente, ILogger<ErrorMiddleware> log)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await siguiente(ctx);
        }
        catch (ReglaNegocioException ex)
        {
            ctx.Response.StatusCode = ex.Status;
            await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error no controlado");
            ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await ctx.Response.WriteAsJsonAsync(new { error = "Ocurrió un error inesperado. Intentá de nuevo." });
        }
    }
}
