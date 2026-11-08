using VentaYa.App.Core.ViewModels;

namespace VentaYa.App.Pages;

public partial class ZonasPage : ContentPage
{
    private readonly ZonasViewModel _vm;

    public ZonasPage(ZonasViewModel vm)
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
