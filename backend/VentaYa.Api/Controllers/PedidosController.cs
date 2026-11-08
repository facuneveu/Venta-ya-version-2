using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentaYa.Api.Data;
using VentaYa.Api.Dtos;
using VentaYa.Api.Models;
using VentaYa.Api.Services;

namespace VentaYa.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/pedidos")]
public class PedidosController(VentaYaDbContext db, NegocioActual actual, PedidosService pedidos) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<PedidoDto>>> Listar([FromQuery] EstadoPedido? estado)
    {
        var negocio = await actual.ObtenerOFallarAsync();
        var query = ConDetalle().Where(p => p.NegocioId == negocio.Id);
        if (estado is { } e) query = query.Where(p => p.Estado == e);

        var lista = await query.OrderByDescending(p => p.CreadoEn).Take(200).ToListAsync();
        return Ok(lista.Select(PedidoDto.Desde).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PedidoDto>> Obtener(int id)
    {
        var negocio = await actual.ObtenerOFallarAsync();
        var pedido = await ConDetalle().FirstOrDefaultAsync(p => p.Id == id && p.NegocioId == negocio.Id)
                     ?? throw new ReglaNegocioException("Pedido no encontrado", 404);
        return Ok(PedidoDto.Desde(pedido));
    }

    /// <summary>Carga manual de un pedido (por ejemplo, uno que llegó por WhatsApp).</summary>
    [HttpPost]
    public async Task<ActionResult<PedidoDto>> Crear(PedidoCrearDto dto)
    {
        var negocio = await actual.ObtenerOFallarAsync();
        var creado = await pedidos.CrearAsync(negocio, dto);
        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, PedidoDto.Desde(creado));
    }

    [HttpPatch("{id:int}/estado")]
    public async Task<ActionResult<PedidoDto>> CambiarEstado(int id, CambiarEstadoDto dto)
    {
        var negocio = await actual.ObtenerOFallarAsync();
        var pedido = await pedidos.CambiarEstadoAsync(negocio, id, dto.Estado);
        return Ok(PedidoDto.Desde(pedido));
    }

    private IQueryable<Pedido> ConDetalle() =>
        db.Pedidos.Include(p => p.Cliente).Include(p => p.Items).ThenInclude(i => i.Producto);
}
