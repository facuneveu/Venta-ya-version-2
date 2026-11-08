using VentaYa.App.Core.ViewModels;

namespace VentaYa.App.Pages;

public partial class RegistroPage : ContentPage
{
    public RegistroPage(RegistroViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
