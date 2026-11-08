using Microsoft.EntityFrameworkCore;
using VentaYa.Api.Data;
using VentaYa.Api.Models;

namespace VentaYa.Api.Services;

public record LimitesPlan(string Nombre, int? MaxProductos, int? MaxPedidosMes, int? MaxRespuestas);

/// <summary>Aplica los límites del plan freemium (Gratis / Prueba / Pago).</summary>
public class PlanService(VentaYaDbContext db)
{
    /// <summary>Límites vigentes: si la prueba venció, rige el plan Gratis.</summary>
    public async Task<LimitesPlan> ObtenerLimitesAsync(Negocio negocio)
    {
        var plan = negocio.Plan ?? await db.Planes.FirstAsync(p => p.Id == negocio.PlanId);
        if (ReglasNegocio.PruebaVencida(plan, negocio.TrialExpiraEn, DateTime.UtcNow))
            plan = await db.Planes.FirstAsync(p => p.Id == PlanesIds.Gratis);
        return new LimitesPlan(plan.Nombre, plan.MaxProductos, plan.MaxPedidosMes, plan.MaxRespuestas);
    }

    public async Task ValidarNuevoProductoAsync(Negocio negocio)
    {
        var limites = await ObtenerLimitesAsync(negocio);
        if (limites.MaxProductos is not { } max) return;
        var actuales = await db.Productos.CountAsync(p => p.NegocioId == negocio.Id);
        if (actuales >= max)
            throw new ReglaNegocioException($"Llegaste al límite de {max} productos del plan {limites.Nombre}. Pasate a Pago para tener productos ilimitados.", 403);
    }

    public async Task ValidarNuevoPedidoAsync(Negocio negocio)
    {
        var limites = await ObtenerLimitesAsync(negocio);
        if (limites.MaxPedidosMes is not { } max) return;
        var ahora = DateTime.UtcNow;
        var inicioMes = new DateTime(ahora.Year, ahora.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var delMes = await db.Pedidos.CountAsync(p => p.NegocioId == negocio.Id && p.CreadoEn >= inicioMes);
        if (delMes >= max)
            throw new ReglaNegocioException($"Llegaste al límite de {max} pedidos por mes del plan {limites.Nombre}.", 403);
    }

    public async Task ValidarNuevaRespuestaAsync(Negocio negocio)
    {
        var limites = await ObtenerLimitesAsync(negocio);
        if (limites.MaxRespuestas is not { } max) return;
        var actuales = await db.RespuestasRapidas.CountAsync(r => r.NegocioId == negocio.Id);
        if (actuales >= max)
            throw new ReglaNegocioException($"Llegaste al límite de {max} respuestas rápidas del plan {limites.Nombre}.", 403);
    }
}
