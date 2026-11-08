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
[Route("api/zonas-envio")]
public class ZonasEnvioController(VentaYaDbContext db, NegocioActual actual) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ZonaDto>>> Listar()
    {
        var negocio = await actual.ObtenerOFallarAsync();
        var lista = await db.ZonasEnvio.Where(z => z.NegocioId == negocio.Id).OrderBy(z => z.Nombre)
            .Select(z => new ZonaDto(z.Id, z.Nombre, z.Precio, z.Activa)).ToListAsync();
        return Ok(lista);
    }

    [HttpPost]
    public async Task<ActionResult<ZonaDto>> Crear(ZonaGuardarDto dto)
    {
        var negocio = await actual.ObtenerOFallarAsync();
        var zona = new ZonaEnvio { NegocioId = negocio.Id, Nombre = dto.Nombre.Trim(), Precio = dto.Precio, Activa = dto.Activa };
        db.ZonasEnvio.Add(zona);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Listar), Mapear(zona));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ZonaDto>> Editar(int id, ZonaGuardarDto dto)
    {
        var zona = await BuscarAsync(id);
        zona.Nombre = dto.Nombre.Trim(); zona.Precio = dto.Precio; zona.Activa = dto.Activa;
        await db.SaveChangesAsync();
        return Ok(Mapear(zona));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var zona = await BuscarAsync(id);
        if (await db.Pedidos.AnyAsync(p => p.ZonaEnvioId == id))
            throw new ReglaNegocioException("La zona ya se usó en pedidos. Desactivala en vez de eliminarla.", 409);
        db.ZonasEnvio.Remove(zona);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static ZonaDto Mapear(ZonaEnvio z) => new(z.Id, z.Nombre, z.Precio, z.Activa);

    private async Task<ZonaEnvio> BuscarAsync(int id)
    {
        var negocio = await actual.ObtenerOFallarAsync();
        return await db.ZonasEnvio.FirstOrDefaultAsync(z => z.Id == id && z.NegocioId == negocio.Id)
               ?? throw new ReglaNegocioException("Zona de envío no encontrada", 404);
    }
}
