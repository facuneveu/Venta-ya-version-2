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
[Route("api/cupones")]
public class CuponesController(VentaYaDbContext db, NegocioActual actual) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CuponDto>>> Listar()
    {
        var negocio = await actual.ObtenerOFallarAsync();
        // Los usos se cuentan desde pedidos (no se guardan: evita dato derivado).
        var lista = await db.Cupones.Where(c => c.NegocioId == negocio.Id).OrderBy(c => c.Codigo)
            .Select(c => new CuponDto(c.Id, c.Codigo, c.Tipo, c.Valor, c.MontoMinimo, c.UsosMaximos, c.VenceEn, c.Activo,
                db.Pedidos.Count(p => p.CuponId == c.Id && p.Estado != EstadoPedido.Cancelado)))
            .ToListAsync();
        return Ok(lista);
    }

    [HttpPost]
    public async Task<ActionResult<CuponDto>> Crear(CuponGuardarDto dto)
    {
        var negocio = await actual.ObtenerOFallarAsync();
        var codigo = Validar(dto);
        if (await db.Cupones.AnyAsync(c => c.NegocioId == negocio.Id && c.Codigo == codigo))
            throw new ReglaNegocioException("Ya existe un cupón con ese código", 409);

        var cupon = new Cupon { NegocioId = negocio.Id };
        Aplicar(cupon, dto, codigo);
        db.Cupones.Add(cupon);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Listar), Mapear(cupon, 0));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CuponDto>> Editar(int id, CuponGuardarDto dto)
    {
        var negocio = await actual.ObtenerOFallarAsync();
        var cupon = await BuscarAsync(negocio, id);
        var codigo = Validar(dto);
        if (await db.Cupones.AnyAsync(c => c.NegocioId == negocio.Id && c.Codigo == codigo && c.Id != id))
            throw new ReglaNegocioException("Ya existe un cupón con ese código", 409);

        Aplicar(cupon, dto, codigo);
        await db.SaveChangesAsync();
        var usos = await db.Pedidos.CountAsync(p => p.CuponId == id && p.Estado != EstadoPedido.Cancelado);
        return Ok(Mapear(cupon, usos));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var negocio = await actual.ObtenerOFallarAsync();
        var cupon = await BuscarAsync(negocio, id);
        if (await db.Pedidos.AnyAsync(p => p.CuponId == id))
            throw new ReglaNegocioException("El cupón ya se usó en pedidos. Desactivalo en vez de eliminarlo.", 409);
        db.Cupones.Remove(cupon);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static string Validar(CuponGuardarDto dto)
    {
        if (dto.Tipo == TipoCupon.Porcentaje && dto.Valor > 100)
            throw new ReglaNegocioException("El porcentaje no puede superar 100");
        return dto.Codigo.Trim().ToUpperInvariant();
    }

    private static void Aplicar(Cupon c, CuponGuardarDto dto, string codigo)
    {
        c.Codigo = codigo; c.Tipo = dto.Tipo; c.Valor = dto.Valor; c.MontoMinimo = dto.MontoMinimo;
        c.UsosMaximos = dto.UsosMaximos; c.VenceEn = dto.VenceEn?.ToUniversalTime(); c.Activo = dto.Activo;
    }

    private static CuponDto Mapear(Cupon c, int usos) =>
        new(c.Id, c.Codigo, c.Tipo, c.Valor, c.MontoMinimo, c.UsosMaximos, c.VenceEn, c.Activo, usos);

    private async Task<Cupon> BuscarAsync(Negocio negocio, int id) =>
        await db.Cupones.FirstOrDefaultAsync(c => c.Id == id && c.NegocioId == negocio.Id)
        ?? throw new ReglaNegocioException("Cupón no encontrado", 404);
}
