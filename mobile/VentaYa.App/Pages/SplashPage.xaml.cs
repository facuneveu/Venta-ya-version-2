using VentaYa.App.Core.ViewModels;

namespace VentaYa.App.Pages;

public partial class SplashPage : ContentPage
{
    private readonly SplashViewModel _vm;

    public SplashPage(SplashViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.IniciarCommand.ExecuteAsync(null);
    }
}
