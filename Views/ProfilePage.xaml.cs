using TaskifyHR.ViewModels;

namespace TaskifyHR.Views;

public partial class ProfilePage : ContentPage
{
    private readonly ProfileViewModel _viewModel;

    public ProfilePage(ProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    // Recharge à chaque affichage : après une reconnexion avec un autre compte, le profil est à jour.
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Load();
    }
}
