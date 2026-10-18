using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskifyHR.Services;

namespace TaskifyHR.ViewModels;

public partial class ManagerViewModel : ObservableObject
{
    private readonly ILeaveService _leave;
    private readonly IAuthService _auth;

    public ManagerViewModel(ILeaveService leave, IAuthService auth)
    {
        _leave = leave;
        _auth = auth;
    }

    public ObservableCollection<ApprovalItem> Items { get; } = new();

    [ObservableProperty] private string heroTitle = "";
    [ObservableProperty] private string heroSubtitle = "";
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private bool isDenied;

    [RelayCommand]
    private async Task LoadAsync()
    {
        HasError = false;
        IsEmpty = false;

        // Règle : écran réservé au manager
        var user = _auth.CurrentUser;
        if (user is not { IsManager: true })
        {
            IsDenied = true;
            return;
        }
        IsDenied = false;
        HeroSubtitle = $"{user.FullName} · {user.Team}";

        IsLoading = true;
        try
        {
            var pending = await _leave.GetPendingApprovalsAsync();
            Items.Clear();
            foreach (var p in pending)
                Items.Add(new ApprovalItem(p, _leave, user.FullName, Refresh));
            Refresh();
        }
        catch (Exception)
        {
            HasError = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Recalcule le titre et l'état "liste vide" à chaque décision.
    private void Refresh()
    {
        var open = Items.Count(i => i.IsOpen);
        HeroTitle = open switch
        {
            0 => "Aucune demande à traiter",
            1 => "1 demande à traiter",
            _ => $"{open} demandes à traiter"
        };
        IsEmpty = open == 0;
    }

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}
