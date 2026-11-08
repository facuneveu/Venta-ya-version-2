using VentaYa.App.Core.Models;
using VentaYa.App.Core.ViewModels;

namespace VentaYa.App.Pages;

public partial class PedidosPage : ContentPage
{
    private readonly PedidosViewModel _vm;

    public PedidosPage(PedidosViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;

        // Botones de filtro por estado: Todos, Pendiente, Confirmado, ...
        foreach (var filtro in PedidosViewModel.Filtros)
        {
            var boton = new Button { Text = filtro?.ToString() ?? "Todos", Style = (Style)Application.Current!.Resources["Chip"] };
            var estado = filtro;
            boton.Clicked += async (_, _) => await _vm.FiltrarCommand.ExecuteAsync(estado);
            Filtros.Add(boton);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CargarCommand.ExecuteAsync(null);
    }
}
