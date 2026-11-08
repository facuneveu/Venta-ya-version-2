using Microsoft.EntityFrameworkCore;
using VentaYa.Api.Data;
using VentaYa.Api.Dtos;
using VentaYa.Api.Models;

namespace VentaYa.Api.Services;

/// <summary>
/// Crea pedidos y cambia su estado. Lo usan tanto el panel (usuario logueado) como el
/// catálogo público (cliente sin sesión), así las reglas son siempre las mismas.
/// </summary>
public class PedidosService(VentaYaDbContext db, PlanService planes)
{
    public async Task<Pedido> CrearAsync(Negocio negocio, PedidoCrearDto dto)
    {
        await planes.ValidarNuevoPedidoAsync(negocio);

        // 1) Productos: el precio SIEMPRE sale de la BD, nunca del cliente.
        var pedidos = dto.Items.GroupBy(i => i.ProductoId)
            .Select(g => (ProductoId: g.Key, Cantidad: g.Sum(x => x.Cantidad))).ToList();
        var ids = pedidos.Select(p => p.ProductoId).ToList();
        var productos = await db.Productos
            .Where(p => p.NegocioId == negocio.Id && ids.Contains(p.Id))
            .ToListAsync();

        var items = new List<ItemPedido>();
        foreach (var (productoId, cantidad) in pedidos)
        {
            var producto = productos.FirstOrDefault(p => p.Id == productoId);
            if (producto is null || !producto.Activo)
                throw new ReglaNegocioException("Uno de los productos ya no está disponible");
            if (producto.Stock is { } stock && cantidad > stock)
                throw new ReglaNegocioException($"Solo quedan {stock} unidades de \"{producto.Nombre}\"");

            if (producto.Stock is not null) producto.Stock -= cantidad; // descuenta stock
            items.Add(new ItemPedido { Producto = producto, Cantidad = cantidad, PrecioUnitario = producto.Precio });
        }
        var subtotal = items.Sum(i => i.Cantidad * i.PrecioUnitario);

        // 2) Envío
        ZonaEnvio? zona = null;
        if (dto.ZonaEnvioId is { } zonaId)
        {
            if (!negocio.EnvioHabilitado) throw new ReglaNegocioException("Este negocio no hace envíos");
            zona = await db.ZonasEnvio.FirstOrDefaultAsync(z => z.Id == zonaId && z.NegocioId == negocio.Id && z.Activa)
                   ?? throw new ReglaNegocioException("La zona de envío no es válida");
            if (string.IsNullOrWhiteSpace(dto.DireccionEntrega))
                throw new ReglaNegocioException("Indicá la dirección de entrega");
        }

        // 3) Cupón
        Cupon? cupon = null;
        decimal descuento = 0;
        if (!string.IsNullOrWhiteSpace(dto.CodigoCupon))
        {
            var codigo = dto.CodigoCupon.Trim().ToUpperInvariant();
            cupon = await db.Cupones.FirstOrDefaultAsync(c => c.NegocioId == negocio.Id && c.Codigo == codigo)
                    ?? throw new ReglaNegocioException("El cupón no existe");
            var usos = await db.Pedidos.CountAsync(p => p.CuponId == cupon.Id && p.Estado != EstadoPedido.Cancelado);
            var motivo = ReglasNegocio.ValidarCupon(cupon, subtotal, usos, DateTime.UtcNow);
            if (motivo is not null) throw new ReglaNegocioException(motivo);
            descuento = ReglasNegocio.CalcularDescuento(cupon, subtotal);
        }

        // 4) Cliente: se busca por teléfono; si no existe se crea.
        var telefono = dto.ClienteTelefono.Trim();
        var cliente = await db.Clientes.FirstOrDefaultAsync(c => c.Telefono == telefono);
        if (cliente is null)
        {
            cliente = new Cliente { Nombre = dto.ClienteNombre.Trim(), Telefono = telefono };
            db.Clientes.Add(cliente);
        }
        else cliente.Nombre = dto.ClienteNombre.Trim();

        // 5) Pedido. Un único SaveChanges = todo se guarda o nada (atómico).
        var pedido = new Pedido
        {
            NegocioId = negocio.Id,
            Cliente = cliente,
            ZonaEnvio = zona,
            Cupon = cupon,
            Estado = EstadoPedido.Pendiente,
            MetodoPago = dto.MetodoPago,
            DireccionEntrega = dto.DireccionEntrega?.Trim(),
            CostoEnvio = zona?.Precio ?? 0,
            DescuentoAplicado = descuento,
            Notas = dto.Notas?.Trim(),
            Items = items,
        };
        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync();
        return pedido;
    }

    public async Task<Pedido> CambiarEstadoAsync(Negocio negocio, int pedidoId, EstadoPedido nuevo)
    {
        var pedido = await db.Pedidos
            .Include(p => p.Cliente).Include(p => p.Items).ThenInclude(i => i.Producto)
            .FirstOrDefaultAsync(p => p.Id == pedidoId && p.NegocioId == negocio.Id)
            ?? throw new ReglaNegocioException("Pedido no encontrado", 404);

        if (!ReglasNegocio.TransicionValida(pedido.Estado, nuevo))
            throw new ReglaNegocioException($"No se puede pasar un pedido de {pedido.Estado} a {nuevo}", 409);

        // Al cancelar se devuelve el stock.
        if (nuevo == EstadoPedido.Cancelado)
            foreach (var item in pedido.Items.Where(i => i.Producto.Stock is not null))
                item.Producto.Stock += item.Cantidad;

        pedido.Estado = nuevo;
        await db.SaveChangesAsync();
        return pedido;
    }
}
