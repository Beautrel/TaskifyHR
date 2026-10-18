using TaskifyHR.ViewModels;

namespace TaskifyHR.Views;

public partial class LoginPage : ContentPage
{
    // Le ViewModel est fourni automatiquement par l'injection de dépendances (MauiProgram.cs).
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
