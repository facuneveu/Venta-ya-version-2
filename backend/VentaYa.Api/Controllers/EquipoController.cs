using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentaYa.Api.Data;
using VentaYa.Api.Dtos;
using VentaYa.Api.Models;
using VentaYa.Api.Services;

namespace VentaYa.Api.Controllers;

/// <summary>
/// Multi-usuario: el propietario invita empleados con un token. El empleado se registra
/// (o inicia sesión) y acepta la invitación; desde ahí ve el negocio del dueño.
/// </summary>
[ApiController]
[Authorize]
[Route("api/equipo")]
public class EquipoController(VentaYaDbContext db, NegocioActual actual) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<MiembroDto>>> Listar()
    {
        var negocio = await actual.ObtenerComoPropietarioAsync();
        var lista = await db.MiembrosNegocio.Include(m => m.Usuario)
            .Where(m => m.NegocioId == negocio.Id).OrderBy(m => m.Id).ToListAsync();
        return Ok(lista.Select(Mapear).ToList());
    }

    /// <summary>Crea una invitación pendiente. Devuelve el token para compartir por WhatsApp.</summary>
    [HttpPost("invitar")]
    public async Task<ActionResult<MiembroDto>> Invitar(InvitarDto dto)
    {
        var negocio = await actual.ObtenerComoPropietarioAsync();
        var email = dto.Email.Trim().ToLowerInvariant();
        if (await db.MiembrosNegocio.AnyAsync(m => m.NegocioId == negocio.Id && m.EmailInvitado == email))
            throw new ReglaNegocioException("Ya invitaste a ese email", 409);

        var miembro = new MiembroNegocio { NegocioId = negocio.Id, EmailInvitado = email };
        db.MiembrosNegocio.Add(miembro);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Listar), Mapear(miembro));
    }

    /// <summary>Revoca el acceso de un empleado o cancela una invitación pendiente.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Revocar(int id)
    {
        var negocio = await actual.ObtenerComoPropietarioAsync();
        var miembro = await db.MiembrosNegocio.FirstOrDefaultAsync(m => m.Id == id && m.NegocioId == negocio.Id)
                      ?? throw new ReglaNegocioException("Miembro no encontrado", 404);
        db.MiembrosNegocio.Remove(miembro);
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>El usuario logueado acepta una invitación con el token que le pasó el dueño.</summary>
    [HttpPost("aceptar/{token}")]
    public async Task<ActionResult<NegocioDto>> Aceptar(string token)
    {
        var miembro = await db.MiembrosNegocio.Include(m => m.Negocio).ThenInclude(n => n.Plan)
                          .FirstOrDefaultAsync(m => m.TokenInvitacion == token)
                      ?? throw new ReglaNegocioException("La invitación no existe", 404);
        if (miembro.AceptadoEn is not null)
            throw new ReglaNegocioException("La invitación ya fue utilizada", 409);
        if (await actual.ObtenerAsync() is not null)
            throw new ReglaNegocioException("Tu cuenta ya pertenece a un negocio. Usá otra cuenta para ser empleado.", 409);

        miembro.UsuarioId = actual.UsuarioId;
        miembro.AceptadoEn = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(NegocioDto.Desde(miembro.Negocio, esPropietario: false));
    }

    private static MiembroDto Mapear(MiembroNegocio m) => new(
        m.Id, m.EmailInvitado, m.Usuario?.Nombre, m.Rol.ToString(), m.AceptadoEn is not null,
        m.AceptadoEn is null ? m.TokenInvitacion : null, m.AceptadoEn);
}
