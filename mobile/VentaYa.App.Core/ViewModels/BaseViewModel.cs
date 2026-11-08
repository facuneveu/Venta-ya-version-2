using CommunityToolkit.Mvvm.ComponentModel;
using VentaYa.App.Core.Services;

namespace VentaYa.App.Core.ViewModels;

/// <summary>Base de todos los ViewModels: estado de "cargando" y manejo uniforme de errores de la API.</summary>
public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NoEstaOcupado))]
    private bool _estaOcupado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HayError))]
    private string? _error;

    public bool NoEstaOcupado => !EstaOcupado;
    public bool HayError => !string.IsNullOrEmpty(Error);

    /// <summary>Ejecuta una acción mostrando "cargando" y capturando los errores de la API en <see cref="Error"/>.</summary>
    protected async Task EjecutarAsync(Func<Task> accion)
    {
        if (EstaOcupado) return; // evita doble toque
        EstaOcupado = true;
        Error = null;
        try
        {
            await accion();
        }
        catch (ApiException ex)
        {
            Error = ex.Message;
        }
        finally
        {
            EstaOcupado = false;
        }
    }
}
