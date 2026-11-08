using VentaYa.App.Core.ViewModels;

namespace VentaYa.App.Pages;

public partial class CuponesPage : ContentPage
{
    private readonly CuponesViewModel _vm;

    public CuponesPage(CuponesViewModel vm)
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
