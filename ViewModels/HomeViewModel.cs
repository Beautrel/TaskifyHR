using System.Globalization;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using TaskifyHR.Models;
using TaskifyHR.Services;

namespace TaskifyHR.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly IAuthService _auth;
    private readonly IAttendanceService _attendance;
    private readonly ILeaveService _leave;
    private readonly ILocationService _location;

    private ClockStatus _status = ClockStatus.BeforeEntry;

    public HomeViewModel(IAuthService auth, IAttendanceService attendance,
                         ILeaveService leave, ILocationService location)
    {
        _auth = auth;
        _attendance = attendance;
        _leave = leave;
        _location = location;
    }

    [ObservableProperty] private string greeting = "Bonjour";
    [ObservableProperty] private string dateText = "";
    [ObservableProperty] private string workedTitle = "00 h 00 travaillées";
    [ObservableProperty] private string statusLine = "";
    [ObservableProperty] private string detailLine = "";
    [ObservableProperty] private string lastSync = "";
    [ObservableProperty] private string balanceText = "";
    [ObservableProperty] private bool isLoading;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(ClockButtonLabel))]
    private string clockButtonText = "Pointer mon entrée";

    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(ClockCommand))]
    private bool canClock = true;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(ClockButtonLabel)), NotifyCanExecuteChangedFor(nameof(ClockCommand))]
    private bool isBusy;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasConfirmation))]
    private string confirmation = "";

    // Carte d'erreur réutilisable (erreur réseau, hors zone, localisation refusée...)
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorTitle = "";
    [ObservableProperty] private string errorMessage = "";
    [ObservableProperty] private string errorActionText = "";
    [ObservableProperty] private ICommand? errorAction;

    public string ClockButtonLabel => IsBusy ? "Chargement…" : ClockButtonText;
    public bool HasConfirmation => !string.IsNullOrEmpty(Confirmation);

    [RelayCommand]
    private async Task LoadAsync()
    {
        HasError = false;
        IsLoading = true;
        try
        {
            var user = _auth.CurrentUser;
            Greeting = user is null ? "Bonjour" : $"Bonjour {user.FirstName}";
            DateText = FrenchDate(DateTime.Now);

            var balance = await _leave.GetBalanceAsync();
            BalanceText = $"{balance.Remaining} jours disponibles · {balance.Pending} jours en attente de validation.";

            ApplyDay(await _attendance.GetTodayAsync());
            LastSync = $"Dernière synchronisation : {DateTime.Now:HH:mm}";
        }
        catch (Exception)
        {
            ShowError("Erreur réseau", "Impossible de charger vos données. Vérifiez votre connexion.",
                      "Réessayer", LoadCommand);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanClockNow))]
    private async Task ClockAsync()
    {
        HasError = false;
        Confirmation = "";
        IsBusy = true;
        try
        {
            // La position n'est demandée qu'ici, au clic (pas de suivi en continu).
            var check = await _location.CheckAsync();
            switch (check)
            {
                case LocationCheck.PermissionDenied:
                    ShowError("Localisation refusée",
                              "La localisation sert uniquement au moment du pointage. Autorisez-la dans les réglages.",
                              "Ouvrir les réglages", new Command(() => AppInfo.Current.ShowSettingsUI()));
                    return;
                case LocationCheck.OutOfZone:
                    ShowError("Hors zone",
                              "Vous êtes en dehors de la zone de pointage. Rapprochez-vous de votre lieu de travail.",
                              "Réessayer", ClockCommand);
                    return;
                case LocationCheck.Unavailable:
                    ShowError("Localisation indisponible",
                              "Activez la localisation de votre téléphone puis réessayez.",
                              "Réessayer", ClockCommand);
                    return;
            }

            var day = _status == ClockStatus.BeforeEntry
                ? await _attendance.ClockInAsync()
                : await _attendance.ClockOutAsync();

            ApplyDay(day);
            Confirmation = _status == ClockStatus.InProgress
                ? $"● Entrée enregistrée à {Fmt(day.ClockIn)}"
                : $"● Sortie enregistrée à {Fmt(day.ClockOut)}";
        }
        catch (Exception)
        {
            ShowError("Erreur réseau", "Pointage non enregistré. Vérifiez votre connexion et réessayez.",
                      "Réessayer le pointage", ClockCommand);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanClockNow() => CanClock && !IsBusy;

    [RelayCommand]
    private async Task RequestLeaveAsync() =>
        await Shell.Current.GoToAsync("leave-request");

    // ----- Utilitaires -----

    private void ApplyDay(AttendanceDay day)
    {
        _status = day.ClockIn is null ? ClockStatus.BeforeEntry
                : day.ClockOut is null ? ClockStatus.InProgress
                : ClockStatus.Done;

        var end = day.ClockOut ?? TimeOnly.FromDateTime(DateTime.Now);
        var worked = day.ClockIn is null ? TimeSpan.Zero : end - day.ClockIn.Value;
        if (worked < TimeSpan.Zero) worked = TimeSpan.Zero;

        WorkedTitle = $"{(int)worked.TotalHours:00} h {worked.Minutes:00} travaillées";
        DetailLine = $"Entrée : {Fmt(day.ClockIn)} · Sortie : {Fmt(day.ClockOut)}";

        StatusLine = _status switch
        {
            ClockStatus.BeforeEntry => "Non pointée",
            ClockStatus.InProgress => "En cours · Sur site",
            _ => "Journée terminée"
        };
        ClockButtonText = _status switch
        {
            ClockStatus.BeforeEntry => "Pointer mon entrée",
            ClockStatus.InProgress => "Pointer ma sortie",
            _ => "Journée terminée"
        };
        CanClock = _status != ClockStatus.Done;
    }

    private void ShowError(string title, string message, string actionText, ICommand action)
    {
        ErrorTitle = title;
        ErrorMessage = message;
        ErrorActionText = actionText;
        ErrorAction = action;
        HasError = true;
    }

    private static string Fmt(TimeOnly? time) => time is null ? "—" : time.Value.ToString("HH:mm");

    private static string FrenchDate(DateTime date)
    {
        var s = date.ToString("dddd d MMMM yyyy", new CultureInfo("fr-FR"));
        return char.ToUpper(s[0]) + s[1..];
    }
}
