using VentaYa.App.Core.Services;
using VentaYa.App.Core.ViewModels;

namespace VentaYa.App.Pages;

public partial class PedidoDetallePage : ContentPage, IQueryAttributable
{
    private readonly PedidoDetalleViewModel _vm;
    private readonly IApiClient _api;

    public PedidoDetallePage(PedidoDetalleViewModel vm, IApiClient api)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _api = api;
    }

    /// <summary>Recibe el id del pedido desde la navegación: "pedido-detalle?id=5".</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var id)) _vm.Id = int.Parse(id.ToString()!);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CargarCommand.ExecuteAsync(null);
    }

    private async void OnWhatsApp(object? sender, EventArgs e)
    {
        if (_vm.Pedido is null) return;
        var numero = new string(_vm.Pedido.ClienteTelefono.Where(char.IsDigit).ToArray());
        if (numero.Length == 0) return;
        await Launcher.Default.OpenAsync($"https://wa.me/{numero}");
    }

    /// <summary>Elige una plantilla, reemplaza {nombre}/{total}/{link} con los datos del pedido y la comparte (WhatsApp, etc.).</summary>
    private async void OnRespuestaRapida(object? sender, EventArgs e)
    {
        try
        {
            var respuestas = await _api.RespuestasAsync();
            if (respuestas.Count == 0)
            {
                await DisplayAlertAsync("Respuestas rápidas", "Todavía no tenés respuestas rápidas. Creá una desde Más → Respuestas rápidas.", "Aceptar");
                return;
            }
            var elegida = await DisplayActionSheetAsync("Elegí un mensaje", "Cancelar", null, respuestas.Select(r => r.Titulo).ToArray());
            var respuesta = respuestas.FirstOrDefault(r => r.Titulo == elegida);
            if (respuesta is null) return;

            var texto = (await _api.RenderizarRespuestaAsync(respuesta.Id, _vm.Id)).Texto;
            await Share.Default.RequestAsync(new ShareTextRequest { Text = texto, Title = "Enviar mensaje" });
        }
        catch (ApiException ex)
        {
            await DisplayAlertAsync("No se pudo", ex.Message, "Aceptar");
        }
    }
}
