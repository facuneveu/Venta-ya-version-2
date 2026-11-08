using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentaYa.Api.Data;
using VentaYa.Api.Dtos;
using VentaYa.Api.Services;

namespace VentaYa.Api.Controllers;

/// <summary>Catálogo público de un negocio: lo usan los clientes finales, sin iniciar sesión.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/publico/{slug}")]
public class PublicoController(VentaYaDbContext db, PedidosService pedidos) : ControllerBase
{
    public record ZonaPublicaDto(int Id, string Nombre, decimal Precio);
    public record CatalogoPublicoDto(
        string Nombre, string? Descripcion, string Telefono, string? LogoUrl, bool EnvioHabilitado,
        List<ProductoDto> Productos, List<ZonaPublicaDto> ZonasEnvio);

    [HttpGet]
    public async Task<ActionResult<CatalogoPublicoDto>> Catalogo(string slug)
    {
        var negocio = await BuscarNegocioAsync(slug);
        var productos = await db.Productos.Include(p => p.Imagenes)
            .Where(p => p.NegocioId == negocio.Id && p.Activo).OrderBy(p => p.Nombre).ToListAsync();
        var zonas = await db.ZonasEnvio.Where(z => z.NegocioId == negocio.Id && z.Activa).OrderBy(z => z.Nombre)
            .Select(z => new ZonaPublicaDto(z.Id, z.Nombre, z.Precio)).ToListAsync();

        return Ok(new CatalogoPublicoDto(
            negocio.Nombre, negocio.Descripcion, negocio.Telefono, negocio.LogoUrl, negocio.EnvioHabilitado,
            productos.Select(ProductoDto.Desde).ToList(), zonas));
    }

    /// <summary>El cliente final envía su pedido. Queda en estado Pendiente.</summary>
    [HttpPost("pedidos")]
    public async Task<ActionResult<PedidoDto>> CrearPedido(string slug, PedidoCrearDto dto)
    {
        var negocio = await BuscarNegocioAsync(slug);
        var creado = await pedidos.CrearAsync(negocio, dto);
        return Ok(PedidoDto.Desde(creado));
    }

    private async Task<Models.Negocio> BuscarNegocioAsync(string slug) =>
        await db.Negocios.Include(n => n.Plan).FirstOrDefaultAsync(n => n.Slug == slug)
        ?? throw new ReglaNegocioException("Negocio no encontrado", 404);
}
