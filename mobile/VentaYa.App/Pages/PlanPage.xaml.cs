using VentaYa.App.Core.ViewModels;

namespace VentaYa.App.Pages;

public partial class PlanPage : ContentPage
{
    private readonly PlanViewModel _vm;

    public PlanPage(PlanViewModel vm)
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
