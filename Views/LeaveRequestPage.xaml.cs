using TaskifyHR.ViewModels;

namespace TaskifyHR.Views;

public partial class LeaveRequestPage : ContentPage
{
    private readonly LeaveRequestViewModel _viewModel;
    private bool _firstAppearing = true;

    public LeaveRequestPage(LeaveRequestViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        // Le ViewModel ne connaît pas la page : il lève un événement, la page fait défiler l'écran.
        _viewModel.ScrollToTopRequested += async () => await MainScroll.ScrollToAsync(0, 0, true);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_firstAppearing) return; // on ne réinitialise le formulaire qu'à l'ouverture
        _firstAppearing = false;
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
