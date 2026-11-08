using VentaYa.Api.Models;
using VentaYa.Api.Services;

namespace VentaYa.Tests;

/// <summary>Tests unitarios de las reglas de negocio puras (sin base de datos).</summary>
public class ReglasNegocioTests
{
    private static readonly DateTime Ahora = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);

    private static Cupon CuponBase() => new()
    {
        Codigo = "PROMO10", Tipo = TipoCupon.Porcentaje, Valor = 10, MontoMinimo = 0, Activo = true,
    };

    [Fact]
    public void Descuento_Porcentaje_SeCalculaSobreElSubtotal() =>
        Assert.Equal(200m, ReglasNegocio.CalcularDescuento(CuponBase(), 2000m));

    [Fact]
    public void Descuento_MontoFijo_NoSuperaElSubtotal()
    {
        var cupon = new Cupon { Tipo = TipoCupon.MontoFijo, Valor = 5000 };
        Assert.Equal(3000m, ReglasNegocio.CalcularDescuento(cupon, 3000m));
        Assert.Equal(500m, ReglasNegocio.CalcularDescuento(new Cupon { Tipo = TipoCupon.MontoFijo, Valor = 500 }, 3000m));
    }

    [Fact]
    public void Cupon_Valido_DevuelveNull() =>
        Assert.Null(ReglasNegocio.ValidarCupon(CuponBase(), 1000m, 0, Ahora));

    [Fact]
    public void Cupon_Inactivo_Rechazado()
    {
        var cupon = CuponBase(); cupon.Activo = false;
        Assert.Equal("El cupón no está activo", ReglasNegocio.ValidarCupon(cupon, 1000m, 0, Ahora));
    }

    [Fact]
    public void Cupon_Vencido_Rechazado()
    {
        var cupon = CuponBase(); cupon.VenceEn = Ahora.AddDays(-1);
        Assert.Equal("El cupón está vencido", ReglasNegocio.ValidarCupon(cupon, 1000m, 0, Ahora));
    }

    [Fact]
    public void Cupon_PorDebajoDelMinimo_Rechazado()
    {
        var cupon = CuponBase(); cupon.MontoMinimo = 5000;
        Assert.Contains("mínimo", ReglasNegocio.ValidarCupon(cupon, 1000m, 0, Ahora));
    }

    [Fact]
    public void Cupon_ConUsosAgotados_Rechazado()
    {
        var cupon = CuponBase(); cupon.UsosMaximos = 3;
        Assert.Null(ReglasNegocio.ValidarCupon(cupon, 1000m, 2, Ahora));
        Assert.Equal("El cupón alcanzó su límite de usos", ReglasNegocio.ValidarCupon(cupon, 1000m, 3, Ahora));
    }

    [Theory]
    [InlineData(EstadoPedido.Pendiente, EstadoPedido.Confirmado, true)]
    [InlineData(EstadoPedido.Pendiente, EstadoPedido.Pagado, true)]
    [InlineData(EstadoPedido.Pendiente, EstadoPedido.Entregado, false)]
    [InlineData(EstadoPedido.Confirmado, EstadoPedido.Entregado, true)]
    [InlineData(EstadoPedido.Pagado, EstadoPedido.Entregado, true)]
    [InlineData(EstadoPedido.Pagado, EstadoPedido.Pendiente, false)]
    [InlineData(EstadoPedido.Entregado, EstadoPedido.Cancelado, false)]
    [InlineData(EstadoPedido.Cancelado, EstadoPedido.Pendiente, false)]
    public void Transiciones_DeEstado(EstadoPedido desde, EstadoPedido hacia, bool esperado) =>
        Assert.Equal(esperado, ReglasNegocio.TransicionValida(desde, hacia));

    [Fact]
    public void Prueba_Vigente_NoEstaVencida()
    {
        var plan = new Plan { Id = PlanesIds.Prueba };
        Assert.False(ReglasNegocio.PruebaVencida(plan, Ahora.AddDays(3), Ahora));
        Assert.True(ReglasNegocio.PruebaVencida(plan, Ahora.AddDays(-1), Ahora));
    }

    [Fact]
    public void PlanPago_NuncaVence() =>
        Assert.False(ReglasNegocio.PruebaVencida(new Plan { Id = PlanesIds.Pago }, null, Ahora));

    [Theory]
    [InlineData("Café Ñandú", "cafe-nandu")]
    [InlineData("  Mi   Kiosco!! ", "mi-kiosco")]
    [InlineData("!!!", "negocio")]
    public void Slug_SeNormaliza(string nombre, string esperado) =>
        Assert.Equal(esperado, ReglasNegocio.GenerarSlug(nombre));
}
