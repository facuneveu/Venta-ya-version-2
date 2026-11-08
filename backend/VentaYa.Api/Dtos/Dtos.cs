using System.ComponentModel.DataAnnotations;
using VentaYa.Api.Models;

namespace VentaYa.Api.Dtos;

// ───── Auth ─────
public record RegistroDto(
    [Required, StringLength(100, MinimumLength = 2)] string Nombre,
    [Required, EmailAddress, StringLength(200)] string Email,
    [Required, StringLength(100, MinimumLength = 6)] string Password);

public record LoginDto(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public record UsuarioDto(int Id, string Nombre, string Email);
public record AuthRespuestaDto(string Token, UsuarioDto Usuario);

// ───── Negocio ─────
public record NegocioGuardarDto(
    [Required, StringLength(100, MinimumLength = 2)] string Nombre,
    [StringLength(500)] string? Descripcion,
    [Required, StringLength(30, MinimumLength = 6)] string Telefono,
    [StringLength(200)] string? Direccion,
    bool EnvioHabilitado = false);

public record NegocioDto(
    int Id, string Nombre, string? Descripcion, string Telefono, string? Direccion,
    string? LogoUrl, string Slug, bool EnvioHabilitado,
    string Plan, string PlanPeriodo, DateTime? TrialExpiraEn, string Rol)
{
    public static NegocioDto Desde(Negocio n, bool esPropietario) => new(
        n.Id, n.Nombre, n.Descripcion, n.Telefono, n.Direccion, n.LogoUrl, n.Slug,
        n.EnvioHabilitado, n.Plan.Nombre, n.PlanPeriodo, n.TrialExpiraEn,
        esPropietario ? "Propietario" : "Empleado");
}

// ───── Productos ─────
public record ProductoGuardarDto(
    [Required, StringLength(120, MinimumLength = 1)] string Nombre,
    [StringLength(1000)] string? Descripcion,
    [Range(0, 100000000)] decimal Precio,
    [Range(0, 1000000)] int? Stock,
    bool Activo = true);

public record ImagenDto(int Id, string Url, int Orden);

public record ProductoDto(
    int Id, string Nombre, string? Descripcion, decimal Precio, int? Stock, bool Activo,
    List<ImagenDto> Imagenes)
{
    public static ProductoDto Desde(Producto p) => new(
        p.Id, p.Nombre, p.Descripcion, p.Precio, p.Stock, p.Activo,
        p.Imagenes.OrderBy(i => i.Orden).Select(i => new ImagenDto(i.Id, i.Url, i.Orden)).ToList());
}

// ───── Pedidos ─────
public record ItemPedidoCrearDto(
    [Range(1, int.MaxValue)] int ProductoId,
    [Range(1, 10000)] int Cantidad);

public record PedidoCrearDto(
    [Required, StringLength(100, MinimumLength = 2)] string ClienteNombre,
    [Required, StringLength(30, MinimumLength = 6)] string ClienteTelefono,
    [Required, MinLength(1)] List<ItemPedidoCrearDto> Items,
    MetodoPago MetodoPago = MetodoPago.Efectivo,
    int? ZonaEnvioId = null,
    [StringLength(30)] string? CodigoCupon = null,
    [StringLength(200)] string? DireccionEntrega = null,
    [StringLength(500)] string? Notas = null);

public record CambiarEstadoDto(EstadoPedido Estado);

public record ItemPedidoDto(int ProductoId, string Producto, int Cantidad, decimal PrecioUnitario, decimal Subtotal);

public record PedidoDto(
    int Id, EstadoPedido Estado, MetodoPago MetodoPago,
    string ClienteNombre, string ClienteTelefono, string? DireccionEntrega,
    decimal Subtotal, decimal CostoEnvio, decimal Descuento, decimal Total,
    string? Notas, DateTime CreadoEn, List<ItemPedidoDto> Items)
{
    /// <summary>Requiere cargados Cliente e Items (con Producto).</summary>
    public static PedidoDto Desde(Pedido p) => new(
        p.Id, p.Estado, p.MetodoPago, p.Cliente.Nombre, p.Cliente.Telefono, p.DireccionEntrega,
        p.Subtotal, p.CostoEnvio, p.DescuentoAplicado, p.Total, p.Notas, p.CreadoEn,
        p.Items.Select(i => new ItemPedidoDto(i.ProductoId, i.Producto.Nombre, i.Cantidad, i.PrecioUnitario, i.Cantidad * i.PrecioUnitario)).ToList());
}
