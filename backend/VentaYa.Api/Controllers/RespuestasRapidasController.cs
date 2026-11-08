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
[Route("api/respuestas-rapidas")]
public class RespuestasRapidasController(VentaYaDbContext db, NegocioActual actual, PlanService planes) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<RespuestaDto>>> Listar()
    {
        var negocio = await actual.ObtenerOFallarAsync();
        var lista = await db.RespuestasRapidas.Where(r => r.NegocioId == negocio.Id).OrderBy(r => r.Titulo)
            .Select(r => new RespuestaDto(r.Id, r.Titulo, r.Contenido)).ToListAsync();
        return Ok(lista);
    }

    [HttpPost]
    public async Task<ActionResult<RespuestaDto>> Crear(RespuestaGuardarDto dto)
    {
        var negocio = await actual.ObtenerOFallarAsync();
        await planes.ValidarNuevaRespuestaAsync(negocio); // límite del plan gratis
        var r = new RespuestaRapida { NegocioId = negocio.Id, Titulo = dto.Titulo.Trim(), Contenido = dto.Contenido.Trim() };
        db.RespuestasRapidas.Add(r);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Listar), new RespuestaDto(r.Id, r.Titulo, r.Contenido));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RespuestaDto>> Editar(int id, RespuestaGuardarDto dto)
    {
        var r = await BuscarAsync(id);
        r.Titulo = dto.Titulo.Trim(); r.Contenido = dto.Contenido.Trim();
        await db.SaveChangesAsync();
        return Ok(new RespuestaDto(r.Id, r.Titulo, r.Contenido));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        db.RespuestasRapidas.Remove(await BuscarAsync(id));
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Devuelve el texto con las variables reemplazadas: {nombre}, {total} y {link}.
    /// Si se indica pedidoId se usan los datos de ese pedido; {link} es el catálogo público.
    /// </summary>
    [HttpGet("{id:int}/renderizada")]
    public async Task<ActionResult<RespuestaRenderizadaDto>> Renderizar(int id, [FromQuery] int? pedidoId)
    {
        var negocio = await actual.ObtenerOFallarAsync();
        var r = await BuscarAsync(id);

        string? nombre = null; decimal? total = null;
        if (pedidoId is { } pid)
        {
            var pedido = await db.Pedidos.Include(p => p.Cliente).Include(p => p.Items)
                             .FirstOrDefaultAsync(p => p.Id == pid && p.NegocioId == negocio.Id)
                         ?? throw new ReglaNegocioException("Pedido no encontrado", 404);
            nombre = pedido.Cliente.Nombre; total = pedido.Total;
        }
        var link = $"{Request.Scheme}://{Request.Host}/api/publico/{negocio.Slug}";
        return Ok(new RespuestaRenderizadaDto(ReglasNegocio.RenderizarRespuesta(r.Contenido, nombre, total, link)));
    }

    private async Task<RespuestaRapida> BuscarAsync(int id)
    {
        var negocio = await actual.ObtenerOFallarAsync();
        return await db.RespuestasRapidas.FirstOrDefaultAsync(r => r.Id == id && r.NegocioId == negocio.Id)
               ?? throw new ReglaNegocioException("Respuesta no encontrada", 404);
    }
}
