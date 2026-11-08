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
[Route("api/negocio")]
public class NegocioController(VentaYaDbContext db, NegocioActual actual) : ControllerBase
{
    /// <summary>Negocio del usuario logueado (como dueño o como empleado).</summary>
    [HttpGet]
    public async Task<ActionResult<NegocioDto>> Obtener()
    {
        var negocio = await actual.ObtenerOFallarAsync();
        return Ok(NegocioDto.Desde(negocio, actual.EsPropietario));
    }

    /// <summary>Onboarding: crea el negocio y arranca la prueba gratuita de 14 días.</summary>
    [HttpPost]
    public async Task<ActionResult<NegocioDto>> Crear(NegocioGuardarDto dto)
    {
        if (await actual.ObtenerAsync() is not null)
            throw new ReglaNegocioException("Ya tenés un negocio", 409);

        // Slug único: "mi-negocio", "mi-negocio-2", ...
        var baseSlug = ReglasNegocio.GenerarSlug(dto.Nombre);
        var slug = baseSlug;
        for (var n = 2; await db.Negocios.AnyAsync(x => x.Slug == slug); n++) slug = $"{baseSlug}-{n}";

        var negocio = new Negocio
        {
            PropietarioId = actual.UsuarioId,
            PlanId = PlanesIds.Prueba,
            TrialExpiraEn = DateTime.UtcNow.AddDays(14),
            Nombre = dto.Nombre.Trim(),
            Descripcion = dto.Descripcion?.Trim(),
            Telefono = dto.Telefono.Trim(),
            Direccion = dto.Direccion?.Trim(),
            EnvioHabilitado = dto.EnvioHabilitado,
            Slug = slug,
        };
        db.Negocios.Add(negocio);
        await db.SaveChangesAsync();

        await db.Entry(negocio).Reference(n => n.Plan).LoadAsync();
        return CreatedAtAction(nameof(Obtener), NegocioDto.Desde(negocio, esPropietario: true));
    }

    /// <summary>Edita el perfil del negocio (solo el propietario).</summary>
    [HttpPut]
    public async Task<ActionResult<NegocioDto>> Editar(NegocioGuardarDto dto)
    {
        var negocio = await actual.ObtenerComoPropietarioAsync();
        negocio.Nombre = dto.Nombre.Trim();
        negocio.Descripcion = dto.Descripcion?.Trim();
        negocio.Telefono = dto.Telefono.Trim();
        negocio.Direccion = dto.Direccion?.Trim();
        negocio.EnvioHabilitado = dto.EnvioHabilitado;
        await db.SaveChangesAsync();
        return Ok(NegocioDto.Desde(negocio, esPropietario: true));
    }
}
