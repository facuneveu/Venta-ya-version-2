namespace VentaYa.App.Core.Models;

// Espejo de los DTOs de la API (backend/VentaYa.Api/Dtos). Los nombres JSON coinciden (camelCase).

public enum EstadoPedido { Pendiente, Confirmado, Pagado, Entregado, Cancelado }
public enum MetodoPago { Efectivo, Transferencia, MercadoPago }
public enum TipoCupon { Porcentaje, MontoFijo }

// ───── Auth ─────
public record RegistroDto(string Nombre, string Email, string Password);
public record LoginDto(string Email, string Password);
public record UsuarioDto(int Id, string Nombre, string Email);
public record AuthRespuestaDto(string Token, UsuarioDto Usuario);
public record YoDto(UsuarioDto Usuario, NegocioDto? Negocio);

// ───── Negocio ─────
public record NegocioGuardarDto(string Nombre, string? Descripcion, string Telefono, string? Direccion, bool EnvioHabilitado);

public record NegocioDto(
    int Id, string Nombre, string? Descripcion, string Telefono, string? Direccion, string? LogoUrl,
    string Slug, bool EnvioHabilitado, string Plan, string PlanPeriodo, DateTime? TrialExpiraEn, string Rol);

public record CambiarPlanDto(string Plan, string Periodo = "mensual");

// ───── Productos ─────
public record ProductoGuardarDto(string Nombre, string? Descripcion, decimal Precio, int? Stock, bool Activo = true);
public record ImagenDto(int Id, string Url, int Orden);

public record ProductoDto(
    int Id, string Nombre, string? Descripcion, decimal Precio, int? Stock, bool Activo, List<ImagenDto> Imagenes);

// ───── Pedidos ─────
public record ItemPedidoCrearDto(int ProductoId, int Cantidad);

public record PedidoCrearDto(
    string ClienteNombre, string ClienteTelefono, List<ItemPedidoCrearDto> Items,
    MetodoPago MetodoPago = MetodoPago.Efectivo, int? ZonaEnvioId = null, string? CodigoCupon = null,
    string? DireccionEntrega = null, string? Notas = null);

public record CambiarEstadoDto(EstadoPedido Estado);
public record ItemPedidoDto(int ProductoId, string Producto, int Cantidad, decimal PrecioUnitario, decimal Subtotal);

public record PedidoDto(
    int Id, EstadoPedido Estado, MetodoPago MetodoPago, string ClienteNombre, string ClienteTelefono,
    string? DireccionEntrega, decimal Subtotal, decimal CostoEnvio, decimal Descuento, decimal Total,
    string? Notas, DateTime CreadoEn, List<ItemPedidoDto> Items);

// ───── Cupones, zonas, respuestas, equipo ─────
public record CuponGuardarDto(
    string Codigo, TipoCupon Tipo, decimal Valor, decimal MontoMinimo = 0, int? UsosMaximos = null,
    DateTime? VenceEn = null, bool Activo = true);

public record CuponDto(
    int Id, string Codigo, TipoCupon Tipo, decimal Valor, decimal MontoMinimo, int? UsosMaximos,
    DateTime? VenceEn, bool Activo, int Usos);

public record ZonaGuardarDto(string Nombre, decimal Precio, bool Activa = true);
public record ZonaDto(int Id, string Nombre, decimal Precio, bool Activa);

public record RespuestaGuardarDto(string Titulo, string Contenido);
public record RespuestaDto(int Id, string Titulo, string Contenido);
public record RespuestaRenderizadaDto(string Texto);

public record InvitarDto(string Email);
public record MiembroDto(int Id, string? Email, string? Nombre, string Rol, bool Aceptado, string? TokenInvitacion, DateTime? AceptadoEn);

// ───── Panel ─────
public record UsoPlanDto(
    string Plan, int? DiasDePruebaRestantes, int? MaxProductos, int Productos,
    int? MaxPedidosMes, int PedidosMes, int? MaxRespuestas, int Respuestas);

public record PanelDto(
    decimal VentasMes, decimal VentasTotales, int PedidosMes, Dictionary<string, int> PedidosPorEstado,
    int ProductosActivos, UsoPlanDto Uso, List<PedidoDto> UltimosPedidos);
