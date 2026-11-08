using VentaYa.App.Core.ViewModels;

namespace VentaYa.App.Pages;

public partial class CatalogoPage : ContentPage
{
    private readonly CatalogoViewModel _vm;

    public CatalogoPage(CatalogoViewModel vm)
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
