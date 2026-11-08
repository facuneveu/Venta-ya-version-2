namespace VentaYa.Api.Models;

/// <summary>Comprador identificado por teléfono. Puede comprar en varios negocios.</summary>
public class Cliente
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string? Email { get; set; }

    public List<Pedido> Pedidos { get; set; } = [];
}

public class ZonaEnvio
{
    public int Id { get; set; }
    public int NegocioId { get; set; }
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public bool Activa { get; set; } = true;

    public Negocio Negocio { get; set; } = null!;
}

public class Cupon
{
    public int Id { get; set; }
    public int NegocioId { get; set; }
    public string Codigo { get; set; } = "";
    public TipoCupon Tipo { get; set; }
    public decimal Valor { get; set; }
    public decimal MontoMinimo { get; set; }
    public int? UsosMaximos { get; set; }
    public DateTime? VenceEn { get; set; }
    public bool Activo { get; set; } = true;

    public Negocio Negocio { get; set; } = null!;
}

public class Pedido
{
    public int Id { get; set; }
    public int NegocioId { get; set; }
    public int ClienteId { get; set; }
    public int? ZonaEnvioId { get; set; }
    public int? CuponId { get; set; }
    public EstadoPedido Estado { get; set; } = EstadoPedido.Pendiente;
    public MetodoPago MetodoPago { get; set; } = MetodoPago.Efectivo;
    public string? DireccionEntrega { get; set; }
    /// <summary>Snapshot del precio de la zona al momento del pedido.</summary>
    public decimal CostoEnvio { get; set; }
    /// <summary>Snapshot del descuento aplicado por el cupón.</summary>
    public decimal DescuentoAplicado { get; set; }
    public string? Notas { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public Negocio Negocio { get; set; } = null!;
    public Cliente Cliente { get; set; } = null!;
    public ZonaEnvio? ZonaEnvio { get; set; }
    public Cupon? Cupon { get; set; }
    public List<ItemPedido> Items { get; set; } = [];

    // El total NO se guarda en la BD (3FN): se calcula a partir de los ítems.
    public decimal Subtotal => Items.Sum(i => i.Cantidad * i.PrecioUnitario);
    public decimal Total => Subtotal + CostoEnvio - DescuentoAplicado;
}

public class ItemPedido
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    /// <summary>Snapshot del precio del producto al momento del pedido.</summary>
    public decimal PrecioUnitario { get; set; }

    public Pedido Pedido { get; set; } = null!;
    public Producto Producto { get; set; } = null!;
}
