using TaskifyHR.ViewModels;

namespace TaskifyHR.Views;

public partial class ManagerPage : ContentPage
{
    private readonly ManagerViewModel _viewModel;

    public ManagerPage(ManagerViewModel viewModel)
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
