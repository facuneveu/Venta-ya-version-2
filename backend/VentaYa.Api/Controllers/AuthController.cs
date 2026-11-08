using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentaYa.Api.Data;
using VentaYa.Api.Dtos;
using VentaYa.Api.Models;
using VentaYa.Api.Services;

namespace VentaYa.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(VentaYaDbContext db, JwtService jwt, NegocioActual actual) : ControllerBase
{
    /// <summary>Crea una cuenta y devuelve el token (el usuario queda logueado).</summary>
    [HttpPost("registro")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthRespuestaDto>> Registro(RegistroDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        if (await db.Usuarios.AnyAsync(u => u.Email == email))
            throw new ReglaNegocioException("Ya existe una cuenta con ese email", 409);

        var usuario = new Usuario
        {
            Nombre = dto.Nombre.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password), // nunca se guarda la contraseña en claro
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        return Ok(new AuthRespuestaDto(jwt.Crear(usuario), new UsuarioDto(usuario.Id, usuario.Nombre, usuario.Email)));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthRespuestaDto>> Login(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
        if (usuario is null || !BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
            throw new ReglaNegocioException("Email o contraseña incorrectos", 401);

        return Ok(new AuthRespuestaDto(jwt.Crear(usuario), new UsuarioDto(usuario.Id, usuario.Nombre, usuario.Email)));
    }

    /// <summary>Datos del usuario logueado y de su negocio (null si todavía no creó uno).</summary>
    [HttpGet("yo")]
    [Authorize]
    public async Task<ActionResult<object>> Yo()
    {
        var usuario = await db.Usuarios.FindAsync(actual.UsuarioId)
                      ?? throw new ReglaNegocioException("Sesión no válida", 401);
        var negocio = await actual.ObtenerAsync();
        return Ok(new
        {
            usuario = new UsuarioDto(usuario.Id, usuario.Nombre, usuario.Email),
            negocio = negocio is null ? null : NegocioDto.Desde(negocio, actual.EsPropietario),
        });
    }
}
