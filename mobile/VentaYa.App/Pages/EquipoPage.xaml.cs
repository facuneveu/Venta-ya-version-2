using VentaYa.App.Core.ViewModels;

namespace VentaYa.App.Pages;

public partial class EquipoPage : ContentPage
{
    private readonly EquipoViewModel _vm;

    public EquipoPage(EquipoViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CargarCommand.ExecuteAsync(null);
    }

    private async void OnCompartirCodigo(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_vm.CodigoGenerado)) return;
        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = "Invitación a VentaYa",
            Text = $"Te invito a trabajar en mi negocio en VentaYa. Instalá la app, creá tu cuenta y en «¿Sos empleado?» pegá este código: {_vm.CodigoGenerado}",
        });
    }
}
