using VentaYa.App.Core.ViewModels;

namespace VentaYa.App.Pages;

public partial class InicioPage : ContentPage
{
    private readonly InicioViewModel _vm;

    public InicioPage(InicioViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CargarCommand.ExecuteAsync(null); // se refresca cada vez que se vuelve a la pestaña
    }
}
