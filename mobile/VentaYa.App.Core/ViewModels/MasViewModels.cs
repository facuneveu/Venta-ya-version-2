using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VentaYa.App.Core.Models;
using VentaYa.App.Core.Services;

namespace VentaYa.App.Core.ViewModels;

/// <summary>Pestaña Más: menú de configuración. El empleado no ve las opciones de propietario.</summary>
public partial class MasViewModel(Sesion sesion, INavegacion nav, IDialogos dialogos) : BaseViewModel
{
    public string NombreNegocio => sesion.Negocio?.Nombre ?? "";
    public string Usuario => sesion.Usuario?.Email ?? "";
    public bool EsPropietario => sesion.EsPropietario;

    [RelayCommand] private Task AbrirAsync(string ruta) => nav.IrAAsync(ruta);

    [RelayCommand]
    private async Task CerrarSesionAsync()
    {
        if (!await dialogos.ConfirmarAsync("Cerrar sesión", "¿Querés cerrar tu sesión?", "Cerrar sesión", "Cancelar")) return;
        sesion.Cerrar();
        await nav.IrAAsync(Rutas.Login, raiz: true);
    }
}

public partial class CuponesViewModel(IApiClient api, IDialogos dialogos) : BaseViewModel
{
    public static IReadOnlyList<TipoCupon> Tipos { get; } = Enum.GetValues<TipoCupon>();
    public ObservableCollection<CuponDto> Cupones { get; } = [];

    [ObservableProperty] private int? _editandoId;
    [ObservableProperty] private string _codigo = "";
    [ObservableProperty] private TipoCupon _tipo = TipoCupon.Porcentaje;
    [ObservableProperty] private string _valor = "";
    [ObservableProperty] private string _montoMinimo = "";
    [ObservableProperty] private string _usosMaximos = "";
    [ObservableProperty] private bool _activo = true;

    [RelayCommand]
    private Task CargarAsync() => EjecutarAsync(async () =>
    {
        Cupones.Clear();
        foreach (var c in await api.CuponesAsync()) Cupones.Add(c);
    });

    [RelayCommand]
    private void Editar(CuponDto c)
    {
        EditandoId = c.Id; Codigo = c.Codigo; Tipo = c.Tipo; Valor = c.Valor.ToString("0.##");
        MontoMinimo = c.MontoMinimo == 0 ? "" : c.MontoMinimo.ToString("0.##");
        UsosMaximos = c.UsosMaximos?.ToString() ?? ""; Activo = c.Activo;
    }

    [RelayCommand]
    private void Limpiar()
    {
        EditandoId = null; Codigo = ""; Tipo = TipoCupon.Porcentaje; Valor = ""; MontoMinimo = ""; UsosMaximos = ""; Activo = true; Error = null;
    }

    [RelayCommand]
    private Task GuardarAsync() => EjecutarAsync(async () =>
    {
        if (string.IsNullOrWhiteSpace(Codigo)) { Error = "Ingresá el código del cupón."; return; }
        if (!Formato.TryParseNumero(Valor, out var valor) || valor <= 0) { Error = "Ingresá un descuento mayor a 0."; return; }
        if (Tipo == TipoCupon.Porcentaje && valor > 100) { Error = "El porcentaje no puede superar 100."; return; }
        var minimo = 0m;
        if (!string.IsNullOrWhiteSpace(MontoMinimo) && !Formato.TryParseNumero(MontoMinimo, out minimo)) { Error = "El monto mínimo no es válido."; return; }
        int? usos = null;
        if (!string.IsNullOrWhiteSpace(UsosMaximos))
        {
            if (!int.TryParse(UsosMaximos.Trim(), out var u) || u < 1) { Error = "Los usos máximos deben ser un número mayor a 0."; return; }
            usos = u;
        }

        var dto = new CuponGuardarDto(Codigo.Trim(), Tipo, valor, minimo, usos, null, Activo);
        if (EditandoId is { } id) await api.EditarCuponAsync(id, dto); else await api.CrearCuponAsync(dto);
        Limpiar();
        Cupones.Clear();
        foreach (var c in await api.CuponesAsync()) Cupones.Add(c);
    });

    [RelayCommand]
    private Task EliminarAsync(CuponDto c) => EjecutarAsync(async () =>
    {
        if (!await dialogos.ConfirmarAsync("Eliminar cupón", $"¿Eliminar el cupón {c.Codigo}?", "Eliminar", "Cancelar")) return;
        await api.EliminarCuponAsync(c.Id); // si ya se usó, la API responde 409 con el motivo
        Cupones.Remove(c);
    });
}

public partial class ZonasViewModel(IApiClient api, IDialogos dialogos) : BaseViewModel
{
    public ObservableCollection<ZonaDto> Zonas { get; } = [];
    [ObservableProperty] private int? _editandoId;
    [ObservableProperty] private string _nombre = "";
    [ObservableProperty] private string _precio = "";
    [ObservableProperty] private bool _activa = true;

    [RelayCommand]
    private Task CargarAsync() => EjecutarAsync(async () =>
    {
        Zonas.Clear();
        foreach (var z in await api.ZonasAsync()) Zonas.Add(z);
    });

    [RelayCommand]
    private void Editar(ZonaDto z) { EditandoId = z.Id; Nombre = z.Nombre; Precio = z.Precio.ToString("0.##"); Activa = z.Activa; }

    [RelayCommand]
    private void Limpiar() { EditandoId = null; Nombre = ""; Precio = ""; Activa = true; Error = null; }

    [RelayCommand]
    private Task GuardarAsync() => EjecutarAsync(async () =>
    {
        if (string.IsNullOrWhiteSpace(Nombre)) { Error = "Ingresá el nombre de la zona."; return; }
        if (!Formato.TryParseNumero(Precio, out var precio) || precio < 0) { Error = "Ingresá un costo de envío válido."; return; }

        var dto = new ZonaGuardarDto(Nombre.Trim(), precio, Activa);
        if (EditandoId is { } id) await api.EditarZonaAsync(id, dto); else await api.CrearZonaAsync(dto);
        Limpiar();
        Zonas.Clear();
        foreach (var z in await api.ZonasAsync()) Zonas.Add(z);
    });

    [RelayCommand]
    private Task EliminarAsync(ZonaDto z) => EjecutarAsync(async () =>
    {
        if (!await dialogos.ConfirmarAsync("Eliminar zona", $"¿Eliminar la zona {z.Nombre}?", "Eliminar", "Cancelar")) return;
        await api.EliminarZonaAsync(z.Id);
        Zonas.Remove(z);
    });
}

public partial class RespuestasViewModel(IApiClient api, IDialogos dialogos) : BaseViewModel
{
    public ObservableCollection<RespuestaDto> Respuestas { get; } = [];
    [ObservableProperty] private int? _editandoId;
    [ObservableProperty] private string _titulo = "";
    [ObservableProperty] private string _contenido = "";

    public const string AyudaVariables = "Podés usar {nombre}, {total} y {link}.";

    [RelayCommand]
    private Task CargarAsync() => EjecutarAsync(async () =>
    {
        Respuestas.Clear();
        foreach (var r in await api.RespuestasAsync()) Respuestas.Add(r);
    });

    [RelayCommand]
    private void Editar(RespuestaDto r) { EditandoId = r.Id; Titulo = r.Titulo; Contenido = r.Contenido; }

    [RelayCommand]
    private void Limpiar() { EditandoId = null; Titulo = ""; Contenido = ""; Error = null; }

    [RelayCommand]
    private Task GuardarAsync() => EjecutarAsync(async () =>
    {
        if (string.IsNullOrWhiteSpace(Titulo) || string.IsNullOrWhiteSpace(Contenido)) { Error = "Completá el título y el mensaje."; return; }
        var dto = new RespuestaGuardarDto(Titulo.Trim(), Contenido.Trim());
        if (EditandoId is { } id) await api.EditarRespuestaAsync(id, dto); else await api.CrearRespuestaAsync(dto);
        Limpiar();
        Respuestas.Clear();
        foreach (var r in await api.RespuestasAsync()) Respuestas.Add(r);
    });

    [RelayCommand]
    private Task EliminarAsync(RespuestaDto r) => EjecutarAsync(async () =>
    {
        if (!await dialogos.ConfirmarAsync("Eliminar respuesta", $"¿Eliminar \"{r.Titulo}\"?", "Eliminar", "Cancelar")) return;
        await api.EliminarRespuestaAsync(r.Id);
        Respuestas.Remove(r);
    });

    /// <summary>Texto con las variables ya reemplazadas, listo para compartir por WhatsApp (la pantalla usa Launcher/Share).</summary>
    public async Task<string?> TextoParaCompartirAsync(RespuestaDto respuesta, int? pedidoId = null)
    {
        string? texto = null;
        await EjecutarAsync(async () => texto = (await api.RenderizarRespuestaAsync(respuesta.Id, pedidoId)).Texto);
        return texto;
    }
}

public partial class EquipoViewModel(IApiClient api, IDialogos dialogos) : BaseViewModel
{
    public ObservableCollection<MiembroDto> Miembros { get; } = [];
    [ObservableProperty] private string _emailInvitado = "";
    [ObservableProperty] private string? _codigoGenerado;

    [RelayCommand]
    private Task CargarAsync() => EjecutarAsync(async () =>
    {
        Miembros.Clear();
        foreach (var m in await api.EquipoAsync()) Miembros.Add(m);
    });

    /// <summary>Genera la invitación; el dueño comparte el código (por WhatsApp) con su empleado.</summary>
    [RelayCommand]
    private Task InvitarAsync() => EjecutarAsync(async () =>
    {
        if (string.IsNullOrWhiteSpace(EmailInvitado) || !EmailInvitado.Contains('@')) { Error = "Ingresá un email válido."; return; }
        var miembro = await api.InvitarAsync(new InvitarDto(EmailInvitado.Trim()));
        CodigoGenerado = miembro.TokenInvitacion;
        EmailInvitado = "";
        Miembros.Clear();
        foreach (var m in await api.EquipoAsync()) Miembros.Add(m);
    });

    [RelayCommand]
    private Task RevocarAsync(MiembroDto m) => EjecutarAsync(async () =>
    {
        var quien = m.Nombre ?? m.Email ?? "este miembro";
        if (!await dialogos.ConfirmarAsync("Revocar acceso", $"¿Quitar el acceso de {quien}?", "Quitar", "Cancelar")) return;
        await api.RevocarAsync(m.Id);
        Miembros.Remove(m);
    });
}

public partial class PerfilViewModel(IApiClient api, Sesion sesion, IDialogos dialogos) : BaseViewModel
{
    [ObservableProperty] private string _nombre = sesion.Negocio?.Nombre ?? "";
    [ObservableProperty] private string _descripcion = sesion.Negocio?.Descripcion ?? "";
    [ObservableProperty] private string _telefono = sesion.Negocio?.Telefono ?? "";
    [ObservableProperty] private string _direccion = sesion.Negocio?.Direccion ?? "";
    [ObservableProperty] private bool _envioHabilitado = sesion.Negocio?.EnvioHabilitado ?? false;
    public string? Slug => sesion.Negocio?.Slug;

    [RelayCommand]
    private Task GuardarAsync() => EjecutarAsync(async () =>
    {
        if (string.IsNullOrWhiteSpace(Nombre)) { Error = "El negocio necesita un nombre."; return; }
        if (Telefono.Trim().Length < 6) { Error = "Ingresá un teléfono de contacto válido."; return; }
        sesion.Negocio = await api.EditarNegocioAsync(new NegocioGuardarDto(
            Nombre.Trim(), string.IsNullOrWhiteSpace(Descripcion) ? null : Descripcion.Trim(),
            Telefono.Trim(), string.IsNullOrWhiteSpace(Direccion) ? null : Direccion.Trim(), EnvioHabilitado));
        await dialogos.AvisarAsync("Listo", "Guardamos los datos de tu negocio.");
    });
}

public partial class PlanViewModel(IApiClient api, Sesion sesion) : BaseViewModel
{
    [ObservableProperty] private UsoPlanDto? _uso;
    [ObservableProperty] private string _planActual = sesion.Negocio?.Plan ?? "";

    [RelayCommand]
    private Task CargarAsync() => EjecutarAsync(async () => Uso = (await api.PanelAsync()).Uso);

    /// <summary>Cambio de plan SIMULADO (sin cobro): sirve para mostrar cómo cambian los límites.</summary>
    [RelayCommand]
    private Task CambiarAsync(string plan) => EjecutarAsync(async () =>
    {
        sesion.Negocio = await api.CambiarPlanAsync(new CambiarPlanDto(plan));
        PlanActual = sesion.Negocio.Plan;
        Uso = (await api.PanelAsync()).Uso;
    });
}
