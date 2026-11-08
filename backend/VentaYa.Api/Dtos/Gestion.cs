using System.ComponentModel.DataAnnotations;
using VentaYa.Api.Models;

namespace VentaYa.Api.Dtos;

// ───── Cupones ─────
public record CuponGuardarDto(
    [Required, StringLength(30, MinimumLength = 2)] string Codigo,
    TipoCupon Tipo,
    [Range(0.01, 100000000)] decimal Valor,
    [Range(0, 100000000)] decimal MontoMinimo = 0,
    [Range(1, 1000000)] int? UsosMaximos = null,
    DateTime? VenceEn = null,
    bool Activo = true);

public record CuponDto(
    int Id, string Codigo, TipoCupon Tipo, decimal Valor, decimal MontoMinimo,
    int? UsosMaximos, DateTime? VenceEn, bool Activo, int Usos);

// ───── Zonas de envío ─────
public record ZonaGuardarDto(
    [Required, StringLength(100, MinimumLength = 2)] string Nombre,
    [Range(0, 100000000)] decimal Precio,
    bool Activa = true);

public record ZonaDto(int Id, string Nombre, decimal Precio, bool Activa);

// ───── Respuestas rápidas ─────
public record RespuestaGuardarDto(
    [Required, StringLength(100, MinimumLength = 2)] string Titulo,
    [Required, StringLength(1000, MinimumLength = 1)] string Contenido);

public record RespuestaDto(int Id, string Titulo, string Contenido);
public record RespuestaRenderizadaDto(string Texto);

// ───── Equipo ─────
public record InvitarDto([Required, EmailAddress, StringLength(200)] string Email);

public record MiembroDto(
    int Id, string? Email, string? Nombre, string Rol, bool Aceptado,
    string? TokenInvitacion, DateTime? AceptadoEn);

// ───── Plan y panel ─────
public record CambiarPlanDto(
    [Required, RegularExpression("^(Gratis|Pago)$", ErrorMessage = "El plan debe ser Gratis o Pago")] string Plan,
    [RegularExpression("^(mensual|anual)$", ErrorMessage = "El período debe ser mensual o anual")] string Periodo = "mensual");

public record UsoPlanDto(
    string Plan, int? DiasDePruebaRestantes,
    int? MaxProductos, int Productos,
    int? MaxPedidosMes, int PedidosMes,
    int? MaxRespuestas, int Respuestas);

public record PanelDto(
    decimal VentasMes, decimal VentasTotales,
    int PedidosMes, Dictionary<string, int> PedidosPorEstado,
    int ProductosActivos, UsoPlanDto Uso, List<PedidoDto> UltimosPedidos);
