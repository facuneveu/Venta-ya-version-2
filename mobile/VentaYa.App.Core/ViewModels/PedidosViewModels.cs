using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VentaYa.App.Core.Models;
using VentaYa.App.Core.Services;

namespace VentaYa.App.Core.ViewModels;

/// <summary>Pestaña Inicio: ventas, pedidos por estado y uso del plan.</summary>
public partial class InicioViewModel(IApiClient api, Sesion sesion, INavegacion nav) : BaseViewModel
{
    [ObservableProperty] private PanelDto? _panel;
    [ObservableProperty] private string _ventasMes = "$0";
    [ObservableProperty] private string _resumenPlan = "";
    [ObservableProperty] private string? _avisoPlan;
    [ObservableProperty] private int _pendientes;
    [ObservableProperty] private int _confirmados;
    [ObservableProperty] private int _pagados;
    [ObservableProperty] private int _entregados;

    public string NombreNegocio => sesion.Negocio?.Nombre ?? "";
    public ObservableCollection<PedidoDto> UltimosPedidos { get; } = [];

    [RelayCommand]
    private Task CargarAsync() => EjecutarAsync(async () =>
    {
        var panel = await api.PanelAsync();
        Panel = panel;
        VentasMes = Formato.Dinero(panel.VentasMes);
        UltimosPedidos.Clear();
        foreach (var p in panel.UltimosPedidos) UltimosPedidos.Add(p);

        var uso = panel.Uso;
        ResumenPlan = uso.DiasDePruebaRestantes is { } dias
            ? $"Plan Prueba · te quedan {dias} días con todo ilimitado"
            : $"Plan {uso.Plan}";
        // Aviso cuando se está cerca del límite del plan Gratis (80 %)
        AvisoPlan = null;
        if (uso is { MaxProductos: { } mp } && uso.Productos >= mp * 0.8)
            AvisoPlan = $"Usaste {uso.Productos} de {mp} productos de tu plan.";
        else if (uso is { MaxPedidosMes: { } mpm } && uso.PedidosMes >= mpm * 0.8)
            AvisoPlan = $"Llevás {uso.PedidosMes} de {mpm} pedidos este mes.";
        Pendientes = Cantidad("Pendiente");
        Confirmados = Cantidad("Confirmado");
        Pagados = Cantidad("Pagado");
        Entregados = Cantidad("Entregado");
        OnPropertyChanged(nameof(NombreNegocio));
    });

    [RelayCommand] private Task NuevoPedidoAsync() => nav.IrAAsync(Rutas.NuevoPedido);
    [RelayCommand] private Task AbrirPedidoAsync(PedidoDto pedido) => nav.IrAAsync(Rutas.PedidoDetalle, pedido.Id);

    public int Cantidad(string estado) => Panel?.PedidosPorEstado.GetValueOrDefault(estado) ?? 0;
}

/// <summary>Pestaña Pedidos: lista con filtro por estado.</summary>
public partial class PedidosViewModel(IApiClient api, INavegacion nav) : BaseViewModel
{
    /// <summary>null = todos.</summary>
    public static IReadOnlyList<EstadoPedido?> Filtros { get; } =
        [null, EstadoPedido.Pendiente, EstadoPedido.Confirmado, EstadoPedido.Pagado, EstadoPedido.Entregado, EstadoPedido.Cancelado];

    [ObservableProperty] private EstadoPedido? _filtro;
    public ObservableCollection<PedidoDto> Pedidos { get; } = [];
    [ObservableProperty] private bool _sinPedidos;

    [RelayCommand]
    private Task CargarAsync() => EjecutarAsync(async () =>
    {
        var lista = await api.PedidosAsync(Filtro);
        Pedidos.Clear();
        foreach (var p in lista) Pedidos.Add(p);
        SinPedidos = Pedidos.Count == 0;
    });

    [RelayCommand]
    private async Task FiltrarAsync(EstadoPedido? estado)
    {
        Filtro = estado;
        await CargarCommand.ExecuteAsync(null);
    }

    [RelayCommand] private Task NuevoPedidoAsync() => nav.IrAAsync(Rutas.NuevoPedido);
    [RelayCommand] private Task AbrirAsync(PedidoDto pedido) => nav.IrAAsync(Rutas.PedidoDetalle, pedido.Id);
}

public partial class PedidoDetalleViewModel(IApiClient api, IDialogos dialogos) : BaseViewModel
{
    [ObservableProperty] private PedidoDto? _pedido;
    public ObservableCollection<EstadoPedido> EstadosPermitidos { get; } = [];

    public int Id { get; set; }

    [RelayCommand]
    private Task CargarAsync() => EjecutarAsync(async () => Mostrar(await api.PedidoAsync(Id)));

    [RelayCommand]
    private Task CambiarEstadoAsync(EstadoPedido nuevo) => EjecutarAsync(async () =>
    {
        if (nuevo == EstadoPedido.Cancelado &&
            !await dialogos.ConfirmarAsync("Cancelar pedido", "Se devolverá el stock de los productos. ¿Cancelar el pedido?", "Sí, cancelar", "No"))
            return;
        Mostrar(await api.CambiarEstadoAsync(Id, nuevo));
    });

    private void Mostrar(PedidoDto pedido)
    {
        Pedido = pedido;
        EstadosPermitidos.Clear();
        foreach (var e in ReglasEstado.Siguientes(pedido.Estado)) EstadosPermitidos.Add(e);
    }
}

/// <summary>Una línea del pedido que se está armando.</summary>
public partial class LineaPedido(ProductoDto producto) : ObservableObject
{
    public ProductoDto Producto { get; } = producto;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Subtotal))] private int _cantidad;
    public decimal Subtotal => Cantidad * Producto.Precio;
}

public partial class NuevoPedidoViewModel(IApiClient api, Sesion sesion, INavegacion nav) : BaseViewModel
{
    public static IReadOnlyList<MetodoPago> MetodosPago { get; } = Enum.GetValues<MetodoPago>();

    public ObservableCollection<LineaPedido> Lineas { get; } = [];
    public ObservableCollection<ZonaDto> Zonas { get; } = [];

    [ObservableProperty] private string _clienteNombre = "";
    [ObservableProperty] private string _clienteTelefono = "";
    [ObservableProperty] private string _direccion = "";
    [ObservableProperty] private string _codigoCupon = "";
    [ObservableProperty] private string _notas = "";
    [ObservableProperty] private MetodoPago _metodoPago = MetodoPago.Efectivo;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(TotalEstimado))] private ZonaDto? _zona;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(TotalEstimado))] private decimal _subtotal;

    public bool HaceEnvios => sesion.Negocio?.EnvioHabilitado == true;

    /// <summary>Estimación mostrada al usuario; el descuento del cupón lo calcula y aplica el servidor.</summary>
    public decimal TotalEstimado => Subtotal + (Zona?.Precio ?? 0);

    [RelayCommand]
    private Task CargarAsync() => EjecutarAsync(async () =>
    {
        var productos = await api.ProductosAsync(soloActivos: true);
        Lineas.Clear();
        foreach (var p in productos) Lineas.Add(new LineaPedido(p));

        Zonas.Clear();
        if (HaceEnvios)
            foreach (var z in (await api.ZonasAsync()).Where(z => z.Activa)) Zonas.Add(z);
    });

    [RelayCommand]
    private void Sumar(LineaPedido linea)
    {
        // Si el producto controla stock no se puede pedir más de lo que hay
        if (linea.Producto.Stock is { } stock && linea.Cantidad >= stock) return;
        linea.Cantidad++;
        RecalcularSubtotal();
    }

    [RelayCommand]
    private void Restar(LineaPedido linea)
    {
        if (linea.Cantidad > 0) linea.Cantidad--;
        RecalcularSubtotal();
    }

    private void RecalcularSubtotal() => Subtotal = Lineas.Sum(l => l.Subtotal);

    [RelayCommand]
    private Task CrearAsync() => EjecutarAsync(async () =>
    {
        var items = Lineas.Where(l => l.Cantidad > 0).Select(l => new ItemPedidoCrearDto(l.Producto.Id, l.Cantidad)).ToList();
        if (items.Count == 0) { Error = "Agregá al menos un producto al pedido."; return; }
        if (string.IsNullOrWhiteSpace(ClienteNombre)) { Error = "Ingresá el nombre del cliente."; return; }
        if (ClienteTelefono.Trim().Length < 6) { Error = "Ingresá el teléfono del cliente."; return; }
        if (Zona is not null && string.IsNullOrWhiteSpace(Direccion)) { Error = "Ingresá la dirección de entrega."; return; }

        var pedido = await api.CrearPedidoAsync(new PedidoCrearDto(
            ClienteNombre.Trim(), ClienteTelefono.Trim(), items, MetodoPago, Zona?.Id,
            string.IsNullOrWhiteSpace(CodigoCupon) ? null : CodigoCupon.Trim(),
            string.IsNullOrWhiteSpace(Direccion) ? null : Direccion.Trim(),
            string.IsNullOrWhiteSpace(Notas) ? null : Notas.Trim()));

        await nav.VolverAsync();
        await nav.IrAAsync(Rutas.PedidoDetalle, pedido.Id);
    });
}
