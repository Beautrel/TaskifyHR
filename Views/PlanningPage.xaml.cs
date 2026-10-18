using TaskifyHR.ViewModels;

namespace TaskifyHR.Views;

public partial class PlanningPage : ContentPage
{
    private readonly PlanningViewModel _viewModel;

    public PlanningPage(PlanningViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    // Recharge à chaque affichage : une décision du manager apparaît tout de suite (orange → vert).
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
