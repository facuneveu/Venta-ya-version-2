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
[Route("api/productos")]
public class ProductosController(
    VentaYaDbContext db, NegocioActual actual, PlanService planes, IWebHostEnvironment env) : ControllerBase
{
    private static readonly string[] ExtensionesPermitidas = [".jpg", ".jpeg", ".png", ".webp"];
    private const int MaxImagenesPorProducto = 5;
    private const long MaxBytesImagen = 5 * 1024 * 1024;

    [HttpGet]
    public async Task<ActionResult<List<ProductoDto>>> Listar([FromQuery] string? q, [FromQuery] bool soloActivos = false)
    {
        var negocio = await actual.ObtenerOFallarAsync();
        var query = db.Productos.Include(p => p.Imagenes).Where(p => p.NegocioId == negocio.Id);
        if (soloActivos) query = query.Where(p => p.Activo);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(p => p.Nombre.Contains(q.Trim()));

        var productos = await query.OrderBy(p => p.Nombre).ToListAsync();
        return Ok(productos.Select(ProductoDto.Desde).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductoDto>> Obtener(int id) =>
        Ok(ProductoDto.Desde(await BuscarAsync(id)));

    [HttpPost]
    public async Task<ActionResult<ProductoDto>> Crear(ProductoGuardarDto dto)
    {
        var negocio = await actual.ObtenerOFallarAsync();
        await planes.ValidarNuevoProductoAsync(negocio); // límite del plan gratis

        var producto = new Producto
        {
            NegocioId = negocio.Id,
            Nombre = dto.Nombre.Trim(),
            Descripcion = dto.Descripcion?.Trim(),
            Precio = dto.Precio,
            Stock = dto.Stock,
            Activo = dto.Activo,
        };
        db.Productos.Add(producto);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Obtener), new { id = producto.Id }, ProductoDto.Desde(producto));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductoDto>> Editar(int id, ProductoGuardarDto dto)
    {
        var producto = await BuscarAsync(id);
        producto.Nombre = dto.Nombre.Trim();
        producto.Descripcion = dto.Descripcion?.Trim();
        producto.Precio = dto.Precio;
        producto.Stock = dto.Stock;
        producto.Activo = dto.Activo;
        await db.SaveChangesAsync();
        return Ok(ProductoDto.Desde(producto));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var producto = await BuscarAsync(id);
        // Si ya se vendió, borrarlo rompería el historial de pedidos: se desactiva en su lugar.
        if (await db.ItemsPedido.AnyAsync(i => i.ProductoId == id))
            throw new ReglaNegocioException("El producto tiene pedidos asociados. Desactivalo en vez de eliminarlo.", 409);

        db.Productos.Remove(producto);
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Sube una imagen (jpg/png/webp, máx. 5 MB, hasta 5 por producto).</summary>
    [HttpPost("{id:int}/imagenes")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ImagenDto>> SubirImagen(int id, IFormFile archivo)
    {
        var producto = await BuscarAsync(id);
        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!ExtensionesPermitidas.Contains(extension))
            throw new ReglaNegocioException("Formato no permitido. Usá JPG, PNG o WEBP.");
        if (archivo.Length is 0 or > MaxBytesImagen)
            throw new ReglaNegocioException("La imagen debe pesar entre 1 byte y 5 MB.");
        if (producto.Imagenes.Count >= MaxImagenesPorProducto)
            throw new ReglaNegocioException($"Máximo {MaxImagenesPorProducto} imágenes por producto.");

        var carpeta = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads", "productos");
        Directory.CreateDirectory(carpeta);
        var nombreArchivo = $"{Guid.NewGuid():N}{extension}"; // nombre generado: nunca se usa el del cliente
        await using (var stream = System.IO.File.Create(Path.Combine(carpeta, nombreArchivo)))
            await archivo.CopyToAsync(stream);

        var imagen = new ImagenProducto
        {
            ProductoId = producto.Id,
            Url = $"/uploads/productos/{nombreArchivo}",
            Orden = producto.Imagenes.Count,
        };
        db.ImagenesProducto.Add(imagen);
        await db.SaveChangesAsync();
        return Ok(new ImagenDto(imagen.Id, imagen.Url, imagen.Orden));
    }

    [HttpDelete("{id:int}/imagenes/{imagenId:int}")]
    public async Task<IActionResult> EliminarImagen(int id, int imagenId)
    {
        var producto = await BuscarAsync(id);
        var imagen = producto.Imagenes.FirstOrDefault(i => i.Id == imagenId)
                     ?? throw new ReglaNegocioException("Imagen no encontrada", 404);

        var ruta = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), imagen.Url.TrimStart('/'));
        if (System.IO.File.Exists(ruta)) System.IO.File.Delete(ruta);

        db.ImagenesProducto.Remove(imagen);
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Busca el producto SOLO dentro del negocio del usuario (aislamiento entre negocios).</summary>
    private async Task<Producto> BuscarAsync(int id)
    {
        var negocio = await actual.ObtenerOFallarAsync();
        return await db.Productos.Include(p => p.Imagenes)
                   .FirstOrDefaultAsync(p => p.Id == id && p.NegocioId == negocio.Id)
               ?? throw new ReglaNegocioException("Producto no encontrado", 404);
    }
}
