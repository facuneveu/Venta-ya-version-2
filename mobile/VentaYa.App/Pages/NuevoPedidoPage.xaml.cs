using VentaYa.App.Core.ViewModels;

namespace VentaYa.App.Pages;

public partial class NuevoPedidoPage : ContentPage
{
    private readonly NuevoPedidoViewModel _vm;

    public NuevoPedidoPage(NuevoPedidoViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CargarCommand.ExecuteAsync(null);
    }
}
