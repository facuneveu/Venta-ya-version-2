using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentaYa.Api.Data;
using VentaYa.Api.Dtos;
using VentaYa.Api.Models;
using VentaYa.Api.Services;

namespace VentaYa.Api.Controllers;

/// <summary>Pantalla de inicio de la app: ventas, pedidos por estado y uso del plan.</summary>
[ApiController]
[Authorize]
[Route("api/panel")]
public class PanelController(VentaYaDbContext db, NegocioActual actual, PlanService planes) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PanelDto>> Obtener()
    {
        var negocio = await actual.ObtenerOFallarAsync();
        var ahora = DateTime.UtcNow;
        var inicioMes = new DateTime(ahora.Year, ahora.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var pedidos = db.Pedidos.Where(p => p.NegocioId == negocio.Id);
        var vendidos = pedidos.Where(p => p.Estado != EstadoPedido.Cancelado);

        // El total no está en la BD: se calcula (ítems + envío - descuento).
        var ventasTotales = await vendidos
            .Select(p => p.Items.Sum(i => i.Cantidad * i.PrecioUnitario) + p.CostoEnvio - p.DescuentoAplicado)
            .SumAsync();
        var ventasMes = await vendidos.Where(p => p.CreadoEn >= inicioMes)
            .Select(p => p.Items.Sum(i => i.Cantidad * i.PrecioUnitario) + p.CostoEnvio - p.DescuentoAplicado)
            .SumAsync();

        var porEstado = await pedidos.GroupBy(p => p.Estado).Select(g => new { g.Key, N = g.Count() }).ToListAsync();
        var estados = Enum.GetValues<EstadoPedido>().ToDictionary(e => e.ToString(), e => porEstado.FirstOrDefault(x => x.Key == e)?.N ?? 0);

        var pedidosMes = await pedidos.CountAsync(p => p.CreadoEn >= inicioMes);
        var productosActivos = await db.Productos.CountAsync(p => p.NegocioId == negocio.Id && p.Activo);
        var productos = await db.Productos.CountAsync(p => p.NegocioId == negocio.Id);
        var respuestas = await db.RespuestasRapidas.CountAsync(r => r.NegocioId == negocio.Id);

        var limites = await planes.ObtenerLimitesAsync(negocio);
        int? diasPrueba = negocio.PlanId == PlanesIds.Prueba && negocio.TrialExpiraEn is { } fin && fin > ahora
            ? (int)Math.Ceiling((fin - ahora).TotalDays) : null;

        var ultimos = await db.Pedidos.Include(p => p.Cliente).Include(p => p.Items).ThenInclude(i => i.Producto)
            .Where(p => p.NegocioId == negocio.Id).OrderByDescending(p => p.CreadoEn).Take(5).ToListAsync();

        return Ok(new PanelDto(
            ventasMes, ventasTotales, pedidosMes, estados, productosActivos,
            new UsoPlanDto(limites.Nombre, diasPrueba, limites.MaxProductos, productos,
                limites.MaxPedidosMes, pedidosMes, limites.MaxRespuestas, respuestas),
            ultimos.Select(PedidoDto.Desde).ToList()));
    }
}
