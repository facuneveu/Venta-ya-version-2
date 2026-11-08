using VentaYa.App.Core.ViewModels;

namespace VentaYa.App.Pages;

public partial class ProductoFormPage : ContentPage, IQueryAttributable
{
    private readonly ProductoFormViewModel _vm;

    public ProductoFormPage(ProductoFormViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    /// <summary>"producto-form" = producto nuevo; "producto-form?id=7" = editar el producto 7.</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var id)) _vm.Id = int.Parse(id.ToString()!);
        Title = _vm.Titulo;
        BtnEliminar.IsVisible = !_vm.EsNuevo;
        SeccionImagenes.IsVisible = !_vm.EsNuevo;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CargarCommand.ExecuteAsync(null);
    }

    private async void OnAgregarImagen(object? sender, EventArgs e)
    {
        try
        {
            var fotos = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions { Title = "Elegí una foto del producto", SelectionLimit = 1 });
            var foto = fotos?.FirstOrDefault();
            if (foto is null) return;
            await using var stream = await foto.OpenReadAsync();
            await _vm.SubirImagenAsync(stream, foto.FileName);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await DisplayAlertAsync("No se pudo abrir la galería", ex.Message, "Aceptar");
        }
    }
}
