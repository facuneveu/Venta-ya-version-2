using Microsoft.EntityFrameworkCore;
using VentaYa.Api.Data;
using VentaYa.Api.Models;

namespace VentaYa.Api.Services;

/// <summary>
/// Resuelve "el negocio del usuario logueado": primero como propietario y, si no tiene
/// uno propio, como empleado aceptado. Todos los controllers filtran por este negocio,
/// así un usuario nunca ve datos de otro negocio.
/// </summary>
public class NegocioActual(IHttpContextAccessor http, VentaYaDbContext db)
{
    private Negocio? _negocio;
    private bool _cargado;

    public int UsuarioId =>
        int.Parse(http.HttpContext!.User.FindFirst("sub")?.Value
                  ?? throw new ReglaNegocioException("Sesión no válida", 401));

    public bool EsPropietario { get; private set; }

    public async Task<Negocio?> ObtenerAsync()
    {
        if (_cargado) return _negocio;
        var usuarioId = UsuarioId;

        _negocio = await db.Negocios.Include(n => n.Plan)
            .FirstOrDefaultAsync(n => n.PropietarioId == usuarioId);
        EsPropietario = _negocio is not null;

        if (_negocio is null)
        {
            _negocio = await db.Negocios.Include(n => n.Plan)
                .FirstOrDefaultAsync(n => n.Miembros.Any(m => m.UsuarioId == usuarioId && m.AceptadoEn != null));
        }
        _cargado = true;
        return _negocio;
    }

    public async Task<Negocio> ObtenerOFallarAsync() =>
        await ObtenerAsync() ?? throw new ReglaNegocioException("Todavía no tenés un negocio. Creá uno primero.", 404);

    /// <summary>Para acciones que solo puede hacer el dueño (perfil, plan, equipo).</summary>
    public async Task<Negocio> ObtenerComoPropietarioAsync()
    {
        var negocio = await ObtenerOFallarAsync();
        if (!EsPropietario) throw new ReglaNegocioException("Esta acción es solo para el propietario del negocio", 403);
        return negocio;
    }
}
