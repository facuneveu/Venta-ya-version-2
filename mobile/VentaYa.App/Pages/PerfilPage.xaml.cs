using VentaYa.App.Core.ViewModels;

namespace VentaYa.App.Pages;

public partial class PerfilPage : ContentPage
{
    private readonly PerfilViewModel _vm;

    public PerfilPage(PerfilViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        LblLink.Text = Link;
    }

    private string Link => $"{Configuracion.ApiBaseUrl}api/publico/{_vm.Slug}";

    private async void OnCompartirLink(object? sender, EventArgs e) =>
        await Share.Default.RequestAsync(new ShareTextRequest { Title = "Mi catálogo", Text = $"Mirá mi catálogo y hacé tu pedido: {Link}" });
}
