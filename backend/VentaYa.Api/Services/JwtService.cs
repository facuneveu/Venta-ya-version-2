using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using VentaYa.Api.Models;

namespace VentaYa.Api.Services;

/// <summary>Genera el token JWT que la app móvil manda en cada request (Authorization: Bearer ...).</summary>
public class JwtService(IConfiguration config)
{
    public string Crear(Usuario usuario)
    {
        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", usuario.Id.ToString()),
                new Claim("email", usuario.Email),
                new Claim("name", usuario.Nombre),
            ]),
            Issuer = config["Jwt:Issuer"],
            Audience = config["Jwt:Audience"],
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
