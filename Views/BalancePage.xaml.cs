using TaskifyHR.ViewModels;

namespace TaskifyHR.Views;

public partial class BalancePage : ContentPage
{
    private readonly BalanceViewModel _viewModel;

    public BalancePage(BalanceViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
