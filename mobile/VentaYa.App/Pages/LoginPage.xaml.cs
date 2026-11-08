using VentaYa.App.Core.ViewModels;

namespace VentaYa.App.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LblServidor.Text = $"Servidor: {Configuracion.ApiBaseUrl}  (tocá para cambiar)";
    }

    /// <summary>Permite apuntar la app al emulador, a la PC de desarrollo o al servidor de MonsterASP sin recompilar.</summary>
    private async void OnCambiarServidor(object? sender, TappedEventArgs e)
    {
        var url = await DisplayPromptAsync("Servidor", "Dirección de la API de VentaYa:", "Guardar", "Cancelar",
            initialValue: Configuracion.ApiBaseUrl, keyboard: Keyboard.Url);
        if (url is null) return;
        if (!Configuracion.UrlValida(url))
        {
            await DisplayAlertAsync("Dirección inválida", "Escribila completa, por ejemplo https://mi-sitio.monsterasp.net/", "Aceptar");
            return;
        }
        Configuracion.GuardarUrl(url);
        LblServidor.Text = $"Servidor: {Configuracion.ApiBaseUrl}  (tocá para cambiar)";
        await DisplayAlertAsync("Listo", "Cerrá y volvé a abrir la app para usar el nuevo servidor.", "Aceptar");
    }
}
