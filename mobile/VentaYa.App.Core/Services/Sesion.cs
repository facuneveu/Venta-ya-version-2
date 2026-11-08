using VentaYa.App.Core.Models;

namespace VentaYa.App.Core.Services;

/// <summary>Guarda el token de forma segura (en MAUI: SecureStorage; en tests: memoria).</summary>
public interface IAlmacenSeguro
{
    Task<string?> LeerAsync();
    Task GuardarAsync(string token);
    Task BorrarAsync();
}

public class AlmacenEnMemoria : IAlmacenSeguro
{
    private string? _token;
    public Task<string?> LeerAsync() => Task.FromResult(_token);
    public Task GuardarAsync(string token) { _token = token; return Task.CompletedTask; }
    public Task BorrarAsync() { _token = null; return Task.CompletedTask; }
}

/// <summary>Estado de la sesión: quién está logueado y a qué negocio pertenece.</summary>
public class Sesion(IAlmacenSeguro almacen)
{
    public string? Token { get; private set; }
    public UsuarioDto? Usuario { get; set; }
    public NegocioDto? Negocio { get; set; }

    public bool EstaLogueado => Token is not null;
    public bool EsPropietario => Negocio?.Rol == "Propietario";

    /// <summary>Se dispara cuando la sesión se cierra (logout o token vencido): la app vuelve al login.</summary>
    public event Action? SesionCerrada;

    public async Task RestaurarAsync() => Token = await almacen.LeerAsync();

    public async Task IniciarAsync(AuthRespuestaDto respuesta)
    {
        Token = respuesta.Token;
        Usuario = respuesta.Usuario;
        await almacen.GuardarAsync(respuesta.Token);
    }

    public void Cerrar()
    {
        var estabaLogueado = Token is not null;
        Token = null; Usuario = null; Negocio = null;
        _ = almacen.BorrarAsync();
        if (estabaLogueado) SesionCerrada?.Invoke();
    }
}
