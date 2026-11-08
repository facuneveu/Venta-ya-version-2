using VentaYa.Api.Services;

namespace VentaYa.Tests;

public class RespuestasRapidasTests
{
    [Fact]
    public void ReemplazaTodasLasVariables()
    {
        var texto = ReglasNegocio.RenderizarRespuesta(
            "Hola {nombre}, tu pedido es {total}. Mirá el catálogo: {link}", "Ana", 6700m, "https://x/api/publico/kiosco");
        Assert.Equal("Hola Ana, tu pedido es $6.700. Mirá el catálogo: https://x/api/publico/kiosco", texto);
    }

    [Fact]
    public void FormateaDecimalesAlEstiloArgentino() =>
        Assert.Equal("Total $1.234.567,5", ReglasNegocio.RenderizarRespuesta("Total {total}", null, 1234567.5m, null));

    [Fact]
    public void SinDatos_DejaLasVariablesSinTocar() =>
        Assert.Equal("Hola {nombre}, {total}", ReglasNegocio.RenderizarRespuesta("Hola {nombre}, {total}", null, null, null));

    [Fact]
    public void LasVariablesNoDistinguenMayusculas() =>
        Assert.Equal("Hola Ana", ReglasNegocio.RenderizarRespuesta("Hola {NOMBRE}", "Ana", null, null));
}
