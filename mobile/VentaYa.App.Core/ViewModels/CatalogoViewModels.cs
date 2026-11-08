using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VentaYa.App.Core.Models;
using VentaYa.App.Core.Services;

namespace VentaYa.App.Core.ViewModels;

/// <summary>Pestaña Catálogo: lista y búsqueda de productos.</summary>
public partial class CatalogoViewModel(IApiClient api, INavegacion nav) : BaseViewModel
{
    [ObservableProperty] private string _busqueda = "";
    [ObservableProperty] private bool _sinProductos;
    public ObservableCollection<ProductoDto> Productos { get; } = [];

    [RelayCommand]
    private Task CargarAsync() => EjecutarAsync(async () =>
    {
        var lista = await api.ProductosAsync(Busqueda);
        Productos.Clear();
        foreach (var p in lista) Productos.Add(p);
        SinProductos = Productos.Count == 0;
    });

    [RelayCommand] private Task NuevoAsync() => nav.IrAAsync(Rutas.ProductoForm);
    [RelayCommand] private Task EditarAsync(ProductoDto producto) => nav.IrAAsync(Rutas.ProductoForm, producto.Id);
}

/// <summary>Alta y edición de un producto (Id = 0 → producto nuevo).</summary>
public partial class ProductoFormViewModel(IApiClient api, INavegacion nav, IDialogos dialogos) : BaseViewModel
{
    [ObservableProperty] private string _nombre = "";
    [ObservableProperty] private string _descripcion = "";
    [ObservableProperty] private string _precio = "";
    [ObservableProperty] private string _stock = "";
    [ObservableProperty] private bool _activo = true;
    public ObservableCollection<ImagenDto> Imagenes { get; } = [];

    public int Id { get; set; }
    public bool EsNuevo => Id == 0;
    public string Titulo => EsNuevo ? "Nuevo producto" : "Editar producto";

    [RelayCommand]
    private Task CargarAsync() => EjecutarAsync(async () =>
    {
        if (EsNuevo) return;
        var p = await api.ProductoAsync(Id);
        Nombre = p.Nombre;
        Descripcion = p.Descripcion ?? "";
        Precio = p.Precio.ToString("0.##");
        Stock = p.Stock?.ToString() ?? "";
        Activo = p.Activo;
        Imagenes.Clear();
        foreach (var i in p.Imagenes) Imagenes.Add(i);
    });

    [RelayCommand]
    private Task GuardarAsync() => EjecutarAsync(async () =>
    {
        if (string.IsNullOrWhiteSpace(Nombre)) { Error = "Ingresá el nombre del producto."; return; }
        if (!Formato.TryParseNumero(Precio, out var precio) || precio < 0) { Error = "Ingresá un precio válido."; return; }

        int? stock = null; // vacío = no se controla stock
        if (!string.IsNullOrWhiteSpace(Stock))
        {
            if (!int.TryParse(Stock.Trim(), out var s) || s < 0) { Error = "El stock debe ser un número entero (o dejalo vacío)."; return; }
            stock = s;
        }

        var dto = new ProductoGuardarDto(Nombre.Trim(), string.IsNullOrWhiteSpace(Descripcion) ? null : Descripcion.Trim(), precio, stock, Activo);
        if (EsNuevo) await api.CrearProductoAsync(dto);
        else await api.EditarProductoAsync(Id, dto);
        await nav.VolverAsync();
    });

    [RelayCommand]
    private Task EliminarAsync() => EjecutarAsync(async () =>
    {
        if (EsNuevo) return;
        if (!await dialogos.ConfirmarAsync("Eliminar producto", $"¿Eliminar \"{Nombre}\"?", "Eliminar", "Cancelar")) return;
        await api.EliminarProductoAsync(Id); // si ya se vendió, la API responde 409 y se muestra el mensaje
        await nav.VolverAsync();
    });

    /// <summary>La pantalla entrega el archivo elegido (galería o cámara).</summary>
    public Task SubirImagenAsync(Stream contenido, string nombreArchivo) => EjecutarAsync(async () =>
    {
        if (EsNuevo) { Error = "Guardá el producto antes de agregar imágenes."; return; }
        Imagenes.Add(await api.SubirImagenAsync(Id, contenido, nombreArchivo));
    });

    [RelayCommand]
    private Task EliminarImagenAsync(ImagenDto imagen) => EjecutarAsync(async () =>
    {
        await api.EliminarImagenAsync(Id, imagen.Id);
        Imagenes.Remove(imagen);
    });
}
