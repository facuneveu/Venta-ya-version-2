using VentaYa.App.Core.Models;
using VentaYa.App.Core.ViewModels;

namespace VentaYa.App.Pages;

public partial class RespuestasPage : ContentPage
{
    private readonly RespuestasViewModel _vm;

    public RespuestasPage(RespuestasViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        LblAyuda.Text = RespuestasViewModel.AyudaVariables;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CargarCommand.ExecuteAsync(null);
    }

    /// <summary>Comparte el mensaje tal cual (las variables sin datos de pedido quedan como {nombre}, {total}...).</summary>
    private async void OnCompartir(object? sender, EventArgs e)
    {
        if (sender is not Button { BindingContext: RespuestaDto respuesta }) return;
        var texto = await _vm.TextoParaCompartirAsync(respuesta);
        if (texto is not null)
            await Share.Default.RequestAsync(new ShareTextRequest { Text = texto, Title = respuesta.Titulo });
    }
}
