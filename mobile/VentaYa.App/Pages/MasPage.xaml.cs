using VentaYa.App.Core.ViewModels;

namespace VentaYa.App.Pages;

public partial class MasPage : ContentPage
{
    private readonly MasViewModel _vm;

    public MasPage(MasViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.ActualizarCommand.Execute(null);
    }
}
