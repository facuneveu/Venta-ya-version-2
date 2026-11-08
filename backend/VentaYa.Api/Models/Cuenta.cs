namespace VentaYa.Api.Models;

/// <summary>Persona que inicia sesión en la app (dueño o empleado).</summary>
public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public List<Negocio> NegociosPropios { get; set; } = [];
    public List<MiembroNegocio> Membresias { get; set; } = [];
}

/// <summary>Plan freemium. Un límite en NULL significa "ilimitado".</summary>
public class Plan
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public decimal PrecioMensual { get; set; }
    public decimal PrecioAnual { get; set; }
    public int? MaxProductos { get; set; }
    public int? MaxPedidosMes { get; set; }
    public int? MaxRespuestas { get; set; }
}

public class Negocio
{
    public int Id { get; set; }
    public int PropietarioId { get; set; }
    public int PlanId { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public string Telefono { get; set; } = "";
    public string? Direccion { get; set; }
    public string? LogoUrl { get; set; }
    public string Slug { get; set; } = "";
    public bool EnvioHabilitado { get; set; }
    public string PlanPeriodo { get; set; } = "mensual"; // mensual | anual
    public DateTime? TrialExpiraEn { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public Usuario Propietario { get; set; } = null!;
    public Plan Plan { get; set; } = null!;
    public List<MiembroNegocio> Miembros { get; set; } = [];
    public List<Producto> Productos { get; set; } = [];
    public List<ZonaEnvio> ZonasEnvio { get; set; } = [];
    public List<Cupon> Cupones { get; set; } = [];
    public List<RespuestaRapida> RespuestasRapidas { get; set; } = [];
    public List<Pedido> Pedidos { get; set; } = [];
}

/// <summary>
/// Relación N:M entre usuarios y negocios (empleados). UsuarioId es NULL mientras
/// la invitación no fue aceptada.
/// </summary>
public class MiembroNegocio
{
    public int Id { get; set; }
    public int NegocioId { get; set; }
    public int? UsuarioId { get; set; }
    public string? EmailInvitado { get; set; }
    public RolMiembro Rol { get; set; } = RolMiembro.Empleado;
    public string TokenInvitacion { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime? AceptadoEn { get; set; }

    public Negocio Negocio { get; set; } = null!;
    public Usuario? Usuario { get; set; }
}
