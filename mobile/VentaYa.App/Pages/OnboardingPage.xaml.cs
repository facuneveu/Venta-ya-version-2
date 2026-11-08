using VentaYa.App.Core.ViewModels;

namespace VentaYa.App.Pages;

public partial class OnboardingPage : ContentPage
{
    public OnboardingPage(OnboardingViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
